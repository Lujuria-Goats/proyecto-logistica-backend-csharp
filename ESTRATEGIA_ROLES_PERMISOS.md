# 🔐 Estrategia de Roles y Permisos - ApexVision

**Fecha**: 4 de Diciembre, 2025  
**Versión**: 1.0.0

---

## 📊 Tabla Comparativa de Roles

| Característica | Admin | Driver |
|---|---|---|
| **Puede crear pedidos** | ✅ | ❌ |
| **Puede ver todos los pedidos** | ✅ | ❌ |
| **Puede ver solo su ruta** | ❌ | ✅ |
| **Puede asignar conductores** | ✅ | ❌ |
| **Puede optimizar rutas** | ✅ | ❌ |
| **Puede completar pedidos** | ❌ | ✅ |
| **Puede subir fotos** | ✅ | ✅ |
| **Puede analizar imágenes (IA)** | ✅ | ❌ |
| **Puede obtener conductores** | ✅ | ❌ |
| **Acceso a Dashboard** | ✅ | ❌ |
| **Acceso a App Móvil** | Opcional | ✅ |

---

## 🔄 Cómo Asignar Roles al Registrarse

### Flujo 1: Registro desde Dashboard Web (Admin)

```
1. Admin quiere crear otro admin o necesita nuevo administrador
2. Va a formulario de registro en el dashboard
3. Completa los datos:
   - Nombre completo
   - Email
   - Contraseña
   - Teléfono
   - Rol: SELECCIONA "Admin"
4. Presiona "Registrar"
5. Backend recibe:
   {
     "fullName": "Juan Pérez",
     "email": "juan@apexvision.com",
     "password": "AdminPass123!",
     "phoneNumber": "+573001234567",
     "role": "Admin"
   }
6. Se crea usuario con rol "Admin"
7. Puede hacer login y acceder a todas las funciones administrativas
```

### Flujo 2: Registro desde App Móvil (Driver)

```
1. Conductor descarga la app
2. Presiona "Registrarse"
3. Completa:
   - Nombre completo
   - Email
   - Contraseña
   - Teléfono
4. Backend automáticamente asigna rol "Driver" (no pregunta)
5. O envía explícitamente:
   {
     "fullName": "Carlos Rodríguez",
     "email": "carlos@apexvision.com",
     "password": "DriverPass123!",
     "phoneNumber": "+573001234567",
     "role": "Driver"
   }
6. Se crea usuario con rol "Driver"
7. Puede hacer login y ver su ruta asignada
```

### Opción: Dejar que el Usuario Elija el Rol

Si quieres que el usuario pueda elegir:

```json
POST /api/Auth/register
{
  "fullName": "Usuario",
  "email": "usuario@apexvision.com",
  "password": "Pass123!",
  "phoneNumber": "+573001234567",
  "role": "Driver"  ← El usuario elige: "Admin" o "Driver"
}
```

---

## 🔐 Niveles de Acceso

### Nivel 1: Usuario Anónimo (No autenticado)
**Endpoints disponibles**:
- `POST /api/Auth/login` - Login
- `POST /api/Auth/register` - Registro

**Lo que puede hacer**:
- Crear cuenta
- Autenticarse

---

### Nivel 2: Driver (Conductor)
**Después de hacer login con rol Driver**

**Endpoints disponibles**:
- `GET /api/Orders/my-route` - Ver su ruta
- `POST /api/Orders/{orderId}/complete` - Completar pedido
- `POST /api/Files/upload` - Subir foto de evidencia
- `DELETE /api/Files/{publicId}` - Eliminar foto

**Lo que puede hacer**:
- ✅ Ver solo SU ruta asignada
- ✅ Completar pedidos asignados a él
- ✅ Subir fotos de entrega
- ✅ Optimizar su propia ruta (después que admin lo solicite)
- ❌ NO puede ver todos los pedidos
- ❌ NO puede crear pedidos
- ❌ NO puede asignar conductores
- ❌ NO puede acceder a funciones administrativas

**Ejemplo de acceso**:
```bash
curl -X GET http://localhost:5132/api/Orders/my-route \
  -H "Authorization: Bearer eyJ...token_driver..."
  
→ Retorna SOLO los pedidos asignados a este conductor
```

