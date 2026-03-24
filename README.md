```
██████╗  █████╗  ██████╗██╗  ██╗███████╗████████╗██╗  ██╗
██╔══██╗██╔══██╗██╔════╝██║ ██╔╝██╔════╝╚══██╔══╝╚██╗██╔╝
██████╔╝███████║██║     █████╔╝ █████╗     ██║     ╚███╔╝  
██╔═══╝ ██╔══██║██║     ██╔═██╗ ██╔══╝     ██║     ██╔██╗
██║     ██║  ██║╚██████╗██║  ██╗███████╗   ██║    ██╔╝ ██╗
╚═╝     ╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝╚══════╝   ╚═╝    ╚═╝  ╚═╝
```

# PacketX Ultimate v3.1

PacketX Ultimate, C# (.NET Framework 4.8) ile geliştirilmiş, çok modüllü bir siber güvenlik analiz aracıdır.
Toplam 30 bağımsız araç içerir ve ağ, sistem ile uygulama katmanında analiz yapar.

Bu yazılım saldırı amaçlı değil, doğrudan savunma ve analiz perspektifiyle geliştirilmiştir.

---

## Genel Bakış

PacketX, farklı güvenlik araçlarının temel işlevlerini tek bir uygulamada toplayan terminal tabanlı bir platformdur.
Her modül bağımsız çalışır ve hedef sistem üzerinde doğrudan test gerçekleştirebilir.

---

## Temel Özellikler

* Kritik ve geniş kapsamlı port tarama
* HTTP güvenlik başlıklarının analizi
* SSL/TLS sertifika inceleme
* DNS (A, MX, TXT) kayıt analizi
* Alt alan adı (subdomain) keşfi
* Canlı paket yakalama
* Dosya bütünlüğü kontrolü (SHA256)
* Zararlı hash kontrolü
* Basit web zafiyet testleri (XSS)
* Sistem ve donanım izleme
* Registry ve başlangıç analizi
* Log inceleme (başarısız girişler)

---

## Mimari

Tüm araçlar ortak bir arayüzü uygular:

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    void Run(ModuleContext context);
}
```

Bu yapı sayesinde modüller dinamik olarak yönetilir ve merkezi menüden çalıştırılır.

---

## Bağımlılıklar

* .NET Framework 4.8
* SharpPcap
* PacketDotNet
* System.Management
* System.Security.Cryptography
* System.Net

---

## Kurulum

Projeyi klonla:

```bash
git clone https://github.com/kullaniciadi/PacketX.git
```

Visual Studio ile aç ve derle:

```
Ctrl + Shift + B
```

---

## Kullanım

1. Programı çalıştır
2. Menüden araç seç
3. Hedef IP veya domain gir
4. Analiz sonuçlarını terminalde görüntüle

---

## Örnek Modüller

* NetworkAnalyzer → Port tarama
* HttpAnalyzer → Güvenlik header kontrolü
* PacketSniffer → Ağ trafiği yakalama
* DNSAnalyzer → DNS kayıt analizi
* SSLAnalyzer → Sertifika inceleme
* IntegrityMonitor → Dosya değişim takibi
* WebVulnScanner → Basit zafiyet testleri

---

## Yasal Uyarı

Bu yazılım yalnızca:

* Eğitim
* Güvenlik araştırması
* Yetkili test ortamları

için kullanılmalıdır.

İzinsiz kullanım:

* Türk Ceza Kanunu 243 kapsamında suçtur
* Hukuki sorumluluk doğurur

Geliştirici, kötüye kullanımdan sorumlu değildir.

---

## Lisans

Freeware

Ticari kullanım veya dağıtım için geliştirici ile iletişime geçilmesi önerilir.

---

## Geliştirici

SushForAhki

Ana kaynak: GitHub

---

## Not

Bu proje, gerçek siber güvenlik araçlarının temel mantığını öğretmek için geliştirilmiştir.
Daha ileri kullanım için ek geliştirmeler önerilir.
