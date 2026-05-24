using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SoundWords.Tests;

public sealed class SoundWordsApp : IAsyncDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"soundwords-test-{Guid.NewGuid():N}.db");
    private readonly Dictionary<string, string?> _previousEnvironment = new();

    public WebApplicationFactory<Program> Factory { get; }

    public SoundWordsApp()
    {
        string connectionString = $"Data Source={_dbPath}";

        SetEnv("DB_TYPE", "SQLite");
        SetEnv("CONNECTION_STRING", connectionString);
        SetEnv("CONNECTION_STRING_USERS", connectionString);
        SetEnv("SITE_URL", "http://localhost");
        SetEnv("SITE_NAME", "SoundWords (test)");
        SetEnv("CACHE_PATH", Path.Combine(Path.GetTempPath(), "soundwords-test-cache"));
        SetEnv("RECORDINGS_FOLDER", Path.Combine(Path.GetTempPath(), "soundwords-test-recordings"));
        SetEnv("RECORDINGS_FOLDER_RESTRICTED", Path.Combine(Path.GetTempPath(), "soundwords-test-restricted"));
        SetEnv("CUSTOM_FOLDER", Path.Combine(Path.GetTempPath(), "soundwords-test-custom"));

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    private void SetEnv(string key, string? value)
    {
        _previousEnvironment[key] = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, value);
    }

    public ValueTask DisposeAsync()
    {
        Factory.Dispose();
        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch
            {
            }
        }

        foreach ((string key, string? value) in _previousEnvironment)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
        return ValueTask.CompletedTask;
    }
}
