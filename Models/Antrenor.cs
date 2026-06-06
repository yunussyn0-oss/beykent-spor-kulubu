using System.ComponentModel.DataAnnotations;

namespace SporKulubu.Models;

public class Antrenor
{
    public int Id { get; set; }
    
    [Display(Name = "Ad Soyad")]
    [Required(ErrorMessage = "Ad Soyad zorunludur")]
    public string AdSoyad { get; set; } = string.Empty;
    
    [Display(Name = "E-posta")]
    [DataType(DataType.EmailAddress)]
    [Required(ErrorMessage = "E-posta zorunludur")]
    public string Email { get; set; } = string.Empty;
    
    [Display(Name = "Telefon")]
    public string Telefon { get; set; } = string.Empty;
    
    [Display(Name = "Uzmanlık")]
    public string Uzmanlik { get; set; } = string.Empty;
    
    [Display(Name = "Şifre")]
    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Şifre zorunludur")]
    public string Sifre { get; set; } = string.Empty;
    
    [Display(Name = "Profil Resmi")]
    public string? ProfilResmi { get; set; }
    
    [Display(Name = "Kayıt Tarihi")]
    [DataType(DataType.Date)]
    public DateTime KayitTarihi { get; set; }
    
    public ICollection<AntrenorTakim>? AntrenorTakimlar { get; set; }
}