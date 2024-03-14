using BoroHFR.Models;
using System.Globalization;

namespace BoroHFR.ViewModels.Home
{
    public class EditEventViewModel
    {
        public Event Event { get; set; }
        public FileId[] Attachments { get; set; } = new FileId[0];
    }
}
