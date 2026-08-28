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

        public void Init(Tuning çalışmaKopyası)
        {
            _t = çalışmaKopyası;
            TuneProfile.Init(_t);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || _t == null) return;

            int seçim = -1;
            if (kb.f1Key.wasPressedThisFrame) seçim = 0;
            else if (kb.f2Key.wasPressedThisFrame) seçim = 1;
            else if (kb.f3Key.wasPressedThisFrame) seçim = 2;
            else if (kb.f4Key.wasPressedThisFrame) seçim = 3;
            if (seçim < 0 || seçim >= TuneProfile.All.Length) return;

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
