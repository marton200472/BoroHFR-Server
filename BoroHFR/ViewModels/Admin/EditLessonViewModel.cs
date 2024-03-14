using BoroHFR.Models;
using Microsoft.AspNetCore.Antiforgery;
using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Admin
{
    public class EditLessonViewModel
    {
        public DbLesson Lesson { get; set; }
    }
}
