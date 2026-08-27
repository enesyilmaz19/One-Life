using System;
using System.IO;
using UnityEngine;

namespace TekCan
{
    /// <summary>Diske yazılan koşu durumu. Küçük tutuluyor — büyüdükçe bozulma riski artar.</summary>
    [Serializable]
    public class SaveData
    {
        public int checkpointRoom = 1;
        public float checkpointSeconds = -1f;      // -1 = henüz checkpoint yok, startSeconds kullan

        /// <summary>Sağlam eşya sayısı, `RevivalKind.All` sırasıyla.</summary>
        public int[] intact = new int[3];

        /// <summary>Kullanılmış (kırık) eşya — zamanla TAMİR edilebilir.</summary>
        public int[] broken = new int[3];

        public bool gameOver;
        public int deaths;
    }

    /// <summary>
    /// Kalıcı ölümün altyapısı.
    ///
    /// ⚠️⚠️ ÖLÜM ANINDA DİSKE YAZILIR. Yazılmazsa oyuncu ölmeden Alt+F4 yapar ve
    /// "tek can" diye bir şey kalmaz — kalıcı ölümlü oyunları bozan tuzak birebir budur.
    /// Bu yüzden `Flush()` gecikmesiz, coroutine'siz, UI'dan ÖNCE çağrılıyor.
    ///
    /// Sıra önemli: eşya DÜŞÜLÜR → DİSKE YAZILIR → sonra diriliş uygulanır.
    /// Ters sırada olsaydı, ölüm anında kapatan oyuncu eşyasını geri kazanırdı.
    /// </summary>
    public static class SaveFile
    {
        public static SaveData Current { get; private set; }

        /// <summary>
        /// Diskte gerçek bir kayıt var mıydı. ⚠️ Bu bayrak olmadan oyunu İLK KEZ açan
        /// oyuncu SIFIR diriliş eşyasıyla başlıyordu ve ilk ölümde "oyun bitti" görüyordu.
        /// Öz-denetimler hep `NewRun` çağırdığı için bu durumu hiç kurmamışlardı.
        /// </summary>
        public static bool LoadedFromDisk { get; private set; }

        /// <summary>Hiç oynanmamış ya da bitmiş koşu — yeni stok verilmeli.</summary>
        public static bool NeedsNewRun => Current == null || !LoadedFromDisk || Current.gameOver;

        static string Path => System.IO.Path.Combine(Application.persistentDataPath, "tekcan_run.json");

        public static void Load()
        {
            try
            {
                if (File.Exists(Path))
                {
                    Current = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path));
                    if (Current != null) { LoadedFromDisk = true; Normalize(); return; }
                }
            }
            catch (Exception e)
            {
                // Bozuk kayıt oyunu açılmaz yapmasın; yenisiyle devam.
                Debug.LogWarning($"[Tek Can] Kayıt okunamadı, yeni koşu: {e.Message}");
            }
            Current = new SaveData();
            LoadedFromDisk = false;
            Normalize();
        }

        /// <summary>⚠️ Diske HEMEN yazar. Ölüm anında gecikme kalıcı ölümü yok eder.</summary>
        public static void Flush()
        {
            if (Current == null) return;
            try { File.WriteAllText(Path, JsonUtility.ToJson(Current)); }
            catch (Exception e) { Debug.LogError($"[Tek Can] Kayıt YAZILAMADI: {e.Message}"); }
        }

        /// <summary>Yeni oyun — game over sonrası ya da elle sıfırlama.</summary>
        /// <summary>
        /// Checkpoint'e kaydedilmiş zaman — hiç checkpoint yoksa koşu başlangıcı.
        /// ⚠️ TEK KAYNAK: hem `RevivalKind` hem `RevivalSystem` bunu ayrı ayrı
        /// hesaplıyordu. Aynı kavramın iki ölçümü, `lessons.md` (9)'un tam konusu.
        /// </summary>
        public static float CheckpointSeconds(Tuning t)
            => Current != null && Current.checkpointSeconds > 0f
                ? Current.checkpointSeconds
                : t.startSeconds;

        public static void NewRun(Tuning t)
        {
            Current = new SaveData();
            LoadedFromDisk = true;      // artık gerçek bir koşu var
            Normalize();
            // ponytail: hikâye yok, eşyalar hikâyeden geliyor olacak. Test için başlangıç
            // stoğu Tuning'den; gerçek oyunda burası kalkacak ve eşyalar hikâyeden gelecek.
            Current.intact[0] = t.testDefianceCount;
            Current.intact[1] = t.testLastStandCount;
            Current.intact[2] = t.testRecallCount;
            Flush();
        }

        /// <summary>Dizi boyları `RevivalKind.All` ile uyumsuzsa (sürüm değişimi) düzelt.</summary>
        static void Normalize()
        {
            int n = RevivalKind.All.Length;
            Current.intact = Fit(Current.intact, n);
            Current.broken = Fit(Current.broken, n);
        }

        static int[] Fit(int[] src, int n)
        {
            var dst = new int[n];
            if (src != null)
                for (int i = 0; i < Mathf.Min(src.Length, n); i++) dst[i] = Mathf.Max(0, src[i]);
            return dst;
        }

        /// <summary>Öz-denetim için: diske dokunmadan bellek içi durum kur.</summary>
        public static void UseInMemory(SaveData data)
        {
            Current = data ?? new SaveData();
            LoadedFromDisk = true;      // öz-denetim durumu kendi kuruyor, taze sayılmaz
            Normalize();
        }
    }
}
