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

