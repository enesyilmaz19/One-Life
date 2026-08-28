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
            t.roomsPerRegion = 3;            // kısa bölge: oda 1, 2, sonra boss odası
            // ⚠️ TEK bölge: bu denetim koşunun yapısını sınıyor (boss = son, kazanma orada).
            // Bölge ZİNCİRİ Faz 10'un konusu ve orada sınanıyor.
            t.regionsPerAct = 1;
            t.roomBaseCount = 2f;
            t.roomCountGrowth = 1f;
            t.enemyTimePool = 10f;

            var root = new GameObject("faz4-check");
            var player = new GameObject("player");
            player.transform.position = Bootstrap.ArenaCenter;

            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform);

            Enemy.All.Clear();
            var wm = root.AddComponent<RoomManager>();
            wm.Init(t, player.transform, null);

            // 1) ⚠️ ODAYA GİRİNCE DÖVÜŞ ZATEN BAŞLAMIŞ
            //    Faz 8'in özü bu: ENTER kalktı, dalga çağırmıyorsun. Eskiden koşu
            //    "ara evresinde, hiç düşman yok" diye başlıyordu — o bir test düzeneğiydi.
            int w1 = Enemy.All.Count;
            failed += Check(ref total, "⚠️ oda 1'de düşmanlar ZATEN duruyor", w1 > 0, w1);
            failed += Check(ref total, "oda 1 boss odası değil", !wm.IsBossRoom, wm.Room);
            failed += Check(ref total, "oda temizlenmemiş sayılıyor", !wm.Cleared, 0);
            float pool1 = Enemy.All[0].TimeLeft;

            // 2) Oyuncu GİRİŞE konuluyor — oda bir yol
            failed += Check(ref total, "oyuncu odanın girişinde",
                            Vector3.Distance(player.transform.position, Bootstrap.Entrance(t)) < 0.01f,
                            Vector3.Distance(player.transform.position, Bootstrap.Entrance(t)));

            // 3) ⚠️ ÇIKIŞ ODA TEMİZLENENE KADAR KAPALI
            var gate = Object.FindFirstObjectByType<ExitGate>();
            failed += Check(ref total, "çıkış kapısı var", gate != null, gate != null ? 1 : 0);
            failed += Check(ref total, "⚠️ çıkış başta KAPALI",
                            gate != null && !gate.IsOpen, 0);

            // 4) Oda temizlenince çıkış açılıyor
            KillAll();
            Tick(wm);
            failed += Check(ref total, "oda temizlendi olarak işaretlendi", wm.Cleared, 1);
            failed += Check(ref total, "⚠️ temizlenince çıkış AÇILIYOR",
                            gate != null && gate.IsOpen, 1);

            // 5) Odalar sertleşiyor
            wm.NextRoom();
            int w2 = Enemy.All.Count;
            float pool2 = Enemy.All[0].TimeLeft;
            failed += Check(ref total, "oda 2 daha kalabalık", w2 > w1, w2 - w1);
            failed += Check(ref total, "oda 2 düşmanı daha dayanıklı", pool2 > pool1, pool2 - pool1);

            // 6) ⚠️ ESKİ ODA SÖKÜLDÜ MÜ
            //    `Enemy.All` statik: nesneyi yok etmek listeden düşürmüyor. Temizlemezsek
            //    yeni oda "hâlâ düşman var" sanır ve çıkış hiç açılmaz.
            int hayalet = 0;
            foreach (var e in Enemy.All) if (e == null) hayalet++;
            failed += Check(ref total, "⚠️ eski odadan hayalet düşman kalmadı", hayalet == 0, hayalet);

            // 7) Boss odası
            KillAll();
            wm.NextRoom();
            failed += Check(ref total, "3. oda BOSS odası", wm.IsBossRoom, wm.Room);
            failed += Check(ref total, "⚠️ boss odasında çıkış YOK (bölge orada bitiyor)",
                            Object.FindFirstObjectByType<ExitGate>() == null, 0);
            var boss = Enemy.All.Count > 0 ? Enemy.All[0] : null;
            failed += Check(ref total, "boss doğdu", boss != null && boss.IsBoss, Enemy.All.Count);
            failed += Check(ref total, "boss çok daha dayanıklı",
                            boss != null && boss.TimeLeft > pool2 * 3f, boss != null ? boss.TimeLeft : 0f);

            // 5) ⚠️ BOSS ÖLÜNCE KOŞU KAZANILIYOR MU — fazın varlık sebebi
            failed += Check(ref total, "boss ölmeden koşu kazanılmamış", !clock.Won, clock.Won ? 1 : 0);

            bool winFired = false;
            clock.OnWin += () => winFired = true;

            KillAll();
            Tick(wm);

            failed += Check(ref total, "BOSS ÖLÜNCE KOŞU KAZANILDI", clock.Won, clock.Won ? 1 : 0);
            failed += Check(ref total, "kazanma olayı tetiklendi", winFired, winFired ? 1 : 0);
            failed += Check(ref total, "kazanınca ölü sayılmıyor", !clock.Dead, clock.Dead ? 1 : 0);

            // ── ⚠️ YERE VURUŞ GERÇEKTEN ÇALIŞIYOR MU
            //    Enes: "dümdüz durup saldırarak kesiyorum." Boss'un oyuncuyu yerinden
            //    kaldıran bir saldırısı yoktu. Yere vuruş 360° — yana kaçmak işe yaramaz,
            //    UZAKLAŞMAK gerekir. Test: halkanın içi vurulur, dışı vurulmaz.
            KillAll();
            GoToRoom(wm, t.roomsPerRegion);           // taze boss odası
            var slamBoss = Enemy.All.Count > 0 ? Enemy.All[0] : null;
            if (slamBoss != null)
            {
                var windup = typeof(Enemy).GetMethod("EnterWindup",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                // İlk saldırı normal, ikincisi yere vuruş (bossSlamEvery = 2)
                windup.Invoke(slamBoss, null);
                failed += Check(ref total, "1. saldırı normal", !slamBoss.Slamming, 0);
                windup.Invoke(slamBoss, null);
                failed += Check(ref total, "2. saldırı YERE VURUŞ", slamBoss.Slamming, 1);

                // Halkanın İÇİNDE: arkasında dursan bile yakalanıyorsun (yön yok)
                player.transform.position = slamBoss.transform.position
                                          - slamBoss.transform.forward * (t.bossSlamRadius * 0.5f);
                failed += Check(ref total, "yere vuruş ARKADA duranı bile yakalıyor",
                                slamBoss.PlayerInStrikeCone(), 1);

                // Halkanın DIŞINDA: kurtuluyorsun
                player.transform.position = slamBoss.transform.position
                                          + Vector3.right * (t.bossSlamRadius + 1.5f);
                failed += Check(ref total, "halkanın DIŞINA çıkan kurtuluyor",
                                !slamBoss.PlayerInStrikeCone(), 0);
            }
            else failed += Check(ref total, "yere vuruş testi için boss doğdu", false, 0);

            // 6) Boss beklenerek yenilemez: akışı normalden çok yavaş
            failed += Check(ref total, "boss akışı yavaşlatılmış",
                            t.bossDecayMultiplier < 0.5f, t.bossDecayMultiplier);

            // ⚠️ ETKİ testi, özellik değil: boss saati GERÇEKTEN emiyor mu?
            KillAll();
            GoToRoom(wm, t.roomsPerRegion);           // yeni boss odası
            var b2 = Enemy.All.Count > 0 ? Enemy.All[0] : null;
            if (b2 != null)
            {
                float auraBefore = clock.Seconds;
                b2.ApplyAura(1f);
                failed += Check(ref total, "BOSS SAATİ GERÇEKTEN EMDİ",
                                clock.Seconds < auraBefore, auraBefore - clock.Seconds);
            }
            else failed += Check(ref total, "boss aura testi için boss doğdu", false, 0);

            KillAll();
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(t);

            if (failed == 0) Debug.Log($"[Faz 4 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 4 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        /// <summary>
        /// Belirli bir odaya doğrudan gir. `NextRoom` boss odasındayken BÖLGE atlıyor,
        /// yani taze bir boss kurmak için kullanılamıyor.
        /// </summary>
        static void GoToRoom(RoomManager rm, int room) =>
            typeof(RoomManager).GetMethod("EnterRoom",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(rm, new object[] { room });

        /// <summary>RoomManager.Update'i bir kez elle çevir — temizlenme kararı orada veriliyor.</summary>
        static void Tick(RoomManager rm) =>
            typeof(RoomManager).GetMethod("Update",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(rm, null);

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
