using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Odanın çıkışı. Düşmanlar durduğu sürece KAPALI, oda temizlenince açılıyor.
    ///
    /// ⚠️ Geçiş kendiliğinden OLMUYOR — oyuncunun yürüyüp girmesi gerekiyor.
    /// Sebep: saat işlemeye devam ediyor. Kapı açıldıktan sonra oyalanmak
    /// (sunağa bakmak, düşünmek, nefes almak) doğrudan ömürden kesiliyor.
    /// Otomatik geçiş bu kararı elinden alırdı; oyunun yasası tam olarak
    /// "oyalanmak pahalıdır" diyor.
    /// </summary>
    public class ExitGate : MonoBehaviour
    {
        static readonly Color ClosedColor = new Color(0.30f, 0.30f, 0.36f);
        static readonly Color OpenColor = new Color(0.45f, 1f, 0.60f);

        Transform _player;
        Renderer _renderer;
        float _enterRadius;
        bool _open;

        public bool IsOpen => _open;

        public static ExitGate Spawn(Tuning t, Transform parent, Vector3 pos, Transform player)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "ExitGate";
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(2.4f, 0.12f, 2.4f);   // yassı disk — zeminde durur

            // `Enemy` ile aynı ayrım: editör modunda Destroy hata basar (öz-denetim burayı çalıştırıyor).
            var col = go.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col);

            var gate = go.AddComponent<ExitGate>();
            gate._player = player;
            gate._renderer = go.GetComponent<Renderer>();
            gate._enterRadius = 1.6f;
            gate.Paint();
            return gate;
        }

        public void Open()
        {
            if (_open) return;
            _open = true;
            Paint();

            // Açılış görünür olmalı: dövüş bitti, göz düşmanlardaydı, kapı arkada.
            Arc.Spawn(transform.position, Quaternion.identity, 3.2f, 360f,
                      new Color(OpenColor.r, OpenColor.g, OpenColor.b, 0.6f), 0.6f,
                      scaleFrom: 0.3f, scaleTo: 1.5f);
            Sfx.Buy();
        }

        void Update()
        {
            if (!_open || _player == null || RoomManager.Instance == null) return;
            if (Clock.Instance != null && (Clock.Instance.Dead || Clock.Instance.Won)) return;

            // ⚠️ YATAY mesafe. Bu projede y'nin yatay hesaba karışması üç kez hata üretti.
            var d = _player.position - transform.position;
            d.y = 0f;
            if (d.magnitude <= _enterRadius) RoomManager.Instance.NextRoom();
        }

        void Paint()
        {
            if (_renderer == null) return;
            _renderer.material.color = _open ? OpenColor : ClosedColor;
        }
    }
}
