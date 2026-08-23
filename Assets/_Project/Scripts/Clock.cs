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

        Tuning _t;
        Transform _player;
        public float Seconds { get; private set; }
        public bool Dead { get; private set; }
        public bool InCombat { get; private set; }

        /// <summary>Koşu içi yükseltmelerle artar (Faz 2). Tavanı Tuning'de.</summary>
        public float HarvestRate { get; private set; }

        public event Action OnDeath;

        public void Init(Tuning tuning, Transform player)
        {
            Instance = this;
            _t = tuning;
            _player = player;
            Seconds = tuning.startSeconds;
            HarvestRate = tuning.harvestRate;
            Dead = false;
        }

        void Update()
        {
            if (Dead || _t == null) return;

            InCombat = AnyLivingEnemyNear();
            Seconds -= (InCombat ? _t.drainInCombat : _t.drainOutOfCombat) * Time.deltaTime;

            if (Seconds <= 0f)
            {
                Seconds = 0f;
                Dead = true;
                OnDeath?.Invoke();
            }
        }

        /// <summary>Hasat: hasar verdikçe sürekli kazanç. Öldürünce toplu ödeme yoktur.</summary>
        public void Add(float seconds)
        {
            if (Dead) return;
            Seconds += Mathf.Max(0f, seconds);
        }

        /// <summary>Darbe yemek ve satın almak aynı kapıdan geçer — ikisi de saniye götürür.</summary>
        public void Spend(float seconds)
        {
            if (Dead) return;
            Seconds -= Mathf.Max(0f, seconds);
            if (Seconds <= 0f)
            {
                Seconds = 0f;
                Dead = true;
                OnDeath?.Invoke();
            }
        }

        /// <summary>Faz 2: saniyeyle satın alınan hasat yükseltmesi. Tavan aşılamaz.</summary>
        public void UpgradeHarvest(float delta)
        {
            HarvestRate = Mathf.Min(HarvestRate + delta, _t.harvestRateCap);
        }

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
