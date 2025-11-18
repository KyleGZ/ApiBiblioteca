using System.ComponentModel.DataAnnotations;

namespace ApiBiblioteca.Models.Dtos
{
    public class BitacoraRequest
    {
        [Required(ErrorMessage = "El IdUsuario es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El IdUsuario debe ser mayor a 0")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "La acción es requerida")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "La acción debe tener entre 1 y 100 caracteres")]
        public string Accion { get; set; }

        [Required(ErrorMessage = "La tabla afectada es requerida")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "La tabla afectada debe tener entre 1 y 50 caracteres")]
        public string TablaAfectada { get; set; }

        [Required(ErrorMessage = "El IdRegistro es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El IdRegistro debe ser mayor a 0")]
        public int IdRegistro { get; set; }
    }
}
