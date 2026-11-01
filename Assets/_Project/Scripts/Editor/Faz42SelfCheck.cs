using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 42 — Parti 1: 20 direkt combo (15 ikili + 5 üçlü; Enes'in onayı 2026-10-25) · Parti 2: 18 açılır (12 ikili EPİK, 6 üçlü
    /// EFSANEVİ yükseltmeyle; onay 2026-10-27) · Parti 3–4: 60 satın alınır (COMBO sunağı; onay 2026-10-28, 10-29) — docs/COMBOLAR.md. Her combo ETKİ olarak, üç kurguda: pencere geçmiş (combo yok) ·
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
                                  İtme = typeof(Enemy).GetField("_push", Gizli), YavaşÇarpan = typeof(Enemy).GetField("_yavaşÇarpan", Gizli);
        static readonly MethodInfo Al = typeof(Altar).GetMethod("Buy", Gizli);

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
            failed += Parti3Yapısı(ref total);
            failed += Parti3İkililer(ref total);
            failed += Parti3Üçlüler(ref total);
            failed += Parti4Yapısı(ref total);
            failed += Parti4İkililer(ref total);
            failed += Parti4Üçlüler(ref total);
            failed += Sınırları(ref total);

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
        static void Silahla(int silah) { _pc.SetWeapon(silah); Vur.Invoke(_pc, null); Uçur(); }
        /// <summary>ÇİFT İMZA'nın havadaki mermilerini sonuna kadar uçur (Faz 43b — mermi artık uçuyor).</summary>
        static void Uçur() { for (int n = 0; n < 500 && ImzaMermisi.Uçanlar.Count > 0; n++) foreach (var m in ImzaMermisi.Uçanlar.ToArray()) m.Step(0.02f); }
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
        /// <summary>ÇİFT İMZA'nın tek mermisi (Faz 43): önündeki hatta ilk düşmana anında iner — adım yazıldı.</summary>
        static void İmzaAt() => Silahla(Kadro.İMZA);
        static readonly PropertyInfo SavurmaSüresi = typeof(PlayerController).GetProperty("SavurmaSüresi", Gizli);
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
                if (Combo.Bu(c).Satın) Combo.SatınAl(c, -1);                              // Parti 3: satın alınmış
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
            bool sayı = Combo.Sayı == 120 && System.Enum.GetValues(typeof(ComboTür)).Length == Combo.Sayı;
            int ikili = yeni.Count(c => Combo.Bu(c).Sıra().Length == 2), üçlü = yeni.Count(c => Combo.Bu(c).Sıra().Length == 3);
            bool direkt = yeni.All(c => !Combo.Bu(c).Açan.HasValue && !string.IsNullOrEmpty(Combo.Bu(c).ArtıAd)
                                        && Combo.Bu(c).MinBölge == (Combo.Bu(c).Sıra().Length == 3 ? 1 : 0));
            var sıralar = Combo.Tümü.Select(t => string.Join(">", t.Sıra())).ToList();
            bool eşsiz = sıralar.Distinct().Count() == sıralar.Count;
            YeniKoşu();
            foreach (var t in Combo.Tümü) if (t.Açan is SilahYükseltme y) SilahSunağı.Al(y, -1);   // açılırlar da sayılsın
            for (int i = 0; i < Combo.Sayı; i++) if (Combo.Bu((ComboTür)i).Satın) Combo.SatınAl((ComboTür)i, -1);   // satın alınırlar da
            // Bir üçlünün başı olan ikili bitince zincir silinmiyor, adım `_isabetYeri`'nden yazılıyor (ComboTetikle) — bugün
            // yalnızca ALACAK DEVRİ ve o koni vuruşuyla bitiyor. Yenisi gelirse burası kırmızı: bitiş yolunu kontrol et.
            var baş = Enumerable.Range(0, Combo.Sayı).Select(i => (ComboTür)i).Where(c => Combo.ÜçlüyeDevam(c, 4)).ToList();
            bool başlar = baş.Count == 1 && baş[0] == ComboTür.AlacakDevri && !Combo.ÜçlüyeDevam(ComboTür.AlacakDevri, 0);
            return Check(ref total, $"⚠️ Parti 1: {Combo.Sayı} combo (= 120), yeni {ikili} ikili + {üçlü} üçlü, hepsi direkt ve \"+\"lı, üçlüler 2. bölgeden; " +
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
                İmzaAt(); Geç();
                Düşman(İleri(1.3f));
                float h = Hasar(uzak, () => Silahla(Kadro.KONİ));
                return (h, uzak.Susturulmuş);
            });
            failed += Check(ref total, $"CİRO (İMZA → KONİ): merminin değdiği yerde koninin %80'i ({cr.yok.uzak:0.00} → {cr.var.uzak:0.00} = {koni * 0.8f:0.00}); TAHSİL CİROSU (+) susturuyor",
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
                İmzaAt(); Geç();
                var yakın = Düşman(İleri(1.3f)); Düşman(Yer(-2.5f, 1.3f));    // üçüncü: yakının yanında, MÜŞTEREK'in yayı dışında
                Silahla(Kadro.MÜŞTEREK);
                var b = _pc.SonBağ;
                return b == null ? 0 : b.Üyeler.Count + (b.Üyeler.Contains(uzak) && b.Üyeler.Contains(yakın) ? 10 : 0);
            });
            failed += Check(ref total, $"ZİNCİRLEME CİRO (İMZA → MÜŞTEREK): tek vuruşta merminin vurduğuna bağ (üye {zc.yok % 10} → {zc.var % 10}, merminin ve vurulan {(zc.var >= 10 ? "bağda" : "BAĞDA DEĞİL")}); CİRO SİLSİLESİ (+) üçüncü ({zc.artı % 10})",
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
                İmzaAt(); Geç();                                                 // mermi rehineye (adım)
                var arka = Düşman(Yer(0f, -1.3f));
                float a0 = arka.TimeLeft;
                float h = Hasar(hedef, () => Silahla(Kadro.KONİ));
                return (h, Yaptı(ComboTür.Ciro), a0 - arka.TimeLeft);
            });
            failed += Check(ref total, $"⚠️ SENETLİ REHİN (REHİN → İMZA → KONİ): merminin değdiği kümedeki önüne çekilip ×1.5 yiyor ({sr.yok.h:0.00} → {sr.var.h:0.00} = {koni * 1.5f:0.00}), CİRO {(sr.var.ciro ? "TETİKLENDİ" : "tetiklenmedi")}; KAMBİYO (+) 360° (arkadaki {sr.var.arka:0.00} → {sr.artı.arka:0.00})",
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
            Silahla(Kadro.REHİN); Silahla(Kadro.REHİN); Geç(); İmzaAt(); Geç(); Düşman(İleri(1.3f)); Silahla(Kadro.KONİ);
            failed += Check(ref total, "üçlüler 1. bölgede kapalı: REHİN → İMZA → KONİ orada CİRO", !Yaptı(ComboTür.SenetliRehin) && Yaptı(ComboTür.Ciro), 0f);
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
            float koni = Taban(Kadro.KONİ), haciz = Taban(Kadro.HACİZ), mermi = Taban(Kadro.İMZA) * _t.imzaBulletMul;
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

            var ib = Üç<(float h, float bek)>(ComboTür.İbraz, () =>
            {
                var e = Düşman(İleri(1.3f));
                İmzaAt(); Geç();                                                 // bir isabet (adım)
                Bekle(Kadro.İFLAS, 5f);
                float h = Hasar(e, () => Silahla(Kadro.İFLAS));
                return (h, _pc.SpecialCooldownOf(Kadro.İFLAS));
            });
            failed += Check(ref total, $"İBRAZ (İMZA → İFLAS): İMZA'nın isabeti İFLAS'ın vurduğuna bir mermi daha ×1.5 ({ib.yok.h:0.00} → {ib.var.h:0.00} = +{1.5f * mermi:0.00}); KAMBİYO TAKİBİ (+) İFLAS'ın beklemesi sıfır",
                            ib.oldu && Yakın(ib.var.h, ib.yok.h + 1.5f * mermi) && ib.var.bek == 5f && ib.artı.bek == 0f, ib.var.h);

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
                Bekle(Kadro.İMZA, 5f);
                float s0 = _clock.Seconds, doyma = _clock.HasatDoyması;
                float h = Hasar(a, () => Silahla(Kadro.İMZA));
                return (h, _clock.Seconds - s0, doyma, _pc.SpecialCooldownOf(Kadro.İMZA));
            });
            failed += Check(ref total, $"KİRA TAHSİLATI (KİRA → İMZA): kulenin menzilindekine birer mermi ({kt.yok.h:0.00} → {kt.var.h:0.00} = {mermi:0.00}), öldürdüğü STOPAJ ödüyor ({kt.yok.saat:+0.00} → {kt.var.saat:+0.00} sn); VERGİ İADESİ (+) İMZA'nın beklemesi sıfır",
                            kt.oldu && kt.yok.h == 0f && Yakın(kt.var.h, mermi) && Yakın(kt.yok.saat, 0f) && Yakın(kt.var.saat, 1.5f * kt.var.doyma)
                            && kt.var.bek == 5f && kt.artı.bek == 0f, kt.var.saat);
            return failed;
        }

        // ── 6) PARTİ 2 — üçlüler (2. bölge, açan EFSANEVİ alınmış) ──────────────
        static int Parti2Üçlüler(ref int total)
        {
            int failed = 0;
            float koni = Taban(Kadro.KONİ), haciz = Taban(Kadro.HACİZ), mermi = Taban(Kadro.İMZA) * _t.imzaBulletMul, rehin = Taban(Kadro.REHİN), işaret = Taban(Kadro.İPOTEK);
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
                SilahSunağı.Al(SilahYükseltme.TopluİmzaArtı, -1);                // İBRAZ (İMZA → İFLAS) da açık: üçlü önce gelmeli
                var içte = Düşman(İleri(Weapon.All[Kadro.İPOTEK].Range(_t)));
                Silahla(Kadro.İPOTEK); Geç();                                   // işaret (adım)
                İmzaAt(); Geç();                                                 // mermi içtekine (adım)
                var kenar = Düşman(İleri(Weapon.All[Kadro.İPOTEK].Range(_t)) + Vector3.right);   // merkezdekinin itilecek yönü yok
                var e = Düşman(İleri(1.3f));
                float i0 = içte.TimeLeft, p0 = İtmesi(kenar), s0 = _clock.Seconds;
                float h = Hasar(e, () => Silahla(Kadro.İFLAS));
                return (i0 - içte.TimeLeft, h, s0, İtmesi(kenar) - p0, Yaptı(ComboTür.İbraz));
            }, saat: 128f);                                                     // 300 sn'de yarı saat de güvenli eşiğin üstü — fark görünmüyor
            failed += Check(ref total, $"⚠️ İPOTEK ZİNCİRİ (İPOTEK → İMZA → İFLAS): İFLAS saatin yarısındaymış gibi ({iz.yok.h:0.00} → {iz.var.h:0.00} = {İflas(iz.var.s0 * 0.5f):0.00}), işaret hemen patlıyor ve içindekine İFLAS da iniyor ({iz.yok.iç:0.00} → {iz.var.iç:0.00} = {işaret:0.00} + {iz.var.h:0.00}), İBRAZ {(iz.var.ibraz ? "TETİKLENDİ" : "tetiklenmedi")}; TAHLİYE EMRİ (+) itiyor ({iz.var.itme:0.0} → {iz.artı.itme:0.0})",
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
                İmzaAt(); Geç();                                                 // mermi uzaktakine (adım)
                var yakın = Düşman(İleri(1.3f));
                Silahla(Kadro.ARBİTRAJ); Geç();
                Bekle(Kadro.İMZA, 5f);
                Silahla(Kadro.REHİN);                                           // yakala
                bool tuttu = _pc.Rehine == uzak;
                float bek = _pc.SpecialCooldownOf(Kadro.İMZA);
                if (tuttu) { Enemy.All.Remove(yakın); Object.DestroyImmediate(yakın.gameObject); }   // fırlatma yolu boş kalsın
                var hedef = Düşman(İleri(4f));
                float atış = Hasar(hedef, () => Silahla(Kadro.REHİN));         // fırlat
                return (tuttu, atış, bek);
            });
            failed += Check(ref total, $"⚠️ PROTESTOLU SENET (İMZA → ARBİTRAJ → REHİN): merminin vurduğu 5 birimden yakalanıyor ({(ps.var.tuttu ? "tuttu" : "TUTMADI")}), fırlatması ×2 ({ps.yok.atış:0.00} → {ps.var.atış:0.00}); ŞERH (+) İMZA'nın beklemesi sıfır",
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
                Silahla(Kadro.İMZA);
                return (a0 - a.TimeLeft, a.SırtDönük);
            });
            failed += Check(ref total, $"⚠️ SPEKÜLATİF ATAK (ARBİTRAJ → MÜŞTEREK → İMZA): bağlıya bir mermi arkadan ({sa.yok.uç:0.00} → {sa.var.uç:0.00} = {mermi * _t.arbitrajBackMul:0.00}); KISA SATIŞ (+) bağlıların sırtı dönük",
                            sa.oldu && sa.yok.uç == 0f && Yakın(sa.var.uç, mermi * _t.arbitrajBackMul) && !sa.var.sırt && sa.artı.sırt, sa.var.uç);
            return failed;
        }

        // ── 7) PARTİ 3 — yapı + satın alma (COMBO sunağı) ───────────────────────
        static int Parti3Yapısı(ref int total)
        {
            int failed = 0;
            var p3 = Enumerable.Range((int)ComboTür.TopluVade, 30).Select(i => (ComboTür)i).ToList();
            YeniKoşu();
            bool satın = p3.All(c => { var t = Combo.Bu(c); return t.Satın && !t.Açan.HasValue && !string.IsNullOrEmpty(t.ArtıAd) && t.MinBölge == (t.Sıra().Length == 3 ? 1 : 0); });
            bool kapalı = p3.All(c => !Combo.Kullanılır(c, 4));
            int ikili = p3.Count(c => Combo.Bu(c).Sıra().Length == 2);
            var t1 = Combo.SatınTeklif(5, 4); var t2 = Combo.SatınTeklif(5, 4); var t0 = Combo.SatınTeklif(5, 0);
            bool teklif = t1.Count == 2 && t1[0] != t1[1] && t1.SequenceEqual(t2) && t0.Count == 2 && t0.All(c => Combo.Bu(c).MinBölge == 0);
            Combo.SatınAl(t1[0], -1);
            bool açıldı = Combo.Kullanılır(t1[0], 4) && !Combo.SatınTeklif(5, 4).Contains(t1[0]);
            failed += Check(ref total, $"⚠️ Parti 3: {ikili} ikili + {p3.Count - ikili} üçlü satın alınır, açansız; alınmadan kapalı ({(kapalı ? "kapalı" : "AÇIK")}), alınınca açık ({(açıldı ? "açık" : "KAPALI")}); " +
                                       $"teklif 2 farklı combo, aynı sunakta aynı, 1. bölgede üçlü yok, alınan teklifte yok",
                            ikili == 24 && satın && kapalı && teklif && açıldı, ikili);

            // Sunak odası: COMBO sunağı — 2 teklif, saniyeyle, sunak başı 1 alım
            YeniKoşu();
            var go = new GameObject("faz42-sunak"); go.transform.SetParent(_root.transform);
            var rm = go.AddComponent<RoomManager>();
            rm.Init(_t, _pGo.transform, null, startRoom: RoomManager.SunakOdası(_t, 0), startRegion: 0);
            var sunaklar = Altar.All.Where(a => a != null && a.Type == Altar.Kind.ComboBuy).ToList();
            bool iki = sunaklar.Count == 2 && sunaklar.All(a => a.SatılıkCombo().HasValue && !a.SoldOut)
                       && sunaklar[0].SatılıkCombo() != sunaklar[1].SatılıkCombo();
            var ilk = sunaklar.FirstOrDefault();
            var alınan = ilk != null ? ilk.SatılıkCombo() : null;
            float fiyat = ilk != null ? ilk.Price : 0f, s0 = _clock.Seconds;
            if (ilk != null) Al.Invoke(ilk, null);
            float ödenen = s0 - _clock.Seconds;
            bool alındı = alınan.HasValue && Combo.Alındı(alınan.Value) && Combo.Kullanılır(alınan.Value, 0);
            bool tükendi = sunaklar.Count == 2 && sunaklar.All(a => a.SoldOut);
            string etiket = sunaklar.Count == 2 ? sunaklar[1].Label() : "";
            float kural = Clock.SilahFiyatı(_t, s0, Enderlik.Nadir) * _t.comboAltarPriceMul;   // 1. bölgede yalnız ikili → nadir × çarpan
            Object.DestroyImmediate(go); RoomManager.Reset();
            failed += Check(ref total, $"⚠️ COMBO sunağı: sunak odasında 2 ayrı teklif ({sunaklar.Count} sunak), [E] {fiyat:0.0} sn alıyor (kural {kural:0.0}, {ödenen:0.0} ödendi), combo açılıyor, sunak başı 1 alım (öbürü \"{etiket}\")",
                            iki && fiyat > 0f && Yakın(fiyat, kural) && _t.comboAltarPriceMul < 1f && Yakın(ödenen, fiyat) && alındı && tükendi && etiket.Contains("bu sunaktan alındı"), ödenen);
            return failed;
        }

        // ── 8) PARTİ 3 — ikililer (satın alınmış) ───────────────────────────────
        static int Parti3İkililer(ref int total)
        {
            int failed = 0;
            float koni = Taban(Kadro.KONİ), haciz = Taban(Kadro.HACİZ), arb = Taban(Kadro.ARBİTRAJ), kar = Taban(Kadro.KARŞILIK);
            float fırlat = Taban(Kadro.REHİN) * _t.rehinThrowDamageMul, v2 = Tahvil.KademeÇarpanı(2);
            var iflas = Weapon.All[Kadro.İFLAS];
            float kiraR = Weapon.All[Kadro.KİRA].Range(_t), ipotekR = Weapon.All[Kadro.İPOTEK].Range(_t);

            var tv = Üç<(float vade, float bek)>(ComboTür.TopluVade, () =>
            {
                Düşman(Yer(-0.7f, 1.3f)); Düşman(İleri(1.3f)); Düşman(Yer(0.7f, 1.3f));
                Silahla(Kadro.KONİ); Geç();                                     // üçünü vurur
                Bekle(Kadro.TAHVİL, 5f);
                W(Kadro.TAHVİL); Bas();                                         // basılı tutma başı
                return ((float)Alan("_vade"), _pc.SpecialCooldownOf(Kadro.TAHVİL));
            });
            failed += Check(ref total, $"TOPLU VADE (KONİ → TAHVİL): üç vuruş olgunluğa +0.75 sn ({tv.yok.vade:0.00} → {tv.var.vade:0.00}); ERKEN İTFA (+) beklemesi sıfır",
                            tv.oldu && tv.yok.vade == 0f && Yakın(tv.var.vade, 0.75f) && tv.var.bek == 5f && tv.artı.bek == 0f, tv.var.vade);

            var ks = Üç<(float kalan, float a, bool sus)>(ComboTür.KiracıSürüsü, () =>
            {
                var a = Düşman(İleri(1.3f));
                Silahla(Kadro.KONİ); Geç();
                Silahla(Kadro.KİRA);                                            // kule nişana
                var k = _pc.Kule;
                if (k == null) return (-1f, -1f, false);
                Düşman(Yer(1f, kiraR));                                         // kulenin dibinde: öncelik yoksa kule buna
                float h = Hasar(a, () => k.Step(0.05f));
                return (k.Kalan, h, a.Susturulmuş);
            });
            failed += Check(ref total, $"KİRACI SÜRÜSÜ (KONİ → KİRA): kule KONİ'nin vurduğuna öncelik veriyor ({ks.yok.a:0.00} → {ks.var.a:0.00}), ömür +1 ({ks.yok.kalan:0.00} → {ks.var.kalan:0.00}); KİRA İHTARI (+) susturuyor",
                            ks.oldu && ks.yok.a == 0f && ks.var.a > 0f && Yakın(ks.var.kalan, ks.yok.kalan + 1f, 0.01f) && !ks.var.sus && ks.artı.sus, ks.var.a);

            var fy = Üç<(float h1, float h2, float hasat)>(ComboTür.FaizYükü, () =>
            {
                var e = Düşman(İleri(1.3f));
                Silahla(Kadro.FAİZLİ); Geç();
                float h1 = 0f;
                float hs = Hasat(() => h1 = Hasar(e, () => Silahla(Kadro.KONİ)));
                _pc.TickCombo(4.1f);                                            // combo beklemesi bitsin
                Silahla(Kadro.FAİZLİ); Geç();
                float h2 = Hasar(e, () => Silahla(Kadro.KONİ));
                return (h1, h2, hs);
            });
            failed += Check(ref total, $"FAİZ YÜKÜ (FAİZLİ → KONİ): odada her seferinde +%15 ({fy.yok.h1:0.00} → {fy.var.h1:0.00} → {fy.var.h2:0.00}); FAİZE FAİZ (+) hasat ×{fy.artı.hasat / Mathf.Max(0.0001f, fy.var.hasat):0.00}",
                            fy.oldu && Yakın(fy.yok.h1, koni) && Yakın(fy.yok.h2, koni) && Yakın(fy.var.h1, koni * 1.15f) && Yakın(fy.var.h2, koni * 1.3f)
                            && Yakın(fy.artı.hasat / Mathf.Max(0.0001f, fy.var.hasat), 2f, 0.05f), fy.var.h2);

            var fs = Üç<(float saat, float bek)>(ComboTür.FaizliSenet, () =>
            {
                HasatKapat();
                Düşman(İleri(1.3f));
                Silahla(Kadro.FAİZLİ); Geç();
                Bekle(Kadro.FAİZLİ, 5f);
                float s0 = _clock.Seconds;
                İmzaAt(); İmzaAt(); İmzaAt();                                   // üç isabet (dibindeki)
                return (_clock.Seconds - s0, _pc.SpecialCooldownOf(Kadro.FAİZLİ));
            });
            failed += Check(ref total, $"FAİZLİ SENET (FAİZLİ → İMZA): serinin ilk iki isabeti savurma bedelini geri ödüyor (üç isabette {fs.yok.saat:+0.00} → {fs.var.saat:+0.00} sn = 2 bedel); SENET İADESİ (+) FAİZLİ'nin beklemesi sıfır",
                            fs.oldu && Yakın(fs.yok.saat, 0f) && Yakın(fs.var.saat, 2f * _t.faizliSwingCost) && fs.var.bek == 5f && fs.artı.bek == 0f, fs.var.saat);

            var it = Üç<(float uzaklık, bool çakılı, bool sus)>(ComboTür.İhtiyatiTedbir, () =>
            {
                var uzak = Düşman(İleri(6.5f));
                Silahla(Kadro.HACİZ); Geç();
                Silahla(Kadro.İPOTEK);
                var m = İşaretler().FirstOrDefault();
                if (m == null) return (-1f, false, false);
                Vector3 d = m.transform.position - uzak.transform.position; d.y = 0f;
                return (d.magnitude, (float)Yavaş.GetValue(uzak) > 0f && (float)YavaşÇarpan.GetValue(uzak) == 0f, uzak.Susturulmuş);
            });
            failed += Check(ref total, $"İHTİYATİ TEDBİR (HACİZ → İPOTEK): işaret HACİZ'in vurduğunun altına ({it.yok.uzaklık:0.00} → {it.var.uzaklık:0.00} birim), o yerine çakılı ({it.var.çakılı}); TEDBİR ŞERHİ (+) susturuyor",
                            it.oldu && it.yok.uzaklık > 1f && it.var.uzaklık >= 0f && it.var.uzaklık < 0.01f && !it.yok.çakılı && it.var.çakılı && !it.var.sus && it.artı.sus, it.var.uzaklık);

            var va = Üç<(bool yavaş, float bek)>(ComboTür.VadeliAlacak, () =>
            {
                var uzak = Düşman(İleri(5.5f));
                Silahla(Kadro.HACİZ); Geç();
                Bekle(Kadro.HACİZ, 5f);
                W(Kadro.TAHVİL); Bas(); _pc.Vadelen(0.1f);                      // basılı tutarken
                return ((float)Yavaş.GetValue(uzak) > 0f, _pc.SpecialCooldownOf(Kadro.HACİZ));
            });
            failed += Check(ref total, $"VADELİ ALACAK (HACİZ → TAHVİL): basılı tuttukça HACİZ'in vurduğu yavaş ({va.yok.yavaş} → {va.var.yavaş}); ALACAK FAİZİ (+) HACİZ'in beklemesi sıfır",
                            va.oldu && !va.yok.yavaş && va.var.yavaş && va.var.bek == 5f && va.artı.bek == 0f, 0f);

            var ii = Üç<(float h, float iç, float itme)>(ComboTür.İpotekliİflas, () =>
            {
                var içte = Düşman(İleri(ipotekR)); var kenar = Düşman(İleri(ipotekR) + Vector3.right);
                Silahla(Kadro.İPOTEK); Geç();
                var e = Düşman(İleri(1.3f));
                float i0 = içte.TimeLeft, p0 = İtmesi(kenar);
                float h = Hasar(e, () => Silahla(Kadro.İFLAS));
                return (h, i0 - içte.TimeLeft, İtmesi(kenar) - p0);
            });
            float iflasDip = iflas.Damage(_t, _clock.AttackDamage) * iflas.DesperationMultiplier(0f, _t);
            failed += Check(ref total, $"İPOTEKLİ İFLAS (İPOTEK → İFLAS): İFLAS saatin dibindeymiş gibi ({ii.yok.h:0.00} → {ii.var.h:0.00} = {iflasDip:0.00}), işaret hemen patlıyor ({ii.yok.iç:0.00} → {ii.var.iç:0.00}); İCRAİ SATIŞ (+) itiyor",
                            ii.oldu && ii.yok.iç == 0f && ii.var.iç > 0f && Yakın(ii.var.h, iflasDip) && ii.var.h > ii.yok.h * 2f && ii.artı.itme > ii.var.itme + 1f, ii.var.h);

            var asp = Üç<(float h, bool görünmez)>(ComboTür.ArsaSpekülasyonu, () =>
            {
                Düşman(İleri(ipotekR));                                          // işaretin içinde biri (şart)
                Silahla(Kadro.İPOTEK); Geç();
                var e = Bakan(İleri(1.2f), Vector3.back);                       // yüzü sana: önden
                float h = Hasar(e, () => Silahla(Kadro.ARBİTRAJ));
                return (h, _pc.Görünmez);
            });
            failed += Check(ref total, $"ARSA SPEKÜLASYONU (İPOTEK → ARBİTRAJ): önden vuruş arkadan ({asp.yok.h:0.00} → {asp.var.h:0.00}); ARSA SPEKÜLATÖRÜ (+) görünmez",
                            asp.oldu && Yakın(asp.yok.h, arb * _t.arbitrajFrontMul) && Yakın(asp.var.h, arb * _t.arbitrajBackMul) && !asp.var.görünmez && asp.artı.görünmez, asp.var.h);
            YeniKoşu(); _ara = 0.2f; Combo.SatınAl(ComboTür.ArsaSpekülasyonu, -1);
            Silahla(Kadro.İPOTEK); Geç(); Bakan(İleri(1.2f), Vector3.back); Silahla(Kadro.ARBİTRAJ);
            failed += Check(ref total, "ARSA SPEKÜLASYONU: işaretin içi boşsa combo YOK (şart)", !Yaptı(ComboTür.ArsaSpekülasyonu), 0f);

            var mk = Üç<(bool tuttu, float bükülü, float bek)>(ComboTür.MalKaçırma, () =>
            {
                var e = Düşman(İleri(2f)); Düşman(Yer(0.9f, 0.6f));             // ikincisi İFLAS'ın yayı dışında, REHİN'inkinde ve daha yakın
                Silahla(Kadro.İFLAS); Geç();
                Bekle(Kadro.İFLAS, 5f);
                Silahla(Kadro.REHİN);
                return (_pc.Rehine == e, _clock.Bükülü, _pc.SpecialCooldownOf(Kadro.İFLAS));
            });
            failed += Check(ref total, $"MAL KAÇIRMA (İFLAS → REHİN): İFLAS'ın vurduğu yakalanıyor ({(mk.var.tuttu ? "tuttu" : "TUTMADI")}), saat duruyor (akış ×{mk.yok.bükülü:0.0} → ×{mk.var.bükülü:0.0}); MAL GİZLEME (+) beklemesi sıfır",
                            mk.oldu && !mk.yok.tuttu && mk.var.tuttu && Yakın(mk.yok.bükülü, 1f) && mk.var.bükülü == 0f && mk.var.bek == 5f && mk.artı.bek == 0f, mk.var.bükülü);

            var tk = Üç<(float atış, float stopaj, float s)>(ComboTür.TasfiyeKulesi, () =>
            {
                HasatKapat();
                Düşman(İleri(1.3f));
                Silahla(Kadro.İFLAS); Geç();
                float s = _clock.Seconds;
                Silahla(Kadro.KİRA);
                var k = _pc.Kule;
                if (k == null) return (-1f, -1f, s);
                var hedef = Düşman(Yer(1f, kiraR));
                return (Hasar(hedef, () => k.Step(0.05f)), k.Stopaj, s);
            }, saat: 30f);
            float dm = Mathf.Max(1f, iflas.DesperationMultiplier(tk.var.s, _t));
            failed += Check(ref total, $"TASFİYE KULESİ (İFLAS → KİRA): saat {tk.var.s:0} sn'de atış İFLAS'ın çarpanıyla ({tk.yok.atış:0.00} → {tk.var.atış:0.00} = ×{dm:0.00}); TASFİYE PAYI (+) STOPAJ ({tk.var.stopaj:0} → {tk.artı.stopaj:0})",
                            tk.oldu && dm > 1.5f && Yakın(tk.var.atış, tk.yok.atış * dm) && tk.var.stopaj == 0f && tk.artı.stopaj >= 1f, tk.var.atış);

            var rk = Üç<(float h, float bek)>(ComboTür.RehinKarşılığı, () =>
            {
                Düşman(İleri(1.5f)); Düşman(İleri(4f));
                Silahla(Kadro.REHİN); Silahla(Kadro.REHİN); Geç();            // yakala, fırlat (adım)
                var e = Düşman(İleri(1.3f));
                Bekle(Kadro.KARŞILIK, 5f);
                float h = Hasar(e, () => Silahla(Kadro.KARŞILIK));
                return (h, _pc.SpecialCooldownOf(Kadro.KARŞILIK));
            });
            failed += Check(ref total, $"REHİN KARŞILIĞI (REHİN → KARŞILIK): fırlatma birikene, KARŞILIK indiriyor ({rk.yok.h:0.00} → {rk.var.h:0.00} = {kar:0.00} + {fırlat:0.00}); KARŞILIK İADESİ (+) beklemesi sıfır",
                            rk.oldu && Yakın(rk.yok.h, kar * 0.6f) && Yakın(rk.var.h, kar + fırlat) && rk.var.bek == 5f && rk.artı.bek == 0f, rk.var.h);

            var iş = Üç<(float uzaklık, float saat, float kalan)>(ComboTür.İşgal, () =>
            {
                HasatKapat();
                var r = Düşman(İleri(1.5f)); Düşman(İleri(4f));
                Silahla(Kadro.REHİN); Silahla(Kadro.REHİN); Geç();            // yakala, fırlat (adım)
                Silahla(Kadro.KİRA);
                var k = _pc.Kule;
                if (k == null) return (-1f, -1f, -1f);
                Vector3 d = k.transform.position - r.transform.position; d.y = 0f;
                float s0 = _clock.Seconds;
                k.Step(1f);
                return (d.magnitude, _clock.Seconds - s0, k.Kalan);
            });
            failed += Check(ref total, $"İŞGAL (REHİN → KİRA): kule rehinenin düştüğü yere ({iş.yok.uzaklık:0.00} → {iş.var.uzaklık:0.00} birim), kirayı rehine ödüyor (saat {iş.yok.saat:+0.00} → {iş.var.saat:+0.00}); TAHLİYE DAVASI (+) ömür +3",
                            iş.oldu && iş.yok.uzaklık > 1f && iş.var.uzaklık >= 0f && iş.var.uzaklık < 0.01f && iş.yok.saat < -0.1f && Yakın(iş.var.saat, 0f)
                            && Yakın(iş.artı.kalan, iş.var.kalan + 3f, 0.01f), iş.var.saat);

            var çi = Üç<(float vade, float bek)>(ComboTür.Senetİskontosu, () =>
            {
                Düşman(İleri(5f));
                İmzaAt(); İmzaAt(); İmzaAt(); Geç();                            // üç isabet (adım)
                Bekle(Kadro.İMZA, 5f);
                W(Kadro.TAHVİL); Bas();
                return ((float)Alan("_vade"), _pc.SpecialCooldownOf(Kadro.İMZA));
            });
            failed += Check(ref total, $"SENET İSKONTOSU (İMZA → TAHVİL): her isabet olgunluğa +0.1 ({çi.yok.vade:0.00} → {çi.var.vade:0.00} = 3 × 0.1); İSKONTO ORANI (+) İMZA'nın beklemesi sıfır",
                            çi.oldu && çi.yok.vade == 0f && Yakın(çi.var.vade, 0.3f) && çi.var.bek == 5f && çi.artı.bek == 0f, çi.var.vade);

            var kç = Üç<(float uzaklık, float kirasız, float bek)>(ComboTür.KiraSenedi, () =>
            {
                var uzak = Düşman(İleri(4f));
                İmzaAt(); Geç();
                Vector3 çarptı = uzak.transform.position;
                Bekle(Kadro.İMZA, 5f);
                Silahla(Kadro.KİRA);
                var k = _pc.Kule;
                if (k == null) return (-1f, -1f, -1f);
                Vector3 d = k.transform.position - çarptı; d.y = 0f;
                return (d.magnitude, k.KirasızKalan, _pc.SpecialCooldownOf(Kadro.İMZA));
            });
            failed += Check(ref total, $"KİRA SENEDİ (İMZA → KİRA): kule merminin değdiği yere ({kç.yok.uzaklık:0.00} → {kç.var.uzaklık:0.00} birim), 3 sn kirasız ({kç.var.kirasız:0}); SENETLE ÖDEME (+) İMZA'nın beklemesi sıfır",
                            kç.oldu && kç.yok.uzaklık > 1f && kç.var.uzaklık >= 0f && kç.var.uzaklık < 0.01f && kç.yok.kirasız == 0f && kç.var.kirasız == 3f
                            && kç.var.bek == 5f && kç.artı.bek == 0f, kç.var.uzaklık);

            var kı = Üç<(float h, float b, bool yavaş)>(ComboTür.Karşılıksızİflas, () =>
            {
                var e = Düşman(İleri(1.3f));
                Alan("_biriken", 2f);
                Silahla(Kadro.KARŞILIK); Geç();
                float b = (float)Alan("_sonBirikenHasarı");
                float h = Hasar(e, () => Silahla(Kadro.İFLAS));
                return (h, b, (float)Yavaş.GetValue(e) > 0f);
            });
            failed += Check(ref total, $"KARŞILIKSIZ İFLAS (KARŞILIK → İFLAS): birikimin iki katı İFLAS'a ({kı.yok.h:0.00} → {kı.var.h:0.00} = +2×{kı.var.b:0.00}); SAHTE İFLAS (+) yavaşlatıyor",
                            kı.oldu && kı.var.b > 0f && Yakın(kı.var.h, kı.yok.h + 2f * kı.var.b) && !kı.var.yavaş && kı.artı.yavaş, kı.var.h);

            var kh = Üç<(float h, bool sus)>(ComboTür.KarşıHamle, () =>
            {
                var e = Bakan(İleri(1.3f), Vector3.back);                       // yüzü sana
                Silahla(Kadro.KARŞILIK); Geç();
                float h = Hasar(e, () => Silahla(Kadro.ARBİTRAJ));
                return (h, e.Susturulmuş);
            });
            failed += Check(ref total, $"KARŞI HAMLE (KARŞILIK → ARBİTRAJ): KARŞILIK'ın vurduğunun arkasına geçip vuruyor ({kh.yok.h:0.00} → {kh.var.h:0.00}); KARŞI İHTAR (+) susturuyor",
                            kh.oldu && Yakın(kh.yok.h, arb * _t.arbitrajFrontMul) && Yakın(kh.var.h, arb * _t.arbitrajBackMul) && !kh.var.sus && kh.artı.sus, kh.var.h);

            var vt = Üç<(float kalan, float hasar, float bek)>(ComboTür.VadeliTapu, () =>
            {
                Düşman(İleri(1.3f));
                TahvilBoz(1.1f); Geç();                                        // 2. kademe
                Bekle(Kadro.İPOTEK, 5f);
                Silahla(Kadro.İPOTEK);
                var m = İşaretler().FirstOrDefault();
                return m == null ? (-1f, -1f, -1f) : (m.Kalan, m.Hasar, _pc.SpecialCooldownOf(Kadro.İPOTEK));
            });
            failed += Check(ref total, $"VADELİ TAPU (TAHVİL 2. kademe → İPOTEK): gecikme yarı ({vt.yok.kalan:0.00} → {vt.var.kalan:0.00}), hasar ×{v2} ({vt.yok.hasar:0.00} → {vt.var.hasar:0.00}); TAPU İSKONTOSU (+) beklemesi sıfır",
                            vt.oldu && vt.yok.kalan > 0f && Yakın(vt.var.kalan, vt.yok.kalan * 0.5f) && Yakın(vt.var.hasar, vt.yok.hasar * v2) && vt.var.bek == 5f && vt.artı.bek == 0f, vt.var.kalan);

            var to = Üç<(float kalan, float payÇ, bool sus)>(ComboTür.TahvilOrtaklığı, () =>
            {
                var a = Düşman(İleri(1.5f)); Düşman(Yer(1.5f, 0f));
                Silahla(Kadro.MÜŞTEREK);                                        // a işaretli (bekleyen)
                TahvilBoz(1.1f); Geç();                                        // 2. kademe (adım)
                _pGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
                Silahla(Kadro.MÜŞTEREK);                                        // b → bağ
                Ortaya();
                var bağ = _pc.SonBağ;
                return bağ == null ? (-1f, -1f, false) : (bağ.Kalan, bağ.PayÇarpanı, a.Susturulmuş);
            });
            failed += Check(ref total, $"TAHVİL ORTAKLIĞI (TAHVİL 2. kademe → MÜŞTEREK): bağ +4 sn ({to.yok.kalan:0.0} → {to.var.kalan:0.0}), yayılma ×{to.var.payÇ:0.00}; ORTAKLIK PAYI (+) bağlılar susuyor",
                            to.oldu && Yakın(to.yok.kalan, _t.musterekSeconds) && Yakın(to.var.kalan, _t.musterekSeconds + 4f) && Yakın(to.yok.payÇ, 1f) && Yakın(to.var.payÇ, 1.2f)
                            && !to.var.sus && to.artı.sus, to.var.kalan);

            var ob = Üç<(float uç, float saat, bool yavaş)>(ComboTür.OrtakBorç, () =>
            {
                HasatKapat();
                var (a, _, _) = BağKurgusu(); Geç();
                Düşman(İleri(1.3f));
                float a0 = a.TimeLeft, s0 = _clock.Seconds;
                Silahla(Kadro.FAİZLİ);
                return (a0 - a.TimeLeft, _clock.Seconds - s0, (float)Yavaş.GetValue(a) > 0f);
            });
            failed += Check(ref total, $"ORTAK BORÇ (MÜŞTEREK → FAİZLİ): bedel kadar bağlıya ({ob.yok.uç:0.00} → {ob.var.uç:0.00}), bedel sana ({ob.yok.saat:+0.00} → {ob.var.saat:+0.00}); MÜŞTEREK BORÇLU (+) yavaşlatıyor",
                            ob.oldu && ob.yok.uç == 0f && Yakın(ob.var.uç, _t.faizliSwingCost) && Yakın(ob.yok.saat, 0f) && Yakın(ob.var.saat, _t.faizliSwingCost)
                            && !ob.var.yavaş && ob.artı.yavaş, ob.var.uç);

            var iç = Üç<(float h, float bek)>(ComboTür.İçİşlem, () =>
            {
                var a = Bakan(İleri(1.5f), Vector3.back); Düşman(Yer(1.5f, 0f));   // a'nın yüzü sana
                Silahla(Kadro.MÜŞTEREK);
                _pGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
                Silahla(Kadro.MÜŞTEREK);                                        // bağ (adım)
                Ortaya(); Geç();
                Bekle(Kadro.ARBİTRAJ, 5f);
                float h = Hasar(a, () => Silahla(Kadro.ARBİTRAJ));
                return (h, _pc.SpecialCooldownOf(Kadro.ARBİTRAJ));
            });
            failed += Check(ref total, $"İÇ İŞLEM (MÜŞTEREK → ARBİTRAJ): bağlılar sırtını dönüyor, yüzü sana olana arkadan ({iç.yok.h:0.00} → {iç.var.h:0.00}); İÇERİDEN BİLGİ (+) beklemesi sıfır",
                            iç.oldu && Yakın(iç.yok.h, arb * _t.arbitrajFrontMul) && Yakın(iç.var.h, arb * _t.arbitrajBackMul) && iç.var.bek == 5f && iç.artı.bek == 0f, iç.var.h);

            var gh = Üç<(float h, bool görünmez)>(ComboTür.GizliHaciz, () =>
            {
                var e = Bakan(İleri(1.2f), Vector3.forward);                    // sırtı sana: arkadan
                Silahla(Kadro.ARBİTRAJ); Geç();
                float h = Hasar(e, () => Silahla(Kadro.HACİZ));                // dibinde: ölü bölgede
                return (h, _pc.Görünmez);
            });
            failed += Check(ref total, $"GİZLİ HACİZ (ARBİTRAJ arkadan → HACİZ): ölü bölge yok, ×1.3 ({gh.yok.h:0.00} → {gh.var.h:0.00}); SESSİZ HACİZ (+) görünmez",
                            gh.oldu && gh.yok.h == 0f && Yakın(gh.var.h, haciz * 1.3f) && !gh.var.görünmez && gh.artı.görünmez, gh.var.h);

            var kc = Üç<(bool tuttu, float bükülü, float bek)>(ComboTür.Kaçırma, () =>
            {
                var e = Bakan(İleri(2f), Vector3.forward);                      // sırtı sana: arkadan
                Düşman(Yer(1f, 0.36f));                                         // ARBİTRAJ'ın yayı dışında, REHİN'inkinde ve daha yakın
                Silahla(Kadro.ARBİTRAJ); Geç();
                Bekle(Kadro.REHİN, 5f);
                Silahla(Kadro.REHİN);
                return (_pc.Rehine == e, _clock.Bükülü, _pc.SpecialCooldownOf(Kadro.REHİN));
            });
            failed += Check(ref total, $"KAÇIRMA (ARBİTRAJ arkadan → REHİN): ARBİTRAJ'ın vurduğu yakalanıyor ({(kc.var.tuttu ? "tuttu" : "TUTMADI")}), saat yarı akıyor (×{kc.yok.bükülü:0.0} → ×{kc.var.bükülü:0.0}); FİDYE (+) beklemesi sıfır",
                            kc.oldu && !kc.yok.tuttu && kc.var.tuttu && Yakın(kc.yok.bükülü, 1f) && Yakın(kc.var.bükülü, 0.5f) && kc.var.bek == 5f && kc.artı.bek == 0f, kc.var.bükülü);

            var sk = Üç<(float h, float kalan)>(ComboTür.SüpürgeKulesi, () =>
            {
                Silahla(Kadro.KİRA); Geç();                                     // kule nişana
                var kuleDibi = Düşman(Yer(1f, kiraR));
                Düşman(İleri(1.3f));
                var k = _pc.Kule;
                float h = Hasar(kuleDibi, () => Silahla(Kadro.KONİ));
                return (h, k != null ? k.Kalan : -1f);
            });
            failed += Check(ref total, $"SÜPÜRGE KULESİ (KİRA → KONİ): kulenin dibindekine de KONİ ({sk.yok.h:0.00} → {sk.var.h:0.00}); KULE SÜPÜRMESİ (+) ömür +2",
                            sk.oldu && sk.yok.h == 0f && Yakın(sk.var.h, koni) && Yakın(sk.artı.kalan, sk.var.kalan + 2f, 0.01f), sk.var.h);

            var kg = Üç<(float saat, float bek)>(ComboTür.KiraGetirisi, () =>
            {
                Silahla(Kadro.KİRA); Geç();
                Bekle(Kadro.TAHVİL, 5f);
                W(Kadro.TAHVİL); Bas();
                float s0 = _clock.Seconds;
                _pc.Vadelen(1f);
                return (_clock.Seconds - s0, _pc.SpecialCooldownOf(Kadro.TAHVİL));
            });
            failed += Check(ref total, $"KİRA GETİRİSİ (KİRA → TAHVİL): basılı tutarken saat iki kat akmıyor ({kg.yok.saat:+0.00} → {kg.var.saat:+0.00} sn/sn); GETİRİ PAYI (+) beklemesi sıfır",
                            kg.oldu && kg.yok.saat < -0.01f && Yakın(kg.var.saat, 0f) && kg.var.bek == 5f && kg.artı.bek == 0f, kg.var.saat);
            return failed;
        }

        // ── 9) PARTİ 3 — üçlüler (2. bölge, satın alınmış) ──────────────────────
        static int Parti3Üçlüler(ref int total)
        {
            int failed = 0;
            float koni = Taban(Kadro.KONİ), arb = Taban(Kadro.ARBİTRAJ);

            var ti = Üç<(float saat, float doyma, bool öldü, float bek)>(ComboTür.TopluİCRA, () =>
            {
                HasatKapat();
                var zayıf = Düşman(Yer(-1f, 1f)); Düşman(Yer(1f, 1f)); Düşman(İleri(1.2f));   // KONİ üçünü vurur; ortadaki rehine olacak
                zayıf.TakeDamage(zayıf.TimeLeft - 1f, 0f);                      // havuzu 1 sn
                Silahla(Kadro.KONİ); Geç();
                Silahla(Kadro.REHİN); Silahla(Kadro.REHİN); Geç();             // ortadakini yakala, fırlat (adım)
                Düşman(İleri(1.3f));
                Bekle(Kadro.KONİ, 5f);
                float s0 = _clock.Seconds, doyma = _clock.HasatDoyması;
                Silahla(Kadro.İFLAS);
                return (_clock.Seconds - s0, doyma, !Enemy.All.Contains(zayıf), _pc.SpecialCooldownOf(Kadro.KONİ));
            });
            failed += Check(ref total, $"⚠️ TOPLU İCRA (KONİ → REHİN → İFLAS): KONİ'nin vurduklarından en zayıfı İFLAS'ın dip vuruşuyla ölüyor ({(ti.var.öldü ? "öldü" : "ÖLMEDİ")}), +2 sn ({ti.yok.saat:+0.00} → {ti.var.saat:+0.00} = {2f * ti.var.doyma:+0.00}); İCRA MASRAFI (+) KONİ'nin beklemesi sıfır",
                            ti.oldu && Yakın(ti.yok.saat, 0f) && Yakın(ti.var.saat, 2f * ti.var.doyma) && ti.var.öldü && ti.var.bek == 5f && ti.artı.bek == 0f, ti.var.saat);

            var bf = Üç<(float payÇ, bool yavaş, bool ortaklık)>(ComboTür.BileşikFaiz, () =>
            {
                Combo.SatınAl(ComboTür.TahvilOrtaklığı, -1);                    // TAHVİL → MÜŞTEREK de açık: üçlü önce gelmeli
                var a = Düşman(İleri(1.5f)); Düşman(Yer(1.5f, 0f));
                Silahla(Kadro.MÜŞTEREK);                                        // a işaretli
                Silahla(Kadro.FAİZLİ); Geç();                                   // (adım)
                TahvilBoz(0.6f); Geç();                                        // 1. kademe (adım)
                _pGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
                Silahla(Kadro.MÜŞTEREK);                                        // b → bağ (bitiriş)
                Ortaya();
                var bağ = _pc.SonBağ;
                return (bağ != null ? bağ.PayÇarpanı : -1f, (float)Yavaş.GetValue(a) > 0f, Yaptı(ComboTür.TahvilOrtaklığı));
            });
            float v1 = Mathf.Min(Tahvil.KademeÇarpanı(1), 1f / _t.musterekShare);
            failed += Check(ref total, $"⚠️ BİLEŞİK FAİZ (FAİZLİ → TAHVİL 1. kademe → MÜŞTEREK): yayılma ×kademe ({bf.yok.payÇ:0.00} → {bf.var.payÇ:0.00} = {v1:0.00}), TAHVİL ORTAKLIĞI {(bf.var.ortaklık ? "TETİKLENDİ" : "tetiklenmedi")}; FAİZ TAHAKKUKU (+) bağlılar yavaş",
                            bf.oldu && Yakın(bf.var.payÇ, v1) && !bf.var.ortaklık && !bf.var.yavaş && bf.artı.yavaş, bf.var.payÇ);

            var zt = Üç<(bool kilit, float uzama, bool çek, bool sus)>(ComboTür.ZincirlemeTahsil, () =>
            {
                Combo.SatınAl(ComboTür.KiraSenedi, -1);                         // İMZA → KİRA da açık: üçlü önce gelmeli
                var uzak = Düşman(İleri(5.5f));
                Silahla(Kadro.HACİZ); Geç();
                İmzaAt(); Geç();                                                 // mermi uzaktakine (adım)
                Silahla(Kadro.KİRA);
                var k = _pc.Kule;
                if (k == null) return (false, -1f, false, false);
                float k0 = k.Kalan;
                İmzaAt();                                                       // İMZA'nın bir isabeti daha
                return (k.Kilit == uzak, k.Kalan - k0, Yaptı(ComboTür.KiraSenedi), uzak.Susturulmuş);
            });
            failed += Check(ref total, $"⚠️ ZİNCİRLEME TAHSİL (HACİZ → İMZA → KİRA): kule HACİZ'in vurduğuna kilitli ({zt.var.kilit}), İMZA'nın isabeti ömre +0.25 ({zt.yok.uzama:0.00} → {zt.var.uzama:0.00}), KİRA SENEDİ {(zt.var.çek ? "TETİKLENDİ" : "tetiklenmedi")}; TAHSİL EMRİ (+) susturuyor",
                            zt.oldu && !zt.yok.kilit && zt.var.kilit && Yakın(zt.yok.uzama, 0f) && Yakın(zt.var.uzama, 0.25f) && !zt.var.çek && !zt.var.sus && zt.artı.sus, zt.var.uzama);

            var kr = Üç<(float h, float b, bool sırt, bool açığa)>(ComboTür.KarşıRehin, () =>
            {
                Düşman(İleri(1.3f));
                Alan("_biriken", 2f);
                Silahla(Kadro.KARŞILIK); Geç();
                float b = (float)Alan("_sonBirikenHasarı");
                Silahla(Kadro.REHİN); Silahla(Kadro.REHİN); Geç();             // yakala, fırlat (adım)
                var t = Bakan(İleri(1.3f), Vector3.back);                       // yüzü sana
                float h = Hasar(t, () => Silahla(Kadro.ARBİTRAJ));
                return (h, b, t.SırtDönük, Yaptı(ComboTür.AçığaSatış));
            });
            failed += Check(ref total, $"⚠️ KARŞI REHİN (KARŞILIK → REHİN → ARBİTRAJ): arkadan, birikim de üstüne ({kr.yok.h:0.00} → {kr.var.h:0.00} = ({arb:0.00} + {kr.var.b:0.00}) × {_t.arbitrajBackMul}), AÇIĞA SATIŞ {(kr.var.açığa ? "TETİKLENDİ" : "tetiklenmedi")}; REHİN AÇIĞI (+) sırtı dönük",
                            kr.oldu && Yakın(kr.yok.h, arb * _t.arbitrajFrontMul) && kr.var.b > 0f && Yakın(kr.var.h, (arb + kr.var.b) * _t.arbitrajBackMul) && !kr.var.açığa
                            && !kr.var.sırt && kr.artı.sırt, kr.var.h);

            var ok = Üç<(float uç, float arka, float k, bool tahsil, float bek)>(ComboTür.OrtakKarşılık, () =>
            {
                var (a, _, ara) = BağKurgusu(); Geç();
                var e = Düşman(İleri(1.3f));
                float k = Hasar(e, () => Silahla(Kadro.KARŞILIK)); Geç();
                Bekle(Kadro.KARŞILIK, 5f);
                float a0 = a.TimeLeft, r0 = ara.TimeLeft;
                Silahla(Kadro.KONİ);
                return (a0 - a.TimeLeft, r0 - ara.TimeLeft, k, Yaptı(ComboTür.KarşılıklıTahsil), _pc.SpecialCooldownOf(Kadro.KARŞILIK));
            });
            failed += Check(ref total, $"⚠️ ORTAK KARŞILIK (MÜŞTEREK → KARŞILIK → KONİ): koni 360° (arkadaki {ok.yok.arka:0.00} → {ok.var.arka:0.00}), KARŞILIK'ın vuruşu bağlıya ({ok.yok.uç:0.00} → {ok.var.uç:0.00} = {ok.var.k:0.00}), KARŞILIKLI TAHSİL {(ok.var.tahsil ? "TETİKLENDİ" : "tetiklenmedi")}; KARŞILIKLI ORTAKLIK (+) beklemesi sıfır",
                            ok.oldu && ok.yok.uç == 0f && ok.yok.arka == 0f && Yakın(ok.var.uç, ok.var.k) && Yakın(ok.var.arka, koni) && !ok.var.tahsil
                            && ok.var.bek == 5f && ok.artı.bek == 0f, ok.var.uç);

            var zk = Üç<(float bükülü, float çekH, float uzama)>(ComboTür.ZamanKirası, () =>
            {
                Silahla(Kadro.KİRA); Geç();
                var e = Düşman(İleri(1.3f));
                Silahla(Kadro.İFLAS); Geç();
                var k = _pc.Kule;
                float k0 = k != null ? k.Kalan : 0f;
                float h = Hasar(e, () => İmzaAt());
                return (_clock.Bükülü, h, k != null ? k.Kalan - k0 : -1f);
            });
            failed += Check(ref total, $"⚠️ ZAMAN KİRASI (KİRA → İFLAS → İMZA): saat duruyor (×{zk.yok.bükülü:0.0} → ×{zk.var.bükülü:0.0}), seri ×1.5 ({zk.yok.çekH:0.00} → {zk.var.çekH:0.00}); ZAMAN AŞIMI (+) kule +3 sn ({zk.artı.uzama:0.0})",
                            zk.oldu && Yakın(zk.yok.bükülü, 1f) && zk.var.bükülü == 0f && Yakın(zk.var.çekH, zk.yok.çekH * 1.5f) && Yakın(zk.var.uzama, 0f) && Yakın(zk.artı.uzama, 3f), zk.var.çekH);
            return failed;
        }

        // ── 10) PARTİ 4 — yapı (son parti: 120) ──────────────────────────────────
        static int Parti4Yapısı(ref int total)
        {
            var p4 = Enumerable.Range((int)ComboTür.TopluTahsilat, 30).Select(i => (ComboTür)i).ToList();
            YeniKoşu();
            bool satın = p4.All(c => { var t = Combo.Bu(c); return t.Satın && !t.Açan.HasValue && !string.IsNullOrEmpty(t.ArtıAd) && t.MinBölge == (t.Sıra().Length == 3 ? 1 : 0); });
            bool kapalı = p4.All(c => !Combo.Kullanılır(c, 4));
            int ikili = p4.Count(c => Combo.Bu(c).Sıra().Length == 2);
            int Say(System.Func<Combo.Tanım, bool> tür, int uzunluk) => Combo.Tümü.Count(t => tür(t) && t.Sıra().Length == uzunluk);
            System.Func<Combo.Tanım, bool> direkt = t => !t.Açan.HasValue && !t.Satın, açılır = t => t.Açan.HasValue, satınAl = t => t.Satın;
            bool dağılım = Say(direkt, 2) == 24 && Say(açılır, 2) == 24 && Say(satınAl, 2) == 48
                           && Say(direkt, 3) == 6 && Say(açılır, 3) == 6 && Say(satınAl, 3) == 12;
            bool ikiÜçlü = Enumerable.Range(0, Weapon.All.Length).All(w => Combo.Tümü.Count(t => t.Sıra().Length == 3 && t.Sıra()[0] == w) == 2);
            return Check(ref total, $"⚠️ Parti 4: {ikili} ikili + {p4.Count - ikili} üçlü satın alınır, açansız, alınmadan kapalı ({(kapalı ? "kapalı" : "AÇIK")}); " +
                                    $"120'nin dağılımı ¼ direkt · ¼ açılır · ½ satın (ikili 24/24/48, üçlü 6/6/12); her silah iki üçlünün başında",
                         Combo.Sayı == 120 && ikili == 24 && satın && kapalı && dağılım && ikiÜçlü, Combo.Sayı);
        }

        // ── 11) PARTİ 4 — ikililer (satın alınmış) ───────────────────────────────
        static int Parti4İkililer(ref int total)
        {
            int failed = 0;
            float haciz = Taban(Kadro.HACİZ), arb = Taban(Kadro.ARBİTRAJ), faizli = Taban(Kadro.FAİZLİ);
            float fırlat = Taban(Kadro.REHİN) * _t.rehinThrowDamageMul, v2 = Tahvil.KademeÇarpanı(2), cost = _t.faizliSwingCost;
            var iflas = Weapon.All[Kadro.İFLAS];
            float kiraR = Weapon.All[Kadro.KİRA].Range(_t), ipotekR = Weapon.All[Kadro.İPOTEK].Range(_t);

            var tt = Üç<(int yan, int sus)>(ComboTür.TopluTahsilat, () =>
            {
                var sol = Düşman(Yer(-1.2f, 1.2f)); var sağ = Düşman(Yer(1.2f, 1.2f)); Düşman(İleri(1.3f));
                Silahla(Kadro.KONİ); Geç();                                     // üçünü vurur
                float s0 = sol.TimeLeft, ğ0 = sağ.TimeLeft;
                Silahla(Kadro.İMZA);                                            // ilk mermi
                return ((sol.TimeLeft < s0 ? 1 : 0) + (sağ.TimeLeft < ğ0 ? 1 : 0), (sol.Susturulmuş ? 1 : 0) + (sağ.Susturulmuş ? 1 : 0));
            });
            failed += Check(ref total, $"TOPLU TAHSİLAT (KONİ → İMZA): yandakilere de birer mermi ({tt.yok.yan} → {tt.var.yan}/2); TAHSİL MAKBUZU (+) susturuyor ({tt.artı.sus}/2)",
                            tt.oldu && tt.yok.yan == 0 && tt.var.yan == 2 && tt.var.sus == 0 && tt.artı.sus == 2, tt.var.yan);

            var sa = Üç<(float h, bool sırt)>(ComboTür.SürüdenAyır, () =>
            {
                var yakın = Bakan(İleri(1.2f), Vector3.back); var uzak = Bakan(İleri(2f), Vector3.back);   // ikisinin de yüzü sana
                Silahla(Kadro.KONİ); Geç();                                     // ikisini vurur
                float h = Hasar(uzak, () => Silahla(Kadro.ARBİTRAJ));
                return (h, yakın.SırtDönük);
            });
            failed += Check(ref total, $"SÜRÜDEN AYIR (KONİ → ARBİTRAJ): en uzaktakinin arkasına geçip vuruyor ({sa.yok.h:0.00} → {sa.var.h:0.00} = {arb * _t.arbitrajBackMul:0.00}); AYRIŞMA (+) öbürü sırtı dönük",
                            sa.oldu && sa.yok.h == 0f && Yakın(sa.var.h, arb * _t.arbitrajBackMul) && !sa.var.sırt && sa.artı.sırt, sa.var.h);

            var be = Üç<(bool tuttu, float akış, float hasat, float bek)>(ComboTür.BorçEsareti, () =>
            {
                var e = Düşman(İleri(2f)); Düşman(Yer(0.9f, 0.6f));             // ikincisi FAİZLİ'nin yayı dışında, REHİN'inkinde ve daha yakın
                Silahla(Kadro.FAİZLİ); Geç();
                Bekle(Kadro.FAİZLİ, 5f);
                Silahla(Kadro.REHİN);
                float e0 = e.TimeLeft;
                float hs = Hasat(() => _pc.TickRehin(1.01f));                    // tuttukça 1 sn
                return (_pc.Rehine == e, e0 - e.TimeLeft, hs, _pc.SpecialCooldownOf(Kadro.FAİZLİ));
            });
            failed += Check(ref total, $"BORÇ ESARETİ (FAİZLİ → REHİN): FAİZLİ'nin vurduğu yakalanıyor ({(be.var.tuttu ? "tuttu" : "TUTMADI")}), 1 sn'de bedel kadar havuzundan ({be.yok.akış:0.00} → {be.var.akış:0.00}), hasat ({be.var.hasat:+0.00}); BORÇ SENEDİ (+) beklemesi sıfır",
                            be.oldu && !be.yok.tuttu && be.var.tuttu && be.yok.akış == 0f && Yakın(be.var.akış, cost) && be.var.hasat > 0f && be.var.bek == 5f && be.artı.bek == 0f, be.var.akış);

            var fk = Üç<(float atış, float saat, float kalan)>(ComboTür.FaizliKira, () =>
            {
                HasatKapat();
                Düşman(İleri(1.3f));
                Silahla(Kadro.FAİZLİ); Geç();
                float s0 = _clock.Seconds;
                Silahla(Kadro.KİRA);
                var k = _pc.Kule;
                if (k == null) return (-1f, -1f, -1f);
                float saat = _clock.Seconds - s0;
                var hedef = Düşman(Yer(1f, kiraR));
                return (Hasar(hedef, () => k.Step(0.05f)), saat, k.Kalan);
            });
            failed += Check(ref total, $"FAİZLİ KİRA (FAİZLİ → KİRA): 4 savurma bedeli ({fk.yok.saat:+0.00;-0.00} → {fk.var.saat:+0.00;-0.00} sn), kule ×2 ({fk.yok.atış:0.00} → {fk.var.atış:0.00}); KİRA SÖZLEŞMESİ (+) ömür +3",
                            fk.oldu && Yakın(fk.yok.saat, 0f) && Yakın(fk.var.saat, -4f * cost) && Yakın(fk.var.atış, fk.yok.atış * 2f) && Yakın(fk.artı.kalan, fk.var.kalan + 3f, 0.01f), fk.var.atış);

            var ho = Üç<(bool bağlı, float bek)>(ComboTür.HacizOrtaklığı, () =>
            {
                var uzak = Düşman(İleri(5.5f)); Düşman(İleri(1.3f));            // HACİZ uzaktakine (yakın ölü bölgede), MÜŞTEREK yakına
                Silahla(Kadro.HACİZ); Geç();
                Bekle(Kadro.HACİZ, 5f);
                Silahla(Kadro.MÜŞTEREK);
                var bağ = _pc.SonBağ;
                return (bağ != null && bağ.Üyeler.Contains(uzak), _pc.SpecialCooldownOf(Kadro.HACİZ));
            });
            failed += Check(ref total, $"HACİZ ORTAKLIĞI (HACİZ → MÜŞTEREK): HACİZ'in vurduğu menzilsiz bağlanıyor ({ho.yok.bağlı} → {ho.var.bağlı}); MÜŞTEREK HACİZ (+) beklemesi sıfır",
                            ho.oldu && !ho.yok.bağlı && ho.var.bağlı && ho.var.bek == 5f && ho.artı.bek == 0f, 0f);

            var kh = Üç<(float uzakH, float yemH, bool sus)>(ComboTür.KiraHaczi, () =>
            {
                var uzak = Düşman(İleri(4f)); var yem = Düşman(Yer(0.3f, kiraR));   // yem nişandaki kulenin dibinde
                Silahla(Kadro.HACİZ); Geç();
                Silahla(Kadro.KİRA);
                var k = _pc.Kule;
                if (k == null) return (-1f, -1f, false);
                float u0 = uzak.TimeLeft, y0 = yem.TimeLeft;
                k.Step(0.05f);
                return (u0 - uzak.TimeLeft, y0 - yem.TimeLeft, uzak.Susturulmuş);
            });
            failed += Check(ref total, $"KİRA HACZİ (HACİZ → KİRA): kule HACİZ'in vurduğunun yanında, önce ona ×1.5 ({kh.yok.uzakH:0.00} → {kh.var.uzakH:0.00} = {kh.yok.yemH:0.00} × 1.5); TAHLİYE İHTARI (+) susturuyor",
                            kh.oldu && kh.yok.uzakH == 0f && kh.yok.yemH > 0f && Yakın(kh.var.uzakH, kh.yok.yemH * 1.5f) && kh.var.yemH == 0f && !kh.var.sus && kh.artı.sus, kh.var.uzakH);

            var tk = Üç<(float h, float saat, float iç)>(ComboTür.TapuKredisi, () =>
            {
                HasatKapat();
                var içte = Düşman(İleri(ipotekR));                               // işaretin içinde biri (şart)
                Silahla(Kadro.İPOTEK); Geç();
                var e = Düşman(İleri(1.3f));
                float i0 = içte.TimeLeft, s0 = _clock.Seconds;
                float h = Hasar(e, () => Silahla(Kadro.FAİZLİ));
                return (h, _clock.Seconds - s0, i0 - içte.TimeLeft);
            });
            failed += Check(ref total, $"TAPU KREDİSİ (İPOTEK → FAİZLİ): ×2 ({tk.yok.h:0.00} → {tk.var.h:0.00}), bedel geri ({tk.yok.saat:+0.00} → {tk.var.saat:+0.00}); İPOTEK FEKKİ (+) işaret hemen patlıyor",
                            tk.oldu && Yakın(tk.yok.h, faizli) && Yakın(tk.var.h, faizli * 2f) && Yakın(tk.yok.saat, 0f) && Yakın(tk.var.saat, cost)
                            && tk.var.iç == 0f && tk.artı.iç > 0f, tk.var.h);

            var tv = Üç<(int k0, int k1, float bek)>(ComboTür.TapuVadesi, () =>
            {
                Düşman(İleri(ipotekR));
                Silahla(Kadro.İPOTEK); Geç();
                var m = İşaretler().FirstOrDefault();
                Bekle(Kadro.İPOTEK, 5f);
                W(Kadro.TAHVİL); Bas();
                int k0 = _pc.VadeKademesi;
                if (m != null) m.HemenPatlat(1f);                                // basılı tutarken işaret patladı
                return (k0, _pc.VadeKademesi, _pc.SpecialCooldownOf(Kadro.İPOTEK));
            });
            failed += Check(ref total, $"TAPU VADESİ (İPOTEK → TAHVİL): basılı tutma 1. kademeden ({tv.yok.k0} → {tv.var.k0}), işaret patlayınca +1 ({tv.yok.k1} → {tv.var.k1}); TAPU TESCİLİ (+) beklemesi sıfır",
                            tv.oldu && tv.yok.k0 == 0 && tv.yok.k1 == 0 && tv.var.k0 == 1 && tv.var.k1 == 2 && tv.var.bek == 5f && tv.artı.bek == 0f, tv.var.k1);

            var iip = Üç<(float uzaklık, float kalan, float hasar, float bek, float s)>(ComboTür.İflasİpoteği, () =>
            {
                var e = Düşman(İleri(1.3f));
                Silahla(Kadro.İFLAS); Geç();
                Bekle(Kadro.İFLAS, 5f);
                float s = _clock.Seconds;
                Silahla(Kadro.İPOTEK);
                var m = İşaretler().FirstOrDefault();
                if (m == null) return (-1f, -1f, -1f, -1f, s);
                Vector3 d = m.transform.position - e.transform.position; d.y = 0f;
                return (d.magnitude, m.Kalan, m.Hasar, _pc.SpecialCooldownOf(Kadro.İFLAS), s);
            }, saat: 30f);
            float dmİ = Mathf.Max(1f, iflas.DesperationMultiplier(iip.var.s, _t));
            failed += Check(ref total, $"İFLAS İPOTEĞİ (İFLAS → İPOTEK): işaret İFLAS'ın vurduğunun altına ({iip.yok.uzaklık:0.00} → {iip.var.uzaklık:0.00}), gecikme ×0.2 ({iip.yok.kalan:0.00} → {iip.var.kalan:0.00}), hasar ×{dmİ:0.00}; MÜHLET (+) beklemesi sıfır",
                            iip.oldu && dmİ > 1.5f && iip.yok.uzaklık > 1f && iip.var.uzaklık >= 0f && iip.var.uzaklık < 0.01f && Yakın(iip.var.kalan, iip.yok.kalan * 0.2f)
                            && Yakın(iip.var.hasar, iip.yok.hasar * dmİ) && iip.var.bek == 5f && iip.artı.bek == 0f, iip.var.hasar);

            var io = Üç<(float payÇ, bool yavaş, float s)>(ComboTür.İflasOrtaklığı, () =>
            {
                var a = Düşman(İleri(1.3f)); Düşman(Yer(1.5f, 0f));
                Silahla(Kadro.İFLAS); Geç();                                    // a'yı vurur
                float s = _clock.Seconds;
                _pGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
                Silahla(Kadro.MÜŞTEREK);                                        // b → a ile bağ
                Ortaya();
                var bağ = _pc.SonBağ;
                return (bağ != null && bağ.Üyeler.Contains(a) ? bağ.PayÇarpanı : -1f, (float)Yavaş.GetValue(a) > 0f, s);
            }, saat: 30f);
            float dmO = Mathf.Min(Mathf.Max(1f, iflas.DesperationMultiplier(io.var.s, _t)), 1f / _t.musterekShare);
            failed += Check(ref total, $"İFLAS ORTAKLIĞI (İFLAS → MÜŞTEREK): İFLAS'ın vurduğu bağlanıyor, yayılma ×çaresizlik ({io.yok.payÇ:0.00} → {io.var.payÇ:0.00} = {dmO:0.00}); ORTAK İFLAS (+) bağlılar yavaş",
                            io.oldu && io.yok.payÇ < 0f && dmO > 1.2f && Yakın(io.var.payÇ, dmO) && !io.var.yavaş && io.artı.yavaş, io.var.payÇ);

            var rk = Üç<(float h, float saat, float bek)>(ComboTür.RehinliKredi, () =>
            {
                HasatKapat();
                Düşman(İleri(1.5f)); Düşman(İleri(4f));
                Silahla(Kadro.REHİN); Silahla(Kadro.REHİN); Geç();            // yakala, fırlat (adım)
                var e = Düşman(İleri(1.3f));
                Bekle(Kadro.REHİN, 5f);
                float s0 = _clock.Seconds;
                float h = Hasar(e, () => Silahla(Kadro.FAİZLİ));
                return (h, _clock.Seconds - s0, _pc.SpecialCooldownOf(Kadro.REHİN));
            });
            failed += Check(ref total, $"REHİNLİ KREDİ (REHİN → FAİZLİ): ×1.5 ({rk.yok.h:0.00} → {rk.var.h:0.00}), bedel geri ({rk.yok.saat:+0.00} → {rk.var.saat:+0.00}); REHİN FEKKİ (+) beklemesi sıfır",
                            rk.oldu && Yakın(rk.var.h, rk.yok.h * 1.5f) && Yakın(rk.yok.saat, 0f) && Yakın(rk.var.saat, cost) && rk.var.bek == 5f && rk.artı.bek == 0f, rk.var.h);

            var rv = Üç<(int k, bool çakılı, bool sus)>(ComboTür.RehinVadesi, () =>
            {
                var r = Düşman(İleri(1.5f)); Düşman(İleri(4f));
                Silahla(Kadro.REHİN); Silahla(Kadro.REHİN); Geç();            // yakala, fırlat (adım)
                W(Kadro.TAHVİL); Bas();
                return (_pc.VadeKademesi, (float)Yavaş.GetValue(r) > 0f && (float)YavaşÇarpan.GetValue(r) == 0f, r.Susturulmuş);
            });
            failed += Check(ref total, $"REHİN VADESİ (REHİN → TAHVİL): rehine çakılı ({rv.yok.çakılı} → {rv.var.çakılı}), basılı tutma 1. kademeden ({rv.yok.k} → {rv.var.k}); HAPİS HAKKI (+) susturuyor",
                            rv.oldu && !rv.yok.çakılı && rv.var.çakılı && rv.yok.k == 0 && rv.var.k == 1 && !rv.var.sus && rv.artı.sus, rv.var.k);

            var çk = Üç<(float h, float saat, bool sus)>(ComboTür.İmzalıKredi, () =>
            {
                HasatKapat();
                Düşman(İleri(5f));
                İmzaAt(); Geç();                                                // mermi (adım)
                var e = Düşman(İleri(1.3f));
                float s0 = _clock.Seconds;
                float h = Hasar(e, () => Silahla(Kadro.FAİZLİ));
                return (h, _clock.Seconds - s0, e.Susturulmuş);
            });
            failed += Check(ref total, $"İMZALI KREDİ (İMZA → FAİZLİ): ×1.3 ({çk.yok.h:0.00} → {çk.var.h:0.00}), bedel geri ({çk.yok.saat:+0.00} → {çk.var.saat:+0.00}); KEFİL İMZASI (+) vurulan susuyor",
                            çk.oldu && Yakın(çk.yok.h, faizli) && Yakın(çk.var.h, faizli * 1.3f) && Yakın(çk.yok.saat, 0f) && Yakın(çk.var.saat, cost)
                            && !çk.var.sus && çk.artı.sus, çk.var.h);

            var kç = Üç<(float h, float bek)>(ComboTür.Karşılıklıİmza, () =>
            {
                var e = Düşman(İleri(1.3f));
                İmzaAt(); İmzaAt(); İmzaAt(); Geç();                            // üç isabet (adım)
                Bekle(Kadro.İMZA, 5f);
                float h = Hasar(e, () => Silahla(Kadro.KARŞILIK));             // birikimi indirir
                return (h, _pc.SpecialCooldownOf(Kadro.İMZA));
            });
            float kar = Taban(Kadro.KARŞILIK);
            failed += Check(ref total, $"KARŞILIKLI İMZA (İMZA → KARŞILIK): üç isabet birikene 0.75 sn, KARŞILIK indiriyor ({kç.yok.h:0.00} → {kç.var.h:0.00} = {kar:0.00} + 0.75 × {_t.karsilikDamagePerSecond}); KARŞILIK İMZASI (+) İMZA'nın beklemesi sıfır",
                            kç.oldu && Yakın(kç.yok.h, kar * 0.6f) && Yakın(kç.var.h, kar + 0.75f * _t.karsilikDamagePerSecond) && kç.var.bek == 5f && kç.artı.bek == 0f, kç.var.h);

            var kz = Üç<(float h, float b, bool sus)>(ComboTür.KarşıHaciz, () =>
            {
                var e = Düşman(İleri(1.3f));                                    // HACİZ'in ölü bölgesinde
                Alan("_biriken", 2f);
                Silahla(Kadro.KARŞILIK); Geç();
                float b = (float)Alan("_sonBirikenHasarı");
                float h = Hasar(e, () => Silahla(Kadro.HACİZ));
                return (h, b, e.Susturulmuş);
            });
            failed += Check(ref total, $"KARŞI HACİZ (KARŞILIK → HACİZ): ölü bölge yok, birikim biniyor ({kz.yok.h:0.00} → {kz.var.h:0.00} = {haciz:0.00} + {kz.var.b:0.00}); KARŞI TEBLİĞ (+) susturuyor",
                            kz.oldu && kz.yok.h == 0f && kz.var.b > 0f && Yakın(kz.var.h, haciz + kz.var.b) && !kz.var.sus && kz.artı.sus, kz.var.h);

            var kk = Üç<(float kirasız, float b, float bek)>(ComboTür.KiraKarşılığı, () =>
            {
                Düşman(İleri(1.3f));
                Alan("_biriken", 2f);
                Silahla(Kadro.KARŞILIK); Geç();
                Bekle(Kadro.KARŞILIK, 5f);
                Silahla(Kadro.KİRA);
                var k = _pc.Kule;
                return (k != null ? k.KirasızKalan : -1f, (float)Alan("_sonBiriken"), _pc.SpecialCooldownOf(Kadro.KARŞILIK));
            });
            failed += Check(ref total, $"KİRA KARŞILIĞI (KARŞILIK → KİRA): kule birikim kadar kirasız ({kk.yok.kirasız:0.0} → {kk.var.kirasız:0.0} = {kk.var.b:0.0} sn); KİRA DEPOZİTOSU (+) beklemesi sıfır",
                            kk.oldu && kk.yok.kirasız == 0f && kk.var.b >= 1f && Yakın(kk.var.kirasız, kk.var.b) && kk.var.bek == 5f && kk.artı.bek == 0f, kk.var.kirasız);

            var vr = Üç<(bool tuttu, float h, float bek)>(ComboTür.VadeliRehin, () =>
            {
                Düşman(İleri(1.3f));                                            // TAHVİL'in vurduğu (adım)
                var uzak = Düşman(Yer(0f, -3.5f)); var hedef = Düşman(Yer(0f, -6.5f));   // arkada: REHİN'in menzili dışında
                TahvilBoz(1.1f); Geç();                                        // 2. kademe
                _pGo.transform.rotation = Quaternion.LookRotation(Vector3.back);
                Bekle(Kadro.TAHVİL, 5f);
                Silahla(Kadro.REHİN);                                           // yakala
                bool tuttu = _pc.Rehine == uzak;
                float h = Hasar(hedef, () => Silahla(Kadro.REHİN));            // fırlat
                Ortaya();
                return (tuttu, h, _pc.SpecialCooldownOf(Kadro.TAHVİL));
            });
            failed += Check(ref total, $"VADELİ REHİN (TAHVİL 2. kademe → REHİN): ×2 menzilden yakalıyor ({(vr.var.tuttu ? "tuttu" : "TUTMADI")}), fırlatma ×{v2} ({vr.var.h:0.00} = {fırlat * v2:0.00}); REHİN İTFASI (+) beklemesi sıfır",
                            vr.oldu && !vr.yok.tuttu && vr.var.tuttu && Yakın(vr.var.h, fırlat * v2) && vr.var.bek == 5f && vr.artı.bek == 0f, vr.var.h);

            var vç = Üç<(float h, float aralık, float bek)>(ComboTür.VadeliSenet, () =>
            {
                var e = Düşman(İleri(1.3f));
                TahvilBoz(1.1f); Geç();                                        // 2. kademe
                Bekle(Kadro.İMZA, 5f);
                W(Kadro.İMZA);
                float h = Hasar(e, () => { Vur.Invoke(_pc, null); Uçur(); }); // ilk mermi
                return (h, (float)SavurmaSüresi.GetValue(_pc), _pc.SpecialCooldownOf(Kadro.İMZA));
            });
            failed += Check(ref total, $"VADELİ SENET (TAHVİL 2. kademe → İMZA): seri ×{v2} ({vç.yok.h:0.00} → {vç.var.h:0.00}), ×1.5 hızlı (mermi aralığı {vç.yok.aralık:0.000} → {vç.var.aralık:0.000}); KEŞİDE (+) beklemesi sıfır",
                            vç.oldu && Yakın(vç.var.h, vç.yok.h * v2) && Yakın(vç.yok.aralık / vç.var.aralık, 1.5f) && vç.var.bek == 5f && vç.artı.bek == 0f, vç.var.h);

            var so = Üç<(float yayılan, float çek, float kalan)>(ComboTür.SenetOrtaklığı, () =>
            {
                var a = Düşman(İleri(1.5f)); var b = Düşman(Yer(1.5f, 0f));
                Silahla(Kadro.MÜŞTEREK);                                        // a işaretli
                _pGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
                Silahla(Kadro.MÜŞTEREK);                                        // b → bağ (adım)
                Ortaya(); Geç();
                var bağ = _pc.SonBağ;
                if (bağ == null) return (-1f, -1f, -1f);
                bağ.Step(1f);                                                   // bağ 1 sn eskidi
                float a0 = a.TimeLeft, b0 = b.TimeLeft;
                İmzaAt();                                                       // ilk mermi a
                return (b0 - b.TimeLeft, a0 - a.TimeLeft, bağ.Kalan);
            });
            failed += Check(ref total, $"SENET ORTAKLIĞI (MÜŞTEREK → İMZA): mermi bağlıya değince tam yayıyor ({so.yok.yayılan:0.00} → {so.var.yayılan:0.00} = {so.var.çek:0.00}); SENET YENİLEME (+) bağ tazeleniyor ({so.var.kalan:0.0} → {so.artı.kalan:0.0})",
                            so.oldu && Yakın(so.yok.yayılan, so.yok.çek * _t.musterekShare) && Yakın(so.var.yayılan, so.var.çek) && Yakın(so.var.kalan, _t.musterekSeconds - 1f)
                            && Yakın(so.artı.kalan, _t.musterekSeconds), so.var.yayılan);

            var ov = Üç<(float saat, bool yavaş)>(ComboTür.OrtakVade, () =>
            {
                var (a, _, _) = BağKurgusu(); Geç();
                W(Kadro.TAHVİL); Bas();
                float s0 = _clock.Seconds;
                _pc.Vadelen(1f);
                return (_clock.Seconds - s0, (float)Yavaş.GetValue(a) > 0f);
            });
            failed += Check(ref total, $"ORTAK VADE (MÜŞTEREK → TAHVİL): bağ yaşadıkça basılı tutarken saat iki kat akmıyor ({ov.yok.saat:+0.00;-0.00} → {ov.var.saat:+0.00;-0.00} sn/sn); ORTAK KUPON (+) bağlılar yavaş",
                            ov.oldu && ov.yok.saat < -0.01f && Yakın(ov.var.saat, 0f) && !ov.var.yavaş && ov.artı.yavaş, ov.var.saat);

            var gi = Üç<(float uzaklık, bool görünmez, bool takip)>(ComboTür.Gizliİpotek, () =>
            {
                var e = Bakan(İleri(1.2f), Vector3.forward);                    // sırtı sana: arkadan
                Silahla(Kadro.ARBİTRAJ); Geç();
                Silahla(Kadro.İPOTEK);
                var m = İşaretler().FirstOrDefault();
                if (m == null) return (-1f, false, false);
                Vector3 d = m.transform.position - e.transform.position; d.y = 0f;
                return (d.magnitude, _pc.Görünmez, m.Takip == e.transform);
            });
            failed += Check(ref total, $"GİZLİ İPOTEK (ARBİTRAJ arkadan → İPOTEK): işaret vurduğunun altına ({gi.yok.uzaklık:0.00} → {gi.var.uzaklık:0.00}), görünmez ({gi.var.görünmez}); TAKİPLİ TAPU (+) işaret onunla gidiyor",
                            gi.oldu && gi.yok.uzaklık > 1f && gi.var.uzaklık >= 0f && gi.var.uzaklık < 0.01f && !gi.yok.görünmez && gi.var.görünmez && !gi.var.takip && gi.artı.takip, gi.var.uzaklık);

            var gk = Üç<(float atış, bool görünmez)>(ComboTür.GizliKule, () =>
            {
                Bakan(İleri(1.2f), Vector3.forward);                            // sırtı sana: arkadan
                Silahla(Kadro.ARBİTRAJ); Geç();
                Silahla(Kadro.KİRA);
                var k = _pc.Kule;
                if (k == null) return (-1f, false);
                var hedef = Düşman(Yer(1f, kiraR));
                return (Hasar(hedef, () => k.Step(0.05f)), _pc.Görünmez);
            });
            failed += Check(ref total, $"GİZLİ KULE (ARBİTRAJ arkadan → KİRA): kulenin atışı arkadan ({gk.yok.atış:0.00} → {gk.var.atış:0.00} = ×{_t.arbitrajBackMul}); PERDE ARKASI (+) görünmez",
                            gk.oldu && Yakın(gk.var.atış, gk.yok.atış * _t.arbitrajBackMul) && !gk.var.görünmez && gk.artı.görünmez, gk.var.atış);
            YeniKoşu(); _ara = 0.2f; Combo.SatınAl(ComboTür.GizliKule, -1);
            Bakan(İleri(1.2f), Vector3.back); Silahla(Kadro.ARBİTRAJ); Geç(); Silahla(Kadro.KİRA);
            failed += Check(ref total, "GİZLİ KULE: ARBİTRAJ önden vurduysa combo YOK (şart)", !Yaptı(ComboTür.GizliKule), 0f);

            var kz2 = Üç<(float h, float itme)>(ComboTür.KuleHaczi, () =>
            {
                Silahla(Kadro.KİRA); Geç();                                     // kule nişana
                var e = Düşman(İleri(1.3f));                                    // HACİZ'in ölü bölgesinde
                float p0 = İtmesi(e);
                float h = Hasar(e, () => Silahla(Kadro.HACİZ));
                return (h, İtmesi(e) - p0);
            });
            failed += Check(ref total, $"KULE HACZİ (KİRA → HACİZ): kule dururken ölü bölge yok, ×1.3 ({kz2.yok.h:0.00} → {kz2.var.h:0.00}); ZORLA TAHLİYE (+) kuleden dışa itiyor",
                            kz2.oldu && kz2.yok.h == 0f && Yakın(kz2.var.h, haciz * 1.3f) && kz2.artı.itme > kz2.var.itme + 1f, kz2.var.h);

            var kr = Üç<(bool tuttu, float bek)>(ComboTür.KiracıRehin, () =>
            {
                Silahla(Kadro.KİRA);                                            // kule nişana (adım)
                var k = _pc.Kule;
                var hedef = Düşman(Yer(1f, kiraR));
                if (k != null) k.Step(0.05f);                                   // kule ona ateş eder
                Geç();
                Bekle(Kadro.KİRA, 5f);
                Silahla(Kadro.REHİN);
                Vector3 d = hedef.transform.position - _pGo.transform.position; d.y = 0f;
                return (_pc.Rehine == hedef && d.magnitude < 2f, _pc.SpecialCooldownOf(Kadro.KİRA));   // eline çekildi
            });
            failed += Check(ref total, $"KİRACI REHİN (KİRA → REHİN): kulenin vurduğu uzaktan yakalanıyor ({(kr.var.tuttu ? "tuttu" : "TUTMADI")}); KİRA REHNİ (+) KİRA'nın beklemesi sıfır",
                            kr.oldu && !kr.yok.tuttu && kr.var.tuttu && kr.var.bek == 5f && kr.artı.bek == 0f, 0f);
            return failed;
        }

        // ── 12) PARTİ 4 — üçlüler (2. bölge, satın alınmış) ─────────────────────
        static int Parti4Üçlüler(ref int total)
        {
            int failed = 0;
            float koni = Taban(Kadro.KONİ), haciz = Taban(Kadro.HACİZ), v2 = Tahvil.KademeÇarpanı(2);
            var iflas = Weapon.All[Kadro.İFLAS];
            float ipotekR = Weapon.All[Kadro.İPOTEK].Range(_t);

            var isn = Üç<(float h, float yan, float bek, float s)>(ComboTür.İflasSenedi, () =>
            {
                Düşman(İleri(1.5f)); Düşman(İleri(4f));
                Silahla(Kadro.REHİN); Silahla(Kadro.REHİN); Geç();            // yakala, fırlat (adım)
                var e = Düşman(İleri(1.3f)); var yan = Düşman(Yer(-1f, 1.3f));  // yan: hattın dışında, e'ye en yakın
                Silahla(Kadro.İFLAS); Geç();
                Bekle(Kadro.İFLAS, 5f);
                float s = _clock.Seconds, y0 = yan.TimeLeft;
                float h = Hasar(e, () => Silahla(Kadro.İMZA));                  // ilk mermi
                return (h, y0 - yan.TimeLeft, _pc.SpecialCooldownOf(Kadro.İFLAS), s);
            }, saat: 30f);
            float dmS = Mathf.Max(1f, iflas.DesperationMultiplier(isn.var.s, _t));
            failed += Check(ref total, $"⚠️ İFLAS SENEDİ (REHİN → İFLAS → İMZA): seri ×çaresizlik ({isn.yok.h:0.00} → {isn.var.h:0.00} = ×{dmS:0.00}), mermi yanındakine seker ({isn.yok.yan:0.00} → {isn.var.yan:0.00}); MASA ALACAĞI (+) İFLAS'ın beklemesi sıfır",
                            isn.oldu && dmS > 1.5f && Yakın(isn.var.h, isn.yok.h * dmS) && isn.yok.yan == 0f && Yakın(isn.var.yan, isn.var.h) && isn.var.bek == 5f && isn.artı.bek == 0f, isn.var.h);

            var tk = Üç<(float h, float mh, float iç, float itme, bool tahsil)>(ComboTür.TapuKarşılığı, () =>
            {
                var içte = Düşman(İleri(ipotekR) + Vector3.right);              // kenarda: merkezdekinin itme yönü yok
                Silahla(Kadro.İPOTEK); Geç();
                var m = İşaretler().FirstOrDefault();
                float mh = m != null ? m.Hasar : -1f;
                var e = Düşman(İleri(1.3f));
                Silahla(Kadro.KARŞILIK); Geç();
                float i0 = içte.TimeLeft, p0 = İtmesi(içte);
                float h = Hasar(e, () => Silahla(Kadro.KONİ));
                return (h, mh, i0 - içte.TimeLeft, İtmesi(içte) - p0, Yaptı(ComboTür.KarşılıklıTahsil));
            });
            failed += Check(ref total, $"⚠️ TAPU KARŞILIĞI (İPOTEK → KARŞILIK → KONİ): işaret hemen patlıyor ({tk.yok.iç:0.00} → {tk.var.iç:0.00}), vurduğu KONİ'ye ({tk.yok.h:0.00} → {tk.var.h:0.00} = {koni:0.00} + {tk.var.mh:0.00}), KARŞILIKLI TAHSİL {(tk.var.tahsil ? "TETİKLENDİ" : "tetiklenmedi")}; TAPU TAHLİYESİ (+) itiyor",
                            tk.oldu && tk.yok.iç == 0f && tk.var.iç > 0f && Yakın(tk.var.h, koni + tk.var.mh) && !tk.var.tahsil && tk.artı.itme > tk.var.itme + 1f, tk.var.h);

            var ai = Üç<(float h, bool görünmez, bool gizli, float s)>(ComboTür.Arkadanİcra, () =>
            {
                Combo.SatınAl(ComboTür.GizliHaciz, -1);                         // ARBİTRAJ arkadan → HACİZ de açık: üçlü önce gelmeli
                var e = Bakan(İleri(1.3f), Vector3.forward);                    // sırtı sana: ARBİTRAJ arkadan
                Silahla(Kadro.İFLAS); Geç();
                Silahla(Kadro.ARBİTRAJ); Geç();                                 // arkadan (adım)
                float s = _clock.Seconds;
                float h = Hasar(e, () => Silahla(Kadro.HACİZ));                // ölü bölgede
                return (h, _pc.Görünmez, Yaptı(ComboTür.GizliHaciz), s);
            }, saat: 30f);
            float dmA = Mathf.Max(1f, iflas.DesperationMultiplier(ai.var.s, _t));
            failed += Check(ref total, $"⚠️ ARKADAN İCRA (İFLAS → ARBİTRAJ → HACİZ): ölü bölge yok, ×çaresizlik ×arka ({ai.yok.h:0.00} → {ai.var.h:0.00} = {haciz:0.00} × {dmA:0.00} × {_t.arbitrajBackMul}), GİZLİ HACİZ {(ai.var.gizli ? "TETİKLENDİ" : "tetiklenmedi")}; İCRA PERDESİ (+) görünmez",
                            ai.oldu && dmA > 1.5f && ai.yok.h == 0f && Yakın(ai.var.h, haciz * dmA * _t.arbitrajBackMul) && !ai.var.gizli && !ai.var.görünmez && ai.artı.görünmez, ai.var.h);

            var sk = Üç<(float uzaklık, float kirasız, float kalan, bool işgal, bool kiracı)>(ComboTür.SenetliKira, () =>
            {
                Combo.SatınAl(ComboTür.İşgal, -1);                              // REHİN → KİRA da açık: üçlü önce gelmeli
                Düşman(İleri(5f));
                İmzaAt(); Geç();                                                 // mermi (adım)
                var r = Düşman(İleri(1.5f));
                Silahla(Kadro.REHİN); Silahla(Kadro.REHİN); Geç();            // yakala, fırlat (adım): ileridekinin dibine
                Silahla(Kadro.KİRA);
                var k = _pc.Kule;
                if (k == null) return (-1f, -1f, -1f, false, false);
                Vector3 d = k.transform.position - r.transform.position; d.y = 0f;
                return (d.magnitude, k.KirasızKalan, k.Kalan, Yaptı(ComboTür.İşgal), k.Kiracı == r);
            });
            failed += Check(ref total, $"⚠️ SENETLİ KİRA (İMZA → REHİN → KİRA): kule rehinenin düştüğü yere ({sk.yok.uzaklık:0.00} → {sk.var.uzaklık:0.00}), 5 sn kirasız, ömür +5 ({sk.yok.kalan:0.0} → {sk.var.kalan:0.0}), İŞGAL {(sk.var.işgal ? "TETİKLENDİ" : "tetiklenmedi")}; SENETLİ KİRACI (+) kirayı rehine ödüyor",
                            sk.oldu && sk.yok.uzaklık > 1f && sk.var.uzaklık >= 0f && sk.var.uzaklık < 0.01f && sk.var.kirasız == 5f && Yakın(sk.var.kalan, sk.yok.kalan + 5f, 0.01f)
                            && !sk.var.işgal && !sk.var.kiracı && sk.artı.kiracı, sk.var.kirasız);

            var vk = Üç<(float h, float b, bool yavaş, bool karşılıksız)>(ComboTür.VadeliKarşılık, () =>
            {
                Combo.SatınAl(ComboTür.Karşılıksızİflas, -1);                   // KARŞILIK → İFLAS da açık: üçlü önce gelmeli
                var e = Düşman(İleri(1.3f));
                TahvilBoz(1.1f); Geç();                                        // 2. kademe
                Alan("_biriken", 2f);
                Silahla(Kadro.KARŞILIK); Geç();
                float b = (float)Alan("_sonBirikenHasarı");
                float h = Hasar(e, () => Silahla(Kadro.İFLAS));
                return (h, b, (float)Yavaş.GetValue(e) > 0f, Yaptı(ComboTür.Karşılıksızİflas));
            });
            failed += Check(ref total, $"⚠️ VADELİ KARŞILIK (TAHVİL 2. kademe → KARŞILIK → İFLAS): birikim ×{v2} İFLAS'a ({vk.yok.h:0.00} → {vk.var.h:0.00} = +{v2}×{vk.var.b:0.00}), KARŞILIKSIZ İFLAS {(vk.var.karşılıksız ? "TETİKLENDİ" : "tetiklenmedi")}; KARŞILIK VADESİ (+) yavaşlatıyor",
                            vk.oldu && vk.var.b > 0f && Yakın(vk.var.h, vk.yok.h + v2 * vk.var.b) && !vk.var.karşılıksız && !vk.var.yavaş && vk.artı.yavaş, vk.var.h);

            var go = Üç<(int üye, bool sırt, bool ciro, float bek)>(ComboTür.GizliOrtaklık, () =>
            {
                var a = Düşman(İleri(1.2f)); var c = Düşman(Yer(-1.5f, 3f)); var d = Düşman(Yer(1.5f, 0f));
                Silahla(Kadro.ARBİTRAJ); Geç();                                 // a (adım)
                İmzaAt();                                                       // mermi a'ya (adım)
                _pGo.transform.rotation = Quaternion.LookRotation(c.transform.position - _pGo.transform.position);
                İmzaAt(); Ortaya(); Geç();                                      // ikincisi c'ye — aynı adım
                Bekle(Kadro.ARBİTRAJ, 5f);
                _pGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
                Silahla(Kadro.MÜŞTEREK);                                        // d
                Ortaya();
                var bağ = _pc.SonBağ;
                return (bağ != null ? bağ.Üyeler.Count : 0, d.SırtDönük, Yaptı(ComboTür.ZincirlemeCiro), _pc.SpecialCooldownOf(Kadro.ARBİTRAJ));
            });
            failed += Check(ref total, $"⚠️ GİZLİ ORTAKLIK (ARBİTRAJ → İMZA → MÜŞTEREK): İMZA'nın vurdukları + MÜŞTEREK'in vurduğu tek bağda ({go.yok.üye} → {go.var.üye} üye), sırtları dönük ({go.var.sırt}), ZİNCİRLEME CİRO {(go.var.ciro ? "TETİKLENDİ" : "tetiklenmedi")}; GİZLİ PAY (+) ARBİTRAJ'ın beklemesi sıfır",
                            go.oldu && go.yok.üye == 0 && go.var.üye == 3 && go.var.sırt && !go.var.ciro && go.var.bek == 5f && go.artı.bek == 0f, go.var.üye);

            var sp = Üç<bool>(ComboTür.SpekülatifAtak, () =>
            {
                Combo.SatınAl(ComboTür.SenetOrtaklığı, -1);                     // MÜŞTEREK → İMZA da açık: üçlü önce gelmeli
                var a = Düşman(İleri(1.5f)); Düşman(Yer(1.5f, 0f));
                Silahla(Kadro.ARBİTRAJ); Geç();
                Silahla(Kadro.MÜŞTEREK);                                        // a işaretli
                _pGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
                Silahla(Kadro.MÜŞTEREK); Ortaya(); Geç();                       // bağ (adım)
                Silahla(Kadro.İMZA);
                return Yaptı(ComboTür.SenetOrtaklığı);
            });
            failed += Check(ref total, $"⚠️ SPEKÜLATİF ATAK (ARBİTRAJ → MÜŞTEREK → İMZA) SENET ORTAKLIĞI'ndan önce: üçlü tetikleniyor, ikili {(sp.var ? "TETİKLENDİ" : "tetiklenmedi")}",
                            sp.oldu && !sp.var, 0f);
            return failed;
        }

        // ── 13) SINIRLAR (Faz 43 denetimi): basılı ateş sınırsız büyütmesin · ıskalayan KARŞILIK iki kez yazmasın ──
        static int Sınırları(ref int total)
        {
            int failed = 0;
            int seri = Mathf.RoundToInt(_t.imzaShotsPerSecond);

            // SENET İSKONTOSU: on isabet de bir seri kadar (+0.6), sınırsız değil
            YeniKoşu(); _ara = 0.2f; Combo.SatınAl(ComboTür.Senetİskontosu, -1);
            Düşman(İleri(5f));
            for (int i = 0; i < 10; i++) İmzaAt();
            Geç();
            W(Kadro.TAHVİL); Bas();
            float vade = (float)Alan("_vade");

            // İBRAZ: on isabet de bir serilik mermi olarak döner
            (float h, float son) İbraz(float ara)
            {
                YeniKoşu(); _ara = ara; SilahSunağı.Al(SilahYükseltme.TopluİmzaArtı, -1);
                var e = Düşman(İleri(1.3f));
                for (int i = 0; i < 10; i++) İmzaAt();
                Geç();
                float son = ((float[])Alan("_sonHasar"))[Kadro.İMZA];
                return (Hasar(e, () => Silahla(Kadro.İFLAS)), son);
            }
            var ibY = İbraz(1f); var ibV = İbraz(0.2f);
            failed += Check(ref total, $"⚠️ basılı ateş sınırsız büyümüyor: on isabet SENET İSKONTOSU'nda +{vade:0.00} (= {seri} × 0.1), İBRAZ'da +{ibV.h - ibY.h:0.00} (= {seri} × {ibV.son:0.00} × 1.5)",
                            Yakın(vade, 0.1f * seri) && Yakın(ibV.h - ibY.h, seri * ibV.son * 1.5f), vade);

            // KARŞILIK ıskalarsa combo tetiklenmez, pencere açık kalır — ikinci savurma aynı birikimi bir daha yazmamalı
            (float b1, float b2) İkiIska(ComboTür c, System.Action adım)
            {
                YeniKoşu(); _ara = 0.2f;
                if (Combo.Bu(c).Açan is SilahYükseltme açan) SilahSunağı.Al(açan, -1);
                if (Combo.Bu(c).Satın) Combo.SatınAl(c, -1);
                adım(); Geç();
                _pGo.transform.rotation = Quaternion.LookRotation(Vector3.back);   // arkada kimse yok: ıska
                Silahla(Kadro.KARŞILIK); float b1 = _pc.Biriken;
                Silahla(Kadro.KARŞILIK); float b2 = _pc.Biriken;
                Ortaya();
                return (b1, b2);
            }
            var ko = İkiIska(ComboTür.Konkordato, () => { Düşman(İleri(1.3f)); Silahla(Kadro.İFLAS); });
            var rk = İkiIska(ComboTür.RehinKarşılığı, () => { Düşman(İleri(1.5f)); Düşman(İleri(4f)); Silahla(Kadro.REHİN); Silahla(Kadro.REHİN); });
            var ki = İkiIska(ComboTür.Karşılıklıİmza, () => { Düşman(İleri(1.3f)); İmzaAt(); İmzaAt(); İmzaAt(); });
            failed += Check(ref total, $"⚠️ ıskalayan KARŞILIK birikimi bir kez yazıyor: KONKORDATO {ko.b1:0.00} → {ko.b2:0.00}, REHİN KARŞILIĞI {rk.b1:0.00} → {rk.b2:0.00}, KARŞILIKLI İMZA {ki.b1:0.00} → {ki.b2:0.00}",
                            ko.b1 > 0f && ko.b2 == ko.b1 && rk.b1 > 0f && rk.b2 == rk.b1 && ki.b1 > 0f && ki.b2 == ki.b1, ki.b2 - ki.b1);
            return failed;
        }

        // ── yardımcılar ─────────────────────────────────────────────────────────
        static void Temizle()
        {
            ImzaMermisi.Temizle();
            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
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
