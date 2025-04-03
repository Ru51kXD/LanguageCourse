using Microsoft.EntityFrameworkCore;
using WebApplication15.Models;
using WebApplication15.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using System;

namespace WebApplication15.Services
{
    public class TestService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TestService> _logger;

        public TestService(ApplicationDbContext context, ILogger<TestService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Получение списка всех тестов
        public async Task<List<Test>> GetAllTests()
        {
            return await _context.Tests
                .Include(t => t.LanguageLevel)
                .ThenInclude(ll => ll.Language)
                .OrderBy(t => t.LanguageLevel.LanguageId)
                .ThenBy(t => t.LanguageLevel.Level)
                .ThenBy(t => t.OrderInLevel)
                .ToListAsync();
        }

        // Получение тестов по языку
        public async Task<List<Test>> GetTestsByLanguage(int languageId)
        {
            return await _context.Tests
                .Include(t => t.LanguageLevel)
                .ThenInclude(ll => ll.Language)
                .Where(t => t.LanguageLevel.LanguageId == languageId)
                .OrderBy(t => t.LanguageLevel.Level)
                .ThenBy(t => t.OrderInLevel)
                .ToListAsync();
        }

        // Получение тестов по уровню языка
        public async Task<List<Test>> GetTestsByLanguageLevelAsync(int languageLevelId)
        {
            return await _context.Tests
                .Include(t => t.Questions)
                .ThenInclude(q => q.Options)
                .Where(t => t.LanguageLevelId == languageLevelId)
                .ToListAsync();
        }

        // Получение теста по ID
        public async Task<Test?> GetTestByIdAsync(int id)
        {
            return await _context.Tests
                .Include(t => t.LanguageLevel)
                .ThenInclude(ll => ll.Language)
                .Include(t => t.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        // Получение теста с вопросами
        public async Task<Test?> GetTestWithQuestions(int testId)
        {
            return await _context.Tests
                .Include(t => t.LanguageLevel)
                .ThenInclude(ll => ll.Language)
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == testId);
        }

        // Проверка ответов на тест
        public async Task<int> CheckAnswers(int testId, Dictionary<int, string> userAnswers, string userId)
        {
            var test = await GetTestWithQuestions(testId);
            if (test == null || test.Questions == null)
            {
                return 0;
            }

            int correctAnswers = 0;

            foreach (var question in test.Questions)
            {
                if (question != null && userAnswers.TryGetValue(question.Id, out var userAnswer) && 
                    userAnswer == question.CorrectAnswer)
                {
                    correctAnswers++;
                }
            }

            // Сохраняем результат
            var testResult = new TestResult
            {
                TestId = testId,
                UserId = userId,
                Score = correctAnswers,
                TotalQuestions = test.Questions.Count,
                CompletedDate = DateTime.UtcNow
            };

            _context.TestResults.Add(testResult);
            await _context.SaveChangesAsync();

            return correctAnswers;
        }

        // Получение результатов пользователя
        public async Task<List<TestResult>> GetUserResults(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new List<TestResult>();
            }

            // Получаем все результаты тестов для указанного пользователя,
            // включая связанные данные о тесте и языке
            return await _context.TestResults
                .Include(tr => tr.Test)
                    .ThenInclude(t => t.LanguageLevel)
                        .ThenInclude(ll => ll.Language)
                .Where(tr => tr.UserId == userId)
                .OrderByDescending(tr => tr.CompletedDate)
                .ToListAsync();
        }

        // Получение результата по ID
        public async Task<TestResult?> GetResultById(int resultId)
        {
            return await _context.TestResults
                .Include(tr => tr.Test)
                .ThenInclude(t => t.LanguageLevel)
                .ThenInclude(ll => ll.Language)
                .FirstOrDefaultAsync(tr => tr.Id == resultId);
        }

        public async Task<bool> CheckAnswerAsync(int questionId, string selectedAnswer)
        {
            var question = await _context.Questions
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null) return false;

            return question.CorrectAnswer == selectedAnswer;
        }

        public async Task<bool> SaveTestResultAsync(TestResult result)
        {
            try
            {
                // Устанавливаем IsPassed на основе процента правильных ответов
                result.IsPassed = result.Score >= 70;
                
                _context.TestResults.Add(result);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении результата теста");
                return false;
            }
        }

