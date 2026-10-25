using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 39 — tempo (Enes 2026-10-22: "8-10 saat"). Bot tam koşusu 3.9 saatte bitmiyordu: odalar bölgeden bölgeye kısalıyordu (havuzda
    /// bölge çarpanı yoktu) ve darbe saatten hızlı büyüyordu. Burada iki kural ETKİ olarak: oda havuzu bölgeyle büyür; darbenin saate
    /// (doyma tavanına) oranı bütün koşu boyunca sabit. Koşunun bitip bitmediğini bot ölçer (`TamKosu`).
    /// </summary>
    public static class Faz39SelfCheck
    {
        const BindingFlags Gizli = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tek Can/Faz 39 öz-denetimi çalıştır (tempo)")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;
            var t = ScriptableObject.CreateInstance<Tuning>();
            t.enemyDecayPerSecond = 0f;
            Region.Reset();

            failed += OdaBölgeyle(t, ref total);
            failed += DarbeSaatle(t, ref total);

            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset(); RoomManager.Reset(); RevivalSystem.Reset();
            Debug.Log($"[Faz 39 öz-denetim] {(failed == 0 ? "✅" : "❌")} {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        // ── 1) Aynı oda, aynı tür ve rütbe: 4. bölgede havuz 1.'ninkinin regionRoomGrowth³ katı ──
        static int OdaBölgeyle(Tuning t, ref int total)
        {
            int failed = 0, oda = 3;
            Dictionary<string, float> Havuzlar(int bölge)
            {
                Temizle(null);
                SaveFile.UseInMemory(new SaveData());
                var root = new GameObject("faz39-check");
                var player = new GameObject("player"); player.transform.SetParent(root.transform);
                player.AddComponent<PlayerController>().Init(t, null);
                var clock = root.AddComponent<Clock>();
                clock.Init(t, player.transform, t.startSeconds);
                var rmGo = new GameObject("rooms"); rmGo.transform.SetParent(root.transform);
                var rm = rmGo.AddComponent<RoomManager>();
                rm.Init(t, player.transform, null, startRoom: 1);
                typeof(RoomManager).GetMethod("EnterRegion", Gizli).Invoke(rm, new object[] { bölge, oda });
                var h = new Dictionary<string, float>();
                foreach (var e in Enemy.All)
                    if (e != null && !e.IsBoss) h[$"{e.Kind.Name}/{e.Rank.Name}"] = e.TimeLeft;   // aynı tür + rütbe = aynı çarpan
                Temizle(root);
                return h;
            }
            var b1 = Havuzlar(0); var b4 = Havuzlar(3);
            var ortak = b1.Keys.Intersect(b4.Keys).ToList();
            float beklenen = Mathf.Pow(t.regionRoomGrowth, 3);
            var oranlar = ortak.Select(k => b4[k] / b1[k]).ToList();
            failed += Check(ref total, $"⚠️⚠️ oda havuzu bölgeyle büyür: 3. oda, aynı tür ve rütbe ({string.Join(", ", ortak)}) — 4. bölgede ×{(oranlar.Count > 0 ? oranlar.Average() : 0f):0.000} (regionRoomGrowth³ = {beklenen:0.000})",
                            ortak.Count > 0 && oranlar.All(o => Mathf.Abs(o - beklenen) < 0.01f), oranlar.Count > 0 ? oranlar.Average() : 0f);
            return failed;
        }

        // ── 2) Darbe saatle büyür: her bölgenin başında darbe/tavan oranı 1. bölgeninkiyle aynı; ara bölgelerde tek yönlü artar ──
        static int DarbeSaatle(Tuning t, ref int total)
        {
            int failed = 0, S = Mathf.Max(1, t.subRegionsPerRegion);
            var oranlar = Enumerable.Range(0, Region.All.Length)
                .Select(b => RoomManager.İlerlemeÇarpanı(t, b, 0) / (Clock.Tavan(t, b) / Clock.Tavan(t, 0))).ToList();
            bool artar = Enumerable.Range(0, Region.All.Length).All(b => Enumerable.Range(1, S - 1)
                .All(s => RoomManager.İlerlemeÇarpanı(t, b, s) > RoomManager.İlerlemeÇarpanı(t, b, s - 1)));
            float son = RoomManager.İlerlemeÇarpanı(t, Region.All.Length - 1, S - 1);
            failed += Check(ref total, $"⚠️⚠️ darbe saatle büyür: bölge başlarında darbe ÷ tavan oranı {string.Join(" · ", oranlar.Select(o => o.ToString("0.00")))} (hepsi 1) · ara bölgelerde artıyor ({(artar ? "evet" : "HAYIR")}) · son ara bölgede ×{son:0.0} (tavan ×{Clock.Tavan(t, Region.All.Length - 1) / Clock.Tavan(t, 0):0.0})",
                            oranlar.All(o => Mathf.Abs(o - 1f) < 0.001f) && artar, oranlar.Max());
            return failed;
        }

        // ── kurgu ──────────────────────────────────────────────────────────────
        static void Temizle(GameObject root)
        {
            foreach (var e in Enemy.All.ToArray()) if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include)) Object.DestroyImmediate(a.gameObject);
            foreach (var g in Object.FindObjectsByType<ExitGate>(FindObjectsInactive.Include)) Object.DestroyImmediate(g.gameObject);
            if (root != null) Object.DestroyImmediate(root);
            Clock.Reset(); RoomManager.Reset(); Region.Reset(); RevivalSystem.Reset(); Altar.ResetAll();
        }

        static int Check(ref int total, string name, bool ok, float actual)
        {
            total++;
            if (ok) Debug.Log($"  ✅ {name}");
            else Debug.LogError($"  ❌ {name}  (gerçek: {actual})");
            return ok ? 0 : 1;
        }
    }
}
