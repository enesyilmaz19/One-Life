# Dersler

## 2026-08-24 — Faz 1: değerlendirilemez bir faz teslim ettim

**Ne oldu:** Faz 1'in ilan edilmiş hedefi *"vurmak iyi hissettirsin"* idi. Oyuncunun saldırısı için **hiçbir görsel yok**, düşman saldırısı için **hiçbir görsel ve hazırlık hareketi yok**. Enes oynadı, saldırdığını göremedi, kendisine saldırıldığını göremedi — sadece saatin azaldığını gördü. Beş değerlendirme sorusundan üçü sorulamadı bile. **Fazın bütün varlık sebebi olan playtest kapısı çalışmadı.**

### Kök sebep 1 — Kolay olanı doğruladım, teslim edileni değil

Hasat matematiğini 10 assert ile doğrulayıp **"10/10 GEÇTİ"** diye rapor ettim. Ama fazın hedefi matematik değildi, **histi**. Yanlış şeyin üstünde yeşil yanan testler bana sahte güven verdi ve fazı hazır ilan ettirdi.

Bu tam olarak Enes'in daha önce kayıt altına aldırdığı hata: *"yeşil testler 'faz tamam' demek değil."* Kuralı biliyordum, dosyaya kendi elimle yazmıştım, yine de yaptım.

### Kök sebep 2 — Engelleri isimlendirip yine de teslim ettim

Raporun altına "dürüst eksikler" diye **ses yok · telegraf yok · gerçek VFX yok** yazdım. Yani biliyordum. Ama bunları *uyarı notu* olarak dosyaladım, oysa doğru okuma şuydu: **saldırı görseli olmadan his kapısı fiziksel olarak çalıştırılamaz.** Bir engeli dipnotta dürüstçe belirtmek, o engel üzerine karar vermenin yerine geçmez. Dürüstlük muhakemenin yerini tutmaz.

### Kök sebep 3 — Greybox "görünmez" demek değil

"Çalışan en küçük şey" ilkesini görsellere de uyguladım. Ama teslim edilen şeyin *kendisi* görsel geri bildirimken, minimum hâlâ çok azdı. Ponytail merdiveni **çözümü** kısaltır, **hedefi** değil. Yanlış yerden kestim.

---

## 2026-08-24 (2) — Testler geçiyordu, oyun çalışmıyordu

**Ne oldu:** "Hasar inmiyor" şikâyetini üç turda çözebildim. İki kez yanlış kök sebep önerdim (önce hit tespiti, sonra ölü `Clock.Instance`). Gerçek sebep: düşmanlar **havada doğuyordu**.

```csharp
new Vector3(Mathf.Cos(a), 1f, Mathf.Sin(a)) * (arenaRadius * 0.55f)
```

Çarpan y'yi de çarpıyordu → düşmanlar y=7.7'de. `ChaseMove` yalnızca yatay hareket ettiği için hiç inmiyorlardı. İsabet hesabı y'yi düzlediği için **matematik doğruydu**; ama izometrikte havadaki nesne ekranda kayık görünür, oyuncu gördüğü yere nişan alıp ıskalıyordu.

**Neden testler yakalayamadı:** testlerim kendi dünyalarını kuruyordu — oyuncu y=0'da, düşman tam önde, ideal koordinatlar. Oyunun gerçek yerleşimini hiç kullanmıyorlardı. Yani test yeşildi ama **test ettiği dünya oyunun dünyası değildi.**

**Neden sonunda bulundu:** öz-inceleme "benzer fonksiyonları birleştirmeli misin?" diye sordu. Spawn geometrisi `Bootstrap` ve `Faz1Repro`'da kopyalanmıştı. Tek kaynağa indirip mesafeyi loglayınca sayı yanlış çıktı: 7.70 yerine **10.89**. Hata kendini orada ele verdi.

### KURAL (2)

> **Test düzeneği, üretim yerleşimini KOPYALAMAZ; ondan İTHAL EDER.**

Bir test koordinat, ölçek, spawn konumu gibi şeyleri kendi elleriyle kuruyorsa, ürünle sessizce ayrışır ve yanlış güven verir. Ortak kaynak (`Bootstrap.EnemySpawnPosition` gibi) hem tekrarı kaldırır hem sapmayı imkânsızlaştırır.

**İkinci kural:** iki kez üst üste yanlış kök sebep önerdiysem, üçüncü tahmini yapmam — **reprodüksiyon yazarım.** `Faz1Repro` gerçek geometriyi kurup gerçek yolu çağırdı ve cevabı tek çalıştırmada verdi.

---

## KURAL

> **Bir fazı playtest'e hazır ilan etmeden önce sor: oyuncu, yargılamasını istediğim şeyi algılayabiliyor mu?**

"Vuruş iyi hissettiriyor mu?" diye soracaksan **görünür bir vuruş olmak zorunda.** Fazın hedefini doğrula, hedefin kolay alt kümesini değil.

**Somut sonucu:** dövüş fazlarında **ilk yazılacak şey geri bildirimdir** — savurma yayı, düşman hazırlık hareketi, isabet tepkisi. Sistemler taslak kalabilir; geri bildirim kalamaz, çünkü değerlendirilen ürün geri bildirimin kendisidir.

---

## 2026-08-24 (3) — Soruyu kendi kelimelerimle sordum

**Ne oldu:** Faz 1 his kapısında dört soru sordum. Enes ikisini cevapladı, ikisine *"neyi test etmemi istiyorsun anlamadım"* ve *"bunu nasıl test edeceğim"* dedi.

