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
        static readonly Color WarnColor = new Color(0.9f, 0.55f, 1f, 0.35f);

        static readonly Color LienColor = new Color(1f, 0.45f, 0.95f, 0.35f);

        Transform _player;
        Tuning _t;
        float _radius, _cost, _timer;
        bool _fired;

        // ⚠️ Bir işaret ya OYUNCUYA ya DÜŞMANLARA vurur, asla ikisine birden.
        // Tek bayrak yerine "hasar > 0 ise düşmanlara" gibi bir çıkarım yapsaydım
        // kural görünmez olurdu; kimin vurulduğu okunabilir kalmalı.
        bool _hitsEnemies;
        float _enemyDamage, _harvestMult;

        /// <summary>DÜŞMANIN bıraktığı tebligat — OYUNCUYA vurur. (SİMSAR, MÜBAŞİR)</summary>
        public static GroundMark Spawn(Tuning t, Vector3 pos, Transform player,
                                       float radius, float delay, float cost, Transform parent)
        {
            if (player == null) return null;

            var go = new GameObject("GroundMark");
            go.transform.position = pos;
            if (parent != null) go.transform.SetParent(parent);

            var m = go.AddComponent<GroundMark>();
            m._t = t;
            m._player = player;
            m._radius = radius;
            m._cost = cost;
            m._timer = delay;

            // ⚠️ Uyarı GÖRÜNÜR ve DOLUYOR: "burada bir şey olacak" değil,
            // "burada ŞU KADAR SÜRE sonra olacak" demeli. Dolan halka süreyi söylüyor.
            Arc.Spawn(pos, Quaternion.identity, radius, 360f, WarnColor, delay,
                      scaleFrom: 0.25f, scaleTo: 1f);
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

            // Oyuncununki KENDİ RENGİNDE (İPOTEK moru) — düşmanın tebligatıyla
            // karışsaydı oyuncu kendi koyduğu işaretten kaçmaya çalışırdı.
            Arc.Spawn(pos, Quaternion.identity, radius, 360f, LienColor, delay,
                      scaleFrom: 0.25f, scaleTo: 1f);
            return m;
        }

        void Update()
        {
            if (_fired) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            _fired = true;
            Detonate();

            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        /// <summary>Öz-denetim dt'yi kendisi veriyor — editörde `Time.deltaTime` 0.</summary>
        public void Step(float dt)
        {
            if (_fired) return;
            _timer -= dt;
            if (_timer > 0f) return;
            _fired = true;
            Detonate();
        }

        public bool Fired => _fired;

        /// <summary>Kaç düşman vuruldu — öz-denetim ETKİYİ buradan ölçüyor.</summary>
        public int EnemiesHit { get; private set; }

        void Detonate()
        {
            Arc.Spawn(transform.position, Quaternion.identity, _radius, 360f,
                      new Color(1f, 0.4f, 0.9f, 0.55f), 0.25f, scaleFrom: 0.9f, scaleTo: 1.25f);
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

            _player.GetComponent<PlayerController>()?.TakeHit(_cost, transform.position);
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
                EnemiesHit++;
            }
        }
    }
}
