using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Faz 1'in bütün his değerleri. Sohbette karara bağlanmaz, oynanarak bulunur.
    /// Play modundayken değiştirilebilir ve değerler kalıcıdır (ScriptableObject).
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
        [Range(0f, 1f)] public float harvestRate = 0.40f;
        [Tooltip("Koşu içi yükseltmelerin çıkabileceği tavan — %100'e yaklaşırsa oyun bozulur")]
        [Range(0f, 1f)] public float harvestRateCap = 0.70f;

        [Header("── OYUNCU: HAREKET ──")]
        public float moveSpeed = 6f;
        public float turnSpeed = 18f;

        [Header("── OYUNCU: DASH ──")]
        public float dashDistance = 4f;
        public float dashDuration = 0.15f;
        public float dashCooldown = 0.5f;
        [Tooltip("Dash başından itibaren dokunulmazlık süresi")]
        public float iFrameDuration = 0.2f;

        [Header("── OYUNCU: SALDIRI ──")]
        [Tooltip("Kısa tutulacak — uzun taahhüt saldırganlığı cezalandırır")]
        public float attackDuration = 0.25f;
        [Tooltip("Vuruşun isabet ettiği an (saldırı süresinin oranı)")]
        [Range(0f, 1f)] public float attackHitMoment = 0.4f;
        public float attackDamage = 10f;
        public float attackRange = 2.2f;
        [Tooltip("Öndeki koninin toplam açısı (derece)")]
        public float attackAngle = 100f;
        [Tooltip("İsabette zamanın donma süresi — tokluk hissi buradan gelir")]
        public float hitStopDuration = 0.06f;
        [Tooltip("İsabette kamera sarsıntısı şiddeti")]
        public float hitShake = 0.14f;
        [Tooltip("Darbe yediğinde kamera sarsıntısı şiddeti")]
        public float hurtShake = 0.30f;
        [Tooltip("İsabet eden düşmanın beyaz parlama süresi")]
        public float hitFlashDuration = 0.07f;
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

        [Header("── ARENA (Faz 1 test) ──")]
        public int enemyCount = 5;
        public float arenaRadius = 14f;

        [Header("── KAMERA (izometrik) ──")]
        public float camYaw = 45f;
        public float camPitch = 50f;
        public float camOrthoSize = 9f;
        public float camFollowLerp = 8f;
    }
}
