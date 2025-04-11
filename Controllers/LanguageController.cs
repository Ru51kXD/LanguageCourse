using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication15.Models;
using WebApplication15.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using WebApplication15.Data;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using System.Threading;

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

        public async Task<IActionResult> Tests(int id)
        {
            // Получаем уровень языка и его идентификатор языка
            var level = await _context.LanguageLevels
                .Include(l => l.Language)
                .FirstOrDefaultAsync(l => l.Id == id);
                
            if (level == null)
            {
                return NotFound();
            }
            
            // Перенаправляем на контроллер Test
            return RedirectToAction("Index", "Test", new { languageId = level.LanguageId, levelId = level.Id });
        }

        public async Task<IActionResult> Videos(int id)
        {
            // Получаем уровень языка и его идентификатор языка
            var level = await _context.LanguageLevels
                .Include(l => l.Language)
                .FirstOrDefaultAsync(l => l.Id == id);
                
            if (level == null)
            {
                return NotFound();
            }
            
            // Перенаправляем на контроллер Video
            return RedirectToAction("Index", "Video", new { languageId = level.LanguageId, levelName = level.Name });
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
            
            // Создаем модель предварительно с полным именем класса
            WebApplication15.Models.LanguageInfoViewModel viewModel = new()
            {
                Name = "Казахский язык",
                NativeName = "Қазақ тілі",
                SpeakersCount = "20+ миллионов",
                Region = "Казахстан и Центральная Азия",
                FlagEmoji = "🇰🇿",
                BannerImage = "/images/kazakh_banner.jpg",
                Description = "Казахский язык - тюркский язык, государственный язык Казахстана. Он использует как кириллицу, так и латинский алфавит. Казахский язык является важной частью культурного наследия казахского народа и играет ключевую роль в сохранении и развитии национальной идентичности. В последние годы наблюдается рост интереса к изучению казахского языка как среди местного населения, так и среди иностранцев, интересующихся культурой и историей Казахстана.",
                Facts = new List<WebApplication15.Models.LanguageFact>
                {
                    new() { Number = "01", Title = "Гармония гласных", Text = "В казахском языке слова следуют правилу гармонии гласных, когда гласные в слове должны быть либо все 'мягкими', либо все 'твердыми'." },
                    new() { Number = "02", Title = "Многообразие диалектов", Text = "Казахский язык имеет три основных диалекта: западный, северо-восточный и южный, каждый со своими уникальными особенностями произношения и словарным запасом." },
                    new() { Number = "03", Title = "Богатый словарный запас", Text = "Язык содержит множество слов для описания кочевого образа жизни, животноводства и природных явлений, отражая традиционный уклад жизни казахского народа." },
                    new() { Number = "04", Title = "Письменность", Text = "Современный казахский язык использует кириллический алфавит, но ведётся переход на латиницу. Исторически казахский язык использовал арабское письмо." }
                }
            };
            
            return View("LanguageInfo", viewModel);
        }

        public async Task<IActionResult> English()
        {
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            ViewBag.IsAuthenticated = _authService.IsAuthenticated();
            
            // Получаем текущую культуру
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
            var cultureTwoLetter = requestCulture?.RequestCulture.Culture.TwoLetterISOLanguageName ?? "ru";
            
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
            
            // Создаем модель предварительно с полным именем класса
            WebApplication15.Models.LanguageInfoViewModel viewModel = new()
            {
                Name = cultureTwoLetter switch
                {
                    "ru" => "Английский язык",
                    "kk" => "Ағылшын тілі",
                    "en" => "English",
                    "tr" => "İngilizce",
                    _ => "Английский язык"
                },
                NativeName = "English",
                SpeakersCount = cultureTwoLetter switch
                {
                    "ru" => "1.5+ миллиарда",
                    "kk" => "1.5+ миллиард",
                    "en" => "1.5+ billion",
                    "tr" => "1.5+ milyar",
                    _ => "1.5+ миллиарда"
                },
                Region = cultureTwoLetter switch
                {
                    "ru" => "Глобальный",
                    "kk" => "Жаһандық",
                    "en" => "Global",
                    "tr" => "Küresel",
                    _ => "Глобальный"
                },
                FlagEmoji = "🇬🇧",
                BannerImage = "/images/english_banner.jpg",
                Description = cultureTwoLetter switch
                {
                    "ru" => "Английский - германский язык, который стал международным языком бизнеса, науки и дипломатии. Он является основным языком для международного общения. Английский язык имеет статус официального или одного из официальных языков более чем в 60 странах мира. Это язык международных организаций, технологий, науки и популярной культуры, что делает его одним из самых важных языков для изучения в современном мире.",
                    "kk" => "Ағылшын тілі - бизнес, ғылым және дипломатияның халықаралық тіліне айналған герман тілі. Бұл халықаралық байланыстың негізгі тілі. Ағылшын тілі әлемнің 60-тан астам елінде ресми немесе ресми тілдердің бірі мәртебесіне ие. Бұл халықаралық ұйымдардың, технологиялардың, ғылым мен танымал мәдениеттің тілі, бұл оны қазіргі әлемде үйренуге ең маңызды тілдердің бірі етеді.",
                    "en" => "English is a Germanic language that has become the international language of business, science, and diplomacy. It is the primary language for international communication. English has official or co-official status in more than 60 countries worldwide. It is the language of international organizations, technology, science, and popular culture, making it one of the most important languages to learn in the modern world.",
                    "tr" => "İngilizce, iş, bilim ve diplomasinin uluslararası dili haline gelmiş bir Cermen dilidir. Uluslararası iletişim için birincil dildir. İngilizce, dünya çapında 60'tan fazla ülkede resmi veya ortak resmi statüye sahiptir. Uluslararası kuruluşların, teknolojinin, bilimin ve popüler kültürün dilidir, bu da onu modern dünyada öğrenilmesi gereken en önemli dillerden biri haline getirir.",
                    _ => "Английский - германский язык, который стал международным языком бизнеса, науки и дипломатии. Он является основным языком для международного общения. Английский язык имеет статус официального или одного из официальных языков более чем в 60 странах мира. Это язык международных организаций, технологий, науки и популярной культуры, что делает его одним из самых важных языков для изучения в современном мире."
                },
                Facts = new List<WebApplication15.Models.LanguageFact>
                {
                    new() { 
                        Number = "01", 
                        Title = cultureTwoLetter switch
                        {
                            "ru" => "Глобальное распространение",
                            "kk" => "Жаһандық тарату",
                            "en" => "Global Reach",
                            "tr" => "Küresel Erişim",
                            _ => "Глобальное распространение"
                        }, 
                        Text = cultureTwoLetter switch
                        {
                            "ru" => "Английский является официальным языком в более чем 50 странах и широко используется как второй язык во многих других, что делает его настоящим глобальным средством коммуникации.",
                            "kk" => "Ағылшын тілі 50-ден астам елде ресми тіл болып табылады және басқа көптеген елдерде екінші тіл ретінде кеңінен қолданылады, бұл оны нағыз жаһандық байланыс құралына айналдырады.",
                            "en" => "English is an official language in more than 50 countries and is widely used as a second language in many others, making it a truly global means of communication.",
                            "tr" => "İngilizce, 50'den fazla ülkede resmi dildir ve diğer birçok ülkede ikinci dil olarak yaygın şekilde kullanılmaktadır, bu da onu gerçek bir küresel iletişim aracı haline getirmektedir.",
                            _ => "Английский является официальным языком в более чем 50 странах и широко используется как второй язык во многих других, что делает его настоящим глобальным средством коммуникации."
                        }
                    },
                    new() { 
                        Number = "02", 
                        Title = cultureTwoLetter switch
                        {
                            "ru" => "Богатый словарный запас",
                            "kk" => "Бай сөздік қоры",
                            "en" => "Rich Vocabulary",
                            "tr" => "Zengin Kelime Hazinesi",
                            _ => "Богатый словарный запас"
                        }, 
                        Text = cultureTwoLetter switch
                        {
                            "ru" => "Английский язык имеет один из самых богатых словарных запасов среди всех языков мира, с более чем 1 миллионом слов, включая технические и специализированные термины.",
                            "kk" => "Ағылшын тілі әлемдегі барлық тіlдер арасында ең бай сөздік қорына ие, техникалық және мамандандырылған терминдерді қоса алғанда 1 миллионнан астам сөзбен.",
                            "en" => "English has one of the richest vocabularies among all languages in the world, with more than 1 million words, including technical and specialized terms.",
                            "tr" => "İngilizce, teknik ve özel terimler dahil olmak üzere 1 milyondan fazla kelimeyle dünyadaki tüm diller arasında en zengin kelime dağarcığından birine sahiptir.",
                            _ => "Английский язык имеет один из самых богатых словарных запасов среди всех языков мира, с более чем 1 миллионом слов, включая технические и специализированные термины."
                        }
                    },
                    new() { 
                        Number = "03", 
                        Title = cultureTwoLetter switch
                        {
                            "ru" => "Историческое влияние",
                            "kk" => "Тарихи әсер",
                            "en" => "Historical Influence",
                            "tr" => "Tarihsel Etki",
                            _ => "Историческое влияние"
                        }, 
                        Text = cultureTwoLetter switch
                        {
                            "ru" => "На английский язык оказали влияние многие языки, включая латинский, французский, немецкий и скандинавские языки, что привело к его богатому и разнообразному словарному составу.",
                            "kk" => "Ағылшын тіліне көптеген тілдер, соның ішінде латын, француз, неміс және скандинавия тілдері әсер етті, бұл оның бай және әртүрлі сөздік құрамына әкелді.",
                            "en" => "English has been influenced by many languages, including Latin, French, German, and Scandinavian languages, resulting in its rich and diverse vocabulary.",
                            "tr" => "İngilizce, Latince, Fransızca, Almanca ve İskandinav dilleri dahil olmak üzere birçok dilden etkilenmiştir, bu da zengin ve çeşitli kelime dağarcığına yol açmıştır.",
                            _ => "На английский язык оказали влияние многие языки, включая латинский, французский, немецкий и скандинавские языки, что привело к его богатому и разнообразному словарному составу."
                        }
                    },
                    new() { 
                        Number = "04", 
                        Title = cultureTwoLetter switch
                        {
                            "ru" => "Диалекты и акценты",
                            "kk" => "Диалектілер мен акценттер",
                            "en" => "Dialects and Accents",
                            "tr" => "Lehçeler ve Aksanlar",
                            _ => "Диалекты и акценты"
                        }, 
                        Text = cultureTwoLetter switch
                        {
                            "ru" => "Английский язык имеет множество диалектов и акцентов, от британского и американского до австралийского, канадского, индийского и многих других, каждый со своими уникальными особенностями.",
                            "kk" => "Ағылшын тілінде көптеген диалектілер мен акценттер бар, британдық пен американдықтан бастап австралиялық, канадалық, үнді және басқа да көптеген диалектілерге дейін, әрқайсысында өзіндік ерекшеліктері бар.",
                            "en" => "English has many dialects and accents, from British and American to Australian, Canadian, Indian, and many others, each with its own unique features.",
                            "tr" => "İngilizce, İngiliz ve Amerikan'dan Avustralya, Kanada, Hint ve daha birçoğuna kadar, her biri kendine özgü özelliklere sahip birçok lehçe ve aksana sahiptir.",
                            _ => "Английский язык имеет множество диалектов и акцентов, от британского и американского до австралийского, канадского, индийского и многих других, каждый со своими уникальными особенностями."
                        }
                    }
                }
            };
            
            return View("LanguageInfo", viewModel);
        }

        public async Task<IActionResult> Turkish()
        {
            ViewBag.IsDarkMode = _themeService.GetCurrentTheme();
            ViewBag.CurrentLanguage = _languageService.GetCurrentLanguage();
            ViewBag.IsAuthenticated = _authService.IsAuthenticated();
            
            // Получаем текущую культуру
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
            var cultureTwoLetter = requestCulture?.RequestCulture.Culture.TwoLetterISOLanguageName ?? "ru";
            
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
            
            // Создаем модель с полным именем класса
            WebApplication15.Models.LanguageInfoViewModel viewModel = new()
            {
                Name = cultureTwoLetter switch
                {
                    "ru" => "Турецкий язык",
                    "kk" => "Түрік тілі",
                    "en" => "Turkish",
                    "tr" => "Türkçe",
                    _ => "Турецкий язык"
                },
                NativeName = "Türkçe",
                SpeakersCount = cultureTwoLetter switch
                {
                    "ru" => "85+ миллионов",
                    "kk" => "85+ миллион",
                    "en" => "85+ million",
                    "tr" => "85+ milyon",
                    _ => "85+ миллионов"
                },
                Region = cultureTwoLetter switch
                {
                    "ru" => "Турция и Кипр",
                    "kk" => "Түркия және Кипр",
                    "en" => "Turkey and Cyprus",
                    "tr" => "Türkiye ve Kıbrıs",
                    _ => "Турция и Кипр"
                },
                FlagEmoji = "🇹🇷",
                BannerImage = "/images/turkish_banner.jpg",
                Description = cultureTwoLetter switch
                {
                    "ru" => "Турецкий язык — это тюркский язык, на котором говорят в Турции и на Кипре. Это официальный язык Турции и Северного Кипра. Турецкий также широко используется в странах со значительной турецкой диаспорой, таких как Германия, Болгария, Македония, Узбекистан и других балканских странах, странах Ближнего Востока и Центральной Азии. Турецкий — это язык с богатой историей и литературой, отражающий влияние различных культур.",
                    "kk" => "Түрік тілі — Түркия мен Кипрде сөйлейтін түркі тілі. Бұл Түркия мен Солтүстік Кипрдің ресми тілі. Түрікше сондай-ақ Германия, Болгария, Македония, Өзбекстан және басқа Балқан елдері, Таяу Шығыс пен Орталық Азия елдері сияқты түрік диаспорасы көп елдерде кеңінен қолданылады. Түрік тілі — бай тарихы мен әдебиеті бар, әртүрлі мәдениеттердің әсерін көрсететін тіl.",
                    "en" => "Turkish is a Turkic language spoken in Turkey and Cyprus. It is the official language of Turkey and Northern Cyprus. Turkish is also widely used in countries with significant Turkish diaspora, such as Germany, Bulgaria, Macedonia, Uzbekistan, and other Balkan countries, Middle Eastern and Central Asian countries. Turkish is a language with a rich history and literature, reflecting the influence of various cultures.",
                    "tr" => "Türkçe, Türkiye ve Kıbrıs'ta konuşulan bir Türk dilidir. Türkiye ve Kuzey Kıbrıs'ın resmi dilidir. Türkçe ayrıca Almanya, Bulgaristan, Makedonya, Özbekistan ve diğer Balkan ülkeleri, Orta Doğu ve Orta Asya ülkeleri gibi önemli Türk diasporasına sahip ülkelerde de yaygın olarak kullanılmaktadır. Türkçe, çeşitli kültürlerin etkisini yansıtan zengin bir tarihe ve edebiyata sahip bir dildir.",
                    _ => "Турецкий язык — это тюркский язык, на котором говорят в Турции и на Кипре. Это официальный язык Турции и Северного Кипра. Турецкий также широко используется в странах со значительной турецкой диаспорой, таких как Германия, Болгария, Македония, Узбекистан и других балканских странах, странах Ближнего Востока и Центральной Азии. Турецкий — это язык с богатой историей и литературой, отражающий влияние различных культур."
                },
                Facts = new List<WebApplication15.Models.LanguageFact>
                {
                    new() { 
                        Number = "01", 
                        Title = cultureTwoLetter switch
                        {
                            "ru" => "Агглютинативный язык",
                            "kk" => "Жалғамалы тіл",
                            "en" => "Agglutinative Language",
                            "tr" => "Bitişken Dil",
                            _ => "Агглютинативный язык"
                        }, 
                        Text = cultureTwoLetter switch
                        {
                            "ru" => "Турецкий — агглютинативный язык, что означает, что грамматические функции выражаются путем добавления суффиксов к словам. Одно турецкое слово может соответствовать целому предложению в других языках из-за наслоения суффиксов.",
                            "kk" => "Түрік тілі жалғамалы тіл болып табылады, бұл грамматикалық функциялар сөздерге жұрнақтар қосу арқылы білдірілетінін білдіреді. Жұрнақтардың қабаттасуына байланысты бір түрік сөзі басқа тіlдердегі бүкіл сөйлемге сәйкес келуі мүмкін.",
                            "en" => "Turkish is an agglutinative language, which means grammatical functions are expressed by adding suffixes to words. A single Turkish word can correspond to an entire sentence in other languages due to the layering of suffixes.",
                            "tr" => "Türkçe bitişken bir dildir, bu da dilbilgisel işlevlerin sözcüklere ekler eklenerek ifade edildiği anlamına gelir. Tek bir Türkçe kelime, eklerin katmanlanması nedeniyle diğer dillerde bütün bir cümleye karşılık gelebilir.",
                            _ => "Турецкий — агглютинативный язык, что означает, что грамматические функции выражаются путем добавления суффиксов к словам. Одно турецкое слово может соответствовать целому предложению в других языках из-за наслоения суффиксов."
                        }
                    },
                    new() { 
                        Number = "02", 
                        Title = cultureTwoLetter switch
                        {
                            "ru" => "Гармония гласных",
                            "kk" => "Дауысты дыбыстар үйлесімі",
                            "en" => "Vowel Harmony",
                            "tr" => "Ünlü Uyumu",
                            _ => "Гармония гласных"
                        }, 
                        Text = cultureTwoLetter switch
                        {
                            "ru" => "В турецком языке существует правило гармонии гласных, где гласные в суффиксах должны соответствовать гласным в корне слова. Это придает языку мелодичное звучание и влияет на грамматику и образование слов.",
                            "kk" => "Түрік тілінде дауысты дыбыстар үйлесімі ережесі бар, онда жұрнақтардағы дауысты дыбыстар сөз түбіріндегі дауысты дыбыстарға сәйкес келуі керек. Бұл тіlге әуезді дыбыс береді және грамматика мен сөз құрастыруға әсер етеді.",
                            "en" => "Turkish has a rule of vowel harmony, where vowels in suffixes must match vowels in the root of the word. This gives the language a melodious sound and affects grammar and word formation.",
                            "tr" => "Türkçe'de, eklerdeki ünlülerin kelimenin kökündeki ünlülere uyması gereken bir ünlü uyumu kuralı vardır. Bu, dile melodik bir ses verir ve dilbilgisini ve kelime oluşumunu etkiler.",
                            _ => "В турецком языке существует правило гармонии гласных, где гласные в суффиксах должны соответствовать гласным в корне слова. Это придает языку мелодичное звучание и влияет на грамматику и образование слов."
                        }
                    },
                    new() { 
                        Number = "03", 
                        Title = cultureTwoLetter switch
                        {
                            "ru" => "Латинский алфавит",
                            "kk" => "Латын әліпбиі",
                            "en" => "Latin Alphabet",
                            "tr" => "Latin Alfabesi",
                            _ => "Латинский алфавит"
                        }, 
                        Text = cultureTwoLetter switch
                        {
                            "ru" => "Современный турецкий язык использует латинский алфавит, который был введен Мустафой Кемалем Ататюрком в 1928 году в рамках модернизации Турции. До этой реформы турецкий язык писался арабским письмом.",
                            "kk" => "Қазіргі түрік тілі латын әліпбиін қолданады, ол 1928 жылы Мұстафа Кемал Ататүрік Түркияны жаңғырту аясында енгізген. Бұл реформаға дейін түрік тілі араб жазуымен жазылған.",
                            "en" => "Modern Turkish uses the Latin alphabet, which was introduced by Mustafa Kemal Atatürk in 1928 as part of the modernization of Turkey. Before this reform, Turkish was written in Arabic script.",
                            "tr" => "Modern Türkçe, 1928'de Mustafa Kemal Atatürk tarafından Türkiye'nin modernleşmesinin bir parçası olarak tanıtılan Latin alfabesini kullanır. Bu reformdan önce Türkçe Arap alfabesiyle yazılıyordu.",
                            _ => "Современный турецкий язык использует латинский алфавит, который был введен Мустафой Кемалем Ататюрком в 1928 году в рамках модернизации Турции. До этой реформы турецкий язык писался арабским письмом."
                        }
                    },
                    new() { 
                        Number = "04", 
                        Title = cultureTwoLetter switch
                        {
                            "ru" => "Культурное влияние",
                            "kk" => "Мәдени әсер",
                            "en" => "Cultural Influence",
                            "tr" => "Kültürel Etki",
                            _ => "Культурное влияние"
                        }, 
                        Text = cultureTwoLetter switch
                        {
                            "ru" => "Турецкий язык оказал значительное влияние на языки Балканского полуострова, Ближнего Востока и Центральной Азии из-за исторического влияния Османской империи. В свою очередь, на сам турецкий язык повлияли арабский и персидский языки.",
                            "kk" => "Түрік тілі Осман империясының тарихи ықпалына байланысты Балқан түбегі, Таяу Шығыс және Орталық Азия тіlдеріне айтарлықтай әсер етті. Өз кезегінде, түрік тіліне араб және парсы тіlдері әсер етті.",
                            "en" => "Turkish has had a significant influence on the languages of the Balkan Peninsula, the Middle East, and Central Asia due to the historical influence of the Ottoman Empire. In turn, Turkish itself has been influenced by Arabic and Persian.",
                            "tr" => "Türkçe, Osmanlı İmparatorluğu'nun tarihsel etkisi nedeniyle Balkan Yarımadası, Orta Doğu ve Orta Asya dillerinde önemli bir etkiye sahip olmuştur. Buna karşılık, Türkçe'nin kendisi de Arapça ve Farsça'dan etkilenmiştir.",
                            _ => "Турецкий язык оказал значительное влияние на языки Балканского полуострова, Ближнего Востока и Центральной Азии из-за исторического влияния Османской империи. В свою очередь, на сам турецкий язык повлияли арабский и персидский языки."
                        }
                    }
                }
            };
            
            return View("LanguageInfo", viewModel);
        }
    }
} 