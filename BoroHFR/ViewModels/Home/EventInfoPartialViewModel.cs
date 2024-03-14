using BoroHFR.Models;

namespace BoroHFR.ViewModels.Home
{
    public class EventInfoPartialViewModel
    {
        public Event Event { get; set; }
        public bool CanModify { get; set; }
        public bool IsAssociated { get; set; }
    }
}
