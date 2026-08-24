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

            // 5) GERİ BİLDİRİM — ilk playtest'i öldüren hata tam buydu.
            //    Saldırı ıskalasa bile görünür bir yay çıkmak ZORUNDA, yoksa
            //    oyuncu saldırdığını göremez ve hissi yargılayamaz.
            var pGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var pc = pGo.AddComponent<PlayerController>();
            pc.Init(t, null);
            int arcsBefore = Object.FindObjectsByType<Arc>(FindObjectsInactive.Include).Length;
            typeof(PlayerController).GetMethod("StartAttack",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(pc, new object[] { Vector3.forward });
            int arcsAfter = Object.FindObjectsByType<Arc>(FindObjectsInactive.Include).Length;

            failed += Check(ref total, "ISKADA BİLE savurma yayı üretildi", arcsAfter == arcsBefore + 1, arcsAfter - arcsBefore);
            failed += Check(ref total, "savurma sayacı arttı", pc.SwingsSpawned == 1, pc.SwingsSpawned);

            // 6) Düşman HAZIRLIK evresinde hasar vermez — hasar yalnızca vuruş anında iner
            var e2Go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var e2 = e2Go.AddComponent<Enemy>();
            e2.Init(t, pGo.transform, null);
            typeof(Enemy).GetMethod("EnterWindup",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(e2, null);
            float clockBeforeWindup = clock.Seconds;
            failed += Check(ref total, "hazırlık evresine girdi", e2.CurrentState == Enemy.State.Windup, (int)e2.CurrentState);
            failed += Check(ref total, "HAZIRLIK hasar VERMEDİ (kaçış penceresi var)", Mathf.Approximately(clock.Seconds, clockBeforeWindup), clock.Seconds - clockBeforeWindup);

            // 7) ⚠️ GERÇEK YOL: Strike() üzerinden hasar iniyor mu?
            //    Eski testler TakeDamage'i doğrudan çağırıyordu ve bu yüzden oyun
            //    kırıkken bile yeşil yanıyordu. Artık oyuncunun bastığı yoldan geçiyoruz.
            pGo.transform.position = Vector3.zero;
            pGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);

            // Önceki bölümlerden kalan düşmanları sahneden çıkar — yoksa Strike onları da
            // yakalar ve sayım yanıltır (ilk denemede tam bunu yaşadım).
            foreach (var leftover in Enemy.All.ToArray()) Object.DestroyImmediate(leftover.gameObject);
            Enemy.All.Clear();

            var e3Go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            e3Go.transform.position = new Vector3(0f, 0f, 1.6f);   // tam önde, menzil içinde
            var e3 = e3Go.AddComponent<Enemy>();
            e3.Init(t, pGo.transform, null);

            float clockBeforeStrike = clock.Seconds;
            float poolBefore = e3.TimeLeft;
            typeof(PlayerController).GetMethod("Strike",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(pc, null);

            failed += Check(ref total, "STRIKE önündeki düşmana hasar verdi", e3.TimeLeft < poolBefore, e3.TimeLeft);
            failed += Check(ref total, "STRIKE saate zaman yazdı", clock.Seconds > clockBeforeStrike, clock.Seconds - clockBeforeStrike);

            // 8) Boş düşman Strike ile TEK ATIMDA ölüyor mu (Enes: "gri olunca ölümsüz")
            while (!e3.Hollow) e3.TakeDamage(50f);
            int countBefore = Enemy.All.Count;
            typeof(PlayerController).GetMethod("Strike",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(pc, null);
            failed += Check(ref total, "BOŞ düşman Strike ile öldü (ölümsüz değil)", Enemy.All.Count == countBefore - 1, Enemy.All.Count);

            // 9) Saldırı sırasında hareket etmek oyuncuyu DÖNDÜRMEMELİ.
            //    Nişan StartAttack'te kilitleniyor ama vuruş ~0.1 sn sonra iniyor;
            //    arada dönerse yay bir yere, isabet başka yere gider.
            pGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var facingBefore = pGo.transform.forward;
            typeof(PlayerController).GetMethod("Move",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(pc, new object[] { Vector3.right, 1f, false });

            failed += Check(ref total, "saldırıda hareket DÖNDÜRMÜYOR",
                Vector3.Angle(facingBefore, pGo.transform.forward) < 0.01f,
                Vector3.Angle(facingBefore, pGo.transform.forward));

            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include)) Object.DestroyImmediate(a.gameObject);
            if (e3Go != null) Object.DestroyImmediate(e3Go);
            if (e2Go != null) Object.DestroyImmediate(e2Go);
            Object.DestroyImmediate(pGo);
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
