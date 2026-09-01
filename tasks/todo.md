## 📥 ENES'İN MASASI

- [x] ✅ **Zorluk kontrolü geçti** (2026-08-25) — Faz 1'deki *"biz çok hızlı düşmanlar
      yavaş"* hissi GEÇMİŞ. Enes: *"genel zorluğa göre kolay değil. Taze Hades 2 bitirdim
      ama yine de hiçbir şeysiz bu kadar düşman zorladı."*
      → Oda testleri kirlenmeyecek, yapıya geçilebilir. Ayar ileride
- [x] ✅ **HİKÂYE / MEKÂN KONUŞMASI TAMAM** (2026-08-25) — bütün 🔴 sorular cevaplandı.
      Kararlar `docs/TASARIM.md`'de: ölüm yapısı · 3 diriliş eşyası · checkpoint ·
      3 perde · 7 düşman + 4 boss · 5 silah · kurgu · EA kapsamı
- [x] ✅ **Faz 10 playtest GEÇTİ** (2026-08-26) — bölgeler farklı hissettiriyor,
      zincir akıyor. **Zorluk çok yüksek, ayar ertelendi** *(Faz 10 bölümüne yazıldı)*

### 🎮 FAZ 12 PLAYTEST — dört boss

**N tuşuyla** bölgeler arası gez, **B** ile boss'a atla. Her bölgenin kendi boss'u var.

- [ ] **Dördü gerçekten farklı mı?** Yoksa "aynı boss, farklı sayı" mı hissettiriyor
- [ ] **Faz 2 geçişi anlaşılıyor mu?** Halka + sarsıntı + ekranda suçu yazıyor.
      Kuralların değiştiğini fark ediyor musun, yoksa şaşırıp mı ölüyorsun
- [ ] **③ ve ④ fullenince ne hissediyorsun?** "Kazandım" sandığın anda dolması —
      şok mu, sinir mi? İkincisiyse mekanik doğru ama sunum yanlış
