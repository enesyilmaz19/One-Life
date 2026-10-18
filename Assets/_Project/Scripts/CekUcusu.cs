using System.Collections.Generic;
using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Faz 31 — ÇEK'in uçuşu: menzile kadar gider, arena kenarına çarparsa seker, sonra sahibine döner.
    /// Gidişte de dönüşte de vurur; her bacakta bir düşmana bir kez. Dönene kadar sahibi ÇEK'le
    /// saldıramıyor (`Sahibinin`). ⚠️ dt DIŞARIDAN (tuzak 3) — öz-denetim `Step` ile uçuruyor.
    /// </summary>
    public class CekUcusu : MonoBehaviour
    {
        public static readonly List<CekUcusu> Uçanlar = new List<CekUcusu>();
        static readonly List<Enemy> _aday = new List<Enemy>();
        const float Yarıçap = 0.45f;

        PlayerController _sahibi;
        Tuning _t;
        Vector3 _yön;
        float _kalan, _hız, _hasar, _hasat, _dönüşÇarpanı;
        int _sekme;
        bool _bitti;
        readonly HashSet<Enemy> _vurulan = new HashSet<Enemy>();

        public bool Dönüyor { get; private set; }
        public int Sekti { get; private set; }
        public int İsabet { get; private set; }

        public static int Sahibinin(PlayerController p)
        {
            int n = 0;
            foreach (var u in Uçanlar) if (u != null && u._sahibi == p) n++;
            return n;
        }

        /// <summary>CİRO: sahibinin havadaki çeklerini yakala (yok et). Kaç tane yakaladı.</summary>
        public static int Yakala(PlayerController p)
        {
            int n = 0;
            for (int i = Uçanlar.Count - 1; i >= 0; i--)
                if (Uçanlar[i] != null && Uçanlar[i]._sahibi == p) { Uçanlar[i].Bitir(); n++; }
            return n;
        }

        public static CekUcusu Fırlat(PlayerController sahibi, Tuning t, Vector3 yön, float menzil, float hız,
                                      float hasar, float hasat, int sekme, float dönüşÇarpanı, Transform kök)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Çek";
            var col = go.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            go.transform.SetParent(kök);
            go.transform.position = sahibi.transform.position + Vector3.up * 0.5f;
            go.transform.localScale = new Vector3(0.55f, 0.06f, 0.32f);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.6f, 0.85f, 0.95f, 1f);

            var u = go.AddComponent<CekUcusu>();
            yön.y = 0f;
            u._sahibi = sahibi; u._t = t;
            u._yön = yön.sqrMagnitude > 0.0001f ? yön.normalized : sahibi.transform.forward;
            u._kalan = menzil; u._hız = hız; u._hasar = hasar; u._hasat = hasat;
            u._sekme = sekme; u._dönüşÇarpanı = dönüşÇarpanı;
            Uçanlar.Add(u);
            return u;
        }

        void Update() => Step(Time.deltaTime);

        void OnDestroy() => Uçanlar.Remove(this);

        public void Step(float dt)
        {
            if (_bitti) return;
            if (_sahibi == null) { Bitir(); return; }
            transform.Rotate(Vector3.up, 900f * dt, Space.World);

            Vector3 p = transform.position;
            if (!Dönüyor)
            {
                float adım = Mathf.Min(_hız * dt, _kalan);
                p += _yön * adım;
                _kalan -= adım;

                // Arena kenarı: sekme hakkı varsa yansı (yeni bacak — vurulanlar sıfırlanıyor), yoksa dön.
                Vector3 merkez = Bootstrap.ArenaCenter;
                Vector3 d = p - merkez; d.y = 0f;                       // tuzak 2
                if (d.magnitude > _t.arenaRadius)
                {
                    Vector3 n = d.normalized;
                    p = new Vector3(merkez.x + n.x * _t.arenaRadius, p.y, merkez.z + n.z * _t.arenaRadius);
                    if (_sekme > 0)
                    {
                        _sekme--; Sekti++;
                        _yön = Vector3.Reflect(_yön, n); _yön.y = 0f; _yön.Normalize();
                        _vurulan.Clear();
                    }
                    else _kalan = 0f;
                }
                if (_kalan <= 0f) { Dönüyor = true; _vurulan.Clear(); }
            }
            else
            {
                Vector3 d = _sahibi.transform.position - p; d.y = 0f;
                float adım = _hız * dt;
                if (d.magnitude <= adım + 0.5f) { Bitir(); return; }   // sahibi yakaladı
                p += d.normalized * adım;
            }
            transform.position = p;
            Vur(p);
        }

        void Vur(Vector3 p)
        {
            // Kopya liste: vuruş öldürüp `Enemy.All`'dan çıkarabilir.
            _aday.Clear();
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dormant || _vurulan.Contains(e)) continue;
                Vector3 d = e.transform.position - p; d.y = 0f;
                if (d.magnitude - e.Radius <= Yarıçap) _aday.Add(e);
            }
            foreach (var e in _aday)
            {
                _vurulan.Add(e);
                Vector3 yer = e.transform.position;                 // ÖNCE: vuruş öldürüp yok edebilir
                e.Push(Dönüyor ? -_yön : _yön, 2f);
                e.TakeDamage(_hasar * (Dönüyor ? _dönüşÇarpanı : 1f), _hasat);
                İsabet++;
                _sahibi.Çekİsabeti(yer);
            }
        }

        void Bitir()
        {
            _bitti = true;
            Uçanlar.Remove(this);
            // ⚠️ Destroy ERTELENİR (tuzak 1): önce kapat.
            gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }
    }
}
