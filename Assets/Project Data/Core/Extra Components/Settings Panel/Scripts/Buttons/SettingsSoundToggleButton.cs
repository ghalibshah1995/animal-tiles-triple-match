#pragma warning disable 649

using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class SettingsSoundToggleButton : SettingsButtonBase
    {
        [SerializeField] Image imageRef;

        [Space]
        [SerializeField] Sprite activeSprite;
        [SerializeField] Sprite disableSprite;

        private bool isActive = true;

        private void Start()
        {
            if (PremiumUIAssets.SoundOn != null) activeSprite = PremiumUIAssets.SoundOn;
            if (PremiumUIAssets.SoundOff != null) disableSprite = PremiumUIAssets.SoundOff;

            isActive = AudioController.GetVolume() != 0;
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
                AudioController.SetVolume(1f);
            }
            else
            {
                AudioController.SetVolume(0f);
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
