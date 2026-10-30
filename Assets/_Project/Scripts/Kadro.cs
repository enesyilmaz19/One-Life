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
        public static int REHİN => İ<Rehin>();
        public static int İMZA => İ<CiftImza>();                 // Faz 43: ÇİFT İMZA, ÇEK'in yerine
        public static int KARŞILIK => İ<Karsilik>();
        public static int TAHVİL => İ<Tahvil>();
        public static int MÜŞTEREK => İ<Musterek>();
        public static int ARBİTRAJ => İ<Arbitraj>();
        public static int KİRA => İ<Kira>();

        /// <summary>Açılış sırası (Enes: "oyun başında 2 silah… boss öncesine kadar 4… iflas 2. bölgede").
        /// Yeni silahlar (Faz 31–32) buraya satır ekler.</summary>
        public static Açılış[] Tablo => new[]
        {
            new Açılış(KONİ, Yol.Başta),
            new Açılış(FAİZLİ, Yol.Başta),
            new Açılış(HACİZ, Yol.Sunak, 0, 1),     // 1. bölge, 2. ara bölgenin sunağında satın
            new Açılış(İPOTEK, Yol.Keşif, 0, 3),    // 1. bölge, 4. ara bölgede gizli köşe
            new Açılış(İFLAS, Yol.Boss, 0),         // 1. boss'tan
            new Açılış(REHİN, Yol.Sunak, 1, 2),     // 2. bölge, 3. ara bölgenin sunağında (Faz 31)
            new Açılış(İMZA, Yol.Boss, 1),          // 2. boss'tan (Faz 43'e dek ÇEK)
            new Açılış(KARŞILIK, Yol.Keşif, 2, 2),  // 3. bölge, 3. ara bölgede gizli köşe
            new Açılış(TAHVİL, Yol.Boss, 2),        // 3. boss'tan (Faz 32)
            new Açılış(MÜŞTEREK, Yol.Sunak, 3, 2),  // 4. bölge, 3. ara bölgenin sunağında
            new Açılış(ARBİTRAJ, Yol.Boss, 3),      // 4. boss'tan
            new Açılış(KİRA, Yol.Keşif, 4, 2),      // 5. bölge, 3. ara bölgede gizli köşe
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
    public enum ComboTür { HacizKesinleşti, Toplu, Temerrüt, HacizKararı, KarşıİCRA, SenetHaczi, SilipSüpür,
                           AçığaSatış, VadeFarkı, OrtakGider,
                           // Faz 33c — combo açan yükseltmeyle (EPİK) açılır
                           Sürü, Takip, Öncelik, Teminat, Konkordato, Kefalet, CiroHattı, Mukabele, Temettü,
                           Konsorsiyum, Spekülasyon, KiraGeliri,
                           // Faz 42 Parti 1 — direkt (açansız): 15 ikili + 5 üçlü
                           Cebriİcra, TopluBorç, AlacakDevri, Tapuİptali, AmmeAlacağı, İflasMasası, İpotekliRehin, Ciro,
                           KarşılıklıTahsil, VadeliHaciz, ZincirlemeCiro, MüşterekKusur, ArkadanDolanım, KiralıkArsa,
                           VadesiGelmişPozisyon, İcraTakibi, SenetliRehin, TeminatlıTahvil, OrtaklığınTasfiyesi, KiraKontratı,
                           // Faz 42 Parti 2 — açılır: ikilileri silahın EPİK, üçlüleri EFSANEVİ yükseltmesi açar
                           AcizVesikası, KonutKredisi, HacizMemuru, TapuTevhidi, İflasDairesi, MüşterekRehin, İbraz,
                           TeminatAkçesi, KiraSertifikası, Hacizİştiraki, Hileliİflas, KiraTahsilatı,
                           İstihkak, İpotekZinciri, İflasErtelemesi, ProtestoluSenet, VadeSonuTahsilatı, SpekülatifAtak,
                           // Faz 42 Parti 3 — satın alınır (ara bölge sunak odasının COMBO sunağında, saniyeyle)
                           TopluVade, KiracıSürüsü, FaizYükü, FaizliSenet, İhtiyatiTedbir, VadeliAlacak, İpotekliİflas,
                           ArsaSpekülasyonu, MalKaçırma, TasfiyeKulesi, RehinKarşılığı, İşgal, Senetİskontosu, KiraSenedi,
                           Karşılıksızİflas, KarşıHamle, VadeliTapu, TahvilOrtaklığı, OrtakBorç, İçİşlem, GizliHaciz, Kaçırma,
                           SüpürgeKulesi, KiraGetirisi, TopluİCRA, BileşikFaiz, ZincirlemeTahsil, KarşıRehin, OrtakKarşılık, ZamanKirası,
                           // Faz 42 Parti 4 — satın alınır, son parti (120 tamam)
                           TopluTahsilat, SürüdenAyır, BorçEsareti, FaizliKira, HacizOrtaklığı, KiraHaczi, TapuKredisi, TapuVadesi,
                           İflasİpoteği, İflasOrtaklığı, RehinliKredi, RehinVadesi, İmzalıKredi, Karşılıklıİmza, KarşıHaciz, KiraKarşılığı,
                           VadeliRehin, VadeliSenet, SenetOrtaklığı, OrtakVade, Gizliİpotek, GizliKule, KuleHaczi, KiracıRehin,
                           İflasSenedi, TapuKarşılığı, Arkadanİcra, SenetliKira, VadeliKarşılık, GizliOrtaklık }

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
            /// <summary>Faz 33c: bu yükseltme alınmadan combo kapalı (null = baştan açık).</summary>
            public SilahYükseltme? Açan;
            /// <summary>Faz 42 Parti 3: satın alınır — COMBO sunağında alınmadan kapalı.</summary>
            public bool Satın;
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
            // Faz 31
            new Tanım { Ad = "KARŞI İCRA", Sıra = () => new[] { Kadro.KARŞILIK, Kadro.FAİZLİ },
                        NeYapar = "KARŞILIK'la vur → FAİZLİ: vuruş ×2",
                        ArtıAd = "İCRA EMRİ", ArtıNeYapar = "FAİZLİ'nin sağ tık (İCRA) beklemesi sıfırlanır" },
            new Tanım { Ad = "SENET HACZİ", Sıra = () => new[] { Kadro.İMZA, Kadro.HACİZ },
                        NeYapar = "İMZA isabet etti → HACİZ: vuruş ×2",
                        ArtıAd = "SENET ZİNCİRİ", ArtıNeYapar = "HACİZ vuruşu yanındaki düşmana da seker" },
            new Tanım { Ad = "SİLİP SÜPÜR", Sıra = () => new[] { Kadro.REHİN, Kadro.KONİ },
                        NeYapar = "REHİN'le vur (fırlat · takas · sürükle; yakalamak sayılmaz) → KONİ: sert iter, vuruş ×1.5",
                        ArtıAd = "TOZ DUMAN", ArtıNeYapar = "KONİ vuruşu çevrendeki herkesi süpürür (360°)" },
            // Faz 32
            new Tanım { Ad = "AÇIĞA SATIŞ", Sıra = () => new[] { Kadro.REHİN, Kadro.ARBİTRAJ },
                        NeYapar = "REHİN'le vur → ARBİTRAJ: vuruş her yönden arkadan sayılır",
                        ArtıAd = "KALDIRAÇ", ArtıNeYapar = "o vuruş ×1.5 daha" },
            new Tanım { Ad = "VADE FARKI", Sıra = () => new[] { Kadro.TAHVİL, Kadro.FAİZLİ },
                        NeYapar = "TAHVİL'le vur → FAİZLİ: TAHVİL'in kademe çarpanıyla (en az ×1.5)",
                        ArtıAd = "KUPON", ArtıNeYapar = "FAİZLİ'nin savurma bedeli geri" },
            new Tanım { Ad = "ORTAK GİDER", Sıra = () => new[] { Kadro.MÜŞTEREK, Kadro.KİRA },
                        NeYapar = "MÜŞTEREK'le bağla → KİRA: kulenin kirasını bağlı düşmanlar öder",
                        ArtıAd = "AİDAT", ArtıNeYapar = "kule önce bağlılara ateş eder, ×1.5" },
            // Faz 33c — açan yükseltme EPİK; "üst enderliğin combo'su da güçlü" (Enes): eski combo'lardan sert. "+" hâlleri
            // Enes'in "evet"iyle (2026-10-19) — kural aynı: "+" sayı büyütmüyor, yeni bir etki ekliyor
            new Tanım { Ad = "SÜRÜ", Sıra = () => new[] { Kadro.KONİ, Kadro.MÜŞTEREK }, Açan = SilahYükseltme.Sürü,
                        NeYapar = "KONİ'yle vur → MÜŞTEREK: KONİ'nin vurduğu herkes (en çok 4) tek bağa",
                        ArtıAd = "AĞIL", ArtıNeYapar = "bağlananlar bağın ortasına çekilir" },
            new Tanım { Ad = "TAKİP", Sıra = () => new[] { Kadro.FAİZLİ, Kadro.ARBİTRAJ }, Açan = SilahYükseltme.Takip,
                        NeYapar = "FAİZLİ'yle vur → ARBİTRAJ: vuruş arkadan sayılır, hedef 2 sn sırtı dönük kalır",
                        ArtıAd = "PUSU", ArtıNeYapar = "sırtı dönükken yeni saldırı da başlatamaz" },
            new Tanım { Ad = "ÖNCELİK", Sıra = () => new[] { Kadro.HACİZ, Kadro.REHİN }, Açan = SilahYükseltme.Öncelik,
                        NeYapar = "HACİZ'le vur → REHİN: HACİZ'in vurduğunu nerede olursa olsun çekip yakalar",
                        ArtıAd = "İHTİYATİ HACİZ", ArtıNeYapar = "HACİZ'in sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "TEMİNAT", Sıra = () => new[] { Kadro.İPOTEK, Kadro.KİRA }, Açan = SilahYükseltme.Teminat,
                        NeYapar = "İPOTEK koy → KİRA: kule işaretin yerine kurulur, ilk 4 sn kira yok",
                        ArtıAd = "İPOTEKLİ KULE", ArtıNeYapar = "kule kurulurken işaret hemen patlar (×1.5)" },
            new Tanım { Ad = "KONKORDATO", Sıra = () => new[] { Kadro.İFLAS, Kadro.KARŞILIK }, Açan = SilahYükseltme.Konkordato,
                        NeYapar = "İFLAS'la vur → KARŞILIK: İFLAS vuruşunun yarısı birikene yazılır",
                        ArtıAd = "ERTELEME", ArtıNeYapar = "İFLAS'ın sağ tık (TASFİYE) beklemesi sıfırlanır" },
            new Tanım { Ad = "KEFALET", Sıra = () => new[] { Kadro.REHİN, Kadro.HACİZ }, Açan = SilahYükseltme.Kefalet,
                        NeYapar = "REHİN'le vur (fırlat · takas · sürükle) → HACİZ: ölü bölge yok, ×2",
                        ArtıAd = "AVAL", ArtıNeYapar = "HACİZ vuruşu yanındaki düşmana da seker" },
            new Tanım { Ad = "CİRO HATTI", Sıra = () => new[] { Kadro.İMZA, Kadro.İPOTEK }, Açan = SilahYükseltme.CiroHattı,
                        NeYapar = "İMZA isabet etti → İPOTEK: ateşin yolu boyunca üç küçük işaret daha",
                        ArtıAd = "HIZLI CİRO", ArtıNeYapar = "İMZA'nın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "MUKABELE", Sıra = () => new[] { Kadro.KARŞILIK, Kadro.İMZA }, Açan = SilahYükseltme.Mukabele,
                        NeYapar = "KARŞILIK'la vur → İMZA: serinin (1 sn) her mermisine son birikimin %25'i biner",
                        ArtıAd = "TEDBİR", ArtıNeYapar = "serinin vurdukları 1 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "TEMETTÜ", Sıra = () => new[] { Kadro.TAHVİL, Kadro.KONİ }, Açan = SilahYükseltme.Temettü,
                        NeYapar = "TAHVİL'le vur → KONİ: TAHVİL'in kademesi başına +30° ve +%25 hasar",
                        ArtıAd = "KÂR PAYI", ArtıNeYapar = "o vuruşun hasadı ×2" },
            new Tanım { Ad = "KONSORSİYUM", Sıra = () => new[] { Kadro.MÜŞTEREK, Kadro.İFLAS }, Açan = SilahYükseltme.Konsorsiyum,
                        NeYapar = "MÜŞTEREK'le vur → İFLAS: bağlıya inen hasar öbürlerine tam (%100) yayılır",
                        ArtıAd = "YAPILANDIRMA", ArtıNeYapar = "bağın süresi baştan başlar" },
            new Tanım { Ad = "SPEKÜLASYON", Sıra = () => new[] { Kadro.ARBİTRAJ, Kadro.TAHVİL }, Açan = SilahYükseltme.Spekülasyon,
                        NeYapar = "ARBİTRAJ'la ARKADAN vur → TAHVİL: basılı tutma 2. kademe dolu başlar",
                        ArtıAd = "VADELİ İŞLEM", ArtıNeYapar = "o basılı tutmada saat iki kat akmaz" },
            new Tanım { Ad = "KİRA GELİRİ", Sıra = () => new[] { Kadro.KİRA, Kadro.FAİZLİ }, Açan = SilahYükseltme.KiraGeliri,
                        NeYapar = "kule kur → FAİZLİ'yle vur: o kule durdukça FAİZLİ'nin savurma bedelini kule öder (savurma başı kulenin ömründen 1 sn)",
                        ArtıAd = "PEŞİN KİRA", ArtıNeYapar = "savurmalar kulenin ömründen de yemiyor" },
            // Faz 42 Parti 1 — direkt; Enes'in onayı 2026-10-25 (docs/COMBOLAR.md). "+" hâlleri öneri
            new Tanım { Ad = "CEBRİ İCRA", Sıra = () => new[] { Kadro.HACİZ, Kadro.KONİ },
                        NeYapar = "HACİZ'le vur → KONİ: HACİZ'in vurduğu önüne çekilir (boss çekilmez), koni ×1.5",
                        ArtıAd = "ZORLA GETİRME", ArtıNeYapar = "çekilen 1.5 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "TOPLU BORÇ", Sıra = () => new[] { Kadro.KONİ, Kadro.FAİZLİ },
                        NeYapar = "KONİ'yle vur → FAİZLİ: KONİ'nin vurduğu her düşman için +%15 (en çok +%75), savurma bedeli geri",
                        ArtıAd = "KEFİLLİ BORÇ", ArtıNeYapar = "FAİZLİ vuruşu yanındaki düşmana da seker" },
            new Tanım { Ad = "ALACAK DEVRİ", Sıra = () => new[] { Kadro.FAİZLİ, Kadro.HACİZ },
                        NeYapar = "FAİZLİ'yle vur → HACİZ: FAİZLİ'nin son vuruşu kadar borç HACİZ'in vurduğuna iner",
                        ArtıAd = "TEMLİK", ArtıNeYapar = "HACİZ'in vurduğu 2 sn sırtı dönük kalır" },
            new Tanım { Ad = "TAPU İPTALİ", Sıra = () => new[] { Kadro.İPOTEK, Kadro.KONİ },
                        NeYapar = "İPOTEK koy → KONİ'yle vur: işaret beklemeden patlar, koninin yayı +60°",
                        ArtıAd = "İSTİMLAK", ArtıNeYapar = "KONİ'nin sağ tık (SÜPÜRME) beklemesi sıfırlanır" },
            new Tanım { Ad = "AMME ALACAĞI", Sıra = () => new[] { Kadro.HACİZ, Kadro.İFLAS },
                        NeYapar = "HACİZ'le vur → İFLAS: vurduğun patlar, vuruşun yarısı 3 birim içindekilere",
                        ArtıAd = "GECİKME ZAMMI", ArtıNeYapar = "o vuruşun hasadı ×2" },
            new Tanım { Ad = "İFLAS MASASI", Sıra = () => new[] { Kadro.İFLAS, Kadro.KONİ },
                        NeYapar = "İFLAS'la vur → KONİ: İFLAS vuruşunun yarısı yaydaki herkese eklenir",
                        ArtıAd = "MASA PAYI", ArtıNeYapar = "o vuruşun hasadı ×2" },
            new Tanım { Ad = "İPOTEKLİ REHİN", Sıra = () => new[] { Kadro.REHİN, Kadro.İPOTEK },
                        NeYapar = "REHİN'i fırlat → İPOTEK: işaret rehinenin düştüğü yere, neredeyse beklemeden (gecikme ×0.1)",
                        ArtıAd = "İKİNCİ DERECE", ArtıNeYapar = "İPOTEK'in sağ tık (İPOTEK YAĞMURU) beklemesi sıfırlanır" },
            new Tanım { Ad = "CİRO", Sıra = () => new[] { Kadro.İMZA, Kadro.KONİ },
                        NeYapar = "İMZA isabet etti → KONİ: son merminin değdiği yerde de yay patlar (koninin %80'i, 2.5 birim)",
                        ArtıAd = "TAHSİL CİROSU", ArtıNeYapar = "patlamanın içindekiler 1 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "KARŞILIKLI TAHSİL", Sıra = () => new[] { Kadro.KARŞILIK, Kadro.KONİ },
                        NeYapar = "KARŞILIK'la vur → KONİ: KARŞILIK vuruşunun yarısı (birikim dahil) yaydaki herkese eklenir",
                        ArtıAd = "MAHSUP", ArtıNeYapar = "KARŞILIK'ın sağ tık (İTİRAZ) beklemesi sıfırlanır" },
            new Tanım { Ad = "VADELİ HACİZ", Sıra = () => new[] { Kadro.TAHVİL, Kadro.HACİZ },
                        NeYapar = "TAHVİL'i boz → HACİZ: menzil ×1.5, TAHVİL'in kademesi başına +%20",
                        ArtıAd = "HACİZ ŞERHİ", ArtıNeYapar = "vurulan 2 sn yarı hızda" },
            new Tanım { Ad = "ZİNCİRLEME CİRO", Sıra = () => new[] { Kadro.İMZA, Kadro.MÜŞTEREK },
                        NeYapar = "İMZA isabet etti → MÜŞTEREK: son merminin vurduğunu tek vuruşta MÜŞTEREK'in vurduğuna bağlar (aynıysa yanındakine)",
                        ArtıAd = "CİRO SİLSİLESİ", ArtıNeYapar = "bağa yanındaki üçüncü düşman da girer" },
            new Tanım { Ad = "MÜŞTEREK KUSUR", Sıra = () => new[] { Kadro.MÜŞTEREK, Kadro.KONİ },
                        NeYapar = "MÜŞTEREK'le bağla → KONİ: bağ kopar, bağlılara ve aralarındaki çizgide duranlara koni vuruşu bir kez daha iner",
                        ArtıAd = "RÜCU", ArtıNeYapar = "MÜŞTEREK'in sağ tık (ZİNCİRLEME) beklemesi sıfırlanır" },
            new Tanım { Ad = "ARKADAN DOLANIM", Sıra = () => new[] { Kadro.ARBİTRAJ, Kadro.KONİ },
                        NeYapar = "ARBİTRAJ'la ARKADAN vur → KONİ: menzil ×1.3, vuruş ×1.3",
                        ArtıAd = "KÖR NOKTA", ArtıNeYapar = "koninin vurdukları 2 sn sırtı dönük kalır" },
            new Tanım { Ad = "KİRALIK ARSA", Sıra = () => new[] { Kadro.KİRA, Kadro.İPOTEK },
                        NeYapar = "kule kur → İPOTEK: işaret kule ateşiyle hızlı patlar (gecikme ×0.3), ×1.5",
                        ArtıAd = "MUAFİYET", ArtıNeYapar = "kule 4 sn kira almaz" },
            new Tanım { Ad = "VADESİ GELMİŞ POZİSYON", Sıra = () => new[] { Kadro.TAHVİL, Kadro.ARBİTRAJ },
                        NeYapar = "TAHVİL'i 2. kademede ya da üstünde boz → ARBİTRAJ: vuruş arkadan sayılır, sonra 1.5 sn görünmezsin",
                        ArtıAd = "ZARAR KES", ArtıNeYapar = "ARBİTRAJ'ın sağ tık (AÇIK POZİSYON) beklemesi sıfırlanır" },
            new Tanım { Ad = "İCRA TAKİBİ", Sıra = () => new[] { Kadro.FAİZLİ, Kadro.HACİZ, Kadro.İFLAS }, MinBölge = 1,
                        NeYapar = "FAİZLİ → HACİZ → İFLAS: İFLAS saatin dörtte birindeymiş gibi sert, iki savurma bedeli geri",
                        ArtıAd = "TAZMİNAT", ArtıNeYapar = "İFLAS'ın sağ tık (TASFİYE) beklemesi sıfırlanır" },
            new Tanım { Ad = "SENETLİ REHİN", Sıra = () => new[] { Kadro.REHİN, Kadro.İMZA, Kadro.KONİ }, MinBölge = 1,
                        NeYapar = "REHİN'i fırlat → İMZA → KONİ: son merminin değdiği yerin 3 birim çevresindekiler önüne çekilir, koni ×1.5",
                        ArtıAd = "KAMBİYO", ArtıNeYapar = "koni 360°" },
            new Tanım { Ad = "TEMİNATLI TAHVİL", Sıra = () => new[] { Kadro.KARŞILIK, Kadro.TAHVİL, Kadro.FAİZLİ }, MinBölge = 1,
                        NeYapar = "KARŞILIK → TAHVİL → FAİZLİ: VADE FARKI'ndan ×1.25 sert, KARŞILIK'ın son birikimi de üstüne",
                        ArtıAd = "TEMİNAT İADESİ", ArtıNeYapar = "TAHVİL'in sağ tık (VADESİ GELMİŞ) beklemesi sıfırlanır" },
            new Tanım { Ad = "ORTAKLIĞIN TASFİYESİ", Sıra = () => new[] { Kadro.MÜŞTEREK, Kadro.İPOTEK, Kadro.KONİ }, MinBölge = 1,
                        NeYapar = "MÜŞTEREK'le bağla → İPOTEK → KONİ: işaret hemen patlar (×1.5), bağ kopar, bağlılara ve aradakilere koni vuruşu bir kez daha iner",
                        ArtıAd = "KAYYUM", ArtıNeYapar = "bağlılar 1.5 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "KİRA KONTRATI", Sıra = () => new[] { Kadro.KİRA, Kadro.ARBİTRAJ, Kadro.HACİZ }, MinBölge = 1,
                        NeYapar = "kule kur → ARBİTRAJ → HACİZ: ×1.5, kule HACİZ'in vurduğuna kilitlenir (menzil sınırı yok)",
                        ArtıAd = "İHTARNAME", ArtıNeYapar = "kilitlenen 2 sn yeni saldırı başlatamaz" },
            // Faz 42 Parti 2 — açılır (Enes 2026-10-27: "hem epik hem efsanevi açsın"): mevcut yükseltme hem kendi etkisini verir hem
            // combo'yu açar; ikilileri silahın EPİK'i, üçlüleri EFSANEVİ'si. "+" hâlleri öneri
            new Tanım { Ad = "ACİZ VESİKASI", Sıra = () => new[] { Kadro.KONİ, Kadro.İFLAS }, Açan = SilahYükseltme.Yorgunluk,
                        NeYapar = "KONİ'yle vur → İFLAS ×1.5; vurduğu yorgunsa (YORGUNLUK) saldırı hazırlığı baştan başlar",
                        ArtıAd = "MAL BEYANI", ArtıNeYapar = "vurulan 2 sn sırtı dönük" },
            new Tanım { Ad = "KONUT KREDİSİ", Sıra = () => new[] { Kadro.FAİZLİ, Kadro.İPOTEK }, Açan = SilahYükseltme.FaizsizKredi,
                        NeYapar = "FAİZLİ'yle vur → İPOTEK: işaret FAİZLİ'nin vurduğuna yapışır, onunla yürür",
                        ArtıAd = "ERKEN ÖDEME", ArtıNeYapar = "İPOTEK'in sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "HACİZ MEMURU", Sıra = () => new[] { Kadro.HACİZ, Kadro.ARBİTRAJ }, Açan = SilahYükseltme.İhtar,
                        NeYapar = "HACİZ'le vur → ARBİTRAJ: HACİZ'in vurduğunun arkasına geçersin, vuruş arkadan",
                        ArtıAd = "TEBLİĞ", ArtıNeYapar = "arkasına geçtiğin 1.5 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "TAPU TEVHİDİ", Sıra = () => new[] { Kadro.İPOTEK, Kadro.MÜŞTEREK }, Açan = SilahYükseltme.ÇiftTapu,
                        NeYapar = "İPOTEK koy → MÜŞTEREK: bekleyen ipoteklerin içindekiler (en çok 4) vurduğunla tek bağa",
                        ArtıAd = "TOPLULAŞTIRMA", ArtıNeYapar = "bağlananlar bağın ortasına çekilir" },
            new Tanım { Ad = "İFLAS DAİRESİ", Sıra = () => new[] { Kadro.İFLAS, Kadro.HACİZ }, Açan = SilahYükseltme.Kurtarma,
                        NeYapar = "İFLAS'la vur → HACİZ: İFLAS'ın çaresizlik çarpanıyla (saat azsa sert, en az ×1), ölü bölge yok",
                        ArtıAd = "İFLAS İLANI", ArtıNeYapar = "vurulan 2 sn yarı hızda" },
            new Tanım { Ad = "MÜŞTEREK REHİN", Sıra = () => new[] { Kadro.REHİN, Kadro.MÜŞTEREK }, Açan = SilahYükseltme.Domino,
                        NeYapar = "REHİN'i fırlat → MÜŞTEREK: rehine, çarptığı ve seken (DOMİNO) vurduğunla tek bağa",
                        ArtıAd = "TEMİNAT TAMAMLAMA", ArtıNeYapar = "bağlılar 1 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "İBRAZ", Sıra = () => new[] { Kadro.İMZA, Kadro.İFLAS }, Açan = SilahYükseltme.TopluİmzaArtı,
                        NeYapar = "İMZA isabet etti → İFLAS: İMZA'nın bu adımdaki her isabeti İFLAS'ın vurduğuna bir mermi daha (×1.5) olarak döner",
                        ArtıAd = "KAMBİYO TAKİBİ", ArtıNeYapar = "İFLAS'ın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "TEMİNAT AKÇESİ", Sıra = () => new[] { Kadro.KARŞILIK, Kadro.İPOTEK }, Açan = SilahYükseltme.Birikim,
                        NeYapar = "KARŞILIK'la vur → İPOTEK: işaret yarı gecikmeyle; KARŞILIK'ın son birikimi hasarına eklenir, alanı büyütür (birikim saniyesi başına +%25, en çok ×2)",
                        ArtıAd = "DEPO", ArtıNeYapar = "KARŞILIK'ın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "KİRA SERTİFİKASI", Sıra = () => new[] { Kadro.TAHVİL, Kadro.KİRA }, Açan = SilahYükseltme.İskonto,
                        NeYapar = "TAHVİL'i boz → KİRA: kule kademe × 2 sn kirasız başlar, atışları kademe çarpanıyla (en az ×1)",
                        ArtıAd = "ANAPARA İADESİ", ArtıNeYapar = "TAHVİL'in sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "HACZE İŞTİRAK", Sıra = () => new[] { Kadro.MÜŞTEREK, Kadro.HACİZ }, Açan = SilahYükseltme.GenişOrtaklık,
                        NeYapar = "MÜŞTEREK'le bağla → HACİZ: bağlıların her birine ayrı, tam HACİZ vuruşu da iner",
                        ArtıAd = "İŞTİRAK PAYI", ArtıNeYapar = "o vuruşların hasadı ×2" },
            new Tanım { Ad = "HİLELİ İFLAS", Sıra = () => new[] { Kadro.ARBİTRAJ, Kadro.İFLAS }, Açan = SilahYükseltme.Çiftİşlem,
                        NeYapar = "ARBİTRAJ'la ARKADAN vur → İFLAS: vurduğuna iki kez iner",
                        ArtıAd = "PERDE", ArtıNeYapar = "vuruştan sonra 1 sn görünmezsin" },
            new Tanım { Ad = "KİRA TAHSİLATI", Sıra = () => new[] { Kadro.KİRA, Kadro.İMZA }, Açan = SilahYükseltme.Stopaj,
                        NeYapar = "kule kur → İMZA: ilk mermide kulenin menzilindeki herkese birer mermi; öldürdükleri STOPAJ öder",
                        ArtıAd = "VERGİ İADESİ", ArtıNeYapar = "İMZA'nın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "İSTİHKAK", Sıra = () => new[] { Kadro.HACİZ, Kadro.KARŞILIK, Kadro.REHİN }, MinBölge = 1, Açan = SilahYükseltme.DarÖlüBölge,
                        NeYapar = "HACİZ → KARŞILIK → REHİN: HACİZ'in vurduğu nerede olursa olsun çekilip yakalanır, KARŞILIK'ın son birikimi ona iner",
                        ArtıAd = "İSTİRDAT", ArtıNeYapar = "REHİN'in sağ tık (TAKAS) beklemesi sıfırlanır" },
            new Tanım { Ad = "İPOTEK ZİNCİRİ", Sıra = () => new[] { Kadro.İPOTEK, Kadro.İMZA, Kadro.İFLAS }, MinBölge = 1, Açan = SilahYükseltme.ZincirTapu,
                        NeYapar = "İPOTEK → İMZA → İFLAS: İFLAS saatin yarısındaymış gibi; bekleyen işaretler hemen patlar, içindekilere de İFLAS iner",
                        ArtıAd = "TAHLİYE EMRİ", ArtıNeYapar = "patlamalar içindekileri dışa iter" },
            new Tanım { Ad = "İFLAS ERTELEMESİ", Sıra = () => new[] { Kadro.İFLAS, Kadro.TAHVİL, Kadro.HACİZ }, MinBölge = 1, Açan = SilahYükseltme.İflasZinciri,
                        NeYapar = "İFLAS → TAHVİL → HACİZ: HACİZ TAHVİL'in bir üst kademesinin çarpanıyla, ölü bölge yok",
                        ArtıAd = "TEDBİR KARARI", ArtıNeYapar = "vurulan 2 sn yarı hızda" },
            new Tanım { Ad = "PROTESTOLU SENET", Sıra = () => new[] { Kadro.İMZA, Kadro.ARBİTRAJ, Kadro.REHİN }, MinBölge = 1, Açan = SilahYükseltme.Protesto,
                        NeYapar = "İMZA → ARBİTRAJ → REHİN: İMZA'nın vurduğu nerede olursa olsun yakalanır, onu fırlatınca ×2",
                        ArtıAd = "ŞERH", ArtıNeYapar = "İMZA'nın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "VADE SONU TAHSİLATI", Sıra = () => new[] { Kadro.TAHVİL, Kadro.İFLAS, Kadro.KONİ }, MinBölge = 1, Açan = SilahYükseltme.VadeSonu,
                        NeYapar = "TAHVİL → İFLAS → KONİ: koni 360° ve TAHVİL'in kademe çarpanıyla; İFLAS'ın vurduğu yerde de patlar (İFLAS vuruşunun yarısı, 2.5 birim)",
                        ArtıAd = "TAHSİL HARCI", ArtıNeYapar = "o vuruşun hasadı ×2" },
            new Tanım { Ad = "SPEKÜLATİF ATAK", Sıra = () => new[] { Kadro.ARBİTRAJ, Kadro.MÜŞTEREK, Kadro.İMZA }, MinBölge = 1, Açan = SilahYükseltme.KısaPozisyon,
                        NeYapar = "ARBİTRAJ → MÜŞTEREK → İMZA: ilk mermide bağlıların her birine birer mermi 'arkadan' iner (ARBİTRAJ'ın arka çarpanıyla)",
                        ArtıAd = "KISA SATIŞ", ArtıNeYapar = "bağlılar 2 sn sırtı dönük" },
            // Faz 42 Parti 3 — satın alınır (Enes 2026-10-28: "onaylıyorum herşeyi"); "+" hâlleri öneri
            new Tanım { Ad = "TOPLU VADE", Sıra = () => new[] { Kadro.KONİ, Kadro.TAHVİL }, Satın = true,
                        NeYapar = "KONİ'yle vur → TAHVİL: KONİ'nin vurduğu her düşman olgunluğa +0.25 sn yazar (basılı tutma kısalır)",
                        ArtıAd = "ERKEN İTFA", ArtıNeYapar = "TAHVİL'in sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "KİRACI SÜRÜSÜ", Sıra = () => new[] { Kadro.KONİ, Kadro.KİRA }, Satın = true,
                        NeYapar = "KONİ'yle vur → kule kur: kule KONİ'nin vurduklarına öncelik verir, her biri kulenin ömrüne +1 sn",
                        ArtıAd = "KİRA İHTARI", ArtıNeYapar = "öncelikliler 1 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "FAİZ YÜKÜ", Sıra = () => new[] { Kadro.FAİZLİ, Kadro.KONİ }, Satın = true,
                        NeYapar = "FAİZLİ'yle vur → KONİ: bu odada her seferinde +%15 daha sert (en çok +%60; oda değişince baştan)",
                        ArtıAd = "FAİZE FAİZ", ArtıNeYapar = "o vuruşun hasadı ×2" },
            new Tanım { Ad = "FAİZLİ SENET", Sıra = () => new[] { Kadro.FAİZLİ, Kadro.İMZA }, Satın = true,
                        NeYapar = "FAİZLİ'yle vur → İMZA: serinin ilk iki isabeti FAİZLİ'nin savurma bedelini geri öder",
                        ArtıAd = "SENET İADESİ", ArtıNeYapar = "FAİZLİ'nin sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "İHTİYATİ TEDBİR", Sıra = () => new[] { Kadro.HACİZ, Kadro.İPOTEK }, Satın = true,
                        NeYapar = "HACİZ'le vur → İPOTEK: HACİZ'in vurduğu 2 sn yerine çakılır, işaret tam altına",
                        ArtıAd = "TEDBİR ŞERHİ", ArtıNeYapar = "çakılan 2 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "VADELİ ALACAK", Sıra = () => new[] { Kadro.HACİZ, Kadro.TAHVİL }, Satın = true,
                        NeYapar = "HACİZ'le vur → TAHVİL: basılı tuttukça HACİZ'in vurduğu yarı hızda",
                        ArtıAd = "ALACAK FAİZİ", ArtıNeYapar = "HACİZ'in sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "İPOTEKLİ İFLAS", Sıra = () => new[] { Kadro.İPOTEK, Kadro.İFLAS }, Satın = true,
                        NeYapar = "İPOTEK'in içinde düşman varken İFLAS: saatin dibindeymiş gibi, bekleyen işaretler hemen patlar",
                        ArtıAd = "İCRAİ SATIŞ", ArtıNeYapar = "patlamalar içindekileri dışa iter" },
            new Tanım { Ad = "ARSA SPEKÜLASYONU", Sıra = () => new[] { Kadro.İPOTEK, Kadro.ARBİTRAJ }, Satın = true,
                        NeYapar = "İPOTEK'in içinde düşman varken ARBİTRAJ: vuruş her yönden arkadan",
                        ArtıAd = "ARSA SPEKÜLATÖRÜ", ArtıNeYapar = "sonra 1 sn görünmezsin" },
            new Tanım { Ad = "MAL KAÇIRMA", Sıra = () => new[] { Kadro.İFLAS, Kadro.REHİN }, Satın = true,
                        NeYapar = "İFLAS'la vur → REHİN: vurduğunu çekip yakalarsın; 3 sn saat akmaz",
                        ArtıAd = "MAL GİZLEME", ArtıNeYapar = "İFLAS'ın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "TASFİYE KULESİ", Sıra = () => new[] { Kadro.İFLAS, Kadro.KİRA }, Satın = true,
                        NeYapar = "İFLAS'la vur → kule kur: atışlar İFLAS'ın çaresizlik çarpanıyla (saat azsa sert, en az ×1)",
                        ArtıAd = "TASFİYE PAYI", ArtıNeYapar = "kule öldürünce +1 sn (doymaya tabi)" },
            new Tanım { Ad = "REHİN KARŞILIĞI", Sıra = () => new[] { Kadro.REHİN, Kadro.KARŞILIK }, Satın = true,
                        NeYapar = "REHİN'i fırlat → KARŞILIK: fırlatmanın hasarı birikene yazılır, KARŞILIK onu indirir",
                        ArtıAd = "KARŞILIK İADESİ", ArtıNeYapar = "KARŞILIK'ın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "İŞGAL", Sıra = () => new[] { Kadro.REHİN, Kadro.KİRA }, Satın = true,
                        NeYapar = "REHİN'i fırlat → kule kur: kule rehinenin düştüğü yere, kirasını rehine öder",
                        ArtıAd = "TAHLİYE DAVASI", ArtıNeYapar = "kulenin ömrü +3 sn" },
            new Tanım { Ad = "SENET İSKONTOSU", Sıra = () => new[] { Kadro.İMZA, Kadro.TAHVİL }, Satın = true,
                        NeYapar = "İMZA isabet etti → TAHVİL: İMZA'nın bu adımdaki her isabeti olgunluğa +0.1 sn yazar (basılı tutma kısalır)",
                        ArtıAd = "İSKONTO ORANI", ArtıNeYapar = "İMZA'nın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "KİRA SENEDİ", Sıra = () => new[] { Kadro.İMZA, Kadro.KİRA }, Satın = true,
                        NeYapar = "İMZA isabet etti → kule kur: kule son merminin değdiği yere, ilk 3 sn kirasız",
                        ArtıAd = "SENETLE ÖDEME", ArtıNeYapar = "İMZA'nın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "KARŞILIKSIZ İFLAS", Sıra = () => new[] { Kadro.KARŞILIK, Kadro.İFLAS }, Satın = true,
                        NeYapar = "KARŞILIK'la vur → İFLAS: KARŞILIK'ın son birikiminin iki katı da iner",
                        ArtıAd = "SAHTE İFLAS", ArtıNeYapar = "vurulan 2 sn yarı hızda" },
            new Tanım { Ad = "KARŞI HAMLE", Sıra = () => new[] { Kadro.KARŞILIK, Kadro.ARBİTRAJ }, Satın = true,
                        NeYapar = "KARŞILIK'la vur → ARBİTRAJ: vurduğunun arkasına geçersin, vuruş arkadan",
                        ArtıAd = "KARŞI İHTAR", ArtıNeYapar = "arkasına geçtiğin 1.5 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "VADELİ TAPU", Sıra = () => new[] { Kadro.TAHVİL, Kadro.İPOTEK }, Satın = true,
                        NeYapar = "TAHVİL'i boz → İPOTEK: gecikme kademe başına −%25, hasar kademe çarpanıyla",
                        ArtıAd = "TAPU İSKONTOSU", ArtıNeYapar = "İPOTEK'in sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "TAHVİL ORTAKLIĞI", Sıra = () => new[] { Kadro.TAHVİL, Kadro.MÜŞTEREK }, Satın = true,
                        NeYapar = "TAHVİL'i boz → MÜŞTEREK: bağın süresi kademe × 2 sn uzar, yayılması kademe başına +%10",
                        ArtıAd = "ORTAKLIK PAYI", ArtıNeYapar = "bağlılar 1 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "ORTAK BORÇ", Sıra = () => new[] { Kadro.MÜŞTEREK, Kadro.FAİZLİ }, Satın = true,
                        NeYapar = "MÜŞTEREK'le bağla → FAİZLİ: savurma bedelini bağlılar öder (her birine bedel kadar vuruş, bedel sana geri)",
                        ArtıAd = "MÜŞTEREK BORÇLU", ArtıNeYapar = "bağlılar 2 sn yarı hızda" },
            new Tanım { Ad = "İÇ İŞLEM", Sıra = () => new[] { Kadro.MÜŞTEREK, Kadro.ARBİTRAJ }, Satın = true,
                        NeYapar = "MÜŞTEREK'le bağla → ARBİTRAJ: bağlıların hepsi 2 sn sırtını döner — ARBİTRAJ onlara arkadan vurur",
                        ArtıAd = "İÇERİDEN BİLGİ", ArtıNeYapar = "ARBİTRAJ'ın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "GİZLİ HACİZ", Sıra = () => new[] { Kadro.ARBİTRAJ, Kadro.HACİZ }, Satın = true,
                        NeYapar = "ARBİTRAJ'la ARKADAN vur → HACİZ: ölü bölge yok, ×1.3",
                        ArtıAd = "SESSİZ HACİZ", ArtıNeYapar = "sonra 1 sn görünmezsin" },
            new Tanım { Ad = "KAÇIRMA", Sıra = () => new[] { Kadro.ARBİTRAJ, Kadro.REHİN }, Satın = true,
                        NeYapar = "ARBİTRAJ'la ARKADAN vur → REHİN: vurduğunu çekip yakalarsın; 4 sn saat yarı akar",
                        ArtıAd = "FİDYE", ArtıNeYapar = "REHİN'in sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "SÜPÜRGE KULESİ", Sıra = () => new[] { Kadro.KİRA, Kadro.KONİ }, Satın = true,
                        NeYapar = "kule kur → KONİ: kulenin menzilindekilere de KONİ vuruşu iner",
                        ArtıAd = "KULE SÜPÜRMESİ", ArtıNeYapar = "kulenin ömrü +2 sn" },
            new Tanım { Ad = "KİRA GETİRİSİ", Sıra = () => new[] { Kadro.KİRA, Kadro.TAHVİL }, Satın = true,
                        NeYapar = "kule kur → TAHVİL: kule duruyorsa basılı tutarken saat iki kat akmaz",
                        ArtıAd = "GETİRİ PAYI", ArtıNeYapar = "TAHVİL'in sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "TOPLU İCRA", Sıra = () => new[] { Kadro.KONİ, Kadro.REHİN, Kadro.İFLAS }, MinBölge = 1, Satın = true,
                        NeYapar = "KONİ → REHİN → İFLAS: KONİ'nin vurduklarından havuzu en azına İFLAS'ın dip vuruşu da iner; öldürürse +2 sn (doymaya tabi)",
                        ArtıAd = "İCRA MASRAFI", ArtıNeYapar = "KONİ'nin sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "BİLEŞİK FAİZ", Sıra = () => new[] { Kadro.FAİZLİ, Kadro.TAHVİL, Kadro.MÜŞTEREK }, MinBölge = 1, Satın = true,
                        NeYapar = "FAİZLİ → TAHVİL → MÜŞTEREK: bağın yayılması TAHVİL'in kademe çarpanıyla büyür (en çok %100)",
                        ArtıAd = "FAİZ TAHAKKUKU", ArtıNeYapar = "bağlılar 2 sn yarı hızda" },
            new Tanım { Ad = "ZİNCİRLEME TAHSİL", Sıra = () => new[] { Kadro.HACİZ, Kadro.İMZA, Kadro.KİRA }, MinBölge = 1, Satın = true,
                        NeYapar = "HACİZ → İMZA → kule kur: kule HACİZ'in vurduğuna kilitli (menzilsiz); İMZA'nın her isabeti kulenin ömrüne +0.25 sn",
                        ArtıAd = "TAHSİL EMRİ", ArtıNeYapar = "kilitlenen 2 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "KARŞI REHİN", Sıra = () => new[] { Kadro.KARŞILIK, Kadro.REHİN, Kadro.ARBİTRAJ }, MinBölge = 1, Satın = true,
                        NeYapar = "KARŞILIK → REHİN → ARBİTRAJ: vuruş arkadan, KARŞILIK'ın son birikimi de üstüne",
                        ArtıAd = "REHİN AÇIĞI", ArtıNeYapar = "vurulan 2 sn sırtı dönük" },
            new Tanım { Ad = "ORTAK KARŞILIK", Sıra = () => new[] { Kadro.MÜŞTEREK, Kadro.KARŞILIK, Kadro.KONİ }, MinBölge = 1, Satın = true,
                        NeYapar = "MÜŞTEREK → KARŞILIK → KONİ: koni 360°, KARŞILIK'ın son vuruşu bağlıların her birine de iner",
                        ArtıAd = "KARŞILIKLI ORTAKLIK", ArtıNeYapar = "KARŞILIK'ın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "ZAMAN KİRASI", Sıra = () => new[] { Kadro.KİRA, Kadro.İFLAS, Kadro.İMZA }, MinBölge = 1, Satın = true,
                        NeYapar = "kule kur → İFLAS → İMZA: kule duruyorsa 3 sn saat akmaz, seri (1 sn) ×1.5",
                        ArtıAd = "ZAMAN AŞIMI", ArtıNeYapar = "kulenin ömrü +3 sn" },
            // Faz 42 Parti 4 — satın alınır, son parti (Enes 2026-10-29: "sonraki partiye geç"); "+" hâlleri öneri
            new Tanım { Ad = "TOPLU TAHSİLAT", Sıra = () => new[] { Kadro.KONİ, Kadro.İMZA }, Satın = true,
                        NeYapar = "KONİ'yle vur → İMZA: ilk mermide KONİ'nin vurduklarının her birine birer mermi (nişansız)",
                        ArtıAd = "TAHSİL MAKBUZU", ArtıNeYapar = "o mermilerin vurdukları 1 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "SÜRÜDEN AYIR", Sıra = () => new[] { Kadro.KONİ, Kadro.ARBİTRAJ }, Satın = true,
                        NeYapar = "KONİ'yle vur → ARBİTRAJ: KONİ'nin vurduklarından en uzaktakinin arkasına geçersin, vuruş arkadan",
                        ArtıAd = "AYRIŞMA", ArtıNeYapar = "KONİ'nin vurduğu öbürleri 2 sn sırtı dönük" },
            new Tanım { Ad = "BORÇ ESARETİ", Sıra = () => new[] { Kadro.FAİZLİ, Kadro.REHİN }, Satın = true,
                        NeYapar = "FAİZLİ'yle vur → REHİN: vurduğunu nerede olursa olsun çekip yakalarsın; tuttukça her sn havuzundan savurma bedeli kadar hasat edersin",
                        ArtıAd = "BORÇ SENEDİ", ArtıNeYapar = "FAİZLİ'nin sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "FAİZLİ KİRA", Sıra = () => new[] { Kadro.FAİZLİ, Kadro.KİRA }, Satın = true,
                        NeYapar = "FAİZLİ'yle vur → kule kur: 4 savurma bedeli ödersin, kule ×2 sert atar",
                        ArtıAd = "KİRA SÖZLEŞMESİ", ArtıNeYapar = "kulenin ömrü +3 sn" },
            new Tanım { Ad = "HACİZ ORTAKLIĞI", Sıra = () => new[] { Kadro.HACİZ, Kadro.MÜŞTEREK }, Satın = true,
                        NeYapar = "HACİZ'le vur → MÜŞTEREK: HACİZ'in vurduğu, menzil sınırı olmadan MÜŞTEREK'in vurduğuna bağlanır",
                        ArtıAd = "MÜŞTEREK HACİZ", ArtıNeYapar = "HACİZ'in sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "KİRA HACZİ", Sıra = () => new[] { Kadro.HACİZ, Kadro.KİRA }, Satın = true,
                        NeYapar = "HACİZ'le vur → kule kur: kule HACİZ'in vurduğunun yanına, ona önce ve ×1.5 ateş eder",
                        ArtıAd = "TAHLİYE İHTARI", ArtıNeYapar = "haczedilen 2 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "TAPU KREDİSİ", Sıra = () => new[] { Kadro.İPOTEK, Kadro.FAİZLİ }, Satın = true,
                        NeYapar = "İPOTEK'in içinde düşman varken FAİZLİ: ×2, savurma bedeli geri",
                        ArtıAd = "İPOTEK FEKKİ", ArtıNeYapar = "bekleyen işaretler hemen patlar" },
            new Tanım { Ad = "TAPU VADESİ", Sıra = () => new[] { Kadro.İPOTEK, Kadro.TAHVİL }, Satın = true,
                        NeYapar = "İPOTEK koy → TAHVİL: basılı tutma 1. kademe dolu başlar; işaret patlarsa +1 kademe",
                        ArtıAd = "TAPU TESCİLİ", ArtıNeYapar = "İPOTEK'in sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "İFLAS İPOTEĞİ", Sıra = () => new[] { Kadro.İFLAS, Kadro.İPOTEK }, Satın = true,
                        NeYapar = "İFLAS'la vur → İPOTEK: işaret İFLAS'ın vurduğunun altına, gecikme ×0.2, hasar İFLAS'ın çaresizlik çarpanıyla (en az ×1)",
                        ArtıAd = "MÜHLET", ArtıNeYapar = "İFLAS'ın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "İFLAS ORTAKLIĞI", Sıra = () => new[] { Kadro.İFLAS, Kadro.MÜŞTEREK }, Satın = true,
                        NeYapar = "İFLAS'la vur → MÜŞTEREK: İFLAS'ın vurduğu MÜŞTEREK'in vurduğuna bağlanır, yayılma İFLAS'ın çaresizlik çarpanıyla (en çok %100)",
                        ArtıAd = "ORTAK İFLAS", ArtıNeYapar = "bağlılar 2 sn yarı hızda" },
            new Tanım { Ad = "REHİNLİ KREDİ", Sıra = () => new[] { Kadro.REHİN, Kadro.FAİZLİ }, Satın = true,
                        NeYapar = "REHİN'le vur (fırlat · takas · sürükle) → FAİZLİ: ×1.5, savurma bedeli geri",
                        ArtıAd = "REHİN FEKKİ", ArtıNeYapar = "REHİN'in sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "REHİN VADESİ", Sıra = () => new[] { Kadro.REHİN, Kadro.TAHVİL }, Satın = true,
                        NeYapar = "REHİN'i fırlat → TAHVİL: rehine 3 sn yerine çakılı, basılı tutma 1. kademe dolu başlar",
                        ArtıAd = "HAPİS HAKKI", ArtıNeYapar = "çakılan 3 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "İMZALI KREDİ", Sıra = () => new[] { Kadro.İMZA, Kadro.FAİZLİ }, Satın = true,
                        NeYapar = "İMZA'yla vur → FAİZLİ: ×1.3, savurma bedeli geri",
                        ArtıAd = "KEFİL İMZASI", ArtıNeYapar = "vurulan 1 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "KARŞILIKLI İMZA", Sıra = () => new[] { Kadro.İMZA, Kadro.KARŞILIK }, Satın = true,
                        NeYapar = "İMZA isabet etti → KARŞILIK'la vur: İMZA'nın bu adımdaki her isabeti birikene 0.25 sn yazar (en çok 2 sn), KARŞILIK onu indirir",
                        ArtıAd = "KARŞILIK İMZASI", ArtıNeYapar = "İMZA'nın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "KARŞI HACİZ", Sıra = () => new[] { Kadro.KARŞILIK, Kadro.HACİZ }, Satın = true,
                        NeYapar = "KARŞILIK'la vur → HACİZ: KARŞILIK'ın son birikimi HACİZ'e biner, ölü bölge yok",
                        ArtıAd = "KARŞI TEBLİĞ", ArtıNeYapar = "vurulan 1.5 sn yeni saldırı başlatamaz" },
            new Tanım { Ad = "KİRA KARŞILIĞI", Sıra = () => new[] { Kadro.KARŞILIK, Kadro.KİRA }, Satın = true,
                        NeYapar = "KARŞILIK'la vur → kule kur: kule KARŞILIK'ın son birikimi kadar sn kirasız",
                        ArtıAd = "KİRA DEPOZİTOSU", ArtıNeYapar = "KARŞILIK'ın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "VADELİ REHİN", Sıra = () => new[] { Kadro.TAHVİL, Kadro.REHİN }, Satın = true,
                        NeYapar = "TAHVİL'i boz → REHİN: kademe kadar uzaktan yakalar (menzil ×kademe), o rehinenin fırlatması kademe çarpanıyla",
                        ArtıAd = "REHİN İTFASI", ArtıNeYapar = "TAHVİL'in sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "VADELİ SENET", Sıra = () => new[] { Kadro.TAHVİL, Kadro.İMZA }, Satın = true,
                        NeYapar = "TAHVİL'i boz → İMZA: seri (1 sn) TAHVİL'in kademe çarpanıyla (en az ×1), kademe başına +%25 hızlı",
                        ArtıAd = "KEŞİDE", ArtıNeYapar = "İMZA'nın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "SENET ORTAKLIĞI", Sıra = () => new[] { Kadro.MÜŞTEREK, Kadro.İMZA }, Satın = true,
                        NeYapar = "MÜŞTEREK'le bağla → İMZA: serinin (1 sn) mermileri bağlıya değince bağ tam (%100) yayar",
                        ArtıAd = "SENET YENİLEME", ArtıNeYapar = "bağın süresi tazelenir" },
            new Tanım { Ad = "ORTAK VADE", Sıra = () => new[] { Kadro.MÜŞTEREK, Kadro.TAHVİL }, Satın = true,
                        NeYapar = "MÜŞTEREK'le bağla → TAHVİL: bağ yaşadıkça basılı tutarken saat iki kat akmaz",
                        ArtıAd = "ORTAK KUPON", ArtıNeYapar = "bağlılar 2 sn yarı hızda" },
            new Tanım { Ad = "GİZLİ İPOTEK", Sıra = () => new[] { Kadro.ARBİTRAJ, Kadro.İPOTEK }, Satın = true,
                        NeYapar = "ARBİTRAJ'la ARKADAN vur → İPOTEK: işaret vurduğunun altına, 1.5 sn görünmezsin",
                        ArtıAd = "TAKİPLİ TAPU", ArtıNeYapar = "işaret hedefle birlikte gider" },
            new Tanım { Ad = "GİZLİ KULE", Sıra = () => new[] { Kadro.ARBİTRAJ, Kadro.KİRA }, Satın = true,
                        NeYapar = "ARBİTRAJ'la ARKADAN vur → kule kur: kulenin atışları arkadan (ARBİTRAJ'ın arka çarpanıyla)",
                        ArtıAd = "PERDE ARKASI", ArtıNeYapar = "kule kurulunca 1 sn görünmezsin" },
            new Tanım { Ad = "KULE HACZİ", Sıra = () => new[] { Kadro.KİRA, Kadro.HACİZ }, Satın = true,
                        NeYapar = "kule kur → HACİZ: kule duruyorsa ölü bölge yok, ×1.3",
                        ArtıAd = "ZORLA TAHLİYE", ArtıNeYapar = "vurulanlar kuleden dışa itilir" },
            new Tanım { Ad = "KİRACI REHİN", Sıra = () => new[] { Kadro.KİRA, Kadro.REHİN }, Satın = true,
                        NeYapar = "kule kur → REHİN: kulenin son vurduğunu nerede olursa olsun çekip yakalar",
                        ArtıAd = "KİRA REHNİ", ArtıNeYapar = "KİRA'nın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "İFLAS SENEDİ", Sıra = () => new[] { Kadro.REHİN, Kadro.İFLAS, Kadro.İMZA }, MinBölge = 1, Satın = true,
                        NeYapar = "REHİN → İFLAS → İMZA: seri (1 sn) İFLAS'ın çaresizlik çarpanıyla (en az ×1), her mermi bir kez yanındakine seker",
                        ArtıAd = "MASA ALACAĞI", ArtıNeYapar = "İFLAS'ın sağ tık beklemesi sıfırlanır" },
            new Tanım { Ad = "TAPU KARŞILIĞI", Sıra = () => new[] { Kadro.İPOTEK, Kadro.KARŞILIK, Kadro.KONİ }, MinBölge = 1, Satın = true,
                        NeYapar = "İPOTEK → KARŞILIK → KONİ: bekleyen işaretler hemen patlar, vurdukları kadar KONİ'ye eklenir",
                        ArtıAd = "TAPU TAHLİYESİ", ArtıNeYapar = "patlamalar içindekileri dışa iter" },
            new Tanım { Ad = "ARKADAN İCRA", Sıra = () => new[] { Kadro.İFLAS, Kadro.ARBİTRAJ, Kadro.HACİZ }, MinBölge = 1, Satın = true,
                        NeYapar = "İFLAS → ARBİTRAJ → HACİZ: HACİZ İFLAS'ın çaresizlik çarpanıyla (en az ×1) ve arkadan, ölü bölge yok",
                        ArtıAd = "İCRA PERDESİ", ArtıNeYapar = "sonra 1 sn görünmezsin" },
            new Tanım { Ad = "SENETLİ KİRA", Sıra = () => new[] { Kadro.İMZA, Kadro.REHİN, Kadro.KİRA }, MinBölge = 1, Satın = true,
                        NeYapar = "İMZA → REHİN'i fırlat → kule kur: kule rehinenin düştüğü yere, 5 sn kirasız, ömrü +5 sn",
                        ArtıAd = "SENETLİ KİRACI", ArtıNeYapar = "kirasız süre bitince kirayı rehine öder" },
            new Tanım { Ad = "VADELİ KARŞILIK", Sıra = () => new[] { Kadro.TAHVİL, Kadro.KARŞILIK, Kadro.İFLAS }, MinBölge = 1, Satın = true,
                        NeYapar = "TAHVİL → KARŞILIK → İFLAS: KARŞILIK'ın son birikimi TAHVİL'in kademe çarpanıyla (en az ×2) İFLAS'a iner",
                        ArtıAd = "KARŞILIK VADESİ", ArtıNeYapar = "vurulan 2 sn yarı hızda" },
            new Tanım { Ad = "GİZLİ ORTAKLIK", Sıra = () => new[] { Kadro.ARBİTRAJ, Kadro.İMZA, Kadro.MÜŞTEREK }, MinBölge = 1, Satın = true,
                        NeYapar = "ARBİTRAJ → İMZA → MÜŞTEREK: İMZA'nın bu adımda vurdukları (en çok 4) MÜŞTEREK'in vurduğuyla tek bağa, bağlılar 2 sn sırtı dönük",
                        ArtıAd = "GİZLİ PAY", ArtıNeYapar = "ARBİTRAJ'ın sağ tık beklemesi sıfırlanır" },
        };

        public static int Sayı => Tümü.Length;

        /// <summary>Faz 33c: bu yükseltmenin açtığı combo (yoksa null).</summary>
        public static Tanım Açtığı(SilahYükseltme y)
        {
            foreach (var t in Tümü) if (t.Açan == y) return t;
            return null;
        }
        public static Tanım Bu(ComboTür c) => Tümü[(int)c];

        /// <summary>Faz 42: bu ikili kullanılabilir bir üçlünün ilk iki adımı mı — öyleyse combo bitince zincir silinmez,
        /// üçlüye devam edilir (FAİZLİ → HACİZ = ALACAK DEVRİ, → İFLAS = İCRA TAKİBİ).</summary>
        public static bool ÜçlüyeDevam(ComboTür c, int bölge)
        {
            var s = Bu(c).Sıra();
            if (s.Length != 2) return false;
            for (int i = 0; i < Sayı; i++)
            {
                var u = Bu((ComboTür)i).Sıra();
                if (u.Length == 3 && u[0] == s[0] && u[1] == s[1] && Kullanılır((ComboTür)i, bölge)) return true;
            }
            return false;
        }

        /// <summary>Silahları açık ve bölgesi gelmiş mi (3'lü combo'lar 2. bölgeden — Enes).</summary>
        public static bool Kullanılır(ComboTür c, int bölge)
        {
            var t = Bu(c);
            if (bölge < t.MinBölge) return false;
            if (t.Açan.HasValue && !SilahSunağı.Var(t.Açan.Value)) return false;      // Faz 33c: açan yükseltme şart
            if (t.Satın && !Alındı(c)) return false;                                   // Faz 42 Parti 3: satın alınmış olmalı
            foreach (var w in t.Sıra()) if (w < 0 || !Kadro.Açık(w)) return false;
            return true;
        }

        // ── Faz 42 Parti 3: SATIN ALMA — ara bölge sunak odasının COMBO sunağı (Enes 2026-10-28, öneri a) ──
        public const int SatınTeklifSayısı = 2;

        public static bool Alındı(ComboTür c)
        {
            var s = SaveFile.Current;
            int i = (int)c;
            return s != null && s.comboBought != null && i < s.comboBought.Length && s.comboBought[i] > 0;
        }

        /// <summary>Bu sunaktan kaç combo alındı (sunak başı 1).</summary>
        public static int SatınGrupAlımı(int anahtar)
        {
            var s = SaveFile.Current;
            return s != null && s.comboOfferBought != null && anahtar >= 0 && anahtar < s.comboOfferBought.Length ? s.comboOfferBought[anahtar] : 0;
        }

        /// <summary>Satın alınan combo'yu kayda yaz — kaydın TEK yazma yolu (anahtar &lt; 0: sunak sayılmaz, öz-denetim/otopilot).</summary>
        public static void SatınAl(ComboTür c, int anahtar)
        {
            var s = SaveFile.Current;
            if (s == null) return;
            s.comboBought = SaveFile.Büyüt(s.comboBought, Sayı);
            s.comboBought[(int)c] = 1;
            if (anahtar < 0) return;
            s.comboOfferBought = SaveFile.Büyüt(s.comboOfferBought, anahtar + 1);
            s.comboOfferBought[anahtar]++;
        }

        /// <summary>
        /// COMBO sunağının teklifi: satın alınır, alınmamış, silahları açık, bölgesi gelmiş combo'lardan rastgele 2. Tohum koşu + sunak —
        /// odaya dönünce aynı teklif. Sunak teklifini ilk sorduğunda saklar (alım yapınca kalanı kaymasın).
        /// </summary>
        public static List<ComboTür> SatınTeklif(int anahtar, int bölge)
        {
            var aday = new List<ComboTür>();
            for (int i = 0; i < Sayı; i++)
            {
                var c = (ComboTür)i; var t = Bu(c);
                if (!t.Satın || Alındı(c) || bölge < t.MinBölge) continue;
                bool açık = true;
                foreach (var w in t.Sıra()) if (w < 0 || !Kadro.Açık(w)) açık = false;
                if (açık) aday.Add(c);
            }
            var r = new System.Random(unchecked(SilahSunağı.Tohum * 53 + anahtar * 6151));
            var seçilen = new List<ComboTür>();
            while (seçilen.Count < SatınTeklifSayısı && aday.Count > 0)
            {
                int i = r.Next(aday.Count);
                seçilen.Add(aday[i]);
                aday.RemoveAt(i);
            }
            return seçilen;
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


        /// <summary>Bu silah açılınca kullanılabilir olan combo'ların ADLARI (bölge şartı olanlar bölgesinde). Faz 42: silah başına
        /// 4–7 combo oldu, açıklamalı liste ekrandaki tek satıra sığmıyordu — açıklama combo yapılınca ve combo sunağında.</summary>
        public static string YeniAçılanlar(int silah)
        {
            int bölge = RoomManager.AktifBölge;
            var adlar = new List<string>();
            for (int i = 0; i < Sayı; i++)
            {
                var c = (ComboTür)i;
                if (System.Array.IndexOf(Bu(c).Sıra(), silah) >= 0 && Kullanılır(c, bölge)) adlar.Add(Bu(c).Ad);
            }
            return adlar.Count > 0 ? "   ·   COMBO AÇILDI: " + string.Join(" · ", adlar) : "";
        }

        /// <summary>Bölgeye girerken: bölge şartı burada dolan combo'ların tanıtımı (3'lü combo 2. bölgede).</summary>
        public static void BölgeTanıtımı(int bölge)
        {
            var adlar = new List<string>();                          // Faz 42: altı üçlü — tek tek duyurulunca yalnız sonuncusu görünüyordu
            for (int i = 0; i < Sayı; i++)
            {
                var c = (ComboTür)i;
                if (Bu(c).MinBölge == bölge && bölge > 0 && Kullanılır(c, bölge)) adlar.Add(Bu(c).Ad);
            }
            if (adlar.Count > 0) Duyur("3'LÜ COMBO AÇILDI: " + string.Join(" · ", adlar), 6f);
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
