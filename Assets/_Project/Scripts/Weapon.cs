using UnityEngine;

namespace TekCan
{
    /// <summary>
    /// Silahlar. İlke: silah *göründüğü* için değil **kararlarını değiştirdiği** için özgün olur.
    ///
    /// ⚠️ YAPISAL ÇÖZÜM — `HarvestMultiplier`.
    /// Koni saldırısı içindeki herkese vurup her birinden ayrı hasat ediyor, yani hedef
    /// sayısı kadar üstün. Tek hedefli silahlar ancak **hasar başına daha çok zaman
    /// çalarsa** rekabet edebilir. Bu çarpan olmadan koni her zaman baskın olur ve
    /// kalabalık oyunu kolaylaştırır — roguelite'ta tam tersi olması gereken şey.
    ///
    /// Denge iddiası tahmin değil: `Tek Can → Denge simülasyonu` her silahı tarıyor.
    ///
    /// ⚠️ **DURGUN KALDIRILDI** (2026-09-02, Enes: *"sadece akan zamanı durdurmak bir
    /// işimize yaramaz"*). Kodun içindeki *"DURGUN dersi"* yorumları BİLEREK duruyor:
    /// o silah "özelliği değil ETKİYİ test et" dersini öğretti (tamamen kırık teslim
    /// edilmişti, testler yeşildi çünkü hiçbiri *"bu silah vurabiliyor mu"* diye
    /// sormuyordu). Ders `tasks/lessons.md`'de; silah gitti, ders kaldı.
    ///
    /// ⚠️ Bu yorum `Weapon` sınıfına ait; aşağıdaki `WeaponSpecial` araya SONRADAN
    /// girdi (Faz 16) ve bir süre yorumu sahiplenmiş görünüyordu. Sınıf sırası
    /// değişince belgenin de taşınması gerekiyor — yoksa dosyayı açan kişi yanlış
    /// sınıfın gerekçesini okur.
    /// </summary>

    /// <summary>
    /// Sağ tık saldırısı — silahın İKİNCİ hamlesi. `BossAttack` ile aynı desen:
    /// veri sınıfı, davranış değil. `PlayerController` bunu okuyup uyguluyor,
    /// yani silah adı geçen tek yer yine `Weapon.cs`.
    ///
    /// ⚠️ TASARIM KURALI: özel saldırı, silahın ZATEN SORDUĞU SORUYU güçlendirir;
    /// yeni bir soru AÇMAZ. Açsaydı beş silah on silah gibi davranırdı ve hiçbiri
    /// kendi kimliğini taşımazdı — TAKSİT'in elenme sebebinin aynısı.
    /// </summary>
    public sealed class WeaponSpecial
    {
        public string Name = "";

        /// <summary>Kaç saniyede bir kullanılabilir — silahtan silaha değişir (Enes).</summary>
        public float Cooldown = 6f;

        public float RangeMul = 1f;
        /// <summary>Mutlak derece. 360 = çevrende halka.</summary>
        public float Angle = 0f;             // 0 = silahın kendi açısı
        public int MaxTargets = int.MaxValue;

        public float DamageMul = 1f;
        public float HarvestMul = 1f;
        public float Knockback = 0f;

        /// <summary>Peşin bedel (saniye). Kullanmadan ödenir; ödeyemiyorsan çalışmaz.</summary>
        public float Cost = 0f;

        /// <summary>
        /// &gt;0 ise peşin bedel saatin bu payı (en az `Cost`) — İCRA (Faz 29). Yüzde olduğu için
        /// seni hiçbir zaman öldürmez: ne kadar azsan o kadar az ödersin.
        /// </summary>
        public float CostPercent = 0f;

        /// <summary>Ödenen her saniye için ek hasar — "hasar ödenen bedele bağlı" (İCRA).</summary>
        public float DamagePerCost = 0f;

        /// <summary>Öldürünce bedelin bu katı geri gelir. 0 = iade yok.</summary>
        public float KillRefundMul = 0f;

        /// <summary>
        /// &gt;0 ise koni değil GECİKMELİ YER VURUŞU bırakır (İPOTEK). `Hits` &gt; 1 ile YAĞMUR olur:
        /// `RangeMul` alanındaki düşmanların üstüne `HitInterval` arayla peş peşe işaret.
        /// </summary>
        public float GroundDelay = 0f;
        public float GroundRadius = 0f;

