# API Optimizer (Front -> Backend -> Java)

Resumen rápido

- Endpoint que el front debe llamar (proxy .NET): `POST /api/optimizer/optimize` (ejemplo: `http://<backend-host>:8082/api/optimizer/optimize`).
- Endpoint interno de Java que procesa la optimización: `POST /api/v1/optimize`.
- Content-Type: `application/json`.

Request (qué enviar)

Ejemplo de body que debe enviar el front:

```json
{
  "fleetId": "flota-pruebas-stress-01",
  "locations": [
    { "id": 1, "latitude": 4.6019, "longitude": -74.0722, "sequenceNumber": null },
    { "id": 2, "latitude": 4.6097, "longitude": -74.0817, "sequenceNumber": null }
  ]
}
```

- `fleetId` (string): identificador de la flota o la sesión.
- `locations` (array): lista de puntos con `id` (number), `latitude` (number), `longitude` (number), `sequenceNumber` (number|null).

Response (qué devuelve Java y qué llegará al front)

Ejemplo de respuesta válida (status 200):

```json
{
  "totalDistanceKm": 117.87,
  "optimizedOrder": [
    { "id": 1, "latitude": 4.6019, "longitude": -74.0722, "sequenceNumber": 0 },
    { "id": 19, "latitude": 4.5981, "longitude": -74.0758, "sequenceNumber": 1 }
  ]
}
```

- `totalDistanceKm` (number): distancia total optimizada en kilómetros.
- `optimizedOrder` (array): orden optimizado con `sequenceNumber` asignado (0..N-1).

Errores y códigos esperados

- 200 OK: respuesta con JSON mostrado arriba.
- 400 Bad Request: JSON inválido o campos faltantes. Mostrar mensaje y permitir reintento.
- 404 Not Found: endpoint incorrecto.
- 500 Internal Server Error: problemas al reenviar al microservicio Java (ej: `Connection refused`). El backend devuelve JSON con `{ message, details, timestamp }`.

Validaciones recomendadas en el front

- Validar `latitude` entre -90 y 90 y `longitude` entre -180 y 180.
- Validar que `locations` tenga al menos 1 elemento.
- Limitar el número de `locations` por petición (por ejemplo 200) para evitar peticiones que excedan recursos.

Headers y autenticación

- Si se usa JWT, enviar `Authorization: Bearer <token>` al backend. Si quieres que el backend reenvíe la cabecera `Authorization` a Java, hay que implementar explícitamente el reenvío de headers en `OptimizerController`.

Cómo probar desde la terminal (curl)

- Directo a Java (útil para depuración):

```bash
curl -i -X POST http://localhost:8081/api/v1/optimize \
  -H "Content-Type: application/json" \
  -d '{"fleetId":"test","locations":[{"id":1,"latitude":4.6019,"longitude":-74.0722,"sequenceNumber":null}]}'
```

- Vía backend (ruta que el front debe usar):

```bash
curl -i -X POST http://localhost:8082/api/optimizer/optimize \
  -H "Content-Type: application/json" \
  -d '{"fleetId":"test","locations":[{"id":1,"latitude":4.6019,"longitude":-74.0722,"sequenceNumber":null}]}'
```

Documentos auxiliares

- `schema/optimizer-request.json` (JSON Schema para validar request en front/back) 
- `schema/optimizer-response.json` (JSON Schema para response)

Si quieres, puedo generar también:
- Interfaces TypeScript para el front (listo para consumir en un proyecto React/Angular/Vue).
- Un archivo OpenAPI/Swagger que documente el endpoint del backend.

---

Fecha: 2025-12-12

