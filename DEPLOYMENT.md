# 🚀 Guía de Despliegue en Producción

## 📋 Archivos para Subir al Servidor VPS

Debes subir **manualmente** vía SFTP los siguientes archivos al servidor:

### 1. `docker-compose.prod.yml`
**Ubicación local**: `/proyecto-logistica-backend-csharp/docker-compose.prod.yml`  
**Ubicación en servidor**: `/root/deploy/proyecto-logistica-backend-csharp/docker-compose.yml`

> [!IMPORTANT]
> Renombra `docker-compose.prod.yml` a `docker-compose.yml` en el servidor

Este archivo contiene todas las credenciales de producción:
- ✅ Conexión a PostgreSQL externa (46.224.92.193)
- ✅ Credenciales de Cloudinary
- ✅ Credenciales de Azure Vision
- ✅ JWT Key de producción
- ✅ Configuración de RabbitMQ externo (rabbitmq.apexvision.crudzaso.com)

### 2. `appsettings.json` (Opcional)
**Ubicación local**: `/proyecto-logistica-backend-csharp/ApexVision.Backend/appsettings.json`  
**Ubicación en servidor**: No es necesario subirlo

> [!NOTE]
> No necesitas subir este archivo porque el Dockerfile ya lo copia durante el build. Las variables de entorno en docker-compose.yml tienen prioridad sobre este archivo.

---

## 🔐 Seguridad de Credenciales

### Archivos Protegidos (NO se suben a GitHub)

El `.gitignore` está configurado para proteger:
- ✅ `appsettings.json` (credenciales reales)
- ✅ `docker-compose.yml` (configuración local)
- ✅ `docker-compose.prod.yml` (credenciales de producción)
- ✅ `appsettings.Production.json`

### Archivos en GitHub (Seguros)

- ✅ `appsettings.Template.json` - Plantilla con valores dummy
- ✅ `docker-compose.example.yml` - Ejemplo con placeholders
- ✅ `Dockerfile` - Sin credenciales
- ✅ Código fuente

---

## 📝 Pasos de Despliegue

### 1. Conectar al Servidor VPS

```bash
ssh root@46.224.92.193
cd /root/deploy/proyecto-logistica-backend-csharp
```

### 2. Actualizar el Código desde GitHub

```bash
git pull origin main
```

> [!NOTE]
> Esto NO sobrescribirá tu `docker-compose.yml` porque está en .gitignore

### 3. Subir docker-compose.prod.yml (Primera vez o si cambió)

**Opción A: Usando SFTP (Recomendado)**
```bash
# En tu máquina local
sftp root@46.224.92.193
cd /root/deploy/proyecto-logistica-backend-csharp
put docker-compose.prod.yml docker-compose.yml
exit
```

**Opción B: Usando SCP**
```bash
# En tu máquina local
scp docker-compose.prod.yml root@46.224.92.193:/root/deploy/proyecto-logistica-backend-csharp/docker-compose.yml
```

### 4. Construir y Desplegar

```bash
# En el servidor VPS
cd /root/deploy/proyecto-logistica-backend-csharp

# Detener contenedores anteriores
docker compose down

# Construir sin caché (para asegurar última versión)
docker compose build --no-cache

# Iniciar en modo detached
docker compose up -d
```

### 5. Verificar el Despliegue

```bash
# Ver logs del backend
docker logs apex_backend -f

# Verificar que el contenedor esté corriendo
docker ps | grep apex_backend

# Probar el endpoint de salud
curl http://localhost:8080/swagger
```

---

## 🔄 Actualizaciones Futuras

### Si solo cambió el código (sin cambios en credenciales):

```bash
# En el servidor
cd /root/deploy/proyecto-logistica-backend-csharp
git pull origin main
docker compose build --no-cache
docker compose up -d
```

### Si cambiaron las credenciales:

1. Actualiza `docker-compose.prod.yml` en tu máquina local
2. Sube el archivo actualizado al servidor (paso 3 de arriba)
3. Ejecuta el despliegue completo

---

## 🌐 Configuración de Nginx Proxy Manager

Una vez que el contenedor esté corriendo, configura el reverse proxy:

### Backend API
- **Domain**: `service.apexvision.crudzaso.com`
- **Forward Hostname/IP**: `localhost` o `46.224.92.193`
- **Forward Port**: `8080`
- **SSL**: Activado (Let's Encrypt)

### RabbitMQ Management UI (si aplica)
- **Domain**: `rabbitmq.apexvision.crudzaso.com`
- **Forward Port**: `15672`
- **SSL**: Activado

---

## ⚠️ Troubleshooting

### Error: "Cannot connect to database"
```bash
# Verificar que PostgreSQL en 46.224.92.193 esté accesible
docker exec apex_backend ping 46.224.92.193

# Verificar logs de conexión
docker logs apex_backend | grep -i "database\|postgres"
```

### Error: "RabbitMQ connection failed"
```bash
# Verificar conectividad a RabbitMQ
docker exec apex_backend ping rabbitmq.apexvision.crudzaso.com

# Verificar logs de RabbitMQ
docker logs apex_backend | grep -i "rabbitmq"
```

### Error: "Port 8080 already in use"
```bash
# Ver qué está usando el puerto
sudo lsof -i :8080

# Detener el contenedor conflictivo
docker compose down
```

---

## 📊 Monitoreo

### Ver logs en tiempo real
```bash
docker logs apex_backend -f --tail 100
```

### Ver uso de recursos
```bash
docker stats apex_backend
```

### Reiniciar el servicio
```bash
docker compose restart apex_backend
```

---

## 🎯 Checklist de Despliegue

- [ ] Subir `docker-compose.prod.yml` al servidor (renombrado a `docker-compose.yml`)
- [ ] Verificar que PostgreSQL (46.224.92.193) esté accesible
- [ ] Verificar que RabbitMQ (rabbitmq.apexvision.crudzaso.com) esté accesible
- [ ] Ejecutar `git pull` en el servidor
- [ ] Ejecutar `docker compose build --no-cache`
- [ ] Ejecutar `docker compose up -d`
- [ ] Verificar logs: `docker logs apex_backend`
- [ ] Configurar Nginx Proxy Manager para `service.apexvision.crudzaso.com`
- [ ] Probar endpoint: `https://service.apexvision.crudzaso.com/swagger`
- [ ] Verificar que el admin puede hacer login
