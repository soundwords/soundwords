using Microsoft.AspNetCore.Mvc;

namespace SoundWords.ViewComponents;

public abstract class ViewComponentBase : ViewComponent
{
    public virtual bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
}
