using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Odanın düşman yerleşimi. `Weapon` ve `EnemyKind` ile aynı desen:
    /// soyut sınıf + adlandırılmış varyantlar + `All` dizisi.
    ///
    /// ⚠️ Yerleşim, ODANIN TASARIMIDIR. Düşmanlar artık doğmuyor — girdiğinde
    /// zaten oradalar. Yani "onları nereye koydum" sorusu doğrudan "bu dövüş nasıl
    /// bir soru soruyor" demek:
    ///   • HALKA seni çevirir → nereden yaracaksın
    ///   • DUVAR yolu keser → dalacak mısın, dolaşacak mısın
    ///   • KANAT ikiye böler → hangi tarafı önce alacaksın
    ///
    /// Hepsi DETERMİNİSTİK (rastgele değil): aynı oda hep aynı yerleşimi verir.
    /// "Bu oda mı zor, ben mi kötü oynadım" sorusu cevaplanabilir kalıyor — aynı
    /// gerekçe `WaveManager.PickKind`'da da geçerliydi.
    /// </summary>
    public abstract class RoomLayout
    {
        public abstract string Name { get; }

        /// <summary>Düşman <paramref name="index"/> nereye konuluyor.</summary>
        public abstract Vector3 Position(Tuning t, int index, int count);

        /// <summary>
        /// Giriş ve çıkış zıt kenarlarda. Oda ARTIK BİR YOL: girdiğin yerden çıkacağın
        /// yere gitmen gerekiyor, o yüzden düşmanlar aradaki banda yerleşiyor.
        /// Tek arenada "her yer aynıydı" ve mekânın hiçbir anlamı yoktu.
        /// </summary>
        protected static float Band(Tuning t, float k) => t.arenaRadius * k;

        static Vector3 At(Tuning t, float x, float z) =>
            new Vector3(Band(t, x), Bootstrap.ArenaCenter.y, Band(t, z));

        // ── varyantlar ───────────────────────────────────────────────────────

        sealed class RingLayout : RoomLayout
        {
            public override string Name => "HALKA";
            public override Vector3 Position(Tuning t, int i, int n)
            {
                float a = (360f / Mathf.Max(1, n)) * i * Mathf.Deg2Rad;
                return At(t, Mathf.Cos(a) * 0.45f, Mathf.Sin(a) * 0.45f + 0.10f);
            }
        }

        sealed class WallLayout : RoomLayout
        {
            public override string Name => "DUVAR";
            public override Vector3 Position(Tuning t, int i, int n)
            {
                // Yolu kesen sıra. Tek düşmansa ortada dursun.
                float k = n <= 1 ? 0f : Mathf.Lerp(-0.55f, 0.55f, i / (float)(n - 1));
                return At(t, k, 0.18f);
            }
        }

        sealed class ClusterLayout : RoomLayout
        {
            public override string Name => "YUMAK";
            public override Vector3 Position(Tuning t, int i, int n)
            {
                // Sıkı bir düğüm: koni silahlar burada parlar, tek hedefli zorlanır.
                float a = (360f / Mathf.Max(1, n)) * i * Mathf.Deg2Rad;
                return At(t, Mathf.Cos(a) * 0.14f, Mathf.Sin(a) * 0.14f + 0.15f);
            }
        }

        sealed class FlankLayout : RoomLayout
        {
            public override string Name => "KANAT";
            public override Vector3 Position(Tuning t, int i, int n)
            {
                // İki grup: aradan sıvışabilir ya da bir tarafı temizleyebilirsin.
                bool sag = i % 2 == 0;
                int sira = i / 2;
                int perGrup = Mathf.Max(1, Mathf.CeilToInt(n / 2f));
                float z = perGrup <= 1 ? 0.1f : Mathf.Lerp(-0.15f, 0.35f, sira / (float)(perGrup - 1));
                return At(t, sag ? 0.50f : -0.50f, z);
            }
        }

        sealed class ScatterLayout : RoomLayout
        {
            public override string Name => "DAĞINIK";
            public override Vector3 Position(Tuning t, int i, int n)
            {
                // Deterministik "rastgelelik": altın oran açısı düzgün dağıtıyor
                // ve rastgele sayı üretecine ihtiyaç bırakmıyor.
                float a = i * 2.39996f;                       // ~137.5° — altın açı
                float r = Mathf.Sqrt((i + 0.5f) / Mathf.Max(1, n)) * 0.55f;
                return At(t, Mathf.Cos(a) * r, Mathf.Sin(a) * r + 0.12f);
            }
        }

        public static readonly RoomLayout Ring = new RingLayout();
        public static readonly RoomLayout Wall = new WallLayout();
        public static readonly RoomLayout Cluster = new ClusterLayout();
        public static readonly RoomLayout Flank = new FlankLayout();
        public static readonly RoomLayout Scatter = new ScatterLayout();

        public static readonly RoomLayout[] All = { Ring, Wall, Cluster, Flank, Scatter };

        /// <summary>Oda numarasından yerleşim — deterministik, ama arka arkaya tekrar etmiyor.</summary>
        public static RoomLayout For(int roomIndex) => All[Mathf.Abs(roomIndex) % All.Length];
    }
}
