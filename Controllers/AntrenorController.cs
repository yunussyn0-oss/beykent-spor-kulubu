using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SporKulubu.Data;
using SporKulubu.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SporKulubu.Controllers;

public class AntrenorController : Controller
{
    private readonly UygulamaDbContext _context;

    public AntrenorController(UygulamaDbContext context)
    {
        _context = context;
    }

    // ========== GİRİŞ SAYFASI ==========
    [HttpGet]
    public IActionResult Giris()
    {
        if (HttpContext.Session.GetInt32("AntrenorId") != null)
        {
            return RedirectToAction("Panel");
        }
        return View();
    }

    // ========== GİRİŞ İŞLEMİ ==========
    [HttpPost]
    public async Task<IActionResult> Giris(string email, string sifre)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sifre))
        {
            ViewBag.Hata = "E-posta ve şifre boş bırakılamaz!";
            return View();
        }

        var antrenor = await _context.Antrenorler
            .FirstOrDefaultAsync(a => a.Email == email && a.Sifre == sifre);

        if (antrenor == null)
        {
            ViewBag.Hata = "E-posta veya şifre hatalı!";
            return View();
        }

        HttpContext.Session.SetInt32("AntrenorId", antrenor.Id);
        HttpContext.Session.SetString("AntrenorAdi", antrenor.AdSoyad);
        HttpContext.Session.SetString("AntrenorEmail", antrenor.Email);

        return RedirectToAction("Panel");
    }

    // ========== ANA PANEL ==========
    public async Task<IActionResult> Panel()
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null)
        {
            return RedirectToAction("Giris");
        }

        var antrenor = await _context.Antrenorler
            .FirstOrDefaultAsync(a => a.Id == antrenorId);

        if (antrenor == null)
        {
            return RedirectToAction("Giris");
        }

        var sporcular = await _context.Uyeler
            .Include(u => u.SporSalonu)
            .Include(u => u.Brans)
            .Include(u => u.Takim)
            .Include(u => u.Grup)
            .OrderBy(u => u.AdSoyad)
            .ToListAsync();

        ViewBag.Sporcular = sporcular;
        ViewBag.SporcuSayisi = sporcular.Count;
        ViewBag.AntrenorAdi = antrenor.AdSoyad;

        return View();
    }

    // ========== SPORCU EKLE (GET) ==========
    [HttpGet]
    public IActionResult SporcuEkle()
    {
        if (HttpContext.Session.GetInt32("AntrenorId") == null)
        {
            return RedirectToAction("Giris");
        }

        ViewBag.SporSalonlari = new SelectList(
            _context.SporSalonlari.ToList(), "Id", "Ad");
        
        ViewBag.Takimlar = new SelectList(Enumerable.Empty<SelectListItem>());
        ViewBag.Gruplar = new SelectList(Enumerable.Empty<SelectListItem>());
        
        return View();
    }

    // ========== SPORCU EKLE (POST) ==========
    [HttpPost]
    public async Task<IActionResult> SporcuEkle(Uye uye)
    {
        if (HttpContext.Session.GetInt32("AntrenorId") == null)
        {
            return RedirectToAction("Giris");
        }

        if (ModelState.IsValid)
        {
            uye.KayitTarihi = DateTime.Now;
            _context.Uyeler.Add(uye);
            await _context.SaveChangesAsync();
            TempData["Basarili"] = "Sporcu başarıyla eklendi!";
            return RedirectToAction("Panel");
        }

        ViewBag.SporSalonlari = new SelectList(
            _context.SporSalonlari.ToList(), "Id", "Ad");
        ViewBag.Takimlar = new SelectList(Enumerable.Empty<SelectListItem>());
        ViewBag.Gruplar = new SelectList(Enumerable.Empty<SelectListItem>());
        
        return View(uye);
    }

    // ========== AJAX: TAKIMLARI GETİR ==========
    [HttpGet]
    public async Task<IActionResult> TakimlariGetir(int salonId)
    {
        try
        {
            var takimlar = await _context.Takimlar
                .Where(t => t.SporSalonuId == salonId)
                .OrderBy(t => t.Ad)
                .Select(t => new { t.Id, t.Ad })
                .ToListAsync();
            
            return Json(takimlar);
        }
        catch
        {
            return Json(new List<object>());
        }
    }

    // ========== AJAX: GRUPLARI GETİR ==========
    [HttpGet]
    public async Task<IActionResult> GruplariGetir(int takimId)
    {
        try
        {
            var gruplar = await _context.Gruplar
                .Where(g => g.TakimId == takimId)
                .OrderBy(g => g.Ad)
                .Select(g => new { g.Id, g.Ad })
                .ToListAsync();
            
            return Json(gruplar);
        }
        catch
        {
            return Json(new List<object>());
        }
    }

    // ========== YOKLAMA (GET) ==========
    [HttpGet]
    public async Task<IActionResult> Yoklama()
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null)
        {
            return RedirectToAction("Giris");
        }

        var sporcular = await _context.Uyeler
            .Include(u => u.Takim)
            .Include(u => u.Grup)
            .OrderBy(u => u.AdSoyad)
            .ToListAsync();

        ViewBag.Sporcular = sporcular;
        return View();
    }

    // ========== YOKLAMA KAYDET (POST) ==========
    [HttpPost]
    public async Task<IActionResult> YoklamaKaydet(List<Yoklama> yoklamalar)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null)
        {
            return RedirectToAction("Giris");
        }

        foreach (var yoklama in yoklamalar)
        {
            if (!string.IsNullOrEmpty(yoklama.Durum))
            {
                yoklama.AntrenorId = antrenorId.Value;
                yoklama.Tarih = DateTime.Now.Date;
                _context.Yoklamalar.Add(yoklama);
            }
        }

        await _context.SaveChangesAsync();
        TempData["Basarili"] = "Yoklama başarıyla kaydedildi!";
        return RedirectToAction("Panel");
    }

    // ========== TOPLU MESAJ (GET) ==========
    [HttpGet]
    public async Task<IActionResult> TopluMesaj()
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null)
        {
            return RedirectToAction("Giris");
        }

        var sporcular = await _context.Uyeler
            .OrderBy(u => u.AdSoyad)
            .ToListAsync();
            
        ViewBag.Sporcular = sporcular;
        return View();
    }

    // ========== ÇIKIŞ ==========
    public IActionResult Cikis()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Giris");
    }

    // ========== VERİTABANINI KUR (YENİ GİRİŞ BİLGİLERİYLE) ==========
    [HttpGet]
    public async Task<IActionResult> VeritabaniKur()
    {
        // Salonlar
        if (!_context.SporSalonlari.Any())
        {
            _context.SporSalonlari.AddRange(
                new SporSalonu { Ad = "Emlak Konut Spor Salonu" },
                new SporSalonu { Ad = "Yakuplu For Life Spor Salonu" },
                new SporSalonu { Ad = "Neşe Sever Spor Salonu" }
            );
            await _context.SaveChangesAsync();
        }

        // Branşlar
        if (!_context.Branslar.Any())
        {
            var salonlar = _context.SporSalonlari.ToList();
            foreach (var salon in salonlar)
            {
                _context.Branslar.Add(new Brans { Ad = "Voleybol", TakimVarMi = true, GrupVarMi = true, SporSalonuId = salon.Id });
            }
            await _context.SaveChangesAsync();
        }

        // Takımlar
        if (!_context.Takimlar.Any())
        {
            var branslar = _context.Branslar.Where(b => b.Ad == "Voleybol").ToList();
            foreach (var brans in branslar)
            {
                _context.Takimlar.AddRange(
                    new Takim { Ad = "Mini Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "Midi Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "Küçük Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id },
                    new Takim { Ad = "Yıldız Takım", SporSalonuId = brans.SporSalonuId, BransId = brans.Id }
                );
            }
            await _context.SaveChangesAsync();
        }

        // Gruplar
        if (!_context.Gruplar.Any())
        {
            string[] grupHarfleri = { "A", "B", "C", "D", "E" };
            var takimlar = _context.Takimlar.ToList();
            foreach (var takim in takimlar)
            {
                foreach (var harf in grupHarfleri)
                {
                    _context.Gruplar.Add(new Grup { Ad = harf + " Grubu", TakimId = takim.Id });
                }
            }
            await _context.SaveChangesAsync();
        }

        // Antrenörler - YENİ GİRİŞ BİLGİLERİ
        if (!_context.Antrenorler.Any())
        {
            _context.Antrenorler.AddRange(
                new Antrenor { AdSoyad = "Burhan Şayan", Email = "burhansayan@bbsk.com", Sifre = "123456", Telefon = "0532 111 2233", Uzmanlik = "Baş Antrenör", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Ertan Tuncel", Email = "ertantuncel@bbsk.com", Sifre = "123456", Telefon = "0532 222 3344", Uzmanlik = "Antrenör", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Özgür Demir", Email = "ozgurdemir@bbsk.com", Sifre = "123456", Telefon = "0532 333 4455", Uzmanlik = "Yakuplu Sorumlusu", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Sezer Yılmaz", Email = "sezeryilmaz@bbsk.com", Sifre = "123456", Telefon = "0532 444 5566", Uzmanlik = "Emlak Konut Sorumlusu", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Nesrin Kaya", Email = "nesrinkaya@bbsk.com", Sifre = "123456", Telefon = "0532 555 6677", Uzmanlik = "Neşe Sever Sorumlusu", KayitTarihi = DateTime.Now }
            );
            await _context.SaveChangesAsync();
        }

        return Content(@"
            <html>
            <head>
                <title>Veritabanı Kuruldu - Beylikdüzü Beykent Spor</title>
                <style>
                    body { font-family: 'Segoe UI', Arial, sans-serif; background: linear-gradient(135deg, #0B2A4A 0%, #1B3B5C 100%); min-height: 100vh; display: flex; justify-content: center; align-items: center; margin: 0; padding: 20px; }
                    .container { background: white; border-radius: 20px; padding: 40px; max-width: 500px; text-align: center; box-shadow: 0 20px 40px rgba(0,0,0,0.2); }
                    h1 { color: #0B2A4A; margin-bottom: 10px; }
                    .success { color: #28a745; font-size: 48px; margin-bottom: 20px; }
                    .info-box { background: #f8f9fa; border-radius: 15px; padding: 20px; margin: 20px 0; text-align: left; }
                    .info-box h3 { color: #0B2A4A; margin-bottom: 15px; }
                    .info-box p { margin: 8px 0; }
                    .btn { display: inline-block; background: #0B2A4A; color: white; padding: 12px 30px; text-decoration: none; border-radius: 8px; margin-top: 20px; }
                    .btn:hover { background: #1B3B5C; }
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='success'>✅</div>
                    <h1>BEYLİKDÜZÜ BEYKENT SPOR</h1>
                    <p>Veritabanı başarıyla kuruldu!</p>
                    
                    <div class='info-box'>
                        <h3>📋 Giriş Bilgileri</h3>
                        <p><strong>👑 Burhan Şayan</strong><br/>burhansayan@bbsk.com / 123456</p>
                        <p><strong>👑 Ertan Tuncel</strong><br/>ertantuncel@bbsk.com / 123456</p>
                        <p><strong>🏋️ Özgür Demir</strong><br/>ozgurdemir@bbsk.com / 123456</p>
                        <p><strong>🏢 Sezer Yılmaz</strong><br/>sezeryilmaz@bbsk.com / 123456</p>
                        <p><strong>🏠 Nesrin Kaya</strong><br/>nesrinkaya@bbsk.com / 123456</p>
                    </div>
                    
                    <a href='/Antrenor/Giris' class='btn'>Giriş Yap</a>
                </div>
            </body>
            </html>
        ", "text/html");
    }
}