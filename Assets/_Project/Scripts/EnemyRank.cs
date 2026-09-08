using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Düşman RÜTBESİ — türün üstüne binen katman.
    /// `EnemyKind` *hangi soruyu sorduğunu* söyler, rütbe *ne kadar ağır olduğunu.*
    ///
    /// ⚠️ NEDEN YENİ TÜR DEĞİL: 7 tür × 3 rütbe = 21 alt sınıf olurdu. Rütbe türün
    /// kimliğini değiştirmiyor, türle ÇARPILIYOR. KASA zırhlıyken de KASA.
    ///
    /// ═══ 2026-09-02 YENİDEN TASARIM — Enes: *"iri ve zırhlı ayrışmıyor" ═══
    ///
    /// İlk tasarımda ikisi de "havuz" ekseninde duruyordu (biri yavaş akan zaman,
    /// öteki büyük havuz) ve oyunda AYNI ŞEY gibi hissediliyorlardı. Enes farkı
    /// başka yere koydu ve doğrusu o: **ikisi farklı EKSENDE olmalı.**
    ///
    ///   ZIRHLI → **MİKTAR ekseni.** Her türden olabilir, sadece çok canlı ve yavaş.
    ///            Odada **birkaç tane**. Değiştirdiği şey TEMPO: oda uzuyor,
    ///            hedef sırası önem kazanıyor. Sıradan ama sürekli.
    ///
    ///   İRİ    → **KİMLİK ekseni.** Mini boss: türünün kendi imzası KAT KAT
    ///            büyütülmüş hâli. Birkaç odada bir, ve odada **TEK**.
    ///            İri SIZINTI seni kasıp kavurur, iri KASA servet öder, iri
    ///            ZAMANAŞIMI göz açıp kapayana kadar şişer.
    ///            Değiştirdiği şey ÖNCELİK: odada ondan başka bir şey yok.
    ///
    /// Fark bu yüzden gerçek: ZIRHLI'yı görünce *"bu oda uzun sürecek"* dersin,
    /// İRİ'yi görünce *"önce şunu halletmeliyim"*. Aynı cümleyi kurdurmuyorlar.
    ///
    /// ⚠️ Rütbe GÖRÜNMEK ZORUNDA (ders 10). Zırh soğuk ve mat, İRİ gövdesiyle
    /// bağırıyor — normalin 1.6 katı ve hazırlığı yavaş, yani boss gibi okunuyor.
    /// </summary>
    public abstract class EnemyRank
    {
        public abstract string Name { get; }

        /// <summary>Havuz çarpanı — "daha çok can".</summary>
        public virtual float PoolMultiplier(Tuning t) => 1f;

        /// <summary>
        /// TÜRÜN KENDİ İMZASININ çarpanı — İRİ'nin kadranı.
        ///
        /// ⚠️ İRİ'yi "büyük ZIRHLI"dan ayıran tek şey bu. Yalnızca havuz büyütseydim
        /// iki rütbe yine aynı eksende olurdu ve Enes'in bildirdiği hata aynen
        /// dururdu. Mini boss demek, TÜRÜN NE YAPTIĞININ katlanması demek:
        /// SIZINTI'nın emişi, SİGORTA'nın yarıçapı, MÜBAŞİR'in tebligat sıklığı,
        /// ZAMANAŞIMI'nın büyüme hızı.
        /// </summary>
        public virtual float TraitMultiplier(Tuning t) => 1f;

        public virtual float SpeedMultiplier => 1f;
        public virtual float ScaleMultiplier => 1f;

        /// <summary>Hazırlık süresi çarpanı — İRİ boss gibi AĞIR telegraf ediyor.</summary>
        public virtual float WindupMultiplier => 1f;

        /// <summary>Tür rengini rütbeye göre kaydırır. Kimlik kalsın, ağırlık okunsun.</summary>
        public virtual Color Shade(Color kindColor) => kindColor;

        public virtual bool IsElite => false;

        /// <summary>Odada yalnızca BİR tane olabilir mi (mini boss kuralı).</summary>
        public virtual bool SoloInRoom => false;

        public static readonly EnemyRank Normal = new NormalRank();
        public static readonly EnemyRank Armored = new ArmoredRank();
        public static readonly EnemyRank Huge = new HugeRank();

        public static readonly EnemyRank[] All = { Normal, Armored, Huge };
    }

    /// <summary>Rütbesiz — kıyas noktası.</summary>
    public sealed class NormalRank : EnemyRank
    {
        public override string Name => "";
    }

    /// <summary>
    /// ZIRHLI — **çok canlı ve yavaş.** Her türden olabilir, odada birkaç tane.
    ///
    /// Enes'in tarifi birebir: *"zırhlı tüm çeşitlerden olabilir, sadece daha çok
    /// canı var ve daha yavaş gidiyor."* İmzası değişmiyor — SIZINTI zırhlıyken de
    /// aynı hızda emiyor, sadece daha uzun yaşıyor.
    ///
    /// ⚠️ Yavaşlık şart: çok canlı AMA normal hızda olsaydı, cezası "uzun sürüyor"
    /// değil "kaçamıyorum" olurdu. Yavaşlık oyuncuya BIRAKIP GİTME seçeneği
    /// bırakıyor — ve o seçenek zırhın sorduğu asıl soru: *değer mi?*
    /// </summary>
    public sealed class ArmoredRank : EnemyRank
    {
        public override string Name => "ZIRHLI";
        public override float PoolMultiplier(Tuning t) => t.armoredPoolMultiplier;
        public override float SpeedMultiplier => 0.7f;
        public override float ScaleMultiplier => 1.12f;
        public override bool IsElite => true;

        // Soğuk ve mat. ⚠️ KAÇAK'ın "soluyor" rengi beyaza doğru açılıyor; zırh ters
        // yöne, koyu-soğuğa gidiyor ki ikisi karışmasın (öz-denetim mesafeyi ölçüyor).
        public override Color Shade(Color kindColor) =>
            Color.Lerp(kindColor, new Color(0.30f, 0.36f, 0.46f), 0.5f);
    }

    /// <summary>
    /// İRİ — **mini boss.** Türünün imzası kat kat büyütülmüş hâli. Odada TEK,
    /// ve yalnızca birkaç odada.
    ///
    /// Enes: *"İriler mini boss gibi, birkaç odada gelecek, normal mobların çok
    /// gelişmiş hali gibi."*
    ///
    /// ⚠️ "Gelişmiş" DAHA ÇOK CAN DEĞİL — o zaten ZIRHLI'nın işi. İRİ'nin
    /// gelişmişliği türün NE YAPTIĞININ katlanmasında: iri SIZINTI zamanını
    /// kasıp kavurur, iri SİGORTA yarım odayı kapatır, iri MÜBAŞİR tebligatı
    /// yağdırır, iri KASA servet öder. Havuz da büyük ama asıl olay o değil.
    ///
    /// Boss gibi okunması için hazırlığı YAVAŞ: gövde büyük, telegraf uzun,
    /// yani adil. Güçlü bir şeyin okunur olması gerekiyor.
    /// </summary>
    public sealed class HugeRank : EnemyRank
    {
        public override string Name => "İRİ";
        public override float PoolMultiplier(Tuning t) => t.hugePoolMultiplier;
        public override float TraitMultiplier(Tuning t) => t.hugeTraitMultiplier;
        public override float SpeedMultiplier => 0.8f;
        public override float ScaleMultiplier => 1.6f;
        public override float WindupMultiplier => 1.35f;      // boss gibi ağır telegraf
        public override bool IsElite => true;
        public override bool SoloInRoom => true;

        // Asıl işaret BOYUT ve YAVAŞ HAZIRLIK. Renk yalnızca koyulaşıyor — tür
        // kimliği (KASA sarısı, SIZINTI moru) kalabalıkta okunur kalmalı.
        public override Color Shade(Color kindColor) => Color.Lerp(kindColor, Color.black, 0.28f);
    }
}
