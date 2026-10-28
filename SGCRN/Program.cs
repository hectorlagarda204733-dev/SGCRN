using Microsoft.EntityFrameworkCore;
using SGCRN.Data;
using SGCRN.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<SGCRNDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("SGCRN")
        ?? throw new InvalidOperationException("Configure la conexión SGCRN en User Secrets.")));
builder.Services.AddScoped<ProductoRepository>();
builder.Services.AddScoped<ProductoTerminadoService>();
builder.Services.AddScoped<MateriaPrimaRepository>();
builder.Services.AddScoped<MateriaPrimaService>();
builder.Services.AddScoped<EntradaInventarioRepository>();
builder.Services.AddScoped<InventarioService>();

var app = builder.Build();

app.UseExceptionHandler("/Home/Error");
app.UseRequestLocalization("es-MX");
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Producto}/{action=Index}/{id?}");

app.Run();
