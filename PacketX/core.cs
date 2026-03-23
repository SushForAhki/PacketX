// ====================================================================
// PacketX Ultimate v3.1 – Tek Dosya, 30 Bağımsız Savunma Aracı
// .NET Framework 4.8, C# 7.3, Tamamen Türkçe, Etik Hacking Odaklı 
// SushForAhki Tarafından Geliştirildi
//=========================Geliştirici Notları=========================
//Yanlış Bir Kullanımda Sorumluluk Kabul Edilmez. Tüm Araçlar Savunma Amaçlıdır!
// Bu araçlar, sistem güvenliğini artırmak ve zayıf noktaları tespit etmek için tasarlanmıştır.
// Her araç, belirli bir güvenlik alanına odaklanır ve sonuçları doğrudan ekrana yazdırır.
// Kullanıcılar, bu araçları yalnızca kendi sistemlerinde veya izin verilen ortamlarda kullanmalıdır.
// SushForAhki, bu araçların kötü amaçlarla kullanılmasından sorumlu tutulamaz. 
// ekstra  özel olarak bir lisansız olarak sunulmuştur. Herhangi bir ticari kullanım veya dağıtım için SushForAhki ile iletişime geçilmesi önerilir.
//bu yazılımı sadece eğitim amaçlı kullanın ve izinsiz kullanımda Tck madde 243 kapsamında cezai işlem uygulanılabilir!!!!
//yazılımın ilk ve tek ana kaynağı SushForAhki'nin GitHub deposudur. Başka kaynaklardan gelen sürümler modifiyeli olabilir ve güvenlik riski taşıyabilir
// ====================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;
using SharpPcap;
using PacketDotNet;

namespace PacketX
{
   
    public interface ITool
    {
        string Name { get; }
        string Description { get; }
        void Run(ModuleContext context);
    }

  
    public class ModuleContext
    {
        public string Target { get; set; }
        public bool IsVerbose { get; set; }
    }

    public static class Logger
    {
        private static readonly object _lock = new object();
        public static void Info(string msg) => WriteColored(msg, ConsoleColor.Cyan);
        public static void Success(string msg) => WriteColored(msg, ConsoleColor.Green);
        public static void Warning(string msg) => WriteColored(msg, ConsoleColor.Yellow);
        public static void Error(string msg) => WriteColored(msg, ConsoleColor.Red);
        private static void WriteColored(string msg, ConsoleColor color)
        {
            lock (_lock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
                Console.ResetColor();
            }
        }
        public static void CyberpunkBanner()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(@"
   ██████╗  █████╗  ██████╗██╗  ██╗███████╗████████╗██╗  ██╗    
   ██╔══██╗██╔══██╗██╔════╝██║ ██╔╝██╔════╝╚══██╔══╝╚██╗██╔╝
   ██████╔╝███████║██║     █████╔╝ █████╗     ██║    ╚███╔╝ 
   ██╔═══╝ ██╔══██║██║     ██╔═██╗ ██╔══╝     ██║    ██╔██╗ 
   ██║     ██║  ██║╚██████╗██║  ██╗███████╗   ██║   ██╔╝ ██╗
   ╚═╝     ╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝╚══════╝   ╚═╝   ╚═╝  ╚═╝
");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("      Ultimate Cyber Defense Suite v3.1 - 30 Bağımsız Araç");    
            Console.WriteLine("                Tüm araçlar savunma odaklıdır, zararsızdır.");
            Console.WriteLine("                SushForAhki Tarafından Açık Kaynak(FreeWare) Olarak Sunulmuştur ");
            Console.ResetColor();
        }
    }

   
    public static class DnsHelper
    {
        private static readonly Random _random = new Random();

        public static List<string> GetTXTRecords(string domain)
        {
            return QueryDNS(domain, 16); // TXT = 16
        }

        public static List<string> GetMXRecords(string domain)
        {
            return QueryMX(domain, 15); // MX = 15
        }

        private static List<string> QueryDNS(string domain, int qtype)
        {
            var results = new List<string>();
            try
            {
                byte[] query = BuildQuery(domain, qtype);
                using (var udp = new UdpClient())
                {
                    udp.Client.ReceiveTimeout = 3000;
                    udp.Connect("8.8.8.8", 53);
                    udp.Send(query, query.Length);
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] response = udp.Receive(ref remote);
                    ParseResponse(response, qtype, results);
                }
            }
            catch { }
            return results;
        }

