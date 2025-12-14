# Apex Vision - Backend 🦅

![.NET](https://img.shields.io/badge/.NET-8-blueviolet) ![PostgreSQL](https://img.shields.io/badge/PostgreSQL-blue) ![Docker](https://img.shields.io/badge/Docker-blue) ![RabbitMQ](https://img.shields.io/badge/RabbitMQ-orange) ![Azure AI](https://img.shields.io/badge/Azure_AI-Vision-blue) ![JWT](https://img.shields.io/badge/Auth-JWT-green)

## 🚀 Overview

**Apex Vision** is the backend for an advanced logistics platform that optimizes and manages delivery routes. Built with **.NET 8** and designed for **Docker** deployment, it uses a scalable microservices architecture integrating artificial intelligence for delivery validation, cloud storage, and asynchronous task processing.

### Key Features

✨ **Secure Authentication**: JWT with roles (Admin, Driver).  
📍 **Order Management**: Creation, assignment, and status tracking.  
🛣️ **Advanced Route Management**: 
  - **Admins**: Save route templates, edit/rename, and assign copies to drivers.
  - **Drivers**: Save and load their own frequent routes.
🤖 **Flexible AI Validation**: 
  - Integration with **Azure Computer Vision 4.0**.
  - Photographic evidence validation with configurable tags ("box", "package", etc.).
  - Automatic rejection of invalid photos.
☁️ **Cloud Storage**: Image upload to Cloudinary.  
🚚 **Route Optimization**: Java microservice for route optimization algorithms.  
📨 **Async Messaging**: RabbitMQ for communication with other microservices.  
📊 **Structured Logging**: Serilog for complete application traceability.  
🚀 **Deployment Ready**: Complete configuration with Docker and Docker Compose for easy deployment on any VPS.

---

## 🛠️ Tech Stack

| Component | Technology |
|:-----------|:-----------|
| Framework | .NET 8 |
| Database | PostgreSQL 15 |
| Containerization | Docker & Docker Compose |
| ORM | Entity Framework Core 8 |
| Authentication | ASP.NET Core Identity + JWT |
| Storage | Cloudinary |
| AI Validation | Azure Computer Vision |
| Messaging | RabbitMQ |
| Logging | Serilog |

---

## 🚀 Deployment Guide

This project is designed to run with Docker Compose, simplifying development and production environment setup.

### Production Update (No Cache)

If you need to deploy recent changes and ensure the latest code versions are used:

```bash
git pull origin dev && docker-compose build --no-cache && docker-compose up -d
```

### 1. Server Preparation (VPS)

Connect to your VPS via Termius/SSH and run the following commands to install Docker, Docker Compose, and Git.

```bash
# --- 1. Remove old versions ---
sudo apt-get remove docker docker-engine docker.io containerd runc

# --- 2. Configure Docker repository ---
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg

sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# --- 3. Install Docker Engine ---
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# --- 4. Install Git ---
sudo apt-get install -y git

# --- 5. Add user to Docker group ---
sudo usermod -aG docker your-username
# Note: Log out and log back in for changes to take effect.
```

### 2. Clone Repository

Create a main folder for the project and clone the repositories.

```bash
mkdir apex-vision-deploy
cd apex-vision-deploy

# Clone .NET Backend
git clone https://github.com/Lujuria-Goats/proyecto-logistica-backend-csharp.git

# Clone Java Microservice
git clone https://github.com/Lujuria-Goats/proyecto-logistica-backend-java.git
```

### 3. Create Dockerfile for Java Service

Navigate to the Java folder and create the `Dockerfile`.

```bash
cd proyecto-logistica-backend-java
touch Dockerfile
nano Dockerfile
```

Paste content (same as Spanish version):

```dockerfile
# (Use the Dockerfile content provided in the Spanish guide)
FROM maven:3.9.5-eclipse-temurin-17 AS builder
WORKDIR /app
COPY pom.xml .
RUN mvn dependency:go-offline
COPY src ./src
RUN mvn clean package -DskipTests

FROM eclipse-temurin:17-jre-alpine
ENV TZ=America/Bogota
WORKDIR /app
COPY --from=builder /app/target/*.jar app.jar
EXPOSE 8080
ENTRYPOINT ["java", "-jar", "app.jar"]
```

### 4. Configuration (Central `docker-compose.yml`)

Create `docker-compose.yml` in `apex-vision-deploy`.

```yaml
services:
  backend:
    build:
      context: ./proyecto-logistica-backend-csharp
      dockerfile: Dockerfile
    container_name: apex_backend
    restart: always
    ports:
      - "8080:8080"
    depends_on:
      - db
      - apex-java
    environment:
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=postgres;Username=DB_USER;Password=DB_PASS
      - Jwt__Key=YOUR_SUPER_SECRET_KEY_HERE
      # ... (User other settings as needed)

  apex-java:
    build:
      context: ./proyecto-logistica-backend-java
      dockerfile: Dockerfile
    container_name: apex_java
    restart: always
    ports:
      - "8081:8080"

  db:
    image: postgres:15-alpine
    container_name: apex_db
    restart: always
    environment:
      - POSTGRES_USER=DB_USER
      - POSTGRES_PASSWORD=DB_PASS
      - POSTGRES_DB=postgres
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
    driver: local
```

### 5. Launch

```bash
docker compose up --build -d
```

---

## 📖 API Endpoints (Swagger)

Access the interactive API documentation at:

**[http://localhost:8080/swagger/index.html](http://localhost:8080/swagger/index.html)**

### Default Admin Credentials

-   **Email**: `admin@apexvision.com`
-   **Password**: `Admin123!`

---

## ⚙️ Environment Variables

Sensitive configurations are managed via environment variables in `docker-compose.yml`.

| Variable | Description |
|:---|:---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string. |
| `Jwt__Key` | Secret key for JWT signing. |
| `AzureVisionSettings__Endpoint` | Azure Computer Vision endpoint. |
| `AzureVisionSettings__Key` | Azure Computer Vision key. |
# ... (Full list in Spanish version)

---

## ⚠️ Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| **401 Unauthorized** | Bad Auth config | Ensure `AddAuthentication` wraps `JwtBearer` before `AddIdentity`. Check `DefaultInboundClaimTypeMap`. |
| **`User.IsInRole` -> FALSE** | Incorrect claim mapping | Comment out `DefaultInboundClaimTypeMap.Clear()` in `Program.cs`. |

---

## 📄 License

MIT License - See LICENSE file for details.

---

**Last Updated:** December 14, 2025  
**Version:** 1.1.0
