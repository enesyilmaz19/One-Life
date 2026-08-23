# Tek Can — Görevler

## 📥 ENES'İN MASASI

- [ ] **Faz 1 bittiğinde oynayıp his onayı vereceksin.** Klavye girdisi otomasyondan Unity play-mode'a geçmiyor — hareketin ve vuruşun nasıl hissettirdiğini sadece sen söyleyebilirsin. Ben log + ekran görüntüsüyle doğrularım, "iyi hissettiriyor mu" sorusunun cevabı sende.
- [ ] *(şimdilik başka bir şey yok — Faz 0 ve 1'de bana düşen kısım var)*

---

## Faz 0 — Kurulum ✅

- [x] Unity projesi oluşturuldu (`UnityProjects/Tek Can`, 6000.5.2f1, **URP**, izometrik 3B)
- [x] Git deposu + Unity `.gitignore` + ilk commit
- [x] Klasör iskeleti: `Assets/_Project/{Scripts,Art,Prefabs,Scenes}`
- [x] Proje `CLAUDE.md`'si
- [x] graphify kuruldu — `Assets/_Project/Scripts/graphify-out/` (78 düğüm, 108 kenar, 10 topluluk)

---

## Faz 1 — Dövüş hissi ⬅️ **KOD YAZILDI, PLAYTEST BEKLİYOR**

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
- [ ] 🎮 **PLAYTEST KAPISI** — 2026-08-24'te denendi, **ÇALIŞMADI**

### ❌ Faz 1 kapanmadı — geri bildirim eksik (2026-08-24 playtest)

Enes'in raporu: *"Dash tamam çalışıyor, düşmanlar bana vurunca zaman gidiyor tamam. Ben saldırı yaptığımı ya da onların bana saldırı yaptığını göremiyorum."*

Sonuç: 5 değerlendirme sorusundan **3, 4, 5 sorulamadı**. Sebebi ve dersi `lessons.md`'de.

**Yapılacaklar — sıra önemli, geri bildirim önce:**

- [ ] **Oyuncu savurma yayı** — saldırı konisi görünsün (dönen mesh ya da iz). Vurduğunu görmeden hissi yargılayamaz
- [ ] **Düşman hazırlık hareketi (telegraph)** — vuruştan önce görünür uyarı. Bu olmadan dodge tahmin, i-frame değerlendirilemez
- [ ] **Düşman saldırı görseli** — darbenin kendisi görünsün
- [ ] **Oyuncu hasar tepkisi** — parlama/geri tepme, "yedim" anı okunmalı
- [ ] Saldırı menzili ve konisi debug çizimi (Gizmos) — ayar yaparken görünsün
- [ ] 🎮 **PLAYTEST TEKRAR** — bu sefer 5 sorunun beşi de sorulabilir olacak

## Faz 2 — Saat ekonomisi

- [ ] Saatin gerçek dengesi: öldürme kazancı / darbe kaybı oranları
- [ ] Saat göstergesi — okunur ve dramatik. **Bu oyunun HUD'ı bu, tek gösterge**
- [ ] Saniyeyle satın alma (dükkân/sunak)
- [ ] Sıfır → koşu bitişi ekranı — **`Clock.OnDeath`'e ya burada abone ol ya da olayı sil.** Şu an abonesi yok, ölü kod (2026-08-24 öz-incelemesinde bulundu)
- [ ] Denge testi: deterministik simülasyonla kazanç/kayıp oranlarını tara

## Faz 3 — Silahlar

- [ ] 3 silah tohumu prototipi: **Faizli** · **Durgun** · **Vadeli** (bkz. `../../FIKIR_DEFTERI.md`)
- [ ] Silah başına VFX kimliği
- [ ] 🎮 Playtest kapısı — hangi silah tutuyor, hangisi çöp

## Faz 4 — Koşu yapısı

- [ ] Oda zinciri + oda başına "girer miyim" risk kararı
- [ ] Düşman çeşitliliği (meta ilerleme yok → tekrar oynanabilirlik tamamen buradan gelir)
- [ ] Koşu sonu / yeniden başlama akışı

---

## Notlar

- Fikir defteri (ikinci oyun "Kaos" dâhil): `../../FIKIR_DEFTERI.md`
- Tasarım: `docs/TASARIM.md`
- **Her fazın sonunda:** ne yapıldığını ve nasıl test edileceğini anlat, dur, Enes'in onayını bekle. Yeşil testler "faz tamam" demek değil.
- **En büyük risk:** içerik çeşitliliği. Meta ilerleme olmadığı için tekrar oynanabilirliğin tamamı koşunun *içinden* gelmek zorunda.
