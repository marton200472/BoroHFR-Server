using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.SysAdmin
{
    public class AuthViewModel
    {
        [Required]
        public string Email { get; set; }
        [DataType(DataType.Password)]
        [Required]
        public string Password { get; set; }

        public bool WrongAuthData { get; set; }
    }
}
