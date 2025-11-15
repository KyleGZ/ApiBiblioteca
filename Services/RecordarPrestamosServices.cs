using Quartz;

namespace ApiBiblioteca.Services
{
    public class RecordarPrestamosServices : IJob
    {
        private readonly INotificacionesServices _notificaionesService;
        private readonly ILogger<RecordarPrestamosServices> _logger;

        public RecordarPrestamosServices(INotificacionesServices notificaciones, ILogger<RecordarPrestamosServices> logger)
        {
            _notificaionesService = notificaciones;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Iniciando tarea de recordar préstamos ({time})...", DateTime.Now);
                var resultado = await _notificaionesService.RecordarPrestamosPorVencerAsync();

                if (resultado.Success)
                    _logger.LogInformation("Tarea completada exitosamente: {Message}", resultado.Message);
                else
                    _logger.LogWarning("Tarea completada con errores: {Message}", resultado.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico en la tarea de recordatorio de préstamos.");
            }
        }



    }
}
