# 🛣️ Endpoints de Rutas - Referencia Rápida

## Para Admin

| Endpoint | Método | ¿Para qué sirve? |
|----------|--------|------------------|
| `GET /api/Routes/all` | GET | **Ver todas las rutas** - Muestra templates propios + rutas asignadas a conductores |
| `POST /api/Routes/save` | POST | **Crear ruta** - Guarda una nueva ruta (template) con lista de órdenes |
| `PUT /api/Routes/saved/{id}` | PUT | **Editar ruta** - Cambiar nombre y/u órdenes de una ruta propia |
| `POST /api/Routes/saved/{id}/assign` | POST | **Asignar ruta** - Copia la ruta a un conductor (por teléfono) |
| `DELETE /api/Routes/saved/{id}` | DELETE | **Eliminar ruta** - Desactiva una ruta |

---

## Para Driver

| Endpoint | Método | ¿Para qué sirve? |
|----------|--------|------------------|
| `GET /api/Routes/saved` | GET | **Ver mis rutas** - Lista las rutas guardadas del conductor |
| `GET /api/Routes/saved/{id}` | GET | **Ver detalle** - Obtiene una ruta con todas sus órdenes |
| `POST /api/Routes/saved/{id}/load` | POST | **Cargar ruta** - Activa la ruta para comenzar entregas |
| `POST /api/Routes/saved/{id}/rename` | POST | **Renombrar** - Cambia el nombre de una ruta |
| `DELETE /api/Routes/saved/{id}` | DELETE | **Eliminar** - Quita la ruta de la lista |

---

## Ejemplos de Body

### Crear/Guardar ruta
```json
POST /api/Routes/save
{
  "routeName": "Ruta Zona Norte",
  "orderIds": [10, 11, 12]
}
```

### Asignar a conductor
```json
POST /api/Routes/saved/5/assign
{
  "driverPhoneNumber": "+573109876543"
}
```

### Editar ruta
```json
PUT /api/Routes/saved/5
{
  "routeName": "Ruta Zona Norte Actualizada",
  "orderIds": [10, 11, 12, 13]
}
```

### Renombrar (Driver)
```json
POST /api/Routes/saved/5/rename
{
  "newName": "Mi ruta favorita"
}
```

---

## Respuesta de GET /api/Routes/all

```json
{
  "totalRoutes": 5,
  "templates": [
    {
      "routeId": 1,
      "routeName": "Ruta Zona Norte",
      "orderCount": 3,
      "driver": { "fullName": "Admin", "phoneNumber": "+573001234567" }
    }
  ],
  "assignedRoutes": [
    {
      "routeId": 2,
      "routeName": "Ruta Zona Norte",
      "orderCount": 3,
      "driver": { "fullName": "Carlos Pérez", "phoneNumber": "+573109876543" }
    }
  ]
}
```

---

**Nota:** Todos requieren `Authorization: Bearer {token}`
