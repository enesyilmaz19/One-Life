using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// İzometrik takip kamerası. Sabit açı, yumuşak takip, ortografik.
    /// ponytail: Cinemachine paketi eklemedik — 20 satır yetiyor. Kamera sarsıntısı,
    /// hedef kilitleme veya çoklu kadraj gerekirse o zaman eklenir.
    /// </summary>
    public class IsoCamera : MonoBehaviour
    {
        public static IsoCamera Instance { get; private set; }

        Tuning _t;
        Transform _target;
        Vector3 _offset;
        float _shake, _shakeDecay;

        /// <summary>İsabette kamerayı sarsar. Hit-stop'la birlikte tokluğun ikinci yarısı.</summary>
        public static void Shake(float amount)
        {
            if (Instance == null) return;
            Instance._shake = Mathf.Max(Instance._shake, amount);
            Instance._shakeDecay = amount / 0.18f;
        }

        public void Init(Tuning tuning, Transform target)
        {
            Instance = this;
            _t = tuning;
            _target = target;

            var cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = tuning.camOrthoSize;

            transform.rotation = Quaternion.Euler(tuning.camPitch, tuning.camYaw, 0f);
            _offset = -transform.forward * 40f;
            transform.position = target.position + _offset;
        }

        void LateUpdate()
        {
            if (_target == null || _t == null) return;

            // Play modunda ayar çevrildiğinde anında görünsün.
            transform.rotation = Quaternion.Euler(_t.camPitch, _t.camYaw, 0f);
            _offset = -transform.forward * 40f;
            GetComponent<Camera>().orthographicSize = _t.camOrthoSize;

            transform.position = Vector3.Lerp(transform.position, _target.position + _offset,
                1f - Mathf.Exp(-_t.camFollowLerp * Time.unscaledDeltaTime));

            // Sarsıntı unscaled zamanla iner ki hit-stop sırasında da titresin.
            if (_shake > 0f)
            {
                _shake = Mathf.Max(0f, _shake - _shakeDecay * Time.unscaledDeltaTime);
                transform.position += (transform.right * Random.Range(-1f, 1f) +
                                       transform.up * Random.Range(-1f, 1f)) * _shake;
            }
        }
    }
}
