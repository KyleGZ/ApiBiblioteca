namespace ApiBiblioteca.Models.Dtos
{
    public class AutorizacionResponse
    {
        public string Token { get; set; }
        public bool Resultado { get; set; }
        public string Msj { get; set; }  // Asegúrate que sea "Msj" no "Msg"
        public int idUsuario { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public List<string> Roles { get; set; }
    }
}
