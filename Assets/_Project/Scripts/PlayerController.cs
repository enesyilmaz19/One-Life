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
        Camera _camera;

        Vector3 _dashDir;
        float _dashTimer, _dashCd, _iFrames;
        float _attackTimer;
        bool _attackHitDone;

        Renderer _renderer;
        Color _baseColor;
        float _hurtFlash;
        Vector3 _knockback;

        bool _dashAttackQueued;
        int _weaponIndex;

        /// <summary>Kuşanılan silah. 1-4 tuşlarıyla değişiyor (Faz 3 test kolaylığı).</summary>
        public Weapon Weapon => TekCan.Weapon.All[_weaponIndex];

        static readonly Color SwingTint = new Color(0.65f, 0.9f, 1f, 0.42f);
        static readonly Color DashSwingTint = new Color(0.55f, 1f, 0.85f, 0.55f);
        static readonly Color HurtColor = new Color(1f, 0.3f, 0.3f);

        /// <summary>Savurma yayı üretildi mi — öz-denetim bunu kontrol eder (ıskada da çıkmalı).</summary>
        public int SwingsSpawned { get; private set; }

        public bool Dashing => _dashTimer > 0f;
        public bool Attacking => _attackTimer > 0f;
        public bool Invulnerable => _iFrames > 0f;

        public void Init(Tuning tuning, Transform cameraTransform)
        {
            _t = tuning;
            _cam = cameraTransform;
            _camera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
            _renderer = GetComponent<Renderer>();
            if (_renderer != null) _baseColor = _renderer.material.color;
        }

        void Update()
        {
            if (_t == null || (Clock.Instance != null && Clock.Instance.Dead)) return;

            _dashCd -= Time.deltaTime;
            _iFrames -= Time.deltaTime;
            TickHurt();
            ReadWeaponSwap();

            Vector3 input = ReadMove();

            // DURGUN: basılı tutarken saat donuyor ama saldıramıyor, dash atamıyorsun.
            // Dünya donmadığı için bedeli savunmasız kalmak.
            if (Weapon.HoldFreezesClock && AttackHeld())
            {
                if (Clock.Instance != null) Clock.Instance.Frozen = true;
                return;
            }
            if (Clock.Instance != null) Clock.Instance.Frozen = false;

            if (Dashing) { TickDash(); return; }        // dash sırasında saldırı girdisi burada okunuyor

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

        /// <summary>DURGUN için: basılı tutuluyor mu (tek kare değil).</summary>
        static bool AttackHeld()
        {
            var m = Mouse.current;
            if (m != null && m.leftButton.isPressed) return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonWest.isPressed;
        }

        void ReadWeaponSwap()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) SetWeapon(0);
            else if (kb.digit2Key.wasPressedThisFrame) SetWeapon(1);
            else if (kb.digit3Key.wasPressedThisFrame) SetWeapon(2);
            else if (kb.digit4Key.wasPressedThisFrame) SetWeapon(3);
        }

        /// <summary>Silah kuşan. Öz-denetim de bu yoldan geçiyor — test kendi yolunu kurmaz.</summary>
        public void SetWeapon(int index)
        {
            if (index < 0 || index >= TekCan.Weapon.All.Length || index == _weaponIndex) return;
            _weaponIndex = index;
            _attackTimer = 0f;                                    // yarım savurma taşınmasın
            if (Clock.Instance != null) Clock.Instance.Frozen = false;
            IsoCamera.Shake(_t.hitShake * 0.4f);
        }

        static bool ReadAttack()
        {
            var m = Mouse.current;
            if (m != null && m.leftButton.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonWest.wasPressedThisFrame;
        }

        /// <summary>
        /// Saldırının yöneleceği yön. Sıra: fare imleci → gamepad sağ çubuk →
        /// hareket girdisi → mevcut bakış. İzometrikte imleçle nişan standarttır;
        /// oyuncu kaçarken bile istediği tarafa vurabilmeli.
        /// </summary>
        Vector3 AimDirection(Vector3 moveInput)
        {
            var mouse = Mouse.current;
            if (mouse != null && _camera != null)
            {
                var ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
                var ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
                if (ground.Raycast(ray, out float dist))
                {
                    Vector3 to = ray.GetPoint(dist) - transform.position;
                    to.y = 0f;
                    if (to.sqrMagnitude > 0.04f) return to.normalized;
                }
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 rs = gp.rightStick.ReadValue();
                if (rs.sqrMagnitude > 0.09f) return CameraRelative(rs).normalized;
            }

            if (moveInput.sqrMagnitude > 0.0001f) return moveInput.normalized;
            return transform.forward;
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

        /// <summary>
        /// rotate=false ise yalnızca yer değiştirir, dönmez.
        /// Saldırı sırasında dönmek YASAK: nişan StartAttack'te imlece kilitleniyor ama
        /// vuruş ~0.1 sn sonra iniyor. Arada hareket tuşu oyuncuyu çevirirse yay bir yere,
        /// isabet başka yere gider — savurmaya bakıp ıskalarsın.
        /// </summary>
        void Move(Vector3 dir, float damping, bool rotate = true)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.position += dir * _t.moveSpeed * damping * Time.deltaTime;
            if (rotate)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir), _t.turnSpeed * Time.deltaTime);
        }

        void StartDash(Vector3 input)
        {
            _dashDir = input.sqrMagnitude > 0.0001f ? input.normalized : transform.forward;
            _dashTimer = _t.dashDuration;
            _dashCd = Clock.Instance != null ? Clock.Instance.DashCooldown : _t.dashCooldown;
            _iFrames = _t.iFrameDuration;
            _attackTimer = 0f;              // dash saldırıyı keser
            _dashAttackQueued = false;      // önceki dash'ten sarkmasın
            transform.rotation = Quaternion.LookRotation(_dashDir);
        }

        void TickDash()
        {
            // DASH-ATAK: dash sırasında saldırıya basarsan dash bitiminde geniş bir
            // vuruş iner. Dash hem kaçış hem giriş oluyor — kaçtığın yerde bırakmıyor,
            // içine dalıp çıkmanı ödüllendiriyor. (Enes istedi, Faz 1'den devir.)
            if (!_dashAttackQueued && ReadAttack()) _dashAttackQueued = true;

            _dashTimer -= Time.deltaTime;
            float speed = _t.dashDistance / Mathf.Max(0.0001f, _t.dashDuration);
            transform.position += _dashDir * speed * Time.deltaTime;

            if (_dashTimer <= 0f && _dashAttackQueued)
            {
                _dashAttackQueued = false;
                DashStrike();
            }
        }

        void DashStrike()
        {
            transform.rotation = Quaternion.LookRotation(_dashDir);

            float range = Weapon.Range(_t) * _t.dashAttackRangeMult;
            float damage = Weapon.Damage(_t, Clock.Instance != null ? Clock.Instance.AttackDamage : _t.attackDamage)
                           * _t.dashAttackDamageMult;

            Arc.Spawn(transform.position, transform.rotation,
                range, _t.dashAttackAngle, DashSwingTint, _t.swingArcLife * 1.3f);
            SwingsSpawned++;

            // Dash-atak silahtan bağımsız olarak GENİŞ vurur — dalış hamlesi olduğu için
            // tek hedefli silahlarda bile kalabalığı süpürmesi kasıtlı, tek AoE çıkışları o.
            StrikeCone(range, _t.dashAttackAngle, damage, _t.hitStopDuration * 1.4f, _t.hitShake * 1.5f,
                       int.MaxValue, Weapon.HarvestMultiplier, out int hits, out int kills);

            Weapon.OnStrike(Clock.Instance, _t, hits, kills);
        }

        // ── saldırı ──────────────────────────────────────────────────────────

        void StartAttack(Vector3 input)
        {
            // Silah savurmayı reddedebilir (FAİZLİ: peşin bedeli ödeyemiyorsan susar).
            if (!Weapon.OnSwingStart(Clock.Instance, _t)) return;

            _attackTimer = Weapon.Duration(_t);
            _attackHitDone = false;

            // ⚠️ NİŞAN İMLEÇTEN ALINIR, hareket yönünden değil.
            // Eskiden hareket yönüne dönüyordu: kaçarken vurunca yay boşluğa gidiyordu,
            // düşman arkanda kalıyordu ve hiçbir şeye isabet etmiyordu.
            Vector3 aim = AimDirection(input);
            if (aim.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(aim);

            // ⚠️ ISKADA DA ÇIKAR. Faz 1'in ilk playtest'i tam olarak bu eksik olduğu için
            // yürümedi: oyuncu saldırdığını göremeyince hissi yargılayamadı.
            // Yay, gerçek vuruş alanının kendisi — menzil ve açı Strike() ile aynı kaynaktan.
            Arc.Spawn(transform.position, transform.rotation,
                Weapon.Range(_t), Weapon.Angle(_t), Weapon.Tint, _t.swingArcLife);
            SwingsSpawned++;
        }

        void TickAttack(Vector3 input)
        {
            _attackTimer -= Time.deltaTime;
            Move(input, _t.attackMoveDamping, rotate: false);   // savurma yönüne bağlı kal

            float duration = Weapon.Duration(_t);
            float elapsed = duration - _attackTimer;
            if (!_attackHitDone && elapsed >= duration * _t.attackHitMoment)
            {
                _attackHitDone = true;
                Strike();
            }
        }

        void Strike()
        {
            // Hasar Clock'tan okunuyor: sunak yükseltmeleri koşu kapsamlı,
            // Tuning asset'i çalışma anında değiştirilmiyor.
            float baseDamage = Clock.Instance != null ? Clock.Instance.AttackDamage : _t.attackDamage;

            StrikeCone(Weapon.Range(_t), Weapon.Angle(_t), Weapon.Damage(_t, baseDamage),
                       _t.hitStopDuration, _t.hitShake,
                       Weapon.MaxTargets, Weapon.HarvestMultiplier,
                       out int hits, out int kills);

            Weapon.OnStrike(Clock.Instance, _t, hits, kills);
        }

        /// <summary>
        /// Tek isabet yolu — normal saldırı da dash-atak da buradan geçiyor.
        /// İki ayrı kopya olsaydı biri düzeltilip diğeri unutulurdu; savurma yayı
        /// da aynı menzil/açıdan çiziliyor, yani görsel ile isabet asla ayrışmaz.
        ///
        /// `maxTargets` 1 ise koni içindeki EN YAKIN düşman seçilir — tek hedefli
        /// silahların tanımı bu. Geometriyi daraltmak yetmezdi: dar koni de iki
        /// düşmanı birden yakalayabilir.
        /// </summary>
        void StrikeCone(float range, float angleDeg, float damage, float hitStop, float shake,
                        int maxTargets, float harvestMult, out int hits, out int kills)
        {
            hits = 0; kills = 0;
            float half = angleDeg * 0.5f;

            if (maxTargets == 1)
            {
                Enemy nearest = null;
                float bestDist = float.MaxValue;

                foreach (var e in Enemy.All)
                {
                    if (e == null) continue;
                    Vector3 to = e.transform.position - transform.position;
                    to.y = 0f;
                    float d = to.magnitude;
                    if (d > range || Vector3.Angle(transform.forward, to) > half) continue;
                    if (d < bestDist) { bestDist = d; nearest = e; }
                }

                if (nearest != null)
                {
                    bool wasHollow = nearest.Hollow;
                    nearest.TakeDamage(damage, harvestMult);
                    hits = 1;
                    if (wasHollow) kills = 1;          // boş düşmana vuruş öldürür
                }
            }
            else
            {
                for (int i = Enemy.All.Count - 1; i >= 0; i--)
                {
                    var e = Enemy.All[i];
                    if (e == null) continue;

                    Vector3 to = e.transform.position - transform.position;
                    to.y = 0f;

                    if (to.magnitude > range) continue;
                    if (Vector3.Angle(transform.forward, to) > half) continue;

                    bool wasHollow = e.Hollow;
                    e.TakeDamage(damage, harvestMult);
                    hits++;
                    if (wasHollow) kills++;
                }
            }

            if (hits > 0)
            {
                HitStop.Freeze(hitStop);
                IsoCamera.Shake(shake);
            }
        }

        /// <summary>
        /// Darbe yemek saniye götürür. Dash i-frame'i içindeyken geçmez.
        /// Kırmızı parlama + geri itilme, "yedim" anını okunur kılıyor —
        /// ilk playtest'te saatin azalması dışında hiçbir işaret yoktu.
        /// </summary>
        public void TakeHit(float secondsCost, Vector3 fromPosition)
        {
            if (Invulnerable) return;

            Clock.Instance?.Spend(secondsCost);
            HitStop.Freeze(_t.hitStopDuration * 1.5f);
            IsoCamera.Shake(_t.hurtShake);

            _hurtFlash = _t.playerHurtFlashDuration;
            Vector3 away = transform.position - fromPosition;
            away.y = 0f;
            if (away.sqrMagnitude > 0.0001f) _knockback = away.normalized * _t.playerKnockback;
            Paint();
        }

        void TickHurt()
        {
            if (_hurtFlash > 0f)
            {
                _hurtFlash -= Time.unscaledDeltaTime;   // hit-stop sırasında da sönsün
                if (_hurtFlash <= 0f) Paint();
            }

            if (_knockback.sqrMagnitude > 0.0001f)
            {
                transform.position += _knockback * Time.deltaTime;
                _knockback = Vector3.Lerp(_knockback, Vector3.zero, 12f * Time.deltaTime);
                if (_knockback.sqrMagnitude < 0.0001f) _knockback = Vector3.zero;
            }
        }

        void Paint()
        {
            if (_renderer == null) return;
            _renderer.material.color = _hurtFlash > 0f ? HurtColor : _baseColor;
        }

        /// <summary>Ayar yaparken vuruş alanı görünsün — Hierarchy'den Player'ı seç.</summary>
        void OnDrawGizmosSelected()
        {
            if (_t == null) return;
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.9f);
            float half = _t.attackAngle * 0.5f;
            for (float a = -half; a <= half; a += 10f)
                Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, a, 0f) * transform.forward * _t.attackRange);
        }
    }
}