        private static List<string> QueryMX(string domain, int qtype)
        {
            var results = new List<string>();
            try
            {
                byte[] query = BuildQuery(domain, qtype);
                using (var udp = new UdpClient())
                {
                    udp.Client.ReceiveTimeout = 3000;
                    udp.Connect("8.8.8.8", 53);
                    udp.Send(query, query.Length);
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] response = udp.Receive(ref remote);
                    ParseMXResponse(response, results);
                }
            }
            catch { }
            return results;
        }

        private static byte[] BuildQuery(string domain, int qtype)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            ushort id = (ushort)_random.Next(0, ushort.MaxValue);
            bw.Write(IPAddress.HostToNetworkOrder((short)id));
            bw.Write(IPAddress.HostToNetworkOrder((short)0x0100));
            bw.Write(IPAddress.HostToNetworkOrder((short)1));
            bw.Write(IPAddress.HostToNetworkOrder((short)0));
            bw.Write(IPAddress.HostToNetworkOrder((short)0));
            bw.Write(IPAddress.HostToNetworkOrder((short)0));

            string[] parts = domain.Split('.');
            foreach (var part in parts)
            {
                bw.Write((byte)part.Length);
                bw.Write(Encoding.ASCII.GetBytes(part));
            }
            bw.Write((byte)0);
            bw.Write(IPAddress.HostToNetworkOrder((short)qtype));
            bw.Write(IPAddress.HostToNetworkOrder((short)1));

            return ms.ToArray();
        }

        private static void ParseResponse(byte[] response, int qtype, List<string> results)
        {
            int pos = 12;
            while (response[pos] != 0) pos++;
            pos += 5;

            while (pos < response.Length)
            {
                int type = (response[pos] << 8) | response[pos + 1];
                if (type == 16)
                {
                    int rdlength = (response[pos + 8] << 8) | response[pos + 9];
                    int txtlen = response[pos + 10];
                    if (txtlen > 0 && txtlen <= rdlength - 1)
                    {
                        string txt = Encoding.ASCII.GetString(response, pos + 11, txtlen);
                        results.Add(txt);
                    }
                }
                pos += 12 + ((response[pos + 8] << 8) | response[pos + 9]);
            }
        }

        private static void ParseMXResponse(byte[] response, List<string> results)
        {
            int pos = 12;
            while (response[pos] != 0) pos++;
            pos += 5;

            while (pos < response.Length)
            {
                int type = (response[pos] << 8) | response[pos + 1];
                if (type == 15)
                {
                    int preference = (response[pos + 10] << 8) | response[pos + 11];
                    int labelStart = pos + 12;
                    string name = DecodeName(response, labelStart);
                    results.Add($"{name} (öncelik {preference})");
                }
                pos += 12 + ((response[pos + 8] << 8) | response[pos + 9]);
            }
        }

        private static string DecodeName(byte[] data, int pos)
        {
            var sb = new StringBuilder();
            bool compressed = false;
            while (true)
            {
                int len = data[pos];
                if (len == 0) break;
                if ((len & 0xC0) == 0xC0)
                {
                    int offset = ((len & 0x3F) << 8) | data[pos + 1];
                    string rest = DecodeName(data, offset);
                    sb.Append(rest);
                    compressed = true;
                    break;
                }
                else
                {
                    pos++;
                    string part = Encoding.ASCII.GetString(data, pos, len);
                    sb.Append(part);
                    sb.Append('.');
                    pos += len;
                }
            }
            if (compressed) return sb.ToString();
            return sb.ToString().TrimEnd('.');
        }
    }

    // ==================== 30 BAĞIMSIZ ARAÇ ====================
    // Her araç ITool arayüzünü uygular ve Run metodu içinde kendi işlevini gerçekleştirir.
    // Sonuçları doğrudan ekrana yazdırır.

    public class NetworkAnalyzer : ITool
    {
        public string Name => "Ağ Analizörü (Kritik Portlar)";
        public string Description => "21,22,23,25,80,443,445,3389,8080 portlarını tarar.";
        public void Run(ModuleContext ctx)
        {
            Logger.Info("Kritik portlar taranıyor...");
            int[] ports = { 21, 22, 23, 25, 80, 443, 445, 3389, 8080 };
            foreach (int p in ports)
            {
                if (IsPortOpen(ctx.Target, p))
                    Logger.Warning($"Port {p} AÇIK");
                else
                    Logger.Info($"Port {p} kapalı");
            }
        }
        private bool IsPortOpen(string host, int port)
        {
            try
            {
                using (var tcp = new TcpClient())
                {
                    var task = tcp.ConnectAsync(host, port);
                    if (task.Wait(1000) && tcp.Connected) return true;
                }
            }
            catch { }
            return false;
        }
    }

    public class HttpAnalyzer : ITool
    {
        public string Name => "HTTP Güvenlik Analizörü";
        public string Description => "Güvenlik başlıklarını denetler.";
        public void Run(ModuleContext ctx)
        {
            Logger.Info("HTTP güvenlik başlıkları kontrol ediliyor...");
            using (var client = new HttpClient())
            {
                try
                {
                    var resp = client.GetAsync($"https://{ctx.Target}").Result;
                    var headers = resp.Headers;
                    if (headers.Contains("Strict-Transport-Security"))
                        Logger.Success("HSTS mevcut");
                    else
                        Logger.Error("HSTS eksik");
                    if (headers.Contains("Content-Security-Policy"))
                        Logger.Success("CSP mevcut");
                    else
                        Logger.Error("CSP eksik");
                    if (headers.Contains("X-Frame-Options"))
                        Logger.Success("X-Frame-Options mevcut");
                    else
                        Logger.Error("X-Frame-Options eksik");
                    if (headers.Contains("X-Content-Type-Options"))
                        Logger.Success("X-Content-Type-Options mevcut");
                    else
                        Logger.Error("X-Content-Type-Options eksik");
                }
                catch (Exception ex)
                {
                    Logger.Error($"HTTP isteği başarısız: {ex.Message}");
                }
            }
        }
    }

    public class ReputationModule : ITool
    {
        public string Name => "İtibar Zekası";
        public string Description => "Yerel kara listede sorgulama yapar.";
        private readonly string _blacklistPath = "blacklist.txt";
        public void Run(ModuleContext ctx)
        {
            Logger.Info("Kara liste kontrol ediliyor...");
            if (!File.Exists(_blacklistPath)) File.WriteAllText(_blacklistPath, "");
            var lines = File.ReadAllLines(_blacklistPath);
            var hit = lines.FirstOrDefault(l => l.Split(',')[0].Equals(ctx.Target, StringComparison.OrdinalIgnoreCase));
            if (hit != null)
            {
                var parts = hit.Split(',');
                int score = int.Parse(parts[1]);
                Logger.Warning($"Hedef kara listede! Skor: {score}");
            }
            else
                Logger.Success("Hedef kara listede değil.");
        }
    }

    public class IntegrityMonitor : ITool
    {
        public string Name => "Bütünlük İzleyici";
        public string Description => "Kritik sistem dosyalarını izler.";
        private readonly string _baselinePath = "baseline.json";
        private readonly List<string> _files = new List<string> { @"C:\Windows\System32\drivers\etc\hosts", @"C:\Windows\System32\config\SAM" };
        public void Run(ModuleContext ctx)
        {
            Logger.Info("Dosya bütünlüğü kontrol ediliyor...");
            var changes = new List<string>();
            Dictionary<string, string> baseline = new Dictionary<string, string>();
            if (File.Exists(_baselinePath))
            {
                var json = File.ReadAllText(_baselinePath);
                baseline = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            foreach (var f in _files)
            {
                if (!File.Exists(f))
                {
                    changes.Add($"{f} silinmiş veya yok!");
                    continue;
                }
                string hash = ComputeSha256(f);
                if (!baseline.ContainsKey(f))
                {
                    changes.Add($"{f} yeni dosya, hash: {hash}");
                    baseline[f] = hash;
                }
                else if (baseline[f] != hash)
                {
                    changes.Add($"{f} DEĞİŞMİŞ! Eski hash: {baseline[f]}, Yeni hash: {hash}");
                    baseline[f] = hash;
                }
                else
                {
                    Logger.Info($"{f} değişmemiş.");
                }
            }
            File.WriteAllText(_baselinePath, JsonSerializer.Serialize(baseline, new JsonSerializerOptions { WriteIndented = true }));
            foreach (var c in changes)
                Logger.Warning(c);
            if (changes.Count == 0)
                Logger.Success("Tüm dosyalar bütünlüğünü koruyor.");
        }
        private string ComputeSha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLower();
        }
    }

    public class HardwareMonitor : ITool
    {
        public string Name => "Donanım İzleyici";
        public string Description => "CPU, RAM, Disk, Ağ kullanımını gösterir.";
        public void Run(ModuleContext ctx)
        {
            try
            {
                var cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpu.NextValue(); Thread.Sleep(1000);
                float cpuUsage = cpu.NextValue();
                Logger.Info($"CPU Kullanımı: %{cpuUsage:F1}");

                using (var p = Process.GetCurrentProcess())
                {
                    long mem = p.PrivateMemorySize64 / (1024 * 1024);
                    Logger.Info($"Bu işlem Bellek: {mem} MB");
                }

                var drive = new DriveInfo("C");
                float diskUsage = (drive.TotalSize - drive.AvailableFreeSpace) / (float)drive.TotalSize * 100;
                Logger.Info($"Disk (C:) Kullanımı: %{diskUsage:F1}");

                var netSent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", "Ethernet");
                var netRecv = new PerformanceCounter("Network Interface", "Bytes Received/sec", "Ethernet");
                float sent = netSent.NextValue();
                float recv = netRecv.NextValue();
                Thread.Sleep(1000);
                sent = netSent.NextValue();
                recv = netRecv.NextValue();
                Logger.Info($"Ağ Gönderim: {sent:F0} B/s, Alım: {recv:F0} B/s");
            }
            catch (Exception ex)
            {
                Logger.Error($"Donanım bilgisi alınamadı: {ex.Message}");
            }
        }
    }

    public class PacketSniffer : ITool
    {
        public string Name => "Paket Yakalayıcı";
        public string Description => "Canlı ağ trafiğini yakalar (5 sn).";
        public void Run(ModuleContext ctx)
        {
            try
            {
                var devices = CaptureDeviceList.Instance;
                if (devices.Count == 0) throw new Exception("Ağ cihazı bulunamadı.");
                var dev = devices[0];
                dev.Open(DeviceModes.Promiscuous, 1000);
                int count = 0;
                const int max = 10;
                Logger.Info("Paket yakalanıyor (5 saniye)...");
                dev.OnPacketArrival += (s, e) =>
                {
                    if (count >= max) return;
                    var packet = Packet.ParsePacket(e.GetPacket().LinkLayerType, e.GetPacket().Data);
                    var ip = packet.Extract<IPPacket>();
                    if (ip != null)
                        Logger.Info($"{ip.SourceAddress} -> {ip.DestinationAddress} [{ip.Protocol}]");
                    count++;
                };
                dev.StartCapture();
                Thread.Sleep(5000);
                dev.StopCapture();
                dev.Close();
                if (count == 0) Logger.Info("Paket yakalanamadı.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Paket yakalama hatası: {ex.Message}");
            }
        }
    }

    public class DNSAnalyzer : ITool
    {
        public string Name => "DNS Analizörü";
        public string Description => "A, MX, TXT kayıtlarını gösterir.";
        public void Run(ModuleContext ctx)
        {
            try
            {
                var ips = Dns.GetHostAddresses(ctx.Target);
                foreach (var ip in ips) Logger.Info($"A: {ip}");

                var mx = DnsHelper.GetMXRecords(ctx.Target);
                foreach (var m in mx) Logger.Info($"MX: {m}");

                var txt = DnsHelper.GetTXTRecords(ctx.Target);
                foreach (var t in txt) Logger.Info($"TXT: {t}");
            }
            catch (Exception ex)
            {
                Logger.Error($"DNS sorgusu başarısız: {ex.Message}");
            }
        }
    }

    public class WhoisLookup : ITool
    {
        public string Name => "Whois Sorgulama";
        public string Description => "Alan adı kayıt bilgilerini getirir (demo).";
        public void Run(ModuleContext ctx)
        {
            Logger.Info("Whois verisi için harici API gerekli. Demo: Domain Örnek Şirket'e kayıtlı.");
        }
    }

    public class SubdomainEnumerator : ITool
    {
        public string Name => "Alt Alan Adı Bulucu";
        public string Description => "Yaygın alt alan adlarını dener.";
        public void Run(ModuleContext ctx)
        {
            string[] common = { "www", "mail", "ftp", "admin", "blog", "api", "test", "dev", "portal", "secure", "vpn" };
            foreach (var sub in common)
            {
                try
                {
                    var host = $"{sub}.{ctx.Target}";
                    var ip = Dns.GetHostAddresses(host);
                    if (ip.Length > 0) Logger.Success($"Bulundu: {host} -> {ip[0]}");
                }
                catch { }
            }
        }
    }

    public class SSLAnalyzer : ITool
    {
        public string Name => "SSL/TLS Derin Analiz";
        public string Description => "Sertifika detaylarını gösterir.";
        public void Run(ModuleContext ctx)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect(ctx.Target, 443);
                    using (var ssl = new System.Net.Security.SslStream(client.GetStream(), false, (sender, cert, chain, errors) => true))
                    {
                        ssl.AuthenticateAsClient(ctx.Target);
                        var cert = ssl.RemoteCertificate;
                        if (cert != null)
                        {
                            var cert2 = new System.Security.Cryptography.X509Certificates.X509Certificate2(cert);
                            Logger.Info($"Konu: {cert2.Subject}");
                            Logger.Info($"Veren: {cert2.Issuer}");
                            Logger.Info($"Geçerlilik Başlangıç: {cert2.NotBefore}");
                            Logger.Info($"Geçerlilik Bitiş: {cert2.NotAfter}");
                            Logger.Info($"Protokol: {ssl.SslProtocol}");
                            if (cert2.NotAfter < DateTime.Now)
                                Logger.Error("SERTİFİKA SÜRESİ DOLMUŞ!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SSL bağlantısı başarısız: {ex.Message}");
            }
        }
    }

    public class AdvancedPortScanner : ITool
    {
        public string Name => "Gelişmiş Port Tarayıcı";
        public string Description => "1-1024 arası portları tarar (yavaş olabilir).";
        public void Run(ModuleContext ctx)
        {
            Logger.Info("Port taraması başladı (1-1024)...");
            for (int i = 1; i <= 1024; i++)
            {
                if (IsPortOpen(ctx.Target, i))
                    Logger.Warning($"Port {i} AÇIK");
            }
        }
        private bool IsPortOpen(string host, int port)
        {
            try
            {
                using (var tcp = new TcpClient())
                {
                    var task = tcp.ConnectAsync(host, port);
                    if (task.Wait(500)) return tcp.Connected;
                }
            }
            catch { }
            return false;
        }
    }

    public class GeoIP : ITool
    {
        public string Name => "Coğrafi Konum";
        public string Description => "IP'nin konumunu gösterir (ip-api.com).";
        public void Run(ModuleContext ctx)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var resp = client.GetAsync($"http://ip-api.com/json/{ctx.Target}").Result;
                    if (resp.IsSuccessStatusCode)
                    {
                        var json = resp.Content.ReadAsStringAsync().Result;
                        var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        var status = root.GetProperty("status").GetString();
                        if (status == "success")
                        {
                            string country = root.GetProperty("country").GetString();
                            string city = root.GetProperty("city").GetString();
                            string isp = root.GetProperty("isp").GetString();
                            Logger.Info($"Ülke: {country}, Şehir: {city}, ISP: {isp}");
                        }
                        else
                            Logger.Error("Coğrafi konum sorgusu başarısız.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"GeoIP hatası: {ex.Message}");
            }
        }
    }

    public class URLExtractor : ITool
    {
        public string Name => "URL Çıkarıcı";
        public string Description => "Web sayfasındaki bağlantıları çıkarır.";
        public void Run(ModuleContext ctx)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var html = client.GetStringAsync($"http://{ctx.Target}").Result;
                    var matches = Regex.Matches(html, @"(https?://[^\s""']+)", RegexOptions.IgnoreCase);
                    var urls = matches.Cast<Match>().Select(m => m.Value).Distinct().Take(20).ToList();
                    foreach (var u in urls)
                        Logger.Info(u);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"URL çıkarılamadı: {ex.Message}");
            }
        }
    }

    public class DirBruteforce : ITool
    {
        public string Name => "Dizin Kaba Kuvvet";
        public string Description => "Gizli dizinleri bulmaya çalışır.";
        public void Run(ModuleContext ctx)
        {
            string[] common = { "admin", "backup", "config", "wp-admin", "login", "images", "css", "js", "uploads", "tmp" };
            using (var client = new HttpClient())
            {
                foreach (var d in common)
                {
                    try
                    {
                        var resp = client.GetAsync($"http://{ctx.Target}/{d}/").Result;
                        if (resp.StatusCode == HttpStatusCode.OK)
                            Logger.Success($"/{d}/ dizini bulundu!");
                    }
                    catch { }
                }
            }
        }
    }

    public class MalwareHashCheck : ITool
    {
        public string Name => "Zararlı Yazılım Hash Kontrolü";
        public string Description => "Dosya hash'ini kara listede arar.";
        public void Run(ModuleContext ctx)
        {
            Console.Write("Kontrol edilecek dosya yolunu girin: ");
            string path = Console.ReadLine();
            if (File.Exists(path))
            {
                string hash = ComputeSha256(path);
                Logger.Info($"SHA256: {hash}");
                var malwareHashes = File.Exists("malware_hashes.txt") ? File.ReadAllLines("malware_hashes.txt") : new string[0];
                if (malwareHashes.Contains(hash))
                    Logger.Warning("Bu hash zararlı yazılım listesinde bulundu!");
                else
                    Logger.Success("Hash temiz görünüyor.");
            }
            else
                Logger.Error("Dosya bulunamadı.");
        }
        private string ComputeSha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLower();
        }
    }

    public class SystemInfo : ITool
    {
        public string Name => "Sistem Bilgisi";
        public string Description => "İşletim sistemi, donanım bilgileri.";
        public void Run(ModuleContext ctx)
        {
            try
            {
                Logger.Info($"İşletim Sistemi: {Environment.OSVersion}");
                Logger.Info($"Makine Adı: {Environment.MachineName}");
                Logger.Info($"İşlemci Sayısı: {Environment.ProcessorCount}");
                using (var pc = new PerformanceCounter("Memory", "Available MBytes"))
                    Logger.Info($"Boş RAM: {pc.NextValue()} MB");
            }
            catch (Exception ex)
            {
                Logger.Error($"Sistem bilgisi alınamadı: {ex.Message}");
            }
        }
    }

    public class ADCheck : ITool
    {
        public string Name => "Active Directory Kontrolü";
        public string Description => "Domain ve kullanıcı bilgileri.";
        public void Run(ModuleContext ctx)
        {
            try
            {
                using (var entry = new DirectoryEntry("LDAP://RootDSE"))
                {
                    string domain = entry.Properties["defaultNamingContext"].Value.ToString();
                    Logger.Info($"Domain: {domain}");
                }
                using (var user = new DirectoryEntry("WinNT://./Administrator"))
                {
                    Logger.Info("Administrator hesabı mevcut.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"AD hatası: {ex.Message}");
            }
        }
    }

    public class LogAnalyzer : ITool
    {
        public string Name => "Günlük Analizörü";
        public string Description => "Güvenlik günlüklerinde başarısız girişleri arar.";
        public void Run(ModuleContext ctx)
        {
            try
            {
                var log = EventLog.GetEventLogs().FirstOrDefault(l => l.Log == "Security");
                if (log != null)
                {
                    int failures = 0;
                    foreach (EventLogEntry entry in log.Entries)
                    {
                        if (entry.InstanceId == 4625) failures++;
                    }
                    Logger.Info($"Son 24 saatte başarısız giriş sayısı: {failures}");
                }
                else
                    Logger.Error("Güvenlik günlüğüne erişilemedi.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Günlük okuma hatası: {ex.Message}");
            }
        }
    }

    public class EmailSecurityCheck : ITool
    {
        public string Name => "E-posta Güvenliği Kontrolü";
        public string Description => "SPF, DKIM, DMARC kayıtlarını denetler.";
        public void Run(ModuleContext ctx)
        {
            try
            {
                var spf = DnsHelper.GetTXTRecords($"_spf.{ctx.Target}");
                var dkim = DnsHelper.GetTXTRecords($"default._domainkey.{ctx.Target}");
                var dmarc = DnsHelper.GetTXTRecords($"_dmarc.{ctx.Target}");
                if (spf.Any()) Logger.Info($"SPF: {spf.First()}");
                else Logger.Warning("SPF kaydı yok");
                if (dkim.Any()) Logger.Info($"DKIM: {dkim.First()}");
                else Logger.Warning("DKIM kaydı yok");
                if (dmarc.Any()) Logger.Info($"DMARC: {dmarc.First()}");
                else Logger.Warning("DMARC kaydı yok");
            }
            catch (Exception ex)
            {
                Logger.Error($"E-posta kontrolü hatası: {ex.Message}");
            }
        }
    }

    public class PasswordStrength : ITool
    {
        public string Name => "Parola Gücü Testi";
        public string Description => "Girilen parolanın gücünü değerlendirir.";
        public void Run(ModuleContext ctx)
        {
            Console.Write("Test edilecek parolayı girin: ");
            string pwd = Console.ReadLine();
            int score = 0;

            if (pwd.Length >= 8) score++;
            if (pwd.Any(char.IsUpper)) score++;
            if (pwd.Any(char.IsLower)) score++;
            if (pwd.Any(char.IsDigit)) score++;
            if (pwd.Any(ch => !char.IsLetterOrDigit(ch))) score++;

            string strength;

            switch (score)
            {
                case 5:
                    strength = "Çok Güçlü";
                    break;
                case 4:
                    strength = "Güçlü";
                    break;
                case 3:
                    strength = "Orta";
                    break;
                case 2:
                    strength = "Zayıf";
                    break;
                default:
                    strength = "Çok Zayıf";
                    break;
            }

            Logger.Info($"Güç: {strength} (Skor {score}/5)");
        }

        public class WirelessScanner : ITool
        {
            public string Name => "Kablosuz Ağ Tarayıcı";
            public string Description => "Yakındaki Wi-Fi ağlarını listeler (placeholder).";
            public void Run(ModuleContext ctx)
            {
                Logger.Info("Kablosuz tarama için ek kütüphaneler gereklidir. Yer tutucu.");
            }
        }

        public class ProcessAnalyzer : ITool
        {
            public string Name => "İşlem Analizörü";
            public string Description => "Çalışan işlemleri listeler (ilk 10).";
            public void Run(ModuleContext ctx)
            {
                var processes = Process.GetProcesses().OrderByDescending(p => p.WorkingSet64).Take(10);
                foreach (var p in processes)
                {
                    Logger.Info($"{p.ProcessName} (PID {p.Id}) - Bellek: {p.WorkingSet64 / (1024 * 1024)} MB");
                }
            }
        }

        public class StartupItems : ITool
        {
            public string Name => "Başlangıç Öğeleri";
            public string Description => "Windows başlangıcında çalışan programları listeler.";
            public void Run(ModuleContext ctx)
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
                    {
                        if (key != null)
                        {
                            foreach (var val in key.GetValueNames())
                            {
                                Logger.Info($"{val}: {key.GetValue(val)}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Kayıt defteri okuma hatası: {ex.Message}");
                }
            }
        }

        public class RegistryAnalyzer : ITool
        {
            public string Name => "Kayıt Defteri Analizörü";
            public string Description => "Otomatik başlatma konumlarını tarar.";
            public void Run(ModuleContext ctx)
            {
                string[] keys = { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce" };
                foreach (var keyPath in keys)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(keyPath))
                    {
                        if (key != null)
                        {
                            foreach (var val in key.GetValueNames())
                            {
                                Logger.Info($"{keyPath}\\{val}: {key.GetValue(val)}");
                            }
                        }
                    }
                }
            }
        }

        public class NetworkTrafficStats : ITool
        {
            public string Name => "Ağ Trafiği İstatistikleri";
            public string Description => "Anlık ağ trafiği özeti.";
            public void Run(ModuleContext ctx)
            {
                try
                {
                    var netSent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", "Ethernet");
                    var netRecv = new PerformanceCounter("Network Interface", "Bytes Received/sec", "Ethernet");
                    float sent = netSent.NextValue();
                    float recv = netRecv.NextValue();
                    Thread.Sleep(1000);
                    sent = netSent.NextValue();
                    recv = netRecv.NextValue();
                    Logger.Info($"Gönderim: {sent:F0} B/s, Alım: {recv:F0} B/s");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Ağ istatistikleri alınamadı: {ex.Message}");
                }
            }
        }

        public class CVEScanner : ITool
        {
            public string Name => "Zafiyet Tarayıcı";
            public string Description => "Yerel CVE veritabanında bilinen zafiyetleri arar (demo).";
            public void Run(ModuleContext ctx)
            {
                Logger.Info("CVE taraması için harici veritabanı gerekir. Demo: CVE-2021-44228 (Log4Shell) tespit edilmedi.");
            }
        }

        public class DataLeakCheck : ITool
        {
            public string Name => "Veri Sızıntısı Kontrolü";
            public string Description => "E-posta veya alan adının sızıntı veritabanlarında olup olmadığını kontrol eder (demo).";
            public void Run(ModuleContext ctx)
            {
                Logger.Info("Bu modül için Have I Been Pwned API entegrasyonu önerilir. Demo: Veri sızıntısı tespit edilmedi.");
            }
        }

        public class WebVulnScanner : ITool
        {
            public string Name => "Web Güvenlik Açığı Tarayıcı";
            public string Description => "Basit XSS ve SQLi testleri yapar.";
            public void Run(ModuleContext ctx)
            {
                string[] xss = { "<script>alert('XSS')</script>", "\"><script>alert('XSS')</script>" };
                using (var client = new HttpClient())
                {
                    foreach (var payload in xss)
                    {
                        try
                        {
                            var resp = client.GetAsync($"http://{ctx.Target}/?q={Uri.EscapeDataString(payload)}").Result;
                            if (resp.Content.ReadAsStringAsync().Result.Contains(payload))
                                Logger.Warning($"XSS zafiyeti potansiyeli: {payload}");
                        }
                        catch { }
                    }
                }
                Logger.Info("SQLi testleri için manuel inceleme gerekir.");
            }
        }

        public class FileSignatureCheck : ITool
        {
            public string Name => "Dosya İmza Kontrolü";
            public string Description => "Dosyanın imzasını (magic bytes) kontrol eder.";
            public void Run(ModuleContext ctx)
            {
                Console.Write("Kontrol edilecek dosya yolunu girin: ");
                string path = Console.ReadLine();
                if (File.Exists(path))
                {
                    byte[] header = new byte[8];
                    using (var fs = File.OpenRead(path))
                        fs.Read(header, 0, 8);
                    string hex = BitConverter.ToString(header).Replace("-", "");
                    Logger.Info($"İmza (hex): {hex}");
                    if (hex.StartsWith("4D5A")) Logger.Info("PE (Windows executable)");
                    else if (hex.StartsWith("7F454C46")) Logger.Info("ELF (Linux executable)");
                    else if (hex.StartsWith("25504446")) Logger.Info("PDF");
                    else Logger.Info("Bilinmeyen tür");
                }
                else
                    Logger.Error("Dosya bulunamadı.");
            }
        }

        public class EncryptedDataDetector : ITool
        {
            public string Name => "Şifrelenmiş Veri Tespiti";
            public string Description => "Yüksek entropili dosyaları tespit eder (şifreleme göstergesi).";
            public void Run(ModuleContext ctx)
            {
                Console.Write("Kontrol edilecek dosya yolunu girin: ");
                string path = Console.ReadLine();
                if (File.Exists(path))
                {
                    byte[] data = File.ReadAllBytes(path);
                    int distinct = data.Distinct().Count();
                    double entropy = (double)distinct / 256.0;
                    Logger.Info($"Entropi: {entropy:F2} (0-1 arası, yüksek şifreleme işareti)");
                    if (entropy > 0.9) Logger.Warning("Dosya şifrelenmiş olabilir.");
                }
                else
                    Logger.Error("Dosya bulunamadı.");
            }
        }

        // ==================== ANA PROGRAM (MENÜ) ====================
        class Program
        {
            static void Main()
            {
                Logger.CyberpunkBanner();
                Console.WriteLine("UYARI: Bu araç yalnızca etik hacking ve savunma amaçlıdır. Yetkisiz kullanım yasaktır.\n");

                var tools = new List<ITool>
            {
                new NetworkAnalyzer(),
                new HttpAnalyzer(),
                new ReputationModule(),
                new IntegrityMonitor(),
                new HardwareMonitor(),
                new PacketSniffer(),
                new DNSAnalyzer(),
                new WhoisLookup(),
                new SubdomainEnumerator(),
                new SSLAnalyzer(),
                new AdvancedPortScanner(),
                new GeoIP(),
                new URLExtractor(),
                new DirBruteforce(),
                new MalwareHashCheck(),
                new SystemInfo(),
                new ADCheck(),
                new LogAnalyzer(),
                new EmailSecurityCheck(),
                new PasswordStrength(),
                new WirelessScanner(),
                new ProcessAnalyzer(),
                new StartupItems(),
                new RegistryAnalyzer(),
                new NetworkTrafficStats(),
                new CVEScanner(),
                new DataLeakCheck(),
                new WebVulnScanner(),
                new FileSignatureCheck(),
                new EncryptedDataDetector()
            };

                while (true)
                {
                    Console.WriteLine("\n--- ANA MENÜ ---");
                    for (int i = 0; i < tools.Count; i++)
                    {
                        Console.WriteLine($"{i + 1,2}. {tools[i].Name} - {tools[i].Description}");
                    }
                    Console.WriteLine("  0. Çıkış");
                    Console.Write("\nSeçiminiz (numara): ");
                    string input = Console.ReadLine();
                    if (input == "0") break;

                    if (int.TryParse(input, out int choice) && choice >= 1 && choice <= tools.Count)
                    {
                        Console.Write("Hedef IP veya domain girin (boş bırakırsanız sistem analizi yapılır): ");
                        string target = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(target)) target = "localhost";

                        var ctx = new ModuleContext { Target = target };
                        Console.Clear();
                        Logger.CyberpunkBanner();
                        Logger.Info($"Araç: {tools[choice - 1].Name}");
                        tools[choice - 1].Run(ctx);
                        Console.WriteLine("\nİşlem tamamlandı. Devam etmek için bir tuşa basın...");
                        Console.ReadKey();
                        Console.Clear();
                    }
                    else
                    {
                        Logger.Error("Geçersiz seçim.");
                    }
                }
            }
        }
    }
}
