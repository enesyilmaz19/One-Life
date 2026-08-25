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

        /// <summary>Basılı tutulunca saati dondurur mu (Durgun).</summary>
        public virtual bool HoldFreezesClock => false;

        public static Weapon[] All => _all ??= new Weapon[]
        {
            new ConeBlade(), new Faizli(), new Durgun(), new Vadeli()
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
    /// DURGUN — yay gerer gibi. Çekili tuttuğun sürece saatin donar, ama **hiç
    /// kıpırdayamazsın**: saldırı, dash, hareket hepsi kapalı. Dünya donmuyor,
    /// düşmanlar gelmeye devam ediyor. **Bıraktığında vuruş iner.**
    ///
    /// Yani "nefesini tutma" mekaniği: akışı durdurabilirsin, bedeli çakılı kalmak.
    ///
    /// ⚠️ İlk teslimde bu silah tamamen kırıktı — basılı tutma bloğu erken `return`
    /// ediyordu, bırakınca hiçbir şey olmuyordu, yani hiç hasar veremiyordu. Öz-denetim
    /// yeşildi çünkü hiçbir test "bu silah vurabiliyor mu" diye sormamıştı. Artık soruyor.
    /// </summary>
    public sealed class Durgun : Weapon
    {
        public override string Name => "DURGUN";
        public override Color Tint => new Color(0.6f, 0.75f, 1f, 0.5f);
        public override int MaxTargets => 1;
        public override float HarvestMultiplier => 2.2f;
        public override float Angle(Tuning t) => t.singleTargetAngle;
        public override float Range(Tuning t) => t.attackRange * 1.25f;   // uzun namlu
        public override bool HoldFreezesClock => true;
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
