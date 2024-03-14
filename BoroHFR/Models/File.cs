using BoroHFR.Helpers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoroHFR.Models
{
    public class File
    {
        [Key]
        public FileId Id { get; set; }
        [MaxLength(255)]
        public required string DownloadName { get; set; }
        [MaxLength(255)]
        public required string ContentType { get; set; }
        public long Size { get; set; }

        public UserId OwnerId { get; set; }

        [ForeignKey(nameof(OwnerId))]
        public required User Owner { get; set; }

        public ClassId ClassId { get; set; }

        [ForeignKey(nameof(ClassId))]
        public Class? Class { get; set; }
    }

    [TypeConverter(typeof(StronglyTypedIdTypeConverter<FileId>))]
    public readonly record struct FileId(Guid value) : IStrongId
    {
        public override string ToString()
        {
            return value.ToString();
        }
    }
}
