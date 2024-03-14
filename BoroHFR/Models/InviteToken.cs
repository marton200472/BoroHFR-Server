using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using BoroHFR.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BoroHFR.Models;

[Index(nameof(Token), IsUnique = true)]
public class InviteToken
{
    [Key]
    public InviteTokenId Id { get; set; }
    [Length(10,10)]
    [Column(TypeName = "CHAR(10)")]
    public required string Token { get; set; }
    public required DateTimeOffset ValidUntil { get; set; }
    [Column(TypeName = "varchar(10)")]
    public UserRole Role { get; set; }

    public int Usages { get; set; }

    public ClassId ClassId { get; set; }
    [ForeignKey(nameof(ClassId))]
    public virtual required Class Class { get; set; }

    public static string GenerateToken()
    {
        const string validChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return RandomNumberGenerator.GetString(validChars, 10);
    }
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<InviteTokenId>))]
public readonly record struct InviteTokenId(Guid value): IStrongId
{
    public override string ToString()
    {
        return value.ToString();
    }
}