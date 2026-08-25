using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 6 — ses. Korunan kurallar:
    ///   • ⚠️ HER SES GERÇEKTEN SES Mİ (sessiz tampon üretmek çok kolay)
    ///   • Sesler birbirinden farklı (hepsi aynı gürültü olmasın)
    ///   • Süreleri makul (kilitlenme yok, sonsuz döngü yok)
    ///
    /// "Ses sistemi kuruldu" demek kolay. Üretilen tamponun içi boşsa hiçbir test
    /// bunu fark etmez — DURGUN dersi burada da geçerli: önce en aptal soru.
    /// </summary>
    public static class Faz6SelfCheck
    {
        [MenuItem("Tek Can/Faz 6 öz-denetimi çalıştır")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            var go = new GameObject("faz6-check");
            Sfx.Reset();
            Sfx.Init(go.AddComponent<AudioSource>());

            var clips = Sfx.AllClips;
            var names = Sfx.AllNames;

            // ── 1) ⚠️ HER SES ÜRETİLDİ Mİ ve İÇİ DOLU MU ──
            for (int i = 0; i < clips.Length; i++)
            {
                failed += Check(ref total, $"{names[i]} üretildi", clips[i] != null, i);
                if (clips[i] == null) continue;

                float peak = Sfx.PeakOf(clips[i]);
                failed += Check(ref total, $"{names[i]} SESSİZ DEĞİL (tepe {peak:0.00})", peak > 0.05f, peak);
                failed += Check(ref total, $"{names[i]} kırpılmamış", peak <= 1.001f, peak);
            }

            // ── 2) Süreler makul ──
            foreach (var c in clips)
            {
                if (c == null) continue;
                failed += Check(ref total, $"{c.name} süresi makul (0.05-1.5 sn)",
                                c.length > 0.05f && c.length < 1.5f, c.length);
            }

            // ── 3) Sesler birbirinden farklı ──
            //    Hepsi aynı gürültüyse "ses var" demek yanıltıcı olur.
            int ayni = 0;
            for (int i = 0; i < clips.Length; i++)
                for (int j = i + 1; j < clips.Length; j++)
                    if (clips[i] != null && clips[j] != null && Ayni(clips[i], clips[j])) ayni++;
            failed += Check(ref total, "sesler birbirinin aynısı değil", ayni == 0, ayni);

            Object.DestroyImmediate(go);
            Sfx.Reset();

            if (failed == 0) Debug.Log($"[Faz 6 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 6 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        /// <summary>İki ses aynı mı — uzunluk ve kaba enerji profiline bakıyor.</summary>
        static bool Ayni(AudioClip a, AudioClip b)
        {
            if (a.samples != b.samples) return false;
            return Mathf.Abs(Enerji(a) - Enerji(b)) < 0.0001f;
        }

        static float Enerji(AudioClip c)
        {
            var d = new float[c.samples];
            c.GetData(d, 0);
            float sum = 0f;
            foreach (var s in d) sum += s * s;
            return sum / Mathf.Max(1, d.Length);
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
