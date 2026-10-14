using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Ölüm anında ne olacağını yürüten yer. `Clock.OnDeath`'i dinliyor.
    ///
    /// ⚠️⚠️ SIRA KUTSAL: **eşya DÜŞÜLÜR → DİSKE YAZILIR → sonra diriliş uygulanır.**
    /// Ters sırada olsaydı ölüm anında oyunu kapatan oyuncu eşyasını geri kazanırdı
    /// ve "tek can" diye bir şey kalmazdı.
    ///
    /// ⚠️ Faz 28 (yasa değişti, Enes 2026-10-12): eşya kalmadıysa oyun BİTMEZ — son kontrol
    /// noktasına dönülür. Kontrol noktaları NEMESİS TOPARLANMASIYLA aşınıyor (`Aşındır`).
    /// </summary>
    public class RevivalSystem : MonoBehaviour
    {
        public static RevivalSystem Instance { get; private set; }
        public static void Reset() => Instance = null;

        Tuning _t;
        Clock _clock;

        /// <summary>GEÇİCİ DÖNÜŞ kuruldu mu — bu sayaç doluyken ölmek checkpoint'e atar.</summary>
        public bool LastStandArmed { get; private set; }

        // ⚠️ Kurulduğu ANDAKİ oda durumu. Olmasaydı şu açık doğardı: temizlenmiş odada
        // (dövüş dışı akıştan) ölürsen GEÇİCİ DÖNÜŞ kurulur kurulmaz `Update` odayı
        // zaten temiz görür ve ANINDA "başardın" der — bedava checkpoint zamanı.
        // Kazanmak için temizlik ARMDAN SONRA gerçekleşmeli.
        int _armRoom;
        bool _armCleared;

        /// <summary>
        /// Son toparlanma: hangi mini boss geri geldi, ne zaman. HUD birkaç saniye gösteriyor.
        /// ⚠️ STATİK: geri dönüş sahneyi yeniden kuruyor (`Bootstrap.Restart`), örnek alanı kaybolurdu.
        /// </summary>
        public static string SonGeriGelen { get; private set; }
        public static float SonGeriGelmeAnı { get; private set; } = -99f;

        /// <summary>HUD ve öz-denetim için: son kullanılan eşya.</summary>
        public RevivalKind LastUsed { get; private set; }

        public void Init(Tuning tuning, Clock clock)
        {
            Instance = this;
            _t = tuning;
            _clock = clock;
            if (clock != null) clock.OnDeath += OnDeath;
        }

        void OnDestroy()
        {
            if (_clock != null) _clock.OnDeath -= OnDeath;
        }

        /// <summary>
        /// Sahnedeki boss'un senden aldığı zamanı hesabına geçir.
        ///
        /// ⚠️ Yalnızca GERÇEK boss — son boss'un çağırdığı yankıların kendi hesabı
        /// yok. Olsaydı final tek ölümde dört ayrı boss'u birden şişirirdi.
        ///
        /// ⚠️ Sıradan düşman hesap tutmuyor (TASARIM.md: "sadece boss ve isimli
        /// düşmanlar; tam nemesis çok pahalı"). Az iş, çok etki.
        /// </summary>
        static void NemesisiYaz()
        {
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.IsBoss || e.SummonedRival || e.Boss.IsMini) continue;   // mini: toparlanma defteri
                if (e.StolenFromPlayer <= 0f) continue;

                SaveFile.CreditBoss(e.Boss, e.StolenFromPlayer);
                Debug.Log($"[Tek Can] NEMESİS — {e.Boss.Name} senden {e.StolenFromPlayer:0.0} sn aldı. " +
                          $"Toplam hesabı: {SaveFile.BossLedger(e.Boss):0.0} sn.");
                e.HesabıKapat();   // yazılan bir daha yazılmasın (aynı dövüşte ikinci ölüm)
            }
        }

        void Update()
        {
            if (!LastStandArmed || _clock == null || _clock.Dead) return;

            // ⚠️ GEÇİCİ DÖNÜŞ'ün kazanma koşulu: odayı temizlemek.
            // Başardıysan eşya boşa gitmedi — checkpoint zamanına kavuşuyorsun.
            // (Enes: "eğer yenemezsen o süre içinde checkpointe atacak" — yani
            //  yenersen atmayacak. Kazanmanın karşılığı budur.)
            var rm = RoomManager.Instance;
            if (rm == null || !rm.Cleared) return;

            // ⚠️ Sunak odası düşmansız ve ilk karesinde "temiz" sayılıyor. Buraya yürümek
            // GEÇİCİ DÖNÜŞ'ü dövüşmeden kazandırıyordu. Koşul "bir odayı TEMİZLEMEK".
            if (rm.IsAltarRoom) return;

            // Aynı oda zaten temizken kurulduysa, o temizlik SENİN eserin değil — saymaz.
            if (rm.Room == _armRoom && _armCleared) return;

            LastStandArmed = false;
            _clock.Revive(CheckpointSeconds());
            rm.SaveRoomStart();   // K1: kurulunca silinen oda başı kaydı, kazanınca geri yazılıyor
            Sfx.Win();
            IsoCamera.Shake(_t.hitShake * 1.2f);
            Debug.Log("[Tek Can] GEÇİCİ DÖNÜŞ başarılı — oda temizlendi, checkpoint zamanı geri verildi");
        }

        public void OnDeath()
        {
            var save = SaveFile.Current;
            if (save == null || save.gameOver) return;

            // Son şans sayacı doluyken ölmek: EŞYA HARCAMAZ, doğrudan checkpoint'e atar.
            if (LastStandArmed)
            {
                LastStandArmed = false;
                NemesisiYaz();   // bu da bir ölüm: seni öldüren boss'un hesabına geçer
                Aşındır(save);
                save.deaths++;
                save.resumeRoom = 0;    // K1: checkpoint'e dönülüyor, oda başı kaydı geçersiz
                SaveFile.Flush();
                Debug.Log("[Tek Can] GEÇİCİ DÖNÜŞ tükendi — checkpoint'e dönülüyor");
                Recall();
                return;
            }

            var kind = RevivalKind.FirstAvailable(save);
            if (kind == null)
            {
                // ⚠️ Faz 28: oyun BİTMİYOR — son kontrol noktasına. Sıra yine kutsal: yaz, sonra dön.
                NemesisiYaz();
                Aşındır(save);
                save.deaths++;
                save.resumeRoom = 0;   // K1: oda başı kaydı geçersiz, kontrol noktasına
                SaveFile.Flush();
                Debug.Log("[Tek Can] Diriliş eşyası yok — son kontrol noktasına dönülüyor");
                Recall();
                return;
            }

            // ⚠️ NEMESİS: seni öldüren boss'un hesabına, SENDEN ALDIĞI zaman yazılıyor.
            //    `TASARIM.md`: "seni öldüren boss, senden aldığı zamanı hesabına
            //    geçiriyor ve bir dahaki karşılaşmada daha büyük oluyor."
            //
            //    ⚠️ Buraya konuldu çünkü ölüm sırası ZATEN burada kutsal
            //    (düş → diske yaz → uygula). Nemesis o sıranın İÇİNE giriyor,
            //    yanına değil: ayrı bir yazma yolu ikinci bir sıra sorunu doğururdu
            //    ve "ölüm anında kapatan oyuncu" açığı nemesis için yeniden açılırdı.
            NemesisiYaz();
            bool geriGeldi = Aşındır(save);

            // ⚠️ ÖNCE DÜŞ, SONRA YAZ, EN SON UYGULA.
            save.intact[kind.Index]--;
            save.broken[kind.Index]++;      // kırıldı — zamanla tamir edilebilir
            save.deaths++;

            // ⚠️ K1 — ODA BAŞI KAYDI ÖLÜMÜ DE TAŞIMALI, aynı yazmanın içinde. Yoksa
            // öl → yerinde diril (az zamanla) → kapat → aç = oda başına GİRİŞ zamanıyla
            // dönüş. Eşya yanmış ama zamanı geri alınmış olurdu.
            // GEÇİCİ DÖNÜŞ de siliyor: kurulu sayaç diske yazılmıyor, sayarken kapatıp
            // açan oyuncu 10 sn ile ve sayaçsız odanın başına düşüyordu — sıradaki ölüm
            // yeni bir eşya yakıyordu. Artık kapatmak = başaramamak = checkpoint (inceleme).
            // ÇAĞRI: bir ÖNCEKİ odanın başı (oda başı kaydına yazılıp oradan kuruluyor). Mini boss
            // geri geldiyse kontrol noktası geri kaydı — ÇAĞRI onun ötesine atlatamaz.
            bool öncekiOdaya = kind.SendsBack && !geriGeldi && save.recallRoom > 0;
            if (öncekiOdaya)
            {
                save.resumeRoom = save.recallRoom; save.resumeRegion = save.recallRegion;
                save.resumeSeconds = kind.SecondsOnUse(_t);
                save.resumeHarvest = save.recallHarvest; save.resumeDamage = save.recallDamage;
                save.resumeDash = save.recallDash; save.resumeBoughtCounts = save.recallBoughtCounts;
            }
            else if (kind.SendsBack || kind.ArmsLastStand) save.resumeRoom = 0;
            else save.resumeSeconds = Mathf.Min(save.resumeSeconds, kind.SecondsOnUse(_t));
            SaveFile.Flush();

            LastUsed = kind;
            LastStandArmed = kind.ArmsLastStand;
            if (LastStandArmed)
            {
                var rm = RoomManager.Instance;
                _armRoom = rm != null ? rm.Room : -1;
                _armCleared = rm != null && rm.Cleared;
            }

            Debug.Log($"[Tek Can] {kind.Name} kullanıldı — kalan sağlam: {RevivalKind.IntactCount(save)}");

            if (kind.SendsBack) { Recall(öncekiOdaya); return; }

            _clock.Revive(kind.SecondsOnUse(_t));
            IsoCamera.Shake(_t.hurtShake * 1.5f);
        }

        /// <summary>
        /// Faz 28 — NEMESİS TOPARLANMASI. Bu ölümün sana kaybettirdiği saniyeler (odaya girerken
        /// saatin + odada hasat ettiğin) SON TEMİZLENMİŞ MİNİ BOSS'un defterine yazılıyor. Defter
        /// o mini boss'un havuzunun ×`miniBossRecoveryShare`'ine ulaşırsa mini boss GERİ GELİYOR:
        /// kontrol noktası bir öncekine kayıyor ve mini boss defteri kadar büyümüş olarak bekliyor.
        /// Enes: *"sonraki minibossu olabildiğince az ölüm ile temizlemek lazım yoksa yenileniyor."*
        /// ⚠️ Bölgenin ilk odasındaki kontrol noktası aşınmaz (önünde mini boss yok; ana boss'un
        /// temizlediği bölge geri gelmez).
        /// </summary>
        /// <returns>Mini boss GERİ GELDİ mi (kontrol noktası geri kaydı).</returns>
        bool Aşındır(SaveData save)
        {
            var rm = RoomManager.Instance;
            if (rm == null || _t == null) return false;
            if (RoomManager.Tür(_t, save.checkpointRoom) != RoomManager.OdaTürü.Sunak) return false;
            int hedef = RoomManager.AraBölge(_t, save.checkpointRoom);

            SaveFile.MiniDefterYaz(hedef, rm.ÖlümKaybı);
            float eşik = _t.enemyTimePool * RoomManager.MiniBossHavuzu(_t, save.checkpointRegion, hedef)
                       * _t.miniBossRecoveryShare;
            float defter = SaveFile.MiniDefter(hedef);
            Debug.Log($"[Tek Can] TOPARLANMA — {hedef + 1}. mini boss {defter:0}/{eşik:0} sn");
            if (defter < eşik) return false;

            // GERİ GELDİ: kontrol noktası bir öncekine (önceki mini boss'un sunağı ya da bölgenin başı).
            save.checkpointRoom = hedef == 0 ? 1 : RoomManager.SunakOdası(_t, hedef - 1);
            var mini = MiniBossKind.For(save.checkpointRegion, hedef, Mathf.Max(1, _t.subRegionsPerRegion));
            SonGeriGelen = mini.Name;
            SonGeriGelmeAnı = Time.unscaledTime;
            Debug.Log($"[Tek Can] {mini.Name} GERİ GELDİ — kontrol noktası oda {save.checkpointRoom}'e kaydı");
            return true;
        }

        /// <summary>Öz-denetim için: kaç kez checkpoint'e dönüldü.</summary>
        public int RecallCount { get; private set; }

        void Recall(bool odaBaşına = false)
        {
            RecallCount++;
            // Sahne yeniden kuruluyor; Bootstrap kayıttan checkpoint odasını ve zamanını okuyor.
            // ⚠️ Editör modunda sahneyi yeniden kurmak anlamsız ve yıkıcı; öz-denetim
            // `RecallCount`'a bakıyor. (`Enemy`/`Arc`/`Mote` ile aynı isPlaying ayrımı.)
            if (Application.isPlaying) Bootstrap.Restart(odaBaşına);
        }

        float CheckpointSeconds() => SaveFile.CheckpointSeconds(_t);
    }
}
