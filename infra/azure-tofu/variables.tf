variable "azure_subscription_id" {
  description = "Azure Subscription ID"
  type        = string
  sensitive   = true
}

variable "azure_location" {
  description = "Azure region (e.g. westeurope, eastus)"
  type        = string
  default     = "westeurope"
}

variable "image_tag" {
  description = "Docker image tag to deploy. Defaults to 'latest'; GitHub Actions passes the commit SHA."
  type        = string
  default     = "latest"
}

variable "backend_min_replicas" {
  description = <<-EOT
    Minimum replicas for the backend container.
    Keep at 1 — game state is in memory so all players must share one instance.
    Set to 0 only to save cost when the app is unused (active games will be lost
    on the next cold-start).
  EOT
  type    = number
  default = 1
}
