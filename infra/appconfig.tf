resource "aws_appconfig_application" "app_stack" {
  name        = "${local.stack_prefix}-application-stack-config"
  description = "AppConfig for all applications in an environment"
}

resource "aws_appconfig_environment" "app_stack_env" {
  name           = var.env
  description    = "Toongabbie Utility ${var.env} Environment"
  application_id = aws_appconfig_application.app_stack.id
}

resource "aws_appconfig_configuration_profile" "profile" {
  application_id = aws_appconfig_application.app_stack.id
  description    = "Configuration Profile"
  name           = "${local.stack_prefix}-config"
  location_uri   = "hosted"
}