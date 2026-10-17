using System.Collections.Generic;
using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Faz 30 — SİLAH KADROSU (Enes 2026-10-16/17). Bütün silahlar başta verilmiyor: oyun 2 silahla
    /// başlıyor, ilk boss'a kadar 4 oluyor; sonra her bölgede +2 (biri boss'tan, öbürü satın alma ya
    /// da keşif). Oyuncu yanına 3 silah alıyor (1–3) ve envanteri sunak odalarında KADRO sunağıyla
    /// düzenliyor. Açılan silah KOŞU BOYU kalıyor — ölüm geri almıyor (kayıtta `unlockedWeapons`).
    /// </summary>
    public static class Kadro
    {
        public const int Yuva = 3;
        public enum Yol { Başta, Sunak, Keşif, Boss }

        public readonly struct Açılış
        {
            public readonly int Silah;
            public readonly Yol Yol;
            public readonly int Bölge, AraBölge;
            public Açılış(int silah, Yol yol, int bölge = 0, int araBölge = 0)
            { Silah = silah; Yol = yol; Bölge = bölge; AraBölge = araBölge; }
        }

        static int İ<T>() where T : Weapon => System.Array.FindIndex(Weapon.All, w => w is T);
        public static int KONİ => İ<ConeBlade>();
        public static int FAİZLİ => İ<Faizli>();
        public static int HACİZ => İ<Haciz>();
        public static int İPOTEK => İ<Ipotek>();
        public static int İFLAS => İ<Iflas>();

        /// <summary>Açılış sırası (Enes: "oyun başında 2 silah… boss öncesine kadar 4… iflas 2. bölgede").
        /// Yeni silahlar (Faz 31–32) buraya satır ekler.</summary>
        public static Açılış[] Tablo => new[]
        {
            new Açılış(KONİ, Yol.Başta),
            new Açılış(FAİZLİ, Yol.Başta),
            new Açılış(HACİZ, Yol.Sunak, 0, 1),     // 1. bölge, 2. ara bölgenin sunağında satın
            new Açılış(İPOTEK, Yol.Keşif, 0, 3),    // 1. bölge, 4. ara bölgede gizli köşe
            new Açılış(İFLAS, Yol.Boss, 0),         // 1. boss'tan
        };

        /// <summary>Keşif silahı ara bölgenin kaçıncı dövüş odasında saklı (1'den).</summary>
        public const int KeşifOdası = 4;

        public static string SonMesaj { get; private set; }
        public static float SonMesajAnı { get; private set; } = -99f;

        static void Duyur(string m)
        {
            SonMesaj = m;
            SonMesajAnı = Time.unscaledTime;
            Debug.Log("[Tek Can] " + m);
        }

        /// <summary>Kaydı kadroya hazırla: eski kayıtta alan yok → başlangıç silahları, ilk yuvalar.</summary>
        static SaveData Hazır()
        {
            var s = SaveFile.Current;
            if (s == null) return null;
            int n = Weapon.All.Length;
            if (s.unlockedWeapons == null || s.unlockedWeapons.Length < n)
            {
                var yeni = new int[n];
                if (s.unlockedWeapons != null && s.unlockedWeapons.Length > 0)
                    System.Array.Copy(s.unlockedWeapons, yeni, s.unlockedWeapons.Length);
                else
                    foreach (var a in Tablo) if (a.Yol == Yol.Başta) yeni[a.Silah] = 1;
                s.unlockedWeapons = yeni;
            }
            if (s.loadout == null || s.loadout.Length != Yuva)
            {
                var l = new int[Yuva];
                for (int i = 0; i < Yuva; i++) l[i] = -1;
                int k = 0;
                for (int w = 0; w < n && k < Yuva; w++) if (s.unlockedWeapons[w] > 0) l[k++] = w;
                s.loadout = l;
            }
            return s;
        }

        /// <summary>Kayıt yoksa (bazı öz-denetimler) her silah açık sayılır — kadro kuralı kayda bağlı.</summary>
        public static bool Açık(int silah)
        {
            var s = Hazır();
            if (s == null) return true;
            return silah >= 0 && silah < s.unlockedWeapons.Length && s.unlockedWeapons[silah] > 0;
        }

        public static int AçıkSayısı
        {
            get { int n = 0; for (int w = 0; w < Weapon.All.Length; w++) if (Açık(w)) n++; return n; }
        }

        /// <summary>Bu yuvadaki silah (−1 boş).</summary>
        public static int Yuvadaki(int yuva)
        {
            var s = Hazır();
            if (yuva < 0 || yuva >= Yuva) return -1;
            if (s == null) return yuva < Weapon.All.Length ? yuva : -1;
            return s.loadout[yuva];
        }

        public static int Yuvası(int silah)
        {
            var s = Hazır();
            return s == null ? (silah < Yuva ? silah : -1) : System.Array.IndexOf(s.loadout, silah);
        }

        /// <summary>Silahı aç; boş yuva varsa oraya koy. Koşu boyu kalıcı, hemen yazılıyor.</summary>
        public static void Aç(int silah, string nasıl)
        {
            var s = Hazır();
            if (s == null || silah < 0 || silah >= Weapon.All.Length || Açık(silah)) return;
            s.unlockedWeapons[silah] = 1;
            int boş = System.Array.IndexOf(s.loadout, -1);
            if (boş >= 0) s.loadout[boş] = silah;
            SaveFile.Flush();
            Duyur($"YENİ SİLAH: {Weapon.All[silah].Name} ({nasıl})" +
                  (boş >= 0 ? $" — {boş + 1}. yuvada" : " — KADRO sunağından envantere al") +
                  Combo.YeniAçılanlar(silah));
        }

        /// <summary>KADRO sunağının adayı: bu yuvaya gelecek, envanterde olmayan bir sonraki açık silah.</summary>
        public static int Sıradaki(int yuva)
        {
            var s = Hazır();
            if (s == null || yuva < 0 || yuva >= Yuva) return -1;
            int n = Weapon.All.Length, şimdiki = s.loadout[yuva];
            for (int k = 1; k <= n; k++)
            {
                int w = ((şimdiki < 0 ? -1 : şimdiki) + k + n) % n;
                if (Açık(w) && System.Array.IndexOf(s.loadout, w) < 0) return w;
            }
            return -1;
        }

        /// <summary>KADRO sunağı: yuvadaki silahı sıradakiyle değiştir. Yeni silahı döndürür (yoksa −1).</summary>
        public static int Değiştir(int yuva)
        {
            int aday = Sıradaki(yuva);
            if (aday < 0) return -1;
            YuvayaKoy(yuva, aday);
            return aday;
        }

        /// <summary>Açık bir silahı yuvaya koy (KADRO sunağı ve otopilot). Başka yuvadaysa yer değiştirir.</summary>
        public static void YuvayaKoy(int yuva, int silah)
        {
            var s = Hazır();
            if (s == null || yuva < 0 || yuva >= Yuva || !Açık(silah)) return;
            int eski = System.Array.IndexOf(s.loadout, silah);
            if (eski >= 0) s.loadout[eski] = s.loadout[yuva];
            s.loadout[yuva] = silah;
            SaveFile.Flush();
        }

        /// <summary>Ana boss düştü: o bölgenin boss silahı açılıyor.</summary>
        public static void BossAçılışı(int bölge)
        {
            foreach (var a in Tablo)
                if (a.Yol == Yol.Boss && a.Bölge == bölge) Aç(a.Silah, $"{bölge + 1}. boss'tan");
        }

        static bool ÖnceYaDaŞimdi(int b, int s, int bölge, int araBölge) => b < bölge || (b == bölge && s <= araBölge);

        /// <summary>
        /// Bu sunak odasında satılan silahlar: sırası gelmiş SATIN ALMA silahları ve KAÇIRILMIŞ keşif
        /// silahları (ara bölgesi geçmiş, bulunmamış — pahalıya). Hiçbir silah sonsuza dek kaçmıyor.
        /// </summary>
        public static List<int> Satılıklar(int bölge, int araBölge)
        {
            var l = new List<int>();
            foreach (var a in Tablo)
            {
                if (Açık(a.Silah)) continue;
                if (a.Yol == Yol.Sunak && ÖnceYaDaŞimdi(a.Bölge, a.AraBölge, bölge, araBölge)) l.Add(a.Silah);
                if (a.Yol == Yol.Keşif && ÖnceYaDaŞimdi(a.Bölge, a.AraBölge, bölge, araBölge)) l.Add(a.Silah);
            }
            return l;
        }

        /// <summary>Keşif silahını sunakta almak "kaçırılmış" demek — fiyatı iki kat.</summary>
        public static bool Keşifti(int silah)
        {
            foreach (var a in Tablo) if (a.Silah == silah) return a.Yol == Yol.Keşif;
            return false;
        }

        /// <summary>Bu dövüş odasında gizli silah var mı (−1 yok).</summary>
        public static int KeşifBurada(int bölge, int araBölge, int araOda)
        {
            if (araOda != KeşifOdası) return -1;
            foreach (var a in Tablo)
                if (a.Yol == Yol.Keşif && a.Bölge == bölge && a.AraBölge == araBölge && !Açık(a.Silah)) return a.Silah;
            return -1;
        }
    }

    /// <summary>Faz 30 — COMBO türleri. Sıra `Combo.Tümü` ile aynı.</summary>
    public enum ComboTür { HacizKesinleşti, Toplu, Temerrüt, HacizKararı }

    /// <summary>
    /// Faz 30 — COMBO (Enes: "hızlı silah geçişleri ile combo saldırılar… bazı silahlarla 3'lü
    /// combolar… combo abuselanmamalı ve combolar gelişip yeni etki de kazanabilmeli").
    /// Kural: A ile İSABET → `comboWindow` içinde B'ye geç ve vur (3'lüde bir adım daha). Aynı combo
    /// `comboCooldown` boyunca tekrar etmiyor, aynı silah iki kez sayılmıyor. Her combo'nun bir
    /// "+" hâli var (silah sunağında): yeni bir ETKİ ekliyor, sayı büyütmüyor. Zincir `PlayerController`'da.
    /// </summary>
    public static class Combo
    {
        public sealed class Tanım
        {
            public string Ad, NeYapar, ArtıAd, ArtıNeYapar;
            public System.Func<int[]> Sıra;
            public int MinBölge;
        }

        public static readonly Tanım[] Tümü =
        {
            new Tanım { Ad = "HACİZ KESİNLEŞTİ", Sıra = () => new[] { Kadro.İPOTEK, Kadro.HACİZ },
                        NeYapar = "İPOTEK koy → HACİZ'le işaretin içindekine vur: işaret HEMEN patlar (×1.5)",
                        ArtıAd = "KESİN HÜKÜM", ArtıNeYapar = "HACİZ'in sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "TOPLU İPOTEK", Sıra = () => new[] { Kadro.KONİ, Kadro.İPOTEK },
                        NeYapar = "KONİ'yle vur → İPOTEK: topladıklarının ortasına hızlı ipotek",
                        ArtıAd = "ÇİFT HACİZ", ArtıNeYapar = "en uzaktaki düşmana ikinci ipotek" },
            new Tanım { Ad = "TEMERRÜT", Sıra = () => new[] { Kadro.FAİZLİ, Kadro.İFLAS },
                        NeYapar = "FAİZLİ'yle vur → İFLAS: saatin yarıdaymış gibi sert",
                        ArtıAd = "TEMERRÜT FAİZİ", ArtıNeYapar = "o vuruşun hasadı ×2" },
            new Tanım { Ad = "HACİZ KARARI", Sıra = () => new[] { Kadro.KONİ, Kadro.HACİZ, Kadro.İPOTEK }, MinBölge = 1,
                        NeYapar = "KONİ → HACİZ → İPOTEK: HACİZ'in hedefine büyük, hızlı ipotek (×2)",
                        ArtıAd = "TAHLİYE", ArtıNeYapar = "patlama düşmanları dışa iter" },
        };

        public static int Sayı => Tümü.Length;
        public static Tanım Bu(ComboTür c) => Tümü[(int)c];

        /// <summary>Silahları açık ve bölgesi gelmiş mi (3'lü combo'lar 2. bölgeden — Enes).</summary>
        public static bool Kullanılır(ComboTür c, int bölge)
        {
            var t = Bu(c);
            if (bölge < t.MinBölge) return false;
            foreach (var w in t.Sıra()) if (w < 0 || !Kadro.Açık(w)) return false;
            return true;
        }

        public static bool Artı(ComboTür c)
        {
            var s = SaveFile.Current;
            int i = (int)c;
            return s != null && s.comboUpgrades != null && i < s.comboUpgrades.Length && s.comboUpgrades[i] > 0;
        }

        public static int BölgeAlımı(int bölge)
        {
            var s = SaveFile.Current;
            return s != null && s.comboAltarBought != null && bölge < s.comboAltarBought.Length ? s.comboAltarBought[bölge] : 0;
        }

        /// <summary>"+" hâlini kayda yaz (silah sunağının combo sunağı; boss başına bir).</summary>
        public static void ArtıAl(ComboTür c, int bölge)
        {
            var s = SaveFile.Current;
            if (s == null) return;
            s.comboUpgrades = SaveFile.Büyüt(s.comboUpgrades, Sayı);
            s.comboAltarBought = SaveFile.Büyüt(s.comboAltarBought, bölge + 1);
            s.comboUpgrades[(int)c] = 1;
            s.comboAltarBought[bölge]++;
        }

        /// <summary>Silah sunağında satılacak combo: kullanılabilir ama "+" hâli alınmamış ilk combo (yoksa null).</summary>
        public static ComboTür? Satılık(int bölge)
        {
            for (int i = 0; i < Sayı; i++)
                if (Kullanılır((ComboTür)i, bölge) && !Artı((ComboTür)i)) return (ComboTür)i;
            return null;
        }


        /// <summary>Bu silah açılınca kullanılabilir olan combo'ların tanıtımı (bölge şartı olanlar bölgesinde).</summary>
        public static string YeniAçılanlar(int silah)
        {
            int bölge = RoomManager.AktifBölge;
            string m = "";
            for (int i = 0; i < Sayı; i++)
            {
                var c = (ComboTür)i;
                if (System.Array.IndexOf(Bu(c).Sıra(), silah) >= 0 && Kullanılır(c, bölge))
                    m += $"   ·   COMBO AÇILDI: {Bu(c).Ad} — {Bu(c).NeYapar}";
            }
            return m;
        }

        /// <summary>Bölgeye girerken: bölge şartı burada dolan combo'ların tanıtımı (3'lü combo 2. bölgede).</summary>
        public static void BölgeTanıtımı(int bölge)
        {
            for (int i = 0; i < Sayı; i++)
            {
                var c = (ComboTür)i;
                if (Bu(c).MinBölge == bölge && bölge > 0 && Kullanılır(c, bölge))
                    Duyur($"3'LÜ COMBO AÇILDI: {Bu(c).Ad} — {Bu(c).NeYapar}", 6f);
            }
        }

        public static string SonCombo { get; private set; }
        public static float SonComboAnı { get; private set; } = -99f;
        public static float SonComboSüresi { get; private set; } = 1.5f;

        public static void Duyur(string m, float süre)
        {
            SonCombo = m;
            SonComboAnı = Time.unscaledTime;
            SonComboSüresi = süre;
            Debug.Log("[Tek Can] " + m);
        }
    }
}
