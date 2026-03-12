using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SporKulubu.Data;
using SporKulubu.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace SporKulubu.Controllers;

[Authorize]
public class AntrenorController : Controller
{
    private readonly UygulamaDbContext _context;

    public AntrenorController(UygulamaDbContext context)
    {
        _context = context;
    }

    // ========== GİRİŞ İŞLEMLERİ ==========
    [AllowAnonymous]
    public IActionResult Index()
    {
        return User.Identity?.IsAuthenticated == true 
            ? RedirectToAction("Panel") 
            : RedirectToAction("Giris");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Giris(int? salonId)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Panel");
        }

        ViewBag.GirisSalonId = salonId;
        
        if (salonId.HasValue && salonId.Value > 0)
        {
            var salon = _context.SporSalonlari
                .AsNoTracking()
                .FirstOrDefault(s => s.Id == salonId.Value);
            ViewBag.GirisSalonAdi = salon?.Ad ?? "Spor Salonu";
        }
        else
        {
            ViewBag.GirisSalonAdi = "Spor Salonu";
        }
        
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Giris(string email, string sifre, int? salonId)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sifre))
        {
            ViewBag.Hata = "E-posta ve şifre boş bırakılamaz!";
            await LoadGirisViewData(salonId);
            return View();
        }

        var antrenor = await _context.Antrenorler
            .Include(a => a.AntrenorTakimlar!)
                .ThenInclude(at => at.SporSalonu)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email == email && a.Sifre == sifre);

        if (antrenor == null)
        {
            ViewBag.Hata = "E-posta veya şifre hatalı!";
            await LoadGirisViewData(salonId);
            return View();
        }

        // Yetki kontrolü
        bool tamYetki = antrenor.AdSoyad.Contains("Burhan") || antrenor.AdSoyad.Contains("Ertan");

        if (salonId.HasValue && salonId.Value > 0 && !tamYetki)
        {
            var yetkiliSalonIds = antrenor.AntrenorTakimlar?
                .Where(at => at.SporSalonuId.HasValue)
                .Select(at => at.SporSalonuId!.Value)
                .ToList() ?? new List<int>();

            if (!yetkiliSalonIds.Contains(salonId.Value))
            {
                ViewBag.Hata = "Bu salona giriş yetkiniz bulunmuyor!";
                await LoadGirisViewData(salonId);
                return View();
            }
        }

        // Claims oluştur
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

        if (antrenor.AntrenorTakimlar != null && antrenor.AntrenorTakimlar.Any())
        {
            var yetkiliSalonIds = antrenor.AntrenorTakimlar
                .Where(at => at.SporSalonuId.HasValue)
                .Select(at => at.SporSalonuId!.Value.ToString())
                .Distinct()
                .ToList();
            
            if (yetkiliSalonIds.Any())
            {
                claims.Add(new Claim("YetkiliSalonlar", string.Join(",", yetkiliSalonIds)));
            }
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        HttpContext.Session.Clear();

        return salonId.HasValue && salonId.Value > 0
            ? RedirectToAction("Panel", new { salonId = salonId.Value })
            : RedirectToAction("Panel");
    }

    [AllowAnonymous]
    public async Task<IActionResult> Cikis()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction("Giris");
    }

    // ========== ANA PANEL ==========
    public async Task<IActionResult> Panel(int? salonId, int? takimId, int? grupId)
    {
        var antrenor = await GetCurrentAntrenorAsync();
        if (antrenor == null)
        {
            return RedirectToAction("Giris");
        }

        bool tamYetki = User.FindFirst("TamYetki")?.Value == "True" || 
                        antrenor.AdSoyad.Contains("Burhan") || 
                        antrenor.AdSoyad.Contains("Ertan");

        var query = _context.Uyeler
            .Include(u => u.SporSalonu)
            .Include(u => u.Brans)
            .Include(u => u.Takim)
            .Include(u => u.Grup)
            .Include(u => u.Aidatlar)
            .AsNoTracking()
            .AsQueryable();

        // Yetki filtresi
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
                
                if (!salonId.HasValue && yetkiliSalonIds.Count == 1)
                {
                    salonId = yetkiliSalonIds.First();
                }
            }
        }

        // Filtreler
        if (salonId.HasValue && salonId.Value > 0)
        {
            query = query.Where(u => u.SporSalonuId == salonId.Value);
        }
        
        if (takimId.HasValue && takimId.Value > 0)
        {
            query = query.Where(u => u.TakimId == takimId.Value);
        }
        
        if (grupId.HasValue && grupId.Value > 0)
        {
            query = query.Where(u => u.GrupId == grupId.Value);
        }

        var sporcular = await query.OrderBy(u => u.AdSoyad).ToListAsync();

        await LoadPanelViewData(salonId, takimId, grupId, tamYetki, antrenor);

        ViewBag.Sporcular = sporcular;
        ViewBag.SporcuSayisi = sporcular.Count;
        ViewBag.TamYetki = tamYetki;

        return View(antrenor);
    }

    // ========== AJAX METOTLARI ==========
    [HttpGet]
    public async Task<IActionResult> TakimlariGetir(int salonId)
    {
        try
        {
            var takimlar = await _context.Takimlar
                .Where(t => t.SporSalonuId == salonId)
                .OrderBy(t => t.Ad)
                .Select(t => new { t.Id, t.Ad })
                .AsNoTracking()
                .ToListAsync();
            
            return Json(takimlar);
        }
        catch
        {
            return Json(new List<object>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> GruplariGetir(int takimId)
    {
        try
        {
            var gruplar = await _context.Gruplar
                .Where(g => g.TakimId == takimId)
                .OrderBy(g => g.Ad)
                .Select(g => new { g.Id, g.Ad })
                .AsNoTracking()
                .ToListAsync();
            
            return Json(gruplar);
        }
        catch
        {
            return Json(new List<object>());
        }
    }

    // ========== YOKLAMA İŞLEMLERİ ==========
    public async Task<IActionResult> Yoklama(DateTime? tarih, int? takimId, int? grupId)
    {
        var antrenor = await GetCurrentAntrenorAsync();
        if (antrenor == null)
        {
            return RedirectToAction("Giris");
        }

        var seciliTarih = tarih ?? DateTime.Now.Date;

        ViewBag.TumTakimlar = new SelectList(
            await _context.Takimlar.AsNoTracking().ToListAsync(), 
            "Id", "Ad", takimId);
        
        if (takimId.HasValue && takimId.Value > 0)
        {
            ViewBag.TumGruplar = new SelectList(
                await _context.Gruplar
                    .Where(g => g.TakimId == takimId.Value)
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "Ad", grupId);
        }

        var query = _context.Uyeler
            .Include(u => u.SporSalonu)
            .Include(u => u.Brans)
            .Include(u => u.Takim)
            .Include(u => u.Grup)
            .AsNoTracking()
            .AsQueryable();

        if (takimId.HasValue && takimId.Value > 0)
        {
            query = query.Where(u => u.TakimId == takimId.Value);
        }
        
        if (grupId.HasValue && grupId.Value > 0)
        {
            query = query.Where(u => u.GrupId == grupId.Value);
        }

        var sporcular = await query.OrderBy(u => u.AdSoyad).ToListAsync();

        var yoklamalar = await _context.Yoklamalar
            .Where(y => y.AntrenorId == antrenor.Id && y.Tarih.Date == seciliTarih.Date)
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
        var antrenor = await GetCurrentAntrenorAsync();
        if (antrenor == null)
        {
            return RedirectToAction("Giris");
        }

        var eskiYoklamalar = await _context.Yoklamalar
            .Where(y => y.AntrenorId == antrenor.Id && y.Tarih.Date == tarih.Date)
            .ToListAsync();

        if (eskiYoklamalar.Any())
        {
            _context.Yoklamalar.RemoveRange(eskiYoklamalar);
        }

        if (yoklamalar != null && yoklamalar.Any())
        {
            foreach (var y in yoklamalar.Where(y => !string.IsNullOrEmpty(y.Durum)))
            {
                y.AntrenorId = antrenor.Id;
                y.Tarih = tarih.Date;
                _context.Yoklamalar.Add(y);
            }
        }

        await _context.SaveChangesAsync();
        TempData["Basarili"] = "Yoklama başarıyla kaydedildi!";

        return RedirectToAction("Yoklama", new { tarih, takimId, grupId });
    }

    // ========== DETAY SAYFALARI ==========
    public async Task<IActionResult> VeliDetay(int id)
    {
        var uye = await _context.Uyeler
            .Include(u => u.SporSalonu)
            .Include(u => u.Brans)
            .Include(u => u.Takim)
            .Include(u => u.Grup)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (uye == null)
        {
            return NotFound();
        }

        return View(uye);
    }

    public async Task<IActionResult> VeliMesaj(int? takimId, int? grupId)
    {
        ViewBag.TumTakimlar = new SelectList(
            await _context.Takimlar.AsNoTracking().ToListAsync(), 
            "Id", "Ad", takimId);
        
        if (takimId.HasValue && takimId.Value > 0)
        {
            ViewBag.TumGruplar = new SelectList(
                await _context.Gruplar
                    .Where(g => g.TakimId == takimId.Value)
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "Ad", grupId);
        }

        var query = _context.Uyeler
            .Include(u => u.SporSalonu)
            .Include(u => u.Brans)
            .Include(u => u.Takim)
            .Include(u => u.Grup)
            .AsNoTracking()
            .AsQueryable();

        if (takimId.HasValue && takimId.Value > 0)
        {
            query = query.Where(u => u.TakimId == takimId.Value);
        }
        
        if (grupId.HasValue && grupId.Value > 0)
        {
            query = query.Where(u => u.GrupId == grupId.Value);
        }

        var sporcular = await query.OrderBy(u => u.AdSoyad).ToListAsync();

        ViewBag.Sporcular = sporcular;
        ViewBag.SecilenTakimId = takimId;
        ViewBag.SecilenGrupId = grupId;

        return View();
    }

    // ========== İSTATİSTİKLER ==========
    public async Task<IActionResult> Istatistikler(int? takimId, int? grupId)
    {
        ViewBag.TumTakimlar = new SelectList(
            await _context.Takimlar.AsNoTracking().ToListAsync(), 
            "Id", "Ad", takimId);
        
        if (takimId.HasValue && takimId.Value > 0)
        {
            ViewBag.TumGruplar = new SelectList(
                await _context.Gruplar
                    .Where(g => g.TakimId == takimId.Value)
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "Ad", grupId);
        }

        var query = _context.Uyeler
            .Include(u => u.Aidatlar)
            .AsNoTracking()
            .AsQueryable();

        if (takimId.HasValue && takimId.Value > 0)
        {
            query = query.Where(u => u.TakimId == takimId.Value);
        }
        
        if (grupId.HasValue && grupId.Value > 0)
        {
            query = query.Where(u => u.GrupId == grupId.Value);
        }

        var uyeler = await query.ToListAsync();

        ViewBag.ToplamSporcu = uyeler.Count;
        ViewBag.ToplamAidat = uyeler.Sum(u => u.AylikAidat);
        ViewBag.BorcluSporcu = uyeler.Count(u => u.Aidatlar != null && u.Aidatlar.Any(a => !a.OdendiMi));
        ViewBag.KiyafetVerilen = uyeler.Count(u => u.KiyafetVerildiMi);

        return View();
    }

    // ========== GEÇMİŞ YOKLAMALAR ==========
    public async Task<IActionResult> GecmisYoklamalar(DateTime? baslangic, DateTime? bitis)
    {
        var antrenor = await GetCurrentAntrenorAsync();
        if (antrenor == null)
        {
            return RedirectToAction("Giris");
        }

        var query = _context.Yoklamalar
            .Where(y => y.AntrenorId == antrenor.Id)
            .Include(y => y.Uye)
            .AsNoTracking()
            .AsQueryable();

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
            .ThenBy(y => y.Uye != null ? y.Uye.AdSoyad : "")
            .Take(100)
            .ToListAsync();

        ViewBag.Baslangic = baslangic;
        ViewBag.Bitis = bitis;

        return View(yoklamalar);
    }

    // ========== KURULUM METOTLARI ==========
    [AllowAnonymous]
    public async Task<IActionResult> AntrenorleriEkle()
    {
        if (!await _context.Antrenorler.AnyAsync())
        {
            var antrenorler = GetDefaultAntrenorler();
            await _context.Antrenorler.AddRangeAsync(antrenorler);
            await _context.SaveChangesAsync();
            return Content("✅ Antrenörler başarıyla eklendi!");
        }
        return Content("ℹ️ Antrenörler zaten mevcut.");
    }

    [AllowAnonymous]
    public async Task<IActionResult> SifreleriSifirla()
    {
        var antrenorler = await _context.Antrenorler.ToListAsync();
        if (antrenorler.Any())
        {
            foreach (var a in antrenorler)
            {
                a.Sifre = "123456";
            }
            await _context.SaveChangesAsync();
            return Content($"✅ {antrenorler.Count} antrenörün şifresi '123456' olarak sıfırlandı!");
        }
        return Content("❌ Antrenör bulunamadı.");
    }

    // ========== PRIVATE YARDIMCI METOTLAR ==========
    private async Task<Antrenor?> GetCurrentAntrenorAsync()
    {
        var antrenorIdClaim = User.FindFirst("AntrenorId")?.Value;
        if (string.IsNullOrEmpty(antrenorIdClaim) || !int.TryParse(antrenorIdClaim, out int antrenorId))
        {
            return null;
        }

        return await _context.Antrenorler
            .Include(a => a.AntrenorTakimlar!)
                .ThenInclude(at => at.SporSalonu)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == antrenorId);
    }

    private async Task LoadGirisViewData(int? salonId)
    {
        if (salonId.HasValue && salonId.Value > 0)
        {
            var salon = await _context.SporSalonlari
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == salonId.Value);
            ViewBag.GirisSalonAdi = salon?.Ad ?? "Spor Salonu";
        }
        else
        {
            ViewBag.GirisSalonAdi = "Spor Salonu";
        }
        ViewBag.GirisSalonId = salonId;
    }

    private async Task LoadPanelViewData(int? salonId, int? takimId, int? grupId, bool tamYetki, Antrenor antrenor)
    {
        IQueryable<SporSalonu> salonQuery = _context.SporSalonlari.AsNoTracking();
        
        if (!tamYetki && antrenor.AntrenorTakimlar != null && antrenor.AntrenorTakimlar.Any())
        {
            var yetkiliSalonIds = antrenor.AntrenorTakimlar
                .Where(at => at.SporSalonuId.HasValue)
                .Select(at => at.SporSalonuId!.Value)
                .Distinct()
                .ToList();
            
            if (yetkiliSalonIds.Any())
            {
                salonQuery = salonQuery.Where(s => yetkiliSalonIds.Contains(s.Id));
            }
        }

        var salonlar = await salonQuery.ToListAsync();
        ViewBag.SporSalonlari = new SelectList(salonlar, "Id", "Ad", salonId);
        
        if (salonId.HasValue && salonId.Value > 0)
        {
            var seciliSalon = salonlar.FirstOrDefault(s => s.Id == salonId.Value);
            ViewBag.SecilenSalonAdi = seciliSalon?.Ad ?? "Seçili Salon";
            
            ViewBag.Takimlar = new SelectList(
                await _context.Takimlar
                    .Where(t => t.SporSalonuId == salonId.Value)
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "Ad", takimId);
        }
        else
        {
            ViewBag.SecilenSalonAdi = "Tüm Salonlar";
        }
        
        if (takimId.HasValue && takimId.Value > 0)
        {
            ViewBag.Gruplar = new SelectList(
                await _context.Gruplar
                    .Where(g => g.TakimId == takimId.Value)
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "Ad", grupId);
        }

        ViewBag.SecilenSalonId = salonId;
        ViewBag.SecilenTakimId = takimId;
        ViewBag.SecilenGrupId = grupId;
    }

    private List<Antrenor> GetDefaultAntrenorler()
    {
        return new List<Antrenor>
        {
            new Antrenor 
            { 
                AdSoyad = "Burhan Şayan", 
                Email = "burhan@beykentspor.com", 
                Sifre = "123456", 
                Telefon = "0532 111 22 33",
                Uzmanlik = "Tam Yetki",
                KayitTarihi = DateTime.Now
            },
            new Antrenor 
            { 
                AdSoyad = "Ertan Tuncel", 
                Email = "ertan@beykentspor.com", 
                Sifre = "123456", 
                Telefon = "0533 444 55 66",
                Uzmanlik = "Tam Yetki",
                KayitTarihi = DateTime.Now
            },
            new Antrenor 
            { 
                AdSoyad = "Özgür", 
                Email = "ozgur@beykentspor.com", 
                Sifre = "123456", 
                Telefon = "0535 123 45 67",
                Uzmanlik = "Yakuplu For Life",
                KayitTarihi = DateTime.Now
            },
            new Antrenor 
            { 
                AdSoyad = "Eftelya", 
                Email = "eftelya@beykentspor.com", 
                Sifre = "123456", 
                Telefon = "0536 234 56 78",
                Uzmanlik = "Yakuplu For Life",
                KayitTarihi = DateTime.Now
            },
            new Antrenor 
            { 
                AdSoyad = "Sezer", 
                Email = "sezer@beykentspor.com", 
                Sifre = "123456", 
                Telefon = "0537 345 67 89",
                Uzmanlik = "Emlak Konut",
                KayitTarihi = DateTime.Now
            },
            new Antrenor 
            { 
                AdSoyad = "Nesrin", 
                Email = "nesrin@beykentspor.com", 
                Sifre = "123456", 
                Telefon = "0538 456 78 90",
                Uzmanlik = "Neşe Sever",
                KayitTarihi = DateTime.Now
            }
        };
    }
}