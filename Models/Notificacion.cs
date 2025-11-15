using System;
using System.Collections.Generic;

namespace ApiBiblioteca.Models;

public partial class Notificacion
{
    public int IdNotificacion { get; set; }

    public int IdUsuario { get; set; }

    public string Asunto { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public DateTime FechaEnvio { get; set; }

    public string Estado { get; set; }
    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
