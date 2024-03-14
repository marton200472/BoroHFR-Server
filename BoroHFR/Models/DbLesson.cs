using BoroHFR.Helpers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BoroHFR.Models
{
    public class DbLesson
    {
        [Key]
        public DbLessonId Id { get; set; }
        
        [MaxLength(255)]
        [Required(ErrorMessage = "A mezőt ki kell tölteni!")]
        public string Label { get; set; } = string.Empty;
        public DateOnly FirstDate { get; set; }
        public DateOnly? LastDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int? RepeatWeeks { get; set; }

        public GroupId GroupId { get; set; }

        [ForeignKey(nameof(GroupId))]
        public Group? Group { get; set; } = null!;


        public IEnumerable<Lesson> GetLessonsInRange(DateOnly start, DateOnly end)
        {
            DateOnly date = FirstDate;

            if (date > end)
            {
                yield break;
            }

            if (RepeatWeeks is null || LastDate is null)
            {
                if (date.IsBetween(start,end))
                {
                    yield return Lesson.FromDbLesson(this, date);
                }
                yield break;
            }

            while (date <= end && !date.IsBetween(start, end))
            {
                date = date.AddDays(RepeatWeeks.Value*7);
            }

            while (date.IsBetween(start,end))
            {
                yield return Lesson.FromDbLesson(this, date);
                date = date.AddDays(RepeatWeeks.Value*7);
            }
        }
    }

    public static class DateOnlyExtensions
    {
        public static bool IsBetween(this DateOnly d, DateOnly start, DateOnly end)
        {
            return d >= start && d <= end;
        }
    }

    [TypeConverter(typeof(StronglyTypedIdTypeConverter<DbLessonId>))]
    public readonly record struct DbLessonId(Guid value) : IStrongId
    {
        public override string ToString()
        {
            return value.ToString();
        }
    }
}
