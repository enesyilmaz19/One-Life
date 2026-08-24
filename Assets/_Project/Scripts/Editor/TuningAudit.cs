using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Tuning.cs varsayılanları ile Tuning.asset'teki kayıtlı değerleri karşılaştırır.
    ///
    /// NEDEN VAR: asset'te zaten serileşmiş bir alan C# varsayılanını **ezer**. 2026-08-24'te
    /// Enes değerleri .cs'ten düzenledi; harvestRate / dashDistance / attackDuration hiç
    /// uygulanmadı ve bütün ayar seansı yanlış varsayımla geçti. Sessiz olduğu için tehlikeli.
    ///
    /// Bu araç hiçbir şeyi DEĞİŞTİRMEZ, yalnızca farkı gösterir.
    /// Ayarları Inspector'dan düzenle; .cs varsayılanları sadece asset'te olmayan yeni alanlar için geçerli.
    /// </summary>
    public static class TuningAudit
    {
        const string AssetPath = "Assets/_Project/Resources/Tuning.asset";

        /// <summary>
        /// Her script derlemesinden sonra sessizce kontrol eder. Amaç: Tuning.cs'i
        /// düzenleyen kişi sapmayı GÜNLER SONRA değil, derleme biter bitmez görsün.
        /// 2026-08-24'te bir playtest seansı bu sessizlik yüzünden yanlış varsayımla geçti.
        /// </summary>
        [InitializeOnLoadMethod]
        static void HookAutoCheck() => EditorApplication.delayCall += AutoCheck;

        static void AutoCheck()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Execute();   // sapma varsa kendi uyarısını basıyor
        }

        [MenuItem("Tek Can/Ayar kaynağı denetimi")]
        public static void Run()
        {
            int problems = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(problems == 0 ? 0 : 1);
        }

        /// <summary>Ayrışan alan sayısını döndürür; toplu koşucu bunu kullanıyor.</summary>
        public static int Execute()
        {
            var asset = AssetDatabase.LoadAssetAtPath<Tuning>(AssetPath);
            if (asset == null)
            {
                Debug.LogWarning($"[Ayar denetimi] {AssetPath} yok — çalışma anında C# varsayılanları geçerli.");
                return 0;
            }

            // C# varsayılanları: sınıfın taze bir örneği alan başlatıcılarını taşır.
            var fresh = ScriptableObject.CreateInstance<Tuning>();
            var serialized = SerializedFieldNames(asset);

            var overridden = new List<string>();
            foreach (var f in typeof(Tuning).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.FieldType != typeof(float) && f.FieldType != typeof(int)) continue;

                object a = f.GetValue(asset), c = f.GetValue(fresh);
                if (Equals(a, c)) continue;

                // Asset'te kayıtlı DEĞİLSE fark olamaz; kayıtlıysa ve farklıysa C# eziliyor.
                if (serialized.Contains(f.Name))
                    overridden.Add($"  {f.Name,-28} C#={c,-10} ÇALIŞAN={a}");
            }

            if (overridden.Count == 0)
                Debug.Log("[Ayar denetimi] ✅ Çalışan değerler C# varsayılanlarıyla uyumlu.");
            else
                Debug.LogWarning(
                    $"[Ayar denetimi] ⚠️ {overridden.Count} alanda .cs değişikliği UYGULANMIYOR " +
                    $"(asset eziyor):\n{string.Join("\n", overridden)}\n" +
                    "→ Bu değerleri Inspector'dan düzenle, .cs'ten değil.");

            Object.DestroyImmediate(fresh);
            return overridden.Count;
        }

        /// <summary>
        /// C# varsayılanlarını asset'e yazar — sapmanın TEK meşru çözüm yolu.
        ///
        /// Yönü bilerek tek taraflı: otomatik senkron yapılsaydı Inspector'da ayarlanmış
        /// his değerleri, biri Tuning.cs'e dokunduğu anda sessizce silinirdi. Bu yüzden
        /// senkron elle tetikleniyor ve neyin değiştiğini tek tek yazıyor.
        ///
        /// Senkrondan sonra asset BÜTÜN alanları içerir; yani bundan sonra .cs
        /// düzenlemeleri hiç uygulanmaz ve otomatik denetim bunu her derlemede söyler.
        /// </summary>
        [MenuItem("Tek Can/Ayarları senkronla (C# → asset)")]
        public static void SyncFromCode()
        {
            var asset = AssetDatabase.LoadAssetAtPath<Tuning>(AssetPath);
            if (asset == null)
            {
                Debug.LogError($"[Ayar senkron] {AssetPath} yok.");
                return;
            }

            var fresh = ScriptableObject.CreateInstance<Tuning>();
            var so = new SerializedObject(asset);
            var changes = new List<string>();

            foreach (var f in typeof(Tuning).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var prop = so.FindProperty(f.Name);
                if (prop == null) continue;

                object codeVal = f.GetValue(fresh), assetVal = f.GetValue(asset);
                if (Equals(codeVal, assetVal)) continue;

                if (f.FieldType == typeof(float)) prop.floatValue = (float)codeVal;
                else if (f.FieldType == typeof(int)) prop.intValue = (int)codeVal;
                else continue;

                changes.Add($"  {f.Name,-28} {assetVal} → {codeVal}");
            }

            Object.DestroyImmediate(fresh);

            if (changes.Count == 0)
            {
                Debug.Log("[Ayar senkron] Zaten uyumlu, değişiklik yok.");
                return;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Ayar senkron] ✅ {changes.Count} alan asset'e yazıldı:\n{string.Join("\n", changes)}");
        }

        /// <summary>Asset dosyasında gerçekten yazılı olan alan adları.</summary>
        static HashSet<string> SerializedFieldNames(Tuning asset)
        {
            var names = new HashSet<string>();
            var so = new SerializedObject(asset);
            var it = so.GetIterator();
            while (it.NextVisible(true)) names.Add(it.name);
            return names;
        }

        /// <summary>Sapma varsa 1 ile çıkar — Faz1SelfCheck ile aynı sözleşme, kapı olarak kullanılabilsin.</summary>
        static void Finish(int problems = 0)
        {
            if (Application.isBatchMode) EditorApplication.Exit(problems == 0 ? 0 : 1);
        }
    }
}
