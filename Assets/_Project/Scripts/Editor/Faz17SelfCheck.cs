using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 17 — düşman rütbesi (ZIRHLI / İRİ), YENİDEN TASARIM.
    ///
    /// Enes playtest'te bildirdi: *"iri ve zırhlı ayrışmıyor."* Haklıydı: ilk
    /// tasarımda ikisi de HAVUZ ekseninde duruyordu (biri yavaş akan zaman, öteki
    /// büyük havuz) ve oyunda aynı şey gibi hissediliyorlardı.
    ///
    /// Yeni ayrım FARKLI EKSENLER:
    ///   ZIRHLI → MİKTAR. Çok can + yavaş. Odada birkaç tane. İmzaya DOKUNMAZ.
    ///   İRİ    → KİMLİK. Mini boss: türün imzası katlanır. Odada TEK.
    ///
    /// ⚠️ Bu denetimin kalbi: **ikisinin farklı eksende olduğunu ÖLÇMEK.** Sadece
    /// "havuzları farklı mı" diye sorsaydım eski hata aynen geçerdi — havuzları
    /// eskiden de farklıydı, ve yine de ayrışmıyorlardı.
    /// </summary>
    public static class Faz17SelfCheck
    {
        [MenuItem("Tek Can/Faz 17 öz-denetimi çalıştır (rütbe)")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 5000f;
            t.enemyTimePool = 10f;
            t.harvestRate = 0.5f;
            t.enemyDecayPerSecond = 0f;     // havuz kendiliğinden erimesin, ölçüm bozulur
            Region.Reset();

            // ── 1) ZIRHLI: DAHA ÇOK CAN, DAHA YAVAŞ (Enes'in tarifi birebir) ──
            var (root1, clock1) = Kur(t);
            var normal = Make(t, root1.transform, EnemyKind.Basic, EnemyRank.Normal);
            var zırhlı = Make(t, root1.transform, EnemyKind.Basic, EnemyRank.Armored);
            var iri = Make(t, root1.transform, EnemyKind.Basic, EnemyRank.Huge);

            failed += Check(ref total, "⚠️ ZIRHLI'nın CANI daha çok",
                            zırhlı.TimeLeft > normal.TimeLeft * 1.4f, zırhlı.TimeLeft / normal.TimeLeft);
            failed += Check(ref total, "⚠️ ZIRHLI daha YAVAŞ (bırakıp gitme seçeneği kalsın)",
                            zırhlı.CurrentSpeed < normal.CurrentSpeed * 0.9f,
                            zırhlı.CurrentSpeed / normal.CurrentSpeed);
            failed += Check(ref total, "İRİ'nin canı ZIRHLI'dan da çok",
                            iri.TimeLeft > zırhlı.TimeLeft * 1.4f, iri.TimeLeft / zırhlı.TimeLeft);
            failed += Check(ref total, "İRİ boss gibi AĞIR telegraf ediyor",
                            iri.CurrentWindup > normal.CurrentWindup * 1.2f,
                            iri.CurrentWindup / normal.CurrentWindup);
            failed += Check(ref total, "İRİ gövdesiyle bağırıyor (yarıçap)",
                            iri.Radius > zırhlı.Radius * 1.3f, iri.Radius / zırhlı.Radius);
            Temizle(root1);

            // ── 2) ⚠️⚠️ TESTİN KALBİ: İKİSİ FARKLI EKSENDE Mİ ──
            //    İRİ türün İMZASINI katlıyor, ZIRHLI katlamıyor. Bunu ölçmezsem
            //    "ayrışmıyor" hatası aynen geri gelir: havuzları eskiden de
            //    farklıydı ve yine de aynı hissediliyorlardı.
            //    Ölçü SIZINTI'nın emişi — havuz farkından TAMAMEN bağımsız.
            float emişNormal = SızıntıEmişi(t, EnemyRank.Normal);
            float emişZırhlı = SızıntıEmişi(t, EnemyRank.Armored);
            float emişİri = SızıntıEmişi(t, EnemyRank.Huge);

            failed += Check(ref total, "⚠️ ZIRHLI türün İMZASINA DOKUNMUYOR (sadece miktar)",
                            Mathf.Abs(emişZırhlı - emişNormal) < 0.01f, emişZırhlı - emişNormal);
            failed += Check(ref total, "⚠️ İRİ türün İMZASINI KATLIYOR (mini boss)",
                            emişİri > emişNormal * 1.8f, emişİri / Mathf.Max(0.001f, emişNormal));
            failed += Check(ref total, "⚠️ ZIRHLI ile İRİ FARKLI EKSENDE (ayrışma kanıtı)",
                            emişİri > emişZırhlı * 1.8f, emişİri / Mathf.Max(0.001f, emişZırhlı));

            // İmza katlanması TEK TÜRE ÖZEL değil — dört kancanın hepsinde işlemeli
            failed += Check(ref total, "İRİ SİGORTA'nın yarıçapını da büyütüyor",
                            EnemyKind.Insurer.ProtectRadius(t) * EnemyRank.Huge.TraitMultiplier(t)
                            > EnemyKind.Insurer.ProtectRadius(t) * 1.8f, 0f);
            failed += Check(ref total, "İRİ MÜBAŞİR'in tebligatını SIKLAŞTIRIYOR",
                            EnemyKind.Bailiff.RangedMarkInterval(t) / EnemyRank.Huge.TraitMultiplier(t)
                            < EnemyKind.Bailiff.RangedMarkInterval(t) * 0.6f, 0f);

            // ── 3) ⚠️ ZAMANAŞIMI: İRİ hızlı büyüyor ama TAVAN korunuyor ──
            var (root3, clock3) = Kur(t);
            var lapseN = Make(t, root3.transform, EnemyKind.Lapse, EnemyRank.Normal);
            var lapseİ = Make(t, root3.transform, EnemyKind.Lapse, EnemyRank.Huge);
            lapseN.TickKindPassives(4f);
            lapseİ.TickKindPassives(4f);
            failed += Check(ref total, "⚠️ İRİ ZAMANAŞIMI daha HIZLI büyüyor",
                            lapseİ.Growth > lapseN.Growth + 0.05f, lapseİ.Growth - lapseN.Growth);

            lapseİ.TickKindPassives(600f);
            failed += Check(ref total, "⚠️ ama TAVAN korunuyor (mini boss güvenliği delmiyor)",
                            lapseİ.Growth <= t.lapseGrowthCap + 0.001f, lapseİ.Growth);
            Temizle(root3);

            // ── 4) ZIRHLI daha çok CAN = daha çok ÖDEME (havuz hem can hem para) ──
            var (kazançN, vuruşN) = TümünüBoşalt(t, EnemyRank.Normal);
            var (kazançZ, vuruşZ) = TümünüBoşalt(t, EnemyRank.Armored);
            failed += Check(ref total, "ZIRHLI daha çok vuruş istiyor", vuruşZ > vuruşN, vuruşZ - vuruşN);
            failed += Check(ref total, "ZIRHLI daha çok ÖDÜYOR (can = para, yasa)",
                            kazançZ > kazançN * 1.4f, kazançZ / Mathf.Max(0.001f, kazançN));

            // ── 5) Boss rütbe ALMIYOR ──
            var (root5, clock5) = Kur(t);
            var boss = Bootstrap.SpawnEnemyAt(t, Bootstrap.ArenaCenter, root5.transform.Find("player"),
                                              null, 1f, boss: true, parent: root5.transform,
                                              bossKind: BossKind.For(0), rank: EnemyRank.Huge);
            failed += Check(ref total, "⚠️ BOSS rütbe almıyor (iki dayanıklılık sistemi çarpışmasın)",
                            boss.Rank == EnemyRank.Normal, boss.Rank.IsElite ? 1f : 0f);
            Temizle(root5);

            // ── 6) ⚠️ RÜTBE GÖRÜNÜYOR — ve KAÇAK'ın solmasıyla KARIŞMIYOR (ders 10) ──
            var (root6, clock6) = Kur(t);
            var g1 = Make(t, root6.transform, EnemyKind.Basic, EnemyRank.Normal);
            var g2 = Make(t, root6.transform, EnemyKind.Basic, EnemyRank.Armored);
            var g3 = Make(t, root6.transform, EnemyKind.Basic, EnemyRank.Huge);
            Color kaçanNormal = Color.Lerp(g1.KindColor, Color.white, 0.45f);   // Paint()'teki formül

            failed += Check(ref total, "⚠️ ZIRHLI normalden AYIRT EDİLİYOR",
                            Fark(g2.KindColor, g1.KindColor) > 0.25f, Fark(g2.KindColor, g1.KindColor));
            failed += Check(ref total, "⚠️ ZIRHLI, KAÇAK'ın solmasıyla KARIŞMIYOR",
                            Fark(g2.KindColor, kaçanNormal) > 0.3f, Fark(g2.KindColor, kaçanNormal));
            failed += Check(ref total, "İRİ'nin TÜR KİMLİĞİ okunur kalıyor (renk boğulmadı)",
                            Fark(g3.KindColor, g1.KindColor) < 0.7f, Fark(g3.KindColor, g1.KindColor));
            Temizle(root6);

            // ── 7) ⚠️ ODA KOMPOZİSYONU: İRİ TEK, ZIRHLI BİRKAÇ ──
            //    Enes'in kuralı birebir. Ders 11 de burada: seçiciyi ÇALIŞTIRIP say.
            var çıkan = new System.Collections.Generic.HashSet<EnemyRank>();
            int enÇokİri = 0, enÇokZırhlı = 0, rütbeliYuva = 0, toplamYuva = 0;

            var (root7, clock7) = Kur(t);
            var rm = root7.AddComponent<RoomManager>();
            rm.Init(t, root7.transform.Find("player"), null);
            var pickRank = typeof(RoomManager).GetMethod("PickRank",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var enterRegion = typeof(RoomManager).GetMethod("EnterRegion",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            for (int bölge = 0; bölge < Region.All.Length; bölge++)
            {
                enterRegion.Invoke(rm, new object[] { bölge, 1 });
                for (int oda = 1; oda <= t.roomsPerRegion; oda++)
                {
                    int odaİri = 0, odaZırhlı = 0;
                    for (int i = 0; i < 9; i++)
                    {
                        var r = (EnemyRank)pickRank.Invoke(rm, new object[] { oda, i });
                        çıkan.Add(r);
                        toplamYuva++;
                        if (r.IsElite) rütbeliYuva++;
                        if (r == EnemyRank.Huge) odaİri++;
                        else if (r == EnemyRank.Armored) odaZırhlı++;
                    }
                    enÇokİri = Mathf.Max(enÇokİri, odaİri);
                    enÇokZırhlı = Mathf.Max(enÇokZırhlı, odaZırhlı);
                }
            }

            failed += Check(ref total, "⚠️ HER RÜTBE gerçekten DOĞUYOR (tanımlı ≠ doğuyor)",
                            çıkan.Count >= EnemyRank.All.Length, çıkan.Count);
            failed += Check(ref total, "⚠️ İRİ odada TEK (mini boss kuralı)",
                            enÇokİri == 1, enÇokİri);
            failed += Check(ref total, "⚠️ ZIRHLI odada BİRKAÇ tane olabiliyor",
                            enÇokZırhlı >= 2, enÇokZırhlı);

            // Öğretim odalarında hiç yok
            enterRegion.Invoke(rm, new object[] { 0, 1 });
            bool öğretimTemiz = true;
            for (int oda = 1; oda <= t.plainRoomCount; oda++)
                for (int i = 0; i < 9; i++)
                    if (((EnemyRank)pickRank.Invoke(rm, new object[] { oda, i })).IsElite)
                        öğretimTemiz = false;
            failed += Check(ref total, "öğretim odalarında rütbe YOK", öğretimTemiz, 0f);

            Temizle(root7);
            RoomManager.Reset();

            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset(); Enemy.All.Clear();

            if (failed == 0) Debug.Log($"[Faz 17 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 17 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        /// <summary>
        /// SIZINTI'nın bir saniyede emdiği zaman — İMZANIN ölçüsü.
        /// ⚠️ Havuz farkından bağımsız: aura havuza değil SÜREYE bağlı, yani
        /// "İRİ daha kalın olduğu için daha çok emmiş" yanılgısı imkânsız.
        /// </summary>
        static float SızıntıEmişi(Tuning t, EnemyRank rank)
        {
            var (root, clock) = Kur(t);
            var e = Make(t, root.transform, EnemyKind.Leak, rank);
            float önce = clock.Seconds;
            e.ApplyAura(1f);                       // dt dışarıdan
            float emiş = önce - clock.Seconds;
            Temizle(root);
            return emiş;
        }

        static (float kazanç, int vuruş) TümünüBoşalt(Tuning t, EnemyRank rank)
        {
            var (root, clock) = Kur(t);
            var e = Make(t, root.transform, EnemyKind.Basic, rank);
            float önce = clock.Seconds;
            int vuruş = 0;
            while (!e.Hollow && vuruş < 900) { e.TakeDamage(1f); vuruş++; }
            float kazanç = clock.Seconds - önce;
            Temizle(root);
            return (kazanç, vuruş);
        }

        static float Fark(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);

        static (GameObject, Clock) Kur(Tuning t)
        {
            Enemy.All.Clear();
            var root = new GameObject("faz17-check");
            var player = new GameObject("player"); player.transform.SetParent(root.transform);
            player.transform.position = Bootstrap.ArenaCenter;
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, t.startSeconds);
            return (root, clock);
        }

        static Enemy Make(Tuning t, Transform parent, EnemyKind kind, EnemyRank rank)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(parent);
            go.transform.position = Bootstrap.ArenaCenter;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(t, parent.Find("player"), null, 1f, false, kind, null, rank);
            return e;
        }

        static void Temizle(GameObject root)
        {
            foreach (var e in Enemy.All.ToArray())
                if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            foreach (var m in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include))
                Object.DestroyImmediate(m.gameObject);
            foreach (var mo in Object.FindObjectsByType<Mote>(FindObjectsInactive.Include))
                Object.DestroyImmediate(mo.gameObject);
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include))
                Object.DestroyImmediate(a.gameObject);
            GroundMark.ResetLiens();
            Object.DestroyImmediate(root);
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
