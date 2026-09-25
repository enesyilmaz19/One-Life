## 📥 ENES'İN MASASI — 2026-09-25

> Tarihçe (Faz 0–23, eski playtest raporları) → [arsiv.md](arsiv.md). Hiçbir şey silinmedi.

| # | Ne | Nasıl | Nereye |
|---|---|---|---|
| 1 | 🚨 **Yedek** | github.com → New → **Private** → ad `tek-can`. Adresi bana ver, bağlayıp gönderirim | — |
| 2 | 🎮 **Birleşik playtest** | Aşağıdaki 12 madde, **tek oturum**. Her maddeye "evet / hayır / bir cümle" yeter | Mesajla |
| 3 | 🔴 **Karar: kapat-aç açığı** (aşağıda K1) | a / b / c seç | Mesajla |
| 4 | 🔴 **Karar: checkpoint tabanı** (K2) | evet / hayır | Mesajla |
| 5 | 🔴 **Karar: sunak odasında bekleme süreleri** (K3) | dursun / dursun-mayın | Mesajla |
| 6 | ✏️ **Adlandırma** | Kahraman, 5 boss, 5 bölge, 3 diriliş eşyası. Şimdikiler çalışma adı | `docs/TASARIM.md` "Açık" |
| 7 | 🖼️ **Referans topla** | Beğendiğin izometrik oyunlardan ekran görüntüsü (Hades, Death's Door…). Asset fazını hızlandırır | Bir klasör, yeri sen seç |

---

## 🔴 Karar bekleyenler

**K1 — Ölmeden önce oyunu kapatmak.** Şu an kayıt sadece checkpoint'i tutuyor. Saatin 3 sn'ye düştü, etrafın sarılı. Alt+F4 → oyunu aç → checkpoint'e **tam zamanla** dönüyorsun. Eşya harcanmıyor, ölüm sayılmıyor. `TASARIM.md` bu tuzağı zaten adıyla yazmış ("oyuncu ölmeden oyunu kapatır…").
- **a)** Checkpoint'ten çıkınca "koşu sürüyor" işareti yazılır. Açılışta bu işaret varsa **ölüm sayılır** (eşya düşer). En sert. Çökme de ölüm olur.
- **b)** Her oda girişinde durum kaydedilir. Kapatırsan **o odanın başına** dönersin, girişteki zamanla. Açık küçülür ama kapanmaz: bir oda içinde hâlâ kaçabilirsin.
- **c)** Böyle kalsın (Hades gibi, kapatmak serbest).
- *Önerim:* **b** — sertlik ile çökme adaleti arasında denge. **a** "tek can" adına en dürüst olan.

**K2 — Checkpoint birkaç saniyeyle kaydedilince.** Checkpoint odasına 4 sn ile girersen o 4 sn kaydediliyor. Sonraki her ÇAĞRI seni 4 sn ile düşmanların yanına atıyor, eşyalar saniyeler içinde eriyor. *Önerim:* ÇAĞRI'nın geri verdiği zamana taban (ÖLÜM ALDATMA'daki `defianceMinSeconds` gibi). *(GEÇİCİ DÖNÜŞ sırasındaki checkpoint yazma açığı kapatıldı, bu ayrı.)*

**K3 — Sunak odasında bekleme süreleri.** Saat duruyor ama özel saldırı/dash bekleme süreleri işliyor. Yani boss'a özelin hazır girebiliyorsun. Bu kasıtlı. Durmasını ister misin?

---

## 🎮 BİRLEŞİK PLAYTEST — tek oturum

> Faz 12–24'ün bekleyen testleri **60 soruydu**. Cevabı bir kararı değiştirenler kaldı: **12 madde.**
> Sıra oyunun akışına göre. Önce **F6** (sıfırdan koşu), bölge 1'i **test tuşu kullanmadan** oyna.
> Her madde: **ne yap → neye bak → ne olursa ne demek.**

**A — Bölge 1, normal oyna**

