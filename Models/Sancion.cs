using System;
using System.Collections.Generic;

namespace ApiBiblioteca.Models;

public partial class Sancion
{
    public int IdSancion { get; set; }

    public int IdUsuario { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public string Motivo { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
