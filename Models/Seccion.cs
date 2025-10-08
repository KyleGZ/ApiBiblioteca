using System;
using System.Collections.Generic;

namespace ApiBiblioteca.Models;

public partial class Seccion
{
    public int IdSeccion { get; set; }

    public string Nombre { get; set; } = null!;

    public string Ubicacion { get; set; } = null!;

    public virtual ICollection<Libro> Libros { get; set; } = new List<Libro>();
}
