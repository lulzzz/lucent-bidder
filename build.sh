#!/bin/bash
set -e
IMAGE_TAG=$(date +%s)
docker pull microsoft/dotnet
docker pull microsoft/dotnet:aspnetcore-runtime

echo 'Building portal'
docker build -t telefrek/lucent-portal:$IMAGE_TAG -f Dockerfile.portal .
docker tag telefrek/lucent-portal:$IMAGE_TAG telefrek/lucent-portal:${LUCENT_VERSION:-alpha}

echo 'Publishing portal'
docker push telefrek/lucent-portal:${LUCENT_VERSION:-alpha}

echo 'Building bidder'
docker build -t telefrek/lucent-bidder:$IMAGE_TAG -f Dockerfile.bidder .
docker tag telefrek/lucent-bidder:$IMAGE_TAG telefrek/lucent-bidder:${LUCENT_VERSION:-alpha}

echo 'Publishing bidder'
docker push telefrek/lucent-bidder:${LUCENT_VERSION:-alpha}

echo 'Building scoring'
docker build -t telefrek/lucent-scoring:$IMAGE_TAG -f Dockerfile.scoring .
docker tag telefrek/lucent-scoring:$IMAGE_TAG telefrek/lucent-scoring:${LUCENT_VERSION:-alpha}

echo 'Publishing scoring'
docker push telefrek/lucent-scoring:${LUCENT_VERSION:-alpha}
