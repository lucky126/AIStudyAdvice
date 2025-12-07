using System;
using System.ComponentModel.DataAnnotations;

namespace Study.Models
{
    public class AdviceHistory
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Textbook { get; set; } = string.Empty;
        [Required]
        public string RequestHash { get; set; } = string.Empty;
        [Required]
        public string ResponseContent { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    }
}
