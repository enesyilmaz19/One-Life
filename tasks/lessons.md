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

## KURAL

> **Bir fazı playtest'e hazır ilan etmeden önce sor: oyuncu, yargılamasını istediğim şeyi algılayabiliyor mu?**

"Vuruş iyi hissettiriyor mu?" diye soracaksan **görünür bir vuruş olmak zorunda.** Fazın hedefini doğrula, hedefin kolay alt kümesini değil.

**Somut sonucu:** dövüş fazlarında **ilk yazılacak şey geri bildirimdir** — savurma yayı, düşman hazırlık hareketi, isabet tepkisi. Sistemler taslak kalabilir; geri bildirim kalamaz, çünkü değerlendirilen ürün geri bildirimin kendisidir.
