using System.Collections.Generic;
using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Düşmanın canı = zaman havuzu. Havuz hem canı hem ödemesi.
    ///
    /// DOLU  (havuz > 0) : hızlı, acıtır, hasar verdikçe hasat edilir
    /// BOŞ   (havuz = 0) : yavaş, zayıf, TEK ATIMLIK, hiçbir şey ödemez
    ///
    /// Havuz kendi kendine de akar. Beklersen boşalır — bedavaya dövüşürsün.
    /// AFK sömürüsü değil: kendi saatin akarken sıfır hasat alırsın, her zaman zararına.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> All = new List<Enemy>();

        /// <summary>Kovala → HAZIRLIK (uyarı görünür) → VURUŞ (yön kilitli) → toparlanma.</summary>
        public enum State { Chase, Windup, Strike, Recover }

        Tuning _t;
        Transform _player;
        Renderer _renderer;
        TimeLabel _label;
        float _attackTimer;
        float _flashTimer;

        Vector3 _baseScale;
        State _state = State.Chase;
        float _stateTimer;
        Vector3 _strikeDir;
        Arc _telegraph;

        public State CurrentState => _state;

        static readonly Color WindupColor = new Color(1f, 0.75f, 0.2f);
        static readonly Color TelegraphTint = new Color(1f, 0.55f, 0.15f, 0.30f);
        static readonly Color StrikeTint = new Color(1f, 0.25f, 0.15f, 0.55f);

        /// <summary>Bedende kalan saniye. Sıfırlanınca ölmez — boşalır.</summary>
        public float TimeLeft { get; private set; }
        public bool Hollow => TimeLeft <= 0f;

        static readonly Color FullColor = new Color(0.85f, 0.25f, 0.25f);
        static readonly Color HollowColor = new Color(0.35f, 0.35f, 0.38f);

        /// <summary>Boss mu — kendi akışı yavaş, saatini emiyor, beklenerek yenilemiyor.</summary>
        public bool IsBoss { get; private set; }

        /// <summary>
        /// Gövde yarıçapı. İsabet hesabı bunu düşüyor: menzil MERKEZE değil YÜZEYE ölçülüyor.
        ///
        /// ⚠️ Bu olmadan büyük düşmanlar "bazen vurulamıyor" oluyordu: boss 1.9 kat büyük,
        /// gövdesine değecek kadar yakınken merkezler arası mesafe hâlâ menzilin dışında
        /// kalabiliyordu. Oyuncu gördüğü şeye vuramıyordu — görsel ile isabet ayrışmıştı.
        /// </summary>
        public float Radius => transform.localScale.x * 0.5f;

        // ── boss ölçekleri ───────────────────────────────────────────────────
        // Boss yalnızca "kalın bir düşman" olmasın: daha hızlı, daha az uyarı verir,
        // daha geniş vurur ve darbesi çok daha pahalıdır. Menzili de gövdesi kadar uzun,
        // yoksa dev bir yaratık burnunun dibinden ıskalıyor gibi görünür.
        float WindupDuration => _t.enemyWindupDuration
                              * (Hollow ? _t.enemyHollowWindupMultiplier : 1f)
                              * (IsBoss ? _t.bossWindupMultiplier : 1f);
        float AttackAngle => _t.enemyAttackAngle * (IsBoss ? _t.bossAttackAngleMultiplier : 1f);
        float AttackRange => _t.enemyAttackRange + (IsBoss ? Radius : 0f);
        /// <summary>
        /// Şu andaki gerçek hızı. Öz-denetim bunu okuyor — formülü kopyalamak yerine.
        /// Kopyalasaydı `MoveSpeed`'e yeni bir çarpan eklendiğinde test yeşil kalır,
        /// oyun bozulurdu (bkz. lessons.md: "düzenek kopyalamaz, ithal eder").
        /// </summary>
        public float CurrentSpeed => MoveSpeed;

        float MoveSpeed => (Hollow ? _t.enemyHollowSpeed : _t.enemyFullSpeed)
                         * (IsBoss ? _t.bossSpeedMultiplier : 1f)
                         * Kind.SpeedMultiplier
                         * (Fleeing ? _t.runnerFleeSpeedMultiplier : 1f);
        float HitCost => (Hollow ? _t.enemyHollowHitCost : _t.enemyFullHitCost)
                       * (IsBoss ? _t.bossHitCostMultiplier : 1f);

        public EnemyKind Kind { get; private set; } = EnemyKind.Basic;

        /// <summary>Kaçak kaçıyor mu — havuzu eşiğin altına inince başlıyor.</summary>
        public bool Fleeing { get; private set; }

        float _startingPool;

        public void Init(Tuning tuning, Transform player, TimeLabel label,
                         float poolMultiplier = 1f, bool boss = false, EnemyKind kind = null)
        {
            _t = tuning;
            _player = player;
            _label = label;
            IsBoss = boss;
            Kind = kind ?? EnemyKind.Basic;
            TimeLeft = tuning.enemyTimePool * poolMultiplier * Kind.PoolMultiplier;
            _startingPool = TimeLeft;
            _baseScale = transform.localScale;
            _renderer = GetComponentInChildren<Renderer>();
            All.Add(this);
            Paint();
        }

        void OnDestroy() => All.Remove(this);

        void Update()
        {
            if (_t == null || _player == null) return;

            bool wasHollow = Hollow;

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.unscaledDeltaTime;   // hit-stop sırasında da sönsün

                // Ölçek darbesi: isabetin en ucuz ve en okunur işareti. Renk solsa da görünür.
                float k = Mathf.Clamp01(_flashTimer / Mathf.Max(0.0001f, _t.hitFlashDuration));
                transform.localScale = _baseScale * (1f + 0.28f * k);

                if (_flashTimer <= 0f) { transform.localScale = _baseScale; Paint(); }
            }

            // Havuz kendi kendine akar. Boşalınca daha aşağı inmez.
            if (!Hollow)
            {
                // Boss'un kendi akışı çok yavaş: boşalmasını bekleyerek yenemezsin.
                float decay = _t.enemyDecayPerSecond * (IsBoss ? _t.bossDecayMultiplier : 1f);
                TimeLeft = Mathf.Max(0f, TimeLeft - decay * Time.deltaTime);
                if (Hollow && !wasHollow) Paint();
            }

            ApplyAura(Time.deltaTime);

            // KAÇAK: havuzu eşiğin altına inince kaçmaya başlar.
            if (!Fleeing && !Hollow && Kind.FleesBelowRatio > 0f &&
                _startingPool > 0f && TimeLeft / _startingPool <= Kind.FleesBelowRatio)
            {
                Fleeing = true;
                Paint();
            }

            // Kaçtı ve kaçabildi: zamanını götürüyor, hiçbir şey ödemiyor.
            // ⚠️ YATAY mesafe: `position.magnitude` y'yi de katardı ve düşmanlar y=1'de.
            // Bu projede y'nin yatay hesaba karışması iki kez hata üretti.
            if (Fleeing && FlatDistanceFromCenter() >= _t.runnerEscapeRadius)
            {
                Escape();
                return;
            }

            TickState();
            if (_label != null) _label.Set(TimeLeft, Hollow);
        }

        // ── saldırı evreleri ─────────────────────────────────────────────────
        // Hazırlık boyunca oyuncuyu takip eder ve uyarı yayı görünür.
        // Vuruş başlarken yön KİLİTLENİR — dodge burada mümkün olur.
        // Kilit olmasaydı telegraf yalan söylerdi ve kaçmak imkânsız olurdu.

        void TickState()
        {
            _stateTimer -= Time.deltaTime;

            switch (_state)
            {
                case State.Chase:
                    _attackTimer -= Time.deltaTime;
                    ChaseMove();
                    if (_attackTimer <= 0f && FlatDistanceToPlayer() <= AttackRange)
                        EnterWindup();
                    break;

                case State.Windup:
                    FacePlayer();                                   // hâlâ takip ediyor
                    if (_stateTimer <= 0f) EnterStrike();
                    break;

                case State.Strike:
                    transform.position += _strikeDir *
                        (_t.enemyLungeDistance / Mathf.Max(0.0001f, _t.enemyStrikeDuration)) * Time.deltaTime;
                    if (_stateTimer <= 0f) EnterState(State.Recover, _t.enemyRecoverDuration);
                    break;

                case State.Recover:
                    if (_stateTimer <= 0f)
                    {
                        _attackTimer = _t.enemyAttackCooldown;
                        EnterState(State.Chase, 0f);
                    }
                    break;
            }
        }

        void EnterWindup()
        {
            float dur = WindupDuration;
            EnterState(State.Windup, dur);
            FacePlayer();

            _telegraph = Arc.Spawn(transform.position, transform.rotation,
                AttackRange, AttackAngle, TelegraphTint, dur, transform);
            Paint();
        }

        void EnterStrike()
        {
            _strikeDir = transform.forward;                         // ← yön burada kilitlenir
            EnterState(State.Strike, _t.enemyStrikeDuration);

            if (_telegraph != null) { _telegraph.Cancel(); _telegraph = null; }
            Arc.Spawn(transform.position, transform.rotation,
                AttackRange, AttackAngle, StrikeTint, _t.enemyStrikeDuration * 1.6f);
            Paint();

            // Hasar yalnızca bu anda, kilitli yöne göre iner. Kaçtıysan ıskalar.
            if (PlayerInStrikeCone())
            {
                float cost = HitCost;
                _player.GetComponent<PlayerController>()?.TakeHit(cost, transform.position);
            }
        }

        void EnterState(State s, float duration)
        {
            _state = s;
            _stateTimer = duration;
            if (s == State.Chase || s == State.Recover) Paint();
        }

        /// <summary>Oyuncuya doğru yatay vektör — y sıfırlanmış, dört yerde aynıydı.</summary>
        Vector3 FlatToPlayer()
        {
            Vector3 to = _player.position - transform.position;
            to.y = 0f;
            return to;
        }

        bool PlayerInStrikeCone()
        {
            Vector3 to = FlatToPlayer();
            return to.magnitude <= AttackRange &&
                   Vector3.Angle(_strikeDir, to) <= AttackAngle * 0.5f;
        }

        float FlatDistanceToPlayer() => FlatToPlayer().magnitude;

        void FacePlayer()
        {
            Vector3 to = FlatToPlayer();
            if (to.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(to.normalized);
        }

        void ChaseMove()
        {
            Vector3 to = FlatToPlayer();
            float dist = to.magnitude;
            if (dist < 0.01f) return;

            // KAÇAK kaçarken oyuncudan UZAKLAŞIYOR — kovalamak senin kararın.
            if (Fleeing)
            {
                transform.position -= (to / dist) * MoveSpeed * Time.deltaTime;
                transform.rotation = Quaternion.LookRotation(-to / dist);
                return;
            }

            if (dist > AttackRange * 0.9f)
                transform.position += (to / dist) * MoveSpeed * Time.deltaTime;
            FacePlayer();
        }

        /// <summary>
        /// Hasar verildiğinde havuzdan düşer ve düşen miktarın hasat oranı kadarı
        /// oyuncunun saatine yazılır. Boş düşmana vurmak hiçbir şey ödemez ve öldürür.
        /// </summary>
        /// <param name="harvestMult">
        /// Silaha göre değişen hasat çarpanı. Tek hedefli silahlar koniyle ancak bu
        /// sayede rekabet edebiliyor — koni, hedef sayısı kadar hasat ediyor.
        /// </param>
        public void TakeDamage(float damage, float harvestMult = 1f)
        {
            if (Hollow)
            {
                Kill();                        // tek atımlık
                return;
            }

            float drained = Mathf.Min(damage, TimeLeft);
            TimeLeft -= drained;
            // Kind.HarvestMultiplier 0 olabilir (KASA): vurarak hiçbir şey alamazsın,
            // bütün ödeme öldürme anında gelir.
            Clock.Instance?.Add(drained * Clock.Instance.HarvestRate * harvestMult * Kind.HarvestMultiplier);

            _flashTimer = _t.hitFlashDuration;
            Paint();
        }

        /// <summary>
        /// KAÇAK arenadan çıktı: zamanını götürüyor, hiçbir şey ödemiyor, öldürme sayılmıyor.
        /// Yatırdığın zaman çöpe gitti — kovalamamanın bedeli bu.
        /// </summary>
        /// <summary>
        /// Yaşadığı sürece saati emen türler (boss, SIZINTI). Oyalanmak ya da yanlış
        /// hedefi seçmek doğrudan cezalandırılıyor.
        ///
        /// dt dışarıdan alınıyor çünkü öz-denetim `Time.deltaTime`'ı kontrol edemiyor
        /// (editör modunda 0). Aksi hâlde "aura tanımlı mı" diye ÖZELLİK test edilirdi,
        /// "saat gerçekten indi mi" diye ETKİ değil — DURGUN dersinin tam tekrarı olurdu.
        /// </summary>
        public void ApplyAura(float dt)
        {
            if (Hollow || _t == null) return;
            float aura = (IsBoss ? _t.bossAuraDrain : 0f) + Kind.AuraDrain(_t);
            if (aura > 0f) Clock.Instance?.Spend(aura * dt);
        }

        float FlatDistanceFromCenter()
        {
            var p = transform.position;
            return new Vector2(p.x, p.z).magnitude;
        }

        void Escape()
        {
            if (_telegraph != null) _telegraph.Cancel();
            All.Remove(this);
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        void Kill()
        {
            if (_telegraph != null) _telegraph.Cancel();   // ölünce uyarı yayı ortada kalmasın
            Kind.OnKilled(Clock.Instance, _t, _startingPool);   // KASA bütün ödemesini burada yapar
            Clock.Instance?.ReportKill();
            All.Remove(this);
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);   // öz-denetim editör modunda çalışabilsin
        }

        void Paint()
        {
            if (_renderer == null) return;

            Color c;
            if (_flashTimer > 0f) c = Color.white;                                  // isabet aldı
            else if (_state == State.Windup || _state == State.Strike) c = WindupColor;  // saldırıyor
            else if (Hollow) c = HollowColor;
            else if (Fleeing) c = Color.Lerp(Kind.FullColor, Color.white, 0.45f);   // kaçıyor: soluyor
            else c = Kind.FullColor;                                                // tür rengi

            _renderer.material.color = c;
            // ponytail: renk değişimi + beyaz parlama greybox için yeterli isabet işareti.
            // Gerçek VFX (parçacık, iz, boşalma efekti) Faz 3'te silahlarla birlikte.
        }
    }
}
