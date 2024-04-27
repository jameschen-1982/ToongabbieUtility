resource "aws_scheduler_schedule_group" "schedule_group" {
  name = "${local.stack_prefix}-schedule-group"
}