using Microsoft.AspNetCore.Mvc;

namespace SporKulubu.Controllers;

public class AntrenorController : Controller
{
    public IActionResult Giris()
    {
        return View();
    }
    
    [HttpPost]
    public IActionResult Giris(string email, string sifre)
    {
        if (email == "burhan@beykentspor.com" && sifre == "123456")
        {
            return RedirectToAction("Panel");
        }
        ViewBag.Hata = "Hatalı giriş!";
        return View();
    }
    
    public IActionResult Panel()
    {
        return Content("HOŞ GELDİN! PANEL SAYFASI");
    }
    
    public IActionResult Cikis()
    {
        return RedirectToAction("Giris");
    }
}