using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 18 — boss çağırma + son boss'un ÜÇÜNCÜ fazı.
    ///
    /// Enes: *"bossların faz 2'sine koymadığın düşman çağırma özelliğini de ekle"*
    /// ve *"son bossa faz 3 ekleyelim; onda da ilk önce önceki bossların her birini
    /// çağırsın, hepsi ölünce kendisi gelsin."*
    ///
    /// ⚠️ Bu denetimin kalbi UYKU KURALI: uykudaki boss vurulmuyor, vurmuyor ve
    /// pasifleri işlemiyor. Üçü de ayrı ayrı ölçülüyor çünkü üçü de ayrı ayrı
    /// unutulabilir — ve her biri unutulursa final SESSİZCE bozulur:
    ///   vurulursa   → oyuncu rakipleri es geçip boss'u döver, kural anlamsızlaşır
    ///   vurursa     → oyuncu beş boss'la aynı anda dövüşür, final bir kaza olur
    ///   dolarsa     → rakipler ölene kadar boss iyileşir, dövüş kazanılamaz olur
    /// </summary>
    public static class Faz18SelfCheck
    {
        [MenuItem("Tek Can/Faz 18 öz-denetimi çalıştır (boss çağırma + faz 3)")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 9000f;
            t.enemyTimePool = 10f;
            t.enemyDecayPerSecond = 0f;
            Region.Reset();

            // ── 1) ⚠️ HER BOSS FAZ 2'DE ÇAĞIRIYOR (Enes: atlanmıştı) ──
            int çağırmayan = 0;
            foreach (var b in BossKind.All)
                if (b.SummonInterval(2) <= 0f) çağırmayan++;
            failed += Check(ref total, "⚠️ HER BOSS faz 2'de yardım çağırıyor", çağırmayan == 0, çağırmayan);

            // ⚠️ Ve faz 1'de HİÇBİRİ çağırmıyor — çağırsaydı faz geçişi bir ARTIŞ
            //    olarak okunmazdı, dövüş zaten kalabalık başlardı.
            int fazBirdeÇağıran = 0;
            foreach (var b in BossKind.All) if (b.SummonInterval(1) > 0f) fazBirdeÇağıran++;
            failed += Check(ref total, "⚠️ faz 1'de KİMSE çağırmıyor (geçiş bir ARTIŞ olsun)",
                            fazBirdeÇağıran == 0, fazBirdeÇağıran);

            // ── 2) ⚠️ ÇAĞIRDIĞI TÜR SUÇUYLA UYUMLU — hepsi FARKLI ──
            //    Rastgele düşman çağırmak "boss zorlaştı" olurdu; kendi işini
            //    yapan türü çağırmak mekaniği ANLATIYOR.
            var türler = new System.Collections.Generic.HashSet<EnemyKind>();
            foreach (var b in BossKind.All) türler.Add(b.SummonKind);
            failed += Check(ref total, "⚠️ her boss FARKLI tür çağırıyor (imza, dolgu değil)",
                            türler.Count == BossKind.All.Length, türler.Count);

            // ── 3) ⚠️ ÇAĞIRMA GERÇEKTEN DOĞURUYOR — özellik değil ETKİ ──
            var (root3, clock3) = Kur(t);
            var boss3 = Boss(t, root3, BossKind.For(0));
            İlerlet(boss3, 2);

            int önce = Enemy.All.Count;
            boss3.TickBossPassives(boss3.Boss.SummonInterval(2) + 0.1f);
            failed += Check(ref total, "⚠️ boss GERÇEKTEN düşman doğurdu",
                            Enemy.All.Count > önce, Enemy.All.Count - önce);

            // ── 4) ⚠️ TAVAN İŞLİYOR — tavansız çağırma dövüşü kazanılamaz yapardı ──
            for (int i = 0; i < 30; i++)
                boss3.TickBossPassives(boss3.Boss.SummonInterval(2) + 0.1f);

            int çağrılan = 0;
            foreach (var e in Enemy.All) if (e != null && e.Summoned) çağrılan++;
            failed += Check(ref total, $"⚠️ çağırma TAVANLI ({çağrılan} ≤ {boss3.Boss.SummonMaxAlive})",
                            çağrılan <= boss3.Boss.SummonMaxAlive, çağrılan);
            Temizle(root3);

            // ── 5) ⚠️ SON BOSS'UN ÜÇ FAZI VAR, diğerlerinin İKİ ──
            var son = BossKind.For(BossKind.All.Length - 1);
            failed += Check(ref total, "⚠️ SON BOSS üç fazlı", son.PhaseCount == 3, son.PhaseCount);
            failed += Check(ref total, "⚠️ SON BOSS faz 3'te RAKİPLERİNİ çağırıyor",
                            son.SummonsRivalsInPhase == 3, son.SummonsRivalsInPhase);

            int üçFazlı = 0;
            foreach (var b in BossKind.All) if (b.PhaseCount >= 3) üçFazlı++;
            failed += Check(ref total, "üçüncü faz YALNIZCA son boss'ta (final özel kalsın)",
                            üçFazlı == 1, üçFazlı);

            // ── 6) ⚠️ FAZ 3: RAKİPLER DOĞUYOR ve BOSS UYKUYA GEÇİYOR ──
            var (root6, clock6) = Kur(t);
            var final = Boss(t, root6, son);
            İlerlet(final, 3);

            int rakip = 0;
            foreach (var e in Enemy.All) if (e != null && e.SummonedRival) rakip++;
            failed += Check(ref total, $"⚠️ faz 3'te ÖNCEKİ BOSS'LAR çağrıldı ({rakip} tane)",
                            rakip == BossKind.All.Length - 1, rakip);
            failed += Check(ref total, "⚠️ çağıran boss UYKUYA geçti", final.Dormant, 0f);

            // ── 7) ⚠️ UYKUDAKİ BOSS VURULMUYOR ──
            //    Vurulabilseydi oyuncu rakipleri es geçip doğrudan onu döverdi ve
            //    "hepsi ölünce kendisi gelsin" kuralı sessizce anlamsızlaşırdı.
            float uykuÖnce = final.TimeLeft;
            final.TakeDamage(999f);
            failed += Check(ref total, "⚠️ UYKUDAKİ boss VURULMUYOR",
                            Mathf.Approximately(final.TimeLeft, uykuÖnce), uykuÖnce - final.TimeLeft);

            // ── 8) ⚠️ UYKUDAKİ BOSS PASİFLERİNİ İŞLETMİYOR ──
            //    Rejenerasyonu uykuda işleseydi rakiplerle dövüşürken arkada
            //    sessizce dolan bir boss olurdu — dövüş kazanılamaz hale gelirdi.
            var düşür = typeof(Enemy).GetMethod("TakeDamage");
            float rejenÖnce = final.TimeLeft;
            final.TickBossPassives(5f);
            failed += Check(ref total, "⚠️ UYKUDA rejenerasyon İŞLEMİYOR",
                            final.TimeLeft <= rejenÖnce + 0.001f, final.TimeLeft - rejenÖnce);

            // ── 9) ⚠️ RAKİPLER ÖLÜNCE BOSS UYANIYOR ──
            foreach (var e in Enemy.All.ToArray())
                if (e != null && e.SummonedRival) Object.DestroyImmediate(e.gameObject);
            Enemy.All.RemoveAll(e => e == null);

            final.TickBossPassives(0.1f);
            failed += Check(ref total, "⚠️ rakipler ölünce boss UYANIYOR", !final.Dormant, 0f);

            float uyanıkÖnce = final.TimeLeft;
            final.TakeDamage(2f);
            failed += Check(ref total, "⚠️ uyanan boss ARTIK VURULUYOR",
                            final.TimeLeft < uyanıkÖnce - 0.001f, uyanıkÖnce - final.TimeLeft);

            // ── 10) ⚠️ RAKİPLER FAZ DEĞİŞTİRMİYOR — zincirleme doğum olmasın ──
            var (root10, clock10) = Kur(t);
            var yankı = Boss(t, root10, BossKind.For(0));
            yankı.MarkAsSummonedRival();
            yankı.TakeDamage(9999f);
            failed += Check(ref total, "⚠️ çağrılan rakip FAZ DEĞİŞTİRMİYOR (yankı, tam boss değil)",
                            yankı.Phase == 1, yankı.Phase);
            Temizle(root10);
            Temizle(root6);

            // ── 11) ⚠️ FAZ 3 DAĞARCIĞI: yeni saldırı VAR ve TÜM HARİTA saldırısı VAR ──
            var faz2 = son.Attacks(2);
            var faz3 = son.Attacks(3);
            failed += Check(ref total, "⚠️ faz 3'ün dağarcığı faz 2'den FARKLI",
                            !AynıAdlar(faz2, faz3), 0f);
            failed += Check(ref total, "faz 3 en az 4 saldırı (Enes: taban, hedef değil)",
                            faz3.Length >= 4, faz3.Length);

            bool haritaSaldırısı = false;
            foreach (var a in faz3)
                if (a.Angle >= 360f && a.RangeMul >= 6f && a.InnerRadius > 0f) haritaSaldırısı = true;
            failed += Check(ref total, "⚠️ TÜM HARİTAYA vuran, MİNİK GÜVENLİ noktalı saldırı var",
                            haritaSaldırısı, 0f);

            // ⚠️ Ve o saldırının hazırlığı MUTLAK ve UZUN olmalı: arenanın öbür
            //    ucundan boss'un dibine KOŞMAN gerekiyor. Çarpanla verilseydi
            //    "yeşil alana geçecek süre kalmıyor" hatası tekrarlanırdı (Enes).
            foreach (var a in faz3)
                if (a.Angle >= 360f && a.RangeMul >= 6f)
                    failed += Check(ref total, $"⚠️ harita saldırısının hazırlığı MUTLAK ve uzun ({a.WindupSeconds:0.0} sn)",
                                    a.WindupSeconds >= 1.5f, a.WindupSeconds);

            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset(); Enemy.All.Clear();

            if (failed == 0) Debug.Log($"[Faz 18 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 18 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        static bool AynıAdlar(BossAttack[] a, BossAttack[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i].Name != b[i].Name) return false;
            return true;
        }

        /// <summary>
        /// Boss'u istenen faza ÜRETİMİN yolundan getir — faz alanını elle yazmıyoruz.
        ///
        /// ⚠️ AZAR AZAR indiriliyor, tek 9999'luk vuruşla değil. İlk yazışımda tek
        /// vuruş kullanmıştım ve fullenmeyen boss faza geçer geçmez BOŞALIYORDU;
        /// boşalmış boss çağırmıyor (haklı olarak — tükenmiş bir boss yardım
        /// çağırmamalı) ve test "çağırma çalışmıyor" diyordu. Suç testin
        /// kurgusundaydı: oyuncu boss'u tek vuruşta bitirmez, YONTAR.
        /// </summary>
        static void İlerlet(Enemy boss, int hedefFaz)
        {
            int guard = 0;
            while (boss.Phase < hedefFaz && guard++ < 4000) boss.TakeDamage(0.25f);
        }

        static (GameObject, Clock) Kur(Tuning t)
        {
            Enemy.All.Clear();
            var root = new GameObject("faz18-check");
            var player = new GameObject("player"); player.transform.SetParent(root.transform);
            player.transform.position = Bootstrap.ArenaCenter;
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
            foreach (var m in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include))
                Object.DestroyImmediate(m.gameObject);
            GroundMark.ResetLiens();
            if (root != null) Object.DestroyImmediate(root);
            Clock.Reset();
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
