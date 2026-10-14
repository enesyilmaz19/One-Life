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

        /// <summary>1, 2 ya da 3 (son boss). Faz geçişi `CheckPhase`'de.</summary>
        public int Phase { get; private set; } = 1;

        /// <summary>
        /// UYKUDA — vurulmuyor, vurmuyor, kovalamıyor.
        ///
        /// Son boss faz 3'te önceki boss'ları çağırıyor ve onlar ölene kadar
        /// kendisi geri çekiliyor (Enes: *"hepsi ölünce kendisi gelsin"*).
        /// ⚠️ Uyku olmadan oyuncu BEŞ boss'la aynı anda dövüşürdü — final değil
        /// kaza olurdu. Uyku aynı zamanda ANLATI: adam öne geçmiyor, borcunu
        /// tahsil ettirmek için adamlarını gönderiyor.
        /// </summary>
        public bool Dormant { get; private set; }

        /// <summary>
        /// Bu, son boss'un çağırdığı bir RAKİP mi.
        /// ⚠️ Faz değiştirmiyorlar: çağrılan boss da faz 2'ye geçip kendi
        /// yardımını çağırsaydı zincirleme bir doğum patlaması olurdu. Onlar
        /// birer YANKI, tam boss değil.
        /// </summary>
        public bool SummonedRival { get; private set; }

        /// <summary>
        /// NEMESİS — bu boss'un BU DÖVÜŞTE senden aldığı toplam saniye.
        /// Ölürsen hesabına geçiyor; bir dahaki karşılaşmada havuzuna ekleniyor.
        /// </summary>
        public float StolenFromPlayer { get; private set; }

        /// <summary>Oyuncudan GERÇEKTEN inen bedeli hesaba yaz — yalnızca boss hesap tutar.</summary>
        public void HesabınaYaz(float saniye)
        {
            if (IsBoss && saniye > 0f) StolenFromPlayer += saniye;
        }

        /// <summary>
        /// Hesap diske geçti, sıfırla. ⚠️ Sıfırlanmasaydı aynı dövüşte ikinci ölüm
        /// (ÖLÜM ALDATMA → tekrar ölüm) birikmiş TOPLAMI ikinci kez yazıyordu ve boss
        /// bir dahaki karşılaşmada hak etmediği kadar büyük geliyordu (denetim 2026-09-23).
        /// </summary>
        public void HesabıKapat() => StolenFromPlayer = 0f;

        /// <summary>Bu boss daha önce seni öldürdüyse havuzuna eklenen borç.</summary>
        public float NemesisDebt { get; private set; }

        /// <summary>
        /// Son boss'un çağırdığı rakip olarak işaretle.
        ///
        /// ⚠️ Nemesis borcu GERİ ALINIYOR: yankıların kendi hesabı yok. Borç
        /// `Init`'te ekleniyor (o an bunun rakip olduğu henüz bilinmiyor), burada
        /// düşülüyor. Kalsaydı finalde aynı borcu İKİ KEZ ödetirdik — bir kez
        /// rakipte, bir kez asıl karşılaşmada.
        /// </summary>
        public void MarkAsSummonedRival()
        {
            SummonedRival = true;
            if (NemesisDebt <= 0f) return;
            TimeLeft = Mathf.Max(0.1f, TimeLeft - NemesisDebt);
            _startingPool = Mathf.Max(0.1f, _startingPool - NemesisDebt);
            NemesisDebt = 0f;
        }

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
        /// <summary>
        /// Öz-denetim için: TEMEL hazırlık (formül kopyalanmasın). ⚠️ Saldırının
        /// çarpanı / mutlak süresi ve tepki payı HARİÇ — gerçek süre `EnterWindup`'ta.
        /// </summary>
        public float CurrentWindup => WindupDuration;

        float WindupDuration => _t.enemyWindupDuration
                              * (Hollow ? _t.enemyHollowWindupMultiplier : 1f)
                              * (IsBoss ? _t.bossWindupMultiplier : 1f)
                              * Rank.WindupMultiplier;   // İRİ: boss gibi ağır telegraf
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
                         * Rank.SpeedMultiplier                                // ZIRHLI / İRİ ağır
                         * Mathf.Min(Growth, 1.5f)                             // ZAMANAŞIMI (hafif)
                         * (Fleeing ? _t.runnerFleeSpeedMultiplier : 1f);
        float HitCost => (Hollow ? _t.enemyHollowHitCost : _t.enemyFullHitCost)
                       * Region.HitCost                            // AĞIRLIK bölgesi kuralı
                       * Growth                                    // ZAMANAŞIMI
                       * Rank.HitCostMultiplier(_t)                // İRİ: daha çok hasar
                       * (IsBoss ? _t.bossHitCostMultiplier : 1f)
                       * İlerleme();                               // İLERLEDİKÇE pahalı (Faz 28)

        float İlerleme()
        {
            var rm = RoomManager.Instance;
            return rm != null ? RoomManager.İlerlemeÇarpanı(_t, rm.RegionIndex, rm.SubRegion) : 1f;
        }

        /// <summary>
        /// ZAMANAŞIMI'nın büyüme çarpanı — yaşadığı süreyle artıyor.
        /// ⚠️ TAVANLI: tavansız bırakılsaydı uzun bir odada tek düşman koşuyu bitirirdi.
        /// Ceza öteleme için, imkânsızlık için değil.
        ///
        /// ⚠️ İRİ büyüme HIZINI katlıyor, TAVANI değil. Tavan "tek düşman koşuyu
        /// bitiremez" güvenliği; onu da katlasaydım mini boss o güvenliği delerdi.
        /// Hızın katlanması "çok gelişmiş hal"i zaten veriyor: tavana çok daha
        /// çabuk varıyor.
        /// </summary>
        public float Growth => Kind.GrowthPerSecond <= 0f ? 1f
                             : Mathf.Min(_t.lapseGrowthCap,
                                         1f + Kind.GrowthPerSecond * Rank.TraitMultiplier(_t) * _aliveTime);

        float _aliveTime;

        public EnemyKind Kind { get; private set; } = EnemyKind.Basic;

        /// <summary>Türün üstüne binen ağırlık katmanı — ZIRHLI / İRİ. Boss'lara uygulanmaz.</summary>
        public EnemyRank Rank { get; private set; } = EnemyRank.Normal;

        /// <summary>
        /// Ekranda görünmesi gereken tür rengi — rütbe kayması DAHİL.
        /// ⚠️ Boyayan her yer buradan geçmeli. `Kind.FullColor`'ı doğrudan okumak,
        /// zırhın kaçarken ya da ölürken sessizce kaybolması demekti (ders 9:
        /// aynı kavramın HER ölçümü düzeltilir, biri değil).
        /// </summary>
        public Color KindColor => Rank.Shade(Kind.FullColor);

        /// <summary>Kaçak kaçıyor mu — havuzu eşiğin altına inince başlıyor.</summary>
        public bool Fleeing { get; private set; }

        float _startingPool;

        public void Init(Tuning tuning, Transform player, TimeLabel label,
                         float poolMultiplier = 1f, bool boss = false, EnemyKind kind = null,
                         BossKind bossKind = null, EnemyRank rank = null)
        {
            _t = tuning;
            _player = player;
            _label = label;
            Boss = boss ? (bossKind ?? BossKind.For(0)) : null;   // IsBoss bundan türüyor
            Kind = kind ?? EnemyKind.Basic;

            // ⚠️ Boss RÜTBE ALMAZ. Boss'un zaten kendi faz/havuz düzeneği var; üstüne
            // zırh binseydi iki ayrı dayanıklılık sistemi çarpışır ve boss dövüşü
            // ayarlanamaz hale gelirdi. Rütbe sıradan odaların meselesi.
            Rank = boss ? EnemyRank.Normal : (rank ?? EnemyRank.Normal);

            TimeLeft = tuning.enemyTimePool * poolMultiplier
                     * Kind.PoolMultiplier * Rank.PoolMultiplier(tuning);

            // ⚠️ NEMESİS: seni öldüren boss, senden aldığı zamanı HAVUZUNA ekleyerek
            // geri geliyor. Sayı uydurulmuyor — defterde ne yazıyorsa onun payı.
            if (Boss != null)
            {
                // ⚠️ TAVANLI. Tavansız bırakılsaydı yeterince ölen oyuncu için boss
                // matematiksel olarak YENİLEMEZ olurdu ve kalıcı ölüm bir ceza değil
                // bir DUVAR olurdu. Nemesis hikâye anlatmalı, koşuyu kilitlememeli.
                float tavan = TimeLeft * tuning.nemesisMaxShare;
                // Mini boss'un defteri ana boss'unki DEĞİL — toparlanma defteri (Faz 28).
                float defter = Boss is MiniBossKind mini ? SaveFile.MiniDefter(mini.AraBölge) : SaveFile.BossLedger(Boss);
                NemesisDebt = Mathf.Min(defter * tuning.nemesisPoolShare, tavan);
                TimeLeft += NemesisDebt;
            }

            _startingPool = TimeLeft;

            // Ölçek rütbeden SONRA yakalanıyor: `_baseScale` isabet darbesinin ve
            // `Radius`'un dayanağı, İRİ'nin gövdesi ikisine de yansımalı.
            if (Rank.ScaleMultiplier != 1f) transform.localScale *= Rank.ScaleMultiplier;

            // ⚠️ NEMESİS GÖRÜNMELİ. `TASARIM.md` bunu açıkça istiyor: "tekrar karşına
            // çıktığında GÖZLE GÖRÜLÜR ŞEKİLDE değişmiş olur." İlk yazışımda yalnızca
            // HUD satırı koymuştum — yani boss sessizce şişiyordu ve fark yalnızca
            // yazıdan anlaşılıyordu. Mekanik doğru, gövde yalan (ders 10).
            //
            // ⚠️ Ölçek havuzun KÖKÜYLE büyüyor, doğrusal değil: borç iki katına
            // çıkınca gövde iki katına çıksaydı ekrana sığmazdı ve isabet yarıçapı
            // (`Radius`) da onunla saçmalardı.
            if (NemesisDebt > 0.05f)
            {
                float oran = 1f + NemesisDebt / Mathf.Max(0.01f, TimeLeft - NemesisDebt);
                transform.localScale *= Mathf.Min(1.6f, Mathf.Sqrt(oran));
            }
            _baseScale = transform.localScale;
            _renderer = GetComponentInChildren<Renderer>();
            All.Add(this);
            Paint();
        }

        void OnDestroy() => All.Remove(this);

        // ── GERİ İTME ────────────────────────────────────────────────────────
        // ⚠️ Enes: *"faizli kodu mu oturtmuyor."* Hit-stop ve sarsıntıyı büyütmüştüm
        // ama asıl eksik BU'ydu: ağır bir vuruş vurduğu şeyi HAREKET ETTİRİR, ve
        // oyunda düşmanı iten hiçbir şey yoktu. Var olanı büyütmek, olmayanın
        // yerini tutmuyor.
        Vector3 _push;

        /// <summary>Silahın darbesiyle geri savrul. Yatay — y karışmasın (tuzak 2).</summary>
        public void Push(Vector3 dir, float force)
        {
            if (force <= 0f) return;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            _push = dir.normalized * force;
        }


        /// <summary>⚠️ dt DIŞARIDAN — editör modunda `Time.deltaTime` 0 (tuzak 3).</summary>
        public void TickPush(float dt)
        {
            if (_push.sqrMagnitude < 0.0001f) { _push = Vector3.zero; return; }

            var next = transform.position + _push * dt;

            // ⚠️ İTME ASLA KAÇIŞA SEBEP OLAMAZ — ve arena dışına çıkaramaz.
            //
            // `ChaseMove` düşmanı hep oyuncuya doğru yürüttüğü için `Enemy`'de hiç
            // sınır kontrolü yoktu; ihtiyaç da yoktu. Geri itme onları DIŞARI
            // sürükleyen ilk şey, yani sınırı ilk gerektiren şey de bu.
            //
            // Sonucu ters yüz bir ceza olurdu: kaçan KAÇAK'ı FAİZLİ ile vurunca
            // onu kaçış yarıçapının dışına iterdin — yani oyunun EN PAHALI silahının
            // (savurma başına saniye ödüyor) EN İYİ vuruşu düşmanı kaçırtır ve
            // zamanını götürürdü. Kaçmak, kaçağın seni geçmesi olmalı; senin ona
            // iyi vurman değil.
            float tavan = Fleeing ? _t.runnerEscapeRadius - 0.5f : _t.arenaRadius;
            var düz = new Vector2(next.x, next.z);
            if (düz.magnitude > tavan)
            {
                düz = düz.normalized * tavan;
                next.x = düz.x;
                next.z = düz.y;
            }

            transform.position = next;
            _push = Vector3.Lerp(_push, Vector3.zero, 11f * dt);
            if (_push.sqrMagnitude < 0.0001f) _push = Vector3.zero;
        }

        void Update()
        {
            if (_t == null || _player == null) return;

            // ⚠️ UYKU HER ŞEYİ DURDURUR — ve pasifler ÖNCE çalışır çünkü UYANDIRAN onlar.
            //
            // İlk yazışımda uykuyu yalnızca `TakeDamage` ve `TickBossPassives`
            // içinde kapatmıştım. `Update`'in geri kalanı çalışmaya devam ediyordu:
            // uykudaki boss KOVALIYOR, VURUYOR ve aurasıyla zaman EMİYORDU — üstelik
            // vurulamıyordu. Yani ÖLDÜRÜLEMEZ BİR SALDIRGAN. Belge yorumu
            // "vurulmuyor, vurmuyor, kovalamıyor" diyordu; kod yalnızca birincisini
            // yapıyordu. Yorumun doğru olması kodun doğru olduğu anlamına gelmiyor.
            // ⚠️ İsabet parlaması UYKUDAN ÖNCE söner. Faz 3'e geçiş hep bir vuruşun
            // içinde oluyor ve o vuruş parlamayı başlatıyor; sönme uykudan sonra
            // kalsaydı boss rakip geçidi boyunca bembeyaz kalırdı, uyku rengi hiç
            // görünmezdi (denetim 2026-09-23).
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.unscaledDeltaTime;   // hit-stop sırasında da sönsün

                // Ölçek darbesi: isabetin en ucuz ve en okunur işareti. Renk solsa da görünür.
                float k = Mathf.Clamp01(_flashTimer / Mathf.Max(0.0001f, _t.hitFlashDuration));
                transform.localScale = _baseScale * (1f + 0.28f * k);

                if (_flashTimer <= 0f) { transform.localScale = _baseScale; Paint(); }
            }

            // ⚠️ Koşu BİTTİ (ölüm ya da zafer): dövüş durur. Durmasaydı "OYUN BİTTİ"
            // ekranında düşmanlar vurmaya devam ediyor, her vuruş hit-stop, sarsıntı
            // ve acı sesi üretiyordu — saat düşmese de ekran "hâlâ dövüşüyorsun" diyordu.
            var saat = Clock.Instance;
            if (saat != null && (saat.Dead || saat.Won)) return;

            if (Dormant)
            {
                TickBossPassives(Time.deltaTime);   // rakipler öldü mü diye BURASI bakıyor
                if (Dormant)                        // hâlâ uykuda: akış, aura, saldırı YOK
                {
                    // ⚠️ Etiket uykuda da güncel: faz 3'e geçiş havuzu FULLÜYOR ama etiket
                    // geçişten önceki son değerde (6.6) donuyordu — "bitmek üzere" yalanı
                    // (otopilot izleme oturumu, 2026-10-09).
                    if (_label != null) _label.Set(TimeLeft, Hollow);
                    return;
                }
            }

            bool wasHollow = Hollow;

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
            TickKindPassives(Time.deltaTime);

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

            TickState(Time.deltaTime);
            if (_label != null) _label.Set(TimeLeft, Hollow);
        }

        // ── saldırı evreleri ─────────────────────────────────────────────────
        // Hazırlığın İLK kısmında oyuncuyu takip eder, SON kısmında (tepki payı)
        // yön KİLİTLİ ve uyarı yayı donuk — dodge burada mümkün olur.
        //
        // ⚠️ Faz 25'e kadar takip vuruş anına kadar sürüyordu. Boss hazırlığı
        // 0.1–0.25 sn, insan tepkisi ~0.25 sn: koni seni takip ediyor, kaçış ancak
        // son karede dash ile. Enes: *"düşmanlar hareketlerimize anlık tepki veriyor."*

        /// <summary>Hazırlığın sonunda yönün kilitli kaldığı süre (saniye).</summary>
        float TepkiPayı => IsBoss ? _t.bossReactionWindow : _t.enemyReactionWindow;

        /// <summary>
        /// Hazırlıkta hâlâ oyuncuya dönüyor mu.
        /// ⚠️ GÜVENLİ AÇIKLIKLI saldırıda HİÇ dönmüyor: açıklık boss'un arkasında ve
        /// boss dönerken açıklık da dönüp hep oyuncunun tam karşısına kaçıyordu —
        /// SİMSAR'ın halkasından kaçmak imkânsızdı (Enes, 2026-09-30).
        /// ⚠️ HARİTA saldırısında da dönmüyor (Faz 26): arena boyu bir ayna ya da ışın
        /// sana dönmeye devam etseydi yana kaçmak imkânsız olurdu.
        /// </summary>
        bool HazırlıktaTakip => _stateTimer > TepkiPayı
                             && (_attack == null || (_attack.SafeAngle <= 0f && !_attack.Harita));

        /// <summary>⚠️ dt DIŞARIDAN — öz-denetim hazırlığı adım adım koşturabilsin (tuzak 3).</summary>
        void TickState(float dt)
        {
            _stateTimer -= dt;

            // ⚠️ Savrulma DURUMDAN BAĞIMSIZ: hazırlıktayken de vurulunca savrulmalı,
            // yoksa "ağır darbe" yalnızca kovalarken görünür ve tesadüfi hissedilir.
            TickPush(dt);

            switch (_state)
            {
                case State.Chase:
                    _attackTimer -= dt;
                    ChaseMove(dt);
                    if (_attackTimer <= 0f && SaldırıyaHazır(dt)) EnterWindup();
                    break;

                case State.Windup:
                    if (HazırlıktaTakip) FacePlayer();
                    if (_stateTimer <= 0f) EnterStrike();
                    break;

                case State.Strike:
                    transform.position += _strikeDir *
                        (_t.enemyLungeDistance / Mathf.Max(0.0001f, _t.enemyStrikeDuration)) * dt;
                    if (_stateTimer <= 0f) EnterState(State.Recover, _t.enemyRecoverDuration);
                    break;

                case State.Recover:
                    if (_stateTimer <= 0f)
                    {
                        // ⚠️ Tekrar ve ZİNCİRDE saldırı BEKLEMESİ YOK — hemen sıradaki hazırlık.
                        // Araya bekleme koyarsak ikisi de sıradan iki saldırıya dönüşür
                        // ve sordukları soru ("yerleşemezsin") kaybolur.
                        // (Her halkanın kendi hazırlığı ve TEPKİ PAYI var — bilerek: 0.1 sn'lik
                        // bir zincir halkası da okunmalı. Faz 25'ten beri zincirler daha yavaş.)
                        if (_repeatsLeft > 0 || _chained != null) { EnterWindup(); break; }

                        _attackTimer = _t.enemyAttackCooldown;
                        EnterState(State.Chase, 0f);
                    }
                    break;
            }
        }

        int _dağarcıkFaz = -1;
        BossAttack[] _dağarcık;

        // ── SALDIRI SEÇİMİ: ÜÇ SIRA (Faz 26, Enes'in kuralı) ──────────────────
        // *"Yakın menzil, uzak menzil ve tüm harita diye 3 çeşit olsun. Mesafeye göre
        // yakın ve uzak arasında değişsin, belirli sayıda saldırıdan sonra tüm harita
        // saldırılarını yapsın."*
        //
        // Faz 25: iki sıra, saldırının sırası MENZİLDEN türüyordu (`RangeMul > 1.7`).
        // Boss'un etrafında patlayan büyük halkalar "uzak" sayılıyor, dibinde dövüşen
        // oyuncu fazda 2-4 saldırı görüyordu (Enes: "son boss 2 saldırı atıyor").
        // Şimdi saldırının türü VERİDE (`BossAttack.Tür`). Mesafe yakın/uzak sırasını
        // seçiyor; her `bossMapAttackEvery`. saldırı HARİTA sırasından, mesafeden bağımsız.
        // Her sıra kendi içinde sırayla dönüyor; sayaç yalnızca saldırı yapılınca ilerliyor.
        //
        // Faz 25'ten kalan: boss sırası gelen saldırının menziline girene kadar
        // yürüyor; oyuncu kaçıp girdirmezse SABIR dolunca aynı sıradan yetişeni yapıyor.
        readonly BossAttack[][] _sıralar = new BossAttack[3][];
        readonly int[] _sayaç = new int[3];

        /// <summary>Son harita saldırısından beri yapılan yakın/uzak saldırı sayısı.</summary>
        int _haritasız;

        /// <summary>Sıradaki saldırının menziline girilemeden geçen süre — kaçışı kapatır.</summary>
        float _sabır;
        float _öncekiMesafe;

        // `SaldırıyaHazır`'ın bulduğu, `PickAttack`'in tükettiği saldırı.
        BossAttack _seçilen;
        SaldırıTürü _seçilenTür;
        int _seçilenİndis;

        // ── ISRAR (Faz 26, Enes'in fikri) ────────────────────────────────────
        // *"Sürekli yakından veya sürekli uzaktan saldıranlar için belli bir eşikten
        // sonra peşpeşe yapılan yakın ve uzak menzil saldırılarının hasarı artsın."*
        // Aynı sıradan art arda saldırı = oyuncu hep aynı mesafede. Eşikten sonra her
        // saldırı daha pahalı; mesafe değişince sıfır. Harita saldırıları ne sayar ne
        // sıfırlar. ⚠️ GÖRÜNÜR: uyarı yayı koyulaşıyor, HUD'da "ISRAR ×" yazıyor —
        // görünmeseydi "boss rastgele sert vurdu" olurdu (ders 10).
        SaldırıTürü _sonBant = SaldırıTürü.Harita;
        int _bantSerisi;
        float _ısrar = 1f;

        /// <summary>Şu anki saldırının ısrar çarpanı (1 = yok). HUD ve öz-denetim okuyor.</summary>
        public float IsrarÇarpanı => _ısrar;

        /// <summary>
        /// Aynı sıradan art arda `seri`. saldırının çarpanı: eşiğe kadar 1, sonra her adımda
        /// BİNEREK (1 + adım)^n, tavanlı. Enes: *"x2 değil, 1.3 1.5 1.8 gibi artarak gitsin,
        /// x5'e kadar"* (2026-10-10) → ×1.25, 1.56, 1.95, 2.44, 3.05, 3.81, 4.77, 5.
        /// </summary>
        public static float IsrarEğrisi(Tuning t, int seri)
            => Mathf.Min(t.bossStreakCap, Mathf.Pow(1f + t.bossStreakStep, Mathf.Max(0, seri - t.bossStreakThreshold)));

        /// <summary>
        /// Bu fazın saldırı dağarcığı — faz başına BİR KEZ alınıyor, üç sıraya bölünüyor.
        /// ⚠️ `Boss.Attacks()` her çağrıda YENİ dizi üretiyor; kareye bir
        /// çağırmak çöp toplayıcıyı beslerdi.
        /// ⚠️ Yalnızca SALDIRILAR önbellekte, menzil DEĞİL: `AttackRange` boss'ta
        /// `Radius`'u içeriyor ve `Radius` isabet parlamasında %28 şişiyor. Menzili
        /// faz başına bir kez hesaplamak geçici titreşimi bütün faza yayardı
        /// (Faz 23'ün bayat önbellek hatası).
        /// </summary>
        BossAttack[] Dağarcık
        {
            get
            {
                if (_dağarcıkFaz != Phase)
                {
                    _dağarcıkFaz = Phase;
                    _dağarcık = Boss.Attacks(Phase);
                    for (int t = 0; t < 3; t++)
                    {
                        var tür = (SaldırıTürü)t;
                        _sıralar[t] = System.Array.FindAll(_dağarcık, a => a.Tür == tür);
                        _sayaç[t] = 0;                   // yeni faz, yeni dağarcık: baştan
                    }
                    // Yakın ya da uzak sıra boşsa öbürü — boş sıra boss'u çaresiz bırakırdı.
                    if (_sıralar[0].Length == 0) _sıralar[0] = _sıralar[1];
                    if (_sıralar[1].Length == 0) _sıralar[1] = _sıralar[0];
                    if (_sıralar[0].Length == 0) _sıralar[0] = _sıralar[1] = _dağarcık;
                    _haritasız = 0;
                    _seçilen = null;
                    _sabır = 0f;
                    // ⚠️ Israr da sıfır: yoksa MURABAHACI rakip dövüşlerinden sonra uyanınca
                    // faz 2'nin ×2'siyle başlıyordu (inceleme 2026-10-10).
                    _sonBant = SaldırıTürü.Harita; _bantSerisi = 0; _ısrar = 1f;
                }
                return _dağarcık;
            }
        }

        BossAttack[] Sıra(SaldırıTürü tür)
        {
            _ = Dağarcık;
            return _sıralar[(int)tür];
        }

        /// <summary>Harita saldırısının sırası geldi mi — mesafeden bağımsız.</summary>
        bool HaritaZamanı => _t.bossMapAttackEvery > 0 && Sıra(SaldırıTürü.Harita).Length > 0
                             && _haritasız >= _t.bossMapAttackEvery - 1;

        /// <summary>
        /// Sırası gelmiş VE cevabına yetişilebilen harita saldırısı (yoksa null).
        /// ⚠️ Simitte cevap boss'a koşmak: 17 birimden yetişilmiyordu
        /// (inceleme 2026-10-10). Erişim şartı `MaxPlayerRange`'te, burada sorgulanıyor.
        /// </summary>
        BossAttack SıradakiHarita(float mesafe)
        {
            if (!HaritaZamanı) return null;
            var h = Sıra(SaldırıTürü.Harita);
            var a = h[_sayaç[(int)SaldırıTürü.Harita] % h.Length];
            return Erişir(a, mesafe) ? a : null;
        }

        /// <summary>Oyuncu bu mesafedeyken hangi sıra: yakın mı, menzilli mi.</summary>
        bool Uzakta(float mesafe) => mesafe > AttackRange * BossAttack.UzakEşiği;

        /// <summary>Saldırı bu mesafeden oyuncuya GERÇEKTEN ulaşıyor ve şartları tutuyor mu.</summary>
        bool Erişir(BossAttack a, float mesafe)
            => mesafe <= AttackRange * a.RangeMul + a.Lunge
            && (a.MinPlayerRange <= 0f || mesafe >= a.MinPlayerRange)
            && (a.MaxPlayerRange <= 0f || mesafe <= a.MaxPlayerRange);

        /// <summary>
        /// Bekleme bitti — saldırıya girilebilir mi. Sıradan düşmanda kendi menzili;
        /// boss'ta mesafenin sırasındaki SIRADAKİ saldırının menzili.
        ///
        /// ⚠️ SABIR: oyuncu kaçıp boss'u menzile hiç sokmazsa, `bossAttackPatience`
        /// sonra aynı sıradan o an yetişen ilk saldırı yapılıyor. Yoksa kaçarak
        /// saldırıları tamamen iptal edebilirdin.
        /// ⚠️ Sabır yalnızca boss YAKLAŞAMIYORKEN işliyor. Her karede işleseydi uzaktan
        /// yürüyerek gelen boss (14 birimden 7'ye ~1.6 sn) yolda sabrını tüketip sırayı
        /// atlardı — "sırayla" kuralı duran oyuncuya karşı bile bozulurdu.
        /// </summary>
        bool SaldırıyaHazır(float dt)
        {
            float mesafe = FlatDistanceToPlayer();
            if (Boss == null) return mesafe <= AttackRange;

            // HARİTA: sırası geldiyse önce o — cevabına yetişilebiliyorsa (`SıradakiHarita`).
            // Yetişilemiyorsa sıra TUTULUYOR, boss yaklaşıyor; sabır dolarsa (oyuncu kaçıyor)
            // mesafenin sırasından yetişen saldırı, harita sırası beklemeye devam ediyor.
            bool sabırsız = _sabır >= _t.bossAttackPatience;
            var harita = SıradakiHarita(mesafe);
            if (harita != null)
            {
                _seçilen = harita; _seçilenTür = SaldırıTürü.Harita;
                _seçilenİndis = _sayaç[(int)SaldırıTürü.Harita] % Sıra(SaldırıTürü.Harita).Length;
                return true;
            }

            if (!HaritaZamanı || sabırsız)
            {
                var tür = Uzakta(mesafe) ? SaldırıTürü.Uzak : SaldırıTürü.Yakın;
                var liste = Sıra(tür);
                int sıra = _sayaç[(int)tür];

                int bakılacak = sabırsız ? liste.Length : 1;
                for (int i = 0; i < bakılacak; i++)
                {
                    int j = (sıra + i) % liste.Length;
                    if (!Erişir(liste[j], mesafe)) continue;
                    _seçilen = liste[j]; _seçilenTür = tür; _seçilenİndis = j;
                    return true;
                }
            }
            if (_öncekiMesafe - mesafe < MoveSpeed * dt * 0.5f) _sabır += dt;   // yaklaşamıyor
            _öncekiMesafe = mesafe;
            return false;
        }

        /// <summary>
        /// Şu anki saldırı 360° mü — halka, yani yön kontrolü yok, UZAKLAŞMAK gerekiyor.
        /// Ses ve öz-denetim buna bakıyor; değeri `_attack`'ten türüyor.
        /// </summary>
        bool _slamming => _attack != null && _attack.Angle >= 360f;

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
        float StrikeRadius => _attack == null ? AttackRange
                            : _attack.ArenaOranı > 0f ? _t.arenaRadius * _attack.ArenaOranı   // gövdeden bağımsız
                            : AttackRange * _attack.RangeMul;
        float StrikeAngle => _attack != null && _attack.Angle > 0f ? _attack.Angle : AttackAngle;
        float StrikeInner => _attack?.InnerRadius ?? 0f;
        Color StrikeColor => IsrarRengi(_attack?.Tint ?? StrikeTint);
        Color TelegraphColor => IsrarRengi(_attack?.Tint ?? TelegraphTint);

        /// <summary>Israr arttıkça uyarı koyulaşıyor — "bu vuruş daha pahalı" GÖRÜNSÜN.</summary>
        Color IsrarRengi(Color c)
        {
            if (_ısrar <= 1f || _t == null) return c;
            // Logaritmik: çarpan binerek büyüyor, her adım AYNI miktar koyulaşsın. İlk adım da
            // görünsün diye %25'ten başlıyor (×5 tavanında doğrusal ölçek ×1.25'i %6 boyuyordu).
            float k = Mathf.Lerp(0.25f, 1f, Mathf.Log(_ısrar) / Mathf.Log(Mathf.Max(1.01f, _t.bossStreakCap)));
            var r = Color.Lerp(c, new Color(0.75f, 0.02f, 0.05f, c.a), k);
            r.a = Mathf.Min(0.85f, c.a * (1f + k));
            return r;
        }

        /// <summary>
        /// Şu anki gerçek vuruş yarıçapı ve güvenli göbeği. Öz-denetim BUNLARI okuyor —
        /// `Tuning`'den yeniden hesaplamıyor. (`CurrentSpeed` ile aynı gerekçe: formül
        /// değişince test yeşil kalıp oyun bozulmasın.)
        /// </summary>
        public float CurrentStrikeRadius => StrikeRadius;

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
                // ⚠️ `_chained`'i burada SIFIRLAMA: SetAttack onu zaten bir sonraki
                // halkaya (`a.Then`) çeviriyor. Sıfırlamak her zinciri 2. halkada
                // kesiyordu — faz 3'ün 4 halkalı zinciri hiç tam oynamadı (denetim 2026-09-23).
                SetAttack(_chained);
            }
            else
            {
                PickAttack();
            }
            _seçilen = null;   // tekrar/zincir dalı seçileni kullanmadı — bayatlamasın

            // ⚠️ MUTLAK süre çarpanı EZER: konum değiştirten saldırılara (güvenli açıklık,
            // simit) çarpan vermek yetmiyordu — boss hazırlıkları ~0.2 sn ve
            // "yeşil alana geçecek kadar süre kalmıyordu" (Enes).
            // ⚠️ TEPKİ PAYI çarpanlı hazırlığa EKLENİYOR: eski süre boyunca nişan alıyor,
            // sonra pay kadar yön kilitli (bkz. `HazırlıktaTakip`). Mutlak süreliler
            // zaten uzun; payları sürelerinin içinden (son kısmı kilitli).
            float dur = _attack != null && _attack.WindupSeconds > 0f
                      ? _attack.WindupSeconds
                      : WindupDuration * (_attack?.WindupMul ?? 1f) + TepkiPayı;
            EnterState(State.Windup, dur);
            // ORTAYA SIÇRAMA (TAM TAHSİL): uyarı çizilmeden ÖNCE — halka ortadan çiziliyor.
            if (_attack != null && _attack.ArenaOranı > 0f)
                transform.position = ArenayaSığdır(Bootstrap.ArenaCenter);
            FacePlayer();

            // ⚠️ Hazırlık işareti artık DOLUYOR (0.25 → 1.0). Sabit yay "vuracağım" diyordu
            // ama "ne zaman" demiyordu — kaçma anını gözle kestiremiyordun. Dolan yay
            // kalan süreyi doğrudan gösteriyor: yay tamamlanınca darbe iniyor.
            // Boss'un yere vuruşunda bu daha da kritik, çünkü halka 360° ve kaçacak yer yok.
            _telegraph = Arc.Spawn(transform.position, transform.rotation,
                                   StrikeRadius, StrikeAngle, TelegraphColor, dur, transform,
                                   scaleFrom: 0.25f, scaleTo: 1f, scaled: true);

            // ⚠️ HARİTADAKİ RASTGELE GÜVENLİ NOKTA — hazırlık başında seçiliyor ve
            // yeşil halkayla APAÇIK gösteriliyor. Saldırıyı adil tutan şey
            // rastgelelik değil TELGRAF: nokta görünmeseydi bu saldırı kaçınılamaz
            // bir ceza olurdu, mekanik doğru ama sunum öldürücü (ders 10).
            // ⚠️ Halka boss'u TAKİP ETMİYOR (`follow` verilmiyor): nokta sabit,
            // boss hareket etse bile yeri değişmemeli.
            if (_attack != null && _attack.SafeSpotRadius > 0f)
            {
                _safeSpot = RastgeleGüvenliNokta(dur);
                Arc.Spawn(_safeSpot, Quaternion.identity, _attack.SafeSpotRadius, 360f,
                          new Color(0.45f, 1f, 0.55f, 0.40f), dur,
                          scaleFrom: 1f, scaleTo: 1f, scaled: true);
            }

            // ⚠️ SİMİT'in güvenli göbeği GÖRÜNMELİ. Görünmezse oyuncu "halka geliyor,
            // kaç" der ve tam ters yönde ölür — mekanik doğru, sunum öldürücü olur.
            if (StrikeInner > 0f)
                Arc.Spawn(transform.position, transform.rotation, StrikeInner, 360f,
                          new Color(0.45f, 1f, 0.55f, 0.35f), dur, transform, scaled: true);

            // ⚠️ AYNA'nın ARKA dilimi de çizilmeli. Çizilmezse oyuncu "koni
            // geliyor, geri çekil" der ve tam arkada ölür — mekanik doğru,
            // sunum öldürücü (bu projenin en pahalı hata türü).
            if (_attack != null && _attack.Mirrored && StrikeAngle < 360f)
                Arc.Spawn(transform.position, transform.rotation * Quaternion.Euler(0f, 180f, 0f),
                          StrikeRadius, StrikeAngle, TelegraphColor, dur, transform,
                          scaleFrom: 0.25f, scaleTo: 1f, followYaw: 180f, scaled: true);

            // ⚠️ GÜVENLİ AÇIKLIK da görünmeli — arkada, yeşil. Görünmezse oyuncu
            // "halka geliyor, uzaklaş" der ve tam kaçamayacağı yerde ölür.
            // Boss'un ARKASINA bakan yönde çiziliyor.
            if (_attack != null && _attack.SafeAngle > 0f)
                Arc.Spawn(transform.position, transform.rotation * Quaternion.Euler(0f, 180f, 0f),
                          StrikeRadius, _attack.SafeAngle,
                          new Color(0.45f, 1f, 0.55f, 0.30f), dur, transform,
                          followYaw: 180f, scaled: true);   // ⚠️ olmazsa takip dönüşü sapmayı siler

            Paint();
        }

        /// <summary>
        /// Sıradaki saldırıyı KUR ve o sıranın sayacını ilerlet.
        ///
        /// Normalde `SaldırıyaHazır` saldırıyı çoktan buldu (menzil + sabır). Bulmadıysa
        /// (hazırlık doğrudan çağrıldı) mesafenin sırasındaki sıradaki saldırı alınıyor.
        ///
        /// ⚠️ Sıra içinde DETERMİNİSTİK ("bu saldırı mı zor, ben mi kötü oynadım"
        /// cevaplanabilir kalsın) — çeşidi mesafe veriyor, zar değil.
        /// </summary>
        void PickAttack()
        {
            if (Boss == null)
            {
                // Boss olmayan düşman: sıradan koni vuruşu.
                _attack = null;
                _repeatsLeft = 0;
                _chained = null;
                return;
            }

            if (_seçilen == null)
            {
                float mesafe = FlatDistanceToPlayer();
                _seçilenTür = SıradakiHarita(mesafe) != null ? SaldırıTürü.Harita
                            : Uzakta(mesafe) ? SaldırıTürü.Uzak : SaldırıTürü.Yakın;
                var liste = Sıra(_seçilenTür);
                _seçilenİndis = _sayaç[(int)_seçilenTür] % liste.Length;
                _seçilen = liste[_seçilenİndis];
            }

            _sayaç[(int)_seçilenTür] = _seçilenİndis + 1;
            if (_seçilenTür == SaldırıTürü.Harita)
            {
                _haritasız = 0;
                _ısrar = 1f;                              // harita ısrarı ne sayar ne sıfırlar
            }
            else
            {
                _haritasız++;
                if (_seçilenTür == _sonBant) _bantSerisi++;
                else { _sonBant = _seçilenTür; _bantSerisi = 1; }
                _ısrar = IsrarEğrisi(_t, _bantSerisi);
            }

            SetAttack(_seçilen);
            _seçilen = null;
            _sabır = 0f;
        }

        /// <summary>Saldırıyı kur — tekrar ve zincir durumu tek yerden ayarlanıyor.</summary>
        /// <summary>Boss'un doğurdukları için kamera — yoksa süre etiketleri kameraya dönmüyordu.</summary>
        static Transform Kamera => IsoCamera.Instance != null ? IsoCamera.Instance.transform : null;

        void SetAttack(BossAttack a)
        {
            _attack = a;
            _repeatsLeft = Mathf.Max(0, a.Repeat - 1);
            _chained = a.Then;                    // tekrarlar bitince zincire geçilir
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
                transform.position = ArenayaSığdır(transform.position + _strikeDir * _attack.Lunge);

            Arc.Spawn(transform.position, transform.rotation,
                StrikeRadius, StrikeAngle, StrikeColor, _t.enemyStrikeDuration * 1.6f);

            // AYNA'nın arka yayı — vuruş anında da görünmeli, yoksa "beni ne vurdu"
            // sorusu cevapsız kalır.
            if (_attack != null && _attack.Mirrored && StrikeAngle < 360f)
                Arc.Spawn(transform.position, transform.rotation * Quaternion.Euler(0f, 180f, 0f),
                    StrikeRadius, StrikeAngle, StrikeColor, _t.enemyStrikeDuration * 1.6f);
            Paint();

            // Hasar yalnızca bu anda, kilitli yöne göre iner. Kaçtıysan ıskalar.
            if (PlayerInStrikeCone())
            {
                float cost = HitCost * (_attack?.CostMul ?? 1f) * _ısrar;    // ISRAR
                // ⚠️ NEMESİS: boss'un senden aldığı saniye SAYILIYOR — ve gerçekten
                // İNEN bedel sayılıyor. Dash i-frame'i vuruşu yutabiliyor; "vurdum,
                // demek ki aldı" varsayımı boss'a hak etmediği alacak yazardı.
                // ⚠️ Hesaba `TakeHit` yazıyor, saat düşmeden ÖNCE: öldüren vuruş ölüm
                // sırasının İÇİNDE diske yazılıyor, sonradan eklenen hiç sayılmıyordu.
                _player.GetComponent<PlayerController>()?.TakeHit(cost, transform.position, this);
            }

            // ÇEKİM: hasardan SONRA. Önce çekseydik oyuncuyu tehlikeye sürükleyip
            // aynı karede cezalandırırdık — kaçınılamaz bir darbe olurdu. Sonra
            // çekmek konumu bozuyor, darbeyi iki katlamıyor.
            // ⚠️ Menzil şartı yok: çekim ISKALADIĞINDA da işliyor, asıl tehdidi
            // bu zaten — vurmuyor, seni yerinden ediyor.
            if (_attack != null && _attack.Pull > 0f && FlatDistanceToPlayer() <= StrikeRadius)
                _player.GetComponent<PlayerController>()?.Yank(transform.position, _attack.Pull);

            // GERİ SIÇRAMA: hamlenin tersi. Vurur ve ÇEKİLİR — "kaç" değil
            // "KOVALA" diye sorar. ⚠️ Arenaya sığdırılıyor: sınıra sıçrayan
            // boss dışarı çıkıp erişilmez olurdu.
            // ⚠️ Hasardan SONRA. Önceden hasardan önce sıçrıyordu: 5 birim geri gidip
            // isabeti YENİ yerden ölçüyordu, 4.25 menzilli DEVİR hiç tutmuyordu
            // (denetim 2026-09-23). Hamle öne kalır: o, mesafeyi VURMADAN önce kapatır.
            if (_attack != null && _attack.Recoil > 0f)
                transform.position = ArenayaSığdır(transform.position - _strikeDir * _attack.Recoil);
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
        public bool PlayerInStrikeCone() => _player != null && İsabet(_player.position, _strikeDir);

        /// <summary>
        /// Şu an VURSA bu noktadaki oyuncuya isabet eder mi — yön şu anki bakış.
        /// Otopilot ("uyarıyı gören insan") bunu soruyor. ⚠️ Aynı hesap, kopya değil:
        /// bot kendi formülünü yazsaydı oyundan ayrışır ve yalan söylerdi.
        /// </summary>
        public bool ŞimdiVursaİsabet(Vector3 oyuncu) => İsabet(oyuncu, transform.forward);

        /// <summary>Hazırlığın kalan süresi — hazırlıkta değilse 0.</summary>
        public float KalanHazırlık => _state == State.Windup ? Mathf.Max(0f, _stateTimer) : 0f;

        bool İsabet(Vector3 oyuncu, Vector3 yön)
        {
            Vector3 to = oyuncu - transform.position;
            to.y = 0f;
            float d = to.magnitude;
            if (d > StrikeRadius) return false;

            // ⚠️ HARİTADAKİ GÜVENLİ NOKTA: dördüncü kaçış sorusu. "Uzaklaş" değil,
            // "yaklaş" değil, "arkasına geç" de değil — ORAYA YETİŞ.
            if (_attack != null && _attack.SafeSpotRadius > 0f)
            {
                var s = oyuncu - _safeSpot;
                s.y = 0f;                                  // tuzak 2
                if (s.magnitude <= _attack.SafeSpotRadius) return false;
            }

            // ⚠️ SİMİT: göbeği GÜVENLİ. Halkanın tam tersi soru — "uzaklaş" değil "YAKLAŞ".
            if (StrikeInner > 0f && d <= StrikeInner) return false;

            // ⚠️ GÜVENLİ AÇIKLIK: halkanın ARKASINDA. Üçüncü soru — "uzaklaş" ya da
            // "yaklaş" değil, "ARKASINA GEÇ". Açıklık öne konsaydı boss hazırlık
            // boyunca oyuncuya döndüğü için oyuncu daima içinde kalır ve saldırı
            // hiç isabet etmezdi.
            float safe = _attack?.SafeAngle ?? 0f;
            if (safe > 0f && Vector3.Angle(-yön, to) <= safe * 0.5f) return false;

            // AYNA: ön VE arka dilim. Hangisi daha yakınsa o sayılıyor — yanlar
            // güvenli kalıyor, geri çekilmek kalmıyor.
            float sapma = Vector3.Angle(yön, to);
            if (_attack != null && _attack.Mirrored)
                sapma = Mathf.Min(sapma, Vector3.Angle(-yön, to));

            // 360°'de yön kontrolü yok: her açı zaten içeride.
            return StrikeAngle >= 360f || sapma <= StrikeAngle * 0.5f;
        }

        /// <summary>Öz-denetim ve HUD için: şu an hangi saldırıyı yapıyor.</summary>
        public BossAttack CurrentAttack => _attack;

        /// <summary>Öz-denetim için: şu an yere vuruş yapıyor mu.</summary>
        public bool Slamming => _slamming;

        /// <summary>
        /// Hamle/geri sıçrama sonrası konumu arenanın İÇİNDE tutuyor.
        /// ⚠️ Tek yerde: iki ayrı sıçrama iki ayrı sınır yorumu yapsaydı
        /// biri er ya da geç dışarıda kalırdı (oyuncu tarafında bu hata
        /// `LateUpdate` kelepçesiyle aynı sebeple tek noktada duruyor).
        /// </summary>
        Vector3 ArenayaSığdır(Vector3 p)
        {
            Vector3 d = p - Bootstrap.ArenaCenter;
            d.y = 0f;
            float sınır = _t.arenaRadius - Radius;
            if (d.magnitude > sınır) p = Bootstrap.ArenaCenter + d.normalized * sınır;
            p.y = transform.position.y;
            return p;
        }

        float FlatDistanceToPlayer() => FlatToPlayer().magnitude;

        void FacePlayer()
        {
            Vector3 to = FlatToPlayer();
            if (to.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(to.normalized);
        }

        void ChaseMove(float dt)
        {
            Vector3 to = FlatToPlayer();
            float dist = to.magnitude;
            if (dist < 0.01f) return;

            // KAÇAK kaçarken oyuncudan UZAKLAŞIYOR — kovalamak senin kararın.
            if (Fleeing)
            {
                transform.position -= (to / dist) * MoveSpeed * dt;
                transform.rotation = Quaternion.LookRotation(-to / dist);
                return;
            }

            // ⚠️ MÜBAŞİR mesafesini KORUYOR — dibine gelirse "menzilli" olmaktan çıkar
            // ve sıradan bir düşmana dönüşür. Çok yaklaşırsa geri çekiliyor.
            float tercih = Kind.PreferredRange(_t);
            if (tercih > 0f)
            {
                // ⚠️ Geri çekilme ARENAYA SIĞDIRILIYOR. Sığdırılmazsa kenara sıkıştırılan
                // MÜBAŞİR sınırın 6-8 birim dışına yerleşiyordu: oyuncu sınırda duruyor,
                // yakın silahlar erişemiyor, oda hiç temizlenmiyordu (denetim 2026-09-23).
                // Köşeye sıkışınca yeri yok — "uzak dur" cevabı orada biter, olması gereken bu.
                if (dist < tercih * 0.85f)
                    transform.position = ArenayaSığdır(transform.position - (to / dist) * MoveSpeed * dt);
                else if (dist > tercih * 1.15f)
                    transform.position += (to / dist) * MoveSpeed * dt;
                FacePlayer();
                return;
            }

            if (dist > AttackRange * 0.9f)
                transform.position += (to / dist) * MoveSpeed * dt;
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
            // ⚠️ UYKUDAKİ boss VURULMUYOR. Vurulabilseydi oyuncu rakipleri es
            // geçip doğrudan onu döverdi — "hepsi ölünce kendisi gelsin" kuralı
            // sessizce anlamsızlaşırdı.
            if (Dormant) return;

            if (Hollow)
            {
                Kill();                        // tek atımlık
                return;
            }

            // ⚠️ ZIRH BURADA — gerçekten (Faz 25). Bu yorum önceden de "zırh burada"
            // diyordu ama altında zırh YOKTU; ZIRHLI yalnızca havuzu ×2 bir düşmandı ve
            // Enes onu İRİ'den ayıramadı (2026-09-30). Hasar kısılıyor, hasat inen
            // hasarla orantılı olduğu için vuruş başına kazanç da aynı oranda düşüyor.
            // TOPLAM ödeme havuz kadar kalıyor, sadece daha çok vuruş istiyor.
            damage *= Rank.DamageTaken(_t);
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
                             * InsuredHarvest()                    // SİGORTA koruması
                             * (IsBoss ? _t.bossHarvestMultiplier : 1f)
                             * clock.HasatDoyması;                 // DOYMA: saat büyüdükçe hasat azalır
                clock.Add(gain);

                // Zaman ondan sana AKIYOR — kazanç sıfırsa (KASA) hiçbir şey akmaz,
                // ve bu doğru: KASA'ya vurmak gerçekten hiçbir şey vermiyor.
                if (gain > 0f)
                    Mote.Spawn(MoteOrigin(), _player, Mote.HarvestColor,
                               size: 0.16f + Mathf.Min(gain, 3f) * 0.05f);   // çok çaldıysan büyük görün
            }

            CheckPhase();

            // Buraya boş gelinmiyor (boşsa yukarıda öldü) — şimdi boşsa BU vuruş boşalttı.
            // ⚠️ Boşalma halkası yalnızca kendi kendine akışta çıkıyordu; asıl yaygın
            // yol olan vuruşla boşalmada hiç çıkmıyordu. CheckPhase'ten SONRA: fullenen
            // boss (③④) boşalmış sayılmasın.
            if (Hollow) OnWentHollow();

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
        /// <summary>
        /// Saniyede emdiği — TEK kaynak: oyun (`ApplyAura`) ve zorluk raporu buna soruyor. Rapor kendi
        /// formülünü yazıyordu ve mini boss'un yarım aurası yerine genel boss aurasını sayıyordu (10 kat).
        /// </summary>
        public float AuraOranı => (Boss != null ? Boss.AuraDrain(_t, Phase)
                                                : (IsBoss ? _t.bossAuraDrain : 0f))
                                + Kind.AuraDrain(_t) * Rank.TraitMultiplier(_t);   // İRİ SIZINTI

        public void ApplyAura(float dt)
        {
            // ⚠️ UYKU KURALI BURADA DA. `Update` uykudayken bu metodu zaten
            // çağırmıyor — ama koruma yalnızca ORADA olsaydı kural TEK BİR
            // ÇAĞIRANDA yaşardı; başka bir yol açılınca sessizce delinirdi.
            // Bu projede aynı ders `SaveFile.Flush`'ta alınmıştı: kural, bütün
            // çağıranların geçtiği YERE konur, çağıranlardan birine değil.
            if (Dormant || Hollow || _t == null) return;

            float aura = AuraOranı;
            if (aura <= 0f) return;

            Clock.Instance?.Spend(aura * dt);
            HesabınaYaz(aura * dt);   // NEMESİS: aura da onun hanesine (tek yazma yolu)

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
            if (Boss == null || _t == null) return;

            // ⚠️ UYKU, PASİFLERDEN ÖNCE. Uykudayken rejenerasyon, tebligat ve
            // çağırma İŞLEMEZ — yoksa oyuncu rakiplerle dövüşürken arkada sessizce
            // güçlenen bir boss olurdu ve dövüş kazanılamaz hale gelirdi.
            if (Dormant)
            {
                // Sahnedeki rakip ölmediyse bekle.
                foreach (var e in All)
                    if (e != null && e != this && e.SummonedRival) return;

                // ⚠️ SIRAYLA çağırılıyor, hepsi birden değil (Enes: "her biri
                // rastgele şekilde çıkmalı"). Dördü aynı anda gelseydi final bir
                // boss savaşı değil bir kalabalık olurdu; sırayla gelince gerçek
                // bir boss geçidi oluyor ve her birinin dağarcığı ayrı ayrı
                // okunuyor.
                if (_rivalQueue != null && _rivalQueue.Count > 0)
                {
                    ÇağırSıradakiRakibi();
                    return;
                }

                Dormant = false;
                Arc.Spawn(transform.position, transform.rotation, Radius * 7f, 360f,
                          new Color(Boss.Tint.r, Boss.Tint.g, Boss.Tint.b, 0.8f), 0.9f,
                          scaleFrom: 0.2f, scaleTo: 2.6f);
                Sfx.Slam();
                Debug.Log($"[Tek Can] {Boss.Name} UYANDI — adamları bitti, kendisi geliyor.");
                Paint();
                return;
            }

            if (Hollow) return;

            // ── ÇAĞIRMA (faz 2+) ──────────────────────────────────────────────
            // Enes: "bossların faz 2'sine düşman çağırma özelliğini de ekle."
            float çağrıAralık = Boss.SummonInterval(Phase);
            if (çağrıAralık > 0f)
            {
                _summonTimer -= dt;
                if (_summonTimer <= 0f)
                {
                    _summonTimer = çağrıAralık;
                    Çağır();
                }
            }

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
                             parent: transform.parent, owner: this);
        }

        /// <summary>
        /// TÜRE ait pasifler — `TickBossPassives`'in eşi. Ayrı duruyorlar çünkü
        /// karıştırmak "boss pasifi" ile "tür pasifi"ni tek yere yığar ve bir sonraki
        /// okuyan hangisinin nerede olduğunu bilemez. `dt` yine dışarıdan.
        /// </summary>
        public void TickKindPassives(float dt)
        {
            if (Hollow || _t == null) return;

            // ZAMANAŞIMI: yaşadığı süre onu besliyor (bkz. `Growth`).
            _aliveTime += dt;

            // MÜBAŞİR: uzaktan tebligat — durduğun YERE bırakıyor, yaklaşmıyor.
            // Bu güne kadar bütün düşmanlar yakın dövüştü, yani "uzak dur" bedava
            // bir cevaptı; bu onu bozuyor.
            float tebligat = Kind.RangedMarkInterval(_t) / Rank.TraitMultiplier(_t);   // İRİ: sık
            if (tebligat <= 0f || _player == null) return;

            _kindMarkTimer -= dt;
            if (_kindMarkTimer > 0f) return;

            _kindMarkTimer = tebligat;
            GroundMark.Spawn(_t, _player.position, _player,
                             radius: _t.bailiffMarkRadius,
                             delay: _t.groundMarkDelay,
                             cost: HitCost * _t.groundMarkCostMultiplier,
                             parent: transform.parent, owner: this);
        }

        // ⚠️ Boss'unkinden AYRI sayaç: aynı düşman hem boss hem MÜBAŞİR olamaz ama
        // tek sayacı paylaştırmak ileride sessiz bir çakışma davetiyesi olurdu.
        float _kindMarkTimer;

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
        /// SİGORTA koruması: yakınında yaşayan bir SİGORTA varsa bu düşmandan alınan
        /// hasat kısılıyor. Kendini korumuyor — yoksa öldürülemez olurdu.
        ///
        /// ⚠️ Statik bayrak tutulmuyor, her seferinde SAHNEYE soruluyor. Bayrak
        /// tutsaydık sigorta ölünce temizlemeyi unutmak "hasadım niye hâlâ düşük"
        /// hatası doğururdu — `BossKind.ActiveHarvestMultiplier` ile aynı gerekçe.
        /// </summary>
        float InsuredHarvest()
        {
            float m = 1f;
            foreach (var e in All)
            {
                if (e == null || e == this || e.Hollow) continue;
                float r = e.Kind.ProtectRadius(_t) * e.Rank.TraitMultiplier(_t);   // İRİ SİGORTA
                if (r <= 0f) continue;

                // ⚠️ YATAY mesafe — y'nin yatay hesaba karışması bu projede dört kez yandı.
                var d = e.transform.position - transform.position;
                d.y = 0f;
                if (d.magnitude <= r) m *= e.Kind.ProtectedHarvest(_t);
            }
            return m;
        }

        /// <summary>
        /// Boss faz geçişi. İki farklı kural (Enes kararı, `BossKind.RefillsOnEmpty`):
        ///   TAHSİLDAR, EKSPER havuz YARIYA düşünce döner
        ///   KEFİL, SİMSAR, MURABAHACI havuz BİTİNCE fullenip döner — "kazandım" sandığın an
        ///
        /// ⚠️ Fullenen havuz KREDİ olarak işaretleniyor: erir ama hasat vermez.
        /// </summary>
        void CheckPhase()
        {
            // ⚠️ Çağrılan rakipler faz değiştirmiyor — zincirleme doğum olmasın.
            if (Boss == null || SummonedRival || Phase >= Boss.PhaseCount) return;

            if (Boss.RefillsOnEmpty)
            {
                if (TimeLeft > 0f) return;
                TimeLeft = _startingPool;      // FULLENİR
                OnCredit = true;               // ...ama artık sahte para
            }
            else if (Phase == 1 && TimeLeft > _startingPool * Boss.PhaseTwoAt) return;
            else if (Phase >= 2 && TimeLeft > 0f) return;   // faz 3 havuz bitince

            AdvancePhase();
        }

        float _summonTimer;

        /// <summary>Bu saldırının güvenli noktası — hazırlıkta seçiliyor.</summary>
        Vector3 _safeSpot;

        /// <summary>Öz-denetim için: şu anki güvenli nokta.</summary>
        public Vector3 CurrentSafeSpot => _safeSpot;

        /// <summary>
        /// Şu anki saldırının güvenli nokta yarıçapı (0 = bu saldırıda yok).
        /// ⚠️ `CurrentStrikeRadius` ile aynı gerekçe: öz-denetim hangi saldırının
        /// seçildiğini ÜRETİMDEN öğreniyor, kendi seçimini dayatmıyor.
        /// </summary>
        public float CurrentSafeSpotRadius => _attack?.SafeSpotRadius ?? 0f;

        /// <summary>
        /// Arenada rastgele bir nokta — kenardan biraz içeride.
        /// ⚠️ Kenara yapışık seçilseydi oyuncu arena sınırına sıkışır ve "yetiştim
        /// ama yine öldüm" derdi; sınır bu oyunda yeni (Enes bildirdi).
        ///
        /// ⚠️ Ve oyuncunun YETİŞEBİLECEĞİ yerden: koşarak, boss'un tepki payı düşülmüş
        /// sürede. Önceden oyuncuya bakılmıyordu, noktaların ~üçte biri yetişilemezdi
        /// (inceleme 2026-10-10). Rastgelelik kalıyor — yalnızca yetişilemeyenler eleniyor.
        /// </summary>
        Vector3 RastgeleGüvenliNokta(float süre)
        {
            float bütçe = _t.moveSpeed * Mathf.Max(0f, süre - _t.bossReactionWindow)
                        + _attack.SafeSpotRadius - 0.5f;
            Vector3 en = Bootstrap.ArenaCenter; float enYakın = float.MaxValue;
            for (int i = 0; i < 24; i++)
            {
                float açı = Random.value * Mathf.PI * 2f;
                float r = Random.Range(0.15f, 0.75f) * _t.arenaRadius;
                var p = new Vector3(Bootstrap.ArenaCenter.x + Mathf.Cos(açı) * r,
                                    Bootstrap.ArenaCenter.y,
                                    Bootstrap.ArenaCenter.z + Mathf.Sin(açı) * r);
                float d = _player != null ? new Vector2(p.x - _player.position.x, p.z - _player.position.z).magnitude : 0f;
                if (d <= bütçe) return p;
                if (d < enYakın) { enYakın = d; en = p; }   // hiçbiri yetmezse en yakını
            }
            return en;
        }

        /// <summary>
        /// Yardım çağır. ⚠️ TAVANLI: tavansız çağırma, oyuncu yavaş oynadığında
        /// odayı doldurup dövüşü matematiksel olarak kazanılamaz yapardı. Boss'un
        /// baskısı ARTMALI, dövüş imkânsızlaşmamalı.
        ///
        /// ⚠️ Çağrılanlar boss'un KENDİ TÜRÜ — rastgele düşman "boss zorlaştı"
        /// olurdu; kendi türünü çağırmak "boss kendini çoğaltıyor" oluyor ve
        /// mekaniği ANLATIYOR.
        /// </summary>
        void Çağır()
        {
            int canlı = 0;
            foreach (var e in All) if (e != null && e.Summoned) canlı++;
            if (canlı >= Boss.SummonMaxAlive) return;

            int adet = Mathf.Min(Boss.SummonCount(Phase), Boss.SummonMaxAlive - canlı);
            for (int i = 0; i < adet; i++)
            {
                // Boss'un ETRAFINDA doğuyorlar — oyuncunun üstünde değil.
                // Üstünde doğsalardı kaçınılamaz hasar olurdu; bu oyunda her
                // tehdidin bir cevabı olmak zorunda.
                float açı = (360f / Mathf.Max(1, adet)) * i * Mathf.Deg2Rad;
                var yer = transform.position + new Vector3(Mathf.Cos(açı), 0f, Mathf.Sin(açı)) * (Radius + 2.2f);
                yer = ArenayaSığdır(yer);   // kenardaki boss dışarıya doğurmasın
                yer.y = Bootstrap.ArenaCenter.y;

                var yeni = Bootstrap.SpawnEnemyAt(_t, yer, _player, Kamera,
                                                  poolMultiplier: 0.55f, boss: false,
                                                  kind: Boss.SummonKind, parent: transform.parent);
                if (yeni != null) yeni.MarkAsSummoned();
            }
            Sfx.Slam();
        }

        /// <summary>Boss'un çağırdığı yardımcı mı — tavan sayımı bunu kullanıyor.</summary>
        public bool Summoned { get; private set; }
        public void MarkAsSummoned() => Summoned = true;

        /// <summary>
        /// FAZ 3: önceki boss'ların hepsini çağır, kendin UYKUYA çekil.
        /// Enes: *"ilk önce önceki bossların her birini çağırsın, hepsi ölünce
        /// kendisi gelsin."*
        ///
        /// ⚠️ Rakipler YANKI, tam boss değil: havuzları kısılmış ve faz
        /// değiştirmiyorlar. Tam boss olsalardı final dört ayrı boss dövüşü
        /// olurdu — Enes'in istediği "hepsi ölünce kendisi gelsin", dört boss
        /// dövüşü üst üste değil.
        /// </summary>
        List<BossKind> _rivalQueue;

        void ÇağırRakipleri()
        {
            Dormant = true;

            // ⚠️ Faz 3 hazırlığın ORTASINDA gelebilir. Uykuda `Update` durduğu için
            // hazırlık sayacı donuyordu; boss uyanınca kalan süre dolup UYARISIZ
            // vuruyordu (telgraf çoktan sönmüştü). Uykuya giren boss saldırısını bırakır.
            if (_telegraph != null) { _telegraph.Cancel(); _telegraph = null; }
            EnterState(State.Chase, 0f);

            // ⚠️ RASTGELE SIRA, HER OYNAYIŞTA FARKLI (Enes). Sabit sıra olsaydı
            // final ezberlenir ve ikinci denemede bir sıralamanın tekrarı olurdu.
            // Sıra değişince aynı dört dövüş her seferinde başka bir ritim kuruyor:
            // hangi mekanikle karşılaşacağını bilmemek finalin kendi gerilimi.
            _rivalQueue = new List<BossKind>();
            for (int i = 0; i < BossKind.All.Length - 1; i++) _rivalQueue.Add(BossKind.All[i]);

            for (int i = _rivalQueue.Count - 1; i > 0; i--)      // Fisher-Yates
            {
                int j = Random.Range(0, i + 1);
                (_rivalQueue[i], _rivalQueue[j]) = (_rivalQueue[j], _rivalQueue[i]);
            }

            Debug.Log($"[Tek Can] {Boss.Name} FAZ 3 — dört alacaklısını çağırıyor. " +
                      "Hepsi ölmeden kendisine dokunamazsın.");
            ÇağırSıradakiRakibi();
            Paint();
        }

        /// <summary>
        /// Kuyruktaki bir sonraki rakibi sahneye sür.
        ///
        /// ⚠️ RAKİPLER YENDİĞİNDEN GÜÇLÜ (Enes). Bunu "daha çok can" ile değil
        /// FAZ 2'DE BAŞLATARAK yapıyoruz: onları yenerken faz 1'i geçip faz 2'yi
        /// öğrenmiştin; burada faz 2 dağarcığıyla, ilk saniyeden geliyorlar.
        /// Havuz da bir miktar yüksek. "Daha çok can" tek başına ZIRHLI dersinin
        /// tekrarı olurdu: kalınlık zorluk değil, süre.
        /// </summary>
        void ÇağırSıradakiRakibi()
        {
            if (_rivalQueue == null || _rivalQueue.Count == 0) return;

            var kind = _rivalQueue[0];
            _rivalQueue.RemoveAt(0);

            // Oyuncunun ÜSTÜNDE değil, arenanın karşı tarafında doğuyor:
            // her tehdidin bir cevabı olmalı, kaçınılamaz hasar olmamalı.
            var yön = (transform.position - Bootstrap.ArenaCenter);
            yön.y = 0f;
            if (yön.sqrMagnitude < 0.01f) yön = Vector3.forward;
            var yer = Bootstrap.ArenaCenter - yön.normalized * (_t.arenaRadius * 0.45f);
            yer.y = Bootstrap.ArenaCenter.y;

            // ⚠️ Havuz O BOSS'UN kendi havuzunun payı (Tuning: "çağırdığı önceki
            // boss'ların havuz çarpanı"). Önceden SIRADAN düşman havuzunun payıydı:
            // 10 × 0.7 = 7 sn, yani "yendiğinden güçlü" rakip iki vuruşta ölüyordu
            // (bölge 1 boss'u ~200 sn). Büyüme, boss'un kendi bölgesinin büyümesi.
            float rakipHavuzu = _t.bossPoolMultiplier * Mathf.Pow(_t.regionBossGrowth, kind.Index)
                              * _t.rivalPoolMultiplier;
            var rakip = Bootstrap.SpawnEnemyAt(_t, yer, _player, Kamera,
                                               poolMultiplier: rakipHavuzu, boss: true,
                                               parent: transform.parent,
                                               bossKind: kind);
            if (rakip == null) return;

            rakip.MarkAsSummonedRival();
            rakip.StartAtPhaseTwo();

            Debug.Log($"[Tek Can] {kind.Name} çağrıldı — FAZ 2 gücüyle. " +
                      $"Kuyrukta {_rivalQueue.Count} kişi daha var.");
        }

        /// <summary>
        /// Doğrudan faz 2'de başlat — çağrılan rakipler için.
        /// ⚠️ `AdvancePhase` çağrılmıyor: o, faz geçişi GÖSTERİSİNİ yapıyor
        /// (halka, sarsıntı, hit-stop) ve doğar doğmaz patlaması yanlış okunurdu —
        /// "faz değişti" değil "bu böyle geldi" denmeli.
        /// </summary>
        public void StartAtPhaseTwo()
        {
            if (Boss == null || Phase >= 2) return;
            Phase = 2;

            // ⚠️ Gelişini DUYUR. Duyuru mekanizması zaten var (faz geçişi onu
            // kullanıyor); rakip sahneye çıkarken kullanmasaydı oyunun en önemli
            // dövüşünde ekranda HİÇBİR ŞEY yazmazdı — kim geldiğini Console'dan
            // öğrenmesi beklenirdi. Var olan yolu kullanmak, yeni bir duyuru
            // sistemi yazmaktan hem küçük hem tutarlı.
            PhaseTwoAnnouncement = Time.unscaledTime;

            _attack = null; _repeatsLeft = 0; _chained = null;
            if (Boss.SplitsOnPhaseTwo) Split();
            Paint();
        }

        void AdvancePhase()
        {
            Phase++;
            PhaseTwoAnnouncement = Time.unscaledTime;

            // ⚠️ FAZ 3: önceki boss'ları çağır ve UYKUYA çekil (son boss).
            if (Boss.SummonsRivalsInPhase == Phase) ÇağırRakipleri();

            // Dağarcık değişti: sıradaki saldırı YENİ listeden seçilsin.
            // ⚠️ `_attack`'e DOKUNMA: faz hazırlığın ortasında değişebiliyor (son vuruş
            // havuzu boşaltıp fazı çeviriyor). Sıfırlanırsa ekrandaki telgraf eski
            // saldırıyı gösterirken düz koni iniyordu — yeşil "güvenli" alanda vurulmak.
            // Sıradaki hazırlıkta PickAttack onu zaten yenisiyle eziyor.
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
            var ikiz = Bootstrap.SpawnEnemyAt(_t, transform.position + yan, _player, Kamera,
                                              poolMultiplier: 1f, boss: true, kind: Kind,
                                              parent: transform.parent, bossKind: Boss);

            // ⚠️ Doğanı DOĞRUDAN alıyoruz. Sahnede "aynı türden faz 1'de olan" diye
            // aramak, ikinci bir bölünmede yanlışını bulabilirdi.
            if (ikiz == null) return;

            // ⚠️ Yankının ikizi de YANKI. İşaretlenmezse son boss onu beklemeden
            // sıradaki rakibi çağırıyor ya da uyanıyordu ("hepsi ölünce kendisi
            // gelsin" deliniyordu) ve ikizin çaldığı zaman KEFİL'in defterine yazılıyordu.
            if (SummonedRival) ikiz.MarkAsSummonedRival();
            ikiz.BecomeTwin(TimeLeft);
        }

        /// <summary>Bölünmeden doğan ikiz — `Split` çağırıyor.</summary>
        void BecomeTwin(float pool)
        {
            TimeLeft = pool;
            _startingPool = pool;
            Phase = 2;
            OnCredit = true;
            // Dayanak da küçülür: yalnızca ölçek küçülseydi ilk isabet darbesi
            // `_baseScale`'e dönüp ikizi kalıcı olarak TAM boya büyütüyordu.
            _baseScale *= 0.8f;
            transform.localScale = _baseScale;
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
                      new Color(KindColor.r, KindColor.g, KindColor.b, 0.7f),
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
            // ⚠️ UYKU GÖRÜNMELİ. Dokunulmaz bir şeye vuran oyuncu sebebini
            // ekranda görmeli, yoksa "boss bugged" der. Soluk ve donuk: "burada
            // ama şu an senin işin değil".
            else if (Dormant) c = Color.Lerp(KindColor, new Color(0.25f, 0.25f, 0.3f), 0.72f);
            else if (Fleeing) c = Color.Lerp(KindColor, Color.white, 0.45f);        // kaçıyor: soluyor
            else c = KindColor;                                                     // tür + rütbe rengi

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
