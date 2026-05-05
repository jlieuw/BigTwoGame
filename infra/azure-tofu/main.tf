# ── ACR name suffix — must be globally unique, 5-50 alphanumeric chars ────────
resource "random_id" "acr" {
  byte_length = 4
}

# ── Resource group ─────────────────────────────────────────────────────────────
resource "azurerm_resource_group" "main" {
  name     = "bigtwo-rg"
  location = var.azure_location
}

# ── Container Registry ─────────────────────────────────────────────────────────
resource "azurerm_container_registry" "main" {
  name                = "bigtwoacr${random_id.acr.hex}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "Basic"
  admin_enabled       = true  # allows password auth used by Container Apps
}

# ── Container Apps environment ─────────────────────────────────────────────────
resource "azurerm_container_app_environment" "main" {
  name                = "bigtwo-env"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
}

# ── Backend Container App ──────────────────────────────────────────────────────
# min_replicas = 1: always one instance alive so in-memory game state is preserved.
# max_replicas = 1: all players share the same instance (no sharding needed).
# External ingress is enabled so the frontend nginx can proxy /gamehub to it
# using the public FQDN — the same nginx-proxy pattern used for Scaleway.
resource "azurerm_container_app" "backend" {
  name                         = "bigtwo-backend"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  registry {
    server               = azurerm_container_registry.main.login_server
    username             = azurerm_container_registry.main.admin_username
    password_secret_name = "acr-pwd"
  }

  secret {
    name  = "acr-pwd"
    value = azurerm_container_registry.main.admin_password
  }

  ingress {
    external_enabled = true
    target_port      = 5000
    transport        = "http"
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = var.backend_min_replicas
    max_replicas = 1

    container {
      name   = "backend"
      image  = "${azurerm_container_registry.main.login_server}/bigtwo-backend:${var.image_tag}"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }
      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:5000"
      }
    }
  }
}

# ── Frontend Container App ─────────────────────────────────────────────────────
# min_replicas = 0: scales to zero when nobody is playing (nginx is stateless).
# nginx.conf.template substitutes BACKEND_URL at container start, so /gamehub
# requests are proxied to the backend over HTTPS without the browser needing
# to know the backend URL directly.
resource "azurerm_container_app" "frontend" {
  name                         = "bigtwo-frontend"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  registry {
    server               = azurerm_container_registry.main.login_server
    username             = azurerm_container_registry.main.admin_username
    password_secret_name = "acr-pwd"
  }

  secret {
    name  = "acr-pwd"
    value = azurerm_container_registry.main.admin_password
  }

  ingress {
    external_enabled = true
    target_port      = 80
    transport        = "http"
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "frontend"
      image  = "${azurerm_container_registry.main.login_server}/bigtwo-frontend:${var.image_tag}"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "BACKEND_URL"
        value = "https://${azurerm_container_app.backend.ingress[0].fqdn}"
      }
    }
  }

  depends_on = [azurerm_container_app.backend]
}
