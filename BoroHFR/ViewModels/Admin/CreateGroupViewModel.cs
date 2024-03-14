using BoroHFR.Models;
using Org.BouncyCastle.Crypto.Engines;
using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Admin
{
    public class CreateGroupViewModel
    {
        public string Name { get; set; }
        public Subject[]? Subjects { get; set; }
        public Guid? SelectedSubjectId { get; set; }
        public string Teacher { get; set; }
    }
}
