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
        /// &gt;0 ise güvenli yer boss'un DİBİ değil, HARİTADA RASTGELE bir nokta.
        ///
        /// Enes: *"ultiden kaçınmak için bossun dibine koşmak yerine haritada
        /// rastgele belirlenen bir yer güvenli bölge olsun."*
        ///
        /// ⚠️ Neden daha iyi: simidin göbeği HEP boss'un dibi, yani cevabı bir kez
        /// öğrenince refleks oluyor. Rastgele nokta her seferinde AYNI SORUYU
        /// yeniden sordurur: "neredeyim, oraya yetişir miyim". Ve saldırıyı adil
        /// tutan şey rastgelelik değil TELGRAF — nokta hazırlık boyunca yeşil
        /// halkayla apaçık gösteriliyor.
        ///
        /// ⚠️ Hazırlık MUTLAK ve UZUN olmak zorunda: haritanın öbür ucuna KOŞMAK
        /// gerekebiliyor. Çarpanla verilirse Enes'in Faz 12'de bildirdiği
        /// "yeşil alana geçecek süre kalmıyor" hatası aynen geri gelir.
        /// </summary>
        public float SafeSpotRadius = 0f;

        /// <summary>
        /// ÖN ve ARKA aynı anda vuruyor; YANLAR güvenli. (EKSPER'in imzası.)
        ///
        /// Koniden farkı geri çekilmeyi KAPATMASI: sıradan koninin iki cevabı
        /// var (yana kaç ya da geri çekil), aynanın tek cevabı var. "Değer
        /// biçen adam iki uçtan ölçer."
        /// </summary>
        public bool Mirrored;

        /// <summary>
        /// &gt;0 ise vuruştan sonra oyuncuyu KENDİNE çeker. (KEFİL'in imzası.)
        ///
        /// ⚠️ Bedeli düşük tutulmalı: tehdit hasar değil KONUM. Kefilin dibi
        /// zaten emiyor ve yakın vuruşları oradan çıkıyor — çekim seni o
        /// tezgâhın ortasına bırakıyor.
        /// </summary>
        public float Pull;

        /// <summary>
        /// &gt;0 ise boss vuruş anında GERİYE sıçrar. (SİMSAR'ın imzası.)
        ///
        /// Hamlenin tersi soruyu sorar: "kaç" değil, "KOVALA". Aracı adam
        /// malı devredip ortadan kayboluyor — ve SİMSAR'ın yer işaretleri
        /// tam da kovalarken üstüne bastığın şeyler.
        /// </summary>
        public float Recoil;

        /// <summary>
        /// Bu saldırıdan HEMEN SONRA gelen saldırı — arada bekleme yok.
        /// `Repeat` aynısını tekrarlar; bu FARKLI birini zincirler. Yeni soru:
        /// "bir cevaba yerleşemezsin" (halka → simit = çık, sonra hemen gir).
        /// </summary>
        public BossAttack Then;

        public Color Tint = new Color(1f, 0.45f, 0.15f, 0.30f);

        /// <summary>Menzilli sayılma eşiği — boss'un temel menziline çarpan.</summary>
        public const float UzakEşiği = 1.7f;

        /// <summary>
        /// UZAK banda ait mi: erişimi uzun ya da mesafeyi kendi kapatıyor.
        /// Boss uzaktayken bunlar seçiliyor.
        /// </summary>
        public bool Uzak => RangeMul > UzakEşiği || Lunge > 0f;

        /// <summary>
        /// HER İKİ banda da uyuyor mu. 360°'lik alan saldırıları ve haritadaki
        /// güvenli noktalı saldırılar yönsüzdür — erişimleri içindeki HER
        /// mesafede aynı soruyu sorarlar, dolayısıyla "yakın" ya da "uzak"
        /// diye ayrılmaları anlamsız.
        ///
        /// ⚠️ Ayrılsalardı somut bir hata çıkardı: son boss'un TÜM HARİTA
        /// saldırısı "menzilli" sayılır ve oyuncu dibine yapıştığında HİÇ
        /// yapılmazdı — yani finalin imza hamlesi sarılarak iptal edilirdi.
        /// </summary>
        public bool HerBant => Angle >= 360f || SafeSpotRadius > 0f;
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

        /// <summary>
        /// `All` içindeki yeri = NEMESİS DEFTERİNDEKİ indis.
        /// `EnemyKind` / `RevivalKind` ile aynı desen — indisi elle yazmak,
        /// listeye bir boss eklendiğinde sessizce YANLIŞ boss'un hesabına yazardı.
        /// </summary>
        public int Index { get; private set; }

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
        //
        // ⚠️⚠️ ÖZGÜN ŞEKİL, TEK SAHİPLİ. Enes: *"özgün diye her bossta aynı
        // 'özgün' saldırıları koymuşsun neredeyse, amk her bossta farklı olacak."*
        // Haklıydı: beş boss'un beşi de halka + simit + açıklık + hamle
        // kullanıyordu, "özgün" sıfatı yalnızca ADIN üstündeydi.
        //
        // Kural artık şu — ve `Faz12SelfCheck` onu ölçüyor:
        //
        //   ① TAHSİLDAR  HALKA (uzaklaş)      + HAMLE (üstüne gelir)
        //   ② EKSPER     SAĞANAK (art arda)   + AYNA (ön+arka, yanlar güvenli)
        //   ③ KEFİL      SİMİT (yaklaş)       + ÇEKİM (seni kendine alır)
        //   ④ SİMSAR     AÇIKLIK (arkasına geç) + GERİ SIÇRAMA (kovala)
        //   ⑤ MURABAHACI ZİNCİR (yerleşemezsin) + RASTGELE NOKTA (oraya yetiş)
        //
        // PAYLAŞILAN yalnızca ikisi, çünkü Enes ikisini herkese şart koştu:
        // "genel bir düz saldırı ve menzilli saldırı" → KONİ ve IŞIN.
        //
        // ⚠️ Tek istisna MURABAHACI'nın FAZ 3'ü: orada hepsini kullanıyor —
        // "son boss önceki tüm bossların özelliğini de kullanabilsin" (Enes).
        // İstisna faz 3'e kilitli: faz 1-2'de de kullansaydı final bir TOPLAM
        // değil bir TEKRAR olurdu, ve faz 3'ün anlamı kalmazdı.

        sealed class Tahsildar : BossKind
        {
            public override string Name => "TAHSİLDAR";
            public override string Crime => "artık kaçamazsın";
            public override Color Tint => new Color(0.85f, 0.3f, 0.25f);

            // ⚠️ İLK BOSS EN ZAYIF OLMALI. Enes: *"aga ilk boss 2.sinden daha
            // güçlü durmuş."* Haklıydı ve sebebi ortadaydı: TAHSİLDAR TAM aura
            // emiyordu (1.0×) — beşinin en yükseği — ve bedel çarpanları da
            // en ağırıydı. Emme ve bedel artık boss sırasıyla TIRMANIYOR;
            // `Faz12SelfCheck` bunu bir kural olarak tutuyor, göz kararı değil.
            public override float AuraDrain(Tuning t, int phase)
                => phase >= 2 ? 0f : t.bossAuraDrain * 0.55f;

            // Faz 1: yanındayken emer → uzak dur, vur, çekil.
            // Faz 2: EMMEYİ BIRAKIR ama kovalar → uzak durmak işe yaramaz.
            // Aynı tehdidin daha çoğu değil; BAŞKA bir soru.
            public override float SpeedMultiplier(int phase) => phase >= 2 ? 1.45f : 1f;

            // Faz 2'de ADAMLARINI çağırıyor — "zorla alan" adam yalnız gelmez.
            // SIZINTI: emmeyi BIRAKTIĞI faz bu, emme işini onlar sürdürüyor.
            public override float SummonInterval(int phase) => phase >= 2 ? 7f : 0f;
            public override EnemyKind SummonKind => EnemyKind.Leak;
            public override int SummonCount(int phase) => 2;

            public override BossAttack[] Attacks(int phase) => phase >= 2
                ? new[]
                {
                    // DÜZ (gelişmiş): artık küçük bir hamlesi de var
                    new BossAttack { Name = "HACİZ+", RangeMul = 1.3f, Angle = 130f,
                                     Lunge = 2.5f, MinPlayerRange = 2.8f,
                                     CostMul = 1.2f, Tint = Cone },

                    // MENZİLLİ (gelişmiş): iki ışın peş peşe
                    new BossAttack { Name = "İHBARNAME+", RangeMul = 3f, Angle = 24f,
                                     Repeat = 2, MinPlayerRange = 3f, WindupMul = 0.7f,
                                     CostMul = 1.05f, Tint = Beam },

                    // ÖZGÜN ① HALKA (gelişmiş): büyüdü ve İKİ KEZ iniyor
                    new BossAttack { Name = "TAHSİL+", RangeMul = 1.4f, Angle = 360f,
                                     Repeat = 2, WindupMul = 0.8f, CostMul = 1.25f, Tint = Ring },

                    // ÖZGÜN ② HAMLE (gelişmiş): daha uzaktan, daha sert
                    new BossAttack { Name = "ZORLA ALIM+", RangeMul = 2.4f, Angle = 70f,
                                     Lunge = 5.5f, MinPlayerRange = 4.5f, WindupMul = 1.15f,
                                     CostMul = 1.3f, Tint = Cone },

                    // YENİ: hamle + halka birlikte — atlıyor ve İNDİĞİ YERDE patlıyor.
                    // İki imzasını tek harekette birleştiriyor; faz 2'nin kazandığı şey
                    // yeni bir sayı değil, iki cevabı AYNI ANDA vermek zorunda kalman.
                    new BossAttack { Name = "KELEPÇE", RangeMul = 1.6f, Angle = 360f,
                                     Lunge = 6f, MinPlayerRange = 5f, WindupMul = 1.3f,
                                     CostMul = 1.35f, Tint = Ring },

                    // YENİ: dibindeyken hızlı çifte vuruş — yaklaşmak da bedava değil
                    new BossAttack { Name = "EL KOYMA", RangeMul = 1.1f, Angle = 165f,
                                     Repeat = 2, WindupMul = 0.7f, CostMul = 0.95f,
                                     MaxPlayerRange = 4.5f, Tint = Cone },
                }
                : new[]
                {
                    // DÜZ · TAHSİLDAR: GENİŞ ve ağır. Zorla alan adam kavrar,
                    // yana kaçmayı zorlaştırır.
                    new BossAttack { Name = "HACİZ", RangeMul = 1.2f, Angle = 135f,
                                     WindupMul = 1.15f, CostMul = 1.05f, Tint = Cone },

                    // MENZİLLİ: uzaktan dar ışın — "uzak dur" bedava bir cevap olmasın.
                    new BossAttack { Name = "İHBARNAME", RangeMul = 2.6f, Angle = 26f,
                                     MinPlayerRange = 3f, WindupMul = 0.85f,
                                     CostMul = 0.95f, Tint = Beam },

                    // ÖZGÜN ① HALKA — "uzaklaş". Oyunun ilk kaçış sorusu, ve en
                    // okunaklısı: ilk boss bunu öğretiyor, sonrakiler üstüne kuruyor.
                    new BossAttack { Name = "TAHSİL", RangeMul = 1.1f, Angle = 360f,
                                     WindupMul = 1.4f, CostMul = 1.15f, Tint = Ring },

                    // ÖZGÜN ② HAMLE — "geri çekilmek kurtarmaz, YANA kaç".
                    // ⚠️ YALNIZCA UZAKTAN: yakından yapılınca oyuncunun üstünden
                    // aşıp ıskalıyordu (Enes bildirdi).
                    new BossAttack { Name = "ZORLA ALIM", RangeMul = 2.2f, Angle = 70f,
                                     Lunge = 4.5f, MinPlayerRange = 4.5f, WindupMul = 1.25f,
                                     CostMul = 1.1f, Tint = Cone },
                };
        }

        sealed class Eksper : BossKind
        {
            public override string Name => "EKSPER";
            public override string Crime => "zamanın artık hiçbir şey etmiyor";
            public override Color Tint => new Color(0.85f, 0.7f, 0.25f);

            // Yaşadığı sürece ne çalarsan çal az eder; faz 2'de HİÇ etmez.
            public override float PlayerHarvest(int phase) => phase >= 2 ? 0f : 0.45f;
            public override float AuraDrain(Tuning t, int phase) => t.bossAuraDrain * 0.75f;

            // Faz 2'de SİGORTA çağırıyor: onlar da geliri kapatıyor.
            public override float SummonInterval(int phase) => phase >= 2 ? 8f : 0f;
            public override EnemyKind SummonKind => EnemyKind.Insurer;
            public override int SummonCount(int phase) => 2;
            public override int SummonMaxAlive => 3;

            public override BossAttack[] Attacks(int phase) => phase >= 2
                ? new[]
                {
                    // MENZİLLİ (gelişmiş)
                    new BossAttack { Name = "DEĞER BİÇME+", RangeMul = 2.8f, Angle = 22f,
                                     WindupMul = 0.8f, CostMul = 1.25f, Tint = Beam },

                    // DÜZ (gelişmiş)
                    new BossAttack { Name = "KESİNTİ+", RangeMul = 1.25f, Angle = 120f,
                                     Repeat = 2, CostMul = 1.2f, Tint = Cone },

                    // ÖZGÜN ① SAĞANAK (gelişmiş): dört ölçüm peş peşe
                    new BossAttack { Name = "REZERV", RangeMul = 2.8f, Angle = 26f,
                                     Repeat = 4, WindupMul = 0.55f, CostMul = 0.95f, Tint = Beam },

                    // ÖZGÜN ② AYNA (gelişmiş): dilim genişledi
                    new BossAttack { Name = "AMORTİSMAN+", RangeMul = 1.7f, Angle = 110f,
                                     Mirrored = true, WindupMul = 0.9f, CostMul = 1.45f,
                                     Tint = Cone },

                    // YENİ: iki imza birlikte — ayna, ÜÇ KEZ. Bir kez yana kaçmak
                    // yetmiyor ve geri çekilme yolu da kapalı.
                    new BossAttack { Name = "TAM EKSPERTİZ", RangeMul = 2.6f, Angle = 46f,
                                     Mirrored = true, Repeat = 3, WindupMul = 0.6f,
                                     CostMul = 1.15f, Tint = Beam },

                    // YENİ: dibine girene geniş kesik
                    new BossAttack { Name = "İBRA", RangeMul = 1.15f, Angle = 200f,
                                     MaxPlayerRange = 3.5f, WindupMul = 0.8f,
                                     CostMul = 1.2f, Tint = Cone },
                }
                : new[]
                {
                    // MENZİLLİ: ince ve UZUN — hattında durmak cezalı
                    new BossAttack { Name = "DEĞER BİÇME", RangeMul = 2.4f, Angle = 24f,
                                     WindupMul = 0.9f, CostMul = 1.3f, Tint = Beam },

                    // DÜZ · EKSPER: DAR ve HIZLI. Ölçer ve keser; ucuz ama sık.
                    // TAHSİLDAR'ın geniş-ağır vuruşunun tam tersi.
                    new BossAttack { Name = "KESİNTİ", RangeMul = 1f, Angle = 65f,
                                     WindupMul = 0.7f, CostMul = 0.95f, Tint = Cone },

                    // ÖZGÜN ① SAĞANAK — "bir kez kaçmak yetmiyor". Üç ölçüm peş peşe,
                    // aralarında boss dönüyor, yani hat her seferinde kayıyor.
                    new BossAttack { Name = "ÇİFT EKSPERTİZ", RangeMul = 2.4f, Angle = 28f,
                                     Repeat = 3, WindupMul = 0.7f, CostMul = 1.05f, Tint = Beam },

                    // ÖZGÜN ② AYNA — ÖN ve ARKA. Koninin iki cevabı vardı (yana kaç
                    // ya da geri çekil); bunun tek cevabı var. "Değer biçen adam
                    // iki uçtan ölçer" — ve TAHSİLDAR'ın öğrettiği "geri çekil"
                    // refleksi ikinci boss'ta ilk kez ceza alıyor.
                    new BossAttack { Name = "AMORTİSMAN", RangeMul = 1.4f, Angle = 90f,
                                     Mirrored = true, WindupMul = 1.1f, CostMul = 1.35f,
                                     Tint = Cone },
                };
        }

        sealed class Kefil : BossKind
        {
            public override string Name => "KEFİL";
            public override string Crime => "borcu garantiye aldı";
            public override Color Tint => new Color(0.45f, 0.75f, 0.85f);

            public override bool RefillsOnEmpty => true;
            public override bool SplitsOnPhaseTwo => true;
            public override float AuraDrain(Tuning t, int phase) => t.bossAuraDrain * 0.9f;

            // Faz 2'de KASA çağırıyor — kefilin işi TEMİNAT göstermek.
            public override float SummonInterval(int phase) => phase >= 2 ? 9f : 0f;
            public override EnemyKind SummonKind => EnemyKind.Vault;
            public override int SummonCount(int phase) => 1;
            public override int SummonMaxAlive => 2;

            public override BossAttack[] Attacks(int phase) => phase >= 2
                ? new[]
                {
                    // DÜZ (gelişmiş)
                    new BossAttack { Name = "KEFALET+", RangeMul = 1.2f, Angle = 140f,
                                     Repeat = 2, CostMul = 1.25f, Tint = Cone },

                    // MENZİLLİ (gelişmiş)
                    new BossAttack { Name = "SENET+", RangeMul = 3f, Angle = 24f,
                                     Repeat = 2, WindupMul = 0.7f, CostMul = 1.2f, Tint = Beam },

                    // ÖZGÜN ① SİMİT (gelişmiş): büyüdü, güvenli göbek DARALDI
                    new BossAttack { Name = "TEMİNAT+", RangeMul = 3.6f, Angle = 360f,
                                     InnerRadius = 1.7f, WindupSeconds = 1.05f,
                                     CostMul = 1.5f, Tint = Donut },

                    // ÖZGÜN ① SİMİT (dar hâli): küçük, hızlı, göbeği iğne deliği
                    new BossAttack { Name = "ORTAK HESAP+", RangeMul = 2.6f, Angle = 360f,
                                     InnerRadius = 1.2f, WindupSeconds = 1f,
                                     CostMul = 1.35f, Tint = Donut },

                    // ÖZGÜN ② ÇEKİM (gelişmiş): daha geniş, daha sert çekiyor
                    new BossAttack { Name = "ZİNCİRLEME SORUMLULUK+", RangeMul = 4f, Angle = 170f,
                                     Pull = 9f, MinPlayerRange = 3.5f, WindupSeconds = 0.9f,
                                     CostMul = 0.95f, Tint = Donut },

                    // YENİ: her yönden çeken halka. Kaçacak yön yok — nereye
                    // kaçarsan kaç içeri alınıyorsun, sonra yakın vuruşları seni
                    // orada buluyor. "Müteselsil kefalet": hepsi birbirine bağlı.
                    new BossAttack { Name = "MÜTESELSİL", RangeMul = 3f, Angle = 360f,
                                     Pull = 6.5f, WindupSeconds = 1.1f, CostMul = 1.4f,
                                     Tint = Donut },
                }
                : new[]
                {
                    // DÜZ · KEFİL: YAVAŞ ve PAHALI. Savunmada durur, nadiren vurur
                    // ama vurunca borcu hatırlatır.
                    new BossAttack { Name = "KEFALET", RangeMul = 0.95f, Angle = 100f,
                                     WindupMul = 1.35f, CostMul = 1.6f, Tint = Cone },

                    // MENZİLLİ
                    new BossAttack { Name = "SENET", RangeMul = 2.6f, Angle = 26f,
                                     Repeat = 2, WindupMul = 0.75f, CostMul = 1.15f, Tint = Beam },

                    // ÖZGÜN ① SİMİT — "uzaklaş" değil "YAKLAŞ". Oyuncunun ilk iki
                    // boss'ta öğrendiği refleks burada onu öldürüyor.
                    // ⚠️ Hazırlık MUTLAK: göbeğe KOŞACAK süre lazım.
                    new BossAttack { Name = "TEMİNAT", RangeMul = 3f, Angle = 360f,
                                     InnerRadius = 2.4f, WindupSeconds = 1.2f,
                                     CostMul = 1.4f, Tint = Donut },

                    // ÖZGÜN ① SİMİT (küçük hâli): aynı soru, çok daha dar cevapla
                    new BossAttack { Name = "ORTAK HESAP", RangeMul = 2.2f, Angle = 360f,
                                     InnerRadius = 1.4f, WindupSeconds = 0.95f,
                                     CostMul = 1.2f, Tint = Donut },

                    // ÖZGÜN ② ÇEKİM — beşinci kaçış sorusu ve en sinsisi: kaçmak
                    // İŞE YARAMIYOR, seni geri alıyor. Bedeli bilerek düşük;
                    // tehdit hasar değil KONUM. Kefilin dibi zaten emiyor ve
                    // KEFALET tam oradan çıkıyor.
                    new BossAttack { Name = "ZİNCİRLEME SORUMLULUK", RangeMul = 3.4f, Angle = 140f,
                                     Pull = 7f, MinPlayerRange = 3.5f, WindupSeconds = 0.9f,
                                     CostMul = 0.75f, Tint = Donut },
                };
        }

        sealed class Simsar : BossKind
        {
            public override string Name => "SİMSAR";
            public override string Crime => "durduğun yeri satıyor";
            public override Color Tint => new Color(0.8f, 0.45f, 0.9f);

            // ⚠️ Diğerlerinden farkı: SANA GELMİYOR, durduğun YERE geliyor.
            public override float GroundMarkInterval(int phase) => phase >= 2 ? 1.1f : 2.0f;
            public override float AuraDrain(Tuning t, int phase) => t.bossAuraDrain * 1f;
            public override float SpeedMultiplier(int phase) => phase >= 2 ? 1.2f : 0.85f;

            // Faz 2'de MÜBAŞİR çağırıyor: onlar da uzaktan yere vuruyor.
            public override float SummonInterval(int phase) => phase >= 2 ? 8.5f : 0f;
            public override EnemyKind SummonKind => EnemyKind.Bailiff;
            public override int SummonCount(int phase) => 2;
            public override int SummonMaxAlive => 3;

            public override BossAttack[] Attacks(int phase) => phase >= 2
                ? new[]
                {
                    // DÜZ (gelişmiş)
                    new BossAttack { Name = "PAZARLIK+", RangeMul = 1.3f, Angle = 165f,
                                     MaxPlayerRange = 4.5f, WindupMul = 0.75f,
                                     CostMul = 1.3f, Tint = Cone },

                    // MENZİLLİ (gelişmiş)
                    new BossAttack { Name = "AÇIK ARTIRMA+", RangeMul = 3f, Angle = 26f,
                                     Repeat = 2, WindupMul = 0.6f, CostMul = 1.25f, Tint = Beam },

                    // ÖZGÜN ① AÇIKLIK (gelişmiş): kapı daraldı
                    new BossAttack { Name = "KOMİSYON+", RangeMul = 3f, Angle = 360f,
                                     SafeAngle = 62f, WindupSeconds = 1.3f,
                                     CostMul = 1.5f, Tint = Ring },

                    // ÖZGÜN ① AÇIKLIK (en geniş hâli): arena kadar erişim, iğne deliği kapı
                    new BossAttack { Name = "MÜZAYEDE", RangeMul = 3.4f, Angle = 360f,
                                     SafeAngle = 45f, WindupSeconds = 1.6f,
                                     CostMul = 1.55f, Tint = Ring },

                    // ÖZGÜN ② GERİ SIÇRAMA (gelişmiş): daha geniş, daha uzağa kaçıyor
                    new BossAttack { Name = "DEVİR+", RangeMul = 1.9f, Angle = 230f,
                                     Recoil = 7f, WindupMul = 0.8f, CostMul = 1.4f, Tint = Cone },

                    // YENİ: iki kez vurup iki kez çekiliyor — peşinden gidersen
                    // ikinciyi yiyorsun, gitmezsen yer işaretleri seni buluyor
                    new BossAttack { Name = "HARAÇ", RangeMul = 2.6f, Angle = 100f,
                                     Recoil = 4f, Repeat = 2, CostMul = 1.3f, Tint = Cone },
                }
                : new[]
                {
                    // DÜZ
                    new BossAttack { Name = "PAZARLIK", RangeMul = 1.15f, Angle = 150f,
                                     MaxPlayerRange = 4f, WindupMul = 0.85f,
                                     CostMul = 1.15f, Tint = Cone },

                    // MENZİLLİ
                    new BossAttack { Name = "AÇIK ARTIRMA", RangeMul = 2.8f, Angle = 30f,
                                     Repeat = 2, WindupMul = 0.8f, CostMul = 1.15f, Tint = Beam },

                    // ÖZGÜN ① AÇIKLIK — "uzaklaş" da değil "yaklaş" da değil:
                    // ARKASINA GEÇ. Aracının işi seni yanlış tarafta tutmak.
                    // ⚠️ Hazırlık MUTLAK ve UZUN: arkasına KOŞMAN gerekiyor.
                    new BossAttack { Name = "KOMİSYON", RangeMul = 2.6f, Angle = 360f,
                                     SafeAngle = 90f, WindupSeconds = 1.5f,
                                     CostMul = 1.45f, Tint = Ring },

                    // ÖZGÜN ① AÇIKLIK (yakın hâli): kısa hazırlık, dar alan,
                    // dibindeyken de aynı soruyu soruyor
                    new BossAttack { Name = "TELLALLIK", RangeMul = 1.6f, Angle = 360f,
                                     SafeAngle = 70f, WindupSeconds = 1f,
                                     CostMul = 1.3f, Tint = Ring },

                    // ÖZGÜN ② GERİ SIÇRAMA — hamlenin TERSİ. "Kaç" değil "KOVALA".
                    // ⚠️ SİMSAR'ın kendi mekaniğiyle birleşiyor: kovalarken
                    // durduğun yere bıraktığı işaretlerin üstünden geçiyorsun.
                    // Tek saldırıda iki mekanik birbirine soru soruyor.
                    new BossAttack { Name = "DEVİR", RangeMul = 1.6f, Angle = 200f,
                                     Recoil = 5f, WindupMul = 0.9f, CostMul = 1.35f, Tint = Cone },
                };
        }

        sealed class Murabahaci : BossKind
        {
            public override string Name => "MURABAHACI";
            public override string Crime => "borç işlemeye devam ediyor";
            public override Color Tint => new Color(0.55f, 0.35f, 0.8f);

            public override bool RefillsOnEmpty => true;
            public override float AuraDrain(Tuning t, int phase) => t.bossAuraDrain * 1.2f;

            // Vurmadığın her saniye havuzu doluyor — ondan HIZLI olmak zorundasın.
            public override float RegenPerSecond(Tuning t, int phase)
                => phase >= 2 ? t.usurerRegenPerSecond : 0f;

            // ── FAZ 3 (Enes kararı) ───────────────────────────────────────────
            // ⚠️ Bu, karakterin kendisi: MURABAHACI hiç kendi eliyle tahsil etmez.
            // Faz 1'de senedini gösterir, faz 2'de faiz işletir, faz 3'te
            // ADAMLARINI YOLLAR. Öne ancak onlar bittiğinde çıkıyor.
            public override int PhaseCount => 3;
            public override int SummonsRivalsInPhase => 3;

            public override float SummonInterval(int phase) => phase == 2 ? 7.5f : 0f;
            public override EnemyKind SummonKind => EnemyKind.Lapse;
            public override int SummonCount(int phase) => 2;
            public override int SummonMaxAlive => 4;

            public override BossAttack[] Attacks(int phase) => phase >= 3
                ? new[]
                {
                    // ══ FAZ 3 — ÖNCEKİ HERKESİN İMZASI ═══════════════════════
                    // Enes: *"son boss önceki tüm bossların özelliğini de
                    // kullanabilsin."* Şekil tekeli YALNIZCA burada kalkıyor,
                    // ve kalkması finalin anlamı: dört alacaklıyı da yendin,
                    // şimdi hepsi tek adamda karşında.

                    // KENDİ ② (en sert hâli): TÜM HARİTA, minicik güvenli nokta
                    new BossAttack { Name = "TAM TASFİYE", RangeMul = 9f, Angle = 360f,
                                     SafeSpotRadius = 2.6f, WindupSeconds = 2.2f,
                                     CostMul = 2.2f, Tint = Ring },

                    // KENDİ ① ZİNCİR (üçlü): halka → simit → açıklık.
                    // Üç ayrı cevabı SIRAYLA vermen gerekiyor — TAHSİLDAR'ın,
                    // KEFİL'in ve SİMSAR'ın sorusu tek nefeste.
                    new BossAttack { Name = "BİLEŞİK FAİZ++", RangeMul = 2.4f, Angle = 360f,
                                     WindupSeconds = 0.9f, CostMul = 1.5f, Tint = Ring,
                                     Then = new BossAttack { Name = "…SİMİT", RangeMul = 3.2f,
                                         Angle = 360f, InnerRadius = 2.2f, WindupSeconds = 0.9f,
                                         CostMul = 1.4f, Tint = Donut,
                                         Then = new BossAttack { Name = "…AÇIKLIK", RangeMul = 2.8f,
                                             Angle = 360f, SafeAngle = 80f, WindupSeconds = 1.2f,
                                             CostMul = 1.4f, Tint = Ring } } },

                    // EKSPER'in SAĞANAK'ı — dört ışın peş peşe
                    new BossAttack { Name = "SON İHTAR", RangeMul = 3.4f, Angle = 22f,
                                     Repeat = 4, WindupMul = 0.55f, CostMul = 1.25f, Tint = Beam },

                    // EKSPER'in AYNA'sı
                    new BossAttack { Name = "SON EKSPERTİZ", RangeMul = 2f, Angle = 120f,
                                     Mirrored = true, Repeat = 2, WindupMul = 0.85f,
                                     CostMul = 1.5f, Tint = Cone },

                    // TAHSİLDAR'ın HAMLE'si + halka kapanışı
                    new BossAttack { Name = "İCRA++", RangeMul = 2.6f, Angle = 110f,
                                     Lunge = 5.5f, MinPlayerRange = 4f, CostMul = 1.55f,
                                     Tint = Cone,
                                     Then = new BossAttack { Name = "…KAPANIŞ", RangeMul = 1.5f,
                                         Angle = 360f, WindupMul = 0.8f, CostMul = 1.45f,
                                         Tint = Ring } },

                    // KEFİL'in ÇEKİM'i
                    new BossAttack { Name = "SON TEMİNAT", RangeMul = 3.6f, Angle = 360f,
                                     Pull = 8f, WindupSeconds = 1f, CostMul = 1.45f, Tint = Donut },

                    // SİMSAR'ın GERİ SIÇRAMA'sı
                    new BossAttack { Name = "SON DEVİR", RangeMul = 2.2f, Angle = 240f,
                                     Recoil = 6f, WindupMul = 0.8f, CostMul = 1.45f, Tint = Cone },

                    // DÜZ (her boss'un klasiği de gelişmeli)
                    new BossAttack { Name = "VADE++", RangeMul = 1.5f, Angle = 140f,
                                     CostMul = 1.4f, Tint = Cone },
                }
                : phase >= 2
                ? new[]
                {
                    // DÜZ (gelişmiş): iki kez, hazırlığı kısa
                    new BossAttack { Name = "FAİZ", RangeMul = 1.15f, Angle = 150f,
                                     Repeat = 2, WindupMul = 0.6f, CostMul = 1.1f, Tint = Cone },

                    // MENZİLLİ (gelişmiş)
                    new BossAttack { Name = "İBRAZ+", RangeMul = 3.2f, Angle = 24f,
                                     Repeat = 2, MinPlayerRange = 3.5f, WindupMul = 0.7f,
                                     CostMul = 1.3f, Tint = Beam },

                    // ÖZGÜN ① ZİNCİR (gelişmiş): halka → HEMEN halka, ikisi de büyük
                    new BossAttack { Name = "BİLEŞİK FAİZ+", RangeMul = 2.6f, Angle = 150f,
                                     WindupSeconds = 0.9f, CostMul = 1.4f, Tint = Cone,
                                     Then = new BossAttack { Name = "…ve TAKSİT+", RangeMul = 1.8f,
                                         Angle = 250f, WindupSeconds = 0.9f, CostMul = 1.45f,
                                         Tint = Cone } },

                    // ÖZGÜN ① ZİNCİR (yakın hâli): koni → halka
                    new BossAttack { Name = "İCRA+", RangeMul = 1.5f, Angle = 220f,
                                     WindupMul = 1.1f, CostMul = 1.5f, Tint = Cone,
                                     Then = new BossAttack { Name = "…ve KAPANIŞ", RangeMul = 1.4f,
                                         Angle = 100f, WindupMul = 0.8f, CostMul = 1.35f,
                                         Tint = Cone } },

                    // ÖZGÜN ② RASTGELE NOKTA (gelişmiş): erişimi büyüdü, nokta küçüldü
                    new BossAttack { Name = "SÖZLEŞME+", RangeMul = 6.5f, Angle = 360f,
                                     SafeSpotRadius = 2.9f, WindupSeconds = 2.1f,
                                     CostMul = 1.9f, Tint = Ring },

                    // YENİ: dibindeyken hızlı geniş vuruş — yaklaşmanın bedeli
                    new BossAttack { Name = "VADE+", RangeMul = 1.5f, Angle = 110f,
                                     MaxPlayerRange = 4f, WindupMul = 0.9f,
                                     CostMul = 1.35f, Tint = Cone },

                    // YENİ: üçlü zincir değil ama en uzun menzilli ışın —
                    // geri sarma fazında uzaktan yıpratma cevabını da kapatıyor
                    new BossAttack { Name = "MUACCELİYET", RangeMul = 3.6f, Angle = 30f,
                                     MinPlayerRange = 4f, WindupMul = 1f,
                                     CostMul = 1.4f, Tint = Beam },
                }
                : new[]
                {
                    // DÜZ
                    new BossAttack { Name = "VADE", RangeMul = 1.4f, Angle = 90f,
                                     CostMul = 1.2f, Tint = Cone },

                    // MENZİLLİ: bu adamdan uzak durarak kaçamıyorsun — kural
                    // faz 1'de kurulmalı, yoksa faz 2 ve 3 yanlış bir cevabı yıkar.
                    new BossAttack { Name = "İBRAZ", RangeMul = 2.8f, Angle = 28f,
                                     Repeat = 2, MinPlayerRange = 3.5f, WindupMul = 0.8f,
                                     CostMul = 1.25f, Tint = Beam },

                    // ÖZGÜN ① ZİNCİR — "bir cevaba yerleşemezsin". Borç İŞLER:
                    // her taksit bir öncekinin üstüne biniyor.
                    new BossAttack { Name = "İCRA", RangeMul = 1.6f, Angle = 200f,
                                     WindupMul = 1.2f, CostMul = 1.45f, Tint = Cone,
                                     Then = new BossAttack { Name = "…ve TEMERRÜT", RangeMul = 1.2f,
                                         Angle = 150f, WindupMul = 0.6f, CostMul = 1.2f,
                                         Tint = Cone } },

                    new BossAttack { Name = "BİLEŞİK FAİZ", RangeMul = 2.4f, Angle = 120f,
                                     WindupSeconds = 0.9f, CostMul = 1.35f, Tint = Cone,
                                     Then = new BossAttack { Name = "…ve TAKSİT", RangeMul = 1.5f,
                                         Angle = 240f, WindupMul = 0.7f, CostMul = 1.4f,
                                         Tint = Cone } },

                    // ÖZGÜN ② RASTGELE NOKTA — dördüncü kaçış sorusu: "uzaklaş"
                    // değil, "yaklaş" değil, "arkasına geç" değil — ORAYA YETİŞ.
                    // ⚠️ Faz 1'de daha küçük ve daha cömert; faz 3'te bütün
                    // haritayı yutuyor. Mekanik önce ÖĞRETİLİYOR, sonra silah
                    // oluyor — projedeki her özgün saldırının izlediği yol.
                    new BossAttack { Name = "SÖZLEŞME", RangeMul = 5f, Angle = 360f,
                                     SafeSpotRadius = 3.2f, WindupSeconds = 2f,
                                     CostMul = 1.7f, Tint = Ring },
                };
        }

        // ⚠️ SIRA = BÖLGE SIRASI. Beş bölge var, beş boss olmalı — dördüncü ve beşinci
        // bölge aynı boss'u alıyordu (`For` son elemana kırpıyordu). Enes bildirdi.
        // MURABAHACI SONDA kalmalı: sözleşmeyi elinde tutan, koşunun finali.
        public static readonly BossKind[] All =
            { new Tahsildar(), new Eksper(), new Kefil(), new Simsar(), new Murabahaci() };

        static BossKind()
        {
            for (int i = 0; i < All.Length; i++) All[i].Index = i;
        }

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
