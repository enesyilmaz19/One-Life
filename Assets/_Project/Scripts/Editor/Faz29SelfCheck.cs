using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 29 — İPOTEK YAĞMURU · HACİZ İHBARI dash-atakta · İCRA yeniden (Enes, eski Faz 20 listesi).
    /// Hepsi ETKİ: işaret gerçekten düşmanın üstüne düşüyor ve patlayınca vuruyor mu, bedel gerçekten
    /// saatin yüzdesi mi, hasar ödenen bedelle gerçekten artıyor mu, iade tavanın üstünde kesiliyor mu.
    /// </summary>
    public static class Faz29SelfCheck
    {
        const BindingFlags Özel = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tek Can/Faz 29 öz-denetimi çalıştır (silah özelleri)")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;
            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 200f;
            t.enemyTimePool = 200f;          // tek vuruşta ölmesin, hasar ölçülebilsin
            t.enemyDecayPerSecond = 0f;
            Region.Reset();
            SaveFile.UseInMemory(new SaveData());
            Temizle();

            var root = new GameObject("faz29-check");
            var clock = root.AddComponent<Clock>();
            var pGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pGo.transform.SetParent(root.transform);
            pGo.transform.position = Bootstrap.ArenaCenter;
            pGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var pc = pGo.AddComponent<PlayerController>();
            pc.Init(t, null);
            clock.Init(t, pGo.transform, t.startSeconds);
            var özel = typeof(PlayerController).GetMethod("StartSpecial", Özel);
            void Sağ() => özel.Invoke(pc, new object[] { Vector3.forward });
            var ipotek = System.Array.Find(Weapon.All, w => w is Ipotek);

            // ── 1) İPOTEK YAĞMURU: alandaki düşmanların ÜSTÜNE peş peşe ipotek, alan dışına değil ──
            pc.SetWeapon(Index<Ipotek>());
            var sp = ipotek.Special(t);
            float alan = ipotek.Range(t) * sp.RangeMul;
            var hedefler = new List<Enemy>();
            for (int i = 0; i < 3; i++)
                hedefler.Add(Düşman(t, pGo.transform, Bootstrap.ArenaCenter + Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * alan * 0.6f));
            var dışarıda = Düşman(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.right * (alan + 3f));
            var havuzlar = hedefler.Select(e => e.TimeLeft).ToArray();
            float dışHavuz = dışarıda.TimeLeft;
            Sağ();
            for (int i = 1; i < sp.Hits; i++) pc.TickFlurry(sp.HitInterval + 0.001f);
            var damlalar = Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include);
            int üstünde = damlalar.Count(m => hedefler.Any(e => Düz(m.transform.position - e.transform.position).magnitude < 0.05f));
            failed += Check(ref total, $"⚠️⚠️ İPOTEK YAĞMURU {damlalar.Length}/{sp.Hits} ipotek, {üstünde} tanesi alandaki düşmanların ÜSTÜNDE (alan dışındakine değil)",
                            sp.Hits >= 4 && damlalar.Length == sp.Hits && üstünde == sp.Hits, üstünde);
            foreach (var m in damlalar) m.Step(sp.GroundDelay + 0.01f);
            int vurulan = 0;
            for (int i = 0; i < hedefler.Count; i++) if (hedefler[i].TimeLeft < havuzlar[i] - 0.01f) vurulan++;
            failed += Check(ref total, $"⚠️ ...gecikmeden sonra PATLAYIP vuruyor ({vurulan}/{hedefler.Count} düşman), alan dışındaki sağlam",
                            vurulan > 0 && dışarıda.TimeLeft >= dışHavuz - 0.01f, vurulan);
            Temizle();

            // ── 2) İCRA: bedel saatin YÜZDESİ ──
            pc.SetWeapon(Index<Faizli>());
            var icra = Weapon.All[Index<Faizli>()].Special(t);
            SıfırlaÖzel(pc);
            clock.Init(t, pGo.transform, 200f);
            float s0 = clock.Seconds; Sağ(); float bedel = s0 - clock.Seconds;
            failed += Check(ref total, $"⚠️ İCRA bedeli saatin %{t.icraCostPercent * 100f:0}'i ({bedel:0.0} sn @200 = {t.icraCostPercent * 200f:0.0})",
                            Mathf.Abs(bedel - t.icraCostPercent * 200f) < 0.01f, bedel);

            // ── 3) İCRA: hasar ÖDENEN bedelle artıyor ──
            float Hasar(float saat)
            {
                Temizle(); SıfırlaÖzel(pc);
                clock.Init(t, pGo.transform, saat);
                var e = Düşman(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.4f);
                float önce = e.TimeLeft; Sağ();
                return önce - e.TimeLeft;
            }
            float h100 = Hasar(100f), h400 = Hasar(400f);
            float fark = (400f - 100f) * t.icraCostPercent * t.icraDamagePerCost;
            failed += Check(ref total, $"⚠️⚠️ İCRA hasarı ödenen bedelle artıyor (@100 {h100:0.0} → @400 {h400:0.0}, fark {h400 - h100:0.0} ≈ {fark:0.0})",
                            Mathf.Abs(h400 - h100 - fark) < 0.5f, h400 - h100);

            // ── 4) İCRA: öldürürse iade — yalnızca saat doyma tavanının ALTINDAYKEN ──
            float hasat = t.harvestRate;
            t.harvestRate = 0f;                                  // hasat sıfır: saat farkı = −bedel + iade
            float İade(float saat)
            {
                Temizle(); SıfırlaÖzel(pc);
                clock.Init(t, pGo.transform, saat);
                var e = Düşman(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.4f);
                e.TakeDamage(e.TimeLeft);                        // boşalt: İCRA vurunca ÖLÜR
                float önce = clock.Seconds; Sağ();
                float ödenen = Mathf.Max(icra.Cost, icra.CostPercent * önce);
                return clock.Seconds - önce + ödenen;
            }
            float altında = İade(t.harvestSoftCap * 0.8f), üstünde2 = İade(t.harvestSoftCap * 2f);
            float beklenen = t.harvestSoftCap * 0.8f * t.icraCostPercent * t.icraKillRefund;
            failed += Check(ref total, $"⚠️ İCRA öldürünce iade: tavanın altında {altında:0.0} ≈ {beklenen:0.0} sn, üstünde {üstünde2:0.0} = 0 (\"eşiğin üstündeysen zaman vermez\")",
                            Mathf.Abs(altında - beklenen) < 0.05f && Mathf.Abs(üstünde2) < 0.05f, üstünde2);
            t.harvestRate = hasat;

            failed += SilahYükseltmeleri(t, pc, pGo, clock, ref total);
            failed += Sunaklar(t, pc, pGo, clock, ref total);

            Temizle();
            Object.DestroyImmediate(root);
            Clock.Reset(); Region.Reset();
            Debug.Log($"[Faz 29 öz-denetim] {(failed == 0 ? "✅" : "❌")} {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── 5) SİLAHA ÖZEL YÜKSELTMELER — on beşinin de ETKİSİ ─────────────────────
        static void Yük(SilahYükseltme y, bool açık)
        {
            var s = SaveFile.Current;
            s.weaponUpgrades = new int[SilahSunağı.Toplam];
            if (açık) s.weaponUpgrades[(int)y] = 1;
        }

        static int SilahYükseltmeleri(Tuning t, PlayerController pc, GameObject pGo, Clock clock, ref int total)
        {
            int failed = 0;
            var vurma = typeof(PlayerController).GetMethod("Strike", Özel);
            var özel = typeof(PlayerController).GetMethod("StartSpecial", Özel);
            var hasarı = typeof(PlayerController).GetMethod("WeaponDamage", Özel);
            Vector3 orta = Bootstrap.ArenaCenter, ileri = Vector3.forward;

            void Hazırla(SilahYükseltme y, bool açık, float saat)
            {
                Temizle(); Yük(y, açık); SıfırlaÖzel(pc);
                pc.SetWeapon(SilahSunağı.Silahı(y));
                pGo.transform.position = orta;
                pGo.transform.rotation = Quaternion.LookRotation(ileri);
                clock.Init(t, pGo.transform, saat);
            }
            bool Vurdu(SilahYükseltme y, bool açık, Vector3 yer)
            {
                Hazırla(y, açık, 200f);
                var e = Düşman(t, pGo.transform, yer); float h = e.TimeLeft;
                vurma.Invoke(pc, null);
                return e.TimeLeft < h - 0.001f;
            }
            // Konum taraması: yükseltme YENİ bir yere isabet kazandırıyor mu, hiçbir isabeti kaybettirmiyor mu.
            void Tara(SilahYükseltme y, System.Func<int, Vector3> yer, int adım, ref int f, ref int tot)
            {
                int kazanılan = 0, kaybedilen = 0;
                for (int i = 0; i < adım; i++)
                {
                    bool önce = Vurdu(y, false, yer(i)), sonra = Vurdu(y, true, yer(i));
                    if (!önce && sonra) kazanılan++;
                    if (önce && !sonra) kaybedilen++;
                }
                f += Check(ref tot, $"⚠️ {SilahSunağı.Ad[(int)y]}: {kazanılan} yeni konuma vuruyor, hiçbirini kaybetmiyor ({kaybedilen})",
                           kazanılan > 0 && kaybedilen == 0, kazanılan);
            }
            var koni = Weapon.All[0]; var haciz = Weapon.All[2];

            // KONİ
            Tara(SilahYükseltme.GenişAğ, i => orta + Quaternion.Euler(0f, i, 0f) * ileri * koni.Range(t) * 0.7f, 180, ref failed, ref total);
            float Bekleme(SilahYükseltme y, bool açık)
            {
                Hazırla(y, açık, 400f);
                özel.Invoke(pc, new object[] { ileri });
                return pc.SpecialCooldownLeft;
            }
            foreach (var y in new[] { SilahYükseltme.KısaSüpürme, SilahYükseltme.KısaİCRA })
            {
                float a = Bekleme(y, false), b = Bekleme(y, true);
                failed += Check(ref total, $"⚠️ {SilahSunağı.Ad[(int)y]}: sağ tık beklemesi ×0.7 ({a:0.0} → {b:0.0} sn)",
                                a > 0f && Mathf.Abs(b / a - 0.7f) < 0.01f, b / Mathf.Max(0.001f, a));
            }
            var itme = typeof(Enemy).GetField("_push", Özel);
            float İtme(bool açık)
            {
                Hazırla(SilahYükseltme.GeriTepme, açık, 200f);
                var e = Düşman(t, pGo.transform, orta + ileri * koni.Range(t) * 0.5f);
                vurma.Invoke(pc, null);
                return ((Vector3)itme.GetValue(e)).magnitude;
            }
            float i0 = İtme(false), i1 = İtme(true);
            failed += Check(ref total, $"⚠️ GERİ TEPME: KONİ savurması da itiyor ({i0:0.0} → {i1:0.0})", i1 > i0 + 1f, i1 - i0);

            // FAİZLİ — iade ve TEMERRÜT, hasatsız (saat farkı = iade)
            float hasat = t.harvestRate; t.harvestRate = 0f;
            float Kazanç(SilahYükseltme y, bool açık, bool düşmanVar)
            {
                Hazırla(y, açık, 200f);
                if (düşmanVar) { var e = Düşman(t, pGo.transform, orta + ileri * 1.4f); e.TakeDamage(e.TimeLeft); }   // boş: vuruş öldürür
                float önce = clock.Seconds; vurma.Invoke(pc, null);
                return clock.Seconds - önce;
            }
            float b0 = Kazanç(SilahYükseltme.Bileşik, false, true), b1 = Kazanç(SilahYükseltme.Bileşik, true, true);
            failed += Check(ref total, $"⚠️ BİLEŞİK: öldürme iadesi ×2 → ×3 ({b0:0.00} → {b1:0.00} sn, savurma bedeli {t.faizliSwingCost})",
                            Mathf.Abs(b0 - 2f * t.faizliSwingCost) < 0.01f && Mathf.Abs(b1 - 3f * t.faizliSwingCost) < 0.01f, b1);
            float m0 = Kazanç(SilahYükseltme.Temerrüt, false, false), m1 = Kazanç(SilahYükseltme.Temerrüt, true, false);
            failed += Check(ref total, $"⚠️ TEMERRÜT: ıskada bedelin yarısı geri ({m0:0.00} → {m1:0.00} sn)",
                            Mathf.Abs(m0) < 0.01f && Mathf.Abs(m1 - 0.5f * t.faizliSwingCost) < 0.01f, m1);
            t.harvestRate = hasat;

            // HACİZ
            Tara(SilahYükseltme.UzunKol, i => orta + ileri * haciz.Range(t) * (0.6f + i * 0.01f), 80, ref failed, ref total);
            Tara(SilahYükseltme.DarÖlüBölge, i => orta + ileri * haciz.MinRange(t) * (0.05f + i * 0.02f), 60, ref failed, ref total);
            int İkiHedef(bool açık)
            {
                Hazırla(SilahYükseltme.Delici, açık, 200f);
                var a = Düşman(t, pGo.transform, orta + ileri * haciz.Range(t) * 0.6f);
                var b = Düşman(t, pGo.transform, orta + ileri * haciz.Range(t) * 0.8f + Vector3.right * 0.3f);
                float ha = a.TimeLeft, hb = b.TimeLeft;
                vurma.Invoke(pc, null);
                return (a.TimeLeft < ha ? 1 : 0) + (b.TimeLeft < hb ? 1 : 0);
            }
            int d0 = İkiHedef(false), d1 = İkiHedef(true);
            failed += Check(ref total, $"⚠️ DELİCİ: iki hedef ({d0} → {d1} düşman)", d0 == 1 && d1 == 2, d1);

            // İPOTEK — işaretin sayısı, gecikmesi, alanı
            var süre = typeof(GroundMark).GetField("_timer", Özel);
            var alan = typeof(GroundMark).GetField("_radius", Özel);
            (int sayı, float gecikme, float yarıçap) İpotek(SilahYükseltme y, bool açık)
            {
                Hazırla(y, açık, 200f);
                vurma.Invoke(pc, null); vurma.Invoke(pc, null);
                var m = Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include);
                return (GroundMark.ActiveLiens, m.Length > 0 ? (float)süre.GetValue(m[0]) : 0f, m.Length > 0 ? (float)alan.GetValue(m[0]) : 0f);
            }
            failed += Check(ref total, $"⚠️ ÇİFT TAPU: aynı anda iki ipotek ({İpotek(SilahYükseltme.ÇiftTapu, false).sayı} → {İpotek(SilahYükseltme.ÇiftTapu, true).sayı})",
                            İpotek(SilahYükseltme.ÇiftTapu, false).sayı == 1 && İpotek(SilahYükseltme.ÇiftTapu, true).sayı == 2, 0f);
            float g0 = İpotek(SilahYükseltme.HızlıTapu, false).gecikme, g1 = İpotek(SilahYükseltme.HızlıTapu, true).gecikme;
            failed += Check(ref total, $"⚠️ HIZLI TAPU: gecikme ×0.7 ({g0:0.00} → {g1:0.00} sn)", g0 > 0f && Mathf.Abs(g1 / g0 - 0.7f) < 0.01f, g1 / Mathf.Max(0.001f, g0));
            float r0 = İpotek(SilahYükseltme.GenişArsa, false).yarıçap, r1 = İpotek(SilahYükseltme.GenişArsa, true).yarıçap;
            failed += Check(ref total, $"⚠️ GENİŞ ARSA: alan ×1.3 ({r0:0.00} → {r1:0.00})", r0 > 0f && Mathf.Abs(r1 / r0 - 1.3f) < 0.01f, r1 / Mathf.Max(0.001f, r0));

            // İFLAS
            float Hasar(bool açık) { Hazırla(SilahYükseltme.Dip, açık, t.iflasSafeSeconds); return (float)hasarı.Invoke(pc, new object[] { 1f }); }
            float h0 = Hasar(false), h1 = Hasar(true);
            failed += Check(ref total, $"⚠️ DİP: güvenli eşikte bile sertleşmeye başlıyor (hasar {h0:0.00} → {h1:0.00})", h1 > h0 + 0.01f, h1 - h0);
            int Seri(bool açık) { Hazırla(SilahYükseltme.Seri, açık, 200f); özel.Invoke(pc, new object[] { ileri }); return pc.FlurryLeft; }
            int s0 = Seri(false), s1 = Seri(true);
            failed += Check(ref total, $"⚠️ SERİ: TASFİYE +2 vuruş (kalan {s0} → {s1})", s1 == s0 + 2, s1 - s0);
            float Hasatı(bool açık)
            {
                Hazırla(SilahYükseltme.Kapanış, açık, t.iflasPanicSeconds);
                var e = Düşman(t, pGo.transform, orta + ileri * 1.4f);
                float önce = clock.TotalHarvested; vurma.Invoke(pc, null);
                return clock.TotalHarvested - önce;
            }
            float k0 = Hasatı(false), k1 = Hasatı(true);
            failed += Check(ref total, $"⚠️ KAPANIŞ: dipte hasat ×1.5 ({k0:0.00} → {k1:0.00})", k0 > 0f && Mathf.Abs(k1 / k0 - 1.5f) < 0.02f, k1 / Mathf.Max(0.001f, k0));

            // Yalnızca KUŞANILAN silahınki: GENİŞ AĞ açıkken HACİZ'in açısı değişmiyor
            Yük(SilahYükseltme.GenişAğ, true); pc.SetWeapon(2);
            var açı = typeof(PlayerController).GetProperty("SavurmaAçısı", Özel);
            failed += Check(ref total, "KONİ'nin yükseltmesi başka silaha geçmiyor (HACİZ'in açısı aynı)",
                            Mathf.Abs((float)açı.GetValue(pc) - haciz.Angle(t)) < 0.001f, (float)açı.GetValue(pc));
            Yük(SilahYükseltme.GenişAğ, false);
            pc.SetWeapon(0);
            Temizle();
            return failed;
        }

        // ── 6) SİLAH SUNAĞI (boss sonrası) · MUAFİYET · SABIR · dönen sunak ───────────
        static int Sunaklar(Tuning t, PlayerController pc, GameObject pGo, Clock clock, ref int total)
        {
            int failed = 0;
            Temizle();
            SaveFile.UseInMemory(new SaveData());
            pc.SetWeapon(0);
            clock.Init(t, pGo.transform, 1000f);
            var rmGo = new GameObject("faz29-oda");
            var rm = rmGo.AddComponent<RoomManager>();
            rm.Init(t, pGo.transform, null, startRoom: t.RoomsInRegion);              // 1. bölgenin ana boss'u
            var güncelle = typeof(RoomManager).GetMethod("Update", Özel);
            var gir = typeof(RoomManager).GetMethod("EnterRoom", Özel);
            var al = typeof(Altar).GetMethod("Buy", Özel);

            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            güncelle.Invoke(rm, null);                                                 // boss düştü
            var silahSunakları = Altar.All.Where(a => a != null && a.Type == Altar.Kind.Weapon).ToList();
            failed += Check(ref total, $"⚠️⚠️ ana boss düşünce boss odası SİLAH SUNAĞI'na dönüyor ({silahSunakları.Count} sunak, saat {(clock.Frozen ? "DURDU" : "akıyor")})",
                            silahSunakları.Count == SilahSunağı.SilahBaşına && clock.Frozen, silahSunakları.Count);
            if (silahSunakları.Count < SilahSunağı.SilahBaşına) { Temizle(); Object.DestroyImmediate(rmGo); RoomManager.Reset(); return failed; }

            string önceEtiket = silahSunakları[0].Label();
            pc.SetWeapon(3);                                                           // İPOTEK
            string sonraEtiket = silahSunakları[0].Label();
            failed += Check(ref total, $"⚠️ sunak KUŞANILAN silahınkini satıyor (silah değişince: '{önceEtiket.Split('—')[0].Trim()}' → '{sonraEtiket.Split('—')[0].Trim()}')",
                            önceEtiket.Contains(SilahSunağı.Ad[0]) && sonraEtiket.Contains(SilahSunağı.Ad[(int)SilahYükseltme.ÇiftTapu]), 0f);

            float s0 = clock.Seconds;
            al.Invoke(silahSunakları[0], null); al.Invoke(silahSunakları[1], null);
            bool üçüncüKapalı = silahSunakları[2].SoldOut;
            al.Invoke(silahSunakları[2], null);
            failed += Check(ref total, $"⚠️ boss başına {t.weaponAltarPurchases} alım: ÇİFT TAPU + HIZLI TAPU alındı, üçüncü kapalı ({SilahSunağı.AlınanSayısı} yükseltme, {s0 - clock.Seconds:0} sn ödendi)",
                            SilahSunağı.Var(SilahYükseltme.ÇiftTapu) && SilahSunağı.Var(SilahYükseltme.HızlıTapu)
                            && !SilahSunağı.Var(SilahYükseltme.GenişArsa) && üçüncüKapalı && SilahSunağı.AlınanSayısı == 2
                            && s0 - clock.Seconds > 0f, SilahSunağı.AlınanSayısı);

            // MUAFİYET: aynı düşmanın vuruşu ucuzluyor · SABIR: sağ tık beklemesi kısalıyor
            gir.Invoke(rm, new object[] { 3 });
            Enemy düşman = Enemy.All.FirstOrDefault(x => x != null && !x.IsBoss);
            var bedel = typeof(Enemy).GetProperty("HitCost", Özel);
            float v0 = (float)bedel.GetValue(düşman);
            clock.Yükselt(Altar.Kind.Insurance);
            float v1 = (float)bedel.GetValue(düşman);
            failed += Check(ref total, $"⚠️ MUAFİYET: düşman vuruşu ×{1f - t.altarInsuranceStep:0.00} ({v0:0.00} → {v1:0.00} sn)",
                            v0 > 0f && Mathf.Abs(v1 / v0 - (1f - t.altarInsuranceStep)) < 0.001f, v1 / Mathf.Max(0.001f, v0));
            pc.SetWeapon(0);
            var özel = typeof(PlayerController).GetMethod("StartSpecial", Özel);
            SıfırlaÖzel(pc); özel.Invoke(pc, new object[] { Vector3.forward }); float c0 = pc.SpecialCooldownLeft;
            clock.Yükselt(Altar.Kind.Patience);
            SıfırlaÖzel(pc); özel.Invoke(pc, new object[] { Vector3.forward }); float c1 = pc.SpecialCooldownLeft;
            failed += Check(ref total, $"⚠️ SABIR: sağ tık beklemesi ×{1f - t.altarPatienceStep:0.00} ({c0:0.0} → {c1:0.0} sn)",
                            c0 > 0f && Mathf.Abs(c1 / c0 - (1f - t.altarPatienceStep)) < 0.01f, c1 / Mathf.Max(0.001f, c0));

            // Dönen dördüncü sunak: 1. ara bölgede MUAFİYET, 2.'de SABIR
            Altar.Kind? Dördüncü(int araBölge)
            {
                gir.Invoke(rm, new object[] { RoomManager.SunakOdası(t, araBölge) });
                var tür = Altar.All.Where(a => a != null && (a.Type == Altar.Kind.Insurance || a.Type == Altar.Kind.Patience)).Select(a => (Altar.Kind?)a.Type).ToList();
                return tür.Count == 1 ? tür[0] : null;
            }
            var d0 = Dördüncü(0); var d1 = Dördüncü(1);
            failed += Check(ref total, $"⚠️ sunak odasında dönen 4. sunak: 1. ara bölge {d0}, 2. ara bölge {d1}",
                            d0 == Altar.Kind.Insurance && d1 == Altar.Kind.Patience, 0f);

            Temizle();
            Object.DestroyImmediate(rmGo);
            RoomManager.Reset();
            return failed;
        }

        // ── yardımcılar ──────────────────────────────────────────────────────────
        static Enemy Düşman(Tuning t, Transform player, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = pos;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(t, player, null);
            return e;
        }

        static void SıfırlaÖzel(PlayerController pc) =>
            typeof(PlayerController).GetField("_specialCd", Özel).SetValue(pc, new float[Weapon.All.Length]);

        static int Index<T>() where T : Weapon => System.Array.FindIndex(Weapon.All, w => w is T);

        static Vector3 Düz(Vector3 v) { v.y = 0f; return v; }

        static void Temizle()
        {
            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            foreach (var m in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include)) Object.DestroyImmediate(m.gameObject);
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include)) Object.DestroyImmediate(a.gameObject);
            GroundMark.ResetLiens();
            foreach (var a in Altar.All.ToArray()) if (a != null) Object.DestroyImmediate(a.gameObject);
            Altar.ResetAll();
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
