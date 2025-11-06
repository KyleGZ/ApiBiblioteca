namespace ApiBiblioteca.Models.Dtos
{
    public class ReservaResponseDto
    {
        public int IdReserva { get; set; }
        public int IdUsuario { get; set; }
        public int IdLibro { get; set; }
        public DateTime FechaReserva { get; set; }
        public int Prioridad { get; set; }
        public string Estado { get; set; } = null!;
        public string? TituloLibro { get; set; }
        public string? NombreUsuario { get; set; }
        public string? Isbn { get; set; }
    }
}
