using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using WebApplication15.Models;
using WebApplication15.Services;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace WebApplication15.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;
        private readonly ThemeService _themeService;
        private readonly LanguageService _languageService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly TestService _testService;

        public AccountController(
            AuthService authService,
            ThemeService themeService,
            LanguageService languageService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TestService testService)
        {
            _authService = authService;
            _themeService = themeService;
            _languageService = languageService;
            _userManager = userManager;
            _signInManager = signInManager;
            _testService = testService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (_authService.IsAuthenticated())
            {
                return RedirectToAction("Profile");
            }

            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Проверяем существование пользователя
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Пользователь с таким email не существует");
                    ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
                    ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Неверный пароль");
            }
            
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (_authService.IsAuthenticated())
            {
                return RedirectToAction("Profile");
            }

            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // Проверяем, авторизован ли пользователь
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login");
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }
            
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            ViewBag.IsAuthenticated = true;

            // Получаем идентификатор пользователя
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Получаем результаты тестов пользователя
            var testResults = await _testService.GetUserResults(userId);
            ViewBag.TestResults = testResults;
            
            // Дополнительные данные для профиля
            ViewBag.Languages = await _languageService.GetAllLanguages();
            ViewBag.TestsCount = testResults?.Count ?? 0;
            
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleTheme()
        {
            try
            {
                // Получаем текущее значение DarkMode из сессии
                bool isDarkMode = HttpContext.Session.GetString("DarkMode") == "true";
                
                // Изменяем значение на противоположное
                HttpContext.Session.SetString("DarkMode", (!isDarkMode).ToString());
                
                // Переключаем тему в сервисе
                _themeService.ToggleTheme();

                return RedirectToAction("Profile");
            }
            catch
            {
                return RedirectToAction("Profile");
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> ChangeLanguage(string languageCode)
        {
            try
            {
                // Сохраняем выбранный язык в сессии
                HttpContext.Session.SetString("PreferredLanguage", languageCode);
                
                // Обновляем язык в сервисе
                _languageService.SetCurrentLanguage(languageCode);

                // Установка культуры через cookies
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(languageCode)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );

                return RedirectToAction("Profile");
            }
            catch
            {
                return RedirectToAction("Profile");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
                ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
                ViewBag.IsAuthenticated = true;
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            user.UserName = model.UserName;
            user.Email = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                
                ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
                ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
                ViewBag.IsAuthenticated = true;
                return View(model);
            }

            return RedirectToAction("Profile");
        }

        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var model = new EditProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email
            };

            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            ViewBag.IsAuthenticated = true;
            return View(model);
        }
    }
} 