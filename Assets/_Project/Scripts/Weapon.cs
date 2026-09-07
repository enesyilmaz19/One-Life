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
    /// </summary>
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
              GroundMarkDelay(t) > 0f ? "yer"
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
            new ConeBlade(), new Faizli(), new Haciz(), new Ipotek(), new Iflas()
        };
        static Weapon[] _all;
    }

    /// <summary>Faz 1-2'nin temel silahı. Kıyas noktası: geniş koni, herkese vurur, çarpan yok.</summary>
    public sealed class ConeBlade : Weapon
    {
        public override string Name => "KONİ";
        public override Color Tint => new Color(0.65f, 0.9f, 1f, 0.42f);
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
        public override float HitStopMultiplier => 1.9f;
        public override float ShakeMultiplier => 2.2f;

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

        public override float DesperationMultiplier(float seconds, Tuning t)
        {
            // Güvenli eşiğin üstünde en zayıf, panik eşiğinin altında en güçlü.
            float k = Mathf.InverseLerp(t.iflasSafeSeconds, t.iflasPanicSeconds, seconds);
            return Mathf.Lerp(GüvendekiÇarpan, t.iflasMaxDamageMult, k);
        }
    }
}
