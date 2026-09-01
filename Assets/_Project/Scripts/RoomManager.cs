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

        // ── ODA HESABI ───────────────────────────────────────────────────────
        // ⚠️ Ölçüm aracı, oyun mekaniği değil. Enes "giden zamana bakarak
        // yorumlayacağım" dedi — ama "azaldı" tek başına teşhis değil. Zamanın
        // NEREYE gittiğini ayırmak lazım: hasat mı yetmedi, akış mı yedi,
        // darbe mi pahalıydı. `ZorlukEgrisi` raporunun canlı karşılığı.
        float _girişSaat, _girişHasat, _girişHarcama, _girişBağış, _girişDarbe;

        /// <summary>Odaya girerken saat kaçtı, temizlendiğinde kaça düştü.</summary>
        public float RoomNet { get; private set; }
        public float RoomHarvest { get; private set; }
        public float RoomSpent { get; private set; }
        /// <summary>Sadece zamanın geçmesiyle giden — akış + aura.</summary>
        public float RoomDrain { get; private set; }
        public float RoomClosedAt { get; private set; } = -99f;

        void OpenLedger()
        {
            var c = Clock.Instance;
            if (c == null) return;
            _girişSaat = c.Seconds;
            _girişHasat = c.TotalHarvested;
            _girişHarcama = c.TotalSpent;
            _girişBağış = c.TotalGranted;
            _girişDarbe = c.TotalHitCost;
        }

        /// <summary>Bu odada dışarıdan verilen zaman (F5 ya da diriliş eşyası).</summary>
        public float RoomGranted { get; private set; }

        /// <summary>Bu odada YENEN DARBELERİN bedeli — sunak alımından ayrı.</summary>
        public float RoomHitCost { get; private set; }

        void CloseLedger()
        {
            var c = Clock.Instance;
            if (c == null) return;

            RoomNet = c.Seconds - _girişSaat;
            RoomHarvest = c.TotalHarvested - _girişHasat;
            RoomSpent = c.TotalSpent - _girişHarcama;
            RoomGranted = c.TotalGranted - _girişBağış;
            RoomHitCost = c.TotalHitCost - _girişDarbe;

            // ⚠️ Akış hiçbir sayaca yazılmıyor, KALANDAN çıkarılıyor. Bu yüzden diğer
            // bütün kalemler doğru sayılmak zorunda: bağış (F5) katılmazsa akış NEGATİF
            // çıkar, darbe ayrılmazsa akışa karışır ve "oyalanmak mı pahalı, dövüş mü"
            // sorusu cevaplanamaz. Ölçüm aracının kendisi de doğru olmalı.
            RoomDrain = RoomHarvest + RoomGranted - RoomSpent - RoomHitCost - RoomNet;
            RoomClosedAt = Time.unscaledTime;

            string bağış = RoomGranted > 0.05f ? $" · ⚠️ bağış +{RoomGranted:0.0} (test)" : "";
            Debug.Log($"[ODA HESABI] bölge {RegionIndex + 1} oda {Room} ({Layout.Name}) — " +
                      $"hasat +{RoomHarvest:0.0} · akış −{RoomDrain:0.0} · " +
                      $"darbe −{RoomHitCost:0.0} · sunak −{RoomSpent:0.0} · " +
                      $"NET {RoomNet - RoomGranted:+0.0;-0.0}" + bağış);
        }

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
            if (SkipRegionPressed()) { EnterRegion(RegionIndex + 1); return; }
            if (RefillClockPressed()) RefillClock();

            if (Cleared || AnyEnemyAlive()) return;
            OnCleared();
        }

        void OnCleared()
        {
            Cleared = true;
            CloseLedger();

            // ⚠️ Koşu YALNIZCA son bölgenin boss'u düşünce kazanılıyor. Öncekilerde
            // boss ölünce bölge biter ve çıkış açılır — zincir devam eder.
            if (IsBossRoom && IsLastRegion) { Clock.Instance.Win(); return; }

            if (_gate != null) _gate.Open();
        }

        /// <summary>
        /// Odayı aynı numarayla yeniden kurar. A/B profil değiştirici kullanıyor:
        /// arena geometrisi oda kurulurken okunuyor, canlı değişince yeniden kurulmalı.
        /// ⚠️ Saat ve diriliş eşyaları KORUNUYOR — yalnızca oda içeriği tazeleniyor.
        /// </summary>
        public void RebuildRoom() => EnterRoom(Room);

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

            OpenLedger();
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
                                       parent: _roomRoot.transform, rank: PickRank(Room, i));
        }

        void PlaceBoss()
        {
            // Boss her bölgede sertleşiyor — yoksa 4. bölgenin boss'u 1. bölgeninkiyle
            // aynı olurdu ve zincirin tırmanma hissi kaybolurdu.
            float havuz = _t.bossPoolMultiplier * Mathf.Pow(_t.regionBossGrowth, RegionIndex);

            // ⚠️ Her bölgenin KENDİ ALACAKLISI var — bölge kuralı da onun yönetiminin
            // sonucu (bkz. TASARIM.md: "kural = kimlik").
            Bootstrap.SpawnEnemyAt(_t, new Vector3(0f, Bootstrap.ArenaCenter.y, _t.arenaRadius * 0.35f),
                                   _player, _cam, havuz, boss: true,
                                   parent: _roomRoot.transform,
                                   bossKind: BossKind.For(RegionIndex));
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
        /// N — sıradaki bölgeye atla. TEST KISAYOLU, gerçek oyunda kalkacak.
        /// Bölge kurallarını hissetmek için 5 odayı temizlemek gerekmesin; kuralı
        /// yargılayamayan onu ayarlayamaz (boss'ta aynı sebeple B eklenmişti).
        /// </summary>
        static bool SkipRegionPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.nKey.wasPressedThisFrame;
        }

        /// <summary>
        /// F5 — saati başlangıç değerine doldur. TEST KISAYOLU, gerçek oyunda kalkacak.
        ///
        /// ⚠️ Zorluk AYARI DEĞİL, ölçüm aracı: Enes bütün içeriği ölmeden görüp
        /// "giden zamana bakarak" yorumlayacak. Akış hızı bozulmuyor — sadece
        /// koşu ölümle kesilmiyor. Ayar yapsaydık ölçtüğümüz şeyi değiştirirdik.
        /// </summary>
        static bool RefillClockPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.f5Key.wasPressedThisFrame;
        }

        /// <summary>
        /// ⚠️ `startSeconds` DEĞİL — o Enes'in ayarı ve sonra ölçeceğimiz şey.
        /// Onu yükseltmek ölçtüğümüz şeyi bozardı. Bu ayrı bir test sayısı.
        /// </summary>
        const float TestRefillSeconds = 300f;

        void RefillClock()
        {
            var c = Clock.Instance;
            if (c == null) return;
            c.Revive(TestRefillSeconds);
            Sfx.Buy();
            Debug.Log($"[TEST] saat dolduruldu → {TestRefillSeconds:0} sn " +
                      "(ayar değil, ölçüm sürsün diye)");
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

            // ⚠️ Oda merdiveni bir ÖNCEKİ bölgenin tavanından başlıyor, 2'den değil.
            // 2'ye sıfırlanıyordu ve `2 + (oda-2)` odaBaşına 6'yı geçemiyordu:
            // `roomsPerRegion` 6 olduğu için YEDİNCİ TÜR MATEMATİKSEL OLARAK ERİŞİLEMEZDİ
            // — bölge tavanı 7 olsa bile. Sıfırlama ayrıca öğrenmeyi geri alıyordu:
            // SIZINTI'nın ilk odası oyuncuya yine iki tür gösteriyordu.
            int taban = RegionIndex == 0 ? 2 : Region.At(RegionIndex - 1).UnlockedKinds;
            int odaKademe = Mathf.Clamp(taban + Mathf.Max(0, room - _t.plainRoomCount - 1),
                                        1, tavan);
            return EnemyKind.All[(index + room + RegionIndex) % odaKademe];
        }

        /// <summary>
        /// Odanın AĞIRLIĞI — hangi düşman zırhlı/iri doğuyor.
        ///
        /// Enes'in tespiti: *"her odada çabuk ölen az canlı düşmanlar var."* Doğru;
        /// oda kompozisyonu türü değiştiriyordu ama ağırlığı hiç değiştirmiyordu, yani
        /// her oda aynı tempoda çözülüyordu. Rütbe o tekdüzeliği kırıyor.
        ///
        /// ⚠️ İki şey bilerek SEYREK:
        ///   • Her oda değil — "ağır oda" ancak istisnaysa bir şey ifade eder.
        ///     Her odada zırh olsaydı zırh bir olay değil, yeni taban olurdu.
        ///   • Odanın hepsi değil — yanında normali durmalı ki oyuncu FARKI görsün.
        ///     Baştan sona zırhlı oda, sadece uzun oda olurdu.
        ///
        /// `PickKind` gibi deterministik: "bu oda mı ağırdı, ben mi kötü oynadım"
        /// sorusu cevaplanabilir kalmalı.
        /// </summary>
        EnemyRank PickRank(int room, int index)
        {
            // Öğretim odalarında rütbe yok — önce temel dövüş otursun.
            if (RegionIndex == 0 && room <= _t.plainRoomCount) return EnemyRank.Normal;

            if (_t.eliteRoomInterval <= 0) return EnemyRank.Normal;              // kapalı
            if ((room + RegionIndex) % _t.eliteRoomInterval != 0) return EnemyRank.Normal;
            if (index % 3 != 0) return EnemyRank.Normal;                         // odanın üçte biri

            // İkisi dönüşümlü: aynı odada ikisi birden çıkmasın, oyuncu ikisini
            // ayrı ayrı öğrensin (biri tempo vergisi, öteki taahhüt — farklı dersler).
            return (room + RegionIndex) % 2 == 0 ? EnemyRank.Armored : EnemyRank.Heavy;
        }

        /// <summary>HUD için.</summary>
        public string Prompt()
        {
            if (IsBossRoom)
            {
                // Boss'un ADI ve fazı görünmeli: hangi suçla dövüştüğün mekaniği açıklıyor.
                var b = BossKind.For(RegionIndex);
                var canlı = BossInRoom();
                string faz = canlı != null && canlı.Phase >= 2 ? "  ·  FAZ 2" : "";
                return $"{b.Name}{faz}" + (IsLastRegion ? "  (SON)" : "");
            }
            if (Cleared) return "ÇIKIŞ AÇIK  ·  saat işlemeye devam ediyor";
            return $"oda {Room} / {_t.roomsPerRegion - 1}   ·   {Layout.Name}";
        }

        /// <summary>Odadaki yaşayan boss — HUD faz durumunu ondan okuyor.</summary>
        public Enemy BossInRoom()
        {
            foreach (var e in Enemy.All)
                if (e != null && e.Boss != null) return e;
            return null;
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
