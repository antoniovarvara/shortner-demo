#!/usr/bin/env bash

set -e
ng build
# aws s3 cp --recursive ./dist/frontend s3://avarvara-dp-backend/shortner-demo/frontend/ --region eu-west-2
aws s3 cp --recursive ./dist/frontend s3://shortnerdemo-dev-eu-west-2-s3-spa-frontend/ --region eu-west-2
