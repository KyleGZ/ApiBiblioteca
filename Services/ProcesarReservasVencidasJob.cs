using Quartz;

namespace ApiBiblioteca.Services
{
    public class ProcesarReservasVencidasJob : IJob
    {
        private readonly INotificacionesServices _notificacionesService;
        private readonly ILogger<ProcesarReservasVencidasJob> _logger;

        public ProcesarReservasVencidasJob(INotificacionesServices notificaciones, ILogger<ProcesarReservasVencidasJob> logger)
        {
         _notificacionesService = notificaciones;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Iniciando tarea de procesar reservas vencidas ({time})...", DateTime.Now);
                var resultado = await _notificacionesService.ProcesarReservasVencidasAsync();
                if (resultado.Success)
                    _logger.LogInformation("Tarea completada exitosamente: {Message}", resultado.Message);
                else
                    _logger.LogWarning("Tarea completada con errores: {Message}", resultado.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico en la tarea de procesamiento de reservas vencidas.");
            }
        }
    }
}
