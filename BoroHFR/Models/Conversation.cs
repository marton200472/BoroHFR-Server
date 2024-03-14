using BoroHFR.Helpers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoroHFR.Models;

public class Conversation
{
    [Key]
    public ConversationId Id { get; set; }
    [MaxLength(255)]
    public required string Name { get; set; }
    public bool IsOpen { get; set; }

    public GroupId GroupId { get; set; }
    [ForeignKey(nameof(GroupId))]
    public required Group Group { get; set; }
    public List<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();

    public List<User> Members { get; set; } = new List<User>();
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<ConversationId>))]
public readonly record struct ConversationId(Guid value) : IStrongId
{
    public override string ToString()
    {
        return value.ToString();
    }
}