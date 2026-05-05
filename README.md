# Iglesia Asistencia - Sistema de GestiÃ³n Congregacional â›ª

Sistema profesional de escritorio desarrollado en **C# con WPF (.NET 8)** bajo el patrÃ³n **MVVM**, diseÃ±ado para la administraciÃ³n eficiente de miembros, visitas y control de asistencia en iglesias cristianas.

## ðŸŒŸ CaracterÃ­sticas Principales

### ðŸ“‹ GestiÃ³n de Directorio
- **Registro Inteligente:** Formulario dinÃ¡mico que adapta sus campos segÃºn la categorÃ­a (Pastor, DiÃ¡cono, Miembro, Visita, Seguimiento).
- **Hitos Espirituales:** Seguimiento de fecha de nacimiento, aceptaciÃ³n a Cristo y bautismos.
- **SimplificaciÃ³n de Interfaz:** Vista de directorio optimizada que muestra solo la informaciÃ³n esencial (Nombre, CategorÃ­a).

### âœ… Control de Asistencia y Seguimiento
- **Pase de Lista RÃ¡pido:** Interfaz optimizada para el marcado de asistencia diaria con soporte para fechas pasadas.
- **GestiÃ³n de Excusas:** Permite registrar motivos de inasistencia para un seguimiento pastoral mÃ¡s humano.
- **Alertas de CumpleaÃ±os:** VisualizaciÃ³n automÃ¡tica de miembros que cumplen aÃ±os en la semana actual.

### ðŸ“„ Reportes y EstadÃ­sticas
- **Reportes PDF Detallados:** GeneraciÃ³n de documentos profesionales con **QuestPDF**, incluyendo logotipos y firmas.
- **DistribuciÃ³n por Edades:** EstadÃ­sticas automÃ¡ticas en los reportes que desglosan la asistencia por rangos (NiÃ±os, Adolescentes, JÃ³venes, Adultos, Adultos Mayores).
- **IntegraciÃ³n con WhatsApp:** EnvÃ­o de resÃºmenes de asistencia automÃ¡ticos para grupos de liderazgo.

### ðŸ› ï¸  Herramientas de Mantenimiento
- **Backups AutomÃ¡ticos:** El sistema realiza copias de seguridad de la base de datos local (.db) automÃ¡ticamente al cerrar el programa.
- **Migraciones AutomÃ¡ticas:** GestiÃ³n transparente de actualizaciones de la base de datos para asegurar la integridad de la informaciÃ³n.

## ðŸ —ï¸  Arquitectura TÃ©cnica
- **Framework:** .NET 8.0 Windows.
- **UI:** WPF con diseÃ±o Premium personalizado (Estilo Dark/Modern).
- **PatrÃ³n:** MVVM con **CommunityToolkit.Mvvm**.
- **Base de Datos:** SQLite gestionado con **Entity Framework Core**.
- **Reportes:** QuestPDF.

## ðŸ“📦 PublicaciÃ³n e InstalaciÃ³n
Para generar una versiÃ³n ejecutable portable:
1. Abrir una terminal en la carpeta del proyecto.
2. Ejecutar el comando de publicaciÃ³n:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
   ```
3. El ejecutable se encontrarÃ¡ en `bin\Release\net8.0-windows\win-x64\publish\IglesiaAsistencia.exe`.

---
*Desarrollado para el servicio de la obra de Dios.*