---

### Nivel 3: Admin (Administrador)
**Después de hacer login con rol Admin**

**Endpoints disponibles**:
- `POST /api/Orders` - Crear pedido
- `GET /api/Orders` - Ver TODOS los pedidos
- `PUT /api/Orders/{orderId}/assign/{driverId}` - Asignar conductor
- `POST /api/Orders/optimize-route/{driverId}` - Optimizar ruta
- `GET /api/users/drivers` - Ver lista de conductores
- `POST /api/Files/upload` - Subir fotos
- `DELETE /api/Files/{publicId}` - Eliminar fotos
- `POST /api/ImageAnalysis/analyze` - Analizar imágenes
- `POST /api/ImageAnalysis/validate-evidence` - Validar evidencia
- `POST /api/ImageAnalysis/extract-text` - Extraer texto

**Lo que puede hacer**:
- ✅ Crear pedidos
- ✅ Ver TODOS los pedidos del sistema
- ✅ Asignar conductores a pedidos
- ✅ Solicitar optimización de rutas (Java)
- ✅ Ver lista completa de conductores
- ✅ Analizar imágenes con IA
- ✅ Validar fotos de evidencia
- ✅ Gestionar todo el sistema
- ❌ NO puede cambiar contraseña de otros usuarios
- ❌ NO puede eliminar usuarios

**Ejemplo de acceso**:
```bash
curl -X GET http://localhost:5132/api/Orders \
  -H "Authorization: Bearer eyJ...token_admin..."
  
→ Retorna TODOS los pedidos del sistema
```

---

## 🛡️ Validación de Roles en JWT

El token JWT contiene el rol del usuario:

**Token Admin**:
```json
{
  "nameid": "1",
  "email": "admin@apexvision.com",
  "role": "Admin",
  "nbf": 1764798946,
  "exp": 1764802546,
  "iat": 1764798946,
  "iss": "ApexVisionAPI",
  "aud": "ApexVisionUsers"
}
```

**Token Driver**:
```json
{
  "nameid": "2",
  "email": "carlos@apexvision.com",
  "role": "Driver",
  "nbf": 1764798946,
  "exp": 1764802546,
  "iat": 1764798946,
  "iss": "ApexVisionAPI",
  "aud": "ApexVisionUsers"
}
```

Cuando hace una request con el token, el servidor:
1. Valida que el token sea válido
2. Extrae el `role` del JWT
3. Verifica si tiene permiso para ese endpoint
4. Retorna 403 si no tiene rol suficiente

---

## ⚠️ Escenarios de Error

### Scenario 1: Driver intenta crear pedido
```bash
curl -X POST http://localhost:5132/api/Orders \
  -H "Authorization: Bearer eyJ...token_driver..." \
  -H "Content-Type: application/json" \
  -d '{"address":"...","latitude":6.2,"longitude":-75.5,...}'

RESPUESTA: 403 Forbidden
Mensaje: "User is not authorized to access this resource"
```

### Scenario 2: Admin intenta ver ruta de otro conductor
```bash
# Admin puede hacer esto:
curl -X GET http://localhost:5132/api/Orders \
  -H "Authorization: Bearer eyJ...token_admin..."

# Pero NO puede hacer esto (no existe endpoint):
curl -X GET http://localhost:5132/api/Orders/driver/2 \
  -H "Authorization: Bearer eyJ...token_admin..."
```

### Scenario 3: Usuario sin token intenta acceder
```bash
curl -X GET http://localhost:5132/api/Orders

RESPUESTA: 401 Unauthorized
Mensaje: "Authorization header missing"
```

---

## 🔄 Cambio de Rol (Futuro)

**NOTA**: Actualmente, los roles se asignan al registro y NO se pueden cambiar.

Si necesitas cambiar el rol de un usuario en el futuro:
1. Crear endpoint `PUT /api/Users/{userId}/role` (solo Admin)
2. Validar que solo Admin pueda cambiar roles
3. Actualizar rol en BD
4. El cambio toma efecto en el siguiente login

---

## 📋 Checklist para Implementación en Frontend

