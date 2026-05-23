using System.ComponentModel.DataAnnotations;

namespace SoundWords.Auth;

public class RegisterRequest
{
    [Required]
    [Display(Name = "Navn")]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Epostadresse")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Passord")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Bekreft passord")]
    [Compare(nameof(Password), ErrorMessage = "Passordet og bekreftelsespassordet stemmer ikke overens.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    [Display(Name = "Epostadresse")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Passord")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Husk meg?")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
