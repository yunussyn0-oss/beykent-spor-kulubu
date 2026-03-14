using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SporKulubu.Data;
using SporKulubu.Models;
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
    
    [HttpGet]
    public IActionResult Giris()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Panel");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Giris(string email, string sifre)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sifre))
        {
            ViewBag.Hata = "E-posta ve şifre boş bırakılamaz!";
            return View();
        }

        var antrenor = await _context.Antrenorler
            .Include(a => a.AntrenorTakimlar!)
                .ThenInclude(at => at.SporSalonu)
            .FirstOrDefaultAsync(a => a.Email == email && a.Sifre == sifre);

        if (antrenor == null)
        {
            ViewBag.Hata = "E-posta veya şifre hatalı!";
            return View();
        }

        // Tam yetki kontrolü (Burhan ve Ertan)
        bool tamYetki = antrenor.AdSoyad.Contains("Burhan") || antrenor.AdSoyad.Contains("Ertan");

        // Claims oluştur
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, antrenor.Id.ToString()),
            new Claim(ClaimTypes.Name, antrenor.AdSoyad),
            new Claim(ClaimTypes.Email, antrenor.Email),
            new Claim("TamYetki", tamYetki.ToString()),
            new Claim("AntrenorId", antrenor.Id.ToString()),
            new Claim("AntrenorAdi", antrenor.AdSoyad)
        };

        // Yetkili olduğu salonları claim'e ekle
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
        
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties { IsPersistent = true });

        return RedirectToAction("Panel");
    }

    public async Task<IActionResult> Cikis()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Giris");
    }

    // ========== PANEL ==========
    
    [Authorize]
    public async Task<IActionResult> Panel()
    {
        var antrenor = await GetCurrentAntrenor();
        if (antrenor == null)
            return RedirectToAction("Giris");

        bool tamYetki = User.FindFirst("TamYetki")?.Value == "True";
        
        // Yetkili olduğu salonları bul
        var yetkiliSalonIds = new List<int>();
        if (!tamYetki && antrenor.AntrenorTakimlar != null)
        {
            yetkiliSalonIds = antrenor.AntrenorTakimlar
                .Where(at => at.SporSalonuId.HasValue)
                .Select(at => at.SporSalonuId!.Value)
                .ToList();
        }

        // Sporcuları getir (yetkiye göre filtrele)
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

        var sporcular = await query.OrderBy(u => u.AdSoyad).ToListAsync();

        ViewBag.Sporcular = sporcular;
        ViewBag.TamYetki = tamYetki;
        ViewBag.SporcuSayisi = sporcular.Count;

        return View(antrenor);
    }

    // ========== YARDIMCI METOTLAR ==========
    
    private async Task<Antrenor?> GetCurrentAntrenor()
    {
        var idClaim = User.FindFirst("AntrenorId")?.Value;
        if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int id))
            return null;

        return await _context.Antrenorler
            .Include(a => a.AntrenorTakimlar!)
                .ThenInclude(at => at.SporSalonu)
            .FirstOrDefaultAsync(a => a.Id == id);
    }
}