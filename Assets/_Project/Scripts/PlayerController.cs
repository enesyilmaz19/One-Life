using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TekCan
{
    /// <summary>
    /// Hareket + dash (i-frame) + kısa, dash ile iptal edilebilen saldırı.
    /// Blok YOK — blok köşede beklemeyi ödüllendirir, saat ekonomisini öldürür.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        Tuning _t;
        Transform _cam;
        Camera _camera;

        Vector3 _dashDir;
        float _dashTimer, _dashCd, _iFrames;
        float _attackTimer;
        bool _attackHitDone;

        Renderer _renderer;
        Color _baseColor;
        float _hurtFlash;
        Vector3 _knockback;

        // ⚠️ HER SİLAHIN AYRI bekleme süresi. Tek `float` idi ve Enes yakaladı:
        // "her bir silahın ayrı cd si olacak, ortak olmuş sanırım." Ortak sayaç
        // kombo oynanışını doğrudan öldürüyordu — bir silahın özelini kullanmak
        // ötekilerinkini de kilitliyordu. Ve HACİZ'in özelinin "gözükmemesi" de
        // buydu: başka silahın bekleme süresi onu yutuyordu.
        float[] _specialCd = new float[TekCan.Weapon.All.Length];
        // Son özelin ödenen bedeli ve öldürme iadesi hakkı (İCRA, Faz 29) — vuruş anında okunuyor.
        float _özelBedeli;
        bool _özelİadeVar = true;

        // ── SİLAH YÜKSELTMELERİ (Faz 29, ana boss sonrası SİLAH SUNAĞI) ──────────
        // Yalnızca KUŞANILAN silahınki geçerli. Silahın verisi (`Weapon`) saf kalıyor; yükseltme
        // değerin KULLANILDIĞI yerde uygulanıyor — savurma yayı da isabet de aynı değerden okuyor.
        bool Y(SilahYükseltme y) => SilahSunağı.Silahı(y) == _weaponIndex && SilahSunağı.Var(y);
        /// <summary>Savurma/dash-atak menzili. KİRA'nın `Range`'i kule KURMA menzili (İPOTEK gibi) — savurması normal.</summary>
        float TemelMenzil => Weapon.Saldırı == Biçim.Kira ? _t.attackRange : Weapon.Range(_t);
        float SavurmaMenzili => TemelMenzil * (Y(SilahYükseltme.UzunKol) ? 1.25f : 1f)
                                                 * (Y(SilahYükseltme.GenişKol) ? 1.5f : 1f);      // REHİN (Faz 31)
        float SavurmaSüresi => Weapon.Duration(_t) * (Y(SilahYükseltme.GenişPencere) ? 1.6f : 1f);  // KARŞILIK
        float SavurmaAçısı => Weapon.Angle(_t) * (Y(SilahYükseltme.GenişAğ) ? 1.35f : 1f);
        float ÖlüBölge => Weapon.MinRange(_t) * (Y(SilahYükseltme.DarÖlüBölge) ? 0.5f : 1f);
        int SavurmaHedefi => Y(SilahYükseltme.Delici) ? 2 : Weapon.MaxTargets;
        static readonly List<(Enemy e, float d)> _hedefler = new List<(Enemy e, float d)>();

        /// <summary>Ne kadar dipte (0 güvende, 1 panikte) — İFLAS'ın eğrisi; DİP eşikleri ×1.5 öteliyor.</summary>
        float DipOranı()
        {
            float saniye = Clock.Instance != null ? Clock.Instance.Seconds : _t.startSeconds;
            if (Y(SilahYükseltme.Dip)) saniye /= 1.5f;
            return Mathf.InverseLerp(_t.iflasSafeSeconds, _t.iflasPanicSeconds, saniye);
        }

        /// <summary>Sağ tık beklemesi: KISA SÜPÜRME / KISA İCRA ×0.7 · SABIR sunağı (Faz 29).</summary>
        float ÖzelBeklemeÇarpanı()
        {
            float k = Y(SilahYükseltme.KısaSüpürme) || Y(SilahYükseltme.KısaİCRA) ? 0.7f : 1f;
            return Clock.Instance != null ? k * Clock.Instance.SabırÇarpanı : k;
        }

        // ── COMBO (Faz 30, Enes: "hızlı silah geçişleri ile combo… abuselanmamalı… gelişip yeni etki") ──
        // Zincir: İSABET eden son adımlar (silah, an, yer). İPOTEK'in koyduğu işaret de bir adım.
        // Zaman `_comboAn` — Update ilerletiyor, öz-denetim `TickCombo` ile (editörde deltaTime 0).
        readonly List<(int silah, float an, Vector3 yer)> _zincir = new List<(int silah, float an, Vector3 yer)>();
        readonly float[] _comboBekleme = new float[Combo.Sayı];
        float _comboAn;
        Vector3 _isabetToplam, _isabetYeri;

        /// <summary>Tetiklenen combo sayısı — öz-denetim ve otopilot sayıyor.</summary>
        public int ComboSayısı { get; private set; }

        public void TickCombo(float dt)
        {
            _comboAn += dt;
            for (int i = 0; i < _comboBekleme.Length; i++) _comboBekleme[i] -= dt;
        }


        /// <summary>Bir adım yaz: aynı silah üst üste sayılmaz (ucu tazelenir), pencere geçtiyse zincir kopar.</summary>
        void ZincireYaz(int silah, Vector3 yer)
        {
            if (_zincir.Count > 0)
            {
                var son = _zincir[_zincir.Count - 1];
                if (_comboAn - son.an > _t.comboWindow) _zincir.Clear();
                else if (son.silah == silah) { _zincir[_zincir.Count - 1] = (silah, _comboAn, yer); return; }
            }
            _zincir.Add((silah, _comboAn, yer));
            if (_zincir.Count > 3) _zincir.RemoveAt(0);
        }

        /// <summary>Şimdiki silahın adımı bu combo'yu tamamlıyor mu: sıra, her adımda pencere, bekleme, açıklık.</summary>
        bool ComboHazır(ComboTür c)
        {
            var sıra = Combo.Bu(c).Sıra();
            int n = sıra.Length;
            if (_comboBekleme[(int)c] > 0f || sıra[n - 1] != _weaponIndex) return false;
            if (!Combo.Kullanılır(c, RoomManager.AktifBölge) || _zincir.Count < n - 1) return false;
            int baş = _zincir.Count - (n - 1);
            for (int i = 0; i < n - 1; i++)
            {
                var adım = _zincir[baş + i];
                if (adım.silah != sıra[i]) return false;
                float sonraki = i + 1 < n - 1 ? _zincir[baş + i + 1].an : _comboAn;
                if (sonraki - adım.an > _t.comboWindow) return false;
            }
            return true;
        }

        /// <summary>Zincirde kaç adım geride (1 = son adım) nerede vurulmuştu — combo'nun hedefi.</summary>
        Vector3 AdımYeri(int geri) => _zincir[_zincir.Count - geri].yer;

        void ComboTetikle(ComboTür c)
        {
            _comboBekleme[(int)c] = _t.comboCooldown;
            _zincir.Clear();
            ComboSayısı++;
            Combo.Duyur($"COMBO: {Combo.Bu(c).Ad}" + (Combo.Artı(c) ? $" + {Combo.Bu(c).ArtıAd}" : ""), 1.5f);
        }

        /// <summary>İsabetten sonra (normal, özel, dash-atak): HACİZ KESİNLEŞTİ'yi dene, yoksa zincire yaz.</summary>
        void İsabetSonrası(int hits)
        {
            if (hits <= 0) return;
            if (ComboHazır(ComboTür.HacizKesinleşti))
            {
                var işaret = GroundMark.İçindekiİpotek(_isabetYeri);
                if (işaret != null)
                {
                    ComboTetikle(ComboTür.HacizKesinleşti);
                    işaret.HemenPatlat(1.5f);
                    if (Combo.Artı(ComboTür.HacizKesinleşti)) _specialCd[_weaponIndex] = 0f;   // KESİN HÜKÜM
                    return;
                }
            }
            ZincireYaz(_weaponIndex, _isabetYeri);
        }

        Enemy EnUzakDüşman(float menzil)
        {
            Enemy en = null; float enUzak = -1f;
            foreach (var e in Enemy.All)
            {
                if (e == null) continue;
                var d = e.transform.position - transform.position; d.y = 0f;
                if (d.magnitude <= menzil && d.magnitude > enUzak) { enUzak = d.magnitude; en = e; }
            }
            return en;
        }

        // ── FAZ 31: REHİN · ÇEK · KARŞILIK ───────────────────────────────────
        // Davranış silahın BİÇİMİNDEN (`Weapon.Saldırı`), adından değil. İsabet yine tek yerden
        // (`InHitCone` / `StrikeCone`); yalnızca koni olmayan vuruşlar (fırlatma, takas, çek) kendi
        // hedefini seçiyor.
        static readonly ComboTür[] Bitirenler = { ComboTür.Temerrüt, ComboTür.KarşıİCRA, ComboTür.SenetHaczi, ComboTür.SilipSüpür,
                                                  ComboTür.AçığaSatış, ComboTür.VadeFarkı };
        Enemy _rehine, _sonVurulan;
        float _biriken, _itirazKalan;
        // Silah değişimi saldırıyı iptal ediyor; KARŞILIK'ta bu toparlanmayı da silerdi ("tıkla → 2 → 1 →
        // tıkla" ile pencere hiç kapanmazdı). Bekleme silah değişiminden bağımsız sayıyor.
        float _karşılamaBekleme;

        /// <summary>Tutulan düşman (yoksa null). Öldüyse `Enemy.All`'dan çıkmış — Destroy ertelense de (tuzak 1).</summary>
        public Enemy Rehine => Tutuyor ? _rehine : null;
        bool Tutuyor => _rehine != null && Enemy.All.Contains(_rehine);
        public Vector3 RehinYeri => transform.position + transform.forward * (0.7f + (_rehine != null ? _rehine.Radius : 0.5f));

        /// <summary>KARŞILIK: karşılanıp biriken bedel (sn) — sonraki vuruşa hasar olarak iner.</summary>
        public float Biriken => _biriken;
        public int KarşılananSayısı { get; private set; }
        /// <summary>REHİN kalkanının yediği vuruş sayısı — otopilot raporu.</summary>
        public int KalkanSayısı { get; private set; }

        /// <summary>KARŞILIK: savurmanın vuruşa kadarki kısmı ya da İTİRAZ boyunca gelen darbe saatten düşmüyor.</summary>
        bool Karşılıyor => Weapon.Saldırı == Biçim.Karşılık && (_itirazKalan > 0f || (Attacking && !_attackHitDone));

        void RehineyiBırak()
        {
            if (_rehine != null) _rehine.Bırak();
            _rehine = null;
        }

        void VuruşHissi(float k)
        {
            Sfx.Hit();
            HitStop.Freeze(_t.hitStopDuration * k);
            IsoCamera.Shake(_t.hitShake * k);
        }

        /// <summary>REHİN · YAKALA: konideki en yakın (boss değil) düşman. Boştaysa yakalanmaz, ölür.</summary>
        bool Yakala(float menzil, float açı)
        {
            Enemy en = null; float enYakın = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || e.IsBoss || e.Tutan != null) continue;
                if (!InHitCone(e, menzil, açı * 0.5f, out float d)) continue;
                if (d < enYakın) { enYakın = d; en = e; }
            }
            if (en == null) return false;
            bool boştu = en.Hollow;
            en.TakeDamage(WeaponDamage() * 0.5f, Weapon.HarvestMultiplier);
            VuruşHissi(0.6f);
            if (!boştu && Enemy.All.Contains(en)) { _rehine = en; en.RehinAl(this); }
            return true;
        }

        void RehinVuruşu()
        {
            if (!Tutuyor) { Yakala(SavurmaMenzili, SavurmaAçısı); return; }   // yakalamak combo adımı DEĞİL

            // FIRLAT: yön boyunca rehinenin genişliğinde bir hat; ilk çarptığına sert vurur, rehine de yer.
            var r = _rehine;
            RehineyiBırak();
            Vector3 ileri = transform.forward;
            float menzil = _t.rehinThrowRange;
            Enemy hedef = null; float enYakın = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || e == r || e.Dormant) continue;
                Vector3 to = e.transform.position - transform.position; to.y = 0f;
                float boy = Vector3.Dot(to, ileri);
                if (boy <= 0f || boy - e.Radius > menzil) continue;
                if ((to - ileri * boy).magnitude > e.Radius + r.Radius) continue;
                if (boy < enYakın) { enYakın = boy; hedef = e; }
            }
            float hasar = WeaponDamage() * _t.rehinThrowDamageMul * (Y(SilahYükseltme.AğırRehin) ? 1.6f : 1f);
            Vector3 varış = hedef != null
                ? hedef.transform.position - ileri * (hedef.Radius + r.Radius)
                : transform.position + ileri * menzil;
            varış.y = r.transform.position.y;
            r.transform.position = varış;                 // ponytail: anlık uçuş; animasyon VFX fazında
            Arc.Spawn(transform.position, transform.rotation, (varış - transform.position).magnitude, 8f, Weapon.Tint, _t.swingArcLife);
            Arc.Spawn(varış, transform.rotation, 1.2f, 360f, Weapon.Tint, _t.swingArcLife, scaleFrom: 0.5f, scaleTo: 1.3f);

            int hits = 1;
            if (hedef != null)
            {
                hedef.Push(ileri, 6f);
                hedef.TakeDamage(hasar, Weapon.HarvestMultiplier);
                hits++;
            }
            r.Push(ileri, 4f);
            r.TakeDamage(hasar * 0.5f, Weapon.HarvestMultiplier);
            _isabetYeri = varış;
            VuruşHissi(1.4f);
            İsabetSonrası(hits);
        }

        /// <summary>REHİN · TAKAS: tuttuğunu en yakın ikinci düşmanla yer değiştirt, ikisine vur; yeni rehine o.</summary>
        void Takas(WeaponSpecial sp)
        {
            if (!Tutuyor) { Yakala(SavurmaMenzili * 2f, SavurmaAçısı); return; }
            var eski = _rehine;
            Enemy yeni = null; float enYakın = _t.rehinThrowRange;
            foreach (var e in Enemy.All)
            {
                if (e == null || e == eski || e.IsBoss || e.Dormant) continue;
                Vector3 d = e.transform.position - transform.position; d.y = 0f;
                if (d.magnitude < enYakın) { enYakın = d.magnitude; yeni = e; }
            }
            if (yeni == null) return;
            Vector3 yer = yeni.transform.position;
            RehineyiBırak();
            eski.transform.position = new Vector3(yer.x, eski.transform.position.y, yer.z);
            float hasar = WeaponDamage() * sp.DamageMul;
            bool yeniBoştu = yeni.Hollow;
            eski.TakeDamage(hasar, Weapon.HarvestMultiplier);
            yeni.TakeDamage(hasar, Weapon.HarvestMultiplier);
            if (!yeniBoştu && Enemy.All.Contains(yeni)) { _rehine = yeni; yeni.RehinAl(this); }
            Arc.Spawn(yer, transform.rotation, 1.2f, 360f, sp.Tint, _t.swingArcLife * 1.4f, scaleFrom: 0.4f, scaleTo: 1.3f);
            SwingsSpawned++;
            _isabetYeri = yer;
            VuruşHissi(1.2f);
            İsabetSonrası(2);
        }

        /// <summary>REHİN · SÜRÜKLEME (dash-atak): tutuyorsan yere çarp (çevresine de), tutmuyorsan yakala.</summary>
        void Sürükle()
        {
            Sfx.Swing();
            if (!Tutuyor) { Yakala(Weapon.Range(_t) * _t.dashAttackRangeMult, Weapon.DashAngle(_t)); return; }
            var r = _rehine;
            RehineyiBırak();
            Vector3 yer = r.transform.position;
            float alan = 2f;
            _hedefler.Clear();
            foreach (var e in Enemy.All)
            {
                if (e == null || e == r || e.Dormant) continue;
                Vector3 d = e.transform.position - yer; d.y = 0f;
                if (d.magnitude - e.Radius <= alan) _hedefler.Add((e, d.magnitude));
            }
            int hits = 1;
            foreach (var (e, _) in _hedefler)
            {
                e.Push(e.transform.position - yer, 6f);
                e.TakeDamage(WeaponDamage(), Weapon.HarvestMultiplier);
                hits++;
            }
            r.TakeDamage(WeaponDamage() * 2f * (Y(SilahYükseltme.AğırRehin) ? 1.6f : 1f), Weapon.HarvestMultiplier);
            Arc.Spawn(yer, transform.rotation, alan, 360f, Weapon.Tint, _t.swingArcLife * 1.3f, scaleFrom: 0.4f, scaleTo: 1.2f);
            SwingsSpawned++;
            _isabetYeri = yer;
            VuruşHissi(1.5f);
            İsabetSonrası(hits);
        }

        /// <summary>ÇEK'i fırlat. Yükseltmeler fırlatma anında çekin içine yazılıyor (kuşanılan ÇEK).</summary>
        void ÇekFırlat(Vector3 yön, float hasarÇarpanı)
        {
            var oda = RoomManager.Instance != null ? RoomManager.Instance.RoomRoot : null;
            CekUcusu.Fırlat(this, _t, yön, Weapon.Range(_t), _t.cekSpeed * (Y(SilahYükseltme.HızlıTahsil) ? 1.4f : 1f),
                            WeaponDamage() * hasarÇarpanı, Weapon.HarvestMultiplier,
                            _t.cekBounces + (Y(SilahYükseltme.ÇiftCiro) ? 1 : 0),
                            Y(SilahYükseltme.DönüşKesintisi) ? 1.5f : 1f,
                            oda != null ? oda : transform.parent);
            SwingsSpawned++;
        }

        /// <summary>Çek bir düşmana değdi — silah o an kuşanılan olmasa da adım ÇEK'in (SENET HACZİ).</summary>
        public void Çekİsabeti(Vector3 yer)
        {
            _isabetYeri = yer;
            VuruşHissi(0.5f);
            ZincireYaz(Kadro.ÇEK, yer);
        }

        /// <summary>ÇEK · CİRO (dash-atak): havadaki çeki yakala, dash yönüne yeniden fırlat.</summary>
        void Ciro()
        {
            CekUcusu.Yakala(this);
            ÇekFırlat(_dashDir, 1.3f);
            Sfx.Swing();
        }

        /// <summary>KARŞILIK'ın hasarı: biriken yoksa zayıf, varsa biriken × `karsilikDamagePerSecond` eklenir.</summary>
        float KarşılıkHasarı() => WeaponDamage() * (_biriken > 0f ? 1f : 0.6f) + _biriken * _t.karsilikDamagePerSecond;

        /// <summary>KARŞILIK · İTİRAZ duruşu. ⚠️ dt DIŞARIDAN. Bitince biriken hepsi önündekilere (geniş koni).</summary>
        public void Tickİtiraz(float dt)
        {
            if (_itirazKalan <= 0f) return;
            _itirazKalan -= dt;
            if (_itirazKalan > 0f) return;
            _itirazKalan = 0f;
            var sp = Weapon.Special(_t);
            float menzil = SavurmaMenzili * (sp != null ? sp.RangeMul : 1f);
            Arc.Spawn(transform.position, transform.rotation, menzil, 120f, Weapon.Tint, _t.swingArcLife * 1.6f, scaleFrom: 0.5f, scaleTo: 1f);
            SwingsSpawned++;
            StrikeCone(menzil, 120f, KarşılıkHasarı(), _t.hitStopDuration * 1.6f, _t.hitShake * 1.8f,
                       int.MaxValue, Weapon.HarvestMultiplier, out int hits, out _);
            if (hits > 0) _biriken = 0f;
            İsabetSonrası(hits);
        }

        /// <summary>SENET ZİNCİRİ (SENET HACZİ +): HACİZ'in vuruşu hedefin yanındakine de seker.</summary>
        void SenetZinciri(float hasar, float hasat)
        {
            Enemy yan = null; float enYakın = 4f;
            foreach (var e in Enemy.All)
            {
                if (e == null || e == _sonVurulan || e.Dormant) continue;
                Vector3 d = e.transform.position - _isabetYeri; d.y = 0f;
                if (d.magnitude < enYakın) { enYakın = d.magnitude; yan = e; }
            }
            if (yan == null) return;
            Arc.Spawn(yan.transform.position, transform.rotation, 0.9f, 360f, Weapon.Tint, _t.swingArcLife, scaleFrom: 0.4f, scaleTo: 1.2f);
            yan.TakeDamage(hasar, hasat);
        }

        // ── FAZ 32: TAHVİL · MÜŞTEREK · ARBİTRAJ · KİRA ──────────────────────
        bool _vadeAçık;
        float _vade, _bozdurma, _sonVadeÇarpanı = 1f, _görünmezKalan;
        bool _zorlaArka;
        Enemy _bekleyen;
        MusterekBag _sonBağ;
        KiraKulesi _kule;
        Vector3 _dashBaşı;

        /// <summary>TAHVİL: basılı tutuluyorsa olgunluk kademesi (0–3), değilse −1. HUD ve otopilot.</summary>
        public int VadeKademesi => _vadeAçık ? Kademe(_vade) : -1;
        /// <summary>ARBİTRAJ · AÇIK POZİSYON: düşmanlar yeni saldırı başlatmıyor (`Enemy`).</summary>
        public bool Görünmez => _görünmezKalan > 0f;
        /// <summary>ARBİTRAJ'ın arkadan sayılan isabetleri — otopilot raporu.</summary>
        public int ArkadanVuruş { get; private set; }
        public KiraKulesi Kule => _kule != null && !_kule.Bitti ? _kule : null;
        public MusterekBag SonBağ => _sonBağ != null && !_sonBağ.Bitti ? _sonBağ : null;
        Transform OdaKökü => RoomManager.Instance != null && RoomManager.Instance.RoomRoot != null
                             ? RoomManager.Instance.RoomRoot : transform.parent;

        static bool AttackHeld()
        {
            var m = Mouse.current;
            if (m != null && m.leftButton.isPressed) return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonWest.isPressed;
        }

        // TAHVİL
        float KademeSüresi => _t.tahvilTierSeconds * (Y(SilahYükseltme.KısaVade) ? 0.7f : 1f);
        int Kademe(float vade) => Mathf.Min(3, Mathf.FloorToInt(vade / KademeSüresi));
        float VadeÇarpanı(int k) => Tahvil.KademeÇarpanı(k) * (k == 3 && Y(SilahYükseltme.YüksekFaiz) ? 1.4f : 1f);

        /// <summary>
        /// TAHVİL: basılı tutulan bir an. ⚠️ dt DIŞARIDAN. Beklerken saat iki kat akar: normal akışın
        /// üstüne bir kat daha, akışın TEK kaynağından (`Clock.AkışHızı`). İSKONTO yarım kat. Sunak
        /// odasında (saat donmuşken) bedel yok — orada akış da yok.
        /// </summary>
        public void Vadelen(float dt)
        {
            if (!_vadeAçık) return;
            int önce = Kademe(_vade);
            _vade += dt;
            var saat = Clock.Instance;
            if (saat != null && !saat.Frozen) saat.Spend(saat.AkışHızı * dt * (Y(SilahYükseltme.İskonto) ? 0.5f : 1f));
            int sonra = Kademe(_vade);
            if (sonra > önce)                                       // kademe atladı: görünsün
                Arc.Spawn(transform.position, transform.rotation, 0.6f + 0.35f * sonra, 360f, Weapon.Tint, 0.25f,
                          scaleFrom: 0.6f, scaleTo: 1.1f);
        }

        /// <summary>TAHVİL'i boz (sol tık bırakılınca): olgunluk kadar sert koni, 3. kademede uzun.</summary>
        void TahvilBoz()
        {
            int k = Kademe(_vade);
            _vadeAçık = false; _vade = 0f;
            float ç = VadeÇarpanı(k);
            _sonVadeÇarpanı = ç;
            float menzil = SavurmaMenzili * (k >= 3 ? 1.3f : 1f);
            Arc.Spawn(transform.position, transform.rotation, menzil, SavurmaAçısı, Weapon.Tint, _t.swingArcLife * (1f + 0.3f * k));
            SwingsSpawned++;
            Sfx.Swing();
            _attackTimer = SavurmaSüresi * 0.5f;                    // kısa toparlanma
            _attackHitDone = true;
            StrikeCone(menzil, SavurmaAçısı, WeaponDamage() * ç,
                       _t.hitStopDuration * (1f + 0.3f * k), _t.hitShake * (1f + 0.3f * k),
                       SavurmaHedefi, Weapon.HarvestMultiplier, out int hits, out int kills);
            Weapon.OnStrike(Clock.Instance, _t, hits, kills);
            İsabetSonrası(hits);
        }

        /// <summary>TAHVİL · VADESİ GELMİŞ: o anki olgunluk (en az 1. kademe) önündeki üç düşmana bölüşülür.</summary>
        void VadesiGelmiş(WeaponSpecial sp)
        {
            int k = Mathf.Max(1, _vadeAçık ? Kademe(_vade) : 1);
            _vadeAçık = false; _vade = 0f;
            float ç = VadeÇarpanı(k);
            _sonVadeÇarpanı = ç;
            float menzil = Weapon.Range(_t) * sp.RangeMul;
            Arc.Spawn(transform.position, transform.rotation, menzil, sp.Angle, sp.Tint, _t.swingArcLife * 1.4f);
            SwingsSpawned++;
            StrikeCone(menzil, sp.Angle, WeaponDamage() * ç * 0.5f,           // üçe bölünmüş ×1.5
                       _t.hitStopDuration, _t.hitShake, sp.MaxTargets, Weapon.HarvestMultiplier,
                       out int hits, out _, special: sp);
            İsabetSonrası(hits);
        }

        /// <summary>
        /// TAHVİL · ERKEN BOZDURMA (dash-atak): dash'e basılı tutarak girdiysen olgunluğun YARISI dash-atağın
        /// üstüne biner, geniş. ⚠️ Düz dash-ataktan aşağı düşmez: ilk hâlinde (silah hasarı × yarı olgunluk)
        /// 2. kademede düz dash-ataktan zayıftı — beklemek cezalanıyordu (Faz 32 kapısı gösterdi).
        /// </summary>
        void Bozdur()
        {
            float ç = Weapon.DashDamageMultiplier(_t) * Mathf.Max(1f, _bozdurma * 0.5f);
            _bozdurma = 0f;
            float menzil = TemelMenzil * _t.dashAttackRangeMult * 1.2f;
            Arc.Spawn(transform.position, transform.rotation, menzil, 160f, Weapon.Tint, _t.swingArcLife * 1.3f);
            SwingsSpawned++;
            Sfx.Swing();
            StrikeCone(menzil, 160f, WeaponDamage() * ç, _t.hitStopDuration * 1.4f, _t.hitShake * 1.5f,
                       int.MaxValue, Weapon.HarvestMultiplier, out int hits, out int kills);
            Weapon.OnStrike(Clock.Instance, _t, hits, kills);
            İsabetSonrası(hits);
        }

        // MÜŞTEREK
        float BağPayı => _t.musterekShare * (Y(SilahYükseltme.SıkıBağ) ? 1.5f : 1f);
        float BağSüresi => _t.musterekSeconds * (Y(SilahYükseltme.UzunBağ) ? 2f : 1f);
        float BağHasadı => Y(SilahYükseltme.OrtakHasat) ? 1f : 0.5f;

        /// <summary>
        /// MÜŞTEREK'in vuruşu: ilk vurulan bekler, sonraki (başka) vurulan ona bağlanır. ⚠️ İşaret halkasıyla
        /// AYNI ömürde: halka 2.5 sn'de sönüp işaret kalsaydı oyuncu "yeniden başladı" sanır, sonraki vuruş
        /// eski işaretliyi bağlardı — görsel yalan.
        /// </summary>
        const float İşaretÖmrü = 2.5f;
        float _bekleyenAn;

        void Bağla(Enemy e)
        {
            if (e == null || !Enemy.All.Contains(e)) return;
            if (_bekleyen != null && _comboAn - _bekleyenAn > İşaretÖmrü) _bekleyen = null;   // halka söndü
            if (_bekleyen != null && _bekleyen != e && Enemy.All.Contains(_bekleyen)
                && !(e.Bağ != null && e.Bağ == _bekleyen.Bağ))
            {
                _sonBağ = MusterekBag.Kur(new[] { _bekleyen, e }, BağPayı, BağHasadı, BağSüresi, OdaKökü);
                _bekleyen = null;
                return;
            }
            _bekleyen = e;
            _bekleyenAn = _comboAn;
            Arc.Spawn(e.transform.position, e.transform.rotation, e.Radius * 1.8f, 360f, Weapon.Tint, İşaretÖmrü,
                      follow: e.transform, scaled: true);
        }

        /// <summary>MÜŞTEREK · ZİNCİRLEME: çevrendeki en yakın dört düşman tek zincire (önce vurulur, sonra bağlanır).</summary>
        void Zincirle(WeaponSpecial sp)
        {
            float menzil = Weapon.Range(_t) * sp.RangeMul;
            _hedefler.Clear();
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dormant) continue;
                Vector3 d = e.transform.position - transform.position; d.y = 0f;
                if (d.magnitude - e.Radius <= menzil) _hedefler.Add((e, d.magnitude));
            }
            _hedefler.Sort((a, b) => a.d.CompareTo(b.d));
            if (_hedefler.Count > sp.MaxTargets) _hedefler.RemoveRange(sp.MaxTargets, _hedefler.Count - sp.MaxTargets);
            Arc.Spawn(transform.position, transform.rotation, menzil, 360f, sp.Tint, _t.swingArcLife * 1.4f, scaleFrom: 0.3f, scaleTo: 1f);
            SwingsSpawned++;
            var üyeler = new List<Enemy>();
            int hits = 0;
            Vector3 toplam = Vector3.zero;
            foreach (var (e, _) in _hedefler)
            {
                toplam += e.transform.position;
                e.TakeDamage(WeaponDamage() * sp.DamageMul, Weapon.HarvestMultiplier);
                hits++;
                if (Enemy.All.Contains(e)) üyeler.Add(e);
            }
            if (üyeler.Count >= 2) _sonBağ = MusterekBag.Kur(üyeler, BağPayı, BağHasadı, BağSüresi, OdaKökü);
            if (hits > 0) { _isabetYeri = toplam / hits; VuruşHissi(0.8f); }
            İsabetSonrası(hits);
        }

        /// <summary>MÜŞTEREK · KOPARMA (dash-atak): dash bir bağı kestiyse bağ kopar, uçlar ortaya çekilip çarpar.</summary>
        bool Kopar()
        {
            foreach (var b in MusterekBag.Tümü.ToArray())
            {
                if (b == null || b.Bitti || !b.Kesiyor(_dashBaşı, transform.position)) continue;
                Vector3 orta = b.Orta;
                var üyeler = b.Üyeler.ToArray();
                b.Bitir();                                          // önce kopar: ×2 bir daha yayılmasın
                int hits = 0;
                foreach (var e in üyeler)
                {
                    if (e == null || !Enemy.All.Contains(e)) continue;
                    e.Push(orta - e.transform.position, 8f);
                    e.TakeDamage(WeaponDamage() * 2f, Weapon.HarvestMultiplier);
                    hits++;
                }
                Arc.Spawn(orta, transform.rotation, 1.6f, 360f, Weapon.Tint, _t.swingArcLife * 1.4f, scaleFrom: 1.4f, scaleTo: 0.4f);
                SwingsSpawned++;
                Sfx.Swing();
                _isabetYeri = orta;
                if (hits > 0) VuruşHissi(1.5f);
                İsabetSonrası(hits);
                return true;
            }
            return false;
        }

        // ARBİTRAJ
        /// <summary>
        /// ARBİTRAJ'ın konum çarpanı: düşmanın YÜZÜNE göre — arkadan sert, önden zayıf, yandan düz.
        /// Görünmezken, TERS İŞLEM'de ve AÇIĞA SATIŞ combo'sunda her yön "arkadan".
        /// </summary>
        public float KonumÇarpanı(Enemy e)
        {
            float arka = _t.arbitrajBackMul * (Y(SilahYükseltme.KeskinBıçak) ? 1.4f : 1f);
            if (_zorlaArka || Görünmez) return arka;
            Vector3 benden = transform.position - e.transform.position; benden.y = 0f;
            Vector3 yüz = e.transform.forward; yüz.y = 0f;
            float açı = Vector3.Angle(yüz, benden);
            return açı >= (Y(SilahYükseltme.GenişAçı) ? 90f : 120f) ? arka
                 : açı <= 60f ? _t.arbitrajFrontMul : 1f;
        }

        /// <summary>ARBİTRAJ · TERS İŞLEM için: dash boyunca içinden geçilen düşman (yoksa null).</summary>
        Enemy Geçilen()
        {
            Vector3 a = _dashBaşı, b = transform.position; a.y = b.y = 0f;
            Vector3 ab = b - a;
            float boy = ab.sqrMagnitude;
            if (boy < 0.0001f) return null;
            Enemy en = null; float enYakın = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dormant) continue;
                Vector3 p = e.transform.position; p.y = 0f;
                float t = Vector3.Dot(p - a, ab) / boy;
                if (t <= 0f || t > 1.05f) continue;                 // dash'in üstünde, geride değil
                float uzak = (a + ab * Mathf.Clamp01(t) - p).magnitude;
                if (uzak > e.Radius + 0.6f) continue;
                if (uzak < enYakın) { enYakın = uzak; en = e; }
            }
            return en;
        }

        /// <summary>TERS İŞLEM: içinden geçtiğin düşmana dön, arkadan vur.</summary>
        void Tersİşlem(Enemy e)
        {
            Vector3 to = e.transform.position - transform.position; to.y = 0f;
            if (to.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(to.normalized);
            float menzil = to.magnitude + e.Radius + 0.5f;
            Arc.Spawn(transform.position, transform.rotation, menzil, 70f, Weapon.Tint, _t.swingArcLife * 1.3f);
            SwingsSpawned++;
            Sfx.Swing();
            _zorlaArka = true;
            StrikeCone(menzil, 70f, WeaponDamage() * Weapon.DashDamageMultiplier(_t),
                       _t.hitStopDuration * 1.4f, _t.hitShake * 1.5f, 1, Weapon.HarvestMultiplier, out int hits, out int kills);
            _zorlaArka = false;
            Weapon.OnStrike(Clock.Instance, _t, hits, kills);
            İsabetSonrası(hits);
        }

        // KİRA
        float KuleSüresi => _t.kiraSeconds * (Y(SilahYükseltme.UzunKontrat) ? 1.5f : 1f);
        float KuleKirası => _t.kiraRentPerSecond * (Y(SilahYükseltme.UcuzKira) ? 0.6f : 1f);
        float KuleMenzili => _t.kiraRadius * (Y(SilahYükseltme.GenişArazi) ? 1.4f : 1f);

        /// <summary>KİRA: nişana kule (menzile kırpılmış). ORTAK GİDER'de kirayı son bağın üyeleri öder.</summary>
        void KuleKur()
        {
            bool ortak = SonBağ != null && ComboHazır(ComboTür.OrtakGider);
            if (ortak) ComboTetikle(ComboTür.OrtakGider);
            _kule = KiraKulesi.Kur(_t, Nişan(Weapon.Range(_t)), KuleSüresi, KuleKirası, KuleMenzili,
                                   WeaponDamage() * _t.kiraShotMul, Weapon.HarvestMultiplier,
                                   ortak ? _sonBağ : null, ortak && Combo.Artı(ComboTür.OrtakGider), OdaKökü);
            SwingsSpawned++;
            Weapon.OnStrike(Clock.Instance, _t, 0, 0);
        }

        /// <summary>KİRA · TAHLİYE: kalan kirayı peşin öde (erken fesih), kuleyi patlat — kalan süre kadar hasar.</summary>
        void Tahliye()
        {
            var k = Kule;
            if (k == null) return;
            Vector3 yer = k.transform.position;
            float yarıçap = k.Yarıçap;
            float hasar = WeaponDamage() * _t.kiraEvictionPerSecond * k.Fesih();
            _hedefler.Clear();
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dormant) continue;
                Vector3 d = e.transform.position - yer; d.y = 0f;
                if (d.magnitude - e.Radius <= yarıçap) _hedefler.Add((e, d.magnitude));
            }
            int hits = 0;
            foreach (var (e, _) in _hedefler) { e.TakeDamage(hasar, Weapon.HarvestMultiplier); hits++; }
            Arc.Spawn(yer, transform.rotation, yarıçap, 360f, Weapon.Tint, _t.swingArcLife * 1.6f, scaleFrom: 0.3f, scaleTo: 1f);
            SwingsSpawned++;
            _isabetYeri = yer;
            if (hits > 0) VuruşHissi(1.4f);
            İsabetSonrası(hits);
        }

        // ── SERİ VURUŞ (İFLAS · TASFİYE) ──────────────────────────────────────
        // ⚠️ Coroutine YOK. Bu projede zaman güdümlü her şey `dt`'yi DIŞARIDAN
        // alıyor (`Arc.Step`, `Mote.Step`, `Enemy.TickPush`) — sebebi editör
        // modunda `Time.deltaTime`in 0 olması ve etkinin ölçülememesi. Coroutine
        // yazsaydım seri vuruş TEST EDİLEMEZ olurdu.
        int _flurryLeft;
        float _flurryTimer;
        WeaponSpecial _flurrySpecial;

        TrailRenderer _dashTrail;    // yalnızca dash boyunca çizer, bkz. BuildDashTrail
        bool _dashAttackQueued;
        int _weaponIndex;

        /// <summary>Kuşanılan silah. 1-4 tuşlarıyla değişiyor (Faz 3 test kolaylığı).</summary>
        public Weapon Weapon => TekCan.Weapon.All[_weaponIndex];

        static readonly Color HurtColor = new Color(1f, 0.3f, 0.3f);

        /// <summary>Savurma yayı üretildi mi — öz-denetim bunu kontrol eder (ıskada da çıkmalı).</summary>
        public int SwingsSpawned { get; private set; }

        public bool Dashing => _dashTimer > 0f;
        public bool Attacking => _attackTimer > 0f;
        public bool Invulnerable => _iFrames > 0f;

        public void Init(Tuning tuning, Transform cameraTransform)
        {
            _t = tuning;
            _cam = cameraTransform;
            _camera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
            _renderer = GetComponent<Renderer>();
            if (_renderer != null) _baseColor = _renderer.material.color;
            BuildDashTrail();
            _weaponIndex = Mathf.Max(0, Kadro.Yuvadaki(0));   // Faz 30: envanterin ilk yuvası
        }

        /// <summary>
        /// Dash izi. Kendi iz kodumuzu yazmıyoruz — `TrailRenderer` Unity'nin hazır
        /// özelliği ve tam bu iş için var.
        ///
        /// Dash'in sesi Faz 6'da geldi ama görseli yoktu: hızlı hareket ettiğinde
        /// kapsül sadece ışınlanıyormuş gibi duruyordu. İz, kat edilen mesafeyi
        /// gösteriyor — i-frame penceresinin nereden nereye taşındığı okunur oluyor.
        /// </summary>
        void BuildDashTrail()
        {
            _dashTrail = gameObject.AddComponent<TrailRenderer>();
            _dashTrail.material = UnlitMat.Additive;
            _dashTrail.time = 0.20f;                  // dashDuration'dan uzun: dash bitince biraz sarksın
            _dashTrail.widthCurve = AnimationCurve.Linear(0f, 0.55f, 1f, 0f);
            _dashTrail.numCapVertices = 4;
            _dashTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _dashTrail.receiveShadows = false;
            _dashTrail.colorGradient = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.55f, 0.85f, 1f), 0f),
                    new GradientColorKey(new Color(0.20f, 0.45f, 0.9f), 1f)
                },
                alphaKeys = new[] { new GradientAlphaKey(0.75f, 0f), new GradientAlphaKey(0f, 1f) }
            };
            _dashTrail.emitting = false;   // yalnızca dash sırasında
        }

        /// <summary>
        /// ⚠️ ARENA SINIRI — TEK YERDE, `LateUpdate`'te.
        ///
        /// Enes: *"arenanın sınırı yok aq."* Oyuncuda hiçbir kısıt yoktu; düşmanların
        /// zaten vardı (`Enemy.ChaseMove`'daki `tavan`).
        ///
        /// ⚠️ Neden `Move()` içinde değil: oyuncuyu HAREKET ETTİREN üç yol var —
        /// yürüme, dash ve geri itilme. Üçüne ayrı ayrı kısıt koysaydım biri
        /// unutulurdu ve tam da o yoldan dışarı çıkılırdı. `LateUpdate` o karedeki
        /// bütün hareketlerden SONRA çalışıyor: hangi yol olursa olsun yakalıyor.
        /// </summary>
        void LateUpdate()
        {
            if (_t == null) return;

            Vector3 merkez = Bootstrap.ArenaCenter;
            Vector3 d = transform.position - merkez;
            d.y = 0f;                                   // tuzak 2: y yatay hesaba karışmasın
            if (d.magnitude <= _t.arenaRadius) return;

            d = d.normalized * _t.arenaRadius;
            transform.position = new Vector3(merkez.x + d.x, transform.position.y, merkez.z + d.z);
            _knockback = Vector3.zero;                  // duvara yapışıp titremesin
        }

        void Update()
        {
            // Koşu bitti (ölüm ya da zafer): oyuncu da durur — düşmanlarla aynı kural.
            if (_t == null || (Clock.Instance != null && (Clock.Instance.Dead || Clock.Instance.Won))) return;

            _dashCd -= Time.deltaTime;
            _iFrames -= Time.deltaTime;
            for (int i = 0; i < _specialCd.Length; i++) _specialCd[i] -= Time.deltaTime;
            TickFlurry(Time.deltaTime);
            Tickİtiraz(Time.deltaTime);
            _karşılamaBekleme -= Time.deltaTime;
            if (_görünmezKalan > 0f && (_görünmezKalan -= Time.deltaTime) <= 0f) Paint();
            TickHurt(Time.deltaTime);
            TickCombo(Time.deltaTime);
            ReadWeaponSwap();

            Vector3 input = ReadMove();

            if (Dashing) { TickDash(); return; }        // dash sırasında saldırı girdisi burada okunuyor

            if (ReadDash() && _dashCd <= 0f)
            {
                StartDash(input);
                return;                     // dash saldırıyı iptal eder
            }

            if (Attacking) { TickAttack(input); return; }

            // TAHVİL (Faz 32): sol tık basılı — olgunlaşıyor; bırakınca iner. Sağ tık olgunluğu bölüştürür.
            if (_vadeAçık)
            {
                if (ReadSpecial() && _specialCd[_weaponIndex] <= 0f) { StartSpecial(input); return; }
                if (AttackHeld())
                {
                    Vector3 nişan = AimDirection(input);
                    if (nişan.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(nişan);
                    Vadelen(Time.deltaTime);
                    Move(input, _t.tahvilMoveDamping, rotate: false);
                    return;
                }
                TahvilBoz();
                return;
            }

            // ⚠️ Özel saldırı normal saldırıdan ÖNCE okunuyor: ikisi de basılıysa
            // pahalı ve bekleme süreli olan kazanmalı, yoksa oyuncu onu kaçırır.
            if (ReadSpecial() && _specialCd[_weaponIndex] <= 0f) { StartSpecial(input); return; }

            if (ReadAttack()) { StartAttack(input); return; }

            Move(input, 1f);
        }

        // ── girdi ────────────────────────────────────────────────────────────
        // ponytail: cihazdan doğrudan okuma. InputActionAsset'e geçiş Faz 2'de,
        // tuş atama menüsü gerektiğinde.

        Vector3 ReadMove()
        {
            Vector2 v = Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed) v.y += 1f;
                if (kb.sKey.isPressed) v.y -= 1f;
                if (kb.dKey.isPressed) v.x += 1f;
                if (kb.aKey.isPressed) v.x -= 1f;
            }
            var gp = Gamepad.current;
            if (gp != null && v == Vector2.zero) v = gp.leftStick.ReadValue();

            if (v.sqrMagnitude > 1f) v.Normalize();
            return CameraRelative(v);
        }

        static bool ReadDash()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonSouth.wasPressedThisFrame;
        }

        /// <summary>
        /// Envanter YUVALARININ tuşları (Faz 30): kadro 12 silaha çıksa da yanında 3 silah var,
        /// tuşlar silahı değil yuvayı seçiyor. Yuvayı KADRO sunağı dolduruyor.
        /// </summary>
        static readonly Key[] SwapKeys = { Key.Digit1, Key.Digit2, Key.Digit3 };

        void ReadWeaponSwap()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // ⚠️ Listeden sürülüyor, elle sayılmıyor. Sabit 1-4 yazılıydı ve kadro
            // 5'e çıkınca BEŞİNCİ SİLAH ERİŞİLEMEZ olacaktı — ders 11'in aynısı
            // ("tanımlı ≠ ulaşılabilir"), sadece silahta. Öz-denetim de bunu ölçüyor.
            for (int i = 0; i < SwapKeys.Length; i++)
                if (kb[SwapKeys[i]].wasPressedThisFrame)
                {
                    int w = Kadro.Yuvadaki(i);                 // boş yuva: hiçbir şey olmuyor
                    if (w >= 0) SetWeapon(w);
                    return;
                }
        }

        /// <summary>Silah kuşan. Öz-denetim de bu yoldan geçiyor — test kendi yolunu kurmaz.</summary>
        public void SetWeapon(int index)
        {
            if (index < 0 || index >= TekCan.Weapon.All.Length || index == _weaponIndex) return;
            _weaponIndex = index;
            _attackTimer = 0f;                                    // yarım savurma taşınmasın
            CancelFlurry();                                       // seri ESKİ silaha aitti
            RehineyiBırak();                                      // REHİN'i başka silahla tutamazsın

            // ⚠️ Eskiden burada ekran sarsılıyordu. Enes: *"silahlar arası geçişi
            // daha hızlı yapabilmeliyiz, kombolar için."* Geçiş KODDA zaten anlıktı
            // (Update'in en başında okunuyor, dash ve saldırı dönüşlerinden ÖNCE);
            // yavaşlatan şey mekanik değil SARSINTIYDI — her tuşa basışta ekran
            // zıplayınca kombo akmıyor, tökezliyor. Ses kalıyor, sarsıntı gidiyor.
            Sfx.Swing();
        }

        static bool ReadSpecial()
        {
            var m = Mouse.current;
            if (m != null && m.rightButton.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            // ⚠️ Kolda Y aynı zamanda SUNAK tuşu (`Altar.UsePressed`). Sunağın yanında
            // özel saldırı sayılmıyor: yoksa alışveriş + FAİZLİ İCRA'sı birlikte
            // ateşleniyor, 3 sn bedel ve bekleme süresi boşa gidiyordu.
            return gp != null && gp.buttonNorth.wasPressedThisFrame && !SunakYakın();
        }

        static bool SunakYakın()
        {
            foreach (var a in Altar.All) if (a != null && a.PlayerInRange) return true;
            return false;
        }

        /// <summary>Öz-denetim ve HUD için: özel saldırı hazır mı, değilse ne kadar kaldı.</summary>
        public float SpecialCooldownLeft => Mathf.Max(0f, _specialCd[_weaponIndex]);

        /// <summary>HUD için: belirli bir silahın bekleme süresi (hepsi ayrı).</summary>
        public float SpecialCooldownOf(int weaponIndex) =>
            weaponIndex >= 0 && weaponIndex < _specialCd.Length
                ? Mathf.Max(0f, _specialCd[weaponIndex]) : 0f;

        /// <summary>
        /// Sağ tık saldırısı. ⚠️ Kendi isabet yolunu YAZMIYOR — `StrikeCone`'dan
        /// geçiyor, tıpkı normal savurma ve dash-atak gibi. İkinci bir isabet kodu
        /// olsaydı biri düzeltilip öteki unutulurdu; bu projede "aynı kavramın her
        /// ölçümü" dersi zaten iki kez alındı.
        /// </summary>
        void StartSpecial(Vector3 input)
        {
            var sp = Weapon.Special(_t);
            if (sp == null) return;
            if (Weapon.Saldırı == Biçim.Çek && CekUcusu.Sahibinin(this) > 0) return;   // dönene kadar silahsız
            if (Weapon.Saldırı == Biçim.Kira && Kule == null) return;                   // patlatacak kule yok

            // Ödeyemiyorsan çalışmıyor — FAİZLİ'nin savurma kuralının aynısı.
            // İCRA (Faz 29): bedel saatin yüzdesi (en az `Cost`). Öldürme iadesi yalnızca saat doyma
            // tavanının altındayken — "eşiğin üstündeysen zaman vermez" (Enes).
            var clock = Clock.Instance;
            float bedel = sp.Cost;
            if (sp.CostPercent > 0f && clock != null) bedel = Mathf.Max(bedel, sp.CostPercent * clock.Seconds);
            _özelBedeli = bedel;
            _özelİadeVar = clock == null || clock.HasatDoyması >= 0.999f;
            if (bedel > 0f)
            {
                if (clock == null || clock.Seconds <= bedel) return;
                clock.Spend(bedel);
            }

            _specialCd[_weaponIndex] = sp.Cooldown * ÖzelBeklemeÇarpanı();
            _attackTimer = Weapon.Duration(_t);      // taahhüt: özel de bir savurma
            _attackHitDone = true;                   // vuruş BURADA indi, TickAttack tekrarlamasın

            Vector3 aim = AimDirection(input);
            if (aim.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(aim);

            Sfx.Swing();

            // ── kendi biçimi olan özeller (Faz 31) ──
            if (Weapon.Saldırı == Biçim.Rehin) { Takas(sp); return; }
            if (Weapon.Saldırı == Biçim.Çek)                         // KARŞILIKSIZ: yelpaze
            {
                for (int i = 0; i < sp.Hits; i++)
                    ÇekFırlat(Quaternion.Euler(0f, (i - (sp.Hits - 1) * 0.5f) * 20f, 0f) * transform.forward, sp.DamageMul);
                return;
            }
            if (Weapon.Saldırı == Biçim.Tahvil) { VadesiGelmiş(sp); return; }         // Faz 32
            if (Weapon.Saldırı == Biçim.Müşterek) { Zincirle(sp); return; }
            if (Weapon.Saldırı == Biçim.Kira) { Tahliye(); return; }
            if (Weapon.Saldırı == Biçim.Arbitraj)                    // AÇIK POZİSYON: görünmez
            {
                _görünmezKalan = _t.arbitrajHideSeconds * (Y(SilahYükseltme.Gölge) ? 1.5f : 1f);
                _attackTimer = 0f;                                   // saklanmak savurma değil
                Paint();
                return;
            }
            if (sp.Duruş > 0f)                                       // İTİRAZ: uzun karşılama
            {
                _itirazKalan = sp.Duruş;
                _attackTimer = sp.Duruş;
                Arc.Spawn(transform.position, transform.rotation, 1.1f, 360f, sp.Tint, sp.Duruş, follow: transform, scaled: true);
                return;
            }

            // ── yer vuruşu olan özel ──
            // İPOTEK YAĞMURU (Faz 29): Hits > 1 ise alandaki düşmanların üstüne peş peşe ipotek —
            // ilki hemen, gerisi seri sayacıyla (TASFİYE ile aynı yol; dash ve silah değişimi keser).
            if (sp.GroundDelay > 0f && sp.Hits > 1)
            {
                YağmurDamlası(sp);
                _flurrySpecial = sp;
                _flurryLeft = sp.Hits - 1;
                _flurryTimer = sp.HitInterval;
                return;
            }
            if (sp.GroundDelay > 0f)
            {
                PlaceGroundMark(Weapon.Range(_t) * sp.RangeMul, sp.GroundRadius, sp.GroundDelay,
                                WeaponDamage() * sp.DamageMul,
                                Weapon.HarvestMultiplier * sp.HarvestMul);
                return;
            }

            // ⚠️ SERİ VURUŞ (İFLAS · TASFİYE): ilk vuruş HEMEN iniyor, gerisi
            // sayaçla. "Bas ve bekle" hissi olmasın — silahın kimliği HIZ.
            SpecialStrike(sp);
            int vuruş = sp.Hits + (Y(SilahYükseltme.Seri) ? 2 : 0);   // SERİ (İFLAS, Faz 29)
            if (vuruş > 1)
            {
                _flurrySpecial = sp;
                _flurryLeft = vuruş - 1;
                _flurryTimer = sp.HitInterval;
                _attackTimer = sp.HitInterval * vuruş;   // seri boyunca taahhüt
            }
        }

        /// <summary>
        /// Seri vuruşun bir adımı. ⚠️ `dt` DIŞARIDAN — projenin kuralı.
        /// Öz-denetim bunu çevirip "yedi vuruş GERÇEKTEN indi mi" diye soruyor;
        /// coroutine olsaydı yalnızca "başladı mı" sorulabilirdi.
        /// </summary>
        public void TickFlurry(float dt)
        {
            if (_flurryLeft <= 0 || _flurrySpecial == null) return;

            _flurryTimer -= dt;
            if (_flurryTimer > 0f) return;

            _flurryTimer = _flurrySpecial.HitInterval;
            _flurryLeft--;
            if (_flurrySpecial.GroundDelay > 0f) YağmurDamlası(_flurrySpecial);
            else SpecialStrike(_flurrySpecial);
            if (_flurryLeft <= 0) _flurrySpecial = null;
        }

        /// <summary>Öz-denetim için: seri vuruşta kaç vuruş kaldı.</summary>
        public int FlurryLeft => _flurryLeft;

        /// <summary>
        /// Seriyi kes. ⚠️ Seri, kendisini BAŞLATAN eylemi aşan bir durum: sayaçla
        /// ilerlediği için silah değişse de dash atılsa da kendi başına devam
        /// ediyordu. İki somut sonucu vardı:
        ///
        ///   ① Silah değiştirince `SpecialStrike` YENİ silahın hasarını, menzilini
        ///      ve hasat çarpanını okuyordu — İFLAS'ın serisini FAİZLİ'nin hasarıyla
        ///      atmak mümkündü. Düpedüz istismar.
        ///   ② Dash seriyi kesmiyordu; oysa dash bu oyunda HER saldırıyı keser
        ///      (`_attackTimer = 0f` iki yerde de zaten var).
        ///
        /// Bu yüzden iptal, saldırının iptal edildiği YERLERE bağlandı — ayrı bir
        /// kural değil, var olan kuralın eksik kalmış yarısı.
        /// </summary>
        void CancelFlurry()
        {
            _flurryLeft = 0;
            _flurrySpecial = null;
            _itirazKalan = 0f;          // İTİRAZ da süren bir saldırı: aynı yerler keser (biriken kalır)
            _vadeAçık = false;          // TAHVİL'in olgunluğu da (dash ERKEN BOZDURMA için önce saklıyor)
            _vade = 0f;
        }

        /// <summary>
        /// Özelin TEK bir vuruşu. Seri de tek de buradan geçiyor — iki ayrı vuruş
        /// kodu olsaydı biri düzeltilip öteki unutulurdu.
        /// </summary>
        void SpecialStrike(WeaponSpecial sp)
        {
            float range = Weapon.Range(_t) * sp.RangeMul;
            float angle = sp.Angle > 0f ? sp.Angle : Weapon.Angle(_t);

            Arc.Spawn(transform.position, transform.rotation, range, angle,
                      sp.Tint, _t.swingArcLife * (sp.Hits > 1 ? 0.7f : 1.6f),
                      scaleFrom: sp.Hits > 1 ? 0.85f : 0.5f, scaleTo: 1f);
            SwingsSpawned++;

            StrikeCone(range, angle, WeaponDamage() * sp.DamageMul + _özelBedeli * sp.DamagePerCost,
                       _t.hitStopDuration * (sp.Hits > 1 ? 0.35f : 1.6f) * Weapon.HitStopMultiplier,
                       _t.hitShake * (sp.Hits > 1 ? 0.5f : 1.8f) * Weapon.ShakeMultiplier,
                       sp.MaxTargets, Weapon.HarvestMultiplier * sp.HarvestMul,
                       out int hits, out int kills,
                       minRange: 0f, special: sp);

            // Öldürme iadesi (FAİZLİ · İCRA): bahsi kazandıysan bedelin katı geri geliyor.
            if (kills > 0 && sp.KillRefundMul > 0f && _özelİadeVar && Clock.Instance != null)
                Clock.Instance.Add(_özelBedeli * sp.KillRefundMul * kills);
            İsabetSonrası(hits);
        }

        static bool ReadAttack()
        {
            var m = Mouse.current;
            if (m != null && m.leftButton.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonWest.wasPressedThisFrame;
        }

        /// <summary>
        /// Saldırının yöneleceği yön. Sıra: fare imleci → gamepad sağ çubuk →
        /// hareket girdisi → mevcut bakış. İzometrikte imleçle nişan standarttır;
        /// oyuncu kaçarken bile istediği tarafa vurabilmeli.
        /// </summary>
        /// <summary>
        /// İmlecin YER DÜZLEMİNDEKİ noktası. `AimDirection` ve İPOTEK'in yerleştirme
        /// noktası aynı ışını istiyordu; iki kopya olsaydı biri düzeltilip öteki
        /// unutulurdu — nişan ile vuruşun ayrışması bu projede zaten üç kez oldu.
        /// </summary>
        bool CursorGround(out Vector3 point)
        {
            point = transform.position;
            var mouse = Mouse.current;
            if (mouse == null || _camera == null) return false;

            var ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            var ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (!ground.Raycast(ray, out float dist)) return false;

            point = ray.GetPoint(dist);
            return true;
        }

        Vector3 AimDirection(Vector3 moveInput)
        {
            if (CursorGround(out Vector3 nokta))
            {
                Vector3 to = nokta - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.04f) return to.normalized;
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 rs = gp.rightStick.ReadValue();
                if (rs.sqrMagnitude > 0.09f) return CameraRelative(rs).normalized;
            }

            if (moveInput.sqrMagnitude > 0.0001f) return moveInput.normalized;
            return transform.forward;
        }

        /// <summary>Girdiyi kamera düzlemine çevirir — izometrikte şart, yoksa WASD çapraz hisseder.</summary>
        Vector3 CameraRelative(Vector2 v)
        {
            if (_cam == null) return new Vector3(v.x, 0f, v.y);
            Vector3 f = _cam.forward; f.y = 0f; f.Normalize();
            Vector3 r = _cam.right;   r.y = 0f; r.Normalize();
            return f * v.y + r * v.x;
        }

        // ── hareket ──────────────────────────────────────────────────────────

        /// <summary>
        /// rotate=false ise yalnızca yer değiştirir, dönmez.
        /// Saldırı sırasında dönmek YASAK: nişan StartAttack'te imlece kilitleniyor ama
        /// vuruş ~0.1 sn sonra iniyor. Arada hareket tuşu oyuncuyu çevirirse yay bir yere,
        /// isabet başka yere gider — savurmaya bakıp ıskalarsın.
        /// </summary>
        void Move(Vector3 dir, float damping, bool rotate = true)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.position += dir * _t.moveSpeed * damping * Time.deltaTime;
            if (rotate)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir), _t.turnSpeed * Time.deltaTime);
        }

        void StartDash(Vector3 input)
        {
            // ⚠️ Her silahta GİTTİĞİN YÖNE. HACİZ Faz 16–24 arası nişanın tersine
            // atıyordu; Enes kafa karıştırdığını bildirdi (2026-09-30), kalktı.
            _dashDir = input.sqrMagnitude > 0.0001f ? input.normalized : transform.forward;

            _dashTimer = _t.dashDuration;
            _dashCd = Clock.Instance != null ? Clock.Instance.DashCooldown : _t.dashCooldown;
            _iFrames = _t.iFrameDuration;
            Sfx.Dash();
            _attackTimer = 0f;              // dash saldırıyı keser
            _dashBaşı = transform.position;                                        // KOPARMA, TERS İŞLEM
            _bozdurma = _vadeAçık ? VadeÇarpanı(Kademe(_vade)) : 0f;               // ERKEN BOZDURMA
            CancelFlurry();                 // ...seriyi de keser, o da bir saldırı
            _dashAttackQueued = false;      // önceki dash'ten sarkmasın
            if (_bozdurma > 0f) _dashAttackQueued = true;   // TAHVİL: basılı tutarak dash = ERKEN BOZDURMA
            transform.rotation = Quaternion.LookRotation(_dashDir);
            if (_dashTrail != null) { _dashTrail.Clear(); _dashTrail.emitting = true; }
        }

        void TickDash()
        {
            // DASH-ATAK: dash sırasında saldırıya basarsan dash bitiminde geniş bir
            // vuruş iner. Dash hem kaçış hem giriş oluyor — kaçtığın yerde bırakmıyor,
            // içine dalıp çıkmanı ödüllendiriyor. (Enes istedi, Faz 1'den devir.)
            if (!_dashAttackQueued && ReadAttack()) _dashAttackQueued = true;

            _dashTimer -= Time.deltaTime;
            float speed = _t.dashDistance / Mathf.Max(0.0001f, _t.dashDuration);
            transform.position += _dashDir * speed * Time.deltaTime;

            if (_dashTimer > 0f) return;

            // İz yalnızca dash boyunca ÇİZİLİR; var olan kuyruk kendi kendine sönüyor.
            // Kapatmasaydık normal yürürken de arkamızda mavi bir şerit kalırdı ve
            // dash'in "bu farklı bir hareket" okunurluğu yok olurdu.
            if (_dashTrail != null) _dashTrail.emitting = false;

            if (_dashAttackQueued)
            {
                _dashAttackQueued = false;
                DashStrike();
            }
        }

        void DashStrike()
        {
            // Dash-atak dash yönüne iner. (HACİZ'in geri dash'i varken burada yön
            // ters çevriliyordu; dash kalkınca o dal da kalktı — Faz 25.)
            if (_dashDir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(_dashDir);

            // İPOTEK (Faz 29, Enes): dash-atak = eski sağ tık HACİZ İHBARI — nişan noktasına geniş,
            // geç patlayan işaret. Yay vurmuyor.
            var ds = Weapon.DashSpecial(_t);
            if (ds != null && ds.GroundDelay > 0f)
            {
                var ihbar = PlaceGroundMark(Weapon.Range(_t) * ds.RangeMul, ds.GroundRadius, ds.GroundDelay,
                                            WeaponDamage() * ds.DamageMul, Weapon.HarvestMultiplier * ds.HarvestMul);
                if (ihbar != null) ZincireYaz(_weaponIndex, ihbar.transform.position);   // combo adımı
                Sfx.Swing();
                return;
            }

            if (Weapon.Saldırı == Biçim.Rehin) { Sürükle(); return; }   // SÜRÜKLEME (Faz 31)
            if (Weapon.Saldırı == Biçim.Çek) { Ciro(); return; }        // CİRO
            if (Weapon.Saldırı == Biçim.Tahvil && _bozdurma > 0f) { Bozdur(); return; }      // ERKEN BOZDURMA (Faz 32)
            if (Weapon.Saldırı == Biçim.Müşterek && Kopar()) return;                         // KOPARMA
            if (Weapon.Saldırı == Biçim.Arbitraj && Geçilen() is Enemy geçilen) { Tersİşlem(geçilen); return; }   // TERS İŞLEM
            if (Weapon.Saldırı == Biçim.Kira && Kule != null) { Kule.Taşı(transform.position); Sfx.Swing(); return; }   // TAŞINMA

            // Dash-atak artık SİLAHA GÖRE değişiyor: tek hedefli silah dash'te de tek
            // hedefli kalır ama çok sert vurur; koni süpürür ama vuruş başına daha az.
            // Eskiden hepsi aynı geniş alan vuruşuydu ve silahın kimliğini siliyordu.
            float range = TemelMenzil * _t.dashAttackRangeMult;
            float angle = Weapon.DashAngle(_t);
            float damage = WeaponDamage() * Weapon.DashDamageMultiplier(_t);
            // KARŞI DAVA (KARŞILIK): biriken karşılığın İKİ katı dash-atağa biniyor.
            float karşıDava = Weapon.Saldırı == Biçim.Karşılık ? _biriken * 2f * _t.karsilikDamagePerSecond : 0f;
            damage += karşıDava;

            // Silahın kendi rengi ama daha parlak — dash-atak olduğu okunsun.
            var tint = Weapon.Tint; tint.a = Mathf.Min(1f, tint.a * 1.5f);
            Arc.Spawn(transform.position, transform.rotation,
                range, angle, tint, _t.swingArcLife * 1.3f);
            SwingsSpawned++;
            Sfx.Swing();

            // Dash-atak zaten ağır; silahın kendi ağırlığı ONUN ÜSTÜNE biniyor,
            // yerine geçmiyor — yoksa FAİZLİ'nin dash'i normal savurmasından hafif kalırdı.
            StrikeCone(range, angle, damage,
                       _t.hitStopDuration * 1.4f * Weapon.HitStopMultiplier,
                       _t.hitShake * 1.5f * Weapon.ShakeMultiplier,
                       Weapon.DashTargets, Weapon.HarvestMultiplier, out int hits, out int kills);
            if (karşıDava > 0f && hits > 0) _biriken = 0f;

            // ⚠️ FAİZLİ: dash-atak TUTARSA savurma bedeli geri geliyor. Silahın
            // sorusu "tutar mı"; dash onu en zor haliyle soruyor (hareket halinde,
            // tek hedef) ve doğru cevabı ödüllendiriyor.
            if (Weapon.DashRefundsCost && hits > 0) Clock.Instance?.Add(_t.faizliSwingCost);

            Weapon.OnStrike(Clock.Instance, _t, hits, kills);
            İsabetSonrası(hits);
        }

        // ── saldırı ──────────────────────────────────────────────────────────

        void StartAttack(Vector3 input)
        {
            // Silah savurmayı reddedebilir (FAİZLİ: peşin bedeli ödeyemiyorsan susar).
            if (Weapon.Saldırı == Biçim.Çek && CekUcusu.Sahibinin(this) > 0) return;   // dönene kadar silahsız
            if (Weapon.Saldırı == Biçim.Karşılık && _karşılamaBekleme > 0f) return;      // toparlanma bitmeden yok
            if (Weapon.Saldırı == Biçim.Tahvil)                                         // basılı tutma başlıyor
            {
                _vadeAçık = true;
                _vade = 0f;
                Vector3 nişan = AimDirection(input);
                if (nişan.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(nişan);
                return;
            }
            if (!Weapon.OnSwingStart(Clock.Instance, _t)) return;

            _attackTimer = SavurmaSüresi;
            if (Weapon.Saldırı == Biçim.Karşılık) _karşılamaBekleme = SavurmaSüresi;
            _attackHitDone = false;

            // ⚠️ NİŞAN İMLEÇTEN ALINIR, hareket yönünden değil.
            // Eskiden hareket yönüne dönüyordu: kaçarken vurunca yay boşluğa gidiyordu,
            // düşman arkanda kalıyordu ve hiçbir şeye isabet etmiyordu.
            Vector3 aim = AimDirection(input);
            if (aim.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(aim);

            // ⚠️ ISKADA DA ÇIKAR. Faz 1'in ilk playtest'i tam olarak bu eksik olduğu için
            // yürümedi: oyuncu saldırdığını göremeyince hissi yargılayamadı.
            // Yay, gerçek vuruş alanının kendisi — menzil ve açı Strike() ile aynı kaynaktan.
            // ⚠️ YER VURUŞU SİLAHI SAVURMA YAYI ÇİZMEZ. İPOTEK koni vurmuyor, ama yay
            // koşulsuz çiziliyordu: ekranda kocaman bir koni açılıyor ve HİÇBİR ŞEYE
            // VURMUYOR. Enes bildirdi. Bu, "mekanik doğru görsel yalan" hatasının
            // DÖRDÜNCÜSÜ — ve en kötüsü, çünkü yay burada isabet alanının kendisi
            // olduğunu söylüyor. İşaretin kendi halkası zaten `GroundMark` içinde çiziliyor.
            // REHİN tutarken: fırlatmanın dar hattı. ÇEK: yay yok, çekin kendisi görünüyor.
            if (Weapon.Saldırı == Biçim.Rehin && Tutuyor)
                Arc.Spawn(transform.position, transform.rotation, _t.rehinThrowRange, 10f, Weapon.Tint, _t.swingArcLife);
            else if (Weapon.GroundMarkDelay(_t) <= 0f && Weapon.Saldırı != Biçim.Çek
                     && !(Weapon.Saldırı == Biçim.Kira && Kule == null))      // kule kurmak savurma değil
                Arc.Spawn(transform.position, transform.rotation,
                    SavurmaMenzili, SavurmaAçısı, Weapon.Tint, _t.swingArcLife);
            // KARŞILIK: karşılama penceresi her yönden — halka, pencere bitene kadar.
            if (Weapon.Saldırı == Biçim.Karşılık)
                Arc.Spawn(transform.position, transform.rotation, 1.1f, 360f, new Color(0.95f, 0.95f, 0.6f, 0.35f),
                          SavurmaSüresi * Weapon.HitMoment(_t), follow: transform, scaled: true);

            // ⚠️ ÖLÜ BÖLGE DE GÖRÜNMELİ (HACİZ). Boss'un simit göbeğiyle aynı gerekçe,
            // ama TERS ANLAM: boss'unki güvenli olduğu için YEŞİL, bu ise silahın
            // ÇALIŞMADIĞI yer, o yüzden kırmızı. Görünmeseydi oyuncu dibindekine
            // savurur, ıskalar ve sebebini asla anlamazdı — ders 10'un aynısı.
            float ölüBölge = ÖlüBölge;
            if (ölüBölge > 0f)
                Arc.Spawn(transform.position, transform.rotation,
                    ölüBölge, SavurmaAçısı, new Color(0.95f, 0.35f, 0.3f, 0.3f),
                    _t.swingArcLife);

            SwingsSpawned++;
            Sfx.Swing();
        }

        void TickAttack(Vector3 input)
        {
            _attackTimer -= Time.deltaTime;

            // ⚠️ SALDIRI TAAHHÜTTÜR. Eskiden savurma sırasında %25 hızla hareket
            // edilebiliyordu; geri çekilince düşman menzilden çıkıyordu ve vuruş
            // ıskalıyordu — Enes'in "hareket tuşlarıyla bozuluyor" dediği şey buydu.
            // Saldırıyı bozan tek şey DASH olmalı, WASD değil.
            Move(input, _t.attackMoveDamping, rotate: false);

            float duration = SavurmaSüresi;
            float elapsed = duration - _attackTimer;
            if (!_attackHitDone && elapsed >= duration * Weapon.HitMoment(_t))
            {
                _attackHitDone = true;
                Strike();
            }
        }

        /// <summary>
        /// Silahın nihai hasarı — sunak yükseltmeleri + İFLAS'ın çaresizlik eğrisi.
        /// ⚠️ TEK yer: normal savurma da dash-atak da buradan geçiyor. İkisinde ayrı
        /// hesaplansaydı İFLAS dash'te eğrisini kaybederdi ve kimse fark etmezdi.
        /// </summary>
        float WeaponDamage(float saatÇarpanı = 1f)
        {
            float baseDamage = Clock.Instance != null ? Clock.Instance.AttackDamage : _t.attackDamage;
            float saniye = (Clock.Instance != null ? Clock.Instance.Seconds : _t.startSeconds) * saatÇarpanı;
            if (Y(SilahYükseltme.Dip)) saniye /= 1.5f;      // DİP (Faz 29): eşikler ×1.5, daha erken sertleşir
            return Weapon.Damage(_t, baseDamage) * Weapon.DesperationMultiplier(saniye, _t);
        }

        void Strike()
        {
            // ⚠️ İPOTEK koni vurmuyor: nişan noktasına GECİKMELİ yer vuruşu bırakıyor.
            // Silahın kendi vuruş biçimi olması `Weapon`'ın deseniyle uyumlu —
            // veri + kanca, `PlayerController` içinde silah adı geçmiyor.
            if (Weapon.GroundMarkDelay(_t) > 0f) { PlaceLien(); return; }
            if (Weapon.Saldırı == Biçim.Rehin) { RehinVuruşu(); return; }
            if (Weapon.Saldırı == Biçim.Tahvil) { TahvilBoz(); return; }               // Faz 32
            if (Weapon.Saldırı == Biçim.Kira && Kule == null) { KuleKur(); return; }
            if (Weapon.Saldırı == Biçim.Çek)
            {
                ÇekFırlat(transform.forward, 1f);
                Weapon.OnStrike(Clock.Instance, _t, 0, 0);
                return;
            }

            // COMBO bitirenleri (Faz 30–31) — zincirin son adımı bu koni vuruşu:
            //   TEMERRÜT (FAİZLİ → İFLAS): saat yarıdaymış gibi; "+" hasat ×2
            //   KARŞI İCRA (KARŞILIK → FAİZLİ) ×2; "+" İCRA'nın beklemesi sıfır
            //   SENET HACZİ (ÇEK → HACİZ) ×2; "+" yanındakine seker
            //   SİLİP SÜPÜR (REHİN → KONİ) ×1.5, sert iter; "+" 360°
            //   AÇIĞA SATIŞ (REHİN → ARBİTRAJ) her yönden arkadan; "+" ×1.5 daha
            //   VADE FARKI (TAHVİL → FAİZLİ) TAHVİL'in kademe çarpanıyla (en az ×1.5); "+" bedel geri
            ComboTür? bitiren = null;
            foreach (var c in Bitirenler) if (ComboHazır(c)) { bitiren = c; break; }
            bool artı = bitiren.HasValue && Combo.Artı(bitiren.Value);
            float hasarÇ = bitiren == ComboTür.KarşıİCRA || bitiren == ComboTür.SenetHaczi ? 2f
                         : bitiren == ComboTür.SilipSüpür ? 1.5f
                         : bitiren == ComboTür.VadeFarkı ? Mathf.Max(1.5f, _sonVadeÇarpanı)
                         : bitiren == ComboTür.AçığaSatış && artı ? 1.5f : 1f;
            _zorlaArka = bitiren == ComboTür.AçığaSatış;
            float hasat = Weapon.HarvestMultiplier * (bitiren == ComboTür.Temerrüt && artı ? 2f : 1f);
            float açı = bitiren == ComboTür.SilipSüpür && artı ? 360f : SavurmaAçısı;          // TOZ DUMAN
            if (açı >= 360f) Arc.Spawn(transform.position, transform.rotation, SavurmaMenzili, 360f, Weapon.Tint, _t.swingArcLife * 1.4f);
            bool karşılık = Weapon.Saldırı == Biçim.Karşılık;
            float hasar = karşılık ? KarşılıkHasarı() : WeaponDamage(bitiren == ComboTür.Temerrüt ? 0.5f : 1f) * hasarÇ;
            StrikeCone(SavurmaMenzili, açı, hasar,
                       _t.hitStopDuration * Weapon.HitStopMultiplier,
                       _t.hitShake * Weapon.ShakeMultiplier,
                       SavurmaHedefi, hasat,
                       out int hits, out int kills,
                       minRange: ÖlüBölge, itmeTaban: bitiren == ComboTür.SilipSüpür ? 8f : 0f);
            _zorlaArka = false;
            if (karşılık && hits > 0) _biriken = 0f;
            if (Weapon.Saldırı == Biçim.Müşterek && hits > 0) Bağla(_sonVurulan);

            Weapon.OnStrike(Clock.Instance, _t, hits, kills);
            if (bitiren.HasValue && hits > 0)
            {
                ComboTetikle(bitiren.Value);
                if (artı && bitiren == ComboTür.KarşıİCRA) _specialCd[Kadro.FAİZLİ] = 0f;   // İCRA EMRİ
                if (artı && bitiren == ComboTür.SenetHaczi) SenetZinciri(hasar, hasat);       // SENET ZİNCİRİ
                if (artı && bitiren == ComboTür.VadeFarkı && Clock.Instance != null)          // KUPON
                    Clock.Instance.Add(_t.faizliSwingCost);
            }
            else İsabetSonrası(hits);

            // FAİZLİ yükseltmeleri (Faz 29): BİLEŞİK öldürme iadesini ×2'den ×3'e çıkarıyor,
            // TEMERRÜT ıskalayınca bedelin yarısını geri veriyor.
            var saat = Clock.Instance;
            if (saat != null && Y(SilahYükseltme.Bileşik) && kills > 0) saat.Add(_t.faizliSwingCost * kills);
            if (saat != null && Y(SilahYükseltme.Temerrüt) && hits == 0) saat.Add(_t.faizliSwingCost * 0.5f);
        }

        /// <summary>
        /// İPOTEK'in vuruşu — nişan noktasına gecikmeli alan işareti.
        ///
        /// ⚠️ Nokta MENZİLE KIRPILIYOR. Kırpmasaydım imleci ekranın ucuna götürüp
        /// arenanın öbür ucuna vurabilirdin ve "menzil" diye bir şey kalmazdı.
        /// Kırpılan nokta yönde kalıyor, yani nişan hep dürüst.
        /// </summary>
        void PlaceLien()
        {
            // ⚠️ AYNI ANDA TEK İPOTEK — spam'in YAPISAL cevabı.
            // Enes: "spam yapınca çok vuruyor, bonk bonk bonk". Gecikme savurmadan
            // uzun olduğu için üç işareti üst üste yığabiliyordun. Hasat çarpanını
            // kısmak bunu çözmezdi, sadece geciktirirdi. Tek işaret kuralı silahın
            // kimliğini de netleştiriyor: koyduğunu BEKLEYECEKSİN — "nereye
            // gidecekler" sorusu ancak bekleyince gerçek bir soru.
            if (GroundMark.ActiveLiens >= (Y(SilahYükseltme.ÇiftTapu) ? 2 : 1)) return;   // ÇİFT TAPU (Faz 29)

            // COMBO (Faz 30): önce HACİZ KARARI (KONİ → HACİZ → İPOTEK), sonra TOPLU İPOTEK (KONİ → İPOTEK).
            // Hedef combo'nun bir önceki adımının vurduğu yer — nişan değil.
            float yarıçap = Weapon.GroundMarkRadius(_t), gecikme = Weapon.GroundMarkDelay(_t), hasar = WeaponDamage();
            Vector3? yer = null;
            float itme = 0f;
            bool ikinci = false;
            if (ComboHazır(ComboTür.HacizKararı))
            {
                yer = AdımYeri(1); yarıçap *= 2f; gecikme *= 0.3f; hasar *= 2f;
                if (Combo.Artı(ComboTür.HacizKararı)) itme = 7f;                    // TAHLİYE
                ComboTetikle(ComboTür.HacizKararı);
            }
            else if (ComboHazır(ComboTür.Toplu))
            {
                yer = AdımYeri(1); gecikme *= 0.4f;
                ikinci = Combo.Artı(ComboTür.Toplu);                                 // ÇİFT HACİZ
                ComboTetikle(ComboTür.Toplu);
            }
            var işaret = PlaceGroundMark(Weapon.Range(_t), yarıçap, gecikme, hasar, Weapon.HarvestMultiplier, yer);
            if (işaret != null) { işaret.İtme = itme; ZincireYaz(_weaponIndex, işaret.transform.position); }
            if (ikinci)
            {
                var uzak = EnUzakDüşman(Weapon.Range(_t) * 1.5f);
                if (uzak != null) PlaceGroundMark(Weapon.Range(_t), yarıçap, gecikme, hasar, Weapon.HarvestMultiplier, uzak.transform.position);
            }

            // Silah "savurdu" sayılıyor: iska/isabet ayrımı işaretin KENDİSİNDE
            // çözülüyor, burada değil. Bedeli olan silahlar yine ödemesini alıyor.
            Weapon.OnStrike(Clock.Instance, _t, 0, 0);
        }

        /// <summary>
        /// Tek isabet yolu — normal saldırı da dash-atak da buradan geçiyor.
        /// İki ayrı kopya olsaydı biri düzeltilip diğeri unutulurdu; savurma yayı
        /// da aynı menzil/açıdan çiziliyor, yani görsel ile isabet asla ayrışmaz.
        ///
        /// `maxTargets` 1 ise koni içindeki EN YAKIN düşman seçilir — tek hedefli
        /// silahların tanımı bu. Geometriyi daraltmak yetmezdi: dar koni de iki
        /// düşmanı birden yakalayabilir.
        /// </summary>
        /// <summary>
        /// İsabet kuralının TEK yeri. Hem MENZİL hem AÇI düşmanın MERKEZİNE değil
        /// YÜZEYİNE ölçülür — düşman bir nokta değil, gövdesi olan bir şey.
        ///
        /// ⚠️ Enes "bossa bazen vuramıyorum" dedi ve İKİ KEZ bildirdi. İlkinde yalnızca
        /// MENZİL yüzeye çevrildi, AÇI merkezde bırakıldı — yani kök nedenin yarısı
        /// düzeltildi ve şikâyet geri geldi. Reprodüksiyon (`BossHitRepro`) ölçtü:
        /// boss 1.2 birim yakındayken 41 DERECELİK ölü bant vardı; gövde yayın içinde
        /// apaçık duruyor, merkezi dışarıda kalıyor, vuruş düşüyordu.
        ///
        /// Yay da aynı açıyla çizildiği için GÖRSEL YALAN SÖYLÜYORDU: gövdeyi yayın
        /// içinde görüyor, hiçbir şey alamıyordun.
        ///
        /// Pay = gövdenin açısal yarıçapı: `asin(r / d)`. Oyuncu gövdenin içindeyse
        /// (d ≤ r) pay 90° olur, yani her yönden vurulur — doğrusu da bu.
        /// </summary>
        bool InHitCone(Enemy e, float range, float halfAngle, out float surfaceDist,
                       float minRange = 0f)
        {
            surfaceDist = float.MaxValue;
            // ⚠️ UYKUDAKİ boss hedef DEĞİL (`TakeDamage` onu zaten yok sayıyor). Seçimde
            // kalsaydı tek hedefli silahlar en yakın gövdeyi — sahnenin ortasında yatan
            // dev boss'u — seçip vuruşu boşa harcıyordu; FAİZLİ bedelini ödüyor,
            // arkadaki rakibe hiçbir şey olmuyordu (denetim 2026-09-23).
            if (e == null || e.Dormant) return false;

            Vector3 to = e.transform.position - transform.position;
            to.y = 0f;

            float dist = to.magnitude;
            surfaceDist = dist - e.Radius;
            if (surfaceDist > range) return false;

            // ⚠️ ÖLÜ BÖLGE (HACİZ) — menzil gibi, o da YÜZEYE ölçülüyor. Merkeze
            // ölçseydim boss isabetindeki ölü bandın aynısını üretirdim, sadece ters
            // yönde: iri gövde ölü bölgeye taşar, oyuncu yayın içinde görür, vuruş
            // düşer. Aynı kavramın HER ölçümü aynı yerden (ders 9).
            if (minRange > 0f && surfaceDist < minRange) return false;

            float acisalYaricap = dist > e.Radius
                ? Mathf.Asin(Mathf.Clamp01(e.Radius / dist)) * Mathf.Rad2Deg
                : 90f;                                    // içindeysen her yön geçerli
            return Vector3.Angle(transform.forward, to) <= halfAngle + acisalYaricap;
        }

        void StrikeCone(float range, float angleDeg, float damage, float hitStop, float shake,
                        int maxTargets, float harvestMult, out int hits, out int kills,
                        float minRange = 0f, WeaponSpecial special = null, float itmeTaban = 0f)
        {
            hits = 0; kills = 0;
            _isabetToplam = Vector3.zero;
            _sonVurulan = null;
            float half = angleDeg * 0.5f;
            // KAPANIŞ (İFLAS): dipte hasat +%50 · GERİ TEPME (KONİ): itme +%60 — Faz 29
            if (Y(SilahYükseltme.Kapanış)) harvestMult *= 1f + 0.5f * DipOranı();
            float itme = special != null ? special.Knockback : Weapon.Knockback(_t);
            if (Y(SilahYükseltme.GeriTepme)) itme = Mathf.Max(itme * 1.6f, 3f);   // savurma da itiyor
            itme = Mathf.Max(itme, itmeTaban);                                      // SİLİP SÜPÜR

            if (maxTargets == 1)
            {
                Enemy nearest = null;
                float bestDist = float.MaxValue;

                foreach (var e in Enemy.All)
                {
                    if (!InHitCone(e, range, half, out float d, minRange)) continue;
                    if (d < bestDist) { bestDist = d; nearest = e; }
                }

                if (nearest != null)
                {
                    bool wasHollow = nearest.Hollow;
                    _isabetToplam = nearest.transform.position;   // ÖNCE: vuruş öldürüp yok edebilir
                    _sonVurulan = nearest;
                    nearest.Push(nearest.transform.position - transform.position,
                                 itme);
                    nearest.TakeDamage(MesafeliHasar(damage, nearest, special), harvestMult);
                    hits = 1;
                    if (wasHollow) kills = 1;          // boş düşmana vuruş öldürür
                }
            }
            else
            {
                // DELİCİ (Faz 29): sınırlı hedefte koni içindeki EN YAKIN n tanesi; sınırsızsa hepsi.
                // Kopya listede dönülüyor — vuruş düşmanı öldürüp `Enemy.All`'dan çıkarabilir.
                _hedefler.Clear();
                foreach (var aday in Enemy.All)
                    if (InHitCone(aday, range, half, out float uzak, minRange)) _hedefler.Add((aday, uzak));
                if (maxTargets < _hedefler.Count)
                {
                    _hedefler.Sort((a, b) => a.d.CompareTo(b.d));
                    _hedefler.RemoveRange(maxTargets, _hedefler.Count - maxTargets);
                }
                foreach (var (e, _) in _hedefler)
                {
                    bool wasHollow = e.Hollow;
                    _isabetToplam += e.transform.position;        // ÖNCE: vuruş öldürüp yok edebilir
                    e.Push(e.transform.position - transform.position,
                           itme);
                    e.TakeDamage(MesafeliHasar(damage, e, special), harvestMult);
                    hits++;
                    if (wasHollow) kills++;
                }
            }

            if (hits > 0)
            {
                _isabetYeri = _isabetToplam / hits;          // combo'nun hedefi: vurulanların ortası
                if (Weapon.Saldırı == Biçim.Arbitraj && Görünmez) { _görünmezKalan = 0f; Paint(); }   // vurunca görünürsün
                Sfx.Hit();
                HitStop.Freeze(hitStop);
                IsoCamera.Shake(shake);
            }
        }

        /// <summary>
        /// Yer işaretini nişan noktasına koyar — MENZİLE KIRPARAK.
        ///
        /// ⚠️ TEK YER. Normal İPOTEK savurması ve HACİZ İHBARI özeli aynı hesabı
        /// yapıyordu ve iki kopya duruyordu: biri düzeltilip öteki unutulurdu.
        /// Bu projede nişan ile vuruşun ayrışması ÜÇ KEZ hata üretti; aynı kapıyı
        /// dördüncü kez açmaya gerek yok.
        ///
        /// Kırpma şart: kırpmasaydım imleci ekranın ucuna götürüp arenanın öbür
        /// ucuna vurabilirdin ve "menzil" diye bir şey kalmazdı.
        /// </summary>
        GroundMark PlaceGroundMark(float menzil, float yarıçap, float gecikme,
                                   float hasar, float hasat, Vector3? yer = null)
        {
            if (Y(SilahYükseltme.HızlıTapu)) gecikme *= 0.7f;    // İPOTEK yükseltmeleri (Faz 29)
            if (Y(SilahYükseltme.GenişArsa)) yarıçap *= 1.3f;
            // Combo bir yer verdiyse oraya (menzil sınırı yok — combo'nun hedefi zaten yakında vuruldu).
            Vector3 nokta;
            if (yer.HasValue) { Vector3 d = yer.Value - transform.position; d.y = 0f; nokta = transform.position + d; }
            else nokta = Nişan(menzil);

            // ⚠️ ODA KÖKÜNE bağlanıyor: oda sökülünce patlamadan gidiyor (`TearDown`
            // sayacı zaten o varsayımla sıfırlıyor). Koşu köküne bağlıydı; yeni odaya
            // taşınıp orada patlıyor ve "aynı anda tek ipotek" kuralını deliyordu.
            // Oda yoksa (öz-denetim) eski yer.
            return GroundMark.SpawnLien(_t, nokta, yarıçap, gecikme, hasar, hasat, OdaKökü);
        }

        /// <summary>Nişan noktası, MENZİLE KIRPILMIŞ — İPOTEK işareti ve KİRA kulesi (Faz 32) tek yerden.</summary>
        Vector3 Nişan(float menzil)
        {
            Vector3 hedef = CursorGround(out Vector3 nokta) ? nokta : transform.position + transform.forward * menzil;
            Vector3 delta = hedef - transform.position;
            delta.y = 0f;                                  // tuzak 2: y yatay hesaba karışmasın
            if (delta.magnitude > menzil) delta = delta.normalized * menzil;
            return transform.position + delta;
        }

        /// <summary>
        /// İPOTEK YAĞMURU'nun bir damlası: alandaki canlı düşmanlardan RASTGELE birinin durduğu
        /// yere ipotek (aynı hedefe yığılmasın diye rastgele). Alanda kimse yoksa alana rastgele.
        /// İşaret normal ipotek: gecikme boyunca kaçabilir — silahın sorusu bu.
        /// </summary>
        void YağmurDamlası(WeaponSpecial sp)
        {
            float alan = Weapon.Range(_t) * sp.RangeMul;
            Enemy hedef = null;
            int aday = 0;
            foreach (var e in Enemy.All)
            {
                if (e == null) continue;
                Vector3 d = e.transform.position - transform.position;
                d.y = 0f;                                   // tuzak 2
                if (d.magnitude > alan) continue;
                if (Random.Range(0, ++aday) == 0) hedef = e;   // eşit olasılıkla biri
            }
            Vector3 yer = hedef != null
                ? hedef.transform.position
                : transform.position + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(0f, alan);
            yer.y = transform.position.y;

            var oda = RoomManager.Instance != null ? RoomManager.Instance.RoomRoot : null;
            GroundMark.SpawnLien(_t, yer, sp.GroundRadius * (Y(SilahYükseltme.GenişArsa) ? 1.3f : 1f),
                                 sp.GroundDelay * (Y(SilahYükseltme.HızlıTapu) ? 0.7f : 1f),
                                 WeaponDamage() * sp.DamageMul, Weapon.HarvestMultiplier * sp.HarvestMul,
                                 oda != null ? oda : transform.parent);
            SwingsSpawned++;
        }

        /// <summary>
        /// HACİZ · UZUN VADE: hasar hedefin UZAKLIĞIYLA artıyor.
        ///
        /// ⚠️ Ölçü YÜZEYE, merkeze değil — bu projede menzilin ve ölü bölgenin
        /// ölçüsü de öyle. Merkeze ölçseydim iri düşman aynı mesafede daha çok
        /// hasar yerdi ve sebebi görünmezdi (ders 9: aynı kavram, aynı ölçüm).
        /// </summary>
        float MesafeliHasar(float damage, Enemy e, WeaponSpecial special)
        {
            if (Weapon.Saldırı == Biçim.Arbitraj && e != null)                              // ARBİTRAJ (Faz 32)
            {
                float k = KonumÇarpanı(e);
                if (k >= _t.arbitrajBackMul) ArkadanVuruş++;
                damage *= k;
            }
            if (special == null || special.DamagePerDistance <= 0f || e == null) return damage;
            Vector3 to = e.transform.position - transform.position;
            to.y = 0f;
            float yüzey = Mathf.Max(0f, to.magnitude - e.Radius);
            return damage * (1f + yüzey * special.DamagePerDistance);
        }

        /// <summary>
        /// Darbe yemek saniye götürür. Dash i-frame'i içindeyken geçmez.
        /// Kırmızı parlama + geri itilme, "yedim" anını okunur kılıyor —
        /// ilk playtest'te saatin azalması dışında hiçbir işaret yoktu.
        /// </summary>
        /// <returns>
        /// GERÇEKTEN inen bedel (dokunulmazsan 0).
        /// ⚠️ Nemesis defteri bunu kullanıyor: "vurdum, demek ki aldı" varsayımı
        /// dash i-frame'inde yanlış olurdu ve boss kaçırdığı vuruşları da hesabına
        /// yazardı. Ölçüm tahmin değil, gerçekten inen miktar.
        /// </returns>
        /// <param name="saldıran">Vuran boss — nemesis hesabı için. Sıradan düşman ve
        /// hesapsız vuruşlar (yer işareti sahipsizse) null verir.</param>
        public float TakeHit(float secondsCost, Vector3 fromPosition, Enemy saldıran = null)
        {
            if (Invulnerable) return 0f;

            // Koşu bittiyse (ölüm ya da zafer) darbe yok — ne bedel ne geri bildirim.
            var clock = Clock.Instance;
            if (clock != null && (clock.Dead || clock.Won)) return 0f;

            // KARŞILIK (Faz 31): pencere içinde gelen darbe saatten düşmüyor, birikiyor. Nemesis
            // hesabına da yazılmıyor — inmedi. İADE: yarısı saate (doyma tavanına tabi).
            if (Karşılıyor)
            {
                _biriken += secondsCost * (Y(SilahYükseltme.TamKarşılık) ? 1.5f : 1f);
                if (Y(SilahYükseltme.İade) && clock != null) clock.Add(secondsCost * 0.5f * clock.HasatDoyması);
                KarşılananSayısı++;
                Arc.Spawn(transform.position, transform.rotation, 1.3f, 360f, new Color(0.95f, 0.95f, 0.6f, 0.8f), 0.2f,
                          scaleFrom: 0.5f, scaleTo: 1.4f);
                Sfx.Hit();
                return 0f;
            }

            // REHİN (Faz 31): önden gelen vuruşu rehine yer (KALKAN: yandan da). Bedelinin katı hasar
            // olarak ona iner, sana hasat yok.
            if (Tutuyor && saldıran != _rehine)
            {
                Vector3 gelen = fromPosition - transform.position; gelen.y = 0f;
                float eşik = Y(SilahYükseltme.Kalkan) ? 0f : 0.7071f;      // cos 45°
                if (gelen.sqrMagnitude < 0.0001f || Vector3.Dot(transform.forward, gelen.normalized) >= eşik)
                {
                    _rehine.TakeDamage(secondsCost * _t.rehinShieldDamagePerSecond, 0f);
                    KalkanSayısı++;
                    if (!Tutuyor) RehineyiBırak();
                    Sfx.Hit();
                    return 0f;
                }
            }

            // ⚠️ GERÇEKTEN İNEN miktar ölçülüyor, istenen değil. Saatinde 2 saniye
            // kalmışken 4 saniyelik darbe yersen boss 4 değil 2 almıştır — nemesis
            // defterine 4 yazmak, olmayan bir alacağı hesabına geçirmek olurdu.
            // (Projenin kuralı: ÖZELLİK değil ETKİ. Burada da geçerli.)
            // ⚠️ Saat farkıyla ölçülmüyor artık: bu darbe öldürüp diriltirse (ÖLÜM
            // ALDATMA) saat YÜKSELMİŞ olarak dönüyor ve fark EKSİ çıkıyordu.
            float inen = clock != null
                       ? Mathf.Min(Mathf.Max(0f, secondsCost), Mathf.Max(0f, clock.Seconds))
                       : secondsCost;

            // ⚠️ Hesaba saat düşmeden ÖNCE yaz: saat sıfırlanırsa ölüm sırası
            // (RevivalSystem) AYNI çağrının içinde defteri diske yazıyor.
            // Unity null'ı için `?.` değil açık kontrol: saldıran yok edilmiş olabilir.
            if (saldıran != null) saldıran.HesabınaYaz(inen);

            if (clock != null) clock.Spend(secondsCost, fromHit: true);   // oda hesabında AYRI sayılıyor

            // Darbe koşuyu yeniden başlattıysa (ÇAĞRI) bu nesne eski koşuya ait:
            // acı sesi ve hit-stop yeni koşunun ilk karesine taşmasın.
            if (clock != null && Clock.Instance != clock) return inen;

            HitStop.Freeze(_t.hitStopDuration * 1.5f);
            IsoCamera.Shake(_t.hurtShake);

            Sfx.Hurt();
            _hurtFlash = _t.playerHurtFlashDuration;
            Vector3 away = transform.position - fromPosition;
            away.y = 0f;
            if (away.sqrMagnitude > 0.0001f) _knockback = away.normalized * _t.playerKnockback;
            Paint();

            return inen;
        }

        /// <summary>
        /// KEFİL'in imzası: oyuncuyu KENDİNE çeker. "Borcu garantiye aldı" —
        /// seni bıraktığı yerde bırakmıyor, yanına alıyor (ve orada emiyor).
        ///
        /// ⚠️ Geri itilmeyle AYNI kanalı kullanıyor (`_knockback`), ters yönde:
        /// ikinci bir konum sürücüsü yazmak, arena sınırı ve dash'le iki ayrı
        /// yerde uzlaşmak demekti. Aynı kanal = aynı sönümleme, aynı sınır.
        ///
        /// ⚠️ Dokunulmazken çekmiyor: dash hem darbeden hem ÇEKİMDEN kurtarmalı,
        /// yoksa "dash'le kaçtım ama yine de içeri alındım" olur ve dash'in
        /// vaadi delinir.
        /// </summary>
        public void Yank(Vector3 toward, float force)
        {
            if (Invulnerable) return;
            Vector3 d = toward - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) _knockback = d.normalized * force;
        }

        /// <summary>
        /// ⚠️ `dt` DIŞARIDAN geliyor (CLAUDE.md tuzak 3). Editör modunda
        /// `Time.deltaTime` 0 olduğu için içeriden okusaydı savrulma ve ÇEKİM
        /// yalnızca "alan dolu mu" diye sorulabilirdi — ETKİ değil ÖZELLİK.
        /// </summary>
        public void TickHurt(float dt)
        {
            if (_hurtFlash > 0f)
            {
                _hurtFlash -= Time.unscaledDeltaTime;   // hit-stop sırasında da sönsün
                if (_hurtFlash <= 0f) Paint();
            }

            if (_knockback.sqrMagnitude > 0.0001f)
            {
                transform.position += _knockback * dt;
                _knockback = Vector3.Lerp(_knockback, Vector3.zero, 12f * dt);
                if (_knockback.sqrMagnitude < 0.0001f) _knockback = Vector3.zero;
            }
        }

        void Paint()
        {
            if (_renderer == null) return;
            _renderer.material.color = _hurtFlash > 0f ? HurtColor
                                     : Görünmez ? new Color(_baseColor.r * 0.3f, _baseColor.g * 0.3f, _baseColor.b * 0.35f, _baseColor.a)
                                     : _baseColor;
        }

        /// <summary>Ayar yaparken vuruş alanı görünsün — Hierarchy'den Player'ı seç.</summary>
        void OnDrawGizmosSelected()
        {
            if (_t == null) return;
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.9f);
            float half = _t.attackAngle * 0.5f;
            for (float a = -half; a <= half; a += 10f)
                Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, a, 0f) * transform.forward * _t.attackRange);
        }
    }
}
