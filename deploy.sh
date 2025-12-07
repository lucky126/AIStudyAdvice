#!/bin/bash
echo "Building and deploying AI Study Advice..."
docker-compose down
docker-compose up -d --build
echo "Deployment complete! Access at http://localhost:8099"
