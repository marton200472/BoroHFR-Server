using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Admin
{
    public class CreateSubjectViewModel
    {
        [Required(ErrorMessage = "A mező nem lehet üres!")]
        public string Name { get; set; }
    }
}
