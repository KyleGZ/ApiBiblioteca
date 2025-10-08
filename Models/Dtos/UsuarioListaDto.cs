namespace ApiBiblioteca.Models.Dtos
{
    public class UsuarioListaDto
    {
        public int IdUsuario { get; set; }
        public string Cedula { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Estado { get; set; } = null!;
        
    }
}
