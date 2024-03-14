using BoroHFR.Helpers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoroHFR.Models;

public class Subject
{
    [Key]
    public SubjectId Id { get; set; }
    [MaxLength(50)]
    public required string Name { get; set; }

    public virtual List<Group> Groups { get; set; } = new List<Group>();

    public ClassId ClassId { get; set; }
    [ForeignKey(nameof(ClassId))]
    public required virtual Class Class { get; set; }
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<SubjectId>))]
public readonly record struct SubjectId(Guid value): IStrongId
{
    public override string ToString()
    {
        return value.ToString();
    }
}