using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication15.Models;
using WebApplication15.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using WebApplication15.Data;

namespace WebApplication15.Controllers
{
    public class LanguageController : Controller
    {
        private readonly LanguageService _languageService;
        private readonly ThemeService _themeService;
        private readonly AuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LanguageController(LanguageService languageService,
                                ThemeService themeService,
                                AuthService authService,
                                ApplicationDbContext context,
                                UserManager<ApplicationUser> userManager)
        {
            _languageService = languageService;
            _themeService = themeService;
            _authService = authService;
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Levels(int id)
        {
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            ViewBag.IsAuthenticated = _authService.IsAuthenticated();
            
            var language = _context.Languages.Find(id);
            if (language == null)
            {
                return NotFound();
            }
            
            ViewBag.Language = language;
            
            var levels = _context.LanguageLevels
                .Where(l => l.LanguageId == id)
                .OrderBy(l => l.Level)
                .ToList();
                
            return View(levels);
        }

        [Authorize]
        public async Task<IActionResult> Tests(int id)
        {
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            ViewBag.IsAuthenticated = true;
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }
            
            ViewBag.CurrentUser = currentUser;
            
            var level = await _context.LanguageLevels
                .Include(l => l.Language)
                .Include(l => l.Tests)
                .FirstOrDefaultAsync(l => l.Id == id);
                
            if (level == null)
            {
                return NotFound();
            }
            
            ViewBag.Level = level;
            
            string userId = currentUser.Id;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account"); // Если ID не удалось преобразовать
            }
            
            foreach (var test in level.Tests)
            {
                // Устанавливаем флаг завершения теста на основе наличия результатов
                var testResult = await _context.TestResults
                    .AnyAsync(tr => tr.TestId == test.Id && tr.UserId == userId);
                ViewData[$"TestCompleted_{test.Id}"] = testResult;
            }

            return View(level);
        }

        [Authorize]
        public async Task<IActionResult> Videos(int id)
        {
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            ViewBag.IsAuthenticated = true;
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }
            
            ViewBag.CurrentUser = currentUser;
            
            var level = await _context.LanguageLevels
                .Include(l => l.Language)
                .Include(l => l.Videos)
                .FirstOrDefaultAsync(l => l.Id == id);
                
            if (level == null)
            {
                return NotFound();
            }
            
