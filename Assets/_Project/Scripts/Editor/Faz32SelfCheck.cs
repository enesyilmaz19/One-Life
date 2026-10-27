using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 32 — TAHVİL · MÜŞTEREK · ARBİTRAJ · KİRA (Enes 2026-10-18 "sonraki faza geç"). Her hamle
    /// ETKİSİYLE: basılı tutmak gerçekten olgunlaştırıyor ve saati gerçekten iki kat akıtıyor mu, bağlıya
    /// hasar gerçekten iniyor mu, arkadan vuruş gerçekten sert mi, kule gerçekten kira ödeyip ateş ediyor mu.
    /// </summary>
    public static class Faz32SelfCheck
    {
        const BindingFlags Gizli = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tek Can/Faz 32 öz-denetimi çalıştır (TAHVİL · MÜŞTEREK · ARBİTRAJ · KİRA)")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        static Tuning _t;
        static Clock _clock;
        static PlayerController _pc;
        static GameObject _pGo, _root;
        static MethodInfo PC(string ad) => typeof(PlayerController).GetMethod(ad, Gizli);
        static readonly MethodInfo Vur = PC("Strike"), Başla = PC("StartAttack"), Sağ = PC("StartSpecial"),
                                   DashAtak = PC("DashStrike"), DashBaşla = PC("StartDash");
        static readonly MethodInfo Gir = typeof(RoomManager).GetMethod("EnterRoom", Gizli);
        static readonly MethodInfo Güncelle = typeof(RoomManager).GetMethod("Update", Gizli);
        static readonly MethodInfo Al = typeof(Altar).GetMethod("Buy", Gizli);
        static readonly MethodInfo DüşmanAdımı = typeof(Enemy).GetMethod("TickState", Gizli);
        static readonly FieldInfo İtme = typeof(Enemy).GetField("_push", Gizli);

        public static int Execute()
        {
            int failed = 0, total = 0;
            _t = ScriptableObject.CreateInstance<Tuning>();
            _t.startSeconds = 300f;
            _t.enemyTimePool = 200f;
            _t.enemyDecayPerSecond = 0f;
            _t.harvestSoftCap = 1e6f;
            Region.Reset();
            Temizle();

            _root = new GameObject("faz32-check");
            _clock = _root.AddComponent<Clock>();
            _pGo = null; _pc = null;

            failed += Kadrosu(ref total);
            failed += Tahvili(ref total);
            failed += Müştereği(ref total);
            failed += Arbitrajı(ref total);
            failed += Kirayı(ref total);
            failed += Comboları(ref total);
            failed += Verisi(ref total);

            Temizle();
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_t);
            Clock.Reset(); Region.Reset(); RoomManager.Reset();
            Debug.Log($"[Faz 32 öz-denetim] {(failed == 0 ? "✅" : "❌")} {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── kurgu ──────────────────────────────────────────────────────────────
        static void YeniKoşu(float saat = 300f)
        {
            Temizle();
            SaveFile.UseInMemory(new SaveData());
            if (_pGo != null) Object.DestroyImmediate(_pGo);
            _pGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _pGo.transform.SetParent(_root.transform);
            Ortaya();
            _pc = _pGo.AddComponent<PlayerController>();
            _pc.Init(_t, null);
            _clock.Init(_t, _pGo.transform, saat);
            for (int w = 0; w < Weapon.All.Length; w++) Kadro.Aç(w, "test");
        }

        static void Ortaya()
        {
            _pGo.transform.position = Bootstrap.ArenaCenter;
            _pGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        }

        static void SıfırKadro() { var s = SaveFile.Current; s.unlockedWeapons = null; s.loadout = null; }

        static RoomManager Oda(int oda, int bölge)
        {
            var go = new GameObject("faz32-oda"); go.transform.SetParent(_root.transform);
            var rm = go.AddComponent<RoomManager>();
            rm.Init(_t, _pGo.transform, null, startRoom: oda, startRegion: bölge);
            Ortaya();
            return rm;
        }

        static void OdayıSil(RoomManager rm) { Object.DestroyImmediate(rm.gameObject); RoomManager.Reset(); }

        /// <summary>Düşman; `bakış` verilirse o yöne döner (ARBİTRAJ'ın konum ölçüsü düşmanın yüzüne bakıyor).</summary>
        static Enemy Düşman(Vector3 yer, Vector3? bakış = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = yer;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(_t, _pGo.transform, null);
            if (bakış.HasValue) e.transform.rotation = Quaternion.LookRotation(bakış.Value);
            return e;
        }

        static Vector3 Yer(float x, float z) => Bootstrap.ArenaCenter + new Vector3(x, 0f, z);
        static Vector3 İleri(float m) => Yer(0f, m);
        static Vector3 Düz(Vector3 v) { v.y = 0f; return v; }
        static float AD => _clock.AttackDamage;
        static void W(int silah) => _pc.SetWeapon(silah);
        static void Silahla(int silah) { _pc.SetWeapon(silah); Vur.Invoke(_pc, null); }
        static void SağTık() => Sağ.Invoke(_pc, new object[] { Vector3.forward });
        static void Bas() => Başla.Invoke(_pc, new object[] { Vector3.forward });
        static void Dash() => DashAtak.Invoke(_pc, null);
        static object Alan(string ad) => typeof(PlayerController).GetField(ad, Gizli).GetValue(_pc);
        static void Alan(string ad, object v) => typeof(PlayerController).GetField(ad, Gizli).SetValue(_pc, v);
        static void Yükselt(SilahYükseltme y) => SilahSunağı.Al(y, 0);
        static void Artı(ComboTür c) { var s = SaveFile.Current; s.comboUpgrades = new int[Combo.Sayı]; s.comboUpgrades[(int)c] = 1; }
        static float Hasar(Enemy e, System.Action a) { float önce = e.TimeLeft; a(); return önce - e.TimeLeft; }
        static float Mul(int silah) => Weapon.All[silah].Damage(_t, 1f);   // silahın hasar çarpanı

        /// <summary>Saatten HARCANAN (hasat dışarıda): akış yok (Tick çağrılmıyor), yalnızca Spend.</summary>
        static float Harcanan(System.Action a)
        {
            float s0 = _clock.Seconds, h0 = _clock.TotalHarvested;
            a();
            return s0 + (_clock.TotalHarvested - h0) - _clock.Seconds;
        }

        // ── 1) KADRO: TAHVİL 3. boss · MÜŞTEREK 4. bölge sunak · ARBİTRAJ 4. boss · KİRA 5. bölge keşif ───
        static int Kadrosu(ref int total)
        {
            int failed = 0;
            bool BossAçar(int bölge, int silah, int açmamalı)
            {
                YeniKoşu(); SıfırKadro();
                var rm = Oda(_t.RoomsInRegion, bölge);
                HepsiniÖldür(); Güncelle.Invoke(rm, null);
                bool ok = Kadro.Açık(silah) && !Kadro.Açık(açmamalı);
                OdayıSil(rm);
                return ok;
            }
            bool tahvil = BossAçar(2, Kadro.TAHVİL, Kadro.ARBİTRAJ), arbitraj = BossAçar(3, Kadro.ARBİTRAJ, Kadro.TAHVİL);
            failed += Check(ref total, $"⚠️ TAHVİL 3. boss'tan ({(tahvil ? "açıldı" : "AÇILMADI")}), ARBİTRAJ 4. boss'tan ({(arbitraj ? "açıldı" : "AÇILMADI")}) — ötekini açmadan",
                            tahvil && arbitraj, 0f);

            YeniKoşu(); SıfırKadro();
            var oda = Oda(RoomManager.SunakOdası(_t, 1), 3);
            bool erken = Sunaklar(Altar.Kind.Unlock).Any(a => a.Yuva == Kadro.MÜŞTEREK);
            Gir.Invoke(oda, new object[] { RoomManager.SunakOdası(_t, 2) });
            var sunak = Sunaklar(Altar.Kind.Unlock).FirstOrDefault(a => a.Yuva == Kadro.MÜŞTEREK);
            if (sunak != null) Al.Invoke(sunak, null);
            failed += Check(ref total, $"⚠️ MÜŞTEREK 4. bölgenin 3. ara bölgesinin sunağında (2.'sinde {(erken ? "VAR" : "yok")}), alınca açık",
                            !erken && sunak != null && Kadro.Açık(Kadro.MÜŞTEREK), 0f);
            OdayıSil(oda);

            YeniKoşu(); SıfırKadro();
            oda = Oda(2 * _t.BlokBoyu + Kadro.KeşifOdası, 4);
            HepsiniÖldür();
            for (int i = 0; i < _t.roomWaves + 1; i++) { Güncelle.Invoke(oda, null); HepsiniÖldür(); }
            var köşe = Object.FindObjectsByType<GizliSilah>(FindObjectsInactive.Exclude).FirstOrDefault();
            if (köşe != null) _pGo.transform.position = köşe.transform.position;
            bool alındı = köşe != null && köşe.Silah == Kadro.KİRA && köşe.Step();
            failed += Check(ref total, $"⚠️ KİRA keşifle: 5. bölgenin 3. ara bölgesinde köşe {(köşe != null ? Weapon.All[köşe.Silah].Name : "YOK")}",
                            alındı && Kadro.Açık(Kadro.KİRA), 0f);
            OdayıSil(oda);
            return failed;
        }

        // ── 2) TAHVİL ─────────────────────────────────────────────────────────
        static int Tahvili(ref int total)
        {
            int failed = 0;
            float m = Mul(Kadro.TAHVİL);

            // Basılı tutmak olgunlaştırıyor: bırakınca kademe çarpanı
            float Bırak(float tut, SilahYükseltme? y = null)
            {
                YeniKoşu();
                if (y.HasValue) Yükselt(y.Value);
                var e = Düşman(İleri(1.4f));
                W(Kadro.TAHVİL);
                Bas();
                _pc.Vadelen(tut);
                return Hasar(e, () => Vur.Invoke(_pc, null));
            }
            float k0 = Bırak(0f), k3 = Bırak(1.6f);
            failed += Check(ref total, $"⚠️⚠️ TAHVİL: basılı tutunca olgunlaşıyor (hemen {k0:0.0} = ×0.7, 1.6 sn {k3:0.0} = ×4.5)",
                            Mathf.Abs(k0 - AD * m * 0.7f) < 0.01f && Mathf.Abs(k3 - AD * m * 4.5f) < 0.01f, k3 / Mathf.Max(0.001f, k0));

            // Beklerken saat iki kat akar (akışın tek kaynağından) · İSKONTO yarım kat
            float Bekleme(bool iskonto)
            {
                YeniKoşu();
                if (iskonto) Yükselt(SilahYükseltme.İskonto);
                W(Kadro.TAHVİL); Bas();
                return Harcanan(() => _pc.Vadelen(1f));
            }
            float akış = _clock.AkışHızı, b0 = Bekleme(false), b1 = Bekleme(true);
            failed += Check(ref total, $"⚠️⚠️ beklerken saat bir kat daha akıyor (1 sn: {b0:0.00} = akış {akış:0.00}); İSKONTO (+) yarım ({b1:0.00})",
                            akış > 0f && Mathf.Abs(b0 - akış) < 0.001f && Mathf.Abs(b1 - akış * 0.5f) < 0.001f, b0);

            // KISA VADE (+) kademeler ×0.7 sürede · YÜKSEK FAİZ (+) 3. kademe ×1.4
            int KademeSonra(float sn, bool kısa)
            {
                YeniKoşu();
                if (kısa) Yükselt(SilahYükseltme.KısaVade);
                W(Kadro.TAHVİL); Bas();
                _pc.Vadelen(sn);
                return _pc.VadeKademesi;
            }
            int düz = KademeSonra(1.1f, false), kısa = KademeSonra(1.1f, true);
            failed += Check(ref total, $"KISA VADE (+): 1.1 sn'de 3. kademe ({düz} → {kısa})", düz == 2 && kısa == 3, kısa);
            float yüksek = Bırak(1.6f, SilahYükseltme.YüksekFaiz);
            failed += Check(ref total, $"YÜKSEK FAİZ (+): 3. kademe ×1.4 ({k3:0.0} → {yüksek:0.0})", Mathf.Abs(yüksek / k3 - 1.4f) < 0.01f, yüksek / Mathf.Max(0.001f, k3));

            // VADESİ GELMİŞ: olgunluk önündeki üç düşmana bölüşülüyor; dördüncü dışarıda
            YeniKoşu();
            var üç = new[] { Düşman(Yer(-0.8f, 1.6f)), Düşman(Yer(0f, 1.8f)), Düşman(Yer(0.8f, 1.6f)) };
            var uzak = Düşman(İleri(3.2f));
            float[] önce = üç.Select(e => e.TimeLeft).ToArray();
            float uzakÖnce = uzak.TimeLeft;
            W(Kadro.TAHVİL); Bas(); _pc.Vadelen(1.1f);           // 2. kademe
            SağTık();
            float[] aldı = üç.Select((e, i) => önce[i] - e.TimeLeft).ToArray();
            float beklenen = AD * m * 2.8f * 0.5f;
            failed += Check(ref total, $"⚠️⚠️ TAHVİL · VADESİ GELMİŞ: 2. kademe üç düşmana ({string.Join(" / ", aldı.Select(x => x.ToString("0.0")))} = {beklenen:0.0}), dördüncüye {uzakÖnce - uzak.TimeLeft:0}",
                            aldı.All(x => Mathf.Abs(x - beklenen) < 0.01f) && uzak.TimeLeft == uzakÖnce && _pc.VadeKademesi == -1, aldı.Sum());

            // ERKEN BOZDURMA: basılı tutarken dash = olgunluğun yarısı, dash yönünde geniş
            float Bozdurma(bool tut)
            {
                YeniKoşu();
                var e = Düşman(İleri(1.4f));
                W(Kadro.TAHVİL);
                if (tut) { Bas(); _pc.Vadelen(1.1f); }
                DashBaşla.Invoke(_pc, new object[] { Vector3.forward });
                bool sırada = (bool)Alan("_dashAttackQueued");
                Ortaya();
                float h = Hasar(e, Dash);
                return tut && !sırada ? -1f : h;
            }
            float bozdur = Bozdurma(true), düzDash = Bozdurma(false);
            float dashÇ = Weapon.All[Kadro.TAHVİL].DashDamageMultiplier(_t);
            failed += Check(ref total, $"⚠️⚠️ TAHVİL · ERKEN BOZDURMA: basılı dash kendiliğinden vuruyor, olgunluğun yarısı dash-atağa biniyor ({bozdur:0.0} = düz dash-atak {düzDash:0.0} × 1.4)",
                            Mathf.Abs(düzDash - AD * m * dashÇ) < 0.01f && Mathf.Abs(bozdur - düzDash * 1.4f) < 0.01f, bozdur / Mathf.Max(0.001f, düzDash));

            YeniKoşu();
            W(Kadro.TAHVİL); Bas(); _pc.Vadelen(1f);
            _pc.SetWeapon(Kadro.KONİ);
            failed += Check(ref total, $"silah değişince olgunluk gidiyor (kademe {_pc.VadeKademesi})", _pc.VadeKademesi == -1, _pc.VadeKademesi);
            return failed;
        }

        // ── 3) MÜŞTEREK ───────────────────────────────────────────────────────
        /// <summary>MÜŞTEREK'le iki düşmanı bağla: öndekini vur, sağa dönüp yandakini vur.</summary>
        static (Enemy a, Enemy b) Bağla()
        {
            var a = Düşman(İleri(1.5f));
            var b = Düşman(Yer(1.5f, 0f));
            Silahla(Kadro.MÜŞTEREK);
            _pGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
            Silahla(Kadro.MÜŞTEREK);
            Ortaya();
            return (a, b);
        }

        static int Müştereği(ref int total)
        {
            int failed = 0;

            YeniKoşu();
            var (a, b) = Bağla();
            var bağ = _pc.SonBağ;
            failed += Check(ref total, $"⚠️⚠️ MÜŞTEREK: ilk vuruş işaretliyor, ikincisi bağlıyor ({(bağ != null ? bağ.Üyeler.Count : 0)} üye)",
                            bağ != null && bağ.Üyeler.Contains(a) && bağ.Üyeler.Contains(b) && a.Bağ == bağ && b.Bağ == bağ, 0f);

            // İşaret halkasıyla aynı ömürde: halka söndükten sonra vurulan yeni bir işaret, bağ değil
            YeniKoşu();
            var eskiA = Düşman(İleri(1.5f)); var eskiB = Düşman(Yer(1.5f, 0f));
            Silahla(Kadro.MÜŞTEREK);
            _pc.TickCombo(3f);
            _pGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
            Silahla(Kadro.MÜŞTEREK);
            Ortaya();
            failed += Check(ref total, $"⚠️ işaret halkası söndükten sonra (3 sn) vurulan bağlanmıyor, yeni işaret oluyor ({(_pc.SonBağ == null ? "bağ yok" : "BAĞLANDI")})",
                            _pc.SonBağ == null && eskiA.Bağ == null && eskiB.Bağ == null, 0f);
            YeniKoşu();
            (a, b) = Bağla();
            _pc.TickCombo(1f);                          // pencere geçsin: MÜŞTEREK → KONİ artık MÜŞTEREK KUSUR (Faz 42)

            // Yayılma: bağlıya, BAŞKA silahla vurulan hasarın %60'ı iniyor
            b.transform.position = Yer(4f, 0f);                 // KONİ'nin dışında
            float bÖnce = b.TimeLeft;
            float aAldı = Hasar(a, () => Silahla(Kadro.KONİ));
            float bAldı = bÖnce - b.TimeLeft;
            failed += Check(ref total, $"⚠️⚠️ bağ her kaynaktan: KONİ öndekine {aAldı:0.0}, bağlıya {bAldı:0.0} (= %{_t.musterekShare * 100f:0})",
                            aAldı > 0f && Mathf.Abs(bAldı - aAldı * _t.musterekShare) < 0.01f, bAldı / Mathf.Max(0.001f, aAldı));

            // SIKI BAĞ (+) %90 · UZUN BAĞ (+) süre ×2 · ORTAK HASAT (+) yayılanın hasadı tam
            YeniKoşu();
            Yükselt(SilahYükseltme.SıkıBağ); Yükselt(SilahYükseltme.UzunBağ); Yükselt(SilahYükseltme.OrtakHasat);
            (a, b) = Bağla();
            _pc.TickCombo(1f);                          // MÜŞTEREK KUSUR olmasın (Faz 42)
            bağ = _pc.SonBağ;
            b.transform.position = Yer(4f, 0f);
            bÖnce = b.TimeLeft;
            aAldı = Hasar(a, () => Silahla(Kadro.KONİ));
            bAldı = bÖnce - b.TimeLeft;
            failed += Check(ref total, $"SIKI BAĞ (+): yayılan %90 ({bAldı / Mathf.Max(0.001f, aAldı):0.00})", Mathf.Abs(bAldı - aAldı * 0.9f) < 0.01f, bAldı / Mathf.Max(0.001f, aAldı));
            failed += Check(ref total, $"UZUN BAĞ (+) süre ×2 ({(bağ != null ? bağ.Kalan : 0):0} sn) · ORTAK HASAT (+) yayılanın hasadı tam ({(bağ != null ? bağ.HasatPayı : 0):0.0})",
                            bağ != null && Mathf.Abs(bağ.Kalan - _t.musterekSeconds * 2f) < 0.01f && bağ.HasatPayı == 1f, 0f);

            // Süre dolunca kopuyor
            YeniKoşu();
            (a, b) = Bağla();
            _pc.TickCombo(1f);                          // MÜŞTEREK KUSUR olmasın (Faz 42)
            bağ = _pc.SonBağ;
            bool kuruldu = bağ != null;
            if (kuruldu) bağ.Step(_t.musterekSeconds + 0.1f);
            failed += Check(ref total, $"bağ {_t.musterekSeconds:0} sn sonra kopuyor (üyelerde bağ {(a.Bağ == null && b.Bağ == null ? "yok" : "KALDI")})",
                            kuruldu && bağ.Bitti && a.Bağ == null && b.Bağ == null, 0f);

            // ZİNCİRLEME: en yakın dört düşman tek zincire, beşincisi dışarıda
            YeniKoşu();
            var dört = new[] { Düşman(Yer(1.2f, 0f)), Düşman(Yer(-1.2f, 0f)), Düşman(İleri(1.4f)), Düşman(İleri(-1.6f)) };
            var beşinci = Düşman(Yer(3.5f, 3.5f));
            W(Kadro.MÜŞTEREK); SağTık();
            bağ = _pc.SonBağ;
            failed += Check(ref total, $"⚠️⚠️ MÜŞTEREK · ZİNCİRLEME: en yakın dördü tek zincirde ({(bağ != null ? bağ.Üyeler.Count : 0)} üye), beşincisi {(beşinci.Bağ == null ? "dışarıda" : "İÇERİDE")}",
                            bağ != null && bağ.Üyeler.Count == 4 && dört.All(e => e.Bağ == bağ) && beşinci.Bağ == null, 0f);

            // KOPARMA: dash bağın üstünden geçerse kopuyor, uçlar ×2 yiyip ortaya çekiliyor
            float Koparma(bool kesiyor)
            {
                YeniKoşu();
                var u1 = Düşman(Yer(-1.2f, 2f)); var u2 = Düşman(Yer(1.2f, 2f));      // MÜŞTEREK'in menzilinde
                _pc.SetWeapon(Kadro.MÜŞTEREK);
                _pGo.transform.rotation = Quaternion.LookRotation(u1.transform.position - _pGo.transform.position);
                Silahla(Kadro.MÜŞTEREK);
                _pGo.transform.rotation = Quaternion.LookRotation(u2.transform.position - _pGo.transform.position);
                Silahla(Kadro.MÜŞTEREK);
                var bg = _pc.SonBağ;
                if (bg == null) return -1f;
                Alan("_dashBaşı", Bootstrap.ArenaCenter);
                _pGo.transform.position = kesiyor ? İleri(5f) : Yer(5f, 0f);
                _pGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                float önce1 = u1.TimeLeft;
                Dash();
                float itildi = ((Vector3)İtme.GetValue(u1)).magnitude;
                return bg.Bitti && itildi > 1f ? önce1 - u1.TimeLeft : 0f;
            }
            float koptu = Koparma(true), kopmadı = Koparma(false);
            failed += Check(ref total, $"⚠️⚠️ MÜŞTEREK · KOPARMA: bağın üstünden dash → koptu, uç ×2 yedi ({koptu:0.0} = 2 × {AD * Mul(Kadro.MÜŞTEREK):0.0}) ve çekildi; üstünden geçmeyen dash koparmıyor ({kopmadı:0})",
                            Mathf.Abs(koptu - 2f * AD * Mul(Kadro.MÜŞTEREK)) < 0.01f && kopmadı == 0f, koptu);
            return failed;
        }

        // ── 4) ARBİTRAJ ───────────────────────────────────────────────────────
        static int Arbitrajı(ref int total)
        {
            int failed = 0;
            float m = Mul(Kadro.ARBİTRAJ);
            float Vuruş(Vector3 bakış, SilahYükseltme? y = null)
            {
                YeniKoşu();
                if (y.HasValue) Yükselt(y.Value);
                var e = Düşman(İleri(1.2f), bakış);
                return Hasar(e, () => Silahla(Kadro.ARBİTRAJ));
            }
            float ön = Vuruş(Vector3.back), yan = Vuruş(Vector3.right), arka = Vuruş(Vector3.forward);
            failed += Check(ref total, $"⚠️⚠️ ARBİTRAJ: önden {ön:0.0} (×{_t.arbitrajFrontMul}), yandan {yan:0.0} (×1), arkadan {arka:0.0} (×{_t.arbitrajBackMul})",
                            Mathf.Abs(ön - AD * m * _t.arbitrajFrontMul) < 0.01f && Mathf.Abs(yan - AD * m) < 0.01f
                            && Mathf.Abs(arka - AD * m * _t.arbitrajBackMul) < 0.01f, arka / Mathf.Max(0.001f, ön));

            float keskin = Vuruş(Vector3.forward, SilahYükseltme.KeskinBıçak);
            Vector3 çapraz = Quaternion.Euler(0f, 100f, 0f) * Vector3.back;       // 100°: geniş açıyla arka, düz yan
            float çaprazDüz = Vuruş(çapraz), çaprazGeniş = Vuruş(çapraz, SilahYükseltme.GenişAçı);
            failed += Check(ref total, $"KESKİN BIÇAK (+) arkadan ×3.5 ({keskin:0.0}) · GENİŞ AÇI (+) 100°'yi arka sayıyor ({çaprazDüz:0.0} → {çaprazGeniş:0.0})",
                            Mathf.Abs(keskin - AD * m * _t.arbitrajBackMul * 1.4f) < 0.01f && Mathf.Abs(çaprazDüz - AD * m) < 0.01f
                            && Mathf.Abs(çaprazGeniş - AD * m * _t.arbitrajBackMul) < 0.01f, keskin);

            // AÇIK POZİSYON: görünmez → düşman saldırı başlatmıyor; ilk vuruş önden de arkadan, sonra görünür
            YeniKoşu();
            var yakın = Düşman(İleri(1f), Vector3.back);
            typeof(Enemy).GetField("_attackTimer", Gizli).SetValue(yakın, 0f);
            W(Kadro.ARBİTRAJ); SağTık();
            bool görünmez = _pc.Görünmez;
            DüşmanAdımı.Invoke(yakın, new object[] { 0.01f });
            bool saldırmadı = yakın.CurrentState == Enemy.State.Chase;
            float görünmezVuruş = Hasar(yakın, () => Vur.Invoke(_pc, null));
            bool göründü = !_pc.Görünmez;
            DüşmanAdımı.Invoke(yakın, new object[] { 0.01f });
            bool sonraSaldırdı = yakın.CurrentState == Enemy.State.Windup;
            failed += Check(ref total, $"⚠️⚠️ ARBİTRAJ · AÇIK POZİSYON: görünmezken düşman {(saldırmadı ? "saldırmadı" : "SALDIRDI")}, önden vuruş arkadan sayıldı ({görünmezVuruş:0.0}), vurunca {(göründü ? "göründün" : "GÖRÜNMEZ KALDIN")}, sonra saldırdı ({(sonraSaldırdı ? "evet" : "HAYIR")})",
                            görünmez && saldırmadı && Mathf.Abs(görünmezVuruş - AD * m * _t.arbitrajBackMul) < 0.01f && göründü && sonraSaldırdı, görünmezVuruş);

            YeniKoşu();
            Yükselt(SilahYükseltme.Gölge);
            W(Kadro.ARBİTRAJ); SağTık();
            float gölge = (float)Alan("_görünmezKalan");
            failed += Check(ref total, $"GÖLGE (+): görünmezlik ×1.5 ({gölge:0.0} sn)", Mathf.Abs(gölge - _t.arbitrajHideSeconds * 1.5f) < 0.001f, gölge);

            // TERS İŞLEM: içinden geçtiğin (sana dönük) düşmana dönüp arkadan vurursun
            float Ters(bool geçti)
            {
                YeniKoşu();
                var e = Düşman(İleri(2f), Vector3.back);
                W(Kadro.ARBİTRAJ);
                Alan("_dashBaşı", Bootstrap.ArenaCenter);
                _pGo.transform.position = geçti ? İleri(4f) : Yer(4f, 0f);
                _pGo.transform.rotation = Quaternion.LookRotation(geçti ? Vector3.forward : Vector3.right);
                return Hasar(e, Dash);
            }
            float ters = Ters(true), geçmedi = Ters(false);
            float beklenen = AD * m * Weapon.All[Kadro.ARBİTRAJ].DashDamageMultiplier(_t) * _t.arbitrajBackMul;
            failed += Check(ref total, $"⚠️⚠️ ARBİTRAJ · TERS İŞLEM: içinden geçtiğine dönüp arkadan ({ters:0.0} = {beklenen:0.0}); geçmediysen ona vuruş yok ({geçmedi:0})",
                            Mathf.Abs(ters - beklenen) < 0.01f && geçmedi == 0f, ters);
            return failed;
        }

        // ── 5) KİRA ───────────────────────────────────────────────────────────
        static int Kirayı(ref int total)
        {
            int failed = 0;
            float m = Mul(Kadro.KİRA);
            float kurma = Weapon.All[Kadro.KİRA].Range(_t);

            YeniKoşu();
            Silahla(Kadro.KİRA);
            var kule = _pc.Kule;
            bool yerinde = kule != null && Düz(kule.transform.position - İleri(kurma)).magnitude < 0.01f;
            float boşKira = kule != null ? Harcanan(() => kule.Step(1f)) : 0f;
            failed += Check(ref total, $"⚠️⚠️ KİRA: nişana (menzile kırpılmış) kule kuruluyor ({(yerinde ? "yerinde" : "YANLIŞ YERDE")}), boşken de kira ödüyor ({boşKira:0.00} sn/sn = {_t.kiraRentPerSecond})",
                            yerinde && Mathf.Abs(boşKira - _t.kiraRentPerSecond) < 0.001f, boşKira);

            var hedef = Düşman(İleri(kurma - 2f));
            float h0 = _clock.TotalHarvested;
            float atış = Hasar(hedef, () => kule.Step(_t.kiraShotInterval));
            failed += Check(ref total, $"⚠️⚠️ kule menzilindekine ateş ediyor ({atış:0.0} = silah × {_t.kiraShotMul}) ve hasat ediyor (+{_clock.TotalHarvested - h0:0.00})",
                            Mathf.Abs(atış - AD * m * _t.kiraShotMul) < 0.01f && _clock.TotalHarvested > h0, atış);

            var önde = Düşman(İleri(1.2f));
            float savurma = Hasar(önde, () => Silahla(Kadro.KİRA));
            failed += Check(ref total, $"kule varken sol tık ikinci kule değil, zayıf savurma ({KiraKulesi.Tümü.Count} kule, {savurma:0.0} = ×{m})",
                            KiraKulesi.Tümü.Count == 1 && Mathf.Abs(savurma - AD * m) < 0.01f, KiraKulesi.Tümü.Count);

            // TAŞINMA (dash-atak): kule bulunduğun yere
            _pGo.transform.position = Yer(-3f, -2f);
            Dash();
            bool taşındı = Düz(kule.transform.position - _pGo.transform.position).magnitude < 0.01f;
            failed += Check(ref total, $"⚠️⚠️ KİRA · TAŞINMA: dash-atak kuleyi bulunduğun yere taşıyor ({(taşındı ? "taşındı" : "TAŞINMADI")})", taşındı, 0f);

            // TAHLİYE (sağ tık): kalan kira süresi kadar hasar; kule yokken bekleme yanmıyor
            YeniKoşu();
            Silahla(Kadro.KİRA);
            kule = _pc.Kule;
            var yanında = Düşman(İleri(kurma + 1f));
            kule.Step(2f);
            float kalan = kule.Kalan, tahliye = 0f;
            float fesih = Harcanan(() => tahliye = Hasar(yanında, SağTık));
            bool gitti = _pc.Kule == null;
            YeniKoşu();
            W(Kadro.KİRA); SağTık();
            float boştaBekleme = _pc.SpecialCooldownLeft;
            failed += Check(ref total, $"⚠️⚠️ KİRA · TAHLİYE: kalan {kalan:0} sn → {tahliye:0.0} (= silah × {_t.kiraEvictionPerSecond} × {kalan:0}), kule {(gitti ? "gitti" : "DURUYOR")}; kule yokken bekleme {boştaBekleme:0.0}",
                            Mathf.Abs(tahliye - AD * m * _t.kiraEvictionPerSecond * kalan) < 0.01f && gitti && boştaBekleme == 0f, tahliye);
            failed += Check(ref total, $"⚠️ istismar: TAHLİYE kalan kirayı peşin ödetiyor ({fesih:0.00} = {kalan:0} × {_t.kiraRentPerSecond}) — \"kur → hemen tahliye\" bedava değil",
                            Mathf.Abs(fesih - kalan * _t.kiraRentPerSecond) < 0.001f, fesih);

            // Yükseltmeler: UCUZ KİRA · GENİŞ ARAZİ · UZUN KONTRAT
            (float kira, bool uzaktaki, float süre) Kule(SilahYükseltme? y)
            {
                YeniKoşu();
                if (y.HasValue) Yükselt(y.Value);
                Silahla(Kadro.KİRA);
                var k = _pc.Kule;
                var uzak = Düşman(İleri(kurma + _t.kiraRadius * 1.2f));
                float s = k.Kalan;
                float önce = uzak.TimeLeft;
                float r = Harcanan(() => k.Step(1f));
                return (r, uzak.TimeLeft < önce, s);
            }
            var düz = Kule(null);
            var ucuz = Kule(SilahYükseltme.UcuzKira);
            var geniş = Kule(SilahYükseltme.GenişArazi);
            var uzun = Kule(SilahYükseltme.UzunKontrat);
            failed += Check(ref total, $"UCUZ KİRA (+) kira ×0.6 ({düz.kira:0.00} → {ucuz.kira:0.00})", Mathf.Abs(ucuz.kira - düz.kira * 0.6f) < 0.001f, ucuz.kira);
            failed += Check(ref total, $"GENİŞ ARAZİ (+) menzil ×1.4 (menzilin 1.2 katındakine: {(düz.uzaktaki ? "VURDU" : "yok")} → {(geniş.uzaktaki ? "vurdu" : "YOK")})", !düz.uzaktaki && geniş.uzaktaki, 0f);
            failed += Check(ref total, $"UZUN KONTRAT (+) ömür ×1.5 ({düz.süre:0} → {uzun.süre:0} sn)", Mathf.Abs(uzun.süre - düz.süre * 1.5f) < 0.001f, uzun.süre);

            YeniKoşu();
            Silahla(Kadro.KİRA);
            kule = _pc.Kule;
            kule.Step(_t.kiraSeconds + 0.1f);
            failed += Check(ref total, $"kule {_t.kiraSeconds:0} sn sonra kalkıyor", kule.Bitti && _pc.Kule == null, 0f);
            return failed;
        }

        // ── 6) COMBO: AÇIĞA SATIŞ · VADE FARKI · ORTAK GİDER ─────────────────────────────────
        static int Comboları(ref int total)
        {
            int failed = 0;

            // AÇIĞA SATIŞ: REHİN'le vur → ARBİTRAJ önden de arkadan; KALDIRAÇ (+) ×1.5 daha
            float Açığa(bool combo, bool artı)
            {
                YeniKoşu();
                if (artı) Artı(ComboTür.AçığaSatış);
                if (combo)
                {
                    Düşman(İleri(1.5f)); Silahla(Kadro.REHİN);            // yakala
                    Düşman(İleri(5f)); Silahla(Kadro.REHİN);              // fırlat: adım
                    HepsiniÖldür();
                    _pc.TickCombo(0.2f);
                }
                var e = Düşman(İleri(1.2f), Vector3.back);
                return Hasar(e, () => Silahla(Kadro.ARBİTRAJ));
            }
            float a0 = Açığa(false, false), a1 = Açığa(true, false);
            int sayı = _pc.ComboSayısı;
            float a2 = Açığa(true, true);
            failed += Check(ref total, $"⚠️⚠️ AÇIĞA SATIŞ: REHİN → ARBİTRAJ önden de arkadan ({a0:0.0} → {a1:0.0}); KALDIRAÇ (+) ×1.5 ({a2:0.0})",
                            sayı == 1 && Mathf.Abs(a1 / a0 - _t.arbitrajBackMul / _t.arbitrajFrontMul) < 0.01f && Mathf.Abs(a2 / a1 - 1.5f) < 0.01f, a1 / Mathf.Max(0.001f, a0));

            // VADE FARKI: TAHVİL'le vur → FAİZLİ kademe çarpanıyla; KUPON (+) savurma bedeli geri
            (float hasar, float saat) Vade(bool combo, bool artı)
            {
                YeniKoşu();
                if (artı) Artı(ComboTür.VadeFarkı);
                var e = Düşman(İleri(1.4f));
                if (combo) { W(Kadro.TAHVİL); Bas(); _pc.Vadelen(1.1f); Vur.Invoke(_pc, null); _pc.TickCombo(0.2f); }
                // Ham saat farkı: KUPON'un iadesi `Clock.Add`'den geçiyor, "hasat" sayılırdı.
                float s0 = _clock.Seconds;
                float h = Hasar(e, () => Silahla(Kadro.FAİZLİ));
                return (h, _clock.Seconds - s0);
            }
            var v0 = Vade(false, false); var v1 = Vade(true, false);
            sayı = _pc.ComboSayısı;
            var v2 = Vade(true, true);
            failed += Check(ref total, $"⚠️⚠️ VADE FARKI: TAHVİL (2. kademe) → FAİZLİ ×2.8 ({v0.hasar:0.0} → {v1.hasar:0.0}); KUPON (+) bedel geri ({v1.saat:+0.00;-0.00} → {v2.saat:+0.00;-0.00})",
                            sayı == 1 && Mathf.Abs(v1.hasar / v0.hasar - 2.8f) < 0.01f && Mathf.Abs(v2.saat - v1.saat - _t.faizliSwingCost) < 0.001f, v1.hasar / Mathf.Max(0.001f, v0.hasar));

            // ORTAK GİDER: MÜŞTEREK'le bağla → KİRA: kulenin kirasını bağlılar öder; AİDAT (+) önce onlara ×1.5
            (float sen, float onlar, float bağlıyaAtış) Ortak(bool combo, bool artı)
            {
                YeniKoşu();
                if (artı) Artı(ComboTür.OrtakGider);
                var (a, b) = Bağla();
                float kurma = Weapon.All[Kadro.KİRA].Range(_t);
                a.transform.position = İleri(kurma + 3f);               // ikisi de kulenin menzilinde
                b.transform.position = İleri(kurma + 3.5f);
                var yabancı = Düşman(İleri(kurma + 1f));                // kuleye daha yakın, bağsız
                if (!combo) _pc.TickCombo(5f);
                else _pc.TickCombo(0.2f);
                Silahla(Kadro.KİRA);
                var k = _pc.Kule;
                float aÖnce = a.TimeLeft + b.TimeLeft;
                float sen = Harcanan(() => k.Step(1f));
                float onlar = aÖnce - (a.TimeLeft + b.TimeLeft);
                float önceA = a.TimeLeft;
                k.Step(_t.kiraShotInterval);
                return (sen, onlar, önceA - a.TimeLeft);
            }
            var o0 = Ortak(false, false); var o1 = Ortak(true, false);
            sayı = _pc.ComboSayısı;
            var o2 = Ortak(true, true);
            failed += Check(ref total, $"⚠️⚠️ ORTAK GİDER: MÜŞTEREK → KİRA: kirayı sen {o0.sen:0.00} → {o1.sen:0.00}, bağlılar {o1.onlar:0.00} ödüyor",
                            sayı == 1 && Mathf.Abs(o0.sen - _t.kiraRentPerSecond) < 0.001f && o1.sen == 0f && o1.onlar > 0f, o1.sen);
            failed += Check(ref total, $"⚠️ AİDAT (+): kule bağsız yakındakini değil önce bağlıya ateş ediyor, ×1.5 ({o1.bağlıyaAtış:0.0} → {o2.bağlıyaAtış:0.0})",
                            o1.bağlıyaAtış < 0.5f && o2.bağlıyaAtış > 0f, o2.bağlıyaAtış);
            return failed;
        }

        // ── 7) VERİ ───────────────────────────────────────────────────────────
        static int Verisi(ref int total)
        {
            int failed = 0;
            int[] yeniler = { Kadro.TAHVİL, Kadro.MÜŞTEREK, Kadro.ARBİTRAJ, Kadro.KİRA };
            int eksik = yeniler.Count(w => string.IsNullOrEmpty(Weapon.All[w].Special(_t).NeYapar) || Weapon.All[w].DashAtakAdı == null);
            int kategori = Weapon.All.Select(w => w.Category(_t)).Distinct().Count();
            failed += Check(ref total, $"yeni silahların sağ tığı ve dash-atağı HUD'da ({eksik} eksik); 12 silahın kategorisi ({kategori} ayrı)",
                            eksik == 0 && Weapon.All.Length == 12, eksik);
            failed += Check(ref total, $"yükseltme tablosu tam: {SilahSunağı.Toplam} yükseltme (ad {SilahSunağı.Ad.Length}, açıklama {SilahSunağı.NeYapar.Length}), 12 silahın her birinde en az 3; {Combo.Sayı} combo'nun hepsinin \"+\" hâli var",
                            Enumerable.Range(0, Weapon.All.Length).All(w => SilahSunağı.Yükseltmeleri(w).Count >= 3)
                            && SilahSunağı.Ad.Length == SilahSunağı.Toplam && SilahSunağı.NeYapar.Length == SilahSunağı.Toplam
                            && Combo.Tümü.All(c => !string.IsNullOrEmpty(c.ArtıAd)) && System.Enum.GetValues(typeof(ComboTür)).Length == Combo.Sayı, 0f);
            return failed;
        }

        // ── yardımcılar ──────────────────────────────────────────────────────────
        static System.Collections.Generic.List<Altar> Sunaklar(Altar.Kind k) => Altar.All.Where(a => a != null && a.Type == k).ToList();

        static void HepsiniÖldür()
        {
            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
        }

        static void Temizle()
        {
            HepsiniÖldür();
            foreach (var u in CekUcusu.Uçanlar.ToArray()) if (u != null) Object.DestroyImmediate(u.gameObject);
            CekUcusu.Uçanlar.Clear();
            foreach (var b in MusterekBag.Tümü.ToArray()) if (b != null) b.Bitir();
            MusterekBag.Tümü.Clear();
            foreach (var k in KiraKulesi.Tümü.ToArray()) if (k != null) k.Bitir();
            KiraKulesi.Tümü.Clear();
            foreach (var m in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include)) Object.DestroyImmediate(m.gameObject);
            foreach (var g in Object.FindObjectsByType<GizliSilah>(FindObjectsInactive.Include)) Object.DestroyImmediate(g.gameObject);
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
