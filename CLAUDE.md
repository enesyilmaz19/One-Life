# Tek Can — Proje Talimatları

Unity **6000.5.2f1** · URP · **izometrik 3B** · aksiyon roguelike · Steam / erken erişim hedefi.

## Yasa (her tasarım kararı buna sorulur)

> **Tek bir sayı var: saniye. Canın da o, süren de o, paran da o.**

Can barı **yok ve yazılmayacak.** Altın/ruh/ayrı para birimi **yok.** Meta ilerleme **yok** — ölünce her şey gider.

Bir özellik bu yasayı bozuyorsa özellik gider, yasa kalır.

## Belgeler

- `tasks/todo.md` — fazlar, ilerleme, **📥 ENES'İN MASASI** bloğu
- `docs/TASARIM.md` — tasarım, kararlaştırılanlar, açık kararlar
- `../FIKIR_DEFTERI.md` — konseptler, silah tohumları, **elenen fikirler ve neden** (tekrar önerme)

## Çalışma kuralları

- **Her faz sonunda dur.** Ne yapıldığını ve nasıl test edileceğini anlat, Enes'in onayını bekle. Yeşil testler "faz tamam" demek değil.
- Enes'e düşen işler `tasks/todo.md` içindeki ENES'İN MASASI bloğunda tutulur **ve mesajda ayrıca yazılır** — dosyaya gömmek yetmez.
- **Klavye girdisi otomasyondan Unity play-mode'a geçmiyor.** His testini insan yapar; ajan log + ekran görüntüsüyle doğrular. Kanıt: `Logs/Editor.log`.
- Görsel kimlik **silah ve VFX** üzerine kurulu, karakter sade ve okunur. VFX sanatçı gerektirmeyen tek sanat dalı — kimliği oraya yasla.

## Ayar kaynağı — tek kural

**`Tuning.asset` çalışma anının tek gerçeğidir.** `Tuning.cs`'teki varsayılanlar yalnızca
asset'te **henüz olmayan** yeni alanların tohumudur; var olan bir alanı .cs'ten değiştirmek
hiçbir işe yaramaz, asset onu ezer.

- **His/denge değeri değiştireceksen → Inspector.**
- **C# varsayılanlarını asset'e taşımak istiyorsan → `Tek Can → Ayarları senkronla (C# → asset)`.**
  Tek yönlü ve elle tetiklenir: otomatik olsaydı biri .cs'e dokunduğu an Inspector'da
  ayarlanmış bütün his değerleri sessizce silinirdi.
- **Sessiz sapma imkânsız:** `TuningAudit` her script derlemesinden sonra kendiliğinden
  çalışır ve ayrışma varsa Console'a uyarı basar. (2026-08-24'te bir playtest seansı
  bu uyarı olmadığı için yanlış varsayımla geçti.)

2026-08-24'te tam senkron yapıldı; asset artık 55 alanın hepsini içeriyor.

## Doğrulama — tek komut

`Tek Can → TÜM kapıları çalıştır`, ya da batch'ten:
`-executeMethod TekCan.EditorTools.AllChecks.Run`

Faz 1 + Faz 2 öz-denetimleri, **hasar reprodüksiyonu** ve ayar denetimini **tek Unity
açılışında** çalıştırır; bir kontrol düşerse 1 ile çıkar. (Ayar sapması yalnızca uyarı —
hangi değerin doğru olduğu Enes'in kararı.)

Ayrı araçlar: `Denge simülasyonu` (hasat oranı taraması) · `Geri bildirim ekran görüntüsü al`.

## graphify

Kod yazıldıkça:

```bash
graphify update Assets/_Project/Scripts
```

⚠️ Proje **kökünden** çalıştırma — Unity `Library/` cache'ini (binlerce dosya) tarayıp timeout eder. Her zaman `Assets/_Project/Scripts` ver.

Mimari sorularından önce `graphify-out/GRAPH_REPORT.md` oku; modüller arası "X ile Y nasıl ilişkili" sorularında grep yerine `graphify query` / `path` / `explain` kullan.

## Unity başsız çalıştırma

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.2f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath "C:\Users\Enesin pc\UnityProjects\Tek Can" -logFile <log>
```
