namespace ApiBiblioteca.Models.Dtos
{
    public class PaginacionResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<T> Data { get; set; } = new List<T>();
        public PaginationInfo Pagination { get; set; } = new PaginationInfo();
    }

    public class PaginationInfo
    {
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalResultados { get; set; }
        public int ResultadosPorPagina { get; set; }
    }
}
