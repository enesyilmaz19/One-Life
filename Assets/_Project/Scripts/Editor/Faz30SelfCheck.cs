using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 30 — SİLAH KADROSU (Enes 2026-10-16/17): 2 silahla başla, ilk boss'a kadar 4, İFLAS
    /// boss'tan; 3'lü envanter + KADRO sunağı; keşif; hızlı geçişle COMBO (2'li, 3'lü), istismara
    /// kapalı, "+" ile gelişen. Hepsi ETKİ: sunak gerçekten satıyor mu, köşe gerçekten silah veriyor
    /// mu, combo gerçekten işareti patlatıyor / yerini değiştiriyor / hasarı artırıyor mu.
    /// </summary>
    public static class Faz30SelfCheck
    {
        const BindingFlags Özel = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tek Can/Faz 30 öz-denetimi çalıştır (silah kadrosu)")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        static Tuning _t;
        static Clock _clock;
        static PlayerController _pc;
        static GameObject _pGo, _root;
        static readonly MethodInfo Vur = typeof(PlayerController).GetMethod("Strike", Özel);
        static readonly MethodInfo Gir = typeof(RoomManager).GetMethod("EnterRoom", Özel);
        static readonly MethodInfo Güncelle = typeof(RoomManager).GetMethod("Update", Özel);
        static readonly MethodInfo Al = typeof(Altar).GetMethod("Buy", Özel);

        public static int Execute()
        {
            int failed = 0, total = 0;
            _t = ScriptableObject.CreateInstance<Tuning>();
            _t.startSeconds = 300f;
            _t.enemyTimePool = 200f;
            _t.enemyDecayPerSecond = 0f;
            Region.Reset();
            Temizle();

            _root = new GameObject("faz30-check");
            _clock = _root.AddComponent<Clock>();
            _pGo = null; _pc = null;

            failed += Kadrosu(ref total);
            failed += Combolar(ref total);
            failed += ComboArtıları(ref total);

            Temizle();
            Object.DestroyImmediate(_root);
            Clock.Reset(); Region.Reset(); RoomManager.Reset();
            Debug.Log($"[Faz 30 öz-denetim] {(failed == 0 ? "✅" : "❌")} {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        static void YeniKoşu(float saat = 300f)
        {
            Temizle();
            SaveFile.UseInMemory(new SaveData());
            // Oyuncu her koşuda YENİDEN kuruluyor (dash izi bileşeni bir kez eklenebiliyor).
            if (_pGo != null) Object.DestroyImmediate(_pGo);
            _pGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _pGo.transform.SetParent(_root.transform);
            _pGo.transform.position = Bootstrap.ArenaCenter;
            _pGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            _pc = _pGo.AddComponent<PlayerController>();
            _pc.Init(_t, null);
            _clock.Init(_t, _pGo.transform, saat);
        }

        static RoomManager Oda(int oda, int bölge = 0)
        {
            var go = new GameObject("faz30-oda"); go.transform.SetParent(_root.transform);
            var rm = go.AddComponent<RoomManager>();
            rm.Init(_t, _pGo.transform, null, startRoom: oda, startRegion: bölge);
            // Oda kurulumu oyuncuyu girişe taşıyor — ölçümler ortada, ileri bakarak
            _pGo.transform.position = Bootstrap.ArenaCenter;
            _pGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            return rm;
        }

        static void HepsiniÖldür()
        {
            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
        }

        static List<Altar> Sunaklar(Altar.Kind k) => Altar.All.Where(a => a != null && a.Type == k).ToList();

        // ── 1) KADRO: başlangıç · sunakta satın · keşif · kaçırılan keşif · boss · KADRO sunağı ──
        static int Kadrosu(ref int total)
        {
            int failed = 0;
            YeniKoşu();
            failed += Check(ref total, $"⚠️⚠️ oyun 2 silahla başlıyor ({Kadro.AçıkSayısı} açık: {Ad(Kadro.Yuvadaki(0))}, {Ad(Kadro.Yuvadaki(1))}, 3. yuva {(Kadro.Yuvadaki(2) < 0 ? "boş" : Ad(Kadro.Yuvadaki(2)))})",
                            Kadro.AçıkSayısı == 2 && Kadro.Yuvadaki(0) == Kadro.KONİ && Kadro.Yuvadaki(1) == Kadro.FAİZLİ
                            && Kadro.Yuvadaki(2) < 0 && _pc.Weapon == Weapon.All[Kadro.KONİ], Kadro.AçıkSayısı);

            // HACİZ: 1. ara bölgenin sunağında yok, 2.'sinde var; alınca 3. yuvaya giriyor
            var rm = Oda(RoomManager.SunakOdası(_t, 0));
            bool ilkte = Sunaklar(Altar.Kind.Unlock).Any(a => a.Yuva == Kadro.HACİZ);
            Gir.Invoke(rm, new object[] { RoomManager.SunakOdası(_t, 1) });
            var hacizSunağı = Sunaklar(Altar.Kind.Unlock).FirstOrDefault(a => a.Yuva == Kadro.HACİZ);
            float s0 = _clock.Seconds;
            if (hacizSunağı != null) Al.Invoke(hacizSunağı, null);
            failed += Check(ref total, $"⚠️ HACİZ 2. ara bölgenin sunağında satılıyor (1.'de {(ilkte ? "VAR" : "yok")}), alınca 3. yuvada ({s0 - _clock.Seconds:0} sn)",
                            !ilkte && hacizSunağı != null && Kadro.Açık(Kadro.HACİZ) && Kadro.Yuvadaki(2) == Kadro.HACİZ, s0 - _clock.Seconds);

            // İPOTEK: 4. ara bölgenin keşif odası temizlenince kenarda köşe; üstüne yürüyünce açılıyor
            int keşifOdası = 3 * _t.BlokBoyu + Kadro.KeşifOdası;
            Gir.Invoke(rm, new object[] { keşifOdası });
            bool öncedenYok = Object.FindObjectsByType<GizliSilah>(FindObjectsInactive.Exclude).Length == 0;
            HepsiniÖldür();
            for (int i = 0; i < _t.roomWaves + 1; i++) { Güncelle.Invoke(rm, null); HepsiniÖldür(); }
            var köşe = Object.FindObjectsByType<GizliSilah>(FindObjectsInactive.Exclude).FirstOrDefault();
            bool uzaktan = köşe != null && köşe.Step();                     // oyuncu ortada: almamalı
            if (köşe != null) _pGo.transform.position = köşe.transform.position;
            bool alındı = köşe != null && köşe.Step();
            failed += Check(ref total, $"⚠️⚠️ İPOTEK keşifle: oda temizlenince köşe çıkıyor, uzaktan alınmıyor, üstüne yürüyünce açılıyor ({Kadro.AçıkSayısı} silah — ilk boss'tan önce 4)",
                            öncedenYok && köşe != null && !uzaktan && alındı && Kadro.Açık(Kadro.İPOTEK) && Kadro.AçıkSayısı == 4
                            && Kadro.Yuvası(Kadro.İPOTEK) < 0, Kadro.AçıkSayısı);
            _pGo.transform.position = Bootstrap.ArenaCenter;

            // KADRO sunağı: kuşanılan yuvanın silahı envanterde olmayan açık silahla değişiyor
            Gir.Invoke(rm, new object[] { RoomManager.SunakOdası(_t, 3) });
            _pc.SetWeapon(Kadro.FAİZLİ);
            var kadro = Sunaklar(Altar.Kind.Loadout).FirstOrDefault();
            if (kadro != null) Al.Invoke(kadro, null);
            failed += Check(ref total, $"⚠️ KADRO sunağı: 2. yuvadaki FAİZLİ yerine {Ad(Kadro.Yuvadaki(1))}, kuşanılan {_pc.Weapon.Name}",
                            kadro != null && Kadro.Yuvadaki(1) == Kadro.İPOTEK && _pc.Weapon == Weapon.All[Kadro.İPOTEK]
                            && Kadro.Yuvası(Kadro.FAİZLİ) < 0, Kadro.Yuvadaki(1));

            // Kaçırılan keşif: İPOTEK bulunmadıysa 4. ara bölgenin sunağında İKİ KAT fiyatla
            Object.DestroyImmediate(rm.gameObject); RoomManager.Reset();   // yeni oyuncu → yeni oda yöneticisi
            YeniKoşu();
            Kadro.Aç(Kadro.HACİZ, "test");
            rm = Oda(RoomManager.SunakOdası(_t, 2));
            bool erken = Sunaklar(Altar.Kind.Unlock).Any(a => a.Yuva == Kadro.İPOTEK);
            Gir.Invoke(rm, new object[] { RoomManager.SunakOdası(_t, 3) });
            var kaçan = Sunaklar(Altar.Kind.Unlock).FirstOrDefault(a => a.Yuva == Kadro.İPOTEK);
            float normal = Clock.SilahAçmaFiyatı(_t, _clock.Seconds, false);
            failed += Check(ref total, $"⚠️ kaçırılan keşif kendi ara bölgesinin sunağında 2 kat fiyatla satılıyor ({(kaçan != null ? kaçan.Price : 0):0} = 2 × {normal:0}; önceki sunakta {(erken ? "VAR" : "yok")})",
                            !erken && kaçan != null && Mathf.Abs(kaçan.Price - 2f * normal) < 0.01f, kaçan != null ? kaçan.Price : 0f);

            // İFLAS: 1. boss'tan
            Gir.Invoke(rm, new object[] { _t.RoomsInRegion });
            HepsiniÖldür();
            Güncelle.Invoke(rm, null);
            failed += Check(ref total, "⚠️ İFLAS 1. boss düşünce açılıyor", Kadro.Açık(Kadro.İFLAS), Kadro.AçıkSayısı);

            Temizle();
            Object.DestroyImmediate(rm.gameObject);
            RoomManager.Reset();
            return failed;
        }

        // ── 2) COMBO'lar — dördü de etkisiyle; pencere, bekleme, isabet şartı ──────────────
        static void HepsiAçık()
        {
            for (int w = 0; w < Weapon.All.Length; w++) Kadro.Aç(w, "test");
        }

        static Enemy Düşman(Vector3 yer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = yer;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(_t, _pGo.transform, null);
            return e;
        }

        static void Silahla(int silah) { _pc.SetWeapon(silah); Vur.Invoke(_pc, null); }

        static Vector3 İleri(float m) => Bootstrap.ArenaCenter + Vector3.forward * m;
        static float İpotekMenzili => Weapon.All[Kadro.İPOTEK].Range(_t);
        static readonly FieldInfo Süre = typeof(GroundMark).GetField("_timer", Özel);

        static GroundMark[] İşaretler() =>
            Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include).Where(m => m != null && m.Bekliyor).ToArray();

        static int Combolar(ref int total)
        {
            int failed = 0;

            // HACİZ KESİNLEŞTİ: İPOTEK koy → HACİZ içindekine vur → işaret HEMEN patlar
            bool Kesinleşti(float ara)
            {
                YeniKoşu(); HepsiAçık();
                var e = Düşman(İleri(İpotekMenzili));
                Silahla(Kadro.İPOTEK);
                var m = İşaretler().FirstOrDefault();
                _pc.TickCombo(ara);
                Silahla(Kadro.HACİZ);
                return m != null && m.Fired;
            }
            bool k1 = Kesinleşti(0.3f), k1Geç = Kesinleşti(_t.comboWindow + 0.2f);
            failed += Check(ref total, $"⚠️⚠️ HACİZ KESİNLEŞTİ: işaretin içindekine HACİZ → işaret HEMEN patlıyor (pencere dışında {(k1Geç ? "PATLADI" : "patlamadı")})",
                            k1 && !k1Geç, k1 ? 1 : 0);

            // TOPLU İPOTEK: KONİ'yle vur → İPOTEK işareti vurulanların ortasına, hızlı
            float koni = Weapon.All[Kadro.KONİ].Range(_t);
            (GroundMark m, Vector3 orta) Toplu(bool vursun, float ara)
            {
                YeniKoşu(); HepsiAçık();
                var a = Düşman(İleri(koni * 0.5f) + Vector3.right * 0.5f);
                var b = Düşman(İleri(koni * 0.5f) - Vector3.right * 0.5f);
                Vector3 orta = (a.transform.position + b.transform.position) * 0.5f;
                if (!vursun) { _pGo.transform.rotation = Quaternion.LookRotation(Vector3.back); }   // ıska
                Silahla(Kadro.KONİ);
                _pGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                _pc.TickCombo(ara);
                Silahla(Kadro.İPOTEK);
                return (İşaretler().FirstOrDefault(), orta);
            }
            var (tm, tOrta) = Toplu(true, 0.2f);
            float temelGecikme = Weapon.All[Kadro.İPOTEK].GroundMarkDelay(_t);
            bool yerinde = tm != null && Düz(tm.transform.position - tOrta).magnitude < 0.05f;
            bool hızlı = tm != null && Mathf.Abs((float)Süre.GetValue(tm) / temelGecikme - 0.4f) < 0.01f;
            failed += Check(ref total, $"⚠️⚠️ TOPLU İPOTEK: KONİ → İPOTEK işareti vurulanların ORTASINA, gecikme ×0.4 ({(yerinde ? "ortada" : "NİŞANDA")}, {(hızlı ? "hızlı" : "normal")})",
                            yerinde && hızlı, 0f);
            Toplu(false, 0.2f);
            failed += Check(ref total, $"⚠️ ISKA combo başlatmıyor (KONİ boşa savurdu → İPOTEK: combo sayısı {_pc.ComboSayısı})",
                            _pc.ComboSayısı == 0, _pc.ComboSayısı);

            // Bekleme: aynı combo 4 sn içinde tekrar etmiyor
            Toplu(true, 0.2f);
            int sayım = _pc.ComboSayısı;
            foreach (var x in İşaretler()) Object.DestroyImmediate(x.gameObject);
            GroundMark.ResetLiens();
            var c = Düşman(İleri(koni * 0.5f));
            _pc.TickCombo(0.5f);
            Silahla(Kadro.KONİ); _pc.TickCombo(0.2f); Silahla(Kadro.İPOTEK);
            failed += Check(ref total, $"⚠️ aynı combo {_t.comboCooldown:0} sn içinde TEKRAR ETMİYOR (combo sayısı {sayım} → {_pc.ComboSayısı})",
                            _pc.ComboSayısı == sayım, _pc.ComboSayısı - sayım);

            // TEMERRÜT: FAİZLİ'yle vur → İFLAS saatin yarıdaymış gibi sert
            float İflasHasarı(bool combo)
            {
                YeniKoşu(_t.iflasSafeSeconds * 1.6f); HepsiAçık();
                var e = Düşman(İleri(1.4f));
                if (combo) { Silahla(Kadro.FAİZLİ); _pc.TickCombo(0.2f); }
                float önce = e.TimeLeft;
                Silahla(Kadro.İFLAS);
                return önce - e.TimeLeft;
            }
            float düz = İflasHasarı(false), temerrüt = İflasHasarı(true);
            failed += Check(ref total, $"⚠️⚠️ TEMERRÜT: FAİZLİ → İFLAS daha sert ({düz:0.0} → {temerrüt:0.0})", temerrüt > düz * 1.2f, temerrüt / Mathf.Max(0.001f, düz));

            // HACİZ KARARI (3'lü): KONİ → HACİZ → İPOTEK — 1. bölgede YOK, 2. bölgede var
            float haciz = Weapon.All[Kadro.HACİZ].Range(_t);
            // Ölçüm oda silinmeden ALINIYOR — işaret odanın köküne bağlı, oda gidince o da gidiyor.
            (bool var, float alan, float hedefe) Karar(int bölge)
            {
                YeniKoşu(); HepsiAçık();
                var rm = Oda(1, bölge); HepsiniÖldür();
                var yakın = Düşman(İleri(koni * 0.5f));
                var uzak = Düşman(İleri(haciz * 0.8f));
                Vector3 hedef = uzak.transform.position;
                Silahla(Kadro.KONİ); _pc.TickCombo(0.2f);
                Silahla(Kadro.HACİZ); _pc.TickCombo(0.2f);
                Silahla(Kadro.İPOTEK);
                var m = İşaretler().FirstOrDefault();
                var sonuç = m == null ? (false, 0f, 0f) : (true, m.Yarıçap, Düz(m.transform.position - hedef).magnitude);
                Object.DestroyImmediate(rm.gameObject); RoomManager.Reset();
                return sonuç;
            }
            var k0 = Karar(0);
            var k2 = Karar(1);
            float temelAlan = Weapon.All[Kadro.İPOTEK].GroundMarkRadius(_t);
            bool ikinciBölgede = k2.var && k2.hedefe < 0.05f && Mathf.Abs(k2.alan / temelAlan - 2f) < 0.01f;
            bool birincideYok = k0.var && Mathf.Abs(k0.alan / temelAlan - 1f) < 0.01f;
            failed += Check(ref total, $"⚠️⚠️ 3'lü HACİZ KARARI: KONİ → HACİZ → İPOTEK = HACİZ'in hedefine ×2 alan (2. bölgede {(ikinciBölgede ? "VAR" : "YOK")}, 1. bölgede {(birincideYok ? "yok" : "VAR")}: {(!k0.var ? "işaret yok" : $"alan ×{k0.alan / temelAlan:0.00}")})",
                            ikinciBölgede && birincideYok, k2.alan / temelAlan);
            return failed;
        }

        // ── 3) COMBO "+" — her biri YENİ bir etki; combo sunağı boss sonrası, bir alım ──────────
        static void Artı(ComboTür c) { var s = SaveFile.Current; s.comboUpgrades = new int[Combo.Sayı]; s.comboUpgrades[(int)c] = 1; }

        static int ComboArtıları(ref int total)
        {
            int failed = 0;
            float koni = Weapon.All[Kadro.KONİ].Range(_t);

            // KESİN HÜKÜM: HACİZ'in sağ tık beklemesi sıfırlanıyor
            YeniKoşu(); HepsiAçık(); Artı(ComboTür.HacizKesinleşti);
            typeof(PlayerController).GetField("_specialCd", Özel).SetValue(_pc, Enumerable.Repeat(5f, Weapon.All.Length).ToArray());
            Düşman(İleri(İpotekMenzili));
            Silahla(Kadro.İPOTEK); _pc.TickCombo(0.3f); Silahla(Kadro.HACİZ);
            failed += Check(ref total, $"⚠️ KESİN HÜKÜM (+): HACİZ'in sağ tık beklemesi sıfır ({_pc.SpecialCooldownOf(Kadro.HACİZ):0.0} sn)",
                            _pc.SpecialCooldownOf(Kadro.HACİZ) < 0.01f, _pc.SpecialCooldownOf(Kadro.HACİZ));

            // ÇİFT HACİZ: ikinci ipotek en uzaktaki düşmana
            YeniKoşu(); HepsiAçık(); Artı(ComboTür.Toplu);
            Düşman(İleri(koni * 0.5f));
            var uzak = Düşman(İleri(İpotekMenzili * 1.3f));
            Silahla(Kadro.KONİ); _pc.TickCombo(0.2f); Silahla(Kadro.İPOTEK);
            var işaretler = İşaretler();
            failed += Check(ref total, $"⚠️ ÇİFT HACİZ (+): ikinci ipotek en uzaktaki düşmanın üstüne ({işaretler.Length} işaret)",
                            işaretler.Length == 2 && işaretler.Any(m => Düz(m.transform.position - uzak.transform.position).magnitude < 0.05f), işaretler.Length);

            // TEMERRÜT FAİZİ: o vuruşun hasadı ×2
            float Hasadı(bool artı)
            {
                YeniKoşu(_t.iflasSafeSeconds * 1.6f); HepsiAçık();
                if (artı) Artı(ComboTür.Temerrüt);
                Düşman(İleri(1.4f));
                Silahla(Kadro.FAİZLİ); _pc.TickCombo(0.2f);
                float önce = _clock.TotalHarvested; Silahla(Kadro.İFLAS);
                return _clock.TotalHarvested - önce;
            }
            float h0 = Hasadı(false), h1 = Hasadı(true);
            failed += Check(ref total, $"⚠️ TEMERRÜT FAİZİ (+): o vuruşun hasadı ×2 ({h0:0.00} → {h1:0.00})", h0 > 0f && Mathf.Abs(h1 / h0 - 2f) < 0.05f, h1 / Mathf.Max(0.001f, h0));

            // TAHLİYE: HACİZ KARARI patlaması düşmanları dışa itiyor
            YeniKoşu(); HepsiAçık(); Artı(ComboTür.HacizKararı);
            var rm = Oda(1, 1); HepsiniÖldür();
            float haciz = Weapon.All[Kadro.HACİZ].Range(_t);
            Düşman(İleri(koni * 0.5f));
            var hedef = Düşman(İleri(haciz * 0.8f));
            Silahla(Kadro.KONİ); _pc.TickCombo(0.2f); Silahla(Kadro.HACİZ); _pc.TickCombo(0.2f); Silahla(Kadro.İPOTEK);
            var karar = İşaretler().FirstOrDefault();
            var komşu = Düşman(hedef.transform.position + Vector3.right * 1f);
            if (karar != null) karar.HemenPatlat(1f);
            var itme = typeof(Enemy).GetField("_push", Özel);
            float itildi = ((Vector3)itme.GetValue(komşu)).magnitude;
            failed += Check(ref total, $"⚠️ TAHLİYE (+): patlama içindekileri dışa itiyor (itme {itildi:0.0})", itildi > 1f, itildi);
            Object.DestroyImmediate(rm.gameObject); RoomManager.Reset();

            // Combo sunağı: boss sonrası, kullanılabilir combo'nun "+" hâli, boss başına bir alım
            YeniKoşu(1000f); HepsiAçık();
            var bossOdası = Oda(_t.RoomsInRegion);
            HepsiniÖldür();
            Güncelle.Invoke(bossOdası, null);
            var sunak = Sunaklar(Altar.Kind.ComboPlus).FirstOrDefault();
            if (sunak != null) Al.Invoke(sunak, null);
            bool ikinciKapalı = sunak != null && sunak.SoldOut;
            failed += Check(ref total, $"⚠️ boss sonrası COMBO sunağı: {(sunak != null ? Combo.Bu((ComboTür)sunak.Yuva).ArtıAd : "YOK")} alındı, sonra kapalı",
                            sunak != null && Combo.Artı((ComboTür)sunak.Yuva) && ikinciKapalı, sunak != null ? 1 : 0);
            // Kapat-aç ile boss odasına dönüp yeniden kesmek ikinci combo'yu açmıyor (boss başına bir)
            Gir.Invoke(bossOdası, new object[] { _t.RoomsInRegion });
            HepsiniÖldür();
            Güncelle.Invoke(bossOdası, null);
            var yeniden = Sunaklar(Altar.Kind.ComboPlus).FirstOrDefault();
            failed += Check(ref total, $"⚠️ aynı boss'tan ikinci combo alınamıyor (yeniden kesince sunak {(yeniden == null ? "yok" : yeniden.SoldOut ? "kapalı" : "AÇIK")})",
                            yeniden == null || yeniden.SoldOut, 0f);
            Object.DestroyImmediate(bossOdası.gameObject); RoomManager.Reset();
            return failed;
        }

        // ── yardımcılar ──────────────────────────────────────────────────────────
        static string Ad(int silah) => silah >= 0 && silah < Weapon.All.Length ? Weapon.All[silah].Name : "—";
        static Vector3 Düz(Vector3 v) { v.y = 0f; return v; }

        static void Temizle()
        {
            HepsiniÖldür();
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
