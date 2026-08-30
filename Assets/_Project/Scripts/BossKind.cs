using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Bir boss saldırısı — veri, davranış değil. Mevcut saldırı makinesi
    /// (hazırlık yayı → kilitli yön → koni kontrolü) zaten bu parametrelerle çalışıyordu;
    /// boss'a özgü saldırı yapmak için yeni makine değil, yeni VERİ gerekiyordu.
    ///
    /// ⚠️ Her saldırı FARKLI BİR SORU sormalı:
    ///   • koni    → yana kaç
    ///   • halka   → UZAKLAŞ
    ///   • simit   → YAKLAŞ (tersi! içi güvenli)
    ///   • ışın    → hattından çık
    ///   • tekrar  → bir kez kaçmak yetmiyor
    /// İkisi aynı soruyu soruyorsa biri gereksizdir.
    /// </summary>
    public sealed class BossAttack
    {
        public string Name = "";

        /// <summary>Boss'un normal menziline çarpan.</summary>
        public float RangeMul = 1f;

        /// <summary>Mutlak derece. 360 = halka (yön kontrolü yok).</summary>
        public float Angle = 0f;          // 0 = düşmanın kendi açısı

        /// <summary>
        /// &gt;0 ise SİMİT: bu yarıçapın İÇİ GÜVENLİ. Halkanın tam tersi soruyu sorar —
        /// "uzaklaş" değil "YAKLAŞ". Boss'un dibi en güvenli yer olunca oyuncu
        /// alışkanlığını bozmak zorunda kalıyor.
        /// </summary>
        public float InnerRadius = 0f;

        public float WindupMul = 1f;
        public float CostMul = 1f;

        /// <summary>Kaç kez arka arkaya vuruyor — bir kez kaçmak yetmesin.</summary>
        public int Repeat = 1;

        /// <summary>Vuruş anında oyuncuya doğru atılma mesafesi — mesafeyi kapatır.</summary>
        public float Lunge = 0f;

        public Color Tint = new Color(1f, 0.45f, 0.15f, 0.30f);
    }

    /// <summary>
    /// Boss kimliği. `EnemyKind` / `Weapon` / `RoomLayout` / `Region` ile aynı desen.
    ///
    /// ⚠️ **HER BOSS SUÇUNU DÖVÜŞÜR.** Mekanikler uydurma değil — borç ekonomisinde
    /// ne yaptıklarının doğrudan sonucu (bkz. `docs/TASARIM.md` → BORÇ EKONOMİSİ):
    ///
    ///   ① TAHSİLDAR  zorla alan      → yanındayken emer, sonra KOVALAR
    ///   ② EKSPER     değeri o biçer  → hasadını düşürür, sonra SIFIRLAR
    ///   ③ KEFİL      borcu garantiler→ kendini de garantiler: İKİYE BÖLÜNÜR
    ///   ④ MURABAHACI sözleşme sahibi → kendi zamanını GERİ SARAR
    ///
    /// ### İki faz (Enes kararı)
    /// ① ② havuz YARIYA düşünce döner · ③ ④ havuz BİTİNCE fullenip döner
    /// ("kazandım" sandığın an bitmediğini öğreniyorsun — ve zamanı derinden
    ///  manipüle edebilenler zaten bu ikisi.)
    ///
    /// ### ⚠️ İKİNCİ FAZIN HAVUZU KREDİDİR
    /// Fullenen havuz vurulunca erir ama **HASAT VERMEZ** — o zaman onun değildi,
    /// gösterdiği para sahte. Yoksa son iki boss oyunun en büyük zaman KAYNAĞI olurdu;
    /// oysa ölçüm boss ekonomisini zaten sorunlu bulmuştu. Sayı çevirerek değil
    /// **kurguyla** çözülüyor.
    /// </summary>
    public abstract class BossKind
    {
        public abstract string Name { get; }

        /// <summary>Ne yaptığı — HUD'da faz geçişinde gösteriliyor.</summary>
        public abstract string Crime { get; }

        public abstract Color Tint { get; }

        /// <summary>③④ true: havuz bitince FULLENİR. ①② false: yarıda döner.</summary>
        public virtual bool RefillsOnEmpty => false;

        /// <summary>Faz 2 eşiği (havuz oranı). Yalnızca fullenmeyen boss'larda geçerli.</summary>
        public virtual float PhaseTwoAt => 0.5f;

        /// <summary>Saniyede emdiği zaman. Faza göre değişebiliyor.</summary>
        public virtual float AuraDrain(Tuning t, int phase) => t.bossAuraDrain;

        /// <summary>Hız çarpanı — TAHSİLDAR faz 2'de kovalıyor.</summary>
        public virtual float SpeedMultiplier(int phase) => 1f;

        /// <summary>
        /// OYUNCUNUN hasadına etkisi — EKSPER'in suçu bu. Yaşadığı sürece işliyor.
        /// </summary>
        public virtual float PlayerHarvest(int phase) => 1f;

        /// <summary>Saniyede kendi havuzunu ne kadar geri sarıyor — MURABAHACI.</summary>
        public virtual float RegenPerSecond(Tuning t, int phase) => 0f;

        /// <summary>Faz 2'ye geçerken ikiye bölünüyor mu — KEFİL.</summary>
        public virtual bool SplitsOnPhaseTwo => false;

        /// <summary>
        /// Bu fazın saldırı dağarcığı. Sırayla dönülüyor (deterministik — "bu saldırı mı
        /// zor, ben mi kötü oynadım" cevaplanabilir kalsın).
        ///
        /// ⚠️ Faz 2 listesi faz 1'in AYNISI OLAMAZ: hem YENİ saldırı hem eskilerin
        /// GELİŞMİŞ hâli olmalı (Enes kararı). "Aynı saldırı ama daha çok hasar"
        /// faz değildir — oyuncunun öğrendiği cevabı geçersiz kılmayan şey yeni değildir.
        /// </summary>
        public abstract BossAttack[] Attacks(int phase);

        // Ortak renkler — saldırının TÜRÜ renginden okunsun.
        protected static readonly Color Cone = new Color(1f, 0.45f, 0.15f, 0.30f);
        protected static readonly Color Ring = new Color(1f, 0.30f, 0.10f, 0.28f);
        protected static readonly Color Donut = new Color(0.4f, 0.7f, 1f, 0.30f);
        protected static readonly Color Beam = new Color(1f, 0.85f, 0.25f, 0.32f);

        // ── varyantlar ───────────────────────────────────────────────────────

        sealed class Tahsildar : BossKind
        {
            public override string Name => "TAHSİLDAR";
            public override string Crime => "artık kaçamazsın";
            public override Color Tint => new Color(0.85f, 0.3f, 0.25f);

            // Faz 1: yanındayken emer → uzak dur, vur, çekil.
            // Faz 2: EMMEYİ BIRAKIR ama kovalar → uzak durmak işe yaramaz, bitirmen lazım.
            // Aynı tehdidin daha çoğu değil; BAŞKA bir soru.
            public override float AuraDrain(Tuning t, int phase)
                => phase >= 2 ? 0f : t.bossAuraDrain;
            public override float SpeedMultiplier(int phase) => phase >= 2 ? 1.55f : 1f;

            // Zorla alan adam: yaklaşır, kapatır, tekrar vurur.
            public override BossAttack[] Attacks(int phase) => phase >= 2
                ? new[]
                {
                    // GELİŞMİŞ: hamle ekledi — geri çekilmek artık kurtarmıyor
                    new BossAttack { Name = "HACİZ+", RangeMul = 1.3f, Angle = 130f,
                                     Lunge = 2.5f, CostMul = 1.15f, Tint = Cone },
                    // GELİŞMİŞ: halka büyüdü ve İKİ KEZ iniyor — bir kez kaçmak yetmiyor
                    new BossAttack { Name = "TAHSİL+", RangeMul = 1.35f, Angle = 360f,
                                     Repeat = 2, WindupMul = 0.8f, CostMul = 1.3f, Tint = Ring },
                    // YENİ: uzaktan üstüne atlıyor. Mesafe tutmak bir cevap olmaktan çıkıyor
                    new BossAttack { Name = "ZORLA ALIM", RangeMul = 2.2f, Angle = 70f,
                                     Lunge = 5f, WindupMul = 1.25f, CostMul = 1.4f, Tint = Cone },
                }
                : new[]
                {
                    new BossAttack { Name = "HACİZ", Angle = 0f, Tint = Cone },
                    new BossAttack { Name = "TAHSİL", RangeMul = 1f, Angle = 360f,
                                     WindupMul = 1.4f, CostMul = 1.6f, Tint = Ring },
                };
        }

        sealed class Eksper : BossKind
        {
            public override string Name => "EKSPER";
            public override string Crime => "zamanın artık hiçbir şey etmiyor";
            public override Color Tint => new Color(0.85f, 0.7f, 0.25f);

            // Yaşadığı sürece ne çalarsan çal az eder; faz 2'de HİÇ etmez.
            // "Acele et" değil, "onu bitirmeden zenginleşemezsin" diyor.
            public override float PlayerHarvest(int phase) => phase >= 2 ? 0f : 0.45f;
            public override float AuraDrain(Tuning t, int phase) => t.bossAuraDrain * 0.5f;

            // Önce ÖLÇER (ince ışın, hattından çık), sonra ölçmeyi bırakıp BİÇER.
            public override BossAttack[] Attacks(int phase) => phase >= 2
                ? new[]
                {
                    // GELİŞMİŞ: artık hassas ölçmüyor, geniş biçiyor
                    new BossAttack { Name = "DEĞER BİÇME+", RangeMul = 2.6f, Angle = 60f,
                                     WindupMul = 0.85f, CostMul = 1.2f, Tint = Beam },
                    // YENİ: SİMİT — halkanın TERSİ. Kaçmak değil YAKLAŞMAK gerekiyor;
                    // oyuncunun "uzaklaş" refleksi burada onu öldürüyor
                    new BossAttack { Name = "HACİM", RangeMul = 3.2f, Angle = 360f,
                                     InnerRadius = 2.6f, WindupMul = 1.3f, CostMul = 1.5f,
                                     Tint = Donut },
                    new BossAttack { Name = "KESİNTİ+", RangeMul = 1.25f, Angle = 120f,
                                     CostMul = 1.2f, Tint = Cone },
                }
                : new[]
                {
                    // İnce ve UZUN: hattında durmak cezalı, yana bir adım yeterli
                    new BossAttack { Name = "DEĞER BİÇME", RangeMul = 2.4f, Angle = 24f,
                                     WindupMul = 0.9f, CostMul = 1.3f, Tint = Beam },
                    new BossAttack { Name = "KESİNTİ", Angle = 0f, Tint = Cone },
                };
        }

        sealed class Kefil : BossKind
        {
            public override string Name => "KEFİL";
            public override string Crime => "borcu garantiye aldı";
            public override Color Tint => new Color(0.45f, 0.75f, 0.85f);

            public override bool RefillsOnEmpty => true;
            public override bool SplitsOnPhaseTwo => true;
            public override float AuraDrain(Tuning t, int phase) => t.bossAuraDrain * 0.6f;

            // Kefil seni KENDİNE ÇEKER: dibi güvenli, uzağı ölüm. Faz 2'de ikisi birden
            // yaptığında iki güvenli bölge arasında koşuşturmak zorunda kalıyorsun.
            public override BossAttack[] Attacks(int phase) => phase >= 2
                ? new[]
                {
                    // GELİŞMİŞ: simit büyüdü, güvenli göbek DARALDI
                    new BossAttack { Name = "TEMİNAT+", RangeMul = 3.6f, Angle = 360f,
                                     InnerRadius = 1.7f, WindupMul = 1.1f, CostMul = 1.35f,
                                     Tint = Donut },
                    // YENİ: düz halka. Simit "yaklaş" derken bu "uzaklaş" diyor —
                    // ikisi arka arkaya gelince ezber bozuluyor
                    new BossAttack { Name = "ORTAK BORÇ", RangeMul = 1.5f, Angle = 360f,
                                     WindupMul = 0.9f, CostMul = 1.25f, Tint = Ring },
                    new BossAttack { Name = "KEFALET+", RangeMul = 1.2f, Angle = 140f,
                                     Repeat = 2, Tint = Cone },
                }
                : new[]
                {
                    new BossAttack { Name = "TEMİNAT", RangeMul = 3f, Angle = 360f,
                                     InnerRadius = 2.4f, WindupMul = 1.2f, CostMul = 1.3f,
                                     Tint = Donut },
                    new BossAttack { Name = "KEFALET", Angle = 0f, Tint = Cone },
                };
        }

        sealed class Murabahaci : BossKind
        {
            public override string Name => "MURABAHACI";
            public override string Crime => "borç işlemeye devam ediyor";
            public override Color Tint => new Color(0.55f, 0.35f, 0.8f);

            public override bool RefillsOnEmpty => true;
            // Vurmadığın her saniye havuzu doluyor — ondan HIZLI olmak zorundasın.
            public override float RegenPerSecond(Tuning t, int phase)
                => phase >= 2 ? t.usurerRegenPerSecond : 0f;

            // Borç İŞLER: faz 2'de saldırıları da bileşik faiz gibi üst üste biniyor.
            public override BossAttack[] Attacks(int phase) => phase >= 2
                ? new[]
                {
                    // YENİ: ÜÇ kez arka arkaya, hazırlığı kısa. "Borç işlemeye devam ediyor" —
                    // bir kez kaçmak yetmiyor, üçünü birden okuman gerekiyor
                    new BossAttack { Name = "FAİZ", RangeMul = 1.15f, Angle = 150f,
                                     Repeat = 3, WindupMul = 0.6f, CostMul = 0.75f, Tint = Cone },
                    // GELİŞMİŞ: vade uzadı ve üstüne geliyor
                    new BossAttack { Name = "VADE+", RangeMul = 2.4f, Angle = 80f,
                                     Lunge = 4f, CostMul = 1.3f, Tint = Beam },
                    // GELİŞMİŞ: icra halkası büyüdü ve çok daha pahalı
                    new BossAttack { Name = "İCRA+", RangeMul = 1.6f, Angle = 360f,
                                     WindupMul = 1.15f, CostMul = 1.8f, Tint = Ring },
                }
                : new[]
                {
                    new BossAttack { Name = "VADE", RangeMul = 1.4f, Angle = 90f, Tint = Cone },
                    new BossAttack { Name = "İCRA", RangeMul = 1.15f, Angle = 360f,
                                     WindupMul = 1.35f, CostMul = 1.5f, Tint = Ring },
                };
        }

        public static readonly BossKind[] All =
            { new Tahsildar(), new Eksper(), new Kefil(), new Murabahaci() };

        /// <summary>Bölge → boss. Her bölgenin kendi alacaklısı.</summary>
        public static BossKind For(int regionIndex)
            => All[Mathf.Clamp(regionIndex, 0, All.Length - 1)];

        /// <summary>
        /// Sahnedeki YAŞAYAN boss'ların oyuncu hasadına toplam etkisi — EKSPER için.
        ///
        /// ⚠️ Statik bir bayrak tutulmuyor, her seferinde SAHNEYE soruluyor.
        /// Bayrak tutsaydık boss ölünce temizlenmeyi unutmak "hasadım niye hâlâ düşük"
        /// diye bir hata doğururdu — bu projede statik listelerin temizlenmemesi
        /// tam olarak öyle bir hata üretmişti (`RoomManager.TearDown`).
        /// </summary>
        public static float ActiveHarvestMultiplier()
        {
            float m = 1f;
            foreach (var e in Enemy.All)
                if (e != null && e.Boss != null && !e.Hollow)
                    m *= e.Boss.PlayerHarvest(e.Phase);
            return m;
        }
    }
}
