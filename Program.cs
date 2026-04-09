using Microsoft.EntityFrameworkCore;
using SporKulubu.Data;

var builder = WebApplication.CreateBuilder(args);

// MVC ekle
builder.Services.AddControllersWithViews();

// Session ekle
builder.Services.AddSession();

// Veritabanı - SQLite
builder.Services.AddDbContext<UygulamaDbContext>(options =>
    options.UseSqlite("Data Source=sporkulubu.db"));

var app = builder.Build();

// Middleware
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Antrenor}/{action=Giris}/{id?}");

app.Run();