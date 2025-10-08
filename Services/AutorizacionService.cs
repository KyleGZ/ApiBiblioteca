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

        public AutorizacionService(IConfiguration configuration, DbContextBiblioteca context)
        {
            _configuration = configuration;
            _context = context;
        }

        private string GenerarToken(string email, List<string> roles)
        {
            var claims = new List<Claim>
            {
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

        //public async Task<AutorizacionResponse> DevolverToken(Login usuario)
        //{
        //    // Buscar el usuario incluyendo la relación con roles
        //    var temp = await _context.Usuarios
        //        .Include(u => u.IdRols)  // Incluir los roles directamente
        //        .FirstOrDefaultAsync(u => u.Email.Equals(usuario.Email) && u.Password.Equals(usuario.Password));

        //    if (temp == null)
        //    {
        //        return new AutorizacionResponse()
        //        {
        //            Token = null,
        //            Resultado = false,
        //            Msj = "Credenciales inválidas"
        //        };
        //    }

        //    // Verificar la contraseña usando BCrypt
        //    if (!BCrypt.Net.BCrypt.Verify(usuario.Password, temp.Password))
        //    {
        //        return new AutorizacionResponse()
        //        {
        //            Token = null,
        //            Resultado = false,
        //            Msj = "Credenciales inválidas"
        //        };
        //    }

        //    // Obtener todos los roles del usuario
        //    var roles = temp.IdRols?
        //        .Select(r => r.NombreRol)  // Usar NombreRol según tu modelo
        //        .Where(rol => !string.IsNullOrEmpty(rol))
        //        .ToList() ?? new List<string>();

        //    // Si no tiene roles, puedes manejarlo como quieras
        //    if (!roles.Any())
        //    {
        //        return new AutorizacionResponse()
        //        {
        //            Token = null,
        //            Resultado = false,
        //            Msj = "El usuario no tiene roles asignados"
        //        };
        //    }

        //    // Generar token con los roles
        //    string tokenCreado = GenerarToken(temp.Email, roles);

        //    return new AutorizacionResponse()
        //    {
        //        Token = tokenCreado,
        //        Resultado = true,
        //        Msj = "OK",  // Corregido: era "Msg" en lugar de "Msj"
        //        Email = temp.Email,
        //        Nombre = temp.Nombre,
        //        Roles = roles
        //    };
        //}
        public async Task<AutorizacionResponse> DevolverToken(Login usuario)
        {
            // Buscar el usuario solo por email (sin verificar contraseña en la consulta)
            var temp = await _context.Usuarios
                .Include(u => u.IdRols)  // Incluir los roles directamente
                .FirstOrDefaultAsync(u => u.Email.Equals(usuario.Email));

            // Si no se encuentra el usuario, retornar error
            if (temp == null)
            {
                return new AutorizacionResponse()
                {
                    Token = null,
                    Resultado = false,
                    Msj = "Credenciales inválidas"
                };
            }

            // Verificar la contraseña usando BCrypt
            if (!BCrypt.Net.BCrypt.Verify(usuario.Password, temp.Password))
            {
                return new AutorizacionResponse()
                {
                    Token = null,
                    Resultado = false,
                    Msj = "Credenciales inválidas"
                };
            }

            // Verificar que el usuario esté activo
            //if (temp.Estado != "activo")
            //{
            //    return new AutorizacionResponse()
            //    {
            //        Token = null,
            //        Resultado = false,
            //        Msj = "El usuario no está activo"
            //    };
            //}

            // Obtener todos los roles del usuario
            var roles = temp.IdRols?
                .Select(r => r.NombreRol)  // Usar NombreRol según tu modelo
                .Where(rol => !string.IsNullOrEmpty(rol))
                .ToList() ?? new List<string>();

            // Si no tiene roles, puedes manejarlo como quieras
            if (!roles.Any())
            {
                return new AutorizacionResponse()
                {
                    Token = null,
                    Resultado = false,
                    Msj = "El usuario no tiene roles asignados"
                };
            }

            // Generar token con los roles
            string tokenCreado = GenerarToken(temp.Email, roles);

            return new AutorizacionResponse()
            {
                Token = tokenCreado,
                Resultado = true,
                Msj = "OK",
                Email = temp.Email,
                Nombre = temp.Nombre,
                Roles = roles
            };
        }

    }
}