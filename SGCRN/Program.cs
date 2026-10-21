using SGCRN.Data;
using SGCRN.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<ProductoRepository>();
builder.Services.AddScoped<ProductoTerminadoService>();

var app = builder.Build();

app.UseExceptionHandler("/Home/Error");
app.UseRequestLocalization("es-MX");
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Producto}/{action=Index}/{id?}");

app.Run();
