using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace MyVocaList.Services
{
    public class LocalizationResourceManager : INotifyPropertyChanged
    {
        private const string ResourceId = "MyVocaList.Resources.Strings.AppResources";
        private static readonly Lazy<LocalizationResourceManager> _instance = new(() => new LocalizationResourceManager());

        public static LocalizationResourceManager Instance => _instance.Value;
        private readonly ResourceManager _resourceManager;

        public event PropertyChangedEventHandler PropertyChanged;

        private LocalizationResourceManager()
        {
            _resourceManager = new ResourceManager(ResourceId, typeof(LocalizationResourceManager).Assembly);
        }

        public string GetString(string key)
        {
            return _resourceManager.GetString(key, CurrentCulture) ?? key;
        }

        public void SetCulture(CultureInfo culture)
        {
            CurrentCulture = culture;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        public CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentUICulture;

        // List of supported languages
        public List<LanguageInfo> SupportedLanguages => new()
        {
            new LanguageInfo { Name = "English", Code = "en-US", NativeName = "English" },
            new LanguageInfo { Name = "Portuguese (Brazil)", Code = "pt-BR", NativeName = "Português (Brasil)" },
            new LanguageInfo { Name = "Spanish", Code = "es", NativeName = "Español" },
            new LanguageInfo { Name = "French", Code = "fr", NativeName = "Français" },
            new LanguageInfo { Name = "German", Code = "de", NativeName = "Deutsch" },
            new LanguageInfo { Name = "Chinese (Simplified)", Code = "zh-CN", NativeName = "简体中文" },
            new LanguageInfo { Name = "Japanese", Code = "ja", NativeName = "日本語" },
            new LanguageInfo { Name = "Korean", Code = "ko", NativeName = "한국어" },
            new LanguageInfo { Name = "Arabic", Code = "ar", NativeName = "العربية" },
            new LanguageInfo { Name = "Russian", Code = "ru", NativeName = "Русский" },
            new LanguageInfo { Name = "Hindi", Code = "hi", NativeName = "हिन्दी" }
        };
    }

    public class LanguageInfo
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string NativeName { get; set; }
    }
}
