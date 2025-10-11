using ApiBiblioteca.Models;
using ApiBiblioteca.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiBiblioteca.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RolController : Controller
    {
        private readonly DbContextBiblioteca _context;
        private readonly IAutorizacionService _autorizacionService;

        public RolController(DbContextBiblioteca context, IAutorizacionService autorizacionService)
        {
            _context = context;
            _autorizacionService = autorizacionService;
        }

        [HttpGet("ObtenerRoles")]
        public async Task<ActionResult<IEnumerable<object>>> GetRoles()
        {
            var roles = await _context.Rols
                .Where(r => r.Estado == "activo")
                .Select(r => new { id = r.IdRol, nombre = r.NombreRol })
                .ToListAsync();

            return Ok(roles);
        }

        [HttpPost("AsignarRolAUsuario")]
        public async Task<IActionResult> AsignarRolAUsuario(int idUsuario, int idRol)
        {
            var usuario = await _context.Usuarios.FindAsync(idUsuario);
            var rol = await _context.Rols.FindAsync(idRol);

            if (usuario == null || rol == null)
            {
                return NotFound(new { mensaje = "Usuario o rol no encontrado." });
            }

            // Verificar si ya existe la relación
            bool yaAsignado = usuario.IdRols.Any(r => r.IdRol == idRol);
            if (yaAsignado)
            {
                return BadRequest(new { mensaje = "El usuario ya tiene este rol asignado." });
            }

            // Asignar el rol al usuario
            usuario.IdRols.Add(rol);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = $"Rol '{rol.NombreRol}' asignado correctamente al usuario '{usuario.Nombre}'." });
        }

        [HttpPost("QuitarRolAUsuario")]
        public async Task<IActionResult> QuitarRolAUsuario(int idUsuario, int idRol)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.IdRols)
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            var rol = await _context.Rols.FindAsync(idRol);

            if (usuario == null || rol == null)
                return NotFound(new { mensaje = "Usuario o rol no encontrado." });

            if (!usuario.IdRols.Any(r => r.IdRol == idRol))
                return BadRequest(new { mensaje = "El usuario no tiene asignado ese rol." });

            usuario.IdRols.Remove(rol);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = $"Rol '{rol.NombreRol}' quitado del usuario '{usuario.Nombre}'." });
        }

    }
}