- [ ] **KREDİ kuralı fark ediliyor mu?** İkinci fazda vurunca saatin artmıyor.
      Bunu anlıyor musun, yoksa "hasat bozuldu" mu sanıyorsun *(anlaşılmıyorsa
      HUD'da göstermem lazım)*
- [ ] **TAHSİLDAR faz 2'de kovalıyor** — emmeyi bırakıyor ama peşine düşüyor. Takas adil mi
- [ ] **EKSPER'in cezası çok mu sert?** Yaşadığı sürece hasat %45, faz 2'de sıfır

### 🎛️ A/B ARACI HAZIR — ⏸️ AYAR OTURUMU ANİMASYON ÖNCESİNE ERTELENDİ

> Enes (2026-08-26): *"bunun testini şimdi yapmak yerine ileride animasyonlar öncesi
> yapmayı daha mantıklı buluyorum"*

Doğru: kadro, silahlar ve boss'lar değişmeden ayar yapmak, içerik gelince baştan
ayarlamak demek. **Araç hazır ve bekliyor** — zamanlama kilidiyle aynı oturumda.

Aşağıdaki sorular o oturuma saklı:

Oyunu aç, dövüşün ortasında **F1–F4** ile profil değiştir. Durma yok, yeniden başlatma yok.
Sol üstte hangi profilde olduğun yazıyor.

| Tuş | Profil | Ne değişiyor |
|-----|--------|--------------|
| **F1** | MEVCUT | taban — karşılaştırma noktası |
| **F2** | BOSS BESLİYOR | boss hasadı ×2, aurası yarı |
| **F3** | ODA KÜÇÜK | arena ×0.68, yürüme bedeli düşüyor |
| **F4** | İKİSİ | ikisi birden |

Profiller **ölçümün gösterdiği iki suçluyu** çeviriyor: boss odası (−23 sn/bölge) ve
yürüme (9.6 sn/oda).

- [ ] **F1 ↔ F2: boss ödül mü olmalı, bedel mi?** Boss'u yendiğinde kazanmış mı
      hissediyorsun, yoksa hayatta kalmış mı
- [ ] **F1 ↔ F3: oda küçülünce daralıyor mu?** Yürüme ucuzluyor ama kaçacak yer de azalıyor
- [ ] **F4 yeterli mi?** İkisi birden oyunu doğru yere getiriyor mu, fazla mı kolaylaştırıyor
- [ ] Beğendiğin profili söyle, kalıcı yaparım — sonra sıradaki kadranı çeviririz

**Önceki fazlardan kalan:**

Faz 8 (oda zinciri):
- [ ] **Odadan odaya geçiş çalışıyor mu?** Temizle → yeşil disk açılır → üstüne yürü
- [ ] **Oda bir YOL gibi hissettiriyor mu?** Girişten çıkışa gidiyorsun, düşmanlar arada
- [ ] **Beş yerleşim ayrışıyor mu?** HALKA · DUVAR · YUMAK · KANAT · DAĞINIK
- [ ] **Arena çok mu büyük?** Boş odada yürümek ~4 saniye, saat işliyor
- [ ] **Sunaklar iki odada bir** — sıklık doğru mu

Faz 9 (diriliş):
- [ ] **Boss'a vuruş düzeldi mi?** *(önceki fazdan kalan)*
- [ ] **Öl ve bak: ÖLÜM ALDATMA yerinde kaldırıyor mu?** Düşmanlar duruyor, az zamanın var
- [ ] **GEÇİCİ DÖNÜŞ gergin mi?** 10 saniye, odayı temizlemek zorundasın
- [ ] **Son eşyada uyarı yeterince BAĞIRIYOR mu?** Ölürsen oyun biter, bunu bilmelisin
- [ ] **ONARIM bedeli doğru mu?** 45 saniye — çok ucuz mu, çok pahalı mı
- [ ] **Checkpoint sıklığı?** 3 odada bir
- [x] ✅ **"Yenmek = odayı temizlemek" ONAYLANDI** (2026-08-26) — Enes geçici dönüş
      anında boss'u yendi ve çalıştı. *Not: normal (boss öncesi) odada da denenecek*
- [x] ✅ **Boss'a vuruş düzeldi** — *"boss düzelmiş"*
- [x] ✅ **Ölüm aldatma + geçici dönüş onaylandı** — *"güzel olmuş"*

- [ ] **Ad koyma** — kahramanın adı, diriliş eşyalarının adları, bölge adları *(sende)*
- [ ] **Referans topla** — asset fazını hızlandırır
### Kalan yol

```
✅ hikâye/mekân kararı
→ Faz 8: oda zinciri        ← ŞİMDİ BURADAYIZ
→ Faz 9: checkpoint + diriliş sistemi
→ Faz 10: zamanlama kilidi
→ animasyon + asset
→ erken erişim (giriş + gelişme perdeleri)
```

**Neden bu sıra:** diriliş checkpoint'e bağlı, checkpoint odalara bağlı. Zamanlama
kilidi odalardan sonra çünkü mekân hissi değiştiriyor. Asset en sonda çünkü kadro
ve zamanlama kesinleşmeden alınan model çöpe gider. *(bedava, hiç çöpe gitmez, asset işini erken açar)*
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

## Faz 13 — Düşman kadrosu + kamera kilidi ⬅️ **KOD YAZILDI, PLAYTEST BEKLİYOR**

> Boss'lar zenginleşti ama sıradan odalar Faz 5'ten beri aynı dört türle dönüyordu —
> ve odalar oyun süresinin %80'i.

- [x] **① SİGORTA** — yaşadığı sürece **çevresindekilerin hasadını ×0.25'e kısıyor.**
      SIZINTI zaman EMER, bu GELİRİ KAPATIR: *"önce sigortayı kes, sonra yağmala"*
- [x] **② MÜBAŞİR** — **uzaktan tebligat.** Yaklaşmıyor (7 birim mesafe koruyor),
      durduğun yere gecikmeli vuruş bırakıyor. Bütün düşmanlar yakın dövüştü, yani
      "uzak dur" bedava bir cevaptı — bu onu bozuyor
- [x] **③ ZAMANAŞIMI** — **yaşadıkça güçleniyor** (%9/sn, tavan ×2.5).
      Kite'lamayı ve dövüşü uzatmayı cezalandırıyor: ötelemek onu besliyor
- [x] `TickKindPassives` — tür pasifleri `TickBossPassives`'in eşi olarak AYRI duruyor
- [x] **Kamera kilidi** + `Kamera kilidi raporu` aracı
- [x] Faz 13 öz-denetimi: **15/15** — çeşitlilik ADdan değil SORDUĞU SORUdan ölçülüyor
- [x] 🐛 **Öz-incelemede yakalandı: yedinci tür (ZAMANAŞIMI) HİÇ DOĞMUYORDU.**
      Testleri geçiyordu çünkü testler türü elle kuruyordu — "oyunda ortaya
      çıkıyor mu" sorusunu kimse sormamıştı. İki kapı kesiyordu: ① `Seep`
      tavanı 6'da kalmış ② asıl kök neden — `PickKind`'ın oda merdiveni her
      bölgede 2'ye sıfırlanıyor, `roomsPerRegion=6` ile üst sınır 6'da
      kalıyordu. Merdiven artık önceki bölgenin tavanından başlıyor.
      Yeni kapı seçiciyi ÇALIŞTIRIP çıkan türleri sayıyor. (ders 11)

### 📐 KAMERA KİLİDİ — asset brief'i

| | |
|---|---|
| Açı / eğim | **45° / 50°** |
| Ortografik boy | **9.0** (18 birim görüş yüksekliği) |
| Eğim kısaltması | dikey nesne **×0.64** görünüyor |
| **1080p'de karakter** | **~77 piksel** |

⚠️ **Bu sayı asset alışverişinin brief'i:** 77 pikselde yüz detayı, ince doku ve küçük
aksesuar GÖRÜNMEZ. Para **siluet ve renge** harcanmalı — o boyutta ayırt edilen tek şey
onlar. Ölçüm, `CLAUDE.md`'deki "kimliği siluet + VFX'e yasla" kararını doğruluyor.

---

## Faz 12 — Boss çeşitliliği ⬅️ **KOD YAZILDI, PLAYTEST BEKLİYOR**

> Şu an **tek boss** var, beş bölgede beşi de aynı. Ve bu en kötü yerde tekrar:
> boss her bölgenin doruk noktası ve ölçüme göre bölgenin −23 saniyesi orada gidiyor.
>
> Hikâye çerçevesi kuruldu (`docs/TASARIM.md` → BORÇ EKONOMİSİ), dört boss belli.

- [x] **`BossKind`** — `EnemyKind` / `Weapon` / `RoomLayout` / `Region` ile aynı desen
- [x] **İki fazlı yapı** — ① ② yarıda döner, ③ ④ havuz bitince FULLENİR
- [x] **⚠️ İkinci fazın havuzu KREDİ** — vurunca erir ama hasat VERMEZ
- [x] **① TAHSİLDAR** — faz 1 emer / faz 2 **emmeyi bırakır ama KOVALAR** (×1.55 hız).
      Aynı tehdidin daha çoğu değil, BAŞKA bir soru: uzak durmak artık işe yaramıyor
- [x] **② EKSPER** — yaşadığı sürece hasadın ×0.45 / faz 2'de **×0** ("acele et" değil,
      "onu bitirmeden zenginleşemezsin")
