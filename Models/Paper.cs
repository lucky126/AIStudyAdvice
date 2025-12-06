using System.ComponentModel.DataAnnotations;

namespace Study.Models
{
    public class Paper
    {
        [Key]
        public Guid PaperId { get; set; }

        public string? UserId { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        public string? Subject { get; set; }

        public string? FileId { get; set; }
    }
}
