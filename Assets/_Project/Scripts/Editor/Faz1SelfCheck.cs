using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 1'in çekirdek kuralını doğrular. Kırılırsa oyun tasarımı kırılmış demektir.
    ///
    /// Kural: hasar havuzu düşürür → düşen miktarın hasat oranı kadarı saate yazılır.
    ///        havuz sıfırlanınca düşman ÖLMEZ, boşalır. Boş düşman tek atımda ölür
    ///        ve hiçbir şey ödemez.
    ///
    /// Çalıştır: Unity.exe -batchmode -quit -executeMethod TekCan.EditorTools.Faz1SelfCheck.Run
    /// ponytail: test çerçevesi paketi eklemedik — assert yeter.
    /// </summary>
    public static class Faz1SelfCheck
    {
        [MenuItem("Tek Can/Faz 1 öz-denetimi çalıştır")]
        public static void Run()
        {
            int failed = 0, total = 0;
            var t = ScriptableObject.CreateInstance<Tuning>();
            t.enemyTimePool = 10f;
            t.harvestRate = 0.40f;
            t.startSeconds = 100f;

            var root = new GameObject("selfcheck");
            var clock = root.AddComponent<Clock>();
            clock.Init(t, root.transform);

            var enemyGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var labelGo = new GameObject("label");
            labelGo.transform.SetParent(enemyGo.transform, false);
            var label = labelGo.AddComponent<TimeLabel>();
            label.Init(root.transform);
            var enemy = enemyGo.AddComponent<Enemy>();
            enemy.Init(t, root.transform, label);

            float before = clock.Seconds;

            // 1) Kısmi hasar: havuz düşer, hasat oranı kadarı saate yazılır
            enemy.TakeDamage(4f);
            failed += Check(ref total, "havuz 4 birim düştü", Mathf.Approximately(enemy.TimeLeft, 6f), enemy.TimeLeft);
            failed += Check(ref total, "saate 4*0.40=1.6 yazıldı", Mathf.Abs(clock.Seconds - (before + 1.6f)) < 0.001f, clock.Seconds - before);
            failed += Check(ref total, "henüz boşalmadı", !enemy.Hollow, enemy.Hollow ? 1 : 0);

            // 2) Aşırı hasar: havuzdan fazlası hasat EDİLMEZ, düşman boşalır ama ÖLMEZ
            before = clock.Seconds;
            enemy.TakeDamage(999f);
            failed += Check(ref total, "havuz sıfırlandı", Mathf.Approximately(enemy.TimeLeft, 0f), enemy.TimeLeft);
            failed += Check(ref total, "sadece kalan 6 hasat edildi (6*0.40=2.4)", Mathf.Abs(clock.Seconds - (before + 2.4f)) < 0.001f, clock.Seconds - before);
            failed += Check(ref total, "BOŞALDI ama ölmedi", enemy.Hollow && enemy != null, enemy.Hollow ? 1 : 0);
            failed += Check(ref total, "hâlâ listede", Enemy.All.Contains(enemy), Enemy.All.Count);

            // 3) Boş düşmana vuruş: tek atımda ölür, hiçbir şey ödemez
            before = clock.Seconds;
            enemy.TakeDamage(1f);
            failed += Check(ref total, "boş düşman tek atımda öldü", Enemy.All.Count == 0, Enemy.All.Count);
            failed += Check(ref total, "boş düşman hiçbir şey ödemedi", Mathf.Approximately(clock.Seconds, before), clock.Seconds - before);

            // 4) Hasat tavanı aşılamaz
            t.harvestRateCap = 0.70f;
            clock.UpgradeHarvest(5f);
            failed += Check(ref total, "hasat oranı tavanı aşmadı", Mathf.Approximately(clock.HarvestRate, 0.70f), clock.HarvestRate);

            Object.DestroyImmediate(root);
            Enemy.All.Clear();

            if (failed == 0) Debug.Log($"[Faz 1 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 1 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");

            if (Application.isBatchMode) EditorApplication.Exit(failed == 0 ? 0 : 1);
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
