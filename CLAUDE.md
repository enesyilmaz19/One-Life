# Tek Can — Proje Talimatları

Unity **6000.5.2f1** · URP · **izometrik 3B** · aksiyon roguelike · Steam / erken erişim hedefi.

## Yasa (her tasarım kararı buna sorulur)

> **Tek bir sayı var: saniye. Canın da o, süren de o, paran da o.**

Can barı **yok ve yazılmayacak.** Altın/ruh/ayrı para birimi **yok.** Meta ilerleme **yok.**

**Ölüm (Enes, 2026-10-12):** koşu tek bir hikâye, taban 8 saat. Ölünce **son kontrol noktasına** dönersin
(mini boss'un temizlediği ara bölge, ya da ana boss'un temizlediği bölge). Kontrol noktaları
**aşınır**: ölümde kaybettiğin saniyeler seni öldürenin NEMESİS defterine yazılır, defter dolarsa
temizlediğin son mini boss geri gelir ve bir önceki kontrol noktasına düşersin. Ana boss'un
temizlediği bölge geri gelmez. *(Eskiden: "ölünce her şey gider".)*

Bir özellik bu yasayı bozuyorsa özellik gider, yasa kalır.

## Belgeler

- `tasks/todo.md` — fazlar, ilerleme, **📥 ENES'İN MASASI** bloğu
- `docs/TASARIM.md` — tasarım, kararlaştırılanlar, açık kararlar
- `../FIKIR_DEFTERI.md` — konseptler, silah tohumları, **elenen fikirler ve neden** (tekrar önerme)

## Çalışma kuralları

- **Her faz sonunda dur.** Ne yapıldığını ve nasıl test edileceğini anlat, Enes'in onayını bekle. Yeşil testler "faz tamam" demek değil.
- Enes'e düşen işler `tasks/todo.md` içindeki ENES'İN MASASI bloğunda tutulur **ve mesajda ayrıca yazılır** — dosyaya gömmek yetmez.
- **Klavye girdisi otomasyondan Unity play-mode'a geçmiyor.** His testini insan yapar; ajan log + ekran görüntüsüyle doğrular. Kanıt: `Logs/Editor.log`.
- Görsel kimlik **silah ve VFX** üzerine kurulu, karakter sade ve okunur. VFX sanatçı gerektirmeyen tek sanat dalı — kimliği oraya yasla.

## Ayar kaynağı — tek kural

**`Tuning.asset` çalışma anının tek gerçeğidir.** `Tuning.cs`'teki varsayılanlar yalnızca
asset'te **henüz olmayan** yeni alanların tohumudur; var olan bir alanı .cs'ten değiştirmek
hiçbir işe yaramaz, asset onu ezer.

- **His/denge değeri değiştireceksen → Inspector.** Oyun asset'in bir **çalışma
  kopyasıyla** koşuyor (A/B profilleri asset'i bozmasın diye), ama **MEVCUT profilinde
  Inspector değişiklikleri canlı geçmeye devam ediyor** — 0.2 sn'de bir içeri alınıyor.
  ⚠️ Bir A/B profili (F2-F4) seçiliyken köprü KAPALI: kopya bilerek ayrışmış durumda.
  Inspector'dan ayar yapacaksan **F1'de ol.**
- **C# varsayılanlarını asset'e taşımak istiyorsan → `Tek Can → Ayarları senkronla (C# → asset)`.**
  Tek yönlü ve elle tetiklenir: otomatik olsaydı biri .cs'e dokunduğu an Inspector'da
  ayarlanmış bütün his değerleri sessizce silinirdi.
- **Sessiz sapma imkânsız:** `TuningAudit` her script derlemesinden sonra kendiliğinden
  çalışır ve ayrışma varsa Console'a uyarı basar. (2026-08-24'te bir playtest seansı
  bu uyarı olmadığı için yanlış varsayımla geçti.)

