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

- [ ] ⚠️ **HİKÂYE — kapsam değişikliği.** 2026-08-24'te Enes "hikâye olacağı için haritayı ileride konuşuruz" dedi. Ama bu belge "hikâye yükü yok, özgünlük mekanikte" varsayımıyla ve **3-4 ay** tahminiyle kuruldu. Hikâye eklenirse: ① süre uzar ② Kaos'un ayırt edici özelliği hikâyeydi, iki oyun birbirine yaklaşır. **Faz 4 (harita/koşu yapısı) başlamadan netleşmeli** — Faz 1-3 dövüş odaklı olduğu için hikâyeden bağımsız, engel değil.
- [ ] Harita / bölge tasarımı — hikâye kararından sonra
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

## Dövüş — düşman canı ve zaman hasadı ✅ (2026-08-23, Enes)

**Çekirdek soru** (oyunun dakikada onlarca kez sorduğu şey): *Hangi düşmana zaman harcayacağım?*

**Düşman canı:** İçeride **düz can** (hit point havuzu — basit, ayarlanabilir, tuhaf etkileşim yok). Dışarıda **zaman olarak gösterilir**: düşmanın üstünde geri sayan bir sayı, "bu bedende kaç saniye kaldı" der. Sıfır → ölür.

**Düşman zamanı kendi kendine de akar** (yavaşça). Süresi dolan düşman **ölmez** — durum değiştirir:

| Durum | Tehlike | Canı | Ödemesi |
|---|---|---|---|
| **Dolu** (süresi var) | **Yüksek** — hızlı, agresif, acıtır | Zaman havuzu | Hasat eder |
| **Boş** (süresi bitti) | **Düşük** — yavaşlar, zayıflar | **Tek atımlık** | **Hiç** |

**Neden AFK sömürüsü değil:** beklemek her zaman baskın olarak kötü. Kendi saatin akar, karşılığında sıfır hasat alırsın. Düşmanları bedavaya temizlersin ama o temizlik sana hiçbir şey kazandırmaz — cebin boş, saatin yanmış.

**Neden bu "hep tehlikeli kalsın"dan iyi:** boşalıp hâlâ ölümcül kalan düşman saf ceza olurdu — hata yapan oyuncuyu kazanılamaz bir duruma sokar. Bu haliyle ceza **ekonomik**, ölümcül değil: odayı batırırsan koşuyu kaybetmezsin, **fakir çıkarsın.** Roguelite'ta doğru ceza budur.

**Kasıtlı kaçış vanası:** kazanamayacağı bir odada oyuncu kaçınıp düşmanların boşalmasını bekleyebilir, sonra tek vuruşla temizler. Bu sömürü değil, **bilinçli bir seçenek** — hayatta kalırsın ama zararına. Beceri kararı: şimdi dövüşüp kâr et, ya da hayatta kal ve zararı ye.

**Okunabilirlik:** boş düşman tek vuruşta ölüyor → oyuncu durum değişimini anında anlıyor, açıklamaya gerek kalmıyor.

**Zaman hasadı:** Öldürünce toplu ödeme **yok**. Hasar verdikçe **sürekli** zaman çalınır (can çalma gibi, ama zaman). Her vuruş anında ödüyor.

**Neden toplu ödemeden iyi:**
- Her vuruş geri bildirim veriyor → dövüş hissi çok daha sıkı
- Toplu ödemede kalın düşmana girmek uzun bir kuraklık olurdu (saat sadece inerdi) → oyuncu tank düşmandan kaçardı. Sürekli hasatta girmek anında ödüyor.
- **"Hızlı öldürme daha çok verir" mekaniğini bedavaya içeriyor:** saat sabit hızda inerken hasat DPS'e bağlı olduğu için hızlı öldürmek zaten matematiksel olarak daha kârlı. Ayrı bir sistem yazmaya gerek yok.

**Açtığı silah ekseni:** hasar başına çalınan zaman **silaha göre değişebilir**. "Az hasar verir ama hasar başına çok zaman çalar" ile "çok vurur az çalar" iki farklı oyun tarzı — DPS eşit olsa bile. Silah tasarımının ana kadranlarından biri bu.

> ⚠️ **OYUNUN EN KRİTİK SAYISI: hasar başına çalınan zaman oranı.**
> Çok yüksekse dövüş kendi kendini besler, saat anlamsızlaşır, oyun biter. Çok düşükse her kavga net kayıptır, oyuncu kaçmaya başlar — ki tasarımın tam tersi.
> **Hedef denge: temiz kavga net pozitif, dağınık kavga net negatif.** Bütün oyun bu tek orana asılı. Faz 2'de deterministik simülasyonla taranacak.

### Denge taraması sonucu (2026-08-24, `Tek Can → Denge simülasyonu`)

Kritik sayı artık tahmin değil, ölçüm. Düşman başına **net saniye** (senkron sonrası
değerlerle: hasar 3.5, savurma 0.35 sn, drenaj 1/sn, darbe 4 sn):

