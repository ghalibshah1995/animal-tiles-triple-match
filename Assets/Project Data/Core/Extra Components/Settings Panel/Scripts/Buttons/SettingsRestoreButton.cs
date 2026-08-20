using UnityEngine;

namespace Watermelon
{
    public class SettingsRestoreButton : SettingsButtonBase
    {
        public override bool IsActive()
        {
            return false;
        }

        public override void OnClick()
        {
            IAPManager.RestorePurchases();

            // Play button sound
            AudioController.PlaySound(AudioController.Sounds.buttonSound);
        }
    }
}

// -----------------
// Settings Panel v 0.3
// -----------------
