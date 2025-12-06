using System;
using System.ComponentModel.DataAnnotations;

namespace Study.Models
{
    public class InvitationCode
    {
        public int Id { get; set; }
        [Required]
        [StringLength(8)]
        public string Code { get; set; } = "";
        public bool IsUsed { get; set; }
        public string? UsedByUsername { get; set; }
        public DateTime? UsedTime { get; set; }
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    }
}