        /// <summary>
        /// &gt;0 ise hasar hedefin UZAKLIĞIYLA artar (HACİZ).
        /// Silahın sorusunu tersine çeviriyor: "yaklaşmalı mıyım" → "UZAKLAŞ".
        /// </summary>
        public float DamagePerDistance = 0f;

        /// <summary>
        /// Kaç vuruş arka arkaya (1 = tek vuruş) ve aralarındaki saniye.
        ///
        /// ⚠️ İFLAS'ın kimliği bu: oyunun EN HIZLI silahı, o yüzden özeli de
        /// "büyük tek vuruş" değil SERİ olmalı (Enes kararı). İlk yazdığımda ona
        /// da ağır bir tek vuruş vermiştim — silahın ne olduğuna değil, "özel
        /// saldırı böyle olur"a bakmışım. Özel, silahın kendi karakterinin
        /// abartılmışı olmalı; onu tersine çeviren değil.
        /// </summary>
        public int Hits = 1;
        public float HitInterval = 0.09f;

        /// <summary>Ekrandaki yayın rengi — silahın kendi tonundan ayrışsın diye.</summary>
        public Color Tint = new Color(1f, 1f, 1f, 0.5f);
    }

    /// <summary>
    /// Faz 29 — SİLAHA ÖZEL YÜKSELTMELER (Enes, eski Faz 20: "boss sonrası sunak, silaha özel
    /// yetenekler"). Her silahın üçü; sıra `Weapon.All` sırası × 3. Ana boss düşünce boss odası
    /// SİLAH SUNAĞI'na dönüyor ve KUŞANDIĞIN silahınkileri satıyor. Koşu boyu kalıyor: ana
    /// boss'un temizlediği bölge geri gelmediği için hiçbir ölüm onları geri almıyor.
    /// ⚠️ Yalnızca kuşanılan silahınki geçerli — etkiler `PlayerController`'da, silahın kendi
    /// verisi (`Weapon`) saf kalıyor (silah sınıfları `Clock`/kayıt okumuyor).
    /// </summary>
    public enum SilahYükseltme
    {
        GenişAğ, KısaSüpürme, GeriTepme,       // KONİ
        Bileşik, KısaİCRA, Temerrüt,           // FAİZLİ
        UzunKol, Delici, DarÖlüBölge,          // HACİZ
        ÇiftTapu, HızlıTapu, GenişArsa,        // İPOTEK
        Dip, Seri, Kapanış                     // İFLAS
    }

    public static class SilahSunağı
    {
        public const int SilahBaşına = 3;

        public static readonly string[] Ad =
        {
            "GENİŞ AĞ", "KISA SÜPÜRME", "GERİ TEPME",
            "BİLEŞİK", "KISA İCRA", "TEMERRÜT",
            "UZUN KOL", "DELİCİ", "DAR ÖLÜ BÖLGE",
            "ÇİFT TAPU", "HIZLI TAPU", "GENİŞ ARSA",
            "DİP", "SERİ", "KAPANIŞ",
        };

        public static readonly string[] NeYapar =
        {
            "savurma açısı +%35", "SÜPÜRME beklemesi −%30", "savurma da iter, SÜPÜRME itmesi +%60",
            "öldürünce iade ×2 → ×3", "İCRA beklemesi −%30", "ıskalayınca bedelin yarısı geri",
            "menzil +%25", "iki hedefe vurur", "ölü bölge yarıya",
            "aynı anda iki ipotek", "ipotek gecikmesi −%30", "ipotek alanı +%30",
            "dipte daha erken sertleşir (eşikler ×1.5)", "TASFİYE +2 vuruş", "dipte hasat +%50",
        };

        public static int Toplam => Weapon.All.Length * SilahBaşına;
        public static int Silahı(SilahYükseltme y) => (int)y / SilahBaşına;
        public static SilahYükseltme Yuvası(int silah, int yuva) => (SilahYükseltme)(silah * SilahBaşına + yuva);

