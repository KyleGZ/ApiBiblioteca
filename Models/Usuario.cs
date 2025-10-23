using System;
using System.Collections.Generic;

namespace ApiBiblioteca.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string Cedula { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public string Estado { get; set; } = null!;

    public virtual ICollection<Bitacora> Bitacoras { get; set; } = new List<Bitacora>();

    public virtual ICollection<InscripcionEvento> InscripcionEventos { get; set; } = new List<InscripcionEvento>();

    public virtual ICollection<Notificacion> Notificacions { get; set; } = new List<Notificacion>();

    public virtual ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

    public virtual ICollection<Sancion> Sancions { get; set; } = new List<Sancion>();

    public virtual ICollection<Rol> IdRols { get; set; } = new List<Rol>();
}
