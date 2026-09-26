# E-Helper v1.0.0 ⚡

> Casper Excalibur dizüstü bilgisayarlar için hafif, açık kaynaklı ve yüksek performanslı sistem yönetim aracı.

---

## 📖 Genel Bakış

**E-Helper**, resmi Excalibur Control Center yazılımının yüksek kaynak tüketen, hantal ve kararsız yapısına alternatif olarak geliştirilmiş bağımsız bir kontrol merkezidir. ASUS ekosistemindeki **G-Helper** felsefesiyle inşa edilmiştir:

- 🪶 **Hafif ve Hızlı:** Sıfır arka plan servisi, minimum RAM (~25-35 MB) ve sıfıra yakın CPU tüketimi.
- 📦 **Taşınabilir (Portable):** Kurulum gerektirmeyen tek çalıştırılabilir dosya mimarisi.
- 🛡️ **Doğrudan Donanım Erişimi:** Resmi hantal DLL kütüphanelerine bağımlı olmadan, doğrudan yerel Windows ACPI WMI SMI (`RW_GMWMI`) köprüsü üzerinden donanım denetimi.

---

## ✨ Temel Özellikler

### 1. 🎛️ Sistem Tepsisi ve Kompakt Panel (G-Helper Ergonomisi)
- Windows görev çubuğunda pencere kaplamaz; doğrudan **Gizli Simgeler (System Tray)** alanına yerleşir.
- Simgeye tıklandığında ekranın sağ alt köşesinde (tarih/saat üstünde) kompakt, modern karanlık temalı panel açılır.
- Panel dışına tıklandığında kendiliğinden otomatik olarak gizlenir (*Auto-hide*).

### 2. ⚡ Performans Profilleri (Tek Tıkla Geçiş)
- 🍃 **Ofis / Sessiz (Office):** Düşük fan devri ve güç tasarrufu modu.
- ⚖️ **Dengeli / Oyun (Gaming):** Optimize edilmiş dinamik fan ve güç dengesi.
- 🚀 **Yüksek Performans (High Performance):** Maksimum soğutma ve tam güç limiti.
- *Windows güç planları (`PowerSetActiveScheme`) ile anlık senkronizasyon.*

### 3. 📊 Canlı Donanım Telemetrisi (3 Saniyede Bir)
- Anlık CPU ve GPU sıcaklıkları (°C) ile dinamik renk uyarıları (Yeşil / Turuncu / Kırmızı).
- Anlık CPU ve GPU fan devirleri (RPM).
- Destekleyen modellerde 3. sistem fanı telemetrisi.

### 4. 🌈 Klavye RGB Aydınlatma Yönetimi
- **Işık Modları:** Sabit (Static), Nefes Alma (Breathing), Renk Döngüsü (Cycle), Kapalı (Off).
- **Parlaklık Ayarı:** 0 - 4 kademe seviye denetimi.
- **Hızlı Renkler & HEX Desteği:** Popüler neon preset renkler ve özel HEX renk kodu tanımlama.

### 5. 💾 Anlık ve Otomatik Kaydetme
- "Kaydet" veya "Uygula" butonu yoktur. Yapılan tüm değişiklikler anında donanıma iletilir ve yerel yapılandırmaya yazılır.

---

## 🛠️ Mimari & Teknoloji Yığını

```
+-------------------------------------------------------------------+
|                     E-Helper WPF UI / Tray                        |
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
|       (BufferBytes: 32-byte SMI_STRUCT_S Binary Data)             |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|        ACPI BIOS / EC (Embedded Controller) Donanımı              |
+-------------------------------------------------------------------+
```

- **Dil & Platform:** C# / .NET 8.0 (WindowsDesktop / WPF + WinForms Tray)
- **Donanım Katmanı:** `System.Management` WMI ACPI SMI (`0xFA00` / `0xFB00` protokolü)
- **Model Tespiti:** Excalibur G920, G870, G770 otomatik donanım algılama

---

## 🚀 Derleme ve Çalıştırma

### Gereksinimler
- Windows 10 / 11 (64-bit)
- .NET 8.0 SDK veya üstü
- Yönetici İzinleri (WMI ACPI donanım erişimi için gereklidir)

### Geliştirme Ortamında Derleme
```powershell
# Depoyu klonlayın
git clone https://github.com/berkevnl/e-helper.git
cd e-helper

# Projeyi derleyin
dotnet build src/EHelper/EHelper.csproj -c Release
```

### Tek Dosya (Portable Single-File) Yayımlama
```powershell
dotnet publish src/EHelper/EHelper.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish/
```
