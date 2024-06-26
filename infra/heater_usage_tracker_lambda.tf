resource "aws_lambda_function" "heater_usage_tracker" {
  function_name = "${local.stack_prefix}-heater-usage-tracker"

  s3_bucket = aws_s3_bucket.lambda_bucket.id
  s3_key    = aws_s3_object.heater_usage_tracker_artifact.key

  runtime     = "dotnet8"
  handler     = "ToongabbieUtility.ElectricityBillReminder::ToongabbieUtility.ElectricityBillReminder.Function_Handler_Generated::Handler"
  timeout     = 30 # seconds
  memory_size = 512

  source_code_hash = data.archive_file.heater_usage_tracker_archive.output_base64sha256

  role = aws_iam_role.heater_usage_tracker_lambda_role.arn

  environment {
    variables = {
      "AppConfig__ApplicationId" = aws_appconfig_application.app_stack.id,
      "AppConfig__EnvironmentId" = aws_appconfig_environment.app_stack_env.environment_id,
      "AppConfig__ConfigProfileId" = aws_appconfig_configuration_profile.profile.configuration_profile_id
      "HeaterBillTopicArn": var.heater_bill_sns_topic_arn
    }
  }
}

data "archive_file" "heater_usage_tracker_archive" {
  type        = "zip"
  source_dir  = "${path.module}/../src/ToongabbieUtility.HeaterUsageTracker/bin/Release/net8.0/linux-x64/publish"
  output_path = "${path.module}/../dist/heater-usage-tracker.zip"

}

resource "aws_s3_object" "heater_usage_tracker_artifact" {
  bucket = aws_s3_bucket.lambda_bucket.id
  key    = "heater-usage-tracker.zip"
  source = data.archive_file.heater_usage_tracker_archive.output_path
  etag   = filemd5(data.archive_file.heater_usage_tracker_archive.output_path)
}

resource "aws_iam_role" "heater_usage_tracker_lambda_role" {
  name = "${local.stack_prefix}-hut-lambda-role"

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

data "aws_iam_policy_document" "heater_usage_tracker_lambda_policy_doc" {
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
      aws_dynamodb_table.daily_heater_usage_table.arn
    ]
  }
}

resource "aws_iam_role_policy" "heater_usage_tracker_lambda_policy" {
  name   = "${local.stack_prefix}-hut-lambda-policy"
  policy = data.aws_iam_policy_document.heater_usage_tracker_lambda_policy_doc.json
  role = aws_iam_role.heater_usage_tracker_lambda_role.name
}

resource "aws_scheduler_schedule" "heater_usage_tracker_scheduler" {
  name       = "${local.stack_prefix}-hut-scheduler"
  description = "Electricity bill reminder scheduler for sending electricity bill on every Monday"
  group_name = aws_scheduler_schedule_group.schedule_group.name

  flexible_time_window {
    mode = "OFF"
  }

  schedule_expression = "cron(30 09 ? * MON *)"
  schedule_expression_timezone = "Australia/Sydney"

  target {
    arn      = aws_lambda_function.heater_usage_tracker.arn
    role_arn = aws_iam_role.heater_usage_tracker_scheduler_role.arn
    input    = jsonencode({
    })
    retry_policy {
      maximum_retry_attempts   = 10
    }
  }
}

resource "aws_iam_role" "heater_usage_tracker_scheduler_role" {
  name               = "${local.stack_prefix}-hut-scheduler-role"
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

resource "aws_iam_role_policy" "heater_usage_tracker_scheduler_role_policy" {
  name   = "${local.stack_prefix}-hut-scheduler-role-policy"
  role   = aws_iam_role.heater_usage_tracker_scheduler_role.id
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
          aws_lambda_function.heater_usage_tracker.arn
        ]
      }
    ]
  })
}