        public static bool Var(SilahYükseltme y)
        {
            var s = SaveFile.Current;
            int i = (int)y;
            return s != null && s.weaponUpgrades != null && i < s.weaponUpgrades.Length && s.weaponUpgrades[i] > 0;
        }

        public static int AlınanSayısı
        {
            get
            {
                var s = SaveFile.Current;
                int n = 0;
                if (s != null && s.weaponUpgrades != null) foreach (var v in s.weaponUpgrades) if (v > 0) n++;
                return n;
            }
        }

        /// <summary>Bu bölgenin boss'undan sonra kaç yükseltme alındı (boss başına sınır için).</summary>
        public static int BölgeAlımı(int bölge)
        {
            var s = SaveFile.Current;
            return s != null && s.weaponAltarBought != null && bölge < s.weaponAltarBought.Length
                 ? s.weaponAltarBought[bölge] : 0;
        }

        /// <summary>Yükseltmeyi kayda yaz ve bölgenin alım sayısını artır — kaydın TEK yazma yolu.</summary>
        public static void Al(SilahYükseltme y, int bölge)
        {
            var s = SaveFile.Current;
            if (s == null) return;
            s.weaponUpgrades = Sığdır(s.weaponUpgrades, Toplam);
            s.weaponAltarBought = Sığdır(s.weaponAltarBought, bölge + 1);
            s.weaponUpgrades[(int)y] = 1;
            s.weaponAltarBought[bölge]++;
        }

        static int[] Sığdır(int[] dizi, int n)
        {
            if (dizi != null && dizi.Length >= n) return dizi;
            var yeni = new int[n];
            if (dizi != null) System.Array.Copy(dizi, yeni, dizi.Length);
            return yeni;
        }
    }

    public abstract class Weapon
    {
        public abstract string Name { get; }
        public abstract Color Tint { get; }

        /// <summary>Bir savurmada en fazla kaç düşman. 1 = tek hedef (en yakını seçilir).</summary>
        public virtual int MaxTargets => int.MaxValue;

        /// <summary>Hasar başına çalınan zamanın çarpanı — silahlar arası dengenin ana kadranı.</summary>
        public virtual float HarvestMultiplier => 1f;

        public virtual float Range(Tuning t) => t.attackRange;
        public virtual float Angle(Tuning t) => t.attackAngle;
        public virtual float Duration(Tuning t) => t.attackDuration;
        public virtual float Damage(Tuning t, float baseDamage) => baseDamage;

        /// <summary>Savurma başlarken. `false` dönerse saldırı hiç başlamaz (ödeyemedin).</summary>
        public virtual bool OnSwingStart(Clock clock, Tuning t) => true;

        /// <summary>Vuruş indikten sonra — isabet sayısı ve öldürme sayısıyla.</summary>
        public virtual void OnStrike(Clock clock, Tuning t, int hits, int kills) { }

        // ── DARBE AĞIRLIĞI ────────────────────────────────────────────────────
        // Enes: *"faizli de kodum mu oturtsun cinsinden olsun."* Ağırlık hissi
        // hasardan gelmiyor — HIT-STOP ve SARSINTIDAN geliyor. Bunlar sabitti, yani
        // bütün silahlar aynı ağırlıkta vuruyordu ve tek hedefli silahın "tek ama
        // sert" olma iddiası ekranda hiç görünmüyordu.

        public virtual float HitStopMultiplier => 1f;
        public virtual float ShakeMultiplier => 1f;

        /// <summary>
        /// Vurduğunu ne kadar GERİ SAVURUYOR (birim/sn).
        ///
        /// ⚠️ Ağırlık hissinin en büyük parçası buydu ve oyunda HİÇ YOKTU: düşmanı
        /// iten bir şey yoktu, yalnızca oyuncu itiliyordu. İlk denemede hit-stop ve
        /// sarsıntıyı büyüttüm, Enes yine *"oturtmuyor"* dedi — çünkü var olanı
        /// büyütmek, OLMAYANIN yerini tutmuyor.
        /// </summary>
        public virtual float Knockback(Tuning t) => 0f;

