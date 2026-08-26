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

            // ── 2) ⚠️ SIZINTI SAATİ GERÇEKTEN EMİYOR MU (özelliği değil, ETKİSİ)
            //    İlk yazdığımda yalnızca `AuraDrain > 0` diye özelliği kontrol etmiştim.
            //    DURGUN dersi tam buydu: özellik doğru olabilir ama etki hiç olmayabilir.
            t.drainInCombat = 0f;            // yalnızca aura ölçülsün
            t.drainOutOfCombat = 0f;

            Clear();
            var leak = Make(t, player.transform, Bootstrap.PlayerSpawn + Vector3.forward * 6f, EnemyKind.Leak);
            float leakBefore = clock.Seconds;
            leak.ApplyAura(1f);                          // bir saniyelik emiş
            float leakDrained = leakBefore - clock.Seconds;

            Clear();
            var plain = Make(t, player.transform, Bootstrap.PlayerSpawn + Vector3.forward * 6f, EnemyKind.Basic);
            float plainBefore = clock.Seconds;
            plain.ApplyAura(1f);
            float plainDrained = plainBefore - clock.Seconds;

            failed += Check(ref total, "SIZINTI SAATİ GERÇEKTEN EMDİ", leakDrained > 0f, leakDrained);
            failed += Check(ref total, "TEMEL hiç emmedi (kıyas)",
                            Mathf.Abs(plainDrained) < 0.0001f, plainDrained);

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

            // ── 6a) ⚠️ KAÇAK YAKALANABİLİYOR MU
            //    Enes: "ibne çok hızlı kaçıyo". Kaçarken hızı 7.25'ti, oyuncu 6.
            //    Yani hiç yakalanamıyordu. Kovala/bırak bir KARAR olmalı; yakalanamayan
            //    düşmanda karar yok, sadece kayıp var.
            //    Hız, düşmanın KENDİSİNDEN okunuyor. Formülü burada kopyalasaydım
            //    Enemy.MoveSpeed'e yeni bir çarpan eklendiğinde test yeşil kalırdı.
            float kacisHizi = runner.CurrentSpeed;
            failed += Check(ref total, "kaçan düşman gerçekten hızlandı",
                            kacisHizi > EnemyKind.Runner.SpeedMultiplier * t.enemyFullSpeed, kacisHizi);
            failed += Check(ref total, "KAÇAK oyuncudan YAVAŞ kaçıyor (yakalanabilir)",
                            kacisHizi < t.moveSpeed, kacisHizi);
            failed += Check(ref total, "ama yeterince hızlı (kovalamak zaman yiyor)",
                            kacisHizi > t.moveSpeed * 0.7f, kacisHizi);

            // ── 6b) ⚠️ KAÇAK GERÇEKTEN KAÇABİLİYOR MU — ve kaçınca HİÇBİR ŞEY ödemiyor mu
            //    Yine özellik değil etki: "kaçmaya başladı" ile "kaçtı ve zamanını götürdü"
            //    aynı şey değil.
            int countBefore = Enemy.All.Count;
            float escapeClockBefore = clock.Seconds;
            runner.transform.position = new Vector3(t.runnerEscapeRadius + 2f, Bootstrap.PlayerSpawn.y, 0f);
            Tick(runner);

            failed += Check(ref total, "KAÇAK kenara varınca KAYBOLDU",
                            Enemy.All.Count == countBefore - 1, Enemy.All.Count);
            failed += Check(ref total, "kaçan düşman HİÇBİR ŞEY ödemedi",
                            Mathf.Approximately(clock.Seconds, escapeClockBefore),
                            clock.Seconds - escapeClockBefore);

            // Kaçış mesafesi YATAY ölçülmeli — y=1'deki düşman erken kaçmış sayılmamalı
            Clear();
            var yTest = Make(t, player.transform,
                             new Vector3(t.runnerEscapeRadius - 0.5f, Bootstrap.PlayerSpawn.y, 0f), EnemyKind.Runner);
            yTest.TakeDamage(yTest.TimeLeft * 0.9f);
            Tick(yTest);
            failed += Check(ref total, "kaçış mesafesi YATAY ölçülüyor (y karışmıyor)",
                            Enemy.All.Count == 1, Enemy.All.Count);

            // ── 7) Türler kademeli açılıyor ──
            Clear();
            var wm = root.AddComponent<RoomManager>();
            wm.Init(t, player.transform, null);
            Clear();      // Init ilk odayı kuruyor; tür seçimini sınayacağız, düşmanları değil
            var pick = typeof(RoomManager).GetMethod("PickKind",
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
