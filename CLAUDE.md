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

- **His/denge değeri değiştireceksen → Inspector.** Oyun asset'in bir **çalışma
  kopyasıyla** koşuyor (A/B profilleri asset'i bozmasın diye), ama **MEVCUT profilinde
  Inspector değişiklikleri canlı geçmeye devam ediyor** — 0.2 sn'de bir içeri alınıyor.
  ⚠️ Bir A/B profili (F2-F4) seçiliyken köprü KAPALI: kopya bilerek ayrışmış durumda.
  Inspector'dan ayar yapacaksan **F1'de ol.**
- **C# varsayılanlarını asset'e taşımak istiyorsan → `Tek Can → Ayarları senkronla (C# → asset)`.**
  Tek yönlü ve elle tetiklenir: otomatik olsaydı biri .cs'e dokunduğu an Inspector'da
  ayarlanmış bütün his değerleri sessizce silinirdi.
- **Sessiz sapma imkânsız:** `TuningAudit` her script derlemesinden sonra kendiliğinden
  çalışır ve ayrışma varsa Console'a uyarı basar. (2026-08-24'te bir playtest seansı
  bu uyarı olmadığı için yanlış varsayımla geçti.)

⚠️ **Kuralın istisnası — asset'te HENÜZ OLMAYAN alanlar.** 2026-08-24'te tam senkron
yapılmıştı, ama sonradan eklenen alanlar asset'e hiç yazılmadı: **118 alanın 49'u
dosyada yok** (2026-09-25; araç sayıyor, tahmin değil — güncel sayı `TuningAudit` çıktısında). O alanlarda kural TERSİNE işliyor — asset
ezecek bir kayıt taşımadığı için **.cs değişikliği uygulanıyor.** Oda/bölge büyümesi,
düşman rütbesi, checkpoint/diriliş, Faz 13 tür kancaları ve HACİZ / İPOTEK / İFLAS
kadranlarının **hepsi** bu grupta — yani ayar oturumunun kadranlarının çoğu.

`TuningAudit` artık hangi alanların dosyada olmadığını da yazıyor. Inspector'dan bir
kere düzenlersen asset'e yazılır ve o andan sonra normal kurala döner.
**`Ayarları senkronla`yı çalıştırma** — tek yönlü ve TOPLU, Inspector'da ayarlanmış
80 alanın hepsini C# varsayılanına geri çeker.

## Doğrulama — tek komut

`Tek Can → TÜM kapıları çalıştır`, ya da batch'ten:
`-executeMethod TekCan.EditorTools.AllChecks.Run`

