using System;
using System.Collections.Generic;

namespace ApiBiblioteca.Models;

public partial class InscripcionEvento
{
    public int IdInscripcionEvento { get; set; }

    public int IdEvento { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaInscripcion { get; set; }

    public string Estado { get; set; } = null!;

    public virtual Evento IdEventoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
