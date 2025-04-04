using Microsoft.AspNetCore.Mvc;
using WebApplication15.Models;
using Microsoft.EntityFrameworkCore;
using WebApplication15.Services;
using WebApplication15.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;

namespace WebApplication15.Controllers
{
    public class VideoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LanguageService _languageService;
        private readonly ThemeService _themeService;
        private readonly AuthService _authService;
        private readonly VideoService _videoService;

        public VideoController(ApplicationDbContext context, 
                              LanguageService languageService,
                              ThemeService themeService,
                              AuthService authService,
                              VideoService videoService)
        {
            _context = context;
            _languageService = languageService;
            _themeService = themeService;
            _authService = authService;
            _videoService = videoService;
        }

        // GET: /Video
        public async Task<IActionResult> Index(int? languageId = null, string? levelName = null)
        {
            // Установка темы
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            
            // Получение языка пользователя
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            
            // Получение текущего пользователя
            ViewBag.IsAuthenticated = _authService.IsAuthenticated();
            string userId = null;
            
            if (ViewBag.IsAuthenticated)
            {
                ViewBag.CurrentUser = await _authService.GetCurrentUser();
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
            
            // Загружаем все языки
            var languages = await _context.Languages.ToListAsync();
            ViewBag.Languages = languages;
            
            // Загружаем все уровни с информацией о языке
            var levels = await _context.LanguageLevels
                .Include(l => l.Language)
                .ToListAsync();
            ViewBag.Levels = levels;
            
            // Формирование запроса с проверкой статуса просмотра
            var query = _context.Videos
                .Include(v => v.LanguageLevel)
                .ThenInclude(ll => ll.Language)
                .AsQueryable();
            
            // Применение фильтра по языку
            if (languageId.HasValue)
            {
                query = query.Where(v => v.LanguageLevel.LanguageId == languageId.Value);
                var selectedLanguage = languages.FirstOrDefault(l => l.Id == languageId.Value);
                ViewBag.SelectedLanguage = selectedLanguage?.Name;
                ViewBag.SelectedLanguageId = languageId;
                
                // Фильтруем уровни только для выбранного языка для показа в фильтре
                var filteredLevels = levels.Where(l => l.LanguageId == languageId.Value).ToList();
                ViewBag.FilteredLevels = filteredLevels;
            }
            else
            {
                ViewBag.FilteredLevels = levels;
            }
            
            // Применение фильтра по уровню языка
            if (!string.IsNullOrEmpty(levelName))
            {
                query = query.Where(v => v.LanguageLevel.Name == levelName);
                ViewBag.SelectedLevel = levelName;
            }
            
            // Дополнительная диагностика
            var videosCount = await query.CountAsync();
            ViewBag.DiagnosticCount = videosCount;
            
            // Получение результатов
            var videos = await query.ToListAsync();
            
            // Если пользователь авторизован, проверяем какие видео просмотрены
            if (!string.IsNullOrEmpty(userId))
            {
                var watchedVideoIds = await _context.WatchedVideos
                    .Where(w => w.UserId == userId)
                    .Select(w => w.VideoId)
                    .ToListAsync();
                    
                foreach (var video in videos)
                {
                    video.IsWatched = watchedVideoIds.Contains(video.Id);
                }
            }
            
            return View(videos);
        }

        // GET: /Video/ByLevel/{languageId}/{levelName}
        public async Task<IActionResult> ByLevel(int languageId, string levelName)
        {
            // Установка темы
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            
            // Получение языка пользователя
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            
            // Получение текущего пользователя
            ViewBag.IsAuthenticated = _authService.IsAuthenticated();
            string userId = null;
            
            if (ViewBag.IsAuthenticated)
            {
                ViewBag.CurrentUser = await _authService.GetCurrentUser();
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
            
            // Получение языка и уровня
            var language = await _context.Languages.FindAsync(languageId);
            if (language == null)
            {
                return NotFound();
            }
            ViewBag.Language = language;
            
            // Получение уровней для выбранного языка
            var levels = await _context.LanguageLevels
                .Where(l => l.LanguageId == languageId)
                .ToListAsync();
            ViewBag.Levels = levels;
            
            var level = await _context.LanguageLevels
                .FirstOrDefaultAsync(l => l.LanguageId == languageId && l.Name == levelName);
            if (level == null)
            {
                return NotFound();
            }
            ViewBag.Level = level;
            
            // Получение видео для выбранного уровня
            var videos = await _context.Videos
                .Include(v => v.LanguageLevel)
                .ThenInclude(ll => ll.Language)
                .Where(v => v.LanguageLevel.Id == level.Id)
                .ToListAsync();
                
            // Если пользователь авторизован, проверяем какие видео просмотрены
            if (!string.IsNullOrEmpty(userId))
            {
                var watchedVideoIds = await _context.WatchedVideos
                    .Where(w => w.UserId == userId)
                    .Select(w => w.VideoId)
                    .ToListAsync();
                    
                foreach (var video in videos)
                {
                    video.IsWatched = watchedVideoIds.Contains(video.Id);
                }
            }
            
            return View("ByLevel", videos);
        }

        // GET: /Video/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            // Установка темы
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            
            // Получение языка пользователя
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            
            // Получение текущего пользователя
            ViewBag.IsAuthenticated = _authService.IsAuthenticated();
            string userId = null;
            
            if (ViewBag.IsAuthenticated)
            {
                ViewBag.CurrentUser = await _authService.GetCurrentUser();
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
            
            // Получение видео с проверкой статуса просмотра
            var video = await _videoService.GetVideoByIdAsync(id, userId);
            
            if (video == null)
            {
                return NotFound();
            }
            
            // Получение других видео для этого же уровня с проверкой статуса просмотра
            var relatedVideos = await _videoService.GetRelatedVideosAsync(id, userId);
            ViewBag.RelatedVideos = relatedVideos;
            
            // Получение всех уровней для языка
            if (video.LanguageLevel?.Language != null)
            {
                var levels = await _context.LanguageLevels
                    .Where(l => l.LanguageId == video.LanguageLevel.Language.Id)
                    .OrderBy(l => l.Level)
                    .ToListAsync();
                ViewBag.Levels = levels;
            }
            
            // Если пользователь авторизован и видео еще не отмечено как просмотренное, отмечаем
            if (!string.IsNullOrEmpty(userId) && !video.IsWatched)
            {
                await _videoService.MarkVideoAsWatchedAsync(id, userId);
                video.IsWatched = true;
            }
            
            return View(video);
        }
        
        // POST: /Video/MarkAsWatched/{id}
        [HttpPost]
        public async Task<IActionResult> MarkAsWatched(int id)
        {
            if (!_authService.IsAuthenticated())
            {
                return Unauthorized();
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            await _videoService.MarkVideoAsWatchedAsync(id, userId);
            
            return Json(new { success = true, videoId = id });
        }
        
        // GET: /Video/WatchedVideos
        public async Task<IActionResult> WatchedVideos()
        {
            // Установка темы
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            
            // Получение языка пользователя
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            
            // Проверка авторизации
            ViewBag.IsAuthenticated = _authService.IsAuthenticated();
            
            if (!ViewBag.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.CurrentUser = await _authService.GetCurrentUser();
            
            // Получение просмотренных видео
            var watchedVideos = await _videoService.GetUserWatchedVideosAsync(userId);
            
            return View(watchedVideos);
        }
    }
} 