<p align="center">
  <img src="docs/screenshots/e-helper-preview.jpg" alt="E-Helper — Lightweight control tool for Casper Excalibur laptops" width="100%">
</p>

# E-Helper — Lightweight Control Tool for Casper Excalibur Laptops

<p align="center">
  <a href="https://github.com/berkevnl/e-helper/releases"><img src="https://img.shields.io/github/v/release/berkevnl/e-helper?color=0078D4&style=flat-square" alt="Latest Release"></a>
  <a href="https://github.com/berkevnl/e-helper/releases/latest/download/EHelper.exe"><img src="https://img.shields.io/badge/Download-E--Helper.exe-0078D4?style=flat-square&logo=windows&logoColor=white" alt="Direct Download"></a>
  <a href="https://github.com/berkevnl/e-helper/blob/main/LICENSE"><img src="https://img.shields.io/badge/License-GPL--3.0-green.svg?style=flat-square" alt="License"></a>
  <a href="#-memory--resource-footprint"><img src="https://img.shields.io/badge/RAM-5--10_MB-success?style=flat-square" alt="RAM Usage"></a>
  <a href="README.tr.md"><img src="https://img.shields.io/badge/Dil-T%C3%BCrk%C3%A7e-red?style=flat-square" alt="Türkçe Döküman"></a>
</p>

---

Small, ultra-lightweight, and bloat-free **Excalibur Control Center** alternative for Casper Excalibur gaming laptops offering essential hardware controls with a near-zero system footprint.

Compatible with **Excalibur G770, G780, G850, G870, G900, G911, G920**, and other Quanta ODM-based Casper Excalibur models.

<p align="center">
  <a href="https://github.com/berkevnl/e-helper/releases/latest/download/EHelper.exe">
    <img src="https://img.shields.io/badge/DOWNLOAD_LATEST_RELEASE-E--Helper.exe-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="Download E-Helper.exe" height="46">
  </a>
  <br>
  <sub><i>If the direct download button does not start automatically, please visit <a href="https://github.com/berkevnl/e-helper/releases/latest">Releases</a> to download the latest executable.</i></sub>
</p>

