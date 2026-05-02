using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IglesiaAsistencia.Models
{
    public partial class Persona : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Nombre { get; set; } = string.Empty;
        
        public string? Telefono { get; set; }
        
        public DateTime? FechaNacimiento { get; set; }
        
        private Categoria _categoria;
        public Categoria Categoria
        {
            get => _categoria;
            set => SetProperty(ref _categoria, value);
        }
        
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        
        // Días de compromiso
        public bool CompromisoLunes { get; set; }
        public bool CompromisoMartes { get; set; }
        public bool CompromisoMiercoles { get; set; }
        public bool CompromisoJueves { get; set; }
        public bool CompromisoViernes { get; set; }
        public bool CompromisoSabado { get; set; }
        public bool CompromisoDomingo { get; set; } = true;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        private bool _isPresente;
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsPresente 
        { 
            get => _isPresente; 
            set => SetProperty(ref _isPresente, value); 
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        private bool _isExcusa;
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsExcusa 
        { 
            get => _isExcusa; 
            set => SetProperty(ref _isExcusa, value); 
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        private string? _currentNotaExcusa;
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? CurrentNotaExcusa 
        { 
            get => _currentNotaExcusa; 
            set => SetProperty(ref _currentNotaExcusa, value); 
        }

        public string? QuienLoInvito { get; set; }
        
        public bool AceptoCristo { get; set; }
        public DateTime? FechaAceptoCristo { get; set; }
        
        public bool EstaBautizado { get; set; }
        public DateTime? FechaBautismo { get; set; }

        public int Edad
        {
            get
            {
                if (!FechaNacimiento.HasValue) return 30; // Valor por defecto si no hay fecha
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
