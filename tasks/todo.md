## 📥 ENES'İN MASASI

- [ ] **Faz 8 planına onay** — zamanlama kilitleme. Aracı kurayım mı
- [ ] **Referans topla** *(bedava, hiç çöpe gitmez, asset işini erken açar)*
      Beğendiğin izometrik oyunlardan ekran görüntüsü biriktir. Hades, Death's Door,
      Moonlighter, Hyper Light Drifter... Neyi beğendiğini bilmek, model alırken
      "şuna benzesin" diyebilmek demek. Şimdiden başlarsan asset fazı hızlanır

### Asset (karakter/silah/mekân) ne zaman?

Şu dördü kilitlenmeden **başlamaz** — yoksa para ve zaman çöpe gider:

1. Düşman kadrosu kesin *(şu an açık — oda yapısı değiştirebilir)*
2. Silah seti kesin *(şu an açık — "ileride değişim yapılır" dedin)*
3. Dövüş zamanlamaları kilitli *(animasyon bunlara göre yapılır)*
4. Kamera açısı/mesafesi kilitli *(izometrikte ne kadar detay görünüyor)*

**Mekân tasarımı daha da sonra** — hikâye kararına bağlı.

> **B** tuşu: doğrudan boss dalgasına atlar (test kısayolu, gerçek oyunda kalkacak)
> **ENTER**: dalga çağırır (test kısayolu, gerçek oyunda kalkacak)

---

## Faz 8 — Zamanlama kilitleme ⬅️ **PLAN, ONAY BEKLİYOR**

> **Enes istedi (2026-08-25):** *"dövüş zamanlamaları kilitli, animasyon eklemeden
> değişiklik yapıp en uyguna oturtmaya çalışalım"*
>
> Asset kapısının 3. maddesi. Zamanlamalar oturmadan animasyon yapılamaz —
> yapılırsa her denge yaması bir sanat işine döner.

### ⚠️ Asıl sorun ayarlamak değil, KARŞILAŞTIRMAK

Şu an: sayıyı değiştir → Play → oyna → Stop → değiştir → Play. İki değeri
**hafızadan** karşılaştırıyorsun. His hafızası saniyeler içinde bozuluyor;
0.30 ile 0.26 arasındaki farkı iki ayrı oturumda ayırt etmek imkânsız.

**Bu yüzden Faz 8'in birinci işi ölçüm değil, ARAÇ.**

- [ ] **① A/B profil değiştirici** *(fazın kalbi)*
      Oyun içinde tuşla iki-üç zamanlama profili arasında **anında** geçiş.
      Durmadan, yeniden başlamadan. Aynı dalgada A'yı hisset, tuşa bas, B'yi hisset.
      Ekranda hangi profilde olduğun yazar
- [ ] **② Hangi sayılar önemli — envanter**
      Her zamanlama değerinin neyi kontrol ettiği tek yerde. Şu an 55 alan var ve
      hangisinin *hisse* dokunduğu belli değil
- [ ] **③ Ölçülebilirleri ÖLÇ, hissettirmeye bırakma**
      · girdi gecikmesi · dash i-frame'i gerçekten darbeyi yiyor mu (kaç kare pay var)
      · saldırı taahhüt penceresi adil mi (basınca ne kadar kilitleniyorsun)
      · düşman hazırlığı kaçmaya yetiyor mu
- [ ] **④ KİLİTLE** — karar verilen değerler `Tuning.asset`'e, `TASARIM.md`'ye
      *"kilitli"* damgası, animasyon germe payı (±%40) yazılır
- [ ] Faz 8 öz-denetimi
- [ ] 🎮 **PLAYTEST KAPISI** — bu fazın playtest'i uzun, çünkü faz **zaten** playtest

---

## Faz 7 — Görsel kimlik (VFX) ✅ **KAPANDI** (2026-08-25)

> **Neden şimdi:** oyunun en özgün mekaniği — zamanın tek kaynak olması — şu an
> GÖRÜNMÜYOR. Hasat sadece sayının büyümesi. Özgünlük şartı buydu.
>
> **Neden asset'ten önce:** VFX asset değil, kod. Modeller gelince çöpe gitmiyor —
> alttaki mesh değişir, efekt katmanı yaşar. Ayrıca asset alma brief'ini bu netleştirir.

**Yöntem:** `Arc.cs`'in kanıtladığı desen — koddan üretilen mesh + URP shader.
Dosya yok, kütüphane yok, lisans yok. `Sfx` ile aynı felsefe.

