using System;
using System.Collections.Generic;

namespace ApiBiblioteca.Models;

public partial class Autor
{
    public int IdAutor { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Libro> IdLibros { get; set; } = new List<Libro>();
}
