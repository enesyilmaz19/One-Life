using System.IO;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Otopilotu başlatır ve K1 sahnesinde "oyunu kapat, yeniden aç"ı yürütür
    /// (Play'den çıkış → yeniden giriş). Bot `Otopilot.cs`'te.
    ///
    /// İzle:  `Tek Can → Otopilot (izle)` — gerçek hızda, ekranda yazılı, ekran görüntülü.
    /// Batch: `Unity.exe -batchmode -projectPath … -executeMethod TekCan.EditorTools.OtopilotMenu.Batch -logFile …`
    ///        ⚠️ `-quit` YOK: bot bitince kendisi çıkar (geçti 0, kaldı 1).
    /// GUI'de doğrudan izleme: aynı komut `-batchmode` olmadan, `…OtopilotMenu.İzle`.
    /// </summary>
    [InitializeOnLoad]
    public static class OtopilotMenu
    {
        static OtopilotMenu() => EditorApplication.playModeStateChanged += Değişti;

        [MenuItem("Tek Can/Otopilot (izle)")]
        public static void İzle() => Başlat(izle: true);

        public static void Batch() => Başlat(izle: false);

        /// <summary>Komut satırından izleme (ASCII ad — Türkçe karakter komut satırında sorun çıkarabilir).</summary>
        public static void Izle() => Başlat(izle: true);

        static void Başlat(bool izle)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[OTOPİLOT] Önce Play'den çık.");
                if (Application.isBatchMode) EditorApplication.Exit(2);
                return;
            }
            // ⚠️ Derleme hatası varken Play'e girilemez; bayrak kalırsa bot Enes'in bir
            // sonraki ELLE Play'ine sızardı (inceleme). Bayrağı koymadan önce bak.
            if (EditorUtility.scriptCompilationFailed || EditorApplication.isCompiling)
            {
                Debug.LogError("[OTOPİLOT] Derleme hatası var ya da derleniyor — başlatılmadı.");
                if (Application.isBatchMode) EditorApplication.Exit(2);
                return;
            }
            SessionState.SetBool(Otopilot.Aktif, true);
            SessionState.SetBool(Otopilot.İzleAnahtar, izle);
            SessionState.SetInt(Otopilot.AdımAnahtar, 0);
            SessionState.SetInt(Otopilot.HataAnahtar, 0);
            SessionState.SetInt(Otopilot.SSAnahtar, 0);
            SessionState.EraseString(Otopilot.RaporAnahtar);
            SessionState.SetString(Otopilot.KlasörAnahtar,
                Path.Combine("Logs", "Otopilot", System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm")));
            Otopilot.KayıtDosyalarınıSil();
            EditorApplication.EnterPlaymode();
        }

        static void Değişti(PlayModeStateChange durum)
        {
            if (durum != PlayModeStateChange.EnteredEditMode) return;
            if (SessionState.GetBool(Otopilot.YenidenGir, false))
            {
                // K1: "oyunu kapat, yeniden aç".
                SessionState.EraseBool(Otopilot.YenidenGir);
                EditorApplication.delayCall += EditorApplication.EnterPlaymode;
                return;
            }
            // ⚠️ Bot bir sonraki ELLE Play'e sızmasın — kayıt yönlendirmesi dahil.
            SessionState.EraseBool(Otopilot.Aktif);
        }
    }
}
