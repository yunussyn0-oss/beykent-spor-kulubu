var builder = WebApplication.CreateBuilder(args);

// MVC desteği ekle
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Static dosyalar için (CSS, JS, resimler)
app.UseStaticFiles();

// Routing
app.UseRouting();

// Varsayılan route: /Antrenor/Giris
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Antrenor}/{action=Giris}/{id?}");

// Port ayarı
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();