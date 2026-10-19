using System.Collections;
using UnityEngine;

namespace TekCan
{
    /// <summary>İsabette zamanı kısa süre dondurur. Vuruşun tokluğu buradan gelir.</summary>
    public class HitStop : MonoBehaviour
    {
        static HitStop _runner;
        static float _until, _saat;

        /// <summary>
        /// Zaman ölçeğinden bağımsız kare adımı — hit-stop'ta da ilerleyen her şey bunu kullanır (donma, yay,
        /// vurulma parlaması). ⚠️ Hızlandırılmış zamanda (otopilot, `Time.captureDeltaTime`) kare başı SABİT:
        /// `unscaledDeltaTime` orada GERÇEK saat; donan kare sayısı bilgisayarın o anki hızına bağlı olur, aynı
        /// deneme iki kez farklı sonuç verir ve hızlanma ×6'da kalırdı (2026-10-18).
        /// </summary>
        public static float Adım => Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;

        void Update() => _saat += Adım;

        public static void Init(HitStop runner) => _runner = runner;

        /// <summary>Play oturumları arası statik kalıntıyı temizler (bkz. Bootstrap.ResetStatics).</summary>
        public static void Reset()
        {
            _runner = null;
            _until = 0f;
            _saat = 0f;
        }

        public static void Freeze(float seconds)
        {
            if (_runner == null || seconds <= 0f) return;

            // Üst üste binen isabetlerde en uzun olanı kazanır, dondurma zincirlenmez.
            float end = _saat + seconds;
            if (end <= _until) return;
            _until = end;

            _runner.StopAllCoroutines();
            _runner.StartCoroutine(_runner.Run());
        }

        IEnumerator Run()
        {
            Time.timeScale = 0f;
            while (_saat < _until) yield return null;
            Time.timeScale = 1f;
        }
    }
}
