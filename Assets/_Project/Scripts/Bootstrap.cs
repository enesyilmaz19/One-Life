using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Faz 1 arenasını kodla kurar — sahne düzenlemesi yok.
    ///
    /// ponytail: sahne YAML'ı elle yazmak veya editörden sahne üretmek kırılgan
    /// (GUID eşleştirme, prefab bağları). Greybox tamamen koddan doğuyor:
    /// Play'e bas, arena hazır. Faz 4'te gerçek seviyeler gelince sahneye geçilir.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Build()
        {
            ResetStatics();

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

            var lightGo = new GameObject("Sun");
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.3f;
            // Kameraya yakın yönden gelsin ki bize dönük yüzler aydınlansın.
            lightGo.transform.rotation = Quaternion.Euler(55f, t.camYaw - 25f, 0f);

            // Zemin
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Arena";
            ground.transform.localScale = Vector3.one * (t.arenaRadius / 5f) * 2f;
            Paint(ground, new Color(0.16f, 0.16f, 0.19f));

            // Oyuncu
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 1f, 0f);
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
            var camGo = new GameObject("IsoCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.09f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<IsoCamera>().Init(t, player.transform);

            // Sistemler
            var systems = new GameObject("Systems");
            HitStop.Init(systems.AddComponent<HitStop>());
            var clock = systems.AddComponent<Clock>();
            clock.Init(t, player.transform);
            var pc = player.AddComponent<PlayerController>();
            pc.Init(t, camGo.transform);
            systems.AddComponent<Hud>().Init(clock, pc);

            // Düşmanlar
            for (int i = 0; i < t.enemyCount; i++)
            {
                float a = (360f / Mathf.Max(1, t.enemyCount)) * i * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(a), 1f, Mathf.Sin(a)) * (t.arenaRadius * 0.55f);
                SpawnEnemy(t, pos, player.transform, camGo.transform);
            }

            Debug.Log($"[Tek Can] Faz 1 arena hazır — {t.enemyCount} düşman, {t.startSeconds:0}s saat.");
        }

        /// <summary>
        /// "Enter Play Mode Options" açıkken domain reload olmaz ve statikler iki Play
        /// arasında yaşar — Enemy.All ölü kayıtlarla dolar, hit-stop kilitli kalır.
        /// His ayarı yaparken Play'e sık basılacağı için o seçenek büyük ihtimalle açılacak.
        /// </summary>
        static void ResetStatics()
        {
            Enemy.All.Clear();
            HitStop.Reset();
            Arc.Reset();
            Time.timeScale = 1f;
        }

        static void SpawnEnemy(Tuning t, Vector3 pos, Transform player, Transform cam)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Enemy";
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
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
