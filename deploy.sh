#!/bin/bash

# ============================================
# ApexVision Backend - Production Deployment Script
# ============================================

set -e  # Exit on any error

echo "🚀 ApexVision Backend - Production Deployment"
echo "=============================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Check if docker-compose.yml exists
if [ ! -f "docker-compose.yml" ]; then
    echo -e "${RED}❌ Error: docker-compose.yml not found!${NC}"
    echo ""
    echo "Please upload docker-compose.prod.yml from your local machine and rename it to docker-compose.yml"
    echo ""
    echo "Using SFTP:"
    echo "  sftp root@46.224.92.193"
    echo "  cd /root/deploy/proyecto-logistica-backend-csharp"
    echo "  put docker-compose.prod.yml docker-compose.yml"
    exit 1
fi

echo -e "${GREEN}✓${NC} docker-compose.yml found"

# Pull latest code
echo ""
echo "📥 Pulling latest code from GitHub..."
git pull origin main || git pull origin master

echo -e "${GREEN}✓${NC} Code updated"

# Stop existing containers
echo ""
echo "🛑 Stopping existing containers..."
docker compose down

echo -e "${GREEN}✓${NC} Containers stopped"

# Build without cache
echo ""
echo "🔨 Building Docker image (this may take a few minutes)..."
docker compose build --no-cache

echo -e "${GREEN}✓${NC} Build completed"

# Start containers
echo ""
echo "🚀 Starting containers..."
docker compose up -d

echo -e "${GREEN}✓${NC} Containers started"

# Wait for container to be ready
echo ""
echo "⏳ Waiting for backend to be ready..."
sleep 5

# Check if container is running
if docker ps | grep -q apex_backend; then
    echo -e "${GREEN}✓${NC} Backend container is running"
else
    echo -e "${RED}❌ Backend container is not running!${NC}"
    echo ""
    echo "Showing logs:"
    docker logs apex_backend --tail 50
    exit 1
fi

# Show logs
echo ""
echo "📋 Recent logs:"
echo "=============================================="
docker logs apex_backend --tail 20

echo ""
echo "=============================================="
echo -e "${GREEN}✅ Deployment completed successfully!${NC}"
echo ""
echo "Next steps:"
echo "  1. Check full logs: docker logs apex_backend -f"
echo "  2. Test API: curl http://localhost:8080/swagger"
echo "  3. Configure Nginx Proxy Manager for service.apexvision.crudzaso.com"
echo ""
