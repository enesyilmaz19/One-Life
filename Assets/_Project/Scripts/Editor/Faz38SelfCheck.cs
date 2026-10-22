using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 38 — 5. bölgenin içeriği (Enes 2026-10-22: "commit at ve sonraki faza geç"). A0: mini boss'un ısrar tavanı (botun zorlanma
    /// ölçüsü: mini zorlasın, öldürmesin). MURABAHACI'nın 5 adamı (TEFECİ, VADECİ, FAİZCİ, İCRA MEMURU, BAŞ TEFECİ), 2 yeni tür (MÜHLET,
    /// REFİNANS), 2 yeni diziliş (SARMAL, ZİKZAK). Her imza ETKİ olarak.
    /// </summary>
    public static class Faz38SelfCheck
    {
        const BindingFlags Gizli = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tek Can/Faz 38 öz-denetimi çalıştır (5. bölge içeriği)")]
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
            failed += Israr(t, ref total);
            failed += İmzalar(t, ref total);
            failed += Mühlet(t, ref total);
            failed += Refinans(t, ref total);
            failed += Açılış(t, ref total);
            failed += Dizilişler(t, ref total);

            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset(); RoomManager.Reset(); RevivalSystem.Reset();
            Debug.Log($"[Faz 38 öz-denetim] {(failed == 0 ? "✅" : "❌")} {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── 1) MURABAHACI'nın 5 adamı · artık geçici bölge yok ──
        static int Adamlar(Tuning t, ref int total)
        {
            int failed = 0, S = Mathf.Max(1, t.subRegionsPerRegion);
            var ana = BossKind.For(4);
            var anaAdlar = ana.Attacks(1).Concat(ana.Attacks(2)).Select(a => a.Name).ToList();
            var adlar = new System.Collections.Generic.List<string>();
            bool tamam = S == MiniBossKind.Adamlar[4].Length;
            for (int s = 0; s < S; s++)
            {
                var m = MiniBossKind.For(4, s, S);
                var tanım = MiniBossKind.Adamlar[4][Mathf.Min(s, MiniBossKind.Adamlar[4].Length - 1)];
                var saldırı = m.Attacks(1).Select(a => a.Name).ToArray();
                adlar.Add($"{m.Name} ({string.Join(" · ", saldırı)})");
                bool yakınUzak = m.Attacks(1).Any(a => a.Tür == SaldırıTürü.Yakın) && m.Attacks(1).Any(a => a.Tür == SaldırıTürü.Uzak);
                bool bedava = m.AuraDrain(t, 1) <= 0f && m.ZorlaAlır <= 0f;
                if (m.Özgün != tanım || m.Name != tanım.Ad || m.PhaseCount != 1 || string.IsNullOrEmpty(m.Crime)
                    || saldırı.Length != tanım.Saldırı.Length || saldırı.Any(a => !anaAdlar.Contains(a)) || !yakınUzak || bedava) tamam = false;
            }
            failed += Check(ref total, $"⚠️⚠️ 5. bölgenin 5 mini boss'u MURABAHACI'nın adamları: saldırıları MURABAHACI'dan, tek faz, her birinde yakın + uzak, hiçbiri bedava değil: {string.Join(" · ", adlar)}",
                            tamam, adlar.Count);
            var son = MiniBossKind.For(4, S - 1, S);
            failed += Check(ref total, $"son adam ({son.Name}) MURABAHACI'nın harita saldırısını öğretiyor",
                            son.Attacks(1).Any(a => a.Tür == SaldırıTürü.Harita), 0);
            var geçici = Enumerable.Range(0, Region.All.Length).SelectMany(b => Enumerable.Range(0, S).Select(s => MiniBossKind.For(b, s, S)))
                                   .Where(m => m.Özgün == null).Select(m => m.Name).ToList();
            failed += Check(ref total, $"geçici mini kalmadı: 5 bölgenin {Region.All.Length * S} mini'sinin hepsi özgün{(geçici.Count > 0 ? " — geçici: " + string.Join(", ", geçici) : "")}",
                            geçici.Count == 0, geçici.Count);
            return failed;
        }

        // ── 2) A0: mini'nin ısrarı ×2.5'te durur, ana boss'unki ×5'e çıkar ──
        static int Israr(Tuning t, ref int total)
        {
            int failed = 0, S = Mathf.Max(1, t.subRegionsPerRegion);
            float Seri(BossKind kind)
            {
                var (r, c, p, b) = Kur(t, kind);
                p.position = b.transform.position + Vector3.back * 1f;          // dibinde: hep yakın sıra
                typeof(Enemy).GetProperty("Dağarcık", Gizli).GetValue(b);         // ⚠️ ilk kuruluş ısrarı sıfırlıyor — seriyi ondan SONRA yaz
                typeof(Enemy).GetField("_sonBant", Gizli).SetValue(b, SaldırıTürü.Yakın);
                typeof(Enemy).GetField("_bantSerisi", Gizli).SetValue(b, 20);
                typeof(Enemy).GetField("_haritasız", Gizli).SetValue(b, 0);
                typeof(Enemy).GetMethod("PickAttack", Gizli).Invoke(b, null);
                float ı = b.IsrarÇarpanı;
                Temizle(r);
                return ı;
            }
            float mini = Seri(MiniBossKind.For(3, 2, S)), ana = Seri(BossKind.For(3));
            failed += Check(ref total, $"⚠️⚠️ A0 — mini zorlasın, öldürmesin: dibinde 21. yakın saldırıda DELLAL ×{mini:0.00} (tavan ×{t.miniBossStreakCap:0.0}), SİMSAR ×{ana:0.00} (tavan ×{t.bossStreakCap:0})",
                            Mathf.Abs(mini - t.miniBossStreakCap) < 0.001f && Mathf.Abs(ana - t.bossStreakCap) < 0.001f, mini);
            return failed;
        }

        // ── 3) İmzalar — ETKİ ──
        static int İmzalar(Tuning t, ref int total)
        {
            int failed = 0, S = Mathf.Max(1, t.subRegionsPerRegion);
            BossKind M(int s) => MiniBossKind.For(4, s, S);
            float Vuruş(Enemy b) { float h0 = b.TimeLeft; b.TakeDamage(1f); return h0 - b.TimeLeft; }
            float Emdi(Enemy b, Clock c) { float s = c.Seconds; b.ApplyAura(1f); return s - c.Seconds; }

            float krediHasat = 0f, sonraHasat = 0f;
            // TEFECİ: vurulduktan sonra 1 sn faiz işlemez, sonra havuzu saniyede %0.4 dolar · VADECİ'nin faizi yok
            float Faiz(BossKind kind, out float erken)
            {
                var (r, c, p, b) = Kur(t, kind);
                float havuz = b.TimeLeft;
                b.TickBossPassives(2f);                                          // önce boşta: isabet bu sayacı SIFIRLAMALI
                b.TakeDamage(havuz * 0.3f);
                float h0 = b.TimeLeft;
                b.TickBossPassives(0.5f);
                erken = b.TimeLeft - h0;
                b.TickBossPassives(1f);
                float dolan = (b.TimeLeft - h0) / havuz;
                float s0 = c.TotalHarvested;
                b.TakeDamage(dolan * havuz);                                     // faizle dolanı vur: kredi — hasat yok
                krediHasat = c.TotalHarvested - s0;
                s0 = c.TotalHarvested;
                b.TakeDamage(1f);
                sonraHasat = c.TotalHarvested - s0;
                Temizle(r);
                return dolan;
            }
            float vadeci = Faiz(M(1), out _), tefeci = Faiz(M(0), out float tefeciErken);   // TEFECİ sonda: kredi ölçümü onun
            failed += Check(ref total, $"⚠️⚠️ TEFECİ borç işler: vurulunca 0.5 sn'de +{tefeciErken:0.00} (durur), 1 sn vurulmayınca havuzun %{tefeci * 100f:0.00}'ü doldu (%0.8/sn), faizle dolanı vurmak hasat vermedi (+{krediHasat:0.000}, sonraki vuruş +{sonraHasat:0.000}) · VADECİ %{vadeci * 100f:0.00}",
                            Mathf.Abs(tefeciErken) < 0.0001f && Mathf.Abs(tefeci - 0.008f) < 0.0001f && Mathf.Abs(krediHasat) < 1e-5f && sonraHasat > 0f
                            && Mathf.Abs(vadeci) < 0.0001f, tefeci);

            // VADECİ: 5 sn'de bir 3 sn açık; kapalıyken vuruş ×0.25 ve koyu · TEFECİ hep açık
            var (r2, c2, p2, vd) = Kur(t, M(1));
            var rend = vd.GetComponentInChildren<Renderer>();
            Color Renk(Enemy e)                                                 // isabet parlaması örtmesin
            {
                typeof(Enemy).GetField("_flashTimer", Gizli).SetValue(e, 0f);
                typeof(Enemy).GetMethod("Paint", Gizli).Invoke(e, null);
                return rend.sharedMaterial.color;
            }
            vd.TickKindPassives(0.01f);
            Color kapalıRenk = Renk(vd);
            float kapalı = Vuruş(vd);
            vd.TickKindPassives(2.5f);
            bool renk = Renk(vd) != kapalıRenk;
            float açık = Vuruş(vd);
            Temizle(r2);
            var (r2b, c2b, p2b, tf) = Kur(t, M(0));
            float tefeciVuruş = Vuruş(tf);
            Temizle(r2b);
            failed += Check(ref total, $"⚠️⚠️ VADECİ vadesi gelmeden ödemez (4 sn'de bir 1 sn kapanır): kapalıyken vuruş {kapalı:0.00} (×0.25), açılınca {açık:0.00}, renk değişti ({(renk ? "evet" : "HAYIR")}) · TEFECİ {tefeciVuruş:0.00}",
                            Mathf.Abs(kapalı - 0.25f) < 0.001f && Mathf.Abs(açık - 1f) < 0.001f && renk && Mathf.Abs(tefeciVuruş - 1f) < 0.001f, kapalı);

            // FAİZCİ: aurası dövüşün her 10 sn'sinde %4 binerek büyür · İCRA MEMURU'nunki büyümez
            float Büyüme(BossKind kind)
            {
                var (r, c, p, b) = Kur(t, kind);
                float baş = Emdi(b, c);
                b.TickBossPassives(30f);
                float son = Emdi(b, c);
                Temizle(r);
                return son / baş;
            }
            float faizci = Büyüme(M(2)), icra = Büyüme(M(3));
            failed += Check(ref total, $"⚠️⚠️ FAİZCİ'nin faizi bileşik: 30 sn sonra aurası ×{faizci:0.000} (1.04³ = {Mathf.Pow(1.04f, 3f):0.000}) · İCRA MEMURU ×{icra:0.000}",
                            Mathf.Abs(faizci - Mathf.Pow(1.04f, 3f)) < 0.001f && Mathf.Abs(icra - 1f) < 0.001f, faizci);

            // İCRA MEMURU: havuzu yarıya inince iki kat emer · BAŞ TEFECİ yarıda da bir kat
            float Muaccel(BossKind kind, out float tam)
            {
                var (r, c, p, b) = Kur(t, kind);
                tam = Emdi(b, c);
                Havuz(b, b.TimeLeft * 0.45f);
                float yarıda = Emdi(b, c);
                Temizle(r);
                return yarıda / tam;
            }
            float muaccel = Muaccel(M(3), out float icraTam), başKat = Muaccel(M(4), out _);
            failed += Check(ref total, $"⚠️⚠️ İCRA MEMURU yarıdan sonra muaccel: tamken {icraTam:0.00}/sn, %45'te ×{muaccel:0.00} (×1.6) · BAŞ TEFECİ %45'te ×{başKat:0.00}",
                            Mathf.Abs(muaccel - 1.6f) < 0.001f && Mathf.Abs(başKat - 1f) < 0.001f, muaccel);

            // BAŞ TEFECİ: FAİZCİ'nin bileşik faizi, yarı hızda (TEFECİ'nin faizi dövüşü 5+ dk'ya uzatıyordu)
            var bş = M(4);
            failed += Check(ref total, $"BAŞ TEFECİ FAİZCİ'nin bileşik faiziyle, yarı hızda: 10 sn'de %{bş.BileşikFaiz * 100f:0} · faizi yok (%{bş.Faiz * 100f:0.0})",
                            Mathf.Abs(bş.BileşikFaiz - 0.02f) < 0.0001f && bş.Faiz <= 0f, bş.BileşikFaiz);
            return failed;
        }

        // ── 4) MÜHLET: süresinde öldürülmezse ödemeden gider, bedeli 3 darbe saatten; öldürülürse bedel yok; süre bittikçe solar ──
        static int Mühlet(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, clock, player, _) = Kur(t, null);
            var e = Düşman(t, root, player, EnemyKind.Grace, Bootstrap.ArenaCenter + Vector3.forward * 6f);
            float darbe = (float)typeof(Enemy).GetProperty("HitCost", Gizli).GetValue(e);
            var rend = e.GetComponentInChildren<Renderer>();
            typeof(Enemy).GetField("_flashTimer", Gizli).SetValue(e, 0f);
            e.TickKindPassives(0.01f);
            Color genç = rend.sharedMaterial.color;
            float s0 = clock.Seconds, süre = EnemyKind.Grace.MühletSüresi;
            e.TickKindPassives(süre - 0.2f);
            bool durdu = !e.MühletDoldu && Enemy.All.Contains(e) && Mathf.Abs(clock.Seconds - s0) < 0.0001f;
            bool soldu = rend.sharedMaterial.color != genç;
            e.TickKindPassives(0.3f);
            bool gitti = e.MühletDoldu && !Enemy.All.Contains(e);
            float bedel = s0 - clock.Seconds;
            var ikinci = Düşman(t, root, player, EnemyKind.Grace, Bootstrap.ArenaCenter + Vector3.forward * 6f);
            ikinci.TickKindPassives(10f);
            ikinci.TakeDamage(1e6f);
            float s1 = clock.Seconds;
            bool öldü = !ikinci.MühletDoldu;
            if (Enemy.All.Contains(ikinci)) ikinci.TickKindPassives(10f);
            float sonrası = s1 - clock.Seconds;
            var üçüncü = Düşman(t, root, player, EnemyKind.Grace, player.position + Vector3.forward * 2f);   // sana gelmez: uzaklaşır
            typeof(Enemy).GetField("_attackTimer", Gizli).SetValue(üçüncü, 99f);
            float u0 = Düz(üçüncü.transform.position - player.position).magnitude;
            typeof(Enemy).GetMethod("TickState", Gizli).Invoke(üçüncü, new object[] { 0.5f });
            float uzaklaştı = Düz(üçüncü.transform.position - player.position).magnitude - u0;
            Temizle(root);
            failed += Check(ref total, $"⚠️⚠️ MÜHLET: {süre - 0.2f:0.0} sn'de yerinde, bedelsiz ({(durdu ? "evet" : "HAYIR")}) ve soldu ({(soldu ? "evet" : "HAYIR")}) · {süre:0}. sn'de gitti ({(gitti ? "evet" : "HAYIR")}), saatten −{bedel:0.0} (3 darbe = {darbe * 3f:0.0}) · süresinde kesilen bedelsiz (−{sonrası:0.0}) · 2 birimdeyken 0.5 sn'de {uzaklaştı:0.0} birim uzaklaştı (sana gelmez)",
                            durdu && soldu && gitti && Mathf.Abs(bedel - darbe * 3f) < 0.01f && öldü && Mathf.Abs(sonrası) < 0.0001f && uzaklaştı > 0.3f, bedel);
            return failed;
        }

        // ── 5) REFİNANS: 4 birim içindekinin havuzunu saniyede %4 doldurur; uzaktakini, boşalmışı ve boss'u değil ──
        static int Refinans(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, clock, player, boss) = Kur(t, MiniBossKind.For(4, 0, Mathf.Max(1, t.subRegionsPerRegion)));
            var yakın = Düşman(t, root, player, EnemyKind.Basic, Bootstrap.ArenaCenter + Vector3.right * 2f);
            var uzak = Düşman(t, root, player, EnemyKind.Basic, Bootstrap.ArenaCenter + Vector3.left * 6f);
            var boş = Düşman(t, root, player, EnemyKind.Basic, Bootstrap.ArenaCenter + Vector3.right * 2f + Vector3.forward);
            var rf = Düşman(t, root, player, EnemyKind.Refinance, Bootstrap.ArenaCenter);
            boss.transform.position = Bootstrap.ArenaCenter + Vector3.forward * 2f;
            float havuz = yakın.TimeLeft;
            foreach (var x in new[] { yakın, uzak }) Havuz(x, havuz * 0.5f);
            Havuz(boş, 0f);
            Havuz(boss, boss.TimeLeft * 0.5f);
            float b0 = boss.TimeLeft;
            int halka0 = Object.FindObjectsByType<Arc>(FindObjectsInactive.Include).Length;
            rf.TickKindPassives(1f);
            bool halka = Object.FindObjectsByType<Arc>(FindObjectsInactive.Include).Length > halka0;
            float yakınDolan = (yakın.TimeLeft - havuz * 0.5f) / havuz, uzakDolan = uzak.TimeLeft - havuz * 0.5f, bossDolan = boss.TimeLeft - b0;
            bool boşKaldı = boş.TimeLeft <= 0f;
            float h0 = clock.TotalHarvested;
            yakın.TakeDamage(yakınDolan * havuz);                                // yenileneni vur: kredi — hasat yok
            float krediHasat = clock.TotalHarvested - h0;
            h0 = clock.TotalHarvested;
            yakın.TakeDamage(0.5f);
            float sonraHasat = clock.TotalHarvested - h0;
            Temizle(root);
            failed += Check(ref total, $"⚠️⚠️ REFİNANS borcu yeniler: 2 birimdekinin havuzu 1 sn'de %{yakınDolan * 100f:0.0} doldu (%4) · 6 birimdeki +{uzakDolan:0.00} · boşalmış boş kaldı ({(boşKaldı ? "evet" : "HAYIR")}) · boss +{bossDolan:0.00} · alanı halkayla görünüyor ({(halka ? "evet" : "HAYIR")}) · yenileneni vurmak hasat vermedi (+{krediHasat:0.000}, sonraki +{sonraHasat:0.000})",
                            Mathf.Abs(yakınDolan - 0.04f) < 0.0001f && Mathf.Abs(uzakDolan) < 0.0001f && boşKaldı && Mathf.Abs(bossDolan) < 0.0001f && halka
                            && Mathf.Abs(krediHasat) < 1e-5f && sonraHasat > 0f, yakınDolan);
            return failed;
        }

        // ── 6) Açılış: MÜHLET ve REFİNANS 5. bölgede, ZAMANAŞIMI hep sonda; tavanlar 4/7/10/14/17 ──
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
            var b4 = Doğan(3); var b5 = Doğan(4);
            Temizle(root);
            failed += Check(ref total, $"⚠️ 5. bölgede {b5.Count} tür ({string.Join(", ", b5.Select(k => k.Name))}) — MÜHLET ve REFİNANS 5. bölgede var, 4. bölgede yok; son tür {EnemyKind.All[EnemyKind.All.Length - 1].Name}; tavanlar {string.Join("/", Region.All.Select(x => x.UnlockedKinds))}",
                            b5.Contains(EnemyKind.Grace) && b5.Contains(EnemyKind.Refinance) && !b4.Contains(EnemyKind.Grace) && !b4.Contains(EnemyKind.Refinance)
                            && b5.Count == EnemyKind.All.Length && EnemyKind.All[EnemyKind.All.Length - 1] == EnemyKind.Lapse
                            && Region.All.Select(x => x.UnlockedKinds).SequenceEqual(new[] { 4, 7, 10, 14, 17 }), b5.Count);
            return failed;
        }

        // ── 7) SARMAL dışa kıvrılır · ZİKZAK yol boyu sağ-sol; 5. bölgenin dövüş odalarında geliyorlar ──
        static int Dizilişler(Tuning t, ref int total)
        {
            int failed = 0, n = 9;
            float R = t.arenaRadius;
            Vector3 merkez = Bootstrap.ArenaCenter + new Vector3(0f, 0f, R * 0.15f);
            var sa = Enumerable.Range(0, n).Select(i => RoomLayout.Spiral.Position(t, i, n)).Select(p => Düz(p - merkez)).ToArray();
            bool sarmal = Enumerable.Range(1, n - 1).All(i => sa[i].magnitude > sa[i - 1].magnitude + 0.01f
                                                          && Mathf.Abs(Vector3.SignedAngle(sa[i - 1], sa[i], Vector3.down) - 75f) < 1f);
            var zi = Enumerable.Range(0, n).Select(i => RoomLayout.Zigzag.Position(t, i, n)).ToArray();
            bool zikzak = Enumerable.Range(0, n).All(i => Mathf.Abs(zi[i].x - Bootstrap.ArenaCenter.x - (i % 2 == 0 ? 1f : -1f) * R * 0.4f) < 0.01f)
                       && Enumerable.Range(1, n - 1).All(i => zi[i].z > zi[i - 1].z);
            var bölge5 = new System.Collections.Generic.HashSet<RoomLayout>(Enumerable.Range(1, t.RoomsInRegion)
                .Where(oda => RoomManager.Tür(t, oda) == RoomManager.OdaTürü.Dövüş).Select(oda => RoomManager.Diziliş(t, 4, oda)));
            failed += Check(ref total, $"⚠️ SARMAL dışa kıvrılır ({(sarmal ? "evet" : "HAYIR")}) · ZİKZAK yol boyu sağ-sol ({(zikzak ? "evet" : "HAYIR")}) · 5. bölgenin dövüş odalarında ikisi de geliyor",
                            sarmal && zikzak && bölge5.Contains(RoomLayout.Spiral) && bölge5.Contains(RoomLayout.Zigzag), sa.Length);
            return failed;
        }

        // ── kurgu ──────────────────────────────────────────────────────────────
        static Vector3 Düz(Vector3 v) { v.y = 0f; return v; }

        static (GameObject, Clock, Transform, Enemy) Kur(Tuning t, BossKind kind)
        {
            Enemy.All.Clear();
            RevivalSystem.Reset();
            var root = new GameObject("faz38-check");
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
