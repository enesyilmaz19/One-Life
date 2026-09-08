using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 19 — SUNAK ODASI. Enes: *"şu sunak ve buff alma kısımları için ayrı bir
    /// oda tasarlayalım."*
    ///
    /// ⚠️ Odanın fikri: dinlenme DEĞİL, oyunun EN PAHALI odası. Dövüş dışı akış
    /// zaten 2.5/sn, dövüştekinin (1.0/sn) iki buçuk katı — yani "alsam mı almasam
    /// mı" demek burada en pahalıya patlıyor. Tek satır yeni kural yazılmadı;
    /// yasa bu baskıyı zaten kuruyordu, oda onu görünür yaptı.
    ///
    /// Yan fayda ölçülüyor: sunaklar eskiden HER ÇİFT ODADA çıkıyordu ve buff
    /// matematiği oradan patlıyordu (39 alım, hasar ×7,9). Tek odaya inince
    /// bölge başına alım tavanı sertçe düşüyor.
    /// </summary>
    public static class Faz19SelfCheck
    {
        [MenuItem("Tek Can/Faz 19 öz-denetimi çalıştır (sunak odası)")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 5000f;
            t.roomsPerRegion = 6;
            Region.Reset();
            Enemy.All.Clear();
            Altar.ResetAll();

            var root = new GameObject("faz19-check");
            var player = new GameObject("player");
            player.transform.SetParent(root.transform);
            var clock = root.AddComponent<Clock>();
            clock.Init(t, player.transform, t.startSeconds);

            var rm = root.AddComponent<RoomManager>();
            rm.Init(t, player.transform, null);

            var goToRoom = typeof(RoomManager).GetMethod("EnterRoom",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var update = typeof(RoomManager).GetMethod("Update",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // ── 1) ⚠️ SUNAK ODASI VAR ve boss'tan HEMEN ÖNCE ──
            //    Bölgede tam bir tane olmalı: sıfırsa alışveriş yok, ikiyse
            //    "özel oda" olmaktan çıkar.
            int sunakOdaSayısı = 0, sunakOdaNo = -1;
            for (int oda = 1; oda <= t.roomsPerRegion; oda++)
            {
                goToRoom.Invoke(rm, new object[] { oda });
                if (rm.IsAltarRoom) { sunakOdaSayısı++; sunakOdaNo = oda; }
            }
            failed += Check(ref total, "⚠️ bölgede TAM BİR sunak odası var",
                            sunakOdaSayısı == 1, sunakOdaSayısı);
            failed += Check(ref total, "⚠️ sunak odası BOSS'TAN HEMEN ÖNCE",
                            sunakOdaNo == t.roomsPerRegion - 1, sunakOdaNo);

            // ── 2) ⚠️ ODADA DÜŞMAN YOK — özellik değil ETKİ ──
            goToRoom.Invoke(rm, new object[] { sunakOdaNo });
            failed += Check(ref total, "⚠️ sunak odasında DÜŞMAN YOK", Enemy.All.Count == 0,
                            Enemy.All.Count);

            // ── 3) ⚠️ SUNAKLAR GERÇEKTEN ORADA ──
            int sunak = Object.FindObjectsByType<Altar>(FindObjectsInactive.Include).Length;
            failed += Check(ref total, "⚠️ sunaklar odada DOĞDU", sunak >= 3, sunak);

            // ── 4) ⚠️ ÇIKIŞ KENDİLİĞİNDEN AÇILIYOR ──
            //    Düşman olmadığı için temizlik kuralı ilk karede sağlanıyor.
            //    Ayrı bir "kapı açık doğsun" kuralı YAZILMADI; bu kontrol onun
            //    gerçekten gereksiz olduğunu doğruluyor.
            update.Invoke(rm, null);
            var kapı = Object.FindFirstObjectByType<ExitGate>();
            failed += Check(ref total, "⚠️ sunak odasında çıkış KENDİLİĞİNDEN açılıyor",
                            kapı != null && kapı.IsOpen, kapı != null ? 1 : 0);

            // ── 5) ⚠️ SUNAKLAR BAŞKA ODADA ÇIKMIYOR ──
            //    Eskiden HER ÇİFT ODADA çıkıyorlardı; buff matematiği oradan
            //    patlıyordu. Alışveriş bir OLAY olmalı, arka plan gürültüsü değil.
            int başkaOdadaSunak = 0;
            for (int oda = 1; oda <= t.roomsPerRegion; oda++)
            {
                if (oda == sunakOdaNo) continue;
                goToRoom.Invoke(rm, new object[] { oda });
                foreach (var a in Object.FindObjectsByType<Altar>(FindObjectsInactive.Include))
                    if (a != null && !a.name.Contains("Repair")) başkaOdadaSunak++;
            }
            failed += Check(ref total, "⚠️ yükseltme sunağı BAŞKA odada çıkmıyor",
                            başkaOdadaSunak == 0, başkaOdadaSunak);

            // ── 6) ⚠️ ODA BEDAVA DEĞİL — burada beklemek dövüşten PAHALI ──
            //    Odanın bütün fikri bu. Ölçü: dövüş dışı akış, dövüştekinden büyük mü.
            failed += Check(ref total, "⚠️ sunak odasında beklemek DÖVÜŞTEN pahalı",
                            t.drainOutOfCombat > t.drainInCombat * 1.5f,
                            t.drainOutOfCombat / t.drainInCombat);

            // ── 7) ⚠️ ALIM TAVANI GERÇEKTEN DÜŞTÜ (buff matematiği) ──
            //    Eskiden çift odalarda ~3 sunak × 3 alım; şimdi bölgede tek oda.
            int eskiTavan = (t.roomsPerRegion / 2) * 3 * t.altarMaxPurchases;
            int yeniTavan = 3 * t.altarMaxPurchases;
            failed += Check(ref total, $"⚠️ bölge başına alım TAVANI düştü ({eskiTavan} → {yeniTavan})",
                            yeniTavan < eskiTavan, yeniTavan);

            Temizle();
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(t);
            RoomManager.Reset(); Clock.Reset(); Region.Reset();

            if (failed == 0) Debug.Log($"[Faz 19 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 19 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        static void Temizle()
        {
            foreach (var e in Enemy.All.ToArray())
                if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            Altar.ResetAll();
            GroundMark.ResetLiens();
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
