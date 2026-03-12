using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SporKulubu.Data;
using SporKulubu.Models;
using System.Text;

namespace SporKulubu.Controllers;

public class YaziciController : Controller
{
    private readonly UygulamaDbContext _context;

    public YaziciController(UygulamaDbContext context)
    {
        _context = context;
    }

    // ========== YAZDIRMA SAYFASI (PDF/Print dostu) ==========
    public async Task<IActionResult> Yazdir(int? salonId, int? bransId, int? takimId, int? grupId)
    {
        try
        {
            // Filtrelere göre üyeleri getir
            var query = _context.Uyeler
                .Include(u => u.SporSalonu)
                .Include(u => u.Brans)
                .Include(u => u.Takim)
                .Include(u => u.Grup)
                .Include(u => u.Aidatlar)
                .AsQueryable();

            // Filtreleme
            if (salonId.HasValue && salonId > 0)
                query = query.Where(u => u.SporSalonuId == salonId);
            
            if (bransId.HasValue && bransId > 0)
                query = query.Where(u => u.BransId == bransId);
            
            if (takimId.HasValue && takimId > 0)
                query = query.Where(u => u.TakimId == takimId);
            
            if (grupId.HasValue && grupId > 0)
                query = query.Where(u => u.GrupId == grupId);

            var uyeler = await query.OrderBy(u => u.AdSoyad).ToListAsync();

            // Filtre bilgilerini ViewBag'e aktar
            if (salonId.HasValue && salonId > 0)
            {
                var salon = await _context.SporSalonlari.FindAsync(salonId);
                ViewBag.FiltreBilgi = salon?.Ad ?? "";
            }
            
            if (bransId.HasValue && bransId > 0)
            {
                var brans = await _context.Branslar.FindAsync(bransId);
                ViewBag.FiltreBilgi += " / " + brans?.Ad;
            }
            
            if (takimId.HasValue && takimId > 0)
            {
                var takim = await _context.Takimlar.FindAsync(takimId);
                ViewBag.FiltreBilgi += " / " + takim?.Ad;
            }
            
            if (grupId.HasValue && grupId > 0)
            {
                var grup = await _context.Gruplar.FindAsync(grupId);
                ViewBag.FiltreBilgi += " / " + grup?.Ad;
            }

            ViewBag.Tarih = DateTime.Now.ToString("dd MMMM yyyy HH:mm");
            ViewBag.ToplamKayit = uyeler.Count;
            ViewBag.ToplamAidat = uyeler.Sum(u => u.AylikAidat);
            ViewBag.BorcluSayisi = uyeler.Count(u => u.Aidatlar != null && u.Aidatlar.Any(a => !a.OdendiMi));

            return View(uyeler);
        }
        catch (Exception ex)
        {
            TempData["Hata"] = "Yazdırma sayfası yüklenirken hata oluştu: " + ex.Message;
            return RedirectToAction("Index", "Uyeler");
        }
    }

    // ========== PDF İNDİR (İsteğe bağlı, ekstra) ==========
    public async Task<IActionResult> PdfIndir(int? salonId, int? bransId, int? takimId, int? grupId)
    {
        try
        {
            // Filtrelere göre üyeleri getir
            var query = _context.Uyeler
                .Include(u => u.SporSalonu)
                .Include(u => u.Brans)
                .Include(u => u.Takim)
                .Include(u => u.Grup)
                .AsQueryable();

            if (salonId.HasValue && salonId > 0)
                query = query.Where(u => u.SporSalonuId == salonId);
            if (bransId.HasValue && bransId > 0)
                query = query.Where(u => u.BransId == bransId);
            if (takimId.HasValue && takimId > 0)
                query = query.Where(u => u.TakimId == takimId);
            if (grupId.HasValue && grupId > 0)
                query = query.Where(u => u.GrupId == grupId);

            var uyeler = await query.OrderBy(u => u.AdSoyad).ToListAsync();

            // HTML içerik oluştur
            StringBuilder html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='UTF-8'>");
            html.AppendLine("<title>Sporcu Listesi</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("h1 { color: #0B2A4A; border-bottom: 2px solid #0B2A4A; padding-bottom: 10px; }");
            html.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            html.AppendLine("th { background: #0B2A4A; color: white; padding: 10px; text-align: left; }");
            html.AppendLine("td { padding: 8px; border-bottom: 1px solid #ddd; }");
            html.AppendLine("tr:hover { background: #f5f5f5; }");
            html.AppendLine(".footer { margin-top: 30px; text-align: center; color: #666; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine($"<h1>BEYKENT SPOR KULÜBÜ - SPORCU LİSTESİ</h1>");
            html.AppendLine($"<p><strong>Tarih:</strong> {DateTime.Now:dd MMMM yyyy HH:mm}</p>");
            html.AppendLine($"<p><strong>Toplam Sporcu:</strong> {uyeler.Count}</p>");
            html.AppendLine($"<p><strong>Toplam Aidat:</strong> {uyeler.Sum(u => u.AylikAidat):C}</p>");

            html.AppendLine("<table>");
            html.AppendLine("<thead><tr>");
            html.AppendLine("<th>#</th><th>Ad Soyad</th><th>Telefon</th><th>Doğum Tarihi</th><th>Salon</th><th>Branş</th><th>Takım</th><th>Grup</th><th>Aidat</th><th>Veli</th>");
            html.AppendLine("</tr></thead><tbody>");

            int sayac = 1;
            foreach (var u in uyeler)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{sayac++}</td>");
                html.AppendLine($"<td>{u.AdSoyad}</td>");
                html.AppendLine($"<td>{u.Telefon}</td>");
                html.AppendLine($"<td>{u.DogumTarihi:dd.MM.yyyy}</td>");
                html.AppendLine($"<td>{u.SporSalonu?.Ad ?? "-"}</td>");
                html.AppendLine($"<td>{u.Brans?.Ad ?? "-"}</td>");
                html.AppendLine($"<td>{u.Takim?.Ad ?? "-"}</td>");
                html.AppendLine($"<td>{u.Grup?.Ad ?? "-"}</td>");
                html.AppendLine($"<td>{u.AylikAidat:C}</td>");
                html.AppendLine($"<td>{u.VeliAdSoyad}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody></table>");
            html.AppendLine($"<div class='footer'>Beykent Spor Kulübü - Tüm hakları saklıdır.</div>");
            html.AppendLine("</body></html>");

            byte[] bytes = Encoding.UTF8.GetBytes(html.ToString());
            return File(bytes, "text/html", $"SporcuListesi_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        }
        catch
        {
            TempData["Hata"] = "PDF oluşturulurken hata oluştu.";
            return RedirectToAction("Index", "Uyeler");
        }
    }
}