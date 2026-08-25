using UnityEngine;
using UnityEngine.InputSystem;

namespace TekCan
{
    /// <summary>
    /// Tek gösterge: saat. Can barı yok, mana yok, para yok — hepsi bu sayı.
    /// Ayrıca sunak istemi ve koşu bitiş ekranı.
    ///
    /// ponytail: greybox için OnGUI. Bu sayı oyunun kimliği olduğu için gerçek
    /// tipografi/animasyon UI geçişinde yapılacak; şimdi okunurluk yeterli.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        Clock _clock;
        PlayerController _player;
        GUIStyle _big, _small, _mid;

        public void Init(Clock clock, PlayerController player)
        {
            _clock = clock;
            _player = player;
            // Ölü kod değil artık: koşu bitiş ekranı bu olaya bağlı.
            clock.OnDeath += OnRunEnded;
        }

        void OnDestroy()
        {
            if (_clock != null) _clock.OnDeath -= OnRunEnded;
        }

        void OnRunEnded() => IsoCamera.Shake(0.5f);

        void Update()
        {
            if (_clock != null && _clock.Dead && RestartPressed()) Bootstrap.Restart();
        }

        static bool RestartPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            return gp != null && gp.startButton.wasPressedThisFrame;
        }

        void EnsureStyles()
        {
            if (_big != null) return;
            _big = new GUIStyle(GUI.skin.label) { fontSize = 64, alignment = TextAnchor.UpperCenter };
            _mid = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.UpperCenter };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperCenter };
        }

        void OnGUI()
        {
            if (_clock == null) return;
            EnsureStyles();

            if (_clock.Dead) { DrawRunEnd(); return; }

            DrawClock();
            DrawAltarPrompt();

            GUI.Label(new Rect(12f, Screen.height - 26f, 900f, 20f),
                "WASD · Sol tık saldırı · Shift/Space dash · dash+saldırı = DASH-ATAK · E sunak · 1-4 silah", _small);
        }

        void DrawClock()
        {
            // Hasat GÖRÜNÜR olmalı: saat kazandığı anda yeşile dönüp büyüyor ve
            // kazancı yazıyor. (Reddedilen 'düşman üstünde uçuşan sayı' değil —
            // tek yerde, saatin kendi tepkisi.)
            float sinceGain = Time.unscaledTime - _clock.LastGainAt;
            bool gaining = sinceGain < 0.30f;
            float punch = gaining ? 1f - sinceGain / 0.30f : 0f;

            _big.fontSize = Mathf.RoundToInt(64 + 22 * punch);
            _big.normal.textColor = gaining
                ? Color.Lerp(Color.white, new Color(0.4f, 1f, 0.5f), punch)
                : (_clock.Seconds < 10f
                    ? Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.unscaledTime * 4f, 1f))
                    : Color.white);

            var r = new Rect(0f, 20f, Screen.width, 90f);
            GUI.Label(r, _clock.Seconds.ToString("0.0"), _big);

            if (gaining)
            {
                _small.normal.textColor = new Color(0.4f, 1f, 0.5f, punch);
                GUI.Label(new Rect(Screen.width * 0.5f + 70f, 34f, 160f, 30f), $"+{_clock.LastGain:0.0}", _small);
                _small.normal.textColor = Color.white;
            }

            r.y += 78f;
            GUI.Label(r, _clock.InCombat ? "dövüşte" : "DÖVÜŞ DIŞI — saat hızlı iniyor", _small);

            r.y += 24f;
            GUI.Label(r, $"hasat %{_clock.HarvestRate * 100f:0}   ·   hasar {_clock.AttackDamage:0.0}", _small);

            DrawWeaponBar();
        }

        /// <summary>
        /// Kuşanılan silah + hasat çarpanı. Çarpanı göstermek şart: tek hedefli
        /// silahların koniyle nasıl rekabet ettiği ancak o sayıyla anlaşılıyor.
        /// </summary>
        void DrawWeaponBar()
        {
            var pc = _player;
            if (pc == null) return;

            var weapons = Weapon.All;
            float w = 150f, total = weapons.Length * w;
            float x = (Screen.width - total) * 0.5f;
            float y = Screen.height - 62f;

            for (int i = 0; i < weapons.Length; i++)
            {
                bool equipped = weapons[i] == pc.Weapon;
                var c = weapons[i].Tint; c.a = equipped ? 1f : 0.35f;
                _small.normal.textColor = c;

                string mult = weapons[i].HarvestMultiplier > 1.001f
                    ? $"×{weapons[i].HarvestMultiplier:0.0}" : "";
                string tek = weapons[i].MaxTargets == 1 ? "tek" : "koni";

                GUI.Label(new Rect(x + i * w, y, w, 40f),
                          $"{i + 1}  {weapons[i].Name}\n{tek} {mult}", _small);
            }
            _small.normal.textColor = Color.white;

            if (_clock.Frozen)
            {
                _mid.normal.textColor = new Color(0.6f, 0.8f, 1f);
                GUI.Label(new Rect(0f, Screen.height * 0.42f, Screen.width, 40f), "SAAT DURDU", _mid);
                _mid.normal.textColor = Color.white;
            }
        }

        /// <summary>Sunağa yaklaşınca ne aldığını ve kaça aldığını söyler.</summary>
        void DrawAltarPrompt()
        {
            for (int i = 0; i < Altar.All.Count; i++)
            {
                var a = Altar.All[i];
                if (a == null || !a.PlayerInRange) continue;

                bool afford = !a.SoldOut && _clock.Seconds > a.Price;
                _mid.normal.textColor = a.SoldOut ? new Color(0.5f, 0.5f, 0.55f)
                                      : afford ? new Color(0.5f, 1f, 0.6f)
                                               : new Color(1f, 0.5f, 0.45f);
                GUI.Label(new Rect(0f, Screen.height - 130f, Screen.width, 40f), a.Label(), _mid);
                _mid.normal.textColor = Color.white;
                return;   // aynı anda tek sunak gösterilir
            }
        }

        void DrawRunEnd()
        {
            _big.fontSize = 64;
            _big.normal.textColor = new Color(1f, 0.35f, 0.35f);
            GUI.Label(new Rect(0f, Screen.height * 0.22f, Screen.width, 90f), "KOŞU BİTTİ", _big);

            _mid.normal.textColor = Color.white;
            float net = _clock.TotalHarvested - _clock.TotalSpent;
            string özet =
                $"hayatta kalınan süre   {_clock.RunTime:0.0} sn\n" +
                $"öldürülen düşman   {_clock.EnemiesKilled}\n" +
                $"hasat edilen   {_clock.TotalHarvested:0.0} sn\n" +
                $"sunaklara harcanan   {_clock.TotalSpent:0.0} sn   ({_clock.Purchases} alım)\n" +
                $"net kazanç   {net:+0.0;-0.0} sn\n" +
                $"son hasat oranı   %{_clock.HarvestRate * 100f:0}";
            GUI.Label(new Rect(0f, Screen.height * 0.35f, Screen.width, 260f), özet, _mid);

            _small.normal.textColor = new Color(0.8f, 0.8f, 0.85f);
            GUI.Label(new Rect(0f, Screen.height * 0.72f, Screen.width, 30f),
                      "R  ·  yeniden başla", _small);
            _small.normal.textColor = Color.white;
        }
    }
}
