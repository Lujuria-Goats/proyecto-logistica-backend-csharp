# 📋 Documentación API - ApexVision Backend

## 🔗 URLs Base

| Ambiente | URL |
|----------|-----|
| **Producción** | `https://service.lujuria.crudzaso.com` |
| **Swagger** | `https://service.lujuria.crudzaso.com/swagger` |
| **Local** | `http://localhost:5132` |

---

## 🔐 AUTENTICACIÓN

### Registro de Admin (Dashboard Web)
```http
POST /api/Auth/register/admin
Content-Type: application/json

{
  "userName": "empresa_abc",
  "fullName": "Juan Carlos Pérez",
  "email": "admin@empresa.com",
  "password": "MiPassword123!",
  "phoneNumber": "3001234567",
  "companyNit": "900123456-1",
  "companyName": "Transportes ABC S.A.S"
}
```

**Respuesta exitosa (200):**
```json
{
  "message": "Admin registrado exitosamente.",
  "userId": 1,
  "userName": "empresa_abc",
  "role": "Admin",
  "companyName": "Transportes ABC S.A.S"
}
```

---

### Registro de Driver (App Móvil)
```http
POST /api/Auth/register/driver
Content-Type: application/json

{
  "userName": "conductor_juan",
  "fullName": "Juan Pérez González",
  "email": "juan@email.com",
  "password": "Driver123!",
  "phoneNumber": "3009876543"
}
```

**Respuesta exitosa (200):**
```json
{
  "message": "Driver registrado exitosamente.",
  "userId": 5,
  "userName": "conductor_juan",
  "role": "Driver"
}
```

---

### Login (Ambos roles)
```http
POST /api/Auth/login
Content-Type: application/json

{
  "identifier": "admin@empresa.com",
  "password": "MiPassword123!"
}
```

> **📌 El campo `identifier` acepta:**
> - Email: `admin@empresa.com`
> - Username: `empresa_abc`
> - Teléfono: `3001234567`
> - NIT/Documento: `900123456-1`

**Respuesta exitosa (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 1,
  "userName": "empresa_abc",
  "fullName": "Juan Carlos Pérez",
  "email": "admin@empresa.com",
  "phoneNumber": "3001234567",
  "role": "Admin",
  "companyName": "Transportes ABC S.A.S",
  "companyNit": "900123456-1"
}
```

---

### Obtener Usuario Actual
```http
GET /api/Auth/me
Authorization: Bearer {token}
```

**Respuesta (200):**
```json
{
  "userId": 1,
  "userName": "empresa_abc",
  "fullName": "Juan Carlos Pérez",
  "email": "admin@empresa.com",
  "phoneNumber": "3001234567",
  "role": "Admin",
  "companyName": "Transportes ABC S.A.S",
  "companyNit": "900123456-1"
}
```

---

## 👥 GESTIÓN DE CONDUCTORES (Solo Admin)

### Flujo de Vinculación

```
┌─────────────────────────────────────────────────────────────┐
│  1. Driver se registra en APP MÓVIL                         │
│     POST /api/Auth/register/driver                          │
│                                                             │
│  2. Driver le comparte su TELÉFONO al Admin                 │
│                                                             │
│  3. Admin busca al driver por teléfono (opcional)           │
│     GET /api/Drivers/search?phone=3009876543                │
│                                                             │
│  4. Admin vincula al driver                                 │
│     POST /api/Drivers/link                                  │
│     { "phoneNumber": "3009876543" }                         │
│                                                             │
│  5. El driver puede estar vinculado a MÚLTIPLES empresas    │
└─────────────────────────────────────────────────────────────┘
```

---

### Buscar Conductor por Teléfono
```http
GET /api/Drivers/search?phone=3009876543
Authorization: Bearer {token_admin}
```

**Respuesta si existe (200):**
```json
{
  "found": true,
  "alreadyLinked": false,
  "driver": {
    "id": 5,
    "fullName": "Juan Pérez González",
    "phoneNumber": "3009876543",
    "email": "juan@email.com"
  }
}
```

**Respuesta si no existe (404):**
```json
{
  "message": "No se encontró un conductor con ese número. Debe registrarse primero en la app móvil."
}
```

---

### Vincular Conductor
```http
POST /api/Drivers/link
Authorization: Bearer {token_admin}
Content-Type: application/json

