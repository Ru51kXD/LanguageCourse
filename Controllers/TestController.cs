using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebApplication15.Data;
using WebApplication15.Models;
using WebApplication15.Services;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;
using System;
using WebApplication15.ViewModels;

namespace WebApplication15.Controllers
{
    public class TestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ThemeService _themeService;
        private readonly LanguageService _languageService;
        private readonly AuthService _authService;
        private readonly ILogger<TestController> _logger;
        private readonly TestService _testService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TestController(
            ApplicationDbContext context,
            ThemeService themeService,
            LanguageService languageService,
            AuthService authService,
            ILogger<TestController> logger,
            TestService testService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _themeService = themeService;
            _languageService = languageService;
            _authService = authService;
            _logger = logger;
            _testService = testService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? language = null, string? level = null)
        {
            // Устанавливаем ViewData для представления
            ViewData["IsDarkMode"] = _themeService.GetCurrentTheme();
            ViewData["CurrentLanguage"] = _languageService.GetCurrentLanguage();
            ViewData["IsAuthenticated"] = _authService.IsAuthenticated();
            
            // Получаем языки и уровни
            var languages = await _context.Languages.ToListAsync();
            ViewData["Languages"] = languages;
            
            var levels = await _context.LanguageLevels.ToListAsync();
            ViewData["Levels"] = levels;

            // Сохраняем выбранные параметры фильтрации
            ViewData["SelectedLanguage"] = language;
            ViewData["SelectedLevel"] = level;

            // Получаем тесты с включением связанных данных
            IQueryable<Test> testsQuery = _context.Tests
                .Include(t => t.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .Include(t => t.Questions)
                .Where(t => t.IsActive);

            // Применяем фильтрацию по языку
            if (!string.IsNullOrEmpty(language))
            {
                testsQuery = testsQuery.Where(t => t.LanguageLevel.Language.Name == language);
            }

            // Применяем фильтрацию по уровню
            if (!string.IsNullOrEmpty(level))
            {
                testsQuery = testsQuery.Where(t => t.LanguageLevel.Name == level);
            }
            
            // Если пользователь авторизован, получаем информацию о пройденных тестах
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var completedTests = await _context.TestResults
                    .Where(tr => tr.UserId == userId)
                    .ToListAsync();
                
                // Отмечаем тесты, которые пользователь уже прошел
                foreach (var result in completedTests)
                {
                    ViewData[$"Completed_{result.TestId}"] = true;
                }
            }
            
            // Преобразуем IQueryable<Test> в List<Test> перед передачей в представление
            var testsList = await testsQuery.ToListAsync();
            return View(testsList);
        }