1. **Odalar.** Dört odayı geç. *Bak:* dizilişler (halka, duvar, yumak, kanat, dağınık) birbirinden ayrışıyor mu? HALKA artık gerçekten geliyor, önceden hiç gelmiyordu. *Hayırsa:* yerleşimler yeniden çizilir.
2. **ZIRHLI ve İRİ.** *Bak:* ZIRHLI görünce "bu oda uzun sürecek", İRİ görünce "önce bunu halletmeliyim" diyor musun? *Hayırsa:* ayrım asset fazına kalır (şu an ikisi de kapsül).
3. **Sunak odası** (boss'tan önceki, düşmansız, "SAAT DURDU"). *Bak:* alışveriş gerçek bir karar mı, yoksa "hepsini al" mı oldu? *"Hepsini al" ise:* fiyat eğrisi sertleşir.
4. **Boss 1 (TAHSİLDAR).** Uzakta dur, sonra dibine gir. *Bak:* uzakta menzilli, yakında yakın saldırı yapıyor mu? Boss 2'den **hafif** mi? *Hayırsa:* bu kadran ayarlanır.

**B — Silahlar** *(1–5 tuşları koşuyu kirletmez, serbest)*

5. **Sağ tıklar.** Beşini de dene. *Bak:* beş farklı şey mi, yoksa "hepsi büyük vuruş" mu? FAİZLİ'nin İCRA'sını ıskalamak canını yakıyor mu (yakmalı)?
6. **HACİZ dibindekine.** Bir düşmanı dibine al, **dash + sol tık.** *Bak:* artık ona vuruyor. Önceden boşluğa vuruyordu (düzeltildi). Bu hamle kendiliğinden aklına geliyor mu?
7. **İPOTEK.** Koşarken dash + saldırı: *arkanda hiçbir şey çıkmamalı.* Sonra kapıya yakın bir ipotek koy ve hemen geç: **yeni odada patlamamalı** (düzeltildi).

**C — Boss'lar** *(N / B ile gez; koşu kirlenir, sonra F6)*

8. **SİMSAR'ın DEVİR'i.** *Bak:* vurup geri sıçrıyor. Önceden önce sıçrıyor, sonra boşluğa vuruyordu, yani **hiç tutmuyordu** (düzeltildi). Şimdi adil mi, sert mi?
9. **KEFİL'in simidi.** Yeşil göbekte dur, faz 2'ye tam geçerken bile. *Bak:* göbekte **vurulmamalısın.** Önceden faz geçişi o anki saldırıyı düz koniye çeviriyordu (düzeltildi).
10. **⚠️ FİNAL — MURABAHACI faz 3.** Üç şey değişti:
    - 4 halkalı zincir (halka → simit → açıklık → ayna) artık **sonuna kadar** oynuyor. Önceden 2. halkada kesiliyordu.
    - Çağırdığı eski boss'lar artık **gerçek havuzla** geliyor. Önceden 7 sn'ydiler, 2 vuruşta ölüyorlardı.
    - Uyurken **soluk** görünmeli. Önceden bembeyaz kalıyordu.
    *Bak:* final **fazla mı uzun?** Uzunsa kadran `rivalPoolMultiplier` (şu an 0.7), Inspector'dan, F1'de.

**D — Ölüm**

11. **Nemesis.** Bir boss'a bilerek öl, ona dön. *Bak:* gövdesinden büyüdüğü anlaşılıyor mu? Aynı boss'a iki kez ölünce üçüncüsü imkânsız mı geliyor? *(Hesap artık doğru tutuluyor: öldüren vuruş sayılıyor, ikinci ölüm çift yazılmıyor.)*
12. **Son eşya.** Eşyaların bitmek üzereyken: *Bak:* uyarı yeterince bağırıyor mu? GEÇİCİ DÖNÜŞ sayarken artık "SON CAN" yazmamalı (o an ölmek eşya harcamıyor).

---

## ✅ FAZ 24 — Denetim düzeltmeleri — **KOD BİTTİ, PLAYTEST BEKLİYOR** (2026-09-25)

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
| **1** | **Birleşik playtest** *(sen)* + bulgular | 10 faz test edilmeden üstüne yenisi kurulmaz | 12 maddenin cevabı + düzeltmeler |
| **2** | **Faz 20** — silaha özel sunak | Tasarlandı, yazılmadı. Silah seçimi sunakta da karar olsun | Kuşandığın silaha göre değişen 4. sunak |
| **3** | **Buff enflasyonunun 2. ayağı** | Hasar yükseltmesi tavansız (×6.3). Sunak fiyatı odalar arası sıfırlanıyor | `altarDamageStep` tavanı + kalıcı fiyat |
| **4** | **Ayar oturumu** *(sen + ben)* | A/B aracı (F1–F4) hazır bekliyor. Zorluk "çok yüksek" diye ertelenmişti | Profil seçildi, sayılar `Tuning.asset`'te |
| **5** | **Zamanlama kilidi** | Animasyon bu sayılara göre yapılır. Önce kilit, sonra animasyon | Saldırı/dash süreleri `TASARIM.md`'de kilitli |
| **6** | **İlk oyuncu build'i** (Windows) | Editörde çalışan her şey build'de çalışmayabilir. Örnek: URP Unlit shader'ı build'de düşebilir, efektlerin toplanarak parlaması (additive) kaybolur | `.exe` açılıyor, bir koşu oynanıyor |
| — | 🚩 **22 Kasım: asset kapısı açık** | kadro ✅ · silahlar ✅ · zamanlama · kamera ✅ | |
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
