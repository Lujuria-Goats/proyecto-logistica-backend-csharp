# 📱 Guía de Integración Móvil (ApexVision Driver)

Esta guía detalla el flujo completo para la aplicación móvil de conductores, desde el login hasta la entrega de pedidos con evidencia fotográfica.

---

## 🔐 1. Autenticación

El conductor debe iniciar sesión para obtener el **Token JWT** que se usará en todas las peticiones siguientes.

**Endpoint:** `POST /api/Auth/login`

**Request:**
```json
{
  "identifier": "driver@apexvision.com", // Email o Username
  "password": "Password123!"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2024-12-15T10:00:00Z",
  "user": {
    "fullName": "Juan Conductor",
    "role": "Driver"
  }
}
```

> 💾 **Guardar:** Almacenar el `token` en almacenamiento seguro (SecureStorage/Keychain).

---

## 🛣️ 2. Gestión de Rutas

El conductor tiene dos formas de obtener trabajo:
1.  **Ver rutas asignadas** (Previamente creadas por el admin).
2.  **Cargar una ruta** para empezar a trabajarla.

### Ver Mis Rutas Guardadas
Lista las rutas que le han sido asignadas o que ha guardado.

**Endpoint:** `GET /api/Routes/saved`
**Header:** `Authorization: Bearer {token}`

**Response:**
```json
{
  "totalSavedRoutes": 2,
  "routes": [
    {
      "id": 10,
      "routeName": "Ruta Norte - Lunes",
      "assignedBy": { "fullName": "Admin Principal" }, // Quién se la asignó
      "createdDate": "2024-12-14T08:00:00Z"
    }
  ]
}
```

### Renombrar Ruta ✏️
Permite al conductor cambiar el nombre de una de sus rutas guardadas.

**Endpoint:** `POST /api/Routes/saved/{id}/rename`
**Body:**
```json
{
  "newName": "Nueva Ruta Martes"
}
```

### Eliminar Ruta 🗑️
Elimina (o desactiva) una ruta de la lista del conductor.

**Endpoint:** `DELETE /api/Routes/saved/{id}`


### Cargar una Ruta (Activar)
Cuando el conductor selecciona una ruta para trabajar, debe "cargarla". Esto trae todas las órdenes asociadas.

**Endpoint:** `POST /api/Routes/saved/{id}/load`

**Response:**
```json
{
  "message": "Ruta cargada exitosamente.",
  "totalOrders": 15,
  "orders": [
    {
      "id": 101,
      "address": "Calle 10 # 5-20",
      "stopOrder": 1,
      "stopNumber": 1,
      "latitude": 6.251,
      "longitude": -75.563,
      "status": "Pending",
      "requiresEvidence": true
    },
    ...
  ]
}
```

> 📱 **UI:** Mostrar estas órdenes en un mapa o lista ordenada.

---

## 📦 3. Gestión de Órdenes (Entregas)

### Actualizar Estado (En Camino)
Opcional: Si quieres trackear cuando el conductor va hacia el destino.

No hay endpoint específico, pero se mantiene localmente.

### Completar Orden (Con Evidencia) 📸
Este es el paso más crítico. Si la orden requiere evidencia (`requiresEvidence: true`), debe subir una foto. La IA validará si es un paquete real.

**Endpoint:** `POST /api/Orders/{orderId}/complete`
**Content-Type:** `multipart/form-data`

**Form Data:**
*   `file`: (Archivo de imagen jpg/png)

**Flujo de Validación:**
1.  App envía foto.
2.  Backend sube a Cloudinary.
3.  Backend envía a Azure Vision IA.
4.  **Si es válida:** Retorna 200 OK.
5.  **Si es inválida:** Retorna 400 Bad Request con mensaje de error.

**Ejemplo de Error (400):**
```json
{
  "message": "La foto no parece mostrar un paquete o entrega. Por favor, toma una foto clara del paquete."
}
```

> ⚠️ **Manejo en App:** Si recibe 400, mostrar alerta al conductor: "Foto rechazada por IA. Intenta enfocar mejor el paquete" y permitir reintentar.

---

## 🔄 4. Flujo Offline (Recomendación)

Como los conductores pueden perder señal:

1.  **Cargar ruta con Wi-Fi/Datos:** Guardar órdenes en base de datos local (SQLite/Realm).
2.  **Entregas Offline:**
    *   Guardar foto localmente.
    *   Marcar orden como "Pendiente de sincronizar".
3.  **Sincronización:**
    *   Cuando vuelva la red, subir las fotos una por una al endpoint `/complete`.

---


## 🔄 4.5. Obtener Ruta Actual (Sincronización)
Si la app se reinicia o necesita refrescar la lista de pedidos pendientes asignados al conductor:

**Endpoint:** `GET /api/Orders/my-route`

---

## 🗺️ 5. Optimización (Opcional)

Si el conductor quiere reordenar sus puntos actuales para ser más eficiente:

**Endpoint:** `POST /api/Orders/my-route/optimize`

**Response:**
Retorna la lista de órdenes reordenada óptimamente.

---

## 🚨 Resumen de Códigos HTTP

| Código | Significado | Acción App |
|--------|-------------|------------|
| 200 | Éxito | Continuar flujo |
| 401 | Token vencido | Redirigir a Login o renovar token |
| 400 | Error de validación (IA) | Mostrar mensaje y pedir nueva foto |
| 404 | Orden no encontrada | Sincronizar lista de órdenes |
| 500 | Error servidor | Reintentar más tarde |

---

**Soporte:**
Si la IA rechaza constantemente una foto válida, el conductor puede contactar a soporte, pero el sistema está calibrado para detectar cajas, paquetes y bolsas de entrega.
