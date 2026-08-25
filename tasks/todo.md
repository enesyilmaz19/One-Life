## 📥 ENES'İN MASASI

**Boss'u tekrar dene.** Artık iki saldırıda bir **yere vuruyor**: etrafında kırmızı halka çıkıyor.

- [ ] **Durup vurmak artık öldürüyor mu?** Simülasyona göre 10. saniyede ölmelisin
- [ ] **Halkadan kaçmak yetişiyor mu?** Hazırlık süresi adil mi, yoksa çok mu kısa?
- [ ] **Halka yarıçapı (3.8) doğru mu?** Çıkmak çok mu uzun sürüyor?
- [ ] **Boss artık zor mu?** Simülasyon "kaçarsan 3.6 saniyeyle kazanırsın" diyor. Gerçekte nasıl?
- [ ] **Sunaktan keskinlik almak fark ediyor mu?** Boss'un zoru yatırım yapmayanı cezalandırmak için ayarlandı

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
- [ ] Gerçek VFX (parçacık, iz, boşalma efekti) — Faz 3te silahlarla birlikte
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
- [ ] Ses — hâlâ yok, vuruş hissinin kalan yarısı

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

## Faz 4 — Koşu yapısı ⬅️ **KOD YAZILDI, PLAYTEST BEKLİYOR**

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
- [ ] 🎮 **PLAYTEST TURU 2** — masadaki sorular
- [ ] ⚠️ **ENTER geçici** — gerçek oyunda dalgalar oyuncu tarafından çağrılmayacak, hikâye/mekân akışına bağlanacak
- [x] ✅ **Düşman çeşitliliği — Faz 5'te çözüldü** (aşağıda)

## Faz 5 — Düşman türleri ⬅️ **KOD YAZILDI, PLAYTEST BEKLİYOR**

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
- [ ] 🎮 **PLAYTEST KAPISI** — masadaki sorular
- [ ] Oda zinciri / uzamsal ilerleme — hikâye kararından sonra
- [ ] Ses — hâlâ yok

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
