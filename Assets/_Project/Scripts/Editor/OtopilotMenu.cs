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

        /// <summary>Faz 26 playtest'inin yedi maddesi (Enes 2026-10-11: "playtesti de bot ile yap"). ~15 dk.</summary>
        [MenuItem("Tek Can/Otopilot — Faz 26 playtest (izle)")]
        public static void Playtest26Izle() => Başlat(izle: true, senaryo: "p26");

        public static void Playtest26() => Başlat(izle: false, senaryo: "p26");

        /// <summary>Faz 29 playtest'i: silah sunağı, İPOTEK YAĞMURU, HACİZ İHBARI, ÇİFT TAPU, İCRA, MUAFİYET/SABIR. ~10 dk.</summary>
        [MenuItem("Tek Can/Otopilot — Faz 29 playtest (izle)")]
        public static void Playtest29Izle() => Başlat(izle: true, senaryo: "p29");

        public static void Playtest29() => Başlat(izle: false, senaryo: "p29");

        /// <summary>Faz 30 playtest'i: 2 silahla başla, HACİZ sunakta, İPOTEK keşifte, KADRO, gerçek combo, İFLAS boss'tan. ~15 dk.</summary>
        [MenuItem("Tek Can/Otopilot — Faz 30 playtest (izle)")]
        public static void Playtest30Izle() => Başlat(izle: true, senaryo: "p30");

        public static void Playtest30() => Başlat(izle: false, senaryo: "p30");

        /// <summary>Faz 27 ölçümü: bütün bölgeler atlamadan, bir tam koşu kaç dakika (Enes 2026-10-12).</summary>
        [MenuItem("Tek Can/Otopilot — tam koşu ölçümü (izle)")]
        public static void TamKosuIzle() => Başlat(izle: true, senaryo: "tam");

        public static void TamKosu() => Başlat(izle: false, senaryo: "tam");

        /// <summary>Teşhis: M ile ilk mini boss, 10 sn'de bir oyun zamanı / saat akışı nabzı.</summary>
        public static void MiniTeshis() => Başlat(izle: false, senaryo: "mini");

        /// <summary>Enes: mini boss zaman + zorluk testi — 5 bölge × 5 mini, beklenen güçle (~1–1.5 saat).</summary>
        public static void MiniTesti() => Başlat(izle: false, senaryo: "minitest");

        /// <summary>Ekonomi ayarı: yalnızca 1. ara bölge (7 oda + mini boss + sunak), ~10 dk.</summary>
        public static void AraBolge1Olcum() => Başlat(izle: false, senaryo: "ab1");

        /// <summary>Faz 28 ölçümü: yalnızca 1. bölge (46 oda) — oda, mini boss ve ana boss süreleri.</summary>
        [MenuItem("Tek Can/Otopilot — 1. bölge ölçümü (izle)")]
        public static void Bolge1OlcumIzle() => Başlat(izle: true, senaryo: "b1");

        public static void Bolge1Olcum() => Başlat(izle: false, senaryo: "b1");

        static void Başlat(bool izle, string senaryo = "")
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
            SessionState.SetString(Otopilot.SenaryoAnahtar, senaryo);
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
