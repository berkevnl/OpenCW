<p align="center">
  <img src="docs/ocwcover.png" alt="OpenCW — Dizüstü Bilgisayarlar İçin Hafif Kontrol Merkezi" width="100%">
</p>

# OpenCW (Open Controlware) — Oyuncu Laptopları İçin Açık Kaynaklı Kontrol Merkezi

<p align="center">
  <a href="https://github.com/berkevnl/OpenCW/releases"><img src="https://img.shields.io/badge/Sürüm-v1.2.3-0078D4?style=flat-square" alt="Sürüm v1.2.3"></a>
  <a href="https://github.com/berkevnl/OpenCW/releases/latest/download/OpenCW.exe"><img src="https://img.shields.io/badge/İndir-OpenCW.exe-0078D4?style=flat-square&logo=windows&logoColor=white" alt="Doğrudan İndir"></a>
  <a href="https://github.com/berkevnl/OpenCW/blob/main/LICENSE"><img src="https://img.shields.io/badge/Lisans-GPL--3.0-green.svg?style=flat-square" alt="Lisans"></a>
  <a href="#-neden-opencw-temel-avantajlar"><img src="https://img.shields.io/badge/RAM-5--10_MB-success?style=flat-square" alt="RAM Tüketimi"></a>
  <a href="README.md"><img src="https://img.shields.io/badge/Language-English-blue?style=flat-square" alt="English Documentation"></a>
</p>

---

**OpenCW (Open Controlware)**, oyuncu dizüstü bilgisayarları için üreticilerin resmi yüksek kaynak tüketen, kararsız ve şişkin kontrol yazılımlarına (Control Center) alternatif olarak geliştirilmiş açık kaynaklı, ultra hafif ve modüler bir donanım yönetim aracıdır.

ASUS ekosisteminde devrim yaratan **G-Helper** felsefesi temel alınarak tasarlanan OpenCW, çoklu marka mimarisine sahiptir: Kullanıcı tek bir taşınabilir `.exe` dosyasını çalıştırdığında uygulama bilgisayarın marka ve modelini otomatik tespit eder, ilgili donanım köprüsünü (ACPI/EC/SMI) bağlar ve markadan bağımsız standart, akıcı bir yönetim paneli sunar.

<p align="center">
  <a href="https://github.com/berkevnl/OpenCW/releases/latest/download/OpenCW.exe">
    <img src="https://img.shields.io/badge/GÜNCEL_SÜRÜMÜ_İNDİR-OpenCW.exe-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="OpenCW İndir" height="46">
  </a>
  <br>
  <sub><i>Doğrudan indirme butonu başlamazsa lütfen <a href="https://github.com/berkevnl/OpenCW/releases/latest">Releases (Sürümler)</a> sayfasından son sürümü edinin.</i></sub>
</p>

---

## ⚡ Neden OpenCW? (Temel Avantajlar)

1. **Çoklu Marka Mimarisi, Tek Executable:** Desteklenen tüm markalar için tek bir taşınabilir `.exe` yeterlidir. Gigabaytlarca indirme veya karmaşık kurulum gerekmez.
2. **Ultra Düşük Bellek Tüketimi:** Arka planda sistem tepsisinde beklerken yalnızca **~5 - 10 MB RAM** harcar. Yönetim paneli ekranda aktif açıkken dahi tüketim **~20 - 35 MB** civarındadır.
3. **Sıfır Arka Plan Yükü (Bloat-Free):** Arka planda çalışan servisler (daemons), telemetri takipçileri, analitik ajanları ve CPU'yu meşgul eden gereksiz süreçler yoktur.
4. **Kalıcı Ayar Korunumu:** Yapılan ayarlar (klavye RGB aydınlatması, fan/güç modu, parlaklık) bilgisayar kapatılıp açıldığında dahi kaybolmaz; Windows Görev Zamanlayıcısı başlangıç entegrasyonu ve açılış sonrası EC firmware senkronizasyonu ile otomatik korunur.
5. **Doğrudan Donanım Köprüsü:** Üreticilerin hantal yazılımları yerine doğrudan anakart gömülü denetleyicisiyle (EC / WMI ACPI SMI) güvenli ve yerel iletişim kurar.
6. **Kompakt Sistem Tepsisi Paneli:** Görev çubuğunda yer kaplamaz, bildirim alanındaki simgesine tıklandığında ekranın köşesinde fırlayan modern bir arayüz açılır ve istendiğinde ekranda serbestçe taşınabilir.
7. **Çift Dil & Çift Tema:** Tek tıkla Türkçe ve İngilizce, ayrıca G-Helper esintili Mat Koyu ve Ferah Açık tema desteği.

