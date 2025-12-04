# Flujo de Ruta Organizada: Dashboard → Móvil

## 📋 Resumen del Flujo

El sistema funciona así:

1. **Admin (Dashboard)** crea pedidos y los asigna a conductores
2. **Admin (Dashboard)** solicita optimización de ruta para un conductor
3. **Java Microservice** calcula la ruta óptima
4. **Backend (.NET)** retorna la ruta optimizada al dashboard
5. **Móvil (Driver)** consulta su ruta asignada y optimizada

---

## 🔄 Flujo Detallado

### Fase 1: Creación de Pedidos (Admin/Dashboard)

```
POST /api/Orders
{
  "address": "Dirección de entrega",
  "latitude": 6.2176,
  "longitude": -75.5353,
  "description": "Descripción del paquete",
  "requiresEvidence": true
}

✅ Response: { "orderId": 1, "message": "Order created successfully" }
```

**Requisito**: Token con rol `Admin`

---

### Fase 2: Asignación de Conductor (Admin/Dashboard)

```
PUT /api/Orders/{orderId}/assign/{driverId}

Ejemplo: PUT /api/Orders/1/assign/2

✅ Response: { "message": "Driver assigned successfully." }
```

**Requisito**: Token con rol `Admin`

---

### Fase 3: Solicitar Optimización de Ruta (Admin/Dashboard)

```
POST /api/Orders/optimize-route/{driverId}

Ejemplo: POST /api/Orders/optimize-route/2

✅ Response: { "message": "Route optimization initiated." }
```

**Requisito**: Token con rol `Admin`

**Lo que sucede internamente:**
1. Backend envía solicitud al **Java Microservice** (`http://apex-java:8081/`)
2. Java calcula la ruta óptima usando algoritmos de optimización
3. Java retorna la ruta ordenada
4. Backend almacena/procesa la ruta optimizada
5. Conductor recibe la ruta en su próxima consulta

---

### Fase 4: Consulta de Ruta Asignada (Móvil/Driver)

```
GET /api/Orders/my-route

✅ Response:
[
  {
    "id": 1,
    "address": "Dirección 1",
    "latitude": 6.2176,
    "longitude": -75.5353,
    "description": "Paquete 1",
    "status": "Pending",
    "requiresEvidence": true,
    "driverId": 2,
    "evidenceUrl": null
  },
  {
    "id": 2,
    "address": "Dirección 2",
    "latitude": 6.2200,
    "longitude": -75.5300,
    "description": "Paquete 2",
    "status": "Pending",
    "requiresEvidence": false,
    "driverId": 2,
    "evidenceUrl": null
  }
]
```

**Requisito**: Token con rol `Driver`

**Nota**: Los pedidos se retornan **en el orden optimizado** después de que se ejecutó la optimización.

---

## 🔐 Control de Acceso por Roles

| Endpoint | Admin | Driver | Descripción |
|----------|-------|--------|-------------|
| `POST /api/Orders` | ✅ | ❌ | Crear pedido |
| `PUT /api/Orders/{id}/assign/{driverId}` | ✅ | ❌ | Asignar conductor |
| `GET /api/Orders` | ✅ | ❌ | Ver todos los pedidos |
| `GET /api/Orders/my-route` | ❌ | ✅ | Ver ruta asignada (conductor) |
| `POST /api/Orders/my-route/optimize` | ❌ | ✅ | Optimizar mi ruta (conductor) |
| `POST /api/Orders/{id}/complete` | ❌ | ✅ | Marcar pedido como completado |

---

## 📱 Ejemplo de Caso de Uso Completo

### 1. Admin crea 3 pedidos en el dashboard

```
POST /api/Orders (Pedido A)
POST /api/Orders (Pedido B)
POST /api/Orders (Pedido C)
```

### 2. Admin asigna todos al conductor #2

```
PUT /api/Orders/1/assign/2
PUT /api/Orders/2/assign/2
PUT /api/Orders/3/assign/2
```

### 3. Admin solicita optimización de ruta

```
POST /api/Orders/optimize-route/2
```

