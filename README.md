# Sistema de Gestión de Asistencia - Iglesia

Este es un sistema profesional para la gestión de asistencia de congregaciones cristianas, desarrollado en C# con WPF y MVVM.

## 🚀 Características
- **Gestión de Personas:** Registro único con control de duplicados (Nombre + Fecha Nacimiento).
- **Control de Asistencia:** Marcado rápido y eficiente para cultos semanales.
- **Seguimiento de Invitados:** Clasificación automática entre "Nuevo" y "Recurrente".
- **Dashboard Visual:** Resumen de estadísticas y tendencias.
- **Sistema de Cumpleaños:** Alertas automáticas para cumpleaños de la semana.
- **Reportes WhatsApp:** Generación y envío de reportes detallados con un solo clic.

## 🛠️ Requisitos Técnicos
- **SDK .NET 8.0**
- **SQLite** (Base de datos local incluida en el proyecto)
- **NuGet Packages:**
  - `Microsoft.EntityFrameworkCore.Sqlite`
  - `MaterialDesignThemes`
  - `CommunityToolkit.Mvvm`
  - `LiveChartsCore.SkiaSharpView.WPF`

## 📦 Instrucciones de Ejecución
1. Abre el proyecto en **Visual Studio 2022**.
2. Restaura los paquetes NuGet (`dotnet restore`).
3. Compila y ejecuta (`F5`).
4. La base de datos `iglesia.db` se creará automáticamente en la carpeta de ejecución.

## 📊 Formato de Reporte WhatsApp
El sistema genera automáticamente un mensaje estructurado para WhatsApp:
- Totales por categoría.
- Lista de cumpleaños hoy y en la semana.
- Lista detallada de invitados nuevos y recurrentes.
