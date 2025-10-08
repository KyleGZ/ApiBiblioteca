using System;
using System.Collections.Generic;

namespace ApiBiblioteca.Models;

public partial class Configuracion
{
    public int IdConfiguracion { get; set; }

    public string Clave { get; set; } = null!;

    public string Valor { get; set; } = null!;

    public string Descripcion { get; set; } = null!;
}
