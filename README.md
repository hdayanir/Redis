
# Dağıtık Önbellek ve Serileştirme Performans Analizi

Bu proje, yüksek trafikli .NET 8 web uygulamalarında **Redis** dağıtık önbellek katmanı üzerinde kullanılan farklı serileştirme (serialization) yöntemlerinin performansını ölçmek ve karşılaştırmak amacıyla geliştirilmiştir.

## 📊 Proje Özeti
Proje kapsamında 100.000 satırlık sentetik bankacılık verisi kullanılarak bir "Stres Testi" gerçekleştirilmiştir. Testler sırasında aşağıdaki formatlar karşılaştırılmıştır:
- **JSON** (Metin tabanlı - Referans)
- **MessagePack** (İkili)
- **gRPC / Protocol Buffers (Protobuf)** (Şemaya dayalı ikili)
- **Apache Avro** (Şemaya dayalı ikili)

## 📈 Test Sonuçları (100.000 Kayıt)

### 1. Bellek (RAM) Tasarrufu
| Format | Bellek Kullanımı | Tasarrufu Oranı |
| :--- | :--- | :--- |
| **JSON** | 29.36 MB | - |
| **Protobuf** | **10.49 MB** | **%64,3** |
| **Avro** | 12.58 MB | %57,1 |

### 2. Yanıt Süreleri (100 Sanal Kullanıcı)
- **SQL (Doğrudan Okuma):** ~32.777 ms
- **Redis JSON:** ~9.678 ms
- **Redis Protobuf:** **~7.468 ms** 🏆

## 🛠️ Kullanılan Teknolojiler
- **Framework:** .NET 8 Web API
- **Önbellek:** Redis (Memurai)
- **Test Araçları:** Apache JMeter 5.6.3
- **Monitoring:** Memurai-CLI

## 📁 Proje Yapısı
- `Controllers/`: Performans testi senaryolarının (SQL, JSON, MsgPack, Protobuf, Avro) bulunduğu API servisleri.
- `Models/`: Serileştirme şemaları ve veri modelleri.
- `Data/`: Veritabanı ve Seed (veri yükleme) işlemleri.

---
*Bu çalışma, Ahmet Yesevi Üniversitesi Yazılım Mühendisliği Yüksek Lisans Dönem Projesi kapsamında geliştirilmiştir.*