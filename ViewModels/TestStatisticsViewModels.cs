using System;
using System.Collections.Generic;
using WebApplication15.Models;

namespace WebApplication15.ViewModels
{
    public class StatisticsViewModel
    {
        public List<LanguageStatisticsViewModel> LanguageStatistics { get; set; }
        public TimeStatisticsViewModel TimeStatistics { get; set; }
        public OverallStatisticsViewModel OverallStatistics { get; set; }
        public List<MonthlyProgressViewModel> MonthlyProgress { get; set; }
        public List<TestResult> RecentTests { get; set; }
    }

    public class LanguageStatisticsViewModel
    {
        public string LanguageName { get; set; }
        public string LanguageFlag { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public double AverageScore { get; set; }
        public int HighestScore { get; set; }
        public DateTime LastTestDate { get; set; }
        public List<LevelStatisticsViewModel> ResultsByLevel { get; set; }
    }

    public class LevelStatisticsViewModel
    {
        public string LevelName { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public double AverageScore { get; set; }
    }

    public class TimeStatisticsViewModel
    {
        public double AverageDuration { get; set; }
        public double TotalTimeSpent { get; set; }
        public double FastestTest { get; set; }
        public double SlowestTest { get; set; }
    }

    public class OverallStatisticsViewModel
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public double AverageScore { get; set; }
        public int TotalLanguages { get; set; }
        public string MostSuccessfulLanguage { get; set; }
        public string MostTakenLevel { get; set; }
    }

    public class MonthlyProgressViewModel
    {
        public DateTime Month { get; set; }
        public int TestsCount { get; set; }
        public double AverageScore { get; set; }
        public double PassRate { get; set; }
    }
} 