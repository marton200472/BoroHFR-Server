using BoroHFR.Models;
using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Admin
{
    public class EditSubjectViewModel
    {
        [Required(ErrorMessage = "A mező nem lehet üres!")]
        public string Name { get; set; }
        public SubjectId SubjId { get; set; }
    }
}