        // Метод для заполнения базы данных демо-тестами
        public async Task SeedDemoTestsAsync()
        {
            try
            {
                // Проверяем, есть ли уже тесты в базе данных
                if (await _context.Tests.AnyAsync())
                {
                    _logger.LogInformation("Тесты уже существуют в базе данных. Очищаем существующие тесты...");
                    
                    // Удаляем существующие тесты
                    var existingOptions = await _context.Options.ToListAsync();
                    _context.Options.RemoveRange(existingOptions);
                    
                    var existingQuestions = await _context.Questions.ToListAsync();
                    _context.Questions.RemoveRange(existingQuestions);
                    
                    var existingTests = await _context.Tests.ToListAsync();
                    _context.Tests.RemoveRange(existingTests);
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Существующие тесты удалены успешно.");
                }

                _logger.LogInformation("Начинаем создание демо-тестов...");

                // Получаем все языки
                var languages = await _context.Languages.ToListAsync();
                
                // Получаем все уровни для каждого языка
                var languageLevels = await _context.LanguageLevels
                    .Include(l => l.Language)
                    .ToListAsync();
                    
                if (languages.Count == 0 || languageLevels.Count == 0)
                {
                    _logger.LogWarning("Языки или уровни не найдены в базе данных.");
                    return;
                }

                _logger.LogInformation($"Найдено {languages.Count} языков и {languageLevels.Count} уровней языков");

                // Создаем тесты для каждого языка и уровня
                var tests = new List<Test>();
                
                // Для каждого языка
                foreach (var language in languages)
                {
                    // Получаем уровни этого языка
                    var levels = languageLevels.Where(ll => ll.LanguageId == language.Id).OrderBy(ll => ll.Level).ToList();
                    
                    if (levels.Count == 0)
                    {
                        _logger.LogWarning($"Уровни для языка {language.Name} не найдены");
                        continue;
                    }
                    
                    // Для каждого уровня создаем 2 теста
                    foreach (var level in levels)
                    {
                        // Первый тест для уровня - словарный запас
                        var test1 = new Test
                        {
                            Title = $"{language.Name} - {level.Name} - Словарный запас",
                            Description = $"Проверьте свой словарный запас {language.Name} языка уровня {level.Name}. Этот тест поможет оценить ваше знание слов и выражений на данном уровне.",
                            LanguageLevelId = level.Id,
                            TimeLimit = 10 + level.Level * 2, // Увеличиваем время в зависимости от уровня
                            PassingScore = 70,
                            OrderInLevel = 1,
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        };
                        tests.Add(test1);
                        
                        // Второй тест для уровня - грамматика
                        var test2 = new Test
                        {
                            Title = $"{language.Name} - {level.Name} - Грамматика",
                            Description = $"Проверьте свои знания грамматики {language.Name} языка уровня {level.Name}. Этот тест фокусируется на грамматических конструкциях и правилах.",
                            LanguageLevelId = level.Id,
                            TimeLimit = 12 + level.Level * 3, // Увеличиваем время в зависимости от уровня
                            PassingScore = 70,
                            OrderInLevel = 2,
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        };
                        tests.Add(test2);
                    }
                }

                // Добавляем тесты в базу данных
                _logger.LogInformation($"Добавляем {tests.Count} тестов в базу данных...");
                await _context.Tests.AddRangeAsync(tests);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Тесты успешно сохранены в базе данных.");

                // Добавляем вопросы к тестам
                _logger.LogInformation("Добавляем вопросы к тестам...");
                
                // Создаем словари с вопросами для каждого языка и уровня
                var allQuestions = new List<Question>();
                
                foreach (var test in tests)
                {
                    // Получаем язык и уровень теста
                    var level = await _context.LanguageLevels
                        .Include(l => l.Language)
                        .FirstOrDefaultAsync(l => l.Id == test.LanguageLevelId);
                        
                    if (level == null) continue;
                    
                    var language = level.Language;
                    var isVocabularyTest = test.OrderInLevel == 1; // 1 - словарный запас, 2 - грамматика
                    
                    // Создаем 5 вопросов для теста
                    var questions = CreateQuestionsForTest(test.Id, language.Name, level.Name, isVocabularyTest);
                    allQuestions.AddRange(questions);
                }
                
                // Добавляем все вопросы в базу данных
                await _context.Questions.AddRangeAsync(allQuestions);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Добавлено {allQuestions.Count} вопросов к тестам");

                _logger.LogInformation("Демо-тесты успешно добавлены в базу данных.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании демо-тестов");
                throw;
            }
        }

