namespace BoroHFR.Models
{
    public interface ICalendarEvent
    {
        public DateOnly Date { get; }
        public TimeOnly StartTime { get; }
        public TimeSpan Duration { get; }
    }
}
