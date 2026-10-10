using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 25 — BİRLEŞİK PLAYTEST DÜZELTMELERİ (2026-09-30). Her kontrol ETKİYİ ölçüyor
    /// ve eski kodda kırmızı yanacak şekilde kurulu.
    ///
    /// Ders 15: Faz 12'nin seçim kontrolü `PickAttack`'i her mesafede çağırıp "her saldırı
    /// bir yerde seçiliyor" diyordu, yeşildi; oyunda MURABAHACI uzak oyuncuya 7 saldırının
    /// 2'sini yapıyordu. Buradaki seçim kontrolleri `TickState`'i ADIM ADIM koşturuyor:
    /// boss gerçekten yürüyor, oyuncu duruyor / dibe giriyor / kaçıyor.
    /// </summary>
    public static class Faz25SelfCheck
    {
        const BindingFlags Özel = BindingFlags.NonPublic | BindingFlags.Instance;
        const float Dt = 0.02f;

        /// <summary>Uyarıyı görüp harekete geçme süresi — insan modeli.</summary>
        const float İnsanTepkisi = 0.25f;

        [MenuItem("Tek Can/Faz 25 öz-denetimi çalıştır (playtest düzeltmeleri)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Faz 25] Play modunda çalıştırma — test kaydı oyunun kaydına karışır.");
                return;
            }
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            SaveFile.PathOverride = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "tekcan_faz25_check.json");

            var t = ScriptableObject.CreateInstance<Tuning>();
            t.bossMapAttackEvery = 0;    // yakın/uzak sırası ölçülüyor; harita sırası Faz26SelfCheck'te
            t.startSeconds = 500f;
            t.enemyTimePool = 10f;
            t.enemyDecayPerSecond = 0f;
            t.testDefianceCount = 3;
            t.testLastStandCount = 0;
            t.testRecallCount = 1;
            Region.Reset();
            SaveFile.NewRun(t);

            // Tepki kontrolleri GERÇEK ayarla: "insan kaçabiliyor mu" sorusu hıza,
            // hazırlığa ve menzile bağlı — varsayılan C# değerleri oyunun değerleri değil.
            var asset = Resources.Load<Tuning>("Tuning");
            var gerçek = asset != null ? Object.Instantiate(asset) : Object.Instantiate(t);

            failed += SeçimKontrolleri(t, ref total);
            failed += TepkiKontrolleri(gerçek, ref total);
            failed += AçıklıkKontrolü(gerçek, ref total);
            failed += RütbeKontrolleri(t, ref total);
            failed += RenkKontrolü(t, ref total);
            failed += HacizDashKontrolü(t, ref total);
            failed += SimsarFazKontrolü(t, ref total);
            failed += KayıtKontrolleri(t, ref total);
            failed += TamGüçKontrolü(t, ref total);

            try
            {
                foreach (var yol in new[] { SaveFile.PathOverride, SaveFile.PathOverride + ".tmp" })
                    if (System.IO.File.Exists(yol)) System.IO.File.Delete(yol);
            }
            catch { /* geçici dosya, önemsiz */ }
            SaveFile.PathOverride = null;

            Object.DestroyImmediate(t);
            Object.DestroyImmediate(gerçek);
            Clock.Reset(); Region.Reset(); Enemy.All.Clear(); RevivalSystem.Reset(); Altar.ResetAll();
            RoomManager.Reset(); GroundMark.ResetLiens();

            if (failed == 0) Debug.Log($"[Faz 25 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 25 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── 1) SALDIRI SEÇİMİ: iki sıra, sırayla, kaçış yok ─────────────────────

        static int SeçimKontrolleri(Tuning t, ref int total)
        {
            int failed = 0;
            int uzakSırasız = 0, yakınSırasız = 0, fazSayısı = 0;

            foreach (var b in BossKind.All)
                for (int f = 1; f <= b.PhaseCount; f++)
                {
                    fazSayısı++;
                    var liste = b.Attacks(f);
                    var uzakAd = new List<string>();
                    var yakınAd = new List<string>();
                    foreach (var a in liste)
                    {
                        if (a.Tür == SaldırıTürü.Uzak) uzakAd.Add(a.Name);
                        if (a.Tür == SaldırıTürü.Yakın) yakınAd.Add(a.Name);
                    }

                    // ① UZAKTA DURAN oyuncu: boss uzaktan yürüyerek geliyor.
                    var (r1, _, b1) = Kur(t, b);
                    FazıKur(b1, f);
                    var oyuncu1 = r1.transform.Find("player");
                    var görülen = new List<string>();
                    for (int n = 0; n < uzakAd.Count; n++)
                    {
                        Sıfırla(b1);
                        oyuncu1.position = Bootstrap.ArenaCenter;
                        b1.transform.position = Bootstrap.ArenaCenter + Vector3.forward * 16f;
                        görülen.Add(SaldırıyıBekle(b1, null)?.Name ?? "—");
                    }
                    if (!AynıSıra(görülen, uzakAd))
                    {
                        uzakSırasız++;
                        Debug.LogError($"    {b.Name} faz {f} UZAK: beklenen [{string.Join(", ", uzakAd)}] " +
                                       $"görülen [{string.Join(", ", görülen)}]");
                    }
                    Temizle(r1);

                    // ② DİBE GİREN oyuncu: her adımda boss'un yarım menzilinde.
                    var (r2, _, b2) = Kur(t, b);
                    FazıKur(b2, f);
                    var oyuncu2 = r2.transform.Find("player");
                    görülen.Clear();
                    for (int n = 0; n < yakınAd.Count; n++)
                    {
                        Sıfırla(b2);
                        float yarım = b2.CurrentStrikeRadius * 0.5f;       // saldırısızken temel menzil
                        oyuncu2.position = b2.transform.position + Vector3.back * yarım;
                        görülen.Add(SaldırıyıBekle(b2, (e, o) =>
                        {
                            var yön = Düz(o.position - e.transform.position);
                            o.position = e.transform.position + (yön.sqrMagnitude > 0.0001f ? yön.normalized : Vector3.back) * yarım;
                        }, oyuncu2)?.Name ?? "—");
                    }
                    if (!AynıSıra(görülen, yakınAd))
                    {
                        yakınSırasız++;
                        Debug.LogError($"    {b.Name} faz {f} YAKIN: beklenen [{string.Join(", ", yakınAd)}] " +
                                       $"görülen [{string.Join(", ", görülen)}]");
                    }
                    Temizle(r2);
                }

            failed += Check(ref total, $"⚠️⚠️ UZAKTA duran oyuncuya o fazın BÜTÜN menzilli saldırıları SIRAYLA ({fazSayısı} faz)",
                            uzakSırasız == 0, uzakSırasız);
            failed += Check(ref total, $"⚠️⚠️ DİBE giren oyuncuya o fazın BÜTÜN yakın saldırıları SIRAYLA ({fazSayısı} faz)",
                            yakınSırasız == 0, yakınSırasız);

            // ③ KAÇAN oyuncu: boss'un sıradaki saldırısının menzilinin dışında, en uzun
            //    erişimin içinde kalıyor. Sabır olmasa hiç saldırı yemezdi.
            int denenen = 0, saldırısız = 0;
            foreach (var b in BossKind.All)
            {
                var uzaklar = System.Array.FindAll(b.Attacks(1), a => a.Tür == SaldırıTürü.Uzak);
                var (r3, _, b3) = Kur(t, b);
                Sıfırla(b3);
                float r = b3.CurrentStrikeRadius;
                float enUzun = 0f;
                foreach (var a in uzaklar) enUzun = Mathf.Max(enUzun, r * a.RangeMul + a.Lunge);
                float mesafe = enUzun - 0.3f;
                if (r * uzaklar[0].RangeMul + uzaklar[0].Lunge >= mesafe) { Temizle(r3); continue; }
                denenen++;

                var oyuncu3 = r3.transform.Find("player");
                oyuncu3.position = b3.transform.position + Vector3.back * mesafe;
                var a3 = SaldırıyıBekle(b3, (e, o) =>
                {
                    var yön = Düz(o.position - e.transform.position).normalized;
                    o.position = e.transform.position + yön * mesafe;
                }, oyuncu3, t.bossAttackPatience + 1f);
                if (a3 == null)
                {
                    saldırısız++;
                    Debug.LogError($"    {b.Name}: {mesafe:0.0} birimde kaçan oyuncu {t.bossAttackPatience + 1f:0.0} sn saldırı yemedi");
                }
                Temizle(r3);
            }
            failed += Check(ref total, $"test kurgusu: kaçış denenen boss sayısı ({denenen})", denenen >= 3, denenen);

            // ④ VERİ TUZAĞI: boss kovalarken temel menzilin %90'ında DURUYOR (`ChaseMove`).
            //    Yakın sıradaki bir saldırının erişimi bundan kısaysa boss dibinde bekler,
            //    sabır dolunca o saldırıyı atlar — saldırı sessizce ölür (ders 13).
            //    Bugün en kısası KEFALET (0.95). Biri 0.9'un altına inerse burası yakalar.
            int kısa = 0;
            foreach (var b in BossKind.All)
                for (int f = 1; f <= b.PhaseCount; f++)
                    foreach (var a in b.Attacks(f))
                        if (a.Tür == SaldırıTürü.Yakın && a.RangeMul < 0.9f)   // yakın = hamlesiz
                        {
                            kısa++;
                            Debug.LogError($"    {b.Name} faz {f}: {a.Name} erişimi boss'un durduğu mesafeye yetmiyor");
                        }
            failed += Check(ref total, "yakın sıradaki her saldırı boss'un DURDUĞU mesafeye yetişiyor (ölü saldırı yok)",
                            kısa == 0, kısa);
            failed += Check(ref total, "⚠️ KAÇAN oyuncu saldırıyı iptal edemiyor (sabır dolunca yetişen saldırı geliyor)",
                            saldırısız == 0, saldırısız);
            return failed;
        }

        // ── 2) TEPKİ PAYI: uyarıyı görüp kaçan insan koniden çıkabiliyor mu ─────

        static int TepkiKontrolleri(Tuning t, ref int total)
        {
            int failed = 0;

            // Sıradan düşman.
            {
                var (r, c, _) = Kur(t, null);
                var oyuncu = r.transform.Find("player");
                var düşman = Bootstrap.SpawnEnemyAt(t, Bootstrap.ArenaCenter + Vector3.forward * 1.2f,
                                                    oyuncu, null, parent: r.transform);
                bool kaçtı = KaçabiliyorMu(düşman, oyuncu, c, null, t);
                failed += Check(ref total, "⚠️⚠️ SIRADAN düşman: uyarıdan 0.25 sn sonra kaçan oyuncu VURULMUYOR",
                                kaçtı, kaçtı ? 1 : 0);
                Temizle(r);
            }

            // Boss'ların çarpanlı (kısa hazırlıklı) dar saldırıları — "anlık tepki"nin yaşandığı yer.
            int denenen = 0;
            var vurulan = new List<string>();
            foreach (var b in BossKind.All)
                for (int f = 1; f <= b.PhaseCount; f++)
                    foreach (var a in b.Attacks(f))
                    {
                        if (a.Angle >= 180f || a.Angle <= 0f || a.Lunge > 0f || a.Pull > 0f || a.Recoil > 0f
                            || a.Mirrored || a.InnerRadius > 0f || a.SafeSpotRadius > 0f || a.SafeAngle > 0f
                            || a.WindupSeconds > 0f) continue;
                        denenen++;
                        var (r, c, boss) = Kur(t, b);
                        FazıKur(boss, f);
                        if (!KaçabiliyorMu(boss, r.transform.Find("player"), c, a, t))
                            vurulan.Add($"{b.Name}/{a.Name}");
                        Temizle(r);
                    }
            if (vurulan.Count > 0) Debug.LogError("    kaçılamayan: " + string.Join(", ", vurulan));
            failed += Check(ref total, $"test kurgusu: kısa hazırlıklı boss konisi sayısı ({denenen})", denenen >= 10, denenen);
            failed += Check(ref total, $"⚠️⚠️ BOSS: kısa hazırlıklı konilerin hepsinden kaçılabiliyor ({denenen - vurulan.Count}/{denenen})",
                            vurulan.Count == 0, vurulan.Count);
            return failed;
        }

        /// <summary>
        /// Oyuncu düşmanın önünde, vuruş yarıçapının %60'ında. Uyarı başladıktan
        /// `İnsanTepkisi` sonra koşmaya başlıyor — iki yoldan birini deniyor: yana,
        /// ya da dışarı. Biri tutarsa kaçılabilir.
        /// </summary>
        static bool KaçabiliyorMu(Enemy e, Transform oyuncu, Clock saat, BossAttack zorla, Tuning t)
        {
            foreach (bool yana in new[] { true, false })
            {
                var başlangıç = e.transform.position;
                Sıfırla(e);
                e.transform.position = başlangıç;
                oyuncu.position = başlangıç + Vector3.back * 1f;
                if (zorla != null) Alan(e, "_chained", zorla);
                Çağır(e, "EnterWindup");
                var yön = Düz(oyuncu.position - başlangıç).normalized;
                oyuncu.position = başlangıç + yön * (e.CurrentStrikeRadius * 0.6f);

                var kaçış = yana ? Vector3.Cross(Vector3.up, yön) : yön;
                float önce = saat.Seconds, geçen = 0f;
                for (int i = 0; i < 2000 && e.CurrentState == Enemy.State.Windup; i++)
                {
                    geçen += Dt;
                    if (geçen >= İnsanTepkisi) oyuncu.position += kaçış * t.moveSpeed * Dt;
                    Çağır(e, "TickState", Dt);
                }
                bool vurulmadı = saat.Seconds >= önce - 0.001f;
                Sıfırla(e);
                e.transform.position = başlangıç;
                if (vurulmadı) return true;
            }
            return false;
        }

        // ── 3) SİMSAR'IN AÇIKLIĞI: arkasına geçen oyuncu güvende mi ─────────────

        static int AçıklıkKontrolü(Tuning t, ref int total)
        {
            int failed = 0, denenen = 0;
            var vurulan = new List<string>();
            foreach (var b in BossKind.All)
                for (int f = 1; f <= b.PhaseCount; f++)
                    foreach (var kök in b.Attacks(f))
                        for (var a = kök; a != null; a = a.Then)
                        {
                            if (a.SafeAngle <= 0f) continue;
                            denenen++;
                            var (r, c, boss) = Kur(t, b);
                            FazıKur(boss, f);
                            var oyuncu = r.transform.Find("player");
                            var merkez = boss.transform.position;

                            // Oyuncu boss'un 2.5 birim önünde — dışarı kaçmanın zor olduğu yer.
                            oyuncu.position = merkez + Vector3.back * 2.5f;
                            Alan(boss, "_chained", a);
                            Çağır(boss, "EnterWindup");
                            var önü = Düz(oyuncu.position - merkez).normalized;
                            var hedef = merkez - önü * 1f;          // boss'un 1 birim ARKASI

                            float önce = c.Seconds, geçen = 0f;
                            for (int i = 0; i < 3000 && boss.CurrentState == Enemy.State.Windup; i++)
                            {
                                geçen += Dt;
                                if (geçen >= İnsanTepkisi)
                                    oyuncu.position = Vector3.MoveTowards(oyuncu.position, hedef, t.moveSpeed * Dt);
                                Çağır(boss, "TickState", Dt);
                            }
                            if (c.Seconds < önce - 0.001f) vurulan.Add($"{b.Name}/{a.Name}");
                            Temizle(r);
                        }
            if (vurulan.Count > 0) Debug.LogError("    açıklığa varıp vurulan: " + string.Join(", ", vurulan));
            failed += Check(ref total, $"test kurgusu: güvenli açıklıklı saldırı sayısı ({denenen})", denenen >= 5, denenen);
            failed += Check(ref total, $"⚠️⚠️ AÇIKLIK yerinde duruyor: arkasına koşan oyuncu güvende ({denenen - vurulan.Count}/{denenen})",
                            vurulan.Count == 0, vurulan.Count);
            return failed;
        }

        // ── 4) ZIRHLI ve İRİ ayrışıyor ──────────────────────────────────────────

        static int RütbeKontrolleri(Tuning t, ref int total)
        {
            int failed = 0;
            var (r, c, _) = Kur(t, null);
            var oyuncu = r.transform.Find("player");

            (float havuz, float kazanç) Vur(EnemyRank rütbe)
            {
                var e = Bootstrap.SpawnEnemyAt(t, Bootstrap.ArenaCenter + Vector3.forward * 5f, oyuncu, null,
                                               parent: r.transform, rank: rütbe);
                float h0 = e.TimeLeft, s0 = c.Seconds;
                e.TakeDamage(2f);
                return (h0 - e.TimeLeft, c.Seconds - s0);
            }

            var (nHavuz, nKazanç) = Vur(EnemyRank.Normal);
            var (zHavuz, zKazanç) = Vur(EnemyRank.Armored);
            failed += Check(ref total, $"test kurgusu: normal düşmana vurmak zaman kazandırıyor ({nKazanç:0.00})",
                            nKazanç > 0.01f, nKazanç);
            failed += Check(ref total, $"⚠️ ZIRHLI aynı vuruşta AZ hasar alıyor ({zHavuz:0.00} = {nHavuz:0.00} × {t.armoredDamageTaken})",
                            Mathf.Abs(zHavuz - nHavuz * t.armoredDamageTaken) < 0.01f && zHavuz < nHavuz, zHavuz);
            failed += Check(ref total, $"⚠️ ZIRHLI vuruş başına AZ zaman veriyor ({zKazanç:0.00} < {nKazanç:0.00})",
                            Mathf.Abs(zKazanç - nKazanç * t.armoredDamageTaken) < 0.01f, zKazanç);

            float Darbe(EnemyRank rütbe)
            {
                var e = Bootstrap.SpawnEnemyAt(t, Bootstrap.ArenaCenter + Vector3.forward * 0.8f, oyuncu, null,
                                               parent: r.transform, rank: rütbe);
                oyuncu.position = Bootstrap.ArenaCenter;
                Çağır(e, "EnterWindup");
                float s0 = c.Seconds;
                Çağır(e, "EnterStrike");
                float bedel = s0 - c.Seconds;
                Object.DestroyImmediate(e.gameObject);
                return bedel;
            }
            float nDarbe = Darbe(EnemyRank.Normal), iDarbe = Darbe(EnemyRank.Huge);
            failed += Check(ref total, $"⚠️ İRİ'nin vuruşu daha PAHALI ({iDarbe:0.00} = {nDarbe:0.00} × {t.hugeHitCostMultiplier})",
                            nDarbe > 0f && Mathf.Abs(iDarbe - nDarbe * t.hugeHitCostMultiplier) < 0.01f, iDarbe);
            Temizle(r);
            return failed;
        }

        // ── 5) DÜŞMANIN İŞARETİ İPOTEK'LE KARIŞMIYOR — çizilen renk ─────────────

        static int RenkKontrolü(Tuning t, ref int total)
        {
            int failed = 0;
            var (r, _, _) = Kur(t, null);
            var oyuncu = r.transform.Find("player");

            Color ÇizilenRenk(System.Action doğur)
            {
                foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include))
                    Object.DestroyImmediate(a.gameObject);
                doğur();
                var arklar = Object.FindObjectsByType<Arc>(FindObjectsInactive.Include);
                return arklar.Length > 0 ? arklar[0].Color : Color.clear;
            }

            var düşmanın = ÇizilenRenk(() => GroundMark.Spawn(t, Bootstrap.ArenaCenter, oyuncu, 2f, 1f, 1f, r.transform));
            var oyuncunun = ÇizilenRenk(() => GroundMark.SpawnLien(t, Bootstrap.ArenaCenter, 2f, 1f, 1f, 1f, r.transform));
            GroundMark.ResetLiens();

            Color.RGBToHSV(düşmanın, out float h1, out _, out _);
            Color.RGBToHSV(oyuncunun, out float h2, out _, out _);
            float fark = Mathf.Abs(Mathf.DeltaAngle(h1 * 360f, h2 * 360f));
            failed += Check(ref total, $"⚠️ düşmanın işareti İPOTEK'ten AYRI tonda ({fark:0}° ≥ 45°)", fark >= 45f, fark);
            failed += Check(ref total, $"düşmanın işareti TEHLİKE renginde — turuncu-kırmızı ({h1 * 360f:0}°)",
                            h1 * 360f < 40f || h1 * 360f > 340f, h1 * 360f);
            Temizle(r);
            return failed;
        }

        // ── 6) HACİZ dash'i gittiğin yöne ───────────────────────────────────────

        static int HacizDashKontrolü(Tuning t, ref int total)
        {
            var (r, _, _) = Kur(t, null);
            var pc = r.transform.Find("player").GetComponent<PlayerController>();
            Alan(pc, "_weaponIndex", System.Array.FindIndex(Weapon.All, w => w is Haciz));
            Çağır(pc, "StartDash", Vector3.right);
            var yön = (Vector3)Alan(pc, "_dashDir");
            int failed = Check(ref total, $"⚠️ HACİZ dash'i GİTTİĞİN YÖNE (sağ → {yön.x:0.0})",
                               Vector3.Dot(yön, Vector3.right) > 0.99f, yön.x);
            Temizle(r);
            return failed;
        }

        // ── 7) SİMSAR faz 2'ye havuz BİTİNCE geçiyor ────────────────────────────

        static int SimsarFazKontrolü(Tuning t, ref int total)
        {
            int failed = 0;
            var simsar = System.Array.Find(BossKind.All, k => k.Name == "SİMSAR");
            var (r, _, b) = Kur(t, simsar);
            float havuz = b.TimeLeft;
            b.TakeDamage(havuz * 0.6f);
            failed += Check(ref total, "⚠️ SİMSAR yarı havuzda faz DEĞİŞTİRMİYOR", b.Phase == 1, b.Phase);
            b.TakeDamage(havuz);
            failed += Check(ref total, "⚠️ SİMSAR havuz bitince faz 2'ye geçip FULLENİYOR (kredi)",
                            b.Phase == 2 && b.TimeLeft > havuz * 0.9f && b.OnCredit, b.TimeLeft);
            Temizle(r);
            return failed;
        }

        // ── 8) K1 oda başı kayıt · K2 ÇAĞRI tabanı ─────────────────────────────

        static int KayıtKontrolleri(Tuning t, ref int total)
        {
            int failed = 0;
            SaveFile.NewRun(t);
            var s = SaveFile.Current;

            var (r, c, _) = Kur(t, null);
            var rev = r.AddComponent<RevivalSystem>();
            rev.Init(t, c);
            var rmGo = new GameObject("rooms"); rmGo.transform.SetParent(r.transform);
            var rm = rmGo.AddComponent<RoomManager>();
            rm.Init(t, r.transform.Find("player"), null);    // oda 1 = checkpoint

            // K1-a: oda girişi yazıyor
            c.Spend(c.Seconds - 321f);
            Çağır(rm, "EnterRoom", 2);
            failed += Check(ref total, $"⚠️ K1: oda girişinde ODA BAŞI kaydı yazılıyor (oda {s.resumeRoom}, {s.resumeSeconds:0} sn)",
                            s.resumeRoom == 2 && Mathf.Abs(s.resumeSeconds - 321f) < 0.01f, s.resumeSeconds);

            // K1-b: açılış oda başından, yeniden kurulum checkpoint'ten
            var açılış = Bootstrap.BaşlangıçNoktası(s, t, true);
            var kurulum = Bootstrap.BaşlangıçNoktası(s, t, false);
            failed += Check(ref total, "⚠️ K1: oyunu kapatıp AÇAN girdiği odanın başına, girdiği zamanla döner",
                            açılış.odaBaşı && açılış.oda == 2 && Mathf.Abs(açılış.saat - 321f) < 0.01f, açılış.oda);
            failed += Check(ref total, "K1: ÇAĞRI / R / F6 oda başını KULLANMIYOR (checkpoint odası)",
                            !kurulum.odaBaşı && kurulum.oda == s.checkpointRoom, kurulum.oda);

            // K1-c: yerinde diriliş oda başı zamanını düşürüyor
            float verilecek = RevivalKind.All[0].SecondsOnUse(t);
            c.Spend(99999f);                                    // ÖLÜM ALDATMA
            failed += Check(ref total, $"test kurgusu: yerinde dirildi ({RevivalKind.All[0].Name})", !c.Dead, c.Seconds);
            failed += Check(ref total, $"⚠️ K1: öl → diril → kapat-aç GİRİŞ zamanını geri vermiyor ({s.resumeSeconds:0} ≤ {verilecek:0})",
                            s.resumeSeconds <= verilecek + 0.01f && s.resumeSeconds < 321f, s.resumeSeconds);

            // K1-d: ÇAĞRI oda başı kaydını siliyor
            s.intact[0] = 0; s.intact[1] = 0; s.intact[2] = 1;
            c.Spend(99999f);
            failed += Check(ref total, "⚠️ K1: ÇAĞRI oda başı kaydını SİLİYOR (dönüş checkpoint'e)",
                            s.resumeRoom == 0 && rev.RecallCount == 1, s.resumeRoom);

            // K2-a: 4 sn'lik checkpoint, ÇAĞRI tabanla döndürüyor
            s.checkpointSeconds = 4f;
            float dönüş = Bootstrap.BaşlangıçNoktası(s, t, false).saat;
            failed += Check(ref total, $"⚠️ K2: 4 sn'lik checkpoint'e ÇAĞRI en az {t.recallMinSeconds:0} sn ile döndürüyor ({dönüş:0})",
                            dönüş >= t.recallMinSeconds - 0.01f, dönüş);

            // K2-b: onarım tabanın altına inemiyor (inseydi taban bedeli geri öderdi)
            s.broken[0] = 1; s.intact[0] = 0;
            s.checkpointSeconds = t.revivalRepairPrice + 5f;
            c.Revive(400f);
            Bootstrap.SpawnRepairAltar(t, r.transform.Find("player"), r.transform);
            var sunak = Altar.All[Altar.All.Count - 1];
            Çağır(sunak, "Buy");
            failed += Check(ref total, $"⚠️ K2: tabanın altına indirecek ONARIM satılmıyor (checkpoint {s.checkpointSeconds:0})",
                            s.broken[0] == 1 && Mathf.Abs(s.checkpointSeconds - (t.revivalRepairPrice + 5f)) < 0.01f,
                            s.broken[0]);

            // ── İnceleme (2026-10-03) bulguları ──
            // H1: onarım kalıcı; bedeli oda başı kaydında kalmazsa kapat-aç onu geri verir.
            s.checkpointSeconds = 200f;
            s.resumeRoom = 1; s.resumeSeconds = 300f;
            Çağır(sunak, "Buy");
            failed += Check(ref total, $"test kurgusu: onarım satıldı ({s.broken[0]} kırık)", s.broken[0] == 0, s.broken[0]);
            failed += Check(ref total, $"⚠️ H1: ONARIM bedeli oda başı kaydından da düşüyor ({s.resumeSeconds:0} = 300 − {t.revivalRepairPrice:0})",
                            Mathf.Abs(s.resumeSeconds - (300f - t.revivalRepairPrice)) < 0.01f, s.resumeSeconds);

            // L1: aynı checkpoint odasına yeniden giriş (kapat-aç, ÇAĞRI) checkpoint'i ezmiyor.
            s.checkpointRoom = 1; s.checkpointRegion = 0; s.checkpointSeconds = 110f;
            c.Revive(38f);                                      // yerinde dirilmiş, az zamanla
            Çağır(rm, "EnterRoom", 1);
            failed += Check(ref total, $"⚠️ L1: checkpoint odasına yeniden giriş checkpoint'i EZMİYOR ({s.checkpointSeconds:0} sn)",
                            Mathf.Abs(s.checkpointSeconds - 110f) < 0.01f, s.checkpointSeconds);

            // M1: GEÇİCİ DÖNÜŞ sayarken kapatmak = başaramamak (checkpoint), kazanınca kayıt geri.
            Çağır(rm, "EnterRoom", 2);
            s.intact[0] = 0; s.intact[1] = 1; s.intact[2] = 0;
            c.Spend(99999f);
            failed += Check(ref total, "⚠️ M1: GEÇİCİ DÖNÜŞ kurulunca oda başı kaydı siliniyor (kapatmak = başaramamak)",
                            rev.LastStandArmed && s.resumeRoom == 0, s.resumeRoom);
            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            Çağır(rm, "Update");                                // oda temizlendi
            Çağır(rev, "Update");                               // GEÇİCİ DÖNÜŞ kazanıldı
            failed += Check(ref total, $"⚠️ M1: GEÇİCİ DÖNÜŞ kazanılınca oda başı kaydı geri yazılıyor (oda {s.resumeRoom})",
                            !rev.LastStandArmed && s.resumeRoom == 2 && Mathf.Abs(s.resumeSeconds - c.Seconds) < 0.01f,
                            s.resumeRoom);

            // Sağlamlaştırma: 0 sn'lik oda başı kaydı tam saatle başlatmıyor.
            s.resumeRoom = 2; s.resumeSeconds = 0f;
            failed += Check(ref total, "0 sn'lik oda başı kaydı KULLANILMIYOR (Clock.Init 0'ı tam saat okur)",
                            !Bootstrap.BaşlangıçNoktası(s, t, true).odaBaşı, 0);

            Temizle(r);
            Altar.ResetAll(); RoomManager.Reset();
            return failed;
        }

        // ── 9) TAM GÜÇ (F7) ─────────────────────────────────────────────────────

        static int TamGüçKontrolü(Tuning t, ref int total)
        {
            int failed = 0;
            SaveFile.NewRun(t);
            var (r, c, _) = Kur(t, null);
            var rmGo = new GameObject("rooms"); rmGo.transform.SetParent(r.transform);
            var rm = rmGo.AddComponent<RoomManager>();
            rm.Init(t, r.transform.Find("player"), null);

            float hasar0 = c.AttackDamage;
            Çağır(rm, "FullPower");
            float beklenen = hasar0 + t.altarDamageStep * t.altarMaxPurchases * rm.RegionCount;
            failed += Check(ref total, $"⚠️ F7 TAM GÜÇ: bütün sunaklar alınmış gibi (hasar {c.AttackDamage:0.0} = {beklenen:0.0})",
                            Mathf.Abs(c.AttackDamage - beklenen) < 0.01f
                            && Mathf.Approximately(c.HarvestRate, t.harvestRateCap), c.AttackDamage);
            failed += Check(ref total, "⚠️ F7 koşuyu KİRLETİYOR (kayda gitmez)", SaveFile.TestJumpUsed, 0);

            Temizle(r);
            Altar.ResetAll(); RoomManager.Reset();
            SaveFile.NewRun(t);                                 // kiri temizle
            return failed;
        }

        // ── yardımcılar ─────────────────────────────────────────────────────────

        /// <summary>
        /// Boss'u kovalamadan başlat, adım adım koştur; YENİ bir saldırı hazırlığa
        /// girince onu döndür. `oyuncuyuOynat` her adımdan önce oyuncuyu yerleştirir.
        /// </summary>
        static BossAttack SaldırıyıBekle(Enemy e, System.Action<Enemy, Transform> oyuncuyuOynat,
                                         Transform oyuncu = null, float sınır = 20f)
        {
            for (float geçen = 0f; geçen < sınır; geçen += Dt)
            {
                if (oyuncuyuOynat != null) oyuncuyuOynat(e, oyuncu);
                Çağır(e, "TickState", Dt);
                if (e.CurrentState == Enemy.State.Windup) return e.CurrentAttack;
            }
            return null;
        }

        static bool AynıSıra(List<string> görülen, List<string> beklenen)
        {
            if (görülen.Count != beklenen.Count) return false;
            for (int i = 0; i < görülen.Count; i++) if (görülen[i] != beklenen[i]) return false;
            return true;
        }

        /// <summary>Saldırı durumunu temizle, bekleme bitmiş olarak kovalamaya dön.</summary>
        static void Sıfırla(Enemy e)
        {
            if (Alan(e, "_telegraph") is Arc tel && tel != null) tel.Cancel();
            Alan(e, "_telegraph", null);
            Alan(e, "_attack", null);
            Alan(e, "_chained", null);
            Alan(e, "_repeatsLeft", 0);
            Alan(e, "_sabır", 0f);
            Çağır(e, "EnterState", Enemy.State.Chase, 0f);
            Alan(e, "_attackTimer", 0f);
        }

        static void FazıKur(Enemy e, int faz)
            => typeof(Enemy).GetProperty("Phase").GetSetMethod(true).Invoke(e, new object[] { faz });

        /// <summary>Oyuncu + saat; `kind` verilirse arenanın ortasından biraz ileride bir boss.</summary>
        static (GameObject, Clock, Enemy) Kur(Tuning t, BossKind kind)
        {
            Enemy.All.Clear();
            RevivalSystem.Reset();
            var root = new GameObject("faz25-check");
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

        static void Çağır(object o, string ad, params object[] args)
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
