using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ApiBiblioteca.Services
{
    public interface IEstadisticasService
    {
        Task<EstadisticasPrestamosDTO> ObtenerEstadisticasPrestamosAsync();
    }

    public class EstadisticasService : IEstadisticasService
    {
        private readonly DbContextBiblioteca _context;

        public EstadisticasService(DbContextBiblioteca context)
        {
            _context = context;
        }

        public async Task<EstadisticasPrestamosDTO> ObtenerEstadisticasPrestamosAsync()
        {
            var fechaInicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var fechaFinMes = fechaInicioMes.AddMonths(1).AddDays(-1);

            var estadisticas = new EstadisticasPrestamosDTO();

            // Prestamos del mes actual
            estadisticas.PrestamosMes = await _context.Prestamos
                .Where(p => p.FechaPrestamo >= fechaInicioMes && p.FechaPrestamo <= fechaFinMes)
                .CountAsync();

            // Prestamos activos (no devueltos)
            estadisticas.PrestamosActivos = await _context.Prestamos
                .Where(p => p.FechaDevolucionReal == null && p.Estado == "Activo")
                .CountAsync();

            // Prestamos devueltos este mes
            estadisticas.PrestamosDevueltos = await _context.Prestamos
                .Where(p => p.FechaDevolucionReal >= fechaInicioMes && p.FechaDevolucionReal <= fechaFinMes)
                .CountAsync();

            // Total de libros
            var totalLibros = await _context.Libros.CountAsync();
            estadisticas.LibrosPrestados = estadisticas.PrestamosActivos;
            estadisticas.LibrosDisponibles = totalLibros - estadisticas.LibrosPrestados;

            // Prestamos por día del mes actual - CORREGIDO
            var prestamosPorDia = await _context.Prestamos
                .Where(p => p.FechaPrestamo >= fechaInicioMes && p.FechaPrestamo <= fechaFinMes)
                .Select(p => new { Fecha = p.FechaPrestamo.Date })
                .ToListAsync(); // Ejecutar en base de datos

            // Procesar en memoria
            estadisticas.PrestamosPorDia = prestamosPorDia
                .GroupBy(p => p.Fecha)
                .Select(g => new PrestamosPorDiaDTO
                {
                    Fecha = g.Key.ToString("dd/MM"),
                    Cantidad = g.Count()
                })
                .OrderBy(x => x.Fecha)
                .ToList();

            // Libros más prestados - CORREGIDO
            var librosPrestados = await _context.Prestamos
                .Include(p => p.IdLibroNavigation)
                .Select(p => new {
                    p.IdLibro,
                    Titulo = p.IdLibroNavigation.Titulo
                })
                .ToListAsync(); // Ejecutar en base de datos

            // Procesar en memoria
            estadisticas.LibrosMasPrestados = librosPrestados
                .GroupBy(p => new { p.IdLibro, p.Titulo })
                .Select(g => new LibrosMasPrestadosDTO
                {
                    Titulo = g.Key.Titulo,
                    VecesPrestado = g.Count()
                })
                .OrderByDescending(x => x.VecesPrestado)
                .Take(10)
                .ToList();

            return estadisticas;
        }
    }
}