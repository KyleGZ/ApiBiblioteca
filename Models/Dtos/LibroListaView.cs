namespace ApiBiblioteca.Models.Dtos
{
    public class LibroListaView
    {
        public int IdLibro { get; set; }
        public string Titulo { get; set; }
        public string ISBN { get; set; }
        public string Autor { get; set; }
        public string Editorial { get; set; }
        public string Genero { get; set; }
        public string Estado { get; set; }
        public string PortadaUrl { get; set; }
    }
}
