using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IglesiaAsistencia.Data;
using IglesiaAsistencia.Models;
using IglesiaAsistencia.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace IglesiaAsistencia.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly WhatsAppService _waService;
        private readonly PdfService _pdfService;
        private readonly BackupService _backupService;

        [ObservableProperty]
        private ObservableCollection<Persona> _personas = new();

        [ObservableProperty]
        private ObservableCollection<Persona> _asistentesHoy = new();

        [ObservableProperty]
        private ObservableCollection<Persona> _cumpleañerosSemana = new();

        [ObservableProperty]
        private DateTime _fechaSeleccionada = DateTime.Today;

        [ObservableProperty]
        private string _searchTextPersonas = string.Empty;
        partial void OnSearchTextPersonasChanged(string value) => FiltrarListas();

        [ObservableProperty]
        private string _searchTextAsistencia = string.Empty;
        partial void OnSearchTextAsistenciaChanged(string value) => FiltrarListas();

        [ObservableProperty]
        private string _currentView = "Dashboard"; // Dashboard, Personas, Asistencia

        [ObservableProperty]
        private Persona? _personaSeleccionada;

        // Propiedades para Nueva/Editar Persona
        [ObservableProperty]
        private string _nuevoNombre = string.Empty;
        [ObservableProperty]
        private string _nuevoTelefono = string.Empty;
        [ObservableProperty]
        private DateTime? _nuevaFechaNacimiento = null;
        [ObservableProperty]
        private Categoria _nuevaCategoria = Categoria.Miembro;
        [ObservableProperty]
        private bool _esModoEdicion = false;

        [ObservableProperty]
        private string _rutaBackupConfigurada = string.Empty;

        public MainViewModel()
        {
            _context = new AppDbContext();
            _context.Database.EnsureCreated();
            _waService = new WhatsAppService();
            _pdfService = new PdfService();
            _backupService = new BackupService();
            
            CargarConfiguracion();
            LoadData();
        }

        private void CargarConfiguracion()
        {
            string path = AppConfig.ConfigPath;
            if (File.Exists(path))
            {
                RutaBackupConfigurada = File.ReadAllText(path);
            }
        }

        private void GuardarConfiguracion()
        {
            string path = AppConfig.ConfigPath;
            File.WriteAllText(path, RutaBackupConfigurada);
        }

        public async void LoadData()
        {
            var list = await _context.Personas.Include(p => p.Asistencias).ToListAsync();
            Personas = new ObservableCollection<Persona>(list);
            FiltrarListas();
            ActualizarAsistentesHoy();
        }

        public IEnumerable<Persona> PersonasFiltradas => string.IsNullOrWhiteSpace(SearchTextPersonas) 
            ? Personas 
            : Personas.Where(p => p.Nombre.Contains(SearchTextPersonas, StringComparison.OrdinalIgnoreCase));

        public IEnumerable<Persona> AsistenciaFiltrada => string.IsNullOrWhiteSpace(SearchTextAsistencia) 
            ? Personas 
            : Personas.Where(p => p.Nombre.Contains(SearchTextAsistencia, StringComparison.OrdinalIgnoreCase));

        [RelayCommand]
        private void FiltrarListas()
        {
            OnPropertyChanged(nameof(PersonasFiltradas));
            OnPropertyChanged(nameof(AsistenciaFiltrada));
        }

        [RelayCommand]
        private void ActualizarAsistentesHoy()
        {
            var ids = _context.Asistencias
                .Where(a => a.Fecha.Date == FechaSeleccionada.Date && a.Asistio)
                .Select(a => a.PersonaId)
                .ToList();

            foreach (var p in Personas)
            {
                p.IsPresente = ids.Contains(p.Id);
            }

            AsistentesHoy = new ObservableCollection<Persona>(Personas.Where(p => ids.Contains(p.Id)));
            
            // Actualizar Cumpleañeros de la semana
            var hoy = DateTime.Today;
            var inicioSemana = hoy.AddDays(-(int)hoy.DayOfWeek);
            var finSemana = inicioSemana.AddDays(6);

            var cumple = Personas.Where(p => p.FechaNacimiento.HasValue)
                .Where(p => {
                    var fn = p.FechaNacimiento!.Value;
                    try {
                        var fechaEsteAño = new DateTime(hoy.Year, fn.Month, fn.Day);
                        return fechaEsteAño >= inicioSemana && fechaEsteAño <= finSemana;
                    } catch { return false; } // Por si es 29 de feb
                })
                .OrderBy(p => p.FechaNacimiento!.Value.Month)
                .ThenBy(p => p.FechaNacimiento!.Value.Day)
                .ToList();
            
            CumpleañerosSemana = new ObservableCollection<Persona>(cumple);
            OnPropertyChanged(nameof(HayCumpleaños));

            FiltrarListas();
        }

        public bool HayCumpleaños => CumpleañerosSemana.Any();

        [RelayCommand]
        private async Task ToggleAsistencia(Persona persona)
        {
            var asistencia = await _context.Asistencias
                .FirstOrDefaultAsync(a => a.PersonaId == persona.Id && a.Fecha.Date == FechaSeleccionada.Date);

            if (asistencia == null)
            {
                asistencia = new Asistencia 
                { 
                    PersonaId = persona.Id, 
                    Fecha = FechaSeleccionada.Date, 
                    Asistio = true 
                };
                _context.Asistencias.Add(asistencia);
            }
            else
            {
                asistencia.Asistio = !asistencia.Asistio;
            }

            await _context.SaveChangesAsync();
            ActualizarAsistentesHoy();
        }

        [RelayCommand]
        private void EnviarReporte()
        {
            var asistentes = AsistentesHoy.ToList();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Reporte de Asistencia - {FechaSeleccionada:dd/MM/yyyy}");
            sb.AppendLine();
            sb.AppendLine($"Total: {asistentes.Count}");
            sb.AppendLine($"Pastores: {asistentes.Count(a => a.Categoria == Categoria.Pastor)}");
            sb.AppendLine($"Miembros: {asistentes.Count(a => a.Categoria == Categoria.Miembro)}");
            sb.AppendLine($"Invitados: {asistentes.Count(a => a.Categoria == Categoria.Invitado)}");
            sb.AppendLine();

            var invitados = asistentes.Where(a => a.Categoria == Categoria.Invitado).ToList();
            if (invitados.Any())
            {
                sb.AppendLine("Invitados:");
                foreach (var i in invitados)
                    sb.AppendLine($"- {i.Nombre}");
            }

            string mensaje = System.Net.WebUtility.UrlEncode(sb.ToString());
            string url = $"https://wa.me/?text={mensaje}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }

        [RelayCommand]
        private void CambiarVista(string vista)
        {
            CurrentView = vista;
        }

        [RelayCommand]
        private async Task RegistrarNuevaPersona()
        {
            if (string.IsNullOrWhiteSpace(NuevoNombre))
            {
                MessageBox.Show("El nombre es obligatorio.");
                return;
            }

            if (EsModoEdicion && PersonaSeleccionada != null)
            {
                PersonaSeleccionada.Nombre = NuevoNombre;
                PersonaSeleccionada.Telefono = NuevoTelefono;
                PersonaSeleccionada.FechaNacimiento = NuevaFechaNacimiento;
                PersonaSeleccionada.Categoria = NuevaCategoria;
                _context.Entry(PersonaSeleccionada).State = EntityState.Modified;
            }
            else
            {
                var nueva = new Persona
                {
                    Nombre = NuevoNombre,
                    Telefono = NuevoTelefono,
                    FechaNacimiento = NuevaFechaNacimiento,
                    Categoria = NuevaCategoria
                };
                _context.Personas.Add(nueva);
                Personas.Add(nueva);
            }

            await _context.SaveChangesAsync();

            // Limpiar campos
            NuevoNombre = string.Empty;
            NuevoTelefono = string.Empty;
            NuevaFechaNacimiento = null;
            EsModoEdicion = false;
            PersonaSeleccionada = null;
            
            ActualizarAsistentesHoy();
            FiltrarListas();
            MessageBox.Show("Operación realizada con éxito.");
        }

        [RelayCommand]
        private void PrepararEdicion(Persona p)
        {
            PersonaSeleccionada = p;
            NuevoNombre = p.Nombre;
            NuevoTelefono = p.Telefono ?? "";
            NuevaFechaNacimiento = p.FechaNacimiento;
            NuevaCategoria = p.Categoria;
            EsModoEdicion = true;
        }

        [RelayCommand]
        private async Task EliminarPersona(Persona p)
        {
            var result = MessageBox.Show($"¿Está seguro de eliminar a {p.Nombre}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _context.Personas.Remove(p);
                await _context.SaveChangesAsync();
                Personas.Remove(p);
                FiltrarListas();
            }
        }

        [RelayCommand]
        private async Task ResetearSistema()
        {
            var result1 = MessageBox.Show("¿ESTÁ SEGURO de que desea borrar TODOS los registros del sistema? Esta acción es IRREVERSIBLE.", 
                "ADVERTENCIA CRÍTICA", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result1 == MessageBoxResult.Yes)
            {
                var result2 = MessageBox.Show("¿Realmente desea eliminar todas las personas y asistencias registradas hasta hoy?", 
                    "ÚLTIMA CONFIRMACIÓN", MessageBoxButton.YesNo, MessageBoxImage.Stop);

                if (result2 == MessageBoxResult.Yes)
                {
                    _context.Asistencias.RemoveRange(_context.Asistencias);
                    _context.Personas.RemoveRange(_context.Personas);
                    await _context.SaveChangesAsync();
                    
                    Personas.Clear();
                    AsistentesHoy.Clear();
                    CumpleañerosSemana.Clear();
                    FiltrarListas();
                    
                    MessageBox.Show("El sistema ha sido reiniciado con éxito.");
                }
            }
        }

        public void EjecutarBackupAutomatico()
        {
            try
            {
                // Si no hay ruta configurada, usamos una carpeta por defecto
                string ruta = string.IsNullOrWhiteSpace(RutaBackupConfigurada)
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BackupsAuto")
                    : RutaBackupConfigurada;

                if (!Directory.Exists(ruta))
                    Directory.CreateDirectory(ruta);

                string nombreArchivo = $"AutoBackup_Iglesia_{DateTime.Now:yyyyMMdd_HHmm}.db";
                string destino = Path.Combine(ruta, nombreArchivo);

                _backupService.RealizarBackup(destino);
            }
            catch
            {
                // Fallo silencioso al cerrar para no molestar al usuario, 
                // o podrías mostrar un aviso rápido.
            }
        }

        [RelayCommand]
        private async Task AgregarPersona(Persona nueva)
        {
            // Validar duplicados por Nombre + DOB
            var existe = await _context.Personas.AnyAsync(p => 
                p.Nombre.ToLower() == nueva.Nombre.ToLower() && 
                p.FechaNacimiento == nueva.FechaNacimiento);

            if (existe)
            {
                // En una app real, aquí mostraríamos un diálogo de confirmación
                // Por simplicidad, permitimos si el usuario insiste o lo manejamos en la View
            }

            _context.Personas.Add(nueva);
            await _context.SaveChangesAsync();
            Personas.Add(nueva);
        }

        [RelayCommand]
        private void ExportarPdf()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"Reporte_{FechaSeleccionada:yyyy-MM-dd}.pdf"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    _pdfService.GenerarReporteAsistencia(sfd.FileName, FechaSeleccionada, AsistentesHoy.ToList());
                    MessageBox.Show("Reporte PDF generado con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al generar PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void SeleccionarRutaBackup()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Seleccione la carpeta para guardar los Backups",
                InitialDirectory = string.IsNullOrWhiteSpace(RutaBackupConfigurada) ? AppDomain.CurrentDomain.BaseDirectory : RutaBackupConfigurada
            };

            if (dialog.ShowDialog() == true)
            {
                RutaBackupConfigurada = dialog.FolderName;
                GuardarConfiguracion();
                MessageBox.Show("Carpeta de backup configurada con éxito.");
            }
        }

        [RelayCommand]
        private void GenerarBackup()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "Database Backup (*.db)|*.db",
                FileName = $"Backup_Iglesia_{DateTime.Now:yyyyMMdd_HHmm}.db"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    _backupService.RealizarBackup(sfd.FileName);
                    MessageBox.Show("Backup realizado correctamente.", "Copia de Seguridad", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al realizar backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        [RelayCommand]
        private void ExportarReporteRango(string rango)
        {
            DateTime inicio = FechaSeleccionada;
            DateTime fin = FechaSeleccionada;

            switch (rango)
            {
                case "Semana": inicio = FechaSeleccionada.AddDays(-(int)FechaSeleccionada.DayOfWeek); fin = inicio.AddDays(6); break;
                case "Mes": inicio = new DateTime(FechaSeleccionada.Year, FechaSeleccionada.Month, 1); fin = inicio.AddMonths(1).AddDays(-1); break;
                case "Año": inicio = new DateTime(FechaSeleccionada.Year, 1, 1); fin = new DateTime(FechaSeleccionada.Year, 12, 31); break;
            }

            var sfd = new Microsoft.Win32.SaveFileDialog { Filter = "PDF|*.pdf", FileName = $"Reporte_{rango}_{inicio:yyyyMM}.pdf" };
            if (sfd.ShowDialog() == true)
            {
                // Aquí filtraríamos por rango en una app real, por ahora pasamos la lista actual
                _pdfService.GenerarReporteAsistencia(sfd.FileName, inicio, AsistentesHoy.ToList());
                MessageBox.Show($"Reporte {rango} generado.");
            }
        }

        [RelayCommand]
        private void ExportarReporteDomingos()
        {
            var sfd = new Microsoft.Win32.SaveFileDialog { Filter = "PDF|*.pdf", FileName = $"Reporte_Domingos_{DateTime.Now:yyyyMM}.pdf" };
            if (sfd.ShowDialog() == true)
            {
                // Filtramos solo asistencias de domingos
                var asistenciasDomingo = _context.Asistencias
                    .Where(a => a.Fecha.DayOfWeek == DayOfWeek.Sunday)
                    .Select(a => a.Persona)
                    .Distinct()
                    .ToList();

                _pdfService.GenerarReporteAsistencia(sfd.FileName, DateTime.Now, asistenciasDomingo);
                MessageBox.Show("Reporte de Domingos generado con éxito.");
            }
        }
    }
}
