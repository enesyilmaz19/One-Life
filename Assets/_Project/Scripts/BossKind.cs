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

        /// <summary>
        /// &gt;0 ise hazırlık süresi MUTLAK saniye (çarpanı ezer).
        ///
        /// ⚠️ Boss hazırlıkları çok kısa (~0.2 sn). Oyuncunun YER DEĞİŞTİRMESİNİ
        /// isteyen bir saldırıya çarpan vermek yetmiyor — Enes'in dediği gibi
        /// "yeşil alana geçecek kadar süre kalmıyor". Konum değiştirten saldırılar
        /// süreyi doğrudan söylemeli, tahmin etmemeli.
        /// </summary>
        public float WindupSeconds = 0f;

        /// <summary>
        /// Oyuncu bundan YAKINSA bu saldırı seçilmiyor.
        ///
        /// ⚠️ Enes: "dash attack yaparken olduğum yerde durunca misliyor —
        /// uzaktayken dash attack yapsın, yakındayken normal vursun."
        /// Hamleli saldırı yakından yapılınca oyuncunun ÜSTÜNDEN aşıyor.
        /// </summary>
        public float MinPlayerRange = 0f;

        /// <summary>Oyuncu bundan UZAKSA bu saldırı seçilmiyor (0 = sınırsız).</summary>
        public float MaxPlayerRange = 0f;

        public float CostMul = 1f;

        /// <summary>Kaç kez arka arkaya vuruyor — bir kez kaçmak yetmesin.</summary>
        public int Repeat = 1;

        /// <summary>Vuruş anında oyuncuya doğru atılma mesafesi — mesafeyi kapatır.</summary>
        public float Lunge = 0f;

        /// <summary>
        /// &gt;0 ise halkanın ARKASINDA bu genişlikte GÜVENLİ BİR AÇIKLIK var.
        /// Yeni bir soru soruyor: "uzaklaş" değil, "ARKASINA GEÇ".
        ///
        /// ⚠️ Açıklık neden ARKA? Hazırlık boyunca boss oyuncuya dönüyor
        /// (`FacePlayer`). Açıklığı ÖNE koysaydık oyuncu daima içinde olurdu ve
        /// saldırı hiç isabet etmezdi — mekanik sessizce anlamsızlaşırdı.
        /// </summary>
        public float SafeAngle = 0f;

        /// <summary>
        /// Bu saldırıdan HEMEN SONRA gelen saldırı — arada bekleme yok.
        /// `Repeat` aynısını tekrarlar; bu FARKLI birini zincirler. Yeni soru:
        /// "bir cevaba yerleşemezsin" (halka → simit = çık, sonra hemen gir).
        /// </summary>
        public BossAttack Then;

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
        /// Kaç saniyede bir oyuncunun DURDUĞU YERE gecikmeli patlama bırakıyor
        /// (0 = bırakmıyor). SİMSAR'ın işi.
        ///
        /// ⚠️ Bu bir AÇIK KAPATIYOR: Enes "durduğum yerde vurarak zamanımı 1200'e
        /// çıkardım" dedi. Bütün boss'lar oyuncuya GİDİYOR, yani sabit durmak
        /// geçerli kalıyordu. SİMSAR sana gelmiyor, DURDUĞUN YERE geliyor.
        /// </summary>
        public virtual float GroundMarkInterval(int phase) => 0f;

        // ── ÇAĞIRMA (Faz 18) ──────────────────────────────────────────────────
        // Enes: *"bossların faz 2'sine koymadığın düşman çağırma özelliğini de ekle."*
        //
        // ⚠️ Çağırma neden YALNIZCA boss'a ait: sıradan odalarda düşmanlar DOĞMUYOR,
        // oda kurulurken yerleştiriliyor (RoomManager'ın kuralı). Doğurma böylece
        // bir kural değil BOSS'UN İMZASI oluyor — pusudan gelen düşmanın ucuzluğu
        // yalnızca boss dövüşünde, ve orada da bir tehdit olarak.
        //
        // ⚠️ Ve YALNIZCA FAZ 2+: faz 1'de de çağırsaydı dövüşün ilk yarısı zaten
        // kalabalık başlar, faz geçişi bir ARTIŞ olarak okunmazdı.

        /// <summary>Kaç saniyede bir yardım çağırıyor (0 = çağırmıyor).</summary>
        public virtual float SummonInterval(int phase) => 0f;

        /// <summary>Her çağrıda kaç düşman.</summary>
        public virtual int SummonCount(int phase) => 2;

        /// <summary>
        /// Çağırdığı tür — boss'un SUÇUYLA uyumlu olmalı.
        /// ⚠️ Rastgele düşman çağırmak "boss zorlaştı" olurdu; kendi türünü
        /// çağırmak "boss kendini çoğaltıyor" oluyor ve mekaniği anlatıyor.
        /// </summary>
        public virtual EnemyKind SummonKind => EnemyKind.Basic;

        /// <summary>
        /// Aynı anda en fazla kaç çağrılmış düşman yaşayabilir.
        /// ⚠️ TAVAN ŞART: tavansız çağırma, oyuncu yavaş oynadığında odayı
        /// doldurup dövüşü matematiksel olarak kazanılamaz yapardı. Boss'un
        /// baskısı ARTMALI, dövüş imkânsızlaşmamalı.
        /// </summary>
        public virtual int SummonMaxAlive => 4;

        // ── FAZ SAYISI (Faz 18) ───────────────────────────────────────────────

        /// <summary>Kaç fazı var. Son boss'ta 3 (Enes kararı).</summary>
        public virtual int PhaseCount => 2;

        /// <summary>
        /// Bu fazda ÖNCEKİ BOSS'LARI çağırıyor mu (0 = hiç).
        ///
        /// Enes: *"son bossa faz 3 ekleyelim; onda da ilk önce önceki bossların
        /// her birini çağırsın, hepsi ölünce kendisi gelsin."*
        ///
        /// ⚠️ Çağıranın kendisi o sırada UYKUDA: vurulmuyor, vurmuyor. Yoksa
        /// oyuncu beş boss'la aynı anda dövüşürdü ve bu bir final değil bir
        /// kaza olurdu. Uyku aynı zamanda ANLATI: adam öne geçmiyor, borcunu
        /// tahsil ettirmek için adamlarını gönderiyor.
        /// </summary>
        public virtual int SummonsRivalsInPhase => 0;

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

            // Faz 2'de ADAMLARINI çağırıyor — "zorla alan" adam yalnız gelmez.
            // SIZINTI çağırıyor: emmeyi BIRAKTIĞI faz bu, emme işini onlar sürdürüyor.
            public override float SummonInterval(int phase) => phase >= 2 ? 7f : 0f;
            public override EnemyKind SummonKind => EnemyKind.Leak;
            public override int SummonCount(int phase) => 2;

            // Zorla alan adam: yaklaşır, kapatır, tekrar vurur.
            public override BossAttack[] Attacks(int phase) => phase >= 2
                ? new[]
                {
                    // GELİŞMİŞ: hamle ekledi — geri çekilmek artık kurtarmıyor
                    new BossAttack { Name = "HACİZ+", RangeMul = 1.3f, Angle = 130f,
                                     Lunge = 2.5f, MinPlayerRange = 2.8f,
                                     CostMul = 1.15f, Tint = Cone },
                    // GELİŞMİŞ: halka büyüdü ve İKİ KEZ iniyor — bir kez kaçmak yetmiyor
                    new BossAttack { Name = "TAHSİL+", RangeMul = 1.35f, Angle = 360f,
                                     Repeat = 2, WindupMul = 0.8f, CostMul = 1.3f, Tint = Ring },
                    // YENİ: uzaktan üstüne atlıyor. ⚠️ YALNIZCA UZAKTAN — yakından
                    // yapılınca oyuncunun üstünden aşıp ıskalıyordu (Enes bildirdi).
                    new BossAttack { Name = "ZORLA ALIM", RangeMul = 2.2f, Angle = 70f,
                                     Lunge = 5f, WindupMul = 1.25f, CostMul = 1.4f,
                                     MinPlayerRange = 4.5f, Tint = Cone },
                    // Yakın mesafenin cevabı: hamlesiz, hızlı, iki kez
                    // GELİŞMİŞ menzilli: üç ışın peş peşe, bir kez yana kaçmak yetmiyor
                    new BossAttack { Name = "İHBARNAME+", RangeMul = 3f, Angle = 24f,
                                     Repeat = 3, MinPlayerRange = 3f, WindupMul = 0.7f,
                                     CostMul = 1.1f, Tint = Beam },
                    // GELİŞMİŞ simit: göbek daraldı, güvenli yer iğne deliği
                    new BossAttack { Name = "KELEPÇE+", RangeMul = 3.4f, Angle = 360f,
                                     InnerRadius = 1.5f, WindupSeconds = 1f,
                                     CostMul = 1.5f, Tint = Donut },
                    new BossAttack { Name = "EL KOYMA", RangeMul = 1.1f, Angle = 160f,
                                     Repeat = 2, WindupMul = 0.7f, CostMul = 0.9f,
                                     MaxPlayerRange = 4.5f, Tint = Cone },
                }
                : new[]
                {
                    // DÜZ: klasik yakın vuruş.
                    new BossAttack { Name = "HACİZ", Angle = 0f, Tint = Cone },

                    // MENZİLLİ: uzaktan dar ışın. ⚠️ EKSİKTİ — TAHSİLDAR faz 1'inde
                    // hiç uzak saldırı yoktu, yani "uzak dur" bedava bir cevaptı ve
                    // oyunun İLK boss'u kaçmayı ödüllendiriyordu. Yanlış ilk ders.
                    new BossAttack { Name = "İHBARNAME", RangeMul = 2.6f, Angle = 26f,
                                     MinPlayerRange = 3f, WindupMul = 0.85f,
                                     CostMul = 1f, Tint = Beam },

                    // ÖZGÜN: halka — uzaklaş.
                    new BossAttack { Name = "TAHSİL", RangeMul = 1f, Angle = 360f,
                                     WindupMul = 1.4f, CostMul = 1.6f, Tint = Ring },

                    // ÖZGÜN: simit — göbeği güvenli, yani YAKLAŞ.
                    // ⚠️ TAHSİLDAR yanındayken zaman EMİYOR: dibine girmek zaten
                    // pahalı. "Güvenli yer aynı zamanda en pahalı yer" — oyunun ilk
                    // boss'u bu gerilimi öğretiyor, sonrakiler üstüne kuruyor.
                    new BossAttack { Name = "KELEPÇE", RangeMul = 2.8f, Angle = 360f,
                                     InnerRadius = 2.2f, WindupSeconds = 1.1f,
                                     CostMul = 1.3f, Tint = Donut },
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

            // Faz 2'de SİGORTA çağırıyor: onlar da geliri kapatıyor, yani boss
            // öldükten sonra bile kesinti bir süre sürüyor. Suçun devri.
            public override float SummonInterval(int phase) => phase >= 2 ? 8f : 0f;
            public override EnemyKind SummonKind => EnemyKind.Insurer;
            public override int SummonCount(int phase) => 2;
            public override int SummonMaxAlive => 3;
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
                                     InnerRadius = 2.6f, WindupSeconds = 1.15f, CostMul = 1.5f,
                                     Tint = Donut },
                    new BossAttack { Name = "KESİNTİ+", RangeMul = 1.25f, Angle = 120f,
                                     Repeat = 2, CostMul = 1.2f, Tint = Cone },
                    // YENİ: üç ışın peş peşe, aralarında dönüyor — hat sürekli kayıyor
                    new BossAttack { Name = "REZERV", RangeMul = 2.8f, Angle = 30f,
                                     Repeat = 3, WindupMul = 0.55f, CostMul = 0.8f, Tint = Beam },
                    // YENİ: uzaktan üstüne, sonra hemen koni — yaklaşıp bastırıyor
                    new BossAttack { Name = "REVİZYON", RangeMul = 2.2f, Angle = 75f,
                                     Lunge = 4.5f, MinPlayerRange = 4.5f,
                                     WindupMul = 1.2f, CostMul = 1.25f, Tint = Cone,
                                     Then = new BossAttack { Name = "…ve İBRA", RangeMul = 1.2f,
                                                             Angle = 170f, WindupMul = 0.55f,
                                                             CostMul = 1f, Tint = Cone } },
                }
                : new[]
                {
                    // İnce ve UZUN: hattında durmak cezalı, yana bir adım yeterli
                    new BossAttack { Name = "DEĞER BİÇME", RangeMul = 2.4f, Angle = 24f,
                                     WindupMul = 0.9f, CostMul = 1.3f, Tint = Beam },
                    new BossAttack { Name = "KESİNTİ", Angle = 0f, Tint = Cone },
                    // YENİ: iki ışın arka arkaya — bir kez yana kaçmak yetmiyor
                    new BossAttack { Name = "ÇİFT EKSPERTİZ", RangeMul = 2.4f, Angle = 28f,
                                     Repeat = 2, WindupMul = 0.7f, CostMul = 1f, Tint = Beam },
                    // YENİ: yaklaşınca geniş süpürme — dibinde durmak da bedava değil
                    // ÖZGÜN: halka — uzaklaş. ⚠️ EKSPER faz 1'inin HİÇ özgün saldırısı
                    // yoktu (hepsi ışın ya da düz koni): ikinci boss'un ilk fazı
                    // oyuncuya hiçbir KAÇIŞ SORUSU sormuyordu. Dağarcık kapısı yakaladı.
                    new BossAttack { Name = "AMORTİSMAN", RangeMul = 1.3f, Angle = 360f,
                                     WindupMul = 1.3f, CostMul = 1.4f, Tint = Ring },

                    // ÖZGÜN: uzaktan üstüne kapanma — "uzak dur" cevabını bozuyor.
                    new BossAttack { Name = "YENİDEN DEĞERLEME", RangeMul = 2.3f, Angle = 75f,
                                     Lunge = 3.2f, MinPlayerRange = 4f,
                                     CostMul = 1.2f, Tint = Cone },

                    new BossAttack { Name = "DEĞER KAYBI", RangeMul = 1.15f, Angle = 200f,
                                     MaxPlayerRange = 3.5f, WindupMul = 0.8f,
                                     CostMul = 1.1f, Tint = Cone },
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

            // Faz 2'de KASA çağırıyor — kefilin işi TEMİNAT göstermek, kasa da
            // teminatın kendisi. Vurarak hiçbir şey alamadığın şeyler odaya doluyor:
            // "bunları öldürmeye değer mi" sorusu boss dövüşünün ortasında soruluyor.
            public override float SummonInterval(int phase) => phase >= 2 ? 9f : 0f;
            public override EnemyKind SummonKind => EnemyKind.Vault;
            public override int SummonCount(int phase) => 1;
            public override int SummonMaxAlive => 2;

            // Kefil seni KENDİNE ÇEKER: dibi güvenli, uzağı ölüm. Faz 2'de ikisi birden
            // yaptığında iki güvenli bölge arasında koşuşturmak zorunda kalıyorsun.
            public override BossAttack[] Attacks(int phase) => phase >= 2
                ? new[]
                {
                    // GELİŞMİŞ: simit büyüdü, güvenli göbek DARALDI
                    new BossAttack { Name = "TEMİNAT+", RangeMul = 3.6f, Angle = 360f,
                                     InnerRadius = 1.7f, WindupSeconds = 1.05f, CostMul = 1.35f,
                                     Tint = Donut },
                    // YENİ: düz halka. Simit "yaklaş" derken bu "uzaklaş" diyor —
                    // ikisi arka arkaya gelince ezber bozuluyor
                    new BossAttack { Name = "ORTAK BORÇ", RangeMul = 1.5f, Angle = 360f,
                                     WindupMul = 0.9f, CostMul = 1.25f, Tint = Ring },
                    // GELİŞMİŞ menzilli: ⚠️ KEFİL faz 2'de HİÇ uzak saldırı yoktu.
                    // İkiye bölündüğü faz bu: uzaklaşıp ikisini ayırmak bedava bir
                    // cevap olurdu. Üç ışın peş peşe, o cevabı kapatıyor.
                    new BossAttack { Name = "SENET+", RangeMul = 3f, Angle = 24f,
                                     Repeat = 3, WindupMul = 0.7f, CostMul = 1.1f, Tint = Beam },

                    new BossAttack { Name = "KEFALET+", RangeMul = 1.2f, Angle = 140f,
                                     Repeat = 2, Tint = Cone },
                    // YENİ: simit → HEMEN halka. Önce içeri çağırıyor, sonra dışarı
                    // atıyor. Zincirin en zalim hâli, ve KEFİL'in tam işi bu
                    new BossAttack { Name = "İPOTEK", RangeMul = 3.2f, Angle = 360f,
                                     InnerRadius = 2.2f, WindupSeconds = 1.1f, CostMul = 1.2f,
                                     Tint = Donut,
                                     Then = new BossAttack { Name = "…ve TASFİYE", RangeMul = 1.6f,
                                                             Angle = 360f, WindupSeconds = 0.7f,
                                                             CostMul = 1.3f, Tint = Ring } },
                }
                : new[]
                {
                    // ⚠️ Simit hazırlığı MUTLAK: göbeğe koşacak süre lazım
                    new BossAttack { Name = "TEMİNAT", RangeMul = 3f, Angle = 360f,
                                     InnerRadius = 2.4f, WindupSeconds = 1.2f, CostMul = 1.3f,
                                     Tint = Donut },
                    new BossAttack { Name = "KEFALET", Angle = 0f, Tint = Cone },
                    // YENİ: uzaktan kapatıyor — göbeğe sığınma alışkanlığını cezalandırıyor
                    new BossAttack { Name = "REHİN", RangeMul = 2.4f, Angle = 90f,
                                     Lunge = 4f, MinPlayerRange = 4f,
                                     WindupMul = 1.2f, CostMul = 1.2f, Tint = Cone },
                    // YENİ: dar ışın — simit ve koni dışında üçüncü bir şekil
                    new BossAttack { Name = "SENET", RangeMul = 2.6f, Angle = 26f,
                                     Repeat = 2, WindupMul = 0.75f, CostMul = 1f, Tint = Beam },

                    // ÖZGÜN: arkasına geç. ⚠️ Üçüncü boss taban 3 özgün istiyor
                    // (Enes) ve ikisi vardı. Kefil seni hep KENDİNE çekiyordu;
                    // bu, ilk kez ondan UZAKLAŞMANI değil ARKASINA geçmeni istiyor.
                    new BossAttack { Name = "ÇEKİNCE", RangeMul = 2.4f, Angle = 360f,
                                     SafeAngle = 100f, WindupSeconds = 1.5f,
                                     CostMul = 1.3f, Tint = Ring },
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

            // ── FAZ 3 (Enes kararı) ───────────────────────────────────────────
            // "Son bossa faz 3 ekleyelim; onda da ilk önce önceki bossların her
            //  birini çağırsın, hepsi ölünce kendisi gelsin."
            //
            // ⚠️ Bu, karakterin kendisi: MURABAHACI hiç kendi eliyle tahsil etmez.
            // Faz 1'de senedini gösterir, faz 2'de faiz işletir, faz 3'te
            // ADAMLARINI YOLLAR. Öne ancak onlar bittiğinde çıkıyor — ve o an
            // oyunun bütün alacaklıları üzerinde ölmüş oluyor.
            public override int PhaseCount => 3;
            public override int SummonsRivalsInPhase => 3;

            // Faz 2'de sıradan yardım: ZAMANAŞIMI. Borcun kendisi gibi, yaşadıkça
            // büyüyorlar — "acele et" baskısını mekanikle söylüyor.
            public override float SummonInterval(int phase) => phase == 2 ? 7.5f : 0f;
            public override EnemyKind SummonKind => EnemyKind.Lapse;
            public override int SummonCount(int phase) => 2;
            public override int SummonMaxAlive => 4;

            // Faz 3'te rejenerasyon SÜRÜYOR ama uykudayken işlemiyor
            // (`TickBossPassives` uykuyu pasiflerden ÖNCE kesiyor) — yani
            // rakiplerle dövüşürken arkada sessizce dolan bir boss yok.

            // Son boss: en geniş dağarcık. Borç İŞLER — taksitler peş peşe gelir,
            // bir cevaba yerleşmene izin vermez.
            public override BossAttack[] Attacks(int phase) => phase >= 3
                ? new[]
                {
                    // ── FAZ 3: her şeyin gelişmişi + TÜM HARİTA ──
                    // ⚠️ TÜM HARİTAYA vuran saldırı, minik güvenli noktayla (Enes).
                    //    Simit: göbeği güvenli. Yarıçap arenayı aşıyor, yani
                    //    KAÇACAK YER YOK — tek cevap boss'un DİBİNE girmek.
                    //    Hazırlık MUTLAK ve uzun: arenanın öbür ucundan koşman gerek.
                    new BossAttack { Name = "TAM TASFİYE", RangeMul = 9f, Angle = 360f,
                                     InnerRadius = 2.6f, WindupSeconds = 2.2f,
                                     CostMul = 2.2f, Tint = Ring },

                    // GELİŞMİŞ: zincir üçe çıktı — halka, simit, sonra açıklık.
                    // Üç ayrı cevabı SIRAYLA vermen gerekiyor, tek bir yere
                    // yerleşmek imkânsız.
                    new BossAttack { Name = "BİLEŞİK FAİZ++", RangeMul = 2.4f, Angle = 360f,
                                     WindupSeconds = 0.9f, CostMul = 1.4f, Tint = Ring,
                                     Then = new BossAttack { Name = "…SİMİT", RangeMul = 3.2f,
                                         Angle = 360f, InnerRadius = 2.2f, WindupSeconds = 0.7f,
                                         CostMul = 1.3f, Tint = Donut,
                                         Then = new BossAttack { Name = "…AÇIKLIK", RangeMul = 2.8f,
                                             Angle = 360f, SafeAngle = 80f, WindupSeconds = 1.2f,
                                             CostMul = 1.3f, Tint = Ring } } },

                    // YENİ: dört ışın peş peşe — SİMSAR'ın işini kendi yapıyor
                    new BossAttack { Name = "SON İHTAR", RangeMul = 3.4f, Angle = 22f,
                                     Repeat = 4, WindupMul = 0.55f, CostMul = 1.1f, Tint = Beam },

                    // GELİŞMİŞ: uzaktan kapatıp süpürüyor, sonra halka
                    new BossAttack { Name = "İCRA++", RangeMul = 2.6f, Angle = 110f,
                                     Lunge = 5.5f, MinPlayerRange = 4f, CostMul = 1.5f,
                                     Tint = Cone,
                                     Then = new BossAttack { Name = "…KAPANIŞ", RangeMul = 1.5f,
                                         Angle = 360f, WindupMul = 0.8f, CostMul = 1.4f,
                                         Tint = Ring } },

                    // YENİ: yakın düz vuruş — her boss'un "klasik" atağı da gelişmeli
                    new BossAttack { Name = "VADE++", RangeMul = 1.5f, Angle = 140f,
                                     Lunge = 2.2f, CostMul = 1.3f, Tint = Cone },
                }
                : phase >= 2
                ? new[]
                {
                    // YENİ: ÜÇ kez arka arkaya, hazırlığı kısa. Bir kez kaçmak yetmiyor
                    new BossAttack { Name = "FAİZ", RangeMul = 1.15f, Angle = 150f,
                                     Repeat = 3, WindupMul = 0.6f, CostMul = 0.75f, Tint = Cone },

                    // YENİ ⚠️ ZİNCİR: halka → HEMEN simit. Çıkıyorsun, sonra hemen
                    // girmen gerekiyor. Tek bir cevaba yerleşemiyorsun —
                    // "bileşik faiz": her taksit bir öncekinin üstüne biniyor.
                    new BossAttack { Name = "BİLEŞİK FAİZ", RangeMul = 1.7f, Angle = 360f,
                                     WindupMul = 1.1f, CostMul = 1.3f, Tint = Ring,
                                     // ⚠️ Zincirin ikinci halkası da KONUM DEĞİŞTİRTİYOR:
                                     // halkadan çıktın, şimdi göbeğe koşman gerek.
                                     // Kısa tutulursa "haksızlık" olur (Enes bildirdi).
                                     Then = new BossAttack { Name = "…ve TAKSİT", RangeMul = 3.4f,
                                                             Angle = 360f, InnerRadius = 2.4f,
                                                             WindupSeconds = 0.95f, CostMul = 1.4f,
                                                             Tint = Donut } },

                    // GELİŞMİŞ: açıklık DARALDI ve arkasından bir de koni geliyor —
                    // sığındığın yere vuruyor
                    new BossAttack { Name = "TEMERRÜT+", RangeMul = 2.4f, Angle = 360f,
                                     SafeAngle = 60f, WindupSeconds = 1.25f, CostMul = 1.5f, Tint = Ring,
                                     Then = new BossAttack { Name = "…ve HACİZ", RangeMul = 1.6f,
                                                             Angle = 120f, WindupMul = 0.6f,
                                                             CostMul = 1.2f, Tint = Cone } },

                    new BossAttack { Name = "VADE+", RangeMul = 2.4f, Angle = 80f,
                                     Lunge = 4f, MinPlayerRange = 4f,
                                     CostMul = 1.3f, Tint = Beam },
                    // ⚠️ Diğer boss'ların dağarcığını genişletince SON BOSS artık en
                    // geniş değildi ve mevcut kapı bunu yakaladı. Bu bir sayı sorunu
                    // değil, bir SIRALAMA kuralı: finalin en zengin dağarcık olması
                    // gerekiyor, yoksa zincirin tırmanma hissi tepede düşüyor.
                    // İki saldırı EKLENDİ, diğerleri kısılmadı.

                    // YENİ menzilli gelişmiş: dört ışın, yandan kaçmak yetmiyor
                    new BossAttack { Name = "İBRAZ+", RangeMul = 3.2f, Angle = 24f,
                                     Repeat = 4, MinPlayerRange = 3.5f, WindupMul = 0.7f,
                                     CostMul = 1.2f, Tint = Beam },

                    // YENİ simit gelişmiş: göbek daraldı ve arkasından halka geliyor
                    new BossAttack { Name = "KREDİ+", RangeMul = 3.6f, Angle = 360f,
                                     InnerRadius = 1.7f, WindupSeconds = 1.1f,
                                     CostMul = 1.4f, Tint = Donut,
                                     Then = new BossAttack { Name = "…ve FAİZ", RangeMul = 2f,
                                         Angle = 360f, WindupSeconds = 0.9f, CostMul = 1.3f,
                                         Tint = Ring } },

                    new BossAttack { Name = "İCRA+", RangeMul = 1.6f, Angle = 360f,
                                     WindupMul = 1.15f, CostMul = 1.8f, Tint = Ring },
                }
                : new[]
                {
                    // DÜZ
                    new BossAttack { Name = "VADE", RangeMul = 1.4f, Angle = 90f, Tint = Cone },

                    // MENZİLLİ: ⚠️ EKSİKTİ. Son boss'un ilk fazında uzak saldırı
                    // olmaması oyuncuya "uzaktan yıprat" diye YANLIŞ bir cevap
                    // öğretiyordu — sonra faz 2 ve 3 o cevabı yıkıyordu. Kural
                    // baştan kurulmalı: bu adamdan uzak durarak kaçamıyorsun.
                    new BossAttack { Name = "İBRAZ", RangeMul = 2.8f, Angle = 28f,
                                     Repeat = 2, MinPlayerRange = 3.5f, WindupMul = 0.8f,
                                     CostMul = 1.1f, Tint = Beam },

                    // ÖZGÜN: halka
                    new BossAttack { Name = "İCRA", RangeMul = 1.15f, Angle = 360f,
                                     WindupMul = 1.35f, CostMul = 1.5f, Tint = Ring },

                    // ÖZGÜN: simit — üç kaçış sorusunun (uzaklaş / yaklaş / arkasına
                    // geç) hepsi faz 1'de tanıtılıyor, faz 2 ve 3 onları BİRLEŞTİRİYOR.
                    new BossAttack { Name = "KREDİ", RangeMul = 3f, Angle = 360f,
                                     InnerRadius = 2.4f, WindupSeconds = 1.2f,
                                     CostMul = 1.3f, Tint = Donut },

                    // YENİ: halkanın ARKASINDA güvenli açıklık. "Uzaklaş" değil
                    // "ARKASINA GEÇ" — mekaniği faz 2 silahlaştırmadan ÖNCE öğretiyor.
                    // ⚠️ Hazırlık MUTLAK ve UZUN: oyuncunun arkasına KOŞMASI gerekiyor.
                    // Çarpanla verilince "yeşil alana geçecek süre kalmıyordu" (Enes).
                    new BossAttack { Name = "TEMERRÜT", RangeMul = 2.2f, Angle = 360f,
                                     SafeAngle = 95f, WindupSeconds = 1.6f, CostMul = 1.4f,
                                     Tint = Ring },
                };
        }

        sealed class Simsar : BossKind
        {
            public override string Name => "SİMSAR";
            public override string Crime => "durduğun yeri satıyor";
            public override Color Tint => new Color(0.8f, 0.45f, 0.9f);

            // ⚠️ Diğerlerinden farkı: SANA GELMİYOR, durduğun YERE geliyor.
            // Tek cevabı hareket etmek — "dur ve vur" burada işlemiyor.
            public override float GroundMarkInterval(int phase) => phase >= 2 ? 1.1f : 2.0f;
            public override float AuraDrain(Tuning t, int phase) => t.bossAuraDrain * 0.4f;
            public override float SpeedMultiplier(int phase) => phase >= 2 ? 1.2f : 0.85f;

            // Faz 2'de MÜBAŞİR çağırıyor: onlar da uzaktan yere vuruyor, yani
            // "durduğun yer satılık" tehdidi üçe katlanıyor. Aracı adamın işi
            // zaten kendi vurmak değil, VURDURMAK.
            public override float SummonInterval(int phase) => phase >= 2 ? 8.5f : 0f;
            public override EnemyKind SummonKind => EnemyKind.Bailiff;
            public override int SummonCount(int phase) => 2;
            public override int SummonMaxAlive => 3;

            // Kendisi yavaş ve uzaktan iş görüyor; asıl baskı yerdeki işaretlerden.
            public override BossAttack[] Attacks(int phase) => phase >= 2
                ? new[]
                {
                    // GELİŞMİŞ düz: ⚠️ SİMSAR faz 2'de hiç yakın düz vuruş yoktu.
                    // Hepsi uzak ya da özgündü, yani dibine girmek bedavaydı —
                    // oysa faz 2'de hızlanıyor. Her boss'un bir klasiği olmalı.
                    new BossAttack { Name = "PAZARLIK+", RangeMul = 1.3f, Angle = 165f,
                                     MaxPlayerRange = 4.5f, WindupMul = 0.75f,
                                     CostMul = 1.2f, Tint = Cone },

                    new BossAttack { Name = "AÇIK ARTIRMA+", RangeMul = 3f, Angle = 34f,
                                     Repeat = 3, WindupMul = 0.6f, CostMul = 0.9f, Tint = Beam },
                    // GELİŞMİŞ: uzaktan üstüne kapanıyor, sonra süpürüyor
                    new BossAttack { Name = "DEVİR+", RangeMul = 2.4f, Angle = 85f,
                                     Lunge = 4.5f, MinPlayerRange = 4.5f, CostMul = 1.3f,
                                     Tint = Cone,
                                     Then = new BossAttack { Name = "…ve TESLİM", RangeMul = 1.3f,
                                                             Angle = 190f, WindupMul = 0.55f,
                                                             CostMul = 1.1f, Tint = Cone } },
                    // GELİŞMİŞ: açıklık daraldı ve arkasından simit geliyor —
                    // "arkasına geç" cevabını verir vermez "içine gir" gerekiyor.
                    new BossAttack { Name = "KOMİSYON+", RangeMul = 3f, Angle = 360f,
                                     SafeAngle = 62f, WindupSeconds = 1.3f, CostMul = 1.4f,
                                     Tint = Ring,
                                     // ⚠️ 0.8 sn yazmıştım ve kapı düştü: konum
                                     // değiştirten saldırılar ≥0.9 sn MUTLAK hazırlık
                                     // vermek zorunda (Enes'in "yeşil alana geçecek
                                     // süre kalmıyor" şikâyetinden doğan kural).
                                     // Zincirin İKİNCİ halkası olması istisna değil —
                                     // aksine, oyuncu ilk halkadan yeni çıkmış oluyor.
                                     Then = new BossAttack { Name = "…ve EKSPERTİZ", RangeMul = 3.2f,
                                         Angle = 360f, InnerRadius = 1.9f, WindupSeconds = 1f,
                                         CostMul = 1.3f, Tint = Donut } },

                    // YENİ: iki kez inen yakın halka — kaçış yönü bırakmıyor
                    new BossAttack { Name = "HARAÇ", RangeMul = 1.6f, Angle = 360f,
                                     Repeat = 2, WindupMul = 1.1f, CostMul = 1.3f, Tint = Ring },

                    new BossAttack { Name = "MÜZAYEDE", RangeMul = 1.9f, Angle = 360f,
                                     WindupSeconds = 1.0f, CostMul = 1.4f, Tint = Ring },
                }
                : new[]
                {
                    new BossAttack { Name = "AÇIK ARTIRMA", RangeMul = 2.8f, Angle = 30f,
                                     Repeat = 2, WindupMul = 0.8f, CostMul = 1f, Tint = Beam },
                    new BossAttack { Name = "DEVİR", RangeMul = 2.2f, Angle = 80f,
                                     Lunge = 3.5f, MinPlayerRange = 4f, CostMul = 1.2f, Tint = Cone },
                    new BossAttack { Name = "PAZARLIK", RangeMul = 1.15f, Angle = 150f,
                                     MaxPlayerRange = 4f, WindupMul = 0.85f,
                                     CostMul = 1f, Tint = Cone },

                    // ÖZGÜN: arkasına geç. Aracının işi seni YANLIŞ TARAFTA tutmak.
                    new BossAttack { Name = "KOMİSYON", RangeMul = 2.6f, Angle = 360f,
                                     SafeAngle = 90f, WindupSeconds = 1.5f,
                                     CostMul = 1.3f, Tint = Ring },

                    // ÖZGÜN: simit. ⚠️ SİMSAR yere işaret bırakıyor; dibine girmek
                    // "güvenli" ama işaretler tam orada birikiyor. İki mekanik
                    // birbirine sorun çıkarıyor — dördüncü boss'un hak ettiği yoğunluk.
                    new BossAttack { Name = "EKSPERTİZ", RangeMul = 3f, Angle = 360f,
                                     InnerRadius = 2.3f, WindupSeconds = 1.1f,
                                     CostMul = 1.2f, Tint = Donut },
                };
        }

        // ⚠️ SIRA = BÖLGE SIRASI. Beş bölge var, beş boss olmalı — dördüncü ve beşinci
        // bölge aynı boss'u alıyordu (`For` son elemana kırpıyordu). Enes bildirdi.
        // MURABAHACI SONDA kalmalı: sözleşmeyi elinde tutan, koşunun finali.
        public static readonly BossKind[] All =
            { new Tahsildar(), new Eksper(), new Kefil(), new Simsar(), new Murabahaci() };

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