        /// <summary>Sağ tık saldırısı. null = yok (hiçbir silahta olmamalı).</summary>
        /// <remarks>⚠️ Her çağrıda YENİDEN kuruluyor, önbellek yok. Önbellekliyken ilk
        /// verilen `Tuning`'i sonsuza dek tutuyordu: Inspector'daki canlı ayar (F1 köprüsü)
        /// ve F2-F4 profilleri özel saldırılara HİÇ geçmiyordu (denetim 2026-09-23).</remarks>
        // ponytail: HUD her OnGUI olayında 5-6 küçük nesne kuruyor. Profilde görünürse
        // önbelleği Tuning DEĞERLERİNE bağla (referansına değil).
        public virtual WeaponSpecial Special(Tuning t) => null;

        /// <summary>
        /// Dash-atağın yerine geçen özel — null ise dash-atak silahın normal yayı. Yalnızca
        /// İPOTEK (Faz 29): eski sağ tığı HACİZ İHBARI dash-atağa taşındı (Enes).
        /// </summary>
        public virtual WeaponSpecial DashSpecial(Tuning t) => null;

        // ── DASH-ATAK KİMLİĞİ ────────────────────────────────────────────────
        // Enes: *"her bir silahın özel dash attack olsun, burada çok belli olmuyor
        // neyin ne yaptığı."*
        //
        // ⚠️ Beş silaha beş yeni davranış YAZILMADI: ikisinin dash'i zaten kendi
        // kimliğini taşıyor (KONİ süpürüyor, İFLAS saate göre sertleşiyor). Yalnızca
        // AYRIŞMAYAN üçüne kanca eklendi. Olanı tekrar yazmak, olmayanı eklemekten
        // daha kötü — beşi de "özel" görünür ama üçü aynı şeyi yapardı.

        /// <summary>Dash-atak isabet ederse savurma bedeli geri gelir (FAİZLİ).</summary>
        public virtual bool DashRefundsCost => false;

        /// <summary>
        /// &gt;0 ise bu mesafeden YAKINDAKİNE vuramaz — menzilli silahın ölü bölgesi.
        ///
        /// ⚠️ Yalnızca NORMAL savurmaya uygulanıyor, dash-atağa değil. Sebebi tasarım:
        /// "biri dibine girdi" durumunun bir CEVABI olmalı, yoksa silah çaresiz kalır.
        /// Dash o cevap. Böylece menzilli silah bir mesafe BANDI oynatıyor.
        /// </summary>
        public virtual float MinRange(Tuning t) => 0f;

        // ── YER VURUŞU (İPOTEK) ───────────────────────────────────────────────
        // >0 ise savurma koni vurmaz: nişan noktasına GECİKMELİ bir alan vuruşu
        // bırakır. Düşmanların gecikme boyunca kaçma şansı var — yani silah
        // "nereye vuruyorum" değil "nereye GİDECEKLER" diye sordurur.

        public virtual float GroundMarkDelay(Tuning t) => 0f;
        public virtual float GroundMarkRadius(Tuning t) => 0f;

        /// <summary>
        /// Saat düştükçe hasar çarpanı (İFLAS). 1 = değişmiyor.
        ///
        /// ⚠️ Saniye DIŞARIDAN veriliyor, `Clock.Instance` okunmuyor. Sebep projenin
        /// kendi kuralı: zaman güdümlü şey yazıyorsan girdiyi dışarıdan al, yoksa
        /// editör modunda ETKİ ölçülemez, yalnızca ÖZELLİK sorulabilir. Denge
        /// simülasyonu da bu sayede silahı gerçek eğrisiyle tarayabiliyor.
        /// </summary>
        public virtual float DesperationMultiplier(float seconds, Tuning t) => 1f;

        /// <summary>
        /// HUD etiketi — silahın kimliğini KENDİ verisinden türetiyor.
        /// Ayrı bir `IsRanged`/`IsArea` bayrağı olsaydı asıl alanlardan ayrışır ve
        /// etiket sessizce yalan söylemeye başlardı.
        /// </summary>
        public string Category(Tuning t) =>
              GroundMarkDelay(t) > 0f ? "yer"
            : MinRange(t) > 0f        ? "menzil"
            : MaxTargets == 1         ? "tek"
                                      : "koni";

