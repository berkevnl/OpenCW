# E-Helper v1.0.0

Casper Excalibur dizüstü bilgisayarlar (G770'ten G920'ye kadar tüm modeller) için geliştirilmiş açık kaynaklı, ultra hafif ve taşınabilir sistem yönetim aracı.

---

## Genel Bakış

E-Helper, resmi Excalibur Control Center yazılımının kararsız, yüksek kaynak tüketen ve hantal yapısına alternatif olarak geliştirilmiştir. ASUS ekosistemindeki G-Helper yaklaşımını temel alır:

- **Ultra Düşük Bellek Tüketimi:** Arka planda çalışırken yalnızca **~2-5 MB RAM** ve sıfıra yakın CPU tüketir (G-Helper seviyesi bellek yönetimi).
- **Taşınabilir (Portable):** Kurulum gerektirmeyen tek bir `EHelper.exe` dosyasından çalışır.
- **Doğrudan Donanım Köprüsü:** Harici üçüncü parti DLL kütüphanelerine bağımlı olmadan, doğrudan Windows yerel WMI ACPI SMI (`RW_GMWMI`) kanalı üzerinden gömülü denetleyiciyle (EC/BIOS) haberleşir.
- **Geniş Model Desteği:** Excalibur G770, G780, G850, G870, G900, G911 ve G920 modellerini otomatik olarak tanır.

---

## Temel Özellikler

### Sistem Tepsisi ve Kompakt Panel
- Görev çubuğunda pencere kaplamaz; doğrudan Windows Gizli Simgeler (System Tray) alanına yerleşir.
- Tepsi simgesine tıklandığında ekranın sağ alt köşesinde kompakt bir yönetim paneli açılır.
- Panel dışına tıklandığında otomatik olarak gizlenir (*Auto-hide*).

### Canlı Telemetri (CPU, GPU, RAM, SSD)
- **CPU & GPU:** Anlık sıcaklıklar (°C) ve fan devirleri (RPM).
- **RAM Kullanımı:** Kullanılan ve toplam bellek miktarı (GB) ile doluluk yüzdesi.
- **SSD (C:) Kullanımı:** Sistem diski doluluk oranı (GB) ve yüzdesi.
- *Panel açıkken 3 saniyede bir güncellenir; panel kapalıyken arka planda kaynak tasarrufu moduna geçer.*

### Performans Profilleri
- **Ofis / Sessiz (Office):** Düşük fan devri ve güç tasarrufu modu.
- **Dengeli / Oyun (Gaming):** Dengeli güç ve akıllı soğutma modu.
- **Yüksek Performans (High Performance):** Tam güç limiti ve maksimum soğutma.
- Windows güç planlarıyla (`PowerSetActiveScheme`) otomatik senkronize çalışır.

### Klavye RGB Aydınlatma Denetimi
- **Modlar:** Sabit (Static), Nefes Alma (Breathing), Dinamik Işık (Gökkuşağı Döngüsü), Kapalı (Off).
- **İnteraktif Renk Paleti:** Canlı renk yelpazesi üzerinden tıklama veya kaydırma ile anında renk seçimi.
- **Hazır Renkler:** Paletin hemen yanında yer alan hızlı ön tanımlı renk butonları.
- **Akıllı Arayüz:** Renk ayarları sadece Sabit ve Nefes modlarında gösterilir; Dinamik ve Kapalı modlarda arayüz sade kalır.

### Açık ve Koyu Tema Desteği
- Alt kısımdaki tema butonu ile **Koyu Tema** ve **Açık Tema** arasında anında geçiş yapılabilir.

### Reaktif Ayar Yönetimi
- "Kaydet" veya "Uygula" butonu yoktur. Yapılan tüm değişiklikler anında donanıma iletilir ve `%LocalAppData%\EHelper\config.json` dosyasına kaydedilir.

---

## İndirme ve Kullanım

### 1. İndirme
GitHub sayfasında yer alan **Releases** bölümünden `EHelper.exe` dosyasını indirin. Kurulum veya yükleme sihirbazı gerekmez.

### 2. Çalıştırma
* Donanım seviyesinde fan, sıcaklık ve aydınlatma kontrolü Windows WMI ACPI arayüzünü kullandığı için `EHelper.exe` dosyasına **Sağ Tık -> Yönetici Olarak Çalıştır** seçeneğiyle izin verilmesi gereklidir (uygulama bildiriminde otomatik yönetici yetkisi istenir).
* Program açıldığında doğrudan Windows bildirim alanındaki **Gizli Simgeler** içerisine yerleşir.

### 3. Kullanım
* Sağ altta yer alan `E` simgesine sol tıklayarak kontrol panelini açabilirsiniz.
* Ayarlarınızı değiştirdikten sonra panel dışındaki herhangi bir yere tıkladığınızda arayüz kendiliğinden gizlenecektir.
* Bilgisayar her açıldığında otomatik başlamasını isterseniz arayüzdeki **"Windows ile Birlikte Başlat"** kutucuğunu işaretleyebilirsiniz.

---

## Mimari Yapı

```
+-------------------------------------------------------------------+
|                     E-Helper WPF / Tray Arayüzü                   |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|      HardwareBridge / HardwareMonitorService / ResourceMonitor    |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|       WMI Servis Katmanı: "root\wmi" -> "RW_GMWMI"                 |
|       (32-bayt SMI_STRUCT_S Binary Protokolü)                     |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|          ACPI BIOS / EC (Embedded Controller) Donanımı            |
+-------------------------------------------------------------------+
```

---

## Kaynak Koddan Derleme

Tek dosya çalıştırılabilir `EHelper.exe` oluşturmak için:

```powershell
# Depoyu klonlayın
git clone https://github.com/berkevnl/e-helper.git
cd e-helper

# Bağımsız tek dosya (Self-Contained Single-File) olarak yayımlayın
dotnet publish src/EHelper/EHelper.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish-standalone/
```

---

## Lisans

Bu proje GNU General Public License v3.0 (GPL-3.0) ile lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakabilirsiniz.
