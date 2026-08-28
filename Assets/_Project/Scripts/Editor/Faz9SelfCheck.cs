using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 9 — checkpoint ve diriliş. Korunan kurallar:
    ///
    ///   • ⚠️⚠️ ÖLÜM ANINDA DİSKE YAZILIYOR MU — kalıcı ölümün tamamı buna bağlı.
    ///     Yazılmazsa oyuncu ölmeden oyunu kapatır ve eşyasını geri kazanır.
    ///     Bu kontrol diske GERÇEKTEN yazıp GERİ OKUYOR; bellekteki sayıya bakmıyor.
    ///   • Kullanım sırası tasarıma gömülü: ÖLÜM ALDATMA → GEÇİCİ DÖNÜŞ → ÇAĞRI
    ///   • Eşya bitince oyun GERÇEKTEN bitiyor
    ///   • GEÇİCİ DÖNÜŞ: süre biterse checkpoint'e, oda temizlenirse kurtuluş
    ///   • ONARIM zamanla ödeniyor (ikinci para birimi doğmuyor — yasa)
    /// </summary>
    public static class Faz9SelfCheck
    {
        [MenuItem("Tek Can/Faz 9 öz-denetimi çalıştır")]
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
            t.lastStandSeconds = 10f;
            t.defianceFraction = 0.35f;
            t.revivalRepairPrice = 20f;

            // ── 1) ⚠️ ÖLÜM DİSKE YAZILIYOR MU — fazın varlık sebebi ──
            //    GERÇEKTEN diske yazıp GERİ OKUYORUZ. Bellekteki sayıya bakmak,
            //    Alt+F4 senaryosunu hiç sınamamak demekti.
            SaveFile.NewRun(t);                       // diske yazar
            int öncekiAldatma = SaveFile.Current.intact[0];

            var (root1, clock1, rev1) = Kur(t, 100f);
            clock1.Spend(999f);                       // öl
            SaveFile.Load();                          // ⚠️ DİSKTEN geri oku
            int sonrakiAldatma = SaveFile.Current.intact[0];

            failed += Check(ref total, "⚠️ ölünce eşya DİSKE düşülmüş (Alt+F4 geri getiremez)",
                            sonrakiAldatma == öncekiAldatma - 1, sonrakiAldatma);
            failed += Check(ref total, "ölüm diskteki sayaca işlenmiş",
                            SaveFile.Current.deaths >= 1, SaveFile.Current.deaths);
            failed += Check(ref total, "kırık eşya diske yazılmış (tamir edilebilir)",
                            SaveFile.Current.broken[0] == 1, SaveFile.Current.broken[0]);
            Object.DestroyImmediate(root1);

            // ── 2) ÖLÜM ALDATMA: yerinde kalkıyorsun ──
            var s = Bellek(defiance: 1, lastStand: 0, recall: 0, checkpointSeconds: 80f);
            var (root2, clock2, rev2) = Kur(t, 100f);
            clock2.Spend(999f);
            failed += Check(ref total, "⚠️ ÖLÜM ALDATMA: ölü DEĞİLSİN artık", !clock2.Dead, 0);
            failed += Check(ref total, "yerinde kalkıyorsun (checkpoint'e atmıyor)",
                            rev2.RecallCount == 0, rev2.RecallCount);
            failed += Check(ref total, $"saat checkpoint'in %35'i ({80f * 0.35f:0.0})",
                            Mathf.Abs(clock2.Seconds - 80f * 0.35f) < 0.1f, clock2.Seconds);
            Object.DestroyImmediate(root2);

            // ── 3) SIRA: aldatma bitince GEÇİCİ DÖNÜŞ ──
            s = Bellek(defiance: 0, lastStand: 1, recall: 1, checkpointSeconds: 80f);
            var (root3, clock3, rev3) = Kur(t, 100f);
            clock3.Spend(999f);
            failed += Check(ref total, "⚠️ SIRA: aldatma yoksa GEÇİCİ DÖNÜŞ kullanılıyor",
                            rev3.LastUsed == RevivalKind.LastStandKind, rev3.LastUsed?.Index ?? -1);
            failed += Check(ref total, "GEÇİCİ DÖNÜŞ 10 saniye veriyor",
                            Mathf.Abs(clock3.Seconds - 10f) < 0.1f, clock3.Seconds);
            failed += Check(ref total, "son şans sayacı kuruldu", rev3.LastStandArmed, 1);
            failed += Check(ref total, "ÇAĞRI harcanmadı (sıra ona gelmedi)",
                            SaveFile.Current.intact[2] == 1, SaveFile.Current.intact[2]);

            // ── 4) ⚠️ GEÇİCİ DÖNÜŞ tükenirse: EŞYA HARCAMADAN checkpoint'e ──
            int çağrıÖnce = SaveFile.Current.intact[2];
            clock3.Spend(999f);                       // süre bitti = tekrar öl
            failed += Check(ref total, "⚠️ süre biterse checkpoint'e atıyor",
                            rev3.RecallCount == 1, rev3.RecallCount);
            failed += Check(ref total, "⚠️ bunun için EŞYA HARCAMIYOR",
                            SaveFile.Current.intact[2] == çağrıÖnce, SaveFile.Current.intact[2]);
            failed += Check(ref total, "son şans sayacı kalktı", !rev3.LastStandArmed, 0);
            Object.DestroyImmediate(root3);

            // ── 5) ⚠️ EŞYA YOKSA OYUN GERÇEKTEN BİTİYOR ──
            s = Bellek(defiance: 0, lastStand: 0, recall: 0, checkpointSeconds: 80f);
            var (root5, clock5, rev5) = Kur(t, 100f);
            clock5.Spend(999f);
            failed += Check(ref total, "⚠️ eşya yoksa OYUN BİTTİ", SaveFile.Current.gameOver, 1);
            failed += Check(ref total, "oyun bitti DİSKE yazıldı (bellekte kalmadı)",
                            DisktenOku().gameOver, 1);
            failed += Check(ref total, "ölü kalıyorsun (diriltilmedin)", clock5.Dead, 1);
            Object.DestroyImmediate(root5);

            // ── 6) ONARIM zamanla ödeniyor ──
            s = Bellek(defiance: 0, lastStand: 0, recall: 0, checkpointSeconds: 80f);
            s.broken[0] = 1;
            var (root6, clock6, _) = Kur(t, 100f);
            float saatÖnce = clock6.Seconds;

            var altarGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            altarGo.transform.SetParent(root6.transform);
            var altar = altarGo.AddComponent<Altar>();
            altar.Init(t, clock6.transform, Altar.Kind.Repair);
            typeof(Altar).GetMethod("Buy",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(altar, null);

            failed += Check(ref total, "⚠️ ONARIM: kırık → sağlam",
                            SaveFile.Current.intact[0] == 1 && SaveFile.Current.broken[0] == 0,
                            SaveFile.Current.intact[0]);
            failed += Check(ref total, "⚠️ ONARIM bedeli ZAMAN (ikinci para birimi yok)",
                            Mathf.Abs((saatÖnce - clock6.Seconds) - t.revivalRepairPrice) < 0.1f,
                            saatÖnce - clock6.Seconds);
            failed += Check(ref total, "onarım DİSKE yazıldı", DisktenOku().intact[0] == 1,
                            DisktenOku().intact[0]);
            Object.DestroyImmediate(root6);

            // ── 7) ⚠️ İLK KEZ AÇILIŞ: eşyasız başlanmıyor ──
            //    Öz-denetimlerin hepsi `NewRun`/bellek kurulumuyla başlıyordu, yani
            //    "hiç kayıt yok" durumunu hiç kurmamışlardı. O durumda oyuncu SIFIR
            //    eşyayla başlıyor ve ilk ölümde oyun bitiyordu.
            SaveFile.UseInMemory(null);
            typeof(SaveFile).GetProperty("LoadedFromDisk")
                ?.SetValue(null, false);                  // taze kurulum taklidi
            failed += Check(ref total, "⚠️ kayıt yokken 'yeni koşu gerekli' deniyor",
                            SaveFile.NeedsNewRun, 1);

            SaveFile.NewRun(t);
            failed += Check(ref total, "⚠️ yeni koşu eşyasız BAŞLAMIYOR",
                            RevivalKind.IntactCount(SaveFile.Current) > 0,
                            RevivalKind.IntactCount(SaveFile.Current));

            // ── 8) ⚠️ TEMİZ ODADA GEÇİCİ DÖNÜŞ bedava kazanç vermiyor ──
            //    Dövüş dışı akıştan temizlenmiş odada ölürsen, sayaç kurulur kurulmaz
            //    oda zaten temiz olduğu için "başardın" sayılabilirdi — bedava zaman.
            s = Bellek(defiance: 0, lastStand: 1, recall: 1, checkpointSeconds: 80f);
            var (root8, clock8, rev8) = Kur(t, 100f);
            var rmGo = new GameObject("rooms"); rmGo.transform.SetParent(root8.transform);
            var rm = rmGo.AddComponent<RoomManager>();
            rm.Init(t, clock8.transform, null);
            KillAll();
            Tick(rm);                                     // oda temizlendi
            clock8.Spend(999f);                           // TEMİZ odada öl → GEÇİCİ DÖNÜŞ

            Tick(rev8);
            failed += Check(ref total, "⚠️ temiz odada GEÇİCİ DÖNÜŞ bedava kazanç VERMİYOR",
                            rev8.LastStandArmed, 1);
            failed += Check(ref total, "saat hâlâ 10 saniyede (checkpoint'e sıçramadı)",
                            clock8.Seconds <= t.lastStandSeconds + 0.1f, clock8.Seconds);
            Object.DestroyImmediate(root8);
            Enemy.All.Clear();

            // ── 9) ⚠️ CHECKPOINT'E DÖNÜŞTE DÜŞMANLAR GERİ GELİYOR MU ──
            //    Enes'in sorusu. Dönüş `Bootstrap.Restart` → oda sıfırdan kurulur.
            //    Burada o yolun sonu sınanıyor: checkpoint odası, dolu düşmanlarla.
            s = Bellek(defiance: 1, lastStand: 0, recall: 0, checkpointSeconds: 80f);
            s.checkpointRoom = 3;
            var (root9, clock9, _) = Kur(t, 80f);
            var rm9Go = new GameObject("rooms"); rm9Go.transform.SetParent(root9.transform);
            var rm9 = rm9Go.AddComponent<RoomManager>();
            rm9.Init(t, clock9.transform, null, SaveFile.Current.checkpointRoom);

            failed += Check(ref total, "⚠️ checkpoint ODASINDA başlıyor", rm9.Room == 3, rm9.Room);
            failed += Check(ref total, "⚠️ dönüşte DÜŞMANLAR GERİ GELİYOR",
                            Enemy.All.Count > 0, Enemy.All.Count);
            bool hepsiDolu = true;
            foreach (var e in Enemy.All) if (e != null && e.Hollow) hepsiDolu = false;
            failed += Check(ref total, "düşmanlar TAM havuzla (boş değil)", hepsiDolu, 1);
            Object.DestroyImmediate(root9);
            Enemy.All.Clear();

            // ── 10) ⚠️ CHECKPOINT YÜKSELTMELERİ DE KAYDEDİYOR ──
            //    Yalnızca saat kaydedilseydi, dönen oyuncu eski zamanıyla ama SIFIR
            //    build'le aynı odalara girerdi — yarım geri yükleme ve ölüm sarmalı.
            s = Bellek(defiance: 1, lastStand: 0, recall: 0, checkpointSeconds: 80f);
            var (root10, clock10, _) = Kur(t, 80f);
            clock10.UpgradeHarvest(0.10f);
            clock10.UpgradeDamage(2f);
            float hasatSonra = clock10.HarvestRate, hasarSonra = clock10.AttackDamage;

            var rm10Go = new GameObject("rooms"); rm10Go.transform.SetParent(root10.transform);
            var rm10 = rm10Go.AddComponent<RoomManager>();
            rm10.Init(t, clock10.transform, null);        // oda 1 = checkpoint → kaydeder

            failed += Check(ref total, "⚠️ checkpoint yükseltmeleri de KAYDEDİYOR",
                            Mathf.Abs(SaveFile.Current.harvestRate - hasatSonra) < 0.001f,
                            SaveFile.Current.harvestRate);

            // Dönüşü taklit et: yeni Clock + geri yükleme
            var clock10b = root10.AddComponent<Clock>();
            clock10b.Init(t, clock10.transform, SaveFile.Current.checkpointSeconds);
            bool sıfırlanmıştı = Mathf.Abs(clock10b.AttackDamage - hasarSonra) > 0.001f;
            clock10b.RestoreUpgrades(SaveFile.Current.harvestRate, SaveFile.Current.attackDamage,
                                     SaveFile.Current.dashCooldown);

            failed += Check(ref total, "yeni Clock yükseltmesiz doğuyor (geri yükleme şart)",
                            sıfırlanmıştı, 1);
            failed += Check(ref total, "⚠️ dönüşte YÜKSELTMELER geri geliyor",
                            Mathf.Abs(clock10b.AttackDamage - hasarSonra) < 0.001f,
                            clock10b.AttackDamage);
            Object.DestroyImmediate(root10);
            Enemy.All.Clear();

            // ── 11) Checkpoint aralığı ──
            failed += Check(ref total, "checkpoint aralığı en az 1", t.checkpointEveryRooms >= 1,
                            t.checkpointEveryRooms);

            Object.DestroyImmediate(t);
            Clock.Reset();
            RevivalSystem.Reset();

            if (failed == 0) Debug.Log($"[Faz 9 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 9 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        /// <summary>Diskteki kaydı Current'a dokunmadan okur — "gerçekten yazıldı mı" için.</summary>
        static SaveData DisktenOku()
        {
            var tut = SaveFile.Current;
            SaveFile.Load();
            var okunan = SaveFile.Current;
            SaveFile.UseInMemory(tut);
            return okunan;
        }

        static SaveData Bellek(int defiance, int lastStand, int recall, float checkpointSeconds)
        {
            var s = new SaveData { checkpointSeconds = checkpointSeconds };
            SaveFile.UseInMemory(s);
            SaveFile.Current.intact[0] = defiance;
            SaveFile.Current.intact[1] = lastStand;
            SaveFile.Current.intact[2] = recall;
            SaveFile.Flush();
            return SaveFile.Current;
        }

        static (GameObject, Clock, RevivalSystem) Kur(Tuning t, float saniye)
        {
            var root = new GameObject("faz9-check");
            var player = new GameObject("player");
            player.transform.SetParent(root.transform);
            player.transform.position = Bootstrap.ArenaCenter;

            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, saniye);

            var rev = root.AddComponent<RevivalSystem>();
            rev.Init(t, clock);
            return (root, clock, rev);
        }

        static void KillAll()
        {
            foreach (var e in Enemy.All.ToArray())
                if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
        }

        /// <summary>Private Update'i elle çevir — kod tabanının öz-denetim deseni.</summary>
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
