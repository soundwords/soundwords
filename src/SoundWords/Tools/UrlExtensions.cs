namespace SoundWords.Tools;

public static class UrlExtensions
{
    public static string UrlEncode20(this string? value)
    {
        return value == null ? string.Empty : Uri.EscapeDataString(value);
    }

    public static bool HasTrailingSlash(this string? rawUrl)
    {
        if (string.IsNullOrEmpty(rawUrl)) return true;
        string[] urlParts = rawUrl.Split('?');
        return urlParts[0].EndsWith('/');
    }

    public static string WithTrailingSlash(this string? rawUrl)
    {
        if (string.IsNullOrEmpty(rawUrl)) return "/";
        string[] urlParts = rawUrl.Split('?');
        string path = urlParts[0];
        if (!path.EndsWith('/')) path += "/";
        return urlParts.Length > 1 ? $"{path}?{string.Join("?", urlParts.Skip(1))}" : path;
    }

    public static string AddQueryParam(this string url, string name, string value)
    {
        char separator = url.Contains('?') ? '&' : '?';
        return $"{url}{separator}{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";
    }
}
