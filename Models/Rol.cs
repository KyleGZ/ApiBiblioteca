using System;
using System.Collections.Generic;

namespace ApiBiblioteca.Models;

public partial class Rol
{
    public int IdRol { get; set; }

    public string NombreRol { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public virtual ICollection<Usuario> IdUsuarios { get; set; } = new List<Usuario>();
}
