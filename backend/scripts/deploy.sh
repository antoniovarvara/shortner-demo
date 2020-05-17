#!/usr/bin/env bash
set -e

template=infrastructure.yaml
output_template=output.yaml
region=eu-west-2
echo "Building lambda"
cd src/webapi
dotnet lambda package
cd ../../
timestamp=$(date +"%Y-%m-%d_%H-%M-%S")
lambda_package_name="webapi-${timestamp}.zip"

aws s3 cp ./src/webapi/bin/Release/netcoreapp3.1/webapi.zip s3://avarvara-dp-backend/shortner-demo/data-artifacts/${lambda_package_name} --region eu-west-2

aws cloudformation package \
        --template-file ./cloudformation/template/${template} \
        --s3-bucket "avarvara-dp-backend" \
        --s3-prefix "shortner-demo/data-artifacts" \
        --output-template-file ${output_template} \
        --region ${region}

echo "Deploying regional template"
aws cloudformation deploy --template-file ./${output_template} \
    --stack-name "shortner-demo" \
    --region ${region} \
    --capabilities "CAPABILITY_IAM" "CAPABILITY_NAMED_IAM" "CAPABILITY_AUTO_EXPAND" \
    --parameter-overrides LAMBDAPACAKGE=${lambda_package_name}

rm ./${output_template}