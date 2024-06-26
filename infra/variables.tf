variable "env" {
  default = "prod"
}

variable "stack_region" {
  default = "ap-southeast-2"
}

variable "tf_state_bucket" {
  default = "c2j"
}

variable "heater_bill_sns_topic_arn" {
  default = "arn:aws:sns:ap-southeast-2:773631419510:test5-app-MySNSTopic-Z7HWKK6EJVXQ"
}