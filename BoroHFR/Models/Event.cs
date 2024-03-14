using BoroHFR.Helpers;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoroHFR.Models
{
    [Index(nameof(Date), IsUnique = false)]
    public class Event : ICalendarEvent
    {
        [Key]
        public EventId Id { get; set; }
        [Column(TypeName = "varchar(255)")]
        public EventType Type { get; set; }
        [Required]
        public DateOnly Date { get; set; }
        [Required]
        public TimeOnly StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        [MaxLength(255)]
        [Required(ErrorMessage = "Címet megadni kötelező!")]
        public required string Title { get; set; }
        [MaxLength(1000)]
        public string? Description { get; set; }
        public GroupId GroupId { get; set; }
        public ConversationId ConversationId { get; set; }
        public UserId CreatorId { get; set; }
        public required DateTimeOffset CreateTime { get; set; }
        public UserId? ModifierId { get; set; }
        public DateTimeOffset? ModifyTime { get; set; }

        

        [ForeignKey(nameof(GroupId))]
        public required Group? Group { get; set; }
        [ForeignKey(nameof(ConversationId))]
        public required Conversation? Conversation { get; set; }
        [ForeignKey(nameof(CreatorId))]
        public required User? Creator { get; set; }
        [ForeignKey(nameof(ModifierId))]
        public User? Modifier { get; set; }

        public List<User> AssociatedUsers { get; set; } = new List<User>();
        public List<File> AttachedFiles { get; set; } = new();

        public TimeSpan Duration => Type == EventType.Task ? TimeSpan.FromMinutes(30) : (EndTime ?? StartTime.AddHours(1)) - StartTime;
    }

    [TypeConverter(typeof(StronglyTypedIdTypeConverter<EventId>))]
    public readonly record struct EventId(Guid value) : IStrongId
    {
        public override string ToString()
        {
            return value.ToString();
        }
    }

    public enum EventType
    {
        Event, Task
    }
}
