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
    /// Eşya kalmadıysa oyun BİTER. Gerçekten biter.
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
                if (e == null || !e.IsBoss || e.SummonedRival) continue;
                if (e.StolenFromPlayer <= 0f) continue;

                SaveFile.CreditBoss(e.Boss, e.StolenFromPlayer);
                Debug.Log($"[Tek Can] NEMESİS — {e.Boss.Name} senden {e.StolenFromPlayer:0.0} sn aldı. " +
                          $"Toplam hesabı: {SaveFile.BossLedger(e.Boss):0.0} sn.");
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

            // Aynı oda zaten temizken kurulduysa, o temizlik SENİN eserin değil — saymaz.
            if (rm.Room == _armRoom && _armCleared) return;

            LastStandArmed = false;
            _clock.Revive(CheckpointSeconds());
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
                save.deaths++;
                SaveFile.Flush();
                Debug.Log("[Tek Can] GEÇİCİ DÖNÜŞ tükendi — checkpoint'e dönülüyor");
                Recall();
                return;
            }

            var kind = RevivalKind.FirstAvailable(save);
            if (kind == null) { GameOver(save); return; }

            // ⚠️ NEMESİS: seni öldüren boss'un hesabına, SENDEN ALDIĞI zaman yazılıyor.
            //    `TASARIM.md`: "seni öldüren boss, senden aldığı zamanı hesabına
            //    geçiriyor ve bir dahaki karşılaşmada daha büyük oluyor."
            //
            //    ⚠️ Buraya konuldu çünkü ölüm sırası ZATEN burada kutsal
            //    (düş → diske yaz → uygula). Nemesis o sıranın İÇİNE giriyor,
            //    yanına değil: ayrı bir yazma yolu ikinci bir sıra sorunu doğururdu
            //    ve "ölüm anında kapatan oyuncu" açığı nemesis için yeniden açılırdı.
            NemesisiYaz();

            // ⚠️ ÖNCE DÜŞ, SONRA YAZ, EN SON UYGULA.
            save.intact[kind.Index]--;
            save.broken[kind.Index]++;      // kırıldı — zamanla tamir edilebilir
            save.deaths++;
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

            if (kind.SendsToCheckpoint) { Recall(); return; }

            _clock.Revive(kind.SecondsOnUse(_t));
            IsoCamera.Shake(_t.hurtShake * 1.5f);
        }

        void GameOver(SaveData save)
        {
            save.gameOver = true;
            save.deaths++;
            SaveFile.Flush();     // ⚠️ oyun bittiği diske YAZILIR, sonra ekran gösterilir
            Debug.Log("[Tek Can] OYUN BİTTİ — diriliş eşyası kalmadı");
        }

        /// <summary>Öz-denetim için: kaç kez checkpoint'e dönüldü.</summary>
        public int RecallCount { get; private set; }

        void Recall()
        {
            RecallCount++;
            // Sahne yeniden kuruluyor; Bootstrap kayıttan checkpoint odasını ve zamanını okuyor.
            // ⚠️ Editör modunda sahneyi yeniden kurmak anlamsız ve yıkıcı; öz-denetim
            // `RecallCount`'a bakıyor. (`Enemy`/`Arc`/`Mote` ile aynı isPlaying ayrımı.)
            if (Application.isPlaying) Bootstrap.Restart();
        }

        float CheckpointSeconds() => SaveFile.CheckpointSeconds(_t);
    }
}