        public async Task<IActionResult> Take(int id)
        {
            var test = await _testService.GetTestByIdAsync(id);
            if (test == null)
            {
                return NotFound();
            }

            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            ViewBag.IsAuthenticated = _authService.IsAuthenticated();

            var model = new TestViewModel
            {
                TestId = test.Id,
                TestTitle = test.Title,
                Language = test.LanguageLevel?.Language?.Name ?? "Неизвестный язык",
                Level = test.LanguageLevel?.Name ?? "Неизвестный уровень",
                TimeLimit = test.TimeLimit,
                Questions = test.Questions.Select(q => new QuestionViewModel
                {
                    Id = q.Id,
                    Text = q.Text,
                    Options = q.Options.Select(o => new OptionViewModel
                    {
                        Id = o.Id,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] TestSubmission submission)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Получаем ID пользователя
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Пользователь не авторизован");
            }

            try
            {
                // Получаем тест из базы данных
                var test = await _testService.GetTestByIdAsync(submission.TestId);
                if (test == null)
                {
                    return NotFound($"Тест с ID {submission.TestId} не найден");
                }

                // Подсчет правильных ответов
                int correctCount = 0;
                int totalQuestions = test.Questions.Count;

                // Проверяем каждый вопрос
                foreach (var question in test.Questions)
                {
                    // Проверяем, есть ли ответ пользователя для этого вопроса
                    if (submission.Answers.TryGetValue(question.Id, out string userAnswer))
                    {
                        // Ищем правильный ответ
                        var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                        if (correctOption != null && correctOption.Text == userAnswer)
                        {
                            correctCount++;
                        }
                    }
                }

                // Рассчитываем процент правильных ответов
                decimal score = totalQuestions > 0 
                    ? Math.Round((decimal)correctCount / totalQuestions * 100, 2) 
                    : 0;

                // Сохраняем результат в базе данных
                var testResult = new TestResult
                {
                    TestId = submission.TestId,
                    UserId = userId,
                    Score = correctCount,
                    MaxScore = totalQuestions,
                    Percentage = (double)score,
                    CorrectAnswers = correctCount,
                    TotalQuestions = totalQuestions,
                    IsPassed = score >= test.PassingScore,
                    CompletedDate = DateTime.Now,
                    Language = test.LanguageLevel?.Language?.Name ?? "Неизвестный",
                    Level = test.LanguageLevel?.Name ?? "Неизвестный",
                    TestTitle = test.Title
                };

                _context.TestResults.Add(testResult);
                await _context.SaveChangesAsync();

                // Возвращаем результат
                return Ok(new 
                {
                    ResultId = testResult.Id,
                    testResult.Percentage,
                    testResult.CorrectAnswers,
                    testResult.TotalQuestions,
                    testResult.IsPassed,
                    testResult.TestTitle,
                    testResult.Language,
                    testResult.Level
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке результатов теста");
                return StatusCode(500, "Произошла ошибка при обработке результатов теста");
            }
        }

        public async Task<IActionResult> Results(int? id = null)
        {
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            ViewBag.IsAuthenticated = _authService.IsAuthenticated();

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Получаем все результаты тестов для пользователя
            var results = await _context.TestResults
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CompletedDate)
                .ToListAsync();

            if (id.HasValue)
            {
                // Если указан id, показываем результат конкретного теста
                var result = results.FirstOrDefault(r => r.Id == id.Value);
                if (result == null)
                {
                    return NotFound();
                }
                ViewBag.SelectedResult = result;
            }

            return View(results);
        }

        [HttpGet]
        public async Task<IActionResult> GetLanguageLevels(int languageId)
        {
            var levels = await _context.LanguageLevels
                .Where(ll => ll.LanguageId == languageId)
                .OrderBy(ll => ll.Level)
                .Select(ll => new { ll.Id, ll.Name })
                .ToListAsync();

            return Json(levels);
        }

        public async Task<IActionResult> StartTest(int id)
        {
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            ViewBag.IsAuthenticated = _authService.IsAuthenticated();

            var test = await _testService.GetTestByIdAsync(id);
            if (test == null)
            {
                return NotFound();
            }

            var model = new TestViewModel
            {
                TestId = test.Id,
                TestTitle = test.Title,
                Language = test.LanguageLevel?.Language?.Name ?? "Неизвестный язык",
                Level = test.LanguageLevel?.Name ?? "Неизвестный уровень",
                TimeLimit = test.TimeLimit,
                Questions = test.Questions.Select(q => new QuestionViewModel
                {
                    Id = q.Id,
                    Text = q.Text,
                    Options = q.Options.Select(o => new OptionViewModel
                    {
                        Id = o.Id,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                }).ToList()
            };

            return View(model);
        }

        // Метод для получения демо теста по ID
        private dynamic GetDemoTestById(int id)
        {
            // Список демо-тестов
            var demoTests = new List<dynamic> 
            {
                new {
                    Id = 1,
                    Language = "Английский",
                    Level = "A1",
                    Title = "Базовый тест по английскому языку",
                    Description = "Проверьте свои начальные знания английского языка уровня A1 (Elementary) с нашим базовым тестом на словарный запас и грамматику.",
                    Questions = new List<dynamic> {
                        new { 
                            Id = 1, 
                            Text = "Как будет 'Здравствуйте' на английском?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Hello", IsCorrect = true },
                                new { Id = 2, Text = "Goodbye", IsCorrect = false },
                                new { Id = 3, Text = "Thank you", IsCorrect = false },
                                new { Id = 4, Text = "Please", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 2, 
                            Text = "Выберите правильный артикль: I have ___ apple.", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "a", IsCorrect = true },
                                new { Id = 2, Text = "an", IsCorrect = false },
                                new { Id = 3, Text = "the", IsCorrect = false },
                                new { Id = 4, Text = "не требуется артикль", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 3, 
                            Text = "Выберите правильный перевод фразы 'Меня зовут Анна':", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "My name Anna", IsCorrect = false },
                                new { Id = 2, Text = "I am name Anna", IsCorrect = false },
                                new { Id = 3, Text = "Me name is Anna", IsCorrect = false },
                                new { Id = 4, Text = "My name is Anna", IsCorrect = true }
                            }
                        },
                        new { 
                            Id = 4, 
                            Text = "Как будет 'яблоко' на английском?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "apricot", IsCorrect = false },
                                new { Id = 2, Text = "apple", IsCorrect = true },
                                new { Id = 3, Text = "application", IsCorrect = false },
                                new { Id = 4, Text = "appear", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 5, 
                            Text = "Выберите правильное местоимение: ___ am a student.", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "I", IsCorrect = true },
                                new { Id = 2, Text = "He", IsCorrect = false },
                                new { Id = 3, Text = "You", IsCorrect = false },
                                new { Id = 4, Text = "We", IsCorrect = false }
                            }
                        }
                    },
                    QuestionsCount = 5,
                    TimeLimit = 10
                },
                new {
                    Id = 2,
                    Language = "Английский",
                    Level = "A2",
                    Title = "Английский для начинающих (A2)",
                    Description = "Проверьте свои знания английского языка уровня A2 (Pre-Intermediate) с этим тестом на базовую грамматику и лексику.",
                    Questions = new List<dynamic> {
                        new { 
                            Id = 1, 
                            Text = "Choose the correct sentence:", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "I don't like never pizza", IsCorrect = false },
                                new { Id = 2, Text = "I never like pizza", IsCorrect = false },
                                new { Id = 3, Text = "I never liking pizza", IsCorrect = false },
                                new { Id = 4, Text = "I never like to eat pizza", IsCorrect = true }
                            }
                        },
                        new { 
                            Id = 2, 
                            Text = "Complete the sentence: 'They ___ in London since 2010.'", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "live", IsCorrect = false },
                                new { Id = 2, Text = "lived", IsCorrect = false },
                                new { Id = 3, Text = "have lived", IsCorrect = true },
                                new { Id = 4, Text = "living", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 3, 
                            Text = "Choose the correct question:", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Where you are going?", IsCorrect = false },
                                new { Id = 2, Text = "Where are you going?", IsCorrect = true },
                                new { Id = 3, Text = "Where going are you?", IsCorrect = false },
                                new { Id = 4, Text = "Where go you?", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 4, 
                            Text = "What's the opposite of 'expensive'?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "cheap", IsCorrect = true },
                                new { Id = 2, Text = "costly", IsCorrect = false },
                                new { Id = 3, Text = "rich", IsCorrect = false },
                                new { Id = 4, Text = "poor", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 5, 
                            Text = "Choose the correct sentence:", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "I'm too tired for go out tonight", IsCorrect = false },
                                new { Id = 2, Text = "I'm too tired to going out tonight", IsCorrect = false },
                                new { Id = 3, Text = "I'm too tired to go out tonight", IsCorrect = true },
                                new { Id = 4, Text = "I'm too tired for going out tonight", IsCorrect = false }
                            }
                        }
                    }
                },
                new {
                    Id = 3,
                    Language = "Английский",
                    Level = "B1",
                    Title = "Английский средний уровень (B1)",
                    Description = "Тест на средний уровень владения английским языком (B1). Проверьте свои знания грамматики, словарного запаса и понимания текста.",
                    Questions = new List<dynamic> {
                        new { 
                            Id = 1, 
                            Text = "If I ___ rich, I would buy a big house.", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "am", IsCorrect = false },
                                new { Id = 2, Text = "would be", IsCorrect = false },
                                new { Id = 3, Text = "were", IsCorrect = true },
                                new { Id = 4, Text = "will be", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 2, 
                            Text = "She asked me if I ___ to the party.", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "am going", IsCorrect = false },
                                new { Id = 2, Text = "was going", IsCorrect = true },
                                new { Id = 3, Text = "would go", IsCorrect = false },
                                new { Id = 4, Text = "went", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 3, 
                            Text = "By the time we arrived, the film ___.", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "has started", IsCorrect = false },
                                new { Id = 2, Text = "had started", IsCorrect = true },
                                new { Id = 3, Text = "was starting", IsCorrect = false },
                                new { Id = 4, Text = "started", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 4, 
                            Text = "I wish I ___ how to swim when I was younger.", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "learned", IsCorrect = false },
                                new { Id = 2, Text = "would learn", IsCorrect = false },
                                new { Id = 3, Text = "have learned", IsCorrect = false },
                                new { Id = 4, Text = "had learned", IsCorrect = true }
                            }
                        }
                    }
                },
                new {
                    Id = 4,
                    Language = "Казахский",
                    Level = "A1",
                    Title = "Базовый тест по казахскому языку",
                    Description = "Проверьте свои начальные знания казахского языка (A1). Базовые слова, приветствия и простые фразы.",
                    Questions = new List<dynamic> {
                        new { 
                            Id = 1, 
                            Text = "Как будет 'Здравствуйте' на казахском?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Сәлем", IsCorrect = false },
                                new { Id = 2, Text = "Сау болыңыз", IsCorrect = false },
                                new { Id = 3, Text = "Сәлеметсіз бе", IsCorrect = true },
                                new { Id = 4, Text = "Рақмет", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 2, 
                            Text = "Как сказать 'Меня зовут...' на казахском?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Менің атым...", IsCorrect = true },
                                new { Id = 2, Text = "Сіздің атыңыз...", IsCorrect = false },
                                new { Id = 3, Text = "Сенің есімің...", IsCorrect = false },
                                new { Id = 4, Text = "Мен...", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 3, 
                            Text = "Как будет 'Спасибо' на казахском?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Сәлем", IsCorrect = false },
                                new { Id = 2, Text = "Рақмет", IsCorrect = true },
                                new { Id = 3, Text = "Кешіріңіз", IsCorrect = false },
                                new { Id = 4, Text = "Сау болыңыз", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 4, 
                            Text = "Что означает слово 'Жақсы'?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Плохо", IsCorrect = false },
                                new { Id = 2, Text = "Хорошо", IsCorrect = true },
                                new { Id = 3, Text = "До свидания", IsCorrect = false },
                                new { Id = 4, Text = "Здравствуйте", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 5, 
                            Text = "Как будет 'вода' на казахском?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Нан", IsCorrect = false },
                                new { Id = 2, Text = "Ет", IsCorrect = false },
                                new { Id = 3, Text = "Су", IsCorrect = true },
                                new { Id = 4, Text = "Шай", IsCorrect = false }
                            }
                        }
                    }
                },
                new {
                    Id = 5,
                    Language = "Казахский",
                    Level = "A2",
                    Title = "Казахский для начинающих (A2)",
                    Description = "Проверьте свои знания казахского языка уровня A2. Базовая грамматика, повседневные фразы и простые диалоги.",
                    Questions = new List<dynamic> {
                        new { 
                            Id = 1, 
                            Text = "Какое окончание используется для множественного числа в казахском языке?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "-лар/-лер", IsCorrect = true },
                                new { Id = 2, Text = "-мен/-бен", IsCorrect = false },
                                new { Id = 3, Text = "-да/-де", IsCorrect = false },
                                new { Id = 4, Text = "-ны/-ні", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 2, 
                            Text = "Как правильно сказать 'Я не понимаю' на казахском языке?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Мен түсінемін", IsCorrect = false },
                                new { Id = 2, Text = "Мен түсінбеймін", IsCorrect = true },
                                new { Id = 3, Text = "Мен білмеймін", IsCorrect = false },
                                new { Id = 4, Text = "Мен айтамын", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 3, 
                            Text = "Выберите правильный порядок слов в предложении 'Я иду в школу':", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Мен мектепке барамын", IsCorrect = true },
                                new { Id = 2, Text = "Мен барамын мектепке", IsCorrect = false },
                                new { Id = 3, Text = "Мектепке мен барамын", IsCorrect = false },
                                new { Id = 4, Text = "Барамын мен мектепке", IsCorrect = false }
                            }
                        }
                    }
                },
                new {
                    Id = 6,
                    Language = "Турецкий",
                    Level = "A1",
                    Title = "Базовый тест по турецкому языку",
                    Description = "Начальный тест по турецкому языку (A1). Проверьте свои знания базовых слов, приветствий и простых фраз.",
                    Questions = new List<dynamic> {
                        new { 
                            Id = 1, 
                            Text = "Как будет 'Здравствуйте' на турецком?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Merhaba", IsCorrect = true },
                                new { Id = 2, Text = "Güle güle", IsCorrect = false },
                                new { Id = 3, Text = "Teşekkürler", IsCorrect = false },
                                new { Id = 4, Text = "Görüşürüz", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 2, 
                            Text = "Как будет 'Спасибо' на турецком?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Lütfen", IsCorrect = false },
                                new { Id = 2, Text = "Teşekkürler", IsCorrect = true },
                                new { Id = 3, Text = "Affedersiniz", IsCorrect = false },
                                new { Id = 4, Text = "Merhaba", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 3, 
                            Text = "Как сказать 'Меня зовут...' на турецком?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Ben...", IsCorrect = false },
                                new { Id = 2, Text = "Adım...", IsCorrect = true },
                                new { Id = 3, Text = "Senin adın...", IsCorrect = false },
                                new { Id = 4, Text = "Benim...", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 4, 
                            Text = "Как будет 'вода' на турецком?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Ekmek", IsCorrect = false },
                                new { Id = 2, Text = "Çay", IsCorrect = false },
                                new { Id = 3, Text = "Su", IsCorrect = true },
                                new { Id = 4, Text = "Kahve", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 5, 
                            Text = "Что означает слово 'Evet'?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Нет", IsCorrect = false },
                                new { Id = 2, Text = "Да", IsCorrect = true },
                                new { Id = 3, Text = "Пожалуйста", IsCorrect = false },
                                new { Id = 4, Text = "Спасибо", IsCorrect = false }
                            }
                        }
                    }
                },
                new {
                    Id = 7,
                    Language = "Турецкий",
                    Level = "A2",
                    Title = "Турецкий для начинающих (A2)",
                    Description = "Проверьте свои знания турецкого языка уровня A2. Базовая грамматика, времена и повседневные выражения.",
                    Questions = new List<dynamic> {
                        new { 
                            Id = 1, 
                            Text = "Выберите правильное местоимение: '... Ankara'da yaşıyorum.' (Я живу в Анкаре)", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "O", IsCorrect = false },
                                new { Id = 2, Text = "Sen", IsCorrect = false },
                                new { Id = 3, Text = "Ben", IsCorrect = true },
                                new { Id = 4, Text = "Biz", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 2, 
                            Text = "Как правильно сказать 'Я не понимаю' на турецком?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Anlıyorum", IsCorrect = false },
                                new { Id = 2, Text = "Anlamıyorum", IsCorrect = true },
                                new { Id = 3, Text = "Bilmiyorum", IsCorrect = false },
                                new { Id = 4, Text = "Söylüyorum", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 3, 
                            Text = "Какое окончание используется для локативного падежа (где?) в турецком языке?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "-i/-ı/-u/-ü", IsCorrect = false },
                                new { Id = 2, Text = "-e/-a", IsCorrect = false },
                                new { Id = 3, Text = "-de/-da", IsCorrect = true },
                                new { Id = 4, Text = "-den/-dan", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 4, 
                            Text = "Как будет 'вчера' на турецком?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "Yarın", IsCorrect = false },
                                new { Id = 2, Text = "Dün", IsCorrect = true },
                                new { Id = 3, Text = "Bugün", IsCorrect = false },
                                new { Id = 4, Text = "Şimdi", IsCorrect = false }
                            }
                        }
                    }
                },
                new {
                    Id = 8,
                    Language = "Английский",
                    Level = "B2",
                    Title = "Английский выше среднего (B2)",
                    Description = "Тест на уровень английского языка B2 (Upper-Intermediate). Сложные грамматические конструкции, идиомы и фразовые глаголы.",
                    Questions = new List<dynamic> {
                        new { 
                            Id = 1, 
                            Text = "Choose the correct phrasal verb: 'The plane will ___ at 10 am.'", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "take up", IsCorrect = false },
                                new { Id = 2, Text = "take off", IsCorrect = true },
                                new { Id = 3, Text = "take away", IsCorrect = false },
                                new { Id = 4, Text = "take down", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 2, 
                            Text = "Complete the sentence: 'If I ___ you, I would apply for that job.'", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "am", IsCorrect = false },
                                new { Id = 2, Text = "would be", IsCorrect = false },
                                new { Id = 3, Text = "were", IsCorrect = true },
                                new { Id = 4, Text = "had been", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 3, 
                            Text = "Choose the correct sentence:", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "No sooner I had arrived than it started to rain", IsCorrect = false },
                                new { Id = 2, Text = "No sooner had I arrived than it started to rain", IsCorrect = true },
                                new { Id = 3, Text = "No sooner I arrived than it started to rain", IsCorrect = false },
                                new { Id = 4, Text = "No sooner did I arrive than it had started to rain", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 4, 
                            Text = "What does the idiom 'to be on cloud nine' mean?", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "To be very sad", IsCorrect = false },
                                new { Id = 2, Text = "To be extremely happy", IsCorrect = true },
                                new { Id = 3, Text = "To be confused", IsCorrect = false },
                                new { Id = 4, Text = "To be angry", IsCorrect = false }
                            }
                        },
                        new { 
                            Id = 5, 
                            Text = "Complete the sentence: 'She ___ her wallet stolen while she was shopping.'", 
                            Options = new List<dynamic> {
                                new { Id = 1, Text = "has", IsCorrect = false },
                                new { Id = 2, Text = "was", IsCorrect = false },
                                new { Id = 3, Text = "had", IsCorrect = true },
                                new { Id = 4, Text = "got", IsCorrect = false }
                            }
                        }
                    }
                }
            };

            return demoTests.FirstOrDefault(t => t.Id == id);
        }

        // Метод для сохранения результатов теста
        [Authorize]
        [HttpPost]
        public IActionResult SaveTestResult([FromBody] TestResultModel resultModel)
        {
            try
            {
                if (resultModel == null)
                {
                    return BadRequest("Данные результата теста отсутствуют");
                }

                var userId = _authService.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Пользователь не авторизован");
                }

                // Создаем новую запись о результате теста
                var testResult = new TestResult
                {
                    TestId = resultModel.TestId,
                    UserId = userId,
                    Score = resultModel.Score,
                    MaxScore = resultModel.MaxScore,
                    Percentage = resultModel.Percentage,
                    CompletedDate = DateTime.Now,
                    Language = resultModel.Language,
                    Level = resultModel.Level,
                    Title = resultModel.Title,
                    CorrectAnswers = resultModel.CorrectAnswers,
                    TotalQuestions = resultModel.TotalQuestions,
                    IsPassed = resultModel.Percentage >= 70
                };

                _context.TestResults.Add(testResult);
                _context.SaveChanges();

                return Ok(new { Success = true, ResultId = testResult.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении результата теста");
                return StatusCode(500, "Произошла ошибка при сохранении результата теста");
            }
        }

        // Метод для отображения результатов тестов в личном кабинете
        [Authorize]
        public IActionResult UserResults()
        {
            var userId = _authService.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var results = _context.TestResults
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CompletedDate)
                .ToList();

            // Группируем результаты по языкам
            var groupedResults = results
                .GroupBy(r => r.Language)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Вычисляем общую статистику
            var totalTests = results.Count;
            var averageScore = results.Any() ? results.Average(r => r.Percentage) : 0;
            var bestResult = results.Any() ? results.Max(r => r.Percentage) : 0;
            var languageProgress = groupedResults.ToDictionary(
                g => g.Key, 
                g => new { 
                    Count = g.Value.Count,
                    AverageScore = g.Value.Average(r => r.Percentage),
                    HighestLevel = g.Value.OrderByDescending(r => r.Level).FirstOrDefault()?.Level
                }
            );

            ViewData["TotalTests"] = totalTests;
            ViewData["AverageScore"] = averageScore;
            ViewData["BestResult"] = bestResult;
            ViewData["LanguageProgress"] = languageProgress;

            return View(results);
        }

        // Метод для ручного создания тестов
        [HttpGet("CreateDemoTests")]
        public async Task<IActionResult> CreateDemoTests()
        {
            try
            {
                // Проверяем, есть ли уже тесты
                if (await _context.Tests.AnyAsync())
                {
                    // Если в базе уже есть тесты, удаляем их
                    var existingTests = await _context.Tests.ToListAsync();
                    _context.Tests.RemoveRange(existingTests);
                    
                    // Удаляем связанные вопросы
                    var existingQuestions = await _context.Questions.ToListAsync();
                    _context.Questions.RemoveRange(existingQuestions);
                    
                    // Удаляем связанные варианты ответов
                    var existingOptions = await _context.Options.ToListAsync();
                    _context.Options.RemoveRange(existingOptions);
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Существующие тесты удалены");
                }
                
                // Получаем все языки
                var languages = await _context.Languages.ToListAsync();
                if (languages.Count == 0)
                {
                    return BadRequest("Языки не найдены в базе данных");
                }
                
                // Получаем все уровни языков
                var languageLevels = await _context.LanguageLevels
                    .Include(ll => ll.Language)
                    .ToListAsync();
                    
                if (languageLevels.Count == 0)
                {
                    return BadRequest("Уровни языков не найдены в базе данных");
                }
                
                _logger.LogInformation($"Найдено {languages.Count} языков и {languageLevels.Count} уровней языков");
                
                // Создаем тесты для всех языков и уровней
                var allTests = new List<Test>();
                int testCount = 0;
                
                foreach (var language in languages)
                {
                    var levelsForLanguage = languageLevels.Where(ll => ll.LanguageId == language.Id).ToList();
                    _logger.LogInformation($"Для языка {language.Name} найдено {levelsForLanguage.Count} уровней");
                    
                    foreach (var level in levelsForLanguage)
                    {
                        // Создаем два теста для каждого уровня языка
                        for (int i = 1; i <= 2; i++)
                        {
                            var test = new Test
                            {
                                Title = GetTestTitle(language.Name, level.Name, i),
                                Description = GetTestDescription(language.Name, level.Name, i),
                                LanguageLevelId = level.Id,
                                TimeLimit = GetTimeLimit(level.Name),
                                PassingScore = 70,
                                OrderInLevel = i,
                                IsActive = true,
                                CreatedDate = DateTime.Now,
                                Questions = GetQuestionsForTest(language.Name, level.Name, i)
                            };
                            
                            allTests.Add(test);
                            testCount++;
                        }
                    }
                }
                
                // Добавляем тесты в базу данных
                await _context.Tests.AddRangeAsync(allTests);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"В базу данных добавлено {testCount} тестов");
                
                return Ok($"Успешно создано {testCount} тестов для {languages.Count} языков");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании тестов");
                return StatusCode(500, $"Ошибка при создании тестов: {ex.Message}");
            }
        }

        // Вспомогательные методы для генерации тестов
        private string GetTestTitle(string language, string level, int orderInLevel)
        {
            // Словарь названий тестов для каждого языка и уровня
            var titles = new Dictionary<string, Dictionary<string, string[]>>
            {
                ["Английский"] = new Dictionary<string, string[]>
                {
                    ["A1"] = new[] { "Базовый тест по английскому языку", "Английская грамматика для начинающих" },
                    ["A2"] = new[] { "Тест по английскому языку (A2)", "Английская грамматика времен" },
                    ["B1"] = new[] { "Английский средний уровень (B1)", "Английские идиомы и фразовые глаголы (B1)" },
                    ["B2"] = new[] { "Английский выше среднего (B2)", "Сложные конструкции в английском (B2)" },
                    ["C1"] = new[] { "Продвинутый английский (C1)", "Академический английский (C1)" },
                    ["C2"] = new[] { "Профессиональный английский (C2)", "Литературный английский (C2)" }
                },
                ["Казахский"] = new Dictionary<string, string[]>
                {
                    ["A1"] = new[] { "Базовый тест по казахскому языку", "Казахский язык - Числа и цвета" },
                    ["A2"] = new[] { "Казахский для начинающих (A2)", "Казахская грамматика (A2)" },
                    ["B1"] = new[] { "Казахский средний уровень (B1)", "Казахские идиомы и выражения (B1)" },
                    ["B2"] = new[] { "Казахский выше среднего (B2)", "Казахская литература (B2)" },
                    ["C1"] = new[] { "Продвинутый казахский (C1)", "Деловой казахский (C1)" },
                    ["C2"] = new[] { "Профессиональный казахский (C2)", "Академический казахский (C2)" }
                },
                ["Турецкий"] = new Dictionary<string, string[]>
                {
                    ["A1"] = new[] { "Базовый тест по турецкому языку", "Турецкие числа и дни недели" },
                    ["A2"] = new[] { "Турецкий для начинающих (A2)", "Турецкая грамматика (A2)" },
                    ["B1"] = new[] { "Турецкий средний уровень (B1)", "Турецкие идиомы и выражения (B1)" },
                    ["B2"] = new[] { "Турецкий выше среднего (B2)", "Турецкая литература (B2)" },
                    ["C1"] = new[] { "Продвинутый турецкий (C1)", "Деловой турецкий (C1)" },
                    ["C2"] = new[] { "Профессиональный турецкий (C2)", "Академический турецкий (C2)" }
                },
                ["Русский"] = new Dictionary<string, string[]>
                {
                    ["A1"] = new[] { "Базовый тест по русскому языку", "Русский алфавит и произношение" },
                    ["A2"] = new[] { "Русский для начинающих (A2)", "Русская грамматика (A2)" },
                    ["B1"] = new[] { "Русский средний уровень (B1)", "Русские идиомы и выражения (B1)" },
                    ["B2"] = new[] { "Русский выше среднего (B2)", "Русская литература (B2)" },
                    ["C1"] = new[] { "Продвинутый русский (C1)", "Деловой русский (C1)" },
                    ["C2"] = new[] { "Профессиональный русский (C2)", "Академический русский (C2)" }
                }
            };

            // Получаем название теста из словаря или генерируем стандартное название
            if (titles.ContainsKey(language) && titles[language].ContainsKey(level) && orderInLevel <= titles[language][level].Length)
            {
                return titles[language][level][orderInLevel - 1];
            }
            
            return $"Тест по {language} языку ({level}) - #{orderInLevel}";
        }

        private string GetTestDescription(string language, string level, int orderInLevel)
        {
            var levelNames = new Dictionary<string, string>
            {
                ["A1"] = "начальный",
                ["A2"] = "элементарный",
                ["B1"] = "средний",
                ["B2"] = "выше среднего",
                ["C1"] = "продвинутый",
                ["C2"] = "профессиональный"
            };
            
            var levelDesc = levelNames.ContainsKey(level) ? levelNames[level] : level;
            
            if (orderInLevel == 1)
            {
                return $"Проверьте свои знания {language.ToLower()} языка уровня {level} ({levelDesc}). Этот тест направлен на проверку словарного запаса и базовых грамматических конструкций.";
            }
            else
            {
                return $"Расширенный тест по {language.ToLower()} языку уровня {level} ({levelDesc}). Проверьте свои знания грамматики, идиом и сложных конструкций.";
            }
        }

        private int GetTimeLimit(string level)
        {
            return level switch
            {
                "A1" => 10,
                "A2" => 15,
                "B1" => 20,
                "B2" => 25,
                "C1" => 30,
                "C2" => 40,
                _ => 15
            };
        }

        // Метод для создания вопросов для теста
        private List<Question> GetQuestionsForTest(string language, string level, int testNumber)
        {
            var questions = new List<Question>();
            
            // В зависимости от языка, уровня и номера теста создаем разные вопросы
            if (language == "Английский")
            {
                if (level == "A1")
                {
                    if (testNumber == 1) // Словарный запас
                    {
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный перевод: 'Дом'",
                            CorrectAnswer = "House",
                            Options = new List<Option>
                            {
                                new Option { Text = "House", IsCorrect = true },
                                new Option { Text = "Car", IsCorrect = false },
                                new Option { Text = "Tree", IsCorrect = false },
                                new Option { Text = "Door", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Что из этого НЕ является цветом?",
                            CorrectAnswer = "Table",
                            Options = new List<Option>
                            {
                                new Option { Text = "Red", IsCorrect = false },
                                new Option { Text = "Blue", IsCorrect = false },
                                new Option { Text = "Green", IsCorrect = false },
                                new Option { Text = "Table", IsCorrect = true }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильную пару противоположностей",
                            CorrectAnswer = "Hot - Cold",
                            Options = new List<Option>
                            {
                                new Option { Text = "Big - Small", IsCorrect = false },
                                new Option { Text = "Hot - Cold", IsCorrect = true },
                                new Option { Text = "Fast - Quick", IsCorrect = false },
                                new Option { Text = "Happy - Glad", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Какое слово обозначает члена семьи?",
                            CorrectAnswer = "Brother",
                            Options = new List<Option>
                            {
                                new Option { Text = "Friend", IsCorrect = false },
                                new Option { Text = "Teacher", IsCorrect = false },
                                new Option { Text = "Brother", IsCorrect = true },
                                new Option { Text = "Doctor", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Переведите на английский: 'Доброе утро'",
                            CorrectAnswer = "Good morning",
                            Options = new List<Option>
                            {
                                new Option { Text = "Good night", IsCorrect = false },
                                new Option { Text = "Good afternoon", IsCorrect = false },
                                new Option { Text = "Good evening", IsCorrect = false },
                                new Option { Text = "Good morning", IsCorrect = true }
                            }
                        });
                    }
                    else // Грамматика
                    {
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный артикль: I see ___ apple.",
                            CorrectAnswer = "an",
                            Options = new List<Option>
                            {
                                new Option { Text = "a", IsCorrect = false },
                                new Option { Text = "an", IsCorrect = true },
                                new Option { Text = "the", IsCorrect = false },
                                new Option { Text = "не нужен артикль", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильную форму глагола: She ___ TV every day.",
                            CorrectAnswer = "watches",
                            Options = new List<Option>
                            {
                                new Option { Text = "watch", IsCorrect = false },
                                new Option { Text = "watches", IsCorrect = true },
                                new Option { Text = "watching", IsCorrect = false },
                                new Option { Text = "to watch", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный предлог: The book is ___ the table.",
                            CorrectAnswer = "on",
                            Options = new List<Option>
                            {
                                new Option { Text = "in", IsCorrect = false },
                                new Option { Text = "at", IsCorrect = false },
                                new Option { Text = "on", IsCorrect = true },
                                new Option { Text = "by", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильное местоимение: ___ are my friends.",
                            CorrectAnswer = "They",
                            Options = new List<Option>
                            {
                                new Option { Text = "She", IsCorrect = false },
                                new Option { Text = "He", IsCorrect = false },
                                new Option { Text = "It", IsCorrect = false },
                                new Option { Text = "They", IsCorrect = true }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильную форму множественного числа: one child, two ___.",
                            CorrectAnswer = "children",
                            Options = new List<Option>
                            {
                                new Option { Text = "childs", IsCorrect = false },
                                new Option { Text = "childes", IsCorrect = false },
                                new Option { Text = "children", IsCorrect = true },
                                new Option { Text = "child", IsCorrect = false }
                            }
                        });
                    }
                }
                else if (level == "A2")
                {
                    if (testNumber == 1) // Словарный запас
                    {
                        questions.Add(new Question
                        {
                            Text = "Какое слово относится к погоде?",
                            CorrectAnswer = "Thunderstorm",
                            Options = new List<Option>
                            {
                                new Option { Text = "Kitchen", IsCorrect = false },
                                new Option { Text = "Thunderstorm", IsCorrect = true },
                                new Option { Text = "Telephone", IsCorrect = false },
                                new Option { Text = "Magazine", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный перевод: 'Путешествовать'",
                            CorrectAnswer = "Travel",
                            Options = new List<Option>
                            {
                                new Option { Text = "Train", IsCorrect = false },
                                new Option { Text = "Track", IsCorrect = false },
                                new Option { Text = "Travel", IsCorrect = true },
                                new Option { Text = "Trip", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Как называется место, где люди могут смотреть фильмы?",
                            CorrectAnswer = "Cinema",
                            Options = new List<Option>
                            {
                                new Option { Text = "Library", IsCorrect = false },
                                new Option { Text = "Museum", IsCorrect = false },
                                new Option { Text = "Cinema", IsCorrect = true },
                                new Option { Text = "Hospital", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите слово, относящееся к теме 'Еда'",
                            CorrectAnswer = "Breakfast",
                            Options = new List<Option>
                            {
                                new Option { Text = "Breakfast", IsCorrect = true },
                                new Option { Text = "Telephone", IsCorrect = false },
                                new Option { Text = "Bicycle", IsCorrect = false },
                                new Option { Text = "Postcard", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Какое из этих слов обозначает профессию?",
                            CorrectAnswer = "Teacher",
                            Options = new List<Option>
                            {
                                new Option { Text = "Market", IsCorrect = false },
                                new Option { Text = "Summer", IsCorrect = false },
                                new Option { Text = "Computer", IsCorrect = false },
                                new Option { Text = "Teacher", IsCorrect = true }
                            }
                        });
                    }
                    else // Грамматика
                    {
                        questions.Add(new Question
                        {
                            Text = "Выберите правильную форму прошедшего времени: I ___ a movie yesterday.",
                            CorrectAnswer = "watched",
                            Options = new List<Option>
                            {
                                new Option { Text = "watch", IsCorrect = false },
                                new Option { Text = "watching", IsCorrect = false },
                                new Option { Text = "watches", IsCorrect = false },
                                new Option { Text = "watched", IsCorrect = true }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильную форму сравнения: This building is ___ than my house.",
                            CorrectAnswer = "taller",
                            Options = new List<Option>
                            {
                                new Option { Text = "tall", IsCorrect = false },
                                new Option { Text = "tallest", IsCorrect = false },
                                new Option { Text = "taller", IsCorrect = true },
                                new Option { Text = "more tall", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный модальный глагол: You ___ eat healthy food.",
                            CorrectAnswer = "should",
                            Options = new List<Option>
                            {
                                new Option { Text = "should", IsCorrect = true },
                                new Option { Text = "must to", IsCorrect = false },
                                new Option { Text = "can to", IsCorrect = false },
                                new Option { Text = "would to", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильную форму будущего времени: I ___ to the party tomorrow.",
                            CorrectAnswer = "will go",
                            Options = new List<Option>
                            {
                                new Option { Text = "go", IsCorrect = false },
                                new Option { Text = "goes", IsCorrect = false },
                                new Option { Text = "went", IsCorrect = false },
                                new Option { Text = "will go", IsCorrect = true }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильное продолженное время: She ___ a book now.",
                            CorrectAnswer = "is reading",
                            Options = new List<Option>
                            {
                                new Option { Text = "reads", IsCorrect = false },
                                new Option { Text = "read", IsCorrect = false },
                                new Option { Text = "is reading", IsCorrect = true },
                                new Option { Text = "reading", IsCorrect = false }
                            }
                        });
                    }
                }
                else if (level == "B1") 
                {
                    if (testNumber == 1) // Словарный запас
                    {
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный синоним к слову 'huge'",
                            CorrectAnswer = "enormous",
                            Options = new List<Option>
                            {
                                new Option { Text = "tiny", IsCorrect = false },
                                new Option { Text = "common", IsCorrect = false },
                                new Option { Text = "enormous", IsCorrect = true },
                                new Option { Text = "interesting", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Какое из этих слов означает 'обещание'?",
                            CorrectAnswer = "promise",
                            Options = new List<Option>
                            {
                                new Option { Text = "surprise", IsCorrect = false },
                                new Option { Text = "promise", IsCorrect = true },
                                new Option { Text = "propose", IsCorrect = false },
                                new Option { Text = "predict", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите фразовый глагол со значением 'сдаваться'",
                            CorrectAnswer = "give up",
                            Options = new List<Option>
                            {
                                new Option { Text = "take off", IsCorrect = false },
                                new Option { Text = "give up", IsCorrect = true },
                                new Option { Text = "look after", IsCorrect = false },
                                new Option { Text = "turn down", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите слово, которое относится к теме 'Окружающая среда'",
                            CorrectAnswer = "pollution",
                            Options = new List<Option>
                            {
                                new Option { Text = "education", IsCorrect = false },
                                new Option { Text = "pollution", IsCorrect = true },
                                new Option { Text = "hospitality", IsCorrect = false },
                                new Option { Text = "entertainment", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Какое значение имеет идиома 'to hit the books'?",
                            CorrectAnswer = "начать усердно учиться",
                            Options = new List<Option>
                            {
                                new Option { Text = "ударить книги", IsCorrect = false },
                                new Option { Text = "начать усердно учиться", IsCorrect = true },
                                new Option { Text = "покупать много книг", IsCorrect = false },
                                new Option { Text = "посетить библиотеку", IsCorrect = false }
                            }
                        });
                    }
                    else // Грамматика
                    {
                        questions.Add(new Question
                        {
                            Text = "Выберите правильную форму условного предложения: If I had more time, I ___ a new language.",
                            CorrectAnswer = "would learn",
                            Options = new List<Option>
                            {
                                new Option { Text = "will learn", IsCorrect = false },
                                new Option { Text = "would learning", IsCorrect = false },
                                new Option { Text = "would learn", IsCorrect = true },
                                new Option { Text = "will be learning", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильную форму страдательного залога: The letter ___ yesterday.",
                            CorrectAnswer = "was sent",
                            Options = new List<Option>
                            {
                                new Option { Text = "was send", IsCorrect = false },
                                new Option { Text = "was sent", IsCorrect = true },
                                new Option { Text = "is sent", IsCorrect = false },
                                new Option { Text = "sent", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный вариант косвенной речи: He said, 'I am tired.' → He said that ___.",
                            CorrectAnswer = "he was tired",
                            Options = new List<Option>
                            {
                                new Option { Text = "he is tired", IsCorrect = false },
                                new Option { Text = "he has tired", IsCorrect = false },
                                new Option { Text = "he was tired", IsCorrect = true },
                                new Option { Text = "he had tired", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный модальный глагол: You ___ be joking! That's incredible!",
                            CorrectAnswer = "must",
                            Options = new List<Option>
                            {
                                new Option { Text = "should", IsCorrect = false },
                                new Option { Text = "can", IsCorrect = false },
                                new Option { Text = "must", IsCorrect = true },
                                new Option { Text = "would", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильную форму времени: I ___ here since 2010.",
                            CorrectAnswer = "have been working",
                            Options = new List<Option>
                            {
                                new Option { Text = "work", IsCorrect = false },
                                new Option { Text = "worked", IsCorrect = false },
                                new Option { Text = "have worked", IsCorrect = false },
                                new Option { Text = "have been working", IsCorrect = true }
                            }
                        });
                    }
                }
            }
            else if (language == "Русский")
            {
                if (level == "A1")
                {
                    if (testNumber == 1) // Словарный запас
                    {
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный перевод: 'Hello'",
                            CorrectAnswer = "Привет",
                            Options = new List<Option>
                            {
                                new Option { Text = "Спасибо", IsCorrect = false },
                                new Option { Text = "Пожалуйста", IsCorrect = false },
                                new Option { Text = "Привет", IsCorrect = true },
                                new Option { Text = "До свидания", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Как по-русски 'family'?",
                            CorrectAnswer = "Семья",
                            Options = new List<Option>
                            {
                                new Option { Text = "Друг", IsCorrect = false },
                                new Option { Text = "Дом", IsCorrect = false },
                                new Option { Text = "Школа", IsCorrect = false },
                                new Option { Text = "Семья", IsCorrect = true }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Какое слово обозначает день недели?",
                            CorrectAnswer = "Понедельник",
                            Options = new List<Option>
                            {
                                new Option { Text = "Понедельник", IsCorrect = true },
                                new Option { Text = "Январь", IsCorrect = false },
                                new Option { Text = "Утро", IsCorrect = false },
                                new Option { Text = "Весна", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите слово, обозначающее цвет",
                            CorrectAnswer = "Красный",
                            Options = new List<Option>
                            {
                                new Option { Text = "Стол", IsCorrect = false },
                                new Option { Text = "Красный", IsCorrect = true },
                                new Option { Text = "Высокий", IsCorrect = false },
                                new Option { Text = "Быстро", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Как по-русски сказать 'thank you'?",
                            CorrectAnswer = "Спасибо",
                            Options = new List<Option>
                            {
                                new Option { Text = "Привет", IsCorrect = false },
                                new Option { Text = "Пожалуйста", IsCorrect = false },
                                new Option { Text = "Спасибо", IsCorrect = true },
                                new Option { Text = "Извините", IsCorrect = false }
                            }
                        });
                    }
                    else // Грамматика
                    {
                        questions.Add(new Question
                        {
                            Text = "Выберите правильную форму множественного числа: стол - ...",
                            CorrectAnswer = "столы",
                            Options = new List<Option>
                            {
                                new Option { Text = "столы", IsCorrect = true },
                                new Option { Text = "стола", IsCorrect = false },
                                new Option { Text = "столов", IsCorrect = false },
                                new Option { Text = "столах", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильное окончание: Я говор... по-русски.",
                            CorrectAnswer = "ю",
                            Options = new List<Option>
                            {
                                new Option { Text = "ю", IsCorrect = true },
                                new Option { Text = "у", IsCorrect = false },
                                new Option { Text = "ит", IsCorrect = false },
                                new Option { Text = "им", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильное личное местоимение: ... идём в кино.",
                            CorrectAnswer = "Мы",
                            Options = new List<Option>
                            {
                                new Option { Text = "Я", IsCorrect = false },
                                new Option { Text = "Ты", IsCorrect = false },
                                new Option { Text = "Он", IsCorrect = false },
                                new Option { Text = "Мы", IsCorrect = true }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильную форму глагола: Она ... музыку.",
                            CorrectAnswer = "слушает",
                            Options = new List<Option>
                            {
                                new Option { Text = "слушаю", IsCorrect = false },
                                new Option { Text = "слушаешь", IsCorrect = false },
                                new Option { Text = "слушает", IsCorrect = true },
                                new Option { Text = "слушают", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный предлог: Книга лежит ... столе.",
                            CorrectAnswer = "на",
                            Options = new List<Option>
                            {
                                new Option { Text = "в", IsCorrect = false },
                                new Option { Text = "на", IsCorrect = true },
                                new Option { Text = "под", IsCorrect = false },
                                new Option { Text = "у", IsCorrect = false }
                            }
                        });
                    }
                }
            }
            else if (language == "Казахский")
            {
                if (level == "A1")
                {
                    if (testNumber == 1) // Словарный запас
                    {
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный перевод: 'Привет'",
                            CorrectAnswer = "Сәлем",
                            Options = new List<Option>
                            {
                                new Option { Text = "Рахмет", IsCorrect = false },
                                new Option { Text = "Кеш жарық", IsCorrect = false },
                                new Option { Text = "Сәлем", IsCorrect = true },
                                new Option { Text = "Сау болыңыз", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Как называется числительное 'один' на казахском?",
                            CorrectAnswer = "бір",
                            Options = new List<Option>
                            {
                                new Option { Text = "бір", IsCorrect = true },
                                new Option { Text = "екі", IsCorrect = false },
                                new Option { Text = "үш", IsCorrect = false },
                                new Option { Text = "төрт", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите название дня недели на казахском",
                            CorrectAnswer = "жексенбі",
                            Options = new List<Option>
                            {
                                new Option { Text = "жексенбі", IsCorrect = true },
                                new Option { Text = "маусым", IsCorrect = false },
                                new Option { Text = "таңертең", IsCorrect = false },
                                new Option { Text = "көктем", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Как будет 'школа' на казахском?",
                            CorrectAnswer = "мектеп",
                            Options = new List<Option>
                            {
                                new Option { Text = "үй", IsCorrect = false },
                                new Option { Text = "дәптер", IsCorrect = false },
                                new Option { Text = "мектеп", IsCorrect = true },
                                new Option { Text = "кітап", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильное приветствие на казахском языке",
                            CorrectAnswer = "Қайырлы таң",
                            Options = new List<Option>
                            {
                                new Option { Text = "Сау болыңыз", IsCorrect = false },
                                new Option { Text = "Қайырлы таң", IsCorrect = true },
                                new Option { Text = "Рахмет", IsCorrect = false },
                                new Option { Text = "Кешіріңіз", IsCorrect = false }
                            }
                        });
                    }
                    else // Грамматика
                    {
                        questions.Add(new Question
                        {
                            Text = "Выберите правильное окончание множественного числа: кітап...",
                            CorrectAnswer = "тар",
                            Options = new List<Option>
                            {
                                new Option { Text = "тар", IsCorrect = true },
                                new Option { Text = "дар", IsCorrect = false },
                                new Option { Text = "дер", IsCorrect = false },
                                new Option { Text = "лар", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильное личное местоимение: ... мектепке барамын.",
                            CorrectAnswer = "Мен",
                            Options = new List<Option>
                            {
                                new Option { Text = "Мен", IsCorrect = true },
                                new Option { Text = "Сен", IsCorrect = false },
                                new Option { Text = "Ол", IsCorrect = false },
                                new Option { Text = "Біз", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный суффикс принадлежности: менің кітаб...",
                            CorrectAnswer = "ым",
                            Options = new List<Option>
                            {
                                new Option { Text = "ым", IsCorrect = true },
                                new Option { Text = "ың", IsCorrect = false },
                                new Option { Text = "ы", IsCorrect = false },
                                new Option { Text = "ымыз", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильную форму глагола: Олар кітап ...",
                            CorrectAnswer = "оқиды",
                            Options = new List<Option>
                            {
                                new Option { Text = "оқимын", IsCorrect = false },
                                new Option { Text = "оқисың", IsCorrect = false },
                                new Option { Text = "оқиды", IsCorrect = true },
                                new Option { Text = "оқимыз", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный послелог: үстел ... кітап",
                            CorrectAnswer = "үстінде",
                            Options = new List<Option>
                            {
                                new Option { Text = "астында", IsCorrect = false },
                                new Option { Text = "үстінде", IsCorrect = true },
                                new Option { Text = "жанында", IsCorrect = false },
                                new Option { Text = "алдында", IsCorrect = false }
                            }
                        });
                    }
                }
            }
            else if (language == "Турецкий")
            {
                if (level == "A1")
                {
                    if (testNumber == 1) // Словарный запас
                    {
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный перевод: 'Здравствуйте'",
                            CorrectAnswer = "Merhaba",
                            Options = new List<Option>
                            {
                                new Option { Text = "Merhaba", IsCorrect = true },
                                new Option { Text = "Teşekkür ederim", IsCorrect = false },
                                new Option { Text = "Güle güle", IsCorrect = false },
                                new Option { Text = "Lütfen", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Как будет 'дом' на турецком?",
                            CorrectAnswer = "Ev",
                            Options = new List<Option>
                            {
                                new Option { Text = "Okul", IsCorrect = false },
                                new Option { Text = "Araba", IsCorrect = false },
                                new Option { Text = "Ev", IsCorrect = true },
                                new Option { Text = "Kitap", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильный перевод числительного 'пять'",
                            CorrectAnswer = "Beş",
                            Options = new List<Option>
                            {
                                new Option { Text = "Bir", IsCorrect = false },
                                new Option { Text = "Üç", IsCorrect = false },
                                new Option { Text = "Beş", IsCorrect = true },
                                new Option { Text = "On", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Как на турецком будет 'понедельник'?",
                            CorrectAnswer = "Pazartesi",
                            Options = new List<Option>
                            {
                                new Option { Text = "Pazartesi", IsCorrect = true },
                                new Option { Text = "Salı", IsCorrect = false },
                                new Option { Text = "Cuma", IsCorrect = false },
                                new Option { Text = "Pazar", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите слово, обозначающее цвет",
                            CorrectAnswer = "Kırmızı",
                            Options = new List<Option>
                            {
                                new Option { Text = "Kırmızı", IsCorrect = true },
                                new Option { Text = "Masa", IsCorrect = false },
                                new Option { Text = "Kalem", IsCorrect = false },
                                new Option { Text = "Güzel", IsCorrect = false }
                            }
                        });
                    }
                    else // Грамматика
                    {
                        questions.Add(new Question
                        {
                            Text = "Выберите правильное окончание множественного числа: kitap...",
                            CorrectAnswer = "lar",
                            Options = new List<Option>
                            {
                                new Option { Text = "lar", IsCorrect = true },
                                new Option { Text = "ler", IsCorrect = false },
                                new Option { Text = "lir", IsCorrect = false },
                                new Option { Text = "da", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильное личное местоимение: ... okula gidiyorum.",
                            CorrectAnswer = "Ben",
                            Options = new List<Option>
                            {
                                new Option { Text = "Ben", IsCorrect = true },
                                new Option { Text = "Sen", IsCorrect = false },
                                new Option { Text = "O", IsCorrect = false },
                                new Option { Text = "Biz", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильное окончание глагола: Ben yemek ye...",
                            CorrectAnswer = "rim",
                            Options = new List<Option>
                            {
                                new Option { Text = "rim", IsCorrect = true },
                                new Option { Text = "rsin", IsCorrect = false },
                                new Option { Text = "r", IsCorrect = false },
                                new Option { Text = "riz", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Какой суффикс используется для местного падежа в турецком? Ev...",
                            CorrectAnswer = "de",
                            Options = new List<Option>
                            {
                                new Option { Text = "de", IsCorrect = true },
                                new Option { Text = "i", IsCorrect = false },
                                new Option { Text = "e", IsCorrect = false },
                                new Option { Text = "den", IsCorrect = false }
                            }
                        });
                        
                        questions.Add(new Question
                        {
                            Text = "Выберите правильное отрицание глагола: Ben okula git...",
                            CorrectAnswer = "miyorum",
                            Options = new List<Option>
                            {
                                new Option { Text = "miyorum", IsCorrect = true },
                                new Option { Text = "memem", IsCorrect = false },
                                new Option { Text = "mezim", IsCorrect = false },
                                new Option { Text = "merim", IsCorrect = false }
                            }
                        });
                    }
                }
            }
            
            // Если вопросов меньше 5, добавляем стандартные вопросы для заполнения
            while (questions.Count < 5)
            {
                questions.Add(new Question
                {
                    Text = $"Дополнительный вопрос для теста по {language.ToLower()} языку уровня {level}",
                    CorrectAnswer = "Правильный ответ",
                    Options = new List<Option>
                    {
                        new Option { Text = "Правильный ответ", IsCorrect = true },
                        new Option { Text = "Неправильный ответ 1", IsCorrect = false },
                        new Option { Text = "Неправильный ответ 2", IsCorrect = false },
                        new Option { Text = "Неправильный ответ 3", IsCorrect = false }
                    }
                });
            }
            
            return questions;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Results()
        {
            var userId = _userManager.GetUserId(User);
            var testResults = await _context.TestResults
                .Include(tr => tr.Test)
                        .ThenInclude(t => t.LanguageLevel)
                            .ThenInclude(ll => ll.Language)
                .Where(tr => tr.UserId == userId)
                .OrderByDescending(tr => tr.CompletedDate)
                    .ToListAsync();
                
            return View(testResults);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Statistics()
        {
            var userId = _userManager.GetUserId(User);
            var allTestResults = await _context.TestResults
                .Include(tr => tr.Test)
                    .ThenInclude(t => t.LanguageLevel)
                        .ThenInclude(ll => ll.Language)
                .Where(tr => tr.UserId == userId)
                .OrderByDescending(tr => tr.CompletedDate)
                .ToListAsync();

            // Группируем результаты по языкам для статистики
            var statisticsByLanguage = allTestResults
                .GroupBy(tr => tr.Language)
                .Select(group => new LanguageStatisticsViewModel
                {
                    LanguageName = group.Key,
                    LanguageFlag = GetLanguageFlag(group.Key),
                    TotalTests = group.Count(),
                    PassedTests = group.Count(tr => tr.IsPassed),
                    AverageScore = group.Average(tr => tr.Percentage),
                    HighestScore = group.Max(tr => tr.Score),
                    LastTestDate = group.Max(tr => tr.CompletedDate),
                    ResultsByLevel = group
                        .GroupBy(tr => tr.Level)
                        .Select(levelGroup => new LevelStatisticsViewModel
                        {
                            LevelName = levelGroup.Key,
                            TotalTests = levelGroup.Count(),
                            PassedTests = levelGroup.Count(tr => tr.IsPassed),
                            AverageScore = levelGroup.Average(tr => tr.Percentage)
                        })
                        .OrderBy(l => l.LevelName)
                        .ToList()
                })
                .ToList();

            // Статистика по времени
            var timeStatistics = new TimeStatisticsViewModel
            {
                AverageDuration = allTestResults.Any() ? allTestResults.Average(tr => tr.Duration.TotalMinutes) : 0,
                TotalTimeSpent = allTestResults.Sum(tr => tr.Duration.TotalMinutes),
                FastestTest = allTestResults.Any() ? allTestResults.Min(tr => tr.Duration.TotalMinutes) : 0,
                SlowestTest = allTestResults.Any() ? allTestResults.Max(tr => tr.Duration.TotalMinutes) : 0
            };

            // Общая статистика
            var overallStatistics = new OverallStatisticsViewModel
            {
                TotalTests = allTestResults.Count,
                PassedTests = allTestResults.Count(tr => tr.IsPassed),
                FailedTests = allTestResults.Count(tr => !tr.IsPassed),
                AverageScore = allTestResults.Any() ? allTestResults.Average(tr => tr.Percentage) : 0,
                TotalLanguages = statisticsByLanguage.Count,
                MostSuccessfulLanguage = statisticsByLanguage.Any() 
                    ? statisticsByLanguage.OrderByDescending(s => s.AverageScore).First().LanguageName 
                    : "Нет данных",
                MostTakenLevel = allTestResults
                    .GroupBy(tr => tr.Level)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault() ?? "Нет данных"
            };

            // Прогресс по времени (месяцам)
            var progressByMonth = allTestResults
                .GroupBy(tr => new { tr.CompletedDate.Year, tr.CompletedDate.Month })
                .Select(group => new MonthlyProgressViewModel
                {
                    Month = new DateTime(group.Key.Year, group.Key.Month, 1, 0, 0, 0, DateTimeKind.Unspecified),
                    TestsCount = group.Count(),
                    AverageScore = group.Average(tr => tr.Percentage),
                    PassRate = (double)group.Count(tr => tr.IsPassed) / group.Count() * 100
                })
                .OrderBy(p => p.Month)
                .ToList();

            var viewModel = new StatisticsViewModel
            {
                LanguageStatistics = statisticsByLanguage,
                TimeStatistics = timeStatistics,
                OverallStatistics = overallStatistics,
                MonthlyProgress = progressByMonth,
                RecentTests = allTestResults.Take(5).ToList()
            };

            return View(viewModel);
        }

        private string GetLanguageFlag(string languageName)
        {
            return languageName switch
            {
                "English" => "🇬🇧",
                "Kazakh" => "🇰🇿",
                "Turkish" => "🇹🇷",
                "Russian" => "🇷🇺",
                _ => "🌎"
            };
        }
    }

    public class TestViewModel
    {
        public int TestId { get; set; }
        public string TestTitle { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public int TimeLimit { get; set; }
        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();
    }

    public class QuestionViewModel
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<OptionViewModel> Options { get; set; } = new List<OptionViewModel>();
    }

    public class OptionViewModel
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    public class TestSubmission
    {
        public int TestId { get; set; }
        public Dictionary<int, string> Answers { get; set; } = new Dictionary<int, string>();
    }

    // Модель для передачи результатов теста
    public class TestResultModel
    {
        public int TestId { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public double Percentage { get; set; }
        public string Language { get; set; }
        public string Level { get; set; }
        public string Title { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
    }
} 