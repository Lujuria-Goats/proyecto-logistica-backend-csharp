# 📡 API Endpoints - ApexVision Backend

> **Base URL Local:** `http://localhost:5132`  
> **Base URL Producción:** `https://service.lujuria.crudzaso.com`

---

## 📊 Resumen

| Controlador | Endpoints | Descripción |
|-------------|-----------|-------------|
| Auth | 4 | Registro y autenticación |
| Orders | 6 | Gestión de pedidos |
| Drivers | 7 | Gestión de conductores |
| Routes | 6 | Rutas guardadas |
| Files | 2 | Subida de archivos |
| ImageAnalysis | 3 | Análisis de imágenes con IA |
| Users | 1 | Usuarios (legacy) |

**Total: 30 endpoints**

---

## 🔐 AUTH - Autenticación

### POST `/api/Auth/register/admin`
Registrar un nuevo administrador (empresa).

**Body:**
```json
{
  "userName": "miempresa",
  "email": "admin@miempresa.com",
  "password": "Password123!",
  "fullName": "Juan Pérez",
  "phoneNumber": "+573001234567",
  "companyName": "Mi Empresa S.A.S",
  "companyNit": "900123456-7"
}
```

**Respuesta exitosa (200):**
```json
{
  "message": "Admin registrado exitosamente.",
  "userId": 15,
  "userName": "miempresa",
  "role": "Admin",
  "companyName": "Mi Empresa S.A.S"
}
```

---

### POST `/api/Auth/register/driver`
Registrar un nuevo conductor (desde app móvil).

**Body:**
```json
{
  "email": "conductor@email.com",
  "password": "Driver@123456",
  "fullName": "Carlos García",
  "phoneNumber": "+573009876543"
}
```

**Respuesta exitosa (200):**
```json
{
  "message": "Driver registrado exitosamente.",
  "userId": 25,
  "userName": "+573009876543",
  "role": "Driver"
}
```

---

### POST `/api/Auth/login`
Iniciar sesión (Admin o Driver).

**Body:**
```json
{
  "identifier": "admin@miempresa.com",
  "password": "Password123!"
}
```

> ⚠️ **Nota:** `identifier` puede ser: email, username, teléfono o NIT.

**Respuesta exitosa (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 15,
  "email": "admin@miempresa.com",
  "fullName": "Juan Pérez",
  "role": "Admin",
  "companyName": "Mi Empresa S.A.S"
}
```

---

### GET `/api/Auth/me`
Obtener información del usuario autenticado.

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
{
  "id": 15,
  "userName": "miempresa",
  "email": "admin@miempresa.com",
  "fullName": "Juan Pérez",
  "phoneNumber": "+573001234567",
  "role": "Admin",
  "companyName": "Mi Empresa S.A.S",
  "companyNit": "900123456-7"
}
```

---

## 📦 ORDERS - Pedidos

### POST `/api/Orders`
Crear un nuevo pedido. **Solo Admin.**

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "address": "Calle 50 #45-23, Medellín",
  "description": "Paquete frágil - electrodoméstico",
  "latitude": 6.2442,
  "longitude": -75.5812,
  "requiresEvidence": true
}
```

**Respuesta exitosa (200):**
```json
{
  "message": "Order created successfully",
  "orderId": 42
}
```

---

### GET `/api/Orders`
Listar todos los pedidos del admin. **Solo Admin.**

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
[
  {
    "id": 42,
    "description": "Paquete frágil",
    "latitude": 6.2442,
    "longitude": -75.5812,
    "address": "Calle 50 #45-23, Medellín",
    "status": "Pending",
    "requiresEvidence": true,
    "driverId": null,
    "evidenceUrl": null
  }
]
```

---

### PUT `/api/Orders/{id}/assign/{driverId}`
Asignar un pedido a un conductor. **Solo Admin.**

**Headers:** `Authorization: Bearer {token}`

**Ejemplo:** `PUT /api/Orders/42/assign/25`

**Respuesta exitosa (200):**
```json
{
  "message": "Driver assigned successfully."
}
```

---

### GET `/api/Orders/my-route`
Obtener pedidos asignados al conductor. **Solo Driver.**

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
{
  "driverId": 25,
  "driverName": "Carlos García",
  "orders": [
    {
      "id": 42,
      "address": "Calle 50 #45-23",
      "status": "Pending",
      "latitude": 6.2442,
      "longitude": -75.5812
    }
  ]
}
```

---

### POST `/api/Orders/{id}/complete`
Marcar pedido como completado con evidencia. **Solo Driver.**

**Headers:** `Authorization: Bearer {token}`

**Form Data:**
- `photo`: Archivo de imagen (evidencia de entrega)

**Respuesta exitosa (200):**
```json
{
  "message": "Order completed successfully.",
  "evidenceUrl": "https://res.cloudinary.com/.../evidence.jpg"
}
```

---

### POST `/api/Orders/my-route/optimize`
Optimizar la ruta del conductor usando Java backend. **Solo Driver.**

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
{
  "message": "Route optimized",
  "totalDistanceKm": 45.8,
  "optimizedOrder": [
    {"id": 42, "sequenceNumber": 0},
    {"id": 43, "sequenceNumber": 1}
  ]
}
```