Sorular şunlardı: *"hit-stop + sarsıntı dozu az mı çok mu?"* ve *"hazırlık süresi 0.55 dodge için yeterli mi?"* — ikisi de **benim sözlüğümde** yazılmış. "Hit-stop" bir geliştirici terimi; oyuncu onu bilmez ve zaten **bilinçli olarak algılayamaz** (10 ms donma). Tek başına yargılanamayacak bir şeyi tek başına yargılamasını istemişim.

**Kural:** Bir değeri onaylatmak istiyorsam ya **ne olduğunu günlük dille anlatırım**, ya da **karşılaştırma sunarım** (uç değerleri arka arkaya denet). Ve testin kendisini adım adım yazarım: *"şunu yap, şuna bak, şu olursa şu demektir."*

Bu, `feedback_verify_the_phase_goal` kuralının kardeşi: orada *algılayabiliyor mu* diye sormuştum, burada eksik olan *anlayabiliyor mu*.

---

## 2026-08-24 (4) — Bir silah teslim ettim, hiç vuramıyordu

**Ne oldu:** Faz 3'te DURGUN silahını yazdım. Hasat çarpanı, menzili, tek hedefliliği,
saat dondurma davranışı hepsi vardı ve 13/13 öz-denetim yeşildi. Ama silah **hiç hasar
veremiyordu**: basılı tutmayı yakalayan blok erken `return` ediyordu, yani saldırı girdisi
hiç okunmuyordu, bırakınca da bir şey olmuyordu.

Simülatör ona "düşman başına +4.8 saniye" dedi. Tamamen hayaldi — sıfır hasar veren bir
silahın kazanç hesabı yapılmıştı.

**Neden testler yakalamadı:** on üç kontrolün hepsi silahın *özelliklerini* doğruluyordu —
çarpanı doğru mu, tek hedefli mi, dondurur mu, bedeli ne zaman keser. Hiçbiri en temel
soruyu sormamıştı: **"bu silah bir düşmana hasar verebiliyor mu?"**

**KURAL (4)**

> **Bir şeyin özelliklerini test etmek, o şeyin işe yaradığını test etmek değildir.**

Her yeni oynanış nesnesi için ilk assert en aptalca olan olmalı: *çalışıyor mu?* Silah
vuruyor mu, düşman ölüyor mu, kapı açılıyor mu. Detay kontrolleri ondan sonra gelir.

Faz 3'e eklenen kontrol tam olarak bu: **bütün silahlar üzerinde döngü kurup her birinin
gerçekten hasar verdiğini** doğruluyor. Yeni silah eklendiğinde otomatik kapsanıyor.

Bu, `feedback_verify_the_phase_goal` ailesinin üçüncü üyesi:
① algılayabiliyor mu · ② anlayabiliyor mu · ③ **hiç çalışıyor mu**

---

## 2026-08-24 (5) — Aynı ayrışma ikinci kez oldu

**Ne oldu:** Faz 4'te dalga sistemi yazarken `WaveManager` kendi doğum konumu formülünü
yazdı — yarıçap 0.62, açı böleni dalga sayısı. `Bootstrap.EnemySpawnPosition` ise 0.55 ve
`enemyCount` kullanıyordu. Yani **oyun artık testlerin sınadığı yoldan geçmiyordu.**

`Faz1Repro`'nun bütün varlık sebebi "gerçek doğum geometrisini sına"maktı. O geometri
WaveManager'a taşındı, repro ölü bir yolu doğrulamaya devam etti ve yeşil yandı.

**Bu, kural (2)'nin aynısı** — daha önce "test düzeneği yerleşimi ithal eder" diye yazmıştım
ve o kuralı uygulamıştım. Ama kuralı yalnızca **testin tarafına** uygulamışım: test üretimden
okuyordu. Yeni üretim kodu geldiğinde o üretim kodu **başka bir yerden** okumaya başladı ve
tek kaynak sessizce ikiye bölündü.

### KURAL (5)

> **Tek kaynak, yalnızca testin ona bakmasıyla korunmaz — yeni üretim kodunun da ona
> bakması gerekir.**

Yeni bir sistem, mevcut bir formülü (konum, ölçek, zamanlama) yeniden yazıyorsa ayrışma
başlamıştır. Doğru refleks: yazmadan önce "bunun bir kaynağı zaten var mı" diye sormak.

**Somut savunma:** repro artık düşmanları elle doğurmuyor, **oyunun kendi yolundan** geçiyor
(`WaveManager.StartNextWave` → `Bootstrap.SpawnEnemyAt`). Üretim kodu nereye kayarsa repro
oraya kayar; bir daha ölü yol sınayamaz.

---

## 2026-08-24 (6) — "Özellik değil etki" kuralını yine yarım uyguladım

**Ne oldu:** Faz 5'te DURGUN dersini hatırlayıp her türün *hasar alabildiğini* test ettim.
Ama SIZINTI için yalnızca `AuraDrain(t) > 0` yazdım — yani **özelliği** kontrol ettim,
saatin gerçekten indiğini değil. KAÇAK'ta da "kaçmaya başladı"yı kontrol ettim, "kaçtı ve
zamanını götürdü"yü değil.

Öz-inceleme sırasında etki testlerini yazınca ikinci bir şey ortaya çıktı: **etki testi
yazılamıyordu**, çünkü `Time.deltaTime` editör modunda 0. Yani aura kodu hiç ölçülemiyordu
ve ben bunu fark etmeden "özellik testi" ile yetinmiştim.

**Kural (4)'ün eksik yarısı:**

> Etki testi yazılamıyorsa, sorun testte değil **tasarımdadır**. Ölçülebilir hale getir.

