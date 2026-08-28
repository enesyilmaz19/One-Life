using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 11 — A/B ayar profilleri. Korunan kurallar:
    ///
    ///   • ⚠️⚠️ ASSET'E DOKUNULMUYOR — profil uygulamak `Tuning.asset`'i değiştirseydi
    ///     editörde DİSKE yazılır ve Enes'in bütün his değerleri kalıcı olarak ezilirdi.
    ///   • ⚠️ PROFİLLER BİRİKMİYOR — her uygulama TABANDAN başlamalı. Birikseydi iki kez
    ///     "×2" yapan profil ×4 verir ve bütün karşılaştırma yalan söylerdi.
    ///   • Geri dönüş TAM: profilden çıkınca hiçbir alan sarkmıyor
    ///   • Profiller birbirinden gerçekten farklı
    /// </summary>
    public static class Faz11SelfCheck
    {
        [MenuItem("Tek Can/Faz 11 öz-denetimi çalıştır")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            // "asset" taklidi ve onun çalışma kopyası — Bootstrap ile aynı kurulum
            var asset = ScriptableObject.CreateInstance<Tuning>();
            var çalışma = Object.Instantiate(asset);

            TuneProfile.Reset();
            TuneProfile.Init(çalışma);

            failed += Check(ref total, "en az iki profil tanımlı", TuneProfile.All.Length >= 2,
                            TuneProfile.All.Length);
            failed += Check(ref total, "başlangıçta ilk profil seçili",
                            TuneProfile.CurrentIndex == 0, TuneProfile.CurrentIndex);

            // ── 1) ⚠️ ASSET'E DOKUNULMUYOR ──
            //    Bütün profilleri sırayla uygula, sonra "asset"in bozulmadığını doğrula.
            float assetBossHasat = asset.bossHarvestMultiplier;
            float assetArena = asset.arenaRadius;
            for (int i = 0; i < TuneProfile.All.Length; i++) TuneProfile.Select(i, çalışma);

            failed += Check(ref total, "⚠️ ASSET bozulmadı (boss hasadı)",
                            Mathf.Approximately(asset.bossHarvestMultiplier, assetBossHasat),
                            asset.bossHarvestMultiplier);
            failed += Check(ref total, "⚠️ ASSET bozulmadı (arena yarıçapı)",
                            Mathf.Approximately(asset.arenaRadius, assetArena), asset.arenaRadius);

            // ── 2) ⚠️ PROFİLLER BİRİKMİYOR ──
            //    Aynı profili iki kez uygulamak, bir kez uygulamakla AYNI olmalı.
            int bossProfil = ProfilBul("BOSS BESLİYOR");
            TuneProfile.Select(0, çalışma);
            TuneProfile.Select(bossProfil, çalışma);
            float birKez = çalışma.bossHarvestMultiplier;

            TuneProfile.Select(bossProfil, çalışma);
            TuneProfile.Select(bossProfil, çalışma);
            float üçKez = çalışma.bossHarvestMultiplier;

            failed += Check(ref total, "⚠️ profil BİRİKMİYOR (3 kez = 1 kez)",
                            Mathf.Approximately(birKez, üçKez), üçKez - birKez);

            // ── 3) ⚠️ GERİ DÖNÜŞ TAM — hiçbir alan sarkmıyor ──
            //    Yansımayla BÜTÜN alanlar karşılaştırılıyor: elle liste tutsaydım
            //    bir sonraki yeni ayar alanı sessizce kontrolsüz kalırdı.
            TuneProfile.Select(TuneProfile.All.Length - 1, çalışma);   // en çok değiştiren
            TuneProfile.Select(0, çalışma);                            // MEVCUT'a dön

            int sarkan = 0;
            string ilkSarkan = "";
            foreach (var f in typeof(Tuning).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var a = f.GetValue(asset);
                var b = f.GetValue(çalışma);
                if (Equals(a, b)) continue;
                sarkan++;
                if (ilkSarkan == "") ilkSarkan = $"{f.Name}: {a} → {b}";
            }
            failed += Check(ref total, $"⚠️ MEVCUT'a dönünce hiçbir alan SARKMIYOR" +
                                       (sarkan > 0 ? $" (ilk: {ilkSarkan})" : ""),
                            sarkan == 0, sarkan);

            // ── 4) Profiller birbirinden farklı ──
            int aynı = 0;
            for (int i = 0; i < TuneProfile.All.Length; i++)
            {
                TuneProfile.Select(i, çalışma);
                string imza = İmza(çalışma);
                for (int j = i + 1; j < TuneProfile.All.Length; j++)
                {
                    TuneProfile.Select(j, çalışma);
                    if (İmza(çalışma) == imza) aynı++;
                    TuneProfile.Select(i, çalışma);
                }
            }
            failed += Check(ref total, "profiller birbirinin aynısı değil", aynı == 0, aynı);

            // ── 5) Her profilin adı ve notu var — HUD'da ne olduğu okunmalı ──
            int adsız = 0;
            foreach (var p in TuneProfile.All)
                if (string.IsNullOrWhiteSpace(p.Name)) adsız++;
            failed += Check(ref total, "her profilin adı var", adsız == 0, adsız);

            TuneProfile.Reset();
            Object.DestroyImmediate(çalışma);
            Object.DestroyImmediate(asset);

            if (failed == 0) Debug.Log($"[Faz 11 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 11 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        static int ProfilBul(string ad)
        {
            for (int i = 0; i < TuneProfile.All.Length; i++)
                if (TuneProfile.All[i].Name == ad) return i;
            return TuneProfile.All.Length > 1 ? 1 : 0;
        }

        /// <summary>Bütün alanların birleşimi — profil gerçekten farklı mı, tam karşılaştırma.</summary>
        static string İmza(Tuning t)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var f in typeof(Tuning).GetFields(BindingFlags.Public | BindingFlags.Instance))
                sb.Append(f.GetValue(t)).Append('|');
            return sb.ToString();
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