        // ── DASH-ATAK: silaha göre değişir ────────────────────────────────────
        // Eskiden her silahta aynı geniş alan vuruşuydu; tek hedefli bir silahın
        // dash'te aniden koni olması silahın kimliğini siliyordu.
        // Kural: silah tek hedefliyse dash-atağı da tek hedefli kalır ama ÇOK sert vurur;
        // koni silahın dash-atağı süpürür ama vuruş başına daha az.

        // ⚠️ MENZİLLİ SİLAHIN DASH'İ ONUN YAKIN CEVABIDIR. HACİZ dash'te ölü
        // bölgesini tanımıyor, İPOTEK dash'te yer işareti değil doğrudan yay
        // vuruyor. Kural ikisinde de aynı: uzaktan iş gören silah dibine biri
        // girdiğinde çaresiz kalmamalı, ama bunun bedeli dash'i harcamak olsun.
        // Öz-denetim ikisini de ölçüyor.

        public virtual int DashTargets => MaxTargets;
        public virtual float DashAngle(Tuning t) => MaxTargets == 1 ? t.singleTargetAngle * 1.6f
                                                                   : t.dashAttackAngle;
        public virtual float DashDamageMultiplier(Tuning t) => MaxTargets == 1
            ? t.dashAttackDamageMult * 1.8f      // tek hedefli: tek isabet, ağır darbe
            : t.dashAttackDamageMult;            // koni: çok isabet, hafif darbe

        public static Weapon[] All => _all ??= new Weapon[]
        {
            new ConeBlade(), new Faizli(), new Haciz(), new Ipotek(), new Iflas()
        };
        static Weapon[] _all;
    }

    /// <summary>Faz 1-2'nin temel silahı. Kıyas noktası: geniş koni, herkese vurur, çarpan yok.</summary>
    public sealed class ConeBlade : Weapon
    {
        public override string Name => "KONİ";
        public override Color Tint => new Color(0.65f, 0.9f, 1f, 0.42f);

