using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 28 — YAPI + TEMPO. Enes (2026-10-12): koşu tek hikâye, 8 saat; bölge içinde ara
    /// bölgeler, her biri mini boss'la biter ve kontrol noktası olur; ölümde kaybedilen saniyeler
    /// son mini boss'un toparlanmasına yazılır, dolarsa mini boss geri gelir.
    ///
    /// Her şey ÜRETİMDEN: oda türünü gerçek `RoomManager` söylüyor, ölümü gerçek `RevivalSystem`
    /// yürütüyor, dalgaları gerçek `Update` doğuruyor, akışı gerçek `Clock.Tick` düşürüyor.
    /// </summary>
    public static class Faz28SelfCheck
    {
        const BindingFlags Özel = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tek Can/Faz 28 öz-denetimi çalıştır (yapı + tempo)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Faz 28] Play modunda çalıştırma — test kaydı oyunun kaydına karışır.");
                return;
            }
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;
            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 100f;
            t.testDefianceCount = 0; t.testLastStandCount = 0; t.testRecallCount = 0;
            Region.Reset();

            failed += Düzen(t, ref total);
            failed += MiniBoss(t, ref total);
            failed += Toparlanma(t, ref total);
            failed += Dalgalar(t, ref total);
            failed += Akış(t, ref total);
            failed += Çağrı(t, ref total);
            failed += MiniBosslar(t, ref total);
            failed += Ekonomi(t, ref total);

            Object.DestroyImmediate(t);
            Temizle(null);
            if (failed == 0) Debug.Log($"[Faz 28 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 28 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── 1) DÜZEN: [7 dövüş → mini → sunak] × 5 → ana boss ─────────────────────
        static int Düzen(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, _, rm, _) = Kur(t, 1);
            int S = t.subRegionsPerRegion, M = t.roomsPerSubRegion;
            var yanlış = new List<string>();
            var noktalar = new List<int>();
            for (int oda = 1; oda <= t.RoomsInRegion; oda++)
            {
                GitOda(rm, oda);
                // Beklenen BAĞIMSIZ hesaplanıyor (üretimin formülü kopyalanmıyor): oda sırayla sayılır.
                int blok = M + 2, yer = (oda - 1) % blok;
                string beklenen = oda == S * blok + 1 ? "boss" : yer < M ? "dövüş" : yer == M ? "mini" : "sunak";
                string gerçek = rm.IsBossRoom ? "boss" : rm.IsMiniBossRoom ? "mini" : rm.IsAltarRoom ? "sunak" : "dövüş";
                if (gerçek != beklenen) yanlış.Add($"{oda}:{gerçek}≠{beklenen}");
                if (rm.IsCheckpointRoom) noktalar.Add(oda);
            }
            if (yanlış.Count > 0) Debug.LogError("    düzen: " + string.Join(", ", yanlış));
            failed += Check(ref total, $"⚠️⚠️ bölge = [{M} dövüş → MİNİ BOSS → SUNAK] × {S} → ANA BOSS ({t.RoomsInRegion} oda)",
                            yanlış.Count == 0 && t.RoomsInRegion == S * (M + 2) + 1, yanlış.Count);
            var beklenenNokta = new List<int> { 1 };
            for (int s = 0; s < S; s++) beklenenNokta.Add((s + 1) * (M + 2));
            failed += Check(ref total, $"⚠️ kontrol noktası = bölgenin başı + her mini boss'un ardındaki sunak ({string.Join(",", noktalar)})",
                            string.Join(",", noktalar) == string.Join(",", beklenenNokta), noktalar.Count);

            // Düşman büyümesi ARA BÖLGE içinde: 2. ara bölgenin 7. odası, 1.'nin 7.'siyle aynı sayıda.
            GitOda(rm, M); int ilk7 = Canlı();
            GitOda(rm, (M + 2) + M); int ikinci7 = Canlı();
            int tavan = Mathf.RoundToInt(t.roomBaseCount + t.roomCountGrowth * (M - 1));
            failed += Check(ref total, $"⚠️ oda büyümesi ara bölge İÇİNDE (7. oda: {ilk7} = {ikinci7} ≤ {tavan}; bölge 46 oda diye patlamıyor)",
                            ilk7 == ikinci7 && ilk7 <= tavan, ikinci7);
            Temizle(root);
            return failed;
        }

        // ── 2) MİNİ BOSS: ana boss'tan dilim, tek faz, adı ekranda ───────────────
        static int MiniBoss(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, _, rm, _) = Kur(t, 1);
            GitOda(rm, RoomManager.MiniBossOdası(t, 0));
            var minis = new List<Enemy>();
            foreach (var e in Enemy.All) if (e != null && e.IsBoss) minis.Add(e);
            var mini = minis.Count == 1 ? minis[0].Boss as MiniBossKind : null;
            var ana = BossKind.For(0);
            var anaAdlar = new HashSet<string>();
            foreach (var a in ana.Attacks(1)) anaAdlar.Add(a.Name);
            bool dilim = mini != null && mini.Ana == ana && mini.Attacks(1).Length >= 2;
            if (dilim) foreach (var a in mini.Attacks(1)) if (!anaAdlar.Contains(a.Name)) dilim = false;
            failed += Check(ref total, $"⚠️⚠️ mini boss odasında TEK mini boss, ana boss'un faz 1'inden DİLİM ({(mini != null ? string.Join(", ", System.Array.ConvertAll(mini.Attacks(1), a => a.Name)) : "yok")})",
                            dilim && mini.Attacks(1).Length < ana.Attacks(1).Length, minis.Count);
            failed += Check(ref total, "mini boss TEK faz, boss listesinde DEĞİL (tekel/kapsama kapıları onu görmesin)",
                            mini != null && mini.PhaseCount == 1 && System.Array.IndexOf(BossKind.All, mini) < 0, 0);
            float beklenenHavuz = t.enemyTimePool * RoomManager.MiniBossHavuzu(t, 0, 0);
            float miniHavuz = minis.Count == 1 ? minis[0].TimeLeft : 0f;
            failed += Check(ref total, $"mini boss havuzu ana boss'tan küçük ({miniHavuz:0} = {beklenenHavuz:0} < {t.enemyTimePool * RoomManager.AnaBossHavuzu(t, 0):0})",
                            minis.Count == 1 && Mathf.Abs(minis[0].TimeLeft - beklenenHavuz) < 0.5f
                            && beklenenHavuz < t.enemyTimePool * RoomManager.AnaBossHavuzu(t, 0), minis.Count > 0 ? minis[0].TimeLeft : 0f);
            string başlık = rm.Prompt();
            failed += Check(ref total, $"mini boss'un ADI ekranda ('{başlık}')", mini != null && başlık.Contains(mini.Name), 0);
            var son = MiniBossKind.For(0, t.subRegionsPerRegion - 1, t.subRegionsPerRegion);
            failed += Check(ref total, "son mini boss ana boss'un HARİTA saldırısını gösteriyor",
                            System.Array.Exists(son.Attacks(1), a => a.Harita), son.Attacks(1).Length);

            // Mini boss ölünce sıradaki oda SUNAK ve kontrol noktası oraya yazılıyor
            ÖldürHepsini(); Tick(rm);
            bool kapı = rm.Cleared;
            rm.NextRoom();
            failed += Check(ref total, $"⚠️ mini boss ölünce kapı → SUNAK, kontrol noktası oraya (oda {SaveFile.Current.checkpointRoom})",
                            kapı && rm.IsAltarRoom && SaveFile.Current.checkpointRoom == RoomManager.SunakOdası(t, 0), SaveFile.Current.checkpointRoom);
            Temizle(root);
            return failed;
        }

        // ── 3) NEMESİS TOPARLANMASI ──────────────────────────────────────────────
        static int Toparlanma(Tuning t, ref int total)
        {
            int failed = 0;
            // Kontrol noktası: 1. mini boss'un ardındaki sunak; eşya yok.
            var (root, clock, rm, rev) = Kur(t, 1);
            var s = SaveFile.Current;
            s.checkpointRoom = RoomManager.SunakOdası(t, 0);
            s.checkpointRegion = 0;
            s.checkpointSeconds = 100f;
            int odaİçeride = RoomManager.SunakOdası(t, 0) + 2;              // 2. ara bölgenin 2. odası
            float eşik = t.enemyTimePool * RoomManager.MiniBossHavuzu(t, 0, 0) * t.miniBossRecoveryShare;

            int ölüm = 0;
            float defterİlk = -1f;
            int beklenenÖlüm = Mathf.CeilToInt(eşik / 100f);   // her ölüm 100 sn kaybettiriyor
            while (SaveFile.Current.checkpointRoom != 1 && ölüm < beklenenÖlüm + 2)
            {
                GitOda(rm, odaİçeride);
                Canlandır(t, clock, 100f);
                typeof(RoomManager).GetMethod("OpenLedger", Özel).Invoke(rm, null);   // odaya 100 sn ile girdin
                clock.Spend(999f);                                                    // öldün
                ölüm++;
                if (defterİlk < 0f) defterİlk = SaveFile.MiniDefter(0);
            }
            failed += Check(ref total, $"⚠️ eşya yokken ölüm → kontrol noktasına dönüş, oyun bitmiyor ({rev.RecallCount} dönüş)",
                            rev.RecallCount == ölüm && !SaveFile.Current.gameOver, rev.RecallCount);
            failed += Check(ref total, $"⚠️⚠️ ölümün kaybı son mini boss'un defterine yazılıyor (ilk ölüm {defterİlk:0} sn = 100)",
                            Mathf.Abs(defterİlk - 100f) < 0.5f, defterİlk);
            failed += Check(ref total, $"⚠️⚠️ defter eşiği ({eşik:0} sn) aşınca MİNİ BOSS GERİ GELİYOR: {ölüm}. ölümde kontrol noktası oda {SaveFile.Current.checkpointRoom}'e kaydı",
                            SaveFile.Current.checkpointRoom == 1 && ölüm == beklenenÖlüm, ölüm);
            failed += Check(ref total, $"geri gelme EKRANDA ('{RevivalSystem.SonGeriGelen}')",
                            RevivalSystem.SonGeriGelen != null && RevivalSystem.SonGeriGelen.Contains("ÇIRAK"), 0);

            // Geri gelen mini boss büyümüş (nemesis)
            Canlandır(t, clock, 100f);
            GitOda(rm, RoomManager.MiniBossOdası(t, 0));
            Enemy mini = null;
            foreach (var e in Enemy.All) if (e != null && e.IsBoss) mini = e;
            float taban = t.enemyTimePool * RoomManager.MiniBossHavuzu(t, 0, 0);
            failed += Check(ref total, $"⚠️ geri gelen mini boss BÜYÜMÜŞ (havuz {(mini != null ? mini.TimeLeft : 0):0} > {taban:0}, borç {(mini != null ? mini.NemesisDebt : 0):0})",
                            mini != null && mini.NemesisDebt > 0f && mini.TimeLeft > taban + 0.5f, mini != null ? mini.TimeLeft : 0f);

            // Yeniden öldürünce borcu kapanıyor
            ÖldürHepsini(); Tick(rm);
            failed += Check(ref total, $"mini boss yeniden ölünce defteri SIFIR ({SaveFile.MiniDefter(0):0})",
                            SaveFile.MiniDefter(0) < 0.01f, SaveFile.MiniDefter(0));

            // Bölgenin başındaki kontrol noktası aşınmaz
            s.checkpointRoom = 1;
            GitOda(rm, 3);
            Canlandır(t, clock, 100f);
            typeof(RoomManager).GetMethod("OpenLedger", Özel).Invoke(rm, null);
            float önce0 = SaveFile.MiniDefter(0);
            clock.Spend(999f);
            failed += Check(ref total, "bölgenin başındaki kontrol noktası AŞINMIYOR (önünde mini boss yok)",
                            SaveFile.Current.checkpointRoom == 1 && Mathf.Abs(SaveFile.MiniDefter(0) - önce0) < 0.01f, SaveFile.MiniDefter(0));

            // Ana boss ölünce bölge KALKIYOR: defterler siliniyor
            SaveFile.MiniDefterYaz(2, 50f);
            Canlandır(t, clock, 100f);
            GitOda(rm, t.RoomsInRegion);
            ÖldürHepsini(); Tick(rm);
            rm.NextRoom();
            failed += Check(ref total, $"⚠️ ana boss ölünce bölge kalkıyor: mini boss defterleri SİLİNDİ (bölge {rm.RegionIndex + 1})",
                            rm.RegionIndex == 1 && SaveFile.MiniDefter(2) < 0.01f, SaveFile.MiniDefter(2));
            Temizle(root);
            return failed;
        }

        // ── 4) DALGALAR ──────────────────────────────────────────────────────────
        static int Dalgalar(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, _, rm, _) = Kur(t, 1);
            GitOda(rm, 2);
            var dalgalar = new List<int> { Canlı() };
            bool erkenTemiz = false;
            for (int i = 1; i < t.roomWaves; i++)
            {
                ÖldürHepsini(); Tick(rm);
                if (rm.Cleared) erkenTemiz = true;
                dalgalar.Add(Canlı());
            }
            ÖldürHepsini(); Tick(rm);
            failed += Check(ref total, $"⚠️⚠️ dövüş odası {t.roomWaves} DALGA ({string.Join(" + ", dalgalar)} düşman), dalgalar bitmeden kapı açılmıyor",
                            dalgalar.Count == t.roomWaves && !dalgalar.Contains(0) && !erkenTemiz && rm.Cleared, dalgalar.Count);
            Temizle(root);
            return failed;
        }

        // ── 5) BOSS ODASINDA YAVAŞ AKIŞ (etki: saat gerçekten daha az düşüyor) ────
        static int Akış(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, clock, rm, _) = Kur(t, 1);
            // Çarpanı ODA girişi kuruyor (`RoomManager.EnterRoom`); düşmanlar ölünce dövüş dışı akış ölçülüyor.
            Canlandır(t, clock, 100f); GitOda(rm, 3); ÖldürHepsini();
            float d0 = clock.Seconds; clock.Tick(2f); float dövüşOdası = d0 - clock.Seconds;
            Canlandır(t, clock, 100f); GitOda(rm, t.RoomsInRegion); ÖldürHepsini();
            float b0 = clock.Seconds; clock.Tick(2f); float bossOdası = b0 - clock.Seconds;
            float oran = bossOdası / Mathf.Max(0.0001f, dövüşOdası);
            failed += Check(ref total, $"⚠️ boss odasında saat YAVAŞ akıyor ({bossOdası:0.00} sn / {dövüşOdası:0.00} sn = ×{oran:0.00} ≈ ×{t.bossRoomDrainMultiplier})",
                            dövüşOdası > 0f && Mathf.Abs(oran - t.bossRoomDrainMultiplier) < 0.02f, oran);

            // ⚠️ BÜYÜK SAATTE KÜÇÜK KARE: float'ta 9000 sn'lik saatten 0.0003 sn'lik kare akışı
            // yuvarlanıp bozuluyordu (bot 3600 kare/sn — geç odalarda saat akmadı, 2026-10-13).
            Canlandır(t, clock, 9000f); GitOda(rm, 3); ÖldürHepsini();
            float k0 = clock.Seconds, r0 = clock.RunTime;
            for (int i = 0; i < 10000; i++) clock.Tick(0.0003f);
            float büyükSaat = k0 - clock.Seconds, beklenen = dövüşOdası / 2f * 3f;
            failed += Check(ref total, $"⚠️ 9000 sn'lik saatte 10 000 küçük kare doğru akıyor ({büyükSaat:0.00} ≈ {beklenen:0.00} sn, süre {clock.RunTime - r0:0.00} ≈ 3)",
                            Mathf.Abs(büyükSaat - beklenen) < 0.02f * beklenen && Mathf.Abs(clock.RunTime - r0 - 3f) < 0.01f, büyükSaat);
            Temizle(root);
            return failed;
        }

        // ── 6) ÇAĞRI bir ÖNCEKİ odaya (Enes 2026-10-14) ──────────────────────────
        static int Çağrı(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, clock, rm, rev) = Kur(t, 1);
            var s = SaveFile.Current;
            s.intact[2] = 1;                                   // yalnızca ÇAĞRI
            Canlandır(t, clock, 100f); GitOda(rm, 3);          // oda 3'ün başı 100 sn
            Canlandır(t, clock, 150f); GitOda(rm, 4);          // oda 4'ün başı 150 sn
            clock.Spend(999f);
            var nokta = Bootstrap.BaşlangıçNoktası(s, t, true);
            failed += Check(ref total, $"⚠️⚠️ ÇAĞRI bir ÖNCEKİ odanın başına döndürüyor (oda 4'te öldün → oda {nokta.oda}, {nokta.saat:0} sn; beklenen 3, 100)",
                            nokta.odaBaşı && nokta.oda == 3 && Mathf.Abs(nokta.saat - 100f) < 0.5f && rev.RecallCount == 1, nokta.oda);

            // Mini boss geri geldiyse ÇAĞRI onun ötesine atlatamaz → kontrol noktası geri kaydı
            s.intact[2] = 1;
            s.checkpointRoom = RoomManager.SunakOdası(t, 0); s.checkpointRegion = 0;
            float eşik = t.enemyTimePool * RoomManager.MiniBossHavuzu(t, 0, 0) * t.miniBossRecoveryShare;
            SaveFile.MiniDefterYaz(0, eşik);                    // defter dolu: bu ölüm taşırır
            Canlandır(t, clock, 100f); GitOda(rm, RoomManager.SunakOdası(t, 0) + 2);
            typeof(RoomManager).GetMethod("OpenLedger", Özel).Invoke(rm, null);
            clock.Spend(999f);
            failed += Check(ref total, $"⚠️ mini boss GERİ GELDİYSE ÇAĞRI kontrol noktasına (oda başı {s.resumeRoom} = 0, kontrol noktası {s.checkpointRoom} = 1)",
                            s.resumeRoom == 0 && s.checkpointRoom == 1, s.resumeRoom);
            Temizle(root);
            return failed;
        }

        // ── 7) 5 BÖLGE × 5 MİNİ BOSS: her biri kendi bölgesinin boss'undan, dağarcıkları farklı ──
        static int MiniBosslar(Tuning t, ref int total)
        {
            int failed = 0, S = Mathf.Max(1, t.subRegionsPerRegion);
            var sorunlar = new List<string>();
            for (int b = 0; b < BossKind.All.Length; b++)
            {
                var imzalar = new HashSet<string>();
                var satır = new List<string>();
                for (int s = 0; s < S; s++)
                {
                    var m = MiniBossKind.For(b, s, S);
                    var adlar = new List<string>();
                    foreach (var a in m.Attacks(1)) adlar.Add(a.Name);
                    string imza = string.Join(" + ", adlar);
                    satır.Add($"{m.Name}: {imza}");
                    var sıralı = new List<string>(adlar); sıralı.Sort();
                    if (m.Ana != BossKind.For(b) || adlar.Count < 2 || !imzalar.Add(string.Join("+", sıralı)))
                        sorunlar.Add($"B{b + 1}.{s + 1} {m.Name}");
                }
                Debug.Log($"[Faz 28] MİNİ BOSS'LAR bölge {b + 1}: {string.Join(" · ", satır)}");
            }
            failed += Check(ref total, $"⚠️ {BossKind.All.Length} bölgenin hepsinde {S} mini boss, her biri o bölgenin boss'undan, DAĞARCIKLARI farklı" +
                                       (sorunlar.Count > 0 ? $" — aynı/eksik: {string.Join(", ", sorunlar)}" : ""),
                            sorunlar.Count == 0, sorunlar.Count);
            return failed;
        }

        // ── 8) EKONOMİ (Enes 2026-10-14): doyma · sunak yüzdesi · ilerleme — hepsi ETKİ ──
        static int Ekonomi(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, clock, rm, _) = Kur(t, 3);
            Enemy e = null;
            foreach (var x in Enemy.All)
                if (x != null && !x.IsBoss && (x.Kind == null || x.Kind.HarvestMultiplier > 0f)) { e = x; break; }

            // DOYMA: aynı vuruş, saat tavanın altında ve 2 katında — kazanç yarıya iner
            float tavan = t.harvestSoftCap;
            Canlandır(t, clock, tavan * 0.5f);
            float h0 = clock.TotalHarvested; e.TakeDamage(1f); float tam = clock.TotalHarvested - h0;
            Canlandır(t, clock, tavan * 2f);
            h0 = clock.TotalHarvested; e.TakeDamage(1f); float dolu = clock.TotalHarvested - h0;
            failed += Check(ref total, $"⚠️⚠️ HASAT DOYMASI: saat tavanın 2 katıyken aynı vuruş yarı kazandırıyor ({dolu:0.000} / {tam:0.000})",
                            tam > 0f && Mathf.Abs(dolu / tam - 0.5f) < 0.01f, dolu / Mathf.Max(0.0001f, tam));

            // SUNAK: fiyat en az saatin yüzdesi; az saatte taban
            Canlandır(t, clock, 1000f);
            float zengin = clock.SıradakiFiyat(Altar.Kind.Damage);
            Canlandır(t, clock, 20f);
            float fakir = clock.SıradakiFiyat(Altar.Kind.Damage);
            failed += Check(ref total, $"⚠️ SUNAK fiyatı saatin %{t.altarPricePercent * 100f:0}'u ({zengin:0} sn @1000), az saatte taban ({fakir:0} = {t.altarBasePrice:0})",
                            Mathf.Abs(zengin - 1000f * t.altarPricePercent) < 0.01f && Mathf.Abs(fakir - t.altarBasePrice) < 0.01f, zengin);

            // İLERLEME: AYNI düşmanın vuruşu 2. bölgenin 3. ara bölgesinde ×büyüme^(S+2)
            var bedel = typeof(Enemy).GetProperty("HitCost", Özel);
            var oda = typeof(RoomManager).GetProperty("Room");
            var bölge = typeof(RoomManager).GetProperty("RegionIndex");
            float c0 = (float)bedel.GetValue(e);
            oda.SetValue(rm, RoomManager.SunakOdası(t, 1) + 3);
            bölge.SetValue(rm, 1);
            float c1 = (float)bedel.GetValue(e);
            float beklenen = Mathf.Pow(t.hitCostProgressGrowth, Mathf.Max(1, t.subRegionsPerRegion) + 2);
            failed += Check(ref total, $"⚠️ İLERLEME: aynı düşmanın vuruşu ileride pahalı ({c0:0.00} → {c1:0.00} sn = ×{c1 / Mathf.Max(0.0001f, c0):0.00} ≈ ×{beklenen:0.00})",
                            c0 > 0f && Mathf.Abs(c1 / c0 - beklenen) < 0.01f, c1 / Mathf.Max(0.0001f, c0));
            Temizle(root);
            return failed;
        }

        // ── yardımcılar ──────────────────────────────────────────────────────────
        static (GameObject, Clock, RoomManager, RevivalSystem) Kur(Tuning t, int oda)
        {
            Temizle(null);
            SaveFile.UseInMemory(new SaveData());
            var root = new GameObject("faz28-check");
            var player = new GameObject("player"); player.transform.SetParent(root.transform);
            player.AddComponent<PlayerController>().Init(t, null);
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, t.startSeconds);
            var rev = root.AddComponent<RevivalSystem>();
            rev.Init(t, clock);
            var rmGo = new GameObject("rooms"); rmGo.transform.SetParent(root.transform);
            var rm = rmGo.AddComponent<RoomManager>();
            rm.Init(t, player.transform, null, startRoom: oda);
            return (root, clock, rm, rev);
        }

        /// <summary>Saati canlandır: ölüm testinden sonra `RoomManager.Update` ölü saatte erken dönüyor.</summary>
        static void Canlandır(Tuning t, Clock c, float saniye)
            => c.Init(t, Object.FindFirstObjectByType<PlayerController>().transform, saniye);

        static void GitOda(RoomManager rm, int oda) => typeof(RoomManager).GetMethod("EnterRoom", Özel).Invoke(rm, new object[] { oda });
        static void Tick(RoomManager rm) => typeof(RoomManager).GetMethod("Update", Özel).Invoke(rm, null);
        static int Canlı() { int n = 0; foreach (var e in Enemy.All) if (e != null) n++; return n; }

        static void ÖldürHepsini()
        {
            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
        }

        static void Temizle(GameObject root)
        {
            ÖldürHepsini();
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include)) Object.DestroyImmediate(a.gameObject);
            foreach (var g in Object.FindObjectsByType<ExitGate>(FindObjectsInactive.Include)) Object.DestroyImmediate(g.gameObject);
            if (root != null) Object.DestroyImmediate(root);
            Clock.Reset(); RoomManager.Reset(); Region.Reset(); RevivalSystem.Reset(); Altar.ResetAll();
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
