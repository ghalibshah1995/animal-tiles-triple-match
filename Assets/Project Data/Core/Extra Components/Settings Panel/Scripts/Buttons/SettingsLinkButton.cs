#pragma warning disable 0649 

using UnityEngine;

namespace Watermelon
{
    public class SettingsLinkButton : SettingsButtonBase
    {
        [SerializeField] bool isActive = true;
        [SerializeField] string url;

        public override bool IsActive()
        {
            return isActive;
        }

        public override void OnClick()
        {
            if (name.IndexOf("privacy", System.StringComparison.OrdinalIgnoreCase) >= 0)
                AdsManager.ShowPrivacyOptionsOrPolicy(url);
            else if (!string.IsNullOrWhiteSpace(url))
                Application.OpenURL(url);
            else
                Debug.LogWarning("[Settings Panel]: Link URL is not configured yet.");

            // Play button sound
            AudioController.PlaySound(AudioController.Sounds.buttonSound);
        }
    }
}

// -----------------
// Settings Panel v 0.3
// -----------------
