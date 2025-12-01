# Apex Vision - Backend 🦅

![.NET](https://img.shields.io/badge/.NET-8-blueviolet) ![PostgreSQL](https://img.shields.io/badge/PostgreSQL-blue) ![RabbitMQ](https://img.shields.io/badge/RabbitMQ-orange) ![JWT](https://img.shields.io/badge/Auth-JWT-green) ![Cloudinary](https://img.shields.io/badge/Storage-Cloudinary-blue)

## 🚀 Descripción General

Este repositorio contiene el backend de **Apex Vision**, una aplicación de logística diseñada para optimizar y gestionar rutas de entrega. La API está construida con **.NET 8** y sigue una arquitectura limpia para garantizar escalabilidad y mantenibilidad.

### La Solución
**Apex Vision** ataca estos problemas con una plataforma digital centralizada:

-   **Para el Despachador (Admin):** Una plataforma web para crear y asignar órdenes de entrega, pudiendo diferenciar entre dos tipos de ruta:
    -   **Ruta Simple:** Entregas estándar.
    -   **Ruta Verificada:** Entregas de alto valor o sensibles que requieren una prueba fotográfica.
-   **Para el Conductor (Driver):** Una aplicación móvil que les muestra su ruta optimizada y los detalles de cada entrega. Al finalizar, pueden:
    -   Confirmar una entrega simple con un solo toque.
    -   Para rutas verificadas, capturar una foto como evidencia (`RequiresEvidence`) que se sube automáticamente.

El objetivo es simple: **entregas más rápidas, más seguras y con total transparencia.**

## ✨ Funcionalidades Principales

-   **Autenticación JWT:** Sistema de autenticación seguro basado en tokens.
-   **Gestión de Usuarios:** Registro público para choferes y roles (Admin, Driver).
-   **Gestión de Pedidos:** Creación, asignación y seguimiento de pedidos con coordenadas geográficas.
-   **Evidencia de Entrega:** Soporte para subir fotos de evidencia a **Cloudinary** en rutas que lo requieran.
-   **Optimización de Rutas:** Integración con un microservicio externo para optimizar las rutas de los choferes.
-   **Comunicación Asíncrona:** Uso de **RabbitMQ** para el procesamiento de tareas en segundo plano.

## 🛠️ Tecnologías Utilizadas

-   **Framework:** .NET 8 Web API
-   **Base de Datos:** PostgreSQL
-   **ORM:** Entity Framework Core 8
-   **Autenticación:** ASP.NET Core Identity con JWT
-   **Almacenamiento de Archivos:** Cloudinary
-   **Mensajería:** RabbitMQ
-   **Contenerización:** Docker

## 🏁 Cómo Empezar

Sigue estos pasos para configurar y ejecutar el proyecto en tu entorno local.

### 1. Prerrequisitos

-   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
-   [Docker](https://www.docker.com/products/docker-desktop) (para ejecutar PostgreSQL y RabbitMQ)
-   Un cliente de base de datos (como [DBeaver](https://dbeaver.io/))

### 2. Configuración del Entorno

#### a. Clonar el Repositorio

```bash
git clone https://github.com/Lujuria-Goats/proyecto-logistica-backend-csharp.git
cd proyecto-logistica-backend-csharp
```

#### b. Iniciar Servicios con Docker

Asegúrate de tener Docker en ejecución y ejecuta los siguientes comandos para iniciar los contenedores de la base de datos y el broker de mensajería:

**PostgreSQL:**
```bash
docker run -d \
  --name apex_db \
  -e POSTGRES_USER=root \
  -e POSTGRES_PASSWORD=xbI4PLOvlMwRwHn7SdZXHivFOZwc99 \
  -e POSTGRES_DB=postgres \
  -p 5432:5432 \
  -v postgres_data:/var/lib/postgresql/data \
  --restart always \
  postgres:15-alpine
```

**RabbitMQ:**
```bash
docker run -d \
  --name apex_rabbit \
  -p 5672:5672 \
  -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=admin \
  -e RABBITMQ_DEFAULT_PASS='Kj9#mP2$qR5@vX8&' \
  --restart always \
  rabbitmq:3-management
```

#### c. Configurar Variables de Entorno

1.  Navega a la carpeta del proyecto backend:
    ```bash
    cd ApexVision.Backend
    ```
2.  Crea un archivo llamado `.env` en esta carpeta.
3.  Copia el contenido de `.env.example` (si existe) o usa la siguiente plantilla y rellena tus credenciales (especialmente las de Cloudinary):

```dotenv
# Database Connection
DB_HOST=localhost
DB_PORT=5432
DB_DATABASE=postgres
DB_USERNAME=root
DB_PASSWORD=xbI4PLOvlMwRwHn7SdZXHivFOZwc99

# JWT
JWT_KEY=UNA_CLAVE_SECRETA_MUY_LARGA_Y_SEGURA_AQUI
JWT_ISSUER=ApexVision
JWT_AUDIENCE=ApexVisionUsers
JWT_EXPIRATION_MINUTES=60

# Cloudinary
CLOUDINARY_CLOUD_NAME=TU_CLOUD_NAME
CLOUDINARY_API_KEY=TU_API_KEY
CLOUDINARY_API_SECRET=TU_API_SECRET

# RabbitMQ
RABBITMQ_HOSTNAME=localhost
RABBITMQ_USERNAME=admin
RABBITMQ_PASSWORD='Kj9#mP2$qR5@vX8&'
RABBITMQ_VIRTUALHOST=/
RABBITMQ_QUEUENAME=pc_commands
```

### 3. Ejecutar la Aplicación

#### a. Instalar Dependencias y Aplicar Migraciones

Desde la carpeta `ApexVision.Backend`, ejecuta los siguientes comandos:

```bash
# Restaurar paquetes de .NET
dotnet restore

# Aplicar las migraciones a la base de datos
dotnet ef database update
```

#### b. Iniciar el Servidor

```bash
dotnet run
```

Una vez iniciado, la API estará disponible en `http://localhost:5000` (o el puerto HTTPS configurado).

## 📚 Documentación de la API

La documentación interactiva de la API (Swagger) está disponible en la siguiente ruta una vez que la aplicación está en ejecución:

**`http://localhost:5000/swagger`**

Desde aquí puedes explorar y probar todos los endpoints disponibles.

## 📄 Licencia

Este proyecto está distribuido bajo la Licencia MIT.
