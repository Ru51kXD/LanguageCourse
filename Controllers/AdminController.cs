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
                    CreatedDate = DateTime.Now
                };
                
                _context.Tests.Add(test);
                await _context.SaveChangesAsync();
                
                // Перенаправляем на страницу добавления вопроса
                return RedirectToAction("AddQuestion", new { testId = test.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании теста");
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

            // Отладочная информация: проверка валидности модели
            if (!ModelState.IsValid)
            {
                // Выводим все ошибки валидации в ModelState для отладки
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Count > 0)
                    {
                        var errors = string.Join(", ", state.Errors.Select(e => e.ErrorMessage));
                        ViewBag.ValidationErrors = ViewBag.ValidationErrors ?? new Dictionary<string, string>();
                        ViewBag.ValidationErrors[key] = errors;
                    }
                }
                
                // Записываем полученные данные для отладки
                ViewBag.ReceivedData = new { 
                    TestTitle = test.Title, 
                    TestDescription = test.Description,
                    LanguageId = LanguageId,
                    LanguageLevelId = test.LanguageLevelId,
                    TimeLimit = test.TimeLimit,
                    PassingScore = test.PassingScore
                };

            return View(test);
            }

            // Проверяем, что выбранный уровень принадлежит выбранному языку
            var level = await _context.LanguageLevels
                .FirstOrDefaultAsync(ll => ll.Id == test.LanguageLevelId && ll.LanguageId == LanguageId);
                
            if (level == null)
            {
                ModelState.AddModelError("LanguageLevelId", "Выбранный уровень не принадлежит выбранному языку");
                return View(test);
            }
            
            // Инициализируем другие обязательные поля по умолчанию, если они не установлены
            test.OrderInLevel = test.OrderInLevel <= 0 ? 1 : test.OrderInLevel;
            test.CreatedDate = DateTime.Now;
            test.IsActive = true;
            
            // Добавляем тест в базу данных
            _context.Tests.Add(test);
            await _context.SaveChangesAsync();
            
            // Перенаправляем на страницу добавления вопроса
            return RedirectToAction("AddQuestion", new { testId = test.Id });
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
        public async Task<IActionResult> EditTest(int id, Test test, int LanguageId)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var languages = _context.Languages
                .Include(l => l.LanguageLevels)
                .ToList();
                
            ViewBag.Languages = languages;

            if (id != test.Id)
            {
                return NotFound();
            }

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
            
                try
                {
                    _context.Update(test);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TestExists(test.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("TestDetails", new { id = test.Id });
            }
            return View(test);
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
            ViewBag.Theme = "admin";
            
            var videos = await _context.Videos
                .Include(v => v.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .OrderBy(v => v.LanguageLevel.Language.Name)
                .ThenBy(v => v.LanguageLevel.Name)
                .ToListAsync();

            return View(videos);
        }

        // GET: /Admin/VideoDetails/5
        public async Task<IActionResult> VideoDetails(int id)
        {
            TempData["ErrorMessage"] = "Функциональность просмотра деталей видео временно отключена";
            return RedirectToAction("Videos");
        }

        // GET: /Admin/CreateVideo
        public IActionResult CreateVideo()
        {
            TempData["ErrorMessage"] = "Функциональность добавления видео временно отключена";
            return RedirectToAction("Videos");
        }

        // POST: /Admin/CreateVideo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVideo(Video video, int LanguageId)
        {
            TempData["ErrorMessage"] = "Функциональность добавления видео временно отключена";
            return RedirectToAction("Videos");
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
        public async Task<IActionResult> EditVideo(int id, Video video, int LanguageId)
        {
            ViewBag.Theme = _themeService.GetCurrentTheme();
            
            var languages = _context.Languages
                .Include(l => l.LanguageLevels)
                .ToList();
                
            ViewBag.Languages = languages;

            if (id != video.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Проверяем, что выбранный уровень принадлежит выбранному языку
                var level = await _context.LanguageLevels
                    .FirstOrDefaultAsync(ll => ll.Id == video.LanguageLevelId && ll.LanguageId == LanguageId);
                    
                if (level == null)
                {
                    ModelState.AddModelError("LanguageLevelId", "Выбранный уровень не принадлежит выбранному языку");
                    return View(video);
                }
            
                try
                {
                    // Обновляем URL миниатюры только если URL видео изменился
                    var existingVideo = await _context.Videos.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
                    if (existingVideo != null && existingVideo.VideoUrl != video.VideoUrl)
                    {
                        video.ThumbnailUrl = await _videoService.GetThumbnailUrlFromVideoUrl(video.VideoUrl);
                    }

                    _context.Update(video);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Видео с ID {id} успешно обновлено");
                    TempData["SuccessMessage"] = "Видео успешно обновлено!";
                    return RedirectToAction("Videos");
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!VideoExists(video.Id))
                    {
                        _logger.LogWarning($"Видео с ID {id} не найдено при обновлении");
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, $"Ошибка при обновлении видео с ID {id}: {ex.Message}");
                        throw;
                    }
                }
            }
            else
            {
                _logger.LogWarning("Ошибка валидации формы редактирования видео: " + 
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }
            
            return View(video);
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

            // Удаляем связанные записи о просмотрах
            await _videoService.DeleteWatchedVideoRecords(id);

            _context.Videos.Remove(video);
            await _context.SaveChangesAsync();
            return RedirectToAction("Videos");
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
                .FirstOrDefaultAsync(t => t.Id == testId);
                
            if (test == null)
            {
                return NotFound();
            }
            
            ViewBag.Test = test;
            return View(new Question { TestId = testId });
        }
        
        // POST: /Admin/AddQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(Question question, string action)
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
                .FirstOrDefaultAsync(t => t.Id == question.TestId);
                
            ViewBag.Test = test;
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
            
            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId);
                
            if (question == null)
            {
                return NotFound();
            }
            
            if (string.IsNullOrEmpty(optionText))
            {
                ModelState.AddModelError("", "Текст варианта ответа не может быть пустым");
                ViewBag.Question = question;
                return View("AddOptions");
            }
            
            var option = new Option
            {
                Text = optionText,
                IsCorrect = isCorrect,
                QuestionId = questionId
            };
            
            _context.Options.Add(option);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("AddOptions", new { questionId });
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
            
            // Предлагаем добавить еще один вопрос к тому же тесту
            return RedirectToAction("AddAnotherQuestion", new { testId = question.TestId });
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
                
                // Создаем тест
                var test = new Test
                {
                    Title = Title,
                    Description = Description ?? "",
                    LanguageLevelId = LanguageLevelId,
                    TimeLimit = TimeLimit,
                    PassingScore = PassingScore,
                    IsActive = IsActive == "on",
                    CreatedDate = DateTime.Now,
                    OrderInLevel = 0 // Будет обновлено позже
                };
                
                _context.Tests.Add(test);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Создан тест с ID: {test.Id}");
                
                // Добавляем вопросы
                foreach (var questionVM in questions)
                {
                    if (string.IsNullOrEmpty(questionVM.Text))
                        continue;
                        
                    var question = new Question
                    {
                        Text = questionVM.Text,
                        TestId = test.Id
                    };
                    
                    _context.Questions.Add(question);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Создан вопрос с ID: {question.Id}");
                    
                    // Добавляем варианты ответов
                    if (questionVM.Options != null && questionVM.Options.Any())
                    {
                        for (int i = 0; i < questionVM.Options.Count; i++)
                        {
                            var optionVM = questionVM.Options[i];
                            if (string.IsNullOrEmpty(optionVM.Text))
                                continue;
                                
                            var option = new Option
                            {
                                Text = optionVM.Text,
                                IsCorrect = (i == questionVM.CorrectOption),
                                QuestionId = question.Id
                            };
                            
                            _context.Options.Add(option);
                        }
                        
                        await _context.SaveChangesAsync();
                    }
                }
                
                TempData["SuccessMessage"] = "Тест успешно создан!";
                return RedirectToAction("TestDetails", new { id = test.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании теста: {Message}", ex.Message);
                ModelState.AddModelError("", $"Ошибка при создании теста: {ex.Message}");
                TempData["ErrorMessage"] = $"Ошибка при создании теста: {ex.Message}";
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