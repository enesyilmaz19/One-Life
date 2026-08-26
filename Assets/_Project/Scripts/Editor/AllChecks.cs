using System.Text;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Bütün kapıları tek Unity açılışında çalıştırır. Üç ayrı batchmode başlatmak
    /// hem yavaş hem de tek tek unutulmaya açıktı (Bootstrap'ı yeniden yapılandırdıktan
    /// sonra Faz1Repro'yu çalıştırmayı unutmuştum — öz-inceleme yakaladı).
    ///
    /// Çalıştır: Unity.exe -batchmode -executeMethod TekCan.EditorTools.AllChecks.Run
    /// Herhangi biri düşerse 1 ile çıkar.
    /// </summary>
    public static class AllChecks
    {
        [MenuItem("Tek Can/TÜM kapıları çalıştır")]
        public static void Run()
        {
            var sb = new StringBuilder("\n══════ TEK CAN — TÜM KAPILAR ══════\n");
            int problems = 0;

            int f1 = Faz1SelfCheck.Execute();
            sb.AppendLine(Line("Faz 1 öz-denetim", f1 == 0, f1 == 0 ? "geçti" : $"{f1} başarısız"));
            problems += f1;

            int f2 = Faz2SelfCheck.Execute();
            sb.AppendLine(Line("Faz 2 öz-denetim", f2 == 0, f2 == 0 ? "geçti" : $"{f2} başarısız"));
            problems += f2;

            // Gerçek spawn geometrisiyle hasar regresyonu — düşmanların havada doğduğu
            // hata buradan geçmişti ve izole testler onu göremiyordu.
            bool repro = Faz1Repro.Execute();
            sb.AppendLine(Line("Hasar reprodüksiyonu", repro, repro ? "gerçek geometride iniyor" : "HASAR İNMİYOR"));
            if (!repro) problems++;

            int f3 = Faz3SelfCheck.Execute();
            sb.AppendLine(Line("Faz 3 öz-denetim", f3 == 0, f3 == 0 ? "geçti" : $"{f3} başarısız"));
            problems += f3;

            int f4 = Faz4SelfCheck.Execute();
            sb.AppendLine(Line("Faz 4 öz-denetim", f4 == 0, f4 == 0 ? "geçti" : $"{f4} başarısız"));
            problems += f4;

            int f5 = Faz5SelfCheck.Execute();
            sb.AppendLine(Line("Faz 5 öz-denetim", f5 == 0, f5 == 0 ? "geçti" : $"{f5} başarısız"));
            problems += f5;

            int f6 = Faz6SelfCheck.Execute();
            sb.AppendLine(Line("Faz 6 öz-denetim", f6 == 0, f6 == 0 ? "geçti" : $"{f6} başarısız"));
            problems += f6;

            int f7 = Faz7SelfCheck.Execute();
            sb.AppendLine(Line("Faz 7 öz-denetim", f7 == 0, f7 == 0 ? "geçti" : $"{f7} başarısız"));
            problems += f7;

            int f8 = Faz8SelfCheck.Execute();
            sb.AppendLine(Line("Faz 8 öz-denetim", f8 == 0, f8 == 0 ? "geçti" : $"{f8} başarısız"));
            problems += f8;

            int audit = TuningAudit.Execute();
            sb.AppendLine(audit == 0
                ? Line("Ayar kaynağı denetimi", true, "C# ↔ asset uyumlu")
                : $"  ⚠️  Ayar kaynağı denetimi   {audit} alan ayrışmış — kapıyı düşürmez");
            // ⚠️ Ayar sapması KAPIYI DÜŞÜRMEZ: hangi değerin doğru olduğu Enes'in kararı,
            // araç yalnızca sessiz kalmasını engelliyor.

            sb.AppendLine("═══════════════════════════════════");
            sb.AppendLine(problems == 0 ? "SONUÇ: ✅ TEMİZ" : $"SONUÇ: ❌ {problems} BAŞARISIZ KONTROL");

            if (problems == 0) Debug.Log(sb.ToString());
            else Debug.LogError(sb.ToString());

            if (Application.isBatchMode) EditorApplication.Exit(problems == 0 ? 0 : 1);
        }

        static string Line(string name, bool ok, string detail)
            => $"  {(ok ? "✅" : "❌")} {name,-24} {detail}";
    }
}
