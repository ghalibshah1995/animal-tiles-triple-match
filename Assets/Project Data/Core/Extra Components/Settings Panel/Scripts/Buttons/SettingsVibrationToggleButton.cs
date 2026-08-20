#pragma warning disable 0649 

using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class SettingsVibrationToggleButton : SettingsButtonBase
    {
        [SerializeField] Image imageRef;

        [Space]
        [SerializeField] Sprite activeSprite;
        [SerializeField] Sprite disableSprite;

        private bool isActive = true;

        private void Start()
        {
            if (PremiumUIAssets.VibrationOn != null) activeSprite = PremiumUIAssets.VibrationOn;
            if (PremiumUIAssets.VibrationOff != null) disableSprite = PremiumUIAssets.VibrationOff;

            isActive = AudioController.IsVibrationEnabled();
            RefreshVisual();
        }

        public override bool IsActive()
        {
            return true;
        }

        public override void OnClick()
        {
            isActive = !isActive;

            if (isActive)
            {
                AudioController.SetVibrationState(true);
            }
            else
            {
                AudioController.SetVibrationState(false);
            }

            RefreshVisual();

            // Play button sound
            AudioController.PlaySound(AudioController.Sounds.buttonSound);
        }

        private void RefreshVisual()
        {
            if (imageRef != null)
                imageRef.sprite = isActive ? activeSprite : disableSprite;
        }
    }
}

// -----------------
// Settings Panel v 0.3
// -----------------
