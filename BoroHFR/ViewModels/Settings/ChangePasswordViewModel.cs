using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Settings;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Kérlek add meg a jelenlegi jelszavad!")]
    [MinLength(8, ErrorMessage = "A jelszónak minimum 8 karaktert kell tartalmaznia.")]
    [DataType(DataType.Password)] public string OldPassword { get; set; }
    public bool BadPassword { get; set; }
    [Required(ErrorMessage = "Kérlek add meg az új jelszót!")]
    [MinLength(8, ErrorMessage = "A jelszónak minimum 8 karaktert kell tartalmaznia.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; }
    [DataType(DataType.Password)]

    [Compare(nameof(NewPassword), ErrorMessage = "A két jelszó nem egyezik.")]
    [Required(ErrorMessage = "Kérlek add meg az új jelszót még egyszer!")]
    public string NewPasswordAgain { get; set; }

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}