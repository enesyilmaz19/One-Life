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

        TrailRenderer _dashTrail;    // yalnızca dash boyunca çizer, bkz. BuildDashTrail
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
            BuildDashTrail();
        }

        /// <summary>
        /// Dash izi. Kendi iz kodumuzu yazmıyoruz — `TrailRenderer` Unity'nin hazır
        /// özelliği ve tam bu iş için var.
        ///
        /// Dash'in sesi Faz 6'da geldi ama görseli yoktu: hızlı hareket ettiğinde
        /// kapsül sadece ışınlanıyormuş gibi duruyordu. İz, kat edilen mesafeyi
        /// gösteriyor — i-frame penceresinin nereden nereye taşındığı okunur oluyor.
        /// </summary>
        void BuildDashTrail()
        {
            _dashTrail = gameObject.AddComponent<TrailRenderer>();
            _dashTrail.material = UnlitMat.Additive;
            _dashTrail.time = 0.20f;                  // dashDuration'dan uzun: dash bitince biraz sarksın
            _dashTrail.widthCurve = AnimationCurve.Linear(0f, 0.55f, 1f, 0f);
            _dashTrail.numCapVertices = 4;
            _dashTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _dashTrail.receiveShadows = false;
            _dashTrail.colorGradient = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.55f, 0.85f, 1f), 0f),
                    new GradientColorKey(new Color(0.20f, 0.45f, 0.9f), 1f)
                },
                alphaKeys = new[] { new GradientAlphaKey(0.75f, 0f), new GradientAlphaKey(0f, 1f) }
            };
            _dashTrail.emitting = false;   // yalnızca dash sırasında
        }

        void Update()
        {
            if (_t == null || (Clock.Instance != null && Clock.Instance.Dead)) return;

            _dashCd -= Time.deltaTime;
            _iFrames -= Time.deltaTime;
            TickHurt();
            ReadWeaponSwap();

            Vector3 input = ReadMove();

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

        /// <summary>Silah değiştirme tuşları — kadro büyüdükçe buraya tuş eklenir.</summary>
        static readonly Key[] SwapKeys =
            { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };

        void ReadWeaponSwap()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // ⚠️ Listeden sürülüyor, elle sayılmıyor. Sabit 1-4 yazılıydı ve kadro
            // 5'e çıkınca BEŞİNCİ SİLAH ERİŞİLEMEZ olacaktı — ders 11'in aynısı
            // ("tanımlı ≠ ulaşılabilir"), sadece silahta. Öz-denetim de bunu ölçüyor.
            for (int i = 0; i < SwapKeys.Length && i < TekCan.Weapon.All.Length; i++)
                if (kb[SwapKeys[i]].wasPressedThisFrame) { SetWeapon(i); return; }
        }

        /// <summary>Silah kuşan. Öz-denetim de bu yoldan geçiyor — test kendi yolunu kurmaz.</summary>
        public void SetWeapon(int index)
        {
            if (index < 0 || index >= TekCan.Weapon.All.Length || index == _weaponIndex) return;
            _weaponIndex = index;
            _attackTimer = 0f;                                    // yarım savurma taşınmasın

            // ⚠️ Eskiden burada ekran sarsılıyordu. Enes: *"silahlar arası geçişi
            // daha hızlı yapabilmeliyiz, kombolar için."* Geçiş KODDA zaten anlıktı
            // (Update'in en başında okunuyor, dash ve saldırı dönüşlerinden ÖNCE);
            // yavaşlatan şey mekanik değil SARSINTIYDI — her tuşa basışta ekran
            // zıplayınca kombo akmıyor, tökezliyor. Ses kalıyor, sarsıntı gidiyor.
            Sfx.Swing();
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
        /// <summary>
        /// İmlecin YER DÜZLEMİNDEKİ noktası. `AimDirection` ve İPOTEK'in yerleştirme
        /// noktası aynı ışını istiyordu; iki kopya olsaydı biri düzeltilip öteki
        /// unutulurdu — nişan ile vuruşun ayrışması bu projede zaten üç kez oldu.
        /// </summary>
        bool CursorGround(out Vector3 point)
        {
            point = transform.position;
            var mouse = Mouse.current;
            if (mouse == null || _camera == null) return false;

            var ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            var ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (!ground.Raycast(ray, out float dist)) return false;

            point = ray.GetPoint(dist);
            return true;
        }

        Vector3 AimDirection(Vector3 moveInput)
        {
            if (CursorGround(out Vector3 nokta))
            {
                Vector3 to = nokta - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.04f) return to.normalized;
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
            Sfx.Dash();
            _attackTimer = 0f;              // dash saldırıyı keser
            _dashAttackQueued = false;      // önceki dash'ten sarkmasın
            transform.rotation = Quaternion.LookRotation(_dashDir);
            if (_dashTrail != null) { _dashTrail.Clear(); _dashTrail.emitting = true; }
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

            if (_dashTimer > 0f) return;

            // İz yalnızca dash boyunca ÇİZİLİR; var olan kuyruk kendi kendine sönüyor.
            // Kapatmasaydık normal yürürken de arkamızda mavi bir şerit kalırdı ve
            // dash'in "bu farklı bir hareket" okunurluğu yok olurdu.
            if (_dashTrail != null) _dashTrail.emitting = false;

            if (_dashAttackQueued)
            {
                _dashAttackQueued = false;
                DashStrike();
            }
        }

        void DashStrike()
        {
            transform.rotation = Quaternion.LookRotation(_dashDir);

            // Dash-atak artık SİLAHA GÖRE değişiyor: tek hedefli silah dash'te de tek
            // hedefli kalır ama çok sert vurur; koni süpürür ama vuruş başına daha az.
            // Eskiden hepsi aynı geniş alan vuruşuydu ve silahın kimliğini siliyordu.
            float range = Weapon.Range(_t) * _t.dashAttackRangeMult;
            float angle = Weapon.DashAngle(_t);
            float damage = WeaponDamage() * Weapon.DashDamageMultiplier(_t);

            // Silahın kendi rengi ama daha parlak — dash-atak olduğu okunsun.
            var tint = Weapon.Tint; tint.a = Mathf.Min(1f, tint.a * 1.5f);
            Arc.Spawn(transform.position, transform.rotation,
                range, angle, tint, _t.swingArcLife * 1.3f);
            SwingsSpawned++;
            Sfx.Swing();

            // Dash-atak zaten ağır; silahın kendi ağırlığı ONUN ÜSTÜNE biniyor,
            // yerine geçmiyor — yoksa FAİZLİ'nin dash'i normal savurmasından hafif kalırdı.
            StrikeCone(range, angle, damage,
                       _t.hitStopDuration * 1.4f * Weapon.HitStopMultiplier,
                       _t.hitShake * 1.5f * Weapon.ShakeMultiplier,
                       Weapon.DashTargets, Weapon.HarvestMultiplier, out int hits, out int kills);

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
            // ⚠️ YER VURUŞU SİLAHI SAVURMA YAYI ÇİZMEZ. İPOTEK koni vurmuyor, ama yay
            // koşulsuz çiziliyordu: ekranda kocaman bir koni açılıyor ve HİÇBİR ŞEYE
            // VURMUYOR. Enes bildirdi. Bu, "mekanik doğru görsel yalan" hatasının
            // DÖRDÜNCÜSÜ — ve en kötüsü, çünkü yay burada isabet alanının kendisi
            // olduğunu söylüyor. İşaretin kendi halkası zaten `GroundMark` içinde çiziliyor.
            if (Weapon.GroundMarkDelay(_t) <= 0f)
                Arc.Spawn(transform.position, transform.rotation,
                    Weapon.Range(_t), Weapon.Angle(_t), Weapon.Tint, _t.swingArcLife);

            // ⚠️ ÖLÜ BÖLGE DE GÖRÜNMELİ (HACİZ). Boss'un simit göbeğiyle aynı gerekçe,
            // ama TERS ANLAM: boss'unki güvenli olduğu için YEŞİL, bu ise silahın
            // ÇALIŞMADIĞI yer, o yüzden kırmızı. Görünmeseydi oyuncu dibindekine
            // savurur, ıskalar ve sebebini asla anlamazdı — ders 10'un aynısı.
            float ölüBölge = Weapon.MinRange(_t);
            if (ölüBölge > 0f)
                Arc.Spawn(transform.position, transform.rotation,
                    ölüBölge, Weapon.Angle(_t), new Color(0.95f, 0.35f, 0.3f, 0.3f),
                    _t.swingArcLife);

            SwingsSpawned++;
            Sfx.Swing();
        }

        void TickAttack(Vector3 input)
        {
            _attackTimer -= Time.deltaTime;

            // ⚠️ SALDIRI TAAHHÜTTÜR. Eskiden savurma sırasında %25 hızla hareket
            // edilebiliyordu; geri çekilince düşman menzilden çıkıyordu ve vuruş
            // ıskalıyordu — Enes'in "hareket tuşlarıyla bozuluyor" dediği şey buydu.
            // Saldırıyı bozan tek şey DASH olmalı, WASD değil.
            Move(input, _t.attackMoveDamping, rotate: false);

            float duration = Weapon.Duration(_t);
            float elapsed = duration - _attackTimer;
            if (!_attackHitDone && elapsed >= duration * _t.attackHitMoment)
            {
                _attackHitDone = true;
                Strike();
            }
        }

        /// <summary>
        /// Silahın nihai hasarı — sunak yükseltmeleri + İFLAS'ın çaresizlik eğrisi.
        /// ⚠️ TEK yer: normal savurma da dash-atak da buradan geçiyor. İkisinde ayrı
        /// hesaplansaydı İFLAS dash'te eğrisini kaybederdi ve kimse fark etmezdi.
        /// </summary>
        float WeaponDamage()
        {
            float baseDamage = Clock.Instance != null ? Clock.Instance.AttackDamage : _t.attackDamage;
            float saniye = Clock.Instance != null ? Clock.Instance.Seconds : _t.startSeconds;
            return Weapon.Damage(_t, baseDamage) * Weapon.DesperationMultiplier(saniye, _t);
        }

        void Strike()
        {
            // ⚠️ İPOTEK koni vurmuyor: nişan noktasına GECİKMELİ yer vuruşu bırakıyor.
            // Silahın kendi vuruş biçimi olması `Weapon`'ın deseniyle uyumlu —
            // veri + kanca, `PlayerController` içinde silah adı geçmiyor.
            if (Weapon.GroundMarkDelay(_t) > 0f) { PlaceLien(); return; }

            StrikeCone(Weapon.Range(_t), Weapon.Angle(_t), WeaponDamage(),
                       _t.hitStopDuration * Weapon.HitStopMultiplier,
                       _t.hitShake * Weapon.ShakeMultiplier,
                       Weapon.MaxTargets, Weapon.HarvestMultiplier,
                       out int hits, out int kills,
                       minRange: Weapon.MinRange(_t));

            Weapon.OnStrike(Clock.Instance, _t, hits, kills);
        }

        /// <summary>
        /// İPOTEK'in vuruşu — nişan noktasına gecikmeli alan işareti.
        ///
        /// ⚠️ Nokta MENZİLE KIRPILIYOR. Kırpmasaydım imleci ekranın ucuna götürüp
        /// arenanın öbür ucuna vurabilirdin ve "menzil" diye bir şey kalmazdı.
        /// Kırpılan nokta yönde kalıyor, yani nişan hep dürüst.
        /// </summary>
        void PlaceLien()
        {
            // ⚠️ AYNI ANDA TEK İPOTEK — spam'in YAPISAL cevabı.
            // Enes: "spam yapınca çok vuruyor, bonk bonk bonk". Gecikme savurmadan
            // uzun olduğu için üç işareti üst üste yığabiliyordun. Hasat çarpanını
            // kısmak bunu çözmezdi, sadece geciktirirdi. Tek işaret kuralı silahın
            // kimliğini de netleştiriyor: koyduğunu BEKLEYECEKSİN — "nereye
            // gidecekler" sorusu ancak bekleyince gerçek bir soru.
            if (GroundMark.ActiveLiens > 0) return;

            Vector3 hedef = CursorGround(out Vector3 nokta)
                          ? nokta
                          : transform.position + transform.forward * Weapon.Range(_t);

            Vector3 delta = hedef - transform.position;
            delta.y = 0f;                                  // tuzak 2: y yatay hesaba karışmasın
            float menzil = Weapon.Range(_t);
            if (delta.magnitude > menzil) delta = delta.normalized * menzil;

            GroundMark.SpawnLien(_t, transform.position + delta,
                                 Weapon.GroundMarkRadius(_t), Weapon.GroundMarkDelay(_t),
                                 WeaponDamage(), Weapon.HarvestMultiplier,
                                 transform.parent);

            // Silah "savurdu" sayılıyor: iska/isabet ayrımı işaretin KENDİSİNDE
            // çözülüyor, burada değil. Bedeli olan silahlar yine ödemesini alıyor.
            Weapon.OnStrike(Clock.Instance, _t, 0, 0);
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
        /// <summary>
        /// İsabet kuralının TEK yeri. Hem MENZİL hem AÇI düşmanın MERKEZİNE değil
        /// YÜZEYİNE ölçülür — düşman bir nokta değil, gövdesi olan bir şey.
        ///
        /// ⚠️ Enes "bossa bazen vuramıyorum" dedi ve İKİ KEZ bildirdi. İlkinde yalnızca
        /// MENZİL yüzeye çevrildi, AÇI merkezde bırakıldı — yani kök nedenin yarısı
        /// düzeltildi ve şikâyet geri geldi. Reprodüksiyon (`BossHitRepro`) ölçtü:
        /// boss 1.2 birim yakındayken 41 DERECELİK ölü bant vardı; gövde yayın içinde
        /// apaçık duruyor, merkezi dışarıda kalıyor, vuruş düşüyordu.
        ///
        /// Yay da aynı açıyla çizildiği için GÖRSEL YALAN SÖYLÜYORDU: gövdeyi yayın
        /// içinde görüyor, hiçbir şey alamıyordun.
        ///
        /// Pay = gövdenin açısal yarıçapı: `asin(r / d)`. Oyuncu gövdenin içindeyse
        /// (d ≤ r) pay 90° olur, yani her yönden vurulur — doğrusu da bu.
        /// </summary>
        bool InHitCone(Enemy e, float range, float halfAngle, out float surfaceDist,
                       float minRange = 0f)
        {
            surfaceDist = float.MaxValue;
            if (e == null) return false;

            Vector3 to = e.transform.position - transform.position;
            to.y = 0f;

            float dist = to.magnitude;
            surfaceDist = dist - e.Radius;
            if (surfaceDist > range) return false;

            // ⚠️ ÖLÜ BÖLGE (HACİZ) — menzil gibi, o da YÜZEYE ölçülüyor. Merkeze
            // ölçseydim boss isabetindeki ölü bandın aynısını üretirdim, sadece ters
            // yönde: iri gövde ölü bölgeye taşar, oyuncu yayın içinde görür, vuruş
            // düşer. Aynı kavramın HER ölçümü aynı yerden (ders 9).
            if (minRange > 0f && surfaceDist < minRange) return false;

            float acisalYaricap = dist > e.Radius
                ? Mathf.Asin(Mathf.Clamp01(e.Radius / dist)) * Mathf.Rad2Deg
                : 90f;                                    // içindeysen her yön geçerli
            return Vector3.Angle(transform.forward, to) <= halfAngle + acisalYaricap;
        }

        void StrikeCone(float range, float angleDeg, float damage, float hitStop, float shake,
                        int maxTargets, float harvestMult, out int hits, out int kills,
                        float minRange = 0f)
        {
            hits = 0; kills = 0;
            float half = angleDeg * 0.5f;

            if (maxTargets == 1)
            {
                Enemy nearest = null;
                float bestDist = float.MaxValue;

                foreach (var e in Enemy.All)
                {
                    if (!InHitCone(e, range, half, out float d, minRange)) continue;
                    if (d < bestDist) { bestDist = d; nearest = e; }
                }

                if (nearest != null)
                {
                    bool wasHollow = nearest.Hollow;
                    nearest.Push(nearest.transform.position - transform.position, Weapon.Knockback(_t));
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
                    if (!InHitCone(e, range, half, out _, minRange)) continue;

                    bool wasHollow = e.Hollow;
                    e.Push(e.transform.position - transform.position, Weapon.Knockback(_t));
                    e.TakeDamage(damage, harvestMult);
                    hits++;
                    if (wasHollow) kills++;
                }
            }

            if (hits > 0)
            {
                Sfx.Hit();
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

            Clock.Instance?.Spend(secondsCost, fromHit: true);   // oda hesabında AYRI sayılıyor
            HitStop.Freeze(_t.hitStopDuration * 1.5f);
            IsoCamera.Shake(_t.hurtShake);

            Sfx.Hurt();
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
