# Apex Vision - Backend 🦅

![.NET](https://img.shields.io/badge/.NET-8-blueviolet) ![PostgreSQL](https://img.shields.io/badge/PostgreSQL-blue) ![Docker](https://img.shields.io/badge/Docker-blue) ![RabbitMQ](https://img.shields.io/badge/RabbitMQ-orange) ![Azure AI](https://img.shields.io/badge/Azure_AI-Vision-blue) ![JWT](https://img.shields.io/badge/Auth-JWT-green)

## 🚀 Descripción General

**Apex Vision** es el backend para una plataforma de logística avanzada que optimiza y gestiona rutas de entrega. Construido con **.NET 8** y diseñado para ser desplegado con **Docker**, utiliza una arquitectura de microservicios escalable que integra inteligencia artificial para la validación de entregas, almacenamiento en la nube y procesamiento asíncrono de tareas.

### Características Clave

✨ **Autenticación Segura**: JWT con roles (Admin, Driver).  
📍 **Gestión de Pedidos**: Creación, asignación y seguimiento de estados.  
🛣️ **Rutas Verificadas vs. Simples**: Lógica para requerir o no evidencia fotográfica.  
🤖 **Validación con IA**: Integración con **Azure Computer Vision** para analizar las fotos de evidencia y asegurar que sean legítimas.  
☁️ **Almacenamiento en Nube**: Subida de imágenes a Cloudinary.  
🚚 **Optimización de Rutas**: Microservicio Java para algoritmos de optimización de rutas.  
📨 **Mensajería Asíncrona**: RabbitMQ para la comunicación con otros microservicios.  
📊 **Logging Estructurado**: Serilog para una trazabilidad completa de la aplicación.  
🚀 **Listo para Despliegue**: Configuración completa con Docker y Docker Compose para un despliegue sencillo en cualquier VPS.

---

## 🛠️ Tecnologías Utilizadas

| Componente | Tecnología |
|:-----------|:-----------|
| Framework | .NET 8 |
| Base de Datos | PostgreSQL 15 |
| Contenerización | Docker & Docker Compose |
| ORM | Entity Framework Core 8 |
| Autenticación | ASP.NET Core Identity + JWT |
| Almacenamiento | Cloudinary |
| Validación IA | Azure Computer Vision |
| Mensajería | RabbitMQ |
| Logging | Serilog |

---

## 🚀 Guía de Despliegue Rápido (con Docker)

Este proyecto está diseñado para ser ejecutado con Docker Compose, lo que simplifica enormemente la configuración del entorno de desarrollo y producción.

### 1. Clonar el Repositorio

```bash
git clone https://github.com/Lujuria-Goats/proyecto-logistica-backend-csharp.git
cd proyecto-logistica-backend-csharp
```

### 2. Configurar el Entorno

El archivo `docker-compose.yml` ya contiene todas las variables de entorno necesarias para levantar los servicios. Asegúrate de revisar y, si es necesario, ajustar los valores (especialmente las claves secretas como `Jwt__Key`).

### 3. Levantar los Servicios

Desde la raíz del proyecto, ejecuta el siguiente comando:

```bash
docker-compose up --build -d
```

Este comando hará lo siguiente:

- **Construirá la imagen Docker** de la aplicación .NET.
- **Levantará los contenedores** para el backend, la base de datos PostgreSQL.
- **Configurará la red** para que los servicios se comuniquen entre sí.

¡Y eso es todo! La API estará corriendo y accesible en `http://localhost:8080`.

---

## 📖 Endpoints de la API (Swagger)

Una vez que la aplicación esté corriendo, puedes acceder a la documentación interactiva de la API a través de Swagger en la siguiente URL:

**[http://localhost:8080/swagger/index.html](http://localhost:8080/swagger/index.html)**

Desde Swagger, podrás ver todos los endpoints, probarlos, y ver los modelos de datos que la API espera y devuelve.

### Credenciales de Administrador por Defecto

El sistema crea automáticamente un usuario administrador para que puedas empezar a probar:

- **Email**: `admin@apexvision.com`
- **Contraseña**: `Admin123!`

---

## ⚙️ Configuración de Variables de Entorno

Todas las configuraciones sensibles se gestionan a través de variables de entorno, definidas en el archivo `docker-compose.yml`.

| Variable | Descripción | Ejemplo |
|:---|:---|:---|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión a PostgreSQL. | `Host=db;...` |
| `Jwt__Key` | Clave secreta para firmar los tokens JWT. | `UNA_CLAVE_SUPER_SECRETA_Y_LARGA` |
| `Cloudinary__CloudName` | Nombre de tu nube en Cloudinary. | `my-cloud` |
| `Cloudinary__ApiKey` | API Key de Cloudinary. | `1234567890` |
| `Cloudinary__ApiSecret` | API Secret de Cloudinary. | `ABCDEFG-HIJKLMNOP` |
| `RabbitMQ__HostName` | Dominio o nombre del servicio de RabbitMQ. | `rabbitmq.lujuria.crudzaso.com` |
| `AZURE_VISION_ENDPOINT` | Endpoint de tu servicio Azure Computer Vision. | `https://my-vision.cognitiveservices.azure.com/` |
| `AZURE_VISION_KEY` | Clave de tu servicio Azure Computer Vision. | `0987654321-ABCDEF` |

---

## 📚 Documentación Adicional

- [.NET 8 Docs](https://docs.microsoft.com/dotnet/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [JWT Authentication](https://tools.ietf.org/html/rfc7519)
- [Serilog](https://serilog.net/)
- [xUnit](https://xunit.net/)

---

## 📄 Licencia

MIT License - Ver LICENSE file para más detalles

---

## 📞 Contacto

**Equipo Apex Vision**

- Backend Lead: Abrahan
- Frontend: Juan
- Mobile: Jeims
- Data/Optimization: Java Microservice Team

---

**Última actualización:** Diciembre 1, 2025  
**Versión:** 1.0.0
