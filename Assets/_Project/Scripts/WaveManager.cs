using UnityEngine;
using UnityEngine.InputSystem;

namespace TekCan
{
    /// <summary>
    /// Koşuya şekil veren şey. Öncesinde 5 düşman bir kez doğuyordu ve koşunun tek
    /// çıkışı ölümdü — yani **arenayı temizlemek kaybetmek demekti**: dövüş dışı
    /// kalıyordun, saat hızlı iniyordu, hasat edecek bir şey kalmıyordu.
    ///
    /// Şimdi: dalgalar sertleşerek gelir, sonuncusu **boss**tur, boss ölürse
    /// **koşu kazanılır.** Sunaklardan alınan güç artık bir şeye hizmet ediyor.
    ///
    /// ⚠️ Dalga arasında ZAMANLAYICI YOK — saat zaten zamanlayıcı. Oyalanmak,
    /// sunaklara bakmak, düşünmek: hepsi doğrudan ömürden kesiliyor. Oyunun yasası
    /// zaten bu, ikinci bir sayaç eklemek gereksizdi.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }
        public static void Reset() => Instance = null;

        public enum Phase { Intermission, Fighting }

        Tuning _t;
        Transform _player;
        Transform _cam;

        public Phase Current { get; private set; } = Phase.Intermission;
        public int Wave { get; private set; }                     // 0 = henüz başlamadı
        public bool IsBossWave => Wave >= _t.waveCount;
        public bool NextIsBoss => Wave + 1 >= _t.waveCount;

        public void Init(Tuning tuning, Transform player, Transform cam)
        {
            Instance = this;
            _t = tuning;
            _player = player;
            _cam = cam;
            Current = Phase.Intermission;
            Wave = 0;
        }

        void Update()
        {
            if (_t == null || Clock.Instance == null || Clock.Instance.Dead || Clock.Instance.Won) return;

            if (Current == Phase.Fighting)
            {
                if (AnyEnemyAlive()) return;

                // Boss dalgası temizlendiyse koşu kazanıldı.
                if (IsBossWave) { Clock.Instance.Win(); return; }

                Current = Phase.Intermission;
                return;
            }

            if (SkipToBossPressed()) { Wave = _t.waveCount - 1; StartNextWave(); return; }
            if (StartPressed()) StartNextWave();
        }

        static bool AnyEnemyAlive()
        {
            foreach (var e in Enemy.All) if (e != null) return true;
            return false;
        }

        /// <summary>
        /// ENTER — SPACE değil. Space zaten dash; aynı tuşa bağlasaydık dalga başlatırken
        /// bir de dash atardın ve iki eylem tek basışta çakışırdı.
        /// </summary>
        static bool StartPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonEast.wasPressedThisFrame;
        }

        /// <summary>
        /// B — doğrudan boss dalgasına atla. TEST KISAYOLU.
        ///
        /// Enes boss'a ulaşamadı, yani boss'u yargılayamadı. Test edilemeyen şey
        /// ayarlanamaz. ENTER ile birlikte bu tuş da gerçek oyunda kalkacak.
        /// </summary>
        static bool SkipToBossPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.bKey.wasPressedThisFrame;
        }

        public void StartNextWave()
        {
            Wave++;
            Current = Phase.Fighting;

            if (IsBossWave) SpawnBoss();
            else SpawnWave(Wave);

            IsoCamera.Shake(_t.hitShake * 0.8f);
        }

        void SpawnWave(int wave)
        {
            int count = Mathf.RoundToInt(_t.waveBaseCount + _t.waveCountGrowth * (wave - 1));
            float poolMult = Mathf.Pow(_t.wavePoolGrowth, wave - 1);

            for (int i = 0; i < count; i++)
            {
                // Halka formülü Bootstrap'ta — burada kopyalamak, oyunun testlerin
                // sınadığı yoldan ayrılması demekti (öz-incelemede yakalandı).
                var pos = Bootstrap.RingPosition(_t, i, count, wave * 0.7f);   // her dalga döner
                Bootstrap.SpawnEnemyAt(_t, pos, _player, _cam, poolMult, boss: false,
                                       kind: PickKind(wave, i));
            }
        }

        /// <summary>
        /// Dalga kompozisyonu. Türler kademeli açılıyor: oyuncu önce temel dövüşü öğrensin,
        /// sonra her dalgada bir yeni soru eklensin. Hepsini birden atmak öğretmez, boğar.
        ///
        /// Deterministik (rastgele değil): aynı dalga hep aynı karışımı verir, böylece
        /// "bu dalga zor mu, ben mi kötü oynadım" sorusu cevaplanabilir kalıyor.
        /// </summary>
        EnemyKind PickKind(int wave, int index)
        {
            if (wave <= _t.plainWaveCount) return EnemyKind.Basic;

            int unlocked = Mathf.Min(EnemyKind.All.Length, 2 + (wave - _t.plainWaveCount - 1));
            return EnemyKind.All[(index + wave) % unlocked];
        }

        void SpawnBoss()
        {
            var pos = Bootstrap.RingPosition(_t, 0, 1);
            Bootstrap.SpawnEnemyAt(_t, pos, _player, _cam, _t.bossPoolMultiplier, boss: true);
        }

        /// <summary>HUD için: dalga arasında ne yazacağını söyler.</summary>
        public string Prompt()
        {
            if (Current == Phase.Fighting)
                return IsBossWave ? "BOSS" : $"dalga {Wave} / {_t.waveCount - 1}";

            if (Wave == 0) return "ENTER  ·  ilk dalgayı başlat";
            return NextIsBoss ? "ENTER  ·  BOSS'u çağır" : $"ENTER  ·  dalga {Wave + 1}'i başlat";
        }
    }
}
