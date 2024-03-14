using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BoroHFR.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BoroHFR.Models;

public class EmailConfirmationToken
{
    [Key]
    public EmailConfirmationTokenId Token { get; set; }
    public required DateTimeOffset ValidUntil { get; set; }
    public bool Used { get; set; }
    public UserId UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual required User User { get; set; }

    public static EmailConfirmationToken Create(User user) => new()
    {
        Token = new(Guid.NewGuid()),
        User = user,
        ValidUntil = DateTimeOffset.UtcNow.AddHours(1)
    };
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<EmailConfirmationTokenId>))]
public readonly record struct EmailConfirmationTokenId(Guid value): IStrongId
{
    public override string ToString()
    {
        return value.ToString();
    }
}