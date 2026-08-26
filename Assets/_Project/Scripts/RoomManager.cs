using UnityEngine;
using UnityEngine.InputSystem;

namespace TekCan
{
    /// <summary>
    /// Koşunun mekânı. `WaveManager`'ın yerini aldı.
    ///
    /// **Neden değişti:** dalgalar tek arenada oluyordu ve ENTER'a basan sen çağırıyordun.
    /// O bir test düzeneğiydi, oyun değil. Şimdi mekân var: odaya girersin, düşmanlar
    /// ZATEN oradadır, temizleyince çıkış açılır, yürüyüp geçersin. Bölgenin sonunda boss.
    ///
    /// Taşınan şeyler (yeniden yazılmadı): `PickKind` kademeli tür açılışı, boss doğumu,
    /// `Bootstrap.SpawnEnemyAt` tek doğum yolu, `Instance`/`Reset` deseni.
    ///
    /// ⚠️ Düşmanlar DOĞMUYOR, oda kurulurken yerleştiriliyor. Doğurma yalnızca boss
    /// dövüşüne özel (Enes kararı) — böylece doğurma bir kural değil BOSS'UN İMZASI
    /// oluyor, ve kalıcı ölümlü oyunda pusudan gelen düşmanın ucuzluğu doğmuyor.
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }
        public static void Reset() => Instance = null;

        Tuning _t;
        Transform _player, _cam;
        GameObject _roomRoot;
        ExitGate _gate;

        /// <summary>1'den başlar. Bölgedeki kaçıncı oda.</summary>
        public int Room { get; private set; }

        public bool IsBossRoom => Room >= _t.roomsPerRegion;
        public bool Cleared { get; private set; }
        public RoomLayout Layout { get; private set; }

        public void Init(Tuning tuning, Transform player, Transform cam)
        {
            Instance = this;
            _t = tuning;
            _player = player;
            _cam = cam;
            EnterRoom(1);
        }

        void Update()
        {
            if (_t == null || Clock.Instance == null || Clock.Instance.Dead || Clock.Instance.Won) return;

            if (SkipToBossPressed()) { EnterRoom(_t.roomsPerRegion); return; }

            if (Cleared || AnyEnemyAlive()) return;
            OnCleared();
        }

        void OnCleared()
        {
            Cleared = true;

            // Boss odasının çıkışı yok — bölge orada bitiyor.
            if (IsBossRoom) { Clock.Instance.Win(); return; }

            if (_gate != null) _gate.Open();
        }

        /// <summary>`ExitGate` çağırıyor — oyuncu açık kapıya yürüdüğünde.</summary>
        public void NextRoom() => EnterRoom(Room + 1);

        void EnterRoom(int index)
        {
            TearDown();

            Room = Mathf.Max(1, index);
            Cleared = false;
            Layout = RoomLayout.For(Room);

            _roomRoot = new GameObject($"Room_{Room}");
            _roomRoot.transform.SetParent(transform.parent);

            // Oyuncu girişte başlar; oda bir YOL, girişten çıkışa gideceksin.
            if (_player != null) _player.position = Bootstrap.Entrance(_t);

            if (IsBossRoom) PlaceBoss();
            else { PlaceEnemies(); PlaceExit(); PlaceAltars(); }

            IsoCamera.Shake(_t.hitShake * 0.6f);
        }

        /// <summary>
        /// Odanın içeriğini söker. ⚠️ `Enemy.All` ve `Altar.All` STATİK listeler —
        /// nesneyi yok etmek listeden düşürmüyor. Temizlemezsek bir sonraki oda
        /// "hâlâ düşman var" sanır ve çıkış hiç açılmaz.
        /// </summary>
        void TearDown()
        {
            foreach (var e in Enemy.All.ToArray())
                if (e != null) Destroy(e.gameObject);
            Enemy.All.Clear();

            Altar.ResetAll();
            _gate = null;

            if (_roomRoot != null)
            {
                // ⚠️ `Destroy` ERTELENİR — eski oda kare sonuna kadar yaşar ve bileşenleri
                // bir tur daha `Update` alır. Ölü çıkış kapısının ikinci kez `NextRoom`
                // çağırıp bir odayı atlaması tam buradan çıkardı.
                // `Bootstrap.Restart` bu tuzağı aynı şekilde kapatıyor.
                _roomRoot.SetActive(false);
                if (Application.isPlaying) Destroy(_roomRoot);
                else DestroyImmediate(_roomRoot);      // öz-denetim editör modunda koşuyor
            }
            _roomRoot = null;
        }

        void PlaceEnemies()
        {
            int count = Mathf.RoundToInt(_t.roomBaseCount + _t.roomCountGrowth * (Room - 1));
            float poolMult = Mathf.Pow(_t.roomPoolGrowth, Room - 1);

            for (int i = 0; i < count; i++)
                Bootstrap.SpawnEnemyAt(_t, Layout.Position(_t, i, count), _player, _cam,
                                       poolMult, boss: false, kind: PickKind(Room, i),
                                       parent: _roomRoot.transform);
        }

        void PlaceBoss()
        {
            Bootstrap.SpawnEnemyAt(_t, new Vector3(0f, Bootstrap.PlayerSpawn.y, _t.arenaRadius * 0.35f),
                                   _player, _cam, _t.bossPoolMultiplier, boss: true,
                                   parent: _roomRoot.transform);
        }

        void PlaceExit()
        {
            _gate = ExitGate.Spawn(_t, _roomRoot.transform, Bootstrap.Exit(_t), _player);
        }

        /// <summary>
        /// ponytail: iki odada bir sunak — ekonomiyi her odada beslersek yükseltmeler
        /// bedava gibi hissettirir. Oda çeşitleri (dükkân, nefes odası, bulmaca)
        /// ayrı bir konuşma; o gelene kadar tek satırlık kural yeterli.
        /// </summary>
        void PlaceAltars()
        {
            if (Room % 2 != 0) return;
            Bootstrap.SpawnAltarRing(_t, _player, _roomRoot.transform);
        }

        static bool AnyEnemyAlive()
        {
            foreach (var e in Enemy.All) if (e != null) return true;
            return false;
        }

        /// <summary>
        /// B — doğrudan boss odasına atla. TEST KISAYOLU, gerçek oyunda kalkacak.
        /// (ENTER kalktı: dalgaları artık oyuncu çağırmıyor.)
        /// </summary>
        static bool SkipToBossPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.bKey.wasPressedThisFrame;
        }

        /// <summary>
        /// Oda kompozisyonu. Türler kademeli açılıyor: oyuncu önce temel dövüşü öğrensin,
        /// sonra her odada bir yeni soru eklensin. Hepsini birden atmak öğretmez, boğar.
        ///
        /// Deterministik (rastgele değil): aynı oda hep aynı karışımı verir, böylece
        /// "bu oda mı zor, ben mi kötü oynadım" sorusu cevaplanabilir kalıyor.
        /// </summary>
        EnemyKind PickKind(int room, int index)
        {
            if (room <= _t.plainRoomCount) return EnemyKind.Basic;

            int unlocked = Mathf.Min(EnemyKind.All.Length, 2 + (room - _t.plainRoomCount - 1));
            return EnemyKind.All[(index + room) % unlocked];
        }

        /// <summary>HUD için.</summary>
        public string Prompt()
        {
            if (IsBossRoom) return "BOSS";
            if (Cleared) return "ÇIKIŞ AÇIK  ·  saat işlemeye devam ediyor";
            return $"oda {Room} / {_t.roomsPerRegion - 1}   ·   {Layout.Name}";
        }
    }
}
