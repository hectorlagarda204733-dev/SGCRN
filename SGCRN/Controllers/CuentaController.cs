using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SGCRN.Models;
using SGCRN.Services;

namespace SGCRN.Controllers;

[AutoValidateAntiforgeryToken]
public class CuentaController(AutenticacionService service) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Producto");
        return View(new LoginInput { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
    }

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost]
    public async Task<IActionResult> Login(LoginInput input)
    {
        if (ModelState.IsValid)
        {
            var usuario = await service.ValidarAsync(input.Usuario, input.Password);
            if (usuario is not null)
            {
                var identity = new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                    new Claim(ClaimTypes.Role, usuario.Rol)
                ], CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = false });
                return Url.IsLocalUrl(input.ReturnUrl)
                    ? LocalRedirect(input.ReturnUrl!) : RedirectToAction("Index", "Producto");
            }
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
        }
        input.Password = "";
        return View(input);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
