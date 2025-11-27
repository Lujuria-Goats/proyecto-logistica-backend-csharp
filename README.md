# ApexVision - Backend

Sistema de gestión logística con autenticación JWT, gestión de archivos y optimización de rutas.

## 🚀 Características Principales

- **Autenticación JWT** segura
- **Gestión de archivos** con Cloudinary
- **Optimización de rutas** para entregas
- Documentación con **Swagger UI**
- **PostgreSQL** como base de datos
- **Entity Framework Core** para ORM

## ⚙️ Configuración

### Variables de Entorno

Copia el archivo de ejemplo y configura tus variables:

```bash
cp .env.example .env
```

### Variables Requeridas

```
# Base de datos
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
```

## 🛠 Instalación

1. Clona el repositorio
2. Configura las variables de entorno
3. Instala las dependencias:
   ```bash
   dotnet restore
   ```
4. Ejecuta las migraciones:
   ```bash
   cd ApexVision.Backend
   dotnet ef database update
   ```
5. Inicia el servidor:
   ```bash
   dotnet run
   ```

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

## 📄 Estructura del Proyecto

```
ApexVision.Backend/
├── Controllers/     # Controladores de la API
├── Data/           # Contexto de base de datos
├── DTOs/           # Objetos de transferencia de datos
├── Filters/        # Filtros personalizados
├── Migrations/     # Migraciones de base de datos
├── Models/         # Modelos de dominio
└── Services/       # Servicios de negocio
```

## 📝 Licencia

Este proyecto está bajo la Licencia MIT.