### Para el Dashboard Web (Admin)
- [ ] Agregar campo "Rol" en formulario de registro
- [ ] Permitir seleccionar "Admin" o "Driver"
- [ ] Enviar `"role": "Admin"` en la request
- [ ] Mostrar todos los endpoints disponibles

### Para la App Móvil (Driver)
- [ ] Formulario de registro SIN campo de rol
- [ ] Enviar `"role": "Driver"` automáticamente
- [ ] O enviar sin el campo (por defecto es Driver)
- [ ] Mostrar SOLO `/my-route` y endpoints de driver

---

## 🧪 Pruebas de Roles

### Test 1: Registrar Admin
```bash
curl -X POST http://localhost:5132/api/Auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Admin Test",
    "email": "admin-test@apexvision.com",
    "password": "AdminTest123!",
    "phoneNumber": "+573001234567",
    "role": "Admin"
  }'

ESPERADO:
{
  "message": "Usuario registrado exitosamente.",
  "role": "Admin"
}
```

### Test 2: Registrar Driver
```bash
curl -X POST http://localhost:5132/api/Auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Driver Test",
    "email": "driver-test@apexvision.com",
    "password": "DriverTest123!",
    "phoneNumber": "+573001234567",
    "role": "Driver"
  }'

ESPERADO:
{
  "message": "Usuario registrado exitosamente.",
  "role": "Driver"
}
```

### Test 3: Registrar sin especificar rol
```bash
curl -X POST http://localhost:5132/api/Auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Default User",
    "email": "default@apexvision.com",
    "password": "Default123!",
    "phoneNumber": "+573001234567"
  }'

ESPERADO:
{
  "message": "Usuario registrado exitosamente.",
  "role": "Driver"  ← Por defecto
}
```

---

---

## 🗺️ ¿Puede un Driver Guardar Varias Rutas?

### Respuesta Corta
**SÍ**, un Driver puede:
1. **Tener múltiples rutas asignadas simultáneamente** (por defecto)
2. **Guardar rutas con su teléfono como identificador**
3. **Cargar rutas guardadas cuando lo necesite**

---

## 📱 NUEVA FUNCIONALIDAD: Guardar Rutas por Teléfono

### Cómo Funciona

**El Driver ahora puede GUARDAR sus rutas** usando estos endpoints:

#### **1. Guardar la ruta actual**
```bash
POST /api/Routes/save
Authorization: Bearer [token_driver]
Content-Type: application/json

{
  "routeName": "Ruta Mañana - Centro",
  "orderIds": [1, 2, 3, 4, 5]
}

RESPUESTA:
{
  "message": "Ruta guardada exitosamente.",
  "routeId": 1,
  "routeName": "Ruta Mañana - Centro",
  "orderCount": 5,
  "phoneNumber": "+573001234567"  ← Se identifica por teléfono
}
```

#### **2. Ver todas sus rutas guardadas**
```bash
GET /api/Routes/saved
Authorization: Bearer [token_driver]

RESPUESTA:
{
  "phoneNumber": "+573001234567",
  "totalSavedRoutes": 3,
  "routes": [
    {
      "id": 1,
      "routeName": "Ruta Mañana - Centro",
      "orderIds": [1, 2, 3, 4, 5],
      "createdDate": "2025-12-04T10:30:00Z",
      "lastUsedDate": "2025-12-04T15:45:00Z",
      "isActive": true,
      "optimizationScore": 95
    },
    {
      "id": 2,
      "routeName": "Ruta Tarde - Periferia",
      "orderIds": [6, 7, 8],
      "createdDate": "2025-12-03T09:00:00Z",
      "lastUsedDate": null,
      "isActive": true,
      "optimizationScore": 87
    }
  ]
}
```

#### **3. Obtener detalles de una ruta guardada**
```bash
GET /api/Routes/saved/1
Authorization: Bearer [token_driver]

RESPUESTA:
{
  "routeId": 1,
  "routeName": "Ruta Mañana - Centro",
  "createdDate": "2025-12-04T10:30:00Z",
  "lastUsedDate": "2025-12-04T15:45:00Z",
  "phoneNumber": "+573001234567",
  "orders": [
    {"id": 1, "address": "Dirección 1", "status": "Completed"},
    {"id": 2, "address": "Dirección 2", "status": "Pending"},
    ...
  ],
  "optimizationScore": 95
}
```

