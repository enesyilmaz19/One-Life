using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TekCan
{
    /// <summary>
    /// Saniyeyle satın alma noktası. Oyunun ekonomisi burada kapanıyor:
    /// zamanı harcayarak zamanı toplamakta daha iyi hale geliyorsun.
    ///
    /// Fiyat ömründen kesiliyor — altın yok, ruh yok, ayrı para birimi yok.
    /// Her alım seni ölüme yaklaştırırken ölmeni zorlaştırıyor. Bileşik yatırım
    /// kararı: erken alırsan koşunun geri kalanı kârlı geçer, ama o parayı
    /// bulmak için önce zar zor idare etmen gerekir.
    /// </summary>
    public class Altar : MonoBehaviour
    {
        // ⚠️ Sıra: ilk beşi `Clock._alınan` dizisinin indisi (sayım kayıtta) — araya ekleme yapma.
        public enum Kind { Harvest, Damage, Dash, Insurance, Patience, Weapon, Loadout, Unlock, ComboPlus, Repair }

        public static readonly List<Altar> All = new List<Altar>();
        public static void ResetAll() => All.Clear();

        Tuning _t;
        Transform _player;
        Renderer _renderer;
        int _bought;

        public Kind Type { get; private set; }
        // ONARIM tükenmez ve fiyatı sabittir: yükseltme değil, ONARIM. Her ölümden sonra
        // aynı bedelle yeniden karşına çıkması gerekiyor.
        public bool SoldOut => Type == Kind.Weapon ? SilahTükendi()
                             : Type == Kind.Loadout ? false
                             : Type == Kind.Unlock ? Kadro.Açık(Yuva)
                             : Type == Kind.ComboPlus ? Combo.Artı((ComboTür)Yuva) || Combo.BölgeAlımı(Bölge()) >= 1
                             : Type != Kind.Repair && _bought >= _t.altarMaxPurchases;

        /// <summary>SİLAH SUNAĞI'nın yuvası (0–2): kuşanılan silahın kaçıncı yükseltmesi.</summary>
        public int Yuva { get; private set; }
        // ⚠️ Faz 27: fiyat bu SUNAĞIN değil, koşunun o türden alım sayısına bağlı — bölge
        // değişince sıfırlanmıyor (kartopu buradan büyüyordu). `_bought` yalnızca sunak başı tavan.
        public float Price => Type == Kind.Weapon || Type == Kind.ComboPlus
                                ? Clock.SilahFiyatı(_t, SilahSunağı.AlınanSayısı, Clock.Instance != null ? Clock.Instance.Seconds : 0f)
                            : Type == Kind.Loadout ? 0f
                            : Type == Kind.Unlock ? Clock.SilahAçmaFiyatı(_t, Clock.Instance != null ? Clock.Instance.Seconds : 0f, Kadro.Keşifti(Yuva))
                            : Type == Kind.Repair ? _t.revivalRepairPrice
                            : Clock.Instance != null ? Clock.Instance.SıradakiFiyat(Type)
                            : Clock.Fiyat(_t, 0);

        float Getiri() => Clock.Instance != null ? Clock.Instance.SıradakiGetiri(Type) : Clock.Getiri(_t, Type, 0);

        static RevivalKind FirstBroken() => RevivalKind.FirstBroken(SaveFile.Current);
        public bool PlayerInRange { get; private set; }

        static readonly Color HarvestColor = new Color(0.35f, 0.85f, 0.55f);
        static readonly Color DamageColor = new Color(0.9f, 0.55f, 0.35f);
        static readonly Color DashColor = new Color(0.45f, 0.65f, 1f);
        static readonly Color RepairColor = new Color(1f, 0.85f, 0.35f);   // diriliş eşyalarının sarısı
        static readonly Color SoldOutColor = new Color(0.3f, 0.3f, 0.33f);

        public void Init(Tuning tuning, Transform player, Kind kind, int yuva = 0)
        {
            _t = tuning;
            _player = player;
            Type = kind;
            Yuva = yuva;
            _renderer = GetComponentInChildren<Renderer>();
            All.Add(this);
            Paint();
        }

        void OnDestroy() => All.Remove(this);

        void Update()
        {
            if (_t == null || _player == null) return;

            Vector3 to = _player.position - transform.position;
            to.y = 0f;
            PlayerInRange = to.magnitude <= _t.altarUseRange;
            if (Type == Kind.Weapon && Silah() != _boyananSilah) Paint();   // silah değişince sunak da değişir

            transform.Rotate(Vector3.up, 40f * Time.deltaTime, Space.World);

            if (PlayerInRange && !SoldOut && UsePressed()) Buy();
        }

        static bool UsePressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonNorth.wasPressedThisFrame;
        }

        void Buy()
        {
            // Tükenmişlik kuralı BURADA — tuşa basma yolunda değil yalnızca: silah sunağının
            // "boss başına iki alım" sınırı başka bir çağıranla delinmesin (Faz 29 kapısı yakaladı).
            if (SoldOut) return;
            var clock = Clock.Instance;

            // ── Faz 30 ──
            if (Type == Kind.Loadout)
            {
                // KADRO: kuşandığın silahın yuvasına envanterde olmayan bir sonraki açık silah gelir.
                int yuva = Mathf.Max(0, Kadro.Yuvası(Silah()));
                int yeni = Kadro.Değiştir(yuva);
                if (yeni < 0) return;
                var pc = _player != null ? _player.GetComponent<PlayerController>() : null;
                if (pc != null) pc.SetWeapon(yeni);
                Sfx.Buy();
                foreach (var a in All) if (a != null) a.Paint();
                return;
            }
            if (Type == Kind.Unlock)
            {
                if (clock == null || !clock.TryBuy(Price)) return;
                Kadro.Aç(Yuva, Kadro.Keşifti(Yuva) ? "keşifte kaçırılmıştı, satın alındı" : "satın alındı");
                Sfx.Buy();
                foreach (var a in All) if (a != null) a.Paint();
                return;
            }
            if (Type == Kind.ComboPlus)
            {
                if (clock == null || !clock.TryBuy(Price)) return;
                var c = (ComboTür)Yuva;
                Combo.ArtıAl(c, Bölge());
                SaveFile.Flush();
                Combo.Duyur($"COMBO GELİŞTİ: {Combo.Bu(c).Ad} + {Combo.Bu(c).ArtıAd} — {Combo.Bu(c).ArtıNeYapar}", 5f);
                Sfx.Buy();
                foreach (var a in All) if (a != null) a.Paint();
                return;
            }

            // ⚠️ ONARIM ZAMANLA yapılır — ayrı malzeme YOK. Ayrı malzeme olsaydı ikinci
            // bir para birimi doğar ve "tek bir sayı var: saniye" yasası çatlardı.
            // Bedeli budur ve oyunun en güzel kararını doğurur: kendi ömrünü verip
            // ikinci bir şans satın alıyorsun.
            if (Type == Kind.Repair)
            {
                var kırık = FirstBroken();
                // ⚠️ Checkpoint bedeli taşıyamıyorsa SATMA. Taşımadan satılsaydı checkpoint
                // 0.5 sn'ye iniyor, her ÇAĞRI düşmanın ortasında anında ölüm oluyor ve kalan
                // eşyalar zincirleme yanıyordu (`TryBuy`'ın "kendini öldüremezsin" kuralı).
                // ⚠️ Ve ÇAĞRI TABANININ ALTINA da inemez (K2): inseydi taban bedeli geri
                // öderdi — öl, dön, onar, öl… sonsuz diriliş geri gelirdi.
                if (kırık == null || clock == null || !OnarımTaşınıyor()
                    || !clock.TryBuy(Price)) return;

                var s = SaveFile.Current;
                s.broken[kırık.Index]--;
                s.intact[kırık.Index]++;

                // ⚠️ Bedel CHECKPOINT'TEN de düşülüyor. Checkpoint odaya GİRİŞTE yazılıyor,
                // onarım ondan SONRA alınıyor: düşülmeseydi her ÇAĞRI bedeli geri veriyordu
                // — öl, dön, yine onar, yine öl… SONSUZ diriliş (denetim 2026-09-23).
                // Onarım kalıcı, bedeli de kalıcı olmalı.
                s.checkpointSeconds = Mathf.Max(0.5f, SaveFile.CheckpointSeconds(_t) - Price);
                // ⚠️ ...ve ODA BAŞI KAYDINDAN da (K1). Onarım kalıcı ve hemen diske gidiyor;
                // bedeli oda başı kaydında kalmasaydı kapat-aç zamanı geri verirdi, onarım
                // kalırdı — K2'nin kapattığı sonsuz diriliş geri gelirdi (inceleme 2026-10-03).
                // Yükseltmelerde bu sorun yok: onlar oda başı kaydıyla BİRLİKTE geri sarılıyor.
                // `Max`: 0'ın altı `Clock.Init`'te "baştan başla" (tam saat) demek.
                s.resumeSeconds = Mathf.Max(0.5f, s.resumeSeconds - Price);
                SaveFile.Flush();          // kazanım da hemen yazılır, ölüm gibi

                Sfx.Buy();
                IsoCamera.Shake(_t.hitShake * 0.7f);
                Paint();
                Debug.Log($"[Tek Can] ONARILDI: {kırık.Name} — {Price:0.0} saniye ödendi");
                return;
            }

            if (Type == Kind.Weapon)
            {
                var y = Yükseltme();
                if (clock == null || SilahSunağı.Var(y) || !clock.TryBuy(Price)) return;
                SilahSunağı.Al(y, Bölge());
                SaveFile.Flush();          // koşu boyu kalıcı — hemen yazılıyor
                Sfx.Buy();
                IsoCamera.Shake(_t.hitShake * 0.7f);
                foreach (var a in All) if (a != null) a.Paint();
                Debug.Log($"[Tek Can] SİLAH SUNAĞI: {SilahSunağı.Ad[(int)y]} alındı ({Weapon.All[SilahSunağı.Silahı(y)].Name})");
                return;
            }

            if (clock == null || !clock.TryBuy(Price)) return;

            _bought++;
            // Tuning'e DEĞİL Clock'a yazılıyor — asset çalışma anında değiştirilirse
            // yükseltmeler bir sonraki koşuya sızar (bkz. Clock'taki not).
            clock.Yükselt(Type);

            Sfx.Buy();
            IsoCamera.Shake(_t.hitShake * 0.7f);
            Paint();
        }

        public string Label()
        {
            if (Type == Kind.Repair)
            {
                var kırık = FirstBroken();
                if (kırık == null) return "ONARIM   [tamir edilecek eşya yok]";
                if (!OnarımTaşınıyor())
                    return $"ONARIM  {kırık.Name}   [checkpoint zamanın {Price:0} sn'yi taşımıyor]";
                return $"ONARIM  {kırık.Name}   —   {Price:0.0} saniye   [E]";
            }

            if (Type == Kind.Loadout)
            {
                int yuva = Mathf.Max(0, Kadro.Yuvası(Silah()));
                int aday = Kadro.Sıradaki(yuva);
                return aday < 0 ? $"KADRO   [başka açık silah yok — {Kadro.AçıkSayısı} silah açık]"
                                : $"KADRO — [E] {yuva + 1}. yuvadaki {Weapon.All[Silah()].Name} yerine {Weapon.All[aday].Name}   (1–3 ile yuva seç)";
            }
            if (Type == Kind.Unlock)
            {
                string ad = $"YENİ SİLAH: {Weapon.All[Yuva].Name}" + (Kadro.Keşifti(Yuva) ? "  (keşifte kaçırılmıştı)" : "");
                return SoldOut ? $"{ad}   [ALINDI]" : $"{ad}   —   {Price:0.0} saniye   [E]";
            }
            if (Type == Kind.ComboPlus)
            {
                var c = Combo.Bu((ComboTür)Yuva);
                string ad = $"COMBO · {c.Ad} + {c.ArtıAd} — {c.ArtıNeYapar}";
                return Combo.Artı((ComboTür)Yuva) ? $"{ad}   [ALINDI]"
                     : SoldOut ? $"{ad}   [bu boss'tan alındı]" : $"{ad}   —   {Price:0.0} saniye   [E]";
            }
            if (Type == Kind.Weapon)
            {
                var y = Yükseltme();
                string ne = $"{Weapon.All[Silah()].Name} · {SilahSunağı.Ad[(int)y]} — {SilahSunağı.NeYapar[(int)y]}";
                return SilahSunağı.Var(y) ? $"{ne}   [ALINDI]"
                     : SoldOut ? $"{ne}   [bu sunaktan {_t.weaponAltarPurchases} alım yapıldı]"
                     : $"{ne}   —   {Price:0.0} saniye   [E]   (silah değiştir: 1–5)";
            }
            string what = Type switch
            {
                // Gösterilen SIRADAKİ alımın getirisi — azalıyor, sabit adım yalan olurdu.
                Kind.Harvest   => $"HASAT  +%{Getiri() * 100f:0.0}",
                Kind.Damage    => $"KESKİNLİK  +{Getiri():0.00}",
                Kind.Insurance => $"MUAFİYET  düşman vuruşu −%{Getiri() * 100f:0.0}",
                Kind.Patience  => $"SABIR  sağ tık beklemesi −%{Getiri() * 100f:0.0}",
                _              => $"ÇEVİKLİK  dash -{Getiri():0.000}sn",
            };
            return SoldOut ? $"{what}   [TÜKENDİ]" : $"{what}   —   {Price:0.0} saniye   [E]";
        }

        /// <summary>Onarım bedeli düşülünce checkpoint ÇAĞRI tabanının üstünde kalıyor mu.</summary>
        bool OnarımTaşınıyor() => SaveFile.CheckpointSeconds(_t) - Price >= _t.recallMinSeconds;

        void Paint()
        {
            if (_renderer == null) return;
            _boyananSilah = Silah();
            Color silahRengi = Weapon.All[_boyananSilah].Tint; silahRengi.a = 1f;
            _renderer.material.color = SoldOut ? SoldOutColor : Type switch
            {
                Kind.Harvest => HarvestColor,
                Kind.Damage => DamageColor,
                Kind.Insurance => InsuranceColor,
                Kind.Patience => PatienceColor,
                Kind.Weapon => silahRengi,
                Kind.Loadout => new Color(0.9f, 0.9f, 0.95f),
                Kind.Unlock => YuvaRengi(),
                Kind.ComboPlus => new Color(1f, 0.8f, 0.2f),
                Kind.Repair => FirstBroken() != null ? RepairColor : SoldOutColor,
                _ => DashColor,
            };
        }

        // ── SİLAH SUNAĞI (Faz 29) ──
        static readonly Color InsuranceColor = new Color(0.75f, 0.75f, 0.85f);
        static readonly Color PatienceColor = new Color(0.6f, 0.45f, 0.9f);
        int _boyananSilah = -1;

        Color YuvaRengi()
        {
            if (Yuva < 0 || Yuva >= Weapon.All.Length) return SoldOutColor;
            var c = Weapon.All[Yuva].Tint; c.a = 1f;
            return c;
        }

        int Silah()
        {
            var pc = _player != null ? _player.GetComponent<PlayerController>() : null;
            return pc != null ? Mathf.Max(0, System.Array.IndexOf(Weapon.All, pc.Weapon)) : 0;
        }

        SilahYükseltme Yükseltme() => SilahSunağı.Yuvası(Silah(), Yuva);

        static int Bölge() => RoomManager.AktifBölge;

        bool SilahTükendi() => SilahSunağı.Var(Yükseltme())
                            || SilahSunağı.BölgeAlımı(Bölge()) >= _t.weaponAltarPurchases;
    }
}
