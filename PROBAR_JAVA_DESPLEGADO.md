# 🚀 Guía para Probar Java Desplegado

## ✅ Estado: Java está FUNCIONANDO

```bash
curl http://localhost:8081/actuator/health
# Respuesta: {"status":"UP"}
```

---

## 🧪 Probar Optimización de Rutas

### Endpoint:
```
POST http://localhost:8081/api/optimize
```

### Request JSON:
```bash
curl -X POST http://localhost:8081/api/optimize \
  -H "Content-Type: application/json" \
  -d '{
  "fleetId": "flota-pruebas-01",
  "locations": [
    {"id": 1, "latitude": 4.6019, "longitude": -74.0722, "sequenceNumber": null},
    {"id": 2, "latitude": 4.7109, "longitude": -74.0719, "sequenceNumber": null},
    {"id": 3, "latitude": 4.5930, "longitude": -74.1396, "sequenceNumber": null},
    {"id": 4, "latitude": 4.6486, "longitude": -74.2482, "sequenceNumber": null},
    {"id": 5, "latitude": 4.6974, "longitude": -74.0396, "sequenceNumber": null}
  ]
}'
```

### Respuesta Esperada:
```json
{
  "totalDistanceKm": 45.23,
  "optimizedOrder": [
    {"id": 1, "latitude": 4.6019, "longitude": -74.0722, "sequenceNumber": 0},
    {"id": 5, "latitude": 4.6974, "longitude": -74.0396, "sequenceNumber": 1},
    {"id": 2, "latitude": 4.7109, "longitude": -74.0719, "sequenceNumber": 2},
    {"id": 3, "latitude": 4.5930, "longitude": -74.1396, "sequenceNumber": 3},
    {"id": 4, "latitude": 4.6486, "longitude": -74.2482, "sequenceNumber": 4}
  ]
}
```

### ✅ Cómo saber que funciona:
1. **sequenceNumber** cambia de `null` a números ordenados (0, 1, 2...)
2. **optimizedOrder** devuelve las ubicaciones ordenadas por distancia óptima
3. **totalDistanceKm** muestra la distancia total en kilómetros

---

## 🔗 URLs de Producción

| Servicio | URL | Estado |
|----------|-----|--------|
| **Java Health** | http://91.99.89.10:8081/actuator/health | ✅ UP |
| **Java Actuator** | http://91.99.89.10:8081/actuator | ✅ |
| **C# Swagger** | https://service.lujuria.crudzaso.com/swagger | ✅ |
| **C# Backend** | http://91.99.89.10:8082 | ✅ |
| **RabbitMQ Admin** | http://91.99.89.10:15672 | ✅ |
| **PostgreSQL** | 91.99.89.10:5432 | ✅ |

---

## 🧪 Comandos de Prueba (Termius)

### Health Check:
```bash
curl http://localhost:8081/actuator/health
```

### Ver endpoints de actuator:
```bash
curl http://localhost:8081/actuator
```

### Ver logs:
```bash
docker logs apex_java --tail 50
```

### Logs en tiempo real:
```bash
docker logs -f apex_java
```

---

## 📊 Estado de Contenedores

```
NAMES           PORTS                    ESTADO
apex_backend    0.0.0.0:8082->8080/tcp   ✅ C# Backend
apex_java       0.0.0.0:8081->8080/tcp   ✅ Java Backend
apex_db         0.0.0.0:5432->5432/tcp   ✅ PostgreSQL
apex_proxy      0.0.0.0:80,443           ✅ Nginx Proxy
apex_rabbit     0.0.0.0:5672,15672       ✅ RabbitMQ
```

---

## 🔧 Comandos Útiles

### Reiniciar Java:
```bash
docker compose restart apex-java
```

### Reconstruir Java:
```bash
docker compose up -d --build apex-java
```

### Ver recursos:
```bash
docker stats apex_java --no-stream
```

---

## ⚙️ Variables de Entorno Java

```yaml
SPRING_APPLICATION_NAME=RouteOptimizer
SPRING_RABBITMQ_HOST=rabbitmq.lujuria.crudzaso.com
SPRING_RABBITMQ_PORT=5672
SPRING_RABBITMQ_USERNAME=admin
JAVA_TOOL_OPTIONS=-Xms256m -Xmx512m
```

---

## 🔄 Integración C# → Java

El backend C# llama a Java internamente:
```
http://apex-java:8080
```

---

**Verificado:** Diciembre 4, 2024 - Java UP ✅

