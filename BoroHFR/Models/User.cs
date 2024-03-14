using Microsoft.EntityFrameworkCore;

namespace BoroHFR.Models;
using BoroHFR.Helpers;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Index(nameof(Username), IsUnique = true)]
[Index(nameof(EMail), IsUnique = true)]
public class User
{
    [Key]
    public UserId Id { get; set; }
    [MaxLength(20)]
    public required string Username { get; set; }
    [Column(TypeName = "CHAR(60)")]
    public required string PasswordHash { get; set; }
    [Column(TypeName = "varchar(10)")]
    public required UserRole Role { get; set; }
    [MaxLength(50)]
    public required string EMail { get; set; }
    public bool EMailConfirmed { get; set; }
    public DateTimeOffset RegisterTime { get; set; }

    public ulong ButtonGameClicks { get; set; }

    public ClassId ClassId { get; set; }
    [ForeignKey(nameof(ClassId))]
    public virtual required Class Class { get; set; }

    public virtual List<Group> Groups { get; set; } = new List<Group>();
    public virtual List<Conversation> Conversations { get; set; } = new List<Conversation>();
    public virtual List<Event> AssociatedEvents { get; set; } = new List<Event>();
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<UserId>))]
public readonly record struct UserId(Guid value) : IStrongId
{
    public override string ToString()
    {
        return value.ToString();
    }
}

public enum UserRole
{
    User, Admin
}