# Combo listesi — FAZ 42

> Enes (2026-10-25): 96 ikili + 24 üçlü, **elle**; ¼ direkt · ¼ açılır (yükseltme açar) · ½ satın alınır (sunakta saniyeyle).
> Kalıplar: `docs/COMBO_ARASTIRMA.md` (K1–K12). Bu dosya partiler hâlinde büyüyor; her parti **Enes'in onayıyla** koda geçer.
> Kural: A ile İSABET → combo penceresinde (0.8 sn) B'ye geç ve vur (üçlüde bir adım daha). "Daha çok hasar" tek başına combo değildir.

## Sayım

| | direkt | açılır | satın alınır | toplam |
|---|---|---|---|---|
| ikili hedef | 24 | 24 | 48 | 96 |
| ikili var | **24 ✅** | **24 ✅** | 0 | 48 |
| üçlü hedef | 6 | 6 | 12 | 24 |
| üçlü var | **6 ✅** | **6 ✅** | 0 | 12 |
| **Parti 3 (onaylı)** | — | — | **24 ikili + 6 üçlü** | 30 |

Direkt ve açılır combo'lar tamam (Parti 1 + 2 kodlandı). Kodda 60 combo var; kalan 60'ı satın alınır (Parti 3–4).

## Zincir kuralı (Faz 42'de eklendi)

Combo bitince zincir silinir — aynı vuruşla ikinci combo başlamasın diye. **İstisna:** ikili bir üçlünün ilk iki adımıysa zincir
silinmez, üçlüye devam edilir. Bugün tek örnek: FAİZLİ → HACİZ (**ALACAK DEVRİ**) → İFLAS (**İCRA TAKİBİ**). Bu kural olmasaydı
İCRA TAKİBİ hiç çalışmazdı: ilk iki adımı ALACAK DEVRİ'ni tetikliyor, zinciri siliyordu.

