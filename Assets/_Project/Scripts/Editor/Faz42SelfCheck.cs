using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 42 — Parti 1: 20 direkt combo (15 ikili + 5 üçlü; Enes'in onayı 2026-10-25) · Parti 2: 18 açılır (12 ikili EPİK, 6 üçlü
    /// EFSANEVİ yükseltmeyle; onay 2026-10-27) — docs/COMBOLAR.md. Her combo ETKİ olarak, üç kurguda: pencere geçmiş (combo yok) ·
    /// pencerede (combo) · pencerede + "+" hâli; açılır olanlarda açan yükseltme üçünde de alınmış. Yapı: sayı, eşsiz sıra,
    /// direkt / açılır, üçlüler 2. bölgeden; bir üçlünün başı olan ikililer (zincir silinmez — ALACAK DEVRİ → İCRA TAKİBİ).
    /// </summary>
    public static class Faz42SelfCheck
    {
        const BindingFlags Gizli = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tek Can/Faz 42 öz-denetimi çalıştır (combo Parti 1)")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        static Tuning _t;
        static Clock _clock;
        static PlayerController _pc;
        static GameObject _pGo, _root;
        static readonly MethodInfo Vur = typeof(PlayerController).GetMethod("Strike", Gizli),
                                   Başla = typeof(PlayerController).GetMethod("StartAttack", Gizli);
        static readonly FieldInfo Yavaş = typeof(Enemy).GetField("_yavaşKalan", Gizli),
                                  Durum = typeof(Enemy).GetField("_state", Gizli), DurumSayacı = typeof(Enemy).GetField("_stateTimer", Gizli),
                                  İtme = typeof(Enemy).GetField("_push", Gizli);

        public static int Execute()
        {
            int failed = 0, total = 0;
            _t = ScriptableObject.CreateInstance<Tuning>();
            _t.startSeconds = 300f;
            _t.enemyTimePool = 200f;
            _t.enemyDecayPerSecond = 0f;
            Region.Reset();
            Temizle();

            _root = new GameObject("faz42-check");
            _clock = _root.AddComponent<Clock>();
            _pGo = null; _pc = null;

            failed += Yapısı(ref total);
            failed += İkililer(ref total);
            failed += Üçlüler(ref total);
            failed += Parti2Yapısı(ref total);
            failed += Parti2İkililer(ref total);
            failed += Parti2Üçlüler(ref total);

            Temizle();
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_t);
            Clock.Reset(); Region.Reset(); RoomManager.Reset();
            Debug.Log($"[Faz 42 öz-denetim] {(failed == 0 ? "✅" : "❌")} {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── kurgu (Faz 33'ünkiyle aynı) ─────────────────────────────────────────
        static void YeniKoşu(float saat = 300f)
        {
            Temizle();
            SaveFile.UseInMemory(new SaveData { upgradeSeed = 12345 });
            if (_pGo != null) Object.DestroyImmediate(_pGo);
            _pGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _pGo.transform.SetParent(_root.transform);
            Ortaya();
            _pc = _pGo.AddComponent<PlayerController>();
            _pc.Init(_t, null);
            _clock.Init(_t, _pGo.transform, saat);
            _clock.Frozen = false;
            for (int w = 0; w < Weapon.All.Length; w++) Kadro.Aç(w, "test");
        }

        static void Ortaya()
        {
            _pGo.transform.position = Bootstrap.ArenaCenter;
            _pGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        }

        static Enemy Düşman(Vector3 yer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = yer;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(_t, _pGo.transform, null);
            return e;
        }

        static Enemy Bakan(Vector3 yer, Vector3 bakış) { var e = Düşman(yer); e.transform.rotation = Quaternion.LookRotation(bakış); return e; }
        static Vector3 Yer(float x, float z) => Bootstrap.ArenaCenter + new Vector3(x, 0f, z);
        static Vector3 İleri(float m) => Yer(0f, m);
        static void W(int silah) => _pc.SetWeapon(silah);
        static void Silahla(int silah) { _pc.SetWeapon(silah); Vur.Invoke(_pc, null); }
        static void Bas() => Başla.Invoke(_pc, new object[] { Vector3.forward });
        static void TahvilBoz(float tut) { W(Kadro.TAHVİL); Bas(); _pc.Vadelen(tut); Vur.Invoke(_pc, null); }   // tut → bırak
        static object Alan(string ad) => typeof(PlayerController).GetField(ad, Gizli).GetValue(_pc);
        static void Alan(string ad, object v) => typeof(PlayerController).GetField(ad, Gizli).SetValue(_pc, v);
        static void Bekle(int silah, float sn) => ((float[])Alan("_specialCd"))[silah] = sn;
        static bool Yaptı(ComboTür c) => ((float[])Alan("_comboBekleme"))[(int)c] > 0f;
        static void ArtıVer(ComboTür c) { var sv = SaveFile.Current; sv.comboUpgrades = SaveFile.Büyüt(sv.comboUpgrades, Combo.Sayı); sv.comboUpgrades[(int)c] = 1; }
        /// <summary>Hasat yok: saat farkı yalnızca combo'nun iadesi olsun (iade de `Clock.Add`'den geçiyor).</summary>
        static void HasatKapat() => typeof(Clock).GetProperty("HarvestRate").GetSetMethod(true).Invoke(_clock, new object[] { 0f });
        static float Hasar(Enemy e, System.Action a) { float önce = e.TimeLeft; a(); return önce - e.TimeLeft; }
        static float Hasat(System.Action a) { float h0 = _clock.TotalHarvested; a(); return _clock.TotalHarvested - h0; }
        static GroundMark[] İşaretler() => Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Exclude).Where(m => m != null && m.Bekliyor).ToArray();
        static int Uçur(System.Func<bool> dur = null)
        {
            int n = 0;
            for (; n < 2000 && CekUcusu.Uçanlar.Count > 0; n++)
            {
                if (dur != null && dur()) break;
                foreach (var u in CekUcusu.Uçanlar.ToArray()) u.Step(0.02f);
            }
            return n;
        }
        /// <summary>ÇEK'i ileri at, ilk isabete kadar uçur (adım yazıldı, çek havada).</summary>
        static void ÇekAt() { Silahla(Kadro.ÇEK); var ç = CekUcusu.Uçanlar.First(); Uçur(() => ç.İsabet >= 1); }
        static bool Yakın(float a, float b, float pay = 0.02f) => Mathf.Abs(a - b) <= pay * Mathf.Max(1f, Mathf.Abs(b));
        static float Taban(int silah) => Weapon.All[silah].Damage(_t, _clock.AttackDamage);
        static Enemy Boş(Vector3 yer) { var e = Düşman(yer); e.TakeDamage(e.TimeLeft, 0f); return e; }   // bir vuruşta ölür (hasatsız boşaltıldı)
        static void HazırlıkKur(Enemy e, float kalan) { Durum.SetValue(e, System.Enum.Parse(Durum.FieldType, "Windup")); DurumSayacı.SetValue(e, kalan); }
        static float İtmesi(Enemy e) => ((Vector3)İtme.GetValue(e)).magnitude;

        /// <summary>Bağla (iki MÜŞTEREK isabeti, ikincisi adım), uçları arkaya taşı: aradaki çizgide, ikisi de koninin dışında.</summary>
        static (Enemy a, Enemy b, Enemy ara) BağKurgusu()
        {
            var a = Düşman(İleri(1.5f)); var b = Düşman(Yer(1.5f, 0f));
            Silahla(Kadro.MÜŞTEREK);
            _pGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
            Silahla(Kadro.MÜŞTEREK);
            Ortaya();
            a.transform.position = Yer(-3f, -1.5f); b.transform.position = Yer(3f, -1.5f);
            return (a, b, Düşman(Yer(0f, -1.5f)));
        }

        // Adımlar arası: pencerede (0.2 sn) ya da pencere geçmiş (1 sn > comboWindow 0.8)
        static float _ara;
        static void Geç() => _pc.TickCombo(_ara);

        /// <summary>
        /// Combo'yu üç kez kur, her biri taze koşu: pencere geçmiş (combo YOK) · pencerede · pencerede + "+".
        /// Üçlüler 2. bölgede (oda yöneticisi kuruluyor, odanın düşmanları siliniyor). `oldu`: yalnızca ortadaki ikisinde
        /// BU combo tetiklendi (bekleme sayacından — başka bir combo'nun tetiklenmesi sayılmaz).
        /// </summary>
        static (T yok, T var, T artı, bool oldu) Üç<T>(ComboTür c, System.Func<T> ölç, float saat = 300f)
        {
            T Koş(float ara, bool artı, out bool yaptı)
            {
                YeniKoşu(saat);
                if (Combo.Bu(c).Açan is SilahYükseltme açan) SilahSunağı.Al(açan, -1);   // Parti 2: açan yükseltme alınmış
                RoomManager rm = null;
                if (Combo.Bu(c).MinBölge > 0)
                {
                    var go = new GameObject("faz42-oda"); go.transform.SetParent(_root.transform);
                    rm = go.AddComponent<RoomManager>();
                    rm.Init(_t, _pGo.transform, null, startRoom: 1, startRegion: Combo.Bu(c).MinBölge);
                    foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
                    Enemy.All.Clear();
                    Ortaya();
                }
                if (artı) ArtıVer(c);
                _ara = ara;
                var m = ölç();
                yaptı = Yaptı(c);
                if (rm != null) { Object.DestroyImmediate(rm.gameObject); RoomManager.Reset(); }
                return m;
            }
            var a = Koş(1f, false, out bool y0);
            var b = Koş(0.2f, false, out bool y1);
            var d = Koş(0.2f, true, out bool y2);
            return (a, b, d, !y0 && y1 && y2);
        }

        // ── 1) YAPI ──────────────────────────────────────────────────────────────
        static int Yapısı(ref int total)
        {
            var yeni = Enumerable.Range((int)ComboTür.Cebriİcra, 20).Select(i => (ComboTür)i).ToList();
            bool sayı = Combo.Sayı == 60 && System.Enum.GetValues(typeof(ComboTür)).Length == Combo.Sayı;
            int ikili = yeni.Count(c => Combo.Bu(c).Sıra().Length == 2), üçlü = yeni.Count(c => Combo.Bu(c).Sıra().Length == 3);
            bool direkt = yeni.All(c => !Combo.Bu(c).Açan.HasValue && !string.IsNullOrEmpty(Combo.Bu(c).ArtıAd)
                                        && Combo.Bu(c).MinBölge == (Combo.Bu(c).Sıra().Length == 3 ? 1 : 0));
            var sıralar = Combo.Tümü.Select(t => string.Join(">", t.Sıra())).ToList();
            bool eşsiz = sıralar.Distinct().Count() == sıralar.Count;
            YeniKoşu();
            foreach (var t in Combo.Tümü) if (t.Açan is SilahYükseltme y) SilahSunağı.Al(y, -1);   // açılırlar da sayılsın
            // Bir üçlünün başı olan ikili bitince zincir silinmiyor, adım `_isabetYeri`'nden yazılıyor (ComboTetikle) — bugün
            // yalnızca ALACAK DEVRİ ve o koni vuruşuyla bitiyor. Yenisi gelirse burası kırmızı: bitiş yolunu kontrol et.
            var baş = Enumerable.Range(0, Combo.Sayı).Select(i => (ComboTür)i).Where(c => Combo.ÜçlüyeDevam(c, 4)).ToList();
            bool başlar = baş.Count == 1 && baş[0] == ComboTür.AlacakDevri && !Combo.ÜçlüyeDevam(ComboTür.AlacakDevri, 0);
            return Check(ref total, $"⚠️ Parti 1: {Combo.Sayı} combo (= 60), yeni {ikili} ikili + {üçlü} üçlü, hepsi direkt ve \"+\"lı, üçlüler 2. bölgeden; " +
                                    $"hiçbir sıra tekrar etmiyor; üçlünün başı olan ikili yalnızca ALACAK DEVRİ ({string.Join(", ", baş.Select(c => Combo.Bu(c).Ad))}), 1. bölgede değil",
                         sayı && ikili == 15 && üçlü == 5 && direkt && eşsiz && başlar, Combo.Sayı);
        }

        // ── 2) İKİLİLER ──────────────────────────────────────────────────────────
        static int İkililer(ref int total)
        {
            int failed = 0;
            float koni = Taban(Kadro.KONİ), faizli = Taban(Kadro.FAİZLİ), haciz = Taban(Kadro.HACİZ), arb = Taban(Kadro.ARBİTRAJ);

            var ci = Üç<(float h, bool sus)>(ComboTür.Cebriİcra, () =>
            {
                var uzak = Düşman(İleri(5.5f));
                Silahla(Kadro.HACİZ); Geç();
                float h = Hasar(uzak, () => Silahla(Kadro.KONİ));
                return (h, uzak.Susturulmuş);
            });
            failed += Check(ref total, $"CEBRİ İCRA (HACİZ → KONİ): 5.5 birimdeki önüne çekilip ×1.5 yiyor ({ci.yok.h:0.00} → {ci.var.h:0.00} = {koni * 1.5f:0.00}); ZORLA GETİRME (+) susturuyor",
                            ci.oldu && ci.yok.h == 0f && Yakın(ci.var.h, koni * 1.5f) && !ci.var.sus && ci.artı.sus, ci.var.h);

            var tb = Üç<(float h, float saat, float yan)>(ComboTür.TopluBorç, () =>
            {
                HasatKapat();
                var sol = Düşman(Yer(-0.7f, 1.3f)); var orta = Düşman(İleri(1.3f)); var sağ = Düşman(Yer(0.7f, 1.3f));
                Silahla(Kadro.KONİ); Geç();                                     // üçünü vurur
                float y0 = sol.TimeLeft + sağ.TimeLeft, s0 = _clock.Seconds;
                float h = Hasar(orta, () => Silahla(Kadro.FAİZLİ));
                return (h, _clock.Seconds - s0, y0 - sol.TimeLeft - sağ.TimeLeft);
            });
            failed += Check(ref total, $"TOPLU BORÇ (KONİ → FAİZLİ): KONİ üç vurdu → +%45 ({tb.yok.h:0.00} → {tb.var.h:0.00} = {faizli * 1.45f:0.00}), savurma bedeli geri ({tb.yok.saat:+0.00} → {tb.var.saat:+0.00}); KEFİLLİ BORÇ (+) yanındakine sekiyor ({tb.var.yan:0.00} → {tb.artı.yan:0.00})",
                            tb.oldu && Yakın(tb.yok.h, faizli) && Yakın(tb.var.h, faizli * 1.45f) && Yakın(tb.yok.saat, 0f) && Yakın(tb.var.saat, _t.faizliSwingCost)
                            && tb.var.yan == 0f && Yakın(tb.artı.yan, tb.artı.h), tb.var.h);

            var ad = Üç<(float h, float f, bool sırt)>(ComboTür.AlacakDevri, () =>
            {
                var yakın = Düşman(İleri(1.3f)); var uzak = Düşman(İleri(5.5f));
                float f = Hasar(yakın, () => Silahla(Kadro.FAİZLİ)); Geç();
                float h = Hasar(uzak, () => Silahla(Kadro.HACİZ));
                return (h, f, uzak.SırtDönük);
            });
            failed += Check(ref total, $"ALACAK DEVRİ (FAİZLİ → HACİZ): FAİZLİ'nin vuruşu HACİZ'in hedefine iniyor ({ad.yok.h:0.00} → {ad.var.h:0.00} = {haciz:0.00} + {ad.var.f:0.00}); TEMLİK (+) sırtını döndürüyor",
                            ad.oldu && Yakın(ad.yok.h, haciz) && ad.var.f > 0f && Yakın(ad.var.h, haciz + ad.var.f) && !ad.var.sırt && ad.artı.sırt, ad.var.h);

            var tp = Üç<(float iç, float yan, int bekleyen, float bek)>(ComboTür.Tapuİptali, () =>
            {
                var içte = Düşman(İleri(Weapon.All[Kadro.İPOTEK].Range(_t))); Düşman(İleri(1.3f));
                Silahla(Kadro.İPOTEK); Geç();                                   // işaret nişana (menzil ucu, içte'nin üstü)
                float a = 75f * Mathf.Deg2Rad;
                var yan = Düşman(Yer(1.6f * Mathf.Sin(a), 1.6f * Mathf.Cos(a)));   // 75°: koninin (100°) dışında, +60°'nin içinde
                Bekle(Kadro.KONİ, 5f);
                float y0 = yan.TimeLeft;
                float i = Hasar(içte, () => Silahla(Kadro.KONİ));
                return (i, y0 - yan.TimeLeft, İşaretler().Length, _pc.SpecialCooldownOf(Kadro.KONİ));
            });
            failed += Check(ref total, $"TAPU İPTALİ (İPOTEK → KONİ): işaret beklemeden patlıyor (içindeki {tp.yok.iç:0.00} → {tp.var.iç:0.00}, bekleyen {tp.yok.bekleyen} → {tp.var.bekleyen}), yay +60° (75°'deki {tp.yok.yan:0.00} → {tp.var.yan:0.00}); İSTİMLAK (+) KONİ'nin beklemesi sıfır",
                            tp.oldu && tp.yok.iç == 0f && tp.yok.bekleyen == 1 && tp.var.iç > 0f && tp.var.bekleyen == 0 && tp.yok.yan == 0f && Yakın(tp.var.yan, koni)
                            && tp.var.bek == 5f && tp.artı.bek == 0f, tp.var.iç);

            var am = Üç<(float h, float k, float hasat)>(ComboTür.AmmeAlacağı, () =>
            {
                Düşman(İleri(5.5f));
                Silahla(Kadro.HACİZ); Geç();
                var hedef = Düşman(İleri(1.3f)); var komşu = Düşman(Yer(2f, 1.3f));
                float h = 0f, hs = 0f;
                float k = Hasar(komşu, () => hs = Hasat(() => h = Hasar(hedef, () => Silahla(Kadro.İFLAS))));
                return (h, k, hs);
            });
            failed += Check(ref total, $"AMME ALACAĞI (HACİZ → İFLAS): vuruşun yarısı 3 birim içindekine ({am.yok.k:0.00} → {am.var.k:0.00} = {am.var.h * 0.5f:0.00}); GECİKME ZAMMI (+) hasat ×{am.artı.hasat / Mathf.Max(0.0001f, am.var.hasat):0.00}",
                            am.oldu && am.yok.k == 0f && am.var.h > 0f && Yakın(am.var.k, am.var.h * 0.5f) && Yakın(am.artı.hasat / Mathf.Max(0.0001f, am.var.hasat), 2f, 0.05f), am.var.k);

            var im = Üç<(float h, float i, float hasat)>(ComboTür.İflasMasası, () =>
            {
                var a = Düşman(İleri(1.3f));
                float i = Hasar(a, () => Silahla(Kadro.İFLAS)); Geç();
                float h = 0f;
                float hs = Hasat(() => h = Hasar(a, () => Silahla(Kadro.KONİ)));
                return (h, i, hs);
            });
            failed += Check(ref total, $"İFLAS MASASI (İFLAS → KONİ): İFLAS'ın yarısı koniye ({im.yok.h:0.00} → {im.var.h:0.00} = {koni:0.00} + {im.var.i:0.00}/2); MASA PAYI (+) hasat ×{im.artı.hasat / Mathf.Max(0.0001f, im.var.hasat):0.00}",
                            im.oldu && Yakın(im.yok.h, koni) && Yakın(im.var.h, koni + 0.5f * im.var.i) && Yakın(im.artı.hasat / Mathf.Max(0.0001f, im.var.hasat), 2f, 0.05f), im.var.h);

            float gecikme = Weapon.All[Kadro.İPOTEK].GroundMarkDelay(_t);
            var ir = Üç<(float kalan, float uzaklık, float bek)>(ComboTür.İpotekliRehin, () =>
            {
                var r = Düşman(İleri(1.5f)); Düşman(İleri(4f));
                Silahla(Kadro.REHİN); Silahla(Kadro.REHİN); Geç();            // yakala, fırlat (adım)
                Vector3 varış = r.transform.position;
                Bekle(Kadro.İPOTEK, 5f);
                Silahla(Kadro.İPOTEK);
                var m = İşaretler().FirstOrDefault();
                if (m == null) return (-1f, -1f, -1f);
                Vector3 d = m.transform.position - varış; d.y = 0f;
                return (m.Kalan, d.magnitude, _pc.SpecialCooldownOf(Kadro.İPOTEK));
            });
            failed += Check(ref total, $"İPOTEKLİ REHİN (REHİN → İPOTEK): işaret rehinenin düştüğü yere ({ir.yok.uzaklık:0.00} → {ir.var.uzaklık:0.00} birim), gecikme ×0.1 ({ir.yok.kalan:0.00} → {ir.var.kalan:0.00} sn); İKİNCİ DERECE (+) İPOTEK'in beklemesi sıfır",
                            ir.oldu && Yakın(ir.yok.kalan, gecikme) && Yakın(ir.var.kalan, gecikme * 0.1f) && ir.yok.uzaklık > 0.5f && ir.var.uzaklık < 0.01f
                            && ir.var.bek == 5f && ir.artı.bek == 0f, ir.var.kalan);

            var cr = Üç<(float uzak, bool sus)>(ComboTür.Ciro, () =>
            {
                var uzak = Düşman(İleri(5f));
                ÇekAt(); Geç();
                Düşman(İleri(1.3f));
                float h = Hasar(uzak, () => Silahla(Kadro.KONİ));
                return (h, uzak.Susturulmuş);
            });
            failed += Check(ref total, $"CİRO (ÇEK → KONİ): çekin çarptığı yerde koninin %80'i ({cr.yok.uzak:0.00} → {cr.var.uzak:0.00} = {koni * 0.8f:0.00}); TAHSİL CİROSU (+) susturuyor",
                            cr.oldu && cr.yok.uzak == 0f && Yakın(cr.var.uzak, koni * 0.8f) && !cr.var.sus && cr.artı.sus, cr.var.uzak);

            var kt = Üç<(float h, float k, float bek)>(ComboTür.KarşılıklıTahsil, () =>
            {
                var a = Düşman(İleri(1.3f));
                Alan("_biriken", 2f);
                float k = Hasar(a, () => Silahla(Kadro.KARŞILIK)); Geç();
                Bekle(Kadro.KARŞILIK, 5f);
                float h = Hasar(a, () => Silahla(Kadro.KONİ));
                return (h, k, _pc.SpecialCooldownOf(Kadro.KARŞILIK));
            });
            failed += Check(ref total, $"KARŞILIKLI TAHSİL (KARŞILIK → KONİ): KARŞILIK'ın yarısı koniye ({kt.yok.h:0.00} → {kt.var.h:0.00} = {koni:0.00} + {kt.var.k:0.00}/2); MAHSUP (+) KARŞILIK'ın beklemesi sıfır",
                            kt.oldu && Yakın(kt.yok.h, koni) && Yakın(kt.var.h, koni + 0.5f * kt.var.k) && kt.var.bek == 5f && kt.artı.bek == 0f, kt.var.h);

            var vh = Üç<(float h, bool yavaş)>(ComboTür.VadeliHaciz, () =>
            {
                Düşman(İleri(1.3f));
                var uzak = Düşman(İleri(9f));                                   // HACİZ'in menzili 7.5: dışında, ×1.5'in içinde
                TahvilBoz(1.1f); Geç();                                            // 2. kademe
                float h = Hasar(uzak, () => Silahla(Kadro.HACİZ));
                return (h, (float)Yavaş.GetValue(uzak) > 0f);
            });
            failed += Check(ref total, $"VADELİ HACİZ (TAHVİL → HACİZ): menzil ×1.5, 2. kademe ×1.4 ({vh.yok.h:0.00} → {vh.var.h:0.00} = {haciz * 1.4f:0.00}); HACİZ ŞERHİ (+) yavaşlatıyor",
                            vh.oldu && vh.yok.h == 0f && Yakın(vh.var.h, haciz * 1.4f) && !vh.var.yavaş && vh.artı.yavaş, vh.var.h);

            var zc = Üç<int>(ComboTür.ZincirlemeCiro, () =>
            {
                var uzak = Düşman(İleri(5f));
                ÇekAt(); Geç();
                var yakın = Düşman(İleri(1.3f)); Düşman(Yer(-2.5f, 1.3f));    // üçüncü: yakının yanında, MÜŞTEREK'in yayı dışında
                Silahla(Kadro.MÜŞTEREK);
                var b = _pc.SonBağ;
                return b == null ? 0 : b.Üyeler.Count + (b.Üyeler.Contains(uzak) && b.Üyeler.Contains(yakın) ? 10 : 0);
            });
            failed += Check(ref total, $"ZİNCİRLEME CİRO (ÇEK → MÜŞTEREK): tek vuruşta çekin çarptığına bağ (üye {zc.yok % 10} → {zc.var % 10}, çekinki ve vurulan {(zc.var >= 10 ? "bağda" : "BAĞDA DEĞİL")}); CİRO SİLSİLESİ (+) üçüncü ({zc.artı % 10})",
                            zc.oldu && zc.yok == 0 && zc.var == 12 && zc.artı == 13, zc.var);

            var mk = Üç<(float ara, float uç, bool bağ, float bek)>(ComboTür.MüşterekKusur, () =>
            {
                var (a, _, ara) = BağKurgusu();
                Düşman(İleri(1.3f)); Geç();
                Bekle(Kadro.MÜŞTEREK, 5f);
                float a0 = a.TimeLeft;
                float h = Hasar(ara, () => Silahla(Kadro.KONİ));
                return (h, a0 - a.TimeLeft, _pc.SonBağ != null, _pc.SpecialCooldownOf(Kadro.MÜŞTEREK));
            });
            failed += Check(ref total, $"MÜŞTEREK KUSUR (MÜŞTEREK → KONİ): bağ kopuyor ({(mk.yok.bağ ? "duruyor" : "YOK")} → {(mk.var.bağ ? "DURUYOR" : "koptu")}), arkadaki uca ve çizgidekine koni vuruşu ({mk.yok.ara:0.00} → {mk.var.ara:0.00}, uç {mk.var.uç:0.00} = {koni:0.00}); RÜCU (+) beklemesi sıfır",
                            mk.oldu && mk.yok.bağ && mk.yok.ara == 0f && mk.yok.uç == 0f && !mk.var.bağ && Yakın(mk.var.ara, koni) && Yakın(mk.var.uç, koni)
                            && mk.var.bek == 5f && mk.artı.bek == 0f, mk.var.ara);

            var ar = Üç<(float uzak, bool sırt)>(ComboTür.ArkadanDolanım, () =>
            {
                var e = Bakan(İleri(1.2f), Vector3.forward);                    // sırtı sana: arkadan
                Silahla(Kadro.ARBİTRAJ); Geç();
                var uzak = Düşman(İleri(2.9f));                                 // KONİ'nin menzili 2.2: dışında, ×1.3'ün içinde
                float h = Hasar(uzak, () => Silahla(Kadro.KONİ));
                return (h, e.SırtDönük);
            });
            failed += Check(ref total, $"ARKADAN DOLANIM (ARBİTRAJ arkadan → KONİ): menzil ×1.3, ×1.3 ({ar.yok.uzak:0.00} → {ar.var.uzak:0.00} = {koni * 1.3f:0.00}); KÖR NOKTA (+) vurulanların sırtı dönük",
                            ar.oldu && ar.yok.uzak == 0f && Yakın(ar.var.uzak, koni * 1.3f) && !ar.var.sırt && ar.artı.sırt, ar.var.uzak);
            // Şart: ARBİTRAJ önden vurduysa combo yok
            YeniKoşu(); _ara = 0.2f;
            Bakan(İleri(1.2f), Vector3.back);
            Silahla(Kadro.ARBİTRAJ); Geç(); Düşman(İleri(1.3f)); Silahla(Kadro.KONİ);
            failed += Check(ref total, "ARKADAN DOLANIM: ARBİTRAJ önden vurduysa combo YOK (şart)", !Yaptı(ComboTür.ArkadanDolanım), 0f);

            var ka = Üç<(float kalan, float hasar, float kirasız)>(ComboTür.KiralıkArsa, () =>
            {
                Silahla(Kadro.KİRA); Geç();
                Silahla(Kadro.İPOTEK);
                var m = İşaretler().FirstOrDefault();
                return m == null ? (-1f, -1f, -1f) : (m.Kalan, m.Hasar, _pc.Kule != null ? _pc.Kule.KirasızKalan : -1f);
            });
            failed += Check(ref total, $"KİRALIK ARSA (KİRA → İPOTEK): gecikme ×0.3 ({ka.yok.kalan:0.00} → {ka.var.kalan:0.00}), ×1.5 ({ka.yok.hasar:0.00} → {ka.var.hasar:0.00}); MUAFİYET (+) kule 4 sn kirasız ({ka.var.kirasız:0} → {ka.artı.kirasız:0})",
                            ka.oldu && ka.yok.kalan > 0f && Yakın(ka.var.kalan, ka.yok.kalan * 0.3f) && Yakın(ka.var.hasar, ka.yok.hasar * 1.5f)
                            && ka.var.kirasız == 0f && ka.artı.kirasız == 4f, ka.var.kalan);

            var vg = Üç<(float h, bool görünmez, float bek)>(ComboTür.VadesiGelmişPozisyon, () =>
            {
                var e = Bakan(İleri(1.2f), Vector3.back);                       // yüzü sana: önden
                TahvilBoz(1.1f); Geç();                                            // 2. kademe
                Bekle(Kadro.ARBİTRAJ, 5f);
                float h = Hasar(e, () => Silahla(Kadro.ARBİTRAJ));
                return (h, _pc.Görünmez, _pc.SpecialCooldownOf(Kadro.ARBİTRAJ));
            });
            failed += Check(ref total, $"VADESİ GELMİŞ POZİSYON (TAHVİL 2. kademe → ARBİTRAJ): önden vuruş arkadan ({vg.yok.h:0.00} → {vg.var.h:0.00} = {arb * _t.arbitrajBackMul:0.00}), sonra görünmez ({vg.yok.görünmez} → {vg.var.görünmez}); ZARAR KES (+) beklemesi sıfır",
                            vg.oldu && Yakın(vg.yok.h, arb * _t.arbitrajFrontMul) && Yakın(vg.var.h, arb * _t.arbitrajBackMul) && !vg.yok.görünmez && vg.var.görünmez
                            && vg.var.bek == 5f && vg.artı.bek == 0f, vg.var.h);
            // Şart: 1. kademede combo yok
            YeniKoşu(); _ara = 0.2f;
            Bakan(İleri(1.2f), Vector3.back);
            TahvilBoz(0.6f); Geç(); Silahla(Kadro.ARBİTRAJ);
            failed += Check(ref total, "VADESİ GELMİŞ POZİSYON: TAHVİL 1. kademedeyse combo YOK (şart)", !Yaptı(ComboTür.VadesiGelmişPozisyon) && !_pc.Görünmez, 0f);
            return failed;
        }

        // ── 3) ÜÇLÜLER (2. bölge) ───────────────────────────────────────────────
        static int Üçlüler(ref int total)
        {
            int failed = 0;
            float koni = Taban(Kadro.KONİ), faizli = Taban(Kadro.FAİZLİ), haciz = Taban(Kadro.HACİZ);
            var iflas = Weapon.All[Kadro.İFLAS];

            var it = Üç<(float h, float saat, float s0, bool devir, float bek)>(ComboTür.İcraTakibi, () =>
            {
                HasatKapat();
                var yakın = Düşman(İleri(1.3f)); Düşman(İleri(5.5f));
                Silahla(Kadro.FAİZLİ); Geç(); Silahla(Kadro.HACİZ); Geç();      // FAİZLİ → HACİZ zaten ALACAK DEVRİ: zincir sürmeli
                Bekle(Kadro.İFLAS, 5f);
                float s0 = _clock.Seconds;
                float h = Hasar(yakın, () => Silahla(Kadro.İFLAS));
                return (h, _clock.Seconds - s0, s0, Yaptı(ComboTür.AlacakDevri), _pc.SpecialCooldownOf(Kadro.İFLAS));
            });
            float İflas(float sn) => iflas.Damage(_t, _clock.AttackDamage) * iflas.DesperationMultiplier(sn, _t);
            failed += Check(ref total, $"⚠️ İCRA TAKİBİ (FAİZLİ → HACİZ → İFLAS): ALACAK DEVRİ'nden sonra zincir sürüyor ({(it.var.devir ? "devir oldu" : "DEVİR YOK")}); İFLAS saatin dörtte birindeymiş gibi ({it.yok.h:0.00} → {it.var.h:0.00} = {İflas(it.var.s0 * 0.25f):0.00}), iki bedel geri ({it.yok.saat:+0.00} → {it.var.saat:+0.00}); TAZMİNAT (+) TASFİYE'nin beklemesi sıfır",
                            it.oldu && it.var.devir && !it.yok.devir && Yakın(it.yok.h, İflas(it.yok.s0)) && Yakın(it.var.h, İflas(it.var.s0 * 0.25f)) && it.var.h > it.yok.h
                            && Yakın(it.yok.saat, 0f) && Yakın(it.var.saat, 2f * _t.faizliSwingCost) && it.var.bek == 5f && it.artı.bek == 0f, it.var.h);

            var sr = Üç<(float h, bool ciro, float arka)>(ComboTür.SenetliRehin, () =>
            {
                var r = Düşman(İleri(1.5f)); var hedef = Düşman(İleri(4f));
                Silahla(Kadro.REHİN); Silahla(Kadro.REHİN); Geç();            // yakala, fırlat (adım)
                ÇekAt(); Geç();                                                 // çek rehineye çarpar (adım)
                var arka = Düşman(Yer(0f, -1.3f));
                float a0 = arka.TimeLeft;
                float h = Hasar(hedef, () => Silahla(Kadro.KONİ));
                return (h, Yaptı(ComboTür.Ciro), a0 - arka.TimeLeft);
            });
            failed += Check(ref total, $"⚠️ SENETLİ REHİN (REHİN → ÇEK → KONİ): çekin çarptığı kümedeki önüne çekilip ×1.5 yiyor ({sr.yok.h:0.00} → {sr.var.h:0.00} = {koni * 1.5f:0.00}), CİRO {(sr.var.ciro ? "TETİKLENDİ" : "tetiklenmedi")}; KAMBİYO (+) 360° (arkadaki {sr.var.arka:0.00} → {sr.artı.arka:0.00})",
                            sr.oldu && sr.yok.h == 0f && Yakın(sr.var.h, koni * 1.5f) && !sr.var.ciro && sr.var.arka == 0f && Yakın(sr.artı.arka, koni * 1.5f), sr.var.h);

            var tt = Üç<(float h, float k, bool vade, float bek)>(ComboTür.TeminatlıTahvil, () =>
            {
                var e = Düşman(İleri(1.3f));
                Alan("_biriken", 2f);
                Silahla(Kadro.KARŞILIK); Geç();
                float biriken = (float)Alan("_sonBirikenHasarı");
                TahvilBoz(0.6f); Geç();                                            // 1. kademe — 0.8 sn'lik pencereye sığan
                Bekle(Kadro.TAHVİL, 5f);
                float h = Hasar(e, () => Silahla(Kadro.FAİZLİ));
                return (h, biriken, Yaptı(ComboTür.VadeFarkı), _pc.SpecialCooldownOf(Kadro.TAHVİL));
            });
            float v1 = Mathf.Max(1.5f, Tahvil.KademeÇarpanı(1));
            failed += Check(ref total, $"⚠️ TEMİNATLI TAHVİL (KARŞILIK → TAHVİL → FAİZLİ): VADE FARKI ×1.25 + birikim ({tt.yok.h:0.00} → {tt.var.h:0.00} = {faizli:0.00}×{v1 * 1.25f:0.00} + {tt.var.k:0.00}), VADE FARKI {(tt.var.vade ? "TETİKLENDİ" : "tetiklenmedi")}; TEMİNAT İADESİ (+) beklemesi sıfır",
                            tt.oldu && Yakın(tt.yok.h, faizli) && tt.var.k > 0f && Yakın(tt.var.h, faizli * v1 * 1.25f + tt.var.k) && !tt.var.vade
                            && tt.var.bek == 5f && tt.artı.bek == 0f, tt.var.h);

            float işaretHasarı = Taban(Kadro.İPOTEK);
            var ot = Üç<(float ara, float iç, bool bağ, bool sus, bool tapu)>(ComboTür.OrtaklığınTasfiyesi, () =>
            {
                var a = Düşman(İleri(1.5f)); var b = Düşman(Yer(1.5f, 0f));
                Silahla(Kadro.MÜŞTEREK);
                _pGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
                Silahla(Kadro.MÜŞTEREK);                                        // bağ (adım)
                Ortaya();
                a.transform.position = Yer(-3f, -1.5f); b.transform.position = Yer(3f, -1.5f);
                var ara = Düşman(Yer(0f, -1.5f));
                Geç();
                var içte = Düşman(İleri(Weapon.All[Kadro.İPOTEK].Range(_t)));
                Silahla(Kadro.İPOTEK); Geç();                                   // işaret (adım)
                Düşman(İleri(1.3f));
                float i0 = içte.TimeLeft;
                float h = Hasar(ara, () => Silahla(Kadro.KONİ));
                return (h, i0 - içte.TimeLeft, _pc.SonBağ != null, a.Susturulmuş, Yaptı(ComboTür.Tapuİptali));
            });
            failed += Check(ref total, $"⚠️ ORTAKLIĞIN TASFİYESİ (MÜŞTEREK → İPOTEK → KONİ): işaret ×1.5 patlıyor ({ot.yok.iç:0.00} → {ot.var.iç:0.00} = {işaretHasarı * 1.5f:0.00}), bağ kopuyor, aradakine koni vuruşu ({ot.yok.ara:0.00} → {ot.var.ara:0.00}), TAPU İPTALİ {(ot.var.tapu ? "TETİKLENDİ" : "tetiklenmedi")}; KAYYUM (+) bağlılar susuyor",
                            ot.oldu && ot.yok.iç == 0f && ot.yok.ara == 0f && ot.yok.bağ && Yakın(ot.var.iç, işaretHasarı * 1.5f) && Yakın(ot.var.ara, koni) && !ot.var.bağ
                            && !ot.var.tapu && !ot.var.sus && ot.artı.sus, ot.var.iç);

            var kk = Üç<(float h, float kule, bool sus)>(ComboTür.KiraKontratı, () =>
            {
                Silahla(Kadro.KİRA); Geç();                                     // kule nişana (5.5 ileri)
                Düşman(İleri(1.2f));
                Silahla(Kadro.ARBİTRAJ); Geç();
                var uzak = Düşman(İleri(5f));
                float h = Hasar(uzak, () => Silahla(Kadro.HACİZ));
                uzak.transform.position = Yer(0f, -10f);                        // kulenin menzilinin çok dışına
                Düşman(Yer(1.5f, 5.5f));                                        // kulenin dibinde: kilit yoksa kule buna ateş eder
                float k = _pc.Kule != null ? Hasar(uzak, () => _pc.Kule.Step(0.05f)) : -1f;
                return (h, k, uzak.Susturulmuş);
            });
            failed += Check(ref total, $"⚠️ KİRA KONTRATI (KİRA → ARBİTRAJ → HACİZ): ×1.5 ({kk.yok.h:0.00} → {kk.var.h:0.00} = {haciz * 1.5f:0.00}), kule menzil dışındaki hedefe kilitli ({kk.yok.kule:0.00} → {kk.var.kule:0.00}); İHTARNAME (+) susturuyor",
                            kk.oldu && Yakın(kk.yok.h, haciz) && Yakın(kk.var.h, haciz * 1.5f) && kk.yok.kule == 0f && kk.var.kule > 0f && !kk.var.sus && kk.artı.sus, kk.var.h);

            // 1. bölgede üçlü yok: aynı sıra (KONİ'siz) ikiliye düşer — SENETLİ REHİN değil CİRO
            YeniKoşu(); _ara = 0.2f;
            Düşman(İleri(1.5f)); Düşman(İleri(4f));
            Silahla(Kadro.REHİN); Silahla(Kadro.REHİN); Geç(); ÇekAt(); Geç(); Düşman(İleri(1.3f)); Silahla(Kadro.KONİ);
            failed += Check(ref total, "üçlüler 1. bölgede kapalı: REHİN → ÇEK → KONİ orada CİRO", !Yaptı(ComboTür.SenetliRehin) && Yaptı(ComboTür.Ciro), 0f);
            return failed;
        }

        // ── 4) PARTİ 2 — yapı ───────────────────────────────────────────────────
        static int Parti2Yapısı(ref int total)
        {
            var p2 = Enumerable.Range((int)ComboTür.AcizVesikası, 18).Select(i => (ComboTür)i).ToList();
            YeniKoşu();
            bool kapalı = p2.All(c => !Combo.Kullanılır(c, 4));
            bool açanlar = p2.All(c =>
            {
                var t = Combo.Bu(c);
                if (!(t.Açan is SilahYükseltme y)) return false;
                bool üç = t.Sıra().Length == 3;
                return SilahSunağı.Ender[(int)y] == (üç ? Enderlik.Efsanevi : Enderlik.Epik) && SilahSunağı.Silahı(y) == t.Sıra()[0]
                       && t.MinBölge == (üç ? 1 : 0) && !string.IsNullOrEmpty(t.ArtıAd) && SilahSunağı.NeYapar[(int)y].Contains("COMBO açar")
                       && Combo.Açtığı(y) == t;
            });
            bool tekTek = p2.Select(c => Combo.Bu(c).Açan).Distinct().Count() == 18;
            foreach (var c in p2) SilahSunağı.Al(Combo.Bu(c).Açan.Value, -1);
            bool açık = p2.All(c => Combo.Kullanılır(c, 4));
            int ikili = p2.Count(c => Combo.Bu(c).Sıra().Length == 2);
            return Check(ref total, $"⚠️ Parti 2: {ikili} ikili + {p2.Count - ikili} üçlü; her biri ilk silahının mevcut yükseltmesiyle açılıyor (ikili EPİK, üçlü EFSANEVİ, " +
                                    $"yükseltmenin açıklaması combo'yu söylüyor, her yükseltme tek combo); açansız kapalı ({(kapalı ? "kapalı" : "AÇIK")}), açanla açık ({(açık ? "açık" : "KAPALI")})",
                         ikili == 12 && açanlar && tekTek && kapalı && açık, ikili);
        }

        // ── 5) PARTİ 2 — ikililer (açan alınmış) ─────────────────────────────────
        static int Parti2İkililer(ref int total)
        {
            int failed = 0;
            float koni = Taban(Kadro.KONİ), haciz = Taban(Kadro.HACİZ), çek = Taban(Kadro.ÇEK);
            float arbArka = Taban(Kadro.ARBİTRAJ) * _t.arbitrajBackMul;

            var av = Üç<(float h, float kalan, float tam, bool sırt)>(ComboTür.AcizVesikası, () =>
            {
                var e = Düşman(İleri(1.3f));
                Silahla(Kadro.KONİ); Geç();                                     // YORGUNLUK: KONİ yorar
                HazırlıkKur(e, 0.1f);
                float h = Hasar(e, () => Silahla(Kadro.İFLAS));
                return (h, e.KalanHazırlık, e.CurrentWindup, e.SırtDönük);
            });
            failed += Check(ref total, $"ACİZ VESİKASI (KONİ → İFLAS): ×1.5 ({av.yok.h:0.00} → {av.var.h:0.00}), yorgunun hazırlığı baştan ({av.yok.kalan:0.00} → {av.var.kalan:0.00} = {av.var.tam:0.00} sn); MAL BEYANI (+) sırtı dönük",
                            av.oldu && Yakın(av.var.h, av.yok.h * 1.5f) && Yakın(av.yok.kalan, 0.1f) && av.var.tam > 0.15f && Yakın(av.var.kalan, av.var.tam)
                            && !av.var.sırt && av.artı.sırt, av.var.kalan);

            var kr = Üç<(float uzak, float bek)>(ComboTür.KonutKredisi, () =>
            {
                var e = Düşman(İleri(1.3f));
                Silahla(Kadro.FAİZLİ); Geç();
                Bekle(Kadro.İPOTEK, 5f);
                Silahla(Kadro.İPOTEK);
                var m = İşaretler().FirstOrDefault();
                if (m == null) return (-1f, -1f);
                e.transform.position = Yer(3f, 1.3f);                          // borçlu kaçar
                m.Step(0.01f);
                Vector3 d = m.transform.position - e.transform.position; d.y = 0f;
                return (d.magnitude, _pc.SpecialCooldownOf(Kadro.İPOTEK));
            });
            failed += Check(ref total, $"KONUT KREDİSİ (FAİZLİ → İPOTEK): işaret FAİZLİ'nin vurduğuna yapışık, kaçınca peşinden ({kr.yok.uzak:0.00} → {kr.var.uzak:0.00} birim); ERKEN ÖDEME (+) İPOTEK'in beklemesi sıfır",
                            kr.oldu && kr.yok.uzak > 1f && kr.var.uzak >= 0f && kr.var.uzak < 0.01f && kr.var.bek == 5f && kr.artı.bek == 0f, kr.var.uzak);

            var hm = Üç<(float h, bool sus)>(ComboTür.HacizMemuru, () =>
            {
                var uzak = Bakan(İleri(5.5f), Vector3.back);                    // yüzü sana
                Silahla(Kadro.HACİZ); Geç();
                float h = Hasar(uzak, () => Silahla(Kadro.ARBİTRAJ));
                return (h, uzak.Susturulmuş);
            });
            failed += Check(ref total, $"HACİZ MEMURU (HACİZ → ARBİTRAJ): 5.5 birimdekinin arkasına geçip arkadan vuruyor ({hm.yok.h:0.00} → {hm.var.h:0.00} = {arbArka:0.00}); TEBLİĞ (+) susturuyor",
                            hm.oldu && hm.yok.h == 0f && Yakın(hm.var.h, arbArka) && !hm.var.sus && hm.artı.sus, hm.var.h);

            float ipotekR = Weapon.All[Kadro.İPOTEK].Range(_t);
            var tv = Üç<(int üye, float itme)>(ComboTür.TapuTevhidi, () =>
            {
                var a = Düşman(İleri(ipotekR)); Düşman(Yer(0.8f, ipotekR));   // ikisi de işaretin içinde
                Silahla(Kadro.İPOTEK); Geç();
                Düşman(İleri(1.3f));
                float i0 = İtmesi(a);
                Silahla(Kadro.MÜŞTEREK);
                var bağ = _pc.SonBağ;
                return (bağ == null ? 0 : bağ.Üyeler.Count, İtmesi(a) - i0);
            });
            failed += Check(ref total, $"TAPU TEVHİDİ (İPOTEK → MÜŞTEREK): işaretin içindeki ikisi vurulanla tek bağda (üye {tv.yok.üye} → {tv.var.üye}); TOPLULAŞTIRMA (+) ortaya çekiyor (itme {tv.var.itme:0.0} → {tv.artı.itme:0.0})",
                            tv.oldu && tv.yok.üye == 0 && tv.var.üye == 3 && tv.artı.üye == 3 && tv.var.itme == 0f && tv.artı.itme > 1f, tv.var.üye);

            var id = Üç<(float h, float s0, bool yavaş)>(ComboTür.İflasDairesi, () =>
            {
                HasatKapat();
                var e = Düşman(İleri(1.3f));
                Silahla(Kadro.İFLAS); Geç();
                float s0 = _clock.Seconds;
                float h = Hasar(e, () => Silahla(Kadro.HACİZ));                // dibinde: ölü bölgede
                return (h, s0, (float)Yavaş.GetValue(e) > 0f);
            }, saat: 30f);
            float İflasÇ(float sn) => Mathf.Max(1f, Weapon.All[Kadro.İFLAS].DesperationMultiplier(sn, _t));
            failed += Check(ref total, $"İFLAS DAİRESİ (İFLAS → HACİZ): ölü bölge yok, saat {id.var.s0:0} sn'de İFLAS'ın çarpanıyla ({id.yok.h:0.00} → {id.var.h:0.00} = {haciz:0.00}×{İflasÇ(id.var.s0):0.00}); İFLAS İLANI (+) yavaşlatıyor",
                            id.oldu && id.yok.h == 0f && İflasÇ(id.var.s0) > 1.5f && Yakın(id.var.h, haciz * İflasÇ(id.var.s0)) && !id.var.yavaş && id.artı.yavaş, id.var.h);

            var mr = Üç<(int üye, bool sus)>(ComboTür.MüşterekRehin, () =>
            {
                Düşman(İleri(1.5f)); Düşman(İleri(4f)); Düşman(Yer(1.5f, 4.5f));   // rehine · çarptığı · DOMİNO'nun sektiği
                Silahla(Kadro.REHİN); Silahla(Kadro.REHİN); Geç();            // yakala, fırlat (adım)
                Düşman(İleri(1.3f));
                Silahla(Kadro.MÜŞTEREK);
                var bağ = _pc.SonBağ;
                return (bağ == null ? 0 : bağ.Üyeler.Count, bağ != null && bağ.Üyeler.All(e => e.Susturulmuş));
            });
            failed += Check(ref total, $"MÜŞTEREK REHİN (REHİN → MÜŞTEREK): rehine, çarptığı ve sektiği vurulanla tek bağda (üye {mr.yok.üye} → {mr.var.üye}); TEMİNAT TAMAMLAMA (+) bağlılar susuyor",
                            mr.oldu && mr.yok.üye == 0 && mr.var.üye == 4 && !mr.var.sus && mr.artı.sus, mr.var.üye);

            var ib = Üç<(float h, int havada, float bek)>(ComboTür.İbraz, () =>
            {
                Düşman(İleri(5f));
                ÇekAt(); Geç();                                                 // çek havada
                var e = Düşman(İleri(1.3f));
                Bekle(Kadro.İFLAS, 5f);
                float h = Hasar(e, () => Silahla(Kadro.İFLAS));
                return (h, CekUcusu.Uçanlar.Count, _pc.SpecialCooldownOf(Kadro.İFLAS));
            });
            failed += Check(ref total, $"İBRAZ (ÇEK → İFLAS): havadaki çek İFLAS'ın vurduğuna dönüp ×1.5 iniyor ({ib.yok.h:0.00} → {ib.var.h:0.00} = +{1.5f * çek:0.00}, havada {ib.yok.havada} → {ib.var.havada}); KAMBİYO TAKİBİ (+) İFLAS'ın beklemesi sıfır",
                            ib.oldu && ib.yok.havada == 1 && ib.var.havada == 0 && Yakın(ib.var.h, ib.yok.h + 1.5f * çek) && ib.var.bek == 5f && ib.artı.bek == 0f, ib.var.h);

            var ta = Üç<(float kalan, float hasar, float yarıçap, float biriken, float bek)>(ComboTür.TeminatAkçesi, () =>
            {
                Düşman(İleri(1.3f));
                Alan("_biriken", 2f);
                Silahla(Kadro.KARŞILIK); Geç();
                float b = (float)Alan("_sonBirikenHasarı");
                Bekle(Kadro.KARŞILIK, 5f);
                Silahla(Kadro.İPOTEK);
                var m = İşaretler().FirstOrDefault();
                return m == null ? (-1f, -1f, -1f, b, -1f) : (m.Kalan, m.Hasar, m.Yarıçap, b, _pc.SpecialCooldownOf(Kadro.KARŞILIK));
            });
            failed += Check(ref total, $"TEMİNAT AKÇESİ (KARŞILIK → İPOTEK): gecikme yarı ({ta.yok.kalan:0.00} → {ta.var.kalan:0.00}), birikim hasara ({ta.yok.hasar:0.00} → {ta.var.hasar:0.00} = +{ta.var.biriken:0.00}), 2 sn birikim alanı ×1.5 ({ta.yok.yarıçap:0.00} → {ta.var.yarıçap:0.00}); DEPO (+) beklemesi sıfır",
                            ta.oldu && ta.var.biriken > 0f && Yakın(ta.var.kalan, ta.yok.kalan * 0.5f) && Yakın(ta.var.hasar, ta.yok.hasar + ta.var.biriken)
                            && Yakın(ta.var.yarıçap, ta.yok.yarıçap * 1.5f) && ta.var.bek == 5f && ta.artı.bek == 0f, ta.var.hasar);

            var ks = Üç<(float kirasız, float atış, float bek)>(ComboTür.KiraSertifikası, () =>
            {
                Düşman(İleri(1.3f));
                TahvilBoz(1.1f); Geç();                                        // 2. kademe
                Bekle(Kadro.TAHVİL, 5f);
                Silahla(Kadro.KİRA);
                var k = _pc.Kule;
                if (k == null) return (-1f, -1f, -1f);
                float kirasız = k.KirasızKalan;
                var hedef = Düşman(Yer(1f, Weapon.All[Kadro.KİRA].Range(_t)));
                float atış = Hasar(hedef, () => k.Step(0.05f));
                return (kirasız, atış, _pc.SpecialCooldownOf(Kadro.TAHVİL));
            });
            float v2 = Tahvil.KademeÇarpanı(2);
            failed += Check(ref total, $"KİRA SERTİFİKASI (TAHVİL 2. kademe → KİRA): kule 4 sn kirasız ({ks.yok.kirasız:0.0} → {ks.var.kirasız:0.0}), atış ×{v2} ({ks.yok.atış:0.00} → {ks.var.atış:0.00}); ANAPARA İADESİ (+) beklemesi sıfır",
                            ks.oldu && ks.yok.kirasız == 0f && Yakın(ks.var.kirasız, 4f) && ks.yok.atış > 0f && Yakın(ks.var.atış, ks.yok.atış * v2)
                            && ks.var.bek == 5f && ks.artı.bek == 0f, ks.var.atış);

            var hi = Üç<(float uç, float hasat)>(ComboTür.Hacizİştiraki, () =>
            {
                var (a, _, _) = BağKurgusu();
                Düşman(İleri(5.5f)); Geç();
                float a0 = a.TimeLeft;
                float hs = Hasat(() => Silahla(Kadro.HACİZ));
                return (a0 - a.TimeLeft, hs);
            });
            failed += Check(ref total, $"HACZE İŞTİRAK (MÜŞTEREK → HACİZ): HACİZ uzaktakine, bağlıya da ayrı tam vuruş ({hi.yok.uç:0.00} → {hi.var.uç:0.00} = {haciz:0.00}); İŞTİRAK PAYI (+) hasat ×{hi.artı.hasat / Mathf.Max(0.0001f, hi.var.hasat):0.00}",
                            hi.oldu && hi.yok.uç == 0f && Yakın(hi.var.uç, haciz) && Yakın(hi.artı.hasat / Mathf.Max(0.0001f, hi.var.hasat), 2f, 0.05f), hi.var.uç);

            var hl = Üç<(float h, bool görünmez)>(ComboTür.Hileliİflas, () =>
            {
                var e = Bakan(İleri(1.2f), Vector3.forward);                    // sırtı sana: arkadan
                Silahla(Kadro.ARBİTRAJ); Geç();
                float h = Hasar(e, () => Silahla(Kadro.İFLAS));
                return (h, _pc.Görünmez);
            });
            failed += Check(ref total, $"HİLELİ İFLAS (ARBİTRAJ arkadan → İFLAS): iki kez iniyor ({hl.yok.h:0.00} → {hl.var.h:0.00}); PERDE (+) görünmez",
                            hl.oldu && Yakın(hl.var.h, 2f * hl.yok.h) && !hl.var.görünmez && hl.artı.görünmez, hl.var.h);

            var kt = Üç<(float h, float saat, float doyma, float bek)>(ComboTür.KiraTahsilatı, () =>
            {
                HasatKapat();
                Silahla(Kadro.KİRA); Geç();                                     // kule nişana
                float kr2 = Weapon.All[Kadro.KİRA].Range(_t);
                var a = Düşman(Yer(1.5f, kr2)); Boş(Yer(-1.5f, kr2));          // boş olan vuruşta ölür → STOPAJ
                Bekle(Kadro.ÇEK, 5f);
                float s0 = _clock.Seconds, doyma = _clock.HasatDoyması;
                float h = Hasar(a, () => Silahla(Kadro.ÇEK));
                return (h, _clock.Seconds - s0, doyma, _pc.SpecialCooldownOf(Kadro.ÇEK));
            });
            failed += Check(ref total, $"KİRA TAHSİLATI (KİRA → ÇEK): kulenin menzilindekine çek vuruşu ({kt.yok.h:0.00} → {kt.var.h:0.00} = {çek:0.00}), öldürdüğü STOPAJ ödüyor ({kt.yok.saat:+0.00} → {kt.var.saat:+0.00} sn); VERGİ İADESİ (+) ÇEK'in beklemesi sıfır",
                            kt.oldu && kt.yok.h == 0f && Yakın(kt.var.h, çek) && Yakın(kt.yok.saat, 0f) && Yakın(kt.var.saat, 1.5f * kt.var.doyma)
                            && kt.var.bek == 5f && kt.artı.bek == 0f, kt.var.saat);
            return failed;
        }

        // ── 6) PARTİ 2 — üçlüler (2. bölge, açan EFSANEVİ alınmış) ──────────────
        static int Parti2Üçlüler(ref int total)
        {
            int failed = 0;
            float koni = Taban(Kadro.KONİ), haciz = Taban(Kadro.HACİZ), çek = Taban(Kadro.ÇEK), rehin = Taban(Kadro.REHİN), işaret = Taban(Kadro.İPOTEK);
            var iflas = Weapon.All[Kadro.İFLAS];
            float İflas(float sn) => iflas.Damage(_t, _clock.AttackDamage) * iflas.DesperationMultiplier(sn, _t);

            var ih = Üç<(bool tuttu, float h, float b, float bek)>(ComboTür.İstihkak, () =>
            {
                var uzak = Düşman(İleri(5.5f)); Düşman(İleri(1.3f));
                Silahla(Kadro.HACİZ); Geç();                                    // DAR ÖLÜ BÖLGE: ölü bölge 1.6 — dipteki hâlâ içinde
                Alan("_biriken", 2f);
                Silahla(Kadro.KARŞILIK); Geç();
                float b = (float)Alan("_sonBirikenHasarı");
                Bekle(Kadro.REHİN, 5f);
                float h = Hasar(uzak, () => Silahla(Kadro.REHİN));
                return (_pc.Rehine == uzak, h, b, _pc.SpecialCooldownOf(Kadro.REHİN));
            });
            failed += Check(ref total, $"⚠️ İSTİHKAK (HACİZ → KARŞILIK → REHİN): HACİZ'in vurduğu 5.5 birimden çekilip yakalanıyor ({(ih.var.tuttu ? "tuttu" : "TUTMADI")}), birikim ona ({ih.yok.h:0.00} → {ih.var.h:0.00} = {ih.var.b:0.00} + {0.5f * rehin:0.00}); İSTİRDAT (+) beklemesi sıfır",
                            ih.oldu && !ih.yok.tuttu && ih.yok.h == 0f && ih.var.tuttu && Yakın(ih.var.h, ih.var.b + 0.5f * rehin) && ih.var.bek == 5f && ih.artı.bek == 0f, ih.var.h);

            var iz = Üç<(float iç, float h, float s0, float itme, bool ibraz)>(ComboTür.İpotekZinciri, () =>
            {
                SilahSunağı.Al(SilahYükseltme.KarşılıksızArtı, -1);              // İBRAZ (ÇEK → İFLAS) da açık: üçlü önce gelmeli
                var içte = Düşman(İleri(Weapon.All[Kadro.İPOTEK].Range(_t)));
                Silahla(Kadro.İPOTEK); Geç();                                   // işaret (adım)
                ÇekAt(); Geç();                                                 // çek içtekine çarpar (adım)
                var kenar = Düşman(İleri(Weapon.All[Kadro.İPOTEK].Range(_t)) + Vector3.right);   // merkezdekinin itilecek yönü yok
                var e = Düşman(İleri(1.3f));
                float i0 = içte.TimeLeft, p0 = İtmesi(kenar), s0 = _clock.Seconds;
                float h = Hasar(e, () => Silahla(Kadro.İFLAS));
                return (i0 - içte.TimeLeft, h, s0, İtmesi(kenar) - p0, Yaptı(ComboTür.İbraz));
            }, saat: 128f);                                                     // 300 sn'de yarı saat de güvenli eşiğin üstü — fark görünmüyor
            failed += Check(ref total, $"⚠️ İPOTEK ZİNCİRİ (İPOTEK → ÇEK → İFLAS): İFLAS saatin yarısındaymış gibi ({iz.yok.h:0.00} → {iz.var.h:0.00} = {İflas(iz.var.s0 * 0.5f):0.00}), işaret hemen patlıyor ve içindekine İFLAS da iniyor ({iz.yok.iç:0.00} → {iz.var.iç:0.00} = {işaret:0.00} + {iz.var.h:0.00}), İBRAZ {(iz.var.ibraz ? "TETİKLENDİ" : "tetiklenmedi")}; TAHLİYE EMRİ (+) itiyor ({iz.var.itme:0.0} → {iz.artı.itme:0.0})",
                            iz.oldu && iz.yok.iç == 0f && iz.var.h > iz.yok.h * 1.3f && Yakın(iz.var.h, İflas(iz.var.s0 * 0.5f)) && Yakın(iz.var.iç, işaret + iz.var.h) && !iz.var.ibraz && iz.artı.itme > iz.var.itme + 1f, iz.var.iç);

            var ie = Üç<(float h, bool vade, bool yavaş)>(ComboTür.İflasErtelemesi, () =>
            {
                var e = Düşman(İleri(1.3f));
                Silahla(Kadro.İFLAS); Geç(); TahvilBoz(0.6f); Geç();             // 1. kademe
                float h = Hasar(e, () => Silahla(Kadro.HACİZ));                // dibinde
                return (h, Yaptı(ComboTür.VadeliHaciz), (float)Yavaş.GetValue(e) > 0f);
            });
            float v2 = Tahvil.KademeÇarpanı(2);
            failed += Check(ref total, $"⚠️ İFLAS ERTELEMESİ (İFLAS → TAHVİL 1. kademe → HACİZ): ölü bölge yok, bir üst kademeyle ({ie.yok.h:0.00} → {ie.var.h:0.00} = {haciz:0.00}×{v2}), VADELİ HACİZ {(ie.var.vade ? "TETİKLENDİ" : "tetiklenmedi")}; TEDBİR KARARI (+) yavaşlatıyor",
                            ie.oldu && ie.yok.h == 0f && Yakın(ie.var.h, haciz * v2) && !ie.var.vade && !ie.var.yavaş && ie.artı.yavaş, ie.var.h);

            var ps = Üç<(bool tuttu, float atış, float bek)>(ComboTür.ProtestoluSenet, () =>
            {
                var uzak = Düşman(İleri(5f));
                ÇekAt(); Geç();                                                 // çek uzaktakine (adım)
                var yakın = Düşman(İleri(1.3f));
                Silahla(Kadro.ARBİTRAJ); Geç();
                Bekle(Kadro.ÇEK, 5f);
                Silahla(Kadro.REHİN);                                           // yakala
                bool tuttu = _pc.Rehine == uzak;
                float bek = _pc.SpecialCooldownOf(Kadro.ÇEK);
                if (tuttu) { Enemy.All.Remove(yakın); Object.DestroyImmediate(yakın.gameObject); }   // fırlatma yolu boş kalsın
                var hedef = Düşman(İleri(4f));
                float atış = Hasar(hedef, () => Silahla(Kadro.REHİN));         // fırlat
                return (tuttu, atış, bek);
            });
            failed += Check(ref total, $"⚠️ PROTESTOLU SENET (ÇEK → ARBİTRAJ → REHİN): çekin vurduğu 5 birimden yakalanıyor ({(ps.var.tuttu ? "tuttu" : "TUTMADI")}), fırlatması ×2 ({ps.yok.atış:0.00} → {ps.var.atış:0.00}); ŞERH (+) ÇEK'in beklemesi sıfır",
                            ps.oldu && !ps.yok.tuttu && ps.var.tuttu && ps.yok.atış > 0f && Yakın(ps.var.atış, 2f * ps.yok.atış) && ps.var.bek == 5f && ps.artı.bek == 0f, ps.var.atış);

            var vs = Üç<(float h, float arka, float i, float hasat, bool masa)>(ComboTür.VadeSonuTahsilatı, () =>
            {
                var e = Düşman(İleri(1.3f));
                TahvilBoz(1.1f); Geç();                                        // 2. kademe
                float i = Hasar(e, () => Silahla(Kadro.İFLAS)); Geç();
                var arka = Düşman(Yer(0f, -1.3f));
                float a0 = arka.TimeLeft, h = 0f;
                float hs = Hasat(() => h = Hasar(e, () => Silahla(Kadro.KONİ)));
                return (h, a0 - arka.TimeLeft, i, hs, Yaptı(ComboTür.İflasMasası));
            });
            float beklenen = koni * v2 + 0.5f * vs.var.i;
            failed += Check(ref total, $"⚠️ VADE SONU TAHSİLATI (TAHVİL 2. kademe → İFLAS → KONİ): koni 360° ×{v2} + İFLAS'ın yerinde patlama (öndeki {vs.yok.h:0.00} → {vs.var.h:0.00}, arkadaki {vs.yok.arka:0.00} → {vs.var.arka:0.00} = {beklenen:0.00}), İFLAS MASASI {(vs.var.masa ? "TETİKLENDİ" : "tetiklenmedi")}; TAHSİL HARCI (+) hasat ×{vs.artı.hasat / Mathf.Max(0.0001f, vs.var.hasat):0.00}",
                            vs.oldu && Yakın(vs.yok.h, koni) && vs.yok.arka == 0f && Yakın(vs.var.h, beklenen) && Yakın(vs.var.arka, beklenen) && !vs.var.masa
                            && Yakın(vs.artı.hasat / Mathf.Max(0.0001f, vs.var.hasat), 2f, 0.05f), vs.var.h);

            var sa = Üç<(float uç, bool sırt)>(ComboTür.SpekülatifAtak, () =>
            {
                var e = Düşman(İleri(1.2f));
                Silahla(Kadro.ARBİTRAJ); Geç();
                e.transform.position = Yer(-6f, 0f);                            // yoldan çekil
                var (a, _, _) = BağKurgusu();                                   // iki MÜŞTEREK isabeti: bağ (adım)
                Geç();
                float a0 = a.TimeLeft;
                Silahla(Kadro.ÇEK);
                return (a0 - a.TimeLeft, a.SırtDönük);
            });
            failed += Check(ref total, $"⚠️ SPEKÜLATİF ATAK (ARBİTRAJ → MÜŞTEREK → ÇEK): bağlıya çek vuruşu arkadan ({sa.yok.uç:0.00} → {sa.var.uç:0.00} = {çek * _t.arbitrajBackMul:0.00}); KISA SATIŞ (+) bağlıların sırtı dönük",
                            sa.oldu && sa.yok.uç == 0f && Yakın(sa.var.uç, çek * _t.arbitrajBackMul) && !sa.var.sırt && sa.artı.sırt, sa.var.uç);
            return failed;
        }

        // ── yardımcılar ─────────────────────────────────────────────────────────
        static void Temizle()
        {
            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            foreach (var u in CekUcusu.Uçanlar.ToArray()) if (u != null) Object.DestroyImmediate(u.gameObject);
            CekUcusu.Uçanlar.Clear();
            foreach (var b in MusterekBag.Tümü.ToArray()) if (b != null) b.Bitir();
            MusterekBag.Tümü.Clear();
            foreach (var k in KiraKulesi.Tümü.ToArray()) if (k != null) k.Bitir();
            KiraKulesi.Tümü.Clear();
            foreach (var m in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include)) Object.DestroyImmediate(m.gameObject);
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include)) Object.DestroyImmediate(a.gameObject);
            foreach (var a in Altar.All.ToArray()) if (a != null) Object.DestroyImmediate(a.gameObject);
            Altar.ResetAll();
            GroundMark.ResetLiens();
        }

        static int Check(ref int total, string name, bool ok, float actual)
        {
            total++;
            if (ok) Debug.Log($"  ✅ {name}");
            else Debug.LogError($"  ❌ {name}  (gerçek: {actual})");
            return ok ? 0 : 1;
        }
    }
}
