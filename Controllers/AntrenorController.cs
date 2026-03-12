using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SporKulubu.Data;
using SporKulubu.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace SporKulubu.Controllers;

public class AntrenorController : Controller
{
    private readonly UygulamaDbContext _context;

    public AntrenorController(UygulamaDbContext context)
    {
        _context = context;
    }

    // ========== GİRİŞ İŞLEMLERİ ==========

    // Ana sayfa - giriş yapmışsa panele yönlendir, yoksa login'e
    public IActionResult Index()
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Panel");
        }
        return RedirectToAction("Giris");
    }

    [HttpGet]
    public IActionResult Giris(int? salonId)
    {
        // Zaten giriş yapmışsa panele yönlendir
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Panel");
        }

        // Hangi salondan gelindiğini ViewBag'e aktar
        ViewBag.GirisSalonId = salonId;
        
        // Salon bilgisini al
        if (salonId.HasValue && salonId > 0)
        {
            var salon = _context.SporSalonlari.FirstOrDefault(s => s.Id == salonId);
            ViewBag.GirisSalonAdi = salon?.Ad ?? "Spor Salonu";
        }
        else
        {
            ViewBag.GirisSalonAdi = "Spor Salonu";
        }
        
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Giris(string email, string sifre, int? salonId)
    {
        // Boş alan kontrolü
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sifre))
        {
            ViewBag.Hata = "E-posta ve şifre boş bırakılamaz!";
            ViewBag.GirisSalonId = salonId;
            var salon1 = _context.SporSalonlari.FirstOrDefault(s => s.Id == salonId);
            ViewBag.GirisSalonAdi = salon1?.Ad ?? "Spor Salonu";
            return View();
        }

        // Antrenörü veritabanında ara
        var antrenor = await _context.Antrenorler
            .Include(a => a.AntrenorTakimlar!)
                .ThenInclude(at => at.SporSalonu)
            .FirstOrDefaultAsync(a => a.Email == email && a.Sifre == sifre);

        // Antrenör bulunamadıysa
        if (antrenor == null)
        {
            ViewBag.Hata = "E-posta veya şifre hatalı!";
            ViewBag.GirisSalonId = salonId;
            var salon2 = _context.SporSalonlari.FirstOrDefault(s => s.Id == salonId);
            ViewBag.GirisSalonAdi = salon2?.Ad ?? "Spor Salonu";
            return View();
        }

        // Tam yetki kontrolü (Burhan ve Ertan)
        bool tamYetki = antrenor.AdSoyad.Contains("Burhan") || antrenor.AdSoyad.Contains("Ertan");

        // Eğer belirli bir salondan gelindiyse ve antrenörün yetkisi yoksa
        if (salonId.HasValue && salonId > 0 && !tamYetki)
        {
            // Antrenörün yetkili olduğu salonlar
            var yetkiliSalonIds = antrenor.AntrenorTakimlar?
                .Where(at => at.SporSalonuId.HasValue)
                .Select(at => at.SporSalonuId!.Value)
                .ToList() ?? new List<int>();

            // Eğer bu salona yetkisi yoksa
            if (!yetkiliSalonIds.Contains(salonId.Value))
            {
                ViewBag.Hata = "Bu salona giriş yetkiniz bulunmuyor!";
                ViewBag.GirisSalonId = salonId;
                var salon3 = _context.SporSalonlari.FirstOrDefault(s => s.Id == salonId);
                ViewBag.GirisSalonAdi = salon3?.Ad ?? "Spor Salonu";
                return View();
            }
        }

        // CLAIM'ler oluştur (Session yerine Cookie authentication için)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, antrenor.Id.ToString()),
            new Claim(ClaimTypes.Name, antrenor.AdSoyad),
            new Claim(ClaimTypes.Email, antrenor.Email),
            new Claim("TamYetki", tamYetki.ToString()),
            new Claim("AntrenorId", antrenor.Id.ToString()),
            new Claim("AntrenorAdi", antrenor.AdSoyad),
            new Claim("AntrenorEmail", antrenor.Email)
        };

        // Yetkili olduğu salonları claim olarak ekle (opsiyonel)
        if (antrenor.AntrenorTakimlar != null && antrenor.AntrenorTakimlar.Any())
        {
            var yetkiliSalonIds = antrenor.AntrenorTakimlar
                .Where(at => at.SporSalonuId.HasValue)
                .Select(at => at.SporSalonuId.Value.ToString())
                .Distinct()
                .ToList();
            
            claims.Add(new Claim("YetkiliSalonlar", string.Join(",", yetkiliSalonIds)));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true, // Beni hatırla
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Session'dan tamamen kurtulmak için session'ı temizle
        HttpContext.Session.Clear();

        // Eğer salonId varsa, panelde o salonu göster
        if (salonId.HasValue && salonId > 0)
        {
            return RedirectToAction("Panel", new { salonId = salonId });
        }

        return RedirectToAction("Panel");
    }

    public async Task<IActionResult> Cikis()
    {
        // Cookie'yi temizle
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        // Session'ı temizle
        HttpContext.Session.Clear();
        return RedirectToAction("Giris");
    }

    // ========== ANA PANEL (Yetkili) ==========

    [Authorize]
    public async Task<IActionResult> Panel(int? salonId, int? takimId, int? grupId)
    {
        // Claim'lerden antrenör ID'sini al
        var antrenorIdClaim = User.FindFirst("AntrenorId")?.Value;
        if (string.IsNullOrEmpty(antrenorIdClaim) || !int.TryParse(antrenorIdClaim, out int antrenorId))
        {
            return RedirectToAction("Giris");
        }

        // Antrenör bilgilerini getir
        var antrenor = await _context.Antrenorler
            .Include(a => a.AntrenorTakimlar!)
                .ThenInclude(at => at.SporSalonu)
            .FirstOrDefaultAsync(a => a.Id == antrenorId);

        if (antrenor == null)
        {
            return RedirectToAction("Giris");
        }

        // Tam yetki kontrolü (Burhan ve Ertan) - Claim'den de alabiliriz
        bool tamYetki = User.FindFirst("TamYetki")?.Value == "True" || 
                        antrenor.AdSoyad.Contains("Burhan") || 
                        antrenor.AdSoyad.Contains("Ertan");

        // Tüm sporcuları getir (filtreleme ile)
        var query = _context.Uyeler
            .Include(u => u.SporSalonu)
            .Include(u => u.Brans)
            .Include(u => u.Takim)
            .Include(u => u.Grup)
            .Include(u => u.Aidatlar)
            .AsQueryable();

        // Tam yetkisi yoksa sadece kendi salonunu görsün
        if (!tamYetki && antrenor.AntrenorTakimlar != null && antrenor.AntrenorTakimlar.Any())
        {
            var yetkiliSalonIds = antrenor.AntrenorTakimlar
                .Where(at => at.SporSalonuId.HasValue)
                .Select(at => at.SporSalonuId!.Value)
                .Distinct()
                .ToList();
            
            if (yetkiliSalonIds.Any())
            {
                query = query.Where(u => u.SporSalonuId.HasValue && yetkiliSalonIds.Contains(u.SporSalonuId.Value));
                
                // Eğer filtrede salon seçilmemişse, yetkili olduğu salonu varsayılan seç
                if (!salonId.HasValue && yetkiliSalonIds.Count == 1)
                {
                    salonId = yetkiliSalonIds.First();
                }
            }
        }

        // Filtreleme
        if (salonId.HasValue && salonId > 0)
        {
            query = query.Where(u => u.SporSalonuId == salonId);
        }
        
        if (takimId.HasValue && takimId > 0)
        {
            query = query.Where(u => u.TakimId == takimId);
        }
        
        if (grupId.HasValue && grupId > 0)
        {
            query = query.Where(u => u.GrupId == grupId);
        }

        var sporcular = await query.OrderBy(u => u.AdSoyad).ToListAsync();

        // Filtre listeleri (yetkiye göre)
        IQueryable<SporSalonu> salonQuery = _context.SporSalonlari;
        
        if (!tamYetki && antrenor.AntrenorTakimlar != null && antrenor.AntrenorTakimlar.Any())
        {
            var yetkiliSalonIds = antrenor.AntrenorTakimlar
                .Where(at => at.SporSalonuId.HasValue)
                .Select(at => at.SporSalonuId!.Value)
                .Distinct()
                .ToList();
            
            salonQuery = salonQuery.Where(s => yetkiliSalonIds.Contains(s.Id));
        }

        var salonlar = await salonQuery.ToListAsync();
        ViewBag.SporSalonlari = new SelectList(salonlar, "Id", "Ad", salonId);
        
        // Seçili salon adını bul
        if (salonId.HasValue && salonId > 0)
        {
            var seciliSalon = salonlar.FirstOrDefault(s => s.Id == salonId);
            ViewBag.SecilenSalonAdi = seciliSalon?.Ad ?? "Seçili Salon";
        }
        else
        {
            ViewBag.SecilenSalonAdi = "Tüm Salonlar";
        }
        
        if (salonId.HasValue && salonId > 0)
        {
            ViewBag.Takimlar = new SelectList(
                await _context.Takimlar.Where(t => t.SporSalonuId == salonId).ToListAsync(),
                "Id",
                "Ad",
                takimId
            );
        }
        
        if (takimId.HasValue && takimId > 0)
        {
            ViewBag.Gruplar = new SelectList(
                await _context.Gruplar.Where(g => g.TakimId == takimId).ToListAsync(),
                "Id",
                "Ad",
                grupId
            );
        }

        ViewBag.Sporcular = sporcular;
        ViewBag.SporcuSayisi = sporcular.Count;
        ViewBag.SecilenSalonId = salonId;
        ViewBag.SecilenTakimId = takimId;
        ViewBag.SecilenGrupId = grupId;
        ViewBag.TamYetki = tamYetki;

        return View(antrenor);
    }

    // ========== AJAX METOTLARI ==========

    [Authorize]
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
        catch (Exception ex)
        {
            return Json(new { error = ex.Message });
        }
    }

    [Authorize]
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
        catch (Exception ex)
        {
            return Json(new { error = ex.Message });
        }
    }

    // ========== YOKLAMA İŞLEMLERİ ==========

    [Authorize]
    public async Task<IActionResult> Yoklama(DateTime? tarih, int? takimId, int? grupId)
    {
        var antrenorIdClaim = User.FindFirst("AntrenorId")?.Value;
        if (string.IsNullOrEmpty(antrenorIdClaim) || !int.TryParse(antrenorIdClaim, out int antrenorId))
        {
            return RedirectToAction("Giris");
        }

        var seciliTarih = tarih ?? DateTime.Now.Date;

        // Tüm takımları getir
        ViewBag.TumTakimlar = new SelectList(await _context.Takimlar.ToListAsync(), "Id", "Ad", takimId);
        
        if (takimId.HasValue && takimId > 0)
        {
            ViewBag.TumGruplar = new SelectList(
                await _context.Gruplar.Where(g => g.TakimId == takimId).ToListAsync(),
                "Id",
                "Ad",
                grupId
            );
        }

        // Sporcuları getir
        var query = _context.Uyeler
            .Include(u => u.SporSalonu)
            .Include(u => u.Brans)
            .Include(u => u.Takim)
            .Include(u => u.Grup)
            .AsQueryable();

        if (takimId.HasValue && takimId > 0)
        {
            query = query.Where(u => u.TakimId == takimId);
        }
        
        if (grupId.HasValue && grupId > 0)
        {
            query = query.Where(u => u.GrupId == grupId);
        }

        var sporcular = await query.OrderBy(u => u.AdSoyad).ToListAsync();

        // Bugünün yoklamalarını getir
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

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YoklamaKaydet(List<Yoklama> yoklamalar, DateTime tarih, int? takimId, int? grupId)
    {
        var antrenorIdClaim = User.FindFirst("AntrenorId")?.Value;
        if (string.IsNullOrEmpty(antrenorIdClaim) || !int.TryParse(antrenorIdClaim, out int antrenorId))
        {
            return RedirectToAction("Giris");
        }

        // Önce o günün eski yoklamalarını sil
        var eskiYoklamalar = await _context.Yoklamalar
            .Where(y => y.AntrenorId == antrenorId && y.Tarih.Date == tarih.Date)
            .ToListAsync();

        if (eskiYoklamalar.Any())
        {
            _context.Yoklamalar.RemoveRange(eskiYoklamalar);
        }

        // Yeni yoklamaları ekle
        foreach (var y in yoklamalar.Where(y => !string.IsNullOrEmpty(y.Durum)))
        {
            y.AntrenorId = antrenorId;
            y.Tarih = tarih.Date;
            _context.Yoklamalar.Add(y);
        }

        await _context.SaveChangesAsync();
        TempData["Basarili"] = "Yoklama başarıyla kaydedildi!";

        return RedirectToAction("Yoklama", new { tarih = tarih, takimId = takimId, grupId = grupId });
    }

    // ========== VELİ DETAY SAYFASI ==========

    [Authorize]
    public async Task<IActionResult> VeliDetay(int id)
    {
        var uye = await _context.Uyeler
            .Include(u => u.SporSalonu)
            .Include(u => u.Brans)
            .Include(u => u.Takim)
            .Include(u => u.Grup)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (uye == null)
        {
            return NotFound();
        }

        return View(uye);
    }

    // ========== VELİ MESAJ SAYFASI ==========

    [Authorize]
    public async Task<IActionResult> VeliMesaj(int? takimId, int? grupId)
    {
        // Tüm takımları getir
        ViewBag.TumTakimlar = new SelectList(await _context.Takimlar.ToListAsync(), "Id", "Ad", takimId);
        
        if (takimId.HasValue && takimId > 0)
        {
            ViewBag.TumGruplar = new SelectList(
                await _context.Gruplar.Where(g => g.TakimId == takimId).ToListAsync(),
                "Id",
                "Ad",
                grupId
            );
        }

        // Sporcuları getir
        var query = _context.Uyeler
            .Include(u => u.SporSalonu)
            .Include(u => u.Brans)
            .Include(u => u.Takim)
            .Include(u => u.Grup)
            .AsQueryable();

        if (takimId.HasValue && takimId > 0)
        {
            query = query.Where(u => u.TakimId == takimId);
        }
        
        if (grupId.HasValue && grupId > 0)
        {
            query = query.Where(u => u.GrupId == grupId);
        }

        var sporcular = await query.OrderBy(u => u.AdSoyad).ToListAsync();

        ViewBag.Sporcular = sporcular;
        ViewBag.SecilenTakimId = takimId;
        ViewBag.SecilenGrupId = grupId;

        return View();
    }

    // ========== İSTATİSTİKLER ==========

    [Authorize]
    public async Task<IActionResult> Istatistikler(int? takimId, int? grupId)
    {
        // Tüm takımları getir
        ViewBag.TumTakimlar = new SelectList(await _context.Takimlar.ToListAsync(), "Id", "Ad", takimId);
        
        if (takimId.HasValue && takimId > 0)
        {
            ViewBag.TumGruplar = new SelectList(
                await _context.Gruplar.Where(g => g.TakimId == takimId).ToListAsync(),
                "Id",
                "Ad",
                grupId
            );
        }

        // İstatistikler
        var query = _context.Uyeler
            .Include(u => u.Aidatlar)
            .AsQueryable();

        if (takimId.HasValue && takimId > 0)
        {
            query = query.Where(u => u.TakimId == takimId);
        }
        
        if (grupId.HasValue && grupId > 0)
        {
            query = query.Where(u => u.GrupId == grupId);
        }

        var uyeler = await query.ToListAsync();

        ViewBag.ToplamSporcu = uyeler.Count;
        ViewBag.ToplamAidat = uyeler.Sum(u => u.AylikAidat);
        ViewBag.BorcluSporcu = uyeler.Count(u => u.Aidatlar != null && u.Aidatlar.Any(a => !a.OdendiMi));
        ViewBag.KiyafetVerilen = uyeler.Count(u => u.KiyafetVerildiMi);

        return View();
    }

    // ========== GEÇMİŞ YOKLAMALAR ==========

    [Authorize]
    public async Task<IActionResult> GecmisYoklamalar(DateTime? baslangic, DateTime? bitis)
    {
        var antrenorIdClaim = User.FindFirst("AntrenorId")?.Value;
        if (string.IsNullOrEmpty(antrenorIdClaim) || !int.TryParse(antrenorIdClaim, out int antrenorId))
        {
            return RedirectToAction("Giris");
        }

        var query = _context.Yoklamalar
            .Where(y => y.AntrenorId == antrenorId)
            .Include(y => y.Uye)
            .AsQueryable();

        // Tarih filtresi
        if (baslangic.HasValue)
        {
            query = query.Where(y => y.Tarih.Date >= baslangic.Value.Date);
        }
        
        if (bitis.HasValue)
        {
            query = query.Where(y => y.Tarih.Date <= bitis.Value.Date);
        }

        var yoklamalar = await query
            .OrderByDescending(y => y.Tarih)
            .ThenBy(y => y.Uye.AdSoyad)
            .Take(100)
            .ToListAsync();

        ViewBag.Baslangic = baslangic;
        ViewBag.Bitis = bitis;

        return View(yoklamalar);
    }

    // ========== KURULUM METOTLARI ==========
    // Bu metotlar herkese açık olmamalı, yetki kontrolü ekleyelim

    public async Task<IActionResult> AntrenorleriEkle()
    {
        // Sadece geliştirme ortamında çalışsın veya özel bir kontrol ekleyelim
        if (!_context.Antrenorler.Any())
        {
            var antrenorler = new List<Antrenor>
            {
                new Antrenor 
                { 
                    AdSoyad = "Burhan Şayan", 
                    Email = "burhan@beykentspor.com", 
                    Telefon = "0532 111 22 33",
                    Uzmanlik = "Tam Yetki",
                    Sifre = "123456",
                    KayitTarihi = DateTime.Now
                },
                new Antrenor 
                { 
                    AdSoyad = "Ertan Tuncel", 
                    Email = "ertan@beykentspor.com", 
                    Telefon = "0533 444 55 66",
                    Uzmanlik = "Tam Yetki",
                    Sifre = "123456",
                    KayitTarihi = DateTime.Now
                }
            };

            await _context.Antrenorler.AddRangeAsync(antrenorler);
            await _context.SaveChangesAsync();
            return Content("✅ Burhan ve Ertan eklendi! Giriş yapabilirsiniz.");
        }
        return Content("ℹ️ Antrenörler zaten mevcut.");
    }

    public async Task<IActionResult> YeniAntrenorleriEkle()
    {
        // Önce eski antrenörleri ve atamaları temizle
        _context.AntrenorTakimlar.RemoveRange(_context.AntrenorTakimlar);
        _context.Antrenorler.RemoveRange(_context.Antrenorler);
        await _context.SaveChangesAsync();

        // Salonları bul
        var yakuplu = await _context.SporSalonlari.FirstOrDefaultAsync(s => s.Ad.Contains("Yakuplu"));
        var emlakKonut = await _context.SporSalonlari.FirstOrDefaultAsync(s => s.Ad.Contains("Emlak"));
        var neseSever = await _context.SporSalonlari.FirstOrDefaultAsync(s => s.Ad.Contains("Neşe"));

        // Yeni antrenörler
        var antrenorler = new List<Antrenor>
        {
            // Tam yetkili
            new Antrenor { AdSoyad = "Burhan Şayan", Email = "burhan@beykentspor.com", Sifre = "123456", Telefon = "0532 111 22 33", Uzmanlik = "Tam Yetki", KayitTarihi = DateTime.Now },
            new Antrenor { AdSoyad = "Ertan Tuncel", Email = "ertan@beykentspor.com", Sifre = "123456", Telefon = "0533 444 55 66", Uzmanlik = "Tam Yetki", KayitTarihi = DateTime.Now },
            
            // Yakuplu For Life
            new Antrenor { AdSoyad = "Özgür", Email = "ozgur@beykentspor.com", Sifre = "123456", Telefon = "0535 123 45 67", Uzmanlik = "Yakuplu For Life", KayitTarihi = DateTime.Now },
            new Antrenor { AdSoyad = "Eftelya", Email = "eftelya@beykentspor.com", Sifre = "123456", Telefon = "0536 234 56 78", Uzmanlik = "Yakuplu For Life", KayitTarihi = DateTime.Now },
            
            // Emlak Konut
            new Antrenor { AdSoyad = "Sezer", Email = "sezer@beykentspor.com", Sifre = "123456", Telefon = "0537 345 67 89", Uzmanlik = "Emlak Konut", KayitTarihi = DateTime.Now },
            
            // Neşe Sever
            new Antrenor { AdSoyad = "Nesrin", Email = "nesrin@beykentspor.com", Sifre = "123456", Telefon = "0538 456 78 90", Uzmanlik = "Neşe Sever", KayitTarihi = DateTime.Now }
        };

        await _context.Antrenorler.AddRangeAsync(antrenorler);
        await _context.SaveChangesAsync();

        // Atamaları yap
        var atamalar = new List<AntrenorTakim>();

        // Özgür (Yakuplu)
        var ozgur = antrenorler.FirstOrDefault(a => a.AdSoyad == "Özgür");
        if (ozgur != null && yakuplu != null)
        {
            atamalar.Add(new AntrenorTakim { AntrenorId = ozgur.Id, SporSalonuId = yakuplu.Id, AtanmaTarihi = DateTime.Now });
        }

        // Eftelya (Yakuplu)
        var eftelya = antrenorler.FirstOrDefault(a => a.AdSoyad == "Eftelya");
        if (eftelya != null && yakuplu != null)
        {
            atamalar.Add(new AntrenorTakim { AntrenorId = eftelya.Id, SporSalonuId = yakuplu.Id, AtanmaTarihi = DateTime.Now });
        }

        // Sezer (Emlak Konut)
        var sezer = antrenorler.FirstOrDefault(a => a.AdSoyad == "Sezer");
        if (sezer != null && emlakKonut != null)
        {
            atamalar.Add(new AntrenorTakim { AntrenorId = sezer.Id, SporSalonuId = emlakKonut.Id, AtanmaTarihi = DateTime.Now });
        }

        // Nesrin (Neşe Sever)
        var nesrin = antrenorler.FirstOrDefault(a => a.AdSoyad == "Nesrin");
        if (nesrin != null && neseSever != null)
        {
            atamalar.Add(new AntrenorTakim { AntrenorId = nesrin.Id, SporSalonuId = neseSever.Id, AtanmaTarihi = DateTime.Now });
        }

        if (atamalar.Any())
        {
            await _context.AntrenorTakimlar.AddRangeAsync(atamalar);
            await _context.SaveChangesAsync();
        }

        return Content(@"
            <div style='font-family: Arial; padding: 20px;'>
                <h2 style='color: green;'>✅ Antrenörler eklendi!</h2>
                <hr/>
                <h3>TAM YETKİLİ:</h3>
                <p><strong>Burhan Şayan</strong> - burhan@beykentspor.com / 123456</p>
                <p><strong>Ertan Tuncel</strong> - ertan@beykentspor.com / 123456</p>
                <h3 style='margin-top:20px;'>YAKUPLU FOR LIFE:</h3>
                <p><strong>Özgür</strong> - ozgur@beykentspor.com / 123456</p>
                <p><strong>Eftelya</strong> - eftelya@beykentspor.com / 123456</p>
                <h3 style='margin-top:20px;'>EMLAK KONUT:</h3>
                <p><strong>Sezer</strong> - sezer@beykentspor.com / 123456</p>
                <h3 style='margin-top:20px;'>NEŞE SEVER:</h3>
                <p><strong>Nesrin</strong> - nesrin@beykentspor.com / 123456</p>
                <p><a href='/Antrenor/Giris' style='display: inline-block; margin-top: 20px; padding: 10px 20px; background: blue; color: white; text-decoration: none; border-radius: 5px;'>Giriş Sayfasına Git</a></p>
            </div>
        ");
    }

    public async Task<IActionResult> SifreleriSifirla()
    {
        var antrenorler = await _context.Antrenorler.ToListAsync();
        foreach (var a in antrenorler)
        {
            a.Sifre = "123456";
        }
        await _context.SaveChangesAsync();
        return Content($"✅ {antrenorler.Count} antrenörün şifresi '123456' olarak sıfırlandı!");
    }

    public async Task<IActionResult> AntrenorleriSil()
    {
        var antrenorler = await _context.Antrenorler.ToListAsync();
        var atamalar = await _context.AntrenorTakimlar.ToListAsync();
        
        if (atamalar.Any())
        {
            _context.AntrenorTakimlar.RemoveRange(atamalar);
        }
        
        if (antrenorler.Any())
        {
            _context.Antrenorler.RemoveRange(antrenorler);
        }
        
        await _context.SaveChangesAsync();
        
        return Content($"✅ {antrenorler.Count} antrenör ve {atamalar.Count} atama silindi.");
    }
    // ========== DEBUG METOTLARI ==========
[AllowAnonymous]
public async Task<IActionResult> DebugAntrenorler()
{
    var antrenorler = await _context.Antrenorler.ToListAsync();
    string result = "<h2>📋 VERİTABANINDAKİ ANTRENÖRLER</h2>";
    result += "<table border='1' cellpadding='5' style='border-collapse: collapse;'>";
    result += "<tr><th>ID</th><th>Ad Soyad</th><th>Email</th><th>Şifre</th><th>Uzmanlık</th></tr>";
    
    foreach (var a in antrenorler)
    {
        result += $"<tr>";
        result += $"<td>{a.Id}</td>";
        result += $"<td>{a.AdSoyad}</td>";
        result += $"<td>{a.Email}</td>";
        result += $"<td>{a.Sifre}</td>";
        result += $"<td>{a.Uzmanlik}</td>";
        result += $"</tr>";
    }
    result += "</table>";
    
    result += "<hr>";
    result += "<h3>➕ Yeni Antrenör Ekle</h3>";
    result += "<form method='post' action='/Antrenor/DebugEkle'>";
    result += "Ad Soyad: <input type='text' name='adSoyad' value='Test Antrenör'><br>";
    result += "Email: <input type='email' name='email' value='test@test.com'><br>";
    result += "Şifre: <input type='text' name='sifre' value='123456'><br>";
    result += "Uzmanlık: <input type='text' name='uzmanlik' value='Test'><br>";
    result += "<button type='submit'>Ekle</button>";
    result += "</form>";
    
    return Content(result, "text/html");
}

[HttpPost]
[AllowAnonymous]
public async Task<IActionResult> DebugEkle(string adSoyad, string email, string sifre, string uzmanlik)
{
    var yeniAntrenor = new Antrenor
    {
        AdSoyad = adSoyad,
        Email = email.Trim().ToLower(),
        Sifre = sifre, // DÜZ METİN (ileride hash'leyeceğiz)
        Telefon = "0555 555 5555",
        Uzmanlik = uzmanlik,
        KayitTarihi = DateTime.Now
    };
    
    _context.Antrenorler.Add(yeniAntrenor);
    await _context.SaveChangesAsync();
    
    return RedirectToAction("DebugAntrenorler");
}

[AllowAnonymous]
public async Task<IActionResult> DebugGirisTest(string email, string sifre)
{
    var antrenor = await _context.Antrenorler
        .FirstOrDefaultAsync(a => a.Email == email && a.Sifre == sifre);
    
    if (antrenor != null)
    {
        return Content($"✅ GİRİŞ BAŞARILI! ID: {antrenor.Id}, Ad: {antrenor.AdSoyad}");
    }
    else
    {
        return Content($"❌ GİRİŞ BAŞARISIZ! Email: {email}, Şifre: {sifre}");
    }
}
}