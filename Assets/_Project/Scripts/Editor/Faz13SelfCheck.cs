using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 13 — düşman kadrosu. Korunan kurallar:
    ///
    ///   • ⚠️ Yedi tür var ve her biri FARKLI BİR SORU soruyor (ad farkı sahte çeşitlilik)
    ///   • ⚠️ SİGORTA yakınındakilerin hasadını GERÇEKTEN kısıyor — ölünce düzeliyor
    ///   • ⚠️ MÜBAŞİR mesafesini koruyor ve UZAKTAN vuruyor
    ///   • ⚠️ ZAMANAŞIMI yaşadıkça güçleniyor — ama TAVANLI
    /// </summary>
    public static class Faz13SelfCheck
    {
        [MenuItem("Tek Can/Faz 13 öz-denetimi çalıştır")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 5000f;
            t.enemyTimePool = 10f;
            t.harvestRate = 0.5f;
            t.enemyDecayPerSecond = 0f;
            Region.Reset();

            // ── 1) Kadro tam ve türler birbirinden FARKLI ──
            failed += Check(ref total, "⚠️ YEDİ düşman türü var (tasarım hedefi)",
                            EnemyKind.All.Length >= 7, EnemyKind.All.Length);

            int aynı = 0, adsız = 0;
            for (int a = 0; a < EnemyKind.All.Length; a++)
            {
                if (string.IsNullOrWhiteSpace(EnemyKind.All[a].Name)) adsız++;
                for (int b = a + 1; b < EnemyKind.All.Length; b++)
                    if (Soru(EnemyKind.All[a], t) == Soru(EnemyKind.All[b], t)) aynı++;
            }
            failed += Check(ref total, "her türün adı var", adsız == 0, adsız);
            failed += Check(ref total, "⚠️ türler FARKLI SORU soruyor (ad farkı yetmez)",
                            aynı == 0, aynı);

            // ── 2) ⚠️ SİGORTA hasadı GERÇEKTEN kısıyor — ETKİ ──
            var (root2, clock2) = Kur(t);
            var kurban = Make(t, root2.transform, EnemyKind.Basic, Bootstrap.ArenaCenter);
            var sigorta = Make(t, root2.transform, EnemyKind.Insurer,
                               Bootstrap.ArenaCenter + Vector3.right * 2f);

            float önce = clock2.Seconds;
            kurban.TakeDamage(3f);
            float sigortalı = clock2.Seconds - önce;

            Object.DestroyImmediate(sigorta.gameObject);
            Enemy.All.Remove(sigorta);

            var kurban2 = Make(t, root2.transform, EnemyKind.Basic, Bootstrap.ArenaCenter);
            önce = clock2.Seconds;
            kurban2.TakeDamage(3f);
            float sigortasız = clock2.Seconds - önce;

            failed += Check(ref total, "⚠️ SİGORTA yaşarken hasat DÜŞÜK",
                            sigortalı < sigortasız - 0.01f, sigortasız - sigortalı);
            failed += Check(ref total, "⚠️ SİGORTA ölünce hasat DÜZELİYOR",
                            sigortasız > 0.1f, sigortasız);
            Temizle(root2);

            // ── 3) SİGORTA menzil DIŞINDAKİNİ korumuyor ──
            var (root3, clock3) = Kur(t);
            var uzak = Make(t, root3.transform, EnemyKind.Basic,
                            Bootstrap.ArenaCenter + Vector3.right * (t.insurerRadius + 4f));
            Make(t, root3.transform, EnemyKind.Insurer, Bootstrap.ArenaCenter);

            önce = clock3.Seconds;
            uzak.TakeDamage(3f);
            float uzaktaki = clock3.Seconds - önce;
            failed += Check(ref total, "menzil DIŞINDAKİ korunmuyor (koruma sınırlı)",
                            uzaktaki > 0.1f, uzaktaki);
            Temizle(root3);

            // ── 4) ⚠️ MÜBAŞİR uzaktan vuruyor ve mesafe koruyor ──
            failed += Check(ref total, "MÜBAŞİR mesafesini koruyor",
                            EnemyKind.Bailiff.PreferredRange(t) > t.enemyAttackRange * 2f,
                            EnemyKind.Bailiff.PreferredRange(t));

            var (root4, clock4) = Kur(t);
            var mübaşir = Make(t, root4.transform, EnemyKind.Bailiff,
                               Bootstrap.ArenaCenter + Vector3.forward * 8f);
            int öncekiİşaret = MarkCount();
            mübaşir.TickKindPassives(t.bailiffMarkInterval + 0.1f);
            failed += Check(ref total, "⚠️ MÜBAŞİR UZAKTAN tebligat bırakıyor",
                            MarkCount() > öncekiİşaret, MarkCount() - öncekiİşaret);

            // Diğer türler bırakmıyor — kimlik dağılmasın
            var temel = Make(t, root4.transform, EnemyKind.Basic, Bootstrap.ArenaCenter);
            int aradaki = MarkCount();
            temel.TickKindPassives(5f);
            failed += Check(ref total, "tebligat MÜBAŞİR'e özel (kimlik dağılmıyor)",
                            MarkCount() == aradaki, MarkCount() - aradaki);
            Temizle(root4);

            // ── 5) ⚠️ ZAMANAŞIMI yaşadıkça GÜÇLENİYOR — ama TAVANLI ──
            var (root5, clock5) = Kur(t);
            var lapse = Make(t, root5.transform, EnemyKind.Lapse, Bootstrap.ArenaCenter);

            float başlangıç = lapse.Growth;
            lapse.TickKindPassives(5f);
            float beşSaniye = lapse.Growth;
            failed += Check(ref total, "⚠️ ZAMANAŞIMI zamanla GÜÇLENİYOR",
                            beşSaniye > başlangıç + 0.05f, beşSaniye - başlangıç);

            lapse.TickKindPassives(600f);          // çok uzun süre
            failed += Check(ref total, "⚠️ büyüme TAVANLI (tek düşman koşuyu bitiremiyor)",
                            lapse.Growth <= t.lapseGrowthCap + 0.001f, lapse.Growth);

            var temel5 = Make(t, root5.transform, EnemyKind.Basic, Bootstrap.ArenaCenter);
            temel5.TickKindPassives(60f);
            failed += Check(ref total, "büyüme ZAMANAŞIMI'na özel",
                            Mathf.Approximately(temel5.Growth, 1f), temel5.Growth);
            Temizle(root5);

            // ── 6) ⚠️ HER TÜR GERÇEKTEN DOĞUYOR MU ──
            //    Faz 13'te üç tür eklendi ama `Region.UnlockedKinds` tavanı 6'da
            //    kalmıştı: yedinci tür (ZAMANAŞIMI) oyunda HİÇ DOĞMUYORDU.
            //    ⚠️ Tavan sayısına bakmak yetmez — üretimin seçicisini ÇALIŞTIRIP
            //    hangi türlerin çıktığını saymak lazım. "Tanımlı" ≠ "doğuyor".
            var çıkanlar = new System.Collections.Generic.HashSet<EnemyKind>();
            var (root6, clock6) = Kur(t);
            var rm = root6.AddComponent<RoomManager>();
            rm.Init(t, root6.transform.Find("player"), null);

            var pick = typeof(RoomManager).GetMethod("PickKind",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var enterRegion = typeof(RoomManager).GetMethod("EnterRegion",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            for (int bölge = 0; bölge < Region.All.Length; bölge++)
            {
                enterRegion.Invoke(rm, new object[] { bölge, 1 });
                for (int oda = 1; oda <= t.roomsPerRegion; oda++)
                    for (int i = 0; i < 12; i++)
                        çıkanlar.Add((EnemyKind)pick.Invoke(rm, new object[] { oda, i }));
            }

            failed += Check(ref total, "⚠️ HER TÜR gerçekten DOĞUYOR (tanımlı ≠ doğuyor)",
                            çıkanlar.Count >= EnemyKind.All.Length, çıkanlar.Count);

            if (çıkanlar.Count < EnemyKind.All.Length)
                foreach (var k in EnemyKind.All)
                    if (!çıkanlar.Contains(k)) Debug.LogError($"     hiç doğmayan tür: {k.Name}");

            Temizle(root6);
            RoomManager.Reset();

            // ── 7) Kamera kilidi — asset kapısı maddesi ──
            failed += Check(ref total, "kamera eğimi makul (izometrik aralık)",
                            t.camPitch > 30f && t.camPitch < 70f, t.camPitch);
            float kısalma = Mathf.Cos(t.camPitch * Mathf.Deg2Rad);
            float piksel = 2f * kısalma * (1080f / (t.camOrthoSize * 2f));
            failed += Check(ref total, $"1080p'de karakter okunur boyda ({piksel:0} piksel)",
                            piksel > 40f && piksel < 400f, piksel);

            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset(); Enemy.All.Clear();

            if (failed == 0) Debug.Log($"[Faz 13 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 13 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        /// <summary>
        /// Türün SORDUĞU SORU — adı değil. İki tür farklı ad taşıyıp aynı soruyu
        /// soruyorsa çeşitlilik sahtedir; `BossKind` denetiminde de aynı ölçü kullanılıyor.
        /// </summary>
        static string Soru(EnemyKind k, Tuning t) =>
            $"aura={k.AuraDrain(t) > 0f}|hasat={k.HarvestMultiplier:0.0}|kaçar={k.FleesBelowRatio > 0f}" +
            $"|korur={k.ProtectRadius(t) > 0f}|menzilli={k.RangedMarkInterval(t) > 0f}" +
            $"|büyür={k.GrowthPerSecond > 0f}";

        static int MarkCount() =>
            Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include).Length;

        static (GameObject, Clock) Kur(Tuning t)
        {
            Enemy.All.Clear();
            var root = new GameObject("faz13-check");
            var player = new GameObject("player"); player.transform.SetParent(root.transform);
            player.transform.position = Bootstrap.ArenaCenter;
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, t.startSeconds);
            return (root, clock);
        }

        static Enemy Make(Tuning t, Transform parent, EnemyKind kind, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(t, parent.Find("player"), null, 1f, false, kind);
            return e;
        }

        static void Temizle(GameObject root)
        {
            foreach (var e in Enemy.All.ToArray())
                if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            foreach (var m in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include))
                Object.DestroyImmediate(m.gameObject);
            Object.DestroyImmediate(root);
            Clock.Reset();
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
