using System.ComponentModel.DataAnnotations;

namespace ApiBiblioteca.Models.Dtos
{
    public class CrearPrestamoDto
    {
        [Required(ErrorMessage = "El usuario es requerido")]
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El libro es requerido")]
        public int LibroId { get; set; }

        [Required(ErrorMessage = "La fecha de préstamo es requerida")]
        [DataType(DataType.Date)]
        public DateTime FechaPrestamo { get; set; }

        [Required(ErrorMessage = "La fecha de vencimiento es requerida")]
        [DataType(DataType.Date)]
        public DateTime FechaVencimiento { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }
    }
}
