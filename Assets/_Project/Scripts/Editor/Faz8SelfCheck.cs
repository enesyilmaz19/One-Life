using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 8 — oda yerleşimleri. Oda zincirinin kendisi `Faz4SelfCheck`'te sınanıyor
    /// (koşu yapısı orası); burada YERLEŞİMLER var.
    ///
    /// Korunan kurallar:
    ///   • ⚠️ Düşman ARENANIN İÇİNDE doğuyor mu (dışarı taşan düşmana ulaşılamaz)
    ///   • ⚠️ Yükseklik doğru mu — bu projede y'nin yatay hesaba karışması ÜÇ kez hata üretti
    ///   • ⚠️ Girişin üstüne düşman konulmuyor mu (odaya girer girmez dayak yemek adil değil)
    ///   • Yerleşimler birbirinden gerçekten farklı mı
    ///   • Deterministik mi — aynı oda hep aynı yerleşimi vermeli
    /// </summary>
    public static class Faz8SelfCheck
    {
        [MenuItem("Tek Can/Faz 8 öz-denetimi çalıştır")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            var t = ScriptableObject.CreateInstance<Tuning>();
            var giris = Bootstrap.Entrance(t);
            var cikis = Bootstrap.Exit(t);

            // ── 1) Giriş ve çıkış zıt kenarlarda ──
            failed += Check(ref total, "giriş ve çıkış zıt taraflarda",
                            Mathf.Sign(giris.z) != Mathf.Sign(cikis.z), giris.z + cikis.z);
            failed += Check(ref total, "giriş arenanın içinde",
                            new Vector2(giris.x, giris.z).magnitude < t.arenaRadius,
                            new Vector2(giris.x, giris.z).magnitude);
            failed += Check(ref total, "çıkış arenanın içinde",
                            new Vector2(cikis.x, cikis.z).magnitude < t.arenaRadius,
                            new Vector2(cikis.x, cikis.z).magnitude);

            // ── 2) Her yerleşim, çeşitli düşman sayılarıyla güvenli mi ──
            foreach (var layout in RoomLayout.All)
            {
                float enUzak = 0f, enYakinGiris = 999f, ySapma = 0f;

                for (int n = 1; n <= 10; n++)
                    for (int i = 0; i < n; i++)
                    {
                        var p = layout.Position(t, i, n);

                        enUzak = Mathf.Max(enUzak, new Vector2(p.x, p.z).magnitude);
                        ySapma = Mathf.Max(ySapma, Mathf.Abs(p.y - Bootstrap.ArenaCenter.y));

                        // ⚠️ YATAY mesafe — y'yi katarsak yine aynı hataya düşeriz.
                        var d = new Vector2(p.x - giris.x, p.z - giris.z).magnitude;
                        enYakinGiris = Mathf.Min(enYakinGiris, d);
                    }

                failed += Check(ref total, $"{layout.Name}: düşmanlar arenanın İÇİNDE",
                                enUzak < t.arenaRadius, enUzak);

                // Girişte doğan düşman = odaya adım atar atmaz dayak. Kalıcı ölümde kabul edilemez.
                failed += Check(ref total, $"{layout.Name}: girişin üstüne düşman KONULMUYOR",
                                enYakinGiris > t.enemyAttackRange * 1.5f, enYakinGiris);

                // ⚠️ Bu projede y'nin yatay hesaba karışması üç kez hata üretti
                //    (havada doğan düşmanlar, kaçış mesafesi, ...). Dördüncüyü burada kesiyoruz.
                failed += Check(ref total, $"{layout.Name}: yükseklik doğru (y sapması yok)",
                                ySapma < 0.001f, ySapma);
            }

            // ── 3) Yerleşimler birbirinden farklı ──
            //    Hepsi aynı şekli veriyorsa "5 şablon var" demek yanıltıcı olur.
            int ayni = 0;
            for (int a = 0; a < RoomLayout.All.Length; a++)
                for (int b = a + 1; b < RoomLayout.All.Length; b++)
                    if (Ayni(RoomLayout.All[a], RoomLayout.All[b], t)) ayni++;
            failed += Check(ref total, "yerleşimler birbirinin aynısı değil", ayni == 0, ayni);

            // ── 4) ⚠️ DETERMİNİSTİK Mİ ──
            //    "Bu oda mı zor, ben mi kötü oynadım" sorusu ancak böyle cevaplanabilir.
            bool sabit = true;
            foreach (var layout in RoomLayout.All)
                for (int i = 0; i < 6; i++)
                    if (layout.Position(t, i, 6) != layout.Position(t, i, 6)) sabit = false;
            failed += Check(ref total, "⚠️ yerleşim DETERMİNİSTİK", sabit, 1);

            // ── 5) Oda numarası yerleşime eşleniyor ve dönüyor ──
            failed += Check(ref total, "oda numarası yerleşim veriyor",
                            RoomLayout.For(0) != null && RoomLayout.For(7) != null, 1);
            failed += Check(ref total, "yerleşimler sırayla dönüyor",
                            RoomLayout.For(0) == RoomLayout.For(RoomLayout.All.Length), 1);

            Object.DestroyImmediate(t);

            if (failed == 0) Debug.Log($"[Faz 8 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 8 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        static bool Ayni(RoomLayout a, RoomLayout b, Tuning t)
        {
            for (int i = 0; i < 6; i++)
                if (Vector3.Distance(a.Position(t, i, 6), b.Position(t, i, 6)) > 0.01f) return false;
            return true;
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
