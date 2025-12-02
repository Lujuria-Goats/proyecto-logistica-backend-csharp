# Cómo Crear tu `Dockerfile` en el Servidor VPS

Esta es una guía rápida para crear el `Dockerfile` de tu proyecto Java directamente en el servidor.

---

### Requisitos

-   Necesitas poder conectarte al servidor VPS (por ejemplo, con Termius o cualquier cliente SSH).
-   El código de tu proyecto Java ya debe estar clonado en el servidor (por ejemplo, en la carpeta `~/apex-vision-deploy/proyecto-logistica-backend-java`).

---

### Pasos a Seguir

1.  **Conéctate al Servidor**: Abre una terminal y conéctate al VPS.

2.  **Navega a tu Proyecto**: Ve al directorio raíz de tu proyecto Java.
    ```bash
    # Ejemplo de ruta, ajústala si es necesario
    cd ~/apex-vision-deploy/proyecto-logistica-backend-java
    ```

3.  **Crea el Archivo `Dockerfile`**: Usa el comando `touch` para crear el archivo vacío.
    ```bash
    touch Dockerfile
    ```

4.  **Abre el Archivo con `nano`**: Usa el editor de texto `nano` para poder pegar el contenido.
    ```bash
    nano Dockerfile
    ```

5.  **Pega el Contenido**: Ahora, pega el siguiente código dentro de la terminal. Este es un `Dockerfile` optimizado y listo para producción para una aplicación de Spring Boot.

    ```dockerfile
    # ==========================================
    # STAGE 1: BUILDER
    # ==========================================
    # Use an official Maven image to build the application
    FROM maven:3.9.5-eclipse-temurin-17 AS builder

    # Set the working directory inside the container
    WORKDIR /app

    # 1. Copy only pom.xml first (Layer Caching Strategy)
    # This allows Docker to cache dependencies if the POM hasn't changed,
    # speeding up future builds significantly.
    COPY pom.xml .

    # 2. Download dependencies (Go offline mode)
    RUN mvn dependency:go-offline

    # 3. Copy the actual source code
    COPY src ./src

    # 4. Build and package the application
    # We skip tests here (-DskipTests) to speed up the deployment build.
    # Tests should be enforced in the CI pipeline (GitHub Actions) before this step.
    RUN mvn clean package -DskipTests

    # ==========================================
    # STAGE 2: RUNTIME
    # ==========================================
    # Use a lightweight JRE image (Alpine Linux) to minimize the final image size
    FROM eclipse-temurin:17-jre-alpine

    # Set TimeZone to Bogota/Colombia (Critical for accurate logs)
    ENV TZ=America/Bogota

    # Set working directory for the runtime
    WORKDIR /app

    # Copy the generated JAR artifact from the 'builder' stage
    # It automatically finds the .jar file and renames it to 'app.jar' for simplicity
    COPY --from=builder /app/target/*.jar app.jar

    # Expose the application port
    EXPOSE 8080

    # Command to start the application
    ENTRYPOINT ["java", "-jar", "app.jar"]
    ```

6.  **Guarda y Cierra**: Para guardar los cambios y salir de `nano`, presiona:
    1.  `Ctrl + X`
    2.  Luego la tecla `Y` (para confirmar que quieres guardar)
    3.  Finalmente, la tecla `Enter` (para confirmar el nombre del archivo)

¡Y eso es todo! Una vez que hayas hecho esto, avísale al encargado del despliegue. El `Dockerfile` ya estará en su sitio, listo para que Docker Compose lo utilice.

