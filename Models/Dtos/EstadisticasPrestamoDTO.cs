namespace ApiBiblioteca.Models.Dtos
{
    public class EstadisticasPrestamosDTO
    {
        public int PrestamosMes { get; set; }
        public int PrestamosActivos { get; set; }
        public int PrestamosDevueltos { get; set; }
        public int LibrosDisponibles { get; set; }
        public int LibrosPrestados { get; set; }
        public List<PrestamosPorDiaDTO> PrestamosPorDia { get; set; } = new();
        public List<LibrosMasPrestadosDTO> LibrosMasPrestados { get; set; } = new();
    }

    public class PrestamosPorDiaDTO
    {
        public string Fecha { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class LibrosMasPrestadosDTO
    {
        public string Titulo { get; set; } = string.Empty;
        public int VecesPrestado { get; set; }
    }
}