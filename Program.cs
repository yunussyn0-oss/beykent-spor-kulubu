using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Port ayarı
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

// Login sayfası
app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <title>Beykent Spor - Giriş</title>
            <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body {
                    background: linear-gradient(145deg, #0B2A4A 0%, #1B3B5C 100%);
                    font-family: 'Segoe UI', Arial, sans-serif;
                    height: 100vh;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                }
                .login-box {
                    background: white;
                    padding: 40px;
                    border-radius: 30px;
                    width: 400px;
                    box-shadow: 0 20px 50px rgba(0,0,0,0.3);
                }
                .logo {
                    text-align: center;
                    margin-bottom: 30px;
                }
                .logo h1 {
                    color: #0B2A4A;
                    font-size: 28px;
                    font-weight: 700;
                }
                .logo p {
                    color: #6B7A8F;
                    margin-top: 5px;
                }
                .form-group {
                    margin-bottom: 20px;
                }
                .form-group label {
                    display: block;
                    font-weight: 600;
                    font-size: 13px;
                    text-transform: uppercase;
                    color: #0B2A4A;
                    margin-bottom: 5px;
                }
                .form-group input {
                    width: 100%;
                    padding: 12px 15px;
                    border: 2px solid #E5E9F0;
                    border-radius: 12px;
                    font-size: 15px;
                    transition: 0.3s;
                }
                .form-group input:focus {
                    border-color: #0B2A4A;
                    outline: none;
                }
                button {
                    width: 100%;
                    padding: 14px;
                    background: linear-gradient(145deg, #0B2A4A, #1B3B5C);
                    color: white;
                    border: none;
                    border-radius: 12px;
                    font-size: 16px;
                    font-weight: 600;
                    cursor: pointer;
                    transition: 0.3s;
                    margin-top: 10px;
                }
                button:hover {
                    transform: translateY(-2px);
                    box-shadow: 0 10px 20px rgba(11,42,74,0.3);
                }
                .info {
                    margin-top: 25px;
                    padding: 15px;
                    background: #F0F4F8;
                    border-radius: 10px;
                    font-size: 14px;
                    color: #0B2A4A;
                }
                .info p {
                    margin: 3px 0;
                }
            </style>
        </head>
        <body>
            <div class='login-box'>
                <div class='logo'>
                    <h1>⚡ BEYKENT SPOR</h1>
                    <p>Antrenör Giriş Paneli</p>
                </div>
                
                <form method='post' action='/giris'>
                    <div class='form-group'>
                        <label>📧 E-POSTA</label>
                        <input type='email' name='email' value='burhan@beykentspor.com' required>
                    </div>
                    
                    <div class='form-group'>
                        <label>🔒 ŞİFRE</label>
                        <input type='password' name='sifre' value='123456' required>
                    </div>
                    
                    <button type='submit'>GİRİŞ YAP</button>
                </form>
                
                <div class='info'>
                    <p><strong>📋 Test Bilgileri:</strong></p>
                    <p>• burhan@beykentspor.com / 123456</p>
                    <p>• ertan@beykentspor.com / 123456</p>
                    <p>• ozgur@beykentspor.com / 123456</p>
                </div>
            </div>
        </body>
        </html>
    ");
});

// Login işlemi
app.MapPost("/giris", async context =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var sifre = form["sifre"].ToString();
    
    // Basit kontrol
    if ((email == "burhan@beykentspor.com" && sifre == "123456") ||
        (email == "ertan@beykentspor.com" && sifre == "123456") ||
        (email == "ozgur@beykentspor.com" && sifre == "123456"))
    {
        context.Response.Redirect("/panel");
    }
    else
    {
        context.Response.Redirect("/?hata=1");
    }
});

