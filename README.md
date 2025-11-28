Restaura los paquetes de .NET y aplica las migraciones a la base de datos.

```bash
# Instalar dependencias
dotnet restore

# Navegar al proyecto principal
cd ApexVision.Backend

# Aplicar migraciones
dotnet ef database update
```

### 4. Ejecutar la Aplicación

Inicia el servidor de desarrollo.

```bash
dotnet run
```
# ApexVision - Backend
Una vez iniciado, la API estará disponible en `http://localhost:5000` (o el puerto configurado).
![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)
## 📚 Uso de la API

### Documentación

La documentación interactiva de la API, generada por Swagger, está disponible en la siguiente ruta una vez que la aplicación está en ejecución:
-   **Base de Datos**: Utiliza PostgreSQL con Entity Framework Core como ORM para una gestión de datos robusta.
`http://localhost:5000/swagger`

### Autenticación
Sigue estos pasos para configurar y ejecutar el proyecto en tu entorno local.
Para acceder a los endpoints protegidos, debes incluir un token JWT en el encabezado `Authorization` de tus solicitudes.

```http
Authorization: Bearer <tu_token_jwt>
```
### 1. Clonar el Repositorio
## 🏗️ Estructura del Proyecto

El proyecto sigue una arquitectura limpia y organizada para facilitar el mantenimiento y la escalabilidad.
```bash
git clone <URL_DEL_REPOSITORIO>
cd ApexVision-Backend
├── Controllers/     # Controladores de la API (puntos de entrada)
├── Data/            # Contexto de la base de datos y configuraciones de EF Core
├── DTOs/            # Objetos de Transferencia de Datos para la API
├── Filters/         # Filtros de acción personalizados
├── Migrations/      # Migraciones de la base de datos generadas por EF Core
├── Models/          # Entidades del dominio
├── Services/        # Lógica de negocio y servicios de aplicación
└── Workers/         # Workers en segundo plano (consumidores de RabbitMQ)
```

## 📄 Licencia

Este proyecto está distribuido bajo la Licencia MIT.
ConnectionStrings__DefaultConnection=Host=tu_host;Database=tu_db;Username=tu_usuario;Password=tu_contraseña

# JWT
Jwt__Key=tu_clave_secreta_muy_segura
Jwt__Issuer=ApexVision
Jwt__Audience=ApexVisionUsers
Jwt__ExpirationMinutes=60

# Cloudinary
Cloudinary__CloudName=tu_cloud_name
Cloudinary__ApiKey=tu_api_key
Cloudinary__ApiSecret=tu_api_secret

# RabbitMQ
RabbitMQ__Host=localhost
RabbitMQ__Username=guest
RabbitMQ__Password=guest
RabbitMQ__QueueName=command_queue
```

### 3. Instalar Dependencias y Aplicar Migraciones


## 📚 Documentación de la API

La documentación interactiva está disponible en:
```
http://localhost:5000/swagger
```

## 🔒 Autenticación

El sistema utiliza JWT para autenticación. Incluye el token en el header de tus peticiones:
```
Authorization: Bearer tu_token_jwt_aquí
```

## 📦 Servicios

### CloudinaryService
Manejo de carga y eliminación de imágenes.

### OptimizationService
Servicio para optimización de rutas de entrega.

### RabbitMQ Services
- **RabbitMqConnection**: Maneja la conexión con RabbitMQ
- **CommandConsumer**: Consume mensajes de la cola de comandos
- **CommandExecutor**: Ejecuta los comandos recibidos

## 📄 Estructura del Proyecto

```
ApexVision.Backend/
├── Controllers/     # Controladores de la API
├── Data/           # Contexto de base de datos
├── DTOs/           # Objetos de transferencia de datos
├── Filters/        # Filtros personalizados
├── Migrations/     # Migraciones de base de datos
├── Models/         # Modelos de dominio
├── Services/       # Servicios de negocio
└── Workers/        # Procesos en segundo plano (RabbitMQ)
```

## 📝 Licencia

Este proyecto está bajo la Licencia MIT.
