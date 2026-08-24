using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TekCan
{
    /// <summary>
    /// Saniyeyle satın alma noktası. Oyunun ekonomisi burada kapanıyor:
    /// zamanı harcayarak zamanı toplamakta daha iyi hale geliyorsun.
    ///
    /// Fiyat ömründen kesiliyor — altın yok, ruh yok, ayrı para birimi yok.
    /// Her alım seni ölüme yaklaştırırken ölmeni zorlaştırıyor. Bileşik yatırım
    /// kararı: erken alırsan koşunun geri kalanı kârlı geçer, ama o parayı
    /// bulmak için önce zar zor idare etmen gerekir.
    /// </summary>
    public class Altar : MonoBehaviour
    {
        public enum Kind { Harvest, Damage, Dash }

        public static readonly List<Altar> All = new List<Altar>();
        public static void ResetAll() => All.Clear();

        Tuning _t;
        Transform _player;
        Renderer _renderer;
        int _bought;

        public Kind Type { get; private set; }
        public bool SoldOut => _bought >= _t.altarMaxPurchases;
        public float Price => _t.altarBasePrice * Mathf.Pow(_t.altarPriceGrowth, _bought);
        public bool PlayerInRange { get; private set; }

        static readonly Color HarvestColor = new Color(0.35f, 0.85f, 0.55f);
        static readonly Color DamageColor = new Color(0.9f, 0.55f, 0.35f);
        static readonly Color DashColor = new Color(0.45f, 0.65f, 1f);
        static readonly Color SoldOutColor = new Color(0.3f, 0.3f, 0.33f);

        public void Init(Tuning tuning, Transform player, Kind kind)
        {
            _t = tuning;
            _player = player;
            Type = kind;
            _renderer = GetComponentInChildren<Renderer>();
            All.Add(this);
            Paint();
        }

        void OnDestroy() => All.Remove(this);

        void Update()
        {
            if (_t == null || _player == null) return;

            Vector3 to = _player.position - transform.position;
            to.y = 0f;
            PlayerInRange = to.magnitude <= _t.altarUseRange;

            transform.Rotate(Vector3.up, 40f * Time.deltaTime, Space.World);

            if (PlayerInRange && !SoldOut && UsePressed()) Buy();
        }

        static bool UsePressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonNorth.wasPressedThisFrame;
        }

        void Buy()
        {
            var clock = Clock.Instance;
            if (clock == null || !clock.TryBuy(Price)) return;

            _bought++;
            // Tuning'e DEĞİL Clock'a yazılıyor — asset çalışma anında değiştirilirse
            // yükseltmeler bir sonraki koşuya sızar (bkz. Clock'taki not).
            switch (Type)
            {
                case Kind.Harvest: clock.UpgradeHarvest(_t.altarHarvestStep); break;
                case Kind.Damage:  clock.UpgradeDamage(_t.altarDamageStep); break;
                case Kind.Dash:    clock.UpgradeDash(_t.altarDashStep); break;
            }

            IsoCamera.Shake(_t.hitShake * 0.7f);
            Paint();
        }

        public string Label()
        {
            string what = Type switch
            {
                Kind.Harvest => $"HASAT  +%{_t.altarHarvestStep * 100f:0}",
                Kind.Damage  => $"KESKİNLİK  +{_t.altarDamageStep:0.0}",
                _            => $"ÇEVİKLİK  dash -{_t.altarDashStep:0.00}sn",
            };
            return SoldOut ? $"{what}   [TÜKENDİ]" : $"{what}   —   {Price:0.0} saniye   [E]";
        }

        void Paint()
        {
            if (_renderer == null) return;
            _renderer.material.color = SoldOut ? SoldOutColor : Type switch
            {
                Kind.Harvest => HarvestColor,
                Kind.Damage => DamageColor,
                _ => DashColor,
            };
        }
    }
}
