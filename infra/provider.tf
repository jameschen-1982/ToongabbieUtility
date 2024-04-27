provider "aws" {
  region = "ap-southeast-2"
  default_tags {
    tags = {
      Environment = var.env
      Application = "ToongabbieUtiility"
    }
  }
}


terraform {
  backend "s3" {
    bucket = "c2j"
    key    = "terraform-states/toongabbie-utility-prod/terraform.tfstate"
  }
}