#### **4. Cargar una ruta guardada**
```bash
POST /api/Routes/saved/1/load
Authorization: Bearer [token_driver]

RESPUESTA:
{
  "message": "Ruta cargada exitosamente.",
  "routeName": "Ruta Mañana - Centro",
  "phoneNumber": "+573001234567",
  "totalOrders": 5,
  "orders": [
    {"id": 1, "address": "Dirección 1", "status": "Pending"},
    {"id": 2, "address": "Dirección 2", "status": "Pending"},
    ...
  ]
}
```

#### **5. Renombrar una ruta guardada**
```bash
POST /api/Routes/saved/1/rename
Authorization: Bearer [token_driver]
Content-Type: application/json

{
  "newName": "Ruta Centro - Optimizada"
}

RESPUESTA:
{
  "message": "Ruta renombrada exitosamente.",
  "routeId": 1,
  "newName": "Ruta Centro - Optimizada",
  "phoneNumber": "+573001234567"
}
```

#### **6. Eliminar una ruta guardada**
```bash
DELETE /api/Routes/saved/1
Authorization: Bearer [token_driver]

RESPUESTA:
{
  "message": "Ruta eliminada exitosamente."
}
```

---

### Escenario Práctico Completo

```
Lunes
├─ Admin crea 5 pedidos
├─ Admin asigna a Driver #2 (teléfono: +573001234567)
├─ Driver ve su ruta: [Pedido 1, 2, 3, 4, 5]
├─ Driver GUARDA esta ruta:
│  POST /api/Routes/save
│  {
│    "routeName": "Ruta Lunes - Centro",
│    "orderIds": [1, 2, 3, 4, 5]
│  }
└─ Se guarda con ID: 1, linkado al teléfono +573001234567

Martes
├─ Admin crea 3 nuevos pedidos
├─ Admin asigna a Driver #2
├─ Driver quiere recordar la ruta del lunes:
│  GET /api/Routes/saved
│  → Ve "Ruta Lunes - Centro" (guardada el lunes)
├─ Driver CARGA la ruta guardada:
│  POST /api/Routes/saved/1/load
│  → Ve nuevamente los 5 pedidos del lunes
├─ Driver decide cambiar el nombre:
│  POST /api/Routes/saved/1/rename
│  {"newName": "Ruta Lunes Exitosa - 100% Completada"}
└─ Ruta renombrada en el historial

Miércoles
├─ Driver revisa sus rutas guardadas
├─ Ve: "Ruta Lunes Exitosa - 100% Completada" (lunes)
├─ Ve: "Ruta Martes Parcial" (martes - incompleta)
├─ Decide no guardar hoy
└─ Solo completa los pedidos asignados
```

---

### Identificación por Teléfono

**Cada Driver está identificado por su `PhoneNumber`**:

```sql
-- En la BD
- User.PhoneNumber = "+573001234567"
- SavedRoute.DriverId → User.Id → User.PhoneNumber

-- Los logs muestran:
"Ruta 'Ruta Mañana' guardada para conductor 2 (+573001234567)"
"Ruta 'Ruta Mañana' cargada para conductor 2 (+573001234567)"
```

---

### Estado Actual vs Futuro

| Característica | Actual | Futuro |
|---|---|---|
| **Un Driver con múltiples pedidos** | ✅ Implementado | ✅ Continuará |
| **Ruta optimizada para todos los pedidos** | ✅ Implementado (Java) | ✅ Continuará |
| **GUARDAR rutas con nombre** | ✅ **NUEVO** | ✅ Continuará |
| **Cargar rutas guardadas** | ✅ **NUEVO** | ✅ Continuará |
| **Renombrar rutas** | ✅ **NUEVO** | ✅ Continuará |
| **Identificadas por teléfono** | ✅ **NUEVO** | ✅ Continuará |
| **Filtrar por fecha** | ❌ No | 🔄 Posible |
| **Rutas por zona** | ❌ No | 🔄 Posible |
| **Historial de rutas completadas** | ❌ No | 🔄 Posible |

---

### Consultas SQL Relevantes

