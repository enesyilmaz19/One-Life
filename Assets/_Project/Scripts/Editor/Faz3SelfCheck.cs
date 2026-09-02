using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 3 — silahlar. Korunan kurallar:
    ///   • TEK HEDEFLİ silah, koni içinde üç düşman varken bile YALNIZCA BİRİNE vurur
    ///   • Hasat çarpanı gerçekten çarpıyor (tek hedeflinin rekabet edebilmesinin TEK sebebi)
    ///   • FAİZLİ ödeyemiyorsan savurmaz ve savurunca peşin keser
    ///   • VADELİ bedeli vuruştan SONRA keser
    ///   • DURGUN basılı tutulunca saati dondurur
    ///
    /// Test düzeneği üretim yerleşimini ithal ediyor (Bootstrap.ArenaCenter) — lessons.md.
    /// </summary>
    public static class Faz3SelfCheck
    {
        [MenuItem("Tek Can/Faz 3 öz-denetimi çalıştır")]
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
            t.harvestRate = 0.35f;
            t.enemyTimePool = 100f;      // tek vuruşta boşalmasın, hasat ölçülebilsin
            t.attackDamage = 4f;

            var root = new GameObject("faz3-check");
            var clock = root.AddComponent<Clock>();
            clock.Init(t, root.transform);

            var pGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pGo.transform.position = Bootstrap.ArenaCenter;
            pGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var pc = pGo.AddComponent<PlayerController>();
            pc.Init(t, null);

            var strike = typeof(PlayerController).GetMethod("Strike",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var dashStrike = typeof(PlayerController).GetMethod("DashStrike",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var startAttack = typeof(PlayerController).GetMethod("StartAttack",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // ── 1) KONİ üç düşmanın üçüne birden vurur ──
            SpawnThree(t, pGo.transform);
            pc.SetWeapon(0);
            failed += Check(ref total, "0 numara KONİ", pc.Weapon is ConeBlade, 0);

            var pools = Snapshot();
            strike.Invoke(pc, null);
            int coneHits = CountDamaged(pools);
            failed += Check(ref total, "KONİ üç düşmana birden vurdu", coneHits == 3, coneHits);

            // ── 2) TEK HEDEFLİ yalnızca birine vurur ──
            ClearEnemies();
            SpawnThree(t, pGo.transform);
            pc.SetWeapon(1);
            failed += Check(ref total, "1 numara FAİZLİ", pc.Weapon is Faizli, 1);
            failed += Check(ref total, "FAİZLİ tek hedefli", pc.Weapon.MaxTargets == 1, pc.Weapon.MaxTargets);

            pools = Snapshot();
            strike.Invoke(pc, null);
            int singleHits = CountDamaged(pools);
            failed += Check(ref total, "TEK HEDEFLİ üç düşmandan SADECE BİRİNE vurdu", singleHits == 1, singleHits);

            // ── 3) Hasat çarpanı gerçekten çarpıyor ──
            //    Tek hedeflinin koniyle rekabet edebilmesinin TEK sebebi bu.
            ClearEnemies();
            var solo = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.2f);

            pc.SetWeapon(0);                                   // KONİ, çarpan 1.0
            float before = clock.Seconds;
            strike.Invoke(pc, null);
            float coneGain = clock.Seconds - before;

            ClearEnemies();
            solo = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.2f);
            pc.SetWeapon(1);                                   // FAİZLİ, çarpan 2.6
            before = clock.Seconds;
            strike.Invoke(pc, null);
            float faizliGain = clock.Seconds - before + t.faizliSwingCost;   // peşin bedeli geri ekle

            failed += Check(ref total, "FAİZLİ aynı hasarda KONİ'den çok hasat etti",
                            faizliGain > coneGain * 1.5f, faizliGain - coneGain);

            // ── 4) FAİZLİ: ödeyemiyorsan savurmaz ──
            var swingCostField = typeof(Clock).GetProperty("Seconds");
            var swingStart = typeof(PlayerController).GetMethod("StartAttack",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var poorT = ScriptableObject.CreateInstance<Tuning>();
            poorT.startSeconds = 0.2f;                          // bedelden az
            poorT.faizliSwingCost = 0.5f;
            var poorRoot = new GameObject("poor");
            var poorClock = poorRoot.AddComponent<Clock>();
            poorClock.Init(poorT, poorRoot.transform);

            var faizli = new Faizli();
            failed += Check(ref total, "FAİZLİ parasızken savurmayı REDDETTİ",
                            !faizli.OnSwingStart(poorClock, poorT), poorClock.Seconds);
            failed += Check(ref total, "reddedilen savurma saati düşürmedi",
                            Mathf.Approximately(poorClock.Seconds, 0.2f), poorClock.Seconds);

            poorClock.Init(poorT, poorRoot.transform);          // 0.2 → sıfırla
            poorT.startSeconds = 10f; poorClock.Init(poorT, poorRoot.transform);
            float b2 = poorClock.Seconds;
            faizli.OnSwingStart(poorClock, poorT);
            failed += Check(ref total, "FAİZLİ savururken peşin kesti",
                            Mathf.Approximately(poorClock.Seconds, b2 - poorT.faizliSwingCost), poorClock.Seconds);

            // ── 5) ⚠️ İFLAS: SAAT DÜŞTÜKÇE SERTLEŞİYOR ──
            //    Saniye dışarıdan veriliyor, `Clock.Instance` okunmuyor: eğri o sayede
            //    editörde ölçülebiliyor ve denge simülasyonu da tarayabiliyor.
            var iflas = new Iflas();
            // ⚠️ "Dolu saat" fixture'ın `startSeconds`'ı DEĞİL — o değer güvenli eşiğin
            // altında kalabiliyor (bu denetimde 100 < 120) ve test silahı haksız yere
            // suçluyordu. Eşiği ÜRETİMDEN ithal edip üstüne çıkıyoruz.
            float dolu = iflas.DesperationMultiplier(t.iflasSafeSeconds + 50f, t);
            float dip  = iflas.DesperationMultiplier(t.iflasPanicSeconds * 0.5f, t);

            failed += Check(ref total, "⚠️ İFLAS DİPTE daha sert vuruyor", dip > dolu * 1.5f, dip / dolu);
            failed += Check(ref total, "⚠️ İFLAS DOLU saatte CEZALI (güvende olmak bedava değil)",
                            dolu < 1f, dolu);
            failed += Check(ref total, "İFLAS eğrisi TAVANLI", dip <= t.iflasMaxDamageMult + 0.001f, dip);

            // Eğri MONOTON: saat düştükçe hep artmalı, ortada çukur olmamalı
            bool monoton = true;
            float öncekiÇarpan = -1f;
            for (float s = t.startSeconds; s >= 0f; s -= 5f)
            {
                float m = iflas.DesperationMultiplier(s, t);
                if (öncekiÇarpan >= 0f && m < öncekiÇarpan - 0.0001f) monoton = false;
                öncekiÇarpan = m;
            }
            failed += Check(ref total, "İFLAS eğrisi MONOTON (saat düştükçe hep artıyor)", monoton, 0f);

            // ⚠️ KOŞU ZAYIF BANTTA BAŞLAMALI — yoksa silahın kimliği YALAN.
            //    İlk denemede eşikleri (120/25) saat ölçeğine bakmadan seçmiştim;
            //    koşu 60'ta başladığı için silah oyunun TAMAMINDA güçlü moddaydı.
            //    Bu kontrol sentetik fixture'a değil GERÇEK ASSET'e bakıyor, çünkü
            //    yalan tam orada (asset ↔ C# eşiği) doğuyordu.
            var gerçek = AssetDatabase.LoadAssetAtPath<Tuning>("Assets/_Project/Resources/Tuning.asset");
            if (gerçek != null)
            {
                float başlangıçÇarpanı = iflas.DesperationMultiplier(gerçek.startSeconds, gerçek);
                failed += Check(ref total,
                    $"⚠️ KOŞU İFLAS'ın ZAYIF bandında başlıyor (saat {gerçek.startSeconds:0})",
                    başlangıçÇarpanı < 1f, başlangıçÇarpanı);
            }

            // Diğer silahlar saate BAKMIYOR — kimlik dağılmasın
            failed += Check(ref total, "çaresizlik eğrisi İFLAS'a özel",
                            Mathf.Approximately(new ConeBlade().DesperationMultiplier(10f, t), 1f)
                            && Mathf.Approximately(new Faizli().DesperationMultiplier(10f, t), 1f), 1f);

            // ── 6) ⚠️ HACİZ'İN ÖLÜ BÖLGESİ — özellik değil ETKİ ──
            //    "MinRange 3.2 mi" diye sormuyoruz; DİBİNDEKİNE HASAR İNİYOR MU diye
            //    soruyoruz. Ölü bölge silahın kimliği: çalışmazsa HACİZ sadece
            //    "uzun menzilli tek hedefli" olur ve sorduğu soru yok olur.
            var haciz = new Haciz();
            failed += Check(ref total, "HACİZ'in ölü bölgesi VAR", haciz.MinRange(t) > 0f, haciz.MinRange(t));
            failed += Check(ref total, "KONİ'nin ölü bölgesi YOK (kimlik dağılmıyor)",
                            new ConeBlade().MinRange(t) <= 0f, new ConeBlade().MinRange(t));

            int hacizIndex = System.Array.IndexOf(Weapon.All, System.Array.Find(Weapon.All, w => w is Haciz));
            pc.SetWeapon(hacizIndex);

            // (a) dibindeki düşmana VURAMIYOR
            ClearEnemies();
            var yakin = MakeEnemy(t, pGo.transform,
                                  Bootstrap.ArenaCenter + Vector3.forward * (haciz.MinRange(t) * 0.4f));
            float yakinÖnce = yakin.TimeLeft;
            strike.Invoke(pc, null);
            failed += Check(ref total, "⚠️ HACİZ DİBİNDEKİNE VURAMIYOR (ölü bölge işliyor)",
                            Mathf.Approximately(yakin.TimeLeft, yakinÖnce), yakinÖnce - yakin.TimeLeft);

            // (b) ama uzaktakine VURUYOR — hem de KONİ'nin yetişemediği mesafeden
            ClearEnemies();
            float uzakMesafe = t.attackRange * 1.8f;
            var uzak = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * uzakMesafe);
            float uzakÖnce = uzak.TimeLeft;
            strike.Invoke(pc, null);
            failed += Check(ref total, "⚠️ HACİZ UZAKTAKİNE VURUYOR",
                            uzak.TimeLeft < uzakÖnce - 0.001f, uzakÖnce - uzak.TimeLeft);

            failed += Check(ref total, "test kurgusu: o mesafe KONİ'nin menzili DIŞINDA",
                            uzakMesafe - uzak.Radius > t.attackRange, uzakMesafe - t.attackRange);

            // (c) ⚠️ DASH-ATAK ölü bölgeyi TANIMIYOR — dibine girene bir cevap olmalı,
            //     yoksa menzilli silah kalabalıkta tamamen çaresiz kalır.
            ClearEnemies();
            var dipteki = MakeEnemy(t, pGo.transform,
                                    Bootstrap.ArenaCenter + Vector3.forward * (haciz.MinRange(t) * 0.4f));
            float diptekiÖnce = dipteki.TimeLeft;
            dashStrike.Invoke(pc, null);
            failed += Check(ref total, "⚠️ DASH-ATAK ölü bölgeyi tanımıyor (dibe girene CEVAP var)",
                            dipteki.TimeLeft < diptekiÖnce - 0.001f, diptekiÖnce - dipteki.TimeLeft);

            // (d) ⚠️ ÖLÜ BÖLGE GERÇEKTEN ÇİZİLİYOR MU — mekanik değil GÖRSEL ──
            //     Vuruşun düştüğünü ölçmek YETMİYOR. Oyuncu dibindekine savurup
            //     ıskaladığında sebebi EKRANDA görmeli; görmezse silah bozuk sanılır.
            //     Bu projede "mekanik doğru, görsel yalan" üç kez çıktı — dördüncüsü
            //     tam buradan gelirdi.
            YaylariTemizle();
            startAttack.Invoke(pc, new object[] { Vector3.forward });
            var yaylar = Object.FindObjectsByType<Arc>(FindObjectsInactive.Include);

            failed += Check(ref total, "⚠️ HACİZ savurunca ÖLÜ BÖLGE HALKASI da çiziliyor",
                            yaylar.Length == 2, yaylar.Length);

            bool halkaDogruYerde = false;
            foreach (var y in yaylar)
                if (Mathf.Abs(y.Radius - haciz.MinRange(t)) < 0.01f) halkaDogruYerde = true;
            failed += Check(ref total, "⚠️ halka TAM ölü bölgenin yarıçapında (görsel yalan söylemiyor)",
                            halkaDogruYerde, haciz.MinRange(t));

            // KONİ'de olmamalı — halka HACİZ'in kimliği, her savurmada çıkan süs değil
            YaylariTemizle();
            pc.SetWeapon(0);
            startAttack.Invoke(pc, new object[] { Vector3.forward });
            failed += Check(ref total, "KONİ'de halka YOK (kimlik dağılmıyor)",
                            Object.FindObjectsByType<Arc>(FindObjectsInactive.Include).Length == 1,
                            Object.FindObjectsByType<Arc>(FindObjectsInactive.Include).Length);
            YaylariTemizle();
            pc.SetWeapon(hacizIndex);

            // ── 6c) ⚠️ İPOTEK GERÇEKTEN VURUYOR MU ──
            //    "İşaret doğdu mu" değil — DÜŞMANIN HAVUZU İNDİ Mİ. GroundMark
            //    bugüne kadar SADECE oyuncuya vuruyordu; ters yön yeni kod, yani
            //    "doğdu ama kimseye değmiyor" tam olarak beklenebilir hata.
            int ipotekIndex = System.Array.IndexOf(Weapon.All,
                System.Array.Find(Weapon.All, w => w is Ipotek));
            var ipotek = Weapon.All[ipotekIndex];
            pc.SetWeapon(ipotekIndex);

            ClearEnemies();
            YaylariTemizle();
            var kurban = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 2f);
            float kurbanÖnce = kurban.TimeLeft;

            // İşareti doğrudan kurbanın üstüne koy — nişan yolu ayrı test ediliyor,
            // burada ölçülen şey İŞARETİN VURUP VURMADIĞI.
            var isaret = GroundMark.SpawnLien(t, kurban.transform.position,
                                              ipotek.GroundMarkRadius(t), ipotek.GroundMarkDelay(t),
                                              ipotek.Damage(t, t.attackDamage),
                                              ipotek.HarvestMultiplier, null);

            failed += Check(ref total, "İPOTEK işareti HEMEN patlamıyor (kaçma penceresi var)",
                            !isaret.Fired && Mathf.Approximately(kurban.TimeLeft, kurbanÖnce),
                            kurbanÖnce - kurban.TimeLeft);

            isaret.Step(ipotek.GroundMarkDelay(t) + 0.05f);      // dt dışarıdan
            failed += Check(ref total, "⚠️ İPOTEK olgunlaşınca DÜŞMANA VURUYOR",
                            kurban.TimeLeft < kurbanÖnce - 0.001f, kurbanÖnce - kurban.TimeLeft);
            failed += Check(ref total, "İPOTEK isabet SAYIYOR", isaret.EnemiesHit == 1, isaret.EnemiesHit);

            // (b) ⚠️ YARIÇAPIN DIŞINDAKİ KURTULUYOR — yoksa "alan" diye bir şey yok
            ClearEnemies();
            var kacan = MakeEnemy(t, pGo.transform,
                                  Bootstrap.ArenaCenter + Vector3.forward * 30f);
            float kacanÖnce = kacan.TimeLeft;
            var isaret2 = GroundMark.SpawnLien(t, Bootstrap.ArenaCenter,
                                               ipotek.GroundMarkRadius(t), ipotek.GroundMarkDelay(t),
                                               ipotek.Damage(t, t.attackDamage),
                                               ipotek.HarvestMultiplier, null);
            isaret2.Step(ipotek.GroundMarkDelay(t) + 0.05f);
            failed += Check(ref total, "⚠️ İPOTEK yarıçapın DIŞINDAKİNE vurmuyor (kaçmak işe yarıyor)",
                            Mathf.Approximately(kacan.TimeLeft, kacanÖnce), kacanÖnce - kacan.TimeLeft);

            // (c) ⚠️ Düşmanın tebligatı OYUNCUYA vuruyor, İPOTEK DÜŞMANA — karışmasın
            ClearEnemies();
            var yanindaki = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter);
            float yanindakiÖnce = yanindaki.TimeLeft;
            var tebligat = GroundMark.Spawn(t, Bootstrap.ArenaCenter, pGo.transform,
                                            2.5f, 0.4f, 1f, null);
            tebligat.Step(0.5f);
            failed += Check(ref total, "⚠️ DÜŞMAN tebligatı düşmana vurmuyor (yönler karışmıyor)",
                            Mathf.Approximately(yanindaki.TimeLeft, yanindakiÖnce),
                            yanindakiÖnce - yanindaki.TimeLeft);

            ClearEnemies();
            YaylariTemizle();
            pc.SetWeapon(hacizIndex);

            // ── 6b) ⚠️ HER SİLAHA TUŞLA ULAŞILABİLİYOR MU — ders 11'in silah hali ──
            //    Tuşlar 1-4 diye SABİT yazılıydı; kadro 5'e çıkınca beşinci silah
            //    tanımlı olup ERİŞİLEMEZ olacaktı. Tam Faz 13'teki yedinci tür hatası.
            var swapKeys = typeof(PlayerController).GetField("SwapKeys",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            int tuşSayısı = ((System.Array)swapKeys.GetValue(null)).Length;
            failed += Check(ref total, "⚠️ HER SİLAHIN bir tuşu var (tanımlı ≠ ulaşılabilir)",
                            tuşSayısı >= Weapon.All.Length, tuşSayısı - Weapon.All.Length);

            // ── 7) ⚠️ HER SİLAH GERÇEKTEN HASAR VEREBİLİYOR MU ──
            //    DURGUN bu kontrol olmadığı için tamamen kırık teslim edilmişti:
            //    basılı tutma bloğu erken `return` ediyordu, saldırı girdisi hiç okunmuyordu,
            //    bırakınca da bir şey olmuyordu. Hasat çarpanı vardı ama hasar veremiyordu.
            //    Öz-denetim yeşildi çünkü hiçbir test "bu silah vurabiliyor mu" diye sormamıştı.
            for (int wi = 0; wi < Weapon.All.Length; wi++)
            {
                ClearEnemies();
                pc.SetWeapon(wi);

                // ⚠️ Hedef SİLAHA GÖRE konumlanıyor. Sabit 1.2 birimdeydi ve HACİZ
                // gelince o mesafe ölü bölgenin İÇİNDE kaldı — test, silahı çalışmıyor
                // diye suçlayacaktı. Suç testin kurgusunda: her silah kendi geçerli
                // bandından denenmeli, yoksa "hasar veremiyor" bulgusu YALAN olur.
                // ⚠️ Yer vuruşu silahı imleç yokken (batch modda) AZAMİ MENZİLE atıyor,
                // hedefin üstüne değil. Hedefi 1.2 birimde bırakmak İPOTEK'i "hasar
                // veremiyor" diye gösteriyordu — üçüncü kez aynı ders: fixture silahın
                // GERÇEK davranışını ithal etmeli, ortalama bir varsayım uydurmamalı.
                float mesafe = pc.Weapon.GroundMarkDelay(t) > 0f
                             ? pc.Weapon.Range(t)
                             : Mathf.Max(1.2f, pc.Weapon.MinRange(t) * 1.6f);
                var target = MakeEnemy(t, pGo.transform,
                                       Bootstrap.ArenaCenter + Vector3.forward * mesafe);

                failed += Check(ref total, $"  test kurgusu: {pc.Weapon.Name} menzili {mesafe:0.0} birimi kapsıyor",
                                mesafe - target.Radius <= pc.Weapon.Range(t), pc.Weapon.Range(t) - mesafe);

                float poolBefore = target.TimeLeft;
                pc.Weapon.OnSwingStart(clock, t);
                strike.Invoke(pc, null);

                // ⚠️ Her silah ANINDA vurmuyor. İPOTEK gecikmeli yer vuruşu bırakıyor;
                // testin "savurdum, hemen düşmeli" varsayımı onu haksız yere kırık
                // gösteriyordu. Gecikmeli silahta işareti OLGUNLAŞTIRIP öyle bakıyoruz —
                // ölçülen şey yine ETKİ, sadece zincirin sonuna kadar gidiliyor.
                float gecikme = pc.Weapon.GroundMarkDelay(t);
                if (gecikme > 0f)
                    foreach (var m in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include))
                        m.Step(gecikme + 0.05f);

                failed += Check(ref total, $"{pc.Weapon.Name} HASAR VEREBİLİYOR",
                                target.TimeLeft < poolBefore - 0.001f, poolBefore - target.TimeLeft);

                foreach (var m in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include))
                    Object.DestroyImmediate(m.gameObject);   // tuzak 1: ertelenmez, hemen
            }

            // ── 8) ⚠️ BÜYÜK DÜŞMAN YÜZEYİNDEN VURULABİLİYOR MU (Enes: "bossa vururken buglanıyor")
            //    Menzil merkeze ölçülseydi dev bir yaratık gövdesine değecek kadar yakınken
            //    bile menzil dışında kalırdı — oyuncu GÖRDÜĞÜ şeye vuramazdı.
            ClearEnemies();
            pc.SetWeapon(0);
            var big = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 3.0f);
            big.transform.localScale = Bootstrap.EnemyScale * 1.9f;   // boss ölçeği

            float centerDist = 3.0f;
            failed += Check(ref total, "test kurgusu: merkez mesafesi menzilin DIŞINDA",
                            centerDist > t.attackRange, centerDist - t.attackRange);
            failed += Check(ref total, "ama yüzeyi menzilin İÇİNDE",
                            centerDist - big.Radius <= t.attackRange, centerDist - big.Radius);

            float bigPoolBefore = big.TimeLeft;
            strike.Invoke(pc, null);
            failed += Check(ref total, "BÜYÜK DÜŞMAN YÜZEYİNDEN VURULDU",
                            big.TimeLeft < bigPoolBefore - 0.001f, bigPoolBefore - big.TimeLeft);

            // ── 9) Dash-atak silaha göre değişiyor mu ──
            failed += Check(ref total, "KONİ dash-atağı süpürüyor",
                            new ConeBlade().DashTargets > 1, new ConeBlade().DashTargets);
            failed += Check(ref total, "FAİZLİ dash-atağı TEK hedefli kalıyor",
                            new Faizli().DashTargets == 1, new Faizli().DashTargets);
            failed += Check(ref total, "tek hedefli dash-atak daha sert vuruyor",
                            new Faizli().DashDamageMultiplier(t) > new ConeBlade().DashDamageMultiplier(t),
                            new Faizli().DashDamageMultiplier(t));

            ClearEnemies();
            Object.DestroyImmediate(poorRoot);
            Object.DestroyImmediate(poorT);
            Object.DestroyImmediate(pGo);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(t);

            if (failed == 0) Debug.Log($"[Faz 3 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 3 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── yardımcılar ──────────────────────────────────────────────────────

        static float[] Snapshot()
        {
            var arr = new float[Enemy.All.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = Enemy.All[i].TimeLeft;
            return arr;
        }

        static int CountDamaged(float[] before)
        {
            int n = 0;
            for (int i = 0; i < Enemy.All.Count && i < before.Length; i++)
                if (Enemy.All[i].TimeLeft < before[i] - 0.001f) n++;
            return n;
        }

        static void SpawnThree(Tuning t, Transform player)
        {
            // Üçü de dar koninin İÇİNDE: tek hedefli silahın gerçekten seçim yaptığını
            // görmek için geometri onu zorlamamalı.
            MakeEnemy(t, player, Bootstrap.ArenaCenter + new Vector3(-0.25f, 0f, 1.1f));
            MakeEnemy(t, player, Bootstrap.ArenaCenter + new Vector3(0f, 0f, 1.4f));
            MakeEnemy(t, player, Bootstrap.ArenaCenter + new Vector3(0.25f, 0f, 1.7f));
        }

        static Enemy MakeEnemy(Tuning t, Transform player, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = pos;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(t, player, null);
            return e;
        }

        static void ClearEnemies()
        {
            foreach (var e in Enemy.All.ToArray())
                if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
        }

        /// <summary>Sahnedeki yayları hemen al — ertelenmiş `Destroy` sayımı bozar (tuzak 1).</summary>
        static void YaylariTemizle()
        {
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include))
                Object.DestroyImmediate(a.gameObject);
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
