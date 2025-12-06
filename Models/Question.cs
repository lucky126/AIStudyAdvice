using System.ComponentModel.DataAnnotations;

namespace Study.Models
{
    public class Question
    {
        [Key]
        public string QuestionId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = "demo_user";

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? UserAnswer { get; set; }

        public bool? IsCorrect { get; set; }

        public string? CorrectAnswer { get; set; }

        [Required]
        public string KnowledgePoint { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = "数学";

        [Required]
        public int Grade { get; set; } = 1;

        [Required]
        public string QuestionType { get; set; } = string.Empty;

        public string ErrorAnalysis { get; set; } = string.Empty;

        [Required]
        public string PaperId { get; set; } = string.Empty;
    }
}
