using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 3 — silahlar. Korunan kurallar:
    ///   • TEK HEDEFLİ silah, koni içinde üç düşman varken bile YALNIZCA BİRİNE vurur
    ///   • Hasat çarpanı gerçekten çarpıyor (tek hedeflinin rekabet edebilmesinin TEK sebebi)
    ///   • FAİZLİ ödeyemiyorsan savurmaz ve savurunca peşin keser
    ///   • VADELİ bedeli vuruştan SONRA keser
    ///   • DURGUN basılı tutulunca saati dondurur
    ///
    /// Test düzeneği üretim yerleşimini ithal ediyor (Bootstrap.PlayerSpawn) — lessons.md.
    /// </summary>
    public static class Faz3SelfCheck
    {
        [MenuItem("Tek Can/Faz 3 öz-denetimi çalıştır")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 100f;
            t.harvestRate = 0.35f;
            t.enemyTimePool = 100f;      // tek vuruşta boşalmasın, hasat ölçülebilsin
            t.attackDamage = 4f;

            var root = new GameObject("faz3-check");
            var clock = root.AddComponent<Clock>();
            clock.Init(t, root.transform);

            var pGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pGo.transform.position = Bootstrap.PlayerSpawn;
            pGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var pc = pGo.AddComponent<PlayerController>();
            pc.Init(t, null);

            var strike = typeof(PlayerController).GetMethod("Strike",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // ── 1) KONİ üç düşmanın üçüne birden vurur ──
            SpawnThree(t, pGo.transform);
            pc.SetWeapon(0);
            failed += Check(ref total, "0 numara KONİ", pc.Weapon is ConeBlade, 0);

            var pools = Snapshot();
            strike.Invoke(pc, null);
            int coneHits = CountDamaged(pools);
            failed += Check(ref total, "KONİ üç düşmana birden vurdu", coneHits == 3, coneHits);

            // ── 2) TEK HEDEFLİ yalnızca birine vurur ──
            ClearEnemies();
            SpawnThree(t, pGo.transform);
            pc.SetWeapon(1);
            failed += Check(ref total, "1 numara FAİZLİ", pc.Weapon is Faizli, 1);
            failed += Check(ref total, "FAİZLİ tek hedefli", pc.Weapon.MaxTargets == 1, pc.Weapon.MaxTargets);

            pools = Snapshot();
            strike.Invoke(pc, null);
            int singleHits = CountDamaged(pools);
            failed += Check(ref total, "TEK HEDEFLİ üç düşmandan SADECE BİRİNE vurdu", singleHits == 1, singleHits);

            // ── 3) Hasat çarpanı gerçekten çarpıyor ──
            //    Tek hedeflinin koniyle rekabet edebilmesinin TEK sebebi bu.
            ClearEnemies();
            var solo = MakeEnemy(t, pGo.transform, Bootstrap.PlayerSpawn + Vector3.forward * 1.2f);

            pc.SetWeapon(0);                                   // KONİ, çarpan 1.0
            float before = clock.Seconds;
            strike.Invoke(pc, null);
            float coneGain = clock.Seconds - before;

            ClearEnemies();
            solo = MakeEnemy(t, pGo.transform, Bootstrap.PlayerSpawn + Vector3.forward * 1.2f);
            pc.SetWeapon(1);                                   // FAİZLİ, çarpan 2.6
            before = clock.Seconds;
            strike.Invoke(pc, null);
            float faizliGain = clock.Seconds - before + t.faizliSwingCost;   // peşin bedeli geri ekle

            failed += Check(ref total, "FAİZLİ aynı hasarda KONİ'den çok hasat etti",
                            faizliGain > coneGain * 1.5f, faizliGain - coneGain);

            // ── 4) FAİZLİ: ödeyemiyorsan savurmaz ──
            var swingCostField = typeof(Clock).GetProperty("Seconds");
            var swingStart = typeof(PlayerController).GetMethod("StartAttack",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var poorT = ScriptableObject.CreateInstance<Tuning>();
            poorT.startSeconds = 0.2f;                          // bedelden az
            poorT.faizliSwingCost = 0.5f;
            var poorRoot = new GameObject("poor");
            var poorClock = poorRoot.AddComponent<Clock>();
            poorClock.Init(poorT, poorRoot.transform);

            var faizli = new Faizli();
            failed += Check(ref total, "FAİZLİ parasızken savurmayı REDDETTİ",
                            !faizli.OnSwingStart(poorClock, poorT), poorClock.Seconds);
            failed += Check(ref total, "reddedilen savurma saati düşürmedi",
                            Mathf.Approximately(poorClock.Seconds, 0.2f), poorClock.Seconds);

            poorClock.Init(poorT, poorRoot.transform);          // 0.2 → sıfırla
            poorT.startSeconds = 10f; poorClock.Init(poorT, poorRoot.transform);
            float b2 = poorClock.Seconds;
            faizli.OnSwingStart(poorClock, poorT);
            failed += Check(ref total, "FAİZLİ savururken peşin kesti",
                            Mathf.Approximately(poorClock.Seconds, b2 - poorT.faizliSwingCost), poorClock.Seconds);

            // ── 5) VADELİ bedeli vuruştan SONRA keser ──
            var vadeli = new Vadeli();
            float b3 = poorClock.Seconds;
            vadeli.OnSwingStart(poorClock, poorT);
            failed += Check(ref total, "VADELİ savurma başında bedel ALMADI",
                            Mathf.Approximately(poorClock.Seconds, b3), poorClock.Seconds);
            vadeli.OnStrike(poorClock, poorT, 1, 0);
            failed += Check(ref total, "VADELİ bedeli vuruştan SONRA kesti",
                            Mathf.Approximately(poorClock.Seconds, b3 - poorT.vadeliCost), poorClock.Seconds);

            // ── 6) DURGUN saati dondurur ──
            failed += Check(ref total, "DURGUN basılı tutunca dondurur", new Durgun().HoldFreezesClock, 1);
            failed += Check(ref total, "KONİ dondurmaz", !new ConeBlade().HoldFreezesClock, 0);

            // ── 7) ⚠️ HER SİLAH GERÇEKTEN HASAR VEREBİLİYOR MU ──
            //    DURGUN bu kontrol olmadığı için tamamen kırık teslim edilmişti:
            //    basılı tutma bloğu erken `return` ediyordu, saldırı girdisi hiç okunmuyordu,
            //    bırakınca da bir şey olmuyordu. Hasat çarpanı vardı ama hasar veremiyordu.
            //    Öz-denetim yeşildi çünkü hiçbir test "bu silah vurabiliyor mu" diye sormamıştı.
            for (int wi = 0; wi < Weapon.All.Length; wi++)
            {
                ClearEnemies();
                var target = MakeEnemy(t, pGo.transform, Bootstrap.PlayerSpawn + Vector3.forward * 1.2f);
                pc.SetWeapon(wi);

                float poolBefore = target.TimeLeft;
                pc.Weapon.OnSwingStart(clock, t);
                strike.Invoke(pc, null);

                failed += Check(ref total, $"{pc.Weapon.Name} HASAR VEREBİLİYOR",
                                target.TimeLeft < poolBefore - 0.001f, poolBefore - target.TimeLeft);
            }

            // ── 8) ⚠️ BÜYÜK DÜŞMAN YÜZEYİNDEN VURULABİLİYOR MU (Enes: "bossa vururken buglanıyor")
            //    Menzil merkeze ölçülseydi dev bir yaratık gövdesine değecek kadar yakınken
            //    bile menzil dışında kalırdı — oyuncu GÖRDÜĞÜ şeye vuramazdı.
            ClearEnemies();
            pc.SetWeapon(0);
            var big = MakeEnemy(t, pGo.transform, Bootstrap.PlayerSpawn + Vector3.forward * 3.0f);
            big.transform.localScale = Bootstrap.EnemyScale * 1.9f;   // boss ölçeği

            float centerDist = 3.0f;
            failed += Check(ref total, "test kurgusu: merkez mesafesi menzilin DIŞINDA",
                            centerDist > t.attackRange, centerDist - t.attackRange);
            failed += Check(ref total, "ama yüzeyi menzilin İÇİNDE",
                            centerDist - big.Radius <= t.attackRange, centerDist - big.Radius);

            float bigPoolBefore = big.TimeLeft;
            strike.Invoke(pc, null);
            failed += Check(ref total, "BÜYÜK DÜŞMAN YÜZEYİNDEN VURULDU",
                            big.TimeLeft < bigPoolBefore - 0.001f, bigPoolBefore - big.TimeLeft);

            // ── 9) Dash-atak silaha göre değişiyor mu ──
            failed += Check(ref total, "KONİ dash-atağı süpürüyor",
                            new ConeBlade().DashTargets > 1, new ConeBlade().DashTargets);
            failed += Check(ref total, "FAİZLİ dash-atağı TEK hedefli kalıyor",
                            new Faizli().DashTargets == 1, new Faizli().DashTargets);
            failed += Check(ref total, "tek hedefli dash-atak daha sert vuruyor",
                            new Faizli().DashDamageMultiplier(t) > new ConeBlade().DashDamageMultiplier(t),
                            new Faizli().DashDamageMultiplier(t));

            ClearEnemies();
            Object.DestroyImmediate(poorRoot);
            Object.DestroyImmediate(poorT);
            Object.DestroyImmediate(pGo);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(t);

            if (failed == 0) Debug.Log($"[Faz 3 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 3 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── yardımcılar ──────────────────────────────────────────────────────

        static float[] Snapshot()
        {
            var arr = new float[Enemy.All.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = Enemy.All[i].TimeLeft;
            return arr;
        }

        static int CountDamaged(float[] before)
        {
            int n = 0;
            for (int i = 0; i < Enemy.All.Count && i < before.Length; i++)
                if (Enemy.All[i].TimeLeft < before[i] - 0.001f) n++;
            return n;
        }

        static void SpawnThree(Tuning t, Transform player)
        {
            // Üçü de dar koninin İÇİNDE: tek hedefli silahın gerçekten seçim yaptığını
            // görmek için geometri onu zorlamamalı.
            MakeEnemy(t, player, Bootstrap.PlayerSpawn + new Vector3(-0.25f, 0f, 1.1f));
            MakeEnemy(t, player, Bootstrap.PlayerSpawn + new Vector3(0f, 0f, 1.4f));
            MakeEnemy(t, player, Bootstrap.PlayerSpawn + new Vector3(0.25f, 0f, 1.7f));
        }

        static Enemy MakeEnemy(Tuning t, Transform player, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = pos;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(t, player, null);
            return e;
        }

        static void ClearEnemies()
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
