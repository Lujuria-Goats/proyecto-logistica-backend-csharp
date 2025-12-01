# Apex Vision - Backend 🦅

![.NET](https://img.shields.io/badge/.NET-8-blueviolet) ![PostgreSQL](https://img.shields.io/badge/PostgreSQL-blue) ![RabbitMQ](https://img.shields.io/badge/RabbitMQ-orange) ![JWT](https://img.shields.io/badge/Auth-JWT-green) ![Cloudinary](https://img.shields.io/badge/Storage-Cloudinary-blue) ![Serilog](https://img.shields.io/badge/Logging-Serilog-red) ![xUnit](https://img.shields.io/badge/Testing-xUnit-success)

## 🚀 Descripción General

**Apex Vision** es una plataforma de logística que optimiza y gestiona rutas de entrega en tiempo real. Construida con **.NET 8**, utiliza una arquitectura escalable que integra inteligencia artificial para optimización de rutas, almacenamiento en la nube y procesamiento asíncrono.

### Características Clave

✨ **Autenticación Segura** - JWT con roles (Admin, Driver)  
📍 **Gestión de Rutas** - Dos tipos: Simple y Verificada (con evidencia fotográfica)  
☁️ **Almacenamiento en Nube** - Integración con Cloudinary  
🤖 **Optimización de Rutas** - Microservicio Java para algoritmos de optimización  
📨 **Mensajería Asíncrona** - RabbitMQ para procesamiento en background  
📊 **Logging Estructurado** - Serilog para trazabilidad completa  
🧪 **Testing Completo** - xUnit + Moq + FluentAssertions

---

## 🛠️ Tecnologías Utilizadas

| Componente | Tecnología | Versión |
|-----------|-----------|---------|
| Framework | .NET | 8.0 |
| API | ASP.NET Core | 8.0 |
| Base de Datos | PostgreSQL | 15 |
| ORM | Entity Framework Core | 8.0 |
| Autenticación | Identity + JWT | - |
| Almacenamiento | Cloudinary | Latest |
| Mensajería | RabbitMQ | 3.13 |
| Logging | Serilog | 4.3.0 |
| Testing | xUnit + Moq | 2.6.2 + 4.20.0 |

---

## 📋 Requisitos Previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Git](https://git-scm.com/)
- [PostgreSQL Client](https://www.postgresql.org/download/) (Opcional)

---

## 🚀 Guía de Instalación

### 1. Clonar Repositorio

```bash
git clone https://github.com/Lujuria-Goats/proyecto-logistica-backend-csharp.git
cd proyecto-logistica-backend-csharp
```

### 2. Iniciar Servicios con Docker

#### PostgreSQL
```bash
docker run -d \
  --name apex_db \
  -e POSTGRES_USER=root \
  -e POSTGRES_PASSWORD=xbI4PLOvlMwRwHn7SdZXHivFOZwc99 \
  -e POSTGRES_DB=ApexVisionDb \
  -p 5432:5432 \
  -v postgres_data:/var/lib/postgresql/data \
  --restart always \
  postgres:15-alpine
```

#### RabbitMQ
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

### 3. Configurar Variables de Entorno

Crea archivo `.env` en `ApexVision.Backend/`:

```dotenv
# Database
Jwt__Key=977df0f8dd4634f798c6440f74d29fb0ea5dd55eb3ea49a2c7097a98951dc515
Jwt__Issuer=ApexVision
Jwt__Audience=ApexVisionUsers
Jwt__ExpirationMinutes=60
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=ApexVisionDb;Username=root;Password=xbI4PLOvlMwRwHn7SdZXHivFOZwc99

# Cloudinary (Obtén credenciales en https://cloudinary.com)
Cloudinary__CloudName=your-cloud-name
Cloudinary__ApiKey=your-api-key
Cloudinary__ApiSecret=your-api-secret

# RabbitMQ
RABBITMQ_HOSTNAME=localhost
RABBITMQ_USERNAME=admin
RABBITMQ_PASSWORD=Kj9#mP2$qR5@vX8&
RABBITMQ_VIRTUALHOST=/
RABBITMQ_QUEUENAME=apex_orders

ASPNETCORE_ENVIRONMENT=Development
```

### 4. Restaurar Paquetes y Aplicar Migraciones

```bash
cd ApexVision.Backend

# Restaurar dependencias NuGet
dotnet restore

# Aplicar migraciones a la BD
dotnet ef database update
```

### 5. Ejecutar la Aplicación

```bash
dotnet run
```

La aplicación se abrirá automáticamente en: **http://localhost:5132/swagger/index.html**

---

## 📚 API Endpoints

### 🔐 Autenticación

```
POST /api/auth/register
Descripción: Registro público para nuevos choferes
Body:
{
  "fullName": "Juan García",
  "email": "juan@example.com",
  "phoneNumber": "+573001234567",
  "password": "SecurePass123!"
}
Response: { "message": "Usuario registrado exitosamente." }
```

```
POST /api/auth/login
Descripción: Iniciar sesión y obtener token JWT
Body:
{
  "email": "juan@example.com",
  "password": "SecurePass123!"
}
Response: { "token": "eyJhbGc..." }
```

### 📦 Pedidos

```
POST /api/orders
Descripción: Crear nuevo pedido (Solo Admin)
Headers: Authorization: Bearer {token}
Body:
{
  "address": "Cra. 30 #45-25, Medellín",
  "latitude": 6.2442,
  "longitude": -75.5898,
  "description": "Paquete urgente",
  "requiresEvidence": true
}
```

```
GET /api/orders
Descripción: Listar todos los pedidos (Solo Admin)
Headers: Authorization: Bearer {token}
Response: [ { id: 1, address: "...", status: "Pending", ... } ]
```

```
GET /api/orders/my-route
Descripción: Obtener mis pedidos asignados (Solo Driver)
Headers: Authorization: Bearer {token}
Response: [ { id: 1, address: "...", requiresEvidence: true } ]
```

```
PUT /api/orders/{id}/assign/{driverId}
Descripción: Asignar chofer a pedido (Solo Admin)
Headers: Authorization: Bearer {token}
Response: { "message": "Driver assigned successfully." }
```

```
POST /api/orders/{id}/complete
Descripción: Completar entrega con evidencia (Solo Driver)
Headers: Authorization: Bearer {token}
Content-Type: multipart/form-data
Body: file (image, optional si requiresEvidence=false)
Response: { "message": "Order completed successfully." }
```

```
POST /api/orders/my-route/optimize
Descripción: Optimizar mi ruta (Solo Driver)
Headers: Authorization: Bearer {token}
Response: { "message": "Route optimization initiated." }
```

### 👥 Usuarios

```
GET /api/users/drivers
Descripción: Listar choferes disponibles (Solo Admin)
Headers: Authorization: Bearer {token}
Response: [ { id: "1", fullName: "Juan", phoneNumber: "+57..." } ]
```

### 📁 Archivos

```
POST /api/files/upload
Descripción: Subir archivo a Cloudinary (Protegido)
Headers: Authorization: Bearer {token}
Body: multipart/form-data { file: image.jpg }
Response: { "success": true, "url": "https://...", "publicId": "..." }
```

```
DELETE /api/files/{publicId}
Descripción: Eliminar archivo de Cloudinary (Protegido)
Headers: Authorization: Bearer {token}
Response: 204 No Content
```

---

## 🧪 Testing

### Ejecutar Pruebas

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar con verbose output
dotnet test --verbosity detailed

# Ejecutar tests específicos
dotnet test --filter "AuthControllerTests"
```

### Cobertura de Tests

- ✅ **AuthController** - Registro, Login, validaciones
- ✅ **OrdersController** - CRUD de pedidos, asignación
- ✅ **Modelos** - Validaciones de dominio
- ⏳ **Servicios** - En progreso
- ⏳ **Integración** - En progreso

---

## 📊 Logging y Monitoreo

### Configuración de Serilog

Los logs se almacenan en:
- **Console** - Salida en tiempo real
- **Archivos** - `logs/apex-vision-{date}.txt`

### Niveles de Log

```
Information - Eventos normales (inicio de app, requests)
Warning - Comportamientos inesperados
Error - Errores que no detienen la app
Fatal - Errores críticos
```

### Ejemplo de Log

```json
{
  "Timestamp": "2025-12-01T17:30:45.1234567Z",
  "Level": "Information",
  "MessageTemplate": "Order {OrderId} created by {AdminId}",
  "Properties": {
    "OrderId": 42,
    "AdminId": "admin-user-id",
    "RequestId": "0HN1GFPJ7BGGE:00000001"
  }
}
```

---

## ⚙️ Estructura del Proyecto

```
ApexVision/
├── ApexVision.Backend/           # API principal
│   ├── Controllers/              # Endpoints
│   ├── Models/                   # Entidades de dominio
│   ├── DTOs/                     # Data Transfer Objects
│   ├── Services/                 # Lógica de negocio
│   ├── Middleware/               # Manejo centralizado de errores
│   ├── Filters/                  # Filtros de Swagger
│   ├── Migrations/               # Migraciones EF Core
│   ├── Data/                     # DbContext
│   └── Program.cs                # Configuración principal
│
├── ApexVision.Tests/             # Pruebas unitarias
│   ├── Controllers/              # Tests de controladores
│   ├── Services/                 # Tests de servicios
│   └── Models/                   # Tests de modelos
│
├── README.md                      # Este archivo
└── ApexVision.sln               # Solución Visual Studio
```

---

## 🔒 Seguridad

- ✅ **JWT con expiración** - Tokens expiran en 60 minutos
- ✅ **HTTPS Redirection** - Fuerza HTTPS en producción
- ✅ **CORS Configurado** - Permite acceso desde frontend/móvil
- ✅ **Roles y Permisos** - Admin y Driver separados
- ✅ **Contraseñas Hash** - Bcrypt via Identity
- ✅ **Rate Limiting** - En planificación

---

## 🚀 Deployment

### Variables de Entorno en Producción

```bash
# En hosting (AWS, Azure, etc.)
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=... # BD remota
Jwt__Key=... # Clave muy segura
Cloudinary__ApiSecret=... # Credentials seguros
RABBITMQ_HOSTNAME=rabbitmq.lujuria.crudzaso.com
```

### Docker Build

```bash
docker build -t apex-vision-api .
docker run -p 5132:8080 apex-vision-api
```

---

## 🤝 Contribución

1. Fork el repositorio
2. Crea rama: `git checkout -b feature/nueva-funcionalidad`
3. Commit cambios: `git commit -m "feat: agregar nueva funcionalidad"`
4. Push: `git push origin feature/nueva-funcionalidad`
5. Pull Request

---

## 📖 Documentación Adicional

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

**Última actualización:** Diciembre 1, 2025  
**Versión:** 1.0.0

