using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TekCan
{
    /// <summary>
    /// Zeminde duran yassı koni. İki iş görür:
    ///   • oyuncunun savurma yayı  (vurduğunu GÖRMEK için — isabet olmasa da çıkar)
    ///   • düşmanın hazırlık işareti (vuruşun nereye ineceğini önceden göstermek için)
    ///
    /// ⚠️ Yay, gerçek vuruş alanının kendisidir: menzil ve açı doğrudan Tuning'den
    /// geliyor. Yani ayarı çevirdiğinde gördüğün şey tam olarak isabet eden şey.
    /// Görsel yalan söylemezse ayar dürüst olur.
    ///
    /// Kural tam olarak şu: **gövdesi yayla örtüşen düşman vurulur.** Düşman bir nokta
    /// değil; `PlayerController.InHitCone` hem menzili hem açıyı YÜZEYE ölçüyor.
    /// Bir ara açı MERKEZE ölçülüyordu: boss'un gövdesi yayın içinde apaçık dururken
    /// vuruş düşüyordu — yay doğru çiziliyor, isabet ona uymuyordu (bkz. `BossHitRepro`).
    /// </summary>
    public class Arc : MonoBehaviour
    {
        // Yay geometrisi yalnızca menzil+açıya bağlı ve bunlar Tuning'den geliyor,
        // yani pratikte iki üç çeşit var. Her savurmada yeni Mesh üretmek boşuna çöp.
        static readonly Dictionary<long, Mesh> _meshCache = new Dictionary<long, Mesh>();

        Transform _follow;
        MeshRenderer _mr;
        MaterialPropertyBlock _mpb;      // her karede yenisini kurmak GC baskısı yapıyordu
        Color _color;
        float _life, _age;

        /// <summary>
        /// Çizildiği yarıçap ve renk — mesh'e gömülü oldukları için dışarıdan
        /// okunamıyordu.
        ///
        /// ⚠️ Öz-denetimlerin GÖRSELİ ölçebilmesi için açık. Bu projede "mekanik
        /// doğru, görsel yalan" hatası ÜÇ KEZ çıktı (sessiz ses, yanlış tarafa
        /// çizilen güvenli dilim, görünmeyen ölü bölge). Görseli test edememek
        /// o hatanın dördüncüsünü davet ediyordu.
        /// </summary>
        public float Radius { get; private set; }
        public Color Color => _color;
        float _scaleFrom = 1f, _scaleTo = 1f;
        float _followYaw;

        /// <param name="scaleFrom">Ömrün başındaki ölçek. <paramref name="scaleTo"/> ile
        /// birlikte yayı BÜYÜTÜR: isabet patlaması dışa açılır, hazırlık işareti vuruş
        /// anına doğru dolar. Sabit yay "ne zaman" sorusunu cevaplamıyordu, büyüyen yay
        /// cevaplıyor.</param>
        /// <param name="followYaw">
        /// Takip edilen nesnenin dönüşüne eklenen sapma (derece).
        ///
        /// ⚠️ Bu olmadan takip eden yaylara verilen açı SESSİZCE SİLİNİYORDU:
        /// `LateUpdate` her karede `_follow.rotation`'ı üzerine yazıyor. Boss'un
        /// güvenli açıklığı 180° çevrilmiş olarak doğuyor ama bir kare sonra ÖNE
        /// dönüyordu — oyuncuya "buradasın, güvendesin" deyip vuruyordu.
        /// Görselin mekanikle çelişmesi bu projede en pahalı hata türü.
        /// </param>
        /// <param name="scaled">
        /// OYUN zamanıyla ilerle (hit-stop'ta dur). ⚠️ Bir ZAMANLAYICIYI gösteren her
        /// yay için şart: düşman hazırlığı ve yer işareti `Time.deltaTime` ile sayıyor,
        /// hit-stop'ta donuyor. Yay gerçek zamanla dolmaya devam ederse "yay dolunca
        /// darbe iner" sözü tam oyuncu vururken bozuluyordu — yay bitiyor, darbe
        /// ekranda hiçbir şey yokken iniyordu (denetim 2026-09-23). Süs yaylar
        /// (isabet halkası, savurma) varsayılanda kalır: hit-stop'ta da sönsünler.
        /// </param>
        public static Arc Spawn(Vector3 pos, Quaternion rot, float radius, float angleDeg,
                                Color color, float life, Transform follow = null,
                                float scaleFrom = 1f, float scaleTo = 1f, float followYaw = 0f,
                                bool scaled = false)
        {
            var go = new GameObject("Arc");
            go.transform.SetPositionAndRotation(pos + Vector3.up * 0.06f, rot);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = BuildFan(radius, angleDeg);

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = UnlitMat.Alpha;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var arc = go.AddComponent<Arc>();
            arc._mr = mr;
            arc._mpb = new MaterialPropertyBlock();
            arc._color = color;
            arc.Radius = radius;
            arc._life = Mathf.Max(0.01f, life);
            arc._follow = follow;
            arc._scaleFrom = scaleFrom;
            arc._scaleTo = scaleTo;
            arc._followYaw = followYaw;
            arc._scaled = scaled;
            go.transform.localScale = Vector3.one * scaleFrom;
            arc.Apply(1f);
            return arc;
        }

        bool _scaled;

        // unscaled: hit-stop sırasında da sönsün, yoksa donan karede takılı kalır.
        // Zamanlayıcı gösteren yaylar hariç (`scaled`) — onlar gösterdikleri şeyle donar.
        void LateUpdate() => Step(_scaled ? Time.deltaTime : Time.unscaledDeltaTime);

        /// <summary>
        /// ⚠️ dt DIŞARIDAN alınıyor — `Enemy.ApplyAura` ile aynı gerekçe. Editör modunda
        /// `Time.unscaledDeltaTime` 0; yay gerçekten büyüyor mu, ömrü bitince gidiyor mu
        /// diye ETKİ sorulamazdı, yalnızca "başlangıç ölçeği doğru mu" diye ÖZELLİK.
        /// </summary>
        public void Step(float dt)
        {
            _age += dt;

            if (_follow != null)
                transform.SetPositionAndRotation(_follow.position + Vector3.up * 0.06f,
                                                 _follow.rotation * Quaternion.Euler(0f, _followYaw, 0f));

            float k = 1f - Mathf.Clamp01(_age / _life);
            if (k <= 0f) { Kill(); return; }

            // Ölçek `follow`'dan SONRA yazılır: SetPositionAndRotation ölçeğe dokunmuyor,
            // ama sıralamayı bozarsak takip eden yaylar ölçeksiz kalır.
            if (_scaleFrom != _scaleTo)
                transform.localScale = Vector3.one * Mathf.Lerp(_scaleFrom, _scaleTo, 1f - k);

            Apply(k);
        }

        /// <summary>Hazırlık işaretini erken kaldırmak için (düşman ölürse yay ortada kalmasın).</summary>
        public void Cancel() { if (this != null) Kill(); }

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
            c.a *= alpha;
            _mpb.SetColor("_BaseColor", c);
            _mpb.SetColor("_Color", c);     // Sprites/Default yedeği için
            _mr.SetPropertyBlock(_mpb);
        }

        /// <summary>Merkezden açılan yelpaze — yerel +Z ekseninde, XZ düzleminde yatık. Önbellekli.</summary>
        static Mesh BuildFan(float radius, float angleDeg)
        {
            long key = ((long)Mathf.RoundToInt(radius * 100f) << 32) ^ (uint)Mathf.RoundToInt(angleDeg * 100f);
            if (_meshCache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int segments = 24;
            var verts = new Vector3[segments + 2];
            var tris = new int[segments * 3];

            verts[0] = Vector3.zero;
            float half = angleDeg * 0.5f;
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(-half, half, i / (float)segments) * Mathf.Deg2Rad;
                verts[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius;
            }
            for (int i = 0; i < segments; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }

            var m = new Mesh { name = "ArcFan" };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            _meshCache[key] = m;
            return m;
        }

        /// <summary>Play oturumları arası statik kalıntıyı temizler (bkz. Bootstrap.ResetStatics).</summary>
        public static void Reset() => _meshCache.Clear();
    }
}
