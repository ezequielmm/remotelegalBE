# This script will be executed by the LocalStack docker container and is used to execute AWS CLI commands for LocalStack
# All further AWS commands should be added here.
scriptPath=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
echo "Creating AWS S3 bucket ..."
awslocal s3 mb s3://${DOCUMENT_BUCKETNAME}

echo "Setting AWS S3 for tigger Lambda on object creation..."
awslocal s3api put-bucket-notification-configuration --bucket "${DOCUMENT_BUCKETNAME}" --notification-configuration file://${scriptPath}/lambda-notification.json
awslocal s3api get-bucket-notification-configuration --bucket "${DOCUMENT_BUCKETNAME}"

echo "Creating AWS Role.User..."
awslocal iam create-role --role-name "lambda-dotnet-ex" --assume-role-policy-document '{"Version": "2012-10-17","Statement": [{"Effect": "Allow","Action": "*","Resource": "*"}]}'

echo "Creating AWS Lambda..."
awslocal lambda create-function --function-name ${EXHIBIT_LAMBDA_NAME} --zip-file fileb://${scriptPath}/publish.zip --handler UploadExhibitLambda::UploadExhibitLambda.UploadExhibitFunction::UploadExhibit --runtime dotnetcore3.1 --role arn:aws:iam::000000000000:role/lambda-dotnet-ex --environment "Variables={BucketName=${DOCUMENT_BUCKETNAME},notificationarn=arn:aws:sns:us-east-1:000000000000:notifications-dev,MaxFileSize=51000000,PdfTronKey=PdfTronKey}" --timeout 60

echo "Creating AWS SNS Topic..."
awslocal sns create-topic --name notifications-dev

echo "Suscribing SnsCallback endpoint to SNS Topic..."
awslocal sns subscribe --topic-arn arn:aws:sns:us-east-1:000000000000:notifications-dev --protocol http --notification-endpoint ${LOCALSTACK_NOTIFICATION_ENDPOINT}

echo "Creating AWS Secretmanager..."
awslocal secretsmanager create-secret --name PdfTronKey --secret-string ${DOCUMENT_PDFTRON_DEMO}

echo "Done!"