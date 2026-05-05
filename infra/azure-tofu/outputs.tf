output "frontend_url" {
  description = "Public HTTPS URL of the Big Two web app"
  value       = "https://${azurerm_container_app.frontend.ingress[0].fqdn}"
}

output "backend_url" {
  description = "Public HTTPS URL of the backend (proxied via nginx; not accessed by browsers directly)"
  value       = "https://${azurerm_container_app.backend.ingress[0].fqdn}"
}

output "acr_login_server" {
  description = "ACR login server — tag images as <acr_login_server>/bigtwo-backend:<tag>"
  value       = azurerm_container_registry.main.login_server
}

output "acr_admin_username" {
  description = "ACR admin username for docker login"
  value       = azurerm_container_registry.main.admin_username
}

output "acr_admin_password" {
  description = "ACR admin password for docker login"
  value       = azurerm_container_registry.main.admin_password
  sensitive   = true
}
