using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BoroHFR.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BoroHFR.Models;

public class PasswordResetToken
{
    [Key]
    public PasswordResetTokenId Token { get; set; }
    public required DateTimeOffset ValidUntil { get; set; }
    public bool Used { get; set; }

    public UserId UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public required User User { get; set; }

    public static PasswordResetToken Create(User user) => new()
    {
        Token = new(Guid.NewGuid()),
        User = user,
        ValidUntil = DateTimeOffset.UtcNow.AddHours(1)
    };
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<PasswordResetTokenId>))]
public readonly record struct PasswordResetTokenId(Guid value): IStrongId
{
    public override string ToString()
    {
        return value.ToString();
    }
}