- [x] **① ZAMANIN AKIŞI** — `Mote.cs`. Vurunca düşmandan sana akan zaman zerresi.
      **Hızlanarak** varır (t²), yavaşlayarak değil — çekiliyor gibi okunsun, bırakılmış gibi değil.
      Çaldığın miktar zerrenin BOYUNU belirliyor · KASA ölünce toplu patlama
      · SIZINTI/boss aurasında **ters yön, kırmızı** (senden ona)
- [x] **② Silah izleri** — **zaten yapılmış**: savurma yayı `Weapon.Tint` + `Weapon.Range`
      + `Weapon.DashAngle` kullanıyor. Faz 3'ten geliyor, yeniden yazılmadı
- [x] **③ İsabet patlaması** — dışa açılan kısa halka, hit-stop ile aynı anda
- [x] **④ Düşman boşalması / ölümü** — boşalınca gri halka *(en kritik durum değişimi,
      renk tek başına sessizdi)* · ölünce **türün renginde** halka, boss'unki çok daha geniş
- [x] **⑤ Dash izi** — `TrailRenderer` (Unity'nin kendi özelliği, iz kodu yazılmadı).
      Yalnızca dash boyunca çizer; sürekli açık kalsa dash'in ayırt ediciliği kaybolurdu
- [x] **⑥ Boss telegraph** — hazırlık yayı artık **doluyor** (0.25 → 1.0).
      Sabit yay "vuracağım" diyordu ama "ne zaman" demiyordu
- [x] **`UnlitMat`** — `Arc` + `Mote` + dash izi aynı shader kurulumunu istiyordu, tek kaynağa indi
- [x] Faz 7 öz-denetimi: **10/10** — aura kısması ÖLÇÜLDÜ (1 sn → 6 zerre, 60 değil)
- [x] 🎮 **Playtest kapısı** — hepsi geçti. **"okunuyor"** (6. soru: kalabalıkta düşman
      renkleri hâlâ ayırt ediliyor) — kural tuttu

### 🎞️ Akış ve dash animasyonları GEÇİCİ (Enes, 2026-08-25)

> *"evet test için durabilir ileride akış animasyonunu değiştireceğiz"* (zerre akışı,
> sızıntı yönü) · *"ileride dash animasyonunu değiştireceğiz"* (iz)

Zerre akışı ve dash izi **doğru bilgiyi veriyor** ama nihai animasyon değil. Silahlar
ve seslerle aynı karar: yapı dursun, içerik sonra. Değişecek olan **görünüş**;
ne zaman/hangi yönde aktığı bilgisi kalıcı.

### ⚠️ VFX okunurluğu BOĞMAYACAK

Düşman durum renkleri taşıyıcı: kırmızı dolu · turuncu hazırlık · beyaz isabet · gri boş.
Dövüş kararları buna bakıyor. Efekt bunları örtemez — güzelleşip okunmaz hale gelirse
efekt gider, okunurluk kalır. (Yasanın VFX'teki karşılığı.)

---

## Faz 0 — Kurulum ✅

- [x] Unity projesi oluşturuldu (`UnityProjects/Tek Can`, 6000.5.2f1, **URP**, izometrik 3B)
- [x] Git deposu + Unity `.gitignore` + ilk commit
- [x] Klasör iskeleti: `Assets/_Project/{Scripts,Art,Prefabs,Scenes}`
- [x] Proje `CLAUDE.md`'si
- [x] graphify kuruldu — `Assets/_Project/Scripts/graphify-out/` (78 düğüm, 108 kenar, 10 topluluk)

---

## Faz 1 — Dövüş hissi ✅ **KAPANDI** (2026-08-24)

> Hedef: **vurmak iyi hissettirsin.** Enes'in 2. Steam kriteri burada kazanılır ya da kaybedilir.
> Can barı **yazılmayacak** — yerine 20 satırlık taslak saat, ki dövüş baştan doğru baskı altında tasarlansın.

- [x] İzometrik kamera kurulumu (açı, mesafe, takip)
- [x] Hareket + dash + i-frame (his burada başlar)
- [x] `Clock` taslağı — azalır, darbe düşürür. **Dengesiz, ayarsız, sadece var**
- [x] Temel saldırı: kısa, dash ile iptal edilebilir, hit-stop + kamera sarsıntısı + isabet parlaması
- [→] Gerçek VFX (parçacık, iz, boşalma efekti) — **Faz 7'ye taşındı** (Faz 3'te yalnızca silah yayı yapıldı)
- [x] Düşman: düz can havuzu, üstünde **geri sayan saniye sayısı** (uçuşan hasar sayısı YOK)
- [x] **Zaman hasadı** — hasar verdikçe sürekli zaman çalma (oranı ayarlanabilir alan)
- [x] Tek düşman tipi + saldırgan oynamayı ödüllendiren basit YZ (state machine, ECS yok)
- [x] Test sahnesi: greybox arena, 5 düşman
- [x] Bütün his değerleri Inspector'da ayarlanabilir olacak — sayıları sohbette değil playtest'te bulacağız
### Geri bildirim katmanı ✅ (ilk playtest bu eksik olduğu için çökmüştü)

- [x] **Oyuncu savurma yayı** — `Arc.cs`, ıskada da çıkıyor, menzil/açı Tuning'den (görsel = gerçek vuruş alanı)
- [x] **Düşman hazırlık hareketi** — turuncu koni + gövde rengi; yön vuruş başında kilitleniyor → kaçış penceresi gerçek
- [x] **Düşman saldırı görseli** — kırmızı koni + öne atılma
- [x] **Oyuncu hasar tepkisi** — kırmızı parlama + geri itilme + kamera sarsıntısı
- [x] Saldırı menzili/konisi Gizmo çizimi (Hierarchy'den Player'ı seç)
- [x] İsabet ölçek darbesi — düşman vurulunca %28 büyüyüp iniyor
- [x] 🎮 **PLAYTEST TEKRAR** — hasar iniyor, Enes onayladı (2026-08-24)
- [x] ✅ **Saat baskısı** — dövüş dışında hızlandığı fark ediliyor (Enes, 2026-08-24)
- [x] ✅ **Boşalma okunuyor** — kırmızı→gri, sayı→"—", tek atımda ölüm anlaşılıyor (Enes, 2026-08-24)
- [x] 🎮 **Hit-stop + sarsıntı dozu** — A/B ile: `hitStopDuration`/`hitShake` sırayla (0, 0) → (0.25, 0.6) → (0.10, 0.22). Tek başına yargılanamaz, karşılaştırınca anlaşılır
- [x] 🎮 **Hazırlık süresi** (`enemyWindupDuration` 0.55) — düşmanın yanında dur, turuncu koni çıkınca dash at, 10 kez. Hep kurtuluyorsan çok uzun (0.35 dene), hiç kurtulamıyorsan çok kısa (0.8 dene)

## Faz 2 — Saat ekonomisi ✅ **KAPANDI** (2026-08-24)

- [x] **Saniyeyle satın alma** — 3 sunak: HASAT (+%8) · KESKİNLİK (+1.2 hasar) · ÇEVİKLİK (dash −0.08sn). Fiyat her alımda ×1.6, sunak başına 3 alım
- [x] **Yükseltmeler koşu kapsamlı** — Clock'ta tutuluyor, Tuning asset'ine SIZMIYOR (test korumalı)
- [x] **Koşu bitiş ekranı** — süre, öldürme, hasat, harcama, net kazanç. `Clock.OnDeath` artık abonesi olan canlı kod
- [x] **R ile anında yeniden başlama** — `Bootstrap.Restart()`, koşu kökü tek Destroy
- [x] **Dash-atak** — dash sırasında saldırıya bas, dash bitiminde geniş vuruş (×1.6 hasar, 160° koni)
- [x] **Denge simülatörü** — `Tek Can → Denge simülasyonu`, gerçek asset'ten okuyor
- [x] Faz 2 öz-denetimi: 15/15
- [x] 🎮 **PLAYTEST KAPISI GEÇİLDİ** — Enes: sunak, HASAT alımı, dash-atak, yeniden başlama hepsi çalışıyor (2026-08-24)
- [x] **Denge kararı:** hasat %20 → **%35** (temiz +0.7 / dağınık −11.3). Darbe maliyeti 4 sn bilerek kaldı
- [ ] Saat göstergesi gerçek tipografi/animasyon — UI geçişine ertelendi, greybox okunuyor
- [x] Ses — **Faz 6'da yapıldı**, vuruş hissinin kalan yarısı kapandı

## Faz 3 — Silahlar ✅ **KAPANDI** (2026-08-24)

- [x] **`Weapon` soyutlaması** — menzil, açı, süre, hasar, hedef sayısı, hasat çarpanı ve özel davranış silahtan geliyor
- [x] **TEK HEDEFLİ sorunu çözüldü** — `HarvestMultiplier`. Koni içinde üç düşman varken tek hedefli **en yakınını** seçiyor (test korumalı)
- [x] **FAİZLİ** ×2.6 — peşin bedel, öldürünce iki katı iade, parasızken savurmuyor
- [x] **DURGUN** ×2.2 — basılı tutunca saat donuyor; saldırı/dash kapalı, dünya donmuyor
- [x] **VADELİ** ×2.0 — ×2.4 hasar, bedel vuruştan SONRA; savurmanın ortasında ölebilirsin
- [x] **Silah başına VFX kimliği** — yay rengi + şekli, ekran görüntüsüyle doğrulandı
- [x] **1-4 tuşlarıyla silah değiştirme** (test kolaylığı)
- [x] **Denge ölçümü** — kesişim **3 düşmanda**; tek hedefliler yalnız kavgada, koni kalabalıkta üstün
- [x] Faz 3 öz-denetimi: 17/17 (her silahın gerçekten hasar verdiği dâhil)
- [x] 🎮 **PLAYTEST KAPISI GEÇİLDİ** — Enes: *"hepsi çalıştı"* (2026-08-24). Silahlar test için donduruldu, değişim ileride

## Faz 4 — Koşu yapısı ✅ **KAPANDI** (2026-08-25)

> **Çözülen sorun:** öncesinde 5 düşman bir kez doğuyordu ve koşunun tek çıkışı ölümdü.
> Yani **arenayı temizlemek kaybetmek demekti** — dövüş dışı kalıyordun, saat hızlı iniyordu,
> hasat edecek bir şey kalmıyordu. Ekonomi de anlamsızdı: gücü ne için alıyordun?

- [x] **Dalga sistemi** — ENTER ile başlatılır, her dalga daha kalabalık ve daha dayanıklı
- [x] **Dalga arası SAYAÇ YOK** — saat zaten sayaç; oyalanmak, sunaklara bakmak, düşünmek doğrudan ömürden kesiliyor
- [x] **BOSS** — 9 kat havuz, akışı %15'e düşük (beklenerek yenilemez), yaşadığı sürece saati emiyor
- [x] **KAZANMA** — boss düşünce koşu kazanılıyor. Ölümden başka bir son var artık
- [x] `Bootstrap.SpawnEnemyAt` tek doğum yolu — iki ayrı doğum kodu olsaydı biri düzeltilip diğeri unutulurdu
- [x] ENTER kullanıldı, SPACE değil — Space dash, aynı tuşa bağlasaydık çakışırdı
- [x] Faz 4 öz-denetimi: 15/15 ("BOSS ÖLÜNCE KOŞU KAZANILDI" dâhil)
- [x] 🎮 **Playtest turu 1** — dalga sistemi onaylandı (test için)
- [x] İsabet menzili YÜZEYE ölçülüyor — "bossa vururken buglanıyor" çözüldü
- [x] Saldırı taahhüt oldu (`attackMoveDamping` 0) — WASD artık bozmuyor, yalnızca dash bozar
- [x] Girdi gecikmesi 140 ms → ~36 ms
- [x] Boss sertleştirildi: hız ×1.25 · hazırlık ×0.85 · koni ×1.5 · darbe ×2.2
- [x] Dash-atak silaha göre değişiyor
- [x] 🎮 **Playtest turu 2** — "hepsi düzeldi". Boss: *"böyle kalsın, şikayetim olursa söylerim"*
- [ ] ⚠️ **ENTER geçici** — gerçek oyunda dalgalar oyuncu tarafından çağrılmayacak, hikâye/mekân akışına bağlanacak
- [x] ✅ **Düşman çeşitliliği — Faz 5'te çözüldü** (aşağıda)

## Faz 6 — Ses ✅ **KAPANDI** (2026-08-25)

> Faz 1'den beri "vuruş hissinin kalan yarısı" diye duruyordu.

- [x] **Sesler koddan üretiliyor** — hiç ses dosyası yok, kütüphane yok, lisans yok.
      Gürültü + zarf + tek kutuplu filtre. Görsel kimlik gibi ses de sanatçı gerektirmiyor
- [x] 9 ses: savurma · isabet · hasar · dash · hasat · yere vuruş · alım · ölüm · kazanma
- [x] **Hafif rastgele perde** — aynı ses üst üste binince makineleşmesin
- [x] **Hasat sesi yalnızca BÜYÜK kazançta** (KASA'nın toplu ödemesi, FAİZLİ'nin iadesi).
      Her vuruşta çalsaydı isabet sesiyle üst üste biner, ikisi de duyulmazdı
- [x] Faz 6 öz-denetimi: 40/40 — **ilk** kontrol "dinleyici yaşayan nesnede mi"
- [x] **B tuşu: boss'a atla** — Enes boss'a ulaşamadı, test edilemeyen şey ayarlanamaz
- [x] 🎮 **Playtest kapısı** — *"aga sesler geliyor"*

### ⚠️ İki tur sessizlik — `Object.Destroy` ertelenir (2026-08-25)

İlk teslimde 9 sesin dokuzu da doğruydu, 37/37 test yeşildi ve oyunda **tek ses
çıkmıyordu**: sahnede hiç `AudioListener` yoktu. İkinci teslimde de çıkmadı, çünkü
düzeltme "dinleyici var mı?" diye soruyordu — `Object.Destroy` ERTELENDİĞİ için
silinmekte olan şablon kamerasının dinleyicisi hâlâ bulunuyordu.

- [x] `Sfx.EnsureListener` artık sormuyor, **koşulsuz kuruyor**
- [x] **Kök neden:** `Bootstrap` şablon temizliği `DestroyImmediate` — `Restart()` bu
      tuzağı zaten biliyordu, `Build()` bilmiyordu
- [x] 6 kanallı `AudioSource` havuzu — tek kaynakta `pitch`, çalmakta olan sesleri de büküyordu
- [x] `Sfx.Init` her koşuda Console'a dinleyici durumunu basıyor
- [x] Denetim artık sahneye **yem dinleyici** bırakıyor — eski kod bu yemi yutup geçerdi
- [x] `lessons.md` (7): *zincirin son halkası testin içinde mi* + *ertelenmiş Destroy'a sorma, kur*

### 🔊 Sesler GEÇİCİ (Enes, 2026-08-25)

> *"seslerin hepsi kötü ama en azından ne olduğunu ayırt edebiliyoruz o açıdan okey.
> değiştirene kadar dursun test için"*

Sentezlenmiş sesler **test kalitesinde tutuluyor** — ayırt edilebilirlik yeterli,
his değil. Silahlarda verilen kararın aynısı: yapı dursun, içerik sonra.

- [ ] Seslerin gerçek tasarımı — `Sfx` çağrı yüzeyi (`Sfx.Hit()` vb.) aynı kalacak,
      yalnızca üretim değişecek

## Faz 5 — Düşman türleri ✅ **KAPANDI** (2026-08-25)

> **Çözülen risk:** meta ilerleme yok, yani tekrar oynanabilirliğin TAMAMI koşunun
> içinden gelmek zorunda. Tek tip düşmanla dövüş iki dakikada tükeniyordu.
> İlke: "daha çok can" olan bir tür eklemek çeşitlilik değil dolgudur — her tür
> çekirdek soruyu farklı bir yerden vurmalı.

- [x] **`EnemyKind` soyutlaması** — `Weapon` ile aynı desen (veri + davranış kancaları)
- [x] **SIZINTI** — saatini emer → *hedef sırası* kararını değiştirir
- [x] **KASA** — vurarak ödemez, ölünce toplu öder → *taahhüt* kararı
- [x] **KAÇAK** — kaçar, kaçarsa zamanını götürür → *kovala/bırak* kararı
- [x] **Kademeli açılış** — ilk dalga yalnızca TEMEL; hepsini birden atmak öğretmez, boğar
- [x] **Deterministik kompozisyon** — aynı dalga hep aynı karışım; "dalga mı zor, ben mi kötü oynadım" cevaplanabilir kalıyor
- [x] HUD'da sahnedeki tür listesi (greybox'ta renk tek başına yetmiyor)
- [x] Faz 5 öz-denetimi: 15/15 (her türün gerçekten çalıştığı dâhil)
- [x] 🎮 **Playtest kapısı** — *"test süreci gayet okey şuanda"*
- [x] KAÇAK çok hızlı kaçıyordu (7.25 vs oyuncu 6) → çarpan 1.3 (5.23), testle korundu
- [ ] Oda zinciri / uzamsal ilerleme — hikâye kararından sonra

---

## ⚠️ Ayar kaynağı

`Tuning.asset` çalışma anının tek gerçeği; `Tuning.cs` varsayılanları yalnızca asset'te
**henüz olmayan** yeni alanların tohumu. Değer değiştirmek için **Inspector**.

- C# değerlerini asset'e taşımak: `Tek Can → Ayarları senkronla (C# → asset)`
- `TuningAudit` her derlemeden sonra kendiliğinden çalışıp sapmayı Console'a basar

## Notlar

- Fikir defteri (ikinci oyun "Kaos" dâhil): `../../FIKIR_DEFTERI.md`
- Tasarım: `docs/TASARIM.md`
- **Her fazın sonunda:** ne yapıldığını ve nasıl test edileceğini anlat, dur, Enes'in onayını bekle. Yeşil testler "faz tamam" demek değil.
- **En büyük risk:** içerik çeşitliliği. Meta ilerleme olmadığı için tekrar oynanabilirliğin tamamı koşunun *içinden* gelmek zorunda.
