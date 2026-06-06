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
    public async Task<IActionResult> Panel(int? salonId, int? takimId, int? grupId)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null) return RedirectToAction("Giris");

        var antrenor = await _context.Antrenorler.FindAsync(antrenorId);
        if (antrenor == null) return RedirectToAction("Giris");

        bool tamYetki = antrenor.AdSoyad.Contains("Burhan") || antrenor.AdSoyad.Contains("Ertan");

        var yetkiliSalonIds = new List<int>();
        if (!tamYetki)
        {
            yetkiliSalonIds = await _context.AntrenorTakimlar
                .Where(at => at.AntrenorId == antrenorId && at.SporSalonuId.HasValue)
                .Select(at => at.SporSalonuId.Value)
                .Distinct()
                .ToListAsync();
        }

        IQueryable<SporSalonu> salonQuery = _context.SporSalonlari.AsNoTracking();
        
        if (!tamYetki && yetkiliSalonIds.Any())
        {
            salonQuery = salonQuery.Where(s => yetkiliSalonIds.Contains(s.Id));
        }

        var salonlar = await salonQuery.OrderBy(s => s.Ad).ToListAsync();
        ViewBag.SporSalonlari = new SelectList(salonlar, "Id", "Ad", salonId);

        if (salonId.HasValue && salonId.Value > 0)
        {
            var takimlar = await _context.Takimlar
                .Where(t => t.SporSalonuId == salonId.Value)
                .OrderBy(t => t.Ad)
                .ToListAsync();
            ViewBag.Takimlar = new SelectList(takimlar, "Id", "Ad", takimId);
        }
        else
        {
            ViewBag.Takimlar = new SelectList(Enumerable.Empty<SelectListItem>());
        }

        if (takimId.HasValue && takimId.Value > 0)
        {
            var gruplar = await _context.Gruplar
                .Where(g => g.TakimId == takimId.Value)
                .OrderBy(g => g.Ad)
                .ToListAsync();
            ViewBag.Gruplar = new SelectList(gruplar, "Id", "Ad", grupId);
        }
        else
        {
            ViewBag.Gruplar = new SelectList(Enumerable.Empty<SelectListItem>());
        }

        var query = _context.Uyeler
            .Include(u => u.SporSalonu)
            .Include(u => u.Brans)
            .Include(u => u.Takim)
            .Include(u => u.Grup)
            .AsQueryable();

        if (!tamYetki && yetkiliSalonIds.Any())
        {
            query = query.Where(u => u.SporSalonuId.HasValue && yetkiliSalonIds.Contains(u.SporSalonuId.Value));
        }

        if (salonId.HasValue && salonId.Value > 0)
            query = query.Where(u => u.SporSalonuId == salonId.Value);
        
        if (takimId.HasValue && takimId.Value > 0)
            query = query.Where(u => u.TakimId == takimId.Value);
        
        if (grupId.HasValue && grupId.Value > 0)
            query = query.Where(u => u.GrupId == grupId.Value);

        var sporcular = await query.OrderBy(u => u.AdSoyad).ToListAsync();

        ViewBag.Sporcular = sporcular;
        ViewBag.SporcuSayisi = sporcular.Count;
        ViewBag.TamYetki = tamYetki;
        ViewBag.SecilenSalonId = salonId;
        ViewBag.SecilenTakimId = takimId;
        ViewBag.SecilenGrupId = grupId;
        ViewBag.AntrenorAdi = antrenor.AdSoyad;
        ViewBag.SecilenSalonAdi = salonId.HasValue ? salonlar.FirstOrDefault(s => s.Id == salonId)?.Ad : "Tüm Salonlar";

        return View(antrenor);
    }

    // ========== SPORCU EKLE (GET) ==========
    [HttpGet]
    public IActionResult SporcuEkle()
    {
        if (HttpContext.Session.GetInt32("AntrenorId") == null)
            return RedirectToAction("Giris");

        ViewBag.SporSalonlari = new SelectList(_context.SporSalonlari.ToList(), "Id", "Ad");
        ViewBag.Takimlar = new SelectList(Enumerable.Empty<SelectListItem>());
        ViewBag.Gruplar = new SelectList(Enumerable.Empty<SelectListItem>());
        
        return View();
    }

    // ========== SPORCU EKLE (POST) ==========
    [HttpPost]
    public async Task<IActionResult> SporcuEkle(Uye uye, IFormFile? SporcuResmi)
    {
        if (HttpContext.Session.GetInt32("AntrenorId") == null)
            return RedirectToAction("Giris");

        if (ModelState.IsValid)
        {
            if (SporcuResmi != null && SporcuResmi.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(SporcuResmi.FileName).ToLower();
                
                if (allowedExtensions.Contains(extension) && SporcuResmi.Length <= 2 * 1024 * 1024)
                {
                    var fileName = $"sporcu_{DateTime.Now.Ticks}{extension}";
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "sporcular");
                    
                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);
                    
                    var filePath = Path.Combine(uploadPath, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await SporcuResmi.CopyToAsync(stream);
                    }
                    
                    uye.SporcuResmi = $"/images/sporcular/{fileName}";
                }
            }
            
            uye.KayitTarihi = DateTime.Now;
            _context.Uyeler.Add(uye);
            await _context.SaveChangesAsync();
            TempData["Basarili"] = "Sporcu başarıyla eklendi!";
            return RedirectToAction("Panel");
        }

        ViewBag.SporSalonlari = new SelectList(_context.SporSalonlari.ToList(), "Id", "Ad");
        return View(uye);
    }

    // ========== SPORCU DÜZENLE ==========
    [HttpGet]
    public async Task<IActionResult> SporcuDuzenle(int id)
    {
        if (HttpContext.Session.GetInt32("AntrenorId") == null)
            return RedirectToAction("Giris");

        var uye = await _context.Uyeler.FindAsync(id);
        if (uye == null) return NotFound();

        ViewBag.SporSalonlari = new SelectList(_context.SporSalonlari.ToList(), "Id", "Ad", uye.SporSalonuId);
        return View(uye);
    }

    [HttpPost]
    public async Task<IActionResult> SporcuDuzenle(int id, Uye uye, IFormFile? SporcuResmi)
    {
        if (id != uye.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var existingUye = await _context.Uyeler.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            
            if (SporcuResmi != null && SporcuResmi.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(SporcuResmi.FileName).ToLower();
                
                if (allowedExtensions.Contains(extension))
                {
                    if (!string.IsNullOrEmpty(existingUye?.SporcuResmi))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingUye.SporcuResmi.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }
                    
                    var fileName = $"sporcu_{DateTime.Now.Ticks}{extension}";
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "sporcular");
                    
                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);
                    
                    var filePath = Path.Combine(uploadPath, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await SporcuResmi.CopyToAsync(stream);
                    }
                    
                    uye.SporcuResmi = $"/images/sporcular/{fileName}";
                }
            }
            else
            {
                uye.SporcuResmi = existingUye?.SporcuResmi;
            }
            
            _context.Update(uye);
            await _context.SaveChangesAsync();
            TempData["Basarili"] = "Sporcu güncellendi!";
            return RedirectToAction("Panel");
        }

        ViewBag.SporSalonlari = new SelectList(_context.SporSalonlari.ToList(), "Id", "Ad", uye.SporSalonuId);
        return View(uye);
    }

    // ========== SPORCU SİL ==========
    [HttpGet]
    public async Task<IActionResult> SporcuSil(int id)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null)
            return RedirectToAction("Giris");

        var uye = await _context.Uyeler.FindAsync(id);
        if (uye == null)
        {
            TempData["Hata"] = "Sporcu bulunamadı!";
            return RedirectToAction("Panel");
        }

        _context.Uyeler.Remove(uye);
        await _context.SaveChangesAsync();
        
        TempData["Basarili"] = "Sporcu silindi!";
        return RedirectToAction("Panel");
    }

    // ========== PROFİL SAYFASI ==========
    [HttpGet]
    public async Task<IActionResult> Profil()
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null)
            return RedirectToAction("Giris");

        var antrenor = await _context.Antrenorler
            .FirstOrDefaultAsync(a => a.Id == antrenorId);

        if (antrenor == null)
            return RedirectToAction("Giris");

        return View(antrenor);
    }

    // ========== PROFİL GÜNCELLE ==========
    [HttpPost]
    public async Task<IActionResult> ProfilGuncelle(Antrenor model)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null)
            return RedirectToAction("Giris");

        var antrenor = await _context.Antrenorler.FindAsync(antrenorId);
        if (antrenor == null)
            return RedirectToAction("Giris");

        antrenor.AdSoyad = model.AdSoyad;
        antrenor.Telefon = model.Telefon;
        antrenor.Uzmanlik = model.Uzmanlik;

        await _context.SaveChangesAsync();
        HttpContext.Session.SetString("AntrenorAdi", antrenor.AdSoyad);
        
        TempData["Basarili"] = "Profil bilgileriniz güncellendi!";
        return RedirectToAction("Profil");
    }

    // ========== PROFİL FOTOĞRAFI YÜKLE ==========
    [HttpPost]
    public async Task<IActionResult> ProfilFotoYukle(IFormFile foto)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null)
            return RedirectToAction("Giris");

        if (foto == null || foto.Length == 0)
        {
            TempData["Hata"] = "Lütfen bir dosya seçin!";
            return RedirectToAction("Profil");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(foto.FileName).ToLower();
        
        if (!allowedExtensions.Contains(extension))
        {
            TempData["Hata"] = "Sadece resim dosyaları yüklenebilir (jpg, jpeg, png, gif)!";
            return RedirectToAction("Profil");
        }

        if (foto.Length > 2 * 1024 * 1024)
        {
            TempData["Hata"] = "Dosya boyutu 2MB'dan küçük olmalıdır!";
            return RedirectToAction("Profil");
        }

        try
        {
            var fileName = $"antrenor_{antrenorId}_{DateTime.Now.Ticks}{extension}";
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiller");
            
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);
            
            var filePath = Path.Combine(uploadPath, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await foto.CopyToAsync(stream);
            }
            
            var antrenor = await _context.Antrenorler.FindAsync(antrenorId);
            
            if (!string.IsNullOrEmpty(antrenor.ProfilResmi))
            {
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", antrenor.ProfilResmi.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                    System.IO.File.Delete(oldFilePath);
            }
            
            antrenor.ProfilResmi = $"/images/profiller/{fileName}";
            await _context.SaveChangesAsync();
            
            TempData["Basarili"] = "Profil fotoğrafınız güncellendi!";
        }
        catch (Exception ex)
        {
            TempData["Hata"] = $"Hata: {ex.Message}";
        }
        
        return RedirectToAction("Profil");
    }

    // ========== AJAX METOTLARI ==========
    [HttpGet]
    public async Task<IActionResult> TakimlariGetir(int salonId)
    {
        var takimlar = await _context.Takimlar
            .Where(t => t.SporSalonuId == salonId)
            .OrderBy(t => t.Ad)
            .Select(t => new { t.Id, t.Ad })
            .ToListAsync();
        return Json(takimlar);
    }

    [HttpGet]
    public async Task<IActionResult> GruplariGetir(int takimId)
    {
        var gruplar = await _context.Gruplar
            .Where(g => g.TakimId == takimId)
            .OrderBy(g => g.Ad)
            .Select(g => new { g.Id, g.Ad })
            .ToListAsync();
        return Json(gruplar);
    }

    // ========== YOKLAMA ==========
    [HttpGet]
    public async Task<IActionResult> Yoklama(DateTime? tarih, int? takimId, int? grupId)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null) return RedirectToAction("Giris");

        var seciliTarih = tarih ?? DateTime.Now.Date;

        ViewBag.TumTakimlar = new SelectList(
            await _context.Takimlar.OrderBy(t => t.Ad).ToListAsync(), 
            "Id", "Ad", takimId);
        
        if (takimId.HasValue && takimId.Value > 0)
        {
            ViewBag.TumGruplar = new SelectList(
                await _context.Gruplar.Where(g => g.TakimId == takimId.Value).OrderBy(g => g.Ad).ToListAsync(),
                "Id", "Ad", grupId);
        }

        var query = _context.Uyeler
            .Include(u => u.Takim)
            .Include(u => u.Grup)
            .AsQueryable();

        if (takimId.HasValue && takimId.Value > 0)
            query = query.Where(u => u.TakimId == takimId.Value);
        
        if (grupId.HasValue && grupId.Value > 0)
            query = query.Where(u => u.GrupId == grupId.Value);

        var sporcular = await query.OrderBy(u => u.AdSoyad).ToListAsync();

        var yoklamalar = await _context.Yoklamalar
            .Where(y => y.AntrenorId == antrenorId && y.Tarih.Date == seciliTarih.Date)
            .ToDictionaryAsync(y => y.UyeId);

        ViewBag.Sporcular = sporcular;
        ViewBag.Yoklamalar = yoklamalar;
        ViewBag.Tarih = seciliTarih;
        ViewBag.SecilenTakimId = takimId;
        ViewBag.SecilenGrupId = grupId;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YoklamaKaydet(List<Yoklama> yoklamalar, DateTime tarih, int? takimId, int? grupId)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null) return RedirectToAction("Giris");

        var eskiYoklamalar = await _context.Yoklamalar
            .Where(y => y.AntrenorId == antrenorId && y.Tarih.Date == tarih.Date)
            .ToListAsync();

        if (eskiYoklamalar.Any())
            _context.Yoklamalar.RemoveRange(eskiYoklamalar);

        if (yoklamalar != null && yoklamalar.Any())
        {
            foreach (var y in yoklamalar.Where(y => !string.IsNullOrEmpty(y.Durum)))
            {
                y.AntrenorId = antrenorId.Value;
                y.Tarih = tarih.Date;
                _context.Yoklamalar.Add(y);
            }
        }

        await _context.SaveChangesAsync();
        TempData["Basarili"] = "Yoklama kaydedildi!";
        return RedirectToAction("Yoklama", new { tarih, takimId, grupId });
    }

    // ========== TOPLU MESAJ ==========
    [HttpGet]
    public async Task<IActionResult> TopluMesaj(int? takimId, int? grupId)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null) return RedirectToAction("Giris");

        ViewBag.TumTakimlar = new SelectList(
            await _context.Takimlar.OrderBy(t => t.Ad).ToListAsync(), 
            "Id", "Ad", takimId);
        
        if (takimId.HasValue && takimId.Value > 0)
        {
            ViewBag.TumGruplar = new SelectList(
                await _context.Gruplar.Where(g => g.TakimId == takimId.Value).OrderBy(g => g.Ad).ToListAsync(),
                "Id", "Ad", grupId);
        }

        var query = _context.Uyeler
            .Include(u => u.SporSalonu)
            .Include(u => u.Brans)
            .Include(u => u.Takim)
            .Include(u => u.Grup)
            .AsQueryable();

        if (takimId.HasValue && takimId.Value > 0)
            query = query.Where(u => u.TakimId == takimId.Value);
        
        if (grupId.HasValue && grupId.Value > 0)
            query = query.Where(u => u.GrupId == grupId.Value);

        var sporcular = await query.OrderBy(u => u.AdSoyad).ToListAsync();

        ViewBag.Sporcular = sporcular;
        ViewBag.SecilenTakimId = takimId;
        ViewBag.SecilenGrupId = grupId;

        return View();
    }

    // ========== VELİ DETAY ==========
    [HttpGet]
    public async Task<IActionResult> VeliDetay(int id)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null) return RedirectToAction("Giris");

        var uye = await _context.Uyeler
            .Include(u => u.SporSalonu)
            .Include(u => u.Brans)
            .Include(u => u.Takim)
            .Include(u => u.Grup)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (uye == null) return NotFound();

        return View(uye);
    }

    // ========== GEÇMİŞ YOKLAMALAR ==========
    [HttpGet]
    public async Task<IActionResult> GecmisYoklamalar(DateTime? baslangic, DateTime? bitis)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null) return RedirectToAction("Giris");

        var query = _context.Yoklamalar
            .Where(y => y.AntrenorId == antrenorId)
            .Include(y => y.Uye)
            .AsQueryable();

        if (baslangic.HasValue)
            query = query.Where(y => y.Tarih.Date >= baslangic.Value.Date);
        
        if (bitis.HasValue)
            query = query.Where(y => y.Tarih.Date <= bitis.Value.Date);

        var yoklamalar = await query
            .OrderByDescending(y => y.Tarih)
            .Take(100)
            .ToListAsync();

        ViewBag.Baslangic = baslangic;
        ViewBag.Bitis = bitis;

        return View(yoklamalar);
    }

    // ========== ŞİFREMİ UNUTTUM ==========
    [HttpGet]
    public IActionResult SifremiUnuttum()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SifremiUnuttum(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            ViewBag.Hata = "E-posta adresi giriniz!";
            return View();
        }

        var antrenor = await _context.Antrenorler
            .FirstOrDefaultAsync(a => a.Email == email);

        if (antrenor == null)
        {
            ViewBag.Hata = "Bu e-posta adresine kayıtlı antrenör bulunamadı!";
            return View();
        }

        antrenor.Sifre = "123456";
        await _context.SaveChangesAsync();

        TempData["Basarili"] = "Şifreniz sıfırlandı! Yeni şifreniz: 123456";
        return RedirectToAction("Giris");
    }

    // ========== ŞİFRE DEĞİŞTİR ==========
    [HttpGet]
    public IActionResult SifreDegistir()
    {
        if (HttpContext.Session.GetInt32("AntrenorId") == null)
            return RedirectToAction("Giris");
        
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SifreDegistir(string eskiSifre, string yeniSifre, string yeniSifreTekrar)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null)
            return RedirectToAction("Giris");

        if (string.IsNullOrEmpty(eskiSifre) || string.IsNullOrEmpty(yeniSifre))
        {
            ViewBag.Hata = "Tüm alanları doldurunuz!";
            return View();
        }

        if (yeniSifre != yeniSifreTekrar)
        {
            ViewBag.Hata = "Yeni şifreler uyuşmuyor!";
            return View();
        }

        if (yeniSifre.Length < 6)
        {
            ViewBag.Hata = "Yeni şifre en az 6 karakter olmalıdır!";
            return View();
        }

        var antrenor = await _context.Antrenorler.FindAsync(antrenorId);
        
        if (antrenor.Sifre != eskiSifre)
        {
            ViewBag.Hata = "Mevcut şifre yanlış!";
            return View();
        }

        antrenor.Sifre = yeniSifre;
        await _context.SaveChangesAsync();

        TempData["Basarili"] = "Şifreniz değiştirildi!";
        return RedirectToAction("Profil");
    }

    // ========== ÇIKIŞ ==========
    public IActionResult Cikis()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Giris");
    }
}