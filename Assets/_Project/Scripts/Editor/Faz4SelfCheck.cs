using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 4 — koşu yapısı. Korunan kurallar:
    ///   • Dalga temizlenince ARA verilir, koşu bitmez
    ///   • Dalgalar sertleşir (daha çok düşman, daha büyük havuz)
    ///   • Boss beklenerek yenilemez (akışı çok yavaş) ve saati emer
    ///   • ⚠️ BOSS ÖLÜNCE KOŞU GERÇEKTEN KAZANILIR
    ///
    /// Sonuncusu en önemlisi: DURGUN dersinden sonra her yeni oynanış nesnesinin
    /// ilk assert'i "hiç çalışıyor mu" olmak zorunda. Burada o soru = kazanılabiliyor mu.
    /// </summary>
    public static class Faz4SelfCheck
    {
        [MenuItem("Tek Can/Faz 4 öz-denetimi çalıştır")]
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
            t.waveCount = 3;                 // kısa koşu: 1, 2, sonra boss
            t.waveBaseCount = 2f;
            t.waveCountGrowth = 1f;
            t.enemyTimePool = 10f;

            var root = new GameObject("faz4-check");
            var player = new GameObject("player");
            player.transform.position = Bootstrap.PlayerSpawn;

            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform);

            Enemy.All.Clear();
            var wm = root.AddComponent<WaveManager>();
            wm.Init(t, player.transform, null);

            // 1) Başlangıç: ara evresinde, hiç düşman yok
            failed += Check(ref total, "koşu ARA evresinde başlıyor",
                            wm.Current == WaveManager.Phase.Intermission, (int)wm.Current);
            failed += Check(ref total, "başlangıçta düşman yok", Enemy.All.Count == 0, Enemy.All.Count);

            // 2) İlk dalga doğuyor
            wm.StartNextWave();
            int w1 = Enemy.All.Count;
            failed += Check(ref total, "dalga 1 düşman doğurdu", w1 > 0, w1);
            failed += Check(ref total, "dövüş evresine geçti",
                            wm.Current == WaveManager.Phase.Fighting, (int)wm.Current);
            float pool1 = Enemy.All[0].TimeLeft;

            // 3) Dalgalar sertleşiyor
            KillAll();
            wm.StartNextWave();
            int w2 = Enemy.All.Count;
            float pool2 = Enemy.All[0].TimeLeft;
            failed += Check(ref total, "dalga 2 daha kalabalık", w2 > w1, w2 - w1);
            failed += Check(ref total, "dalga 2 düşmanı daha dayanıklı", pool2 > pool1, pool2 - pool1);

            // 4) Boss dalgası
            KillAll();
            wm.StartNextWave();
            failed += Check(ref total, "3. dalga BOSS dalgası", wm.IsBossWave, wm.Wave);
            var boss = Enemy.All.Count > 0 ? Enemy.All[0] : null;
            failed += Check(ref total, "boss doğdu", boss != null && boss.IsBoss, Enemy.All.Count);
            failed += Check(ref total, "boss çok daha dayanıklı",
                            boss != null && boss.TimeLeft > pool2 * 3f, boss != null ? boss.TimeLeft : 0f);

            // 5) ⚠️ BOSS ÖLÜNCE KOŞU KAZANILIYOR MU — fazın varlık sebebi
            failed += Check(ref total, "boss ölmeden koşu kazanılmamış", !clock.Won, clock.Won ? 1 : 0);

            bool winFired = false;
            clock.OnWin += () => winFired = true;

            KillAll();
            typeof(WaveManager).GetMethod("Update",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(wm, null);

            failed += Check(ref total, "BOSS ÖLÜNCE KOŞU KAZANILDI", clock.Won, clock.Won ? 1 : 0);
            failed += Check(ref total, "kazanma olayı tetiklendi", winFired, winFired ? 1 : 0);
            failed += Check(ref total, "kazanınca ölü sayılmıyor", !clock.Dead, clock.Dead ? 1 : 0);

            // 6) Boss beklenerek yenilemez: akışı normalden çok yavaş
            failed += Check(ref total, "boss akışı yavaşlatılmış",
                            t.bossDecayMultiplier < 0.5f, t.bossDecayMultiplier);
            failed += Check(ref total, "boss saati emiyor", t.bossAuraDrain > 0f, t.bossAuraDrain);

            KillAll();
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(t);

            if (failed == 0) Debug.Log($"[Faz 4 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 4 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        static void KillAll()
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
