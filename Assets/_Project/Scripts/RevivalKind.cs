using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Diriliş eşyaları. `Weapon` / `EnemyKind` / `RoomLayout` ile aynı desen.
    ///
    /// ⚠️ **KULLANIM SIRASI OYUNCUYA SORULMAZ.** Menü yok, öncelik ayarı yok.
    /// Sıra tasarıma gömülü ve kendiliğinden mantıklı:
    ///
    ///   ① ÖLÜM ALDATMA   → önce DEVAM ETMEYİ dene (en az kayıp)
    ///   ② GEÇİCİ DÖNÜŞ   → sonra KAZANMAYI dene (kumar)
    ///   ③ ÇAĞRI          → sonra kaybet (son çare, checkpoint'e düşersin)
    ///
    /// Sebep: oyunun tüm HUD'ı TEK BİR SAYI. Üstüne envanter ekranı + öncelik ayarı
    /// binerse minimalizm çöker. Oyuncu yalnızca elinde ne kaldığını görüyor.
    ///
    /// Hiçbiri kalmadıysa ölüm gerçekten sondur — oyun biter.
    /// </summary>
    public abstract class RevivalKind
    {
        public abstract string Name { get; }
        public abstract Color Tint { get; }

        /// <summary>Kullanıldığında saat kaça ayarlanır.</summary>
        public abstract float SecondsOnUse(Tuning t);

        /// <summary>Checkpoint'e döndürüyor mu (sahne yeniden kurulur) yoksa yerinde mi kalıyor.</summary>
        public virtual bool SendsToCheckpoint => false;

        /// <summary>Kullanınca "son şans" sayacı kurulur mu.</summary>
        public virtual bool ArmsLastStand => false;

        /// <summary>HUD ve öz-denetim için: `All` içindeki yeri = kayıttaki dizi indisi.</summary>
        public int Index { get; private set; }

        // ── varyantlar ───────────────────────────────────────────────────────

        sealed class Defiance : RevivalKind
        {
            public override string Name => "ÖLÜM ALDATMA";
            public override Color Tint => new Color(1f, 0.85f, 0.35f);
            // Checkpoint'teki zamanın küçük bir kısmı (Enes kararı). Yerinde kalkıyorsun,
            // düşmanlar da yerinde — yani ucuz değil, dövüşün ortasında az zamanla kalıyorsun.
            public override float SecondsOnUse(Tuning t)
                => SaveFile.CheckpointSeconds(t) * t.defianceFraction;
        }

        sealed class LastStand : RevivalKind
        {
            public override string Name => "GEÇİCİ DÖNÜŞ";
            public override Color Tint => new Color(1f, 0.45f, 0.40f);
            public override bool ArmsLastStand => true;
            // ⚠️ SABİT 10 saniye (Enes kararı) — checkpoint'e bağlı değil.
            // Kısa ve okunur: saat zaten ekranda, geri sayımı görüyorsun.
            public override float SecondsOnUse(Tuning t) => t.lastStandSeconds;
        }

        sealed class Recall : RevivalKind
        {
            public override string Name => "ÇAĞRI";
            public override Color Tint => new Color(0.55f, 0.75f, 1f);
            public override bool SendsToCheckpoint => true;
            public override float SecondsOnUse(Tuning t) => SaveFile.CheckpointSeconds(t);
        }

        /// <summary>Onarım sırası: `All` sırasıyla ilk KIRIK eşya. `FirstAvailable`ın eşi.</summary>
        public static RevivalKind FirstBroken(SaveData s)
        {
            if (s == null) return null;
            foreach (var k in All)
                if (k.Index < s.broken.Length && s.broken[k.Index] > 0) return k;
            return null;
        }

        public static readonly RevivalKind DefianceKind = new Defiance();
        public static readonly RevivalKind LastStandKind = new LastStand();
        public static readonly RevivalKind RecallKind = new Recall();

        /// <summary>⚠️ SIRA BUDUR — kullanım önceliği dizinin sırasıdır.</summary>
        public static readonly RevivalKind[] All = { DefianceKind, LastStandKind, RecallKind };

        static RevivalKind()
        {
            for (int i = 0; i < All.Length; i++) All[i].Index = i;
        }

        /// <summary>Sırası gelen sağlam eşya; hiç yoksa null (= oyun biter).</summary>
        public static RevivalKind FirstAvailable(SaveData s)
        {
            if (s == null) return null;
            foreach (var k in All)
                if (k.Index < s.intact.Length && s.intact[k.Index] > 0) return k;
            return null;
        }

        public static int IntactCount(SaveData s)
        {
            if (s == null) return 0;
            int n = 0;
            foreach (var v in s.intact) n += v;
            return n;
        }
    }
}
