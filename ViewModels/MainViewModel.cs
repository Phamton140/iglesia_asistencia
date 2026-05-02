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
        partial void OnNuevoNombreChanged(string value)
        {
            if (value != null && value != value.ToUpper())
            {
                NuevoNombre = value.ToUpper();
            }
        }
        [ObservableProperty]
        private string _nuevoTelefono = string.Empty;
        partial void OnNuevoTelefonoChanged(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            var digitsOnly = new string(value.Where(char.IsDigit).ToArray());
            if (digitsOnly.Length > 10) digitsOnly = digitsOnly.Substring(0, 10);
            string formatted = digitsOnly;
            if (digitsOnly.Length > 3 && digitsOnly.Length <= 6)
                formatted = $"{digitsOnly.Substring(0, 3)}-{digitsOnly.Substring(3)}";
            else if (digitsOnly.Length > 6)
                formatted = $"{digitsOnly.Substring(0, 3)}-{digitsOnly.Substring(3, 3)}-{digitsOnly.Substring(6)}";

            if (value != formatted)
            {
                NuevoTelefono = formatted;
            }
        }
        [ObservableProperty]
        private DateTime? _nuevaFechaNacimiento = null;
        [ObservableProperty]
        private Categoria _nuevaCategoria = Categoria.Miembro;
        [ObservableProperty]
        private bool _esModoEdicion = false;

        // Propiedades de compromiso para nueva/editar persona
        [ObservableProperty] private bool _nuevaCompromisoLunes;
        [ObservableProperty] private bool _nuevaCompromisoMartes;
        [ObservableProperty] private bool _nuevaCompromisoMiercoles;
        [ObservableProperty] private bool _nuevaCompromisoJueves;
        [ObservableProperty] private bool _nuevaCompromisoViernes;
        [ObservableProperty] private bool _nuevaCompromisoSabado;
        [ObservableProperty] private bool _nuevaCompromisoDomingo = true;

        [ObservableProperty]
        private string _rutaBackupConfigurada = string.Empty;

        [ObservableProperty]
        private string _rutaReportesConfigurada = string.Empty;

        [ObservableProperty]
        private string _nombreIglesia = "Comunidad Del Reino";
        partial void OnNombreIglesiaChanged(string value) => GuardarConfiguracion();

        [ObservableProperty]
        private string? _logoPath;

        [ObservableProperty]
        private Categoria _categoriaConfig = Categoria.Miembro;
        partial void OnCategoriaConfigChanged(Categoria value) => CargarDiasDefault();

        [ObservableProperty] private bool _defDom;
        [ObservableProperty] private bool _defLun;
        [ObservableProperty] private bool _defMar;
        [ObservableProperty] private bool _defMie;
        [ObservableProperty] private bool _defJue;
        [ObservableProperty] private bool _defVie;
        [ObservableProperty] private bool _defSab;

        private Dictionary<string, bool[]> _defaultCommitments = new();

        [ObservableProperty]
        private string _nuevaQuienLoInvito = string.Empty;

        [ObservableProperty]
        private bool _visitoHoy = false;

        [ObservableProperty]
        private DateTime? _nuevaFechaAceptoCristo;

        [ObservableProperty]
        private bool _nuevaAceptoCristo;
        partial void OnNuevaAceptoCristoChanged(bool value)
        {
            if (value && NuevaCategoria == Categoria.Visita)
            {
                NuevaCategoria = Categoria.Seguimiento;
                // Al pasar a seguimiento, la fecha de nacimiento debe estar vacía para que la elijan
                NuevaFechaNacimiento = null;

                // Si estamos editando, guardar inmediatamente en BD
                if (EsModoEdicion && PersonaSeleccionada != null)
                {
                    PersonaSeleccionada.Categoria = Categoria.Seguimiento;
                    PersonaSeleccionada.AceptoCristo = true;
                    if (!PersonaSeleccionada.FechaAceptoCristo.HasValue)
                        PersonaSeleccionada.FechaAceptoCristo = DateTime.Today;
                    PersonaSeleccionada.FechaNacimiento = null;

                    _context.Entry(PersonaSeleccionada).State = EntityState.Modified;
                    _ = _context.SaveChangesAsync();

                    FiltrarListas();
                }
            }
        }

        [ObservableProperty]
        private DateTime? _nuevaFechaBautismo;

        partial void OnNuevaFechaAceptoCristoChanged(DateTime? value)
        {
            if (value.HasValue) NuevaAceptoCristo = true;
        }

        partial void OnNuevaFechaBautismoChanged(DateTime? value)
        {
            if (value.HasValue) NuevaEstaBautizado = true;
        }

        [ObservableProperty]
        private bool _nuevaEstaBautizado;
        partial void OnNuevaEstaBautizadoChanged(bool value)
        {
            if (value)
            {
                if (!NuevaFechaNacimiento.HasValue)
                {
                    MessageBox.Show("Para marcar un bautismo es obligatorio ingresar la fecha de nacimiento primero.", "Requisito faltante", MessageBoxButton.OK, MessageBoxImage.Warning);
                    
                    // Usar Dispatcher para revertir el check sin causar conflictos de concurrencia en la UI
                    Application.Current.Dispatcher.InvokeAsync(() => NuevaEstaBautizado = false);
                    return;
                }

                if (NuevaCategoria == Categoria.Seguimiento || NuevaCategoria == Categoria.Visita)
                {
                    NuevaCategoria = Categoria.Miembro;
                }
            }
        }

        partial void OnNuevaCategoriaChanged(Categoria value)
        {
            // Solo aplicar defaults si NO estamos en modo edición (nuevo registro)
            if (!EsModoEdicion && _defaultCommitments.TryGetValue(value.ToString(), out var days))
            {
                NuevaCompromisoDomingo = days[0];
                NuevaCompromisoLunes = days[1];
                NuevaCompromisoMartes = days[2];
                NuevaCompromisoMiercoles = days[3];
                NuevaCompromisoJueves = days[4];
                NuevaCompromisoViernes = days[5];
                NuevaCompromisoSabado = days[6];
            }
        }

        public MainViewModel()
        {
            _context = new AppDbContext();
            _context.Database.EnsureCreated();
            EjecutarMigraciones();

            _waService = new WhatsAppService();
            _pdfService = new PdfService();
            _backupService = new BackupService();
            
            CargarConfiguracion();
            LoadData();
        }

        private void EjecutarMigraciones()
        {
            // Crear tabla de control para que cada migración solo corra UNA vez
            _context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS __Migraciones (
                    Id TEXT PRIMARY KEY,
                    Fecha TEXT
                );");

            void Migrar(string id, Action accion)
            {
                var count = _context.Database
                    .SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM __Migraciones WHERE Id = '{id}'")
                    .First();
                if (count == 0)
                {
                    try { accion(); } catch { }
                    _context.Database.ExecuteSqlRaw(
                        $"INSERT INTO __Migraciones(Id, Fecha) VALUES('{id}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}')");
                }
            }

            // Columnas nuevas (seguras de repetir, pero las controlamos igual)
            Migrar("col_QuienLoInvito", () => _context.Database.ExecuteSqlRaw("ALTER TABLE Personas ADD COLUMN QuienLoInvito TEXT;"));
            Migrar("col_AceptoCristo", () => _context.Database.ExecuteSqlRaw("ALTER TABLE Personas ADD COLUMN AceptoCristo INTEGER DEFAULT 0;"));
            Migrar("col_FechaAceptoCristo", () => _context.Database.ExecuteSqlRaw("ALTER TABLE Personas ADD COLUMN FechaAceptoCristo TEXT;"));
            Migrar("col_EstaBautizado", () => _context.Database.ExecuteSqlRaw("ALTER TABLE Personas ADD COLUMN EstaBautizado INTEGER DEFAULT 0;"));
            Migrar("col_FechaBautismo", () => _context.Database.ExecuteSqlRaw("ALTER TABLE Personas ADD COLUMN FechaBautismo TEXT;"));

            // Migración del enum shift al eliminar Adolescente
            // Antes: Pastor=0, Diacono=1, Miembro=2, Adolescente=3, Visita=4, Seguimiento=5
            // Ahora: Pastor=0, Diacono=1, Miembro=2, Visita=3, Seguimiento=4
            Migrar("enum_shift_v1", () =>
            {
                _context.Database.ExecuteSqlRaw("UPDATE Personas SET Categoria = 99 WHERE Categoria = 5;"); // viejo Seguimiento → temp
                _context.Database.ExecuteSqlRaw("UPDATE Personas SET Categoria = 3 WHERE Categoria = 4;");  // viejo Visita → nuevo Visita
                _context.Database.ExecuteSqlRaw("UPDATE Personas SET Categoria = 2 WHERE Categoria = 3 AND AceptoCristo = 0 AND EstaBautizado = 0 AND (CompromisoLunes=0 AND CompromisoMartes=0 AND CompromisoMiercoles=0 AND CompromisoJueves=0 AND CompromisoViernes=0 AND CompromisoSabado=0 AND CompromisoDomingo=0);"); // Adolescentes → Miembro
                _context.Database.ExecuteSqlRaw("UPDATE Personas SET Categoria = 4 WHERE Categoria = 99;"); // temp → nuevo Seguimiento
            });

            // Recuperar Visitas que fueron incorrectamente movidas a Miembro por el bug de migraciones repetidas
            // Heurística: Miembro sin bautismo, sin AceptoCristo, sin ningún compromiso = era Visita
            Migrar("recovery_visitas_v1", () =>
            {
                _context.Database.ExecuteSqlRaw(@"
                    UPDATE Personas SET Categoria = 3
                    WHERE Categoria = 2
                    AND AceptoCristo = 0
                    AND EstaBautizado = 0
                    AND CompromisoLunes = 0
                    AND CompromisoMartes = 0
                    AND CompromisoMiercoles = 0
                    AND CompromisoJueves = 0
                    AND CompromisoViernes = 0
                    AND CompromisoSabado = 0
                    AND CompromisoDomingo = 0;");
            });
        }

        private void CargarConfiguracion()
        {
            var config = AppConfig.Load();
            RutaBackupConfigurada = config.RutaBackup;
            RutaReportesConfigurada = config.RutaReportes;
            NombreIglesia = config.NombreIglesia;
            LogoPath = config.LogoPath;
            _defaultCommitments = config.DefaultCommitments;
            CargarDiasDefault();
        }

        private void GuardarConfiguracion()
        {
            var config = new ConfigData
            {
                RutaBackup = RutaBackupConfigurada,
                RutaReportes = RutaReportesConfigurada,
                NombreIglesia = NombreIglesia,
                LogoPath = LogoPath,
                DefaultCommitments = _defaultCommitments
            };
            AppConfig.Save(config);
        }

        private void CargarDiasDefault()
        {
            if (_defaultCommitments.TryGetValue(CategoriaConfig.ToString(), out var days))
            {
                DefDom = days[0];
                DefLun = days[1];
                DefMar = days[2];
                DefMie = days[3];
                DefJue = days[4];
                DefVie = days[5];
                DefSab = days[6];
            }
        }

        [RelayCommand]
        private void GuardarDiasDefault()
        {
            _defaultCommitments[CategoriaConfig.ToString()] = new[] { DefDom, DefLun, DefMar, DefMie, DefJue, DefVie, DefSab };
            GuardarConfiguracion();
            MessageBox.Show($"Configuración para {CategoriaConfig} guardada.");
        }

        [RelayCommand]
        private void SeleccionarLogo()
        {
            var ofd = new OpenFileDialog { Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp" };
            if (ofd.ShowDialog() == true)
            {
                LogoPath = ofd.FileName;
                GuardarConfiguracion();
                MessageBox.Show("Logo cargado correctamente. Se utilizará en todos los reportes PDF generados a partir de ahora.", "Logo Actualizado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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

        public IEnumerable<Persona> AsistenciaFiltrada
        {
            get
            {
                var dia = FechaSeleccionada.DayOfWeek;
                var list = Personas.AsEnumerable();
                
                // Filtrar por compromiso del día seleccionado
                // Los Miembros/Líderes/Seguimiento se filtran por compromiso semanal.
                // Las Visitas SOLO aparecen si asistieron ese día (ya marcado en el directorio).
                list = list.Where(p => 
                {
                    if (p.Categoria == Categoria.Visita)
                    {
                        return p.IsPresente; // Ya cargado en ActualizarAsistentesHoy
                    }

                    return dia switch {
                        DayOfWeek.Monday => p.CompromisoLunes,
                        DayOfWeek.Tuesday => p.CompromisoMartes,
                        DayOfWeek.Wednesday => p.CompromisoMiercoles,
                        DayOfWeek.Thursday => p.CompromisoJueves,
                        DayOfWeek.Friday => p.CompromisoViernes,
                        DayOfWeek.Saturday => p.CompromisoSabado,
                        DayOfWeek.Sunday => p.CompromisoDomingo,
                        _ => false
                    };
                });

                if (!string.IsNullOrWhiteSpace(SearchTextAsistencia))
                    list = list.Where(p => p.Nombre.Contains(SearchTextAsistencia, StringComparison.OrdinalIgnoreCase));
                    
                return list.ToList();
            }
        }

        [RelayCommand]
        private void FiltrarListas()
        {
            OnPropertyChanged(nameof(PersonasFiltradas));
            OnPropertyChanged(nameof(AsistenciaFiltrada));
        }

        [RelayCommand]
        private void ActualizarAsistentesHoy()
        {
            var asistencias = _context.Asistencias
                .Where(a => a.Fecha.Date == FechaSeleccionada.Date)
                .ToList();

            foreach (var p in Personas)
            {
                var reg = asistencias.FirstOrDefault(a => a.PersonaId == p.Id);
                p.IsPresente = reg?.Asistio ?? false;
                p.IsExcusa = reg?.EsExcusa ?? false;
                p.CurrentNotaExcusa = reg?.NotaExcusa;
            }

            var idsAsistentes = asistencias.Where(a => a.Asistio).Select(a => a.PersonaId).ToList();
            AsistentesHoy = new ObservableCollection<Persona>(Personas.Where(p => idsAsistentes.Contains(p.Id)));
            
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
                    Asistio = true,
                    EsExcusa = false
                };
                _context.Asistencias.Add(asistencia);
            }
            else
            {
                asistencia.Asistio = !asistencia.Asistio;
                if (asistencia.Asistio) asistencia.EsExcusa = false; 
            }

            // ACTUALIZACIÓN INMEDIATA (Antes del await para evitar lag en reportes)
            persona.IsPresente = asistencia.Asistio;
            persona.IsExcusa = asistencia.EsExcusa;

            if (persona.IsPresente)
            {
                if (!AsistentesHoy.Any(a => a.Id == persona.Id))
                    AsistentesHoy.Add(persona);
            }
            else
            {
                var existing = AsistentesHoy.FirstOrDefault(a => a.Id == persona.Id);
                if (existing != null) AsistentesHoy.Remove(existing);
            }

            FiltrarListas();

            await _context.SaveChangesAsync();
        }

        [RelayCommand]
        private async Task ToggleExcusa(Persona persona)
        {
            var asistencia = await _context.Asistencias
                .FirstOrDefaultAsync(a => a.PersonaId == persona.Id && a.Fecha.Date == FechaSeleccionada.Date);

            if (asistencia == null)
            {
                asistencia = new Asistencia 
                { 
                    PersonaId = persona.Id, 
                    Fecha = FechaSeleccionada.Date, 
                    Asistio = false,
                    EsExcusa = true 
                };
                _context.Asistencias.Add(asistencia);
            }
            else
            {
                asistencia.EsExcusa = !asistencia.EsExcusa;
                if (asistencia.EsExcusa) asistencia.Asistio = false; 
            }

            // ACTUALIZACIÓN INMEDIATA
            persona.IsPresente = asistencia.Asistio;
            persona.IsExcusa = asistencia.EsExcusa;

            var existingExcusa = AsistentesHoy.FirstOrDefault(a => a.Id == persona.Id);
            if (existingExcusa != null) AsistentesHoy.Remove(existingExcusa);

            FiltrarListas();

            await _context.SaveChangesAsync();
        }

        [RelayCommand]
        private async Task GuardarNotaExcusa(Persona persona)
        {
            var asistencia = await _context.Asistencias
                .FirstOrDefaultAsync(a => a.PersonaId == persona.Id && a.Fecha.Date == FechaSeleccionada.Date);

            if (asistencia != null)
            {
                asistencia.NotaExcusa = persona.CurrentNotaExcusa;
                await _context.SaveChangesAsync();
            }
        }

        [RelayCommand]
        private void EnviarReporte()
        {
            var asistentes = AsistentesHoy.ToList();
            var Visitas = asistentes.Where(a => a.Categoria == Categoria.Visita).ToList();
            var excusas = Personas.Where(p => p.IsExcusa).ToList();
            
            // Ausentes con compromiso: Estaban en AsistenciaFiltrada pero no asistieron ni tienen excusa
            var ausentes = AsistenciaFiltrada.Where(p => !p.IsPresente && !p.IsExcusa && p.Categoria != Categoria.Visita).ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"*Reporte de Asistencia - {FechaSeleccionada:dd/MM/yyyy}*");
            sb.AppendLine();
            sb.AppendLine($"👥 *Total Presentes:* {asistentes.Count}");
            sb.AppendLine();
            
            if (Visitas.Any())
            {
                sb.AppendLine("*NUESTRAS VISITAS (VISITAS):*");
                foreach (var i in Visitas)
                {
                    sb.AppendLine($"- {i.Nombre}");
                }
                sb.AppendLine();
            }

            if (ausentes.Any())
            {
                sb.AppendLine("*HERMANOS AUSENTES (Con compromiso hoy):*");
                foreach (var a in ausentes)
                {
                    sb.AppendLine($"- {a.Nombre}");
                }
                sb.AppendLine();
            }

            if (excusas.Any())
            {
                sb.AppendLine("*EXCUSAS PRESENTADAS:*");
                foreach (var e in excusas)
                {
                    string nota = string.IsNullOrWhiteSpace(e.CurrentNotaExcusa) ? "Sin descripción" : e.CurrentNotaExcusa;
                    sb.AppendLine($"- *{e.Nombre}*: {nota}");
                }
            }

            string mensaje = System.Net.WebUtility.UrlEncode(sb.ToString());
            string url = $"https://wa.me/?text={mensaje}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }

        [RelayCommand]
        private void CambiarVista(string vista)
        {
            CurrentView = vista;
            LimpiarFormulario();
        }

        [RelayCommand]
        public void LimpiarFormulario()
        {
            NuevoNombre = string.Empty;
            NuevoTelefono = string.Empty;
            NuevaFechaNacimiento = null;
            NuevaCategoria = Categoria.Miembro;
            NuevaQuienLoInvito = string.Empty;
            VisitoHoy = false;
            NuevaAceptoCristo = false;
            NuevaFechaAceptoCristo = null;
            NuevaEstaBautizado = false;
            NuevaFechaBautismo = null;
            
            NuevaCompromisoLunes = false;
            NuevaCompromisoMartes = false;
            NuevaCompromisoMiercoles = false;
            NuevaCompromisoJueves = false;
            NuevaCompromisoViernes = false;
            NuevaCompromisoSabado = false;
            NuevaCompromisoDomingo = true;

            EsModoEdicion = false;
            PersonaSeleccionada = null;
        }

        [RelayCommand]
        private async Task RegistrarNuevaPersona()
        {
            if (string.IsNullOrWhiteSpace(NuevoNombre))
            {
                MessageBox.Show("El nombre es obligatorio.");
                return;
            }

            Persona pToRecord;

            if (EsModoEdicion && PersonaSeleccionada != null)
            {
                PersonaSeleccionada.Nombre = NuevoNombre;
                PersonaSeleccionada.Telefono = NuevoTelefono;
                PersonaSeleccionada.FechaNacimiento = NuevaCategoria == Categoria.Visita ? null : NuevaFechaNacimiento;
                
                // Actualizar Aceptó a Cristo: Priorizar fecha del DatePicker
                PersonaSeleccionada.AceptoCristo = NuevaAceptoCristo || NuevaFechaAceptoCristo.HasValue;
                PersonaSeleccionada.FechaAceptoCristo = NuevaFechaAceptoCristo ?? (PersonaSeleccionada.AceptoCristo ? (PersonaSeleccionada.FechaAceptoCristo ?? DateTime.Today) : null);

                // Actualizar Bautismo: Priorizar fecha del DatePicker
                PersonaSeleccionada.EstaBautizado = NuevaEstaBautizado || NuevaFechaBautismo.HasValue;
                PersonaSeleccionada.FechaBautismo = NuevaFechaBautismo ?? (PersonaSeleccionada.EstaBautizado ? (PersonaSeleccionada.FechaBautismo ?? DateTime.Today) : null);

                PersonaSeleccionada.Categoria = NuevaCategoria;
                PersonaSeleccionada.QuienLoInvito = NuevaCategoria == Categoria.Visita ? NuevaQuienLoInvito : null;
                
                if (NuevaCategoria == Categoria.Visita)
                {
                    PersonaSeleccionada.CompromisoLunes = false;
                    PersonaSeleccionada.CompromisoMartes = false;
                    PersonaSeleccionada.CompromisoMiercoles = false;
                    PersonaSeleccionada.CompromisoJueves = false;
                    PersonaSeleccionada.CompromisoViernes = false;
                    PersonaSeleccionada.CompromisoSabado = false;
                    PersonaSeleccionada.CompromisoDomingo = false;
                }
                else
                {
                    PersonaSeleccionada.CompromisoLunes = NuevaCompromisoLunes;
                    PersonaSeleccionada.CompromisoMartes = NuevaCompromisoMartes;
                    PersonaSeleccionada.CompromisoMiercoles = NuevaCompromisoMiercoles;
                    PersonaSeleccionada.CompromisoJueves = NuevaCompromisoJueves;
                    PersonaSeleccionada.CompromisoViernes = NuevaCompromisoViernes;
                    PersonaSeleccionada.CompromisoSabado = NuevaCompromisoSabado;
                    PersonaSeleccionada.CompromisoDomingo = NuevaCompromisoDomingo;
                }

                _context.Entry(PersonaSeleccionada).State = EntityState.Modified;
                pToRecord = PersonaSeleccionada;
            }
            else
            {
                var nueva = new Persona
                {
                    Nombre = NuevoNombre,
                    Telefono = NuevoTelefono,
                    FechaNacimiento = NuevaCategoria == Categoria.Visita ? null : NuevaFechaNacimiento,
                    Categoria = NuevaCategoria,
                    AceptoCristo = NuevaAceptoCristo || NuevaFechaAceptoCristo.HasValue,
                    FechaAceptoCristo = NuevaFechaAceptoCristo ?? (NuevaAceptoCristo ? DateTime.Today : null),
                    EstaBautizado = NuevaEstaBautizado || NuevaFechaBautismo.HasValue,
                    FechaBautismo = NuevaFechaBautismo ?? (NuevaEstaBautizado ? DateTime.Today : null),
                    QuienLoInvito = NuevaCategoria == Categoria.Visita ? NuevaQuienLoInvito : null,
                    CompromisoLunes = NuevaCategoria != Categoria.Visita && NuevaCompromisoLunes,
                    CompromisoMartes = NuevaCategoria != Categoria.Visita && NuevaCompromisoMartes,
                    CompromisoMiercoles = NuevaCategoria != Categoria.Visita && NuevaCompromisoMiercoles,
                    CompromisoJueves = NuevaCategoria != Categoria.Visita && NuevaCompromisoJueves,
                    CompromisoViernes = NuevaCategoria != Categoria.Visita && NuevaCompromisoViernes,
                    CompromisoSabado = NuevaCategoria != Categoria.Visita && NuevaCompromisoSabado,
                    CompromisoDomingo = NuevaCategoria != Categoria.Visita && NuevaCompromisoDomingo
                };
                _context.Personas.Add(nueva);
                Personas.Add(nueva);
                pToRecord = nueva;
            }

            // Guardar persona primero para tener ID
            await _context.SaveChangesAsync();

            // Si es Visita y se marcó "Visito Hoy", registrar asistencia
            if (NuevaCategoria == Categoria.Visita && VisitoHoy)
            {
                var asistencia = await _context.Asistencias
                    .FirstOrDefaultAsync(a => a.PersonaId == pToRecord.Id && a.Fecha.Date == DateTime.Today);
                
                if (asistencia == null)
                {
                    asistencia = new Asistencia { PersonaId = pToRecord.Id, Fecha = DateTime.Today, Asistio = true };
                    _context.Asistencias.Add(asistencia);
                }
                else
                {
                    asistencia.Asistio = true;
                    asistencia.EsExcusa = false;
                }
                await _context.SaveChangesAsync();
            }

            // Limpiar campos
            NuevoNombre = string.Empty;
            NuevoTelefono = string.Empty;
            NuevaFechaNacimiento = null;
            NuevaCategoria = Categoria.Miembro;
            NuevaQuienLoInvito = string.Empty;
            VisitoHoy = false;
            NuevaAceptoCristo = false;
            NuevaFechaAceptoCristo = null;
            NuevaEstaBautizado = false;
            NuevaFechaBautismo = null;
            
            NuevaCompromisoLunes = false;
            NuevaCompromisoMartes = false;
            NuevaCompromisoMiercoles = false;
            NuevaCompromisoJueves = false;
            NuevaCompromisoViernes = false;
            NuevaCompromisoSabado = false;
            NuevaCompromisoDomingo = true;

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
            NuevaQuienLoInvito = p.QuienLoInvito ?? "";
            VisitoHoy = p.IsPresente;
            NuevaAceptoCristo = p.AceptoCristo;
            NuevaFechaAceptoCristo = p.FechaAceptoCristo;
            NuevaEstaBautizado = p.EstaBautizado;
            NuevaFechaBautismo = p.FechaBautismo;
            
            NuevaCompromisoLunes = p.CompromisoLunes;
            NuevaCompromisoMartes = p.CompromisoMartes;
            NuevaCompromisoMiercoles = p.CompromisoMiercoles;
            NuevaCompromisoJueves = p.CompromisoJueves;
            NuevaCompromisoViernes = p.CompromisoViernes;
            NuevaCompromisoSabado = p.CompromisoSabado;
            NuevaCompromisoDomingo = p.CompromisoDomingo;

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
                FileName = $"Reporte_{FechaSeleccionada:yyyy-MM-dd}.pdf",
                InitialDirectory = string.IsNullOrWhiteSpace(RutaReportesConfigurada) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : RutaReportesConfigurada
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var asistentes = AsistentesHoy.ToList();
                    var excusas = Personas.Where(p => p.IsExcusa).ToList();
                    var ausentes = AsistenciaFiltrada.Where(p => !p.IsPresente && !p.IsExcusa && p.Categoria != Categoria.Visita).ToList();

                    _pdfService.GenerarReporteDiarioDetallado(sfd.FileName, FechaSeleccionada, asistentes, excusas, ausentes, NombreIglesia, LogoPath);
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
        private void SeleccionarRutaReportes()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Seleccione la carpeta para guardar los Reportes PDF",
                InitialDirectory = string.IsNullOrWhiteSpace(RutaReportesConfigurada) ? AppDomain.CurrentDomain.BaseDirectory : RutaReportesConfigurada
            };

            if (dialog.ShowDialog() == true)
            {
                RutaReportesConfigurada = dialog.FolderName;
                GuardarConfiguracion();
                MessageBox.Show("Carpeta de reportes configurada con éxito.");
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
        private async Task ExportarReporteRango(string rango)
        {
            DateTime inicio = FechaSeleccionada;
            DateTime fin = FechaSeleccionada;

            switch (rango)
            {
                case "Semana": 
                    inicio = FechaSeleccionada.AddDays(-(int)FechaSeleccionada.DayOfWeek); 
                    fin = inicio.AddDays(6); 
                    break;
                case "Mes": 
                    inicio = new DateTime(FechaSeleccionada.Year, FechaSeleccionada.Month, 1); 
                    fin = inicio.AddMonths(1).AddDays(-1); 
                    break;
                case "Año": 
                    inicio = new DateTime(FechaSeleccionada.Year, 1, 1); 
                    fin = new DateTime(FechaSeleccionada.Year, 12, 31); 
                    break;
            }

            var sfd = new Microsoft.Win32.SaveFileDialog 
            { 
                Filter = "PDF|*.pdf", 
                FileName = $"Reporte_{rango}_{inicio:yyyyMMdd}.pdf",
                InitialDirectory = string.IsNullOrWhiteSpace(RutaReportesConfigurada) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : RutaReportesConfigurada
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    // 1. Obtener todas las asistencias y excusas en el rango
                    var registrosRango = await _context.Asistencias
                        .Where(a => a.Fecha.Date >= inicio.Date && a.Fecha.Date <= fin.Date)
                        .ToListAsync();

                    // 2. Obtener las fechas únicas en las que hubo algún tipo de servicio (asistencia o excusa)
                    var fechasServicio = registrosRango.Select(a => a.Fecha.Date).Distinct().ToList();

                    if (!fechasServicio.Any())
                    {
                        MessageBox.Show("No hay registros en el rango seleccionado.", "Información");
                        return;
                    }

                    // 3. Calcular datos por persona considerando sus días de compromiso y excusas
                    var todasLasPersonas = await _context.Personas.ToListAsync();
                    var datosReporte = todasLasPersonas.Select(p => {
                        // Fechas de servicio que coinciden con el compromiso de esta persona
                        var fechasCompromiso = fechasServicio.Where(f => f.DayOfWeek switch {
                            DayOfWeek.Monday => p.CompromisoLunes,
                            DayOfWeek.Tuesday => p.CompromisoMartes,
                            DayOfWeek.Wednesday => p.CompromisoMiercoles,
                            DayOfWeek.Thursday => p.CompromisoJueves,
                            DayOfWeek.Friday => p.CompromisoViernes,
                            DayOfWeek.Saturday => p.CompromisoSabado,
                            DayOfWeek.Sunday => p.CompromisoDomingo,
                            _ => false
                        }).ToList();

                        int totalPotencial = fechasCompromiso.Count;
                        if (totalPotencial == 0) return null; // No tenía compromiso en los días que hubo servicio

                        var misRegistros = registrosRango.Where(r => r.PersonaId == p.Id && fechasCompromiso.Contains(r.Fecha.Date)).ToList();
                        int asistencias = misRegistros.Count(r => r.Asistio);
                        int excusas = misRegistros.Count(r => r.EsExcusa);

                        // El total efectivo excluye las excusas del denominador
                        int totalEfectivo = totalPotencial - excusas;

                        return new PersonaReporteDto
                        {
                            Nombre = p.Nombre,
                            Categoria = p.Categoria,
                            AsistenciasRealizadas = asistencias,
                            TotalServicios = totalEfectivo > 0 ? totalEfectivo : asistencias // Evitar división por cero si todas fueron excusas
                        };
                    })
                    .Where(d => d != null)
                    .Cast<PersonaReporteDto>()
                    .Where(d => d.AsistenciasRealizadas > 0 || d.Categoria != Categoria.Visita)
                    .ToList();

                    _pdfService.GenerarReporteDetallado(sfd.FileName, $"Reporte de Asistencia ({rango})", inicio, fin, datosReporte, NombreIglesia, LogoPath);
                    MessageBox.Show($"Reporte {rango} generado con éxito.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al generar el reporte: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task ExportarReporteDomingos()
        {
            var sfd = new Microsoft.Win32.SaveFileDialog 
            { 
                Filter = "PDF|*.pdf", 
                FileName = $"Reporte_Domingos_{DateTime.Now:yyyyMM}.pdf",
                InitialDirectory = string.IsNullOrWhiteSpace(RutaReportesConfigurada) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : RutaReportesConfigurada
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    // 1. Obtener todas las asistencias de domingos
                    var asistenciasDomingo = await _context.Asistencias
                        .Where(a => a.Fecha.DayOfWeek == DayOfWeek.Sunday && a.Asistio)
                        .ToListAsync();

                    // 2. Obtener el total de domingos con asistencia
                    int totalDomingos = asistenciasDomingo.Select(a => a.Fecha.Date).Distinct().Count();

                    if (totalDomingos == 0)
                    {
                        MessageBox.Show("No hay registros de asistencia en domingos.", "Información");
                        return;
                    }

                    // 3. Calcular datos por persona
                    var todasLasPersonas = await _context.Personas.ToListAsync();
                    var datosReporte = todasLasPersonas.Select(p => new PersonaReporteDto
                    {
                        Nombre = p.Nombre,
                        Categoria = p.Categoria,
                        AsistenciasRealizadas = asistenciasDomingo.Count(a => a.PersonaId == p.Id),
                        TotalServicios = totalDomingos
                    })
                    .Where(d => d.AsistenciasRealizadas > 0 || d.Categoria != Categoria.Visita)
                    .ToList();

                    var primerDomingo = asistenciasDomingo.Min(a => a.Fecha);
                    var ultimoDomingo = asistenciasDomingo.Max(a => a.Fecha);

                    _pdfService.GenerarReporteDetallado(sfd.FileName, "Reporte Histórico de Domingos", primerDomingo, ultimoDomingo, datosReporte, NombreIglesia, LogoPath);
                    MessageBox.Show("Reporte de Domingos generado con éxito.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al generar el reporte: {ex.Message}");
                }
            }
        }
    }
}
