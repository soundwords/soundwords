using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using SoundWords.Social;

namespace SoundWords.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class EscapedFragmentAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return ActivatorUtilities.CreateInstance<EscapedFragmentFilter>(serviceProvider);
    }
}

public class EscapedFragmentFilter : IActionFilter
{
    private readonly ILogger<EscapedFragmentFilter> _logger;

    public EscapedFragmentFilter(ILogger<EscapedFragmentFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Request.Query.TryGetValue("_escaped_fragment_", out var values))
        {
            return;
        }

        string escapedFragment = Uri.UnescapeDataString(values.ToString());

        _logger.LogDebug("A robot is here! User-Agent={UserAgent} Path={Path}",
                         context.HttpContext.Request.Headers.UserAgent.ToString(),
                         context.HttpContext.Request.Path);

        foreach (object? argument in context.ActionArguments.Values)
        {
            if (argument is IHaveEscapedFragment haveEscapedFragment)
            {
                haveEscapedFragment.EscapedFragment = escapedFragment;
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
