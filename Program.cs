using Microsoft.EntityFrameworkCore;
using SporKulubu.Data;
using SporKulubu.Models;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<UygulamaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "sporkulubu.db");
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

            // 2. VOLEYBOL BRANŞLARI (Tüm salonlar için)
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

            // 4. VOLEYBOL GRUPLARI (A, B, C, D, E)
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

                // BASKETBOL TAKIMLARI (U8, U10, U12, U14)
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

                // BASKETBOL GRUPLARI (A, B, C, D, E)
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

                // 6. ATLETİK PERFORMANS BRANŞI
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

            Console.WriteLine("🎉 Tüm örnek veriler başarıyla eklendi!");
        }
        else
        {
            Console.WriteLine("ℹ️ Veritabanı zaten dolu, yeni veri eklenmedi.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Hata oluştu: {ex.Message}");
        Console.WriteLine($"Detay: {ex.InnerException?.Message}");
    }
}
// ========== VERİTABANI KURULUMU SONU ==========

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Antrenor}/{action=Giris}/{id?}");

app.Run();