{
  "phoneNumber": "3009876543"
}
```

**Respuesta exitosa (200):**
```json
{
  "message": "Conductor vinculado exitosamente.",
  "driver": {
    "id": 5,
    "userName": "conductor_juan",
    "fullName": "Juan Pérez González",
    "email": "juan@email.com",
    "phoneNumber": "3009876543",
    "totalOrders": 0,
    "pendingOrders": 0,
    "linkedAt": "2025-12-05T16:30:00Z"
  }
}
```

**Errores posibles:**
- `404` - Conductor no encontrado
- `400` - Ya está vinculado a tu cuenta

---

### Listar Mis Conductores
```http
GET /api/Drivers
Authorization: Bearer {token_admin}
```

**Respuesta (200):**
```json
{
  "totalDrivers": 3,
  "drivers": [
    {
      "id": 5,
      "userName": "conductor_juan",
      "fullName": "Juan Pérez González",
      "email": "juan@email.com",
      "phoneNumber": "3009876543",
      "totalOrders": 15,
      "pendingOrders": 2,
      "linkedAt": "2025-12-05T16:30:00Z"
    },
    {
      "id": 8,
      "userName": "conductor_maria",
      "fullName": "María López",
      "email": "maria@email.com",
      "phoneNumber": "3005551234",
      "totalOrders": 8,
      "pendingOrders": 0,
      "linkedAt": "2025-12-04T10:00:00Z"
    }
  ]
}
```

---

### Obtener Conductor Específico
```http
GET /api/Drivers/{driverId}
Authorization: Bearer {token_admin}
```

**Respuesta (200):**
```json
{
  "id": 5,
  "userName": "conductor_juan",
  "fullName": "Juan Pérez González",
  "email": "juan@email.com",
  "phoneNumber": "3009876543",
  "totalOrders": 15,
  "pendingOrders": 2,
  "linkedAt": "2025-12-05T16:30:00Z"
}
```

---

### Desvincular Conductor
```http
DELETE /api/Drivers/{driverId}
Authorization: Bearer {token_admin}
```

**Respuesta exitosa (200):**
```json
{
  "message": "Conductor desvinculado exitosamente."
}
```

**Error si tiene pedidos pendientes (400):**
```json
{
  "message": "No se puede desvincular un conductor con pedidos pendientes."
}
```

---

## 📦 GESTIÓN DE PEDIDOS

### Crear Pedido (Admin)
```http
POST /api/Orders
Authorization: Bearer {token_admin}
Content-Type: application/json

{
  "address": "Calle 50 #30-20, Medellín",
  "latitude": 6.2442,
  "longitude": -75.5812,
  "description": "Paquete frágil - electrodomésticos",
  "requiresEvidence": true
}
```

**Respuesta (200):**
```json
{
  "message": "Order created successfully",
  "orderId": 1
}
```

---

### Asignar Conductor a Pedido (Admin)
```http
PUT /api/Orders/{orderId}/assign/{driverId}
Authorization: Bearer {token_admin}
```

**Respuesta (200):**
```json
{
  "message": "Driver assigned successfully."
}
```

---

### Listar Pedidos (Admin)
```http
GET /api/Orders
Authorization: Bearer {token_admin}
```

**Respuesta (200):**
```json
[
  {
    "id": 1,
    "address": "Calle 50 #30-20, Medellín",
    "latitude": 6.2442,
    "longitude": -75.5812,
    "description": "Paquete frágil",
    "status": "Pending",
    "requiresEvidence": true,
    "driverId": 5,
    "evidenceUrl": null
  }
]
```

---

### Obtener Mi Ruta (Driver)
```http
GET /api/Orders/my-route
Authorization: Bearer {token_driver}
```

**Respuesta (200):**
```json
{
  "driverId": 5,
  "driverName": "Juan Pérez",
  "orders": [
    {
      "id": 1,
      "address": "Calle 50 #30-20",
      "latitude": 6.2442,
      "longitude": -75.5812,
      "description": "Paquete frágil",
      "status": "Pending",
      "requiresEvidence": true
    }
  ]
}
```

---

### Completar Entrega (Driver)
```http
POST /api/Orders/{orderId}/complete
Authorization: Bearer {token_driver}
Content-Type: multipart/form-data

file: [imagen de evidencia]
```

**Respuesta (200):**
```json
{
  "message": "Order completed successfully",
  "evidenceUrl": "https://cloudinary.com/..."
}
```

---

## 🗺️ RUTAS GUARDADAS (Driver)

### Guardar Ruta
```http
POST /api/Routes/save
Authorization: Bearer {token_driver}
Content-Type: application/json

{
  "routeName": "Ruta Centro Medellín",
  "orderIds": [1, 3, 5, 7]
}
```

---

### Listar Rutas Guardadas
```http
GET /api/Routes/saved
Authorization: Bearer {token_driver}
```

---

### Cargar Ruta Guardada
```http
POST /api/Routes/saved/{routeId}/load
Authorization: Bearer {token_driver}
```

---

## 📊 CÓDIGOS DE RESPUESTA

| Código | Significado |
|--------|-------------|
| 200 | Éxito |
| 400 | Error de validación / Solicitud incorrecta |
| 401 | No autorizado (token inválido o expirado) |
| 403 | Prohibido (sin permisos para este recurso) |
| 404 | Recurso no encontrado |
| 500 | Error interno del servidor |

---

## 🔑 HEADERS REQUERIDOS

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

---

## 📱 RESUMEN POR ROL

### Admin (Dashboard Web)
- Registrar empresa con NIT
- Login con email/username/teléfono/NIT
- Vincular conductores por teléfono
- Crear y asignar pedidos
- Ver todos los pedidos y conductores

### Driver (App Móvil)
- Registrar con teléfono
- Login con email/username/teléfono
- Ver ruta asignada
- Completar entregas con foto
- Guardar rutas favoritas

---

**Última actualización:** Diciembre 5, 2025

