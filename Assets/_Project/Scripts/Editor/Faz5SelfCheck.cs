using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 5 — düşman türleri. Korunan kurallar:
    ///   • ⚠️ HER TÜR GERÇEKTEN ÇALIŞIYOR MU (DURGUN dersi: önce en aptal soru)
    ///   • SIZINTI yaşadığı sürece saati emiyor
    ///   • KASA vurarak ödemiyor, ÖLDÜĞÜNDE tek seferde ödüyor
    ///   • KAÇAK eşiğin altında kaçmaya başlıyor ve kaçarsa hiçbir şey ödemiyor
    ///   • Türler kademeli açılıyor (ilk dalgalar temel)
    /// </summary>
    public static class Faz5SelfCheck
    {
        [MenuItem("Tek Can/Faz 5 öz-denetimi çalıştır")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 500f;
            t.enemyTimePool = 10f;
            t.harvestRate = 0.35f;
            t.enemyDecayPerSecond = 0f;      // akış testi bulandırmasın

            var root = new GameObject("faz5-check");
            var player = new GameObject("player");
            player.transform.position = Bootstrap.PlayerSpawn;
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform);

            // ── 1) ⚠️ HER TÜR ÇALIŞIYOR MU — en aptal soru önce ──
            foreach (var kind in EnemyKind.All)
            {
                Clear();
                var e = Make(t, player.transform, Bootstrap.PlayerSpawn + Vector3.forward * 1.2f, kind);
                float before = e.TimeLeft;
                e.TakeDamage(3f);
                failed += Check(ref total, $"{kind.Name} hasar alabiliyor",
                                e.TimeLeft < before - 0.001f, before - e.TimeLeft);
            }

            // ── 2) SIZINTI saati emiyor ──
            failed += Check(ref total, "SIZINTI aura emişi tanımlı",
                            EnemyKind.Leak.AuraDrain(t) > 0f, EnemyKind.Leak.AuraDrain(t));
            failed += Check(ref total, "TEMEL emmiyor",
                            EnemyKind.Basic.AuraDrain(t) == 0f, EnemyKind.Basic.AuraDrain(t));

            // ── 3) KASA vurarak ÖDEMİYOR ──
            Clear();
            var vault = Make(t, player.transform, Bootstrap.PlayerSpawn + Vector3.forward * 1.2f, EnemyKind.Vault);
            float clockBefore = clock.Seconds;
            vault.TakeDamage(5f);
            failed += Check(ref total, "KASA vurarak HİÇBİR ŞEY ödemedi",
                            Mathf.Approximately(clock.Seconds, clockBefore), clock.Seconds - clockBefore);

            // ── 4) ...ama ÖLDÜĞÜNDE tek seferde ödüyor ──
            clockBefore = clock.Seconds;
            while (!vault.Hollow) vault.TakeDamage(50f);
            failed += Check(ref total, "KASA boşalırken de ödemedi",
                            Mathf.Approximately(clock.Seconds, clockBefore), clock.Seconds - clockBefore);

            clockBefore = clock.Seconds;
            vault.TakeDamage(1f);                      // öldürücü darbe
            failed += Check(ref total, "KASA ÖLÜNCE tek seferde ödedi",
                            clock.Seconds > clockBefore + 1f, clock.Seconds - clockBefore);

            // ── 5) Temel düşman tam tersi: vurarak öder ──
            Clear();
            var basic = Make(t, player.transform, Bootstrap.PlayerSpawn + Vector3.forward * 1.2f, EnemyKind.Basic);
            clockBefore = clock.Seconds;
            basic.TakeDamage(5f);
            failed += Check(ref total, "TEMEL vurarak ödüyor (kıyas)",
                            clock.Seconds > clockBefore, clock.Seconds - clockBefore);

            // ── 6) KAÇAK eşiğin altında kaçmaya başlıyor ──
            Clear();
            var runner = Make(t, player.transform, Bootstrap.PlayerSpawn + Vector3.forward * 1.2f, EnemyKind.Runner);
            failed += Check(ref total, "KAÇAK başta kaçmıyor", !runner.Fleeing, 0);

            float threshold = runner.TimeLeft * (1f - EnemyKind.Runner.FleesBelowRatio) + 0.2f;
            runner.TakeDamage(threshold);
            Tick(runner);
            failed += Check(ref total, "KAÇAK eşiğin altında KAÇMAYA BAŞLADI", runner.Fleeing, 1);
            failed += Check(ref total, "TEMEL hiç kaçmaz", EnemyKind.Basic.FleesBelowRatio == 0f, 0);

            // ── 7) Türler kademeli açılıyor ──
            Clear();
            var wm = root.AddComponent<WaveManager>();
            wm.Init(t, player.transform, null);
            var pick = typeof(WaveManager).GetMethod("PickKind",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            bool ilkDalgaHepTemel = true;
            for (int i = 0; i < 8; i++)
                if ((EnemyKind)pick.Invoke(wm, new object[] { 1, i }) != EnemyKind.Basic) ilkDalgaHepTemel = false;
            failed += Check(ref total, "1. dalga yalnızca TEMEL (oyuncu önce öğrensin)", ilkDalgaHepTemel, 1);

            var geçDalga = new System.Collections.Generic.HashSet<EnemyKind>();
            for (int i = 0; i < 8; i++)
                geçDalga.Add((EnemyKind)pick.Invoke(wm, new object[] { 5, i }));
            failed += Check(ref total, "geç dalgada birden çok tür karışıyor", geçDalga.Count > 1, geçDalga.Count);

            Clear();
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(t);

            if (failed == 0) Debug.Log($"[Faz 5 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 5 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        static Enemy Make(Tuning t, Transform player, Vector3 pos, EnemyKind kind)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = pos;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(t, player, null, 1f, false, kind);
            return e;
        }

        /// <summary>Enemy.Update'i bir kez elle çevir — kaçma kararı orada veriliyor.</summary>
        static void Tick(Enemy e) =>
            typeof(Enemy).GetMethod("Update",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(e, null);

        static void Clear()
        {
            foreach (var e in Enemy.All.ToArray())
                if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
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
