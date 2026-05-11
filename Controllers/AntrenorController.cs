using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SporKulubu.Data;
using SporKulubu.Models;

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
        // Zaten giriş yapmışsa panele yönlendir
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
        // Boş alan kontrolü
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sifre))
        {
            ViewBag.Hata = "E-posta ve şifre boş bırakılamaz!";
            return View();
        }

        // Antrenörü veritabanında ara
        var antrenor = await _context.Antrenorler
            .FirstOrDefaultAsync(a => a.Email == email && a.Sifre == sifre);

        // Antrenör bulunamadıysa
        if (antrenor == null)
        {
            ViewBag.Hata = "E-posta veya şifre hatalı!";
            return View();
        }

        // Session'a kaydet
        HttpContext.Session.SetInt32("AntrenorId", antrenor.Id);
        HttpContext.Session.SetString("AntrenorAdi", antrenor.AdSoyad);
        HttpContext.Session.SetString("AntrenorEmail", antrenor.Email);

        return RedirectToAction("Panel");
    }

    // ========== ANA PANEL ==========
    public async Task<IActionResult> Panel()
    {
        // Oturum kontrolü
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null)
        {
            return RedirectToAction("Giris");
        }

        // Antrenör bilgilerini getir
        var antrenor = await _context.Antrenorler
            .FirstOrDefaultAsync(a => a.Id == antrenorId);

        if (antrenor == null)
        {
            return RedirectToAction("Giris");
        }

        // Tüm sporcuları getir
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

    // ========== SPORCU EKLE ==========
    [HttpGet]
    public IActionResult SporcuEkle()
    {
        // Oturum kontrolü
        if (HttpContext.Session.GetInt32("AntrenorId") == null)
        {
            return RedirectToAction("Giris");
        }

        // Salon listesini view'a gönder
        ViewBag.SporSalonlari = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            _context.SporSalonlari.ToList(), "Id", "Ad");
        
        return View();
    }

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

        ViewBag.SporSalonlari = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            _context.SporSalonlari.ToList(), "Id", "Ad");
        
        return View(uye);
    }

    // ========== YOKLAMA ==========
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

    // ========== TOPLU MESAJ ==========
    [HttpGet]
    public async Task<IActionResult> TopluMesaj()
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null)
        {
            return RedirectToAction("Giris");
        }

        var sporcular = await _context.Uyeler.ToListAsync();
        ViewBag.Sporcular = sporcular;
        return View();
    }

    [HttpPost]
    public IActionResult MesajGonder(string mesaj, List<int> seciliVeliler)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null)
        {
            return RedirectToAction("Giris");
        }

        // WhatsApp/SMS entegrasyonu buraya gelecek
        TempData["Basarili"] = "Mesajlar başarıyla gönderildi!";
        return RedirectToAction("Panel");
    }

    // ========== ÇIKIŞ ==========
    public IActionResult Cikis()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Giris");
    }

    // ========== VERİTABANINI KUR (GEÇİCİ) ==========
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
            <body style='font-family:Arial;padding:20px'>
                <h2 style='color:green'>✅ Veritabanı başarıyla kuruldu!</h2>
                <h3>Giriş Bilgileri:</h3>
                <ul>
                    <li><strong>burhan@beykentspor.com</strong> / 123456 (Tam Yetki)</li>
                    <li><strong>ertan@beykentspor.com</strong> / 123456 (Tam Yetki)</li>
                    <li><strong>ozgur@beykentspor.com</strong> / 123456 (Yakuplu)</li>
                </ul>
                <a href='/Antrenor/Giris' style='display:inline-block;margin-top:20px;padding:10px20px;background:#0B2A4A;color:white;text-decoration:none;border-radius:5px'>Giriş Yap</a>
            </body>
            </html>
        ", "text/html");
    }
}