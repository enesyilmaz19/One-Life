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

        [MenuItem("Tek Can/Ayar kaynağı denetimi")]
        public static void Run()
        {
            var asset = AssetDatabase.LoadAssetAtPath<Tuning>(AssetPath);
            if (asset == null)
            {
                Debug.LogWarning($"[Ayar denetimi] {AssetPath} yok — çalışma anında C# varsayılanları geçerli.");
                Finish();
                return;
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
            Finish();
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

        static void Finish()
        {
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
