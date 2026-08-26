using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 7 — görsel kimlik. Korunan kurallar:
    ///   • ⚠️ Efekt GERÇEKTEN DOĞUYOR MU (çağrı var diye efekt var sayılmaz)
    ///   • Zaman akışı yönü doğru mu (hasat sana, sızıntı ondan)
    ///   • KASA'ya vuruş zerre ÜRETMEZ — vurarak hiçbir şey alamıyorsun, görsel de yalan söylememeli
    ///   • Aura zerresi KISILI (karede bir zerre = saniyede 60 nesne)
    ///
    /// Faz 6 dersinin devamı: "üretildi" yetmez, "vardığı yerde etkisi var mı".
    /// Burada ölçülen şey efektin sahnede DOĞMASI — sayı değil, nesne.
    /// </summary>
    public static class Faz7SelfCheck
    {
        [MenuItem("Tek Can/Faz 7 öz-denetimi çalıştır")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 500f;
            t.enemyTimePool = 10f;
            t.harvestRate = 0.35f;
            t.enemyDecayPerSecond = 0f;      // kendi kendine boşalma testi bulandırmasın

            var root = new GameObject("faz7-check");
            var player = new GameObject("player");
            player.transform.position = Bootstrap.ArenaCenter;
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform);

            UnlitMat.Reset();
            Mote.Reset();
            Arc.Reset();

            // ── 1) ⚠️ MATERYALLER GERÇEK Mİ ──
            //    Materyal null kalırsa Unity pembe render eder ve hiçbir sayı bunu görmez.
            failed += Check(ref total, "alpha materyali kuruldu", UnlitMat.Alpha != null);
            failed += Check(ref total, "additive materyali kuruldu", UnlitMat.Additive != null);
            failed += Check(ref total, "iki materyal AYRI nesne",
                            UnlitMat.Alpha != UnlitMat.Additive);

            // ── 2) ⚠️ HASAT ZERRESİ DOĞUYOR MU ──
            ClearVfx(); ClearEnemies();
            var basic = Make(t, player.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.2f,
                             EnemyKind.Basic);
            int motesBefore = MoteCount();
            basic.TakeDamage(3f);
            int motesAfter = MoteCount();
            failed += Check(ref total, "⚠️ vuruş ZAMAN ZERRESİ doğuruyor", motesAfter > motesBefore,
                            motesAfter - motesBefore);

            // ── 3) İsabet patlaması ──
            ClearVfx(); ClearEnemies();
            var b2 = Make(t, player.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.2f,
                          EnemyKind.Basic);
            int arcsBefore = ArcCount();
            b2.TakeDamage(3f);
            failed += Check(ref total, "vuruş isabet halkası doğuruyor", ArcCount() > arcsBefore,
                            ArcCount() - arcsBefore);

            // ── 4) ⚠️ KASA'ya vuruş zerre ÜRETMEZ ──
            //    Görsel, ekonominin YALANINI söylememeli: KASA'ya vurarak hiçbir şey
            //    alamıyorsun, o yüzden hiçbir şey akmamalı. Akarsa oyuncu kazandığını sanır.
            var vault = FindKind("KASA");
            if (vault != null)
            {
                ClearVfx(); ClearEnemies();
                var v = Make(t, player.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.2f, vault);
                int before = MoteCount();
                v.TakeDamage(3f);
                failed += Check(ref total, "⚠️ KASA'ya vuruş zerre üretmiyor (hasat 0)",
                                MoteCount() == before, MoteCount() - before);
            }

            // ── 5) Ölüm halkası ──
            ClearVfx(); ClearEnemies();
            var dying = Make(t, player.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.2f,
                             EnemyKind.Basic);
            dying.TakeDamage(999f);          // havuzu boşalt
            ClearVfx();                      // isabet efektlerini say(ma)
            dying.TakeDamage(999f);          // boş düşmana vuruş öldürür
            failed += Check(ref total, "ölüm halkası doğuyor", ArcCount() > 0, ArcCount());

            // ── 6) ⚠️ AURA ZERRESİ KISILI MI ──
            //    Etki testi: 1 saniyelik aura, karede bir zerre doğursaydı ~60 nesne olurdu.
            //    Kısma çalışıyorsa 10'un altında kalmalı. Bu, "özellik değil etki" dersinin
            //    doğrudan uygulaması — kısma değişkeni değil, DOĞAN NESNE sayılıyor.
            var leak = FindKind("SIZINTI");
            if (leak != null)
            {
                ClearVfx(); ClearEnemies();
                var l = Make(t, player.transform, Bootstrap.ArenaCenter + Vector3.forward * 2f, leak);
                for (int i = 0; i < 60; i++) l.ApplyAura(1f / 60f);
                int n = MoteCount();
                failed += Check(ref total, $"⚠️ aura zerresi kısılı (1 sn → {n} zerre, 60 değil)",
                                n > 0 && n < 12, n);

                // Yön doğru mu: sızıntı zerresi SENDEN çıkıp ONA gidiyor, tersi değil.
                // Doğuş noktası oyuncuya düşmandan daha yakın olmalı.
                var any = Object.FindFirstObjectByType<Mote>();
                if (any != null)
                {
                    float toPlayer = Vector3.Distance(any.transform.position, player.transform.position);
                    float toEnemy = Vector3.Distance(any.transform.position, l.transform.position);
                    failed += Check(ref total, "⚠️ sızıntı zerresi OYUNCUDAN çıkıyor (yön doğru)",
                                    toPlayer < toEnemy, toPlayer - toEnemy);
                }
            }

            // ── 7) ⚠️ YAY GERÇEKTEN BÜYÜYOR MU ──
            //    "scaleFrom uygulandı" ÖZELLİK; "zamanla büyüdü" ETKİ. Hazırlık işaretinin
            //    "ne zaman" demesi ikincisine bağlı — ilki doğru olup ikincisi bozuk olabilir.
            ClearVfx();
            var grow = Arc.Spawn(Vector3.zero, Quaternion.identity, 1f, 90f,
                                 Color.white, 1f, null, scaleFrom: 0.2f, scaleTo: 1f);
            float s0 = grow.transform.localScale.x;
            grow.Step(0.5f);
            float sMid = grow.transform.localScale.x;
            failed += Check(ref total, "yay küçük başlıyor", Mathf.Abs(s0 - 0.2f) < 0.01f, s0);
            failed += Check(ref total, "⚠️ yay zamanla BÜYÜYOR", sMid > s0 + 0.1f, sMid - s0);

            // ── 8) ⚠️ YAY ÖMRÜ BİTİNCE GİDİYOR MU ──
            //    Gitmezse sahnede birikir. Hiçbir "doğdu mu" testi bunu göremez.
            grow.Step(0.6f);
            failed += Check(ref total, "⚠️ yay ömrü bitince YOK OLUYOR", ArcCount() == 0, ArcCount());

            // ── 9) ⚠️ ZERRE HEDEFE VARIYOR VE YOK OLUYOR MU ──
            //    Kimlik anının tamamı buna bağlı: zerre yola çıkıp yolda kalırsa
            //    "zaman bana aktı" değil "ortalıkta yeşil toplar var" olur.
            ClearVfx();
            var hedef = new GameObject("hedef");
            hedef.transform.position = new Vector3(6f, 0f, 0f);
            var mote = Mote.Spawn(Vector3.zero, hedef.transform, Mote.HarvestColor, life: 0.30f);

            float uzakBaslangic = Vector3.Distance(mote.transform.position, hedef.transform.position);
            mote.Step(0.15f);
            float uzakOrta = Vector3.Distance(mote.transform.position, hedef.transform.position);
            failed += Check(ref total, "⚠️ zerre HEDEFE DOĞRU ilerliyor", uzakOrta < uzakBaslangic,
                            uzakOrta - uzakBaslangic);

            mote.Step(0.20f);
            failed += Check(ref total, "⚠️ zerre varınca YOK OLUYOR (birikmiyor)",
                            MoteCount() == 0, MoteCount());
            Object.DestroyImmediate(hedef);

            // ── temizlik ──
            ClearVfx(); ClearEnemies();
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(t);
            Clock.Reset();
            UnlitMat.Reset(); Mote.Reset(); Arc.Reset();

            if (failed == 0) Debug.Log($"[Faz 7 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 7 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        static int MoteCount() => Object.FindObjectsByType<Mote>(FindObjectsInactive.Include).Length;
        static int ArcCount() => Object.FindObjectsByType<Arc>(FindObjectsInactive.Include).Length;

        static EnemyKind FindKind(string name)
        {
            foreach (var k in EnemyKind.All) if (k.Name == name) return k;
            return null;
        }

        /// <summary>Üretim doğum yolunu kullanır — kendi kopyasını kurmaz (bkz. lessons.md).</summary>
        static Enemy Make(Tuning t, Transform player, Vector3 pos, EnemyKind kind)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = pos;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(t, player, null, 1f, false, kind);
            return e;
        }

        static void ClearVfx()
        {
            foreach (var m in Object.FindObjectsByType<Mote>(FindObjectsInactive.Include))
                Object.DestroyImmediate(m.gameObject);
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include))
                Object.DestroyImmediate(a.gameObject);
        }

        static void ClearEnemies()
        {
            foreach (var e in Enemy.All.ToArray())
                if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
        }

        static int Check(ref int total, string name, bool ok, float actual = 0f)
        {
            total++;
            if (ok) Debug.Log($"  ✅ {name}");
            else Debug.LogError($"  ❌ {name}  (gerçek: {actual})");
            return ok ? 0 : 1;
        }
    }
}
