using System;
using System.Collections.Generic;

namespace ApiBiblioteca.Models;

public partial class Reserva
{
    public int IdReserva { get; set; }

    public int IdUsuario { get; set; }

    public int IdLibro { get; set; }

    public DateTime FechaReserva { get; set; }

    public int Prioridad { get; set; }

    public string Estado { get; set; } = null!;

    public virtual Libro IdLibroNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
