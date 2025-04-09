using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication15.Models;
using WebApplication15.Services;
using Microsoft.AspNetCore.Localization;
using WebApplication15.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading;

namespace WebApplication15.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly LanguageService _languageService;
    private readonly ThemeService _themeService;
    private readonly AuthService _authService;
    private readonly LocalizationService _localizationService;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, 
                         LanguageService languageService,
                         ThemeService themeService,
                         AuthService authService,
                         LocalizationService localizationService,
                         ApplicationDbContext context)
    {
        _logger = logger;
        _languageService = languageService;
        _themeService = themeService;
        _authService = authService;
        _localizationService = localizationService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Устанавливаем текущую культуру принудительно
        var currentLanguage = _languageService.GetCurrentLanguage();
        var cultureInfo = new CultureInfo(currentLanguage);
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        
        // Устанавливаем культуру для Request
        var requestCultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
        if (requestCultureFeature != null)
        {
            var cookieName = CookieRequestCultureProvider.DefaultCookieName;
            var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cultureInfo));
            
            Response.Cookies.Append(cookieName, cookieValue, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true
            });
        }
        
        ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
        ViewBag.CurrentLanguage = currentLanguage;
        ViewBag.IsAuthenticated = _authService.IsAuthenticated();
        ViewBag.LocalizationService = _localizationService;
        
        if (ViewBag.IsAuthenticated)
        {
            ViewBag.CurrentUser = await _authService.GetCurrentUser();
        }
        
        var languages = await _languageService.GetAllLanguages();
        return View(languages);
    }

    [HttpPost]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        // Установка cookie для локализации
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions 
            { 
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                HttpOnly = true,
                Secure = false
            }
        );

        // Установка текущей культуры для текущего запроса
        var cultureInfo = new CultureInfo(culture);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;
        
        // Сохранение выбранного языка в сессии
        _languageService.SetCurrentLanguage(culture);

        return LocalRedirect(returnUrl ?? "~/");
    }

    [HttpPost]
    public IActionResult ToggleTheme(string returnUrl)
    {
        _themeService.ToggleTheme();
        return Redirect(returnUrl ?? "/");
    }

    public IActionResult Privacy()
    {
        ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
        ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
        ViewBag.IsAuthenticated = _authService.IsAuthenticated();
        
        return View();
    }

    public async Task<IActionResult> AboutUs()
    {
        ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
        ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
        ViewBag.IsAuthenticated = _authService.IsAuthenticated();
        
        if (ViewBag.IsAuthenticated)
        {
            ViewBag.CurrentUser = await _authService.GetCurrentUser();
        }

        var teamMembers = new List<dynamic>
        {
            new 
            {
                Name = "Алексей Петров",
                Role = "Основатель и CEO",
                Bio = "Магистр лингвистики с 15-летним опытом преподавания языков. Основал платформу с целью сделать обучение языкам доступным для всех.",
                AvatarUrl = "/images/team/avatar1.jpg"
            },
            new 
            {
                Name = "Мария Иванова",
                Role = "Директор по обучению",
                Bio = "PhD в области образовательных технологий. Разрабатывает методики обучения и следит за качеством образовательного контента.",
                AvatarUrl = "/images/team/avatar2.jpg"
            },
            new 
            {
                Name = "Дмитрий Сидоров",
                Role = "Технический директор",
                Bio = "Более 10 лет опыта в разработке образовательных платформ. Отвечает за техническую инфраструктуру и разработку.",
                AvatarUrl = "/images/team/avatar3.jpg"
            },
            new 
            {
                Name = "Елена Соколова",
                Role = "Контент-менеджер",
                Bio = "Лингвист-переводчик с опытом создания образовательных материалов. Курирует производство видеоуроков и тестов.",
                AvatarUrl = "/images/team/avatar4.jpg"
            }
        };

        ViewBag.TeamMembers = teamMembers;
        
        // Получаем несколько видео для демонстрации
        var videos = await _context.Videos
            .Include(v => v.LanguageLevel)
            .ThenInclude(ll => ll.Language)
            .OrderByDescending(v => v.Id)
            .Take(3)
            .ToListAsync();
        
        return View(videos);
    }

    public IActionResult LearnMore()
    {
        ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
        ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
        ViewBag.IsAuthenticated = _authService.IsAuthenticated();
        
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

public class TeamMember
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}