---

## 🏗️ Çoklu Marka (Multi-Vendor) Mimarisi

OpenCW, temiz yazılım mimarisi ilkelerine göre katmanlara ayrılmıştır:

```
                  ┌─────────────────────────────────────────┐
                  │          OpenCW Ortak Arayüz            │
                  │   (Flyout / Tray / Tekil Tasarım)       │
                  └────────────────────┬────────────────────┘
                                       │
                      ┌────────────────┴────────────────┐
                      │    HardwareDetector & Factory   │
                      │   (WMI / SMBIOS Cihaz Keşfi)    │
                      └────────────────┬────────────────┘
                                       │
            ┌──────────────────────────┼──────────────────────────┐
            ▼                          ▼                          ▼
 ┌──────────────────────┐   ┌──────────────────────┐   ┌──────────────────────┐
 │   Casper Excalibur   │   │  Gelecek Markalar... │   │   Simülasyon Modu    │
 │  (ACPI WMI SMI EC)   │   │    (Lenovo / HP)     │   │  (Geliştirme & Test) │
 └──────────────────────┘   └──────────────────────┘   └──────────────────────┘
```

* **Dinamik Nesne Üretimi (Lazy Loading):** Açılışta yalnızca algılanan üreticiye ait köprü sınıfı belleğe (`RAM`) alınır. Diğer markaların kodları belleğe yüklenmez; bu sayede RAM kullanımı ve açılış süresi daima minimumda kalır.
* **Ortak Kullanıcı Deneyimi:** Bilgisayarınız bir Excalibur da olsa, gelecekte eklenecek bir model de olsa arayüz standart, pürüzsüz ve tutarlıdır.

---

## 🎯 Özellikler

### 1. Performans Modları
* **Tasarruf (Eco / Office):** Düşük fan devri, sessiz çalışma ortamı ve güç tasarrufu.
* **Dengeli (Balanced / Gaming):** Dinamik fan eğrisi ve dengeli termal yönetim.
* **Performans (Performance / Turbo):** Maksimum fan devri ve en yüksek donanım gücü.
* *Windows yerel güç planlarıyla (`PowerSetActiveScheme`) otomatik senkronize çalışır.*

### 2. Canlı Donanım Telemetrisi
* **CPU & GPU Değerleri:** Anlık işlemci ve ekran kartı sıcaklıkları (°C) ile fan RPM değerleri.
* **Sistem Kaynakları:** Canlı RAM kullanımı (GB ve yüzde) ve sistem diski SSD (C:) doluluk oranı.
* **Sistem Tepsisi İpucu (Tooltip):** Fare tepsi simgesinin üzerine getirildiğinde pencereyi açmadan anlık sıcaklık ve RPM bilgileri görüntülenir.

### 3. RGB Klavye Aydınlatma Denetimi
* **Modlar:** Sabit (Static), Nefes Alma (Breathing), Renk Döngüsü (Dinamik) ve Kapalı (Off).
* **Canlı Renk Spektrumu:** 360 derecelik renk yelpazesi üzerinden akıcı seçim ve popüler hazır renk butonları.
* **Donanımsal Parlaklık Kademeleri:**
  * `0` : **Kapalı (%0)** — Tüm klavye ışıklarını donanımsal olarak kapatır.
  * `1` : **%50 Parlaklık** — Dengeli ortam parlaklığı.
  * `2` : **%100 Parlaklık** — Tam parlaklık seviyesi.
