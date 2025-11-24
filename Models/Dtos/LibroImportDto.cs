namespace ApiBiblioteca.Models.Dtos
{
    public class LibroImportDto
    {
        public string? Titulo { get; set; }
        public string? Isbn { get; set; }
        public string? Editorial { get; set; }
        public string? Seccion { get; set; }
        public string? Estado { get; set; }
        public string? Descripcion { get; set; }
        public string? PortadaUrl { get; set; }
        public string? Autores { get; set; }   
        public string? Generos { get; set; } 
    }
}