`ApplyAura(float dt)` olarak ayrıldı: `Update` `Time.deltaTime` ile çağırıyor, öz-denetim
1 saniyelik adımla çağırıyor. Artık hem boss hem SIZINTI için "saat GERÇEKTEN indi mi"
sorulabiliyor.

**Aynı turda yakalanan bug:** kaçış mesafesi `transform.position.magnitude` ile ölçülüyordu
— y'yi de katıyor ve düşmanlar y=1'de. Bu projede y'nin yatay hesaba karışması **üçüncü**
kez çıktı (havada doğan düşmanlar, sonra bu). Yatay mesafe için ayrı bir yardımcı kullanılıyor
artık.


---

## 2026-08-25 (7) — Sesin verisini test ettim, duyulup duyulmadığını değil

**Ne oldu:** Faz 6'da 9 ses koddan üretildi, 37/37 kontrol yeşil yandı, "Faz 6 bitti" dedim.
Sahnede **hiç `AudioListener` yoktu.** Yani oyunda tek bir ses çıkmıyordu.

Sebep: `Bootstrap.Build` sahnedeki bütün kameraları siliyor. Şablonun kamerası dinleyiciyi
taşıyordu, o da gitti. Yerine kurulan kamerada yalnızca `Camera` + `IsoCamera` var.

Testler neden görmedi: hepsi `AudioClip`'in **içine** bakıyordu — üretildi mi, sessiz mi,
kırpılmış mı, süresi makul mü. Hiçbiri "bu ses oyuncunun kulağına ulaşıyor mu" diye sormadı.
Ses yolunun son halkası test edilen bölgenin dışındaydı.

**Bu, kural (4)'ün üçüncü tekrarı.** DURGUN'da, SIZINTI'da, şimdi seste. Ortak kalıp:
*üretilen şeyi* ölçüyorum, *ulaştığı yeri* değil. Zincirin son halkası hep testin dışında
kalıyor.

**Kuralın genişlemiş hali:**

> Bir sistemin testi, o sistemin **çıktısında** değil, çıktının **vardığı yerde** biter.
> Ses için: kulak. Hasar için: düşmanın zamanı. Aura için: saatin sayısı.
> "Zincirin son halkası testin içinde mi?" — teslimden önceki son soru.

**Somut savunma:** dinleyiciyi artık ses sisteminin kendisi garantiliyor (`Sfx.EnsureListener`),
kamerayı kuran kodun hatırlamasına bırakılmadı. Sesin duyulabilir olması ses sisteminin işi.
Faz 6 denetiminin **ilk** kontrolü de bu — diğer 38'i geçse bile bu düşerse ses yok demektir.

**Aynı turda yakalanan ikinci bug:** tek `AudioSource` kullanılıyordu ve `pitch` o kaynakta
**çalmakta olan** sesleri de büküyor. Kalabalıkta her yeni vuruş, hâlâ duyulan öncekini
perdeden kaydırıyordu. 6 kanallı havuza geçildi.

**Devam (aynı gün, ikinci tur):** Yukarıdaki düzeltmeyi yaptım, 39/39 yeşil yandı,
Enes yine "ses gelmiyor" dedi. Gerçek sebep daha derindeydi:

> **`Object.Destroy` ERTELENİR.** Yok edilmeye işaretlenen nesne kare SONUNA kadar
> yaşar ve `FindFirstObjectByType` onu bulmaya devam eder.

Bootstrap şablonun Main Camera'sını (dinleyiciyi taşıyan) siliyor. `EnsureListener`
aynı karede "dinleyici var" diye buluyor ve hiçbir şey eklemiyordu. Kare bitince o
dinleyici de ölüyordu → sıfır dinleyici → sessizlik.

Denetimim boş batch sahnesinde çalıştığı için tuzağı hiç kurmamıştı: **kolay durumu
ölçtüm, oyunun durumunu değil.** Bu, "test düzeneği üretim koşullarını kurmalı"
kuralının dördüncü tekrarı.

**Kural:** Unity'de "var mı?" diye SORUP karar verme — ertelenmiş yok etme yüzünden
cevap yalan olabilir. Sahip olman gereken şeyi **koşulsuz KUR.** `EnsureListener`
artık hiç sormuyor: ölüleri hemen alıyor, yenisini kendi nesnesine takıyor.

**Denetim düzeltmesi:** Faz 6'da artık sahnede bir "yem dinleyici" bırakılıyor. Eski
kod bu yemi yutup geçerdi; yeni kod kendi nesnesine kurmak zorunda. Yani test artık
tam olarak bu hatayı yakalıyor.

**Kabul edilen sınır:** play mode otomasyondan sürülemedi (batchmode `EnterPlaymode`
takılıyor). Bunun yerine `Sfx.Init` her koşuda Console'a dinleyici durumunu basıyor —
bir daha sessizlik olursa "ses gelmiyor" değil, Console satırı geliyor.

---

## 2026-08-25 (8) — Efekti test ettim sandım, yine doğuşu test etmişim

**Ne oldu:** Faz 7'de VFX yazdım ve öz-denetimi "zerre doğdu mu / halka doğdu mu" diye
kurdum. 10/10 yeşil yandı. Öz-incelemede fark ettim: **hiçbiri hareketi ölçmüyordu.**

Ölçülmeyenler: zerre hedefe varıyor mu · yay gerçekten büyüyor mu · ömrü bitince
kendini yok ediyor mu. Sonuncusu ciddi — yok etmezse sahnede birikirler ve "doğdu mu"
testi bunu asla göremez.

**Neden yine oldu:** `LateUpdate` editör modunda çalışmıyor, `Time.unscaledDeltaTime` 0.
Yani hareket ölçülemiyordu. Kayıt (6)'da bu tam olarak yaşanmış ve çözümü yazılmıştı:

