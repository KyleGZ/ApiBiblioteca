namespace ApiBiblioteca.Models.Dtos
{
    

    public class PerfilUsuario
    {
        public int idUsuario { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Cedula { get; set; }
        public string? Password { get; set; }

    }
}
