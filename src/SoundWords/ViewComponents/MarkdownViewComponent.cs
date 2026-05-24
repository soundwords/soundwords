using Microsoft.AspNetCore.Mvc;
using SoundWords.Tools;

namespace SoundWords.ViewComponents;

public class MarkdownViewComponent : ViewComponentBase
{
    private readonly IMarkdownTool _markdownTool;

    public MarkdownViewComponent(IMarkdownTool markdownTool)
    {
        _markdownTool = markdownTool;
    }

    public IViewComponentResult Invoke(string key)
    {
        string? content = _markdownTool.Get(key);
        return View(new Markdown { Content = content });
    }

    public class Markdown
    {
        public string? Content { get; set; }
    }
}
