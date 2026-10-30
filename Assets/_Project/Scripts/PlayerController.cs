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
        float SavurmaSüresi => Weapon.Duration(_t) * (Y(SilahYükseltme.GenişPencere) ? 1.6f : 1f)    // KARŞILIK
                                                   / (Weapon.Saldırı == Biçim.İmza ? AteşHızı : 1f);   // ÇİFT İMZA: bir mermi
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
            float k = Y(SilahYükseltme.KısaSüpürme) || Y(SilahYükseltme.KısaİCRA) || Y(SilahYükseltme.TopluİmzaArtı) ? 0.7f : 1f;
            return Clock.Instance != null ? k * Clock.Instance.SabırÇarpanı : k;
        }

        // ── COMBO (Faz 30, Enes: "hızlı silah geçişleri ile combo… abuselanmamalı… gelişip yeni etki") ──
        // Zincir: İSABET eden son adımlar (silah, an, yer). İPOTEK'in koyduğu işaret de bir adım.
        // Zaman `_comboAn` — Update ilerletiyor, öz-denetim `TickCombo` ile (editörde deltaTime 0).
        readonly List<(int silah, float an, Vector3 yer, Enemy hedef)> _zincir = new List<(int silah, float an, Vector3 yer, Enemy hedef)>();
        readonly float[] _comboBekleme = new float[Combo.Sayı];
        float _comboAn;
        Vector3 _isabetToplam, _isabetYeri;

        /// <summary>Tetiklenen combo sayısı — öz-denetim ve otopilot sayıyor.</summary>
        public int ComboSayısı { get; private set; }
        /// <summary>Bu combo'nun kalan beklemesi — otopilot "BU combo mu tetiklendi"yi buradan okuyor (Faz 42: zincirde önce
        /// başka bir ikili de tetiklenebilir, ALACAK DEVRİ → İCRA TAKİBİ; combo sayısı yetmiyor).</summary>
        public float ComboBeklemesi(ComboTür c) => _comboBekleme[(int)c];

        public void TickCombo(float dt)
        {
            _comboAn += dt;
            for (int i = 0; i < _comboBekleme.Length; i++) _comboBekleme[i] -= dt;
        }


        /// <summary>Bir adım yaz: aynı silah üst üste sayılmaz (ucu tazelenir), pencere geçtiyse zincir kopar.</summary>
        /// <summary>Zincire yazılan adım sayısı — otopilot "vuruş indi, şimdi geç" anını buradan okuyor.</summary>
        public int AdımSayısı { get; private set; }

        void ZincireYaz(int silah, Vector3 yer, Enemy hedef = null)
        {
            AdımSayısı++;
            _sonAdımHedefi = hedef;
            if (_zincir.Count > 0)
            {
                var son = _zincir[_zincir.Count - 1];
                if (_comboAn - son.an > _t.comboWindow) _zincir.Clear();
                else if (son.silah == silah) { _zincir[_zincir.Count - 1] = (silah, _comboAn, yer, hedef); return; }
            }
            _zincir.Add((silah, _comboAn, yer, hedef));
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
        /// <summary>Zincirde kaç adım geride kimin vurulduğu (Faz 42: İSTİHKAK · PROTESTOLU SENET iki adım geriye bakıyor). Ölmüş olabilir.</summary>
        Enemy AdımHedefi(int geri) => _zincir[_zincir.Count - geri].hedef;

        void ComboTetikle(ComboTür c)
        {
            _comboBekleme[(int)c] = _t.comboCooldown;
            // Faz 42: ikili bir üçlünün ilk iki adımıysa zincir silinmez, bu vuruş adım olarak yazılır — üçlüye devam
            // (ALACAK DEVRİ → İCRA TAKİBİ). ⚠️ Adım `_isabetYeri`'nden: şimdilik bu ikililer koni vuruşuyla bitiyor,
            // Strike isabeti önce yazıyor. Faz42SelfCheck bu ikilileri sayıyor — yenisi gelirse kapı uyarır.
            if (Combo.ÜçlüyeDevam(c, RoomManager.AktifBölge)) ZincireYaz(_weaponIndex, _isabetYeri, _sonVurulan);
            else _zincir.Clear();
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
            ZincireYaz(_weaponIndex, _isabetYeri, _sonVurulan);
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

        // ── FAZ 31: REHİN · ÇİFT İMZA (Faz 43'e dek ÇEK) · KARŞILIK ───────────────────────────────────
        // Davranış silahın BİÇİMİNDEN (`Weapon.Saldırı`), adından değil. İsabet yine tek yerden
        // (`InHitCone` / `StrikeCone`); yalnızca koni olmayan vuruşlar (fırlatma, takas, mermi) kendi
        // hedefini seçiyor.
        static readonly ComboTür[] Bitirenler = { // Faz 42 — üçlüler ÖNCE: son iki adımları bir ikilinin sırası olabilir
                                                  // (TEMİNATLI TAHVİL ⊃ VADE FARKI, SENETLİ REHİN ⊃ CİRO, İCRA TAKİBİ ⊃ AMME ALACAĞI …)
                                                  ComboTür.İcraTakibi, ComboTür.SenetliRehin, ComboTür.TeminatlıTahvil,
                                                  ComboTür.OrtaklığınTasfiyesi, ComboTür.KiraKontratı,
                                                  ComboTür.İpotekZinciri, ComboTür.İflasErtelemesi, ComboTür.VadeSonuTahsilatı,   // Parti 2
                                                  ComboTür.TopluİCRA, ComboTür.BileşikFaiz, ComboTür.KarşıRehin, ComboTür.OrtakKarşılık,   // Parti 3
                                                  ComboTür.TapuKarşılığı, ComboTür.Arkadanİcra, ComboTür.VadeliKarşılık, ComboTür.GizliOrtaklık,   // Parti 4
                                                  ComboTür.Temerrüt, ComboTür.KarşıİCRA, ComboTür.SenetHaczi, ComboTür.SilipSüpür,
                                                  ComboTür.AçığaSatış, ComboTür.VadeFarkı,
                                                  // Faz 33c — koni vuruşuyla biten
                                                  ComboTür.Takip, ComboTür.Kefalet, ComboTür.Temettü, ComboTür.Konsorsiyum,
                                                  ComboTür.Konkordato, ComboTür.KiraGeliri, ComboTür.Sürü,
                                                  // Faz 42 Parti 1
                                                  ComboTür.Cebriİcra, ComboTür.TopluBorç, ComboTür.AlacakDevri, ComboTür.Tapuİptali,
                                                  ComboTür.AmmeAlacağı, ComboTür.İflasMasası, ComboTür.Ciro, ComboTür.KarşılıklıTahsil,
                                                  ComboTür.VadeliHaciz, ComboTür.ZincirlemeCiro, ComboTür.MüşterekKusur,
                                                  ComboTür.ArkadanDolanım, ComboTür.VadesiGelmişPozisyon,
                                                  // Faz 42 Parti 2 (İPOTEKLİ/KİRA/İMZA/REHİN bitirişleri kendi yerlerinde)
                                                  ComboTür.AcizVesikası, ComboTür.HacizMemuru, ComboTür.TapuTevhidi, ComboTür.İflasDairesi,
                                                  ComboTür.MüşterekRehin, ComboTür.İbraz, ComboTür.Hacizİştiraki, ComboTür.Hileliİflas,
                                                  // Faz 42 Parti 3 (TAHVİL'in basılı tutması, kule, ipotek, İMZA, REHİN'in yakalaması kendi yerlerinde)
                                                  ComboTür.FaizYükü, ComboTür.İpotekliİflas, ComboTür.ArsaSpekülasyonu, ComboTür.RehinKarşılığı,
                                                  ComboTür.Karşılıksızİflas, ComboTür.KarşıHamle, ComboTür.TahvilOrtaklığı, ComboTür.OrtakBorç,
                                                  ComboTür.İçİşlem, ComboTür.GizliHaciz, ComboTür.SüpürgeKulesi,
                                                  // Faz 42 Parti 4 (TAHVİL, kule, ipotek, İMZA, REHİN yine kendi yerlerinde)
                                                  ComboTür.SürüdenAyır, ComboTür.HacizOrtaklığı, ComboTür.TapuKredisi, ComboTür.İflasOrtaklığı,
                                                  ComboTür.RehinliKredi, ComboTür.İmzalıKredi, ComboTür.Karşılıklıİmza, ComboTür.KarşıHaciz,
                                                  ComboTür.KuleHaczi };
        /// <summary>REHİN'in uzaktan yakaladığı combo'lar — kaç adım gerideki hedefi (Faz 42).</summary>
        static readonly (ComboTür c, int geri)[] UzakYakalayanlar =
            { (ComboTür.İstihkak, 2), (ComboTür.ProtestoluSenet, 2), (ComboTür.MalKaçırma, 1), (ComboTür.Kaçırma, 1), (ComboTür.BorçEsareti, 1) };
        /// <summary>Kule kurarken biten satın alınırlar (Faz 42 Parti 3–4) — üçlüler önce: İMZA → KİRA, REHİN → KİRA onlarda da son.</summary>
        static readonly ComboTür[] KuleBitirenleri =
            { ComboTür.ZincirlemeTahsil, ComboTür.SenetliKira, ComboTür.KiracıSürüsü, ComboTür.TasfiyeKulesi, ComboTür.İşgal, ComboTür.KiraSenedi,
              ComboTür.FaizliKira, ComboTür.KiraHaczi, ComboTür.KiraKarşılığı, ComboTür.GizliKule };

        /// <summary>Faz 42: bazı bitirişler bir duruma bağlı — yoksa combo olmaz, vuruş zincire adım olarak yazılır.</summary>
        bool Şart(ComboTür c) => c switch
        {
            ComboTür.ArkadanDolanım or ComboTür.Hileliİflas or ComboTür.GizliHaciz or ComboTür.Kaçırma
                or ComboTür.Gizliİpotek or ComboTür.GizliKule
                => _comboAn - _sonArkadanAn <= _t.comboWindow,                                                  // ARBİTRAJ adımı arkadandı
            ComboTür.VadesiGelmişPozisyon => _sonKademe >= 2,
            ComboTür.MüşterekKusur or ComboTür.OrtaklığınTasfiyesi or ComboTür.Hacizİştiraki
                or ComboTür.OrtakBorç or ComboTür.İçİşlem or ComboTür.OrtakKarşılık => SonBağ != null,
            ComboTür.ZincirlemeCiro or ComboTür.HacizMemuru or ComboTür.KarşıHamle
                or ComboTür.HacizOrtaklığı or ComboTür.İflasOrtaklığı or ComboTür.KiraHaczi => _sonAdımHedefi != null && Enemy.All.Contains(_sonAdımHedefi),
            ComboTür.TapuTevhidi or ComboTür.İpotekliİflas or ComboTür.ArsaSpekülasyonu or ComboTür.TapuKredisi => İpotektekiler().Count > 0,
            ComboTür.SüpürgeKulesi or ComboTür.KuleHaczi => Kule != null,
            ComboTür.SürüdenAyır => _koniVurulanlar.Exists(e => e != null && Enemy.All.Contains(e)),
            ComboTür.KiraKontratı => Kule != null,
            _ => true,
        };
        // Faz 33c combo'larının ihtiyaç duyduğu son adım bilgileri
        Enemy _sonAdımHedefi;                                   // ÖNCELİK: zincirin son adımının vurduğu
        readonly List<Enemy> _fırlatılanlar = new List<Enemy>();   // MÜŞTEREK REHİN (Faz 42): son fırlatmanın rehinesi, çarptığı, sektiği
        Enemy _protestolu;                                      // PROTESTOLU SENET: fırlatılınca ×2
        float _sonBiriken;                                      // TEMİNAT AKÇESİ: son inen birikim (sn)
        // Faz 42 Parti 3
        float _sonFırlatma;                                     // REHİN KARŞILIĞI: son fırlatmanın hasarı
        Enemy _alacaklı;                                        // VADELİ ALACAK: basılı tuttukça yavaşlayan
        KiraKulesi _tahsilKulesi;                               // ZİNCİRLEME TAHSİL: İMZA'nın isabeti bu kulenin ömrünü uzatır
        int _faizYükü, _faizYüküOdası = -1;                     // FAİZ YÜKÜ: bu odada kaçıncı
        // Faz 42 Parti 4
        Enemy _esir;                                            // BORÇ ESARETİ: tuttukça her sn bedel ondan sana
        float _esaret;
        Enemy _vadeliRehine;                                    // VADELİ REHİN: fırlatılınca kademe çarpanıyla
        GroundMark _tapuVadesi;                                 // TAPU VADESİ: patlarsa basılı tutmaya +1 kademe
        MusterekBag _ortakVadeBağı;                             // ORTAK VADE: yaşadıkça basılı tutma bedelsiz
        readonly List<Enemy> _imzaVurdukları = new List<Enemy>();  // GİZLİ ORTAKLIK: bu İMZA adımının vurdukları
        readonly List<Enemy> _koniVurulanlar = new List<Enemy>();   // SÜRÜ
        readonly float[] _sonHasar = new float[Weapon.All.Length];  // KONKORDATO: silahın son isabetli vuruşunun hasarı
        float _sonBirikenHasarı, _sonArkadanAn = -99f;          // MUKABELE · SPEKÜLASYON
        int _sonKademe;                                         // TEMETTÜ
        KiraKulesi _kiraGeliri;                                 // KİRA GELİRİ
        KiraKulesi KiraGeliriKulesi => _kiraGeliri != null && !_kiraGeliri.Bitti ? _kiraGeliri : null;
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
            _esir = null;                                                         // BORÇ ESARETİ tutarken sürer
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
            Tut(en);
            return true;
        }

        /// <summary>Nerede olursa olsun ele çekilebilir mi (ÖNCELİK · İSTİHKAK · PROTESTOLU SENET): canlı, boss değil, tutulmuyor.</summary>
        static bool Çekilir(Enemy h) => h != null && Enemy.All.Contains(h) && !h.IsBoss && h.Tutan == null;
        void ElineÇek(Enemy h) { Vector3 el = RehinYeri; el.y = h.transform.position.y; h.transform.position = el; }

        /// <summary>Yakalamanın kendisi (YAKALA ve ÖNCELİK): yarım vuruş; boştaysa ölür, değilse rehine.</summary>
        void Tut(Enemy en)
        {
            bool boştu = en.Hollow;
            en.TakeDamage(WeaponDamage() * 0.5f, Weapon.HarvestMultiplier);
            VuruşHissi(0.6f);
            if (!boştu && Enemy.All.Contains(en)) { _rehine = en; en.RehinAl(this); }
        }

        void RehinVuruşu()
        {
            if (!Tutuyor)                                                         // yakalamak combo adımı DEĞİL
            {
                // Faz 42: uzaktan yakalayanlar — İSTİHKAK · PROTESTOLU SENET iki adım gerideki hedefi, MAL KAÇIRMA · KAÇIRMA bir adım
                foreach (var (uzun, geri) in UzakYakalayanlar)
                {
                    if (!ComboHazır(uzun) || !Şart(uzun) || !(AdımHedefi(geri) is Enemy u) || !Çekilir(u)) continue;
                    ElineÇek(u);
                    bool uzunArtı = Combo.Artı(uzun);
                    var saat = Clock.Instance;
                    switch (uzun)
                    {
                        case ComboTür.İstihkak:
                            u.TakeDamage(_sonBirikenHasarı, Weapon.HarvestMultiplier);          // KARŞILIK'ın birikimi ona
                            if (uzunArtı) _specialCd[Kadro.REHİN] = 0f;                         // İSTİRDAT
                            break;
                        case ComboTür.ProtestoluSenet:
                            _protestolu = u;                                                     // fırlatılınca ×2
                            if (uzunArtı) _specialCd[Kadro.İMZA] = 0f;                          // ŞERH
                            break;
                        case ComboTür.MalKaçırma:
                            if (saat != null) saat.AkışıBük(3f, 0f);                           // 3 sn saat akmaz
                            if (uzunArtı) _specialCd[Kadro.İFLAS] = 0f;                         // MAL GİZLEME
                            break;
                        case ComboTür.Kaçırma:
                            if (saat != null) saat.AkışıBük(4f, 0.5f);                         // 4 sn saat yarı akar
                            if (uzunArtı) _specialCd[Kadro.REHİN] = 0f;                         // FİDYE
                            break;
                        case ComboTür.BorçEsareti:                                               // Parti 4
                            if (uzunArtı) _specialCd[Kadro.FAİZLİ] = 0f;                        // BORÇ SENEDİ
                            break;
                    }
                    if (Enemy.All.Contains(u)) Tut(u);
                    if (uzun == ComboTür.BorçEsareti) { _esir = _rehine; _esaret = 0f; }   // Tut'tan sonra: rehine o
                    ComboTetikle(uzun);
                    return;
                }
                var h = _sonAdımHedefi;                                           // ÖNCELİK (Faz 33c): HACİZ'in vurduğunu çekip yakala
                if (ComboHazır(ComboTür.Öncelik) && Çekilir(h))
                {
                    ElineÇek(h);
                    Tut(h);
                    if (Combo.Artı(ComboTür.Öncelik)) _specialCd[Kadro.HACİZ] = 0f;     // İHTİYATİ HACİZ
                    ComboTetikle(ComboTür.Öncelik);
                    return;
                }
                if (ComboHazır(ComboTür.VadeliRehin) && Yakala(SavurmaMenzili * Mathf.Max(1, _sonKademe), SavurmaAçısı))   // VADELİ REHİN (Parti 4)
                {
                    _vadeliRehine = _rehine;                                      // fırlatılınca kademe çarpanıyla
                    if (Combo.Artı(ComboTür.VadeliRehin)) _specialCd[Kadro.TAHVİL] = 0f;   // REHİN İTFASI
                    ComboTetikle(ComboTür.VadeliRehin);
                    return;
                }
                if (ComboHazır(ComboTür.KiracıRehin) && Kule is KiraKulesi kiraKulesi && Çekilir(kiraKulesi.SonVurulan))   // KİRACI REHİN (Parti 4)
                {
                    var kiracıRehin = kiraKulesi.SonVurulan;
                    ElineÇek(kiracıRehin);
                    Tut(kiracıRehin);
                    if (Combo.Artı(ComboTür.KiracıRehin)) _specialCd[Kadro.KİRA] = 0f;     // KİRA REHNİ
                    ComboTetikle(ComboTür.KiracıRehin);
                    return;
                }
                Yakala(SavurmaMenzili, SavurmaAçısı);
                return;
            }

            // FIRLAT: yön boyunca rehinenin genişliğinde bir hat; ilk çarptığına sert vurur, rehine de yer.
            var r = _rehine;
            RehineyiBırak();
            Vector3 ileri = transform.forward;
            float menzil = _t.rehinThrowRange;
            Enemy hedef = HattakiİlkDüşman(transform.position, ileri, menzil, r, r.Radius);
            float hasar = WeaponDamage() * _t.rehinThrowDamageMul * (Y(SilahYükseltme.AğırRehin) ? 1.6f : 1f)
                        * (r == _protestolu ? 2f : 1f)                                   // PROTESTOLU SENET (Faz 42)
                        * (r == _vadeliRehine ? Mathf.Max(1f, _sonVadeÇarpanı) : 1f);   // VADELİ REHİN (Parti 4)
            _protestolu = null; _vadeliRehine = null;
            _sonFırlatma = hasar;                                                 // REHİN KARŞILIĞI
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
            _fırlatılanlar.Clear(); _fırlatılanlar.Add(r);                        // MÜŞTEREK REHİN (Faz 42)
            if (hedef != null) _fırlatılanlar.Add(hedef);
            if (hedef != null && Y(SilahYükseltme.Domino) && Sıçrat(hasar * 0.7f, Weapon.HarvestMultiplier, hedef, r) is Enemy sekti)   // DOMİNO
                _fırlatılanlar.Add(sekti);
            VuruşHissi(1.4f);
            İsabetSonrası(hits);
        }

        /// <summary>REHİN · TEHDİT (Faz 33): rehine tutarken önündeki düşman yeni saldırı başlatamaz (`Enemy`).</summary>
        public bool Tehdit(Enemy e)
        {
            if (e == null || e == _rehine || !Tutuyor || !Y(SilahYükseltme.Tehdit)) return false;
            Vector3 d = e.transform.position - transform.position; d.y = 0f;
            return d.sqrMagnitude < 0.0001f || Vector3.Dot(transform.forward, d.normalized) >= 0.7071f;
        }

        float _sorgu;
        /// <summary>REHİN · SORGU (Faz 33): tutarken rehinenin havuzu akar, sana hasat olur. ⚠️ dt DIŞARIDAN; yarım saniyelik dilimler.</summary>
        public void TickRehin(float dt)
        {
            if (Tutuyor && _rehine == _esir && (_esaret += dt) >= 1f)            // BORÇ ESARETİ (Parti 4): her sn bedel kadar ondan sana
            {
                _esaret -= 1f;
                _rehine.TakeDamage(_t.faizliSwingCost, 1f);
                if (!Tutuyor) { RehineyiBırak(); return; }
            }
            if (!Tutuyor || !Y(SilahYükseltme.Sorgu)) { _sorgu = 0f; return; }
            _sorgu += dt;
            if (_sorgu < 0.5f) return;
            _rehine.TakeDamage(_sorgu, Weapon.HarvestMultiplier);
            _sorgu = 0f;
            if (!Tutuyor) RehineyiBırak();
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

        // ── FAZ 43: ÇİFT İMZA (ÇEK'in yerine) ────────────────────────────────
        // Mermi anında iner (uçuş yok). Combo İMZA'yla bitiyorsa ilk mermide tetiklenir, etkisi 1 sn'lik SERİ'ye biner —
        // ÇEK'te "o çek"in taşıdığı her şey (çarpan, ek, iade, tam pay, sekme, susturma) burada.
        sealed class İmzaSerisi { public float Bitiş, Çarpan = 1f, Hız = 1f, Ek; public bool TamPay, Seker, Sustur; public int İade; }
        İmzaSerisi _seri;
        /// <summary>Süren seri (yoksa null) — zaman `_comboAn`'dan: öz-denetim `TickCombo` ile ilerletiyor.</summary>
        İmzaSerisi Seri => _seri != null && _comboAn <= _seri.Bitiş ? _seri : null;
        İmzaSerisi YeniSeri() => _seri = new İmzaSerisi { Bitiş = _comboAn + 1f };
        bool _sağTabanca;
        Enemy _onayBekleyen;                                    // ONAY: önceki mermi bunu vurdu
        float _onayAn = -99f;
        int _imzaİsabeti;                                       // bu İMZA adımının isabeti (İBRAZ · SENET İSKONTOSU · KARŞILIKLI İMZA)

        /// <summary>İMZA'nın ateş hızı çarpanı: SERİ EL · VADELİ SENET'in serisi.</summary>
        float AteşHızı => (Y(SilahYükseltme.SeriEl) ? 1.3f : 1f) * (Seri is İmzaSerisi s ? s.Hız : 1f);

        /// <summary>Bir hatta (yön, menzil) ilk değen düşman — REHİN'in fırlatması ve İMZA'nın mermisi. `pay`: hattın yarı genişliği.</summary>
        static Enemy HattakiİlkDüşman(Vector3 baş, Vector3 yön, float menzil, Enemy hariç, float pay)
        {
            Enemy en = null; float enYakın = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || e == hariç || e.Dormant) continue;
                Vector3 to = e.transform.position - baş; to.y = 0f;                // tuzak 2
                float boy = Vector3.Dot(to, yön);
                if (boy <= 0f || boy - e.Radius > menzil) continue;
                if ((to - yön * boy).magnitude > e.Radius + pay) continue;
                if (boy < enYakın) { enYakın = boy; en = e; }
            }
            return en;
        }

        /// <summary>YÖNELİM: hatta kimse yoksa nişanın 10° yakınındaki en yakın düşman.</summary>
        static Enemy YönelimHedefi(Vector3 baş, Vector3 yön, float menzil)
        {
            Enemy en = null; float enYakın = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dormant) continue;
                Vector3 to = e.transform.position - baş; to.y = 0f;
                if (to.magnitude - e.Radius > menzil || Vector3.Angle(yön, to) > 10f) continue;
                if (to.magnitude < enYakın) { enYakın = to.magnitude; en = e; }
            }
            return en;
        }

        /// <summary>Tek mermi: iki tabanca sırayla, yönde ilk değdiğine anında iner. İz çizgisi ıskada da görünür. Vurduğu (yoksa null).</summary>
        Enemy Mermi(Vector3 yön, float çarpan)
        {
            yön.y = 0f;
            yön = yön.sqrMagnitude > 0.0001f ? yön.normalized : transform.forward;
            _sağTabanca = !_sağTabanca;
            Vector3 baş = transform.position + Vector3.Cross(Vector3.up, yön) * (_sağTabanca ? 0.3f : -0.3f);
            float menzil = Weapon.Range(_t) * (Y(SilahYükseltme.UzunNamlu) ? 1.3f : 1f);     // UZUN NAMLU
            var hedef = HattakiİlkDüşman(baş, yön, menzil, null, 0.25f);
            if (hedef == null && Y(SilahYükseltme.Yönelim)) hedef = YönelimHedefi(baş, yön, menzil);   // YÖNELİM
            Mermiİzi(baş, hedef != null ? hedef.transform.position : baş + yön * menzil, yön);
            SwingsSpawned++;
            if (hedef == null) return null;
            Vector3 hp = hedef.transform.position;                                  // ÖNCE: vuruş öldürebilir
            float hasar = WeaponDamage() * _t.imzaBulletMul * çarpan;
            MermiVur(hedef, hasar, true);
            if (Y(SilahYükseltme.DeliciMermi)                                         // DELİCİ MERMİ: arkadakine yarım
                && HattakiİlkDüşman(hp, yön, Mathf.Max(0f, menzil - Vector3.Distance(baş, hp)), hedef, 0.25f) is Enemy arka)
                MermiVur(arka, hasar * 0.5f, false);
            return hedef;
        }

        /// <summary>Merminin iz çizgisi — ıskada da görünür (`yedekYön`: baş ile son üst üsteyse).</summary>
        void Mermiİzi(Vector3 baş, Vector3 son, Vector3 yedekYön)
        {
            Vector3 iz = son - baş; iz.y = 0f;
            Arc.Spawn(baş, Quaternion.LookRotation(iz.sqrMagnitude > 0.0001f ? iz : yedekYön), iz.magnitude, 3f, Weapon.Tint, 0.08f);
        }

        /// <summary>Merminin isabeti: ONAY, seri, ONAY HAKKI, sekme (PROTESTO · İFLAS SENEDİ); adım İMZA'nın.
        /// `ana` değilse (deliş, combo'nun ek mermisi) ONAY'a sayılmaz.</summary>
        void MermiVur(Enemy e, float hasar, bool ana)
        {
            bool onay = ana && e == _onayBekleyen && _comboAn - _onayAn <= 0.5f;   // ONAY: aynı düşmana peş peşe ikinci mermi
            if (ana) { _onayBekleyen = onay ? null : e; _onayAn = _comboAn; }
            if (onay) hasar *= _t.imzaOnayMul;
            var seri = Seri;
            if (seri != null) hasar = hasar * seri.Çarpan + seri.Ek;
            Vector3 yer = e.transform.position;                                     // ÖNCE: vuruş öldürebilir
            bool boştu = e.Hollow;
            if (seri != null && seri.Sustur) e.Sustur(1f);                          // TEDBİR — vuruştan önce
            bool tam = seri != null && seri.TamPay && e.Bağ != null;                // SENET ORTAKLIĞI
            if (tam) MusterekBag.TamPay = true;
            e.TakeDamage(hasar, Weapon.HarvestMultiplier);
            if (tam) MusterekBag.TamPay = false;
            var saat = Clock.Instance;
            if (onay && boştu && saat != null && Y(SilahYükseltme.OnayHakkı)) saat.Add(0.5f * saat.HasatDoyması);   // ONAY HAKKI
            if (seri != null && seri.İade > 0 && saat != null) { saat.Add(_t.faizliSwingCost); seri.İade--; }     // FAİZLİ SENET
            _sonHasar[Kadro.İMZA] = hasar;                                          // İBRAZ
            İmzaİsabeti(yer, e);
            if (seri != null && seri.Seker || Y(SilahYükseltme.Protesto)) Sıçrat(hasar, Weapon.HarvestMultiplier, e);   // İFLAS SENEDİ · PROTESTO
        }

        /// <summary>İMZA'nın isabeti zincire adım (ÇEK'in isabeti gibi). Hit-stop yok: saniyede altı mermi takılırdı.</summary>
        void İmzaİsabeti(Vector3 yer, Enemy hedef)
        {
            _isabetYeri = yer; _sonVurulan = hedef;
            Sfx.Hit();
            if (_tahsilKulesi != null && !_tahsilKulesi.Bitti) _tahsilKulesi.Ömür(0.25f);   // ZİNCİRLEME TAHSİL
            var son = _zincir.Count > 0 ? _zincir[_zincir.Count - 1] : default;      // yeni İMZA adımı: sayaçlar baştan
            if (_zincir.Count == 0 || son.silah != Kadro.İMZA || _comboAn - son.an > _t.comboWindow) { _imzaVurdukları.Clear(); _imzaİsabeti = 0; }
            _imzaİsabeti++;
            if (!_imzaVurdukları.Contains(hedef)) _imzaVurdukları.Add(hedef);
            ZincireYaz(Kadro.İMZA, yer, hedef);                                     // hedef: ZİNCİRLEME CİRO
        }

        /// <summary>Sol tık: bir mermi (basılı tutunca `Update` döngüyü yeniler). İMZA'yla biten combo ilk mermide.</summary>
        void İmzaAteşi()
        {
            İmzaBitirişi();
            Mermi(transform.forward, 1f);
            Weapon.OnStrike(Clock.Instance, _t, 0, 0);
        }

        /// <summary>ÇİFT İMZA · ÇAPRAZ ATEŞ (dash-atak): iki yana birer ağır mermi (silah hasarı × dash çarpanı).</summary>
        void ÇaprazAteş()
        {
            float ç = Weapon.DashDamageMultiplier(_t) / Mathf.Max(0.01f, _t.imzaBulletMul);
            Mermi(transform.right, ç);
            Mermi(-transform.right, ç);
            Sfx.Swing();
        }

        /// <summary>Birikene yaz — BİRİKİM ilk yazıştan sayar. Karşılama · KONKORDATO · REHİN KARŞILIĞI · KARŞILIKLI İMZA.</summary>
        public void Biriktir(float sn)
        {
            if (_biriken <= 0f) _birikenAn = _comboAn;
            _biriken += sn;
        }

        /// <summary>KARŞILIK'ın hasarı: biriken yoksa zayıf, varsa biriken × `karsilikDamagePerSecond` eklenir.</summary>
        float KarşılıkHasarı() => WeaponDamage() * (_biriken > 0f ? 1f : 0.6f) + BirikenHasarı;

        /// <summary>Birikenin hasarı. BİRİKİM (Faz 33b): tutuldukça saniyede +%25, en çok ×2.</summary>
        float BirikenHasarı => _biriken * _t.karsilikDamagePerSecond
                             * (Y(SilahYükseltme.Birikim) ? 1f + 0.25f * Mathf.Clamp(_comboAn - _birikenAn, 0f, 4f) : 1f);

        /// <summary>
        /// Biriken indi. KISMİ ÖDEME (Faz 33b): yarısı kalır. 1 sn'nin altı silinir — yoksa biriken hiç
        /// sıfırlanmaz, "biriken yoksa zayıf vuruş" kuralı da sessizce kalkardı.
        /// </summary>
        void BirikeniHarca()
        {
            _sonBirikenHasarı = BirikenHasarı;                                    // MUKABELE için
            _sonBiriken = _biriken;                                               // TEMİNAT AKÇESİ (Faz 42)
            _biriken = Y(SilahYükseltme.KısmiÖdeme) && _biriken >= 1f ? _biriken * 0.5f : 0f;
            _birikenAn = _comboAn;
        }

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
            if (hits > 0) BirikeniHarca();
            İsabetSonrası(hits);
        }

        /// <summary>
        /// Vuruş `_isabetYeri`'nin yanındakine SIÇRAR (4 birim): SENET ZİNCİRİ (Faz 31), FAİZLİ · SIÇRAMA ve
        /// REHİN · DOMİNO (Faz 33) — üç kopya olmasın diye tek yer.
        /// </summary>
        Enemy Sıçrat(float hasar, float hasat, Enemy hariç, Enemy hariç2 = null)
        {
            Enemy yan = null; float enYakın = 4f;
            foreach (var e in Enemy.All)
            {
                if (e == null || e == hariç || e == hariç2 || e.Dormant) continue;
                Vector3 d = e.transform.position - _isabetYeri; d.y = 0f;
                if (d.magnitude < enYakın) { enYakın = d.magnitude; yan = e; }
            }
            if (yan == null) return null;
            Arc.Spawn(yan.transform.position, transform.rotation, 0.9f, 360f, Weapon.Tint, _t.swingArcLife, scaleFrom: 0.4f, scaleTo: 1.2f);
            yan.TakeDamage(hasar, hasat);
            return yan;
        }

        // ── FAZ 32: TAHVİL · MÜŞTEREK · ARBİTRAJ · KİRA ──────────────────────
        bool _vadeAçık;
        float _vade, _bozdurma, _sonVadeÇarpanı = 1f, _görünmezKalan;
        bool _zorlaArka;
        Enemy _bekleyen;
        MusterekBag _sonBağ;
        KiraKulesi _kule, _eskiKule;
        Vector3 _dashBaşı;
        // Faz 33b
        bool _rolloverKullanıldı, _arkadanÖldürdü, _bedelsizVade;
        float _arkaHasar, _birikenAn;

        /// <summary>TAHVİL: basılı tutuluyorsa olgunluk kademesi (0–3), değilse −1. HUD ve otopilot.</summary>
        public int VadeKademesi => _vadeAçık ? Kademe(_vade) : -1;
        /// <summary>ARBİTRAJ · AÇIK POZİSYON: düşmanlar yeni saldırı başlatmıyor (`Enemy`).</summary>
        public bool Görünmez => _görünmezKalan > 0f;
        /// <summary>ARBİTRAJ'ın arkadan sayılan isabetleri — otopilot raporu.</summary>
        public int ArkadanVuruş { get; private set; }
        public KiraKulesi Kule => _kule != null && !_kule.Bitti ? _kule : null;
        /// <summary>KİRA · ÇİFT KULE (Faz 33b): eski (ikinci) kule.</summary>
        KiraKulesi EskiKule => _eskiKule != null && !_eskiKule.Bitti ? _eskiKule : null;
        /// <summary>Sol tık kule kurar mı: kule yok — ya da ÇİFT KULE'yle ikincisi yok.</summary>
        bool KuleKurulur => Kule == null || (Y(SilahYükseltme.ÇiftKule) && EskiKule == null);
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
        /// <summary>Basılı tutarken yürüyüş. HAREKETLİ (Faz 33b): ×0.8.</summary>
        float TutmaHızı => Y(SilahYükseltme.Hareketli) ? 0.8f : _t.tahvilMoveDamping;

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
            if (_alacaklı != null && Enemy.All.Contains(_alacaklı)) _alacaklı.Yavaşlat(0.2f, 0.5f);   // VADELİ ALACAK
            var saat = Clock.Instance;
            bool kilit = önce >= 3 && Y(SilahYükseltme.Kilit) || _bedelsizVade     // KİLİT (Faz 33b): 3. kademede bedelsiz · VADELİ İŞLEM
                         || _ortakVadeBağı != null && !_ortakVadeBağı.Bitti;        // ORTAK VADE (Parti 4): bağ yaşadıkça
            if (saat != null && !saat.Frozen && !kilit) saat.Spend(saat.AkışHızı * dt * (Y(SilahYükseltme.İskonto) ? 0.5f : 1f));
            int sonra = Kademe(_vade);
            if (sonra > önce)                                       // kademe atladı: görünsün
                Arc.Spawn(transform.position, transform.rotation, 0.6f + 0.35f * sonra, 360f, Weapon.Tint, 0.25f,
                          scaleFrom: 0.6f, scaleTo: 1.1f);
        }

        /// <summary>TAHVİL'in basılı tutması başlarken biten satın alınırlar (Faz 42 Parti 3) — SPEKÜLASYON gibi.</summary>
        void TahvilBaşlangıcı()
        {
            ComboTür? c = null;
            if (ComboHazır(ComboTür.TopluVade)) { _vade += 0.25f * _koniVurulanlar.Count; c = ComboTür.TopluVade; }
            else if (ComboHazır(ComboTür.VadeliAlacak) && AdımHedefi(1) is Enemy borçlu && Enemy.All.Contains(borçlu))
            { _alacaklı = borçlu; c = ComboTür.VadeliAlacak; }
            else if (ComboHazır(ComboTür.Senetİskontosu)) { _vade += 0.1f * _imzaİsabeti; c = ComboTür.Senetİskontosu; }   // her isabet +0.1 sn
            else if (Kule != null && ComboHazır(ComboTür.KiraGetirisi)) { _bedelsizVade = true; c = ComboTür.KiraGetirisi; }   // saat iki kat akmaz
            // Parti 4
            else if (ComboHazır(ComboTür.TapuVadesi))
            {
                _vade = KademeSüresi;                                              // 1. kademe dolu
                if (GroundMark.İçindekiİpotek(AdımYeri(1)) is GroundMark tapu)      // patlarsa +1 kademe (basılı tutarken)
                {
                    _tapuVadesi = tapu;
                    tapu.Patladı += m => { if (_vadeAçık && _tapuVadesi == m) { _vade += KademeSüresi; _tapuVadesi = null; } };
                }
                c = ComboTür.TapuVadesi;
            }
            else if (ComboHazır(ComboTür.RehinVadesi))
            {
                _vade = KademeSüresi;
                var çakılı = _fırlatılanlar.Count > 0 ? _fırlatılanlar[0] : null;
                if (çakılı != null && Enemy.All.Contains(çakılı))
                {
                    çakılı.Yavaşlat(3f, 0f);                                         // 3 sn yerine çakılı
                    if (Combo.Artı(ComboTür.RehinVadesi)) çakılı.Sustur(3f);         // HAPİS HAKKI
                }
                c = ComboTür.RehinVadesi;
            }
            else if (SonBağ is MusterekBag ortak && ComboHazır(ComboTür.OrtakVade)) { _ortakVadeBağı = ortak; c = ComboTür.OrtakVade; }
            if (!(c is ComboTür bu)) return;
            if (Combo.Artı(bu))
                switch (bu)
                {
                    case ComboTür.VadeliAlacak: _specialCd[Kadro.HACİZ] = 0f; break;     // ALACAK FAİZİ
                    case ComboTür.Senetİskontosu: _specialCd[Kadro.İMZA] = 0f; break;    // İSKONTO ORANI
                    case ComboTür.TapuVadesi: _specialCd[Kadro.İPOTEK] = 0f; break;      // TAPU TESCİLİ
                    case ComboTür.RehinVadesi: break;                                     // HAPİS HAKKI yukarıda
                    case ComboTür.OrtakVade: foreach (var e in _ortakVadeBağı.Üyeler) if (e != null) e.Yavaşlat(2f, 0.5f); break;   // ORTAK KUPON
                    default: _specialCd[Kadro.TAHVİL] = 0f; break;                        // ERKEN İTFA · GETİRİ PAYI
                }
            ComboTetikle(bu);
        }

        /// <summary>TAHVİL'i boz (sol tık bırakılınca): olgunluk kadar sert koni, 3. kademede uzun.</summary>
        void TahvilBoz()
        {
            int k = Kademe(_vade);
            _vadeAçık = false; _vade = 0f;
            float ç = VadeÇarpanı(k);
            _sonVadeÇarpanı = ç;
            _sonKademe = k;                                                       // TEMETTÜ
            float menzil = SavurmaMenzili * (k >= 3 ? 1.3f : 1f);
            float açı = k >= 3 && Y(SilahYükseltme.VadeSonu) ? 360f : SavurmaAçısı;   // VADE SONU (Faz 33b)
            Arc.Spawn(transform.position, transform.rotation, menzil, açı, Weapon.Tint, _t.swingArcLife * (1f + 0.3f * k));
            SwingsSpawned++;
            Sfx.Swing();
            _attackTimer = SavurmaSüresi * 0.5f;                    // kısa toparlanma
            _attackHitDone = true;
            StrikeCone(menzil, açı, WeaponDamage() * ç,
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

        /// <summary>Bağı kur ve yükseltmelerini yaz (Faz 33b: ÇEKİŞME, MÜTESELSİL) — iki kuruluş yolu tek yerden.</summary>
        MusterekBag BağKur(IList<Enemy> üyeler)
        {
            var b = MusterekBag.Kur(üyeler, BağPayı, BağHasadı, BağSüresi, OdaKökü);
            b.Çekişme = Y(SilahYükseltme.Çekişme) ? 2.5f : 0f;
            b.Müteselsil = Y(SilahYükseltme.Müteselsil);
            return b;
        }

        // ── FAZ 42 Parti 1 yardımcıları ──
        /// <summary>Düşmanı önüne çek (boss ve tutulan çekilmez) — CEBRİ İCRA · SENETLİ REHİN.</summary>
        void ÖnüneÇek(Enemy e, float yan = 0f)
        {
            if (e == null || !Enemy.All.Contains(e) || e.IsBoss || e.Tutan != null) return;
            Vector3 p = transform.position + transform.forward * (1f + e.Radius) + transform.right * yan;
            e.transform.position = new Vector3(p.x, e.transform.position.y, p.z);
        }

        /// <summary>SENETLİ REHİN: bir yerin `alan` çevresindekileri önüne yan yana çek.</summary>
        void KümeyiÇek(Vector3 yer, float alan)
        {
            var küme = new List<Enemy>();
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dormant) continue;
                Vector3 d = e.transform.position - yer; d.y = 0f;                   // tuzak 2
                if (d.magnitude - e.Radius <= alan) küme.Add(e);
            }
            for (int i = 0; i < küme.Count; i++) ÖnüneÇek(küme[i], i - (küme.Count - 1) * 0.5f);
        }

        /// <summary>Bir yerin çevresine anlık vuruş (AMME ALACAĞI · CİRO · KİRA TAHSİLATI). `sustur` > 0: içindekiler o kadar saldırı
        /// başlatamaz. Kaç düşman öldü (boşu vurmak öldürür — StrikeCone'la aynı sayım).</summary>
        int AlanVuruşu(Vector3 yer, float alan, float hasar, float hasat, Enemy hariç = null, float sustur = 0f)
        {
            _hedefler.Clear();
            foreach (var e in Enemy.All)
            {
                if (e == null || e == hariç || e.Dormant) continue;
                Vector3 d = e.transform.position - yer; d.y = 0f;
                if (d.magnitude - e.Radius <= alan) _hedefler.Add((e, d.magnitude));
            }
            int öldü = 0;
            foreach (var (e, _) in _hedefler)                                       // kopya liste: vuruş öldürebilir
            {
                if (sustur > 0f) e.Sustur(sustur);                                  // vuruştan ÖNCE
                if (e.Hollow) öldü++;
                e.TakeDamage(hasar, hasat);
            }
            Arc.Spawn(yer, transform.rotation, alan, 360f, Weapon.Tint, _t.swingArcLife * 1.3f, scaleFrom: 0.4f, scaleTo: 1.1f);
            return öldü;
        }

        static float ÇizgiyeUzaklık(Vector3 a, Vector3 b, Vector3 p)
        {
            a.y = b.y = p.y = 0f;                                                  // tuzak 2
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return (a + ab * t - p).magnitude;
        }

        /// <summary>Bağ kopar: üyelere ve üyeler arasındaki çizgide duranlara bu hasar (MÜŞTEREK KUSUR · ORTAKLIĞIN TASFİYESİ).
        /// `sustur` yalnızca üyelere (KAYYUM).</summary>
        void BağıKopar(MusterekBag b, float hasar, float hasat, float sustur = 0f)
        {
            var üyeler = b.Üyeler.ToArray();
            b.Bitir();                                                             // önce kopar: hasar bir daha yayılmasın
            _hedefler.Clear();
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dormant) continue;
                bool üye = System.Array.IndexOf(üyeler, e) >= 0, çizgide = false;
                for (int i = 1; i < üyeler.Length && !üye && !çizgide; i++)
                    çizgide = ÇizgiyeUzaklık(üyeler[i - 1].transform.position, üyeler[i].transform.position, e.transform.position) <= e.Radius + 0.5f;
                if (üye || çizgide) _hedefler.Add((e, üye ? 1f : 0f));
            }
            foreach (var (e, üye) in _hedefler)
            {
                if (üye > 0f && sustur > 0f) e.Sustur(sustur);
                e.TakeDamage(hasar, hasat);
            }
            for (int i = 1; i < üyeler.Length; i++)
                if (üyeler[i - 1] != null && üyeler[i] != null)
                    Arc.Spawn(Vector3.Lerp(üyeler[i - 1].transform.position, üyeler[i].transform.position, 0.5f), transform.rotation, 1.2f, 360f,
                              Weapon.Tint, _t.swingArcLife * 1.3f, scaleFrom: 0.4f, scaleTo: 1.2f);
        }

        /// <summary>ZİNCİRLEME CİRO: çekin son çarptığını MÜŞTEREK'in vurduğuna bağla — aynı düşmansa yanındakine.</summary>
        void CiroBağı(Enemy çekin, bool artı)
        {
            var hedef = _sonVurulan;
            if (hedef == null || !Enemy.All.Contains(hedef)) return;
            var üyeler = new List<Enemy> { hedef };
            var öbür = çekin != null && çekin != hedef && Enemy.All.Contains(çekin) ? çekin : Yanındaki(hedef, üyeler);
            if (öbür == null) { Bağla(hedef); return; }
            üyeler.Add(öbür);
            if (artı && Yanındaki(hedef, üyeler) is Enemy üçüncü) üyeler.Add(üçüncü);    // CİRO SİLSİLESİ
            _sonBağ = BağKur(üyeler);
            _bekleyen = null;
        }

        /// <summary>
        /// Kaynaktaki canlılar (en çok 4) + MÜŞTEREK'in vurduğu tek bağda: SÜRÜ (Faz 33c, KONİ'nin vurdukları) · TAPU TEVHİDİ
        /// (ipoteklerin içindekiler) · MÜŞTEREK REHİN (fırlatmanın vurdukları). İkiden azsa düz MÜŞTEREK vuruşu (null).
        /// `ortaya`: bağlananlar bağın ortasına çekilir (AĞIL · TOPLULAŞTIRMA; itme ~ kuvvet/11 birim).
        /// </summary>
        MusterekBag TopluBağ(IEnumerable<Enemy> kaynak, bool ortaya)
        {
            var üyeler = new List<Enemy>();
            foreach (var e in kaynak)
                if (e != null && Enemy.All.Contains(e) && !üyeler.Contains(e) && üyeler.Count < 4) üyeler.Add(e);
            if (_sonVurulan != null && Enemy.All.Contains(_sonVurulan) && !üyeler.Contains(_sonVurulan)) üyeler.Add(_sonVurulan);
            if (üyeler.Count < 2) { Bağla(_sonVurulan); return null; }
            _sonBağ = BağKur(üyeler);
            if (!ortaya) return _sonBağ;
            Vector3 o = _sonBağ.Orta;
            foreach (var e in üyeler)
            {
                Vector3 d = o - e.transform.position; d.y = 0f;
                if (d.magnitude > 0.8f) e.Push(d, (d.magnitude - 0.8f) * 11f);
            }
            return _sonBağ;
        }

        void SürüBağı() => TopluBağ(_koniVurulanlar, Combo.Artı(ComboTür.Sürü));

        /// <summary>Bekleyen oyuncu ipotekleri (TAPU TEVHİDİ · İPOTEK ZİNCİRİ).</summary>
        static List<GroundMark> Bekleyenİpotekler()
        {
            var l = new List<GroundMark>();
            foreach (var m in FindObjectsByType<GroundMark>(FindObjectsSortMode.None)) if (m != null && m.Bekliyor) l.Add(m);
            return l;
        }

        /// <summary>Bekleyen ipoteklerin içindeki düşmanlar (yüzeye ölçü — işaret de öyle vuruyor).</summary>
        static List<Enemy> İpotektekiler()
        {
            var l = new List<Enemy>();
            foreach (var m in Bekleyenİpotekler())
                foreach (var e in Enemy.All)
                {
                    if (e == null || e.Dormant || l.Contains(e)) continue;
                    Vector3 d = e.transform.position - m.transform.position; d.y = 0f;   // tuzak 2
                    if (d.magnitude - e.Radius <= m.Yarıçap) l.Add(e);
                }
            return l;
        }

        /// <summary>Bağlıların her birine AYRI vuruş, bağın yayılması kapalı (HACZE İŞTİRAK · SPEKÜLATİF ATAK).</summary>
        void BağlılaraVur(MusterekBag b, float hasar, float hasat, Enemy hariç = null)
        {
            MusterekBag.Kapalı = true;
            foreach (var e in b.Üyeler.ToArray())                                  // vuruş öldürüp listeden çıkarabilir
                if (e != null && e != hariç && Enemy.All.Contains(e)) e.TakeDamage(hasar, hasat);
            MusterekBag.Kapalı = false;
        }

        /// <summary>HACİZ MEMURU: hedefin öbür yanına geç (arena içinde), ona dön.</summary>
        void ArkasınaGeç(Enemy e)
        {
            if (e == null || !Enemy.All.Contains(e)) return;
            Vector3 öte = e.transform.position - transform.position; öte.y = 0f;
            if (öte.sqrMagnitude < 0.0001f) öte = transform.forward;
            Vector3 p = e.transform.position + öte.normalized * (e.Radius + 0.8f);
            Vector3 m = Bootstrap.ArenaCenter, d = p - m; d.y = 0f;
            if (d.magnitude > _t.arenaRadius - 0.5f) p = m + d.normalized * (_t.arenaRadius - 0.5f);
            transform.position = new Vector3(p.x, transform.position.y, p.z);
            Vector3 bak = e.transform.position - transform.position; bak.y = 0f;
            if (bak.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(bak.normalized);
            Arc.Spawn(transform.position, transform.rotation, 1.2f, 360f, Weapon.Tint, _t.swingArcLife, scaleFrom: 1.2f, scaleTo: 0.4f);
        }

        /// <summary>KİRA HACZİ (Faz 42 Parti 4): düşmanın sana bakan yanı, yüzeyinden 0.6 birim.</summary>
        Vector3 Yanı(Enemy e)
        {
            Vector3 d = transform.position - e.transform.position; d.y = 0f;     // tuzak 2
            return e.transform.position + (d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.back) * (e.Radius + 0.6f);
        }

        /// <summary>GENİŞ ORTAKLIK: e'nin 4 birim yakınındaki, listede olmayan en yakın düşman.</summary>
        Enemy Yanındaki(Enemy e, List<Enemy> hariç)
        {
            Enemy en = null; float enYakın = 4f;
            foreach (var x in Enemy.All)
            {
                if (x == null || x.Dormant || hariç.Contains(x)) continue;
                Vector3 d = x.transform.position - e.transform.position; d.y = 0f;
                if (d.magnitude < enYakın) { enYakın = d.magnitude; en = x; }
            }
            return en;
        }

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
                var üyeler = new List<Enemy> { _bekleyen, e };
                if (Y(SilahYükseltme.GenişOrtaklık) && Yanındaki(e, üyeler) is Enemy üçüncü) üyeler.Add(üçüncü);   // GENİŞ ORTAKLIK (Faz 33b)
                _sonBağ = BağKur(üyeler);
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
            int en = sp.MaxTargets + (Y(SilahYükseltme.ZincirlemeArtı) ? 2 : 0);          // ZİNCİRLEME+ (Faz 33b)
            if (_hedefler.Count > en) _hedefler.RemoveRange(en, _hedefler.Count - en);
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
            if (üyeler.Count >= 2) _sonBağ = BağKur(üyeler);
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
            if (_zorlaArka || Görünmez || e.SırtDönük) return arka;                    // SırtDönük: TAKİP (Faz 33c)
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
            int hits = 0, kills = 0;
            for (int i = Y(SilahYükseltme.Çiftİşlem) ? 2 : 1; i > 0; i--)                 // ÇİFT İŞLEM (Faz 33b)
            {
                StrikeCone(menzil, 70f, WeaponDamage() * Weapon.DashDamageMultiplier(_t),
                           _t.hitStopDuration * 1.4f, _t.hitShake * 1.5f, 1, Weapon.HarvestMultiplier, out int h, out int k);
                hits += h; kills += k;
            }
            _zorlaArka = false;
            Weapon.OnStrike(Clock.Instance, _t, hits, kills);
            İsabetSonrası(hits);
        }

        // KİRA
        float KuleSüresi => _t.kiraSeconds * (Y(SilahYükseltme.UzunKontrat) ? 1.5f : 1f);
        float KuleKirası => _t.kiraRentPerSecond * (Y(SilahYükseltme.UcuzKira) ? 0.6f : 1f)
                            / (Y(SilahYükseltme.UzunKontrat) ? 1.5f : 1f)                     // UZUN KONTRAT: ömür ×1.5, toplam kira aynı
                            * (Y(SilahYükseltme.ÇiftKule) ? 0.5f : 1f);                     // ÇİFT KULE: kule başı yarım
        float KuleMenzili => _t.kiraRadius * (Y(SilahYükseltme.GenişArazi) ? 1.4f : 1f);

        /// <summary>KİRA: nişana kule (menzile kırpılmış). ORTAK GİDER'de kirayı son bağın üyeleri öder.</summary>
        void KuleKur()
        {
            bool ortak = SonBağ != null && ComboHazır(ComboTür.OrtakGider);
            if (ortak) ComboTetikle(ComboTür.OrtakGider);
            bool teminat = !ortak && ComboHazır(ComboTür.Teminat);                 // TEMİNAT (Faz 33c): işaretin yerine, ilk 4 sn kirasız
            bool sertifika = !ortak && !teminat && ComboHazır(ComboTür.KiraSertifikası);   // KİRA SERTİFİKASI (Faz 42)
            ComboTür? satın = null;                                                // Faz 42 Parti 3
            if (!ortak && !teminat && !sertifika)
                foreach (var c in KuleBitirenleri) if (ComboHazır(c) && Şart(c)) { satın = c; break; }
            Enemy haczedilen = satın == ComboTür.KiraHaczi ? AdımHedefi(1) : null;       // KİRA HACZİ (Parti 4): yanına
            Vector3 yer = teminat || satın == ComboTür.İşgal || satın == ComboTür.KiraSenedi || satın == ComboTür.SenetliKira ? AdımYeri(1)
                        : haczedilen != null ? Yanı(haczedilen) : Nişan(Weapon.Range(_t));
            Enemy kilit = satın == ComboTür.ZincirlemeTahsil ? AdımHedefi(2) : null;     // ComboTetikle zinciri silmeden önce
            Enemy kiracı = (satın == ComboTür.İşgal || satın == ComboTür.SenetliKira) && _fırlatılanlar.Count > 0 ? _fırlatılanlar[0] : null;
            if (teminat)
            {
                if (Combo.Artı(ComboTür.Teminat) && GroundMark.İçindekiİpotek(yer) is GroundMark işaret) işaret.HemenPatlat(1.5f);   // İPOTEKLİ KULE
                ComboTetikle(ComboTür.Teminat);
            }
            if (Kule != null) _eskiKule = _kule;                                   // ÇİFT KULE (Faz 33b): eskisi kalıyor
            float atış = WeaponDamage() * _t.kiraShotMul
                       * (sertifika ? Mathf.Max(1f, _sonVadeÇarpanı) : satın == ComboTür.TasfiyeKulesi ? Mathf.Max(1f, İflasÇarpanı())
                          : satın == ComboTür.FaizliKira ? 2f : satın == ComboTür.GizliKule ? _t.arbitrajBackMul : 1f);   // Parti 4: ×2 · arkadan
            _kule = KiraKulesi.Kur(_t, yer, KuleSüresi, KuleKirası, KuleMenzili, atış, Weapon.HarvestMultiplier,
                                   ortak ? _sonBağ : null, ortak && Combo.Artı(ComboTür.OrtakGider), OdaKökü);
            _kule.Depozito = Y(SilahYükseltme.Depozito) ? 0.3f : 0f;                // Faz 33b
            _kule.Stopaj = Y(SilahYükseltme.Stopaj) ? 1.5f : 0f;
            if (teminat) _kule.KirasızKalan = 4f;
            if (sertifika)
            {
                _kule.KirasızKalan = 2f * _sonKademe;
                if (Combo.Artı(ComboTür.KiraSertifikası)) _specialCd[Kadro.TAHVİL] = 0f;   // ANAPARA İADESİ
                ComboTetikle(ComboTür.KiraSertifikası);
            }
            if (satın is ComboTür sc)
            {
                bool artı = Combo.Artı(sc);
                switch (sc)
                {
                    case ComboTür.KiracıSürüsü:
                        foreach (var e in _koniVurulanlar)
                            if (e != null && Enemy.All.Contains(e)) { _kule.Öncelikli.Add(e); _kule.Ömür(1f); if (artı) e.Sustur(1f); }   // KİRA İHTARI
                        break;
                    case ComboTür.TasfiyeKulesi: if (artı) _kule.Stopaj = Mathf.Max(_kule.Stopaj, 1f); break;   // TASFİYE PAYI
                    case ComboTür.İşgal:
                        if (kiracı != null && Enemy.All.Contains(kiracı)) _kule.Kiracı = kiracı;
                        if (artı) _kule.Ömür(3f);                                                                 // TAHLİYE DAVASI
                        break;
                    case ComboTür.KiraSenedi:
                        _kule.KirasızKalan = 3f;
                        if (artı) _specialCd[Kadro.İMZA] = 0f;                                                    // SENETLE ÖDEME
                        break;
                    case ComboTür.ZincirlemeTahsil:
                        if (kilit != null && Enemy.All.Contains(kilit)) { _kule.Kilit = kilit; if (artı) kilit.Sustur(2f); }   // TAHSİL EMRİ
                        _tahsilKulesi = _kule;
                        break;
                    // Parti 4
                    case ComboTür.SenetliKira:
                        _kule.KirasızKalan = 5f; _kule.Ömür(5f);
                        if (artı && kiracı != null && Enemy.All.Contains(kiracı)) _kule.Kiracı = kiracı;     // SENETLİ KİRACI
                        break;
                    case ComboTür.FaizliKira:
                        var kiraSaati = Clock.Instance;
                        if (kiraSaati != null && !kiraSaati.Frozen) kiraSaati.Spend(4f * _t.faizliSwingCost);   // 4 savurma bedeli
                        if (artı) _kule.Ömür(3f);                                                                 // KİRA SÖZLEŞMESİ
                        break;
                    case ComboTür.KiraHaczi:
                        if (haczedilen != null && Enemy.All.Contains(haczedilen))
                        {
                            _kule.Haczedilen = haczedilen;
                            if (artı) haczedilen.Sustur(2f);                                                      // TAHLİYE İHTARI
                        }
                        break;
                    case ComboTür.KiraKarşılığı:
                        _kule.KirasızKalan = Mathf.Max(_kule.KirasızKalan, _sonBiriken);                         // birikim kadar sn
                        if (artı) _specialCd[Kadro.KARŞILIK] = 0f;                                                // KİRA DEPOZİTOSU
                        break;
                    case ComboTür.GizliKule: if (artı) { _görünmezKalan = Mathf.Max(_görünmezKalan, 1f); Paint(); } break;   // PERDE ARKASI
                }
                ComboTetikle(sc);
            }
            SwingsSpawned++;
            Weapon.OnStrike(Clock.Instance, _t, 0, 0);
            ZincireYaz(_weaponIndex, _kule.transform.position);                    // kule kurmak combo adımı (KİRA GELİRİ)
        }

        /// <summary>KİRA · TAHLİYE: kalan kirayı peşin öde (erken fesih), kuleyi patlat — kalan süre kadar hasar. ÇİFT KULE'de ikisi.</summary>
        void Tahliye()
        {
            var eski = EskiKule;
            TahliyeEt(Kule);
            TahliyeEt(eski);
        }

        void TahliyeEt(KiraKulesi k)
        {
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
        int _flurryLeft, _zincirEki;
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
        /// <summary>Dash'e kalan bekleme — otopilot SÜLÜK'ten silkelenmeden önce bakıyor (Faz 36).</summary>
        public float DashCooldownLeft => Mathf.Max(0f, _dashCd);
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
            TickRehin(Time.deltaTime);
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
                    Move(input, TutmaHızı, rotate: false);
                    return;
                }
                TahvilBoz();
                return;
            }

            // ⚠️ Özel saldırı normal saldırıdan ÖNCE okunuyor: ikisi de basılıysa
            // pahalı ve bekleme süreli olan kazanmalı, yoksa oyuncu onu kaçırır.
            if (ReadSpecial() && _specialCd[_weaponIndex] <= 0f) { StartSpecial(input); return; }

            if (ReadAttack()) { StartAttack(input); return; }
            if (Weapon.Saldırı == Biçim.İmza && AttackHeld()) { StartAttack(input); return; }   // ÇİFT İMZA: basılı tuttukça seri

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
            if (Weapon.Saldırı == Biçim.Kira && Kule == null && EskiKule == null) return;   // patlatacak kule yok

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
            if (Weapon.Saldırı == Biçim.İmza)                        // TOPLU İMZA: yelpaze
            {
                int adet = sp.Hits + (Y(SilahYükseltme.TopluİmzaArtı) ? 6 : 0);           // TOPLU İMZA+: on altı mermi
                for (int i = 0; i < adet; i++)
                    Mermi(Quaternion.Euler(0f, (i / (float)(adet - 1) - 0.5f) * sp.Angle, 0f) * transform.forward, sp.DamageMul);
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
                float duruş = sp.Duruş * (Y(SilahYükseltme.Temyiz) ? 1.5f : 1f);   // TEMYİZ (Faz 33b)
                _itirazKalan = duruş;
                _attackTimer = duruş;
                Arc.Spawn(transform.position, transform.rotation, 1.1f, 360f, sp.Tint, duruş, follow: transform, scaled: true);
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
            _zincirEki = 0; _flurryLeft = 0;
            SpecialStrike(sp);                                         // İFLAS ZİNCİRİ ilk vuruşta öldürürse seriye ekliyor
            int vuruş = sp.Hits + (Y(SilahYükseltme.Seri) ? 2 : 0) + _flurryLeft;   // SERİ (İFLAS, Faz 29)
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
            if (kills > 0 && Y(SilahYükseltme.İflasZinciri))                                 // İFLAS ZİNCİRİ (Faz 33)
            {
                _specialCd[_weaponIndex] = 0f;
                int ek = Mathf.Min(kills, 3 - _zincirEki);                                   // öldürdükçe seriye +1, en çok +3
                if (ek > 0) { _zincirEki += ek; _flurryLeft += ek; _attackTimer += sp.HitInterval * ek; }
            }
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
        /// <summary>ARBİTRAJ · SESSİZ ADIM (Faz 33b): görünmezken ×1.3 · KOLCU'nun kelepçesi (Faz 34): ×0.6 · BLOKE'nin yanı (Faz 37): ×0.55.</summary>
        public float YürüyüşÇarpanı => (Görünmez && Y(SilahYükseltme.SessizAdım) ? 1.3f : 1f) * (Kelepçeli ? 0.6f : 1f)
                                     * Enemy.BlokeÇarpanı(transform.position);

        /// <summary>ŞAHİT (Faz 36): ödenen dash bedeli — otopilot imzanın dövüşte işlediğini buradan sayıyor.</summary>
        public float ÖdenenDashBedeli { get; private set; }

        float _kelepçeBitiş = -1f;
        /// <summary>KOLCU (Faz 34): isabeti yürüyüşü kısa süre yavaşlatır. Dash serbest — çıkış yolu o.</summary>
        public void Kelepçele(float süre) => _kelepçeBitiş = Mathf.Max(_kelepçeBitiş, Time.time + süre);
        public bool Kelepçeli => Time.time < _kelepçeBitiş;

        void Move(Vector3 dir, float damping, bool rotate = true)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.position += dir * _t.moveSpeed * damping * YürüyüşÇarpanı * Time.deltaTime;
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
            _dashCd = (Clock.Instance != null ? Clock.Instance.DashCooldown : _t.dashCooldown)
                    * (Y(SilahYükseltme.SonKuyruk) && Clock.Instance != null && Clock.Instance.Seconds < 30f ? 0.5f : 1f);   // SON KUYRUK
            _iFrames = _t.iFrameDuration;
            float şahit = BossKind.ActiveDashCost();                         // ŞAHİT (Faz 36): dash'ini görür
            if (şahit > 0f && Clock.Instance != null) { Clock.Instance.Spend(şahit); ÖdenenDashBedeli += şahit; }
            Sfx.Dash();
            _attackTimer = 0f;              // dash saldırıyı keser
            _dashBaşı = transform.position;                                        // KOPARMA, TERS İŞLEM
            // ROLLOVER (Faz 33b): basılıyken İLK dash ERKEN BOZDURMA'yı vurur ama olgunluğu bozmaz. İlk hâli
            // bozdurmayı yutuyordu — bedeli ödenen yükseltme bot ölçümünde zarar ettiriyordu (−5.5 sn/oda).
            bool rollover = _vadeAçık && Y(SilahYükseltme.Rollover) && !_rolloverKullanıldı;
            float vade = _vade;
            _bozdurma = _vadeAçık ? VadeÇarpanı(Kademe(_vade)) : 0f;               // ERKEN BOZDURMA
            CancelFlurry();                 // ...seriyi de keser, o da bir saldırı
            if (rollover) { _vadeAçık = true; _vade = vade; _rolloverKullanıldı = true; }
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
            if (Weapon.Saldırı == Biçim.İmza) { ÇaprazAteş(); return; } // ÇAPRAZ ATEŞ (Faz 43)
            if (Weapon.Saldırı == Biçim.Tahvil && _bozdurma > 0f) { Bozdur(); return; }      // ERKEN BOZDURMA (Faz 32)
            if (Weapon.Saldırı == Biçim.Müşterek && Kopar()) return;                         // KOPARMA
            if (Weapon.Saldırı == Biçim.Arbitraj && Geçilen() is Enemy geçilen) { Tersİşlem(geçilen); return; }   // TERS İŞLEM
            if (Weapon.Saldırı == Biçim.Kira && Kule != null)                                 // TAŞINMA
            {
                Kule.Taşı(transform.position);
                if (Y(SilahYükseltme.SağlamYapı)) Kule.Uzat(3f);                              // SAĞLAM YAPI (Faz 33b)
                Sfx.Swing();
                return;
            }

            // Dash-atak artık SİLAHA GÖRE değişiyor: tek hedefli silah dash'te de tek
            // hedefli kalır ama çok sert vurur; koni süpürür ama vuruş başına daha az.
            // Eskiden hepsi aynı geniş alan vuruşuydu ve silahın kimliğini siliyordu.
            float range = TemelMenzil * _t.dashAttackRangeMult;
            float angle = Y(SilahYükseltme.Halka) ? 360f : Weapon.DashAngle(_t);   // HALKA (Faz 33)
            float damage = WeaponDamage() * Weapon.DashDamageMultiplier(_t);
            // KARŞI DAVA (KARŞILIK): biriken karşılığın İKİ katı dash-atağa biniyor.
            float karşıDava = Weapon.Saldırı == Biçim.Karşılık ? BirikenHasarı * 2f : 0f;
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
            if (karşıDava > 0f && hits > 0) BirikeniHarca();

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
            if (Weapon.Saldırı == Biçim.Karşılık && _karşılamaBekleme > 0f) return;      // toparlanma bitmeden yok
            if (Weapon.Saldırı == Biçim.Tahvil)                                         // basılı tutma başlıyor
            {
                _vadeAçık = true;
                _vade = 0f;
                _rolloverKullanıldı = false;
                _bedelsizVade = false;
                _alacaklı = null;
                _tapuVadesi = null; _ortakVadeBağı = null;                       // Parti 4
                if (ComboHazır(ComboTür.Spekülasyon) && _comboAn - _sonArkadanAn <= _t.comboWindow)   // SPEKÜLASYON (Faz 33c)
                {
                    _vade = KademeSüresi * 2f;                                    // 2. kademe dolu
                    _bedelsizVade = Combo.Artı(ComboTür.Spekülasyon);             // VADELİ İŞLEM
                    ComboTetikle(ComboTür.Spekülasyon);
                }
                else TahvilBaşlangıcı();                                          // Faz 42 Parti 3
                Vector3 nişan = AimDirection(input);
                if (nişan.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(nişan);
                return;
            }
            if (!Weapon.OnSwingStart(Clock.Instance, _t)) return;
            if (_weaponIndex == Kadro.FAİZLİ && KiraGeliriKulesi is KiraKulesi kg && Clock.Instance != null)   // KİRA GELİRİ (Faz 33c)
            {
                Clock.Instance.Add(_t.faizliSwingCost);
                if (!Combo.Artı(ComboTür.KiraGeliri)) kg.Kısalt(1f);                    // PEŞİN KİRA: ömürden de yemiyor
            }
            if (Y(SilahYükseltme.Sermaye) && Clock.Instance != null && Clock.Instance.HasatDoyması < 0.999f)
                Clock.Instance.Add(_t.faizliSwingCost * 0.5f);                                   // SERMAYE (Faz 33)

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
            // REHİN tutarken: fırlatmanın dar hattı. İMZA: yay yok, merminin izi görünüyor.
            if (Weapon.Saldırı == Biçim.Rehin && Tutuyor)
                Arc.Spawn(transform.position, transform.rotation, _t.rehinThrowRange, 10f, Weapon.Tint, _t.swingArcLife);
            else if (Weapon.GroundMarkDelay(_t) <= 0f && Weapon.Saldırı != Biçim.İmza      // İMZA: merminin izi
                     && !(Weapon.Saldırı == Biçim.Kira && KuleKurulur))       // kule kurmak savurma değil
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

            if (Weapon.Saldırı != Biçim.İmza) SwingsSpawned++;          // İMZA: mermi kendini sayıyor (`Mermi`) — iki kez sayılmasın
            Sfx.Swing();
        }

        void TickAttack(Vector3 input)
        {
            _attackTimer -= Time.deltaTime;

            // ⚠️ SALDIRI TAAHHÜTTÜR. Eskiden savurma sırasında %25 hızla hareket
            // edilebiliyordu; geri çekilince düşman menzilden çıkıyordu ve vuruş
            // ıskalıyordu — Enes'in "hareket tuşlarıyla bozuluyor" dediği şey buydu.
            // Saldırıyı bozan tek şey DASH olmalı, WASD değil.
            Move(input, Weapon.Saldırı == Biçim.İmza ? _t.imzaMoveDamping : _t.attackMoveDamping, rotate: false);   // İMZA ateşte yürür

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

        /// <summary>İMZA'yla biten combo'lar (Faz 42–43) — ilk mermide, isabet beklemeden; etkisi 1 sn'lik seriye biner.
        /// Üçlü önce: SPEKÜLATİF ATAK'ın son ikisi SENET ORTAKLIĞI.</summary>
        void İmzaBitirişi()
        {
            float mermi = WeaponDamage() * _t.imzaBulletMul, hasat = Weapon.HarvestMultiplier;
            if (ComboHazır(ComboTür.Mukabele))                                       // MUKABELE (33c)
            {
                var s = YeniSeri(); s.Ek = 0.25f * _sonBirikenHasarı; s.Sustur = Combo.Artı(ComboTür.Mukabele);   // TEDBİR
                ComboTetikle(ComboTür.Mukabele);
            }
            else if (Kule != null && ComboHazır(ComboTür.ZamanKirası))              // ZAMAN KİRASI (Parti 3)
            {
                if (Clock.Instance != null) Clock.Instance.AkışıBük(3f, 0f);       // 3 sn saat akmaz
                if (Combo.Artı(ComboTür.ZamanKirası)) Kule.Ömür(3f);               // ZAMAN AŞIMI
                YeniSeri().Çarpan = 1.5f;
                ComboTetikle(ComboTür.ZamanKirası);
            }
            else if (ComboHazır(ComboTür.SpekülatifAtak) && SonBağ is MusterekBag bağ)
            {
                if (Combo.Artı(ComboTür.SpekülatifAtak)) foreach (var e in bağ.Üyeler) if (e != null) e.SırtDön(2f);   // KISA SATIŞ
                BağlılaraVur(bağ, mermi * _t.arbitrajBackMul, hasat);                // birer mermi, arkadan
                ComboTetikle(ComboTür.SpekülatifAtak);
            }
            else if (Kule is KiraKulesi kule && ComboHazır(ComboTür.KiraTahsilatı))
            {
                int öldü = AlanVuruşu(kule.transform.position, kule.Yarıçap, mermi, hasat);   // menzildekilere birer mermi
                if (öldü > 0 && kule.Stopaj > 0f && Clock.Instance != null) Clock.Instance.Add(kule.Stopaj * öldü * Clock.Instance.HasatDoyması);
                if (Combo.Artı(ComboTür.KiraTahsilatı)) _specialCd[Kadro.İMZA] = 0f;      // VERGİ İADESİ
                ComboTetikle(ComboTür.KiraTahsilatı);
            }
            else if (ComboHazır(ComboTür.FaizliSenet))                               // FAİZLİ SENET (Parti 3)
            {
                YeniSeri().İade = 2;                                                 // serinin ilk iki isabeti bedeli geri öder
                if (Combo.Artı(ComboTür.FaizliSenet)) _specialCd[Kadro.FAİZLİ] = 0f;      // SENET İADESİ
                ComboTetikle(ComboTür.FaizliSenet);
            }
            else if (ComboHazır(ComboTür.İflasSenedi))                               // Parti 4
            {
                var s = YeniSeri(); s.Çarpan = Mathf.Max(1f, İflasÇarpanı()); s.Seker = true;
                if (Combo.Artı(ComboTür.İflasSenedi)) _specialCd[Kadro.İFLAS] = 0f;       // MASA ALACAĞI
                ComboTetikle(ComboTür.İflasSenedi);
            }
            else if (ComboHazır(ComboTür.TopluTahsilat))
            {
                bool makbuz = Combo.Artı(ComboTür.TopluTahsilat);                    // TAHSİL MAKBUZU
                ComboTetikle(ComboTür.TopluTahsilat);                                // önce: ek mermiler yeni adım yazsın
                foreach (var e in _koniVurulanlar)                                   // her birine birer mermi (nişansız)
                {
                    if (e == null || !Enemy.All.Contains(e)) continue;
                    if (makbuz) e.Sustur(1f);
                    Mermiİzi(transform.position, e.transform.position, transform.forward);
                    MermiVur(e, mermi, false);
                }
            }
            else if (ComboHazır(ComboTür.VadeliSenet))
            {
                var s = YeniSeri(); s.Çarpan = Mathf.Max(1f, _sonVadeÇarpanı); s.Hız = 1f + 0.25f * _sonKademe;
                if (Combo.Artı(ComboTür.VadeliSenet)) _specialCd[Kadro.İMZA] = 0f;       // KEŞİDE
                ComboTetikle(ComboTür.VadeliSenet);
            }
            else if (SonBağ is MusterekBag senetBağı && ComboHazır(ComboTür.SenetOrtaklığı))
            {
                YeniSeri().TamPay = true;                                            // bağlıya değince %100 yayar
                if (Combo.Artı(ComboTür.SenetOrtaklığı)) senetBağı.Tazele(BağSüresi);   // SENET YENİLEME
                ComboTetikle(ComboTür.SenetOrtaklığı);
            }
        }

        /// <summary>İFLAS'ın şimdiki çaresizlik çarpanı (İFLAS DAİRESİ · TASFİYE KULESİ) — saat azsa sert, çoksa ×0.7.</summary>
        float İflasÇarpanı() => Weapon.All[Kadro.İFLAS].DesperationMultiplier(Clock.Instance != null ? Clock.Instance.Seconds : _t.startSeconds, _t);

        /// <summary>FAİZ YÜKÜ: bu odada kaçıncı kez (oda değişince baştan). Vuruştan önce okunur, isabette artar.</summary>
        int FaizYüküOdada()
        {
            var rm = RoomManager.Instance;
            int oda = rm != null ? rm.RegionIndex * 1000 + rm.Room : 0;
            if (oda != _faizYüküOdası) { _faizYüküOdası = oda; _faizYükü = 0; }
            return _faizYükü;
        }

        void Strike()
        {
            // ⚠️ İPOTEK koni vurmuyor: nişan noktasına GECİKMELİ yer vuruşu bırakıyor.
            // Silahın kendi vuruş biçimi olması `Weapon`'ın deseniyle uyumlu —
            // veri + kanca, `PlayerController` içinde silah adı geçmiyor.
            if (Weapon.GroundMarkDelay(_t) > 0f) { PlaceLien(); return; }
            if (Weapon.Saldırı == Biçim.Rehin) { RehinVuruşu(); return; }
            if (Weapon.Saldırı == Biçim.Tahvil) { TahvilBoz(); return; }               // Faz 32
            if (Weapon.Saldırı == Biçim.Kira && KuleKurulur) { KuleKur(); return; }
            if (Weapon.Saldırı == Biçim.İmza) { İmzaAteşi(); return; }           // ÇİFT İMZA (Faz 43)

            // COMBO bitirenleri (Faz 30–31) — zincirin son adımı bu koni vuruşu:
            //   TEMERRÜT (FAİZLİ → İFLAS): saat yarıdaymış gibi; "+" hasat ×2
            //   KARŞI İCRA (KARŞILIK → FAİZLİ) ×2; "+" İCRA'nın beklemesi sıfır
            //   SENET HACZİ (İMZA → HACİZ) ×2; "+" yanındakine seker
            //   SİLİP SÜPÜR (REHİN → KONİ) ×1.5, sert iter; "+" 360°
            //   AÇIĞA SATIŞ (REHİN → ARBİTRAJ) her yönden arkadan; "+" ×1.5 daha
            //   VADE FARKI (TAHVİL → FAİZLİ) TAHVİL'in kademe çarpanıyla (en az ×1.5); "+" bedel geri
            ComboTür? bitiren = null;
            foreach (var c in Bitirenler) if (ComboHazır(c) && Şart(c)) { bitiren = c; break; }
            bool artı = bitiren.HasValue && Combo.Artı(bitiren.Value);
            // Faz 42: combo'nun önceki adımı — ComboTetikle zinciri silmeden önce
            Vector3 adımYeri = bitiren.HasValue ? AdımYeri(1) : _isabetYeri;
            Enemy adımHedefi = _sonAdımHedefi;
            float hasarÇ = bitiren == ComboTür.KarşıİCRA || bitiren == ComboTür.SenetHaczi ? 2f
                         : bitiren == ComboTür.SilipSüpür ? 1.5f
                         : bitiren == ComboTür.VadeFarkı ? Mathf.Max(1.5f, _sonVadeÇarpanı)
                         : bitiren == ComboTür.AçığaSatış && artı ? 1.5f : 1f;
            if (bitiren == ComboTür.Kefalet) hasarÇ = 2f;                                              // KEFALET (33c): ×2, ölü bölge yok
            if (bitiren == ComboTür.Temettü) hasarÇ = 1f + 0.25f * _sonKademe;                         // TEMETTÜ (33c)
            if (bitiren == ComboTür.Konkordato && _t.karsilikDamagePerSecond > 0f)                    // KONKORDATO (33c)
                Biriktir(_sonHasar[Kadro.İFLAS] * 0.5f / _t.karsilikDamagePerSecond);
            // Faz 42 Parti 1 — vuruştan önce: çarpan, menzil, ek hasar, çekme
            float menzil = SavurmaMenzili, ek = 0f, dip = bitiren == ComboTür.Temerrüt ? 0.5f : 1f;
            switch (bitiren)
            {
                case ComboTür.Cebriİcra: hasarÇ = 1.5f; ÖnüneÇek(adımHedefi); break;
                case ComboTür.SenetliRehin: hasarÇ = 1.5f; KümeyiÇek(adımYeri, 3f); break;
                case ComboTür.TopluBorç: hasarÇ = 1f + Mathf.Min(0.75f, 0.15f * _koniVurulanlar.Count); break;
                case ComboTür.AlacakDevri: ek = _sonHasar[Kadro.FAİZLİ]; break;
                case ComboTür.İflasMasası: ek = 0.5f * _sonHasar[Kadro.İFLAS]; break;
                case ComboTür.KarşılıklıTahsil: ek = 0.5f * _sonHasar[Kadro.KARŞILIK]; break;
                case ComboTür.VadeliHaciz: menzil *= 1.5f; hasarÇ = 1f + 0.2f * _sonKademe; break;
                case ComboTür.ArkadanDolanım: menzil *= 1.3f; hasarÇ = 1.3f; break;
                case ComboTür.TeminatlıTahvil: hasarÇ = 1.25f * Mathf.Max(1.5f, _sonVadeÇarpanı); ek = _sonBirikenHasarı; break;
                case ComboTür.KiraKontratı: hasarÇ = 1.5f; break;
                case ComboTür.İcraTakibi: dip = 0.25f; break;
                // Faz 42 Parti 2
                case ComboTür.AcizVesikası: hasarÇ = 1.5f; break;
                case ComboTür.HacizMemuru: ArkasınaGeç(adımHedefi); break;
                case ComboTür.İflasDairesi:
                    hasarÇ = Mathf.Max(1f, İflasÇarpanı());
                    break;
                case ComboTür.İpotekZinciri: dip = 0.5f; break;
                case ComboTür.İflasErtelemesi: hasarÇ = VadeÇarpanı(Mathf.Min(3, _sonKademe + 1)); break;
                case ComboTür.VadeSonuTahsilatı: hasarÇ = Mathf.Max(1f, _sonVadeÇarpanı); break;
                // Faz 42 Parti 3
                case ComboTür.FaizYükü: hasarÇ = 1f + 0.15f * Mathf.Min(4, FaizYüküOdada() + 1); break;
                case ComboTür.İpotekliİflas: dip = 0f; break;                                   // saatin dibindeymiş gibi
                case ComboTür.RehinKarşılığı:
                    if (_t.karsilikDamagePerSecond > 0f)
                        Biriktir(_sonFırlatma / _t.karsilikDamagePerSecond);                    // fırlatmanın hasarı birikene
                    break;
                case ComboTür.Karşılıksızİflas: ek = 2f * _sonBirikenHasarı; break;
                case ComboTür.KarşıHamle: ArkasınaGeç(adımHedefi); break;
                case ComboTür.İçİşlem: if (SonBağ is MusterekBag içBağ) foreach (var e in içBağ.Üyeler) if (e != null) e.SırtDön(2f); break;
                case ComboTür.GizliHaciz: hasarÇ = 1.3f; break;
                case ComboTür.KarşıRehin: ek = _sonBirikenHasarı; break;
                // Faz 42 Parti 4
                case ComboTür.SürüdenAyır:
                    Enemy ayrık = null; float ayrıkUzak = -1f;                                  // KONİ'nin vurduklarından en uzaktaki
                    foreach (var e in _koniVurulanlar)
                    {
                        if (e == null || !Enemy.All.Contains(e)) continue;
                        Vector3 ayrıkD = e.transform.position - transform.position; ayrıkD.y = 0f;   // tuzak 2
                        if (ayrıkD.magnitude > ayrıkUzak) { ayrıkUzak = ayrıkD.magnitude; ayrık = e; }
                    }
                    ArkasınaGeç(ayrık);
                    break;
                case ComboTür.TapuKredisi: hasarÇ = 2f; break;
                case ComboTür.RehinliKredi: hasarÇ = 1.5f; break;
                case ComboTür.İmzalıKredi: hasarÇ = 1.3f; break;
                case ComboTür.Karşılıklıİmza: Biriktir(Mathf.Min(2f, 0.25f * _imzaİsabeti)); break;   // isabetler birikene, KARŞILIK indirir
                case ComboTür.KarşıHaciz: ek = _sonBirikenHasarı; break;
                case ComboTür.KuleHaczi: hasarÇ = 1.3f; break;
                case ComboTür.TapuKarşılığı:
                    foreach (var tapuİşareti in Bekleyenİpotekler())
                    {
                        int önce = tapuİşareti.EnemiesHit;
                        if (artı) tapuİşareti.İtme = 7f;                                         // TAPU TAHLİYESİ
                        tapuİşareti.HemenPatlat(1f);
                        ek += tapuİşareti.Hasar * (tapuİşareti.EnemiesHit - önce);              // vurdukları kadar KONİ'ye
                    }
                    break;
                case ComboTür.Arkadanİcra: hasarÇ = Mathf.Max(1f, İflasÇarpanı()) * _t.arbitrajBackMul; break;   // arka çarpanı elle: HACİZ ARBİTRAJ değil
                case ComboTür.VadeliKarşılık: ek = _sonBirikenHasarı * Mathf.Max(2f, _sonVadeÇarpanı); break;   // en az KARŞILIKSIZ İFLAS kadar
            }
            _zorlaArka = bitiren == ComboTür.AçığaSatış || bitiren == ComboTür.Takip                   // TAKİP (33c): arkadan
                      || bitiren == ComboTür.VadesiGelmişPozisyon || bitiren == ComboTür.HacizMemuru
                      || bitiren == ComboTür.ArsaSpekülasyonu || bitiren == ComboTür.KarşıHamle || bitiren == ComboTür.KarşıRehin
                      || bitiren == ComboTür.SürüdenAyır;
            float hasat = Weapon.HarvestMultiplier * ((bitiren == ComboTür.Temerrüt || bitiren == ComboTür.Temettü
                                                        || bitiren == ComboTür.AmmeAlacağı || bitiren == ComboTür.İflasMasası
                                                        || bitiren == ComboTür.Hacizİştiraki || bitiren == ComboTür.VadeSonuTahsilatı
                                                        || bitiren == ComboTür.FaizYükü) && artı ? 2f : 1f);   // … · FAİZE FAİZ   // TEMERRÜT FAİZİ · KÂR PAYI · GECİKME ZAMMI · MASA PAYI · İŞTİRAK PAYI · TAHSİL HARCI
            float açı = (bitiren == ComboTür.SilipSüpür || bitiren == ComboTür.SenetliRehin) && artı ? 360f : SavurmaAçısı;   // TOZ DUMAN · KAMBİYO
            if (bitiren == ComboTür.Temettü) açı = Mathf.Min(360f, açı + 30f * _sonKademe);
            if (bitiren == ComboTür.Tapuİptali) açı = Mathf.Min(360f, açı + 60f);
            if (bitiren == ComboTür.VadeSonuTahsilatı || bitiren == ComboTür.OrtakKarşılık) açı = 360f;
            if (açı >= 360f) Arc.Spawn(transform.position, transform.rotation, menzil, 360f, Weapon.Tint, _t.swingArcLife * 1.4f);
            bool karşılık = Weapon.Saldırı == Biçim.Karşılık;
            float hasar = (karşılık ? KarşılıkHasarı() : WeaponDamage(dip) * hasarÇ) + ek;
            MusterekBag.TamPay = bitiren == ComboTür.Konsorsiyum;                                     // KONSORSİYUM (33c): bu vuruş boyunca
            StrikeCone(menzil, açı, hasar,
                       _t.hitStopDuration * Weapon.HitStopMultiplier,
                       _t.hitShake * Weapon.ShakeMultiplier,
                       SavurmaHedefi, hasat,
                       out int hits, out int kills,
                       minRange: bitiren == ComboTür.Kefalet || bitiren == ComboTür.İflasDairesi || bitiren == ComboTür.İflasErtelemesi
                                 || bitiren == ComboTür.GizliHaciz || bitiren == ComboTür.KarşıHaciz || bitiren == ComboTür.KuleHaczi
                                 || bitiren == ComboTür.Arkadanİcra ? 0f : ÖlüBölge,
                       itmeTaban: bitiren == ComboTür.SilipSüpür ? 8f : 0f);
            _zorlaArka = false;
            MusterekBag.TamPay = false;
            if (bitiren == ComboTür.Takip && hits > 0 && _sonVurulan != null)
            {
                _sonVurulan.SırtDön(2f);
                if (artı) _sonVurulan.Sustur(2f);                                                     // PUSU
            }
            if (bitiren == ComboTür.KiraGeliri && hits > 0) _kiraGeliri = Kule != null ? Kule : EskiKule;
            if (karşılık && hits > 0) BirikeniHarca();
            if (Weapon.Saldırı == Biçim.Müşterek && hits > 0)
            {
                if (bitiren == ComboTür.Sürü) SürüBağı();                                            // SÜRÜ (33c)
                else if (bitiren == ComboTür.ZincirlemeCiro) CiroBağı(adımHedefi, artı);             // ZİNCİRLEME CİRO (42)
                else if (bitiren == ComboTür.TapuTevhidi) TopluBağ(İpotektekiler(), artı);           // TAPU TEVHİDİ · TOPLULAŞTIRMA
                else if (bitiren == ComboTür.GizliOrtaklık)                                          // GİZLİ ORTAKLIK (Parti 4)
                {
                    if (TopluBağ(_imzaVurdukları, false) is MusterekBag gizliBağ)
                        foreach (var e in gizliBağ.Üyeler) if (e != null) e.SırtDön(2f);
                }
                else if (bitiren == ComboTür.HacizOrtaklığı || bitiren == ComboTür.İflasOrtaklığı)  // HACİZ · İFLAS ORTAKLIĞI (Parti 4)
                {
                    if (TopluBağ(new[] { adımHedefi }, false) is MusterekBag ortakBağ && bitiren == ComboTür.İflasOrtaklığı)
                        ortakBağ.PayÇarpanı = Mathf.Min(Mathf.Max(1f, İflasÇarpanı()), 1f / Mathf.Max(0.01f, ortakBağ.Pay));   // en çok %100
                }
                else if (bitiren == ComboTür.MüşterekRehin)                                          // MÜŞTEREK REHİN
                {
                    if (TopluBağ(_fırlatılanlar, false) is MusterekBag rehinBağı && artı)
                        foreach (var e in rehinBağı.Üyeler) if (e != null) e.Sustur(1f);           // TEMİNAT TAMAMLAMA
                }
                else Bağla(_sonVurulan);
            }
            Vector3 anaYer = _isabetYeri; Enemy anaHedef = _sonVurulan;
            if (Y(SilahYükseltme.Yankı))                                                       // YANKI (Faz 33)
            {
                Arc.Spawn(transform.position, transform.rotation, SavurmaMenzili, açı, Weapon.Tint, _t.swingArcLife, scaleFrom: 1.1f, scaleTo: 0.9f);
                StrikeCone(SavurmaMenzili, açı, hasar * 0.5f, 0f, 0f, SavurmaHedefi, hasat, out _, out int yankıÖldürdü, minRange: ÖlüBölge);
                kills += yankıÖldürdü;
                _isabetYeri = anaYer; _sonVurulan = anaHedef;                                  // combo ve sıçrama ana vuruşun yerinden
            }
            if (Y(SilahYükseltme.GeriÇekil) && hits > 0) _knockback = -transform.forward * 12f;  // GERİ ÇEKİL (Faz 33)

            Weapon.OnStrike(Clock.Instance, _t, hits, kills);
            if (bitiren.HasValue && hits > 0)
            {
                ComboTetikle(bitiren.Value);
                if (artı && bitiren == ComboTür.KarşıİCRA) _specialCd[Kadro.FAİZLİ] = 0f;   // İCRA EMRİ
                if (artı && bitiren == ComboTür.SenetHaczi) Sıçrat(hasar, hasat, _sonVurulan);  // SENET ZİNCİRİ
                if (artı && bitiren == ComboTür.VadeFarkı && Clock.Instance != null)          // KUPON
                    Clock.Instance.Add(_t.faizliSwingCost);
                if (artı && bitiren == ComboTür.Konkordato) _specialCd[Kadro.İFLAS] = 0f;     // ERTELEME
                if (artı && bitiren == ComboTür.Kefalet) Sıçrat(hasar, hasat, _sonVurulan);   // AVAL
                if (artı && bitiren == ComboTür.Konsorsiyum && _sonVurulan != null && _sonVurulan.Bağ != null)
                    _sonVurulan.Bağ.Tazele(BağSüresi);                                          // YAPILANDIRMA
                // Faz 42 Parti 1 — vuruştan sonra ("+" hâlleri yorumda)
                switch (bitiren.Value)
                {
                    case ComboTür.Cebriİcra: if (artı && adımHedefi != null) adımHedefi.Sustur(1.5f); break;           // ZORLA GETİRME
                    case ComboTür.TopluBorç:
                        if (Clock.Instance != null) Clock.Instance.Add(_t.faizliSwingCost);                          // bedel geri
                        if (artı) Sıçrat(hasar, hasat, _sonVurulan);                                                   // KEFİLLİ BORÇ
                        break;
                    case ComboTür.AlacakDevri: if (artı && _sonVurulan != null) _sonVurulan.SırtDön(2f); break;       // TEMLİK
                    case ComboTür.Tapuİptali:
                        if (GroundMark.İçindekiİpotek(adımYeri) is GroundMark tapu) tapu.HemenPatlat(1f);
                        if (artı) _specialCd[Kadro.KONİ] = 0f;                                                         // İSTİMLAK
                        break;
                    case ComboTür.AmmeAlacağı: AlanVuruşu(_isabetYeri, 3f, hasar * 0.5f, hasat, _sonVurulan); break;
                    case ComboTür.Ciro: AlanVuruşu(adımYeri, 2.5f, hasar * 0.8f, hasat, null, artı ? 1f : 0f); break; // TAHSİL CİROSU
                    case ComboTür.KarşılıklıTahsil: if (artı) _specialCd[Kadro.KARŞILIK] = 0f; break;                  // MAHSUP
                    case ComboTür.VadeliHaciz: if (artı && _sonVurulan != null) _sonVurulan.Yavaşlat(2f, 0.5f); break; // HACİZ ŞERHİ
                    case ComboTür.MüşterekKusur:
                        if (SonBağ is MusterekBag kusur) BağıKopar(kusur, hasar, hasat);
                        if (artı) _specialCd[Kadro.MÜŞTEREK] = 0f;                                                     // RÜCU
                        break;
                    case ComboTür.ArkadanDolanım:
                        if (artı) foreach (var vurulan in _koniVurulanlar) if (vurulan != null) vurulan.SırtDön(2f);   // KÖR NOKTA
                        break;
                    case ComboTür.VadesiGelmişPozisyon:
                        _görünmezKalan = Mathf.Max(_görünmezKalan, 1.5f); Paint();
                        if (artı) _specialCd[Kadro.ARBİTRAJ] = 0f;                                                     // ZARAR KES
                        break;
                    case ComboTür.İcraTakibi:
                        if (Clock.Instance != null) Clock.Instance.Add(2f * _t.faizliSwingCost);                     // iki savurma bedeli
                        if (artı) _specialCd[Kadro.İFLAS] = 0f;                                                        // TAZMİNAT
                        break;
                    case ComboTür.TeminatlıTahvil: if (artı) _specialCd[Kadro.TAHVİL] = 0f; break;                    // TEMİNAT İADESİ
                    case ComboTür.OrtaklığınTasfiyesi:
                        if (GroundMark.İçindekiİpotek(adımYeri) is GroundMark tasfiye) tasfiye.HemenPatlat(1.5f);
                        if (SonBağ is MusterekBag ortaklık) BağıKopar(ortaklık, hasar, hasat, artı ? 1.5f : 0f);   // KAYYUM
                        break;
                    case ComboTür.KiraKontratı:
                        if (Kule != null && _sonVurulan != null)
                        {
                            Kule.Kilit = _sonVurulan;
                            if (artı) _sonVurulan.Sustur(2f);                                                          // İHTARNAME
                        }
                        break;
                    // Faz 42 Parti 2
                    case ComboTür.AcizVesikası:
                        if (_sonVurulan != null && Enemy.All.Contains(_sonVurulan))
                        {
                            if (_sonVurulan.Yorgun) _sonVurulan.HazırlığıBaşaSar();
                            if (artı) _sonVurulan.SırtDön(2f);                                                         // MAL BEYANI
                        }
                        break;
                    case ComboTür.HacizMemuru: if (artı && adımHedefi != null) adımHedefi.Sustur(1.5f); break;        // TEBLİĞ
                    case ComboTür.İflasDairesi:
                    case ComboTür.İflasErtelemesi:
                        if (artı && _sonVurulan != null) _sonVurulan.Yavaşlat(2f, 0.5f);                               // İFLAS İLANI · TEDBİR KARARI
                        break;
                    case ComboTür.İbraz:
                        if (_sonVurulan != null && Enemy.All.Contains(_sonVurulan))       // İMZA'nın isabetleri ona döner, her biri ×1.5
                            _sonVurulan.TakeDamage(_imzaİsabeti * _sonHasar[Kadro.İMZA] * 1.5f, hasat);
                        if (artı) _specialCd[Kadro.İFLAS] = 0f;                                                        // KAMBİYO TAKİBİ
                        break;
                    case ComboTür.Hacizİştiraki: if (SonBağ is MusterekBag iştirak) BağlılaraVur(iştirak, hasar, hasat, _sonVurulan); break;
                    case ComboTür.Hileliİflas:
                        if (_sonVurulan != null && Enemy.All.Contains(_sonVurulan)) _sonVurulan.TakeDamage(hasar, hasat);   // ikinci kez
                        if (artı) { _görünmezKalan = Mathf.Max(_görünmezKalan, 1f); Paint(); }                         // PERDE
                        break;
                    case ComboTür.İpotekZinciri:
                        var içtekiler = İpotektekiler();
                        foreach (var işaret in Bekleyenİpotekler()) { if (artı) işaret.İtme = 7f; işaret.HemenPatlat(1f); }   // TAHLİYE EMRİ
                        foreach (var e in içtekiler) if (e != _sonVurulan && Enemy.All.Contains(e)) e.TakeDamage(hasar, hasat);
                        break;
                    case ComboTür.VadeSonuTahsilatı: AlanVuruşu(adımYeri, 2.5f, 0.5f * _sonHasar[Kadro.İFLAS], hasat); break;
                    // Faz 42 Parti 3
                    case ComboTür.FaizYükü: FaizYüküOdada(); _faizYükü++; break;
                    case ComboTür.İpotekliİflas:
                        foreach (var işaret in Bekleyenİpotekler()) { if (artı) işaret.İtme = 7f; işaret.HemenPatlat(1f); }   // İCRAİ SATIŞ
                        break;
                    case ComboTür.ArsaSpekülasyonu:
                    case ComboTür.GizliHaciz:
                    case ComboTür.Arkadanİcra:
                        if (artı) { _görünmezKalan = Mathf.Max(_görünmezKalan, 1f); Paint(); }                         // ARSA SPEKÜLATÖRÜ · SESSİZ HACİZ · İCRA PERDESİ
                        break;
                    case ComboTür.RehinKarşılığı: if (artı) _specialCd[Kadro.KARŞILIK] = 0f; break;                  // KARŞILIK İADESİ
                    case ComboTür.Karşılıksızİflas:
                    case ComboTür.VadeliKarşılık:
                        if (artı && _sonVurulan != null) _sonVurulan.Yavaşlat(2f, 0.5f);                                // SAHTE İFLAS · KARŞILIK VADESİ
                        break;
                    case ComboTür.KarşıHamle: if (artı && adımHedefi != null) adımHedefi.Sustur(1.5f); break;        // KARŞI İHTAR
                    case ComboTür.TahvilOrtaklığı:
                        if (SonBağ is MusterekBag tBağ)
                        {
                            tBağ.Tazele(tBağ.Kalan + 2f * _sonKademe);
                            tBağ.PayÇarpanı = 1f + 0.1f * _sonKademe;
                            if (artı) foreach (var e in tBağ.Üyeler) if (e != null) e.Sustur(1f);                     // ORTAKLIK PAYI
                        }
                        break;
                    case ComboTür.OrtakBorç:
                        if (SonBağ is MusterekBag borçBağı)
                        {
                            if (artı) foreach (var e in borçBağı.Üyeler) if (e != null) e.Yavaşlat(2f, 0.5f);         // MÜŞTEREK BORÇLU
                            BağlılaraVur(borçBağı, _t.faizliSwingCost, 0f);
                            if (Clock.Instance != null) Clock.Instance.Add(_t.faizliSwingCost);                      // bedel sana geri
                        }
                        break;
                    case ComboTür.İçİşlem: if (artı) _specialCd[Kadro.ARBİTRAJ] = 0f; break;                          // İÇERİDEN BİLGİ
                    case ComboTür.SüpürgeKulesi:
                        if (Kule is KiraKulesi süpürge)
                        {
                            AlanVuruşu(süpürge.transform.position, süpürge.Yarıçap, hasar, hasat);
                            if (artı) süpürge.Ömür(2f);                                                               // KULE SÜPÜRMESİ
                        }
                        break;
                    case ComboTür.TopluİCRA:
                        Enemy zayıf = null;
                        foreach (var e in _koniVurulanlar)
                            if (e != null && Enemy.All.Contains(e) && (zayıf == null || e.TimeLeft < zayıf.TimeLeft)) zayıf = e;
                        if (zayıf != null)
                        {
                            bool boştu = zayıf.Hollow;                                                               // boşu vurmak öldürür
                            zayıf.TakeDamage(WeaponDamage(0f), hasat);                                               // İFLAS'ın dip vuruşu
                            if ((boştu || zayıf.Hollow || !Enemy.All.Contains(zayıf)) && Clock.Instance != null)
                                Clock.Instance.Add(2f * Clock.Instance.HasatDoyması);
                        }
                        if (artı) _specialCd[Kadro.KONİ] = 0f;                                                        // İCRA MASRAFI
                        break;
                    case ComboTür.BileşikFaiz:
                        if (SonBağ is MusterekBag fBağ)
                        {
                            fBağ.PayÇarpanı = Mathf.Min(Mathf.Max(1f, _sonVadeÇarpanı), 1f / Mathf.Max(0.01f, fBağ.Pay));   // en çok %100
                            if (artı) foreach (var e in fBağ.Üyeler) if (e != null) e.Yavaşlat(2f, 0.5f);           // FAİZ TAHAKKUKU
                        }
                        break;
                    case ComboTür.KarşıRehin: if (artı && _sonVurulan != null) _sonVurulan.SırtDön(2f); break;       // REHİN AÇIĞI
                    case ComboTür.OrtakKarşılık:
                        if (SonBağ is MusterekBag kBağ) BağlılaraVur(kBağ, _sonHasar[Kadro.KARŞILIK], hasat);
                        if (artı) _specialCd[Kadro.KARŞILIK] = 0f;                                                    // KARŞILIKLI ORTAKLIK
                        break;
                    // Faz 42 Parti 4
                    case ComboTür.SürüdenAyır:
                        if (artı) foreach (var e in _koniVurulanlar) if (e != null && e != _sonVurulan && Enemy.All.Contains(e)) e.SırtDön(2f);   // AYRIŞMA
                        break;
                    case ComboTür.HacizOrtaklığı: if (artı) _specialCd[Kadro.HACİZ] = 0f; break;                     // MÜŞTEREK HACİZ
                    case ComboTür.İflasOrtaklığı:
                        if (artı && SonBağ is MusterekBag iflasBağı) foreach (var e in iflasBağı.Üyeler) if (e != null) e.Yavaşlat(2f, 0.5f);   // ORTAK İFLAS
                        break;
                    case ComboTür.TapuKredisi:
                    case ComboTür.RehinliKredi:
                    case ComboTür.İmzalıKredi:
                        if (Clock.Instance != null) Clock.Instance.Add(_t.faizliSwingCost);                          // savurma bedeli geri
                        if (artı && bitiren == ComboTür.TapuKredisi) foreach (var işaret in Bekleyenİpotekler()) işaret.HemenPatlat(1f);   // İPOTEK FEKKİ
                        if (artı && bitiren == ComboTür.RehinliKredi) _specialCd[Kadro.REHİN] = 0f;                    // REHİN FEKKİ
                        if (artı && bitiren == ComboTür.İmzalıKredi && _sonVurulan != null) _sonVurulan.Sustur(1f);    // KEFİL İMZASI
                        break;
                    case ComboTür.Karşılıklıİmza: if (artı) _specialCd[Kadro.İMZA] = 0f; break;                    // KARŞILIK İMZASI
                    case ComboTür.KarşıHaciz: if (artı && _sonVurulan != null) _sonVurulan.Sustur(1.5f); break;      // KARŞI TEBLİĞ
                    case ComboTür.KuleHaczi:
                        if (artı && Kule is KiraKulesi haczKulesi && _sonVurulan != null)
                            _sonVurulan.Push(_sonVurulan.transform.position - haczKulesi.transform.position, 7f);     // ZORLA TAHLİYE
                        break;
                    case ComboTür.GizliOrtaklık: if (artı) _specialCd[Kadro.ARBİTRAJ] = 0f; break;                  // GİZLİ PAY
                }
            }
            else İsabetSonrası(hits);

            // FAİZLİ yükseltmeleri (Faz 29): BİLEŞİK öldürme iadesini ×2'den ×3'e çıkarıyor,
            // TEMERRÜT ıskalayınca bedelin yarısını geri veriyor.
            var saat = Clock.Instance;
            if (saat != null && Y(SilahYükseltme.Bileşik) && kills > 0) saat.Add(_t.faizliSwingCost * kills);
            if (saat != null && Y(SilahYükseltme.Temerrüt) && hits == 0) saat.Add(_t.faizliSwingCost * 0.5f);
            // Faz 33: FAİZ ORANI iade ×2 → ×2.6 · FAİZSİZ KREDİ isabet bedelsiz · SIÇRAMA öldürünce yanındakine
            if (saat != null && Y(SilahYükseltme.FaizOranı) && kills > 0) saat.Add(_t.faizliSwingCost * 0.6f * kills);
            if (saat != null && Y(SilahYükseltme.FaizsizKredi) && hits > 0) saat.Add(_t.faizliSwingCost);
            if (Y(SilahYükseltme.Sıçrama) && kills > 0) Sıçrat(hasar, hasat, _sonVurulan);
            // İFLAS · KURTARMA: dipteyken öldürünce +3 sn (doyma tavanına tabi)
            if (saat != null && Y(SilahYükseltme.Kurtarma) && kills > 0 && DipOranı() >= 0.5f) saat.Add(3f * kills * saat.HasatDoyması);
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
            Vector3? hat = null;
            Transform yapış = null;
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
            else if (ComboHazır(ComboTür.CiroHattı))                                 // CİRO HATTI (Faz 33c)
            {
                hat = AdımYeri(1);
                if (Combo.Artı(ComboTür.CiroHattı)) _specialCd[Kadro.İMZA] = 0f;    // HIZLI CİRO
                ComboTetikle(ComboTür.CiroHattı);
            }
            else if (ComboHazır(ComboTür.İpotekliRehin))                             // İPOTEKLİ REHİN (Faz 42): rehinenin düştüğü yere
            {
                yer = AdımYeri(1); gecikme *= 0.1f;
                if (Combo.Artı(ComboTür.İpotekliRehin)) _specialCd[Kadro.İPOTEK] = 0f;    // İKİNCİ DERECE
                ComboTetikle(ComboTür.İpotekliRehin);
            }
            else if (Kule != null && ComboHazır(ComboTür.KiralıkArsa))               // KİRALIK ARSA (Faz 42): kule ateşiyle hızlı patlar
            {
                gecikme *= 0.3f; hasar *= 1.5f;
                if (Combo.Artı(ComboTür.KiralıkArsa)) Kule.KirasızKalan = Mathf.Max(Kule.KirasızKalan, 4f);   // MUAFİYET
                ComboTetikle(ComboTür.KiralıkArsa);
            }
            else if (ComboHazır(ComboTür.KonutKredisi) && AdımHedefi(1) is Enemy borçlu && Enemy.All.Contains(borçlu))   // KONUT KREDİSİ (Faz 42)
            {
                yer = borçlu.transform.position; yapış = borçlu.transform;
                if (Combo.Artı(ComboTür.KonutKredisi)) _specialCd[Kadro.İPOTEK] = 0f;     // ERKEN ÖDEME
                ComboTetikle(ComboTür.KonutKredisi);
            }
            else if (ComboHazır(ComboTür.TeminatAkçesi))                             // TEMİNAT AKÇESİ (Faz 42)
            {
                gecikme *= 0.5f; hasar += _sonBirikenHasarı; yarıçap *= 1f + Mathf.Min(1f, 0.25f * _sonBiriken);
                if (Combo.Artı(ComboTür.TeminatAkçesi)) _specialCd[Kadro.KARŞILIK] = 0f;   // DEPO
                ComboTetikle(ComboTür.TeminatAkçesi);
            }
            else if (ComboHazır(ComboTür.İhtiyatiTedbir) && AdımHedefi(1) is Enemy tedbirli && Enemy.All.Contains(tedbirli))   // İHTİYATİ TEDBİR (Parti 3)
            {
                yer = tedbirli.transform.position;
                tedbirli.Yavaşlat(2f, 0f);                                             // 2 sn yerine çakılı
                if (Combo.Artı(ComboTür.İhtiyatiTedbir)) tedbirli.Sustur(2f);          // TEDBİR ŞERHİ
                ComboTetikle(ComboTür.İhtiyatiTedbir);
            }
            else if (ComboHazır(ComboTür.VadeliTapu))                                   // VADELİ TAPU (Parti 3)
            {
                gecikme *= Mathf.Max(0.25f, 1f - 0.25f * _sonKademe); hasar *= Mathf.Max(1f, _sonVadeÇarpanı);
                if (Combo.Artı(ComboTür.VadeliTapu)) _specialCd[Kadro.İPOTEK] = 0f;   // TAPU İSKONTOSU
                ComboTetikle(ComboTür.VadeliTapu);
            }
            else if (ComboHazır(ComboTür.İflasİpoteği))                                 // İFLAS İPOTEĞİ (Parti 4)
            {
                var iflaslı = AdımHedefi(1);
                yer = iflaslı != null && Enemy.All.Contains(iflaslı) ? iflaslı.transform.position : AdımYeri(1);
                gecikme *= 0.2f; hasar *= Mathf.Max(1f, İflasÇarpanı());
                if (Combo.Artı(ComboTür.İflasİpoteği)) _specialCd[Kadro.İFLAS] = 0f;  // MÜHLET
                ComboTetikle(ComboTür.İflasİpoteği);
            }
            else if (ComboHazır(ComboTür.Gizliİpotek) && Şart(ComboTür.Gizliİpotek))   // GİZLİ İPOTEK (Parti 4): ARBİTRAJ arkadandı
            {
                var gizli = AdımHedefi(1);
                bool canlı = gizli != null && Enemy.All.Contains(gizli);
                yer = canlı ? gizli.transform.position : AdımYeri(1);
                if (canlı && Combo.Artı(ComboTür.Gizliİpotek)) yapış = gizli.transform;   // TAKİPLİ TAPU
                _görünmezKalan = Mathf.Max(_görünmezKalan, 1.5f); Paint();
                ComboTetikle(ComboTür.Gizliİpotek);
            }
            var işaret = PlaceGroundMark(Weapon.Range(_t), yarıçap, gecikme, hasar, Weapon.HarvestMultiplier, yer, yapış);
            if (işaret != null) { işaret.İtme = itme; ZincireYaz(_weaponIndex, işaret.transform.position); }
            if (hat.HasValue)                                                        // çekin yolu boyunca üç küçük işaret
                for (int i = 1; i <= 3; i++)
                    GroundMark.SpawnLien(_t, Vector3.Lerp(transform.position, hat.Value, i / 3f), yarıçap * 0.6f,
                                         gecikme * 0.6f, hasar * 0.7f, Weapon.HarvestMultiplier, OdaKökü);
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
            _arkaHasar = 0f; _arkadanÖldürdü = false;
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
                    Etkile(nearest);
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
                if (Y(SilahYükseltme.Kalabalık) && _hedefler.Count >= 2) harvestMult *= 1.3f;   // KALABALIK (Faz 33)
                // Çok hedefli vuruşta da "son vurulan" en yakını — zincirin adım hedefi buradan (Faz 42: KARŞILIK'ın
                // adımı hedefsiz kalıyordu, KARŞI HAMLE hiç tetiklenmiyordu). Vuruştan ÖNCE: öldürebilir.
                float enYakın = float.MaxValue;
                foreach (var (e, d) in _hedefler) if (d < enYakın) { enYakın = d; _sonVurulan = e; }
                foreach (var (e, _) in _hedefler)
                {
                    bool wasHollow = e.Hollow;
                    _isabetToplam += e.transform.position;        // ÖNCE: vuruş öldürüp yok edebilir
                    Etkile(e);
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
            if (hits > 0 && _weaponIndex < _sonHasar.Length) _sonHasar[_weaponIndex] = damage;   // KONKORDATO (33c)
            if (hits > 0 && _weaponIndex == Kadro.KONİ && maxTargets > 1)                       // SÜRÜ (33c)
            {
                _koniVurulanlar.Clear();
                foreach (var (e, _) in _hedefler) _koniVurulanlar.Add(e);
            }
            // ARBİTRAJ (Faz 33b) — her isabet yolu buradan geçiyor (savurma, dash-atak, TERS İŞLEM, combo):
            // YAYILMA arkadan vuruşun yarısı yanındakine · KISA POZİSYON arkadan öldürünce AÇIK POZİSYON hazır.
            if (_arkaHasar > 0f && Y(SilahYükseltme.Yayılma)) Sıçrat(_arkaHasar * 0.5f, harvestMult, _sonVurulan);
            if (_arkadanÖldürdü && Y(SilahYükseltme.KısaPozisyon)) _specialCd[_weaponIndex] = 0f;
            _arkaHasar = 0f; _arkadanÖldürdü = false;
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
                                   float hasar, float hasat, Vector3? yer = null, Transform takip = null)
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
            return İpotekYükselt(GroundMark.SpawnLien(_t, nokta, yarıçap, gecikme, hasar, hasat, OdaKökü, takip));
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
            İpotekYükselt(GroundMark.SpawnLien(_t, yer, sp.GroundRadius * (Y(SilahYükseltme.GenişArsa) ? 1.3f : 1f),
                                 sp.GroundDelay * (Y(SilahYükseltme.HızlıTapu) ? 0.7f : 1f),
                                 WeaponDamage() * sp.DamageMul, Weapon.HarvestMultiplier * sp.HarvestMul,
                                 oda != null ? oda : transform.parent));
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
                damage *= k;
                if (k >= _t.arbitrajBackMul)
                {
                    ArkadanVuruş++;
                    _sonArkadanAn = _comboAn;                                                 // SPEKÜLASYON (Faz 33c)
                    _arkaHasar = damage;                                                      // YAYILMA (Faz 33b)
                    _arkadanÖldürdü |= e.Hollow;                                              // KISA POZİSYON: boşu vurmak öldürür
                }
            }
            if (e == null) return damage;
            Vector3 to = e.transform.position - transform.position;
            to.y = 0f;
            float yüzey = Mathf.Max(0f, to.magnitude - e.Radius);
            if (special == null && Y(SilahYükseltme.SonSöz) && yüzey >= SavurmaMenzili * 0.75f) damage *= 1.4f;   // SON SÖZ (Faz 33)
            if (special == null || special.DamagePerDistance <= 0f) return damage;
            return damage * (1f + yüzey * special.DamagePerDistance * (Y(SilahYükseltme.UzunVadeArtı) ? 1.5f : 1f));   // UZUN VADE+
        }

        /// <summary>İsabetin düşmana kalıcı etkileri (Faz 33): KONİ · YORGUNLUK, HACİZ · İHTAR. Vuruştan ÖNCE (öldürebilir).</summary>
        void Etkile(Enemy e)
        {
            if (Y(SilahYükseltme.Yorgunluk)) e.Yor(3f);
            if (Y(SilahYükseltme.İhtar)) e.Yavaşlat(1.5f, 0.5f);
        }

        /// <summary>
        /// İPOTEK işaretinin yükseltmeleri (Faz 33) — normal ipotek de YAĞMUR damlası da buradan:
        /// DERİN TEMEL, ÇEKİM, BOŞ ARSA (boşa patlarsa sağ tık −2 sn), ZİNCİR TAPU (ikinci işaret).
        /// </summary>
        GroundMark İpotekYükselt(GroundMark m)
        {
            if (m == null) return null;
            if (Y(SilahYükseltme.DerinTemel)) m.MerkezÇarpanı = 1.5f;
            if (Y(SilahYükseltme.Çekim)) m.Çekim = 3f;
            bool boş = Y(SilahYükseltme.BoşArsa), zincir = Y(SilahYükseltme.ZincirTapu);
            if (boş || zincir)
            {
                int silah = _weaponIndex;
                m.Patladı += p =>
                {
                    if (boş && p.EnemiesHit == 0 && silah < _specialCd.Length) _specialCd[silah] -= 2f;
                    if (zincir && !p.İkincil)
                        for (int i = 0; i < p.VurulanYerler.Count && i < 3; i++)                // en çok 3
                        {
                            var ikinci = GroundMark.SpawnLien(_t, p.VurulanYerler[i], p.Yarıçap * 0.6f, 0.5f, p.Hasar, p.HasatÇarpanı, OdaKökü);
                            if (ikinci != null) ikinci.İkincil = true;
                        }
                };
            }
            return m;
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
                Biriktir(secondsCost * (Y(SilahYükseltme.TamKarşılık) ? 1.5f : 1f));   // BİRİKİM ilk karşılamadan sayar
                if (Y(SilahYükseltme.Red) && saldıran != null) saldıran.Sustur(1f);   // RED (Faz 33b)
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
                    _rehine.TakeDamage(secondsCost * _t.rehinShieldDamagePerSecond * (Y(SilahYükseltme.SıkıTutuş) ? 0.5f : 1f), 0f);
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
            secondsCost *= Enemy.CezaÇarpanı();                                                // CEZAİ ŞART (Faz 37): zincir
            if (Y(SilahYükseltme.Cesaret) && DipOranı() >= 0.5f) secondsCost *= 0.8f;          // CESARET (Faz 33)
            float inen = clock != null
                       ? Mathf.Min(Mathf.Max(0f, secondsCost), Mathf.Max(0f, clock.Seconds))
                       : secondsCost;

            // ⚠️ Hesaba saat düşmeden ÖNCE yaz: saat sıfırlanırsa ölüm sırası
            // (RevivalSystem) AYNI çağrının içinde defteri diske yazıyor.
            // Unity null'ı için `?.` değil açık kontrol: saldıran yok edilmiş olabilir.
            if (saldıran != null) saldıran.HesabınaYaz(inen);

            if (clock != null) clock.Spend(secondsCost, fromHit: true);   // oda hesabında AYRI sayılıyor
            if (inen > 0f) Enemy.CezaYaz();                                // CEZAİ ŞART (Faz 37): darbe indi, zincir uzar

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
