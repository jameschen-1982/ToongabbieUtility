variable "env" {
  default = "prod"
}

variable "stack_region" {
  default = "ap-southeast-2"
}

variable "tf_state_bucket" {
  default = "c2j"
}

variable "dynamo_table_arns" {
  type  = list(string)
  default = [
    "arn:aws:dynamodb:ap-southeast-2:773631419510:table/ToongabbieTenants", 
    "arn:aws:dynamodb:ap-southeast-2:773631419510:table/EfergySensors",
    "arn:aws:dynamodb:ap-southeast-2:773631419510:table/test5-app-DDBTable-1M2H022KQT2KL"
  ]
}
