using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// "Hasar inmiyor" sorununu Enes'i beklemeden aramak için: Bootstrap'in KURDUĞU
    /// gerçek geometriyi birebir kurar (y=1, düşman ölçeği 0.9, gerçek spawn yarıçapı),
    /// düşmanları kovalama mantığıyla yaklaştırır, sonra Strike'ı ateşler ve raporlar.
    ///
    /// İzole testler geçiyordu çünkü ideal koordinatlar kullanıyorlardı (y=0, tam önde).
    /// Burada oyunun kendi sayıları var.
    /// </summary>
    public static class Faz1Repro
    {
        [MenuItem("Tek Can/Faz 1 hasar reprodüksiyonu")]
        public static void Run()
        {
            var t = AssetDatabase.LoadAssetAtPath<Tuning>("Assets/_Project/Resources/Tuning.asset")
                    ?? ScriptableObject.CreateInstance<Tuning>();

            Enemy.All.Clear();
            var root = new GameObject("repro");

            // ── Bootstrap ile BİREBİR aynı kurulum ──
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.transform.SetParent(root.transform);
            player.transform.position = Bootstrap.PlayerSpawn;        // ← oyunun kendi yerleşiminden

            var clockGo = new GameObject("clock"); clockGo.transform.SetParent(root.transform);
            var clock = clockGo.AddComponent<Clock>();
            clock.Init(t, player.transform);

            var pc = player.AddComponent<PlayerController>();
            pc.Init(t, null);

            for (int i = 0; i < t.enemyCount; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.SetParent(root.transform);
                go.transform.position = Bootstrap.EnemySpawnPosition(t, i);   // ← tek kaynak
                go.transform.localScale = Bootstrap.EnemyScale;
                go.AddComponent<Enemy>().Init(t, player.transform, null);
            }

            Debug.Log($"[REPRO] kurulum: {Enemy.All.Count} düşman, " +
                      $"ilk spawn mesafesi {Bootstrap.EnemySpawnPosition(t, 0).magnitude:0.00}");

            // ── Kovalamayı simüle et: ChaseMove ile AYNI matematik ──
            foreach (var e in Enemy.All)
            {
                Vector3 to = player.transform.position - e.transform.position;
                to.y = 0f;
                float stop = t.enemyAttackRange * 0.9f;
                if (to.magnitude > stop)
                    e.transform.position += to.normalized * (to.magnitude - stop);
            }

            var nearest = Enemy.All[0];
            float d0 = Vector3.Distance(
                new Vector3(nearest.transform.position.x, 0f, nearest.transform.position.z),
                new Vector3(player.transform.position.x, 0f, player.transform.position.z));
            Debug.Log($"[REPRO] kovalama sonrası en yakın düşman mesafesi = {d0:0.000} " +
                      $"(saldırı menzili {t.attackRange})");

            // ── Oyuncuyu en yakın düşmana döndür (imleçle nişan almış gibi) ──
            Vector3 aim = nearest.transform.position - player.transform.position;
            aim.y = 0f;
            player.transform.rotation = Quaternion.LookRotation(aim.normalized);

            float before = clock.Seconds;
            float poolBefore = nearest.TimeLeft;

            typeof(PlayerController).GetMethod("Strike", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(pc, null);

            Debug.Log($"[REPRO] SONUÇ: savurma={pc.StrikesFired} isabet={pc.HitsLanded} | " +
                      $"düşman havuzu {poolBefore:0.00} -> {nearest.TimeLeft:0.00} | " +
                      $"saat {before:0.00} -> {clock.Seconds:0.00}");
            Debug.Log($"[REPRO] rapor: {pc.LastStrikeReport}");

            bool ok = pc.HitsLanded > 0 && clock.Seconds > before;
            Debug.Log(ok
                ? "[REPRO] ✅ GERÇEK GEOMETRİDE HASAR İNİYOR — sorun Strike'ta değil, ona giden yolda"
                : "[REPRO] ❌ GERÇEK GEOMETRİDE HASAR İNMİYOR — sebep burada");

            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            Object.DestroyImmediate(root);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
