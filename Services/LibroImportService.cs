using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Settings;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Transactions;
using Microsoft.Extensions.Options;

namespace ApiBiblioteca.Services
{
    public class LibroImportService : ILibroImportService
    {
        private readonly DbContextBiblioteca _context;
        private readonly ImportDefaults _defaults;


        public LibroImportService(DbContextBiblioteca context,
            IOptions<ImportDefaults> defaults)
        {
            _context = context;
            _defaults = defaults.Value;
        }


        public async Task<ApiResponse> ImportarLibrosDesdeExcelAsync(IFormFile archivoExcel)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            var detallesErrores = new List<string>();
            int total = 0, insertados = 0, errores = 0;

            try
            {
                using var stream = new MemoryStream();
                await archivoExcel.CopyToAsync(stream);
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    return new ApiResponse { Success = false, Message = "No se encontró una hoja en el archivo." };

                int rowCount = worksheet.Dimension.Rows;

                // Caches locales para evitar duplicados en una sola ejecución
                var editorialesCache = new Dictionary<string, Editorial>(StringComparer.OrdinalIgnoreCase);
                var seccionesCache = new Dictionary<string, Seccion>(StringComparer.OrdinalIgnoreCase);
                var autoresCache = new Dictionary<string, Autor>(StringComparer.OrdinalIgnoreCase);
                var generosCache = new Dictionary<string, Genero>(StringComparer.OrdinalIgnoreCase);

                for (int row = 2; row <= rowCount; row++)
                {
                    total++;
                    try
                    {
                        string titulo = worksheet.Cells[row, 1].Text?.Trim() ?? "";
                        string isbn = worksheet.Cells[row, 2].Text?.Trim() ?? "";
                        string editorialNombre = worksheet.Cells[row, 3].Text?.Trim();
                        string seccionNombre = worksheet.Cells[row, 4].Text?.Trim();
                        string estado = worksheet.Cells[row, 5].Text?.Trim() ?? "Disponible";
                        string descripcion = worksheet.Cells[row, 6].Text?.Trim() ?? "";
                        string portadaUrl = worksheet.Cells[row, 7].Text?.Trim() ?? "";
                        string autoresCelda = worksheet.Cells[row, 8].Text?.Trim();
                        string generosCelda = worksheet.Cells[row, 9].Text?.Trim();

                        // Validar ISBN único
                        if (await _context.Libros.AnyAsync(l => l.Isbn == isbn))
                            throw new Exception($"El ISBN '{isbn}' ya existe.");

                        // ===== EDITORIAL =====
                        string nombreEditorial = string.IsNullOrWhiteSpace(editorialNombre)
                            ? _defaults.EditorialPorDefecto
                            : editorialNombre;

                        if (!editorialesCache.TryGetValue(nombreEditorial, out var editorial))
                        {
                            editorial = await _context.Editorials
                                .FirstOrDefaultAsync(e => e.Nombre.ToLower() == nombreEditorial.ToLower());
                            if (editorial == null)
                            {
                                editorial = new Editorial { Nombre = nombreEditorial };
                                _context.Editorials.Add(editorial);
                            }
                            editorialesCache[nombreEditorial] = editorial;
                        }

                        // ===== SECCION =====
                        string nombreSeccion = string.IsNullOrWhiteSpace(seccionNombre)
                            ? _defaults.SeccionPorDefecto
                            : seccionNombre;

                        if (!seccionesCache.TryGetValue(nombreSeccion, out var seccion))
                        {
                            seccion = await _context.Seccions
                                .FirstOrDefaultAsync(s => s.Nombre.ToLower() == nombreSeccion.ToLower());
                            if (seccion == null)
                            {
                                seccion = new Seccion { Nombre = nombreSeccion,
                                                        Ubicacion = nombreSeccion};
                                _context.Seccions.Add(seccion);
                            }
                            seccionesCache[nombreSeccion] = seccion;
                        }

                        // ===== LIBRO =====
                        var libro = new Libro
                        {
                            Titulo = titulo,
                            Isbn = isbn,
                            Descripcion = descripcion,
                            Estado = estado,
                            PortadaUrl = portadaUrl,
                            IdEditorialNavigation = editorial,
                            IdSeccionNavigation = seccion
                        };

                        // ===== AUTORES =====
                        if (!string.IsNullOrWhiteSpace(autoresCelda))
                        {
                            var autoresNombres = autoresCelda.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            foreach (var nombreAutor in autoresNombres)
                            {
                                if (!autoresCache.TryGetValue(nombreAutor, out var autor))
                                {
                                    autor = await _context.Autors
                                        .FirstOrDefaultAsync(a => a.Nombre.ToLower() == nombreAutor.ToLower());
                                    if (autor == null)
                                    {
                                        autor = new Autor { Nombre = nombreAutor };
                                        _context.Autors.Add(autor);
                                    }
                                    autoresCache[nombreAutor] = autor;
                                }
                                libro.IdAutors.Add(autor);
                            }
                        }

                        // ===== GENEROS =====
                        if (!string.IsNullOrWhiteSpace(generosCelda))
                        {
                            var generosNombres = generosCelda.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            foreach (var nombreGenero in generosNombres)
                            {
                                if (!generosCache.TryGetValue(nombreGenero, out var genero))
                                {
                                    genero = await _context.Generos
                                        .FirstOrDefaultAsync(g => g.Nombre.ToLower() == nombreGenero.ToLower());
                                    if (genero == null)
                                    {
                                        genero = new Genero { Nombre = nombreGenero };
                                        _context.Generos.Add(genero);
                                    }
                                    generosCache[nombreGenero] = genero;
                                }
                                libro.IdGeneros.Add(genero);
                            }
                        }

                        _context.Libros.Add(libro);
                        insertados++;
                    }
                    catch (Exception exFila)
                    {
                        errores++;
                        detallesErrores.Add($"Fila {row}: {exFila.Message}");
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                /*
                 * Nuevos cambios
                 */

                bool importacionExitosa = insertados > 0 && errores == 0;
                bool importacionParcial = insertados > 0 && errores > 0;
                bool importacionFallida = insertados == 0 && errores > 0;
                bool archivoVacio = total == 0;

                if (archivoVacio)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "El archivo Excel está vacío. No se importaron libros.",
                        Data = new { total, insertados, errores, detallesErrores }
                    };

                }else if (importacionExitosa)
                {
                    return new ApiResponse
                    {
                        Success = true,
                        Message = $"Importación exitosa. Total: {total}, Insertados: {insertados}.",
                        Data = new { total, insertados, errores, detallesErrores }
                    };

                }else if (importacionParcial)
                {

                    return new ApiResponse
                    {
                        Success = false,
                        Message = $"Importación parcial. Total: {total}, Insertados: {insertados}, Errores: {errores}.",
                        Data = new { total, insertados, errores, detallesErrores }
                    };
                }
                else // importacionFallida
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = $"Importación fallida. No se insertaron libros. Total: {total}, Errores: {errores}.",
                        Data = new { total, insertados, errores, detallesErrores }
                    };

                }
                    
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ApiResponse
                {
                    Success = false,
                    Message = $"Error durante la importación: {ex.Message}. Se deshicieron todos los cambios",
                    Data = new
                    {
                        innerException = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace,
                        total,
                        insertados = 0,
                        errores,
                        detallesErrores
                    }
                };
            }
        }


        /*
         *  Este metodo genera una plantilla de Excel para la importacion de libros
         */

        public async Task<byte[]> GenerarPlantillaExcelAsync()
        {
            using var package = new ExcelPackage();

            var worksheet = package.Workbook.Worksheets.Add("Plantilla Libros");

            // Encabezados
            var encabezados = new[]
            {
        "Titulo", "Isbn", "Editorial", "Seccion", "Estado", "Descripcion", "PortadaUrl", "Autores", "Generos"
    };

            for (int i = 0; i < encabezados.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = encabezados[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Ejemplo de fila de muestra
            worksheet.Cells[2, 1].Value = "El Quijote";
            worksheet.Cells[2, 2].Value = "9781234567890";
            worksheet.Cells[2, 3].Value = "Editorial Ejemplo";
            worksheet.Cells[2, 4].Value = "Sección General";
            worksheet.Cells[2, 5].Value = "disponible";
            worksheet.Cells[2, 6].Value = "Ejemplo de descripción";
            worksheet.Cells[2, 7].Value = "https://tuservidor.com/portadas/quijote.jpg";
            worksheet.Cells[2, 8].Value = "Miguel de Cervantes";
            worksheet.Cells[2, 9].Value = "Novela, Clásico";

            worksheet.Cells.AutoFitColumns();

            // Devuelve el Excel como byte[]
            return await Task.FromResult(package.GetAsByteArray());
        }

    }

    public interface ILibroImportService
    {
        Task<ApiResponse> ImportarLibrosDesdeExcelAsync(IFormFile archivoExcel);
        Task<byte[]> GenerarPlantillaExcelAsync();

    }
}
