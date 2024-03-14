using BoroHFR.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BoroHFR.ViewModels.Admin
{
    public class CreateLessonViewModel
    {
        [Required(ErrorMessage = "A mezőt kötelező kitölteni!")]
        public string Label { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public DateOnly LastDate { get; set; }
        public DateOnly FirstDate { get; set; }
        public int RepeatWeeks { get; set; }
        public Group[]? Groups { get; set; }
        public Guid SelectedGroupGuid { get; set; }
    }
}
