Guía práctica para la App móvil (Driver)

Resumen rápido

Esta guía explica cómo la app móvil (React Native, Android nativo, iOS nativo o cualquier cliente móvil) debe integrarse con el backend de ApexVision para:
- subir evidencia fotográfica (Cloudinary, vía backend),
- confirmar/completar pedidos,
- traer rutas y rutas guardadas,
- guardar/cargar/eliminar rutas guardadas,
- optimizar rutas (disparar al servicio Java vía el proxy .NET).

Requisitos previos

- Tener un token JWT válido (obtenido en login) para llamar endpoints protegidos.
- La app nunca debe contener las claves secretas de Cloudinary. El backend hace el upload a Cloudinary.
- En desarrollo, puedes usar `http://localhost:8082` para el backend (o el host que corresponda en el servidor).

Endpoints relevantes (resumen)

- POST /api/Files/upload
  - Descripción: subir una foto al backend (y backend la sube a Cloudinary).
  - Auth: Authorization: Bearer <JWT>
  - Body: multipart/form-data con campo `file`
  - Respuesta: 200 OK -> FileUploadResponse { success, url, publicId, message }
  - Validación en servidor: extensiones [.jpg, .jpeg, .png, .gif, .webp], tamaño máx 5 MB.

- DELETE /api/Files/{publicId}
  - Descripción: eliminar recurso subido (Cloudinary) por publicId
  - Auth: Authorization

- POST /api/Orders/{id}/complete
  - Descripción: marcar pedido como completado; si la orden requiere evidencia envía `file` en multipart
  - Auth: Authorization (DriverOnly)
  - Body: multipart/form-data -> campo `file` (requerido si Order.RequiresEvidence = true)
  - Respuesta: 200 OK { message }

- GET /api/Orders/my-route
  - Descripción: traer pedidos asignados al conductor (ruta actual)
  - Auth: Authorization (DriverOnly)
  - Respuesta: 200 OK -> lista de `OrderDto` con lat/lon, address, evidenceUrl, etc.

- POST /api/Routes/save
  - Descripción: guardar una ruta personalizada (routeName + orderIds[]). DriverOnly.
  - Body: JSON { routeName, orderIds }

- GET /api/Routes/saved
  - Descripción: listar rutas guardadas del driver
  - Auth: Authorization

- POST /api/Routes/saved/{routeId}/load
  - Descripción: marcar ruta como usada y devolver pedidos asociados
  - Auth: Authorization

- POST /api/optimizer/optimize  (proxy .NET -> Java optimizer)
  - Descripción: optimiza un conjunto de ubicaciones
  - Body: JSON { fleetId, locations: [{ id, latitude, longitude, sequenceNumber }] }
  - Auth: opcional según front/back (si el front llama al backend, incluye Authorization si procede)
  - Respuesta: { totalDistanceKm, optimizedOrder: [...] }

Flujos recomendados (mobile)

A) Confirmar entrega con evidencia (recomendado — un solo paso)
1. El driver toma la foto desde la app.
2. Validar en la app: extensión y tamaño (<= 5 MB). Opcional: redimensionar/comprimir.
3. Enviar multipart/form-data a `POST /api/Orders/{id}/complete` con el archivo en campo `file` y header Authorization.
4. Backend sube a Cloudinary, valida con AI (si corresponde) y marca la orden como completada.
5. Mostrar resultado al usuario (OK o error con mensaje específico).

B) Subir foto a Cloudinary vía backend (dos pasos — sólo si necesitas almacenar antes)
1. Subir: POST /api/Files/upload -> recibe Url y PublicId.
2. (Si necesitas) llamar otro endpoint para relacionar esa Url con la orden (NO hay endpoint específico hoy — preferir Opción A).

Validaciones en la app (obligatorias antes de enviar)
- Extensión: .jpg, .jpeg, .png, .gif, .webp
- Tamaño: <= 5 MB (si excede, comprimir o rechazar)
- Lat/lon: lat en [-90, 90], lon en [-180, 180]
- IDs: integer, no vacío
- Para optimize: lista de locations no vacía

Ejemplos prácticos

1) Curl — completar orden con foto (prueba rápida desde Termius)

```bash
curl -i -X POST "http://localhost:8082/api/Orders/123/complete" \
  -H "Authorization: Bearer <JWT>" \
  -F "file=@/ruta/a/foto.jpg"
```

2) Curl — subir foto (FilesController)

```bash
curl -i -X POST "http://localhost:8082/api/Files/upload" \
  -H "Authorization: Bearer <JWT>" \
  -F "file=@/ruta/a/foto.jpg"
```

