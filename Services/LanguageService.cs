using MyVocaList.Infra.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using MyVocaList.Domain;

namespace MyVocaList.Services
{
    public class LanguageService : ILanguageService
    {
        private readonly AppDbContext _dbContext;
        private const string PreferenceKey = "UserLanguage";

        public LanguageService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> GetUserLanguageAsync()
        {
            // Check app preferences first
            var language = Preferences.Get(PreferenceKey, string.Empty);

            if (!string.IsNullOrEmpty(language))
                return language;

            // If not found, check database
            var setting = await _dbContext.ConfiguracoesSistema
                .FirstOrDefaultAsync(c => c.Chave == PreferenceKey);

            return setting?.Valor ?? CultureInfo.CurrentCulture.Name;
        }

        public async Task SetUserLanguageAsync(string languageCode)
        {
            // Save to preferences for quick access
            Preferences.Set(PreferenceKey, languageCode);

            // Save to database for persistence and backup
            var setting = await _dbContext.ConfiguracoesSistema
                .FirstOrDefaultAsync(c => c.Chave == PreferenceKey);

            if (setting == null)
            {
                setting = new ConfiguracaoSistema
                {
                    Chave = PreferenceKey,
                    Valor = languageCode
                };
                _dbContext.ConfiguracoesSistema.Add(setting);
            }
            else
            {
                setting.Valor = languageCode;
            }

            await _dbContext.SaveChangesAsync();
        }

        public bool IsLanguageSelected()
        {
            // Check if user has already selected a language
            return Preferences.ContainsKey(PreferenceKey);
        }
    }
}
