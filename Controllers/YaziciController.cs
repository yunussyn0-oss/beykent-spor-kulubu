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

    // ========== SPORCU LİSTESİNİ YAZDIRMAYA HAZIRLA ==========
    public async Task<IActionResult> Yazdir(int? salonId, int? bransId, int? takimId, int? grupId)
    {
        try
        {
            // Sorguyu oluştur
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

            // HTML içeriği oluştur
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='UTF-8'>");
            sb.AppendLine("<title>Beykent Spor Kulübü - Sporcu Listesi</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("    body { font-family: 'Segoe UI', Arial, sans-serif; margin: 30px; color: #333; }");
            sb.AppendLine("    .header { text-align: center; margin-bottom: 30px; }");
            sb.AppendLine("    h1 { color: #0d6efd; margin-bottom: 5px; }");
            sb.AppendLine("    .subtitle { color: #6c757d; font-size: 14px; margin-bottom: 20px; }");
            sb.AppendLine("    .filters { background: #f8f9fa; padding: 15px; border-radius: 8px; margin-bottom: 25px; }");
            sb.AppendLine("    .filters p { margin: 5px 0; }");
            sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-top: 20px; font-size: 14px; }");
            sb.AppendLine("    th { background: #0d6efd; color: white; padding: 12px; text-align: left; }");
            sb.AppendLine("    td { border: 1px solid #dee2e6; padding: 10px; }");
            sb.AppendLine("    tr:nth-child(even) { background-color: #f8f9fa; }");
            sb.AppendLine("    .total { margin-top: 20px; font-weight: bold; text-align: right; }");
            sb.AppendLine("    .footer { margin-top: 30px; text-align: center; color: #6c757d; font-size: 12px; }");
            sb.AppendLine("    @media print { .no-print { display: none; } }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            
            // Başlık
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("    <h1>🏐 Beykent Spor Kulübü</h1>");
            sb.AppendLine("    <div class='subtitle'>Sporcu Listesi</div>");
            sb.AppendLine("</div>");

            // Filtre bilgileri
            sb.AppendLine("<div class='filters'>");
            sb.AppendLine("    <p><strong>Rapor Tarihi:</strong> " + DateTime.Now.ToString("dd MMMM yyyy HH:mm") + "</p>");
            sb.AppendLine("    <p><strong>Toplam Sporcu:</strong> " + uyeler.Count + "</p>");
            
            if (salonId.HasValue && salonId > 0)
            {
                var salon = await _context.SporSalonlari.FindAsync(salonId);
                sb.AppendLine("    <p><strong>Filtre:</strong> " + (salon?.Ad ?? "Seçili Salon") + "</p>");
            }
            
            sb.AppendLine("</div>");

            // Tablo
            sb.AppendLine("<table>");
            sb.AppendLine("    <thead>");
            sb.AppendLine("        <tr>");
            sb.AppendLine("            <th>#</th>");
            sb.AppendLine("            <th>Ad Soyad</th>");
            sb.AppendLine("            <th>Telefon</th>");
            sb.AppendLine("            <th>Doğum Tarihi</th>");
            sb.AppendLine("            <th>Salon</th>");
            sb.AppendLine("            <th>Branş</th>");
            sb.AppendLine("            <th>Takım</th>");
            sb.AppendLine("            <th>Grup</th>");
            sb.AppendLine("            <th>Aidat</th>");
            sb.AppendLine("            <th>Son Ödeme</th>");
            sb.AppendLine("            <th>Kıyafet</th>");
            sb.AppendLine("            <th>Veli Adı</th>");
            sb.AppendLine("            <th>Veli Telefon</th>");
            sb.AppendLine("        </tr>");
            sb.AppendLine("    </thead>");
            sb.AppendLine("    <tbody>");

            int index = 0;
            foreach (var uye in uyeler)
            {
                index++;
                sb.AppendLine("        <tr>");
                sb.AppendLine("            <td>" + index + "</td>");
                sb.AppendLine("            <td><strong>" + uye.AdSoyad + "</strong></td>");
                sb.AppendLine("            <td>" + uye.Telefon + "</td>");
                sb.AppendLine("            <td>" + uye.DogumTarihi.ToString("dd.MM.yyyy") + "</td>");
                sb.AppendLine("            <td>" + (uye.SporSalonu?.Ad ?? "-") + "</td>");
                sb.AppendLine("            <td>" + (uye.Brans?.Ad ?? "-") + "</td>");
                sb.AppendLine("            <td>" + (uye.Takim?.Ad ?? "-") + "</td>");
                sb.AppendLine("            <td>" + (uye.Grup?.Ad ?? "-") + "</td>");
                sb.AppendLine("            <td>" + uye.AylikAidat.ToString("C") + "</td>");
                sb.AppendLine("            <td>" + (uye.SonAidatOdemeTarihi?.ToString("dd.MM.yyyy") ?? "-") + "</td>");
                sb.AppendLine("            <td>" + (uye.KiyafetVerildiMi ? "Verildi" : "Verilmedi") + "</td>");
                sb.AppendLine("            <td>" + uye.VeliAdSoyad + "</td>");
                sb.AppendLine("            <td>" + uye.VeliTelefon + "</td>");
                sb.AppendLine("        </tr>");
            }

            sb.AppendLine("    </tbody>");
            sb.AppendLine("</table>");

            // Toplam bilgisi
            sb.AppendLine("<div class='total'>");
            sb.AppendLine("    <p>Toplam Sporcu: <strong>" + uyeler.Count + "</strong></p>");
            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("    <p>© " + DateTime.Now.Year + " Beykent Spor Kulübü</p>");
            sb.AppendLine("</div>");

            // Yazdır butonu
            sb.AppendLine("<div class='no-print' style='text-align: center; margin: 20px;'>");
            sb.AppendLine("    <button onclick='window.print()' style='background: #0d6efd; color: white; border: none; padding: 10px 30px; border-radius: 5px; font-size: 16px; cursor: pointer;'>");
            sb.AppendLine("        🖨️ Yazdır / PDF Olarak Kaydet");
            sb.AppendLine("    </button>");
            sb.AppendLine("</div>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return Content(sb.ToString(), "text/html");
        }
        catch (Exception ex)
        {
            TempData["Hata"] = "Yazdırma sayfası hazırlanırken bir hata oluştu: " + ex.Message;
            return RedirectToAction("Index", "Uyeler");
        }
    }

    // ========== BORÇLU ÜYELERİ YAZDIR ==========
    public async Task<IActionResult> BorcluYazdir()
    {
        try
        {
            var uyeler = await _context.Uyeler
                .Include(u => u.SporSalonu)
                .Include(u => u.Brans)
                .Include(u => u.Takim)
                .Include(u => u.Grup)
                .Include(u => u.Aidatlar)
                .Where(u => u.Aidatlar != null && u.Aidatlar.Any(a => !a.OdendiMi))
                .OrderByDescending(u => u.Aidatlar.Where(a => !a.OdendiMi).Sum(a => a.Tutar))
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head>");
            sb.AppendLine("<meta charset='UTF-8'>");
            sb.AppendLine("<title>Borçlu Üyeler</title>");
            sb.AppendLine("<style>body{font-family:Arial;margin:30px} h1{color:#dc3545} table{width:100%;border-collapse:collapse} th{background:#dc3545;color:white;padding:10px} td{border:1px solid #ddd;padding:8px} .total{font-weight:bold;margin-top:20px}</style>");
            sb.AppendLine("</head><body>");
            
            sb.AppendLine("<h1>Borçlu Üyeler</h1>");
            sb.AppendLine("<p>Tarih: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + "</p>");
            sb.AppendLine("<p>Toplam Borçlu Üye: " + uyeler.Count + "</p>");
            
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>#</th><th>Ad Soyad</th><th>Telefon</th><th>Salon</th><th>Branş</th><th>Takım</th><th>Grup</th><th>Borç Miktarı</th><th>Veli Telefon</th></tr>");

            int index = 0;
            foreach (var uye in uyeler)
            {
                index++;
                decimal borc = uye.Aidatlar?.Where(a => !a.OdendiMi).Sum(a => a.Tutar) ?? 0;
                
                sb.AppendLine("<tr>");
                sb.AppendLine("<td>" + index + "</td>");
                sb.AppendLine("<td><strong>" + uye.AdSoyad + "</strong></td>");
                sb.AppendLine("<td>" + uye.Telefon + "</td>");
                sb.AppendLine("<td>" + (uye.SporSalonu?.Ad ?? "-") + "</td>");
                sb.AppendLine("<td>" + (uye.Brans?.Ad ?? "-") + "</td>");
                sb.AppendLine("<td>" + (uye.Takim?.Ad ?? "-") + "</td>");
                sb.AppendLine("<td>" + (uye.Grup?.Ad ?? "-") + "</td>");
                sb.AppendLine("<td><strong style='color:#dc3545'>" + borc.ToString("C") + "</strong></td>");
                sb.AppendLine("<td>" + uye.VeliTelefon + "</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<div style='text-align:center;margin-top:20px'><button onclick='window.print()'>Yazdır</button></div>");
            sb.AppendLine("</body></html>");

            return Content(sb.ToString(), "text/html");
        }
        catch (Exception ex)
        {
            TempData["Hata"] = "Hata: " + ex.Message;
            return RedirectToAction("BorcluUyeler", "Uyeler");
        }
    }
}