using Microsoft.AspNetCore.Mvc;
using SporKulubu.Data;
using Microsoft.EntityFrameworkCore;

namespace SporKulubu.Controllers;

public class AntrenorController : Controller
{
    private readonly UygulamaDbContext _context;

    public AntrenorController(UygulamaDbContext context)
    {
        _context = context;
    }

    // Giriş sayfası
    [HttpGet]
    public IActionResult Giris()
    {
        return View();
    }

    // Giriş yap
    [HttpPost]
    public async Task<IActionResult> Giris(string email, string sifre)
    {
        var antrenor = await _context.Antrenorler
            .FirstOrDefaultAsync(a => a.Email == email && a.Sifre == sifre);

        if (antrenor == null)
        {
            ViewBag.Hata = "E-posta veya şifre hatalı!";
            return View();
        }

        // Session'a kaydet
        HttpContext.Session.SetString("AntrenorAdi", antrenor.AdSoyad);
        HttpContext.Session.SetInt32("AntrenorId", antrenor.Id);

        return RedirectToAction("Panel");
    }

    // Panel
    public IActionResult Panel()
    {
        var antrenorAdi = HttpContext.Session.GetString("AntrenorAdi");
        if (string.IsNullOrEmpty(antrenorAdi))
        {
            return RedirectToAction("Giris");
        }
        
        ViewBag.AntrenorAdi = antrenorAdi;
        return View();
    }

    // Çıkış
    public IActionResult Cikis()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Giris");
    }
}