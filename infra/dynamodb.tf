resource "aws_dynamodb_table" "toongabbie_tenant_table" {
  name           = "${local.stack_prefix}-ToongabbieTenants"
  billing_mode   = "PROVISIONED"
  read_capacity  = 1
  write_capacity = 1
  hash_key       = "RoomNumber"

  attribute {
    name = "RoomNumber"
    type = "N"
  }
}

resource "aws_dynamodb_table" "efergy_sensors_table" {
  name           = "${local.stack_prefix}-EfergySensors"
  billing_mode   = "PROVISIONED"
  read_capacity  = 1
  write_capacity = 1
  hash_key       = "Sid"


  attribute {
    name = "Sid"
    type = "S"
  }

}

resource "aws_dynamodb_table" "daily_heater_usage_table" {
  name           = "${local.stack_prefix}-DailyHeaterUsage"
  billing_mode   = "PROVISIONED"
  read_capacity  = 1
  write_capacity = 1
  hash_key       = "Sid"
  range_key      = "Date"

  attribute {
    name = "Sid"
    type = "S"
  }

  attribute {
    name = "Date"
    type = "S"
  }
}


resource "aws_dynamodb_table" "weekly_heater_usage_table" {
  name           = "${local.stack_prefix}-WeeklyHeaterUsage"
  billing_mode   = "PROVISIONED"
  read_capacity  = 1
  write_capacity = 1
  hash_key       = "Sid"
  range_key      = "StartDate"

  attribute {
    name = "Sid"
    type = "S"
  }

  attribute {
    name = "StartDate"
    type = "S"
  }

}