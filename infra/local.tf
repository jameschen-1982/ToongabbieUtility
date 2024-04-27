locals {
  stack_prefix = "tu-${var.env}-${random_string.rnd.id}"
}