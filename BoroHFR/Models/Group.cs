namespace BoroHFR.Models;
using BoroHFR.Helpers;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Group
{
    [Key]
    public GroupId Id { get; set; }
    [MaxLength(50)]
    public required string Name { get; set; }
    [MaxLength(50)]
    public required string Teacher { get; set; }
    public ClassId ClassId { get; set; }
    public ConversationId DefaultConversationId { get; set; }

    public required Class Class { get; set; }

    public SubjectId? SubjectId { get; set; }
    [ForeignKey(nameof(SubjectId))]
    public virtual required Subject? Subject { get; set; }

    public virtual List<User> Members { get; set; } = new List<User>();
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<GroupId>))]
public readonly record struct GroupId(Guid value): IStrongId
{
    public override string ToString()
    {
        return value.ToString();
    }
}