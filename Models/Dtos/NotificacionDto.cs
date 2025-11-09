namespace ApiBiblioteca.Models.Dtos
{
    public class NotificacionDto
    {
        public int IdUsuario { get; set; }
        public string Asunto { get; set; }
        public string Mensaje { get; set; }
        public bool enviarCorreo { get; set; }
    }
}
