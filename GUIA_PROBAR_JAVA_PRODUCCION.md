# 🚀 Guía Completa: Probar Java (Route Optimizer) en Producción

## 📌 Resumen

El servicio Java (Route Optimizer) se comunica con el backend C# a través de **RabbitMQ**. No tiene endpoints HTTP directos que puedas llamar. Se activa automáticamente cuando se crean pedidos en el sistema.

---

## 🏗️ Arquitectura

```
┌─────────────────────────────────────┐
│   Cliente (Web o Mobile)            │
└────────────────┬────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────┐
│   Backend C# (.NET 8)               │
│  - Recibe pedidos del cliente       │
│  - Almacena en PostgreSQL           │
│  - Envía trabajo a RabbitMQ         │
└────────────────┬────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────┐
│   RabbitMQ (Message Queue)          │
│  - Cola: pc_commands                │
│  - Almacena trabajos pendientes     │
└────────────────┬────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────┐
│   Java Optimizer (SpringBoot)       │
│  - Consume mensajes de RabbitMQ     │
│  - Calcula rutas optimizadas        │
│  - Devuelve resultados              │
└─────────────────────────────────────┘
```

---

## 🔧 Verificar que Java está Corriendo

### Opción 1: Verificar Docker

**En el servidor de producción:**

```bash
# SSH al servidor
ssh user@lujuria.crudzaso.com

# Ver containers activos
docker ps

# Buscar el container Java
docker ps | grep apex_java

# Resultado esperado:
# 7a8f9c1e2d3b  apex_java  "java -jar app.jar"  2 weeks ago  Up 5 days  8081/tcp
```

### Opción 2: Ver Logs en Tiempo Real

```bash
# Ver los últimos 50 líneas
docker logs apex_java -n 50

# Ver logs en vivo (última línea)
docker logs apex_java -f

# Esperar CTRL+C para salir
```

**Logs esperados:**
```
2025-12-04 14:30:00 INFO  RouteOptimizer started successfully
2025-12-04 14:30:01 INFO  Listening on port 8081
2025-12-04 14:30:02 INFO  Connected to RabbitMQ at rabbitmq.lujuria.crudzaso.com:5672
2025-12-04 14:30:03 INFO  Consumer ready for queue: pc_commands
```

### Opción 3: Verificar Conexión a RabbitMQ

```bash
# Entrar al container Java
docker exec -it apex_java bash

# Verificar conectividad
curl -v amqp://admin:Kj9#mP2$qR5@vX8&@rabbitmq.lujuria.crudzaso.com:5672/

# O con telnet
telnet rabbitmq.lujuria.crudzaso.com 5672
```

---

## 📨 Monitorear RabbitMQ

### Acceder a RabbitMQ Management

**URL:** `https://rabbitmq.lujuria.crudzaso.com`  
**Usuario:** `admin`  
**Contraseña:** `Kj9#mP2$qR5@vX8&`

### En el Dashboard:

1. Ir a **Queues and Streams**
2. Buscar la cola `pc_commands`
3. Ver:
   - **Ready**: Mensajes esperando ser procesados
   - **Unacked**: Mensajes siendo procesados
   - **Total**: Total de mensajes

### Inspeccionar Mensajes

1. Click en la cola `pc_commands`
2. Click en **Get messages**
3. Seleccionar **Ack (requeue)**
4. Ver el contenido del mensaje JSON

---

## 🧪 Probar Java - Flujo Completo

### Paso 1: Crear un Pedido (Trigger para Java)

```bash
# 1. Login como Admin
TOKEN=$(curl -s -X POST https://service.lujuria.crudzaso.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email":"admin@apexvision.com",
    "password":"Admin123!"
  }' | jq -r '.token')

echo "Token: $TOKEN"

# 2. Crear un pedido
ORDER=$(curl -s -X POST https://service.lujuria.crudzaso.com/api/Orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "address": "Medellín Centro",
    "latitude": 6.2442,
    "longitude": -75.5898,
    "description": "Entrega urgente",
    "requiresEvidence": false
  }')

echo "Pedido creado: $ORDER"
ORDER_ID=$(echo $ORDER | jq -r '.orderId')
echo "Order ID: $ORDER_ID"

# 3. Ir a RabbitMQ y verificar que hay un nuevo mensaje en pc_commands
```

### Paso 2: Verificar en RabbitMQ

1. Abrir `https://rabbitmq.lujuria.crudzaso.com`
2. Ir a **Queues and Streams**
3. Hacer click en `pc_commands`
4. Deberías ver un nuevo mensaje
5. Click en **Get messages** con **Ack mode = Nack, requeue if possible**
6. Ver el JSON con los detalles del pedido