**Bütün** faz öz-denetimlerini, **hasar** ve **boss isabet** reprodüksiyonlarını ve ayar
denetimini **tek Unity açılışında** çalıştırır; bir kontrol düşerse 1 ile çıkar.
*(Yeni faz eklerken `AllChecks.Run` içine bir satır ekle — buradaki sayı eskimesin diye
faz numarası yazılmıyor.)* (Ayar sapması yalnızca uyarı —
hangi değerin doğru olduğu Enes'in kararı.)

**Otopilot — oyunu GERÇEK Play modunda oynayan bot** (Faz 25b). Kapılar zamanı elle
ilerletiyor; otopilot oyunun kendi karelerinde, Unity'nin SANAL klavye/faresiyle oynuyor
(işletim sistemi tuşları Play'e geçmiyor, sanal cihaz geçiyor). Enes'in kaydına dokunmaz.
- İzle: `Tek Can → Otopilot (izle)` — gerçek hızda, ekranda yazılı, ekran görüntülü
- Faz 26 playtest'i: `Tek Can → Otopilot — Faz 26 playtest (izle)` ya da batch `…OtopilotMenu.Playtest26` (~15 dk) — todo'daki 7 maddeyi oynayıp ölçer, sonunda saldırı başına vurulma tablosu
- Batch: `-executeMethod TekCan.EditorTools.OtopilotMenu.Batch` — **`-quit` YOK**, bot kendisi çıkar, ~12 dk
- Çıktı: `Logs/Otopilot/<zaman>/rapor.md` + görüntüler. Uzun sürdüğü için `AllChecks`'te değil.
- ⚠️ Batch'te pencere odakta olmuyor: bot girdi ayarının kopyasında `backgroundBehavior = IgnoreFocus` açıyor (yoksa tuşlar düşüyor).

Ayrı araçlar (kapı değil, rapor): `Denge simülasyonu` (hasat oranı taraması) ·
`Zorluk eğrisi raporu` (her bölge/oda için kazanç–masraf kırılımı + kaçınma pencereleri) ·
`Geri bildirim ekran görüntüsü al` · `Oda yerleşimlerini çek`.

⚠️ **Kayıt dosyası korumalı:** `SaveFile` yalnızca `Application.isPlaying` iken oyuncunun
dosyasına yazar; editör ve batch modda kendiliğinden geçici dosyaya gider. Yeni araç
yazarken bir şey hatırlaman gerekmiyor. (`PathOverride` yine de var — bilinen bir dosya
gerekiyorsa, örneğin `Faz9SelfCheck`'in diske yazıp geri okuma turu.)

Sebep: öz-denetimler ve raporlar checkpoint yazdırıyor. Koruma yokken `AllChecks`
çalıştırmak oyuncunun koşusunu siliyordu — 2026-08-26'da **canlı** bulundu.

## Tekrar eden tuzaklar — yeni kod yazmadan önce oku

Bu üç hata sınıfı projede defalarca çıktı. Yeni kod yazarken önce bunları sor:

1. **`Object.Destroy` ERTELENİR.** Yok edilmeye işaretlenen nesne kare SONUNA kadar yaşar
   ve `FindObjectsByType` onu bulmaya devam eder, `Update`'i bir tur daha çalışır.
   *"Var mı?"* diye sorup karar verme — sahip olman gerekeni **koşulsuz kur**, ya da
   `SetActive(false)` + `DestroyImmediate` ile hemen al. (Sessiz ses, atlanmış oda.)
2. **y yatay hesaba karışıyor.** Mesafe ölçerken `Vector2(x, z)` kullan, `magnitude` değil.
   (Havada doğan düşmanlar, kaçış mesafesi, çıkış kapısı.)
3. **Özellik değil ETKİ test et.** Zincirin son halkası testin içinde mi? Ses üretildi mi
   değil, DUYULUYOR mu. Efekt doğdu mu değil, hedefe VARDI mı. Zaman güdümlü sınıf
   yazıyorsan `dt`'yi dışarıdan al (`Enemy.ApplyAura`, `Arc.Step`, `Mote.Step`) —
   yoksa editör modunda `Time.deltaTime` 0 olduğu için etki ölçülemez.
4. **Test kısayolu KALICI DURUM YAZMAZ.** Yeni bir kısayol eklerken
   `SaveFile.MarkTestJump()` çağır — koşu kirlenir ve `Flush()` diske hiç yazmaz.
   `N` bunu yapmadığı için atladığı bölgeyi checkpoint'e yazıyordu ve Enes bir daha
   1. bölgeye **dönemedi** (2026-09-02, ders 12).
   *Ölçü:* KURALI ya da İLERLEMEYİ değiştiren kısayol kirletir (`N`, `B`, `F5`,
   `F2-F4`); yalnızca KUŞANIMI değiştiren kirletmez (1-5 silah — checkpoint'e zaten
   yazılmıyor, kirletseydi kir sürekli açık kalır ve işe yaramazdı).
   Ve her kısayolun **geri alma yolu** olmalı, kısayolun kendisi kadar erişilebilir
   bir yerde: `F6` sıfırdan yeni koşu başlatır ve **ölüm ekranında da çalışır**.
   ⚠️ Bu kural klavye girdisiyle test edilemiyor — o yüzden burada yazıyor.
5. **Yok edilmiş OLABİLECEK Unity nesnesinde `?.` / `??` kullanma** (saldıran düşman, işaretin
   sahibi, eski odanın nesnesi). Yok edilmiş `UnityEngine.Object` C# tarafında null değil;
   `x?.Foo()` ölü nesneye yazar. Açık `if (x != null)` yaz. (`Clock.Reset` yorumu, ders 14.)
6. **Kapıyı yazınca bir kere BOZ.** Hatayı geri koy, kırmızıya dönüyor mu bak. 2026-09-25
   denetiminde bozuk dört mekaniğin dördünün de kapısı yeşildi (ders 14).

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
