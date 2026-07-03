using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Compost.Contexts.Services;

/// <summary>
/// Secure storage for Git credentials using ASP.NET Core Data Protection.
/// Stores encrypted tokens outside the YesSql content store.
/// </summary>
public interface IGitSecretStore
{
    Task<string?> GetTokenAsync();
    Task SetTokenAsync(string? token);
}

public class GitSecretStore : IGitSecretStore
{
    private readonly IDataProtector _dataProtector;
    private readonly ILogger<GitSecretStore> _logger;
    private readonly string _storagePath;

    public GitSecretStore(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<GitSecretStore> logger)
    {
        _dataProtector = dataProtectionProvider.CreateProtector("Compost.GitCredentials");
        _logger = logger;
        // Store in App_Data folder, outside YesSql
        var appData = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
        _storagePath = Path.Combine(appData, "Compost", "git-secrets.json");
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            if (!File.Exists(_storagePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(_storagePath);
            var container = JsonSerializer.Deserialize<SecretContainer>(json);
            
            if (container?.ProtectedToken == null)
            {
                return null;
            }

            return _dataProtector.Unprotect(container.ProtectedToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve or decrypt Git token");
            return null;
        }
    }

    public async Task SetTokenAsync(string? token)
    {
        try
        {
            var dir = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var container = new SecretContainer
            {
                ProtectedToken = token != null ? _dataProtector.Protect(token) : null
            };

            var json = JsonSerializer.Serialize(container);
            await File.WriteAllTextAsync(_storagePath, json);
            
            _logger.LogInformation("Git token stored securely");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store Git token");
            throw;
        }
    }

    private class SecretContainer
    {
        public string? ProtectedToken { get; set; }
    }
}
