using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Oyunun bütün his ve denge değerleri. Sohbette karara bağlanmaz, oynanarak bulunur.
    /// Play modundayken değiştirilebilir ve değerler kalıcıdır (ScriptableObject).
    ///
    /// ⚠️ Buradaki varsayılanlar yalnızca `Tuning.asset`'te HENÜZ OLMAYAN yeni alanlar
    /// için geçerlidir. Var olan bir değeri değiştirmek istiyorsan Inspector'dan yap;
    /// .cs'i düzenlemek işe yaramaz, asset onu ezer. Bkz. `Tek Can → Ayar kaynağı denetimi`.
    /// </summary>
    [CreateAssetMenu(fileName = "Tuning", menuName = "Tek Can/Tuning")]
    public class Tuning : ScriptableObject
    {
        [Header("── SAAT (oyuncunun tek kaynağı) ──")]
        [Tooltip("Koşuya kaç saniyeyle başlanır")]
        public float startSeconds = 60f;
        [Tooltip("Dövüşteyken saniyede kaç saniye iner")]
        public float drainInCombat = 1f;
        [Tooltip("Dövüş dışında saniyede kaç saniye iner — beklemek cezalandırılır")]
        public float drainOutOfCombat = 2.5f;
        [Tooltip("Bu yarıçapta canlı düşman yoksa 'dövüş dışı' sayılır")]
        public float combatRadius = 12f;

        [Header("── HASAT (oyunun en kritik sayısı) ──")]
        [Tooltip("Verilen hasarın ne kadarı saate yazılır. Yüksekse dövüş kendini besler, saat anlamsızlaşır.")]
        [Range(0f, 1f)] public float harvestRate = 0.35f;
        [Tooltip("Koşu içi yükseltmelerin çıkabileceği tavan — %100'e yaklaşırsa oyun bozulur")]
        [Range(0f, 1f)] public float harvestRateCap = 0.70f;

        [Header("── OYUNCU: HAREKET ──")]
        public float moveSpeed = 6f;
        public float turnSpeed = 18f;

        [Header("── OYUNCU: DASH ──")]
        public float dashDistance = 2.5f;
        public float dashDuration = 0.15f;
        public float dashCooldown = 0.5f;
        [Tooltip("Dash başından itibaren dokunulmazlık süresi")]
        public float iFrameDuration = 0.2f;

        [Header("── OYUNCU: SALDIRI ──")]
        [Tooltip("Kısa tutulacak — uzun taahhüt saldırganlığı cezalandırır")]
        public float attackDuration = 0.30f;
        [Tooltip("Vuruşun isabet ettiği an (saldırı süresinin oranı)")]
        [Range(0f, 1f)] public float attackHitMoment = 0.12f;
        public float attackDamage = 3.5f;
        public float attackRange = 2.2f;
        [Tooltip("Öndeki koninin toplam açısı (derece)")]
        public float attackAngle = 100f;
        [Tooltip("İsabette zamanın donma süresi — tokluk hissi buradan gelir")]
        public float hitStopDuration = 0.10f;
        [Tooltip("İsabette kamera sarsıntısı şiddeti")]
        public float hitShake = 0.22f;
        [Tooltip("Darbe yediğinde kamera sarsıntısı şiddeti")]
        public float hurtShake = 0.30f;
        [Tooltip("İsabet eden düşmanın beyaz parlama süresi")]
        public float hitFlashDuration = 0.20f;
        [Tooltip("Savurma yayının ekranda kalma süresi — vuruşu GÖRMEK için, isabet olmasa da çıkar")]
        public float swingArcLife = 0.20f;

        [Header("── OYUNCU: HASAR TEPKİSİ ──")]
        public float playerHurtFlashDuration = 0.18f;
        [Tooltip("Darbe yiyince ne kadar geri itilirsin — 'yedim' anı okunmalı")]
        public float playerKnockback = 1.2f;

        [Header("── DÜŞMAN: SALDIRI EVRELERİ ──")]
        [Tooltip("HAZIRLIK: vuruştan önceki NİŞAN süresi — düşman bu sürede sana dönüyor. " +
                 "Üstüne tepki payı (enemyReactionWindow) ekleniyor: o sürede yön kilitli.")]
        public float enemyWindupDuration = 0.20f;
        [Tooltip("VURUŞ: hasarın indiği kısa evre. Yön tepki payı başında KİLİTLENDİ — o andan sonrası kaçılabilir.")]
        public float enemyStrikeDuration = 0.10f;
        [Tooltip("TOPARLANMA: vuruştan sonra açık kaldığı süre")]
        public float enemyRecoverDuration = 0.20f;
        [Tooltip("Boş düşmanın hazırlığı bu kat daha yavaş — zayıflığı görünür olsun")]
        public float enemyHollowWindupMultiplier = 1.6f;
        [Tooltip("Düşman vuruşunun koni açısı (derece)")]
        public float enemyAttackAngle = 90f;
        [Tooltip("Vuruş anında öne atılma mesafesi")]
        public float enemyLungeDistance = 0.8f;
        [Tooltip("TEPKİ PAYI: hazırlığın SON bu kadar saniyesinde düşman sana dönmeyi bırakır, " +
                 "uyarı yayı donar. Hazırlığa EKLENİYOR. ⚠️ Enes (2026-09-30): 'düşmanlar " +
                 "hareketlerimize anlık tepki veriyor.' Önceden yön ancak vuruş anında kilitleniyordu.")]
        public float enemyReactionWindow = 0.25f;
        [Tooltip("Saldırı sırasında hareket hızı çarpanı")]
        [Range(0f, 1f)] public float attackMoveDamping = 0f;

        [Header("── DÜŞMAN ──")]
        [Tooltip("Bedende kaç saniye var — hem canı hem ödemesi")]
        public float enemyTimePool = 10f;
        [Tooltip("Kendi kendine saniyede ne kadar akar. Dokunmazsan boşalır ve bedavaya dövüşürsün.")]
        public float enemyDecayPerSecond = 0.4f;
        public float enemyFullSpeed = 3.5f;
        public float enemyHollowSpeed = 1.5f;
        [Tooltip("DOLU düşmanın vuruşu kaç saniye götürür")]
        public float enemyFullHitCost = 4f;
        [Tooltip("BOŞ düşmanın vuruşu kaç saniye götürür")]
        public float enemyHollowHitCost = 1.5f;
        public float enemyAttackRange = 1.8f;
        public float enemyAttackCooldown = 1.5f;

        [Header("── SUNAK / EKONOMİ (Faz 2) ──")]
        [Tooltip("İlk alımın saniye fiyatı")]
        public float altarBasePrice = 6f;
        [Tooltip("Her alımda fiyat bu kat artar — bileşik yatırım kararı")]
        public float altarPriceGrowth = 1.6f;
        [Tooltip("Bir sunaktan en fazla kaç alım")]
        public int altarMaxPurchases = 3;
        [Tooltip("Kullanmak için sunağa bu kadar yaklaşman gerek")]
        public float altarUseRange = 2.5f;
        [Tooltip("HASAT sunağı: her alımda hasat oranı bu kadar artar")]
        public float altarHarvestStep = 0.08f;
        [Tooltip("KESKİNLİK sunağı: her alımda saldırı hasarı bu kadar artar")]
        public float altarDamageStep = 1.2f;
        [Tooltip("ÇEVİKLİK sunağı: her alımda dash bekleme süresi bu kadar kısalır")]
        public float altarDashStep = 0.08f;
        [Tooltip("AZALAN GETİRİ (Faz 27): aynı türden her alım bir öncekinin bu katı kadar verir. " +
                 "0.8 → hasar en fazla +adım/(1−0.8) = +5 adım (1.2 × 5 = +6). 1 = azalma yok. " +
                 "Kartopu kırılsın diye: bot 43 alımla hasarı ×5.8 yapıp oyunu 9 dk'da bitirdi.")]
        public float altarStepDecay = 0.9f;

        [Header("── SİLAHLAR (Faz 3) ──")]
        [Tooltip("Tek hedefli silahların koni açısı — dar, çünkü tek düşman seçiyor")]
        public float singleTargetAngle = 45f;
        [Tooltip("FAİZLİ: her savurmanın peşin bedeli (saniye). Iska pahalı olsun diye var.")]
        public float faizliSwingCost = 0.5f;
        [Tooltip("FAİZLİ: hasar çarpanı. Tek hedefli silahın 'tek ama sert' iddiası.")]
        public float faizliDamageMult = 1.75f;
        [Tooltip("FAİZLİ: vurduğunu ne kadar geri savurur (birim/sn). " +
                 "⚠️ Ağırlık hissinin ASIL kaynağı — hit-stop ve sarsıntı tek başına yetmedi.")]
        public float faizliKnockback = 9f;
        // VADELİ kaldırıldı (2026-09-02): FAİZLİ ile aynı soruyu soruyordu ve
        // `Denge simülasyonu`nda HER düşman sayısında geride kalıyordu (+2,8 / +5,9).
        // `vadeliDamageMult` ve `vadeliCost` asset'te ölü anahtar olarak kalıyor —
        // Unity tanımadığı anahtarı yok sayıyor, temizlemek için asset'i ezmek
        // gerekirdi ve o da Inspector'daki bütün his ayarlarını silerdi.

        [Header("── SAĞ TIK ÖZEL SALDIRILARI (Faz 16) ──")]
        // ⚠️ Bekleme süreleri BİLEREK farklı (Enes: "silahtan silaha süresi değişir").
        // Aynı olsaydı özel saldırı silahın değil oyuncunun ritmi olurdu.
        [Tooltip("KONİ · SÜPÜRME — çevrendeki herkese")]
        public float specialCooldownCone = 7f;
        [Tooltip("FAİZLİ · İCRA — ağır peşin bedel, öldürünce 3 kat iade")]
        public float specialCooldownFaizli = 9f;
        [Tooltip("HACİZ · UZUN VADE — hasar uzaklıkla artar")]
        public float specialCooldownHaciz = 6f;
        [Tooltip("İPOTEK · İPOTEK YAĞMURU — alandaki düşmanlara peş peşe ipotek (HACİZ İHBARI artık dash-atak)")]
        public float specialCooldownIpotek = 8f;
        [Tooltip("İFLAS · TASFİYE — arka arkaya seri vuruş")]
        public float specialCooldownIflas = 10f;
        [Tooltip("İFLAS · TASFİYE kaç vuruş yapıyor. Silahın kimliği HIZ, " +
                 "o yüzden özeli de seri (Enes kararı).")]
        public int iflasFlurryHits = 7;
        [Tooltip("İFLAS · TASFİYE vuruşları arası saniye")]
        public float iflasFlurryInterval = 0.075f;

        [Tooltip("İPOTEK: yer vuruşunun olgunlaşma süresi (saniye). Kaçma penceresi bu.")]
        public float ipotekDelay = 0.85f;
        [Tooltip("İPOTEK: yer vuruşunun yarıçapı")]
        public float ipotekRadius = 2.4f;
        [Tooltip("İPOTEK: hasar çarpanı — gecikmeli ve kaçılabilir olduğu için yüksek")]
        public float ipotekDamageMult = 1.6f;

        // ⚠️ Bu iki eşik `startSeconds`e (60) GÖRE seçildi. İlk denemede 120/25
        // yazmıştım — koşu 60'ta başladığı için silah OYUNUN TAMAMINDA güçlü
        // moddaydı ve "güvendeyken zayıf" kimliği hiç ortaya çıkmıyordu.
        // Denge simülasyonu ×1,77 gösterince yakalandı. Öz-denetim artık
        // koşunun ZAYIF BANTTA başladığını gerçek asset'ten doğruluyor.
        [Tooltip("İFLAS: bu saatin ÜSTÜNDE en zayıf hâlinde (saniye). " +
                 "startSeconds'ten BÜYÜK olmamalı, yoksa silah baştan güçlü başlar.")]
        public float iflasSafeSeconds = 80f;
        [Tooltip("İFLAS: bu saatin ALTINDA en güçlü hâlinde (saniye)")]
        public float iflasPanicSeconds = 15f;
        [Tooltip("İFLAS: dipteki hasar çarpanı. Güvendeki çarpan 0.7 (silahın kimliği).")]
        public float iflasMaxDamageMult = 2.4f;

        [Header("── DASH-ATAK (Faz 2) ──")]
        [Tooltip("Dash sırasında saldırıya basınca dash sonunda inen vuruşun hasar çarpanı")]
        public float dashAttackDamageMult = 1.6f;
        [Tooltip("Dash-atak konisi — normal saldırıdan geniş, çünkü hareket hâlindesin")]
        public float dashAttackAngle = 160f;
        [Tooltip("Dash-atak menzil çarpanı")]
        public float dashAttackRangeMult = 1.15f;

        [Header("── YAPI: bölge → ara bölge → oda (Faz 28) ──")]
        [Tooltip("Bölge başına ARA BÖLGE — her biri bir MİNİ BOSS'la biter, ardından sunak (kontrol noktası). " +
                 "Enes 2026-10-12: tek koşu, hikâye 8 saat.")]
        public int subRegionsPerRegion = 5;
        [Tooltip("Ara bölge başına DÖVÜŞ odası (mini boss ve sunak odası hariç)")]
        public int roomsPerSubRegion = 7;
        [Tooltip("Dövüş odası kaç DALGA — dalga temizlenince sıradaki doğar, sonuncudan sonra kapı açılır")]
        public int roomWaves = 5;
        [Tooltip("Her ara bölgede düşman havuzu bu kat büyür (ara bölge İÇİNDE ayrıca roomPoolGrowth)")]
        public float subRegionPoolGrowth = 1.12f;
        [Tooltip("Mini boss havuzu = bölgenin TABAN boss havuzunun bu payı")]
        public float miniBossPoolShare = 5f;
        [Tooltip("Ana boss havuzu ek çarpanı — tempo: ana boss 6–8 dk")]
        public float mainBossPoolScale = 12f;
        [Tooltip("Boss ve mini boss odasında saat akışı çarpanı — uzun dövüşte saat (can) erimesin")]
        public float bossRoomDrainMultiplier = 0.35f;
        [Tooltip("Mini boss'un GERİ GELMESİ: ölümlerde kaybettiğin saniyeler, mini boss havuzunun bu katına ulaşınca")]
        public float miniBossRecoveryShare = 1.5f;

        // Enes (2026-10-14): "başlarda kolay ve oyuncu dostu, ilerledikçe zorlaşan ve bu zorluğu
        // bufflar ile kapatan… iyi oyuncu bile absürt saatlere ulaşmamalı." Sunak için B seçildi.
        [Header("── EKONOMİ: doyma · sunak yüzdesi · ilerleme (Faz 28) ──")]
        [Tooltip("HASAT DOYMASI: saat bu sayının üstündeyken hasat ×(tavan / saat) — 1. bölgede")]
        public float harvestSoftCap = 250f;
        [Tooltip("Doyma tavanı her bölgede bu kat büyür (boss'lar büyüyor, tampon da büyümeli)")]
        public float harvestSoftCapGrowth = 1.35f;
        [Tooltip("Sunak fiyatı en az saatin bu payı — biriktiren daha çok öder")]
        public float altarPricePercent = 0.10f;
        [Tooltip("İLERLEME: düşman vuruşunun bedeli her ara bölgede (bölgeler boyunca sayılır) bu kat artar")]
        public float hitCostProgressGrowth = 1.09f;

        // Enes (eski Faz 20 listesi): "İPOTEK: dash-atak = şimdiki sağ tık, yeni sağ tık = büyük alan
        // içinde düşmanların üstüne birkaç saniye peş peşe ipotek" · "FAİZLİ İCRA yeniden: bedel saatin
        // yüzdesi, hasar ödenen bedele bağlı… eşiğin üstündeysen zaman vermez".
        [Header("── Faz 29: İPOTEK YAĞMURU · İCRA ──")]
        [Tooltip("İPOTEK YAĞMURU: kaç ipotek yağar")]
        public int ipotekRainHits = 6;
        [Tooltip("İPOTEK YAĞMURU: iki ipotek arası (sn)")]
        public float ipotekRainInterval = 0.4f;
        [Tooltip("İPOTEK YAĞMURU: alan = İPOTEK menzili × bu")]
        public float ipotekRainRangeMul = 1.6f;
        [Tooltip("İCRA bedeli = saatin bu payı (en az 3 savurma bedeli)")]
        public float icraCostPercent = 0.12f;
        [Tooltip("İCRA: ödenen her saniye için ek hasar")]
        public float icraDamagePerCost = 0.5f;
        [Tooltip("İCRA öldürürse bedelin bu katı geri — yalnızca saat doyma tavanının ALTINDAYSA")]
        public float icraKillRefund = 1.5f;

        [Header("── Faz 29: yeni sunaklar · silah sunağı ──")]
        [Tooltip("MUAFİYET (ara bölge sunağı): ilk alım düşman vuruşunun bedelini bu kadar düşürür (azalan getiri, tavan %50)")]
        public float altarInsuranceStep = 0.08f;
        [Tooltip("SABIR (ara bölge sunağı): ilk alım sağ tık beklemesini bu kadar kısaltır (azalan getiri, tavan %50)")]
        public float altarPatienceStep = 0.10f;
        [Tooltip("SİLAH SUNAĞI (ana boss sonrası): bir boss odasında kaç yükseltme alınabilir")]
        public int weaponAltarPurchases = 2;
        [Tooltip("SİLAH SUNAĞI taban fiyatı = sunak tabanı × bu (× artış^alınan)")]
        public float weaponAltarBaseMul = 2f;
        [Tooltip("SİLAH SUNAĞI fiyatı en az saatin bu payı")]
        public float weaponAltarPricePercent = 0.15f;

        [Header("── Faz 30: kadro · combo ──")]
        [Tooltip("COMBO: bir sonraki silahla vurmak için süre (sn) — hızlı geçiş")]
        public float comboWindow = 0.8f;
        [Tooltip("COMBO: aynı combo bu kadar sn tekrar etmez (istismar olmasın)")]
        public float comboCooldown = 4f;
        [Tooltip("Sunakta silah açma taban fiyatı (sn)")]
        public float weaponUnlockBase = 30f;
        [Tooltip("Sunakta silah açma: en az saatin bu payı (keşifte kaçırılmışsa ×2)")]
        public float weaponUnlockPercent = 0.15f;

        /// <summary>Bir ara bölgenin dövüş odası sayısı (en az 1).</summary>
        public int DövüşOdası => Mathf.Max(1, roomsPerSubRegion);
        /// <summary>Bir ara bölgenin oda sayısı: dövüş odaları + mini boss + sunak.</summary>
        public int BlokBoyu => DövüşOdası + 2;
        /// <summary>Bölgedeki oda sayısı — sonuncusu ANA BOSS.</summary>
        public int RoomsInRegion => Mathf.Max(1, subRegionsPerRegion) * BlokBoyu + 1;

        [Header("── ODALAR (Faz 8) ──")]
        [Tooltip("İlk odadaki düşman sayısı")]
        public float roomBaseCount = 3f;
        [Tooltip("Her odada kaç düşman eklenir")]
        public float roomCountGrowth = 1.5f;
        [Tooltip("Her odada düşman zaman havuzu bu kat büyür — odalar sertleşir")]
        public float roomPoolGrowth = 1.10f;

        [Header("── DÜŞMAN TÜRLERİ (Faz 13) ──")]
        [Tooltip("SİGORTA: bu yarıçaptaki düşmanların hasadını kısar")]
        public float insurerRadius = 5f;
        [Tooltip("SİGORTA: koruduğu düşmanlardan alınan hasadın çarpanı")]
        public float insurerHarvestMultiplier = 0.25f;
        [Tooltip("MÜBAŞİR: kaç saniyede bir uzaktan tebligat bırakır")]
        public float bailiffMarkInterval = 2.6f;
        [Tooltip("MÜBAŞİR: oyuncuya bu mesafeden fazla yaklaşmaz")]
        public float bailiffKeepDistance = 7f;
        [Tooltip("MÜBAŞİR: tebligatın yarıçapı — SİMSAR'ınkinden küçük")]
        public float bailiffMarkRadius = 2.1f;
        [Tooltip("ZAMANAŞIMI: büyüme tavanı. Tavansız bırakılırsa tek düşman koşuyu bitirir.")]
        public float lapseGrowthCap = 2.5f;

        [Tooltip("HACİZ: bu mesafeden yakındakine VURAMAZ (ölü bölge). " +
                 "Dash-atağa uygulanmaz — dibine girene cevap odur.")]
        public float hacizMinRange = 3.2f;

        [Tooltip("SON BOSS faz 3: çağırdığı önceki boss'ların havuz çarpanı. " +
                 "⚠️ Enes: 'yendiğimiz halinden daha güçlü olmalılar.' Güç HAVUZDAN " +
                 "değil FAZ 2'DE BAŞLAMALARINDAN geliyor — ilk saniyeden en geniş " +
                 "dağarcıklarıyla dövüşüyorlar. Havuz kısık kalıyor çünkü SIRAYLA " +
                 "geliyorlar; dördü de tam havuzla gelseydi final bir yıpratma olurdu.")]
        public float rivalPoolMultiplier = 0.7f;

        [Tooltip("NEMESİS: boss'un senden aldığı zamanın ne kadarı havuzuna ekleniyor. " +
                 "1.0 = tamamı. ⚠️ Tam ödetmek ölüm sarmalı yapabilir: her ölüm boss'u " +
                 "biraz daha yenilmez kılar. Pay, hikâyeyi bozmadan sarmalı kesiyor.")]
        public float nemesisPoolShare = 0.5f;
        [Tooltip("NEMESİS: borç havuzun bu katını geçemez — tavan. " +
                 "⚠️ Tavansız bırakılsaydı yeterince ölen oyuncu için boss matematiksel " +
                 "olarak yenilemez olurdu ve kalıcı ölüm bir CEZA değil bir DUVAR olurdu.")]
        public float nemesisMaxShare = 1.5f;

        [Header("── DÜŞMAN RÜTBESİ (Faz 17) ──")]
        // ⚠️ ZIRHLI = MİKTAR ekseni, İRİ = KİMLİK ekseni. İlk tasarımda ikisi de
        // havuz ekseninde durup aynı hissettiriyordu (Enes: "ayrışmıyor").
        [Tooltip("ZIRHLI: havuz çarpanı. ⚠️ 2.0 idi; Faz 25'te zırh GERÇEK oldu " +
                 "(armoredDamageTaken), havuz normale döndü — yoksa ikisi üst üste biner.")]
        public float armoredPoolMultiplier = 1.0f;
        [Tooltip("ZIRHLI: vuruş başına aldığı hasarın oranı. Hasat hasarla orantılı, " +
                 "yani vuruş başına kazanç da bu oranda (Enes: 'gelen can da orantılı az').")]
        public float armoredDamageTaken = 0.5f;
        [Tooltip("ZIRHLI: kaç yuvada bir. 3 = her üçüncü düşman. 0 = kapalı.")]
        public int armoredEverySlot = 3;

        [Tooltip("İRİ: havuz çarpanı. Mini boss, o yüzden ZIRHLI'dan da kalın.")]
        public float hugePoolMultiplier = 4.0f;
        [Tooltip("İRİ: vuruşunun bedeli normalin kaç katı (Enes: 'daha çok hasar').")]
        public float hugeHitCostMultiplier = 1.6f;
        [Tooltip("İRİ: TÜRÜN İMZASININ çarpanı — onu 'büyük zırhlı'dan ayıran tek şey bu. " +
                 "SIZINTI'nın emişi, SİGORTA'nın yarıçapı, MÜBAŞİR'in sıklığı, " +
                 "ZAMANAŞIMI'nın büyüme hızı hep bununla katlanıyor.")]
        public float hugeTraitMultiplier = 2.5f;
        [Tooltip("İRİ: kaç odada bir mini boss çıkar. 4 = her dördüncü oda. " +
                 "O odada YALNIZCA BİR tane olur. 0 = kapalı.")]
        public int hugeRoomInterval = 4;

        [Header("── BOSS TÜRLERİ (Faz 12) ──")]
        [Tooltip("SİMSAR: durduğun yere bıraktığı işaretin yarıçapı")]
        public float groundMarkRadius = 2.6f;
        [Tooltip("SİMSAR: işaret kaç saniye sonra patlıyor — çıkmaya yetmeli")]
        public float groundMarkDelay = 1.3f;
        [Tooltip("SİMSAR: işaretin bedeli, normal darbeye çarpan")]
        public float groundMarkCostMultiplier = 0.9f;
        [Tooltip("MURABAHACI faz 2: vurmadığın her saniye havuzunu bu kadar geri sarar. " +
                 "Kredi doldurmak hasat vermiyor — sadece dövüşü uzatıyor.")]
        public float usurerRegenPerSecond = 6f;

        [Header("── BÖLGELER (Faz 10) ──")]
        [Tooltip("Perdedeki bölge sayısı. Tavan Region.All uzunluğu. " +
                 "5 = bütün kurallar oyunda (test için); koşu uzunluğunu ayarlarken düşür.")]
        public int regionsPerAct = 5;
        [Tooltip("Her bölgede boss havuzu bu kat büyür — zincir tırmanmalı")]
        public float regionBossGrowth = 1.35f;

        [Header("── DİRİLİŞ / CHECKPOINT (Faz 9) ──")]
        [Tooltip("ÖLÜM ALDATMA: checkpoint'teki zamanın bu kadarıyla yerinde kalkarsın")]
        public float defianceFraction = 0.35f;
        [Tooltip("GEÇİCİ DÖNÜŞ: bu kadar saniyen olur. Odayı temizlersen kurtulursun, " +
                 "temizleyemezsen checkpoint'e düşersin.")]
        public float lastStandSeconds = 10f;
        [Tooltip("Kırık diriliş eşyasını tamir etmenin SANİYE bedeli. " +
                 "⚠️ Ayrı malzeme YOK — tamir zamanla yapılır, yoksa ikinci para birimi doğar.")]
        public float revivalRepairPrice = 45f;

        [Tooltip("ÖLÜM ALDATMA en az bu kadar saniye verir. ⚠️ Oranla hesaplanan süre " +
                 "düşük checkpoint'te 4-5 saniyeye iniyordu: eşya yanıyor, oyuncu hiçbir " +
                 "şey olmadı sanıyor ve sıradaki eşya devreye giriyor. Enes bunu 'ölüm " +
                 "aldatmadan önce geçici dönüş çalıştı' diye bildirdi — sıra değil SÜRE hatası.")]
        public float defianceMinSeconds = 12f;

        [Tooltip("ÇAĞRI (checkpoint'e dönüş) en az bu kadar saniyeyle döndürür. ⚠️ K2 " +
                 "(2026-09-30): checkpoint odasına 4 sn ile girince her ÇAĞRI seni 4 sn ile " +
                 "düşmanların yanına atıyordu, eşyalar saniyeler içinde eriyordu.")]
        public float recallMinSeconds = 20f;

        [Tooltip("TEST: başlangıçtaki ÖLÜM ALDATMA sayısı. Gerçek oyunda eşyalar hikâyeden gelecek.")]
        public int testDefianceCount = 1;
        [Tooltip("TEST: başlangıçtaki GEÇİCİ DÖNÜŞ sayısı")]
        public int testLastStandCount = 1;
        [Tooltip("TEST: başlangıçtaki ÇAĞRI sayısı")]
        public int testRecallCount = 1;

        [Header("── DÜŞMAN TÜRLERİ (Faz 5) ──")]
        [Tooltip("SIZINTI: yaşadığı sürece saatinden saniyede bu kadar emer")]
        public float leakAuraDrain = 0.55f;
        [Tooltip("KASA: öldüğünde havuzunun hasat oranı × bu kadarını tek seferde öder")]
        public float vaultKillBonus = 1.8f;
        [Tooltip("KAÇAK: kaçarken normal hızının bu katı. Oyuncudan YAVAŞ kalmalı, yoksa hiç yakalanamaz.")]
        public float runnerFleeSpeedMultiplier = 1.3f;
        [Tooltip("KAÇAK: merkeze bu uzaklığa varırsa kaybolur ve zamanını götürür")]
        public float runnerEscapeRadius = 17f;
        [Tooltip("İlk kaç odada yalnızca temel düşman olsun — oyuncu önce oyunu öğrensin")]
        public int plainRoomCount = 1;

        [Header("── BOSS ──")]
        [Tooltip("Boss'un zaman havuzu normal düşmanın kaç katı")]
        public float bossPoolMultiplier = 20f;
        [Tooltip("Boss yaşadığı sürece saatini saniyede bu kadar emer — bekleyerek yenemezsin")]
        public float bossAuraDrain = 1.1f;
        [Tooltip("Boss'un kendi kendine akışı bu kat yavaş — boşalmasını bekleyemezsin")]
        public float bossDecayMultiplier = 0.15f;
        [Tooltip("Boss'un vuruşu normal düşmanın kaç katı saniye götürür")]
        public float bossHitCostMultiplier = 2.2f;
        [Tooltip("Boss bu kat hızlı hareket eder — kaçmak yetmiyor olmalı")]
        public float bossSpeedMultiplier = 1.25f;
        [Tooltip("Boss'un hazırlık süresi bu kat — 1'in altı daha az uyarı demek")]
        public float bossWindupMultiplier = 0.85f;
        [Tooltip("Boss'un vuruş konisi bu kat geniş — yandan kaçmak zorlaşır")]
        public float bossAttackAngleMultiplier = 1.5f;
        [Tooltip("TEPKİ PAYI (boss): hazırlığın son bu kadar saniyesinde yön kilitli. " +
                 "Çarpanlı hazırlıklara EKLENİYOR; mutlak süreliler (simit, açıklık…) zaten uzun. " +
                 "⚠️ 0.3'te 30 hızlı koninin 8'inden koşarak çıkılamıyordu (SAYIM, PROTESTO…): " +
                 "geniş ya da uzun saldırıda 0.25 sn tepkiden sonra kalan süre yetmiyordu. " +
                 "Faz25SelfCheck ölçüyor; düşürürsen hangilerinin kaçılmaz olduğunu söyler.")]
        public float bossReactionWindow = 0.45f;
        [Tooltip("Boss sıradaki saldırısının menziline giremezse bu kadar bekler, sonra " +
                 "aynı sıradan o an yetişen saldırıyı yapar. Kaçarak saldırıları iptal etmek olmasın.")]
        public float bossAttackPatience = 1.5f;
        [Tooltip("Her kaçıncı saldırı TÜM HARİTA sırasından (mesafeden bağımsız). " +
                 "4 = üç yakın/uzak, bir harita. 0 = harita saldırısı yok. (Enes, Faz 26)")]
        public int bossMapAttackEvery = 4;
        [Tooltip("ISRAR: aynı sıradan (hep yakın ya da hep uzak) bu kadar saldırıdan SONRA bedel artmaya başlar")]
        public int bossStreakThreshold = 3;
        [Tooltip("ISRAR: eşikten sonraki her saldırıda bedel bu oranda BİNEREK artar (0.25 → ×1.25, 1.56, " +
                 "1.95, 2.44, 3.05, 3.81, 4.77, 5). Enes: \"1.3, 1.5, 1.8 gibi artarak, ×5'e kadar\" (2026-10-10).")]
        public float bossStreakStep = 0.25f;
        [Tooltip("ISRAR: bedel çarpanının tavanı (Enes: şimdilik ×5)")]
        public float bossStreakCap = 5f;
        [Tooltip("Boss'a vurmak normal düşmanın kaç katı zaman kazandırır. DÜŞÜK olmalı: boss uzun bir dövüş, seni beslememeli.")]
        public float bossHarvestMultiplier = 0.4f;

        // Yere vuruşun ayarları buradaydı. Faz 12'de her boss KENDİ saldırı dağarcığını
        // kazandı (`BossKind.Attacks`); halka saldırıları (TAHSİL, İCRA, ORTAK BORÇ,
        // MÜZAYEDE) yarıçapını, hazırlığını ve bedelini kendi verisinden alıyor.
        // Bu alanlar hiçbir şeyi sürmüyordu; Inspector'da durup "çevirdim ama değişmedi"
        // dedirtmesinler diye silindiler.

        [Header("── ARENA (greybox test) ──")]
        [Tooltip("YALNIZCA ÖZ-DENETİM İÇİN. Oyunda düşman sayısını oda sistemi belirliyor " +
                 "(roomBaseCount / roomCountGrowth); bu alan sadece testlerin sabit bir halka " +
                 "kurmasına yarıyor. Oyun dengesini buradan ayarlama.")]
        public int enemyCount = 5;
        public float arenaRadius = 14f;

        [Header("── KAMERA (izometrik) ──")]
        public float camYaw = 45f;
        public float camPitch = 50f;
        public float camOrthoSize = 9f;
        public float camFollowLerp = 8f;
    }
}
