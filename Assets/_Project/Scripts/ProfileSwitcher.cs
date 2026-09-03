using UnityEngine;
using UnityEngine.InputSystem;

namespace TekCan
{
    /// <summary>
    /// F1–F4 ile ayar profilleri arasında ANINDA geçiş. Durma yok, yeniden başlatma yok.
    ///
    /// ⚠️ TEST ARACI — gerçek oyunda kalkacak.
    ///
    /// Fazın kalbi bu: iki değeri hafızadan karşılaştırmak imkânsız, yan yana
    /// hissetmek şart. Aynı dalgada F1'e bas, F2'ye bas, farkı DERHAL hisset.
    /// </summary>
    public class ProfileSwitcher : MonoBehaviour
    {
        Tuning _t;
        float _pullTimer;

        /// <summary>Inspector köprüsünün tazelenme aralığı — 60Hz gereksiz, göz farkı görmez.</summary>
        const float PullInterval = 0.2f;

        public void Init(Tuning asset, Tuning çalışmaKopyası)
        {
            _t = çalışmaKopyası;
            TuneProfile.Init(asset, _t);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (_t == null) return;

            // ⚠️ Inspector köprüsü: MEVCUT profilindeyken asset'te yapılan değişiklikler
            // oyuna geçmeye devam ediyor. Çalışma kopyası asset'i profillerden koruyor
            // ama Enes'in günlük ayar yöntemini kırmamalı.
            _pullTimer -= Time.unscaledDeltaTime;
            if (_pullTimer <= 0f)
            {
                _pullTimer = PullInterval;
                TuneProfile.PullFromAssetIfBaseline(_t);
            }

            if (kb == null) return;

            int seçim = -1;
            if (kb.f1Key.wasPressedThisFrame) seçim = 0;
            else if (kb.f2Key.wasPressedThisFrame) seçim = 1;
            else if (kb.f3Key.wasPressedThisFrame) seçim = 2;
            else if (kb.f4Key.wasPressedThisFrame) seçim = 3;
            if (seçim < 0 || seçim >= TuneProfile.All.Length) return;

            // ⚠️ TABAN DIŞI PROFİL KOŞUYU KİRLETİR — `N`/`B`/`F5` ile aynı sınıf hata.
            // F2-F4 hasat, boss ve arena değerlerini oyunun ortasında değiştiriyor;
            // o kurallar altında kazanılan saat checkpoint'e yazılırsa kayıt, hiç
            // oynanmamış bir oyunun ilerlemesini tutar. Bir kez sapıldıysa geri
            // dönmek temizlemiyor: F2'de oynanan dakika F1'e basınca geri gelmiyor.
            if (seçim != 0) SaveFile.MarkTestJump();

            TuneProfile.Select(seçim, _t);
            Sfx.Buy();
            IsoCamera.Shake(0.15f);

            // Geometri değişen profillerde odayı YENİDEN KUR — yoksa "hiçbir şey
            // olmadı" sanılır. Arena yarıçapı oda kurulurken kullanılıyor.
            if (TuneProfile.Current.NeedsNewRoom || GeometriDeğişmişti)
                RoomManager.Instance?.RebuildRoom();

            GeometriDeğişmişti = TuneProfile.Current.NeedsNewRoom;
        }

        /// <summary>
        /// Geometrili profilden ÇIKARKEN de yeniden kurmak gerekiyor: yoksa küçük
        /// arenadan normale dönünce oda küçük kalır ve karşılaştırma yalan söyler.
        /// </summary>
        bool GeometriDeğişmişti;
    }
}
