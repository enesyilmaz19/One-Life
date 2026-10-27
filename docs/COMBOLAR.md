# Combo listesi — FAZ 42

> Enes (2026-10-25): 96 ikili + 24 üçlü, **elle**; ¼ direkt · ¼ açılır (yükseltme açar) · ½ satın alınır (sunakta saniyeyle).
> Kalıplar: `docs/COMBO_ARASTIRMA.md` (K1–K12). Bu dosya partiler hâlinde büyüyor; her parti **Enes'in onayıyla** koda geçer.
> Kural: A ile İSABET → combo penceresinde (0.8 sn) B'ye geç ve vur (üçlüde bir adım daha). "Daha çok hasar" tek başına combo değildir.

## Sayım

| | direkt | açılır | satın alınır | toplam |
|---|---|---|---|---|
| ikili hedef | 24 | 24 | 48 | 96 |
| ikili var | **24 ✅** | 12 | 0 | 36 |
| üçlü hedef | 6 | 6 | 12 | 24 |
| üçlü var | **6 ✅** | 0 | 0 | 6 |
| **Parti 2 (onaylı)** | — | **12 ikili + 6 üçlü** | — | 18 |

Direkt combo'lar tamam (Parti 1 kodlandı). Kodda 42 combo var.

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

## Parti 2 — açılır — **ONAYLANDI** (Enes 2026-10-27), kodlanıyor

**Açan:** ikililer o silahın **mevcut EPİK yükseltmesiyle** açılır (her silahta tam bir tane var). Combo o yükseltmenin değiştirdiği
şeyi kullanır; yükseltme hem kendi etkisini verir hem combo'yu açar. 33c'deki gibi yeni açan yükseltme eklemedim — silah başına
8 yükseltme kalıyor. **Üçlüler EFSANEVİ ile** açılır (Enes, 33c: *"üst enderliğin combo'su da güçlü"*); ilk adımı o silah, 2. bölgeden.

**Karar (Enes 2026-10-27):** *"karar 1 gibi olsun ama hem epik hem efsanevi açsın sadece epik değil"* — mevcut yükseltmeler açıyor;
ikilileri EPİK, üçlüleri EFSANEVİ. *"+ halleri önerinden devam edelim şimdilik"* · süre ayarı bütün combo'lardan sonra.

### İkili (12) — açan: silahın EPİK yükseltmesi

| # | sıra | ad | açan | ne yapar | kalıp |
|---|---|---|---|---|---|
| A1 | KONİ → İFLAS | **ACİZ VESİKASI** | YORGUNLUK | KONİ'nin yorduğuna İFLAS: saldırı hazırlığı baştan başlar, vuruş ×1.5 | K3 durum + durum |
| A2 | FAİZLİ → İPOTEK | **KONUT KREDİSİ** | FAİZSİZ KREDİ | FAİZLİ'nin vurduğuna ipotek yapışır: işaret onunla yürür, kaçamaz | K1 işaretle-patlat |
| A3 | HACİZ → ARBİTRAJ | **HACİZ MEMURU** | İHTAR | HACİZ'in vurduğunun (yavaşlamış) arkasına geçersin, ARBİTRAJ vuruşu arkadan | K6 konum |
| A4 | İPOTEK → MÜŞTEREK | **TAPU TEVHİDİ** | ÇİFT TAPU | MÜŞTEREK vuruşu bekleyen ipoteklerin içindekileri (en çok 4) tek bağa alır | K12 · K3 |
| A5 | İFLAS → HACİZ | **İFLAS DAİRESİ** | KURTARMA | İFLAS'la öldür → HACİZ: İFLAS'ın çaresizliğiyle vurur (saat azsa sert), ölü bölge yok | K5 bitiriş |
| A6 | REHİN → MÜŞTEREK | **MÜŞTEREK REHİN** | DOMİNO | fırlattığın rehine ve çarptıkları MÜŞTEREK'le tek bağa | K10 zincir |
| A7 | ÇEK → İFLAS | **İBRAZ** | KARŞILIKSIZ+ | havadaki bütün çekler İFLAS'ın vurduğuna döner, her biri ona bir kez ×1.5 | K2 topla-vur |
| A8 | KARŞILIK → İPOTEK | **TEMİNAT AKÇESİ** | BİRİKİM | birikim ipoteğe biner: işaretin hasarına eklenir, alan birikimle büyür | K7 · K12 |
| A9 | TAHVİL → KİRA | **KİRA SERTİFİKASI** | İSKONTO | kule TAHVİL'in kademesi × 2 sn kirasız başlar, atışları kademe çarpanıyla | K5 bitiriş |
| A10 | MÜŞTEREK → HACİZ | **HACZE İŞTİRAK** | GENİŞ ORTAKLIK | HACİZ bir bağlıya vurunca bağlıların hepsine ayrı tam vuruş (yayılma değil) | K3 durum + durum |
| A11 | ARBİTRAJ → İFLAS | **HİLELİ İFLAS** | ÇİFT İŞLEM | ARBİTRAJ'la arkadan vur → İFLAS iki kez iner | K6 konum |
| A12 | KİRA → ÇEK | **KİRA TAHSİLATI** | STOPAJ | çek kuleye uğrar, menzilindeki her düşmana bir kez seker; öldürdükleri STOPAJ öder | K10 zincir |

