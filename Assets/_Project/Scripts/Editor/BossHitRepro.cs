using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// "Bossa bazen vuramıyorum" şikâyetinin reprodüksiyonu.
    ///
    /// Enes bunu İKİ KEZ bildirdi. İlkinde menzil düzeltildi (merkeze değil YÜZEYE
    /// ölçülür oldu) ama şikâyet geri geldi — yani kök neden tam bulunmamıştı.
    /// `lessons.md` kuralı: iki kez yanlış kök-neden tahmininden sonra REPRO yaz.
    ///
    /// ⚠️ Ölçüm BAĞIMSIZ: üretimin formülünü tekrar çalıştırmıyor. Boss'un gövde
    /// çemberi üzerinde noktalar örnekleniyor; bir nokta hem menzilde hem koninin
    /// içindeyse gövde yayla ÖRTÜŞÜYOR demektir ve vuruşun inmesi ZORUNLUDUR.
    /// Üretim formülünü ikinci kez yazsaydık aynı hatayı iki yerde yapar ve test
    /// yeşil yanardı.
    /// </summary>
    public static class BossHitRepro
    {
        [MenuItem("Tek Can/Boss isabet reprodüksiyonu")]
        public static void Run()
        {
            bool ok = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>Gövde yayla örtüşen HER durumda vuruş iniyorsa true.</summary>
        public static bool Execute()
        {
            var t = AssetDatabase.LoadAssetAtPath<Tuning>("Assets/_Project/Resources/Tuning.asset")
                    ?? ScriptableObject.CreateInstance<Tuning>();

            Enemy.All.Clear();
            var root = new GameObject("boss-hit-repro");

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.transform.SetParent(root.transform);
            player.transform.position = Bootstrap.ArenaCenter;

            var clockGo = new GameObject("clock"); clockGo.transform.SetParent(root.transform);
            var clock = clockGo.AddComponent<Clock>();
            clock.Init(t, player.transform);

            var pc = player.AddComponent<PlayerController>();
            pc.Init(t, null);

            var inHitCone = typeof(PlayerController).GetMethod("InHitCone",
                BindingFlags.NonPublic | BindingFlags.Instance);

            float range = t.attackRange;
            float half = t.attackAngle * 0.5f;

            int kayipToplam = 0, denemeToplam = 0, yanlisIsabet = 0;
            float enKotuAci = 0f;

            // Boss'u çeşitli mesafelere koy, oyuncuyu etrafında döndür.
            foreach (float mesafe in new[] { 1.2f, 1.6f, 2.0f, 2.5f, 3.0f })
            {
                var boss = MakeBoss(t, player.transform, root.transform, mesafe);
                float r = boss.Radius;
                int kayip = 0;
                float ilkKayipAci = -1f;

                for (int derece = 0; derece <= 90; derece++)
                {
                    player.transform.rotation = Quaternion.Euler(0f, derece, 0f);
                    denemeToplam++;

                    bool gercekteOrtusuyor = GovdeYaylaOrtusuyorMu(
                        player.transform, boss.transform.position, r, range, half);

                    // ⚠️ Son eleman `minRange` (HACİZ'in ölü bölgesi) — burada 0 olmalı,
                    // bu reprodüksiyon ölçülen şey MENZİL/AÇI geometrisi. Yansımayla
                    // çağırdığımız için isteğe bağlı parametre de ELLE verilmek zorunda.
                    var args = new object[] { boss, range, half, 0f, 0f };
                    bool oyunVuruyor = (bool)inHitCone.Invoke(pc, args);

                    if (gercekteOrtusuyor && !oyunVuruyor)
                    {
                        kayip++;
                        if (ilkKayipAci < 0f) ilkKayipAci = derece;
                        enKotuAci = Mathf.Max(enKotuAci, derece);
                    }

                    // ⚠️ TERS YÖN de sınanmalı: yoksa `return true;` yazmak testi geçerdi.
                    // Gövde yaydan AÇIKÇA uzaktayken (cömert pay verilmiş hâliyle bile
                    // örtüşmüyorken) vuruş inmemeli.
                    bool acikcaDisarida = !GovdeYaylaOrtusuyorMu(
                        player.transform, boss.transform.position, r, range * 1.3f, half + 10f);
                    if (acikcaDisarida && oyunVuruyor)
                    {
                        yanlisIsabet++;
                        Debug.LogError($"[BOSS-REPRO] ❌ {derece}° — gövde yayın AÇIKÇA DIŞINDA " +
                                       $"ama vuruş iniyor (mesafe {mesafe:0.0})");
                    }
                }

                kayipToplam += kayip;
                Debug.Log($"[BOSS-REPRO] mesafe {mesafe:0.0} (yarıçap {r:0.00}) → " +
                          (kayip == 0
                              ? "kayıp yok"
                              : $"❌ {kayip}° KAYIP, ilk kayıp {ilkKayipAci:0}° açıda"));

                Object.DestroyImmediate(boss.gameObject);
                Enemy.All.Clear();
            }

            bool ok = kayipToplam == 0 && yanlisIsabet == 0;
            if (kayipToplam > 0)
                Debug.LogError($"[BOSS-REPRO] ❌ {kayipToplam} durumda GÖVDE YAYIN İÇİNDE AMA " +
                               $"VURUŞ İNMİYOR (en kötü {enKotuAci:0}°) — görsel yalan söylüyor");
            if (yanlisIsabet > 0)
                Debug.LogError($"[BOSS-REPRO] ❌ {yanlisIsabet} durumda yay DIŞINDAKİ gövdeye " +
                               "vuruluyor — pay fazla verilmiş");
            if (ok)
                Debug.Log($"[BOSS-REPRO] ✅ {denemeToplam} durum: örtüşenlerin hepsinde vuruş iniyor, " +
                          "açıkça dışarıdakilerin hiçbirine inmiyor");

            Object.DestroyImmediate(root);
            Enemy.All.Clear();
            return ok;
        }

        /// <summary>
        /// BAĞIMSIZ geometri: gövde çemberinden nokta örnekleyip yayın içinde olan
        /// var mı diye bakar. Üretimin formülünü kullanmaz — kasıtlı.
        /// </summary>
        static bool GovdeYaylaOrtusuyorMu(Transform oyuncu, Vector3 merkez, float yaricap,
                                          float menzil, float yarimAci)
        {
            for (int i = 0; i < 360; i++)
            {
                float a = i * Mathf.Deg2Rad;
                var nokta = merkez + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * yaricap;

                var to = nokta - oyuncu.position;
                to.y = 0f;

                if (to.magnitude > menzil) continue;
                if (Vector3.Angle(oyuncu.forward, to) <= yarimAci) return true;
            }
            return false;
        }

        static Enemy MakeBoss(Tuning t, Transform player, Transform parent, float mesafe)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Boss";
            go.transform.SetParent(parent);
            go.transform.localScale = Bootstrap.EnemyScale * 1.9f;      // Bootstrap ile aynı
            go.transform.position = player.position + new Vector3(0f, 0f, mesafe);
            var e = go.AddComponent<Enemy>();
            e.Init(t, player, null, t.bossPoolMultiplier, true, null);
            return e;
        }
    }
}
