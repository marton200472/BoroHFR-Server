using BoroHFR.Models;

namespace BoroHFR.ViewModels.Tasks
{
    public class TasksViewModel
    {
        public Event[] Tasks { get; set; }
        public bool IsUserAdmin { get; set; }
        public UserId UserId { get; set; }
    }
}
