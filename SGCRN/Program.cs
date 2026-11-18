using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;
using SGCRN.Data;
using SGCRN.Models;
using SGCRN.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.None }));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Cuenta/Login";
    options.AccessDeniedPath = "/Cuenta/Login";
    options.Cookie.Name = "SGCRN.Sesion";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
    // Cloudflare usa Host local hacia el origen; las redirecciones deben conservar
    // el dominio que está viendo el navegador.
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Redirect(new Uri(context.RedirectUri).PathAndQuery);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.Redirect(new Uri(context.RedirectUri).PathAndQuery);
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole("Administrador").Build());
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Solo proxies locales de confianza (valores predeterminados); Cloudflare termina TLS.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.OnRejected = async (context, cancellation) =>
        await context.HttpContext.Response.WriteAsync("Demasiados intentos. Espera un minuto e intenta de nuevo.", cancellation);
});
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<AutenticacionService>();
builder.Services.AddDbContext<SGCRNDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("SGCRN")
        ?? throw new InvalidOperationException("Configure la conexión SGCRN en User Secrets.")));
builder.Services.AddScoped<ProductoRepository>();
builder.Services.AddScoped<ProductoTerminadoService>();
builder.Services.AddScoped<ImagenProductoService>();
builder.Services.AddScoped<MateriaPrimaRepository>();
builder.Services.AddScoped<MateriaPrimaService>();
builder.Services.AddScoped<EntradaInventarioRepository>();
builder.Services.AddScoped<InventarioService>();
builder.Services.AddScoped<ClientePuntoVentaRepository>();
builder.Services.AddScoped<ClientePuntoVentaService>();
builder.Services.AddScoped<SalidaDistribucionRepository>();
builder.Services.AddScoped<SalidaDistribucionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<AutenticacionService>().PrepararAdministradorAsync();

app.UseForwardedHeaders();
app.UseExceptionHandler("/Home/Error");
app.UseRequestLocalization("es-MX");
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads") && context.User.Identity?.IsAuthenticated != true)
    {
        await context.ChallengeAsync();
        return;
    }
    if (context.Request.Path.StartsWithSegments("/uploads"))
        context.Response.Headers.CacheControl = "private, no-store";
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Producto}/{action=Index}/{id?}");

app.Run();
