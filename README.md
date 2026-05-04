# AIRT — Sistema de Monitoreo de Estrés Hídrico en Arándano mediante Termografía Infrarroja

<div align="center">

[![CI - Build and Analyze](https://github.com/RFGRONA/ArandanoIRTSoftware/actions/workflows/ci.yml/badge.svg)](https://github.com/RFGRONA/ArandanoIRTSoftware/actions/workflows/ci.yml)
[![Build, Push, and Deploy](https://github.com/RFGRONA/ArandanoIRTSoftware/actions/workflows/deploy.yml/badge.svg)](https://github.com/RFGRONA/ArandanoIRTSoftware/actions/workflows/deploy.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-336791?logo=postgresql)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Containerized-2496ED?logo=docker)](https://www.docker.com/)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

**Plataforma web de monitoreo agrícola de precisión para la detección temprana del estrés hídrico en plantas de arándano (*Vaccinium corymbosum*) mediante el índice CWSI y termografía infrarroja.**

[Arquitectura](#-arquitectura-del-sistema) · [Características](#-características-principales) · [Inicio Rápido](#-inicio-rápido-desarrollo-local) · [Despliegue](#-despliegue-en-producción) · [Estructura](#-estructura-del-proyecto)

</div>

---

## 📋 Descripción del Proyecto

**AIRT** (*Arándano IRT*) es el componente de software principal de un proyecto de grado de ingeniería cuyo objetivo es desarrollar un sistema de monitoreo agrícola de bajo costo que permita a los productores detectar de forma temprana y objetiva el estrés hídrico en cultivos de arándano, reemplazando los métodos visuales subjetivos por un análisis basado en datos de termografía infrarroja.

El sistema calcula el **Índice de Estrés Hídrico del Cultivo (CWSI)** a partir de lecturas de temperatura de canopia y datos ambientales capturados por dispositivos IoT de bajo costo basados en ESP32. La plataforma web centraliza la gestión de cultivos, dispositivos, plantas y el análisis automatizado de estrés, todo dentro de una interfaz de administración segura y multi-tenant.

### Contexto Académico

Este repositorio contiene el código fuente de la **aplicación web monolítica** del sistema AIRT. El proyecto completo está compuesto por tres repositorios:

| Repositorio | Descripción |
|---|---|
| `ArandanoIRTSoftware` (este) | Aplicación web ASP.NET Core (monolito) |
| `ArandanoIRTOps` | Infraestructura de despliegue (Docker Compose, Nginx, configuraciones de VPS) |
| Firmware ESP32 | Firmware de los dispositivos de captura térmica e IoT |

---

## ✨ Características Principales

### Monitoreo y Análisis
- **Cálculo automatizado del CWSI**: Servicio en segundo plano que evalúa el estrés hídrico para cada planta monitorizada dentro de una ventana horaria configurable por cultivo.
- **Clasificación de estado de planta**: Clasificación automática del estado de las plantas (`Óptimo`, `Estrés Incipiente`, `Estrés Crítico`) basada en umbrales CWSI parametrizables.
- **Detección de anomalías**: Algoritmo de detección de anomalías en lecturas de temperatura (ΔT) con umbral y duración configurables.
- **Predicción con modelo ONNX**: Predictor de condición de planta basado en un modelo de árbol de decisión exportado al formato ONNX, ejecutado en tiempo real sobre el servidor mediante `Microsoft.ML.OnnxRuntime`.
- **Captura térmica**: Gestión y almacenamiento de imágenes térmicas por planta, con soporte para máscaras de región de interés (ROI).

### Gestión y Administración
- **Panel de control (Dashboard)**: Resumen en tiempo real del estado de todos los cultivos y plantas activas.
- **Gestión multi-entidad**: Administración completa de Cultivos, Plantas, Dispositivos IoT, Usuarios y Observaciones de campo.
- **Sistema de invitaciones**: Control de acceso mediante códigos de invitación para incorporar nuevos usuarios sin registro público.
- **Historiales y auditoría**: Registro histórico de cambios de estado de plantas y actividad de usuario.

### Asistente IA (RAG)
- **Asistente agrícola con RAG**: Módulo de inteligencia artificial conversacional basado en Retrieval-Augmented Generation (RAG), integrado como microservicio externo (FastAPI + Python). Permite a los usuarios consultar información técnica sobre arándano, estrés hídrico y prácticas agronómicas, con respuestas citadas.
- **Sincronización de base de conocimiento**: Servicio en segundo plano que ingiere automáticamente los registros de análisis en la base de conocimiento del asistente.

### Infraestructura y Operaciones
- **API para dispositivos IoT**: Endpoint seguro para la recepción de datos de sensores en tiempo real desde dispositivos ESP32 autenticados por token.
- **Alertas por correo electrónico**: Sistema de alertas automatizadas vía Brevo (SendinBlue) para notificaciones de estrés crítico, inactividad de dispositivos y recordatorios de calibración.
- **Generación de reportes PDF**: Generación de informes analíticos en formato PDF usando QuestPDF y Razor como motor de plantillas.
- **Almacenamiento de objetos**: Gestión de archivos estáticos (imágenes térmicas, etc.) mediante MinIO (compatible con S3).
- **Datos meteorológicos**: Integración con una API climática externa para enriquecer el análisis con datos ambientales del entorno del cultivo.
- **Protección anti-bot**: Integración con Cloudflare Turnstile en el formulario de inicio de sesión.

---

## 🏛️ Arquitectura del Sistema

El proyecto sigue los principios de **Arquitectura por Capas** (*Layered Architecture*), con una separación explícita de responsabilidades que facilita la mantenibilidad y las pruebas.

```
┌────────────────────────────────────────────────────────────────┐
│                    3_Presentation Layer                        │
│         Controllers · ViewModels · ViewComponents             │
│                   (ASP.NET Core MVC)                          │
├────────────────────────────────────────────────────────────────┤
│                    1_Application Layer                         │
│            Services (Contracts + Implementation)              │
│                         DTOs · Helpers                        │
├────────────────────────────────────────────────────────────────┤
│                   2_Infrastructure Layer                       │
│   EF Core (PostgreSQL) · MinIO · Brevo · RAG Client          │
│        Background Services · Auth Handlers · Middleware       │
├────────────────────────────────────────────────────────────────┤
│                      0_Domain Layer                           │
│            Entities · Enums · Common Abstractions            │
└────────────────────────────────────────────────────────────────┘
```

### Stack Tecnológico

| Categoría | Tecnología |
|---|---|
| **Framework** | ASP.NET Core 8.0 (MVC + Razor Views) |
| **ORM** | Entity Framework Core 8 + Npgsql |
| **Base de datos** | PostgreSQL 15 |
| **Almacenamiento** | MinIO (S3-compatible) |
| **Autenticación** | ASP.NET Core Identity + Cookie Auth + Esquema de dispositivos personalizado |
| **Inferencia ML** | Microsoft.ML.OnnxRuntime (modelo ONNX) |
| **Logging** | Serilog (JSON structured logging) |
| **Email** | Brevo SDK (`brevo_csharp`) |
| **PDF** | QuestPDF + SkiaSharp |
| **HTTP Policies** | Polly (Circuit Breaker) |
| **Contenerización** | Docker (multi-stage build) |
| **CI/CD** | GitHub Actions (Build · Lint · CodeQL · Deploy) |
| **Registro de imágenes** | GitHub Container Registry (GHCR) |

---

## 🗂️ Estructura del Proyecto

```
ArandanoIRTSoftware/
│
├── 0_Domain/                   # Capa de Dominio: Núcleo de negocio puro
│   ├── Entities/               # Entidades de base de datos (Plant, Crop, Device, etc.)
│   ├── Enums/                  # Enumeraciones del dominio (PlantStatus, DeviceStatus...)
│   └── Common/                 # Abstracciones compartidas (Result pattern, etc.)
│
├── 1_Application/              # Capa de Aplicación: Casos de uso y contratos
│   ├── Services/
│   │   ├── Contracts/          # Interfaces de servicios (IPlantService, ICropService...)
│   │   └── Implementation/     # Implementaciones de la lógica de negocio
│   ├── DTOs/                   # Objetos de transferencia de datos
│   └── Helper/                 # Utilidades de la capa de aplicación
│
├── 2_Infrastructure/           # Capa de Infraestructura: Implementaciones técnicas
│   ├── Data/                   # DbContext (EF Core) y configuraciones de entidades
│   ├── Services/               # Servicios externos (MinIO, Brevo, RAG, ONNX...)
│   ├── Authentication/         # Handlers de autenticación de dispositivos IoT
│   ├── Middleware/             # Middleware personalizado (auditoría de usuario)
│   ├── Settings/               # POCOs para configuración (IOptions<T>)
│   └── DependencyInjection.cs  # Registro centralizado de todos los servicios
│
├── 3_Presentation/             # Capa de Presentación: Controladores y ViewModels
│   ├── Controllers/
│   │   ├── Admin/              # Controladores del panel de administración (MVC)
│   │   └── Api/                # Controladores de la API REST para dispositivos IoT
│   ├── ViewModels/             # ViewModels tipados para cada vista
│   ├── ViewComponents/         # Componentes de vista reutilizables
│   └── Attributes/             # Atributos de validación personalizados
│
├── Views/                      # Vistas Razor (.cshtml) — organizadas por controlador
├── Assets/                     # Activos del servidor (modelo ONNX)
├── Migrations/                 # Migraciones de EF Core
├── ArandanoIRT.Tests/          # Proyecto de pruebas unitarias (xUnit)
│
├── .github/workflows/
│   ├── ci.yml                  # Pipeline CI: Build + Lint + CodeQL
│   └── deploy.yml              # Pipeline CD: Docker Build + Push + Deploy VPS
│
├── Dockerfile                  # Build multi-etapa optimizado para producción
├── Program.cs                  # Punto de entrada y bootstrap de la aplicación
└── ArandanoIRT.Web.csproj      # Definición del proyecto y dependencias NuGet
```

---

## 🚀 Inicio Rápido: Desarrollo Local

El entorno de desarrollo utiliza un **enfoque híbrido**: la aplicación .NET se ejecuta de forma nativa para un ciclo de depuración rápido, mientras que las dependencias de infraestructura (PostgreSQL, MinIO) corren en contenedores Docker.

### Prerrequisitos

- [Git](https://git-scm.com/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (o Docker Engine + Compose)
- Un IDE: [Visual Studio 2022+](https://visualstudio.microsoft.com/), [JetBrains Rider](https://www.jetbrains.com/rider/), o [VS Code](https://code.visualstudio.com/)

### Paso 1: Clonar los Repositorios

Ambos repositorios deben estar en la **misma carpeta raíz**:

```bash
mkdir ArandanoProject && cd ArandanoProject
git clone https://github.com/RFGRONA/ArandanoIRTSoftware.git
git clone https://github.com/RFGRONA/ArandanoIRTOps.git
```

La estructura resultante debe ser:
```
ArandanoProject/
├── ArandanoIRTSoftware/   ← Estás en el README de este repositorio
└── ArandanoIRTOps/
```

### Paso 2: Configurar los Servicios de Infraestructura (Docker)

Navega al repositorio de operaciones y crea el archivo de override para desarrollo local. **Este archivo no debe ser subido a Git** (ya está en `.gitignore`).

```bash
cd ArandanoIRTOps
```

Crea el archivo `docker-compose.override.yml` con el siguiente contenido:

```yaml
# docker-compose.override.yml
# Configuración exclusiva para desarrollo local. NO subir a Git.
version: '3.8'

services:
  postgres:
    ports:
      - "5433:5432"      # Expone PostgreSQL al host en el puerto 5433
    volumes:
      - postgres-data:/var/lib/postgresql/data

  minio:
    ports:
      - "9000:9000"      # API S3 de MinIO
      - "9001:9001"      # Consola web de MinIO
    volumes:
      - minio-data:/data

volumes:
  postgres-data:
  minio-data:
```

Levanta los servicios:
```bash
docker-compose up -d
```

> [!TIP]
> Puedes verificar el estado de los contenedores con `docker-compose ps`. La consola web de MinIO estará disponible en `http://localhost:9001` (usuario: `minioadmin`, contraseña: `minioadmin`).

### Paso 3: Configurar la Aplicación .NET

Regresa al directorio de la aplicación:
```bash
cd ../ArandanoIRTSoftware
```

Crea el archivo `appsettings.Development.json` en la raíz del proyecto. **Este archivo no debe ser subido a Git** (ya está en `.gitignore`), ya que contiene secretos locales.

```json
{
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Port=5433;Database=arandano_db;Username=user;Password=pass"
  },
  "Minio": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "BucketName": "arandano-dev",
    "UseSSL": false
  },
  "Rag": {
    "BaseUrl": "",
    "ApiKey": ""
  }
}
```

### Paso 4: Aplicar Migraciones de Base de Datos

```bash
dotnet ef database update
```

### Paso 5: Ejecutar la Aplicación

Abre la solución en tu IDE y ejecuta el proyecto (`F5`), o desde la terminal:

```bash
dotnet run
```

La aplicación estará disponible en `https://localhost:7XXX` (el puerto se muestra en la consola).

> [!NOTE]
> En el primer arranque, si no existe un usuario administrador, el sistema ejecuta un flujo de *bootstrap* accesible en la ruta `/Bootstrap` para crear las credenciales iniciales.

---

## 🚢 Despliegue en Producción

El despliegue en producción es **completamente automatizado** mediante GitHub Actions. Al hacer `push` a la rama `main`:

1. **`ci.yml`** (en ramas de feature): Compila el proyecto, verifica el formato del código con `dotnet format` y realiza análisis estático de seguridad con **GitHub CodeQL**.
2. **`deploy.yml`** (en `main`): Construye la imagen Docker con un **multi-stage build** optimizado, la publica en el **GitHub Container Registry (GHCR)** y despliega en el servidor VPS de producción vía SSH.

### Configuración de Secretos de GitHub

Para que el pipeline funcione, deben configurarse los siguientes secretos en el repositorio (`Settings > Secrets and variables > Actions`):

| Secreto | Descripción |
|---|---|
| `GHCR_TOKEN` | Personal Access Token con permisos `write:packages` para publicar en GHCR |
| `SSH_HOST` | Dirección IP o hostname del servidor VPS de producción |
| `SSH_USERNAME` | Usuario SSH del servidor |
| `SSH_PRIVATE_KEY` | Clave privada SSH (formato PEM) para autenticación sin contraseña |
| `SSH_FINGERPRINT` | Fingerprint del host SSH para verificación de identidad |

### Imagen Docker

La imagen se construye mediante un **Dockerfile multi-etapa** que separa las fases de compilación y ejecución, produciendo una imagen final ligera basada únicamente en el runtime de ASP.NET:

```dockerfile
# Etapa 1: Build (SDK completo)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Etapa 2: Final (solo runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
```

Para construir y ejecutar la imagen manualmente:

```bash
# Construir la imagen
docker build -t arandano-irt-app .

# Ejecutar el contenedor
docker run -d -p 8080:8080 \
  -e ConnectionStrings__PostgresConnection="..." \
  arandano-irt-app
```

---

## 🧪 Pruebas

El proyecto incluye un proyecto de pruebas unitarias (`ArandanoIRT.Tests`) basado en **xUnit**.

```bash
# Ejecutar todas las pruebas
dotnet test ArandanoIRT.Tests/ArandanoIRT.Tests.csproj --verbosity normal
```

---

## ⚙️ Variables de Configuración Clave

La aplicación se configura mediante el sistema estándar de `appsettings.json` de ASP.NET Core con soporte para múltiples entornos.

| Sección | Descripción |
|---|---|
| `ConnectionStrings:PostgresConnection` | Cadena de conexión a la base de datos PostgreSQL |
| `Minio` | Configuración del servicio de almacenamiento de objetos (endpoint, credenciales, bucket) |
| `Brevo` | Credenciales de la API de Brevo para el envío de correos electrónicos |
| `Rag` | URL base y API key del microservicio de IA (RAG) |
| `BackgroundJobs` | Intervalos de los servicios en segundo plano (análisis, inactividad) |
| `AnalysisParameters` | Parámetros globales por defecto para el cálculo del CWSI |
| `AnomalyParameters` | Parámetros globales por defecto para la detección de anomalías (ΔT) |
| `Turnstile` | Claves de Cloudflare Turnstile para protección anti-bot en el login |
| `AdminCredentials` | Credenciales del administrador raíz del sistema |

---

## 🤝 Contribuciones y Flujo de Trabajo

Este repositorio corresponde a un proyecto académico de grado. Las contribuciones externas no están abiertas en este momento, pero el código se publica con fines educativos bajo la licencia GPLv3.

### Ramas

| Rama | Propósito |
|---|---|
| `main` | Rama de producción. Los pushes aquí disparan el despliegue automático. |
| `feature/*` | Ramas de desarrollo de nuevas funcionalidades. Los pushes disparan el pipeline de CI. |

---

## 📄 Licencia

Este proyecto está distribuido bajo la licencia **GNU General Public License v3.0**. Consulta el archivo [LICENSE](./LICENSE) para más detalles.

---

<div align="center">
  <sub>Desarrollado como proyecto de grado en Ingeniería — 2026</sub>
</div>