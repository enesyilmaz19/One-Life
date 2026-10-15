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

            Arc halka = null;
            foreach (var y in yaylar)
                if (Mathf.Abs(y.Radius - haciz.MinRange(t)) < 0.01f) halka = y;
            failed += Check(ref total, "⚠️ halka TAM ölü bölgenin yarıçapında (görsel yalan söylemiyor)",
                            halka != null, haciz.MinRange(t));

            // ⚠️ Doğru yerde olmak yetmiyor, doğru ŞEYİ SÖYLEMELİ. Savurmanın kendi
            // rengindeyse oyuncu onu "yayın bir parçası" okur, "burası ölü" değil.
            // Renk değeri sabit yazılmıyor (ayar değişince test kırılmasın); ANLAMI
            // ölçülüyor: savurmadan farklı, ve uyarı tonunda (kırmızı baskın).
            if (halka != null)
            {
                var c = halka.Color;
                failed += Check(ref total, "⚠️ halka savurma renginden FARKLI (yayın parçası sanılmıyor)",
                                Mathf.Abs(c.r - haciz.Tint.r) + Mathf.Abs(c.g - haciz.Tint.g)
                                + Mathf.Abs(c.b - haciz.Tint.b) > 0.3f, 0f);
                failed += Check(ref total, "halka UYARI tonunda (kırmızı baskın)",
                                c.r > c.g && c.r > c.b, c.r);
            }

            // KONİ'de olmamalı — halka HACİZ'in kimliği, her savurmada çıkan süs değil
            YaylariTemizle();
            pc.SetWeapon(0);
            startAttack.Invoke(pc, new object[] { Vector3.forward });
            failed += Check(ref total, "KONİ'de halka YOK (kimlik dağılmıyor)",
                            Object.FindObjectsByType<Arc>(FindObjectsInactive.Include).Length == 1,
                            Object.FindObjectsByType<Arc>(FindObjectsInactive.Include).Length);
            YaylariTemizle();
            pc.SetWeapon(hacizIndex);

            // ── 6g) ⚠️ FAİZLİ VURDUĞUNU GERİ SAVURUYOR MU — "oturtma"nın ölçüsü ──
            //    Enes iki kez *"oturtmuyor"* dedi. İlkinde hit-stop ve sarsıntıyı
            //    büyütmüştüm — yani VAR OLANI. Eksik olan şey düşmanı İTMEKTİ ve
            //    oyunda onu yapan hiçbir şey yoktu. ⚠️ Ölçülen ÖZELLİK değil ETKİ:
            //    "Knockback > 0 mu" değil, DÜŞMAN YERİNDEN OYNADI MI.
            ClearEnemies();
            YaylariTemizle();
            int faizliIndex = System.Array.IndexOf(Weapon.All,
                System.Array.Find(Weapon.All, w => w is Faizli));
            pc.SetWeapon(faizliIndex);

            var itilen = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.2f);
            Vector3 itilenÖnce = itilen.transform.position;
            pc.Weapon.OnSwingStart(clock, t);
            strike.Invoke(pc, null);
            itilen.TickPush(0.1f);            // dt dışarıdan (tuzak 3)

            failed += Check(ref total, "⚠️ FAİZLİ vurduğunu GERİ SAVURUYOR (oturtma)",
                            (itilen.transform.position - itilenÖnce).magnitude > 0.05f,
                            (itilen.transform.position - itilenÖnce).magnitude);
            failed += Check(ref total, "savrulma OYUNCUDAN UZAĞA (içine çekmiyor)",
                            Vector3.Dot((itilen.transform.position - itilenÖnce).normalized,
                                        Vector3.forward) > 0.5f, 0f);

            // ⚠️ İTME KAÇIŞA SEBEP OLAMAZ — ters yüz cezanın kapısı.
            //    Kaçan KAÇAK'ı FAİZLİ ile vurunca onu kaçış yarıçapının DIŞINA
            //    itiyordum: en pahalı silahın en iyi vuruşu düşmanı kaçırtıp
            //    zamanını götürüyordu. ⚠️ Ölçü bayrak değil KONUM: itmeden sonra
            //    hâlâ kaçış eşiğinin içinde mi.
            ClearEnemies();
            pc.SetWeapon(faizliIndex);
            var kaçak = MakeEnemy(t, pGo.transform,
                                  new Vector3(0f, Bootstrap.ArenaCenter.y, t.runnerEscapeRadius - 0.8f));
            kaçak.Init(t, pGo.transform, null, 0.05f, false, EnemyKind.Runner);  // havuzu düşük → kaçıyor
            kaçak.TakeDamage(0.01f);                                            // kaçış durumu otursun
            kaçak.Push(Vector3.forward, 40f);                                   // sert savur
            for (int i = 0; i < 30; i++) kaçak.TickPush(0.05f);

            float merkezMesafe = new Vector2(kaçak.transform.position.x,
                                             kaçak.transform.position.z).magnitude;
            failed += Check(ref total, "⚠️ İTME kaçış yarıçapının DIŞINA çıkaramıyor",
                            merkezMesafe < t.runnerEscapeRadius, merkezMesafe);
            ClearEnemies();

            // KONİ savurmuyor — ağırlık FAİZLİ'nin kimliği, herkesin özelliği değil
            ClearEnemies();
            pc.SetWeapon(0);
            var itilmeyen = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.2f);
            Vector3 itilmeyenÖnce = itilmeyen.transform.position;
            strike.Invoke(pc, null);
            itilmeyen.TickPush(0.1f);
            failed += Check(ref total, "KONİ savurmuyor (ağırlık FAİZLİ'ye özel)",
                            (itilmeyen.transform.position - itilmeyenÖnce).magnitude < 0.01f,
                            (itilmeyen.transform.position - itilmeyenÖnce).magnitude);
            ClearEnemies();
            YaylariTemizle();

            // ── 6f) ⚠️ İPOTEK SAVURMA YAYI ÇİZMEMELİ ──
            //    Enes bildirdi: *"ipotekte iki alan açılıyor, ilk bastığımızda çıkan
            //    koni hasar vurmuyor."* Yay koşulsuz çiziliyordu — ekranda kocaman
            //    bir isabet alanı açılıyor ve hiçbir şeye değmiyor. "Mekanik doğru,
            //    görsel yalan" hatasının DÖRDÜNCÜSÜ, ve en kötüsü: yay tanımı gereği
            //    "isabet alanı burası" diyor.
            YaylariTemizle();
            ClearEnemies();
            pc.SetWeapon(System.Array.IndexOf(Weapon.All,
                System.Array.Find(Weapon.All, w => w is Ipotek)));
            startAttack.Invoke(pc, new object[] { Vector3.forward });

            int ipotekYay = 0;
            foreach (var y in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include))
                if (y.Radius > new Ipotek().GroundMarkRadius(t) + 0.5f) ipotekYay++;
            failed += Check(ref total, "⚠️ İPOTEK savurunca SAHTE KONİ çıkmıyor",
                            ipotekYay == 0, ipotekYay);
            YaylariTemizle();

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

            // (a2) ⚠️ PATLAMADAN YOK EDİLEN İŞARET SAYACI SIZDIRMAMALI
            //      Oda değişimi / restart bekleyen işareti patlatmadan yok ediyor.
            //      Sayaç düşmezse `ActiveLiens` kalıcı >0 takılır ve İPOTEK
            //      TAMAMEN çalışmaz olur — hem de sessizce: savuruyorsun, hiçbir
            //      şey olmuyor. ⚠️ Ölçülen şey bayrak değil SONRAKİ SAVURMA.
            GroundMark.ResetLiens();
            var ölüİşaret = GroundMark.SpawnLien(t, Bootstrap.ArenaCenter,
                                                 ipotek.GroundMarkRadius(t), 99f,
                                                 5f, 1f, null);

            // ⚠️ ÜRETİMİN GERÇEK YOLU çağrılıyor — `TearDown`. Testin kendi
            // temizliğini yazsaydım oda geçişinin sayacı sıfırladığını değil,
            // testin sıfırladığını doğrulardım (düzeneği KOPYALAMA, İTHAL ET).
            // ⚠️ `RoomManager.Init` oyuncuyu ODANIN GİRİŞİNE taşıyor; bu denetimdeki
            // sonraki bütün testler düşmanı arena merkezine koyup oradan vuruyor.
            // İlk yazışımda konumu geri koymamıştım ve BEŞ silahın hepsi birden
            // "hasar veremiyor" dedi — silahlar değil, testin kendisi bozulmuştu.
            var oyuncuKonum = pGo.transform.position;
            var oyuncuYön = pGo.transform.rotation;

            var rmGo = new GameObject("teardown-check");
            var rmc = rmGo.AddComponent<RoomManager>();
            rmc.Init(t, pGo.transform, null);
            typeof(RoomManager).GetMethod("TearDown", BindingFlags.NonPublic | BindingFlags.Instance)
                               .Invoke(rmc, null);
            Object.DestroyImmediate(rmGo);
            RoomManager.Reset();
            if (ölüİşaret != null) Object.DestroyImmediate(ölüİşaret.gameObject);

            pGo.transform.SetPositionAndRotation(oyuncuKonum, oyuncuYön);
            ClearEnemies();

            failed += Check(ref total, "⚠️ ODA DEĞİŞİMİ bekleyen ipotek sayacını sıfırlıyor",
                            GroundMark.ActiveLiens == 0, GroundMark.ActiveLiens);

            ClearEnemies();
            var sonraki = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 2f);
            float sonrakiÖnce = sonraki.TimeLeft;
            var yeni = GroundMark.SpawnLien(t, sonraki.transform.position,
                                            ipotek.GroundMarkRadius(t), ipotek.GroundMarkDelay(t),
                                            ipotek.Damage(t, t.attackDamage),
                                            ipotek.HarvestMultiplier, null);
            yeni.Step(ipotek.GroundMarkDelay(t) + 0.05f);
            failed += Check(ref total, "⚠️ sızıntıdan SONRA İPOTEK hâlâ vuruyor",
                            sonraki.TimeLeft < sonrakiÖnce - 0.001f, sonrakiÖnce - sonraki.TimeLeft);

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

            // ── 6d) ⚠️ SİLAHLAR FARKLI SORU SORUYOR MU ──
            //    Bu ölçü `EnemyKind`'da ve `BossKind`'da vardı, SİLAHLARDA YOKTU —
            //    ve VADELİ tam bu yüzden aylarca FAİZLİ'nin kopyası olarak durdu.
            //    Onu silerken kullandığım ölçüyü koda geçirmezsem bir sonraki silah
            //    aynı sessizlikle yeniden kopya olur.
            int ayni = 0, adsiz = 0;
            for (int a = 0; a < Weapon.All.Length; a++)
            {
                if (string.IsNullOrWhiteSpace(Weapon.All[a].Name)) adsiz++;
                for (int b = a + 1; b < Weapon.All.Length; b++)
                    if (SilahSorusu(Weapon.All[a], t) == SilahSorusu(Weapon.All[b], t))
                    {
                        ayni++;
                        Debug.LogError($"     aynı soruyu soruyor: {Weapon.All[a].Name} = {Weapon.All[b].Name}");
                    }
            }
            failed += Check(ref total, "her silahın adı var", adsiz == 0, adsiz);
            failed += Check(ref total, "⚠️ silahlar FARKLI SORU soruyor (ad farkı çeşitlilik değil)",
                            ayni == 0, ayni);
            failed += Check(ref total, "kadro BEŞ silah (TASARIM.md kararı)",
                            Weapon.All.Length == 5, Weapon.All.Length);

            // ── 6e) ⚠️ MENZİLLİ SİLAHIN DASH'İ YAKIN CEVABIDIR ──
            //    HACİZ için ayrıca ölçüldü; İPOTEK için de geçerli olmalı, yoksa
            //    kural "HACİZ'e özel bir istisna" olur ve bir dahaki menzilli
            //    silahta kimse hatırlamaz.
            foreach (var w in Weapon.All)
            {
                if (w.MinRange(t) <= 0f && w.GroundMarkDelay(t) <= 0f) continue;

                ClearEnemies();
                pc.SetWeapon(System.Array.IndexOf(Weapon.All, w));
                var dipte = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.1f);
                float dipteÖnce = dipte.TimeLeft;
                dashStrike.Invoke(pc, null);
                // Faz 29 (Enes): İPOTEK'in dash-atağı HACİZ İHBARI — cevap GECİKMELİ ama geniş, işaretin
                // alanı dibindekini de kapsıyor. Patlamayı bekle, sonra ölç.
                var işaretler = Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include);
                foreach (var m in işaretler) m.Step(99f);
                failed += Check(ref total, $"⚠️ {w.Name} DASH'i dibindekine vuruyor (çaresiz kalmıyor{(w.DashSpecial(t) != null ? $" — {w.DashSpecial(t).Name}'nın alanı kapsıyor" : "")})",
                                dipte.TimeLeft < dipteÖnce - 0.001f, dipteÖnce - dipte.TimeLeft);
                foreach (var m in işaretler) if (m != null) Object.DestroyImmediate(m.gameObject);
                GroundMark.ResetLiens();
            }
            ClearEnemies();
            YaylariTemizle();
            // ⚠️ İPOTEK normal savurmasında işaret bırakıyor ve aynı anda YALNIZCA
            // BİR tanesi yaşayabiliyor. Sayaç sıfırlanmazsa sonraki İPOTEK testi
            // "hasar veremiyor" der ve suç yine testin kurgusunda olur.
            // (Aynı tekillik kuralı Enes'in bildirdiği hatanın da yarısıydı:
            //  dash'in arkada bıraktığı hayalet işaret, nişan alınan ipoteğin
            //  yerini çalıyordu. O kanca silindi.)
            foreach (var m in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include))
                Object.DestroyImmediate(m.gameObject);
            GroundMark.ResetLiens();

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

                // ⚠️ İşareti yok etmek SAYACI düşürmüyor (üretimde `TearDown`/
                // `ResetStatics` düşürüyor, `OnDestroy` DEĞİL — Unity edit modda o
                // kancayı çağırmıyor). İşaret yok edilen her yerde sayaç da
                // sıfırlanmalı, yoksa sonraki test ipotek koyamıyor.
                foreach (var m in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include))
                    Object.DestroyImmediate(m.gameObject);   // tuzak 1: ertelenmez, hemen
                GroundMark.ResetLiens();
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

        /// <summary>
        /// Silahın SORDUĞU SORU — adı, rengi ya da sayıları değil.
        /// `EnemyKind` ve `BossKind` denetimlerindeki ölçünün aynısı.
        ///
        /// ⚠️ "Bedelli mi" ÖZELLİKTEN değil ETKİDEN okunuyor: bir saat kurulup
        /// savurma yaşatılıyor ve saniye düştü mü diye BAKILIYOR. `OnSwingStart`
        /// override edilmiş mi diye sormak, bedeli sıfır olan bir silahı da
        /// "bedelli" sayardı.
        /// </summary>
        static string SilahSorusu(Weapon w, Tuning t)
        {
            var root = new GameObject("soru");
            var clock = root.AddComponent<Clock>();
            clock.Init(t, root.transform);
            float önce = clock.Seconds;
            w.OnSwingStart(clock, t);
            w.OnStrike(clock, t, 1, 0);
            bool bedelli = clock.Seconds < önce - 0.0001f;
            Object.DestroyImmediate(root);
            Clock.Reset();

            bool saateBağlı = !Mathf.Approximately(w.DesperationMultiplier(0f, t),
                                                   w.DesperationMultiplier(9999f, t));
            return $"çoklu={w.MaxTargets != 1}|band={w.MinRange(t) > 0f}"
                 + $"|yer={w.GroundMarkDelay(t) > 0f}|bedelli={bedelli}"
                 + $"|saate={saateBağlı}|uzun={w.Range(t) > t.attackRange * 1.5f}";
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
