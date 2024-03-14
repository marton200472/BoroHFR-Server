using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Authentication;

public class NewPasswordViewModel
{
    [Required(ErrorMessage = "Kérlek add meg az új jelszavad!")]
    [MinLength(8, ErrorMessage = "A jelszónak minimum 8 karakter hosszúnak kell lennie.")]
    public string NewPassword { get; set; }
    [Required(ErrorMessage = "Kérlek add meg még egyszer a jelszót.")]
    [Compare(nameof(NewPassword), ErrorMessage = "A jelszavak nem egyeznek.")]
    public string NewPasswordAgain { get; set; }
}