using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Authentication;

public class RegisterViewModel
{
    [DataType(DataType.Text)]
    [MinLength(3, ErrorMessage = "A minimális hossz 3.")]
    [MaxLength(20, ErrorMessage = "A maximális hossz 20.")]
    [Required(ErrorMessage = "Kérlek adj meg egy felhasználónevet!")]
    public string Username { get; set; }
    [Required(ErrorMessage = "Kérlek adj meg egy jelszót!")]
    [MinLength(8, ErrorMessage = "A jelszónak minimum 8 karaktert kell tartalmaznia.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required(ErrorMessage = "Kérlek add meg még egyszer a jelszót!")]
    [Compare(nameof(Password), ErrorMessage = "A megadott jelszavak nem egyeznek.")]
    public string PasswordAgain { get; set; }

    [Required(ErrorMessage = "Kérlek adj meg egy e-mail címet, melyen elérhető vagy! (Nem küldünk felesleges e-maileket.)")]
    [EmailAddress(ErrorMessage = "Kérlek adj meg egy érvényes e-mail címet.")]
    public string EMail { get; set; }
    [DataType(DataType.Text)]
    [Required(ErrorMessage = "A regisztrációhoz add meg a meghívó kódod!")]
    public string Token { get; set; }
    public bool AcceptPrivacyPolicy { get; set; }

    public bool UsernameTaken { get; set; }
    public bool EMailTaken { get; set; }
    public bool BadToken { get; set; }
    public bool DidNotAcceptPrivacyPolicy { get; set; }
}