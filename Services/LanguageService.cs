using System.Globalization;
using Microsoft.Maui.Storage;

namespace MyVocaList.Services
{
    public class LanguageService : ILanguageService
    {
        private const string PreferenceKey = "UserLanguage";

        public Task<string> GetUserLanguageAsync()
        {
            var language = Preferences.Get(PreferenceKey, string.Empty);
            return Task.FromResult(!string.IsNullOrEmpty(language) ? language : CultureInfo.CurrentCulture.Name);
        }

        public Task SetUserLanguageAsync(string languageCode)
        {
            Preferences.Set(PreferenceKey, languageCode);
            return Task.CompletedTask;
        }

        public bool IsLanguageSelected() => Preferences.ContainsKey(PreferenceKey);
    }
}