⚠️ **Kuralın istisnası — asset'te HENÜZ OLMAYAN alanlar.** 2026-08-24'te tam senkron
yapılmıştı, ama sonradan eklenen alanlar asset'e hiç yazılmadı: **118 alanın 49'u
dosyada yok** (2026-09-25; araç sayıyor, tahmin değil — güncel sayı `TuningAudit` çıktısında). O alanlarda kural TERSİNE işliyor — asset
ezecek bir kayıt taşımadığı için **.cs değişikliği uygulanıyor.** Oda/bölge büyümesi,
düşman rütbesi, checkpoint/diriliş, Faz 13 tür kancaları ve HACİZ / İPOTEK / İFLAS
kadranlarının **hepsi** bu grupta — yani ayar oturumunun kadranlarının çoğu.

`TuningAudit` artık hangi alanların dosyada olmadığını da yazıyor. Inspector'dan bir
kere düzenlersen asset'e yazılır ve o andan sonra normal kurala döner.
**`Ayarları senkronla`yı çalıştırma** — tek yönlü ve TOPLU, Inspector'da ayarlanmış
80 alanın hepsini C# varsayılanına geri çeker.

## Doğrulama — tek komut

`Tek Can → TÜM kapıları çalıştır`, ya da batch'ten:
`-executeMethod TekCan.EditorTools.AllChecks.Run`

