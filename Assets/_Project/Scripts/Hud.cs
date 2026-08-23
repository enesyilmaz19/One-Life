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

            _big.normal.textColor = _clock.Seconds < 10f
                ? Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.unscaledTime * 4f, 1f))
                : Color.white;

            var r = new Rect(0f, 20f, Screen.width, 90f);
            GUI.Label(r, _clock.Seconds.ToString("0.0"), _big);

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
