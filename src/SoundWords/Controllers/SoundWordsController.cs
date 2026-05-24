using Microsoft.AspNetCore.Mvc;

namespace SoundWords.Controllers;

public abstract class SoundWordsController : Controller
{
    protected bool IncludeRestricted => User.Identity?.IsAuthenticated == true;
}
