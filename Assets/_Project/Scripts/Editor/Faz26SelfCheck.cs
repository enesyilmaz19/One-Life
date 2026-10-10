using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 26 — ÜÇ SIRA (yakın / uzak / tüm harita), ISRAR, fazlar arası taşınma.
    ///
    /// Enes: *"son boss 2 saldırı atıyor, biz ona bayağı saldırı yazdık."* Faz 25'in
    /// dersi (ders 15) burada da geçerli: "listede var" ölçü değil. Akış kontrolleri
    /// `TickState`'i adım adım koşturuyor; harita saldırılarının CEVABI, uyarıyı
    /// gördükten 0.25 sn sonra en yakın güvenli yere koşan bir insanla sınanıyor.
    /// </summary>
    public static class Faz26SelfCheck
    {
        const BindingFlags Özel = BindingFlags.NonPublic | BindingFlags.Instance;
        const float Dt = 0.02f;
        const float İnsanTepkisi = 0.25f;

        [MenuItem("Tek Can/Faz 26 öz-denetimi çalıştır (üç sıra)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Faz 26] Play modunda çalıştırma — test kaydı oyunun kaydına karışır.");
                return;
            }
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;
            SaveFile.PathOverride = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tekcan_faz26_check.json");

            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 500f;
            t.enemyTimePool = 10f;
            t.enemyDecayPerSecond = 0f;
            t.testDefianceCount = 3; t.testLastStandCount = 0; t.testRecallCount = 0;
            Region.Reset();
            SaveFile.NewRun(t);

            var asset = Resources.Load<Tuning>("Tuning");
            var gerçek = asset != null ? Object.Instantiate(asset) : Object.Instantiate(t);
            gerçek.startSeconds = 500f;

            failed += VeriKontrolleri(t, ref total);
            failed += AkışKontrolü(t, ref total);
            failed += CevapKontrolü(gerçek, ref total);
            failed += IsrarKontrolleri(t, ref total);
            failed += UykuEtiketi(t, ref total);

            try { if (System.IO.File.Exists(SaveFile.PathOverride)) System.IO.File.Delete(SaveFile.PathOverride); }
            catch { /* geçici */ }
            SaveFile.PathOverride = null;
            Object.DestroyImmediate(t);
            Object.DestroyImmediate(gerçek);
            Clock.Reset(); Region.Reset(); Enemy.All.Clear(); RevivalSystem.Reset(); Altar.ResetAll(); RoomManager.Reset();

            if (failed == 0) Debug.Log($"[Faz 26 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 26 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── 1) VERİ: sayılar, tür–şekil uyumu, fazlar arası taşınma ──────────────

        static int VeriKontrolleri(Tuning t, ref int total)
        {
            int failed = 0;

            // Temel menzil (boss gövdesi dahil) — üretimden okunuyor, uydurulmuyor.
            var (r0, _, b0) = Kur(t, BossKind.For(0));
            Sıfırla(b0);
            float menzil = b0.CurrentStrikeRadius;
            Temizle(r0);
            float haritaEşiği = 0.8f * t.arenaRadius;

            int ince = 0, uyumsuz = 0, taşınmayan = 0;
            var tablo = new System.Text.StringBuilder("\n    boss · faz → yakın/uzak/harita");
            foreach (var b in BossKind.All)
                for (int f = 1; f <= b.PhaseCount; f++)
                {
                    int y = 0, u = 0, h = 0;
                    foreach (var a in b.Attacks(f))
                    {
                        float erişim = menzil * a.RangeMul + a.Lunge;
                        bool ışın = a.Angle > 0f && a.Angle <= 50f;
                        bool uzakŞekli = a.Lunge > 0f || a.MinPlayerRange > 0f || ışın;
                        bool ok = a.Tür switch
                        {
                            SaldırıTürü.Uzak => uzakŞekli,
                            SaldırıTürü.Harita => erişim >= haritaEşiği,
                            _ => !uzakŞekli && erişim < haritaEşiği,
                        };
                        if (!ok) { uyumsuz++; Debug.LogError($"    {b.Name} f{f}: {a.Name} türü {a.Tür} ama şekli uymuyor (erişim {erişim:0.0})"); }
                        if (a.Tür == SaldırıTürü.Yakın) y++; else if (a.Tür == SaldırıTürü.Uzak) u++; else h++;
                    }
                    tablo.Append($"\n    {b.Name} f{f}: {y}/{u}/{h}");
                    if (y < 2 || u < 2 || h < 1) { ince++; Debug.LogError($"    {b.Name} f{f}: {y}/{u}/{h} — sıralardan biri ince"); }

                    if (f >= b.PhaseCount) continue;
                    // Taşınma: bu fazın her saldırısının sonraki fazda devamı var mı
                    // ("+" eklenmiş hâli ya da `Önceki` ile bağlanmış yeni adı).
                    var sonraki = b.Attacks(f + 1);
                    foreach (var a in b.Attacks(f))
                    {
                        string kök = a.Name.TrimEnd('+');
                        bool var = System.Array.Exists(sonraki, x => x.Name.TrimEnd('+') == kök || x.Önceki == kök);
                        if (!var) { taşınmayan++; Debug.LogError($"    {b.Name}: faz {f}'deki {a.Name} faz {f + 1}'e taşınmıyor"); }
                    }
                }
            Debug.Log("[Faz 26] sıralar:" + tablo);
            failed += Check(ref total, "her fazda yakın ≥ 2, uzak ≥ 2, harita ≥ 1", ince == 0, ince);
            failed += Check(ref total, "⚠️ her saldırının TÜRÜ şekliyle uyuşuyor (ışın 'yakın' olamaz, harita arenayı kaplıyor)",
                            uyumsuz == 0, uyumsuz);
            failed += Check(ref total, "⚠️ her saldırının sonraki fazda DEVAMI var (Enes: 'taşınmıyor')",
                            taşınmayan == 0, taşınmayan);
            return failed;
        }

        // ── 2) AKIŞ: her 4. saldırı harita, mesafeden bağımsız ───────────────────

        static int AkışKontrolü(Tuning t, ref int total)
        {
            int failed = 0, bozuk = 0, denenen = 0;
            int n = t.bossMapAttackEvery;
            var görülmeyen = new List<string>();
            foreach (var b in BossKind.All)
                for (int f = 1; f <= b.PhaseCount; f++)
                {
                    // KAPSAMA (Enes: "yazılmış tüm saldırılar kullanılıyor mu"): dibinde ve
                    // uzakta 3n'er saldırı — her sıranın her saldırısına yer var (en kalabalık
                    // sıra 7, 3n'de 9 yakın/uzak, 3+3 harita).
                    var görülen = new HashSet<string>();
                    foreach (bool uzakta in new[] { false, true })
                    {
                        denenen++;
                        var (r, _, boss) = Kur(t, b);
                        FazıKur(boss, f);
                        var oyuncu = r.transform.Find("player");
                        var sıra = new List<BossAttack>();
                        for (int k = 0; k < 3 * n; k++)
                        {
                            Sıfırla(boss);
                            oyuncu.position = Bootstrap.ArenaCenter;
                            float yarım = boss.CurrentStrikeRadius * 0.5f;
                            boss.transform.position = Bootstrap.ArenaCenter + Vector3.forward * (uzakta ? 16f : yarım);
                            sıra.Add(SaldırıyıBekle(boss, uzakta ? null : (e, o) =>
                            {
                                var yön = Düz(o.position - e.transform.position);
                                o.position = e.transform.position + (yön.sqrMagnitude > 0.0001f ? yön.normalized : Vector3.back) * yarım;
                            }, oyuncu));
                        }
                        // Beklenen: n., 2n., 3n. saldırı HARİTA; gerisi mesafenin sırasından.
                        // ⚠️ "Uzakta" boss 16 birimden geliyor: simit/açıklıkta harita sırası
                        // erişene kadar TUTULUYOR — araya menzilli saldırı girerse kırmızı.
                        var bant = uzakta ? SaldırıTürü.Uzak : SaldırıTürü.Yakın;
                        bool ok = true;
                        for (int k = 0; k < sıra.Count; k++)
                        {
                            var beklenen = (k + 1) % n == 0 ? SaldırıTürü.Harita : bant;
                            if (sıra[k] == null || sıra[k].Tür != beklenen) ok = false;
                            if (sıra[k] != null) görülen.Add(sıra[k].Name);
                        }
                        if (!ok)
                        {
                            bozuk++;
                            var adlar = sıra.ConvertAll(a => a == null ? "—" : $"{a.Name}({a.Tür})");
                            Debug.LogError($"    {b.Name} f{f} {(uzakta ? "UZAKTA" : "DİBİNDE")}: {string.Join(", ", adlar)}");
                        }
                        Temizle(r);
                    }
                    foreach (var a in b.Attacks(f))
                        if (!görülen.Contains(a.Name)) görülmeyen.Add($"{b.Name} f{f} {a.Name}");
                }
            failed += Check(ref total, $"⚠️⚠️ her {n}. saldırı HARİTA, gerisi mesafenin sırası — dibinde de uzakta da ({denenen} deneme)",
                            bozuk == 0, bozuk);
            if (görülmeyen.Count > 0) Debug.LogError("    hiç yapılmayan: " + string.Join(", ", görülmeyen));
            failed += Check(ref total, "⚠️⚠️ KAPSAMA: her fazın YAZILI HER SALDIRISI dövüşte yapılıyor (dibinde + uzakta)",
                            görülmeyen.Count == 0, görülmeyen.Count);
            return failed;
        }

        // ── 3) HARİTA SALDIRILARININ CEVABI VAR ──────────────────────────────────

        static int CevapKontrolü(Tuning t, ref int total)
        {
            // ⚠️ İlk sürüm boss'u hep (0,4)'e, oyuncuyu hep 3/7 birim önüne koyuyordu ve
            // NEMESİS büyümesini hiç denemiyordu — arenadan büyük halka, duvar dibinde 13
            // birimlik kaçış, 17 birimden yetişilmeyen simit yeşil geçti (inceleme
            // 2026-10-10). Şimdi boss {orta, yarı yol, kenar} × oyuncu {arkasında duvar,
            // dibi, 4, 8, 14, karşı duvar, yan duvar} × gövde {1, ×1.6}.
            int failed = 0, denenen = 0, atlanan = 0;
            var vurulan = new List<string>();
            var vurulanSaldırı = new Dictionary<string, int>();     // saldırı → kaç konumda
            var hiç = new List<string>();
            float R = t.arenaRadius;
            var C = Bootstrap.ArenaCenter;
            foreach (var b in BossKind.All)
                for (int f = 1; f <= b.PhaseCount; f++)
                    foreach (var a in b.Attacks(f))
                    {
                        if (a.Tür != SaldırıTürü.Harita) continue;
                        int yapılan = 0;
                        int tohumlar = a.SafeSpotRadius > 0f ? 3 : 1;     // rastgele nokta: birkaç kez
                        foreach (float ölçek in new[] { 1f, 1.6f })
                            foreach (float oran in new[] { 0f, 0.5f, 0.85f })
                            {
                                var bossYeri = C + Vector3.forward * (oran * R);
                                foreach (var (ad, yer) in OyuncuYerleri(bossYeri, R))
                                    for (int tohum = 1; tohum <= tohumlar; tohum++)
                                    {
                                        bool? kaçtı = İnsanKaçar(t, b, f, a, bossYeri, yer, ölçek, tohum);
                                        if (kaçtı == null) { atlanan++; continue; }   // erişmiyor: seçilmezdi
                                        denenen++; yapılan++;
                                        if (kaçtı == false)
                                        {
                                            string anahtar = $"{b.Name}/{a.Name}";
                                            vurulanSaldırı.TryGetValue(anahtar, out int k);
                                            vurulanSaldırı[anahtar] = k + 1;
                                            if (k == 0) vurulan.Add($"{anahtar} (ör. boss {oran:0.00}R, oyuncu {ad}, ×{ölçek}, tohum {tohum})");
                                        }
                                    }
                            }
                        if (yapılan == 0) hiç.Add($"{b.Name}/{a.Name}");
                    }
            int vurulanToplam = 0;
            foreach (var kv in vurulanSaldırı) vurulanToplam += kv.Value;
            foreach (var v in vurulan)
                Debug.LogError($"    cevabı uygulayıp vurulan: {v} — {vurulanSaldırı[v.Substring(0, v.IndexOf(" (ör."))]} konumda");
            if (hiç.Count > 0) Debug.LogError("    hiçbir konumdan erişmeyen: " + string.Join(", ", hiç));
            Debug.Log($"[Faz 26] cevap taraması: {denenen} deneme, {atlanan} konumda saldırı seçilmezdi (erişmiyor)");
            failed += Check(ref total, $"test kurgusu: harita saldırısı denemesi ({denenen} ≥ 500)", denenen >= 500, denenen);
            failed += Check(ref total, "her harita saldırısı en az bir konumdan YAPILIYOR (erişim şartı onu öldürmüyor)",
                            hiç.Count == 0, hiç.Count);
            failed += Check(ref total, $"⚠️⚠️ her HARİTA saldırısının cevabı var — her konumda, NEMESİS boyunda da: uyarıdan 0.25 sn sonra en yakın güvenli yere koşan VURULMUYOR ({denenen - vurulanToplam}/{denenen})",
                            vurulanToplam == 0, vurulanToplam);
            return failed;
        }

        /// <summary>Boss'a göre oyuncu konumları — duvar diplerine sıkışmışları dahil.</summary>
        static IEnumerable<(string, Vector3)> OyuncuYerleri(Vector3 boss, float R)
        {
            var C = Bootstrap.ArenaCenter;
            float sınır = R * 0.95f;
            Vector3 Sığdır(Vector3 p)
            {
                var d = Düz(p - C);
                return d.magnitude > sınır ? C + d.normalized * sınır : new Vector3(p.x, C.y, p.z);
            }
            yield return ("arkasında duvar", C + Vector3.forward * sınır);
            yield return ("dibi", Sığdır(boss + Vector3.back * 1.5f));
            foreach (float d in new[] { 4f, 8f, 14f })
                yield return ($"{d:0} önünde", Sığdır(boss + Vector3.back * d));
            yield return ("karşı duvar", C + Vector3.back * sınır);
            yield return ("yan duvar", C + Vector3.right * sınır);
        }

        /// <summary>
        /// İnsan modeli: her hazırlığın başında uyarıyı görür, 0.25 sn sonra en yakın
        /// GÜVENLİ noktaya koşar (üretimin kendi isabet hesabıyla — `ŞimdiVursaİsabet`).
        /// Koşarak yetişemeyecekse bir kez dash atar. Tekrarlar ve her iniş ayrı sınanır.
        /// null = bu konumdan saldırı seçilmezdi (üretimin `HaritaYapılabilir`'i).
        /// </summary>
        static bool? İnsanKaçar(Tuning t, BossKind b, int f, BossAttack a, Vector3 bossYeri, Vector3 oyuncuYeri, float ölçek, int tohum)
        {
            var (r, c, boss) = Kur(t, b);
            FazıKur(boss, f);
            Random.InitState(tohum);
            boss.transform.localScale *= ölçek;                       // NEMESİS büyümesi
            boss.transform.position = new Vector3(bossYeri.x, boss.transform.position.y, bossYeri.z);
            var oyuncu = r.transform.Find("player");
            oyuncu.position = oyuncuYeri;
            float mesafe = Düz(oyuncu.position - boss.transform.position).magnitude;
            if (!(bool)typeof(Enemy).GetMethod("HaritaYapılabilir", Özel).Invoke(boss, new object[] { a, mesafe }))
            {
                Temizle(r);
                return null;
            }
            Alan(boss, "_chained", a);
            Çağır(boss, "EnterWindup");

            float önce = c.Seconds, geçen = 0f;
            Vector3? hedef = null;
            bool dash = false;
            var önceki = Enemy.State.Windup;
            for (int i = 0; i < 6000; i++)
            {
                var durum = boss.CurrentState;
                if (durum == Enemy.State.Chase) break;
                if (durum == Enemy.State.Windup && önceki != Enemy.State.Windup) { geçen = 0f; hedef = null; dash = false; }
                if (durum == Enemy.State.Windup)
                {
                    geçen += Dt;
                    if (geçen >= İnsanTepkisi)
                    {
                        if (hedef == null) hedef = EnYakınGüvenli(boss, oyuncu.position, t);
                        if (hedef != null)
                        {
                            var yol = Düz(hedef.Value - oyuncu.position);
                            float kalan = boss.KalanHazırlık;
                            float hız = t.moveSpeed;
                            if (!dash && yol.magnitude > t.moveSpeed * Mathf.Max(0f, kalan - 0.05f))
                            { dash = true; oyuncu.position += yol.normalized * Mathf.Min(yol.magnitude, t.dashDistance); }
                            else oyuncu.position = Vector3.MoveTowards(oyuncu.position, hedef.Value, hız * Dt);
                        }
                    }
                }
                önceki = durum;
                Çağır(boss, "TickState", Dt);
            }
            bool kaçtı = c.Seconds >= önce - 0.001f;
            Temizle(r);
            return kaçtı;
        }

        static Vector3? EnYakınGüvenli(Enemy boss, Vector3 ben, Tuning t)
        {
            Vector3? en = null; float d = float.MaxValue;
            float sınır = t.arenaRadius * 0.95f;
            for (float x = -sınır; x <= sınır; x += 0.5f)
                for (float z = -sınır; z <= sınır; z += 0.5f)
                {
                    var p = new Vector3(Bootstrap.ArenaCenter.x + x, ben.y, Bootstrap.ArenaCenter.z + z);
                    if (Düz(p - Bootstrap.ArenaCenter).magnitude > sınır) continue;
                    if (boss.ŞimdiVursaİsabet(p)) continue;
                    // Güvenli yerin KENARINA değil biraz içine: yarım birim pay.
                    bool payVar = true;
                    foreach (var ofs in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                        if (boss.ŞimdiVursaİsabet(p + ofs * 0.5f)) { payVar = false; break; }
                    if (!payVar) continue;
                    float x2 = Düz(p - ben).magnitude;
                    if (x2 < d) { d = x2; en = p; }
                }
            return en;
        }

        // ── 4) ISRAR ──────────────────────────────────────────────────────────

        static int IsrarKontrolleri(Tuning t, ref int total)
        {
            int failed = 0;
            int eskiN = t.bossMapAttackEvery;
            t.bossMapAttackEvery = 0;                       // yalnızca yakın/uzak — ısrar burada sayılıyor

            // a) Akış: dibinde 6 saldırı → 1, 1, 1, 1.25, 1.5, 1.75 · sonra uzakta → 1
            var (r, c, boss) = Kur(t, BossKind.For(0));
            var oyuncu = r.transform.Find("player");
            var çarpanlar = new List<float>();
            for (int k = 0; k < 6; k++)
            {
                Sıfırla(boss);
                float yarım = boss.CurrentStrikeRadius * 0.5f;
                oyuncu.position = boss.transform.position + Vector3.back * yarım;
                SaldırıyıBekle(boss, null, oyuncu);
                çarpanlar.Add(boss.IsrarÇarpanı);
            }
            Sıfırla(boss);
            oyuncu.position = Bootstrap.ArenaCenter;
            boss.transform.position = Bootstrap.ArenaCenter + Vector3.forward * 16f;
            SaldırıyıBekle(boss, null, oyuncu);
            float uzakta = boss.IsrarÇarpanı;
            float Beklenen(int k) => Mathf.Min(t.bossStreakCap, 1f + Mathf.Max(0, k + 1 - t.bossStreakThreshold) * t.bossStreakStep);
            bool sıraOk = true;
            for (int k = 0; k < çarpanlar.Count; k++) if (Mathf.Abs(çarpanlar[k] - Beklenen(k)) > 0.001f) sıraOk = false;
            failed += Check(ref total, $"⚠️ ISRAR: hep dibinde → {string.Join(", ", çarpanlar.ConvertAll(x => x.ToString("0.00")))}",
                            sıraOk, çarpanlar[çarpanlar.Count - 1]);
            failed += Check(ref total, $"⚠️ ISRAR: mesafeni değiştirince SIFIRLANIYOR (uzakta ×{uzakta:0.00})",
                            Mathf.Abs(uzakta - 1f) < 0.001f, uzakta);

            // a2) Faz değişince (ve uykudan uyanınca) ısrar sıfır — eski fazın serisi taşınmıyor.
            Alan(boss, "_ısrar", 1.75f); Alan(boss, "_bantSerisi", 6); Alan(boss, "_sonBant", SaldırıTürü.Yakın);
            FazıKur(boss, 2);
            typeof(Enemy).GetProperty("Dağarcık", Özel).GetValue(boss);
            failed += Check(ref total, $"⚠️ ISRAR faz değişince SIFIR (×{boss.IsrarÇarpanı:0.00}, seri {(int)Alan(boss, "_bantSerisi")})",
                            Mathf.Abs(boss.IsrarÇarpanı - 1f) < 0.001f && (int)Alan(boss, "_bantSerisi") == 0, boss.IsrarÇarpanı);
            FazıKur(boss, 1);

            // b) Bedel: aynı vuruş, ısrar ×1.5 → 1.5 kat pahalı
            var halka = System.Array.Find(BossKind.For(0).Attacks(1), a => a.Angle >= 360f && a.Tür == SaldırıTürü.Yakın);
            float Vuruş(float ısrar)
            {
                Sıfırla(boss);
                oyuncu.position = boss.transform.position + Vector3.back * 0.5f;
                Alan(boss, "_ısrar", ısrar);
                Alan(boss, "_chained", halka);
                Çağır(boss, "EnterWindup");
                float s0 = c.Seconds;
                Çağır(boss, "EnterStrike");
                return s0 - c.Seconds;
            }
            float düz = Vuruş(1f), sert = Vuruş(1.5f);
            failed += Check(ref total, $"⚠️ ISRAR bedele iniyor: ×1.5 → {sert:0.00} = {düz:0.00} × 1.5",
                            düz > 0f && Mathf.Abs(sert - düz * 1.5f) < 0.01f, sert);

            // c) Görünür: uyarı yayı koyulaşıyor (çizilen renk)
            Color Yay(float ısrar)
            {
                Sıfırla(boss);
                Alan(boss, "_ısrar", ısrar);
                Alan(boss, "_chained", halka);
                Çağır(boss, "EnterWindup");
                var arc = Alan(boss, "_telegraph") as Arc;
                return arc != null ? arc.Color : Color.clear;
            }
            Color normal = Yay(1f), ısrarlı = Yay(1.75f);
            failed += Check(ref total, $"⚠️ ISRAR GÖRÜNÜYOR: uyarı yayı koyulaşıyor (a {normal.a:0.00} → {ısrarlı.a:0.00}, g {normal.g:0.00} → {ısrarlı.g:0.00})",
                            ısrarlı.a > normal.a + 0.05f && ısrarlı.g < normal.g - 0.05f, ısrarlı.a);
            Temizle(r);

            // d) HUD: boss odasının başlığında "ISRAR ×"
            {
                var (r2, _, _) = Kur(t, null);
                var rmGo = new GameObject("rooms"); rmGo.transform.SetParent(r2.transform);
                var rm = rmGo.AddComponent<RoomManager>();
                rm.Init(t, r2.transform.Find("player"), null, startRoom: t.roomsPerRegion);
                var b2 = rm.BossInRoom();
                if (b2 != null) Alan(b2, "_ısrar", 1.5f);
                string başlık = rm.Prompt();
                failed += Check(ref total, $"⚠️ ISRAR HUD'da yazıyor ('{başlık}')", başlık.Contains("ISRAR"), 0);

                // İkinci gövde (KEFİL ikiye bölünür): HUD en yüksek ısrarı gösteriyor, ilkini değil.
                var ikiz = Bootstrap.SpawnEnemyAt(t, Bootstrap.ArenaCenter + Vector3.left * 3f, r2.transform.Find("player"),
                                                  null, 1f, boss: true, parent: r2.transform, bossKind: BossKind.For(2));
                Alan(ikiz, "_ısrar", 1.75f);
                string ikizli = rm.Prompt();
                failed += Check(ref total, $"ISRAR HUD'da İKİ gövdenin yüksek olanı ('{ikizli}')",
                                ikizli.Contains("×" + 1.75f.ToString("0.00")), 0);
                Temizle(r2);
                Altar.ResetAll(); RoomManager.Reset();
            }

            t.bossMapAttackEvery = eskiN;
            return failed;
        }

        // ── 5) UYUYAN BOSS'UN ETİKETİ DONMUYOR ──────────────────────────────────

        static int UykuEtiketi(Tuning t, ref int total)
        {
            var son = BossKind.All[BossKind.All.Length - 1];
            var (r, _, boss) = Kur(t, son);
            Çağır(boss, "ÇağırRakipleri");                    // uyku + rakip
            var etiket = boss.GetComponentInChildren<TimeLabel>();
            var yazı = boss.GetComponentInChildren<TextMesh>();
            etiket.Set(6.6f, false);                         // faz geçişinden önceki son değer
            typeof(Enemy).GetMethod("Update", Özel).Invoke(boss, null);
            string beklenen = boss.TimeLeft.ToString("0.0");
            int failed = Check(ref total, $"⚠️ uyuyan boss'un etiketi GÜNCEL ('{yazı.text}' = {beklenen}, 6.6'da donmuyor)",
                               boss.Dormant && yazı != null && yazı.text == beklenen, 0);
            Temizle(r);
            return failed;
        }

        // ── yardımcılar (Faz 25 ile aynı desen) ───────────────────────────────

        static BossAttack SaldırıyıBekle(Enemy e, System.Action<Enemy, Transform> oyuncuyuOynat, Transform oyuncu = null, float sınır = 20f)
        {
            for (float geçen = 0f; geçen < sınır; geçen += Dt)
            {
                oyuncuyuOynat?.Invoke(e, oyuncu);
                Çağır(e, "TickState", Dt);
                if (e.CurrentState == Enemy.State.Windup) return e.CurrentAttack;
            }
            return null;
        }

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

        static (GameObject, Clock, Enemy) Kur(Tuning t, BossKind kind)
        {
            Enemy.All.Clear();
            RevivalSystem.Reset();
            var root = new GameObject("faz26-check");
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
            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include)) Object.DestroyImmediate(a.gameObject);
            foreach (var m in Object.FindObjectsByType<Mote>(FindObjectsInactive.Include)) Object.DestroyImmediate(m.gameObject);
            foreach (var g in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include)) Object.DestroyImmediate(g.gameObject);
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
