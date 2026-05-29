using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.UI.Services;

/// <summary>Delegates to MAUI SecureStorage. Registered in MauiProgram.cs for the MAUI project.</summary>
public class SecureStorageWrapper : ISecureStorageWrapper
{
    /// <inheritdoc />
    public Task<string?> GetAsync(string key) => SecureStorage.GetAsync(key);

    /// <inheritdoc />
    public Task SetAsync(string key, string value) => SecureStorage.SetAsync(key, value);

    /// <inheritdoc />
    public bool Remove(string key) => SecureStorage.Remove(key);
}
