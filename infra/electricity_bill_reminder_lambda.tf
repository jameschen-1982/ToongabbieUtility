resource "aws_lambda_function" "electricity_bill_reminder" {
  function_name = "${local.stack_prefix}-electricity-bill-reminder"

  s3_bucket = aws_s3_bucket.lambda_bucket.id
  s3_key    = aws_s3_object.electricity_bill_reminder_artifact.key

  runtime     = "provided.al2023"
  handler     = "not-used-in-custom-runtime"
  timeout     = 30 # seconds
  memory_size = 512

  source_code_hash = data.archive_file.electricity_bill_reminder_archive.output_base64sha256

  role = aws_iam_role.electricity_bill_reminder_lambda_role.arn
}

data "archive_file" "electricity_bill_reminder_archive" {
  type        = "zip"
  source_dir  = "${path.module}/../ElectricityBillReminder/lambda/ToongabbieUtility.ElectricityBillReminder/bin/Release/net8.0/linux-x64/publish"
  output_path = "${path.module}/../dist/electricity-bill-reminder.zip"

}

resource "aws_s3_object" "electricity_bill_reminder_artifact" {
  bucket = aws_s3_bucket.lambda_bucket.id
  key    = "electricity-bill-reminder.zip"
  source = data.archive_file.electricity_bill_reminder_archive.output_path
  etag   = filemd5(data.archive_file.electricity_bill_reminder_archive.output_path)
}

resource "aws_iam_role" "electricity_bill_reminder_lambda_role" {
  name = "${local.stack_prefix}-ebr-lambda-role"

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

data "aws_iam_policy_document" "electricity_bill_reminder_lambda_policy_doc" {
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
      aws_dynamodb_table.toongabbie-tenant-table.arn,
      aws_dynamodb_table.efergy-sensors-table.arn,
      "arn:aws:dynamodb:ap-southeast-2:773631419510:table/test5-app-DDBTable-1M2H022KQT2KL"
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

resource "aws_iam_role_policy" "electricity_bill_reminder_lambda_policy" {
  name   = "${local.stack_prefix}-ebr-lambda-policy"
  policy = data.aws_iam_policy_document.electricity_bill_reminder_lambda_policy_doc.json
  role = aws_iam_role.electricity_bill_reminder_lambda_role.name
}

resource "aws_scheduler_schedule" "electricity_bill_reminder_scheduler" {
  name       = "${local.stack_prefix}-ebr-scheduler"
  description = "Electricity bill reminder scheduler for sending electricity bill on every Monday"
  group_name = aws_scheduler_schedule_group.schedule_group.name

  flexible_time_window {
    mode = "OFF"
  }

  schedule_expression = "cron(15 10 ? * MON *)"
  schedule_expression_timezone = "Australia/Sydney"

  target {
    arn      = aws_lambda_function.electricity_bill_reminder.arn
    role_arn = aws_iam_role.electricity_bill_reminder_scheduler_role.arn
    input    = jsonencode({
    })
    retry_policy {
      maximum_retry_attempts   = 0
    }
  }
}

resource "aws_iam_role" "electricity_bill_reminder_scheduler_role" {
  name               = "${local.stack_prefix}-ebr-scheduler-role"
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

resource "aws_iam_role_policy" "electricity_bill_reminder_scheduler_role_policy" {
  name   = "${local.stack_prefix}-ebr-scheduler-role-policy"
  role   = aws_iam_role.electricity_bill_reminder_scheduler_role.id
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
          aws_lambda_function.electricity_bill_reminder.arn
        ]
      }
    ]
  })
}