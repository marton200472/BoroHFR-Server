using BoroHFR.Helpers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoroHFR.Models;

public class ConversationMessage
{
    [Key]
    public ConversationMessageId Id { get; set; }
    [MaxLength(255)]
    public required string Message { get; set; }
    public required DateTimeOffset SendTime { get; set; }
    public bool Deleted { get; set; }
    public bool Edited { get; set; }

    public List<File> Attachments { get; set; } = new List<File>();

    
    public ConversationId ConversationId { get; set; }
    [ForeignKey(nameof(ConversationId))]
    public required Conversation Conversation { get; set; }

    public UserId SenderId { get; set; }

    [ForeignKey(nameof(SenderId))]
    public virtual required User Sender { get; set; }
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<ConversationMessageId>))]
public readonly record struct ConversationMessageId(Guid value) : IStrongId
{
    public override string ToString()
    {
        return value.ToString();
    }
}