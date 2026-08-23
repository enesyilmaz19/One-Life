using System.Collections;
using UnityEngine;

namespace TekCan
{
    /// <summary>İsabette zamanı kısa süre dondurur. Vuruşun tokluğu buradan gelir.</summary>
    public class HitStop : MonoBehaviour
    {
        static HitStop _runner;
        static float _until;

        public static void Init(HitStop runner) => _runner = runner;

        public static void Freeze(float seconds)
        {
            if (_runner == null || seconds <= 0f) return;

            // Üst üste binen isabetlerde en uzun olanı kazanır, dondurma zincirlenmez.
            float end = Time.unscaledTime + seconds;
            if (end <= _until) return;
            _until = end;

            _runner.StopAllCoroutines();
            _runner.StartCoroutine(_runner.Run());
        }

        IEnumerator Run()
        {
            Time.timeScale = 0f;
            while (Time.unscaledTime < _until) yield return null;
            Time.timeScale = 1f;
        }
    }
}
