using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 21 — NEMESİS. `docs/TASARIM.md`: *"Seni öldüren boss, senden aldığı zamanı
    /// hesabına geçiriyor ve bir dahaki karşılaşmada daha büyük oluyor."*
    ///
    /// ⚠️ Borç metaforu kesintisiz: bu oyunda ölmek bir CEZA değil bir TEMERRÜT.
    /// Kaybettiğin zaman yok olmuyor, KARŞI TARAFA geçiyor.
    ///
    /// ⚠️ Bu denetimin kalbi: zincirin HER HALKASI ölçülüyor, çünkü her biri ayrı
    /// ayrı sessizce kopabilir —
    ///   ① boss senden aldığını SAYIYOR mu (ıskaladığını saymıyor mu)
    ///   ② ölüm anında DİSKE yazılıyor mu
    ///   ③ bir dahaki doğuşta havuzuna EKLENİYOR mu
    ///   ④ TAVAN tutuyor mu — yoksa kalıcı ölüm bir duvara dönüşür
    ///   ⑤ kalıcı ölümde SİLİNİYOR mu — meta ilerleme yok
    /// </summary>
    public static class Faz21SelfCheck
    {
        [MenuItem("Tek Can/Faz 21 öz-denetimi çalıştır (nemesis)")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            // ⚠️ Diske GERÇEKTEN yazıp geri okuyoruz — ama oyuncunun dosyasına değil.
            SaveFile.PathOverride = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "tekcan_nemesis_check.json");

            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 500f;
            t.enemyTimePool = 10f;
            t.enemyDecayPerSecond = 0f;
            t.bossAuraDrain = 2f;
            Region.Reset();

            SaveFile.NewRun(t);

            // ── 1) Defter TEMİZ başlıyor ──
            float başlangıç = 0f;
            foreach (var b in BossKind.All) başlangıç += SaveFile.BossLedger(b);
            failed += Check(ref total, "yeni koşuda defter TEMİZ", Mathf.Approximately(başlangıç, 0f),
                            başlangıç);

            // ── 2) ⚠️ BOSS SENDEN ALDIĞINI SAYIYOR — özellik değil ETKİ ──
            var (root2, clock2) = Kur(t);
            var boss = Boss(t, root2, BossKind.For(0));
            failed += Check(ref total, "dövüş başında hesabı SIFIR",
                            Mathf.Approximately(boss.StolenFromPlayer, 0f), boss.StolenFromPlayer);

            boss.ApplyAura(3f);            // dt dışarıdan (tuzak 3)
            failed += Check(ref total, "⚠️ AURA da boss'un hanesine yazılıyor",
                            boss.StolenFromPlayer > 0.1f, boss.StolenFromPlayer);

            float çalınan = boss.StolenFromPlayer;

            // ── 3) ⚠️ ÖLÜM ANINDA DİSKE YAZILIYOR ──
            //    Yazan yer `RevivalSystem`, yani ölüm sırasının İÇİ. Ayrı bir yol
            //    olsaydı "ölüm anında kapatan oyuncu" açığı nemesis için yeniden
            //    açılırdı.
            var rev = root2.AddComponent<RevivalSystem>();
            rev.Init(t, clock2);
            clock2.Spend(9999f);           // öl

            SaveFile.Load();               // DİSKTEN geri oku
            float defter = SaveFile.BossLedger(BossKind.For(0));
            failed += Check(ref total, $"⚠️ ölünce boss'un hesabına DİSKE yazıldı ({defter:0.0} sn)",
                            defter >= çalınan - 0.01f, defter);

            // Diğer boss'ların hesabı BOŞ — yalnızca seni öldüren yazılıyor
            failed += Check(ref total, "yalnızca DÖVÜŞTÜĞÜN boss'un hesabı doldu",
                            Mathf.Approximately(SaveFile.BossLedger(BossKind.For(1)), 0f),
                            SaveFile.BossLedger(BossKind.For(1)));
            Temizle(root2);

            // ── 4) ⚠️ BİR DAHAKİ KARŞILAŞMADA HAVUZUNA EKLENİYOR ──
            //    Tasarımın kalbi bu. Eklenmezse defter tutulur ama HİÇBİR ŞEY OLMAZ.
            var (root4, clock4) = Kur(t);
            var yeni = Boss(t, root4, BossKind.For(0));
            var temiz = Boss(t, root4, BossKind.For(1));

            failed += Check(ref total, "⚠️ borçlu boss DAHA BÜYÜK geliyor",
                            yeni.TimeLeft > temiz.TimeLeft + 0.01f, yeni.TimeLeft - temiz.TimeLeft);
            failed += Check(ref total, "⚠️ borcun kendisi okunabiliyor (HUD için)",
                            yeni.NemesisDebt > 0.01f, yeni.NemesisDebt);
            failed += Check(ref total, "hesabı temiz boss'un borcu YOK",
                            Mathf.Approximately(temiz.NemesisDebt, 0f), temiz.NemesisDebt);
            Temizle(root4);

            // ── 5) ⚠️ TAVAN TUTUYOR — kalıcı ölüm bir DUVAR olmamalı ──
            //    Tavansız bırakılsaydı yeterince ölen oyuncu için boss matematiksel
            //    olarak yenilemez olurdu. Nemesis hikâye anlatmalı, koşuyu kilitlememeli.
            SaveFile.CreditBoss(BossKind.For(0), 100000f);
            var (root5, clock5) = Kur(t);
            var şişman = Boss(t, root5, BossKind.For(0));
            float tabanHavuz = t.enemyTimePool * t.bossPoolMultiplier;
            failed += Check(ref total, $"⚠️ borç TAVANLI (havuz {şişman.TimeLeft:0} sn, sınırsız değil)",
                            şişman.NemesisDebt <= tabanHavuz * t.nemesisMaxShare * 1.5f + 1f,
                            şişman.NemesisDebt);
            Temizle(root5);

            // ── 6) ⚠️ ÇAĞRILAN RAKİPLERİN kendi hesabı YOK ──
            //    Olsaydı finalde aynı borcu İKİ KEZ ödetirdik: bir kez yankıda,
            //    bir kez asıl karşılaşmada.
            var (root6, clock6) = Kur(t);
            var yankı = Boss(t, root6, BossKind.For(0));
            yankı.MarkAsSummonedRival();
            failed += Check(ref total, "⚠️ çağrılan YANKI borç taşımıyor (çifte ödeme yok)",
                            Mathf.Approximately(yankı.NemesisDebt, 0f), yankı.NemesisDebt);
            Temizle(root6);

            // ── 7) ⚠️ KALICI ÖLÜMDE SİLİNİYOR — meta ilerleme YOK ──
            //    Nemesis bir KOŞUNUN içindeki hikâye. Koşular arası taşınsaydı
            //    oyunun yasası ("ölünce her şey gider") delinirdi.
            SaveFile.NewRun(t);
            float sonra = 0f;
            foreach (var b in BossKind.All) sonra += SaveFile.BossLedger(b);
            failed += Check(ref total, "⚠️ SIFIRDAN koşuda defter SİLİNİYOR (meta ilerleme yok)",
                            Mathf.Approximately(sonra, 0f), sonra);

            // ── 8) Defter boyu kadroyla UYUMLU ──
            failed += Check(ref total, "defter boyu boss sayısıyla uyumlu",
                            SaveFile.Current.bossLedger.Length == BossKind.All.Length,
                            SaveFile.Current.bossLedger.Length);

            try { if (System.IO.File.Exists(SaveFile.PathOverride)) System.IO.File.Delete(SaveFile.PathOverride); }
            catch { /* geçici dosya, önemsiz */ }
            SaveFile.PathOverride = null;

            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset(); Enemy.All.Clear(); RevivalSystem.Reset();

            if (failed == 0) Debug.Log($"[Faz 21 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 21 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        static (GameObject, Clock) Kur(Tuning t)
        {
            Enemy.All.Clear();
            RevivalSystem.Reset();
            var root = new GameObject("faz21-check");
            var player = new GameObject("player"); player.transform.SetParent(root.transform);
            player.transform.position = Bootstrap.ArenaCenter;
            player.AddComponent<PlayerController>().Init(t, null);
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, t.startSeconds);
            return (root, clock);
        }

        static Enemy Boss(Tuning t, GameObject root, BossKind kind) =>
            Bootstrap.SpawnEnemyAt(t, Bootstrap.ArenaCenter, root.transform.Find("player"),
                                   null, 1f, boss: true, parent: root.transform, bossKind: kind);

        static void Temizle(GameObject root)
        {
            foreach (var e in Enemy.All.ToArray())
                if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include))
                Object.DestroyImmediate(a.gameObject);
            foreach (var m in Object.FindObjectsByType<Mote>(FindObjectsInactive.Include))
                Object.DestroyImmediate(m.gameObject);
            if (root != null) Object.DestroyImmediate(root);
            Clock.Reset(); RevivalSystem.Reset();
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
