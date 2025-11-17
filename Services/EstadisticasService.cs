using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ApiBiblioteca.Services
{
    public interface IEstadisticasService
    {
        Task<EstadisticasPrestamosDTO> ObtenerEstadisticasPrestamosAsync();
        Task<EstadisticasPrestamosDTO> ObtenerEstadisticasPorRangoAsync(FiltroEstadisticasDTO filtro);
        Task<byte[]> GenerarReporteExcelAsync(FiltroEstadisticasDTO filtro);

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

            var filtro = new FiltroEstadisticasDTO
            {
                FechaInicio = fechaInicioMes,
                FechaFin = fechaFinMes
            };

            return await ObtenerEstadisticasPorRangoAsync(filtro);
        }

        public async Task<EstadisticasPrestamosDTO> ObtenerEstadisticasPorRangoAsync(FiltroEstadisticasDTO filtro)
        {
            var fechaInicio = filtro.FechaInicio ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var fechaFin = filtro.FechaFin ?? DateTime.Now.Date;

            var estadisticas = new EstadisticasPrestamosDTO
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };

            // Prestamos del rango de fechas
            estadisticas.PrestamosMes = await _context.Prestamos
                .Where(p => p.FechaPrestamo >= fechaInicio && p.FechaPrestamo <= fechaFin)
                .CountAsync();

            // Prestamos activos (no devueltos)
            estadisticas.PrestamosActivos = await _context.Prestamos
                .Where(p => p.FechaDevolucionReal == null && p.Estado == "Activo")
                .CountAsync();

            // Prestamos devueltos en el rango
            estadisticas.PrestamosDevueltos = await _context.Prestamos
                .Where(p => p.FechaDevolucionReal >= fechaInicio && p.FechaDevolucionReal <= fechaFin)
                .CountAsync();

            // Total de libros
            var totalLibros = await _context.Libros.CountAsync();
            estadisticas.LibrosPrestados = estadisticas.PrestamosActivos;
            estadisticas.LibrosDisponibles = totalLibros - estadisticas.LibrosPrestados;

            // Prestamos por día
            estadisticas.PrestamosPorDia = await ObtenerPrestamosPorDiaOptimizadoAsync(fechaInicio, fechaFin);

            // Libros más prestados
            estadisticas.LibrosMasPrestados = await ObtenerLibrosMasPrestadosAsync(fechaInicio, fechaFin);

            return estadisticas;
        }

        private async Task<List<PrestamosPorDiaDTO>> ObtenerPrestamosPorDiaOptimizadoAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            var fechasPrestamos = await _context.Prestamos
                .Where(p => p.FechaPrestamo >= fechaInicio && p.FechaPrestamo <= fechaFin)
                .Select(p => p.FechaPrestamo.Date)
                .ToListAsync();

            return fechasPrestamos
                .GroupBy(f => f)
                .Select(g => new PrestamosPorDiaDTO
                {
                    Fecha = g.Key.ToString("dd/MM"),
                    Cantidad = g.Count()
                })
                .OrderBy(x => DateTime.ParseExact(x.Fecha, "dd/MM", null))
                .ToList();
        }

        private async Task<List<LibrosMasPrestadosDTO>> ObtenerLibrosMasPrestadosAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            var datos = await _context.Prestamos
                .Include(p => p.IdLibroNavigation)
                .Where(p => p.FechaPrestamo >= fechaInicio && p.FechaPrestamo <= fechaFin)
                .Select(p => new { p.IdLibro, Titulo = p.IdLibroNavigation.Titulo })
                .ToListAsync();

            return datos
                .GroupBy(x => new { x.IdLibro, x.Titulo })
                .Select(g => new LibrosMasPrestadosDTO
                {
                    Titulo = g.Key.Titulo,
                    VecesPrestado = g.Count()
                })
                .OrderByDescending(x => x.VecesPrestado)
                .Take(10)
                .ToList();
        }

        public async Task<byte[]> GenerarReporteExcelAsync(FiltroEstadisticasDTO filtro)
        {
            var estadisticas = await ObtenerEstadisticasPorRangoAsync(filtro);

            using var package = new OfficeOpenXml.ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Reporte Préstamos");

            // Título y rango de fechas
            worksheet.Cells[1, 1].Value = "Reporte de Préstamos - Biblioteca Pública de Esparza";
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[2, 1].Value = $"Período: {estadisticas.FechaInicio:dd/MM/yyyy} - {estadisticas.FechaFin:dd/MM/yyyy}";
            worksheet.Cells[2, 1].Style.Font.Bold = true;

            // Estadísticas generales
            worksheet.Cells[4, 1].Value = "Estadísticas Generales";
            worksheet.Cells[4, 1].Style.Font.Bold = true;
            worksheet.Cells[4, 1].Style.Font.Size = 14;

            var statsData = new[]
            {
                new[] { "Concepto", "Cantidad" },
                new[] { "Préstamos del período", estadisticas.PrestamosMes.ToString() },
                new[] { "Préstamos activos", estadisticas.PrestamosActivos.ToString() },
                new[] { "Préstamos devueltos", estadisticas.PrestamosDevueltos.ToString() },
                new[] { "Libros disponibles", estadisticas.LibrosDisponibles.ToString() },
                new[] { "Libros prestados", estadisticas.LibrosPrestados.ToString() }
            };

            for (int i = 0; i < statsData.Length; i++)
            {
                worksheet.Cells[5 + i, 1].Value = statsData[i][0];
                worksheet.Cells[5 + i, 2].Value = statsData[i][1];
            }

            // Préstamos por día
            worksheet.Cells[12, 1].Value = "Préstamos por Día";
            worksheet.Cells[12, 1].Style.Font.Bold = true;
            worksheet.Cells[12, 1].Style.Font.Size = 14;

            worksheet.Cells[13, 1].Value = "Fecha";
            worksheet.Cells[13, 2].Value = "Cantidad";
            worksheet.Cells[13, 1].Style.Font.Bold = true;
            worksheet.Cells[13, 2].Style.Font.Bold = true;

            int row = 14;
            foreach (var item in estadisticas.PrestamosPorDia)
            {
                worksheet.Cells[row, 1].Value = item.Fecha;
                worksheet.Cells[row, 2].Value = item.Cantidad;
                row++;
            }

            // Libros más prestados
            worksheet.Cells[12, 4].Value = "Libros Más Prestados";
            worksheet.Cells[12, 4].Style.Font.Bold = true;
            worksheet.Cells[12, 4].Style.Font.Size = 14;

            worksheet.Cells[13, 4].Value = "Título";
            worksheet.Cells[13, 5].Value = "Veces Prestado";
            worksheet.Cells[13, 4].Style.Font.Bold = true;
            worksheet.Cells[13, 5].Style.Font.Bold = true;

            row = 14;
            foreach (var libro in estadisticas.LibrosMasPrestados)
            {
                worksheet.Cells[row, 4].Value = libro.Titulo;
                worksheet.Cells[row, 5].Value = libro.VecesPrestado;
                row++;
            }

            // Autoajustar columnas
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }
    }
}