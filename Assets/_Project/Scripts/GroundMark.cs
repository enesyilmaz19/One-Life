using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Yere bırakılan gecikmeli patlama. SİMSAR'ın işi: **durduğun yeri işaretliyor.**
    ///
    /// ⚠️ Bu mekanik doğrudan bir AÇIĞI kapatıyor. Enes testte şunu bildirdi:
    /// *"durduğum yerde vurarak zamanımı 1200'e çıkardım."* Bütün boss'lar oyuncuya
    /// gidiyordu, yani sabit durup vurmak geçerli bir strateji kalıyordu.
    /// SİMSAR sana gelmiyor — DURDUĞUN YERE geliyor. Tek cevabı hareket etmek.
    ///
    /// `Arc` gibi kendi kendini doğuran ve sönen bir nesne; hazırlık boyunca
    /// büyüyen halka uyarıyor, süre dolunca patlıyor.
    /// </summary>
    public class GroundMark : MonoBehaviour
    {
        static readonly Color WarnColor = new Color(0.9f, 0.55f, 1f, 0.35f);

        Transform _player;
        Tuning _t;
        float _radius, _cost, _timer;
        bool _fired;

        public static GroundMark Spawn(Tuning t, Vector3 pos, Transform player,
                                       float radius, float delay, float cost, Transform parent)
        {
            if (player == null) return null;

            var go = new GameObject("GroundMark");
            go.transform.position = pos;
            if (parent != null) go.transform.SetParent(parent);

            var m = go.AddComponent<GroundMark>();
            m._t = t;
            m._player = player;
            m._radius = radius;
            m._cost = cost;
            m._timer = delay;

            // ⚠️ Uyarı GÖRÜNÜR ve DOLUYOR: "burada bir şey olacak" değil,
            // "burada ŞU KADAR SÜRE sonra olacak" demeli. Dolan halka süreyi söylüyor.
            Arc.Spawn(pos, Quaternion.identity, radius, 360f, WarnColor, delay,
                      scaleFrom: 0.25f, scaleTo: 1f);
            return m;
        }

        void Update()
        {
            if (_fired) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            _fired = true;
            Detonate();

            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        /// <summary>Öz-denetim dt'yi kendisi veriyor — editörde `Time.deltaTime` 0.</summary>
        public void Step(float dt)
        {
            if (_fired) return;
            _timer -= dt;
            if (_timer > 0f) return;
            _fired = true;
            Detonate();
        }

        public bool Fired => _fired;

        void Detonate()
        {
            Arc.Spawn(transform.position, Quaternion.identity, _radius, 360f,
                      new Color(1f, 0.4f, 0.9f, 0.55f), 0.25f, scaleFrom: 0.9f, scaleTo: 1.25f);
            Sfx.Slam();

            if (_player == null) return;

            // ⚠️ YATAY mesafe — y'nin yatay hesaba karışması bu projede dört kez hata üretti.
            var d = _player.position - transform.position;
            d.y = 0f;
            if (d.magnitude > _radius) return;

            _player.GetComponent<PlayerController>()?.TakeHit(_cost, transform.position);
        }
    }
}
