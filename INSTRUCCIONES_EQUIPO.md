# 📋 Instrucciones para el Equipo ApexVision

## 🎯 Estado Actual del Proyecto

✅ **Backend C# completamente funcional**
- ✅ 13 endpoints verificados
- ✅ Autenticación JWT implementada
- ✅ Autorización basada en roles (Admin/Driver)
- ✅ Base de datos PostgreSQL lista
- ✅ Integración con RabbitMQ
- ✅ Integración con Azure AI Vision

⏳ **Pendiente: Frontend Mobile (React Native/Flutter)**  
⏳ **Pendiente: Despliegue completo de Java (Route Optimizer)**

---

## 👨‍💻 Instrucciones por Rol

### 1. FRONTEND (James)

#### 📱 Qué falta:
- [ ] Implementar pantalla de login (Admin y Driver)
- [ ] Implementar pantalla de creación de pedidos (Admin)
- [ ] Implementar mapa para ver pedidos
- [ ] Implementar pantalla de rutas guardadas (Driver)
- [ ] Implementar pantalla para completar entregas con foto

#### 🔌 Endpoints que necesitas usar:

**Login:**
```
POST /api/auth/login
Body: { email, password }
Response: { token }
```

**Crear Pedido (Admin):**
```
POST /api/Orders
Headers: Authorization: Bearer {token}
Body: { address, latitude, longitude, description, requiresEvidence }
```

**Ver Pedidos del Driver:**
```
GET /api/Orders/my-route
Headers: Authorization: Bearer {token}
```

**Guardar Ruta:**
```
POST /api/Routes/save
Headers: Authorization: Bearer {token}
Body: { routeName, orderIds: [1,2,3] }
```

**Ver Rutas Guardadas:**
```
GET /api/Routes/saved
Headers: Authorization: Bearer {token}
```

**Completar Entrega:**
```
POST /api/Orders/{orderId}/complete
Headers: Authorization: Bearer {token}
Body: FormData con file (opcional si requiresEvidence=true)
```

#### 📚 Documentación Completa:
Ver `DOCUMENTACION_ENDPOINTS_COMPLETA.md`

#### 🧪 Testing Rápido:
```bash
# En local
http://localhost:5132/swagger

# En producción
https://service.lujuria.crudzaso.com/swagger
```

---

### 2. BACKEND JAVA (Route Optimizer)

#### 📦 Qué está actualmente:
- ✅ Java SpringBoot está compilado
- ✅ Docker image creado
- ✅ Container en producción corriendo
- ⏳ Necesita verificación de funcionalidad completa

#### 🔌 Endpoints Java (Internos - Queue):

El microservicio Java se comunica con C# a través de **RabbitMQ**. No tiene endpoints HTTP directos en desarrollo local.

**Flujo de comunicación:**
```
C# Backend → RabbitMQ → Java Optimizer
            ↓
        Recibe órdenes
        ↓
        Calcula rutas optimizadas
        ↓
        Devuelve resultado
```

#### 🚀 Cómo probar Java en Producción:

**1. Verificar que está corriendo:**
```bash
# SSH al servidor
ssh user@lujuria.crudzaso.com

# Verificar containers
docker ps | grep apex

# Ver logs
docker logs apex_java -f
```

**2. Trigger una optimización desde C#:**
```bash
# Hacer un POST /api/Orders desde el cliente
# El C# automáticamente enviará el trabajo a Java via RabbitMQ
```

**3. Monitorear RabbitMQ:**
```
URL: https://rabbitmq.lujuria.crudzaso.com
Username: admin
Password: Kj9#mP2$qR5@vX8&
```

#### 📋 Checklist para Java:

- [ ] Verificar que el container Java está corriendo en Docker
- [ ] Revisar logs de Java en producción
- [ ] Probar que recibe mensajes de RabbitMQ
- [ ] Validar que calcula rutas correctamente
- [ ] Optimizar algoritmo de rutas si es necesario

#### 📚 Variables de Entorno Java:

```properties
SPRING_APPLICATION_NAME=RouteOptimizer
LOGGING_LEVEL_COM_APEXVISION_OPTIMIZER=INFO
SPRING_RABBITMQ_HOST=rabbitmq.lujuria.crudzaso.com
SPRING_RABBITMQ_PORT=5672
SPRING_RABBITMQ_USERNAME=admin
SPRING_RABBITMQ_PASSWORD=Kj9#mP2$qR5@vX8&
SPRING_RABBITMQ_VIRTUAL_HOST=/
MYAPP_RABBITMQ_QUEUE=pc_commands
```

---

### 3. DEVOPS / DEPLOYMENT

#### 📦 Estado Actual del Deploy:

**Stack Tecnológico:**
- **Backend C#:** .NET 8 en Docker
- **Base de Datos:** PostgreSQL 15
- **Cache/Queue:** RabbitMQ
- **Java Optimizer:** SpringBoot en Docker
- **Proxy:** Nginx Proxy Manager
- **Storage:** Cloudinary (imágenes)

