using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiBiblioteca.Services
{
    public class AutorizacionService : IAutorizacionService
    {
        private readonly IConfiguration _configuration;
        private readonly DbContextBiblioteca _context;
        public static int UsuarioAutenticadoId { get; private set; } = 0;

        public AutorizacionService(IConfiguration configuration, DbContextBiblioteca context)
        {
            _configuration = configuration;
            _context = context;
        }

        private string GenerarToken(int userId, string email, List<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Agregar cada rol como un claim
            foreach (var rol in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, rol));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<AutorizacionResponse> DevolverToken(Login usuario)
        {
            // Buscar el usuario por email (incluyendo roles)
            var temp = await _context.Usuarios
                .Include(u => u.IdRols)
                .FirstOrDefaultAsync(u => u.Email.Equals(usuario.Email));

            // Usuario no existe
            if (temp == null)
            {
                return new AutorizacionResponse
                {
                    Token = null,
                    Resultado = false,
                    Msj = "Credenciales inválidas"
                };
            }

            // Verificar contraseña
            if (!BCrypt.Net.BCrypt.Verify(usuario.Password, temp.Password))
            {
                return new AutorizacionResponse
                {
                    Token = null,
                    Resultado = false,
                    Msj = "Credenciales inválidas"
                };
            }

            // Bloquear si el usuario está Inactivo
            var estado = (temp.Estado ?? string.Empty).Trim();
            if (estado.Equals("Inactivo", StringComparison.OrdinalIgnoreCase))
            {
                return new AutorizacionResponse
                {
                    Token = null,
                    Resultado = false,
                    Msj = "El usuario está inactivo. Contacte al administrador."
                };
            }

            // Obtener roles
            var roles = temp.IdRols?
                .Select(r => r.NombreRol)
                .Where(rol => !string.IsNullOrWhiteSpace(rol))
                .ToList() ?? new List<string>();

            if (!roles.Any())
            {
                return new AutorizacionResponse
                {
                    Token = null,
                    Resultado = false,
                    Msj = "El usuario no tiene roles asignados"
                };
            }
            UsuarioAutenticadoId = temp.IdUsuario;

            // Generar token con Id de usuario
            string tokenCreado = GenerarToken(temp.IdUsuario, temp.Email, roles);

            return new AutorizacionResponse
            {
                Token = tokenCreado,
                Resultado = true,
                Msj = "OK",
                idUsuario = temp.IdUsuario,
                Email = temp.Email,
                Nombre = temp.Nombre,
                Roles = roles
            };
            
        }
        // Método para obtener el ID del usuario autenticado
        public static int ObtenerUsuarioAutenticadoId()
        {
            return UsuarioAutenticadoId;
        }

        // Método para limpiar el usuario autenticado (logout)
        public static void LimpiarUsuarioAutenticado()
        {
            UsuarioAutenticadoId = 0;
        }
    }
}