Backend envía esto al Java microservice:
```
Pedidos a optimizar para conductor 2: [1, 2, 3]
Ubicaciones: [(6.2176, -75.5353), (6.2200, -75.5300), (6.2150, -75.5400)]
```

Java retorna:
```
Ruta optimizada: [3, 1, 2]
Orden recomendado: Primero 3, luego 1, luego 2
```

### 4. Conductor (móvil) consulta su ruta

```
GET /api/Orders/my-route
```

Recibe los 3 pedidos **EN EL ORDEN OPTIMIZADO**:
```
[Pedido 3, Pedido 1, Pedido 2]
```

El móvil muestra esta secuencia al conductor como la ruta más eficiente.

---

## ⚙️ Servicios Involucrados

### Backend (.NET)
- **OrdersController**: Gestiona creación, asignación y consulta de pedidos
- **OptimizationService**: Se comunica con el microservicio Java

### Microservicio Java
- **RouteOptimizer**: Calcula la ruta óptima usando algoritmos de optimización
- **Recibe**: Lista de pedidos con coordenadas
- **Retorna**: Orden optimizado de los pedidos

### Móvil
- **GET /api/Orders/my-route**: Obtiene la ruta asignada y optimizada
- **Muestra**: Los pedidos en el orden más eficiente
- **POST /api/Orders/{id}/complete**: Marca cada pedido como completado

---

## 🚀 Flujo Visual

```
┌─────────────────┐
│   Dashboard     │
│   (Admin)       │
└────────┬────────┘
         │
         ├─► POST /api/Orders (Create 3 orders)
         │
         ├─► PUT /api/Orders/X/assign/2 (Assign to Driver #2)
         │
         └─► POST /api/Orders/optimize-route/2
                │
                ▼
         ┌──────────────────┐
         │  Backend (.NET)  │
         │   ApexVision     │
         └────────┬─────────┘
                  │
                  ├─► OptimizationService
                  │
                  └──────────────────────────────────┐
                                                     │
                                    ┌────────────────▼───────┐
                                    │ Java Microservice      │
                                    │ (RouteOptimizer)       │
                                    │ http://apex-java:8081/ │
                                    │                        │
                                    │ Calcula ruta óptima    │
                                    └────────────────┬───────┘
                                                     │
                                    ┌────────────────▼───────┐
         ┌──────────────────┐       │ Retorna orden          │
         │  Móvil (Driver)  │       │ optimizado             │
         │                  │       └────────────────────────┘
         │ GET /my-route    │◄──────────┐
         │                  │           │
         │ Muestra ruta     │           │
         │ optimizada       │           │
         └──────────────────┘   Backend retorna
                                orden al móvil
```

---

## ✅ Verificación del Flujo

Para verificar que todo funciona:

1. **Login como Admin**
   ```
   POST /api/auth/login
   ```

2. **Crear un pedido**
   ```
   POST /api/Orders
   ```

3. **Obtener ID del conductor**
   ```
   GET /api/users/drivers
   ```

4. **Asignar conductor**
   ```
   PUT /api/Orders/{orderId}/assign/{driverId}
   ```

5. **Optimizar ruta**
   ```
   POST /api/Orders/optimize-route/{driverId}
   ```

6. **Login como Conductor**
   ```
   POST /api/auth/login (con credenciales de conductor)
   ```

7. **Consultar ruta asignada**
   ```
   GET /api/Orders/my-route
   ```

---

## 📝 Notas Importantes

- La **ruta optimizada se calcula en el microservicio Java**
- El **backend almacena la información de los pedidos en PostgreSQL**
- El **orden optimizado se retorna al móvil cada vez que consulta `/my-route`**
- Los **roles (Admin/Driver) se validan mediante JWT**
- La **autenticación es obligatoria** en todos los endpoints excepto `/api/auth/login` y `/api/auth/register`

---

## 🔗 Tecnologías de Comunicación

- **Dashboard ↔ Backend**: REST API HTTP (JSON)
- **Backend ↔ Java Microservice**: REST API HTTP (JSON, Puerto 8081)
- **Backend ↔ Base de Datos**: Entity Framework Core (PostgreSQL)
- **Móvil ↔ Backend**: REST API HTTP (JSON)


