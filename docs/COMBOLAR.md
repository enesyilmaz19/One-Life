# Combo listesi — FAZ 42

> Enes (2026-10-25): 96 ikili + 24 üçlü, **elle**; ¼ direkt · ¼ açılır (yükseltme açar) · ½ satın alınır (sunakta saniyeyle).
> Kalıplar: `docs/COMBO_ARASTIRMA.md` (K1–K12). Bu dosya partiler hâlinde büyüyor; her parti **Enes'in onayıyla** koda geçer.
> Kural: A ile İSABET → combo penceresinde (0.8 sn) B'ye geç ve vur (üçlüde bir adım daha). "Daha çok hasar" tek başına combo değildir.

## Sayım

| | direkt | açılır | satın alınır | toplam |
|---|---|---|---|---|
| ikili hedef | 24 | 24 | 48 | 96 |
| ikili var | **24 ✅** | **24 ✅** | **48 ✅** | **96 ✅** |
| üçlü hedef | 6 | 6 | 12 | 24 |
| üçlü var | **6 ✅** | **6 ✅** | **12 ✅** | **24 ✅** |

**120 combo kodda** (Parti 1–4). Sırada süre ayarı (senin oynayışınla, masa #20).

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

## Parti 3 — satın alınır — **KODLANDI** (Enes'in onayı 2026-10-28: *"onaylıyorum herşeyi"*; sadeleşenler 2026-10-29: *"combolar şimdilik dursun"*)

**Satın alma (karar a):** her ara bölgenin sunak odasında, şans sunağının iki yanında **COMBO sunağı** (turkuaz): elindeki silahlarla
yapılabilen, alınmamış, bölgesi gelmiş satın alınır combo'lardan 2 teklif; birini alırsın (sunak başı 1). Teklif koşunun tohumundan,
odaya dönünce aynı. Fiyat yükseltmelerin kuralıyla — ikili NADİR, üçlü EPİK fiyatında, × 0.85 (Enes 2026-10-29: *"biraz pahalı olmuş ama çok az"* — kadran `comboAltarPriceMul`;
1. bölgede saat 300'ken 45 → **38 sn**).
Alınca "COMBO ALINDI: …". Bir koşuda ~25 sunak odası → 60 satın alınırın yarısından azı; her koşu başka.

"Ne yapar" kodda olan hâli; "+" hâlleri benim önerim.

### İkili (24)

| # | sıra | ad | ne yapar (kodda) | "+" (öneri) |
|---|---|---|---|---|
| S1 | KONİ → TAHVİL | **TOPLU VADE** | KONİ'yle vur → TAHVİL: KONİ'nin vurduğu her düşman olgunluğa +0.25 sn yazar (basılı tutma kısalır) | ERKEN İTFA: TAHVİL'in sağ tık beklemesi sıfırlanır |
| S2 | KONİ → KİRA | **KİRACI SÜRÜSÜ** | KONİ'yle vur → kule kur: kule KONİ'nin vurduklarına öncelik verir, her biri kulenin ömrüne +1 sn | KİRA İHTARI: öncelikliler 1 sn yeni saldırı başlatamaz |
| S3 | FAİZLİ → KONİ | **FAİZ YÜKÜ** | FAİZLİ'yle vur → KONİ: bu odada her seferinde +%15 daha sert (en çok +%60; oda değişince baştan) | FAİZE FAİZ: o vuruşun hasadı ×2 |
| S4 | FAİZLİ → ÇEK | **FAİZLİ SENET** | FAİZLİ'yle vur → ÇEK: çekin her isabeti FAİZLİ'nin savurma bedelini geri öder | SENET İADESİ: FAİZLİ'nin sağ tık beklemesi sıfırlanır |
| S5 | HACİZ → İPOTEK | **İHTİYATİ TEDBİR** | HACİZ'le vur → İPOTEK: HACİZ'in vurduğu 2 sn yerine çakılır, işaret tam altına | TEDBİR ŞERHİ: çakılan 2 sn yeni saldırı başlatamaz |
| S6 | HACİZ → TAHVİL | **VADELİ ALACAK** | HACİZ'le vur → TAHVİL: basılı tuttukça HACİZ'in vurduğu yarı hızda | ALACAK FAİZİ: HACİZ'in sağ tık beklemesi sıfırlanır |
| S7 | İPOTEK → İFLAS | **İPOTEKLİ İFLAS** | İPOTEK'in içinde düşman varken İFLAS: saatin dibindeymiş gibi, bekleyen işaretler hemen patlar | İCRAİ SATIŞ: patlamalar içindekileri dışa iter |
| S8 | İPOTEK → ARBİTRAJ | **ARSA SPEKÜLASYONU** | İPOTEK'in içinde düşman varken ARBİTRAJ: vuruş her yönden arkadan | ARSA SPEKÜLATÖRÜ: sonra 1 sn görünmezsin |
| S9 | İFLAS → REHİN | **MAL KAÇIRMA** | İFLAS'la vur → REHİN: vurduğunu çekip yakalarsın; 3 sn saat akmaz | MAL GİZLEME: İFLAS'ın sağ tık beklemesi sıfırlanır |
| S10 | İFLAS → KİRA | **TASFİYE KULESİ** | İFLAS'la vur → kule kur: atışlar İFLAS'ın çaresizlik çarpanıyla (saat azsa sert, en az ×1) | TASFİYE PAYI: kule öldürünce +1 sn (doymaya tabi) |
| S11 | REHİN → KARŞILIK | **REHİN KARŞILIĞI** | REHİN'i fırlat → KARŞILIK: fırlatmanın hasarı birikene yazılır, KARŞILIK onu indirir | KARŞILIK İADESİ: KARŞILIK'ın sağ tık beklemesi sıfırlanır |
| S12 | REHİN → KİRA | **İŞGAL** | REHİN'i fırlat → kule kur: kule rehinenin düştüğü yere, kirasını rehine öder | TAHLİYE DAVASI: kulenin ömrü +3 sn |
| S13 | ÇEK → TAHVİL | **ÇEK İSKONTOSU** | ÇEK isabet etti → TAHVİL: basılı tutarken çekin her isabeti olgunluğa +0.25 sn | İSKONTO ORANI: ÇEK'in sağ tık beklemesi sıfırlanır |
| S14 | ÇEK → KİRA | **KİRA ÇEKİ** | ÇEK isabet etti → kule kur: kule çekin son çarptığı yere, ilk 3 sn kirasız | ÇEKLE ÖDEME: ÇEK'in sağ tık beklemesi sıfırlanır |
| S15 | KARŞILIK → İFLAS | **KARŞILIKSIZ İFLAS** | KARŞILIK'la vur → İFLAS: KARŞILIK'ın son birikiminin iki katı da iner | SAHTE İFLAS: vurulan 2 sn yarı hızda |
| S16 | KARŞILIK → ARBİTRAJ | **KARŞI HAMLE** | KARŞILIK'la vur → ARBİTRAJ: vurduğunun arkasına geçersin, vuruş arkadan | KARŞI İHTAR: arkasına geçtiğin 1.5 sn yeni saldırı başlatamaz |
| S17 | TAHVİL → İPOTEK | **VADELİ TAPU** | TAHVİL'i boz → İPOTEK: gecikme kademe başına −%25, hasar kademe çarpanıyla | TAPU İSKONTOSU: İPOTEK'in sağ tık beklemesi sıfırlanır |
| S18 | TAHVİL → MÜŞTEREK | **TAHVİL ORTAKLIĞI** | TAHVİL'i boz → MÜŞTEREK: bağın süresi kademe × 2 sn uzar, yayılması kademe başına +%10 | ORTAKLIK PAYI: bağlılar 1 sn yeni saldırı başlatamaz |
| S19 | MÜŞTEREK → FAİZLİ | **ORTAK BORÇ** | MÜŞTEREK'le bağla → FAİZLİ: savurma bedelini bağlılar öder (her birine bedel kadar vuruş, bedel sana geri) | MÜŞTEREK BORÇLU: bağlılar 2 sn yarı hızda |
| S20 | MÜŞTEREK → ARBİTRAJ | **İÇ İŞLEM** | MÜŞTEREK'le bağla → ARBİTRAJ: bağlıların hepsi 2 sn sırtını döner — ARBİTRAJ onlara arkadan vurur | İÇERİDEN BİLGİ: ARBİTRAJ'ın sağ tık beklemesi sıfırlanır |
| S21 | ARBİTRAJ → HACİZ | **GİZLİ HACİZ** | ARBİTRAJ'la ARKADAN vur → HACİZ: ölü bölge yok, ×1.3 | SESSİZ HACİZ: sonra 1 sn görünmezsin |
| S22 | ARBİTRAJ → REHİN | **KAÇIRMA** | ARBİTRAJ'la ARKADAN vur → REHİN: vurduğunu çekip yakalarsın; 4 sn saat yarı akar | FİDYE: REHİN'in sağ tık beklemesi sıfırlanır |
| S23 | KİRA → KONİ | **SÜPÜRGE KULESİ** | kule kur → KONİ: kulenin menzilindekilere de KONİ vuruşu iner | KULE SÜPÜRMESİ: kulenin ömrü +2 sn |
| S24 | KİRA → TAHVİL | **KİRA GETİRİSİ** | kule kur → TAHVİL: kule duruyorsa basılı tutarken saat iki kat akmaz | GETİRİ PAYI: TAHVİL'in sağ tık beklemesi sıfırlanır |

### Üçlü (6, 2. bölgeden)

| # | sıra | ad | ne yapar (kodda) | "+" (öneri) |
|---|---|---|---|---|
| SÜ1 | KONİ → REHİN → İFLAS | **TOPLU İCRA** | KONİ → REHİN → İFLAS: KONİ'nin vurduklarından havuzu en azına İFLAS'ın dip vuruşu da iner; öldürürse +2 sn (doymaya tabi) | İCRA MASRAFI: KONİ'nin sağ tık beklemesi sıfırlanır |
| SÜ2 | FAİZLİ → TAHVİL → MÜŞTEREK | **BİLEŞİK FAİZ** | FAİZLİ → TAHVİL → MÜŞTEREK: bağın yayılması TAHVİL'in kademe çarpanıyla büyür (en çok %100) | FAİZ TAHAKKUKU: bağlılar 2 sn yarı hızda |
| SÜ3 | HACİZ → ÇEK → KİRA | **ZİNCİRLEME TAHSİL** | HACİZ → ÇEK → kule kur: kule HACİZ'in vurduğuna kilitli (menzilsiz); çekin her isabeti kulenin ömrüne +1 sn | TAHSİL EMRİ: kilitlenen 2 sn yeni saldırı başlatamaz |
| SÜ4 | KARŞILIK → REHİN → ARBİTRAJ | **KARŞI REHİN** | KARŞILIK → REHİN → ARBİTRAJ: vuruş arkadan, KARŞILIK'ın son birikimi de üstüne | REHİN AÇIĞI: vurulan 2 sn sırtı dönük |
| SÜ5 | MÜŞTEREK → KARŞILIK → KONİ | **ORTAK KARŞILIK** | MÜŞTEREK → KARŞILIK → KONİ: koni 360°, KARŞILIK'ın son vuruşu bağlıların her birine de iner | KARŞILIKLI ORTAKLIK: KARŞILIK'ın sağ tık beklemesi sıfırlanır |
| SÜ6 | KİRA → İFLAS → ÇEK | **ZAMAN KİRASI** | kule kur → İFLAS → ÇEK: kule duruyorsa 3 sn saat akmaz, çek ×1.5 | ZAMAN AŞIMI: kulenin ömrü +3 sn |

### Onaylı metinden sadeleşenler / değişenler (kodlarken)

- **Orta adıma yine etki yok:** TOPLU İCRA'da REHİN "en zayıfı yakalamıyor" — İFLAS bitirişinde KONİ'nin vurduklarından en zayıfına dip
  vuruşu iniyor; ORTAK KARŞILIK'ta 3 sn'lik birikim yok — KARŞILIK'ın son vuruşu bağlılara iniyor; KARŞI REHİN'de birikim rehineye binmiyor —
  ARBİTRAJ'ın vuruşuna ekleniyor (arka çarpanıyla birlikte); ZAMAN KİRASI'nda çek kuleden dönmüyor — ×1.5.
- **"Tutarken" → "yakalayınca":** MAL KAÇIRMA 3 sn saat durur, KAÇIRMA 4 sn yarı akar — bıraksan da sürüyor.
- **İPOTEKLİ İFLAS · ARSA SPEKÜLASYONU:** "işaretin içindekine" değil — işaretin içinde düşman varken İFLAS / ARBİTRAJ kimi vurursa.
- **KARŞI HAMLE:** "son karşıladığın saldırgan" değil, KARŞILIK'ın vurduğu (karşılama şart olsaydı bot da insan da zor denerdi).
- **İÇ İŞLEM değişti:** "yayılması da arkadan" KONSORSİYUM'un aynısı oluyordu → bağlıların hepsi 2 sn sırtını döner, ARBİTRAJ onlara arkadan.
- **GİZLİ HACİZ:** "görünmezken" olamıyordu — ARBİTRAJ'ın vuruşu görünmezliği bitiriyor; şart "ARBİTRAJ arkadan vurdu", üstüne ×1.3.
- **ORTAK BORÇ:** her bağlıya bedel kadar vuruş (hasatsız), bedel bir kez sana geri.
- **FAİZ YÜKÜ:** ilk seferde de +%15 (×1.15 · 1.30 · 1.45 · 1.60).
- **BİLEŞİK FAİZ:** yayılma en çok %100.

**Yolda bulunan hata:** çok hedefli vuruşlar (KONİ, KARŞILIK, TAHVİL) "son vurulan düşman"ı hiç yazmıyordu — zincirde adımları hedefsizdi,
KARŞI HAMLE hiç tetiklenmiyordu. Artık en yakın vurulan yazılıyor (eski kapıların hiçbiri bozulmadı).

## Parti 4 — satın alınır, son parti — **KODLANDI** (Enes 2026-10-29: *"commitle ve sonraki partiye geç"*)

Son parti: **24 ikili + 6 üçlü** → toplam 120. Her silah iki ikilinin başında; üçlüler bugüne kadar tek üçlüsü olan altı silaha
(REHİN, İPOTEK, İFLAS, ÇEK, TAHVİL, ARBİTRAJ) — böylece her silah iki üçlünün başında. Satın alma Parti 3'teki gibi (COMBO sunağı).

"Ne yapar" kodda olan hâli; "+" hâlleri benim önerim.

### İkili (24)

| # | sıra | ad | ne yapar (kodda) | "+" (öneri) |
|---|---|---|---|---|
| T1 | KONİ → ÇEK | **TOPLU TAHSİLAT** | KONİ'yle vur → ÇEK: çek KONİ'nin vurduklarının her birine sırayla uğrar | TAHSİL MAKBUZU: çekin vurdukları 1 sn yeni saldırı başlatamaz |
| T2 | KONİ → ARBİTRAJ | **SÜRÜDEN AYIR** | KONİ'yle vur → ARBİTRAJ: KONİ'nin vurduklarından en uzaktakinin arkasına geçersin, vuruş arkadan | AYRIŞMA: KONİ'nin vurduğu öbürleri 2 sn sırtı dönük |
| T3 | FAİZLİ → REHİN | **BORÇ ESARETİ** | FAİZLİ'yle vur → REHİN: vurduğunu nerede olursa olsun çekip yakalarsın; tuttukça her sn havuzundan savurma bedeli kadar hasat edersin | BORÇ SENEDİ: FAİZLİ'nin sağ tık beklemesi sıfırlanır |
| T4 | FAİZLİ → KİRA | **FAİZLİ KİRA** | FAİZLİ'yle vur → kule kur: 4 savurma bedeli ödersin, kule ×2 sert atar | KİRA SÖZLEŞMESİ: kulenin ömrü +3 sn |
| T5 | HACİZ → MÜŞTEREK | **HACİZ ORTAKLIĞI** | HACİZ'le vur → MÜŞTEREK: HACİZ'in vurduğu, menzil sınırı olmadan MÜŞTEREK'in vurduğuna bağlanır | MÜŞTEREK HACİZ: HACİZ'in sağ tık beklemesi sıfırlanır |
| T6 | HACİZ → KİRA | **KİRA HACZİ** | HACİZ'le vur → kule kur: kule HACİZ'in vurduğunun yanına, ona önce ve ×1.5 ateş eder | TAHLİYE İHTARI: haczedilen 2 sn yeni saldırı başlatamaz |
| T7 | İPOTEK → FAİZLİ | **TAPU KREDİSİ** | İPOTEK'in içinde düşman varken FAİZLİ: ×2, savurma bedeli geri | İPOTEK FEKKİ: bekleyen işaretler hemen patlar |
| T8 | İPOTEK → TAHVİL | **TAPU VADESİ** | İPOTEK koy → TAHVİL: basılı tutma 1. kademe dolu başlar; işaret patlarsa +1 kademe | TAPU TESCİLİ: İPOTEK'in sağ tık beklemesi sıfırlanır |
| T9 | İFLAS → İPOTEK | **İFLAS İPOTEĞİ** | İFLAS'la vur → İPOTEK: işaret İFLAS'ın vurduğunun altına, gecikme ×0.2, hasar İFLAS'ın çaresizlik çarpanıyla (en az ×1) | MÜHLET: İFLAS'ın sağ tık beklemesi sıfırlanır |
| T10 | İFLAS → MÜŞTEREK | **İFLAS ORTAKLIĞI** | İFLAS'la vur → MÜŞTEREK: İFLAS'ın vurduğu MÜŞTEREK'in vurduğuna bağlanır, yayılma İFLAS'ın çaresizlik çarpanıyla (en çok %100) | ORTAK İFLAS: bağlılar 2 sn yarı hızda |
| T11 | REHİN → FAİZLİ | **REHİNLİ KREDİ** | REHİN'le vur (fırlat · takas · sürükle) → FAİZLİ: ×1.5, savurma bedeli geri | REHİN FEKKİ: REHİN'in sağ tık beklemesi sıfırlanır |
| T12 | REHİN → TAHVİL | **REHİN VADESİ** | REHİN'i fırlat → TAHVİL: rehine 3 sn yerine çakılı, basılı tutma 1. kademe dolu başlar | HAPİS HAKKI: çakılan 3 sn yeni saldırı başlatamaz |
| T13 | ÇEK → FAİZLİ | **ÇEKLİ KREDİ** | havada çek varken FAİZLİ: ×1.3, savurma bedeli geri | ELDEN TAHSİL: havadaki çekler eline döner |
| T14 | ÇEK → KARŞILIK | **KARŞILIKLI ÇEK** | ÇEK isabet etti → KARŞILIK'la vur: havadaki çekin her isabeti birikene 0.5 sn yazar (KARŞILIK indirene dek) | KARŞILIK ÇEKİ: ÇEK'in sağ tık beklemesi sıfırlanır |
| T15 | KARŞILIK → HACİZ | **KARŞI HACİZ** | KARŞILIK'la vur → HACİZ: KARŞILIK'ın son birikimi HACİZ'e biner, ölü bölge yok | KARŞI TEBLİĞ: vurulan 1.5 sn yeni saldırı başlatamaz |
| T16 | KARŞILIK → KİRA | **KİRA KARŞILIĞI** | KARŞILIK'la vur → kule kur: kule KARŞILIK'ın son birikimi kadar sn kirasız | KİRA DEPOZİTOSU: KARŞILIK'ın sağ tık beklemesi sıfırlanır |
| T17 | TAHVİL → REHİN | **VADELİ REHİN** | TAHVİL'i boz → REHİN: kademe kadar uzaktan yakalar (menzil ×kademe), o rehinenin fırlatması kademe çarpanıyla | REHİN İTFASI: TAHVİL'in sağ tık beklemesi sıfırlanır |
| T18 | TAHVİL → ÇEK | **VADELİ ÇEK** | TAHVİL'i boz → ÇEK: çek kademe kadar fazla seker, kademe çarpanıyla (en az ×1) | KEŞİDE: ÇEK'in sağ tık beklemesi sıfırlanır |
| T19 | MÜŞTEREK → ÇEK | **SENET ORTAKLIĞI** | MÜŞTEREK'le bağla → ÇEK: çek bağlıya çarpınca bağ tam (%100) yayar | SENET YENİLEME: bağın süresi tazelenir |
| T20 | MÜŞTEREK → TAHVİL | **ORTAK VADE** | MÜŞTEREK'le bağla → TAHVİL: bağ yaşadıkça basılı tutarken saat iki kat akmaz | ORTAK KUPON: bağlılar 2 sn yarı hızda |
| T21 | ARBİTRAJ → İPOTEK | **GİZLİ İPOTEK** | ARBİTRAJ'la ARKADAN vur → İPOTEK: işaret vurduğunun altına, 1.5 sn görünmezsin | TAKİPLİ TAPU: işaret hedefle birlikte gider |
| T22 | ARBİTRAJ → KİRA | **GİZLİ KULE** | ARBİTRAJ'la ARKADAN vur → kule kur: kulenin atışları arkadan (ARBİTRAJ'ın arka çarpanıyla) | PERDE ARKASI: kule kurulunca 1 sn görünmezsin |
| T23 | KİRA → HACİZ | **KULE HACZİ** | kule kur → HACİZ: kule duruyorsa ölü bölge yok, ×1.3 | ZORLA TAHLİYE: vurulanlar kuleden dışa itilir |
| T24 | KİRA → REHİN | **KİRACI REHİN** | kule kur → REHİN: kulenin son vurduğunu nerede olursa olsun çekip yakalar | KİRA REHNİ: KİRA'nın sağ tık beklemesi sıfırlanır |

### Üçlü (6, 2. bölgeden)

| # | sıra | ad | ne yapar (kodda) | "+" (öneri) |
|---|---|---|---|---|
| TÜ1 | REHİN → İFLAS → ÇEK | **İFLAS SENEDİ** | çek İFLAS'ın çaresizlik çarpanıyla (en az ×1), 2 kez fazla seker | MASA ALACAĞI: İFLAS'ın sağ tık beklemesi sıfırlanır |
| TÜ2 | İPOTEK → KARŞILIK → KONİ | **TAPU KARŞILIĞI** | bekleyen işaretler hemen patlar, vurdukları kadar KONİ'ye eklenir | TAPU TAHLİYESİ: patlamalar içindekileri dışa iter |
| TÜ3 | İFLAS → ARBİTRAJ → HACİZ | **ARKADAN İCRA** | HACİZ İFLAS'ın çaresizlik çarpanıyla (en az ×1) ve arkadan (×2.5), ölü bölge yok | İCRA PERDESİ: sonra 1 sn görünmezsin |
| TÜ4 | ÇEK → REHİN → KİRA | **SENETLİ KİRA** | kule rehinenin düştüğü yere, 5 sn kirasız, ömrü +5 sn | SENETLİ KİRACI: kirasız süre bitince kirayı rehine öder |
| TÜ5 | TAHVİL → KARŞILIK → İFLAS | **VADELİ KARŞILIK** | KARŞILIK'ın son birikimi TAHVİL'in kademe çarpanıyla (en az ×2) İFLAS'a iner | KARŞILIK VADESİ: vurulan 2 sn yarı hızda |
| TÜ6 | ARBİTRAJ → ÇEK → MÜŞTEREK | **GİZLİ ORTAKLIK** | çekin çarptıkları (en çok 4) MÜŞTEREK'in vurduğuyla tek bağa, bağlılar 2 sn sırtı dönük | GİZLİ PAY: ARBİTRAJ'ın sağ tık beklemesi sıfırlanır |

### Onaylı metinden sadeleşenler / değişenler (kodlarken)

- **BORÇ ESARETİ:** "sana akar" → havuzundan hasat edersin. Hasat kuralından geçiyor (doyma, kredi) — SORGU yükseltmesi gibi; doğrudan
  saniye verseydi tutulan her düşman bedava gelir olurdu.
- **TAPU KREDİSİ · REHİNLİ KREDİ:** "işaretin içindekine" / "fırlatılan rehineye" → FAİZLİ kimi vurursa (Parti 3'teki İPOTEKLİ İFLAS gibi).
- **KULE HACZİ:** "kulenin menzilindekilere" → kule dururken.
- **KARŞILIKLI ÇEK:** combo anında havadaki çekler yazar; sonradan atılan çek yazmaz (yoksa birikim sınırsız büyürdü).
- **TAPU KARŞILIĞI:** birikime yazılmıyor, doğrudan KONİ'ye ekleniyor — KONİ birikimi indiremiyor, sonuç aynı.
- **VADELİ KARŞILIK:** en az ×2 — üçlü, kendi sonu olan KARŞILIKSIZ İFLAS'tan (×2) zayıf olmasın.
- **ARKADAN İCRA:** "arkadan" ×2.5 elle çarpılıyor — arka çarpanı yalnız ARBİTRAJ'ın kendi vuruşunda işliyor.
- **KİRA HACZİ:** ×1.5'e ek olarak kule ona **önce** ateş ediyor (yanında başka düşman olsa da).
- **TOPLU TAHSİLAT:** çek hepsine uğrayana dek menzili bitmiyor.
- **İFLAS ORTAKLIĞI:** yayılma en çok %100 (BİLEŞİK FAİZ gibi).

**Sıra çakışmaları:** üçlülerin sonu altı ikiliyle aynı (TAPU KARŞILIĞI ⊃ KARŞILIKLI TAHSİL, ARKADAN İCRA ⊃ GİZLİ HACİZ, SENETLİ KİRA ⊃ İŞGAL,
VADELİ KARŞILIK ⊃ KARŞILIKSIZ İFLAS, GİZLİ ORTAKLIK ⊃ ZİNCİRLEME CİRO) — üçlü önce geliyor. Bir de ters yönden: eski SPEKÜLATİF ATAK
(ARBİTRAJ → MÜŞTEREK → ÇEK) yeni SENET ORTAKLIĞI'nı içine alıyor — o da önce geliyor. Kapı altısını da ölçüyor.
