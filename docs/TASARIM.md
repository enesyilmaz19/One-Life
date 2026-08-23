# TEK CAN — Tasarım Belgesi

**Kod adı:** Tek Can (geçici — nihai isim belirlenmedi)
**Motor:** Unity 6000.5.2f1 · WebGL + Windows Standalone modülleri kurulu
**Hedef:** Steam, önce erken erişim
**Tahmini süre:** 3-4 ay ilk EA sürümü
**Başlangıç:** 2026-08-23

---

## Yasa

> **Tek bir sayı var: saniye. Canın da o, süren de o, paran da o.**

| Eylem | Saat |
|---|---|
| Darbe yemek | − saniye |
| Düşman öldürmek | + saniye |
| Güçlenmek (silah, yetenek, kestirme) | − saniye |
| Saat sıfır | Koşu biter. **Hiçbir şey taşınmaz.** |

Altın yok. Ruh yok. Ayrı para birimi yok. Tek sayı oyunun tamamını çevirir.

**Steam cümlesi:** *Zamanın var. Canın da o, paran da o.*

---

## Kararlaştırılanlar

- [x] Çekirdek kaynak: tek sayı (saniye)
- [x] Klasik roguelike: **meta ilerleme yok**, ölünce her şey gider
- [x] Hikâye yükü yok — özgünlük mekanikte
- [x] Görsel kimlik **silah + VFX** üzerine kurulacak, karakter sade ve okunur
- [x] Silah ilkesi: *göründüğü* için değil **kararı değiştirdiği** için özgün
- [x] Erken erişim hedefi

- [x] **Bakış açısı: izometrik 3B** (2026-08-23). Kaos da izometrik olacak — kamera tempoyu belirlemez, ayrışma dövüş tasarımından gelir (Hades izometrik ve hızlı, Diablo izometrik ve ağır)
- [x] **Yapım sırası: temel oynanış önce, saat ekonomisi sonra** — ama can barı hiç yazılmayacak, yerine baştan taslak saat konacak (gerekçe aşağıda)

## Açık kararlar

- [ ] Nihai silah seti (tohumlar `../../FIKIR_DEFTERI.md` içinde, nihai değil)
- [ ] Koşu uzunluğu hedefi
- [ ] Oda/bölüm yapısı: elle tasarım mı, prosedürel mi, karma mı
- [ ] Düşman sayısı ve çeşidi
- [ ] Nihai isim

---

## Bakış açısı — izometrik 3B ✅

Saat mekaniği "yeterince hızlı öldürüyor muyum" gerilimi üzerine kurulu ve bu gerilim **her yönden baskı** ister — kuşatılmış hissetmezsen saat inerken panik olmaz. İzometrik 3B 360° tehdit veriyor, okunur, ve 3B modeller VFX'i parlatıyor (görsel kimliğimiz oraya kurulu).

Kaos da izometrik olacak. Kamera tempoyu belirlemez — Hades izometrik ve hızlı, Diablo izometrik ve ağır. İki oyunun ayrışması **dövüş tasarımından** gelecek, açıdan değil.

## Yapım sırası — temel oynanış önce ✅

Saat, dövüşün *üstüne binen* bir sistem. Dövüş yokken saati ayarlayamazsın: "öldürme kaç saniye getirsin" sorusunun cevabı bir dövüşün ne kadar sürdüğüne bağlı. Önce saat yazılırsa bütün sayılar tahminle konur, dövüş gelince hepsi baştan ayarlanır — iki kez iş.

Ayrıca risk sırası da bunu söylüyor: bu projenin en büyük batma riski **dövüş hissi**. Dövüş kötü hissettiriyorsa saat onu kurtaramaz. Önce en ölümcül riske vurulur.

**Ama bir tuzak var:** dövüşü normal can barıyla kurup sonra saati takarsan **yanlış dövüşü** kurmuş olursun. Saat ekonomisi saldırganlığı zorunlu kılıyor; can barıyla tasarlanan dövüş savunmayı ödüllendirir ve o alışkanlıklar sisteme gömülür.

**Çözüm:** can barı **hiç yazılmayacak.** Faz 1'de saat 20 satırlık bir taslak olarak konur (azalır, darbe düşürür, öldürme yükseltir — dengesiz, ayarsız). Dövüş baştan doğru baskı altında tasarlanır. Gerçek ekonomi işi (fiyatlar, satın alma, denge) Faz 3'e kalır.

---

## Riskler

| Risk | Not |
|---|---|
| **İçerik çeşitliliği** | Meta ilerleme yok → tekrar oynanabilirliğin tamamı koşunun *içindeki* çeşitlilikten gelmeli. Bu türün asıl batma sebebi. |
| **Dövüş hissi** | Sistemler özgünlüğü taşımıyorsa yükü vuruş hissi taşır. Enes'in 2. Steam kriteri zaten bu. |
| **Denge** | Saniye ekonomisi tek sayı olduğu için hassas; kazanç/kayıp oranları erken kalibre edilmeli. Deterministik simülasyonla test edilebilir. |
