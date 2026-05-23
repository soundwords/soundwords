using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SoundWords.Auth;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace SoundWords.Controllers;

public class AccountController : SoundWordsController
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet("/Login")]
    [HttpGet("/Account/Login")]
    public IActionResult Login([FromQuery] string? returnUrl)
    {
        return View(new LoginRequest { ReturnUrl = returnUrl });
    }

    [HttpPost("/Login")]
    [HttpPost("/Account/Login")]
    public async Task<IActionResult> Login(LoginRequest model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        SignInResult result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password,
                                                                      model.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Brukernavn eller passord er feil.");
            return View(model);
        }

        return RedirectToLocal(model.ReturnUrl ?? model.Continue);
    }

    [HttpGet("/Account/Register")]
    public IActionResult Register([FromQuery] string? returnUrl)
    {
        return View(new RegisterRequest { ReturnUrl = returnUrl });
    }

    [HttpPost("/Account/Register")]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string[] nameParts = model.DisplayName.Split(' ');
        string firstName = string.Join(' ', nameParts.Take(nameParts.Length - 1));
        string lastName = nameParts.Last();

        ApplicationUser user = new()
                               {
                                   UserName = model.Email,
                                   Email = model.Email,
                                   DisplayName = model.DisplayName,
                                   FirstName = firstName,
                                   LastName = lastName
                               };

        IdentityResult result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpPost("/Account/Logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl)
            && Url.IsLocalUrl(returnUrl)
            && !PointsAtAccount(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return Redirect("/");
    }

    private static bool PointsAtAccount(string returnUrl)
    {
        // Strip the query string so '/Account/Login?...' is matched too.
        int queryStart = returnUrl.IndexOf('?');
        ReadOnlySpan<char> path = queryStart >= 0 ? returnUrl.AsSpan(0, queryStart) : returnUrl.AsSpan();
        return path.Equals("/Login", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/Login/", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/Account/Login/", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/Account/Register", StringComparison.OrdinalIgnoreCase);
    }
}
