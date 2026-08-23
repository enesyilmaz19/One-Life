using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Düşmanın üstündeki geri sayan saniye. Can barı değil — bedende kalan süre.
    ///
    /// UÇUŞAN HASAR SAYISI YOK. Ekranı rakam çorbasına çeviren, oyunu mobil
    /// gösteren şey odur. Tek, sakin, kalıcı sayı.
    ///
    /// ponytail: yerleşik TextMesh kullanıldı — TMP ayrıca "essentials" import
    /// adımı istiyor. Greybox'ta gereksiz. Faz 2'de tipografi işi gelince TMP'ye geçilir.
    /// </summary>
    public class TimeLabel : MonoBehaviour
    {
        TextMesh _text;
        Transform _cam;

        static readonly Color FullColor = new Color(1f, 0.95f, 0.8f);
        static readonly Color HollowColor = new Color(0.45f, 0.45f, 0.5f);

        public void Init(Transform cameraTransform)
        {
            _cam = cameraTransform;
            _text = gameObject.AddComponent<TextMesh>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var mr = GetComponent<MeshRenderer>();
            if (_text.font != null && mr != null) mr.sharedMaterial = _text.font.material;
            _text.fontSize = 64;
            _text.characterSize = 0.06f;
            _text.anchor = TextAnchor.LowerCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = FullColor;
        }

        public void Set(float seconds, bool hollow)
        {
            if (_text == null) return;
            _text.text = hollow ? "—" : seconds.ToString("0.0");
            _text.color = hollow ? HollowColor : FullColor;
        }

        void LateUpdate()
        {
            if (_cam == null) return;
            transform.rotation = _cam.rotation;   // kameraya dönük dursun
        }
    }
}