        // Метод для создания вопросов для теста в зависимости от языка и уровня
        private List<Question> CreateQuestionsForTest(int testId, string language, string level, bool isVocabularyTest)
        {
            var questions = new List<Question>();
            
            if (language == "Английский" || language == "English")
            {
                if (isVocabularyTest)
                {
                    // Вопросы для теста по словарному запасу английского языка
                    switch (level)
                    {
                        case "A1":
                            questions.Add(CreateQuestion(testId, "What is the correct word for 'a place where people live'?", "House", 
                                new[] { "House", "Car", "Tree", "Book" }));
                            questions.Add(CreateQuestion(testId, "Choose the word that means 'a piece of furniture for sitting':", "Chair", 
                                new[] { "Chair", "Table", "Door", "Window" }));
                            questions.Add(CreateQuestion(testId, "Which word means 'a person who teaches'?", "Teacher", 
                                new[] { "Teacher", "Doctor", "Driver", "Student" }));
                            questions.Add(CreateQuestion(testId, "Select the word that means 'not big':", "Small", 
                                new[] { "Small", "Large", "Heavy", "Tall" }));
                            questions.Add(CreateQuestion(testId, "What is the correct word for 'the first meal of the day'?", "Breakfast", 
                                new[] { "Breakfast", "Lunch", "Dinner", "Supper" }));
                            break;
                        case "A2":
                            questions.Add(CreateQuestion(testId, "Which word means 'to put in order'?", "Arrange", 
                                new[] { "Arrange", "Forget", "Break", "Sleep" }));
                            questions.Add(CreateQuestion(testId, "Choose the correct word for 'not expensive':", "Cheap", 
                                new[] { "Cheap", "Costly", "Pricy", "Expensive" }));
                            questions.Add(CreateQuestion(testId, "What is the opposite of 'remember'?", "Forget", 
                                new[] { "Forget", "Recall", "Remind", "Memorize" }));
                            questions.Add(CreateQuestion(testId, "Which word means 'to enjoy yourself'?", "Fun", 
                                new[] { "Fun", "Boring", "Difficult", "Sad" }));
                            questions.Add(CreateQuestion(testId, "Select the word that means 'a person who sells things':", "Seller", 
                                new[] { "Seller", "Buyer", "Shopper", "Customer" }));
                            break;
                        case "B1":
                            questions.Add(CreateQuestion(testId, "Which word means 'to make better'?", "Improve", 
                                new[] { "Improve", "Worsen", "Damage", "Maintain" }));
                            questions.Add(CreateQuestion(testId, "Choose the correct synonym for 'beautiful':", "Gorgeous", 
                                new[] { "Gorgeous", "Ugly", "Plain", "Ordinary" }));
                            questions.Add(CreateQuestion(testId, "What is the meaning of 'to postpone'?", "To delay", 
                                new[] { "To delay", "To accelerate", "To cancel", "To continue" }));
                            questions.Add(CreateQuestion(testId, "Which word means 'happening now'?", "Current", 
                                new[] { "Current", "Past", "Future", "Ancient" }));
                            questions.Add(CreateQuestion(testId, "Select the word that means 'to get rid of':", "Eliminate", 
                                new[] { "Eliminate", "Acquire", "Keep", "Collect" }));
                            break;
                        case "B2":
                            questions.Add(CreateQuestion(testId, "Which word means 'to reduce in strength or force'?", "Diminish", 
                                new[] { "Diminish", "Increase", "Strengthen", "Amplify" }));
                            questions.Add(CreateQuestion(testId, "Choose the correct synonym for 'skeptical':", "Doubtful", 
                                new[] { "Doubtful", "Believing", "Trusting", "Convinced" }));
                            questions.Add(CreateQuestion(testId, "What is the meaning of 'redundant'?", "Unnecessary", 
                                new[] { "Unnecessary", "Essential", "Required", "Needed" }));
                            questions.Add(CreateQuestion(testId, "Which word means 'a formal expression of praise'?", "Commendation", 
                                new[] { "Commendation", "Criticism", "Rebuke", "Disapproval" }));
                            questions.Add(CreateQuestion(testId, "Select the word that means 'not straightforward':", "Convoluted", 
                                new[] { "Convoluted", "Direct", "Simple", "Clear" }));
                            break;
                        case "C1":
                            questions.Add(CreateQuestion(testId, "Which word means 'to speak in an unclear way'?", "Mumble", 
                                new[] { "Mumble", "Articulate", "Pronounce", "Enunciate" }));
                            questions.Add(CreateQuestion(testId, "Choose the correct synonym for 'ephemeral':", "Fleeting", 
                                new[] { "Fleeting", "Permanent", "Lasting", "Eternal" }));
                            questions.Add(CreateQuestion(testId, "What is the meaning of 'ubiquitous'?", "Present everywhere", 
                                new[] { "Present everywhere", "Rare", "Uncommon", "Unusual" }));
                            questions.Add(CreateQuestion(testId, "Which word means 'to officially forbid something'?", "Proscribe", 
                                new[] { "Proscribe", "Allow", "Permit", "Authorize" }));
                            questions.Add(CreateQuestion(testId, "Select the word that means 'to give up a position':", "Abdicate", 
                                new[] { "Abdicate", "Accept", "Maintain", "Uphold" }));
                            break;
                        case "C2":
                            questions.Add(CreateQuestion(testId, "Which word means 'to criticize severely'?", "Excoriate", 
                                new[] { "Excoriate", "Praise", "Compliment", "Flatter" }));
                            questions.Add(CreateQuestion(testId, "Choose the correct synonym for 'mellifluous':", "Melodious", 
                                new[] { "Melodious", "Harsh", "Dissonant", "Cacophonous" }));
                            questions.Add(CreateQuestion(testId, "What is the meaning of 'recondite'?", "Obscure or difficult to understand", 
                                new[] { "Obscure or difficult to understand", "Simple", "Obvious", "Clear" }));
                            questions.Add(CreateQuestion(testId, "Which word means 'a strong dislike or hatred'?", "Antipathy", 
                                new[] { "Antipathy", "Affection", "Fondness", "Admiration" }));
                            questions.Add(CreateQuestion(testId, "Select the word that means 'a long, pompous speech':", "Peroration", 
                                new[] { "Peroration", "Whisper", "Silence", "Brevity" }));
                            break;
                    }
                }
                else
                {
                    // Вопросы для теста по грамматике английского языка
                    switch (level)
                    {
                        case "A1":
                            questions.Add(CreateQuestion(testId, "Choose the correct form: She ___ a student.", "is", 
                                new[] { "is", "am", "are", "be" }));
                            questions.Add(CreateQuestion(testId, "Select the correct sentence:", "I don't like coffee.", 
                                new[] { "I don't like coffee.", "I not like coffee.", "I doesn't like coffee.", "I no like coffee." }));
                            questions.Add(CreateQuestion(testId, "What is the plural of 'child'?", "Children", 
                                new[] { "Children", "Childs", "Childes", "Childrens" }));
                            questions.Add(CreateQuestion(testId, "Choose the correct question:", "Where do you live?", 
                                new[] { "Where do you live?", "Where you live?", "Where does you live?", "Where living you?" }));
                            questions.Add(CreateQuestion(testId, "Select the correct article: ___ apple", "an", 
                                new[] { "an", "a", "the", "- (no article)" }));
                            break;
                        case "A2":
                            questions.Add(CreateQuestion(testId, "Choose the correct past form: Yesterday I ___ to the cinema.", "went", 
                                new[] { "went", "go", "goed", "was go" }));
                            questions.Add(CreateQuestion(testId, "Select the correct sentence:", "She has been to Paris twice.", 
                                new[] { "She has been to Paris twice.", "She have been to Paris twice.", "She been to Paris twice.", "She has went to Paris twice." }));
                            questions.Add(CreateQuestion(testId, "Choose the correct form: They ___ TV when I called.", "were watching", 
                                new[] { "were watching", "was watching", "watched", "are watching" }));
                            questions.Add(CreateQuestion(testId, "What is the comparative form of 'good'?", "better", 
                                new[] { "better", "gooder", "more good", "best" }));
                            questions.Add(CreateQuestion(testId, "Select the correct preposition: She's afraid ___ spiders.", "of", 
                                new[] { "of", "from", "at", "by" }));
                            break;
                        case "B1":
                            questions.Add(CreateQuestion(testId, "Choose the correct tense: I ___ here since 2010.", "have lived", 
                                new[] { "have lived", "live", "am living", "lived" }));
                            questions.Add(CreateQuestion(testId, "Select the correct conditional: If it rains, I ___ stay at home.", "will", 
                                new[] { "will", "would", "should", "must" }));
                            questions.Add(CreateQuestion(testId, "Choose the correct passive form: The letter ___ yesterday.", "was sent", 
                                new[] { "was sent", "is sent", "sent", "has been sent" }));
                            questions.Add(CreateQuestion(testId, "What is the correct reported speech: He said, 'I am happy.'", "He said that he was happy.", 
                                new[] { "He said that he was happy.", "He said that he is happy.", "He said that I am happy.", "He said that I was happy." }));
                            questions.Add(CreateQuestion(testId, "Select the correct modal verb: You ___ be tired after working all night.", "must", 
                                new[] { "must", "should", "would", "could" }));
                            break;
                        case "B2":
                            questions.Add(CreateQuestion(testId, "Choose the correct tense: By the time we arrived, the film ___.", "had already started", 
                                new[] { "had already started", "already started", "has already started", "was already starting" }));
                            questions.Add(CreateQuestion(testId, "Select the correct phrase: ___ to the theatre last night, I met an old friend.", "While going", 
                                new[] { "While going", "During going", "When go", "As went" }));
                            questions.Add(CreateQuestion(testId, "Choose the correct construction: She suggested ___ for a walk.", "going", 
                                new[] { "going", "to go", "go", "that we went" }));
                            questions.Add(CreateQuestion(testId, "What is the correct sentence with an inverted conditional?", "Had I known earlier, I would have told you.", 
                                new[] { "Had I known earlier, I would have told you.", "If I had known earlier, I would have told you.", "I had known earlier, I would have told you.", "I would have told you, had I known earlier." }));
                            questions.Add(CreateQuestion(testId, "Select the correct emphatic structure:", "Not only did he arrive late, but he also forgot his presentation.", 
                                new[] { "Not only did he arrive late, but he also forgot his presentation.", "Not only he arrived late, but he also forgot his presentation.", "He not only arrived late, but also he forgot his presentation.", "He arrived not only late, but also forgot his presentation." }));
                            break;
                        case "C1":
                            questions.Add(CreateQuestion(testId, "Choose the correct subjunctive: It is essential that he ___ present at the meeting.", "be", 
                                new[] { "be", "is", "will be", "would be" }));
                            questions.Add(CreateQuestion(testId, "Select the correct structure: ___ I known about your problem, I could have helped.", "Had", 
                                new[] { "Had", "If had", "Would have", "Have" }));
                            questions.Add(CreateQuestion(testId, "Choose the correct usage: The novel, ___ was published last year, has won several awards.", "which", 
                                new[] { "which", "what", "that", "who" }));
                            questions.Add(CreateQuestion(testId, "What is the correct causative form: She ___ her hair cut yesterday.", "had", 
                                new[] { "had", "got", "made", "did" }));
                            questions.Add(CreateQuestion(testId, "Select the correct cleft sentence:", "It was in 2008 that the financial crisis began.", 
                                new[] { "It was in 2008 that the financial crisis began.", "It was in 2008 when the financial crisis began.", "In 2008 it was that the financial crisis began.", "In 2008 was it that the financial crisis began." }));
                            break;
                        case "C2":
                            questions.Add(CreateQuestion(testId, "Choose the correct inversion: ___ I understand the complexity of the situation.", "Little did", 
                                new[] { "Little did", "Little", "Very little", "Did little" }));
                            questions.Add(CreateQuestion(testId, "Select the correct idiom: The project is on the back burner, which means it is ___.", "postponed or given lower priority", 
                                new[] { "postponed or given lower priority", "being worked on intensively", "almost completed", "cancelled permanently" }));
                            questions.Add(CreateQuestion(testId, "Choose the correct subjunctive: The committee recommended that the proposal ___ immediately.", "be implemented", 
                                new[] { "be implemented", "is implemented", "would be implemented", "implements" }));
                            questions.Add(CreateQuestion(testId, "What is the correct form with fronting: ___ I could accept, but his rudeness was intolerable.", "His incompetence", 
                                new[] { "His incompetence", "That his incompetence", "While his incompetence", "However his incompetence" }));
                            questions.Add(CreateQuestion(testId, "Select the correct sentence with a compound adjective:", "The time-consuming project required detailed planning.", 
                                new[] { "The time-consuming project required detailed planning.", "The time consuming project required detailed planning.", "The project of time-consuming required detailed planning.", "The project required time-consuming detailed planning." }));
                            break;
                    }
                }
            }
            else if (language == "Казахский" || language == "Kazakh")
            {
                // Добавляем базовые вопросы для казахского языка
                if (isVocabularyTest)
                {
                    // Лексика
                    questions.Add(CreateQuestion(testId, "Выберите правильный перевод слова 'дом':", "Үй", 
                        new[] { "Үй", "Мектеп", "Көше", "Қала" }));
                    questions.Add(CreateQuestion(testId, "Как на казахском языке 'собака'?", "Ит", 
                        new[] { "Ит", "Мысық", "Тышқан", "Аю" }));
                    questions.Add(CreateQuestion(testId, "Выберите правильный перевод слова 'вода':", "Су", 
                        new[] { "Су", "Шай", "Сүт", "Қант" }));
                    questions.Add(CreateQuestion(testId, "Как на казахском языке 'книга'?", "Кітап", 
                        new[] { "Кітап", "Дәптер", "Қалам", "Сурет" }));
                    questions.Add(CreateQuestion(testId, "Выберите правильный перевод слова 'школа':", "Мектеп", 
                        new[] { "Мектеп", "Дүкен", "Аурухана", "Кітапхана" }));
                }
                else
                {
                    // Грамматика
                    questions.Add(CreateQuestion(testId, "Выберите правильное окончание множественного числа: кітап___", "тар", 
                        new[] { "тар", "дар", "лар", "тер" }));
                    questions.Add(CreateQuestion(testId, "Какой суффикс используется для образования прилагательного от слова 'су' (вода)?", "лы", 
                        new[] { "лы", "ды", "ты", "сыз" }));
                    questions.Add(CreateQuestion(testId, "Выберите правильный вариант местоимения 'я' в казахском языке:", "Мен", 
                        new[] { "Мен", "Сен", "Ол", "Біз" }));
                    questions.Add(CreateQuestion(testId, "Какой суффикс используется для образования отрицательной формы глагола в казахском языке?", "ма/ме", 
                        new[] { "ма/ме", "ба/бе", "па/пе", "да/де" }));
                    questions.Add(CreateQuestion(testId, "Выберите правильный вариант спряжения глагола 'бару' (идти) в 1-м лице единственного числа:", "барамын", 
                        new[] { "барамын", "барасың", "барады", "барамыз" }));
                }
            }
            else if (language == "Турецкий" || language == "Turkish")
            {
                // Добавляем базовые вопросы для турецкого языка
                if (isVocabularyTest)
                {
                    // Лексика
                    questions.Add(CreateQuestion(testId, "Выберите правильный перевод слова 'дом':", "Ev", 
                        new[] { "Ev", "Okul", "Sokak", "Şehir" }));
                    questions.Add(CreateQuestion(testId, "Как на турецком языке 'собака'?", "Köpek", 
                        new[] { "Köpek", "Kedi", "Fare", "Ayı" }));
                    questions.Add(CreateQuestion(testId, "Выберите правильный перевод слова 'вода':", "Su", 
                        new[] { "Su", "Çay", "Süt", "Şeker" }));
                    questions.Add(CreateQuestion(testId, "Как на турецком языке 'книга'?", "Kitap", 
                        new[] { "Kitap", "Defter", "Kalem", "Resim" }));
                    questions.Add(CreateQuestion(testId, "Выберите правильный перевод слова 'школа':", "Okul", 
                        new[] { "Okul", "Dükkan", "Hastane", "Kütüphane" }));
                }
                else
                {
                    // Грамматика
                    questions.Add(CreateQuestion(testId, "Выберите правильное окончание множественного числа: kitap___", "lar", 
                        new[] { "lar", "ler", "tar", "tet" }));
                    questions.Add(CreateQuestion(testId, "Какой суффикс используется для образования прилагательного от слова 'su' (вода)?", "lu", 
                        new[] { "lu", "lı", "sız", "li" }));
                    questions.Add(CreateQuestion(testId, "Выберите правильный вариант местоимения 'я' в турецком языке:", "Ben", 
                        new[] { "Ben", "Sen", "O", "Biz" }));
                    questions.Add(CreateQuestion(testId, "Какой суффикс используется для образования отрицательной формы глагола в турецком языке?", "ma/me", 
                        new[] { "ma/me", "mi/mı", "mu/mü", "sa/se" }));
                    questions.Add(CreateQuestion(testId, "Выберите правильный вариант спряжения глагола 'gelmek' (приходить) в 1-м лице единственного числа настоящего времени:", "geliyorum", 
                        new[] { "geliyorum", "geliyorsun", "geliyor", "geliyoruz" }));
                }
            }
            else if (language == "Русский" || language == "Russian")
            {
                // Добавляем базовые вопросы для русского языка
                if (isVocabularyTest)
                {
                    // Лексика
                    questions.Add(CreateQuestion(testId, "Какое из этих слов обозначает место проживания людей?", "Дом", 
                        new[] { "Дом", "Машина", "Дерево", "Книга" }));
                    questions.Add(CreateQuestion(testId, "Выберите слово, обозначающее предмет мебели для сидения:", "Стул", 
                        new[] { "Стул", "Стол", "Дверь", "Окно" }));
                    questions.Add(CreateQuestion(testId, "Какое из этих слов обозначает человека, который учит других?", "Учитель", 
                        new[] { "Учитель", "Врач", "Водитель", "Студент" }));
                    questions.Add(CreateQuestion(testId, "Выберите слово, означающее 'не большой':", "Маленький", 
                        new[] { "Маленький", "Большой", "Тяжелый", "Высокий" }));
                    questions.Add(CreateQuestion(testId, "Какое из этих слов обозначает первый прием пищи?", "Завтрак", 
                        new[] { "Завтрак", "Обед", "Ужин", "Полдник" }));
                }
                else
                {
                    // Грамматика
                    questions.Add(CreateQuestion(testId, "Выберите правильное окончание множественного числа: стол___", "ы", 
                        new[] { "ы", "и", "а", "я" }));
                    questions.Add(CreateQuestion(testId, "Какой падеж отвечает на вопрос 'кого? чего?'?", "Родительный", 
                        new[] { "Родительный", "Дательный", "Винительный", "Именительный" }));
                    questions.Add(CreateQuestion(testId, "Выберите правильное спряжение глагола 'читать' в 1-м лице единственного числа:", "читаю", 
                        new[] { "читаю", "читаешь", "читает", "читаем" }));
                    questions.Add(CreateQuestion(testId, "Какое из этих слов является местоимением?", "Он", 
                        new[] { "Он", "Быстро", "Красивый", "Ходить" }));
                    questions.Add(CreateQuestion(testId, "Выберите слово с правильно расставленным ударением:", "звонИт", 
                        new[] { "звонИт", "звОнит", "Звонит", "зво-нит" }));
                }
            }
            
            return questions;
        }

        // Вспомогательный метод для создания вопроса с вариантами ответов
        private Question CreateQuestion(int testId, string text, string correctAnswer, string[] options)
        {
            var question = new Question
            {
                Text = text,
                TestId = testId,
                CorrectAnswer = correctAnswer,
                Options = new List<Option>()
            };
            
            foreach (var option in options)
            {
                question.Options.Add(new Option
                {
                    Text = option,
                    IsCorrect = option == correctAnswer
                });
            }
            
            return question;
        }
    }
} 