using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Her oda yerleşimini GERÇEK `RoomManager` ile kurup PNG'ye çeker.
    ///
    /// ⚠️ `FeedbackCapture`'dan farkı: o sahneyi elle kuruyor, yani üretimi KOPYALIYOR.
    /// Bu araç `RoomManager.Init` + `NextRoom` çağırıyor — oyunun kendi yolundan geçiyor.
    /// Yerleşim üretimde kayarsa görüntü de kayar; kopyalasaydık sessizce ayrışırdı
    /// (`lessons.md`: test düzeneği üretim yerleşimini KOPYALAMAZ, İTHAL EDER).
    ///
    /// Amacı iki türlü: (1) oda sisteminin gerçekten kurulduğunu görsel olarak kanıtlar,
    /// (2) Enes yerleşimleri oynamadan yargılayabilir.
    ///
    /// Çalıştır: -executeMethod TekCan.EditorTools.RoomCapture.Run
    /// </summary>
    public static class RoomCapture
    {
        const int W = 1100, H = 700;

        [MenuItem("Tek Can/Oda yerleşimlerini çek")]
        public static void Run()
        {
            var t = AssetDatabase.LoadAssetAtPath<Tuning>("Assets/_Project/Resources/Tuning.asset")
                    ?? ScriptableObject.CreateInstance<Tuning>();

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.48f);

            var outDir = Path.Combine(Directory.GetCurrentDirectory(), "oda_yerlesimleri");
            Directory.CreateDirectory(outDir);

            // 1. bölgenin düşmanlı odaları. ⚠️ Dosya adı ÜRETİMİN kurduğu yerleşimden
            // (`rm.Layout`): önceden döngü sayacından türetiliyordu ve "0_HALKA.png"
            // aslında oda 1'in DUVAR'ıydı — yalanı yakalayan araç yalan söylüyordu.
            int adet = 0;
            for (int room = 1; room <= t.DövüşOdası; room++, adet++) Shoot(t, room, outDir);   // ilk ara bölgenin dövüş odaları

            Debug.Log($"[Tek Can] {adet} oda yerleşimi çekildi: {outDir}");
        }

        static void Shoot(Tuning t, int room, string outDir)
        {
            Enemy.All.Clear();
            var root = new GameObject("room-capture");

            var sun = new GameObject("Sun"); sun.transform.SetParent(root.transform);
            var l = sun.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 1.3f;
            sun.transform.rotation = Quaternion.Euler(55f, t.camYaw - 25f, 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(root.transform);
            ground.transform.localScale = Vector3.one * (t.arenaRadius / 5f) * 2f;
            Tint(ground, new Color(0.16f, 0.16f, 0.19f));

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.transform.SetParent(root.transform);
            Tint(player, new Color(0.9f, 0.9f, 0.95f));

            // ⚠️ ÜRETİM YOLU: odayı RoomManager kuruyor, biz değil.
            var rmGo = new GameObject("rooms"); rmGo.transform.SetParent(root.transform);
            var rm = rmGo.AddComponent<RoomManager>();
            rm.Init(t, player.transform, null);
            while (rm.Room < room) rm.NextRoom();

            var cam = BuildCamera(t, root.transform);
            var tex = Render(cam);

            string ad = $"{room}_{rm.Layout.Name}";
            File.WriteAllBytes(Path.Combine(outDir, ad + ".png"), tex.EncodeToPNG());
            Debug.Log($"  ✅ oda {room} · {rm.Layout.Name} · {Enemy.All.Count} düşman → {ad}.png");

            Object.DestroyImmediate(tex);
            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            Altar.ResetAll();
            Object.DestroyImmediate(root);
            RoomManager.Reset();
        }

        static Camera BuildCamera(Tuning t, Transform parent)
        {
            var camGo = new GameObject("cap-cam"); camGo.transform.SetParent(parent);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = t.camOrthoSize * 1.6f;      // bütün oda sığsın
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.09f);
            camGo.transform.rotation = Quaternion.Euler(t.camPitch, t.camYaw, 0f);
            camGo.transform.position = -camGo.transform.forward * 45f;
            return cam;
        }

        static Texture2D Render(Camera cam)
        {
            var rt = new RenderTexture(W, H, 24) { antiAliasing = 4 };
            // URP: Camera.Render() ışıklandırmayı uygulamıyor, pipeline'a istek göndermek gerek.
            cam.targetTexture = rt;
            cam.SubmitRenderRequest(new RenderPipeline.StandardRequest { destination = rt });

            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            rt.Release();
            Object.DestroyImmediate(rt);
            return tex;
        }

        static void Tint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = c;
        }
    }
}
