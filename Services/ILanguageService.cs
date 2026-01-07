namespace MyVocaList.Services
{
    public interface ILanguageService
    {
        Task<string> GetUserLanguageAsync();
        Task SetUserLanguageAsync(string languageCode);
        bool IsLanguageSelected();
    }
}
