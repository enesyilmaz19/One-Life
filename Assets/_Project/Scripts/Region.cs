using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Bölge: birkaç oda + kendi düşman havuzu + **kendi kuralı** + boss.
    /// `Weapon` / `EnemyKind` / `RoomLayout` / `RevivalKind` ile aynı desen.
    ///
    /// ⚠️ **KURAL SAATTEN GEÇER.** Enes "bölgeler kurala ve hikâyeye göre ayrılır"
    /// dedi; kuralların hepsi tek sayıya — saniyeye — dokunuyor, çünkü yasa o.
    /// Bölgeye özel can barı, zırh, direnç YOK; olsaydı ikinci bir sistem doğardı.
    ///
    /// Ucuz ama büyük kazanç: her kural tek bir çarpan, ama bölgeler gerçekten
    /// FARKLI HİSSETTİRİYOR — sadece farklı görünmüyor.
    /// </summary>
    public abstract class Region
    {
        public abstract string Name { get; }
        public abstract string RuleText { get; }
        public abstract Color GroundColor { get; }

        /// <summary>Dövüş dışı akış çarpanı — oyalanmanın bedeli.</summary>
        public virtual float DrainMultiplier => 1f;

        /// <summary>Hasat çarpanı — vuruş başına ne kadar alıyorsun.</summary>
        public virtual float HarvestMultiplier => 1f;

        /// <summary>Darbe yeme bedeli çarpanı — hata ne kadar acıtıyor.</summary>
        public virtual float HitCostMultiplier => 1f;

        /// <summary>Sürekli arka plan emişi (saniyede saniye). Dövüşte de işler.</summary>
        public virtual float BackgroundDrain => 0f;

        /// <summary>Bu bölgede kaç düşman türü açık.</summary>
        public abstract int UnlockedKinds { get; }

        /// <summary>Zincirdeki sırası — 0 tabanlı.</summary>
        public int Index { get; private set; }

        // ── varyantlar ───────────────────────────────────────────────────────

        sealed class Plain : Region
        {
            public override string Name => "EŞİK";
            public override string RuleText => "";                  // kural yok: önce öğren
            public override Color GroundColor => new Color(0.16f, 0.16f, 0.19f);
            public override int UnlockedKinds => 2;
        }

        sealed class Pressure : Region
        {
            public override string Name => "DARALMA";
            public override string RuleText => "oyalanmak pahalı";
            public override Color GroundColor => new Color(0.20f, 0.15f, 0.15f);
            // Dövüş dışı akış hızlanıyor: durup düşünmek, sunağa bakmak, nefes almak
            // burada daha çok kesiyor. Oyuncuyu ileri itiyor.
            public override float DrainMultiplier => 1.7f;
            public override int UnlockedKinds => 3;
        }

        sealed class Scarcity : Region
        {
            public override string Name => "KITLIK";
            public override string RuleText => "hasat az";
            public override Color GroundColor => new Color(0.15f, 0.18f, 0.17f);
            // Her vuruş daha az veriyor: dövüş kazanç değil, masraf olmaya yaklaşıyor.
            // Hangi düşmanı ALMAYA DEĞER sorusu burada keskinleşiyor.
            public override float HarvestMultiplier => 0.65f;
            public override int UnlockedKinds => 4;
        }

        sealed class Weight : Region
        {
            public override string Name => "AĞIRLIK";
            public override string RuleText => "hata affetmiyor";
            public override Color GroundColor => new Color(0.19f, 0.16f, 0.21f);
            // Darbe yemek çok daha pahalı: kaçınma zorunlu hale geliyor,
            // "yeriz devam ederiz" oynanışı burada çalışmıyor.
            public override float HitCostMultiplier => 1.8f;
            public override int UnlockedKinds => 5;
        }

        sealed class Seep : Region
        {
            public override string Name => "SIZINTI";
            public override string RuleText => "sürekli emiliyor";
            public override Color GroundColor => new Color(0.14f, 0.14f, 0.22f);
            // Arka planda sabit emiş — dövüşte bile. Tek çare hızlı olmak.
            public override float BackgroundDrain => 0.6f;
            public override int UnlockedKinds => 6;
        }

        public static readonly Region[] All =
        {
            new Plain(), new Pressure(), new Scarcity(), new Weight(), new Seep()
        };

        static Region()
        {
            for (int i = 0; i < All.Length; i++) All[i].Index = i;
        }

        /// <summary>Zincirdeki bölge; taşarsa sonuncuda kalır.</summary>
        public static Region At(int index) => All[Mathf.Clamp(index, 0, All.Length - 1)];

        /// <summary>
        /// Şu an içinde olunan bölge. `RoomManager` yazıyor; `Clock` ve `Enemy` okuyor.
        /// ⚠️ Null olabilir (öz-denetimler bölge kurmadan da koşuyor) — okuyanlar
        /// `Mul()` yardımcılarını kullansın, doğrudan `Current.X` yazmasın.
        /// </summary>
        public static Region Current { get; private set; }

        public static void SetCurrent(Region r) => Current = r;
        public static void Reset() => Current = null;

        // Bölge yokken kural da yok — okuyanların her yerde null kontrolü yapmaması için.
        public static float Drain => Current?.DrainMultiplier ?? 1f;
        public static float Harvest => Current?.HarvestMultiplier ?? 1f;
        public static float HitCost => Current?.HitCostMultiplier ?? 1f;
        public static float Background => Current?.BackgroundDrain ?? 0f;
    }
}