### Paso 3: Verificar Logs de Java

```bash
docker logs apex_java -f

# Deberías ver algo como:
# 2025-12-04 14:30:45 INFO  Received order for optimization: [123]
# 2025-12-04 14:30:46 INFO  Starting route calculation...
# 2025-12-04 14:30:47 INFO  Route optimization completed. Score: 85.5
# 2025-12-04 14:30:48 INFO  Message acknowledged
```

---

## 🔍 Debugging Común

### Problema: "Java container no está corriendo"

```bash
# Reiniciar Java
docker restart apex_java

# O si no existe el container, recrearlo
docker-compose up -d apex_java

# Verificar
docker ps | grep apex_java
```

### Problema: "RabbitMQ connection refused"

```bash
# Verificar RabbitMQ está corriendo
docker ps | grep rabbitmq

# Verificar conectividad
docker exec apex_java ping rabbitmq.lujuria.crudzaso.com

# Ver logs de RabbitMQ
docker logs rabbitmq -f
```

### Problema: "Queue pc_commands no existe"

```bash
# Entrar a RabbitMQ Admin
# Ir a Queues and Streams
# Crear manualmente:
# Name: pc_commands
# Durable: ✓
# Auto delete: ✗
```

### Problema: "Java recibe mensaje pero no procesa"

```bash
# Ver logs detallados
docker logs apex_java -f --since 5m

# Verificar que el algoritmo está funcionando
# Revisa el código en: ApexVisionJava/src/main/java/com/apexvision/optimizer/
```

---

## 📊 Monitoreo y Métricas

### Variables a Monitorear

**En RabbitMQ:**
- Ready messages: Debe bajar cuando Java procesa
- Unacked: Debe volver a 0 cuando Java termina
- Total processed: Debe crecer en el tiempo

**En Java:**
```bash
# Ver uso de memoria
docker stats apex_java

# Resultado esperado:
# CONTAINER  CPU %  MEM USAGE / LIMIT
# apex_java  2.5%   512MB / 650MB
```

**En Logs:**
```bash
# Contador de órdenes procesadas
docker logs apex_java | grep "Route optimization" | wc -l

# Órdenes fallidas
docker logs apex_java | grep "ERROR\|Exception" | wc -l
```

---

## 📈 Optimizaciones y Escalado

### Aumentar Recursos de Java

En `docker-compose.yml`:
```yaml
apex-java:
  deploy:
    resources:
      limits:
        memory: 1G        # Aumentar de 650M
        cpus: '1.5'       # Aumentar de 0.80
```

Luego:
```bash
docker-compose up -d
```

### Aumentar Workers de RabbitMQ

En el código Java (application.properties):
```properties
spring.rabbitmq.listener.simple.concurrency=5
spring.rabbitmq.listener.simple.max-concurrency=10
```

---

## 🚀 Despliegue Completo de Java

### Si necesitas actualizar el código Java:

```bash
# 1. Hacer cambios en código Java local
# 2. Compilar y crear JAR
mvn clean package

# 3. Crear Docker image
docker build -t apex_java:latest .

# 4. En servidor, actualizar
docker pull apex_java:latest  # Si está en registry
docker-compose up -d apex_java

# 5. Verificar
docker logs apex_java -f
```

---

## 📋 Checklist de Verificación

- [ ] Java container está corriendo: `docker ps | grep apex_java`
- [ ] Java se conectó a RabbitMQ: Ver en logs "Connected to RabbitMQ"
- [ ] Cola `pc_commands` existe en RabbitMQ
- [ ] Crear un pedido y verificar que aparece en RabbitMQ
- [ ] Java procesa el mensaje: Ver en logs "Route optimization"
- [ ] Mensaje desaparece de RabbitMQ después de procesar
- [ ] Usar `docker stats` para verificar recursos
- [ ] No hay errores en logs: `docker logs apex_java | grep ERROR`

---

## 📞 Solución de Problemas Contacto

Si Java no funciona:

1. **Verificar logs:** `docker logs apex_java -f`
2. **Revisar RabbitMQ:** `https://rabbitmq.lujuria.crudzaso.com`
3. **Revisar recursos:** `docker stats apex_java`
4. **Reiniciar:** `docker restart apex_java`
5. **Si nada funciona:** Contactar al equipo DevOps

---

## 📚 Referencias

- **RabbitMQ Management:** https://rabbitmq.lujuria.crudzaso.com
- **Docker:** `docker-compose.yml` en raíz del proyecto
- **Java Code:** `proyecto-logistica-backend-java/`
- **C# Integration:** `ApexVision.Backend/Services/OptimizationService.cs`

---

Última actualización: 4 de Diciembre de 2025

