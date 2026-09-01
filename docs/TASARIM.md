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

- [x] ✅ **HİKÂYE VAR — karar verildi** (2026-08-24, Enes: *"oyunun genel yapısı tek can ve hikâye üzerinden kurulacak"*).
  Sonuçları: ① **3-4 ay tahmini geçersiz**, hikâye içeriği kadar uzar ② Kaos'un ayırt edici özelliği hikâyeydi;
  iki oyun birbirine yaklaşıyor, Kaos seçilirse teması sıfırdan kurulmalı ③ sıra: önce temel mekanik,
  sonra görsellik ve oynanış, sonra hikâye dokusu.
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

**Çözüm:** can barı **hiç yazılmayacak.** Faz 1'de saat 20 satırlık bir taslak olarak konur (azalır, darbe düşürür, öldürme yükseltir — dengesiz, ayarsız). Dövüş baştan doğru baskı altında tasarlanır. Gerçek ekonomi işi (fiyatlar, satın alma, denge) Faz 2'ye kaldı ve orada yapıldı.

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

### Denge taraması sonucu — **KARAR VERİLDİ** (2026-08-24, Enes)

`Tek Can → Denge simülasyonu`. Düşman başına **net saniye** (hasar 3.5, savurma 0.35 sn,
drenaj 1/sn, darbe 4 sn):

| hasat | TEMİZ | ORTA | DAĞINIK | |
|---|---:|---:|---:|---|
| %20 | −0.7 | −5.1 | −12.6 | temiz oyun bile zarardaydı |
| %30 | +0.2 | −4.1 | −11.7 | en düşük uygun |
| **%35** ✅ | **+0.7** | −3.7 | −11.3 | **seçilen** |
| %40 | +1.2 | −3.2 | −10.8 | |
| %70 (tavan) | +4.1 | −0.3 | −8.1 | dağınık bile kâra yaklaşır |

**Karar: hasat %35, darbe maliyeti 4 sn olarak KALIYOR** (Enes, 2026-08-24).

Tasarım hedefi tutuyor: temiz kavga net pozitif (+0.7), dağınık kavga ağır negatif (−11.3).
Marj bilerek dar — tek hedefli oyun kârlı ama zengin etmiyor, kâr beceriyle geliyor.

**Darbe maliyeti bilinçli olarak sert bırakıldı:** 4 sn, düşman başına kârın ~6 katı.
Bir hata altı temiz öldürmeyi siliyor. Roguelike'ta bu seçim savunulabilir; kazara değil.

## Koşu yapısı — Faz 4

### ⚠️ ENTER GEÇİCİ (Enes, 2026-08-24)

Dalgaları `ENTER` ile çağırmak **yalnızca mekanik testi için**. Gerçek oyunda dalgalar
oyuncu tarafından çağrılmayacak — hikâye/mekân akışına bağlanacak (oda geçişi, kapı,
tetikleyici, ne olacaksa). `WaveManager.StartNextWave()` zaten dışarıdan çağrılabilir;
tuş bağı kaldırılıp yerine o akış konacak.

### Boss

Yalnızca "kalın bir düşman" değil: daha hızlı (×1.25), daha az uyarı verir (hazırlık ×0.85),
daha geniş vurur (koni ×1.5), darbesi çok daha pahalı (×2.2). Menzili gövdesi kadar uzun —
yoksa dev bir yaratık burnunun dibinden ıskalıyor gibi görünüyordu.

### Boss: yere vuruş ✅ (2026-08-24)

Enes: *"boss baya kolay, dümdüz durup saldırarak kesiyorum."*

Simülasyon doğruladı: boss 7.5 saniyede ölüyordu ve oyuncunun 42.6/60 saati kalıyordu —
hiç kaçmasa bile. **Sorun sayıda değildi.** Boss'un oyuncuyu yerinden kaldıran bir saldırısı
yoktu; normal düşmanla aynı yapay zekâyı kullanıyordu, sadece daha büyüktü.

**Yere vuruş:** iki saldırıda bir, 360° halka. Yön yok — yana kaçmak işe yaramıyor,
halkanın dışına çıkman gerekiyor. Çıkmak da vurmayı kesmek demek, yani dövüş uzuyor.
Kararın bedeli o. Hazırlığı 3.5 kat uzun: sürpriz değil, karar olmalı.

**Boss'un hasatı düşük (×0.4).** Havuz hem canı hem ödemesi olduğu için, boss'u
tankileştirmek onu zorlaştırmaz — **ödül yapardı.** Düşük hasat: uzun bir dövüş ama
seni beslemiyor. Gerçek bir yarış oluyor.

**Ölçüm** (`Tek Can → Denge simülasyonu`, havuz ×20):

| profil | süre | kalan saat | sonuç |
|---|---:|---:|---|
| durup vuruyor | 9.9 sn | −12.8 | ❌ ölüyor |
| yere vuruştan kaçıyor | 19.1 sn | +3.6 | ✅ kazanıyor |

Kaçan oyuncu **3.6 saniyeyle** sıyırıyor. Simülasyon sunak yükseltmelerini saymıyor;
keskinlik alan rahat kazanır, hiç yatırım yapmayan zor kazanır. **Ekonomiyi ödüllendiren
ayar bu.**

> ⚠️ Simülasyonu yazarken kaçış maliyetini 1.83 sn saymıştım (bütün yarıçapı koşmak).
> O modelle kaçan oyuncu durandan KÖTÜ çıkıyordu. Gerçekte bir dash yetiyor — model
> düzeltilince tablo tersine döndü. **Yanlış model, yanlış ayarın en sinsi kaynağı.**

### İsabet kutusu asimetrik — KASITLI (2026-08-26)

| Yön | Nasıl ölçülüyor | Sonuç |
|-----|-----------------|-------|
| **Oyuncu → düşman** | menzil **ve açı** düşmanın YÜZEYİNE | cömert: gövdesi yaydaysa vuruyorsun |
| **Düşman → oyuncu** | menzil ve açı oyuncunun MERKEZİNE | dar: merkezin dışarıdaysa yemiyorsun |

