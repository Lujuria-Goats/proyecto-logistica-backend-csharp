Guía para el Front: Integración con el servicio de optimización de rutas

Resumen rápido

Este documento explica cómo el front debe comunicarse con el backend para optimizar rutas, qué JSON enviar y qué esperar en la respuesta, ejemplos listos (curl, fetch, axios), manejo de errores y pruebas locales.

Endpoints relevantes

- Endpoint que debe llamar el front (proxy .NET):
  POST http://<BACKEND_HOST>:8082/api/optimizer/optimize
  - Ejemplo local: http://localhost:8082/api/optimizer/optimize

- Endpoint interno del microservicio Java (no usarlo desde el front; es el destino al que el backend reenvía):
  POST http://<JAVA_HOST>:8081/api/v1/optimize
  - Ejemplo local: http://localhost:8081/api/v1/optimize

Por qué llamar al proxy (.NET)

- El front debe llamar siempre al backend (.NET) porque ahí están las reglas de negocio, autenticación, CORS y logs centralizados.
- El backend reenvía la petición al microservicio Java y devuelve la respuesta al front.

Contrato: Request (qué debe enviar el front)

Content-Type: application/json

Estructura JSON recomendada (obligatorio enviar al menos 1 ubicación):

{
  "fleetId": "string",            // obligatorio
  "locations": [                   // obligatorio
    {
      "id": number,               // obligatorio
      "latitude": number,         // obligatorio
      "longitude": number,        // obligatorio
      "sequenceNumber": number | null // opcional, null si aún no existe
    },
    ...
  ],
  "settings": { }                 // opcional: parámetros de optimización (si se soportan)
}

Notas de validación en front
- latitude: -90 <= latitude <= 90
- longitude: -180 <= longitude <= 180
- id: entero único por petición (ideal)
- Limitar cantidad de locations por petición (ej. 200) para evitar peticiones muy pesadas

Contrato: Response (qué recibirá el front)

Respuesta 200 OK (ejemplo):

{
  "totalDistanceKm": 117.87,
  "optimizedOrder": [
    { "id": 1, "latitude": 4.6019, "longitude": -74.0722, "sequenceNumber": 0 },
    { "id": 19, "latitude": 4.5981, "longitude": -74.0758, "sequenceNumber": 1 },
    ...
  ]
}

- totalDistanceKm: número (km)
- optimizedOrder: array ordenado con sequenceNumber asignado (0..N-1)

Formato de errores (lo que puede devolver el backend)

- 200 OK: respuesta válida (JSON)
- 400 Bad Request: JSON inválido o validación fallida
- 404 Not Found: ruta incorrecta (ver endpoint)
- 500 Internal Server Error: error en el backend o en la comunicación con Java
  Ejemplo de body en 500 (backend .NET):
  { "message": "Internal server error.", "details": "Connection refused (apex-java:8080)", "timestamp": "..." }

Recomendaciones en el front sobre manejo de errores
- Mostrar mensaje amigable y permitir reintento.
- Si el backend devuelve `details` en 500, mostrarlo solo en ambiente debug. Para el usuario, mostrar "Error en el servidor, inténtalo de nuevo".
- Guardar o reportar los logs de fallo a Sentry/Logger si la app lo tiene.

Ejemplos prácticos

Curl (probar en local)

1) Llamar al Java (para debug; usualmente usar backend):

```bash
curl -s -X POST http://localhost:8081/api/v1/optimize \
  -H "Content-Type: application/json" \
  -d '{"fleetId":"test","locations":[{"id":1,"latitude":4.6019,"longitude":-74.0722,"sequenceNumber":null}]}' \
  | jq .
```

2) Llamar al backend (lo que debe usar el front):

```bash
curl -s -X POST http://localhost:8082/api/optimizer/optimize \
  -H "Content-Type: application/json" \
  -d '{"fleetId":"test","locations":[{"id":1,"latitude":4.6019,"longitude":-74.0722,"sequenceNumber":null}]}' \
  | jq .
```

Fetch (vanilla JS)

```javascript
const payload = {
  fleetId: 'flota-pruebas-stress-01',
  locations: [ { id:1, latitude:4.6019, longitude:-74.0722, sequenceNumber: null } ]
};

fetch('http://localhost:8082/api/optimizer/optimize', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(payload)
})
  .then(async res => {
    if (!res.ok) {
      const txt = await res.text();
      throw new Error(`Error ${res.status}: ${txt}`);
    }
    return res.json();
  })
  .then(data => console.log('Optimized:', data))
  .catch(err => console.error('Optimize error', err));
```

Axios (TypeScript)