---

## 👷 DRIVERS - Conductores

### GET `/api/Drivers`
Listar conductores vinculados al admin. **Solo Admin.**

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
{
  "totalDrivers": 3,
  "drivers": [
    {
      "id": 25,
      "userName": "carlos_driver",
      "fullName": "Carlos García",
      "email": "carlos@email.com",
      "phoneNumber": "+573009876543",
      "totalOrders": 15,
      "pendingOrders": 3,
      "linkedAt": "2025-12-01T10:30:00Z"
    }
  ]
}
```

---

### POST `/api/Drivers/link`
Vincular un conductor por número de teléfono. **Solo Admin.**

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "phoneNumber": "+573009876543"
}
```

**Respuesta exitosa (200):**
```json
{
  "message": "Conductor vinculado exitosamente.",
  "driver": {
    "id": 25,
    "fullName": "Carlos García",
    "phoneNumber": "+573009876543",
    "linkedAt": "2025-12-06T15:00:00Z"
  }
}
```

**Errores posibles:**
- `404`: Conductor no encontrado
- `409`: Conductor ya vinculado

---

### GET `/api/Drivers/{driverId}`
Obtener información de un conductor específico. **Solo Admin.**

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
{
  "id": 25,
  "fullName": "Carlos García",
  "phoneNumber": "+573009876543",
  "totalOrders": 15,
  "pendingOrders": 3
}
```

---

### DELETE `/api/Drivers/{driverId}`
Desvincular un conductor. **Solo Admin.**

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
{
  "message": "Conductor desvinculado exitosamente.",
  "unassignedOrders": 2
}
```

---

### GET `/api/Drivers/search?phone={phone}`
Buscar conductor por teléfono antes de vincular. **Solo Admin.**

**Headers:** `Authorization: Bearer {token}`

**Ejemplo:** `GET /api/Drivers/search?phone=3009876543`

**Respuesta exitosa (200):**
```json
{
  "found": true,
  "alreadyLinked": false,
  "driver": {
    "id": 25,
    "fullName": "Carlos García",
    "phoneNumber": "+573009876543",
    "email": "carlos@email.com"
  }
}
```

---

### GET `/api/Drivers/dashboard`
Obtener estadísticas del dashboard. **Solo Admin.**

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
{
  "totalDrivers": 5,
  "activeDrivers": 3,
  "totalOrders": 150,
  "pendingOrders": 25,
  "inTransitOrders": 10,
  "deliveredOrders": 115,
  "activeRoutes": 3,
  "deliveriesToday": 8,
  "ordersByStatus": [
    {"status": "Pending", "count": 25},
    {"status": "InTransit", "count": 10},
    {"status": "Delivered", "count": 115}
  ]
}
```

---

### GET `/api/Drivers/activities?limit={n}`
Obtener actividades recientes. **Solo Admin.**

**Headers:** `Authorization: Bearer {token}`

**Ejemplo:** `GET /api/Drivers/activities?limit=20`

**Respuesta exitosa (200):**
```json
{
  "totalActivities": 15,
  "activities": [
    {
      "id": 42,
      "type": "delivery",
      "description": "Pedido entregado en Calle 50",
      "driverId": 25,
      "driverName": "Carlos García",
      "timestamp": "2025-12-06T14:30:00Z"
    }
  ]
}
```

---

### GET `/api/Drivers/{driverId}/stats`
Estadísticas detalladas de un conductor. **Solo Admin.**

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
{
  "driverId": 25,
  "driverName": "Carlos García",
  "totalOrders": 50,
  "deliveredOrders": 45,
  "pendingOrders": 3,
  "inTransitOrders": 2,
  "savedRoutes": 5,
  "deliveryRate": 90.0
}
```

---

## 🗺️ ROUTES - Rutas Guardadas

### POST `/api/Routes/save`
Guardar la ruta actual del conductor. **Solo Driver.**

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "orderIds": [42, 43, 44],
  "routeName": "Ruta Centro Medellín"
}
```

**Respuesta exitosa (200):**
```json
{
  "message": "Ruta guardada exitosamente.",
  "routeId": 5,
  "routeName": "Ruta Centro Medellín",
  "orderCount": 3,
  "phoneNumber": "+573009876543"
}
```

---

### GET `/api/Routes/saved`
Listar rutas guardadas del conductor. **Solo Driver.**

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
{
  "phoneNumber": "+573009876543",
  "totalSavedRoutes": 3,
  "routes": [
    {
      "id": 5,
      "routeName": "Ruta Centro Medellín",
      "orderIds": [42, 43, 44],
      "createdDate": "2025-12-06T10:00:00Z",
      "isActive": true
    }
  ]
}
```

