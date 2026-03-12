using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SporKulubu.Models;

public class Uye
{
    public int Id { get; set; }
    
    // Kişisel Bilgiler
    [Display(Name = "Ad Soyad")]
    [Required(ErrorMessage = "Ad Soyad zorunludur")]
    public string AdSoyad { get; set; } = string.Empty;
    
    [Display(Name = "Telefon")]
    public string Telefon { get; set; } = string.Empty;
    
    [Display(Name = "Doğum Tarihi")]
    [DataType(DataType.Date)]
    public DateTime DogumTarihi { get; set; }
    
    [Display(Name = "Branş")]
    public string Branş { get; set; } = string.Empty;
    
    [Display(Name = "Kayıt Tarihi")]
    [DataType(DataType.Date)]
    public DateTime KayitTarihi { get; set; }
    
    [Display(Name = "Aylık Aidat")]
    [DataType(DataType.Currency)]
    public decimal AylikAidat { get; set; }
    
    [Display(Name = "Son Aidat Ödeme Tarihi")]
    [DataType(DataType.Date)]
    public DateTime? SonAidatOdemeTarihi { get; set; }
    
    // Veli Bilgileri
    [Display(Name = "Veli Ad Soyad")]
    public string VeliAdSoyad { get; set; } = string.Empty;
    
    [Display(Name = "Veli Telefon")]
    public string VeliTelefon { get; set; } = string.Empty;
    
    // Spor Salonu
    public int? SporSalonuId { get; set; }
    public SporSalonu? SporSalonu { get; set; }
    
    // Branş
    public int? BransId { get; set; }
    public Brans? Brans { get; set; }
    
    // Takım
    public int? TakimId { get; set; }
    public Takim? Takim { get; set; }
    
    // Grup
    public int? GrupId { get; set; }
    public Grup? Grup { get; set; }
    
    // Kıyafet
    [Display(Name = "Kıyafet Verildi mi?")]
    public bool KiyafetVerildiMi { get; set; }
    
    [Display(Name = "Kıyafet Veriliş Tarihi")]
    [DataType(DataType.Date)]
    public DateTime? KiyafetVerilisTarihi { get; set; }
    
    [Display(Name = "Kıyafet Notları")]
    public string? KiyafetNotlari { get; set; }
    
    // Aidatlar
    public ICollection<Aidat>? Aidatlar { get; set; }
    
    // KALAN BORÇ (Hesaplanan alan)
    [NotMapped]
    public decimal KalanBorc
    {
        get
        {
            if (Aidatlar == null || !Aidatlar.Any())
                return 0;
            return Aidatlar.Where(a => !a.OdendiMi).Sum(a => a.Tutar);
        }
    }
    
    [NotMapped]
    public decimal ToplamBorc => KalanBorc;
}