# Combo listesi — FAZ 42

> Enes (2026-10-25): 96 ikili + 24 üçlü, **elle**; ¼ direkt · ¼ açılır (yükseltme açar) · ½ satın alınır (sunakta saniyeyle).
> Kalıplar: `docs/COMBO_ARASTIRMA.md` (K1–K12). Bu dosya partiler hâlinde büyüyor; her parti **Enes'in onayıyla** koda geçer.
> Kural: A ile İSABET → combo penceresinde B'ye geç ve vur (üçlüde bir adım daha). "Daha çok hasar" tek başına combo değildir.

## Sayım

| | direkt | açılır | satın alınır | toplam |
|---|---|---|---|---|
| ikili hedef | 24 | 24 | 48 | 96 |
| ikili var | 9 | 12 | 0 | 21 |
| üçlü hedef | 6 | 6 | 12 | 24 |
| üçlü var | 1 | 0 | 0 | 1 |
| **bu parti** | **15 ikili + 5 üçlü** | — | — | 20 |

## Var olanlar (sıraları dolu — yeni combo bunları tekrar etmez)

**Direkt (10):** İPOTEK→HACİZ HACİZ KESİNLEŞTİ · KONİ→İPOTEK TOPLU İPOTEK · FAİZLİ→İFLAS TEMERRÜT · KARŞILIK→FAİZLİ KARŞI İCRA ·
ÇEK→HACİZ SENET HACZİ · REHİN→KONİ SİLİP SÜPÜR · REHİN→ARBİTRAJ AÇIĞA SATIŞ · TAHVİL→FAİZLİ VADE FARKI · MÜŞTEREK→KİRA ORTAK GİDER ·
üçlü KONİ→HACİZ→İPOTEK HACİZ KARARI.

**Açılır (12, Faz 33c):** KONİ→MÜŞTEREK SÜRÜ · FAİZLİ→ARBİTRAJ TAKİP · HACİZ→REHİN ÖNCELİK · İPOTEK→KİRA TEMİNAT · İFLAS→KARŞILIK
KONKORDATO · REHİN→HACİZ KEFALET · ÇEK→İPOTEK CİRO HATTI · KARŞILIK→ÇEK MUKABELE · TAHVİL→KONİ TEMETTÜ · MÜŞTEREK→İFLAS KONSORSİYUM ·
ARBİTRAJ→TAHVİL SPEKÜLASYON · KİRA→FAİZLİ KİRA GELİRİ.

## Parti 1 — direkt (öneri, **onayını bekliyor**)

Direkt = silahlar elindeyse kendiliğinden çalışır; oyuncunun kendi bulabileceği en okunur kalıplar. Silahın geldiği bölge şartı
kendiliğinden (REHİN, ÇEK 2. bölge · KARŞILIK, TAHVİL 3. · MÜŞTEREK, ARBİTRAJ 4. · KİRA 5.).

### İkili (15)

