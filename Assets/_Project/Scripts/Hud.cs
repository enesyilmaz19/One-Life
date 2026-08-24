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


        public void Init(Clock clock) => _clock = clock;

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

        }
    }
}
