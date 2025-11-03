using System.ComponentModel.DataAnnotations;

namespace ApiBiblioteca.Models.Dtos
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }
    }
}
