using System;
using System.IO;
using UnityEngine;

namespace TekCan
{
    /// <summary>Diske yazılan koşu durumu. Küçük tutuluyor — büyüdükçe bozulma riski artar.</summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>
        /// NEMESİS DEFTERİ — her boss'un SENDEN ALDIĞI toplam saniye.
        ///
        /// `TASARIM.md`: *"Seni öldüren boss, senden aldığı zamanı hesabına
        /// geçiriyor ve bir dahaki karşılaşmada daha büyük oluyor."*
        ///
        /// ⚠️ Borç metaforu kesintisiz: bu oyunda ölmek bir ceza değil bir
        /// TEMERRÜT. Kaybettiğin zaman yok olmuyor, KARŞI TARAFA geçiyor.
        ///
        /// ⚠️ Kalıcı ölümle siliniyor (`NewRun`): meta ilerleme yok, nemesis de
        /// meta değil — bir KOŞUNUN içindeki hikâye.
        /// </summary>
        public float[] bossLedger = new float[0];

        /// <summary>
        /// Faz 28 — MİNİ BOSS TOPARLANMA DEFTERİ (bu bölgede, ara bölge başına). Ölümde kaybettiğin
        /// saniyeler son temizlenmiş mini boss'un hanesine yazılıyor; dolunca o mini boss geri geliyor.
        /// Bölge değişince siliniyor — ana boss'un temizlediği bölge geri gelmez.
        /// </summary>
        public float[] miniLedger = new float[0];

        public int checkpointRoom = 1;
        public int checkpointRegion;               // 0 tabanlı
        public float checkpointSeconds = -1f;      // -1 = henüz checkpoint yok, startSeconds kullan

        /// <summary>Sağlam eşya sayısı, `RevivalKind.All` sırasıyla.</summary>
        public int[] intact = new int[3];

        /// <summary>Kullanılmış (kırık) eşya — zamanla TAMİR edilebilir.</summary>
        public int[] broken = new int[3];

        /// <summary>Koşu BİTTİ — eşyasız ölümle YA DA zaferle. Sıradaki açılış yeni koşu.</summary>
        public bool gameOver;
        public int deaths;

        // ⚠️ Checkpoint SADECE zamanı değil, DURUMU kaydeder. Yalnızca saat kaydedilseydi
        // checkpoint'e dönen oyuncu eski zamanıyla ama SIFIR yükseltmeyle aynı odalara
        // girerdi — yarım geri yükleme, ve ölüm sarmalı. Sunağa ödediğin zaman
        // checkpoint'in ARDINDA kalmamalı.
        // (Meta ilerleme DEĞİL: game over'da `NewRun` hepsini siliyor.)
        public float harvestRate = -1f;
        public float attackDamage = -1f;
        public float dashCooldown = -1f;
        /// <summary>Faz 27: türe göre alım sayısı (hasat, hasar, dash) — kalıcı fiyat ve azalan getiri.</summary>
        public int[] boughtCounts = new int[0];

        public bool HasUpgrades => harvestRate > 0f;

        // ⚠️ ODA BAŞI KAYIT (K1, Enes kararı 2026-09-30). Her oda girişinde yazılıyor;
        // oyunu KAPATIP AÇAN oyuncu girdiği odanın başına, girdiği andaki zamanla döner.
        // Önceden yalnızca checkpoint vardı: saat 3 sn'ye düşünce Alt+F4 → checkpoint'e
        // TAM zamanla dönüş, eşya harcanmıyordu. Açık küçüldü ama bilerek kapanmadı:
        // bir oda içinde kapatmak hâlâ o odanın başına döndürüyor ("oda başı" kayıtlı
        // oyunlar gibi). ÇAĞRI bu kaydı KULLANMAZ — o checkpoint'e atar.
        public int resumeRoom;                      // 0 = yok
        public int resumeRegion;
        public float resumeSeconds = -1f;
        public float resumeHarvest = -1f;
        public float resumeDamage = -1f;
        public float resumeDash = -1f;
        public int[] resumeBoughtCounts = new int[0];

