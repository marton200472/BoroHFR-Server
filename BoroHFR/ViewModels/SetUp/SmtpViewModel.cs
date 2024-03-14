using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Setup
{
    public class SmtpViewModel
    {
        [Required(ErrorMessage = "Kötelező mező!")]
        public string Server { get; set; }
        [Required(ErrorMessage = "Kötelező mező!")]
        [Range(1, 65535, ErrorMessage = "Adj meg egy érvényes portot.")]
        public uint Port { get; set; }
        [Required(ErrorMessage = "Kötelező mező!")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Kötelező mező!")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required(ErrorMessage = "Kötelező mező!")]
        [EmailAddress(ErrorMessage = "Kérlek adj meg egy érvényes e-mail címet.")]
        public string SenderAddress { get; set; }
        [Required(ErrorMessage = "Kötelező mező!")]
        public string SenderName { get; set; }

        public bool ConnectionFailed { get; set; }
    }
}
