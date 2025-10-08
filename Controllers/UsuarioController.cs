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

        [HttpGet("Listar")]
        public async Task<IActionResult> ListarUsuarios()
        {
            try
            {
                var usuarios = await _context.Usuarios
                    .Select(u => new UsuarioListaDto
                    {
                        IdUsuario = u.IdUsuario,
                        Cedula = u.cedula,
                        Nombre = u.Nombre,
                        Email = u.Email,
                        Estado = u.Estado,
                        
                    })
                    .OrderBy(u => u.Nombre) // Ordenar por nombre
                    .ToListAsync();

                return Ok(new
                {
                    mensaje = "Lista de usuarios obtenida exitosamente",
                    totalUsuarios = usuarios.Count,
                    usuarios = usuarios
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
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

        [HttpPut("Editar")]
        public async Task<IActionResult> EditarUsuario([FromBody] EditarUsuarioDto editarDto)
        {
            try
            {
                // Validar que el modelo sea válido
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { mensaje = "Datos de edición inválidos", errores = ModelState.Values.SelectMany(v => v.Errors) });
                }

                // Validar que el IdUsuario venga en el DTO
                if (editarDto.IdUsuario <= 0)
                {
                    return BadRequest(new { mensaje = "El ID de usuario es requerido" });
                }

                // Buscar el usuario existente
                var usuarioExistente = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.IdUsuario == editarDto.IdUsuario);

                if (usuarioExistente == null)
                {
                    return NotFound(new { mensaje = "Usuario no encontrado" });
                }

                // Verificar si el nuevo email ya existe en otro usuario
                if (!string.IsNullOrEmpty(editarDto.Email) && editarDto.Email != usuarioExistente.Email)
                {
                    var emailExistente = await _context.Usuarios
                        .FirstOrDefaultAsync(u => u.Email == editarDto.Email && u.IdUsuario != editarDto.IdUsuario);

                    if (emailExistente != null)
                    {
                        return Conflict(new { mensaje = "El email ya está registrado en otro usuario" });
                    }
                }

                // Verificar si la nueva cédula ya existe en otro usuario
                if (!string.IsNullOrEmpty(editarDto.cedula) && editarDto.cedula != usuarioExistente.cedula)
                {
                    var cedulaExistente = await _context.Usuarios
                        .FirstOrDefaultAsync(u => u.cedula == editarDto.cedula && u.IdUsuario != editarDto.IdUsuario);

                    if (cedulaExistente != null)
                    {
                        return Conflict(new { mensaje = "La cédula ya está registrada en otro usuario" });
                    }
                }

                // Actualizar solo los campos que se enviaron
                if (!string.IsNullOrEmpty(editarDto.Nombre))
                    usuarioExistente.Nombre = editarDto.Nombre;

                if (!string.IsNullOrEmpty(editarDto.Email))
                    usuarioExistente.Email = editarDto.Email;

                if (!string.IsNullOrEmpty(editarDto.cedula))
                    usuarioExistente.cedula = editarDto.cedula;

                if (!string.IsNullOrEmpty(editarDto.Password))
                    usuarioExistente.Password = BCrypt.Net.BCrypt.HashPassword(editarDto.Password);

                if (!string.IsNullOrEmpty(editarDto.Estado))
                    usuarioExistente.Estado = editarDto.Estado;

                // Guardar cambios
                _context.Usuarios.Update(usuarioExistente);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Usuario actualizado exitosamente",
                    idUsuario = usuarioExistente.IdUsuario
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        [HttpDelete("Desactivar")]
        public async Task<IActionResult> DesactivarUsuario(int id)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.IdUsuario == id);

                if (usuario == null)
                {
                    return NotFound(new { mensaje = "Usuario no encontrado" });
                }

                // Cambiar estado a inactivo
                usuario.Estado = "Inactivo";

                _context.Usuarios.Update(usuario);
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Usuario desactivado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        [HttpPatch("Activar")]
        public async Task<IActionResult> ActivarUsuario(int id)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.IdUsuario == id);

                if (usuario == null)
                {
                    return NotFound(new { mensaje = "Usuario no encontrado" });
                }

                usuario.Estado = "Activo";

                _context.Usuarios.Update(usuario);
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Usuario activado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }


    }
}
