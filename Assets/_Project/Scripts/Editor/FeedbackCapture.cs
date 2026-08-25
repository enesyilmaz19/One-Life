using System.IO;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Geri bildirim katmanının GÖRÜNDÜĞÜNÜ kanıtlar — iddia etmez, resmini çeker.
    ///
    /// Faz 1'in ilk teslimi tam da bu adım atlandığı için çöktü: sistemler doğruydu,
    /// ekranda hiçbir şey yoktu, ve ben "hazır" dedim. Görsel iş görülmeden teslim edilmez.
    ///
    /// Çalıştır (-nographics OLMADAN, render için ekran kartı gerekiyor):
    ///   Unity.exe -batchmode -quit -executeMethod TekCan.EditorTools.FeedbackCapture.Run
    ///
    /// ⚠️ BATCHMODE SINIRI: URP'nin Lit gölgelendirmesi burada tam çalışmıyor — gövdeler
    /// düz nötr çıkıyor. Yani bu araç YAY GEOMETRİSİNİ ve Unlit renkleri kanıtlar,
    /// gövde durum renklerini (kırmızı/turuncu/beyaz/gri) kanıtlamaz. Onlar editörde
    /// Play'e basılarak doğrulanır. `material.color` URP'de `_BaseColor`'a yazıyor
    /// (ölçüldü), yani veri doğru; eksik olan yalnızca headless gölgelendirme.
    ///
    /// Tasarım güvencesi: durumlar renge TEK BAŞINA bağlı değil — hazırlık turuncu
    /// koniyle, dolu/boş ayrımı düşman üstündeki sayı/"—" ile de okunuyor.
    /// </summary>
    public static class FeedbackCapture
    {
        [MenuItem("Tek Can/Geri bildirim ekran görüntüsü al")]
        public static void Run()
        {
            var t = AssetDatabase.LoadAssetAtPath<Tuning>("Assets/_Project/Resources/Tuning.asset")
                    ?? ScriptableObject.CreateInstance<Tuning>();

            var root = new GameObject("capture-root");

            // Işık — Bootstrap ile aynı ayar, yoksa ekran görüntüsü oyunu temsil etmez
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.48f);
            var sun = new GameObject("Sun"); sun.transform.SetParent(root.transform);
            var l = sun.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 1.3f;
            sun.transform.rotation = Quaternion.Euler(55f, t.camYaw - 25f, 0f);

            // Zemin
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(root.transform);
            ground.transform.localScale = Vector3.one * 4f;
            Tint(ground, new Color(0.16f, 0.16f, 0.19f));

            // Oyuncu + ÖNÜNDE SAVURMA YAYI (ıska durumu — yay yine de görünmeli)
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.transform.SetParent(root.transform);
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            Tint(player, new Color(0.9f, 0.9f, 0.95f));

            Arc.Spawn(player.transform.position, player.transform.rotation,
                t.attackRange, t.attackAngle, new Color(0.65f, 0.9f, 1f, 0.42f), 999f);

            // Düşman + HAZIRLIK İŞARETİ (turuncu koni = "buraya vuracağım")
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.transform.SetParent(root.transform);
            enemy.transform.position = new Vector3(3.4f, 1f, 2.2f);
            enemy.transform.rotation = Quaternion.LookRotation(player.transform.position - enemy.transform.position);
            Tint(enemy, new Color(1f, 0.75f, 0.2f));      // hazırlık rengi

            Arc.Spawn(enemy.transform.position, enemy.transform.rotation,
                t.enemyAttackRange, t.enemyAttackAngle, new Color(1f, 0.55f, 0.15f, 0.30f), 999f);

            // Dolu düşman (kırmızı) — karşılaştırma için
            var full = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            full.transform.SetParent(root.transform);
            full.transform.position = new Vector3(-3.2f, 1f, 1.5f);
            Tint(full, new Color(0.85f, 0.25f, 0.25f));

            // Boş düşman (gri) — durum farkı okunuyor mu
            var hollow = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hollow.transform.SetParent(root.transform);
            hollow.transform.position = new Vector3(-1.5f, 1f, -3f);
            Tint(hollow, new Color(0.35f, 0.35f, 0.38f));

            // Faz 2 sunakları — üç tür, renkle ayrışıyor mu
            var altarColors = new[]
            {
                new Color(0.35f, 0.85f, 0.55f),   // HASAT
                new Color(0.9f, 0.55f, 0.35f),    // KESKİNLİK
                new Color(0.45f, 0.65f, 1f),      // ÇEVİKLİK
            };
            for (int i = 0; i < 3; i++)
            {
                var alt = GameObject.CreatePrimitive(PrimitiveType.Cube);
                alt.transform.SetParent(root.transform);
                float ang = (360f / 3f) * i * Mathf.Deg2Rad + Mathf.PI * 0.25f;
                alt.transform.position = new Vector3(Mathf.Cos(ang) * 3.9f, 1.2f, Mathf.Sin(ang) * 3.9f);
                alt.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);
                alt.transform.rotation = Quaternion.Euler(0f, 25f * i, 0f);
                Tint(alt, altarColors[i]);
            }

            // Faz 3: dört silahın yayı yan yana — şekilleri gerçekten ayrışıyor mu?
            // (KONİ geniş, tek hedefliler dar; DURGUN uzun namlulu)
            for (int i = 0; i < Weapon.All.Length; i++)
            {
                var w = Weapon.All[i];
                var origin = new Vector3(-6f + i * 3.2f, 1f, -6.5f);
                Arc.Spawn(origin, Quaternion.Euler(0f, -35f, 0f),
                          w.Range(t), w.Angle(t), w.Tint, 999f);
            }

            // İzometrik kamera
            var camGo = new GameObject("cap-cam"); camGo.transform.SetParent(root.transform);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = t.camOrthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.09f);
            camGo.transform.rotation = Quaternion.Euler(t.camPitch, t.camYaw, 0f);
            camGo.transform.position = -camGo.transform.forward * 40f;

            // Render → PNG
            const int W = 1280, H = 720;
            var rt = new RenderTexture(W, H, 24) { antiAliasing = 4 };

            // URP'de Camera.Render() desteklenmiyor — SRP devrede olduğu için ışıklandırma
            // uygulanmadan çiziyor (Unlit yaylar çıkıyor, Lit gövdeler düz gri kalıyor).
            // Doğru yol: pipeline'a render isteği göndermek.
            // targetTexture'ı ÖNCE ata: yoksa cam.pixelWidth/Height varsayılan kalır ve
            // WorldToScreenPoint yanlış koordinat verir (örnekleme bunu yakaladı).
            cam.targetTexture = rt;
            cam.SubmitRenderRequest(new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = rt });
            Debug.Log($"KAMERA piksel boyutu: {cam.pixelWidth}x{cam.pixelHeight}");

            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            // Projeksiyon matematiğine güvenme — görüntünün kendisine sor.
            // Renk geri bildirimi çalışıyorsa kırmızımsı/turuncumsu piksel VAR olmalı.
            Histogram(tex, W, H);

            var outPath = Path.Combine(Directory.GetCurrentDirectory(), "faz1_geribildirim.png");
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Debug.Log("[Tek Can] Ekran görüntüsü yazıldı: " + outPath);

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include))
                Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(root);
        }

        static void Histogram(Texture2D tex, int w, int h)
        {
            var px = tex.GetPixels();
            int reddish = 0, orangish = 0, bright = 0, neutral = 0;

            foreach (var p in px)
            {
                float mx = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
                float mn = Mathf.Min(p.r, Mathf.Min(p.g, p.b));
                float sat = mx > 0.001f ? (mx - mn) / mx : 0f;

                if (sat < 0.12f) { neutral++; if (mx > 0.75f) bright++; continue; }
                if (p.r > p.g && p.g > p.b) { if (p.g > 0.45f) orangish++; else reddish++; }
            }

            int total = px.Length;
            Debug.Log($"HISTOGRAM toplam={total} | notr(%{100f * neutral / total:0.0}) " +
                      $"parlak(%{100f * bright / total:0.00}) " +
                      $"kirmizimsi(%{100f * reddish / total:0.00}) " +
                      $"turuncumsu(%{100f * orangish / total:0.00})");
            Debug.Log(reddish + orangish > total / 2000
                ? "SONUC: RENK GERI BILDIRIMI GORUNUYOR"
                : "SONUC: RENK YOK — gövdeler tint almiyor, render sorunu");
        }

        static void Tint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) { Debug.LogError($"TINT: {go.name} renderer YOK"); return; }

            var m = new Material(r.sharedMaterial);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            r.sharedMaterial = m;

            var chk = r.sharedMaterial;
            Debug.Log($"TINT {go.name}: istenen={c} | shader={chk.shader.name} | " +
                      $"_BaseColor={(chk.HasProperty("_BaseColor") ? chk.GetColor("_BaseColor").ToString() : "yok")}");
        }
    }
}
