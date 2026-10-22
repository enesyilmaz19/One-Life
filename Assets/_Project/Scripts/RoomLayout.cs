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

        sealed class PincerLayout : RoomLayout
        {
            public override string Name => "KISKAÇ";
            public override Vector3 Position(Tuning t, int i, int n)
            {
                // İki kol, ağzı GİRİŞE açık (Faz 34): içine yürürsün, iki yandan kapanır. Kolun ucu girişe
                // yakın ve dışta, dibi çıkışa doğru ortada — ya bir kolu kırarsın ya ağızdan dalarsın.
                bool sağ = i % 2 == 0;
                int sıra = i / 2, perKol = Mathf.Max(1, Mathf.CeilToInt(n / 2f));
                float k = perKol <= 1 ? 0.5f : sıra / (float)(perKol - 1);
                return At(t, (sağ ? 1f : -1f) * Mathf.Lerp(0.55f, 0.15f, k), Mathf.Lerp(-0.15f, 0.40f, k));
            }
        }

        sealed class DoubleRowLayout : RoomLayout
        {
            public override string Name => "ÇİFT SIRA";
            public override Vector3 Position(Tuning t, int i, int n)
            {
                // İki hat (Faz 34): öndeki yolu keser, arkadaki boşluklarına kaydırılmış — birini yarınca
                // öbürü karşında. DUVAR'ın tek hattı tek kararı soruyordu.
                int ön = Mathf.CeilToInt(n / 2f);
                bool arka = i >= ön;
                int yuva = arka ? 2 * (i - ön) + 1 : 2 * i;              // ön çift yuvada, arka tek: hep iki öndekinin arasında
                int enBüyük = Mathf.Max(2 * (ön - 1), 2 * (n - ön) - 1);
                float x = enBüyük <= 0 ? 0f : yuva / (float)enBüyük - 0.5f;
                return At(t, x, arka ? 0.42f : 0.02f);
            }
        }

        sealed class CorridorLayout : RoomLayout
        {
            public override string Name => "KORİDOR";
            public override Vector3 Position(Tuning t, int i, int n)
            {
                // İki sıra yol boyu, aralarında dar geçit (Faz 35): ortadan koşarsan iki yandan gelirler —
                // ya bir sırayı silersin ya hızlı geçersin. KANAT'ın iki öbeği yan yana; bu bir tünel.
                bool sağ = i % 2 == 0;
                int sıra = i / 2, perSıra = Mathf.Max(1, Mathf.CeilToInt(n / 2f));
                float z = perSıra <= 1 ? 0.25f : Mathf.Lerp(-0.2f, 0.65f, sıra / (float)(perSıra - 1));
                return At(t, sağ ? 0.22f : -0.22f, z);
            }
        }

        sealed class ArrowLayout : RoomLayout
        {
            public override string Name => "OK";
            public override Vector3 Position(Tuning t, int i, int n)
            {
                // Ucu girişe bakan V (Faz 35) — KISKAÇ'ın tersi: önce ucu kırarsın, sonra iki kol iki yandan gelir.
                if (i == 0) return At(t, 0f, -0.1f);
                bool sağ = i % 2 == 1;
                int sıra = (i - 1) / 2, perKol = Mathf.Max(1, Mathf.CeilToInt((n - 1) / 2f));
                float k = (sıra + 1) / (float)perKol;
                return At(t, (sağ ? 1f : -1f) * 0.5f * k, Mathf.Lerp(-0.1f, 0.45f, k));
            }
        }

        sealed class NestedLayout : RoomLayout
        {
            public override string Name => "İÇ İÇE";
            public override Vector3 Position(Tuning t, int i, int n)
            {
                // İç halka + dış halka (Faz 36): dışı yarıp içe dalarsan iki yandan sarılırsın; HALKA'nın tek çemberi tek yarıktı.
                int iç = Mathf.Max(1, n / 3);
                bool içte = i < iç;
                int j = içte ? i : i - iç, m = içte ? iç : Mathf.Max(1, n - iç);
                float a = (360f / m) * j * Mathf.Deg2Rad + (içte ? 0f : Mathf.PI / m);
                float r = içte ? 0.18f : 0.5f;
                return At(t, Mathf.Cos(a) * r, Mathf.Sin(a) * r + 0.12f);
            }
        }

        sealed class ColumnLayout : RoomLayout
        {
            public override string Name => "SÜTUN";
            public override Vector3 Position(Tuning t, int i, int n)
            {
                // Yol boyu tek sıra (Faz 36): teker teker gelirler — koni bir tanesini alır, sıra seni yorar.
                float z = n <= 1 ? 0.2f : Mathf.Lerp(-0.15f, 0.6f, i / (float)(n - 1));
                return At(t, i % 2 == 0 ? 0.06f : -0.06f, z);
            }
        }

        sealed class CornersLayout : RoomLayout
        {
            public override string Name => "DÖRT KÖŞE";
            public override Vector3 Position(Tuning t, int i, int n)
            {
                // Dört öbek, ortası boş (Faz 37): ortadan geçersen dört yandan kapanırlar — AĞIRLIK'ta pahalı. Bir köşeyi al, kenardan ilerle.
                int köşe = i % 4, sıra = i / 4;
                float a = sıra * 2.39996f, r = sıra == 0 ? 0f : 0.08f;          // öbek içinde altın açı
                return At(t, (köşe % 2 == 0 ? 1f : -1f) * 0.45f + Mathf.Cos(a) * r, (köşe < 2 ? -0.05f : 0.5f) + Mathf.Sin(a) * r);
            }
        }

        sealed class CrossLayout : RoomLayout
        {
            public override string Name => "ÇAPRAZ";
            public override Vector3 Position(Tuning t, int i, int n)
            {
                // X (Faz 37): ortası dolu, dört kol çaprazlara — kesişime dalarsan dört kol birden gelir; bir kolu silip yandan geçersin.
                if (i == 0) return At(t, 0f, 0.2f);
                int kol = (i - 1) % 4, sıra = (i - 1) / 4, perKol = Mathf.Max(1, Mathf.CeilToInt((n - 1) / 4f));
                float k = 0.5f * (sıra + 1) / perKol;
                return At(t, (kol % 2 == 0 ? 1f : -1f) * k, 0.2f + (kol < 2 ? 1f : -1f) * k * 0.8f);
            }
        }

        public static readonly RoomLayout Ring = new RingLayout();
        public static readonly RoomLayout Wall = new WallLayout();
        public static readonly RoomLayout Cluster = new ClusterLayout();
        public static readonly RoomLayout Flank = new FlankLayout();
        public static readonly RoomLayout Scatter = new ScatterLayout();

        public static readonly RoomLayout Pincer = new PincerLayout();
        public static readonly RoomLayout DoubleRow = new DoubleRowLayout();
        public static readonly RoomLayout Corridor = new CorridorLayout();
        public static readonly RoomLayout Arrow = new ArrowLayout();
        public static readonly RoomLayout Nested = new NestedLayout();
        public static readonly RoomLayout Column = new ColumnLayout();
        public static readonly RoomLayout Corners = new CornersLayout();
        public static readonly RoomLayout Cross = new CrossLayout();

        public static readonly RoomLayout[] All = { Ring, Wall, Cluster, Flank, Scatter, Pincer, DoubleRow, Corridor, Arrow, Nested, Column, Corners, Cross };

        /// <summary>Oda numarasından yerleşim — deterministik, ama arka arkaya tekrar etmiyor.</summary>
        public static RoomLayout For(int roomIndex) => All[Mathf.Abs(roomIndex) % All.Length];
    }
}
