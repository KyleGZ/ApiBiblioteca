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


        //public async Task RegistrarAccionAsync(int idUsuario, string accion, string tablaAfectada, int idRegistro)
        //{
        //    // Ejecutar en segundo plano sin esperar
        //    _ = Task.Run(async () =>
        //    {
        //        try
        //        {
        //            // Pequeña pausa para no interferir con la operación principal
        //            await Task.Delay(100);

        //            var bitacora = new Bitacora
        //            {
        //                IdUsuario = idUsuario,
        //                Accion = accion?.Trim() ?? "DESCONOCIDO",
        //                TablaAfectada = tablaAfectada?.Trim() ?? "DESCONOCIDO",
        //                IdRegistro = idRegistro,
        //                FechaHora = DateTime.Now
        //            };

        //            _context.Bitacoras.Add(bitacora);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (Exception ex)
        //        {
        //            // Log silencioso - no rompe el flujo
        //            Console.WriteLine($"Error no crítico en bitácora: {ex.Message}");
        //        }
        //    });
        //}
        public async Task RegistrarAccionAsync(int idUsuario, string accion, string tablaAfectada, int idRegistro)
        {
            Console.WriteLine($"🚀 INICIANDO Bitácora - Usuario: {idUsuario}, Acción: {accion}");

            // Ejecutar en segundo plano sin esperar
            _ = Task.Run(async () =>
            {
                // Crear un scope INDEPENDIENTE para el contexto
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    try
                    {
                        Console.WriteLine($"📝 EJECUTANDO Bitácora en segundo plano...");

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

                        Console.WriteLine($"💾 GUARDANDO Bitácora en BD...");
                        context.Bitacoras.Add(bitacora);
                        await context.SaveChangesAsync();

                        Console.WriteLine($"✅ BITÁCORA GUARDADA - ID: {bitacora.IdBitacora}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ ERROR en bitácora: {ex.Message}");
                        Console.WriteLine($"🔍 StackTrace: {ex.StackTrace}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"🔍 InnerException: {ex.InnerException.Message}");
                        }
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
