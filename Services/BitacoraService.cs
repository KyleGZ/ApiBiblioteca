using ApiBiblioteca.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiBiblioteca.Services
{
    public class BitacoraService : IBitacoraService
    {

        private readonly DbContextBiblioteca _context;

        public BitacoraService(DbContextBiblioteca dbContext)
        {
            _context = dbContext;
        }


        public async Task RegistrarAccionAsync(int idUsuario, string accion, string tablaAfectada, int idRegistro)
        {
            // Ejecutar en segundo plano sin esperar
            _ = Task.Run(async () =>
            {
                try
                {
                    // Pequeña pausa para no interferir con la operación principal
                    await Task.Delay(100);

                    var bitacora = new Bitacora
                    {
                        IdUsuario = idUsuario,
                        Accion = accion?.Trim() ?? "DESCONOCIDO",
                        TablaAfectada = tablaAfectada?.Trim() ?? "DESCONOCIDO",
                        IdRegistro = idRegistro,
                        FechaHora = DateTime.Now
                    };

                    _context.Bitacoras.Add(bitacora);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // Log silencioso - no rompe el flujo
                    Console.WriteLine($"Error no crítico en bitácora: {ex.Message}");
                }
            });
        }

    }

    public interface IBitacoraService
    {
        Task RegistrarAccionAsync(int idUsuario, string accion, string tablaAfectada, int idRegistro);

    }
}
