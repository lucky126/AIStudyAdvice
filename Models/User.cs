using System;
using System.ComponentModel.DataAnnotations;

namespace Study.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string Username { get; set; } = "";
        [Required]
        public string PasswordHash { get; set; } = "";
        public string Nickname { get; set; } = "";
        public DateTime RegistrationTime { get; set; } = DateTime.UtcNow;
        public string? UsedInvitationCode { get; set; }
        public int LoginCount { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
