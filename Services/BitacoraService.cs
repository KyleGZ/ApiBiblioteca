using ApiBiblioteca.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiBiblioteca.Services
{
    public class BitacoraService : IBitacoraService
    {

       
        private IServiceScopeFactory _serviceScopeFactory;
        public BitacoraService(IServiceScopeFactory serviceScopeFactory)
        {
            
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task RegistrarAccionAsync(int idUsuario, string accion, string tablaAfectada, int idRegistro)
        {
            // Ejecutar en segundo plano sin esperar
            _ = Task.Run(async () =>
            {
                // Crear un scope INDEPENDIENTE para el contexto
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    try
                    {
                        // Obtener un NUEVO contexto desde el scope
                        var context = scope.ServiceProvider.GetRequiredService<DbContextBiblioteca>();

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
                        context.Bitacoras.Add(bitacora);
                        await context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {

                    }
                }
            });
        }
    }

    public interface IBitacoraService
    {
        Task RegistrarAccionAsync(int idUsuario, string accion, string tablaAfectada, int idRegistro);

    }
}
