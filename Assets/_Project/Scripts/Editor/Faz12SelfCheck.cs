using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 12 — boss çeşitliliği. Korunan kurallar:
    ///
    ///   • ⚠️⚠️ KREDİ KURALI — fullenen havuz vurunca HASAT VERMİYOR. Bu olmasaydı
    ///     son iki boss oyunun en büyük zaman KAYNAĞI olurdu; oysa boss'un seni
    ///     beslememesi gerekiyor. Ölçüm ölçülen etkiye bakıyor: SAATE ne geçti.
    ///   • Faz geçişi doğru kuralla: ①② yarıda, ③④ havuz bitince FULLENEREK
    ///   • Her boss'un mekaniği GERÇEKTEN farklı (aynı boss dört kez değil)
    ///   • EKSPER yaşadığı sürece hasadı düşürüyor — ÖLÜNCE düzeliyor
    /// </summary>
    public static class Faz12SelfCheck
    {
        [MenuItem("Tek Can/Faz 12 öz-denetimi çalıştır")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            var t = ScriptableObject.CreateInstance<Tuning>();
            t.harvestSoftCap = 1e6f;   // Faz 28 hasat doyması bu kapının konusu değil (büyük saatte kazancı kısıyor)
            t.startSeconds = 5000f;
            t.enemyTimePool = 10f;
            t.harvestRate = 0.5f;
            t.enemyDecayPerSecond = 0f;
            t.bossHarvestMultiplier = 1f;      // kredi kuralını izole et
            Region.Reset();

            // ── 1) Dört boss ve hepsi farklı ──
            // ⚠️ Gerçek kural "dört boss" değil, HER BÖLGENİN KENDİ BOSS'U olması.
            //    Önceki hâl 4'ü sabitliyordu ve 5 bölge varken son ikisi AYNI boss'u
            //    alıyordu (`For` son elemana kırpıyor) — testler yeşilken Enes oyunda
            //    fark etti. Sayı değil, EŞLEŞME sınanmalı.
            var t2 = ScriptableObject.CreateInstance<Tuning>();
            int bölgeSayısı = t2.regionsPerAct;
            Object.DestroyImmediate(t2);

            failed += Check(ref total, "⚠️ HER BÖLGEYE bir boss var (kırpılma yok)",
                            BossKind.All.Length >= bölgeSayısı, BossKind.All.Length);

            var görülen = new System.Collections.Generic.HashSet<BossKind>();
            for (int i = 0; i < bölgeSayısı; i++) görülen.Add(BossKind.For(i));
            failed += Check(ref total, "⚠️ iki bölge AYNI boss'u paylaşmıyor",
                            görülen.Count == bölgeSayısı, görülen.Count);
            int aynı = 0;
            for (int a = 0; a < BossKind.All.Length; a++)
                for (int b = a + 1; b < BossKind.All.Length; b++)
                    if (İmza(BossKind.All[a], t) == İmza(BossKind.All[b], t)) aynı++;
            failed += Check(ref total, "⚠️ boss mekanikleri GERÇEKTEN farklı (aynısı 4 kez değil)",
                            aynı == 0, aynı);

            // ── 2) Faz geçiş kuralları ──
            failed += Check(ref total, "TAHSİLDAR yarıda dönüyor",
                            !Bul("TAHSİLDAR").RefillsOnEmpty, 0);
            failed += Check(ref total, "EKSPER yarıda dönüyor", !Bul("EKSPER").RefillsOnEmpty, 0);
            failed += Check(ref total, "⚠️ KEFİL havuz bitince FULLENİYOR",
                            Bul("KEFİL").RefillsOnEmpty, 1);
            failed += Check(ref total, "⚠️ MURABAHACI havuz bitince FULLENİYOR",
                            Bul("MURABAHACI").RefillsOnEmpty, 1);

            // ── 3) ①② YARIDA faz 2'ye geçiyor — ETKİ ──
            var (root3, clock3, boss3) = Kur(t, Bul("TAHSİLDAR"));
            failed += Check(ref total, "boss faz 1'de başlıyor", boss3.Phase == 1, boss3.Phase);
            float havuz = boss3.TimeLeft;
            boss3.TakeDamage(havuz * 0.3f);
            failed += Check(ref total, "%30 hasarda hâlâ faz 1", boss3.Phase == 1, boss3.Phase);
            boss3.TakeDamage(havuz * 0.3f);
            failed += Check(ref total, "⚠️ yarıyı geçince FAZ 2", boss3.Phase == 2, boss3.Phase);
            failed += Check(ref total, "yarıda dönen boss KREDİYE geçmiyor", !boss3.OnCredit, 0);

            // TAHSİLDAR faz 2: emmeyi bırakıyor ama HIZLANIYOR — başka bir soru
            float aura2 = Bul("TAHSİLDAR").AuraDrain(t, 2);
            float aura1 = Bul("TAHSİLDAR").AuraDrain(t, 1);
            failed += Check(ref total, "⚠️ TAHSİLDAR faz 2'de EMMEYİ bırakıyor",
                            aura2 < aura1, aura2);
            failed += Check(ref total, "⚠️ ...ama HIZLANIYOR (kaçmak işe yaramıyor)",
                            Bul("TAHSİLDAR").SpeedMultiplier(2) > 1.2f,
                            Bul("TAHSİLDAR").SpeedMultiplier(2));
            Temizle(root3);

            // ── 4) ⚠️⚠️ KREDİ KURALI — fazın varlık sebebi ──
            //    Havuz fullenince vuruş SAATE hiçbir şey geçirmemeli.
            //    Çarpanı okumak yetmez; GERÇEK saat ölçülüyor.
            var (root4, clock4, boss4) = Kur(t, Bul("MURABAHACI"));
            float öncekiHavuz = boss4.TimeLeft;

            float saatÖnce = clock4.Seconds;
            boss4.TakeDamage(öncekiHavuz * 0.5f);
            float faz1Kazanç = clock4.Seconds - saatÖnce;
            failed += Check(ref total, "faz 1'de vuruş hasat VERİYOR", faz1Kazanç > 0.1f, faz1Kazanç);

            boss4.TakeDamage(öncekiHavuz);                    // havuzu bitir → FULLENİR
            failed += Check(ref total, "⚠️ havuz bitince FULLENDİ",
                            boss4.TimeLeft > öncekiHavuz * 0.9f, boss4.TimeLeft);
            failed += Check(ref total, "⚠️ fullenen havuz KREDİ olarak işaretli", boss4.OnCredit, 1);

            saatÖnce = clock4.Seconds;
            boss4.TakeDamage(öncekiHavuz * 0.5f);
            float krediKazanç = clock4.Seconds - saatÖnce;
            failed += Check(ref total, "⚠️⚠️ KREDİ havuzu HASAT VERMİYOR (saate 0 geçti)",
                            Mathf.Abs(krediKazanç) < 0.001f, krediKazanç);
            failed += Check(ref total, "...ama havuz yine de ERİYOR (dövüş sürüyor)",
                            boss4.TimeLeft < öncekiHavuz, boss4.TimeLeft);
            Temizle(root4);

            // ── 5) ⚠️ EKSPER yaşarken hasat DÜŞÜK, ölünce DÜZELİYOR ──
            //    Ölünce temizlenmeyi unutmak "hasadım niye hâlâ düşük" hatası doğururdu;
            //    bu projede statik bayrak unutmak tam olarak öyle hatalar üretti.
            var (root5, clock5, eksper) = Kur(t, Bul("EKSPER"));
            var kurban = Make(t, root5.transform, EnemyKind.Basic);

            float önce5 = clock5.Seconds;
            kurban.TakeDamage(3f);
            float eksperliKazanç = clock5.Seconds - önce5;

            Object.DestroyImmediate(eksper.gameObject);
            Enemy.All.Remove(eksper);

            var kurban2 = Make(t, root5.transform, EnemyKind.Basic);
            önce5 = clock5.Seconds;
            kurban2.TakeDamage(3f);
            float eksperizKazanç = clock5.Seconds - önce5;

            failed += Check(ref total, "⚠️ EKSPER yaşarken hasat DÜŞÜK",
                            eksperliKazanç < eksperizKazanç - 0.01f,
                            eksperizKazanç - eksperliKazanç);
            failed += Check(ref total, "⚠️ EKSPER ölünce hasat DÜZELİYOR",
                            eksperizKazanç > 0.1f, eksperizKazanç);
            Temizle(root5);

            // ── 6) KEFİL bölünüyor ──
            var (root6, clock6, kefil) = Kur(t, Bul("KEFİL"));
            int önceSayı = Enemy.All.Count;
            kefil.TakeDamage(kefil.TimeLeft + 1f);            // havuzu bitir → fullen + bölün
            failed += Check(ref total, "⚠️ KEFİL faz 2'de İKİYE BÖLÜNÜYOR",
                            Enemy.All.Count > önceSayı, Enemy.All.Count - önceSayı);
            Temizle(root6);

            // ── 7) MURABAHACI faz 2'de geri sarıyor — ETKİ ──
            var (root7, clock7, murabaha) = Kur(t, Bul("MURABAHACI"));
            murabaha.TakeDamage(murabaha.TimeLeft + 1f);      // fullen, faz 2
            murabaha.TakeDamage(murabaha.TimeLeft * 0.5f);    // yarıya indir
            float sarmaÖnce = murabaha.TimeLeft;
            murabaha.TickBossPassives(1f);                    // 1 saniye geçir
            failed += Check(ref total, "⚠️ MURABAHACI faz 2'de havuzunu GERİ SARIYOR",
                            murabaha.TimeLeft > sarmaÖnce + 0.1f, murabaha.TimeLeft - sarmaÖnce);
            Temizle(root7);

            // ── 8) ⚠️ SALDIRI DAĞARCIĞI — her boss KENDİ saldırılarıyla dövüşüyor ──
            int adsız = 0, kopya = 0, fazAynı = 0;
            foreach (var b in BossKind.All)
            {
                var f1 = b.Attacks(1);
                var f2 = b.Attacks(2);

                foreach (var a in f1) if (string.IsNullOrWhiteSpace(a.Name)) adsız++;
                foreach (var a in f2) if (string.IsNullOrWhiteSpace(a.Name)) adsız++;

                // ⚠️ Faz 2 listesi faz 1'in AYNISI OLAMAZ (Enes kararı: yeni saldırılar
                //    + eskilerin gelişmiş hâli). Aynıysa "faz" diye bir şey yok demektir.
                if (Dizi(f1) == Dizi(f2)) fazAynı++;

                // ⚠️ Aynı boss içinde iki saldırı aynı soruyu soruyorsa biri
                // gereksiz — Enes: "hiçbir bossta birbirinin aynısı iki saldırı
                // olmayacak." Tarama artık HER FAZI geziyor: faz 1'le sınırlı
                // olduğu için faz 2 ve 3'teki kopyalar sessizce geçiyordu, ve
                // Enes'in gördüğü tekrarların çoğu tam oradaydı.
                for (int f = 1; f <= b.PhaseCount; f++)
                {
                    var liste = b.Attacks(f);
                    for (int i = 0; i < liste.Length; i++)
                        for (int j = i + 1; j < liste.Length; j++)
                            if (Soru(liste[i]) == Soru(liste[j]))
                            {
                                kopya++;
                                Debug.LogError($"    {b.Name} faz {f}: {liste[i].Name} ≡ {liste[j].Name}");
                            }
                }
            }
            failed += Check(ref total, "her saldırının adı var", adsız == 0, adsız);
            failed += Check(ref total, "⚠️ FAZ 2 dağarcığı faz 1'in AYNISI DEĞİL",
                            fazAynı == 0, fazAynı);
            failed += Check(ref total, "aynı boss içinde iki saldırı aynı soruyu sormuyor",
                            kopya == 0, kopya);

            // Faz 2'de hem YENİ hem GELİŞMİŞ olmalı
            int yenisizFaz = 0;
            foreach (var b in BossKind.All)
            {
                var adlar1 = new System.Collections.Generic.HashSet<string>();
                foreach (var a in b.Attacks(1)) adlar1.Add(a.Name);
                bool yeni = false;
                foreach (var a in b.Attacks(2))
                    if (!adlar1.Contains(a.Name) && !a.Name.EndsWith("+")) yeni = true;
                if (!yeni) yenisizFaz++;
            }
            failed += Check(ref total, "⚠️ her boss faz 2'de YENİ bir saldırı kazanıyor",
                            yenisizFaz == 0, yenisizFaz);

            // ── 8b) ⚠️⚠️ ÖZGÜN ŞEKİL TEK SAHİPLİ ─────────────────────────────
            //    Enes: *"özgün diye her bossta aynı 'özgün' saldırıları koymuşsun
            //    neredeyse, amk her bossta farklı olacak."*
            //
            //    ⚠️ Bu kapı yazılmadan önce beş boss'un beşi de halka + simit +
            //    açıklık + hamle kullanıyordu; "özgün" sıfatı yalnızca ADIN
            //    üstündeydi. Kural göz kararı tutulamaz — dağarcığa her dokunuşta
            //    sessizce delinir. Ölçü burada duruyor.
            //
            //    ⚠️ İSTİSNA: MURABAHACI'nın FAZ 3'ü. "Son boss önceki tüm
            //    bossların özelliğini de kullanabilsin" (Enes). İstisna faz 3'e
            //    KİLİTLİ: faz 1-2'de de serbest olsaydı final bir TOPLAM değil
            //    bir TEKRAR olurdu ve faz 3'ün anlamı kalmazdı.
            int ihlal = 0;
            var ihlalMetni = new System.Text.StringBuilder();
            foreach (var b in BossKind.All)
                for (int f = 1; f <= b.PhaseCount; f++)
                {
                    if (f >= 3) continue;                    // finalin toplama fazı
                    foreach (var kök in b.Attacks(f))
                        for (var a = kök; a != null; a = a.Then)
                        {
                            string ş = Şekil(a);
                            foreach (var (şekil, sahip) in Tekel)
                                if (ş == şekil && b.Name != sahip)
                                {
                                    ihlal++;
                                    ihlalMetni.Append($" {b.Name}/{a.Name}={ş}(sahibi {sahip})");
                                }
                        }
                }
            if (ihlal > 0) Debug.LogError("    tekel ihlali:" + ihlalMetni);
            failed += Check(ref total, "⚠️⚠️ her ÖZGÜN şeklin TEK sahibi var (faz 3 hariç)",
                            ihlal == 0, ihlal);

            // ZİNCİR bir şekil değil MODİFİKATÖR (`Then`), o yüzden `Şekil()`
            // onu görmüyor — ama tekeli aynı: "bir cevaba yerleşemezsin" sorusu
            // MURABAHACI'nın. Ayrı soruluyor, yoksa kuraldan sessizce kaçardı.
            int zincirİhlali = 0;
            foreach (var b in BossKind.All)
                for (int f = 1; f <= b.PhaseCount && f < 3; f++)
                    foreach (var a in b.Attacks(f))
                        if (a.Then != null && b.Name != "MURABAHACI") zincirİhlali++;
            failed += Check(ref total, "⚠️ ZİNCİR yalnızca MURABAHACI'da (faz 3 hariç)",
                            zincirİhlali == 0, zincirİhlali);

            // Tekelin öbür yarısı: sahip o şekli GERÇEKTEN kullanıyor mu.
            // Yalnızca "başkası kullanmasın" denseydi kural boş listeyle de geçerdi.
            int sahipsiz = 0;
            foreach (var (şekil, sahip) in Tekel)
            {
                bool var = false;
                var sb = Bul(sahip);
                for (int f = 1; f <= 2; f++)
                    foreach (var kök in sb.Attacks(f))
                        for (var a = kök; a != null; a = a.Then)
                            if (Şekil(a) == şekil) var = true;
                if (!var) { sahipsiz++; Debug.LogError($"    {sahip} kendi imzasını ({şekil}) kullanmıyor"); }
            }
            failed += Check(ref total, "⚠️ her boss KENDİ imzasını gerçekten kullanıyor",
                            sahipsiz == 0, sahipsiz);

            // ── 8c) ⚠️ PAYLAŞILAN İKİLİ her fazda var — Enes'in şartı ─────────
            //    "Her birinde özgün bir mekanik, genel bir DÜZ saldırı ve
            //     MENZİLLİ saldırı." Ve mesafeye göre seçim geldiği için bu
            //    artık bir üslup değil bir GEREKLİLİK: yakın bandı boş olan
            //    boss dibindeyken çaresiz kalır.
            int bandsız = 0;
            foreach (var b in BossKind.All)
                for (int f = 1; f <= b.PhaseCount; f++)
                {
                    bool yakın = false, uzakV = false;
                    foreach (var a in b.Attacks(f))
                    {
                        if (a.Uzak) uzakV = true;
                        else if (!a.Harita) yakın = true;
                    }
                    if (!yakın || !uzakV)
                    {
                        bandsız++;
                        Debug.LogError($"    {b.Name} faz {f}: yakın={yakın} uzak={uzakV}");
                    }
                }
            failed += Check(ref total, "⚠️ her boss'un HER fazında hem YAKIN hem UZAK saldırı var",
                            bandsız == 0, bandsız);

            // ── 8d) ⚠️⚠️ ZORLUK TIRMANIYOR — boss sırası bir SÖZ ──────────────
            //    Enes: *"aga ilk boss 2.sinden daha güçlü durmuş :D"*
            //    Haklıydı ve göz kararı bulunamazdı: TAHSİLDAR TAM aura emiyordu
            //    (beşinin en yükseği) ve bedel çarpanları da en ağırıydı.
            //    Oyuncunun doğrudan hissettiği iki kadran bunlar — ikisi de
            //    boss sırasıyla ARTMAK zorunda.
            float öncekiAura = -1f, öncekiBedel = -1f;
            int tersTırmanış = 0;
            foreach (var b in BossKind.All)
            {
                float aura = b.AuraDrain(t, 1);
                float bedel = 0f;
                foreach (var a in b.Attacks(1)) bedel += a.CostMul;
                bedel /= b.Attacks(1).Length;

                Debug.Log($"    {b.Name}: aura {aura:0.00} · ortalama bedel {bedel:0.00} · " +
                          $"faz1 {b.Attacks(1).Length} saldırı");
                if (aura < öncekiAura - 0.001f || bedel < öncekiBedel - 0.001f) tersTırmanış++;
                öncekiAura = aura; öncekiBedel = bedel;
            }
            failed += Check(ref total, "⚠️⚠️ boss sırası boyunca EMME ve BEDEL tırmanıyor",
                            tersTırmanış == 0, tersTırmanış);

            // ── 8e) ⚠️⚠️ FAZ ATLAYINCA ETKİLER GÜÇLENİYOR ────────────────────
            //    Enes: *"faz atlayınca etkilerinin güçlenmesini [istiyorum]"* —
            //    ilk raporunda da yazmıştı: *"faz değişiminde yeni özgün
            //    saldırılar eklenip hem mekanik hem de saldırılar güçlenecek."*
            //
            //    ⚠️ Bu kural ELLE tutulamaz. Dağarcık 97 saldırıya çıktı; bir
            //    sayıyı yanlış yöne çevirmek gözle görülmez, oyunda "faz 2 daha
            //    kolay geldi" diye çıkar. Ve "+ koydum, demek ki güçlendi"
            //    varsayımı bir AD kuralıdır, etki değil.
            //
            //    Ölçü her şekle AYNI formülle bakıyor: bedel × erişim × tekrar ×
            //    güvenli yerin DARALMASI. Son çarpan olmasaydı göbeği 2.4'ten
            //    1.1'e düşen bir simit "güçlenmemiş" sayılırdı — oysa oyuncunun
            //    hissettiği asıl şey tam olarak o.
            int gerileyen = 0;
            foreach (var b in BossKind.All)
                for (int f = 1; f < b.PhaseCount; f++)
                {
                    var önceki = ŞekilSertlikleri(b.Attacks(f));
                    var sonraki = ŞekilSertlikleri(b.Attacks(f + 1));
                    foreach (var kv in önceki)
                    {
                        if (!sonraki.TryGetValue(kv.Key, out float yeni)) continue;  // o fazda yok
                        if (yeni <= kv.Value + 0.001f)
                        {
                            gerileyen++;
                            Debug.LogError($"    {b.Name} {kv.Key}: faz {f} {kv.Value:0.0} → " +
                                           $"faz {f + 1} {yeni:0.0} (GÜÇLENMEMİŞ)");
                        }
                    }

                    // ZİNCİR bir şekil değil, UZUNLUK. Ayrıca ölçülüyor.
                    int z1 = EnUzunZincir(b.Attacks(f)), z2 = EnUzunZincir(b.Attacks(f + 1));
                    if (z1 > 1 && z2 <= z1)
                    {
                        gerileyen++;
                        Debug.LogError($"    {b.Name} ZİNCİR: faz {f} {z1} halka → faz {f + 1} {z2}");
                    }
                }
            failed += Check(ref total, "⚠️⚠️ her ŞEKİL bir sonraki fazda GÜÇLENİYOR",
                            gerileyen == 0, gerileyen);

            // ── 8f) ⚠️ BOSS'UN KENDİ MEKANİĞİ de faz başına güçleniyor ────────
            //    Saldırılar güçlenip mekanik aynı kalsaydı faz geçişi "aynı adam,
            //    daha sert vuruyor" olurdu. Enes'in istediği bu değil: mekanik de
            //    tırmanmalı. Ölçü boss'un KENDİ yöntemlerinden okunuyor.
            //
            //    ⚠️ TEK TEK değil TOPLAM ölçülüyor, ve bu bilerek: TAHSİLDAR faz
            //    2'de emmeyi BIRAKIP hızlanıyor — tasarımın kendisi bu ("aynı
            //    tehdidin daha çoğu değil, BAŞKA bir soru"). Her kadranın tek tek
            //    artmasını şart koşsaydım o takası yasaklamış olurdum. Kural şu:
            //    boss bir kadranı bırakabilir, ama toplam baskı ARTMAK zorunda.
            int mekanikGerileyen = 0;
            foreach (var b in BossKind.All)
            {
                var ölçüler = new System.Collections.Generic.List<float>();
                for (int f = 1; f <= b.PhaseCount; f++) ölçüler.Add(Baskı(b, t, f));
                Debug.Log($"    {b.Name} mekanik baskısı: {string.Join(" → ", ölçüler.ConvertAll(x => x.ToString("0.00")))}");
                for (int f = 1; f < ölçüler.Count; f++)
                    if (ölçüler[f] <= ölçüler[f - 1] + 0.001f) mekanikGerileyen++;
            }
            failed += Check(ref total, "⚠️⚠️ boss'un MEKANİĞİ de faz başına güçleniyor",
                            mekanikGerileyen == 0, mekanikGerileyen);

            // ── 8g) ⚠️ ÖLÜ MEKANİK YOK — alan doldurulmuş ama işe yaramıyor ────
            //    İki sessiz ölüm yolu var ve ikisi de "yeşil test, bozuk oyun"
            //    üretirdi: ① arkada güvenli açıklık, ama koni zaten arkaya
            //    ulaşmıyorsa açıklık hiçbir şey değiştirmez; ② ayna ön+arka
            //    vurur, ama koni 180°'den genişse zaten her yeri vuruyordur.
            int ölüMekanik = 0;
            foreach (var a in TümSaldırılar())
            {
                if (a.SafeAngle > 0f && a.Angle + a.SafeAngle <= 360f)
                {
                    ölüMekanik++;
                    Debug.LogError($"    {a.Name}: açıklık ÖLÜ (koni {a.Angle}° zaten arkaya ulaşmıyor)");
                }
                if (a.Mirrored && a.Angle >= 180f)
                {
                    ölüMekanik++;
                    Debug.LogError($"    {a.Name}: ayna ÖLÜ (koni {a.Angle}° zaten her yeri vuruyor)");
                }
            }
            failed += Check(ref total, "⚠️ açıklık ve ayna GERÇEKTEN bir şey değiştiriyor",
                            ölüMekanik == 0, ölüMekanik);

            // ── 9) ⚠️ SİMİT gerçekten TERS soruyor — göbeği GÜVENLİ ──
            //    Bu, halkanın tersi. Çalışmazsa oyuncunun "uzaklaş" refleksi doğru olur
            //    ve saldırının varlık sebebi kalmaz.
            var (root9, clock9, kefil9) = Kur(t, Bul("KEFİL"));
            var simit = İlkSimit(Bul("KEFİL").Attacks(1));
            failed += Check(ref total, "KEFİL'in SİMİT saldırısı var", simit != null, 1);
            if (simit != null)
            {
                failed += Check(ref total, "⚠️ simidin göbeği menzilinden KÜÇÜK (güvenli alan var)",
                                simit.InnerRadius > 0f && simit.InnerRadius < 90f, simit.InnerRadius);
            }
            Temizle(root9);

            // ── 10) TEKRARLI saldırı var mı — bir kez kaçmak yetmesin ──
            bool tekrarVar = false;
            foreach (var b in BossKind.All)
                foreach (var a in b.Attacks(2))
                    if (a.Repeat > 1) tekrarVar = true;
            failed += Check(ref total, "faz 2'de TEKRARLI saldırı var (bir kez kaçmak yetmiyor)",
                            tekrarVar, 1);

            // ── 11) ⚠️ SON BOSS en geniş dağarcığa sahip ──
            //    Her boss'un EN KALABALIK fazı kıyaslanıyor. Faz 26'ya kadar yalnızca faz 2
            //    bakılıyordu; harita saldırılarıyla öbürlerinin faz 2'si 10'a çıktı, finalin
            //    asıl dağarcığı ise faz 3'te (14) — faz 2'ye bakmak kuralın amacını kaçırıyordu.
            var son = BossKind.For(BossKind.All.Length - 1);
            int EnKalabalık(BossKind b)
            {
                int en = 0;
                for (int f = 1; f <= b.PhaseCount; f++) en = Mathf.Max(en, b.Attacks(f).Length);
                return en;
            }
            int enFazla = 0;
            foreach (var b in BossKind.All) if (b != son) enFazla = Mathf.Max(enFazla, EnKalabalık(b));
            failed += Check(ref total, $"⚠️ SON BOSS en geniş dağarcığa sahip (en kalabalık fazı {EnKalabalık(son)} ≥ {enFazla})",
                            EnKalabalık(son) > enFazla, EnKalabalık(son));

            // ── 12) ⚠️ GÜVENLİ AÇIKLIK gerçekten ARKADA mı ──
            //    Öne konsaydı boss hazırlık boyunca oyuncuya döndüğü için oyuncu
            //    DAİMA içinde kalır ve saldırı hiç isabet etmezdi — mekanik sessizce
            //    anlamsızlaşırdı. Bu kontrol tam olarak onu yakalıyor.
            // ⚠️ Açıklığın sahibi artık SİMSAR (şekil tekeli). Mekanik SAHİBİNDE
            //    ölçülüyor — son boss'ta aramak, tekel kuralından sonra kapıyı
            //    yalnızca faz 3'e bakan bir kapıya çevirirdi.
            var açıklıkBoss = Bul("SİMSAR");
            var açıklıklı = İlkAçıklık(açıklıkBoss.Attacks(1)) ?? İlkAçıklık(açıklıkBoss.Attacks(2));
            failed += Check(ref total, "AÇIKLIĞIN SAHİBİ (SİMSAR) o saldırıyı taşıyor",
                            açıklıklı != null, 1);

            if (açıklıklı != null)
            {
                var (rootA, clockA, bossA) = Kur(t, açıklıkBoss);
                var windup = typeof(Enemy).GetMethod("EnterWindup",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var strike = typeof(Enemy).GetMethod("EnterStrike",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                // ⚠️ Saldırıyı hazırlıktan ÖNCE zorluyoruz: `EnterWindup` telegrafı
                // seçtiği saldırıya göre çiziyor. Sonra zorlasaydık çizilen yay BAŞKA
                // saldırınınki olurdu — ilk yazımda öyleydi ve mesafe filtresi gelip
                // seçimi değiştirene kadar şansla geçiyordu.
                // `_chained` üretimin kendi yolu: zincirlenen saldırı seçimi ezer.
                typeof(Enemy).GetField("_chained",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(bossA, açıklıklı);

                windup.Invoke(bossA, null);
                strike.Invoke(bossA, null);

                // ⚠️ Boss'un baktığı OYUNCU nesnesi — `clockA.transform` kök nesne,
                // onu oynatmak hiçbir şeyi değiştirmiyordu (test ilk yazımda öyleydi
                // ve "arkasına geçen kurtuluyor" haksız yere düşüyordu).
                var oyuncu = rootA.transform.Find("player");
                float r = bossA.CurrentStrikeRadius * 0.6f;

                // ÖNÜNDE: yakalanmalı
                oyuncu.position = bossA.transform.position + bossA.transform.forward * r;
                bool önde = bossA.PlayerInStrikeCone();

                // ARKASINDA: KURTULMALI — açıklık orada
                oyuncu.position = bossA.transform.position - bossA.transform.forward * r;
                bool arkada = bossA.PlayerInStrikeCone();

                failed += Check(ref total, "⚠️ açıklıklı halka ÖNDE duranı yakalıyor", önde, 1);
                failed += Check(ref total, "⚠️ ARKASINA geçen KURTULUYOR (açıklık arkada)",
                                !arkada, 0);

                // ⚠️ GÖRSEL MEKANİKLE UYUŞUYOR MU — bu projenin en pahalı hata türü.
                //    Yeşil dilim takip sapması olmadan bir kare sonra ÖNE dönüyordu:
                //    oyuncuya "buradasın, güvendesin" deyip vuruyordu.
                // Takip bir kare işlesin — sapmanın silinip silinmediği ancak
                // ondan SONRA görülüyor. Doğuş anına bakmak tam da kaçırılan şeydi.
                foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include))
                    a.Step(0.01f);

                // ⚠️ "En son doğan yay" diye aramak kırılgandı — `FindObjectsByType`
                // doğuş sırasını GARANTİ ETMİYOR. Güvenli dilim RENGİNDEN bulunuyor:
                // onu ayıran şey zaten yeşil olması.
                Arc yeşil = null;
                foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include))
                    if (Yeşilce(a)) yeşil = a;

                failed += Check(ref total, "güvenli dilim ÇİZİLDİ", yeşil != null, 1);
                if (yeşil != null)
                {
                    // Güvenli dilim boss'un ARKASINA bakmalı: ileri yönleri TERS olmalı
                    float uyum = Vector3.Dot(yeşil.transform.forward, bossA.transform.forward);
                    failed += Check(ref total, "⚠️ YEŞİL dilim gerçekten ARKAYA bakıyor (görsel ≠ yalan)",
                                    uyum < -0.5f, uyum);
                }
                Temizle(rootA);
            }

            // ── 13) ⚠️ ZİNCİR — farklı bir saldırı hemen ardından geliyor ──
            BossAttack zincirli = null;
            foreach (var b in BossKind.All)
                foreach (var a in b.Attacks(2))
                    if (a.Then != null) zincirli = a;
            failed += Check(ref total, "⚠️ ZİNCİRLİ saldırı var (bir cevaba yerleşemezsin)",
                            zincirli != null, 1);
            if (zincirli != null)
                failed += Check(ref total, "zincirin ikinci halkası FARKLI soru soruyor",
                                Soru(zincirli) != Soru(zincirli.Then), 1);

            // ── 14) ⚠️ MESAFE FİLTRESİ — hamleli saldırı YAKINDAN seçilmiyor ──
            //    Enes: "dash attack yaparken olduğum yerde durunca misliyor."
            //    Hamle oyuncunun üstünden aşıyordu.
            int filtresizHamle = 0;
            foreach (var a in TümSaldırılar())
                if (a.Lunge > 2f && a.MinPlayerRange <= 0f) filtresizHamle++;
            failed += Check(ref total, "⚠️ büyük HAMLELİ saldırıların hepsinde mesafe şartı var",
                            filtresizHamle == 0, filtresizHamle);

            // ── 15) ⚠️ KONUM DEĞİŞTİRTEN saldırılar GERÇEK süre veriyor ──
            //    Enes: "yeşil alana geçecek kadar süre kalmıyor, çok hızlı iniyor."
            //    Çarpan yetmiyordu — boss hazırlıkları ~0.2 sn.
            int kısaKonum = 0;
            foreach (var a in TümSaldırılar())
                // ⚠️ Haritadaki rastgele güvenli nokta da KONUM DEĞİŞTİRTEN bir
                //    saldırıdır — hatta en uzun koşuyu isteyen. Listeye eklenmeseydi
                //    yeni tip bu kuralın DIŞINDA kalır ve kural sessizce delinirdi.
                if ((a.SafeAngle > 0f || a.InnerRadius > 0f || a.SafeSpotRadius > 0f)
                    && a.WindupSeconds < 0.9f) kısaKonum++;
            failed += Check(ref total, "⚠️ simit/açıklık saldırıları ≥0.9 sn MUTLAK hazırlık veriyor",
                            kısaKonum == 0, kısaKonum);

            // ── 16) ⚠️ SİMSAR "dur ve vur"u cezalandırıyor ──
            var simsar = Bul("SİMSAR");
            failed += Check(ref total, "SİMSAR tanımlı", simsar.Name == "SİMSAR", 1);
            failed += Check(ref total, "⚠️ SİMSAR durduğun YERE işaret bırakıyor",
                            simsar.GroundMarkInterval(1) > 0f, simsar.GroundMarkInterval(1));
            failed += Check(ref total, "faz 2'de daha SIK bırakıyor",
                            simsar.GroundMarkInterval(2) < simsar.GroundMarkInterval(1),
                            simsar.GroundMarkInterval(2));

            int işaretsiz = 0;
            foreach (var b in BossKind.All)
                if (b != simsar && b.GroundMarkInterval(2) > 0f) işaretsiz++;
            failed += Check(ref total, "yer işareti SİMSAR'a özel (kimlik dağılmıyor)",
                            işaretsiz == 0, işaretsiz);

            // ⚠️ ETKİ testi: aralık değerini okumak ÖZELLİK testidir. İşaret
            // GERÇEKTEN doğuyor mu ve DURDUĞUN YERE mi doğuyor — asıl soru bu.
            foreach (var g in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include))
                Object.DestroyImmediate(g.gameObject);

            var (rootS, clockS, bossS) = Kur(t, simsar);
            var oyuncuS = rootS.transform.Find("player");
            oyuncuS.position = Bootstrap.ArenaCenter + Vector3.right * 5f;

            bossS.TickBossPassives(simsar.GroundMarkInterval(1) + 0.1f);
            var işaretler = Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include);
            failed += Check(ref total, "⚠️ SİMSAR GERÇEKTEN işaret bırakıyor",
                            işaretler.Length > 0, işaretler.Length);

            if (işaretler.Length > 0)
            {
                float d = Vector3.Distance(işaretler[0].transform.position, oyuncuS.position);
                failed += Check(ref total, "⚠️ işaret OYUNCUNUN DURDUĞU yere bırakılıyor",
                                d < 0.01f, d);
            }

            foreach (var g in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include))
                Object.DestroyImmediate(g.gameObject);
            Temizle(rootS);

            // ── 16b) MESAFEYE GÖRE SALDIRI SEÇİMİ → Faz25SelfCheck ──────────────
            //    Buradaki kontrol `PickAttack`'i 0.5×–7× menzil arası HER mesafede
            //    çağırıp "her saldırı bir yerde seçiliyor" diyordu ve yeşildi. Oyunda
            //    o mesafelerin çoğuna hiç gelinmiyordu: boss en uzun erişimin sınırında
            //    saldırıya giriyordu, MURABAHACI faz 1'de uzak oyuncu 7 saldırının 2'sini
            //    gördü (ders 15). Faz 25 seçimi değiştirdi (iki sıra) ve ölçümü OYUN
            //    AKIŞINDAN yapıyor: boss yürüyor, oyuncu duruyor ya da dibe giriyor.

            // ── 16c) ⚠️⚠️ ÜÇ YENİ ŞEKİL GERÇEKTEN ÇALIŞIYOR MU — ETKİ ────────
            //    Enes'in "her bossta farklı olacak" şartını üç yeni şekille
            //    karşıladım. Yalnızca "alanı dolu mu" diye sorsaydım, bu projenin
            //    en pahalı hata türünü (mekanik doğru, etki yalan) YENİ bir
            //    mekanikte tekrarlamış olurdum — dört kez oldu, dördünü de Enes
            //    oyunda buldu. Üçü de burada ÖLÇÜLÜYOR.
            var vuruş = typeof(Enemy).GetMethod("EnterStrike",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var zincir = typeof(Enemy).GetField("_chained",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hazırlık = typeof(Enemy).GetMethod("EnterWindup",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // ① AYNA — ARKA da vuruyor, YANLAR güvende kalıyor
            var (rootY, clockY, bossY) = Kur(t, Bul("EKSPER"));
            var oyuncuY = rootY.transform.Find("player");
            var ayna = Ara(Bul("EKSPER").Attacks(1), a => a.Mirrored);
            failed += Check(ref total, "AYNA saldırısı bulundu", ayna != null, 1);

            // Bulunamazsa KIRMIZI kontrol yeter; istisna atmak AllChecks'in
            // kalan kapılarını da hiç ölçülmeden düşürürdü.
            if (ayna != null) {
                zincir.SetValue(bossY, ayna);    // üretimin kendi yolu
                hazırlık.Invoke(bossY, null);
                vuruş.Invoke(bossY, null);
                float rY = bossY.CurrentStrikeRadius * 0.5f;

                oyuncuY.position = bossY.transform.position + bossY.transform.forward * rY;
                bool önY = bossY.PlayerInStrikeCone();
                oyuncuY.position = bossY.transform.position - bossY.transform.forward * rY;
                bool arkaY = bossY.PlayerInStrikeCone();
                oyuncuY.position = bossY.transform.position + bossY.transform.right * rY;
                bool yanY = bossY.PlayerInStrikeCone();

                failed += Check(ref total, "⚠️ AYNA: ÖNDEKİ vuruluyor", önY, 1);
                failed += Check(ref total, "⚠️ AYNA: ARKADAKİ de vuruluyor (geri çekilmek kurtarmıyor)",
                                arkaY, 1);
                failed += Check(ref total, "⚠️ AYNA: YANDAKİ kurtuluyor (yoksa halkadan farkı kalmaz)",
                                !yanY, 0);
            }
            Temizle(rootY);

            // ② GERİ SIÇRAMA — boss vuruş anında UZAKLAŞIYOR, arenada kalıyor
            var (rootG, clockG, bossG) = Kur(t, Bul("SİMSAR"));
            var oyuncuG = rootG.transform.Find("player");
            var sıçra = Ara(Bul("SİMSAR").Attacks(1), a => a.Recoil > 0f);
            failed += Check(ref total, "GERİ SIÇRAMA saldırısı bulundu", sıçra != null, 1);

            oyuncuG.position = bossG.transform.position + Vector3.forward * 2f;
            // Bulunamazsa KIRMIZI kontrol yeter; istisna atmak AllChecks'in
            // kalan kapılarını da hiç ölçülmeden düşürürdü.
            if (sıçra != null) {
                zincir.SetValue(bossG, sıçra);
                hazırlık.Invoke(bossG, null);
                float önceMesafe = Vector3.Distance(bossG.transform.position, oyuncuG.position);
                vuruş.Invoke(bossG, null);
                float sonraMesafe = Vector3.Distance(bossG.transform.position, oyuncuG.position);

                failed += Check(ref total, "⚠️ GERİ SIÇRAMA: boss GERÇEKTEN uzaklaşıyor (kovala)",
                                sonraMesafe > önceMesafe + 1f, sonraMesafe - önceMesafe);

                // ⚠️ ...ama arenadan ÇIKMIYOR. Çıksaydı boss erişilemez olurdu ve
                //    dövüş kilitlenirdi — arena sınırı bu oyunda yeni (Enes bildirdi).
                var dG = bossG.transform.position - Bootstrap.ArenaCenter; dG.y = 0f;
                failed += Check(ref total, "⚠️ ...ve arenanın İÇİNDE kalıyor",
                                dG.magnitude <= t.arenaRadius + 0.01f, dG.magnitude);
            }
            Temizle(rootG);

            // ③ ÇEKİM — oyuncu GERÇEKTEN boss'a doğru YER DEĞİŞTİRİYOR
            //    ⚠️ Zincirin SON halkası ölçülüyor: hız vektörü doğru yöne bakıyor
            //    mu değil, oyuncunun KONUMU yaklaştı mı. `TickHurt` bu yüzden
            //    `dt`'yi dışarıdan alıyor (CLAUDE.md tuzak 3).
            var (rootC, clockC, bossC) = Kur(t, Bul("KEFİL"));
            var oyuncuC = rootC.transform.Find("player");
            var pcC = oyuncuC.gameObject.AddComponent<PlayerController>();
            pcC.Init(t, null);

            var çekim = Ara(Bul("KEFİL").Attacks(1), a => a.Pull > 0f);
            failed += Check(ref total, "ÇEKİM saldırısı bulundu", çekim != null, 1);

            oyuncuC.position = bossC.transform.position + Vector3.forward * 4f;
            // Bulunamazsa KIRMIZI kontrol yeter; istisna atmak AllChecks'in
            // kalan kapılarını da hiç ölçülmeden düşürürdü.
            if (çekim != null) {
                zincir.SetValue(bossC, çekim);
                hazırlık.Invoke(bossC, null);
                float çekimÖnce = Vector3.Distance(bossC.transform.position, oyuncuC.position);
                vuruş.Invoke(bossC, null);
                for (int i = 0; i < 20; i++) pcC.TickHurt(0.02f);   // dt dışarıdan
                float çekimSonra = Vector3.Distance(bossC.transform.position, oyuncuC.position);

                failed += Check(ref total, "⚠️⚠️ ÇEKİM: oyuncu GERÇEKTEN boss'a çekiliyor",
                                çekimSonra < çekimÖnce - 0.5f, çekimÖnce - çekimSonra);

                // ⚠️ Ve dash DOKUNULMAZLIĞI çekimden de korumalı: korumasaydı dash
                //    "kaçtım ama yine de içeri alındım" olur ve vaadi delinirdi.
                oyuncuC.position = bossC.transform.position + Vector3.forward * 4f;
                typeof(PlayerController).GetField("_iFrames",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(pcC, 1f);
                float dokunulmazÖnce = Vector3.Distance(bossC.transform.position, oyuncuC.position);
                vuruş.Invoke(bossC, null);
                for (int i = 0; i < 20; i++) pcC.TickHurt(0.02f);
                float dokunulmazSonra = Vector3.Distance(bossC.transform.position, oyuncuC.position);

                failed += Check(ref total, "⚠️ ÇEKİM dash DOKUNULMAZLIĞINI delmiyor",
                                Mathf.Abs(dokunulmazSonra - dokunulmazÖnce) < 0.01f,
                                dokunulmazÖnce - dokunulmazSonra);
            }
            Temizle(rootC);

            // ── 16d) ⚠️⚠️ TEKRARLI saldırı GERÇEKTEN yeniden soruyor mu — ETKİ ──
            //    Enes'e "İHTARNAME'nin noktası her inişte başka yerde" ve
            //    "İHALE'nin kapısı yer değiştiriyor" diye söyledim. İkisi de
            //    `EnterWindup`'ın tekrar dalından geçmesine bağlı — yani bir
            //    İDDİA'ydılar, ölçüm değil. Bu projede tam olarak o boşluktan
            //    dört hata çıktı; ikisi de artık ölçülüyor.

            // ① Güvenli NOKTA her tekrarda yeniden seçiliyor
            var (rootN, clockN, bossN) = Kur(t, Bul("MURABAHACI"));
            var tekrarlıNokta = Ara(Bul("MURABAHACI").Attacks(1),
                                    a => a.SafeSpotRadius > 0f && a.Repeat > 1);
            failed += Check(ref total, "TEKRARLI güvenli-nokta saldırısı var",
                            tekrarlıNokta != null, 1);
            if (tekrarlıNokta != null)
            {
                zincir.SetValue(bossN, tekrarlıNokta);
                hazırlık.Invoke(bossN, null);
                Vector3 nokta1 = bossN.CurrentSafeSpot;
                hazırlık.Invoke(bossN, null);          // TEKRAR dalı
                Vector3 nokta2 = bossN.CurrentSafeSpot;

                failed += Check(ref total, "⚠️ hâlâ AYNI saldırı (tekrar dalı saldırıyı değiştirmiyor)",
                                bossN.CurrentAttack == tekrarlıNokta, 1);
                failed += Check(ref total, "⚠️⚠️ güvenli nokta HER İNİŞTE yeniden seçiliyor",
                                Vector3.Distance(nokta1, nokta2) > 0.5f,
                                Vector3.Distance(nokta1, nokta2));
            }
            Temizle(rootN);

            // ② AÇIKLIĞIN KAPISI yer değiştiriyor — boss her hazırlıkta sana dönüyor
            var (rootK, clockK, bossK) = Kur(t, Bul("SİMSAR"));
            var oyuncuK = rootK.transform.Find("player");
            var tekrarlıKapı = Ara(Bul("SİMSAR").Attacks(1),
                                   a => a.SafeAngle > 0f && a.Repeat > 1);
            failed += Check(ref total, "TEKRARLI açıklık saldırısı var", tekrarlıKapı != null, 1);
            if (tekrarlıKapı != null)
            {
                oyuncuK.position = bossK.transform.position + Vector3.forward * 3f;
                zincir.SetValue(bossK, tekrarlıKapı);
                hazırlık.Invoke(bossK, null);
                Vector3 yön1 = bossK.transform.forward;

                // Oyuncu KARŞI tarafa geçiyor: kapı arkada olduğuna göre
                // boss dönmezse oyuncu aynı yerde güvende kalırdı.
                oyuncuK.position = bossK.transform.position - Vector3.forward * 3f;
                hazırlık.Invoke(bossK, null);          // TEKRAR dalı
                Vector3 yön2 = bossK.transform.forward;

                failed += Check(ref total, "⚠️⚠️ tekrarda boss YENİDEN dönüyor (kapı yer değiştiriyor)",
                                Vector3.Dot(yön1, yön2) < -0.5f, Vector3.Dot(yön1, yön2));
            }
            Temizle(rootK);

            // ── 17) Bölge → boss eşlemesi ──
            bool eşleşme = true;
            for (int i = 0; i < BossKind.All.Length; i++)
                if (BossKind.For(i) != BossKind.All[i]) eşleşme = false;
            failed += Check(ref total, "her bölgenin kendi alacaklısı var", eşleşme, 1);

            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset(); Enemy.All.Clear();

            if (failed == 0) Debug.Log($"[Faz 12 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 12 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        static string İmza(BossKind b, Tuning t) =>
            $"{b.RefillsOnEmpty}|{b.SplitsOnPhaseTwo}|{b.PlayerHarvest(1)}|{b.PlayerHarvest(2)}|" +
            $"{b.SpeedMultiplier(2)}|{b.RegenPerSecond(t, 2)}|{b.AuraDrain(t, 1)}|{b.AuraDrain(t, 2)}";

        /// <summary>
        /// Saldırının ŞEKLİ — yani oyuncuya sorduğu KAÇIŞ SORUSU.
        /// ⚠️ Tek sahipli olanlar (`Sahip`) bu adlarla eşleşiyor.
        /// </summary>
        public static string Şekil(BossAttack a) =>
              a.SafeSpotRadius > 0f ? "NOKTA(oraya yetiş)"
            : a.InnerRadius > 0f    ? "SİMİT(yaklaş)"
            : a.SafeAngle > 0f      ? "AÇIKLIK(arkasına geç)"
            : a.Mirrored            ? "AYNA(yana kaç, geri değil)"
            : a.Pull > 0f           ? "ÇEKİM(kaçmak işe yaramaz)"
            : a.Recoil > 0f         ? "GERİ SIÇRAMA(kovala)"
            : a.Lunge > 0f          ? "HAMLE(üstüne geliyor)"
            : a.Repeat >= 3         ? "SAĞANAK(bir kez kaçmak yetmez)"
            : a.Angle >= 360f       ? "HALKA(uzaklaş)"
            : a.Angle > 0f && a.Angle < 40f ? "IŞIN(hattan çık)"
                                    : "KONİ(yana kaç)";

        /// <summary>
        /// Saldırının SORDUĞU SORU — adı değil. İki saldırı farklı ad taşıyıp aynı
        /// soruyu soruyorsa çeşitlilik sahtedir; test buna bakıyor.
        ///
        /// ⚠️ Eskiden imzada `menzil={RangeMul:0.0}` vardı ve KAÇAMAK oydu:
        /// menzili 3.0 yerine 3.2 yazmak iki özdeş saldırıyı "farklı" gösteriyordu.
        /// Enes oyunda gördü — *"hiçbir bossta birbirinin aynısı iki saldırı
        /// olmayacak"*. Yerine oyuncunun GERÇEKTEN hissettiği şeyler kondu:
        /// alanın açısı, güvenli yerin genişliği ve hangi mesafede soruluyor.
        /// </summary>
        static string Soru(BossAttack a)
        {
            string açı = a.Angle >= 360f ? "halka"
                       : a.Angle >= 120f ? "geniş"
                       : a.Angle >= 40f  ? "orta"
                                         : "dar";

            // Güvenli yerin genişliği: 2.4 birimlik göbekte rahat durursun,
            // 1.2'lik göbek iğne deliğidir. Aynı şekil, BAŞKA problem.
            float ölçü = a.InnerRadius > 0f ? a.InnerRadius
                       : a.SafeAngle > 0f ? a.SafeAngle / 30f
                       : a.SafeSpotRadius;
            string bant = ölçü <= 0f ? "-"
                        : ölçü < 1.6f ? "iğne"
                        : ölçü < 2.3f ? "dar"
                                      : "geniş";

            // ⚠️ ERİŞİM kaba ölçekte (kısa/uzun): TAHSİL ile KUŞATMA aynı soruyu soruyor
            // ama KUŞATMA'da GERÇEKTEN koşman gerekiyor. Faz 25'e kadar bu ayrım `Uzak`
            // bayrağından geliyordu; Faz 26'da tür veriye geçince imzadan düştü ve
            // beş çift "kopya" göründü. Tam sayı DEĞİL, eşik — sayı kaçamağı olmasın.
            // ⚠️ "harita" üçüncü basamak, TÜR değil: arena boyu saldırıda "menzilden çık"
            // cevabı KAPALI — TEMİNAT'ın iki cevabı var (gir ya da dışarı kaç), TOPLU
            // TEMİNAT'ın tek. `tür=` token'ı bunu da, yakın/uzak ayrımını da ölçüsüzce
            // ayırıyordu ve dört gerçek kopyayı saklıyordu (inceleme 2026-10-10).
            string erişim = a.Harita ? "harita" : a.RangeMul > BossAttack.UzakEşiği ? "uzun" : "kısa";
            return $"{Şekil(a)}|{açı}|{bant}|tekrar={a.Repeat > 1}" +
                   $"|zincir={a.Then != null}|erişim={erişim}";
        }

        /// <summary>
        /// TEK SAHİPLİ şekiller: hangi boss'un imzası hangisi.
        /// ⚠️ Paylaşılan yalnızca KONİ ve IŞIN — Enes ikisini herkese şart
        /// koştu ("genel bir düz saldırı ve menzilli saldırı"). Gerisi tekelli.
        /// </summary>
        static readonly (string şekil, string sahip)[] Tekel =
        {
            ("HALKA(uzaklaş)",            "TAHSİLDAR"),
            ("HAMLE(üstüne geliyor)",     "TAHSİLDAR"),
            ("SAĞANAK(bir kez kaçmak yetmez)", "EKSPER"),
            ("AYNA(yana kaç, geri değil)",     "EKSPER"),
            ("SİMİT(yaklaş)",             "KEFİL"),
            ("ÇEKİM(kaçmak işe yaramaz)", "KEFİL"),
            ("AÇIKLIK(arkasına geç)",     "SİMSAR"),
            ("GERİ SIÇRAMA(kovala)",      "SİMSAR"),
            ("NOKTA(oraya yetiş)",        "MURABAHACI"),
        };

        static string Dizi(BossAttack[] xs)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var a in xs) sb.Append(a.Name).Append(':').Append(Soru(a)).Append('|');
            return sb.ToString();
        }

        /// <summary>
        /// Bütün boss'ların bütün fazlarındaki bütün saldırılar — ZİNCİRLENMİŞ olanlar
        /// DÂHİL. ⚠️ Zinciri atlayan bir tarama, `Then` içine gizlenmiş bir hatayı
        /// göremezdi; ilk yazımda tam olarak öyleydi.
        /// </summary>
        static System.Collections.Generic.List<BossAttack> TümSaldırılar()
        {
            var liste = new System.Collections.Generic.List<BossAttack>();
            foreach (var b in BossKind.All)
                for (int f = 1; f <= b.PhaseCount; f++)     // ⚠️ faz 3 DÂHİL
                    foreach (var a in b.Attacks(f))
                        for (var x = a; x != null; x = x.Then) liste.Add(x);
            return liste;
        }

        /// <summary>Güvenli bölge yayı mı — yeşil olması onu ayıran şey.</summary>
        static bool Yeşilce(Arc a)
        {
            var mpb = new MaterialPropertyBlock();
            var r = a.GetComponent<Renderer>();
            if (r == null) return false;
            r.GetPropertyBlock(mpb);
            var c = mpb.GetColor("_BaseColor");
            return c.g > 0.7f && c.g > c.r + 0.3f;
        }

        /// <summary>
        /// Bir saldırının SERTLİĞİ. Bedel × erişim × tekrar × güvenli yerin
        /// DARALMASI.
        ///
        /// ⚠️ Son çarpan olmazsa kural yarım kalırdı: göbeği 2.4'ten 1.1'e düşen
        /// bir simit, erişimi aynı kaldıysa "güçlenmemiş" sayılırdı — oysa
        /// oyuncunun hissettiği asıl şey tam olarak güvenli yerin daralması.
        /// Bölenlerdeki taban değerler sıfıra bölmeyi değil, SAÇMA ÇARPANI
        /// engelliyor (0.1'lik bir göbek 30× sertlik yazardı).
        /// </summary>
        static float Sertlik(BossAttack a)
        {
            float daralma = a.InnerRadius > 0f ? 3f / Mathf.Max(0.5f, a.InnerRadius)
                          : a.SafeAngle > 0f ? 180f / Mathf.Max(20f, a.SafeAngle)
                          : a.SafeSpotRadius > 0f ? 4f / Mathf.Max(1f, a.SafeSpotRadius)
                          : 1f;
            // ⚠️ Ortaya sıçrayan halkada (TAM TAHSİL) `RangeMul` yalnızca erişim; boyu `ArenaOranı`.
            //    RangeMul'a baksaydık faz 2'de halka küçülse de "güçlendi" derdik.
            float boy = a.ArenaOranı > 0f ? a.ArenaOranı : a.RangeMul;
            return a.CostMul * boy * Mathf.Max(1, a.Repeat) * daralma;
        }

        /// <summary>Her şeklin bu fazdaki EN SERT örneği.</summary>
        static System.Collections.Generic.Dictionary<string, float> ŞekilSertlikleri(BossAttack[] xs)
        {
            var d = new System.Collections.Generic.Dictionary<string, float>();
            foreach (var a in xs)
            {
                // ⚠️ Harita saldırısı AYRI basamak: RangeMul 11 şeklin en serti olup aynı
                // şeklin normal saldırısındaki gerilemeyi örtüyordu (inceleme 2026-10-10).
                string ş = Şekil(a) + (a.Harita ? "·harita" : "");
                float s = Sertlik(a);
                if (!d.TryGetValue(ş, out float v) || s > v) d[ş] = s;
            }
            return d;
        }

        static int EnUzunZincir(BossAttack[] xs)
        {
            int en = 0;
            foreach (var a in xs)
            {
                int n = 0;
                for (var x = a; x != null; x = x.Then) n++;
                if (n > en) en = n;
            }
            return en;
        }

        /// <summary>
        /// Boss'un KENDİ mekaniğinin bu fazdaki baskısı — saldırıları değil,
        /// imzasını ölçüyor (emme, hız, hasat kesintisi, geri sarma, yer
        /// işareti, çağırma, bölünme, rakip kapısı).
        ///
        /// ⚠️ Boss'un kendi yöntemlerinden okunuyor, elle tutulan bir tablodan
        /// değil: tablo olsaydı bir kadran değiştiğinde tablo eskir ve kapı
        /// doğru kodu yanlış bulurdu (Faz 18'in "özgün" listesi tam olarak
        /// böyle eskimişti).
        /// </summary>
        static float Baskı(BossKind b, Tuning t, int faz) =>
              b.AuraDrain(t, faz)
            + (b.SpeedMultiplier(faz) - 1f) * 2f
            + (1f - b.PlayerHarvest(faz)) * 2f
            + b.RegenPerSecond(t, faz) * 2f
            + (b.GroundMarkInterval(faz) > 0f ? 3f / b.GroundMarkInterval(faz) : 0f)
            + (b.SummonInterval(faz) > 0f ? 8f / b.SummonInterval(faz) : 0f)
            + (b.SummonsRivalsInPhase == faz ? 6f : 0f)
            + (b.SplitsOnPhaseTwo && faz >= 2 ? 2f : 0f);

        /// <summary>
        /// Saldırıyı ÖZELLİĞİNDEN bulur, indisinden değil.
        /// ⚠️ `Attacks(1)[3]` yazmak kırılgan: liste sırası değişince test
        /// sessizce BAŞKA bir saldırıyı ölçer ve yeşil kalır.
        /// </summary>
        static BossAttack Ara(BossAttack[] xs, System.Func<BossAttack, bool> şart)
        {
            foreach (var a in xs) if (şart(a)) return a;
            return null;
        }

        static BossAttack İlkAçıklık(BossAttack[] xs)
        {
            foreach (var a in xs) if (a.SafeAngle > 0f) return a;
            return null;
        }

        static BossAttack İlkSimit(BossAttack[] xs)
        {
            foreach (var a in xs) if (a.InnerRadius > 0f) return a;
            return null;
        }

        static BossKind Bul(string ad)
        {
            foreach (var b in BossKind.All) if (b.Name == ad) return b;
            return BossKind.All[0];
        }

        static (GameObject, Clock, Enemy) Kur(Tuning t, BossKind boss)
        {
            Enemy.All.Clear();
            var root = new GameObject("faz12-check");
            var player = new GameObject("player"); player.transform.SetParent(root.transform);
            player.transform.position = Bootstrap.ArenaCenter;

            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, t.startSeconds);

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(root.transform);
            go.transform.position = Bootstrap.ArenaCenter + Vector3.forward * 2f;
            go.transform.localScale = Bootstrap.EnemyScale * 1.9f;
            var e = go.AddComponent<Enemy>();
            e.Init(t, player.transform, null, 5f, true, null, boss);
            return (root, clock, e);
        }

        static Enemy Make(Tuning t, Transform parent, EnemyKind kind)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(parent);
            go.transform.position = Bootstrap.ArenaCenter + Vector3.right * 2f;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(t, parent, null, 1f, false, kind);
            return e;
        }

        static void Temizle(GameObject root)
        {
            foreach (var e in Enemy.All.ToArray())
                if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
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
