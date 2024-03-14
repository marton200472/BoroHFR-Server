using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Admin
{
    public class InviteUsersViewModel
    {
        [Required]
        public bool MultiUse { get; set; }
        [Range(1,100)]
        public int Quantity { get; set; }
        [Required]
        public DateTimeOffset Validity { get; set; }
        public string[]? Tokens { get; set; }
    }
}
