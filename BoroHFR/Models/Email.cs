using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoroHFR.Models
{
    public class Email(string subject, string body, params string[] recipients)
    {
        [Column(TypeName = "VARCHAR(255)")]
        public string[] Recipients { get; set; } = recipients;
        [MaxLength(255)]
        public string Subject { get; set; } = subject;
        public string Body { get; set; } = body;
    }
}
