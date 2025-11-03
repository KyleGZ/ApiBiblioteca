using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiBiblioteca.Models
{
    public partial class PasswordResetToken
    {

        public int IdForgotPassword { get; set; }


        public int IdUsuario { get; set; }

     
        public string Token { get; set; }


        public DateTime Expires { get; set; }


        public bool Used { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        public virtual Usuario Usuario { get; set; }
    }
}
