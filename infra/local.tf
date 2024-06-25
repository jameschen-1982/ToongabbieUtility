locals {
  stack_prefix = "tu-${var.env}-${random_string.rnd.id}"
}

data "aws_caller_identity" "current" {}

data "aws_region" "current" {}