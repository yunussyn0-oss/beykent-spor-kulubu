using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using SporKulubu.Data;
using SporKulubu.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Authentication ekleyelim (COOKIE tabanlı)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Antrenor/Giris";
        options.LogoutPath = "/Antrenor/Logout";
        options.AccessDeniedPath = "/Antrenor/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// Database configuration - Render'da /tmp klasörünü kullan
var dbPath = Environment.GetEnvironmentVariable("RENDER") != null 
    ? "/tmp/sporkulubu.db"  // Render'da geçici dizin (yazılabilir)
    : "sporkulubu.db";       // Lokalde normal

builder.Services.AddDbContext<UygulamaDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ========== VERİTABANINI OTOMATİK OLUŞTUR VE ÖRNEK VERİLERİ EKLE ==========
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UygulamaDbContext>();
    
    try
    {
        // Veritabanı dosyasının yolunu kontrol et
        Console.WriteLine($"Veritabanı yolu: {dbPath}");
        
        // Veritabanı yoksa oluştur
        context.Database.EnsureCreated();
        Console.WriteLine("✅ Veritabanı oluşturuldu veya zaten mevcut.");
        
        // Eğer salonlar tablosu boşsa, örnek verileri ekle
        if (!context.SporSalonlari.Any())
        {
            Console.WriteLine("📌 Örnek veriler ekleniyor...");

            // 1. SPOR SALONLARI
            var salonlar = new List<SporSalonu>
            {
                new SporSalonu { Ad = "Emlak Konut Spor Salonu" },
                new SporSalonu { Ad = "Yakuplu For Life Spor Salonu" },
                new SporSalonu { Ad = "Neşe Sever Spor Salonu" }
            };
            context.SporSalonlari.AddRange(salonlar);
            context.SaveChanges();
            Console.WriteLine("✅ 3 spor salonu eklendi.");

            // 2. VOLEYBOL BRANŞLARI
            foreach (var salon in salonlar)
            {
                var voleybol = new Brans
                {
                    Ad = "Voleybol",
                    TakimVarMi = true,
                    GrupVarMi = true,
                    SporSalonuId = salon.Id
                };
                context.Branslar.Add(voleybol);
            }
            context.SaveChanges();
            Console.WriteLine("✅ Voleybol branşları eklendi.");

            // 3. VOLEYBOL TAKIMLARI
            var voleybolBranslari = context.Branslar.Where(b => b.Ad == "Voleybol").ToList();
            foreach (var brans in voleybolBranslari)
            {
                var takimlar = new List<Takim>
                {
                    new Takim { Ad = "Mini Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "Midi Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "Küçük Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "Yıldız Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id }
                };
                context.Takimlar.AddRange(takimlar);
            }
            context.SaveChanges();
            Console.WriteLine("✅ Voleybol takımları eklendi.");

            // 4. VOLEYBOL GRUPLARI
            var takimList = context.Takimlar.Where(t => t.Brans != null && t.Brans.Ad == "Voleybol").ToList();
            string[] grupHarfleri = { "A", "B", "C", "D", "E" };
            foreach (var takim in takimList)
            {
                foreach (var harf in grupHarfleri)
                {
                    context.Gruplar.Add(new Grup 
                    { 
                        Ad = $"{harf} Grubu", 
                        TakimId = takim.Id 
                    });
                }
            }
            context.SaveChanges();
            Console.WriteLine("✅ Voleybol grupları eklendi.");

            // 5. YAKUPLU'YA BASKETBOL BRANŞI
            var yakuplu = context.SporSalonlari.FirstOrDefault(s => s.Ad.Contains("Yakuplu"));
            if (yakuplu != null)
            {
                var basketbol = new Brans
                {
                    Ad = "Basketbol",
                    TakimVarMi = true,
                    GrupVarMi = true,
                    SporSalonuId = yakuplu.Id
                };
                context.Branslar.Add(basketbol);
                context.SaveChanges();
                Console.WriteLine("✅ Basketbol branşı eklendi.");

                var basketbolTakimlari = new List<Takim>
                {
                    new Takim { Ad = "U8 Takım", SporSalonuId = yakuplu.Id, BransId = basketbol.Id },
                    new Takim { Ad = "U10 Takım", SporSalonuId = yakuplu.Id, BransId = basketbol.Id },
                    new Takim { Ad = "U12 Takım", SporSalonuId = yakuplu.Id, BransId = basketbol.Id },
                    new Takim { Ad = "U14 Takım", SporSalonuId = yakuplu.Id, BransId = basketbol.Id }
                };
                context.Takimlar.AddRange(basketbolTakimlari);
                context.SaveChanges();
                Console.WriteLine("✅ Basketbol takımları eklendi.");

                foreach (var takim in basketbolTakimlari)
                {
                    foreach (var harf in grupHarfleri)
                    {
                        context.Gruplar.Add(new Grup 
                        { 
                            Ad = $"{harf} Grubu", 
                            TakimId = takim.Id 
                        });
                    }
                }
                context.SaveChanges();
                Console.WriteLine("✅ Basketbol grupları eklendi.");

                var atletik = new Brans
                {
                    Ad = "Atletik Performans",
                    TakimVarMi = false,
                    GrupVarMi = false,
                    SporSalonuId = yakuplu.Id
                };
                context.Branslar.Add(atletik);
                context.SaveChanges();
                Console.WriteLine("✅ Atletik Performans branşı eklendi.");
            }

            // 6. ÖRNEK ANTRENÖRLER
            if (!context.Antrenorler.Any())
            {
                var antrenorler = new List<Antrenor>
                {
                    new Antrenor 
                    { 
                        Email = "burhan@beykentspor.com", 
                        Sifre = "123456", 
                        AdSoyad = "Burhan Şayan",
                        Telefon = "0532 111 2233",
                        Uzmanlik = "Tam Yetki",
                        KayitTarihi = DateTime.Now
                    },
                    new Antrenor 
                    { 
                        Email = "ertan@beykentspor.com", 
                        Sifre = "123456", 
                        AdSoyad = "Ertan Tuncel",
                        Telefon = "0532 222 3344",
                        Uzmanlik = "Tam Yetki",
                        KayitTarihi = DateTime.Now
                    },
                    new Antrenor 
                    { 
                        Email = "ozgur@beykentspor.com", 
                        Sifre = "123456", 
                        AdSoyad = "Özgür",
                        Telefon = "0532 333 4455",
                        Uzmanlik = "Yakuplu For Life",
                        KayitTarihi = DateTime.Now
                    },
                    new Antrenor 
                    { 
                        Email = "sezer@beykentspor.com", 
                        Sifre = "123456", 
                        AdSoyad = "Sezer",
                        Telefon = "0532 444 5566",
                        Uzmanlik = "Emlak Konut",
                        KayitTarihi = DateTime.Now
                    },
                    new Antrenor 
                    { 
                        Email = "nesrin@beykentspor.com", 
                        Sifre = "123456", 
                        AdSoyad = "Nesrin",
                        Telefon = "0532 555 6677",
                        Uzmanlik = "Neşe Sever",
                        KayitTarihi = DateTime.Now
                    }
                };
                context.Antrenorler.AddRange(antrenorler);
                context.SaveChanges();
                Console.WriteLine("✅ Örnek antrenörler eklendi.");
            }

            Console.WriteLine("🎉 Tüm örnek veriler başarıyla eklendi!");
        }
        else
        {
            Console.WriteLine("ℹ️ Veritabanı zaten dolu, yeni veri eklenmedi.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Veritabanı hatası: {ex.Message}");
    }
}
// ========== VERİTABANI KURULUMU SONU ==========

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Render'da HttpsRedirection KAPALI!
    // app.UseHttpsRedirection();
}
else
{
    app.UseHttpsRedirection(); // Sadece development'da açık
}

app.UseStaticFiles();

app.UseRouting();

// Sıralama önemli!
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Antrenor}/{action=Giris}/{id?}");

// PORT ayarı - Render için çok önemli!
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();