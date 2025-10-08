using ApiBiblioteca.Models.Dtos;

namespace ApiBiblioteca.Services
{
    public interface IAutorizacionService
    {
        Task<AutorizacionResponse> DevolverToken(Login usuario);
    }
}
