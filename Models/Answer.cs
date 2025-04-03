using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication15.Models
{
    public class Answer
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Текст ответа обязателен")]
        [Display(Name = "Текст ответа")]
        public string Text { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Вопрос")]
        public int QuestionId { get; set; }

        [Display(Name = "Правильный ответ")]
        public bool IsCorrect { get; set; } = false;

        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; } = null!;
    }
} 