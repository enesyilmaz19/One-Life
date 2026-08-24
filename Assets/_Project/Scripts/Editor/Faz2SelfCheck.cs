using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 2 — saat ekonomisi. Korunan kurallar:
    ///   • Satın alma ödeyemiyorsan gerçekleşmez ve seni ÖLDÜRMEZ
    ///   • Yükseltmeler Tuning asset'ine SIZMAZ (koşu kapsamlı)
    ///   • Hasat tavanı aşılamaz
    ///   • Koşu defteri doğru sayıyor
    ///
    /// Çalıştır: Unity.exe -batchmode -executeMethod TekCan.EditorTools.Faz2SelfCheck.Run
    /// </summary>
    public static class Faz2SelfCheck
    {
        [MenuItem("Tek Can/Faz 2 öz-denetimi çalıştır")]
        public static void Run()
        {
            int failed = 0, total = 0;

            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 100f;
            t.harvestRate = 0.20f;
            t.harvestRateCap = 0.50f;
            t.attackDamage = 3.5f;
            t.dashCooldown = 0.5f;

            float damageBefore = t.attackDamage;
            float dashBefore = t.dashCooldown;

            var root = new GameObject("faz2-check");
            var clock = root.AddComponent<Clock>();
            clock.Init(t, root.transform);

            // 1) Ödeyemediğin şeyi alamazsın ve ölmezsin
            failed += Check(ref total, "pahalı alım reddedildi", !clock.TryBuy(999f), clock.Seconds);
            failed += Check(ref total, "reddedilen alım saati düşürmedi", Mathf.Approximately(clock.Seconds, 100f), clock.Seconds);
            failed += Check(ref total, "reddedilen alım ÖLDÜRMEDİ", !clock.Dead, clock.Dead ? 1 : 0);

            // 2) Alım saatten kesiyor ve deftere yazıyor
            failed += Check(ref total, "alım gerçekleşti", clock.TryBuy(10f), clock.Seconds);
            failed += Check(ref total, "saatten 10 sn kesildi", Mathf.Approximately(clock.Seconds, 90f), clock.Seconds);
            failed += Check(ref total, "harcama deftere yazıldı", Mathf.Approximately(clock.TotalSpent, 10f), clock.TotalSpent);
            failed += Check(ref total, "alım sayacı arttı", clock.Purchases == 1, clock.Purchases);

            // 3) ⚠️ EN KRİTİK: yükseltmeler Tuning asset'ini DEĞİŞTİRMEZ.
            //    Değiştirseydi bir sonraki koşu önceki koşunun gücüyle başlardı
            //    ve editörde kalıcı olurdu.
            clock.UpgradeDamage(5f);
            clock.UpgradeDash(0.2f);
            clock.UpgradeHarvest(0.1f);
            failed += Check(ref total, "hasar Clock'ta arttı", clock.AttackDamage > damageBefore, clock.AttackDamage);
            failed += Check(ref total, "TUNING hasarı DEĞİŞMEDİ", Mathf.Approximately(t.attackDamage, damageBefore), t.attackDamage);
            failed += Check(ref total, "dash Clock'ta kısaldı", clock.DashCooldown < dashBefore, clock.DashCooldown);
            failed += Check(ref total, "TUNING dash'i DEĞİŞMEDİ", Mathf.Approximately(t.dashCooldown, dashBefore), t.dashCooldown);

            // 4) Hasat tavanı
            clock.UpgradeHarvest(99f);
            failed += Check(ref total, "hasat tavanı aşılmadı", Mathf.Approximately(clock.HarvestRate, t.harvestRateCap), clock.HarvestRate);

            // 5) Ölüm ve defter
            clock.ReportKill(); clock.ReportKill();
            failed += Check(ref total, "öldürme sayacı", clock.EnemiesKilled == 2, clock.EnemiesKilled);

            bool died = false;
            clock.OnDeath += () => died = true;
            clock.Spend(999f);
            failed += Check(ref total, "saat sıfırlanınca ölüm olayı tetiklendi", died, clock.Seconds);
            failed += Check(ref total, "ölüden sonra alım yapılamaz", !clock.TryBuy(1f), clock.Seconds);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(t);

            if (failed == 0) Debug.Log($"[Faz 2 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 2 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");

            if (Application.isBatchMode) EditorApplication.Exit(failed == 0 ? 0 : 1);
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
