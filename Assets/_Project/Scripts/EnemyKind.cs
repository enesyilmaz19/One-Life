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

        // ── Faz 13 kancaları ─────────────────────────────────────────────────

        /// <summary>
        /// &gt;0 ise bu yarıçaptaki DİĞER düşmanların hasadını kısıyor — SİGORTA.
        /// SIZINTI zaman EMER; bu GELİRİ KAPATIR. İkisi de hedef sırasını değiştiriyor
        /// ama farklı soruyla: biri "ne kadar pahalı", öteki "ne zaman kârlı".
        /// </summary>
        public virtual float ProtectRadius(Tuning t) => 0f;

        /// <summary>Koruma altındaki düşmanlardan alınan hasadın çarpanı.</summary>
        public virtual float ProtectedHarvest(Tuning t) => 1f;

        /// <summary>
        /// &gt;0 ise bu kadar saniyede bir UZAKTAN oyuncunun yerine gecikmeli vuruş
        /// bırakıyor — MÜBAŞİR. Bütün düşmanlar yakın dövüştü, yani mesafe tutmak
        /// her zaman güvenliydi; bu onu bozuyor.
        /// </summary>
        public virtual float RangedMarkInterval(Tuning t) => 0f;

        /// <summary>
        /// &gt;0 ise oyuncuya bu mesafeden fazla YAKLAŞMIYOR. Menzilli düşman dibine
        /// gelmemeli, yoksa "menzilli" olmaktan çıkar.
        /// </summary>
        public virtual float PreferredRange(Tuning t) => 0f;

        /// <summary>
        /// Saniyede ne kadar güçleniyor (0.05 = %5/sn) — ZAMANAŞIMI.
        /// Dövüşü uzatmayı ve kite'lamayı cezalandırıyor: ötelemek onu besliyor.
        /// </summary>
        public virtual float GrowthPerSecond => 0f;

        // ── Faz 34 kancaları ─────────────────────────────────────────────────

        /// <summary>&lt;1 ise ÖNÜNDEN gelen hasar × bu — KALKANLI. Arkası çıplak.</summary>
        public virtual float ÖndenHasar => 1f;

        /// <summary>&gt;0 ise oyuncuya bu hızla döner (derece/sn) — KALKANLI. 0 = anında.</summary>
        public virtual float DönüşHızı => 0f;

        /// <summary>&gt;0 ise öldüğü yerde bu yarıçapta gecikmeli patlar — MÜFLİS.</summary>
        public virtual float PatlamaYarıçapı => 0f;
        public virtual float PatlamaGecikmesi => 1f;
        /// <summary>Patlamanın oyuncuya bedeli, tam darbeye çarpan.</summary>
        public virtual float PatlamaBedeli => 1f;
        /// <summary>Patlamanın içindeki düşmanlara hasarı, MÜFLİS'in başlangıç havuzuna oran.</summary>
        public virtual float PatlamaHasarı => 0f;

        // ── Faz 35 kancaları ─────────────────────────────────────────────────

        /// <summary>&gt;0 ise bu kadar saniyede bir açılır — VADELİ. Kapalıyken hasar × `KapalıHasar`.</summary>
        public virtual float VadeAralığı => 0f;
        public virtual float VadeAçıklığı => 0f;
        public virtual float KapalıHasar => 1f;

        /// <summary>&gt;0 ise ölünce bu kadar küçük taksit doğar — TAKSİT. Her biri başlangıç havuzunun `TaksitPayı`.</summary>
        public virtual int TaksitSayısı => 0;
        public virtual float TaksitPayı => 0f;

        // ── Faz 36 kancaları ─────────────────────────────────────────────────

        /// <summary>&gt;0 ise bu menzilde oyuncuya yapışır — SÜLÜK. Yapışıkken saniyede `YapışıkEmme` emer; dash `KopmaSüresi` koparır.</summary>
        public virtual float YapışmaMenzili => 0f;
        public virtual float YapışıkEmme => 0f;
        public virtual float KopmaSüresi => 0f;

        /// <summary>&gt;0 ise yaşadıkça hasadı artar (saniyede bu kadar, en çok `FaizTavanı`) — MEVDUAT.</summary>
        public virtual float FaizHızı => 0f;
        public virtual float FaizTavanı => 1f;
        public float Faiz(float yaş) => FaizHızı > 0f ? Mathf.Min(FaizTavanı, 1f + FaizHızı * yaş) : 1f;

        // ── Faz 37 kancaları ─────────────────────────────────────────────────

        /// <summary>&gt;0 ise bu yarıçap içinde oyuncunun yürüyüşü × `BlokeYavaşlığı` — BLOKE. Dash serbest.</summary>
        public virtual float BlokeYarıçapı => 0f;
        public virtual float BlokeYavaşlığı => 1f;

        /// <summary>
        /// &gt;0 ise yaşadığı sürece oyuncuya inen her darbe sonrakini bu oran kadar pahalı yapar (en çok `CezaTavanı`);
        /// `CezaSüresi` darbesiz geçince zincir söner — CEZAİ ŞART.
        /// </summary>
        public virtual float CezaArtışı => 0f;
        public virtual float CezaTavanı => 1f;
        public virtual float CezaSüresi => 0f;

        // ── Faz 38 kancaları ─────────────────────────────────────────────────

        /// <summary>&gt;0 ise bu kadar saniye yaşarsa ödemeden gider, bedeli oyuncuya (`MühletBedeli` darbe, dash'le kaçılmaz) — MÜHLET.</summary>
        public virtual float MühletSüresi => 0f;
        public virtual float MühletBedeli => 0f;

        /// <summary>&gt;0 ise bu yarıçaptaki öbür düşmanların havuzunu saniyede başlangıçlarının `RefinansHızı` kadar doldurur — REFİNANS.</summary>
        public virtual float RefinansYarıçapı => 0f;
        public virtual float RefinansHızı => 0f;

        public static readonly EnemyKind Basic = new BasicKind();
        public static readonly EnemyKind Leak = new LeakKind();
        public static readonly EnemyKind Vault = new VaultKind();
        public static readonly EnemyKind Runner = new RunnerKind();
        public static readonly EnemyKind Insurer = new InsurerKind();
        public static readonly EnemyKind Bailiff = new BailiffKind();
        public static readonly EnemyKind Lapse = new LapseKind();
        public static readonly EnemyKind Shield = new ShieldKind();
        public static readonly EnemyKind Bankrupt = new BankruptKind();
        public static readonly EnemyKind Term = new TermKind();
        public static readonly EnemyKind Installment = new InstallmentKind();
        /// <summary>TAKSİT'in doğurduğu küçük taksit — odalar bunu seçmez (`All`'da değil), bölünmez.</summary>
        public static readonly EnemyKind InstallmentPart = new InstallmentPartKind();
        public static readonly EnemyKind Leech = new LeechKind();
        public static readonly EnemyKind Deposit = new DepositKind();
        public static readonly EnemyKind Frozen = new FrozenKind();
        public static readonly EnemyKind Penalty = new PenaltyKind();
        public static readonly EnemyKind Grace = new GraceKind();
        public static readonly EnemyKind Refinance = new RefinanceKind();

        /// <summary>
        /// Oda kompozisyonu bu listeden seçiyor. ⚠️ SIRA = AÇILIŞ SIRASI:
        /// `RoomManager.PickKind` bölgeye göre baştan kaç tür açık olduğunu belirliyor,
        /// yani buraya eklenen yeni tür GEÇ bölgelere düşer. Öğretim sırası budur.
        /// ⚠️ Faz 34: KALKANLI ve MÜFLİS 1. bölgenin (SIZINTI'dan sonra) — bölge tavanları da 2 arttı.
        /// ⚠️ Faz 35: VADELİ ve TAKSİT 2. bölgenin (KASA'dan sonra) — tavanlar 4 · 7 · 8 · 10 · 11.
        /// ⚠️ Faz 36: SÜLÜK ve MEVDUAT 3. bölgenin (KAÇAK'tan sonra) — tavanlar 4 · 7 · 10 · 12 · 13.
        /// ⚠️ Faz 37: BLOKE ve CEZAİ ŞART 4. bölgenin (MEVDUAT'tan sonra) — tavanlar 4 · 7 · 10 · 14 · 15.
        /// ⚠️ Faz 38: MÜHLET ve REFİNANS 5. bölgenin (MÜBAŞİR'den sonra, ZAMANAŞIMI hep sonda) — tavanlar 4 · 7 · 10 · 14 · 17.
        /// </summary>
        public static readonly EnemyKind[] All =
            { Basic, Leak, Shield, Bankrupt, Vault, Term, Installment, Runner, Leech, Deposit, Frozen, Penalty, Insurer, Bailiff, Grace, Refinance, Lapse };
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

    /// <summary>
    /// SİGORTA — yaşadığı sürece ÇEVRESİNDEKİLERİN hasadını kısar. Kendisi zararsız.
    ///
    /// Değiştirdiği karar: hedef sırası — ama SIZINTI'dan FARKLI bir soruyla.
    /// SIZINTI "ne kadar pahalı" diye sordurur (zaman emiyor); SİGORTA "ne zaman
    /// kârlı" diye sordurur (gelirini kapatıyor). Yanındakileri kesmek boşa emek:
    /// önce sigortayı kes, sonra yağmala.
    ///
    /// Zayıf ve yavaş — tehdidi kendi vuruşu değil, VARLIĞI.
    /// </summary>
    public sealed class InsurerKind : EnemyKind
    {
        public override string Name => "SİGORTA";
        public override Color FullColor => new Color(0.35f, 0.55f, 0.95f);
        public override float ProtectRadius(Tuning t) => t.insurerRadius;
        public override float ProtectedHarvest(Tuning t) => t.insurerHarvestMultiplier;
        public override float PoolMultiplier => 0.7f;
        public override float SpeedMultiplier => 0.8f;
    }

    /// <summary>
    /// MÜBAŞİR — uzaktan tebligat. Yaklaşmıyor, durduğun yere gecikmeli vuruş bırakıyor.
    ///
    /// Değiştirdiği karar: MESAFE. Bu güne kadar bütün düşmanlar yakın dövüştü, yani
    /// uzak durmak her zaman güvenliydi ve "kite" bedava bir cevaptı. MÜBAŞİR onu
    /// bozuyor — ya yaklaşıp kesersin ya tebligatı yersin.
    ///
    /// ⚠️ `PreferredRange` şart: dibine gelirse "menzilli" olmaktan çıkar.
    /// </summary>
    public sealed class BailiffKind : EnemyKind
    {
        public override string Name => "MÜBAŞİR";
        public override Color FullColor => new Color(0.95f, 0.55f, 0.25f);
        public override float RangedMarkInterval(Tuning t) => t.bailiffMarkInterval;
        public override float PreferredRange(Tuning t) => t.bailiffKeepDistance;
        public override float PoolMultiplier => 0.65f;
        public override float SpeedMultiplier => 0.9f;
    }

    /// <summary>
    /// KALKANLI — önden gelen hasarı kalkanıyla kısar, yavaş döner (Enes'in fikri, playtest 2026-09-30).
    ///
    /// Değiştirdiği karar: KONUM. Bütün türler "hangisi önce" diye sordururken bu "nereden" diye
    /// sordurur: önünden vurmak beşte bir eder, arkasına geç. ZIRHLI'dan farkı: zırh her yönden kısar,
    /// kalkan yalnız önden — cevabı olan bir engel. Yavaş dönmesi şart: anında dönseydi arkası hiç olmazdı.
    /// </summary>
    public sealed class ShieldKind : EnemyKind
    {
        public override string Name => "KALKANLI";
        public override Color FullColor => new Color(0.6f, 0.68f, 0.78f);
        public override float ÖndenHasar => 0.2f;
        public override float DönüşHızı => 100f;
        public override float SpeedMultiplier => 0.85f;
    }

    /// <summary>
    /// MÜFLİS — öldüğü yerde borcu patlar (1 sn sonra): oyuncuya darbe, içindeki düşmanlara hasar.
    ///
    /// Değiştirdiği karar: NEREDE ÖLDÜRECEĞİN. Kalabalığın ortasında öldür — patlama öbürlerini de
    /// vurur — ve sen çekil. Dibinde öldürüp durursan borcunu sen ödersin. Zayıf, ki "ne zaman"
    /// değil "nerede" sorusu kalsın.
    /// </summary>
    public sealed class BankruptKind : EnemyKind
    {
        public override string Name => "MÜFLİS";
        public override Color FullColor => new Color(0.72f, 0.5f, 0.32f);
        public override float PoolMultiplier => 0.6f;
        public override float SpeedMultiplier => 1.05f;
        public override float PatlamaYarıçapı => 2.4f;
        public override float PatlamaBedeli => 1.2f;
        public override float PatlamaHasarı => 0.6f;
    }

    /// <summary>
    /// VADELİ — 4 sn'de bir 1.5 sn açılır; kapalıyken vuruşun onda biri eder (rengi koyulaşır).
    ///
    /// Değiştirdiği karar: ZAMANLAMA. KALKANLI "nereden" diye sordururken bu "ne zaman" diye sordurur:
    /// kapalıyken başkasına dön, vadesi gelince ona. DARALMA'da (oyalanmak pahalı) beklemek de bedelli.
    /// </summary>
    public sealed class TermKind : EnemyKind
    {
        public override string Name => "VADELİ";
        public override Color FullColor => new Color(0.45f, 0.8f, 0.85f);
        public override float VadeAralığı => 4f;
        public override float VadeAçıklığı => 1.5f;
        public override float KapalıHasar => 0.1f;
    }

    /// <summary>
    /// TAKSİT — ölünce iki küçük taksit doğar (havuzunun %35'i; onlar bölünmez).
    ///
    /// Değiştirdiği karar: TOPLAMAK. Tek hedefli silahla kesersen borç çoğalır; ikisini birden
    /// alan bir savuruş için yerini seç. Toplam havuz %170 — dağınık kesmek pahalı.
    /// </summary>
    public sealed class InstallmentKind : EnemyKind
    {
        public override string Name => "TAKSİT";
        public override Color FullColor => new Color(0.85f, 0.55f, 0.7f);
        public override int TaksitSayısı => 2;
        public override float TaksitPayı => 0.35f;
    }

    /// <summary>
    /// SÜLÜK — değince yapışır, peşinden gelir ve saniyede 1 sn emer; yapışıkken saldırmaz.
    ///
    /// Değiştirdiği karar: DASH. Dash bugüne kadar yalnızca kaçmaktı; burada bir harcama: dash'le silkelen (2 sn
    /// yapışamaz) ya da dash'ini sakla, yapışıkken vurup sök. Hızlı ve zayıf — yakalaması değil, ondan kurtulması iş.
    /// </summary>
    public sealed class LeechKind : EnemyKind
    {
        public override string Name => "SÜLÜK";
        public override Color FullColor => new Color(0.5f, 0.15f, 0.25f);
        public override float YapışmaMenzili => 1f;
        public override float YapışıkEmme => 1f;
        public override float KopmaSüresi => 2f;
        public override float PoolMultiplier => 0.7f;
        public override float SpeedMultiplier => 1.25f;
    }

    /// <summary>
    /// MEVDUAT — yaşadıkça hasadı artar: saniyede +%10, en çok ×2.5 (altın rengine döner).
    ///
    /// Değiştirdiği karar: BEKLEMEK. ZAMANAŞIMI'nın tersi: o beklemeyi cezalandırıyor, bu ödüllendiriyor — ama
    /// beklerken sana vuruyor. KITLIK'ta (hasat az) sabrın getirisi; açgözlülüğün bedeli darbe.
    /// </summary>
    public sealed class DepositKind : EnemyKind
    {
        public override string Name => "MEVDUAT";
        public override Color FullColor => new Color(0.55f, 0.6f, 0.35f);
        public override float FaizHızı => 0.1f;
        public override float FaizTavanı => 2.5f;
    }

    /// <summary>
    /// BLOKE — yanındayken (3 birim) hesabın bloke: yürüyüşün ×0.55. Dash serbest; çevresinde soluk bir halka.
    ///
    /// Değiştirdiği karar: NEREDE DÖVÜŞECEĞİN. AĞIRLIK'ta (darbe pahalı) yavaşlamak kaçamamak demek: onun yanında
    /// dövüşme — önce onu kes, ya da öbürlerini ondan uzağa çek. Kalın ve yavaş, ki "önce onu" ucuz bir cevap olmasın.
    /// </summary>
    public sealed class FrozenKind : EnemyKind
    {
        public override string Name => "BLOKE";
        public override Color FullColor => new Color(0.45f, 0.42f, 0.7f);
        public override float BlokeYarıçapı => 3f;
        public override float BlokeYavaşlığı => 0.55f;
        public override float PoolMultiplier => 1.2f;
        public override float SpeedMultiplier => 0.8f;
    }

    /// <summary>
    /// CEZAİ ŞART — yaşadığı sürece sana inen her darbe sonrakini %40 pahalı yapar (en çok ×2.2); 4 sn darbesiz kalırsan
    /// zincir söner. Zincir uzadıkça kızarır.
    ///
    /// Değiştirdiği karar: HATADAN SONRA. AĞIRLIK'ın kuralı (hata affetmiyor) burada katlanıyor: darbe yediysen geri çekil,
    /// zincir sönsün — ya da önce onu kes, zincir onunla ölür. Kendisi sıradan ve hızlı.
    /// </summary>
    public sealed class PenaltyKind : EnemyKind
    {
        public override string Name => "CEZAİ ŞART";
        public override Color FullColor => new Color(0.8f, 0.3f, 0.45f);
        public override float CezaArtışı => 0.4f;
        public override float CezaTavanı => 2.2f;
        public override float CezaSüresi => 4f;
        public override float PoolMultiplier => 0.75f;
        public override float SpeedMultiplier => 1.1f;
    }

    /// <summary>
    /// MÜHLET — sana gelmez, 6 birim uzakta durur, vurmaz; 15 sn mühleti var: o sürede peşine düşüp kesmezsen ödemeden gider, bedeli
    /// sana (3 darbe, dash'le kaçılmaz). Süre azaldıkça soluyor.
    ///
    /// Değiştirdiği karar: YARIŞ. SIZINTI'da (sürekli emiliyor) hız zaten şart; bu ona bir saat koyuyor: odaya girince önce kimi
    /// keseceğin artık "kim tehlikeli" değil "kimin süresi bitiyor" — kalabalığı bırakıp kovalar mısın, bedeli mi ödersin.
    /// KAÇAK canı azalınca kaçar, bu süresi dolunca.
    /// ⚠️ Uzak durması şart: üstüne koşan MÜHLET ilk kalabalıkla ölüyordu — 18 sn'de de 12 sn'de de 20'nin 20'si yetişti (P38).
    /// </summary>
    public sealed class GraceKind : EnemyKind
    {
        public override string Name => "MÜHLET";
        public override Color FullColor => new Color(0.95f, 0.8f, 0.45f);
        public override float MühletSüresi => 15f;
        public override float PreferredRange(Tuning t) => 6f;
        public override float MühletBedeli => 3f;
        public override float PoolMultiplier => 0.8f;
        public override float SpeedMultiplier => 1.05f;
    }

    /// <summary>
    /// REFİNANS — yanındakilerin borcunu yeniler: 4 birim içindeki öbür düşmanların havuzu saniyede başlangıcının %4'ü dolar.
    /// Çevresinde yeşil halka.
    ///
    /// Değiştirdiği karar: HEDEF SIRASI — SİGORTA'dan farklı soruyla: o geliri kısar, bu dövüşü UZATIR. SIZINTI'da uzayan her
    /// saniye emiliyor: önce onu kes, ya da kalabalığı ondan uzağa çek. Kendisi zayıf ve yavaş.
    /// </summary>
    public sealed class RefinanceKind : EnemyKind
    {
        public override string Name => "REFİNANS";
        public override Color FullColor => new Color(0.4f, 0.85f, 0.5f);
        public override float RefinansYarıçapı => 4f;
        public override float RefinansHızı => 0.04f;
        public override float PoolMultiplier => 0.7f;
        public override float SpeedMultiplier => 0.85f;
    }

    /// <summary>TAKSİT'in küçük taksiti — hızlı, bölünmez.</summary>
    public sealed class InstallmentPartKind : EnemyKind
    {
        public override string Name => "TAKSİT PARÇASI";
        public override Color FullColor => new Color(0.95f, 0.72f, 0.85f);
        public override float SpeedMultiplier => 1.2f;
    }

    /// <summary>
    /// ZAMANAŞIMI — yaşadıkça güçlenir. Ötelemek onu besliyor.
    ///
    /// Değiştirdiği karar: SIRA ve SÜRE. Diğer türler "önce hangisi" diye sordururken
    /// bu "ne kadar bekleyebilirim" diye sordurur. Kite'lamak, kaçmak, dövüşü uzatmak —
    /// hepsi onun lehine işliyor. Odayı yavaş temizlemenin bedeli.
    ///
    /// Başlangıçta zayıf, ki ceza gecikmeden değil ERTELEMEDEN gelsin.
    /// </summary>
    public sealed class LapseKind : EnemyKind
    {
        public override string Name => "ZAMANAŞIMI";
        public override Color FullColor => new Color(0.6f, 0.85f, 0.3f);
        public override float GrowthPerSecond => 0.09f;     // saniyede %9
        public override float PoolMultiplier => 0.9f;
        public override float SpeedMultiplier => 0.95f;
    }
}
