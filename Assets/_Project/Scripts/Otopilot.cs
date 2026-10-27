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

            // SESSİZ (Enes, 2026-10-12: "bot testi yaparken arkada oyunun sesi geliyor").
            // Yalnızca bu Play oturumu — editörün kalıcı "Mute Audio" ayarına dokunulmuyor.
            _eskiSes = AudioListener.volume;
            AudioListener.volume = 0f;
        }

        float _eskiSes = 1f;

        void OnDestroy()
        {
            if (_kb != null) InputSystem.RemoveDevice(_kb);
            if (_fare != null) InputSystem.RemoveDevice(_fare);
            if (_eskiAyar != null) InputSystem.settings = _eskiAyar;
            AudioListener.volume = _eskiSes;
            Time.captureDeltaTime = 0f;                          // Play'den çıkınca elle oyun gerçek hızda
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
            if (!_bitti && Time.realtimeSinceStartup - _başlangıç > _bekçiDk * 60f)
            {
                Kaydet("⏱", "Bekçi", false, $"{_bekçiDk:0} dk doldu — bot takıldı");
                StartCoroutine(Bitir());
            }
        }

        void Bas(Key k) => _birKare.Add(k);

        /// <summary>
        /// Faz 30: tuşlar ENVANTER YUVASI. Test senaryosu silahı açıp (kendi yuvası yoksa 3. yuvaya)
        /// koyar ve o yuvanın tuşuna basar — oyuncunun yolu: aç → envantere al → tuşla kuşan.
        /// </summary>
        void Kuşan(int silah)
        {
            Kadro.Aç(silah, "otopilot");
            int yuva = Kadro.Yuvası(silah);
            if (yuva < 0) { yuva = Kadro.Yuva - 1; Kadro.YuvayaKoy(yuva, silah); }
            Bas(yuva == 0 ? Key.Digit1 : yuva == 1 ? Key.Digit2 : Key.Digit3);
        }

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

        // ── HIZLANDIRILMIŞ ZAMAN (Enes 2026-10-18: "her test çok uzun sürüyor, kısaltacak bir çözüm") ──
        // Bot görünmez pencerede saniyede binlerce kare koşuyor ama her kareye GERÇEK geçen süre kadar
        // oyun zamanı düşüyordu: 1 oyun saniyesi = 1 gerçek saniye. İzlenmeyen koşuda her kare sabit
        // `KareSüresi` oyun saniyesi (`Time.captureDeltaTime`): oyun bilgisayarın yetiştiği hızda akar, her
        // kare aynı uzunlukta olduğu için sonuç da tekrarlanabilir. ⚠️ Botun bütün beklemeleri ve süre
        // sınırları bu yüzden OYUN saatiyle (`Şimdi`) — gerçek saatle yazılsaydı hızlanınca "0.6 sn bekle"
        // 30 oyun saniyesi olurdu. Gerçek saatte kalan: bekçi (takılmaya karşı) ve iki nabız teşhisi.
        const float KareSüresi = 1f / 60f;
        static float Şimdi => Time.time;
        float _oyunBaşı;
        float _bekçiDk = 30f;
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
            _oyunBaşı = Time.time;
            if (!_izle) Time.captureDeltaTime = KareSüresi;
            _t = Resources.Load<Tuning>("Tuning");
            yield return null; yield return null;           // Bootstrap kurulsun

            string senaryo = SessionState.GetString(SenaryoAnahtar, "");
            _bekçiDk = senaryo == "tam" || senaryo == "b1" || senaryo == "minitest" ? 150f : 30f;   // uzun senaryolar
            _yalnızBölge1 = senaryo == "b1";
            _tekAraBölge = senaryo == "ab1";   // ekonomi ayarı: 7 oda + mini + sunak, ~10 dk
            IEnumerator[] sahneler = senaryo == "p26"
                ? new[] { Kurulum(), P26Tahsildar(), P26Kefil(), P26Simsar(), P26Murabahacı(), Bitir() }
                : senaryo == "tam" || senaryo == "b1" || senaryo == "ab1"
                ? new[] { Kurulum(), TamKoşu(), Bitir() }
                : senaryo == "mini"
                ? new[] { Kurulum(), MiniTeşhis(), Bitir() }
                : senaryo == "minitest"
                ? new[] { Kurulum(), MiniTesti(), Bitir() }
                : senaryo == "p29"
                ? new[] { Kurulum(), P29(), Bitir() }
                : senaryo == "p30"
                ? new[] { Kurulum(), P30(), Bitir() }
                : senaryo == "p31"
                ? new[] { Kurulum(), P31(), Bitir() }
                : senaryo == "p32"
                ? new[] { Kurulum(), P32(), Bitir() }
                : senaryo == "p33"
                ? new[] { Kurulum(), P33(), Bitir() }
                : senaryo == "simsar"
                ? new[] { Kurulum(), SimsarTeşhis(), Bitir() }
                : senaryo == "bosstest"
                ? new[] { Kurulum(), BossTesti(), Bitir() }
                : senaryo == "fiyat"
                ? new[] { Kurulum(), FiyatÖlçümü(Kadro.KONİ, Kadro.FAİZLİ, Kadro.HACİZ, Kadro.İPOTEK, Kadro.İFLAS, Kadro.REHİN), Bitir() }
                : senaryo == "fiyat33b"
                ? new[] { Kurulum(), FiyatÖlçümü(Kadro.ÇEK, Kadro.KARŞILIK, Kadro.TAHVİL, Kadro.MÜŞTEREK, Kadro.ARBİTRAJ, Kadro.KİRA), Bitir() }
                : senaryo == "p33c"
                ? new[] { Kurulum(), P33c(), Bitir() }
                : senaryo == "p42"
                ? new[] { Kurulum(), P42(), Bitir() }
                : senaryo == "p34"
                ? new[] { Kurulum(), P34(), Bitir() }
                : senaryo == "kapi"
                ? new[] { Kurulum(), KapıTesti(), Bitir() }
                : senaryo == "p35"
                ? new[] { Kurulum(), P35(), Bitir() }
                : senaryo == "p36"
                ? new[] { Kurulum(), P36(), Bitir() }
                : senaryo == "p37"
                ? new[] { Kurulum(), P37(), Bitir() }
                : senaryo == "p38"
                ? new[] { Kurulum(), P38(), Bitir() }
                : SessionState.GetInt(AdımAnahtar, 0) == 0
                ? new[] { Kurulum(), GirdiKanıtı(), K1Hazırla() }          // K1: Play'den çıkıp giriyor
                : new[] { K1Doğrula(), Bölge1(), Sunak(), Tahsildar(), Haciz(), Simsar(), Final(), Bitir() };
            if (senaryo == "mini") StartCoroutine(Nabız());   // teşhis — öbür koşuların logunu kirletmesin
            foreach (var sahne in sahneler)
            {
                if (_bitti) yield break;                     // bir sahne botu durdurduysa
                yield return sahne;
            }
        }

        /// <summary>
        /// TEŞHİS (Faz 28): mini boss odasında saat neredeyse akmıyordu ve ilk mini 20 dk'da
        /// ölmedi. 10 sn'de bir: oyun zamanı gerçekten ilerliyor mu, saat akıyor mu, neden.
        /// </summary>
        IEnumerator Nabız()
        {
            while (!_bitti)
            {
                float r0 = Time.realtimeSinceStartup, g0 = Time.time;
                var c0 = Saat;
                float akış0 = c0 != null ? c0.TotalTickDrain : 0f, s0 = c0 != null ? c0.Seconds : 0f;
                int kare = 0, donuk = 0;
                while (Time.realtimeSinceStartup - r0 < 10f) { kare++; if (Time.timeScale == 0f) donuk++; yield return null; }
                var c = Saat; var rm = Oda; var b = Boss();
                if (c == null || rm == null || c != c0) continue;
                Debug.Log($"[NABIZ] oda {rm.Room} · 10 sn gerçek → oyun {Time.time - g0:0.0} sn · {kare} kare ({donuk} donuk) · " +
                          $"saat {s0:0}→{c.Seconds:0} · saat akışı {c.TotalTickDrain - akış0:0.0} · frozen {c.Frozen} · " +
                          $"çarpan {c.DrainMultiplier:0.00} · dövüşte {c.InCombat} · düşman {Enemy.All.Count}" +
                          (b != null ? $" · boss {b.TimeLeft:0} · mesafe {Düz(b.transform.position - Ben).magnitude:0.0}" : ""));
            }
        }

        /// <summary>
        /// Enes (2026-10-14): "mini bosslarda özel bir zaman testi ve zorluk testi yap bot üzerinden".
        /// 5 bölge × 5 mini boss, hepsi M/N ile. Her dövüşe O NOKTADA BEKLENEN güçle girilir
        /// (önceki her sunaktan üç alım) ve saat o bölgenin doyma tavanında. Ölçülen: süre (oyun
        /// zamanı), saat kaybı, yenen darbe, takviye.
        /// </summary>
        IEnumerator MiniTesti()
        {
            int S = Mathf.Max(1, _t.subRegionsPerRegion);
            var satırlar = new List<string>();
            for (int b = 0; b < 5 && !_bitti; b++)
            {
                if (b > 0) { Bas(Key.N); yield return Bekle(1.5f); }
                for (int s = 0; s < S && !_bitti; s++)
                {
                    Bas(Key.M); yield return Bekle(1.5f);
                    var c = Saat; var boss = Boss();
                    if (c == null || Oda == null || !Oda.IsMiniBossRoom || boss == null)
                    { Kaydet("MT", $"B{b + 1}.{s + 1} mini boss odası açılmadı", false, Oda != null ? $"oda {Oda.Room}" : "oda yok"); continue; }

                    // Beklenen güç: önceki her sunaktan türünden bir alım (b×S + s sunak)
                    int beklenen = b * S + s;
                    foreach (Altar.Kind k in new[] { Altar.Kind.Harvest, Altar.Kind.Damage, Altar.Kind.Dash })
                        while (c.Alınan(k) < beklenen) c.Yükselt(k);
                    float giriş = Clock.Tavan(_t, b);
                    c.Revive(giriş);

                    string ad = BossAdı(boss);
                    float g0 = c.RunTime, d0 = c.TotalHitCost, h0 = c.TotalHarvested, b0 = c.TotalGranted;
                    int t0 = _takviye;
                    yield return OdayıGeç(900f);
                    c = Saat;
                    if (c == null) break;
                    bool öldü = Oda != null && Oda.IsMiniBossRoom && !Oda.Cleared;
                    float süre = c.RunTime - g0, darbe = c.TotalHitCost - d0, hasat = c.TotalHarvested - h0;
                    float kayıp = giriş + (c.TotalGranted - b0) - c.Seconds;
                    satırlar.Add($"B{b + 1}.{s + 1} {ad}: {süre / 60f:0.0} dk · saat {-kayıp:+0;−0} (%{-kayıp / giriş * 100f:+0;−0}) · darbe −{darbe:0} · hasat +{hasat:0}" +
                                 (_takviye > t0 ? $" · ⚠️ {_takviye - t0} takviye" : "") + (öldü ? " · ❌ ÖLMEDİ" : ""));
                    Debug.Log($"[MİNİ TESTİ] {satırlar[satırlar.Count - 1]}");
                }
            }
            Kaydet("MT", "Mini boss testi (beklenen güç, saat = doyma tavanı)", null, string.Join("<br>", satırlar));
        }

        // ── FAZ 29 PLAYTEST (Enes 2026-10-15: "test koşusunu bottan yaptır") ─────────
        // Gerçek oyunda, gerçek girdiyle: silah sunağı, İPOTEK YAĞMURU, dash-atak HACİZ İHBARI,
        // ÇİFT TAPU, İCRA, dönen MUAFİYET / SABIR. Kapı (Faz29SelfCheck) ETKİYİ elle ölçüyor;
        // bu, oyuncunun yolunun (tuş → sunak → alım → silah) gerçekten açık olduğunu ölçüyor.
        static readonly System.Reflection.FieldInfo İşaretAlanı =
            typeof(GroundMark).GetField("_radius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        static readonly System.Reflection.FieldInfo İşaretOyuncunun =
            typeof(GroundMark).GetField("_hitsEnemies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        /// <summary>Oyuncunun yeni ipotekleri (daha önce görülmemiş) — alanlarıyla.</summary>
        static List<float> Yeniİpotekler(HashSet<GroundMark> görülen)
        {
            var l = new List<float>();
            foreach (var m in FindObjectsByType<GroundMark>(FindObjectsSortMode.None))
            {
                if (m == null || !(bool)İşaretOyuncunun.GetValue(m) || !görülen.Add(m)) continue;
                l.Add((float)İşaretAlanı.GetValue(m));
            }
            return l;
        }

        IEnumerator P29()
        {
            _bekçiDk = 40f;
            // 1) SİLAH SUNAĞI — TAM GÜÇ ile (F7) boss çabuk düşsün; test sunağı ölçüyor, boss'u değil
            Sahne("P29 1/4 SİLAH SUNAĞI", "B → boss'u kes → boss odası sunağa dönüyor mu, iki alım");
            Bas(Key.B); yield return Bekle(1f);
            Bas(Key.F7); yield return Bekle(0.5f);
            yield return BossuÖldür(600f);
            if (_bitti) yield break;
            yield return Bekle(1f);
            int sunak = 0;
            foreach (var a in Altar.All) if (a != null && a.Type == Altar.Kind.Weapon) sunak++;
            // Faz 33 (Enes): boss'ta iki sıra — 3 yaygın/nadir + 3 epik/efsanevi.
            Kaydet("29-1", "Boss düşünce SİLAH SUNAĞI (3 + 3)", sunak == 2 * SilahSunağı.TeklifSayısı && Saat.Frozen,
                   $"{sunak} sunak · saat {(Saat.Frozen ? "durdu" : "AKIYOR")}");

            Kuşan(Kadro.İPOTEK); yield return Bekle(0.5f);
            Bas(Key.F5); yield return Bekle(0.3f);
            var alınan = new List<string>();
            float s0 = Saat.Seconds;
            for (int i = 0; i < 4; i++)
            {
                Altar hedef = null;
                foreach (var a in Altar.All) if (a != null && a.Type == Altar.Kind.Weapon && !a.SoldOut) { hedef = a; break; }
                if (hedef == null) break;
                string etiket = hedef.Label().Split('—')[0].Trim();
                yield return Git(hedef.transform.position, 1.2f, 6f);
                int alım = Saat.Purchases;
                Bas(Key.E); yield return Bekle(0.4f);
                if (Saat.Purchases == alım) break;
                alınan.Add(etiket);
            }
            bool kapalı = true;
            foreach (var a in Altar.All) if (a != null && a.Type == Altar.Kind.Weapon && !a.SoldOut) kapalı = false;
            Kaydet("29-2", "Silah sunağında her sıradan bir alım (iki), sonra hepsi kapalı", alınan.Count == 2 && kapalı,
                   $"{string.Join(", ", alınan)} · {s0 - Saat.Seconds:0} sn ödendi");

            // 2) İPOTEK — sonraki bölgenin ilk odası
            Sahne("P29 2/4 İPOTEK", "YAĞMUR · dash-atak HACİZ İHBARI · ÇİFT TAPU");
            yield return KapıdanÇık(25f);
            yield return Bekle(1f);
            Bas(Key.F5); yield return Bekle(0.3f);
            var görülen = new HashSet<GroundMark>();
            Yeniİpotekler(görülen);
            var hedefD = EnYakın();
            if (hedefD != null) NişanAl(hedefD.transform.position);
            _sağ = true;
            var yağmur = new List<float>();
            float t0 = Şimdi;
            while (Şimdi - t0 < 3.5f) { yağmur.AddRange(Yeniİpotekler(görülen)); yield return null; }
            int beklenen = Oyuncu.Weapon.Special(_t).Hits;
            Kaydet("29-3", "İPOTEK YAĞMURU (sağ tık)", yağmur.Count >= beklenen, $"{yağmur.Count} ipotek ({beklenen} bekleniyor)");

            yield return Bekle(1.5f);
            Yeniİpotekler(görülen);
            hedefD = EnYakın();
            if (hedefD != null) NişanAl(hedefD.transform.position);
            Yürü(hedefD != null ? hedefD.transform.position - Ben : Vector3.forward);
            Bas(Key.Space); yield return null;
            _sol = true; yield return null;
            Dur();
            yield return Bekle(0.6f);
            var ihbar = Yeniİpotekler(görülen);
            float normal = Oyuncu.Weapon.GroundMarkRadius(_t);
            Kaydet("29-4", "İPOTEK dash-atağı HACİZ İHBARI", ihbar.Count == 1 && ihbar[0] > normal * 1.5f,
                   ihbar.Count > 0 ? $"{ihbar.Count} işaret, alan {ihbar[0]:0.0} (normal {normal:0.0})" : "işaret yok");

            yield return Bekle(2.5f);
            int enÇok = 0;
            t0 = Şimdi;
            while (Şimdi - t0 < 2f)
            {
                hedefD = EnYakın();
                if (hedefD != null) NişanAl(hedefD.transform.position);
                if (!Oyuncu.Attacking) _sol = true;
                enÇok = Mathf.Max(enÇok, GroundMark.ActiveLiens);
                yield return null;
            }
            bool çift = SilahSunağı.Var(SilahYükseltme.ÇiftTapu);
            Kaydet("29-5", "ÇİFT TAPU: aynı anda iki ipotek", !çift || enÇok >= 2, $"en çok {enÇok} ipotek aynı anda (ÇİFT TAPU {(çift ? "alındı" : "alınmadı")})");

            // 3) İCRA
            Sahne("P29 3/4 İCRA", "FAİZLİ: bedel saatin %12'si, ödediği kadar sert");
            Kuşan(Kadro.FAİZLİ); yield return Bekle(0.5f);
            Bas(Key.F5); yield return Bekle(0.3f);
            hedefD = EnYakın();
            if (hedefD != null) { yield return Git(hedefD.transform.position, 1.8f, 4f); hedefD = EnYakın(); }
            if (hedefD != null) NişanAl(hedefD.transform.position);
            float önce = Saat.Seconds;
            var bedelAlanı = typeof(PlayerController).GetField("_özelBedeli", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _sağ = true; yield return null; yield return null;
            float ödenen = (float)bedelAlanı.GetValue(Oyuncu);
            Kaydet("29-6", "İCRA bedeli saatin yüzdesi", Mathf.Abs(ödenen - _t.icraCostPercent * önce) < 0.5f,
                   $"{ödenen:0.0} sn ödendi @ {önce:0} sn (%{ödenen / Mathf.Max(1f, önce) * 100f:0})");

            // 4) Dönen sunak: M → mini → sunak (1. ara bölge MUAFİYET, 2. SABIR)
            Sahne("P29 4/4 MUAFİYET · SABIR", "M → mini boss → sunak odasında 4. sunak");
            Kuşan(Kadro.KONİ); yield return Bekle(0.3f);
            foreach (var (tür, ad) in new[] { (Altar.Kind.Insurance, "MUAFİYET"), (Altar.Kind.Patience, "SABIR") })
            {
                Bas(Key.M); yield return Bekle(1f);
                Bas(Key.F5); yield return Bekle(0.3f);
                yield return OdayıGeç(600f);
                yield return Bekle(1f);
                Altar dördüncü = null;
                foreach (var a in Altar.All)
                    if (a != null && (a.Type == Altar.Kind.Insurance || a.Type == Altar.Kind.Patience)) dördüncü = a;
                int sayım = Saat.Alınan(tür);
                if (dördüncü != null && dördüncü.Type == tür)
                {
                    Bas(Key.F5); yield return Bekle(0.3f);
                    yield return Git(dördüncü.transform.position, 1.2f, 6f);
                    Bas(Key.E); yield return Bekle(0.4f);
                }
                Kaydet("29-7", $"Sunak odasında {ad}", dördüncü != null && dördüncü.Type == tür && Saat.Alınan(tür) == sayım + 1,
                       dördüncü == null ? "4. sunak yok" : $"4. sunak {dördüncü.Type} · alım {sayım} → {Saat.Alınan(tür)}");
                yield return KapıdanÇık(20f);
            }
        }

        // ── FAZ 30 PLAYTEST — kadro: 2 silahla başla, HACİZ sunakta, İPOTEK keşifte, KADRO, gerçek combo, İFLAS boss'tan ──
        static readonly System.Reflection.MethodInfo OdayaGir =
            typeof(RoomManager).GetMethod("EnterRoom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        IEnumerator SunakOdasına(int araBölge)
        {
            // M → mini boss → temizle → sunak odası
            Bas(Key.M); yield return Bekle(1f);
            Bas(Key.F5); yield return Bekle(0.3f);
            yield return OdayıGeç(600f);
            yield return Bekle(1f);
        }

        IEnumerator AltaraAl(Altar a)
        {
            if (a == null) yield break;
            yield return Git(a.transform.position, 1.2f, 6f);
            Bas(Key.E); yield return Bekle(0.4f);
        }

        IEnumerator P30()
        {
            _bekçiDk = 45f;
            Sahne("P30 1/5 BAŞLANGIÇ", "2 silah, 3. yuva boş");
            yield return Bekle(0.5f);
            var önce = Oyuncu.Weapon;
            Bas(Key.Digit3); yield return Bekle(0.3f);
            bool üçBoş = Oyuncu.Weapon == önce;
            Bas(Key.Digit2); yield return Bekle(0.3f);
            Kaydet("30-1", "Oyun 2 silahla başlıyor, 3 boş, 2 = FAİZLİ", Kadro.AçıkSayısı == 2 && üçBoş && Oyuncu.Weapon == Weapon.All[Kadro.FAİZLİ],
                   $"{Kadro.AçıkSayısı} açık · 3 tuşu {(üçBoş ? "hiçbir şey yapmadı" : "SİLAH DEĞİŞTİRDİ")} · 2 → {Oyuncu.Weapon.Name}");
            Bas(Key.Digit1); yield return Bekle(0.3f);

            Sahne("P30 2/5 HACİZ", "2. ara bölgenin sunağında satın");
            yield return SunakOdasına(0);
            bool ilkte = false;
            foreach (var a in Altar.All) if (a != null && a.Type == Altar.Kind.Unlock && a.Yuva == Kadro.HACİZ) ilkte = true;
            yield return KapıdanÇık(20f);
            yield return SunakOdasına(1);
            Altar haciz = null;
            foreach (var a in Altar.All) if (a != null && a.Type == Altar.Kind.Unlock && a.Yuva == Kadro.HACİZ) haciz = a;
            Bas(Key.F5); yield return Bekle(0.3f);
            yield return AltaraAl(haciz);
            Bas(Key.Digit3); yield return Bekle(0.3f);
            Kaydet("30-2", "HACİZ 2. ara bölgenin sunağında, 3. yuvaya girdi", !ilkte && haciz != null && Kadro.Açık(Kadro.HACİZ) && Oyuncu.Weapon == Weapon.All[Kadro.HACİZ],
                   $"1. sunakta {(ilkte ? "VAR" : "yok")} · 2. sunakta {(haciz != null ? "var" : "YOK")} · 3 → {Oyuncu.Weapon.Name}");

            Sahne("P30 3/5 KEŞİF", "4. ara bölgenin 4. odası: köşe → İPOTEK");
            SaveFile.MarkTestJump();
            OdayaGir.Invoke(Oda, new object[] { 3 * _t.BlokBoyu + Kadro.KeşifOdası });
            yield return Bekle(1f);
            Bas(Key.F5); yield return Bekle(0.3f);
            Bas(Key.Digit1); yield return Bekle(0.3f);
            yield return OdayıTemizle(300f);
            yield return Bekle(0.6f);
            // Bot dövüşürken köşenin üstünden geçip silahı alabiliyor (köşe kenarda, bot kenarda dolaşıyor):
            // ölçü köşenin kendisi değil, silahın KEŞİFLE açılması.
            var köşe = FindFirstObjectByType<GizliSilah>();
            if (köşe != null) yield return Git(köşe.transform.position, 0.6f, 10f);
            yield return Bekle(0.4f);
            bool keşifle = Kadro.SonMesaj != null && Kadro.SonMesaj.Contains("İPOTEK (keşif)");
            Kaydet("30-3", "Keşif: oda temizlenince köşe, üstüne yürüyünce İPOTEK", keşifle && Kadro.Açık(Kadro.İPOTEK) && Kadro.AçıkSayısı == 4,
                   $"İPOTEK {(keşifle ? "keşifle açıldı" : "KEŞİFLE AÇILMADI")} · {Kadro.AçıkSayısı} silah açık (ilk boss'tan önce 4)");
            yield return KapıdanÇık(20f);

            Sahne("P30 4/5 KADRO + COMBO", "KADRO ile İPOTEK'i 2. yuvaya al, KONİ → İPOTEK");
            yield return SunakOdasına(3);
            Bas(Key.Digit2); yield return Bekle(0.3f);                 // FAİZLİ
            Altar kadro = null;
            foreach (var a in Altar.All) if (a != null && a.Type == Altar.Kind.Loadout) kadro = a;
            yield return AltaraAl(kadro);
            Kaydet("30-4", "KADRO sunağı: 2. yuvaya İPOTEK", Kadro.Yuvadaki(1) == Kadro.İPOTEK && Oyuncu.Weapon == Weapon.All[Kadro.İPOTEK],
                   $"2. yuva {Weapon.All[Mathf.Max(0, Kadro.Yuvadaki(1))].Name} · kuşanılan {Oyuncu.Weapon.Name}");
            yield return KapıdanÇık(20f);
            yield return Bekle(1f);
            Bas(Key.F5); yield return Bekle(0.3f);
            int c0 = Oyuncu.ComboSayısı, deneme = 0;
            float t0 = Şimdi;
            while (Şimdi - t0 < 60f && Oyuncu.ComboSayısı == c0)
            {
                var d = EnYakın();
                if (d == null) { yield return null; continue; }
                Bas(Key.Digit1); yield return null;                    // KONİ
                yield return Git(d.transform.position, 1.3f, 3f);
                if (d == null) continue;
                NişanAl(d.transform.position);
                float h0 = Saat.TotalHarvested;
                _sol = true; yield return Bekle(0.4f);
                if (Saat.TotalHarvested > h0 && d != null)
                {
                    deneme++;
                    Bas(Key.Digit2); yield return null;                // İPOTEK — hızlı geçiş
                    NişanAl(d.transform.position);
                    _sol = true; yield return Bekle(0.7f);
                }
                yield return Bekle(0.2f);
                if (Saat != null && Saat.Seconds < 60f) { Bas(Key.F5); yield return Bekle(0.3f); }
            }
            Kaydet("30-5", "Gerçek tuşlarla combo: KONİ → (2) İPOTEK = TOPLU İPOTEK", Oyuncu.ComboSayısı > c0,
                   $"{Oyuncu.ComboSayısı - c0} combo · {deneme} deneme");

            Sahne("P30 5/5 BOSS", "İFLAS 1. boss'tan, combo sunağı");
            bool önceİflas = Kadro.Açık(Kadro.İFLAS);
            Bas(Key.Digit1); yield return Bekle(0.3f);
            Bas(Key.B); yield return Bekle(1f);
            Bas(Key.F5); yield return Bekle(0.3f);
            yield return BossuÖldür(900f);
            if (_bitti) yield break;
            yield return Bekle(1f);
            Altar combo = null;
            foreach (var a in Altar.All) if (a != null && a.Type == Altar.Kind.ComboPlus) combo = a;
            Bas(Key.F5); yield return Bekle(0.3f);
            yield return AltaraAl(combo);
            Kaydet("30-6", "1. boss: İFLAS açıldı, combo sunağında artı hâli alındı",
                   !önceİflas && Kadro.Açık(Kadro.İFLAS) && combo != null && Combo.Artı((ComboTür)combo.Yuva),
                   $"İFLAS {(Kadro.Açık(Kadro.İFLAS) ? "açık" : "KAPALI")} · combo sunağı {(combo != null ? Combo.Bu((ComboTür)combo.Yuva).ArtıAd : "YOK")}");
        }

        // ── FAZ 31 PLAYTEST — REHİN · ÇEK · KARŞILIK: açılış yolları + gerçek tuşlarla hamleler ve combo'lar ──
        /// <summary>Test kurgusu: silahları aç ve sırayla 1–3 yuvalarına diz.</summary>
        void Diz(params int[] silahlar)
        {
            for (int i = 0; i < silahlar.Length && i < Kadro.Yuva; i++)
            {
                Kadro.Aç(silahlar[i], "otopilot");
                Kadro.YuvayaKoy(i, silahlar[i]);
            }
        }

        static Altar AçmaSunağı(int silah)
        {
            foreach (var a in Altar.All) if (a != null && a.Type == Altar.Kind.Unlock && a.Yuva == silah) return a;
            return null;
        }

        /// <summary>En yakın düşman — rehineyi ve verilen düşmanı saymadan.</summary>
        Enemy EnYakınBaşka(Enemy hariç, float menzil = float.MaxValue)
        {
            Enemy en = null; float d = menzil;
            foreach (var e in Enemy.All)
            {
                if (e == null || e == hariç || e.Dormant || e.Tutan != null) continue;
                float x = Düz(e.transform.position - Ben).magnitude;
                if (x < d) { d = x; en = e; }
            }
            return en;
        }

        IEnumerator OdayaAtla(int oda)
        {
            SaveFile.MarkTestJump();
            OdayaGir.Invoke(Oda, new object[] { oda });
            yield return Bekle(1f);
            Bas(Key.F5); yield return Bekle(0.3f);
        }

        /// <summary>Oda temizlendiyse sonrakine geç; saat azsa doldur. Döngülerin ortak başı.</summary>
        IEnumerator Sürdür(float eşik = 60f)
        {
            if (Oda != null && Oda.Cleared) { yield return KapıdanÇık(20f); yield return Bekle(1f); }
            if (Saat != null && Saat.Seconds < eşik) { Bas(Key.F5); yield return Bekle(0.3f); }
        }

        /// <summary>
        /// Bu denemede combo oldu mu — AYNI oyuncu nesnesinde. Bot ölürse oyuncu yeniden kuruluyor ve
        /// sayaçlar sıfırlanıyor (Faz 32 botu 5. bölgede öldü, "−1 combo" yazdı).
        /// </summary>
        bool ComboOldu(PlayerController pc, int önce) => pc != null && Oyuncu == pc && pc.ComboSayısı > önce;

        static int ArtıSayısı() { int n = 0; for (int i = 0; i < Combo.Sayı; i++) if (Combo.Artı((ComboTür)i)) n++; return n; }

        IEnumerator P31()
        {
            _bekçiDk = 55f;
            int REHİN = Kadro.REHİN, ÇEK = Kadro.ÇEK, KARŞILIK = Kadro.KARŞILIK;

            Sahne("P31 1/6 REHİN AÇILIŞI", "2. bölge: 2. ara bölgenin sunağında yok, 3.'sünde satın");
            Bas(Key.N); yield return Bekle(1.2f);
            yield return OdayaAtla(RoomManager.SunakOdası(_t, 1));
            bool erken = AçmaSunağı(REHİN) != null;
            yield return OdayaAtla(RoomManager.SunakOdası(_t, 2));
            var sunak = AçmaSunağı(REHİN);
            yield return AltaraAl(sunak);
            Bas(Key.Digit3); yield return Bekle(0.3f);
            Kaydet("31-1", "REHİN 2. bölgenin 3. ara bölgesinin sunağında, 3. yuvaya girdi",
                   Oda != null && Oda.RegionIndex == 1 && !erken && sunak != null && Kadro.Açık(REHİN) && Oyuncu.Weapon == Weapon.All[REHİN],
                   $"bölge {(Oda != null ? Oda.RegionIndex + 1 : 0)} · 2. sunakta {(erken ? "VAR" : "yok")} · 3. sunakta {(sunak != null ? "var" : "YOK")} · 3 → {Oyuncu.Weapon.Name}");

            Sahne("P31 2/6 REHİN", "yakala → fırlat → (1) KONİ = SİLİP SÜPÜR");
            yield return OdayaAtla(RoomManager.SunakOdası(_t, 2) + 1);
            int yakaladı = 0, fırlattı = 0, c0 = Oyuncu.ComboSayısı, kalkan0 = Oyuncu.KalkanSayısı;
            float t0 = Şimdi;
            while (Şimdi - t0 < 150f && (Oyuncu.ComboSayısı == c0 || fırlattı == 0))
            {
                yield return Sürdür();
                var d = EnYakınBaşka(null);
                if (d == null) { yield return null; continue; }
                Bas(Key.Digit3); yield return null;                          // REHİN
                yield return Git(d.transform.position, 1.6f + d.Radius, 3f);
                if (d == null) continue;
                NişanAl(d.transform.position);
                _sol = true; yield return Bekle(0.5f);                       // YAKALA
                var r = Oyuncu.Rehine;
                if (r == null) continue;
                yakaladı++;
                var hedef = EnYakınBaşka(r, 4f);
                if (hedef == null) { yield return Bekle(1.2f); hedef = EnYakınBaşka(r, 6f); }   // gelen vuruşu rehine yesin
                NişanAl(hedef != null ? hedef.transform.position : Ben + Oyuncu.transform.forward * 3f);
                _sol = true; yield return Bekle(0.3f);                       // FIRLAT
                if (Oyuncu.Rehine != null) continue;
                fırlattı++;
                var süpür = EnYakınBaşka(null, 2.6f);
                Bas(Key.Digit1); yield return null;                          // KONİ — hızlı geçiş
                if (süpür != null) NişanAl(süpür.transform.position);
                _sol = true; yield return Bekle(0.5f);
            }
            Kaydet("31-2", "REHİN gerçek tuşlarla: yakala → fırlat → (1) KONİ = SİLİP SÜPÜR",
                   yakaladı > 0 && fırlattı > 0 && Oyuncu.ComboSayısı > c0,
                   $"{yakaladı} yakalama · {fırlattı} fırlatma · {Oyuncu.ComboSayısı - c0} combo · kalkan {Oyuncu.KalkanSayısı - kalkan0} vuruş yedi");

            Sahne("P31 3/6 ÇEK AÇILIŞI", "2. boss → ÇEK");
            bool önceÇek = Kadro.Açık(ÇEK);
            Bas(Key.Digit1); yield return Bekle(0.3f);
            Bas(Key.B); yield return Bekle(1f);
            Bas(Key.F5); yield return Bekle(0.3f);
            yield return BossuÖldür(900f);
            if (_bitti) yield break;
            yield return Bekle(1f);
            Kaydet("31-3", "2. boss düşünce ÇEK açıldı", !önceÇek && Kadro.Açık(ÇEK) && Kadro.SonMesaj != null && Kadro.SonMesaj.Contains("ÇEK"),
                   $"ÇEK {(Kadro.Açık(ÇEK) ? "açık" : "KAPALI")} · \"{Kadro.SonMesaj}\"");

            Sahne("P31 4/6 ÇEK", "fırlat → değince (2) HACİZ = SENET HACZİ");
            yield return KapıdanÇık(30f);                                    // 3. bölgenin ilk odası
            yield return Bekle(1f);
            Diz(ÇEK, Kadro.HACİZ, Kadro.KONİ);
            Bas(Key.F5); yield return Bekle(0.3f);
            int atış = 0, değdi = 0, enÇokİsabet = 0;
            c0 = Oyuncu.ComboSayısı;
            float bant = Weapon.All[Kadro.HACİZ].Range(_t) * 0.7f;
            t0 = Şimdi;
            while (Şimdi - t0 < 150f && (Oyuncu.ComboSayısı == c0 || enÇokİsabet < 2))
            {
                yield return Sürdür();
                var d = EnYakınBaşka(null);
                if (d == null) { yield return null; continue; }
                Bas(Key.Digit1); yield return null;                          // ÇEK
                yield return Git(d.transform.position, bant, 3f);            // HACİZ'in bandında dur
                if (d == null) continue;
                NişanAl(d.transform.position);
                _sol = true;
                // ⚠️ İsabeti ÇEKİN KENDİSİNDEN oku. Havuz kendi kendine de akıyor: "havuz düştü" ilk koşuda
                // çek daha atılmadan HACİZ'e geçirtti, geçiş de ÇEK savurmasını iptal etti (0 combo).
                CekUcusu çek = null;
                float ta = Şimdi;
                while (çek == null && Şimdi - ta < 0.5f)
                {
                    if (CekUcusu.Uçanlar.Count > 0) çek = CekUcusu.Uçanlar[0];
                    yield return null;
                }
                if (ReferenceEquals(çek, null)) continue;
                atış++;
                while (çek != null && çek.İsabet == 0 && Şimdi - ta < 1.5f) yield return null;
                bool isabet = çek.İsabet > 0;                                // yok edilse de C# nesnesinden okunur
                if (isabet)
                {
                    değdi++;
                    Bas(Key.Digit2); yield return null;                      // HACİZ — hızlı geçiş
                    if (d != null) NişanAl(d.transform.position);
                    _sol = true;
                }
                float tb = Şimdi;
                while (çek != null && Şimdi - tb < 2.5f) yield return null;   // çek dönene kadar
                enÇokİsabet = Mathf.Max(enÇokİsabet, çek.İsabet);
            }
            Kaydet("31-4", "ÇEK gerçek tuşlarla: gidip dönüyor, değince (2) HACİZ = SENET HACZİ",
                   değdi > 0 && enÇokİsabet >= 2 && Oyuncu.ComboSayısı > c0,
                   $"{atış} atış · {değdi} isabet · bir çekte en çok {enÇokİsabet} isabet (gidiş + dönüş) · {Oyuncu.ComboSayısı - c0} combo");

            Sahne("P31 5/6 KARŞILIK AÇILIŞI", "3. bölgenin 3. ara bölgesi, 4. oda: köşe");
            bool önceKarşılık = Kadro.Açık(KARŞILIK);
            Diz(Kadro.KONİ, Kadro.HACİZ, ÇEK);
            yield return OdayaAtla(2 * _t.BlokBoyu + Kadro.KeşifOdası);
            Bas(Key.Digit1); yield return Bekle(0.3f);
            yield return OdayıTemizle(300f);
            yield return Bekle(0.6f);
            var köşe = FindFirstObjectByType<GizliSilah>();
            if (köşe != null) yield return Git(köşe.transform.position, 0.6f, 10f);
            yield return Bekle(0.4f);
            bool keşifle = Kadro.SonMesaj != null && Kadro.SonMesaj.Contains("KARŞILIK (keşif)");
            Kaydet("31-5", "KARŞILIK keşifle: oda temizlenince köşe, üstüne yürüyünce açılıyor", !önceKarşılık && keşifle && Kadro.Açık(KARŞILIK),
                   $"bölge {(Oda != null ? Oda.RegionIndex + 1 : 0)} · KARŞILIK {(keşifle ? "keşifle açıldı" : "KEŞİFLE AÇILMADI")}");

            Sahne("P31 6/6 KARŞILIK", "karşıla → biriken iner · (2) FAİZLİ = KARŞI İCRA");
            yield return KapıdanÇık(20f);
            yield return Bekle(1f);
            Diz(KARŞILIK, Kadro.FAİZLİ, Kadro.KONİ);
            Bas(Key.F5); yield return Bekle(0.3f);
            int k0 = Oyuncu.KarşılananSayısı, indi = 0, vuruş = 0;
            c0 = Oyuncu.ComboSayısı;
            t0 = Şimdi;
            while (Şimdi - t0 < 180f && (Oyuncu.ComboSayısı == c0 || Oyuncu.KarşılananSayısı == k0 || indi == 0))
            {
                yield return Sürdür();
                var d = EnYakınBaşka(null);
                if (d == null) { yield return null; continue; }
                Bas(Key.Digit1); yield return null;                          // KARŞILIK
                yield return Git(d.transform.position, 1.4f + d.Radius, 3f);
                if (d == null) continue;
                NişanAl(d.transform.position);
                float h0 = Saat.TotalHarvested, bÖnce = Oyuncu.Biriken;
                int kÖnce = Oyuncu.KarşılananSayısı;
                _sol = true; yield return Bekle(0.3f);                       // pencere + biriken iner
                bool vurdu = Saat.TotalHarvested > h0;                       // havuz kendi akar — hasat yalnızca vuruşla
                if (!vurdu) { yield return Bekle(0.45f); continue; }         // toparlanma
                vuruş++;
                if ((bÖnce > 0f || Oyuncu.KarşılananSayısı > kÖnce) && Oyuncu.Biriken == 0f) indi++;   // biriken BU vuruşa indi
                Bas(Key.Digit2); yield return null;                          // FAİZLİ — hızlı geçiş
                if (d != null) NişanAl(d.transform.position);
                _sol = true; yield return Bekle(0.6f);
            }
            Kaydet("31-6", "KARŞILIK gerçek oyunda: karşılıyor, biriken vuruşa iniyor, (2) FAİZLİ = KARŞI İCRA",
                   Oyuncu.KarşılananSayısı > k0 && indi > 0 && Oyuncu.ComboSayısı > c0,
                   $"{Oyuncu.KarşılananSayısı - k0} darbe karşılandı · {vuruş} KARŞILIK isabeti ({indi}'inde biriken vardı) · {Oyuncu.ComboSayısı - c0} combo");
        }

        // ── FAZ 32 PLAYTEST — TAHVİL · MÜŞTEREK · ARBİTRAJ · KİRA ──────────────────────────────
        /// <summary>
        /// Bölgenin ana boss'unu kes. N ile atlanan bölgelerin sunakları hiç alınmadığı için bot taban güçle
        /// kalıyordu (3. boss 15 dk'ya sığmadı): MiniTesti'nin "beklenen güç"ü — bu bölge dahil her sunaktan
        /// türünden bir alım. Ölçülen şey silahın boss'tan AÇILMASI, boss dövüşü değil.
        /// </summary>
        IEnumerator BossuKes(float sınır = 900f)
        {
            Diz(Kadro.KONİ, Kadro.FAİZLİ, Kadro.İFLAS);
            Bas(Key.Digit1); yield return Bekle(0.3f);
            Bas(Key.B); yield return Bekle(1f);
            var c = Saat;
            if (c != null && Oda != null)
            {
                int beklenen = (Oda.RegionIndex + 1) * Mathf.Max(1, _t.subRegionsPerRegion);
                foreach (Altar.Kind k in new[] { Altar.Kind.Harvest, Altar.Kind.Damage, Altar.Kind.Dash })
                    while (c.Alınan(k) < beklenen) c.Yükselt(k);
            }
            Bas(Key.F5); yield return Bekle(0.3f);
            yield return BossuÖldür(sınır);
        }

        /// <summary>
        /// TEŞHİS (2026-10-18): P32 5/8'de SİMSAR beklenen güçle 30 dk'da ölmedi; 3. boss aynı kurulumla öldü.
        /// P32'nin yolu (N N → 3. boss → çık → B → 4. boss), ikisinde de 10 sn'de bir `[BOSS-NABIZ]`.
        /// ⚠️ Sonuç (2026-10-20): ölüyordu — ikisinde de ~940 sn. P32 MÜBAŞİR'ler sağken bakıyordu (`BossuÖldür`).
        /// </summary>
        IEnumerator SimsarTeşhis()
        {
            _bekçiDk = 60f;
            var nabız = StartCoroutine(BossNabzı());
            Bas(Key.N); yield return Bekle(1.2f);
            Bas(Key.N); yield return Bekle(1.2f);
            yield return BossuKes();
            if (_bitti) yield break;
            Kaydet("S-1", "3. boss (kontrol) öldü", Boss() == null, BossAdı(Boss()));
            yield return Bekle(1f);
            // ⚠️ KapıdanÇık YETMİYOR: KEFİL'in çağırdıkları sağken oda temiz değil, kapı kapalı —
            // bot 30 sn kapıya yürüyüp kaldı, B yine KEFİL'i açtı (ilk teşhis koşusu).
            yield return OdayıGeç(60f);
            yield return Bekle(1f);
            Kaydet("S-1b", "4. bölgeye geçildi", Oda != null && Oda.RegionIndex == 3, $"bölge {(Oda != null ? Oda.RegionIndex + 1 : 0)}");
            yield return BossuKes(1800f);
            if (_bitti) yield break;
            Kaydet("S-2", "4. boss öldü", Boss() == null, BossAdı(Boss()));
            StopCoroutine(nabız);
        }

        /// <summary>
        /// Enes (2026-10-20): "bossların uzun sürmesi okeydir ama her bir boss öncekinden daha uzun sürmeli."
        /// 5 ana boss, her biri yeni koşuda (F6 → N × bölge → B), `BossuKes`'in beklenen gücüyle. Ölçülen: dövüş
        /// süresi (oyun zamanı), faz 2'nin başladığı an, havuz, darbe, F5.
        /// </summary>
        IEnumerator BossTesti()
        {
            var satırlar = new List<string>();
            float önceki = 0f; bool sıralı = true;
            for (int b = 0; b < BossKind.All.Length && !_bitti; b++)
            {
                Bas(Key.F6); yield return Bekle(0.6f);
                _pc = null; _kam = null;
                for (int n = 0; n < b; n++) { Bas(Key.N); yield return Bekle(1.2f); }
                yield return BossuKes(0f);                         // sınır 0 = yalnızca kurulum, dövüşü burada sayıyoruz
                var c = Saat; var boss = Boss();
                if (c == null || boss == null) { satırlar.Add($"B{b + 1}: boss odası açılmadı"); sıralı = false; continue; }
                string ad = BossAdı(boss);
                // Saat = bölgenin doyma tavanı (MiniTesti gibi). ⚠️ F5'in 300 sn'si ve 35 sn eşiği 5. bölgede tek
                // vuruştan az (~100–200 sn): bot MURABAHACI'da 4 dk'da üç kez öldü, süre ölçülemedi (ilk koşu).
                float giriş = Clock.Tavan(_t, b);
                c.Revive(giriş);
                float havuz = boss.TimeLeft, d0 = c.TotalHitCost, t0 = Şimdi, faz2 = -1f, s0 = c.Seconds, b0 = c.TotalGranted;
                int f0 = _takviye;
                while (Şimdi - t0 < 3600f && !_bitti && Saat == c && !c.Dead && Oda != null && Oda.IsBossRoom)   // ölüp kontrol noktasına dönmek "öldürdü" sayılmasın
                {
                    var bb = Boss();
                    if (bb == null) break;
                    if (c.Seconds < giriş * 0.4f) { c.Revive(giriş); _takviye++; }
                    if (faz2 < 0f && bb.Phase >= 2) faz2 = Şimdi - t0;
                    DövüşAdımı(Mod.Dövüş, bb);
                    yield return null;
                }
                Dur();
                float süre = Şimdi - t0;
                bool öldü = Boss() == null && Saat == c && !c.Dead && Oda != null && Oda.IsBossRoom;
                bool kısa = b > 0 && süre <= önceki;
                float kayıp = s0 + (c.TotalGranted - b0) - c.Seconds;               // takviyeler düşülmüş net saat kaybı
                // ⚠️ Faz 41'den beri yine KAPI: ekonomi sıkılınca gerçek koşuda saat tavanın 1.4–2.8 katında; bot sunakta tavanın altına
                // harcamıyor, yani boss'a en az tavanla giriyor — tavanla giren botu öldürmeyen boss gerçek koşuda da öldürmez.
                bool botuÖldürdü = kayıp >= giriş;
                if (!öldü || kısa || botuÖldürdü) sıralı = false;
                önceki = süre;
                satırlar.Add($"B{b + 1} {ad}: {süre / 60f:0.0} dk" + (faz2 >= 0f ? $" · faz 2 {faz2 / 60f:0.0}. dk'da" : "") +
                             $" · havuz {havuz:0} · saat −{kayıp:0} / giriş {giriş:0} · darbe −{c.TotalHitCost - d0:0} · F5 {_takviye - f0}" +
                             (öldü ? "" : " · ❌ ÖLMEDİ") + (kısa ? " · ⚠️ öncekinden kısa" : "") + (botuÖldürdü ? " · ⚠️ BOTU ÖLDÜRÜRDÜ (kayıp > giriş)" : ""));
                Debug.Log($"[BOSS TESTİ] {satırlar[satırlar.Count - 1]}");
            }
            Kaydet("BT", "Her ana boss öncekinden uzun sürüyor ve tavanla giren botu öldürmüyor (beklenen güç)", sıralı, string.Join("<br>", satırlar));
        }

        /// <summary>10 sn OYUN zamanında bir: havuz, inen hasar (karelik düşüşlerin toplamı), botun dalları.</summary>
        IEnumerator BossNabzı()
        {
            float başla = 0f; string önceki = null;
            while (!_bitti)
            {
                float g0 = Time.time, r0 = Time.realtimeSinceStartup;
                var c0 = Saat;
                float darbe0 = c0 != null ? c0.TotalHitCost : 0f, hasar = 0f, mesafe = 0f, havuz = -1f;
                int t0 = _takviye, kare = 0, sw0 = Oyuncu != null ? Oyuncu.SwingsSpawned : 0;
                _dal.Clear();
                while (Time.time - g0 < 10f && !_bitti)
                {
                    float h = 0f; Enemy bb = null;
                    foreach (var e in Enemy.All)                       // KEFİL ikiye bölünüyor: hepsinin toplamı
                        if (e != null && e.IsBoss && !e.SummonedRival) { h += e.TimeLeft; bb = e; }
                    if (bb != null && havuz >= 0f && h < havuz) hasar += havuz - h;   // fullenme (artış) sayılmıyor
                    havuz = bb != null ? h : -1f;
                    if (bb != null) { mesafe += Düz(bb.transform.position - Ben).magnitude; kare++; }
                    yield return null;
                }
                var boss = Boss(); var c = Saat;
                if (boss == null || c == null || c != c0 || kare == 0) continue;
                if (boss.Boss.Name != önceki) { başla = g0; önceki = boss.Boss.Name; }
                float pencere = Mathf.Max(0.01f, Time.time - g0);
                int yardımcı = 0, işaret = 0;
                foreach (var e in Enemy.All) if (e != null && !e.IsBoss) yardımcı++;
                foreach (var g in FindObjectsByType<GroundMark>(FindObjectsSortMode.None))
                    if (g != null && g.OyuncuyaVurur && !g.Fired) işaret++;
                var dallar = new List<KeyValuePair<string, float>>(_dal);
                dallar.Sort((x, y) => y.Value.CompareTo(x.Value));
                var sb = new StringBuilder();
                foreach (var kv in dallar) sb.Append($" {kv.Key} %{100f * kv.Value / pencere:0}");
                int sw = (Oyuncu != null ? Oyuncu.SwingsSpawned : 0) - sw0;
                Debug.Log($"[BOSS-NABIZ] {boss.Boss.Name} faz {boss.Phase}{(boss.OnCredit ? " KREDİ" : "")} · {Time.time - başla:0} sn · " +
                          $"havuz {havuz:0} · hasar {hasar:0} · savurma {sw} · hasar gücü {c.AttackDamage:0.00} · mesafe {mesafe / kare:0.0} · " +
                          $"yardımcı {yardımcı} · işaret {işaret} · saat {c.Seconds:0} · F5 {_takviye - t0} · darbe {c.TotalHitCost - darbe0:0} · " +
                          $"gerçek {Time.realtimeSinceStartup - r0:0.0} sn |{sb}");
            }
        }

        IEnumerator P32()
        {
            _bekçiDk = 120f;
            int TAHVİL = Kadro.TAHVİL, MÜŞTEREK = Kadro.MÜŞTEREK, ARBİTRAJ = Kadro.ARBİTRAJ, KİRA = Kadro.KİRA;

            Sahne("P32 1/8 TAHVİL AÇILIŞI", "N N → 3. bölge, B → 3. boss");
            Bas(Key.N); yield return Bekle(1.2f);
            Bas(Key.N); yield return Bekle(1.2f);
            bool önceTahvil = Kadro.Açık(TAHVİL);
            yield return BossuKes();
            if (_bitti) yield break;
            yield return Bekle(1f);
            Kaydet("32-1", "3. boss düşünce TAHVİL açıldı", !önceTahvil && Kadro.Açık(TAHVİL) && Kadro.SonMesaj != null && Kadro.SonMesaj.Contains("TAHVİL"),
                   $"\"{Kadro.SonMesaj}\"");

            Sahne("P32 2/8 TAHVİL", "basılı tut → 3. kademe → bırak · (2) FAİZLİ = VADE FARKI");
            yield return KapıdanÇık(30f);
            yield return Bekle(1f);
            Diz(TAHVİL, Kadro.FAİZLİ, Kadro.KONİ);
            Bas(Key.F5); yield return Bekle(0.3f);
            int enYüksek = -1, olgun = 0, combo = 0;
            float t0 = Şimdi;
            while (Şimdi - t0 < 150f && (olgun == 0 || combo == 0))
            {
                yield return Sürdür(200f);
                var pc = Oyuncu; int cb = pc != null ? pc.ComboSayısı : 0;
                var d = EnYakınBaşka(null);
                if (d == null) { yield return null; continue; }
                Bas(Key.Digit1); yield return null;                          // TAHVİL
                yield return Git(d.transform.position, 1.2f + d.Radius, 3f);
                if (d == null) continue;
                int kademe = -1;
                float tb = Şimdi;
                while (Şimdi - tb < 1.7f && d != null)                     // basılı tut
                {
                    NişanAl(d.transform.position);
                    _sol = true;
                    kademe = Mathf.Max(kademe, Oyuncu.VadeKademesi);
                    yield return null;
                }
                enYüksek = Mathf.Max(enYüksek, kademe);
                float h0 = Saat.TotalHarvested;
                yield return null; yield return null;                        // bırak → iner
                yield return Bekle(0.1f);
                if (Saat.TotalHarvested <= h0) continue;                     // ıska
                if (kademe >= 3) olgun++;
                Bas(Key.Digit2); yield return null;                          // FAİZLİ — hızlı geçiş
                if (d != null) NişanAl(d.transform.position);
                _sol = true; yield return Bekle(0.5f);
                if (ComboOldu(pc, cb)) combo++;
            }
            Kaydet("32-2", "TAHVİL gerçek tuşlarla: basılı tut → 3. kademe → bırak · (2) FAİZLİ = VADE FARKI",
                   olgun > 0 && enYüksek == 3 && combo > 0,
                   $"en yüksek kademe {enYüksek} · {olgun} olgun isabet · {combo} combo");

            Sahne("P32 3/8 MÜŞTEREK AÇILIŞI", "4. bölge: 2. sunakta yok, 3.'sünde satın");
            yield return OdayaAtla(RoomManager.SunakOdası(_t, 1));
            bool erken = AçmaSunağı(MÜŞTEREK) != null;
            yield return OdayaAtla(RoomManager.SunakOdası(_t, 2));
            var sunak = AçmaSunağı(MÜŞTEREK);
            yield return AltaraAl(sunak);
            Kaydet("32-3", "MÜŞTEREK 4. bölgenin 3. ara bölgesinin sunağında",
                   Oda != null && Oda.RegionIndex == 3 && !erken && sunak != null && Kadro.Açık(MÜŞTEREK),
                   $"bölge {(Oda != null ? Oda.RegionIndex + 1 : 0)} · 2. sunakta {(erken ? "VAR" : "yok")} · 3. sunakta {(sunak != null ? "var" : "YOK")}");

            Sahne("P32 4/8 MÜŞTEREK", "iki düşmanı bağla → birine KONİ'yle vur, öbürüne de insin");
            yield return OdayaAtla(RoomManager.SunakOdası(_t, 2) + 1);
            Diz(MÜŞTEREK, Kadro.KONİ, TAHVİL);
            int bağlandı = 0, yayıldı = 0;
            t0 = Şimdi;
            while (Şimdi - t0 < 150f && yayıldı == 0)
            {
                yield return Sürdür(200f);
                var a = EnYakınBaşka(null);
                if (a == null) { yield return null; continue; }
                Bas(Key.Digit1); yield return null;                          // MÜŞTEREK
                yield return Git(a.transform.position, 1.6f + a.Radius, 3f);
                if (a == null) continue;
                NişanAl(a.transform.position); _sol = true; yield return Bekle(0.4f);
                var b = EnYakınBaşka(a, 6f);
                if (b == null) continue;
                yield return Git(b.transform.position, 1.6f + b.Radius, 3f);
                if (b == null) continue;
                NişanAl(b.transform.position); _sol = true; yield return Bekle(0.4f);
                var bağ = Oyuncu.SonBağ;
                if (bağ == null) continue;
                bağlandı++;
                int y0 = bağ.Yayılma;
                var hedef = bağ.Üyeler.Count > 0 ? bağ.Üyeler[0] : null;
                if (hedef == null) continue;
                Bas(Key.Digit2); yield return null;                          // KONİ
                yield return Git(hedef.transform.position, 1.4f + hedef.Radius, 2f);
                if (hedef != null) NişanAl(hedef.transform.position);
                _sol = true; yield return Bekle(0.4f);
                if (bağ.Yayılma > y0) yayıldı++;                             // yok edilse de C# nesnesinden
            }
            Kaydet("32-4", "MÜŞTEREK gerçek tuşlarla: iki vuruşla bağ, KONİ'nin vuruşu bağlıya da indi",
                   bağlandı > 0 && yayıldı > 0, $"{bağlandı} bağ · {yayıldı} yayılma");

            Sahne("P32 5/8 ARBİTRAJ AÇILIŞI", "B → 4. boss");
            bool önceArb = Kadro.Açık(ARBİTRAJ);
            yield return BossuKes(1800f);                 // SİMSAR ~940 sn (faz 2 havuzu fullenip dönüyor)
            if (_bitti) yield break;
            yield return Bekle(1f);
            Kaydet("32-5", "4. boss düşünce ARBİTRAJ açıldı", !önceArb && Kadro.Açık(ARBİTRAJ) && Kadro.SonMesaj != null && Kadro.SonMesaj.Contains("ARBİTRAJ"),
                   $"\"{Kadro.SonMesaj}\"");

            Sahne("P32 6/8 ARBİTRAJ", "görünmezken arkadan · TERS İŞLEM · REHİN → ARBİTRAJ = AÇIĞA SATIŞ");
            if (Oda != null && Oda.RegionIndex == 3 && Boss() != null) { Bas(Key.N); yield return Bekle(1.2f); }
            else yield return KapıdanÇık(30f);
            yield return Bekle(1f);
            Diz(ARBİTRAJ, Kadro.REHİN, Kadro.KONİ);
            Bas(Key.F5); yield return Bekle(0.3f);
            int gizli = 0, ters = 0, açığa = 0;
            t0 = Şimdi;
            while (Şimdi - t0 < 240f && (gizli == 0 || ters == 0 || açığa == 0))
            {
                yield return Sürdür(200f);
                var pc = Oyuncu; int cb = pc != null ? pc.ComboSayısı : 0;
                var d = EnYakınBaşka(null);
                if (d == null) { yield return null; continue; }
                Bas(Key.Digit1); yield return null;                          // ARBİTRAJ
                int a0 = Oyuncu.ArkadanVuruş;
                if (gizli == 0 && Oyuncu.SpecialCooldownLeft <= 0f)          // AÇIK POZİSYON → arkadan
                {
                    _sağ = true; yield return Bekle(0.1f);
                    yield return Git(d.transform.position, 1.2f + d.Radius, 2f);
                    if (d == null) continue;
                    NişanAl(d.transform.position); _sol = true; yield return Bekle(0.3f);
                    if (Oyuncu == pc && pc.ArkadanVuruş > a0) gizli++;
                    continue;
                }
                if (ters == 0)                                               // TERS İŞLEM: içinden geçen dash
                {
                    yield return Git(d.transform.position, 1.1f, 3f);
                    if (d == null) continue;
                    Yürü(d.transform.position - Ben); Bas(Key.Space);
                    yield return null; yield return null;
                    _sol = true; yield return null;                          // dash sırasında sol tık
                    Dur(); yield return Bekle(0.4f);
                    if (Oyuncu == pc && pc.ArkadanVuruş > a0) ters++;
                    continue;
                }
                // AÇIĞA SATIŞ: REHİN'le yakala, fırlat, hemen ARBİTRAJ
                Bas(Key.Digit2); yield return null;                          // REHİN
                yield return Git(d.transform.position, 1.6f + d.Radius, 3f);
                if (d == null) continue;
                NişanAl(d.transform.position); _sol = true; yield return Bekle(0.4f);
                var r = Oyuncu.Rehine;
                if (r == null) continue;
                var öbür = EnYakınBaşka(r, 4f);
                NişanAl(öbür != null ? öbür.transform.position : Ben + Oyuncu.transform.forward * 3f);
                _sol = true; yield return Bekle(0.3f);                       // FIRLAT
                if (Oyuncu.Rehine != null) continue;
                var vur = EnYakınBaşka(null, 2.4f);
                Bas(Key.Digit1); yield return null;                          // ARBİTRAJ — hızlı geçiş
                if (vur != null) NişanAl(vur.transform.position);
                _sol = true; yield return Bekle(0.4f);
                if (ComboOldu(pc, cb)) açığa++;
            }
            Kaydet("32-6", "ARBİTRAJ gerçek tuşlarla: görünmezken arkadan · TERS İŞLEM arkadan · AÇIĞA SATIŞ",
                   gizli > 0 && ters > 0 && açığa > 0,
                   $"görünmez arkadan {gizli} · ters işlem {ters} · {açığa} combo");

            Sahne("P32 7/8 KİRA AÇILIŞI", "5. bölgenin 3. ara bölgesi, 4. oda: köşe");
            bool önceKira = Kadro.Açık(KİRA);
            Diz(Kadro.KONİ, ARBİTRAJ, Kadro.REHİN);
            yield return OdayaAtla(2 * _t.BlokBoyu + Kadro.KeşifOdası);
            Bas(Key.Digit1); yield return Bekle(0.3f);
            int keşifOdası = Oda != null ? Oda.Room : -1;
            yield return OdayıTemizle(300f);
            yield return Bekle(0.6f);
            var köşe = FindFirstObjectByType<GizliSilah>();
            if (köşe != null) yield return Git(köşe.transform.position, 0.6f, 10f);
            yield return Bekle(0.4f);
            bool keşifle = Kadro.SonMesaj != null && Kadro.SonMesaj.Contains("KİRA (keşif)");
            Kaydet("32-7", "KİRA keşifle açıldı", !önceKira && keşifle && Kadro.Açık(KİRA),
                   $"bölge {(Oda != null ? Oda.RegionIndex + 1 : 0)} oda {(Oda != null ? Oda.Room : -1)} (keşif odası {keşifOdası}) · KİRA {(keşifle ? "keşifle açıldı" : "KEŞİFLE AÇILMADI")}");

            Sahne("P32 8/8 KİRA", "kule kur → ateş eder · TAHLİYE · MÜŞTEREK → KİRA = ORTAK GİDER");
            yield return KapıdanÇık(20f);
            yield return Bekle(1f);
            Diz(KİRA, MÜŞTEREK, Kadro.KONİ);
            Bas(Key.F5); yield return Bekle(0.3f);
            int atış = 0, tahliye = 0, ortak = 0;
            t0 = Şimdi;
            while (Şimdi - t0 < 240f && (atış == 0 || tahliye == 0 || ortak == 0))
            {
                yield return Sürdür(200f);
                var pc = Oyuncu; int cb = pc != null ? pc.ComboSayısı : 0;
                var d = EnYakınBaşka(null);
                if (d == null) { yield return null; continue; }
                if (atış == 0 || tahliye == 0)
                {
                    Bas(Key.Digit1); yield return null;                      // KİRA
                    if (Oyuncu.Kule == null) { NişanAl(d.transform.position); _sol = true; yield return Bekle(0.3f); }
                    var kule = Oyuncu.Kule;
                    if (kule == null) continue;
                    float tk = Şimdi;
                    while (kule.Atış == 0 && !kule.Bitti && Şimdi - tk < 4f) yield return null;
                    if (kule.Atış > 0) atış++;
                    _sağ = true; yield return Bekle(0.3f);                   // TAHLİYE
                    if (kule.Bitti && Oyuncu.Kule == null) tahliye++;
                    continue;
                }
                // ORTAK GİDER: MÜŞTEREK'le iki düşmanı bağla, hemen KİRA
                Bas(Key.Digit2); yield return null;                          // MÜŞTEREK
                yield return Git(d.transform.position, 1.6f + d.Radius, 3f);
                if (d == null) continue;
                NişanAl(d.transform.position); _sol = true; yield return Bekle(0.35f);
                var b = EnYakınBaşka(d, 3f);
                if (b == null) continue;
                NişanAl(b.transform.position); _sol = true; yield return Bekle(0.3f);
                if (Oyuncu.SonBağ == null) continue;
                Bas(Key.Digit1); yield return null;                          // KİRA — hızlı geçiş
                NişanAl(b != null ? b.transform.position : Ben);
                _sol = true; yield return Bekle(0.4f);
                if (ComboOldu(pc, cb)) ortak++;
                else if (Oyuncu.Kule != null) { _sağ = true; yield return Bekle(0.3f); }   // olmadıysa kuleyi kaldır, yeniden dene
            }
            Kaydet("32-8", "KİRA gerçek tuşlarla: kule ateş etti · TAHLİYE · MÜŞTEREK → KİRA = ORTAK GİDER",
                   atış > 0 && tahliye > 0 && ortak > 0,
                   $"{atış} kule ateş etti · {tahliye} tahliye · {ortak} combo");
        }

        // ── FAZ 33a PLAYTEST — enderlik: mini boss sunağı (1 yaygın/nadir) · boss sunağı (1 + 1) ──
        static List<Altar> SilahSunakları(SunakGrubu g)
        {
            var l = new List<Altar>();
            foreach (var a in Altar.All) if (a != null && a.Type == Altar.Kind.Weapon && a.Grup == g) l.Add(a);
            return l;
        }

        static string Etiketler(List<Altar> l)
        {
            var sb = new StringBuilder();
            foreach (var a in l)
            {
                var y = a.Yükseltme();
                sb.Append(y.HasValue ? $"[{SilahSunağı.EnderlikAdı[(int)SilahSunağı.Ender[(int)y.Value]]}] {SilahSunağı.Ad[(int)y.Value]}" : "—").Append(a.SoldOut ? " (kapalı)" : "").Append(" · ");
            }
            return sb.ToString();
        }

        IEnumerator P33()
        {
            _bekçiDk = 40f;
            Sahne("P33 1/2 MİNİ SUNAĞI", "M → mini boss → sunak odası: kuşanılan silahın 3 yaygın/nadir yükseltmesi, 1 alım");
            yield return SunakOdasına(0);
            Bas(Key.F5); yield return Bekle(0.3f);
            var mini = SilahSunakları(SunakGrubu.Mini);
            bool ender = mini.Count == 3, silahın = true;
            foreach (var a in mini)
            {
                var y = a.Yükseltme();
                if (y.HasValue && SilahSunağı.Ender[(int)y.Value] > Enderlik.Nadir) ender = false;
                if (y.HasValue && SilahSunağı.Silahı(y.Value) != System.Array.IndexOf(Weapon.All, Oyuncu.Weapon)) silahın = false;
            }
            int önce = SilahSunağı.AlınanSayısı;
            Altar ilk = null, öbür = null;
            foreach (var a in mini) { if (ilk == null && !a.SoldOut) ilk = a; else if (öbür == null && a != ilk) öbür = a; }
            yield return AltaraAl(ilk);
            bool aldı = SilahSunağı.AlınanSayısı == önce + 1;
            yield return AltaraAl(öbür);                                     // ikinci alım denemesi
            bool ikinciYok = SilahSunağı.AlınanSayısı == önce + 1;
            Kaydet("33-1", "Mini boss sunağı: kuşanılan silahın 3 yaygın/nadir yükseltmesi, [E] ile 1 alım, ikincisi kapalı",
                   ender && silahın && aldı && ikinciYok,
                   $"{mini.Count} sunak · {Etiketler(mini)}şans sunağı {SilahSunakları(SunakGrubu.Şans).Count}");

            Sahne("P33 2/2 BOSS SUNAĞI", "B → 1. boss → 3 yaygın/nadir + 3 epik/efsanevi, her sıradan 1");
            yield return BossuKes();
            if (_bitti) yield break;
            yield return Bekle(1f);
            Bas(Key.F5); yield return Bekle(0.3f);
            var alt = SilahSunakları(SunakGrubu.BossAlt);
            var üst = SilahSunakları(SunakGrubu.BossÜst);
            string önceEtiket = $"alt: {Etiketler(alt)}| üst: {Etiketler(üst)}";
            int ö2 = SilahSunağı.AlınanSayısı;
            Altar altta = null, üstte = null;
            foreach (var a in alt) if (altta == null && !a.SoldOut) altta = a;
            // Enes'in testi (masa #13): dış halkadaki combo AÇANI al. Kuşanılan KONİ'nin 3 epik/efsanevisi = dış halka, SÜRÜ hep orada.
            foreach (var a in üst)
                if (!a.SoldOut && (üstte == null || a.Yükseltme() is SilahYükseltme ay && Combo.Açtığı(ay) != null)) üstte = a;
            var üstY = üstte != null ? üstte.Yükseltme() : null;
            int artı0 = ArtıSayısı();
            yield return AltaraAl(altta);
            yield return AltaraAl(üstte);
            bool ikisi = SilahSunağı.AlınanSayısı == ö2 + 2;
            int artıFarkı = ArtıSayısı() - artı0;                            // dış halkanın 270°'si combo sunağının üstündeydi (2026-10-20)
            bool üstEnder = üstY.HasValue && SilahSunağı.Ender[(int)üstY.Value] >= Enderlik.Epik && SilahSunağı.Var(üstY.Value);
            bool hepsiKapalı = true;
            foreach (var a in alt) if (!a.SoldOut) hepsiKapalı = false;
            foreach (var a in üst) if (!a.SoldOut) hepsiKapalı = false;
            Kaydet("33-2", "Boss sunağı: 3 yaygın/nadir + 3 epik/efsanevi, her sıradan [E] ile 1 alım",
                   alt.Count == 3 && üst.Count == 3 && ikisi && üstEnder && hepsiKapalı && artıFarkı == 0,
                   $"{önceEtiket} · alınan {SilahSunağı.AlınanSayısı - ö2}" + (artıFarkı != 0 ? $" · ⚠️ aynı basışlarla {artıFarkı} combo \"+\"sı da alındı" : ""));

            var açılan = üstY.HasValue ? Combo.Açtığı(üstY.Value) : null;
            Sahne("P33 3/3 AÇILAN COMBO", açılan != null ? $"\"COMBO AÇILDI: {açılan.Ad}\" → gerçek tuşlarla dene" : "dış halkada açan yok");
            bool duyuruldu = açılan != null && Combo.SonCombo != null && Combo.SonCombo.Contains("COMBO AÇILDI: " + açılan.Ad);
            string duyuru = Combo.SonCombo;
            bool oldu = false; int deneme = 0;
            for (int i = 0; i < Combo.Sayı && açılan != null; i++)
                if (Combo.Bu((ComboTür)i) == açılan) yield return ComboDene((ComboTür)i, (o, d) => { oldu = o; deneme = d; });
            Kaydet("33-3", "Dış halkadan alınan açan combo'yu açıyor: \"COMBO AÇILDI\" yazıyor, combo gerçek tuşlarla oluyor",
                   duyuruldu && oldu,
                   açılan == null ? "dış halkada açan yok" : $"{açılan.Ad} · duyuru \"{duyuru}\" · {(oldu ? "oldu" : "OLMADI")} ({deneme} deneme)");
        }

        // ── FAZ 33 FİYAT ÖLÇÜMÜ — Enes (2026-10-18): "fiyat uygunluğunu bot üzerinden deneyelim" ──
        // Her yükseltme aynı iki dövüş odasında (1. bölge, oda 1–2) VAR ve YOK diye oynanır; saat her
        // odada doyma tavanından başlar. DEĞER = oda başı net saat farkı (saat = can = para: hasat,
        // darbe, akış hepsi içinde). GERİ ÖDEME = fiyat / değer, oda cinsinden. Rakip: aynı saatte bir
        // HASAT / HASAR / DASH sunağı alımı. Gürültü: yükseltmesiz deneme bloğun başında VE sonunda.
        static readonly int[] FiyatOdaları = { 1, 2 };

        /// <summary>F6 → silahı kuşan → yükseltme / sunak → her odada saat = tavan, odayı temizle.
        /// Sonuç: oda başı net saat ve süre; deneme olmadıysa NaN.</summary>
        IEnumerator FiyatDenemesi(int silah, SilahYükseltme? y, Altar.Kind? sunak, System.Action<float, float> sonuç)
        {
            Bas(Key.F6); yield return Bekle(0.6f);
            _pc = null; _kam = null;
            Kuşan(silah); yield return Bekle(0.3f);
            if (y.HasValue) SilahSunağı.Al(y.Value, -1);
            if (sunak.HasValue && Saat != null) Saat.Yükselt(sunak.Value);
            float net = 0f, süre = 0f;
            foreach (int oda in FiyatOdaları)
            {
                // Aynı zar her denemede (hızlandırılmış zamanda kare de sabit): A/B farkı yalnızca yükseltmeden gelsin.
                UnityEngine.Random.InitState(1009 * oda);
                yield return OdayaAtla(oda);
                var c = Saat;
                if (c == null || Oyuncu == null || Oyuncu.Weapon != Weapon.All[silah]) { sonuç(float.NaN, 0f); yield break; }
                c.Revive(_t.harvestSoftCap);
                float s0 = c.Seconds, g0 = c.TotalGranted, r0 = c.RunTime, t0 = Şimdi;
                while (Oda != null && !Oda.Cleared && !c.Dead && Şimdi - t0 < 240f)
                { DövüşAdımı(Mod.Dövüş); yield return null; }
                Dur();
                if (Oda == null || !Oda.Cleared || c.Dead) { sonuç(float.NaN, 0f); yield break; }
                net += c.Seconds - s0 - (c.TotalGranted - g0);
                süre += c.RunTime - r0;
            }
            sonuç(net / FiyatOdaları.Length, süre / FiyatOdaları.Length);
        }

        IEnumerator FiyatÖlçümü(params int[] silahlar)
        {
            _bekçiDk = 180f;
            _tahvilTut = true;
            float saat = _t.harvestSoftCap;
            float net = 0f, süre = 0f;
            System.Action<float, float> al = (n, s) => { net = n; süre = s; };
            string Geri(float değer, float fiyat, float gürültü) =>
                float.IsNaN(değer) ? "deneme olmadı" : değer <= gürültü ? "ölçülemedi (gürültü içinde)" : $"{fiyat / değer:0.0} oda";
            var enderlik = new Dictionary<Enderlik, List<(float değer, float fiyat)>>();
            float koniTaban = float.NaN, koniGürültü = 0f;

            foreach (int w in silahlar)
            {
                if (_bitti) yield break;
                var liste = SilahSunağı.Yükseltmeleri(w);
                string ad = Weapon.All[w].Name;
                Sahne($"FİYAT {ad}", "yükseltmesiz (1)");
                yield return FiyatDenemesi(w, null, null, al);
                float b1 = net, bs1 = süre;
                var ölçüm = new List<(SilahYükseltme y, float net, float süre)>();
                foreach (var y in liste)
                {
                    if (_bitti) yield break;
                    Sahne($"FİYAT {ad}", SilahSunağı.Ad[(int)y]);
                    yield return FiyatDenemesi(w, y, null, al);
                    ölçüm.Add((y, net, süre));
                    Debug.Log($"[FİYAT] {ad} · {SilahSunağı.Ad[(int)y]} · net {net:+0.0;−0.0} sn/oda · {süre:0} sn");
                }
                Sahne($"FİYAT {ad}", "yükseltmesiz (2)");
                yield return FiyatDenemesi(w, null, null, al);
                float taban = (b1 + net) / 2f, gürültü = Mathf.Abs(b1 - net) / 2f;
                Debug.Log($"[FİYAT] {ad} · yükseltmesiz {b1:+0.0;−0.0} / {net:+0.0;−0.0} sn/oda · {bs1:0} / {süre:0} sn");
                if (w == Kadro.KONİ) { koniTaban = taban; koniGürültü = gürültü; }

                var sb = new StringBuilder($"yükseltmesiz {taban:+0.0;−0.0} sn/oda (±{gürültü:0.0}), {(bs1 + süre) / 2f:0} sn");
                foreach (var (y, n, s) in ölçüm)
                {
                    var e = SilahSunağı.Ender[(int)y];
                    float fiyat = Clock.SilahFiyatı(_t, saat, e), değer = n - taban;
                    if (!float.IsNaN(değer))
                    {
                        if (!enderlik.TryGetValue(e, out var l)) enderlik[e] = l = new List<(float, float)>();
                        l.Add((değer, fiyat));
                    }
                    sb.Append($"<br>[{SilahSunağı.EnderlikAdı[(int)e]}] {SilahSunağı.Ad[(int)y]}: {değer:+0.0;−0.0} sn/oda · {s:0} sn · " +
                              $"fiyat {fiyat:0} → geri ödeme {Geri(değer, fiyat, gürültü)}");
                }
                Kaydet("F", $"{ad} (saat {saat:0})", null, sb.ToString());
            }

            // Rakip: aynı odalarda KONİ + ilk sunak alımı
            if (!float.IsNaN(koniTaban) && !_bitti)
            {
                var sb = new StringBuilder();
                foreach (var k in new[] { Altar.Kind.Harvest, Altar.Kind.Damage, Altar.Kind.Dash })
                {
                    Sahne("FİYAT rakip", k.ToString());
                    yield return FiyatDenemesi(Kadro.KONİ, null, k, al);
                    float fiyat = Clock.Fiyat(_t, 0, saat), değer = net - koniTaban;
                    Debug.Log($"[FİYAT] rakip {k} · net {net:+0.0;−0.0} sn/oda · {süre:0} sn");
                    sb.Append($"{k}: {değer:+0.0;−0.0} sn/oda · fiyat {fiyat:0} → geri ödeme {Geri(değer, fiyat, koniGürültü)}<br>");
                }
                Kaydet("F", "Rakip: ilk HASAT / HASAR / DASH sunağı (KONİ'yle)", null, sb.ToString());
            }

            var özet = new StringBuilder();
            foreach (Enderlik e in System.Enum.GetValues(typeof(Enderlik)))
            {
                if (!enderlik.TryGetValue(e, out var l) || l.Count == 0) continue;
                float d = 0f, f = 0f;
                foreach (var (dd, ff) in l) { d += dd; f += ff; }
                d /= l.Count; f /= l.Count;
                özet.Append($"{SilahSunağı.EnderlikAdı[(int)e]} ({l.Count}): ortalama {d:+0.0;−0.0} sn/oda · fiyat {f:0} → " +
                            $"{(d > 0f ? $"{f / d:0.0} oda" : "ödemiyor")}<br>");
            }
            Kaydet("F", "Enderlik başına (ortalama değer, fiyat, geri ödeme)", null, özet.ToString());
        }

        /// <summary>
        /// Bir bölgenin özgün mini boss'ları M ile, sırayla (Faz 34–35'in ortak yolu): beklenen güç (önceki her sunaktan türünden
        /// bir alım) ve saat o bölgenin doyma tavanında. Ölçülen: adı ekranda mı, öldü mü, süre, saat (bota kazandırmamalı — Enes
        /// 2026-10-21: "zorlasınlar"), darbe, saldırı sayımı ve `Adam`'daki HER imzanın dövüşte görülüp görülmediği. İsabet isteyen
        /// imzalar (zorla alır, kelepçe, defter, yüzde bedel) yalnızca bot vurulduysa görülebilir; vurulmadıysa sayılmaz.
        /// </summary>
        /// <summary>
        /// BEKLENEN GÜÇ: N ile atlayan bot gerçek koşudaki sunak alımlarını yapmamış olur — her sunaktan türünden `alım` kez yükseltir
        /// (mini testi ve oda testleri ortak). ⚠️ Faz 40: odalar bölgeyle büyüyünce yükseltmesiz bot 5. bölgede odaları 300 sn'de
        /// bitiremedi, MÜHLET'lerin hepsi kaçtı (P38) — test oyunu değil atlamayı ölçüyordu. Gerçek koşuda o odalar ~100 sn.
        /// </summary>
        static void BeklenenGüç(Clock c, int alım)
        {
            if (c == null) return;
            foreach (Altar.Kind k in new[] { Altar.Kind.Harvest, Altar.Kind.Damage, Altar.Kind.Dash })
                while (c.Alınan(k) < alım) c.Yükselt(k);
        }

        IEnumerator AdamlarıDöv(int bölge, string etiket, string kayıtNo, string kayıtAdı)
        {
            int S = Mathf.Max(1, _t.subRegionsPerRegion);
            var satırlar = new List<string>();
            bool tamam = true;
            for (int s = 0; s < S && !_bitti; s++)
            {
                var tanım = bölge < MiniBossKind.Adamlar.Length && s < MiniBossKind.Adamlar[bölge].Length ? MiniBossKind.Adamlar[bölge][s] : null;
                Sahne($"{etiket} ADAM {s + 1}/{S}", tanım != null ? $"{tanım.Ad} — {tanım.Suç}" : "?");
                Bas(Key.M); yield return Bekle(1.5f);
                var c = Saat; var boss = Boss();
                if (c == null || Oda == null || !Oda.IsMiniBossRoom || Oda.RegionIndex != bölge || boss == null || tanım == null)
                { satırlar.Add($"{s + 1}: {bölge + 1}. bölgenin mini boss odası açılmadı"); tamam = false; continue; }
                BeklenenGüç(c, bölge * S + s);
                float giriş = Clock.Tavan(_t, bölge);
                c.Revive(giriş);
                float s0 = c.Seconds, b0 = c.TotalGranted, t0 = Şimdi, d0 = c.TotalHitCost, sonDarbe = c.TotalHitCost, son = boss.TimeLeft;
                float aura = 0f, hasatEnAz = 1f, değerKaybı = 0f, enBüyükPay = 0f;
                float dash0 = Oyuncu != null ? Oyuncu.ÖdenenDashBedeli : 0f;
                int dolma = 0;
                bool ekranda = Oda.Prompt().Contains(tanım.Ad), öfke = false, kelepçe = false, kilit = false, harita = false, kefalet = false, yakınAura = false, duruyor = false, vadeKapalı = false, muaccel = false;
                float bileşik = 1f;
                int takviye0 = _takviye;
                var işaretler = new HashSet<GroundMark>();
                _dal.TryGetValue("dolan", out float dolan0);
                var dal0 = new Dictionary<string, float>(_dal);                   // teşhis: dövüş süresi hangi dala gitti
                var saldırılar = new Dictionary<string, int>();
                _yeniSaldırı = e =>                                              // her saldırı başlangıcı — aynısının tekrarı da
                {
                    if (e == null || !e.IsBoss || e.CurrentAttack == null) return;
                    saldırılar[e.CurrentAttack.Name] = (saldırılar.TryGetValue(e.CurrentAttack.Name, out var sn) ? sn : 0) + 1;
                };
                while (Şimdi - t0 < 900f && !_bitti && Saat == c && !c.Dead && Oda != null && Oda.IsMiniBossRoom && !Oda.Cleared)
                {
                    var bb = Boss();
                    if (bb != null)
                    {
                        if (bb.TimeLeft > son + 0.01f) dolma++;
                        son = bb.TimeLeft;
                        aura = Mathf.Max(aura, bb.AuraOranı);
                        öfke |= bb.Öfkeli;
                        değerKaybı = Mathf.Max(değerKaybı, bb.DeğerKaybı);
                        kefalet |= bb.Kefalette;
                        yakınAura |= bb.YakınAurada;
                        duruyor |= bb.Duruyor;
                        vadeKapalı |= !bb.VadeAçık;
                        muaccel |= bb.Muaccel;
                        bileşik = Mathf.Max(bileşik, bb.BileşikÇarpanı);
                        if (tanım.İşaret > 0f) foreach (var g in FindObjectsByType<GroundMark>(FindObjectsSortMode.None)) işaretler.Add(g);
                        harita |= bb.CurrentAttack != null && bb.CurrentAttack.Tür == SaldırıTürü.Harita;
                    }
                    hasatEnAz = Mathf.Min(hasatEnAz, BossKind.ActiveHarvestMultiplier());
                    if (Oyuncu != null) kelepçe |= Oyuncu.Kelepçeli;
                    kilit |= c.HasatKilitli;
                    float inen = c.TotalHitCost - sonDarbe;                          // bu karede inen darbe, saatin kaçta kaçı
                    if (inen > 0f) enBüyükPay = Mathf.Max(enBüyükPay, inen / Mathf.Max(1f, c.Seconds + inen));
                    sonDarbe = c.TotalHitCost;
                    if (c.Seconds < giriş * 0.4f) { c.Revive(giriş); _takviye++; }
                    DövüşAdımı(Mod.Dövüş, bb);
                    yield return null;
                }
                Dur();
                _yeniSaldırı = null;
                float darbe = c.TotalHitCost - d0, kayıp = s0 + (c.TotalGranted - b0) - c.Seconds;
                float dashBedeli = (Oyuncu != null ? Oyuncu.ÖdenenDashBedeli : 0f) - dash0;
                // ⚠️ C# nesnesinden: `boss != null` Unity'nin eşitliği — yok edilmiş boss'u null sayıp 0 okuyordu (BAŞ SİMSAR "SIÇRAMADI", P37)
                int yutulan = boss is null ? 0 : boss.TeminatYuttu;
                int sıçradı = boss is null ? 0 : boss.Sıçradı;
                float faizleDolan = boss is null ? 0f : boss.FaizleDolan;
                int takviye = _takviye - takviye0;
                _dal.TryGetValue("dolan", out float dolan1);
                float süre = Şimdi - t0;
                bool öldü = Oda != null && Oda.IsMiniBossRoom && Oda.Cleared && Saat == c, vuruldu = darbe > 0f;
                var imza = new List<string>(); bool ok = true;
                void İz(bool koşul, string evet, string hayır) { ok &= koşul; imza.Add(koşul ? evet : hayır); }
                if (tanım.Aura > 0f) İz(aura > 0f, $"aura {aura:0.00}/sn", "AURA YOK");
                if (tanım.Hasat < 1f) İz(hasatEnAz <= tanım.Hasat + 0.001f, $"hasat ×{hasatEnAz:0.00}", $"HASAT KESİLMEDİ (×{hasatEnAz:0.00})");
                if (tanım.Öfke != 1f) İz(öfke, "öfkelendi", "ÖFKELENMEDİ");
                if (tanım.Amortisman > 0f) İz(değerKaybı > 0f, $"değer kaybı %{değerKaybı * 100f:0}", "DEĞER KAYBI YOK");
                if (tanım.ZorlaAlır > 0f) İz(!vuruldu || dolma > 0, vuruldu ? $"havuz {dolma} kez doldu" : "(zorla alır) bot hiç vurulmadı", "HAVUZ DOLMADI");
                if (tanım.Kelepçe > 0f) İz(!vuruldu || kelepçe, vuruldu ? "kelepçelendi" : "(kelepçe) bot hiç vurulmadı", "KELEPÇELENMEDİ");
                if (tanım.HasatKilidi > 0f) İz(!vuruldu || kilit, vuruldu ? "deftere yazıldı" : "(defter) bot hiç vurulmadı", "DEFTERE YAZILMADI");
                if (tanım.Kefalet < 1f) İz(kefalet, "kefalete girdi", "KEFALETE GİRMEDİ");
                if (tanım.TeminatSayısı > 0) İz(yutulan > 0, $"teminat {yutulan} vuruş yuttu", "TEMİNAT YUTMADI");
                if (tanım.YakınAura != 1f) İz(yakınAura, "dibinde iki kat emdi", "DİBİNDE İKİ KAT EMMEDİ");
                if (tanım.DashBedeli > 0f) İz(dashBedeli > 0f, $"dash bedeli −{dashBedeli:0.0} sn", "DASH BEDELİ YOK");
                if (tanım.İşaret > 0f) İz(işaretler.Count >= süre / (tanım.İşaret * 1.3f), $"{işaretler.Count} işaret ({süre / Mathf.Max(1, işaretler.Count):0.0} sn'de bir)", $"İŞARET SEYREK ({işaretler.Count})");
                if (tanım.ÖndenHasar < 1f) İz(dolan1 > dolan0, $"arkasına {dolan1 - dolan0:0} sn dolandı", "HİÇ DOLANMADI");
                if (tanım.DuranAura != 1f) İz(duruyor, "yerinde durana iki kat emdi", "DURANA İKİ KAT EMMEDİ");
                if (tanım.Sıçrayış > 0f) İz(sıçradı > 0, $"{sıçradı} kez geri sıçradı", "SIÇRAMADI");
                if (tanım.Faiz > 0f) İz(faizleDolan > 0f, $"faizle +{faizleDolan:0} sn doldu", "FAİZ İŞLEMEDİ");
                if (tanım.VadeAralığı > 0f) İz(vadeKapalı, "vadesi kapandı", "VADE KAPANMADI");
                if (tanım.BileşikFaiz > 0f) İz(bileşik > 1.1f, $"aurası ×{bileşik:0.00}'e bindi", "FAİZ BİNMEDİ");
                if (tanım.MuaccelAura != 1f) İz(muaccel, "muaccel oldu", "MUACCEL OLMADI");
                if (tanım.YüzdeBedel > 0f) İz(!vuruldu || enBüyükPay >= tanım.YüzdeBedel * 0.9f, vuruldu ? $"en büyük darbe saatin %{enBüyükPay * 100f:0.0}'i" : "(yüzde bedel) bot hiç vurulmadı", $"DARBE %{enBüyükPay * 100f:0.0}");
                if (s == S - 1) İz(harita, "harita saldırısı geldi", "HARİTA SALDIRISI GELMEDİ");
                İz(kayıp > 0f, $"saat {-kayıp:+0;−0}", $"⚠️ SAAT KAZANDIRDI ({-kayıp:+0;−0})");   // Enes "zorlasınlar": bedava geçilmesin
                // Enes 2026-10-22 "botun zorlanma ölçüsüne göre": zorlasın ama öldürmesin — kayıp giriş saatini geçmesin
                İz(kayıp < giriş, $"öldürmedi (giriş {giriş:0}{(takviye > 0 ? $", {takviye} takviye" : "")})", $"⚠️ BOTU ÖLDÜRÜRDÜ (−{kayıp:0} > giriş {giriş:0})");
                var sYazı = new List<string>();
                foreach (var (ad, kaç) in saldırılar) sYazı.Add($"{ad} {kaç}");
                if (!öldü || !ekranda || !ok) tamam = false;
                satırlar.Add($"{(öldü && ekranda && ok ? "✅" : "❌")} {tanım.Ad}: {(Şimdi - t0) / 60f:0.0} dk · {string.Join(" · ", imza)} · darbe −{darbe:0} · saldırılar: {string.Join(", ", sYazı)}" +
                             (ekranda ? "" : " · ⚠️ ADI EKRANDA DEĞİL") + (öldü ? "" : " · ❌ ÖLMEDİ"));
                Debug.Log($"[{etiket}] {satırlar[satırlar.Count - 1]}");
                var dallar = new List<(string, float)>();
                foreach (var (ad, sn) in _dal) dallar.Add((ad, sn - (dal0.TryGetValue(ad, out var önce) ? önce : 0f)));
                dallar.Sort((a, b) => b.Item2.CompareTo(a.Item2));
                Debug.Log($"[{etiket}-DAL] {tanım.Ad}: " + string.Join(" · ", dallar.FindAll(x => x.Item2 >= 1f).ConvertAll(x => $"{x.Item1} {x.Item2:0} sn")));
            }
            Kaydet(kayıtNo, kayıtAdı, tamam, string.Join("<br>", satırlar));
        }

        // ── FAZ 38 PLAYTEST — 5. bölge içeriği (Enes 2026-10-22: "commit at ve sonraki faza geç") ──
        /// <summary>
        /// MURABAHACI'nın 5 adamı (N×4, sonra M — `AdamlarıDöv`), sonra F6 → N×4 → 5. bölgenin dövüş odaları: MÜHLET ve REFİNANS
        /// doğuyor mu, MÜHLET'lerin kaçı süresinde kesildi, REFİNANS öbürlerini yeniledi mi, SARMAL ve ZİKZAK geliyor mu, odalar temizleniyor mu.
        /// </summary>
        IEnumerator P38()
        {
            _bekçiDk = 30f;
            for (int i = 0; i < 4; i++) { Bas(Key.N); yield return Bekle(1.2f); }
            yield return AdamlarıDöv(4, "P38", "38-1", "MURABAHACI'nın 5 adamı: adı ekranda, imzası dövüşte, öldü, bota saat kazandırmadı ama öldürmedi (beklenen güç)");
            if (_bitti) yield break;

            Sahne("P38 2/2 YENİ DÜŞMANLAR", "F6 → N×4 → 5. bölgenin 2–7. ve 10–13. odası: MÜHLET · REFİNANS · SARMAL · ZİKZAK");
            Bas(Key.F6); yield return Bekle(0.6f);
            _pc = null; _kam = null;
            for (int i = 0; i < 4; i++) { Bas(Key.N); yield return Bekle(1.2f); }
            BeklenenGüç(Saat, 4 * Mathf.Max(1, _t.subRegionsPerRegion));                                     // gerçek koşudaki gibi güçlü gir
            var türler = new Dictionary<string, int>();
            var dizilişler = new HashSet<string>();
            var görülen = new HashSet<Enemy>();
            var mühletler = new HashSet<Enemy>();
            var refinanslar = new HashSet<Enemy>();
            var odalar = new List<string>();
            bool temiz = true;
            foreach (int oda in new[] { 2, 3, 4, 5, 6, 7, 10, 11, 12, 13 })        // 8. oda mini boss, 9. sunak · SARMAL 12., ZİKZAK 13.
            {
                if (_bitti) break;
                yield return OdayaAtla(oda);
                if (Oda == null || RoomManager.Tür(_t, Oda.Room) != RoomManager.OdaTürü.Dövüş) { odalar.Add($"{oda}. oda DÖVÜŞ ODASI DEĞİL"); temiz = false; continue; }
                string diziliş = Oda.Layout.Name;
                dizilişler.Add(diziliş);
                float t0 = Şimdi;
                while (Oda != null && Oda.Room == oda && !Oda.Cleared && Şimdi - t0 < 300f && !_bitti)
                {
                    foreach (var e in Enemy.All)
                    {
                        if (e == null || e.Kind == null) continue;
                        if (görülen.Add(e)) türler[e.Kind.Name] = (türler.TryGetValue(e.Kind.Name, out var n) ? n : 0) + 1;
                        if (e.Kind == EnemyKind.Grace) mühletler.Add(e);
                        if (e.Kind == EnemyKind.Refinance) refinanslar.Add(e);
                    }
                    if (Saat != null && Saat.Seconds < 60f && Time.time - _sonTakviye > 2f) { Bas(Key.F5); _sonTakviye = Time.time; _takviye++; }
                    DövüşAdımı(Mod.Dövüş);
                    yield return null;
                }
                Dur();
                bool bitti = Oda != null && (Oda.Room != oda || Oda.Cleared);
                temiz &= bitti;
                odalar.Add($"{oda}. oda {diziliş} {Şimdi - t0:0} sn" + (bitti ? "" : " ⚠️ TEMİZLENMEDİ"));
            }
            int kaçan = 0; float yenilenen = 0f, enGeç = 0f;
            foreach (var e in mühletler) { if (e.MühletDoldu) kaçan++; else enGeç = Mathf.Max(enGeç, e.Yaş); }   // C# alanı: yok edilmiş olsa da okunur
            foreach (var e in refinanslar) yenilenen += e.Yeniledi;
            var türYazı = new List<string>();
            foreach (var (ad, kaç) in türler) türYazı.Add($"{ad} {kaç}");
            Kaydet("38-2", "5. bölgede MÜHLET ve REFİNANS doğuyor, MÜHLET süresi dolunca gidiyor, REFİNANS öbürlerini yeniliyor, SARMAL ve ZİKZAK geliyor, odalar temizleniyor",
                   mühletler.Count > 0 && refinanslar.Count > 0 && yenilenen > 0f && temiz
                   && dizilişler.Contains(RoomLayout.Spiral.Name) && dizilişler.Contains(RoomLayout.Zigzag.Name),
                   $"{string.Join(" · ", türYazı)} · MÜHLET {mühletler.Count}: {mühletler.Count - kaçan} süresinde kesildi (en geç {enGeç:0} sn'de), {kaçan} kaçtı · REFİNANS {refinanslar.Count}: öbürlerine +{yenilenen:0} sn yeniledi" +
                   $"<br>{string.Join(" · ", odalar)}");
        }

        // ── FAZ 37 PLAYTEST — 4. bölge içeriği (Enes 2026-10-21: "commitle ve sıradaki faza geç") ──
        /// <summary>
        /// SİMSAR'ın 5 adamı (N N N, sonra M — `AdamlarıDöv`), sonra F6 → N N N → 4. bölgenin dövüş odaları: BLOKE ve CEZAİ ŞART
        /// doğuyor mu, BLOKE botu yavaşlatıyor mu, CEZAİ ŞART zinciri uzuyor mu (bot onun yanında vurulduysa), DÖRT KÖŞE ve ÇAPRAZ
        /// geliyor mu, odalar temizleniyor mu.
        /// </summary>
        IEnumerator P37()
        {
            _bekçiDk = 30f;
            for (int i = 0; i < 3; i++) { Bas(Key.N); yield return Bekle(1.2f); }
            yield return AdamlarıDöv(3, "P37", "37-1", "SİMSAR'ın 5 adamı: adı ekranda, imzası dövüşte, öldü, bota saat kazandırmadı (beklenen güç)");
            if (_bitti) yield break;

            Sahne("P37 2/2 YENİ DÜŞMANLAR", "F6 → N N N → 4. bölgenin 2–7. ve 10–12. odası: BLOKE · CEZAİ ŞART · DÖRT KÖŞE · ÇAPRAZ");
            Bas(Key.F6); yield return Bekle(0.6f);
            _pc = null; _kam = null;
            for (int i = 0; i < 3; i++) { Bas(Key.N); yield return Bekle(1.2f); }
            BeklenenGüç(Saat, 3 * Mathf.Max(1, _t.subRegionsPerRegion));                                     // gerçek koşudaki gibi güçlü gir
            var türler = new Dictionary<string, int>();
            var dizilişler = new HashSet<string>();
            var görülen = new HashSet<Enemy>();
            var odalar = new List<string>();
            bool temiz = true;
            float blokeAnı = 0f, enAğır = 1f, enUzunZincir = 1f, şartlıyDarbe = 0f;
            foreach (int oda in new[] { 2, 3, 4, 5, 6, 7, 10, 11, 12 })            // 8. oda mini boss, 9. sunak · ÇAPRAZ 12.'de
            {
                if (_bitti) break;
                yield return OdayaAtla(oda);
                if (Oda == null || RoomManager.Tür(_t, Oda.Room) != RoomManager.OdaTürü.Dövüş) { odalar.Add($"{oda}. oda DÖVÜŞ ODASI DEĞİL"); temiz = false; continue; }
                string diziliş = Oda.Layout.Name;
                dizilişler.Add(diziliş);
                float t0 = Şimdi, sonDarbe = Saat != null ? Saat.TotalHitCost : 0f;
                while (Oda != null && Oda.Room == oda && !Oda.Cleared && Şimdi - t0 < 300f && !_bitti)
                {
                    bool şartVar = false;
                    foreach (var e in Enemy.All)
                    {
                        if (e == null || e.Kind == null) continue;
                        if (görülen.Add(e)) türler[e.Kind.Name] = (türler.TryGetValue(e.Kind.Name, out var n) ? n : 0) + 1;
                        şartVar |= e.Kind == EnemyKind.Penalty && !e.Hollow;
                    }
                    float ağır = Enemy.BlokeÇarpanı(Ben);
                    if (ağır < 1f) { blokeAnı += Time.deltaTime; enAğır = Mathf.Min(enAğır, ağır); }
                    enUzunZincir = Mathf.Max(enUzunZincir, Enemy.CezaÇarpanı());
                    if (Saat != null)
                    {
                        if (şartVar) şartlıyDarbe += Saat.TotalHitCost - sonDarbe;     // CEZAİ ŞART yaşarken inen darbe
                        sonDarbe = Saat.TotalHitCost;
                    }
                    if (Saat != null && Saat.Seconds < 60f && Time.time - _sonTakviye > 2f) { Bas(Key.F5); _sonTakviye = Time.time; _takviye++; }
                    DövüşAdımı(Mod.Dövüş);
                    yield return null;
                }
                Dur();
                bool bitti = Oda != null && (Oda.Room != oda || Oda.Cleared);
                temiz &= bitti;
                odalar.Add($"{oda}. oda {diziliş} {Şimdi - t0:0} sn" + (bitti ? "" : " ⚠️ TEMİZLENMEDİ"));
            }
            türler.TryGetValue(EnemyKind.Frozen.Name, out int bloke);
            türler.TryGetValue(EnemyKind.Penalty.Name, out int şart);
            var türYazı = new List<string>();
            foreach (var (ad, kaç) in türler) türYazı.Add($"{ad} {kaç}");
            bool zincir = şartlıyDarbe <= 0f || enUzunZincir > 1.01f;                 // vurulmadıysa zincir görülemez
            Kaydet("37-2", "4. bölgede BLOKE ve CEZAİ ŞART doğuyor, BLOKE botu yavaşlatıyor, CEZAİ ŞART zinciri uzuyor, DÖRT KÖŞE ve ÇAPRAZ geliyor, odalar temizleniyor",
                   bloke > 0 && şart > 0 && blokeAnı > 0f && enAğır < 0.6f && zincir && temiz
                   && dizilişler.Contains(RoomLayout.Corners.Name) && dizilişler.Contains(RoomLayout.Cross.Name),
                   $"{string.Join(" · ", türYazı)} · BLOKE'nin yanında {blokeAnı:0} sn (yürüyüş ×{enAğır:0.00}) · CEZAİ ŞART yaşarken darbe −{şartlıyDarbe:0}, en uzun zincir ×{enUzunZincir:0.00}" +
                   $"<br>{string.Join(" · ", odalar)}");
        }

        // ── FAZ 36 PLAYTEST — 3. bölge içeriği (Enes 2026-10-21: "commitle ve sıradaki faza geç") ──
        /// <summary>
        /// KEFİL'in 5 adamı (N N, sonra M — `AdamlarıDöv`), sonra F6 → N N → 3. bölgenin dövüş odaları: SÜLÜK ve MEVDUAT doğuyor
        /// mu, SÜLÜK yapışıp bot dash'le silkeleniyor mu, MEVDUAT olgunlaşıyor mu, İÇ İÇE ve SÜTUN geliyor mu, odalar temizleniyor mu.
        /// </summary>
        IEnumerator P36()
        {
            _bekçiDk = 30f;
            Bas(Key.N); yield return Bekle(1.2f);
            Bas(Key.N); yield return Bekle(1.2f);
            yield return AdamlarıDöv(2, "P36", "36-1", "KEFİL'in 5 adamı: adı ekranda, imzası dövüşte, öldü, bota saat kazandırmadı (beklenen güç)");
            if (_bitti) yield break;

            Sahne("P36 2/2 YENİ DÜŞMANLAR", "F6 → N N → 3. bölgenin 2–7., 10. ve 11. odası: SÜLÜK · MEVDUAT · İÇ İÇE · SÜTUN");
            Bas(Key.F6); yield return Bekle(0.6f);
            _pc = null; _kam = null;
            Bas(Key.N); yield return Bekle(1.2f);
            Bas(Key.N); yield return Bekle(1.2f);
            BeklenenGüç(Saat, 2 * Mathf.Max(1, _t.subRegionsPerRegion));                                     // gerçek koşudaki gibi güçlü gir
            var türler = new Dictionary<string, int>();
            var dizilişler = new HashSet<string>();
            var görülen = new HashSet<Enemy>();
            var odalar = new List<string>();
            bool temiz = true;
            int yapıştı = 0, silkelendi = 0;
            float olgunluk = 1f;
            foreach (int oda in new[] { 2, 3, 4, 5, 6, 7, 10, 11 })                // 8. oda mini boss, 9. sunak
            {
                if (_bitti) break;
                yield return OdayaAtla(oda);
                if (Oda == null || RoomManager.Tür(_t, Oda.Room) != RoomManager.OdaTürü.Dövüş) { odalar.Add($"{oda}. oda DÖVÜŞ ODASI DEĞİL"); temiz = false; continue; }
                string diziliş = Oda.Layout.Name;
                dizilişler.Add(diziliş);
                var sülükler = new HashSet<Enemy>();
                float t0 = Şimdi;
                while (Oda != null && Oda.Room == oda && !Oda.Cleared && Şimdi - t0 < 300f && !_bitti)
                {
                    foreach (var e in Enemy.All)
                    {
                        if (e == null || e.Kind == null) continue;
                        if (görülen.Add(e)) türler[e.Kind.Name] = (türler.TryGetValue(e.Kind.Name, out var n) ? n : 0) + 1;
                        if (e.Kind == EnemyKind.Leech) sülükler.Add(e);
                        if (e.Kind == EnemyKind.Deposit) olgunluk = Mathf.Max(olgunluk, e.Kind.Faiz(e.Yaş));
                    }
                    if (Saat != null && Saat.Seconds < 60f && Time.time - _sonTakviye > 2f) { Bas(Key.F5); _sonTakviye = Time.time; _takviye++; }
                    DövüşAdımı(Mod.Dövüş);
                    yield return null;
                }
                Dur();
                foreach (var e in sülükler) { yapıştı += e.Yapıştı; silkelendi += e.Silkelendi; }   // ölü sülüğün C# nesnesinden
                bool bitti = Oda != null && (Oda.Room != oda || Oda.Cleared);
                temiz &= bitti;
                odalar.Add($"{oda}. oda {diziliş} {Şimdi - t0:0} sn" + (bitti ? "" : " ⚠️ TEMİZLENMEDİ"));
            }
            türler.TryGetValue(EnemyKind.Leech.Name, out int sülük);
            türler.TryGetValue(EnemyKind.Deposit.Name, out int mevduat);
            var türYazı = new List<string>();
            foreach (var (ad, kaç) in türler) türYazı.Add($"{ad} {kaç}");
            Kaydet("36-2", "3. bölgede SÜLÜK ve MEVDUAT doğuyor, SÜLÜK yapışıyor ve bot dash'le silkeleniyor, MEVDUAT olgunlaşıyor, İÇ İÇE ve SÜTUN geliyor, odalar temizleniyor",
                   sülük > 0 && mevduat > 0 && yapıştı > 0 && silkelendi > 0 && olgunluk > 1.3f && temiz
                   && dizilişler.Contains(RoomLayout.Nested.Name) && dizilişler.Contains(RoomLayout.Column.Name),
                   $"{string.Join(" · ", türYazı)} · SÜLÜK {yapıştı} kez yapıştı, {silkelendi} kez silkelendi · MEVDUAT en olgun ×{olgunluk:0.00}<br>{string.Join(" · ", odalar)}");
        }

        // ── FAZ 35 PLAYTEST — 2. bölge içeriği (Enes 2026-10-21: "sıradaki faza geç") ──
        /// <summary>
        /// EKSPER'in 5 adamı (N, sonra M), beklenen güçle ve saat 2. bölgenin doyma tavanında: adı ekranda, imzası dövüşte,
        /// öldü mü, süre, saat kaybı, saldırı sayımı. Sonra F6 → N → 2. bölgenin 2–8. odası: VADELİ ve TAKSİT doğuyor mu,
        /// TAKSİT ölünce bölünüyor mu, KORİDOR ve OK geliyor mu, odalar temizleniyor mu.
        /// </summary>
        IEnumerator P35()
        {
            _bekçiDk = 30f;
            Bas(Key.N); yield return Bekle(1.2f);
            yield return AdamlarıDöv(1, "P35", "35-1", "EKSPER'in 5 adamı: adı ekranda, imzası dövüşte, öldü, bota saat kazandırmadı (beklenen güç)");
            if (_bitti) yield break;

            Sahne("P35 2/2 YENİ DÜŞMANLAR", "F6 → N → 2. bölgenin 2–7. ve 10. odası: VADELİ · TAKSİT · KORİDOR · OK");
            Bas(Key.F6); yield return Bekle(0.6f);
            _pc = null; _kam = null;
            Bas(Key.N); yield return Bekle(1.2f);
            BeklenenGüç(Saat, 1 * Mathf.Max(1, _t.subRegionsPerRegion));                                     // gerçek koşudaki gibi güçlü gir
            var türler = new Dictionary<string, int>();
            var dizilişler = new HashSet<string>();
            var görülen = new HashSet<Enemy>();
            var odalar = new List<string>();
            bool temiz = true;
            // ⚠️ 8. oda MİNİ BOSS odası (ilk koşu onu "OK odası, 167 sn" diye saydı); OK dövüşte ilk 10. odada
            foreach (int oda in new[] { 2, 3, 4, 5, 6, 7, 10 })
            {
                if (_bitti) break;
                yield return OdayaAtla(oda);
                if (Oda == null || RoomManager.Tür(_t, Oda.Room) != RoomManager.OdaTürü.Dövüş) { odalar.Add($"{oda}. oda DÖVÜŞ ODASI DEĞİL"); temiz = false; continue; }
                string diziliş = Oda.Layout.Name;
                dizilişler.Add(diziliş);
                float t0 = Şimdi, vadeBekleme = 0f;
                while (Oda != null && Oda.Room == oda && !Oda.Cleared && Şimdi - t0 < 300f && !_bitti)
                {
                    bool hepsiKapalı = Enemy.All.Count > 0;
                    foreach (var e in Enemy.All)
                    {
                        if (e == null) continue;
                        if (e.Kind != null && görülen.Add(e))
                            türler[e.Kind.Name] = (türler.TryGetValue(e.Kind.Name, out var n) ? n : 0) + 1;
                        if (e.VadeAçık) hepsiKapalı = false;
                    }
                    if (hepsiKapalı) vadeBekleme += Time.deltaTime;                 // kalanların hepsi kapalı VADELİ
                    if (Saat != null && Saat.Seconds < 60f && Time.time - _sonTakviye > 2f) { Bas(Key.F5); _sonTakviye = Time.time; _takviye++; }
                    DövüşAdımı(Mod.Dövüş);
                    yield return null;
                }
                Dur();
                bool bitti = Oda != null && (Oda.Room != oda || Oda.Cleared);
                temiz &= bitti;
                odalar.Add($"{oda}. oda {diziliş} {Şimdi - t0:0} sn" + (vadeBekleme > 1f ? $" (yalnız kapalı VADELİ kaldı {vadeBekleme:0} sn)" : "") + (bitti ? "" : " ⚠️ TEMİZLENMEDİ"));
            }
            türler.TryGetValue(EnemyKind.Term.Name, out int vadeli);
            türler.TryGetValue(EnemyKind.Installment.Name, out int taksit);
            türler.TryGetValue(EnemyKind.InstallmentPart.Name, out int parça);
            var türYazı = new List<string>();
            foreach (var (ad, kaç) in türler) türYazı.Add($"{ad} {kaç}");
            Kaydet("35-2", "2. bölgede VADELİ ve TAKSİT doğuyor, her TAKSİT ölünce iki parçaya bölünüyor, KORİDOR ve OK geliyor, odalar temizleniyor",
                   vadeli > 0 && taksit > 0 && parça == 2 * taksit && temiz
                   && dizilişler.Contains(RoomLayout.Corridor.Name) && dizilişler.Contains(RoomLayout.Arrow.Name),
                   $"{string.Join(" · ", türYazı)}<br>{string.Join(" · ", odalar)}");
        }

        // ── MASA #14 — KAPI TESTİ (Enes 2026-10-21: "14'ün testini sen yap") ──
        /// <summary>
        /// Gerçek oyunda: son düşmanı KAPININ ÜSTÜNDE durarak, gerçek bir vuruşla öldür → kapı açılır ama geçirmez.
        /// Diskten çık, yeniden bas → geçer (bu, dışarıdan yürüyüp girmenin de kendisi).
        /// </summary>
        IEnumerator KapıTesti()
        {
            _bekçiDk = 10f;
            Sahne("KAPI 1/2", "son düşman kapının üstünde ölüyor → kapı açılmalı ama geçirmemeli");
            yield return OdayaAtla(2);
            var oda = Oda; var kapı = FindFirstObjectByType<ExitGate>();
            if (oda == null || kapı == null) { Kaydet("K-1", "kapı testi kurulamadı", false, "oda ya da kapı yok"); yield break; }
            int no = oda.Room;
            int Canlı() { int n = 0; foreach (var e in Enemy.All) if (e != null && !e.Dormant) n++; return n; }
            float t0 = Şimdi;
            while (Şimdi - t0 < 60f && !(oda.RemainingWaves == 0 && Canlı() == 1))   // dalgaları birer birer düşür, biri kalsın
            {
                foreach (var e in Enemy.All)
                    if (e != null && !e.Dormant) { e.TakeDamage(e.TimeLeft); e.TakeDamage(1f); break; }
                yield return null;
            }
            Enemy son = null;
            foreach (var e in Enemy.All) if (e != null && !e.Dormant) son = e;
            if (son == null) { Kaydet("K-1", "kapı testi kurulamadı", false, "son düşman yok"); yield break; }
            Vector3 k = kapı.transform.position;
            Dur();
            Oyuncu.transform.position = new Vector3(k.x, Ben.y, k.z);                     // diskin ortası
            son.transform.position = new Vector3(k.x, son.transform.position.y, k.z - 1.2f);
            son.TakeDamage(Mathf.Max(0f, son.TimeLeft - 0.1f));                           // tek gerçek vuruşla ölsün
            yield return null;
            t0 = Şimdi;
            while (son != null && Şimdi - t0 < 10f)                                      // yerinden kıpırdamadan vur (DövüşAdımı'nın ritmi)
            {
                NişanAl(son.transform.position);
                if (Oyuncu != null && !Oyuncu.Attacking && Time.time - _sonVuruş > 0.15f) { _sol = true; _sonVuruş = Time.time; }
                yield return null;
            }
            bool öldü = son == null;
            float öldüğündeMesafe = Düz(Ben - k).magnitude;
            yield return Bekle(1.5f);
            bool geçmedi = Oda != null && Oda.Room == no;
            Kaydet("K-1", "Son düşman KAPININ ÜSTÜNDE gerçek vuruşla öldü: kapı açıldı, GEÇİRMEDİ",
                   öldü && kapı != null && kapı.IsOpen && öldüğündeMesafe <= 1.6f && geçmedi,
                   $"son düşman {(öldü ? "öldü" : "ÖLMEDİ")} · oda temiz {(Oda != null && Oda.Cleared)} · dalga {(Oda != null ? Oda.RemainingWaves : -1)} · canlı {Canlı()} · öldüğünde kapıya {öldüğündeMesafe:0.00} birim (disk 1.6) · kapı {(kapı != null && kapı.IsOpen ? "açık" : "KAPALI")} · 1.5 sn sonra oda {(Oda != null ? Oda.Room : -1)} ({(geçmedi ? "aynı" : "GEÇİRDİ")})");

            Sahne("KAPI 2/2", "diskten çık, yeniden bas → geçmeli");
            t0 = Şimdi;
            while (Şimdi - t0 < 0.8f) { Yürü(Vector3.back); yield return null; }
            Dur();
            float dışarıda = Düz(Ben - k).magnitude;
            bool çıkınca = Oda != null && Oda.Room == no;
            t0 = Şimdi;
            while (Oda != null && Oda.Room == no && Şimdi - t0 < 3f) { Yürü(k - Ben); yield return null; }
            Dur();
            Kaydet("K-2", "Diskten çıkıp yeniden basınca GEÇTİ (dışarıdan yürüyüp girmek)",
                   dışarıda > 1.6f && çıkınca && Oda != null && Oda.Room == no + 1,
                   $"çıkınca kapıya {dışarıda:0.00} birim, oda {(çıkınca ? "aynı" : "değişti")} · basınca oda {no} → {(Oda != null ? Oda.Room : -1)}");
        }

        // ── FAZ 34 PLAYTEST — 1. bölge içeriği (Enes 2026-10-20: "bittikten sonra testi bota yaptır") ──
        /// <summary>
        /// TAHSİLDAR'ın 5 adamı M ile, beklenen güçle ve saat doyma tavanında (MiniTesti gibi): adı ekranda mı, imzası
        /// dövüşte görüldü mü, öldü mü, kaç dakika. Sonra F6 → 1. bölgenin 2–7. odaları: KALKANLI ve MÜFLİS doğuyor mu,
        /// MÜFLİS patlayıp düşmana vuruyor mu, bot KALKANLI'nın arkasına dolanıyor mu, KISKAÇ ve ÇİFT SIRA geliyor mu.
        /// </summary>
        IEnumerator P34()
        {
            _bekçiDk = 30f;
            yield return AdamlarıDöv(0, "P34", "34-1", "TAHSİLDAR'ın 5 adamı: adı ekranda, imzası dövüşte, öldü, bota saat kazandırmadı (beklenen güç)");
            if (_bitti) yield break;

            Sahne("P34 2/2 YENİ DÜŞMANLAR", "F6 → 1. bölgenin 2–7. odası: KALKANLI · MÜFLİS · KISKAÇ · ÇİFT SIRA");
            Bas(Key.F6); yield return Bekle(0.6f);
            _pc = null; _kam = null;
            var türler = new Dictionary<string, int>();
            var dizilişler = new HashSet<string>();
            var işaretler = new HashSet<GroundMark>();
            var görülen = new HashSet<Enemy>();
            var odalar = new List<string>();
            _dal.Clear();
            for (int oda = 2; oda <= 7 && !_bitti; oda++)
            {
                yield return OdayaAtla(oda);
                if (Oda == null) break;
                string diziliş = Oda.Layout.Name;
                dizilişler.Add(diziliş);
                float t0 = Şimdi;
                while (Oda != null && Oda.Room == oda && !Oda.Cleared && Şimdi - t0 < 300f && !_bitti)
                {
                    foreach (var e in Enemy.All)
                        if (e != null && e.Kind != null && görülen.Add(e))
                            türler[e.Kind.Name] = (türler.TryGetValue(e.Kind.Name, out var n) ? n : 0) + 1;
                    foreach (var g in FindObjectsByType<GroundMark>(FindObjectsSortMode.None))
                        if (g != null && g.DüşmanHasarı > 0f) işaretler.Add(g);
                    if (Saat != null && Saat.Seconds < 60f && Time.time - _sonTakviye > 2f) { Bas(Key.F5); _sonTakviye = Time.time; _takviye++; }
                    DövüşAdımı(Mod.Dövüş);
                    yield return null;
                }
                Dur();
                odalar.Add($"{oda}. oda {diziliş} {Şimdi - t0:0} sn");
            }
            int vurulan = 0; foreach (var g in işaretler) vurulan += g.EnemiesHit;   // patlamış işaretin C# nesnesi okunuyor
            _dal.TryGetValue("dolan", out float dolandı);
            türler.TryGetValue(EnemyKind.Shield.Name, out int kalkan);
            türler.TryGetValue(EnemyKind.Bankrupt.Name, out int müflis);
            var türYazı = new List<string>(); foreach (var kv in türler) türYazı.Add($"{kv.Key} {kv.Value}");
            Kaydet("34-2", "1. bölgede KALKANLI ve MÜFLİS doğuyor, MÜFLİS patlayıp düşmana vuruyor, bot KALKANLI'nın arkasına dolanıyor, KISKAÇ ve ÇİFT SIRA geliyor",
                   kalkan > 0 && müflis > 0 && işaretler.Count > 0 && vurulan > 0 && dolandı > 0f
                   && dizilişler.Contains(RoomLayout.Pincer.Name) && dizilişler.Contains(RoomLayout.DoubleRow.Name),
                   $"{string.Join(" · ", türYazı)} · MÜFLİS patlaması {işaretler.Count} (düşmana {vurulan} isabet) · dolanma {dolandı:0.0} sn<br>{string.Join(" · ", odalar)}");
        }

        // ── FAZ 33c PLAYTEST — 12 yeni combo GERÇEK tuşlarla (açan yükseltmeler verilmiş) ──
        // Her combo: taze oda → A silahı yuva 1, B yuva 2 → A ile adım → 2 → B ile adım → combo oldu mu.
        // Kapı her combo'nun ETKİSİNİ ölçüyor; bu, oyuncunun tuş yolunun (adım → geçiş → bitiriş) gerçekten açık olduğunu.
        IEnumerator P33c()
        {
            _bekçiDk = 30f;
            for (int i = 0; i < Combo.Sayı; i++)                                 // açanlar + "+" hâlleri: "+" yolları da gerçek oyunda koşsun
                if (Combo.Bu((ComboTür)i).Açan is SilahYükseltme açan) { SilahSunağı.Al(açan, -1); Combo.ArtıAl((ComboTür)i, 0); }
            var satırlar = new List<string>();
            int tamam = 0, toplam = 0;
            for (int i = 0; i < Combo.Sayı && !_bitti; i++)
            {
                var c = (ComboTür)i; var tanım = Combo.Bu(c);
                if (!tanım.Açan.HasValue) continue;
                toplam++;
                Sahne($"P33c {tanım.Ad}", $"{Weapon.All[tanım.Sıra()[0]].Name} → {Weapon.All[tanım.Sıra()[1]].Name}");
                bool oldu = false; int deneme = 0;
                yield return ComboDene(c, (o, d) => { oldu = o; deneme = d; });
                if (oldu) tamam++;
                satırlar.Add($"{(oldu ? "✅" : "❌")} {tanım.Ad} ({deneme})");
                Debug.Log($"[P33c] {tanım.Ad}: {(oldu ? "oldu" : "OLMADI")} · {deneme} deneme");
            }
            Kaydet("33c", "12 yeni combo gerçek tuşlarla: A ile adım → 2 → B ile bitir (açanlar ve \"+\" hâlleri verilmiş)", tamam == toplam,
                   $"{tamam}/{toplam} · {string.Join(" · ", satırlar)}");
        }

        /// <summary>Bir combo'yu gerçek tuşlarla dene (en çok 6 kez): adım → 2 → (adım → 3 →) bitir. Tetiklenen BU combo mu —
        /// beklemesinden okunuyor: zincirde önce başka bir ikili de tetiklenebilir (ALACAK DEVRİ → İCRA TAKİBİ, Faz 42).</summary>
        IEnumerator ComboDene(ComboTür c, System.Action<bool, int> sonuç)
        {
            var tanım = Combo.Bu(c);
            int[] sıra = tanım.Sıra();
            bool oldu = false; int deneme = 0;
            while (deneme < 6 && !oldu && !_bitti)
            {
                deneme++;
                yield return OdayaAtla(1);
                Diz(sıra);
                Bas(Key.Digit1); yield return Bekle(0.3f);
                var pc = Oyuncu;
                var d = EnYakınBaşka(null);
                if (pc == null || d == null) continue;
                while (pc.ComboBeklemesi(c) > 0f) yield return null;            // önceki denemenin beklemesi bitsin
                if (Özeli(c) is int özel) { float t0 = Şimdi; while (pc.SpecialCooldownOf(özel) > 0f && Şimdi - t0 < 12f) yield return null; }   // sağ tık hazır olsun
                float b0 = pc.ComboBeklemesi(c);
                _adımNotu = "";
                // HACİZ'li combo'da duruş — TAHVİL'li hariç: basılı tutarken düşmanlar 1 sn yaklaşıp ölü bölgeye giriyor, orada HACİZ anında uzaktaki aranır
                if (System.Array.IndexOf(sıra, Kadro.HACİZ) >= 0 && System.Array.IndexOf(sıra, Kadro.TAHVİL) < 0) { yield return HacizDuruşu(d); var yakın = EnYakınBaşka(null); if (yakın != null) d = yakın; }
                else yield return Git(d.transform.position, 1.1f + d.Radius, 3f);
                for (int k = 0; k < sıra.Length && !_bitti; k++)
                {
                    if (k > 0) { Bas(k == 1 ? Key.Digit2 : Key.Digit3); yield return null; }
                    var hedef = k == 0 || c == ComboTür.Konsorsiyum || c == ComboTür.Öncelik ? d : EnYakınBaşka(null);   // bitiriş: bağlıya / en yakına
                    yield return ComboAdımı(sıra[k], hedef != null ? hedef : d, k, sıra.Length, c);
                }
                yield return Bekle(0.4f);
                oldu = Oyuncu == pc && pc.ComboBeklemesi(c) > b0;
                if (!oldu) Debug.Log($"[combo] {tanım.Ad} deneme {deneme}: adımlar{_adımNotu} · " +
                                     $"silah {(Oyuncu != null ? Oyuncu.Weapon.Name : "—")} · rehine {(Oyuncu != null && Oyuncu.Rehine != null ? (Oyuncu.Rehine == d ? "hedef" : "başka") : "yok")} · hedef {(d != null ? "canlı" : "öldü")}");
            }
            sonuç(oldu, deneme);
        }

        string _adımNotu = "";                                                  // başarısız denemede adım adım ne oldu
        /// <summary>Bağ isteyen combo: MÜŞTEREK adımı ZİNCİRLEME (sağ tık) — oyuncu gibi, tek basışta bağ.</summary>
        static bool BağGerekli(ComboTür c) => c == ComboTür.MüşterekKusur || c == ComboTür.OrtaklığınTasfiyesi;
        /// <summary>İlk adımı sağ tıkla yapılan combo'nun silahı (ZİNCİRLEME · AÇIK POZİSYON) — deneme beklemesi bitince başlasın.</summary>
        static int? Özeli(ComboTür c) => BağGerekli(c) ? Kadro.MÜŞTEREK
                                        : c == ComboTür.Spekülasyon || c == ComboTür.ArkadanDolanım ? Kadro.ARBİTRAJ : (int?)null;

        /// <summary>
        /// HACİZ'li combo'nun duruşu, oyuncu gibi: bir düşmanın dibi (bitiriş ona), öbürü HACİZ'in menzilinde (yüzey 4.2–6.8; dipteki
        /// ölü bölgede kalır). En yakın dibe yürür, düşmanlar yürüdükçe yer yeniden hesaplanır. Böyle çift yoksa en yakının dibine.
        /// ⚠️ Eski yol (önce en yakının dibi, uzak yoksa tek dash) ölü bölgede kalıyordu: dash 2.9'a açıyor, ölü bölge 3.2 (P42 ilk koşu).
        /// </summary>
        IEnumerator HacizDuruşu(Enemy d)
        {
            Enemy N = null, F = null; float iyi = float.MaxValue;
            foreach (var a in Enemy.All)
            {
                if (a == null || a.Dormant || a.Tutan != null) continue;
                Vector3 p = Dibi(a);
                foreach (var b in Enemy.All)
                {
                    if (b == null || b == a || b.Dormant) continue;
                    float y = Düz(b.transform.position - p).magnitude - b.Radius;
                    float yol = Düz(p - Ben).magnitude;
                    if (y > 4.2f && y < 6.8f && yol < iyi) { iyi = yol; N = a; F = b; }
                }
            }
            if (N == null) { yield return Git(d.transform.position, 1.1f + d.Radius, 3f); yield break; }
            float t0 = Şimdi;
            while (N != null && Enemy.All.Contains(N) && Şimdi - t0 < 3f)
            {
                Vector3 p = Dibi(N);
                if (Düz(p - Ben).magnitude < 0.3f) break;
                Yürü(KapıyıDolan(p));
                yield return null;
            }
            Dur();
        }

        /// <summary>Düşmanın oyuncuya bakan tarafında, yüzeyinden 0.9 birim — combo'nun yakın vuruşları buradan değer.</summary>
        Vector3 Dibi(Enemy e) => e.transform.position + Düz(Ben - e.transform.position).normalized * (e.Radius + 0.9f);

        /// <summary>HACİZ'in hedefi, oyuncu gibi: yüzeyi 4–7 birim uzaktaki (ölü bölge 3.2), 5.5'e en yakın düşman (yoksa null).</summary>
        Enemy Uzaktaki()
        {
            Enemy uzak = null; float iyi = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dormant) continue;
                float y = Düz(e.transform.position - Ben).magnitude - e.Radius;
                if (y > 4f && y < 7f && Mathf.Abs(y - 5.5f) < iyi) { iyi = Mathf.Abs(y - 5.5f); uzak = e; }
            }
            return uzak;
        }

        /// <summary>Bir silahın combo adımı (k. adım, n adımlı combo), oyuncunun yaptığı gibi: TAHVİL basılı tut-bırak (ilk adımda
        /// 2. kademe, ortada 1. — 0.8 sn'lik pencereye sığsın), REHİN yakala-fırlat, ÇEK değene kadar bekle, HACİZ uzaktakine,
        /// arkadan isteyen combo'da önce AÇIK POZİSYON, bağ isteyen MÜŞTEREK'te önce başkasını işaretle; öbürleri tık.</summary>
        IEnumerator ComboAdımı(int silah, Enemy d, int k, int n, ComboTür c)
        {
            bool ilk = k == 0, adım = k < n - 1;
            string not = Weapon.All[silah].Name;
            if (silah == Kadro.HACİZ)
            {
                var uzak = Uzaktaki();
                if (uzak != null) d = uzak;
                else if (d != null) { not += "(uzak yok → dash)"; yield return TekKareDash(Ben - d.transform.position); }   // yoksa açıl
            }
            if (d != null) not += $" {Düz(d.transform.position - Ben).magnitude - d.Radius:0.0}";
            float an = Şimdi;
            int adım0 = Oyuncu != null ? Oyuncu.AdımSayısı : 0;
            if (d != null) NişanAl(d.transform.position);
            if (adım && silah == Kadro.TAHVİL)
            {
                float t0 = Şimdi, tut = ilk ? 1.1f : 0.55f;
                while (Şimdi - t0 < tut) { if (d != null) NişanAl(d.transform.position); _sol = true; yield return null; }
            }
            else
            {
                if (ilk && (c == ComboTür.Spekülasyon || c == ComboTür.ArkadanDolanım)) { _sağ = true; yield return Bekle(0.15f); }   // AÇIK POZİSYON: her vuruş arkadan
                if (ilk && silah == Kadro.REHİN)                                 // yakala (adım değil), sonra fırlat
                {
                    _sol = true; yield return null;
                    float t1 = Şimdi;
                    while (Oyuncu != null && Oyuncu.Attacking && Şimdi - t1 < 1f) yield return null;
                    if (d != null) NişanAl(d.transform.position);
                }
                if (ilk && silah == Kadro.MÜŞTEREK && BağGerekli(c)) _sağ = true;   // ZİNCİRLEME: en yakın dördü tek bağa
                else _sol = true;
            }
            if (!adım) { yield return Bekle(0.25f); _adımNotu += $" · {not} bitiş"; yield break; }
            // Oyuncu gibi: adım zincire yazılır yazılmaz geç — beklersen HACİZ'in toparlanması 0.8 sn'lik pencereyi yer,
            // erken geçersen silah değişimi vuruşu keser.
            float t2 = Şimdi;
            yield return null;
            while (Oyuncu != null && Oyuncu.AdımSayısı == adım0 && Şimdi - t2 < 1.5f) yield return null;
            _adımNotu += $" · {not} {(Oyuncu != null && Oyuncu.AdımSayısı != adım0 ? $"✓ {Şimdi - an:0.00} sn" : "✗ YAZILMADI")}";
        }

        // ── FAZ 42 PLAYTEST — Parti 1'in 20 direkt combo'su GERÇEK tuşlarla ("+" hâlleri verilmiş) ──
        // İkililer 1. bölgede, üçlüler N ile 2. bölgede (üçlü kuralı). Kapı her combo'nun ETKİSİNİ ölçüyor; bu, oyuncunun tuş
        // yolunun (adım → geçiş → adım → geçiş → bitiriş) gerçekten açık olduğunu.
        IEnumerator P42()
        {
            _bekçiDk = 30f;
            var satırlar = new List<string>();
            int tamam = 0, toplam = 0;
            for (int üçlü = 0; üçlü < 2 && !_bitti; üçlü++)
            {
                if (üçlü == 1) { Bas(Key.N); yield return Bekle(1.2f); }
                for (int i = (int)ComboTür.Cebriİcra; i < Combo.Sayı && !_bitti; i++)
                {
                    var c = (ComboTür)i; var tanım = Combo.Bu(c);
                    if ((tanım.Sıra().Length == 3) != (üçlü == 1)) continue;
                    Combo.ArtıAl(c, 0);
                    toplam++;
                    var adlar = new List<string>(); foreach (int w in tanım.Sıra()) adlar.Add(Weapon.All[w].Name);
                    Sahne($"P42 {tanım.Ad}", string.Join(" → ", adlar));
                    bool oldu = false; int deneme = 0;
                    yield return ComboDene(c, (o, d) => { oldu = o; deneme = d; });
                    if (oldu) tamam++;
                    satırlar.Add($"{(oldu ? "✅" : "❌")} {tanım.Ad} ({deneme})");
                    Debug.Log($"[P42] {tanım.Ad}: {(oldu ? "oldu" : "OLMADI")} · {deneme} deneme");
                }
            }
            Kaydet("42", "Parti 1'in 20 direkt combo'su gerçek tuşlarla: ikililer 1. bölgede, üçlüler 2. bölgede (\"+\" hâlleri verilmiş)", tamam == toplam,
                   $"{tamam}/{toplam} · {string.Join(" · ", satırlar)}");
        }

        IEnumerator MiniTeşhis()
        {
            Sahne("MİNİ TEŞHİS", "M: ilk mini boss · 10 sn'de bir nabız");
            Bas(Key.M); yield return Bekle(1f);
            Bas(Key.F5); yield return Bekle(0.3f);
            yield return OdayıGeç(600f);
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
            float t0 = Şimdi;
            while (Saat != null && giriş - Saat.Seconds < 1.5f && Şimdi - t0 < 10f)
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
            // Faz 28: ilk sunak 7 dövüş odası + mini boss'tan sonra (eskiden 4 oda)
            for (int i = 0; i < 12 && Oda != null && !Oda.IsAltarRoom; i++)
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
            float ölçek0 = boss.Radius * 2f, havuz0 = _bossİlkHavuz;    // `Radius`: parlamanın şişirmediği boy
            float defter0 = SaveFile.BossLedger(boss.Boss);
            int ölüm0 = SaveFile.Current.deaths;
            SS("nemesis_once");
            float t0 = Şimdi;
            while (SaveFile.Current.deaths == ölüm0 && Şimdi - t0 < 120f)
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
            float yeniÖlçek = yeni != null ? yeni.Radius * 2f : -1f;
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
            Kuşan(Kadro.HACİZ);
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
            Kuşan(Kadro.KONİ);
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
            float t0 = Şimdi;
            while (boss != null && !(boss.Phase >= 3 && !boss.Dormant) && Şimdi - t0 < 300f)
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

        // ── TAM KOŞU (Faz 27 ölçümü) ─────────────────────────────────────────
        // Enes (2026-10-12): hedef "son boss'un İLK yenilmesine kadar toplam 8–10 saat".
        // Bunu sayıya çevirmek için önce BİR KOŞUNUN gerçekte kaç dakika sürdüğü lazım —
        // tahmin değil ölçüm. Bot bütün bölgeleri atlamadan oynuyor; F5 takviyesi koşu bitmesin
        // diye var ama her biri "saat burada bitiyordu" diye yazılıyor.

        bool _tamKoşu, _yalnızBölge1, _tekAraBölge;
        /// <summary>Fiyat ölçümü: TAHVİL'i tık tık değil basılı tutarak oyna (yükseltmelerinin çoğu olgunlukla ilgili).</summary>
        bool _tahvilTut;
        readonly List<string> _takviyeYeri = new List<string>();

        IEnumerator TamKoşu()
        {
            _tamKoşu = true;
            var bölgeSüresi = new float[8];
            var bossSüresi = new List<string>();
            var satırlar = new StringBuilder();
            float t0 = Şimdi;
            int güvenlik = 0;
            var miniSüresi = new List<string>();
            var dövüşOdası = new List<float>();
            var araBölgeSüresi = new Dictionary<string, float>();
            while (Saat != null && !Saat.Won && güvenlik++ < 400)
            {
                if (Saat.Dead) { Kaydet("TK", "bot öldü", false, _sahne); break; }
                var rm = Oda;
                if (rm == null) { yield return Bekle(0.5f); continue; }
                if (_yalnızBölge1 && rm.RegionIndex > 0) break;   // 1. bölge ölçümü bitti
                if (_tekAraBölge && (rm.RegionIndex > 0 || rm.SubRegion > 0)) break;
                int bölge = rm.RegionIndex, oda = rm.Room;
                string tür = rm.IsBossRoom ? "BOSS" : rm.IsMiniBossRoom ? "MİNİ" : rm.IsAltarRoom ? "SUNAK" : "oda";
                string araBölge = $"B{bölge + 1}.{rm.SubRegion + 1}";
                Sahne($"TAM KOŞU B{bölge + 1}·O{oda}", tür);
                float giriş = Saat.Seconds, ta = Şimdi;
                int takviye0 = _takviye;

                if (rm.IsBossRoom)
                {
                    string ad = BossAdı(Boss());
                    yield return BossuÖldür(1200f, kazanınca: rm.IsLastRegion);
                    bossSüresi.Add($"{ad} {(Şimdi - ta) / 60f:0.0} dk");
                    // Boss'un çağırdıkları kalabiliyor: oda temizlenmeden kapı açılmıyor.
                    if (!Saat.Won) yield return OdayıGeç(120f);
                }
                else if (rm.IsAltarRoom)
                {
                    // Faz 41: insan gibi yedek bırakır — boss'tan hemen önceki sunakta tavanı, öbürlerinde yarım tavanı korur. 40 sn yedekle
                    // sıkılan ekonomide MURABAHACI'ya tavanın üçte biriyle giriyordu; her sunakta tam tavanla da güçlenemeyip 1. bölgede ölüyordu.
                    bool bossÖncesi = rm.Room == RoomManager.SunakOdası(_t, Mathf.Max(1, _t.subRegionsPerRegion) - 1);   // son ara bölgenin sunağı
                    yield return SunaktanAl(Clock.Tavan(_t, rm.RegionIndex) * (bossÖncesi ? 1f : 0.5f));
                    yield return KapıdanÇık(30f);
                }
                else if (rm.IsMiniBossRoom)
                {
                    string ad = BossAdı(Boss());
                    yield return OdayıGeç(900f);
                    miniSüresi.Add($"{ad} {(Şimdi - ta) / 60f:0.0} dk");
                }
                else yield return OdayıGeç(600f);

                float süre = Şimdi - ta;
                if (tür == "oda") dövüşOdası.Add(süre);
                araBölgeSüresi.TryGetValue(araBölge, out float ab);
                araBölgeSüresi[araBölge] = ab + süre;
                if (bölge < bölgeSüresi.Length) bölgeSüresi[bölge] += süre;
                satırlar.Append($"B{bölge + 1}·O{oda} {tür} {süre:0}sn (saat {giriş:0}→{Saat.Seconds:0}" +
                                (_takviye > takviye0 ? $", takviye {_takviye - takviye0}" : "") + ") · ");
                // Oda değişmediyse (kapı bulunamadı) sonsuz döngüye girmesin.
                if (Oda != null && Oda.RegionIndex == bölge && Oda.Room == oda && !Saat.Won)
                { Kaydet("TK", "oda geçilemedi", false, $"B{bölge + 1}·O{oda} {tür}"); break; }
            }
            float toplam = (Şimdi - t0) / 60f;
            var bölgeler = new List<string>();
            for (int i = 0; i < bölgeSüresi.Length; i++) if (bölgeSüresi[i] > 0f) bölgeler.Add($"B{i + 1} {bölgeSüresi[i] / 60f:0.0}");
            bool tamam = Saat != null && (Saat.Won || (_yalnızBölge1 && Oda != null && Oda.RegionIndex > 0)
                                          || (_tekAraBölge && Oda != null && (Oda.RegionIndex > 0 || Oda.SubRegion > 0)));
            Kaydet("TK", _yalnızBölge1 ? "1. bölge" : _tekAraBölge ? "1. ara bölge" : "Tam koşu", tamam,
                   $"{(Saat != null && Saat.Won ? "KAZANILDI" : tamam ? "BİTTİ" : "bitmedi")} · {toplam:0.0} dk · bölgeler (dk): {string.Join(", ", bölgeler)}");
            if (dövüşOdası.Count > 0)
            {
                dövüşOdası.Sort();
                float ort = 0f; foreach (var x in dövüşOdası) ort += x; ort /= dövüşOdası.Count;
                Kaydet("TK", "Dövüş odası", null,
                       $"{dövüşOdası.Count} oda · ortalama {ort:0} sn · en kısa {dövüşOdası[0]:0} · en uzun {dövüşOdası[dövüşOdası.Count - 1]:0} sn");
            }
            var ab2 = new List<string>();
            foreach (var kv in araBölgeSüresi) ab2.Add($"{kv.Key} {kv.Value / 60f:0.0}");
            Kaydet("TK", "Ara bölgeler (dk)", null, string.Join(" · ", ab2));
            Kaydet("TK", "Mini boss dövüşleri", null, miniSüresi.Count > 0 ? string.Join(", ", miniSüresi) : "—");
            Kaydet("TK", "Boss dövüşleri", null, string.Join(", ", bossSüresi));
            Kaydet("TK", "Saat bitecekti (F5 takviyesi)", null,
                   _takviyeYeri.Count == 0 ? "hiç" : $"{_takviyeYeri.Count} kez: {string.Join(", ", _takviyeYeri)}");
            Kaydet("TK", "Sunak", null, $"{Saat.Purchases} alım · hasat %{Saat.HarvestRate * 100:0} · hasar {Saat.AttackDamage:0.0} · dash {Saat.DashCooldown:0.00} sn");
            Kaydet("TK", "Oda oda", null, satırlar.ToString());
        }

        /// <summary>Sunak odası: `yedek` saniye kalacak şekilde en ucuz geliştirmeyi al (ALICI profili).</summary>
        IEnumerator SunaktanAl(float yedek)
        {
            yield return Bekle(0.6f);
            for (int tur = 0; tur < 12; tur++)
            {
                Altar en = null;
                foreach (var a in Altar.All)
                    if (a != null && a.Type != Altar.Kind.Repair && a.Type != Altar.Kind.Loadout && a.Type != Altar.Kind.ComboPlus
                        && !a.SoldOut && (en == null || a.Price < en.Price)) en = a;
                if (en == null || Saat.Seconds - en.Price < yedek) break;
                yield return Git(en.transform.position, 1.2f, 6f);
                int alım = Saat.Purchases;
                Bas(Key.E);
                yield return Bekle(0.3f);
                if (Saat.Purchases == alım) break;      // alınamadı — döngüye girme
            }
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
            float t0 = Şimdi;
            while (boss != null && n < adet && Şimdi - t0 < sınır && (yeter == null || !yeter()))
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
            float g0 = Şimdi;
            int beşte = 0;
            while (boss != null && Şimdi - g0 < 150f && n < 20 && beşte < 3)
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
            float t0 = Şimdi;
            while (boss != null && !(boss.Phase >= 3 && !boss.Dormant) && Şimdi - t0 < 300f)
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
            float gerçek = (Time.realtimeSinceStartup - _başlangıç) / 60f, oyun = (Time.time - _oyunBaşı) / 60f;
            sb.AppendLine($"Süre: oyun {oyun:0.0} dk · gerçek {gerçek:0.0} dk" +
                          (Time.captureDeltaTime > 0f ? $" (hızlandırılmış ×{oyun / Mathf.Max(0.01f, gerçek):0})" : " (gerçek hız)"));
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
        /// <summary>
        /// `DövüşAdımı`'nın hangi dalında ne kadar OYUN zamanı geçti (vur / yaklaş / bekle / kaç·&lt;saldırı&gt; / işaret).
        /// `BossNabzı` 10 sn'de bir okuyup sıfırlıyor. (2026-10-18: SİMSAR 30 dk'da ölmedi — bot ne yapıyordu?)
        /// </summary>
        readonly Dictionary<string, float> _dal = new Dictionary<string, float>();
        string _kaçNeden = "";
        void Dal(string ad) { _dal.TryGetValue(ad, out var s); _dal[ad] = s + Time.deltaTime; }
        /// <summary>Saldırı adı → (bota dönük hazırlık, isabet). Haksız aday tablosu bundan.</summary>
        readonly Dictionary<string, Vector2Int> _saldırıİst = new Dictionary<string, Vector2Int>();
        float _sonTakviye = -99f, _sonVuruş = -99f, _bossİlkHavuz, _sonSilkme = -99f;
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
            // VADELİ (Faz 35): insan gibi — vadesi kapalıysa vadesi açık olana dön; hepsi kapalıysa en yakına
            // MÜHLET (Faz 40): insan gibi — solan MÜHLET önce; uzak duruyor, en yakını seçen bot ona hiç sıra vermiyordu (27'de 22 kaçtı)
            Enemy en = null, açık = null, mühlet = null; float d = float.MaxValue, da = float.MaxValue, dm = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dormant) continue;
                float x = Düz(e.transform.position - Ben).magnitude;
                if (x < d) { d = x; en = e; }
                if (e.VadeAçık && x < da) { da = x; açık = e; }
                if (e.Kind != null && e.Kind.MühletSüresi > 0f && !e.Hollow && x < dm) { dm = x; mühlet = e; }
            }
            return mühlet != null ? mühlet : açık != null ? açık : en;
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
                if (_tamKoşu && Oda != null) _takviyeYeri.Add($"B{Oda.RegionIndex + 1}·O{Oda.Room}");
            }

            // SÜLÜK (Faz 36): yapıştıysa insan gibi dash'le silkelen (ondan uzağa)
            if (pc.DashCooldownLeft <= 0f && Time.time - _sonSilkme > 0.5f)
                foreach (var e in Enemy.All)
                    if (e != null && e.Yapışık) { Yürü(ben - e.transform.position); Bas(Key.Space); _sonSilkme = Time.time; Dal("silkelen"); return; }

            bool kaçan = mod == Mod.Dövüş || mod == Mod.ÇokUzak || mod == Mod.DibindeKaçar;
            _kaçıyor = kaçan;
            if (kaçan && Kaç(ben)) { Dal("kaç·" + _kaçNeden); return; }
            if (hedef == null || hedef.Dormant) hedef = EnYakın();
            if (kaçan && İşarettenKaç(ben, mod == Mod.Dövüş ? hedef : null)) { Dal("işaret"); return; }   // uzak durma modlarında menzile çekme

            if (hedef == null) { Dur(); return; }
            Vector3 to = Düz(hedef.transform.position - ben);
            float d = to.magnitude;
            NişanAl(hedef.transform.position);

            switch (mod)
            {
                case Mod.Dövüş:
                    float menzil = pc.Weapon.Range(_t) * 0.8f + hedef.Radius;
                    if (d > menzil) { if (AlanaGirer(ben, to) || İşareteGirer(ben, to)) { Dur(); Dal("bekle"); } else { Yürü(to); Dal("yaklaş"); } }
                    else if (hedef.ÖndenÇarpanı < 1f && hedef.Önünde(ben))
                    {
                        Yürü(Vector3.Cross(Vector3.up, to)); Dal("dolan");          // KALKANLI (Faz 34) · ARACI (Faz 37): insan gibi yanından dolan
                    }
                    else
                    {
                        Dal("vur");
                        Dur();
                        if (_tahvilTut && pc.Weapon.Saldırı == Biçim.Tahvil)
                        { if (pc.VadeKademesi < 3) _sol = true; }                   // oyuncu gibi: 3. kademeye kadar basılı tut, bırak
                        else if (!pc.Attacking && Time.time - _sonVuruş > 0.15f) { _sol = true; _sonVuruş = Time.time; }
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

            if (_final != null && _final.Dormant && _uykuBaşı < 0f) _uykuBaşı = Şimdi;
            // ⚠️ Parlama sönsün diye 0.6 sn sonra: faz 3'e geçiş HEP bir vuruşun içinde,
            // o vuruşun 0.2 sn'lik beyaz parlaması ilk karede hâlâ yanıyor. İlk koşuda
            // ilk karede ölçüp "beyaz" dedim — ölçüm hatası.
            if (_final != null && _final.Dormant && !_uykuGörüldü && Şimdi - _uykuBaşı > 0.6f)
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
            var bt = tehdit.Find(x => x.IsBoss);
            _kaçNeden = bt != null && bt.CurrentAttack != null ? bt.CurrentAttack.Name : tehdit[0].Kind.Name;

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

        bool İşarettenKaç(Vector3 ben, Enemy hedef)
        {
            foreach (var g in FindObjectsByType<GroundMark>(FindObjectsSortMode.None))
            {
                if (g == null || !g.OyuncuyaVurur || g.Fired) continue;
                Vector3 to = Düz(ben - g.transform.position);
                float dış = g.Yarıçap + 0.6f;
                if (to.magnitude > dış) continue;
                // ⚠️ Çıkış noktası HEDEFİ MENZİLDE tutsun. İşaret DURDUĞUN yere düşüyor, bot hep tam
                // ortasındaydı: düz dışarı (`to` ≈ 0 → +X) kaçıp menzilden çıkıyor, işaretin patlamasını
                // bekleyip geri yürüyordu — SİMSAR faz 2'de zamanın %36'sı işaret, %17'si yaklaş, yalnızca
                // %25'i vuruş (teşhis 2026-10-18). `hedef` null ise eskisi gibi düz dışarı.
                Vector3 çık = ben + (to.sqrMagnitude > 0.01f ? to.normalized : Vector3.right) * (dış - to.magnitude);
                if (hedef != null) MenzildeÇıkış(ben, hedef, ref çık);
                // ⚠️ Yalnızca patlamaya YETİŞEMEYECEKSEN kaç. Bot işaretin içine girer girmez
                // kaçıyordu: SİMSAR faz 2'de 1.1 sn'de bir işaret düşüyor, bot hiç vuramadı,
                // 20 dk'da öldüremedi (tam koşu ölçümü). İnsan vurur, patlamadan önce çıkar.
                float çıkış = Düz(çık - ben).magnitude / _t.moveSpeed + İnsanTepkisi;
                if (g.Kalan > çıkış + 0.2f) continue;
                Yürü(çık - ben);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Hedefin çevresinde, menzil içinde, kurulu hiçbir işaretin (payıyla) içinde olmayan en yakın nokta —
        /// 15°'lik adımlarla iki yana (`GüvenliYön` deseni). Yoksa `çık` olduğu gibi kalır.
        /// </summary>
        void MenzildeÇıkış(Vector3 ben, Enemy hedef, ref Vector3 çık)
        {
            Vector3 h = hedef.transform.position; h.y = ben.y;
            Vector3 kol = Düz(ben - h);
            if (kol.sqrMagnitude < 0.01f) kol = Vector3.right;
            float r = Mathf.Clamp(kol.magnitude, hedef.Radius + 0.5f, Oyuncu.Weapon.Range(_t) * 0.7f + hedef.Radius);
            kol = kol.normalized * r;
            var işaretler = FindObjectsByType<GroundMark>(FindObjectsSortMode.None);
            for (int i = 1; i <= 24; i++)
            {
                var aday = h + Quaternion.Euler(0f, ((i + 1) / 2) * 15f * (i % 2 == 0 ? 1f : -1f), 0f) * kol;
                if (Düz(aday).magnitude > _t.arenaRadius * 0.9f) continue;
                bool temiz = true;
                foreach (var g in işaretler)
                    if (g != null && g.OyuncuyaVurur && !g.Fired && Düz(aday - g.transform.position).magnitude <= g.Yarıçap + 0.6f)
                    { temiz = false; break; }
                if (temiz) { çık = aday; return; }
            }
        }

        /// <summary>
        /// Bir sonraki adım kurulu bir işaretin İÇİNE mi (dışındayken). Girme — hedef zaten sana geliyor;
        /// bot çıktığı işarete geri yürüyüp yeniden kaçıyordu (teşhis 2026-10-18).
        /// </summary>
        static bool İşareteGirer(Vector3 ben, Vector3 yön)
        {
            var sonraki = ben + yön.normalized * 0.5f;
            foreach (var g in FindObjectsByType<GroundMark>(FindObjectsSortMode.None))
            {
                if (g == null || !g.OyuncuyaVurur || g.Fired) continue;
                float r = g.Yarıçap + 0.3f;
                if (Düz(sonraki - g.transform.position).magnitude < r && Düz(ben - g.transform.position).magnitude >= r)
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
            float bitiş = Şimdi + sn;
            while (Şimdi < bitiş) { Gözler(Ben); yield return null; }
        }

        IEnumerator Dövüş(float sn)
        {
            float bitiş = Şimdi + sn;
            while (Şimdi < bitiş) { DövüşAdımı(Mod.Dövüş); yield return null; }
            Dur();
        }

        IEnumerator Git(Vector3 hedef, float yakın, float sınır)
        {
            float t0 = Şimdi;
            while (Düz(hedef - Ben).magnitude > yakın && Şimdi - t0 < sınır)
            {
                Yürü(KapıyıDolan(hedef));
                yield return null;
            }
            Dur();
        }

        /// <summary>
        /// Hedefe yön, ama çıkış kapısının üstünden geçmeden: açık kapıya basmak odayı değiştiriyor. Boss'u kapının
        /// kuzeyinde bitiren bot sunağa yürürken kapıdan geçip 2. bölgeye düştü (P30, 2026-10-20). İnsan kapının yanından dolanır.
        /// </summary>
        Vector3 KapıyıDolan(Vector3 hedef)
        {
            Vector3 yön = Düz(hedef - Ben);
            foreach (var g in FindObjectsByType<ExitGate>(FindObjectsSortMode.None))
            {
                Vector3 k = Düz(g.transform.position - Ben);
                if (Düz(hedef - g.transform.position).magnitude < 2.6f || k.magnitude > 2.6f || Vector3.Dot(k, yön) <= 0f) continue;
                Vector3 yan = Vector3.Cross(Vector3.up, k).normalized;               // kapının teğeti — hedefe yakın yana
                return Vector3.Dot(yan, yön) >= 0f ? yan : -yan;
            }
            return yön;
        }

        /// <summary>Odadaki herkesi öldür, açılan kapıdan geç.</summary>
        IEnumerator OdayıGeç(float sınır)
        {
            if (Oda == null) yield break;
            yield return OdayıTemizle(sınır);
            if (Oda != null && !Oda.Cleared)
                Kaydet("—", $"oda {Oda.Room} temizlenemedi", false, $"{sınır:0} sn yetmedi");
            yield return KapıdanÇık(20f);
        }

        /// <summary>
        /// Odadaki herkesi öldür, odada kal (keşif köşesi). Kapı açıldığında üstündeysen geçirmiyor (`ExitGate`) —
        /// eskiden son düşmanı kapıya 1.51'de öldüren bot o karede sonraki odadaydı (P32-7, 2026-10-20).
        /// </summary>
        IEnumerator OdayıTemizle(float sınır)
        {
            float t0 = Şimdi;
            while (Oda != null && !Oda.Cleared && Şimdi - t0 < sınır) { DövüşAdımı(Mod.Dövüş); yield return null; }
            Dur();
        }

        IEnumerator KapıdanÇık(float sınır)
        {
            if (Oda == null) yield break;
            int oda = Oda.Room, bölge = Oda.RegionIndex;
            float t0 = Şimdi;
            while (Oda != null && Oda.Room == oda && Oda.RegionIndex == bölge
                   && Şimdi - t0 < sınır)
            {
                if (Saat != null && Saat.Seconds < 20f && Time.time - _sonTakviye > 2f)
                { Bas(Key.F5); _sonTakviye = Time.time; _takviye++; }
                var kapı = FindFirstObjectByType<ExitGate>();
                Vector3 hedef = kapı != null ? kapı.transform.position : Bootstrap.Exit(_t);
                // Kapı açıldığında üstündeydin: önce diskten çık, sonra yeniden bas (yoksa kapının ortasında takılırsın).
                if (kapı != null && kapı.ÖnceÇık)
                {
                    Vector3 dış = Düz(Ben - hedef);
                    hedef += (dış.sqrMagnitude > 0.01f ? dış.normalized : Vector3.back) * 2.5f;
                }
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
            float t0 = Şimdi;
            while (boss != null && liste.Count < adet && Şimdi - t0 < sınır)
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
            float t0 = Şimdi;
            while (Şimdi - t0 < sınır)
            {
                // ⚠️ Boss düştü ≠ oda temiz. Çağırdıkları (SİMSAR'ın MÜBAŞİR'leri, KEFİL'inkiler) sağken silah
                // açılmıyor, sunak çıkmıyor, kapı kapalı — hepsi oda TEMİZLENİNCE (`RoomManager`). P32-5 SİMSAR'ı
                // 939. sn'de öldürüp 1 sn sonra ARBİTRAJ'a baktı, 2 MÜBAŞİR sağdı (2026-10-20; "30 dk'da ölmüyor"
                // sanılmıştı). Ölüp kontrol noktasına dönmek de bitiş: artık boss odasında değilsin.
                if (kazanınca ? (Saat != null && Saat.Won) : Boss() == null && (Oda == null || !Oda.IsBossRoom || Oda.Cleared)) break;
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
