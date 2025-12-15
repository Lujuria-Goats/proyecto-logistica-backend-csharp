# 📱 Guía Completa de API Móvil (ApexVision Driver)

Esta documentación describe en detalle los endpoints disponibles para la aplicación móvil de conductores, incluyendo las estructuras de datos (JSON) requeridas y las respuestas esperadas.

---

## 🔐 1. Autenticación

### 1.1 Registro de Conductor (Nuevo)
Permite a un nuevo conductor registrarse en la plataforma.

**Endpoint:** `POST /api/Auth/register/driver`
**Auth:** Pública (No requiere token)

**Cuerpo de la Petición (JSON):**

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| `fullName` | string | **Sí** | Nombre completo del conductor. |
| `email` | string | **Sí** | Correo electrónico único. |
| `password` | string | **Sí** | Contraseña (mínimo 6 caracteres). |
| `phoneNumber` | string | **Sí** | Número de celular único (usado para vinculación). |
| `userName` | string | No | Nombre de usuario opcional. |

**Ejemplo Request:**
```json
{
  "fullName": "Carlos Pérez",
  "email": "carlos@example.com",
  "password": "Password123!",
  "phoneNumber": "3001234567"
}
```

**Ejemplo Response (200 OK):**
```json
{
  "message": "Driver registrado exitosamente.",
  "userId": "guid-del-usuario",
  "userName": "3001234567", // Si no se envió usuario, usa el teléfono o email
  "role": "Driver"
}
```

---

### 1.2 Iniciar Sesión (Login)
Obtiene el token de acceso necesario para todas las demás operaciones.

**Endpoint:** `POST /api/Auth/login`
**Auth:** Pública

**Cuerpo de la Petición (JSON):**

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| `identifier` | string | **Sí** | Puede ser: Email, Username o Número de Teléfono. |
| `password` | string | **Sí** | La contraseña del usuario. |

**Ejemplo Request:**
```json
{
  "identifier": "3001234567",
  "password": "Password123!"
}
```

