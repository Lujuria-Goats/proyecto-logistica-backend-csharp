# 🚀 GUÍA DE DESPLIEGUE DOCKER/VPS - APEX VISION

**Fecha:** Diciembre 1, 2025  
**Status:** ✅ LISTO PARA DESPLIEGUE

---

## ✅ LAS 4 COSAS QUE DEBES TENER LISTAS

### **1️⃣ CORRECCIÓN DE HTTPS (YA HECHA ✅)**
```csharp
// ✅ YA ESTÁ COMENTADO EN Program.cs
// app.UseHttpsRedirection();  
// Nginx maneja HTTPS internamente, no lo necesitamos
```

**Por qué:** Nginx Proxy Manager convierte HTTPS a HTTP internamente. Si dejamos esta línea activa, crea un bucle:
- Cliente → HTTPS (Nginx)
- Nginx → HTTP (Docker)
- Docker dice "necesitas HTTPS" → redirige
- Loop infinito ❌

**Solución:** Ya está comentado ✅

---

### **2️⃣ VARIABLES DE ENTORNO PARA docker-compose.yml**

En **Termius**, cuando crees el `docker-compose.yml`, debes incluir EXACTAMENTE estas variables:

```yaml
services:
  apex-backend:
    image: tu-usuario/apex-vision:latest
    environment:
      # ============================================
      # 🗄️ BASE DE DATOS (CRÍTICO)
      # ============================================
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=postgres;Username=root;Password=xbI4PLOvlMwRwHn7SdZXHivFOZwc99
      
      # ============================================
      # 🔐 JWT (DEBEN SER IGUALES)
      # ============================================
      - Jwt__Key=977df0f8dd4634f798c6440f74d29fb0ea5dd55eb3ea49a2c7097a98951dc515
      - Jwt__Issuer=ApexVision
      - Jwt__Audience=ApexVisionUsers
      - Jwt__ExpirationMinutes=60
      
      # ============================================
      # ☁️ CLOUDINARY (TUS CREDENCIALES REALES)
      # ============================================
      - Cloudinary__CloudName=dqxblxx8y
      - Cloudinary__ApiKey=189848418239129
      - Cloudinary__ApiSecret=yvKKf2Q_eW1UFlu55_sLMd38XUw
      
      # ============================================
      # 🌐 RABBITMQ
      # ============================================
      - RABBITMQ_HOSTNAME=rabbitmq.lujuria.crudzaso.com
      - RABBITMQ_USERNAME=admin
      - RABBITMQ_PASSWORD=Kj9#mP2$qR5@vX8&
      - RABBITMQ_VIRTUALHOST=/
      - RABBITMQ_QUEUENAME=pc_commands
      
      # ============================================
      # ⚙️ ENTORNO
      # ============================================
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    
    ports:
      - "8080:8080"
    
    depends_on:
      - db
    
    networks:
      - apex-network
```

**IMPORTANTE:** 
- `Host=db` NO es `localhost` (porque está dentro de Docker)
- `db` es el nombre del servicio de PostgreSQL en el docker-compose
- Los otros campos (JWT, Cloudinary) son IDs/claves reales

---

### **3️⃣ NOMBRE DEL SERVICIO JAVA (CRÍTICO)**

En tu `Program.cs` tienes hardcodeado:

```csharp
client.BaseAddress = new Uri("http://apex-java:8081/");
```

**Esto significa:** El servicio Java TIENE que llamarse `apex-java` en el docker-compose.

**Ejemplo CORRECTO en docker-compose.yml:**

```yaml
apex-java:  # <--- ¡TIENE QUE SER EXACTAMENTE ESTE NOMBRE!
  image: jeferson/apex-java:latest  # La imagen que Jeferson te proporcione
  ports:
    - "8081:8081"  # Puerto interno/externo
  networks:
    - apex-network
  environment:
    - SPRING_DATASOURCE_URL=jdbc:postgresql://db:5432/postgres
    - SPRING_DATASOURCE_USERNAME=root
    - SPRING_DATASOURCE_PASSWORD=xbI4PLOvlMwRwHn7SdZXHivFOZwc99
```

**Si Jeferson te dice "estoy enviando la imagen como `spring-app`", entonces debes cambiar en tu código:**
```csharp
// CAMBIAR A:
client.BaseAddress = new Uri("http://spring-app:8081/");
```

