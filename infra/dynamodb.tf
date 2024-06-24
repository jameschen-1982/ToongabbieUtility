resource "aws_dynamodb_table" "toongabbie-tenant-table" {
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

resource "aws_dynamodb_table" "efergy-sensors-table" {
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