* **Yeniden Başlatmada Korunum:** Bilgisayar yeniden başlatıldığında klavye aydınlatma modunuz, parlaklığınız ve renginiz otomatik olarak tekrar uygulanır.

### 4. Kullanılabilirlik & Sistem Entegrasyonu
* **Başlangıçta Çalıştır:** Windows açılışında UAC uyarısı vermeden arka planda sessizce başlatılan Görev Zamanlayıcısı entegrasyonu.
* **Tek Tıkla Tema Değişimi:** Mat Koyu ve Ferah Açık tema seçenekleri.
* **Entegre Güncelleyici:** GitHub üzerinden yeni sürümleri tek tıkla sorgulama.

---

## 💻 Donanım Desteği

OpenCW açılışta cihazınızı dinamik olarak tespit eder:

### ✅ Casper Excalibur Ailesi (Doğrudan ACPI SMI Motoru)
> Fiziksel donanım üzerinde test edilmiş ve sorunsuz çalıştığı doğrulanmıştır: **Excalibur G770**, **G850** ve **G911** (performans profilleri, telemetri ve klavye RGB aydınlatması).

* **Excalibur G770** (Test Edildi & Doğrulandı)
* **Excalibur G850 / G860** (Test Edildi & Doğrulandı)
* **Excalibur G911** (Test Edildi & Doğrulandı)
* **Excalibur G780**
* **Excalibur G870**
* **Excalibur G900**
* **Excalibur G920**

### ⏳ Eklenecek Donanım Köprüleri (Planlanan)
* **Lenovo Legion Serisi**
* **HP Omen & Victus Serisi**

*(Topluluk katkılarına açıktır! Diğer dizüstü bilgisayarlar için ACPI/WMI köprüsü geliştirmek veya tersine mühendislik verisi sağlamak isterseniz [Hardware Dizinine](src/OpenCW/Hardware/) göz atabilirsiniz.)*

### 🧪 Simülasyon Modu
* Desteklenmeyen cihazlarda, harici masaüstü sistemlerde veya sanal makinelerde uygulama otomatik olarak simülasyon moduna geçer; arayüz ve kontroller güvenle incelenebilir.

---

## 🚀 İndirme ve Kullanım

1. [Releases (Sürümler)](https://github.com/berkevnl/OpenCW/releases/latest) sayfasına gidin.
2. `OpenCW.exe` dosyasını indirin (kurulum gerekmez).
3. `OpenCW.exe` dosyasına sağ tıklayıp **Yönetici Olarak Çalıştır** deyin *(Windows'un ACPI WMI SMI donanım kontrolcüsüne erişebilmesi için yönetici yetkisi gereklidir)*.
4. OpenCW ekranın sağ altındaki **Sistem Tepsisine** (saatin yanına) yerleşecektir. Kırmızı OpenCW simgesine tıklayarak paneli açıp kapatabilirsiniz!

---

## 💖 Teşekkür & İlham Kaynağı

Bu proje, **[Seerge](https://github.com/seerge)** tarafından geliştirilen öncü **[G-Helper](https://github.com/seerge/g-helper)** projesinden ilham almıştır.

G-Helper, hantal ve kapalı kutu OEM üretici yazılımlarının açık kaynaklı, ultra hafif, şeffaf ve minimalist araçlarla ikame edilebileceğini kanıtlamıştır. **OpenCW**, bu felsefeyi çoklu marka destekli bir yapıyla dizüstü bilgisayar ekosistemine kazandırmayı amaçlamaktadır.

---

## ⚖️ Yasal Haklar Bildirimi

* Bu projede adı geçen marka ve model isimleri yalnızca donanım uyumluluğunu belirtmek amacıyla kullanılmıştır ve ilgili hak sahiplerinin mülkiyetindedir.
* **OpenCW**, bağımsız ve açık kaynaklı bir topluluk projesidir.
* Bu yazılım **GNU General Public License v3.0 (GPL-3.0)** lisansı altında, hiçbir garanti verilmeksizin "olduğu gibi" sunulmaktadır.