Aksiyon oyunlarının standart pratiği — oyuncuya cömert vuruş kutusu, dar hasar kutusu.
Ölçülen pay: normal düşmanda **0.5 birim** (menzilin %28'i) + 1.5 birim mesafede **19.5°**.

Turuncu hazırlık yayı bu kuralla dürüst kalıyor: *"merkezin buraya girmesin"* diyor ve
izometrikte oyuncu zaten karakterinin ayak noktasını okuyor.

⚠️ Bu bir KARAR, kaza değil. Faz 10'da (zorluk kilidi) yeniden bakılacak — simetrik
yapılırsa düşmanlar belirgin şekilde daha çok vurur.

### İsabet menzili YÜZEYE ölçülür, merkeze değil

Enes: *"bossa vururken bazen buglanıyor vuramıyorum."* Sebep: menzil merkezden ölçülüyordu.
Boss 1.9 kat büyük olduğu için gövdesine değecek kadar yakınken bile merkezler arası mesafe
menzilin dışında kalabiliyordu — **oyuncu gördüğü şeye vuramıyordu.** Artık `Enemy.Radius`
düşülüyor. Test korumalı.

### Saldırı TAAHHÜTTÜR

Enes: *"saldırıya başlayınca hareket tuşları ile bozuluyor, dashin bozması lazım."*
`attackMoveDamping` 0.25 → **0**. Savurma sırasında hiç hareket yok; geri çekilince düşman
menzilden çıkıp vuruş ıskalıyordu. Saldırıyı bozan tek şey **dash**.

Ayrıca girdi gecikmesi: `attackHitMoment` 0.4 → **0.12**, `attackDuration` 0.35 → **0.30**.
Vuruş tıklamadan ~36 ms sonra iniyor (önce 140 ms'ti).

### Dash-atak silaha göre

Eskiden her silahta aynı geniş alan vuruşuydu ve silahın kimliğini siliyordu.
Artık: **tek hedefli silah dash'te de tek hedefli kalır ama çok sert vurur** (×1.8 ek çarpan),
**koni süpürür ama vuruş başına daha az.** Yay da silahın kendi rengiyle çıkıyor.

## Silahlar — Faz 3 ✅ (2026-08-24)

**Yapısal çözüm: `HarvestMultiplier`.** Koni, içindeki her düşmandan ayrı hasat ettiği
için hedef sayısı kadar üstün. Tek hedefli silahlar bunu **hasar başına daha çok zaman
çalarak** kapatıyor.

| silah | hedef | çarpan | özellik |
|---|---|---:|---|
| **KONİ** | koni | ×1.0 | Kıyas noktası. Geniş, herkese vurur |
| **FAİZLİ** | tek | ×2.6 | Savurmak saniye götürür, öldürünce iki katı geri verir. Iska pahalı |
| **DURGUN** | tek | ×2.2 | Basılı tutunca saat donar; saldıramaz, dash atamazsın. Dünya donmaz |
| **VADELİ** | tek | ×2.0 | ×2.4 hasar, bedel vuruştan SONRA kesilir — savurmanın ortasında ölebilirsin |

### Denge ölçümü — kesişim 3 düşmanda

`Tek Can → Denge simülasyonu`, düşman başına net saniye (TEMİZ oyuncu):

| silah | 1 düşman | 2 | 3 | 4 |
|---|---:|---:|---:|---:|
| KONİ | +0.7 | +4.1 | **+7.5** | **+10.9** |
| FAİZLİ | **+5.7** | **+5.7** | +5.7 | +5.7 |
| DURGUN | +4.8 | +4.8 | +4.8 | +4.8 |
| VADELİ | +2.6 | +2.6 | +2.6 | +2.6 |

**Sonuç: hiçbiri baskın değil.** Tek hedefliler yalnız/düello kavgalarında üstün (1 düşmanda
FAİZLİ koninin 8 katı), koni 3 düşmandan sonra devralıyor. Silah seçimi duruma bağlı bir
karar oldu — Faz 3'ün hedefi buydu.

> ⚠️ **Simülatörün dürüst sınırı:** DURGUN'un asıl değeri saati durdurabilmesi ve simülasyon
> bunu hiç modellemiyor. +4.8 rakamı onun yalnızca hasar tarafı. Gerçek değeri playtest'te
> ölçülecek. VADELİ de en zayıf görünüyor (+2.6) ama risk profili farklı — tek vuruşta
> büyük parça koparıyor, simülasyon ortalama alıyor.

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

### Düşman türleri ✅ (Faz 5, 2026-08-24)

Meta ilerleme olmadığı için tekrar oynanabilirliğin **tamamı** koşunun içinden gelmek
zorunda. Tek tip düşmanla dövüş iki dakikada tükeniyordu — oyunun en büyük batma riskiydi.

İlke: **yalnızca "daha çok can" veya "daha çok hasar" olan bir tür eklemek çeşitlilik değil,
dolgudur.** Her tür çekirdek soruyu (*hangi düşmana zaman harcayacağım?*) farklı bir yerden
vurmalı.

| tür | mekanik | değiştirdiği karar |
|---|---|---|
| **TEMEL** | dengenin sıfır noktası | — |
| **SIZINTI** | yaşadığı sürece saatini emer (0.55/sn); zayıf ve hızlı | **hedef sırası.** Diğerleri "ne kadar kârlı" diye sordururken bu "ne kadar pahalı" diye sordurur — önce onu öldürmezsen bütün kavga zarara geçer |
| **KASA** | vurarak **hiçbir şey** ödemez; bütün havuzunu öldüğünde tek seferde öder (×1.8). Kalın, yavaş | **taahhüt.** Yarım bırakırsan harcadığın zaman tamamen boşa gider |
| **KAÇAK** | havuzu %45'in altına inince kaçar; arena kenarına varırsa kaybolur ve zamanını götürür | **kovalamak mı, bırakmak mı.** Kovalamak seni kalabalıktan ayırır ve saat işlemeye devam eder |

**Kademeli açılış:** ilk dalga yalnızca TEMEL, sonra her dalgada bir yeni soru ekleniyor.
Hepsini birden atmak öğretmez, boğar.

**Deterministik kompozisyon** (rastgele değil): aynı dalga hep aynı karışımı verir, böylece
*"bu dalga mı zor, ben mi kötü oynadım"* sorusu cevaplanabilir kalıyor.

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

## Ses — Faz 6 ✅ (2026-08-25)

Sesler **koddan üretiliyor**: gürültü + zarf + tek kutuplu alçak geçiren filtre.
Ses dosyası yok, kütüphane yok, lisans yok. Görsel kimlik için verilen kararın aynısı —
sanatçı gerektirmeyen yere yaslan.

9 ses: savurma · isabet · hasar · dash · hasat · yere vuruş · alım · ölüm · kazanma.

### Sesler GEÇİCİ — Enes kararı (2026-08-25)

> *"seslerin hepsi kötü ama en azından ne olduğunu ayırt edebiliyoruz o açıdan okey.
> değiştirene kadar dursun test için"*

Şu anki sesler **test kalitesinde**: amaçları "ne oldu" sorusunu ayırt edilebilir
kılmak, his vermek değil. Silahlarda verilen kararın aynısı — yapı dursun, içerik sonra.

`Sfx.Hit()` gibi çağrı yüzeyi sabit; gerçek ses tasarımı gelince yalnızca üretim
tarafı değişecek, çağıran hiçbir kod dokunulmayacak.

### ⚠️ Sesin duyulması ses sisteminin işi

İki tur boyunca sesler doğru üretildi ve **hiç duyulmadı** — sahnede `AudioListener`
yoktu. Dersin kalıcı hali:

> Unity'de "var mı?" diye SORUP karar verme. `Object.Destroy` ertelenir; silinmekte
> olan nesne o kare boyunca bulunmaya devam eder, yani cevap yalan olabilir.
> Sahip olman gereken şeyi **koşulsuz KUR.**

Şablon temizliği artık `DestroyImmediate` (kaynak), `Sfx` dinleyiciyi kendi nesnesine
koşulsuz takıyor (yeniden başlatma için).

## Zamanlama animasyondan ÖNCE kilitlenir ✅ (2026-08-25, Enes)

> *"test için dövüş zamanlamaları kilitli animasyon eklemeden değişiklik yapıp
> en uyguna oturtmaya çalışalım"*

Hazırlık/vuruş/toparlanma süreleri **kapsüllerle** oturtulur. Animasyon sonra gelir ve
oturmuş sayılara uyar. Tersi yapılırsa — önce animasyon, sonra ayar — her his denemesi
bir sanat işine dönüşür ve tek kişilik ekip orada durur.

### Kural: zamanı `Tuning` sahiplenir, animasyon uyar

Animasyon klibi sabit bir **referans uzunlukta** yapılır, çalışma anında gerilir:

```csharp
animator.SetFloat("WindupSpeed", 1f / t.enemyWindupDuration);
```

Ayar değişince klibe dokunulmaz. Erken erişimde gelecek ayar değişikliklerinin tamamı
bu yolla emilir. Süre animasyonun içine **gömülmez** — gömülürse her denge yaması bir
animasyon işi olur.

### ⚠️ `attackHitMoment` animasyonla birlikte ORANA döner

Şu an saniye cinsinden (0.12 / 0.30 = vuruşun %40'ı). Süre 0.40'a çıkarsa hasar %30'a
kayar, ama animasyondaki "silah hedefte" pozu %40'ta kalır → **görüntü ile hasar ayrışır.**

Animasyon geldiğinde: `attackHitMoment` oran olur (0.4), saniye
`attackDuration × hitFraction` diye hesaplanır. Tek sayı ikisini birden yönetir.

### Germe sınırı: ±%40

O aralıkta gerilme göze görünmez. Ötesinde (0.20 → 0.60) animasyon sünük veya bulanık
görünür, yeniden yapılması gerekir. **Kilitlemenin amacı bu:** "bir daha değişmeyecek"
değil, *"doğru mahalleye otursun ki germe payı içinde kalsın."*

Animasyonlar beklenen ayar aralığının **ortasına** göre yapılır, o anki değere göre değil —
iki yöne de pay kalsın.

### Kilitleme sırası: odalardan SONRA, animasyondan ÖNCE (2026-08-25)

Zamanlama kilidi başta "hemen sırada" diye planlanmıştı; Enes *"animasyon ve asset
gelmeden önce daha mantıklı değil mi"* diye sordu ve haklıydı.

**Gerekçe:** mekân, zamanlamanın hissini değiştiriyor. `dashDistance 2.5` geniş arenada
kısa bir sıçrama, dar bir odada odayı baştan başa geçmek demek — `attackRange` de öyle.
Oda yapısı oturmadan kilitlenen sayılar, odalar gelince kırılmak zorunda kalır.

**Sıra:**

```
hikâye/mekân kararı → oda yapısı → düşman kadrosu kesinleşir
→ zamanlama kilidi → animasyon + asset
```

**İkinci gerekçe:** ortada çözülecek somut bir his sorusu yok. Tek açık şikâyet Faz 1'den
kalma ("biz çok hızlı, düşmanlar yavaş vuruyor") ve o gözlem attack-commitment
düzeltmesinden, tek hedefli silahlardan ve boss sertleştirmesinden ÖNCE yapıldı.
Var olmayabilecek bir soru için alet kurmak aletin kendisini israf eder.

**⚠️ Ertelemenin riski:** dövüş zamanlaması gerçekten bozuksa bundan sonraki her
playtest kirlenir — oda tasarımı yargılanırken aslında dövüş yargılanır ve yanlış yer
düzeltilir. Bu yüzden odalara başlamadan önce tek soruluk kontrol yapılıyor.

**Kontrol yapıldı ve GEÇTİ** (2026-08-25). Enes: *"genel zorluğa göre kolay değil.
Taze Hades 2 bitirdim ama yine de hiçbir şeysiz bu kadar düşman zorladı."* Faz 1'deki
"biz çok hızlı, düşmanlar yavaş" hissi geçmiş. Oda testleri kirlenmeyecek.

---

## Ölüm ve hikâye yapısı ✅ (2026-08-25, Enes)

**Hedef: 8-10 saatlik hikâye, belki daha fazla.** Tek koşu, tek can.

### Karar

> *"Saf tek can gibi olsun ama kullanıcı hikayede ilerledikçe bir şey kazansın.
> Öldükçe onu geri getirecek ve checkpoint'ten devam etmesini sağlayacak bir eşya.
> Eğer yoksa ve ölürse game over. Eşya kullanıldığında senin ölümünle şekillenmiş
> bir dünyada oynuyorsun."*

- **Varsayılan hâl: TEK CAN.** Elinde bir şey yoksa ölüm gerçekten sondur.
- **Diriliş eşyası** *(isim Enes'in — çalışma adı)*: hikâyede ilerledikçe kazanılır.
  Sayılı, görünür, biriktirilebilir.
- **Kullanıldığında** checkpoint'ten devam edersin — ama **dünya senin ölümünle
  şekillenmiştir.** Ölüm geri sarma değil, anlatısal bir olay.
- Eşya yoksa + ölüm = **game over.** *(Enes: "burayı başka bir şey de yapabiliriz,
  kafamda güzel fikirler var" — açık.)*

### Neden bu yapı tutuyor

**İsim yalan söylemiyor.** Ekstra canlar bağışlanmıyor, KAZANILIYOR. Tek can varsayılan
hâlin; fazlası senin emeğin. "Tek can" bir zorluk ayarı değil, oyunun kimliği olarak
kalıyor.

**Oyunun en değerli nesnesi oluyor.** Hiçbir silahtan, hiçbir sunak yükseltmesinden
kıymetli. Her risk kararı "hâlâ var mı?" sorusundan geçiyor — sunaktaki zaman harcama
kararının üstüne ikinci bir katman biniyor.

**Yasayı bozmuyor.** Diriliş eşyası PARA BİRİMİ DEĞİL — satın alınmıyor, hikâyeyle
kazanılıyor. Yani "tek bir sayı var: saniye" yasası duruyor. ⚠️ Eşya satın alınabilir
hâle gelirse ikinci bir para birimi doğar ve yasa çatlar. *(Açık karar.)*

### Üç çeşit diriliş eşyası ✅ (2026-08-25)

Hepsi **hikâyeden** kazanılır — satın alınmaz, yani ikinci para birimi doğmaz, yasa durur.
Birden fazla taşınabilir ve **kullanılmışlar tamir edilebilir.**

| # | Eşya | Ne yapar | Saat |
|---|------|----------|------|
| 1 | **Ölüm aldatma** | Yerinde dirilirsin, dövüş devam eder | Checkpoint'teki zamanın **küçük bir kısmı** |
| 2 | **Geçici dönüş** | Ayakta tutar; o süre içinde yenemezsen checkpoint'e atar | **10 saniye** |
| 3 | **Checkpoint** | Son checkpoint'e döner | Checkpoint'e kaydedilmiş zaman |

**⚠️ Kullanım sırası oyuncuya SORULMAZ, tasarıma gömülüdür.** Menü yok, öncelik ayarı yok —
sıra zaten doğal: *önce devam etmeyi dene (1) → sonra kazanmayı dene (2) → sonra kaybet (3).*
Oyunun tüm HUD'ı tek bir sayı; üstüne envanter + öncelik ekranı binerse minimalizm çöker.
Saatin yanında birkaç ikon yeter.

**Tamir ZAMANLA yapılır.** Ayrı malzemeyle olsaydı ikinci para birimi doğar ve yasa
çatlardı. Zamanla olunca oyunun en güzel kararı doğuyor:
*"90 saniyemi verip güvenlik ağımı mı tamir edeyim, yoksa o 90 saniyeyle ilerleyeyim mi?"*
Kendi ömrünü verip ikinci şans satın alıyorsun — temanın tam kalbi.

### Checkpoint ✅

- **Aralık: 30 dk – 1 saat.** *(C5'teki "3 bölüm" anlatısal perdeler; buradaki "bölüm"
  o değil, daha küçük kesit.)*
- Checkpoint'e ulaşınca **o andaki zaman kaydedilir**; diriliş o değeri geri yükler.
  Böylece "3 saniyeyle ölüp 3 saniyeyle dirilip anında tekrar ölme" sorunu doğmuyor.
- **⚠️ Ölüm diske ANINDA yazılmalı.** Yazılmazsa oyuncu ölmeden oyunu kapatır ve kalıcı
  ölüm diye bir şey kalmaz. Kalıcı ölümlü oyunları bozan tuzak birebir budur.
- ⚪ 30 dk mı 1 saat mi — oynayarak. Gerilim kadranı eşya sayısından çok BU.

### Ölümle şekillenmiş dünya ✅ — (a) + (c) birlikte

**(a) Seni öldüren güçlenir.** Çaldığı zaman bir yere gitti, artık onda.
**⚠️ Sadece boss ve isimli düşmanlar** — tam nemesis sistemi (Shadow of Mordor) çok pahalı;
sıradan düşman seni öldürünce yalnızca o odaya iz kalır. Boss ise ad alır, çaldığı zamanı
havuzuna ekler, tekrar karşına çıktığında gözle görülür şekilde değişmiş olur. Az iş, çok etki.

**(c) Sen ölüyken dünya ilerledi.** ⚠️ **Betikle yazılır, simüle EDİLMEZ.** Checkpoint başına
elle yazılmış varyasyon: bir kapı kapanmış, biri gitmiş, bir diyalog değişmiş. Gerçek dünya
simülasyonu tek kişilik ekiple bitmez.

### Game over = oyunun tamamı biter ✅

Elinde eşya kalmadıysa ölüm gerçekten sondur.

**⚠️ Gerçek tehlike sertlik değil, SÜRPRİZ.** 7 saat oynayıp son eşyasının bittiğini
fark etmeden ölen oyuncu iade de edemez (Steam penceresi 2 saat). Son eşyaya düşünce
oyun bunu **bağırmalı** — ekran, ses, saatin görünümü değişsin. Oyuncu *"bittiğimi
biliyordum, kendi hatam"* demeli; o zaman aynı sertlik övgüye döner.

---

## Dünya, hikâye ve kadro ✅ (2026-08-25, Enes)

**Evren:** Kitap serisiyle **aynı evren**, ama kitapla ilgisi olmayan bir konu.

**Kahraman:** Bir erkek *(ad henüz yok)*. Ailesini ve kendisini zamandan kurtarmaya çalışıyor.

**Kurgu — zaman neden tek kaynak:** Zamanda yolculuk deneyi sırasında bir patlama oluyor.
Sonucunda **zaman tek korunan büyüklük hâline geliyor** — madde, enerji, değer, hepsi
süreye çöküyor. Bu yüzden yaşamak, almak ve dövüşmek aynı şeyi harcamak zorunda; mekanik
keyfi değil **kaçınılmaz.** Sonradan bazı insanlar zamanı manipüle etmeyi öğreniyor ve
başımıza gelenlere sebep oluyorlar. Biz intikam + kurtarma peşindeyiz.

**⚠️ Hikâye BOĞMAYACAK.** Girişte geçmişin yalnızca bir kısmı anlatılır, gerisi ilerledikçe
şekillenir. Hem oyuncu boğulmaz hem yazım yükü öne yığılmaz.

### Yapı: 3 perde

| Perde | Düşman türü | Boss |
|-------|-------------|------|
| **Giriş** | 2 ile başlar, 3'e çıkar | 1 |
| **Gelişme** | +3 yeni (aşamalı) → 6 | 1 |
| **Sonuç** | +1 yeni → 7 | 2 |

**Toplam: 7 düşman türü, 4 boss.** *(Şu an 4 tür + 1 boss var.)*

**Bölgeler:** en az 3, kurala ve hikâyeye göre ayrılıyor. → **Faz 10'da kuruldu, aşağıda.**

### Bölge kuralları ✅ (Faz 10, 2026-08-26)

Enes'in *"kurala göre ayrılır"* kararının somut hâli. **Her kural SAATTEN geçiyor** —
bölgeye özel can, zırh, direnç YOK. Olsaydı ikinci bir sistem doğar ve yasa çatlardı.

| Bölge | Kural | Mekanik | Doğurduğu soru |
|-------|-------|---------|----------------|
| **EŞİK** | — | — | (öğrenme bölgesi) |
| **DARALMA** | oyalanmak pahalı | dövüş dışı akış ×1.7 | *acele mi etsem?* |
| **KITLIK** | hasat az | vuruş kazancı ×0.65 | *bu düşman almaya değer mi?* |
| **AĞIRLIK** | hata affetmiyor | darbe bedeli ×1.8 | *kaçınmak zorunda mıyım?* |
| **SIZINTI** | sürekli emiliyor | **dövüşte bile** 0.6 sn/sn | *nefes alacak vaktim var mı?* |

Her biri **tek bir çarpan** — ucuz kod, ama bölgeler gerçekten farklı HİSSETTİRİYOR,
sadece farklı görünmüyor. Kuralın oyuncuya sorduğu soru sütunu asıl tasarım; çarpan
onun aracı.

**Zincir:** ara bölgelerin boss'u düşünce çıkış açılır, sıradaki bölgeye geçilir.
Koşu YALNIZCA son bölgenin boss'u düşünce kazanılır. Boss her bölgede sertleşiyor
(`regionBossGrowth`).

**Düşman havuzu bölgeye bağlı** — yeni tür yeni MEKÂNLA geliyor, oda numarasıyla değil.

**Kimlik (greybox):** zemin rengi + HUD'da bölge adı ve kuralı. Kural hissediliyor ama
görünmüyor; yazı onu duyuruyor ki oyuncu *"burada neden hızlı eriyorum"* diye sormasın.

### ⚠️ Bu bölgeler TEMSİLİ — neyin kalıcı olduğu (Enes sordu, 2026-08-26)

Silah / ses / VFX kararının aynısı: **yapı dursun, içerik sonra.**

**KALICI — mekanizma:**
- Bölge kavramı: odalar + düşman havuzu + kural + boss
- Zincir: ara boss bölgeyi bitirir, son boss koşuyu kazandırır
- **Kuralın SAATTEN geçmesi** — yasa sonucu, tercih değil
- Düşman havuzunun bölgeye bağlı olması · checkpoint'in bölgeyi kaydetmesi

**GEÇİCİ — içerik:**
- **İsimler** (EŞİK/DARALMA/KITLIK/AĞIRLIK/SIZINTI) — ajanın uydurması.
  Gerçek bölgeler Enes'in dünyasının yerleri olacak, adlandırma onda
- Kural şiddetleri (×1.7 / ×0.65 / ×1.8 / 0.6) — hepsi tahmin, oynanarak oturacak
- Zemin renkleri — greybox
- `regionsPerAct = 5` — sırf bütün kurallar test edilebilsin diye
- Hangi kuralın hangi bölgede olduğu — hikâyeye göre değişecek

**ORTADA — karar bekliyor:** dört kural TİPİNİN kendisi (baskı / kıtlık / ağırlık /
sızıntı). Ajanın önerisi, Enes'in kararı. Playtest'in asıl sorusu bu:
*bu tip kurallar bölgeleri gerçekten farklı yapıyor mu?*
Yapıyorsa mekanizma doğru, kurallar sonra yazılır. Yapmıyorsa bölgeleri başka bir
şeyle ayırmak gerekir — mekân, düşman kadrosu, yapı.

**Playtest cevabı ✅ (2026-08-26):** *"bölgeler gittikçe zorlaşıyor canımı çıkarttılar"* —
kurallar bölgeleri gerçekten farklı yapıyor, mekanizma doğrulandı.

## ⚠️ ZORLUK ÇOK YÜKSEK — ertelendi (Enes, 2026-08-26)

> *"oyunun güncel hali çok zor ileride değiştireceğiz"*

Ayar ertelendi ama **sebebi şimdi yazılıyor**, yoksa ileride kör ayar yapılır.

### ⛔ AŞAĞIDAKİ İKİ BÖLÜM ESKİDİR — SONUNDAKİ DÜZELTMEYE BAK

Bu bölümdeki *"asıl suçlu boss odası"* ve *"beş eğri çarpılıyor"* teşhislerinin
**ikisi de çürüdü.** İkisi de yükseltmeleri yok sayan bir modelden çıkmıştı.
Geçerli teşhis: **"RAPOR DÜZELTİLDİ"** başlığı — hasarın tavansız olması.
*(Yanlış teşhisler kayıt için duruyor; nasıl yanıldığımız da bilgi.)*

### ÖLÇÜLDÜ (2026-08-26) — ilk tahminim yanlıştı ⛔ BU DA ESKİDİ

`Tek Can → Zorluk eğrisi raporu` her (bölge, oda) için kazanç/masraf hesaplıyor.
Oda kompozisyonu **üretimden okunuyor** (gerçek `RoomManager` odayı kuruyor),
yalnızca dövüş modelleniyor.

**Aşağıdaki "beş eğri çarpılıyor" tahminim ÖLÇÜMLE ÇÜRÜDÜ.** Gerçek tablo:

| | TEMİZ oyuncu | ORTA oyuncu |
|---|---|---|
| Nerede ölüyor | bölge 2, oda 2 | bölge 1, oda 3 |
| Yürüme | %26 | %16 |
| Dövüş akışı | %36 | %31 |
| Yenen darbe | %19 | %37 |
| Aura | %19 | %16 |

**① ASIL SUÇLU: BOSS ODASI.** Bölge 1 boss'u tek başına **−23 saniye**:
kazanç 28 · masraf 51 (19 dövüş + **21 aura** + 9.6 yürüme).
Sebebi tasarım: `bossHarvestMultiplier` 0.4 (boss seni beslemiyor) + `bossAuraDrain`
1.1/sn × uzun dövüş. Her bölge bir boss'la bitiyor, yani **her bölge −23 ile kapanıyor.**
5 bölge = başlangıç saatinin (60) neredeyse iki katı, sadece boss'lardan.

**② Normal odalar ASLINDA DENGEDE.** Bölge 1'de net: −6.1 · −6.9 · −3.9 · **+0.5** · **+2.8**.
Oda kalabalıklaştıkça pozitife dönüyor — sabit yürüme bedeli daha çok düşmana bölünüyor.
Yani oda büyüme eğrileri sorun değil; tam tersi, işe yarıyor.

**③ Yürüme SABİT ve pahalı:** oda başına 3.8 sn yol × 2.5/sn = **9.6 sn**, başlangıç
saatinin %16'sı. DARALMA'da ×1.7 → 16.3 sn. O çarpan tam da SABİT masrafa biniyor,
yani en acımasız yerde.

### ⚠️ Model düzeltmesi — ilk ölçüm de yanlıştı

İlk sürüm bütün düşmanları dövüş boyunca yaşatıyor ve **aurayı ciddi şekilde
abartıyordu** (aura %32 görünüyordu, düzeltince %19). Gerçekte düşmanlar sırayla ölüyor,
aura azalıyor. Düzeltilince tablo tamamen değişti: TEMİZ oyuncu bölge 1'i geçer oldu.
*Yanlış model, yanlış kadranı çevirtir.*

### Kaçınma pencereleri SAĞLAM (ölçüldü)

Zorluk haksız kare pencerelerinden gelmiyor:
dash dokunulmazlığı 12 kare vs düşman vuruşu 6 kare (+6 pay ✅) ·
dash mesafesi 2.5 > düşman menzili 1.8 ✅ · dash bekleme 0.5 < saldırı aralığı 1.5 ✅

### Ayar sırası (ölçüme göre güncellendi)

1. **BOSS ekonomisi** — `bossHarvestMultiplier` ve `bossAuraDrain`. En büyük tek kalem
2. **Yürüme bedeli** — `arenaRadius` (oda boyu) ya da `drainOutOfCombat`
3. **DARALMA çarpanı** — sabit masrafa bindiği için en sert kural
4. Kalan kural şiddetleri

⚠️ Rapor **yükseltmeleri yok sayıyor** — sunaktan alınan hasat/hasar tabloyu pozitife
kaydırır. → **BU VARSAYIM SONUCUN TAMAMINI BELİRLİYORMUŞ, aşağıda düzeltildi.**

### ✅ RAPOR DÜZELTİLDİ (2026-08-26) — asıl sorun BUYMUŞ

Enes testte **1200 saniyeye** çıktı; rapor "her oda net negatif" diyordu. Rapor
yükseltmeleri modellemiyordu ve o eksik varsayım tabloyu tersine çeviriyormuş.
Yükseltme ilerlemesi modele katıldı (`ALIMSIZ` / `ALICI` profilleri):

| Profil | Sonuç |
|--------|-------|
| TEMİZ / ALIMSIZ | bölge 2 oda 2'de ölüyor |
| **TEMİZ / ALICI** | **koşuyu +297 sn ile bitiriyor** *(sunağa 396 sn ödemiş)* |
| ORTA / ALIMSIZ | bölge 1 oda 3'te ölüyor |
| ORTA / ALICI | bölge 1 oda 3'te ölüyor *(ilk sunağa yetişemiyor)* |

**① ASIL SUÇLU: HASARIN TAVANI YOK.** `altarDamageStep` 1.2 ve sınırsız — hasar
koşu boyunca **3.5 → 19.1**'e çıkıyor. Ve hasar **üç kalemi birden** iyileştiriyor:
dövüş kısalıyor → akış azalıyor → aura azalıyor. Tabloda görünüyor: dövüş masrafı
bölge 1'de 9.7, bölge 4'te **0.8**. Hasat tavanlı (%70), hasar değil.

**② SUNAK FİYATLARI HER ODA SIFIRLANIYOR.** `Altar._bought` bir **örnek alanı** ve
sunaklar her odada YENİDEN doğuyor — fiyat artışı (×1.6) koşu boyunca birikmiyor,
her sunak odasında 6 saniyeden başlıyor. 42 alım yapılabiliyor.

**③ Boss ekonomisi kendi kendine düzeliyor.** "Boss −23 sn" bulgusu yalnızca DÜŞÜK
hasarda doğruydu; hasar yükselince boss odaları **+40'a** çıkıyor. Yani boss'a
dokunmadan önce hasar eğrisi düzeltilmeli — yoksa yanlış kadranı çeviririz.

**④ Yenen darbe %32-37** ile en büyük masraf kalemlerinden. Zorluk buradan gelmeli,
akıştan değil.

### ⏸️ DENGE DEĞİŞİKLİKLERİ SONA ERTELENDİ (Enes, 2026-08-26)

> *"bu denge değişikliklerini sona da atabiliriz"*

Doğru: kadro, silahlar ve içerik tamamlanmadan ayar yapmak, içerik gelince baştan
ayarlamak demek. **Teşhis hazır ve bekliyor**; ayar oturumu animasyon öncesinde,
zamanlama kilidiyle aynı seansta yapılacak.

### Ayar sırası (ölçüme göre — ikinci kez güncellendi)

1. **Hasar yükseltmesine TAVAN** ya da azalan getiri — tek büyük kaldıraç bu
2. **Sunak fiyat birikimi** — `_bought` koşu boyunca korunmalı (oda başına değil)
3. Sonra koşu uzunluğu ve kural şiddetleri
4. **Boss ekonomisine EN SON dokunulur** — ilk üçü düzelince kendiliğinden değişiyor

---

### Eski tahmin (ölçümden ÖNCE, kayıt için)

Beş ayrı büyüme eğrisi aynı anda işliyor ve her biri **tek başına** seçilmişti:

| Kaynak | Alan | Etkisi |
|--------|------|--------|
| Oda başına düşman sayısı | `roomCountGrowth` 1.5 | her odada +1.5 düşman |
| Oda başına havuz | `roomPoolGrowth` 1.18 | her odada %18 daha dayanıklı |
| Bölge boss'u | `regionBossGrowth` 1.35 | her bölgede %35 daha büyük boss |
| Bölge kuralı | ×1.7 / ×0.65 / ×1.8 / 0.6 | üstüne biner |
| Tür açılımı | bölgeye bağlı | daha çok tehdit çeşidi |

Bunların **çarpımı** hiçbir yerde hesaplanmadı. 5. bölgenin 5. odası, tasarlanmış bir
zorluk değil — dört eğrinin kazasıyla çıkan bir sayı.

### Ne kadarı test artefaktı

⚠️ `regionsPerAct` **5'e ajan tarafından çıkarıldı** — sırf bütün kurallar
denenebilsin diye. Gerçek koşu muhtemelen daha kısa olacak; yani zorluğun bir kısmı
tasarım değil, test ayarı.

### Ayarlarken bakılacak sıra

1. **Önce koşu uzunluğu** (`regionsPerAct`) — kaç bölge gerçekten olacak
2. **Sonra eğrileri TEK TEK** ölç, çarpımı gör (`BalanceSim` genişletilebilir)
3. **En son kural şiddetleri** — çünkü onlar en üste biniyor

Bu, Faz 10'un (zamanlama kilidi) doğal parçası: aynı oturumda A/B aracıyla yapılmalı.

**Odalar:** karışık — her büyük perdede **birkaç basit şablon** tekrar kullanılır,
tek kullanımlık set-piece'ler ayrı yapılır. Tamamı el yapımı olsaydı bitmezdi.

**⚠️ Düşmanlar odada DURUYOR olur.** Doğurma yalnızca **boss dövüşlerine özel** —
ikinci faz, yardımcı çağırma, arena değişimi. Böylece doğurma bir kural değil
**boss'un imzası** oluyor; sıradan dövüş adil kalıyor (kalıcı ölümde pusu ucuz hissettirir),
boss doğurunca "bu farklı" diye okunuyor. Ayrıca en az iş çıkaran seçenek bu:
odayı tasarlamak = dövüşü tasarlamak.

### Silahlar ✅ — 5 tane

4+4 yuva fikri **elendi**: 8-10 saatlik oyun için gereksiz. **5 silah yeter**, eldekiler
ayarlanır/değiştirilir. *(Şu an 4 var: KONİ · FAİZLİ · DURGUN · VADELİ.)*

Silahlar **yolda bulunur.** ⚠️ 1-4 tuşlarıyla anında değiştirme **test kısayolu** —
ENTER ve B gibi gerçek oyunda kalkacak.

**Karakter:** tek, seçilemez.

### Erken erişim ✅ — en az 2 perde

Enes'in kararı: EA için **giriş + gelişme** hazır olmalı, sonuç saklanır.

Bu doğru: perde 3 ödemenin kendisi, EA'da finali vermek istemezsin. Tek eklemem —
gelişme perdesi **parça parça** da gelebilir; EA güncellemeleriyle doldurmak standart
pratik ve piyasaya daha erken çıkarır.

---

# HİKÂYE ÇERÇEVESİ — BORÇ EKONOMİSİ ✅ (2026-08-26, Enes onayladı)

## Nereden çıktı: oyunun dili zaten kurulmuştu

Silah ve düşman adlarına bakınca ortaya çıktı:

| Silahlar | Düşmanlar |
|----------|-----------|
| **FAİZLİ** · **VADELİ** · **DURGUN** | **KASA** · **SIZINTI** · **KAÇAK** |

Faiz, vade, kasa, sızıntı, kaçak — bu bir **borç ekonomisi sözlüğü.** Farkında olmadan
dünyanın dilini çoktan kurmuşuz. Zaman para olunca ortaya çıkacak şey de tam olarak bu.

⚠️ **Yöntem notu:** hikâye sıfırdan uydurulmadı, **zaten orada olan adlandırıldı.**
Silahlarda, düşmanlarda, bölgelerde ve seslerde izlenen kalıbın aynısı.

## Dünya

Patlamadan sonra zaman **tek korunan büyüklük** oldu. Zamanı olan yaşar, olmayan durur.
Her para biriminde olduğu gibi **onu ödünç verenler** türedi: zamanı manipüle edebilenler
bir ekonomi kurdu. Faizle zaman verirler; ödeyemezsen alırlar.

## Bahis: aile TEMİNAT

**Ölmediler. REHİN VERİLDİLER.** Bir borcun teminatı olarak tutuluyorlar ve borç
işledikçe onların zamanı çekiliyor. Kahraman ulaşamıyor.

Yani *"aileni zamandan kurtarmak"* birebir şu: **borcu kapatmak ya da alacaklıyı öldürmek.**

⚠️ Bu, saatin baskısını HİKÂYEYE bağlıyor: acele etmen sadece mekanik değil — sen
oyalandıkça onlar da tükeniyor.

## Dört boss = ekonominin dört katı

**Her boss SUÇUNU DÖVÜŞÜYOR.** Mekanikler uydurma değil, ne yaptıklarının doğrudan
sonucu. *(Adlar çalışma adı — nihai adlandırma Enes'te.)*

| # | Boss | Ne yapmış | Faz 1 | Faz 2 |
|---|------|-----------|-------|-------|
| ① | **TAHSİLDAR** | zorla alan, sokak seviyesi | yanındayken emer → uzak dur, vur, çekil | emmeyi bırakır, **KOVALAR** → uzak durmak işe yaramaz |
| ② | **EKSPER** | zamanının ne ettiğine karar verir | hasadın düşük → acele et | hasadını **SIFIRLAR** → ondan artık hiçbir şey alamazsın |
| ③ | **KEFİL** | borcu garantiye alır | normal dövüş | **İKİYE BÖLÜNÜR**, ikisi de yarım havuzla |
| ④ | **MURABAHACI** | sözleşmeyi elinde tutan, son boss | büyük ama düz | **ZAMANINI GERİ SARAR** — vurmadığın her saniye havuzu doluyor |

⚠️ Her fazın **farklı bir soru** sorması şart. "Aynısı ama daha çok" faz değildir.

## Faz geçişi ✅ (Enes kararı)

- **① ve ②** → havuz **yarıya** düşünce faz 2
- **③ ve ④** → havuz **bitince FULLENİR** ve faz 2 başlar
  *("kazandım" sandığın an bitmediğini öğreniyorsun — ve zamanı derinden manipüle
  edebilenlerin ikisi bunlar, yani kurguyla örtüşüyor)*

## ⚠️ İKİNCİ FAZIN HAVUZU KREDİDİR, ALTIN DEĞİL

③ ve ④ fullenince havuzları iki kez sömürülebilir olurdu — ve son iki boss oyunun
en büyük ZAMAN KAYNAĞI hâline gelirdi. Oysa tasarım boss'un seni beslememesini istiyor
(ölçüm de boss ekonomisini zaten sorunlu bulmuştu).

> **İkinci fazda vurursun, havuz erir, ama sana HİÇBİR ŞEY GEÇMEZ.**
> Çünkü o zaman onun değildi zaten — gösterdiği para sahte, kredi.

Murabahacı için birebir: sana borç gösteriyor, elinde bir şey yok.
**Ölçümün bulduğu boss ekonomisi sorununu sayı çevirerek değil KURGUYLA çözüyor.**

## Nemesis buraya oturuyor

Seni öldüren boss, senden aldığı zamanı **hesabına geçiriyor** ve bir dahaki
karşılaşmada daha büyük oluyor. Borç metaforu kesintisiz devam ediyor.

## Hikâye NASIL anlatılacak — dört kanal, hepsi ZATEN VAR

Hikâye oyunun üstüne EKLENMİYOR; var olan yapıya **isim veriliyor.**

**① Checkpoint = güvenli nokta.** Onarım orada yapılıyor, doğal soluklanma yeri.
**② Ölüm = TEMERRÜT.** Diriliş eşyası harcamak "borcu bir kez daha ertelemek".
   Kalıcı ölüm bir ceza değil, **olay örgüsü anı** — "dünya ilerledi" buraya bağlanıyor.
**③ Bölge = alacaklının bölgesi.** Kural, orayı kimin yönettiğinin sonucu.
   DARALMA'nın acelesi bir tahsildarın bölgesi olduğu için. **Kural = kimlik.**
**④ Boss = hesaplaşma.** Doruk zaten orada.

⚠️ Enes: *"hikâye boğmasın"* — girişte geçmişin yalnızca bir kısmı anlatılır.

## Açık

- Adlandırma (kahraman, boss'lar, bölgeler, eşyalar) — Enes'te
- Borcu kim aldı, neden? Kahraman mı imzaladı, ailesi mi, miras mı?
- Borç KAPATILABİLİR mi (alternatif son) yoksa tek çıkış öldürmek mi?
