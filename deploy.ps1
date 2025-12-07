Write-Host "Building and deploying AI Study Advice..."
docker-compose down
docker-compose up -d --build
Write-Host "Deployment complete! Access at http://localhost:8099"
