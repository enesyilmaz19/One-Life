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
        public float Seconds { get; private set; }
        public bool Dead { get; private set; }
        public bool InCombat { get; private set; }

        /// <summary>Akış durdurulmuş mu — DURGUN silahı basılı tutulurken.</summary>
        public bool Frozen { get; set; }

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
        public float RunTime { get; private set; }
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
        public void RestoreUpgrades(float harvest, float damage, float dashCd)
        {
            if (harvest > 0f) HarvestRate = Mathf.Min(harvest, _t.harvestRateCap);
            if (damage > 0f) AttackDamage = damage;
            if (dashCd > 0f) DashCooldown = dashCd;
        }

        /// <summary>
        /// Diriliş: ölümü geri alır. YALNIZCA `RevivalSystem` çağırır — ölümün geri
        /// alınabilir olması bir eşyaya bağlı, keyfî değil.
        /// </summary>
        public void Revive(float seconds)
        {
            Dead = false;
            Seconds = Mathf.Max(0.5f, seconds);   // 0 ile dirilmek anında tekrar ölmek demek
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
        }

        void Update()
        {
            if (Dead || Won || _t == null) return;

            RunTime += Time.deltaTime;
            InCombat = AnyLivingEnemyNear();

            // DURGUN silahı akışı durdurabiliyor. Dünya donmuyor — düşmanlar gelmeye
            // devam ediyor — yani bedeli savunmasız kalmak.
            if (Frozen) return;

            Seconds -= (InCombat ? _t.drainInCombat : _t.drainOutOfCombat) * Time.deltaTime;
            CheckDeath();
        }

        /// <summary>Saatin sıfıra inmesinin tek çıkışı. Akış da harcama da buradan geçer.</summary>
        void CheckDeath()
        {
            if (Dead || Seconds > 0f) return;
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

            Seconds += seconds;
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
        public void Spend(float seconds)
        {
            if (Dead) return;
            Seconds -= Mathf.Max(0f, seconds);
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
            Seconds -= price;
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
