resource "aws_lambda_function" "roster_duty_reminder" {
  function_name = "${local.stack_prefix}-roster-duty-reminder"

  s3_bucket = aws_s3_bucket.lambda_bucket.id
  s3_key    = aws_s3_object.roster_duty_reminder_artifact.key

  runtime     = "provided.al2023"
  handler     = "not-used-in-custom-runtime"
  timeout     = 30 # seconds
  memory_size = 512

  source_code_hash = data.archive_file.roster_duty_reminder_archive.output_base64sha256

  role = aws_iam_role.roster_duty_reminder_lambda_role.arn
}

data "archive_file" "roster_duty_reminder_archive" {
  type        = "zip"
  source_dir  = "${path.module}/../RosterDutyReminder/lambda/ToongabbieUtility.RosterDutyReminder/bin/Release/net8.0/linux-x64/publish"
  output_path = "${path.module}/../dist/roster-duty-reminder.zip"

}

resource "aws_s3_object" "roster_duty_reminder_artifact" {
  bucket = aws_s3_bucket.lambda_bucket.id
  key    = "roster-duty-reminder.zip"
  source = data.archive_file.roster_duty_reminder_archive.output_path
  etag   = filemd5(data.archive_file.roster_duty_reminder_archive.output_path)
}

resource "aws_iam_role" "roster_duty_reminder_lambda_role" {
  name = "${local.stack_prefix}-lambda-rdr-role"

  assume_role_policy = jsonencode({
    "Version" : "2012-10-17",
    "Statement" : [
      {
        "Effect" : "Allow",
        "Principal" : {
          "Service" : "lambda.amazonaws.com"
        },
        "Action" : "sts:AssumeRole"
      }
    ]
  })
}

data "aws_iam_policy_document" "roster_duty_reminder_lambda_policy_doc" {
  statement {
    sid    = "AllowInvokingLambdas"
    effect = "Allow"

    resources = [
      "arn:aws:lambda:*:*:function:*"
    ]

    actions = [
      "lambda:InvokeFunction"
    ]
  }

  statement {
    sid    = "AllowCreatingLogGroups"
    effect = "Allow"

    resources = [
      "arn:aws:logs:*:*:*"
    ]

    actions = [
      "logs:CreateLogGroup"
    ]
  }

  statement {
    sid    = "AllowWritingLogs"
    effect = "Allow"

    resources = [
      "arn:aws:logs:*:*:log-group:/aws/lambda/*:*"
    ]

    actions = [
      "logs:CreateLogStream",
      "logs:PutLogEvents",
    ]
  }

  statement {
    sid    = "ReadWriteTable"
    effect = "Allow"
    actions = [
      "dynamodb:BatchGetItem",
      "dynamodb:GetItem",
      "dynamodb:Query",
      "dynamodb:Scan",
      "dynamodb:BatchWriteItem",
      "dynamodb:PutItem",
      "dynamodb:UpdateItem",
      "dynamodb:DescribeTable"
    ]
    resources = [
      var.tenant_table_arn
    ]
  }

  statement {
    sid    = "SendSMS"
    effect = "Allow"
    actions = [
      "sns:Publish"
    ]
    resources = [
      "*"
    ]
  }
}

resource "aws_iam_role_policy" "roster_duty_reminder_lambda_policy" {
  name   = "${local.stack_prefix}-lambda-rdr-policy"
  policy = data.aws_iam_policy_document.roster_duty_reminder_lambda_policy_doc.json
  role = aws_iam_role.roster_duty_reminder_lambda_role.name
}

resource "aws_scheduler_schedule" "roster_duty_reminder_scheduler" {
  name       = "${local.stack_prefix}-rdr-scheduler"
  group_name = aws_scheduler_schedule_group.schedule_group.name

  flexible_time_window {
    mode = "OFF"
  }

  schedule_expression = "cron(0 20 ? * SUN *)"
  schedule_expression_timezone = "Australia/Sydney"

  target {
    arn      = aws_lambda_function.roster_duty_reminder.arn
    role_arn = aws_iam_role.roster_duty_reminder_scheduler_role.arn
    input    = jsonencode({
      "Action": "MoveNextTenant"
    })
    retry_policy {
      maximum_retry_attempts   = 0
    }
  }
}


resource "aws_scheduler_schedule" "bin_duty_reminder_scheduler" {
  name       = "${local.stack_prefix}-rdr-bin-duty-scheduler"
  group_name = aws_scheduler_schedule_group.schedule_group.name

  flexible_time_window {
    mode = "OFF"
  }

  schedule_expression = "cron(0 20 ? * TUE *)"
  schedule_expression_timezone = "Australia/Sydney"

  target {
    arn      = aws_lambda_function.roster_duty_reminder.arn
    role_arn = aws_iam_role.roster_duty_reminder_scheduler_role.arn
    input    = jsonencode({
      "Action": "RemindBinDuty"
    })
    retry_policy {
      maximum_retry_attempts   = 0
    }
  }
}

resource "aws_iam_role" "roster_duty_reminder_scheduler_role" {
  name               = "${local.stack_prefix}-rdr-scheduler-role"
  assume_role_policy = jsonencode({
    Version   = "2012-10-17"
    Statement = [
      {
        Effect    = "Allow"
        Principal = {
          Service = ["scheduler.amazonaws.com"]
        }
        Action = "sts:AssumeRole"
      }
    ]
  })
}

resource "aws_iam_role_policy" "roster_duty_reminder_scheduler_role_policy" {
  name   = "${local.stack_prefix}-rdr-scheduler-role-policy"
  role   = aws_iam_role.roster_duty_reminder_scheduler_role.id
  policy = jsonencode({
    "Version" : "2012-10-17",
    "Statement" : [
      {
        "Sid" : "AllowEventBridgeToInvokeLambda",
        "Action" : [
          "lambda:InvokeFunction"
        ],
        "Effect" : "Allow",
        "Resource" : [
          aws_lambda_function.roster_duty_reminder.arn
        ]
      }
    ]
  })
}