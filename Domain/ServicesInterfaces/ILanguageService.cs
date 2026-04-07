namespace MyVocaList.Domain.ServicesInterfaces
{
    public interface ILanguageService
    {
        Task<string> GetUserLanguageAsync();
        Task SetUserLanguageAsync(string languageCode);
        bool IsLanguageSelected();
    }
}
