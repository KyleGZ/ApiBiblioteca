using System;
using System.Collections.Generic;

namespace ApiBiblioteca.Models;

public partial class Evento
{
    public int IdEvento { get; set; }

    public string Titulo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public int AforoMaximo { get; set; }

    public virtual ICollection<InscripcionEvento> InscripcionEventos { get; set; } = new List<InscripcionEvento>();
}
