# Instrucciones para Desplegar el Microservicio Java con Docker

Estas instrucciones te guiarán a través del proceso de despliegue de tu microservicio Java en un servidor VPS utilizando Docker y Docker Compose.

---

## Requisitos Previos

Asegúrate de que tu servidor VPS tenga instalados:

- Docker
- Docker Compose

---

## Paso 1: Clonar el Repositorio

Accede a tu servidor VPS y clona el repositorio de tu microservicio Java en una carpeta de tu elección.

```bash
git clone https://github.com/tu-usuario/tu-repo-java.git
```

---

## Paso 2: Crear el Dockerfile

Navega a la carpeta de tu microservicio Java y crea un archivo llamado `Dockerfile`. Este archivo contendrá las instrucciones para construir la imagen de Docker de tu microservicio.

```bash
cd tu-repo-java
touch Dockerfile
```

Abre el `Dockerfile` en un editor de texto y añade las siguientes líneas básicas:

```
# Usa una imagen base de Java
FROM openjdk:11-jre-slim

# Copia el archivo JAR de tu aplicación al contenedor
COPY target/tu-aplicacion.jar /app/tu-aplicacion.jar

# Establece el punto de entrada para ejecutar la aplicación
ENTRYPOINT ["java", "-jar", "/app/tu-aplicacion.jar"]
```

Asegúrate de reemplazar `tu-aplicacion.jar` con el nombre real de tu archivo JAR.

---

## Paso 3: Construir la Imagen de Docker

Construye la imagen de Docker utilizando el siguiente comando. Asegúrate de estar en la carpeta donde se encuentra tu `Dockerfile`.

```bash
docker build -t tu-imagen-java .
```

---

### Opción A: Construir en tu PC y subir a Docker Hub

**Usa este método si construyes la imagen en tu computadora local y necesitas transferirla al servidor.**

#### 1. Inicia sesión en Docker Hub
Desde la terminal de tu PC, inicia sesión en tu cuenta de Docker Hub.
```bash
docker login
```

#### 2. Etiqueta tu imagen
Etiqueta la imagen que construiste con tu nombre de usuario de Docker Hub.
```bash
docker tag tu-imagen-java tu-usuario-de-dockerhub/apex-java:latest
```

#### 3. Sube la imagen a Docker Hub
```bash
docker push tu-usuario-de-dockerhub/apex-java:latest
```

#### 4. Comparte el nombre de la imagen
El nombre que debes compartir con el equipo de .NET es:
**`tu-usuario-de-dockerhub/apex-java:latest`**

---

### Opción B: Construir directamente en el VPS (Método recomendado)

**Usa este método si clonaste el código de tu proyecto Java directamente en el VPS. Es más simple porque no necesitas usar Docker Hub.**

**Tu Tarea:**

1.  **Asegúrate de tener el `Dockerfile`** en la raíz de tu proyecto Java en el VPS (como se explica en el Paso 2).
2.  **¡Eso es todo!** No necesitas construir ni subir la imagen manualmente. Docker Compose se encargará de construirla automáticamente durante el despliegue.
3.  **Informa al desarrollador de .NET** la ruta de la carpeta donde clonaste tu proyecto en el VPS (por ejemplo, `/home/user/apex-vision-java`).

---

## Notas Adicionales

La propiedad `context` en la sección `build` **NO es un dominio**. Es la **ruta local a la carpeta en el servidor VPS** donde se encuentra el código fuente de cada proyecto. Docker Compose usa esta ruta para encontrar el `Dockerfile` y construir la imagen.

---

## ¿El Servicio de Java Tendrá un Dominio Público?

No. En esta arquitectura, el servicio de Java es un **microservicio interno**.

- **Solo el backend de .NET** es accesible desde internet a través de su dominio público.
- El backend de .NET se comunica con el servicio de Java a través de la **red privada de Docker**, usando el nombre del servicio (`apex-java`).
- El servicio de Java **no necesita ni debe** estar expuesto al público.
