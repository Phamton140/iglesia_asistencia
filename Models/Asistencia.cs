using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IglesiaAsistencia.Models
{
    public class Asistencia
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PersonaId { get; set; }
        
        [ForeignKey("PersonaId")]
        public virtual Persona Persona { get; set; } = null!;
        
        [Required]
        public DateTime Fecha { get; set; }
        
        public bool Asistio { get; set; } = true;
        
        public bool EsExcusa { get; set; }
        
        public string? NotaExcusa { get; set; }
    }
}
