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
                    FechaEnvio = DateTime.Now,
                    Estado = "No leída"
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
                        FechaEnvio = DateTime.Now,
                        Estado = "No leída"
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

        public async Task<ApiResponse> ProcesarReservasVencidasAsync(
            int diasExpiracion = 1,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var fechaCorte = DateTime.Now.AddDays(-diasExpiracion);

                // Obtener reservas activas que ya vencieron
                var reservasVencidas = await _context.Reservas
                    .Include(r => r.IdLibroNavigation)
                    .Include(r => r.IdUsuarioNavigation)
                    .Where(r => r.Estado == "Activa" && r.FechaReserva <= fechaCorte)
                    .ToListAsync(cancellationToken);

                if (!reservasVencidas.Any())
                {
                    return new ApiResponse
                    {
                        Success = true,
                        Message = "No hay reservas vencidas para procesar."
                    };
                }

                int reservasProcesadas = 0;
                int librosLiberados = 0;
                int usuariosNotificados = 0;

                foreach (var reserva in reservasVencidas)
                {
                    var libro = reserva.IdLibroNavigation;
                    var usuario = reserva.IdUsuarioNavigation;

                    // Marcar como expirada
                    reserva.Estado = "Expirada";
                    reservasProcesadas++;

                    // Liberar el libro (solo si sigue estando reservado)
                    if (libro != null && libro.Estado == "Reservado")
                    {
                        libro.Estado = "Disponible";
                        librosLiberados++;
                    }

                    // Notificar al usuario
                    if (usuario != null && !string.IsNullOrEmpty(usuario.Email))
                    {
                        string asunto = "Tu reserva ha expirado";
                        string htmlBody = $@"
                <html>
                    <body style='font-family: Arial, sans-serif; color: #333;'>
                        <h2 style='color: #2C3E50;'>{asunto}</h2>
                        <p>Hola {usuario.Nombre},</p>
                        <p>Tu reserva del libro '<strong>{libro?.Titulo}</strong>' ha expirado porque no fue confirmada a tiempo.</p>
                        <p>Si aún deseas este libro, puedes realizar una nueva reserva.</p>
                        <hr style='border: 0; border-top: 1px solid #ccc;'/>
                        <p style='font-size: 12px; color: #888;'>
                            Biblioteca Esparza<br/>
                            Este es un mensaje automático, por favor no responder.
                        </p>
                    </body>
                </html>";

                        await _emailService.SendAsync(usuario.Email, asunto, htmlBody, cancellationToken);
                        usuariosNotificados++;
                    }

                    // Guardar notificación interna
                    _context.Notificacions.Add(new Notificacion
                    {
                        IdUsuario = reserva.IdUsuario,
                        Asunto = "Tu reserva ha expirado",
                        Mensaje = $"Tu reserva del libro '{libro?.Titulo}' ha expirado por falta de confirmación.",
                        FechaEnvio = DateTime.Now,
                        Estado = "No leída"
                    });
                }

                await _context.SaveChangesAsync(cancellationToken);

                return new ApiResponse
                {
                    Success = true,
                    Message = $"Reservas vencidas procesadas: {reservasProcesadas}. Libros liberados: {librosLiberados}. Usuarios notificados: {usuariosNotificados}.",
                    Data = new
                    {
                        ReservasProcesadas = reservasProcesadas,
                        LibrosLiberados = librosLiberados,
                        UsuariosNotificados = usuariosNotificados
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = $"Error al procesar reservas vencidas: {ex.Message}",
                    Data = ex.StackTrace
                };
            }
        }





    }

    public interface INotificacionesServices
    {
        Task<ApiResponse> CrearNotificacionAsync(NotificacionDto model);
        Task<ApiResponse> RecordarPrestamosPorVencerAsync(int diasAnticipacion = 1, CancellationToken cancellationToken = default);
        Task<ApiResponse> ProcesarReservasVencidasAsync(int diasExpiracion = 1, CancellationToken cancellationToken = default);

    }
}
