using System.Collections.Generic;
using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Faz 32 — KİRA'nın kulesi: kurulu kaldıkça kira öder (saniyede saniye) ve menzilindeki en yakın
    /// düşmana ateş edip hasat eder. Soru: "bu alanı ne kadar tutayım?" — düşman yoksa kira boşa.
    /// ORTAK GİDER combo'sunda kirayı bağlı düşmanlar öder. ⚠️ dt DIŞARIDAN (tuzak 3).
    /// </summary>
    public class KiraKulesi : MonoBehaviour
    {
        public static readonly List<KiraKulesi> Tümü = new List<KiraKulesi>();

        Tuning _t;
        float _kira, _hasar, _hasat, _atışSayacı, _ortakBorç;
        MusterekBag _ortak;
        bool _aidat;

        public float Kalan { get; private set; }
        public float Yarıçap { get; private set; }
        public int Atış { get; private set; }
        public float ÖdenenKira { get; private set; }
        public bool Bitti { get; private set; }
        /// <summary>DEPOZİTO (Faz 33b): süresi dolunca SAATTEN ödenen kiranın bu payı geri (TAHLİYE'de değil).</summary>
        public float Depozito { get; set; }
        /// <summary>STOPAJ (Faz 33b): kule öldürünce bu kadar sn (doymaya tabi).</summary>
        public float Stopaj { get; set; }
        float _saattenÖdenen;
        /// <summary>TEMİNAT combo'su (Faz 33c): bu kadar sn kira yok.</summary>
        public float KirasızKalan { get; set; }
        /// <summary>KİRA KONTRATI combo'su (Faz 42): kule bu düşmana kilitli — o yaşadıkça menzil sınırı yok.</summary>
        public Enemy Kilit { get; set; }
        /// <summary>KİRACI SÜRÜSÜ (Faz 42): kule önce bunlara ateş eder.</summary>
        public readonly HashSet<Enemy> Öncelikli = new HashSet<Enemy>();
        /// <summary>İŞGAL (Faz 42): kirayı bu düşman öder (yaşadıkça) — ORTAK GİDER'in bağı gibi, tek kişi.</summary>
        public Enemy Kiracı { get; set; }
        bool _uzatıldı;
        bool OrtakÖdüyor => _ortak != null && !_ortak.Bitti;

        public static KiraKulesi Kur(Tuning t, Vector3 yer, float süre, float kira, float yarıçap,
                                     float hasar, float hasat, MusterekBag ortak, bool aidat, Transform kök)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "KiraKulesi";
            var col = go.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            go.transform.SetParent(kök);
            go.transform.position = new Vector3(yer.x, yer.y, yer.z);
            go.transform.localScale = new Vector3(0.45f, 0.7f, 0.45f);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.95f, 0.8f, 0.45f, 1f);

            var k = go.AddComponent<KiraKulesi>();
            k._t = t; k.Kalan = süre; k._kira = kira; k.Yarıçap = yarıçap;
            k._hasar = hasar; k._hasat = hasat; k._ortak = ortak; k._aidat = aidat;

            // Menzil halkası — kuleyle birlikte gidiyor (alt nesne).
            var halka = new GameObject("menzil").AddComponent<LineRenderer>();
            halka.transform.SetParent(go.transform, false);
            halka.useWorldSpace = false;
            halka.loop = true;
            halka.material = UnlitMat.Additive;
            halka.widthMultiplier = 0.06f;
            halka.startColor = halka.endColor = new Color(0.95f, 0.8f, 0.45f, 0.5f);
            halka.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            const int n = 40;
            halka.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                // ölçek telafisi: halka dünyada `yarıçap` olsun
                halka.SetPosition(i, new Vector3(Mathf.Cos(a) * yarıçap / 0.45f, -0.9f, Mathf.Sin(a) * yarıçap / 0.45f));
            }
            Tümü.Add(k);
            return k;
        }

        void Update() => Step(Time.deltaTime);

        void OnDestroy() => Tümü.Remove(this);

        public void Step(float dt)
        {
            if (Bitti) return;
            Kalan -= dt;

            // Kira: saatten — ORTAK GİDER'de bağlı düşmanın havuzundan (yarım saniyelik dilimlerle: her
            // `TakeDamage` bir isabet halkası çiziyor, kare başına çağırmak ekranı doldururdu).
            float borç = _kira * dt;
            if (KirasızKalan > 0f) { KirasızKalan -= dt; borç = 0f; }      // TEMİNAT
            ÖdenenKira += borç;
            bool kiracı = Kiracı != null && Enemy.All.Contains(Kiracı);           // İŞGAL
            if (kiracı || OrtakÖdüyor)
            {
                _ortakBorç += borç;
                Enemy öder = kiracı ? Kiracı : _ortak.Üyeler.Count > 0 ? _ortak.Üyeler[0] : null;
                if (_ortakBorç >= 0.5f && öder != null)
                {
                    öder.TakeDamage(_ortakBorç, 0f);   // ORTAK GİDER'de bağ öbürlerine de yayıyor
                    _ortakBorç = 0f;
                }
            }
            else
            {
                var saat = Clock.Instance;
                if (saat != null && !saat.Frozen) { saat.Spend(borç); _saattenÖdenen += borç; }
            }

            _atışSayacı -= dt;
            if (_atışSayacı <= 0f) { _atışSayacı = _t.kiraShotInterval; Ateş(); }
            if (Kalan <= 0f)
            {
                if (Depozito > 0f && Clock.Instance != null) Clock.Instance.Add(_saattenÖdenen * Depozito);   // süresi doldu
                Bitir();
            }
        }

        void Ateş()
        {
            Enemy hedef = Kilit != null && Enemy.All.Contains(Kilit) && !Kilit.Dormant ? Kilit : null;   // KİRA KONTRATI
            float en = float.MaxValue;
            if (hedef == null) foreach (var e in Enemy.All)
            {
                if (e == null || e.Dormant || e.Tutan != null) continue;
                Vector3 d = e.transform.position - transform.position; d.y = 0f;     // tuzak 2
                float yüzey = d.magnitude - e.Radius;
                if (yüzey > Yarıçap) continue;
                if (_aidat && OrtakÖdüyor && e.Bağ == _ortak) yüzey -= 1000f;        // AİDAT: önce bağlılar
                if (Öncelikli.Contains(e)) yüzey -= 1000f;                           // KİRACI SÜRÜSÜ
                if (yüzey < en) { en = yüzey; hedef = e; }
            }
            if (hedef == null) return;
            float hasar = _hasar * (_aidat && OrtakÖdüyor && hedef.Bağ == _ortak ? 1.5f : 1f);
            Arc.Spawn(hedef.transform.position, hedef.transform.rotation, 0.6f, 360f,
                      new Color(0.95f, 0.8f, 0.45f, 0.8f), 0.15f, scaleFrom: 0.4f, scaleTo: 1.2f);
            bool öldürür = hedef.Hollow;                                         // boşu vurmak öldürür
            hedef.TakeDamage(hasar, _hasat);
            Atış++;
            if (öldürür && Stopaj > 0f && Clock.Instance != null) Clock.Instance.Add(Stopaj * Clock.Instance.HasatDoyması);
        }

        /// <summary>
        /// TAHLİYE için ERKEN FESİH: kalan kira peşin ödenir (kirayı kim ödüyorsa o — ORTAK GİDER'de bağlılar),
        /// kule kalkar; kalan süreyi döndürür. ⚠️ Bedelsiz olsaydı "kur → hemen tahliye" her beklemede
        /// kira ödemeden tam patlama verirdi (Faz 32 spam hesabı).
        /// </summary>
        public float Fesih()
        {
            if (Bitti) return 0f;
            float kalan = Mathf.Max(0f, Kalan), bedel = kalan * _kira;
            ÖdenenKira += bedel;
            if (OrtakÖdüyor && _ortak.Üyeler.Count > 0) _ortak.Üyeler[0].TakeDamage(bedel + _ortakBorç, 0f);
            else if (Clock.Instance != null && !Clock.Instance.Frozen) Clock.Instance.Spend(bedel);
            Bitir();
            return kalan;
        }

        /// <summary>SAĞLAM YAPI (Faz 33b): kuleyi bir kez uzat (kira da o kadar sürer).</summary>
        public void Uzat(float sn)
        {
            if (_uzatıldı || Bitti) return;
            _uzatıldı = true;
            Kalan += sn;
        }

        /// <summary>KİRACI SÜRÜSÜ · İŞGAL · ZİNCİRLEME TAHSİL · SÜPÜRGE KULESİ · ZAMAN KİRASI (Faz 42): ömür uzar — SAĞLAM YAPI'nın
        /// Uzat'ı tek seferlik, bu değil.</summary>
        public void Ömür(float sn) { if (!Bitti) Kalan += sn; }

        /// <summary>KİRA GELİRİ combo'su (Faz 33c): FAİZLİ'nin bedelini kule ömründen öder.</summary>
        public void Kısalt(float sn) => Kalan -= sn;

        /// <summary>TAŞINMA: kuleyi oraya taşı (kira sürer).</summary>
        public void Taşı(Vector3 yer) => transform.position = new Vector3(yer.x, yer.y, yer.z);

        public void Bitir()
        {
            if (Bitti) return;
            Bitti = true;
            Tümü.Remove(this);
            gameObject.SetActive(false);                                  // tuzak 1
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }
    }
}
