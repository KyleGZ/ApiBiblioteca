namespace ApiBiblioteca.Models.Dtos
{
    public class LibroListaView
    {
        public int IdLibro { get; set; }
        public string Titulo { get; set; }
        public string ISBN { get; set; }
        public List<string> Autor { get; set; } = new();
        public string Editorial { get; set; }
        public List<string> Genero { get; set; } = new();
        public string Estado { get; set; }
        public string PortadaUrl { get; set; }
    }
}
