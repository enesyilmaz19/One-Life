using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 27 — KARTOPU: sunak fiyatı koşu boyunca sürüyor, getiri azalıyor.
    ///
    /// Ölçüm (tam koşu botu, 2026-10-12): iyi oyuncu 43 alımla hasarı ×5.8 yapıp bütün oyunu
    /// 9 dk'da bitirdi, boss'lar 20 sn'de eridi. Sebep iki: fiyat her bölgenin sunağında
    /// SIFIRLANIYORDU ve her alım aynı miktarı veriyordu. Bu kapı ikisini de ETKİDEN ölçüyor:
    /// gerçek `Altar`'dan alıp fiyatı başka bir sunakta okuyor, gerçek kayıt yolundan geri yüklüyor.
    /// </summary>
    public static class Faz27SelfCheck
    {
        const BindingFlags Özel = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tek Can/Faz 27 öz-denetimi çalıştır (kartopu)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Faz 27] Play modunda çalıştırma — test kaydı oyunun kaydına karışır.");
                return;
            }
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;
            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 2000f;                         // alım bedeli testi bitirmesin
            SaveFile.UseInMemory(new SaveData());

            var root = new GameObject("faz27-check");
            var player = new GameObject("player"); player.transform.SetParent(root.transform);
            player.AddComponent<PlayerController>().Init(t, null);
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, t.startSeconds);

            Altar Sunak(Altar.Kind k)
            {
                var go = new GameObject($"sunak-{k}"); go.transform.SetParent(root.transform);
                var a = go.AddComponent<Altar>();
                a.Init(t, player.transform, k);
                return a;
            }
            void Al(Altar a) => typeof(Altar).GetMethod("Buy", Özel).Invoke(a, null);

            // ── 1) KALICI FİYAT: başka bir sunak (sonraki bölge) sıfırdan başlamıyor ──
            var hasar1 = Sunak(Altar.Kind.Damage);
            float ilk = hasar1.Price;
            Al(hasar1); Al(hasar1);
            var hasar2 = Sunak(Altar.Kind.Damage);           // "sonraki bölgenin sunağı"
            var hasat = Sunak(Altar.Kind.Harvest);
            float beklenen = t.altarBasePrice * Mathf.Pow(t.altarPriceGrowth, 2);
            failed += Check(ref total, $"⚠️⚠️ KALICI FİYAT: 2 hasar alımından sonra YENİ hasar sunağı {hasar2.Price:0.0} sn (ilk {ilk:0.0}, sıfırlanmıyor)",
                            Mathf.Abs(hasar2.Price - beklenen) < 0.01f && hasar2.Price > ilk + 0.01f, hasar2.Price);
            failed += Check(ref total, $"fiyat TÜRE göre: hasat sunağı hâlâ ilk fiyatta ({hasat.Price:0.0})",
                            Mathf.Abs(hasat.Price - t.altarBasePrice) < 0.01f, hasat.Price);

            // ── 2) AZALAN GETİRİ: her alım öncekinden az, toplam sınırlı ──
            var artışlar = new System.Collections.Generic.List<float>();
            for (int i = 0; i < 12; i++)
            {
                float önce = clock.AttackDamage;
                clock.Yükselt(Altar.Kind.Damage);
                artışlar.Add(clock.AttackDamage - önce);
            }
            bool azalıyor = true;
            for (int i = 1; i < artışlar.Count; i++) if (artışlar[i] >= artışlar[i - 1] - 1e-5f) azalıyor = false;
            float tavan = t.attackDamage + t.altarDamageStep / (1f - t.altarStepDecay);
            failed += Check(ref total, $"⚠️⚠️ AZALAN GETİRİ: her hasar alımı bir öncekinden az ({artışlar[2]:0.00} → {artışlar[3]:0.00} → … {artışlar[artışlar.Count - 1]:0.00})",
                            azalıyor && artışlar[0] > 0f, artışlar[artışlar.Count - 1]);
            failed += Check(ref total, $"⚠️ HASAR TAVANI: 14 alımda {clock.AttackDamage:0.0} ≤ {tavan:0.0} (taban {t.attackDamage} × {tavan / t.attackDamage:0.0})",
                            clock.AttackDamage <= tavan + 1e-3f, clock.AttackDamage);

            // ── 3) SUNAK DOĞRUYU GÖSTERİYOR: yazan getiri, sıradaki alımınki ──
            string etiket = hasar2.Label();
            string sıradaki = clock.SıradakiGetiri(Altar.Kind.Damage).ToString("0.00");
            failed += Check(ref total, $"sunak SIRADAKİ getiriyi yazıyor ('{etiket}' ∋ +{sıradaki})",
                            etiket.Contains("+" + sıradaki), 0);

            // ── 4) KAYIT: checkpoint ve oda başı kaydı sayımı taşıyor ──
            var rmGo = new GameObject("rooms"); rmGo.transform.SetParent(root.transform);
            var rm = rmGo.AddComponent<RoomManager>();
            rm.Init(t, player.transform, null);
            var s = SaveFile.Current;
            s.checkpointRoom = -1;                            // "aynı oda" koruması devreye girmesin
            typeof(RoomManager).GetMethod("SaveCheckpoint", Özel).Invoke(rm, null);
            rm.SaveRoomStart();
            int[] önce2 = clock.AlınanKopya;
            var yeni = root.AddComponent<Clock>();
            yeni.Init(t, player.transform, 100f);
            yeni.RestoreUpgrades(s.harvestRate, s.attackDamage, s.dashCooldown, s.boughtCounts);
            var odaBaşı = root.AddComponent<Clock>();
            odaBaşı.Init(t, player.transform, 100f);
            odaBaşı.RestoreUpgrades(s.resumeHarvest, s.resumeDamage, s.resumeDash, s.resumeBoughtCounts);
            failed += Check(ref total, $"⚠️ ölüp checkpoint'e dönünce sayım korunuyor (hasar {yeni.Alınan(Altar.Kind.Damage)} = {önce2[1]}, fiyat {yeni.SıradakiFiyat(Altar.Kind.Damage):0} sn)",
                            yeni.Alınan(Altar.Kind.Damage) == önce2[1] && önce2[1] > 0, yeni.Alınan(Altar.Kind.Damage));
            failed += Check(ref total, $"kapat-aç (oda başı) da sayımı koruyor ({odaBaşı.Alınan(Altar.Kind.Damage)})",
                            odaBaşı.Alınan(Altar.Kind.Damage) == önce2[1], odaBaşı.Alınan(Altar.Kind.Damage));

            // ── 5) F7 TAM GÜÇ de aynı kuraldan geçiyor (azalan getiri) ──
            var güç = root.AddComponent<Clock>();
            güç.Init(t, player.transform, 100f);
            typeof(RoomManager).GetMethod("FullPower", Özel).Invoke(rm, null);
            int n = t.altarMaxPurchases * rm.RegionCount;
            float f7 = t.attackDamage + t.altarDamageStep * (1f - Mathf.Pow(t.altarStepDecay, n)) / (1f - t.altarStepDecay);
            failed += Check(ref total, $"F7 TAM GÜÇ azalan getiriyle ({Clock.Instance.AttackDamage:0.00} = {f7:0.00})",
                            Mathf.Abs(Clock.Instance.AttackDamage - f7) < 0.01f, Clock.Instance.AttackDamage);

            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include)) Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(t);
            Clock.Reset(); Altar.ResetAll(); RoomManager.Reset(); Region.Reset(); RevivalSystem.Reset();

            if (failed == 0) Debug.Log($"[Faz 27 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 27 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
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
