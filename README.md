# Iglesia Asistencia - Sistema de Gestión Congregacional ⛪

Sistema profesional de escritorio desarrollado en **C# con WPF (.NET 8)** bajo el patrón **MVVM**, diseñado para la administración eficiente de miembros, visitas y control de asistencia en iglesias cristianas.

## 🌟 Características Principales

### 📋 Gestión de Directorio
- **Registro Inteligente:** Formulario dinámico que adapta sus campos según la categoría (Pastor, Diácono, Miembro, Visita, Seguimiento).
- **Hitos Espirituales:** Seguimiento detallado de fechas de nacimiento, aceptación a Cristo y bautismos.
- **Búsqueda Avanzada:** Filtrado en tiempo real por nombre en el directorio de miembros.

### ✅ Control de Asistencia y Seguimiento
- **Pase de Lista Rápido:** Interfaz optimizada para el marcado de asistencia diaria.
- **Gestión de Excusas:** Permite registrar motivos de inasistencia para un seguimiento pastoral más humano.
- **Alertas de Cumpleaños:** Visualización automática de miembros que cumplen años hoy o en la próxima semana.

### 📄 Reportes y Comunicación
- **Reportes PDF Profesionales:** Generación de documentos detallados utilizando **QuestPDF**, incluyendo logotipos institucionales y estadísticas por rango de fecha.
- **Integración con WhatsApp:** Envío de resúmenes de asistencia automáticos con un solo clic.
- **Exportación de Datos:** Capacidad de exportar reportes semanales, mensuales o históricos.

### 🛠️ Herramientas de Mantenimiento
- **Generador de Datos (Seed):** Herramienta integrada para generar registros de prueba y validar el comportamiento del sistema a gran escala.
- **Backups Automáticos:** El sistema realiza copias de seguridad de la base de datos local cada vez que se cierra la aplicación.

## 🏗️ Arquitectura Técnica
- **Framework:** .NET 8.0 (Windows)
- **UI:** WPF con **Material Design** y diseño Premium personalizado.
- **Patrón:** MVVM con **CommunityToolkit.Mvvm**.
- **Base de Datos:** SQLite local gestionado con **Entity Framework Core**.
- **Reportes:** QuestPDF (Motor de diseño de documentos).
- **Gráficos:** LiveCharts2 para visualización estadística.

## 📦 Instalación y Configuración
1. Clonar el repositorio.
2. Asegurarse de tener instalado el **SDK de .NET 8.0**.
3. Restaurar dependencias:
   ```bash
   dotnet restore
   ```
4. Ejecutar la aplicación:
   ```bash
   dotnet run
   ```
   *La base de datos SQLite se creará automáticamente en la carpeta local del usuario.*

## 📂 Estructura del Proyecto
- **/Models**: Definición de entidades (Persona, Asistencia, Categoría).
- **/ViewModels**: Lógica de negocio y enlace de datos (Patrón MVVM).
- **/Views**: Definición de interfaces de usuario en XAML.
- **/Services**: Servicios especializados (Generación de PDF, WhatsApp, Acceso a Datos).
- **/Data**: Contexto de base de datos y configuraciones globales.

---
*Desarrollado para la gloria de Dios y el servicio de su iglesia.*
