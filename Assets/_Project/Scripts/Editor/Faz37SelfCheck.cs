using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 37 — 4. bölgenin içeriği (Enes 2026-10-21: "commitle ve sıradaki faza geç"). SİMSAR'ın 5 adamı (TELLAL, ARACI, DELLAL,
    /// SEYYAR, BAŞ SİMSAR), 2 yeni tür (BLOKE, CEZAİ ŞART), 2 yeni diziliş (DÖRT KÖŞE, ÇAPRAZ). Her imza ETKİ olarak.
    /// </summary>
    public static class Faz37SelfCheck
    {
        const BindingFlags Gizli = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tek Can/Faz 37 öz-denetimi çalıştır (4. bölge içeriği)")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;
            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 300f;
            t.enemyDecayPerSecond = 0f;
            t.harvestSoftCap = 1e6f;          // doyma bu kapının konusu değil
            Region.Reset();

            failed += Adamlar(t, ref total);
            failed += İmzalar(t, ref total);
            failed += Bloke(t, ref total);
            failed += Ceza(t, ref total);
            failed += Açılış(t, ref total);
            failed += Dizilişler(t, ref total);

            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset(); RoomManager.Reset(); RevivalSystem.Reset();
            Debug.Log($"[Faz 37 öz-denetim] {(failed == 0 ? "✅" : "❌")} {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── 1) SİMSAR'ın 5 adamı ──
        static int Adamlar(Tuning t, ref int total)
        {
            int failed = 0, S = Mathf.Max(1, t.subRegionsPerRegion);
            var ana = BossKind.For(3);
            var anaAdlar = ana.Attacks(1).Concat(ana.Attacks(2)).Select(a => a.Name).ToList();
            var adlar = new System.Collections.Generic.List<string>();
            bool tamam = S == MiniBossKind.Adamlar[3].Length;
            for (int s = 0; s < S; s++)
            {
                var m = MiniBossKind.For(3, s, S);
                var tanım = MiniBossKind.Adamlar[3][Mathf.Min(s, MiniBossKind.Adamlar[3].Length - 1)];
                var saldırı = m.Attacks(1).Select(a => a.Name).ToArray();
                adlar.Add($"{m.Name} ({string.Join(" · ", saldırı)})");
                bool yakınUzak = m.Attacks(1).Any(a => a.Tür == SaldırıTürü.Yakın) && m.Attacks(1).Any(a => a.Tür == SaldırıTürü.Uzak);
                bool bedava = m.AuraDrain(t, 1) <= 0f && m.ZorlaAlır <= 0f;
                if (m.Özgün != tanım || m.Name != tanım.Ad || m.PhaseCount != 1 || string.IsNullOrEmpty(m.Crime)
                    || saldırı.Length != tanım.Saldırı.Length || saldırı.Any(a => !anaAdlar.Contains(a)) || !yakınUzak || bedava) tamam = false;
            }
            failed += Check(ref total, $"⚠️⚠️ 4. bölgenin 5 mini boss'u SİMSAR'ın adamları: saldırıları SİMSAR'dan, tek faz, her birinde yakın + uzak, hiçbiri bedava değil: {string.Join(" · ", adlar)}",
                            tamam, adlar.Count);
            var son = MiniBossKind.For(3, S - 1, S);
            failed += Check(ref total, $"son adam ({son.Name}) SİMSAR'ın harita saldırısını öğretiyor",
                            son.Attacks(1).Any(a => a.Tür == SaldırıTürü.Harita), 0);
            return failed;
        }

        // ── 2) İmzalar — ETKİ ──
        static int İmzalar(Tuning t, ref int total)
        {
            int failed = 0, S = Mathf.Max(1, t.subRegionsPerRegion);
            BossKind M(int s) => MiniBossKind.For(3, s, S);
            float Vuruş(Enemy b, float hasar = 1f) { float h0 = b.TimeLeft; b.TakeDamage(hasar); return h0 - b.TimeLeft; }
            void Bak(Enemy b, Vector3 nereye) => b.transform.rotation = Quaternion.LookRotation(Düz(nereye - b.transform.position));

            // TELLAL: 1.5 sn'de bir işaret — ailenin 4 sn'sinden sık; ailenin hepsi işaret bırakıyor
            int İşaretler(BossKind kind)
            {
                var (r, c, p, b) = Kur(t, kind);
                for (int i = 0; i < 60; i++) b.TickBossPassives(0.1f);
                int n = Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include).Length;
                Temizle(r);
                return n;
            }
            int tellal = İşaretler(M(0)), aracı = İşaretler(M(1));
            failed += Check(ref total, $"⚠️ TELLAL durduğun yeri ilan eder: 6 sn'de {tellal} işaret (1.5 sn'de bir) · ARACI {aracı} (ailenin 4 sn'si)",
                            tellal >= 4 && aracı >= 1 && aracı <= 2, tellal);

            // ARACI: önünden vuruş ×0.4, arkasından tam · yavaş döner (120°/sn) — SEYYAR anında döner
            var (r2, c2, p2, ar) = Kur(t, M(1));
            Bak(ar, p2.position);
            float önden = Vuruş(ar);
            Bak(ar, ar.transform.position * 2f - p2.position);
            float arkadan = Vuruş(ar);
            typeof(Enemy).GetMethod("FacePlayer", Gizli).Invoke(ar, new object[] { 0.5f });
            float kalanAçı = Vector3.Angle(Düz(ar.transform.forward), Düz(p2.position - ar.transform.position));
            Temizle(r2);
            var (r2b, c2b, p2b, sey) = Kur(t, M(3));
            Bak(sey, sey.transform.position * 2f - p2b.position);
            typeof(Enemy).GetMethod("FacePlayer", Gizli).Invoke(sey, new object[] { 0.5f });
            float seyAçı = Vector3.Angle(Düz(sey.transform.forward), Düz(p2b.position - sey.transform.position));
            Temizle(r2b);
            failed += Check(ref total, $"⚠️⚠️ ARACI araya girer: önünden vuruş {önden:0.00} (×0.4), arkasından {arkadan:0.00} · sırtı dönükken 0.5 sn'de 40° döndü (kalan {kalanAçı:0}°) · SEYYAR anında döner (kalan {seyAçı:0}°)",
                            Mathf.Abs(önden - 0.4f) < 0.001f && Mathf.Abs(arkadan - 1f) < 0.001f && Mathf.Abs(kalanAçı - 140f) < 1f && seyAçı < 1f, önden);

            // DELLAL: oyuncu yerinde durursa iki kat emer, yürürken bir kat; TELLAL yerinde durana bir kat
            var (r3, c3, p3, del) = Kur(t, M(2));
            float Emdi(Enemy b, Clock c) { float s = c.Seconds; b.ApplyAura(1f); return s - c.Seconds; }
            p3.position += Vector3.right * 2f;
            float yürürken = Emdi(del, c3);
            float dururken = Emdi(del, c3);
            bool duruyordu = del.Duruyor;
            p3.position += Vector3.left * 2f;
            float yineYürürken = Emdi(del, c3);
            float kendi = M(2).AuraDrain(t, 1);
            Temizle(r3);
            var (r3b, c3b, p3b, tel) = Kur(t, M(0));
            Emdi(tel, c3b);
            float tellalDururken = Emdi(tel, c3b), tellalKendi = M(0).AuraDrain(t, 1);
            Temizle(r3b);
            failed += Check(ref total, $"⚠️⚠️ DELLAL yerinde durana iki kat: yürürken {yürürken:0.00}/sn, dururken {dururken:0.00}/sn ({(duruyordu ? "duruyor" : "DURMUYOR")}), yine yürürken {yineYürürken:0.00}/sn (kendi aurası {kendi:0.00}) · TELLAL dururken bir kat",
                            Mathf.Abs(yürürken - kendi) < 0.001f && Mathf.Abs(dururken - kendi * 2f) < 0.001f && duruyordu
                            && Mathf.Abs(yineYürürken - kendi) < 0.001f && Mathf.Abs(tellalDururken - tellalKendi) < 0.001f, dururken);

            // SEYYAR: havuzunun her %10'unda oyuncudan 4 birim geri sıçrar; ARACI sıçramaz
            float Sıçrayış(BossKind kind, out int sıçradı, out bool erken)
            {
                var (r, c, p, b) = Kur(t, kind);
                b.transform.position = Bootstrap.ArenaCenter + Vector3.forward * 2f;
                Bak(b, b.transform.position * 2f - p.position);                 // sırtı dönük: ARACI'nın kalkanı karışmasın
                float havuz = b.TimeLeft, d0 = Düz(b.transform.position - p.position).magnitude;
                b.TakeDamage(havuz * 0.06f);
                erken = Mathf.Abs(Düz(b.transform.position - p.position).magnitude - d0) > 0.001f;
                b.TakeDamage(havuz * 0.06f);
                float d1 = Düz(b.transform.position - p.position).magnitude;
                sıçradı = b.Sıçradı;
                Temizle(r);
                return d1 - d0;
            }
            float uzaklaştı = Sıçrayış(M(3), out int seySıçradı, out bool seyErken), aracıUzaklaştı = Sıçrayış(M(1), out int aracıSıçradı, out _);
            failed += Check(ref total, $"⚠️⚠️ SEYYAR vurdukça kaçar: havuzun %6'sında yerinde ({(seyErken ? "SIÇRADI" : "evet")}), %12'sinde {uzaklaştı:0.0} birim geri sıçradı ({seySıçradı} kez) · ARACI {aracıUzaklaştı:0.0} birim ({aracıSıçradı} kez)",
                            !seyErken && Mathf.Abs(uzaklaştı - 4f) < 0.01f && seySıçradı == 1 && Mathf.Abs(aracıUzaklaştı) < 0.001f && aracıSıçradı == 0, uzaklaştı);

            // BAŞ SİMSAR: SEYYAR'ın sıçrayışı, kalkansız (kalkanla 6.5 dk sürdü)
            var bş = M(4);
            failed += Check(ref total, $"BAŞ SİMSAR SEYYAR'ın sıçrayışıyla: sıçrayış {bş.Sıçrayış:0} birim · önden ×{bş.ÖndenHasar:0.0} (kalkansız)",
                            Mathf.Abs(bş.ÖndenHasar - 1f) < 0.001f && Mathf.Abs(bş.Sıçrayış - 4f) < 0.001f, bş.Sıçrayış);
            return failed;
        }

        // ── 3) BLOKE: yanındayken (3 birim) yürüyüş ×0.55, uzaktayken tam; alanı halkayla görünüyor ──
        static int Bloke(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, clock, player, _) = Kur(t, null);
            var pc = player.GetComponent<PlayerController>();
            float önce = pc.YürüyüşÇarpanı;
            var e = Düşman(t, root, player, EnemyKind.Frozen, Bootstrap.ArenaCenter + Vector3.forward * 2f);
            float yanında = pc.YürüyüşÇarpanı;
            player.position = Bootstrap.ArenaCenter + Vector3.back * 2f;
            float uzakta = pc.YürüyüşÇarpanı;
            int halka0 = Object.FindObjectsByType<Arc>(FindObjectsInactive.Include).Length;
            e.TickKindPassives(0.01f);
            bool halka = Object.FindObjectsByType<Arc>(FindObjectsInactive.Include).Length > halka0;
            Temizle(root);
            failed += Check(ref total, $"⚠️⚠️ BLOKE: yokken yürüyüş ×{önce:0.00}, 2 birimde ×{yanında:0.00}, 4 birimde ×{uzakta:0.00} · alanı halkayla görünüyor ({(halka ? "evet" : "HAYIR")})",
                            Mathf.Abs(önce - 1f) < 0.001f && Mathf.Abs(yanında - 0.55f) < 0.001f && Mathf.Abs(uzakta - 1f) < 0.001f && halka, yanında);
            return failed;
        }

        // ── 4) CEZAİ ŞART: her inen darbe sonrakini %40 pahalı yapar (en çok ×2.2), 4 sn darbesiz söner; yokken zincir yok ──
        static int Ceza(Tuning t, ref int total)
        {
            int failed = 0;
            float[] Darbeler(bool şartlı, int kaç, out float söndü, out bool kızardı)
            {
                var (root, clock, player, _) = Kur(t, null);
                var şart = şartlı ? Düşman(t, root, player, EnemyKind.Penalty, Bootstrap.ArenaCenter + Vector3.forward * 6f) : null;
                var pc = player.GetComponent<PlayerController>();
                float Darbe() { float s = clock.Seconds; pc.TakeHit(1f, Bootstrap.ArenaCenter + Vector3.forward); return s - clock.Seconds; }
                var l = new float[kaç];
                for (int i = 0; i < kaç; i++) l[i] = Darbe();
                söndü = -1f; kızardı = false;
                if (şart != null)
                {
                    var rend = şart.GetComponentInChildren<Renderer>();
                    typeof(Enemy).GetField("_flashTimer", Gizli).SetValue(şart, 0f);
                    şart.TickKindPassives(0.01f);
                    Color kızgın = rend.sharedMaterial.color;
                    şart.TickKindPassives(4.1f);                                 // 4 sn darbesiz
                    kızardı = kızgın != rend.sharedMaterial.color;
                    söndü = Darbe();
                }
                Temizle(root);
                return l;
            }
            var şartlı = Darbeler(true, 6, out float söndü, out bool kızardı);
            var şartsız = Darbeler(false, 3, out _, out _);
            bool zincir = Mathf.Abs(şartlı[0] - 1f) < 0.001f && Mathf.Abs(şartlı[1] - 1.4f) < 0.001f && Mathf.Abs(şartlı[2] - 1.8f) < 0.001f
                       && Mathf.Abs(şartlı[4] - 2.2f) < 0.001f && Mathf.Abs(şartlı[5] - 2.2f) < 0.001f;
            failed += Check(ref total, $"⚠️⚠️ CEZAİ ŞART zinciri: darbeler {string.Join(" · ", şartlı.Select(x => x.ToString("0.0")))} (en çok ×2.2) · zincir kızarttı ({(kızardı ? "evet" : "HAYIR")}) · 4 sn sonra {söndü:0.0} · CEZAİ ŞART yokken {string.Join(" · ", şartsız.Select(x => x.ToString("0.0")))}",
                            zincir && kızardı && Mathf.Abs(söndü - 1f) < 0.001f && şartsız.All(x => Mathf.Abs(x - 1f) < 0.001f), şartlı[1]);
            return failed;
        }

        // ── 5) Açılış: BLOKE ve CEZAİ ŞART 4. bölgede, tavanlar 4/7/10/14/15 ──
        static int Açılış(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, clock, player, _) = Kur(t, null);
            var rm = root.AddComponent<RoomManager>();
            rm.Init(t, player, null);
            var pick = typeof(RoomManager).GetMethod("PickKind", Gizli);
            var gir = typeof(RoomManager).GetMethod("EnterRegion", Gizli);
            System.Collections.Generic.HashSet<EnemyKind> Doğan(int bölge)
            {
                gir.Invoke(rm, new object[] { bölge, 1 });
                var h = new System.Collections.Generic.HashSet<EnemyKind>();
                for (int oda = 1; oda <= t.RoomsInRegion; oda++)
                    for (int i = 0; i < 12; i++) h.Add((EnemyKind)pick.Invoke(rm, new object[] { oda, i }));
                return h;
            }
            var b3 = Doğan(2); var b4 = Doğan(3);
            Temizle(root);
            failed += Check(ref total, $"⚠️ 4. bölgede {b4.Count} tür ({string.Join(", ", b4.Select(k => k.Name))}) — BLOKE ve CEZAİ ŞART 4. bölgede var, 3. bölgede yok; 4. bölgenin tavanı {Region.At(3).UnlockedKinds}",
                            b4.Contains(EnemyKind.Frozen) && b4.Contains(EnemyKind.Penalty) && !b3.Contains(EnemyKind.Frozen) && !b3.Contains(EnemyKind.Penalty)
                            && Region.At(3).UnlockedKinds == 14, b4.Count);
            return failed;
        }

        // ── 6) DÖRT KÖŞE dört öbek, ortası boş · ÇAPRAZ ortası dolu, kollar çaprazlarda; 4. bölgenin dövüş odalarında geliyorlar ──
        static int Dizilişler(Tuning t, ref int total)
        {
            int failed = 0, n = 9;
            float R = t.arenaRadius;
            var kö = Enumerable.Range(0, n).Select(i => RoomLayout.Corners.Position(t, i, n)).ToArray();
            Vector3 orta = Bootstrap.ArenaCenter + new Vector3(0f, 0f, R * 0.225f);
            bool ortasıBoş = kö.All(p => Düz(p - orta).magnitude > R * 0.4f);
            int öbek = kö.Select(p => (p.x > Bootstrap.ArenaCenter.x, p.z > orta.z)).Distinct().Count();
            var ça = Enumerable.Range(0, n).Select(i => RoomLayout.Cross.Position(t, i, n)).ToArray();
            Vector3 kesişim = Bootstrap.ArenaCenter + new Vector3(0f, 0f, R * 0.2f);
            bool çapraz = Düz(ça[0] - kesişim).magnitude < 0.01f
                       && ça.Skip(1).All(p => Mathf.Abs(Mathf.Abs(p.x - kesişim.x) * 0.8f - Mathf.Abs(p.z - kesişim.z)) < 0.01f && Düz(p - kesişim).magnitude > R * 0.1f);
            var bölge4 = new System.Collections.Generic.HashSet<RoomLayout>(Enumerable.Range(1, t.RoomsInRegion)
                .Where(oda => RoomManager.Tür(t, oda) == RoomManager.OdaTürü.Dövüş).Select(oda => RoomManager.Diziliş(t, 3, oda)));
            failed += Check(ref total, $"⚠️ DÖRT KÖŞE {öbek} öbek, ortası boş ({(ortasıBoş ? "evet" : "HAYIR")}) · ÇAPRAZ ortası dolu, kollar çaprazlarda ({(çapraz ? "evet" : "HAYIR")}) · 4. bölgenin dövüş odalarında ikisi de geliyor",
                            öbek == 4 && ortasıBoş && çapraz && bölge4.Contains(RoomLayout.Corners) && bölge4.Contains(RoomLayout.Cross), öbek);
            return failed;
        }

        // ── kurgu ──────────────────────────────────────────────────────────────
        static Vector3 Düz(Vector3 v) { v.y = 0f; return v; }

        static (GameObject, Clock, Transform, Enemy) Kur(Tuning t, BossKind kind)
        {
            Enemy.All.Clear();
            RevivalSystem.Reset();
            var root = new GameObject("faz37-check");
            var player = new GameObject("player"); player.transform.SetParent(root.transform);
            player.transform.position = Bootstrap.ArenaCenter;
            player.AddComponent<PlayerController>().Init(t, null);
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, t.startSeconds);
            clock.Frozen = false;
            Enemy boss = null;
            if (kind != null)
                boss = Bootstrap.SpawnEnemyAt(t, Bootstrap.ArenaCenter + Vector3.forward * 4f, player.transform,
                                              null, 1f, boss: true, parent: root.transform, bossKind: kind);
            return (root, clock, player.transform, boss);
        }

        static Enemy Düşman(Tuning t, GameObject root, Transform player, EnemyKind kind, Vector3 yer)
            => Bootstrap.SpawnEnemyAt(t, yer, player, null, 1f, boss: false, kind: kind, parent: root.transform);

        static void Temizle(GameObject root)
        {
            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include)) Object.DestroyImmediate(a.gameObject);
            foreach (var m in Object.FindObjectsByType<Mote>(FindObjectsInactive.Include)) Object.DestroyImmediate(m.gameObject);
            foreach (var g in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include)) Object.DestroyImmediate(g.gameObject);
            if (root != null) Object.DestroyImmediate(root);
            Clock.Reset(); RevivalSystem.Reset(); RoomManager.Reset();
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
