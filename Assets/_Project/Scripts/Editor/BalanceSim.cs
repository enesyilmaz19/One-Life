using System.Text;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Saat ekonomisinin denge tarayıcısı.
    ///
    /// TASARIM.md'nin "OYUNUN EN KRİTİK SAYISI" dediği hasat oranını tahminle değil
    /// ölçerek buluyor. Hedef: **temiz kavga net pozitif, dağınık kavga net negatif.**
    /// Çok yüksekse dövüş kendini besler ve saat anlamsızlaşır; çok düşükse her kavga
    /// zarardır ve oyuncu kaçmaya başlar — ikisi de oyunu bitirir.
    ///
    /// Değerleri gerçek Tuning.asset'ten okur (lessons.md: düzenek yerleşimi ithal eder).
    /// Deterministik: aynı tohum, aynı sonuç.
    ///
    /// Çalıştır: Unity.exe -batchmode -quit -executeMethod TekCan.EditorTools.BalanceSim.Run
    /// </summary>
    public static class BalanceSim
    {
        /// <summary>Oyuncu beceri profili — kaç savurmadan kaçı tutuyor, düşman başına kaç darbe yiyor.</summary>
        struct Skill
        {
            public string Name;
            public float HitChance;       // savurmaların isabet oranı
            public float HitsTaken;       // düşman başına yenen darbe sayısı
            public float TargetsPerSwing; // koni kaç düşmanı yakalıyor
        }

        static readonly Skill[] Skills =
        {
            new Skill { Name = "TEMİZ  ", HitChance = 0.90f, HitsTaken = 0.4f, TargetsPerSwing = 1f },
            new Skill { Name = "ORTA   ", HitChance = 0.65f, HitsTaken = 1.4f, TargetsPerSwing = 1f },
            new Skill { Name = "DAĞINIK", HitChance = 0.40f, HitsTaken = 3.0f, TargetsPerSwing = 1f },
        };

        [MenuItem("Tek Can/Denge simülasyonu")]
        public static void Run()
        {
            var t = AssetDatabase.LoadAssetAtPath<Tuning>("Assets/_Project/Resources/Tuning.asset")
                    ?? ScriptableObject.CreateInstance<Tuning>();

            var sb = new StringBuilder();
            sb.AppendLine("[DENGE] Düşman başına NET saniye (pozitif = kâr)");
            sb.AppendLine($"        havuz={t.enemyTimePool} akış={t.enemyDecayPerSecond}/sn hasar={t.attackDamage} " +
                          $"savurma={t.attackDuration}sn drenaj={t.drainInCombat}/sn darbe={t.enemyFullHitCost}sn");
            sb.AppendLine();
            sb.AppendLine("  hasat |   TEMİZ    ORTA   DAĞINIK  | karar");
            sb.AppendLine("  ------+---------------------------+---------------------------");

            float best = -1f;
            for (float hr = 0.10f; hr <= 0.71f; hr += 0.05f)
            {
                float clean = NetPerEnemy(t, Skills[0], hr);
                float mid = NetPerEnemy(t, Skills[1], hr);
                float messy = NetPerEnemy(t, Skills[2], hr);

                bool ok = clean > 0f && messy < 0f;
                if (ok && best < 0f) best = hr;

                sb.AppendLine($"   %{hr * 100f,3:0} | {clean,7:+0.0;-0.0} {mid,7:+0.0;-0.0} {messy,8:+0.0;-0.0}  | " +
                              (ok ? "✅ HEDEF ARALIK" : clean <= 0f ? "temiz bile zarar — çok düşük"
                                                                   : "dağınık bile kâr — çok yüksek"));
            }

            sb.AppendLine();
            sb.AppendLine(best >= 0f
                ? $"→ EN DÜŞÜK UYGUN HASAT: %{best * 100f:0}   (şu anki ayar: %{t.harvestRate * 100f:0})"
                : "→ HİÇBİR HASAT ORANI HEDEFİ TUTMUYOR: hasar/havuz/drenaj oranları elden geçmeli");

            // Çoklu hedef etkisi — bilinen yapısal sorun, sayıyla göster
            sb.AppendLine();
            sb.AppendLine("[ÇOKLU HEDEF] aynı ayarla koni kaç düşman yakalarsa ne oluyor (TEMİZ oyuncu):");
            for (int n = 1; n <= 4; n++)
            {
                var s = Skills[0]; s.TargetsPerSwing = n;
                sb.AppendLine($"   {n} hedef → düşman başına {NetPerEnemy(t, s, t.harvestRate),6:+0.0;-0.0} sn");
            }
            sb.AppendLine("   (hedef sayısı arttıkça kâr artıyorsa kalabalık oyunu KOLAYLAŞTIRIYOR)");

            Debug.Log(sb.ToString());
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// Tek düşmanı öldürmenin net saniye bilançosu.
        /// Adım adım simüle ediliyor çünkü havuz hem oyuncudan hem kendi akışından
        /// eriyor — kapalı formül yazmak kolayca yanlış olur.
        /// </summary>
        static float NetPerEnemy(Tuning t, Skill skill, float harvestRate)
        {
            const float dt = 1f / 60f;
            var rng = new System.Random(12345);              // deterministik

            float pool = t.enemyTimePool;
            float harvested = 0f, elapsed = 0f, swingTimer = 0f;

            while (pool > 0f && elapsed < 60f)
            {
                elapsed += dt;
                pool = Mathf.Max(0f, pool - t.enemyDecayPerSecond * dt);

                swingTimer -= dt;
                if (swingTimer <= 0f)
                {
                    swingTimer = t.attackDuration;
                    if (rng.NextDouble() < skill.HitChance)
                    {
                        float drained = Mathf.Min(t.attackDamage, pool);
                        pool -= drained;
                        // Koni birden çok düşmanı yakalarsa her biri ayrı ödüyor.
                        harvested += drained * harvestRate * skill.TargetsPerSwing;
                    }
                }
            }

            elapsed += t.attackDuration;                     // boşalanı bitiren vuruş
            float drained_ = t.drainInCombat * elapsed;      // saat akışı
            float taken = skill.HitsTaken * t.enemyFullHitCost;

            return harvested - drained_ - taken;
        }
    }
}
