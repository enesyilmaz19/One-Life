using System.Collections.Generic;
using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Silahlar. İlke: silah *göründüğü* için değil **kararlarını değiştirdiği** için özgün olur.
    ///
    /// ⚠️ YAPISAL ÇÖZÜM — `HarvestMultiplier`.
    /// Koni saldırısı içindeki herkese vurup her birinden ayrı hasat ediyor, yani hedef
    /// sayısı kadar üstün. Tek hedefli silahlar ancak **hasar başına daha çok zaman
    /// çalarsa** rekabet edebilir. Bu çarpan olmadan koni her zaman baskın olur ve
    /// kalabalık oyunu kolaylaştırır — roguelite'ta tam tersi olması gereken şey.
    ///
    /// Denge iddiası tahmin değil: `Tek Can → Denge simülasyonu` her silahı tarıyor.
    ///
    /// ⚠️ **DURGUN KALDIRILDI** (2026-09-02, Enes: *"sadece akan zamanı durdurmak bir
    /// işimize yaramaz"*). Kodun içindeki *"DURGUN dersi"* yorumları BİLEREK duruyor:
    /// o silah "özelliği değil ETKİYİ test et" dersini öğretti (tamamen kırık teslim
    /// edilmişti, testler yeşildi çünkü hiçbiri *"bu silah vurabiliyor mu"* diye
    /// sormuyordu). Ders `tasks/lessons.md`'de; silah gitti, ders kaldı.
    ///
    /// ⚠️ Bu yorum `Weapon` sınıfına ait; aşağıdaki `WeaponSpecial` araya SONRADAN
    /// girdi (Faz 16) ve bir süre yorumu sahiplenmiş görünüyordu. Sınıf sırası
    /// değişince belgenin de taşınması gerekiyor — yoksa dosyayı açan kişi yanlış
    /// sınıfın gerekçesini okur.
    /// </summary>

    /// <summary>Saldırının biçimi (Faz 31): `PlayerController` buna göre dallanıyor, silah adına göre değil.
    /// Yer vuruşu (İPOTEK) ayrı biçim değil — `GroundMarkDelay` &gt; 0.</summary>
    public enum Biçim { Koni, Rehin, Çek, Karşılık, Tahvil, Müşterek, Arbitraj, Kira }

    /// <summary>
    /// Sağ tık saldırısı — silahın İKİNCİ hamlesi. `BossAttack` ile aynı desen:
    /// veri sınıfı, davranış değil. `PlayerController` bunu okuyup uyguluyor,
    /// yani silah adı geçen tek yer yine `Weapon.cs`.
    ///
    /// ⚠️ TASARIM KURALI: özel saldırı, silahın ZATEN SORDUĞU SORUYU güçlendirir;
    /// yeni bir soru AÇMAZ. Açsaydı beş silah on silah gibi davranırdı ve hiçbiri
    /// kendi kimliğini taşımazdı — TAKSİT'in elenme sebebinin aynısı.
    /// </summary>
    public sealed class WeaponSpecial
    {
        public string Name = "";

        /// <summary>HUD'da ne yaptığı — verisinden türetilemeyen özeller için (Faz 31).</summary>
        public string NeYapar = "";

        /// <summary>&gt;0 ise DURUŞ: bu kadar sn karşılama (KARŞILIK · İTİRAZ).</summary>
        public float Duruş = 0f;

        /// <summary>Kaç saniyede bir kullanılabilir — silahtan silaha değişir (Enes).</summary>
        public float Cooldown = 6f;

        public float RangeMul = 1f;
        /// <summary>Mutlak derece. 360 = çevrende halka.</summary>
        public float Angle = 0f;             // 0 = silahın kendi açısı
        public int MaxTargets = int.MaxValue;

        public float DamageMul = 1f;
        public float HarvestMul = 1f;
        public float Knockback = 0f;

        /// <summary>Peşin bedel (saniye). Kullanmadan ödenir; ödeyemiyorsan çalışmaz.</summary>
        public float Cost = 0f;

        /// <summary>
        /// &gt;0 ise peşin bedel saatin bu payı (en az `Cost`) — İCRA (Faz 29). Yüzde olduğu için
        /// seni hiçbir zaman öldürmez: ne kadar azsan o kadar az ödersin.
        /// </summary>
        public float CostPercent = 0f;

        /// <summary>Ödenen her saniye için ek hasar — "hasar ödenen bedele bağlı" (İCRA).</summary>
        public float DamagePerCost = 0f;

        /// <summary>Öldürünce bedelin bu katı geri gelir. 0 = iade yok.</summary>
        public float KillRefundMul = 0f;

        /// <summary>
        /// &gt;0 ise koni değil GECİKMELİ YER VURUŞU bırakır (İPOTEK). `Hits` &gt; 1 ile YAĞMUR olur:
        /// `RangeMul` alanındaki düşmanların üstüne `HitInterval` arayla peş peşe işaret.
        /// </summary>
        public float GroundDelay = 0f;
        public float GroundRadius = 0f;

        /// <summary>
        /// &gt;0 ise hasar hedefin UZAKLIĞIYLA artar (HACİZ).
        /// Silahın sorusunu tersine çeviriyor: "yaklaşmalı mıyım" → "UZAKLAŞ".
        /// </summary>
        public float DamagePerDistance = 0f;

        /// <summary>
        /// Kaç vuruş arka arkaya (1 = tek vuruş) ve aralarındaki saniye.
        ///
        /// ⚠️ İFLAS'ın kimliği bu: oyunun EN HIZLI silahı, o yüzden özeli de
        /// "büyük tek vuruş" değil SERİ olmalı (Enes kararı). İlk yazdığımda ona
        /// da ağır bir tek vuruş vermiştim — silahın ne olduğuna değil, "özel
        /// saldırı böyle olur"a bakmışım. Özel, silahın kendi karakterinin
        /// abartılmışı olmalı; onu tersine çeviren değil.
        /// </summary>
        public int Hits = 1;
        public float HitInterval = 0.09f;

        /// <summary>Ekrandaki yayın rengi — silahın kendi tonundan ayrışsın diye.</summary>
        public Color Tint = new Color(1f, 1f, 1f, 0.5f);
    }

    /// <summary>
    /// Faz 29 — SİLAHA ÖZEL YÜKSELTMELER (Enes, eski Faz 20: "boss sonrası sunak, silaha özel
    /// yetenekler"). Her silahın üçü; sıra `Weapon.All` sırası × 3. Ana boss düşünce boss odası
    /// SİLAH SUNAĞI'na dönüyor ve KUŞANDIĞIN silahınkileri satıyor. Koşu boyu kalıyor: ana
    /// boss'un temizlediği bölge geri gelmediği için hiçbir ölüm onları geri almıyor.
    /// ⚠️ Yalnızca kuşanılan silahınki geçerli — etkiler `PlayerController`'da, silahın kendi
    /// verisi (`Weapon`) saf kalıyor (silah sınıfları `Clock`/kayıt okumuyor).
    /// </summary>
    public enum SilahYükseltme
    {
        GenişAğ, KısaSüpürme, GeriTepme,       // KONİ
        Bileşik, KısaİCRA, Temerrüt,           // FAİZLİ
        UzunKol, Delici, DarÖlüBölge,          // HACİZ
        ÇiftTapu, HızlıTapu, GenişArsa,        // İPOTEK
        Dip, Seri, Kapanış,                    // İFLAS
        AğırRehin, GenişKol, Kalkan,           // REHİN (Faz 31)
        ÇiftCiro, HızlıTahsil, DönüşKesintisi, // ÇEK
        GenişPencere, TamKarşılık, İade,       // KARŞILIK
        KısaVade, YüksekFaiz, İskonto,         // TAHVİL (Faz 32)
        SıkıBağ, UzunBağ, OrtakHasat,          // MÜŞTEREK
        KeskinBıçak, GenişAçı, Gölge,          // ARBİTRAJ
        UcuzKira, GenişArazi, UzunKontrat,     // KİRA
        // Faz 33a — silah başına +4. Sıranın SONUNA: eski 36'nın numarası değişmiyor, kayıtlar bozulmuyor.
        Kalabalık, Yankı, Halka, Yorgunluk,            // KONİ
        Sermaye, FaizOranı, Sıçrama, FaizsizKredi,     // FAİZLİ
        İhtar, SonSöz, UzunVadeArtı, GeriÇekil,        // HACİZ
        BoşArsa, DerinTemel, Çekim, ZincirTapu,        // İPOTEK
        SonKuyruk, Cesaret, Kurtarma, İflasZinciri,    // İFLAS
        SıkıTutuş, Sorgu, Domino, Tehdit,              // REHİN
        // Faz 33b
        Tahsilat, Yönelim, KarşılıksızArtı, Protesto,          // ÇEK
        Temyiz, Red, Birikim, KısmiÖdeme,                      // KARŞILIK
        Hareketli, Rollover, Kilit, VadeSonu,                  // TAHVİL
        ZincirlemeArtı, Çekişme, GenişOrtaklık, Müteselsil,    // MÜŞTEREK
        SessizAdım, Yayılma, Çiftİşlem, KısaPozisyon,          // ARBİTRAJ
        SağlamYapı, Depozito, Stopaj, ÇiftKule,                // KİRA
        // Faz 33c — combo AÇAN (her silaha bir, EPİK): adı açtığı combo'nun adı
        Sürü, Takip, Öncelik, Teminat, Konkordato, Kefalet,
        CiroHattı, Mukabele, Temettü, Konsorsiyum, Spekülasyon, KiraGeliri
    }

    /// <summary>Faz 33 — yükseltmenin enderliği (Enes 2026-10-18). Silah başına hedef: 3 yaygın · 2 nadir · 2 epik · 1 efsanevi.</summary>
    public enum Enderlik { Yaygın, Nadir, Epik, Efsanevi }

    /// <summary>Faz 33 — sunak grubu: hangi enderlikleri satar, kaç alım (her gruptan 1).</summary>
    public enum SunakGrubu { Mini, BossAlt, BossÜst, Şans }

    public static class SilahSunağı
    {
        public const int SilahBaşına = 3;

        public static readonly string[] Ad =
        {
            "GENİŞ AĞ", "KISA SÜPÜRME", "GERİ TEPME",
            "BİLEŞİK", "KISA İCRA", "TEMERRÜT",
            "UZUN KOL", "DELİCİ", "DAR ÖLÜ BÖLGE",
            "ÇİFT TAPU", "HIZLI TAPU", "GENİŞ ARSA",
            "DİP", "SERİ", "KAPANIŞ",
            "AĞIR REHİN", "GENİŞ KOL", "KALKAN",
            "ÇİFT CİRO", "HIZLI TAHSİL", "DÖNÜŞ KESİNTİSİ",
            "GENİŞ PENCERE", "TAM KARŞILIK", "İADE",
            "KISA VADE", "YÜKSEK FAİZ", "İSKONTO",
            "SIKI BAĞ", "UZUN BAĞ", "ORTAK HASAT",
            "KESKİN BIÇAK", "GENİŞ AÇI", "GÖLGE",
            "UCUZ KİRA", "GENİŞ ARAZİ", "UZUN KONTRAT",
            "KALABALIK", "YANKI", "HALKA", "YORGUNLUK",
            "SERMAYE", "FAİZ ORANI", "SIÇRAMA", "FAİZSİZ KREDİ",
            "İHTAR", "SON SÖZ", "UZUN VADE+", "GERİ ÇEKİL",
            "BOŞ ARSA", "DERİN TEMEL", "ÇEKİM", "ZİNCİR TAPU",
            "SON KUYRUK", "CESARET", "KURTARMA", "İFLAS ZİNCİRİ",
            "SIKI TUTUŞ", "SORGU", "DOMİNO", "TEHDİT",
            "TAHSİLAT", "YÖNELİM", "KARŞILIKSIZ+", "PROTESTO",
            "TEMYİZ", "RED", "BİRİKİM", "KISMİ ÖDEME",
            "HAREKETLİ", "ROLLOVER", "KİLİT", "VADE SONU",
            "ZİNCİRLEME+", "ÇEKİŞME", "GENİŞ ORTAKLIK", "MÜTESELSİL",
            "SESSİZ ADIM", "YAYILMA", "ÇİFT İŞLEM", "KISA POZİSYON",
            "SAĞLAM YAPI", "DEPOZİTO", "STOPAJ", "ÇİFT KULE",
            "SÜRÜ", "TAKİP", "ÖNCELİK", "TEMİNAT", "KONKORDATO", "KEFALET",
            "CİRO HATTI", "MUKABELE", "TEMETTÜ", "KONSORSİYUM", "SPEKÜLASYON", "KİRA GELİRİ",
        };

        public static readonly string[] NeYapar =
        {
            "savurma açısı +%35", "SÜPÜRME beklemesi −%30", "savurma da iter, SÜPÜRME itmesi +%60",
            "öldürünce iade ×2 → ×3", "İCRA beklemesi −%30", "ıskalayınca bedelin yarısı geri",
            "menzil +%25", "iki hedefe vurur", "ölü bölge yarıya · COMBO açar: HACİZ → KARŞILIK → REHİN",
            "aynı anda iki ipotek · COMBO açar: İPOTEK → MÜŞTEREK", "ipotek gecikmesi −%30", "ipotek alanı +%30",
            "dipte daha erken sertleşir (eşikler ×1.5)", "TASFİYE +2 vuruş", "dipte hasat +%50",
            "fırlatma hasarı ×1.6", "tutma menzili ×1.5", "yandan gelenleri de karşılar",
            "bir sekme daha", "çek ×1.4 hızlı", "dönüşte hasar ×1.5",
            "karşılama penceresi ×1.6", "biriken hasar ×1.5", "karşıladığın darbenin yarısı saate geri",
            "kademeler ×0.7 sürede olgunlaşır", "3. kademe ×1.4", "beklerken saat ×1.5 akar (×2 değil) · COMBO açar: TAHVİL → KİRA",
            "bağlıya yayılan hasar %60 → %90", "bağın süresi ×2", "yayılan hasarın hasadı tam (yarım değil)",
            "arkadan ×3.5", "arka sayılan açı 120° → 90°", "görünmezlik ×1.5",
            "kira ×0.6", "kule menzili ×1.4", "kulenin ömrü ×1.5, toplam kira aynı",
            "2+ düşmana değen savurmada hasat ×1.3", "her savurma yarım hasarla bir kez daha iner", "dash-atak çevrendeki herkese (360°)", "vurduklarının saldırı hazırlığı 3 sn boyunca ×1.8 uzun · COMBO açar: KONİ → İFLAS",
            "saat doyma tavanının üstündeyken savurma bedeli yarı", "öldürme iadesi ×2 → ×2.6", "öldürünce yanındakine aynı hasar sıçrar", "isabet eden savurma bedel ödemez · COMBO açar: FAİZLİ → İPOTEK",
            "vurulan 1.5 sn yarı hızla yürür · COMBO açar: HACİZ → ARBİTRAJ", "menzilin son çeyreğinde vuruş ×1.4", "UZUN VADE'nin mesafe hasarı ×1.5", "vurunca bir adım geri kayarsın — mesafe korunur",
            "işaret boşa patlarsa İPOTEK'in sağ tık beklemesi −2 sn", "işaretin merkezinde hasar ×1.5", "patlamadan önce içindekileri merkeze çeker", "patlama vurduklarının yerine (en çok 3) küçük ikinci işaret bırakır, tam hasarla · COMBO açar: İPOTEK → ÇEK → İFLAS",
            "saat 30'un altındayken dash beklemesi yarı", "dipteyken alınan darbe −%20", "dipteyken öldürünce +3 sn · COMBO açar: İFLAS → HACİZ", "TASFİYE öldürürse beklemesi sıfırlanır, öldürdüğü her düşman seriye +1 vuruş (en çok +3) · COMBO açar: İFLAS → TAHVİL → HACİZ",
            "kalkan rehineyi yarı yıpratır", "tutarken rehinenin havuzu akar, sana hasat olur", "fırlatılan rehine çarptığından bir kez daha seker · COMBO açar: REHİN → MÜŞTEREK", "tutarken önündekiler saldırı başlatmaz",
            "dönüşte vurduğu her düşman için +0.25 sn", "sekince en yakın düşmana döner", "KARŞILIKSIZ üç yerine beş çek, beklemesi −%30, havadayken de savurursun · COMBO açar: ÇEK → İFLAS", "dönüşte vurduklarını yanına çeker, yakalayınca çevrene dönüş hasarının iki katı iner · COMBO açar: ÇEK → ARBİTRAJ → REHİN",
            "İTİRAZ ×1.5 uzun", "karşıladığın saldırgan 1 sn yeni saldırı başlatamaz", "biriken tutuldukça büyür: saniyede +%25, en çok ×2 · COMBO açar: KARŞILIK → İPOTEK", "biriken inince yarısı kalır",
            "basılı tutarken yürüyüş ×0.4 → ×0.8", "basılıyken ilk dash ERKEN BOZDURMA'yı da vurur, olgunluğu da bozmaz", "3. kademede beklemek saat akıtmaz", "3. kademe çevrendeki herkese (360°) · COMBO açar: TAHVİL → İFLAS → KONİ",
            "ZİNCİRLEME dört yerine altı düşman", "bağlılar birbirine sürüklenir", "ikinci vuruş yanındakini de bağlar (üçlü bağ) · COMBO açar: MÜŞTEREK → HACİZ", "bağlı biri ölünce öbürleri havuzunun %10'unu kaybeder",
            "görünmezken yürüyüş ×1.3", "arkadan vuruşun yarısı yanındakine iner", "TERS İŞLEM iki kez vurur · COMBO açar: ARBİTRAJ → İFLAS", "arkadan öldürünce AÇIK POZİSYON hemen hazır · COMBO açar: ARBİTRAJ → MÜŞTEREK → ÇEK",
            "TAŞINMA kuleyi bir kez +3 sn uzatır", "kule süresi dolunca ödediğin kiranın %30'u geri", "kule öldürünce +1.5 sn · COMBO açar: KİRA → ÇEK", "aynı anda iki kule, kule başı kira yarım; TAHLİYE ikisini birden",
            "COMBO açar: KONİ → MÜŞTEREK", "COMBO açar: FAİZLİ → ARBİTRAJ", "COMBO açar: HACİZ → REHİN",
            "COMBO açar: İPOTEK → KİRA", "COMBO açar: İFLAS → KARŞILIK", "COMBO açar: REHİN → HACİZ",
            "COMBO açar: ÇEK → İPOTEK", "COMBO açar: KARŞILIK → ÇEK", "COMBO açar: TAHVİL → KONİ",
            "COMBO açar: MÜŞTEREK → İFLAS", "COMBO açar: ARBİTRAJ → TAHVİL", "COMBO açar: KİRA → FAİZLİ",
        };

        // Enderlik — sıra `SilahYükseltme` ile aynı (Y yaygın · N nadir · E epik · F efsanevi).
        // Denge (Enes 2026-10-19, karar A): bot fiyat ölçümünde değerine göre takas — silah başına 3·2·1·1 aynı.
        // FAİZLİ SERMAYE↔FAİZ ORANI · HACİZ DAR ÖLÜ BÖLGE F, GERİ ÇEKİL E, İHTAR N, SON SÖZ ve UZUN VADE+ Y ·
        // REHİN AĞIR REHİN↔KALKAN · ÇEK HIZLI TAHSİL↔YÖNELİM · TAHVİL KISA VADE↔ROLLOVER, İSKONTO↔KİLİT ·
        // KİRA UCUZ KİRA ve GENİŞ ARAZİ N, UZUN KONTRAT ve DEPOZİTO Y. İkinci ölçümden: HACİZ İHTAR E ↔ GERİ ÇEKİL N,
        // ÇEK TAHSİLAT N ↔ ÇİFT CİRO Y, FAİZLİ SIÇRAMA F ↔ FAİZSİZ KREDİ E.
        const Enderlik Y = Enderlik.Yaygın, N = Enderlik.Nadir, E = Enderlik.Epik, F = Enderlik.Efsanevi;
        public static readonly Enderlik[] Ender =
        {
            Y, Y, N,   N, Y, Y,   Y, N, F,   E, Y, Y,   Y, N, Y,   N, Y, Y,      // KONİ … REHİN
            Y, N, Y,   Y, Y, N,   N, Y, E,   Y, Y, N,   N, Y, Y,   N, N, Y,      // ÇEK … KİRA
            N, F, Y, E,   N, Y, F, E,   E, Y, Y, N,   Y, N, N, F,   Y, N, E, F,   Y, N, E, F,   // Faz 33a
            N, Y, E, F,   Y, N, E, F,   Y, Y, N, F,   Y, N, E, F,   Y, N, E, F,   Y, Y, E, F,   // Faz 33b
            E, E, E, E, E, E,   E, E, E, E, E, E,                                                    // Faz 33c — combo açanlar
        };

        public static readonly string[] EnderlikAdı = { "YAYGIN", "NADİR", "EPİK", "EFSANEVİ" };

        public static int Toplam => System.Enum.GetValues(typeof(SilahYükseltme)).Length;

        // İlk 36 (Faz 29–32) silah × 3 + yuva sırasında; Faz 33'ten sonrakiler sonda, silahı tabloda.
        const int İlkSıra = 36;
        static int[] _ekSilah;
        static int[] EkSilah => _ekSilah ??= new[]
        {
            Kadro.KONİ, Kadro.KONİ, Kadro.KONİ, Kadro.KONİ,     Kadro.FAİZLİ, Kadro.FAİZLİ, Kadro.FAİZLİ, Kadro.FAİZLİ,
            Kadro.HACİZ, Kadro.HACİZ, Kadro.HACİZ, Kadro.HACİZ, Kadro.İPOTEK, Kadro.İPOTEK, Kadro.İPOTEK, Kadro.İPOTEK,
            Kadro.İFLAS, Kadro.İFLAS, Kadro.İFLAS, Kadro.İFLAS, Kadro.REHİN, Kadro.REHİN, Kadro.REHİN, Kadro.REHİN,
            Kadro.ÇEK, Kadro.ÇEK, Kadro.ÇEK, Kadro.ÇEK,         Kadro.KARŞILIK, Kadro.KARŞILIK, Kadro.KARŞILIK, Kadro.KARŞILIK,
            Kadro.TAHVİL, Kadro.TAHVİL, Kadro.TAHVİL, Kadro.TAHVİL, Kadro.MÜŞTEREK, Kadro.MÜŞTEREK, Kadro.MÜŞTEREK, Kadro.MÜŞTEREK,
            Kadro.ARBİTRAJ, Kadro.ARBİTRAJ, Kadro.ARBİTRAJ, Kadro.ARBİTRAJ, Kadro.KİRA, Kadro.KİRA, Kadro.KİRA, Kadro.KİRA,
            Kadro.KONİ, Kadro.FAİZLİ, Kadro.HACİZ, Kadro.İPOTEK, Kadro.İFLAS, Kadro.REHİN,
            Kadro.ÇEK, Kadro.KARŞILIK, Kadro.TAHVİL, Kadro.MÜŞTEREK, Kadro.ARBİTRAJ, Kadro.KİRA,
        };

        public static int Silahı(SilahYükseltme y) => (int)y < İlkSıra ? (int)y / SilahBaşına : EkSilah[(int)y - İlkSıra];

        /// <summary>Bir silahın bütün yükseltmeleri, sırayla.</summary>
        public static List<SilahYükseltme> Yükseltmeleri(int silah)
        {
            var l = new List<SilahYükseltme>();
            for (int i = 0; i < Toplam; i++) if (Silahı((SilahYükseltme)i) == silah) l.Add((SilahYükseltme)i);
            return l;
        }

        public static SilahYükseltme Yuvası(int silah, int yuva) => Yükseltmeleri(silah)[yuva];

        // ── TEKLİF (Faz 33): sunak grubu, enderlik, rastgele 3 ──
        public const int TeklifSayısı = 3;
        static readonly Enderlik[][] Gruplar =
        {
            new[] { Enderlik.Yaygın, Enderlik.Nadir },                                  // mini boss sunağı
            new[] { Enderlik.Yaygın, Enderlik.Nadir },                                  // boss: alt sıra
            new[] { Enderlik.Epik, Enderlik.Efsanevi },                                 // boss: üst sıra
            new[] { Enderlik.Yaygın, Enderlik.Nadir, Enderlik.Epik, Enderlik.Efsanevi }, // şans sunağı
        };
        static readonly int[] Ağırlık = { 3, 2, 2, 1 };                                 // yaygın sık, efsanevi seyrek

        public static bool Satar(SunakGrubu g, Enderlik e) => System.Array.IndexOf(Gruplar[(int)g], e) >= 0;

        /// <summary>Koşunun teklif tohumu — yeni koşuda bir kez atılıyor, kayıtta (devam edince aynı teklifler).</summary>
        public static int Tohum
        {
            get
            {
                var s = SaveFile.Current;
                if (s == null) return 1;
                if (s.upgradeSeed == 0) s.upgradeSeed = UnityEngine.Random.Range(1, int.MaxValue);
                return s.upgradeSeed;
            }
        }

        /// <summary>Sunak grubunun kayıt anahtarı: bölge × ara bölge (boss odası = son ara bölgeden sonra) × grup.</summary>
        public static int Anahtar(int bölge, int araBölge, int araBölgeSayısı, SunakGrubu g)
            => (bölge * (araBölgeSayısı + 1) + araBölge) * 4 + (int)g;

        /// <summary>
        /// Teklif: silahın bu grubun enderliklerinden ALINMAMIŞ yükseltmeleri arasından ağırlıklı rastgele 3. Tohum
        /// koşu + sunak anahtarı + silah — silah değiştirip dönünce aynı teklif (yeni zar yok).
        /// </summary>
        public static List<SilahYükseltme> Teklif(int silah, SunakGrubu g, int anahtar)
        {
            var aday = new List<SilahYükseltme>();
            foreach (var y in Yükseltmeleri(silah)) if (Satar(g, Ender[(int)y]) && !Var(y)) aday.Add(y);
            var r = new System.Random(unchecked(Tohum * 31 + anahtar * 7919 + silah * 104729));
            var seçilen = new List<SilahYükseltme>();
            while (seçilen.Count < TeklifSayısı && aday.Count > 0)
            {
                int toplam = 0;
                foreach (var y in aday) toplam += Ağırlık[(int)Ender[(int)y]];
                int zar = r.Next(toplam), i = 0;
                while (zar >= Ağırlık[(int)Ender[(int)aday[i]]]) { zar -= Ağırlık[(int)Ender[(int)aday[i]]]; i++; }
                seçilen.Add(aday[i]);
                aday.RemoveAt(i);
            }
            return seçilen;
        }

        /// <summary>Bu sunak odasında ŞANS sunağı var mı — koşunun tohumundan, odaya sabit.</summary>
        public static bool ŞansVar(Tuning t, int anahtar)
            => new System.Random(unchecked(Tohum * 17 + anahtar * 31337)).NextDouble() < t.chanceAltarProbability;

        public static int GrupAlımı(int anahtar)
        {
            var s = SaveFile.Current;
            return s != null && s.upgradeGroupBought != null && anahtar >= 0 && anahtar < s.upgradeGroupBought.Length
                 ? s.upgradeGroupBought[anahtar] : 0;
        }

        public static bool Var(SilahYükseltme y)
        {
            var s = SaveFile.Current;
            int i = (int)y;
            return s != null && s.weaponUpgrades != null && i < s.weaponUpgrades.Length && s.weaponUpgrades[i] > 0;
        }

        public static int AlınanSayısı
        {
            get
            {
                var s = SaveFile.Current;
                int n = 0;
                if (s != null && s.weaponUpgrades != null) foreach (var v in s.weaponUpgrades) if (v > 0) n++;
                return n;
            }
        }

        /// <summary>Yükseltmeyi kayda yaz ve sunak grubunun alım sayısını artır — kaydın TEK yazma yolu.</summary>
        public static void Al(SilahYükseltme y, int anahtar)
        {
            var s = SaveFile.Current;
            if (s == null) return;
            s.weaponUpgrades = SaveFile.Büyüt(s.weaponUpgrades, Toplam);
            s.weaponUpgrades[(int)y] = 1;
            if (anahtar < 0) return;
            s.upgradeGroupBought = SaveFile.Büyüt(s.upgradeGroupBought, anahtar + 1);
            s.upgradeGroupBought[anahtar]++;
        }

    }

    public abstract class Weapon
    {
        public abstract string Name { get; }
        public abstract Color Tint { get; }

        /// <summary>Bir savurmada en fazla kaç düşman. 1 = tek hedef (en yakını seçilir).</summary>
        public virtual int MaxTargets => int.MaxValue;

        /// <summary>Hasar başına çalınan zamanın çarpanı — silahlar arası dengenin ana kadranı.</summary>
        public virtual float HarvestMultiplier => 1f;

        public virtual float Range(Tuning t) => t.attackRange;
        public virtual float Angle(Tuning t) => t.attackAngle;
        public virtual float Duration(Tuning t) => t.attackDuration;
        public virtual float Damage(Tuning t, float baseDamage) => baseDamage;

        /// <summary>Savurma başlarken. `false` dönerse saldırı hiç başlamaz (ödeyemedin).</summary>
        public virtual bool OnSwingStart(Clock clock, Tuning t) => true;

        /// <summary>Vuruş indikten sonra — isabet sayısı ve öldürme sayısıyla.</summary>
        public virtual void OnStrike(Clock clock, Tuning t, int hits, int kills) { }

        // ── DARBE AĞIRLIĞI ────────────────────────────────────────────────────
        // Enes: *"faizli de kodum mu oturtsun cinsinden olsun."* Ağırlık hissi
        // hasardan gelmiyor — HIT-STOP ve SARSINTIDAN geliyor. Bunlar sabitti, yani
        // bütün silahlar aynı ağırlıkta vuruyordu ve tek hedefli silahın "tek ama
        // sert" olma iddiası ekranda hiç görünmüyordu.

        public virtual float HitStopMultiplier => 1f;
        public virtual float ShakeMultiplier => 1f;

        /// <summary>
        /// Vurduğunu ne kadar GERİ SAVURUYOR (birim/sn).
        ///
        /// ⚠️ Ağırlık hissinin en büyük parçası buydu ve oyunda HİÇ YOKTU: düşmanı
        /// iten bir şey yoktu, yalnızca oyuncu itiliyordu. İlk denemede hit-stop ve
        /// sarsıntıyı büyüttüm, Enes yine *"oturtmuyor"* dedi — çünkü var olanı
        /// büyütmek, OLMAYANIN yerini tutmuyor.
        /// </summary>
        public virtual float Knockback(Tuning t) => 0f;

        /// <summary>Sağ tık saldırısı. null = yok (hiçbir silahta olmamalı).</summary>
        /// <remarks>⚠️ Her çağrıda YENİDEN kuruluyor, önbellek yok. Önbellekliyken ilk
        /// verilen `Tuning`'i sonsuza dek tutuyordu: Inspector'daki canlı ayar (F1 köprüsü)
        /// ve F2-F4 profilleri özel saldırılara HİÇ geçmiyordu (denetim 2026-09-23).</remarks>
        // ponytail: HUD her OnGUI olayında 5-6 küçük nesne kuruyor. Profilde görünürse
        // önbelleği Tuning DEĞERLERİNE bağla (referansına değil).
        public virtual WeaponSpecial Special(Tuning t) => null;

        /// <summary>
        /// Dash-atağın yerine geçen özel — null ise dash-atak silahın normal yayı. Yalnızca
        /// İPOTEK (Faz 29): eski sağ tığı HACİZ İHBARI dash-atağa taşındı (Enes).
        /// </summary>
        public virtual WeaponSpecial DashSpecial(Tuning t) => null;

        /// <summary>Saldırı biçimi (Faz 31) — yeni silahlar kendi biçimini söylüyor.</summary>
        public virtual Biçim Saldırı => Biçim.Koni;

        /// <summary>Savurmanın kaçta kaçında vuruş iniyor. KARŞILIK'ta geç: o ana kadar karşılıyor.</summary>
        public virtual float HitMoment(Tuning t) => t.attackHitMoment;

        /// <summary>Dash-atağın adı ve ne yaptığı — HUD için (yalnızca kendine özgü dash-atağı olanlar).</summary>
        public virtual string DashAtakAdı => null;
        public virtual string DashAtakNeYapar => null;

        // ── DASH-ATAK KİMLİĞİ ────────────────────────────────────────────────
        // Enes: *"her bir silahın özel dash attack olsun, burada çok belli olmuyor
        // neyin ne yaptığı."*
        //
        // ⚠️ Beş silaha beş yeni davranış YAZILMADI: ikisinin dash'i zaten kendi
        // kimliğini taşıyor (KONİ süpürüyor, İFLAS saate göre sertleşiyor). Yalnızca
        // AYRIŞMAYAN üçüne kanca eklendi. Olanı tekrar yazmak, olmayanı eklemekten
        // daha kötü — beşi de "özel" görünür ama üçü aynı şeyi yapardı.

        /// <summary>Dash-atak isabet ederse savurma bedeli geri gelir (FAİZLİ).</summary>
        public virtual bool DashRefundsCost => false;

        /// <summary>
        /// &gt;0 ise bu mesafeden YAKINDAKİNE vuramaz — menzilli silahın ölü bölgesi.
        ///
        /// ⚠️ Yalnızca NORMAL savurmaya uygulanıyor, dash-atağa değil. Sebebi tasarım:
        /// "biri dibine girdi" durumunun bir CEVABI olmalı, yoksa silah çaresiz kalır.
        /// Dash o cevap. Böylece menzilli silah bir mesafe BANDI oynatıyor.
        /// </summary>
        public virtual float MinRange(Tuning t) => 0f;

        // ── YER VURUŞU (İPOTEK) ───────────────────────────────────────────────
        // >0 ise savurma koni vurmaz: nişan noktasına GECİKMELİ bir alan vuruşu
        // bırakır. Düşmanların gecikme boyunca kaçma şansı var — yani silah
        // "nereye vuruyorum" değil "nereye GİDECEKLER" diye sordurur.

        public virtual float GroundMarkDelay(Tuning t) => 0f;
        public virtual float GroundMarkRadius(Tuning t) => 0f;

        /// <summary>
        /// Saat düştükçe hasar çarpanı (İFLAS). 1 = değişmiyor.
        ///
        /// ⚠️ Saniye DIŞARIDAN veriliyor, `Clock.Instance` okunmuyor. Sebep projenin
        /// kendi kuralı: zaman güdümlü şey yazıyorsan girdiyi dışarıdan al, yoksa
        /// editör modunda ETKİ ölçülemez, yalnızca ÖZELLİK sorulabilir. Denge
        /// simülasyonu da bu sayede silahı gerçek eğrisiyle tarayabiliyor.
        /// </summary>
        public virtual float DesperationMultiplier(float seconds, Tuning t) => 1f;

        /// <summary>
        /// HUD etiketi — silahın kimliğini KENDİ verisinden türetiyor.
        /// Ayrı bir `IsRanged`/`IsArea` bayrağı olsaydı asıl alanlardan ayrışır ve
        /// etiket sessizce yalan söylemeye başlardı.
        /// </summary>
        public string Category(Tuning t) =>
              Saldırı == Biçim.Rehin    ? "tutma"
            : Saldırı == Biçim.Çek      ? "fırlatma"
            : Saldırı == Biçim.Karşılık ? "karşılama"
            : Saldırı == Biçim.Tahvil   ? "olgunlaşma"
            : Saldırı == Biçim.Müşterek ? "bağ"
            : Saldırı == Biçim.Arbitraj ? "arkadan"
            : Saldırı == Biçim.Kira     ? "kule"
            : GroundMarkDelay(t) > 0f ? "yer"
            : MinRange(t) > 0f        ? "menzil"
            : MaxTargets == 1         ? "tek"
                                      : "koni";

        // ── DASH-ATAK: silaha göre değişir ────────────────────────────────────
        // Eskiden her silahta aynı geniş alan vuruşuydu; tek hedefli bir silahın
        // dash'te aniden koni olması silahın kimliğini siliyordu.
        // Kural: silah tek hedefliyse dash-atağı da tek hedefli kalır ama ÇOK sert vurur;
        // koni silahın dash-atağı süpürür ama vuruş başına daha az.

        // ⚠️ MENZİLLİ SİLAHIN DASH'İ ONUN YAKIN CEVABIDIR. HACİZ dash'te ölü
        // bölgesini tanımıyor, İPOTEK dash'te yer işareti değil doğrudan yay
        // vuruyor. Kural ikisinde de aynı: uzaktan iş gören silah dibine biri
        // girdiğinde çaresiz kalmamalı, ama bunun bedeli dash'i harcamak olsun.
        // Öz-denetim ikisini de ölçüyor.

        public virtual int DashTargets => MaxTargets;
        public virtual float DashAngle(Tuning t) => MaxTargets == 1 ? t.singleTargetAngle * 1.6f
                                                                   : t.dashAttackAngle;
        public virtual float DashDamageMultiplier(Tuning t) => MaxTargets == 1
            ? t.dashAttackDamageMult * 1.8f      // tek hedefli: tek isabet, ağır darbe
            : t.dashAttackDamageMult;            // koni: çok isabet, hafif darbe

        public static Weapon[] All => _all ??= new Weapon[]
        {
            new ConeBlade(), new Faizli(), new Haciz(), new Ipotek(), new Iflas(),
            new Rehin(), new Cek(), new Karsilik(),         // Faz 31
            new Tahvil(), new Musterek(), new Arbitraj(), new Kira()   // Faz 32
        };
        static Weapon[] _all;
    }

    /// <summary>Faz 1-2'nin temel silahı. Kıyas noktası: geniş koni, herkese vurur, çarpan yok.</summary>
    public sealed class ConeBlade : Weapon
    {
        public override string Name => "KONİ";
        public override Color Tint => new Color(0.65f, 0.9f, 1f, 0.42f);

        /// SÜPÜRME — çevrendeki HERKESE. Sorusu "kaç kişiye birden vurabilirim";
        /// halka o sorunun azamisi. Konumlanmayı ödüllendiriyor: topla, sonra bas.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "SÜPÜRME", Cooldown = t.specialCooldownCone,
            Angle = 360f, RangeMul = 1.5f, DamageMul = 1.15f, Knockback = 5f,
            Tint = new Color(0.65f, 0.95f, 1f, 0.5f)
        };
        // MaxTargets sınırsız, HarvestMultiplier 1 — dengenin sıfır noktası.
    }

    /// <summary>
    /// FAİZLİ — savurmak saniye götürür, öldürünce iki katı geri verir.
    /// Iskalamak pahalı: her boşa savurma doğrudan ömürden kesiliyor.
    /// Tek hedef olduğu için hasat çarpanı yüksek.
    /// </summary>
    public sealed class Faizli : Weapon
    {
        public override string Name => "FAİZLİ";
        public override Color Tint => new Color(1f, 0.85f, 0.35f, 0.5f);
        public override int MaxTargets => 1;
        public override float HarvestMultiplier => 2.6f;
        public override float Angle(Tuning t) => t.singleTargetAngle;

        // ⚠️ "Oturtsun" (Enes). Ağırlık HASARDAN değil DARBEDEN geliyor: hit-stop
        // uzuyor, ekran daha çok sarsılıyor, hasar da artıyor. Üçü birlikte tek
        // vuruşu olay yapıyor — tek hedefli silahın varlık sebebi bu.
        public override float Damage(Tuning t, float baseDamage) => baseDamage * t.faizliDamageMult;
        public override float HitStopMultiplier => 2.4f;
        public override float ShakeMultiplier => 2.6f;
        public override float Knockback(Tuning t) => t.faizliKnockback;   // asıl "oturtan" şey

        /// İCRA — bahsi büyütüyor. Faz 29 (Enes): bedel SAATİNİN YÜZDESİ, hasar ÖDEDİĞİNE bağlı —
        /// ne kadar çok yatırırsan o kadar sert. Öldürürsen bedelin 1.5 katı geri, ama saat doyma
        /// tavanının üstündeyse geri vermez ("eşiğin üstündeysen zaman vermez"). Sorusu yine
        /// "bu savurma tutar mı"; zenginken bahis büyüyor ama kazancı kalkıyor.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "İCRA", Cooldown = t.specialCooldownFaizli,
            MaxTargets = 1, Angle = t.singleTargetAngle * 0.8f, RangeMul = 1.3f,
            DamageMul = 1f, DamagePerCost = t.icraDamagePerCost, HarvestMul = 1.2f,
            Knockback = t.faizliKnockback * 1.8f,
            Cost = t.faizliSwingCost * 3f, CostPercent = t.icraCostPercent, KillRefundMul = t.icraKillRefund,
            Tint = new Color(1f, 0.8f, 0.25f, 0.6f)
        };

        /// ⚠️ Dash-atak isabet ederse savurma bedeli GERİ GELİYOR. FAİZLİ'nin sorusu
        /// "tutar mı"; dash onu en zor haliyle soruyor (hareket halinde, tek hedef)
        /// ve doğru cevabı ödüllendiriyor.
        public override bool DashRefundsCost => true;

        public override bool OnSwingStart(Clock clock, Tuning t)
        {
            // Ödeyemiyorsan savuramıyorsun — silah seni öldürmüyor, sadece susuyor.
            if (clock == null) return true;
            if (clock.Seconds <= t.faizliSwingCost) return false;
            clock.Spend(t.faizliSwingCost);
            return true;
        }

        public override void OnStrike(Clock clock, Tuning t, int hits, int kills)
        {
            if (kills > 0) clock?.Add(t.faizliSwingCost * 2f * kills);
        }
    }

    /// <summary>
    /// HACİZ — uzaktan, dar ve tek hedef. **Dibindekine vuramaz.**
    ///
    /// Sorduğu soru: **"yaklaşmalı mıyım?"** Bu güne kadar dört silahın dördü de yakın
    /// dövüştü, yani *uzak durmak* bedava bir cevaptı — hiçbir silah onu ödüllendirmiyor
    /// da, cezalandırmıyor da değildi, sadece alakasızdı. HACİZ mesafeyi bir TERCİH
    /// haline getiriyor: bir bandın içinde kalman gerekiyor.
    ///
    /// MÜBAŞİR'in (Faz 13) tam karşılığı — o da uzaktan vuruyor, mesafesini koruyor.
    /// Artık oyuncunun elinde de aynı alet var.
    ///
    /// ⚠️ Ölü bölgesi **görünüyor** (savurma yayının içinde ikinci bir halka). Görünmeseydi
    /// oyuncu dibindeki düşmana savurup boşa ıskalar ve sebebini asla anlamazdı —
    /// ders 10'un aynısı: mekanik doğru, görsel yalan.
    /// </summary>
    public sealed class Haciz : Weapon
    {
        public override string Name => "HACİZ";
        public override Color Tint => new Color(0.55f, 1f, 0.7f, 0.42f);
        public override int MaxTargets => 1;
        public override float HarvestMultiplier => 2.2f;
        public override float Range(Tuning t) => t.attackRange * 3.4f;        // Enes: menzil buffu
        public override float Angle(Tuning t) => t.singleTargetAngle * 0.6f;  // dar: nişan
        public override float MinRange(Tuning t) => t.hacizMinRange;          // ölü bölge
        public override float Duration(Tuning t) => t.attackDuration * 1.15f; // nişan alıyor

        /// UZUN VADE — hasar hedefin UZAKLIĞIYLA artıyor. Silahın sorusunu
        /// ("yaklaşmalı mıyım") tersine çevirip cevabını veriyor: UZAKLAŞ.
        /// Ölü bölgesi olan silahın en uzaktan en sert vurması, mesafe bandını
        /// ceza olmaktan çıkarıp SİLAH haline getiriyor.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "UZUN VADE", Cooldown = t.specialCooldownHaciz,
            MaxTargets = 1, Angle = t.singleTargetAngle * 0.45f, RangeMul = 1.5f,
            DamageMul = 0.8f, DamagePerDistance = 0.42f, HarvestMul = 1.1f,
            Tint = new Color(0.55f, 1f, 0.7f, 0.55f)
        };

        // Dash artık diğerleri gibi GİTTİĞİN YÖNE (Faz 25). Faz 16'da nişanın tersine
        // atıyordu; playtest sorusu "kafa karıştırıyor mu" idi, Enes: "hacizin dashi
        // hep terse doğru gidiyor, dash sol tık yapınca düşmandan uzağa kayıp vuruyor."
        // Kimliği dash-atakta kalıyor: ölü bölgeyi tanımıyor, dibindekine iniyor.
    }

    /// <summary>
    /// İPOTEK — nişan aldığın YERE gecikmeli bir alan vuruşu koyuyorsun.
    /// Gecikme boyunca düşmanlar oradan çıkabilir.
    ///
    /// Sorduğu soru: **"nereye GİDECEKLER?"** HACİZ *nişan* silahı (neredeler),
    /// İPOTEK *tahmin* silahı. İkisi de uzak ama aynı soruyu sormuyorlar —
    /// yoksa ikinci menzilli silah birincinin kostümü olurdu.
    ///
    /// ⚠️ SİMSAR ve MÜBAŞİR'in sana yaptığının aynısı, ters yönde. `GroundMark`
    /// aynen kullanılıyor: mekanik zaten oyunda ve oyuncu onu ÖĞRENMİŞ oluyor —
    /// önce üzerinde deneniyor, sonra eline veriliyor.
    ///
    /// Alan vuruşu olduğu için hasat çarpanı düşük; yoksa hem çoklu hem yüksek
    /// çarpan olur ve koninin dengelendiği yerden kaçardı.
    /// </summary>
    public sealed class Ipotek : Weapon
    {
        public override string Name => "İPOTEK";
        public override Color Tint => new Color(1f, 0.45f, 0.95f, 0.42f);
        // ⚠️ Playtest sonrası SERT KISILDI (Enes: "ipotek fazla güçlü amk"). Üç kadran
        // birden çevrildi çünkü üç ayrı yerden güçlüydü: alan + yığılabilirlik + hasat.
        // Yığılma yapısal olarak kapandı (tek işaret); burada kalan ikisi.
        public override float HarvestMultiplier => 0.7f;                    // 1.3 → 0.7
        public override float Range(Tuning t) => t.attackRange * 2.2f;      // koyabildiğin mesafe
        public override float Duration(Tuning t) => t.attackDuration * 1.45f;  // ağır: spam'i kesiyor
        public override float GroundMarkDelay(Tuning t) => t.ipotekDelay;
        public override float GroundMarkRadius(Tuning t) => t.ipotekRadius;
        public override float Damage(Tuning t, float baseDamage) => baseDamage * t.ipotekDamageMult;

        /// İPOTEK YAĞMURU (Faz 29, Enes: "büyük alan içinde düşmanların üstüne birkaç saniye peş
        /// peşe ipotek"). Sorusu aynı — "nereye gidecekler" — ama tek hedef yerine KALABALIK:
        /// işaretler düşmanın DURDUĞU yere düşüyor, gecikme boyunca kaçabiliyorlar.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "İPOTEK YAĞMURU", Cooldown = t.specialCooldownIpotek,
            Hits = t.ipotekRainHits, HitInterval = t.ipotekRainInterval, RangeMul = t.ipotekRainRangeMul,
            GroundDelay = t.ipotekDelay, GroundRadius = t.ipotekRadius * 0.8f,
            DamageMul = 0.9f, HarvestMul = 1f,
            Tint = new Color(1f, 0.45f, 0.95f, 0.5f)
        };

        /// HACİZ İHBARI — çok geniş, çok geç patlayan işaret. Eskiden sağ tıktı, Faz 29'dan beri
        /// DASH-ATAK (Enes). Sorusu aynı ama UFKU uzuyor: iki katı sonrasını tahmin edeceksin.
        /// ⚠️ NİŞAN noktasına düşüyor, dash'in başladığı yere değil — 2026-09-11'de arkana düşen
        /// "hayalet ipotek" nişan aldığınınkinin yerini çalıyordu (Enes'in hatası, Faz 16 kapısı).
        public override WeaponSpecial DashSpecial(Tuning t) => new WeaponSpecial
        {
            Name = "HACİZ İHBARI",
            GroundDelay = t.ipotekDelay * 2.1f, GroundRadius = t.ipotekRadius * 2f,
            RangeMul = 1f, DamageMul = 2.4f, HarvestMul = 1f,
            Tint = new Color(1f, 0.45f, 0.95f, 0.5f)
        };
    }

    /// <summary>
    /// İFLAS — saatin düştükçe daha sert vuruyor. Dolu saatte zayıf, dipte canavar.
    ///
    /// Sorduğu soru: **"ne kadar dibe inmeye cesaretim var?"** Oyunun TEK kaynağını
    /// silahın kadranı yapıyor — başka hiçbir silah saate BAKMIYOR bile.
    ///
    /// ⚠️ Kendi kendini sınırlıyor, o yüzden tavan serbest bırakılabiliyor: 25
    /// saniyedeyken tek darbe koşuyu bitiriyor. "Bilerek dipte kal" stratejisi
    /// teorik olarak var ama bedeli ölüm, ve bu bir TERCİH — istismar değil.
    /// (Playtest'te doğrulanacak; yanlışsa `iflasMaxDamageMult` inecek.)
    /// </summary>
    public sealed class Iflas : Weapon
    {
        /// <summary>Dolu saatteyken cezası — silahın kimliği bu: güvendeyken KÖTÜ.</summary>
        const float GüvendekiÇarpan = 0.7f;

        public override string Name => "İFLAS";
        public override Color Tint => new Color(1f, 0.3f, 0.25f, 0.55f);
        public override int MaxTargets => 1;
        public override float HarvestMultiplier => 2.1f;                        // 1.8 → 2.1
        public override float Angle(Tuning t) => t.singleTargetAngle;

        // ⚠️ HIZLI, ağır değil (Enes: "iflasın saldırı hızına da buff yapalım").
        // 1.25 idi — yani dipteyken sertleşen silah tam da sertleştiği anda YAVAŞ
        // vuruyordu ve eğrisi hissedilmiyordu. Hızlı olması eğriyi görünür kılıyor:
        // dipte hem sert hem seri, yani gerçekten "son çare" gibi çalışıyor.
        public override float Duration(Tuning t) => t.attackDuration * 0.8f;

        /// KAPANIŞ — saatinden büyük bir parça ödüyorsun ve çok sert vuruyorsun.
        ///
        /// ⚠️ Bedeli ayrı bir "güçlenme" mekaniği DEĞİL: ödediğin saniye saatini
        /// düşürüyor, saat düşünce İFLAS zaten sertleşiyor. Yani bedel BİZZAT
        /// buff'ın kendisi. İkinci bir sistem yazmadan silahın sorusunu bir
        /// EYLEME çeviriyor: "ne kadar dibe inmeye cesaretin var" artık bir tuş.
        /// TASFİYE — arka arkaya çok hızlı vuruşlar. Silahın kendi karakterinin
        /// abartılmışı: İFLAS oyunun en hızlı silahı, özeli de SERİ.
        ///
        /// ⚠️ Bedeli YOK ve olmamalı. İFLAS zaten saat düştükçe sertleşiyor; seri
        /// vuruş dipteyken kendiliğinden korkunç oluyor. Üstüne bir de saniye
        /// bedeli koysaydım iki ceza üst üste binerdi ve silah "dipte güçlü" değil
        /// "dipte kullanılamaz" olurdu — kimliğinin tam tersi.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "TASFİYE", Cooldown = t.specialCooldownIflas,
            MaxTargets = 1, Angle = t.singleTargetAngle * 1.2f, RangeMul = 1.1f,
            Hits = t.iflasFlurryHits, HitInterval = t.iflasFlurryInterval,
            DamageMul = 0.62f, HarvestMul = 1f, Knockback = 2f,
            Tint = new Color(1f, 0.3f, 0.25f, 0.6f)
        };

        public override float DesperationMultiplier(float seconds, Tuning t)
        {
            // Güvenli eşiğin üstünde en zayıf, panik eşiğinin altında en güçlü.
            float k = Mathf.InverseLerp(t.iflasSafeSeconds, t.iflasPanicSeconds, seconds);
            return Mathf.Lerp(GüvendekiÇarpan, t.iflasMaxDamageMult, k);
        }
    }

    // ══ FAZ 31 — yeni silahlar 1 (Enes 2026-10-17 onayı) ═══════════════════════════════
    // Davranış `PlayerController`'da `Saldırı` biçimine göre; burada yalnızca veri ve kimlik.

    /// <summary>
    /// REHİN — "kimi tutayım?" İlk vuruş yakındakini YAKALAR (boss yakalanmaz). Tutarken önden gelen
    /// vuruşu rehine yer — kalkan. İkinci vuruş FIRLATIR: yön boyunca ilk çarptığına sert vurur.
    /// </summary>
    public sealed class Rehin : Weapon
    {
        public override string Name => "REHİN";
        public override Color Tint => new Color(0.8f, 0.6f, 0.4f, 0.5f);
        public override Biçim Saldırı => Biçim.Rehin;
        public override int MaxTargets => 1;
        public override float HarvestMultiplier => 1.8f;
        public override float Range(Tuning t) => t.attackRange * 1.1f;      // tutma menzili
        public override float Angle(Tuning t) => 120f;
        public override float Duration(Tuning t) => t.attackDuration * 1.1f;
        public override string DashAtakAdı => "SÜRÜKLEME";
        public override string DashAtakNeYapar => "tutuyorsan dash sonunda yere çarpar (çevresine de), tutmuyorsan yakalar";

        /// TAKAS — tuttuğunu en yakın ikinci düşmanla yer değiştirtir, ikisine birden vurur; yeni rehine
        /// o ikincisi. Tutmuyorsan iki kat menzilden yakalar.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "TAKAS", Cooldown = t.specialCooldownRehin, DamageMul = 1.4f,
            NeYapar = "tuttuğunu en yakın ikinci düşmanla yer değiştirtir, ikisine vurur",
            Tint = new Color(0.8f, 0.6f, 0.4f, 0.6f)
        };
    }

    /// <summary>
    /// ÇEK — "geri dönüş yolunu nasıl kurarım?" Fırlatılır, arena kenarından seker, sana döner;
    /// gidişte de dönüşte de vurur. Dönene kadar silahsızsın (`CekUcusu`).
    /// </summary>
    public sealed class Cek : Weapon
    {
        public override string Name => "ÇEK";
        public override Color Tint => new Color(0.6f, 0.85f, 0.95f, 0.5f);
        public override Biçim Saldırı => Biçim.Çek;
        public override float HarvestMultiplier => 1.3f;
        public override float Range(Tuning t) => t.attackRange * 4f;        // gidiş mesafesi
        public override float Damage(Tuning t, float baseDamage) => baseDamage * 0.9f;
        public override string DashAtakAdı => "CİRO";
        public override string DashAtakNeYapar => "havadaki çeki yakalayıp dash yönüne yeniden fırlatır";

        /// KARŞILIKSIZ — üç çek yelpaze gibi, her biri bir kez seker.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "KARŞILIKSIZ", Cooldown = t.specialCooldownCek, Hits = 3, DamageMul = 0.6f,
            NeYapar = "üç çek yelpaze gibi fırlar, her biri seker",
            Tint = new Color(0.6f, 0.85f, 0.95f, 0.6f)
        };
    }

    /// <summary>
    /// KARŞILIK — "ne zaman karşılamalıyım?" Kısa karşılama: o anda gelen darbe saatinden düşmez,
    /// birikir; pencere bitince biriken önündekilere iner. Kimse vurmadıysa zayıf bir vuruş.
    /// </summary>
    public sealed class Karsilik : Weapon
    {
        public override string Name => "KARŞILIK";
        public override Color Tint => new Color(0.95f, 0.95f, 0.6f, 0.5f);
        public override Biçim Saldırı => Biçim.Karşılık;
        public override float HarvestMultiplier => 1.4f;
        public override float Angle(Tuning t) => 100f;
        /// Pencere = süre × vuruş anı ≈ 0.23 sn, ardından ~0.43 sn TOPARLANMA. Ortak vuruş anıyla (%12)
        /// pencere 0.05 sn olurdu — oynanmaz. Toparlanma yoksa (ilk hâli: pencere savurmanın %80'i)
        /// tıklamayı basılı tutan zamanın ~%75'inde dokunulmaz oluyordu; şimdi ~%35 ve açık kaldığı
        /// an karşılayamıyor. Spam değil zamanlama ödüllendiriliyor.
        public override float Duration(Tuning t) => t.attackDuration * 2.2f;
        public override float HitMoment(Tuning t) => 0.35f;
        public override string DashAtakAdı => "KARŞI DAVA";
        public override string DashAtakNeYapar => "biriken karşılık varsa dash-atak onun iki katıyla vurur";

        /// İTİRAZ — uzun karşılama; o sırada gelen her darbe birikir, sonunda hepsi önündekilere.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "İTİRAZ", Cooldown = t.specialCooldownKarsilik, Duruş = t.karsilikItirazSeconds, RangeMul = 1.5f,
            NeYapar = $"{t.karsilikItirazSeconds:0.0} sn karşılar, biriken hepsi önündekilere",
            Tint = new Color(0.95f, 0.95f, 0.6f, 0.6f)
        };
    }

    // ══ FAZ 32 — yeni silahlar 2 ══════════════════════════════════════════════════════
    // Davranış `PlayerController`'da (biçime göre), bağ `MusterekBag.cs`, kule `KiraKulesi.cs`.

    /// <summary>
    /// TAHVİL — "ne kadar bekleyeyim?" Sol tıkı basılı tut: vuruş kademe kademe olgunlaşır, beklerken
    /// saat iki kat akar; bırakınca iner. Beklemenin bedeli de ödülü de saniye.
    /// </summary>
    public sealed class Tahvil : Weapon
    {
        public override string Name => "TAHVİL";
        public override Color Tint => new Color(0.55f, 0.95f, 0.65f, 0.5f);
        public override Biçim Saldırı => Biçim.Tahvil;
        public override float HarvestMultiplier => 1.2f;
        public override string DashAtakAdı => "ERKEN BOZDURMA";
        public override string DashAtakNeYapar => "basılı tutarken dash: olgunluğun yarısı dash-atağa biner, geniş";

        /// <summary>Kademenin hasar çarpanı (0 = olgunlaşmamış).</summary>
        public static float KademeÇarpanı(int kademe) => kademe <= 0 ? 0.7f : kademe == 1 ? 1.6f : kademe == 2 ? 2.8f : 4.5f;

        /// VADESİ GELMİŞ — o anki olgunluğu (en az 1. kademe) üç parçaya bölüp üç düşmana dağıtır.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "VADESİ GELMİŞ", Cooldown = t.specialCooldownTahvil, MaxTargets = 3, RangeMul = 1.5f, Angle = 140f,
            NeYapar = "o anki olgunluğu (en az 1. kademe) önündeki üç düşmana bölüştürür",
            Tint = new Color(0.55f, 0.95f, 0.65f, 0.6f)
        };
    }

    /// <summary>
    /// MÜŞTEREK — "kimi kime bağlarım?" Vurduğunu işaretler, sonraki vuruşta ikinciyi ona bağlar:
    /// birine inen hasarın bir payı öbürüne de iner — hangi silahtan gelirse gelsin.
    /// </summary>
    public sealed class Musterek : Weapon
    {
        public override string Name => "MÜŞTEREK";
        public override Color Tint => new Color(0.95f, 0.6f, 0.9f, 0.5f);
        public override Biçim Saldırı => Biçim.Müşterek;
        public override int MaxTargets => 1;
        public override float HarvestMultiplier => 1.6f;
        public override float Range(Tuning t) => t.attackRange * 1.2f;
        public override float Angle(Tuning t) => t.singleTargetAngle * 1.4f;
        public override float Damage(Tuning t, float baseDamage) => baseDamage * 0.8f;
        public override string DashAtakAdı => "KOPARMA";
        public override string DashAtakNeYapar => "bağın üstünden dash'le geç: bağ kopar, uçlar birbirine çarpar (×2)";

        /// ZİNCİRLEME — çevrendeki en yakın dört düşmanı tek zincire takar.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "ZİNCİRLEME", Cooldown = t.specialCooldownMusterek, MaxTargets = 4, RangeMul = 2.5f, DamageMul = 0.5f,
            NeYapar = "çevrendeki en yakın dört düşmanı tek zincire takar",
            Tint = new Color(0.95f, 0.6f, 0.9f, 0.6f)
        };
    }

    /// <summary>
    /// ARBİTRAJ — "arkasına geçebilir miyim?" Arkadan çok sert, önden zayıf. Düşmanlar sana dönük
    /// yürüdüğü için arkaya geçmek iş: dash'in (TERS İŞLEM) ya da görünmezliğin (AÇIK POZİSYON).
    /// </summary>
    public sealed class Arbitraj : Weapon
    {
        public override string Name => "ARBİTRAJ";
        public override Color Tint => new Color(0.75f, 0.75f, 0.95f, 0.5f);
        public override Biçim Saldırı => Biçim.Arbitraj;
        public override int MaxTargets => 1;
        public override float HarvestMultiplier => 1.7f;
        public override float Angle(Tuning t) => t.singleTargetAngle * 1.6f;
        public override string DashAtakAdı => "TERS İŞLEM";
        public override string DashAtakNeYapar => "içinden geçtiğin düşmana dönüp arkadan vurursun";

        /// AÇIK POZİSYON — kısa süre görünmez; ilk vuruş her yönden "arkadan".
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "AÇIK POZİSYON", Cooldown = t.specialCooldownArbitraj,
            NeYapar = $"{t.arbitrajHideSeconds:0} sn görünmezsin: yeni saldırı yok, ilk vuruşun arkadan sayılır",
            Tint = new Color(0.75f, 0.75f, 0.95f, 0.6f)
        };
    }

    /// <summary>
    /// KİRA — "bu alanı ne kadar tutayım?" Nişana kule kurarsın; kule kira öder (saniyende saniye),
    /// kurulu kaldıkça en yakın düşmana ateş eder ve hasat eder. Kule varken sol tık zayıf bir savurma.
    /// </summary>
    public sealed class Kira : Weapon
    {
        public override string Name => "KİRA";
        public override Color Tint => new Color(0.95f, 0.8f, 0.45f, 0.5f);
        public override Biçim Saldırı => Biçim.Kira;
        public override float HarvestMultiplier => 1.2f;
        public override float Range(Tuning t) => t.attackRange * 2.5f;     // kurma menzili
        public override float Damage(Tuning t, float baseDamage) => baseDamage * 0.6f;
        public override string DashAtakAdı => "TAŞINMA";
        public override string DashAtakNeYapar => "kuleyi bulunduğun yere taşırsın (kira sürer)";

        /// TAHLİYE — kuleyi patlat: kalan kira süresi kadar hasar.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "TAHLİYE", Cooldown = t.specialCooldownKira,
            NeYapar = "kalan kirayı peşin ödersin, kule patlar: kalan kira süresi kadar hasar",
            Tint = new Color(0.95f, 0.8f, 0.45f, 0.6f)
        };
    }
}
