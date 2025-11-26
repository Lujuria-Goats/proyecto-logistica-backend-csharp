# ApexVision

## Configuración de Variables de Entorno

Este proyecto utiliza variables de entorno para gestionar la configuración sensible. No se incluyen archivos `appsettings.json` en el repositorio por razones de seguridad.

### Pasos para configurar el proyecto

1. **Copia el archivo de ejemplo**:
   ```bash
   cp .env.example .env
   ```

2. **Edita el archivo `.env`** con tus valores reales:
   - `ConnectionStrings__DefaultConnection`: Cadena de conexión a PostgreSQL
   - `Jwt__Key`: Clave secreta para firmar tokens JWT (mínimo 32 caracteres)
   - `Jwt__Issuer`: Emisor del token JWT
   - `Jwt__Audience`: Audiencia del token JWT
   - `Jwt__ExpirationMinutes`: Tiempo de expiración del token en minutos

3. **Crea el archivo `appsettings.json`** (opcional):
   ```bash
   cp ApexVision.Backend/appsettings.json.example ApexVision.Backend/appsettings.json
   ```
   Luego edita el archivo con tus valores reales.

### Variables de Entorno Disponibles

Consulta el archivo `.env.example` para ver todas las variables de entorno disponibles.

### Ejecutar el proyecto

```bash
cd ApexVision.Backend
dotnet run
```

### Base de Datos

El proyecto utiliza PostgreSQL. Asegúrate de tener una instancia de PostgreSQL ejecutándose y actualiza la cadena de conexión en las variables de entorno.

### Migraciones

Para aplicar las migraciones de base de datos:

```bash
cd ApexVision.Backend
dotnet ef database update
```
