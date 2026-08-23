# Tek Can — Görevler

## 📥 ENES'İN MASASI

- [ ] **Bakış açısı kararı** — izometrik/tepeden mi, 2.5D yandan mı? Önerim izometrik (gerekçe: `docs/TASARIM.md` → Bakış açısı). Bugün bedava, bir ay sonra pahalı. → cevabı buraya
- [ ] **Unity projesini ben mi açayım, sen Hub'dan mı açacaksın?** (bu klasöre, 6000.5.2f1, URP) → cevabı buraya

---

## Faz 0 — Kurulum (başlamadı)

- [ ] Unity projesi oluştur (`UnityProjects/Tek Can`, 6000.5.2f1, URP)
- [ ] Git deposu + `.gitignore` (Unity şablonu)
- [ ] graphify'ı proje içine kur, `graphify-out/` üret
- [ ] Proje `CLAUDE.md`'sini yaz (ev dizinindeki ölü olan silindi)
- [ ] Klasör iskeleti: `Assets/_Project/{Scripts,Art,Prefabs,Scenes}`

## Faz 1 — Saat (ilk oynanabilir çekirdek)

Hedef: **tek sayının çalıştığını görmek.** Görsel yok, düşman yok, sadece sistem.

- [ ] `Clock` — tek kaynak: azalır, darbe düşürür, öldürme yükseltir
- [ ] Ekranda saat göstergesi (okunur, dramatik — bu oyunun HUD'ı)
- [ ] Test sahnesi: bir düşman, bir vuruş, saatin hareket ettiğini gör
- [ ] Sıfır → koşu bitişi

## Faz 2 — Dövüş hissi

Hedef: **vurmak iyi hissettirsin.** Enes'in 2. Steam kriteri burada kazanılır ya da kaybedilir.

- [ ] Hareket + dash
- [ ] Temel saldırı, hit-stop, ekran sarsıntısı, VFX
- [ ] Tek düşman tipi, saldırgan oynamayı ödüllendiren yapay zekâ
- [ ] 🎮 **PLAYTEST KAPISI — Enes oynar, his onaylanmadan Faz 3 başlamaz**

## Faz 3 — Ekonomi

- [ ] Saniyeyle satın alma (dükkân/sunak)
- [ ] 3 silah tohumu prototipi (Faizli · Durgun · Vadeli)
- [ ] Risk/ödül döngüsü: oda başına "girer miyim" kararı

## Faz 4 — Koşu yapısı

- [ ] Oda zinciri + koşu sonu
- [ ] Düşman çeşitliliği
- [ ] Denge: saniye kazanç/kayıp oranları

---

## Notlar

- Fikir defteri (ikinci oyun "Kaos" dâhil): `../../FIKIR_DEFTERI.md`
- Tasarım: `docs/TASARIM.md`
- **Her fazın sonunda:** ne yapıldığını ve nasıl test edileceğini anlat, dur, Enes'in onayını bekle. Yeşil testler "faz tamam" demek değil.
