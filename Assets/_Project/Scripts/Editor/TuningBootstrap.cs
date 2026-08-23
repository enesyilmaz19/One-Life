using System.IO;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Tuning.asset yoksa oluşturur. Varlığı Unity'nin kendisi yazsın —
    /// .asset YAML'ını elle yazmak GUID eşleştirmesi yüzünden kırılgan.
    /// </summary>
    [InitializeOnLoad]
    public static class TuningBootstrap
    {
        const string Dir = "Assets/_Project/Resources";
        const string Path = Dir + "/Tuning.asset";

        // Editörde: import bittikten sonra. Batchmode'da delayCall çalışmadığı için
        // CLI'dan -executeMethod TekCan.EditorTools.TuningBootstrap.Ensure ile çağrılır.
        static TuningBootstrap() => EditorApplication.delayCall += Ensure;

        [MenuItem("Tek Can/Tuning varlığını oluştur")]
        public static void Ensure()
        {
            if (AssetDatabase.LoadAssetAtPath<Tuning>(Path) != null) return;

            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
                AssetDatabase.Refresh();
            }

            var asset = ScriptableObject.CreateInstance<Tuning>();
            AssetDatabase.CreateAsset(asset, Path);
            AssetDatabase.SaveAssets();
            Debug.Log("[Tek Can] Tuning.asset oluşturuldu: " + Path);
        }
    }
}
