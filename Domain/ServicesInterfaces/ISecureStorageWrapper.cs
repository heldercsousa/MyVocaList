namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>Thin wrapper around SecureStorage to allow unit testing without platform binding.</summary>
public interface ISecureStorageWrapper
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    bool Remove(string key);
}
