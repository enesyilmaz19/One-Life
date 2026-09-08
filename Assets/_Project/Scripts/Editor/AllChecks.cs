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

            // Enes "bossa bazen vuramıyorum" dedi İKİ KEZ. İlk düzeltme kök nedenin
            // yarısıydı (menzil yüzeye çevrildi, açı merkezde kaldı). Bu kapı ikinci
            // yarıyı koruyor: gövde yayla örtüşüyorsa vuruş İNMEK ZORUNDA.
            bool bossHit = BossHitRepro.Execute();
            sb.AppendLine(Line("Boss isabet reprodüksiyonu", bossHit,
                               bossHit ? "gövde yaydaysa vuruş iniyor" : "ÖLÜ BANT VAR"));
            if (!bossHit) problems++;

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

            int f9 = Faz9SelfCheck.Execute();
            sb.AppendLine(Line("Faz 9 öz-denetim", f9 == 0, f9 == 0 ? "geçti" : $"{f9} başarısız"));
            problems += f9;

            int f10 = Faz10SelfCheck.Execute();
            sb.AppendLine(Line("Faz 10 öz-denetim", f10 == 0, f10 == 0 ? "geçti" : $"{f10} başarısız"));
            problems += f10;

            int f11 = Faz11SelfCheck.Execute();
            sb.AppendLine(Line("Faz 11 öz-denetim", f11 == 0, f11 == 0 ? "geçti" : $"{f11} başarısız"));
            problems += f11;

            int f12 = Faz12SelfCheck.Execute();
            sb.AppendLine(Line("Faz 12 öz-denetim", f12 == 0, f12 == 0 ? "geçti" : $"{f12} başarısız"));
            problems += f12;

            int f13 = Faz13SelfCheck.Execute();
            sb.AppendLine(Line("Faz 13 öz-denetim", f13 == 0, f13 == 0 ? "geçti" : $"{f13} başarısız"));
            problems += f13;

            int f16 = Faz16SelfCheck.Execute();
            sb.AppendLine(Line("Faz 16 öz-denetim", f16 == 0, f16 == 0 ? "geçti" : $"{f16} başarısız"));
            problems += f16;

            int f17 = Faz17SelfCheck.Execute();
            sb.AppendLine(Line("Faz 17 öz-denetim", f17 == 0, f17 == 0 ? "geçti" : $"{f17} başarısız"));
            problems += f17;

            int f18 = Faz18SelfCheck.Execute();
            sb.AppendLine(Line("Faz 18 öz-denetim", f18 == 0, f18 == 0 ? "geçti" : $"{f18} başarısız"));
            problems += f18;

            int f19 = Faz19SelfCheck.Execute();
            sb.AppendLine(Line("Faz 19 öz-denetim", f19 == 0, f19 == 0 ? "geçti" : $"{f19} başarısız"));
            problems += f19;

            int f21 = Faz21SelfCheck.Execute();
            sb.AppendLine(Line("Faz 21 öz-denetim", f21 == 0, f21 == 0 ? "geçti" : $"{f21} başarısız"));
            problems += f21;

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
