using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Authentication;

public class LoginViewModel
{
    [Required(ErrorMessage = "Add meg a felhasználóneved!")]
    [MinLength(3, ErrorMessage = "A felhasználónevek minimum 3 karakter hosszúak.")]
    [MaxLength(20, ErrorMessage = "A felhasználónevek maximum 20 karakter hosszúak.")]
    [DataType(DataType.Text)]
    public string Username { get; set; }
    [Required(ErrorMessage = "Add meg a jelszavad!")]
    [MinLength(8, ErrorMessage = "A jelszavak minimum 8 karakterből állnak.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
    public bool RemainLoggedIn { get; set; }
    public bool BadUsername { get; set; }
    public bool BadPassword { get; set; }
}