| # | sıra | ad | ne yapar | kalıp |
|---|---|---|---|---|
| 1 | HACİZ → KONİ | **CEBRİ İCRA** | HACİZ'le vurduğun KONİ'ye geçince sana doğru çekilir; koni onu ve arkasındakileri ×1.5 vurur | K2 topla · K6 konum |
| 2 | KONİ → FAİZLİ | **TOPLU BORÇ** | KONİ'nin vurduğu her düşman için FAİZLİ'nin sıradaki vuruşu +%15 (en çok +%75); savurma bedeli alınmaz | K11 risk |
| 3 | FAİZLİ → HACİZ | **ALACAK DEVRİ** | FAİZLİ'nin vurduğu borçlanır; HACİZ'le BAŞKA birine vurunca o borç (ödediğin savurma bedelinin 3 katı) ona hasar olarak iner | K3 durum + durum |
| 4 | İPOTEK → KONİ | **TAPU İPTALİ** | İPOTEK alanının içine KONİ'yle vurursan alan beklemeden patlar ve koninin yayı alan kadar genişler | K12 kurulum + tetik |
| 5 | HACİZ → İFLAS | **AMME ALACAĞI** | HACİZ'in vurduğuna İFLAS: hedef patlar, aldığı hasarın yarısı 3 birim içindekilere | K1 işaretle-patlat |
| 6 | İFLAS → KONİ | **İFLAS MASASI** | İFLAS'ın öldürdüğü düşmanın taşan hasarı KONİ'ye geçince yayın içindekilere bölünür | K10 zincir |
| 7 | REHİN → İPOTEK | **İPOTEKLİ REHİN** | rehineyi fırlat, İPOTEK'e geç: ipotek rehinenin düştüğü yere hazırlıksız iner | K12 kurulum + tetik |
| 8 | ÇEK → KONİ | **CİRO** | ÇEK dönerken KONİ'ye geç: dönüş yolunun her iki yanına yay iner; ÇEK dönene kadar silahsız kalmazsın | K4 iptal · K2 |
| 9 | KARŞILIK → KONİ | **KARŞILIKLI TAHSİL** | KARŞILIK'ın birikimi tek hedef yerine koninin yayına bölünerek iner (kalabalıkta toplam ×1.25) | K7 savunma → saldırı |
| 10 | TAHVİL → HACİZ | **VADELİ HACİZ** | olgun TAHVİL'i bıraktıktan sonra HACİZ: menzil ×1.5, vuruş TAHVİL'in kademesi kadar ağır (×1.2 / 1.4 / 1.6) | K5 bitiriş |
| 11 | ÇEK → MÜŞTEREK | **ZİNCİRLEME CİRO** | ÇEK'in çarptığı ilk iki düşmanı MÜŞTEREK tek vuruşla birbirine bağlar | K10 zincir · K3 |
| 12 | MÜŞTEREK → KONİ | **MÜŞTEREK KUSUR** | bağlı ikiliden birine KONİ'yle vur: bağ kopar, aradaki çizgide duran herkese bağın taşıdığı hasar iner | K10 zincir |
| 13 | ARBİTRAJ → KONİ | **ARKADAN DOLANIM** | arkadan vuruştan sonra KONİ hedefin arkasındaki kalabalığa açılır ve onlara da "arkadan" sayılır (×1.3) | K6 konum |
| 14 | KİRA → İPOTEK | **KİRALIK ARSA** | kulenin menziline İPOTEK at: kule alanı ateşle tetikler, alan ×1.5 ve kule o alana öncelik verir | K12 · K3 |
| 15 | TAHVİL → ARBİTRAJ | **VADESİ GELMİŞ POZİSYON** | 3. kademe TAHVİL'i bırakınca 1.5 sn görünmez olursun — ARBİTRAJ'la arkaya geçmek için | K5 · K6 |

### Üçlü (5, hepsi 2. bölgeden — üçlü kuralı)

| # | sıra | ad | ne yapar | kalıp |
|---|---|---|---|---|
| Ü1 | FAİZLİ → HACİZ → İFLAS | **İCRA TAKİBİ** | borç yükle, uzaktan işaretle, iflas ettir: işaretliye İFLAS dip vuruşu iner, o ana kadar ödediğin savurma bedelleri iki katıyla geri döner | K11 · K1 |
| Ü2 | REHİN → ÇEK → KONİ | **SENETLİ REHİN** | rehineyi fırlat, ÇEK onun izinden sekip çarptıklarını toplar, KONİ dönüşte kümeyi tek yayda alır | K2 · K10 |
| Ü3 | KARŞILIK → TAHVİL → FAİZLİ | **TEMİNATLI TAHVİL** | KARŞILIK'ın birikimi TAHVİL'i beklemeden 3. kademeye çıkarır; FAİZLİ bitirişi birikimi ve olgunluğu birlikte boşaltır | K7 · K5 |
| Ü4 | MÜŞTEREK → İPOTEK → KONİ | **ORTAKLIĞIN TASFİYESİ** | bağlı ikilinin arasına İPOTEK uzanan bir şerit olarak düşer, KONİ şeridi tetikler: bağlı ikili ve aradakiler birlikte yanar | K12 · K3 |
| Ü5 | KİRA → ARBİTRAJ → HACİZ | **KİRA KONTRATI** | kule kurulu, ARBİTRAJ'la görünmezken kule ateşi "arkadan" sayılır (×2), HACİZ bitirişi kulenin hedefini işaretler, kule ona kilitlenir | K6 · K12 |

## Sıradaki partiler (plan)
- **Parti 2 — açılır 12 ikili + 6 üçlü:** her biri bir silah yükseltmesinin değiştirdiği şeyi kullanır (açan yükseltme EPİK — 33c kuralı).
- **Parti 3–4 — satın alınır 48 ikili + 12 üçlü:** K3 (iki durum), K8 (zaman bükme — saat durur), K9 (evrim) ağırlıklı; combo sunağında saniyeyle.
