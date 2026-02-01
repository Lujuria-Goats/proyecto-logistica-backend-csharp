# 🚀 Despliegue Unificado en Producción

Este documento detalla los pasos para desplegar toda la plataforma (C# Backend, Java Backend, Frontend, Databases) en el servidor VPS.

## 📋 Prerrequisitos en el Servidor

El servidor debe tener instalado:
- Docker y Docker Compose
- Git

## 📂 Directorio de Despliegue

La estructura final en el servidor (`/root/deploy`) será:

```text
/root/deploy
├── docker-compose.yml              # Archivo MAESTRO con credenciales (subido por SFTP)
├── deploy.sh                       # Script de automatización
├── Dockerfile                      # Dockerfile de C#
├── ApexVision.Backend/             # Código de C#
├── proyecto-logistica-backend-java/ # Clonado automáticamente
└── proyecto-logistica-frontend-web/ # Clonado automáticamente
```

## 🔐 Archivo Maestro: `docker-compose.prod.yml`

Este es el archivo más importante. Contiene la definición de **TODOS** los servicios y **TODAS** las credenciales de producción.

**Ubicación local**: `/proyecto-logistica-backend-csharp/docker-compose.prod.yml`

> [!CAUTION]
> Este archivo contiene secretos reales. NUNCA lo compartas ni lo subas a GitHub.

---

## 🛠️ Pasos de Despliegue

### 1. Preparar el Servidor

```bash
# Conectar al VPS
ssh root@46.224.92.193

# Crear carpeta de despliegue (si no existe)
mkdir -p /root/deploy
cd /root/deploy

# Clonar el repo principal (C#) si es la primera vez
# Nota: Si ya tienes los archivos ahí, asegúrate de hacer git pull
git clone https://github.com/Lujuria-Goats/proyecto-logistica-backend-csharp.git .
# O si ya existe:
git pull origin dev
```

### 2. Subir el Archivo Maestro (SFTP)

Desde tu máquina local, sube el archivo de producción y renómbralo:

```bash
# Sube docker-compose.prod.yml y guárdalo como docker-compose.yml en el servidor
scp docker-compose.prod.yml root@46.224.92.193:/root/deploy/docker-compose.yml
```

### 3. Ejecutar el Despliegue

En el servidor:

```bash
cd /root/deploy

# Dar permisos al script
chmod +x deploy.sh

# Ejecutar despliegue
./deploy.sh
```

El script se encargará de:
1. Clonar/Actualizar el repositorio de Java
2. Clonar/Actualizar el repositorio de Frontend
3. Construir todas las imágenes
4. Levantar todos los contenedores

---

## 🌐 Servicios Desplegados

| Servicio | Puerto Externo | URL Interna Docker |
|----------|----------------|--------------------|
| **C# Backend** | `8080` | `http://csharp-backend:8080` |
| **Java Backend** | `8081` | `http://java-backend:8080` |
| **Frontend** | `8084` | `http://frontend-web:80` |
| **RabbitMQ** | `5672` / `15672` | `rabbitmq` |
| **PostgreSQL** | `5432` | `postgres-db` |
| **MySQL** | `3306` | `mysql-db` |

## 🔄 Actualizaciones Futuras

### Para actualizar C# Backend:
```bash
cd /root/deploy
git pull origin dev
./deploy.sh
```

### Para actualizar Java o Frontend:
El script `./deploy.sh` automáticamente hace pull de los repositorios de Java y Frontend cada vez que se ejecuta.
