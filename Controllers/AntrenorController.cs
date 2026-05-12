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
    public async Task<IActionResult> SporcuEkle()
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

    // ========== VERİTABANINI KUR ==========
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

        // Antrenörler
        if (!_context.Antrenorler.Any())
        {
            _context.Antrenorler.AddRange(
                new Antrenor { AdSoyad = "Burhan Şayan", Email = "burhan@beykentspor.com", Sifre = "123456", Telefon = "0532 111 2233", Uzmanlik = "Tam Yetki", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Ertan Tuncel", Email = "ertan@beykentspor.com", Sifre = "123456", Telefon = "0532 222 3344", Uzmanlik = "Tam Yetki", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Özgür", Email = "ozgur@beykentspor.com", Sifre = "123456", Telefon = "0532 333 4455", Uzmanlik = "Yakuplu", KayitTarihi = DateTime.Now }
            );
            await _context.SaveChangesAsync();
        }

        return Content(@"
            <html>
            <body style='font-family:Arial;padding:20px;text-align:center'>
                <h2 style='color:green'>✅ Veritabanı başarıyla kuruldu!</h2>
                <h3>Beylikdüzü Beykent Spor Kulübü</h3>
                <h4>Giriş Bilgileri:</h4>
                <ul style='display:inline-block;text-align:left'>
                    <li><strong>burhan@beykentspor.com</strong> / 123456 (Tam Yetki)</li>
                    <li><strong>ertan@beykentspor.com</strong> / 123456 (Tam Yetki)</li>
                    <li><strong>ozgur@beykentspor.com</strong> / 123456 (Yakuplu)</li>
                </ul>
                <br/>
                <a href='/Antrenor/Giris' style='display:inline-block;margin-top:20px;padding:10px 20px;background:#0B2A4A;color:white;text-decoration:none;border-radius:5px'>Giriş Yap</a>
            </body>
            </html>
        ", "text/html");
    }
}