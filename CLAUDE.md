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
