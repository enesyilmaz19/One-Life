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

        /// <summary>
        /// &gt;0 ise bu mesafeden YAKINDAKİNE vuramaz — menzilli silahın ölü bölgesi.
        ///
        /// ⚠️ Yalnızca NORMAL savurmaya uygulanıyor, dash-atağa değil. Sebebi tasarım:
        /// "biri dibine girdi" durumunun bir CEVABI olmalı, yoksa silah çaresiz kalır.
        /// Dash o cevap. Böylece menzilli silah bir mesafe BANDI oynatıyor.
        /// </summary>
        public virtual float MinRange(Tuning t) => 0f;

        // ── DASH-ATAK: silaha göre değişir ────────────────────────────────────
        // Eskiden her silahta aynı geniş alan vuruşuydu; tek hedefli bir silahın
        // dash'te aniden koni olması silahın kimliğini siliyordu.
        // Kural: silah tek hedefliyse dash-atağı da tek hedefli kalır ama ÇOK sert vurur;
        // koni silahın dash-atağı süpürür ama vuruş başına daha az.

        public virtual int DashTargets => MaxTargets;
        public virtual float DashAngle(Tuning t) => MaxTargets == 1 ? t.singleTargetAngle * 1.6f
                                                                   : t.dashAttackAngle;
        public virtual float DashDamageMultiplier(Tuning t) => MaxTargets == 1
            ? t.dashAttackDamageMult * 1.8f      // tek hedefli: tek isabet, ağır darbe
            : t.dashAttackDamageMult;            // koni: çok isabet, hafif darbe

        public static Weapon[] All => _all ??= new Weapon[]
        {
            new ConeBlade(), new Faizli(), new Vadeli(), new Haciz()
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
        public override float Range(Tuning t) => t.attackRange * 2.6f;        // uzun
        public override float Angle(Tuning t) => t.singleTargetAngle * 0.6f;  // dar: nişan
        public override float MinRange(Tuning t) => t.hacizMinRange;          // ölü bölge
        public override float Duration(Tuning t) => t.attackDuration * 1.15f; // nişan alıyor
    }

    /// <summary>
    /// VADELİ — korkunç hasar, ama bedeli vuruş İNDİKTEN sonra kesiliyor.
    /// Savurmaya başlarken hesap yapamazsın; savurmanın ortasında ölebilirsin.
    /// </summary>
    public sealed class Vadeli : Weapon
    {
        public override string Name => "VADELİ";
        public override Color Tint => new Color(1f, 0.4f, 0.35f, 0.55f);
        public override int MaxTargets => 1;
        public override float HarvestMultiplier => 2.0f;
        public override float Angle(Tuning t) => t.singleTargetAngle;
        public override float Duration(Tuning t) => t.attackDuration * 1.35f;  // ağır
        public override float Damage(Tuning t, float baseDamage) => baseDamage * t.vadeliDamageMult;

        public override void OnStrike(Clock clock, Tuning t, int hits, int kills)
        {
            // Bedel vuruştan SONRA — ıskaladıysan bile ödüyorsun ve ölebilirsin.
            clock?.Spend(t.vadeliCost);
        }
    }
}
