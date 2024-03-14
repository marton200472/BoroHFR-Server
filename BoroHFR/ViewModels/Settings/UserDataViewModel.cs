using BoroHFR.Models;

namespace BoroHFR.ViewModels.Settings
{
    public class UserDataViewModel
    {
        public string Username { get; set; }
        public string StorageUsed { get; set; }
        public UserRole Role { get; set; }
        public int EventsCreated { get; set; }
        public int HomeworksDone { get; set; }
        public int EventsAttended { get; set; }
        public DateOnly RegDate { get; set; }
    }
}
