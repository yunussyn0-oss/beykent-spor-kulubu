var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapGet("/", () => "Beykent Spor Uygulaması Çalışıyor!");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Antrenor}/{action=Giris}/{id?}");

app.Run();