3) React Native — subir y completar con `fetch` (ejemplo)

```js
// suponer que obtienes fileUri con react-native-image-picker
async function completeOrderWithImage(orderId, fileUri, fileName, token) {
  const formData = new FormData();
  formData.append('file', { uri: fileUri, name: fileName || 'photo.jpg', type: 'image/jpeg' });

  const res = await fetch(`http://<API_HOST>:8082/api/Orders/${orderId}/complete`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
      // no establecer Content-Type: multipart; fetch lo hace con boundary
    },
    body: formData
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Error ${res.status}: ${text}`);
  }
  return res.json ? await res.json() : { message: 'OK' };
}
```

4) React Native con `axios` y progreso (ejemplo)

```js
import axios from 'axios';

async function uploadWithProgress(orderId, fileObj, token, onProgress) {
  const formData = new FormData();
  formData.append('file', fileObj); // fileObj: { uri, name, type }

  const resp = await axios.post(`http://<API_HOST>:8082/api/Orders/${orderId}/complete`, formData, {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'multipart/form-data'
    },
    onUploadProgress: (progressEvent) => {
      const percent = Math.round((progressEvent.loaded * 100) / progressEvent.total);
      onProgress(percent);
    }
  });
  return resp.data;
}
```

5) Obtener la ruta del driver (fetch)

```js
async function getMyRoute(token) {
  const res = await fetch('http://<API_HOST>:8082/api/Orders/my-route', {
    headers: { Authorization: `Bearer ${token}` }
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json();
}
```

Manejo de errores (qué mostrar al usuario)

- 400 Bad Request: mostrar el mensaje que viene del servidor (e.g. "Evidence file is required for this order.")
- 401 Unauthorized: pedir re-login o refrescar token
- 404 Not Found: "Orden no encontrada"
- 500 Internal Server Error / 503 Service Unavailable: "Error en el servidor, inténtalo más tarde"
- En caso de errores de validación de imagen, mostrar consejo claro: "La foto debe ser JPG/PNG y menor a 5MB"

Buenas prácticas y recomendaciones

- Comprimir / redimensionar imágenes en el móvil antes de subir (para ahorrar datos y acelerar). Por ejemplo: 1024px de ancho y calidad 80% suele ser suficiente.
- Mostrar barra de progreso y bloquear acciones duplicadas.
- Retry/backoff: si la subida falla por red, reintentar 2 veces con backoff exponencial.
- Guardar evidencia localmente si el upload falla, y reintentar en segundo plano cuando haya conexión.

Seguridad

- Nunca almacenar Cloudinary API secret en la app móvil.
- Envío de imágenes siempre por HTTPS en producción.
- Validar tokens y permisos en backend; la app sólo envía token en Authorization header.

Debugging y pruebas (en Termius o CI)

- Asegúrate que `apex_backend` está arriba: `docker ps | grep apex_backend`
- Probar endpoints con curl (ejemplos arriba)
- Si obtienes `Connection refused (apex_java:8080)` al usar optimize vía proxy, el backend no puede resolver al servicio Java (revisar docker network y variable `JavaOptimizationApi__BaseUrl`)

Postman / colección

Puedo generar una colección Postman con las siguientes requests listos para el equipo móvil:
- POST /api/Files/upload (form-data)
- POST /api/Orders/{id}/complete (form-data)
- GET /api/Orders/my-route
- POST /api/Routes/save
- POST /api/optimizer/optimize

¿Querés que genere la colección Postman y la suba a `postman/Optimizer_Mobile.postman_collection.json` en el repo? (responde S/N)

Ejemplo de flujo de UX detallado

1) Pantalla de "Orden" con botón "Completar". Si `RequiresEvidence == true` mostrar "Tomar foto" y un mensaje explicativo.
2) Al tocar "Tomar foto" abrir cámara; tras captura, mostrar preview y botón "Subir y completar".
3) Validar local y llamar `POST /api/Orders/{id}/complete` con FormData. Mostrar progreso. Al éxito, mostrar "Orden completada" y refrescar lista de órdenes.
4) Si falla validación AI, mostrar el mensaje que devuelva el backend; permitir reintentar o adjuntar otra foto.

Soporte y siguientes pasos

Puedo generar:
- A) Ejemplo completo en React Native (component + integración con react-native-image-picker + upload + UI states),
- B) Colección Postman para QA y pruebas manuales,
- C) Un README corto con checklists para QA.

Decime cuál (A/B/C) querés que implemente ahora y lo creo en el repo.

Fecha: 2025-12-12