**URLs en Producción:**
```
Frontend: https://service.lujuria.crudzaso.com
Backend C#: https://service.lujuria.crudzaso.com/api
RabbitMQ: https://rabbitmq.lujuria.crudzaso.com
```

#### 🔧 Verificar Deployments:

**1. C# Backend:**
```bash
# En servidor
docker logs apex_backend -f

# Endpoint de salud (agregar si no existe)
curl https://service.lujuria.crudzaso.com/api/health
```

**2. Java:**
```bash
docker logs apex_java -f
```

**3. PostgreSQL:**
```bash
docker exec apex_db psql -U root -d postgres -c "SELECT * FROM \"AspNetUsers\" LIMIT 1;"
```

**4. RabbitMQ:**
```bash
docker ps | grep rabbitmq
# Abrir en navegador: https://rabbitmq.lujuria.crudzaso.com
```

#### 🚀 Actualizar Backend en Producción:

```bash
# 1. Hacer push a rama dev
git add .
git commit -m "mensaje"
git push origin dev

# 2. En servidor, actualizar código
cd /path/to/apex-vision-deploy
git pull origin dev

# 3. Reconstruir y reiniciar
docker-compose up -d --build

# 4. Verificar logs
docker-compose logs -f apex_backend
```

#### 📊 Variables de Entorno para Producción:

Verificar que están configuradas en docker-compose.yml:
- `ConnectionStrings__DefaultConnection` → PostgreSQL
- `Jwt__Key` → Clave secreta (cambiar en producción)
- `Cloudinary__*` → Credenciales de almacenamiento
- `RabbitMQ__*` → Configuración de queue
- `AzureVisionSettings__*` → Clave de Azure AI

---

## 🧪 Testing Completo del Sistema

### Test Local (C#):
```powershell
# En C:\Users\user\OneDrive\Desktop\ApexVision
powershell -ExecutionPolicy Bypass -File "test-all.ps1"

# Resultado esperado:
# ✅ Exitosos: 13
# ❌ Fallos: 0
```

### Test en Producción:
```bash
# Login
curl -X POST https://service.lujuria.crudzaso.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@apexvision.com","password":"Admin123!"}'

# Crear Pedido
curl -X POST https://service.lujuria.crudzaso.com/api/Orders \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "address":"Test",
    "latitude":6.2,
    "longitude":-75.5,
    "description":"Test",
    "requiresEvidence":false
  }'
```

---

## 📝 Git Workflow

**Rama Principal:** `main` (producción)  
**Rama de Desarrollo:** `dev` (staging)  
**Ramas Feature:** `feature/nombre` (desarrollo)

**Flujo:**
```
feature/feature-name → dev → main (release)
```

**Comandos:**
```bash
# Crear feature
git checkout -b feature/nueva-feature

# Cuando termines
git add .
git commit -m "Feature: descripción"
git push origin feature/nueva-feature

# Luego hacer Pull Request a dev
# Cuando esté en dev, hacer merge a main
```

---

## 📞 Contactos y Responsabilidades

| Rol | Responsable | Tareas |
|-----|------------|--------|
| **Frontend Mobile** | James | Implementar UI, conectar endpoints |
| **Backend Java** | [Asignar] | Algoritmo de rutas, optimización |
| **DevOps** | [Asignar] | Deploy, monitoreo, bases de datos |
| **Backend C#** | [Actual] | APIs, autenticación, lógica de negocio |

---

## ✅ Checklist Final

- [x] Backend C# completamente funcional
- [x] 13 endpoints verificados
- [x] Autenticación JWT implementada
- [x] Base de datos migrada
- [x] RabbitMQ integrado
- [x] Docker compose configurado
- [ ] Frontend en desarrollo
- [ ] Java optimizer verificado en producción
- [ ] Testing E2E completado
- [ ] Documentación actualizada

---

## 🔐 Seguridad

**Importante para Producción:**

1. **Cambiar JWT Key:**
   - No usar la clave por defecto
   - Usar una clave segura de 32+ caracteres
   - Guardar en variables de entorno

2. **HTTPS Obligatorio:**
   - Nginx Proxy Manager ya maneja redirección

3. **CORS:**
   - Actualmente permitido desde cualquier origen
   - En producción: Especificar dominio del frontend

4. **Credenciales:**
   - Nunca commitear credenciales
   - Usar .env.local para desarrollo
   - En producción: usar secrets en Docker

---

## 📚 Recursos

- 📖 Documentación de Endpoints: `DOCUMENTACION_ENDPOINTS_COMPLETA.md`
- 🐳 Docker Compose: `docker-compose.yml`
- 📝 Migraciones DB: `ApexVision.Backend/Migrations/`
- 🔗 Swagger Local: `http://localhost:5132/swagger`
- 🔗 Swagger Producción: `https://service.lujuria.crudzaso.com/swagger`

---

Última actualización: 4 de Diciembre de 2025  
Versión: 1.0

