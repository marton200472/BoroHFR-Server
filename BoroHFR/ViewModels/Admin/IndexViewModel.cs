using BoroHFR.Models;

namespace BoroHFR.ViewModels.Admin
{
    public class IndexViewModel
    {
        public Group[] Groups { get; set; }
        public User[] Members { get; set; }
        public Subject[] Subjects { get; set; }
        public Subject Default { get; set; }
        public DbLesson[] Lessons { get; set; }
        public Class Class { get; set; }
    }
}
