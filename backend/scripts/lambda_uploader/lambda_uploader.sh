#!/usr/bin/env bash
set -e

echo "Building lambda"
cd src/webapi
dotnet lambda package
cd ../../

aws s3 cp ./src/webapi/bin/Release/netcoreapp3.1/webapi.zip s3://avarvara-dp-backend/shortner-demo/data-artifacts/webapi.zip --region eu-west-2

aws lambda update-function-code --function-name "shortnerdemo_dev_lambda_api" --s3-bucket "avarvara-dp-backend" --s3-key "shortner-demo/data-artifacts/webapi.zip" --region eu-west-2 >/dev/null
aws lambda update-function-code --function-name "shortnerdemo_dev_lambda_api_redirect" --s3-bucket "avarvara-dp-backend" --s3-key "shortner-demo/data-artifacts/webapi.zip" --region eu-west-2 >/dev/null
echo "package uploaded"