---

### GET `/api/Routes/saved/{routeId}`
Obtener detalle de una ruta guardada. **Solo Driver.**

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
{
  "routeId": 5,
  "routeName": "Ruta Centro Medellín",
  "createdDate": "2025-12-06T10:00:00Z",
  "orders": [
    {"id": 42, "address": "Calle 50 #45-23"},
    {"id": 43, "address": "Carrera 70 #30-15"}
  ]
}
```

---

### POST `/api/Routes/saved/{routeId}/load`
Cargar una ruta guardada. **Solo Driver.**

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
{
  "message": "Ruta cargada exitosamente.",
  "routeName": "Ruta Centro Medellín",
  "totalOrders": 3,
  "orders": [...]
}
```

---

### POST `/api/Routes/saved/{routeId}/rename`
Renombrar una ruta guardada. **Solo Driver.**

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "newName": "Ruta Centro - Actualizada"
}
```

**Respuesta exitosa (200):**
```json
{
  "message": "Ruta renombrada exitosamente.",
  "routeId": 5,
  "newName": "Ruta Centro - Actualizada"
}
```

---

### DELETE `/api/Routes/saved/{routeId}`
Eliminar una ruta guardada. **Solo Driver.**

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
{
  "message": "Ruta eliminada exitosamente."
}
```

---

## 📁 FILES - Archivos

### POST `/api/Files/upload`
Subir archivo a Cloudinary. **Autenticado.**

**Headers:** `Authorization: Bearer {token}`

**Form Data:**
- `file`: Archivo a subir

**Respuesta exitosa (200):**
```json
{
  "url": "https://res.cloudinary.com/.../image.jpg",
  "publicId": "apexvision/abc123"
}
```

---

### DELETE `/api/Files/{publicId}`
Eliminar archivo de Cloudinary. **Autenticado.**

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
{
  "message": "File deleted successfully."
}
```

---

## 🤖 IMAGE ANALYSIS - Análisis con IA (Azure Vision)

### POST `/api/ImageAnalysis/analyze`
Analizar imagen con Azure AI Vision. **Autenticado.**

**Headers:** `Authorization: Bearer {token}`

**Form Data:**
- `image`: Archivo de imagen

**Respuesta exitosa (200):**
```json
{
  "description": "Una caja de cartón en la puerta de una casa",
  "tags": ["box", "door", "house", "delivery"],
  "confidence": 0.95
}
```

---

### POST `/api/ImageAnalysis/validate-evidence`
Validar si la imagen es evidencia válida de entrega. **Autenticado.**

**Headers:** `Authorization: Bearer {token}`

**Form Data:**
- `image`: Archivo de imagen

**Respuesta exitosa (200):**
```json
{
  "isValid": true,
  "confidence": 0.92,
  "reason": "La imagen muestra un paquete entregado en una puerta."
}
```

---

### POST `/api/ImageAnalysis/extract-text`
Extraer texto de una imagen (OCR). **Autenticado.**

**Headers:** `Authorization: Bearer {token}`

**Form Data:**
- `image`: Archivo de imagen

**Respuesta exitosa (200):**
```json
{
  "text": "FRAGIL - NO APILAR\nRemitente: Empresa XYZ\nDestinatario: Juan Pérez",
  "lines": [
    "FRAGIL - NO APILAR",
    "Remitente: Empresa XYZ",
    "Destinatario: Juan Pérez"
  ]
}
```

---

## 👥 USERS - Usuarios (Legacy)

### GET `/api/Users/drivers`
Listar todos los drivers del sistema. **Solo Admin.**

**Headers:** `Authorization: Bearer {token}`

**Respuesta exitosa (200):**
```json
[
  {
    "id": 25,
    "fullName": "Carlos García",
    "email": "carlos@email.com"
  }
]
```

---

## 🔑 Códigos de Estado HTTP

| Código | Significado |
|--------|-------------|
| `200` | Éxito |
| `400` | Datos inválidos |
| `401` | No autenticado |
| `403` | Sin permisos |
| `404` | No encontrado |
| `409` | Conflicto (duplicado) |
| `500` | Error del servidor |

---

## 🧪 Probar con cURL

```bash
# Login
curl -X POST http://localhost:5132/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"identifier":"admin@apexvision.com","password":"Admin123!"}'

# Crear pedido (usar token del login)
curl -X POST http://localhost:5132/api/Orders \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"address":"Calle 50","latitude":6.24,"longitude":-75.58,"requiresEvidence":false}'
```

---

**Última actualización:** Diciembre 2025

