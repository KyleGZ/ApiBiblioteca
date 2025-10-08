using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiBiblioteca.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly DbContextBiblioteca _context;
        private readonly IAutorizacionService _autorizacionService;

        public UsuarioController(DbContextBiblioteca context, IAutorizacionService autorizacionService)
        {
            _context = context;
            _autorizacionService = autorizacionService;
        }

        [HttpGet("ListaUsuarios")]

        public async Task<ActionResult<IEnumerable<Usuario>>> ListaUsuarios() { 
        return await _context.Usuarios.ToListAsync();
        }


        /*
         * Metodo para iniciar sesion
         */
        [HttpPost("Login")]
        
        public async Task<IActionResult> Login([FromBody] Login login)
        {
            try
            {
                var resultado = await _autorizacionService.DevolverToken(login);

                if (resultado.Resultado)
                {
                    return Ok(resultado);
                }
                else
                {
                    return Unauthorized(resultado);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }


        [HttpPost("Registro")]

        public async Task<IActionResult> Registror([FromBody] UsuarioDto registro)
        {
            try
            {
                // Validar que el modelo sea válido
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { mensaje = "Datos de registro inválidos", errores = ModelState.Values.SelectMany(v => v.Errors) });
                }


                // Verificar si la cedula ya existe
                var cedulaExistente = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.cedula == registro.cedula);

                // Verificar si el email ya existe
                var usuarioExistente = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == registro.Email);

                if (usuarioExistente != null)
                {
                    return Conflict(new { mensaje = "El email ya está registrado" });
                }

                if (cedulaExistente != null)
                {
                    return Conflict(new { mensaje = "la cedula ya está registrado" });
                }

                //// Validar que se hayan proporcionado roles
                //if (registro.Roles == null || !registro.Roles.Any())
                //{
                //    return BadRequest(new { mensaje = "Debe asignar al menos un rol al usuario" });
                //}

                //// Verificar que los roles existan en la base de datos
                //var rolesExistentes = await _context.Rols
                //    .Where(r => registro.Roles.Contains(r.IdRol) && r.Estado == "activo")
                //    .ToListAsync();

                //if (rolesExistentes.Count != registro.Roles.Count)
                //{
                //    return BadRequest(new { mensaje = "Uno o más roles no existen o están inactivos" });
                //}

                // Hashear la contraseña
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(registro.Password);

                // Crear nuevo usuario
                var nuevoUsuario = new Usuario
                {
                    Nombre = registro.Nombre,
                    Email = registro.Email,
                    cedula = registro.cedula,
                    Password = passwordHash,
                    FechaRegistro = DateTime.Now.Date,
                    Estado = "Activo"
                };

                //// Asignar roles al usuario
                //foreach (var rol in rolesExistentes)
                //{
                //    nuevoUsuario.IdRols.Add(rol);
                //}

                // Guardar en la base de datos
                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Usuario registrado exitosamente", idUsuario = nuevoUsuario.IdUsuario });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

    }
}
