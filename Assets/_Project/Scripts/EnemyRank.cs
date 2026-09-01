using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Düşman RÜTBESİ — türün üstüne binen ince katman.
    /// `EnemyKind` *hangi soruyu sorduğunu* söyler, rütbe *bu sefer ne kadar ağır olduğunu.*
    ///
    /// ⚠️ NEDEN YENİ TÜR DEĞİL: 7 tür × 3 rütbe = 21 alt sınıf olurdu. Rütbe türün
    /// kimliğini değiştirmiyor, AĞIRLIĞINI değiştiriyor — o yüzden türle ÇARPILIYOR,
    /// türün yerine geçmiyor. KASA zırhlıyken de KASA'dır.
    ///
    /// ⚠️ ZIRHLI İLE İRİ AYNI ŞEY DEĞİL — birleştirmek ikisini de öldürür:
    ///
    ///   İRİ    → havuzu büyük, akışı normal.
    ///            Uzun sürer ama sonunda DAHA ÇOK öder. Saniye başına gelirin AYNI.
    ///            Sattığı şey: **TAAHHÜT.** "Bunu bitirmeye kalkışayım mı?"
    ///
    ///   ZIRHLI → havuzu normal, hasar zamana daha yavaş çevriliyor.
    ///            Uzun sürer ve toplamda AYNI öder — yani saniye başına gelirin DÜŞER.
    ///            Sattığı şey: **TEMPO.** "Bunu şimdi mi kesmeliyim?"
    ///
    /// Enes ikisini ayrı ayrı söyledi ("zırhlı ya da daha çok canlı"). Tek bir "elit"
    /// rütbesinde toplasaydım iki farklı soru tek dolgu düşmana inerdi — ve `EnemyKind`
    /// zaten tam bunu yasaklıyor: *"yalnızca daha çok can olan bir tür çeşitlilik değil."*
    ///
    /// ⚠️ Rütbe GÖRÜNMEK ZORUNDA. Zırhlı düşman normal görünürse oyuncu "niye ölmüyor
    /// bu" diye sorar ve cevabı ekranda yoktur — dersler 7/10'un tam hatası. Zırh soğuk
    /// ve mat, İRİ iri. İkisi de mekaniğin ne yaptığını söylüyor.
    /// </summary>
    public abstract class EnemyRank
    {
        public abstract string Name { get; }

        /// <summary>Havuz çarpanı — İRİ'nin kadranı.</summary>
        public virtual float PoolMultiplier(Tuning t) => 1f;

        /// <summary>
        /// Hasarın zamana çevrilme oranı. &lt;1 = havuzu daha YAVAŞ akıyor.
        /// Hasat akan zamanla orantılı olduğu için toplam ödeme değişmez, sadece
        /// daha uzun sürer: saf tempo vergisi.
        /// </summary>
        public virtual float DrainMultiplier(Tuning t) => 1f;

        public virtual float SpeedMultiplier => 1f;
        public virtual float ScaleMultiplier => 1f;

        /// <summary>Tür rengini rütbeye göre kaydırır. Kimlik kalsın, ağırlık okunsun.</summary>
        public virtual Color Shade(Color kindColor) => kindColor;

        /// <summary>Normal dışındaki her rütbe. HUD ve öz-denetim için.</summary>
        public virtual bool IsElite => false;

        public static readonly EnemyRank Normal = new NormalRank();
        public static readonly EnemyRank Armored = new ArmoredRank();
        public static readonly EnemyRank Heavy = new HeavyRank();

        public static readonly EnemyRank[] All = { Normal, Armored, Heavy };
    }

    /// <summary>Rütbesiz — kıyas noktası. Diğer ikisi buna göre okunuyor.</summary>
    public sealed class NormalRank : EnemyRank
    {
        public override string Name => "";
    }

    /// <summary>
    /// ZIRHLI — zamanı çok daha yavaş akıyor. Havuzu normal, ama her vuruş ondan
    /// daha az koparıyor. Aynı parayı çok daha uzun sürede ödüyor.
    ///
    /// Bu, oyunun yasasında **gelir vergisidir**: saat sen vururken de işliyor,
    /// yani saniye başına düşen hasat düşünce dövüş kârdan masrafa kayıyor.
    /// Cevabı "daha çok vur" değil, "buna girmeye değer mi".
    ///
    /// Biraz yavaş — zırh ağırdır, ve oyuncuya kaçma seçeneği bırakması gerekiyor.
    /// </summary>
    public sealed class ArmoredRank : EnemyRank
    {
        public override string Name => "ZIRHLI";
        public override float DrainMultiplier(Tuning t) => t.armoredDrainMultiplier;
        public override float SpeedMultiplier => 0.85f;
        public override bool IsElite => true;

        // Soğuk ve mat. ⚠️ KAÇAK'ın "soluyor" rengi beyaza doğru açılıyor; zırh ters
        // yöne, koyu-soğuğa gidiyor ki ikisi karışmasın (öz-denetim mesafeyi ölçüyor).
        public override Color Shade(Color kindColor) =>
            Color.Lerp(kindColor, new Color(0.30f, 0.36f, 0.46f), 0.5f);
    }

    /// <summary>
    /// İRİ — havuzu kat kat büyük, akışı normal. Uzun sürer ama tamamını öder.
    ///
    /// Bu bir vergi değil, **taahhüt**: girersen kârlısın, yarıda bırakırsan
    /// harcadığın zaman gider. KASA'nın sorduğu soruyu her türe taşıyor.
    ///
    /// İri ve yavaş: tehdidi kaçamaması değil, seni ORADA TUTMASI.
    /// </summary>
    public sealed class HeavyRank : EnemyRank
    {
        public override string Name => "İRİ";
        public override float PoolMultiplier(Tuning t) => t.heavyPoolMultiplier;
        public override float SpeedMultiplier => 0.78f;
        public override float ScaleMultiplier => 1.32f;
        public override bool IsElite => true;

        // Asıl işaret BOYUT. Renk yalnızca bir tık koyulaşıyor — tür kimliği
        // (KASA sarısı, SIZINTI moru) kalabalıkta okunur kalmalı.
        public override Color Shade(Color kindColor) => Color.Lerp(kindColor, Color.black, 0.22f);
    }
}
