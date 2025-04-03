using Microsoft.EntityFrameworkCore;
using WebApplication15.Data;
using WebApplication15.Models;

namespace WebApplication15.Services
{
    public class TestDataService
    {
        private readonly ApplicationDbContext _context;

        public TestDataService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAdditionalTests()
        {
            // Получаем все языковые уровни
            var languageLevels = await _context.LanguageLevels
                .Include(ll => ll.Language)
                .ToListAsync();

            // Добавляем новые тесты для английского языка (4 теста)
            await AddEnglishTests(languageLevels);
            
            // Добавляем новые тесты для казахского языка (5 тестов)
            await AddKazakhTests(languageLevels);
            
            // Добавляем новые тесты для турецкого языка (5 тестов)
            await AddTurkishTests(languageLevels);
            
            // Сохраняем изменения в базе данных
            await _context.SaveChangesAsync();
        }

        private async Task AddEnglishTests(List<LanguageLevel> languageLevels)
        {
            // Получаем уровни для английского языка
            var englishA1 = languageLevels.FirstOrDefault(ll => ll.Language.Name == "Английский" && ll.Name == "A1");
            var englishA2 = languageLevels.FirstOrDefault(ll => ll.Language.Name == "Английский" && ll.Name == "A2");
            var englishB1 = languageLevels.FirstOrDefault(ll => ll.Language.Name == "Английский" && ll.Name == "B1");
            var englishB2 = languageLevels.FirstOrDefault(ll => ll.Language.Name == "Английский" && ll.Name == "B2");

            if (englishA1 != null)
            {
                // Тест 1 - Базовый словарный запас
                var test1 = new Test
                {
                    Title = "English Basics - Vocabulary",
                    Description = "Test your basic English vocabulary knowledge",
                    LanguageLevelId = englishA1.Id,
                    TimeLimit = 10,
                    Questions = new List<Question>
                    {
                        new Question
                        {
                            Text = "What is the correct translation for 'hello'?",
                            Options = new List<Option>
                            {
                                new Option { Text = "привет", IsCorrect = true },
                                new Option { Text = "пока", IsCorrect = false },
                                new Option { Text = "спасибо", IsCorrect = false },
                                new Option { Text = "извините", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "What does 'apple' mean?",
                            Options = new List<Option>
                            {
                                new Option { Text = "яблоко", IsCorrect = true },
                                new Option { Text = "банан", IsCorrect = false },
                                new Option { Text = "апельсин", IsCorrect = false },
                                new Option { Text = "груша", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Translate 'cat' to Russian:",
                            Options = new List<Option>
                            {
                                new Option { Text = "кошка", IsCorrect = true },
                                new Option { Text = "собака", IsCorrect = false },
                                new Option { Text = "птица", IsCorrect = false },
                                new Option { Text = "рыба", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "What's the Russian word for 'family'?",
                            Options = new List<Option>
                            {
                                new Option { Text = "семья", IsCorrect = true },
                                new Option { Text = "друг", IsCorrect = false },
                                new Option { Text = "работа", IsCorrect = false },
                                new Option { Text = "дом", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "What is 'water' in Russian?",
                            Options = new List<Option>
                            {
                                new Option { Text = "вода", IsCorrect = true },
                                new Option { Text = "огонь", IsCorrect = false },
                                new Option { Text = "воздух", IsCorrect = false },
                                new Option { Text = "земля", IsCorrect = false }
                            }
                        }
                    }
                };
                _context.Tests.Add(test1);
            }

            if (englishA2 != null)
            {
                // Тест 2 - Грамматика Present Simple
                var test2 = new Test
                {
                    Title = "English Grammar - Present Simple",
                    Description = "Test your knowledge of Present Simple tense",
                    LanguageLevelId = englishA2.Id,
                    TimeLimit = 15,
                    Questions = new List<Question>
                    {
                        new Question
                        {
                            Text = "She ___ to school every day.",
                            Options = new List<Option>
                            {
                                new Option { Text = "goes", IsCorrect = true },
                                new Option { Text = "go", IsCorrect = false },
                                new Option { Text = "going", IsCorrect = false },
                                new Option { Text = "went", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "I ___ coffee in the morning.",
                            Options = new List<Option>
                            {
                                new Option { Text = "drink", IsCorrect = true },
                                new Option { Text = "drinks", IsCorrect = false },
                                new Option { Text = "drinking", IsCorrect = false },
                                new Option { Text = "drank", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "We ___ English twice a week.",
                            Options = new List<Option>
                            {
                                new Option { Text = "study", IsCorrect = true },
                                new Option { Text = "studies", IsCorrect = false },
                                new Option { Text = "studying", IsCorrect = false },
                                new Option { Text = "studied", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "He ___ TV in the evening.",
                            Options = new List<Option>
                            {
                                new Option { Text = "watches", IsCorrect = true },
                                new Option { Text = "watch", IsCorrect = false },
                                new Option { Text = "watching", IsCorrect = false },
                                new Option { Text = "watched", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "They ___ in London.",
                            Options = new List<Option>
                            {
                                new Option { Text = "live", IsCorrect = true },
                                new Option { Text = "lives", IsCorrect = false },
                                new Option { Text = "living", IsCorrect = false },
                                new Option { Text = "lived", IsCorrect = false }
                            }
                        }
                    }
                };
                _context.Tests.Add(test2);
            }

            if (englishB1 != null)
            {
                // Тест 3 - Условные предложения
                var test3 = new Test
                {
                    Title = "English - Conditionals",
                    Description = "Test your knowledge of conditional sentences",
                    LanguageLevelId = englishB1.Id,
                    TimeLimit = 20,
                    Questions = new List<Question>
                    {
                        new Question
                        {
                            Text = "If it rains tomorrow, I ___ at home.",
                            Options = new List<Option>
                            {
                                new Option { Text = "will stay", IsCorrect = true },
                                new Option { Text = "stay", IsCorrect = false },
                                new Option { Text = "would stay", IsCorrect = false },
                                new Option { Text = "stayed", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "If I ___ rich, I would buy a big house.",
                            Options = new List<Option>
                            {
                                new Option { Text = "were", IsCorrect = true },
                                new Option { Text = "am", IsCorrect = false },
                                new Option { Text = "will be", IsCorrect = false },
                                new Option { Text = "would be", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "If he had studied harder, he ___ the exam.",
                            Options = new List<Option>
                            {
                                new Option { Text = "would have passed", IsCorrect = true },
                                new Option { Text = "would pass", IsCorrect = false },
                                new Option { Text = "will pass", IsCorrect = false },
                                new Option { Text = "passed", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "She ___ you if she had your phone number.",
                            Options = new List<Option>
                            {
                                new Option { Text = "would call", IsCorrect = true },
                                new Option { Text = "will call", IsCorrect = false },
                                new Option { Text = "called", IsCorrect = false },
                                new Option { Text = "calls", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "If I ___ the answer, I'll tell you.",
                            Options = new List<Option>
                            {
                                new Option { Text = "know", IsCorrect = true },
                                new Option { Text = "knew", IsCorrect = false },
                                new Option { Text = "would know", IsCorrect = false },
                                new Option { Text = "known", IsCorrect = false }
                            }
                        }
                    }
                };
                _context.Tests.Add(test3);
            }

            if (englishB2 != null)
            {
                // Тест 4 - Идиомы и фразовые глаголы
                var test4 = new Test
                {
                    Title = "English - Idioms & Phrasal Verbs",
                    Description = "Test your knowledge of common English idioms",
                    LanguageLevelId = englishB2.Id,
                    TimeLimit = 20,
                    Questions = new List<Question>
                    {
                        new Question
                        {
                            Text = "What does 'break a leg' mean?",
                            Options = new List<Option>
                            {
                                new Option { Text = "Good luck", IsCorrect = true },
                                new Option { Text = "Get injured", IsCorrect = false },
                                new Option { Text = "Run away", IsCorrect = false },
                                new Option { Text = "Be careful", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "The idiom 'it's raining cats and dogs' means:",
                            Options = new List<Option>
                            {
                                new Option { Text = "It's raining heavily", IsCorrect = true },
                                new Option { Text = "It's a strange weather", IsCorrect = false },
                                new Option { Text = "It's a light rain", IsCorrect = false },
                                new Option { Text = "Animals are falling from the sky", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "What does 'to give someone the cold shoulder' mean?",
                            Options = new List<Option>
                            {
                                new Option { Text = "To ignore someone", IsCorrect = true },
                                new Option { Text = "To give someone a jacket", IsCorrect = false },
                                new Option { Text = "To make someone cold", IsCorrect = false },
                                new Option { Text = "To help someone", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "The phrase 'to make up' can mean:",
                            Options = new List<Option>
                            {
                                new Option { Text = "To reconcile after an argument", IsCorrect = true },
                                new Option { Text = "To apply cosmetics", IsCorrect = false },
                                new Option { Text = "To create a story", IsCorrect = false },
                                new Option { Text = "All of the above", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "What does 'a piece of cake' mean?",
                            Options = new List<Option>
                            {
                                new Option { Text = "Something very easy", IsCorrect = true },
                                new Option { Text = "A dessert", IsCorrect = false },
                                new Option { Text = "A small portion", IsCorrect = false },
                                new Option { Text = "A celebration", IsCorrect = false }
                            }
                        }
                    }
                };
                _context.Tests.Add(test4);
            }
        }

        private async Task AddKazakhTests(List<LanguageLevel> languageLevels)
        {
            // Получаем уровни для казахского языка
            var kazakhA1 = languageLevels.FirstOrDefault(ll => ll.Language.Name == "Казахский" && ll.Name == "A1");
            var kazakhA2 = languageLevels.FirstOrDefault(ll => ll.Language.Name == "Казахский" && ll.Name == "A2");
            var kazakhB1 = languageLevels.FirstOrDefault(ll => ll.Language.Name == "Казахский" && ll.Name == "B1");
            var kazakhB2 = languageLevels.FirstOrDefault(ll => ll.Language.Name == "Казахский" && ll.Name == "B2");
            var kazakhC1 = languageLevels.FirstOrDefault(ll => ll.Language.Name == "Казахский" && ll.Name == "C1");

            if (kazakhA1 != null)
            {
                // Тест 1 - Числа и счет
                var test1 = new Test
                {
                    Title = "Казахский - Числа и счет",
                    Description = "Проверьте свои знания чисел на казахском языке",
                    LanguageLevelId = kazakhA1.Id,
                    TimeLimit = 10,
                    Questions = new List<Question>
                    {
                        new Question
                        {
                            Text = "Как будет 'один' на казахском?",
                            Options = new List<Option>
                            {
                                new Option { Text = "бір", IsCorrect = true },
                                new Option { Text = "екі", IsCorrect = false },
                                new Option { Text = "үш", IsCorrect = false },
                                new Option { Text = "төрт", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'пять' на казахском?",
                            Options = new List<Option>
                            {
                                new Option { Text = "бес", IsCorrect = true },
                                new Option { Text = "алты", IsCorrect = false },
                                new Option { Text = "жеті", IsCorrect = false },
                                new Option { Text = "сегіз", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'десять' на казахском?",
                            Options = new List<Option>
                            {
                                new Option { Text = "он", IsCorrect = true },
                                new Option { Text = "тоғыз", IsCorrect = false },
                                new Option { Text = "жиырма", IsCorrect = false },
                                new Option { Text = "отыз", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'двадцать' на казахском?",
                            Options = new List<Option>
                            {
                                new Option { Text = "жиырма", IsCorrect = true },
                                new Option { Text = "отыз", IsCorrect = false },
                                new Option { Text = "қырық", IsCorrect = false },
                                new Option { Text = "елу", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'сто' на казахском?",
                            Options = new List<Option>
                            {
                                new Option { Text = "жүз", IsCorrect = true },
                                new Option { Text = "мың", IsCorrect = false },
                                new Option { Text = "он", IsCorrect = false },
                                new Option { Text = "елу", IsCorrect = false }
                            }
                        }
                    }
                };
                _context.Tests.Add(test1);
            }

            if (kazakhA2 != null)
            {
                // Тест 2 - Семья и родственники
                var test2 = new Test
                {
                    Title = "Казахский - Семья и родственники",
                    Description = "Проверьте свои знания терминов семьи на казахском языке",
                    LanguageLevelId = kazakhA2.Id,
                    TimeLimit = 15,
                    Questions = new List<Question>
                    {
                        new Question
                        {
                            Text = "Как будет 'отец' на казахском?",
                            Options = new List<Option>
                            {
                                new Option { Text = "әке", IsCorrect = true },
                                new Option { Text = "ана", IsCorrect = false },
                                new Option { Text = "аға", IsCorrect = false },
                                new Option { Text = "іні", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'мать' на казахском?",
                            Options = new List<Option>
                            {
                                new Option { Text = "ана", IsCorrect = true },
                                new Option { Text = "әке", IsCorrect = false },
                                new Option { Text = "апа", IsCorrect = false },
                                new Option { Text = "әже", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'брат' на казахском?",
                            Options = new List<Option>
                            {
                                new Option { Text = "аға/іні", IsCorrect = true },
                                new Option { Text = "апа/сіңлі", IsCorrect = false },
                                new Option { Text = "әке", IsCorrect = false },
                                new Option { Text = "бала", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'сестра' на казахском?",
                            Options = new List<Option>
                            {
                                new Option { Text = "апа/сіңлі", IsCorrect = true },
                                new Option { Text = "аға/іні", IsCorrect = false },
                                new Option { Text = "ана", IsCorrect = false },
                                new Option { Text = "қыз", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'дедушка' на казахском?",
                            Options = new List<Option>
                            {
                                new Option { Text = "ата", IsCorrect = true },
                                new Option { Text = "әже", IsCorrect = false },
                                new Option { Text = "әке", IsCorrect = false },
                                new Option { Text = "немере", IsCorrect = false }
                            }
                        }
                    }
                };
                _context.Tests.Add(test2);
            }

            // Добавляем еще 3 теста для казахского (B1, B2, C1)
            // ...
        }

        private async Task AddTurkishTests(List<LanguageLevel> languageLevels)
        {
            // Получаем уровни для турецкого языка
            var turkishA1 = languageLevels.FirstOrDefault(ll => ll.Language.Name == "Турецкий" && ll.Name == "A1");
            var turkishA2 = languageLevels.FirstOrDefault(ll => ll.Language.Name == "Турецкий" && ll.Name == "A2");
            var turkishB1 = languageLevels.FirstOrDefault(ll => ll.Language.Name == "Турецкий" && ll.Name == "B1");
            var turkishB2 = languageLevels.FirstOrDefault(ll => ll.Language.Name == "Турецкий" && ll.Name == "B2");
            var turkishC1 = languageLevels.FirstOrDefault(ll => ll.Language.Name == "Турецкий" && ll.Name == "C1");

            if (turkishA1 != null)
            {
                // Тест 1 - Базовые приветствия
                var test1 = new Test
                {
                    Title = "Türkçe - Temel Selamlaşma",
                    Description = "Проверьте свои знания базовых приветствий на турецком языке",
                    LanguageLevelId = turkishA1.Id,
                    TimeLimit = 10,
                    Questions = new List<Question>
                    {
                        new Question
                        {
                            Text = "Как будет 'здравствуйте' на турецком?",
                            Options = new List<Option>
                            {
                                new Option { Text = "Merhaba", IsCorrect = true },
                                new Option { Text = "Güle güle", IsCorrect = false },
                                new Option { Text = "Teşekkür ederim", IsCorrect = false },
                                new Option { Text = "Özür dilerim", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'доброе утро' на турецком?",
                            Options = new List<Option>
                            {
                                new Option { Text = "Günaydın", IsCorrect = true },
                                new Option { Text = "İyi akşamlar", IsCorrect = false },
                                new Option { Text = "İyi geceler", IsCorrect = false },
                                new Option { Text = "İyi günler", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'спасибо' на турецком?",
                            Options = new List<Option>
                            {
                                new Option { Text = "Teşekkür ederim", IsCorrect = true },
                                new Option { Text = "Lütfen", IsCorrect = false },
                                new Option { Text = "Özür dilerim", IsCorrect = false },
                                new Option { Text = "Affedersiniz", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'до свидания' на турецком?",
                            Options = new List<Option>
                            {
                                new Option { Text = "Hoşça kal", IsCorrect = true },
                                new Option { Text = "Merhaba", IsCorrect = false },
                                new Option { Text = "Nasılsın", IsCorrect = false },
                                new Option { Text = "Tamam", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'как дела?' на турецком?",
                            Options = new List<Option>
                            {
                                new Option { Text = "Nasılsın?", IsCorrect = true },
                                new Option { Text = "Nerelisin?", IsCorrect = false },
                                new Option { Text = "Adın ne?", IsCorrect = false },
                                new Option { Text = "Kaç yaşındasın?", IsCorrect = false }
                            }
                        }
                    }
                };
                _context.Tests.Add(test1);
            }

            if (turkishA2 != null)
            {
                // Тест 2 - Еда и напитки
                var test2 = new Test
                {
                    Title = "Türkçe - Yiyecek ve İçecekler",
                    Description = "Проверьте свои знания названий еды и напитков на турецком языке",
                    LanguageLevelId = turkishA2.Id,
                    TimeLimit = 15,
                    Questions = new List<Question>
                    {
                        new Question
                        {
                            Text = "Как будет 'хлеб' на турецком?",
                            Options = new List<Option>
                            {
                                new Option { Text = "Ekmek", IsCorrect = true },
                                new Option { Text = "Su", IsCorrect = false },
                                new Option { Text = "Kahve", IsCorrect = false },
                                new Option { Text = "Çay", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'вода' на турецком?",
                            Options = new List<Option>
                            {
                                new Option { Text = "Su", IsCorrect = true },
                                new Option { Text = "Çay", IsCorrect = false },
                                new Option { Text = "Kahve", IsCorrect = false },
                                new Option { Text = "Süt", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'чай' на турецком?",
                            Options = new List<Option>
                            {
                                new Option { Text = "Çay", IsCorrect = true },
                                new Option { Text = "Kahve", IsCorrect = false },
                                new Option { Text = "Su", IsCorrect = false },
                                new Option { Text = "Süt", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'мясо' на турецком?",
                            Options = new List<Option>
                            {
                                new Option { Text = "Et", IsCorrect = true },
                                new Option { Text = "Balık", IsCorrect = false },
                                new Option { Text = "Tavuk", IsCorrect = false },
                                new Option { Text = "Peynir", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Как будет 'фрукты' на турецком?",
                            Options = new List<Option>
                            {
                                new Option { Text = "Meyve", IsCorrect = true },
                                new Option { Text = "Sebze", IsCorrect = false },
                                new Option { Text = "Balık", IsCorrect = false },
                                new Option { Text = "Ekmek", IsCorrect = false }
                            }
                        }
                    }
                };
                _context.Tests.Add(test2);
            }

            // Добавляем еще 3 теста для турецкого (B1, B2, C1)
            // ...
        }
    }
} 