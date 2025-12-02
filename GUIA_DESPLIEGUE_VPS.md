# Guía de Despliegue Completa en VPS con Termius

Esta guía te llevará paso a paso a través del despliegue de la aplicación completa (Backend .NET y Microservicio Java) en tu servidor VPS usando Docker.

---

## Orden de Despliegue

El despliegue se realiza en 4 fases principales:

1.  **Preparación del Servidor**: Instalar las herramientas necesarias.
2.  **Subida del Código**: Clonar los repositorios de .NET y Java.
3.  **Configuración Final**: Crear el `docker-compose.yml` que orquesta todo.
4.  **Lanzamiento**: Ejecutar Docker Compose y configurar el dominio.

---

### Fase 1: Preparación del Servidor

Conéctate a tu VPS a través de Termius y ejecuta los siguientes comandos para instalar todo lo que necesitas.

**Estos son los comandos oficiales de Docker para garantizar una instalación limpia y sin conflictos.**

```bash
# --- 1. Desinstalar versiones antiguas o conflictivas ---
sudo apt-get remove docker docker-engine docker.io containerd runc

# --- 2. Configurar el repositorio oficial de Docker ---
# Actualizar la lista de paquetes e instalar prerrequisitos
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg

# Añadir la clave GPG oficial de Docker
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

# Añadir el repositorio a las fuentes de APT
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# --- 3. Instalar Docker Engine y Docker Compose ---
# Actualizar la lista de paquetes de nuevo (ahora incluye Docker)
sudo apt-get update

# Instalar las últimas versiones
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# --- 4. Instalar Git ---
sudo apt-get install -y git

# --- 5. (Opcional pero recomendado) Añadir tu usuario al grupo de Docker ---
# Esto te permite ejecutar comandos de Docker sin tener que escribir 'sudo' cada vez.
# Reemplaza 'tu-usuario' con tu nombre de usuario real (ej: abraham).
sudo usermod -aG docker tu-usuario

# IMPORTANTE: Cierra la sesión de Termius y vuelve a conectarte
# para que los cambios de grupo de usuario tengan efecto.
```

---

### Fase 2: Subida del Código

1.  **Crea una carpeta principal** para el proyecto en tu VPS.
    ```bash
    mkdir apex-vision-deploy
    cd apex-vision-deploy
    ```

2.  **Clona los dos repositorios** dentro de esta carpeta.
    ```bash
    # Clonar el backend de .NET
    git clone https://github.com/Lujuria-Goats/proyecto-logistica-backend-csharp.git

    # Clonar el microservicio de Java
    git clone https://github.com/Lujuria-Goats/proyecto-logistica-backend-java.git
    ```

3.  **El desarrollador de Java debe crear su `Dockerfile`** dentro de la carpeta `proyecto-logistica-backend-java`, como se explica en el otro documento de instrucciones. **No necesita un `docker-compose.yml` en su proyecto ni crear el contenedor manualmente.**

---

### Fase 3: Configuración Final (El `docker-compose.yml` Central)

Este es el paso más importante. Usaremos **un único archivo `docker-compose.yml`** para gobernar todos los servicios. Créalo y ábrelo para editarlo.

```bash
# 1. Crea el archivo vacío
touch docker-compose.yml

# 2. Ábrelo con el editor de texto 'nano'
nano docker-compose.yml
```

Ahora, **pega el siguiente contenido** dentro de la terminal (en `nano`):

```yaml
services:
  # API Backend (.NET)
  backend:
    build:
      context: ./proyecto-logistica-backend-csharp # Ruta a la carpeta .NET
      dockerfile: Dockerfile
    container_name: apex_backend
    restart: always
    ports:
      - "8080:8080" # Puerto público para Nginx
    depends_on:
      - db
      - apex-java # Asegura que Java inicie antes que .NET
    environment:
      # --- REVISA Y AJUSTA ESTOS VALORES ---
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=postgres;Username=TU_USUARIO_DB;Password=TU_PASSWORD_DB
      - Jwt__Key=UNA_CLAVE_SECRETA_SUPER_LARGA_Y_SEGURA_DE_MAS_DE_32_CARACTERES
      - Jwt__Issuer=ApexVisionAPI
      - Jwt__Audience=ApexVisionUsers
      - Jwt__ExpirationMinutes=1440
      - Cloudinary__CloudName=TU_CLOUDINARY_CLOUD_NAME
      - Cloudinary__ApiKey=TU_CLOUDINARY_API_KEY
      - Cloudinary__ApiSecret=TU_CLOUDINARY_API_SECRET
      - RabbitMQ__HostName=rabbitmq.lujuria.crudzaso.com
      - RabbitMQ__UserName=TU_USUARIO_RABBITMQ
      - RabbitMQ__Password=TU_PASSWORD_RABBITMQ
      - AZURE_VISION_ENDPOINT=TU_AZURE_VISION_ENDPOINT
      - AZURE_VISION_KEY=TU_AZURE_VISION_KEY
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080

  # Microservicio de Optimización (Java)
  apex-java:
    build:
      context: ./proyecto-logistica-backend-java # Ruta a la carpeta Java
      dockerfile: Dockerfile
    container_name: apex_java
    restart: always
    ports:
      - "8081:8080" # Puerto interno para que .NET se comunique

  # Base de Datos (PostgreSQL)
  db:
    image: postgres:15-alpine
    container_name: apex_db
    restart: always
    environment:
      - POSTGRES_USER=TU_USUARIO_DB
      - POSTGRES_PASSWORD=TU_PASSWORD_DB
      - POSTGRES_DB=postgres
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
    driver: local
```

Después de pegar el contenido, **guarda y cierra el archivo** presionando:
1.  `Ctrl + X`
2.  Luego la tecla `Y` (para confirmar que quieres guardar)
3.  Finalmente, la tecla `Enter` (para confirmar el nombre del archivo)

---

### Fase 4: Lanzamiento

1.  **Ejecuta Docker Compose**: Desde la carpeta `apex-vision-deploy` (donde está tu `docker-compose.yml`), ejecuta el comando de lanzamiento. **Nota: Usa `docker compose` (con espacio), que es la sintaxis moderna.**
    ```bash
    docker compose up --build -d
    ```
    Docker construirá las imágenes de .NET y Java y levantará todos los contenedores.

2.  **Verifica que todo funcione**: Puedes ver los logs para asegurarte de que no haya errores.
    ```bash
    # Ver logs del backend
    docker logs apex_backend

    # Ver logs de Java
    docker logs apex_java
    ```

3.  **Configura el Dominio (Reverse Proxy)**:
    *   Ve a tu proveedor de DNS y crea un registro `A` para `service.lujuria.crudzaso.com` que apunte a la IP de tu VPS.
    *   En tu VPS (usando Nginx Proxy Manager o similar), crea un nuevo "Proxy Host" que redirija el tráfico de `service.lujuria.crudzaso.com` a `http://localhost:8080`.
    *   Activa el SSL en Nginx para tener `https://`.

¡Y eso es todo! Tu aplicación completa estará desplegada y funcionando.
