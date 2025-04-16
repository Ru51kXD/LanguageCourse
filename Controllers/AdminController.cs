using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication15.Data;
using WebApplication15.Models;
using WebApplication15.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace WebApplication15.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ThemeService _themeService;
        private readonly LanguageService _languageService;
        private readonly VideoService _videoService;
        private readonly TestService _testService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            ThemeService themeService,
            LanguageService languageService,
            VideoService videoService,
            TestService testService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _themeService = themeService;
            _languageService = languageService;
            _videoService = videoService;
            _testService = testService;
            _logger = logger;
        }

        // GET: /Admin/Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.Theme = _themeService.GetCurrentTheme();
            return View();
        }

        // POST: /Admin/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.Theme = _themeService.GetCurrentTheme();

            if (ModelState.IsValid)
            {
                // Находим пользователя
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user != null)
                {
                    // Проверяем пароль
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        // Проверяем, является ли пользователь администратором
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            return RedirectToAction("Dashboard", "Admin");
                        }
                        else
                        {
                            // Пользователь успешно вошел, но не является администратором
                            await _signInManager.SignOutAsync();
                            ModelState.AddModelError(string.Empty, "У вас нет прав администратора.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Неверный пароль.");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Пользователь не найден.");
                }
            }

            return View(model);
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            ViewBag.UserCount = await _context.Users.CountAsync();
            ViewBag.TestCount = await _context.Tests.CountAsync();
            ViewBag.VideoCount = await _context.Videos.CountAsync();
            ViewBag.TestResultCount = await _context.TestResults.CountAsync();

            return View();
        }

        // GET: /Admin/Users
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await GetUsersWithRoles();
            return View(users);
        }

        // GET: /Admin/UserDetails/5
        public async Task<IActionResult> UserDetails(string id)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Загружаем результаты тестов для пользователя
            var testResults = await _context.TestResults
                .Include(tr => tr.Test)
                    .ThenInclude(t => t.LanguageLevel)
                        .ThenInclude(ll => ll.Language)
                .Where(tr => tr.UserId == id)
                .OrderByDescending(tr => tr.CompletedDate)
                .Take(10)
                .ToListAsync();

            // Загружаем просмотренные видео
            var watchedVideos = await _context.WatchedVideos
                .Include(wv => wv.Video)
                    .ThenInclude(v => v.LanguageLevel)
                        .ThenInclude(ll => ll.Language)
                .Where(wv => wv.UserId == id)
                .OrderByDescending(wv => wv.WatchedDate)
                .Take(10)
                .ToListAsync();

            var userViewModel = new UserDetailsViewModel
            {
                User = user,
                TestResults = testResults,
                WatchedVideos = watchedVideos,
                UserRoles = await _userManager.GetRolesAsync(user)
            };

            ViewBag.AvailableRoles = _roleManager.Roles.ToList();

            return View(userViewModel);
        }

        // POST: /Admin/ToggleUserRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserRole(string userId, string roleName)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleName))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                // Если пользователь является админом и это последний админ, запретим удаление
                if (roleName == "Admin")
                {
                    var admins = await _userManager.GetUsersInRoleAsync("Admin");
                    if (admins.Count <= 1)
                    {
                        return BadRequest("Нельзя удалить последнего администратора.");
                    }
                }
                await _userManager.RemoveFromRoleAsync(user, roleName);
            }
            else
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }

            return RedirectToAction("UserDetails", new { id = userId });
        }

        // POST: /Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                return RedirectToAction(nameof(Users));

            return BadRequest(result.Errors);
        }

        // GET: /Admin/Tests
        public async Task<IActionResult> Tests()
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            var tests = await _context.Tests
                .Include(t => t.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Options)
                .OrderBy(t => t.LanguageLevel.Language.Name)
                .ThenBy(t => t.LanguageLevel.Name)
                .ToListAsync();

            return View(tests);
        }

        // GET: /Admin/TestDetails/5
        public async Task<IActionResult> TestDetails(int id)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            var test = await _context.Tests
                .Include(t => t.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null)
            {
                return NotFound();
            }

            // Получаем результаты по тесту
            var results = await _context.TestResults
                .Include(tr => tr.User)
                .Where(tr => tr.TestId == id)
                .OrderByDescending(tr => tr.CompletedDate)
                .Take(10)
                .ToListAsync();

            ViewBag.TestResults = results;
            ViewBag.PassRate = test.TestResults.Any() 
                ? (double)test.TestResults.Count(tr => tr.IsPassed) / test.TestResults.Count * 100 
                : 0;

            return View(test);
        }

        // GET: /Admin/CreateTest
        public IActionResult CreateTest()
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var languages = _context.Languages
                .Include(l => l.LanguageLevels)
                .ToList();
                
            ViewBag.Languages = languages;
            
            return View();
        }

        // GET: /Admin/CreateTestWithQuestion
        public IActionResult CreateTestWithQuestion()
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var languages = _context.Languages
                .Include(l => l.LanguageLevels)
                .ToList();
                
            ViewBag.Languages = languages;
            
            return View("CreateTest"); // Используем то же представление, что и для создания теста
        }

        // POST: /Admin/CreateTestWithQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTestWithQuestion(string Title, string Description, int LanguageId, int LanguageLevelId, int TimeLimit, int PassingScore, string IsActive)
        {
            _logger.LogInformation($"CreateTestWithQuestion: Title={Title}, LanguageId={LanguageId}, LanguageLevelId={LanguageLevelId}, IsActive={IsActive}");
            
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var languages = _context.Languages
                .Include(l => l.LanguageLevels)
                .ToList();
                
            ViewBag.Languages = languages;
            
            try {
                // Проверяем, что все необходимые поля заполнены
                if (string.IsNullOrEmpty(Title))
                {
                    ModelState.AddModelError("Title", "Название теста обязательно");
                }
                
                if (LanguageId == 0)
                {
                    ModelState.AddModelError("LanguageId", "Выберите язык");
                }
                
                if (LanguageLevelId == 0)
                {
                    ModelState.AddModelError("LanguageLevelId", "Выберите уровень языка");
                }
                
                if (TimeLimit <= 0)
                {
                    ModelState.AddModelError("TimeLimit", "Ограничение времени должно быть положительным числом");
                }
                
                if (PassingScore <= 0 || PassingScore > 100)
                {
                    ModelState.AddModelError("PassingScore", "Проходной балл должен быть от 1 до 100");
                }
                
                if (!ModelState.IsValid)
                {
                    ViewBag.ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage))
                    );
                    
                    ViewBag.ReceivedData = new
                    {
                        Title,
                        Description,
                        LanguageId,
                        LanguageLevelId,
                        TimeLimit,
                        PassingScore,
                        IsActive
                    };
                    
                    return View("CreateTest");
                }
                
                // Проверяем, что выбранный уровень принадлежит выбранному языку
                var level = await _context.LanguageLevels
                    .FirstOrDefaultAsync(ll => ll.Id == LanguageLevelId && ll.LanguageId == LanguageId);
                    
                if (level == null)
                {
                    ModelState.AddModelError("LanguageLevelId", "Выбранный уровень не принадлежит выбранному языку");
                    
                    ViewBag.ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage))
                    );
                    
                    ViewBag.ReceivedData = new
                    {
                        Title,
                        Description,
                        LanguageId,
                        LanguageLevelId,
                        TimeLimit,
                        PassingScore,
                        IsActive
                    };
                    
                    return View("CreateTest");
                }
                
                // Преобразуем значение IsActive из строки в bool
                bool isActiveValue = !string.IsNullOrEmpty(IsActive);
                
                // Создаем тест
                var test = new Test
                {
                    Title = Title,
                    Description = Description ?? "",
                    LanguageLevelId = LanguageLevelId,
                    TimeLimit = TimeLimit,
                    PassingScore = PassingScore,
                    IsActive = isActiveValue,
                    OrderInLevel = 1,
                    CreatedDate = DateTime.Now,
                    // Инициализируем коллекции
                    Questions = new List<Question>(),
                    TestResults = new List<TestResult>()
                };
                
                _context.Tests.Add(test);
                await _context.SaveChangesAsync();
                
                // Перенаправляем на страницу добавления вопроса
                return RedirectToAction("AddQuestion", new { testId = test.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании теста: {Message}", ex.Message);
                // Логируем внутреннее исключение, если оно есть
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Внутреннее исключение: {Message}", ex.InnerException.Message);
                }
                ModelState.AddModelError("", $"Произошла ошибка при создании теста: {ex.Message}");
                
                ViewBag.ValidationErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage))
                );
                
                return View("CreateTest");
            }
        }

        // POST: /Admin/CreateTest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTest(Test test, int LanguageId)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var languages = _context.Languages
                .Include(l => l.LanguageLevels)
                .ToList();
                
            ViewBag.Languages = languages;

            try
            {
                // Проверяем правильность данных
                if (ModelState.IsValid)
                {
                    // Проверяем, что выбранный уровень принадлежит выбранному языку
                    var level = await _context.LanguageLevels
                        .FirstOrDefaultAsync(ll => ll.Id == test.LanguageLevelId && ll.LanguageId == LanguageId);
                        
                    if (level == null)
                    {
                        ModelState.AddModelError("LanguageLevelId", "Выбранный уровень не принадлежит выбранному языку");
                        return View(test);
                    }
                    
                    // Используем транзакцию для обеспечения целостности данных
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            // Устанавливаем значения по умолчанию
                            test.CreatedDate = DateTime.Now;
                            if (string.IsNullOrEmpty(test.Description))
                            {
                                test.Description = "";
                            }
                            
                            // Сохраняем тест без коллекций сначала
                            var testEntity = new Test
                            {
                                Title = test.Title,
                                Description = test.Description,
                                LanguageLevelId = test.LanguageLevelId,
                                TimeLimit = test.TimeLimit,
                                PassingScore = test.PassingScore,
                                IsActive = test.IsActive,
                                OrderInLevel = test.OrderInLevel,
                                CreatedDate = test.CreatedDate
                            };
                            
                            _context.Tests.Add(testEntity);
                            await _context.SaveChangesAsync();
                            
                            // Подтверждаем транзакцию
                            await transaction.CommitAsync();
                            
                            // Перенаправляем на страницу добавления вопросов
                            return RedirectToAction("AddQuestion", new { testId = testEntity.Id });
                        }
                        catch (Exception ex)
                        {
                            // Откатываем транзакцию в случае ошибки
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании теста: {Message}", ex.Message);
                // Логируем внутреннее исключение, если оно есть
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Внутреннее исключение: {Message}", ex.InnerException.Message);
                }
                ModelState.AddModelError("", $"Произошла ошибка при сохранении теста: {ex.Message}");
            }
            
            return View(test);
        }

        // GET: /Admin/EditTest/5
        public async Task<IActionResult> EditTest(int id)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var languages = _context.Languages
                .Include(l => l.LanguageLevels)
                .ToList();
                
            ViewBag.Languages = languages;

            var test = await _context.Tests
                .Include(t => t.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .FirstOrDefaultAsync(t => t.Id == id);
                
            if (test == null)
            {
                return NotFound();
            }

            return View(test);
        }

        // POST: /Admin/EditTest/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTest(int id, string Title, string Description, 
            int LanguageId, int LanguageLevelId, int TimeLimit, int PassingScore, string IsActive, int OrderInLevel = 1)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var languages = _context.Languages
                .Include(l => l.LanguageLevels)
                .ToList();
                
            ViewBag.Languages = languages;

            try
            {
                // Получаем существующий тест
                var test = await _context.Tests.FindAsync(id);
                if (test == null)
            {
                return NotFound();
            }

                // Проверяем, что все необходимые поля заполнены
                if (string.IsNullOrEmpty(Title))
                {
                    ModelState.AddModelError("Title", "Название теста обязательно");
                }
                
                if (LanguageId == 0)
                {
                    ModelState.AddModelError("LanguageId", "Выберите язык");
                }
                
                if (LanguageLevelId == 0)
                {
                    ModelState.AddModelError("LanguageLevelId", "Выберите уровень языка");
                }
                
                if (TimeLimit <= 0)
                {
                    ModelState.AddModelError("TimeLimit", "Ограничение времени должно быть положительным числом");
                }
                
                if (PassingScore <= 0 || PassingScore > 100)
                {
                    ModelState.AddModelError("PassingScore", "Проходной балл должен быть от 1 до 100");
                }
                
                if (!ModelState.IsValid)
                {
                    ViewBag.ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage))
                    );
                    
                    return View(test);
                }
                
                // Проверяем, что выбранный уровень принадлежит выбранному языку
                var level = await _context.LanguageLevels
                    .FirstOrDefaultAsync(ll => ll.Id == LanguageLevelId && ll.LanguageId == LanguageId);
                    
                if (level == null)
                {
                    ModelState.AddModelError("LanguageLevelId", "Выбранный уровень не принадлежит выбранному языку");
                    
                    ViewBag.ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage))
                    );
                    
                    return View(test);
                }
            
                // Преобразуем значение IsActive из строки в bool
                bool isActiveValue = !string.IsNullOrEmpty(IsActive);
                
                // Обновляем поля теста
                test.Title = Title;
                test.Description = Description ?? "";
                test.LanguageLevelId = LanguageLevelId;
                test.TimeLimit = TimeLimit;
                test.PassingScore = PassingScore;
                test.IsActive = isActiveValue;
                test.OrderInLevel = OrderInLevel;
                
                    await _context.SaveChangesAsync();
                
                // Устанавливаем сообщение об успехе
                TempData["SuccessMessage"] = $"Тест \"{test.Title}\" успешно обновлен!";
                
                // Перенаправляем на страницу деталей теста
                return RedirectToAction("TestDetails", new { id = test.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении теста: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Внутреннее исключение: {Message}", ex.InnerException.Message);
                }
                
                var test = await _context.Tests.FindAsync(id);
                if (test != null)
                {
                    ModelState.AddModelError("", $"Произошла ошибка при обновлении теста: {ex.Message}");
                    return View(test);
                }
                
                return NotFound();
            }
        }

        // POST: /Admin/DeleteTest/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTest(int id)
        {
            var test = await _context.Tests
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Options)
                .Include(t => t.TestResults)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null)
            {
                return NotFound();
            }

            // Удаляем связанные данные
            _context.TestResults.RemoveRange(test.TestResults);
            
            foreach (var question in test.Questions)
            {
                _context.Options.RemoveRange(question.Options);
            }
            
            _context.Questions.RemoveRange(test.Questions);
            _context.Tests.Remove(test);
            
            await _context.SaveChangesAsync();
            
            return RedirectToAction("Tests");
        }

        // GET: /Admin/Videos
        public async Task<IActionResult> Videos()
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            try
            {
                var videos = await _context.Videos
                    .Include(v => v.LanguageLevel)
                        .ThenInclude(ll => ll.Language)
                    .OrderBy(v => v.LanguageLevel.Language.Name)
                    .ThenBy(v => v.LanguageLevel.Name)
                    .ToListAsync();

                return View(videos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке списка видео: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Ошибка при загрузке списка видео";
                return View(new List<Video>());
            }
        }

        // GET: /Admin/VideoDetails/5
        public async Task<IActionResult> VideoDetails(int id)
        {
            _logger.LogInformation($"Запрос деталей видео с ID {id}");
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            try
            {
                var video = await _context.Videos
                    .Include(v => v.LanguageLevel)
                        .ThenInclude(ll => ll.Language)
                    .AsNoTracking() // Убедимся, что получаем актуальные данные из базы
                    .FirstOrDefaultAsync(v => v.Id == id);
                    
                if (video == null)
                {
                    _logger.LogWarning($"Видео с ID {id} не найдено при запросе деталей");
                    return NotFound();
                }
                
                _logger.LogInformation($"Видео найдено: ID={video.Id}, Название={video.Title}, " +
                    $"Язык={video.LanguageLevel?.Language?.Name}, Уровень={video.LanguageLevel?.Name}");
                
                // Получаем количество просмотров
                var watchCount = await _context.WatchedVideos
                    .CountAsync(wv => wv.VideoId == id);
                
                _logger.LogInformation($"Количество просмотров видео: {watchCount}");
                
                ViewBag.WatchCount = watchCount;
                
                return View(video);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении деталей видео с ID {id}: {ex.Message}");
                TempData["ErrorMessage"] = "Произошла ошибка при загрузке данных видео.";
                return RedirectToAction("Videos");
            }
        }

        // GET: /Admin/CreateVideo
        public IActionResult CreateVideo()
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var languages = _context.Languages
                .Include(l => l.LanguageLevels)
                .ToList();
                
            ViewBag.Languages = languages;
            
            return View();
        }

        // POST: /Admin/CreateVideo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVideo(string Title, string Description, string VideoUrl, 
            int LanguageId, int LanguageLevelId, string Duration, string IsActive, string IsFeatured)
        {
            _logger.LogInformation($"CreateVideo: Title={Title}, VideoUrl={VideoUrl}, LanguageId={LanguageId}, LanguageLevelId={LanguageLevelId}");
            
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var languages = _context.Languages
                .Include(l => l.LanguageLevels)
                .ToList();
                
            ViewBag.Languages = languages;

            try {
                // Проверяем, что все необходимые поля заполнены
                if (string.IsNullOrEmpty(Title))
                {
                    ModelState.AddModelError("Title", "Название видео обязательно");
                }
                
                if (string.IsNullOrEmpty(VideoUrl))
                {
                    ModelState.AddModelError("VideoUrl", "URL видео обязательно");
                }
                
                if (LanguageId == 0)
                {
                    ModelState.AddModelError("LanguageId", "Выберите язык");
                }
                
                if (LanguageLevelId == 0)
                {
                    ModelState.AddModelError("LanguageLevelId", "Выберите уровень языка");
                }
                
                if (string.IsNullOrEmpty(Duration))
                {
                    ModelState.AddModelError("Duration", "Длительность должна быть указана");
                }
                
                if (!ModelState.IsValid)
                {
                    ViewBag.ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage))
                    );
                    
                    ViewBag.ReceivedData = new
                    {
                        Title,
                        Description,
                        VideoUrl,
                        LanguageId,
                        LanguageLevelId,
                        Duration,
                        IsActive,
                        IsFeatured
                    };
                    
                    return View("CreateVideo");
                }
                
                // Проверяем, что выбранный уровень принадлежит выбранному языку
                var level = await _context.LanguageLevels
                    .FirstOrDefaultAsync(ll => ll.Id == LanguageLevelId && ll.LanguageId == LanguageId);
                    
                if (level == null)
                {
                    ModelState.AddModelError("LanguageLevelId", "Выбранный уровень не принадлежит выбранному языку");
                    
                    ViewBag.ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage))
                    );
                    
                    ViewBag.ReceivedData = new
                    {
                        Title,
                        Description,
                        VideoUrl,
                        LanguageId,
                        LanguageLevelId,
                        Duration,
                        IsActive,
                        IsFeatured
                    };
                    
                    return View("CreateVideo");
                }
                
                // Получаем миниатюру из URL видео YouTube
                string thumbnailUrl = GenerateThumbnailUrlFromYoutube(VideoUrl);
                
                // Преобразуем значения IsActive и IsFeatured из строки в bool
                bool isActiveValue = !string.IsNullOrEmpty(IsActive);
                bool isFeaturedValue = !string.IsNullOrEmpty(IsFeatured);
                
                // Создаем видеоурок
                var video = new Video
                {
                    Title = Title,
                    Description = Description ?? "",
                    VideoUrl = VideoUrl,
                    ThumbnailUrl = thumbnailUrl,
                    LanguageLevelId = LanguageLevelId,
                    Duration = Duration,
                    IsActive = isActiveValue,
                    IsFeatured = isFeaturedValue,
                    CreatedDate = DateTime.Now
                };
                
                _context.Videos.Add(video);
                await _context.SaveChangesAsync();
                            
                            // Устанавливаем сообщение об успехе
                TempData["SuccessMessage"] = $"Видео \"{video.Title}\" успешно создано!";
                            
                            // Перенаправляем на страницу деталей видео
                _logger.LogInformation($"Видео создано успешно, ID={video.Id}");
                return RedirectToAction("VideoDetails", new { id = video.Id });
                        }
                        catch (Exception ex)
                        {
                _logger.LogError(ex, "Ошибка при создании видео: {Message}", ex.Message);
                // Логируем внутреннее исключение, если оно есть
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Внутреннее исключение: {Message}", ex.InnerException.Message);
                }
                ModelState.AddModelError("", $"Произошла ошибка при создании видео: {ex.Message}");
                
                ViewBag.ValidationErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage))
                );
                
                return View("CreateVideo");
            }
        }

        // Вспомогательный метод для получения миниатюры YouTube
        private string GenerateThumbnailUrlFromYoutube(string videoUrl)
        {
            try
            {
                // Поддерживаемые форматы URL YouTube
                // https://www.youtube.com/watch?v=VIDEO_ID
                // https://youtu.be/VIDEO_ID
                // https://www.youtube.com/embed/VIDEO_ID
                
                string videoId = null;
                
                // Пробуем извлечь ID видео из разных форматов URL
                if (videoUrl.Contains("youtube.com/watch"))
                {
                    // Формат https://www.youtube.com/watch?v=VIDEO_ID
                    var uri = new Uri(videoUrl);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    videoId = query["v"];
                }
                else if (videoUrl.Contains("youtu.be/"))
                {
                    // Формат https://youtu.be/VIDEO_ID
                    var uri = new Uri(videoUrl);
                    videoId = uri.AbsolutePath.TrimStart('/');
                }
                else if (videoUrl.Contains("youtube.com/embed/"))
                {
                    // Формат https://www.youtube.com/embed/VIDEO_ID
                    var uri = new Uri(videoUrl);
                    videoId = uri.AbsolutePath.Replace("/embed/", "").TrimStart('/');
                }
                
                if (string.IsNullOrEmpty(videoId))
                {
                    // Если не удалось извлечь ID, возвращаем заглушку
                    return "https://img.youtube.com/vi/default/hqdefault.jpg";
                }
                
                // Возвращаем URL миниатюры стандартного разрешения
                return $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении миниатюры: {Message}", ex.Message);
                // В случае ошибки возвращаем заглушку
                return "https://via.placeholder.com/480x360.png?text=Video+Thumbnail";
            }
        }

        // GET: /Admin/EditVideo/5
        public async Task<IActionResult> EditVideo(int id)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var languages = _context.Languages
                .Include(l => l.LanguageLevels)
                .ToList();
                
            ViewBag.Languages = languages;

            var video = await _context.Videos
                .Include(v => v.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .FirstOrDefaultAsync(v => v.Id == id);
                
            if (video == null)
            {
                return NotFound();
            }

            return View(video);
        }

        // POST: /Admin/EditVideo/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVideo(int id, string Title, string Description, string VideoUrl, 
            int LanguageId, int LanguageLevelId, string Duration, string IsActive, string IsFeatured)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var languages = _context.Languages
                .Include(l => l.LanguageLevels)
                .ToList();
                
            ViewBag.Languages = languages;

            try
            {
                // Получаем существующее видео
                var video = await _context.Videos.FindAsync(id);
                if (video == null)
            {
                return NotFound();
            }
            
                // Проверяем, что все необходимые поля заполнены
                if (string.IsNullOrEmpty(Title))
                {
                    ModelState.AddModelError("Title", "Название видео обязательно");
                }
                
                if (string.IsNullOrEmpty(VideoUrl))
                {
                    ModelState.AddModelError("VideoUrl", "URL видео обязательно");
                }
                
                if (LanguageId == 0)
                {
                    ModelState.AddModelError("LanguageId", "Выберите язык");
                }
                
                if (LanguageLevelId == 0)
                {
                    ModelState.AddModelError("LanguageLevelId", "Выберите уровень языка");
                }
                
                if (string.IsNullOrEmpty(Duration))
                {
                    ModelState.AddModelError("Duration", "Длительность должна быть указана");
                }
                
                if (!ModelState.IsValid)
                {
                    ViewBag.ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage))
                    );
                    
                    return View(video);
                }
                
                // Проверяем, что выбранный уровень принадлежит выбранному языку
                var level = await _context.LanguageLevels
                    .FirstOrDefaultAsync(ll => ll.Id == LanguageLevelId && ll.LanguageId == LanguageId);
                    
                if (level == null)
                {
                    ModelState.AddModelError("LanguageLevelId", "Выбранный уровень не принадлежит выбранному языку");
                    
                    ViewBag.ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage))
                    );
                    
                    return View(video);
                }
                
                // Преобразуем значения IsActive и IsFeatured из строки в bool
                bool isActiveValue = !string.IsNullOrEmpty(IsActive);
                bool isFeaturedValue = !string.IsNullOrEmpty(IsFeatured);
                
                // Проверяем, изменился ли URL видео
                bool videoUrlChanged = video.VideoUrl != VideoUrl;
                
                // Обновляем поля видео
                video.Title = Title;
                video.Description = Description ?? "";
                video.VideoUrl = VideoUrl;
                video.LanguageLevelId = LanguageLevelId;
                video.Duration = Duration;
                video.IsActive = isActiveValue;
                video.IsFeatured = isFeaturedValue;
                
                // Если URL видео изменился, обновляем миниатюру
                if (videoUrlChanged)
                {
                    video.ThumbnailUrl = GenerateThumbnailUrlFromYoutube(VideoUrl);
                }
                
                await _context.SaveChangesAsync();
                
                // Устанавливаем сообщение об успехе
                TempData["SuccessMessage"] = $"Видео \"{video.Title}\" успешно обновлено!";
                
                // Перенаправляем на страницу деталей видео
                return RedirectToAction("VideoDetails", new { id = video.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении видео: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Внутреннее исключение: {Message}", ex.InnerException.Message);
                }
                
                var video = await _context.Videos.FindAsync(id);
                if (video != null)
                {
                    ModelState.AddModelError("", $"Произошла ошибка при обновлении видео: {ex.Message}");
                    return View(video);
                }
                
                return NotFound();
            }
        }

        // POST: /Admin/DeleteVideo/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideo(int id)
        {
            var video = await _context.Videos.FindAsync(id);
            if (video == null)
            {
                return NotFound();
            }

            try
            {
                // Удаляем связанные записи о просмотрах
                await _videoService.DeleteWatchedVideoRecords(id);

                _context.Videos.Remove(video);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Видео с ID {id} успешно удалено");
                TempData["SuccessMessage"] = "Видео успешно удалено!";
                return RedirectToAction("Videos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при удалении видео с ID {id}: {ex.Message}");
                TempData["ErrorMessage"] = $"Ошибка при удалении видео: {ex.Message}";
                return RedirectToAction("VideoDetails", new { id });
            }
        }

        // GET: /Admin/Logout
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Admin");
        }

        // Вспомогательный метод для проверки существования теста
        private bool TestExists(int id)
        {
            return _context.Tests.Any(e => e.Id == id);
        }

        // Вспомогательный метод для проверки существования видео
        private bool VideoExists(int id)
        {
            return _context.Videos.Any(e => e.Id == id);
        }

        [HttpGet]
        public async Task<IActionResult> GetLanguageLevels(int languageId)
        {
            var levels = await _context.LanguageLevels
                .Where(l => l.LanguageId == languageId)
                .Select(l => new { l.Id, l.Name })
                .ToListAsync();
            
            return Json(levels);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            return Json(roles);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromForm] CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                RegistrationDate = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRolesAsync(user, model.Roles);
                return RedirectToAction(nameof(Users));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View("Users", await GetUsersWithRoles());
        }

        [HttpPost]
        public async Task<IActionResult> EditUser([FromForm] EditUserViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            user.UserName = model.Username;
            user.Email = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRolesAsync(user, model.Roles);
                return RedirectToAction(nameof(Users));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View("Users", await GetUsersWithRoles());
        }

        private async Task<List<Models.UserWithRolesViewModel>> GetUsersWithRoles()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<Models.UserWithRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new Models.UserWithRolesViewModel
                {
                    User = user,
                    Roles = roles.ToList()
                });
            }

            return userViewModels;
        }

        // GET: /Admin/AddQuestion/{testId}
        public async Task<IActionResult> AddQuestion(int testId)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var test = await _context.Tests
                .Include(t => t.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(t => t.Id == testId);
                
            if (test == null)
            {
                return NotFound();
            }
            
            ViewBag.Test = test;
            ViewBag.ExistingQuestions = test.Questions.OrderBy(q => q.Order).ToList();
            return View(new Question { TestId = testId });
        }
        
        // POST: /Admin/AddQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(Question question, string action)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            try
            {
                // Логирование входящих данных для отладки
                _logger.LogInformation("Добавление вопроса. Данные: {QuestionText}, TestId: {TestId}, QuestionType: {QuestionType}", 
                    question.Text, question.TestId, question.QuestionType);
                
                // Удаляем ошибки валидации для навигационных свойств
                if (!ModelState.IsValid)
                {
                    // Удаляем ошибку валидации для поля Test
                    if (ModelState.ContainsKey("Test"))
                    {
                        ModelState.Remove("Test");
                    }
                    
                    // Логируем оставшиеся ошибки
                    _logger.LogWarning("ModelState невалиден при добавлении вопроса. Ошибки: {Errors}", 
                        string.Join(", ", ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)));
                }
                
                // Проверяем только основные поля
                if (ModelState.IsValid || (ModelState.ErrorCount == 1 && ModelState.ContainsKey("Test")))
                {
                    // Используем транзакцию
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            // Получаем связанный тест
                            var test = await _context.Tests
                                .Include(t => t.Questions)
                                .FirstOrDefaultAsync(t => t.Id == question.TestId);
                            
                            if (test == null)
                            {
                                return NotFound("Тест не найден");
                            }
                            
                            // Устанавливаем порядок вопроса
                            question.Order = test.Questions?.Count ?? 0;
                            question.QuestionType = question.QuestionType >= 0 ? question.QuestionType : 0;
                            
                            // Создаем новый экземпляр вопроса без навигационных свойств
                            var newQuestion = new Question
                            {
                                Text = question.Text,
                                TestId = question.TestId,
                                Order = question.Order,
                                QuestionType = question.QuestionType,
                                CorrectAnswer = string.IsNullOrEmpty(question.CorrectAnswer) ? "" : question.CorrectAnswer
                            };
                            
                            // Добавляем вопрос в базу данных
                            _context.Questions.Add(newQuestion);
                            await _context.SaveChangesAsync();
                            
                            // Получаем данные о вариантах ответов из формы
                            var formData = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
                            
                            // Проверяем наличие данных о вариантах ответов
                            if (!formData.Any(x => x.Key.StartsWith("options[")))
                            {
                                _logger.LogWarning("Отсутствуют данные о вариантах ответов в форме");
                                ModelState.AddModelError("", "Необходимо добавить хотя бы два варианта ответа");
                                throw new Exception("Не удалось получить данные о вариантах ответов из формы");
                            }
                            
                            int optionIndex = 0;
                            bool hasCorrectOption = false;
                            
                            // Проходим по всем вариантам ответов из формы
                            while (formData.ContainsKey($"options[{optionIndex}].text"))
                            {
                                string optionText = formData[$"options[{optionIndex}].text"];
                                // Чекбокс возвращает "on" при отмеченном состоянии или отсутствует в форме при неотмеченном
                                // Проверяем наличие ключа в форме
                                bool isCorrect = formData.ContainsKey($"options[{optionIndex}].isCorrect");
                                
                                if (!string.IsNullOrEmpty(optionText))
                                {
                                    var option = new Option
                                    {
                                        Text = optionText,
                                        IsCorrect = isCorrect,
                                        QuestionId = newQuestion.Id
                                    };
                                    
                                    _context.Options.Add(option);
                                    
                                    if (isCorrect)
                                    {
                                        hasCorrectOption = true;
                                        // Для вопроса с одиночным выбором (тип 0) сохраняем правильный ответ
                                        if (newQuestion.QuestionType == 0)
                                        {
                                            newQuestion.CorrectAnswer = optionText;
                                        }
                                    }
                                }
                                
                                optionIndex++;
                            }
                            
                            // Проверяем, что есть хотя бы один правильный ответ
                            if (!hasCorrectOption)
                            {
                                throw new Exception("Необходимо отметить хотя бы один правильный ответ");
                            }
                            
                            // Проверяем, что добавлено хотя бы два варианта ответа
                            if (optionIndex < 2)
                            {
                                throw new Exception("Необходимо добавить как минимум два варианта ответа");
                            }
                            
                            // Сохраняем изменения
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                            
                            // Устанавливаем сообщение об успехе
                            TempData["SuccessMessage"] = "Тест успешно изменен! Вопрос и варианты ответов добавлены.";
                            
                            // Возвращаемся к деталям теста
                            return RedirectToAction("TestDetails", new { id = question.TestId });
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении вопроса: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Внутреннее исключение: {Message}", ex.InnerException.Message);
                }
                
                // Логирование данных формы для отладки
                _logger.LogInformation("Данные формы при ошибке: Question.Text={Text}, TestId={TestId}, Form Data={FormData}",
                    question.Text,
                    question.TestId,
                    JsonSerializer.Serialize(Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString())));
                
                ModelState.AddModelError("", $"Произошла ошибка при сохранении вопроса: {ex.Message}");
            }
            
            // Загружаем тест для отображения в форме
            var testForView = await _context.Tests
                .Include(t => t.LanguageLevel)
                .ThenInclude(ll => ll.Language)
                .FirstOrDefaultAsync(t => t.Id == question.TestId);
                
            ViewBag.Test = testForView;
            
            // Переходим обратно на форму с вопросом в случае ошибки
            return View(question);
        }
        
        // GET: /Admin/EditQuestion/{id}
        public async Task<IActionResult> EditQuestion(int id)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var question = await _context.Questions
                .Include(q => q.Test)
                    .ThenInclude(t => t.LanguageLevel)
                        .ThenInclude(ll => ll.Language)
                .FirstOrDefaultAsync(q => q.Id == id);
                
            if (question == null)
            {
                return NotFound();
            }
            
            ViewBag.Test = question.Test;
            return View(question);
        }
        
        // POST: /Admin/EditQuestion/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(int id, Question question)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            if (id != question.Id)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(question);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionExists(question.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("TestDetails", new { id = question.TestId });
            }
            
            var test = await _context.Tests
                .Include(t => t.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .FirstOrDefaultAsync(t => t.Id == question.TestId);
                
            ViewBag.Test = test;
            return View(question);
        }
        
        // POST: /Admin/DeleteQuestion/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);
                
            if (question == null)
            {
                return NotFound();
            }
            
            int testId = question.TestId;
            
            // Удаляем варианты ответов
            _context.Options.RemoveRange(question.Options);
            
            // Удаляем вопрос
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("TestDetails", new { id = testId });
        }
        
        // GET: /Admin/AddOptions/{questionId}
        public async Task<IActionResult> AddOptions(int questionId)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var question = await _context.Questions
                .Include(q => q.Test)
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId);
                
            if (question == null)
            {
                return NotFound();
            }
            
            ViewBag.Question = question;
            return View();
        }
        
        // POST: /Admin/AddOption
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOption(int questionId, string optionText, bool isCorrect)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            if (string.IsNullOrEmpty(optionText))
            {
                ModelState.AddModelError("", "Текст варианта ответа не может быть пустым");
                
                var questionForView = await _context.Questions
                    .Include(q => q.Test)
                    .Include(q => q.Options)
                    .FirstOrDefaultAsync(q => q.Id == questionId);
                
                ViewBag.Question = questionForView;
                return View("AddOptions");
            }
            
            // Получаем вопрос из базы данных
            var question = await _context.Questions
                .Include(q => q.Options)
                .Include(q => q.Test)
                .FirstOrDefaultAsync(q => q.Id == questionId);
                
            if (question == null)
            {
                return NotFound("Вопрос не найден");
            }
            
            try
            {
                // Используем транзакцию для обеспечения целостности данных
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Создаем новый вариант ответа
                        var option = new Option
                        {
                            Text = optionText,
                            IsCorrect = isCorrect,
                            QuestionId = questionId
                        };
                        
                        // Добавляем вариант ответа в базу данных
                        _context.Options.Add(option);
                        await _context.SaveChangesAsync();
                        
                        // Если это правильный ответ для вопроса с одиночным выбором
                        if (isCorrect && question.QuestionType == 0) // Тип 0 - одиночный выбор
                        {
                            // Если был другой правильный ответ, снимаем флаг
                            var previousCorrectOptions = await _context.Options
                                .Where(o => o.QuestionId == questionId && o.IsCorrect && o.Id != option.Id)
                                .ToListAsync();
                                
                            foreach (var prevOption in previousCorrectOptions)
                            {
                                prevOption.IsCorrect = false;
                            }
                            
                            if (previousCorrectOptions.Any())
                            {
                                _context.Options.UpdateRange(previousCorrectOptions);
                                await _context.SaveChangesAsync();
                            }
                            
                            // Обновляем правильный ответ в вопросе
                            question.CorrectAnswer = optionText;
                            _context.Questions.Update(question);
                            await _context.SaveChangesAsync();
                        }
                        
                        await transaction.CommitAsync();
                        
                        // Перенаправляем обратно на страницу с вариантами ответов
                        TempData["SuccessMessage"] = "Вариант ответа успешно добавлен!";
                        return RedirectToAction("AddOptions", new { questionId = questionId });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении варианта ответа: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Внутреннее исключение: {Message}", ex.InnerException.Message);
                }
                ModelState.AddModelError("", $"Произошла ошибка при сохранении варианта ответа: {ex.Message}");
                
                // В случае ошибки возвращаемся на ту же страницу
                ViewBag.Question = question;
                return View("AddOptions", question);
            }
        }
        
        // POST: /Admin/DeleteOption/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOption(int id)
        {
            var option = await _context.Options.FindAsync(id);
            
            if (option == null)
            {
                return NotFound();
            }
            
            int questionId = option.QuestionId;
            
            _context.Options.Remove(option);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("AddOptions", new { questionId });
        }
        
        // POST: /Admin/CompleteQuestionCreation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteQuestionCreation(int questionId)
        {
            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId);
                
            if (question == null)
            {
                return NotFound();
            }
            
            if (!question.Options.Any(o => o.IsCorrect))
            {
                ModelState.AddModelError("", "Должен быть хотя бы один правильный вариант ответа");
                ViewBag.Question = question;
                return View("AddOptions");
            }
            
            // Устанавливаем сообщение об успешном добавлении вопроса
            TempData["SuccessMessage"] = "Вопрос успешно добавлен! Вы можете добавить ещё один вопрос к тесту.";
            
            // Перенаправляем на страницу добавления вопроса к тому же тесту
            return RedirectToAction("AddQuestion", new { testId = question.TestId });
        }
        
        // GET: /Admin/AddAnotherQuestion/{testId}
        public async Task<IActionResult> AddAnotherQuestion(int testId)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var test = await _context.Tests
                .Include(t => t.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == testId);
                
            if (test == null)
            {
                return NotFound();
            }
            
            ViewBag.Test = test;
            ViewBag.QuestionCount = test.Questions.Count;
            
            return View(new Question { TestId = testId });
        }
        
        // POST: /Admin/AddAnotherQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAnotherQuestion(Question question, string action)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            if (ModelState.IsValid)
            {
                _context.Questions.Add(question);
                await _context.SaveChangesAsync();
                
                // В зависимости от выбранного действия перенаправляем пользователя
                if (action == "saveAndAddOptions")
                {
                    // Перенаправляем на страницу добавления вариантов ответа
                    return RedirectToAction("AddOptions", new { questionId = question.Id });
                }
                else
                {
                    // Просто сохраняем вопрос и возвращаемся к деталям теста
                    return RedirectToAction("TestDetails", new { id = question.TestId });
                }
            }
            
            var test = await _context.Tests
                .Include(t => t.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == question.TestId);
                
            ViewBag.Test = test;
            ViewBag.QuestionCount = test.Questions.Count;
            
            return View(question);
        }
        
        // POST: /Admin/FinishTestCreation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FinishTestCreation(int testId)
        {
            return RedirectToAction("TestDetails", new { id = testId });
        }
        
        private bool QuestionExists(int id)
        {
            return _context.Questions.Any(q => q.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTestWithQuestions(string Title, string Description, 
            int LanguageId, int LanguageLevelId, int TimeLimit, int PassingScore, string IsActive)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            // Получаем данные о вопросах из формы
            var formData = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            _logger.LogInformation($"CreateTestWithQuestions: Title={Title}, LanguageId={LanguageId}, LanguageLevelId={LanguageLevelId}");
            _logger.LogInformation($"Form data: {string.Join(", ", formData.Keys)}");
            
            // Получаем список языков для формы
            ViewBag.Languages = await _context.Languages.ToListAsync();
            
            if (LanguageId > 0)
            {
                ViewBag.LanguageLevels = await _context.LanguageLevels
                    .Where(ll => ll.LanguageId == LanguageId)
                    .ToListAsync();
            }
            
            // Валидация основных данных
            if (string.IsNullOrEmpty(Title) || LanguageId == 0 || LanguageLevelId == 0)
            {
                ModelState.AddModelError("", "Заполните все обязательные поля теста");
                TempData["ErrorMessage"] = "Заполните все обязательные поля теста";
                return View("CreateTest");
            }
            
            try
            {
                // Извлекаем вопросы из формы
                var questions = new List<QuestionViewModel>();
                int questionIndex = 0;
                bool hasQuestions = false;
                
                while (formData.ContainsKey($"questions[{questionIndex}].text"))
                {
                    string questionText = formData[$"questions[{questionIndex}].text"];
                    if (!string.IsNullOrEmpty(questionText))
                    {
                        hasQuestions = true;
                        int correctOption = -1;
                        if (formData.ContainsKey($"questions[{questionIndex}].correctOption"))
                        {
                            int.TryParse(formData[$"questions[{questionIndex}].correctOption"], out correctOption);
                        }
                        
                        var options = new List<OptionViewModel>();
                        int optionIndex = 0;
                        while (formData.ContainsKey($"questions[{questionIndex}].options[{optionIndex}].text"))
                        {
                            string optionText = formData[$"questions[{questionIndex}].options[{optionIndex}].text"];
                            if (!string.IsNullOrEmpty(optionText))
                            {
                                options.Add(new OptionViewModel { Text = optionText });
                            }
                            optionIndex++;
                        }
                        
                        if (options.Count >= 2)
                        {
                            questions.Add(new QuestionViewModel
                            {
                                Text = questionText,
                                CorrectOption = correctOption,
                                Options = options
                            });
                        }
                    }
                    questionIndex++;
                }
                
                if (!hasQuestions || questions.Count == 0)
                {
                    ModelState.AddModelError("", "Добавьте хотя бы один вопрос с вариантами ответов");
                    TempData["ErrorMessage"] = "Добавьте хотя бы один вопрос с вариантами ответов";
                    return View("CreateTest");
                }
                
                // Получаем уровень языка
                var languageLevel = await _context.LanguageLevels
                    .FirstOrDefaultAsync(ll => ll.Id == LanguageLevelId && ll.LanguageId == LanguageId);
                
                if (languageLevel == null)
                {
                    ModelState.AddModelError("", "Указанный уровень языка не найден");
                    TempData["ErrorMessage"] = "Указанный уровень языка не найден";
                    return View("CreateTest");
                }
                
                // Начинаем транзакцию для гарантии целостности данных
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Создаем тест
                        var test = new Test
                        {
                            Title = Title,
                            Description = Description ?? "",
                            LanguageLevelId = LanguageLevelId,
                            TimeLimit = TimeLimit,
                            PassingScore = PassingScore,
                            IsActive = !string.IsNullOrEmpty(IsActive),
                            OrderInLevel = 1,
                            CreatedDate = DateTime.Now
                        };
                        
                        _context.Tests.Add(test);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Создан тест с ID={test.Id}");
                        
                        int questionOrder = 0;
                        // Добавляем вопросы
                        foreach (var questionVM in questions)
                        {
                            if (string.IsNullOrEmpty(questionVM.Text))
                                continue;
                                
                            var question = new Question
                            {
                                Text = questionVM.Text,
                                TestId = test.Id,
                                Order = questionOrder++,
                                QuestionType = 0, // Одиночный выбор
                                Options = new List<Option>(),
                                // Инициализируем CorrectAnswer пустой строкой, чтобы избежать NULL
                                CorrectAnswer = ""
                            };
                            
                            _context.Questions.Add(question);
                            await _context.SaveChangesAsync();
                            
                            _logger.LogInformation($"Создан вопрос с ID: {question.Id}");
                            
                            // Добавляем варианты ответов
                            if (questionVM.Options != null && questionVM.Options.Any())
                            {
                                string correctAnswerText = null;
                                
                                for (int i = 0; i < questionVM.Options.Count; i++)
                                {
                                    var optionVM = questionVM.Options[i];
                                    if (string.IsNullOrEmpty(optionVM.Text))
                                        continue;
                                        
                                    bool isCorrect = (i == questionVM.CorrectOption);
                                    
                                    var option = new Option
                                    {
                                        Text = optionVM.Text,
                                        IsCorrect = isCorrect,
                                        QuestionId = question.Id
                                    };
                                    
                                    if (isCorrect)
                                    {
                                        correctAnswerText = optionVM.Text;
                                    }
                                    
                                    _context.Options.Add(option);
                                }
                                
                                // Устанавливаем правильный ответ в вопросе
                                if (!string.IsNullOrEmpty(correctAnswerText))
                                {
                                    question.CorrectAnswer = correctAnswerText;
                                    _context.Questions.Update(question);
                                }
                                else if (questionVM.Options.Count > 0) // Если не выбран правильный ответ, берем первый вариант
                                {
                                    question.CorrectAnswer = questionVM.Options[0].Text;
                                    _context.Questions.Update(question);
                                }
                                
                                await _context.SaveChangesAsync();
                            }
                        }
                        
                        // Если всё успешно, подтверждаем транзакцию
                        await transaction.CommitAsync();
                        
                        TempData["SuccessMessage"] = "Тест успешно создан!";
                        return RedirectToAction("TestDetails", new { id = test.Id });
                    }
                    catch (Exception ex)
                    {
                        // В случае ошибки отменяем транзакцию
                        await transaction.RollbackAsync();
                        throw; // Пробрасываем исключение для обработки во внешнем блоке catch
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании теста с вопросами: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Внутреннее исключение: {Message}", ex.InnerException.Message);
                }
                ModelState.AddModelError("", $"Произошла ошибка при создании теста: {ex.Message}");
                TempData["ErrorMessage"] = $"Произошла ошибка при создании теста: {ex.Message}";
                return View("CreateTest");
            }
        }
        
        public class QuestionViewModel
        {
            public string Text { get; set; }
            public int CorrectOption { get; set; }
            public List<OptionViewModel> Options { get; set; }
        }
        
        public class OptionViewModel
        {
            public string Text { get; set; }
        }
    }

    // ViewModel для входа в админ панель
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Введите имя пользователя")]
        [Display(Name = "Имя пользователя")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Введите пароль")]
        [Display(Name = "Пароль")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }

    // ViewModel для отображения деталей пользователя
    public class UserDetailsViewModel
    {
        public ApplicationUser User { get; set; }
        public IList<string> UserRoles { get; set; }
        public IList<TestResult> TestResults { get; set; }
        public IList<WatchedVideo> WatchedVideos { get; set; }
    }
} 