---

### **4️⃣ DOCKERFILE (REQUIERE PUERTO 8080)**

Tu Dockerfile debe tener:

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app

COPY bin/Release/net8.0/ .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ApexVision.Backend.dll"]

# Importante: .NET 8 escucha en puerto 8080 por defecto
# Si quieres cambiar el puerto, usa: 
# ENTRYPOINT ["dotnet", "ApexVision.Backend.dll", "--urls", "http://+:3000"]
```

---

## 🎯 CHECKLIST PRE-DESPLIEGUE

```
[✅] Program.cs tiene HTTPS comentado
[✅] Migraciones automáticas implementadas
[✅] Seeding del usuario Admin incluido
[✅] Swagger habilitado siempre (no solo Development)
[✅] Logging Serilog configurado
[✅] Middleware de errores centralizado
[✅] Build sin errores (0 advertencias)

[ ] docker-compose.yml listo con variables de entorno
[ ] PostgreSQL configurado en docker-compose
[ ] RabbitMQ conectado (rabbit.lujuria.crudzaso.com)
[ ] Cloudinary credenciales verificadas
[ ] Servicio Java disponible y llamado "apex-java"
[ ] Dockerfile generado y testeado localmente
[ ] Imagen Docker creada: docker build -t tu-usuario/apex-vision .
[ ] Imagen subida a DockerHub (opcional pero recomendado)
```

---

## 📋 PASO A PASO PARA DESPLIEGUE EN TERMIUS

### **Paso 1: Build de Docker Local**
```bash
cd ApexVision.Backend
dotnet publish -c Release -o publish
docker build -t tu-usuario/apex-vision:latest .
```

### **Paso 2: Subir imagen a DockerHub (Opcional)**
```bash
docker login
docker push tu-usuario/apex-vision:latest
```

### **Paso 3: En Termius, crear docker-compose.yml**
Con las variables de entorno de la sección 2️⃣

### **Paso 4: Ejecutar**
```bash
docker-compose up -d
```

### **Paso 5: Verificar logs**
```bash
docker-compose logs -f apex-backend
```

---

## 🧪 CÓMO PROBAR QUE TODO FUNCIONA

### **Test 1: Swagger está disponible**
```bash
curl -I http://localhost:8080/swagger
# Debe retornar HTTP 200, no 404
```

### **Test 2: Base de datos inicializada**
```bash
# Ver logs
docker-compose logs apex-backend | grep "Rol 'Admin' creado"
# Debe mostrar este mensaje
```

### **Test 3: Usuario Admin funciona**
```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@apexvision.com",
    "password": "Admin123!"
  }'
# Debe retornar un JWT token
```

### **Test 4: Java API conectada**
```bash
# Dentro del contenedor:
docker-compose exec apex-backend curl -I http://apex-java:8081/
# Debe conectar sin problemas
```

---

## ⚠️ ERRORES COMUNES Y SOLUCIONES

| Error | Causa | Solución |
|-------|-------|----------|
| **404 en /swagger** | HTTPS activo o Development mode | Verificar que `app.UseHttpsRedirection()` está comentado |
| **"Too many redirects"** | HTTPS redirige infinitamente | Comentar `UseHttpsRedirection()` |
| **"Cannot connect to db"** | Host=localhost en Docker | Cambiar a `Host=db` (nombre del servicio) |
| **"apex-java unreachable"** | Servicio tiene otro nombre | Cambiar el nombre en docker-compose a `apex-java` |
| **"JWT Key invalid"** | Keys no coinciden | Verificar `Jwt__Key` en docker-compose |
| **"Port 8080 already in use"** | Otro proceso usa el puerto | `docker-compose down` o cambiar puerto en compose |

---

## 🚀 RESUMEN FINAL

**Tu Program.cs está 100% listo porque:**
- ✅ Migraciones automáticas
- ✅ Seeding del usuario Admin
- ✅ HTTPS comentado (evita bucles)
- ✅ Swagger habilitado
- ✅ Logging persistente
- ✅ Error handling centralizado

**Lo único que falta es:**
1. Crear `docker-compose.yml` con las variables de entorno
2. Asegurar que el servicio Java se llame `apex-java`
3. Verificar que Dockerfile expone puerto 8080
4. Hacer build y push de la imagen

**¡Estás listo para el VPS!** 🦅🚀


