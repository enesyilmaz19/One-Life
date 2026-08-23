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

## Açık kararlar

- [ ] **Bakış açısı** — 2.5D yandan mı, izometrik/tepeden mi? (aşağıdaki nota bak, bugün ucuz, bir ay sonra pahalı)
- [ ] Nihai silah seti (tohumlar `../../FIKIR_DEFTERI.md` içinde, nihai değil)
- [ ] Koşu uzunluğu hedefi
- [ ] Oda/bölüm yapısı: elle tasarım mı, prosedürel mi, karma mı
- [ ] Düşman sayısı ve çeşidi
- [ ] Nihai isim

---

## Bakış açısı — karar verilmedi, varsayım olarak sürüklendi

Motor sorusu "2.5D hack and slash" üzerinden konuşuldu ve o yön varsayıldı, ama **Tek Can'ın kendi tasarımına karşı hiç sınanmadı.**

**Mekaniğin istediği:** saat mekaniği "yeterince hızlı öldürüyor muyum" gerilimi üzerine kurulu. Bu gerilim **her yönden baskı** ister — kuşatılmış hissetmezsen saat inerken panik olmaz.

| Bakış | Tek Can'a uyumu |
|---|---|
| **2.5D yandan** (Ori, Hollow Knight, Nine Sols) | Düşman tek eksenden gelir → kuşatma hissi zayıf. Ağır ve düşünen dövüşe uygun, sürü baskısına değil. |
| **Tepeden / twin-stick** (Nuclear Throne) | 360° tehdit → saat mekaniğine en uygun. |
| **İzometrik 3B** (Hades) | 360° tehdit + okunur + 3B modeller VFX'i parlatır. **Mekanik olarak en uygunu.** |

**Ek gerekçe — iki oyunu ayırmak:** B mekaniğini iki oyunda kullanmama sebebiyle aynı: Tek Can ile Kaos birbirinin kostümü gibi görünmemeli. Tek Can izometrik/tepeden, Kaos 2.5D yandan olursa hem her biri kendi mekaniğine hizmet eder hem görsel olarak ayrışırlar.

**Öneri:** Tek Can = izometrik/tepeden 3B · Kaos = 2.5D yandan. **Enes'in kararı bekleniyor.**

---

## Riskler

| Risk | Not |
|---|---|
| **İçerik çeşitliliği** | Meta ilerleme yok → tekrar oynanabilirliğin tamamı koşunun *içindeki* çeşitlilikten gelmeli. Bu türün asıl batma sebebi. |
| **Dövüş hissi** | Sistemler özgünlüğü taşımıyorsa yükü vuruş hissi taşır. Enes'in 2. Steam kriteri zaten bu. |
| **Denge** | Saniye ekonomisi tek sayı olduğu için hassas; kazanç/kayıp oranları erken kalibre edilmeli. Deterministik simülasyonla test edilebilir. |
