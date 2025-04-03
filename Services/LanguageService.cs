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
            var userLanguage = _httpContextAccessor.HttpContext?.Session.GetString("CurrentLanguage");
            return userLanguage ?? "kk"; // По умолчанию казахский
        }

        // Установка текущего языка для пользователя
        public void SetCurrentLanguage(string languageCode)
        {
            _httpContextAccessor.HttpContext?.Session.SetString("CurrentLanguage", languageCode);
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