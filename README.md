# E-Helper v1.0.0

Casper Excalibur dizüstü bilgisayarlar için geliştirilmiş açık kaynaklı, hafif ve taşınabilir sistem yönetim aracı.

---

## Genel Bakış

E-Helper, resmi Excalibur Control Center yazılımının kararsız, yüksek kaynak tüketen ve hantal yapısına alternatif olarak geliştirilmiştir. ASUS ekosistemindeki G-Helper yaklaşımını temel alır:

- **Hafif ve Optimize:** Arka plan servisi barındırmaz, ortalama 25-35 MB RAM ve sıfıra yakın CPU tüketir.
- **Taşınabilir (Portable):** Kurulum gerektirmeyen tek bir `EHelper.exe` dosyasından çalışır.
- **Doğrudan Donanım Köprüsü:** Harici üçüncü parti DLL kütüphanelerine bağımlı olmadan, doğrudan Windows yerel WMI ACPI SMI (`RW_GMWMI`) kanalı üzerinden gömülü denetleyiciyle (EC/BIOS) haberleşir.

---

## Temel Özellikler

### Sistem Tepsisi ve Kompakt Panel
- Görev çubuğunda pencere kaplamaz; doğrudan Windows Gizli Simgeler (System Tray) alanına yerleşir.
- Tepsi simgesine tıklandığında ekranın sağ alt köşesinde kompakt bir yönetim paneli açılır.
- Panel dışına tıklandığında otomatik olarak gizlenir (*Auto-hide*).

### Performans Profilleri
- **Ofis / Sessiz (Office):** Düşük fan devri ve güç tasarrufu modu.
- **Dengeli / Oyun (Gaming):** Dengeli güç ve akıllı soğutma modu.
- **Yüksek Performans (High Performance):** Tam güç limiti ve maksimum soğutma.
- Windows güç planlarıyla (`PowerSetActiveScheme`) otomatik senkronize çalışır.

### Donanım Telemetrisi (3 Saniyede Bir)
- Anlık CPU ve GPU sıcaklıkları (°C).
- Anlık CPU ve GPU fan devirleri (RPM).
- Destekleyen modellerde 3. sistem fanı telemetrisi.

### Klavye RGB Aydınlatma Denetimi
- **Modlar:** Sabit (Static), Nefes Alma (Breathing), Renk Döngüsü (Cycle), Kapalı (Off).
- **Parlaklık:** 0 ile 4 kademe arası parlaklık ayarı.
- **Renk Seçimi:** Hazır renk paleti ve özel HEX renk kodu tanımlama desteği.

### Reaktif Ayar Yönetimi
- "Kaydet" veya "Uygula" butonu yoktur. Yapılan tüm değişiklikler anında donanıma iletilir ve `%LocalAppData%\EHelper\config.json` dosyasına kaydedilir.

---

## İndirme ve Kullanım

### 1. İndirme
GitHub sayfasında yer alan **Releases** bölümünden en güncel `EHelper.exe` dosyasını indirin. Kurulum veya yükleme sihirbazı gerekmez.

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
|               HardwareBridge / HardwareMonitorService             |
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

# Tek dosya (Single-File) olarak yayımlayın
dotnet publish src/EHelper/EHelper.csproj -c Release -o publish/
```

Derleme tamamlandığında `publish/EHelper.exe` dosyası tek başına taşınabilir olarak kullanıma hazırdır.

---

## Lisans

Bu proje GNU General Public License v3.0 (GPL-3.0) ile lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakabilirsiniz.
