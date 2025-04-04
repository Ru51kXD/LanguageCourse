using System;
using System.Collections.Generic;
using WebApplication15.Models;

namespace WebApplication15.ViewModels
{
    public class TestSubmitViewModel
    {
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public TimeSpan TimeTaken { get; set; }
        public Test Test { get; set; } = null!;
        public string LanguageName { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;
        public string ResultMessage { get; set; } = string.Empty;
        public string ScoreClass { get; set; } = string.Empty;
        public DateTime CompletedDate { get; set; }
        
        // Конструктор по умолчанию
        public TestSubmitViewModel()
        {
            CompletedDate = DateTime.Now;
        }
    }
} 