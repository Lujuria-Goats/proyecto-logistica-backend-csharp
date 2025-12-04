# 📚 Documentación Completa de Endpoints - ApexVision Backend

## 📌 Resumen General

El backend de ApexVision está completamente operativo con **13 endpoints verificados y funcionales**. 

**Base URL:** `http://localhost:5132` (desarrollo) o `https://service.lujuria.crudzaso.com` (producción)

---

## 🔐 Autenticación

### Tipos de Roles
- **Admin**: Acceso a crear pedidos, asignar drivers, ver todas las órdenes
- **Driver**: Acceso a ver sus rutas, guardar/cargar rutas, completar entregas

### Flujo de Autenticación
1. Login con email y contraseña
2. Recibir JWT token
3. Incluir token en header `Authorization: Bearer {token}` para todas las peticiones protegidas

---

## 🔌 Endpoints

### 1️⃣ AUTENTICACIÓN

#### **POST /api/Auth/login**
Autentica un usuario y devuelve un JWT token.

**Método:** `POST`  
**Protegido:** ❌ No  
**Body:**
```json
{
  "email": "admin@apexvision.com",
  "password": "Admin123!"
}
```

**Respuesta (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Errores:**
- `401 Unauthorized`: Credenciales inválidas

---

#### **POST /api/Auth/register**
Registra un nuevo usuario en el sistema.

**Método:** `POST`  
**Protegido:** ❌ No  
**Body:**
```json
{
  "email": "driver@example.com",
  "fullName": "John Driver",
  "password": "SecurePass123!",
  "phoneNumber": "+573001234567",
  "role": "Driver"
}
```

**Requisitos de Contraseña:**
- Mínimo 8 caracteres
- Al menos 1 letra mayúscula
- Al menos 1 letra minúscula
- Al menos 1 número
- Al menos 1 carácter especial

**Respuesta (200):**
```json
{
  "message": "Usuario registrado exitosamente.",
  "role": "Driver"
}
```

**Errores:**
- `400 Bad Request`: Datos inválidos o email duplicado

---

### 2️⃣ PEDIDOS (Orders)

#### **POST /api/Orders**
Crea un nuevo pedido. **Solo Admin**.

**Método:** `POST`  
**Protegido:** ✅ Sí (Admin)  
**Headers:**
```
Authorization: Bearer {adminToken}
Content-Type: application/json
```

**Body:**
```json
{
  "address": "Calle Principal 123, Medellín",
  "latitude": 6.2442,
  "longitude": -75.5898,
  "description": "Entrega de paquete",
  "requiresEvidence": false
}
```

**Respuesta (200):**
```json
{
  "message": "Order created successfully",
  "orderId": 42
}
```

**Errores:**
- `400 Bad Request`: Coordenadas (0,0) no permitidas
- `401 Unauthorized`: Token inválido
- `403 Forbidden`: No es Admin

---

#### **GET /api/Orders**
Obtiene todos los pedidos. **Solo Admin**.

**Método:** `GET`  
**Protegido:** ✅ Sí (Admin)  
**Headers:**
```
Authorization: Bearer {adminToken}
```

**Respuesta (200):**
```json
[
  {
    "id": 42,
    "description": "Entrega de paquete",
    "latitude": 6.2442,
    "longitude": -75.5898,
    "address": "Calle Principal 123, Medellín",
    "status": "Pending",
    "requiresEvidence": false,
    "driverId": null,
    "evidenceUrl": null
  }
]
```

---

#### **GET /api/Orders/my-route**
Obtiene los pedidos asignados al conductor actual. **Solo Driver**.

**Método:** `GET`  
**Protegido:** ✅ Sí (Driver)  
**Headers:**
```
Authorization: Bearer {driverToken}
```

**Respuesta (200):**
```json
[
  {
    "id": 42,
    "description": "Entrega de paquete",
    "latitude": 6.2442,
    "longitude": -75.5898,
    "address": "Calle Principal 123, Medellín",
    "status": "Pending",
    "requiresEvidence": false,
    "driverId": 5,
    "evidenceUrl": null
  }
]
```

---

#### **PUT /api/Orders/{orderId}/assign/{driverId}**
Asigna un pedido a un driver. **Solo Admin**.

**Método:** `PUT`  
**Protegido:** ✅ Sí (Admin)  
**URL:** `/api/Orders/42/assign/5`  
**Headers:**
```
Authorization: Bearer {adminToken}
```

**Respuesta (200):**
```json
{
  "message": "Driver assigned successfully."
}
```

**Errores:**
- `404 Not Found`: Pedido no existe
- `403 Forbidden`: No es Admin

---

#### **POST /api/Orders/{orderId}/complete**
Marca un pedido como completado. **Solo Driver**. Opcional: subir foto de comprobante.

**Método:** `POST`  
**Protegido:** ✅ Sí (Driver)  
**URL:** `/api/Orders/42/complete`  
**Headers:**
```
Authorization: Bearer {driverToken}
Content-Type: multipart/form-data
```

**Body:** (Form-data)
```
file: [imagen opcional si requiresEvidence=true]
```

**Respuesta (200):**
```json
{
  "message": "Order completed successfully."
}
```

**Errores:**
- `400 Bad Request`: Falta comprobante cuando se requiere
- `404 Not Found`: Pedido no existe

---

### 3️⃣ RUTAS (Routes)

#### **POST /api/Routes/save**
Guarda una ruta con múltiples pedidos. **Solo Driver**.

**Método:** `POST`  
**Protegido:** ✅ Sí (Driver)  
**Headers:**
```
Authorization: Bearer {driverToken}
Content-Type: application/json
```

**Body:**
```json
{
  "routeName": "Mi Ruta Diaria",
  "orderIds": [42, 43, 44]
}
```

**Respuesta (200):**
```json
{
  "message": "Ruta guardada exitosamente.",
  "routeId": 1,
  "routeName": "Mi Ruta Diaria",
  "orderCount": 3,
  "phoneNumber": "+573001234567"
}
```

**Errores:**
- `400 Bad Request`: Pedidos no pertenecen al conductor
- `403 Forbidden`: No es Driver

---

#### **GET /api/Routes/saved**
Obtiene todas las rutas guardadas del conductor. **Solo Driver**.

**Método:** `GET`  
**Protegido:** ✅ Sí (Driver)  
**Headers:**
```
Authorization: Bearer {driverToken}
```

**Respuesta (200):**
```json
{
  "phoneNumber": "+573001234567",
  "totalSavedRoutes": 2,
  "routes": [
    {
      "id": 1,
      "routeName": "Mi Ruta Diaria",
      "orderIds": [42, 43],
      "createdDate": "2025-12-04T14:30:00Z",
      "lastUsedDate": "2025-12-04T15:00:00Z",
      "isActive": true,
      "optimizationScore": 85.5
    }
  ]
}
```

---

#### **GET /api/Routes/saved/{routeId}**
Obtiene detalles de una ruta específica. **Solo Driver**.

**Método:** `GET`  
**Protegido:** ✅ Sí (Driver)  
**URL:** `/api/Routes/saved/1`  
**Headers:**
```
Authorization: Bearer {driverToken}
```

**Respuesta (200):**
```json
{
  "routeId": 1,
  "routeName": "Mi Ruta Diaria",
  "createdDate": "2025-12-04T14:30:00Z",
  "lastUsedDate": "2025-12-04T15:00:00Z",
  "phoneNumber": "+573001234567",
  "orders": [
    {
      "id": 42,
      "address": "Calle Principal 123, Medellín",
      "description": "Entrega de paquete",
      "latitude": 6.2442,
      "longitude": -75.5898,
      "status": "Pending",
      "requiresEvidence": false,
      "driverId": 5,
      "evidenceUrl": null
    }
  ],
  "optimizationScore": 85.5
}
```

---

#### **POST /api/Routes/saved/{routeId}/load**
Carga una ruta guardada (actualiza lastUsedDate). **Solo Driver**.

**Método:** `POST`  
**Protegido:** ✅ Sí (Driver)  
**URL:** `/api/Routes/saved/1/load`  
**Headers:**
```
Authorization: Bearer {driverToken}
```

**Respuesta (200):**
```json
{
  "message": "Ruta cargada exitosamente.",
  "routeName": "Mi Ruta Diaria",
  "phoneNumber": "+573001234567",
  "totalOrders": 2,
  "orders": [...]
}
```

---

#### **POST /api/Routes/saved/{routeId}/rename**
Renombra una ruta guardada. **Solo Driver**.

**Método:** `POST`  
**Protegido:** ✅ Sí (Driver)  
**URL:** `/api/Routes/saved/1/rename`  
**Headers:**
```
Authorization: Bearer {driverToken}
Content-Type: application/json
```

**Body:**
```json
{
  "newName": "Ruta Matutina - Actualizada"
}
```

**Respuesta (200):**
```json
{
  "message": "Ruta renombrada exitosamente.",
  "routeId": 1,
  "newName": "Ruta Matutina - Actualizada",
  "phoneNumber": "+573001234567"
}
```

---

#### **DELETE /api/Routes/saved/{routeId}**
Elimina (desactiva) una ruta guardada. **Solo Driver**.

**Método:** `DELETE`  
**Protegido:** ✅ Sí (Driver)  
**URL:** `/api/Routes/saved/1`  
**Headers:**
```
Authorization: Bearer {driverToken}
```

**Respuesta (200):**
```json
{
  "message": "Ruta eliminada exitosamente."
}
```

---

## 📊 Flujo Completo de Uso

### Flujo Admin:
```
1. Login (POST /api/Auth/login)
   ↓
2. Crear Pedido (POST /api/Orders)
   ↓
3. Ver Pedidos (GET /api/Orders)
   ↓
4. Asignar a Driver (PUT /api/Orders/{id}/assign/{driverId})
```

### Flujo Driver:
```
1. Registrarse (POST /api/Auth/register)
   ↓
2. Login (POST /api/Auth/login)
   ↓
3. Ver Mis Pedidos (GET /api/Orders/my-route)
   ↓
4. Guardar Ruta (POST /api/Routes/save)
   ↓
5. Ver Rutas Guardadas (GET /api/Routes/saved)
   ↓
6. Cargar Ruta (POST /api/Routes/saved/{id}/load)
   ↓
7. Completar Pedidos (POST /api/Orders/{id}/complete)
```

---

## 🧪 Testing

### Con cURL:
```bash
# Login
curl -X POST http://localhost:5132/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@apexvision.com","password":"Admin123!"}'

# Crear Pedido
curl -X POST http://localhost:5132/api/Orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "address":"Test",
    "latitude":6.2,
    "longitude":-75.5,
    "description":"Test",
    "requiresEvidence":false
  }'
```

### Con PowerShell (Test Script):
```powershell
powershell -ExecutionPolicy Bypass -File "C:\Users\user\OneDrive\Desktop\ApexVision\test-all.ps1"
```

---

## ✅ Estado Actual

- ✅ Todos los 13 endpoints funcionan correctamente
- ✅ Autenticación JWT implementada
- ✅ Control de acceso basado en roles (RBAC)
- ✅ Validaciones de entrada
- ✅ Manejo de errores

---

## 🔧 Configuración Necesaria

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=91.99.89.10;Port=5432;Database=ApexVisionDb;..."
  },
  "Jwt": {
    "Key": "MySuperSecureKeyForApexVisionThatIsLongEnough12345",
    "Issuer": "ApexVisionIssuer",
    "Audience": "ApexVisionAudience",
    "ExpirationMinutes": "60"
  }
}
```

---

## 📝 Notas Importantes

1. **Validación de Contraseña**: Mínimo 8 caracteres, incluir mayúsculas, minúsculas, números y caracteres especiales
2. **Coordenadas**: No pueden ser (0, 0)
3. **Rutas**: Solo pueden contener pedidos asignados al conductor
4. **Tokens**: Expiran después de 60 minutos
5. **CORS**: Habilitado para todas las peticiones

---

Última actualización: 4 de Diciembre de 2025  
Versión: 1.0