### Üçlü (6) — açan: silahın EFSANEVİ yükseltmesi, 2. bölgeden

| # | sıra | ad | açan | ne yapar | kalıp |
|---|---|---|---|---|---|
| Ü6 | HACİZ → KARŞILIK → REHİN | **İSTİHKAK** | DAR ÖLÜ BÖLGE | HACİZ'in vurduğu nerede olursa olsun REHİN'e çekilir, KARŞILIK'ın birikimi ona iner | K2 · K7 |
| Ü7 | İPOTEK → ÇEK → İFLAS | **İPOTEK ZİNCİRİ** | ZİNCİR TAPU | çek bekleyen işaretlerden geçince hepsi patlar; İFLAS patlamanın vurduklarına dip vuruşu | K12 · K10 |
| Ü8 | İFLAS → TAHVİL → HACİZ | **İFLAS ERTELEMESİ** | İFLAS ZİNCİRİ | İFLAS'ın vuruşu TAHVİL'e +1 kademe yazar; HACİZ bu vadeyle, ölü bölgesiz | K8 · K5 |
| Ü9 | ÇEK → ARBİTRAJ → REHİN | **PROTESTOLU SENET** | PROTESTO | çekin vurduğunun arkasına geçersin, REHİN onu arkadan yakalar: fırlatma ×2 | K6 · K2 |
| Ü10 | TAHVİL → İFLAS → KONİ | **VADE SONU TAHSİLATI** | VADE SONU | KONİ 360° ve TAHVİL'in kademe çarpanıyla; İFLAS'ın vurduğu ortada patlar | K5 · K1 |
| Ü11 | ARBİTRAJ → MÜŞTEREK → ÇEK | **SPEKÜLATİF ATAK** | KISA POZİSYON | çek bağlılar arasında gidip gelir (her birine bir kez), bağlılara vuruş "arkadan" | K10 · K6 |

Notlar:
- Hiçbir sıra tekrar etmiyor; Parti 2'nin ikilileri hiçbir üçlünün başı değil (zincir kuralına yeni istisna yok).
- Üçlülerin ilk adımları bugün üçlüsü olmayan altı silah: HACİZ, İPOTEK, İFLAS, ÇEK, TAHVİL, ARBİTRAJ.
- "+" hâllerini kodlarken öneririm (Parti 1'deki gibi).

## Sıradaki partiler (plan)
- **Parti 3–4 — satın alınır 48 ikili + 12 üçlü:** K3 (iki durum), K8 (zaman bükme — saat durur), K9 (evrim) ağırlıklı; combo sunağında saniyeyle.
