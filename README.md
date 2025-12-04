# Apex Vision - Backend 🦅

![.NET](https://img.shields.io/badge/.NET-8-blueviolet) ![PostgreSQL](https://img.shields.io/badge/PostgreSQL-blue) ![Docker](https://img.shields.io/badge/Docker-blue) ![RabbitMQ](https://img.shields.io/badge/RabbitMQ-orange) ![Azure AI](https://img.shields.io/badge/Azure_AI-Vision-blue) ![JWT](https://img.shields.io/badge/Auth-JWT-green)

## 🚀 Descripción General

**Apex Vision** es el backend para una plataforma de logística avanzada que optimiza y gestiona rutas de entrega. Construido con **.NET 8** y diseñado para ser desplegado con **Docker**, utiliza una arquitectura de microservicios escalable que integra inteligencia artificial para la validación de entregas, almacenamiento en la nube y procesamiento asíncrono de tareas.

### Características Clave

✨ **Autenticación Segura**: JWT con roles (Admin, Driver).  
📍 **Gestión de Pedidos**: Creación, asignación y seguimiento de estados.  
🛣️ **Rutas Verificadas vs. Simples**: Lógica para requerir o no evidencia fotográfica.  
🤖 **Validación con IA**: Integración con **Azure Computer Vision** para analizar las fotos de evidencia y asegurar que sean legítimas.  
☁️ **Almacenamiento en Nube**: Subida de imágenes a Cloudinary.  
🚚 **Optimización de Rutas**: Microservicio Java para algoritmos de optimización de rutas.  
📨 **Mensajería Asíncrona**: RabbitMQ para la comunicación con otros microservicios.  
📊 **Logging Estructurado**: Serilog para una trazabilidad completa de la aplicación.  
🚀 **Listo para Despliegue**: Configuración completa con Docker y Docker Compose para un despliegue sencillo en cualquier VPS.

---

## 🛠️ Tecnologías Utilizadas

| Componente | Tecnología |
|:-----------|:-----------|
| Framework | .NET 8 |
| Base de Datos | PostgreSQL 15 |
| Contenerización | Docker & Docker Compose |
| ORM | Entity Framework Core 8 |
| Autenticación | ASP.NET Core Identity + JWT |
| Almacenamiento | Cloudinary |
| Validación IA | Azure Computer Vision |
| Mensajería | RabbitMQ |
| Logging | Serilog |

---

## 🚀 Guía de Despliegue

Este proyecto está diseñado para ser ejecutado con Docker Compose, lo que simplifica enormemente la configuración del entorno de desarrollo y producción.

### 1. Preparación del Servidor (VPS)

Conéctate a tu VPS a través de Termius y ejecuta los siguientes comandos para instalar Docker, Docker Compose y Git.

```bash
# --- 1. Desinstalar versiones antiguas o conflictivas ---
sudo apt-get remove docker docker-engine docker.io containerd runc

# --- 2. Configurar el repositorio oficial de Docker ---
# Actualizar la lista de paquetes e instalar prerrequisitos
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg

# Añadir la clave GPG oficial de Docker
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

# Añadir el repositorio a las fuentes de APT
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# --- 3. Instalar Docker Engine y Docker Compose ---
# Actualizar la lista de paquetes de nuevo (ahora incluye Docker)
sudo apt-get update

# Instalar las últimas versiones
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# --- 4. Instalar Git ---
sudo apt-get install -y git

# --- 5. (Opcional pero recomendado) Añadir tu usuario al grupo de Docker ---
# Esto te permite ejecutar comandos de Docker sin tener que escribir 'sudo' cada vez.
# Reemplaza 'tu-usuario' con tu nombre de usuario real (ej: abraham).
sudo usermod -aG docker tu-usuario

# IMPORTANTE: Cierra la sesión de Termius y vuelve a conectarte
# para que los cambios de grupo de usuario tengan efecto.
```

### 2. Clonar el Repositorio

Crea una carpeta principal para el proyecto en tu VPS y clona los repositorios de .NET y Java dentro de ella.

```bash
mkdir apex-vision-deploy
cd apex-vision-deploy

# Clonar el backend de .NET
git clone https://github.com/Lujuria-Goats/proyecto-logistica-backend-csharp.git

# Clonar el microservicio de Java
git clone https://github.com/Lujuria-Goats/proyecto-logistica-backend-java.git
```

### 3. Crear Dockerfile para el Microservicio Java

Navega a la carpeta de tu microservicio Java y crea un archivo llamado `Dockerfile` con el siguiente contenido.

```bash
cd proyecto-logistica-backend-java
touch Dockerfile
nano Dockerfile
```

Pega el siguiente contenido en `Dockerfile`:

```dockerfile
# ==========================================
# STAGE 1: BUILDER
# ==========================================
# Use an official Maven image to build the application
FROM maven:3.9.5-eclipse-temurin-17 AS builder

# Set the working directory inside the container
WORKDIR /app

# 1. Copy only pom.xml first (Layer Caching Strategy)
# This allows Docker to cache dependencies if the POM hasn't changed,
# speeding up future builds significantly.
COPY pom.xml .

# 2. Download dependencies (Go offline mode)
RUN mvn dependency:go-offline

# 3. Copy the actual source code
COPY src ./src

# 4. Build and package the application
# We skip tests here (-DskipTests) to speed up the deployment build.
# Tests should be enforced in the CI pipeline (GitHub Actions) before this step.
RUN mvn clean package -DskipTests

# ==========================================
# STAGE 2: RUNTIME
# ==========================================
# Use a lightweight JRE image (Alpine Linux) to minimize the final image size
FROM eclipse-temurin:17-jre-alpine

# Set TimeZone to Bogota/Colombia (Critical for accurate logs)
ENV TZ=America/Bogota

# Set working directory for the runtime
WORKDIR /app

# Copy the generated JAR artifact from the 'builder' stage
# It automatically finds the .jar file and renames it to 'app.jar' for simplicity
COPY --from=builder /app/target/*.jar app.jar

# Expose the application port
EXPOSE 8080

# Command to start the application
ENTRYPOINT ["java", "-jar", "app.jar"]
```

Guarda y cierra el archivo (`Ctrl + X`, `Y`, `Enter`).

### 4. Configuración Final (El `docker-compose.yml` Central)

Crea un único archivo `docker-compose.yml` en la carpeta `apex-vision-deploy` para orquestar todos los servicios.

```bash
cd ~/apex-vision-deploy
touch docker-compose.yml
nano docker-compose.yml
```

Pega el siguiente contenido en `docker-compose.yml`:

```yaml
services:
  # API Backend (.NET)
  backend:
    build:
      context: ./proyecto-logistica-backend-csharp # Ruta a la carpeta .NET
      dockerfile: Dockerfile
    container_name: apex_backend
    restart: always
    ports:
      - "8080:8080" # Puerto público para Nginx
    depends_on:
      - db
      - apex-java # Asegura que Java inicie antes que .NET
    environment:
      # --- REVISA Y AJUSTA ESTOS VALORES ---
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=postgres;Username=TU_USUARIO_DB;Password=TU_PASSWORD_DB
      - Jwt__Key=UNA_CLAVE_SECRETA_SUPER_LARGA_Y_SEGURA_DE_MAS_DE_32_CARACTERES
      - Jwt__Issuer=ApexVisionAPI
      - Jwt__Audience=ApexVisionUsers
      - Jwt__ExpirationMinutes=1440
      - Cloudinary__CloudName=TU_CLOUDINARY_CLOUD_NAME
      - Cloudinary__ApiKey=TU_CLOUDINARY_API_KEY
      - Cloudinary__ApiSecret=TU_CLOUDINARY_API_SECRET
      - RabbitMQ__HostName=rabbitmq.lujuria.crudzaso.com
      - RabbitMQ__UserName=TU_USUARIO_RABBITMQ
      - RabbitMQ__Password=TU_PASSWORD_RABBITMQ
      - AzureVisionSettings__Endpoint=TU_AZURE_VISION_ENDPOINT
      - AzureVisionSettings__Key=TU_AZURE_VISION_KEY
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080

  # Microservicio de Optimización (Java)
  apex-java:
    build:
      context: ./proyecto-logistica-backend-java # Ruta a la carpeta Java
      dockerfile: Dockerfile
    container_name: apex_java
    restart: always
    ports:
      - "8081:8080" # Puerto interno para que .NET se comunique

  # Base de Datos (PostgreSQL)
  db:
    image: postgres:15-alpine
    container_name: apex_db
    restart: always
    environment:
      - POSTGRES_USER=TU_USUARIO_DB
      - POSTGRES_PASSWORD=TU_PASSWORD_DB
      - POSTGRES_DB=postgres
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
    driver: local
```

Guarda y cierra el archivo (`Ctrl + X`, `Y`, `Enter`).

### 5. Lanzamiento

Desde la carpeta `apex-vision-deploy`, ejecuta Docker Compose.

```bash
docker compose up --build -d
```

Verifica los logs para asegurarte de que no haya errores:

```bash
docker logs apex_backend
docker logs apex_java
```

Configura el Dominio (Reverse Proxy):
*   Crea un registro `A` para `service.lujuria.crudzaso.com` que apunte a la IP de tu VPS.
*   En tu VPS (usando Nginx Proxy Manager o similar), crea un nuevo "Proxy Host" que redirija el tráfico de `service.lujuria.crudzaso.com` a `http://localhost:8080`.
*   Activa el SSL en Nginx para tener `https://`.

---

## 📖 Endpoints de la API (Swagger)

Una vez que la aplicación esté corriendo, puedes acceder a la documentación interactiva de la API a través de Swagger en la siguiente URL:

**[http://localhost:8080/swagger/index.html](http://localhost:8080/swagger/index.html)**

Desde Swagger, podrás ver todos los endpoints, probarlos, y ver los modelos de datos que la API espera y devuelve.

### Credenciales de Administrador por Defecto

El sistema crea automáticamente un usuario administrador para que puedas empezar a probar:

-   **Email**: `admin@apexvision.com`
-   **Contraseña**: `Admin123!`

---

## ⚙️ Configuración de Variables de Entorno

Todas las configuraciones sensibles se gestionan a través de variables de entorno, definidas en el archivo `docker-compose.yml`.

| Variable | Descripción | Ejemplo |
|:---|:---|:---|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión a PostgreSQL. | `Host=db;...` |
| `Jwt__Key` | Clave secreta para firmar los tokens JWT. | `UNA_CLAVE_SUPER_SECRETA_Y_LARGA` |
| `Jwt__Issuer` | Emisor del token JWT. | `ApexVisionAPI` |
| `Jwt__Audience` | Audiencia del token JWT. | `ApexVisionUsers` |
| `Jwt__ExpirationMinutes` | Tiempo de expiración del token JWT en minutos. | `1440` |
| `Cloudinary__CloudName` | Nombre de tu nube en Cloudinary. | `my-cloud` |
| `Cloudinary__ApiKey` | API Key de Cloudinary. | `1234567890` |
| `Cloudinary__ApiSecret` | API Secret de Cloudinary. | `ABCDEFG-HIJKLMNOP` |
| `RabbitMQ__HostName` | Dominio o nombre del servicio de RabbitMQ. | `rabbitmq.lujuria.crudzaso.com` |
| `RabbitMQ__UserName` | Nombre de usuario para RabbitMQ. | `admin` |
| `RabbitMQ__Password` | Contraseña para RabbitMQ. | `Kj9#mP2$qR5@vX8&` |
| `AzureVisionSettings__Endpoint` | Endpoint de tu servicio Azure Computer Vision. | `https://my-vision.cognitiveservices.azure.com/` |
| `AzureVisionSettings__Key` | Clave de tu servicio Azure Computer Vision. | `0987654321-ABCDEF` |
| `ASPNETCORE_ENVIRONMENT` | Entorno de la aplicación ASP.NET Core. | `Production` |
| `ASPNETCORE_URLS` | URLs en las que la aplicación ASP.NET Core escuchará. | `http://+:8080` |

---

## 🧪 Cómo Probar que Todo Funciona

### Test 1: Swagger está disponible
```bash
curl -I http://localhost:8080/swagger
# Debe retornar HTTP 200, no 404
```

### Test 2: Base de datos inicializada
```bash
# Ver logs
docker-compose logs apex-backend | grep "Rol 'Admin' creado"
# Debe mostrar este mensaje
```

### Test 3: Usuario Admin funciona
```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@apexvision.com",
    "password": "Admin123!"
  }'
# Debe retornar un JWT token
```

### Test 4: Java API conectada
```bash
# Dentro del contenedor:
docker-compose exec apex-backend curl -I http://apex-java:8081/
# Debe conectar sin problemas
```

---

## ⚠️ Errores Comunes y Soluciones

| Error | Causa | Solución |
|-------|-------|----------|
| **404 en /swagger** | HTTPS activo o Development mode | Verificar que `app.UseHttpsRedirection()` está comentado en `Program.cs` |
| **"Too many redirects"** | HTTPS redirige infinitamente | Comentar `UseHttpsRedirection()` en `Program.cs` |
| **"Cannot connect to db"** | Host=localhost en Docker | Cambiar a `Host=db` (nombre del servicio) en `docker-compose.yml` |
| **"apex-java unreachable"** | Servicio tiene otro nombre | Cambiar el nombre del servicio Java en `docker-compose.yml` a `apex-java` y en `Program.cs` si está hardcodeado. |
| **"JWT Key invalid"** | Keys no coinciden | Verificar `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` en `docker-compose.yml` y `appsettings.json` |
| **"Port 8080 already in use"** | Otro proceso usa el puerto | `docker-compose down` o cambiar puerto en compose |
| **401 Unauthorized en endpoints protegidos** | Orden incorrecto de configuración de autenticación | Asegurarse de que `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)` se llama ANTES de `AddIdentity()` en `Program.cs`. |
| **`User.IsInRole("Admin")` devuelve FALSE** | Mapeo incorrecto de claims de rol | En `JwtService.cs`, usar `claims.Add(new Claim(ClaimTypes.Role, role));` en lugar de `claims.Add(new Claim("role", role));` |

---

## 📋 Ejemplos de JSON para Pruebas en Swagger

Colección completa de ejemplos listos para usar en las pruebas de la API de ApexVision a través de Swagger.

### 1. Autenticación (`/api/Auth`)

#### 1.1 Login de Administrador
**Endpoint:** `POST /api/Auth/login`
**Request:**
```json
{
  "email": "admin@apexvision.com",
  "password": "Admin123!"
}
```

#### 1.2 Registro de un Nuevo Conductor
**Endpoint:** `POST /api/Auth/register`
**Request:**
```json
{
  "fullName": "Carlos Rodríguez Pérez",
  "email": "carlos.driver@example.com",
  "password": "SecurePass123!",
  "phoneNumber": "+573109876543"
}
```

### 2. Gestión de Usuarios (`/api/Users`)

**⚠️ Requerimiento:** Debes estar autorizado como **Admin**

#### 2.1 Obtener Todos los Usuarios
**Endpoint:** `GET /api/users`

#### 2.2 Obtener Todos los Conductores
**Endpoint:** `GET /api/users/drivers`

#### 2.3 Obtener un Usuario por ID
**Endpoint:** `GET /api/users/{id}`
**Ejemplo:** `GET /api/users/2`

### 3. Gestión de Pedidos (`/api/Orders`)

**⚠️ Requerimiento:** Debes estar autorizado como **Admin** para crear y gestionar pedidos

#### 3.1 Crear un Pedido Simple (Sin Verificación)
**Endpoint:** `POST /api/Orders`
**Request:**
```json
{
  "address": "Centro Comercial Santafé, Medellín",
  "latitude": 6.198,
  "longitude": -75.578,
  "description": "Entrega de paquete electrónico - No requiere evidencia",
  "requiresEvidence": false
}
```

#### 3.2 Crear un Pedido Verificado (Requiere Evidencia con IA)
**Endpoint:** `POST /api/Orders`
**Request:**
```json
{
  "address": "Parque Lleras, Medellín",
  "latitude": 6.2098,
  "longitude": -75.567,
  "description": "Paquete frágil - Debe entregar en recepción. Requiere foto de comprobante.",
  "requiresEvidence": true
}
```

#### 3.3 Obtener Todos los Pedidos
**Endpoint:** `GET /api/Orders`

#### 3.4 Obtener un Pedido por ID
**Endpoint:** `GET /api/Orders/{id}`
**Ejemplo:** `GET /api/Orders/1`

#### 3.5 Asignar un Conductor a un Pedido
**Endpoint:** `PUT /api/Orders/{orderId}/assign/{driverId}`
**Ejemplo:** `PUT /api/Orders/1/assign/2`

#### 3.6 Obtener Pedidos Pendientes
**Endpoint:** `GET /api/Orders/pending`

### 4. Gestión de Archivos (`/api/Files`)

**⚠️ Requerimiento:** Debes estar autorizado (Admin o Driver)

#### 4.1 Subir una Foto de Entrega (Multipart/Form-Data)
**Endpoint:** `POST /api/Files/upload`
**Parámetros (Form-Data):**
- `orderId` (number): ID del pedido
- `file` (file): Imagen a subir (JPG, PNG, etc.)

### 5. Análisis de Imágenes con IA (`/api/ImageAnalysis`)

**⚠️ Requerimiento:** Debes estar autorizado como **Admin**

#### 5.1 Analizar una Imagen Subida
**Endpoint:** `POST /api/ImageAnalysis/analyze`
**Request:**
```json
{
  "orderId": 2,
  "imageUrl": "https://res.cloudinary.com/apexvision/image/upload/v1733236200/apex-vision/order_2_xyz123.jpg",
  "analysisType": "delivery-verification"
}
```

### 6. Optimización de Rutas (`/api/Orders`)

**⚠️ Requerimiento:** Debes estar autorizado como **Admin**

#### 6.1 Optimizar Ruta de un Conductor
**Endpoint:** `POST /api/Orders/optimize-route/{driverId}`
**Ejemplo:** `POST /api/Orders/optimize-route/2`

---

## 📚 Documentación Adicional

- [.NET 8 Docs](https://docs.microsoft.com/dotnet/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [JWT Authentication](https://tools.ietf.org/html/rfc7519)
- [Serilog](https://serilog.net/)
- [xUnit](https://xunit.net/)

---

## 📄 Licencia

MIT License - Ver LICENSE file para más detalles

---

## 📞 Contacto

**Equipo Apex Vision**

- Backend Lead: Abrahan
- Frontend: Juan
- Mobile: Jeims
- Data/Optimization: Java Microservice Team

---

**Última actualización:** Diciembre 4, 2025  
**Versión:** 1.0.0

