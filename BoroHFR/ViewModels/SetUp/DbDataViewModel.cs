using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Setup
{
    public class DbDataViewModel
    {
        [Required]
        public string Server { get; set; } = "localhost";
        [Required]
        [Range(1, 65535)]
        public uint Port { get; set; } = 3306;
        [Required]
        public string Database { get; set; } = "borohfr";
        [Required]
        public string User { get; set; }
        public string? Password { get; set; }

        public bool FailedToConnect { get; set; }
    }
}
