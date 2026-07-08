using System.Text;
using Mical.Entities;
using Mical.Helpers;
using Mical.Services.Interfaces;
using Mical.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace Mical.Controllers;

/// <summary>
/// Registro, inicio/cierre de sesión, perfil, cambio y recuperación de contraseña.
/// Autenticación por cookies de ASP.NET Identity (sin JWT).
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _email;
    private readonly IEmailTemplateRenderer _templates;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService email,
        IEmailTemplateRenderer templates,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _email = email;
        _templates = templates;
        _logger = logger;
    }

    // ---------- Registro ----------

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterVm());
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> Register(RegisterVm model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, Roles.Usuario);
            _logger.LogInformation("Nuevo usuario registrado: {Email}", model.Email);
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToLocal(returnUrl);
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, Translate(error));

        return View(model);
    }

    // ---------- Login ----------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginVm());
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> Login(LoginVm model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        // lockoutOnFailure: true → cuenta intentos fallidos para el bloqueo configurado.
        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("Inicio de sesión: {Email}", model.Email);

            // Sin returnUrl explícito, los administradores van directo al panel.
            if (string.IsNullOrEmpty(returnUrl))
            {
                var user = await _signInManager.UserManager.FindByEmailAsync(model.Email);
                if (user is not null && await _signInManager.UserManager.IsInRoleAsync(user, Roles.Administrador))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToLocal(returnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Cuenta bloqueada temporalmente: {Email}", model.Email);
            ModelState.AddModelError(string.Empty,
                "La cuenta está bloqueada temporalmente por varios intentos fallidos. Probá de nuevo en unos minutos.");
            return View(model);
        }

        // Mensaje genérico: no revelar si el email existe o no.
        ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
        return View(model);
    }

    // ---------- Logout ----------

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Cierre de sesión.");
        return RedirectToAction("Index", "Home");
    }

    // ---------- Perfil ----------

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction(nameof(Login));

        var model = new ProfileVm
        {
            FullName = user.FullName ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email ?? string.Empty,
            CreatedAt = user.CreatedAt
        };
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction(nameof(Login));

        // Reponemos los campos de solo lectura para el re-render.
        model.Email = user.Email ?? string.Empty;
        model.CreatedAt = user.CreatedAt;

        if (!ModelState.IsValid)
            return View(model);

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["StatusMessage"] = "Perfil actualizado.";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, Translate(error));

        return View(model);
    }

    // ---------- Cambio de contraseña ----------

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordVm());

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordVm model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction(nameof(Login));

        var result = await _userManager.ChangePasswordAsync(
            user, model.CurrentPassword, model.NewPassword);

        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Contraseña cambiada para {Email}.", user.Email);
            TempData["StatusMessage"] = "Contraseña actualizada.";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, Translate(error));

        return View(model);
    }

    // ---------- Recuperación de contraseña ----------

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVm model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null)
        {
            try
            {
                // 1) Identity genera el token; se codifica para viajar en la URL.
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                // 2) URL absoluta de recuperación.
                var resetUrl = Url.Action(
                    nameof(ResetPassword), "Account",
                    new { email = user.Email, token = encodedToken },
                    Request.Scheme)!;

                // 3) Se renderiza la plantilla y 4) se reemplazan los placeholders.
                var html = await _templates.RenderAsync("ForgotPassword", new Dictionary<string, string>
                {
                    ["Name"] = user.FullName ?? "Hola",
                    ["ResetUrl"] = resetUrl
                });

                // 5) Envío desacoplado del proveedor.
                await _email.SendEmailAsync(user.Email!, "Recuperá tu contraseña - Mical", html);
                _logger.LogInformation("Email de recuperación solicitado para {Email}.", user.Email);
            }
            catch (Exception ex)
            {
                // No exponemos el fallo al usuario (anti-enumeración); queda en el log.
                _logger.LogError(ex, "No se pudo enviar el email de recuperación a {Email}.", user.Email);
            }
        }

        // Mensaje genérico siempre: no revelar si el email existe.
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    public IActionResult ResetPassword(string? email = null, string? token = null)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            return RedirectToAction(nameof(Login));

        return View(new ResetPasswordVm { Email = email, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> ResetPassword(ResetPasswordVm model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            // Mismo resultado que un token inválido: no revelamos si el email existe.
            TempData["StatusMessage"] = "Tu contraseña fue restablecida. Ya podés iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        // Identity valida el token (previa decodificación).
        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("Contraseña restablecida para {Email}.", user.Email);
            TempData["StatusMessage"] = "Tu contraseña fue restablecida. Ya podés iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, Translate(error));

        return View(model);
    }

    // ---------- Acceso denegado ----------

    [HttpGet]
    public IActionResult Denied() => View();

    // ---------- Helpers ----------

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    /// <summary>Traduce al español los errores más comunes de Identity.</summary>
    private static string Translate(IdentityError error) => error.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Ya existe una cuenta con ese email.",
        "PasswordTooShort" => "La contraseña es demasiado corta.",
        "PasswordRequiresDigit" => "La contraseña debe incluir al menos un número.",
        "PasswordRequiresLower" => "La contraseña debe incluir al menos una minúscula.",
        "PasswordRequiresUpper" => "La contraseña debe incluir al menos una mayúscula.",
        "PasswordMismatch" => "La contraseña actual es incorrecta.",
        "InvalidToken" => "El enlace de recuperación no es válido o expiró. Pedí uno nuevo.",
        _ => error.Description
    };
}