> Etki testi yazılamıyorsa, sorun testte değil tasarımdadır. Ölçülebilir hale getir.

`Enemy.ApplyAura(float dt)` bu dersin ürünüydü. Ama yeni sınıf yazarken **kendi
kod tabanımdaki çözümü uygulamadım** — dersi biliyordum, deseni kullanmadım.

**Kural (yeni yarı):**

> Yeni bir zaman güdümlü sınıf yazarken, ilk soru: *bu kod tabanında bu problem
> daha önce çözüldü mü?* Ders hatırlamak yetmiyor, **deseni aramak** gerekiyor.

`Arc.Step(dt)` ve `Mote.Step(dt)` ayrıldı; `LateUpdate` yalnızca dt'yi geçiriyor.
Denetim 10 → 14, eklenen dördü de hareket ve yok olma.

**Aynı turda yakalanan ikinci tutarsızlık:** `Enemy` yok etmeyi
`Application.isPlaying ? Destroy : DestroyImmediate` diye ayırıyordu, `Arc`/`Mote`
ayırmıyordu. Editör modunda sürülünce `Destroy` hata basar — testi imkânsız kılan
şeylerden biri de buydu. İkisi de `Enemy`'nin desenine getirildi.

---

## 2026-08-26 (9) — Kök nedenin YARISINI düzeltip "çözüldü" dedim

**Ne oldu:** Enes "bossa vururken bazen buglanıyor, vuramıyorum" dedi. Menzilin
düşmanın MERKEZİNE ölçüldüğünü buldum, YÜZEYE çevirdim, kapattım.

Şikâyet geri geldi. Çünkü isabet testinde İKİ ölçüm var ve ben birini düzeltmiştim:

```csharp
surfaceDist = to.magnitude - e.Radius;                        // YÜZEYE  ✅ düzeltmiştim
if (surfaceDist > range) return false;
return Vector3.Angle(transform.forward, to) <= halfAngle;     // MERKEZE ❌ dokunmamıştım
```

Boss'un yarıçapı 0.855. Yakınken gövdesi yayın içinde apaçık duruyor ama merkezi
yaydan çıkabiliyor. Reprodüksiyon ölçtü: **1.2 birim mesafede 41 DERECELİK ölü bant.**
Hepsi tam 50°'de başlıyordu — yani koninin yarım açısında.

**En kötü tarafı:** yay da aynı açıyla çizildiği için **görsel yalan söylüyordu.**
Gövdeyi yayın içinde görüyor, hiçbir şey alamıyordun. "Bug" hissinin kaynağı buydu.

**Kural:**

> Aynı kavramın (burada "düşmanın yeri") birden çok ölçümü varsa, düzeltmeyi
> HEPSİNE uygula. Birini düzeltip diğerini bırakmak hatayı yok etmez, **daha nadir**
> yapar — ve nadir hata "bazen buglanıyor" diye geri gelir, teşhisi daha zor olur.
>
> Teslimden önceki soru: *"bu kavram başka nerede ölçülüyor?"*

**Somut savunma:** `BossHitRepro` yazıldı ve `AllChecks`'e bağlandı. Boss'u beş mesafeye
koyup oyuncuyu 0-90° döndürüyor, 455 durumu sınıyor. Ölçüm **BAĞIMSIZ**: üretimin
formülünü tekrar çalıştırmıyor, gövde çemberinden nokta örnekliyor. Aynı formülü iki
yere yazsaydık aynı hatayı iki kez yapar ve test yeşil yanardı.

**Çift yönlü:** hem "örtüşüyorsa vurmalı" hem "açıkça dışarıdaysa vurmamalı" sınanıyor.
Yalnızca ilkini yazsaydım `return true;` yazmak testi geçerdi.

---

## 2026-08-26 (10) — Takip eden görselin açısı sessizce siliniyordu

**Ne oldu:** Son boss'a "güvenli açıklık" saldırısı ekledim: halka geliyor ama boss'un
ARKASINDA güvenli bir dilim var. İsabet testi doğruydu. Yeşil dilimi de 180° çevirip
doğurdum. **Ama görsel yanlış yeri gösteriyordu.**

`Arc` takip modunda her karede şunu yapıyor:

```csharp
transform.SetPositionAndRotation(_follow.position, _follow.rotation);
```

Yani doğuşta verdiğim 180° sapma **bir kare sonra siliniyordu.** Yeşil dilim boss'un
ÖNÜNE, yani tam da oyuncunun durduğu tarafa dönüyordu. Oyuncuya *"buradasın,
güvendesin"* deyip vuracaktı.

**Bu, bu projedeki en pahalı hata türünün üçüncü tekrarı:**
① yay açıyı merkeze ölçüyordu, gövde içerideyken vuruş düşüyordu
② ses üretiliyordu ama dinleyici yoktu
③ şimdi bu

Hepsinde **mekanik doğru, görsel yalan.** Ve hepsinde testler yeşildi çünkü mekaniği
ölçüyorlardı, GÖRSELİ değil.

**Kural:**

> Görsel bir işaret mekaniğin yerini gösteriyorsa, **testin ikisini de ölçmesi** gerekir.
> "Isabet doğru mu" yetmiyor; "gösterilen yer isabet edilen yer mi" diye sorulmalı.

**Somut savunma:** `Arc.Spawn(followYaw:)` eklendi ve öz-denetim yeşil dilimin ileri
yönünü boss'unkiyle karşılaştırıyor (`Dot < -0.5` = gerçekten arkaya bakıyor).
Takip bir kare işletilip ÖYLE ölçülüyor — doğuş anına bakmak tam da kaçırılan şeydi.

