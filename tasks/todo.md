## 📥 ENES'İN MASASI — 2026-09-30

> Tarihçe (Faz 0–23, eski playtest raporları, 12 maddelik playtest soruları) → [arsiv.md](arsiv.md). Hiçbir şey silinmedi.

| # | Ne | Nasıl | Nereye |
|---|---|---|---|
| 2 | 🎮 *(isteğe bağlı)* **Faz 26 his testi** | Bot 7 maddeyi oynadı (aşağıda). Sana kalan his: TAM TAHSİL/İHALE sıçraması okunuyor mu, ×5 ısrar adil mi | Gördüklerini yaz |
| 4 | ✏️ **Adlandırma** | Kahraman, 5 boss, 5 bölge, 3 diriliş eşyası. Şimdikiler çalışma adı | `docs/TASARIM.md` "Açık" |
| 6 | 🎮 **Faz 28 testi** | F6 yeni koşu → dövüş odası 5 dalga · **M** sıradaki mini boss · mini ölünce sunak (kontrol noktası) · eşyasız ölümde kontrol noktasına dönüş, birkaç ölümde "… TOPARLANDI" · **B** ana boss (saat yavaş akar). His: oda süresi, dalgalar, mini boss, saatin gidişi | Gördüklerini yaz |
| 7 | 🎮 *(isteğe bağlı)* **Faz 29 his testi** | Bot oynadı, 8/8 geçti. Sana kalan his: silah sunağı seçimi karar gibi mi, fiyatlar, MUAFİYET hissediliyor mu | Gördüklerini yaz |
| 8 | 🎮 **Faz 30 testi** | **F6** yeni koşu: 2 silah, 3. yuva boş · **M** ile sunaklar: 2. ara bölgenin sunağında HACİZ · 4. ara bölgenin 4. odasında kenarda parlayan köşe (İPOTEK) · sunak odasında **KADRO** sunağı ([E] yuvadaki silahı değiştirir) · combo: KONİ'yle vur, hemen 2'ye geç, İPOTEK at · **B** boss → İFLAS + combo sunağı | Gördüklerini yaz: combo hissi, 0.8 sn yetiyor mu, köşe görünüyor mu |
| 9 | 🎮 **Faz 31 testi** | **F6** yeni koşu, **N** 2. bölge · 3. ara bölgenin sunağında REHİN: sol tık yakala, tekrar sol tık fırlat, hemen **1** KONİ = SİLİP SÜPÜR · sağ tık TAKAS · dash + sol tık SÜRÜKLEME · **B** 2. boss → ÇEK (KADRO sunağından envantere al): fırlat, değince HACİZ'e geç = SENET HACZİ · 3. bölgenin 3. ara bölgesinin 4. odasında köşe → KARŞILIK: düşman vurmadan hemen önce sol tık (karşılar), sonra FAİZLİ = KARŞI İCRA | Gördüklerini yaz: KARŞILIK penceresi (0.23 sn) yetiyor mu, rehine taşıma hissi, çekin dönüşü |
| 10 | 🎮 **Faz 32 testi** | **F6**, **N N**, **B** 3. boss → TAHVİL: sol tıkı BASILI TUT (VADE 1/2/3 yazar, saat iki kat akar), bırak · hemen **2** FAİZLİ = VADE FARKI · 4. bölgenin 3. sunağında MÜŞTEREK: bir düşmana vur, sonra başkasına → pembe çizgi = bağ; birine ne vurursan öbürü de yer · sağ tık ZİNCİRLEME · bağın üstünden dash = KOPARMA · **B** 4. boss → ARBİTRAJ: arkadan ×2.5, önden ×0.5 · sağ tık görünmezlik · düşmanın içinden dash + sol tık = TERS İŞLEM · 5. bölgenin 3. ara bölgesinin 4. odasında köşe → KİRA: kule kur, sağ tık TAHLİYE (kalan kirayı peşin ödersin) | Gördüklerini yaz: basılı tutmak sıkıcı mı, bağ okunuyor mu, arkaya geçmek zevkli mi, kule kirası adil mi |
| 5 | 🖼️ **Referans topla** | Beğendiğin izometrik oyunlardan ekran görüntüsü (Hades, Death's Door…). Asset fazını hızlandırır | Bir klasör, yeri sen seç |
| 13 | 🎮 *(isteğe bağlı)* **Faz 33 his testi** | Bot oynadı (P33 3/3, P33c 12/12 — aşağıda "BOSS SÜRELERİ" bölümü). Sana kalan yalnızca göz: **F6**, **M** → sunak renkleri okunuyor mu (açık gri yaygın · mavi nadir · mor epik · turuncu efsanevi), **B** → efsanevi "vay" dedirtiyor mu | Gördüklerini yaz |

---

## ✅ Kararlar — 2026-09-30

- **K1 = b** — oda başı kayıt. Oyunu kapatırsan girdiğin odanın başına, girdiğin andaki zamanla dönersin.
- **K2 = evet** — ÇAĞRI'nın geri verdiği zamana taban.
- **K3 = kalsın** — sunak odasında bekleme süreleri işlemeye devam ediyor. Kod değişmiyor.
- **SİMSAR faz 2 = havuz bitince** (2026-10-03) — KEFİL ve MURABAHACI gibi fullenip dönüyor.
- **FAİZLİ İCRA eşiği = evet** (2026-10-03) — eşiğin üstündeyken İCRA öldürünce zaman vermez. Faz 20 ile yapılacak.
- **Yükseltme gücü = A** (2026-10-19) — zayıf nadir/epik/efsanevileri güçlendir, aşırı güçlü yaygınları üst enderliğe taşı, fiyatlar aynı. Uygulandı (Faz 33 bölümü, "A — güç ayarı").
- **Boss süreleri** (2026-10-20) — *"bossların uzun sürmesi okeydir ama her bir boss öncekinden daha uzun sürmeli"*. Bot ölçtü, EKSPER'in havuzu ×1.25 (aşağıda "BOSS SÜRELERİ").
- **Bot düzeltmesi (işaretten menzilde çık) = uygula** (2026-10-20). Uygulandı.

---

## 🎮 BİRLEŞİK PLAYTEST — SONUÇ (2026-09-30)

| # | Madde | Cevap (kısa) | Ne olacak |
|---|---|---|---|
| 1 | Odalar | Dizilişler tamam. Oda şekli asset fazında değişken olacak | Not → asset fazı |
| 2 | ZIRHLI / İRİ | **Ayırt edilmiyor.** İRİ: daha çok can + daha çok hasar. ZIRHLI: az hasar alır, az zaman verir. Yeni fikir: KALKANLI | Faz 25 (ayrım) · KALKANLI sonra |
| 3 | Sunak | Karar oluyor, fiyat artıyor. Ama ilk sunakta hasadı fullemek çok fazla zaman basıyor | Faz 26 (denge) |
| 4 | TAHSİLDAR | **Tek atımda öldü** (1 hasar, 1 çeviklik, 3 hasat ile, 5 sn). Düşmanlar bölge 1'de zorlamıyor, bölge 2'den sonra çok kolay | Faz 26 (denge) |
| 5 | Sağ tıklar | Beşi farklı, ama daha özgün olabilir. FAİZLİ İCRA yeniden. Silaha özel güçlendirmeler | Faz 20 ile |
| 6 | HACİZ | Vuruş değiyor. **Ama geri dash kafa karıştırıyor** | Faz 25 |
| 7 | İPOTEK | Arkada iz yok, yeni odada patlamıyor ✅. Dash-atak = şimdiki sağ tık, yeni sağ tık = ipotek yağmuru | Faz 20 ile |
| 8 | SİMSAR DEVİR | (cevaplanmadı) Yeni istek: boss sonrası ayrı bir sunak, silaha özel yetenekler | Faz 20 = bu |
| 9 | KEFİL / SİMSAR | KEFİL güzel ✅. **SİMSAR: kırmızı halkadan kaçılamıyor, "İPOTEK bug'ı" geri geldi, faz 1 hep alan saldırısı, faz 2 yarı havuzda.** Genel: düşmanlar anlık tepki veriyor | Faz 25 |
| 10 | MURABAHACI | **Faz 1'de 2 saldırı görüldü.** Zorluğu güzel, uzun. Faz 3'e gelinemedi → test tuşu lazım | Faz 25 |
| 11 | Nemesis | Çalışıyor ✅ | — |
| 12 | Son eşya | Çalışıyor ✅ | — |
| — | Genel | Her geçiş (oda, faz…) çok ani. Araya animasyon ve bekleme | Not → asset fazı |

---

## ✅ FAZ 25 — Playtest düzeltmeleri — **KOD BİTTİ, PLAYTEST BEKLİYOR** (2026-10-03)

Kök nedenler kodda bulundu. Her madde: **ne görüldü → sebep → düzeltme → kapı ne ölçecek.**

**Hatalar**
- [x] 🐛 **Boss'lar dağarcığın çoğunu hiç kullanmıyor** (madde 9, 10). Dağarcıkta 97 saldırı var ama oyuncuya ulaşan küçük bir kısmı.
  *Sebep:* boss, dağarcığındaki EN UZUN erişimli saldırının menziline girdiğin an saldırıya başlıyor (`Enemy.TetikMenzili`), sonra yalnızca o mesafeden erişenler arasından seçiyor (`Ayıkla`). MURABAHACI faz 1'de en uzun erişim ~13 birim, arena 14 → her yerden saldırıyor. ~9.6 birimden uzaktaysan erişen yalnızca SÖZLEŞME ve İHTARNAME. Gördüğün 2 saldırı bunlar. SİMSAR'da uzak ve orta mesafede erişenlerin çoğu halka → "hep alan saldırısı".
  *Düzeltme (Enes'in kuralı):* **yakındaysan yakın saldırılar sırayla, uzaktaysan menzilli saldırılar sırayla.** İki ayrı sıra, iki ayrı sayaç. Her saldırı TEK bir sıraya ait, kendi erişimine göre (`BossAttack.Uzak`). Alan saldırıları artık "iki sıraya da" girmiyor: halkalar iki listede birden olunca SİMSAR'ın listelerinin yarısından çoğu halka oluyordu.
  Boss sırası gelen saldırının menziline girene kadar yürür, sonra vurur. Sayaç yalnızca saldırı gerçekten yapılınca ilerler; sen mesafe değiştirirsen öbür sıranın sıradaki saldırısına bakar.
  *Kaçış kapanıyor:* sen koşup boss'un yetişmesine izin vermezsen, kısa bir beklemeden sonra (kadran, ~1.5 sn) aynı sıradan o an yetişen ilk saldırıyı yapar. Yoksa kaçarak saldırıları tamamen iptal edebilirdin.
  ⚠️ *Yan etki:* MURABAHACI'nın haritadaki güvenli nokta saldırıları (SÖZLEŞME, İHTARNAME) menzilli sırada. Dibine yapışırsan bunlar gelmez, yakın saldırıları gelir. Senin kuralının doğal sonucu, playtest'te bak.
  *Kapı:* simüle dövüş — uzak duran oyuncuya o fazın BÜTÜN menzilli saldırıları sırayla, dibe giren oyuncuya BÜTÜN yakın saldırıları sırayla OYNANIYOR mu. Kaçan oyuncu saldırıdan kurtulamıyor mu.
- [x] 🐛 **Düşmanlar anlık tepki veriyor** (madde 9, genel).
  *Sebep:* hazırlık boyunca düşman her karede sana dönüyor (`Enemy.TickState` → `FacePlayer`), yön ancak vuruş anında kilitleniyor. Boss hazırlığı 0.1–0.25 sn, insan tepkisi ~0.25 sn. Koni seni takip ediyor, kaçış ancak son karede dash ile.
  *Düzeltme:* hazırlığın ilk kısmında takip, sonra yön KİLİT (uyarı yayı donar) + asgari hazırlık süresi. İkisi de `Tuning`'de kadran, ayar oturumunda ayarlanır.
  *Kapı:* uyarı başladıktan 0.25 sn sonra yana kaçmaya başlayan oyuncu koniden çıkabiliyor mu.
- [x] 🐛 **SİMSAR'ın kırmızı halkasından kaçılamıyor** (madde 9).
  *Sebep:* güvenli açıklık boss'un ARKASINDA ve boss hazırlık boyunca sana dönüyor. Sen açıklığa koştukça açıklık da dönüp hep tam karşına kaçıyor.
  *Düzeltme:* açıklıklı saldırılarda yön hazırlığın BAŞINDA kilitlenir.
  *Kapı:* hazırlık başında koşmaya başlayan oyuncu açıklığa yetişebiliyor mu. Yetişemiyorsa süre ya da açıklığın yeri ayarlanır.
- [x] 🐛 **"İPOTEK bug'ı" = SİMSAR'ın işareti** (madde 9). SİMSAR 2 sn'de bir (faz 2'de 1.1) DURDUĞUN YERE gecikmeli patlama bırakıyor. Faz 2'de çağırdığı MÜBAŞİR'ler de bırakıyor. Rengi İPOTEK'inkiyle neredeyse aynı mor. Yorum "karışmasın diye kendi renginde" diyor, sayılar aynı. Enes bu saldırıyı hiç fark etmemiş: renk yüzünden kendi ipoteği sanmış.
  *Düzeltme (Enes onayladı):* düşmanın bıraktığı işaretler tehlike rengine (turuncu-kırmızı), oyuncunun ipoteği mor kalır.
  *Kapı:* iki rengin farkı ölçülüyor (ton farkı eşiğin üstünde), sayı değil GÖRÜNEN renk: `Arc`'ın gerçekten çizdiği renk okunuyor.
- [x] 🐛 **ZIRHLI'da zırh yok** (madde 2). `Enemy.TakeDamage` yorumu "ZIRH BURADA" diyor, kodda zırh yok. ZIRHLI yalnızca havuzu ×2. Yani ikisi de "daha çok can".
  *Düzeltme (senin kuralın):* ZIRHLI vuruş başına az hasar alır. Hasat hasarla orantılı, yani az zaman verir. Havuzu normale yakın. İRİ büyük havuz + pahalı vuruş.
  *Kapı:* aynı silahla ZIRHLI'ya vurunca havuz ve kazanç yarı, İRİ'nin vuruşu normalden pahalı.
- [x] **HACİZ geri dash kalkıyor** (madde 6). Faz 16'da benim tasarımımdı, playtest sorusu "kafa karıştırıyor mu" idi. Cevap evet. HACİZ de gittiğin yöne dash atar, dash-atak nişana vurur.
- [x] **SİMSAR faz 2** (madde 9). Hata değil ama kimse karar vermemiş: SİMSAR sonradan eklendi, TAHSİLDAR/EKSPER gibi "yarıda dön" varsayılanını aldı. *Öneri:* KEFİL ve MURABAHACI gibi havuz bitince fullenip dönsün. **Senin kararın.**

**Kararlar**
- [x] **K1 — oda başı kayıt.** Her oda girişinde saat + yükseltmeler + oda yazılır. Açılışta varsa oraya dönülür. ÇAĞRI yine checkpoint'e atar (ikisi ayrı kayıt). *Kapı:* oda ortasında kapat-aç → girişteki zaman; ölümden sonra açılış → ölü kalıyor.
- [x] **K2 — ÇAĞRI tabanı.** Checkpoint 4 sn olsa bile ÇAĞRI en az X sn ile döndürür (`recallMinSeconds`, ÖLÜM ALDATMA'daki taban gibi). *Kapı:* 4 sn'lik checkpoint → ÇAĞRI sonrası saat ≥ X.

**Test kolaylığı**
- [x] **TAM GÜÇ tuşu** — bütün yükseltmeler dolu + saat dolu. Yalnızca geliştirme build'i, koşuyu kirletir. "Teslimden önce kalkacak" listesine girer. Faz 3'ü ve uyku rengini test etmek için.

**Kurallar:** her yeni kapı bir kere bilerek bozulur, kırmızıya döndüğü gösterilir. Kapılar batch'ten. Commit Enes isteyince.

### Sonuç
- **Kapı:** `Faz25SelfCheck` — **35/35.** Oyun akışını adım adım koşturuyor (boss yürüyor, oyuncu duruyor / dibe giriyor / kaçıyor).
- **Bozma turu:** 12 düzeltmenin 12'si bilerek geri kondu, ilgili 21 kontrolün **hepsi kırmızıya döndü**, başka hiçbir şey düşmedi. K1'in ölüm kontrolü ilk turda yeşil kaldı (oda başı kaydını yazmayan bozma onu da maskeliyordu); tek başına bozulunca kırmızı yandı.
- **Tüm kapılar: 22/22 yeşil — SONUÇ: TEMİZ** (2026-10-04, inceleme düzeltmeleri dahil). Faz 12 / 16 / 17 / 24'ün eski tasarımı ölçen kontrolleri yeni tasarıma çevrildi (geri dash, ZIRHLI'nın havuzu, eski seçim).
- **Kapının bulduğu:** boss tepki payı 0.3 sn'de 30 hızlı koninin 8'inden koşarak çıkılamıyordu (EKSPER SAYIM, MURABAHACI PROTESTO…). **0.45 sn**'ye çıktı. Kadran: `bossReactionWindow` / `enemyReactionWindow` (0.25).
- **Yeni ayar alanları** (asset'te yok, .cs geçerli): `enemyReactionWindow` 0.25 · `bossReactionWindow` 0.45 · `bossAttackPatience` 1.5 · `armoredDamageTaken` 0.5 · `hugeHitCostMultiplier` 1.6 · `recallMinSeconds` 20 · `armoredPoolMultiplier` 2 → **1**.
- ⚠️ **Bilinçli yan etki:** yakın sıralar ince. Çoğu fazda 2–3 yakın saldırı var (MURABAHACI faz 3'te 3: VADE++ ve iki güvenli nokta). Dibine yapışan oyuncu az çeşit görür. "Ekstra saldırılar" maddesinde yakın saldırı eklemek buna cevap.
- **İnceleme turu** (iki ajan: kod + kayıt açıkları). Bulunan ve düzeltilenler, her biri kapıda ve bilerek bozulunca kırmızı:
  - 🐛 **H1 (yüksek):** ONARIM + kapat-aç = bedava onarım → sonsuz diriliş geri gelmişti. Onarım diske hemen gidiyor ama bedeli oda başı kaydından düşmüyordu. Artık düşüyor.
  - 🐛 **M1:** GEÇİCİ DÖNÜŞ sayarken kapatınca 10 sn ile ve sayaçsız odanın başına düşüyordun, sıradaki ölüm yeni eşya yakıyordu. Artık kapatmak = başaramamak (checkpoint); kazanınca oda başı kaydı geri yazılıyor.
  - 🐛 **L1:** dirildikten sonra checkpoint odasında kapat-aç checkpoint'i kalıcı düşürüyordu. Aynı checkpoint odasına dönüş artık checkpoint'i ezmiyor. (Faz 9'un bir testi bu durumu "ilk giriş" sanıyordu, gerçek akışa çevrildi.)
  - 0 sn'lik oda başı kaydı tam saatle başlatabilirdi → kapalı.
  - Veri tuzağı kilitlendi: yakın sıradaki bir saldırının erişimi boss'un durduğu mesafeden kısa olursa sessizce ölür. Bugün yok (en kısa KEFALET 0.95), kapı artık yakalıyor.
  - Yalan söyleyen 6 yorum düzeltildi (hazırlık, açıklık, zincir beklemesi, menzil şartı, zorluk raporu).
- ⚠️ **Zorluk değişti:** tepki payı bütün düşman hazırlıklarını uzattı (sıradan 0.2 → 0.45 sn, boss çarpanlıları ~0.1–0.25 → ~0.55–0.7 sn). Kolaylaştıysa kadranı düşür; kapı hangi saldırının kaçılmaz olduğunu söyler.

## ✅ FAZ 25b — OTOPİLOT — **KOD BİTTİ, İZLEME OTURUMU BEKLİYOR** (2026-10-09)

Enes: *"bilgisayarımı kontrol edip bu testleri sen yapar mısın, direkt Unity üzerinden."*
`Otopilot.cs` oyunu **gerçek Play modunda** oynuyor. Unity'nin sanal klavye ve faresiyle basıyor, `PlayerController` onu Enes'in klavyesi gibi okuyor. Enes'in kaydına dokunmuyor (geçici dosya; gerçek kaydın tarihi 28 Ağustos, değişmedi). Editörde derleniyor, build'e girmiyor.
Çalıştırma: `Tek Can → Otopilot (izle)` ya da batch `…OtopilotMenu.Batch` (~12 dk). Rapor: `Logs/Otopilot/<zaman>/rapor.md`.

**9 sahne, son koşu (2026-10-09): hepsi ✅, çıkış kodu 0**
| Sahne | Ölçüm |
|---|---|
| 1 Girdi | 0.5 sn W → 3.3 birim · dash → 2.5 · sol tık → savurma |
| 2 K1 | oda 2'ye 100.7 sn ile girdi, 99.2'de kapattı → açılışta oda 2, 100.5 sn |
| 3 ZIRHLI | vuruş başına 1.78, normal 3.56 (tam yarı) |
| 4 Sunak | saat duruyor (110.4 → 110.4) · 6 alım, üç değer de değişti, fiyat arttı |
| 5 TAHSİLDAR | uzakta İHBARNAME → KUŞATMA → ZORLA ALIM → YAKA PAÇA · dibinde HACİZ ↔ TAHSİL · hepsi oyuncu menzildeyken başladı |
| 5 Nemesis | bot bilerek öldü: defter 234 sn · boss havuzu 200 → 317, gövdesi 1.71 → 2.15 |
| 6 HACİZ | dash gittiği yöne, 2.5 birim |
| 7 SİMSAR | işaret 16° (turuncu-kırmızı) · faz 2 havuz %0'da, fullenerek · açıklıklı 30 saldırının 26'sından boşluğa koşarak kaçtı |
| 8 Final | F7 ile faz 3 → rakipler → uyandı → **KAZANDI** · uyurken renderer gerçekten soluk (0.29 / 0.38) · 4 halkalı zincir tam |

- **Ders 14'ün açık maddesi kapandı:** uyku rengi artık gerçek renderer'dan ölçülüyor.
- **Bozma:** 10 hata birlikte geri kondu, 10 kontrol kırmızıya döndü (+ dash'siz koşu girdi sahnesinde durdu). Çıkış kodu 1.
- **İnceleme ajanı:** gerçek kayda dokunma yolu (bitişte yönlendirme bir kare erken kalkıyordu), derleme hatasında bayrağın sızması, dash'i ölçemeyen kontrol, istisnada 30 dk asılma. Hepsi düzeltildi.
- Botun ilk iki kırmızısı **botun kendi ölçüm hatasıydı** (K1'de saati beklerken ölçtü, uyku renginde parlama sönmeden baktı). Ders 15 yan derslerinde.

### ✅ Kapandı — MURABAHACI'nın dibinde harita saldırıları → Faz 26 (üç sıra) çözüyor
> **Otopilot verisi (gerçek oyun, faz 3):** dibinde dövüşen bota gelenler **VADE++, TAM TASFİYE, İHTARNAME++** — üç saldırı, ikisi harita boyu. Uzakta duran bota gelenler 9 menzilli saldırının **9'u** (4 halkalı zincir dahil). Yani yakın oyuncu finalin çoğunu hiç görmüyor.

Harita boyu güvenli noktalı saldırılar (SÖZLEŞME, İHTARNAME, TAM TASFİYE) iki sırada da var. Yoksa dibine yapışınca finalin imza hamlesi hiç gelmezdi. Ama MURABAHACI'nın gerçek yakın saldırısı az olduğu için yakın sırasının yarısı bunlar oluyor: faz 1'de 4'ün 2'si, **faz 3'te 3'ün 2'si.** Dibine yapışan oyuncu "hep alan saldırısı" görür, SİMSAR'daki şikâyetin aynısı.
- **a)** Böyle kalsın, playtest'te bak.
- **b)** Harita saldırıları yalnızca menzilli sırada olsun. Dibine yapışan bunları hiç görmez, faz 3'te tek yakın saldırı (VADE++) döner.
- **c)** MURABAHACI'ya yakın saldırı ekle (faz 3'e en az 2). "Ekstra saldırılar" maddesinin ilk işi bu olur.
- *Önerim:* **a**, sonra **c**. Önce oyna. Fazla geliyorsa c ile çözülür, b finalin imzasını dibinde siler.

### 🎮 Playtest — Faz 25
> Önce **F6**. K1 testinde **test tuşu kullanma** (F5/F7/B/N koşuyu kirletir, kirli koşu diske yazılmaz).

1. **Boss'un sırası.** B ile TAHSİLDAR'a git. Uzakta dur: İHBARNAME → KUŞATMA → ZORLA ALIM → YAKA PAÇA sırayla gelmeli. Dibine gir: HACİZ ↔ TAHSİL. *Bak:* çeşit artık hissediliyor mu?
2. **Tepki payı.** Uyarı yayı vuruştan hemen önce DONUYOR (boss'ta ~0.45 sn, sıradanda 0.25). *Bak:* yay donunca kaçabiliyor musun? Oyun fazla mı kolaylaştı?
3. **SİMSAR'ın halkası** (N×3, B). Yeşil boşluk artık sen koşarken dönmüyor. Boss'un içinden geçip arkasına geç. *Bak:* kaçılabiliyor mu, hâlâ haksız mı?
4. **SİMSAR'ın işareti.** Durduğun yere bıraktığı halkalar artık **turuncu-kırmızı**, İPOTEK'inki mor. *Bak:* ayırt ediliyor mu? Bu saldırıyı artık fark ediyor musun?
5. **SİMSAR faz 2** canı BİTİNCE geliyor, dolup başlıyor.
6. **ZIRHLI / İRİ.** ZIRHLI'ya vurunca az eriyor, az zaman veriyor. İRİ çok acıtıyor. *Bak:* artık ayırt ediliyor mu?
7. **HACİZ dash'i** gittiğin yöne.
8. **K1.** Temiz koşuda bir odaya gir, biraz dövüş, zaman kaybet. Play'i durdur, tekrar bas. *Bak:* aynı odanın başında, GİRDİĞİN zamanla mısın?
9. **F7 TAM GÜÇ → final.** N×4, B, **F7**. Faz 3'e kadar git. *Bak:* 4 halkalı zincir sonuna kadar oynuyor mu? Uyurken soluk mu? Rakipler hemen ölmüyor mu? (Faz 24'ten kalan madde 10.)
10. *(Faz 24'ten açık)* **SİMSAR'ın DEVİR'i** vurup geri sıçrıyor — adil mi, sert mi?

---

## ✅ FAZ 26 — Üç sıra: yakın / uzak / tüm harita — **KOD BİTTİ, PLAYTEST BEKLİYOR** (2026-10-10)

Enes: *"son boss fighta baktım, 2 saldırı atıyor, biz ona bayağı saldırı yazdık. Tüm bosslar için yakın menzil, uzak menzil ve tüm harita diye 3 çeşit olsun."* · *"4 olsun, başla."* · *"sürekli yakından veya sürekli uzaktan saldıranlar için belli bir eşikten sonra peşpeşe yapılan saldırıların hasarı artsın."* · *"önceki fazlardaki saldırılar sonraki faza taşınmıyor, onu da hallet."*

**Ne yapıldı**
- Her saldırının türü veride (`Tür`: Yakın / Uzak / Harita), menzilden tahmin edilmiyor. Mesafe yakın/uzak sırasını seçiyor. **Her 4. saldırı harita sırasından** (`bossMapAttackEvery` = 4).
- Harita saldırısı yalnızca **cevabına yetişilebiliyorsa** yapılıyor. Yetişilemiyorsa sıra bekliyor, boss yaklaşıyor. Sen kaçmaya devam edersen ~1.5 sn sonra normal saldırısını yapıyor, harita sırası beklemeye devam ediyor.
- **ISRAR:** aynı sıradan art arda 3 saldırıdan sonra bedel BİNEREK artıyor: ×1.25, 1.56, 1.95, 2.44, 3.05, 3.81, 4.77, **5** (tavan). Enes (2026-10-10): *"x2 değil, 1.3 1.5 1.8 gibi artarak, x5'e kadar."* Mesafe ya da faz değişince sıfır. Harita saldırıları ne sayar ne sıfırlar. Uyarı yayı koyulaşıyor, HUD'da "ISRAR ×1.50 — mesafeni değiştir" (KEFİL ikiye bölününce ikisinin yükseği). Kadranlar: `bossStreakThreshold` 3 · `bossStreakStep` 0.25 (her adımda ×1.25) · `bossStreakCap` 5. Uyarı yayı ilk adımda bile görünür koyulaşıyor, her adımda biraz daha. Israr saldırı başına bir kez belirleniyor, tekrarlı saldırının **her vuruşunda** geçerli (Enes kararı, 2026-10-10).
- **Taşınma:** her fazın her saldırısının sonraki fazda devamı var ("+" hâli ya da `Önceki` ile bağlı). MURABAHACI faz 3'e FAİZ++, İBRAZ++, MUACCELİYET+, PROTESTO++ eklendi. REZERV ← ÇİFT EKSPERTİZ, TAM TASFİYE ← SÖZLEŞME.

**Sayılar (yakın / uzak / harita)**
| Boss | Faz 1 | Faz 2 | Faz 3 |
|---|---|---|---|
| TAHSİLDAR | 3/3/1 | 3/5/1 | |
| EKSPER | 3/3/1 | 4/4/2 | |
| KEFİL | 5/2/1 | 6/2/2 | |
| SİMSAR | 4/2/1 | 5/2/2 | |
| MURABAHACI | 3/2/2 | 4/3/2 | 7/5/2 |

**Harita saldırıları** (her biri kendi imzasının arena boyu hâli)
- **TAHSİLDAR — TAM TAHSİL(+):** arenanın ORTASINA sıçrıyor, halka arenanın %65'i (+ hâlinde %72). Cevap: kenara koş. *Planda "dalga" vardı (halka → simit zinciri). Zincir MURABAHACI'nın tekeli, düşürüldü. İlk yazılan hâli boss'un durduğu yerde dev halkaydı. Duvar dibinden kaçılamıyordu, nemesis büyümesiyle de arenadan büyük oluyordu. Ortaya sıçrayınca kaçış yolu her yerden en fazla halkanın yarıçapı kadar.*
- **EKSPER — TAM DEĞERLEME(+), BÜYÜK SAĞANAK:** arena boyu ayna şeritleri, beş dar ışın. Cevap: yana çık.
- **KEFİL — TOPLU TEMİNAT(+), TOPLU REHİN:** arena boyu simit. Cevap: ona koş. 11–12 birimden uzaktaysan yapılmıyor.
- **SİMSAR — BÜYÜK İHALE(+), BÜYÜK MÜZAYEDE:** arenanın ORTASINA sıçrıyor, halka arenanın tamamı, tek çıkış arkasındaki kapı. Hazırlık 2.3 sn: duvar dibinden ortanın arkasına yetişilsin. İHALE+ 3 inişten 2'ye indi. *İlk hâli boss'un yerinde patlıyordu ve "boss duvara yakınsa yapma" şartı vardı: bot playtest'inde 16 saldırıda yalnızca 1 harita geldi. Ortaya sıçrayınca 16'da 4.*
- **MURABAHACI — SÖZLEŞME, İHTARNAME(+, ++), TAM TASFİYE:** güvenli nokta artık senin yetişebileceğin yerlerden seçiliyor. Önceden ~üçte biri yetişilemezdi.

**Kapı — `Faz26SelfCheck` 19/19 · tüm kapılar 23/23 TEMİZ**
- Veri: sayılar, tür–şekil uyumu, taşınma.
- Akış: dibinde ve uzakta 12'şer saldırı, her 4.'sü harita. Uzaktaki oyuncuya simit/açıklık sırası gelince boss yaklaşıyor, araya başka saldırı girmiyor.
- **Kapsama (Enes'in sorusu):** her fazın yazılı HER saldırısı dövüşte yapılıyor.
- **Cevap taraması:** her harita saldırısı, boss 3 konumda × oyuncu 7 konumda (duvara sıkışmış dahil) × gövde 1 ve ×1.6 (nemesis). Uyarıdan 0.25 sn sonra en yakın güvenli yere koşan vurulmuyor: **961/961** (SİMSAR'ın ortaya sıçraması kaldırılınca 30 konumda kırmızı).
- Israr: eşiğe kadar ×1, ilk adım ~1.3, her artış bir öncekinden büyük, tavan tam ×5; sıfırlanma (mesafe + faz), bedel, renk (ilk adımda bile görünür), HUD. Bozma: düz artış, tavansız eğri, eski renk → üçü de kırmızı.
- Uyuyan boss'un etiketi güncel.
- **Bozma:** 18 hata bilerek geri kondu (ilk 10 + inceleme düzeltmelerinin 8'i), hepsi kırmızı. Biri öbürünü maskeliyordu (erişim şartı yokken "sıra tutma" ölçülemiyor), ayrı koşuyla gösterildi.
- **İnceleme turu buldu:** ilk kapı kolay konumları ölçüyordu (boss sabit, oyuncu 3/7 birim önde, nemesis yok). TAM TAHSİL, uzaktan simit/açıklık ve rastgele nokta orada yeşil geçiyordu. Faz 12'nin "aynı soru" imzasındaki `tür=` dört kopyayı saklıyordu → erişim basamağına (kısa / uzun / harita) çevrildi. Sertlik merdiveni harita saldırılarını ayrı ölçüyor, TAM TAHSİL'in boyunu `ArenaOranı`'ndan okuyor (bozma: TAM TAHSİL+'nın halkası küçülünce kırmızı).

**Otopilot (gerçek oyun, 2026-10-10 17:19): hepsi ✅, çıkış kodu 0**
- TAHSİLDAR uzakta: İHBARNAME → KUŞATMA → ZORLA ALIM → **TAM TAHSİL** → HACİZ → YAKA PAÇA → İHBARNAME → **TAM TAHSİL** (her 4.'sü harita). KUŞATMA ve HACİZ bot duvara sıkışınca geldi, doğru davranış. Dibinde ısrar ×1.50'ye çıktı.
- SİMSAR: 20 açıklıklı saldırının 15'inden kaçtı. Final KAZANILDI, 4 halkalı zincir tam (artık dibinde geliyor).
- Kapsama 47/82: bot her mesafede durmuyor, EKSPER/KEFİL'i yalnızca finalde rakip olarak görüyor. Bilgi amaçlı. Asıl kanıt kapıdaki kapsama kontrolü.
- Önceki koşunun iki kırmızısı botun kendi varsayımıydı (uzakta = hep uzak; zincir uzak sırasında). İkisi de düzeltildi.

### 🤖 Bot playtest'i — Faz 26 (2026-10-11, `Tek Can → Otopilot — Faz 26 playtest`)
Enes: *"playtesti de bot ile yap, sorun çıkarsa düzelt."* Son koşu: hepsi ✅, final KAZANILDI.
| Madde | Ölçüm |
|---|---|
| 1 Her 4. saldırı | TAHSİLDAR 24 saldırı (12 uzakta + 12 dibinde): her 4.'sü TAM TAHSİL |
| 2 TAM TAHSİL | 13 hazırlığın 13'ünde boss ortada · bot 4'te 1 vuruldu |
| 3 Israr | sıfırdan **15. saldırıda, 42 sn'de ×5** · gerçek dövüşte eğri: 1, 1, 1, 1.25, 1.56 … 5 · HUD "ISRAR ×5,00" · uzaklaşınca ×1 · **×5'te tek vuruş 48 sn** (TAHSİLDAR) |
| 4 KEFİL | hep kaçan bota 35 sn'de 6 saldırı geldi, simit dahil · 4 harita saldırısının hepsi erişimdeyken |
| 5 SİMSAR | 16 saldırıda 4 harita (her 4.'sü), 4'ünde de boss ortada · bot İHALE'de 6'da 0 vuruldu |
| 6 MURABAHACI | faz 3 dibinde 7 yakın saldırının **7'si** geldi · yeşil nokta 7 inişin 7'sinde yetişilebilir |
| 7 Haksız aday | bota dönük 146 boss saldırısının 5'inde vuruldu (%3). Öne çıkan yok (en yüksek KUŞATMA 2/4) |

**Playtest'in bulduğu ve düzeltilenler**
- 🐛 **SİMSAR'ın harita saldırısı neredeyse hiç gelmiyordu** (16'da 1) → ortaya sıçrıyor (yukarıda).
- Botun kendi hataları (oyunun değil): ısrarı önceki seriden devam ederken sıfırdan saydı · "vurulma"ya hazırlık boyunca başka yerden yenen darbeleri yazıyordu (kalabalıkta KOMİSYON+ 14/14 görünüyordu, gerçekte 0/4) · dev halkada kenarda girip çıkıyordu, ortaya sıçrayan halkada vuruluyordu.
- 📊 **Faz 27'ye veri:** sunaktan hiç almamış bot KEFİL'i 5 dk'da öldüremedi.

### 🎮 Playtest — Faz 26 (insan için, isteğe bağlı)
> Önce **F6**.
1. **Her 4. saldırı.** B ile TAHSİLDAR. Yakında ya da uzakta dur, say: 3 normal, 1 harita. *Bak:* harita saldırısı ayırt ediliyor mu?
2. **TAM TAHSİL.** Boss ortaya sıçrayıp dev halka basıyor, kenara koş. *Bak:* yetişiyor musun? Sıçrama okunuyor mu (animasyon yok, ışınlanıyor)?
3. **ISRAR.** Boss'un dibinde kal. 4. yakın saldırıdan sonra uyarı yayı koyulaşmalı, HUD'da "ISRAR ×1.25" yazmalı, her saldırıda büyümeli (×5'e kadar). Uzaklaş: sıfırlanmalı. *Bak:* fark ediliyor mu, ×5 çok mu sert, ne kadar sürede ×5'e çıkıyor?
4. **KEFİL — TOPLU TEMİNAT** (N, B). Uzaktaysan boss önce yaklaşıyor, sonra mavi simit: dibine koş.
5. **SİMSAR — BÜYÜK İHALE** (N×3, B). Boss ortaya sıçrıyor, ortanın arkasındaki kapıya geç. *Bak:* sıçrama okunuyor mu, 2.3 sn yetiyor mu?
6. **MURABAHACI** (N×4, B, F7). Yeşil nokta hep yetişebileceğin yerde mi? Faz 3'te dibinde dururken 7 farklı yakın saldırı görmelisin.
7. **Kapsama.** "Bu saldırıyı ilk defa görüyorum" dediğin oldu mu? Hangisi sıkıcı ya da haksız?

**İzleme oturumundan kalan küçükler** — hepsi yapıldı
- [x] Uyuyan boss'un süre etiketi donuyordu → uykuda da güncelleniyor
- [x] Otopilot K1 payı → "kapanıştan değil girişten yakın mı"
- [x] Otopilot üst yazısı → sol üst

---

## ⏱️ BOSS SÜRELERİ + Faz 33 bot testi — **BİTTİ, commit'siz** (2026-10-20)

Enes: *"testi bot ile yap"* · *"her bir boss öncekinden daha uzun sürmeli"* · *"bot düzeltmesini yap önce"*.

- ✅ **Bot düzeltmesi** (#12) uygulandı: SİMSAR'ın işaretinden çıkarken boss'u menzilde tutan nokta.
- ✅ **`…OtopilotMenu.BossTesti`** (yeni, ~2.5 dk): 5 ana boss, her biri yeni koşuda, beklenen güçle, saat bölgenin doyma tavanında. Oyun zamanında:

  | boss | önce | sonra |
  |---|---|---|
  | 1 TAHSİLDAR | 3.8 dk | 3.8 dk |
  | 2 EKSPER | 3.6 dk ⚠️ öncekinden kısa | **4.5 dk** (havuz ×1.25) |
  | 3 KEFİL | 5.3 dk | 5.7 dk |
  | 4 SİMSAR | 15.4 dk | 15.4 dk |
  | 5 MURABAHACI | 40 dk'da bitmedi (sınır) | **39.9 dk** (sınır 60 dk) |

  MURABAHACI ölmez değil: üç fazın her biri havuzu baştan dolduruyor (7972 sn), faz 2'de havuz geri sarıyor — faz 1 7.3. dk, faz 2 21.5. dk, faz 3 ~40. dk'da bitiyor.
  İlk koşuda bot MURABAHACI'da 4 dk'da üç kez öldü: F5'in 300 sn'si 5. bölgede bir-iki vuruş (vuruş ~100–200 sn). Ölçüm kurulumu düzeltildi (saat = bölgenin doyma tavanı, MiniTesti gibi), oyun değil.
- 🐞 **Bulunan hata — tek E iki alım:** her sunak E'yi kendisi dinliyordu. Faz 33'ün dış halkasının 270°'deki sunağı combo "+" sunağının **0.4 birim** yanına düştü; bot bir basışta SÜRÜ'yü ve TEMERRÜT+'yı birden aldı. Sunak odasında da mini sunaklar 2.4 birim arayla — ortada üçü birden menzildeydi. HUD ilk sunağı gösteriyordu, en yakını değil.
  **Düzeltme:** menzilde yalnızca EN YAKIN sunak (`Altar.EnYakını`) — yazan = alınan; combo sunağı 0.45R → 0.75R. Kapı: Faz 33 "tek E = tek alım" (sunak odası + boss odası), iki bozmanın ikisi kırmızı. Eski P30-6 bu hatanın SAYESİNDE yeşildi (combo sunağına basınca yanındaki dış halka sunağı da alınıyordu).
- 🤖 **Bot yolu:** boss'u kapının kuzeyinde bitiren bot sunağa yürürken açık çıkış kapısının üstünden geçip 2. bölgeye düşüyordu (P30-6 ara sıra kırmızı). `Git` artık kapıyı dolanıyor; botu kapının kuzeyine koyup denendi: düzeltmeyle ✅, düzeltmesiz ❌.
- ✅ **Faz 33 testi botla** (#13): P33'e 3. sahne — boss'un dış halkasından combo AÇANI al → "COMBO AÇILDI" → combo gerçek tuşlarla. **P33 3/3 · P33c 12/12.**
- **Son tur:** tüm kapılar ✅ (Faz 33: 87 kontrol) · P29 8/8 · P30 6/6 · P31 ✅ · P33 3/3 · P33c 12/12 · BossTesti ✅.
  ⚠️ **P32 6/8 — iki kırmızı ESKİ** (2026-10-18 raporlarında da kırmızı): 32-5 SİMSAR P32'nin yolunda 30 dk'da ölmüyor
  (oysa `SimsarTeshis`'in aynı yolunda 15.3 dk'da, `BossTesti`'nde 15.4 dk'da ölüyor → P32'nin önceki adımlarından kalan
  bir bot durumu) · 32-7 bot keşif odasını temizleyince sonraki iki odaya geçti, köşeyi kaçırdı (muhtemelen son düşmanı
  çıkış kapısının üstünde öldürdü — kapı altında açılınca hemen geçiriyor). Ayrı iş.

---

## 🧩 FAZ 33 — Silah başına 8 yükseltme + combo AÇAN yükseltmeler — **33a (6343c60) · 33b (4c9f704) · A + 33c (efc0b6e) commit'li · 33c "+" hâlleri BİTTİ, commit'siz, Enes'in testini bekliyor** (2026-10-19)

**✅ Enes'in cevapları (2026-10-18):**
1. **ENDERLİK** — 4 tip: **YAYGIN · NADİR · EPİK · EFSANEVİ**. Mini boss'tan **1** yükseltme (yaygın ya da nadir). Ana boss'tan **1 yaygın/nadir + 1 epik/efsanevi**. Ayrıca **şansa bağlı**, enderlik bakılmaksızın sunaklarda da satılabilsin. ("bundan kaynaklı biraz daha güçlendirme eklenebilir")
2. **Üst enderlikteki yükseltmelerin combo'ları da ekstra güçlü** olmalı (33c).
3. Sunakta **rastgele 3**.
4. Liste uygun, **33a'yla başla**.

**33a yapı kararları (benim, Enes beğenmezse değişir):**
- Silah başına 8 = **3 yaygın · 2 nadir · 2 epik (biri combo açan, 33c) · 1 efsanevi**. Mevcut 36 yükseltme de bu dağılıma yerleştirildi (aşağıdaki tablo).
- Satış: sunak odasında (mini boss sonrası) 3 YAYGIN/NADİR sunak, **1 alım**. Boss odasında 3 YAYGIN/NADİR + 3 EPİK/EFSANEVİ, **her sıradan 1 alım**. **ŞANS SUNAĞI:** sunak odalarında %30 ihtimalle bir sunak daha — herhangi bir enderlik, fiyat ×1.5, 1 alım.
- "Rastgele 3" ODA ve SİLAH başına sabit: silah değiştirip yeni zar atılamaz (istismar kapısı).
- Fiyat enderlikle artıyor: taban = saatin %15'i (en az 24 sn) × **yaygın 0.6 · nadir 1 · epik 1.5 · efsanevi 2.2** (dört kadran `Tuning`'de; ilk taslaktaki ×1/×1.5/×2.5/×4 uygulamada yumuşatıldı). Yasa: saniyeyle ödenir.
- Yeni yükseltmeler sıranın SONUNA eklenir: eski 36'nın numarası değişmez, kayıtlar bozulmaz.

**33a adımları (2026-10-18)**
- [x] Yapı: `Enderlik` + `SunakGrubu`; 24 yeni yükseltme sıranın sonunda (kayıt uyumu); "rastgele 3" teklif ağırlıklı (yaygın 3 · nadir 2 · epik 2 · efsanevi 1), tohum koşu + sunak + silah; sunak teklifi silah başına önbellekte (alım yapınca kalanlar kaymıyor); grup başına 1 alım kayıtta (`upgradeGroupBought`); fiyat `Clock.SilahFiyatı(t, saat, enderlik)` — alım sayısıyla büyüme kalktı.
- [x] Sunaklar: sunak odasında 3 mini (solda) + %30 şans sunağı (sağda); boss odasında 3 alt (iç halka) + 3 üst (dış halka).
- [x] 24 yükseltmenin etkisi (KONİ · FAİZLİ · HACİZ · İPOTEK · İFLAS · REHİN). Düşmana etkiler `Enemy`'de (YORGUNLUK hazırlık ×1.3, İHTAR yavaşlık, TEHDİT saldırı başlatmama); İPOTEK işaretinin yükseltmeleri tek yerden (`İpotekYükselt`: normal ipotek + YAĞMUR damlası); sıçrama tek yerden (`Sıçrat`: SENET ZİNCİRİ, SIÇRAMA, DOMİNO).
- [x] Kapılar: `Faz33SelfCheck` **35 kontrol** · **39 bozmanın hepsi kırmızı** (5 koşu; çift korunan iki kontrol — teklif önbelleği + tohum — ayrı koşularda; öz-incelemeden: şans fiyatı ×1.5 ve her mini boss'un sunağı ayrı sayılıyor) · tüm kapılar ✅. Faz 29 kapısı yeni sunak kuralına (3 + 3, her sıradan 1), Faz 31/32'nin "36 yükseltme" sayısı tablo tutarlılığına çevrildi; Faz 11 için enderlik fiyatı dizi değil dört kadran.
- [x] Bot playtest'i (`Playtest33`) **2/2**: mini boss sunağında KONİ'nin 3 yaygın/nadir yükseltmesi (+ şans sunağı çıktı), [E] ile 1 alım, ikincisi kapalı · boss'ta 3 + 3, alınan HALKA bir daha teklif edilmedi, her sıradan 1 alım, sonra hepsi kapalı.
- Eski `Playtest29` botunun "boss'ta 3 sunak" beklentisi 3 + 3'e çevrildi (alım adımı yeni kuralda da geçiyor: her sıradan 1).
- ⚠️ Geçici boşluk: her silahta şimdilik 2 epik/efsanevi var (8.'si combo açan, 33c) → boss'un üst sırasının 3. sunağı "alınacak yükseltme kalmadı" diyor. 33b'den önce 33b silahlarında üst sıra tamamen boştu — artık 2 dolu. 33c 3.'yü dolduruyor.

**🤖 Fiyat ölçümü — bot (Enes 2026-10-18: *"fiyat uygunluğunu bot üzerinden deneyelim"*)**
- Yöntem (`…OtopilotMenu.FiyatOlcum` / `…FiyatOlcum33b`, hızlandırılmış ~2.5 dk her biri): her yükseltme aynı iki dövüş odasında (1. bölge, oda 1–2) VAR ve YOK diye oynandı, saat her odada doyma tavanından (250) başladı, zar her odada sabit. **Değer** = oda başı net saat farkı (hasat + darbe + akış, hepsi saniye). **Geri ödeme** = fiyat ÷ değer, oda. Rapor: `Logs/Otopilot/2026-10-19_00-04/rapor.md` (33a) · `…/2026-10-19_00-00/rapor.md` (33b).
- Saat 250'de fiyatlar: yaygın 23 · nadir 38 · epik 56 · efsanevi 83 sn (saatin %9 · 15 · 22 · 33'ü); şans sunağında ×1.5. 1. bölgede 35 dövüş odası var → birkaç odada kendini ödeyen yükseltme açık kâr.
- Gürültü artık çoğu silahta 0 (aynı deneme aynı sonucu veriyor); HACİZ ±14, REHİN ±2, ÇEK ±0.6, KARŞILIK ±1.

  | geri ödeme | ≤ 5 oda | 5–15 oda | ≥ 15 oda | bot ölçemiyor (≈0) |
  |---|---|---|---|---|
  | YAYGIN | DAR ÖLÜ BÖLGE 0.2 · İHTAR 0.4 · SERMAYE 2.5 · AĞIR REHİN 3.8 · KISA VADE 1.6 · TAHSİLAT 2.0 · HAREKETLİ 2.3 · HIZLI TAHSİL 2.4 · GENİŞ ARAZİ 2.6 · UCUZ KİRA 2.8 | HIZLI TAPU 9.7 · DİP 14.7 · DÖNÜŞ KESİNTİSİ 11.5 | GENİŞ ARSA 21 | GENİŞ AĞ · KISA SÜPÜRME · HALKA · KISA İCRA · TEMERRÜT · UZUN KOL · BOŞ ARSA · KAPANIŞ · SON KUYRUK · GENİŞ KOL · SIKI TUTUŞ · TEMYİZ · SAĞLAM YAPI … |
  | NADİR | İSKONTO 1.9 | BİLEŞİK 5.8 · GERİ TEPME 9.4 · FAİZ ORANI 9.6 · ÇEKİM 11 | DERİN TEMEL 17 · CESARET 25 · YÖNELİM 16 · ORTAK HASAT 25 · DEPOZİTO 37 · KALABALIK 110 | SON SÖZ · SERİ · KALKAN · SORGU · RED · ROLLOVER · ÇEKİŞME … |
  | EPİK | — | SIÇRAMA 6.8 · ÇİFT TAPU 9.9 · DOMİNO 15 · STOPAJ 10.6 | KURTARMA 37 · ÇİFT İŞLEM 68 | YORGUNLUK · UZUN VADE+ · KARŞILIKSIZ+ · BİRİKİM · KİLİT · GENİŞ ORTAKLIK |
  | EFSANEVİ | GERİ ÇEKİL 2.4 · FAİZSİZ KREDİ 4.2 | YANKI 11 · PROTESTO 9.5 | ZİNCİR TAPU 35 · İFLAS ZİNCİRİ 40 · ÇİFT KULE 55 | TEHDİT · KISMİ ÖDEME · VADE SONU · MÜTESELSİL · KISA POZİSYON |
  | rakip | — | — | ilk HASAT sunağı 29 | HASAR · DASH sunağı |

- **Sonuç:** fiyatlar ödenebilir; iyi yükseltmeler 1–10 odada kendini ödüyor. **Sorun fiyat değil güç sıralaması:** hızlı ödeyenlerin çoğu YAYGIN, NADİR ve EPİK ortada ya da geride, EFSANEVİ karışık. "Üst enderlik daha güçlü" sözü bugün tutmuyor. → Enes'in kararı **A** (2026-10-19), aşağıda "A — güç ayarı".
- Bot ne ölçemiyor: dash-atak yapmıyor, saat 250'de "dip" yok, karşılamayı zamanlamıyor, arkaya geçmiyor, bağ kurmayı planlamıyor → KARŞILIK, MÜŞTEREK, ARBİTRAJ ve İFLAS'ın dip yükseltmeleri burada ≈0. Bu, işe yaramadıkları demek değil — kapı (`Faz33SelfCheck`) her birinin etkisini ayrı ölçüyor. HACİZ'in sayıları botun ölü bölgede oynamasından şişkin.
- Yan bulgu: yükseltmesiz oda kazancı KONİ +17 · FAİZLİ +38 · İFLAS +49 · REHİN +42 · MÜŞTEREK +30 · ARBİTRAJ +21 · ÇEK +11 · KİRA −1 · KARŞILIK −2 · İPOTEK −3 · TAHVİL −22 · HACİZ −58 sn. Silah dengesi ayrı bir konu.
- İlk ölçüm (gerçek hız, 2026-10-18 22:34, `…/2026-10-18_21-03`) aynı sonucu verdi; sayılar kare hızı farkından ötürü biraz farklı — karşılaştırma yalnızca aynı koşuda.

**🔗 33c — 12 combo AÇAN yükseltme + 12 combo (2026-10-19)**

Her silaha bir EPİK açan (adı açtığı combo'nun adı) → her silah artık **8 yükseltme: 3 yaygın · 2 nadir · 2 epik · 1 efsanevi**; boss'un dış halkasında artık 3 epik/efsanevi var (geçici boşluk kapandı). Açan alınmadan combo kapalı; alınınca "COMBO AÇILDI" yazıyor. Plan listesinden iki ad değişti (çakışma): TAHVİL → KONİ'nin **KUPON**'u → **TEMETTÜ** (VADE FARKI'nın "+"sı KUPON), MÜŞTEREK → İFLAS'ın **ORTAKLIK**'ı → **KONSORSİYUM** (MÜŞTEREK'in GENİŞ ORTAKLIK'ı var).

| combo | sıra | ne yapıyor |
|---|---|---|
| SÜRÜ | KONİ → MÜŞTEREK | KONİ'nin vurduğu herkes (en çok 4) tek bağa |
| TAKİP | FAİZLİ → ARBİTRAJ | vuruş arkadan sayılır, hedef 2 sn sırtı dönük kalır |
| ÖNCELİK | HACİZ → REHİN | HACİZ'in vurduğunu nerede olursa olsun çekip yakalar |
| TEMİNAT | İPOTEK → KİRA | kule işaretin yerine kurulur, ilk 4 sn kira yok |
| KONKORDATO | İFLAS → KARŞILIK | İFLAS vuruşunun yarısı birikene yazılır |
| KEFALET | REHİN → HACİZ | ölü bölge yok, ×2 |
| CİRO HATTI | ÇEK → İPOTEK | çekin yolu boyunca üç küçük işaret daha |
| MUKABELE | KARŞILIK → ÇEK | son biriken çeke biner, her isabette o kadar ek hasar |
| TEMETTÜ | TAHVİL → KONİ | TAHVİL'in kademesi başına +30° ve +%25 hasar |
| KONSORSİYUM | MÜŞTEREK → İFLAS | bağlıya inen hasar öbürlerine tam (%100) yayılır |
| SPEKÜLASYON | ARBİTRAJ (arkadan) → TAHVİL | basılı tutma 2. kademe dolu başlar |
| KİRA GELİRİ | kule kur → FAİZLİ | o kule durdukça FAİZLİ'nin savurma bedelini kule öder (savurma başı kulenin ömründen 1 sn) |

- ✅ **"+" hâlleri** (Enes 2026-10-19: *"evet"*) — kural aynı: "+" sayı büyütmüyor, yeni bir ETKİ ekliyor. Combo sunağı eskilerin "+"sı alındıktan sonra açılmış 33c combo'sununkini satıyor.

  | combo | "+" | yeni etki |
  |---|---|---|
  | SÜRÜ | AĞIL | bağlananlar bağın ortasına çekilir |
  | TAKİP | PUSU | sırtı dönükken yeni saldırı da başlatamaz |
  | ÖNCELİK | İHTİYATİ HACİZ | HACİZ'in sağ tık beklemesi sıfırlanır |
  | TEMİNAT | İPOTEKLİ KULE | kule kurulurken işaret hemen patlar (×1.5) |
  | KONKORDATO | ERTELEME | İFLAS'ın sağ tık (TASFİYE) beklemesi sıfırlanır |
  | KEFALET | AVAL | HACİZ vuruşu yanındaki düşmana da seker |
  | CİRO HATTI | VADESİZ | ÇEK o an eline döner — yeniden atabilirsin |
  | MUKABELE | TEDBİR | çekin vurduğu düşman 1 sn yeni saldırı başlatamaz |
  | TEMETTÜ | KÂR PAYI | o vuruşun hasadı ×2 |
  | KONSORSİYUM | YAPILANDIRMA | bağın süresi baştan başlar |
  | SPEKÜLASYON | VADELİ İŞLEM | o basılı tutmada saat iki kat akmaz |
  | KİRA GELİRİ | PEŞİN KİRA | savurmalar kulenin ömründen de yemiyor |

  Kapı: `Faz33SelfCheck` **85 kontrol** (her "+" açık ve kapalı) · 12 bozmanın 12'si kırmızı (Faz 33 toplamı **80**) · tüm kapılar ✅. Bot `Playtest33c` "+"lar açıkken de **12/12**, sıfır hata.
- [x] Kod: combo tanımında `Açan` (yükseltme), `Combo.Kullanılır` açansız kapalı. Koni vuruşuyla bitenler var olan bitiriş yolunda (`Bitirenler`); öbürleri kendi anında: ÖNCELİK REHİN'in yakalamasında, TEMİNAT kule kurulurken, CİRO HATTI ipotek konurken, MUKABELE çek atılırken, SPEKÜLASYON basılı tutma başlarken. Kule kurmak artık bir combo adımı. Zincir son adımın hedefini taşıyor (ÖNCELİK). KONSORSİYUM bağın yayılmasını yalnız o vuruş boyunca tam yapıyor (`MusterekBag.TamPay`).
- [x] Kapılar: `Faz33SelfCheck` **73 kontrol** (her combo açansız ve açanla, etkisi sayıyla) · 33c'nin **18 bozması kırmızı** (toplam 68) · Faz 31/32'nin "her combo'nun + hâli var" kontrolü "açanlılar hariç" oldu · tüm kapılar ✅. Bozma iki gevşek testi yakaladı (TEMETTÜ'nün açısı, combo sunağının "+"sızları satmaması) → keskinleşti.
- [x] Bot (`…OtopilotMenu.Playtest33c`, hızlandırılmış ~1 dk): **12/12 combo gerçek tuşlarla, hepsi ilk denemede.** İlk iki koşuda ÖNCELİK ve KEFALET olmadı — combo değil bot: sabit beklemeyle silahı erken değiştirip HACİZ'in ağır vuruşunu kesiyordu; HACİZ'i ölü bölgenin sınırına (3.2) atıyordu; fırlatılan rehineye menzil dışından nişan alıyordu. Bot artık adım zincire yazılınca geçiyor (`PlayerController.AdımSayısı`) ve HACİZ'i uzaktakine atıyor.

**⚖️ A — güç ayarı (Enes 2026-10-19: *"A okeydir"*)** — fiyatlar aynı; silah başına 3 · 2 · 1 · 1 aynı; bot fiyat ölçümüyle iki tur.
- Enderlik takası: FAİZLİ SERMAYE → nadir, FAİZ ORANI → yaygın, SIÇRAMA → efsanevi, FAİZSİZ KREDİ → epik · HACİZ DAR ÖLÜ BÖLGE → efsanevi, İHTAR → epik, GERİ ÇEKİL → nadir, SON SÖZ ve UZUN VADE+ → yaygın · REHİN AĞIR REHİN → nadir, KALKAN → yaygın · ÇEK HIZLI TAHSİL ve TAHSİLAT → nadir, ÇİFT CİRO ve YÖNELİM → yaygın · TAHVİL KISA VADE → nadir, ROLLOVER → yaygın, İSKONTO → epik, KİLİT → nadir · KİRA UCUZ KİRA ve GENİŞ ARAZİ → nadir, UZUN KONTRAT ve DEPOZİTO → yaygın.
- Güçlendirme: KALABALIK 2+ düşmanda ×1.3 · YORGUNLUK 3 sn ×1.8 · ZİNCİR TAPU vurduklarının yerine en çok 3 ikinci işaret, tam hasar · İFLAS ZİNCİRİ öldürdükçe seriye +1 (en çok +3) · KARŞILIKSIZ+ beklemesi −%30 ve havadaki çekler silahsız bırakmıyor · PROTESTO yakalama darbesi ×2 · STOPAJ +1.5 sn · ÇİFT KULE kule başı kira yarım. Kısma: TAHSİLAT 0.25 sn.
- Bedeli ödenip zarar ettirenler düzeldi (ders: "ücretli seçenek bedavadan zayıf olmasın"): UZUN KONTRAT ömür ×1.5 ama toplam kira aynı · ROLLOVER ilk dash'te ERKEN BOZDURMA'yı da vuruyor (önce yutuyordu) · KARŞILIKSIZ+ beş çek dönene kadar silahsız bırakıyordu.
- Sonuç (bot, `Logs/Otopilot/2026-10-19_17-31` + `…_17-35`): enderlik başına ortalama değer **33a: yaygın +0.4 · nadir +8.8 · epik +17.8 · efsanevi +25.3 sn/oda** (önce +8.5 · +2.6 · +2.6 · +10.5) · **33b: −1.5 · +3.5 · +3.9 · +5.2** (önce +1.9 · +0.6 · +1.4 · +2.3). Sıra artık her iki grupta yaygın < nadir < epik < efsanevi. 33b'nin ortalaması düşük çünkü KARŞILIK / MÜŞTEREK / ARBİTRAJ bot için ≈0 (onlara dokunulmadı — ölçemediğim şeyi ayarlamadım).
- Kalan: İPOTEK'te ZİNCİR TAPU (efsanevi +6.4) ≈ ÇİFT TAPU (epik +6.6); KONİ'de YORGUNLUK (epik +5.9) ≈ nadirler (+6.5–6.8); KARŞILIKSIZ+ (epik +2.7) ÇEK'in nadirlerinden zayıf. Fark ~1 sn/oda, insan testinde bakılsın.
- Kapılar: `Faz33SelfCheck` 60 kontrol (UZUN KONTRAT eklendi) · bu turun 16 bozması kırmızı (toplam 50) · tüm kapılar ✅.
- ⚠️ Ölçüm hatası bulundu ve düzeltildi: vurulma parlaması düşmanı %28 büyütüyor ve vuruş yarıçapı ölçekten (`Enemy.Radius`). Parlama hızlandırılmış modda gerçek saatle sönüyordu → düşmanlar ~30 kat uzun büyük kaldı, isabet kolaylaştı (KONİ +17 → +14, FAİZLİ +38 → +24). `HitStop.Adım`'a bağlandı; 2026-10-19 00:00/00:04 raporları bu yüzden biraz şişkin. Oyunun kendisinde de parlama vuruş alanını büyütüyordu → **düzeltildi (2026-10-19):** `Radius` artık `_baseScale`'den, parlama yalnızca görsel; kapı `Faz3SelfCheck` 8b (bozunca kırmızı).

**33b — ÇEK · KARŞILIK · TAHVİL · MÜŞTEREK · ARBİTRAJ · KİRA'nın 4'er yükseltmesi (2026-10-18)** — *enderlikler ilk hâli; A'dan sonraki hâli yukarıda ve `Weapon.cs` → `Ender`*

Her silaha yaygın · nadir · epik · efsanevi birer tane → her silah 3 yaygın · 2 nadir · 1 epik · 1 efsanevi (8.'si 33c). Plan listesinden üç ad değişti (çakışma): KARŞILIK'ın **YANKI** → **KISMİ ÖDEME** (KONİ'de YANKI var), **SABIR** → **BİRİKİM** (ara bölge sunağı SABIR), KİRA'nın **AİDAT TOPLAMA** → **STOPAJ** (ORTAK GİDER'in "+" hâli AİDAT).

| silah | YAYGIN | NADİR | EPİK | EFSANEVİ |
|---|---|---|---|---|
| **ÇEK** | TAHSİLAT — dönüşte vurduğu her düşman için +0.5 sn | YÖNELİM — sekince en yakın düşmana döner | KARŞILIKSIZ+ — üç yerine beş çek | PROTESTO — dönüşte vurduklarını yanına çeker, yakalayınca çevrene (2.5 birim) iner |
| **KARŞILIK** | TEMYİZ — İTİRAZ ×1.5 uzun | RED — karşıladığın saldırgan 1 sn yeni saldırı başlatamaz | BİRİKİM — biriken tutuldukça büyür, saniyede +%25, en çok ×2 | KISMİ ÖDEME — biriken inince yarısı kalır |
| **TAHVİL** | HAREKETLİ — basılı tutarken yürüyüş ×0.4 → ×0.8 | ROLLOVER — basılıyken ilk dash olgunluğu bozmaz | KİLİT — 3. kademede beklemek saat akıtmaz | VADE SONU — 3. kademe 360° |
| **MÜŞTEREK** | ZİNCİRLEME+ — dört yerine altı | ÇEKİŞME — bağlılar birbirine sürüklenir | GENİŞ ORTAKLIK — ikinci vuruş yanındakini de bağlar (üçlü bağ) | MÜTESELSİL — bağlı biri ölünce öbürleri havuzunun %10'unu kaybeder |
| **ARBİTRAJ** | SESSİZ ADIM — görünmezken yürüyüş ×1.3 | YAYILMA — arkadan vuruşun yarısı yanındakine | ÇİFT İŞLEM — TERS İŞLEM iki kez vurur | KISA POZİSYON — arkadan öldürünce AÇIK POZİSYON hemen hazır |
| **KİRA** | SAĞLAM YAPI — TAŞINMA kuleyi bir kez +3 sn uzatır | DEPOZİTO — kule süresi dolunca ödediğin kiranın %30'u geri | STOPAJ — kule öldürünce +1 sn | ÇİFT KULE — aynı anda iki kule, TAHLİYE ikisini birden |

- [x] Kod: tablo sıranın sonunda (kayıt uyumu: TAHSİLAT = 60). ÇEK'in yükseltmeleri fırlatma anında çeke yazılıyor (`CekUcusu`); bağın yükseltmeleri kuruluşta (`BağKur` — iki kuruluş yolu tek yerden); kuleninkiler kurulurken. ARBİTRAJ'ın YAYILMA / KISA POZİSYON'u `StrikeCone`'un sonunda: savurma, dash-atak, TERS İŞLEM ve AÇIĞA SATIŞ hepsi oradan geçiyor. RED düşmanda `Sustur` (saldırı başlatmama — TEHDİT ile aynı kapı).
- [x] İstismar/kenar: KISMİ ÖDEME 1 sn'nin altını siliyor (yoksa biriken hiç sıfırlanmaz, "biriken yoksa zayıf vuruş" kuralı sessizce kalkardı). BİRİKİM ilk karşılamadan sayıyor. MÜTESELSİL'de boss en çok ölenin havuzu kadar kaybediyor (çağrılmışlarla boss'u yüzde yüzde eritmek olmasın) ve boşalmış üye kayıptan ATLANIYOR — vurulan üye bağın yayılmasıyla önce öbürünü öldürüyor, o da geri dönüp vurulanı vurursa düşman iki kez ölüyordu (bozma C bunu gösterdi: kapı çöktü). Ölen üye bağdan anında çıkıyor. DEPOZİTO yalnız süre dolunca ve yalnız saatten ödenen kiradan. ÇEKİŞME itme değil sürükleme (vuruşun itmesini ezmesin).
- [x] Kapılar: `Faz33SelfCheck` **59 kontrol** (33a 35 + 33b 24) · **34 bozmanın hepsi kırmızı** (4 koşu: her kontrole bir; ikinci kurallar ayrı; MÜTESELSİL'in çift ölüm koruması ayrı; ÇEKİŞME boss muafiyeti ayrı) · tüm kapılar ✅. Kapı kurgusundaki bir sızıntı bulundu: sunak odası testi saati dondurup bırakıyordu, sonraki "saat akıyor mu" testleri yalan kırmızıydı — `YeniKoşu` donmayı sıfırlıyor.
- [x] Bot: `…OtopilotMenu.FiyatOlcum33b` (bot TAHVİL'i 3. kademeye kadar basılı tutuyor) — yeni yükseltmeler gerçek odada da işliyor: TAHSİLAT +11.4 · HAREKETLİ +9.8 · PROTESTO +8.7 · STOPAJ +5.3 · YÖNELİM +2.4 · ÇİFT KULE +1.5 sn/oda (odayı 26 → 16 sn'ye indiriyor, iki kira yiyor). KARŞILIK / MÜŞTEREK / ARBİTRAJ'ınkiler bot için ≈0 (yukarıdaki sınır). Tablo yukarıda.
- [x] ÇEKİŞME boss'u (mini dahil) sürüklemiyor — saldırı alanları boss'un yerine çiziliyor (öz-incelemeden; kapı + bozma D).

**⏩ Bot hızlandırma (Enes 2026-10-18: *"her bir test için botu sil baştan … çok uzun sürüyor, kısaltacak bir çözüm"*)**
- İzlenmeyen koşuda her kare sabit 1/60 sn oyun zamanı (`Time.captureDeltaTime`); botun 57 beklemesi/süre sınırı gerçek saatten OYUN saatine (`Şimdi`); bekçi gerçek saatte. İzleme modu gerçek hızda.
- Asıl darboğaz hit-stop'tu: donma gerçek saatle sayılıyordu, hızlandırınca her isabet gerçek 0.05–0.1 sn yiyordu. `HitStop` artık hızlandırılmış modda kareyle sayıyor; vuruş yayı (`Arc`) ve vurulma parlaması da aynı adımla (`HitStop.Adım`, tek yer). Gerçek oyunda değişen bir şey yok.
- Sonuç: 33b fiyat ölçümü **~100 dk → 2.3 dk (×30)**; 33a ölçümü 2.5 dk. Fiyat denemesinde zar her odada sabit → çoğu silahta aynı deneme aynı sonucu veriyor (gürültü 0).
- ⚠️ Eskiden bot oyunu ~3600 kare/sn görüyordu (insanüstü tepki), şimdi oyuncu gibi 60 — eski raporlarla sayı karşılaştırma. `-nographics` denendi, hız aynı (darboğaz çizim değildi).

> Enes (2026-10-17): *"her bir silahın en az 8 geliştirmesi olacak. Bazı geliştirmeler de normalde combo olmayan
> silahlar ile combo yapılabilmesini sağlayacak."* Bugün 12 silah × 3 = 36 yükseltme var. Öneri: silah başına
> **+4 yükseltme** (48) ve **+1 combo açan** (12) → silah başına 8, toplam 96. İlke (fikir defteri): silah
> göründüğü için değil KARARLARI değiştirdiği için özgün — yükseltme de sayı büyütmekten çok kararı değiştirsin.

**Bugünkü satış:** ana boss'tan sonra kuşandığın silahın yükseltmeleri silah sunağında, **boss başına 2 alım**.
Son boss oyunu bitirdiği için koşu başına **8 alım**. 96'lık havuzda 8 alım = her koşu farklı bir yapı (roguelike),
ama silah başına 8'in çoğu hiç görülmez. → Soru 1–2 aşağıda.

**Kayıt uyumu:** yeni yükseltmeler sıranın SONUNA ekleniyor (eski 36'nın numarası değişmiyor) — mevcut kayıtlar bozulmaz.

*Silah başına 4 yeni yükseltme + 1 combo açan (🔗):*
| Silah (sorusu) | 4 yeni yükseltme | 🔗 combo açan |
|---|---|---|
| **KONİ** (kaç kişiye birden) | KALABALIK (3+ düşmana değen savurmada hasat ×1.3) · YANKI (her 3. savurma yarım hasarla bir kez daha iner) · HALKA (dash-atak 360°) · YORGUNLUK (vurduklarının saldırı hazırlığı 1 sn ×1.3 uzar) | **SÜRÜ** — KONİ → MÜŞTEREK: MÜŞTEREK'in vuruşu KONİ'nin vurduğu herkesi (en çok 4) tek zincire takar |
| **FAİZLİ** (bu savurma tutar mı) | SERMAYE (saat doyma tavanının üstündeyken bedel yarı) · SIÇRAMA (öldürünce yanındakine bedel kadar hasar) · KREDİ (bedeli ödeyemezken de savurur, borç sonraki hasattan düşer) · FAİZ ORANI (öldürme iadesi hasadı da ×1.3) | **TAKİP** — FAİZLİ → ARBİTRAJ: FAİZLİ'nin vurduğu düşman 2 sn sırtını döner |
| **HACİZ** (yaklaşmalı mıyım) | GERİ ÇEKİL (vurunca 1 birim geri kayarsın, mesafe korunur) · İHTAR (vurulan 1 sn yavaşlar) · SON SÖZ (menzilin son çeyreğinde ×1.4) · UZUN VADE+ (özelin mesafe hasarı ×1.5) | **ÖNCELİK** — HACİZ → REHİN: HACİZ'in vurduğu uzaktaki düşmanı REHİN yanına çekip yakalar |
| **İPOTEK** (nereye gidecekler) | ÇEKİM (patlamadan önce içindekileri merkeze hafif çeker) · ZİNCİR TAPU (patlama içindekilerden birinin yerine küçük ikinci işaret bırakır) · DERİN TEMEL (merkezde ×1.5) · BOŞ ARSA (boşa patlarsa sağ tık beklemesi −2 sn) | **TEMİNAT** — İPOTEK → KİRA: kule işaretin yerine kurulur, ilk 4 sn kira yok |
| **İFLAS** (ne kadar dibe inerim) | CESARET (dipteyken alınan darbe −%20) · KURTARMA (dipte öldürünce +3 sn) · SON KUYRUK (saat 30'un altında dash beklemesi yarı) · TASFİYE+ (serinin son darbesi ×2) | **KONKORDATO** — İFLAS → KARŞILIK: İFLAS'ın vurduğunun yarısı KARŞILIK'ın birikenine yazılır |
| **REHİN** (kimi tutayım) | SORGU (tutarken rehinenin havuzu akar, sana hasat olur) · DOMİNO (fırlatılan rehine çarptığından bir kez daha seker) · SIKI TUTUŞ (kalkan rehineyi yarı yıpratır) · TEHDİT (tutarken önündekiler saldırı başlatmaz) | **KEFALET** — REHİN → HACİZ: fırlatılan rehineye HACİZ'in ölü bölgesi yok, ×2 |
| **ÇEK** (dönüş yolunu nasıl kurarım) | KARŞILIKSIZ+ (üç yerine beş çek) · TAHSİLAT (dönünce vurduğu düşman başına +0.5 sn) · YÖNELİM (sekince en yakın düşmana döner) · PROTESTO (dönerken yolundakileri sana çeker) | **CİRO HATTI** — ÇEK → İPOTEK: İPOTEK çekin yolu boyunca üç küçük işaret bırakır |
| **KARŞILIK** (ne zaman karşılamalıyım) | YANKI (biriken iki vuruşa bölünür) · SABIR (3 sn tutulan biriken ×1.5) · RED (karşılanan saldırgan 1 sn saldıramaz) · TEMYİZ (İTİRAZ ×1.5 uzun) | **MUKABELE** — KARŞILIK → ÇEK: biriken çeke biner, her isabette taşır |
| **TAHVİL** (ne kadar bekleyeyim) | ROLLOVER (basılıyken dash atınca olgunluk bir kez korunur) · KİLİT (3. kademede beklemek saat akıtmaz) · HAREKETLİ (basılıyken hareket ×0.8) · VADE SONU (3. kademe 360°) | **KUPON** — TAHVİL → KONİ: TAHVİL'in kademesi KONİ'nin açısına biner (kademe başına +30°) |
| **MÜŞTEREK** (kimi kime bağlarım) | GENİŞ ORTAKLIK (ikinci vuruş üçüncüyü de bağlar) · MÜTESELSİL (bağlı biri ölünce öbürleri havuzunun %10'unu kaybeder) · ZİNCİRLEME+ (dört yerine altı) · ÇEKİŞME (bağlılar birbirine sürüklenir) | **ORTAKLIK** — MÜŞTEREK → İFLAS: İFLAS bağlıya vurursa yayılma %100 |
| **ARBİTRAJ** (arkasına geçebilir miyim) | SESSİZ ADIM (görünmezken hareket ×1.3) · YAYILMA (arkadan vuruş yanındakine yarım iner) · KISA POZİSYON (arkadan öldürünce görünmezlik beklemesi sıfır) · ÇİFT İŞLEM (TERS İŞLEM iki kez vurur) | **SPEKÜLASYON** — ARBİTRAJ → TAHVİL: arkadan vuruştan sonra TAHVİL 1. kademe dolu başlar |
| **KİRA** (bu alanı ne kadar tutayım) | ÇİFT KULE (aynı anda iki kule) · DEPOZİTO (süresi dolunca ödenen kiranın %30'u geri) · AİDAT TOPLAMA (kule öldürünce +1 sn) · SAĞLAM YAPI (TAŞINMA kuleyi bir kez daha uzatır, +3 sn) | **KİRA GELİRİ** — KİRA → FAİZLİ: kule ateş ederken FAİZLİ'nin bedelini kule öder (kiraya eklenir) |

*Faz bölme önerisi* (tek faz çok büyük — Faz 32'nin 4 katı yükseltme):
- **33a:** yapı (8 yuva, kayıt uyumu, satış düzeni — soru 1–2) + KONİ · FAİZLİ · HACİZ · İPOTEK · İFLAS · REHİN'in 4'er yükseltmesi
- **33b:** ÇEK · KARŞILIK · TAHVİL · MÜŞTEREK · ARBİTRAJ · KİRA'nın 4'er yükseltmesi
- **33c:** 12 combo açan yükseltme + 12 combo

---

## 🧮 FAZ 32 — Yeni silahlar 2: TAHVİL · MÜŞTEREK · ARBİTRAJ · KİRA — **BİTTİ, Enes'in testini bekliyor** (2026-10-18)

> Sağ tık ve dash-atak onaylı plandan (2026-10-17). Normal saldırı ayrıntısı, yükseltmeler ve combo'lar benim önerim — Enes beğenmezse değişir.
> Kadro bununla 12 silaha tamamlanıyor. Silah başına 8 yükseltme + combo AÇAN yükseltmeler → Faz 33.

| Silah | Normal | Sağ tık | Dash-atak | Yükseltmeler |
|---|---|---|---|---|
| **TAHVİL** (4. bölge, 3. boss'tan) | sol tıkı BASILI TUT: vuruş olgunlaşır (0.5 sn'de bir kademe, 3 kademe: ×0.7 → ×1.6 → ×2.8 → ×4.5); beklerken saat İKİ KAT akar; bırakınca iner | **VADESİ GELMİŞ** — o anki olgunluğu (en az 1. kademe) üç parçaya bölüp önündeki üç düşmana dağıtır | **ERKEN BOZDURMA** — basılı tutarken dash: olgunluğun yarısı dash-atağa biner (düz dash-ataktan aşağı düşmez), geniş | KISA VADE (kademeler ×0.7 sürede) · YÜKSEK FAİZ (3. kademe ×1.4) · İSKONTO (beklerken saat ×1.5 akar, ×2 değil) |
| **MÜŞTEREK** (4. bölge, sunakta) | tek hedef; vurduğunu işaretler, sonraki vuruşta ikinciyi ona BAĞLAR: birine inen hasarın %60'ı öbürüne de iner (her kaynaktan, 8 sn) | **ZİNCİRLEME** — çevrendeki en yakın dört düşmanı tek zincire takar | **KOPARMA** — dash'le bağın üstünden geçersen bağ kopar, uçlar birbirine çekilip çarpışır (×2) | SIKI BAĞ (%60 → %90) · UZUN BAĞ (süre ×2) · ORTAK HASAT (yayılan hasarın hasadı yarım değil tam) |
| **ARBİTRAJ** (5. bölge, 4. boss'tan) | tek hedef; ARKADAN ×2.5, yandan ×1, ÖNDEN ×0.5 (düşmanlar sana dönük — arkaya geçmek iş) | **AÇIK POZİSYON** — 2 sn görünmezsin: düşmanlar yeni saldırı başlatmaz, ilk vuruşun her yönden "arkadan" sayılır (vurunca görünürsün) | **TERS İŞLEM** — düşmanın içinden geçen dash: çıktığın an ona dönüp arkadan vurursun | KESKİN BIÇAK (arkadan ×3.5) · GENİŞ AÇI (arka sayılan açı 120° → 90°) · GÖLGE (görünmezlik ×1.5) |
| **KİRA** (5. bölge, keşif) | nişana KULE kurarsın (tek kule): saniyede 0.6 sn kira öder, 12 sn boyunca çevresindeki en yakın düşmana ateş eder ve hasat eder; kule varken sol tık zayıf bir savurma | **TAHLİYE** — kalan kirayı peşin ödersin (erken fesih), kule patlar: kalan kira süresi kadar hasar | **TAŞINMA** — dash'le kuleyi bulunduğun yere taşırsın (kira sürer) | UCUZ KİRA (kira ×0.6) · GENİŞ ARAZİ (kule menzili ×1.4) · UZUN KONTRAT (süre ×1.5) |

*Yeni combo'lar:*
| Combo | Sıra | Etki | "+" |
|---|---|---|---|
| **AÇIĞA SATIŞ** | REHİN → ARBİTRAJ | ARBİTRAJ'ın vuruşu her yönden "arkadan" sayılır | KALDIRAÇ: o vuruş ×1.5 daha |
| **VADE FARKI** | TAHVİL → FAİZLİ | FAİZLİ, TAHVİL'in ulaştığı kademe çarpanıyla vurur (en az ×1.5) | KUPON: FAİZLİ'nin savurma bedeli geri |
| **ORTAK GİDER** | MÜŞTEREK → KİRA | kurulan kulenin kirasını SEN değil bağlı düşmanlar öder (havuzlarından) | AİDAT: kule önce bağlılara ateş eder, ×1.5 |

- [x] A. Silah verileri, biçimler, kadro tablosu (12 silah tamam), 36 yükseltme, 10 combo
- [x] B. TAHVİL (basılı tutma; saat akışı tek kaynaktan: `Clock.AkışHızı`) · ARBİTRAJ (konum çarpanı isabetin tek yolunda, görünmezlik `Enemy`'de tek koşul, TERS İŞLEM)
- [x] C. MÜŞTEREK (bağ: `MusterekBag.cs`, `Enemy.TakeDamage` her kaynaktan yayıyor) · KİRA (kule: `KiraKulesi.cs`; nişan kırpması İPOTEK'le tek yerde: `Nişan`)
- [x] D. Combo'lar + HUD (VADE kademesi, GÖRÜNMEZSİN, KULE süresi)
- [x] E. Kapılar: `Faz32SelfCheck` **39 kontrol** · **46 bozmanın 46'sı kırmızı** (6 koşu, her kontrol kendi bozmasıyla) · tüm kapılar ✅. Faz 3: kadro 12, KİRA kulesi ateşle vuruyor. Son kontrol öz-incelemeden: MÜŞTEREK'in işareti halkasıyla aynı ömürde (halka söner, işaret kalırdı — görsel yalan). `docs/TASARIM.md` 12 silahlık kadroya güncellendi.
- [x] F. Bot playtest'i (`Playtest32`) — **7/8** (rapor `Logs/Otopilot/2026-10-18_15-58/`)
  - ✅ 32-1 3. boss → TAHVİL · ✅ 32-2 basılı tut → 3. kademe → bırak, FAİZLİ = VADE FARKI · ✅ 32-3 MÜŞTEREK 4. bölgenin 3. sunağında (2.'sinde yok) · ✅ 32-4 iki vuruşla bağ, KONİ'nin vuruşu bağlıya indi
  - ❌ 32-5 **bot 4. boss'u (SİMSAR) 30 dk'da kesemedi** — 2. fazda havuz doluyor. ARBİTRAJ'ın boss'tan açılması kapıda gerçek oda yöneticisi yoluyla doğrulandı. SİMSAR'ın botla kesilememesi AYRI iş (görev çipi açıldı): denge mi, botun boss dövüşü mü
  - ✅ 32-6 görünmezken arkadan · TERS İŞLEM arkadan · REHİN → ARBİTRAJ = AÇIĞA SATIŞ · ✅ 32-7 KİRA 5. bölgede keşifle · ✅ 32-8 kule ateş etti, TAHLİYE, MÜŞTEREK → KİRA = ORTAK GİDER
  - Bot düzeltmeleri: boss'tan önce bölgenin "beklenen güç"ü (N ile atlanan sunaklar); combo'lar aynı oyuncu nesnesinde sayılıyor (bot 5. bölgede öldüğünde sayaçlar sıfırlanıyordu)

**Denge notu (Enes'e):** TERS İŞLEM tek vuruşta silah hasarının ~7 katı (KESKİN BIÇAK'la ~10). ZİNCİRLEME'deki dört düşmana KONİ değince her isabet öbür üçüne %60 yayılıyor. İkisi de geç bölge silahı; his testinde fazla gelirse kadranlar `Tuning`'de (`arbitrajBackMul`, `musterekShare`).

**Commit:** atılmadı (Enes istemedi).

**Yolda bulunan iki hata (düzeltildi, kapıda):**
- ERKEN BOZDURMA ilk hâlinde 2. kademede düz dash-ataktan zayıftı (4.9 < 5.6) — beklemek cezalanıyordu. Şimdi olgunluğun yarısı dash-atağın üstüne biniyor, aşağı düşmüyor.
- KİRA: "kur → hemen TAHLİYE" kira ödemeden her 5.5 sn'de tam patlama veriyordu. TAHLİYE artık kalan kirayı peşin ödetiyor (erken fesih); saniye başına hasarı 0.5 → 1.2.

---

## 🪢 FAZ 31 — Yeni silahlar 1: REHİN · ÇEK · KARŞILIK — **BİTTİ, Enes'in testini bekliyor** (2026-10-18)

> Enes: *"tüm silahlara özel geliştirmeler ve bu geliştirmelerin açtığı comboları ekledin mi?"* — Bugün: 5 silahın 3'er yükseltmesi (Faz 29) + 4 combo ve "+" hâlleri (Faz 30). **Silah başına 8 yükseltme ve combo AÇAN yükseltmeler → Faz 33** (yeni silahlar geldikten sonra, iki kez yazılmasın diye). Faz 31–32'de her yeni silah kendi 3 yükseltmesi ve combo'larıyla geliyor.

| Silah | Normal | Sağ tık | Dash-atak | Yükseltmeler |
|---|---|---|---|---|
| **REHİN** (2. bölge, sunakta) | yakındakini YAKALA (boss değil); tutarken önden gelen vuruşu o yer; ikinci vuruş FIRLAT: ilk çarptığına sert vurur | **TAKAS** — tuttuğunu en yakın ikinci düşmanla yer değiştirtir, ikisine vurur | **SÜRÜKLEME** — tutuyorsan dash sonunda yere çarpar (çevresine de), tutmuyorsan yakalar | AĞIR REHİN (fırlatma ×1.6) · GENİŞ KOL (tutma menzili ×1.5) · KALKAN (yandan gelenleri de karşılar) |
| **ÇEK** (3. bölge, 2. boss'tan) | fırlatılır, arena kenarından seker, döner; gidişte de dönüşte de vurur; dönene kadar silahsızsın | **KARŞILIKSIZ** — üç çek yelpaze gibi | **CİRO** — havadaki çeki yakalayıp dash yönüne yeniden fırlatırsın | ÇİFT CİRO (bir sekme daha) · HIZLI TAHSİL (hız ×1.4) · DÖNÜŞ KESİNTİSİ (dönüşte hasar ×1.5) |
| **KARŞILIK** (3. bölge, keşif) | kısa karşılama (0.23 sn, sonra 0.43 sn toparlanma): o anda gelen darbe saatinden düşmez, birikir; pencere bitince önüne iner | **İTİRAZ** — uzun karşılama, biriken hepsi önündekilere | **KARŞI DAVA** — biriken varsa dash-atak onun iki katıyla vurur | GENİŞ PENCERE (×1.6) · TAM KARŞILIK (biriken ×1.5) · İADE (karşıladığının yarısı saate geri) |

*Yeni combo'lar:* **KARŞI İCRA** (KARŞILIK → FAİZLİ: FAİZLİ ×2; + İCRA beklemesi sıfır) · **SENET HACZİ** (ÇEK değdi → HACİZ: HACİZ ×2; + yanındaki düşmana da seker) · **SİLİP SÜPÜR** (REHİN'le vur — fırlat/takas/sürükle, yakalamak sayılmaz → KONİ: sert iter, hasar ×1.5; + 360° süpürür).

- [x] A. Saldırı biçimi (`Weapon.Saldırı`: koni / rehin / çek / karşılık) ve üç silahın normal saldırısı. ÇEK'in uçuşu ayrı dosya: `CekUcusu.cs`
- [x] B. Sağ tık (TAKAS, KARŞILIKSIZ, İTİRAZ) ve dash-atakları (SÜRÜKLEME, CİRO, KARŞI DAVA)
- [x] C. 9 yükseltme, kadro tablosu (REHİN 2. bölge 3. sunak · ÇEK 2. boss · KARŞILIK 3. bölge 3. ara bölge keşif), 3 combo + "+"
- [x] D. Kapılar: `Faz31SelfCheck` **36 kontrol** · **42 bozmanın 42'si kırmızı** (4 koşu; her kontrol en az bir kez) · tüm kapılar ✅. Faz 3: kadro 8 silah, ÇEK uçarak vuruyor. Faz 16: "farklı soru" silahın biçimini de sayıyor.
- [x] E. Bot playtest'i (`…OtopilotMenu.Playtest31`) — **6/6** (rapor `Logs/Otopilot/2026-10-18_13-59/`)
  - ✅ 31-1 REHİN 2. bölgenin 3. sunağında (2.'sinde yok), 3. yuvaya girdi
  - ✅ 31-2 gerçek tuşlarla yakala → fırlat → KONİ = SİLİP SÜPÜR, ilk denemede · kalkan 1 vuruş yedi
  - ✅ 31-3 2. boss düşünce ÇEK açıldı
  - ✅ 31-4 çek gidip döndü (bir atışta 4 isabet), değince HACİZ = SENET HACZİ, ilk atışta
  - ✅ 31-5 KARŞILIK 3. bölgenin 3. ara bölgesinde keşifle açıldı
  - ✅ 31-6 karşıladı, biriken vuruşa indi, FAİZLİ = KARŞI İCRA (2 combo). Bot şansla karşılıyor (1 karşılama) — pencerenin hissi Enes'te
  - İlk koşuda 31-4 kırmızıydı: hata botun ölçüsündeydi ("havuz düştü" = isabet sanıyordu), oyunda değil. Ders lessons.md'de.

**Yolda bulunan iki istismar (düzeltildi, kapıda):**
- KARŞILIK'ın ilk hâlinde pencere savurmanın %80'iydi → tıklamayı basılı tutan zamanın ~%75'inde dokunulmazdı. Şimdi pencere 0.23 sn + 0.43 sn toparlanma → ~%35, toparlanırken karşılayamıyor.
- Silah değiştirmek saldırıyı iptal ediyor → "tıkla → 2 → 1 → tıkla" toparlanmayı siliyordu. KARŞILIK'a silah değişiminden bağımsız bekleme kondu.

**Commit:** atılmadı (Enes istemedi).

---

## 🗡️ FAZ 30 — Silah kadrosu — **BİTTİ** (2026-10-18)

**✅ Enes'in cevapları (2026-10-16)** — *"sonraki faza geçme"*: başlanmadı, sıradaki oturum buradan.
1. **Silah açılması:** ana boss yenilince **1** silah. Öbürü ya **satın alınır** ya da **keşifle** bulunur — açık dünya yok, ama hikâye için mekânlara eşyalar gömülecek; silahlar gizli/saklı yerlerde bulunabilir.
2. **Envanter:** sunak odalarında ve silah sunağında düzenleniyor (öneri onaylandı, "şimdilik öyle").
3. **Combo:** **hızlı silah geçişleriyle.** Bazı silahlarla **3'lü combo**. Combo **istismar edilmemeli** (abuse). Combo'lar **gelişip yeni etki kazanabilmeli** (yükseltme).
4. **Başlangıç:** oyun **2 silahla** başlıyor, ilk boss'a kadar **4 silah** oluyor (KONİ, FAİZLİ, HACİZ, İPOTEK). **İFLAS 2. bölgede** açılıyor. **Combo mekaniği** ilk boss'tan önce ya da 2. bölgede tanıtılsın.

**📝 ÖNERİ (2026-10-17) — Enes'in onayını bekliyor**

*7 yeni silah* — her biri YENİ bir soru soruyor (elenenler tekrar önerilmedi: VADELİ, DURGUN, TAKSİT):
| Silah | Sorusu | Nasıl |
|---|---|---|
| **REHİN** | kimi tutayım? | Bir düşmanı yakalarsın; tutarken önden gelen vuruşu o yer, fırlatınca çarptığına vurur |
| **ÇEK** | geri dönüş yolunu nasıl kurarım? | Fırlatılır, duvardan seker, sana döner — gidişte de dönüşte de vurur; dönene kadar silahsızsın |
| **KARŞILIK** | ne zaman karşılamalıyım? | Kısa karşılama penceresi: o anda gelen darbe saatinden değil bir sonraki vuruşunun hasarına yazılır |
| **TAHVİL** | ne kadar bekleyeyim? | Basılı tut, vuruş olgunlaşır (3 kademe); beklerken saat iki kat akar |
| **MÜŞTEREK** | kimi kime bağlarım? | İki düşmanı birbirine bağlar; birine vurduğun öbürüne de iner |
| **ARBİTRAJ** | arkasına geçebilir miyim? | Arkadan çok sert, önden zayıf (fikir defterindeki "arkadakine vuran kılıç") |
| **KİRA** | bu alanı ne kadar tutayım? | Yere bir kule kurarsın; saniyede kira öder, kurulu kaldıkça vurur ve hasat eder |

*Yeni silahların sağ tığı ve dash-atağı* (Enes 2026-10-17: "planlamayı unutma") — ilke aynı: özel, silahın ZATEN sorduğu soruyu güçlendirir, dash-atak onun YAKIN/hareketli cevabıdır:
| Silah | Sağ tık | Dash-atak |
|---|---|---|
| **REHİN** | **TAKAS** — tuttuğun düşmanı en yakın ikinci düşmanla yer değiştirtip ikisine birden vurur | **SÜRÜKLEME** — dash boyunca yakaladığını önünde kalkan gibi taşır, dash sonunda yere çarpar |
| **ÇEK** | **KARŞILIKSIZ** — üç çek yelpaze gibi fırlar, her biri bir kez seker | **CİRO** — dash'in sonunda havadaki çeki yakalayıp aynı yöne yeniden fırlatırsın (dönüşü beklemeden) |
| **KARŞILIK** | **İTİRAZ** — uzun karşılama penceresi; o sırada gelen her darbe birikir, pencere bitince hepsi önündekilere geri iner | **KARŞI DAVA** — dash'le saldırgana atılırsın; karşılama penceresinde dash atarsan biriken hasarın iki katı |
| **TAHVİL** | **VADESİ GELMİŞ** — o anki olgunluğu bekletmeden üç parçaya bölüp üç hedefe dağıtır | **ERKEN BOZDURMA** — dash'le olgunluğu harcar: yarı hasar ama beklemeden, dash yönünde geniş |
| **MÜŞTEREK** | **ZİNCİRLEME** — bağı ikiden dörde çıkarır (dört düşmanı tek zincire takar) | **KOPARMA** — dash'le bağın ortasından geçersen bağ kopar, iki uç birbirine çekilip çarpışır |
| **ARBİTRAJ** | **AÇIK POZİSYON** — kısa süre görünmez olursun, ilk vuruş her yönden "arkadan" sayılır | **TERS İŞLEM** — düşmanın içinden geçen dash; çıktığın an arkasındasın ve vuruş arkadan iner |
| **KİRA** | **TAHLİYE** — kurulu kuleyi patlatırsın: kalan kira süresi kadar hasar, kira biter | **TAŞINMA** — dash'le kuleyi bulunduğun yere taşırsın (kira sürer) |

*Açılış sırası* (her bölgede +2: 1 boss'tan, 1 satın alma ya da keşif):
| Bölge | Açılan |
|---|---|
| Başlangıç | KONİ · FAİZLİ |
| 1. bölge (boss'tan önce) | HACİZ (sunakta satın) · İPOTEK (keşif) |
| 2. bölge | İFLAS (1. boss) · REHİN |
| 3. bölge | ÇEK (2. boss) · KARŞILIK |
| 4. bölge | TAHVİL (3. boss) · MÜŞTEREK |
| 5. bölge | ARBİTRAJ (4. boss) · KİRA |

*Keşif:* bazı dövüş odalarının kenarında soluk parlayan bir köşe; oda temizlenince oraya yürürsen silahı bulursun. Kaçırırsan aynı silah sonraki sunakta pahalıya satılır — hiçbir silah sonsuza dek kaçmaz.

*Combo kuralları:*
- A ile vur → **0.8 sn** içinde B'ye geç ve vur = 2'li combo. → 0.8 sn içinde C = **3'lü** bitiriş.
- **İstismar yok:** her vuruş İSABET etmeli (boşa savurma sayılmaz) · aynı combo **4 sn** tekrar edilemez · aynı silah iki kez sayılmaz.
- **Gelişme:** silah sunağında combo yükseltmesi — combo'ya yeni etki ekliyor.
- İlk combo'lar mevcut silahlarla: **HACİZ → İPOTEK** (işaretin içindekine HACİZ vurursa işaret hemen patlar) · **KONİ → İPOTEK** (itip topladığının ortasına hızlı ipotek) · **FAİZLİ → İFLAS** (FAİZLİ'nin ödediği İFLAS'ın dip hesabına sayılır) · 3'lü **KONİ → HACİZ → İPOTEK**.
- *Tanıtım:* ilk 2'li combo İPOTEK açılınca (1. boss'tan önce), 3'lü combo'lar 2. bölgede.

**Faz 30 adımları (2026-10-18)**
- [x] **Kadro altyapısı** (`Kadro.cs`): açılış tablosu (başta / sunak / keşif / boss), koşu boyu kalıcı (`SaveData.unlockedWeapons`, `loadout`). Oyun KONİ + FAİZLİ ile başlıyor; **1–3 tuşları envanter yuvası** (silah değil).
- [x] **HACİZ** 1. bölgenin 2. ara bölgesinin sunağında satılıyor (taban 30 sn ya da saatin %15'i) → boş yuvaya giriyor.
- [x] **İPOTEK keşifle** (`GizliSilah.cs`): 4. ara bölgenin 4. odası temizlenince arenanın kenarında soluk parlayan köşe; üstüne yürüyünce açılıyor. Kaçırılırsa o ara bölgenin sunağında ve sonrakilerde **2 kat** fiyatla satılıyor.
- [x] **İFLAS 1. boss'tan.** Boss odasında artık: silah sunağı + KADRO + combo sunağı.
- [x] **KADRO sunağı** (sunak odalarında ve boss sonrası): [E] kuşandığın yuvadaki silahı envanterde olmayan bir sonraki açık silahla değiştiriyor. HUD: 3 yuva + "+N silah KADRO sunağında".
- [x] **Combo** (`Combo` + `PlayerController` zinciri): isabet şart · adım arası 0.8 sn (`comboWindow`) · aynı combo 4 sn tekrar yok (`comboCooldown`) · aynı silah iki kez sayılmaz.
  | Combo | Sıra | Etki | "+" (silah sunağında, boss başına 1) |
  |---|---|---|---|
  | HACİZ KESİNLEŞTİ | İPOTEK → HACİZ | işaretin içindekine HACİZ vurursa işaret HEMEN patlar ×1.5 | KESİN HÜKÜM: HACİZ'in sağ tığı sıfırlanır |
  | TOPLU İPOTEK | KONİ → İPOTEK | işaret vurulanların ortasına, gecikme ×0.4 | ÇİFT HACİZ: en uzaktaki düşmana ikinci ipotek |
  | TEMERRÜT | FAİZLİ → İFLAS | İFLAS saatin yarıdaymış gibi sert | TEMERRÜT FAİZİ: o vuruşun hasadı ×2 |
  | HACİZ KARARI (3'lü, 2. bölgeden) | KONİ → HACİZ → İPOTEK | HACİZ'in hedefine ×2 alan, gecikme ×0.3, hasar ×2 | TAHLİYE: patlama düşmanları dışa iter |
- [x] Kapı: `Faz30SelfCheck` **18 kontrol** · **21 bozmanın 21'i kırmızı** (3 koşu) · tüm kapılar ✅. Faz 3 kapısı: tuş artık yuva başına.
- [x] **Bot playtest'i** (`Otopilot — Faz 30 playtest`): **6/6 ✅**, istisna yok — 2 silahla başla (3 tuşu boş) · HACİZ 2. sunakta, 1.'de yok · İPOTEK keşifle (bot kenarda dolaşırken köşenin üstünden geçti) · KADRO ile 2. yuvaya İPOTEK · **gerçek tuşlarla TOPLU İPOTEK** (ilk denemede) · 1. boss → İFLAS + combo sunağında KESİN HÜKÜM. Not: bot 1. boss'ta KUŞATMA'dan 41'de 19 kez vuruldu (önceki ölçümlerde de en çok vuran saldırı).

*Faz bölme önerisi* (tek faz çok büyük):
- **Faz 30 — kadro altyapısı:** 3'lü envanter, kilit açma (boss / satın / keşif), mevcut 5 silahın açılış sırası, combo sistemi + 4 ilk combo.
- **Faz 31 — yeni silahlar 1:** REHİN · ÇEK · KARŞILIK (+ combo'ları).
- **Faz 32 — yeni silahlar 2:** TAHVİL · MÜŞTEREK · ARBİTRAJ · KİRA.
- İçerik (25 mini boss, düşmanlar) bunlardan sonra.


> Enes: *"Silahların hepsini oyunun başında vermeyeceğiz. Bazıları oyunun ileri kısmında açılacak (totalde 12 olabilir, her bölgede 2 tane açılan silahlar gibi, ilk bölgede 4 tane silah olcak). Kullanıcı yanına 3 tane silah alabilir sadece… bazı silahların birbiri ile combo saldırıları olsun. Kullanıcı yeni silahlar elde edince envanterini düzenleyip güçlendirme alıp ona göre oynayacak."*

- **12 silah:** 1. bölgede 4 açık, sonraki her bölgede +2 (4 + 2×4 = 12). Bugün 5 var → **7 yeni silah** tasarlanacak (her biri yeni bir SORU sormalı — `Weapon.cs` ilkesi; elenenler `FIKIR_DEFTERI.md`'de).
- **Envanter = 3 silah.** 1–3 tuşları kuşanılan üçü. Yeni silah açılınca envanter düzenlenir.
- **Combo'lar:** bazı silah çiftleri birbirine bağlanıyor (biriyle vurup hemen öbürüne geçince ek etki).
- Silah sunağı (Faz 29) envanterdeki silahlara satıyor; kalıcı yükseltme silahta kalıyor.
- **❓ Sorular (Enes):**
  1. Yeni silah nasıl açılıyor? *Önerim:* ana boss düşünce silah sunağının yanında 2 silah duruyor, ikisini de alıyorsun (ücretsiz) — "her bölgede 2 tane açılan".
  2. Envanteri nerede düzenliyorsun? *Önerim:* yalnızca sunak odalarında ve silah sunağında (saat duruyor, karar anı) — dövüşte değil.
  3. Combo nasıl çalışıyor? *Önerim:* A silahıyla vurduktan sonra 1 sn içinde B'ye geçip vurursan ek etki (ör. İPOTEK işaretinin içindekine HACİZ vurursa işaret hemen patlar).
  4. İlk 4 silah hangileri? *Önerim:* KONİ, FAİZLİ, HACİZ, İPOTEK; İFLAS 2. bölgede açılsın (dipte oynamak öğrenilmiş bir şey).

---

## ⚔️ FAZ 29 — Silaha özel yükseltmeler + yeni sunak güçlendirmeleri — **BİTTİ** (2026-10-15)

Enes: *"bu düzenlemelerden sonra commit at ve sonraki faza geç"*. Kaynak: eski "Faz 20" listesi (aşağıda) + birleşik playtest maddeleri 5, 7, 8.

**Kararlaştırılmış — yapılıyor**
- [x] **A. İPOTEK:** dash-atak = HACİZ İHBARI (nişan noktasına geniş, geç patlayan işaret — arkana değil). **Yeni sağ tık = İPOTEK YAĞMURU:** menzilin 1.6 katı alandaki düşmanlardan rastgele birinin üstüne 0.4 sn arayla 6 ipotek (`ipotekRain*`). İlki hemen düşüyor; dash ya da silah değişimi yağmuru kesiyor. ⚠️ Yağmur sürerken sol tık ipoteği bekliyor (aynı anda tek ipotek kuralı).
- [x] **B. FAİZLİ İCRA yeniden:** bedel saatin **%12**'si (en az 1.5 sn) — yüzde olduğu için seni öldürmez. Hasar = silah hasarı + ödenen her saniye için **0.5**. Öldürürse bedelin **1.5 katı** geri, ama saat doyma tavanının üstündeyse geri vermez (eşik = doyma tavanı). Örnek: 250 sn saatte 30 sn öder, ~21 vurur (eskisi 19.6).
- [x] Kapılar: `Faz29SelfCheck` 5 kontrol (yağmur düşmanların üstüne · patlayıp vuruyor · İCRA yüzdesi · hasar bedelle artıyor · iade tavanın altında var, üstünde yok) + Faz 3/16 yeni kurala uyarlandı · 6 bozmanın 6'sı kırmızı (2 koşu) · tüm kapılar ✅. ⚠️ "Öldürmek" = boş düşmana vurmak: İCRA bahsi boş düşmanı bitirmek.

**Öneri — Enes: "devam et" (2026-10-15)**
- [x] **C. Silah sunağı (eski Faz 20):** ana boss ölünce boss odası sunağa dönüyor (saat durur). Kuşandığın silaha özel 2 yükseltme satıyor; koşu boyu kalıyor. Fiyat sunak kuralıyla (en az saatin %10'u).
  | Silah | Yükseltme önerisi |
  |---|---|
  | KONİ | GENİŞ AĞ (açı +) · SÜPÜRME bekleme −%30 · GERİ TEPME (itme +) |
  | FAİZLİ | BİLEŞİK (öldürme iadesi ×2 → ×3) · İCRA bekleme −%30 · TEMERRÜT (ıskada bedelin yarısı geri) |
  | HACİZ | UZUN KOL (menzil +) · DELİCİ (2 hedef) · DAR ÖLÜ BÖLGE |
  | İPOTEK | ÇİFT TAPU (aynı anda 2 işaret) · HIZLI TAPU (gecikme −%30) · GENİŞ ARSA (alan +) |
  | İFLAS | DİP (panik eşiği yukarı) · SERİ (TASFİYE +2 vuruş) · KAPANIŞ (dipte hasat +) |
  - Yapıldığı gibi: ana boss düşünce (son bölge hariç) boss odasında 3 sunak çıkıyor, saat duruyor. Sunaklar KUŞANDIĞIN silahın üç yükseltmesini satıyor; silahı değiştirince sunak da değişiyor. Boss başına **2 alım**. Fiyat: en az saatin %15'i (taban 24 sn). Koşu boyu kalıcı (`SaveData.weaponUpgrades`) — ana boss'un bölgesi geri gelmediği için ölüm geri almıyor; kapat-aç ile yeniden almak da yok (bölge başına sayım kayıtta).
  - GERİ TEPME değişti: KONİ'nin normal savurması hiç itmiyordu (×1.6 sıfırı büyütmez) → "savurma da iter, SÜPÜRME itmesi +%60".
- [x] **D. Yeni ara bölge sunakları:** **MUAFİYET** (düşman vuruşu −%8, azalan getiri, tavan %50) · **SABIR** (sağ tık beklemesi −%10, tavan %50). Sabit 3 + dönen 1: 1., 3., 5. ara bölgede MUAFİYET, 2. ve 4.'de SABIR. ⚠️ Ad değişti: SİGORTA zaten bir düşmanın adı (hasadı kısan) — karışmasın diye MUAFİYET.
- [x] E. Kapılar: `Faz29SelfCheck` **27 kontrol** — 15 yükseltmenin 15'i de ETKİSİYLE (konum taraması, iade, bekleme, işaret süresi/alanı, hasar, hasat), silah sunağının açılması, kuşanılan silaha göre değişmesi, boss başına 2 alım, MUAFİYET, SABIR, dönen sunak. **21 bozmanın 21'i kırmızı** (4 koşu, birbirini örtmesin diye). Tüm kapılar ✅.
- [x] Hata (kapı yakaladı): sunak "tükendi" kuralı yalnızca tuşa basma yolundaydı; `Buy` kendisi bakmıyordu → kural `Buy`'ın başında.
- [x] **Bot playtest'i** (`Otopilot — Faz 29 playtest`, Enes: "test koşusunu bottan yaptır"): 8/8 ✅ — boss düşünce 3 silah sunağı + saat durdu · iki alım (ÇİFT TAPU + HIZLI TAPU, 83 sn), üçüncü kapalı · YAĞMUR 6/6 ipotek · dash-atak HACİZ İHBARI (alan 4.8, normal 2.4) · ÇİFT TAPU ile aynı anda 2 ipotek · İCRA 35.9 sn @ 299 (%12) · sunak odasında MUAFİYET, sonrakinde SABIR alındı. İstisna yok, tüm kapılar ✅.
- **Zorluk raporu:** TEMİZ + alan koşuyu 1306 sn ile bitiriyor (85 alım) · TEMİZ almayan bölge 2 · **ORTA + alan artık bölge 3'e** varıyor (önce 2) — MUAFİYET ilerleme zorluğunu kapatıyor · ORTA almayan bölge 1. Silah yükseltmeleri raporda modellenmiyor.

---

## 🏗️ FAZ 28 — Yapı + tempo — **BİTTİ** (2026-10-13 · ekonomi 2026-10-14)

Enes: *"başla"* (YAPI taslağı ve tempo önerisi onaylı). Önce 1 bölge ile ölçülür.

**Oda düzeni (bölge başına):** `[7 dövüş odası → MİNİ BOSS → SUNAK]` × 5 ara bölge → **ANA BOSS** = 46 oda.
- Sunak odası her mini boss'tan sonra; aynı zamanda **kontrol noktası** (saat durur, onarım sunağı orada). Bölgenin ilk odası da kontrol noktası.
- Dövüş odalarının büyümesi ara bölge İÇİNDE (oda 1→7) + ara bölgeler arası çarpan; bölge 46 odaya çıkınca eski "oda numarası" formülü patlamasın.

**Mini boss:** ana boss'un dağarcığından küçük bir dilim (her mini farklı 2–3 saldırı, tek faz, çağırma yok). *"Mini boss'lar ana boss'un saldırılarını tek tek öğretiyor, ana boss hepsini birden yapıyor."* Kendi içeriği (25 özgün mini boss) Faz 30+.

**Ölüm:**
- Diriliş eşyası varsa bugünkü gibi çalışır. **Eşya yoksa oyun BİTMEZ → son kontrol noktasına** dönülür.
- **Nemesis toparlanması:** her ölümde o odanın sana kaybettirdiği saniyeler (oda girişindeki saat + odada hasat ettiğin) son temizlenmiş mini boss'un defterine yazılır. Defter, mini boss havuzunun ×`miniBossRecoveryShare` katına ulaşınca mini boss **geri gelir**: kontrol noktası bir öncekine kayar, mini boss defteri kadar BÜYÜMÜŞ olarak tekrar dövüşülür. Ana boss'un temizlediği bölge geri gelmez.
- ❓ Faz sonunda sorulacak: ÇAĞRI (kontrol noktasına dönüş eşyası) artık eşyasız ölümle aynı işi yapıyor — rolü ne olsun?

**Tempo:** dövüş odası **dalgalarla** (~1.5 dk), mini boss 3–4 dk, ana boss 6–8 dk; boss/mini boss odasında saat akışı yavaş (`bossRoomDrainMultiplier`). Bot ile 1. bölge ölçülür.

**Adımlar**
- [x] A. Oda düzeni: `RoomManager.Tür` tek kaynak (oyun + zorluk raporu + kapılar); `roomsPerRegion` ve `checkpointEveryRooms` kalktı → `subRegionsPerRegion` 5 · `roomsPerSubRegion` 7 · `RoomsInRegion` 46. Test tuşu **M** = sıradaki mini boss. Eski kapılar yeni düzene uyarlandı (Faz 4, 9, 10, 13, 17, 19, 24, 25, 26, 27)
- [x] B. Mini boss (`MiniBossKind`): ana boss'un faz 1'inden 2 yakın + 1 uzak (sonuncusu + harita), tek faz, havuz ana boss'un ×0.5'i; geçici unvanlar ÇIRAK · KÂTİP · MEMUR · VEKİL · KÂHYA. Boss listesinde değil (tekel/kapsama kapıları görmesin)
- [x] C. Ölüm: eşyasız ölüm → kontrol noktası (oyun bitmiyor); toparlanma defteri (`SaveData.miniLedger`), eşik = mini havuzu × 1.5 (`miniBossRecoveryShare`); geri gelen mini nemesis kadar büyük; HUD "… TOPARLANDI — kontrol noktası geri kaydı"
- [x] D. Tempo: 5 dalga (`roomWaves`), oda havuzu ara bölge içinde ×1.10 (`roomPoolGrowth`), ara bölgeler arası ×1.12, mini boss havuzu ×5 (`miniBossPoolShare`), ana boss ×12 (`mainBossPoolScale`), boss/mini odasında akış ×0.35; sunak 12 sn × 1.15, sunak başına türden 1 alım, getiri azalması ×0.9
- [x] E. `Faz28SelfCheck` 19/19 · tüm kapılar 25/25 · 8 bozmanın 8'i kırmızı (3 koşuda, birbirini örtmesin diye)
- [x] F. Bot: 1. bölge tam ölçüm (`Otopilot — 1. bölge ölçümü`, kalibrasyon 2)
  | | Ölçüm |
  |---|---|
  | 1. bölge | **46.7 dk** · ara bölgeler 8.3 · 8.1 · 8.3 · 8.4 · 13.5 dk |
  | Dövüş odası | ortalama 49 sn (26–70) — insan için ~1–1.5 dk |
  | Mini boss | 2.4–3.3 dk · TAHSİLDAR 4.4 dk |
  | Saat (başlangıç 110) | mini girişleri 469 → 1033 → 1573 → 2722 → 4181 · bölge sonu **4208** |
  | Bot | vurulma %3 · 15 alım (hasat %51, hasar 5.5, dash 0.38) |
- [x] **HATA — float saat:** bot ~3600 kare/sn koşuyor; büyük float saatte kare başı akış (~0.0003 sn) yuvarlanıp kayboluyordu → ilk ölçümün 9931 sn'si yarı yarıya ölçüm hatasıydı. `Clock` birikimleri `double`. Kapı: `Faz28SelfCheck` 20/20 (9000 sn'lik saatte küçük kareler), bozunca 9.77 ≠ 7.50 kırmızı. Ders 16.
- [x] Araçlar: oda hesabında `süre / saat akışı / aura` · bot senaryoları `AraBolge1Olcum` (~10 dk) ve `MiniTeshis` (`[NABIZ]`) · zorluk raporu dalgaları ayrı sayıyor (aura ~5 kat abartılıyordu). Zırhlı/iri sayısı her oda girişinde sıfırlanıyor.
- [x] Tüm kapılar ✅ TEMİZ.
- ~~Ekonomi iki uçlu~~ → **G** (Enes 2026-10-14).

**G. Ekonomi + ÇAĞRI + mini boss'lar (Enes 2026-10-14)**
> *"başlarda kolay ve oyuncu dostu, ilerledikçe zorlaşan ve bu zorluğu bufflar ile kapatmaya çalışan bir yapı lazım. İyi oyuncu bile o kadar absürt saatlere ulaşmamalı… Seçenek B daha iyi: almazsa boss'ları kesemeyecek hale gelmeli ama çok hardcore sevenleri de tuttukları zaman ile ödüllendirmeli."*
- [x] **Hasat doyması:** saat tavanın üstündeyse hasat ×(tavan / saat). Tavan 250 sn (1. bölge), her bölgede ×1.35 (`harvestSoftCap`, `harvestSoftCapGrowth`). HUD: "SAAT DOLU — hasat ×0.50".
- [x] **Sunak = B:** fiyat en az saatin %10'u (`altarPricePercent`). Biriktiren daha çok öder; almayan tamponunu tutar ama güçlenmez.
- [x] **İlerleme:** düşman vuruşu her ara bölgede ×1.09 pahalanıyor (`hitCostProgressGrowth`, bölgeler boyunca sayılır: son ara bölgede ×7.9). Başlangıç ucuzladı: darbe 4 → **1.5 sn**, boş düşman 1.5 → 0.6.
- [x] **ÇAĞRI = bir önceki odanın başı** (oradaki saat ve güçle, K2 tabanı 20 sn). Mini boss geri geldiyse kontrol noktasına (ötesine atlatamaz). Bölge sınırını geçmez.
- [x] **Mini boss'lar 5 bölgenin hepsinde** (zaten öyleydi) — ama TAHSİLDAR ve EKSPER'de 4. mini 1.'yi tekrarlıyordu. Artık ilk üçü faz 1'den, son ikisi faz 2'den (`+`'lı saldırılar). Kapı: 25 mini'nin dağarcığı farklı.
- [x] Zorluk raporu düzeltildi: aura oyunun kendi değerinden (`Enemy.AuraOranı`), mini boss aurası 10 kat abartılıyordu.
- [x] Kapılar: `Faz28SelfCheck` 25 kontrol (ÇAĞRI ×2, mini boss'lar, doyma, sunak yüzdesi, ilerleme) · 6 bozmanın 6'sı kırmızı · tüm kapılar ✅.
- **Zorluk raporu (yeni ekonomi):**
  | Oyuncu | Sonuç |
  |---|---|
  | TEMİZ + alıyor | koşuyu **1302 sn** ile bitiriyor · saat 1. bölgede ≤ ~700, en çok ~1580 |
  | TEMİZ, almıyor | bölge 2'de ölüyor |
  | ORTA + alıyor | 1. bölgeyi geçiyor, **bölge 2'de** zorlanıp ölüyor |
  | ORTA, almıyor | 1. bölgenin sonunda ölüyor |
- [x] **Bot — 1. bölge (yeni ekonomi):** 45.9 dk · oda ort. 48 sn · mini 2.4–2.9 dk · TAHSİLDAR 4.4 dk · saat en çok **~770**, bölge sonu **467** (önce 4208; ilk ölçümde 9931).
- [x] **Bot — mini boss testi** (`MiniTesti`: 25 mini, o noktada beklenen güç, saat = doyma tavanı). Süre (oyun zamanı): bölge 1–3 **1.5–3.0 dk**, bölge 4 **2.5–4.3**, bölge 5 **3.1–5.4** — ilerledikçe uzuyor. Bot neredeyse hiç vurulmuyor (%1) ve mini boss'tan genelde KÂRLI çıkıyor (+%4…+%32 saat). Asıl tehlike **KUŞATMA** (11/48 isabet): KÂTİP · TAHSİLDAR −209 sn, KÂTİP · SİMSAR −255 sn. İnsan için mini boss'un bedeli vurulmak.
- Faz 28 COMMIT'SİZ.

---

## 🗺️ YAPI — tek koşu, hikâye 8 saat (Enes, 2026-10-12) — **✅ ONAYLANDI**

> Enes: *"Her bossun bölgesi var, boss temizlenince orası kayboluyor, sonraki bölgeye geçiyoruz. Bölge içinde bölgeler var. Mini bossların temizlendiği yerde o ara bölge kayboluyor, checkpoint gibi. Her minibossun toparlanma süresi oluyor ölünce; sonraki minibossu az ölümle temizlemek lazım yoksa yenileniyor ve başa dönüyoruz. Ana boss yenince bölge kalktığı için sorun olmuyor. Ara bölge, perde ve oda sayısını sen belirle."*

**Kurallar (Enes: "doğru anladın")**
- 5 **bölge** = 5 ana boss (bugünküler). Ana boss ölünce bölge kalkar: KALICI kontrol noktası.
- Her bölgede **ara bölgeler**, her biri bir **mini boss**la biter. Mini boss ölünce ara bölge kalkar: kontrol noktası.
- Ölünce son kontrol noktasına dönülür, saat orada kaydedilen değerle.
- **Toparlanma:** her ölümde, temizlenmiş son mini boss toparlanmaya başlar. Toparlanması dolarsa geri gelir, ara bölgesi geri açılır, bir önceki kontrol noktasına düşersin. Ana boss'un temizlediği bölge asla geri gelmez.
  - ✅ **Toparlanma = NEMESİS** (Enes): ölümde kaybettiğin saniyeler seni öldürenin defterine yazılıyor (bugünkü nemesis). Defter, temizlenmiş son mini boss'un toparlanması: dolarsa geri gelir — nemesis olduğu için BÜYÜYEREK.
- ✅ Yasa değişti (Enes: "yessir"): "ölünce her şey gider" → "ölünce son kontrol noktasına; kontrol noktaları nemesis defteriyle aşınır". Meta ilerleme yine yok. CLAUDE.md ve TASARIM.md güncellendi.

**Sayılar — taslak (hedef taban 8 saat)**
| | Sayı | Süre |
|---|---|---|
| Bölge (ana boss) | 5 | ~95 dk / bölge |
| Ara bölge (mini boss) | bölge başına **5** → toplam **25 mini boss** | ~15 dk / ara bölge |
| Oda | ara bölge başına **7** (+ sunak odası) → toplam ~175 oda | ~1.5 dk / oda |
| Mini boss | | ~3–4 dk |
| Ana boss | | ~6–8 dk |
| **Toplam** | | **~8 saat**, ölümlerle ~10 |
- **Perde** mekanik değil, hikâye bölümü: Perde 1 = bölge 1–2, Perde 2 = bölge 3–4, Perde 3 = bölge 5 (final).
- **İçerik ihtiyacı:** bölge başına 2–3 yeni düşman türü (bugün 7 tür), 25 mini boss (bölgenin ana boss'unun suçundan türeyen, daha küçük dağarcıklı), yeni oda dizilişleri.

**Faz sırası**
1. ✅ **Faz 27 — kartopu:** sunak fiyatı kalıcı, azalan getiri, hasar tavanı (tempo Faz 28'e taşındı).
2. **Faz 28 — yapı + tempo:** ara bölge + mini boss + kontrol noktası + nemesis toparlanması; dalgalı odalar (~1.5 dk), uzun boss (3–8 dk, boss odasında yavaş akış). Önce 1 bölge ile.
3. **Faz 29 — silaha özel yükseltmeler + yeni sunak güçlendirmeleri** (Enes: "bir faza eklemeyi unutma"): eski "Faz 20" (boss sonrası sunak, silaha özel yetenekler), FAİZLİ İCRA yeniden, İPOTEK yeni sağ tık, sunaklara yeni güçlendirme türleri. Faz 27'nin ekonomi kurallarıyla (kalıcı fiyat, azalan getiri) çalışır.
4. **Faz 30 — silah kadrosu** (Enes 2026-10-15): 12 silah, kilit açma, 3'lü envanter, combo'lar. Plan aşağıda.
5. **Faz 31+ — içerik:** 25 mini boss, yeni düşman türleri, oda dizilişleri, bölge bölge.

---

## ⚖️ FAZ 27 — Denge — **ADIM 1 BİTTİ (kartopu) · ADIM 2 (tempo) Faz 28'e taşındı** (2026-10-13)

**✅ Adım 1 — kartopu kırıldı**
- **Kalıcı fiyat:** her türün fiyatı KOŞU boyunca o türden kaç alım yapıldığına bağlı (`Clock.Fiyat`). Önceden her bölgenin sunağında sıfırlanıyordu.
- **Azalan getiri:** aynı türden her alım bir öncekinin ×0.8'i (`altarStepDecay`). Hasar tavanı kendiliğinden: 3.5 + 1.2/(1−0.8) = **9.5 (×2.7)**. Sunak sıradaki gerçek getiriyi yazıyor.
- Sayım kayıtta (checkpoint + oda başı) — ölüm kalıcı fiyatı delmiyor. F7 TAM GÜÇ ve zorluk raporu aynı formülü çağırıyor.
- **Kapı:** `Faz27SelfCheck` 8/8 · tüm kapılar 24/24 · 4 bozmanın 4'ü kırmızı (fiyat sunağa bağlı / azalma yok / kayıt sayımı taşımıyor / sunak sabit adım yazıyor).
- **Tam koşu (bot):**
| | Önce | Sonra |
|---|---|---|
| Bütün oyun | 8.6 dk | **13.6 dk** |
| Alım · hasar | 43 · 20.3 (×5.8) | **19 · 7.9 (×2.3)** |
| Boss (TAH · EKS · KEF · SİM · MUR) | 25 · 21 · 24 · 55 · 107 sn | 22 · 25 · 43 sn · **2.1 · 5.5 dk** |
| Saat bitecekti | hiç | 3 kez (son iki boss) |

**✅ Adım 2 — tempo (Enes onayladı, 2026-10-13): Faz 28 yapısıyla birlikte**
- **Oda ~1.5 dk → DALGALAR:** oda bileşimi 3–4 dalgada geliyor, dalga temizlenince sıradaki doğuyor, son dalgadan sonra kapı açılıyor. Düşmanları süngere çevirmek yerine (her biri 10 kat can) çeşit ve baskı artıyor.
- **Boss 3–8 dk:** havuz büyüyor + boss odasında saat akışı yavaşlıyor. Yoksa 6 dakikalık dövüşte saat (can) erir: bugün boss odasında ~1.6 sn/sn akıyor, 6 dk = 576 sn.
- ⚠️ **Sıra önerisi:** tempoyu YAPI (Faz 28) ile birlikte ayarlamak. Ara bölge başına 7 oda, mini boss'lar ve kontrol noktaları gelince odanın ve boss'un süresi de, saat ekonomisinin toplamı da yeniden ölçülecek; tempo bugünkü 4 odalı bölgeye göre ayarlanırsa iki kez ayarlanır.


> Enes: *"satın alma buffları hâlâ çok bozuk gibi. Oyunu yaklaşık 8-10 saate yayacağımız için ona göre bir dengeleme yaparız, bossları da ona göre düzenleriz."*

**Ölçüm — şimdiki sayılarla** (`Zorluk eğrisi raporu`, 2026-10-11). Bir koşu = 5 bölge × 6 oda (4 dövüş + sunak + boss) = 30 oda.
| Oyuncu | Sunaktan alıyor | Sonuç |
|---|---|---|
| TEMİZ | evet | koşuyu **294 sn** ile bitiriyor (sunağa 260 sn, 27 alım) |
| TEMİZ | hayır | bölge 2 boss'unda saat bitiyor |
| ORTA | evet / hayır | **ilk boss'ta** saat bitiyor |
- Bot (sunaktan almamış) KEFİL'i 5 dk'da öldüremedi.
- Israr ×5'te TAHSİLDAR'ın tek vuruşu **48 sn** (başlangıç saati 110).
- Hasat taraması: temiz oyuncunun kâra geçtiği en düşük oran %30, şimdiki %35.

**Ölçüm 2 — tam koşu (bot gerçek oyunda, atlamadan, 2026-10-12, `Otopilot — tam koşu ölçümü`)**
Bot neredeyse hiç vurulmayan iyi bir oyuncu gibi oynuyor (boss saldırılarının %1'inde vuruldu), sunaktan 40 sn yedekle alıyor.
| | Süre |
|---|---|
| **Bütün oyun — KAZANDI** | **8.6 dk** (saat hiç bitmedi, sonda 507 sn) |
| Bölge 1 · 2 · 3 · 4 · 5 | 1.4 · 1.4 · 1.3 · 1.8 · 2.7 dk |
| Boss | TAHSİLDAR 25 sn · EKSPER 21 sn · KEFİL 24 sn · SİMSAR 55 sn · MURABAHACI 107 sn |
| Dövüş odası | 6–21 sn |
| Sunak | 43 alım: hasar 3.5 → **20.3** (×5.8), hasat %70 (tavan), dash 0.10 sn |
**Sonuç:** oyun iki uçta da bozuk. Vuruş yiyen orta oyuncu ilk boss'ta ölüyor (zorluk raporu); vurulmayan ve alan oyuncu 9 dakikada bitiriyor, boss'lar 20 saniyede eriyor. Kartopu (alım → hasar → hasat → daha çok alım) dengeyi yutuyor.

**✅ Karar (Enes, 2026-10-12, düzeltme): TEK KOŞU = hikâye, taban 8 saat, ölümlerle ~10 saat.** *"hikayesi 8-10 saat sürecek."* Bir koşu baştan sona bir hikâye; ölümler ~2 saat ekliyor.
- **Ölçek:** şu an iyi oyuncu bütün oyunu 9 dk'da bitiriyor. 8 saat ≈ **50 kat**. Bu denge değil, YAPI ve İÇERİK işi.
- **❓ Açık — ölüm ne olacak?** Yasa "ölünce her şey gider" diyor; 8 saatlik koşuda bu 8 saati sıfırlamak olur. "Ölümlerle 10 saat" ölümün bir KONTROL NOKTASINA döndürdüğünü söylüyor (kaybedilen = o noktadan beri geçen süre). Yasanın bu maddesi değişecek mi?
- **❓ Açık — 50 kat nereden?** Kabaca: perde/bölge sayısı, bölge başına oda, oda ve boss süreleri. Örnek bir dağılım: 3 perde × 5 bölge × 10 oda, boss 3–5 dk, oda 1–2 dk ≈ 6–8 saat.
- **Faz 27'nin yeni kapsamı:** kartopunu kır (her durumda gerekli) + yavaş tempo (oda 1–2 dk, boss 3–5 dk). Yapı (perde/bölge/oda sayısı) ve içerik ayrı fazlar.

~~ilk anlayış:~~ "8–10 saat" = son boss'un İLK yenilmesine kadar toplam süre — *"zaten 1 kere yenince hikâye bitiyor."* Koşu kısa kalıyor, oyuncu birçok denemede öğrenip bitiriyor. Hedef: orta oyuncu ilk denemelerde bölge 2–3'e varsın, her denemede biraz daha ileri.
**✅ Karar:** ×5 ısrar (tek vuruş 48 sn) **kalsın**.

**Plan**
- [ ] **Hasat azalan getiri:** her alım bir öncekinden az versin. İlk sunakta fullemek mümkün olmasın, çok yatıran yine rahat geçsin (madde 3)
- [ ] **Hasar tavanı + kalıcı sunak fiyatı:** fiyat bölgeler arası sıfırlanmasın (yol haritası 3)
- [ ] **Bölge ölçeği:** her boss'tan sonra katlanarak zorlaşsın. Bir oda boss'un ¼'ü kadar terletmesin (madde 4). Kapı: oda masrafı / boss masrafı
- [ ] **Boss havuzları:** TAHSİLDAR tek atımda ölmesin, sunaktan az almış oyuncu boss'u makul sürede öldürebilsin (madde 4 + bot verisi)
- [ ] **Orta oyuncu hedefi** (a'ya göre): zorluk raporunda ORTA/ALICI en az bölge 2'yi bitirsin
- [ ] ZIRHLI kendi kendine normal düşman kadar hızlı eriyor (havuz ×1, akış zırhı tanımıyor): beklersen zırh boşa gidiyor. Akışa da zırh mı uygulansın? *(Faz 25 incelemesi)*
- [ ] Kapı: zorluk raporu bir öz-denetime dönüşür (hedefler sayı olarak, kırmızı/yeşil)

---

## 🗂️ Faz 29 ile birlikte (eski "Faz 20" listesi)

- [ ] **Faz 20 = boss sonrası sunak.** Boss'tan sonra ikinci bir sunak odası, silaha özel yetenekler satıyor (madde 8, 5)
- [ ] **FAİZLİ İCRA yeniden:** bedel saatinin yüzdesi, hasar ödenen bedele bağlı. Eşiğin altındaysan öldürmez. Eşiğin üstündeysen zaman vermez (masada soru 3)
- [ ] **İPOTEK:** dash-atak = şimdiki sağ tık (HACİZ İHBARI). Yeni sağ tık = büyük alan içinde düşmanların üstüne birkaç saniye peş peşe ipotek
- [ ] **KALKANLI** — kalkanın baktığı yönden hasar almaz. *Önerim:* rütbe olarak (ZIRHLI/İRİ gibi), yedi türün hepsine takılabilsin
- [ ] Boss'lara klasik şekiller dışında ekstra saldırılar — önce seçim düzeltmesinden sonra dağarcık yetiyor mu bak

## 📝 Sonraya notlar (Enes, 2026-09-30)

- **Ekstra oyun modu — tekrar oynanabilirlik için** (Enes, 2026-10-12). Oyun bittikten sonra bakılacak. **2026-10-15:** *"tekrar tekrar oynanabilen, eğlenceli, kısa süreli (yarım saatlik falan) bir mod"* — ~30 dk.
- **Silah yükseltmeleri büyüyecek** (Enes, 2026-10-15): *"şu anlık her silah için iki geliştirme iyi"* (şimdi silah başına 3, boss başına 2 alım) — ileride **her silahın en az 8 geliştirmesi**. Bazı geliştirmeler normalde combo yapmayan silahlar arasında combo açacak.
- **5 bölgenin tamamı** içerikle doldurulacak (Enes, 2026-10-15).
- Silahlar değişecek, yeni özellikler eklenecek (ileride bir revizyon)
- Her geçiş (oda, faz, boss girişi…) çok ani. Araya animasyon ve bekleme
- Oda şekilleri asset fazında her odada biraz değişecek (boyut + şekil)
- Oyuncu ileride daha çok güçlenmeli (final zorluğu şimdilik iyi)

---

## ✅ FAZ 24 — Denetim düzeltmeleri — **PLAYTEST ✅ 2026-09-30** (bulgular → Faz 25)

İki ajan kodu baştan sona taradı, ~30 hata buldu. Kaynaklar: dövüş (`Enemy`, `BossKind`, `PlayerController`, `Weapon`) ve akış/kayıt (`RoomManager`, `SaveFile`, `RevivalSystem`, `Clock`, `Hud`).

**Oyunun kuralını delenler**
- [x] 🐛 **Sonsuz diriliş:** tamir sunağının bedeli her ÇAĞRI'da geri geliyordu. Bedel artık checkpoint'ten de düşülüyor
- [x] 🐛 **GEÇİCİ DÖNÜŞ:** sayarken checkpoint yazılıyordu, sunak odasına yürüyünce dövüşmeden kazanılıyordu
- [x] 🐛 **Kazanılan koşu** kaydedilmiyordu. R ya da yeniden açış son checkpoint'e atıyordu
- [x] 🐛 **Zaferden sonra** patlayan bir işaret koşuyu öldürüp eşya harcatabiliyordu
- [x] 🐛 **Kayıt yazılırken çökme** dosyayı boşaltıyor, koşu sessizce siliniyordu. Artık yazma tek hamlede (atomik)

**Dövüş**
- [x] 🐛 Boss **zincirleri 2. halkada kesiliyordu** (faz 3'ün 4 halkalısı hiç tam oynamadı)
- [x] 🐛 **Faz geçişi saldırının ortasına** denk gelince telgraf eski saldırıyı gösteriyor, düz koni iniyordu
- [x] 🐛 **Geri sıçrayan saldırılar** önce sıçrayıp sonra vuruyordu, yani hiç tutmuyordu (SİMSAR)
- [x] 🐛 **Hit-stop** sırasında saldırı uyarısı dolmaya devam ediyor, darbe duruyordu. İkisi kayıyordu
- [x] 🐛 **Faz 3 rakipleri 7 sn** havuzla geliyordu. Artık kendi boss havuzlarının %70'i
- [x] 🐛 KEFİL yankısının **ikizi yankı sayılmıyordu**. Final onu beklemeden ilerliyordu
- [x] 🐛 **Uyuyan son boss** bembeyaz kalıyordu, hazırlığın ortasında uyursa uyanınca uyarısız vuruyordu
- [x] 🐛 **Uyuyan boss** tek hedefli vuruşları yutuyordu
- [x] 🐛 **HACİZ dash-atağı** boşluğa vuruyordu
- [x] 🐛 **İPOTEK** yeni odaya taşınıp orada patlıyordu
- [x] 🐛 **MÜBAŞİR** arenanın dışına çekilip odayı kilitleyebiliyordu
- [x] 🐛 **Nemesis defteri:** öldüren vuruş sayılmıyor, diriliş anında eksi yazılıyor, ikinci ölüm çift yazılıyordu. Boss'un yere bıraktığı işaret de artık sayılıyor
- [x] 🐛 **Oyun bittikten sonra** düşmanlar vurmaya, sarsıntı ve ses üretmeye devam ediyordu
- [x] 🐛 **Sağ tık özelleri** canlı ayarı hiç görmüyordu (Inspector ve F2–F4)
- [x] 🐛 **Kolda Y:** sunağın yanında hem alışveriş hem özel saldırı oluyordu
- [x] 🐛 **HALKA yerleşimi** oyundan çıkmıştı. `RoomCapture` de dosyaları yanlış adlandırıyordu

**Ekran**
- [x] GEÇİCİ DÖNÜŞ sırasında yanlış "SON CAN" uyarısı · sunak odasında "saat işliyor" yalanı · faz 3'te "FAZ 2" yazması · boss'un doğurduklarının süre etiketi kameraya dönmüyordu

**Fazlalık**
- [x] Ölü kod: `BeingPushed`, `CurrentStrikeInner`, `SwingTint`, `DashSwingTint`, `Mote._shared`, `_slamming` bayrağı (artık saldırıdan türüyor), `GroundMark.Update`'teki kopya mantık
- [x] Unity şablon artıkları: `Assets/TutorialInfo/`, `Readme.asset` (oyunla birlikte derleniyordu)
- [x] `Tuning.asset`'teki 11 ölü anahtar (VADELİ, dalga, eski yere vuruş)
- [x] Proje kökündeki 17 log dosyası (~3.5 MB) · `.gitignore`'da 16 ayrı satır → tek `/*.log`
- [x] Her yeniden başlatmada biriken çalışma anı varlıkları (`Resources.UnloadUnusedAssets`)

**İnceleme turu** (iki bağımsız ajan: doğruluk + güvenlik/veri kaybı) — kritik yok, şunlar eklendi:
- [x] 🐛 Tamir, checkpoint bedeli taşıyamıyorsa **satılmıyor** — yoksa checkpoint 0.5 sn'ye inip her ÇAĞRI anında ölüm oluyordu (benim ilk düzeltmemin açtığı delik). Sunak sebebi yazıyor
- [x] 🐛 Kayıt yer değiştiremezse (antivirüs kilidi) en yeni durum yan dosyada kalıyordu → `Load` artık önce yan dosyayı okuyor · yazma diske tam iniyor (`Flush(true)`)
- [x] 🐛 Kayda dokunan test tuşları (B, N, F5, F6, F1–F4) artık **yalnızca geliştirme build'inde** — kalkana kadar sürüm build'i güvende
- [x] 🐛 Zaferden sonra oyuncu da duruyor (düşmanlar duruyordu, oyuncu savurmaya devam ediyordu)
- [x] 🐛 Kapılar Play modunda çalıştırılamıyor (test kaydı oyunun kaydına karışırdı)
- [x] 🐛 Faz 18'in "aynı anda tek rakip" kontrolü **titrekti**: sıra rastgele, KEFİL ilk gelirse bölünüp 2 gövde sayılıyordu. Artık boss sayıyor

**Kapı:** `Faz24SelfCheck` — **20/20.** Her kontrol düzeltmenin ETKİSİNİ ölçüyor. Altı hatayı bilerek geri koydum: altısı da **kırmızıya döndü**, başka hiçbir şey düşmedi. Boş kural değil.
**Tüm kapılar: 21/21 yeşil — SONUÇ: TEMİZ** (2026-09-25). ⚠️ **Commit atılmadı**, senin onayını bekliyor.

⚠️ Faz 4'teki bir kontrol kazanılmış saati tekrar kullanıyordu. Düzeltmeden sonra doğru olarak kırmızıya döndü. Test taze saat alacak şekilde düzeltildi, `Clock.Init` artık zaferi de sıfırlıyor.

---

## 🧭 YOL HARİTASI — 2026-09-25

**Durum:** oyun planın çok önünde. Plan Kasım'da "çekirdek döngü oynanabilir" diyordu, 5 bölge, 5 boss, 7 düşman, 5 silah şimdiden var.
**Güzün işi:** büyütmek değil, **kilitlemek.** Hedef 22 Kasım'da **asset kapısının açık** olması.

| Sıra | Ne | Neden bu sırada | Bitti sayılır |
|---|---|---|---|
| **1** | **Birleşik playtest** ✅ (2026-09-30) + **Faz 25** düzeltmeleri | 10 faz test edilmeden üstüne yenisi kurulmaz | Faz 25 kapıları + playtest |
| **2** | **Faz 20** — silaha özel sunak | Tasarlandı, yazılmadı. Silah seçimi sunakta da karar olsun | Kuşandığın silaha göre değişen 4. sunak |
| **3** | **Buff enflasyonunun 2. ayağı** | Hasar yükseltmesi tavansız (×6.3). Sunak fiyatı odalar arası sıfırlanıyor | `altarDamageStep` tavanı + kalıcı fiyat |
| **4** | **Ayar oturumu** *(sen + ben)* | A/B aracı (F1–F4) hazır bekliyor. Zorluk "çok yüksek" diye ertelenmişti | Profil seçildi, sayılar `Tuning.asset`'te |
| **5** | **Zamanlama kilidi** | Animasyon bu sayılara göre yapılır. Önce kilit, sonra animasyon | Saldırı/dash süreleri `TASARIM.md`'de kilitli |
| **6** | **İlk oyuncu build'i** (Windows) | Editörde çalışan her şey build'de çalışmayabilir. Örnek: URP Unlit shader'ı build'de düşebilir, efektlerin toplanarak parlaması (additive) kaybolur | `.exe` açılıyor, bir koşu oynanıyor |
| — | 🚩 **22 Kasım: asset kapısı açık** *(Çakmak arası 2026-09-30'da kaldırıldı; tarih playtest bulgularıyla yeniden bakılacak)* | kadro ✅ · silahlar ✅ · zamanlama · kamera ✅ | |
| **7** | Asset ve animasyon *(Aralık–Şubat)* | Önce silüet: ZIRHLI kalın, İRİ büyük. Düşmanın ne yaptığı duruşundan okunmalı, renginden değil | Bir bölge modelli |
| **8** | Oyun kabuğu | Ana menü, duraklatma, ayarlar, kol desteği (İPOTEK nişanı), kayıt sürümü | Oyuncu menüden koşu başlatıyor |
| **9** | **Dikey dilim** *(Mart)* | Bir bölge tam cilalı: model, ses, VFX, hikâye girişi | Başkasına oynatılabilir |
| **10** | **Demo** *(Nisan)* | itch.io'da ya da Steam sayfasıyla | İndirilebilir |
| **11** | Hikâye içeriği · EA | "Ölümle şekillenmiş dünya" (betikli varyasyonlar), 3 perde. EA = giriş + gelişme | — |

**Açık tasarım soruları** (acil değil, sırası gelince): 5 bölge mi, 3 perde mi (`regionsPerAct = 5` test değeri) · borç kapatılabilir mi (alternatif son) · nihai isim.

**Öneri, karar değil:** graphify çıktısı `Assets/_Project/Scripts/graphify-out/` içinde. 614 dosya, 7 MB, Unity her açılışta hepsini içeri alıyor. Çıktıyı `Assets` dışına almak ya da klasör adını `graphify-out~` yapmak (Unity `~` ile biteni yok sayar) açılışı hızlandırır.

---

## 🚨 TESLİMDEN ÖNCE KALKACAK — test kolaylıkları

> Erken erişime debug tuşlarıyla çıkmak gerçek bir hata. Liste 2026-09-25'te **denetimle tamamlandı**, eskisi 8 maddeyi kaçırıyordu.
> Ara önlem: B, N, F5, F6, F1–F4 artık `Debug.isDebugBuild` ile kilitli (sürüm build'inde çalışmıyor). **Yine de kalkacaklar.**

- [ ] **B** — boss odasına atla *(`RoomManager.cs`, `SkipToBossPressed`)*
- [ ] **N** — sıradaki bölgeye atla *(`RoomManager.cs`, `SkipRegionPressed`)*
- [ ] **1–5** — anlık silah değiştirme *(`PlayerController.ReadWeaponSwap`)*. Gerçek oyunda silahlar yolda bulunuyor
- [ ] **F5** — saati doldur *(`RoomManager`, `TestRefillSeconds`)* — ⚠️ eski listede yoktu
- [ ] **F7** — TAM GÜÇ *(`RoomManager.FullPower`)* — Faz 25'te eklendi
- [ ] **F6** — sıfırdan koşu *(`RoomManager`)* — ⚠️ eski listede yoktu. **Onaysız** kaydı siliyor ve F5'in yanında: playtest'te yanlış tuş koşuyu siler
- [ ] **F1–F4** — A/B profilleri *(`ProfileSwitcher`, `TuneProfile`, `Bootstrap`, `Hud`)* — ⚠️ eski listede yoktu. Build'de F2–F4 **kaydı sessizce kapatıyor**. `TuneProfile` ayrıca her 0.2 sn'de 118 alanı yansımayla tarıyor
- [ ] HUD'daki test tuşu açıklaması ve "koşu kirli" yazısı *(`Hud.cs`)*
- [ ] **Oda hesabı** ölçüm aracı *(`Hud`, `RoomManager` "ODA HESABI" logu)* — kendisi "gerçek oyunda kalkacak" diyor
- [ ] `SaveFile.MarkTestJump` / `TestJumpUsed` altyapısı — kısayollar gidince ölü kalır
- [ ] `testDefianceCount` / `testLastStandCount` / `testRecallCount` *(`Tuning`)* — diriliş eşyaları HİKÂYEDEN gelecek
- [ ] `regionsPerAct = 5` — gerçek değer koşu uzunluğu ayarlanınca
- [ ] `enemyCount` / `Bootstrap.RingPosition` — yalnızca Faz 1 öz-denetimi kullanıyor
- [ ] *(İsteğe bağlı)* olay başına `Debug.Log`'lar

---

## 📝 Asset ve animasyon fazına notlar
- Renk **ayırt ettiriyor ama anlatmıyor** (Enes). Animasyonun birinci işi: her türün ne yaptığı duruşundan okunmalı
- ZIRHLI ile İRİ silüetten ayrılmalı: ZIRHLI *kalın*, İRİ *büyük ve ağır*
- Nemesis borcu gövdeden de okunmalı (şu an ölçekle)
- Karakter 1080p'de **~77 piksel**. Para silüete ve renge harcanmalı, yüz detayı görünmez *(kamera kilidi raporu)*
- Sesler ve akış/dash animasyonları geçici
