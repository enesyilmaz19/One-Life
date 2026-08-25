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
        public float attackDuration = 0.35f;
        [Tooltip("Vuruşun isabet ettiği an (saldırı süresinin oranı)")]
        [Range(0f, 1f)] public float attackHitMoment = 0.4f;
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
        [Tooltip("HAZIRLIK: vuruştan önceki uyarı süresi. Bu olmadan dodge tahmindir.")]
        public float enemyWindupDuration = 0.20f;
        [Tooltip("VURUŞ: hasarın indiği kısa evre. Yön hazırlık bitince KİLİTLENİR — sonrası kaçılabilir.")]
        public float enemyStrikeDuration = 0.10f;
        [Tooltip("TOPARLANMA: vuruştan sonra açık kaldığı süre")]
        public float enemyRecoverDuration = 0.20f;
        [Tooltip("Boş düşmanın hazırlığı bu kat daha yavaş — zayıflığı görünür olsun")]
        public float enemyHollowWindupMultiplier = 1.6f;
        [Tooltip("Düşman vuruşunun koni açısı (derece)")]
        public float enemyAttackAngle = 90f;
        [Tooltip("Vuruş anında öne atılma mesafesi")]
        public float enemyLungeDistance = 0.8f;
        [Tooltip("Saldırı sırasında hareket hızı çarpanı")]
        [Range(0f, 1f)] public float attackMoveDamping = 0.25f;

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

        [Header("── SİLAHLAR (Faz 3) ──")]
        [Tooltip("Tek hedefli silahların koni açısı — dar, çünkü tek düşman seçiyor")]
        public float singleTargetAngle = 45f;
        [Tooltip("FAİZLİ: her savurmanın peşin bedeli (saniye). Iska pahalı olsun diye var.")]
        public float faizliSwingCost = 0.5f;
        [Tooltip("VADELİ: hasar çarpanı")]
        public float vadeliDamageMult = 2.4f;
        [Tooltip("VADELİ: vuruş İNDİKTEN sonra kesilen bedel (saniye)")]
        public float vadeliCost = 1.4f;

        [Header("── DASH-ATAK (Faz 2) ──")]
        [Tooltip("Dash sırasında saldırıya basınca dash sonunda inen vuruşun hasar çarpanı")]
        public float dashAttackDamageMult = 1.6f;
        [Tooltip("Dash-atak konisi — normal saldırıdan geniş, çünkü hareket hâlindesin")]
        public float dashAttackAngle = 160f;
        [Tooltip("Dash-atak menzil çarpanı")]
        public float dashAttackRangeMult = 1.15f;

        [Header("── DALGALAR / KOŞU YAPISI (Faz 4) ──")]
        [Tooltip("Kaçıncı dalgada boss çıkar — koşu o dalga yenilince KAZANILIR")]
        public int waveCount = 6;
        [Tooltip("İlk dalgadaki düşman sayısı")]
        public float waveBaseCount = 3f;
        [Tooltip("Her dalgada kaç düşman eklenir")]
        public float waveCountGrowth = 1.5f;
        [Tooltip("Her dalgada düşman zaman havuzu bu kat büyür — dalgalar sertleşir")]
        public float wavePoolGrowth = 1.18f;

        [Header("── BOSS ──")]
        [Tooltip("Boss'un zaman havuzu normal düşmanın kaç katı")]
        public float bossPoolMultiplier = 9f;
        [Tooltip("Boss yaşadığı sürece saatini saniyede bu kadar emer — bekleyerek yenemezsin")]
        public float bossAuraDrain = 0.8f;
        [Tooltip("Boss'un kendi kendine akışı bu kat yavaş — boşalmasını bekleyemezsin")]
        public float bossDecayMultiplier = 0.15f;

        [Header("── ARENA (greybox test) ──")]
        [Tooltip("Faz 4 öncesi sabit doğum için kullanılıyordu; dalga sistemi devrede artık kullanılmıyor")]
        public int enemyCount = 5;
        public float arenaRadius = 14f;

        [Header("── KAMERA (izometrik) ──")]
        public float camYaw = 45f;
        public float camPitch = 50f;
        public float camOrthoSize = 9f;
        public float camFollowLerp = 8f;
    }
}