```sql
-- Ver todas las rutas guardadas de un Driver (por teléfono)
SELECT r.* FROM SavedRoutes r
JOIN AspNetUsers u ON r.DriverId = u.Id
WHERE u.PhoneNumber = '+573001234567' AND r.IsActive = true
ORDER BY r.CreatedDate DESC;

-- Ver cuántas rutas ha guardado un Driver
SELECT COUNT(*) as RutasGuardadas FROM SavedRoutes
WHERE DriverId = (SELECT Id FROM AspNetUsers WHERE PhoneNumber = '+573001234567')
AND IsActive = true;

-- Ver ruta más utilizada (última fecha de uso)
SELECT * FROM SavedRoutes
WHERE DriverId = 2
ORDER BY LastUsedDate DESC NULLS LAST;

-- Ver Driver con más rutas guardadas
SELECT u.PhoneNumber, COUNT(r.Id) as TotalRutas
FROM SavedRoutes r
JOIN AspNetUsers u ON r.DriverId = u.Id
WHERE r.IsActive = true
GROUP BY u.PhoneNumber
ORDER BY TotalRutas DESC;
```

---

### Respuesta a Preguntas Frecuentes

**P: ¿Las rutas guardadas se pierden si el conductor se va?**
R: No. Las rutas se guardan en BD y el teléfono es el identificador permanente.

**P: ¿Puede un Driver compartir una ruta con otro Driver?**
R: No actualmente. Cada ruta es privada por conductor (identificada por DriverId + PhoneNumber).

**P: ¿Cuántas rutas puede guardar un Driver?**
R: Sin límite actual. Se pueden guardar todas las que necesite.

**P: ¿Qué pasa si cambia de teléfono?**
R: Las rutas quedan vinculadas al DriverId (ID en BD), no al número. Si cambia PhoneNumber en el perfil, sigue teniendo acceso.

**P: ¿Se puede exportar una ruta guardada?**
R: Actualmente no, pero se podría agregar un endpoint `GET /api/Routes/saved/{routeId}/export` en el futuro.

---

## 📊 Tabla de Endpoints Actualizada

| Endpoint | Método | Rol | Propósito |
|----------|--------|-----|----------|
| `/api/Auth/login` | POST | - | Login |
| `/api/Auth/register` | POST | - | Registro |
| `/api/Orders/my-route` | GET | Driver | Ver ruta actual |
| `/api/Orders/{orderId}/complete` | POST | Driver | Completar pedido |
| `/api/Files/upload` | POST | Auth | Subir foto |
| **`/api/Routes/save`** | **POST** | **Driver** | **GUARDAR ruta** |
| **`/api/Routes/saved`** | **GET** | **Driver** | **Ver rutas guardadas** |
| **`/api/Routes/saved/{routeId}`** | **GET** | **Driver** | **Ver detalles** |
| **`/api/Routes/saved/{routeId}/load`** | **POST** | **Driver** | **Cargar ruta** |
| **`/api/Routes/saved/{routeId}/rename`** | **POST** | **Driver** | **Renombrar ruta** |
| **`/api/Routes/saved/{routeId}`** | **DELETE** | **Driver** | **Eliminar ruta** |
| `/api/Orders` | POST | Admin | Crear pedido |
| `/api/Orders` | GET | Admin | Ver todos |
| `/api/Orders/{orderId}/assign/{driverId}` | PUT | Admin | Asignar |
| `/api/Orders/optimize-route/{driverId}` | POST | Admin | Optimizar (Java) |

---

## 📊 Resumen Final

| Acción | Admin | Driver |
|--------|-------|--------|
| Registrarse | ✅ `role: "Admin"` | ✅ `role: "Driver"` o default |
| Login | ✅ | ✅ |
| Ver todos los pedidos | ✅ | ❌ |
| Ver su ruta (múltiples pedidos) | ❌ | ✅ |
| Ver pedidos de otro driver | ❌ | ❌ |
| Crear pedidos | ✅ | ❌ |
| Completar sus propios pedidos | ❌ | ✅ |
| Optimizar rutas (Java) | ✅ | ❌ |
| Analizar imágenes | ✅ | ❌ |
| Tener múltiples rutas simultáneamente | ❌ | ✅ |

---

**Última actualización**: 4 de Diciembre, 2025  
**Versión**: 1.1.0

