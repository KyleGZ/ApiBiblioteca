using System;
using System.Collections.Generic;

namespace ApiBiblioteca.Models;

public partial class Libro
{
    public int IdLibro { get; set; }

    public string Titulo { get; set; } = null!;

    public string Isbn { get; set; } = null!;

    public int IdEditorial { get; set; }

    public int IdSeccion { get; set; }

    public string Estado { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string PortadaUrl { get; set; } = null!;

    public virtual Editorial IdEditorialNavigation { get; set; } = null!;

    public virtual Seccion IdSeccionNavigation { get; set; } = null!;

    public virtual ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

    public virtual ICollection<Autor> IdAutors { get; set; } = new List<Autor>();

    public virtual ICollection<Genero> IdGeneros { get; set; } = new List<Genero>();
}
