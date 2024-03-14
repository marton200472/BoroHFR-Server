using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.SysAdmin
{
    public class CreateClassViewModel
    {
        [Required(ErrorMessage = "Név megadása kötelező!")]
        public string Name { get; set; }

        public bool NameUsed { get; set; }
    }
}
