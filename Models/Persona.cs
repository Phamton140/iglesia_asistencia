using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace IglesiaAsistencia.Models
{
    public partial class Persona : ObservableValidator
    {
        [Key]
        public int Id { get; set; }
        
        [ObservableProperty]
        [Required]
        private string _nombre = string.Empty;
        
        [ObservableProperty]
        private string? _telefono;
        
        [ObservableProperty]
        private DateTime? _fechaNacimiento;
        
        [ObservableProperty]
        private Categoria _categoria;
        
        [ObservableProperty]
        private DateTime _fechaRegistro = DateTime.Now;
        
        // Días de compromiso
        [ObservableProperty] private bool _compromisoLunes;
        [ObservableProperty] private bool _compromisoMartes;
        [ObservableProperty] private bool _compromisoMiercoles;
        [ObservableProperty] private bool _compromisoJueves;
        [ObservableProperty] private bool _compromisoViernes;
        [ObservableProperty] private bool _compromisoSabado;
        [ObservableProperty] private bool _compromisoDomingo = true;

        [property: System.ComponentModel.DataAnnotations.Schema.NotMapped]
        [ObservableProperty]
        private bool _isPresente;

        [property: System.ComponentModel.DataAnnotations.Schema.NotMapped]
        [ObservableProperty]
        private bool _isExcusa;

        [property: System.ComponentModel.DataAnnotations.Schema.NotMapped]
        [ObservableProperty]
        private string? _currentNotaExcusa;

        [ObservableProperty]
        private string? _quienLoInvito;
        
        [ObservableProperty]
        private bool _aceptoCristo;
        
        [ObservableProperty]
        private bool _estaBautizado;

        public int Edad
        {
            get
            {
                if (!FechaNacimiento.HasValue) return 30; // Valor por defecto para compatibilidad UI
                return EdadReal;
            }
        }

        public int EdadReal
        {
            get
            {
                if (!FechaNacimiento.HasValue) return 0;
                var hoy = DateTime.Today;
                var edad = hoy.Year - FechaNacimiento.Value.Year;
                if (FechaNacimiento.Value.Date > hoy.AddYears(-edad)) edad--;
                return edad;
            }
        }

        // Navigation property for attendance
        public virtual ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();

        // Helper property to determine if it's a guest and their status
        public bool EsVisita => Categoria == Categoria.Visita;
        
        public int ContadorVisitas => Asistencias?.Count ?? 0;
        
        public string TipoVisita => ContadorVisitas <= 1 ? "Primera vez" : "Recurrente";
        
        public bool EsCumpleañosHoy => FechaNacimiento.HasValue && 
                                       FechaNacimiento.Value.Month == DateTime.Today.Month && 
                                       FechaNacimiento.Value.Day == DateTime.Today.Day;

        public bool CumplioEstaSemana => FechaNacimiento.HasValue && 
                                        EstaEnLosUltimos7Dias(FechaNacimiento.Value);

        private bool EstaEnLosUltimos7Dias(DateTime fecha)
        {
            var hoy = DateTime.Today;
            var cumpleEsteAño = new DateTime(hoy.Year, fecha.Month, fecha.Day);
            var diferencia = (hoy - cumpleEsteAño).TotalDays;
            return diferencia >= 0 && diferencia <= 7;
        }
    }
}