            ViewBag.Level = level;
            return View(level);
        }

        public async Task<IActionResult> Kazakh()
        {
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            ViewBag.IsAuthenticated = _authService.IsAuthenticated();
            
            // Получаем язык и его уровни для формирования ссылок
            var language = await _context.Languages.FirstOrDefaultAsync(l => l.Code == "kk");
            var levels = new List<LanguageLevel>();
            
            if (language != null)
            {
                levels = await _context.LanguageLevels
                    .Where(l => l.LanguageId == language.Id)
                    .OrderBy(l => l.Level)
                    .ToListAsync();
                
                ViewBag.Language = language;
                ViewBag.Levels = levels;
            }
            
            var viewModel = new LanguageInfoViewModel
            {
                Name = "Казахский язык",
                NativeName = "Қазақ тілі",
                SpeakersCount = "20+ миллионов",
                Region = "Казахстан и Центральная Азия",
                FlagEmoji = "🇰🇿",
                BannerImage = "/images/kazakh_banner.jpg",
                Description = "Казахский язык - тюркский язык, государственный язык Казахстана. Он использует как кириллицу, так и латинский алфавит. Казахский язык является важной частью культурного наследия казахского народа и играет ключевую роль в сохранении и развитии национальной идентичности. В последние годы наблюдается рост интереса к изучению казахского языка как среди местного населения, так и среди иностранцев, интересующихся культурой и историей Казахстана.",
                Facts = new List<LanguageFact>
                {
                    new LanguageFact { Number = "01", Title = "Гармония гласных", Text = "В казахском языке слова следуют правилу гармонии гласных, когда гласные в слове должны быть либо все 'мягкими', либо все 'твердыми'." },
                    new LanguageFact { Number = "02", Title = "Многообразие диалектов", Text = "Казахский язык имеет три основных диалекта: западный, северо-восточный и южный, каждый со своими уникальными особенностями произношения и словарным запасом." },
                    new LanguageFact { Number = "03", Title = "Богатый словарный запас", Text = "Язык содержит множество слов для описания кочевого образа жизни, животноводства и природных явлений, отражая традиционный уклад жизни казахского народа." },
                    new LanguageFact { Number = "04", Title = "Письменность", Text = "Современный казахский язык использует кириллический алфавит, но ведётся переход на латиницу. Исторически казахский язык использовал арабское письмо." }
                }
            };
            
            return View("LanguageInfo", viewModel);
        }

        public async Task<IActionResult> English()
        {
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            ViewBag.IsAuthenticated = _authService.IsAuthenticated();
            
            // Получаем язык и его уровни для формирования ссылок
            var language = await _context.Languages.FirstOrDefaultAsync(l => l.Code == "en");
            var levels = new List<LanguageLevel>();
            
            if (language != null)
            {
                levels = await _context.LanguageLevels
                    .Where(l => l.LanguageId == language.Id)
                    .OrderBy(l => l.Level)
                    .ToListAsync();
                
                ViewBag.Language = language;
                ViewBag.Levels = levels;
            }
            
            var viewModel = new LanguageInfoViewModel
            {
                Name = "Английский язык",
                NativeName = "English",
                SpeakersCount = "1.5+ миллиарда",
                Region = "Глобальный",
                FlagEmoji = "🇬🇧",
                BannerImage = "/images/english_banner.jpg",
                Description = "Английский - германский язык, который стал международным языком бизнеса, науки и дипломатии. Он является основным языком для международного общения. Английский язык имеет статус официального или одного из официальных языков более чем в 60 странах мира. Это язык международных организаций, технологий, науки и популярной культуры, что делает его одним из самых важных языков для изучения в современном мире.",
                Facts = new List<LanguageFact>
                {
                    new LanguageFact { Number = "01", Title = "Глобальное распространение", Text = "Английский является официальным языком в более чем 50 странах и широко используется как второй язык во многих других, что делает его настоящим глобальным средством коммуникации." },
                    new LanguageFact { Number = "02", Title = "Богатый словарный запас", Text = "Английский язык имеет один из самых богатых словарных запасов среди всех языков мира, с более чем 1 миллионом слов, включая технические и специализированные термины." },
                    new LanguageFact { Number = "03", Title = "Историческое влияние", Text = "На английский язык оказали влияние многие языки, включая латинский, французский, немецкий и скандинавские языки, что привело к его богатому и разнообразному словарному составу." },
                    new LanguageFact { Number = "04", Title = "Диалекты и акценты", Text = "Английский язык имеет множество диалектов и акцентов, от британского и американского до австралийского, канадского, индийского и многих других, каждый со своими уникальными особенностями." }
                }
            };
            
            return View("LanguageInfo", viewModel);
        }

        public async Task<IActionResult> Turkish()
        {
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            ViewBag.IsAuthenticated = _authService.IsAuthenticated();
            
            // Получаем язык и его уровни для формирования ссылок
            var language = await _context.Languages.FirstOrDefaultAsync(l => l.Code == "tr");
            var levels = new List<LanguageLevel>();
            
            if (language != null)
            {
                levels = await _context.LanguageLevels
                    .Where(l => l.LanguageId == language.Id)
                    .OrderBy(l => l.Level)
                    .ToListAsync();
                
                ViewBag.Language = language;
                ViewBag.Levels = levels;
            }
            
            var viewModel = new LanguageInfoViewModel
            {
                Name = "Турецкий язык",
                NativeName = "Türkçe",
                SpeakersCount = "90+ миллионов",
                Region = "Турция и Ближний Восток",
                FlagEmoji = "🇹🇷",
                BannerImage = "/images/turkish_banner.jpg",
                Description = "Турецкий язык - тюркский язык, официальный язык Турции. После языковой реформы Ататюрка в 1928 году он использует латинский алфавит. Турецкий язык имеет долгую и богатую историю, являясь наследником османского языка и развиваясь под влиянием арабского и персидского языков. Сегодня это динамичный и живой язык, на котором говорят не только в Турции, но и в частях Кипра, Балкан и диаспорах по всему миру.",
                Facts = new List<LanguageFact>
                {
                    new LanguageFact { Number = "01", Title = "Агглютинативный язык", Text = "Турецкий является агглютинативным языком, в котором слова формируются путем добавления суффиксов к корню слова, что позволяет создавать очень длинные и сложные слова из простых корней." },
                    new LanguageFact { Number = "02", Title = "Гармония гласных", Text = "Как и казахский, турецкий подчиняется правилам гармонии гласных, что влияет на произношение и грамматику, делая язык более мелодичным и ритмичным." },
                    new LanguageFact { Number = "03", Title = "Богатая литературная традиция", Text = "Турецкая литература имеет богатую историю, от османской поэзии до современных романов, признанных во всем мире. Орхан Памук, турецкий писатель, получил Нобелевскую премию по литературе в 2006 году." },
                    new LanguageFact { Number = "04", Title = "Языковая реформа", Text = "В 1928 году Мустафа Кемаль Ататюрк провел масштабную языковую реформу, заменив арабское письмо на латиницу и очистив язык от многих арабских и персидских заимствований, что изменило облик турецкого языка." }
                }
            };
            
            return View("LanguageInfo", viewModel);
        }
    }
} 