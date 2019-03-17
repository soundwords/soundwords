using Microsoft.AspNetCore.Mvc;
using SoundWords.Tools;

namespace SoundWords.ViewComponents
{
    public class MarkdownViewComponent : ViewComponentBase
    {
        private readonly IMarkdownTool _markdownTool;

        public MarkdownViewComponent(IMarkdownTool markdownTool)
        {
            _markdownTool = markdownTool;
        }

        public IViewComponentResult Invoke(string name)
        {
            string content = _markdownTool.Get(name);
          
            return View(new Markdown {Content = content});
        }

        public class Markdown
        {
            public string Content { get; set; }
        }
    }
}
