using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IglesiaAsistencia.Models
{
    public class Persona
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Nombre { get; set; } = string.Empty;
        
        public string? Telefono { get; set; }
        
        public DateTime? FechaNacimiento { get; set; }
        
        public Categoria Categoria { get; set; }
        
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsPresente { get; set; }

        // Navigation property for attendance
        public virtual ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();

        // Helper property to determine if it's a guest and their status
        public bool EsInvitado => Categoria == Categoria.Invitado;
        
        public int ContadorVisitas => Asistencias?.Count ?? 0;
        
        public string TipoInvitado => ContadorVisitas <= 1 ? "🆕 Nuevo" : "🔁 Recurrente";
        
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
