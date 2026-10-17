using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Yere bırakılan gecikmeli patlama. SİMSAR'ın işi: **durduğun yeri işaretliyor.**
    ///
    /// ⚠️ Bu mekanik doğrudan bir AÇIĞI kapatıyor. Enes testte şunu bildirdi:
    /// *"durduğum yerde vurarak zamanımı 1200'e çıkardım."* Bütün boss'lar oyuncuya
    /// gidiyordu, yani sabit durup vurmak geçerli bir strateji kalıyordu.
    /// SİMSAR sana gelmiyor — DURDUĞUN YERE geliyor. Tek cevabı hareket etmek.
    ///
    /// `Arc` gibi kendi kendini doğuran ve sönen bir nesne; hazırlık boyunca
    /// büyüyen halka uyarıyor, süre dolunca patlıyor.
    /// </summary>
    public class GroundMark : MonoBehaviour
    {
        // ⚠️ DÜŞMANIN işareti TEHLİKE renginde (boss halkalarının turuncu-kırmızısı),
        // OYUNCUNUN ipoteği mor. Faz 25'e kadar ikisi de mordu: (0.9, 0.55, 1) ile
        // (1, 0.45, 0.95) — yorum "karışmasın diye kendi renginde" diyordu, sayılar
        // aynıydı. Enes SİMSAR'ın işaretini "İPOTEK bug'ı" sandı; SİMSAR'ın böyle bir
        // saldırısı olduğunu hiç fark etmemişti (2026-09-30). Faz25SelfCheck tonu ölçüyor.
        static readonly Color WarnColor = new Color(1f, 0.32f, 0.08f, 0.38f);
        static readonly Color WarnBlastColor = new Color(1f, 0.3f, 0.1f, 0.6f);

        static readonly Color LienColor = new Color(1f, 0.45f, 0.95f, 0.35f);
        static readonly Color LienBlastColor = new Color(1f, 0.4f, 0.9f, 0.55f);

        Transform _player;
        Enemy _owner;       // bırakan — boss ise nemesis hesabına yazılır
        Tuning _t;
        float _radius, _cost, _timer;
        bool _fired;

        // ⚠️ Bir işaret ya OYUNCUYA ya DÜŞMANLARA vurur, asla ikisine birden.
        // Tek bayrak yerine "hasar > 0 ise düşmanlara" gibi bir çıkarım yapsaydım
        // kural görünmez olurdu; kimin vurulduğu okunabilir kalmalı.
        bool _hitsEnemies;
        float _enemyDamage, _harvestMult;

        /// <summary>
        /// Şu an olgunlaşmayı bekleyen OYUNCU ipoteği sayısı.
        ///
        /// ⚠️ Enes: *"ipotek solo boss fightlarda bile spam yapınca çok vuruyor,
        /// 1k pasif stacklemiş Veigar W'su gibi bonk bonk bonk."* Sebep sayı değil
        /// YAPI: gecikme 0.85 sn, savurma 0.35 sn — yani ilk işaret patlamadan
        /// üç tane daha koyabiliyordun ve hepsi aynı noktada üst üste patlıyordu.
        /// Hasat çarpanını kısmak bunu çözmez, sadece geciktirir.
        /// </summary>
        /// ⚠️ SAYAÇ PATLAMADAN YOK EDİLEN İŞARETİ GÖREMEZ. İşaret oda değişiminde,
        /// restart'ta ve yeni koşuda patlamadan ölüyor; her biri sayacı bir artırılmış
        /// bırakırdı ve birkaç odadan sonra `ActiveLiens` kalıcı &gt;0 takılıp
        /// **İPOTEK'i tamamen çalışmaz** hale getirirdi — sessizce: savuruyorsun,
        /// hiçbir şey olmuyor.
        ///
        /// Çözüm `OnDestroy` DEĞİL: Unity edit modda o kancayı çağırmıyor, yani
        /// koruma test edilemez olurdu (aynı sebeple `Update` yerine `Step(dt)` var).
        /// Bunun yerine işaretleri yok eden ÜRETİM YOLLARI sayacı sıfırlıyor:
        /// `RoomManager.TearDown` ve `Bootstrap.ResetStatics`. İkisi de test edilebilir.
        public static int ActiveLiens { get; private set; }
        public static void ResetLiens() => ActiveLiens = 0;

        /// <summary>DÜŞMANIN bıraktığı tebligat — OYUNCUYA vurur. (SİMSAR, MÜBAŞİR)</summary>
        public static GroundMark Spawn(Tuning t, Vector3 pos, Transform player,
                                       float radius, float delay, float cost, Transform parent,
                                       Enemy owner = null)
        {
            if (player == null) return null;

            var go = new GameObject("GroundMark");
            go.transform.position = pos;
            if (parent != null) go.transform.SetParent(parent);

            var m = go.AddComponent<GroundMark>();
            m._t = t;
            m._player = player;
            m._owner = owner;
            m._radius = radius;
            m._cost = cost;
            m._timer = delay;

            // ⚠️ Uyarı GÖRÜNÜR ve DOLUYOR: "burada bir şey olacak" değil,
            // "burada ŞU KADAR SÜRE sonra olacak" demeli. Dolan halka süreyi söylüyor.
            // ⚠️ `scaled`: sayaç oyun zamanıyla işliyor, halka da öyle dolmalı.
            Arc.Spawn(pos, Quaternion.identity, radius, 360f, WarnColor, delay,
                      scaleFrom: 0.25f, scaleTo: 1f, scaled: true);
            return m;
        }

        /// <summary>
        /// OYUNCUNUN koyduğu ipotek — DÜŞMANLARA vurur. (İPOTEK silahı)
        ///
        /// ⚠️ Aynı sınıf bilerek: oyuncu bu mekaniği önce KENDİ ÜZERİNDE öğreniyor
        /// (SİMSAR, MÜBAŞİR), sonra eline alıyor. Ayrı bir sınıf yazsaydım aynı
        /// zamanlama ve aynı uyarı görselini ikinci kez uygulamam gerekirdi ve
        /// ikisi zamanla ayrışırdı.
        /// </summary>
        public static GroundMark SpawnLien(Tuning t, Vector3 pos, float radius, float delay,
                                           float damage, float harvestMult, Transform parent)
        {
            var go = new GameObject("Lien");
            go.transform.position = pos;
            if (parent != null) go.transform.SetParent(parent);

            var m = go.AddComponent<GroundMark>();
            m._t = t;
            m._radius = radius;
            m._timer = delay;
            m._hitsEnemies = true;
            m._enemyDamage = damage;
            m._harvestMult = harvestMult;
            ActiveLiens++;

            // Oyuncununki KENDİ RENGİNDE (İPOTEK moru) — düşmanın tebligatıyla
            // karışsaydı oyuncu kendi koyduğu işaretten kaçmaya çalışırdı.
            Arc.Spawn(pos, Quaternion.identity, radius, 360f, LienColor, delay,
                      scaleFrom: 0.25f, scaleTo: 1f, scaled: true);
            return m;
        }

        // Patlama mantığı TEK yerde (`Step`) — `Arc`/`Mote` ile aynı desen.
        void Update()
        {
            Step(Time.deltaTime);
            if (_fired) Destroy(gameObject);
        }

        /// <summary>Öz-denetim dt'yi kendisi veriyor — editörde `Time.deltaTime` 0.</summary>
        public void Step(float dt)
        {
            if (_fired) return;
            _timer -= dt;
            if (_timer > 0f) return;
            _fired = true;
            if (_hitsEnemies) ActiveLiens = Mathf.Max(0, ActiveLiens - 1);
            Detonate();
        }

        public bool Fired => _fired;

        // ── COMBO kancaları (Faz 30) ──
        /// <summary>TAHLİYE (HACİZ KARARI+): patlama içindekileri dışa iter.</summary>
        public float İtme { get; set; }
        public bool Bekliyor => _hitsEnemies && !_fired;
        public float Yarıçap => _radius;

        /// <summary>HACİZ KESİNLEŞTİ: bekleyen ipotek şimdi patlıyor, hasarı ×çarpan.</summary>
        public void HemenPatlat(float hasarÇarpanı)
        {
            if (_fired) return;
            _enemyDamage *= hasarÇarpanı;
            _timer = 0f;
            Step(0f);
        }

        /// <summary>Bu noktayı içine alan, henüz patlamamış oyuncu ipoteği (yoksa null).</summary>
        public static GroundMark İçindekiİpotek(Vector3 nokta)
        {
            foreach (var m in FindObjectsByType<GroundMark>(FindObjectsSortMode.None))
            {
                if (m == null || !m.Bekliyor) continue;
                var d = nokta - m.transform.position;
                d.y = 0f;                                    // tuzak 2
                if (d.magnitude <= m._radius) return m;
            }
            return null;
        }

        /// <summary>Oyuncuya mı vuruyor (düşmanın tebligatı) — otopilot bundan kaçıyor.</summary>
        public bool OyuncuyaVurur => !_hitsEnemies;

        /// <summary>Patlamaya kalan süre — otopilot "çıkmaya yetişir miyim" diye soruyor.</summary>
        public float Kalan => _timer;

        /// <summary>Kaç düşman vuruldu — öz-denetim ETKİYİ buradan ölçüyor.</summary>
        public int EnemiesHit { get; private set; }

        void Detonate()
        {
            Arc.Spawn(transform.position, Quaternion.identity, _radius, 360f,
                      _hitsEnemies ? LienBlastColor : WarnBlastColor, 0.25f, scaleFrom: 0.9f, scaleTo: 1.25f);
            Sfx.Slam();

            if (_hitsEnemies) { VurDüşmanlara(); return; }

            if (_player == null) return;

            // ⚠️ YATAY mesafe — y'nin yatay hesaba karışması bu projede dört kez hata üretti.
            //
            // ⚠️ VE BU ÖLÇÜM OYUNCUNUN MERKEZİNE — `VurDüşmanlara` ise düşmanın
            // YÜZEYİNE bakıyor. Tutarsızlık DEĞİL, düzeltilmemeli:
            //   • Oyuncuya vuran işaret merkezi ölçer, çünkü izometrikte oyuncu kendi
            //     AYAK NOKTASINI okuyor; "merkezin halkaya girmesin" dürüst bir söz.
            //     (Boss'un simit göbeği de aynı gerekçeyle merkeze ölçüyor.)
            //   • Düşmana vuran işaret yüzeyi ölçer, çünkü oyuncu GÖRDÜĞÜ GÖVDEYE
            //     nişan alıyor; İRİ rütbeli düşman halkaya taşmışken vurulmaması
            //     boss isabetindeki ölü bandın aynısı olurdu (ders 9).
            // Biri "tutarlılık" diye ötekine çevrilirse isabet oranları sessizce kayar.
            var d = _player.position - transform.position;
            d.y = 0f;
            if (d.magnitude > _radius) return;

            _player.GetComponent<PlayerController>()?.TakeHit(_cost, transform.position, _owner);
        }

        void VurDüşmanlara()
        {
            // ⚠️ Yarıçap düşmanın YÜZEYİNE ölçülüyor, merkezine değil — İRİ rütbeli
            // düşman halkanın kenarına taşmışken vurulmamak yanlış olurdu. Oyuncunun
            // savurma isabetinde de kural bu (ders 9: aynı kavram, aynı ölçüm).
            for (int i = Enemy.All.Count - 1; i >= 0; i--)
            {
                var e = Enemy.All[i];
                if (e == null) continue;

                var d = e.transform.position - transform.position;
                d.y = 0f;
                if (d.magnitude - e.Radius > _radius) continue;

                e.TakeDamage(_enemyDamage, _harvestMult);
                if (İtme > 0f) e.Push(d, İtme);              // TAHLİYE (Faz 30)
                EnemiesHit++;
            }
        }
    }
}
