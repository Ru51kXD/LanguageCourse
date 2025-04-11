using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using WebApplication15.Models;
using Microsoft.AspNetCore.Http;
using WebApplication15.Data;

namespace WebApplication15.Services
{
    public class LanguageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LanguageService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Получение всех языков
        public async Task<List<Language>> GetAllLanguages()
        {
            return await _context.Languages.ToListAsync();
        }

        // Получение языка по ID
        public async Task<Language?> GetLanguageById(int id)
        {
            return await _context.Languages.FindAsync(id);
        }

        // Получение языка по коду
        public async Task<Language?> GetLanguageByCode(string code)
        {
            return await _context.Languages.FirstOrDefaultAsync(l => l.Code == code);
        }

        // Получение текущего языка пользователя
        public string GetCurrentLanguage()
        {
            if (_httpContextAccessor.HttpContext == null)
                return "ru";

            // Сначала проверяем язык в сессии
            var sessionLanguage = _httpContextAccessor.HttpContext.Session.GetString("CurrentLanguage");
            if (!string.IsNullOrEmpty(sessionLanguage))
                return sessionLanguage;

            // Затем проверяем язык в куки
            var cookieValue = _httpContextAccessor.HttpContext.Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];
            if (!string.IsNullOrEmpty(cookieValue))
            {
                try
                {
                    // Формат куки: c=CULTURE|UICULTURE or c=CULTURE
                    var cultureParts = cookieValue.Split('|');
                    var culture = cultureParts[0].Substring(2);
                    
                    // Сохраняем в сессию для согласованности
                    SetCurrentLanguage(culture);
                    
                    return culture;
                }
                catch
                {
                    // В случае ошибки используем RequestCultureFeature
                }
            }
            
            // Пробуем через RequestCultureFeature
            var requestCultureFeature = _httpContextAccessor.HttpContext.Features.Get<IRequestCultureFeature>();
            if (requestCultureFeature != null)
            {
                var culture = requestCultureFeature.RequestCulture.Culture.TwoLetterISOLanguageName;
                if (!string.IsNullOrEmpty(culture))
                {
                    // Сохраняем в сессию для согласованности
                    SetCurrentLanguage(culture);
                    return culture;
                }
            }

            // По умолчанию возвращаем русский и сохраняем его в сессию
            SetCurrentLanguage("ru");
            return "ru";
        }

        // Установка текущего языка для пользователя
        public void SetCurrentLanguage(string languageCode)
        {
            if (_httpContextAccessor.HttpContext == null)
                return;
                
            // Устанавливаем язык в сессии
            _httpContextAccessor.HttpContext.Session.SetString("CurrentLanguage", languageCode);
            
            // И в куки для надежности
            var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(languageCode));
            _httpContextAccessor.HttpContext.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                cookieValue,
                new CookieOptions 
                { 
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    HttpOnly = false
                }
            );
        }

        // Установка языка для пользователя в базе данных
        public async Task SetUserLanguage(int userId, string languageCode)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.PreferredLanguage = languageCode;
                await _context.SaveChangesAsync();
            }

            // Также устанавливаем язык в сессии
            SetCurrentLanguage(languageCode);
        }

        // Получение всех уровней для языка
        public async Task<List<LanguageLevel>> GetLanguageLevels(int languageId)
        {
            return await _context.LanguageLevels
                .Where(l => l.LanguageId == languageId)
                .OrderBy(l => l.Level)
                .ToListAsync();
        }

        // Получение тестов для конкретного уровня языка
        public async Task<List<Test>> GetTestsByLevel(int levelId)
        {
            return await _context.Tests
                .Where(t => t.LanguageLevelId == levelId)
                .ToListAsync();
        }

        // Получение видеоуроков для конкретного уровня языка
        public async Task<List<Video>> GetVideosByLevel(int levelId)
        {
            return await _context.Videos
                .Where(v => v.LanguageLevelId == levelId)
                .ToListAsync();
        }
    }
} 