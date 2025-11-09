using System;
using System.Collections.Generic;

namespace ApiBiblioteca.Models;

public partial class Prestamo
{
    public int IdPrestamo { get; set; }

    public int IdLibro { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaPrestamo { get; set; }

    public DateTime FechaDevolucionPrevista { get; set; }

    public DateTime? FechaDevolucionReal { get; set; }

    public int Renovaciones { get; set; }

    public string Estado { get; set; } = null!;

    public virtual Libro IdLibroNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
