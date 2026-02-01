#!/bin/bash

# ============================================
# ApexVision Unified Deployment Script
# ============================================

set -e

echo "🚀 ApexVision Unified Deployment"
echo "================================="

# 1. Verificar estructura de directorios
if [ ! -d "proyecto-logistica-backend-java" ]; then
    echo "📦 Cloning Java Backend (dev branch)..."
    git clone -b dev https://github.com/Lujuria-Goats/proyecto-logistica-backend-java.git
else
    echo "📦 Pulling latest Java Backend (dev branch)..."
    cd proyecto-logistica-backend-java && git fetch && git checkout dev && git pull origin dev && cd ..
fi

if [ ! -d "proyecto-logistica-frontend-web" ]; then
    echo "📦 Cloning Frontend..."
    git clone https://github.com/Lujuria-Goats/proyecto-logistica-frontend-web.git
else
    echo "📦 Pulling latest Frontend..."
    cd proyecto-logistica-frontend-web && git pull origin main && cd ..
fi

# 2. Verificar/Crear Dockerfile para Java
# Nota: Si el repo ya tiene Dockerfile, esto no lo sobreescribirá
if [ ! -f "proyecto-logistica-backend-java/Dockerfile" ]; then
    echo "📝 Creating Dockerfile for Java Backend..."
    cat <<EOF > proyecto-logistica-backend-java/Dockerfile
# STAGE 1: BUILDER
FROM maven:3.9.5-eclipse-temurin-17 AS builder
WORKDIR /app
COPY pom.xml .
RUN mvn dependency:go-offline
COPY src ./src
RUN mvn clean package -DskipTests

# STAGE 2: RUNTIME
FROM eclipse-temurin:17-jre-alpine
ENV TZ=America/Bogota
WORKDIR /app
COPY --from=builder /app/target/*.jar app.jar
EXPOSE 8080
ENTRYPOINT ["java", "-jar", "app.jar"]
EOF
fi

# 3. VERIFICAR SI EXISTE nginx.conf ANTES DE CREAR DOCKERFILE
# Si existe nginx.conf en el repo, lo copiamos. Si no, no.
NGINX_CONF_CMD=""
if [ -f "proyecto-logistica-frontend-web/nginx.conf" ]; then
    echo "ℹ️ nginx.conf found, including in Dockerfile"
    NGINX_CONF_CMD="COPY nginx.conf /etc/nginx/conf.d/default.conf"
fi

# 4. Forzar Creación de Dockerfile para Frontend (Node 22)
echo "📝 Updating Dockerfile for Frontend (Node 22)..."
cat <<EOF > proyecto-logistica-frontend-web/Dockerfile
# Build Stage
FROM node:22-alpine as build-stage
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

# Production Stage
FROM nginx:stable-alpine as production-stage
COPY --from=build-stage /app/dist /usr/share/nginx/html
$NGINX_CONF_CMD
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
EOF

# 5. Verificar docker-compose.yml
if [ ! -f "docker-compose.yml" ]; then
    echo "❌ Error: docker-compose.yml not found!"
    echo "Please upload docker-compose.prod.yml -> docker-compose.yml via SFTP"
    exit 1
fi

# 6. Despliegue
echo "🛑 Stopping containers..."
# Limpieza agresiva de contenedores conflictivos
docker rm -f frontend-web csharp-backend java-backend || true
docker compose down

echo "🔨 Building containers..."
# Usamos --no-cache para asegurar que tome los cambios en Dockerfiles
docker compose build --no-cache

echo "🚀 Starting services..."
docker compose up -d

echo "✅ Deployment completed!"
docker ps
