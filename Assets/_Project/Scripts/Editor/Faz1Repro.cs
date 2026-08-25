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
            bool ok = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>Hasar iniyorsa true. Toplu koşucu bunu kullanıyor.</summary>
        public static bool Execute()
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

            // ⚠️ Düşmanları OYUNUN kendi yolundan doğur: WaveManager → Bootstrap.SpawnEnemyAt.
            // Elle doğursaydık, Faz 4'te olduğu gibi oyun kendi formülüne kayınca reprodüksiyon
            // sessizce ölü bir yolu sınamaya devam ederdi.
            var wmGo = new GameObject("wave"); wmGo.transform.SetParent(root.transform);
            var wm = wmGo.AddComponent<WaveManager>();
            wm.Init(t, player.transform, null);
            wm.StartNextWave();

            Debug.Log($"[REPRO] kurulum: dalga {wm.Wave}, {Enemy.All.Count} düşman, " +
                      $"ilk spawn mesafesi {Enemy.All[0].transform.position.magnitude:0.00}");

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

            // Sayaçlara değil SONUCA bakılıyor: havuz eridi mi, saat arttı mı.
            Debug.Log($"[REPRO] SONUÇ: düşman havuzu {poolBefore:0.00} -> {nearest.TimeLeft:0.00} | " +
                      $"saat {before:0.00} -> {clock.Seconds:0.00}");

            bool ok = nearest.TimeLeft < poolBefore && clock.Seconds > before;
            // Regresyon kapısı: düşmanların havada doğduğu hata buradan geçmişti.
            // Gerçek spawn geometrisiyle hasarın indiğini her seferinde kanıtlar.
            Debug.Log(ok
                ? "[REPRO] ✅ Gerçek geometride hasar iniyor ve saat artıyor"
                : "[REPRO] ❌ GERÇEK GEOMETRİDE HASAR İNMİYOR — regresyon");

            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            Object.DestroyImmediate(root);
            return ok;
        }
    }
}
