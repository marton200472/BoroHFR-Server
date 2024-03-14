using BoroHFR.Models;

namespace BoroHFR.ViewModels.Home
{
    public class CreateEventViewModel
    {
        public Event Event { get; set; }
        public Group[]? Groups { get; set; }
        public Guid SelectedGroupGuid { get; set; }
        public EventType EvType { get; set; }
        public FileId[] Attachments { get; set; } = new FileId[0];
    }
}
