using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 33a — ENDERLİK + silah başına 8 yükseltmenin ilk yarısı (Enes 2026-10-18). Yapı: enderlik tablosu,
    /// kayıt uyumu, "rastgele 3" teklif (grup, sahiplik, sabit tohum), sunak grupları ve her gruptan 1 alım,
    /// enderlikli fiyat. Etki: KONİ · FAİZLİ · HACİZ · İPOTEK · İFLAS · REHİN'in 4'er yeni yükseltmesi.
    /// </summary>
    public static class Faz33SelfCheck
    {
        const BindingFlags Gizli = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tek Can/Faz 33 öz-denetimi çalıştır (enderlik + yükseltmeler)")]
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
        static readonly MethodInfo Vur = PC("Strike"), Başla = PC("StartAttack"), Sağ = PC("StartSpecial"),
                                   DashAtak = PC("DashStrike"), DashBaşla = PC("StartDash");
        static readonly MethodInfo Gir = typeof(RoomManager).GetMethod("EnterRoom", Gizli);
        static readonly MethodInfo Al = typeof(Altar).GetMethod("Buy", Gizli);
        static readonly MethodInfo DüşmanAdımı = typeof(Enemy).GetMethod("TickState", Gizli);
        static readonly FieldInfo İtme = typeof(Enemy).GetField("_push", Gizli);
        static readonly FieldInfo DurumSayacı = typeof(Enemy).GetField("_stateTimer", Gizli);
        static readonly FieldInfo SaldırıSayacı = typeof(Enemy).GetField("_attackTimer", Gizli);
        static readonly PropertyInfo Hız = typeof(Enemy).GetProperty("MoveSpeed", Gizli);

        public static int Execute()
        {
            int failed = 0, total = 0;
            _t = ScriptableObject.CreateInstance<Tuning>();
            _t.startSeconds = 300f;
            _t.enemyTimePool = 200f;
            _t.enemyDecayPerSecond = 0f;
            Region.Reset();
            Temizle();

            _root = new GameObject("faz33-check");
            _clock = _root.AddComponent<Clock>();
            _pGo = null; _pc = null;

            failed += Yapısı(ref total);
            failed += Sunakları(ref total);
            failed += Koniyi(ref total);
            failed += Faizliyi(ref total);
            failed += Haczi(ref total);
            failed += İpoteği(ref total);
            failed += İflası(ref total);
            failed += Rehini(ref total);

            Temizle();
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_t);
            Clock.Reset(); Region.Reset(); RoomManager.Reset();
            Debug.Log($"[Faz 33 öz-denetim] {(failed == 0 ? "✅" : "❌")} {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── kurgu ──────────────────────────────────────────────────────────────
        static void YeniKoşu(float saat = 300f)
        {
            Temizle();
            SaveFile.UseInMemory(new SaveData { upgradeSeed = 12345 });
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

        static Enemy Düşman(Vector3 yer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = yer;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(_t, _pGo.transform, null);
            return e;
        }

        /// <summary>Boş (bir vuruşta ölecek) düşman — HASATSIZ boşaltılıyor: hasat etseydi saati 70 sn şişirir, "dipte" kurgusunu bozardı.</summary>
        static Enemy Boş(Vector3 yer) { var e = Düşman(yer); e.TakeDamage(e.TimeLeft, 0f); return e; }
        static Vector3 Yer(float x, float z) => Bootstrap.ArenaCenter + new Vector3(x, 0f, z);
        static Vector3 İleri(float m) => Yer(0f, m);
        static void W(int silah) => _pc.SetWeapon(silah);
        static void Silahla(int silah) { _pc.SetWeapon(silah); Vur.Invoke(_pc, null); }
        static void SağTık() => Sağ.Invoke(_pc, new object[] { Vector3.forward });
        static void Bas() => Başla.Invoke(_pc, new object[] { Vector3.forward });
        static void Dash() => DashAtak.Invoke(_pc, null);
        static object Alan(string ad) => typeof(PlayerController).GetField(ad, Gizli).GetValue(_pc);
        static void Alan(string ad, object v) => typeof(PlayerController).GetField(ad, Gizli).SetValue(_pc, v);
        static void Yükselt(SilahYükseltme y) => SilahSunağı.Al(y, -1);
        static float Hasar(Enemy e, System.Action a) { float önce = e.TimeLeft; a(); return önce - e.TimeLeft; }
        static float SaatFarkı(System.Action a) { float s0 = _clock.Seconds; a(); return _clock.Seconds - s0; }
        static float Hasat(System.Action a) { float h0 = _clock.TotalHarvested; a(); return _clock.TotalHarvested - h0; }
        static GroundMark[] İşaretler() => Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Exclude).Where(m => m != null && m.Bekliyor).ToArray();
        static void Patlat() { for (int i = 0; i < 4; i++) foreach (var m in İşaretler()) m.Step(5f); }
        static System.Collections.Generic.List<Altar> Sunaklar(SunakGrubu g) =>
            Altar.All.Where(a => a != null && a.Type == Altar.Kind.Weapon && a.Grup == g).ToList();

        /// <summary>Aynı kurguyu yükseltmesiz ve yükseltmeli koşturur.</summary>
        static (float önce, float sonra) İkiKez(SilahYükseltme y, System.Func<float> ölç, float saat = 300f)
        {
            YeniKoşu(saat); float a = ölç();
            YeniKoşu(saat); Yükselt(y); float b = ölç();
            return (a, b);
        }

        // ── 1) YAPI: enderlik tablosu · kayıt uyumu · teklif ─────────────────────
        static int Yapısı(ref int total)
        {
            int failed = 0;
            int[] ilk6 = { Kadro.KONİ, Kadro.FAİZLİ, Kadro.HACİZ, Kadro.İPOTEK, Kadro.İFLAS, Kadro.REHİN };
            int Say(int w, Enderlik e) => SilahSunağı.Yükseltmeleri(w).Count(y => SilahSunağı.Ender[(int)y] == e);
            bool dağılım = ilk6.All(w => SilahSunağı.Yükseltmeleri(w).Count == 7 && Say(w, Enderlik.Yaygın) == 3
                                         && Say(w, Enderlik.Nadir) == 2 && Say(w, Enderlik.Epik) == 1 && Say(w, Enderlik.Efsanevi) == 1);
            bool öteki = Enumerable.Range(0, Weapon.All.Length).Except(ilk6).All(w => SilahSunağı.Yükseltmeleri(w).Count == 3);
            failed += Check(ref total, "⚠️ enderlik: 33a'nın 6 silahında 7 yükseltme (3 yaygın · 2 nadir · 1 epik · 1 efsanevi; 8.'si combo açan, 33c), ötekilerde 3",
                            dağılım && öteki, 0f);

            int n = SilahSunağı.Toplam;
            bool uyum = (int)SilahYükseltme.GenişAğ == 0 && (int)SilahYükseltme.Kapanış == 14 && (int)SilahYükseltme.UzunKontrat == 35
                     && (int)SilahYükseltme.Kalabalık == 36 && SilahSunağı.Silahı(SilahYükseltme.Kalabalık) == Kadro.KONİ
                     && SilahSunağı.Silahı(SilahYükseltme.Tehdit) == Kadro.REHİN && SilahSunağı.Silahı(SilahYükseltme.UzunKontrat) == Kadro.KİRA;
            failed += Check(ref total, $"⚠️ kayıt uyumu: eski 36'nın numarası aynı, yeniler sonda ve doğru silahta ({n} yükseltme; ad {SilahSunağı.Ad.Length}, açıklama {SilahSunağı.NeYapar.Length}, enderlik {SilahSunağı.Ender.Length})",
                            uyum && SilahSunağı.Ad.Length == n && SilahSunağı.NeYapar.Length == n && SilahSunağı.Ender.Length == n, n);

            YeniKoşu();
            var mini = SilahSunağı.Teklif(Kadro.KONİ, SunakGrubu.Mini, 7);
            var üst = SilahSunağı.Teklif(Kadro.KONİ, SunakGrubu.BossÜst, 7);
            var şans = SilahSunağı.Teklif(Kadro.KONİ, SunakGrubu.Şans, 7);
            bool doğru = mini.Count == 3 && mini.All(y => SilahSunağı.Ender[(int)y] <= Enderlik.Nadir)
                      && üst.Count == 2 && üst.All(y => SilahSunağı.Ender[(int)y] >= Enderlik.Epik)
                      && şans.Count == 3 && mini.All(y => SilahSunağı.Silahı(y) == Kadro.KONİ);
            failed += Check(ref total, $"⚠️⚠️ teklif: mini yalnız yaygın/nadir ({mini.Count}), boss üst yalnız epik/efsanevi ({üst.Count}), şans her enderlikten ({şans.Count}) — hepsi kuşanılan silahın",
                            doğru, mini.Count);

            bool aynı = SilahSunağı.Teklif(Kadro.KONİ, SunakGrubu.Mini, 7).SequenceEqual(mini);
            int farklı = Enumerable.Range(0, 12).Select(k => string.Join(",", SilahSunağı.Teklif(Kadro.KONİ, SunakGrubu.Mini, k))).Distinct().Count();
            Yükselt(mini[0]);
            bool alınanYok = !SilahSunağı.Teklif(Kadro.KONİ, SunakGrubu.Mini, 7).Contains(mini[0]);
            failed += Check(ref total, $"⚠️ teklif sabit tohumlu (aynı sunakta aynı: {(aynı ? "evet" : "HAYIR")}), sunaktan sunağa değişiyor ({farklı} farklı / 12), alınan bir daha çıkmıyor",
                            aynı && farklı >= 3 && alınanYok, farklı);
            return failed;
        }

        // ── 2) SUNAKLAR: gruplar · her gruptan 1 alım · kayıtta · fiyat ────────────
        static int Sunakları(ref int total)
        {
            int failed = 0;
            (int mini, int şans) Oda(float ihtimal)
            {
                YeniKoşu();
                _t.chanceAltarProbability = ihtimal;
                var go = new GameObject("faz33-oda"); go.transform.SetParent(_root.transform);
                var rm = go.AddComponent<RoomManager>();
                rm.Init(_t, _pGo.transform, null, startRoom: RoomManager.SunakOdası(_t, 0), startRegion: 0);
                Ortaya();
                var sonuç = (Sunaklar(SunakGrubu.Mini).Count, Sunaklar(SunakGrubu.Şans).Count);
                Object.DestroyImmediate(go); RoomManager.Reset();
                return sonuç;
            }
            var her = Oda(1f); var hiç = Oda(0f);
            _t.chanceAltarProbability = 0.3f;
            failed += Check(ref total, $"⚠️ sunak odası: 3 mini sunak ({her.mini}); ŞANS sunağı ihtimale bağlı (ihtimal 1 → {her.şans}, 0 → {hiç.şans})",
                            her.mini == 3 && her.şans == 1 && hiç.mini == 3 && hiç.şans == 0, her.mini);

            // Her gruptan 1 alım; kapat-aç (yeni sunaklar) aynı grubu yeniden açmıyor; silah değişip dönünce aynı teklif
            YeniKoşu(1000f);
            _t.chanceAltarProbability = 1f;
            var odaGo = new GameObject("faz33-oda"); odaGo.transform.SetParent(_root.transform);
            var oda = odaGo.AddComponent<RoomManager>();
            oda.Init(_t, _pGo.transform, null, startRoom: RoomManager.SunakOdası(_t, 0), startRegion: 0);
            Ortaya();
            W(Kadro.KONİ);
            var m = Sunaklar(SunakGrubu.Mini); var ş = Sunaklar(SunakGrubu.Şans);
            var konide = m[0].Yükseltme();
            W(Kadro.FAİZLİ); var faizlide = m[0].Yükseltme();
            W(Kadro.KONİ); var yine = m[0].Yükseltme();
            failed += Check(ref total, $"⚠️ silah değiştirip dönünce AYNI teklif — yeni zar yok ({Ad(konide)} → {Ad(faizlide)} → {Ad(yine)})",
                            konide.HasValue && yine.HasValue && konide.Value == yine.Value && faizlide.HasValue
                            && SilahSunağı.Silahı(faizlide.Value) == Kadro.FAİZLİ, 0f);

            // Şans sunağı ×1.5 — aynı enderliğin normal fiyatına göre
            var şy = ş.Count == 1 ? ş[0].Yükseltme() : null;
            float şansFiyat = ş.Count == 1 ? ş[0].Price : 0f;
            float normal = şy.HasValue ? Clock.SilahFiyatı(_t, _clock.Seconds, SilahSunağı.Ender[(int)şy.Value]) : 0f;
            failed += Check(ref total, $"ŞANS sunağı aynı enderliğin fiyatının ×{_t.chanceAltarPriceMul} katı ({şansFiyat:0} = {normal:0} × {_t.chanceAltarPriceMul})",
                            şy.HasValue && Mathf.Abs(şansFiyat - normal * _t.chanceAltarPriceMul) < 0.01f, şansFiyat / Mathf.Max(0.001f, normal));

            var m1 = m[1].Yükseltme();
            Al.Invoke(m[0], null);
            bool ikinciKapalı = m[1].SoldOut, ilkAlındı = konide.HasValue && SilahSunağı.Var(konide.Value);
            bool şansAçık = ş.Count == 1 && !ş[0].SoldOut;
            bool etiketYerinde = m[0].Label().Contains("[ALINDI]") && m[1].Yükseltme() == m1;
            Gir.Invoke(oda, new object[] { RoomManager.SunakOdası(_t, 0) });                 // kapat-aç: yeni sunaklar
            var yeni = Sunaklar(SunakGrubu.Mini);
            bool kayıtta = yeni.Count == 3 && yeni.All(a => a.SoldOut);
            failed += Check(ref total, $"⚠️⚠️ gruptan 1 alım: biri alındı, ikincisi kapalı; şans sunağı ayrı ({(şansAçık ? "açık" : "KAPALI")}); alınan sunak yerinde [ALINDI]; yeniden girince de kapalı (kayıtta)",
                            ilkAlındı && ikinciKapalı && şansAçık && etiketYerinde && kayıtta, 0f);
            // Her mini boss kendi alımı: sonraki ara bölgenin sunağı açık
            Gir.Invoke(oda, new object[] { RoomManager.SunakOdası(_t, 1) });
            var sonraki = Sunaklar(SunakGrubu.Mini);
            failed += Check(ref total, $"⚠️ her mini boss'un sunağı ayrı sayılıyor (2. ara bölgenin sunağında {sonraki.Count(x => !x.SoldOut)} açık)",
                            sonraki.Count == 3 && sonraki.Any(x => !x.SoldOut), sonraki.Count(x => !x.SoldOut));
            Object.DestroyImmediate(odaGo); RoomManager.Reset();
            _t.chanceAltarProbability = 0.3f;

            // Boss odası: 3 alt + 3 üst
            YeniKoşu();
            var bossGo = new GameObject("faz33-boss"); bossGo.transform.SetParent(_root.transform);
            var boss = bossGo.AddComponent<RoomManager>();
            boss.Init(_t, _pGo.transform, null, startRoom: _t.RoomsInRegion, startRegion: 0);
            Ortaya();
            HepsiniÖldür();
            typeof(RoomManager).GetMethod("Update", Gizli).Invoke(boss, null);
            int alt = Sunaklar(SunakGrubu.BossAlt).Count, üst = Sunaklar(SunakGrubu.BossÜst).Count;
            bool üstEnder = Sunaklar(SunakGrubu.BossÜst).All(a => !a.Yükseltme().HasValue || SilahSunağı.Ender[(int)a.Yükseltme().Value] >= Enderlik.Epik);
            failed += Check(ref total, $"⚠️ boss odası: {alt} yaygın/nadir + {üst} epik/efsanevi sunak (üst sıra yalnız epik/efsanevi: {(üstEnder ? "evet" : "HAYIR")})",
                            alt == 3 && üst == 3 && üstEnder, alt + üst);
            Object.DestroyImmediate(bossGo); RoomManager.Reset();

            // Fiyat: enderlikle, alım sayısıyla değil; şans ×1.5
            float y = Clock.SilahFiyatı(_t, 1000f, Enderlik.Yaygın), f = Clock.SilahFiyatı(_t, 1000f, Enderlik.Efsanevi);
            YeniKoşu(1000f);
            float önce = Clock.SilahFiyatı(_t, _clock.Seconds, Enderlik.Nadir);
            for (int i = 0; i < 10; i++) Yükselt((SilahYükseltme)i);
            float sonra = Clock.SilahFiyatı(_t, _clock.Seconds, Enderlik.Nadir);
            float oran = _t.rarityPriceLegendary / _t.rarityPriceCommon;
            failed += Check(ref total, $"⚠️ fiyat enderlikle (yaygın {y:0} · efsanevi {f:0} = ×{oran:0.0}), 10 alımdan sonra aynı ({önce:0} → {sonra:0}) — kartopu yok",
                            Mathf.Abs(f / y - oran) < 0.001f && Mathf.Abs(sonra - önce) < 0.001f, f / y);
            return failed;
        }

        // ── 3) KONİ ───────────────────────────────────────────────────────────
        static int Koniyi(ref int total)
        {
            int failed = 0;
            float KalabalıkHasadı(int kaç)
            {
                for (int i = 0; i < kaç; i++) Düşman(Yer((i - (kaç - 1) * 0.5f) * 0.7f, 1.3f));
                return Hasat(() => Silahla(Kadro.KONİ)) / kaç;
            }
            var üç = İkiKez(SilahYükseltme.Kalabalık, () => KalabalıkHasadı(3));
            var iki = İkiKez(SilahYükseltme.Kalabalık, () => KalabalıkHasadı(2));
            failed += Check(ref total, $"KALABALIK (nadir): 3 düşmana değince hasat ×1.3 ({üç.önce:0.00} → {üç.sonra:0.00}), 2'de değişmiyor ({iki.önce:0.00} → {iki.sonra:0.00})",
                            Mathf.Abs(üç.sonra / üç.önce - 1.3f) < 0.01f && Mathf.Abs(iki.sonra / iki.önce - 1f) < 0.01f, üç.sonra / Mathf.Max(0.001f, üç.önce));

            var yankı = İkiKez(SilahYükseltme.Yankı, () => { var e = Düşman(İleri(1.3f)); return Hasar(e, () => Silahla(Kadro.KONİ)); });
            failed += Check(ref total, $"⚠️ YANKI (efsanevi): savurma yarım hasarla bir kez daha ({yankı.önce:0.0} → {yankı.sonra:0.0} = ×1.5)",
                            Mathf.Abs(yankı.sonra / yankı.önce - 1.5f) < 0.01f, yankı.sonra / Mathf.Max(0.001f, yankı.önce));

            var halka = İkiKez(SilahYükseltme.Halka, () => { W(Kadro.KONİ); var e = Düşman(İleri(-1.3f)); return Hasar(e, Dash); });
            failed += Check(ref total, $"HALKA (yaygın): dash-atak arkandakine de ({halka.önce:0.0} → {halka.sonra:0.0})", halka.önce == 0f && halka.sonra > 0f, halka.sonra);

            float Hazırlık(bool yor)
            {
                YeniKoşu();
                if (yor) Yükselt(SilahYükseltme.Yorgunluk);
                var e = Düşman(İleri(1f));
                Silahla(Kadro.KONİ);
                SaldırıSayacı.SetValue(e, 0f);
                DüşmanAdımı.Invoke(e, new object[] { 0.001f });
                return e.CurrentState == Enemy.State.Windup ? (float)DurumSayacı.GetValue(e) : -1f;
            }
            float h0 = Hazırlık(false), h1 = Hazırlık(true);
            failed += Check(ref total, $"⚠️ YORGUNLUK (epik): vurduğunun saldırı hazırlığı ×1.3 ({h0:0.000} → {h1:0.000} sn)",
                            h0 > 0f && Mathf.Abs(h1 / h0 - 1.3f) < 0.01f, h1 / Mathf.Max(0.001f, h0));
            return failed;
        }

        // ── 4) FAİZLİ ─────────────────────────────────────────────────────────
        static int Faizliyi(ref int total)
        {
            int failed = 0;
            float bedel = _t.faizliSwingCost;
            float Savur() { W(Kadro.FAİZLİ); return -SaatFarkı(Bas); }
            var tepede = İkiKez(SilahYükseltme.Sermaye, Savur, 1000f);
            var dipte = İkiKez(SilahYükseltme.Sermaye, Savur, 100f);
            failed += Check(ref total, $"SERMAYE (yaygın): doyma tavanının üstünde savurma bedeli yarı ({tepede.önce:0.00} → {tepede.sonra:0.00}), altında değil ({dipte.sonra:0.00})",
                            Mathf.Abs(tepede.sonra - bedel * 0.5f) < 0.001f && Mathf.Abs(tepede.önce - bedel) < 0.001f && Mathf.Abs(dipte.sonra - bedel) < 0.001f, tepede.sonra);

            var iade = İkiKez(SilahYükseltme.FaizOranı, () => { Boş(İleri(1.4f)); return SaatFarkı(() => Silahla(Kadro.FAİZLİ)); });
            failed += Check(ref total, $"FAİZ ORANI (nadir): öldürme iadesi +{iade.sonra - iade.önce:0.00} (= 0.6 × bedel)", Mathf.Abs(iade.sonra - iade.önce - 0.6f * bedel) < 0.001f, iade.sonra - iade.önce);

            var sıçra = İkiKez(SilahYükseltme.Sıçrama, () => { Boş(İleri(1.4f)); var yan = Düşman(Yer(2f, 1.6f)); return Hasar(yan, () => Silahla(Kadro.FAİZLİ)); });
            failed += Check(ref total, $"⚠️ SIÇRAMA (epik): öldürünce yanındakine sıçrıyor ({sıçra.önce:0.0} → {sıçra.sonra:0.0})", sıçra.önce == 0f && sıçra.sonra > 0f, sıçra.sonra);

            var kredi = İkiKez(SilahYükseltme.FaizsizKredi, () => { W(Kadro.FAİZLİ); Düşman(İleri(1.4f)); return SaatFarkı(() => { Bas(); Vur.Invoke(_pc, null); }); });
            failed += Check(ref total, $"⚠️ FAİZSİZ KREDİ (efsanevi): isabet eden savurma bedelsiz (+{kredi.sonra - kredi.önce:0.00} = bedel)", Mathf.Abs(kredi.sonra - kredi.önce - bedel) < 0.001f, kredi.sonra - kredi.önce);
            return failed;
        }

        // ── 5) HACİZ ──────────────────────────────────────────────────────────
        static int Haczi(ref int total)
        {
            int failed = 0;
            YeniKoşu();
            Yükselt(SilahYükseltme.İhtar);
            var e = Düşman(İleri(4f));
            float hız0 = (float)Hız.GetValue(e);
            Silahla(Kadro.HACİZ);
            float hız1 = (float)Hız.GetValue(e);
            failed += Check(ref total, $"İHTAR (yaygın): vurulan yarı hızla ({hız0:0.00} → {hız1:0.00})", e.Yavaş && Mathf.Abs(hız1 / hız0 - 0.5f) < 0.01f, hız1 / Mathf.Max(0.001f, hız0));

            float menzil = Weapon.All[Kadro.HACİZ].Range(_t);
            var uzak = İkiKez(SilahYükseltme.SonSöz, () => { var d = Düşman(İleri(menzil * 0.9f)); return Hasar(d, () => Silahla(Kadro.HACİZ)); });
            var yakın = İkiKez(SilahYükseltme.SonSöz, () => { var d = Düşman(İleri(4f)); return Hasar(d, () => Silahla(Kadro.HACİZ)); });
            failed += Check(ref total, $"SON SÖZ (nadir): menzilin son çeyreğinde ×1.4 ({uzak.önce:0.0} → {uzak.sonra:0.0}), yakında değil ({yakın.önce:0.0} → {yakın.sonra:0.0})",
                            Mathf.Abs(uzak.sonra / uzak.önce - 1.4f) < 0.01f && Mathf.Abs(yakın.sonra - yakın.önce) < 0.01f, uzak.sonra / Mathf.Max(0.001f, uzak.önce));

            float k = Weapon.All[Kadro.HACİZ].Special(_t).DamagePerDistance;
            float mesafe = 5f;
            var vade = İkiKez(SilahYükseltme.UzunVadeArtı, () => { W(Kadro.HACİZ); var d = Düşman(İleri(mesafe)); return Hasar(d, SağTık); });
            float yüzey = mesafe - Bootstrap.EnemyScale.x * 0.5f;
            float beklenen = vade.önce / (1f + yüzey * k) * (1f + yüzey * k * 1.5f);
            failed += Check(ref total, $"UZUN VADE+ (epik): özelin mesafe hasarı ×1.5 ({vade.önce:0.0} → {vade.sonra:0.0}, beklenen {beklenen:0.0})",
                            k > 0f && Mathf.Abs(vade.sonra - beklenen) < 0.05f, vade.sonra);

            YeniKoşu();
            Yükselt(SilahYükseltme.GeriÇekil);
            Düşman(İleri(4f));
            Silahla(Kadro.HACİZ);
            var geri = (Vector3)Alan("_knockback");
            failed += Check(ref total, $"⚠️ GERİ ÇEKİL (efsanevi): vurunca geri kayıyorsun (itme {geri.magnitude:0.0}, yön {(Vector3.Dot(geri.normalized, Vector3.back) > 0.99f ? "geri" : "YANLIŞ")})",
                            geri.magnitude > 5f && Vector3.Dot(geri.normalized, Vector3.back) > 0.99f, geri.magnitude);
            return failed;
        }

        // ── 6) İPOTEK ─────────────────────────────────────────────────────────
        static int İpoteği(ref int total)
        {
            int failed = 0;
            float menzil = Weapon.All[Kadro.İPOTEK].Range(_t);
            float yarıçap = Weapon.All[Kadro.İPOTEK].GroundMarkRadius(_t);

            var boş = İkiKez(SilahYükseltme.BoşArsa, () =>
            {
                W(Kadro.İPOTEK);
                Alan("_specialCd", Enumerable.Repeat(5f, Weapon.All.Length).ToArray());
                Vur.Invoke(_pc, null);
                Patlat();
                return _pc.SpecialCooldownOf(Kadro.İPOTEK);
            });
            failed += Check(ref total, $"BOŞ ARSA (yaygın): boşa patlayan işaret sağ tık beklemesini −2 sn ({boş.önce:0.0} → {boş.sonra:0.0})",
                            Mathf.Abs(boş.önce - 5f) < 0.001f && Mathf.Abs(boş.sonra - 3f) < 0.001f, boş.sonra);

            float İşaretHasarı(float uzaklık) { var e = Düşman(İleri(menzil) + Vector3.right * uzaklık); W(Kadro.İPOTEK); return Hasar(e, () => { Vur.Invoke(_pc, null); Patlat(); }); }
            var merkez = İkiKez(SilahYükseltme.DerinTemel, () => İşaretHasarı(0f));
            var kenar = İkiKez(SilahYükseltme.DerinTemel, () => İşaretHasarı(yarıçap * 0.9f));
            failed += Check(ref total, $"DERİN TEMEL (nadir): merkezde ×1.5 ({merkez.önce:0.0} → {merkez.sonra:0.0}), kenarda değil ({kenar.önce:0.0} → {kenar.sonra:0.0})",
                            Mathf.Abs(merkez.sonra / merkez.önce - 1.5f) < 0.01f && Mathf.Abs(kenar.sonra - kenar.önce) < 0.01f, merkez.sonra / Mathf.Max(0.001f, merkez.önce));

            YeniKoşu();
            Yükselt(SilahYükseltme.Çekim);
            var içte = Düşman(İleri(menzil) + Vector3.right * yarıçap * 0.7f);
            Silahla(Kadro.İPOTEK);
            foreach (var mk in İşaretler()) mk.Step(0.05f);
            var çekiş = (Vector3)İtme.GetValue(içte);
            failed += Check(ref total, $"ÇEKİM (nadir): beklerken içindekini merkeze çekiyor (yön {(Vector3.Dot(çekiş.normalized, Vector3.left) > 0.9f ? "merkeze" : "YANLIŞ")}, {çekiş.magnitude:0.0})",
                            çekiş.magnitude > 1f && Vector3.Dot(çekiş.normalized, Vector3.left) > 0.9f, çekiş.magnitude);

            YeniKoşu();
            Yükselt(SilahYükseltme.ZincirTapu);
            var hedef = Düşman(İleri(menzil));
            Silahla(Kadro.İPOTEK);
            var ilk = İşaretler().First();
            float h0 = hedef.TimeLeft;
            ilk.Step(5f);
            float birinci = h0 - hedef.TimeLeft;
            var ikinci = İşaretler().FirstOrDefault(x => x.İkincil);
            float h1 = hedef.TimeLeft;
            if (ikinci != null) ikinci.Step(5f);
            float ikinciHasar = h1 - hedef.TimeLeft;
            bool zincirlemez = İşaretler().Length == 0;
            failed += Check(ref total, $"⚠️ ZİNCİR TAPU (efsanevi): patlama vurduğunun yerine küçük ikinci işaret ({(ikinci != null ? "çıktı" : "YOK")}), o da vuruyor ({ikinciHasar:0.0} = ×0.7 {birinci:0.0}), bir daha zincirlemiyor",
                            ikinci != null && Mathf.Abs(ikinciHasar - birinci * 0.7f) < 0.05f && zincirlemez, ikinciHasar);
            return failed;
        }

        // ── 7) İFLAS ──────────────────────────────────────────────────────────
        static int İflası(ref int total)
        {
            int failed = 0;
            float Dash30(float saat) { W(Kadro.İFLAS); _clock.Init(_t, _pGo.transform, saat); DashBaşla.Invoke(_pc, new object[] { Vector3.forward }); return (float)Alan("_dashCd"); }
            var altında = İkiKez(SilahYükseltme.SonKuyruk, () => Dash30(20f));
            var üstünde = İkiKez(SilahYükseltme.SonKuyruk, () => Dash30(200f));
            failed += Check(ref total, $"SON KUYRUK (yaygın): saat 30'un altında dash beklemesi yarı ({altında.önce:0.00} → {altında.sonra:0.00}), üstünde değil ({üstünde.sonra:0.00})",
                            Mathf.Abs(altında.sonra / altında.önce - 0.5f) < 0.01f && Mathf.Abs(üstünde.sonra - üstünde.önce) < 0.001f, altında.sonra / Mathf.Max(0.001f, altında.önce));

            float dip = _t.iflasPanicSeconds;
            var cesaret = İkiKez(SilahYükseltme.Cesaret, () => { W(Kadro.İFLAS); return _pc.TakeHit(5f, İleri(2f)); }, dip);
            var güvende = İkiKez(SilahYükseltme.Cesaret, () => { W(Kadro.İFLAS); return _pc.TakeHit(5f, İleri(2f)); }, 300f);
            failed += Check(ref total, $"CESARET (nadir): dipteyken darbe −%20 ({cesaret.önce:0.0} → {cesaret.sonra:0.0}), güvendeyken değil ({güvende.sonra:0.0})",
                            Mathf.Abs(cesaret.sonra - 4f) < 0.001f && Mathf.Abs(cesaret.önce - 5f) < 0.001f && Mathf.Abs(güvende.sonra - 5f) < 0.001f, cesaret.sonra);

            var kurtar = İkiKez(SilahYükseltme.Kurtarma, () => { Boş(İleri(1.2f)); return SaatFarkı(() => Silahla(Kadro.İFLAS)); }, dip);
            failed += Check(ref total, $"⚠️ KURTARMA (epik): dipteyken öldürünce +{kurtar.sonra - kurtar.önce:0.0} sn (= 3)", Mathf.Abs(kurtar.sonra - kurtar.önce - 3f) < 0.01f, kurtar.sonra - kurtar.önce);

            var zincir = İkiKez(SilahYükseltme.İflasZinciri, () => { Boş(İleri(1.2f)); W(Kadro.İFLAS); SağTık(); return _pc.SpecialCooldownOf(Kadro.İFLAS); });
            failed += Check(ref total, $"⚠️ İFLAS ZİNCİRİ (efsanevi): TASFİYE öldürünce beklemesi sıfır ({zincir.önce:0.0} → {zincir.sonra:0.0})",
                            zincir.önce > 1f && zincir.sonra < 0.001f, zincir.sonra);
            return failed;
        }

        // ── 8) REHİN ──────────────────────────────────────────────────────────
        static Enemy Yakala() { var e = Düşman(İleri(1.5f)); Silahla(Kadro.REHİN); return e; }

        static int Rehini(ref int total)
        {
            int failed = 0;
            var sıkı = İkiKez(SilahYükseltme.SıkıTutuş, () => { var r = Yakala(); return Hasar(r, () => _pc.TakeHit(4f, İleri(3f))); });
            failed += Check(ref total, $"SIKI TUTUŞ (yaygın): kalkan rehineyi yarı yıpratıyor ({sıkı.önce:0.0} → {sıkı.sonra:0.0})", Mathf.Abs(sıkı.sonra / sıkı.önce - 0.5f) < 0.01f, sıkı.sonra / Mathf.Max(0.001f, sıkı.önce));

            var sorgu = İkiKez(SilahYükseltme.Sorgu, () => { var r = Yakala(); return Hasat(() => _pc.TickRehin(1f)); });
            failed += Check(ref total, $"SORGU (nadir): tutarken rehinenin havuzu akıyor, hasat oluyor ({sorgu.önce:0.00} → {sorgu.sonra:0.00})", sorgu.önce == 0f && sorgu.sonra > 0f, sorgu.sonra);

            var domino = İkiKez(SilahYükseltme.Domino, () =>
            {
                Yakala();
                Düşman(İleri(5f));
                var yan = Düşman(Yer(1.6f, 5f));
                return Hasar(yan, () => Silahla(Kadro.REHİN));
            });
            failed += Check(ref total, $"⚠️ DOMİNO (epik): fırlatılan rehine çarptığından yanındakine seker ({domino.önce:0.0} → {domino.sonra:0.0})", domino.önce == 0f && domino.sonra > 0f, domino.sonra);

            bool Saldırdı(bool tehdit)
            {
                YeniKoşu();
                if (tehdit) Yükselt(SilahYükseltme.Tehdit);
                Yakala();
                var ön = Düşman(Yer(0.5f, 1f));                     // önde ve saldırı menzilinde
                SaldırıSayacı.SetValue(ön, 0f);
                DüşmanAdımı.Invoke(ön, new object[] { 0.01f });
                return ön.CurrentState == Enemy.State.Windup;
            }
            bool tehditsiz = Saldırdı(false), tehditli = Saldırdı(true);
            failed += Check(ref total, $"⚠️ TEHDİT (efsanevi): rehine tutarken önündeki saldırı başlatmıyor (yükseltmesiz {(tehditsiz ? "saldırdı" : "SALDIRMADI")}, yükseltmeli {(tehditli ? "SALDIRDI" : "saldırmadı")})",
                            tehditsiz && !tehditli, 0f);
            return failed;
        }

        // ── yardımcılar ──────────────────────────────────────────────────────────
        static string Ad(SilahYükseltme? y) => y.HasValue ? SilahSunağı.Ad[(int)y.Value] : "—";

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
            foreach (var b in MusterekBag.Tümü.ToArray()) if (b != null) b.Bitir();
            MusterekBag.Tümü.Clear();
            foreach (var k in KiraKulesi.Tümü.ToArray()) if (k != null) k.Bitir();
            KiraKulesi.Tümü.Clear();
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
