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

                // Aynı boss içinde iki saldırı aynı soruyu soruyorsa biri gereksiz.
                for (int i = 0; i < f1.Length; i++)
                    for (int j = i + 1; j < f1.Length; j++)
                        if (Soru(f1[i]) == Soru(f1[j])) kopya++;
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
            var son = BossKind.For(BossKind.All.Length - 1);
            int enFazla = 0;
            foreach (var b in BossKind.All) enFazla = Mathf.Max(enFazla, b.Attacks(2).Length);
            failed += Check(ref total, "⚠️ SON BOSS en geniş dağarcığa sahip",
                            son.Attacks(2).Length >= enFazla, son.Attacks(2).Length);

            // ── 12) ⚠️ GÜVENLİ AÇIKLIK gerçekten ARKADA mı ──
            //    Öne konsaydı boss hazırlık boyunca oyuncuya döndüğü için oyuncu
            //    DAİMA içinde kalır ve saldırı hiç isabet etmezdi — mekanik sessizce
            //    anlamsızlaşırdı. Bu kontrol tam olarak onu yakalıyor.
            var açıklıklı = İlkAçıklık(son.Attacks(1)) ?? İlkAçıklık(son.Attacks(2));
            failed += Check(ref total, "SON BOSS'un güvenli açıklıklı saldırısı var",
                            açıklıklı != null, 1);

            if (açıklıklı != null)
            {
                var (rootA, clockA, bossA) = Kur(t, son);
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
        /// Saldırının SORDUĞU SORU — adı değil. İki saldırı farklı ad taşıyıp aynı
        /// soruyu soruyorsa çeşitlilik sahtedir; test buna bakıyor.
        /// </summary>
        static string Soru(BossAttack a)
        {
            string şekil = a.InnerRadius > 0f ? "SİMİT(yaklaş)"
                         : a.SafeAngle > 0f ? "AÇIKLIK(arkasına geç)"
                         : a.Angle >= 360f ? "HALKA(uzaklaş)"
                         : a.Angle > 0f && a.Angle < 40f ? "IŞIN(hattan çık)"
                         : "KONİ(yana kaç)";
            return $"{şekil}|tekrar={a.Repeat > 1}|zincir={a.Then != null}" +
                   $"|hamle={a.Lunge > 0f}|menzil={a.RangeMul:0.0}";
        }

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
                for (int f = 1; f <= 2; f++)
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
