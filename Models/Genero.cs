using System;
using System.Collections.Generic;

namespace ApiBiblioteca.Models;

public partial class Genero
{
    public int IdGenero { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Libro> IdLibros { get; set; } = new List<Libro>();
}