```ts
import axios from 'axios';

const payload = { fleetId: 'test', locations: [{ id:1, latitude:4.6019, longitude:-74.0722, sequenceNumber:null }] };

axios.post('http://localhost:8082/api/optimizer/optimize', payload)
  .then(r => console.log(r.data))
  .catch(e => console.error(e.response?.data || e.message));
```

Integración con TypeScript (types)

Archivo ya generado: `docs/types/optimizer.d.ts` (usa esas interfaces). Ejemplo mínimo:

```ts
export interface LocationInput { id:number; latitude:number; longitude:number; sequenceNumber:number|null }
export interface OptimizerRequest { fleetId:string; locations: LocationInput[] }
export interface OptimizerResponse { totalDistanceKm:number; optimizedOrder: Array<{id:number;latitude:number;longitude:number;sequenceNumber:number}> }
```

CORS y Autenticación

- CORS: si el front se sirve desde otro dominio/puerto, el backend debe habilitar CORS permitiendo el origen del front. Pide al equipo backend que agregue el policy con el origen del front.
- Authorization: si el front manda JWT en `Authorization: Bearer <token>` y Java lo necesita, por defecto el proxy no reenvía headers. Se puede cambiar `OptimizerController` para añadir:

```csharp
if (Request.Headers.TryGetValue("Authorization", out var authHeader))
{
    forwardRequest.Headers.TryAddWithoutValidation("Authorization", (string)authHeader);
}
```

Esto hace que Java reciba el token.

Validaciones y UX

- Bloquear el botón "Optimizar" mientras la petición está en curso.
- Mostrar un spinner y el progreso.
- Si la respuesta llega con `optimizedOrder`, actualizar la UI y asignar el `sequenceNumber` a cada parada.
- Mostrar la `totalDistanceKm` si la API la devuelve.

Pruebas end-to-end (rápido)

1) Asegurate que Docker esté levantado: `docker ps` y que `apex_backend` y `apex_java` estén `Up`.
2) Probar Java directo: `curl` al puerto 8081 (ver sección Curl).
3) Probar proxy: `curl` al puerto 8082 (ver sección Curl).
4) Si el proxy falla con `Connection refused (apex-java:8080)`, verificar `docker inspect` y la red de Docker según la guía del repo.

JSON Schema y validación automática

- Usa `docs/schema/optimizer-request.json` para validar la petición en el front antes de enviarla.
- Usa `docs/schema/optimizer-response.json` para validar la respuesta (útil en pruebas E2E).

Postman / Colección

- Recomiendo crear una colección Postman con 3 requests: (1) Java directo, (2) Proxy backend, (3) Prueba con varias ubicaciones. Incluye ejemplos de payload y guardalas en el repo como `postman/Optimizer.postman_collection.json` si querés que lo genere.

Checklist mínimo antes de integrar en producción

- [ ] Validar CORS y orígenes permitidos
- [ ] Validar y reenviar Authorization si es necesario
- [ ] Limitar tamaño del payload (max locations)
- [ ] Tests E2E con curl/Postman en CI

Soporte / Contacto

Si querés, genero la colección Postman y agrego el snippet para reenviar Authorization en `OptimizerController` y hago commit en `dev`. ¿Qué preferís que haga ahora?

---

Fecha: 2025-12-12

## Integración específica para Vue.js (Vue 3 + Composition API + TypeScript)

A continuación tienes ejemplos listos para integrar el endpoint de optimización desde una aplicación Vue 3 con TypeScript.

1) Dependencias recomendadas

- axios (HTTP client)
- opcional: ajv o yup para validar JSON según `docs/schema/optimizer-request.json`

Instalación (en tu proyecto frontend):

```bash
npm install axios
# opcional: npm install ajv
```

2) Tipos (copiar o importar a tu proyecto)

Puedes copiar `docs/types/optimizer.d.ts` al directorio `src/types/` de tu frontend o importarlo si el frontend comparte el repo.

3) Composable: `src/composables/useOptimizer.ts`

```ts
import { ref } from 'vue'
import axios from 'axios'
import type { OptimizerRequest, OptimizerResponse } from '@/types/optimizer'

export function useOptimizer() {
  const loading = ref(false)
  const error = ref<string | null>(null)
  const data = ref<OptimizerResponse | null>(null)

  async function optimize(req: OptimizerRequest, token?: string) {
    loading.value = true
    error.value = null
    data.value = null
    try {
      const headers: Record<string, string> = { 'Content-Type': 'application/json' }
      if (token) headers['Authorization'] = `Bearer ${token}`

      const resp = await axios.post<OptimizerResponse>('http://localhost:8082/api/optimizer/optimize', req, { headers })
      data.value = resp.data
      return resp.data
    } catch (e: any) {
      // captura errores del servidor y del cliente
      error.value = e.response?.data?.message ?? e.message ?? 'Error desconocido'
      throw e
    } finally {
      loading.value = false
    }
  }

  return { loading, error, data, optimize }
}
```

