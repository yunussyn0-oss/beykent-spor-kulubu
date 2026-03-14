using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using SporKulubu.Data;
using SporKulubu.Models;

var builder = WebApplication.CreateBuilder(args);

// MVC ekle
builder.Services.AddControllersWithViews();

// Authentication ekle
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Antrenor/Giris";
        options.LogoutPath = "/Antrenor/Cikis";
        options.AccessDeniedPath = "/Antrenor/Giris";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

// Veritabanı
var dbPath = Environment.GetEnvironmentVariable("RENDER") != null 
    ? "/tmp/sporkulubu.db" 
    : "sporkulubu.db";

builder.Services.AddDbContext<UygulamaDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ========== VERİTABANI KURULUMU ==========
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UygulamaDbContext>();
    
    try
    {
        Console.WriteLine($"Veritabanı yolu: {dbPath}");
        context.Database.EnsureCreated();
        Console.WriteLine("✅ Veritabanı hazır.");
        
        // ÖRNEK VERİ EKLE (SADECE BOŞSA)
        if (!context.SporSalonlari.Any())
        {
            Console.WriteLine("📌 Örnek veriler ekleniyor...");
            
            // Salonlar
            var salonlar = new List<SporSalonu>
            {
                new SporSalonu { Ad = "Emlak Konut Spor Salonu" },
                new SporSalonu { Ad = "Yakuplu For Life Spor Salonu" },
                new SporSalonu { Ad = "Neşe Sever Spor Salonu" }
            };
            context.SporSalonlari.AddRange(salonlar);
            context.SaveChanges();
            
            // Antrenörler
            var antrenorler = new List<Antrenor>
            {
                new Antrenor { AdSoyad = "Burhan Şayan", Email = "burhan@beykentspor.com", Sifre = "123456", Uzmanlik = "Tam Yetki", Telefon = "0532 111 2233", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Ertan Tuncel", Email = "ertan@beykentspor.com", Sifre = "123456", Uzmanlik = "Tam Yetki", Telefon = "0532 222 3344", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Özgür", Email = "ozgur@beykentspor.com", Sifre = "123456", Uzmanlik = "Yakuplu", Telefon = "0532 333 4455", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Sezer", Email = "sezer@beykentspor.com", Sifre = "123456", Uzmanlik = "Emlak Konut", Telefon = "0532 444 5566", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Nesrin", Email = "nesrin@beykentspor.com", Sifre = "123456", Uzmanlik = "Neşe Sever", Telefon = "0532 555 6677", KayitTarihi = DateTime.Now }
            };
            context.Antrenorler.AddRange(antrenorler);
            context.SaveChanges();
            
            // Yetkilendirme
            var yakuplu = salonlar.First(s => s.Ad.Contains("Yakuplu"));
            var emlak = salonlar.First(s => s.Ad.Contains("Emlak"));
            var nese = salonlar.First(s => s.Ad.Contains("Neşe"));
            
            var ozgur = antrenorler.First(a => a.Email == "ozgur@beykentspor.com");
            var sezer = antrenorler.First(a => a.Email == "sezer@beykentspor.com");
            var nesrin = antrenorler.First(a => a.Email == "nesrin@beykentspor.com");
            
            context.AntrenorTakimlar.AddRange(
                new AntrenorTakim { AntrenorId = ozgur.Id, SporSalonuId = yakuplu.Id, AtanmaTarihi = DateTime.Now },
                new AntrenorTakim { AntrenorId = sezer.Id, SporSalonuId = emlak.Id, AtanmaTarihi = DateTime.Now },
                new AntrenorTakim { AntrenorId = nesrin.Id, SporSalonuId = nese.Id, AtanmaTarihi = DateTime.Now }
            );
            context.SaveChanges();
            
            Console.WriteLine("🎉 Örnek veriler eklendi!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Veritabanı hatası: {ex.Message}");
    }
}

// Middleware
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Antrenor}/{action=Giris}/{id?}");

// Port
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();