| hasat | TEMİZ | ORTA | DAĞINIK | |
|---|---:|---:|---:|---|
| **%20** (şu anki) | **−0.7** | −5.1 | −12.6 | ❌ temiz oyun bile zarar |
| %30 | +0.2 | −4.1 | −11.7 | en düşük uygun |
| %40 | +1.2 | −3.2 | −10.8 | ✅ rahat aralık |
| %70 (tavan) | +4.1 | −0.3 | −8.1 | dağınık bile kâra yaklaşıyor |

> ⚠️ **AÇIK KARAR — şu anki %20 tasarım hedefini tutmuyor.**
> "Temiz kavga net pozitif" kuralı bozuluyor: tek hedefle ne kadar iyi oynarsan oyna
> her düşman **−0.7 sn** zarar. Yani kâr etmenin tek yolu kalabalığa vurmak kalıyor —
> ki bu tam olarak kaçınmak istediğimiz şey.
>
> Kaldıraçlar: hasat oranını %30-40'a çek · `enemyFullHitCost`'u (4 sn) düşür ·
> `enemyTimePool`'u büyüt (kavga uzar ama hasat da artar). Karar Enes'in.

**Ayrıca:** darbe maliyeti (4 sn) düşman başına kârı (en iyi ihtimalle ~1.2 sn) eziyor.
**Bir hata üç kusursuz öldürmeyi siliyor.** Roguelike için savunulabilir ama bilinçli olmalı.

> ⚠️ **ÇOKLU HEDEF SORUNU (2026-08-24 playtest, Enes yakaladı):** Savurma koni içindeki
> HERKESE tam hasar veriyor ve her birinden ayrı hasat ediliyor. Sonuç: kalabalık arttıkça
> oyun **kolaylaşıyor** — roguelite'ta olmaması gereken şey.
>
> Örnek (o günkü çalışan değerlerle): 3.5 hasar × %40 hasat = hedef başına 1.4 sn.
> Üç düşman koni içindeyse tek savurmada +4.2 sn, saat 1 sn/sn iniyor.
>
> **Asıl sonucu:** tek hedefli silahlar bu yapıyla asla rekabet edemez; koni her zaman
> hedef sayısı kadar üstün olur. Bu bir denge sayısı meselesi değil, **yapısal**:
> hasat oranı silaha göre değişmeli — "az vurur, hasar başına çok çalar" (tek hedef)
> ↔ "çok vurur, az çalar" (koni). Faz 3'ün ilk maddesi.

**Özel düşmanlar:** bazılarının özel zaman etkileri olacak (saati hızlandıran, vurunca zaman çalan, öldürücü darbede toplu ödeyen, kaçıp zamanını götüren...). Meta ilerleme olmadığı için **koşu içi çeşitliliğin ana kaynağı burası.**

**Bosslar:** ayrı kural. Daha özel, zamanla ilişkili mekanikler (faz geçişi oyuncunun saatine bağlı, büyük saldırı zaman çalar, süre kapısı vb.). Faz 4+ konusu.

### Savunma

| Karar | Sebep |
|---|---|
| **Blok yok** | Blok = köşede bekleme = tasarım ölür |
| **Savunma = dash + i-frame** | Hareket temelli, oyuncuyu kavganın içinde tutar |
| **Saldırılar kısa, dash ile iptal edilebilir** | Uzun taahhüt saldırganlığı cezalandırır |
| **Dövüş dışında saat daha hızlı iner** | Kaçmak ve beklemek açıkça cezalandırılır |
| **Parry Faz 1'de YOK** | İleride eklenecek. Eklenirse **saniye geri vermeli**, yoksa "bekle ve savuştur" tasarımı bozar |

### Gösterge — sayı, parlaklık değil ✅

Parlaklık fikri reddedildi (Enes: saçma kaçar). Düşmanın üstünde **geri sayan sayı** olacak, saniye biriminde.

⚠️ **Ama iki tür sayıyı ayırmak şart:**
- **Kalıcı sayı** (düşmanda kalan süre) → var, tek, sakin tipografi, ölçülü
- **Uçuşan hasar sayıları** → **YOK.** Ekranı rakam çorbasına çeviren şey bu; "mobil/tarayıcı oyunu" görüntüsü buradan gelir

## Riskler

| Risk | Not |
|---|---|
| **İçerik çeşitliliği** | Meta ilerleme yok → tekrar oynanabilirliğin tamamı koşunun *içindeki* çeşitlilikten gelmeli. Bu türün asıl batma sebebi. |
| **Dövüş hissi** | Sistemler özgünlüğü taşımıyorsa yükü vuruş hissi taşır. Enes'in 2. Steam kriteri zaten bu. |
| **Denge** | Saniye ekonomisi tek sayı olduğu için hassas; kazanç/kayıp oranları erken kalibre edilmeli. Deterministik simülasyonla test edilebilir. |
