using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Tek gösterge: saat. Can barı yok, mana yok, para yok — hepsi bu sayı.
    ///
    /// ponytail: greybox için OnGUI. Bu sayı oyunun kimliği olduğu için Faz 2'de
    /// gerçek tipografi ve animasyonla yeniden yapılacak.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        Clock _clock;
        GUIStyle _big, _small;

        PlayerController _player;

        public void Init(Clock clock, PlayerController player)
        {
            _clock = clock;
            _player = player;
        }

        /// <summary>
        /// TEŞHİS PANELİ — "hasar inmiyor" sorunu için. Her savurmada hangi kontrolün
        /// elediğini canlı gösteriyor. Sorun çözülünce bu blok komple silinecek.
        /// </summary>
        void DrawDiagnostics()
        {
            if (_player == null) return;

            var st = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                alignment = TextAnchor.UpperLeft,
                wordWrap = false,
                richText = false
            };
            st.normal.textColor = _player.HitsLanded > 0 ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.6f, 0.6f);

            string txt =
                $" TEŞHİS  (F1 gizler)\n" +
                $" sahnedeki düşman : {Enemy.All.Count}\n" +
                $" savurma / isabet : {_player.StrikesFired} / {_player.HitsLanded}\n" +
                $" {_player.LastStrikeReport}";

            GUI.Box(new Rect(12f, 90f, 480f, 96f), txt, st);
        }

        void OnGUI()
        {
            if (_clock == null) return;

            if (_big == null)
            {
                _big = new GUIStyle(GUI.skin.label) { fontSize = 64, alignment = TextAnchor.UpperCenter };
                _small = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperCenter };
            }

            // ⚠️ Hasat GÖRÜNÜR olmalı. Enes'in şikâyeti buydu: "zaman çekip çekmediğimi
            // anlamıyorum". Saat, kazandığı anda yeşile dönüp büyüyor ve kazancı yazıyor.
            // (Bu, reddedilen 'düşman üstünde uçuşan hasar sayısı' değil — tek yerde,
            //  saatin kendi tepkisi.)
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
                GUI.Label(new Rect(Screen.width * 0.5f + 70f, 34f, 160f, 30f),
                          $"+{_clock.LastGain:0.0}", _small);
                _small.normal.textColor = Color.white;
            }

            r.y += 78f;
            GUI.Label(r, _clock.Dead ? "KOŞU BİTTİ"
                : (_clock.InCombat ? "dövüşte" : "DÖVÜŞ DIŞI — saat hızlı iniyor"), _small);

            r.y += 24f;
            GUI.Label(r, $"hasat %{_clock.HarvestRate * 100f:0}", _small);

            // Faz 1 kontrol hatırlatması
            GUI.Label(new Rect(12f, Screen.height - 26f, 600f, 20f),
                "WASD hareket · Sol tık saldırı · Shift/Space dash", _small);

            DrawDiagnostics();
        }
    }
}