        /// SÜPÜRME — çevrendeki HERKESE. Sorusu "kaç kişiye birden vurabilirim";
        /// halka o sorunun azamisi. Konumlanmayı ödüllendiriyor: topla, sonra bas.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "SÜPÜRME", Cooldown = t.specialCooldownCone,
            Angle = 360f, RangeMul = 1.5f, DamageMul = 1.15f, Knockback = 5f,
            Tint = new Color(0.65f, 0.95f, 1f, 0.5f)
        };
        // MaxTargets sınırsız, HarvestMultiplier 1 — dengenin sıfır noktası.
    }

    /// <summary>
    /// FAİZLİ — savurmak saniye götürür, öldürünce iki katı geri verir.
    /// Iskalamak pahalı: her boşa savurma doğrudan ömürden kesiliyor.
    /// Tek hedef olduğu için hasat çarpanı yüksek.
    /// </summary>
    public sealed class Faizli : Weapon
    {
        public override string Name => "FAİZLİ";
        public override Color Tint => new Color(1f, 0.85f, 0.35f, 0.5f);
        public override int MaxTargets => 1;
        public override float HarvestMultiplier => 2.6f;
        public override float Angle(Tuning t) => t.singleTargetAngle;

        // ⚠️ "Oturtsun" (Enes). Ağırlık HASARDAN değil DARBEDEN geliyor: hit-stop
        // uzuyor, ekran daha çok sarsılıyor, hasar da artıyor. Üçü birlikte tek
        // vuruşu olay yapıyor — tek hedefli silahın varlık sebebi bu.
        public override float Damage(Tuning t, float baseDamage) => baseDamage * t.faizliDamageMult;
        public override float HitStopMultiplier => 2.4f;
        public override float ShakeMultiplier => 2.6f;
        public override float Knockback(Tuning t) => t.faizliKnockback;   // asıl "oturtan" şey

        /// İCRA — bahsi büyütüyor. Faz 29 (Enes): bedel SAATİNİN YÜZDESİ, hasar ÖDEDİĞİNE bağlı —
        /// ne kadar çok yatırırsan o kadar sert. Öldürürsen bedelin 1.5 katı geri, ama saat doyma
        /// tavanının üstündeyse geri vermez ("eşiğin üstündeysen zaman vermez"). Sorusu yine
        /// "bu savurma tutar mı"; zenginken bahis büyüyor ama kazancı kalkıyor.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "İCRA", Cooldown = t.specialCooldownFaizli,
            MaxTargets = 1, Angle = t.singleTargetAngle * 0.8f, RangeMul = 1.3f,
            DamageMul = 1f, DamagePerCost = t.icraDamagePerCost, HarvestMul = 1.2f,
            Knockback = t.faizliKnockback * 1.8f,
            Cost = t.faizliSwingCost * 3f, CostPercent = t.icraCostPercent, KillRefundMul = t.icraKillRefund,
            Tint = new Color(1f, 0.8f, 0.25f, 0.6f)
        };

        /// ⚠️ Dash-atak isabet ederse savurma bedeli GERİ GELİYOR. FAİZLİ'nin sorusu
        /// "tutar mı"; dash onu en zor haliyle soruyor (hareket halinde, tek hedef)
        /// ve doğru cevabı ödüllendiriyor.
        public override bool DashRefundsCost => true;

        public override bool OnSwingStart(Clock clock, Tuning t)
        {
            // Ödeyemiyorsan savuramıyorsun — silah seni öldürmüyor, sadece susuyor.
            if (clock == null) return true;
            if (clock.Seconds <= t.faizliSwingCost) return false;
            clock.Spend(t.faizliSwingCost);
            return true;
        }

        public override void OnStrike(Clock clock, Tuning t, int hits, int kills)
        {
            if (kills > 0) clock?.Add(t.faizliSwingCost * 2f * kills);
        }
    }

    /// <summary>
    /// HACİZ — uzaktan, dar ve tek hedef. **Dibindekine vuramaz.**
    ///
    /// Sorduğu soru: **"yaklaşmalı mıyım?"** Bu güne kadar dört silahın dördü de yakın
    /// dövüştü, yani *uzak durmak* bedava bir cevaptı — hiçbir silah onu ödüllendirmiyor
    /// da, cezalandırmıyor da değildi, sadece alakasızdı. HACİZ mesafeyi bir TERCİH
    /// haline getiriyor: bir bandın içinde kalman gerekiyor.
    ///
    /// MÜBAŞİR'in (Faz 13) tam karşılığı — o da uzaktan vuruyor, mesafesini koruyor.
    /// Artık oyuncunun elinde de aynı alet var.
    ///
    /// ⚠️ Ölü bölgesi **görünüyor** (savurma yayının içinde ikinci bir halka). Görünmeseydi
    /// oyuncu dibindeki düşmana savurup boşa ıskalar ve sebebini asla anlamazdı —
    /// ders 10'un aynısı: mekanik doğru, görsel yalan.
    /// </summary>
    public sealed class Haciz : Weapon
    {
        public override string Name => "HACİZ";
        public override Color Tint => new Color(0.55f, 1f, 0.7f, 0.42f);
        public override int MaxTargets => 1;
        public override float HarvestMultiplier => 2.2f;
        public override float Range(Tuning t) => t.attackRange * 3.4f;        // Enes: menzil buffu
        public override float Angle(Tuning t) => t.singleTargetAngle * 0.6f;  // dar: nişan
        public override float MinRange(Tuning t) => t.hacizMinRange;          // ölü bölge
        public override float Duration(Tuning t) => t.attackDuration * 1.15f; // nişan alıyor

        /// UZUN VADE — hasar hedefin UZAKLIĞIYLA artıyor. Silahın sorusunu
        /// ("yaklaşmalı mıyım") tersine çevirip cevabını veriyor: UZAKLAŞ.
        /// Ölü bölgesi olan silahın en uzaktan en sert vurması, mesafe bandını
        /// ceza olmaktan çıkarıp SİLAH haline getiriyor.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "UZUN VADE", Cooldown = t.specialCooldownHaciz,
            MaxTargets = 1, Angle = t.singleTargetAngle * 0.45f, RangeMul = 1.5f,
            DamageMul = 0.8f, DamagePerDistance = 0.42f, HarvestMul = 1.1f,
            Tint = new Color(0.55f, 1f, 0.7f, 0.55f)
        };

        // Dash artık diğerleri gibi GİTTİĞİN YÖNE (Faz 25). Faz 16'da nişanın tersine
        // atıyordu; playtest sorusu "kafa karıştırıyor mu" idi, Enes: "hacizin dashi
        // hep terse doğru gidiyor, dash sol tık yapınca düşmandan uzağa kayıp vuruyor."
        // Kimliği dash-atakta kalıyor: ölü bölgeyi tanımıyor, dibindekine iniyor.
    }

    /// <summary>
    /// İPOTEK — nişan aldığın YERE gecikmeli bir alan vuruşu koyuyorsun.
    /// Gecikme boyunca düşmanlar oradan çıkabilir.
    ///
    /// Sorduğu soru: **"nereye GİDECEKLER?"** HACİZ *nişan* silahı (neredeler),
    /// İPOTEK *tahmin* silahı. İkisi de uzak ama aynı soruyu sormuyorlar —
    /// yoksa ikinci menzilli silah birincinin kostümü olurdu.
    ///
    /// ⚠️ SİMSAR ve MÜBAŞİR'in sana yaptığının aynısı, ters yönde. `GroundMark`
    /// aynen kullanılıyor: mekanik zaten oyunda ve oyuncu onu ÖĞRENMİŞ oluyor —
    /// önce üzerinde deneniyor, sonra eline veriliyor.
    ///
    /// Alan vuruşu olduğu için hasat çarpanı düşük; yoksa hem çoklu hem yüksek
    /// çarpan olur ve koninin dengelendiği yerden kaçardı.
    /// </summary>
    public sealed class Ipotek : Weapon
    {
        public override string Name => "İPOTEK";
        public override Color Tint => new Color(1f, 0.45f, 0.95f, 0.42f);
        // ⚠️ Playtest sonrası SERT KISILDI (Enes: "ipotek fazla güçlü amk"). Üç kadran
        // birden çevrildi çünkü üç ayrı yerden güçlüydü: alan + yığılabilirlik + hasat.
        // Yığılma yapısal olarak kapandı (tek işaret); burada kalan ikisi.
        public override float HarvestMultiplier => 0.7f;                    // 1.3 → 0.7
        public override float Range(Tuning t) => t.attackRange * 2.2f;      // koyabildiğin mesafe
        public override float Duration(Tuning t) => t.attackDuration * 1.45f;  // ağır: spam'i kesiyor
        public override float GroundMarkDelay(Tuning t) => t.ipotekDelay;
        public override float GroundMarkRadius(Tuning t) => t.ipotekRadius;
        public override float Damage(Tuning t, float baseDamage) => baseDamage * t.ipotekDamageMult;

        /// İPOTEK YAĞMURU (Faz 29, Enes: "büyük alan içinde düşmanların üstüne birkaç saniye peş
        /// peşe ipotek"). Sorusu aynı — "nereye gidecekler" — ama tek hedef yerine KALABALIK:
        /// işaretler düşmanın DURDUĞU yere düşüyor, gecikme boyunca kaçabiliyorlar.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "İPOTEK YAĞMURU", Cooldown = t.specialCooldownIpotek,
            Hits = t.ipotekRainHits, HitInterval = t.ipotekRainInterval, RangeMul = t.ipotekRainRangeMul,
            GroundDelay = t.ipotekDelay, GroundRadius = t.ipotekRadius * 0.8f,
            DamageMul = 0.9f, HarvestMul = 1f,
            Tint = new Color(1f, 0.45f, 0.95f, 0.5f)
        };

        /// HACİZ İHBARI — çok geniş, çok geç patlayan işaret. Eskiden sağ tıktı, Faz 29'dan beri
        /// DASH-ATAK (Enes). Sorusu aynı ama UFKU uzuyor: iki katı sonrasını tahmin edeceksin.
        /// ⚠️ NİŞAN noktasına düşüyor, dash'in başladığı yere değil — 2026-09-11'de arkana düşen
        /// "hayalet ipotek" nişan aldığınınkinin yerini çalıyordu (Enes'in hatası, Faz 16 kapısı).
        public override WeaponSpecial DashSpecial(Tuning t) => new WeaponSpecial
        {
            Name = "HACİZ İHBARI",
            GroundDelay = t.ipotekDelay * 2.1f, GroundRadius = t.ipotekRadius * 2f,
            RangeMul = 1f, DamageMul = 2.4f, HarvestMul = 1f,
            Tint = new Color(1f, 0.45f, 0.95f, 0.5f)
        };
    }

    /// <summary>
    /// İFLAS — saatin düştükçe daha sert vuruyor. Dolu saatte zayıf, dipte canavar.
    ///
    /// Sorduğu soru: **"ne kadar dibe inmeye cesaretim var?"** Oyunun TEK kaynağını
    /// silahın kadranı yapıyor — başka hiçbir silah saate BAKMIYOR bile.
    ///
    /// ⚠️ Kendi kendini sınırlıyor, o yüzden tavan serbest bırakılabiliyor: 25
    /// saniyedeyken tek darbe koşuyu bitiriyor. "Bilerek dipte kal" stratejisi
    /// teorik olarak var ama bedeli ölüm, ve bu bir TERCİH — istismar değil.
    /// (Playtest'te doğrulanacak; yanlışsa `iflasMaxDamageMult` inecek.)
    /// </summary>
    public sealed class Iflas : Weapon
    {
        /// <summary>Dolu saatteyken cezası — silahın kimliği bu: güvendeyken KÖTÜ.</summary>
        const float GüvendekiÇarpan = 0.7f;

        public override string Name => "İFLAS";
        public override Color Tint => new Color(1f, 0.3f, 0.25f, 0.55f);
        public override int MaxTargets => 1;
        public override float HarvestMultiplier => 2.1f;                        // 1.8 → 2.1
        public override float Angle(Tuning t) => t.singleTargetAngle;

        // ⚠️ HIZLI, ağır değil (Enes: "iflasın saldırı hızına da buff yapalım").
        // 1.25 idi — yani dipteyken sertleşen silah tam da sertleştiği anda YAVAŞ
        // vuruyordu ve eğrisi hissedilmiyordu. Hızlı olması eğriyi görünür kılıyor:
        // dipte hem sert hem seri, yani gerçekten "son çare" gibi çalışıyor.
        public override float Duration(Tuning t) => t.attackDuration * 0.8f;

        /// KAPANIŞ — saatinden büyük bir parça ödüyorsun ve çok sert vuruyorsun.
        ///
        /// ⚠️ Bedeli ayrı bir "güçlenme" mekaniği DEĞİL: ödediğin saniye saatini
        /// düşürüyor, saat düşünce İFLAS zaten sertleşiyor. Yani bedel BİZZAT
        /// buff'ın kendisi. İkinci bir sistem yazmadan silahın sorusunu bir
        /// EYLEME çeviriyor: "ne kadar dibe inmeye cesaretin var" artık bir tuş.
        /// TASFİYE — arka arkaya çok hızlı vuruşlar. Silahın kendi karakterinin
        /// abartılmışı: İFLAS oyunun en hızlı silahı, özeli de SERİ.
        ///
        /// ⚠️ Bedeli YOK ve olmamalı. İFLAS zaten saat düştükçe sertleşiyor; seri
        /// vuruş dipteyken kendiliğinden korkunç oluyor. Üstüne bir de saniye
        /// bedeli koysaydım iki ceza üst üste binerdi ve silah "dipte güçlü" değil
        /// "dipte kullanılamaz" olurdu — kimliğinin tam tersi.
        public override WeaponSpecial Special(Tuning t) => new WeaponSpecial
        {
            Name = "TASFİYE", Cooldown = t.specialCooldownIflas,
            MaxTargets = 1, Angle = t.singleTargetAngle * 1.2f, RangeMul = 1.1f,
            Hits = t.iflasFlurryHits, HitInterval = t.iflasFlurryInterval,
            DamageMul = 0.62f, HarvestMul = 1f, Knockback = 2f,
            Tint = new Color(1f, 0.3f, 0.25f, 0.6f)
        };

        public override float DesperationMultiplier(float seconds, Tuning t)
        {
            // Güvenli eşiğin üstünde en zayıf, panik eşiğinin altında en güçlü.
            float k = Mathf.InverseLerp(t.iflasSafeSeconds, t.iflasPanicSeconds, seconds);
            return Mathf.Lerp(GüvendekiÇarpan, t.iflasMaxDamageMult, k);
        }
    }
}
