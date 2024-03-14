namespace BoroHFR.Models
{
    public class Lesson : ICalendarEvent
    {
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string Label { get; set; } = string.Empty;
        public DbLessonId Id { get; set; }

        public TimeSpan Duration => EndTime-StartTime;

        public static Lesson FromDbLesson(DbLesson dbl, DateOnly date)
        {
            var datet = new DateTime(date, dbl.StartTime);
            var dateStartt = new DateTime(dbl.FirstDate, dbl.StartTime);
            TimeSpan utcDiff = datet - datet.ToUniversalTime();
            TimeSpan utcStartDiff = dateStartt - dateStartt.ToUniversalTime();
            return new Lesson() {
                Date = date,
                StartTime = dbl.StartTime/*.AddHours(utcStartDiff.Hours - utcDiff.Hours)*/,
                Id = dbl.Id,
                EndTime = dbl.EndTime,
                Label = dbl.Label
            };
        }
    }
}
