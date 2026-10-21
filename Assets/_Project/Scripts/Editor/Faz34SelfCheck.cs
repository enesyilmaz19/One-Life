using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 34 — 1. bölgenin içeriği (Enes 2026-10-20: "sıradaki faza geç"). TAHSİLDAR'ın 5 adamı (özgün mini boss:
    /// ad, imza, ana boss'tan öğrettiği saldırılar), 2 yeni düşman türü (KALKANLI, MÜFLİS), 2 yeni diziliş
    /// (KISKAÇ, ÇİFT SIRA). Her imza ETKİ olarak ölçülüyor: saat indi mi, havuz doldu mu, kalkan tuttu mu.
    /// </summary>
    public static class Faz34SelfCheck
    {
        const BindingFlags Gizli = BindingFlags.NonPublic | BindingFlags.Instance;
        static readonly PropertyInfo Hız = typeof(Enemy).GetProperty("MoveSpeed", Gizli);
        static readonly MethodInfo Adım = typeof(Enemy).GetMethod("TickState", Gizli);

        [MenuItem("Tek Can/Faz 34 öz-denetimi çalıştır (1. bölge içeriği)")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;
            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 300f;
            t.enemyDecayPerSecond = 0f;
            t.harvestSoftCap = 1e6f;          // doyma bu kapının konusu değil
            Region.Reset();

            failed += Adamlar(t, ref total);
            failed += İmzalar(t, ref total);
            failed += Kalkanlı(t, ref total);
            failed += Müflis(t, ref total);
            failed += Açılış(t, ref total);
            failed += Dizilişler(t, ref total);

            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset(); RoomManager.Reset(); RevivalSystem.Reset();
            Debug.Log($"[Faz 34 öz-denetim] {(failed == 0 ? "✅" : "❌")} {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── 1) TAHSİLDAR'ın 5 adamı: ad, ana boss'un saldırıları, tek faz, dersler farklı ──
        static int Adamlar(Tuning t, ref int total)
        {
            int failed = 0, S = Mathf.Max(1, t.subRegionsPerRegion);
            var ana = BossKind.For(0);
            var anaAdlar = ana.Attacks(1).Concat(ana.Attacks(2)).Select(a => a.Name).ToList();
            var adlar = new System.Collections.Generic.List<string>();
            bool tamam = S == MiniBossKind.Adamlar[0].Length;
            for (int s = 0; s < S; s++)
            {
                var m = MiniBossKind.For(0, s, S);
                var tanım = MiniBossKind.Adamlar[0][Mathf.Min(s, MiniBossKind.Adamlar[0].Length - 1)];
                var saldırı = m.Attacks(1).Select(a => a.Name).ToArray();
                adlar.Add($"{m.Name} ({string.Join(" · ", saldırı)})");
                if (m.Özgün != tanım || m.Name != tanım.Ad || m.PhaseCount != 1 || string.IsNullOrEmpty(m.Crime)
                    || saldırı.Length != tanım.Saldırı.Length || saldırı.Any(a => !anaAdlar.Contains(a))) tamam = false;
            }
            failed += Check(ref total, $"⚠️⚠️ 1. bölgenin 5 mini boss'u TAHSİLDAR'ın adamları, saldırıları TAHSİLDAR'dan, tek faz: {string.Join(" · ", adlar)}",
                            tamam, adlar.Count);
            var son = MiniBossKind.For(0, S - 1, S);
            failed += Check(ref total, $"son adam ({son.Name}) TAHSİLDAR'ın harita saldırısını öğretiyor",
                            son.Attacks(1).Any(a => a.Tür == SaldırıTürü.Harita), 0);
            // Üç sıra (Faz 26) mesafeye göre seçiyor: yakını olmayan dibindekine, uzağı olmayan uzaktakine vuramaz
            var eksik = Enumerable.Range(0, S).Select(s => MiniBossKind.For(0, s, S))
                        .Where(m => !m.Attacks(1).Any(a => a.Tür == SaldırıTürü.Yakın) || !m.Attacks(1).Any(a => a.Tür == SaldırıTürü.Uzak))
                        .Select(m => m.Name).ToList();
            failed += Check(ref total, $"⚠️ her adamın en az bir YAKIN ve bir UZAK saldırısı var{(eksik.Count > 0 ? " — eksik: " + string.Join(", ", eksik) : "")}",
                            eksik.Count == 0, eksik.Count);
            var geçici = MiniBossKind.For(2, 0, S);                             // Faz 35'ten beri 2. bölge de özgün
            failed += Check(ref total, $"öbür bölgeler geçici dilimde kalıyor ({geçici.Name})", geçici.Özgün == null && geçici.Name.Contains("·"), 0);
            return failed;
        }

        // ── 2) İmzalar — ETKİ ──
        static int İmzalar(Tuning t, ref int total)
        {
            int failed = 0, S = Mathf.Max(1, t.subRegionsPerRegion);
            BossKind M(int s) => MiniBossKind.For(0, s, S);

            // HACİZCİ: yanındayken emer — TAHSİLDAR faz 1 aurasının ×1.6'sı; ailenin geri kalanı ×1.2 (geçici dilim yarısıydı)
            var (r1, c1, p1, haciz) = Kur(t, M(0));
            float s0 = c1.Seconds; haciz.ApplyAura(1f);
            float emdi = s0 - c1.Seconds, tam = BossKind.For(0).AuraDrain(t, 1);
            failed += Check(ref total, $"⚠️ HACİZCİ yanındayken emer: 1 sn'de {emdi:0.00} sn = TAHSİLDAR faz 1 aurası {tam:0.00} × 1.6 · ailenin en çok emeni",
                            emdi > 0f && Mathf.Abs(emdi - tam * 1.6f) < 0.001f && Enumerable.Range(1, S - 1).All(i => M(i).AuraDrain(t, 1) < emdi), emdi);
            Temizle(r1);

            // SENETÇİ: İNEN isabeti havuzunu %4 doldurur; ıskalayınca dolmaz
            var (r2, c2, p2, senet) = Kur(t, M(1));
            float başla = senet.TimeLeft, pay = M(1).ZorlaAlır;
            Havuz(senet, başla * 0.5f);
            float isabet = Vur(senet, p2, "HACİZ", yakın: true);
            float dolan = senet.TimeLeft - başla * 0.5f;
            Havuz(senet, başla * 0.5f);
            Vur(senet, p2, "HACİZ", yakın: false);
            float ıska = senet.TimeLeft - başla * 0.5f;
            // Dash'in yuttuğu isabet de sayılmaz — koni tutuyor ama saniye inmiyor
            Havuz(senet, başla * 0.5f);
            typeof(PlayerController).GetField("_iFrames", Gizli).SetValue(p2.GetComponent<PlayerController>(), 1f);
            float yutulan = Vur(senet, p2, "HACİZ", yakın: true);
            float dashta = senet.TimeLeft - başla * 0.5f;
            failed += Check(ref total, $"⚠️⚠️ SENETÇİ zorla alır: isabet (−{isabet:0.0} sn) havuzunu +{dolan:0.0} doldurdu (= %{pay * 100f:0} × {başla:0}), ıskada +{ıska:0.0}, dash'in yuttuğunda (−{yutulan:0.0}) +{dashta:0.0}",
                            pay > 0f && isabet > 0f && Mathf.Abs(dolan - başla * pay) < 0.01f && Mathf.Abs(ıska) < 0.001f
                            && Mathf.Abs(yutulan) < 0.001f && Mathf.Abs(dashta) < 0.001f, dolan);
            Temizle(r2);

            // KABADAYI: havuz yarıya inince hız ×1.4 — tamken değil
            var (r3, c3, p3, kaba) = Kur(t, M(2));
            float tamHız = (float)Hız.GetValue(kaba);
            Havuz(kaba, kaba.TimeLeft * 0.45f);
            float öfkeHız = (float)Hız.GetValue(kaba);
            failed += Check(ref total, $"⚠️ KABADAYI yarıda öfkelenir: hız {tamHız:0.00} → {öfkeHız:0.00} (×{öfkeHız / tamHız:0.00}, öfkeli {kaba.Öfkeli})",
                            kaba.Öfkeli && Mathf.Abs(öfkeHız / tamHız - 1.4f) < 0.001f, öfkeHız / Mathf.Max(0.001f, tamHız));
            Temizle(r3);

            // KOLCU: isabeti kelepçeler — yürüyüş ×0.6; HACİZCİ'nin isabeti kelepçelemez
            var (r4, c4, p4, kolcu) = Kur(t, M(3));
            var pc4 = p4.GetComponent<PlayerController>();
            float önce = pc4.YürüyüşÇarpanı;
            Vur(kolcu, p4, "EL KOYMA", yakın: true);
            float sonra = pc4.YürüyüşÇarpanı;
            Temizle(r4);
            var (r5, c5, p5, haciz2) = Kur(t, M(0));
            Vur(haciz2, p5, "HACİZ", yakın: true);
            bool başkasıKelepçeledi = p5.GetComponent<PlayerController>().Kelepçeli;
            Temizle(r5);
            failed += Check(ref total, $"⚠️ KOLCU kelepçeler: isabetten sonra yürüyüş ×{önce:0.00} → ×{sonra:0.00}; HACİZCİ'ninki kelepçelemez ({(başkasıKelepçeledi ? "KELEPÇELEDİ" : "evet")})",
                            Mathf.Abs(önce - 1f) < 0.001f && Mathf.Abs(sonra - 0.6f) < 0.001f && !başkasıKelepçeledi, sonra);

            // Enes (2026-10-21): "süresi yakın ama rastgele olsun … ama zorlasınlar yine de" — dibinde bedava durulan adam yok:
            // bot KABADAYI'nın 46 konisinin 46'sından, KOLCU'nunkilerin hepsinden kaçtı, ikisi bota 2–6 sn'ye mal oldu
            var bedava = Enumerable.Range(0, S).Select(M).Where(m => m.AuraDrain(t, 1) <= 0f && m.ZorlaAlır <= 0f).Select(m => m.Name).ToList();
            failed += Check(ref total, $"⚠️ dibinde bedava durulan adam yok: her biri emiyor ya da isabeti havuzunu dolduruyor{(bedava.Count > 0 ? " — bedava: " + string.Join(", ", bedava) : "")}",
                            bedava.Count == 0, bedava.Count);

            // SAĞ KOL: yarım aura + yarıda öfke ×1.25 — patronun provası
            var sağ = M(S - 1);
            failed += Check(ref total, $"SAĞ KOL ikisinden biraz: aura {sağ.AuraDrain(t, 1):0.00} (ailenin ×1.2'si) · öfke ×{sağ.ÖfkeHızı:0.00}",
                            Mathf.Abs(sağ.AuraDrain(t, 1) - BossKind.For(0).AuraDrain(t, 1) * 1.2f) < 0.001f && Mathf.Abs(sağ.ÖfkeHızı - 1.25f) < 0.001f, sağ.ÖfkeHızı);
            return failed;
        }

        // ── 3) KALKANLI: önden ×0.2 (hasat da), arkadan tam; yavaş döner, hazırlık başında dönmez ──
        static int Kalkanlı(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, clock, player, _) = Kur(t, null);
            var e = Düşman(t, root, player, EnemyKind.Shield, Bootstrap.ArenaCenter + Vector3.forward * 2f);
            Bak(e, player.position);
            float Hasar(out float hasat)
            {
                float h0 = e.TimeLeft, s0 = clock.TotalHarvested;
                e.TakeDamage(1f);
                hasat = clock.TotalHarvested - s0;
                return h0 - e.TimeLeft;
            }
            float ön = Hasar(out float önHasat);
            Bak(e, e.transform.position + (e.transform.position - player.position));     // sırtını dön
            float arka = Hasar(out float arkaHasat);
            failed += Check(ref total, $"⚠️⚠️ KALKANLI önden {ön:0.00} (hasat {önHasat:0.000}), arkadan {arka:0.00} (hasat {arkaHasat:0.000}) — kalkan ×0.2",
                            arka > 0f && Mathf.Abs(ön / arka - 0.2f) < 0.001f && Mathf.Abs(önHasat / Mathf.Max(1e-6f, arkaHasat) - 0.2f) < 0.01f, ön / Mathf.Max(0.001f, arka));

            // Yavaş dönüş: sırtı sana dönükken 0.1 sn'de en çok 10°; 2.5 sn'de sana döner
            typeof(Enemy).GetField("_attackTimer", Gizli).SetValue(e, 99f);
            float açı0 = Açı(e, player.position);
            Adım.Invoke(e, new object[] { 0.1f });
            float açı1 = Açı(e, player.position);
            for (int i = 0; i < 25; i++) Adım.Invoke(e, new object[] { 0.1f });
            float açı2 = Açı(e, player.position);
            failed += Check(ref total, $"⚠️ KALKANLI yavaş döner: arkasındayken 0.1 sn'de {açı0 - açı1:0.0}° (≤ 10°), 2.6 sn sonra sana {açı2:0.0}°",
                            açı0 > 170f && açı0 - açı1 > 0.5f && açı0 - açı1 <= 10.01f && açı2 < 5f, açı0 - açı1);

            // Hazırlık BAŞINDA dönmüyor (saldırı yönü bakıştan kilitleniyor) — TEMEL anında dönüyor
            Bak(e, e.transform.position + (e.transform.position - player.position));
            typeof(Enemy).GetMethod("EnterWindup", Gizli).Invoke(e, null);
            float hazırlıkta = Açı(e, player.position);
            var temel = Düşman(t, root, player, EnemyKind.Basic, Bootstrap.ArenaCenter + Vector3.left * 2f);
            Bak(temel, temel.transform.position + (temel.transform.position - player.position));
            typeof(Enemy).GetMethod("EnterWindup", Gizli).Invoke(temel, null);
            failed += Check(ref total, $"KALKANLI hazırlık başında sana dönmüyor ({hazırlıkta:0}° — arkası güvenli), TEMEL dönüyor ({Açı(temel, player.position):0}°)",
                            hazırlıkta > 170f && Açı(temel, player.position) < 1f, hazırlıkta);
            Temizle(root);
            return failed;
        }

        // ── 4) MÜFLİS: öldüğü yerde 1 sn sonra patlar — oyuncuya darbe, içindekilere hasar ──
        static int Müflis(Tuning t, ref int total)
        {
            int failed = 0;
            (float darbe, float yanınaHasar) Öldür(float oyuncuUzaklığı)
            {
                var (root, clock, player, _) = Kur(t, null);
                var m = Düşman(t, root, player, EnemyKind.Bankrupt, Bootstrap.ArenaCenter);
                var yan = Düşman(t, root, player, EnemyKind.Basic, Bootstrap.ArenaCenter + Vector3.right * 1.2f);
                player.position = Bootstrap.ArenaCenter + Vector3.back * oyuncuUzaklığı;
                float havuz = m.TimeLeft, y0 = yan.TimeLeft;
                m.TakeDamage(m.TimeLeft); m.TakeDamage(1f);                          // boşalt, sonra öldür
                var işaret = Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Exclude).FirstOrDefault(g => g.DüşmanHasarı > 0f);
                float d0 = clock.TotalHitCost;                                       // saat değil: yanındakinin hasadı saati artırıyor
                if (işaret != null) işaret.Step(1.1f);
                var sonuç = (clock.TotalHitCost - d0, y0 - yan.TimeLeft);
                bool beklenen = işaret != null && Mathf.Abs(işaret.DüşmanHasarı - havuz * 0.6f) < 0.001f;
                Temizle(root);
                return beklenen ? sonuç : (-1f, -1f);
            }
            var dibinde = Öldür(1f);
            var uzakta = Öldür(5f);
            failed += Check(ref total, $"⚠️⚠️ MÜFLİS borcu patlar: dibindeysen −{dibinde.darbe:0.0} sn, yanındaki düşmana −{dibinde.yanınaHasar:0.0}; 5 birim ötedeysen −{uzakta.darbe:0.0} sn (düşman yine −{uzakta.yanınaHasar:0.0})",
                            dibinde.darbe > 0f && dibinde.yanınaHasar > 0f && Mathf.Abs(uzakta.darbe) < 0.001f && uzakta.yanınaHasar > 0f, dibinde.darbe);
            return failed;
        }

        // ── 5) Açılış: KALKANLI ve MÜFLİS 1. bölgede doğuyor; tavanlar 2 arttı ──
        static int Açılış(Tuning t, ref int total)
        {
            int failed = 0;
            var (root, clock, player, _) = Kur(t, null);
            var rm = root.AddComponent<RoomManager>();
            rm.Init(t, player, null);
            var pick = typeof(RoomManager).GetMethod("PickKind", Gizli);
            typeof(RoomManager).GetMethod("EnterRegion", Gizli).Invoke(rm, new object[] { 0, 1 });
            var çıkan = new System.Collections.Generic.HashSet<EnemyKind>();
            int ilkKalkan = -1, ilkMüflis = -1;
            for (int oda = 1; oda <= t.RoomsInRegion; oda++)
                for (int i = 0; i < 12; i++)
                {
                    var k = (EnemyKind)pick.Invoke(rm, new object[] { oda, i });
                    çıkan.Add(k);
                    if (k == EnemyKind.Shield && ilkKalkan < 0) ilkKalkan = oda;
                    if (k == EnemyKind.Bankrupt && ilkMüflis < 0) ilkMüflis = oda;
                }
            Temizle(root);
            failed += Check(ref total, $"⚠️ 1. bölgede {çıkan.Count} tür doğuyor ({string.Join(", ", çıkan.Select(k => k.Name))}) — KALKANLI {ilkKalkan}. odada, MÜFLİS {ilkMüflis}. odada; tavanlar {string.Join("/", Region.All.Select(r => r.UnlockedKinds))}",
                            çıkan.Count == 4 && ilkKalkan > 0 && ilkMüflis > 0 && Region.At(0).UnlockedKinds == 4
                            && EnemyKind.All[2] == EnemyKind.Shield && EnemyKind.All[3] == EnemyKind.Bankrupt, çıkan.Count);
            return failed;
        }

        // ── 6) Dizilişler: KISKAÇ ağzı girişe açık · ÇİFT SIRA iki hat, arkası kayık; 1. bölgede geliyorlar ──
        static int Dizilişler(Tuning t, ref int total)
        {
            int failed = 0, n = 8;
            var k = Enumerable.Range(0, n).Select(i => RoomLayout.Pincer.Position(t, i, n)).ToArray();
            // kol ucu (ilk sıra) dışta ve girişe yakın, dip (son sıra) içte ve çıkışa doğru
            bool ağız = Mathf.Abs(k[0].x) > Mathf.Abs(k[n - 2].x) + 1f && k[0].z < k[n - 2].z - 3f && Mathf.Sign(k[0].x) != Mathf.Sign(k[1].x);
            var ç = Enumerable.Range(0, n).Select(i => RoomLayout.DoubleRow.Position(t, i, n)).ToArray();
            var önSıra = ç.Take(n / 2).ToArray(); var arkaSıra = ç.Skip(n / 2).ToArray();
            float enYakınX = arkaSıra.Min(a => önSıra.Min(b => Mathf.Abs(a.x - b.x)));
            bool iki = önSıra.All(p => Mathf.Abs(p.z - önSıra[0].z) < 0.01f) && arkaSıra.All(p => Mathf.Abs(p.z - arkaSıra[0].z) < 0.01f)
                    && arkaSıra[0].z - önSıra[0].z > 4f && enYakınX > 0.5f;
            var bölge1 = Enumerable.Range(1, t.RoomsInRegion).Where(o => RoomManager.Tür(t, o) == RoomManager.OdaTürü.Dövüş)
                                   .Select(o => RoomManager.Diziliş(t, 0, o)).ToList();             // yalnız dövüş odaları
            failed += Check(ref total, $"⚠️ KISKAÇ ağzı girişe açık ({(ağız ? "evet" : "HAYIR")}) · ÇİFT SIRA iki hat, arka hat boşluklara kayık (en yakın x farkı {enYakınX:0.0}) · 1. bölgede ikisi de geliyor",
                            ağız && iki && bölge1.Contains(RoomLayout.Pincer) && bölge1.Contains(RoomLayout.DoubleRow), enYakınX);
            return failed;
        }

        // ── kurgu ──────────────────────────────────────────────────────────────
        static (GameObject, Clock, Transform, Enemy) Kur(Tuning t, BossKind kind)
        {
            Enemy.All.Clear();
            RevivalSystem.Reset();
            var root = new GameObject("faz34-check");
            var player = new GameObject("player"); player.transform.SetParent(root.transform);
            player.transform.position = Bootstrap.ArenaCenter;
            player.AddComponent<PlayerController>().Init(t, null);
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, t.startSeconds);
            clock.Frozen = false;
            Enemy boss = null;
            if (kind != null)
                boss = Bootstrap.SpawnEnemyAt(t, Bootstrap.ArenaCenter + Vector3.forward * 4f, player.transform,
                                              null, 1f, boss: true, parent: root.transform, bossKind: kind);
            return (root, clock, player.transform, boss);
        }

        static Enemy Düşman(Tuning t, GameObject root, Transform player, EnemyKind kind, Vector3 yer)
            => Bootstrap.SpawnEnemyAt(t, yer, player, null, 1f, boss: false, kind: kind, parent: root.transform);

        /// <summary>Boss'a bu saldırıyı yaptır: oyuncu yakınsa dibinde, değilse menzil dışında. Dönen: saatten inen.</summary>
        static float Vur(Enemy boss, Transform player, string saldırı, bool yakın)
        {
            var a = boss.Boss.Attacks(1).First(x => x.Name == saldırı);
            player.position = boss.transform.position + Vector3.back * (yakın ? 0.5f : 30f);
            typeof(Enemy).GetField("_chained", Gizli).SetValue(boss, a);
            typeof(Enemy).GetMethod("EnterWindup", Gizli).Invoke(boss, null);
            float s0 = Clock.Instance.Seconds;
            typeof(Enemy).GetMethod("EnterStrike", Gizli).Invoke(boss, null);
            return s0 - Clock.Instance.Seconds;
        }

        static void Havuz(Enemy e, float v) => typeof(Enemy).GetProperty("TimeLeft").GetSetMethod(true).Invoke(e, new object[] { v });
        static void Bak(Enemy e, Vector3 nokta) { var d = nokta - e.transform.position; d.y = 0f; e.transform.rotation = Quaternion.LookRotation(d.normalized); }
        static float Açı(Enemy e, Vector3 nokta) { var d = nokta - e.transform.position; d.y = 0f; var f = e.transform.forward; f.y = 0f; return Vector3.Angle(f, d); }

        static void Temizle(GameObject root)
        {
            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include)) Object.DestroyImmediate(a.gameObject);
            foreach (var m in Object.FindObjectsByType<Mote>(FindObjectsInactive.Include)) Object.DestroyImmediate(m.gameObject);
            foreach (var g in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include)) Object.DestroyImmediate(g.gameObject);
            if (root != null) Object.DestroyImmediate(root);
            Clock.Reset(); RevivalSystem.Reset(); RoomManager.Reset();
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
