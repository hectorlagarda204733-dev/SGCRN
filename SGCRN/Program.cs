using SGCRN.Data;
using SGCRN.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<ProductoTerminadoService>();

var app = builder.Build();

app.UseExceptionHandler("/Error");
app.UseRequestLocalization("es-MX");
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapGet("/", () => Results.Redirect("/ProductosTerminados"));

app.Run();
