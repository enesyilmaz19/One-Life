using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 31 — REHİN · ÇEK · KARŞILIK (Enes 2026-10-18 "onaylıyorum başla"; dash-atak ve sağ tık dahil).
    /// Her hamle ETKİSİYLE: rehine gerçekten taşınıyor ve vuruşu yiyor mu, çek gerçekten gidip
    /// dönüp iki kez vuruyor mu, karşılanan darbe gerçekten saatten düşmeyip sonraki vuruşa biniyor mu.
    /// </summary>
    public static class Faz31SelfCheck
    {
        const BindingFlags Gizli = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tek Can/Faz 31 öz-denetimi çalıştır (REHİN · ÇEK · KARŞILIK)")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        static Tuning _t;
        static Clock _clock;
        static PlayerController _pc;
        static GameObject _pGo, _root;
        static MethodInfo PC(string ad) => typeof(PlayerController).GetMethod(ad, Gizli);
        static readonly MethodInfo Vur = PC("Strike"), Başla = PC("StartAttack"), Sağ = PC("StartSpecial"), DashAtak = PC("DashStrike");
        static readonly MethodInfo Gir = typeof(RoomManager).GetMethod("EnterRoom", Gizli);
        static readonly MethodInfo Güncelle = typeof(RoomManager).GetMethod("Update", Gizli);
        static readonly MethodInfo Al = typeof(Altar).GetMethod("Buy", Gizli);
        static readonly FieldInfo İtme = typeof(Enemy).GetField("_push", Gizli);

        public static int Execute()
        {
            int failed = 0, total = 0;
            _t = ScriptableObject.CreateInstance<Tuning>();
            _t.startSeconds = 300f;
            _t.enemyTimePool = 200f;
            _t.enemyDecayPerSecond = 0f;
            _t.harvestSoftCap = 1e6f;          // İADE ölçüsü doymaya takılmasın
            Region.Reset();
            Temizle();

            _root = new GameObject("faz31-check");
            _clock = _root.AddComponent<Clock>();
            _pGo = null; _pc = null;

            failed += Kadrosu(ref total);
            failed += Rehini(ref total);
            failed += Çeki(ref total);
            failed += Karşılığı(ref total);
            failed += Comboları(ref total);
            failed += Verisi(ref total);

            Temizle();
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_t);
            Clock.Reset(); Region.Reset(); RoomManager.Reset();
            Debug.Log($"[Faz 31 öz-denetim] {(failed == 0 ? "✅" : "❌")} {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── kurgu ──────────────────────────────────────────────────────────────
        static void YeniKoşu(float saat = 300f)
        {
            Temizle();
            SaveFile.UseInMemory(new SaveData());
            if (_pGo != null) Object.DestroyImmediate(_pGo);
            _pGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _pGo.transform.SetParent(_root.transform);
            Ortaya();
            _pc = _pGo.AddComponent<PlayerController>();
            _pc.Init(_t, null);
            _clock.Init(_t, _pGo.transform, saat);
            for (int w = 0; w < Weapon.All.Length; w++) Kadro.Aç(w, "test");
        }

        static void Ortaya()
        {
            _pGo.transform.position = Bootstrap.ArenaCenter;
            _pGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        }

        static RoomManager Oda(int oda, int bölge)
        {
            var go = new GameObject("faz31-oda"); go.transform.SetParent(_root.transform);
            var rm = go.AddComponent<RoomManager>();
            rm.Init(_t, _pGo.transform, null, startRoom: oda, startRegion: bölge);
            Ortaya();
            return rm;
        }

        static void OdayıSil(RoomManager rm) { Object.DestroyImmediate(rm.gameObject); RoomManager.Reset(); }

        static Enemy Düşman(Vector3 yer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = yer;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(_t, _pGo.transform, null);
            return e;
        }

        static Vector3 İleri(float m) => Bootstrap.ArenaCenter + Vector3.forward * m;
        static Vector3 Düz(Vector3 v) { v.y = 0f; return v; }
        static float AD => _clock.AttackDamage;
        static int W(int silah) { _pc.SetWeapon(silah); return silah; }
        static void Silahla(int silah) { _pc.SetWeapon(silah); Vur.Invoke(_pc, null); }
        static void SağTık() => Sağ.Invoke(_pc, new object[] { Vector3.forward });
        static void Dash() => DashAtak.Invoke(_pc, null);
        static void Alan(string ad, object v) => typeof(PlayerController).GetField(ad, Gizli).SetValue(_pc, v);
        static void Yükselt(SilahYükseltme y) => SilahSunağı.Al(y, 0);
        static void Artı(ComboTür c) { var s = SaveFile.Current; s.comboUpgrades = new int[Combo.Sayı]; s.comboUpgrades[(int)c] = 1; }
        static float Hasar(Enemy e, System.Action a) { float önce = e.TimeLeft; a(); return önce - e.TimeLeft; }

        /// <summary>Havadaki bütün çekleri sonuna kadar uçur. Kaç adım sürdü.</summary>
        static int Uçur(System.Func<bool> dur = null)
        {
            int n = 0;
            for (; n < 2000 && CekUcusu.Uçanlar.Count > 0; n++)
            {
                if (dur != null && dur()) break;
                foreach (var u in CekUcusu.Uçanlar.ToArray()) u.Step(0.02f);
            }
            return n;
        }

        /// <summary>REHİN'le önündekini yakala (1.5 birim).</summary>
        static Enemy Rehin()
        {
            var e = Düşman(İleri(1.5f));
            Silahla(Kadro.REHİN);
            return e;
        }

        // ── 1) KADRO: REHİN sunakta · ÇEK 2. boss'tan · KARŞILIK keşifle ────────────────
        static int Kadrosu(ref int total)
        {
            int failed = 0;
            YeniKoşu();
            var s = SaveFile.Current;
            s.unlockedWeapons = null; s.loadout = null;                      // gerçek başlangıç kadrosu
            var rm = Oda(RoomManager.SunakOdası(_t, 1), 1);
            bool erken = Sunaklar(Altar.Kind.Unlock).Any(a => a.Yuva == Kadro.REHİN);
            Gir.Invoke(rm, new object[] { RoomManager.SunakOdası(_t, 2) });
            var sunak = Sunaklar(Altar.Kind.Unlock).FirstOrDefault(a => a.Yuva == Kadro.REHİN);
            if (sunak != null) Al.Invoke(sunak, null);
            failed += Check(ref total, $"⚠️ REHİN 2. bölgenin 3. ara bölgesinin sunağında satılıyor (2.'sinde {(erken ? "VAR" : "yok")}), alınca açık",
                            !erken && sunak != null && Kadro.Açık(Kadro.REHİN), sunak != null ? 1 : 0);
            OdayıSil(rm);

            // ÇEK: 2. boss düşünce (1. bölgenin boss'u İFLAS'ı açar, ÇEK'i değil)
            YeniKoşu();
            s = SaveFile.Current; s.unlockedWeapons = null; s.loadout = null;
            rm = Oda(_t.RoomsInRegion, 1);
            HepsiniÖldür(); Güncelle.Invoke(rm, null);
            failed += Check(ref total, $"⚠️ ÇEK 2. boss düşünce açılıyor (İFLAS {(Kadro.Açık(Kadro.İFLAS) ? "da AÇILDI" : "açılmadı — o 1. boss'un")})",
                            Kadro.Açık(Kadro.ÇEK) && !Kadro.Açık(Kadro.İFLAS), Kadro.AçıkSayısı);
            OdayıSil(rm);

            // KARŞILIK: 3. bölgenin 3. ara bölgesinde, keşif odası temizlenince köşe
            YeniKoşu();
            s = SaveFile.Current; s.unlockedWeapons = null; s.loadout = null;
            rm = Oda(2 * _t.BlokBoyu + Kadro.KeşifOdası, 2);
            HepsiniÖldür();
            for (int i = 0; i < _t.roomWaves + 1; i++) { Güncelle.Invoke(rm, null); HepsiniÖldür(); }
            var köşe = Object.FindObjectsByType<GizliSilah>(FindObjectsInactive.Exclude).FirstOrDefault();
            if (köşe != null) _pGo.transform.position = köşe.transform.position;
            bool alındı = köşe != null && köşe.Silah == Kadro.KARŞILIK && köşe.Step();
            failed += Check(ref total, $"⚠️ KARŞILIK keşifle: 3. bölgenin 3. ara bölgesinde köşe {(köşe != null ? Weapon.All[köşe.Silah].Name : "YOK")}, üstüne yürüyünce açık",
                            alındı && Kadro.Açık(Kadro.KARŞILIK), köşe != null ? 1 : 0);
            OdayıSil(rm);

            // Kaçırılırsa sonraki sunaklarda (2 kat fiyatla) satılıyor
            YeniKoşu();
            s = SaveFile.Current; s.unlockedWeapons = null; s.loadout = null;
            failed += Check(ref total, "kaçırılan KARŞILIK aynı ara bölgenin sunağında ve sonra satılıyor, keşif fiyatıyla (2 kat)",
                            !Kadro.Satılıklar(2, 1).Contains(Kadro.KARŞILIK) && Kadro.Satılıklar(2, 2).Contains(Kadro.KARŞILIK)
                            && Kadro.Satılıklar(3, 0).Contains(Kadro.KARŞILIK) && Kadro.Keşifti(Kadro.KARŞILIK), 0f);
            return failed;
        }

        // ── 2) REHİN: yakala · kalkan · fırlat · TAKAS · SÜRÜKLEME · AĞIR REHİN · KALKAN ─────────
        static int Rehini(ref int total)
        {
            int failed = 0;

            YeniKoşu();
            var e = Düşman(İleri(1.5f));
            float yakalama = Hasar(e, () => Silahla(Kadro.REHİN));
            failed += Check(ref total, $"⚠️⚠️ REHİN · YAKALA: önündeki tutuluyor (rehine {(_pc.Rehine == e ? "o" : "YOK")}, yarım hasar {yakalama:0.0})",
                            _pc.Rehine == e && e.Tutan == _pc && Mathf.Abs(yakalama - AD * 0.5f) < 0.01f, yakalama);

            // Rehine oyuncunun önünde taşınıyor (Enemy.Update'in rehin dalı)
            _pGo.transform.position += Vector3.right * 3f;
            typeof(Enemy).GetMethod("Update", Gizli).Invoke(e, null);
            float önünde = Düz(e.transform.position - _pc.RehinYeri).magnitude;
            failed += Check(ref total, $"⚠️ rehine oyuncuyla taşınıyor (önündeki yerine uzaklık {önünde:0.00})", önünde < 0.01f, önünde);
            Ortaya();

            float önceE = e.TimeLeft, önceSaat = _clock.Seconds;
            float önden = _pc.TakeHit(4f, İleri(3f));
            float rehineYedi = önceE - e.TimeLeft;
            failed += Check(ref total, $"⚠️⚠️ KALKAN: önden gelen vuruşu rehine yiyor (sana {önden:0.0}, saat {önceSaat - _clock.Seconds:0.0}, rehine {rehineYedi:0.0} = 4 × {_t.rehinShieldDamagePerSecond:0})",
                            önden == 0f && Mathf.Abs(_clock.Seconds - önceSaat) < 0.001f && Mathf.Abs(rehineYedi - 4f * _t.rehinShieldDamagePerSecond) < 0.01f, rehineYedi);

            float arkadan = _pc.TakeHit(4f, İleri(-3f));
            float yandan = _pc.TakeHit(4f, Bootstrap.ArenaCenter + Vector3.right * 3f);
            Yükselt(SilahYükseltme.Kalkan);
            float yandanKalkanla = _pc.TakeHit(4f, Bootstrap.ArenaCenter + Vector3.right * 3f);
            failed += Check(ref total, $"⚠️ arkadan ve yandan gelen kalkana takılmıyor ({arkadan:0}, {yandan:0}); KALKAN (+) yandakini de tutuyor ({yandanKalkanla:0})",
                            arkadan == 4f && yandan == 4f && yandanKalkanla == 0f, yandanKalkanla);

            // FIRLAT: ikinci vuruş — yön boyunca ilk çarptığına silah hasarı × 2.2
            float Fırlatma(bool ağır)
            {
                YeniKoşu();
                if (ağır) Yükselt(SilahYükseltme.AğırRehin);
                var r = Rehin();
                var hedef = Düşman(İleri(5f));
                float h = Hasar(hedef, () => Silahla(Kadro.REHİN));
                return _pc.Rehine == null && r.Tutan == null && Düz(r.transform.position - hedef.transform.position).magnitude < 1.5f ? h : -1f;
            }
            float fırlatma = Fırlatma(false), ağırFırlatma = Fırlatma(true);
            failed += Check(ref total, $"⚠️⚠️ REHİN · FIRLAT: rehine 5 birimdekine uçuyor, ona {fırlatma:0.0} (= silah × {_t.rehinThrowDamageMul})",
                            Mathf.Abs(fırlatma - AD * _t.rehinThrowDamageMul) < 0.01f, fırlatma);
            failed += Check(ref total, $"AĞIR REHİN (+): fırlatma ×1.6 ({fırlatma:0.0} → {ağırFırlatma:0.0})",
                            fırlatma > 0f && Mathf.Abs(ağırFırlatma / fırlatma - 1.6f) < 0.01f, ağırFırlatma / Mathf.Max(0.001f, fırlatma));

            // TAKAS (sağ tık): tuttuğun en yakın ikinciyle yer değiştiriyor, yeni rehine o
            YeniKoşu();
            var eski = Rehin();
            var yeni = Düşman(Bootstrap.ArenaCenter + Vector3.right * 3f);
            Vector3 yeniYeri = yeni.transform.position;
            float eskiÖnce = eski.TimeLeft, yeniÖnce = yeni.TimeLeft;
            W(Kadro.REHİN); SağTık();
            failed += Check(ref total, $"⚠️⚠️ REHİN · TAKAS: eski rehine ikincinin yerinde, yeni rehine o, ikisi de yedi ({eskiÖnce - eski.TimeLeft:0.0} / {yeniÖnce - yeni.TimeLeft:0.0})",
                            Düz(eski.transform.position - yeniYeri).magnitude < 0.01f && _pc.Rehine == yeni && eski.Tutan == null
                            && eski.TimeLeft < eskiÖnce && yeni.TimeLeft < yeniÖnce, 0f);

            // SÜRÜKLEME (dash-atak): rehineyi yere çarp — o ×2, çevresi vuruluyor ve itiliyor
            YeniKoşu();
            var çarpan = Rehin();
            var yanında = Düşman(_pc.RehinYeri + Vector3.right * 1.2f);
            float çÖnce = çarpan.TimeLeft, yÖnce = yanında.TimeLeft;
            W(Kadro.REHİN); Dash();
            float itildi = ((Vector3)İtme.GetValue(yanında)).magnitude;
            failed += Check(ref total, $"⚠️⚠️ REHİN · SÜRÜKLEME: rehine yere çarpıyor ({çÖnce - çarpan.TimeLeft:0.0} = 2 × silah), yanındaki vuruldu ve itildi ({itildi:0.0})",
                            _pc.Rehine == null && Mathf.Abs(çÖnce - çarpan.TimeLeft - 2f * AD) < 0.01f && yanında.TimeLeft < yÖnce && itildi > 1f, itildi);

            // Boş düşman yakalanmıyor, ölüyor · silah değişince rehine bırakılıyor
            YeniKoşu();
            var boş = Düşman(İleri(1.5f));
            boş.TakeDamage(boş.TimeLeft);
            Silahla(Kadro.REHİN);
            bool öldü = !Enemy.All.Contains(boş) && _pc.Rehine == null;
            var tutulan = Rehin();
            _pc.SetWeapon(Kadro.KONİ);
            failed += Check(ref total, $"boş düşman yakalanmıyor, ölüyor ({(öldü ? "öldü" : "YAKALANDI")}); silah değişince rehine bırakılıyor ({(tutulan.Tutan == null ? "bıraktı" : "TUTUYOR")})",
                            öldü && tutulan.Tutan == null && _pc.Rehine == null, 0f);
            return failed;
        }

        // ── 3) ÇEK: gidip dönüyor · sekiyor · silahsız · KARŞILIKSIZ · CİRO · yükseltmeler ────────
        static int Çeki(ref int total)
        {
            int failed = 0;

            YeniKoşu();
            var e = Düşman(İleri(5f));
            float önce = e.TimeLeft;
            Silahla(Kadro.ÇEK);
            var u = CekUcusu.Uçanlar.FirstOrDefault();
            Başla.Invoke(_pc, new object[] { Vector3.forward });
            bool silahsız = !_pc.Attacking;
            Uçur();
            float iki = önce - e.TimeLeft;
            Başla.Invoke(_pc, new object[] { Vector3.forward });
            bool dönünce = _pc.Attacking;
            // Çek dönünce yok ediliyor: Unity'nin `!= null`'ı artık false — sayıyı C# nesnesinden oku.
            int isabet = ReferenceEquals(u, null) ? 0 : u.İsabet;
            failed += Check(ref total, $"⚠️⚠️ ÇEK: gidişte ve dönüşte vuruyor ({isabet} isabet, {iki:0.0} = 2 × {AD * 0.9f:0.0})",
                            isabet == 2 && Mathf.Abs(iki - 2f * AD * 0.9f) < 0.01f, iki);
            failed += Check(ref total, $"⚠️ ÇEK dönene kadar silahsız ({(silahsız ? "savuramadı" : "SAVURDU")}), dönünce yeniden ({(dönünce ? "savurdu" : "SAVURAMADI")})",
                            silahsız && dönünce, 0f);

            // Arena kenarından seker; ÇİFT CİRO (+) bir sekme daha
            int Sekme(bool çift, out int hak)
            {
                YeniKoşu();
                if (çift) Yükselt(SilahYükseltme.ÇiftCiro);
                _pGo.transform.position = İleri(_t.arenaRadius - 3f);
                Silahla(Kadro.ÇEK);
                var ç = CekUcusu.Uçanlar.First();
                hak = (int)typeof(CekUcusu).GetField("_sekme", Gizli).GetValue(ç);
                Uçur();
                return ç.Sekti;
            }
            int sekti = Sekme(false, out int hak1);
            Sekme(true, out int hak2);
            failed += Check(ref total, $"⚠️ ÇEK arena kenarından sekiyor ({sekti} kez); ÇİFT CİRO (+) bir hak daha ({hak1} → {hak2})",
                            sekti == 1 && hak1 == _t.cekBounces && hak2 == _t.cekBounces + 1, sekti);

            // DÖNÜŞ KESİNTİSİ (+): dönüşte ×1.5 · HIZLI TAHSİL (+): ×1.4 hızlı
            (float gidiş, float dönüş, int adım) Bacaklar(SilahYükseltme? y)
            {
                YeniKoşu();
                if (y.HasValue) Yükselt(y.Value);
                var d = Düşman(İleri(5f));
                float b = d.TimeLeft;
                Silahla(Kadro.ÇEK);
                var ç = CekUcusu.Uçanlar.First();
                int a = Uçur(() => ç.İsabet >= 1);
                float g = b - d.TimeLeft;
                a += Uçur();
                return (g, b - d.TimeLeft - g, a);
            }
            var düz = Bacaklar(null);
            var kesinti = Bacaklar(SilahYükseltme.DönüşKesintisi);
            var hızlı = Bacaklar(SilahYükseltme.HızlıTahsil);
            failed += Check(ref total, $"DÖNÜŞ KESİNTİSİ (+): dönüş hasarı ×1.5 ({düz.dönüş:0.0} → {kesinti.dönüş:0.0}, gidiş {kesinti.gidiş:0.0} aynı)",
                            Mathf.Abs(kesinti.dönüş / düz.dönüş - 1.5f) < 0.01f && Mathf.Abs(kesinti.gidiş - düz.gidiş) < 0.01f, kesinti.dönüş / Mathf.Max(0.001f, düz.dönüş));
            failed += Check(ref total, $"HIZLI TAHSİL (+): uçuş ×1.4 kısa ({düz.adım} → {hızlı.adım} adım)",
                            Mathf.Abs((float)düz.adım / hızlı.adım - 1.4f) < 0.06f, (float)düz.adım / Mathf.Max(1, hızlı.adım));

            // KARŞILIKSIZ (sağ tık): üç çek yelpaze
            YeniKoşu();
            W(Kadro.ÇEK); SağTık();
            var üç = CekUcusu.Uçanlar.ToArray();
            foreach (var ç in üç) for (int i = 0; i < 20; i++) ç.Step(0.02f);
            var xler = üç.Select(ç => ç.transform.position.x).OrderBy(x => x).ToArray();
            failed += Check(ref total, $"⚠️⚠️ ÇEK · KARŞILIKSIZ: üç çek yelpaze gibi ({üç.Length} çek, yanlara {(xler.Length == 3 ? $"{xler[0]:0.0} / {xler[1]:0.0} / {xler[2]:0.0}" : "—")})",
                            üç.Length == 3 && xler[0] < -0.5f && Mathf.Abs(xler[1]) < 0.05f && xler[2] > 0.5f, üç.Length);

            // CİRO (dash-atak): havadakini yakalayıp dash yönüne yeniden fırlatıyor
            YeniKoşu();
            Silahla(Kadro.ÇEK);
            var ilk = CekUcusu.Uçanlar.First();
            for (int i = 0; i < 10; i++) ilk.Step(0.02f);
            Alan("_dashDir", Vector3.right);
            Dash();
            var ikinci = CekUcusu.Uçanlar.FirstOrDefault();
            if (ikinci != null) for (int i = 0; i < 10; i++) ikinci.Step(0.02f);
            failed += Check(ref total, $"⚠️⚠️ ÇEK · CİRO: havadaki yakalandı, dash yönüne yenisi ({CekUcusu.Uçanlar.Count} çek, sağa {(ikinci != null ? ikinci.transform.position.x : 0f):0.0})",
                            CekUcusu.Uçanlar.Count == 1 && ikinci != null && ikinci != ilk && ikinci.transform.position.x > 1f, CekUcusu.Uçanlar.Count);
            return failed;
        }

        // ── 4) KARŞILIK: pencere · biriken · İTİRAZ · KARŞI DAVA · yükseltmeler ────────────────
        static int Karşılığı(ref int total)
        {
            int failed = 0;

            YeniKoşu();
            W(Kadro.KARŞILIK);
            Başla.Invoke(_pc, new object[] { Vector3.forward });
            float önceSaat = _clock.Seconds;
            float pencerede = _pc.TakeHit(5f, İleri(2f));
            failed += Check(ref total, $"⚠️⚠️ KARŞILIK: pencerede gelen darbe saatten düşmüyor, birikiyor (sana {pencerede:0}, saat {önceSaat - _clock.Seconds:0.0}, biriken {_pc.Biriken:0.0})",
                            pencerede == 0f && Mathf.Abs(_clock.Seconds - önceSaat) < 0.001f && Mathf.Abs(_pc.Biriken - 5f) < 0.001f, _pc.Biriken);

            var e = Düşman(İleri(1.5f));
            float salıver = Hasar(e, () => Vur.Invoke(_pc, null));
            float biriktiKaldı = _pc.Biriken;
            Alan("_attackHitDone", true);
            float dışarıda = _pc.TakeHit(5f, İleri(2f));
            YeniKoşu();
            W(Kadro.KONİ);
            Başla.Invoke(_pc, new object[] { Vector3.forward });
            float koniyle = _pc.TakeHit(5f, İleri(2f));
            failed += Check(ref total, $"⚠️ pencere bitince ({dışarıda:0}) ve başka silahla ({koniyle:0}) karşılama YOK",
                            dışarıda == 5f && koniyle == 5f, dışarıda + koniyle);

            // İstismar: silah değiştirmek saldırıyı iptal ediyor — "tıkla → 2 → 1 → tıkla" pencereyi yeniden açmamalı
            YeniKoşu();
            W(Kadro.KARŞILIK);
            Başla.Invoke(_pc, new object[] { Vector3.forward });
            W(Kadro.KONİ); W(Kadro.KARŞILIK);
            Başla.Invoke(_pc, new object[] { Vector3.forward });
            bool yenidenAçtı = _pc.Attacking;
            failed += Check(ref total, $"⚠️ silah değiştirip dönmek KARŞILIK'ın toparlanmasını silmiyor (yeni pencere {(yenidenAçtı ? "AÇILDI" : "açılmadı")})",
                            !yenidenAçtı, yenidenAçtı ? 1 : 0);

            YeniKoşu();
            var z = Düşman(İleri(1.5f));
            float zayıf = Hasar(z, () => Silahla(Kadro.KARŞILIK));
            failed += Check(ref total, $"⚠️⚠️ biriken sonraki vuruşa iniyor: {salıver:0.0} = silah + 5 × {_t.karsilikDamagePerSecond} (kimse vurmadıysa zayıf: {zayıf:0.0} = 0.6 × silah), sonra sıfır ({biriktiKaldı:0})",
                            Mathf.Abs(salıver - (AD + 5f * _t.karsilikDamagePerSecond)) < 0.01f && Mathf.Abs(zayıf - 0.6f * AD) < 0.01f && biriktiKaldı == 0f, salıver);

            // TAM KARŞILIK (+) biriken ×1.5 · İADE (+) darbenin yarısı saate · GENİŞ PENCERE (+) ×1.6
            YeniKoşu();
            Yükselt(SilahYükseltme.TamKarşılık); Yükselt(SilahYükseltme.İade); Yükselt(SilahYükseltme.GenişPencere);
            W(Kadro.KARŞILIK);
            Başla.Invoke(_pc, new object[] { Vector3.forward });
            float süre = (float)typeof(PlayerController).GetField("_attackTimer", Gizli).GetValue(_pc);
            önceSaat = _clock.Seconds;
            _pc.TakeHit(4f, İleri(2f));
            failed += Check(ref total, $"TAM KARŞILIK (+): 4 sn darbe → biriken ×1.5 ({_pc.Biriken:0.0})",
                            Mathf.Abs(_pc.Biriken - 6f) < 0.001f, _pc.Biriken);
            failed += Check(ref total, $"İADE (+): karşılanan darbenin yarısı saate (+{_clock.Seconds - önceSaat:0.0})",
                            Mathf.Abs(_clock.Seconds - önceSaat - 2f) < 0.001f, _clock.Seconds - önceSaat);
            failed += Check(ref total, $"GENİŞ PENCERE (+): savurma ×1.6 ({süre:0.00} sn) — pencere de",
                            Mathf.Abs(süre - Weapon.All[Kadro.KARŞILIK].Duration(_t) * 1.6f) < 0.001f, süre);

            // İTİRAZ (sağ tık): uzun karşılama; bitince biriken hepsi önündekilere (geniş koni)
            YeniKoşu();
            W(Kadro.KARŞILIK); SağTık();
            float sp1 = _pc.TakeHit(3f, İleri(2f)), sp2 = _pc.TakeHit(3f, İleri(-2f));
            var önde = Düşman(İleri(3f));
            float erken = Hasar(önde, () => _pc.Tickİtiraz(_t.karsilikItirazSeconds * 0.5f));
            float itiraz = Hasar(önde, () => _pc.Tickİtiraz(_t.karsilikItirazSeconds));
            failed += Check(ref total, $"⚠️⚠️ KARŞILIK · İTİRAZ: {_t.karsilikItirazSeconds:0.0} sn her yönden karşılıyor ({sp1:0}, {sp2:0}); bitince 3 birimdekine {itiraz:0.0} = silah + 6 × {_t.karsilikDamagePerSecond} (erken {erken:0})",
                            sp1 == 0f && sp2 == 0f && erken == 0f && Mathf.Abs(itiraz - (AD + 6f * _t.karsilikDamagePerSecond)) < 0.01f && _pc.Biriken == 0f, itiraz);

            // KARŞI DAVA (dash-atak): birikenin İKİ katı
            float DashHasarı(float biriken)
            {
                YeniKoşu();
                W(Kadro.KARŞILIK);
                Alan("_biriken", biriken);
                var d = Düşman(İleri(1.5f));
                return Hasar(d, Dash);
            }
            float dashDüz = DashHasarı(0f), karşıDava = DashHasarı(4f);
            failed += Check(ref total, $"⚠️⚠️ KARŞILIK · KARŞI DAVA: biriken 4 → dash-atak +{karşıDava - dashDüz:0.0} (= 2 × 4 × {_t.karsilikDamagePerSecond}), sonra sıfır",
                            Mathf.Abs(karşıDava - dashDüz - 2f * 4f * _t.karsilikDamagePerSecond) < 0.01f && _pc.Biriken == 0f, karşıDava - dashDüz);
            return failed;
        }

        // ── 5) COMBO: KARŞI İCRA · SENET HACZİ · SİLİP SÜPÜR ve "+" hâlleri ─────────────────────
        static int Comboları(ref int total)
        {
            int failed = 0;

            // KARŞI İCRA: KARŞILIK'la vur → FAİZLİ ×2; İCRA EMRİ (+) İCRA'nın beklemesi sıfır
            float FaizliHasarı(bool combo, bool artı)
            {
                YeniKoşu();
                if (artı) Artı(ComboTür.KarşıİCRA);
                Alan("_specialCd", Enumerable.Repeat(5f, Weapon.All.Length).ToArray());
                var d = Düşman(İleri(1.4f));
                if (combo) { Silahla(Kadro.KARŞILIK); _pc.TickCombo(0.2f); }
                return Hasar(d, () => Silahla(Kadro.FAİZLİ));
            }
            float f0 = FaizliHasarı(false, false), f1 = FaizliHasarı(true, false);
            int sayı = _pc.ComboSayısı;
            FaizliHasarı(true, true);
            failed += Check(ref total, $"⚠️⚠️ KARŞI İCRA: KARŞILIK → FAİZLİ ×2 ({f0:0.0} → {f1:0.0}); İCRA EMRİ (+) İCRA beklemesi {_pc.SpecialCooldownOf(Kadro.FAİZLİ):0.0}",
                            sayı == 1 && Mathf.Abs(f1 / f0 - 2f) < 0.01f && _pc.SpecialCooldownOf(Kadro.FAİZLİ) < 0.01f, f1 / Mathf.Max(0.001f, f0));

            // SENET HACZİ: ÇEK isabet etti → HACİZ ×2 (gerçek çek: uçuşta değdi, o an HACİZ'e geç)
            float haciz = Weapon.All[Kadro.HACİZ].Range(_t) * 0.8f;
            (float hedef, float yan) Senet(bool combo, bool artı)
            {
                YeniKoşu();
                if (artı) Artı(ComboTür.SenetHaczi);
                var d = Düşman(İleri(haciz));
                var komşu = Düşman(İleri(haciz) + Vector3.right * 1.6f);
                if (combo)
                {
                    Silahla(Kadro.ÇEK);
                    var ç = CekUcusu.Uçanlar.First();
                    Uçur(() => ç.İsabet >= 1);
                    foreach (var x in CekUcusu.Uçanlar.ToArray()) Object.DestroyImmediate(x.gameObject);
                }
                float b = d.TimeLeft, k = komşu.TimeLeft;
                Silahla(Kadro.HACİZ);
                return (b - d.TimeLeft, k - komşu.TimeLeft);
            }
            var s0 = Senet(false, false); var s1 = Senet(true, false);
            sayı = _pc.ComboSayısı;
            var s2 = Senet(true, true);
            failed += Check(ref total, $"⚠️⚠️ SENET HACZİ: ÇEK değdi → HACİZ ×2 ({s0.hedef:0.0} → {s1.hedef:0.0}); SENET ZİNCİRİ (+) yanındakine de ({s1.yan:0.0} → {s2.yan:0.0})",
                            sayı == 1 && Mathf.Abs(s1.hedef / s0.hedef - 2f) < 0.01f && s1.yan == 0f && s2.yan > 0f, s1.hedef / Mathf.Max(0.001f, s0.hedef));

            // SİLİP SÜPÜR: REHİN'le vur (fırlat) → KONİ ×1.5, sert iter; TOZ DUMAN (+) 360°
            (float hasar, float itme, float arka) Süpür(bool combo, bool artı, bool yalnızYakala = false)
            {
                YeniKoşu();
                if (artı) Artı(ComboTür.SilipSüpür);
                if (combo)
                {
                    Rehin();                                                     // yakala
                    if (!yalnızYakala)
                    {
                        Düşman(İleri(5f));
                        Silahla(Kadro.REHİN);                                    // fırlat: combo adımı
                    }
                    HepsiniÖldür();
                    _pc.TickCombo(0.2f);
                }
                var ön = Düşman(İleri(1.2f));
                var arka = Düşman(İleri(-1.2f));
                float b = ön.TimeLeft, a = arka.TimeLeft;
                Silahla(Kadro.KONİ);
                return (b - ön.TimeLeft, ((Vector3)İtme.GetValue(ön)).magnitude, a - arka.TimeLeft);
            }
            var p0 = Süpür(false, false);
            var p1 = Süpür(true, false);
            sayı = _pc.ComboSayısı;
            var yakaladı = Süpür(true, false, yalnızYakala: true);
            int yakalamaSayısı = _pc.ComboSayısı;
            var p2 = Süpür(true, true);
            failed += Check(ref total, $"⚠️⚠️ SİLİP SÜPÜR: REHİN fırlat → KONİ ×1.5 ({p0.hasar:0.0} → {p1.hasar:0.0}), itme {p0.itme:0.0} → {p1.itme:0.0}; yakalamak adım değil ({yakaladı.hasar:0.0}, combo {yakalamaSayısı})",
                            sayı == 1 && Mathf.Abs(p1.hasar / p0.hasar - 1.5f) < 0.01f && p1.itme >= 8f && yakalamaSayısı == 0
                            && Mathf.Abs(yakaladı.hasar - p0.hasar) < 0.01f, p1.hasar / Mathf.Max(0.001f, p0.hasar));
            failed += Check(ref total, $"⚠️ TOZ DUMAN (+): KONİ arkandakini de süpürüyor (arka {p1.arka:0.0} → {p2.arka:0.0})",
                            p1.arka == 0f && p2.arka > 0f, p2.arka);
            return failed;
        }

        // ── 6) VERİ: HUD metinleri · yükseltme tablosu · combo "+" ────────────────────────────
        static int Verisi(ref int total)
        {
            int failed = 0;
            int[] yeniler = { Kadro.REHİN, Kadro.ÇEK, Kadro.KARŞILIK };
            int eksik = yeniler.Count(w => string.IsNullOrEmpty(Weapon.All[w].Special(_t).NeYapar) || Weapon.All[w].DashAtakAdı == null);
            var kategoriler = yeniler.Select(w => Weapon.All[w].Category(_t)).Distinct().Count();
            failed += Check(ref total, $"yeni silahların sağ tığı ve dash-atağı HUD'da anlatılıyor ({eksik} eksik), kategorileri ayrı ({kategoriler}/3)",
                            eksik == 0 && kategoriler == 3, eksik);
            failed += Check(ref total, $"yükseltme tablosu tam: {SilahSunağı.Toplam} yükseltme (ad {SilahSunağı.Ad.Length}, açıklama {SilahSunağı.NeYapar.Length}); yeni üç silahın 3'er yükseltmesi",
                            new[] { Kadro.REHİN, Kadro.ÇEK, Kadro.KARŞILIK }.All(w => SilahSunağı.Yükseltmeleri(w).Count >= 3)
                            && SilahSunağı.Ad.Length == SilahSunağı.Toplam
                            && SilahSunağı.NeYapar.Length == SilahSunağı.Toplam
                            && System.Enum.GetValues(typeof(SilahYükseltme)).Length == SilahSunağı.Toplam, SilahSunağı.Ad.Length);
            failed += Check(ref total, $"her combo'nun \"+\" hâli var — açan yükseltmeyle açılan Faz 33c combo'ları hariç, onlarda yok ({Combo.Sayı} combo)",
                            Combo.Tümü.All(c => c.Açan.HasValue != !string.IsNullOrEmpty(c.ArtıAd)) && System.Enum.GetValues(typeof(ComboTür)).Length == Combo.Sayı, Combo.Sayı);
            return failed;
        }

        // ── yardımcılar ──────────────────────────────────────────────────────────
        static System.Collections.Generic.List<Altar> Sunaklar(Altar.Kind k) => Altar.All.Where(a => a != null && a.Type == k).ToList();

        static void HepsiniÖldür()
        {
            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
        }

        static void Temizle()
        {
            HepsiniÖldür();
            foreach (var u in CekUcusu.Uçanlar.ToArray()) if (u != null) Object.DestroyImmediate(u.gameObject);
            CekUcusu.Uçanlar.Clear();
            foreach (var m in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include)) Object.DestroyImmediate(m.gameObject);
            foreach (var g in Object.FindObjectsByType<GizliSilah>(FindObjectsInactive.Include)) Object.DestroyImmediate(g.gameObject);
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include)) Object.DestroyImmediate(a.gameObject);
            foreach (var a in Altar.All.ToArray()) if (a != null) Object.DestroyImmediate(a.gameObject);
            Altar.ResetAll();
            GroundMark.ResetLiens();
        }

        static int Check(ref int total, string name, bool ok, float actual)
        {
            total++;
            if (ok) Debug.Log($"  ✅ {name}");
            else Debug.LogError($"  ❌ {name}  (gerçek: {actual})");
            return ok ? 0 : 1;
        }
    }
}
