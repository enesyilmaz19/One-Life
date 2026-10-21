using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 35 — 2. bölgenin içeriği (Enes 2026-10-21: "sıradaki faza geç"). EKSPER'in 5 adamı (MUHAMMİN, MUHASİP,
    /// DEFTERDAR, BİLİRKİŞİ, BAŞ EKSPER), 2 yeni tür (VADELİ, TAKSİT), 2 yeni diziliş (KORİDOR, OK). Her imza ETKİ olarak.
    /// </summary>
    public static class Faz35SelfCheck
    {
        const BindingFlags Gizli = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tek Can/Faz 35 öz-denetimi çalıştır (2. bölge içeriği)")]
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
            failed += Vadeli(t, ref total);
            failed += Taksit(t, ref total);
            failed += Açılış(t, ref total);
            failed += Dizilişler(t, ref total);

            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset(); RoomManager.Reset(); RevivalSystem.Reset();
            Debug.Log($"[Faz 35 öz-denetim] {(failed == 0 ? "✅" : "❌")} {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── 1) EKSPER'in 5 adamı ──
        static int Adamlar(Tuning t, ref int total)
        {
            int failed = 0, S = Mathf.Max(1, t.subRegionsPerRegion);
            var ana = BossKind.For(1);
            var anaAdlar = ana.Attacks(1).Concat(ana.Attacks(2)).Select(a => a.Name).ToList();
            var adlar = new System.Collections.Generic.List<string>();
            bool tamam = S == MiniBossKind.Adamlar[1].Length;
            for (int s = 0; s < S; s++)
            {
                var m = MiniBossKind.For(1, s, S);
                var tanım = MiniBossKind.Adamlar[1][Mathf.Min(s, MiniBossKind.Adamlar[1].Length - 1)];
                var saldırı = m.Attacks(1).Select(a => a.Name).ToArray();
                adlar.Add($"{m.Name} ({string.Join(" · ", saldırı)})");
                bool yakınUzak = m.Attacks(1).Any(a => a.Tür == SaldırıTürü.Yakın) && m.Attacks(1).Any(a => a.Tür == SaldırıTürü.Uzak);
                bool bedava = m.AuraDrain(t, 1) <= 0f && m.ZorlaAlır <= 0f;
                if (m.Özgün != tanım || m.Name != tanım.Ad || m.PhaseCount != 1 || string.IsNullOrEmpty(m.Crime)
                    || saldırı.Length != tanım.Saldırı.Length || saldırı.Any(a => !anaAdlar.Contains(a)) || !yakınUzak || bedava) tamam = false;
            }
            failed += Check(ref total, $"⚠️⚠️ 2. bölgenin 5 mini boss'u EKSPER'in adamları: saldırıları EKSPER'den, tek faz, her birinde yakın + uzak, hiçbiri bedava değil: {string.Join(" · ", adlar)}",
                            tamam, adlar.Count);
            var son = MiniBossKind.For(1, S - 1, S);
            failed += Check(ref total, $"son adam ({son.Name}) EKSPER'in harita saldırısını öğretiyor",
                            son.Attacks(1).Any(a => a.Tür == SaldırıTürü.Harita), 0);
            var geçici = MiniBossKind.For(2, 0, S);
            failed += Check(ref total, $"3. bölgeden sonrası geçici dilimde ({geçici.Name})", geçici.Özgün == null && geçici.Name.Contains("·"), 0);
            return failed;
        }

        // ── 2) İmzalar — ETKİ ──
        static int İmzalar(Tuning t, ref int total)
        {
            int failed = 0, S = Mathf.Max(1, t.subRegionsPerRegion);
            BossKind M(int s) => MiniBossKind.For(1, s, S);

            // MUHAMMİN: yaşadığı sürece hasadın ×0.45 (EKSPER faz 1'in tamamı) — öldürülünce geri
            float Hasat(BossKind kind)
            {
                var (r, c, p, b) = Kur(t, kind);
                var e = Düşman(t, r, p, EnemyKind.Basic, Bootstrap.ArenaCenter + Vector3.left * 2f);
                float h0 = c.TotalHarvested; e.TakeDamage(1f);
                float h = c.TotalHarvested - h0;
                Temizle(r);
                return h;
            }
            float yalın = Hasat(null), muhammin = Hasat(M(0)), aile = Hasat(M(1));
            failed += Check(ref total, $"⚠️ MUHAMMİN hasadını kısar: yanında vuruş {muhammin:0.000} = {yalın:0.000} × 0.45 (EKSPER faz 1) · ailenin kesintisi × 0.6 (MUHASİP {aile:0.000})",
                            yalın > 0f && Mathf.Abs(muhammin / yalın - 0.45f) < 0.001f && Mathf.Abs(aile / yalın - 0.6f) < 0.001f, muhammin / Mathf.Max(1e-6f, yalın));

            // MUHASİP: dövüş sürdükçe vuruş her 10 sn %10 değer kaybeder, en çok %50
            var (r2, c2, p2, muhasip) = Kur(t, M(1));
            float Vuruş(Enemy b) { float h0 = b.TimeLeft; b.TakeDamage(1f); return h0 - b.TimeLeft; }
            float baş = Vuruş(muhasip);
            muhasip.TickBossPassives(25f);
            float yirmi = Vuruş(muhasip);
            muhasip.TickBossPassives(100f);
            float tavan = Vuruş(muhasip);
            Temizle(r2);
            var (r2b, c2b, p2b, muhammin2) = Kur(t, M(0));
            muhammin2.TickBossPassives(60f);
            float başkası = Vuruş(muhammin2);
            Temizle(r2b);
            failed += Check(ref total, $"⚠️⚠️ MUHASİP amortisman: vuruş başta {baş:0.00} · 25 sn'de {yirmi:0.00} (−%20) · 125 sn'de {tavan:0.00} (en çok −%50) · MUHAMMİN'de 60 sn'de {başkası:0.00}",
                            Mathf.Abs(baş - 1f) < 0.001f && Mathf.Abs(yirmi - 0.8f) < 0.001f && Mathf.Abs(tavan - 0.5f) < 0.001f && Mathf.Abs(başkası - 1f) < 0.001f, yirmi);

            // DEFTERDAR: İNEN isabeti hasadı 3 sn sıfırlar; MUHAMMİN'in isabeti sıfırlamaz
            (float isabet, float sonrakiHasat) Defter(BossKind kind, string saldırı)
            {
                var (r, c, p, b) = Kur(t, kind);
                var e = Düşman(t, r, p, EnemyKind.Basic, Bootstrap.ArenaCenter + Vector3.left * 2f);
                float inen = Vur(b, p, saldırı, yakın: true);
                Object.DestroyImmediate(b.gameObject); Enemy.All.Remove(b);              // hasat kesintisi (MUHAMMİN) karışmasın
                float h0 = c.TotalHarvested; e.TakeDamage(1f);
                var sonuç = (inen, c.TotalHarvested - h0);
                Temizle(r);
                return sonuç;
            }
            var defter = Defter(M(2), "SAYIM"); var değil = Defter(M(0), "KESİNTİ");
            failed += Check(ref total, $"⚠️⚠️ DEFTERDAR deftere yazar: isabetten (−{defter.isabet:0.0} sn) sonra vuruşun hasadı {defter.sonrakiHasat:0.000}; MUHAMMİN'in isabetinden sonra {değil.sonrakiHasat:0.000}",
                            defter.isabet > 0f && Mathf.Abs(defter.sonrakiHasat) < 1e-6f && değil.isabet > 0f && değil.sonrakiHasat > 0f, defter.sonrakiHasat);

            // BİLİRKİŞİ: isabeti saatinin %6'sı — ama normal bedelden az değil
            float Bedel(BossKind kind, string saldırı, float saat)
            {
                var (r, c, p, b) = Kur(t, kind);
                c.Init(t, p, saat); c.Frozen = false;
                float inen = Vur(b, p, saldırı, yakın: true);
                Temizle(r);
                return inen;
            }
            float zengin = Bedel(M(3), "İBRA", 1000f), fakir = Bedel(M(3), "İBRA", 40f), normal = Bedel(M(4), "KESİNTİ+", 1000f);
            failed += Check(ref total, $"⚠️ BİLİRKİŞİ değer biçer: 1000 sn'de isabet −{zengin:0.0} (= %6), 40 sn'de −{fakir:0.0} (normal bedel, %6'dan büyük); BAŞ EKSPER'in yakını 1000 sn'de −{normal:0.0}",
                            Mathf.Abs(zengin - 60f) < 0.01f && fakir > 40f * 0.06f && fakir < zengin && normal < 30f, zengin);
            return failed;
        }

        // ── 3) VADELİ: 4 sn'de bir 1.5 sn açık; kapalıyken ×0.1 (hasat da), görünür ──
        static int Vadeli(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, clock, player, _) = Kur(t, null);
            var e = Düşman(t, root, player, EnemyKind.Term, Bootstrap.ArenaCenter + Vector3.forward * 2f);
            var r = e.GetComponentInChildren<Renderer>();
            float Vuruş(out float hasat) { float h0 = e.TimeLeft, s0 = clock.TotalHarvested; e.TakeDamage(1f); hasat = clock.TotalHarvested - s0; return h0 - e.TimeLeft; }
            bool k0 = e.VadeAçık; Color renkKapalı = r.sharedMaterial.color;
            float kapalı = Vuruş(out float kHasat);
            typeof(Enemy).GetField("_flashTimer", Gizli).SetValue(e, 0f);            // vurulma parlaması rengi örtmesin (bozma H yakaladı)
            e.TickKindPassives(3f);                                                   // 3. sn: açık (2.5–4)
            bool a3 = e.VadeAçık; Color renkAçık = r.sharedMaterial.color;
            float açık = Vuruş(out float aHasat);
            e.TickKindPassives(1.2f);                                                 // 4.2. sn: yeniden kapalı
            bool k42 = e.VadeAçık;
            Color koyu = Color.Lerp(e.KindColor, new Color(0.18f, 0.18f, 0.22f), 0.65f);
            bool renkDoğru = Renk(renkKapalı, koyu) && Renk(renkAçık, e.KindColor);
            Temizle(root);
            failed += Check(ref total, $"⚠️⚠️ VADELİ: kapalıyken {kapalı:0.00} (hasat {kHasat:0.000}), açıkken {açık:0.00} (hasat {aHasat:0.000}) — ×0.1 · 0. sn {(k0 ? "AÇIK" : "kapalı")} · 3. sn {(a3 ? "açık" : "KAPALI")} · 4.2. sn {(k42 ? "AÇIK" : "kapalı")} · kapalıyken koyu, açıkken kendi rengi ({(renkDoğru ? "evet" : $"HAYIR: kapalı {renkKapalı} açık {renkAçık}")})",
                            !k0 && a3 && !k42 && Mathf.Abs(kapalı / açık - 0.1f) < 0.001f && Mathf.Abs(kHasat / Mathf.Max(1e-6f, aHasat) - 0.1f) < 0.01f && renkDoğru, kapalı / Mathf.Max(0.001f, açık));
            return failed;
        }

        // ── 4) TAKSİT: ölünce 2 parça (havuzunun %35'i), parçalar bölünmez ──
        static int Taksit(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, clock, player, _) = Kur(t, null);
            var e = Düşman(t, root, player, EnemyKind.Installment, Bootstrap.ArenaCenter + Vector3.forward * 3f);
            float havuz = e.TimeLeft;
            e.TakeDamage(e.TimeLeft); e.TakeDamage(1f);
            var parçalar = Enemy.All.Where(x => x != null && x.Kind == EnemyKind.InstallmentPart).ToList();
            bool doğru = parçalar.Count == 2 && parçalar.All(p => Mathf.Abs(p.TimeLeft - havuz * 0.35f) < 0.01f);
            int önce = Enemy.All.Count;
            float parçaHavuz = parçalar.Count > 0 ? parçalar[0].TimeLeft : 0f;
            if (parçalar.Count > 0) { parçalar[0].TakeDamage(parçalar[0].TimeLeft); parçalar[0].TakeDamage(1f); }
            int sonra = Enemy.All.Count;
            Temizle(root);
            failed += Check(ref total, $"⚠️⚠️ TAKSİT ölünce {parçalar.Count} parça doğdu (her biri {parçaHavuz:0.0} = %35 × {havuz:0}); parça ölünce bölünmedi ({önce} → {sonra})",
                            doğru && sonra == önce - 1, parçalar.Count);
            return failed;
        }

        // ── 5) Açılış: VADELİ ve TAKSİT 2. bölgede, tavanlar 4/7/8/10/11 ──
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
            var b1 = Doğan(0); var b2 = Doğan(1);
            Temizle(root);
            failed += Check(ref total, $"⚠️ 2. bölgede {b2.Count} tür ({string.Join(", ", b2.Select(k => k.Name))}) — VADELİ ve TAKSİT 2. bölgede var, 1. bölgede yok; tavanlar {string.Join("/", Region.All.Select(r => r.UnlockedKinds))}",
                            b2.Contains(EnemyKind.Term) && b2.Contains(EnemyKind.Installment) && !b1.Contains(EnemyKind.Term) && !b1.Contains(EnemyKind.Installment)
                            && !b2.Contains(EnemyKind.InstallmentPart) && System.Array.IndexOf(EnemyKind.All, EnemyKind.InstallmentPart) < 0
                            && Region.All.Select(r => r.UnlockedKinds).SequenceEqual(new[] { 4, 7, 8, 10, 11 }), b2.Count);
            return failed;
        }

        // ── 6) KORİDOR iki dar sıra yol boyu · OK ucu girişe bakan V; 2. bölgede geliyorlar ──
        static int Dizilişler(Tuning t, ref int total)
        {
            int failed = 0, n = 9;
            var k = Enumerable.Range(0, n).Select(i => RoomLayout.Corridor.Position(t, i, n)).ToArray();
            float x0 = Mathf.Abs(k[0].x);
            bool tünel = k.All(p => Mathf.Abs(Mathf.Abs(p.x) - x0) < 0.01f) && k.Any(p => p.x > 0f) && k.Any(p => p.x < 0f)
                      && k.Max(p => p.z) - k.Min(p => p.z) > t.arenaRadius * 0.6f && x0 * 2f < t.arenaRadius * 0.5f;
            var o = Enumerable.Range(0, n).Select(i => RoomLayout.Arrow.Position(t, i, n)).ToArray();
            bool uç = Mathf.Abs(o[0].x) < 0.01f && o.Skip(1).All(p => p.z > o[0].z) && Mathf.Abs(o[n - 1].x) > Mathf.Abs(o[1].x) + 1f && o[n - 1].z > o[1].z;
            System.Collections.Generic.HashSet<RoomLayout> Dövüşte(int bölge) => new System.Collections.Generic.HashSet<RoomLayout>(
                Enumerable.Range(1, t.RoomsInRegion).Where(oda => RoomManager.Tür(t, oda) == RoomManager.OdaTürü.Dövüş)
                          .Select(oda => RoomManager.Diziliş(t, bölge, oda)));
            var bölge2 = Dövüşte(1);
            failed += Check(ref total, $"⚠️ KORİDOR iki dar sıra yol boyu ({(tünel ? "evet" : "HAYIR")}, aralık {x0 * 2f:0.0}) · OK ucu girişe bakan V ({(uç ? "evet" : "HAYIR")}) · 2. bölgenin DÖVÜŞ odalarında ikisi de geliyor",
                            tünel && uç && bölge2.Contains(RoomLayout.Corridor) && bölge2.Contains(RoomLayout.Arrow), x0);
            // ⚠️ Diziliş oda numarasıyla dönüyordu; 9 diziliş = 9'luk blok → ikisi her bölgede yalnız mini / sunak odasına düştü
            var eksik = Enumerable.Range(0, Region.All.Length).Select(b => (b, RoomLayout.All.Length - Dövüşte(b).Count)).Where(x => x.Item2 > 0).ToList();
            failed += Check(ref total, $"⚠️⚠️ her bölgede {RoomLayout.All.Length} dizilişin hepsi en az bir DÖVÜŞ odasına düşüyor{(eksik.Count > 0 ? " — eksik: " + string.Join(", ", eksik.Select(x => $"{x.b + 1}. bölgede {x.Item2}")) : "")}",
                            eksik.Count == 0, eksik.Count);
            return failed;
        }

        // ── kurgu ──────────────────────────────────────────────────────────────
        static (GameObject, Clock, Transform, Enemy) Kur(Tuning t, BossKind kind)
        {
            Enemy.All.Clear();
            RevivalSystem.Reset();
            var root = new GameObject("faz35-check");
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

        static bool Renk(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.01f;

        static Enemy Düşman(Tuning t, GameObject root, Transform player, EnemyKind kind, Vector3 yer)
            => Bootstrap.SpawnEnemyAt(t, yer, player, null, 1f, boss: false, kind: kind, parent: root.transform);

        /// <summary>Boss'a bu saldırıyı yaptır: oyuncu yakınsa dibinde, değilse menzil dışında. Dönen: saatten inen.</summary>
        static float Vur(Enemy boss, Transform player, string saldırı, bool yakın)
        {
            var a = boss.Boss.Attacks(1).First(x => x.Name == saldırı);
            player.position = boss.transform.position + Vector3.back * (yakın ? 0.5f : 30f);
            typeof(Enemy).GetField("_chained", Gizli).SetValue(boss, a);
            typeof(Enemy).GetMethod("EnterWindup", Gizli).Invoke(boss, null);
            float d0 = Clock.Instance.TotalHitCost;
            typeof(Enemy).GetMethod("EnterStrike", Gizli).Invoke(boss, null);
            return Clock.Instance.TotalHitCost - d0;
        }

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
