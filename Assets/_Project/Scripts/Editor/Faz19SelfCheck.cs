using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 19 — SUNAK ODASI. Enes: *"şu sunak ve buff alma kısımları için ayrı bir
    /// oda tasarlayalım."*
    ///
    /// ⚠️ Odanın fikri: **SAATİN DURDUĞU TEK YER** (Enes kararı). Oyunun tamamı
    /// zaman baskısı; baskı altında alışveriş yapmak "karar vermek" değil, panikle
    /// almak ya da hiç almamak olur. Saat durunca alışveriş GERÇEK bir karar oluyor.
    ///
    /// ⚠️ İlk sürümde odayı tam TERSİ tasarlamıştım (en pahalı oda). Enes çevirdi.
    /// Bu denetim o yüzden "bedeli var mı" değil "saat DURUYOR mu" diye soruyor —
    /// ve donmanın odadan ÇIKINCA kalktığını da ölçüyor: kalıcı donma sessiz ve
    /// yıkıcı olurdu, oyun zamansız kalır ve hiçbir şey hata vermezdi.
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

            // ── 6) ⚠️ ODADA SAAT DURUYOR — odanın bütün fikri (Enes kararı) ──
            //    ⚠️ ÖZELLİK değil ETKİ: "Frozen true mu" değil, SAAT GERÇEKTEN
            //    AKMIYOR MU. Bayrağa bakmak, `Tick`'in onu okumayı unutması
            //    ihtimalini test etmezdi.
            goToRoom.Invoke(rm, new object[] { sunakOdaNo });
            float durgunÖnce = clock.Seconds;
            clock.Tick(3f);                              // dt dışarıdan (tuzak 3)
            failed += Check(ref total, "⚠️ sunak odasında SAAT DURUYOR",
                            Mathf.Approximately(clock.Seconds, durgunÖnce),
                            durgunÖnce - clock.Seconds);

            // ── 6b) ⚠️ VE ODADAN ÇIKINCA GERİ AKIYOR ──
            //    Kalıcı donma sessiz ve yıkıcı olurdu: oyun bir anda zamansız
            //    kalır ve hiçbir şey hata vermez.
            goToRoom.Invoke(rm, new object[] { 1 });
            float akanÖnce = clock.Seconds;
            clock.Tick(3f);
            failed += Check(ref total, "⚠️ odadan çıkınca saat GERİ AKIYOR",
                            clock.Seconds < akanÖnce - 0.01f, akanÖnce - clock.Seconds);

            // ── 6c) ⚠️ BOSS ODASINDA da akıyor — sunak boss'un HEMEN ÖNÜNDE ──
            //    Dondurmayı yalnızca "sunaktan çıkarken" kaldırsaydım, sunaktan
            //    sonraki oda boss odası olduğu için boss dövüşü zamansız geçerdi.
            goToRoom.Invoke(rm, new object[] { t.roomsPerRegion });
            float bossÖnce = clock.Seconds;
            clock.Tick(3f);
            failed += Check(ref total, "⚠️ BOSS odasında saat akıyor (donma taşınmıyor)",
                            clock.Seconds < bossÖnce - 0.01f, bossÖnce - clock.Seconds);

            goToRoom.Invoke(rm, new object[] { sunakOdaNo });

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
