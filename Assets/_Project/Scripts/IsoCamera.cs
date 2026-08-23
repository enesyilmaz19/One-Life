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
        Tuning _t;
        Transform _target;
        Vector3 _offset;

        public void Init(Tuning tuning, Transform target)
        {
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
        }
    }
}
