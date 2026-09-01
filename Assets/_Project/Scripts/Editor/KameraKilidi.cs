using System.Text;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Kamera kilidi raporu — **asset alışverişinin brief'i.**
    ///
    /// Asset kapısının dördüncü maddesi: "kamera açısı ve mesafesi kilitli."
    /// Ama kilidin işe yarayan çıktısı açı değil, **ekranda kaç piksel görünüyor**
    /// sorusunun cevabı. O sayı olmadan model alışverişi körlemesine olur:
    /// yüz detayı, ince doku, küçük aksesuar — hiçbiri görünmüyorsa parası boşa gider.
    ///
    /// ⚠️ Sayılar İDDİA EDİLMİYOR, HESAPLANIYOR. Ortografik kamerada ekranın
    /// yarı yüksekliği `camOrthoSize` dünya birimidir; dikey bir nesnenin ekrana
    /// düşen boyu ise eğim yüzünden `cos(pitch)` kadar kısalır.
    ///
    /// Çalıştır: -executeMethod TekCan.EditorTools.KameraKilidi.Run
    /// </summary>
    public static class KameraKilidi
    {
        /// <summary>Kapsül oyuncunun dünya birimi cinsinden boyu (Bootstrap ölçeği).</summary>
        const float KarakterBoyu = 2f;

        [MenuItem("Tek Can/Kamera kilidi raporu")]
        public static void Run()
        {
            var t = AssetDatabase.LoadAssetAtPath<Tuning>("Assets/_Project/Resources/Tuning.asset")
                    ?? ScriptableObject.CreateInstance<Tuning>();

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("═══════ KAMERA KİLİDİ ═══════");
            sb.AppendLine($"  açı (yaw)       : {t.camYaw:0}°");
            sb.AppendLine($"  eğim (pitch)    : {t.camPitch:0}°");
            sb.AppendLine($"  ortografik boy  : {t.camOrthoSize:0.0}  (ekran yarı yüksekliği, dünya birimi)");
            sb.AppendLine();

            float görüşYüksekliği = t.camOrthoSize * 2f;
            sb.AppendLine($"  görüş alanı     : {görüşYüksekliği:0.0} birim yükseklik");
            sb.AppendLine($"  arena çapı      : {t.arenaRadius * 2f:0.0} birim  → " +
                          (t.arenaRadius * 2f > görüşYüksekliği
                              ? "⚠️ arena EKRANA SIĞMIYOR, kamera takip ediyor"
                              : "arena tamamen görünüyor"));
            sb.AppendLine();

            // ⚠️ Dikey nesne eğik kamerada KISALIR. Ekran yukarısı kameranın yukarısı;
            // dünya dikeyinin oraya düşen payı cos(pitch).
            float kısalma = Mathf.Cos(t.camPitch * Mathf.Deg2Rad);
            float ekranBirimi = KarakterBoyu * kısalma;

            sb.AppendLine($"  ⚠️ EĞİM KISALTMASI: dikey nesne ×{kısalma:0.00} görünüyor " +
                          $"({t.camPitch:0}° eğim)");
            sb.AppendLine($"     {KarakterBoyu:0.0} birimlik karakter ekranda {ekranBirimi:0.00} birim");
            sb.AppendLine();

            sb.AppendLine("  ── ASSET BRIEF: karakter ekranda kaç PİKSEL ──");
            foreach (int yükseklik in new[] { 720, 1080, 1440, 2160 })
            {
                float pikselBirim = yükseklik / görüşYüksekliği;
                float karakterPiksel = ekranBirimi * pikselBirim;
                sb.AppendLine($"     {yükseklik,4}p → karakter ≈ {karakterPiksel,5:0} piksel  " +
                              $"({pikselBirim:0} piksel/birim)");
            }
            sb.AppendLine();

            float p1080 = ekranBirimi * (1080f / görüşYüksekliği);
            sb.AppendLine("  ── NE ANLAMA GELİYOR ──");
            sb.AppendLine($"     1080p'de karakter ~{p1080:0} piksel yüksekliğinde.");
            sb.AppendLine(p1080 < 150f
                ? "     → DÜŞÜK POLİ yeterli. Yüz detayı, ince doku, küçük aksesuar GÖRÜNMEZ.\n" +
                  "       Para SİLUET ve RENGE harcanmalı; bu boyutta ayırt edilen tek şey onlar."
                : "     → Orta detay görünür hale geliyor; yine de siluet önce gelir.");
            sb.AppendLine();
            sb.AppendLine("     ⚠️ Bu proje görsel kimliği zaten SİLUET + VFX üzerine kurdu");
            sb.AppendLine("       (CLAUDE.md). Ölçüm o kararı doğruluyor, tesadüf değil.");

            Debug.Log(sb.ToString());
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
