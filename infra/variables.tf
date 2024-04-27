variable "env" {
  default = "prod"
}

variable "stack_region" {
  default = "ap-southeast-2"
}

variable "tf_state_bucket" {
  default = "c2j"
}

variable "tenant_table_arn" {
  default = "arn:aws:dynamodb:ap-southeast-2:773631419510:table/ToongabbieTenants"
}