// Panel sayfası
app.MapGet("/panel", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <title>Panel - Beykent Spor</title>
            <style>
                body {
                    font-family: 'Segoe UI', Arial, sans-serif;
                    background: #f5f7fa;
                    margin: 0;
                    padding: 40px;
                }
                .container {
                    max-width: 800px;
                    margin: 0 auto;
                }
                .card {
                    background: white;
                    border-radius: 20px;
                    padding: 30px;
                    box-shadow: 0 5px 20px rgba(0,0,0,0.1);
                }
                h1 { color: #0B2A4A; }
                .success {
                    color: green;
                    padding: 10px;
                    background: #e8f5e8;
                    border-radius: 10px;
                }
                .logout {
                    display: inline-block;
                    margin-top: 20px;
                    padding: 10px 20px;
                    background: #dc3545;
                    color: white;
                    text-decoration: none;
                    border-radius: 8px;
                }
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='card'>
                    <h1>✅ GİRİŞ BAŞARILI!</h1>
                    <p class='success'>Hoş geldiniz, panele erişiminiz var.</p>
                    <p>Bu basit bir test sayfasıdır. Ana uygulama daha sonra eklenecek.</p>
                    <a href='/' class='logout'>Çıkış Yap</a>
                </div>
            </div>
        </body>
        </html>
    ");
});

// Hata mesajı ile ana sayfa
app.MapGet("/", async context =>
{
    var hata = context.Request.Query.ContainsKey("hata") ? "E-posta veya şifre hatalı!" : "";
    
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync($@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <title>Beykent Spor - Giriş</title>
            <style>
                * {{ margin: 0; padding: 0; box-sizing: border-box; }}
                body {{
                    background: linear-gradient(145deg, #0B2A4A 0%, #1B3B5C 100%);
                    font-family: 'Segoe UI', Arial, sans-serif;
                    height: 100vh;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                }}
                .login-box {{
                    background: white;
                    padding: 40px;
                    border-radius: 30px;
                    width: 400px;
                    box-shadow: 0 20px 50px rgba(0,0,0,0.3);
                }}
                .logo h1 {{ color: #0B2A4A; font-size: 28px; font-weight: 700; text-align: center; }}
                .logo p {{ color: #6B7A8F; text-align: center; margin: 5px 0 25px; }}
                .error {{
                    background: #FEF2F2;
                    color: #991B1B;
                    padding: 12px;
                    border-radius: 8px;
                    margin-bottom: 20px;
                    text-align: center;
                }}
                .form-group {{ margin-bottom: 20px; }}
                .form-group input {{
                    width: 100%;
                    padding: 12px 15px;
                    border: 2px solid #E5E9F0;
                    border-radius: 12px;
                    font-size: 15px;
                }}
                button {{
                    width: 100%;
                    padding: 14px;
                    background: linear-gradient(145deg, #0B2A4A, #1B3B5C);
                    color: white;
                    border: none;
                    border-radius: 12px;
                    font-size: 16px;
                    font-weight: 600;
                    cursor: pointer;
                }}
                .info {{ margin-top: 25px; padding: 15px; background: #F0F4F8; border-radius: 10px; }}
            </style>
        </head>
        <body>
            <div class='login-box'>
                <div class='logo'>
                    <h1>⚡ BEYKENT SPOR</h1>
                    <p>Antrenör Giriş Paneli</p>
                </div>
                
                {(hata != "" ? $"<div class='error'>❌ {hata}</div>" : "")}
                
                <form method='post' action='/giris'>
                    <div class='form-group'>
                        <input type='email' name='email' placeholder='E-posta adresiniz' value='burhan@beykentspor.com' required>
                    </div>
                    
                    <div class='form-group'>
                        <input type='password' name='sifre' placeholder='Şifreniz' value='123456' required>
                    </div>
                    
                    <button type='submit'>GİRİŞ YAP</button>
                </form>
                
                <div class='info'>
                    <p><strong>📋 Test Bilgileri:</strong></p>
                    <p>• burhan@beykentspor.com / 123456</p>
                    <p>• ertan@beykentspor.com / 123456</p>
                    <p>• ozgur@beykentspor.com / 123456</p>
                </div>
            </div>
        </body>
        </html>
    ");
});

app.Run();