- [x] **③ KEFİL** — faz 2'de **ikiye bölünür**, ikisi de yarım havuz + kredi
- [x] **④ MURABAHACI** — faz 2'de **havuzunu geri sarar**, vurmadığın her saniye doluyor
- [x] **Bölge → boss eşlemesi** — her bölgenin kendi alacaklısı
- [x] **HUD duyurusu** — faz 2'de boss'un SUÇU yazıyor ("artık kaçamazsın").
      Halka "bir şey oldu" diyor, yazı NE olduğunu söylüyor
- [x] **`BossAttack` dağarcığı** (Enes istedi) — her boss KENDİ saldırılarıyla dövüşüyor,
      faz 2'de hem YENİ saldırı hem eskilerin GELİŞMİŞ hâli
- [x] **İki yeni saldırı kavramı** — mevcut makine yeni VERİYLE çalışabildi:
      **SİMİT** (göbeği güvenli → halkanın TERSİ, "yaklaş") ve **TEKRAR** (bir kez
      kaçmak yetmiyor). Ayrıca **HAMLE** (vuruşta mesafe kapatma)
- [x] Faz 12 öz-denetimi: **29/29** — kredi kuralı GERÇEK SAATTEN, saldırı çeşitliliği
      **SORDUĞU SORUDAN** ölçülüyor (ad farkı sahte çeşitlilik olabilir)
