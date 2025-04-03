using Microsoft.Extensions.Localization;
using System.Reflection;

namespace WebApplication15.Models
{
    public class LocalizationService
    {
        private readonly IStringLocalizer _localizer;
        private static IStringLocalizer? _staticLocalizer;

        public LocalizationService(IStringLocalizerFactory factory)
        {
            try
            {
                var type = typeof(LocalizationService);
                var assemblyName = new AssemblyName(type.Assembly.FullName ?? "WebApplication15");
                _localizer = factory.Create(type.Name, assemblyName.Name ?? "WebApplication15");
                _staticLocalizer = _localizer;
            }
            catch (Exception)
            {
                // В случае ошибки создаем пустой локализатор
                _localizer = new EmptyStringLocalizer();
                _staticLocalizer = _localizer;
            }
        }

        public string GetLocalizedString(string key)
        {
            try
            {
                return _localizer[key];
            }
            catch
            {
                // В случае ошибки возвращаем ключ как значение
                return key;
            }
        }
        
        // Статический метод для использования в представлениях
        public static string GetLocalizedStringStatic(string key)
        {
            try
            {
                if (_staticLocalizer != null)
                {
                    return _staticLocalizer[key];
                }
                return key;
            }
            catch
            {
                // В случае ошибки возвращаем ключ как значение
                return key;
            }
        }
    }

    // Пустой локализатор для резервного использования
    public class EmptyStringLocalizer : IStringLocalizer
    {
        public LocalizedString this[string name] => new LocalizedString(name, name);

        public LocalizedString this[string name, params object[] arguments] => new LocalizedString(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return Array.Empty<LocalizedString>();
        }
    }
} 