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

        Tuning _t;
        Transform _player;
        Renderer _renderer;
        TimeLabel _label;
        float _attackTimer;
        float _flashTimer;

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
                if (_flashTimer <= 0f) Paint();
            }

            // Havuz kendi kendine akar. Boşalınca daha aşağı inmez.
            if (!Hollow)
            {
                TimeLeft = Mathf.Max(0f, TimeLeft - _t.enemyDecayPerSecond * Time.deltaTime);
                if (Hollow && !wasHollow) Paint();
            }

            Chase();
            TryAttack();
            if (_label != null) _label.Set(TimeLeft, Hollow);
        }

        void Chase()
        {
            Vector3 to = _player.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 0.01f) return;

            Vector3 dir = to / dist;
            if (dist > _t.enemyAttackRange * 0.9f)
            {
                float speed = Hollow ? _t.enemyHollowSpeed : _t.enemyFullSpeed;
                transform.position += dir * speed * Time.deltaTime;
            }
            transform.rotation = Quaternion.LookRotation(dir);
        }

        void TryAttack()
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f) return;

            Vector3 flat = _player.position - transform.position;
            flat.y = 0f;
            if (flat.magnitude > _t.enemyAttackRange) return;

            _attackTimer = _t.enemyAttackCooldown;
            float cost = Hollow ? _t.enemyHollowHitCost : _t.enemyFullHitCost;
            var player = _player.GetComponent<PlayerController>();
            if (player != null) player.TakeHit(cost);
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
            All.Remove(this);
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);   // öz-denetim editör modunda çalışabilsin
        }

        void Paint()
        {
            if (_renderer == null) return;
            _renderer.material.color = _flashTimer > 0f ? Color.white : (Hollow ? HollowColor : FullColor);
            // ponytail: renk değişimi + beyaz parlama greybox için yeterli isabet işareti.
            // Gerçek VFX (parçacık, iz, boşalma efekti) Faz 3'te silahlarla birlikte.
        }
    }
}
