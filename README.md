# ArandanoIRTSoftware

Este repositorio contiene el código fuente de la aplicación web principal para el Sistema de Monitoreo de Estrés Hídrico en Plantas de Arándano.

## Configuración del Entorno de Desarrollo Local

Este proyecto utiliza un **enfoque híbrido** para el desarrollo local, combinando la ejecución nativa de la aplicación .NET con dependencias de infraestructura corriendo en contenedores de Docker para un flujo de trabajo rápido y eficiente.

### Prerrequisitos

* Git
* .NET 8 SDK
* Docker y Docker Compose
* Un IDE de tu preferencia (Visual Studio, JetBrains Rider, VS Code)

### Pasos de Configuración

1.  **Clonar los Repositorios:** Clona los repositorios `ArandanoIRTSoftware` y `ArandanoIRTOps` en la misma carpeta raíz en tu máquina. Tu estructura de carpetas debería verse así:
    ```
    /tus-proyectos/
    ├── ArandanoIRTSoftware/  <-- Estás en el README de este repo
    └── ArandanoIRTOps/
    ```

2.  **Configurar Dependencias de Infraestructura:**
    * Navega a la carpeta del repositorio de operaciones: `cd ../ArandanoIRTOps`.
    * Crea un archivo llamado `docker-compose.override.yml`. **Este archivo no debe ser subido a Git.**
    * Pega el siguiente contenido en el archivo. Este `override` define los servicios de base de datos y almacenamiento que usaremos localmente:

        ```yaml
        # docker-compose.override.yml
        # Configuración exclusiva para desarrollo local.

        version: '3.8'

        services:
          postgres:
            ports:
              - "5433:5432" # Expone el puerto de la BD al host en el puerto 5433
            volumes:
              - postgres-data:/var/lib/postgresql/data

          minio:
            ports:
              - "9000:9000" # API de MinIO
              - "9001:9001" # Consola web de MinIO
            volumes:
              - minio-data:/data

        volumes:
          postgres-data:
          minio-data:
        ```

3.  **Configurar la Aplicación .NET:**
    * Regresa a la carpeta de este proyecto (`cd ../ArandanoIRTSoftware`).
    * Dentro del proyecto web, crea un archivo llamado `appsettings.Development.json`. **Este archivo no debe ser subido a Git.**
    * Pega el siguiente contenido, reemplazando las credenciales si es necesario. Estas son las cadenas de conexión que tu aplicación usará para encontrar los contenedores de Docker.

        ```json
        {
          "ConnectionStrings": {
            "DefaultConnection": "Host=localhost;Port=5433;Database=arandano_db;Username=user;Password=pass"
          },
          "S3": {
            "ServiceURL": "http://localhost:9000",
            "AccessKey": "minioadmin",
            "SecretKey": "minioadmin"
          }
        }
        ```

### Flujo de Trabajo

1.  **Iniciar Dependencias:** Abre una terminal en la carpeta `ArandanoIRTOps` y ejecuta `docker-compose up -d`. Esto iniciará los contenedores de PostgreSQL y MinIO en segundo plano. Solo necesitas hacerlo una vez por sesión de trabajo.
2.  **Ejecutar la Aplicación:** Abre la solución `ArandanoIRT.Web.sln` en tu IDE y presiona "Run" o "Debug" (F5).

¡Listo! Tu aplicación .NET se ejecutará localmente y se conectará a los servicios que corren en Docker, dándote un entorno de desarrollo rápido y realista.