using System.Text;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// "Oyun çok zor" şikâyetini SAYIYA çeviren araç.
    ///
    /// Sorun teşhis edilmemişti: beş büyüme eğrisi aynı anda işliyor
    /// (oda düşman sayısı × oda havuzu × bölge boss'u × bölge kuralı × tür açılımı)
    /// ve **hiçbiri diğerlerinin varlığında hesaplanmadı.** Bu araç çarpımı gösteriyor.
    ///
    /// ⚠️ Oda KOMPOZİSYONU modellenmiyor, ÜRETİMDEN OKUNUYOR: gerçek `RoomManager`
    /// odayı kuruyor, biz doğan düşmanların havuzlarını ve türlerini sayıyoruz.
    /// Kompozisyonu burada yeniden hesaplasaydık `RoomManager` değiştiğinde rapor
    /// sessizce yalan söylerdi (lessons.md: düzenek üretimi KOPYALAMAZ, İTHAL EDER).
    ///
    /// Modellenen tek şey DÖVÜŞÜN KENDİSİ, varsayımları raporun başında yazılı.
    ///
    /// Çalıştır: -executeMethod TekCan.EditorTools.ZorlukEgrisi.Run
    /// </summary>
    public static class ZorlukEgrisi
    {
        /// <summary>Oyuncu profili — `BalanceSim` ile aynı kavram, aynı isimler.</summary>
        struct Beceri
        {
            public string Ad;
            public float İsabet;        // savurmaların kaçı tutuyor
            public float DarbePerDüşman; // düşman başına yenen darbe
        }

        static readonly Beceri[] Beceriler =
        {
            new Beceri { Ad = "TEMİZ", İsabet = 0.90f, DarbePerDüşman = 0.35f },
            new Beceri { Ad = "ORTA",  İsabet = 0.65f, DarbePerDüşman = 1.10f },
        };

        /// <summary>
        /// ⚠️ Koşu boyunca DEĞİŞEN durum. İlk sürüm bunu yok sayıyordu ve rapor
        /// yanıltıcıydı: "her oda net negatif" diyordu, Enes ise yükseltmelerle
        /// 1200 saniyeye çıktı. Varsayım sonucun tamamını belirliyormuş.
        /// </summary>
        struct Durum
        {
            public float Hasat, Hasar;
            public int Alım;
        }

        /// <summary>Sunaktan alım yapan profil — asıl fark burada çıkıyor.</summary>
        struct Alışveriş
        {
            public string Ad;
            public bool Alıyor;
            public float Rezerv;      // bu kadar saniye kalacak şekilde alıyor
        }

        static readonly Alışveriş[] Alışverişler =
        {
            new Alışveriş { Ad = "ALIMSIZ", Alıyor = false },
            new Alışveriş { Ad = "ALICI  ", Alıyor = true, Rezerv = 30f },
        };

        [MenuItem("Tek Can/Zorluk eğrisi raporu")]
        public static void Run()
        {
            var t = AssetDatabase.LoadAssetAtPath<Tuning>("Assets/_Project/Resources/Tuning.asset")
                    ?? ScriptableObject.CreateInstance<Tuning>();

            // ⚠️ 30 oda ölçerken her biri checkpoint yazdırıyor. Oyuncunun kaydına
            // DEĞİL — rapor almak koşuyu silmemeli.
            SaveFile.PathOverride = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "tekcan_zorluk_rapor.json");

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("═══════ ZORLUK EĞRİSİ ═══════");
            sb.AppendLine($"varsayımlar: hasar={t.attackDamage} savurma={t.attackDuration}sn " +
                          $"hasat=%{t.harvestRate * 100f:0} dövüş akışı={t.drainInCombat}/sn " +
                          $"darbe={t.enemyFullHitCost}sn hız={t.moveSpeed}");
            sb.AppendLine("⚠️ oda KOMPOZİSYONU üretimden okunuyor; yalnızca dövüş modelleniyor");
            sb.AppendLine("⚠️ SUNAK ODASI (S): düşman yok ve SAAT DURUYOR — tek bedeli aldıkların");
            sb.AppendLine($"⚠️ SUNAK: {t.altarMaxPurchases} alım × 3 sunak, taban {t.altarBasePrice:0} sn, " +
                          $"artış ×{t.altarPriceGrowth:0.0} — SUNAK ODASINDA (S), bölgede bir kez");
            sb.AppendLine($"   hasat +{t.altarHarvestStep:0.00}/alım (tavan %{t.harvestRateCap * 100f:0}) · " +
                          $"hasar +{t.altarDamageStep:0.0}/alım (TAVAN YOK)");
            sb.AppendLine();

            foreach (var b in Beceriler)
            foreach (var alış in Alışverişler)
            {
                sb.AppendLine($"── {b.Ad} oyuncu · {alış.Ad} ──");
                sb.AppendLine("  blg oda  düşman | kazanç     NET | yürüme dövüş darbe aura | hasat hasar | biriken");
                sb.AppendLine("  ---------------+---------------+------------------------+-------------+--------");

                float saat = t.startSeconds;
                int öldüBölge = -1, öldüOda = -1;
                float tYürüme = 0f, tDövüş = 0f, tDarbe = 0f, tAura = 0f, tAlım = 0f;

                var d = new Durum { Hasat = t.harvestRate, Hasar = t.attackDamage };

                int bölgeSayısı = Mathf.Clamp(t.regionsPerAct, 1, Region.All.Length);
                for (int r = 0; r < bölgeSayısı; r++)
                {
                    for (int oda = 1; oda <= t.roomsPerRegion; oda++)
                    {
                        // ⚠️ SUNAK ODASI (Faz 19): boss'tan önceki odada DÜŞMAN YOK.
                        //    Rapor bunu modellemek ZORUNDA — modellemeseydi tam da
                        //    ölçmek için var olduğu şeyde (yükseltme ekonomisi)
                        //    yanılırdı: olmayan düşmandan hasat sayar, odanın saf
                        //    akış maliyetini görmez, alımları yanlış odalara dağıtır.
                        bool sunakOdası = t.roomsPerRegion >= 3 && oda == t.roomsPerRegion - 1;

                        Ölçüm ö;
                        if (sunakOdası)
                        {
                            // ⚠️ SAAT DURUYOR (Enes kararı): kazanç da yok, MASRAF da yok.
                            //    Odanın tek bedeli sunakta ÖDEDİĞİN saniye.
                            //    İlk sürümde burayı akış maliyetiyle modellemiştim —
                            //    tasarım değişti, model onu takip etmek ZORUNDA. Etmeseydi
                            //    ayar turu var olmayan bir masrafa göre kadran çevirirdi.
                            ö = new Ölçüm { Sayı = 0, Kazanç = 0f, Yürüme = 0f,
                                            Dövüş = 0f, Darbe = 0f, Aura = 0f, Net = 0f };
                        }
                        else ö = OdayıÖlç(t, r, oda, b, d);

                        saat += ö.Net;
                        tYürüme += ö.Yürüme; tDövüş += ö.Dövüş; tDarbe += ö.Darbe; tAura += ö.Aura;

                        // ⚠️ ALIŞVERİŞ ARTIK TEK ODADA. Eskiden her ÇİFT odada oluyordu
                        //    ve fiyat her odada tabana dönüyordu: bölge başına ~27 alım.
                        //    Faz 19 onu 9'a indirdi. Rapor bu değişikliği taklit etmezse
                        //    ayar turu hâlâ eski ekonomiye bakarak karar verir.
                        float harcandı = 0f;
                        if (alış.Alıyor && sunakOdası)
                            harcandı = Alışverişi(t, ref d, ref saat, alış.Rezerv);
                        tAlım += harcandı;

                        string boss = oda >= t.roomsPerRegion ? "B" : sunakOdası ? "S" : " ";
                        sb.AppendLine($"  {r + 1,3} {oda,3}{boss} {ö.Sayı,6} | " +
                                      $"{ö.Kazanç,6:0.0}  {ö.Net,+6:0.0} | " +
                                      $"{ö.Yürüme,6:0.0} {ö.Dövüş,5:0.0} {ö.Darbe,5:0.0} {ö.Aura,4:0.0} | " +
                                      $"%{d.Hasat * 100f,4:0} {d.Hasar,6:0.0} | " +
                                      $"{saat,7:0.0}");

                        if (saat <= 0f && öldüBölge < 0) { öldüBölge = r + 1; öldüOda = oda; }
                    }
                    var kural = Region.At(r);
                    sb.AppendLine($"      ↑ {kural.Name}" +
                                  (string.IsNullOrEmpty(kural.RuleText) ? "" : $" ({kural.RuleText})"));
                }

                sb.AppendLine(öldüBölge < 0
                    ? $"  ✅ {b.Ad}/{alış.Ad}: koşuyu {saat:0.0} saniyeyle bitiriyor" +
                      (alış.Alıyor ? $" (sunağa {tAlım:0.0} sn ödedi, {d.Alım} alım)" : "")
                    : $"  ❌ {b.Ad}/{alış.Ad}: BÖLGE {öldüBölge} ODA {öldüOda}'da saat bitiyor");

                // ⚠️ ASIL TEŞHİS: masraf nereye gidiyor. Toplam masrafa bakıp "zor" demek
                // teşhis değil; suçlu kalemi ancak yüzdeler gösteriyor.
                float tToplam = Mathf.Max(0.01f, tYürüme + tDövüş + tDarbe + tAura);
                sb.AppendLine($"  MASRAF NEREYE GİDİYOR:  yürüme %{tYürüme / tToplam * 100f:0}  ·  " +
                              $"dövüş %{tDövüş / tToplam * 100f:0}  ·  " +
                              $"yenen darbe %{tDarbe / tToplam * 100f:0}  ·  " +
                              $"aura %{tAura / tToplam * 100f:0}");
                sb.AppendLine($"  (oda başına yürüme bedeli: " +
                              $"{Vector3.Distance(Bootstrap.Entrance(t), Bootstrap.Exit(t)) / t.moveSpeed:0.0} sn yol × " +
                              $"{t.drainOutOfCombat:0.0}/sn akış)");
                sb.AppendLine();
            }

            sb.Append(KaçınmaPenceresi(t));

            try { if (System.IO.File.Exists(SaveFile.PathOverride)) System.IO.File.Delete(SaveFile.PathOverride); }
            catch { /* geçici dosya silinemedi, önemsiz */ }
            SaveFile.PathOverride = null;      // oyuncunun kaydına dönülüyor

            Debug.Log(sb.ToString());
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        // ⚠️ Masraf KIRILIMI şart: toplam masrafa bakıp "zor" demek teşhis değil.
        // Suçlunun hangi kalem olduğu ancak ayrıştırınca görünüyor.
        struct Ölçüm
        {
            public int Sayı;
            public float Havuz, Kazanç, Masraf, Net;
            public float Yürüme, Dövüş, Darbe, Aura;
        }

        /// <summary>
        /// Sunak odasında alışveriş. ⚠️ `Altar` her odada YENİDEN DOĞUYOR, yani
        /// `_bought` sıfırlanıyor ve fiyat tabana dönüyor — model bunu birebir
        /// taklit ediyor (tahmin etmiyor, üretimin davranışını yansıtıyor).
        ///
        /// Politika AÇIKÇA yazılı: rezerv kalacak şekilde, sırayla üç sunaktan
        /// tavana kadar alıyor. Politika gizli kalsaydı çıkan sayı yorumlanamazdı.
        /// </summary>
        static float Alışverişi(Tuning t, ref Durum d, ref float saat, float rezerv)
        {
            float toplam = 0f;

            // Üç sunak: HASAT, KESKİNLİK, ÇEVİKLİK. Her biri `altarMaxPurchases` kez.
            for (int sunak = 0; sunak < 3; sunak++)
            {
                for (int i = 0; i < t.altarMaxPurchases; i++)
                {
                    float fiyat = t.altarBasePrice * Mathf.Pow(t.altarPriceGrowth, i);
                    if (saat - fiyat < rezerv) break;

                    // Hasat tavana dayandıysa o sunağa para verme — oyuncu da vermez.
                    if (sunak == 0 && d.Hasat >= t.harvestRateCap - 0.001f) break;

                    saat -= fiyat;
                    toplam += fiyat;
                    d.Alım++;

                    if (sunak == 0) d.Hasat = Mathf.Min(t.harvestRateCap, d.Hasat + t.altarHarvestStep);
                    else if (sunak == 1) d.Hasar += t.altarDamageStep;   // ⚠️ TAVANI YOK
                }
            }
            return toplam;
        }

        /// <summary>
        /// Odayı GERÇEK `RoomManager` ile kurup içindekileri sayar, sonra dövüşü modeller.
        /// </summary>
        static Ölçüm OdayıÖlç(Tuning t, int bölge, int oda, Beceri b, Durum d)
        {
            Enemy.All.Clear();
            SaveFile.UseInMemory(new SaveData());

            var root = new GameObject("zorluk-ölçüm");
            var player = new GameObject("p"); player.transform.SetParent(root.transform);
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, t.startSeconds);

            var rm = root.AddComponent<RoomManager>();
            rm.Init(t, player.transform, null);
            GoToRegion(rm, bölge);
            GoToRoom(rm, oda);

            // ── üretimden OKUNAN kompozisyon ──
            // ⚠️ Havuz ve aura BİRLİKTE tutuluyor: ayrı iki liste tutup birini
            // sıralasaydım indis eşleşmesi bozulur ve her düşmanın aurası başkasının
            // ölüm anıyla çarpılırdı — sessiz, tamamen yanlış bir rapor.
            var düşmanlar = new System.Collections.Generic.List<(float havuz, float aura)>();

            int sayı = 0;
            float havuz = 0f, kazanç = 0f;
            foreach (var e in Enemy.All)
            {
                if (e == null) continue;
                sayı++;
                havuz += e.TimeLeft;

                // Vuruşla alınan: havuz × hasat × tür çarpanı × BÖLGE kuralı
                float türÇarpan = e.Kind != null ? e.Kind.HarvestMultiplier : 1f;
                float bossÇarpan = e.IsBoss ? t.bossHarvestMultiplier : 1f;
                // ⚠️ `t.harvestRate` DEĞİL, koşunun O ANKİ hasadı — sunaktan yükseliyor.
                kazanç += e.TimeLeft * d.Hasat * türÇarpan * Region.Harvest * bossÇarpan;

                düşmanlar.Add((e.TimeLeft,
                               (e.Kind != null ? e.Kind.AuraDrain(t) : 0f)
                               + (e.IsBoss ? t.bossAuraDrain : 0f)));
            }

            // ── modellenen dövüş ──
            // Etkin hasar hızı: savurma başına hasar × isabet oranı / savurma süresi
            // ⚠️ `t.attackDamage` DEĞİL, koşunun O ANKİ hasarı. Hasar yükseldikçe
            // dövüş kısalıyor, kısalan dövüş akışı ve aurayı da azaltıyor — yani
            // yükseltmeler ÜÇ kalemi birden iyileştiriyor. İlk model bunu göremiyordu.
            float dps = Mathf.Max(0.01f, d.Hasar * b.İsabet / Mathf.Max(0.01f, t.attackDuration));
            float süre = havuz / dps;

            // Girişten çıkışa yürüme — dövüş dışı, bölge akış kuralı burada işliyor
            float yol = Vector3.Distance(Bootstrap.Entrance(t), Bootstrap.Exit(t));
            float yürüme = yol / Mathf.Max(0.1f, t.moveSpeed);

            float mDövüş = (t.drainInCombat + Region.Background) * süre;
            float mYürüme = (t.drainOutOfCombat * Region.Drain + Region.Background) * yürüme;

            // ⚠️ AURA, düşman ÖLDÜKÇE AZALIR. İlk model hepsini dövüş boyunca yaşatıyordu
            // ve aurayı ciddi şekilde ABARTIYORDU — yanlış model yanlış kadranı çevirtir
            // (bkz. lessons.md, kaçınma modeli hatası).
            // Sırayla öldürüyorsun: her düşman, ondan öncekiler bitene kadar + kendi
            // süresi boyunca emiyor. Zayıftan güçlüye gidiliyor (makul oyuncu davranışı).
            düşmanlar.Sort((a, b2) => a.havuz.CompareTo(b2.havuz));
            float mAura = 0f, geçen = 0f;
            foreach (var düş in düşmanlar)
            {
                geçen += düş.havuz / dps;                    // bu düşman bu anda ölüyor
                mAura += düş.aura * geçen;                   // o ana kadar emmiş
            }
            float mDarbe = b.DarbePerDüşman * sayı * t.enemyFullHitCost * Region.HitCost;
            float masraf = mDövüş + mYürüme + mAura + mDarbe;

            KillAll();
            Object.DestroyImmediate(root);
            Clock.Reset(); RoomManager.Reset(); Region.Reset();

            return new Ölçüm { Sayı = sayı, Havuz = havuz, Kazanç = kazanç,
                               Masraf = masraf, Net = kazanç - masraf,
                               Yürüme = mYürüme, Dövüş = mDövüş, Darbe = mDarbe, Aura = mAura };
        }

        /// <summary>
        /// ④ HİSSEDİLEMEYENLER — bunlar his değil aritmetik, ölçülür.
        /// Oyuncu "haksızlık" dediğinde cevabı burada aranmalı.
        /// </summary>
        static string KaçınmaPenceresi(Tuning t)
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════ KAÇINMA PENCERESİ (his değil, aritmetik) ═══════");

            float kare = 1f / 60f;
            float iframe = t.iFrameDuration;
            float vuruş = t.enemyStrikeDuration;
            float pay = iframe - vuruş;

            sb.AppendLine($"  dash dokunulmazlığı : {iframe:0.000} sn  ({iframe / kare:0} kare)");
            sb.AppendLine($"  düşman vuruş anı    : {vuruş:0.000} sn  ({vuruş / kare:0} kare)");
            sb.AppendLine($"  PAY                 : {pay:+0.000;-0.000} sn ({pay / kare:+0;-0} kare)  " +
                          (pay < 0f ? "❌ dash darbeyi KARŞILAMIYOR"
                           : pay > 0.25f ? "⚠️ çok cömert — dash bedava kaçış"
                           : "✅ makul"));
            sb.AppendLine();

            sb.AppendLine($"  düşman hazırlığı    : {t.enemyWindupDuration:0.000} sn  " +
                          $"({t.enemyWindupDuration / kare:0} kare tepki payı)");
            float dashMesafe = t.dashDistance;
            float menzil = t.enemyAttackRange;
            sb.AppendLine($"  dash mesafesi       : {dashMesafe:0.00} · düşman menzili {menzil:0.00}  " +
                          (dashMesafe > menzil ? "✅ dash menzilden ÇIKARIYOR"
                                               : "❌ dash menzilden çıkarmıyor, kaçış yok"));
            sb.AppendLine();

            sb.AppendLine($"  saldırı taahhüdü    : {t.attackDuration:0.000} sn kilitlisin " +
                          $"({t.attackDuration / kare:0} kare)");
            sb.AppendLine($"  isabet anı          : {t.attackHitMoment:0.000} sn " +
                          $"(%{t.attackHitMoment / t.attackDuration * 100f:0} noktasında)");
            sb.AppendLine($"  dash bekleme        : {t.dashCooldown:0.00} sn — " +
                          $"düşman saldırı aralığı ~{t.enemyAttackCooldown:0.00} sn  " +
                          (t.dashCooldown < t.enemyAttackCooldown ? "✅ her saldırıdan kaçılabilir"
                                                                  : "⚠️ her saldırıdan kaçılamaz"));
            return sb.ToString();
        }

        static void KillAll()
        {
            foreach (var e in Enemy.All.ToArray())
                if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
        }

        static void GoToRoom(RoomManager rm, int room) =>
            typeof(RoomManager).GetMethod("EnterRoom",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(rm, new object[] { room });

        static void GoToRegion(RoomManager rm, int region) =>
            typeof(RoomManager).GetMethod("EnterRegion",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(rm, new object[] { region, 1 });
    }
}
