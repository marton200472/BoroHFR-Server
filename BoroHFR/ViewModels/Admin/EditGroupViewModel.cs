using BoroHFR.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Admin
{
    public class EditGroupViewModel
    {
        [Required(ErrorMessage = "A mező nem lehet üres!")]
        public string GroupName { get; set; }
        [Required(ErrorMessage = "A mező nem lehet üres!")]
        public string Teacher { get; set; }
        public GroupId GrId { get; set; }
    }
}
