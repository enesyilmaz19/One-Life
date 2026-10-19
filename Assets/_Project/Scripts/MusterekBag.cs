using System.Collections.Generic;
using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Faz 32 — MÜŞTEREK'in bağı: üyelerden birine inen hasarın bir payı öbürlerine de iner — hangi
    /// silahtan gelirse gelsin (`Enemy.TakeDamage` çağırıyor). Yayılan hasar bir daha yayılmaz.
    /// Bir düşman tek bağda. ⚠️ dt DIŞARIDAN (tuzak 3) — öz-denetim `Step` ile ilerletiyor.
    /// </summary>
    public class MusterekBag : MonoBehaviour
    {
        public static readonly List<MusterekBag> Tümü = new List<MusterekBag>();

        public readonly List<Enemy> Üyeler = new List<Enemy>();
        public float Pay { get; private set; }
        public float HasatPayı { get; private set; }
        public float Kalan { get; private set; }
        public bool Bitti { get; private set; }
        /// <summary>Kaç kez bir üyeye yayıldı — otopilot raporu.</summary>
        public int Yayılma { get; private set; }
        /// <summary>ÇEKİŞME (Faz 33b): üyeler bağın ortasına bu hızla (birim/sn) sürüklenir; 0 = yok.</summary>
        public float Çekişme { get; set; }
        /// <summary>MÜTESELSİL (Faz 33b): bir üye ölünce öbürleri havuzunun %10'unu kaybeder.</summary>
        public bool Müteselsil { get; set; }
        bool _dağıtıyor;
        LineRenderer _çizgi;

        public static MusterekBag Kur(IList<Enemy> üyeler, float pay, float hasatPayı, float süre, Transform kök)
        {
            var go = new GameObject("MüşterekBağ");
            go.transform.SetParent(kök);
            var b = go.AddComponent<MusterekBag>();
            b.Pay = pay; b.HasatPayı = hasatPayı; b.Kalan = süre;
            foreach (var e in üyeler)
            {
                if (e == null || b.Üyeler.Contains(e)) continue;
                if (e.Bağ != null) e.Bağ.Çıkar(e);
                e.Bağ = b;
                b.Üyeler.Add(e);
            }
            b._çizgi = go.AddComponent<LineRenderer>();
            b._çizgi.material = UnlitMat.Additive;
            b._çizgi.widthMultiplier = 0.09f;
            b._çizgi.startColor = b._çizgi.endColor = new Color(0.95f, 0.6f, 0.9f, 0.8f);
            b._çizgi.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Tümü.Add(b);
            if (b.Üyeler.Count < 2) b.Bitir(); else b.Çiz();
            return b;
        }

        /// <summary>Kaynağa inen hasarın payını öbür üyelere indir. Yayılan hasar tekrar yayılmaz.</summary>
        public void Dağıt(Enemy kaynak, float hasar, float hasat)
        {
            if (_dağıtıyor || Bitti) return;
            _dağıtıyor = true;
            foreach (var e in Üyeler.ToArray())                       // vuruş öldürüp listeden çıkarabilir
                if (e != null && e != kaynak && Enemy.All.Contains(e)) { e.TakeDamage(hasar * Pay, hasat * HasatPayı); Yayılma++; }
            _dağıtıyor = false;
        }

        /// <summary>
        /// Bir üye öldü (`Enemy.Kill`): bağdan HEMEN çık — ölü bir daha vurulup ikinci kez ödemesin.
        /// MÜTESELSİL'de öbürleri havuzunun %10'unu kaybeder (boss: en çok ölenin havuzu kadar — yoksa
        /// boss'a bağlanan çağrılmışlar boss'u yüzde yüzde eritirdi). Kayıp bir daha YAYILMAZ.
        /// ⚠️ BOŞALMIŞ üye atlanıyor: %10'u zaten 0, ama ona vurmak onu öldürürdü — o da şu an vurulmakta
        /// olan üye olabilir (bağın yayılması onu önce öldürüyor) ve ikinci kez ölüp ikinci kez öderdi.
        /// </summary>
        public void Öldü(Enemy ölen, float ölenHavuzu)
        {
            if (Bitti) return;
            var diğer = new List<Enemy>(Üyeler);
            diğer.Remove(ölen);
            Üyeler.Remove(ölen);
            if (ölen != null && ölen.Bağ == this) ölen.Bağ = null;
            if (Müteselsil)
            {
                bool önce = _dağıtıyor;
                _dağıtıyor = true;
                foreach (var e in diğer)
                {
                    if (e == null || e.Bağ != this || e.Hollow || !Enemy.All.Contains(e)) continue;
                    float kayıp = e.TimeLeft * 0.1f;
                    if (e.IsBoss) kayıp = Mathf.Min(kayıp, ölenHavuzu);
                    e.TakeDamage(kayıp, HasatPayı);
                }
                _dağıtıyor = önce;
            }
            if (Üyeler.Count < 2) Bitir();
        }

        public void Çıkar(Enemy e)
        {
            Üyeler.Remove(e);
            if (e != null && e.Bağ == this) e.Bağ = null;
            if (Üyeler.Count < 2) Bitir();
        }

        void Update() => Step(Time.deltaTime);

        void OnDestroy() => Tümü.Remove(this);

        public void Step(float dt)
        {
            if (Bitti) return;
            Kalan -= dt;
            Üyeler.RemoveAll(e => e == null || !Enemy.All.Contains(e));
            if (Kalan <= 0f || Üyeler.Count < 2) { Bitir(); return; }
            if (Çekişme > 0f)                                         // ÇEKİŞME: ortaya sürüklen (itme değil — vuruşun itmesini ezmesin)
            {
                Vector3 o = Orta;
                foreach (var e in Üyeler)
                {
                    if (e.IsBoss) continue;                           // boss (mini dahil) kaymaz: saldırı alanları onun yerine çiziliyor
                    Vector3 d = o - e.transform.position; d.y = 0f;   // tuzak 2
                    float boy = d.magnitude - 1.2f;
                    if (boy > 0f) e.transform.position += d.normalized * Mathf.Min(Çekişme * dt, boy);
                }
            }
            Çiz();
        }

        void Çiz()
        {
            _çizgi.positionCount = Üyeler.Count;
            for (int i = 0; i < Üyeler.Count; i++) _çizgi.SetPosition(i, Üyeler[i].transform.position + Vector3.up * 0.6f);
        }

        public Vector3 Orta
        {
            get
            {
                Vector3 t = Vector3.zero;
                foreach (var e in Üyeler) t += e.transform.position;
                return Üyeler.Count > 0 ? t / Üyeler.Count : Vector3.zero;
            }
        }

        /// <summary>KOPARMA: düzlemdeki [a, b] parçası bağın bir halkasını kesiyor mu (tuzak 2: y yok).</summary>
        public bool Kesiyor(Vector3 a, Vector3 b)
        {
            for (int i = 0; i + 1 < Üyeler.Count; i++)
                if (Kesişir(a, b, Üyeler[i].transform.position, Üyeler[i + 1].transform.position)) return true;
            return false;
        }

        static bool Kesişir(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2)
        {
            float Yön(Vector3 a, Vector3 b, Vector3 c) => (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
            float d1 = Yön(q1, q2, p1), d2 = Yön(q1, q2, p2), d3 = Yön(p1, p2, q1), d4 = Yön(p1, p2, q2);
            return d1 * d2 < 0f && d3 * d4 < 0f;
        }

        public void Bitir()
        {
            if (Bitti) return;
            Bitti = true;
            Tümü.Remove(this);
            foreach (var e in Üyeler) if (e != null && e.Bağ == this) e.Bağ = null;
            Üyeler.Clear();
            // ⚠️ Destroy ERTELENİR (tuzak 1): önce kapat.
            gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }
    }
}
