using Microsoft.EntityFrameworkCore;
using SporKulubu.Data;
using SporKulubu.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Session services - Kalıcı oturum için
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.MaxAge = TimeSpan.FromDays(30);
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ========== KALICI VERİTABANI (Render Disk veya Lokal) ==========
string dbPath;
if (Directory.Exists("/data"))
{
    dbPath = Path.Combine("/data", "sporkulubu.db");
    Console.WriteLine("Render Disk kullanılıyor: " + dbPath);
}
else
{
    dbPath = "sporkulubu.db";
    Console.WriteLine("Lokal veritabanı kullanılıyor: " + dbPath);
}

builder.Services.AddDbContext<UygulamaDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

// ========== VERİTABANINI OTOMATİK OLUŞTUR VE ÖRNEK VERİLERİ EKLE ==========
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UygulamaDbContext>();
    
    try
    {
        // Veritabanını oluştur
        context.Database.EnsureCreated();
        Console.WriteLine("✅ Veritabanı oluşturuldu veya zaten mevcut.");
        
        // ========== SPOR SALONLARI ==========
        if (!context.SporSalonlari.Any())
        {
            Console.WriteLine("📌 Spor salonları ekleniyor...");
            context.SporSalonlari.AddRange(
                new SporSalonu { Ad = "Emlak Konut Spor Salonu" },
                new SporSalonu { Ad = "Yakuplu For Life Spor Salonu" },
                new SporSalonu { Ad = "Neşe Sever Spor Salonu" }
            );
            context.SaveChanges();
            Console.WriteLine("✅ 3 spor salonu eklendi.");
        }
        
        // ========== BRANŞLAR ==========
        if (!context.Branslar.Any())
        {
            Console.WriteLine("📌 Branşlar ekleniyor...");
            var salonlar = context.SporSalonlari.ToList();
            foreach (var salon in salonlar)
            {
                context.Branslar.Add(new Brans { Ad = "Voleybol", TakimVarMi = true, GrupVarMi = true, SporSalonuId = salon.Id });
                context.Branslar.Add(new Brans { Ad = "Basketbol", TakimVarMi = true, GrupVarMi = true, SporSalonuId = salon.Id });
                context.Branslar.Add(new Brans { Ad = "Atletik Performans", TakimVarMi = false, GrupVarMi = false, SporSalonuId = salon.Id });
            }
            context.SaveChanges();
            Console.WriteLine("✅ Branşlar eklendi.");
        }
        
        // ========== TAKIMLAR ==========
        if (!context.Takimlar.Any())
        {
            Console.WriteLine("📌 Takımlar ekleniyor...");
            var branslar = context.Branslar.Where(b => b.TakimVarMi == true).ToList();
            foreach (var brans in branslar)
            {
                context.Takimlar.AddRange(
                    new Takim { Ad = "U8 Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "U10 Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "U12 Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "U14 Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "U16 Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id }
                );
            }
            context.SaveChanges();
            Console.WriteLine("✅ Takımlar eklendi.");
        }
        
        // ========== GRUPLAR ==========
        if (!context.Gruplar.Any())
        {
            Console.WriteLine("📌 Gruplar ekleniyor...");
            string[] grupHarfleri = { "A", "B", "C", "D", "E" };
            var takimlar = context.Takimlar.ToList();
            foreach (var takim in takimlar)
            {
                foreach (var harf in grupHarfleri)
                {
                    context.Gruplar.Add(new Grup { Ad = harf + " Grubu", TakimId = takim.Id });
                }
            }
            context.SaveChanges();
            Console.WriteLine("✅ Gruplar eklendi.");
        }
        
        // ========== ANTRENÖRLER (YENİ MAİLLERLE) ==========
        if (!context.Antrenorler.Any())
        {
            Console.WriteLine("📌 Antrenörler ekleniyor...");
            context.Antrenorler.AddRange(
                new Antrenor { AdSoyad = "Burhan Şayan", Email = "burhansayan@bbsk.com", Sifre = "123456", Telefon = "0532 111 2233", Uzmanlik = "Baş Antrenör", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Ertan Tuncel", Email = "ertantuncel@bbsk.com", Sifre = "123456", Telefon = "0532 222 3344", Uzmanlik = "Antrenör", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Özgür Demir", Email = "ozgurdemir@bbsk.com", Sifre = "123456", Telefon = "0532 333 4455", Uzmanlik = "Yakuplu Sorumlusu", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Sezer Yılmaz", Email = "sezeryilmaz@bbsk.com", Sifre = "123456", Telefon = "0532 444 5566", Uzmanlik = "Emlak Konut Sorumlusu", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Nesrin Kaya", Email = "nesrinkaya@bbsk.com", Sifre = "123456", Telefon = "0532 555 6677", Uzmanlik = "Neşe Sever Sorumlusu", KayitTarihi = DateTime.Now }
            );
            context.SaveChanges();
            Console.WriteLine("✅ Antrenörler eklendi.");
        }
        
        // ========== ANTRENÖR-TAKIM ATAMALARI ==========
        if (!context.AntrenorTakimlar.Any())
        {
            Console.WriteLine("📌 Antrenör-takım atamaları ekleniyor...");
            var yakuplu = context.SporSalonlari.FirstOrDefault(s => s.Ad.Contains("Yakuplu"));
            var emlak = context.SporSalonlari.FirstOrDefault(s => s.Ad.Contains("Emlak"));
            var nese = context.SporSalonlari.FirstOrDefault(s => s.Ad.Contains("Neşe"));
            
            var ozgur = context.Antrenorler.FirstOrDefault(a => a.Email == "ozgurdemir@bbsk.com");
            var sezer = context.Antrenorler.FirstOrDefault(a => a.Email == "sezeryilmaz@bbsk.com");
            var nesrin = context.Antrenorler.FirstOrDefault(a => a.Email == "nesrinkaya@bbsk.com");
            
            if (ozgur != null && yakuplu != null)
            {
                context.AntrenorTakimlar.Add(new AntrenorTakim { AntrenorId = ozgur.Id, SporSalonuId = yakuplu.Id, AtanmaTarihi = DateTime.Now });
            }
            
            if (sezer != null && emlak != null)
            {
                context.AntrenorTakimlar.Add(new AntrenorTakim { AntrenorId = sezer.Id, SporSalonuId = emlak.Id, AtanmaTarihi = DateTime.Now });
            }
            
            if (nesrin != null && nese != null)
            {
                context.AntrenorTakimlar.Add(new AntrenorTakim { AntrenorId = nesrin.Id, SporSalonuId = nese.Id, AtanmaTarihi = DateTime.Now });
            }
            
            context.SaveChanges();
            Console.WriteLine("✅ Antrenör-takım atamaları eklendi.");
        }
        
        // ========== ÖRNEK SPORCULAR ==========
        if (!context.Uyeler.Any())
        {
            Console.WriteLine("📌 Örnek sporcular ekleniyor...");
            var yakuplu = context.SporSalonlari.FirstOrDefault(s => s.Ad.Contains("Yakuplu"));
            var voleybol = context.Branslar.FirstOrDefault(b => b.Ad == "Voleybol" && b.SporSalonuId == yakuplu.Id);
            var u8Takim = context.Takimlar.FirstOrDefault(t => t.Ad == "U8 Takım" && t.SporSalonuId == yakuplu.Id);
            var aGrubu = context.Gruplar.FirstOrDefault(g => g.Ad == "A Grubu" && g.TakimId == u8Takim.Id);
            
            if (yakuplu != null && voleybol != null && u8Takim != null && aGrubu != null)
            {
                context.Uyeler.Add(new Uye
                {
                    AdSoyad = "Ahmet Yılmaz",
                    Telefon = "0532 111 2233",
                    DogumTarihi = new DateTime(2010, 5, 15),
                    Branş = "Voleybol",
                    AylikAidat = 500,
                    VeliAdSoyad = "Mehmet Yılmaz",
                    VeliTelefon = "0532 111 2233",
                    SporSalonuId = yakuplu.Id,
                    BransId = voleybol.Id,
                    TakimId = u8Takim.Id,
                    GrupId = aGrubu.Id,
                    KiyafetVerildiMi = true,
                    KayitTarihi = DateTime.Now
                });
                context.SaveChanges();
                Console.WriteLine("✅ Örnek sporcu eklendi.");
            }
        }
        
        Console.WriteLine("🎉 Tüm örnek veriler başarıyla eklendi!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Veritabanı hatası: {ex.Message}");
        Console.WriteLine($"Detay: {ex.InnerException?.Message}");
    }
}
// ========== VERİTABANI KURULUMU SONU ==========

// Configure the HTTP request pipeline.
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Ana sayfayı direkt login'e yönlendir
app.MapGet("/", async context =>
{
    context.Response.Redirect("/Antrenor/Giris");
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Antrenor}/{action=Giris}/{id?}");

// PORT ayarı - Render için
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

Console.WriteLine($"✅ Uygulama başlatıldı! Port: {port}");
Console.WriteLine($"✅ Veritabanı yolu: {dbPath}");

app.Run();