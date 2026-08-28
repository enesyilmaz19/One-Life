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

        /// <summary>Zincirdeki kaçıncı bölge — 0 tabanlı.</summary>
        public int RegionIndex { get; private set; }

        /// <summary>Perdedeki bölge sayısı. `Region.All` tavan; `Tuning` kısaltabilir.</summary>
        public int RegionCount => Mathf.Clamp(_t.regionsPerAct, 1, Region.All.Length);

        /// <summary>Son bölge mi — koşu ancak burada KAZANILIYOR.</summary>
        public bool IsLastRegion => RegionIndex >= RegionCount - 1;

        public void Init(Tuning tuning, Transform player, Transform cam,
                         int startRoom = 1, int startRegion = 0)
        {
            Instance = this;
            _t = tuning;
            _player = player;
            _cam = cam;
            EnterRegion(startRegion, startRoom);
        }

        /// <summary>
        /// Bölgeye giriş: kural devreye girer, zemin rengi değişir, oda 1'den başlar.
        ///
        /// ⚠️ Kural `Region.Current` üstünden okunuyor; `Clock` ve `Enemy` oradan
        /// bakıyor. Bölgeye özel ayrı bir sistem YOK — hepsi saatten geçiyor (yasa).
        /// </summary>
        void EnterRegion(int index, int startRoom = 1)
        {
            RegionIndex = Mathf.Clamp(index, 0, RegionCount - 1);
            Region.SetCurrent(Region.At(RegionIndex));
            Bootstrap.SetGroundColor(Region.Current.GroundColor);

            Debug.Log($"[Tek Can] BÖLGE {RegionIndex + 1}/{RegionCount} — {Region.Current.Name}" +
                      (string.IsNullOrEmpty(Region.Current.RuleText) ? "" : $" ({Region.Current.RuleText})"));

            EnterRoom(startRoom);
        }

        /// <summary>
        /// Bu oda bir checkpoint mi. Greybox'ta ölçü ODA; gerçek oyunda 30dk-1sa'lik
        /// kesitler olacak (bkz. TASARIM.md).
        /// </summary>
        public bool IsCheckpointRoom => (Room - 1) % Mathf.Max(1, _t.checkpointEveryRooms) == 0;

        /// <summary>
        /// ⚠️ Checkpoint HEMEN diske yazılır. Ölüm anında yazmak yetmiyor — checkpoint
        /// de yazılmazsa oyuncu ilerlemesini kaybeder ve kalıcı ölüm "haksızlık" olur.
        /// </summary>
        void SaveCheckpoint()
        {
            var s = SaveFile.Current;
            if (s == null || Clock.Instance == null) return;

            s.checkpointRoom = Room;
            s.checkpointRegion = RegionIndex;
            s.checkpointSeconds = Clock.Instance.Seconds;

            // Zaman TEK BAŞINA yetmez: sunağa ödediğin zaman checkpoint'in ardında
            // kalırsa geri dönüş yarım olur ve ölüm sarmalı doğar.
            s.harvestRate = Clock.Instance.HarvestRate;
            s.attackDamage = Clock.Instance.AttackDamage;
            s.dashCooldown = Clock.Instance.DashCooldown;

            SaveFile.Flush();
            Debug.Log($"[Tek Can] CHECKPOINT — bölge {RegionIndex + 1} oda {Room}, " +
                      $"{s.checkpointSeconds:0.0}s, hasat %{s.harvestRate * 100f:0}, " +
                      $"hasar {s.attackDamage:0.0}");
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

            // ⚠️ Koşu YALNIZCA son bölgenin boss'u düşünce kazanılıyor. Öncekilerde
            // boss ölünce bölge biter ve çıkış açılır — zincir devam eder.
            if (IsBossRoom && IsLastRegion) { Clock.Instance.Win(); return; }

            if (_gate != null) _gate.Open();
        }

        /// <summary>
        /// `ExitGate` çağırıyor — oyuncu açık kapıya yürüdüğünde.
        /// Boss odasındaysak sıradaki BÖLGEYE, değilsek sıradaki ODAYA.
        /// </summary>
        public void NextRoom()
        {
            if (IsBossRoom) EnterRegion(RegionIndex + 1);
            else EnterRoom(Room + 1);
        }

        void EnterRoom(int index)
        {
            TearDown();

            Room = Mathf.Max(1, index);
            Cleared = false;
            Layout = RoomLayout.For(Room);

            _roomRoot = new GameObject($"Room_{Room}");
            // ⚠️ Ebeveyn yoksa KENDİMİZE bağlan. Oyunda `Systems` altındayız ve
            // `transform.parent` = Run kökü; ama kök seviyesindeki bir nesnede parent
            // NULL olur ve oda kökü sahnede ÖKSÜZ kalır — sahibi yok edilse bile
            // temizlenmez. Öz-denetimler tam öyle kuruyor ve sonraki denetime
            // kalıntı çıkış kapısı bırakıyordu (kontroller sıraya bağımlı hale geliyordu).
            _roomRoot.transform.SetParent(transform.parent != null ? transform.parent : transform);

            // Oyuncu girişte başlar; oda bir YOL, girişten çıkışa gideceksin.
            if (_player != null) _player.position = Bootstrap.Entrance(_t);

            if (IsBossRoom)
            {
                PlaceBoss();
                // Son bölge değilse boss odasının da çıkışı var: devrilince zincir sürüyor.
                if (!IsLastRegion) PlaceExit();
            }
            else { PlaceEnemies(); PlaceExit(); PlaceAltars(); }

            // Checkpoint odaya GİRERKEN yazılır, temizlenince değil: checkpoint'in anlamı
            // "buraya kadar geldin", "burayı da bitirdin" değil.
            if (IsCheckpointRoom) SaveCheckpoint();

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
            // Boss her bölgede sertleşiyor — yoksa 4. bölgenin boss'u 1. bölgeninkiyle
            // aynı olurdu ve zincirin tırmanma hissi kaybolurdu.
            float havuz = _t.bossPoolMultiplier * Mathf.Pow(_t.regionBossGrowth, RegionIndex);
            Bootstrap.SpawnEnemyAt(_t, new Vector3(0f, Bootstrap.ArenaCenter.y, _t.arenaRadius * 0.35f),
                                   _player, _cam, havuz, boss: true,
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
        // ponytail: checkpoint'e dönünce bu odanın sunakları SIFIRDAN geliyor (taban
        // fiyatla, tükenmemiş). Yani bir diriliş eşyası harcayıp aynı odanın stoğunu
        // yeniden alabilirsin. Tavan: eşyalar sonlu ve pahalı olduğu için bedeli var,
        // ama bedava değil de UCUZ kalırsa satın alma sayaçları da checkpoint'e yazılır.
        // Playtest kararı — şimdiden makine kurmak erken.
        void PlaceAltars()
        {
            // ONARIM yalnızca checkpoint odasında: güvenli noktada soluklanıp
            // ömründen verip güvenlik ağını tamir ediyorsun. Her odada olsaydı karar
            // olmaktan çıkar, rutine dönerdi.
            if (IsCheckpointRoom)
                Bootstrap.SpawnRepairAltar(_t, _player, _roomRoot.transform);

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
            // İlk bölgenin ilk odaları yalnızca TEMEL — oyun kendini önce öğretiyor.
            if (RegionIndex == 0 && room <= _t.plainRoomCount) return EnemyKind.Basic;

            // ⚠️ Tür havuzu artık BÖLGEYE bağlı, odaya değil. Enes'in kararı:
            // "bölgeye göre yenileri ekleniyor". Oda içinde de kademe var ama tavanı
            // bölge belirliyor — böylece yeni tür yeni bir MEKÂNLA birlikte geliyor.
            int tavan = Mathf.Clamp(Region.Current != null ? Region.Current.UnlockedKinds : 2,
                                    1, EnemyKind.All.Length);
            int odaKademe = Mathf.Min(tavan, 2 + Mathf.Max(0, room - _t.plainRoomCount - 1));
            return EnemyKind.All[(index + room + RegionIndex) % odaKademe];
        }

        /// <summary>HUD için.</summary>
        public string Prompt()
        {
            if (IsBossRoom)
                return IsLastRegion ? "SON BOSS" : "BÖLGE BOSS'U";
            if (Cleared) return "ÇIKIŞ AÇIK  ·  saat işlemeye devam ediyor";
            return $"oda {Room} / {_t.roomsPerRegion - 1}   ·   {Layout.Name}";
        }

        /// <summary>HUD üst satırı: bölge adı + kuralı.</summary>
        public string RegionPrompt()
        {
            var r = Region.Current;
            if (r == null) return "";
            string kural = string.IsNullOrEmpty(r.RuleText) ? "" : $"  ·  {r.RuleText}";
            return $"BÖLGE {RegionIndex + 1}/{RegionCount}  —  {r.Name}{kural}";
        }
    }
}
