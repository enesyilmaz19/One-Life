#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TekCan
{
    /// <summary>
    /// OTOPİLOT — oyunu Play modunda, GERÇEK oyun döngüsünde oynayan bot (Faz 25b).
    ///
    /// Enes: *"bilgisayarımı kontrol edip bu testleri sen yapar mısın, matematik ve kod
    /// üstünden değil de direkt Unity üzerinden."* Kapılar (Faz*SelfCheck) zamanı elle
    /// ilerletiyor; bu, oyunun KENDİ karelerinde, kendi `Update`'leriyle koşuyor.
    ///
    /// ⚠️ Tuşlara Unity'nin SANAL klavye ve faresiyle basıyor (Input System cihazı),
    /// işletim sisteminden değil. `PlayerController` onu Enes'in klavyesi gibi okuyor,
    /// yani oyuncunun gerçek girdi yolu da sınanıyor. (CLAUDE.md: işletim sistemi
    /// otomasyonunun tuşları Play moduna geçmiyordu.)
    ///
    /// ⚠️ Enes'in KAYDINA DOKUNMAZ: etkinken kayıt geçici dosyaya gidiyor.
    /// ⚠️ Yalnızca editörde derleniyor — oyunun build'ine girmez.
    ///
    /// Başlatma: `Tek Can → Otopilot (izle)` ya da batch'ten
    /// `-executeMethod TekCan.EditorTools.OtopilotMenu.Batch` (`-quit` YOK, bot kendisi çıkar).
    /// Çıktı: `Logs/Otopilot/<zaman>/rapor.md` + ekran görüntüleri.
    /// </summary>
    public class Otopilot : MonoBehaviour
    {
        // ── oturum (Play'den çıkıp girince — K1 — da yaşıyor) ─────────────────
        public const string Aktif = "TekCan.Otopilot.Aktif";
        public const string İzleAnahtar = "TekCan.Otopilot.Izle";
        public const string AdımAnahtar = "TekCan.Otopilot.Adim";
        public const string YenidenGir = "TekCan.Otopilot.YenidenGir";
        public const string KlasörAnahtar = "TekCan.Otopilot.Klasor";
        public const string RaporAnahtar = "TekCan.Otopilot.Rapor";
        public const string HataAnahtar = "TekCan.Otopilot.Hata";
        public const string SSAnahtar = "TekCan.Otopilot.SS";
        /// <summary>"p26" = Faz 26 playtest senaryosu (Enes 2026-10-11: "playtesti de bot ile yap").</summary>
        public const string SenaryoAnahtar = "TekCan.Otopilot.Senaryo";
        const string K1Oda = "TekCan.Otopilot.K1Oda", K1Bölge = "TekCan.Otopilot.K1Bolge",
                     K1Saat = "TekCan.Otopilot.K1Saat", K1Kapanış = "TekCan.Otopilot.K1Kapanis";

        public static string KayıtYolu => Path.Combine(Path.GetTempPath(), "tekcan_otopilot.json");

        /// <summary>İnsan modeli: uyarıyı görüp harekete geçme süresi.</summary>
        const float İnsanTepkisi = 0.25f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void KaydıKoru()
        {
            // ⚠️ Bootstrap'tan (AfterSceneLoad) ÖNCE: ilk Load bile geçici dosyadan.
            if (SessionState.GetBool(Aktif, false)) SaveFile.PathOverride = KayıtYolu;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Doğ()
        {
            if (SessionState.GetBool(Aktif, false)) new GameObject("Otopilot").AddComponent<Otopilot>();
        }

        // ── sanal girdi ──────────────────────────────────────────────────────
        Keyboard _kb;
        Mouse _fare;
        InputSettings _eskiAyar;
        readonly HashSet<Key> _tutulan = new HashSet<Key>();
        readonly HashSet<Key> _birKare = new HashSet<Key>();
        bool _sol, _sağ;
        Vector2 _fareNoktası;

        void OnEnable() => Application.logMessageReceived += Günlük;
        void OnDisable() => Application.logMessageReceived -= Günlük;

        /// <summary>
        /// Bot ya da OYUN istisna fırlatırsa: kırmızı yaz, bitir. Koroutin zinciri istisnada
        /// ölüyor; bu olmasa batch 30 dk bekçiyi bekliyordu (inceleme). Oyunun kendi
        /// istisnası da gerçek bir hata — sessizce geçilmemeli.
        /// </summary>
        void Günlük(string mesaj, string yığın, LogType tür)
        {
            if (tür != LogType.Exception || _bitti) return;
            Kaydet("⚠", "İstisna", false, mesaj);
            StartCoroutine(Bitir());
        }

        InputSettings _ayar;

        void Awake()
        {
            // ⚠️ Editör, Oyun penceresi odakta değilken klavye/fare girdisini oyuna
            // geçirmiyor. Bu oturum için kopya ayarla "her girdi oyuna" — asıl ayar
            // dokunulmadan geri konuyor.
            _eskiAyar = InputSystem.settings;
            var ayar = _ayar = Instantiate(_eskiAyar);
            ayar.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            // ⚠️ Batch'te pencere hiç ODAKTA olmuyor; varsayılan davranış odak yokken
            // klavye/fareyi kapatıyor (ilk koşuda bot bu yüzden kıpırdayamadı).
            ayar.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings = ayar;

            _kb = InputSystem.AddDevice<Keyboard>("OtopilotKlavye");
            _fare = InputSystem.AddDevice<Mouse>("OtopilotFare");
            _kb.MakeCurrent();
            _fare.MakeCurrent();
            _izle = SessionState.GetBool(İzleAnahtar, false) && !Application.isBatchMode;
        }

        void OnDestroy()
        {
            if (_kb != null) InputSystem.RemoveDevice(_kb);
            if (_fare != null) InputSystem.RemoveDevice(_fare);
            if (_eskiAyar != null) InputSystem.settings = _eskiAyar;
            if (_ayar != null) Destroy(_ayar);
        }

        /// <summary>
        /// Kararlar koroutinde (Update'ten SONRA) veriliyor; burada sanal cihaza
        /// yazılıyor, oyun bir sonraki karede okuyor — insan elinin gecikmesi gibi.
        /// </summary>
        void LateUpdate()
        {
            if (_kb == null) return;
            var tuşlar = new List<Key>(_tutulan);
            tuşlar.AddRange(_birKare);
            _birKare.Clear();
            InputSystem.QueueStateEvent(_kb, new KeyboardState(tuşlar.ToArray()));

            var fs = new MouseState { position = _fareNoktası }
                .WithButton(MouseButton.Left, _sol).WithButton(MouseButton.Right, _sağ);
            _sol = _sağ = false;
            InputSystem.QueueStateEvent(_fare, fs);

            if (Keyboard.current != _kb) _kb.MakeCurrent();
            if (Mouse.current != _fare) _fare.MakeCurrent();

            // Bekçi: bot takılırsa süreç sonsuza dek açık kalmasın.
            if (!_bitti && Time.realtimeSinceStartup - _başlangıç > 30f * 60f)
            {
                Kaydet("⏱", "Bekçi", false, "30 dk doldu — bot takıldı");
                StartCoroutine(Bitir());
            }
        }

        void Bas(Key k) => _birKare.Add(k);

        void Yürü(Vector3 dünyaYönü)
        {
            _tutulan.Remove(Key.W); _tutulan.Remove(Key.A);
            _tutulan.Remove(Key.S); _tutulan.Remove(Key.D);
            dünyaYönü.y = 0f;
            var kam = Kamera;
            if (dünyaYönü.sqrMagnitude < 0.0001f || kam == null) return;
            dünyaYönü.Normalize();
            Vector3 f = kam.transform.forward; f.y = 0f; f.Normalize();
            Vector3 r = kam.transform.right; r.y = 0f; r.Normalize();
            float x = Vector3.Dot(dünyaYönü, r), y = Vector3.Dot(dünyaYönü, f);
            if (x > 0.38f) _tutulan.Add(Key.D);
            if (x < -0.38f) _tutulan.Add(Key.A);
            if (y > 0.38f) _tutulan.Add(Key.W);
            if (y < -0.38f) _tutulan.Add(Key.S);
        }

        void Dur() => Yürü(Vector3.zero);

        void NişanAl(Vector3 dünya)
        {
            var kam = Kamera;
            if (kam == null) return;
            var p = kam.WorldToScreenPoint(dünya);
            _fareNoktası = new Vector2(p.x, p.y);
        }

        // ── oyunun parçaları (yeniden kurulumda değişir, Unity null'ı ile) ──────
        PlayerController _pc;
        Camera _kam;
        Tuning _t;
        PlayerController Oyuncu => _pc != null ? _pc : (_pc = FindFirstObjectByType<PlayerController>());
        Camera Kamera
        {
            get
            {
                if (_kam != null) return _kam;
                var iso = FindFirstObjectByType<IsoCamera>();
                return _kam = iso != null ? iso.GetComponent<Camera>() : null;
            }
        }
        static Clock Saat => Clock.Instance;
        static string OdaNo => Oda != null ? Oda.Room.ToString() : "?";
        static string BossAdı(Enemy b) => b != null && b.Boss != null ? b.Boss.Name : "yok";
        static RoomManager Oda => RoomManager.Instance;

        static Vector3 Düz(Vector3 v) { v.y = 0f; return v; }
        Vector3 Ben => Oyuncu != null ? Oyuncu.transform.position : Vector3.zero;

        // ── rapor ─────────────────────────────────────────────────────────────
        bool _izle, _bitti;
        float _başlangıç;
        string _sahne = "", _ne = "";

        string Klasör => SessionState.GetString(KlasörAnahtar, Path.Combine("Logs", "Otopilot", "son"));

        void Kaydet(string no, string ad, bool? geçti, string ölçüm)
        {
            string işaret = geçti == null ? "ℹ️" : geçti.Value ? "✅" : "❌";
            string satır = $"| {no} | {ad} | {işaret} | {ölçüm.Replace("|", "/")} |";
            SessionState.SetString(RaporAnahtar, SessionState.GetString(RaporAnahtar, "") + satır + "\n");
            if (geçti == false) SessionState.SetInt(HataAnahtar, SessionState.GetInt(HataAnahtar, 0) + 1);
            Debug.Log($"[OTOPİLOT] {işaret} {no} {ad} — {ölçüm}");
        }

        void Sahne(string sahne, string ne = "")
        {
            _sahne = sahne; _ne = ne;
            Debug.Log($"[OTOPİLOT] ▶ {sahne} {ne}");
        }

        /// <summary>
        /// Ekran görüntüsü — yalnızca izleme modunda (batch'te Oyun penceresi yok).
        /// ⚠️ Bot görüntüyü KENDİSİ alıyor, tam anında: uyarı yayının donduğu kare
        /// saniyenin onda biri; dışarıdan ekran görüntüsü o anı yakalayamaz.
        /// </summary>
        void SS(string ad)
        {
            if (!_izle) return;
            Directory.CreateDirectory(Klasör);
            int no = SessionState.GetInt(SSAnahtar, 0) + 1;       // K1 yeniden açılışında da sürsün
            SessionState.SetInt(SSAnahtar, no);
            string yol = Path.Combine(Klasör, $"{no:00}_{ad}.png");
            ScreenCapture.CaptureScreenshot(yol);
            SessionState.SetString(RaporAnahtar, SessionState.GetString(RaporAnahtar, "") +
                                   $"|   | 📷 {ad} | | `{Path.GetFileName(yol)}` |\n");
        }

        void OnGUI()
        {
            if (!_izle) return;
            var st = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter, richText = true };
            st.normal.textColor = Color.white;
            GUI.Box(new Rect(12f, 70f, 620f, 64f),          // sol üst: saat ortada, onu örtmesin
                    $"🤖 OTOPİLOT · {_sahne}\n<size=16>{_ne}</size>", st);
        }

        // ── senaryo ───────────────────────────────────────────────────────────

        IEnumerator Start()
        {
            _başlangıç = Time.realtimeSinceStartup;
            _t = Resources.Load<Tuning>("Tuning");
            yield return null; yield return null;           // Bootstrap kurulsun

            bool p26 = SessionState.GetString(SenaryoAnahtar, "") == "p26";
            IEnumerator[] sahneler = p26
                ? new[] { Kurulum(), P26Tahsildar(), P26Kefil(), P26Simsar(), P26Murabahacı(), Bitir() }
                : SessionState.GetInt(AdımAnahtar, 0) == 0
                ? new[] { Kurulum(), GirdiKanıtı(), K1Hazırla() }          // K1: Play'den çıkıp giriyor
                : new[] { K1Doğrula(), Bölge1(), Sunak(), Tahsildar(), Haciz(), Simsar(), Final(), Bitir() };
            foreach (var sahne in sahneler)
            {
                if (_bitti) yield break;                     // bir sahne botu durdurduysa
                yield return sahne;
            }
        }

        IEnumerator Kurulum()
        {
            Sahne("0/9 KURULUM", "F6: sıfırdan, temiz koşu");
            SessionState.SetInt(HataAnahtar, 0);
            Bas(Key.F6);
            yield return Bekle(0.6f);
            _pc = null; _kam = null;
        }

        // 1) Bot gerçekten oynayabiliyor mu
        IEnumerator GirdiKanıtı()
        {
            Sahne("1/9 GİRDİ", "yürü · dash · vur");
            yield return Bekle(0.3f);
            var pc = Oyuncu;
            Vector3 p0 = Ben;
            var kam = Kamera;
            Vector3 ileri = kam != null ? Düz(kam.transform.forward).normalized : Vector3.forward;
            Yürü(ileri);
            yield return Bekle(0.5f);
            Dur();
            float yürüdü = Düz(Ben - p0).magnitude;

            // ⚠️ Yön TEK KARE tutulup bırakılıyor: tutulsaydı 0.25 sn yürümek de ~1.6 birim
            // ederdi ve dash hiç atılmasa bile eşik geçilirdi (inceleme). Dash bırakınca da sürer.
            Vector3 p1 = Ben;
            yield return TekKareDash(-ileri);
            float dash = Düz(Ben - p1).magnitude;

            int savurma = pc != null ? pc.SwingsSpawned : 0;
            NişanAl(Ben + ileri * 3f);
            yield return null;
            _sol = true;
            yield return Bekle(0.5f);
            int savurdu = (pc != null ? pc.SwingsSpawned : 0) - savurma;

            bool ok = yürüdü > 1f && dash > _t.dashDistance * 0.8f && savurdu > 0;
            Kaydet("1", "Girdi: sanal klavye/fare oyuna geçiyor", ok,
                   $"0.5 sn W → {yürüdü:0.0} birim · dash → {dash:0.0} birim · sol tık → {savurdu} savurma");
            if (!ok)
            {
                Kaydet("1", "Girdi geçmiyor — otopilot durduruldu", false,
                       $"klavye {Keyboard.current?.name} (bizim: {Keyboard.current == _kb}) · etkin {_kb.enabled} · " +
                       $"odak {Application.isFocused} · oyuncu {(pc != null)} · güncelleme {InputSystem.settings.updateMode}");
                yield return Bitir();
            }
        }

        // 2a) K1: temiz koşuda oda 2'ye gir, zaman kaybet, Play'den çık
        IEnumerator K1Hazırla()
        {
            Sahne("2/9 K1 — ODA BAŞI KAYIT", "oda 1'i temizle, oda 2'ye gir");
            yield return OdayıGeç(90f);
            var s = SaveFile.Current;
            float giriş = s != null ? s.resumeSeconds : -1f;
            int oda = Oda != null ? Oda.Room : -1, bölge = Oda != null ? Oda.RegionIndex : -1;
            Sahne("2/9 K1 — ODA BAŞI KAYIT", $"oda {oda}'ye {giriş:0} sn ile girdim. Bekleyip zaman kaybediyorum…");
            // ⚠️ DURUP bekliyor: saat girişten en az 1 sn uzaklaşmalı, yoksa açılışta okunan
            // değerin "giriş" mi "kapanıştaki canlı saat" mi olduğu ayırt edilemez (inceleme:
            // önceki koşuda 93.2'ye karşı 93.3'tü).
            float t0 = Time.realtimeSinceStartup;
            while (Saat != null && giriş - Saat.Seconds < 1.5f && Time.realtimeSinceStartup - t0 < 10f)
            { Dur(); Gözler(Ben); yield return null; }
            float kapanış = Saat != null ? Saat.Seconds : -1f;
            SS("k1_kapatmadan_once");
            bool kurgu = oda >= 2 && !SaveFile.TestJumpUsed && giriş - kapanış >= 1f;
            Kaydet("2", "test kurgusu: K1 (oda ≥ 2, temiz koşu, saat ≥ 1 sn aktı)", kurgu,
                   $"oda {oda}, girişte {giriş:0.0} sn · kapatırken {kapanış:0.0} sn · kirli {SaveFile.TestJumpUsed}");
            SessionState.SetFloat(K1Kapanış, kapanış);

            SessionState.SetInt(K1Oda, oda);
            SessionState.SetInt(K1Bölge, bölge);
            SessionState.SetFloat(K1Saat, giriş);
            SessionState.SetInt(AdımAnahtar, 1);
            Sahne("2/9 K1 — ODA BAŞI KAYIT", "oyunu KAPATIYORUM (Play'den çık), sonra yeniden açacağım");
            yield return Bekle(1f);
            // ⚠️ Çıkıştan HEMEN önce: önceden 1 sn erken konuyordu, o arada Stop'a basan
            // kişiyi editör botla birlikte Play'e geri sokardı (inceleme).
            SessionState.SetBool(YenidenGir, true);
            EditorApplication.isPlaying = false;
        }

        // 2b) K1: açılışta aynı odanın başında, girdiği zamanla mı
        IEnumerator K1Doğrula()
        {
            int oda = SessionState.GetInt(K1Oda, -1), bölge = SessionState.GetInt(K1Bölge, -1);
            float saat = SessionState.GetFloat(K1Saat, -1f);
            Sahne("2/9 K1 — ODA BAŞI KAYIT", "yeniden açıldı: neredeyim?");
            // ⚠️ HEMEN ölç. İlk koşuda yarım saniye bekleyip ölçtüm, saat o arada 1.8 sn
            // aktı ve doğru çalışan K1 kırmızı yandı — botun ölçüm hatası, oyunun değil.
            float görülen = Saat != null ? Saat.Seconds : -1f;
            int görülenOda = Oda != null ? Oda.Room : -1;
            float kapanış = SessionState.GetFloat(K1Kapanış, -1f);
            // ⚠️ "Girişe kapanıştan YAKIN mı": ekranlı editörde ilk kareler ağır, saat açılışta
            // 0.4 sn akabiliyor (izleme oturumu, 2026-10-09) — sabit 0.3 pay kırmızı yakıyordu.
            bool ok = Oda != null && görülenOda == oda && Oda.RegionIndex == bölge
                   && Mathf.Abs(görülen - saat) < 1f
                   && Mathf.Abs(görülen - saat) < Mathf.Abs(görülen - kapanış);
            Kaydet("2", "K1: kapat-aç → girdiğin odanın başı, girdiğin zaman", ok,
                   $"beklenen oda {oda} / {saat:0.0} sn (kapanışta {kapanış:0.0}) · görülen oda {görülenOda} / {görülen:0.0} sn");
            yield return null;
            SS("k1_acilista");
        }

        // 3) Bölge 1'in kalan odaları: ZIRHLI/İRİ vuruş başına ölçüm
        readonly Dictionary<Enemy, float> _sonHavuz = new Dictionary<Enemy, float>();
        readonly Dictionary<string, List<float>> _rütbeDüşüş = new Dictionary<string, List<float>>();

        IEnumerator Bölge1()
        {
            Sahne("3/9 BÖLGE 1", "odaları temizliyorum · ZIRHLI/İRİ vuruşlarını ölçüyorum");
            _rütbeÖlç = true;
            for (int i = 0; i < 6 && Oda != null && !Oda.IsAltarRoom; i++)
                yield return OdayıGeç(120f);
            _rütbeÖlç = false;

            float Ort(string r) => _rütbeDüşüş.TryGetValue(r, out var l) && l.Count > 0 ? Ortalama(l) : -1f;
            float n = Ort("normal"), z = Ort("ZIRHLI"), i2 = Ort("İRİ");
            string ölçüm = $"vuruş başına havuz düşüşü — normal {Göster(n)} · ZIRHLI {Göster(z)} · İRİ {Göster(i2)}";
            if (n > 0f && z > 0f)
                Kaydet("3", "ZIRHLI vuruş başına AZ eriyor", z < n * 0.75f, ölçüm);
            else
                Kaydet("3", "ZIRHLI / İRİ ölçümü", null, ölçüm + " (ZIRHLI ya da normal görülmedi)");
        }

        bool _rütbeÖlç;

        void RütbeÖlç()
        {
            foreach (var e in Enemy.All)
            {
                if (e == null || e.IsBoss) continue;
                float önce;
                if (_sonHavuz.TryGetValue(e, out önce) && önce - e.TimeLeft > 0.3f)
                {
                    string r = string.IsNullOrEmpty(e.Rank.Name) ? "normal" : e.Rank.Name;
                    if (!_rütbeDüşüş.TryGetValue(r, out var l)) _rütbeDüşüş[r] = l = new List<float>();
                    if (!e.Hollow) l.Add(önce - e.TimeLeft);
                }
                _sonHavuz[e] = e.TimeLeft;
            }
        }

        // 4) Sunak odası: geliştirme al
        IEnumerator Sunak()
        {
            Sahne("4/9 SUNAK ODASI", "saat durdu mu · her sunaktan alıyorum");
            yield return Bekle(0.6f);
            if (Oda == null || !Oda.IsAltarRoom)
            {
                Kaydet("4", "Sunak odası", false, $"sunak odasına varılamadı (oda {OdaNo})");
                yield break;
            }
            if (Saat.Seconds < 60f) { Bas(Key.F5); yield return Bekle(0.3f); }
            SS("sunak_odasi");
            float saat0 = Saat.Seconds;
            yield return Bekle(1f);
            float saat1 = Saat.Seconds;
            bool durdu = Mathf.Abs(saat1 - saat0) < 0.05f;

            float hasat0 = Saat.HarvestRate, hasar0 = Saat.AttackDamage, dash0 = Saat.DashCooldown;
            int alım0 = Saat.Purchases;
            float ödenen = 0f;
            bool fiyatArttı = true;
            foreach (var sunak in new List<Altar>(Altar.All))
            {
                if (sunak == null || sunak.Type == Altar.Kind.Repair) continue;
                _ne = $"{sunak.Type} sunağına yürüyorum";
                yield return Git(sunak.transform.position, 1.2f, 6f);
                for (int k = 0; k < 2 && !sunak.SoldOut; k++)
                {
                    float fiyat = sunak.Price, önce = Saat.Seconds;
                    int alım = Saat.Purchases;
                    Bas(Key.E);
                    yield return Bekle(0.25f);
                    if (Saat.Purchases > alım)
                    {
                        ödenen += önce - Saat.Seconds;
                        if (sunak.Price <= fiyat) fiyatArttı = false;
                    }
                }
            }
            SS("sunak_sonrasi");
            bool aldı = Saat.Purchases - alım0 >= 3;
            bool etki = Saat.HarvestRate > hasat0 && Saat.AttackDamage > hasar0 && Saat.DashCooldown < dash0;
            Kaydet("4", "Sunak: saat duruyor", durdu, $"1 sn bekledim, saat {saat0:0.0} → {saat1:0.0}");
            Kaydet("4", "Sunak: geliştirme alındı, etkisi var, fiyat artıyor", aldı && etki && fiyatArttı,
                   $"{Saat.Purchases - alım0} alım, {ödenen:0.0} sn ödendi · hasat %{hasat0 * 100:0}→%{Saat.HarvestRate * 100:0} · " +
                   $"hasar {hasar0:0.0}→{Saat.AttackDamage:0.0} · dash {dash0:0.00}→{Saat.DashCooldown:0.00} sn");
            yield return KapıdanÇık(20f);
        }

        // 5) TAHSİLDAR: uzak dur / dibe gir sıraları, tepki, NEMESİS
        IEnumerator Tahsildar()
        {
            Sahne("5/9 TAHSİLDAR", "uzakta duruyorum — gelen saldırıları yazıyorum");
            yield return Bekle(0.5f);
            var boss = Boss();
            if (boss == null) { Kaydet("5", "TAHSİLDAR", false, $"boss odası değil (oda {OdaNo})"); yield break; }
            if (Saat.Seconds < 100f) { Bas(Key.F5); yield return Bekle(0.3f); }

            var uzak = new List<string>();
            yield return SaldırıTopla(boss, Mod.UzakDur, 8, uzak, 70f);
            var beklenenUzak = Sıra(boss, SaldırıTürü.Uzak);
            var harita = Sıra(boss, SaldırıTürü.Harita);
            int farklıUzak = Farklı(uzak, beklenenUzak);
            // ⚠️ "Listede olmayan gelmiyor" şartı bozma turunda eklendi: iki sıra kaldırılınca
            // uzaktaki bota YAKIN saldırı (HACİZ) geldi, ama kontrol yalnızca çeşide baktığı
            // için yeşil kaldı (3/4) — kendi varsayımını ölçen kapı, ders 14.
            // ⚠️ Yalnızca bot GERÇEKTEN uzaktayken (≥ 6 birim) başlayanlar. Duvara sıkışan bota
            // boss yaklaşınca yakın saldırı DOĞRU davranış — oto26'da (2026-10-10) KUŞATMA
            // böyle geldi ve kontrol, botun hep uzakta kaldığını varsayıp kırmızı yaktı.
            bool uzaktaYabancı = false;
            for (int k = 0; k < uzak.Count; k++)
                if (_başMesafe[k] >= 6f && !beklenenUzak.Contains(uzak[k]) && !harita.Contains(uzak[k]))
                    uzaktaYabancı = true;
            Kaydet("5", "TAHSİLDAR uzakta: yalnızca menzilli (+ harita) saldırılar, hepsi",
                   farklıUzak >= beklenenUzak.Count - 1 && !uzaktaYabancı,
                   $"{farklıUzak}/{beklenenUzak.Count} farklı · görülen: {string.Join(", ", uzak)}");
            // Faz 26: her 4. saldırı harita — 8 saldırıda en az bir kez gelmeli.
            int haritaSayısı = uzak.FindAll(a => harita.Contains(a)).Count;
            Kaydet("5", "TAHSİLDAR harita saldırısı geliyor (her 4. saldırı)", haritaSayısı >= 1,
                   $"8 saldırıda {haritaSayısı} harita ({string.Join(", ", harita)})");

            Kaydet("5", "TAHSİLDAR saldırıları oyuncu ERİŞİMDEYKEN başlıyor (bağımsız ölçü)", _menzilDışı.Count == 0,
                   _menzilDışı.Count == 0 ? $"{uzak.Count} saldırının hepsi" : string.Join(", ", _menzilDışı));
            _menzilDışı.Clear();
            Sahne("5/9 TAHSİLDAR", "dibine giriyorum — yakın saldırıları yazıyorum");
            var yakın = new List<string>();
            _donukSS = true;
            yield return SaldırıTopla(boss, Mod.DibeGir, 6, yakın, 50f);
            var beklenenYakın = Sıra(boss, SaldırıTürü.Yakın);
            int farklıYakın = Farklı(yakın, beklenenYakın);
            bool dışarıda = yakın.Exists(a => !beklenenYakın.Contains(a) && !harita.Contains(a));
            Kaydet("5", "TAHSİLDAR dibinde: yalnızca yakın (+ harita) saldırılar, hepsi", farklıYakın == beklenenYakın.Count && !dışarıda,
                   $"{farklıYakın}/{beklenenYakın.Count} · görülen: {string.Join(", ", yakın)}");
            Kaydet("5", "ISRAR (dibinde, hep yakın)", null, $"en yüksek çarpan ×{_enYüksekIsrar:0.00}");

            // NEMESİS: bilerek öl, sonra dön
            Sahne("5/9 NEMESİS", "bilerek ölüyorum — boss büyüyecek mi?");
            float ölçek0 = boss.transform.localScale.x, havuz0 = _bossİlkHavuz;
            float defter0 = SaveFile.BossLedger(boss.Boss);
            int ölüm0 = SaveFile.Current.deaths;
            SS("nemesis_once");
            float t0 = Time.realtimeSinceStartup;
            while (SaveFile.Current.deaths == ölüm0 && Time.realtimeSinceStartup - t0 < 120f)
            {
                DövüşAdımı(Mod.Ölümüne, boss);
                yield return null;
            }
            float defter = SaveFile.BossLedger(boss.Boss);
            Dur();
            Sahne("5/9 NEMESİS", $"öldüm, defter {defter0:0} → {defter:0} sn. B ile boss'a dönüyorum");
            yield return Bekle(1f);
            Bas(Key.B);
            yield return Bekle(1f);
            var yeni = Boss();
            SS("nemesis_sonra");
            float yeniHavuz = yeni != null ? yeni.TimeLeft : -1f;
            float yeniÖlçek = yeni != null ? yeni.transform.localScale.x : -1f;
            float borç = yeni != null ? yeni.NemesisDebt : -1f;
            bool büyüdü = yeni != null && borç > 0f && yeniHavuz > havuz0 + 0.5f && yeniÖlçek > ölçek0 + 0.01f;
            Kaydet("5", "NEMESİS: öldüren boss büyüyerek dönüyor", büyüdü,
                   $"defter {defter0:0.0}→{defter:0.0} sn · havuz {havuz0:0}→{yeniHavuz:0} · " +
                   $"gövde {ölçek0:0.00}→{yeniÖlçek:0.00} · borç {borç:0.0}");

            Sahne("5/9 TAHSİLDAR", "şimdi dövüşüyorum: uyarıyı 0.25 sn sonra görüp kaçıyorum");
            _tehdit = 0; _vuruldu = 0;
            yield return BossuÖldür(180f);
            Kaydet("5", "Tepki payı (TAHSİLDAR)", null, $"tehdit {_tehdit}, vurulma {_vuruldu}");
            yield return KapıdanÇık(20f);
        }

        // 6) HACİZ dash'i gittiğin yöne
        IEnumerator Haciz()
        {
            Sahne("6/9 HACİZ", "3'e bas, sağa dash at — sağa mı gidiyor?");
            Bas(Key.Digit3);
            yield return Bekle(0.3f);
            var kam = Kamera;
            Vector3 sağ = kam != null ? Düz(kam.transform.right).normalized : Vector3.right;
            NişanAl(Ben + sağ * 4f);                      // eski kod nişanın TERSİNE atardı
            yield return null;
            Vector3 p0 = Ben;
            yield return TekKareDash(sağ);
            float ilerleme = Vector3.Dot(Düz(Ben - p0), sağ);
            Kaydet("6", "HACİZ dash'i gittiğin yöne", ilerleme > _t.dashDistance * 0.8f,
                   $"sağa {ilerleme:0.0} birim (dash {_t.dashDistance:0.0})");
            Bas(Key.Digit1);
            yield return Bekle(0.2f);
        }

        // 7) SİMSAR: açıklık, turuncu işaret, faz 2
        IEnumerator Simsar()
        {
            Sahne("7/9 SİMSAR", "N, N, B");
            Bas(Key.N); yield return Bekle(0.5f);
            Bas(Key.N); yield return Bekle(0.5f);
            Bas(Key.B); yield return Bekle(1f);
            var boss = Boss();
            if (boss == null || boss.Boss.Name != "SİMSAR")
            {
                Kaydet("7", "SİMSAR", false, $"SİMSAR bulunamadı ({BossAdı(boss)})");
                yield break;
            }
            Sahne("7/9 SİMSAR", "dövüşüyorum · yeşil boşluğa koşuyorum · işaretlerden kaçıyorum");
            _simsar = boss; _işaretGörüldü = false; _açıklık = 0; _açıklıkVuruldu = 0;
            _faz2Önce = -1f; _faz2Kredi = false;
            _tehdit = 0; _vuruldu = 0;
            yield return BossuÖldür(240f);

            Kaydet("7", "SİMSAR işareti turuncu-kırmızı (çizilen renk)", _işaretGörüldü ? _işaretTuruncu : (bool?)null,
                   _işaretGörüldü ? $"ton {_işaretTon:0}°" : "işaret görülmedi");
            Kaydet("7", "SİMSAR faz 2: havuz BİTİNCE, fullenerek", _faz2Önce >= 0f ? _faz2Önce < 0.15f && _faz2Kredi : (bool?)null,
                   _faz2Önce >= 0f ? $"geçişten önce havuz %{_faz2Önce * 100:0}, kredi {_faz2Kredi}" : "faz 2 görülmedi");
            Kaydet("7", "SİMSAR açıklıklı saldırılar", _açıklık > 0 ? _açıklıkVuruldu * 2 <= _açıklık : (bool?)null,
                   $"{_açıklık} açıklıklı saldırı, {_açıklıkVuruldu} vurulma (bot boşluğa koşuyor) · toplam tehdit {_tehdit}, vurulma {_vuruldu}");
            _simsar = null;
        }

        // 8) Final: F7, faz 3, uyku rengi, zincir
        IEnumerator Final()
        {
            Sahne("8/9 FİNAL", "N, B, F7 TAM GÜÇ");
            Bas(Key.N); yield return Bekle(0.5f);
            Bas(Key.B); yield return Bekle(1f);
            Bas(Key.F7); yield return Bekle(0.3f);
            var boss = Boss();
            if (boss == null || boss.Boss.PhaseCount < 3)
            {
                Kaydet("8", "Final", false, $"son boss bulunamadı ({BossAdı(boss)})");
                yield break;
            }
            Sahne("8/9 FİNAL", "faz 3'e kadar dövüşüyorum");
            _final = boss; _uykuGörüldü = false; _uykuBaşı = -1f; _finalSaldırılar.Clear();
            _tehdit = 0; _vuruldu = 0;
            float t0 = Time.realtimeSinceStartup;
            while (boss != null && !(boss.Phase >= 3 && !boss.Dormant) && Time.realtimeSinceStartup - t0 < 300f)
            {
                if (Saat != null && Saat.Dead) { Kaydet("—", "bot öldü", false, _sahne); yield return Bitir(); yield break; }
                DövüşAdımı(Mod.Dövüş, Boss());
                yield return null;
            }
            Kaydet("8", "Final faz 3'e ulaşıldı", _uykuGörüldü, _uykuGörüldü ? "rakipler çağrıldı, boss uyudu ve uyandı" : "faz 3'e gelinemedi");
            if (_uykuGörüldü)
                Kaydet("8", "Uyuyan boss SOLUK (gerçek renderer rengi)", _uykuSoluk, _uykuRenk);

            // Faz 3, UZAKTA: menzilli sıra — 4 halkalı zincir burada.
            Sahne("8/9 FİNAL", "faz 3: uzakta duruyorum — zinciri bekliyorum");
            _finalSaldırılar.Clear();
            var uzak3 = new List<string>();
            yield return SaldırıTopla(boss, Mod.UzakDur, 10, uzak3, 90f);
            yield return Bekle(3f);                       // 10. saldırı zincirin başıysa halkaları gelsin
            Kaydet("8", "Faz 3 UZAKTA gelenler", null, string.Join(", ", uzak3));

            // Faz 3, DİBİNDE: yakın sıra — MURABAHACI kararı için (a/b/c).
            Sahne("8/9 FİNAL", "faz 3: dibine girip öldürüyorum — yakın saldırıları yazıyorum");
            var yakın3 = new List<string>();
            _yeniSaldırı = e => { if (e == boss && e.CurrentAttack != null) yakın3.Add(e.CurrentAttack.Name); };
            yield return BossuÖldür(300f, kazanınca: true);
            _yeniSaldırı = null;
            Kaydet("8", "Faz 3 DİBİNDE gelenler (MURABAHACI kararı)", null,
                   yakın3.Count > 0 ? string.Join(", ", yakın3) : "—");

            // 4 halkalı zincir — Faz 26'dan beri YAKIN sırasında (BİLEŞİK FAİZ++), uzakta
            // aramak kırmızı yakıyordu (oto26). Uzakta + dibinde bütün saldırılarda, boss
            // ölmeden tamamlanmış ilk zincir aranıyor.
            var tümü = new List<string>(_finalSaldırılar);
            List<string> halka = null;
            for (int i = tümü.IndexOf("BİLEŞİK FAİZ++"); i >= 0 && halka == null; i = tümü.IndexOf("BİLEŞİK FAİZ++", i + 1))
                if (i + 4 <= tümü.Count) halka = tümü.GetRange(i, 4);
            if (halka != null)
                Kaydet("8", "4 halkalı zincir sonuna kadar oynuyor", halka[3] == "…AYNA", string.Join(" → ", halka));
            else Kaydet("8", "4 halkalı zincir", false,
                        tümü.Contains("BİLEŞİK FAİZ++") ? "başladı ama boss halkalar bitmeden öldü" : "faz 3'te BİLEŞİK FAİZ++ hiç gelmedi");
            Kaydet("8", "Final", null, (Saat != null && Saat.Won ? "KAZANILDI" : "kazanılamadı (süre doldu)") +
                   $" · tehdit {_tehdit}, vurulma {_vuruldu}, süre takviyesi {_takviye}");
            _final = null;
        }

        // ── FAZ 26 PLAYTEST ───────────────────────────────────────────────────
        // Enes (2026-10-11): *"playtesti de bot ile yap, sorun çıkarsa düzelt."* todo.md
        // "Playtest — Faz 26"nın yedi maddesi, gerçek oyunda. Kapı (Faz26SelfCheck) ideal
        // insanla ADALETİ ölçüyor; bu, gerçek döngüde AKIŞI ve SERTLİĞİ ölçüyor.

        bool _p26;
        int _p26Orta, _p26Erişim, _p26Açıklık, _p26Nokta;
        readonly List<string> _p26OrtaHata = new List<string>(), _p26ErişimHata = new List<string>(),
                              _p26AçıklıkHata = new List<string>(), _p26NoktaHata = new List<string>();

        /// <summary>
        /// Her boss hazırlığının başında (tekrarlar dahil) üretimin verdiği sözü bağımsız ölçer:
        /// ortaya sıçrama, harita erişimi, açıklıkta boss ortaya yakın, güvenli nokta yetişilebilir.
        /// </summary>
        void P26Gözle(Enemy e, Vector3 ben, bool yeni)
        {
            var a = e.CurrentAttack;
            float merkezden = Düz(e.transform.position - Bootstrap.ArenaCenter).magnitude;
            float mesafe = Düz(ben - e.transform.position).magnitude;
            if (a.ArenaOranı > 0f)
            {
                _p26Orta++;
                if (merkezden > 0.6f) _p26OrtaHata.Add($"{a.Name}: merkezden {merkezden:0.0}");
            }
            // Erişim ve açıklık yalnızca YENİ saldırıda seçiliyor; tekrarlar zorunlu.
            if (yeni && a.Harita && a.MaxPlayerRange > 0f)
            {
                _p26Erişim++;
                if (mesafe > a.MaxPlayerRange + 0.4f) _p26ErişimHata.Add($"{a.Name}: {mesafe:0.0} > {a.MaxPlayerRange:0}");
            }
            if (yeni && a.Harita && a.SafeAngle > 0f)
            {
                _p26Açıklık++;
                if (merkezden > 0.6f) _p26AçıklıkHata.Add($"{a.Name}: boss merkezden {merkezden:0.0}");   // ortaya sıçrıyor
            }
            if (a.SafeSpotRadius > 0f)
            {
                _p26Nokta++;
                float yol = Düz(ben - e.CurrentSafeSpot).magnitude - a.SafeSpotRadius;
                float bütçe = _t.moveSpeed * Mathf.Max(0f, e.KalanHazırlık - _t.bossReactionWindow) + 0.3f;
                if (yol > bütçe) _p26NoktaHata.Add($"{a.Name}: yol {yol:0.0} > koşu {bütçe:0.0}");
            }
        }

        /// <summary>`adet` yeni saldırı (ya da `sınır` sn) boyunca `mod`'da oyna; her yeni saldırıda `her`.</summary>
        IEnumerator Topla(Enemy boss, Mod mod, int adet, float sınır, System.Action<Enemy> her, System.Func<bool> yeter = null)
        {
            int n = 0;
            _yeniSaldırı = e => { if (e != boss || e.CurrentAttack == null) return; n++; her(e); };
            float t0 = Time.realtimeSinceStartup;
            while (boss != null && n < adet && Time.realtimeSinceStartup - t0 < sınır && (yeter == null || !yeter()))
            {
                if (Saat != null && Saat.Dead) { Kaydet("—", "bot öldü", false, _sahne); _yeniSaldırı = null; yield return Bitir(); yield break; }
                DövüşAdımı(mod, boss);
                _ne = $"{n}/{adet} saldırı";
                yield return null;
            }
            _yeniSaldırı = null;
            Dur();
        }

        IEnumerator BossaGit(params Key[] tuşlar)
        {
            foreach (var k in tuşlar) { Bas(k); yield return Bekle(0.6f); }
            yield return Bekle(0.6f);
            if (Saat != null && Saat.Seconds < 200f) { Bas(Key.F5); yield return Bekle(0.3f); }
        }

        IEnumerator P26Tahsildar()
        {
            _p26 = true;
            Sahne("P1 TAHSİLDAR", "B — boss odası");
            yield return BossaGit(Key.B);
            var boss = Boss();
            if (boss == null || boss.Boss.Name != "TAHSİLDAR") { Kaydet("P1", "TAHSİLDAR", false, $"bulunamadı ({BossAdı(boss)})"); yield break; }

            // Madde 1 — her 4. saldırı harita (uzakta ve dibinde, art arda)
            var sıra = new List<BossAttack>();
            Sahne("P1 TAHSİLDAR", "madde 1: uzakta 12 saldırı");
            yield return Topla(boss, Mod.UzakDur, 12, 120f, e => sıra.Add(e.CurrentAttack));
            int uzaktaki = sıra.Count;
            Sahne("P1 TAHSİLDAR", "madde 1: dibinde 12 saldırı (kaçarak)");
            yield return Topla(boss, Mod.DibindeKaçar, 12, 120f, e => sıra.Add(e.CurrentAttack));
            bool dördüncü = sıra.Count >= 24;
            for (int k = 0; k < sıra.Count; k++) if (sıra[k].Harita != ((k + 1) % 4 == 0)) dördüncü = false;
            string Dizi(List<BossAttack> l, int b, int s) => string.Join(", ", l.GetRange(b, s).ConvertAll(a => a.Harita ? $"[{a.Name}]" : a.Name));
            Kaydet("P1", "Madde 1 — her 4. saldırı harita (24 saldırı, uzakta + dibinde)", dördüncü,
                   $"uzakta: {Dizi(sıra, 0, uzaktaki)} · dibinde: {Dizi(sıra, uzaktaki, sıra.Count - uzaktaki)}");

            // Madde 3 — ISRAR: önce seriyi sıfırla (uzakta bir menzilli saldırı), sonra dibinde, kaçmadan.
            // ⚠️ İlk koşuda bu adım yoktu: madde 1'in "dibinde" serisi ×4.77'den devam etti ve bot
            // sıfırdan bekleyip kırmızı yaktı — oyun doğru saymıştı, ölçen yanlıştı.
            Sahne("P1 TAHSİLDAR", "madde 3: önce uzaklaşıp seriyi sıfırlıyorum");
            bool uzakGeldi = false;
            yield return Topla(boss, Mod.UzakDur, 6, 40f, e => { if (e.CurrentAttack.Tür == SaldırıTürü.Uzak) uzakGeldi = true; }, () => uzakGeldi);
            Sahne("P1 TAHSİLDAR", "madde 3: dibinde duruyorum, kaçmıyorum — ısrar kaç saniyede ×5?");
            _takviyeEşiği = 150f;
            var ısrar = new List<(SaldırıTürü tür, float x, float t)>();
            float t0 = Time.time, enBüyük5 = 0f, önceki = Saat.TotalHitCost;
            string hud5 = "";
            int n = 0;
            _yeniSaldırı = e =>
            {
                if (e != boss || e.CurrentAttack == null) return;
                n++;
                ısrar.Add((e.CurrentAttack.Tür, e.IsrarÇarpanı, Time.time - t0));
                if (e.IsrarÇarpanı >= 4.99f && hud5 == "" && Oda != null) hud5 = Oda.Prompt();
            };
            float g0 = Time.realtimeSinceStartup;
            int beşte = 0;
            while (boss != null && Time.realtimeSinceStartup - g0 < 150f && n < 20 && beşte < 3)
            {
                if (Saat.Dead) { Kaydet("—", "bot öldü (ısrar)", false, _sahne); _yeniSaldırı = null; yield return Bitir(); yield break; }
                DövüşAdımı(Mod.DibeGir, boss);
                float darbe = Saat.TotalHitCost - önceki;
                önceki = Saat.TotalHitCost;
                if (boss.IsrarÇarpanı >= 4.99f) enBüyük5 = Mathf.Max(enBüyük5, darbe);
                beşte = ısrar.FindAll(x => x.x >= 4.99f).Count;
                _ne = $"ısrar ×{boss.IsrarÇarpanı:0.00}";
                yield return null;
            }
            _yeniSaldırı = null;
            _takviyeEşiği = 35f;
            // Beklenen: aynı sıradan seri sayılıyor, harita ne sayar ne sıfırlar.
            int seri = 0; SaldırıTürü bant = SaldırıTürü.Harita;
            bool eğri = ısrar.Count > 0;
            foreach (var (tür, x, _) in ısrar)
            {
                if (tür == SaldırıTürü.Harita) { if (Mathf.Abs(x - 1f) > 0.01f) eğri = false; continue; }
                seri = tür == bant ? seri + 1 : 1; bant = tür;
                if (Mathf.Abs(x - Enemy.IsrarEğrisi(_t, seri)) > 0.01f) eğri = false;
            }
            int ilk5 = ısrar.FindIndex(x => x.x >= 4.99f);
            Kaydet("P1", "Madde 3 — ısrar gerçek dövüşte eğriyi izliyor (seri, harita sayılmıyor)", eğri,
                   string.Join(", ", ısrar.ConvertAll(x => x.tür == SaldırıTürü.Harita ? "[H]" : x.x.ToString("0.00"))));
            Kaydet("P1", "Madde 3 — ısrar ×5'e çıkıyor, HUD gösteriyor", ilk5 >= 0 && hud5.Contains("ISRAR ×5"),
                   ilk5 >= 0 ? $"{ilk5 + 1}. saldırıda, {ısrar[ilk5].t:0} sn'de · HUD '{hud5}'" : "×5'e çıkmadı");
            Kaydet("P1", "Madde 3 — ×5'te tek vuruş ne götürüyor", null, $"en büyük darbe {enBüyük5:0.0} sn (saat {Saat.Seconds:0} sn)");

            Sahne("P1 TAHSİLDAR", "madde 3: uzaklaşıyorum — ısrar sıfırlanıyor mu?");
            var sonra = new List<(SaldırıTürü, float)>();
            yield return Topla(boss, Mod.UzakDur, 3, 40f, e => sonra.Add((e.CurrentAttack.Tür, e.IsrarÇarpanı)));
            int ilkUzak = sonra.FindIndex(x => x.Item1 == SaldırıTürü.Uzak);
            Kaydet("P1", "Madde 3 — uzaklaşınca ısrar sıfır", ilkUzak >= 0 ? Mathf.Abs(sonra[ilkUzak].Item2 - 1f) < 0.01f : (bool?)null,
                   string.Join(", ", sonra.ConvertAll(x => $"{x.Item1} ×{x.Item2:0.00}")));

            // Madde 2 — TAM TAHSİL: dövüş, öldür
            Sahne("P1 TAHSİLDAR", "madde 2: dövüşüyorum — TAM TAHSİL'de kenara koşuyorum");
            _tehdit = 0; _vuruldu = 0;
            yield return BossuÖldür(240f);
            _saldırıİst.TryGetValue("TAM TAHSİL", out var tt);
            Kaydet("P1", "Madde 2 — TAM TAHSİL her seferinde ORTAYA sıçrıyor", _p26Orta > 0 ? _p26OrtaHata.Count == 0 : (bool?)null,
                   $"{_p26Orta} hazırlık" + (_p26OrtaHata.Count > 0 ? " · " + string.Join(", ", _p26OrtaHata) : ""));
            Kaydet("P1", "Madde 2 — TAM TAHSİL'den kaçış (bot)", null, $"bota dönük {tt.x}, vurulma {tt.y}");
            yield return KapıdanÇık(20f);
        }

        IEnumerator P26Kefil()
        {
            Sahne("P2 KEFİL", "N, B");
            yield return BossaGit(Key.N, Key.B);
            var boss = Boss();
            if (boss == null || boss.Boss.Name != "KEFİL") { Kaydet("P2", "KEFİL", false, $"bulunamadı ({BossAdı(boss)})"); yield break; }
            Sahne("P2 KEFİL", "madde 4: hep kaçıyorum — simit sırası gelince ne oluyor?");
            var uzakta = new List<string>();
            yield return Topla(boss, Mod.ÇokUzak, 6, 35f, e => uzakta.Add(e.CurrentAttack.Name));
            Kaydet("P2", "Madde 4 — hep kaçan bota gelenler", null, uzakta.Count > 0 ? string.Join(", ", uzakta) : "35 sn'de hiç saldırı yok");
            Sahne("P2 KEFİL", "madde 4: dibinde kaçarak 12 saldırı (simite koşuyorum)");
            yield return Topla(boss, Mod.DibindeKaçar, 12, 120f, e => { });
            // ⚠️ Güç almamış bot KEFİL'i 5 dk'da öldüremedi (ilk koşu); öldürmek ölçümün parçası
            // değil — N ile sonraki bölgeye.
            Kaydet("P2", "Madde 4 — simit yalnızca ERİŞİLEBİLİRKEN başlıyor", _p26Erişim > 0 ? _p26ErişimHata.Count == 0 : (bool?)null,
                   $"{_p26Erişim} harita saldırısı" + (_p26ErişimHata.Count > 0 ? " · " + string.Join(", ", _p26ErişimHata) : ""));
            _p26Erişim = 0; _p26ErişimHata.Clear();
        }

        IEnumerator P26Simsar()
        {
            Sahne("P3 SİMSAR", "N, B");
            yield return BossaGit(Key.N, Key.B);
            var boss = Boss();
            if (boss == null || boss.Boss.Name != "SİMSAR") { Kaydet("P3", "SİMSAR", false, $"bulunamadı ({BossAdı(boss)})"); yield break; }
            Sahne("P3 SİMSAR", "madde 5: dibinde kaçarak 16 saldırı — BÜYÜK İHALE'de arkasına geçiyorum");
            var simsar = new List<BossAttack>();
            yield return Topla(boss, Mod.DibindeKaçar, 16, 180f, e => simsar.Add(e.CurrentAttack));
            int haritaS = simsar.FindAll(a => a.Harita).Count;
            // ⚠️ İlk playtest'te 16 saldırıda 1 harita geldi ("duvardaysa yapma" şartı hep tutuyordu).
            Kaydet("P3", "Madde 5 — SİMSAR'ın harita sırası GELİYOR (16 saldırıda ≥ 3)", simsar.Count >= 16 ? haritaS >= 3 : (bool?)null,
                   $"{simsar.Count} saldırıda {haritaS} harita · " + string.Join(", ", simsar.ConvertAll(a => a.Harita ? $"[{a.Name}]" : a.Name)));
            Kaydet("P3", "Madde 5 — açıklıklı harita saldırısında boss ORTADA", _p26Açıklık > 0 ? _p26AçıklıkHata.Count == 0 : (bool?)null,
                   $"{_p26Açıklık} saldırı" + (_p26AçıklıkHata.Count > 0 ? " · " + string.Join(", ", _p26AçıklıkHata) : ""));
            _saldırıİst.TryGetValue("BÜYÜK İHALE", out var bi);
            _saldırıİst.TryGetValue("BÜYÜK İHALE+", out var bi2);
            _saldırıİst.TryGetValue("BÜYÜK MÜZAYEDE", out var bm);
            Kaydet("P3", "Madde 5 — açıklıktan kaçış (bot)", null,
                   $"İHALE {bi.y}/{bi.x} · İHALE+ {bi2.y}/{bi2.x} · MÜZAYEDE {bm.y}/{bm.x} (vurulma/bota dönük)");
        }

        IEnumerator P26Murabahacı()
        {
            Sahne("P4 MURABAHACI", "N, B, F7 TAM GÜÇ");
            yield return BossaGit(Key.N, Key.B, Key.F7);
            var boss = Boss();
            if (boss == null || boss.Boss.PhaseCount < 3) { Kaydet("P4", "MURABAHACI", false, $"bulunamadı ({BossAdı(boss)})"); yield break; }
            Sahne("P4 MURABAHACI", "madde 6: faz 3'e kadar dövüşüyorum — yeşil noktalara koşuyorum");
            float t0 = Time.realtimeSinceStartup;
            while (boss != null && !(boss.Phase >= 3 && !boss.Dormant) && Time.realtimeSinceStartup - t0 < 300f)
            {
                if (Saat != null && Saat.Dead) { Kaydet("—", "bot öldü", false, _sahne); yield return Bitir(); yield break; }
                DövüşAdımı(Mod.Dövüş, Boss());
                yield return null;
            }
            bool faz3 = boss != null && boss.Phase >= 3 && !boss.Dormant;
            Kaydet("P4", "Faz 3'e ulaşıldı", faz3, faz3 ? "rakipler bitti, boss uyandı" : "faz 3'e gelinemedi");
            if (faz3)
            {
                Sahne("P4 MURABAHACI", "madde 6: faz 3 dibinde — 7 yakın saldırının hepsi geliyor mu?");
                var beklenen = Sıra(boss, SaldırıTürü.Yakın);
                var görülen = new HashSet<string>();
                yield return Topla(boss, Mod.DibindeKaçar, 24, 180f,
                                   e => { if (e.CurrentAttack.Tür == SaldırıTürü.Yakın) görülen.Add(e.CurrentAttack.Name); },
                                   () => görülen.Count >= beklenen.Count);
                var eksik = beklenen.FindAll(a => !görülen.Contains(a));
                Kaydet("P4", $"Madde 6 — faz 3 dibinde {beklenen.Count} yakın saldırının hepsi geliyor", eksik.Count == 0,
                       $"{görülen.Count}/{beklenen.Count}" + (eksik.Count > 0 ? " · gelmeyen: " + string.Join(", ", eksik) : ""));
            }
            Sahne("P4 MURABAHACI", "öldürüyorum");
            yield return BossuÖldür(300f, kazanınca: true);
            Kaydet("P4", "Madde 6 — yeşil nokta her inişte YETİŞİLEBİLİR", _p26Nokta > 0 ? _p26NoktaHata.Count == 0 : (bool?)null,
                   $"{_p26Nokta} iniş" + (_p26NoktaHata.Count > 0 ? " · " + string.Join(", ", _p26NoktaHata) : ""));
            Kaydet("P4", "Final", null, (Saat != null && Saat.Won ? "KAZANILDI" : "kazanılamadı") + $" · süre takviyesi {_takviye}");
        }

        /// <summary>
        /// Madde 7 — "hangisi haksız": her saldırıda bota dönük hazırlık ve isabet. Bot kusursuz
        /// değil (ortalaması da yazılıyor); haksız ADAY, ortalamanın çok üstünde vurandır.
        /// </summary>
        void SaldırıİstatistikRaporu()
        {
            if (_saldırıİst.Count == 0) return;
            int tt = 0, tv = 0;
            var l = new List<(string ad, int t, int v)>();
            foreach (var kv in _saldırıİst) { tt += kv.Value.x; tv += kv.Value.y; if (kv.Value.x >= 3) l.Add((kv.Key, kv.Value.x, kv.Value.y)); }
            l.Sort((a, b) => (b.v * a.t).CompareTo(a.v * b.t));
            var sb = new StringBuilder();
            for (int i = 0; i < Mathf.Min(8, l.Count); i++) sb.Append($"{l[i].ad} {l[i].v}/{l[i].t} · ");
            Kaydet("İ", "Saldırı başına vurulma (en çok vuranlar, ≥3 kez bota dönük)", null,
                   $"ortalama {tv}/{tt} (%{100f * tv / Mathf.Max(1, tt):0}) · {sb}");
        }

        IEnumerator Bitir()
        {
            if (_bitti) yield break;
            _bitti = true;
            Dur();
            Sahne("9/9 BİTTİ", "rapor yazılıyor");
            SS("son");
            yield return Bekle(0.5f);

            SaldırıİstatistikRaporu();
            KapsamaRaporu();
            int hata = SessionState.GetInt(HataAnahtar, 0);
            try { RaporuYaz(hata); }
            catch (System.Exception ex) { Debug.LogError($"[OTOPİLOT] Rapor yazılamadı: {ex.Message}"); hata++; }
            finally
            {
                // ⚠️ `SaveFile.PathOverride` BİLEREK sıfırlanmıyor: `isPlaying = false` bir kare
                // sonra işliyor ve oyun o karede bir kayıt yazarsa GERÇEK dosyaya giderdi
                // (inceleme). Play'den çıkışta statik zaten yeniden yükleniyor.
                SessionState.EraseBool(Aktif);
                KayıtDosyalarınıSil();
                // ⚠️ Her durumda çık — batch'te `-quit` yok, takılırsa süreç sonsuza dek açık kalır.
                if (Application.isBatchMode) EditorApplication.Exit(hata == 0 ? 0 : 1);
                else EditorApplication.isPlaying = false;
            }
        }

        /// <summary>
        /// Enes: *"bosslarda yazılmış tüm saldırılar kullanılıyor mu?"* Bu koşuda boss/faz
        /// başına görülen ve görülmeyen saldırılar. ℹ️ — bot her mesafede durmuyor.
        /// ⚠️ Yalnızca K1'den sonraki dövüşler (Play'den çıkış sözlüğü sıfırlıyor).
        /// </summary>
        void KapsamaRaporu()
        {
            int toplamGörülen = 0, toplam = 0;
            foreach (var b in BossKind.All)
                for (int f = 1; f <= b.PhaseCount; f++)
                {
                    if (!_görülen.TryGetValue($"{b.Index}|{f}", out var küme)) continue;
                    var eksik = new List<string>();
                    int fazda = 0;
                    foreach (var a in b.Attacks(f)) { fazda++; if (!küme.Contains(a.Name)) eksik.Add(a.Name); }
                    toplam += fazda; toplamGörülen += fazda - eksik.Count;
                    Kaydet("K", $"Kapsama: {b.Name} faz {f}", null,
                           $"{fazda - eksik.Count}/{fazda} görüldü" + (eksik.Count > 0 ? $" · görülmeyen: {string.Join(", ", eksik)}" : ""));
                }
            if (toplam > 0)
                Kaydet("K", "Kapsama: dövüşülen fazların toplamı", null, $"{toplamGörülen}/{toplam} saldırı görüldü");
        }

        public static void KayıtDosyalarınıSil()
        {
            foreach (var yol in new[] { KayıtYolu, KayıtYolu + ".tmp" })   // `Load` önce .tmp'yi okur
                try { if (File.Exists(yol)) File.Delete(yol); } catch { /* önemsiz */ }
        }

        void RaporuYaz(int hata)
        {
            Directory.CreateDirectory(Klasör);
            var sb = new StringBuilder();
            sb.AppendLine($"# Otopilot raporu — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
            sb.AppendLine(hata == 0 ? "**SONUÇ: ✅ geçti**" : $"**SONUÇ: ❌ {hata} kontrol başarısız**");
            sb.AppendLine();
            sb.AppendLine("| # | Ne | | Ölçüm |");
            sb.AppendLine("|---|---|---|---|");
            sb.Append(SessionState.GetString(RaporAnahtar, ""));
            File.WriteAllText(Path.Combine(Klasör, "rapor.md"), sb.ToString());
            Debug.Log($"[OTOPİLOT] RAPOR: {Path.GetFullPath(Path.Combine(Klasör, "rapor.md"))}\n{sb}");

        }

        // ── dövüş beyni ───────────────────────────────────────────────────────

        enum Mod { Dövüş, UzakDur, DibeGir, Ölümüne, ÇokUzak, DibindeKaçar }

        readonly Dictionary<Enemy, float> _gördüğüAn = new Dictionary<Enemy, float>();
        readonly Dictionary<Enemy, Enemy.State> _sonDurum = new Dictionary<Enemy, Enemy.State>();
        readonly List<(Enemy e, float hasar, bool açıklık, string ad)> _bekleyen = new List<(Enemy, float, bool, string)>();
        int _tehdit, _vuruldu, _takviye;
        /// <summary>Saat bunun altına inince F5. Israr ölçümünde yüksek: ×5'lik vuruş 35 sn'den büyük.</summary>
        float _takviyeEşiği = 35f;
        /// <summary>
        /// Bot şu an KAÇIYOR mu. Vurulma tablosu yalnızca bunu sayar: "uzakta dur" ve "dibinde
        /// kaçmadan dur" bölümlerinde bot bilerek vuruluyor — ilk playtest'te TAM TAHSİL'i
        /// 6'da 4 gösteren buydu.
        /// </summary>
        bool _kaçıyor;
        /// <summary>Saldırı adı → (bota dönük hazırlık, isabet). Haksız aday tablosu bundan.</summary>
        readonly Dictionary<string, Vector2Int> _saldırıİst = new Dictionary<string, Vector2Int>();
        float _sonTakviye = -99f, _sonVuruş = -99f, _bossİlkHavuz;
        bool _donukSS;

        Enemy _simsar, _final;
        bool _işaretGörüldü, _işaretTuruncu, _faz2Kredi, _uykuGörüldü, _uykuSoluk;
        float _işaretTon, _faz2Önce, _öncekiOran, _uykuBaşı = -1f;
        int _açıklık, _açıklıkVuruldu;
        string _uykuRenk = "";
        readonly List<string> _finalSaldırılar = new List<string>();

        /// <summary>`SaldırıTopla`: her saldırı başlarken botun boss'a uzaklığı (listeyle aynı sıra).</summary>
        readonly List<float> _başMesafe = new List<float>();
        readonly List<string> _menzilDışı = new List<string>();

        /// <summary>"Boss|faz" → bu koşuda görülen saldırı adları. Kapsama raporu bundan.</summary>
        readonly Dictionary<string, HashSet<string>> _görülen = new Dictionary<string, HashSet<string>>();
        float _enYüksekIsrar = 1f;
        readonly HashSet<Enemy> _bossGörüldü = new HashSet<Enemy>();

        static Enemy Boss()
        {
            foreach (var e in Enemy.All)
                if (e != null && e.IsBoss && !e.SummonedRival) return e;
            return null;
        }

        Enemy EnYakın()
        {
            Enemy en = null; float d = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dormant) continue;
                float x = Düz(e.transform.position - Ben).magnitude;
                if (x < d) { d = x; en = e; }
            }
            return en;
        }

        /// <summary>Her karede: gözler (uyarılar), süre takviyesi, kaçış, hedef.</summary>
        void DövüşAdımı(Mod mod, Enemy hedef = null)
        {
            var pc = Oyuncu;
            if (pc == null || Saat == null || _t == null) return;
            Vector3 ben = Ben;

            Gözler(ben);
            if (_rütbeÖlç) RütbeÖlç();
            Gözlem();

            // Süre takviyesi (Enes: "gerektiğinde süre takviyesi al koşuyu bitirmek için")
            if (mod != Mod.Ölümüne && Saat.Seconds < _takviyeEşiği && Time.time - _sonTakviye > 2f)
            {
                Bas(Key.F5); _sonTakviye = Time.time; _takviye++;
            }

            bool kaçan = mod == Mod.Dövüş || mod == Mod.ÇokUzak || mod == Mod.DibindeKaçar;
            _kaçıyor = kaçan;
            if (kaçan && Kaç(ben)) return;
            if (kaçan && İşarettenKaç(ben)) return;

            if (hedef == null || hedef.Dormant) hedef = EnYakın();
            if (hedef == null) { Dur(); return; }
            Vector3 to = Düz(hedef.transform.position - ben);
            float d = to.magnitude;
            NişanAl(hedef.transform.position);

            switch (mod)
            {
                case Mod.Dövüş:
                    float menzil = pc.Weapon.Range(_t) * 0.8f + hedef.Radius;
                    if (d > menzil) { if (AlanaGirer(ben, to)) Dur(); else Yürü(to); }
                    else
                    {
                        Dur();
                        if (!pc.Attacking && Time.time - _sonVuruş > 0.15f) { _sol = true; _sonVuruş = Time.time; }
                        if (pc.SpecialCooldownLeft <= 0f && d < menzil * 0.8f) _sağ = true;
                    }
                    break;
                case Mod.UzakDur:
                    if (d < 7f) Yürü(Uzaklaş(ben, to)); else Dur();
                    break;
                case Mod.ÇokUzak:
                    Yürü(Uzaklaş(ben, to));                       // hep kaç: erişilmez kal
                    break;
                case Mod.DibindeKaçar:
                    if (d > 1.2f + hedef.Radius * 0.5f && !AlanaGirer(ben, to)) Yürü(to); else Dur();
                    break;
                case Mod.DibeGir:
                case Mod.Ölümüne:
                    if (d > 1.2f + hedef.Radius * 0.5f) Yürü(to); else Dur();
                    break;
            }
        }

        Vector3 Uzaklaş(Vector3 ben, Vector3 to)
        {
            Vector3 kaç = -to;
            if (Düz(ben + kaç.normalized * 2f).magnitude > _t.arenaRadius * 0.85f)
                kaç = Vector3.Cross(Vector3.up, to);         // kenardaysan yanlamasına
            return kaç;
        }

        /// <summary>Hazırlığa girenleri not al; vuruş anında isabet oldu mu say.</summary>
        void Gözler(Vector3 ben)
        {
            if (Saat == null || _t == null) return;          // yeniden kurulumun ortası

            // Ölen düşmanların kayıtları (Unity null'ı) birikmesin.
            _bekleyen.RemoveAll(b => b.e == null);
            foreach (var ölü in new List<Enemy>(_sonDurum.Keys))
                if (ölü == null) { _sonDurum.Remove(ölü); _gördüğüAn.Remove(ölü); _sonHavuz.Remove(ölü); }

            foreach (var e in Enemy.All)
            {
                if (e == null) continue;
                _sonDurum.TryGetValue(e, out var önce);
                var şimdi = e.CurrentState;
                if (şimdi == Enemy.State.Windup && !_gördüğüAn.ContainsKey(e)) _gördüğüAn[e] = Time.time;
                if (şimdi != Enemy.State.Windup) _gördüğüAn.Remove(e);

                // Vuruş indi: bize mi?
                if (önce == Enemy.State.Windup && şimdi == Enemy.State.Strike && BekleyenVar(e, out int k))
                {
                    var b = _bekleyen[k];
                    _bekleyen.RemoveAt(k);
                    bool vurdu = Saat.TotalHitCost > b.hasar + 0.01f;
                    // Tablo için daha sıkı: darbe TAM bu vuruşun karesinde mi indi. Hazırlık
                    // boyunca BAŞKA bir yerden (rakipler, SİMSAR'ın işaretleri) yenen darbe
                    // bu saldırıya yazılıyordu (playtest 3: kalabalıkta KOMİSYON+ 14/14).
                    bool buVuruş = Saat.TotalHitCost > _kareDarbesi + 0.01f;
                    if (vurdu) _vuruldu++;
                    if (b.açıklık) { _açıklık++; if (vurdu) _açıklıkVuruldu++; }
                    if (b.ad != null)
                    {
                        _saldırıİst.TryGetValue(b.ad, out var ist);
                        _saldırıİst[b.ad] = new Vector2Int(ist.x + 1, ist.y + (buVuruş ? 1 : 0));
                    }
                }
                // Tehdit sayımı: hazırlık başında bize dönük mü
                if (önce != Enemy.State.Windup && şimdi == Enemy.State.Windup && e.ŞimdiVursaİsabet(ben))
                {
                    _tehdit++;
                    _bekleyen.Add((e, Saat.TotalHitCost, e.CurrentAttack != null && e.CurrentAttack.SafeAngle > 0f,
                                   _kaçıyor && e.IsBoss && e.CurrentAttack != null ? e.CurrentAttack.Name : null));
                }
                // Boss saldırı başlangıçları (yalnızca kovalamadan gelen = yeni saldırı)
                if (önce == Enemy.State.Chase && şimdi == Enemy.State.Windup && e.IsBoss)
                {
                    _yeniSaldırı?.Invoke(e);
                    if (e.Boss != null && e.CurrentAttack != null)
                    {
                        string anahtar = $"{e.Boss.Index}|{e.Phase}";
                        if (!_görülen.TryGetValue(anahtar, out var küme)) _görülen[anahtar] = küme = new HashSet<string>();
                        küme.Add(e.CurrentAttack.Name);
                    }
                    _enYüksekIsrar = Mathf.Max(_enYüksekIsrar, e.IsrarÇarpanı);
                }
                if (e == _final && önce != Enemy.State.Windup && şimdi == Enemy.State.Windup && e.CurrentAttack != null)
                    _finalSaldırılar.Add(e.CurrentAttack.Name);
                if (_p26 && e.IsBoss && önce != Enemy.State.Windup && şimdi == Enemy.State.Windup && e.CurrentAttack != null)
                    P26Gözle(e, ben, yeni: önce == Enemy.State.Chase);

                // Donmuş uyarı yayı — bir kez
                if (_donukSS && şimdi == Enemy.State.Windup && e.IsBoss && e.KalanHazırlık < _t.bossReactionWindow * 0.6f
                    && e.KalanHazırlık > 0.05f)
                {
                    _donukSS = false;
                    SS("donmus_uyari_yayi");
                }
                _sonDurum[e] = şimdi;
            }
            _kareDarbesi = Saat.TotalHitCost;
        }

        /// <summary>Bir önceki karenin sonunda toplam darbe — vuruşun KENDİ karesini ayırmak için.</summary>
        float _kareDarbesi;

        bool BekleyenVar(Enemy e, out int k)
        {
            for (k = 0; k < _bekleyen.Count; k++) if (_bekleyen[k].e == e) return true;
            k = -1; return false;
        }

        System.Action<Enemy> _yeniSaldırı;

        /// <summary>SİMSAR / final gözlemleri — renk, faz, uyku.</summary>
        void Gözlem()
        {
            if (_simsar != null)
            {
                if (!_işaretGörüldü)
                    foreach (var g in FindObjectsByType<GroundMark>(FindObjectsSortMode.None))
                    {
                        if (g == null || !g.OyuncuyaVurur) continue;
                        foreach (var a in FindObjectsByType<Arc>(FindObjectsSortMode.None))
                        {
                            if (a == null || Düz(a.transform.position - g.transform.position).magnitude > 0.2f) continue;
                            Color.RGBToHSV(a.Color, out float h, out _, out _);
                            _işaretTon = h * 360f;
                            _işaretTuruncu = _işaretTon < 40f || _işaretTon > 340f;
                            _işaretGörüldü = true;
                            SS("simsar_isareti");
                            break;
                        }
                        if (_işaretGörüldü) break;
                    }
                if (_simsar != null && _simsar.Phase == 1)
                    _öncekiOran = _simsar.TimeLeft / Mathf.Max(0.01f, _bossİlkHavuz);
                else if (_simsar != null && _simsar.Phase == 2 && _faz2Önce < 0f)
                {
                    _faz2Önce = _öncekiOran;
                    _faz2Kredi = _simsar.OnCredit && _simsar.TimeLeft > _bossİlkHavuz * 0.8f;
                    SS("simsar_faz2");
                }
            }

            if (_final != null && _final.Dormant && _uykuBaşı < 0f) _uykuBaşı = Time.realtimeSinceStartup;
            // ⚠️ Parlama sönsün diye 0.6 sn sonra: faz 3'e geçiş HEP bir vuruşun içinde,
            // o vuruşun 0.2 sn'lik beyaz parlaması ilk karede hâlâ yanıyor. İlk koşuda
            // ilk karede ölçüp "beyaz" dedim — ölçüm hatası.
            if (_final != null && _final.Dormant && !_uykuGörüldü && Time.realtimeSinceStartup - _uykuBaşı > 0.6f)
            {
                _uykuGörüldü = true;
                var r = _final.GetComponentInChildren<Renderer>();
                var c = r != null ? r.material.color : Color.white;
                var tür = _final.KindColor;
                float parlaklık(Color x) => 0.2126f * x.r + 0.7152f * x.g + 0.0722f * x.b;
                _uykuSoluk = parlaklık(c) < parlaklık(tür) * 0.8f && c != Color.white;
                _uykuRenk = $"renderer {c} · tür rengi {tür} · parlaklık {parlaklık(c):0.00} / {parlaklık(tür):0.00}";
                SS("final_uyuyor");
            }
        }

        /// <summary>
        /// İnsan gibi kaç: uyarıyı GÖRDÜKTEN 0.25 sn sonra. Önce koşarak güvenli yer ara,
        /// olmazsa dash (dokunulmazlık), o da olmazsa uzaklaş.
        /// </summary>
        bool Kaç(Vector3 ben)
        {
            var tehdit = new List<Enemy>();
            float kalan = 9f;
            foreach (var kv in _gördüğüAn)
            {
                var e = kv.Key;
                if (e == null || Time.time - kv.Value < İnsanTepkisi) continue;
                if (!Tehlikede(e, ben)) continue;
                tehdit.Add(e);
                kalan = Mathf.Min(kalan, e.KalanHazırlık);
            }
            if (tehdit.Count == 0) return false;

            Vector3 merkez = Vector3.zero;
            foreach (var e in tehdit) merkez += e.transform.position;
            merkez /= tehdit.Count;
            Vector3 uzak = Düz(ben - merkez);
            if (uzak.sqrMagnitude < 0.01f) uzak = Vector3.forward;
            uzak.Normalize();

            float koşu = _t.moveSpeed * Mathf.Max(0.05f, kalan - 0.03f);
            if (GüvenliYön(ben, tehdit, uzak, koşu, out var yön)) { Yürü(yön); return true; }
            if (GüvenliYön(ben, tehdit, uzak, _t.dashDistance + koşu * 0.5f, out yön))
            {
                Yürü(yön); Bas(Key.Space); return true;
            }
            Yürü(uzak);
            if (kalan <= _t.iFrameDuration * 0.9f) Bas(Key.Space);   // dokunulmazlıkla karşıla
            return true;
        }

        /// <summary>
        /// 16 yönün her birinde ışın boyunca İLK güvenli nokta; en yakını (eşitse "uzaklaş"a yakın olan).
        /// ⚠️ Eskiden yalnızca `mesafe` uzaklığındaki TEK noktaya bakıyordu: kenara koşulacak dev
        /// halkada (TAM TAHSİL) o nokta arenanın dışına düşüyor, bot yanlamasına kaçıp vuruluyordu
        /// (Faz 26 playtest, 2026-10-11: 5'te 4). İnsan en yakın güvenli yere koşar.
        /// </summary>
        bool GüvenliYön(Vector3 ben, List<Enemy> tehdit, Vector3 uzak, float mesafe, out Vector3 yön)
        {
            float en = float.MaxValue;
            yön = uzak;
            for (int i = 0; i < 16; i++)
            {
                float açı = ((i + 1) / 2) * 22.5f * (i % 2 == 0 ? 1f : -1f);
                var d = Quaternion.Euler(0f, açı, 0f) * uzak;
                for (float x = 0.5f; x <= mesafe && x < en; x += 0.5f)
                {
                    var aday = ben + d * x;
                    if (Düz(aday).magnitude > _t.arenaRadius * 0.95f) break;
                    bool güvenli = true;
                    foreach (var e in tehdit) if (Tehlikede(e, aday)) { güvenli = false; break; }
                    if (güvenli) { en = x; yön = d; break; }
                }
            }
            return en < float.MaxValue;
        }

        /// <summary>
        /// Bu nokta ya da 0.4 birim çevresi vurulur mu. ⚠️ Pay şart: bot güvenli yere VARINCA kaçmayı
        /// bırakıyordu, alanın tam kenarında duruyordu.
        /// </summary>
        static bool Tehlikede(Enemy e, Vector3 p)
        {
            if (e.ŞimdiVursaİsabet(p)) return true;
            // ⚠️ Pay yalnızca HARİTA saldırısında: her saldırıda pay bırakan bot boss'a hiç
            // yanaşamadı, F7'yle bile MURABAHACI'yı faz 3'e taşıyamadı (playtest 3).
            if (e.CurrentAttack == null || !e.CurrentAttack.Harita) return false;
            foreach (var o in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                if (e.ŞimdiVursaİsabet(p + o * 0.4f)) return true;
            return false;
        }

        /// <summary>
        /// Bir sonraki adım hazırlanan bir HARİTA saldırısının alanına giriyor mu. ⚠️ Bot dövüşürken
        /// boss'a yürüyordu: ortaya sıçrayan TAM TAHSİL'in halkasında kenarda girip çıkıyor, vuruş
        /// anında içeride kalıyordu (Faz 26 playtest). İnsan kenarda bekler. Kısa hazırlıklı normal
        /// saldırılarda beklemek botu pasifleştiriyordu — orada eski davranış.
        /// </summary>
        static bool AlanaGirer(Vector3 ben, Vector3 yön)
        {
            var sonraki = ben + yön.normalized * 1f;
            foreach (var e in Enemy.All)
                if (e != null && e.CurrentState == Enemy.State.Windup && e.CurrentAttack != null && e.CurrentAttack.Harita
                    && Tehlikede(e, sonraki) && !e.ŞimdiVursaİsabet(ben))
                    return true;
            return false;
        }

        bool İşarettenKaç(Vector3 ben)
        {
            foreach (var g in FindObjectsByType<GroundMark>(FindObjectsSortMode.None))
            {
                if (g == null || !g.OyuncuyaVurur || g.Fired) continue;
                Vector3 to = Düz(ben - g.transform.position);
                if (to.magnitude > g.Yarıçap + 0.6f) continue;
                Yürü(to.sqrMagnitude > 0.01f ? to : Vector3.right);
                return true;
            }
            return false;
        }

        // ── yardımcı koroutinler ───────────────────────────────────────────────

        /// <summary>Yönü bir kare tut + boşluk, sonra bırak; dash'in kendi mesafesi ölçülsün.</summary>
        IEnumerator TekKareDash(Vector3 yön)
        {
            Yürü(yön); Bas(Key.Space);
            yield return null;                            // LateUpdate cihaza yazar
            yield return null;                            // oyun okur, dash başlar
            Dur();
            yield return Bekle(0.3f);
        }

        IEnumerator Bekle(float sn)
        {
            float bitiş = Time.realtimeSinceStartup + sn;
            while (Time.realtimeSinceStartup < bitiş) { Gözler(Ben); yield return null; }
        }

        IEnumerator Dövüş(float sn)
        {
            float bitiş = Time.realtimeSinceStartup + sn;
            while (Time.realtimeSinceStartup < bitiş) { DövüşAdımı(Mod.Dövüş); yield return null; }
            Dur();
        }

        IEnumerator Git(Vector3 hedef, float yakın, float sınır)
        {
            float t0 = Time.realtimeSinceStartup;
            while (Düz(hedef - Ben).magnitude > yakın && Time.realtimeSinceStartup - t0 < sınır)
            {
                Yürü(hedef - Ben);
                yield return null;
            }
            Dur();
        }

        /// <summary>Odadaki herkesi öldür, açılan kapıdan geç.</summary>
        IEnumerator OdayıGeç(float sınır)
        {
            var rm = Oda;
            if (rm == null) yield break;
            float t0 = Time.realtimeSinceStartup;
            while (Oda != null && !Oda.Cleared && Time.realtimeSinceStartup - t0 < sınır)
            {
                DövüşAdımı(Mod.Dövüş);
                yield return null;
            }
            if (Oda != null && !Oda.Cleared)
                Kaydet("—", $"oda {Oda.Room} temizlenemedi", false, $"{sınır:0} sn yetmedi");
            yield return KapıdanÇık(20f);
        }

        IEnumerator KapıdanÇık(float sınır)
        {
            if (Oda == null) yield break;
            int oda = Oda.Room, bölge = Oda.RegionIndex;
            float t0 = Time.realtimeSinceStartup;
            while (Oda != null && Oda.Room == oda && Oda.RegionIndex == bölge
                   && Time.realtimeSinceStartup - t0 < sınır)
            {
                if (Saat != null && Saat.Seconds < 20f && Time.time - _sonTakviye > 2f)
                { Bas(Key.F5); _sonTakviye = Time.time; _takviye++; }
                var kapı = FindFirstObjectByType<ExitGate>();
                Vector3 hedef = kapı != null ? kapı.transform.position : Bootstrap.Exit(_t);
                Yürü(hedef - Ben);
                yield return null;
            }
            Dur();
            yield return Bekle(0.3f);
        }

        IEnumerator SaldırıTopla(Enemy boss, Mod mod, int adet, List<string> liste, float sınır)
        {
            _bossİlkHavuz = _bossİlkHavuz > 0f ? _bossİlkHavuz : boss.TimeLeft;
            _başMesafe.Clear();
            _yeniSaldırı = e =>
            {
                if (e != boss || e.CurrentAttack == null) return;
                liste.Add(e.CurrentAttack.Name);
                // BAĞIMSIZ ÖLÇÜ: saldırı başladığında oyuncu gerçekten erişimde mi. Botun
                // listeleri üretimle aynı alanı (Tür) kullanıyor; bu kullanmıyor.
                // ⚠️ ORTAYA SIÇRAYAN (TAM TAHSİL) hariç: erişimi arena, halkası kenarı boş bırakıyor.
                float mesafe = Düz(Ben - e.transform.position).magnitude;
                _başMesafe.Add(mesafe);
                if (e.CurrentAttack.ArenaOranı <= 0f && mesafe > e.CurrentStrikeRadius + e.CurrentAttack.Lunge + 0.3f)
                    _menzilDışı.Add($"{e.CurrentAttack.Name} ({mesafe:0.0} > {e.CurrentStrikeRadius + e.CurrentAttack.Lunge:0.0})");
            };
            float t0 = Time.realtimeSinceStartup;
            while (boss != null && liste.Count < adet && Time.realtimeSinceStartup - t0 < sınır)
            {
                DövüşAdımı(mod, boss);
                _ne = $"görülen: {string.Join(", ", liste)}";
                yield return null;
            }
            _yeniSaldırı = null;
            Dur();
        }

        IEnumerator BossuÖldür(float sınır, bool kazanınca = false)
        {
            var boss = Boss();
            if (boss != null && !_bossGörüldü.Contains(boss)) { _bossGörüldü.Add(boss); _bossİlkHavuz = boss.TimeLeft; }
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < sınır)
            {
                if (kazanınca ? (Saat != null && Saat.Won) : Boss() == null) break;
                if (Saat != null && Saat.Dead) { Kaydet("—", "bot öldü (eşya kalmadı)", false, _sahne); yield return Bitir(); yield break; }
                DövüşAdımı(Mod.Dövüş, Boss());
                yield return null;
            }
            Dur();
        }

        static List<string> Sıra(Enemy boss, SaldırıTürü tür)
        {
            var l = new List<string>();
            foreach (var a in boss.Boss.Attacks(boss.Phase))
                if (a.Tür == tür) l.Add(a.Name);
            return l;
        }

        static int Farklı(List<string> görülen, List<string> beklenen)
        {
            var k = new HashSet<string>();
            foreach (var a in görülen) if (beklenen.Contains(a)) k.Add(a);
            return k.Count;
        }

        static float Ortalama(List<float> l) { float t = 0f; foreach (var x in l) t += x; return t / l.Count; }
        static string Göster(float x) => x < 0f ? "—" : x.ToString("0.00");
    }
}
#endif
