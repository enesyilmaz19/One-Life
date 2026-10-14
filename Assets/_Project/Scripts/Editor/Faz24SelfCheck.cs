using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 24 — DENETİM DÜZELTMELERİ (2026-09-23). İki ajanlı kod taraması ~30 hata buldu;
    /// bu kapı en ağırlarının ETKİSİNİ ölçüyor. Her kontrol ESKİ kodda kırmızı yanacak
    /// şekilde kurulu — "özellik değil etki" (CLAUDE.md tuzak 3).
    ///
    /// Taramanın asıl dersi: dört eski kapı YANLIŞ şeyi ölçtüğü için yeşildi —
    /// zincir uzunluğu VERİDEN sayılıyordu (çalışandan değil), geri sıçrama yalnızca
    /// "uzaklaştı mı" diye soruluyordu (vurdu mu diye değil), HACİZ dash-atağı
    /// `StartDash`'i atlayarak test ediliyordu. Buradakiler üretim yolunu koşturuyor.
    /// </summary>
    public static class Faz24SelfCheck
    {
        const BindingFlags Özel = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tek Can/Faz 24 öz-denetimi çalıştır (denetim düzeltmeleri)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Faz 24] Play modunda çalıştırma — test kaydı oyunun kaydına karışır.");
                return;
            }
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            SaveFile.PathOverride = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "tekcan_faz24_check.json");

            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 500f;
            t.enemyTimePool = 10f;
            t.enemyDecayPerSecond = 0f;
            t.testDefianceCount = 3;      // ölüm = yerinde diriliş (ÇAĞRI sahneyi söker)
            t.testLastStandCount = 0;
            t.testRecallCount = 0;
            Region.Reset();
            SaveFile.NewRun(t);

            // ── 1) BOSS ZİNCİRİ SONUNA KADAR OYNUYOR (C-1) ──
            //    `SetAttack` sıradaki halkayı kuruyordu, hemen ardından `_chained = null`
            //    onu siliyordu: her zincir 2. halkada kopuyordu. Faz 12 zincir boyunu
            //    VERİDEN saydığı için yeşildi.
            var (zincirSahibi, baş, boy) = EnUzunZincir();
            failed += Check(ref total, $"test kurgusu: 3+ halkalı zincir var ({zincirSahibi?.Name}, {boy})",
                            boy >= 3, boy);
            if (boy >= 3)
            {
                var (r1, c1, b1) = Kur(t, zincirSahibi);
                Özel_Çağır(b1, "SetAttack", baş);
                int oynayan = 1;
                var önceki = (BossAttack)Alan(b1, "_attack");
                for (int güvenlik = 0; güvenlik < 40; güvenlik++)
                {
                    if ((int)Alan(b1, "_repeatsLeft") <= 0 && Alan(b1, "_chained") == null) break;
                    Özel_Çağır(b1, "EnterWindup");
                    var şimdiki = (BossAttack)Alan(b1, "_attack");
                    if (şimdiki != önceki) { oynayan++; önceki = şimdiki; }
                }
                failed += Check(ref total, $"⚠️ zincirin {boy} halkasının HEPSİ oynuyor ({oynayan})",
                                oynayan == boy, oynayan);
                Temizle(r1);
            }

            // ── 2) FAZ GEÇİŞİ HAZIRLIKTAKİ SALDIRIYI SİLMİYOR (C-2) ──
            //    Son vuruş havuzu boşaltıp fazı çeviriyordu; `_attack = null` telgrafta
            //    SİMİT görünürken DÜZ KONİ indiriyordu — yeşil göbekte vurulmak.
            var (simitSahibi, simit) = İlk(a => a.InnerRadius > 0.5f && a.Angle >= 360f, 1);
            if (simit != null)
            {
                var (r2, c2, b2) = Kur(t, simitSahibi);
                Alan(b2, "_chained", simit);
                Özel_Çağır(b2, "EnterWindup");                 // telgraf: simit
                var oyuncu = r2.transform.Find("player");
                oyuncu.position = b2.transform.position + b2.transform.forward * (simit.InnerRadius * 0.4f);
                Özel_Çağır(b2, "AdvancePhase");                // faz hazırlığın ORTASINDA değişiyor
                float önce = c2.Seconds;
                Özel_Çağır(b2, "EnterStrike");
                failed += Check(ref total, "⚠️ faz hazırlığın ortasında değişince GÜVENLİ GÖBEK hâlâ güvenli",
                                Mathf.Approximately(c2.Seconds, önce), önce - c2.Seconds);
                Temizle(r2);
            }
            else failed += Check(ref total, "test kurgusu: faz 1'de simit saldırısı var", false, 0);

            // ── 3) GERİ SIÇRAMA ÖNCE VURUYOR, SONRA ÇEKİLİYOR (C-3) ──
            //    Önce sıçrayıp isabeti YENİ yerden ölçüyordu: DEVİR (menzil 4.25, sıçrama 5)
            //    hiç tutmuyordu. Faz 12 yalnızca "uzaklaştı mı" diye soruyordu.
            var (sıçrayan, geri) = İlk(a => a.Recoil > 0f, 0);
            if (geri != null)
            {
                var (r3, c3, b3) = Kur(t, sıçrayan);
                Alan(b3, "_chained", geri);
                Özel_Çağır(b3, "EnterWindup");
                var oyuncu = r3.transform.Find("player");
                oyuncu.position = b3.transform.position + b3.transform.forward * (b3.CurrentStrikeRadius * 0.5f);
                b3.transform.rotation = Quaternion.LookRotation(Düz(oyuncu.position - b3.transform.position));
                float önce = c3.Seconds;
                float mesafeÖnce = Düz(oyuncu.position - b3.transform.position).magnitude;
                Özel_Çağır(b3, "EnterStrike");
                failed += Check(ref total, $"⚠️ '{geri.Name}' ÖNÜNDEKİNİ VURUYOR",
                                c3.Seconds < önce - 0.01f, önce - c3.Seconds);
                failed += Check(ref total, "…ve SONRA çekiliyor (kovala sorusu duruyor)",
                                Düz(oyuncu.position - b3.transform.position).magnitude > mesafeÖnce + 0.5f,
                                Düz(oyuncu.position - b3.transform.position).magnitude - mesafeÖnce);
                Temizle(r3);
            }
            else failed += Check(ref total, "test kurgusu: geri sıçrayan saldırı var", false, 0);

            // ── 4) NEMESİS: öldüren vuruş SAYILIYOR, ikinci ölüm ÇİFT yazmıyor (C-6 / F-7) ──
            SaveFile.NewRun(t);
            {
                var (r4, c4, b4) = Kur(t, BossKind.For(0));
                var rev = r4.AddComponent<RevivalSystem>();
                rev.Init(t, c4);
                var pc = r4.transform.Find("player").GetComponent<PlayerController>();

                c4.Spend(c4.Seconds - 3f);                     // saatte 3 sn kaldı
                pc.TakeHit(10f, b4.transform.position, b4);     // öldüren vuruş → ÖLÜM ALDATMA
                float ilk = SaveFile.BossLedger(BossKind.For(0));
                failed += Check(ref total, $"⚠️ ÖLDÜREN vuruş hesaba yazıldı — inen 3 sn, istenen 10 değil ({ilk:0.00})",
                                Mathf.Abs(ilk - 3f) < 0.01f, ilk);
                failed += Check(ref total, "test kurgusu: yerinde dirildi", !c4.Dead, c4.Seconds);

                b4.HesabınaYaz(5f);
                c4.Spend(99999f);                               // aynı dövüşte ikinci ölüm
                float ikinci = SaveFile.BossLedger(BossKind.For(0));
                failed += Check(ref total, $"⚠️ ikinci ölüm yalnızca YENİ alacağı yazıyor (3+5=8, {ikinci:0.00})",
                                Mathf.Abs(ikinci - 8f) < 0.01f, ikinci);
                Temizle(r4);
            }

            // ── 5) TAMİR BEDELİ CHECKPOINT'TEN DÜŞÜYOR — sonsuz diriliş yok (F-2) ──
            SaveFile.NewRun(t);
            {
                var (r5, c5, _) = Kur(t, null);
                var s = SaveFile.Current;
                s.checkpointSeconds = 200f;
                s.intact[0]--; s.broken[0]++;                   // tamir edilecek bir eşya
                Bootstrap.SpawnRepairAltar(t, r5.transform.Find("player"), r5.transform);
                var sunak = Altar.All[Altar.All.Count - 1];
                float fiyat = sunak.Price;
                Özel_Çağır(sunak, "Buy");
                failed += Check(ref total, "test kurgusu: onarım satın alındı", s.broken[0] == 0, s.broken[0]);
                failed += Check(ref total, $"⚠️ bedel CHECKPOINT'ten de düştü (ÇAĞRI iade etmiyor) — {s.checkpointSeconds:0.0}",
                                Mathf.Abs(s.checkpointSeconds - (200f - fiyat)) < 0.01f, s.checkpointSeconds);
                Temizle(r5);
                Altar.ResetAll();
            }

            // ── 6) KAZANILMIŞ KOŞU ÖLMEZ (C-16 / F-8) ──
            {
                var (r6, c6, _) = Kur(t, null);
                c6.Win();
                c6.Spend(99999f);                               // zaferden sonra patlayan işaret
                failed += Check(ref total, "⚠️ zaferden sonra gelen darbe ÖLDÜRMÜYOR", !c6.Dead, c6.Seconds);
                Temizle(r6);
            }

            // ── 7) FİNAL: rakip havuzu, ikizi yankı, uyuyan boss hedef değil (C-5, C-7, C-8, C-11) ──
            SaveFile.NewRun(t);
            {
                var son = BossKind.All[BossKind.All.Length - 1];
                var (r7, c7, b7) = Kur(t, son);
                var herhangi = son.Attacks(1)[0];
                Alan(b7, "_chained", herhangi);
                Özel_Çağır(b7, "EnterWindup");                 // hazırlığın ORTASINDA uyuyor
                Özel_Çağır(b7, "ÇağırRakipleri");

                failed += Check(ref total, "⚠️ uykuya giren boss hazırlığı BIRAKIYOR (uyanınca uyarısız vurmaz)",
                                (Enemy.State)Alan(b7, "_state") == Enemy.State.Chase, (int)(Enemy.State)Alan(b7, "_state"));

                Enemy rakip = null;
                foreach (var e in Enemy.All) if (e != null && e.SummonedRival) rakip = e;
                // Sıra rastgele; KEFİL gelirse bölünüp yarıya iner — taban onu da kapsıyor.
                // Eski hâl: 10 × 0.7 = 7 sn (bölünürse 3.5). Taban bunun ~10 katı.
                float taban = t.enemyTimePool * t.bossPoolMultiplier * t.rivalPoolMultiplier * 0.45f;
                failed += Check(ref total, $"⚠️ rakibin havuzu BOSS havuzunun payı ({(rakip != null ? rakip.TimeLeft : 0f):0} sn ≥ {taban:0})",
                                rakip != null && rakip.TimeLeft >= taban, rakip != null ? rakip.TimeLeft : 0f);

                var pc = r7.transform.Find("player").GetComponent<PlayerController>();
                var args = new object[] { b7, 99f, 180f, 0f, 0f };
                bool hedef = (bool)typeof(PlayerController).GetMethod("InHitCone", Özel).Invoke(pc, args);
                failed += Check(ref total, "⚠️ UYKUDAKİ boss vuruş hedefi DEĞİL (rakibe giden vuruşu yutmuyor)",
                                b7.Dormant && !hedef, hedef ? 1 : 0);
                Temizle(r7);

                // İkiz: KEFİL yankısı faz 2'de bölünüyor — ikizi de yankı olmalı.
                var kefil = System.Array.Find(BossKind.All, k => k.SplitsOnPhaseTwo);
                var (r8, c8, b8) = Kur(t, kefil);
                b8.MarkAsSummonedRival();
                b8.StartAtPhaseTwo();
                int yankı = 0;
                foreach (var e in Enemy.All) if (e != null && e.SummonedRival) yankı++;
                failed += Check(ref total, $"⚠️ bölünen yankının İKİZİ de yankı ({yankı}/2) — final onu bekliyor",
                                yankı == 2, yankı);
                Temizle(r8);
            }

            // ── 8) HACİZ DASH-ATAĞI DİBİNDEKİNE İNİYOR (C-10) ──
            //    Faz 3 bu yolu `_dashDir` sıfırken koşturuyordu — yani hiç sınamıyordu.
            //    Faz 25: geri dash kalktı (Enes), dash-atak dash yönüne iniyor. Soru
            //    aynı: menzilli silah ÖLÜ BÖLGESİNDEKİNE dash-atakla vurabiliyor mu.
            {
                var (r9, c9, _) = Kur(t, null);
                var pGo = r9.transform.Find("player");
                var pc = pGo.GetComponent<PlayerController>();
                pGo.rotation = Quaternion.identity;
                int hacizİndis = System.Array.FindIndex(Weapon.All, w => w is Haciz);
                Alan(pc, "_weaponIndex", hacizİndis);
                Alan(pc, "_dashDir", Vector3.forward);          // dibindekine doğru dash
                var dipteki = Bootstrap.SpawnEnemyAt(t, pGo.position + Vector3.forward * (t.hacizMinRange * 0.4f),
                                                     pGo, null, parent: r9.transform);
                float önce = dipteki.TimeLeft;
                Özel_Çağır(pc, "DashStrike");
                failed += Check(ref total, "⚠️ HACİZ dash-atağı ÖLÜ BÖLGESİNDEKİNE vuruyor (yakın cevabı tutuyor)",
                                dipteki.TimeLeft < önce - 0.001f, önce - dipteki.TimeLeft);
                Temizle(r9);
            }

            // ── 9) ÖZEL SALDIRI CANLI AYARI GÖRÜYOR (C-9) ──
            //    İlk verilen Tuning önbellekte kalıyordu; Inspector köprüsü ve F2-F4
            //    özel saldırılara hiç geçmiyordu.
            {
                var t2 = Object.Instantiate(t);
                t.specialCooldownCone = 7f; t2.specialCooldownCone = 3f;
                var koni = System.Array.Find(Weapon.All, w => w is ConeBlade);
                float a = koni.Special(t).Cooldown, b = koni.Special(t2).Cooldown;
                failed += Check(ref total, $"⚠️ özel saldırı GÜNCEL ayarı okuyor ({a:0} → {b:0})",
                                !Mathf.Approximately(a, b), a - b);
                Object.DestroyImmediate(t2);
            }

            // ── 10) BEŞ YERLEŞİMİN BEŞİ DE OYUNA GİRİYOR (F-13) ──
            //     Oda 5 (= HALKA) sunak odası olunca HALKA oyundan çıkmıştı (ders 11:
            //     tanımlı ≠ ulaşılabilir). Seçiciyi ÇALIŞTIRIP çıkanı sayıyoruz.
            {
                var (r10, c10, _) = Kur(t, null);
                var rmGo = new GameObject("rooms"); rmGo.transform.SetParent(r10.transform);
                var rm = rmGo.AddComponent<RoomManager>();
                rm.Init(t, r10.transform.Find("player"), null);
                var görülen = new HashSet<string>();
                for (int bölge = 0; bölge < t.regionsPerAct; bölge++)
                {
                    typeof(RoomManager).GetMethod("EnterRegion", Özel).Invoke(rm, new object[] { bölge, 1 });
                    for (int oda = 1; oda <= t.RoomsInRegion; oda++)
                    {
                        if (RoomManager.Tür(t, oda) != RoomManager.OdaTürü.Dövüş) continue;   // Faz 28
                        typeof(RoomManager).GetMethod("EnterRoom", Özel).Invoke(rm, new object[] { oda });
                        görülen.Add(rm.Layout.Name);
                    }
                }
                failed += Check(ref total, $"⚠️ düşmanlı odalarda {görülen.Count}/{RoomLayout.All.Length} yerleşim geliyor",
                                görülen.Count == RoomLayout.All.Length, görülen.Count);
                Temizle(r10);
                Altar.ResetAll(); RoomManager.Reset();
            }

            // ── 11) KAYIT ATOMİK — yan dosya kalmıyor, üstüne yazılan okunuyor (F-5) ──
            {
                SaveFile.NewRun(t);
                SaveFile.Flush();                               // ilk yazış (Move)
                SaveFile.Current.deaths = 7;
                SaveFile.Flush();                               // üstüne yazış (Replace)
                bool yanYok = !System.IO.File.Exists(SaveFile.PathOverride + ".tmp");
                SaveFile.Load();
                failed += Check(ref total, "⚠️ kayıt yan dosyadan TEK HAMLEDE yerine geçiyor",
                                yanYok && SaveFile.LoadedFromDisk && SaveFile.Current.deaths == 7,
                                SaveFile.Current.deaths);

                // Yer değiştirme DÜŞTÜ (kilitli dosya): en yeni durum yan dosyada, ana dosya yok.
                // Yan dosya okunmasaydı koşu sessizce silinirdi — atomikliğin asıl sözü bu.
                SaveFile.Current.deaths = 9;
                System.IO.File.WriteAllText(SaveFile.PathOverride + ".tmp", JsonUtility.ToJson(SaveFile.Current));
                System.IO.File.Delete(SaveFile.PathOverride);
                SaveFile.Load();
                failed += Check(ref total, "⚠️ ana dosya gitmiş, yan dosya duruyorsa koşu YAN DOSYADAN dönüyor",
                                SaveFile.LoadedFromDisk && SaveFile.Current.deaths == 9, SaveFile.Current.deaths);
            }

            try
            {
                foreach (var yol in new[] { SaveFile.PathOverride, SaveFile.PathOverride + ".tmp" })
                    if (System.IO.File.Exists(yol)) System.IO.File.Delete(yol);
            }
            catch { /* geçici dosya, önemsiz */ }
            SaveFile.PathOverride = null;

            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset(); Enemy.All.Clear(); RevivalSystem.Reset(); Altar.ResetAll();

            if (failed == 0) Debug.Log($"[Faz 24 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 24 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── kurulum ─────────────────────────────────────────────────────────

        /// <summary>Oyuncu + saat; `kind` verilirse arenanın ortasından biraz ileride bir boss.</summary>
        static (GameObject, Clock, Enemy) Kur(Tuning t, BossKind kind)
        {
            Enemy.All.Clear();
            RevivalSystem.Reset();
            var root = new GameObject("faz24-check");
            var player = new GameObject("player"); player.transform.SetParent(root.transform);
            player.transform.position = Bootstrap.ArenaCenter;
            player.AddComponent<PlayerController>().Init(t, null);
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, t.startSeconds);

            Enemy boss = null;
            if (kind != null)
                boss = Bootstrap.SpawnEnemyAt(t, Bootstrap.ArenaCenter + Vector3.forward * 4f, player.transform,
                                              null, 1f, boss: true, parent: root.transform, bossKind: kind);
            return (root, clock, boss);
        }

        static void Temizle(GameObject root)
        {
            foreach (var e in Enemy.All.ToArray())
                if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include))
                Object.DestroyImmediate(a.gameObject);
            foreach (var m in Object.FindObjectsByType<Mote>(FindObjectsInactive.Include))
                Object.DestroyImmediate(m.gameObject);
            foreach (var g in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include))
                Object.DestroyImmediate(g.gameObject);
            if (root != null) Object.DestroyImmediate(root);
            Clock.Reset(); RevivalSystem.Reset();
        }

        // ── veri taraması ───────────────────────────────────────────────────

        static (BossKind, BossAttack, int) EnUzunZincir()
        {
            BossKind sahip = null; BossAttack baş = null; int boy = 0;
            foreach (var k in BossKind.All)
                for (int faz = 1; faz <= k.PhaseCount; faz++)
                    foreach (var a in k.Attacks(faz))
                    {
                        int n = 0;
                        for (var x = a; x != null && n < 20; x = x.Then) n++;
                        if (n > boy) { boy = n; baş = a; sahip = k; }
                    }
            return (sahip, baş, boy);
        }

        /// <summary>Şartı sağlayan ilk saldırı. `faz` 0 ise bütün fazlar taranır.</summary>
        static (BossKind, BossAttack) İlk(System.Func<BossAttack, bool> şart, int faz)
        {
            foreach (var k in BossKind.All)
                for (int f = 1; f <= k.PhaseCount; f++)
                {
                    if (faz != 0 && f != faz) continue;
                    foreach (var a in k.Attacks(f)) if (şart(a)) return (k, a);
                }
            return (null, null);
        }

        // ── yansıma ─────────────────────────────────────────────────────────

        static void Özel_Çağır(object o, string ad, params object[] args)
            => o.GetType().GetMethod(ad, Özel).Invoke(o, args.Length == 0 ? null : args);

        static object Alan(object o, string ad) => o.GetType().GetField(ad, Özel).GetValue(o);

        static void Alan(object o, string ad, object değer) => o.GetType().GetField(ad, Özel).SetValue(o, değer);

        static Vector3 Düz(Vector3 v) { v.y = 0f; return v; }

        static int Check(ref int total, string name, bool ok, float actual)
        {
            total++;
            if (ok) Debug.Log($"  ✅ {name}");
            else Debug.LogError($"  ❌ {name}  (gerçek: {actual})");
            return ok ? 0 : 1;
        }
    }
}