Notas:
- Si el backend requiere que el proxy reenvíe `Authorization`, asegúrate de enviar el token (el proxy puede reenviarlo a Java si se implementa).

4) Componente de ejemplo `src/components/OptimizeButton.vue`

```vue
<template>
  <div>
    <button :disabled="loading" @click="onOptimize">Optimizar ruta</button>
    <div v-if="loading">Optimizando...</div>
    <pre v-if="result">{{ result }}</pre>
    <div v-if="error" class="error">{{ error }}</div>
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue'
import { useOptimizer } from '@/composables/useOptimizer'
import type { OptimizerRequest } from '@/types/optimizer'

const { loading, error, data, optimize } = useOptimizer()

const payload: OptimizerRequest = reactive({
  fleetId: 'flota-pruebas-stress-01',
  locations: [ { id: 1, latitude: 4.6019, longitude: -74.0722, sequenceNumber: null } ]
})

async function onOptimize() {
  try {
    const res = await optimize(payload /*, token if needed */)
    console.log('Optimized response', res)
  } catch (e) {
    console.error('Failed optimize', e)
  }
}

const result = data
</script>

<style scoped>
.error { color: red }
</style>
```

5) Validación básica en el front (antes de enviar)

```ts
function validateLocations(locations: Array<{ id:number; latitude:number; longitude:number }>) {
  if (!Array.isArray(locations) || locations.length === 0) return 'Debe haber al menos una ubicación'
  for (const loc of locations) {
    if (typeof loc.id !== 'number') return 'id inválido'
    if (typeof loc.latitude !== 'number' || loc.latitude < -90 || loc.latitude > 90) return 'latitude inválida'
    if (typeof loc.longitude !== 'number' || loc.longitude < -180 || loc.longitude > 180) return 'longitude inválida'
  }
  return null
}
```

6) Configurar proxy en desarrollo (Vite)

Si tu front corre en `localhost:3000` y quieres evitar CORS en desarrollo, añade proxy en `vite.config.ts`:

```ts
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:8082',
        changeOrigin: true,
        secure: false,
      }
    }
  }
})
```

Con esto puedes usar rutas relativas en el front: `fetch('/api/optimizer/optimize', ...)` y en desarrollo Vite las reenviará a `http://localhost:8082`.

7) Mostrar la ruta optimizada en UI / mapa

- Una vez tengas `optimizedOrder` en `data.value.optimizedOrder`, puedes reordenar la lista de paradas en UI por `sequenceNumber` y dibujar la ruta en el mapa (Leaflet, Mapbox, Google Maps).
- Ejemplo rápido:

```ts
const ordered = [...data.value.optimizedOrder].sort((a,b)=>a.sequenceNumber-b.sequenceNumber)
// usar ordered para render o para pasar a la librería de mapas
```

8) Tests rápidos (curl)

- Java directo (debug):

```bash
curl -s -X POST http://localhost:8081/api/v1/optimize \
  -H "Content-Type: application/json" \
  -d '{"fleetId":"test","locations":[{"id":1,"latitude":4.6019,"longitude":-74.0722,"sequenceNumber":null}]}' \
  | jq .
```

- Proxy (.NET) — lo que usará el front en producción:

```bash
curl -s -X POST http://localhost:8082/api/optimizer/optimize \
  -H "Content-Type: application/json" \
  -d '{"fleetId":"test","locations":[{"id":1,"latitude":4.6019,"longitude":-74.0722,"sequenceNumber":null}]}' \
  | jq .
```

9) Reenvío del header Authorization (si usas JWT)

Enviar el header desde el front:

```ts
// ejemplo axios
axios.post('/api/optimizer/optimize', payload, { headers: { Authorization: `Bearer ${token}` } })
```

Y asegurarte que el backend (`OptimizerController`) reenvíe la cabecera a Java como ya se discutió.

---

He agregado esta sección práctica al final de `docs/FRONTEND_GUIDE_OPTIMIZER.md` para que el equipo frontend (Vue) pueda copiar y usar los snippets directamente. Si querés, ahora:

- A) Hago commit y push de este cambio en `dev`,
- B) Genero la colección Postman automatizada,
- C) Agrego el snippet en `OptimizerController` para reenviar `Authorization` y lo commiteo,
- D) Habilito CORS en el backend y commiteo.

Decime la letra (A/B/C/D) y lo hago.
