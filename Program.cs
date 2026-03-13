using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using SporKulubu.Data;
using SporKulubu.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Authentication ekleyelim (Cookie tabanlı)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Antrenor/Giris";
        options.LogoutPath = "/Antrenor/Cikis";
        options.AccessDeniedPath = "/Antrenor/Giris";
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
        Console.WriteLine($"Veritabanı yolu: {dbPath}");
        context.Database.EnsureCreated();
        Console.WriteLine("✅ Veritabanı oluşturuldu veya zaten mevcut.");
        
        // SADECE TABLOLAR BOŞSA VERİ EKLE
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

            // 2. BRANŞLAR
            foreach (var salon in salonlar)
            {
                context.Branslar.Add(new Brans { Ad = "Voleybol", TakimVarMi = true, GrupVarMi = true, SporSalonuId = salon.Id });
            }
            context.SaveChanges();

            // 3. TAKIMLAR
            var voleybolBranslari = context.Branslar.Where(b => b.Ad == "Voleybol").ToList();
            foreach (var brans in voleybolBranslari)
            {
                context.Takimlar.AddRange(new List<Takim>
                {
                    new Takim { Ad = "Mini Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "Midi Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "Küçük Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "Yıldız Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id }
                });
            }
            context.SaveChanges();

            // 4. GRUPLAR
            string[] grupHarfleri = { "A", "B", "C", "D", "E" };
            foreach (var takim in context.Takimlar.Where(t => t.Brans != null && t.Brans.Ad == "Voleybol"))
            {
                foreach (var harf in grupHarfleri)
                {
                    context.Gruplar.Add(new Grup { Ad = $"{harf} Grubu", TakimId = takim.Id });
                }
            }
            context.SaveChanges();

            // 5. YAKUPLU'YA BASKETBOL
            var yakuplu = context.SporSalonlari.FirstOrDefault(s => s.Ad.Contains("Yakuplu"));
            if (yakuplu != null)
            {
                var basketbol = new Brans { Ad = "Basketbol", TakimVarMi = true, GrupVarMi = true, SporSalonuId = yakuplu.Id };
                context.Branslar.Add(basketbol);
                context.SaveChanges();

                var basketbolTakimlari = new List<Takim>
                {
                    new Takim { Ad = "U8 Takım", SporSalonuId = yakuplu.Id, BransId = basketbol.Id },
                    new Takim { Ad = "U10 Takım", SporSalonuId = yakuplu.Id, BransId = basketbol.Id },
                    new Takim { Ad = "U12 Takım", SporSalonuId = yakuplu.Id, BransId = basketbol.Id },
                    new Takim { Ad = "U14 Takım", SporSalonuId = yakuplu.Id, BransId = basketbol.Id }
                };
                context.Takimlar.AddRange(basketbolTakimlari);
                context.SaveChanges();

                foreach (var takim in basketbolTakimlari)
                {
                    foreach (var harf in grupHarfleri)
                    {
                        context.Gruplar.Add(new Grup { Ad = $"{harf} Grubu", TakimId = takim.Id });
                    }
                }
                context.SaveChanges();

                context.Branslar.Add(new Brans { Ad = "Atletik Performans", TakimVarMi = false, GrupVarMi = false, SporSalonuId = yakuplu.Id });
                context.SaveChanges();
            }

            // 6. ANTRENÖRLER
            var antrenorler = new List<Antrenor>
            {
                new Antrenor { AdSoyad = "Burhan Şayan", Email = "burhan@beykentspor.com", Sifre = "123456", Telefon = "0532 111 2233", Uzmanlik = "Tam Yetki", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Ertan Tuncel", Email = "ertan@beykentspor.com", Sifre = "123456", Telefon = "0532 222 3344", Uzmanlik = "Tam Yetki", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Özgür", Email = "ozgur@beykentspor.com", Sifre = "123456", Telefon = "0532 333 4455", Uzmanlik = "Yakuplu For Life", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Sezer", Email = "sezer@beykentspor.com", Sifre = "123456", Telefon = "0532 444 5566", Uzmanlik = "Emlak Konut", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Nesrin", Email = "nesrin@beykentspor.com", Sifre = "123456", Telefon = "0532 555 6677", Uzmanlik = "Neşe Sever", KayitTarihi = DateTime.Now }
            };
            context.Antrenorler.AddRange(antrenorler);
            context.SaveChanges();

            // 7. YETKİ ATAMALARI
            var atamalar = new List<AntrenorTakim>();
            var ozgurDb = context.Antrenorler.FirstOrDefault(a => a.Email == "ozgur@beykentspor.com");
            var sezerDb = context.Antrenorler.FirstOrDefault(a => a.Email == "sezer@beykentspor.com");
            var nesrinDb = context.Antrenorler.FirstOrDefault(a => a.Email == "nesrin@beykentspor.com");

            if (ozgurDb != null && yakuplu != null)
                atamalar.Add(new AntrenorTakim { AntrenorId = ozgurDb.Id, SporSalonuId = yakuplu.Id, AtanmaTarihi = DateTime.Now });

            if (sezerDb != null)
            {
                var emlak = context.SporSalonlari.FirstOrDefault(s => s.Ad.Contains("Emlak"));
                if (emlak != null)
                    atamalar.Add(new AntrenorTakim { AntrenorId = sezerDb.Id, SporSalonuId = emlak.Id, AtanmaTarihi = DateTime.Now });
            }

            if (nesrinDb != null)
            {
                var nese = context.SporSalonlari.FirstOrDefault(s => s.Ad.Contains("Neşe"));
                if (nese != null)
                    atamalar.Add(new AntrenorTakim { AntrenorId = nesrinDb.Id, SporSalonuId = nese.Id, AtanmaTarihi = DateTime.Now });
            }

            if (atamalar.Any())
            {
                context.AntrenorTakimlar.AddRange(atamalar);
                context.SaveChanges();
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Render'da HttpsRedirection KAPALI!
    // app.UseHttpsRedirection();
}
else
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Antrenor}/{action=Giris}/{id?}");

// PORT ayarı - Render için
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();