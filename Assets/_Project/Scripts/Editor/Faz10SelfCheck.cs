using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 10 — bölge zinciri. Korunan kurallar:
    ///
    ///   • ⚠️ KURALLAR GERÇEKTEN ETKİ EDİYOR MU — "tanımlı mı" değil, saati DEĞİŞTİRİYOR mu.
    ///     `lessons.md` (6): özellik testi etki testinin yerini tutmaz.
    ///   • Zincir yürüyor: boss ölünce bölge biter, sonuncuda koşu KAZANILIR
    ///   • Bölgeler birbirinden gerçekten farklı
    ///   • Her kural SAATTEN geçiyor (yasa) — bölgeye özel ikinci sistem yok
    /// </summary>
    public static class Faz10SelfCheck
    {
        [MenuItem("Tek Can/Faz 10 öz-denetimi çalıştır")]
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
            t.enemyDecayPerSecond = 0f;
            t.roomsPerRegion = 2;          // kısa bölge: oda 1, sonra boss
            t.regionsPerAct = 3;
            t.plainRoomCount = 0;

            // ── 1) Bölgeler birbirinden farklı ──
            failed += Check(ref total, "en az 3 bölge tanımlı", Region.All.Length >= 3,
                            Region.All.Length);
            int aynıAd = 0, aynıKural = 0;
            for (int a = 0; a < Region.All.Length; a++)
                for (int b = a + 1; b < Region.All.Length; b++)
                {
                    if (Region.All[a].Name == Region.All[b].Name) aynıAd++;
                    if (KuralImzası(Region.All[a]) == KuralImzası(Region.All[b])) aynıKural++;
                }
            failed += Check(ref total, "bölge adları benzersiz", aynıAd == 0, aynıAd);
            failed += Check(ref total, "⚠️ bölgelerin KURALLARI farklı (aynı oyun değil)",
                            aynıKural == 0, aynıKural);

            // ── 2) ⚠️ DARALMA saati GERÇEKTEN hızlı eritiyor mu ──
            //    Çarpanın tanımlı olması yetmez; Clock'un onu OKUDUĞU kanıtlanmalı.
            float kuralsız = DövüşDışıAkış(t, Region.At(0));
            float daralma = DövüşDışıAkış(t, Bul("DARALMA"));
            failed += Check(ref total, "⚠️ DARALMA: saat GERÇEKTEN daha hızlı eriyor",
                            daralma > kuralsız + 0.01f, daralma - kuralsız);

            // ── 3) ⚠️ SIZINTI dövüşte BİLE emiyor ──
            //    Dövüş dışı akıştan farkı bu; test dövüş hâlini kuruyor.
            float sızıntıDövüşte = DövüşteAkış(t, Bul("SIZINTI"));
            float kuralsızDövüşte = DövüşteAkış(t, Region.At(0));
            failed += Check(ref total, "⚠️ SIZINTI: DÖVÜŞTE bile emiyor",
                            sızıntıDövüşte > kuralsızDövüşte + 0.01f,
                            sızıntıDövüşte - kuralsızDövüşte);

            // ── 4) ⚠️ KITLIK hasadı GERÇEKTEN düşürüyor mu ──
            float bolHasat = VuruşKazancı(t, Region.At(0));
            float kıtHasat = VuruşKazancı(t, Bul("KITLIK"));
            failed += Check(ref total, "⚠️ KITLIK: vuruş GERÇEKTEN daha az veriyor",
                            kıtHasat < bolHasat - 0.01f, bolHasat - kıtHasat);

            // ── 5) ⚠️ AĞIRLIK darbeyi GERÇEKTEN pahalılaştırıyor mu ──
            float normalDarbe = DarbeBedeli(t, Region.At(0));
            float ağırDarbe = DarbeBedeli(t, Bul("AĞIRLIK"));
            failed += Check(ref total, "⚠️ AĞIRLIK: darbe GERÇEKTEN daha pahalı",
                            ağırDarbe > normalDarbe + 0.01f, ağırDarbe - normalDarbe);

            // ── 6) ⚠️ ZİNCİR YÜRÜYOR MU ──
            Region.Reset();
            SaveFile.UseInMemory(new SaveData());
            var root = new GameObject("faz10-check");
            var player = new GameObject("player"); player.transform.SetParent(root.transform);
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, 500f);
            var rm = root.AddComponent<RoomManager>();
            rm.Init(t, player.transform, null);

            failed += Check(ref total, "bölge 0'da başlıyor", rm.RegionIndex == 0, rm.RegionIndex);
            failed += Check(ref total, "ilk bölge SON bölge değil", !rm.IsLastRegion, 0);

            // Boss odasına git, boss'u devir → bölge bitmeli, koşu BİTMEMELİ
            GoToRoom(rm, t.roomsPerRegion);
            failed += Check(ref total, "boss odasına gelindi", rm.IsBossRoom, rm.Room);
            failed += Check(ref total, "⚠️ ara bölgenin boss odasında ÇIKIŞ VAR (zincir sürüyor)",
                            Object.FindFirstObjectByType<ExitGate>() != null, 1);

            KillAll(); Tick(rm);
            failed += Check(ref total, "⚠️ ara boss ölünce koşu KAZANILMIYOR", !clock.Won, 0);

            rm.NextRoom();          // çıkıştan geç → sıradaki bölge
            failed += Check(ref total, "⚠️ boss'tan sonra SIRADAKİ BÖLGEYE geçiyor",
                            rm.RegionIndex == 1, rm.RegionIndex);
            failed += Check(ref total, "yeni bölgede oda 1'den başlıyor", rm.Room == 1, rm.Room);
            failed += Check(ref total, "bölge kuralı DEĞİŞTİ",
                            Region.Current == Region.At(1), Region.Current?.Index ?? -1);

            // Son bölgeye atla, boss'u devir → koşu KAZANILMALI
            GoToRegion(rm, t.regionsPerAct - 1);
            failed += Check(ref total, "son bölgeye gelindi", rm.IsLastRegion, rm.RegionIndex);
            GoToRoom(rm, t.roomsPerRegion);
            failed += Check(ref total, "⚠️ SON bölgenin boss odasında çıkış YOK",
                            Object.FindFirstObjectByType<ExitGate>() == null, 0);

            KillAll(); Tick(rm);
            failed += Check(ref total, "⚠️ SON boss ölünce KOŞU KAZANILIYOR", clock.Won, 1);

            // ── 7) Checkpoint bölgeyi de kaydediyor ──
            failed += Check(ref total, "checkpoint BÖLGEYİ de kaydetti",
                            SaveFile.Current.checkpointRegion >= 1,
                            SaveFile.Current.checkpointRegion);

            KillAll();
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(t);
            Region.Reset(); Clock.Reset(); RoomManager.Reset();

            if (failed == 0) Debug.Log($"[Faz 10 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 10 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── ölçüm yardımcıları: hepsi ETKİYİ ölçüyor, çarpanı okumuyor ──

        /// <summary>
        /// ⚠️ GERÇEK SAATTEN ölçülüyor — formül yeniden hesaplanmıyor.
        /// Kendim `t.drainOutOfCombat * Region.Drain` yazsaydım, `Clock` o çarpanı
        /// uygulamayı unutsa bile test geçerdi. (`BossHitRepro`'nun dersi.)
        /// </summary>
        static float DövüşDışıAkış(Tuning t, Region r)
        {
            Region.SetCurrent(r);
            Enemy.All.Clear();                       // düşman yok = dövüş dışı
            return SaatteDüşüş(t, 1f);
        }

        /// <summary>DÖVÜŞTE 1 saniyelik düşüş — sızıntı burada ayrışıyor.</summary>
        static float DövüşteAkış(Tuning t, Region r)
        {
            Region.SetCurrent(r);
            var root = new GameObject("dövüş");
            var player = new GameObject("p"); player.transform.SetParent(root.transform);
            player.transform.position = Bootstrap.ArenaCenter;

            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, 500f);
            Make(t, player.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.5f);

            float önce = clock.Seconds;
            clock.Tick(1f);
            float düşüş = önce - clock.Seconds;

            bool dövüşteydi = clock.InCombat;
            KillAll();
            Object.DestroyImmediate(root);
            Clock.Reset();

            return dövüşteydi ? düşüş : -1f;         // dövüş kurulamadıysa testi düşür
        }

        static float SaatteDüşüş(Tuning t, float dt)
        {
            var root = new GameObject("akış");
            var player = new GameObject("p"); player.transform.SetParent(root.transform);
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, 500f);

            float önce = clock.Seconds;
            clock.Tick(dt);
            float düşüş = önce - clock.Seconds;

            Object.DestroyImmediate(root);
            Clock.Reset();
            return düşüş;
        }

        /// <summary>Aynı vuruş bu bölgede ne kadar saat kazandırıyor — GERÇEK saatten ölçülüyor.</summary>
        static float VuruşKazancı(Tuning t, Region r)
        {
            Region.SetCurrent(r);
            var root = new GameObject("kazanç");
            var player = new GameObject("p"); player.transform.SetParent(root.transform);
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, 500f);

            var e = Make(t, player.transform, Bootstrap.ArenaCenter + Vector3.forward * 2f);
            float önce = clock.Seconds;
            e.TakeDamage(3f);
            float kazanç = clock.Seconds - önce;

            KillAll();
            Object.DestroyImmediate(root);
            Clock.Reset();
            return kazanç;
        }

        /// <summary>
        /// ⚠️ ÜRETİMİN değerini okuyor: `Enemy.HitCost` private, yansımayla alınıyor.
        /// `t.enemyFullHitCost * Region.HitCost` diye yeniden hesaplasaydım, `Enemy`
        /// çarpanı uygulamayı unutsa bile test geçerdi.
        /// </summary>
        static float DarbeBedeli(Tuning t, Region r)
        {
            Region.SetCurrent(r);
            var root = new GameObject("darbe");
            var player = new GameObject("p"); player.transform.SetParent(root.transform);
            var e = Make(t, player.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.5f);

            var prop = typeof(Enemy).GetProperty("HitCost",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float bedel = prop != null ? (float)prop.GetValue(e) : -1f;

            KillAll();
            Object.DestroyImmediate(root);
            return bedel;
        }

        static string KuralImzası(Region r) =>
            $"{r.DrainMultiplier:0.00}/{r.HarvestMultiplier:0.00}/" +
            $"{r.HitCostMultiplier:0.00}/{r.BackgroundDrain:0.00}";

        static Region Bul(string ad)
        {
            foreach (var r in Region.All) if (r.Name == ad) return r;
            return Region.At(0);
        }

        static Enemy Make(Tuning t, Transform player, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = pos;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(t, player, null, 1f, false, EnemyKind.Basic);
            return e;
        }

        static void KillAll()
        {
            foreach (var e in Enemy.All.ToArray())
                if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
        }

        static void GoToRoom(RoomManager rm, int room) =>
            typeof(RoomManager).GetMethod("EnterRoom",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(rm, new object[] { room });

        static void GoToRegion(RoomManager rm, int region) =>
            typeof(RoomManager).GetMethod("EnterRegion",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(rm, new object[] { region, 1 });

        static void Tick(MonoBehaviour mb) =>
            mb.GetType().GetMethod("Update",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(mb, null);

        static int Check(ref int total, string name, bool ok, float actual)
        {
            total++;
            if (ok) Debug.Log($"  ✅ {name}");
            else Debug.LogError($"  ❌ {name}  (gerçek: {actual})");
            return ok ? 0 : 1;
        }
    }
}
