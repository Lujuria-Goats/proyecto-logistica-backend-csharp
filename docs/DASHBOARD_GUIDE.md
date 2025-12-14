# 📊 Guía de Integración - Dashboard Admin

Este endpoint consolida todas las estadísticas clave para el panel principal del Administrador.

**Endpoint:** `GET /api/Dashboard/admin-summary`
**Header:** `Authorization: Bearer {token}`

---

## 📦 Estructura de la Respuesta

El objeto JSON raíz tiene dos bloques principales: `stats` (contadores) y `recentActivity` (lista cronológica).

```json
{
  "stats": {
    "orders": {
      "total": 1500,          // Total histórico
      "today": 45,            // Creados hoy
      "pending": 10,          // Pendientes de acción
      "inTransit": 5,         // En camino ahora mismo
      "completedToday": 30,   // Entregados hoy
      "canceled": 2           // Cancelados (histórico)
    },
    "drivers": {
      "totalLinked": 12,      // Total conductores vinculados
      "activeToday": 8        // Conductores que han entregado hoy
    },
    "routes": {
      "total": 50,            // Total rutas (templates + asignadas)
      "active": 5             // Rutas activas actualmente
    }
  },
  "recentActivity": [
    // Lista mezclada de últimas entregas y rutas creadas (Max 10)
    {
      "type": "Entrega",
      "message": "Pedido a Calle 10 completado",
      "subtext": "Juan Pérez",
      "date": "2024-12-14T10:30:00Z"
    },
    {
      "type": "Ruta",
      "message": "Ruta 'Zona Norte' asignada",
      "subtext": "Juan Pérez",
      "date": "2024-12-14T08:00:00Z"
    }
  ]
}
```

---

## 🎨 Sugerencias para el Frontend

### 1. Tarjetas de Resumen (Top Cards)
*   **Pedidos Hoy:** Usa `stats.orders.today`. (Subtítulo: "vs ayer" si quisieras calcularlo, por ahora solo valor absoluto).
*   **En Curso:** Suma `stats.orders.inTransit` o usa `stats.routes.active`.
*   **Efectividad:** `(completedToday / ordersToday) * 100` % (Muestra tasa de cumplimiento diario).
*   **Conductores Activos:** `stats.drivers.activeToday` de `stats.drivers.totalLinked`.

### 2. Gráficos (Opcional)
*   Un gráfico de torta (Pie Chart) con el estado de las órdenes actuales:
    *   Pendientes: `stats.orders.pending`
    *   En Camino: `stats.orders.inTransit`
    *   Entregados: `stats.orders.completedToday`

### 3. Lista de Actividad Reciente
*   Usa el array `recentActivity`.
*   Muestra un **icono** diferente según `type` ("Entrega" 📦, "Ruta" 🗺️).
*   Muestra `message` como título y `subtext` (nombre del conductor) como detalle.
*   Muestra `date` formateada (ej: "Hace 10 min").

---

## 🚨 Manejo de Errores
*   **401 Unauthorized:** El token expiró o no es Admin. Redirigir a login.
*   **500 Server Error:** Fallo en base de datos. Mostrar "Error cargando métricas".
