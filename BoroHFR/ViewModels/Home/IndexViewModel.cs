using BoroHFR.Models;

namespace BoroHFR.ViewModels.Home
{
    public class IndexViewModel
    {
        public Event[] Events { get; set; }
        public ConversationId DefaultConversation { get; set; }
        public DateOnly StartDate { get; set; }
    }
}
