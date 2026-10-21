using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 36 — 3. bölgenin içeriği (Enes 2026-10-21: "commitle ve sıradaki faza geç"). KEFİL'in 5 adamı (KEFALETÇİ, GARANTÖR,
    /// ORTAK BORÇLU, ŞAHİT, BAŞ KEFİL), 2 yeni tür (SÜLÜK, MEVDUAT), 2 yeni diziliş (İÇ İÇE, SÜTUN). Her imza ETKİ olarak.
    /// </summary>
    public static class Faz36SelfCheck
    {
        const BindingFlags Gizli = BindingFlags.NonPublic | BindingFlags.Instance;
        static readonly MethodInfo Adım = typeof(Enemy).GetMethod("TickState", Gizli);

        [MenuItem("Tek Can/Faz 36 öz-denetimi çalıştır (3. bölge içeriği)")]
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
            failed += Sülük(t, ref total);
            failed += Mevduat(t, ref total);
            failed += Açılış(t, ref total);
            failed += Dizilişler(t, ref total);

            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset(); RoomManager.Reset(); RevivalSystem.Reset();
            Debug.Log($"[Faz 36 öz-denetim] {(failed == 0 ? "✅" : "❌")} {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── 1) KEFİL'in 5 adamı ──
        static int Adamlar(Tuning t, ref int total)
        {
            int failed = 0, S = Mathf.Max(1, t.subRegionsPerRegion);
            var ana = BossKind.For(2);
            var anaAdlar = ana.Attacks(1).Concat(ana.Attacks(2)).Select(a => a.Name).ToList();
            var adlar = new System.Collections.Generic.List<string>();
            bool tamam = S == MiniBossKind.Adamlar[2].Length;
            for (int s = 0; s < S; s++)
            {
                var m = MiniBossKind.For(2, s, S);
                var tanım = MiniBossKind.Adamlar[2][Mathf.Min(s, MiniBossKind.Adamlar[2].Length - 1)];
                var saldırı = m.Attacks(1).Select(a => a.Name).ToArray();
                adlar.Add($"{m.Name} ({string.Join(" · ", saldırı)})");
                bool yakınUzak = m.Attacks(1).Any(a => a.Tür == SaldırıTürü.Yakın) && m.Attacks(1).Any(a => a.Tür == SaldırıTürü.Uzak);
                bool bedava = m.AuraDrain(t, 1) <= 0f && m.ZorlaAlır <= 0f;
                if (m.Özgün != tanım || m.Name != tanım.Ad || m.PhaseCount != 1 || string.IsNullOrEmpty(m.Crime)
                    || saldırı.Length != tanım.Saldırı.Length || saldırı.Any(a => !anaAdlar.Contains(a)) || !yakınUzak || bedava) tamam = false;
            }
            failed += Check(ref total, $"⚠️⚠️ 3. bölgenin 5 mini boss'u KEFİL'in adamları: saldırıları KEFİL'den, tek faz, her birinde yakın + uzak, hiçbiri bedava değil: {string.Join(" · ", adlar)}",
                            tamam, adlar.Count);
            var son = MiniBossKind.For(2, S - 1, S);
            failed += Check(ref total, $"son adam ({son.Name}) KEFİL'in harita saldırısını öğretiyor",
                            son.Attacks(1).Any(a => a.Tür == SaldırıTürü.Harita), 0);
            var geçici = MiniBossKind.For(3, 0, S);
            failed += Check(ref total, $"4. bölgeden sonrası geçici dilimde ({geçici.Name})", geçici.Özgün == null && geçici.Name.Contains("·"), 0);
            return failed;
        }

        // ── 2) İmzalar — ETKİ ──
        static int İmzalar(Tuning t, ref int total)
        {
            int failed = 0, S = Mathf.Max(1, t.subRegionsPerRegion);
            BossKind M(int s) => MiniBossKind.For(2, s, S);
            float Vuruş(Enemy b) { float h0 = b.TimeLeft; b.TakeDamage(1f); return h0 - b.TimeLeft; }

            // KEFALETÇİ: havuzu %30'un altına inince aldığı hasar yarıya iner
            var (r1, c1, p1, kefaletçi) = Kur(t, M(0));
            float tam = Vuruş(kefaletçi);
            Havuz(kefaletçi, kefaletçi.TimeLeft * 0.25f);
            float dipte = Vuruş(kefaletçi);
            Temizle(r1);
            var (r1b, c1b, p1b, başka) = Kur(t, M(2));
            Havuz(başka, başka.TimeLeft * 0.25f);
            float başkaDipte = Vuruş(başka);
            Temizle(r1b);
            failed += Check(ref total, $"⚠️ KEFALETÇİ son çeyrekte direnir: vuruş tamken {tam:0.00}, %25'te {dipte:0.00} (×0.5) · ORTAK BORÇLU %25'te {başkaDipte:0.00}",
                            Mathf.Abs(tam - 1f) < 0.001f && Mathf.Abs(dipte - 0.5f) < 0.001f && Mathf.Abs(başkaDipte - 1f) < 0.001f, dipte);

            // GARANTÖR: 12 sn'de bir 3 vuruşu HASARSIZ ve HASATSIZ yutar; 4. vuruş işler; 12 sn sonra yenilenir
            var (r2, c2, p2, garantör) = Kur(t, M(1));
            garantör.TickBossPassives(0.01f);
            float h0 = garantör.TimeLeft, s0 = c2.TotalHarvested;
            for (int i = 0; i < 3; i++) garantör.TakeDamage(1f);
            bool yuttu = Mathf.Abs(garantör.TimeLeft - h0) < 0.001f && Mathf.Abs(c2.TotalHarvested - s0) < 1e-6f && garantör.Teminat == 0 && garantör.TeminatYuttu == 3;
            float dördüncü = Vuruş(garantör);
            garantör.TickBossPassives(12f);
            int yenilendi = garantör.Teminat;
            Temizle(r2);
            failed += Check(ref total, $"⚠️⚠️ GARANTÖR teminat: 3 vuruş hasarsız/hasatsız yutuldu ({(yuttu ? "evet" : "HAYIR")}), 4. vuruş {dördüncü:0.00} işledi, 12 sn sonra teminat {yenilendi}",
                            yuttu && Mathf.Abs(dördüncü - 1f) < 0.001f && yenilendi == 3, dördüncü);

            // ORTAK BORÇLU: oyuncu 3 birim içindeyken aurası iki kat; uzaktayken bir kat
            float Emdi(BossKind kind, float uzaklık)
            {
                var (r, c, p, b) = Kur(t, kind);
                p.position = b.transform.position + Vector3.back * uzaklık;
                float s = c.Seconds; b.ApplyAura(1f);
                Temizle(r);
                return s - c.Seconds;
            }
            float yakın = Emdi(M(2), 2f), uzak = Emdi(M(2), 5f), kefil = M(2).AuraDrain(t, 1);
            failed += Check(ref total, $"⚠️ ORTAK BORÇLU dibindeyken iki kat emer: 2 birimde {yakın:0.00}/sn, 5 birimde {uzak:0.00}/sn (kendi aurası {kefil:0.00})",
                            Mathf.Abs(uzak - kefil) < 0.001f && Mathf.Abs(yakın - kefil * 2f) < 0.001f, yakın);

            // ŞAHİT: yaşadığı sürece her dash 1.5 sn; başka adam varken bedava
            float Dash(BossKind kind)
            {
                var (r, c, p, b) = Kur(t, kind);
                var pc = p.GetComponent<PlayerController>();
                float s = c.Seconds;
                typeof(PlayerController).GetMethod("StartDash", Gizli).Invoke(pc, new object[] { Vector3.forward });
                float bedel = s - c.Seconds, ödenen = pc.ÖdenenDashBedeli;
                Temizle(r);
                return Mathf.Abs(bedel - ödenen) < 0.001f ? bedel : -1f;
            }
            float şahitle = Dash(M(3)), onsuz = Dash(M(0));
            failed += Check(ref total, $"⚠️ ŞAHİT dash'ini görür: yanında dash −{şahitle:0.0} sn, KEFALETÇİ'nin yanında −{onsuz:0.0} sn",
                            Mathf.Abs(şahitle - 1.5f) < 0.001f && Mathf.Abs(onsuz) < 0.001f, şahitle);

            // BAŞ KEFİL: kefalet + teminat (15 sn'de 2)
            var bk = M(4);
            failed += Check(ref total, $"BAŞ KEFİL ikisinden: kefalet ×{bk.Kefalet:0.0} · teminat {bk.TeminatSayısı} / {bk.TeminatAralığı:0} sn",
                            Mathf.Abs(bk.Kefalet - 0.5f) < 0.001f && bk.TeminatSayısı == 2 && Mathf.Abs(bk.TeminatAralığı - 15f) < 0.001f, bk.TeminatSayısı);
            return failed;
        }

        // ── 3) SÜLÜK: değince yapışır, peşinden gelir, emer; dash silkeler, 2 sn yapışamaz ──
        static int Sülük(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, clock, player, _) = Kur(t, null);
            var pc = player.GetComponent<PlayerController>();
            var e = Düşman(t, root, player, EnemyKind.Leech, Bootstrap.ArenaCenter + Vector3.forward * 0.8f);
            typeof(Enemy).GetField("_attackTimer", Gizli).SetValue(e, 99f);
            Adım.Invoke(e, new object[] { 0.01f });
            bool yapıştı = e.Yapışık;
            player.position = Bootstrap.ArenaCenter + Vector3.right * 3f;              // oyuncu kaçtı
            float s0 = clock.Seconds;
            Adım.Invoke(e, new object[] { 0.5f });
            float peşinde = new Vector2(e.transform.position.x - player.position.x, e.transform.position.z - player.position.z).magnitude;
            float emdi = s0 - clock.Seconds;
            typeof(PlayerController).GetField("_iFrames", Gizli).SetValue(pc, 1f);     // dash'in dokunulmazlığı
            Adım.Invoke(e, new object[] { 0.01f });
            bool koptu = !e.Yapışık && e.Silkelendi == 1;
            typeof(PlayerController).GetField("_iFrames", Gizli).SetValue(pc, 0f);
            e.transform.position = player.position + Vector3.forward * 0.8f;
            Adım.Invoke(e, new object[] { 0.01f });
            bool hemenYapışmadı = !e.Yapışık;
            Adım.Invoke(e, new object[] { 2.1f });
            e.transform.position = player.position + Vector3.forward * 0.8f;
            Adım.Invoke(e, new object[] { 0.01f });
            bool yineYapıştı = e.Yapışık && e.Yapıştı == 2;
            Temizle(root);
            failed += Check(ref total, $"⚠️⚠️ SÜLÜK: değince yapıştı ({(yapıştı ? "evet" : "HAYIR")}), oyuncu kaçınca peşinden geldi ({peşinde:0.0} birim), 0.5 sn'de −{emdi:0.00} sn · dash'le koptu ({(koptu ? "evet" : "HAYIR")}), hemen yapışmadı ({(hemenYapışmadı ? "evet" : "HAYIR")}), 2 sn sonra yine yapıştı ({(yineYapıştı ? "evet" : "HAYIR")})",
                            yapıştı && peşinde < 1f && Mathf.Abs(emdi - 0.5f) < 0.01f && koptu && hemenYapışmadı && yineYapıştı, emdi);
            return failed;
        }

        // ── 4) MEVDUAT: yaşadıkça hasadı artar, saniyede +%10, en çok ×2.5 ──
        static int Mevduat(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, clock, player, _) = Kur(t, null);
            var e = Düşman(t, root, player, EnemyKind.Deposit, Bootstrap.ArenaCenter + Vector3.forward * 3f);
            var r = e.GetComponentInChildren<Renderer>();
            float Hasat() { float s0 = clock.TotalHarvested; e.TakeDamage(0.5f); return clock.TotalHarvested - s0; }
            typeof(Enemy).GetField("_flashTimer", Gizli).SetValue(e, 0f);
            Color genç = r.sharedMaterial.color;
            float h0 = Hasat();
            typeof(Enemy).GetField("_flashTimer", Gizli).SetValue(e, 0f);
            e.TickKindPassives(10f);
            Color olgun = r.sharedMaterial.color;
            float h10 = Hasat();
            e.TickKindPassives(20f);
            float h30 = Hasat();
            Temizle(root);
            failed += Check(ref total, $"⚠️⚠️ MEVDUAT değerlenir: hasat 0. sn {h0:0.000} · 10. sn {h10:0.000} (×2) · 30. sn {h30:0.000} (en çok ×2.5) · olgunlaştıkça renk değişti ({(genç != olgun ? "evet" : "HAYIR")})",
                            h0 > 0f && Mathf.Abs(h10 / h0 - 2f) < 0.01f && Mathf.Abs(h30 / h0 - 2.5f) < 0.01f && genç != olgun, h10 / Mathf.Max(1e-6f, h0));
            return failed;
        }

        // ── 5) Açılış: SÜLÜK ve MEVDUAT 3. bölgede, tavanlar 4/7/10/12/13 ──
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
            var b2 = Doğan(1); var b3 = Doğan(2);
            Temizle(root);
            failed += Check(ref total, $"⚠️ 3. bölgede {b3.Count} tür ({string.Join(", ", b3.Select(k => k.Name))}) — SÜLÜK ve MEVDUAT 3. bölgede var, 2. bölgede yok; tavanlar {string.Join("/", Region.All.Select(x => x.UnlockedKinds))}",
                            b3.Contains(EnemyKind.Leech) && b3.Contains(EnemyKind.Deposit) && !b2.Contains(EnemyKind.Leech) && !b2.Contains(EnemyKind.Deposit)
                            && Region.All.Select(x => x.UnlockedKinds).SequenceEqual(new[] { 4, 7, 10, 12, 13 }), b3.Count);
            return failed;
        }

        // ── 6) İÇ İÇE iki halka · SÜTUN yol boyu tek sıra; 3. bölgenin dövüş odalarında geliyorlar ──
        static int Dizilişler(Tuning t, ref int total)
        {
            int failed = 0, n = 9;
            Vector3 merkez = Bootstrap.ArenaCenter + new Vector3(0f, 0f, t.arenaRadius * 0.12f);
            var iç = Enumerable.Range(0, n).Select(i => RoomLayout.Nested.Position(t, i, n)).Select(p => new Vector2(p.x - merkez.x, p.z - merkez.z).magnitude).ToArray();
            int içtekiler = iç.Count(r => Mathf.Abs(r - t.arenaRadius * 0.18f) < 0.01f), dıştakiler = iç.Count(r => Mathf.Abs(r - t.arenaRadius * 0.5f) < 0.01f);
            bool ikiHalka = içtekiler == n / 3 && dıştakiler == n - n / 3;
            var sü = Enumerable.Range(0, n).Select(i => RoomLayout.Column.Position(t, i, n)).ToArray();
            bool sütun = sü.All(p => Mathf.Abs(p.x) <= t.arenaRadius * 0.061f) && sü.Max(p => p.z) - sü.Min(p => p.z) > t.arenaRadius * 0.7f;
            var bölge3 = new System.Collections.Generic.HashSet<RoomLayout>(Enumerable.Range(1, t.RoomsInRegion)
                .Where(oda => RoomManager.Tür(t, oda) == RoomManager.OdaTürü.Dövüş).Select(oda => RoomManager.Diziliş(t, 2, oda)));
            failed += Check(ref total, $"⚠️ İÇ İÇE iki halka ({içtekiler} içte, {dıştakiler} dışta) · SÜTUN yol boyu tek sıra ({(sütun ? "evet" : "HAYIR")}) · 3. bölgenin dövüş odalarında ikisi de geliyor",
                            ikiHalka && sütun && bölge3.Contains(RoomLayout.Nested) && bölge3.Contains(RoomLayout.Column), içtekiler);
            return failed;
        }

        // ── kurgu ──────────────────────────────────────────────────────────────
        static (GameObject, Clock, Transform, Enemy) Kur(Tuning t, BossKind kind)
        {
            Enemy.All.Clear();
            RevivalSystem.Reset();
            var root = new GameObject("faz36-check");
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

        static void Havuz(Enemy e, float v) => typeof(Enemy).GetProperty("TimeLeft").GetSetMethod(true).Invoke(e, new object[] { v });

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
