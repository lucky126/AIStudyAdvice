#!/bin/bash
echo "Building and deploying AI Study Advice..."
set -e

echo "[1/3] Build image"
docker build -t study-app .

echo "[2/3] Stop old container if exists"
docker rm -f aistudyadvice-web-1 || true

echo "[3/3] Run container with host network and fixed env"
docker run -d --name aistudyadvice-web-1 --network host \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://0.0.0.0:80 \
  -e ConnectionStrings__Postgres="Host=127.0.0.1;Port=5432;Database=studydb;Username=postgres;Password=postgres" \
  -e LocalApiUrl=http://localhost:80 \
  -e Admin__Username=admin \
  -e Admin__Password="bUUlwqIfm+HMqeQfOqQC4HZe5fzD5/6jShabFzCuOG4=" \
  -e Coze__BaseUrl="https://api.coze.cn/v1" \
  $( [ -n "$COZE_API_KEY" ] && echo -e "-e Coze__ApiKey=\"$COZE_API_KEY\"" ) \
  $( [ -n "$COZE_WORKFLOW_ID_PARSE" ] && echo -e "-e Coze__WorkflowIdParse=\"$COZE_WORKFLOW_ID_PARSE\"" ) \
  $( [ -n "$COZE_WORKFLOW_ID_GENERATE" ] && echo -e "-e Coze__WorkflowIdGenerate=\"$COZE_WORKFLOW_ID_GENERATE\"" ) \
  $( [ -n "$COZE_WORKFLOW_ID_ADVICE" ] && echo -e "-e Coze__WorkflowIdAdvice=\"$COZE_WORKFLOW_ID_ADVICE\"" ) \
  study-app

echo "Deployment complete! Access at http://localhost:80"
echo "PostgreSQL connection: 127.0.0.1:5432 (username=postgres)"