- [ ] 🎮 **PLAYTEST KAPISI**

### Saldırı dağarcıkları

| Boss | Faz 1 | Faz 2 |
|------|-------|-------|
| **TAHSİLDAR** | HACİZ (koni) · TAHSİL (halka) | HACİZ+ *(hamle)* · TAHSİL+ *(×2)* · **ZORLA ALIM** *(5 birim atılış)* |
| **EKSPER** | DEĞER BİÇME (24° ışın) · KESİNTİ | DEĞER BİÇME+ *(60°, artık biçiyor)* · **HACİM** *(simit)* · KESİNTİ+ |
| **KEFİL** | TEMİNAT (simit) · KEFALET | TEMİNAT+ *(göbek daraldı)* · **ORTAK BORÇ** *(düz halka — simidin tersi)* · KEFALET+ *(×2)* |
| **MURABAHACI** | VADE · İCRA · **TEMERRÜT** *(açıklık)* | **FAİZ** *(×3)* · **BİLEŞİK FAİZ** *(zincir)* · **TEMERRÜT+** *(zincir)* · VADE+ · İCRA+ |

**Son boss'a iki YENİ soru eklendi** (Enes istedi — 5 saldırılık dağarcık):

- **GÜVENLİ AÇIKLIK** — halkanın **arkasında** güvenli bölge. Üçüncü kaçış sorusu:
  "uzaklaş" da değil "yaklaş" da değil, **"arkasına geç"**.
  ⚠️ Açıklık ÖNE konsaydı boss hazırlık boyunca oyuncuya döndüğü için oyuncu daima
  içinde kalır ve saldırı hiç isabet etmezdi — mekanik sessizce anlamsızlaşırdı
- **ZİNCİR** — farklı bir saldırı beklemeden ardından geliyor. `Repeat` aynısını
  tekrarlar, zincir FARKLI birini bağlar: *BİLEŞİK FAİZ* halkayla dışarı atıyor,
  hemen ardından simitle içeri çağırıyor. **Bir cevaba yerleşemiyorsun**

⚠️ **KEFİL faz 2'de en zoru:** simit "yaklaş" derken ORTAK BORÇ "uzaklaş" diyor, ve
ikisi birden var. Ezber bozuluyor.