---

## 2026-09-01 (11) — Türü ekledim, testleri yazdım, ama oyuna hiç girmemişti

**Ne oldu:** Faz 13'te üç düşman türü eklendi. `EnemyKind.All` yedi elemanlı oldu,
her tür için ETKİ testi yazıldı (SİGORTA gerçekten hasadı kısıyor mu, MÜBAŞİR
gerçekten tebligat bırakıyor mu, ZAMANAŞIMI gerçekten büyüyor mu). Hepsi yeşil.
Faz kapandı, commit atıldı.

**Yedinci tür oyunda hiç doğmuyordu.**

**Neden testler görmedi:** Testler türü `Make(...)` ile ELLE kuruyordu. Yani
"bu tür doğru çalışıyor mu" sorusunu cevaplıyorlardı — ama "bu tür oyunda
ORTAYA ÇIKIYOR MU" sorusunu kimse sormamıştı. Doğru cevap, sorulmamış soru.

**İki ayrı kapı kesiyordu:**
1. `Region.Seep.UnlockedKinds` 6'da kalmıştı — tür eklerken güncellenmedi.
2. Onu düzelttim, hâlâ 6 tür çıktı. Asıl kök neden: `PickKind`'ın oda merdiveni
   her bölgede 2'ye sıfırlanıyordu; `2 + (oda - 2)` ve `roomsPerRegion = 6`
   olunca üst sınır 6'ydı. **Bölge tavanı 7 olsa bile erişilemezdi.**

⚠️ Birinci düzeltmeden sonra "tamam" deyip geçseydim ders 9'u (kök nedenin
yarısı) aynen tekrarlamış olurdum. Testi ÇALIŞTIRDIĞIM için ikincisi çıktı.

**Kural:**

> Bir içerik listesine (tür, silah, bölge, boss) eleman eklerken sor:
> **onu oyuna sokan SEÇİCİ bu elemana ulaşabiliyor mu?**
> "Tanımlı" ≠ "doğuyor". Listenin uzunluğuna bakmak yetmez — seçiciyi
> ÇALIŞTIRIP çıkan kümeyi say.

**Somut savunma:** `Faz13SelfCheck` artık `PickKind`'ı beş bölge × altı oda ×
on iki yuva çalıştırıp çıkan türleri `HashSet`'te topluyor; küme `EnemyKind.All`
kadar değilse eksik türün ADINI bastırıyor. Sayıya değil, çıktıya bakıyor.

---

## 2026-09-02 (12) — Test kısayolu kalıcı kaydı bozdu, geri dönüş yolu yoktu

**Ne oldu:** Enes bildirdi: *"bölgeyi başa saramıyorum, restart atınca 5. bölgeye
geçiyor."*

`N` (bölge atla) test kısayolu, atladığı bölgenin **1. odasına** giriyor — ve
`IsCheckpointRoom` oda 1 için doğru. Yani her test atlayışı **diske "5. bölgedesin"
yazıyordu.** Enes bölgeleri gezmeye gitti, bir daha 1. bölgeye dönemedi.

Beter eden şey: **geri dönüş yolu hiç yoktu.** `R` yalnızca ölünce çalışıyor ve
checkpoint'e döndürüyor — yani yine 5. bölgeye. Sıfırdan başlatan tek yol eşyaların
bitmesiydi.

**Kural:**

> **Test kısayolu KALICI durum yazmamalı.** Kısayol geçicidir, bıraktığı iz kalıcı
> olursa oyuncunun/testçinin kaydını sessizce bozar.
>
> Ve her kısayolun **geri alma yolu** olmalı — kısayolun kendisi kadar erişilebilir
> bir yerde.

**Somut savunma:**
- `SaveFile.MarkTestJump()` → koşu kirlenir, `Flush()` diske hiç yazmaz.
  ⚠️ Koruma `Flush`'ta, `SaveCheckpoint`'te DEĞİL: ölüm kaydı, eşya harcaması, oyun
  sonu — hepsi oradan geçiyor. Tek çağıranı korumak, bir sonraki yazma yolunda aynı
  hatayı üretirdi. (`PathOverride` dersinin aynısı: **kural kaynağa konur.**)
- **F6 = sıfırdan yeni koşu**, ve **ölüm ekranında da çalışıyor** — ilk yazışımda
  ölüm kontrolünün ARDINA koymuştum, yani çakılmanın tam şeklinde (kayıt 5. bölgede,
  ölüyorsun, R seni yine oraya atıyor) kullanılamıyordu. **Çıkış yolunu
  kullanılamadığı yere koymak, çıkış yolu olmamasıyla aynı şey.**
- Reprodüksiyon `Faz9SelfCheck`'te ve **bayrağı değil DİSKTE NE YAZDIĞINI** ölçüyor:
  yaz, geri oku, bölge hâlâ 0 mı.

---

## Ders 13 — Mekanik VAR ama ULAŞILAMIYOR (2026-09-11)

**Ne oldu:** Enes bildirdi: *"aga bosslarda uzakta olduğunda menzilli saldıracak,
yakında ise yakın saldırılarını yapacak mekaniği koymamışsın. sırayla hamlelerini
yapıyor gibi."*

Koda baktım ve mesafe filtresi **vardı**: `BossAttack.MinPlayerRange` /
`MaxPlayerRange`, üstelik uzun bir yorumla, Enes'in daha önceki bir şikâyetinden
doğmuş. "Filtre var, demek ki birkaç saldırıya eklemem yeter" diye düşündüm.

