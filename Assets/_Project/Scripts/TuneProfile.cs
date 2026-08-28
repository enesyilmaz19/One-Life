using System;
using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// A/B ayar profilleri — **his karşılaştırmasını mümkün kılan araç.**
    ///
    /// ⚠️ Neden var: bir sayıyı değiştirip Play'e basmak, sonra eskisini HAFIZADAN
    /// karşılaştırmak demek. His hafızası saniyeler içinde bozuluyor; 0.30 ile 0.26
    /// arasındaki farkı iki ayrı oturumda ayırt etmek imkânsız. Bu araç ikisini
    /// AYNI DALGADA, bir saniye arayla yan yana koyuyor.
    ///
    /// ⚠️ TEST ARACI — gerçek oyunda kalkacak (ENTER, B, N gibi).
    ///
    /// Profiller `Tuning`'in ÇALIŞMA KOPYASINI değiştiriyor (bkz. `Bootstrap.Build`),
    /// asset'e dokunulmuyor. Yoksa bir profil denemesi Inspector'daki bütün his
    /// değerlerini kalıcı olarak ezerdi.
    ///
    /// ### Şu anki profiller neden bunlar
    /// `Zorluk eğrisi raporu` iki suçlu ölçtü:
    ///   ① BOSS ODASI — bölge başına −23 sn (kazanç 28, masraf 51; 21'i aura)
    ///   ② YÜRÜME — oda başına 9.6 sn, başlangıç saatinin %16'sı
    /// Profiller tam olarak bu iki kadranı çeviriyor.
    /// </summary>
    public class TuneProfile
    {
        public string Name;
        public string Note;

        /// <summary>Çalışma kopyasına uygulanan değişiklik. Taban değerler `baz`da.</summary>
        public Action<Tuning, Tuning> Apply;

        /// <summary>Odayı yeniden kurmadan etkisi görünmez mi (geometri değişiyorsa evet).</summary>
        public bool NeedsNewRoom;

        public static readonly TuneProfile[] All =
        {
            new TuneProfile
            {
                Name = "MEVCUT",
                Note = "değişiklik yok — karşılaştırma tabanı",
                Apply = (t, baz) => { }
            },
            new TuneProfile
            {
                Name = "BOSS BESLİYOR",
                Note = "boss hasadı ×2, aurası yarı",
                Apply = (t, baz) =>
                {
                    // Ölçüm: boss masrafının 21 sn'si AURA, kazancı yalnızca 28.
                    // Bu profil boss'u zaman kuyusu olmaktan çıkarıyor — ama yenmesi
                    // hâlâ uzun. Soru: boss ödül mü olmalı, bedel mi?
                    t.bossHarvestMultiplier = baz.bossHarvestMultiplier * 2f;
                    t.bossAuraDrain = baz.bossAuraDrain * 0.5f;
                }
            },
            new TuneProfile
            {
                Name = "ODA KÜÇÜK",
                Note = "arena yarıçapı ×0.68 — yürüme bedeli düşüyor",
                NeedsNewRoom = true,
                Apply = (t, baz) => { t.arenaRadius = baz.arenaRadius * 0.68f; }
            },
            new TuneProfile
            {
                Name = "İKİSİ",
                Note = "boss besliyor + oda küçük",
                NeedsNewRoom = true,
                Apply = (t, baz) =>
                {
                    t.bossHarvestMultiplier = baz.bossHarvestMultiplier * 2f;
                    t.bossAuraDrain = baz.bossAuraDrain * 0.5f;
                    t.arenaRadius = baz.arenaRadius * 0.68f;
                }
            },
        };

        /// <summary>
        /// ⚠️ `Clock.Init` bu alanları KOPYALIYOR — koşu başında bir kez okunuyor,
        /// sonra `Clock` kendi kopyasını kullanıyor (sunak yükseltmeleri oraya biniyor).
        /// Yani bir profil bunları değiştirse CANLI HİÇBİR ETKİSİ OLMAZ ve "profil
        /// çalışmıyor" diye saatlerce aranır.
        ///
        /// Bunları ayarlamak gerekirse `Clock`'a canlı tazeleme eklenmeli — o zaman
        /// da sunaktan alınan yükseltmelerin korunması gerekir. Ayrı bir iş.
        /// `Faz11SelfCheck` hiçbir profilin bunlara dokunmadığını doğruluyor.
        /// </summary>
        public static readonly string[] ClockCopiedFields =
            { "harvestRate", "attackDamage", "dashCooldown" };

        public static TuneProfile Current { get; private set; }
        public static int CurrentIndex { get; private set; }

        /// <summary>Profil değişince HUD parlasın diye.</summary>
        public static float ChangedAt { get; private set; } = -99f;

        static Tuning _baz;

        /// <summary>Gerçek `Tuning.asset` — Inspector'ın yazdığı yer.</summary>
        static Tuning _asset;

        // Yansıma alan listesi bir kez — her karede yeniden çıkarmak boşuna.
        static System.Reflection.FieldInfo[] _fields;
        static System.Reflection.FieldInfo[] Fields => _fields ??= typeof(Tuning).GetFields(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        public static void Reset()
        {
            // ⚠️ Taban bir ScriptableObject KLONU — sahneye bağlı değil, yani `Destroy`
            // edilmezse her yeniden başlatmada bir tane daha birikiyor.
            if (_baz != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_baz);
                else UnityEngine.Object.DestroyImmediate(_baz);
            }
            Current = null;
            CurrentIndex = 0;
            _baz = null;
            ChangedAt = -99f;
        }

        /// <summary>
        /// Taban değerleri saklar. ⚠️ Profiller ÜST ÜSTE BİNMEMELİ: her uygulama
        /// tabandan başlıyor, yoksa iki kez "×2" yapan bir profil ×4 verirdi ve
        /// karşılaştırma anlamsızlaşırdı.
        /// </summary>
        public static void Init(Tuning asset, Tuning çalışma)
        {
            _asset = asset;
            _baz = UnityEngine.Object.Instantiate(çalışma);
            _baz.name = "Tuning (A/B tabanı)";
            Select(0, çalışma);
        }

        /// <summary>
        /// ⚠️ MEVCUT profilindeyken INSPECTOR DEĞİŞİKLİKLERİNİ İÇERİ ALIR.
        ///
        /// Çalışma kopyası asset'i profillerden korumak için var — ama o koruma
        /// Enes'in GÜNLÜK AYAR YÖNTEMİNİ kırmıştı: Inspector'da bir değeri çevirmek
        /// artık oyuna geçmiyordu. (`CLAUDE.md`: "His/denge değeri değiştireceksen →
        /// Inspector.") A/B aracı geçici, Inspector kalıcı; kalıcı olan kazanır.
        ///
        /// Yalnızca MEVCUT'tayken: bir profil seçiliyken kopya BİLEREK ayrışıyor,
        /// oraya asset'i basmak profili anında silerdi.
        /// Taban da tazeleniyor ki F2'ye bastığında profil GÜNCEL değerlere uygulansın.
        /// </summary>
        public static void PullFromAssetIfBaseline(Tuning çalışma)
        {
            if (CurrentIndex != 0 || _asset == null || çalışma == null || _baz == null) return;
            CopyFrom(_asset, _baz);
            CopyFrom(_asset, çalışma);
        }

        /// <summary>Öz-denetim için: Inspector köprüsü kurulu mu.</summary>
        public static bool AssetLinked => _asset != null;

        public static void Select(int index, Tuning çalışma)
        {
            if (çalışma == null || _baz == null || All.Length == 0) return;

            CurrentIndex = Mathf.Clamp(index, 0, All.Length - 1);
            Current = All[CurrentIndex];

            // Her seferinde TABANDAN: profiller birikmiyor.
            CopyFrom(_baz, çalışma);
            Current.Apply(çalışma, _baz);

            ChangedAt = Time.unscaledTime;
            Debug.Log($"[Tek Can] AYAR PROFİLİ → {Current.Name} ({Current.Note})");
        }

        /// <summary>
        /// Taban → çalışma kopyası. Yansımayla, çünkü elle alan listesi tutmak
        /// bir sonraki yeni ayar alanında SESSİZCE eksik kalırdı.
        /// </summary>
        static void CopyFrom(Tuning kaynak, Tuning hedef)
        {
            foreach (var f in Fields) f.SetValue(hedef, f.GetValue(kaynak));
        }
    }
}
