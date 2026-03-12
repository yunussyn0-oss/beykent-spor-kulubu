using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SporKulubu.Data;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SporKulubu.Data;
using SporKulubu.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace SporKulubu.Controllers;

public class ExcelController : Controller
{
    private readonly UygulamaDbContext _context;

    public ExcelController(UygulamaDbContext context)
    {
        _context = context;
    }

    // ========== TÜM ÜYELERİ EXCEL'E AKTAR (Filtrelemeli) ==========
    public async Task<IActionResult> UyeleriExcelAktar(int? salonId, int? bransId, int? takimId, int? grupId)
    {
        try
        {
            // Filtreleme
            var query = _context.Uyeler
                .Include(u => u.SporSalonu)
                .Include(u => u.Brans)
                .Include(u => u.Takim)
                .Include(u => u.Grup)
                .Include(u => u.Aidatlar)
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

            // Excel paketi lisansı (EPPlus için)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                // Çalışma sayfası oluştur
                var worksheet = package.Workbook.Worksheets.Add("Sporcu Listesi");

                // Başlıkları yaz
                worksheet.Cells[1, 1].Value = "Id";
                worksheet.Cells[1, 2].Value = "Ad Soyad";
                worksheet.Cells[1, 3].Value = "Telefon";
                worksheet.Cells[1, 4].Value = "Doğum Tarihi";
                worksheet.Cells[1, 5].Value = "Salon";
                worksheet.Cells[1, 6].Value = "Branş";
                worksheet.Cells[1, 7].Value = "Takım";
                worksheet.Cells[1, 8].Value = "Grup";
                worksheet.Cells[1, 9].Value = "Aylık Aidat (TL)";
                worksheet.Cells[1, 10].Value = "Son Ödeme Tarihi";
                worksheet.Cells[1, 11].Value = "Kıyafet Durumu";
                worksheet.Cells[1, 12].Value = "Veli Adı";
                worksheet.Cells[1, 13].Value = "Veli Telefon";

                // Başlık stilleri
                using (var range = worksheet.Cells[1, 1, 1, 13])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 12;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Verileri yaz
                int row = 2;
                foreach (var uye in uyeler)
                {
                    worksheet.Cells[row, 1].Value = uye.Id;
                    worksheet.Cells[row, 2].Value = uye.AdSoyad;
                    worksheet.Cells[row, 3].Value = uye.Telefon;
                    worksheet.Cells[row, 4].Value = uye.DogumTarihi.ToString("dd.MM.yyyy");
                    worksheet.Cells[row, 5].Value = uye.SporSalonu?.Ad ?? "-";
                    worksheet.Cells[row, 6].Value = uye.Brans?.Ad ?? "-";
                    worksheet.Cells[row, 7].Value = uye.Takim?.Ad ?? "-";
                    worksheet.Cells[row, 8].Value = uye.Grup?.Ad ?? "-";
                    worksheet.Cells[row, 9].Value = uye.AylikAidat;
                    worksheet.Cells[row, 10].Value = uye.SonAidatOdemeTarihi?.ToString("dd.MM.yyyy") ?? "-";
                    worksheet.Cells[row, 11].Value = uye.KiyafetVerildiMi ? "Verildi" : "Verilmedi";
                    worksheet.Cells[row, 12].Value = uye.VeliAdSoyad;
                    worksheet.Cells[row, 13].Value = uye.VeliTelefon;

                    // Para birimi formatı
                    worksheet.Cells[row, 9].Style.Numberformat.Format = "₺#,##0.00";

                    // Tarih formatı
                    worksheet.Cells[row, 4].Style.Numberformat.Format = "dd.MM.yyyy";
                    worksheet.Cells[row, 10].Style.Numberformat.Format = "dd.MM.yyyy";

                    row++;
                }

                // Sütun genişliklerini otomatik ayarla
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Alternatif satır renklendirme
                for (int i = 2; i < row; i++)
                {
                    if (i % 2 == 0)
                    {
                        worksheet.Cells[i, 1, i, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[i, 1, i, 13].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    }
                }

                // Border ekle
                worksheet.Cells[1, 1, row - 1, 13].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[1, 1, row - 1, 13].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[1, 1, row - 1, 13].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[1, 1, row - 1, 13].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                // Excel dosyasını indir
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                // Dosya adı oluştur (tarih ve filtre bilgisi ile)
                string fileName = $"Sporcu_Listesi_{DateTime.Now:yyyyMMdd_HHmmss}";
                
                if (salonId.HasValue)
                {
                    var salon = await _context.SporSalonlari.FindAsync(salonId);
                    if (salon != null)
                        fileName += $"_{salon.Ad.Replace(" ", "_")}";
                }
                
                fileName += ".xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        catch (Exception ex)
        {
            TempData["Hata"] = "Excel oluşturulurken bir hata oluştu: " + ex.Message;
            return RedirectToAction("Index", "Uyeler");
        }
    }

    // ========== BORÇLU ÜYELERİ EXCEL'E AKTAR ==========
    public async Task<IActionResult> BorcluUyelerExcel()
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
                .OrderByDescending(u => u.Aidatlar!.Where(a => !a.OdendiMi).Sum(a => a.Tutar))
                .ToListAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Borçlu Üyeler");

                worksheet.Cells[1, 1].Value = "Id";
                worksheet.Cells[1, 2].Value = "Ad Soyad";
                worksheet.Cells[1, 3].Value = "Telefon";
                worksheet.Cells[1, 4].Value = "Salon";
                worksheet.Cells[1, 5].Value = "Branş";
                worksheet.Cells[1, 6].Value = "Takım";
                worksheet.Cells[1, 7].Value = "Grup";
                worksheet.Cells[1, 8].Value = "Kalan Borç (TL)";
                worksheet.Cells[1, 9].Value = "Veli Adı";
                worksheet.Cells[1, 10].Value = "Veli Telefon";

                using (var range = worksheet.Cells[1, 1, 1, 10])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                int row = 2;
                foreach (var uye in uyeler)
                {
                    worksheet.Cells[row, 1].Value = uye.Id;
                    worksheet.Cells[row, 2].Value = uye.AdSoyad;
                    worksheet.Cells[row, 3].Value = uye.Telefon;
                    worksheet.Cells[row, 4].Value = uye.SporSalonu?.Ad ?? "-";
                    worksheet.Cells[row, 5].Value = uye.Brans?.Ad ?? "-";
                    worksheet.Cells[row, 6].Value = uye.Takim?.Ad ?? "-";
                    worksheet.Cells[row, 7].Value = uye.Grup?.Ad ?? "-";
                    worksheet.Cells[row, 8].Value = uye.ToplamBorc;
                    worksheet.Cells[row, 9].Value = uye.VeliAdSoyad;
                    worksheet.Cells[row, 10].Value = uye.VeliTelefon;

                    worksheet.Cells[row, 8].Style.Numberformat.Format = "₺#,##0.00";
                    worksheet.Cells[row, 8].Style.Font.Color.SetColor(Color.Red);
                    worksheet.Cells[row, 8].Style.Font.Bold = true;

                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string fileName = $"Borclu_Uyeler_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        catch (Exception ex)
        {
            TempData["Hata"] = "Excel oluşturulurken bir hata oluştu: " + ex.Message;
            return RedirectToAction("BorcluUyeler", "Uyeler");
        }
    }
}