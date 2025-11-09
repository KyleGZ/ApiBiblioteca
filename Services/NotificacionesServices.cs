using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ApiBiblioteca.Services
{
    public class NotificacionesServices : INotificacionesServices
    {
        private readonly IEmailService _emailService;
        private readonly DbContextBiblioteca _context;

        public NotificacionesServices(DbContextBiblioteca dbContext, IEmailService emailService)
        {
            _context = dbContext;
            _emailService = emailService;
        }

        public async Task<ApiResponse> CrearNotificacionAsync(NotificacionDto model)
        {
            try
            {
                // Crear notificación y guardar en BD
                var notificacion = new Notificacion
                {
                    IdUsuario = model.IdUsuario,
                    Asunto = model.Asunto,
                    Mensaje = model.Mensaje,
                    FechaEnvio = DateTime.Now
                };

                _context.Notificacions.Add(notificacion);
                await _context.SaveChangesAsync();

                // Si se requiere enviar correo
                if (model.enviarCorreo)
                {
                    var usuario = await _context.Usuarios
                        .FirstOrDefaultAsync(u => u.IdUsuario == model.IdUsuario);

                    if (usuario != null && !string.IsNullOrEmpty(usuario.Email))
                    {
                        
                        string htmlBody = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif; color: #333;'>
                            <h2 style='color: #2C3E50;'>{model.Asunto}</h2>
                            <p>{model.Mensaje}</p>
                            <hr style='border: 0; border-top: 1px solid #ccc;'/>
                            <p style='font-size: 12px; color: #888;'>
                                Biblioteca Esparza<br/>
                                Este es un mensaje automático, por favor no responder.
                            </p>
                        </body>
                    </html>";

                        await _emailService.SendAsync(usuario.Email, model.Asunto, htmlBody);
                    }
                }

                // Respuesta uniforme de éxito
                return new ApiResponse
                {
                    Success = true,
                    Message = "Notificación creada correctamente."
                };
            }
            catch (Exception ex)
            {
                // Respuesta uniforme de error
                return new ApiResponse
                {
                    Success = false,
                    Message = $"Error al crear la notificación: {ex.Message}"
                };
            }
        }

       
        public async Task<ApiResponse> RecordarPrestamosPorVencerAsync(int diasAnticipacion = 1, CancellationToken cancellationToken = default)
        {
            try
            {
                var fechaObjetivo = DateTime.Now.Date.AddDays(diasAnticipacion);

                var prestamos = await _context.Prestamos
                    .Include(p => p.IdUsuarioNavigation)
                    .Include(p => p.IdLibroNavigation)
                    .Where(p => p.Estado == "Activo" && p.FechaDevolucionPrevista == fechaObjetivo)
                    .ToListAsync(cancellationToken);

                if (!prestamos.Any())
                    return new ApiResponse
                    {
                     Success = true, 
                     Message = "No hay préstamos por vencer en la fecha objetivo." 
                    };

                foreach (var prestamo in prestamos)
                {
                    var asunto = "Recordatorio de devolución";
                    var mensaje = $"El libro '{prestamo.IdLibroNavigation.Titulo}' debe devolverse el {prestamo.FechaDevolucionPrevista:dd/MM/yyyy}.";

                    // Guardar notificación
                    var noti = new Notificacion
                    {
                        IdUsuario = prestamo.IdUsuario,
                        Asunto = asunto,
                        Mensaje = mensaje,
                        FechaEnvio = DateTime.Now
                    };
                    _context.Notificacions.Add(noti);

                    // Enviar correo si usuario tiene email
                    var usuario = prestamo.IdUsuarioNavigation;
                    if (usuario != null && !string.IsNullOrEmpty(usuario.Email))
                    {
                        string htmlBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; color: #333;'>
                            <h2 style='color: #2C3E50;'>{asunto}</h2>
                            <p>Hola {usuario.Nombre},</p>
                            <p>{mensaje}</p>
                            <hr style='border: 0; border-top: 1px solid #ccc;'/>
                            <p style='font-size: 12px; color: #888;'>
                                Biblioteca Esparza<br/>
                                Este es un mensaje automático, por favor no responder.
                            </p>
                        </body>
                        </html>";


                        await _emailService.SendAsync(usuario.Email, asunto, htmlBody, cancellationToken);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                return new ApiResponse { Success = true, Message = $"Se procesaron {prestamos.Count} recordatorios." };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = $"Error al procesar recordatorios: {ex.Message}" };
            }
        }


}

    public interface INotificacionesServices
    {
        Task<ApiResponse> CrearNotificacionAsync(NotificacionDto model);
        Task<ApiResponse> RecordarPrestamosPorVencerAsync(int diasAnticipacion = 1, CancellationToken cancellationToken = default);
        
    }
}
