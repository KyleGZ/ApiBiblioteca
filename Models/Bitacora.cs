using System;
using System.Collections.Generic;

namespace ApiBiblioteca.Models;

public partial class Bitacora
{
    public int IdBitacora { get; set; }

    public int IdUsuario { get; set; }

    public string Accion { get; set; } = null!;

    public string TablaAfectada { get; set; } = null!;

    public int? IdRegistro { get; set; }

    public DateTime FechaHora { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
