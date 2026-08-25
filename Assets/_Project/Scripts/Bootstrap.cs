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
        /// Arena yerleşiminin TEK kaynağı. Faz1Repro da buradan okuyor —
        /// yoksa reprodüksiyon oyundan sessizce ayrışır ve yanlış sonuç verir.
        /// </summary>
        public static readonly Vector3 PlayerSpawn = new Vector3(0f, 1f, 0f);
        public static readonly Vector3 EnemyScale = new Vector3(0.9f, 0.9f, 0.9f);

        public static Vector3 EnemySpawnPosition(Tuning t, int index)
        {
            // ⚠️ Yükseklik yarıçapla ÇARPILMAMALI. Eski hali
            //   new Vector3(cos, 1f, sin) * radius
            // idi ve y'yi de çarpıyordu: düşmanlar y=7.7'de, havada doğuyordu.
            // ChaseMove yalnızca yatay hareket ettiği için hiç inmiyorlardı.
            // İzometrikte havadaki nesne ekranda kayık görünür → oyuncu gördüğü
            // yere nişan alıp ıskalıyordu. "Hasar vurmuyorum" şikâyetinin sebebi buydu.
            float a = (360f / Mathf.Max(1, t.enemyCount)) * index * Mathf.Deg2Rad;
            float r = t.arenaRadius * 0.55f;
            return new Vector3(Mathf.Cos(a) * r, PlayerSpawn.y, Mathf.Sin(a) * r);
        }

        /// <summary>Koşuya ait her şey bunun altında — yeniden başlatma tek Destroy.</summary>
        static GameObject _runRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Build()
        {
            ResetStatics();
            _runRoot = new GameObject("Run");

            var t = Resources.Load<Tuning>("Tuning");
            if (t == null)
            {
                Debug.LogWarning("[Tek Can] Tuning.asset bulunamadı, varsayılan değerlerle çalışılıyor.");
                t = ScriptableObject.CreateInstance<Tuning>();
            }

            // Şablondan gelen sahne içeriğini temizle
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
                Object.Destroy(c.gameObject);

            // Aydınlatma okunabilirliğin parçası: düşman durumları RENKLE ayrışıyor
            // (kırmızı dolu / turuncu hazırlık / beyaz isabet / gri boş). Ortam ışığı
            // zayıfsa hepsi aynı koyu griye ezilir ve geri bildirim yok olur.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.48f);

            foreach (var old in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
                Object.Destroy(old.gameObject);

            var lightGo = new GameObject("Sun"); lightGo.transform.SetParent(_runRoot.transform);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.3f;
            // Kameraya yakın yönden gelsin ki bize dönük yüzler aydınlansın.
            lightGo.transform.rotation = Quaternion.Euler(55f, t.camYaw - 25f, 0f);

            // Zemin
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Arena"; ground.transform.SetParent(_runRoot.transform);
            ground.transform.localScale = Vector3.one * (t.arenaRadius / 5f) * 2f;
            Paint(ground, new Color(0.16f, 0.16f, 0.19f));

            // Oyuncu
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player"; player.transform.SetParent(_runRoot.transform);
            player.transform.position = PlayerSpawn;
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
            var clock = systems.AddComponent<Clock>();
            clock.Init(t, player.transform);
            var pc = player.AddComponent<PlayerController>();
            pc.Init(t, camGo.transform);
            systems.AddComponent<Hud>().Init(clock, pc);

            // Düşmanlar
            for (int i = 0; i < t.enemyCount; i++)
                SpawnEnemy(t, EnemySpawnPosition(t, i), player.transform, camGo.transform);

            // Sunaklar — saniyeyle satın alma (Faz 2)
            var kinds = new[] { Altar.Kind.Harvest, Altar.Kind.Damage, Altar.Kind.Dash };
            for (int i = 0; i < kinds.Length; i++)
            {
                float a = (360f / kinds.Length) * i * Mathf.Deg2Rad + Mathf.PI * 0.25f;
                float r = t.arenaRadius * 0.28f;
                SpawnAltar(t, new Vector3(Mathf.Cos(a) * r, PlayerSpawn.y, Mathf.Sin(a) * r),
                           player.transform, kinds[i]);
            }

            Debug.Log($"[Tek Can] Arena hazır — {t.enemyCount} düşman, {kinds.Length} sunak, {t.startSeconds:0}s saat.");
        }

        /// <summary>Koşuyu sıfırdan kurar. Ölüm ekranı R'ye basınca burayı çağırıyor.</summary>
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

        static void SpawnAltar(Tuning t, Vector3 pos, Transform player, Altar.Kind kind)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Altar_" + kind;
            go.transform.SetParent(_runRoot.transform);
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
            HitStop.Reset();
            Arc.Reset();
            Clock.Reset();        // ← eksikti: ölü saate hasar yazılmasını önler
            IsoCamera.Reset();    // ← eksikti: aynı gerekçe
            Time.timeScale = 1f;
        }

        static void SpawnEnemy(Tuning t, Vector3 pos, Transform player, Transform cam)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Enemy"; go.transform.SetParent(_runRoot.transform);
            go.transform.position = pos;
            go.transform.localScale = EnemyScale;
            Object.Destroy(go.GetComponent<Collider>());

            var labelGo = new GameObject("TimeLabel");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var label = labelGo.AddComponent<TimeLabel>();
            label.Init(cam);

            go.AddComponent<Enemy>().Init(t, player, label);
        }

        static void Paint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = c;
        }
    }
}
