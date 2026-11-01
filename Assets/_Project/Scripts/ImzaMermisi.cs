using System.Collections.Generic;
using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Faz 43b — ÇİFT İMZA'nın mermisi (Enes 2026-10-31: "anlık ulaşmak yerine atış olarak gitsinler öyle tutturalım"). Düz gider,
    /// yolunu kare kare süpürür (hızlı mermi bir karede düşmanın içinden atlamasın), ilk değdiğine iner; menzilde söner.
    /// Vuruşun kuralı sahibinde (`PlayerController.MermiDeğdi`). Mermi ATIŞ anındaki her şeyi taşır: combo'da uçarken silah
    /// değişiyor — değdiği an kuşanılana bakılsaydı hasat ve yükseltmeler yanlış silahtan gelirdi (ÇEK de böyleydi).
    /// ⚠️ dt DIŞARIDAN (tuzak 3): öz-denetim `Step` ile uçuruyor.
    /// </summary>
    public class ImzaMermisi : MonoBehaviour
    {
        public static readonly List<ImzaMermisi> Uçanlar = new List<ImzaMermisi>();
        /// <summary>Merminin yarı genişliği — düşmanın yüzeyine bu kadar yaklaşan değer.</summary>
        public const float Yarıçap = 0.2f;

        PlayerController _sahibi;
        Vector3 _yön;
        float _kalan, _hız;
        bool _bitti;
        Enemy _deldiği;

        // Atış anında yazılanlar
        internal float Hasar, Hasat;
        internal bool Ana, Delici, OnayHakkı, Protesto, Sustur;
        internal PlayerController.İmzaSerisi Seri;
        /// <summary>Güdümlü (TOPLU TAHSİLAT): yalnızca buna gider ve yalnızca buna değer; ölürse söner.</summary>
        internal Enemy Güdüm;

        public static ImzaMermisi At(PlayerController sahibi, Vector3 baş, Vector3 yön, float menzil, float hız, Transform kök)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Mermi";
            var col = go.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            go.transform.SetParent(kök);
            go.transform.position = baş + Vector3.up * 0.5f;
            go.transform.localScale = Vector3.one * 0.18f;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.6f, 0.85f, 0.95f, 1f);

            var m = go.AddComponent<ImzaMermisi>();
            yön.y = 0f;
            m._sahibi = sahibi;
            m._yön = yön.sqrMagnitude > 0.0001f ? yön.normalized : sahibi.transform.forward;
            m._kalan = menzil; m._hız = hız;
            Uçanlar.Add(m);
            return m;
        }

        void Update() => Step(Time.deltaTime);

        void OnDestroy() => Uçanlar.Remove(this);

        public void Step(float dt)
        {
            if (_bitti) return;
            if (_sahibi == null) { Bitir(); return; }
            float adım = Mathf.Min(_hız * dt, _kalan);
            Vector3 p = transform.position;
            if (Güdüm != null)                                              // güdümlü: hedefine döner
            {
                if (!Enemy.All.Contains(Güdüm)) { Bitir(); return; }
                Vector3 g = Güdüm.transform.position - p; g.y = 0f;
                if (g.sqrMagnitude > 0.0001f) _yön = g.normalized;
                _kalan = Mathf.Max(_kalan, adım);                            // hedefine varana dek sönmez
            }
            Enemy hedef = null; float enYakın = float.MaxValue;
            foreach (var e in Enemy.All)                                    // bu karenin yolunda ilk değen
            {
                if (e == null || e.Dormant || e == _deldiği || Güdüm != null && e != Güdüm) continue;
                Vector3 to = e.transform.position - p; to.y = 0f;           // tuzak 2
                float boy = Vector3.Dot(to, _yön);
                if (boy <= 0f || boy - e.Radius > adım) continue;
                if ((to - _yön * boy).magnitude > e.Radius + Yarıçap) continue;
                if (boy < enYakın) { enYakın = boy; hedef = e; }
            }
            if (hedef == null)
            {
                transform.position = p + _yön * adım;
                if ((_kalan -= adım) <= 0f) Bitir();
                return;
            }
            float gitti = Mathf.Max(0f, enYakın - hedef.Radius);
            transform.position = p + _yön * gitti;
            _kalan -= gitti;
            _sahibi.MermiDeğdi(this, hedef);
            if (Delici && _deldiği == null) { _deldiği = hedef; Hasar *= 0.5f; Ana = false; return; }   // DELİCİ MERMİ: bir kez deler, yarımla sürer
            Bitir();
        }

        void Bitir()
        {
            _bitti = true;
            Uçanlar.Remove(this);
            gameObject.SetActive(false);                                    // tuzak 1: Destroy ertelenir
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }

        /// <summary>Havadakilerin hepsini kaldır — öz-denetim kurgusu.</summary>
        public static void Temizle()
        {
            for (int i = Uçanlar.Count - 1; i >= 0; i--) if (Uçanlar[i] != null) Uçanlar[i].Bitir();
            Uçanlar.Clear();
        }
    }
}
