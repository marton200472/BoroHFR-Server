using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Authentication;

public class PasswordResetRequestViewModel
{
    [Required(ErrorMessage = "A mező kitöltése kötelező.")]
    [EmailAddress(ErrorMessage = "Kérlek adj meg egy érvényes e-mail címet.")]
    public string EMail { get; set; }
}