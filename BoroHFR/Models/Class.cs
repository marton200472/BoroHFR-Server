using BoroHFR.Helpers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoroHFR.Models;

public class Class
{
    [Key]
    public ClassId Id { get; set; }
    [MaxLength(50)]
    public required string Name { get; set; }
    public GroupId DefaultGroupId { get; set; }

    public List<User> Members { get; set; } = new List<User>();
    public List<Group> Groups { get; set; } = new List<Group>();
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<ClassId>))]
public readonly record struct ClassId(Guid value) : IStrongId
{
    public override string ToString()
    {
        return value.ToString();
    }
}