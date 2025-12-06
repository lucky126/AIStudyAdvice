using System.ComponentModel.DataAnnotations;

namespace Study.Models
{
    public class KnowledgeStat
    {
        [Required]
        public string UserId { get; set; } = "demo_user";

        [Required]
        public int Grade { get; set; } = 1;

        [Required]
        public string Subject { get; set; } = "数学";

        [Required]
        public string KnowledgePoint { get; set; } = string.Empty;

        public int Total { get; set; }

        public int Correct { get; set; }

        public double Accuracy { get; set; }

        public string MasteryLevel { get; set; } = "不明白";
    }
}
