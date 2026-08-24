## 📥 ENES'İN MASASI

*Şu an sende bekleyen iş yok.* Faz 2 kapandı, Faz 3'e geçilmedi — Enes "kal burda" dedi.

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

## Faz 3 — Silahlar

- [ ] **TEK HEDEFLİ silahlar** — koni herkese vurup her birinden hasat ettiği için kalabalık oyunu KOLAYLAŞTIRIYOR. Tek hedefli silah ancak **hasar başına daha çok zaman çalarsa** rekabet edebilir → hasat oranı silaha göre değişmeli
- [ ] 3 silah tohumu prototipi: **Faizli** · **Durgun** · **Vadeli** (bkz. `../../FIKIR_DEFTERI.md`)
- [ ] Silah başına VFX kimliği
- [ ] 🎮 Playtest kapısı — hangi silah tutuyor, hangisi çöp

## Faz 4 — Koşu yapısı

- [ ] Oda zinciri + oda başına "girer miyim" risk kararı
- [ ] Düşman çeşitliliği (meta ilerleme yok → tekrar oynanabilirlik tamamen buradan gelir)
- [ ] Koşu sonu / yeniden başlama akışı

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
