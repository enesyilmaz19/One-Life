using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Test arenasını kodla kurar — sahne düzenlemesi yok.
    /// Zemin, oyuncu, kamera, sistemler, düşmanlar ve sunaklar; hepsi tek `Run` kökü altında,
    /// böylece yeniden başlatma tek `Destroy` (bkz. `Restart`).
    ///
    /// ponytail: sahne YAML'ı elle yazmak veya editörden sahne üretmek kırılgan
    /// (GUID eşleştirme, prefab bağları). Greybox tamamen koddan doğuyor:
    /// Play'e bas, arena hazır. Faz 4'te gerçek seviyeler gelince sahneye geçilir.
    /// </summary>
    public static class Bootstrap
    {
        /// <summary>
        /// Arenanın merkezi, duruş yüksekliğinde. Yerleşim matematiğinin TEK kaynağı:
        /// `RoomLayout`, `Entrance`, `Exit` ve testler `.y`'sini buradan alıyor.
        /// Faz1Repro da buradan okuyor — yoksa reprodüksiyon oyundan sessizce ayrışır.
        ///
        /// ⚠️ Faz 8'e kadar adı `PlayerSpawn`'dı ve o ad ARTIK YALANDI: oyuncu burada
        /// değil, `Entrance(t)`'de başlıyor. Konum için iki gerçek kaynağı bu projede
        /// defalarca hata üretti; ismin doğruyu söylemesi o hatanın panzehiri.
        /// </summary>
        public static readonly Vector3 ArenaCenter = new Vector3(0f, 1f, 0f);
        public static readonly Vector3 EnemyScale = new Vector3(0.9f, 0.9f, 0.9f);

        /// <summary>Düşmanların doğduğu halkanın arena yarıçapına oranı.</summary>
        public const float SpawnRingFactor = 0.62f;

        /// <summary>
        /// Halka doğum formülü. Faz 8'de oda yerleşimleri `RoomLayout`'a taşındı;
        /// bu formül testlerin sabit halka kurması için duruyor.
        ///
        /// ⚠️ İki kez ayrıştı, iki kez canımızı yaktı:
        /// ① Yükseklik yarıçapla ÇARPILMAMALI. Eski hali `new Vector3(cos, 1f, sin) * radius`
        ///    idi ve y'yi de çarpıyordu → düşmanlar y=7.7'de, havada doğuyordu. ChaseMove
        ///    yalnızca yatay hareket ettiği için hiç inmiyorlardı; izometrikte havadaki nesne
        ///    ekranda kayık görünür, oyuncu gördüğü yere nişan alıp ıskalıyordu.
        /// ② Faz 4'te WaveManager kendi formülünü yazdı (yarıçap 0.62 vs 0.55) → oyun artık
        ///    testlerin sınadığı yoldan geçmiyordu. Öz-inceleme yakaladı, tek kaynağa indirildi.
        /// </summary>
        public static Vector3 RingPosition(Tuning t, int index, int count, float angleOffsetRad = 0f)
        {
            float a = (360f / Mathf.Max(1, count)) * index * Mathf.Deg2Rad + angleOffsetRad;
            float r = t.arenaRadius * SpawnRingFactor;
            return new Vector3(Mathf.Cos(a) * r, ArenaCenter.y, Mathf.Sin(a) * r);
        }

        /// <summary>Testler için kısayol — üretim halkasının aynısı.</summary>
        public static Vector3 EnemySpawnPosition(Tuning t, int index)
            => RingPosition(t, index, t.enemyCount);

        /// <summary>
        /// Odanın girişi ve çıkışı — zıt kenarlarda.
        ///
        /// ⚠️ Oda artık BİR YOL. Tek arenada her yer aynıydı ve mekânın hiçbir anlamı
        /// yoktu; şimdi girdiğin yerden çıkacağın yere gitmen gerekiyor ve düşmanlar
        /// aradaki bandı tutuyor. `RoomLayout` yerleşimleri bu eksene göre kuruluyor.
        /// </summary>
        public static Vector3 Entrance(Tuning t)
            => new Vector3(0f, ArenaCenter.y, -t.arenaRadius * 0.82f);

        public static Vector3 Exit(Tuning t)
            => new Vector3(0f, ArenaCenter.y, t.arenaRadius * 0.82f);

        /// <summary>Koşuya ait her şey bunun altında — yeniden başlatma tek Destroy.</summary>
        static GameObject _runRoot;

        /// <summary>Zemin — bölge kimliğinin greybox'taki taşıyıcısı.</summary>
        static Renderer _groundRenderer;

        /// <summary>
        /// `Tuning.asset`'in çalışma kopyası. ⚠️ Sahneye bağlı DEĞİL — `Run` kökü
        /// yok edilince onunla gitmiyor, elle alınması gerekiyor (bkz. ResetStatics).
        /// </summary>
        static Tuning _workingTuning;

        /// <summary>
        /// Bölge kimliği: zemin rengi. Greybox'ta bölgeler arasındaki farkı gözle
        /// gördüğün tek şey bu — kural HİSSEDİLİYOR ama görünmüyor, renk onu duyuruyor.
        /// </summary>
        public static void SetGroundColor(Color c)
        {
            if (_groundRenderer != null) _groundRenderer.material.color = c;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Build()
        {
            ResetStatics();
            _runRoot = new GameObject("Run");

            var tuningAsset = Resources.Load<Tuning>("Tuning");
            var t = tuningAsset;
            if (t == null)
            {
                Debug.LogWarning("[Tek Can] Tuning.asset bulunamadı, varsayılan değerlerle çalışılıyor.");
                t = ScriptableObject.CreateInstance<Tuning>();
            }
            else
            {
                // ⚠️ ASSET'İN KENDİSİYLE OYNANMAZ — KLONLA çalışılır.
                // Editörde bir ScriptableObject'i çalışma anında değiştirmek DİSKE yazar.
                // A/B profil değiştirici tam olarak bunu yapıyor (ayarları canlı çeviriyor);
                // klon olmasaydı bir profil denemesi Enes'in Inspector'daki bütün his
                // değerlerini kalıcı olarak ezerdi.
                t = Object.Instantiate(t);
                t.name = "Tuning (çalışma kopyası)";
                _workingTuning = t;      // ResetStatics bunu alacak
            }

            // Şablondan gelen sahne içeriğini temizle.
            //
            // ⚠️ `Destroy` DEĞİL, `DestroyImmediate`. `Object.Destroy` yok etmeyi kare
            // SONUNA erteler; işaretlenen nesne o kare boyunca yaşamaya ve `FindObjectsByType`
            // tarafından BULUNMAYA devam eder. Aşağıda kurulan sistemler "sahnede X var mı?"
            // diye sorduğunda ölmekte olan şablon nesnesini görüp yanlış karar veriyorlar.
            //
            // Bu tam olarak sesi öldüren şeydi: şablonun Main Camera'sı AudioListener
            // taşıyor; `Sfx.Init` "dinleyici var" diye onu buluyor, yenisini eklemiyordu;
            // kare bitince o dinleyici de kamerayla gidiyordu → oyunda hiç ses yok.
            // (`Restart` bu tuzağı zaten biliyordu ve SetActive(false) ile korunuyordu;
            //  burada aynı bilgi uygulanmamıştı.)
            //
            // Şablon içeriği bizim değil ve hiçbir şey henüz kurulmadı — hemen silmek güvenli.
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
                Object.DestroyImmediate(c.gameObject);

            // Aydınlatma okunabilirliğin parçası: düşman durumları RENKLE ayrışıyor
            // (kırmızı dolu / turuncu hazırlık / beyaz isabet / gri boş). Ortam ışığı
            // zayıfsa hepsi aynı koyu griye ezilir ve geri bildirim yok olur.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.48f);

            // Yukarıdaki gerekçe: şablon temizliği ertelenmez.
            foreach (var old in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
                Object.DestroyImmediate(old.gameObject);

            var lightGo = new GameObject("Sun"); lightGo.transform.SetParent(_runRoot.transform);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.3f;
            // Kameraya yakın yönden gelsin ki bize dönük yüzler aydınlansın.
            lightGo.transform.rotation = Quaternion.Euler(55f, t.camYaw - 25f, 0f);

            // Zemin — rengi BÖLGEYE ait, `RoomManager` bölgeye girerken boyuyor.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Arena"; ground.transform.SetParent(_runRoot.transform);
            ground.transform.localScale = Vector3.one * (t.arenaRadius / 5f) * 2f;
            _groundRenderer = ground.GetComponent<Renderer>();
            Paint(ground, new Color(0.16f, 0.16f, 0.19f));

            // Oyuncu
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player"; player.transform.SetParent(_runRoot.transform);
            player.transform.position = ArenaCenter;
            Object.Destroy(player.GetComponent<Collider>());
            Paint(player, new Color(0.9f, 0.9f, 0.95f));

            // Yön göstergesi — hangi tarafa baktığı greybox'ta görünmeli
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.2f, 0.7f);
            nose.transform.localScale = new Vector3(0.25f, 0.25f, 0.6f);
            Object.Destroy(nose.GetComponent<Collider>());
            Paint(nose, new Color(1f, 0.55f, 0.2f));

            // Kamera
            var camGo = new GameObject("IsoCamera"); camGo.transform.SetParent(_runRoot.transform);
            var cam = camGo.AddComponent<Camera>();
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.09f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<IsoCamera>().Init(t, player.transform);

            // Sistemler
            var systems = new GameObject("Systems"); systems.transform.SetParent(_runRoot.transform);
            HitStop.Init(systems.AddComponent<HitStop>());
            Sfx.Init(systems.AddComponent<AudioSource>());

            // Kayıt her şeyden ÖNCE: checkpoint odası ve oradaki zaman buradan geliyor.
            if (SaveFile.Current == null) SaveFile.Load();
            // ⚠️ Hiç kayıt yoksa DA yeni koşu kurulmalı — yoksa oyunu ilk kez açan
            // oyuncu sıfır diriliş eşyasıyla başlar ve ilk ölümde oyun biter.
            if (SaveFile.NeedsNewRun) SaveFile.NewRun(t);
            var save = SaveFile.Current;

            var clock = systems.AddComponent<Clock>();
            clock.Init(t, player.transform, save.checkpointSeconds);
            // Checkpoint DURUMU geri yüklüyor, yalnızca saati değil.
            if (save.HasUpgrades)
                clock.RestoreUpgrades(save.harvestRate, save.attackDamage, save.dashCooldown);
            systems.AddComponent<RevivalSystem>().Init(t, clock);
            var pc = player.AddComponent<PlayerController>();
            pc.Init(t, camGo.transform);
            systems.AddComponent<Hud>().Init(clock, pc);

            // A/B ayar profilleri (F1-F4) — TEST ARACI, gerçek oyunda kalkacak.
            systems.AddComponent<ProfileSwitcher>().Init(tuningAsset, t);

            // Oda yöneticisi — ilk odayı kendisi kuruyor: düşmanları yerleştiriyor,
            // oyuncuyu girişe koyuyor, çıkışı açıyor. Sunaklar da odaya ait artık,
            // arenaya değil (bkz. RoomManager.PlaceAltars).
            systems.AddComponent<RoomManager>().Init(t, player.transform, camGo.transform,
                                                     save.checkpointRoom, save.checkpointRegion);

            Debug.Log($"[Tek Can] Arena hazır — oda {save.checkpointRoom}/{t.roomsPerRegion}, " +
                      $"{clock.Seconds:0}s saat, {RevivalKind.IntactCount(save)} diriliş eşyası.");
        }

        /// <summary>
        /// Oyun bittikten sonra SIFIRDAN yeni oyun. Checkpoint, eşyalar, her şey gider —
        /// "tek can" bunu söylüyor. `Restart` ile karıştırma: o checkpoint'e döner.
        /// </summary>
        public static void NewRun()
        {
            var t = Resources.Load<Tuning>("Tuning") ?? ScriptableObject.CreateInstance<Tuning>();
            SaveFile.NewRun(t);
            Restart();
        }

        /// <summary>Sahneyi yeniden kurar — checkpoint'ten devam. Kaydı SIFIRLAMAZ.</summary>
        public static void Restart()
        {
            if (_runRoot != null)
            {
                // Destroy kare sonunda işliyor; önce kapat ki eski koşunun bileşenleri
                // yeni koşuyla aynı karede bir tur daha Update almasın.
                _runRoot.SetActive(false);
                Object.Destroy(_runRoot);
            }
            _runRoot = null;
            Build();
        }

        /// <summary>
        /// Üç sunağı odanın ortasına halka hâlinde koyar. Yerleşimin TEK kaynağı —
        /// `RoomManager` de testler de buradan geçiyor.
        /// </summary>
        public static void SpawnAltarRing(Tuning t, Transform player, Transform parent)
        {
            var kinds = new[] { Altar.Kind.Harvest, Altar.Kind.Damage, Altar.Kind.Dash };
            for (int i = 0; i < kinds.Length; i++)
            {
                float a = (360f / kinds.Length) * i * Mathf.Deg2Rad + Mathf.PI * 0.25f;
                float r = t.arenaRadius * 0.28f;
                SpawnAltar(t, new Vector3(Mathf.Cos(a) * r, ArenaCenter.y, Mathf.Sin(a) * r),
                           player, kinds[i], parent);
            }
        }

        /// <summary>
        /// ONARIM sunağı — girişe yakın, oyuncunun yolunun üstünde. Checkpoint odasında
        /// duruyor: soluklanma noktasında ömründen verip güvenlik ağını tamir ediyorsun.
        /// </summary>
        public static void SpawnRepairAltar(Tuning t, Transform player, Transform parent)
        {
            var pos = new Vector3(t.arenaRadius * 0.22f, ArenaCenter.y, -t.arenaRadius * 0.55f);
            SpawnAltar(t, pos, player, Altar.Kind.Repair, parent);
        }

        static void SpawnAltar(Tuning t, Vector3 pos, Transform player, Altar.Kind kind,
                               Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Altar_" + kind;
            go.transform.SetParent(parent != null ? parent : _runRoot.transform);
            go.transform.position = pos + Vector3.up * 0.2f;
            go.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);
            Object.Destroy(go.GetComponent<Collider>());
            go.AddComponent<Altar>().Init(t, player, kind);
        }

        /// <summary>
        /// "Enter Play Mode Options" açıkken domain reload olmaz ve statikler iki Play
        /// arasında yaşar — Enemy.All ölü kayıtlarla dolar, hit-stop kilitli kalır.
        /// His ayarı yaparken Play'e sık basılacağı için o seçenek büyük ihtimalle açılacak.
        /// </summary>
        static void ResetStatics()
        {
            Enemy.All.Clear();
            Altar.ResetAll();
            RoomManager.Reset();
            RevivalSystem.Reset();
            Region.Reset();
            TuneProfile.Reset();

            // ⚠️ Çalışma kopyası da bir klon — sahneye bağlı değil, elle alınmalı.
            // Yoksa her Restart'ta bir Tuning daha birikiyordu.
            if (_workingTuning != null)
            {
                if (Application.isPlaying) Object.Destroy(_workingTuning);
                else Object.DestroyImmediate(_workingTuning);
                _workingTuning = null;
            }

            _groundRenderer = null;
            HitStop.Reset();
            Arc.Reset();
            Mote.Reset();
            UnlitMat.Reset();
            Sfx.Reset();
            Clock.Reset();        // ← eksikti: ölü saate hasar yazılmasını önler
            IsoCamera.Reset();    // ← eksikti: aynı gerekçe
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Düşman doğurmanın TEK yolu — WaveManager da buradan geçiyor.
        /// İki ayrı doğum kodu olsaydı biri düzeltilip diğeri unutulurdu
        /// (düşmanların havada doğduğu hata tam olarak öyle bir ayrışmadan çıkmıştı).
        /// </summary>
        public static void SpawnEnemyAt(Tuning t, Vector3 pos, Transform player, Transform cam,
                                        float poolMultiplier = 1f, bool boss = false,
                                        EnemyKind kind = null, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = boss ? "Boss" : "Enemy";
            // Oda kökü verildiyse oraya: oda sökülünce içindekiler onunla gider.
            if (parent != null) go.transform.SetParent(parent);
            else if (_runRoot != null) go.transform.SetParent(_runRoot.transform);
            go.transform.position = pos;
            go.transform.localScale = boss ? EnemyScale * 1.9f : EnemyScale;
            Object.Destroy(go.GetComponent<Collider>());

            var labelGo = new GameObject("TimeLabel");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, boss ? 1.1f : 1.6f, 0f);
            var label = labelGo.AddComponent<TimeLabel>();
            label.Init(cam);

            go.AddComponent<Enemy>().Init(t, player, label, poolMultiplier, boss, kind);
        }

        static void Paint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = c;
        }
    }
}
