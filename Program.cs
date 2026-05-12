using Microsoft.EntityFrameworkCore;
using SporKulubu.Data;
using SporKulubu.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Session services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Database configuration
builder.Services.AddDbContext<UygulamaDbContext>(options =>
    options.UseSqlite("Data Source=sporkulubu.db"));

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
            }
            context.SaveChanges();
            Console.WriteLine("✅ Branşlar eklendi.");
        }
        
        // ========== TAKIMLAR ==========
        if (!context.Takimlar.Any())
        {
            Console.WriteLine("📌 Takımlar ekleniyor...");
            var branslar = context.Branslar.ToList();
            foreach (var brans in branslar)
            {
                context.Takimlar.AddRange(
                    new Takim { Ad = "Mini Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "Midi Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "Küçük Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "Yıldız Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id }
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
                    context.Gruplar.Add(new Grup { Ad = $"{harf} Grubu", TakimId = takim.Id });
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
        
        // ========== ÖRNEK SPORCULAR ==========
        if (!context.Uyeler.Any())
        {
            Console.WriteLine("📌 Örnek sporcular ekleniyor...");
            var yakuplu = context.SporSalonlari.FirstOrDefault(s => s.Ad.Contains("Yakuplu"));
            var voleybol = context.Branslar.FirstOrDefault(b => b.Ad == "Voleybol" && b.SporSalonuId == yakuplu?.Id);
            var miniTakim = context.Takimlar.FirstOrDefault(t => t.Ad == "Mini Takım" && t.SporSalonuId == yakuplu?.Id);
            var aGrubu = context.Gruplar.FirstOrDefault(g => g.Ad == "A Grubu" && g.TakimId == miniTakim?.Id);
            
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
                Console.WriteLine("✅ Örnek sporcu eklendi.");
            }
        }
        
        Console.WriteLine("🎉 Tüm örnek veriler başarıyla eklendi!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Veritabanı hatası: {ex.Message}");
    }
}
// ========== VERİTABANI KURULUMU SONU ==========

// Configure the HTTP request pipeline.
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Antrenor}/{action=Giris}/{id?}");

// PORT ayarı - Render için
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();