> [!TIP]
> 🇹🇷 **Türkçe kullanıcılar için:** Bu dökümanın tamamını Türkçe okumak için [README.tr.md](README.tr.md) sayfasına gidebilir veya sayfa sonundaki [Türkçe Tanıtım ve Kullanım Kılavuzu](#-türkçe-tanıtım-ve-kullanım-kılavuzu) bölümüne atlayabilirsiniz.

---

## ⚡ Why E-Helper? (Advantages)

1. **Ultra-Low Memory Footprint:** Consumes only **~5 - 10 MB RAM** in the background (system tray) and **~20 - 35 MB RAM** when the management panel is actively open.
2. **Zero Bloatware & Portable:** A single standalone `.exe` file. No installer, no background telemetry services, no scheduled tasks, and no system clutter.
3. **Native Direct Hardware Bridge:** Communicates directly with the Embedded Controller (EC / ACPI BIOS) through Windows Native WMI SMI (`RW_GMWMI`), completely bypassing heavy third-party vendor services.
4. **Instant Flyout UI:** Docks neatly in the Windows System Tray (notification area). Clicking the tray icon opens a sleek, sharp control panel in the corner of your screen.
5. **Bidirectional Hardware Sync:** Respects physical `Fn + Space` key shortcuts and hardware brightness changes in real-time.
6. **Built-in Auto Updater:** One-click GitHub Release version checker and updater.
7. **Clean Aesthetics:** Tailored dark and light themes inspired by modern minimalist design.

---

## 📊 Comparison: E-Helper vs. Excalibur Control Center

| Feature | Casper Excalibur Control Center | E-Helper |
| :--- | :---: | :---: |
| **Background RAM Usage** | 150 MB – 400 MB+ | **~5 – 10 MB** |
| **Active Window RAM** | 300 MB – 600 MB | **~20 – 35 MB** |
| **Background Services** | 3 to 5 persistent services | **0 (None)** |
| **Startup Time** | 15 – 30 seconds | **< 1 second** |
| **Installation** | ~500 MB setup wizard | **Portable single file** |
| **Telemetry / Tracking** | Yes | **None (100% Offline & Open Source)** |
| **System Tray Flyout** | Heavy window | **G-Helper style fast flyout** |

---

## 🎯 Features

### 1. Performance Modes
* **Silent (Office / Sessiz):** Quiet operation, reduced fan speeds, and energy conservation.
* **Balanced (Gaming / Dengeli):** Balanced thermal dissipation and dynamic fan control.
* **Turbo (High Performance / Yüksek Performans):** Maximum fan airflow and full hardware power headroom.
* *Automatically synchronized with native Windows Power Schemes (`PowerSetActiveScheme`).*

### 2. Real-Time Telemetry
* **CPU & GPU Metrics:** Instant temperature readings (°C) and tachometer fan speeds (RPM).
* **System Resources:** Live RAM usage (used / total GB and percentage) and primary SSD (C:) storage utilization.
* **Hover Tooltip:** Hovering over the system tray icon displays instant temperatures and fan RPMs without opening the window.

### 3. RGB Keyboard Backlight Control
* **Modes:** Static (Sabit), Breathing (Nefes), Rainbow Dynamic Cycle (Dinamik), and Off (Kapalı).
* **Interactive Color Spectrum:** Smooth gradient spectrum slider and curated quick-select color presets.
* **3-Stage Hardware Brightness:** 
  * `0` : **Off (0%)** — Turns off all keyboard LED zones.
  * `1` : **50% Brightness** — Balanced ambient brightness.
  * `2` : **100% Brightness** — Full maximum illumination.

### 4. Usability & Customization
* **Start with Windows:** Optional registry-based auto-start toggle with zero background overhead.
* **One-Click Theme Switch:** Seamless toggle between G-Helper Dark Mode and Clean Light Mode.
* **Bilingual UI:** Instant one-click language toggle between **Türkçe (TR)** and **English (EN)**.

---

## 💻 Supported Models

Tested and architected for Casper Excalibur laptops featuring Quanta Computer ODM motherboards and WMI ACPI SMI interfaces:

* **Excalibur G770** (All generations: Intel 9th, 10th, 11th, 12th Gen)
* **Excalibur G780**
* **Excalibur G850 / G860**
* **Excalibur G870** (Intel 12th & 13th Gen)
* **Excalibur G900**
* **Excalibur G911**
* **Excalibur G920**
* *Other Quanta ODM laptops supporting `root\wmi:RW_GMWMI`.*

---

## 🚀 Download & Installation

1. Go to the [Releases](https://github.com/berkevnl/e-helper/releases/latest) page.
2. Download `EHelper.exe` (standalone single-file, no installation required).
3. Right-click `EHelper.exe` and select **Run as Administrator** *(Administrator privileges are required by Windows to access ACPI WMI SMI hardware ports)*.
4. E-Helper will minimize to your **System Tray** (near the clock). Click the **E** icon to toggle the control flyout!

---

<br>

---

## 🇹🇷 Türkçe Tanıtım ve Kullanım Kılavuzu

Casper Excalibur serisi dizüstü bilgisayarlar için resmi **Excalibur Control Center** yazılımına alternatif olarak geliştirilmiş açık kaynaklı, ultra hafif ve taşınabilir sistem yönetim aracıdır.

### 🌟 Öne Çıkan Avantajlar
* **Ultra Düşük Bellek Kullanımı:** Sistem tepsisinde arka planda çalışırken yalnızca **~5 - 10 MB RAM** tüketir. Yönetim paneli açıldığında dahi tüketim yalnızca **~20 - 35 MB** aralığında kalır.
* **Kurulumsuz & Taşınabilir (Portable):** Tek bir `.exe` dosyasından çalışır. Sisteme sürücü, arka plan servisi veya kalıntı bırakmaz.
* **Doğrudan Donanım Köprüsü:** Harici hantal servislere bağımlı olmadan, Windows'un yerel WMI ACPI SMI (`RW_GMWMI`) kanalı üzerinden doğrudan gömülü denetleyiciyle (EC / BIOS) haberleşir.
* **Donanımsal 3 Kademeli Aydınlatma:** Excalibur donanımının orijinal çalışma standardına tam uyumlu Kapalı (%0), %50 ve %100 kademeleri.
* **Çift Yönlü Senkronizasyon:** Klavyedeki `Fn + Space` kısayoluyla yapılan parlaklık değişikliklerini anında arayüze yansıtır.
* **Tek Tıkla Otomatik Güncelleme:** GitHub Release üzerinden yeni sürümleri otomatik kontrol eder ve kullanıcı onayıyla günceller.

### 📋 Temel Özellikler
1. **Güç Profilleri:** Sessiz (Office), Dengeli (Gaming) ve Turbo (Yüksek Performans) modları. Windows güç planlarıyla otomatik senkronize çalışır.
2. **Canlı Telemetri:** CPU ve GPU anlık sıcaklıkları (°C) ile fan devirleri (RPM), RAM bellek ve SSD (C:) disk doluluk oranları.
3. **RGB Klavye Denetimi:** Sabit Renk, Nefes Alma, Gökkuşağı Dinamik Döngü ve Kapalı modları. Canlı renk yelpazesi ve hızlı renk paleti.
4. **Açık / Koyu Tema & Dil:** Tek tıkla Türkçe ve İngilizce dil değişimi, karanlık ve aydınlık mod desteği.

### 🛠️ Kurulum ve Çalıştırma
1. [Son Sürüm (Releases)](https://github.com/berkevnl/e-helper/releases/latest) sayfasından `EHelper.exe` dosyasını indirin.
2. İndirdiğiniz dosyaya sağ tıklayıp **Yönetici Olarak Çalıştır** seçeneğini kullanın *(Anakartın EC çipine WMI SMI üzerinden erişmek için Windows yönetici izni gereklidir)*.
3. Uygulama sistem tepsisine (saat yanına) yerleşecektir. Mavi **E** simgesine tıklayarak paneli açıp kapatabilirsiniz.

---

## 💖 Acknowledgments & Inspiration

This project draws immense inspiration from the pioneering work of **[Seerge](https://github.com/seerge)** and the **[G-Helper](https://github.com/seerge/g-helper)** project. 

G-Helper revolutionized laptop utility design by proving that proprietary, bloated OEM software suites can be replaced with lightning-fast, transparent, and respectful open-source tools. E-Helper aims to bring that exact same philosophy, visual elegance, and performance excellence to the Casper Excalibur community.

---

## ⚖️ Trademarks & Legal Disclaimer

* **Casper®**, **Excalibur®**, and the Casper Excalibur logos are registered trademarks of **Casper Bilgisayar Sistemleri A.Ş.**
* **E-Helper** is an independent, community-driven open-source project. It is **not** developed by, affiliated with, sponsored by, or endorsed by Casper Bilgisayar Sistemleri A.Ş.
* All other product names, logos, and brands are property of their respective owners.
* This software is provided under the **GNU General Public License v3.0 (GPL-3.0)** "as is", without warranty of any kind. Use at your own discretion.
