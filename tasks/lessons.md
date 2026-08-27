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
