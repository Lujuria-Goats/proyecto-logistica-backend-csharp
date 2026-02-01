#!/bin/bash

# ============================================
# ApexVision Unified Deployment Script
# ============================================

set -e

echo "🚀 ApexVision Unified Deployment"
echo "================================="

# 1. Verificar estructura de directorios
if [ ! -d "proyecto-logistica-backend-java" ]; then
    echo "📦 Cloning Java Backend..."
    git clone https://github.com/Lujuria-Goats/proyecto-logistica-backend-java.git
else
    echo "📦 Pulling latest Java Backend..."
    cd proyecto-logistica-backend-java && git pull origin main && cd ..
fi

if [ ! -d "proyecto-logistica-frontend-web" ]; then
    echo "📦 Cloning Frontend..."
    git clone https://github.com/Lujuria-Goats/proyecto-logistica-frontend-web.git
else
    echo "📦 Pulling latest Frontend..."
    cd proyecto-logistica-frontend-web && git pull origin main && cd ..
fi

# 2. Verificar docker-compose.yml
if [ ! -f "docker-compose.yml" ]; then
    echo "❌ Error: docker-compose.yml not found!"
    echo "Please upload docker-compose.prod.yml -> docker-compose.yml via SFTP"
    exit 1
fi

# 3. Despliegue
echo "🛑 Stopping containers..."
docker compose down

echo "🔨 Building containers..."
docker compose build --no-cache

echo "🚀 Starting services..."
docker compose up -d

echo "✅ Deployment completed!"
docker ps
