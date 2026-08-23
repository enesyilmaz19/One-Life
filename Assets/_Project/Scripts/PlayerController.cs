using UnityEngine;
using UnityEngine.InputSystem;

namespace TekCan
{
    /// <summary>
    /// Hareket + dash (i-frame) + kısa, dash ile iptal edilebilen saldırı.
    /// Blok YOK — blok köşede beklemeyi ödüllendirir, saat ekonomisini öldürür.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        Tuning _t;
        Transform _cam;

        Vector3 _dashDir;
        float _dashTimer, _dashCd, _iFrames;
        float _attackTimer;
        bool _attackHitDone;

        public bool Dashing => _dashTimer > 0f;
        public bool Attacking => _attackTimer > 0f;
        public bool Invulnerable => _iFrames > 0f;

        public void Init(Tuning tuning, Transform cameraTransform)
        {
            _t = tuning;
            _cam = cameraTransform;
        }

        void Update()
        {
            if (_t == null || (Clock.Instance != null && Clock.Instance.Dead)) return;

            _dashCd -= Time.deltaTime;
            _iFrames -= Time.deltaTime;

            Vector3 input = ReadMove();

            if (Dashing) { TickDash(); return; }

            if (ReadDash() && _dashCd <= 0f)
            {
                StartDash(input);
                return;                     // dash saldırıyı iptal eder
            }

            if (Attacking) { TickAttack(input); return; }

            if (ReadAttack()) { StartAttack(input); return; }

            Move(input, 1f);
        }

        // ── girdi ────────────────────────────────────────────────────────────
        // ponytail: cihazdan doğrudan okuma. InputActionAsset'e geçiş Faz 2'de,
        // tuş atama menüsü gerektiğinde.

        Vector3 ReadMove()
        {
            Vector2 v = Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed) v.y += 1f;
                if (kb.sKey.isPressed) v.y -= 1f;
                if (kb.dKey.isPressed) v.x += 1f;
                if (kb.aKey.isPressed) v.x -= 1f;
            }
            var gp = Gamepad.current;
            if (gp != null && v == Vector2.zero) v = gp.leftStick.ReadValue();

            if (v.sqrMagnitude > 1f) v.Normalize();
            return CameraRelative(v);
        }

        static bool ReadDash()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonSouth.wasPressedThisFrame;
        }

        static bool ReadAttack()
        {
            var m = Mouse.current;
            if (m != null && m.leftButton.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonWest.wasPressedThisFrame;
        }

        /// <summary>Girdiyi kamera düzlemine çevirir — izometrikte şart, yoksa WASD çapraz hisseder.</summary>
        Vector3 CameraRelative(Vector2 v)
        {
            if (_cam == null) return new Vector3(v.x, 0f, v.y);
            Vector3 f = _cam.forward; f.y = 0f; f.Normalize();
            Vector3 r = _cam.right;   r.y = 0f; r.Normalize();
            return f * v.y + r * v.x;
        }

        // ── hareket ──────────────────────────────────────────────────────────

        void Move(Vector3 dir, float damping)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.position += dir * _t.moveSpeed * damping * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir), _t.turnSpeed * Time.deltaTime);
        }

        void StartDash(Vector3 input)
        {
            _dashDir = input.sqrMagnitude > 0.0001f ? input.normalized : transform.forward;
            _dashTimer = _t.dashDuration;
            _dashCd = _t.dashCooldown;
            _iFrames = _t.iFrameDuration;
            _attackTimer = 0f;              // dash saldırıyı keser
            transform.rotation = Quaternion.LookRotation(_dashDir);
        }

        void TickDash()
        {
            _dashTimer -= Time.deltaTime;
            float speed = _t.dashDistance / Mathf.Max(0.0001f, _t.dashDuration);
            transform.position += _dashDir * speed * Time.deltaTime;
        }

        // ── saldırı ──────────────────────────────────────────────────────────

        void StartAttack(Vector3 input)
        {
            _attackTimer = _t.attackDuration;
            _attackHitDone = false;
            if (input.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(input);
        }

        void TickAttack(Vector3 input)
        {
            _attackTimer -= Time.deltaTime;
            Move(input, _t.attackMoveDamping);

            float elapsed = _t.attackDuration - _attackTimer;
            if (!_attackHitDone && elapsed >= _t.attackDuration * _t.attackHitMoment)
            {
                _attackHitDone = true;
                Strike();
            }
        }

        void Strike()
        {
            bool hitSomething = false;
            float half = _t.attackAngle * 0.5f;

            for (int i = Enemy.All.Count - 1; i >= 0; i--)
            {
                var e = Enemy.All[i];
                if (e == null) continue;

                Vector3 to = e.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude > _t.attackRange) continue;
                if (Vector3.Angle(transform.forward, to) > half) continue;

                e.TakeDamage(_t.attackDamage);
                hitSomething = true;
            }

            if (hitSomething) HitStop.Freeze(_t.hitStopDuration);
        }

        /// <summary>Darbe yemek saniye götürür. Dash i-frame'i içindeyken geçmez.</summary>
        public void TakeHit(float secondsCost)
        {
            if (Invulnerable) return;
            Clock.Instance?.Spend(secondsCost);
            HitStop.Freeze(_t.hitStopDuration * 1.5f);
        }
    }
}
