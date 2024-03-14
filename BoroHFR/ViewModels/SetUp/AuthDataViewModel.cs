using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Setup
{
    public class AuthDataViewModel
    {
        [DataType(DataType.Password)]
        [MinLength(12, ErrorMessage = "A jelszónak legalább 12 karakter hosszúnak kell lennie.")]
        [Required(ErrorMessage = "Jelszó megadása kötelező!")]
        public string Password { get; set; }
        [Compare(nameof(Password), ErrorMessage = "A két jelszónak egyeznie kell.")]
        [Required(ErrorMessage = "Jelszó megadása kötelező!")]
        [DataType(DataType.Password)]
        public string PasswordAgain { get; set; }
        [Required(ErrorMessage = "E-mail cím megadása kötelező!")]
        [EmailAddress(ErrorMessage = "Kérlek adj meg egy érvényes e-mail címet.")]
        public string Email { get; set; }
    }
}
