# Ejemplos de JSON para Pruebas en Swagger

Aquí tienes una colección de ejemplos de JSON listos para usar en las pruebas de la API de ApexVision a través de Swagger.

---

## 1. Autenticación (`/api/Auth`)

### Login de Administrador

Usa este JSON para obtener el token de autenticación del usuario administrador que se crea automáticamente al iniciar la aplicación.

**Endpoint:** `POST /api/Auth/login`

```json
{
  "email": "admin@apexvision.com",
  "password": "Admin123!"
}
```
> **Nota:** Después de ejecutar esta petición, copia el `token` de la respuesta. Deberás hacer clic en el botón **"Authorize"** en la parte superior de Swagger y pegar el token en el formato `Bearer TU_TOKEN_AQUI` para poder probar los endpoints protegidos.

### Registro de un Nuevo Conductor (Público)

Cualquier persona puede registrar un nuevo conductor.

**Endpoint:** `POST /api/Auth/register`

```json
{
  "fullName": "Carlos Rodriguez",
  "email": "carlos.driver@example.com",
  "password": "DriverPass123!",
  "phoneNumber": "+573109876543",
  "role": "Driver"
}
```

---

## 2. Gestión de Pedidos (`/api/Orders`)

**Importante:** Recuerda que debes estar autorizado como **Admin** para usar estos endpoints.

### Crear un Pedido Simple (Sin Evidencia)

Este pedido no requerirá que el conductor suba una foto para completarlo.

**Endpoint:** `POST /api/orders`

```json
{
  "address": "Centro Comercial Santafé, Medellín",
  "latitude": 6.198,
  "longitude": -75.578,
  "description": "Entrega de paquete electrónico",
  "requiresEvidence": false
}
```

### Crear un Pedido Verificado (Requiere Evidencia con IA)

Este pedido **obligará** al conductor a subir una foto de la entrega (un paquete, una caja, etc.) para poder marcarlo como completado.

**Endpoint:** `POST /api/orders`

```json
{
  "address": "Parque Lleras, Medellín",
  "latitude": 6.2098,
  "longitude": -75.567,
  "description": "Paquete frágil, entregar en recepción. Requiere foto.",
  "requiresEvidence": true
}
```

---

## 3. Optimización de Ruta (`/api/Orders`)

### Disparar la Optimización para un Conductor

Este endpoint toma todos los pedidos pendientes de un conductor específico y los envía al microservicio de Java para que calcule la ruta óptima.

**Endpoint:** `POST /api/orders/optimize-route/{driverId}`

**Ejemplo:** Para optimizar la ruta del conductor con `Id = 2` (puedes obtener los Ids de los conductores desde `GET /api/users/drivers`).

> No necesitas enviar un cuerpo JSON para esta petición, solo el `driverId` en la URL.

---
