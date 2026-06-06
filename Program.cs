using Microsoft.EntityFrameworkCore;
using SporKulubu.Data;
using SporKulubu.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Session services - Kalıcı oturum için
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.MaxAge = TimeSpan.FromDays(30);
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ========== KALICI VERİTABANI ==========
string dbPath;
if (Directory.Exists("/data"))
{
    dbPath = "/data/sporkulubu.db";
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

// ========== VERİTABANINI OTOMATİK OLUŞTUR ==========
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UygulamaDbContext>();
    
    try
    {
        context.Database.EnsureCreated();
        Console.WriteLine("Veritabanı hazır.");
        
        // ========== 1. SPOR SALONLARI ==========
        if (!context.SporSalonlari.Any())
        {
            Console.WriteLine("Spor salonları ekleniyor...");
            context.SporSalonlari.AddRange(
                new SporSalonu { Ad = "Emlak Konut Spor Salonu" },
                new SporSalonu { Ad = "Yakuplu For Life Spor Salonu" },
                new SporSalonu { Ad = "Neşe Sever Spor Salonu" }
            );
            context.SaveChanges();
            Console.WriteLine("3 spor salonu eklendi.");
        }
        
        // ========== 2. BRANŞLAR ==========
        if (!context.Branslar.Any())
        {
            Console.WriteLine("Branşlar ekleniyor...");
            var salonlar = context.SporSalonlari.ToList();
            
            foreach (var salon in salonlar)
            {
                // Voleybol - Tüm salonlarda
                context.Branslar.Add(new Brans { Ad = "Voleybol", TakimVarMi = true, GrupVarMi = true, SporSalonuId = salon.Id });
                
                // Atletik Performans - Tüm salonlarda
                context.Branslar.Add(new Brans { Ad = "Atletik Performans", TakimVarMi = false, GrupVarMi = false, SporSalonuId = salon.Id });
                
                // Basketbol - SADECE Yakuplu salonunda
                if (salon.Ad.Contains("Yakuplu"))
                {
                    context.Branslar.Add(new Brans { Ad = "Basketbol", TakimVarMi = true, GrupVarMi = true, SporSalonuId = salon.Id });
                }
            }
            context.SaveChanges();
            Console.WriteLine("Branşlar eklendi. (Basketbol sadece Yakuplu'da)");
        }
        
        // ========== 3. TAKIMLAR ==========
        if (!context.Takimlar.Any())
        {
            Console.WriteLine("Takımlar ekleniyor...");
            var voleybolBranslari = context.Branslar.Where(b => b.Ad == "Voleybol").ToList();
            var basketbolBranslari = context.Branslar.Where(b => b.Ad == "Basketbol").ToList();
            
            string[] voleybolTakimlari = { "Mini Takım", "Midi Takım", "Küçük Takım", "Yıldız Takım" };
            string[] basketbolTakimlari = { "U8 Takım", "U10 Takım", "U12 Takım", "U14 Takım" };
            
            // Voleybol takımları - HER SALON İÇİN
            foreach (var brans in voleybolBranslari)
            {
                foreach (var takimAd in voleybolTakimlari)
                {
                    context.Takimlar.Add(new Takim
                    {
                        Ad = takimAd,
                        SporSalonuId = brans.SporSalonuId,
                        BransId = brans.Id
                    });
                }
            }
            
            // Basketbol takımları - SADECE YAKUPLU İÇİN
            foreach (var brans in basketbolBranslari)
            {
                foreach (var takimAd in basketbolTakimlari)
                {
                    context.Takimlar.Add(new Takim
                    {
                        Ad = takimAd,
                        SporSalonuId = brans.SporSalonuId,
                        BransId = brans.Id
                    });
                }
            }
            context.SaveChanges();
            Console.WriteLine("Takımlar eklendi.");
        }
        
        // ========== 4. GRUPLAR ==========
        if (!context.Gruplar.Any())
        {
            Console.WriteLine("Gruplar ekleniyor...");
            string[] grupHarfleri = { "A Grubu", "B Grubu", "C Grubu", "D Grubu", "E Grubu" };
            var takimlar = context.Takimlar.ToList();
            
            foreach (var takim in takimlar)
            {
                foreach (var grupAd in grupHarfleri)
                {
                    context.Gruplar.Add(new Grup { Ad = grupAd, TakimId = takim.Id });
                }
            }
            context.SaveChanges();
            Console.WriteLine("Gruplar eklendi.");
        }
        
        // ========== 5. ANTRENÖRLER (YENİ ŞİFRELERLE) ==========
        if (!context.Antrenorler.Any())
        {
            Console.WriteLine("Antrenörler ekleniyor...");
            context.Antrenorler.AddRange(
                new Antrenor { AdSoyad = "Burhan Şayan", Email = "burhansayan@bbsk.com", Sifre = "Pasör7", Telefon = "0505 896 8407", Uzmanlik = "Tam Yetki", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Ertan Tunçel", Email = "ertantunçel@bbsk.com", Sifre = "Ertan347837.", Telefon = "0531 703 92 43", Uzmanlik = "Tam Yetki", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Özgür Doğan", Email = "özgurdogan@bbsk.com", Sifre = "Özgür3434.", Telefon = "0532 333 4455", Uzmanlik = "Yakuplu Sorumlusu", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Eftelya Köse", Email = "eftelyakose@bbsk.com", Sifre = "Eftelya3467.", Telefon = "0532 444 5566", Uzmanlik = "Yakuplu Sorumlusu", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Sezer Kaya", Email = "sezerkaya@bbsk.com", Sifre = "Sezer3443.", Telefon = "0532 555 6677", Uzmanlik = "Emlak Konut Sorumlusu", KayitTarihi = DateTime.Now }
            );
            context.SaveChanges();
            Console.WriteLine("Antrenörler eklendi. (Şifreler güncellendi)");
        }
        
        // ========== 6. ANTRENÖR-YETKİ ATAMALARI ==========
        if (!context.AntrenorTakimlar.Any())
        {
            Console.WriteLine("Antrenör-yetki atamaları ekleniyor...");
            var yakuplu = context.SporSalonlari.FirstOrDefault(s => s.Ad.Contains("Yakuplu"));
            var emlak = context.SporSalonlari.FirstOrDefault(s => s.Ad.Contains("Emlak"));
            
            var ozgur = context.Antrenorler.FirstOrDefault(a => a.Email == "ozgurdogan@bbsk.com");
            var eftelya = context.Antrenorler.FirstOrDefault(a => a.Email == "eftelyakose@bbsk.com");
            var sezer = context.Antrenorler.FirstOrDefault(a => a.Email == "sezerkaya@bbsk.com");
            
            if (ozgur != null && yakuplu != null)
            {
                context.AntrenorTakimlar.Add(new AntrenorTakim { AntrenorId = ozgur.Id, SporSalonuId = yakuplu.Id, AtanmaTarihi = DateTime.Now });
                Console.WriteLine("Özgür Doğan -> Yakuplu yetkisi eklendi");
            }
            if (eftelya != null && yakuplu != null)
            {
                context.AntrenorTakimlar.Add(new AntrenorTakim { AntrenorId = eftelya.Id, SporSalonuId = yakuplu.Id, AtanmaTarihi = DateTime.Now });
                Console.WriteLine("Eftelya Köse -> Yakuplu yetkisi eklendi");
            }
            if (sezer != null && emlak != null)
            {
                context.AntrenorTakimlar.Add(new AntrenorTakim { AntrenorId = sezer.Id, SporSalonuId = emlak.Id, AtanmaTarihi = DateTime.Now });
                Console.WriteLine("Sezer Kaya -> Emlak Konut yetkisi eklendi");
            }
            
            context.SaveChanges();
            Console.WriteLine("Antrenör-yetki atamaları eklendi.");
        }
        
        // ========== 7. ÖRNEK SPORCULAR ==========
        if (!context.Uyeler.Any())
        {
            Console.WriteLine("Örnek sporcular ekleniyor...");
            var yakuplu = context.SporSalonlari.FirstOrDefault(s => s.Ad.Contains("Yakuplu"));
            var voleybol = context.Branslar.FirstOrDefault(b => b.Ad == "Voleybol" && b.SporSalonuId == yakuplu.Id);
            var miniTakim = context.Takimlar.FirstOrDefault(t => t.Ad == "Mini Takım" && t.SporSalonuId == yakuplu.Id);
            var aGrubu = context.Gruplar.FirstOrDefault(g => g.Ad == "A Grubu" && g.TakimId == miniTakim.Id);
            
            if (yakuplu != null && voleybol != null && miniTakim != null && aGrubu != null)
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
                    TakimId = miniTakim.Id,
                    GrupId = aGrubu.Id,
                    KiyafetVerildiMi = true,
                    KayitTarihi = DateTime.Now
                });
                context.SaveChanges();
                Console.WriteLine("Örnek sporcu eklendi.");
            }
        }
        
        Console.WriteLine("Veritabanı kurulumu tamamlandı!");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Veritabanı hatasi: " + ex.Message);
    }
}

// ========== MIDDLEWARE ==========
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

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

Console.WriteLine($"Uygulama başlatildi! Port: {port}");
Console.WriteLine($"Veritabani yolu: {dbPath}");

app.Run();