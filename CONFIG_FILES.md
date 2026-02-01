# 📁 Estructura de Archivos de Configuración

Este documento explica la estructura de archivos de configuración del proyecto y cuáles deben subirse a GitHub vs. cuáles deben mantenerse locales.

## 🔐 Archivos de Configuración

### Archivos en GitHub (Públicos/Plantillas)

| Archivo | Propósito | Ubicación |
|---------|-----------|-----------|
| `appsettings.Template.json` | Plantilla con valores dummy para desarrollo | `ApexVision.Backend/` |
| `docker-compose.example.yml` | Ejemplo de configuración Docker con placeholders | Raíz del proyecto |
| `.env.example` | Ejemplo de variables de entorno | Raíz del proyecto |

### Archivos Locales (NO en GitHub)

| Archivo | Propósito | Ubicación | Protegido por .gitignore |
|---------|-----------|-----------|--------------------------|
| `appsettings.json` | Configuración con credenciales REALES | `ApexVision.Backend/` | ✅ Sí |
| `docker-compose.yml` | Docker compose local para desarrollo | Raíz del proyecto | ✅ Sí |
| `docker-compose.prod.yml` | Docker compose de producción con credenciales | Raíz del proyecto | ✅ Sí |
| `.env` | Variables de entorno locales | Raíz del proyecto | ✅ Sí |

## 🚀 Flujo de Trabajo

### Para Desarrollo Local

1. Copia `appsettings.Template.json` a `appsettings.json`
2. Edita `appsettings.json` con tus credenciales de desarrollo
3. Ejecuta la aplicación con `dotnet run` o tu IDE favorito

### Para Producción (VPS)

1. Sube `docker-compose.prod.yml` al servidor vía SFTP
2. Renómbralo a `docker-compose.yml` en el servidor
3. Ejecuta `./deploy.sh` en el servidor

## 📋 Contenido de Cada Archivo

### appsettings.json (Local - NO en Git)

Contiene las credenciales REALES para tu entorno:
- Conexión a base de datos PostgreSQL
- JWT Key
- Cloudinary credentials
- Azure Vision credentials
- RabbitMQ configuration

**Este archivo NUNCA debe subirse a GitHub**

### appsettings.Template.json (En Git)

Plantilla con valores de ejemplo:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=apexvision_dev;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "YOUR_DEVELOPMENT_JWT_KEY_AT_LEAST_32_CHARACTERS_LONG"
  }
  // ... más configuraciones con placeholders
}
```

### docker-compose.prod.yml (Local - NO en Git)

Configuración de Docker con todas las credenciales de producción:
- Variables de entorno con valores reales
- Configuración de recursos (CPU, memoria)
- Configuración de red
- Puertos expuestos

**Este archivo se sube manualmente al servidor vía SFTP**

### docker-compose.example.yml (En Git)

Ejemplo de configuración Docker con placeholders:
```yaml
environment:
  - ConnectionStrings__DefaultConnection=Host=YOUR_DB_HOST;Port=5432;...
  - Jwt__Key=YOUR_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG
  # ... más variables con placeholders
```

## 🔄 Cómo Actualizar Credenciales

### En Desarrollo Local

1. Edita `appsettings.json` directamente
2. Reinicia la aplicación

### En Producción

1. Edita `docker-compose.prod.yml` en tu máquina local
2. Sube el archivo actualizado al servidor:
   ```bash
   scp docker-compose.prod.yml root@46.224.92.193:/root/deploy/proyecto-logistica-backend-csharp/docker-compose.yml
   ```
3. Reinicia los contenedores:
   ```bash
   ssh root@46.224.92.193
   cd /root/deploy/proyecto-logistica-backend-csharp
   docker compose restart apex_backend
   ```

## ⚠️ Importante

- **NUNCA** hagas commit de archivos con credenciales reales
- **SIEMPRE** verifica que `.gitignore` esté protegiendo los archivos correctos
- **SIEMPRE** usa SFTP/SCP para transferir archivos con credenciales al servidor
- **NUNCA** compartas `docker-compose.prod.yml` o `appsettings.json` en chats, issues, o pull requests

## 🔍 Verificar Protección

Para verificar que los archivos sensibles están protegidos:

```bash
# Ver qué archivos están siendo trackeados por git
git status

# Verificar que appsettings.json está ignorado
git check-ignore -v ApexVision.Backend/appsettings.json

# Verificar que docker-compose.prod.yml está ignorado
git check-ignore -v docker-compose.prod.yml
```

Si alguno de estos archivos NO está siendo ignorado, revisa tu `.gitignore`.
