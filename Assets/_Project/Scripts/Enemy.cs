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

        /// <summary>
        /// Boss kimliği — boss değilse null. Mekaniğini bu belirliyor (bkz. `BossKind`).
        /// </summary>
        public BossKind Boss { get; private set; }

        /// <summary>
        /// Boss mu — kendi akışı yavaş, saatini emiyor, beklenerek yenilemiyor.
        ///
        /// ⚠️ Bir zamanlar ayrı bir `bool` alandı ve `Boss` ile AYNI ŞEYİ söyleyen iki
        /// kaynak vardı. Üstündeki eski not "üçüncü bir özel düşman ihtiyacı çıkarsa
        /// birleştir" diyordu — Faz 12'de `BossKind` gelince o koşul gerçekleşti.
        /// Tek kaynağa indirildi: boss olmak = bir `BossKind`'ı olmak.
        /// </summary>
        public bool IsBoss => Boss != null;

        /// <summary>1 veya 2. Faz geçişi `CheckPhase`'de.</summary>
        public int Phase { get; private set; } = 1;

        /// <summary>
        /// ⚠️ Havuz FULLENDİ mi — fullendiyse artık KREDİ, vurunca HASAT VERMEZ.
        /// Gösterdiği para sahte; o zaman zaten onun değildi.
        /// </summary>
        public bool OnCredit { get; private set; }

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
                         * (Boss != null ? Boss.SpeedMultiplier(Phase) : 1f)   // TAHSİLDAR faz 2
                         * Kind.SpeedMultiplier
                         * (Fleeing ? _t.runnerFleeSpeedMultiplier : 1f);
        float HitCost => (Hollow ? _t.enemyHollowHitCost : _t.enemyFullHitCost)
                       * Region.HitCost                            // AĞIRLIK bölgesi kuralı
                       * (IsBoss ? _t.bossHitCostMultiplier : 1f);

        public EnemyKind Kind { get; private set; } = EnemyKind.Basic;

        /// <summary>Kaçak kaçıyor mu — havuzu eşiğin altına inince başlıyor.</summary>
        public bool Fleeing { get; private set; }

        float _startingPool;

        public void Init(Tuning tuning, Transform player, TimeLabel label,
                         float poolMultiplier = 1f, bool boss = false, EnemyKind kind = null,
                         BossKind bossKind = null)
        {
            _t = tuning;
            _player = player;
            _label = label;
            Boss = boss ? (bossKind ?? BossKind.For(0)) : null;   // IsBoss bundan türüyor
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
            TickBossPassives(Time.deltaTime);

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
                        // ⚠️ Tekrar ve ZİNCİRDE bekleme YOK — hemen devam ediyor.
                        // Araya bekleme koyarsak ikisi de sıradan iki saldırıya dönüşür
                        // ve sordukları soru ("yerleşemezsin") kaybolur.
                        if (_repeatsLeft > 0 || _chained != null) { EnterWindup(); break; }

                        _attackTimer = _t.enemyAttackCooldown;
                        EnterState(State.Chase, 0f);
                    }
                    break;
            }
        }

        int _attackCount;

        /// <summary>
        /// Şu anki saldırı 360° mü — halka, yani yön kontrolü yok, UZAKLAŞMAK gerekiyor.
        /// Ses ve öz-denetim buna bakıyor; değeri `_attack`'ten türüyor.
        /// </summary>
        bool _slamming;

        /// <summary>
        /// Şu anki boss saldırısı — boss değilse null, sıradan koni kullanılıyor.
        /// ⚠️ Eski `_slamming` bayrağı bunun ÖZEL BİR HÂLİ oldu: `Angle >= 360`.
        /// Tek bir özel saldırı yerine dağarcık var artık (bkz. `BossKind.Attacks`).
        /// </summary>
        BossAttack _attack;
        int _repeatsLeft;

        // Saldırı verisi varsa o belirliyor; yoksa sıradan düşman koni vuruşu.
        // ⚠️ Eskiden burada bir de `_slamming ? bossSlamRadius : ...` dalı vardı; yere
        // vuruş `BossAttack`'e taşınınca ÖLÜ KOD oldu (saldırı yokken `_slamming` da
        // false). Bırakılsaydı "hangi yol çalışıyor" sorusu iki cevaplı kalırdı.
        float StrikeRadius => _attack != null ? AttackRange * _attack.RangeMul : AttackRange;
        float StrikeAngle => _attack != null && _attack.Angle > 0f ? _attack.Angle : AttackAngle;
        float StrikeInner => _attack?.InnerRadius ?? 0f;
        Color StrikeColor => _attack?.Tint ?? StrikeTint;
        Color TelegraphColor => _attack?.Tint ?? TelegraphTint;

        /// <summary>
        /// Şu anki gerçek vuruş yarıçapı ve güvenli göbeği. Öz-denetim BUNLARI okuyor —
        /// `Tuning`'den yeniden hesaplamıyor. (`CurrentSpeed` ile aynı gerekçe: formül
        /// değişince test yeşil kalıp oyun bozulmasın.)
        /// </summary>
        public float CurrentStrikeRadius => StrikeRadius;
        public float CurrentStrikeInner => StrikeInner;

        void EnterWindup()
        {
            if (_repeatsLeft > 0)
            {
                _repeatsLeft--;                       // AYNI saldırı devam ediyor
            }
            else if (_chained != null)
            {
                // ZİNCİR: farklı bir saldırı hemen ardından geliyor. Bir cevaba
                // yerleşemiyorsun — halka çıkarttı, simit hemen içeri çağırıyor.
                SetAttack(_chained);
                _chained = null;
            }
            else
            {
                _attackCount++;
                PickAttack();
            }

            // ⚠️ MUTLAK süre çarpanı EZER: konum değiştirten saldırılara (güvenli açıklık,
            // simit) çarpan vermek yetmiyordu — boss hazırlıkları ~0.2 sn ve
            // "yeşil alana geçecek kadar süre kalmıyordu" (Enes).
            float dur = _attack != null && _attack.WindupSeconds > 0f
                      ? _attack.WindupSeconds
                      : WindupDuration * (_attack?.WindupMul ?? 1f);
            EnterState(State.Windup, dur);
            FacePlayer();

            // ⚠️ Hazırlık işareti artık DOLUYOR (0.25 → 1.0). Sabit yay "vuracağım" diyordu
            // ama "ne zaman" demiyordu — kaçma anını gözle kestiremiyordun. Dolan yay
            // kalan süreyi doğrudan gösteriyor: yay tamamlanınca darbe iniyor.
            // Boss'un yere vuruşunda bu daha da kritik, çünkü halka 360° ve kaçacak yer yok.
            _telegraph = Arc.Spawn(transform.position, transform.rotation,
                                   StrikeRadius, StrikeAngle, TelegraphColor, dur, transform,
                                   scaleFrom: 0.25f, scaleTo: 1f);

            // ⚠️ SİMİT'in güvenli göbeği GÖRÜNMELİ. Görünmezse oyuncu "halka geliyor,
            // kaç" der ve tam ters yönde ölür — mekanik doğru, sunum öldürücü olur.
            if (StrikeInner > 0f)
                Arc.Spawn(transform.position, transform.rotation, StrikeInner, 360f,
                          new Color(0.45f, 1f, 0.55f, 0.35f), dur, transform);

            // ⚠️ GÜVENLİ AÇIKLIK da görünmeli — arkada, yeşil. Görünmezse oyuncu
            // "halka geliyor, uzaklaş" der ve tam kaçamayacağı yerde ölür.
            // Boss'un ARKASINA bakan yönde çiziliyor.
            if (_attack != null && _attack.SafeAngle > 0f)
                Arc.Spawn(transform.position, transform.rotation * Quaternion.Euler(0f, 180f, 0f),
                          StrikeRadius, _attack.SafeAngle,
                          new Color(0.45f, 1f, 0.55f, 0.30f), dur, transform,
                          followYaw: 180f);   // ⚠️ olmazsa takip dönüşü sapmayı siler

            Paint();
        }

        /// <summary>
        /// Sıradaki saldırı. Deterministik: aynı sıra hep aynı saldırıyı verir, böylece
        /// "bu saldırı mı zor, ben mi kötü oynadım" cevaplanabilir kalıyor —
        /// yerleşimlerde ve tür seçiminde uygulanan kuralın aynısı.
        /// </summary>
        void PickAttack()
        {
            if (Boss == null)
            {
                // Boss olmayan düşman: sıradan koni vuruşu.
                _attack = null;
                _slamming = false;
                _repeatsLeft = 0;
                _chained = null;
                return;
            }

            var liste = Boss.Attacks(Phase);
            float mesafe = FlatDistanceToPlayer();

            // ⚠️ MESAFE FİLTRESİ. Enes: "dash attack yaparken olduğum yerde durunca
            // misliyor — uzaktayken dash attack yapsın, yakındayken normal vursun."
            // Hamleli saldırı yakından yapılınca oyuncunun ÜSTÜNDEN aşıyordu.
            // Sıra korunuyor: uygun olan İLK saldırıdan itibaren dönülüyor,
            // böylece determinizm bozulmuyor.
            int başlangıç = Mathf.Abs(_attackCount - 1) % liste.Length;
            for (int i = 0; i < liste.Length; i++)
            {
                var aday = liste[(başlangıç + i) % liste.Length];
                if (aday.MinPlayerRange > 0f && mesafe < aday.MinPlayerRange) continue;
                if (aday.MaxPlayerRange > 0f && mesafe > aday.MaxPlayerRange) continue;
                SetAttack(aday);
                return;
            }

            // Hiçbiri uymadıysa sıradakini yine de kullan — saldırısız kalmasın.
            SetAttack(liste[başlangıç]);
        }

        /// <summary>Saldırıyı kur — tekrar ve zincir durumu tek yerden ayarlanıyor.</summary>
        void SetAttack(BossAttack a)
        {
            _attack = a;
            _repeatsLeft = Mathf.Max(0, a.Repeat - 1);
            _chained = a.Then;                    // tekrarlar bitince zincire geçilir
            _slamming = a.Angle >= 360f;          // eski kavram, saldırıdan türüyor
        }

        BossAttack _chained;
        float _markTimer;

        void EnterStrike()
        {
            _strikeDir = transform.forward;                         // ← yön burada kilitlenir
            EnterState(State.Strike, _t.enemyStrikeDuration);

            if (_telegraph != null) { _telegraph.Cancel(); _telegraph = null; }
            // ⚠️ Yalnızca halka saldırısı ses çıkarıyor. Bir ara buraya `Sfx.Swing()`
            // eklemiştim — ama o OYUNCUNUN silah sesi. Düşmana verilince "ben mi
            // vurdum, düşman mı vurdu" ayırt edilemez oluyordu; sessizlikten kötü.
            // Düşman saldırılarının duyulması istenirse KENDİ sesi üretilmeli.
            if (_slamming) Sfx.Slam();

            // HAMLE: mesafeyi vuruş anında kapatıyor. Geri çekilmek tek başına
            // cevap olmaktan çıkıyor — hazırlığı okuyup YANA kaçman gerekiyor.
            if (_attack != null && _attack.Lunge > 0f)
                transform.position += _strikeDir * _attack.Lunge;

            Arc.Spawn(transform.position, transform.rotation,
                StrikeRadius, StrikeAngle, StrikeColor, _t.enemyStrikeDuration * 1.6f);
            Paint();

            // Hasar yalnızca bu anda, kilitli yöne göre iner. Kaçtıysan ıskalar.
            if (PlayerInStrikeCone())
            {
                float cost = HitCost * (_attack != null ? _attack.CostMul
                                      : (_slamming ? _t.bossSlamCostMultiplier : 1f));
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

        /// <summary>
        /// Yere vuruşta yön yok, yalnızca mesafe — yana kaçmak işe yaramıyor.
        ///
        /// ⚠️ KASITLI ASİMETRİ — kaza değil, karar (2026-08-26):
        /// Burada hem menzil hem açı oyuncunun MERKEZİNE ölçülüyor. Oyuncunun kendi
        /// vuruşu ise düşmanın YÜZEYİNE ölçülüyor (`PlayerController.InHitCone`).
        ///
        /// Yani oyuncunun vuruş kutusu cömert, hasar alma kutusu dar. Aksiyon
        /// oyunlarında standart pratik budur. Ölçülen pay: normal düşmanda 0.5 birim
        /// (menzilin %28'i) + 1.5 birim mesafede 19.5° açı.
        ///
        /// Turuncu yay bu kuralla DÜRÜST kalıyor: "merkezin buraya girmesin" diyor,
        /// izometrikte oyuncu zaten ayak noktasını okuyor.
        ///
        /// Faz 10'da (zamanlama/zorluk kilidi) yeniden bakılacak; simetrik yapılırsa
        /// düşmanlar belirgin şekilde daha çok vurur.
        /// </summary>
        public bool PlayerInStrikeCone()
        {
            Vector3 to = FlatToPlayer();
            float d = to.magnitude;
            if (d > StrikeRadius) return false;

            // ⚠️ SİMİT: göbeği GÜVENLİ. Halkanın tam tersi soru — "uzaklaş" değil "YAKLAŞ".
            if (StrikeInner > 0f && d <= StrikeInner) return false;

            // ⚠️ GÜVENLİ AÇIKLIK: halkanın ARKASINDA. Üçüncü soru — "uzaklaş" ya da
            // "yaklaş" değil, "ARKASINA GEÇ". Açıklık öne konsaydı boss hazırlık
            // boyunca oyuncuya döndüğü için oyuncu daima içinde kalır ve saldırı
            // hiç isabet etmezdi.
            float safe = _attack?.SafeAngle ?? 0f;
            if (safe > 0f && Vector3.Angle(-_strikeDir, to) <= safe * 0.5f) return false;

            // 360°'de yön kontrolü yok: her açı zaten içeride.
            return StrikeAngle >= 360f || Vector3.Angle(_strikeDir, to) <= StrikeAngle * 0.5f;
        }

        /// <summary>Öz-denetim ve HUD için: şu an hangi saldırıyı yapıyor.</summary>
        public BossAttack CurrentAttack => _attack;

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
                // ⚠️ KREDİ KURALI: fullenmiş havuz vurulunca erir ama HİÇBİR ŞEY VERMEZ.
                // O zaman zaten onun değildi. Bu olmasaydı ③④ fullendiği için havuzları
                // iki kez sömürülür ve son iki boss oyunun en büyük ZAMAN KAYNAĞI olurdu —
                // oysa boss'un seni beslememesi gerekiyor (ölçüm de öyle diyordu).
                float gain = OnCredit ? 0f
                           : drained * clock.HarvestRate * harvestMult
                             * Kind.HarvestMultiplier
                             * Region.Harvest                      // KITLIK bölgesi kuralı
                             * BossKind.ActiveHarvestMultiplier()  // EKSPER'in suçu
                             * (IsBoss ? _t.bossHarvestMultiplier : 1f);
                clock.Add(gain);

                // Zaman ondan sana AKIYOR — kazanç sıfırsa (KASA) hiçbir şey akmaz,
                // ve bu doğru: KASA'ya vurmak gerçekten hiçbir şey vermiyor.
                if (gain > 0f)
                    Mote.Spawn(MoteOrigin(), _player, Mote.HarvestColor,
                               size: 0.16f + Mathf.Min(gain, 3f) * 0.05f);   // çok çaldıysan büyük görün
            }

            CheckPhase();

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

            float aura = (Boss != null ? Boss.AuraDrain(_t, Phase)
                                       : (IsBoss ? _t.bossAuraDrain : 0f))
                       + Kind.AuraDrain(_t);
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

        /// <summary>
        /// Boss'un pasif yetenekleri — MURABAHACI'nın geri sarması, SİMSAR'ın yer
        /// işaretleri. Aura DEĞİL.
        ///
        /// ⚠️ Bunlar bir ara `ApplyAura` içindeydi ve o adı YALANCI yapıyordu:
        /// "aura uygula" diye çağırıp bomba bıraktırıyordu. Fonksiyonun adı ne
        /// yaptığını söylemiyorsa bir sonraki okuyan yanlış yerde arar.
        /// `dt` yine dışarıdan — editör modunda `Time.deltaTime` 0.
        /// </summary>
        public void TickBossPassives(float dt)
        {
            if (Boss == null || Hollow || _t == null) return;

            // MURABAHACI faz 2: vurmadığın her saniye havuzu doluyor. Ondan HIZLI
            // olmak zorundasın. Krediyi doldurmak hasat vermiyor, dövüşü uzatıyor.
            float regen = Boss.RegenPerSecond(_t, Phase);
            if (regen > 0f)
            {
                TimeLeft = Mathf.Min(_startingPool, TimeLeft + regen * dt);
                if (_label != null) _label.Set(TimeLeft, Hollow);
            }

            // SİMSAR: oyuncunun DURDUĞU YERE gecikmeli patlama bırakıyor. Diğer
            // boss'ların hepsi oyuncuya GİDİYOR, yani "dur ve vur" geçerli kalıyordu
            // (Enes 1200 saniyeye öyle çıktı). Bu ona gelmiyor, durduğu yere geliyor.
            float aralık = Boss.GroundMarkInterval(Phase);
            if (aralık <= 0f || _player == null) return;

            _markTimer -= dt;
            if (_markTimer > 0f) return;

            _markTimer = aralık;
            GroundMark.Spawn(_t, _player.position, _player,
                             radius: _t.groundMarkRadius,
                             delay: _t.groundMarkDelay,
                             cost: HitCost * _t.groundMarkCostMultiplier,
                             parent: transform.parent);
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
        /// Boss faz geçişi. İki farklı kural (Enes kararı):
        ///   ① ② havuz YARIYA düşünce döner
        ///   ③ ④ havuz BİTİNCE fullenip döner — "kazandım" sandığın an
        ///
        /// ⚠️ Fullenen havuz KREDİ olarak işaretleniyor: erir ama hasat vermez.
        /// </summary>
        void CheckPhase()
        {
            if (Boss == null || Phase >= 2) return;

            if (Boss.RefillsOnEmpty)
            {
                if (TimeLeft > 0f) return;
                TimeLeft = _startingPool;      // FULLENİR
                OnCredit = true;               // ...ama artık sahte para
            }
            else if (TimeLeft > _startingPool * Boss.PhaseTwoAt) return;

            EnterPhaseTwo();
        }

        void EnterPhaseTwo()
        {
            Phase = 2;
            PhaseTwoAnnouncement = Time.unscaledTime;

            // Dağarcık değişti: sıradaki saldırı YENİ listeden seçilsin.
            _attack = null;
            _repeatsLeft = 0;
            _chained = null;

            // Faz değişimi GÖRÜNÜR olmalı: oyuncu kuralların değiştiğini anlamadan
            // ölürse "haksızlık" der. Halka + sarsıntı + ses aynı anda.
            Arc.Spawn(transform.position, transform.rotation, Radius * 6f, 360f,
                      new Color(Boss.Tint.r, Boss.Tint.g, Boss.Tint.b, 0.75f), 0.8f,
                      scaleFrom: 0.2f, scaleTo: 2.4f);
            Sfx.Slam();
            IsoCamera.Shake(_t.hitShake * 2f);
            HitStop.Freeze(_t.hitStopDuration * 2f);

            Debug.Log($"[Tek Can] {Boss.Name} FAZ 2 — {Boss.Crime}" +
                      (OnCredit ? "  (havuz KREDİ, hasat vermez)" : ""));

            if (Boss.SplitsOnPhaseTwo) Split();
            Paint();
        }

        /// <summary>Faz 2 ne zaman başladı — HUD duyurusu için.</summary>
        public float PhaseTwoAnnouncement { get; private set; } = -99f;

        /// <summary>
        /// KEFİL faz 2'de ikiye bölünüyor: kendi havuzu yarılanıyor, aynısından bir
        /// tane daha doğuyor. "Borcu garantiye alan adam kendini de garantiliyor."
        /// ⚠️ İkizi de KREDİ üstünde — ikisinden de hasat alınmıyor.
        /// </summary>
        void Split()
        {
            TimeLeft *= 0.5f;

            var yan = transform.right * Radius * 2.2f;
            var ikiz = Bootstrap.SpawnEnemyAt(_t, transform.position + yan, _player, null,
                                              poolMultiplier: 1f, boss: true, kind: Kind,
                                              parent: transform.parent, bossKind: Boss);

            // ⚠️ Doğanı DOĞRUDAN alıyoruz. Sahnede "aynı türden faz 1'de olan" diye
            // aramak, ikinci bir bölünmede yanlışını bulabilirdi.
            if (ikiz != null) ikiz.BecomeTwin(TimeLeft);
        }

        /// <summary>Bölünmeden doğan ikiz — `Split` çağırıyor.</summary>
        void BecomeTwin(float pool)
        {
            TimeLeft = pool;
            _startingPool = pool;
            Phase = 2;
            OnCredit = true;
            transform.localScale = _baseScale * 0.8f;
            Paint();
        }

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
            // ⚠️ Bu renkler TAŞIYICI, süs değil: dövüş kararları bunlara bakıyor.
            // Faz 7'de efektler eklenirken kural buydu — efekt bu renkleri örterse
            // efekt gider, okunurluk kalır. Playtest'te doğrulandı ("okunuyor").
            //
            // Efektler renge EK olarak çalışıyor, yerine değil:
            // isabet halkası (TakeDamage) · boşalma halkası (OnWentHollow) · ölüm halkası (Kill).
        }
    }
}
