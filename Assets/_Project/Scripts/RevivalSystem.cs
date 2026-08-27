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

        void Update()
        {
            if (!LastStandArmed || _clock == null || _clock.Dead) return;

            // ⚠️ GEÇİCİ DÖNÜŞ'ün kazanma koşulu: odayı temizlemek.
            // Başardıysan eşya boşa gitmedi — checkpoint zamanına kavuşuyorsun.
            // (Enes: "eğer yenemezsen o süre içinde checkpointe atacak" — yani
            //  yenersen atmayacak. Kazanmanın karşılığı budur.)
            var rm = RoomManager.Instance;
            if (rm == null || !rm.Cleared) return;

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

            // ⚠️ ÖNCE DÜŞ, SONRA YAZ, EN SON UYGULA.
            save.intact[kind.Index]--;
            save.broken[kind.Index]++;      // kırıldı — zamanla tamir edilebilir
            save.deaths++;
            SaveFile.Flush();

            LastUsed = kind;
            LastStandArmed = kind.ArmsLastStand;

            Debug.Log($"[Tek Can] {kind.Name} kullanıldı — kalan sağlam: {RevivalKind.IntactCount(save)}");

            if (kind.SendsToCheckpoint) { Recall(); return; }

            _clock.Revive(kind.SecondsOnUse(_t, save));
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

        float CheckpointSeconds()
        {
            var s = SaveFile.Current;
            return s != null && s.checkpointSeconds > 0f ? s.checkpointSeconds : _t.startSeconds;
        }
    }
}
