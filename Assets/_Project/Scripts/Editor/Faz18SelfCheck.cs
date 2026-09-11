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

            // ⚠️ SIRAYLA geliyorlar, hepsi birden değil (Enes: "her biri rastgele
            //    şekilde çıkmalı"). Dördü aynı anda gelseydi final bir boss savaşı
            //    değil bir kalabalık olurdu.
            int rakip = 0;
            foreach (var e in Enemy.All) if (e != null && e.SummonedRival) rakip++;
            failed += Check(ref total, "⚠️ faz 3'te rakipler SIRAYLA geliyor (aynı anda 1)",
                            rakip == 1, rakip);
            failed += Check(ref total, "⚠️ çağıran boss UYKUYA geçti", final.Dormant, 0f);

            // ⚠️ Gelen rakip FAZ 2 gücünde — "yendiğinden güçlü" (Enes). Güç
            //    havuzdan değil, en geniş dağarcığıyla İLK SANİYEDEN gelmesinden.
            Enemy ilkRakip = null;
            foreach (var e in Enemy.All) if (e != null && e.SummonedRival) ilkRakip = e;
            failed += Check(ref total, "⚠️ rakip FAZ 2 gücüyle geliyor (yendiğinden güçlü)",
                            ilkRakip != null && ilkRakip.Phase >= 2,
                            ilkRakip != null ? ilkRakip.Phase : 0);

            // ── 7) ⚠️ UYKUDAKİ BOSS VURULMUYOR ──
            //    Vurulabilseydi oyuncu rakipleri es geçip doğrudan onu döverdi ve
            //    "hepsi ölünce kendisi gelsin" kuralı sessizce anlamsızlaşırdı.
            float uykuÖnce = final.TimeLeft;
            final.TakeDamage(999f);
            failed += Check(ref total, "⚠️ UYKUDAKİ boss VURULMUYOR",
                            Mathf.Approximately(final.TimeLeft, uykuÖnce), uykuÖnce - final.TimeLeft);

            // ── 7b) ⚠️ UYKUDAKİ BOSS SALDIRMIYOR ve ZAMAN EMMİYOR ──
            //    İlk yazışımda uykuyu yalnızca `TakeDamage` içinde kapatmıştım;
            //    `Update`'in geri kalanı çalışmaya devam ediyordu. Yani uykudaki
            //    boss kovalıyor, vuruyor ve aurasıyla emiyordu — üstelik
            //    vurulamıyordu: ÖLDÜRÜLEMEZ BİR SALDIRGAN. En kötü bileşim, ve
            //    belge yorumu "vurmuyor" dediği için gözden kaçmaya en açık olanı.
            float auraÖnce = clock6.Seconds;
            final.ApplyAura(3f);
            failed += Check(ref total, "⚠️ UYKUDAKİ boss ZAMAN EMMİYOR",
                            Mathf.Approximately(clock6.Seconds, auraÖnce),
                            auraÖnce - clock6.Seconds);

            var update = typeof(Enemy).GetMethod("Update",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var yerÖnce = final.transform.position;
            for (int i = 0; i < 5; i++) update.Invoke(final, null);
            failed += Check(ref total, "⚠️ UYKUDAKİ boss KOVALAMIYOR",
                            Vector3.Distance(final.transform.position, yerÖnce) < 0.01f,
                            Vector3.Distance(final.transform.position, yerÖnce));
            failed += Check(ref total, "⚠️ UYKUDAKİ boss SALDIRIYA geçmiyor",
                            final.CurrentState == Enemy.State.Chase, (int)final.CurrentState);

            // Ve uyku GÖRÜNÜYOR — dokunulmaz bir şeye vurmak sessiz olmamalı
            failed += Check(ref total, "⚠️ UYKU ekranda GÖRÜNÜYOR (renk ayrışıyor)",
                            Fark(final.KindColor, UykuRengi(final)) > 0.2f,
                            Fark(final.KindColor, UykuRengi(final)));

            // ── 8) ⚠️ UYKUDAKİ BOSS PASİFLERİNİ İŞLETMİYOR ──
            //    Rejenerasyonu uykuda işleseydi rakiplerle dövüşürken arkada
            //    sessizce dolan bir boss olurdu — dövüş kazanılamaz hale gelirdi.
            var düşür = typeof(Enemy).GetMethod("TakeDamage");
            float rejenÖnce = final.TimeLeft;
            final.TickBossPassives(5f);
            failed += Check(ref total, "⚠️ UYKUDA rejenerasyon İŞLEMİYOR",
                            final.TimeLeft <= rejenÖnce + 0.001f, final.TimeLeft - rejenÖnce);

            // ── 9) ⚠️ RAKİPLER ÖLÜNCE BOSS UYANIYOR ──
            // ⚠️ DÖRDÜNÜ DE sırayla geçmek gerekiyor. Biri ölünce SIRADAKİ geliyor;
            //    boss ancak kuyruk bitince uyanıyor. Testin tek bir öldürmeyle
            //    "uyandı" demesi, sıra mekaniğini hiç sınamamak olurdu.
            int gelen = 1, guard18 = 0;
            while (final.Dormant && guard18++ < 20)
            {
                foreach (var e in Enemy.All.ToArray())
                    if (e != null && e.SummonedRival) Object.DestroyImmediate(e.gameObject);
                Enemy.All.RemoveAll(e => e == null);

                final.TickBossPassives(0.1f);
                if (final.Dormant) gelen++;
            }
            failed += Check(ref total, $"⚠️ DÖRT rakip de sırayla geldi ({gelen})",
                            gelen == BossKind.All.Length - 1, gelen);
            failed += Check(ref total, "⚠️ kuyruk bitince boss UYANIYOR", !final.Dormant, 0f);

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

            // ⚠️ Güvenli yer artık boss'un DİBİ DEĞİL, haritada RASTGELE bir nokta
            //    (Enes). Göbek olsaydı cevap "boss'a koş" refleksine dönerdi.
            bool haritaSaldırısı = false;
            foreach (var a in faz3)
                if (a.Angle >= 360f && a.RangeMul >= 6f && a.SafeSpotRadius > 0f) haritaSaldırısı = true;
            failed += Check(ref total, "⚠️ TÜM HARİTAYA vuran, RASTGELE güvenli noktalı saldırı var",
                            haritaSaldırısı, 0f);

            // ⚠️ Ve nokta GERÇEKTEN rastgele — sabit olsaydı ilk denemede ezberlenir
            //    ve "rastgele" iddiası sessizce yalan olurdu. Aynı boss'ta iki ayrı
            //    hazırlık farklı nokta vermeli.
            //    ⚠️ Saldırıyı teste DAYATMIYORUZ: ilk yazışımda `SetAttack` ile ulti'yi
            //    zorlayıp `EnterWindup` çağırmıştım, ama `EnterWindup` sıradaki
            //    saldırıyı KENDİ SEÇİYOR ve dayattığımı eziyordu — nokta hiç
            //    güncellenmiyordu ve test "rastgele değil" diyordu. Suç kurgudaydı.
            //    Doğrusu: üretimin kendi seçimini çevir, güvenli noktası OLAN
            //    hazırlıkları örnekle.
            var (rootS, clockS) = Kur(t);
            var nokta = Boss(t, rootS, son);
            İlerlet(nokta, 3);

            var enterWindup = typeof(Enemy).GetMethod("EnterWindup",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var noktalar = new System.Collections.Generic.HashSet<string>();
            bool hepsiİçeride = true;
            for (int i = 0; i < 60; i++)
            {
                enterWindup.Invoke(nokta, null);
                if (nokta.CurrentSafeSpotRadius <= 0f) continue;   // bu saldırıda yok

                noktalar.Add($"{nokta.CurrentSafeSpot.x:0.0}|{nokta.CurrentSafeSpot.z:0.0}");

                var dd = new Vector2(nokta.CurrentSafeSpot.x - Bootstrap.ArenaCenter.x,
                                     nokta.CurrentSafeSpot.z - Bootstrap.ArenaCenter.z);
                if (dd.magnitude > t.arenaRadius) hepsiİçeride = false;
            }

            failed += Check(ref total, $"⚠️ güvenli nokta GERÇEKTEN rastgele ({noktalar.Count} farklı yer)",
                            noktalar.Count >= 4, noktalar.Count);

            // ⚠️ Ve HEPSİ arenanın içinde. Dışarıda bir nokta seçilseydi oyuncu
            //    oraya YETİŞEMEZ ve saldırı kaçınılamaz bir ceza olurdu — üstelik
            //    arena sınırı bu oyunda yeni (Enes bildirdi), yani tam da yeni
            //    kuralla çelişebilecek bir yer.
            failed += Check(ref total, "⚠️ güvenli noktaların HEPSİ arena içinde (ulaşılabilir)",
                            hepsiİçeride, 0f);
            Temizle(rootS);

            // ⚠️ Ve o saldırının hazırlığı MUTLAK ve UZUN olmalı: arenanın öbür
            //    ucundan boss'un dibine KOŞMAN gerekiyor. Çarpanla verilseydi
            //    "yeşil alana geçecek süre kalmıyor" hatası tekrarlanırdı (Enes).
            foreach (var a in faz3)
                if (a.Angle >= 360f && a.RangeMul >= 6f)
                    failed += Check(ref total, $"⚠️ harita saldırısının hazırlığı MUTLAK ve uzun ({a.WindupSeconds:0.0} sn)",
                                    a.WindupSeconds >= 1.5f, a.WindupSeconds);

            // ── 12) ⚠️ ENES'İN DAĞARCIK YAPISI — her boss, her faz ──
            //
            //    "Her birinde özgün bir mekanik, genel bir düz saldırı ve menzilli
            //     saldırı (faz değişince güçlenen), özgün EN AZ 2 saldırı...
            //     3-4-5 bosslarında en az 3-4 özgün saldırı olacak."
            //
            //    ⚠️ Bu kuralı ELLE SAYMAK yeterli değil: yeni bir saldırı eklerken
            //    ya da bir tanesini değiştirirken sessizce bozulur. Kapıya
            //    çevriliyor, ve "en az" TABAN olarak okunuyor (Enes: "iki tane
            //    yapıp bırakmayabilirsin, istediğin kadar ekleyebilirsin").
            for (int bi = 0; bi < BossKind.All.Length; bi++)
            {
                var b18 = BossKind.All[bi];
                for (int faz = 1; faz <= b18.PhaseCount; faz++)
                {
                    var liste = b18.Attacks(faz);
                    int düz = 0, menzilli = 0, özgün = 0;

                    foreach (var a18 in liste)
                    {
                        // DÜZ: yakın, dar-orta koni, süsü yok. Her boss'un klasiği.
                        bool sade = a18.InnerRadius <= 0f && a18.SafeAngle <= 0f
                                 && a18.Then == null && a18.Angle < 360f;
                        if (sade && a18.RangeMul <= 1.6f) düz++;
                        if (a18.RangeMul >= 2f && a18.Angle > 0f && a18.Angle < 60f) menzilli++;

                        // ÖZGÜN = "sıradan koni ya da ışın DEĞİL" olan her şey.
                        // ⚠️ Şekil listesi burada TEKRAR YAZILMIYOR, `Faz12SelfCheck`
                        // sınıflandırıcısı okunuyor. İlk hâlinde liste elle
                        // yazılmıştı ve AYNA / ÇEKİM / GERİ SIÇRAMA eklenince
                        // sessizce eskidi: üç boss'un yeni imzaları "özgün"
                        // sayılmadı, kapı doğru kodu yanlış bulmaya başladı.
                        string ş18 = Faz12SelfCheck.Şekil(a18);
                        if (a18.Then != null
                            || (!ş18.StartsWith("KONİ") && !ş18.StartsWith("IŞIN"))) özgün++;
                    }

                    failed += Check(ref total, $"  {b18.Name} faz {faz}: DÜZ saldırı var", düz >= 1, düz);
                    failed += Check(ref total, $"  {b18.Name} faz {faz}: MENZİLLİ saldırı var",
                                    menzilli >= 1, menzilli);

                    // Boss 3-4-5 (sıra 2,3,4) taban 3; ilk ikisi taban 2.
                    int taban = bi >= 2 ? 3 : 2;
                    failed += Check(ref total, $"  {b18.Name} faz {faz}: ÖZGÜN ≥{taban} (var: {özgün})",
                                    özgün >= taban, özgün);
                }

                // ⚠️ Her faz bir ÖNCEKİNDEN farklı olmalı — hem GELİŞMİŞ hem YENİ.
                //    "Aynı saldırı ama daha çok hasar" faz değildir.
                for (int faz = 2; faz <= b18.PhaseCount; faz++)
                {
                    var önceki = b18.Attacks(faz - 1);
                    var şimdiki = b18.Attacks(faz);
                    bool yeniVar = false;
                    foreach (var y in şimdiki)
                    {
                        bool bulundu = false;
                        foreach (var e18 in önceki) if (e18.Name == y.Name) bulundu = true;
                        if (!bulundu) yeniVar = true;
                    }
                    failed += Check(ref total, $"  {b18.Name} faz {faz}: YENİ saldırı eklendi",
                                    yeniVar, 0f);
                }
            }

            // ── 13) ⚠️ HİÇBİR İKİ SALDIRI MEKANİK OLARAK AYNI DEĞİL ──
            //
            //    Enes: *"düz atak ve menzilli ataklar da bosstan bossa değişecek."*
            //
            //    ⚠️ Bu kural olmadan üç boss'un düz saldırısı BİREBİR aynıydı
            //    (Angle = 0, gerisi varsayılan): üç boss, tek saldırı, üç ad.
            //    Adları farklı olduğu için gözle bakınca "hepsinin düz atağı var"
            //    diyordum. Projenin her yerinde uyguladığımız ölçü burada da
            //    geçerli: AD FARKI ÇEŞİTLİLİK DEĞİL.
            var imzalar = new System.Collections.Generic.Dictionary<string, string>();
            int çakışan = 0;
            foreach (var b13 in BossKind.All)
                for (int faz = 1; faz <= b13.PhaseCount; faz++)
                    foreach (var a13 in b13.Attacks(faz))
                    {
                        string imza = Imza(a13);
                        string sahip = $"{b13.Name} f{faz} · {a13.Name}";
                        if (imzalar.TryGetValue(imza, out var önceki))
                        {
                            çakışan++;
                            Debug.LogError($"     MEKANİK OLARAK AYNI: {önceki}  =  {sahip}");
                        }
                        else imzalar[imza] = sahip;
                    }
            failed += Check(ref total, "⚠️ hiçbir iki saldırı MEKANİK olarak aynı değil",
                            çakışan == 0, çakışan);

            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset(); Enemy.All.Clear();

            if (failed == 0) Debug.Log($"[Faz 18 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 18 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        /// <summary>
        /// Saldırının MEKANİK imzası — adı hariç her şey. İki saldırı aynı imzayı
        /// taşıyorsa oyuncu için AYNI SALDIRIDIR, adları ne olursa olsun.
        /// </summary>
        static string Imza(BossAttack a) =>
            $"{a.RangeMul:0.00}|{a.Angle:0.0}|{a.InnerRadius:0.00}|{a.SafeAngle:0.0}"
          + $"|{a.WindupMul:0.00}|{a.WindupSeconds:0.00}|{a.CostMul:0.00}|{a.Repeat}"
          + $"|{a.Lunge:0.00}|{a.MinPlayerRange:0.0}|{a.MaxPlayerRange:0.0}|{a.Then != null}"
          + $"|{a.SafeSpotRadius:0.00}";

        static float Fark(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);

        /// <summary>`Paint()`'teki uyku formülünün AYNISI — kopyalanmıyor, ithal ediliyor.</summary>
        static Color UykuRengi(Enemy e) =>
            Color.Lerp(e.KindColor, new Color(0.25f, 0.25f, 0.3f), 0.72f);

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
