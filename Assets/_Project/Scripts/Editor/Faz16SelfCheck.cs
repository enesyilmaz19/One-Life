using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TekCan.EditorTools
{
    /// <summary>
    /// Faz 16 — sağ tık özel saldırıları + silaha özel dash kimlikleri.
    ///
    /// Enes: *"her bir silahta sağ klik yapınca ayrı bir saldırı daha yapsın,
    /// birkaç saniyede bir kullanılabilen (silahtan silaha süresi değişir)"* ve
    /// *"her bir silahın özel dash attack olsun, burada çok belli olmuyor neyin
    /// ne yaptığı."*
    ///
    /// ⚠️ TASARIM KURALI ve bu denetimin kalbi: özel saldırı silahın ZATEN
    /// SORDUĞU SORUYU güçlendirir, yeni bir soru AÇMAZ. Açsaydı beş silah on
    /// silah gibi davranırdı — TAKSİT'in elenme sebebinin aynısı.
    /// </summary>
    public static class Faz16SelfCheck
    {
        [MenuItem("Tek Can/Faz 16 öz-denetimi çalıştır (özel saldırılar)")]
        public static void Run()
        {
            int failures = Execute();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int Execute()
        {
            int failed = 0, total = 0;

            var t = ScriptableObject.CreateInstance<Tuning>();
            t.startSeconds = 400f;
            t.enemyTimePool = 200f;      // tek vuruşta boşalmasın, hasar ölçülebilsin
            t.attackDamage = 4f;
            t.enemyDecayPerSecond = 0f;
            Region.Reset();

            var root = new GameObject("faz16-check");
            var clock = root.AddComponent<Clock>();
            clock.Init(t, root.transform);

            var pGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pGo.transform.position = Bootstrap.ArenaCenter;
            pGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var pc = pGo.AddComponent<PlayerController>();
            pc.Init(t, null);

            var startSpecial = typeof(PlayerController).GetMethod("StartSpecial",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var dashStrike = typeof(PlayerController).GetMethod("DashStrike",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // ── 1) ⚠️ HER SİLAHIN özel saldırısı VAR (ders 11: tanımlı ≠ var) ──
            int eksik = 0;
            foreach (var w in Weapon.All) if (w.Special(t) == null) eksik++;
            failed += Check(ref total, "⚠️ HER SİLAHIN özel saldırısı var", eksik == 0, eksik);

            // ── 2) ⚠️ BEKLEME SÜRELERİ FARKLI (Enes: silahtan silaha değişir) ──
            //    Aynı olsalardı özel saldırı silahın değil OYUNCUNUN ritmi olurdu.
            var süreler = new System.Collections.Generic.HashSet<float>();
            foreach (var w in Weapon.All) süreler.Add(w.Special(t).Cooldown);
            failed += Check(ref total, "⚠️ bekleme süreleri silahtan silaha FARKLI",
                            süreler.Count == Weapon.All.Length, süreler.Count);

            // ── 3) ⚠️ ÖZELLER FARKLI SORU SORUYOR — silah ölçüsünün aynısı ──
            int aynı = 0;
            for (int a = 0; a < Weapon.All.Length; a++)
                for (int b = a + 1; b < Weapon.All.Length; b++)
                    if (Soru(Weapon.All[a].Special(t)) == Soru(Weapon.All[b].Special(t)))
                    {
                        aynı++;
                        Debug.LogError($"     aynı özel: {Weapon.All[a].Name} = {Weapon.All[b].Name}");
                    }
            failed += Check(ref total, "⚠️ özel saldırılar FARKLI SORU soruyor", aynı == 0, aynı);

            // ── 4) ⚠️ KONİ · SÜPÜRME: ARKANDAKİNE de vuruyor (halka ETKİSİ) ──
            pc.SetWeapon(Index<ConeBlade>());
            ClearEnemies();
            var arkadaki = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter - Vector3.forward * 1.6f);
            float arkaÖnce = arkadaki.TimeLeft;
            startSpecial.Invoke(pc, new object[] { Vector3.forward });
            failed += Check(ref total, "⚠️ SÜPÜRME ARKANDAKİNE de vuruyor (halka)",
                            arkadaki.TimeLeft < arkaÖnce - 0.001f, arkaÖnce - arkadaki.TimeLeft);

            // Normal savurma arkadakine VURMUYOR — özel gerçekten farklı bir şey
            ClearEnemies();
            ResetSpecial(pc);
            var arka2 = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter - Vector3.forward * 1.6f);
            float arka2Önce = arka2.TimeLeft;
            typeof(PlayerController).GetMethod("Strike", BindingFlags.NonPublic | BindingFlags.Instance)
                                    .Invoke(pc, null);
            failed += Check(ref total, "normal savurma arkadakine vurmuyor (özel gerçekten farklı)",
                            Mathf.Approximately(arka2.TimeLeft, arka2Önce), arka2Önce - arka2.TimeLeft);

            // ── 5) ⚠️ BEKLEME SÜRESİ İŞLİYOR — art arda basılamıyor ──
            ClearEnemies();
            ResetSpecial(pc);
            var hedef = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.4f);
            startSpecial.Invoke(pc, new object[] { Vector3.forward });
            failed += Check(ref total, "⚠️ özel kullanınca BEKLEME başlıyor",
                            pc.SpecialCooldownLeft > 0.1f, pc.SpecialCooldownLeft);

            // ── 5b) ⚠️ BEKLEME SÜRESİ SİLAHA AİT, ORTAK DEĞİL ──
            //    Enes bunu oyunda yakaladı: *"her bir silahın ayrı cd si olacak,
            //    ortak olmuş sanırım."* Haklıydı — tek bir `float` vardı, yani bir
            //    silahın özelini kullanmak ÖTEKİLERİNKİNİ de kilitliyordu. Kombo
            //    oynanışını doğrudan öldüren bir hataydı ve HACİZ'in özelinin
            //    "gözükmemesi" de buydu: başka silahın beklemesi onu yutuyordu.
            //    ⚠️ ÖZELLİK değil ETKİ: alan tipine bakmıyor, BİR SİLAHI KULLANIP
            //    ÖTEKİNİN hazır kaldığını ölçüyor.
            ResetSpecial(pc);
            ClearEnemies();
            pc.SetWeapon(Index<ConeBlade>());
            startSpecial.Invoke(pc, new object[] { Vector3.forward });
            float koniBekleme = pc.SpecialCooldownLeft;

            pc.SetWeapon(Index<Iflas>());
            failed += Check(ref total, "⚠️ bekleme SİLAHA AİT — biri kullanılınca öteki HAZIR",
                            pc.SpecialCooldownLeft <= 0.01f, pc.SpecialCooldownLeft);

            pc.SetWeapon(Index<ConeBlade>());
            failed += Check(ref total, "geri dönünce KONİ hâlâ beklemede (sayaç kaybolmuyor)",
                            pc.SpecialCooldownLeft > koniBekleme - 0.1f, pc.SpecialCooldownLeft);

            // ── 6) ⚠️ FAİZLİ · İCRA: ISKALAYINCA ÖDÜYORSUN ──
            //    ⚠️ İlk yazışımda hedef VARKEN net saati ölçüyordum ve test düştü:
            //    bedel ödendi ama hasat onu fazlasıyla örtüyordu (bahis kazanılınca
            //    kârlı olması ZATEN doğru tasarım). Yani ölçüm yanlıştı, silah değil.
            //    Peşin bedelin ölçüsü ISKA: hedefsiz savur, saat düşmeli.
            //    Bu aynı zamanda silahın kimliği — "tutar mı" sorusunun bedeli.
            pc.SetWeapon(Index<Faizli>());
            ResetSpecial(pc);
            ClearEnemies();
            float saatÖnce = clock.Seconds;
            startSpecial.Invoke(pc, new object[] { Vector3.forward });
            float icraBedeli = saatÖnce - clock.Seconds;
            failed += Check(ref total, "⚠️ İCRA ISKALAYINCA da ödüyor (bahis gerçek)",
                            icraBedeli > 0.5f, icraBedeli);
            failed += Check(ref total, "İCRA'nın bedeli normal savurmadan AĞIR",
                            icraBedeli > t.faizliSwingCost * 2f, icraBedeli / t.faizliSwingCost);

            // Ve TUTARSA kârlı — bahsi kazanmanın karşılığı olmalı
            ResetSpecial(pc);
            ClearEnemies();
            MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.4f);
            float tutanÖnce = clock.Seconds;
            startSpecial.Invoke(pc, new object[] { Vector3.forward });
            failed += Check(ref total, "⚠️ İCRA TUTARSA kârlı (bahis kazanıldı)",
                            clock.Seconds > tutanÖnce, clock.Seconds - tutanÖnce);

            // ── 7) ⚠️ HACİZ · UZUN VADE: UZAKTAKİNE DAHA ÇOK vuruyor ──
            //    Silahın sorusunu ("yaklaşmalı mıyım") tersine çeviriyor. ⚠️ ÖZELLİK
            //    değil ETKİ: "DamagePerDistance > 0 mu" değil, iki mesafede inen
            //    hasar farklı mı.
            pc.SetWeapon(Index<Haciz>());
            float yakınHasar = HacizHasari(t, pc, pGo, startSpecial, 4f);
            float uzakHasar = HacizHasari(t, pc, pGo, startSpecial, 9f);
            failed += Check(ref total, "⚠️ UZUN VADE uzaktakine DAHA ÇOK vuruyor",
                            uzakHasar > yakınHasar * 1.3f, uzakHasar / Mathf.Max(0.001f, yakınHasar));

            // ── 8) ⚠️ İPOTEK · HACİZ İHBARI: daha GENİŞ ve daha GEÇ ──
            var ipotek = System.Array.Find(Weapon.All, w => w is Ipotek);
            var ihbar = ipotek.Special(t);
            failed += Check(ref total, "⚠️ HACİZ İHBARI normalden GENİŞ",
                            ihbar.GroundRadius > ipotek.GroundMarkRadius(t) * 1.5f, ihbar.GroundRadius);
            failed += Check(ref total, "⚠️ HACİZ İHBARI daha GEÇ patlıyor (ufuk uzuyor)",
                            ihbar.GroundDelay > ipotek.GroundMarkDelay(t) * 1.5f, ihbar.GroundDelay);

            // ── 9) ⚠️ İFLAS · TASFİYE: SERİ VURUŞ ──
            //    Enes: *"kendisi hızlı saldıran silahımız, sağ tıkı da üst üste
            //    çok hızlı saldırılar yapmasını sağlasın."*
            //
            //    ⚠️ İlk yazdığımda İFLAS'a da AĞIR TEK VURUŞ vermiştim — silahın ne
            //    olduğuna değil "özel saldırı böyle olur"a bakmışım. Özel, silahın
            //    KENDİ karakterinin abartılmışı olmalı; onu tersine çeviren değil.
            var iflasW = System.Array.Find(Weapon.All, w => w is Iflas);
            failed += Check(ref total, "⚠️ TASFİYE SERİ vuruş (silahın kimliği HIZ)",
                            iflasW.Special(t).Hits >= 5, iflasW.Special(t).Hits);

            //    ⚠️ ÖZELLİK değil ETKİ: "Hits 7 mi" değil, YEDİ VURUŞ GERÇEKTEN
            //    İNİYOR MU. Seri `TickFlurry(dt)` ile çevriliyor — coroutine
            //    yazsaydım editörde bunu ölçemezdim, yalnızca "başladı mı"
            //    sorabilirdim (projenin `Step(dt)` kuralı).
            pc.SetWeapon(Index<Iflas>());
            ResetSpecial(pc);
            ClearEnemies();
            var kurban = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.2f);
            float kurbanÖnce = kurban.TimeLeft;

            startSpecial.Invoke(pc, new object[] { Vector3.forward });
            float ilkVuruştanSonra = kurban.TimeLeft;
            failed += Check(ref total, "⚠️ TASFİYE'nin İLK vuruşu HEMEN iniyor (bekleme hissi yok)",
                            ilkVuruştanSonra < kurbanÖnce - 0.001f, kurbanÖnce - ilkVuruştanSonra);

            var sp2 = iflasW.Special(t);
            for (int i = 0; i < sp2.Hits + 2; i++) pc.TickFlurry(sp2.HitInterval + 0.001f);

            failed += Check(ref total, "⚠️ TASFİYE'nin TÜM vuruşları indi (etki)",
                            kurban.TimeLeft < ilkVuruştanSonra - 0.001f,
                            ilkVuruştanSonra - kurban.TimeLeft);
            failed += Check(ref total, "seri BİTİYOR (sonsuza kadar vurmuyor)",
                            pc.FlurryLeft == 0, pc.FlurryLeft);

            // ⚠️ SERİ, KENDİSİNİ BAŞLATAN EYLEMİ AŞMAMALI ──
            //    Sayaçla ilerlediği için silah değişse de dash atılsa da kendi
            //    başına devam ediyordu. En kötüsü: `SpecialStrike` o an KUŞANILAN
            //    silahın hasarını okuyor, yani İFLAS'ın serisini FAİZLİ'nin
            //    hasarıyla atmak mümkündü.
            ResetSpecial(pc);
            ClearEnemies();
            MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.2f);
            startSpecial.Invoke(pc, new object[] { Vector3.forward });
            failed += Check(ref total, "test kurgusu: seri başladı", pc.FlurryLeft > 0, pc.FlurryLeft);

            pc.SetWeapon(Index<Faizli>());
            failed += Check(ref total, "⚠️ SİLAH DEĞİŞİNCE seri kesiliyor (istismar kapalı)",
                            pc.FlurryLeft == 0, pc.FlurryLeft);

            // Dash de kesmeli — dash bu oyunda HER saldırıyı keser
            pc.SetWeapon(Index<Iflas>());
            ResetSpecial(pc);
            ClearEnemies();
            MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * 1.2f);
            startSpecial.Invoke(pc, new object[] { Vector3.forward });
            typeof(PlayerController).GetMethod("StartDash", BindingFlags.NonPublic | BindingFlags.Instance)
                                    .Invoke(pc, new object[] { Vector3.forward });
            failed += Check(ref total, "⚠️ DASH seriyi kesiyor (dash her saldırıyı keser)",
                            pc.FlurryLeft == 0, pc.FlurryLeft);

            // Seri, İFLAS'a ÖZEL — diğerlerinin özeli tek vuruş
            int seriSayısı = 0;
            foreach (var w in Weapon.All) if (w.Special(t).Hits > 1) seriSayısı++;
            failed += Check(ref total, "seri vuruş İFLAS'a özel (kimlik dağılmıyor)",
                            seriSayısı == 1, seriSayısı);

            // ── 10) ⚠️ DASH KİMLİKLERİ — üçü ayrışıyor mu ──
            failed += Check(ref total, "⚠️ HACİZ dash'i NİŞANIN TERSİNE",
                            System.Array.Find(Weapon.All, w => w is Haciz).DashBackwards, 0f);
            failed += Check(ref total, "⚠️ FAİZLİ dash'i bedeli İADE ediyor",
                            System.Array.Find(Weapon.All, w => w is Faizli).DashRefundsCost, 0f);
            int kimlikli = 0;
            foreach (var w in Weapon.All)
                if (w.DashBackwards || w.DashRefundsCost) kimlikli++;
            failed += Check(ref total, "dash kimlikleri İKİ silaha kanca gerektirdi (üçününki zaten ayrı)",
                            kimlikli == 2, kimlikli);

            // ── ⚠️ İPOTEK'in dash kimliği: YER İŞARETİ DEĞİL, DOĞRUDAN YAY ──
            //
            // Enes: *"ipotek buga girmiş, hareket ederken arkamda ipoteğin düz
            // saldırıları çıkıyor. hasar vermiyor sadece efekti çıkıyo."*
            //
            // Sebep benim eklediğim `DashLeavesMark`'tı: dash-atak oyuncunun
            // BAŞLADIĞI noktaya — yani arkasına — bir ipotek bırakıyordu. Ve
            // İPOTEK'te aynı anda TEK ipotek yaşayabildiği için o hayalet işaret
            // oyuncunun NİŞAN ALDIĞI ipoteğin yerini de çalıyordu: iki şikâyet,
            // tek sebep.
            //
            // Kanca silindi; silahın ASIL dash kimliği zaten belgeliydi ve
            // yerinde duruyor — menzilli/yer silahının YAKIN CEVABI. Kapı artık
            // onu ölçüyor: dash yay vuruyor ve GERİDE İŞARET BIRAKMIYOR.
            pc.SetWeapon(Index<Ipotek>());
            ClearEnemies();
            TemizleIsaretler();
            int öncekiİşaret = MarkCount();
            int öncekiYay = pc.SwingsSpawned;
            dashStrike.Invoke(pc, null);
            failed += Check(ref total, "⚠️ İPOTEK dash'i DOĞRUDAN yay vuruyor (yakın cevabı)",
                            pc.SwingsSpawned > öncekiYay,
                            pc.SwingsSpawned - öncekiYay);
            failed += Check(ref total, "⚠️ ...ve ARKASINA hayalet ipotek BIRAKMIYOR (Enes'in hatası)",
                            MarkCount() == öncekiİşaret, MarkCount() - öncekiİşaret);
            TemizleIsaretler();

            ClearEnemies();
            Object.DestroyImmediate(pGo);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(t);
            Clock.Reset(); Region.Reset();

            if (failed == 0) Debug.Log($"[Faz 16 öz-denetim] ✅ {total}/{total} GEÇTİ");
            else Debug.LogError($"[Faz 16 öz-denetim] ❌ {total} kontrolden {failed} tanesi BAŞARISIZ");
            return failed;
        }

        /// <summary>Özel saldırının SORDUĞU SORU — adı değil. Silah ölçüsünün aynısı.</summary>
        static string Soru(WeaponSpecial s) =>
            $"halka={s.Angle >= 360f}|tek={s.MaxTargets == 1}|yer={s.GroundDelay > 0f}"
          + $"|bedelli={s.Cost > 0f}|iade={s.KillRefundMul > 0f}|mesafeli={s.DamagePerDistance > 0f}"
          + $"|seri={s.Hits > 1}";   // İFLAS'ın kimlik ekseni

        /// <summary>HACİZ özelinin belirli mesafede indirdiği hasar — ETKİ ölçümü.</summary>
        static float HacizHasari(Tuning t, PlayerController pc, GameObject pGo,
                                 MethodInfo startSpecial, float mesafe)
        {
            ClearEnemies();
            ResetSpecial(pc);
            var e = MakeEnemy(t, pGo.transform, Bootstrap.ArenaCenter + Vector3.forward * mesafe);
            float önce = e.TimeLeft;
            startSpecial.Invoke(pc, new object[] { Vector3.forward });
            return önce - e.TimeLeft;
        }

        /// <summary>Bütün silahların bekleme sürelerini sıfırla — her biri AYRI.</summary>
        static void ResetSpecial(PlayerController pc) =>
            typeof(PlayerController).GetField("_specialCd", BindingFlags.NonPublic | BindingFlags.Instance)
                                    .SetValue(pc, new float[Weapon.All.Length]);

        static int Index<T>() where T : Weapon =>
            System.Array.FindIndex(Weapon.All, w => w is T);

        static int MarkCount() =>
            Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include).Length;

        static void TemizleIsaretler()
        {
            foreach (var m in Object.FindObjectsByType<GroundMark>(FindObjectsInactive.Include))
                Object.DestroyImmediate(m.gameObject);
            GroundMark.ResetLiens();     // yok etmek sayacı düşürmüyor (edit modda OnDestroy yok)
        }

        static Enemy MakeEnemy(Tuning t, Transform player, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = pos;
            go.transform.localScale = Bootstrap.EnemyScale;
            var e = go.AddComponent<Enemy>();
            e.Init(t, player, null);
            return e;
        }

        static void ClearEnemies()
        {
            foreach (var e in Enemy.All.ToArray())
                if (e != null) Object.DestroyImmediate(e.gameObject);
            Enemy.All.Clear();
            foreach (var a in Object.FindObjectsByType<Arc>(FindObjectsInactive.Include))
                Object.DestroyImmediate(a.gameObject);
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
