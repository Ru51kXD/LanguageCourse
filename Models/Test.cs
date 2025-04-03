using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace WebApplication15.Models
{
    public class Test
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Название теста обязательно")]
        [Display(Name = "Название")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Описание")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Уровень языка обязателен")]
        [Display(Name = "Уровень языка")]
        public int LanguageLevelId { get; set; }

        [Required(ErrorMessage = "Порядок теста обязателен")]
        [Display(Name = "Порядок в уровне")]
        public int OrderInLevel { get; set; } = 1;

        [Required(ErrorMessage = "Ограничение времени обязательно")]
        [Display(Name = "Ограничение времени (мин)")]
        [Range(5, 120, ErrorMessage = "Ограничение времени должно быть от 5 до 120 минут")]
        public int TimeLimit { get; set; } = 30;

        [Required(ErrorMessage = "Проходной балл обязателен")]
        [Display(Name = "Проходной балл (%)")]
        [Range(50, 100, ErrorMessage = "Проходной балл должен быть от 50 до 100%")]
        public int PassingScore { get; set; } = 70;

        [Display(Name = "Дата создания")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public int PassRate { get; set; }

        [NotMapped]
        public int QuestionsCount => Questions?.Count ?? 0;

        [ForeignKey("LanguageLevelId")]
        public virtual LanguageLevel LanguageLevel { get; set; } = null!;

        public virtual ICollection<Question> Questions { get; set; }
        public virtual ICollection<TestResult> TestResults { get; set; }

        public Test()
        {
            Questions = new HashSet<Question>();
            TestResults = new HashSet<TestResult>();
        }
    }
} 