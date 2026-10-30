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

        // Faz 33b — fırlatan ÇEK'in yükseltmeleri, fırlatma anında yazılıyor (`PlayerController.ÇekFırlat`).
        /// <summary>TAHSİLAT: dönüşte vurduğu her düşman için saate bu kadar (doymaya tabi).</summary>
        public float Tahsilat { get; set; }
        /// <summary>YÖNELİM: sekince yansımak yerine en yakın düşmana döner.</summary>
        public bool Yönelim { get; set; }
        /// <summary>PROTESTO: dönüşte vurduklarını sahibinin dibine çeker; yakalanınca çevresine bir kez iner.</summary>
        public bool Protesto { get; set; }
        /// <summary>MUKABELE + TEDBİR (Faz 33c): vurduğu düşman 1 sn yeni saldırı başlatamaz.</summary>
        public bool Susturur { get; set; }
        /// <summary>FAİZLİ SENET combo'su (Faz 42): her isabette saate bu kadar geri (FAİZLİ'nin savurma bedeli).</summary>
        public float İade { get; set; }
        /// <summary>KARŞILIKSIZ+: havadayken sahibini silahsız bırakmaz (`Sahibinin` saymıyor).</summary>
        public bool Serbest { get; set; }
        /// <summary>TOPLU TAHSİLAT (Faz 42 Parti 4): sırayla bunlara yönelir — hepsine uğrayana dek menzil bitmez.</summary>
        public readonly List<Enemy> Rota = new List<Enemy>();
        /// <summary>SENET ORTAKLIĞI (Faz 42 Parti 4): bağlıya çarpınca bağ tam (%100) yayar.</summary>
        public bool TamPay { get; set; }
        /// <summary>KARŞILIKLI ÇEK (Faz 42 Parti 4): her isabet sahibinin birikenine bu kadar sn yazar.</summary>
        public float Biriktirir { get; set; }

        /// <summary>VADELİ ÇEK · İFLAS SENEDİ (Faz 42 Parti 4): fırlatıldıktan sonra hasar ×çarpan, fazladan sekme.</summary>
        public void Güçlendir(float çarpan, int ekSekme) { _hasar *= çarpan; _sekme += ekSekme; }

        /// <summary>KARŞILIKLI ÇEK: sahibinin havadaki bütün çekleri birikene yazar.</summary>
        public static void Biriktir(PlayerController p, float sn)
        {
            foreach (var u in Uçanlar) if (u != null && u._sahibi == p) u.Biriktirir = sn;
        }

        public bool Dönüyor { get; private set; }
        public int Sekti { get; private set; }
        public int İsabet { get; private set; }

        public static int Sahibinin(PlayerController p)
        {
            int n = 0;
            foreach (var u in Uçanlar) if (u != null && u._sahibi == p && !u.Serbest) n++;
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

        /// <summary>Sahibinin havadaki bütün çekleri (KARŞILIKSIZ'ın serbestleri dahil).</summary>
        public static int Havadaki(PlayerController p)
        {
            int n = 0;
            foreach (var u in Uçanlar) if (u != null && u._sahibi == p) n++;
            return n;
        }

        /// <summary>İBRAZ combo'su (Faz 42): sahibinin havadaki bütün çekleri hedefe döner, her biri ona bir kez (×çarpan), sonra biter.</summary>
        public static int İbraz(PlayerController p, Enemy hedef, float çarpan)
        {
            int n = 0;
            for (int i = Uçanlar.Count - 1; i >= 0; i--)
            {
                var u = Uçanlar[i];
                if (u == null || u._sahibi != p) continue;
                if (hedef != null && Enemy.All.Contains(hedef))
                {
                    Arc.Spawn(hedef.transform.position, Quaternion.identity, 0.9f, 360f, new Color(0.6f, 0.85f, 0.95f, 0.8f), 0.2f, scaleFrom: 0.4f, scaleTo: 1.2f);
                    hedef.TakeDamage(u._hasar * çarpan, u._hasat);
                    n++;
                }
                u.Bitir();
            }
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
                while (Rota.Count > 0 && (Rota[0] == null || !Enemy.All.Contains(Rota[0]) || _vurulan.Contains(Rota[0]))) Rota.RemoveAt(0);
                if (Rota.Count > 0)                                     // TOPLU TAHSİLAT: sıradakine yönel
                {
                    _yön = Rota[0].transform.position - p; _yön.y = 0f;
                    if (_yön.sqrMagnitude > 0.0001f) _yön.Normalize();
                    _kalan = Mathf.Max(_kalan, _hız * dt);
                }
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
                        if (Yönelim && EnYakın(p) is Enemy hedef)            // YÖNELİM
                        { _yön = hedef.transform.position - p; _yön.y = 0f; _yön.Normalize(); }
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
                if (d.magnitude <= adım + 0.5f) { if (Protesto) Yakalandı(); Bitir(); return; }   // sahibi yakaladı
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
                if (Dönüyor && Protesto)                            // PROTESTO: sahibinin dibine (1.5 birim) — itme kuvvet/11 birim gidiyor
                {
                    Vector3 sahibe = _sahibi.transform.position - yer; sahibe.y = 0f;
                    e.Push(sahibe, Mathf.Max(0f, sahibe.magnitude - 1.5f) * 11f);
                }
                else e.Push(Dönüyor ? -_yön : _yön, 2f);
                if (Susturur) e.Sustur(1f);                         // TEDBİR — vuruştan ÖNCE (öldürebilir)
                bool tam = TamPay && e.Bağ != null;                 // SENET ORTAKLIĞI
                if (tam) MusterekBag.TamPay = true;
                e.TakeDamage(_hasar * (Dönüyor ? _dönüşÇarpanı : 1f), _hasat);
                if (tam) MusterekBag.TamPay = false;
                İsabet++;
                if (Biriktirir > 0f) _sahibi.Biriktir(Biriktirir);  // KARŞILIKLI ÇEK
                if (Dönüyor && Tahsilat > 0f && Clock.Instance != null) Clock.Instance.Add(Tahsilat * Clock.Instance.HasatDoyması);
                if (İade > 0f && Clock.Instance != null) Clock.Instance.Add(İade);   // FAİZLİ SENET
                _sahibi.Çekİsabeti(yer, e);
            }
        }

        static Enemy EnYakın(Vector3 p)
        {
            Enemy en = null; float enYakın = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dormant) continue;
                Vector3 d = e.transform.position - p; d.y = 0f;
                if (d.magnitude < enYakın) { enYakın = d.magnitude; en = e; }
            }
            return en;
        }

        /// <summary>PROTESTO: çek sahibine dönünce çevresine (2.5 birim) dönüş hasarının iki katıyla bir kez iner.</summary>
        void Yakalandı()
        {
            Vector3 o = _sahibi.transform.position;
            _aday.Clear();
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dormant) continue;
                Vector3 d = e.transform.position - o; d.y = 0f;
                if (d.magnitude - e.Radius <= 2.5f) _aday.Add(e);
            }
            foreach (var e in _aday)
            {
                Vector3 yer = e.transform.position;
                e.TakeDamage(_hasar * _dönüşÇarpanı * 2f, _hasat);
                İsabet++;
                _sahibi.Çekİsabeti(yer, e);
            }
            Arc.Spawn(o, Quaternion.identity, 2.5f, 360f, new Color(0.6f, 0.85f, 0.95f, 0.7f), 0.3f, scaleFrom: 0.4f, scaleTo: 1f);
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
