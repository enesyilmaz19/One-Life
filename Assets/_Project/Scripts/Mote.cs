using UnityEngine;
using UnityEngine.Rendering;

namespace TekCan
{
    /// <summary>
    /// Bir hedefe doğru akan zaman zerresi.
    ///
    /// ⚠️ Bu, oyunun KİMLİK ANI. Yasa "tek bir sayı var: saniye" diyor ama Faz 6'ya
    /// kadar hasat yalnızca HUD'daki sayının büyümesiydi — yani oyunun en özgün
    /// mekaniği GÖRÜNMÜYORDU. Zerre, zamanın düşmandan sana AKTIĞINI gösteriyor:
    /// çaldığın şeyin nereden geldiği ve nereye gittiği artık ekranda.
    ///
    /// Yön anlam taşır:
    ///   • düşman → oyuncu : hasat (yeşil), sen çalıyorsun
    ///   • oyuncu → düşman : sızıntı/aura (kırmızı), o çalıyor
    ///
    /// `Arc` ile aynı desen: kendi kendini doğuran MonoBehaviour, paylaşılan materyal,
    /// unscaled zamanla sönme, `Bootstrap.ResetStatics`'e bağlı statik temizlik.
    /// </summary>
    public class Mote : MonoBehaviour
    {
        public static readonly Color HarvestColor = new Color(0.40f, 1f, 0.50f);   // HUD kazanç yeşiliyle aynı
        public static readonly Color DrainColor = new Color(1f, 0.35f, 0.30f);

        static Material _shared;
        static Mesh _sharedMesh;

        Transform _target;
        Vector3 _from;
        Vector3 _fallbackTarget;     // hedef ölürse zerre havada asılı kalmasın
        MeshRenderer _mr;
        MaterialPropertyBlock _mpb;
        Color _color;
        float _size, _arcHeight, _life, _age;

        /// <summary>
        /// <param name="target">Takip edilen hedef — sabit nokta DEĞİL. Oyuncu hareket
        /// ediyor; sabit bir varış noktasına gönderseydik zerre onun arkasında kalır ve
        /// "bana aktı" hissi kaybolurdu.</param>
        /// </summary>
        public static Mote Spawn(Vector3 from, Transform target, Color color,
                                 float size = 0.22f, float life = 0.32f, float arcHeight = 0.9f)
        {
            if (target == null) return null;

            var go = new GameObject("Mote");
            go.transform.position = from;
            go.transform.localScale = Vector3.one * size;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = SharedMesh();

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = UnlitMat.Additive;   // zerre ışık gibi dursun, üst üste binince parlasın
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var m = go.AddComponent<Mote>();
            m._mr = mr;
            m._mpb = new MaterialPropertyBlock();
            m._from = from;
            m._target = target;
            m._fallbackTarget = target.position;
            m._color = color;
            m._size = size;
            m._arcHeight = arcHeight;
            m._life = Mathf.Max(0.01f, life);
            m.Apply(1f);
            return m;
        }

        // unscaled: hit-stop 0.10 sn donuyor; zerre de donarsa akış kesik görünür.
        // Arc da aynı gerekçeyle unscaled.
        void LateUpdate() => Step(Time.unscaledDeltaTime);

        /// <summary>
        /// ⚠️ dt DIŞARIDAN alınıyor — `Enemy.ApplyAura` ile aynı gerekçe.
        /// Editör modunda `Time.unscaledDeltaTime` 0 olduğu için hareket hiç ölçülemezdi
        /// ve öz-denetim yalnızca "zerre doğdu mu" diye ÖZELLİK sorabilirdi;
        /// "hedefe vardı mı, kendini yok etti mi" diye ETKİ soramazdı.
        /// </summary>
        public void Step(float dt)
        {
            _age += dt;
            float t = Mathf.Clamp01(_age / _life);

            if (t >= 1f) { Kill(); return; }

            if (_target != null) _fallbackTarget = _target.position;

            // ⚠️ HIZLANARAK varır (t²), yavaşlayarak değil. Yavaşlayan zerre "bırakıldı"
            // gibi okunuyor; hızlanan zerre ÇEKİLİYOR gibi. Çalınma hissi burada.
            float ease = t * t;
            var pos = Vector3.Lerp(_from, _fallbackTarget, ease);

            // Hafif kavis: düz çizgi zeminde kayıyormuş gibi duruyor, kavis "uçuyor" yapıyor.
            pos.y += Mathf.Sin(t * Mathf.PI) * _arcHeight;
            transform.position = pos;

            // Varışa doğru küçülür: emiliyor.
            transform.localScale = Vector3.one * _size * (1f - 0.55f * t);
            Apply(1f - t * t);          // son anda hızlı sön
        }

        /// <summary>`Enemy` ile aynı ayrım: editör modunda `Destroy` hata basar.</summary>
        void Kill()
        {
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        void Apply(float alpha)
        {
            if (_mr == null || _mpb == null) return;
            var c = _color;
            c.a *= Mathf.Clamp01(alpha);
            _mpb.SetColor("_BaseColor", c);
            _mpb.SetColor("_Color", c);
            _mr.SetPropertyBlock(_mpb);
        }

        /// <summary>Küre ilkelinden bir kez çalınan mesh — her zerre için yenisi üretilmez.</summary>
        static Mesh SharedMesh()
        {
            if (_sharedMesh != null) return _sharedMesh;

            // ponytail: kod ile küre mesh'i üretmek onlarca satır; Unity'nin ilkeli hazır.
            // Bootstrap da düşman/sunak için aynı yolu kullanıyor.
            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _sharedMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Destroy(temp); else DestroyImmediate(temp);
            return _sharedMesh;
        }

        /// <summary>Play oturumları arası statik kalıntı (bkz. Bootstrap.ResetStatics).</summary>
        public static void Reset() => _sharedMesh = null;
    }
}
