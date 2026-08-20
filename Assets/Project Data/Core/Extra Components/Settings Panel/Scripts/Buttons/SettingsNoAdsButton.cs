using UnityEngine;

namespace Watermelon
{
    public class SettingsNoAdsButton : SettingsButtonBase
    {
        public override bool IsActive()
        {
            return false;
        }

        public override void OnClick()
        {
            // Ads are not part of the pure gameplay build.
        }
    }
}

// -----------------
// Settings Panel v 0.3
// -----------------
