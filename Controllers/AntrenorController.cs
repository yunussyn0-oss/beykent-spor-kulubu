using Microsoft.AspNetCore.Mvc;

namespace SporKulubu.Controllers;

public class AntrenorController : Controller
{
    public IActionResult Giris()
    {
        return Content("Giriş sayfası çalışıyor! /Giris çalışıyor");
    }
    
    public IActionResult Panel()
    {
        return Content("Panel sayfası çalışıyor!");
    }
}