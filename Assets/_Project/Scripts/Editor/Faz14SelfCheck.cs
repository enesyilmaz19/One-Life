using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 14 — düşman rütbesi (ZIRHLI / İRİ). Korunan kurallar:
    ///
    ///   • ⚠️ ZIRHLI ödülü DEĞİL, TEMPOYU kısıyor — toplam ödeme aynı kalmak ZORUNDA
    ///   • ⚠️ İRİ ise gerçekten DAHA ÇOK ödüyor — ikisi farklı şeyler
    ///   • ⚠️ Rütbe GÖRÜNÜYOR, ve KAÇAK'ın solmasıyla karışmıyor
    ///   • ⚠️ Rütbeler oyunda GERÇEKTEN doğuyor (ders 11: tanımlı ≠ doğuyor)
    ///   • Boss rütbe almıyor — iki dayanıklılık sistemi çarpışmasın
    /// </summary>
    public static class Faz14SelfCheck
    {
        [MenuItem("Tek Can/Faz 14 öz-denetimi çalıştır")]
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

            // ── 1) ⚠️ ZIRHLI: aynı hasar DAHA AZ zaman koparıyor ──
            var (root1, clock1) = Kur(t);
            var normal = Make(t, root1.transform, EnemyKind.Basic, EnemyRank.Normal);
            var zirhli = Make(t, root1.transform, EnemyKind.Basic, EnemyRank.Armored);

            float nÖnce = normal.TimeLeft, zÖnce = zirhli.TimeLeft;
            normal.TakeDamage(2f);
            zirhli.TakeDamage(2f);
            float nAkan = nÖnce - normal.TimeLeft, zAkan = zÖnce - zirhli.TimeLeft;

            failed += Check(ref total, "⚠️ ZIRHLI'dan aynı hasarda DAHA AZ zaman akıyor",
                            zAkan < nAkan - 0.01f, nAkan - zAkan);
            failed += Check(ref total, "ZIRHLI'nın HAVUZU normalle aynı (zırh can değil)",
                            Mathf.Abs(zÖnce - nÖnce) < 0.001f, zÖnce - nÖnce);
            Temizle(root1);

            // ── 2) ⚠️ TESTİN KALBİ: ZIRHLI toplamda AYNI ödüyor, sadece GEÇ ödüyor ──
            //    Zırhı havuzu küçülterek yapsaydım "daha az veren düşman" olurdu —
            //    bambaşka ve çok daha kötü bir şey. Ölçüm o farkı tutuyor.
            var (kazançN, vuruşN) = TümünüBoşalt(t, EnemyKind.Basic, EnemyRank.Normal);
            var (kazançZ, vuruşZ) = TümünüBoşalt(t, EnemyKind.Basic, EnemyRank.Armored);
            var (kazançİ, vuruşİ) = TümünüBoşalt(t, EnemyKind.Basic, EnemyRank.Heavy);

            failed += Check(ref total, "⚠️ ZIRHLI TOPLAMDA aynı ödüyor (ödül değil TEMPO vergisi)",
                            Mathf.Abs(kazançZ - kazançN) < 0.05f, kazançZ - kazançN);
            failed += Check(ref total, "⚠️ ZIRHLI çok daha ÇOK VURUŞ istiyor",
                            vuruşZ > vuruşN * 1.5f, (float)vuruşZ / Mathf.Max(1, vuruşN));

            // ── 3) ⚠️ İRİ FARKLI BİR ŞEY: daha çok vuruş AMA daha çok ödeme ──
            failed += Check(ref total, "⚠️ İRİ gerçekten DAHA ÇOK ödüyor (taahhüt, vergi değil)",
                            kazançİ > kazançN * 1.5f, kazançİ / Mathf.Max(0.001f, kazançN));
            failed += Check(ref total, "İRİ de daha çok vuruş istiyor",
                            vuruşİ > vuruşN, vuruşİ - vuruşN);

            // İkisi AYNI ŞEY olsaydı denetim bunu yakalamalı: zırh ödemeyi artırmıyor.
            failed += Check(ref total, "⚠️ ZIRHLI ile İRİ AYNI ŞEY DEĞİL (ödemeleri ayrışıyor)",
                            kazançİ > kazançZ * 1.5f, kazançİ - kazançZ);

            // ── 4) İRİ'nin gövdesi büyük — isabet yarıçapı da onunla büyüyor ──
            var (root4, clock4) = Kur(t);
            var düz = Make(t, root4.transform, EnemyKind.Basic, EnemyRank.Normal);
            var iri = Make(t, root4.transform, EnemyKind.Basic, EnemyRank.Heavy);
            failed += Check(ref total, "İRİ'nin isabet YARIÇAPI da büyüyor (gövde yalan söylemiyor)",
                            iri.Radius > düz.Radius * 1.1f, iri.Radius / düz.Radius);
            Temizle(root4);

            // ── 5) ⚠️ RÜTBE GÖRÜNÜYOR — ve KAÇAK'ın solmasıyla KARIŞMIYOR ──
            //    Ders 10: mekanik doğru ama görsel yalan söylüyorsa hata görselde.
            //    "Niye ölmüyor bu?" sorusunun cevabı EKRANDA olmalı.
            var (root5, clock5) = Kur(t);
            var g1 = Make(t, root5.transform, EnemyKind.Basic, EnemyRank.Normal);
            var g2 = Make(t, root5.transform, EnemyKind.Basic, EnemyRank.Armored);
            var g3 = Make(t, root5.transform, EnemyKind.Basic, EnemyRank.Heavy);

            Color kaçanNormal = Color.Lerp(g1.KindColor, Color.white, 0.45f);   // Paint()'teki formül

            failed += Check(ref total, "⚠️ ZIRHLI normalden AYIRT EDİLİYOR",
                            Fark(g2.KindColor, g1.KindColor) > 0.25f,
                            Fark(g2.KindColor, g1.KindColor));
            failed += Check(ref total, "⚠️ ZIRHLI, KAÇAK'ın solmasıyla KARIŞMIYOR",
                            Fark(g2.KindColor, kaçanNormal) > 0.3f,
                            Fark(g2.KindColor, kaçanNormal));
            failed += Check(ref total, "İRİ'nin TÜR KİMLİĞİ okunur kalıyor (renk boğulmadı)",
                            Fark(g3.KindColor, g1.KindColor) < 0.6f,
                            Fark(g3.KindColor, g1.KindColor));
            Temizle(root5);

            // ── 6) Boss rütbe ALMIYOR ──
            var (root6, clock6) = Kur(t);
            var boss = Bootstrap.SpawnEnemyAt(t, Bootstrap.ArenaCenter, root6.transform.Find("player"),
                                              null, 1f, boss: true, parent: root6.transform,
                                              bossKind: BossKind.For(0), rank: EnemyRank.Armored);
            failed += Check(ref total, "⚠️ BOSS rütbe almıyor (iki dayanıklılık sistemi çarpışmasın)",
                            boss.Rank == EnemyRank.Normal, boss.Rank.IsElite ? 1f : 0f);
            Temizle(root6);

            // ── 7) ⚠️ RÜTBELER GERÇEKTEN DOĞUYOR — ders 11 ──
            //    Faz 13'te yedinci tür tanımlıydı, testleri geçiyordu, HİÇ DOĞMUYORDU.
            //    Aynı hatayı rütbede tekrarlamamak için seçiciyi ÇALIŞTIRIP sayıyoruz.
            var çıkan = new System.Collections.Generic.HashSet<EnemyRank>();
            int rütbeli = 0, toplamYuva = 0;
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
                    for (int i = 0; i < 8; i++)
                    {
                        var r = (EnemyRank)pickRank.Invoke(rm, new object[] { oda, i });
                        çıkan.Add(r);
                        toplamYuva++;
                        if (r.IsElite) rütbeli++;
                    }
            }

            failed += Check(ref total, "⚠️ HER RÜTBE gerçekten DOĞUYOR (tanımlı ≠ doğuyor)",
                            çıkan.Count >= EnemyRank.All.Length, çıkan.Count);
            if (çıkan.Count < EnemyRank.All.Length)
                foreach (var r in EnemyRank.All)
                    if (!çıkan.Contains(r)) Debug.LogError($"     hiç doğmayan rütbe: {r.Name}");

            float oran = rütbeli / (float)toplamYuva;
            failed += Check(ref total, $"⚠️ rütbe SEYREK — düşmanların ~%{oran * 100:0}'i " +
                                       "(her odada olsaydı yeni taban olurdu)",
                            oran > 0.02f && oran < 0.25f, oran);

            // Öğretim odalarında hiç yok — oyun önce kendini öğretiyor
            enterRegion.Invoke(rm, new object[] { 0, 1 });
            bool öğretimTemiz = true;
            for (int oda = 1; oda <= t.plainRoomCount; oda++)
                for (int i = 0; i < 8; i++)
                    if (((EnemyRank)pickRank.Invoke(rm, new object[] { oda, i })).IsElite)
                        öğretimTemiz = false;
            failed += Check(ref total, "öğretim odalarında rütbe YOK", öğretimTemiz, 0f);

            Temizle(root7);
            RoomManager.Reset();

            // ── 8) KASA ile birleşim: zırh ödemeyi değiştirmiyor, İRİ değiştiriyor ──
            //    KASA bütün ödemesini ÖLÜMDE yapıyor — zırh oraya dokunmamalı.
            float kasaN = KasaÖdemesi(t, EnemyRank.Normal);
            float kasaZ = KasaÖdemesi(t, EnemyRank.Armored);
            float kasaİ = KasaÖdemesi(t, EnemyRank.Heavy);
            failed += Check(ref total, "⚠️ ZIRHLI KASA aynı ödüyor (zırh ödüle dokunmuyor)",
                            Mathf.Abs(kasaZ - kasaN) < 0.05f, kasaZ - kasaN);
            failed += Check(ref total, "İRİ KASA daha çok ödüyor",
                            kasaİ > kasaN * 1.5f, kasaİ / Mathf.Max(0.001f, kasaN));

            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset(); Enemy.All.Clear();

            if (failed == 0) Debug.Log($"[Faz 14 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 14 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        /// <summary>
        /// Havuzu tamamen boşaltır; TOPLAM kazancı ve kaç vuruş sürdüğünü döner.
        /// ⚠️ Özellik değil ETKİ: "DrainMultiplier 0.45 mi" diye sormuyor,
        /// saatin ne kadar dolduğunu ölçüyor.
        /// </summary>
        static (float kazanç, int vuruş) TümünüBoşalt(Tuning t, EnemyKind kind, EnemyRank rank)
        {
            var (root, clock) = Kur(t);
            var e = Make(t, root.transform, kind, rank);
            float önce = clock.Seconds;
            int vuruş = 0;
            while (!e.Hollow && vuruş < 500) { e.TakeDamage(1f); vuruş++; }
            float kazanç = clock.Seconds - önce;
            Temizle(root);
            return (kazanç, vuruş);
        }

        /// <summary>KASA'nın ölüm ödemesi — vurarak değil, ÖLDÜREREK alınan.</summary>
        static float KasaÖdemesi(Tuning t, EnemyRank rank)
        {
            var (root, clock) = Kur(t);
            var e = Make(t, root.transform, EnemyKind.Vault, rank);
            float önce = clock.Seconds;
            int guard = 0;
            while (e != null && !e.Hollow && guard++ < 500) e.TakeDamage(1f);
            if (e != null) e.TakeDamage(1f);          // hollow'a inen son vuruş öldürüyor
            float ödeme = clock.Seconds - önce;
            Temizle(root);
            return ödeme;
        }

        static float Fark(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);

        static (GameObject, Clock) Kur(Tuning t)
        {
            Enemy.All.Clear();
            var root = new GameObject("faz14-check");
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
            // Hasat zerreleri sahnede birikmesin — sonraki ölçüm onları saymasın.
            foreach (var m in Object.FindObjectsByType<Mote>(FindObjectsInactive.Include))
                Object.DestroyImmediate(m.gameObject);
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include))
                Object.DestroyImmediate(a.gameObject);
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
