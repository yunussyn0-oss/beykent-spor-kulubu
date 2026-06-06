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

        // YETKİ KONTROLÜ - Tam Yetki (Burhan ve Ertan)
        bool tamYetki = antrenor.AdSoyad.Contains("Burhan") || antrenor.AdSoyad.Contains("Ertan");

        // Yetkili olduğu salonları bul
        var yetkiliSalonIds = new List<int>();
        if (!tamYetki)
        {
            yetkiliSalonIds = await _context.AntrenorTakimlar
                .Where(at => at.AntrenorId == antrenorId && at.SporSalonuId.HasValue)
                .Select(at => at.SporSalonuId.Value)
                .Distinct()
                .ToListAsync();
        }

        // ===== SALON LİSTESİ =====
        IQueryable<SporSalonu> salonQuery = _context.SporSalonlari.AsNoTracking();
        
        if (!tamYetki && yetkiliSalonIds.Any())
        {
            salonQuery = salonQuery.Where(s => yetkiliSalonIds.Contains(s.Id));
        }

        var salonlar = await salonQuery.OrderBy(s => s.Ad).ToListAsync();
        ViewBag.SporSalonlari = new SelectList(salonlar, "Id", "Ad", salonId);

        // ===== TAKIM LİSTESİ =====
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

        // ===== GRUP LİSTESİ =====
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

        // ===== SPORCU LİSTESİ =====
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

    // ========== SPORCU DÜZENLE (GET) ==========
    [HttpGet]
    public async Task<IActionResult> SporcuDuzenle(int id)
    {
        if (HttpContext.Session.GetInt32("AntrenorId") == null)
            return RedirectToAction("Giris");

        var uye = await _context.Uyeler.FindAsync(id);
        if (uye == null) return NotFound();

        ViewBag.SporSalonlari = new SelectList(_context.SporSalonlari.ToList(), "Id", "Ad", uye.SporSalonuId);
        
        if (uye.SporSalonuId.HasValue)
        {
            var takimlar = await _context.Takimlar
                .Where(t => t.SporSalonuId == uye.SporSalonuId.Value)
                .ToListAsync();
            ViewBag.Takimlar = new SelectList(takimlar, "Id", "Ad", uye.TakimId);
        }
        
        if (uye.TakimId.HasValue)
        {
            var gruplar = await _context.Gruplar
                .Where(g => g.TakimId == uye.TakimId.Value)
                .ToListAsync();
            ViewBag.Gruplar = new SelectList(gruplar, "Id", "Ad", uye.GrupId);
        }

        return View(uye);
    }

    // ========== SPORCU DÜZENLE (POST) ==========
    [HttpPost]
    public async Task<IActionResult> SporcuDuzenle(int id, Uye uye)
    {
        if (id != uye.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(uye);
                await _context.SaveChangesAsync();
                TempData["Basarili"] = "Sporcu başarıyla güncellendi!";
                return RedirectToAction("Panel");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Uyeler.Any(e => e.Id == id))
                    return NotFound();
                throw;
            }
        }

        ViewBag.SporSalonlari = new SelectList(_context.SporSalonlari.ToList(), "Id", "Ad", uye.SporSalonuId);
        return View(uye);
    }

    // ========== SPORCU SİL ==========
    [HttpGet]
    public async Task<IActionResult> SporcuSil(int id)
    {
        if (HttpContext.Session.GetInt32("AntrenorId") == null)
            return RedirectToAction("Giris");

        var uye = await _context.Uyeler.FindAsync(id);
        if (uye == null) return NotFound();

        _context.Uyeler.Remove(uye);
        await _context.SaveChangesAsync();
        TempData["Basarili"] = "Sporcu başarıyla silindi!";
        return RedirectToAction("Panel");
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
                await _context.Gruplar
                    .Where(g => g.TakimId == takimId.Value)
                    .OrderBy(g => g.Ad)
                    .ToListAsync(),
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
        
        return RedirectToAction("Yoklama", new { tarih = tarih, takimId = takimId, grupId = grupId });
    }

    // ========== TOPLU MESAJ (GET) ==========
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
                await _context.Gruplar
                    .Where(g => g.TakimId == takimId.Value)
                    .OrderBy(g => g.Ad)
                    .ToListAsync(),
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

    // ========== İSTATİSTİKLER ==========
    [HttpGet]
    public async Task<IActionResult> Istatistikler(int? takimId, int? grupId)
    {
        var antrenorId = HttpContext.Session.GetInt32("AntrenorId");
        if (antrenorId == null) return RedirectToAction("Giris");

        ViewBag.TumTakimlar = new SelectList(
            await _context.Takimlar.OrderBy(t => t.Ad).ToListAsync(), 
            "Id", "Ad", takimId);
        
        if (takimId.HasValue && takimId.Value > 0)
        {
            ViewBag.TumGruplar = new SelectList(
                await _context.Gruplar
                    .Where(g => g.TakimId == takimId.Value)
                    .OrderBy(g => g.Ad)
                    .ToListAsync(),
                "Id", "Ad", grupId);
        }

        var query = _context.Uyeler
            .Include(u => u.Aidatlar)
            .AsQueryable();

        if (takimId.HasValue && takimId.Value > 0)
            query = query.Where(u => u.TakimId == takimId.Value);
        
        if (grupId.HasValue && grupId.Value > 0)
            query = query.Where(u => u.GrupId == grupId.Value);

        var uyeler = await query.ToListAsync();

        ViewBag.ToplamSporcu = uyeler.Count;
        ViewBag.ToplamAidat = uyeler.Sum(u => u.AylikAidat);
        ViewBag.BorcluSporcu = uyeler.Count(u => u.Aidatlar != null && u.Aidatlar.Any(a => !a.OdendiMi));
        ViewBag.KiyafetVerilen = uyeler.Count(u => u.KiyafetVerildiMi);

        return View();
    }

    // ========== ŞİFREMİ UNUTTUM (GET) ==========
    [HttpGet]
    public IActionResult SifremiUnuttum()
    {
        return View();
    }

    // ========== ŞİFREMİ UNUTTUM (POST) ==========
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

    // ========== ŞİFRE DEĞİŞTİR (GET) ==========
    [HttpGet]
    public IActionResult SifreDegistir()
    {
        if (HttpContext.Session.GetInt32("AntrenorId") == null)
            return RedirectToAction("Giris");
        
        return View();
    }

    // ========== ŞİFRE DEĞİŞTİR (POST) ==========
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

        TempData["Basarili"] = "Şifreniz başarıyla değiştirildi!";
        return RedirectToAction("Panel");
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
                if (salon.Ad.Contains("Yakuplu"))
                {
                    _context.Branslar.Add(new Brans { Ad = "Basketbol", TakimVarMi = true, GrupVarMi = true, SporSalonuId = salon.Id });
                }
                _context.Branslar.Add(new Brans { Ad = "Atletik Performans", TakimVarMi = false, GrupVarMi = false, SporSalonuId = salon.Id });
            }
            await _context.SaveChangesAsync();
        }

        // Takımlar
        if (!_context.Takimlar.Any())
        {
            var voleybolBranslari = _context.Branslar.Where(b => b.Ad == "Voleybol").ToList();
            var basketbolBranslari = _context.Branslar.Where(b => b.Ad == "Basketbol").ToList();
            string[] voleybolTakimlari = { "Mini Takım", "Midi Takım", "Küçük Takım", "Yıldız Takım" };
            string[] basketbolTakimlari = { "U8 Takım", "U10 Takım", "U12 Takım", "U14 Takım" };
            
            foreach (var brans in voleybolBranslari)
            {
                foreach (var takimAd in voleybolTakimlari)
                {
                    _context.Takimlar.Add(new Takim { Ad = takimAd, SporSalonuId = brans.SporSalonuId, BransId = brans.Id });
                }
            }
            foreach (var brans in basketbolBranslari)
            {
                foreach (var takimAd in basketbolTakimlari)
                {
                    _context.Takimlar.Add(new Takim { Ad = takimAd, SporSalonuId = brans.SporSalonuId, BransId = brans.Id });
                }
            }
            await _context.SaveChangesAsync();
        }

        // Gruplar
        if (!_context.Gruplar.Any())
        {
            string[] grupHarfleri = { "A Grubu", "B Grubu", "C Grubu", "D Grubu", "E Grubu" };
            var takimlar = _context.Takimlar.ToList();
            foreach (var takim in takimlar)
            {
                foreach (var grupAd in grupHarfleri)
                {
                    _context.Gruplar.Add(new Grup { Ad = grupAd, TakimId = takim.Id });
                }
            }
            await _context.SaveChangesAsync();
        }

        // Antrenörler
        if (!_context.Antrenorler.Any())
        {
            _context.Antrenorler.AddRange(
                new Antrenor { AdSoyad = "Burhan Şayan", Email = "burhansayan@bbsk.com", Sifre = "123456", Telefon = "0532 111 2233", Uzmanlik = "Tam Yetki", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Ertan Tuncel", Email = "ertantuncel@bbsk.com", Sifre = "123456", Telefon = "0532 222 3344", Uzmanlik = "Tam Yetki", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Özgür Demir", Email = "ozgurdemir@bbsk.com", Sifre = "123456", Telefon = "0532 333 4455", Uzmanlik = "Yakuplu Sorumlusu", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Eftelya Köse", Email = "eftelyakose@bbsk.com", Sifre = "123456", Telefon = "0532 444 5566", Uzmanlik = "Yakuplu Sorumlusu", KayitTarihi = DateTime.Now },
                new Antrenor { AdSoyad = "Sezer Kaya", Email = "sezerkaya@bbsk.com", Sifre = "123456", Telefon = "0532 555 6677", Uzmanlik = "Emlak Konut Sorumlusu", KayitTarihi = DateTime.Now }
            );
            await _context.SaveChangesAsync();
        }

        // Antrenör-yetki atamaları
        if (!_context.AntrenorTakimlar.Any())
        {
            var yakuplu = _context.SporSalonlari.FirstOrDefault(s => s.Ad.Contains("Yakuplu"));
            var emlak = _context.SporSalonlari.FirstOrDefault(s => s.Ad.Contains("Emlak"));
            var ozgur = _context.Antrenorler.FirstOrDefault(a => a.Email == "ozgurdemir@bbsk.com");
            var eftelya = _context.Antrenorler.FirstOrDefault(a => a.Email == "eftelyakose@bbsk.com");
            var sezer = _context.Antrenorler.FirstOrDefault(a => a.Email == "sezerkaya@bbsk.com");
            
            if (ozgur != null && yakuplu != null)
                _context.AntrenorTakimlar.Add(new AntrenorTakim { AntrenorId = ozgur.Id, SporSalonuId = yakuplu.Id, AtanmaTarihi = DateTime.Now });
            if (eftelya != null && yakuplu != null)
                _context.AntrenorTakimlar.Add(new AntrenorTakim { AntrenorId = eftelya.Id, SporSalonuId = yakuplu.Id, AtanmaTarihi = DateTime.Now });
            if (sezer != null && emlak != null)
                _context.AntrenorTakimlar.Add(new AntrenorTakim { AntrenorId = sezer.Id, SporSalonuId = emlak.Id, AtanmaTarihi = DateTime.Now });
            
            await _context.SaveChangesAsync();
        }

        return Content(@"
            <html>
            <head><title>Veritabanı Kuruldu</title></head>
            <body style='font-family:Arial;text-align:center;padding:50px'>
                <h2 style='color:green'>✅ Veritabanı başarıyla kuruldu!</h2>
                <h3>Giriş Bilgileri:</h3>
                <p>burhansayan@bbsk.com / 123456 (Tam Yetki)</p>
                <p>ertantuncel@bbsk.com / 123456 (Tam Yetki)</p>
                <p>ozgurdemir@bbsk.com / 123456 (Yakuplu)</p>
                <p>eftelyakose@bbsk.com / 123456 (Yakuplu)</p>
                <p>sezerkaya@bbsk.com / 123456 (Emlak Konut)</p>
                <a href='/Antrenor/Giris' style='display:inline-block;margin-top:20px;padding:10px20px;background:#0B2A4A;color:white;text-decoration:none;border-radius:5px'>Giriş Yap</a>
            </body>
            </html>
        ", "text/html");
    }

    // ========== ÇIKIŞ ==========
    public IActionResult Cikis()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Giris");
    }
}