**Bu fazda YAPILMAYACAK:** nemesis *(boss kimliği oturduktan sonra)*, isimler
*(Enes'te)*, hikâye metinleri *(içerik)*.

---

## Faz 10 — Bölge zinciri ✅ **KAPANDI** (2026-08-26)

> Enes: *"basitçe istediğin ve koyduğun mekanikler çalışıyor. bölgeler gittikçe
> zorlaşıyor canımı çıkarttılar. bossu yendikten sonra sonraki bölgeye geçebiliyorum."*
>
> **Kural mekanizması DOĞRULANDI** — bölgeler gerçekten farklı hissettiriyor.

### ⚠️ ZORLUK ÇOK YÜKSEK — ayar ertelendi (Enes kararı)

> *"oyunun güncel hali çok zor ileride değiştireceğiz"*

Sebebi `docs/TASARIM.md`'ye yazıldı, kör ayar yapmayalım diye. Özeti:
**beş büyüme eğrisi çarpılıyor ve kimse çarpımı tasarlamadı** — oda düşman sayısı
(×1.5) · oda havuzu (×1.18) · bölge boss'u (×1.35) · bölge kuralı · tür açılımı.
5. bölgenin 5. odası tasarlanmış bir zorluk değil, dört eğrinin kazası.

⚠️ Zorluğun bir kısmı **test artefaktı**: `regionsPerAct` bütün kurallar denensin
diye ajan tarafından 5'e çıkarıldı.

- [ ] **Zorluk ayarı** — Faz 11'in (zamanlama kilidi) parçası, A/B aracıyla aynı oturumda.
      Sıra: ① koşu uzunluğu ② eğrileri tek tek ölç ③ en son kural şiddetleri

> Boss ölünce oyun bitmiyor artık. **Bölge** bitiyor, çıkış açılıyor, sıradaki bölgeye
> geçiyorsun. Koşu yalnızca SON bölgenin boss'u düşünce kazanılıyor.

- [x] **`Region`** — 5 bölge, `Weapon`/`EnemyKind`/`RoomLayout` ile aynı desen
- [x] **⚠️ KURALLAR SAATTEN GEÇİYOR** — bölgeye özel can/zırh/direnç YOK, yasa duruyor

| Bölge | Kural | Ne değişiyor |
|---|---|---|
| **EŞİK** | — | Önce öğren |
| **DARALMA** | oyalanmak pahalı | Dövüş dışı akış ×1.7 |
| **KITLIK** | hasat az | Vuruş kazancı ×0.65 |
| **AĞIRLIK** | hata affetmiyor | Darbe bedeli ×1.8 |
| **SIZINTI** | sürekli emiliyor | Dövüşte BİLE 0.6/sn arka plan emişi |

- [x] **Zincir** — ara boss'un odasında ÇIKIŞ var, son boss'unkinde yok
- [x] **Düşman havuzu bölgeye bağlı** — Enes: *"bölgeye göre yenileri ekleniyor"*.
      Yeni tür yeni MEKÂNLA geliyor
- [x] **Boss her bölgede sertleşiyor** (`regionBossGrowth` 1.35)
- [x] **Bölge kimliği** — zemin rengi + HUD'da bölge adı ve kuralı
- [x] **Checkpoint bölgeyi de kaydediyor**
- [x] Faz 10 öz-denetimi: **19/19** — kuralların ETKİSİ ölçülüyor, tanımı değil
- [x] Faz 4 öz-denetimi tek bölgeye sabitlendi (koşu yapısı orası, zincir burası)
- [ ] 🎮 **PLAYTEST KAPISI**

### Yakalanan kırılganlık

`RoomManager` oda kökünü `transform.parent`'a bağlıyordu. Oyunda `Systems` altında
olduğu için çalışıyordu, ama kök seviyesindeki bir nesnede `parent` NULL olur ve oda
kökü **öksüz kalır** — sahibi yok edilse bile temizlenmez. Öz-denetimler arası kalıntı
çıkış kapısı bıraktığı için yakalandı (kontroller sıraya bağımlı hale gelmişti).

---

## Faz 9 — Checkpoint + diriliş ✅ **KAPANDI** (2026-08-26)

- [x] **`SaveFile`** — diske JSON. ⚠️ **Ölüm anında YAZILIYOR**, gecikmesiz, UI'dan önce.
      Sıra kutsal: **eşya düşülür → diske yazılır → sonra diriliş uygulanır**
- [x] **Üç diriliş eşyası** (`RevivalKind`), kullanım sırası **tasarıma gömülü**:
      ① ÖLÜM ALDATMA (yerinde kalk, checkpoint zamanının %35'i)
      ② GEÇİCİ DÖNÜŞ (10 sn — odayı temizle ya da checkpoint'e düş)
      ③ ÇAĞRI (checkpoint'e dön)
- [x] **Eşya yoksa OYUN BİTER** — gerçekten. Checkpoint de eşyalar da gider
- [x] **Son eşyada HUD bağırıyor** — "SON CAN · ölürsen biter", yanıp sönerek.
      Gerçek tehlike sertlik değil SÜRPRİZ
- [x] **ONARIM sunağı** — checkpoint odasında, bedeli **ZAMAN**.
      Ayrı malzeme yok → ikinci para birimi doğmuyor → yasa duruyor
- [x] **Checkpoint** — `checkpointEveryRooms` (3) odada bir, odaya GİRERKEN yazılıyor
- [x] Faz 9 öz-denetimi: **20/20** — en kritik test diske GERÇEKTEN yazıp GERİ OKUYOR

### ⚠️ Yorumladığım yer — onayla ya da düzelt

Enes: *"geçici olarak geri getirip eğer yenemezsen o süre içinde checkpointe atacak."*

**"Yenmek" = odayı temizlemek** diye yorumladım. Başarırsan checkpoint zamanına
kavuşuyorsun (eşya boşa gitmedi). Başaramazsan checkpoint'e düşüyorsun ve **bunun için
ikinci bir eşya harcanmıyor.**

### ⚠️ Bu fazda YAPILMADI

- **Eşyalar hikâyeden gelmiyor** — hikâye yok. Şimdilik `Tuning`'deki test sayıları
  (`testDefianceCount` vb.). Gerçek oyunda o alanlar kalkacak
- **Nemesis (a)** — seni öldüren boss güçlenmiyor henüz
- **Dünya ilerledi (c)** — betikli varyasyon yok, içerik gerektiriyor
- **Checkpoint aralığı oda cinsinden** — gerçek ölçü 30dk-1sa olacak

---

## Faz 8 — Oda zinciri ✅ **KAPANDI** (2026-08-26)

> Artık tek arena yok. Odadan odaya yürüyorsun, her odada duran düşmanlar var,
> bölgenin sonunda boss. **ENTER kalkıyor** — dalgayı sen çağırmıyorsun.

**İyi haber:** sıfırdan başlamıyoruz. Şu anki arena zaten bir oda —
alan + yerleştirilmiş düşmanlar + bir dövüş. Eksik olan **zincir.**

- [x] **① Oda = alan + yerleştirilmiş düşmanlar + çıkış** (`RoomManager`)
      Düşmanlar odada DURUYOR. Oyuncu girişe konuluyor, oda bir YOL oldu
- [x] **② Odalar arası geçiş** — `ExitGate`. Temizleyince açılır, YÜRÜYEREK geçersin.
      Otomatik değil: saat işlemeye devam ediyor, oyalanma kararı oyuncunun
- [x] **③ Bölge = oda dizisi + sonunda boss** — boss odasının çıkışı yok, bölge orada biter
- [x] **④ ENTER KALKTI** — dalga çağırmak yok, odaya girince dövüş başlamış
- [x] **⑤ Beş oda şablonu** (`RoomLayout`) — HALKA · DUVAR · YUMAK · KANAT · DAĞINIK.
      `Weapon`/`EnemyKind` ile aynı desen. Deterministik
- [x] **⑥ Doğurma yalnızca boss odasına ayrıldı** — sıradan odalarda hiç doğurma yok
- [x] `Tuning` alanları odaya çevrildi (`waveCount` → `roomsPerRegion` vb.)
- [x] Faz 8 öz-denetimi: **22/22** · Faz 4 öz-denetimi odaya göre yeniden yazıldı (25)
- [x] **`RoomCapture`** — beş yerleşimi GERÇEK `RoomManager` ile kurup PNG'ye çeker
- [ ] 🎮 **PLAYTEST KAPISI**

### ⚠️ Bu fazda YAPILMADI — dürüst kayıt

**Boss yardımcı çağırmıyor.** Karar "doğurma yalnızca boss dövüşüne özel" idi; bu fazda
yalnızca **negatif** tarafı yapıldı (hiçbir sıradan oda doğurmuyor). Boss'un ikinci fazı /
yardımcı çağırması bir BOSS tasarımı işi ve henüz yok. Boss dövüşü fazına kaldı.

### Ölü kod uyarısı

`Bootstrap.RingPosition` ve `enemyCount` artık yalnızca testlerin sabit halka kurması için
duruyor — oyun `RoomLayout` kullanıyor. Silinmedi çünkü `Faz1SelfCheck`/`Faz2SelfCheck`
onlara dayanıyor.

**Bu fazda YAPILMAYACAK** *(sonraki fazlar)*: checkpoint, diriliş eşyaları, kayıt
sistemi, gerçek harita/görsel bölge kimliği.

---

## Faz 10 — Zamanlama kilitleme ⏸️ **ERTELENDİ — odalardan sonra** (2026-08-25)

> **Enes sordu:** *"bunu şimdi kurmak mı yoksa animasyon ve asset gelmeden önce
> kurmak mı daha mantıklı"* — sezgisi "animasyon/asset öncesi" idi ve doğruydu.

**Neden ertelendi:**

1. **Mekân yapısı zamanlama hissini değiştiriyor.** `dashDistance` 2.5, geniş arenada
   kısa bir sıçrama, dar odada odayı baştan başa geçmek demek. `attackRange` de öyle.
   Odalar gelmeden kilitlenen sayılar, odalar gelince kırılır.
2. **Ortada çözülecek somut soru yok.** Tek açık şikâyet Faz 1'den kalma
   ("biz çok hızlı, düşmanlar yavaş vuruyor") ve **bayat** — attack-commitment
   düzeltmesinden, tek hedefli silahlardan ve boss sertleştirmesinden ÖNCE söylendi.

**Yeni yeri:** oda yapısı oturduktan SONRA, animasyondan ÖNCE.

**⚠️ Ertelemenin riski ve ucuz sigortası:** dövüş zamanlaması gerçekten bozuksa
bundan sonraki her playtest kirlenir — odaları test ederken "oda kötü" dersin ama
dövüş kötüdür, yanlış yeri düzeltirsin. Sigorta: Enes bir koşu oynayıp eski hissin
durup durmadığını söyler *(masada)*. Duruyorsa alet beklemeden, tek değişken olarak
şimdi düzeltilir.

---

## Faz 8 planı (ertelendi, içerik duruyor)

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

## 🚨 TESLİMDEN ÖNCE KALKACAK — test kolaylıkları

> Erken erişime debug tuşlarıyla çıkmak gerçek bir hata. Bunlar dört ayrı faz
> bölümüne dağılmıştı; tek liste olmadan biri mutlaka unutulur.

- [ ] **B tuşu** — boss odasına atla *(`RoomManager.SkipToBossPressed`)*
- [ ] **N tuşu** — sıradaki bölgeye atla *(`RoomManager.SkipRegionPressed`)*
- [ ] **1-4 tuşları** — anlık silah değiştirme *(`PlayerController`)*.
      Gerçek oyunda silahlar YOLDA BULUNUYOR
- [ ] **`testDefianceCount` / `testLastStandCount` / `testRecallCount`** *(`Tuning`)* —
      diriliş eşyaları HİKÂYEDEN gelecek, başlangıç stoğundan değil
- [ ] **`regionsPerAct = 5`** — bütün kurallar test edilebilsin diye; gerçek değer
      koşu uzunluğu ayarlanınca belirlenecek
- [ ] **`enemyCount` / `Bootstrap.RingPosition`** — yalnızca öz-denetimler kullanıyor,
      oyun `RoomLayout` kullanıyor. Testler taşınırsa silinebilir

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
- [x] ✅ **ENTER kalktı — Faz 8'de** (odaya girince dövüş zaten başlamış oluyor)
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