**Bütün** faz öz-denetimlerini, **hasar** ve **boss isabet** reprodüksiyonlarını ve ayar
denetimini **tek Unity açılışında** çalıştırır; bir kontrol düşerse 1 ile çıkar.
*(Yeni faz eklerken `AllChecks.Run` içine bir satır ekle — buradaki sayı eskimesin diye
faz numarası yazılmıyor.)* (Ayar sapması yalnızca uyarı —
hangi değerin doğru olduğu Enes'in kararı.)

**Otopilot — oyunu GERÇEK Play modunda oynayan bot** (Faz 25b). Kapılar zamanı elle
ilerletiyor; otopilot oyunun kendi karelerinde, Unity'nin SANAL klavye/faresiyle oynuyor
(işletim sistemi tuşları Play'e geçmiyor, sanal cihaz geçiyor). Enes'in kaydına dokunmaz.
- İzle: `Tek Can → Otopilot (izle)` — gerçek hızda, ekranda yazılı, ekran görüntülü
- ⏩ **Hızlandırılmış zaman** (Enes 2026-10-18: "testler çok uzun sürüyor"): izlenmeyen koşuda her kare sabit 1/60 sn
  (`Time.captureDeltaTime`) — oyun bilgisayarın yetiştiği hızda, ölçülen **~×30** (68 oyun dk = 2.3 gerçek dk). Aşağıdaki
  dakikalar GERÇEK HIZDA ölçüldü; şimdi ~30'da biri. `HitStop` hızlandırılmış modda donmayı KAREYLE sayıyor (gerçek
  saatle sayınca hızlanma ×6'da kalıyordu — her isabet gerçek 0.05–0.1 sn yiyordu). Botun bütün beklemeleri ve süre sınırları OYUN saatiyle
  (`Şimdi` = `Time.time`) — yeni senaryoda `Time.realtimeSinceStartup` yazma (yalnızca bekçi ve nabız teşhisleri).
  ⚠️ Eski ölçümlerle karıştırma: eskiden bot oyunu ~3600 kare/sn görüyordu (insanüstü tepki), şimdi oyuncu gibi 60 —
  sayılar kayıyor, sıralama kalıyor. `-nographics` denendi: aynı hız (darboğaz çizim değil oyun hesabı).
- Faz 26 playtest'i: `Tek Can → Otopilot — Faz 26 playtest (izle)` ya da batch `…OtopilotMenu.Playtest26` (~15 dk) — todo'daki 7 maddeyi oynayıp ölçer, sonunda saldırı başına vurulma tablosu
- Faz 29 playtest'i: batch `…OtopilotMenu.Playtest29` (~5 dk) — silah sunağı, İPOTEK YAĞMURU, dash-atak HACİZ İHBARI, ÇİFT TAPU, İCRA, MUAFİYET/SABIR
- Faz 30 playtest'i: batch `…OtopilotMenu.Playtest30` (~15 dk) — 2 silahla başla, HACİZ sunakta, İPOTEK keşifte, KADRO, gerçek tuşlarla combo, İFLAS boss'tan. ⚠️ 1–3 tuşları artık ENVANTER YUVASI; bot silahı `Kuşan(silah)` ile açıp yuvaya koyuyor.
- Faz 31 playtest'i: batch `…OtopilotMenu.Playtest31` (~20 dk) — N ile 2. bölge: REHİN sunakta + yakala/fırlat/SİLİP SÜPÜR · 2. boss → ÇEK + SENET HACZİ · 3. bölgede keşifle KARŞILIK + karşılama/KARŞI İCRA. Bot `Diz(...)` ile silahları yuvalara diziyor.
- Faz 32 playtest'i: batch `…OtopilotMenu.Playtest32` (~35 dk) — N N, 3. boss → TAHVİL (basılı tut, VADE FARKI) · 4. bölgede MÜŞTEREK sunakta (bağ + yayılma) · 4. boss → ARBİTRAJ (görünmez/TERS İŞLEM arkadan, AÇIĞA SATIŞ) · 5. bölgede keşifle KİRA (kule, TAHLİYE, ORTAK GİDER). Bot isabeti olayın kendi sayacından okuyor (`ArkadanVuruş`, `MusterekBag.Yayılma`, `KiraKulesi.Atış`).
- Faz 33c playtest'i: batch `…OtopilotMenu.Playtest33c` (~1 dk) — 12 yeni combo gerçek tuşlarla (açanlar ve "+" hâlleri verilmiş): A ile adım → 2 → B ile bitir. Bot adım zincire yazılınca geçiyor (`PlayerController.AdımSayısı`), HACİZ'i ölü bölgenin dışındakine atıyor.
- Faz 42 playtest'i: batch `…OtopilotMenu.Playtest42` (~2 dk) — Parti 1'in 20 direkt combo'su gerçek tuşlarla ("+" verilmiş): ikililer 1. bölgede, üçlüler N ile 2. bölgede. `ComboDene` n adımlı (1 → 2 → 3) ve tetiklenen combo'yu KENDİ beklemesinden okur (`PlayerController.ComboBeklemesi`) — combo sayısı yetmez, zincirde önce başka ikili tetiklenebilir. HACİZ'li combo'da önce duruş (`HacizDuruşu`: bir düşmanın dibi, öbürü 4.2–6.8 yüzeyde); başarısız denemede adım adım döküm `[combo]`.
- ⚠️ N ile atlayan test GÜÇSÜZ girer (sunak alımları yok). Oda ve mini testleri `BeklenenGüç(saat, bölge × 5)` ile gerçek koşudaki güce çekiliyor (Faz 40) — yeni senaryoda atlamadan sonra çağır, yoksa test oyunu değil atlamayı ölçer.
- Mini boss'lar (Faz 34–38): ortak yordam `AdamlarıDöv(bölge, …)` — her `Adam` imzasını dövüşte ölçer ve **bot hiçbir mini'den saat kazanarak çıkmasın, hiçbirinde ölmesin** diye bakar (Enes 2026-10-21: "zorlasınlar"; 2026-10-22: "botun zorlanma ölçüsüne göre"); yeni bölgenin adamları için yalnızca çağır. Dövüşün dal dökümü logda `[<etiket>-DAL]`.
- Faz 34 playtest'i: batch `…OtopilotMenu.Playtest34` — TAHSİLDAR'ın 5 adamı M ile (beklenen güç, saat doyma tavanında): adı ekranda, imzası dövüşte görüldü mü (isabet isteyenler yalnız bot vurulduysa), öldü mü, süre · sonra F6 → 1. bölgenin 2–7. odaları: KALKANLI/MÜFLİS doğuyor, MÜFLİS patlıyor, bot KALKANLI'nın arkasına dolanıyor (`Dal("dolan")`), KISKAÇ/ÇİFT SIRA geliyor.
- Faz 35 playtest'i: batch `…OtopilotMenu.Playtest35` — EKSPER'in 5 adamı (N, M; beklenen güç, saat 2. bölgenin doyma tavanında): adı ekranda, imza (hasat kesintisi, değer kaybı, defter, %6 bedel), öldü mü, saat kaybı, saldırı sayımı · F6 → N → 2. bölgenin 2–8. odaları: VADELİ/TAKSİT doğuyor, her TAKSİT iki parçaya bölünüyor, KORİDOR/OK geliyor. Bot VADELİ kapalıyken açık olana dönüyor (`EnYakın`).
- Faz 36 playtest'i: batch `…OtopilotMenu.Playtest36` — KEFİL'in 5 adamı (N N, `AdamlarıDöv`: kefalet, teminat, dibinde iki kat aura, dash bedeli) · F6 → N N → 3. bölgenin 2–7., 10., 11. odaları: SÜLÜK yapışıyor ve bot dash'le silkeleniyor (`Dal("silkelen")`), MEVDUAT olgunlaşıyor, İÇ İÇE/SÜTUN geliyor.
- Faz 37 playtest'i: batch `…OtopilotMenu.Playtest37` — SİMSAR'ın 5 adamı (N N N, `AdamlarıDöv`: işaret sıklığı, kalkanın arkasına dolanma, durana iki kat aura, geri sıçrama; her dövüşün dal dökümü logda `[P37-DAL]`) · F6 → N N N → 4. bölgenin 2–7., 10–12. odaları: BLOKE botu yavaşlatıyor, CEZAİ ŞART zinciri uzuyor, DÖRT KÖŞE/ÇAPRAZ geliyor.
- Faz 38 playtest'i: batch `…OtopilotMenu.Playtest38` — MURABAHACI'nın 5 adamı (N×4, `AdamlarıDöv`: faiz, vade, bileşik faiz, muaccel) · F6 → N×4 → 5. bölgenin 2–7., 10–13. odaları: MÜHLET (kaçı süresinde kesildi), REFİNANS (öbürlerine yenilediği), SARMAL/ZİKZAK. `AdamlarıDöv` artık her mini için "öldürmedi" de istiyor (kayıp < giriş saati, Enes 2026-10-22: "botun zorlanma ölçüsü").
- Kapı testi (masa #14): batch `…OtopilotMenu.KapiTesti` (~10 sn oyun) — son düşmanı kapının üstünde gerçek vuruşla öldür → geçirmemeli; çık, yeniden bas → geçmeli.
- Fiyat ölçümü (Faz 33): batch `…OtopilotMenu.FiyatOlcum` (KONİ…REHİN + rakip sunaklar) · `…FiyatOlcum33b` (ÇEK…KİRA), hızlandırılmış ~2.5 dk — her yükseltme aynı iki odada VAR / YOK, saat her odada doyma tavanından; değer sn/oda, geri ödeme = fiyat ÷ değer (oda). Gürültüyü görmek için yükseltmesiz deneme bloğun başında ve sonunda; her odada zar sabit (`Random.InitState`) — çoğu silahta aynı deneme aynı sonucu veriyor. Bot dash-atak yapmıyor, "dip"e inmiyor, arkaya geçmiyor — o yükseltmeler ölçülemiyor.
- Boss süreleri: batch `…OtopilotMenu.BossTesti` (~2.5 dk) — 5 ana boss, her biri yeni koşuda, beklenen güçle, saat bölgenin doyma tavanında (`Clock.Tavan`); süre, faz 2 anı, saat kaybı / giriş, darbe. Kural (Enes 2026-10-20): **her boss öncekinden uzun** — boss/bölge dengesine dokunduktan sonra koş. Boss başına havuz payı `BossKind.HavuzPayı`. **Faz 41'den beri:** tavanla giren botu ÖLDÜRMEMELİ (kayıp < giriş) — ekonomi sıkı, bot sunakta tavanın altına harcamıyor, yani bu gerçek koşunun en kötü hâli. MURABAHACI sınıra yakın ve gürültülü: kırmızıysa bir kez daha koş, iki kez kırmızıysa gerçek.
- Boss teşhisi: batch `…OtopilotMenu.SimsarTeshis` (~30 dk) — P32'nin yolunda KEFİL (kontrol) + SİMSAR, 10 sn'de bir `[BOSS-NABIZ]`: havuz, inen hasar, savurma, darbe, F5 ve botun hangi dalda olduğu (vur / yaklaş / bekle / kaç·<saldırı> / işaret). "Boss ölmüyor" dendiğinde önce buna bak.
- Tam koşu ölçümü (Faz 27): `Tek Can → Otopilot — tam koşu ölçümü (izle)` ya da batch `…OtopilotMenu.TamKosu` (~9–11 dk) — bütün bölgeler atlamadan; oda/bölge/boss süreleri, sunak alımları, saatin bittiği yerler (F5 takviyesi). **8–10 saat hedefinin ölçüsü** (Faz 39): baştan sona KAZANILMALI, F5'siz; toplam süre ve oda ortalaması buradan.
- Ekonomi ayarı (Faz 28): batch `…OtopilotMenu.AraBolge1Olcum` (~10 dk, 1. ara bölge: 7 oda + mini + sunak) · `…Bolge1Olcum` (~55 dk, 1. bölge) · `…MiniTeshis` (M ile ilk mini, 10 sn'de bir `[NABIZ]`: oyun zamanı / saat akışı) · `…MiniTesti` (5 bölge × 5 mini boss, beklenen güçle; süre / saat kaybı / darbe, ~1–1.5 saat)
- Batch: `-executeMethod TekCan.EditorTools.OtopilotMenu.Batch` — **`-quit` YOK**, bot kendisi çıkar, ~12 dk
- Çıktı: `Logs/Otopilot/<zaman>/rapor.md` + görüntüler. Uzun sürdüğü için `AllChecks`'te değil.
- ⚠️ Batch'te pencere odakta olmuyor: bot girdi ayarının kopyasında `backgroundBehavior = IgnoreFocus` açıyor (yoksa tuşlar düşüyor).

Ayrı araçlar (kapı değil, rapor): `Denge simülasyonu` (hasat oranı taraması) ·
`Zorluk eğrisi raporu` (her bölge/oda için kazanç–masraf kırılımı + kaçınma pencereleri) ·
`Geri bildirim ekran görüntüsü al` · `Oda yerleşimlerini çek`.

⚠️ **Kayıt dosyası korumalı:** `SaveFile` yalnızca `Application.isPlaying` iken oyuncunun
dosyasına yazar; editör ve batch modda kendiliğinden geçici dosyaya gider. Yeni araç
yazarken bir şey hatırlaman gerekmiyor. (`PathOverride` yine de var — bilinen bir dosya
gerekiyorsa, örneğin `Faz9SelfCheck`'in diske yazıp geri okuma turu.)

Sebep: öz-denetimler ve raporlar checkpoint yazdırıyor. Koruma yokken `AllChecks`
çalıştırmak oyuncunun koşusunu siliyordu — 2026-08-26'da **canlı** bulundu.

## Tekrar eden tuzaklar — yeni kod yazmadan önce oku

Bu üç hata sınıfı projede defalarca çıktı. Yeni kod yazarken önce bunları sor:

1. **`Object.Destroy` ERTELENİR.** Yok edilmeye işaretlenen nesne kare SONUNA kadar yaşar
   ve `FindObjectsByType` onu bulmaya devam eder, `Update`'i bir tur daha çalışır.
   *"Var mı?"* diye sorup karar verme — sahip olman gerekeni **koşulsuz kur**, ya da
   `SetActive(false)` + `DestroyImmediate` ile hemen al. (Sessiz ses, atlanmış oda.)
2. **y yatay hesaba karışıyor.** Mesafe ölçerken `Vector2(x, z)` kullan, `magnitude` değil.
   (Havada doğan düşmanlar, kaçış mesafesi, çıkış kapısı.)
3. **Özellik değil ETKİ test et.** Zincirin son halkası testin içinde mi? Ses üretildi mi
   değil, DUYULUYOR mu. Efekt doğdu mu değil, hedefe VARDI mı. Zaman güdümlü sınıf
   yazıyorsan `dt`'yi dışarıdan al (`Enemy.ApplyAura`, `Arc.Step`, `Mote.Step`) —
   yoksa editör modunda `Time.deltaTime` 0 olduğu için etki ölçülemez.
4. **Test kısayolu KALICI DURUM YAZMAZ.** Yeni bir kısayol eklerken
   `SaveFile.MarkTestJump()` çağır — koşu kirlenir ve `Flush()` diske hiç yazmaz.
   `N` bunu yapmadığı için atladığı bölgeyi checkpoint'e yazıyordu ve Enes bir daha
   1. bölgeye **dönemedi** (2026-09-02, ders 12).
   *Ölçü:* KURALI ya da İLERLEMEYİ değiştiren kısayol kirletir (`N`, `B`, `F5`,
   `F2-F4`); yalnızca KUŞANIMI değiştiren kirletmez (1-3 yuva — checkpoint'e zaten
   yazılmıyor, kirletseydi kir sürekli açık kalır ve işe yaramazdı).
   Ve her kısayolun **geri alma yolu** olmalı, kısayolun kendisi kadar erişilebilir
   bir yerde: `F6` sıfırdan yeni koşu başlatır ve **ölüm ekranında da çalışır**.
   ⚠️ Bu kural klavye girdisiyle test edilemiyor — o yüzden burada yazıyor.
5. **Yok edilmiş OLABİLECEK Unity nesnesinde `?.` / `??` kullanma** (saldıran düşman, işaretin
   sahibi, eski odanın nesnesi). Yok edilmiş `UnityEngine.Object` C# tarafında null değil;
   `x?.Foo()` ölü nesneye yazar. Açık `if (x != null)` yaz. (`Clock.Reset` yorumu, ders 14.)
6. **Kapıyı yazınca bir kere BOZ.** Hatayı geri koy, kırmızıya dönüyor mu bak. 2026-09-25
   denetiminde bozuk dört mekaniğin dördünün de kapısı yeşildi (ders 14).

7. **Kare kare biriken sayı `double` olur.** Bot ~3600 kare/sn koşuyor; büyük `float` saate
   0.0003 sn eklemek/çıkarmak yuvarlanıp kayboluyor (saat 9000'de hiç akmadı, ders 16).
   Fizik dışı bir ölçüm (saat akmıyor) gördüğünde ayar değil hata ara.

## graphify

Kod yazıldıkça:

```bash
graphify update Assets/_Project/Scripts
```

⚠️ Proje **kökünden** çalıştırma — Unity `Library/` cache'ini (binlerce dosya) tarayıp timeout eder. Her zaman `Assets/_Project/Scripts` ver.

⚠️ Komut PATH'te değil: `"/c/Users/Enesin pc/AppData/Local/Programs/Python/Python312/Scripts/graphify.exe"`. Çıktı `Assets/_Project/Scripts/graphify-out/` (gitignore'da).

Mimari sorularından önce `Assets/_Project/Scripts/graphify-out/GRAPH_REPORT.md` oku; modüller arası "X ile Y nasıl ilişkili" sorularında grep yerine `graphify query` / `path` / `explain` kullan.

## Unity başsız çalıştırma

⚠️ **Gizli pencerede çalıştır** (Enes, 2026-10-12: "sürekli alttab atıyorsun" — her Unity açılışı odağı çalıyordu):
```bash
powershell -NoProfile -ExecutionPolicy Bypass -File Araclar/unity-gizli.ps1 -Method TekCan.EditorTools.AllChecks.Run -Log <log>
powershell -NoProfile -ExecutionPolicy Bypass -File Araclar/unity-gizli.ps1 -Method TekCan.EditorTools.OtopilotMenu.TamKosu -Log <log> -Otopilot
```
Kapıları mümkün olduğunca TEK açılışta topla (`AllChecks`). Bot testleri sessiz (`Otopilot.Awake`).

Düz komut (yalnızca betik çalışmazsa):

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.2f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath "C:\Users\Enesin pc\UnityProjects\Tek Can" -logFile <log>
```
