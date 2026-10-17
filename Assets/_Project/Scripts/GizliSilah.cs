using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Faz 30 — KEŞİF (Enes: "silahları da gizli saklı yerlere koyup oradan bulmalarını sağlayabiliriz").
    /// Oda temizlenince arenanın kenarında soluk parlayan bir köşe çıkıyor; üstüne yürüyen silahı buluyor.
    /// Kaçırılırsa aynı silah sonraki sunaklarda pahalıya satılıyor (`Kadro.Satılıklar`).
    /// </summary>
    public class GizliSilah : MonoBehaviour
    {
        Transform _player;
        float _menzil;
        public int Silah { get; private set; }

        public void Init(Transform player, int silah, float menzil)
        {
            _player = player;
            Silah = silah;
            _menzil = menzil;
        }

        void Update()
        {
            transform.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);
            Step();
        }

        /// <summary>Oyuncu üstünde mi — öyleyse silahı aç ve kaybol. Öz-denetim doğrudan çağırıyor.</summary>
        public bool Step()
        {
            if (_player == null) return false;
            Vector3 d = _player.position - transform.position;
            d.y = 0f;                                          // tuzak 2
            if (d.magnitude > _menzil) return false;
            Kadro.Aç(Silah, "keşif");
            // ⚠️ Destroy ERTELENİR (tuzak 1): önce kapat, kare sonunda gitsin.
            gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(gameObject);
            return true;
        }
    }
}
