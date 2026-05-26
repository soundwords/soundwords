using Microsoft.Extensions.Configuration;

namespace SoundWords;

internal static class DockerSecretsConfigurationExtensions
{
    /// <summary>
    /// Loads each file under <paramref name="path"/> as a configuration key/value
    /// pair (file name → key, trimmed content → value). The built-in
    /// <c>KeyPerFile</c> provider leaves the trailing newline that most editors
    /// and <c>docker secret create</c> append, which corrupts connection strings
    /// and API tokens — this one trims it.
    /// </summary>
    public static IConfigurationBuilder AddDockerSecrets(this IConfigurationBuilder builder,
                                                         string path = "/run/secrets")
    {
        if (!Directory.Exists(path))
        {
            return builder;
        }

        Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (string file in Directory.EnumerateFiles(path))
        {
            values[Path.GetFileName(file)] = File.ReadAllText(file).TrimEnd();
        }

        return builder.AddInMemoryCollection(values);
    }
}
