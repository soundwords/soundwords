using Microsoft.AspNetCore.Mvc.Razor;

namespace SoundWords;

public abstract class ViewBase : ViewBase<object>
{
}

public abstract class ViewBase<T> : RazorPage<T>
{
    private ISoundWordsConfiguration? _configuration;

    protected ISoundWordsConfiguration Configuration =>
        _configuration ??= (ISoundWordsConfiguration)ViewContext.HttpContext.RequestServices.GetService(typeof(ISoundWordsConfiguration))!;

    public bool IsAuthenticated => ViewContext.HttpContext.User.Identity?.IsAuthenticated == true;
}
