using System;
using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Oyunun tek kaynağı. Can da bu, süre de bu, para da bu.
    /// Can barı YOKTUR ve yazılmayacaktır — bu sınıf onun yerine geçer.
    /// </summary>
    public class Clock : MonoBehaviour
    {
        public static Clock Instance { get; private set; }

        /// <summary>
        /// Play oturumları arası statik kalıntıyı temizler (bkz. Bootstrap.ResetStatics).
        /// ⚠️ Kritik: domain reload kapalıyken Instance ÖNCEKİ oturumun yok edilmiş
        /// Clock'unu tutar. `Clock.Instance?.Add(...)` C# null kontrolü yapar ve yok
        /// edilmiş Unity nesnesi C# tarafında null DEĞİLDİR — yani hasar ölü saate
        /// yazılır, HUD yeni saati gösterir, hiçbir şey olmuyormuş gibi görünür.
        /// </summary>
        public static void Reset() => Instance = null;

        Tuning _t;
        Transform _player;
        // ⚠️ double: saat ve süre KARE KARE birikiyor. float'ta saat ~6000'e çıkınca kare başı
        // akış (yüksek FPS'te ~0.0003 sn) yuvarlanıp KAYBOLUYORDU — bot 3600 kare/sn koştu ve
        // geç odalarda saat hiç akmadı, 9931 sn'lik "şişmiş" saat ölçüldü (2026-10-13).
        // 8 saatlik koşuda RunTime da aynı yolla bozulurdu.
        double _seconds, _runTime, _tickDrain;
        public float Seconds { get => (float)_seconds; private set => _seconds = value; }
        public bool Dead { get; private set; }
        public bool InCombat { get; private set; }

        /// <summary>
        /// Akış durdurulmuş mu — SUNAK ODASI kuruyor, `RoomManager` yönetiyor.
        ///
        /// ⚠️ Bir zamanlar DURGUN silahı için vardı ve silahla birlikte silinmişti.
        /// Geri geldi ama SAHİBİ BAŞKA: artık bir MEKÂN özelliği, silah değil.
        /// Sahibi tek olduğu için "kim dondurdu" sorusu hiç doğmuyor.
        /// </summary>
        public bool Frozen { get; set; }

        /// <summary>
        /// Akış çarpanı — `RoomManager` her odada kuruyor (boss / mini boss odasında yavaş).
        /// Faz 28: ana boss 6–8 dk sürecek; bugünkü akışla (~1.6 sn/sn) o dövüşte saat (can) erirdi.
        /// </summary>
        public float DrainMultiplier { get; set; } = 1f;

        // ── koşu boyunca yükseltilebilen değerler ────────────────────────────
        // ⚠️ Bunlar Tuning'den KOPYALANIR, Tuning'e YAZILMAZ. ScriptableObject
        // çalışma anında değiştirilirse editörde kalıcı olur ve bir sonraki koşu
        // önceki koşunun yükseltmeleriyle başlar. Sunaklar burayı değiştirir.
        public float HarvestRate { get; private set; }
        public float AttackDamage { get; private set; }
        public float DashCooldown { get; private set; }

        // ── koşu defteri ─────────────────────────────────────────────────────
        // Koşu bitiş ekranı bunları gösteriyor. Saat zaten koşunun kasası,
        // hesabı da burada tutmak tutarlı.
        public float RunTime => (float)_runTime;
        /// <summary>Saatin kendi akışıyla (aura ve darbe HARİÇ) giden toplam — oda hesabı akışı auradan ayırıyor.</summary>
        public float TotalTickDrain => (float)_tickDrain;
        public float TotalHarvested { get; private set; }
        public float TotalSpent { get; private set; }
        public int EnemiesKilled { get; private set; }
        public int Purchases { get; private set; }

        /// <summary>Boss yenildi mi — koşunun ölümden başka bir sonu var artık.</summary>
        public bool Won { get; private set; }

        public event Action OnDeath;
        public event Action OnWin;

        public void Win()
        {
            if (Dead || Won) return;
            Won = true;
            Sfx.Win();
            OnWin?.Invoke();
        }

        /// <summary>
        /// Checkpoint'teki yükseltmeleri geri yükler. `Bootstrap` çağırıyor.
        /// ⚠️ Checkpoint zamanı geri verip build'i vermemek yarım geri yükleme olurdu:
        /// eski zamanınla ama sıfır güçle aynı odalara girerdin.
        /// </summary>
        public void RestoreUpgrades(float harvest, float damage, float dashCd, int[] alınan = null)
        {
            if (harvest > 0f) HarvestRate = Mathf.Min(harvest, _t.harvestRateCap);
            if (damage > 0f) AttackDamage = damage;
            if (dashCd > 0f) DashCooldown = dashCd;
            // ⚠️ Sayım da geri yükleniyor: yoksa checkpoint'e dönen oyuncu güçlü ama fiyatlar
            // SIFIRDAN — kalıcı fiyat bir ölümle delinirdi. (Eski kayıtta dizi yok → 0.)
            if (alınan != null && alınan.Length == _alınan.Length) System.Array.Copy(alınan, _alınan, _alınan.Length);
        }

        // ── SUNAK EKONOMİSİ (Faz 27) ──────────────────────────────────────────
        // Bot oyunu 9 dk'da bitirdi: 43 alım, hasar ×5.8, boss'lar 20 sn'de eridi. Sebep iki:
        // fiyat her bölgenin sunağında SIFIRLANIYORDU, ve her alım aynı miktarı veriyordu.
        // Şimdi her türün alım sayısı KOŞU boyunca sürüyor; fiyat da getiri de ona bağlı.
        // ⚠️ Tek yer: sunak, TAM GÜÇ (F7), kayıt ve zorluk raporu bunları çağırıyor.

        /// <summary>Türe göre (hasat, hasar, dash) bu koşuda kaç alım yapıldı.</summary>
        readonly int[] _alınan = new int[3];

        public int Alınan(Altar.Kind k) => k == Altar.Kind.Repair ? 0 : _alınan[(int)k];
        public int[] AlınanKopya => (int[])_alınan.Clone();

        /// <summary>
        /// n. alımın (0'dan) fiyatı — hangi sunakta olursa olsun: taban × artış^n, ama EN AZ saatin
        /// `altarPricePercent`'i (Enes 2026-10-14, B: "almazsa boss'ları kesemesin, biriktiren ödesin").
        /// </summary>
        public static float Fiyat(Tuning t, int n, float saat = 0f)
            => Mathf.Max(t.altarBasePrice * Mathf.Pow(t.altarPriceGrowth, n), t.altarPricePercent * saat);

        /// <summary>
        /// HASAT DOYMASI (Faz 28, Enes: "süre arttıkça artmasını zorlaştıran bir yapı"). Saat tavanın
        /// altındayken hasat tam; üstünde ×(tavan / saat) — saat tavanın 2 katıysa hasat yarı.
        /// İyi oyuncunun saati bir dengede durur, az saati olana dokunmaz. Tavan bölgeyle büyür.
        /// </summary>
        public static float Doyma(Tuning t, float saat, int bölge)
        {
            float tavan = t.harvestSoftCap * Mathf.Pow(t.harvestSoftCapGrowth, bölge);
            return saat <= tavan ? 1f : tavan / saat;
        }

        public float HasatDoyması
        {
            get
            {
                if (_t == null) return 1f;
                var rm = RoomManager.Instance;
                return Doyma(_t, Seconds, rm != null ? rm.RegionIndex : 0);
            }
        }

        /// <summary>n. alımın (0'dan) getirisi: adım × azalma^n. Toplam sınırlı: adım / (1 − azalma).</summary>
        public static float Getiri(Tuning t, Altar.Kind k, int n)
        {
            float adım = k == Altar.Kind.Harvest ? t.altarHarvestStep
                       : k == Altar.Kind.Damage ? t.altarDamageStep
                       : k == Altar.Kind.Dash ? t.altarDashStep : 0f;
            return adım * Mathf.Pow(t.altarStepDecay, n);
        }

        public float SıradakiFiyat(Altar.Kind k) => Fiyat(_t, Alınan(k), Seconds);
        public float SıradakiGetiri(Altar.Kind k) => Getiri(_t, k, Alınan(k));

        /// <summary>Bir alımın ETKİSİ (bedel `TryBuy`'da). Sayımı ilerletir.</summary>
        public void Yükselt(Altar.Kind k)
        {
            if (k == Altar.Kind.Repair) return;
            float d = SıradakiGetiri(k);
            if (k == Altar.Kind.Harvest) UpgradeHarvest(d);
            else if (k == Altar.Kind.Damage) UpgradeDamage(d);
            else UpgradeDash(d);
            _alınan[(int)k]++;
        }

        /// <summary>
        /// Diriliş: ölümü geri alır. YALNIZCA `RevivalSystem` çağırır — ölümün geri
        /// alınabilir olması bir eşyaya bağlı, keyfî değil.
        /// </summary>
        /// <summary>
        /// Dövüşle KAZANILMAMIŞ, dışarıdan verilen zaman: diriliş eşyaları ve test
        /// kısayolu (F5).
        ///
        /// ⚠️ Oda hesabı bunu bilmek ZORUNDA. Bilmezse "akış" negatif çıkar:
        ///   akış = hasat + bağış − harcama − net
        /// F5'e basılan bir oda ölçüm olarak çöp olurdu ve o çöp sayılara göre
        /// ayar yapardık.
        /// </summary>
        public float TotalGranted { get; private set; }

        public void Revive(float seconds)
        {
            float yeni = Mathf.Max(0.5f, seconds);   // 0 ile dirilmek anında tekrar ölmek demek
            TotalGranted += Mathf.Max(0f, yeni - Seconds);
            Dead = false;
            Seconds = yeni;
            LastGainAt = -99f;
        }

        /// <param name="startSeconds">Checkpoint'ten dönülüyorsa oradaki zaman;
        /// 0'dan küçükse koşu başlangıcı (`startSeconds`).</param>
        public void Init(Tuning tuning, Transform player, float startSeconds = -1f)
        {
            Instance = this;
            _t = tuning;
            _player = player;
            Seconds = startSeconds > 0f ? startSeconds : tuning.startSeconds;
            HarvestRate = tuning.harvestRate;
            AttackDamage = tuning.attackDamage;
            DashCooldown = tuning.dashCooldown;
            Dead = false;
            Won = false;     // taze saat: ölüm gibi zafer de sıfırlanır
        }

        void Update() => Tick(Time.deltaTime);

        /// <summary>
        /// Saatin bir adımı. ⚠️ dt DIŞARIDAN alınıyor — `Enemy.ApplyAura`, `Arc.Step`,
        /// `Mote.Step` ile aynı gerekçe: editör modunda `Time.deltaTime` 0 olduğu için
        /// akış hiç ÖLÇÜLEMİYORDU. Öz-denetim çarpanı kendi hesaplayıp "doğru" diyordu;
        /// `Clock` o çarpanı uygulamayı unutsa test yine geçerdi. Artık gerçek saat
        /// ölçülüyor. (`lessons.md` 6 ve 9'un birleşimi.)
        /// </summary>
        public void Tick(float dt)
        {
            if (Dead || Won || _t == null) return;

            _runTime += dt;
            InCombat = AnyLivingEnemyNear();

            // ⚠️ SUNAK ODASINDA AKIŞ DURUYOR (Enes kararı, Faz 19).
            // `RunTime` yine ilerliyor: geçen SÜRE gerçek, akmayan şey oyuncunun
            // KAYNAĞI. İkisini karıştırmak ölçüm araçlarını bozardı.
            //
            // ⚠️ Yasayı bozmuyor: ikinci bir para birimi doğmuyor, yalnızca bir
            // odada harcama olmuyor.
            //
            // ⚠️ NE DURUYOR, NE DURMUYOR — tam sınır:
            //   DURAN     : SANİYE. Oyunun tek kaynağı, yani yasanın konusu olan şey.
            //   DURMAYAN  : bekleme süreleri (özel saldırı, dash). Onlar GERÇEK
            //               zamanla işliyor ve hep öyleydi — ayrı bir saat.
            // Yani beklemek SANİYE kazandırmıyor (sömürü yok), ama özel saldırını
            // hazır hale getiriyor. Bu bilerek: boss'a hazırlanma odası burası.
            // ⚠️ "Hiçbir şey kazandırmıyor" diye yazmıştım ve YANLIŞTI — bekleme
            // süreleri işlemeye devam ediyor. Yorum düzeltildi, kod değil: sınır
            // zaten doğru yerde.
            if (Frozen) return;

            // ⚠️ BÖLGE KURALI buradan giriyor. Dövüş dışı akış çarpanı (DARALMA) ve
            // arka plan emişi (SIZINTI) — ikisi de saatten geçiyor, çünkü yasa o.
            // Bölgeye özel zırh/direnç olsaydı ikinci bir sistem doğardı.
            float drain = InCombat ? _t.drainInCombat : _t.drainOutOfCombat * Region.Drain;
            float akış = (drain + Region.Background) * DrainMultiplier * dt;
            _seconds -= akış;
            _tickDrain += akış;
            CheckDeath();
        }

        /// <summary>Saatin sıfıra inmesinin tek çıkışı. Akış da harcama da buradan geçer.</summary>
        void CheckDeath()
        {
            // ⚠️ KAZANILMIŞ koşu ölemez. Kazanmadan önce bırakılmış bir yer işareti
            // 1.3 sn sonra patlayıp saati sıfırlayınca eşya harcatıyor, ÇAĞRI ile
            // zaferi siliyor ya da kazandıktan sonra "oyun bitti" yazdırıyordu (denetim 2026-09-23).
            if (Dead || Won || Seconds > 0f) return;
            Seconds = 0f;
            Dead = true;
            Sfx.Die();
            OnDeath?.Invoke();
        }

        /// <summary>Son hasadın miktarı ve ne zaman olduğu — HUD bunu görünür kılıyor.</summary>
        public float LastGain { get; private set; }
        public float LastGainAt { get; private set; } = -99f;

        /// <summary>Hasat: hasar verdikçe sürekli kazanç. Öldürünce toplu ödeme yoktur.</summary>
        public void Add(float seconds)
        {
            if (Dead) return;
            seconds = Mathf.Max(0f, seconds);
            if (seconds <= 0f) return;

            _seconds += seconds;
            TotalHarvested += seconds;
            // Yalnızca BÜYÜK kazançlarda ses: KASA'nın toplu ödemesi, FAİZLİ'nin iadesi.
            // Her vuruşta çalsaydı isabet sesiyle üst üste biner ve ikisi de duyulmazdı —
            // isabet zaten "değdi" diyor, bu ses "kasa doldu" demeli.
            if (seconds >= 1.5f) Sfx.Harvest();

            // Aynı karede birden çok düşmana isabet ederse tek bir toplamda göster.
            LastGain = (Time.unscaledTime - LastGainAt < 0.05f) ? LastGain + seconds : seconds;
            LastGainAt = Time.unscaledTime;
        }

        /// <summary>Darbe yemek ve satın almak aynı kapıdan geçer — ikisi de saniye götürür.</summary>
        /// <summary>
        /// Yenen darbelerin toplam bedeli. ⚠️ `TotalSpent` yalnızca SUNAK alımlarını
        /// sayıyor; darbeler hiçbir sayaca yazılmıyordu ve oda hesabında "akış"a
        /// karışıyordu — yani "dövüş mü pahalı, oyalanmak mı" sorusu cevaplanamıyordu.
        /// Tam olarak ayırmak istediğimiz iki şeydi.
        /// </summary>
        public float TotalHitCost { get; private set; }

        /// <param name="fromHit">Yenen darbe mi (ayrı sayaca yazılır) yoksa aura/akış mı.</param>
        public void Spend(float seconds, bool fromHit = false)
        {
            if (Dead || Won) return;
            float x = Mathf.Max(0f, seconds);
            _seconds -= x;
            if (fromHit) TotalHitCost += x;
            CheckDeath();
        }

        /// <summary>Faz 2: saniyeyle satın alınan hasat yükseltmesi. Tavan aşılamaz.</summary>
        public void UpgradeHarvest(float delta)
        {
            HarvestRate = Mathf.Min(HarvestRate + delta, _t.harvestRateCap);
        }

        public void UpgradeDamage(float delta) => AttackDamage += Mathf.Max(0f, delta);

        public void UpgradeDash(float delta) => DashCooldown = Mathf.Max(0.1f, DashCooldown - delta);

        /// <summary>
        /// Satın alma. Ödeyemiyorsan gerçekleşmez — ve ÖLDÜRMEZ:
        /// oyuncu yanlışlıkla kendini bitirememeli, ölüm dövüşte olmalı, menüde değil.
        /// </summary>
        public bool TryBuy(float price)
        {
            if (Dead || price <= 0f || Seconds <= price) return false;
            _seconds -= price;
            TotalSpent += price;
            Purchases++;
            return true;
        }

        public void ReportKill() => EnemiesKilled++;

        bool AnyLivingEnemyNear()
        {
            if (_player == null) return false;
            float r2 = _t.combatRadius * _t.combatRadius;
            var list = Enemy.All;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null) continue;
                if ((e.transform.position - _player.position).sqrMagnitude <= r2) return true;
            }
            return false;
        }
    }
}
