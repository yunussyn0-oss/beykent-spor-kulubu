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

        IQueryable<SporSalonu> salonQuery = _context.SporSalonlari.AsNoTracking();
        
        if (!tamYetki)
        {
            var yetkiliSalonIds = await _context.AntrenorTakimlar
                .Where(at => at.AntrenorId == antrenorId && at.SporSalonuId.HasValue)
                .Select(at => at.SporSalonuId.Value)
                .Distinct()
                .ToListAsync();
            
            if (yetkiliSalonIds.Any())
            {
                salonQuery = salonQuery.Where(s => yetkiliSalonIds.Contains(s.Id));
            }
        }

        var salonlar = await salonQuery.OrderBy(s => s.Ad).ToListAsync();
        ViewBag.SporSalonlari = new SelectList(salonlar, "Id", "Ad", salonId);

        if (salonId.HasValue && salonId.Value > 0)
        {
            var takimlar = await _context.Takimlar
                .Where(t => t.SporSalonuId == salonId.Value)
                .OrderBy(t => t.Ad)
                .AsNoTracking()
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
                .AsNoTracking()
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

        if (!tamYetki && salonlar.Any())
        {
            var yetkiliIds = salonlar.Select(s => s.Id).ToList();
            query = query.Where(u => u.SporSalonuId.HasValue && yetkiliIds.Contains(u.SporSalonuId.Value));
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
    public async Task<IActionResult> SporcuEkle(Uye uye)
    {
        if (HttpContext.Session.GetInt32("AntrenorId") == null)
            return RedirectToAction("Giris");

        if (ModelState.IsValid)
        {
            uye.KayitTarihi = DateTime.Now;
            _context.Uyeler.Add(uye);
            await _context.SaveChangesAsync();
            TempData["Basarili"] = "Sporcu başarıyla eklendi!";
            return RedirectToAction("Panel");
        }

        ViewBag.SporSalonlari = new SelectList(_context.SporSalonlari.ToList(), "Id", "Ad");
        return View(uye);
    }

    // ========== SPORCU SİL ==========
    [HttpGet]
    public async Task<IActionResult> SporcuSil(int id)
    {
        if (HttpContext.Session.GetInt32("AntrenorId") == null)
            return RedirectToAction("Giris");

        var uye = await _context.Uyeler.FindAsync(id);
        if (uye == null)
            return NotFound();

        _context.Uyeler.Remove(uye);
        await _context.SaveChangesAsync();
        TempData["Basarili"] = "Sporcu başarıyla silindi!";
        return RedirectToAction("Panel");
    }

    // ========== SPORCU DÜZENLE (GET) ==========
    [HttpGet]
    public async Task<IActionResult> SporcuDuzenle(int id)
    {
        if (HttpContext.Session.GetInt32("AntrenorId") == null)
            return RedirectToAction("Giris");

        var uye = await _context.Uyeler.FindAsync(id);
        if (uye == null)
            return NotFound();

        ViewBag.SporSalonlari = new SelectList(_context.SporSalonlari.ToList(), "Id", "Ad", uye.SporSalonuId);
        return View(uye);
    }

    // ========== SPORCU DÜZENLE (POST) ==========
    [HttpPost]
    public async Task<IActionResult> SporcuDuzenle(int id, Uye uye)
    {
        if (id != uye.Id) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(uye);
            await _context.SaveChangesAsync();
            TempData["Basarili"] = "Sporcu başarıyla güncellendi!";
            return RedirectToAction("Panel");
        }

        ViewBag.SporSalonlari = new SelectList(_context.SporSalonlari.ToList(), "Id", "Ad", uye.SporSalonuId);
        return View(uye);
    }

    // ========== AJAX: TAKIMLARI GETİR ==========
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

    // ========== AJAX: GRUPLARI GETİR ==========
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

    // ========== YOKLAMA (GET) ==========
    [HttpGet]
    public async Task<IActionResult> Yoklama(DateTime? tarih, int? takimId, int? grupId)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null) return RedirectToAction("Giris");

        var seciliTarih = tarih ?? DateTime.Now.Date;

        ViewBag.TumTakimlar = new SelectList(await _context.Takimlar.OrderBy(t => t.Ad).ToListAsync(), "Id", "Ad", takimId);
        
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

    // ========== YOKLAMA KAYDET (POST) ==========
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
        {
            _context.Yoklamalar.RemoveRange(eskiYoklamalar);
        }

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
        TempData["Basarili"] = "Yoklama başarıyla kaydedildi!";
        return RedirectToAction("Yoklama", new { tarih, takimId, grupId });
    }

    // ========== TOPLU MESAJ (GET) ==========
    [HttpGet]
    public async Task<IActionResult> TopluMesaj(int? takimId, int? grupId)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null) return RedirectToAction("Giris");

        ViewBag.TumTakimlar = new SelectList(await _context.Takimlar.OrderBy(t => t.Ad).ToListAsync(), "Id", "Ad", takimId);
        
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

    // ========== ÇIKIŞ ==========
    public IActionResult Cikis()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Giris");
    }
}