**Ejemplo Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...", // Token JWT para usar en headers
  "userId": "guid-usuario",
  "userName": "3001234567",
  "fullName": "Carlos Pérez",
  "email": "carlos@example.com",
  "phoneNumber": "3001234567",
  "role": "Driver"
}
```

> ⚠️ **Importante:** Debes enviar el token en el header `Authorization` para las siguientes peticiones:
> `Authorization: Bearer <tu_token_aqui>`

---

### 1.3 Obtener Info Usuario
Valida si el token sigue activo y obtiene datos actualizados del perfil.

**Endpoint:** `GET /api/Auth/me`
**Auth:** Bearer Token

**Response (200 OK):** Retorna el mismo objeto de usuario que el Login (sin el token).

---

### 1.4 Cambiar Contraseña 🔐
Permite a cualquier usuario autenticado (Driver o Admin) cambiar su contraseña actual.

**Endpoint:** `POST /api/Auth/change-password`
**Auth:** Bearer Token

**Cuerpo (JSON):**
| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| `currentPassword` | string | **Sí** | Contraseña actual del usuario. |
| `newPassword` | string | **Sí** | Nueva contraseña (min 6 caracteres). |

**Ejemplo:**
```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword456!"
}
```

**Response (200 OK):**
```json
{ "message": "Contraseña actualizada correctamente." }
```

---

## 🚦 Consultas de Estado (Dashboard)

### 1.5 Estado de la Ruta Asignada
Para saber el "estado" de la ruta (cuántos pedidos faltan, completados, etc.), el frontend debe consultar la lista de pedidos y calcularlo localmente.

**Endpoint:** `GET /api/Orders/my-route`
*   **Total Pedidos:** `response.length`
*   **Pendientes:** Filtrar por `status: "Pending"`
*   **Completados:** Filtrar por `status: "Completed"`

### 1.6 Transportadores Activos (Admin)
Para ver qué conductores están trabajando.

**Endpoint:** `GET /api/Drivers` (Lista todos) ó `GET /api/Drivers/dashboard` (Resumen numérico).
*   El endpoint `GET /api/Drivers` devuelve la lista de conductores.
*   El frontend puede inferir "Activo" si tienen pedidos pendientes asignados (requiere lógica de negocio adicional si se necesita un flag específico 'IsOnline').

---

## 🛣️ 2. Gestión de Rutas (Saved Routes)

El conductor puede tener múltiples rutas "guardadas" (plantillas) y seleccionar cuál cargar para trabajar hoy.

### 2.1 Listar Rutas Guardadas
Obtiene todas las rutas disponibles para el conductor (asignadas por admin o guardadas por él mismo).

**Endpoint:** `GET /api/Routes/saved`
**Auth:** Bearer Token

**Response (200 OK):** Array de rutas.

```json
{
  "phoneNumber": "3001234567",
  "totalSavedRoutes": 2,
  "routes": [
    {
      "id": 15, // ID de la ruta
      "routeName": "Ruta Lunes - Norte",
      "createdDate": "2024-12-14T10:00:00Z",
      "lastUsedDate": null,
      "isActive": true,
      "optimizationScore": 95,
      "assignedBy": { // Si fue asignada por un admin
          "id": 1,
          "fullName": "Admin Principal",
          "companyName": "Logística SAS"
      }
    }
  ]
}
```

---

### 2.2 Ver Detalle de Ruta
Muestra qué pedidos contiene una ruta guardada antes de cargarla.

**Endpoint:** `GET /api/Routes/saved/{routeId}`
**Auth:** Bearer Token

**Response:** Devuelve detalles de la ruta y la lista completa de pedidos (`orders`).

---

### 2.3 Cargar Ruta (Activar para Trabajo) 🚀
Selecciona una ruta guardada y la establece como la ruta activa de trabajo. Esto trae los pedidos y los prepara para la entrega.

**Endpoint:** `POST /api/Routes/saved/{routeId}/load`
**Auth:** Bearer Token
**Body:** Vacío (no requiere cuerpo).

**Response (200 OK):**

```json
{
  "message": "Ruta cargada exitosamente.",
  "routeName": "Ruta Lunes - Norte",
  "totalOrders": 10,
  "orders": [ // Lista de pedidos ordenada según la secuencia guardada
    {
      "id": 105,
      "description": "Entrega Paquete #123",
      "latitude": 6.2512,
      "longitude": -75.5630,
      "address": "Calle 10 # 5-50",
      "status": "Pending", // Pending, InTransit, Delivered
      "requiresEvidence": true,
      "stopOrder": 1, // Orden sugerido de visita (Secuencia)
      "stopNumber": 1
    },
    {
      "id": 108,
      "address": "Carrera 40 # 20-10",
      "stopOrder": 2,
      "stopNumber": 2,
      ...
    }
  ]
}
```

---

### 2.4 Renombrar Ruta
Cambia el nombre para identificarla mejor (ej: "Ruta Martes").

**Endpoint:** `POST /api/Routes/saved/{routeId}/rename`
**Auth:** Bearer Token

**Cuerpo (JSON):**
| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| `newName` | string | **Sí** | Nuevo nombre (max 100 caracteres). |

**Ejemplo:**
```json
{ "newName": "Mi Ruta Favorita" }
```

---

### 2.5 Eliminar Ruta
Elimina una ruta de la lista de guardadas (no elimina los pedidos, solo la agrupación).

**Endpoint:** `DELETE /api/Routes/saved/{routeId}`
**Auth:** Bearer Token

---

### 2.6 Guardar Ruta Actual
Si el conductor tiene pedidos asignados y quiere guardar esa lista como una nueva plantilla (ej: para repetirla mañana).

**Endpoint:** `POST /api/Routes/save`
**Auth:** Bearer Token

**Cuerpo (JSON):**
| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| `routeName` | string | **Sí** | Nombre para identificar la ruta. |
| `orderIds` | array[int] | **Sí** | Lista de IDs de pedidos a incluir. |

**Ejemplo:**
```json
{
  "routeName": "Ruta Personalizada Viernes",
  "orderIds": [101, 102, 105] 
}
```

---

## 📦 3. Gestión de Pedidos (Operación Diaria)

### 3.1 Obtener Mis Pedidos Activos (Sincronización)
Si la app se cierra y abre, usar este endpoint para recuperar el estado actual de los pedidos asignados.

**Endpoint:** `GET /api/Orders/my-route`
**Auth:** Bearer Token

**Response:** Devuelve una lista de `OrderDto` (igual que en "Cargar Ruta").

---

### 3.2 Completar Pedido y Subir Evidencia 📸
Este es el endpoint principal para confirmar una entrega.

**Endpoint:** `POST /api/Orders/{orderId}/complete`
**Auth:** Bearer Token
**Content-Type:** `multipart/form-data` (No es JSON)

**Parámetros Form-Data:**

| Campo | Tipo | Descripción |
|---|---|---|
| `file` | File (Binary) | **Requerido si `requiresEvidence: true`**. La foto de la entrega.Formato: JPG/PNG. |

**Validación IA (Lado Servidor):**
El servidor analizará la foto con Azure Vision.
*   **Éxito (200):** La foto muestra un paquete/entrega válido. El pedido pasa a estado `Completed`.
*   **Fallo (400):** La IA no detectó un paquete válido.
    *   **Mensaje de error:** "La foto no parece mostrar un paquete o entrega. Por favor, toma una foto clara del paquete."
    *   **Acción App:** Mostrar alerta al usuario y pedir que repita la foto.

---

### 3.3 Optimizar Ruta ⚡
Reordena los puntos de entrega actuales para minimizar la distancia recorrida.

**Endpoint:** `POST /api/Orders/my-route/optimize`
**Auth:** Bearer Token
**Body:** Vacío.

**Response (200 OK):**
```json
{ "message": "Route optimization initiated." }
```

---

### 3.4 Historial de Entregas (Nuevo) 🕒
Obtiene la lista de los últimos 50 pedidos que ya han sido completados por el conductor.

**Endpoint:** `GET /api/Orders/history`
**Auth:** Bearer Token

**Response (200 OK):**
```json
[
  {
    "id": 105,
    "description": "Entrega Paquete #123",
    "address": "Calle 10 # 5-50",
    "status": "Completed",
    "evidenceUrl": "https://res.cloudinary.com/...",
    "deliveredAt": "2024-12-15T15:30:00Z" // Fecha de entrega
  }
]
```

---

### 3.5 Resumen de Ruta (Estadísticas) 📊
Perfecto para mostrar una barra de progreso o dashboard. Obtiene el conteo exacto de pedidos asignados.

**Endpoint:** `GET /api/Orders/route-summary`
**Auth:** Bearer Token

**Response (200 OK):**
```json
{
  "total": 10,
  "pending": 6,
  "completed": 4
}
```

---

## 📋 Resumen de Modelos JSON

### OrderDto
Objeto principal de pedido.
```json
{
  "id": 1,
  "description": "Caja frágil",
  "latitude": 6.217,
  "longitude": -75.567,
  "address": "Dirección completa",
  "status": "Pending", // Pending, InTransit, Delivered, Completed
  "requiresEvidence": true,  // Si es true, exige foto al completar
  "evidenceUrl": "https://imagen... jpg", // URL si ya se completó
  "stopOrder": 1, // Número de secuencia para la ruta
  "stopNumber": 1 // (Legacy/Alias de stopOrder)
}
```

---

## 💡 Recomendaciones para App Móvil

1.  **Manejo Offline:** Implementar base de datos local (SQLite). Si falla la subida de foto (`/complete`) por red, guardarla en cola y reintentar cuando haya conexión.
2.  **Validación de Errores:** Mostrar mensajes claros si falla el Login (401) o la Validación de Foto (400).
3.  **Mapas:** Usar `latitude` y `longitude` para trazar marcadores en Google Maps/Mapbox.