Asıl sebep bir üst katmandaydı: boss saldırıya **ancak `AttackRange` içine girince**
giriyordu (`TickState` → `Chase`). Yani her dövüşte önce yakın dövüşe kadar
**yürüyor**, saldırıyı orada başlatıyordu. *"Uzaktayken"* diye bir durum hiç
oluşmuyordu. Menzili 3.4 olan ışın, oyuncunun burnunun dibinden savruluyordu.

Mesafeye göre seçim yazmak **tek başına hiçbir şeyi değiştirmezdi** — seçim hep
aynı mesafede yapılıyordu.

**Kural:**

> Bir mekanik "yok" diye bildirildiğinde, önce **var mı** diye bakma —
> **çalıştırılabiliyor mu** diye bak. Koşulun kendisi doğru olabilir ama o koşula
> hiç GELİNMİYOR olabilir. Girdiyi üreten katmana kadar geri git.

**Somut savunma:**
- Kapı ÖZELLİK değil ETKİ ölçüyor: *"`MinPlayerRange` alanı var mı"* diye sorsaydık
  test **eski kodda da yeşil yanardı** ve Enes'in gördüğü hata yerinde dururdu.
  Soru şu oldu: boss **uzaktayken** hangi saldırıyı **seçiyor**.
- ⚠️ Ve o kapıyı ilk yazışımda kendi uydurduğum bir mesafeyle (4× menzil) kurdum;
  oraya **hiçbir saldırı erişemiyordu**, yani test üretimi değil kendi varsayımını
  ölçüyordu. Mesafe artık üretimin kendi `AttackTriggerRange`'inden alınıyor.
  *(Ders 10'un aynısı: kapı üretimi İÇERİ ALIR, yeniden türetmez.)*
- İkinci kapı: **dağarcıktaki her saldırı yüzeye çıkıyor mu.** Enes'in ikinci
  şikâyeti (*"faz 2'deki yeni özgün saldırıların hiçbirini yapmamışsın"*) aynı kökün
  öbür yüzüydü — ölü saldırı, olmayan saldırıdır.

### Yan ders — ölçünün içindeki KAÇAMAK

Aynı raporda: *"hiçbir bossta birbirinin aynısı iki saldırı olmayacak."* Bu kural
zaten kapıdaydı ve **yeşildi.** Çünkü imzaya `menzil={RangeMul:0.0}` yazmıştım:
menzili 3.0 yerine 3.2 yapmak iki özdeş saldırıyı "farklı" gösteriyordu.

> Bir kuralı ölçerken imzaya **oyuncunun hissetmediği** bir alan koyma. O alan
> kuralın kaçamağı olur ve kapı kendi kendini kandırır.

Yerine oyuncunun gerçekten hissettiği şeyler kondu: alanın açısı, güvenli yerin
genişliği, hangi mesafede sorulduğu.

---

## Ders 14 — Dört kapı yeşildi, dört mekanik bozuktu (2026-09-25, denetim)

**Ne oldu:** İki ajan kodu baştan sona taradı, ~30 hata buldu. Asıl ders sayıda değil:
bozuk dört mekaniğin **dördünün de kapısı vardı ve yeşildi.**

| Mekanik | Kapı neyi ölçüyordu | Neyi ölçmeliydi |
|---|---|---|
| Boss zinciri (faz 3'ün 4 halkası) | Zincirin boyunu **veriden** saydı: 4 | Kaç halkanın **oynadığını**: 2 |
| Geri sıçrayan saldırı (DEVİR) | Boss **uzaklaştı mı** | Saldırı **vurdu mu** — hiç vurmuyordu |
| HACİZ dash-atağı | `DashStrike`'ı `_dashDir` **sıfırken** çağırdı | `StartDash`'in kurduğu yönle — boşluğa vuruyordu |
| Son boss'un uyku rengi | Testin **kendi hesapladığı** iki rengi kıyasladı | Renderer'ın **gerçekte** ne gösterdiği |

Hepsi ders 3'ün ("özellik değil etki") aynısı. Kural biliniyordu. Uygulanmadı, çünkü her
kapı yazıldığı gün **gerçek bir soruyu** cevaplıyordu. Soru sonra kaydı ama kapı kaymadı.

**Kural:**

> Kapı yazdıktan sonra **bir kere bilerek boz** — hatayı geri koy ve kırmızıya döndüğünü
> gör. Dönmüyorsa kapı üretimi değil kendi varsayımını ölçüyor.

**Somut savunma:** `Faz24SelfCheck` en ağır düzeltmelerin etkisini ölçüyor. Altı hata bilerek
geri kondu, altı kontrol kırmızıya döndü, başka hiçbir şey düşmedi.
⚠️ **Hâlâ açık:** uyku RENGİ gerçek renderer'dan ölçülmüyor (Faz 18'in kontrolü boş,
Faz 24 yalnızca hazırlığın bırakıldığını ölçüyor). Playtest maddesi 10 bunu gözle soruyor.

### Yan dersler
- **Unity null'ı ve `?.`**: yok edilmiş bir `UnityEngine.Object` C# tarafında null DEĞİL.
  `saldıran?.HesabınaYaz(...)` yok edilmiş boss'a yazardı. Unity nesnesinde açık
  `if (x != null)` kullan (`Clock.Reset` yorumundaki tuzağın aynısı).
- **Yorum ile kod ayrıştı**: `TearDown` "bekleyen İPOTEK oda ile birlikte yok oluyor"
  diyordu, ipotek odaya değil koşu köküne bağlıydı. `PlayerController`, düşmanların
  arenaya sığdırıldığını söylüyordu, MÜBAŞİR sığdırılmıyordu. **Yorum bir iddiadır,
  kanıt değil.**
- **Düzeltme bir testi kırdıysa önce testi sorgula**: Faz 4 aura kontrolü kazanılmış
  saati tekrar kullanıyordu. "Kazanılmış koşu zaman kaybetmez" düzeltmesiyle kırmızıya
  döndü. Kırmızı haklıydı ama sebebi testin kurgusuydu, üretim değil.
- **Rastgele sıra + gövde saymak = titrek kapı**: Faz 18 "aynı anda tek rakip"i gövdeyle
  sayıyordu. Rakip sırası rastgele. KEFİL ilk gelince bölünüyor ve 4'te 1 ihtimalle kırmızı
  yanıyordu. Önceki koşularda şansla yeşildi. Kuralın dilindeki birimi say (burada: boss).
- **Düzeltmenin kendisi delik açabilir**: tamir bedelini checkpoint'ten düşmek sonsuz
  dirilişi kapattı, ama checkpoint bedelden küçükse 0.5 sn'lik ölüm döngüsü açtı. Güvenlik
  incelemesi yakaladı. Her "bedeli şuradan düş" düzeltmesinde sor: *o hesap sıfıra inerse?*
- **`.gitignore` satır sonu yorumu desteklemez**: `#` yalnızca satır başında yorum.
  `/*.log   # açıklama` yazınca desen bozulur. Yorum ayrı satıra.

## Ders 15 — Dağarcık 97, oyuncunun gördüğü 2 (2026-09-30, birleşik playtest)

**Ne oldu:** Enes *"boss saldırılarını çeşitlendirmeni istemiştim, yapılmamış"* dedi.
Veride yapılmıştı: 2026-09-11'de dağarcık 69 → 97. Ama MURABAHACI faz 1'de Enes **2
saldırı** gördü. Sebep seçim kodu: boss en uzun erişimli saldırısının menziline girdiğin
an saldırıyor, sonra yalnızca o mesafeden erişenleri seçiyor. Uzakta duran oyuncuya 7
saldırının 2'si ulaşıyor. Kapı dağarcığın BOYUNU saydı, oynananı değil.

Ders 13 ("mekanik var ama ulaşılamıyor") ve ders 14'ün ("veriyi saydı, oynananı değil")
aynısı, üçüncü kez. Oyuncu için veride duran ama önüne gelmeyen saldırı **yapılmamış**
saldırıdır. Enes'in cümlesi doğruydu.

**Kural:**
> İçerik eklerken kapı **oyuncunun davranışıyla** ölçsün: gerçekçi mesafelerde duran bir
> simüle oyuncu her saldırıyı en az bir kez görüyor mu? "Listede var" ölçü değil.

### Yan ders
- **Yorum yine koddan ayrışmıştı:** `Enemy.TakeDamage` "ZIRH BURADA" diyor, altında zırh
  yok. ZIRHLI yalnızca havuzu ×2. Oyuncu ZIRHLI ile İRİ'yi ayırt edemedi, çünkü ikisi de
  "daha çok can". *Yorum bir iddiadır* (ders 14) — rütbeyi ölçen bir kapı olsaydı yakalardı.
- **"Kendi renginde" dedi, sayılar aynıydı:** oyuncunun İPOTEK işareti ile SİMSAR'ın işareti
  (1, 0.45, 0.95) ve (0.9, 0.55, 1). Enes SİMSAR'ın işaretini "İPOTEK bug'ı" sandı; SİMSAR'ın böyle bir saldırısı olduğunu hiç fark etmemişti ("ilk defa duyuyom"). Renk yüzünden bir boss mekaniği oyunda GÖRÜNMEZ olmuştu. Renk
  ayrımı iddiası göze sorulmalı, yoruma değil.
- **İkinci kayıt noktası, birinci kayıt noktasının açığını yeniden açtı (Faz 25 inceleme):**
  K1 oda başı kaydını ekledim. ONARIM eşyayı diske hemen yazıyor, bedelini checkpoint'ten
  düşüyordu — ama oda başı kaydından düşmüyordu. Kapat-aç bedeli geri veriyor, onarım
  kalıyordu: K2'nin kapattığı sonsuz diriliş. Güvenlik ajanı buldu, kapılarım değil.
  **Kural:** geri sarılabilen her yeni kayıt noktasında sor — *hangi etki diske HEMEN
  gidiyor (onarım, eşya, nemesis), ve bedeli bu kayıtta da duruyor mu?* Etki kalıcı,
  bedel geri sarılıyorsa bedava bir şey var demektir.
- **Bozma turunda iki bozma birbirini maskeleyebilir:** oda başı kaydını yazmayan bozma,
  ölümün o kaydı düşürüp düşürmediğini ölçen kontrolü de yeşil bırakıyordu (ölçecek kayıt
  yoktu). Toplu bozmada yeşil kalan kontrolü TEK BAŞINA boz.
- **Otopilotun ilk iki kırmızısı botun kendi ölçüm hatasıydı:** K1'de yarım saniye bekleyip
  ölçtü, o arada saat 1.8 sn aktı. Uyku renginde ilk karede baktı, faz geçişinin 0.2 sn'lik
  beyaz parlaması sönmemişti. **Ölçüm de bir mekanik:** akan bir şeyi beklerken ölçme, geçici
  durumların (parlama, sarsıntı) sönmesini bekle. Kırmızı gelince önce ölçeni sorgula (ders 14 yan ders).
- **Ölçtüğün etkiyi başka bir şey de üretiyor mu?** Dash kontrolü "0.25 sn'de 1.5 birim gitti mi"
  diye soruyordu; yürümek de 1.6 ediyordu. Dash hiç atılmasa da yeşildi. İnceleme ajanı buldu.
  Etki ölçerken, o etkiyi üretebilecek ÖBÜR yolları kapat (burada: yönü tek kare tut, bırak).

- **"Cevabı var" testi tek konumda yeşil, oyunun yarısında kırmızıydı (Faz 26 inceleme):**
  harita saldırılarının kapısı boss'u hep aynı yere, oyuncuyu hep 3/7 birim önüne koyuyordu,
  nemesis büyümesini hiç denemiyordu. 873 konumluk taramada SİMSAR'ın açıklığı duvar dibinde
  kaçılamaz çıktı (arkası duvar, kapı arenanın dışında), TAM TAHSİL nemesis boyunda arenadan
  büyüktü, KEFİL'in simitine 17 birimden yetişilmiyordu. **Kural:** "oyuncu kaçabiliyor mu"
  sorusunu geometriye bağlı her saldırıda KONUM (orta / kenar / duvara sıkışmış) ve ÖLÇEK
  (nemesis) üzerinden tara. Kolay kurgu, sorulmak istenen soruyu sormaz.
- **Botun kontrolü de varsayım taşır:** otopilot "uzakta dur" sahnesinde gelen her yakın
  saldırıyı hata sayıyordu. Bot duvara sıkışınca boss yaklaştı, yakın saldırı DOĞRU geldi.
  Kontrol artık saldırı başlarken botun gerçek uzaklığına bakıyor.
- **Adalet şartı mekaniği sessizce öldürebilir (Faz 26 bot playtest'i):** SİMSAR'ın açıklıklı
  harita saldırısına "boss duvara yakınsa yapma" şartı koydum; kapı adaleti ölçtü ve yeşildi.
  Gerçek dövüşte SİMSAR çoğu zaman kenarda: 16 saldırıda 1 harita geldi, olması gereken 4.
  **Kural:** bir saldırıya "şu durumda yapma" şartı eklerken şartın OYUNDA ne sıklıkla
  tuttuğunu da ölç. Çoğu zaman tutuyorsa şart değil tasarım değişmeli (burada: ortaya sıçra).
- **Ölçüm penceresi başka olayları da yakalar:** bot "hazırlık başladı → vuruş indi" arasında
  saat düştüyse "bu saldırı vurdu" diyordu. Kalabalıkta (rakipler, işaretler) KOMİSYON+ 14/14
  göründü, gerçekte 0/4. Etkiyi ölçerken pencereyi etkinin KENDİ anına daralt (aynı kare).

## Ders 16 — Saat şişmedi, float yuttu (2026-10-13, Faz 28 bot ölçümü)

Bot 1. bölgeyi 9931 sn ile bitirdi. Geç odalarda ve mini boss'larda saat neredeyse hiç akmıyordu
(oda 43: 228 sn, akış −9.7). "Ekonomi çok cömert" diye kadran çevirmeye hazırdım.

**Asıl sebep:** bot gizli pencerede saniyede ~3600 kare koşuyor. Kare başı akış ~0.0003 sn.
`float` saatte 8192'nin üstünde iki sayı arası 0.001 — çıkarma yuvarlanıp KAYBOLUYOR;
4096–8192 arasında ise yukarı yuvarlanıp FAZLA akıyor. Ölçüm çöp, ama çöpün bir kısmı
(ilk ara bölgenin +960'ı) gerçekti — iki sorun üst üste binmişti.

- **Kural:** kare kare biriken her şey (saat, toplam süre, toplam akış) `double` tutulur.
  8 saatlik koşuda `RunTime` yüksek FPS'te aynı yolla bozulurdu — bot yalnızca erken gösterdi.
- **Kural:** "saat akmıyor" gibi FİZİĞE aykırı bir ölçüm, ayar sorusu değil hata sorusudur.
  Önce oyun zamanı / gerçek zaman / akış ayrımını ölç (bot nabzı, oda hesabında `süre`), sonra ayarla.
- Kapı: `Faz28SelfCheck` 9000 sn'lik saatte 10 000 × 0.0003 sn'lik kare. float'la 9.77, doğrusu 7.50.
- **Rapor kendi formülünü yazınca yalan söyler (aynı gün):** zorluk raporu düşmanın aurasını
  kendisi hesaplıyordu (`Kind.AuraDrain + bossAuraDrain`); oyun ise boss'un KENDİ aurasını
  kullanıyor (mini boss'ta yarısı). Rapor mini boss aurasını 10 kat fazla saydı, ORTA oyuncuyu
  erken öldürdü. **Kural:** rapor oyunun değerini SORAR (`Enemy.AuraOranı`), kopyalamaz —
  `RoomManager.Tür`, `Clock.Fiyat`, `Clock.Doyma` ile aynı ilke.
- **Kural tuşa basma yolunda yaşarsa başka çağıran deler (Faz 29):** sunağın "tükendi" kuralı
  `Update`'te (`!SoldOut && UsePressed()`) duruyordu, `Buy` bakmıyordu. Silah sunağının "boss
  başına iki alım" kapısı `Buy`'ı doğrudan çağırınca üçüncüyü sattı. **Kural:** sınır, bütün
  çağıranların geçtiği fonksiyonun başında olur (ders 14'ün `SaveFile.Flush` dersiyle aynı).
- **Bozmalar aynı satıra dokunursa biri öbürünü örter:** "yükseltme başka silaha geçmiyor"
  bozması GENİŞ AĞ bozmasıyla aynı satırdaydı (`SavurmaAçısı`), ilk koşuda yeşil kaldı. Tek
  başına koşunca kırmızı. Bozma turunu dokunulan SATIRA göre böl, mekaniğe göre değil.
