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

        /// <summary>Aura zerresi kısma sayacı — gerekçe `ApplyAura` içinde.</summary>
        const float AuraMoteInterval = 0.16f;
        float _auraMoteTimer;
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
        // ponytail: boss `IsBoss` bayrağıyla, diğer çeşitler `EnemyKind` ile çalışıyor —
        // aynı iş için iki mekanizma. `BossKind : EnemyKind` yapılabilir ama boss dalga
        // seviyesinde bir kavram (WaveManager kazanmayı ondan anlıyor), taşımak
        // WaveManager + Bootstrap + testleri birden dolaşır. Üçüncü bir "özel düşman"
        // ihtiyacı çıkarsa o zaman birleştir; iki mekanizma için erken.
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
                if (Hollow && !wasHollow) { Paint(); OnWentHollow(); }
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

        int _attackCount;
        bool _slamming;

        /// <summary>
        /// Boss'un yere vuruşu. Enes: "dümdüz durup saldırarak kesiyorum."
        /// Sorun sayıda değildi — boss'un oyuncuyu YERİNDEN KALDIRAN bir saldırısı yoktu,
        /// normal düşmanla aynı yapay zekâyı kullanıyordu.
        ///
        /// Yere vuruş 360° halka: yanına kaçmak işe yaramıyor, uzaklaşman gerekiyor.
        /// Uzaklaşmak da vurmayı kesmek demek, yani dövüş uzuyor. Bedeli o.
        /// Hazırlığı uzun tutuldu — sürpriz değil, karar olmalı.
        /// </summary>
        static readonly Color SlamTint = new Color(1f, 0.3f, 0.1f, 0.28f);

        // Yere vuruş, ölçeklenen boss özellikleriyle aynı deseni kullanıyor:
        // koşul tek yerde, kullanan yerler sade kalıyor.
        float StrikeRadius => _slamming ? _t.bossSlamRadius : AttackRange;
        float StrikeAngle => _slamming ? 360f : AttackAngle;
        Color StrikeColor => _slamming ? SlamTint : StrikeTint;
        Color TelegraphColor => _slamming ? SlamTint : TelegraphTint;

        void EnterWindup()
        {
            _attackCount++;
            _slamming = IsBoss && _t.bossSlamEvery > 0 && _attackCount % _t.bossSlamEvery == 0;

            float dur = WindupDuration * (_slamming ? _t.bossSlamWindupMultiplier : 1f);
            EnterState(State.Windup, dur);
            FacePlayer();

            // ⚠️ Hazırlık işareti artık DOLUYOR (0.25 → 1.0). Sabit yay "vuracağım" diyordu
            // ama "ne zaman" demiyordu — kaçma anını gözle kestiremiyordun. Dolan yay
            // kalan süreyi doğrudan gösteriyor: yay tamamlanınca darbe iniyor.
            // Boss'un yere vuruşunda bu daha da kritik, çünkü halka 360° ve kaçacak yer yok.
            _telegraph = Arc.Spawn(transform.position, transform.rotation,
                                   StrikeRadius, StrikeAngle, TelegraphColor, dur, transform,
                                   scaleFrom: 0.25f, scaleTo: 1f);
            Paint();
        }

        void EnterStrike()
        {
            _strikeDir = transform.forward;                         // ← yön burada kilitlenir
            EnterState(State.Strike, _t.enemyStrikeDuration);

            if (_telegraph != null) { _telegraph.Cancel(); _telegraph = null; }
            if (_slamming) Sfx.Slam();
            Arc.Spawn(transform.position, transform.rotation,
                StrikeRadius, StrikeAngle, StrikeColor, _t.enemyStrikeDuration * 1.6f);
            Paint();

            // Hasar yalnızca bu anda, kilitli yöne göre iner. Kaçtıysan ıskalar.
            if (PlayerInStrikeCone())
            {
                float cost = HitCost * (_slamming ? _t.bossSlamCostMultiplier : 1f);
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

        /// <summary>Yere vuruşta yön yok, yalnızca mesafe — yana kaçmak işe yaramıyor.</summary>
        public bool PlayerInStrikeCone()
        {
            Vector3 to = FlatToPlayer();
            if (to.magnitude > StrikeRadius) return false;

            // Yere vuruşta yön kontrolü yok: 360° olduğu için her açı zaten içeride.
            return _slamming || Vector3.Angle(_strikeDir, to) <= StrikeAngle * 0.5f;
        }

        /// <summary>Öz-denetim için: şu an yere vuruş yapıyor mu.</summary>
        public bool Slamming => _slamming;

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

            var clock = Clock.Instance;
            if (clock != null)
            {
                // Kind.HarvestMultiplier 0 olabilir (KASA): vurarak hiçbir şey alamazsın,
                // bütün ödeme öldürme anında gelir.
                // Boss'un hasatı düşük: uzun bir dövüş, ama seni beslemiyor. Yoksa tankiliği
                // onu zorlaştırmaz, ödül yapardı — havuzu hem canı hem ödemesi olduğu için.
                float gain = drained * clock.HarvestRate * harvestMult
                             * Kind.HarvestMultiplier
                             * (IsBoss ? _t.bossHarvestMultiplier : 1f);
                clock.Add(gain);

                // Zaman ondan sana AKIYOR — kazanç sıfırsa (KASA) hiçbir şey akmaz,
                // ve bu doğru: KASA'ya vurmak gerçekten hiçbir şey vermiyor.
                if (gain > 0f)
                    Mote.Spawn(MoteOrigin(), _player, Mote.HarvestColor,
                               size: 0.16f + Mathf.Min(gain, 3f) * 0.05f);   // çok çaldıysan büyük görün
            }

            _flashTimer = _t.hitFlashDuration;

            // İsabet patlaması: dışa açılan kısa halka. Beyaz parlama "değdi" diyor,
            // bu "SERT değdi" diyor — hit-stop ve sarsıntıyla aynı anda.
            Arc.Spawn(transform.position, transform.rotation, 0.55f, 360f,
                      new Color(1f, 0.95f, 0.8f, 0.75f), 0.13f,
                      scaleFrom: 0.35f, scaleTo: 1.7f);

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
            if (aura <= 0f) return;

            Clock.Instance?.Spend(aura * dt);

            // ⚠️ Zerreyi KISARAK çıkarıyoruz. Aura her karede işliyor; her karede zerre
            // doğursaydık saniyede ~60 nesne olurdu — hem çöp hem ekran karmaşası.
            // Kısılmış akış "sürekli emiliyor" hissini zaten veriyor.
            _auraMoteTimer -= dt;
            if (_auraMoteTimer > 0f || _player == null) return;
            _auraMoteTimer = AuraMoteInterval;

            // Yön TERS: zaman SENDEN ona gidiyor. Rengi de ters (kırmızı).
            Mote.Spawn(_player.position + Vector3.up * 0.6f, transform, Mote.DrainColor,
                       size: 0.18f, life: 0.45f);
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

        /// <summary>Zerrenin çıktığı nokta — kapsülün gövde ortası, ayağı değil.</summary>
        Vector3 MoteOrigin() => transform.position + Vector3.up * 0.5f;

        /// <summary>
        /// Havuz bitti: artık tek atımlık. Bu, dövüşün en önemli durum değişimi —
        /// "bunu bırak, sıradakine geç" ya da "bir vuruşluk iş kaldı" kararı buna bağlı.
        ///
        /// Renk değişimi tek başına sessizdi: bakmıyorsan kaçırıyordun. Halka gözü
        /// çekiyor, çünkü hareket renkten daha görünür.
        /// </summary>
        void OnWentHollow()
        {
            Arc.Spawn(transform.position, transform.rotation, Radius * 2.2f, 360f,
                      new Color(0.75f, 0.75f, 0.82f, 0.55f), 0.30f,
                      scaleFrom: 0.5f, scaleTo: 1.35f);
        }

        void Kill()
        {
            if (_telegraph != null) _telegraph.Cancel();   // ölünce uyarı yayı ortada kalmasın

            // Ölüm halkası TÜRÜN renginde: kalabalıkta neyi devirdiğin okunuyor.
            // Boss daha geniş — devrilişi olay olmalı, sıradan bir düşman gibi sönmemeli.
            Arc.Spawn(transform.position, transform.rotation,
                      Radius * (IsBoss ? 5f : 2.6f), 360f,
                      new Color(Kind.FullColor.r, Kind.FullColor.g, Kind.FullColor.b, 0.7f),
                      IsBoss ? 0.65f : 0.28f,
                      scaleFrom: 0.35f, scaleTo: IsBoss ? 2.2f : 1.6f);

            // ⚠️ Ödemeyi TÜRE SORMUYORUZ, saati ölçüyoruz. KASA toplu öder, TEMEL hiç
            // ödemez, ileride başka tür başka türlü ödeyebilir — ölçüm hepsinde çalışır,
            // `EnemyKind`'a tek satır eklemeden. (Silah/tür deseninin bozulmaması için.)
            float before = Clock.Instance != null ? Clock.Instance.Seconds : 0f;
            Kind.OnKilled(Clock.Instance, _t, _startingPool);   // KASA bütün ödemesini burada yapar
            float paid = (Clock.Instance != null ? Clock.Instance.Seconds : 0f) - before;

            if (paid > 0f && _player != null)
            {
                // Toplu ödeme toplu görünmeli: tek zerre "kasa patladı" demiyor.
                int n = Mathf.Clamp(Mathf.RoundToInt(paid), 3, 9);
                for (int i = 0; i < n; i++)
                {
                    var jitter = new Vector3(Random.Range(-0.45f, 0.45f), Random.Range(-0.3f, 0.5f),
                                             Random.Range(-0.45f, 0.45f));
                    Mote.Spawn(MoteOrigin() + jitter, _player, Mote.HarvestColor,
                               size: 0.20f, life: Random.Range(0.30f, 0.52f),   // dağınık varış = patlama
                               arcHeight: Random.Range(0.6f, 1.5f));
                }
            }

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
