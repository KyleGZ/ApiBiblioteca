namespace ApiBiblioteca.Models.Dtos
{
    public class ObtenerLibroEditar
    {
        public int IdLibro { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public int EditorialId { get; set; }
        public int SeccionId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string PortadaUrl { get; set; } = string.Empty;
        public List<AutorChipDto> Autores { get; set; } = new List<AutorChipDto>();
        public List<GeneroChipDto> Generos { get; set; } = new List<GeneroChipDto>();
    }
    public class AutorChipDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class GeneroChipDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

}
