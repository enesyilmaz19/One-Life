using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Düşman türleri. `Weapon` ile aynı desen: veri + davranış kancaları, MonoBehaviour değil.
    ///
    /// ⚠️ NEDEN VAR: meta ilerleme yok, yani tekrar oynanabilirliğin TAMAMI koşunun içinden
    /// gelmek zorunda. Tek tip düşmanla dövüş iki dakikada tükeniyordu — bu, oyunun en büyük
    /// batma riskiydi.
    ///
    /// Her tür oyunun çekirdek sorusunu (*hangi düşmana zaman harcayacağım?*) farklı bir
    /// yerden vuruyor. Yalnızca "daha çok can" veya "daha çok hasar" olan bir tür eklemek
    /// çeşitlilik değil, dolgu olurdu.
    /// </summary>
    public abstract class EnemyKind
    {
        public abstract string Name { get; }
        public abstract Color FullColor { get; }

        /// <summary>Hasar verdikçe hasat çarpanı. 0 = vurarak hiçbir şey alamazsın.</summary>
        public virtual float HarvestMultiplier => 1f;

        /// <summary>Yaşadığı sürece oyuncunun saatinden saniyede ne kadar emer.</summary>
        public virtual float AuraDrain(Tuning t) => 0f;

        /// <summary>Havuzu bu orana düşünce kaçmaya başlar. 0 = hiç kaçmaz.</summary>
        public virtual float FleesBelowRatio => 0f;

        public virtual float PoolMultiplier => 1f;
        public virtual float SpeedMultiplier => 1f;

        /// <summary>Öldüğünde. KASA bütün ödemesini burada yapıyor.</summary>
        public virtual void OnKilled(Clock clock, Tuning t, float startingPool) { }

        public static readonly EnemyKind Basic = new BasicKind();
        public static readonly EnemyKind Leak = new LeakKind();
        public static readonly EnemyKind Vault = new VaultKind();
        public static readonly EnemyKind Runner = new RunnerKind();

        /// <summary>Dalga kompozisyonu bu listeden seçiyor.</summary>
        public static readonly EnemyKind[] All = { Basic, Leak, Vault, Runner };
    }

    /// <summary>Temel düşman — dengenin sıfır noktası, diğerleri buna göre okunuyor.</summary>
    public sealed class BasicKind : EnemyKind
    {
        public override string Name => "TEMEL";
        public override Color FullColor => new Color(0.85f, 0.25f, 0.25f);
    }

    /// <summary>
    /// SIZINTI — yaşadığı sürece saatini emer. Vurmasa bile sana mal oluyor.
    ///
    /// Değiştirdiği karar: hedef sırası. Diğerleri "ne kadar kârlı" diye sorarken bu
    /// "ne kadar pahalı" diye sordurur — önce onu öldürmezsen bütün kavga zararına geçer.
    /// Zayıf ve hızlı, ki yakalaması iş olsun.
    /// </summary>
    public sealed class LeakKind : EnemyKind
    {
        public override string Name => "SIZINTI";
        public override Color FullColor => new Color(0.55f, 0.3f, 0.85f);
        public override float AuraDrain(Tuning t) => t.leakAuraDrain;
        public override float PoolMultiplier => 0.55f;
        public override float SpeedMultiplier => 1.3f;
    }

    /// <summary>
    /// KASA — vurarak hiçbir şey alamazsın; bütün havuzunu ÖLDÜĞÜNDE tek seferde öder.
    ///
    /// Değiştirdiği karar: taahhüt. Yarım bırakırsan harcadığın zaman tamamen boşa gider.
    /// "Şimdi vazgeçsem mi" sorusu bu düşmanda gerçek bir bedele bağlanıyor.
    /// Kalın, ki taahhüt gerçekten büyük olsun.
    /// </summary>
    public sealed class VaultKind : EnemyKind
    {
        public override string Name => "KASA";
        public override Color FullColor => new Color(0.9f, 0.75f, 0.2f);
        public override float HarvestMultiplier => 0f;      // vurarak hiçbir şey yok
        public override float PoolMultiplier => 1.9f;
        public override float SpeedMultiplier => 0.75f;

        public override void OnKilled(Clock clock, Tuning t, float startingPool)
        {
            // Bütün ödeme burada. Öldüremezsen sıfır.
            clock?.Add(startingPool * (clock?.HarvestRate ?? 0f) * t.vaultKillBonus);
        }
    }

    /// <summary>
    /// KAÇAK — canı azalınca kaçar. Arena kenarına ulaşırsa kaybolur ve zamanını götürür.
    ///
    /// Değiştirdiği karar: kovalamak mı, bırakmak mı. Kovalamak seni kalabalıktan ayırır
    /// ve saat işlemeye devam eder; bırakmak yatırdığın zamanı çöpe atar.
    /// </summary>
    public sealed class RunnerKind : EnemyKind
    {
        public override string Name => "KAÇAK";
        public override Color FullColor => new Color(0.3f, 0.8f, 0.7f);
        public override float FleesBelowRatio => 0.45f;
        public override float PoolMultiplier => 0.8f;
        public override float SpeedMultiplier => 1.15f;
    }
}
