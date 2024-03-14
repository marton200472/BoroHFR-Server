using BoroHFR.Models;

namespace BoroHFR.ViewModels.SysAdmin
{
    public class ResetPasswordViewModel
    {
        public bool Confirmed { get; set; }
        public string Password { get; set; }
        public User User { get; set; }
    }
}
