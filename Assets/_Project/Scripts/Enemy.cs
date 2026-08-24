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

        public void Init(Tuning tuning, Transform player, TimeLabel label)
        {
            _t = tuning;
            _player = player;
            _label = label;
            TimeLeft = tuning.enemyTimePool;
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
                TimeLeft = Mathf.Max(0f, TimeLeft - _t.enemyDecayPerSecond * Time.deltaTime);
                if (Hollow && !wasHollow) Paint();
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
                    if (_attackTimer <= 0f && FlatDistanceToPlayer() <= _t.enemyAttackRange)
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
            float dur = _t.enemyWindupDuration * (Hollow ? _t.enemyHollowWindupMultiplier : 1f);
            EnterState(State.Windup, dur);
            FacePlayer();

            _telegraph = Arc.Spawn(transform.position, transform.rotation,
                _t.enemyAttackRange, _t.enemyAttackAngle, TelegraphTint, dur, transform);
            Paint();
        }

        void EnterStrike()
        {
            _strikeDir = transform.forward;                         // ← yön burada kilitlenir
            EnterState(State.Strike, _t.enemyStrikeDuration);

            if (_telegraph != null) { _telegraph.Cancel(); _telegraph = null; }
            Arc.Spawn(transform.position, transform.rotation,
                _t.enemyAttackRange, _t.enemyAttackAngle, StrikeTint, _t.enemyStrikeDuration * 1.6f);
            Paint();

            // Hasar yalnızca bu anda, kilitli yöne göre iner. Kaçtıysan ıskalar.
            if (PlayerInStrikeCone())
            {
                float cost = Hollow ? _t.enemyHollowHitCost : _t.enemyFullHitCost;
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
            return to.magnitude <= _t.enemyAttackRange &&
                   Vector3.Angle(_strikeDir, to) <= _t.enemyAttackAngle * 0.5f;
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

            if (dist > _t.enemyAttackRange * 0.9f)
            {
                float speed = Hollow ? _t.enemyHollowSpeed : _t.enemyFullSpeed;
                transform.position += (to / dist) * speed * Time.deltaTime;
            }
            FacePlayer();
        }

        /// <summary>
        /// Hasar verildiğinde havuzdan düşer ve düşen miktarın hasat oranı kadarı
        /// oyuncunun saatine yazılır. Boş düşmana vurmak hiçbir şey ödemez ve öldürür.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (Hollow)
            {
                Kill();                        // tek atımlık
                return;
            }

            float drained = Mathf.Min(damage, TimeLeft);
            TimeLeft -= drained;
            Clock.Instance?.Add(drained * Clock.Instance.HarvestRate);

            _flashTimer = _t.hitFlashDuration;
            Paint();
        }

        void Kill()
        {
            if (_telegraph != null) _telegraph.Cancel();   // ölünce uyarı yayı ortada kalmasın
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
            else c = Hollow ? HollowColor : FullColor;

            _renderer.material.color = c;
            // ponytail: renk değişimi + beyaz parlama greybox için yeterli isabet işareti.
            // Gerçek VFX (parçacık, iz, boşalma efekti) Faz 3'te silahlarla birlikte.
        }
    }
}
