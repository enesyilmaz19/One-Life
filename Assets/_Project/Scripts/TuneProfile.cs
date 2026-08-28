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

        public static TuneProfile Current { get; private set; }
        public static int CurrentIndex { get; private set; }

        /// <summary>Profil değişince HUD parlasın diye.</summary>
        public static float ChangedAt { get; private set; } = -99f;

        static Tuning _baz;

        public static void Reset()
        {
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
        public static void Init(Tuning çalışma)
        {
            _baz = UnityEngine.Object.Instantiate(çalışma);
            _baz.name = "Tuning (A/B tabanı)";
            Select(0, çalışma);
        }

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
            foreach (var f in typeof(Tuning).GetFields(
                         System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                f.SetValue(hedef, f.GetValue(kaynak));
        }
    }
}