        // ÇAĞRI'nın hedefi (Enes 2026-10-14: "bir önceki odaya atsın"): bir ÖNCEKİ odanın başı.
        // Oda başı kaydı her girişte buraya kayıyor; bölge sınırını geçmiyor (ana boss'un bölgesi geri gelmez).
        public int recallRoom;                      // 0 = yok
        public int recallRegion;
        public float recallSeconds = -1f;
        public float recallHarvest = -1f;
        public float recallDamage = -1f;
        public float recallDash = -1f;
        public int[] recallBoughtCounts = new int[0];
    }

    /// <summary>
    /// Kalıcı ölümün altyapısı.
    ///
    /// ⚠️⚠️ ÖLÜM ANINDA DİSKE YAZILIR. Yazılmazsa oyuncu ölmeden Alt+F4 yapar ve
    /// "tek can" diye bir şey kalmaz — kalıcı ölümlü oyunları bozan tuzak birebir budur.
    /// Bu yüzden `Flush()` gecikmesiz, coroutine'siz, UI'dan ÖNCE çağrılıyor.
    ///
    /// Sıra önemli: eşya DÜŞÜLÜR → DİSKE YAZILIR → sonra diriliş uygulanır.
    /// Ters sırada olsaydı, ölüm anında kapatan oyuncu eşyasını geri kazanırdı.
    /// </summary>
    public static class SaveFile
    {
        public static SaveData Current { get; private set; }

        /// <summary>
        /// Diskte gerçek bir kayıt var mıydı. ⚠️ Bu bayrak olmadan oyunu İLK KEZ açan
        /// oyuncu SIFIR diriliş eşyasıyla başlıyordu ve ilk ölümde "oyun bitti" görüyordu.
        /// Öz-denetimler hep `NewRun` çağırdığı için bu durumu hiç kurmamışlardı.
        /// </summary>
        public static bool LoadedFromDisk { get; private set; }

        /// <summary>Hiç oynanmamış ya da bitmiş koşu — yeni stok verilmeli.</summary>
        public static bool NeedsNewRun => Current == null || !LoadedFromDisk || Current.gameOver;

        /// <summary>
        /// ⚠️ Öz-denetimler ve raporlar OYUNCUNUN GERÇEK KAYDINI EZMEMELİ.
        ///
        /// `Faz9SelfCheck` diske gerçekten yazıp geri okuyor (kalıcı ölümün kanıtı bu),
        /// `Faz10SelfCheck` ve `ZorlukEgrisi` de checkpoint yazdırıyor. Hepsi varsayılan
        /// yola yazıyordu — yani `AllChecks`'i her çalıştırmak Enes'in koşusunu siliyordu.
        /// Araçlar burayı geçici bir dosyaya çevirip iş bitince geri bırakıyor.
        /// </summary>
        public static string PathOverride { get; set; }

        /// <summary>
        /// ⚠️⚠️ OYNANMIYORSA OYUNCUNUN DOSYASINA YAZILMAZ.
        ///
        /// İlk düzeltmede yalnızca o an dokunduğum üç araca `PathOverride` koymuştum.
        /// Ama `RoomManager` kuran HER araç checkpoint yazdırıyor — `Faz4SelfCheck`,
        /// `Faz5SelfCheck`, `Faz1Repro`, `RoomCapture` hâlâ gerçek kaydı eziyordu.
        /// Her dosyaya tek tek koruma serpmek bir sonraki araçta yine unutulurdu.
        ///
        /// Bu yüzden kural kaynağa taşındı: **yalnızca gerçekten oynanırken**
        /// (`Application.isPlaying`) oyuncunun dosyasına yazılır. Editör ve batch
        /// modda otomatik olarak geçici dosyaya gidilir — yeni araç yazan kimsenin
        /// bir şey hatırlamasına gerek yok.
        /// </summary>
        static string Path => PathOverride
            ?? (Application.isPlaying
                ? System.IO.Path.Combine(Application.persistentDataPath, "tekcan_run.json")
                : System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tekcan_editor.json"));

        /// <summary>Öz-denetim için: şu an oyuncunun gerçek dosyasına mı yazılıyor.</summary>
        public static bool WritesToPlayerSave => PathOverride == null && Application.isPlaying;

        public static void Load()
        {
            // ⚠️ Önce YAN dosya: varsa, yarıda kalmış en YENİ yazmadır (başarılı her
            // yazma onu tüketiyor). Yer değiştirme kilitli dosyada (antivirüs, bulut
            // senkronu) düşerse ölüm kaydı orada kalıyordu; okumasaydık oyuncu eşyasını
            // geri kazanır ya da ana dosya da yoksa koşu sessizce silinirdi.
            // Bozuksa (yazarken çökme) ana dosyaya düşülür.
            foreach (var yol in new[] { Path + ".tmp", Path })
            {
                try
                {
                    if (!File.Exists(yol)) continue;
                    var okunan = JsonUtility.FromJson<SaveData>(File.ReadAllText(yol));
                    if (okunan == null) continue;
                    Current = okunan; LoadedFromDisk = true; Normalize(); return;
                }
                catch (Exception e)
                {
                    // Bozuk kayıt oyunu açılmaz yapmasın; sıradakiyle ya da yenisiyle devam.
                    Debug.LogWarning($"[Tek Can] Kayıt okunamadı ({System.IO.Path.GetFileName(yol)}): {e.Message}");
                }
            }
            Current = new SaveData();
            LoadedFromDisk = false;
            Normalize();
        }

        /// <summary>⚠️ Diske HEMEN yazar. Ölüm anında gecikme kalıcı ölümü yok eder.</summary>
        /// <summary>
        /// ⚠️ TEST KISAYOLU KULLANILDI — bu koşu artık DİSKE YAZILMAZ.
        ///
        /// Enes bildirdi: *"bölgeyi başa saramıyorum, restart atınca 5. bölgeye geçiyor."*
        /// Sebep: `N` ile atlanan bölgenin 1. odası checkpoint odası, yani her test
        /// atlayışı diske **"5. bölgedesin"** yazıyordu. Kısayol geçiciydi, bıraktığı
        /// iz kalıcı — ve geri dönüşü yoktu.
        ///
        /// Koruma `Flush`'ta, `SaveCheckpoint`'te DEĞİL: ölüm kaydı, diriliş eşyası
        /// harcaması, oyun sonu — hepsi buradan geçiyor. Tek çağıranı korumak, bir
        /// sonraki yazma yolunda aynı hatayı tekrar üretirdi (bu proje o dersi
        /// `PathOverride` ile bir kez öğrendi: kural KAYNAĞA konur).
        /// </summary>
        public static bool TestJumpUsed { get; private set; }

        /// <summary>Test kısayolu basıldı — koşuyu kirlet. Geri alınmıyor: kirlenen kirlidir.</summary>
        public static void MarkTestJump()
        {
            if (TestJumpUsed) return;
            TestJumpUsed = true;
            Debug.LogWarning("[Tek Can] ⚠️ TEST KISAYOLU kullanıldı — bu koşu artık DİSKE " +
                             "YAZILMIYOR. Kaydın olduğu yerde duruyor. (F6 = sıfırdan yeni koşu)");
        }

        public static void Flush()
        {
            if (Current == null) return;
            if (TestJumpUsed) return;                    // ⚠️ kirli koşu diske gitmez
            try
            {
                // ⚠️ ATOMİK: önce yan dosyaya yaz, sonra tek hamlede yerine koy.
                // `WriteAllText` dosyayı önce SIFIRLIYOR, sonra yazıyor; arada çökme ya da
                // elektrik kesintisi boş dosya bırakıyor, `Load` onu okuyamayıp sessizce
                // YENİ KOŞU başlatıyordu — kalıcı ölümlü koşu habersiz siliniyordu
                // (denetim 2026-09-23). Yarım kalan yazma artık yalnızca yan dosyayı bozar.
                // Yedek (.bak) BİLEREK yok: ana dosyayı silen oyuncu yedekten ölüm
                // öncesine dönerdi — "tek can"ı delen yeni bir yol.
                string yan = Path + ".tmp";
                // `Flush(true)`: veri DİSKE inmeden yer değiştirilirse elektrik kesintisinde
                // dosya doğru boyda ama sıfırlarla dolu kalabiliyor (NTFS adı günlükler, içeriği değil).
                using (var fs = new FileStream(yan, FileMode.Create, FileAccess.Write))
                using (var w = new StreamWriter(fs))
                {
                    w.Write(JsonUtility.ToJson(Current));
                    w.Flush();
                    fs.Flush(true);
                }
                if (File.Exists(Path)) File.Replace(yan, Path, null);
                else File.Move(yan, Path);
            }
            catch (Exception e) { Debug.LogError($"[Tek Can] Kayıt YAZILAMADI: {e.Message}"); }
        }

        /// <summary>Yeni oyun — game over sonrası ya da elle sıfırlama.</summary>
        /// <summary>
        /// Checkpoint'e kaydedilmiş zaman — hiç checkpoint yoksa koşu başlangıcı.
        /// ⚠️ TEK KAYNAK: hem `RevivalKind` hem `RevivalSystem` bunu ayrı ayrı
        /// hesaplıyordu. Aynı kavramın iki ölçümü, `lessons.md` (9)'un tam konusu.
        /// </summary>
        /// ⚠️ TABANLI (K2, 2026-09-30): checkpoint odasına 4 sn ile girince her ÇAĞRI
        /// seni 4 sn ile düşmanların yanına atıyordu, eşyalar saniyeler içinde eriyordu.
        /// Taban BURADA, tek kaynakta — ÇAĞRI, GEÇİCİ DÖNÜŞ'ün başarısı ve açılış hep
        /// buradan okuyor. Onarım sunağı bu yüzden tabanın altına inen alımı satmıyor
        /// (yoksa bedeli taban geri öderdi: sonsuz diriliş, ders 14 yan ders).
        public static float CheckpointSeconds(Tuning t)
            => Mathf.Max(t.recallMinSeconds,
                         Current != null && Current.checkpointSeconds > 0f
                             ? Current.checkpointSeconds
                             : t.startSeconds);

        public static void NewRun(Tuning t)
        {
            // ⚠️ Sıfırdan yeni koşu KİRİ DE SİLER — yoksa test kısayolu kullanılmış
            // bir oturumdan sonra hiçbir yeni koşu bir daha kaydedilemezdi.
            TestJumpUsed = false;

            Current = new SaveData();
            LoadedFromDisk = true;      // artık gerçek bir koşu var
            Normalize();
            // ponytail: hikâye yok, eşyalar hikâyeden geliyor olacak. Test için başlangıç
            // stoğu Tuning'den; gerçek oyunda burası kalkacak ve eşyalar hikâyeden gelecek.
            Current.intact[0] = t.testDefianceCount;
            Current.intact[1] = t.testLastStandCount;
            Current.intact[2] = t.testRecallCount;
            Flush();
        }

        /// <summary>Dizi boyları `RevivalKind.All` ile uyumsuzsa (sürüm değişimi) düzelt.</summary>
        static void Normalize()
        {
            int n = RevivalKind.All.Length;
            Current.intact = Fit(Current.intact, n);
            Current.broken = Fit(Current.broken, n);

            // ⚠️ Boss defteri de boy uyumlu tutuluyor — kadroya boss eklenirse
            // eski kayıt kısa kalır ve indis okuma patlardı.
            Current.bossLedger = FitFloat(Current.bossLedger, BossKind.All.Length);
        }

        static float[] FitFloat(float[] src, int n)
        {
            var dst = new float[n];
            if (src != null)
                for (int i = 0; i < Mathf.Min(src.Length, n); i++) dst[i] = Mathf.Max(0f, src[i]);
            return dst;
        }

        /// <summary>Bu boss senden ne kadar aldı — havuzuna eklenecek borç.</summary>
        public static float MiniDefter(int araBölge)
        {
            var l = Current?.miniLedger;
            return l != null && araBölge >= 0 && araBölge < l.Length ? l[araBölge] : 0f;
        }

        public static void MiniDefterYaz(int araBölge, float saniye)
        {
            if (Current == null || araBölge < 0) return;
            if (Current.miniLedger == null || Current.miniLedger.Length <= araBölge)
                Current.miniLedger = FitFloat(Current.miniLedger, araBölge + 1);
            Current.miniLedger[araBölge] += Mathf.Max(0f, saniye);
        }

        /// <summary>Mini boss yeniden öldü: borcu kapandı.</summary>
        public static void MiniDefterSıfırla(int araBölge)
        {
            var l = Current?.miniLedger;
            if (l != null && araBölge >= 0 && araBölge < l.Length) l[araBölge] = 0f;
        }

        /// <summary>Yeni bölge: önceki bölgenin mini boss'ları geri gelmez.</summary>
        public static void MiniDefterleriSil()
        {
            if (Current != null) Current.miniLedger = new float[0];
        }

        public static float BossLedger(BossKind kind)
        {
            if (kind == null || Current?.bossLedger == null) return 0f;
            return kind.Index < Current.bossLedger.Length ? Current.bossLedger[kind.Index] : 0f;
        }

        /// <summary>
        /// Boss'un hesabına yaz — ÖLÜM ANINDA çağrılıyor.
        /// ⚠️ Çağıran yer `RevivalSystem`: ölüm sırası zaten orada kutsal
        /// (düş → diske yaz → uygula) ve nemesis o sıranın İÇİNE giriyor,
        /// yanına değil. Ayrı bir yazma yolu ikinci bir sıra sorunu doğururdu.
        /// </summary>
        public static void CreditBoss(BossKind kind, float seconds)
        {
            if (kind == null || Current == null || seconds <= 0f) return;
            Normalize();
            Current.bossLedger[kind.Index] += seconds;
        }

        static int[] Fit(int[] src, int n)
        {
            var dst = new int[n];
            if (src != null)
                for (int i = 0; i < Mathf.Min(src.Length, n); i++) dst[i] = Mathf.Max(0, src[i]);
            return dst;
        }

        /// <summary>Öz-denetim için: diske dokunmadan bellek içi durum kur.</summary>
        public static void UseInMemory(SaveData data)
        {
            Current = data ?? new SaveData();
            LoadedFromDisk = true;      // öz-denetim durumu kendi kuruyor, taze sayılmaz
            Normalize();
        }
    }
}