Bir sıra hem üçlünün sonu hem ikili olabilir (KARŞILIK → TAHVİL → FAİZLİ'nin sonu VADE FARKI): o zaman üçlü önce gelir.

## Var olanlar (sıraları dolu — yeni combo bunları tekrar etmez)

**Direkt (10):** İPOTEK→HACİZ HACİZ KESİNLEŞTİ · KONİ→İPOTEK TOPLU İPOTEK · FAİZLİ→İFLAS TEMERRÜT · KARŞILIK→FAİZLİ KARŞI İCRA ·
ÇEK→HACİZ SENET HACZİ · REHİN→KONİ SİLİP SÜPÜR · REHİN→ARBİTRAJ AÇIĞA SATIŞ · TAHVİL→FAİZLİ VADE FARKI · MÜŞTEREK→KİRA ORTAK GİDER ·
üçlü KONİ→HACİZ→İPOTEK HACİZ KARARI.

**Açılır (12, Faz 33c):** KONİ→MÜŞTEREK SÜRÜ · FAİZLİ→ARBİTRAJ TAKİP · HACİZ→REHİN ÖNCELİK · İPOTEK→KİRA TEMİNAT · İFLAS→KARŞILIK
KONKORDATO · REHİN→HACİZ KEFALET · ÇEK→İPOTEK CİRO HATTI · KARŞILIK→ÇEK MUKABELE · TAHVİL→KONİ TEMETTÜ · MÜŞTEREK→İFLAS KONSORSİYUM ·
ARBİTRAJ→TAHVİL SPEKÜLASYON · KİRA→FAİZLİ KİRA GELİRİ.

## Parti 1 — direkt — **KODLANDI** (Enes'in onayı 2026-10-25)

"Ne yapar" kodda olan hâli. Onaylı metinden farklı olanlar aşağıda ayrıca yazılı. "+" hâlleri benim önerim (kural aynı: "+" sayı
büyütmez, yeni bir etki ekler) — beğenmediğini değiştir.

### İkili (15)

| # | sıra | ad | ne yapar | "+" (öneri) |
|---|---|---|---|---|
| 1 | HACİZ → KONİ | **CEBRİ İCRA** | HACİZ'in vurduğu önüne çekilir (boss çekilmez), koni ×1.5 | ZORLA GETİRME: çekilen 1.5 sn saldıramaz |
| 2 | KONİ → FAİZLİ | **TOPLU BORÇ** | KONİ'nin vurduğu her düşman için +%15 (en çok +%75), savurma bedeli geri | KEFİLLİ BORÇ: yanındakine seker |
| 3 | FAİZLİ → HACİZ | **ALACAK DEVRİ** | FAİZLİ'nin son vuruşu kadar borç HACİZ'in vurduğuna iner | TEMLİK: HACİZ'in vurduğu 2 sn sırtı dönük |
| 4 | İPOTEK → KONİ | **TAPU İPTALİ** | işaret beklemeden patlar, koninin yayı +60° | İSTİMLAK: KONİ'nin sağ tık beklemesi sıfır |
| 5 | HACİZ → İFLAS | **AMME ALACAĞI** | İFLAS'ın vurduğu patlar, vuruşun yarısı 3 birim içindekilere | GECİKME ZAMMI: hasat ×2 |
| 6 | İFLAS → KONİ | **İFLAS MASASI** | İFLAS vuruşunun yarısı yaydaki herkese eklenir | MASA PAYI: hasat ×2 |
| 7 | REHİN → İPOTEK | **İPOTEKLİ REHİN** | işaret rehinenin düştüğü yere, gecikme ×0.1 | İKİNCİ DERECE: İPOTEK'in sağ tık beklemesi sıfır |
| 8 | ÇEK → KONİ | **CİRO** | çekin son çarptığı yerde de yay patlar (koninin %80'i, 2.5 birim) | TAHSİL CİROSU: patlamadakiler 1 sn saldıramaz |
| 9 | KARŞILIK → KONİ | **KARŞILIKLI TAHSİL** | KARŞILIK vuruşunun yarısı (birikim dahil) yaydaki herkese eklenir | MAHSUP: KARŞILIK'ın sağ tık beklemesi sıfır |
| 10 | TAHVİL → HACİZ | **VADELİ HACİZ** | menzil ×1.5, TAHVİL'in kademesi başına +%20 | HACİZ ŞERHİ: vurulan 2 sn yarı hızda |
| 11 | ÇEK → MÜŞTEREK | **ZİNCİRLEME CİRO** | çekin son çarptığını tek vuruşta MÜŞTEREK'in vurduğuna bağlar (aynıysa yanındakine) | CİRO SİLSİLESİ: üçüncü düşman da bağa |
| 12 | MÜŞTEREK → KONİ | **MÜŞTEREK KUSUR** | bağ kopar; bağlılara ve aralarındaki çizgide duranlara koni vuruşu bir kez daha | RÜCU: MÜŞTEREK'in sağ tık beklemesi sıfır |
| 13 | ARBİTRAJ → KONİ | **ARKADAN DOLANIM** | ARBİTRAJ arkadan vurduysa: KONİ'nin menzili ×1.3, vuruş ×1.3 | KÖR NOKTA: vurulanlar 2 sn sırtı dönük |
| 14 | KİRA → İPOTEK | **KİRALIK ARSA** | kule kurulu: işaret hızlı patlar (gecikme ×0.3), ×1.5 | MUAFİYET: kule 4 sn kira almaz |
| 15 | TAHVİL → ARBİTRAJ | **VADESİ GELMİŞ POZİSYON** | TAHVİL 2. kademe ya da üstündeyse: ARBİTRAJ vuruşu arkadan, sonra 1.5 sn görünmezsin | ZARAR KES: ARBİTRAJ'ın sağ tık beklemesi sıfır |

### Üçlü (5, hepsi 2. bölgeden)

| # | sıra | ad | ne yapar | "+" (öneri) |
|---|---|---|---|---|
| Ü1 | FAİZLİ → HACİZ → İFLAS | **İCRA TAKİBİ** | İFLAS saatin dörtte birindeymiş gibi sert, iki savurma bedeli geri | TAZMİNAT: İFLAS'ın sağ tık beklemesi sıfır |
| Ü2 | REHİN → ÇEK → KONİ | **SENETLİ REHİN** | çekin son çarptığı yerin 3 birim çevresindekiler önüne çekilir, koni ×1.5 | KAMBİYO: koni 360° |
| Ü3 | KARŞILIK → TAHVİL → FAİZLİ | **TEMİNATLI TAHVİL** | VADE FARKI'ndan ×1.25 sert, KARŞILIK'ın son birikimi de üstüne | TEMİNAT İADESİ: TAHVİL'in sağ tık beklemesi sıfır |
| Ü4 | MÜŞTEREK → İPOTEK → KONİ | **ORTAKLIĞIN TASFİYESİ** | işaret hemen patlar (×1.5), bağ kopar, bağlılara ve aradakilere koni vuruşu bir kez daha | KAYYUM: bağlılar 1.5 sn saldıramaz |
| Ü5 | KİRA → ARBİTRAJ → HACİZ | **KİRA KONTRATI** | HACİZ ×1.5, kule HACİZ'in vurduğuna kilitlenir (menzil sınırı yok) | İHTARNAME: kilitlenen 2 sn saldıramaz |

### Onaylı metinden sadeleşenler (kodlarken)

- **Orta adıma etki koymadım.** Üçlülerde etki hep bitirişte. Ü2'de çek rehinenin izinden sekmiyor, Ü3'te birikim TAHVİL'i 3. kademeye
  çıkarmıyor, Ü4'te İPOTEK şerit olmuyor, Ü5'te kule ateşi "arkadan" sayılmıyor. Sebep: her adımda ayrı kod yolu açmak 20 combo'yu
  20 ayrı sisteme çevirirdi. İstersen tek tek geri ekleriz.
- **CEBRİ İCRA:** yalnızca HACİZ'in vurduğu çekilir (arkasındakiler değil).
- **ALACAK DEVRİ:** borç "savurma bedelinin 3 katı" değil, FAİZLİ'nin son vuruşu kadar. 3 kat = 1.5 sn sabit kalırdı, oyun ilerledikçe
  hiçbir şey olurdu. "Başka birine" şartı yok.
- **TAPU İPTALİ:** "alanın içine vurursan" şartı yok, KONİ isabet etmesi yetiyor. Yay "alan kadar" değil, +60°.
- **AMME ALACAĞI:** patlayan İFLAS'ın vurduğu. HACİZ'in vurduğu olamıyor: HACİZ uzaktakine, İFLAS dibindekine vuruyor.
- **İFLAS MASASI:** "taşan hasar" tutulmuyor — İFLAS vuruşunun yarısı. Öldürme şartı yok.
- **CİRO:** "dönüş yolunun iki yanı" yerine çekin son çarptığı yerde patlama. "Silahsız kalmazsın" zaten öyleydi (silah değişince).
- **KARŞILIKLI TAHSİL:** birikim bölünmüyor; KARŞILIK vuruşunun yarısı yaydaki herkese. Kalabalıkta toplam daha çok.
- **ZİNCİRLEME CİRO:** "ilk iki düşman" değil — çekin son çarptığı ve MÜŞTEREK'in vurduğu.
- **MÜŞTEREK KUSUR:** bağlıya vurmak şart değil, bağ olması yeter. Bağın "taşıdığı hasar" yerine koni vuruşu.
- **ARKADAN DOLANIM:** "arkadaki kalabalığa açılır" yerine menzil ×1.3. Şart: ARBİTRAJ adımı arkadan vurmuş olmalı.
- **KİRALIK ARSA:** "kulenin menziline at" şartı ve "kule o alana öncelik verir" yok.
- **VADESİ GELMİŞ POZİSYON:** 3. değil 2. kademeden. Görünmezlik TAHVİL'i bırakınca değil, ARBİTRAJ vuruşundan sonra.
- **Ü3:** TAHVİL ortada 0.8 sn'lik pencereye sığmalı — pratikte 1. kademe (yarım saniye basılı tut).

## Parti 2 — açılır — **KODLANDI** (Enes'in onayı 2026-10-27)

**Karar (Enes 2026-10-27):** *"karar 1 gibi olsun ama hem epik hem efsanevi açsın sadece epik değil"* — combo'yu silahın **mevcut**
yükseltmesi açıyor: ikilileri EPİK, üçlüleri EFSANEVİ (ilk adımın silahı). Yükseltme hem kendi etkisini verir hem combo'yu açar;
sunakta açıklamasının sonunda "COMBO açar: …" yazıyor, alınınca "COMBO AÇILDI". *"+ halleri önerinden devam edelim şimdilik"* ·
süre ayarı bütün combo'lardan sonra.

### İkili (12) — açan: silahın EPİK yükseltmesi

| # | sıra | ad | açan | ne yapar (kodda) | "+" (öneri) |
|---|---|---|---|---|---|
| A1 | KONİ → İFLAS | **ACİZ VESİKASI** | YORGUNLUK | İFLAS ×1.5; vurduğu yorgunsa saldırı hazırlığı baştan başlar | MAL BEYANI: 2 sn sırtı dönük |
| A2 | FAİZLİ → İPOTEK | **KONUT KREDİSİ** | FAİZSİZ KREDİ | işaret FAİZLİ'nin vurduğuna yapışır, onunla yürür | ERKEN ÖDEME: İPOTEK'in sağ tık beklemesi sıfır |
| A3 | HACİZ → ARBİTRAJ | **HACİZ MEMURU** | İHTAR | HACİZ'in vurduğunun öbür yanına geçersin, vuruş arkadan | TEBLİĞ: o 1.5 sn saldıramaz |
| A4 | İPOTEK → MÜŞTEREK | **TAPU TEVHİDİ** | ÇİFT TAPU | bekleyen ipoteklerin içindekiler (en çok 4) vurduğunla tek bağa | TOPLULAŞTIRMA: bağın ortasına çekilirler |
| A5 | İFLAS → HACİZ | **İFLAS DAİRESİ** | KURTARMA | HACİZ İFLAS'ın çaresizlik çarpanıyla (en az ×1), ölü bölge yok | İFLAS İLANI: vurulan 2 sn yarı hızda |
| A6 | REHİN → MÜŞTEREK | **MÜŞTEREK REHİN** | DOMİNO | rehine, çarptığı ve sektiği vurduğunla tek bağa | TEMİNAT TAMAMLAMA: bağlılar 1 sn saldıramaz |
| A7 | ÇEK → İFLAS | **İBRAZ** | KARŞILIKSIZ+ | havadaki bütün çekler İFLAS'ın vurduğuna döner, her biri ×1.5 | KAMBİYO TAKİBİ: İFLAS'ın sağ tık beklemesi sıfır |
| A8 | KARŞILIK → İPOTEK | **TEMİNAT AKÇESİ** | BİRİKİM | işaret yarı gecikmeyle; son birikim hasarına eklenir, alanı büyütür (birikim sn başına +%25, en çok ×2) | DEPO: KARŞILIK'ın sağ tık beklemesi sıfır |
| A9 | TAHVİL → KİRA | **KİRA SERTİFİKASI** | İSKONTO | kule kademe × 2 sn kirasız, atışları kademe çarpanıyla (en az ×1) | ANAPARA İADESİ: TAHVİL'in sağ tık beklemesi sıfır |
| A10 | MÜŞTEREK → HACİZ | **HACZE İŞTİRAK** | GENİŞ ORTAKLIK | bağlıların her birine ayrı, tam HACİZ vuruşu da iner | İŞTİRAK PAYI: hasat ×2 |
| A11 | ARBİTRAJ → İFLAS | **HİLELİ İFLAS** | ÇİFT İŞLEM | ARBİTRAJ arkadan vurduysa İFLAS iki kez iner | PERDE: sonra 1 sn görünmez |
| A12 | KİRA → ÇEK | **KİRA TAHSİLATI** | STOPAJ | kulenin menzilindeki herkese çek vuruşu iner; öldürdükleri STOPAJ öder | VERGİ İADESİ: ÇEK'in sağ tık beklemesi sıfır |

### Üçlü (6) — açan: silahın EFSANEVİ yükseltmesi, 2. bölgeden

| # | sıra | ad | açan | ne yapar (kodda) | "+" (öneri) |
|---|---|---|---|---|---|
| Ü6 | HACİZ → KARŞILIK → REHİN | **İSTİHKAK** | DAR ÖLÜ BÖLGE | HACİZ'in vurduğu nerede olursa olsun çekilip yakalanır, son birikim ona iner | İSTİRDAT: REHİN'in sağ tık beklemesi sıfır |
| Ü7 | İPOTEK → ÇEK → İFLAS | **İPOTEK ZİNCİRİ** | ZİNCİR TAPU | İFLAS saatin yarısındaymış gibi; bekleyen işaretler hemen patlar, içindekilere İFLAS da iner | TAHLİYE EMRİ: patlamalar dışa iter |
| Ü8 | İFLAS → TAHVİL → HACİZ | **İFLAS ERTELEMESİ** | İFLAS ZİNCİRİ | HACİZ TAHVİL'in bir üst kademesinin çarpanıyla, ölü bölge yok | TEDBİR KARARI: 2 sn yarı hızda |
| Ü9 | ÇEK → ARBİTRAJ → REHİN | **PROTESTOLU SENET** | PROTESTO | çekin çarptığı nerede olursa olsun yakalanır, onu fırlatınca ×2 | ŞERH: ÇEK'in sağ tık beklemesi sıfır |
| Ü10 | TAHVİL → İFLAS → KONİ | **VADE SONU TAHSİLATI** | VADE SONU | koni 360° ve TAHVİL'in kademe çarpanıyla; İFLAS'ın vurduğu yerde de patlar (İFLAS vuruşunun yarısı, 2.5 birim) | TAHSİL HARCI: hasat ×2 |
| Ü11 | ARBİTRAJ → MÜŞTEREK → ÇEK | **SPEKÜLATİF ATAK** | KISA POZİSYON | bağlıların her birine çek vuruşu "arkadan" iner (ARBİTRAJ'ın arka çarpanıyla) | KISA SATIŞ: bağlılar 2 sn sırtı dönük |

### Onaylı metinden sadeleşenler (kodlarken)

- **Orta adıma yine etki yok** (Parti 1'deki gibi): Ü7'de çek işaretlerden geçince patlamıyor — işaretler İFLAS'ta patlıyor; Ü9'da
  ARBİTRAJ'la arkaya geçme yok — REHİN çekin vurduğunu uzaktan yakalıyor; Ü11'de çek bağlılar arasında uçmuyor — vuruş anında iniyor.
- **A1:** İFLAS her zaman ×1.5; "yorgun" şartı yalnız hazırlığın baştan başlaması için.
- **A5:** "İFLAS'la öldür" şartı yok (bot da insan da tek vuruşta öldürmekte zorlanıyordu). Çarpan en az ×1: saat çokken İFLAS
  zayıf (×0.7), combo HACİZ'i zayıflatmasın.
- **A8:** gecikme yarı — onaylı metinde yoktu; birikim yokken de combo bir şey yapsın diye.
- **A12:** çek kuleye uğrayıp sekmiyor; çek yine normal atılıyor, kulenin menzilindekilere vuruş o anda iniyor.
- Botta: MÜŞTEREK REHİN'de bot rehineyi yakındaki düşmana fırlatıyor (oyuncu gibi) — yoksa MÜŞTEREK'in vuracağı kimse menzilde kalmıyor.

## Parti 3 — satın alınır — **ONAYLANDI** (Enes 2026-10-28: *"onaylıyorum herşeyi"*), kodlanıyor

Kalan 48 satın alınır ikili + 12 üçlüyü ikiye böldüm: **Parti 3 = 24 ikili + 6 üçlü** (her silah iki ikilinin başında), Parti 4 kalanı.
Ağırlık K8 (zaman bükme — saat durur / yavaşlar) ve K9 (evrim — odada kullandıkça büyür): satın alınan combo'nun saniyesini
geri ödemesi lazım. Üçlülerin başı olan 11 sıra (KONİ → HACİZ, REHİN → ÇEK, …) ikiliye verilmedi.

**Satın alma — karar: (a)** (Enes 2026-10-28, öneriyi onayladı):
- (a) **Seçilen:** her ara bölgenin sunak odasında bir **COMBO sunağı**: elindeki silahlarla yapılabilen 2 satın alınır combo teklif eder,
  birini alırsın; fiyat yükseltmelerinkiyle aynı kural (bölgeyle büyür). Koşu başına ~25 sunak → 60'ın yarısından azı; her koşu başka.
- (b) Boss sonrası combo sunağında, "+"nın yanında (koşu başına 5 alım — çoğu hiç görülmez).

### İkili (24)

| # | sıra | ad | ne yapar | kalıp |
|---|---|---|---|---|
| S1 | KONİ → TAHVİL | **TOPLU VADE** | KONİ'nin vurduğu her düşman TAHVİL'in olgunluğuna +0.25 sn yazar (basılı tutma kısalır) | K8 zaman |
| S2 | KONİ → KİRA | **KİRACI SÜRÜSÜ** | kule KONİ'nin vurduklarına öncelik verir; her biri kulenin ömrüne +1 sn | K3 · K12 |
| S3 | FAİZLİ → KONİ | **FAİZ YÜKÜ** | bu odada her FAİZLİ → KONİ bir öncekinden +%15 sert (en çok +%60; oda bitince sıfır) | K9 evrim |
| S4 | FAİZLİ → ÇEK | **FAİZLİ SENET** | çekin her isabeti FAİZLİ'nin savurma bedelini geri öder | K11 risk |
| S5 | HACİZ → İPOTEK | **İHTİYATİ TEDBİR** | HACİZ'in vurduğu 2 sn yerine çakılır (yürüyemez), ipotek tam altına | K3 · K1 |
| S6 | HACİZ → TAHVİL | **VADELİ ALACAK** | TAHVİL'i basılı tuttukça HACİZ'in vurduğu yarı hızda | K8 zaman |
| S7 | İPOTEK → İFLAS | **İPOTEKLİ İFLAS** | işaretin içindekine İFLAS: işaret hemen patlar, İFLAS saatin dibindeymiş gibi | K12 · K5 |
| S8 | İPOTEK → ARBİTRAJ | **ARSA SPEKÜLASYONU** | işaretin içindekilere ARBİTRAJ vuruşu her yönden arkadan | K6 konum |
| S9 | İFLAS → REHİN | **MAL KAÇIRMA** | İFLAS'ın vurduğunu çekip yakalarsın; tutarken saat akmaz (en çok 3 sn) | K8 zaman |
| S10 | İFLAS → KİRA | **TASFİYE KULESİ** | kulenin atışları İFLAS'ın çaresizlik çarpanıyla (saat azsa sert) | K5 bitiriş |
| S11 | REHİN → KARŞILIK | **REHİN KARŞILIĞI** | fırlatmanın verdiği hasar birikene yazılır | K7 savunma → saldırı |
| S12 | REHİN → KİRA | **İŞGAL** | kule rehinenin düştüğü yere kurulur, kirasını rehine öder | K3 durum + durum |
| S13 | ÇEK → TAHVİL | **ÇEK İSKONTOSU** | havadaki çekin her isabeti TAHVİL'e +0.25 sn olgunluk | K8 zaman |
| S14 | ÇEK → KİRA | **KİRA ÇEKİ** | kule çekin son çarptığı yere kurulur, ilk 3 sn kirasız | K12 kurulum |
| S15 | KARŞILIK → İFLAS | **KARŞILIKSIZ İFLAS** | İFLAS'a birikimin iki katı iner | K7 savunma → saldırı |
| S16 | KARŞILIK → ARBİTRAJ | **KARŞI HAMLE** | son karşıladığın saldırganın arkasına geçip vurursun | K6 · K7 |
| S17 | TAHVİL → İPOTEK | **VADELİ TAPU** | ipotek gecikmesi kademe başına −%25, hasarı kademe çarpanıyla | K5 bitiriş |
| S18 | TAHVİL → MÜŞTEREK | **TAHVİL ORTAKLIĞI** | bağın süresi kademe × 2 sn uzar, yayılması kademe başına +%10 | K5 · K3 |
| S19 | MÜŞTEREK → FAİZLİ | **ORTAK BORÇ** | FAİZLİ'nin savurma bedelini bağlılar öder (her birinden bedel kadar hasar, saniye sana) | K11 risk |
| S20 | MÜŞTEREK → ARBİTRAJ | **İÇ İŞLEM** | bağlıya arkadan vuruşun yayılması da "arkadan" (arka çarpanıyla) | K6 · K10 |
| S21 | ARBİTRAJ → HACİZ | **GİZLİ HACİZ** | görünmezken HACİZ ölü bölgesiz, vurunca görünmezlik bozulmaz | K6 konum |
| S22 | ARBİTRAJ → REHİN | **KAÇIRMA** | arkadan vurduğunu yakalarsın; tutarken saat yarı akar | K8 zaman |
| S23 | KİRA → KONİ | **SÜPÜRGE KULESİ** | kulenin menzilindekilere de KONİ vuruşu iner | K12 kurulum + tetik |
| S24 | KİRA → TAHVİL | **KİRA GETİRİSİ** | kule durdukça TAHVİL basılı tutarken saat akmaz | K8 zaman |

### Üçlü (6, 2. bölgeden)

| # | sıra | ad | ne yapar | kalıp |
|---|---|---|---|---|
| SÜ1 | KONİ → REHİN → İFLAS | **TOPLU İCRA** | KONİ'nin vurduklarından havuzu en az olanı yakalarsın, İFLAS onu dip vuruşuyla bitirir; öldürürse +2 sn (doymaya tabi) | K2 · K1 |
| SÜ2 | FAİZLİ → TAHVİL → MÜŞTEREK | **BİLEŞİK FAİZ** | bağ süresince bağa inen her hasar TAHVİL'in kademe çarpanıyla yayılır | K9 · K3 |
| SÜ3 | HACİZ → ÇEK → KİRA | **ZİNCİRLEME TAHSİL** | kule HACİZ'in vurduğuna kilitli kurulur; çekin her isabeti kulenin ömrüne +1 sn | K10 · K12 |
| SÜ4 | KARŞILIK → REHİN → ARBİTRAJ | **KARŞI REHİN** | birikim yakaladığın rehineye biner; ARBİTRAJ'la arkadan vurunca rehine ve arkasındakiler birikimi yer | K7 · K6 |
| SÜ5 | MÜŞTEREK → KARŞILIK → KONİ | **ORTAK KARŞILIK** | 3 sn bağlılara inen her hasar birikene yazılır; KONİ birikimi 360° boşaltır | K7 · K3 |
| SÜ6 | KİRA → İFLAS → ÇEK | **ZAMAN KİRASI** | kule durdukça 3 sn saat akmaz; çek İFLAS'ın vurduğuna kuleden döner | K8 · K10 |

Notlar:
- Hiçbir sıra tekrar etmiyor; Parti 3'ün ikilileri hiçbir üçlünün başı değil.
- ⚠️ K8'liler (saat akmaz / yarı akar) yasanın kalbine dokunuyor: saat = can = para. Satın alınır olmaları bilerek — saniyeyle alınan
  şey saniye kazandırmalı. Aşırı gelirse süre ayarında (bütün combo'lardan sonra) kısarız.
- "+" hâllerini kodlarken öneririm.

## Sıradaki partiler (plan)
- **Parti 4 — satın alınır 24 ikili + 6 üçlü:** kalan sıralar (üçlü başları dahil, zincir kuralıyla), Parti 3'ün satın alma kararına göre.
