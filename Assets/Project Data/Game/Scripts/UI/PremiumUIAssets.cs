using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Watermelon
{
    /// <summary>
    /// Central access to the approved premium UI sprites. Keeping the lookup here
    /// avoids changing prefab IDs or any gameplay/button wiring.
    /// </summary>
    public static class PremiumUIAssets
    {
        private const string Root = "PremiumUI/";

        private static Sprite levelCompleted;
        private static Sprite levelCurrent;
        private static Sprite levelLocked;
        private static Sprite levelPressed;
        private static Sprite settings;
        private static Sprite life;
        private static Sprite coin;
        private static Sprite soundOn;
        private static Sprite soundOff;
        private static Sprite vibrationOn;
        private static Sprite vibrationOff;
        private static Sprite homeNormal;
        private static Sprite homePressed;
        private static Sprite homeDisabled;

        public static Sprite LevelCompleted => Load(ref levelCompleted, "level_completed");
        public static Sprite LevelCurrent => Load(ref levelCurrent, "level_current");
        public static Sprite LevelLocked => Load(ref levelLocked, "level_locked");
        public static Sprite LevelPressed => Load(ref levelPressed, "level_pressed");
        public static Sprite Settings => Load(ref settings, "settings");
        public static Sprite Life => Load(ref life, "life");
        public static Sprite Coin => Load(ref coin, "coin");
        public static Sprite SoundOn => Load(ref soundOn, "sound_on");
        public static Sprite SoundOff => Load(ref soundOff, "sound_off");
        public static Sprite VibrationOn => Load(ref vibrationOn, "vibration_on");
        public static Sprite VibrationOff => Load(ref vibrationOff, "vibration_off");
        public static Sprite HomeNormal => Load(ref homeNormal, "home_normal");
        public static Sprite HomePressed => Load(ref homePressed, "home_pressed");
        public static Sprite HomeDisabled => Load(ref homeDisabled, "home_disabled");

        public static void ApplyHomeButton(Button button)
        {
            if (button == null || HomeNormal == null) return;

            // Replace the button's main graphic. The old prefabs use a separate
            // cottage child named "Home Image"; styling that child alone can be
            // overwritten when the prefab animation is initialised.
            Image image = button.GetComponent<Image>();
            if (image == null)
                image = button.targetGraphic as Image;
            if (image == null) return;

            foreach (Image candidate in button.GetComponentsInChildren<Image>(true))
            {
                // Legacy prefabs use different names ("Home Image" in gameplay,
                // plain "Image" on the complete screen). The approved sprite
                // already contains the full icon, so every old child graphic
                // must be hidden to prevent a cottage overlay.
                if (candidate != image)
                    candidate.enabled = false;
            }

            image.sprite = HomeNormal;
            image.color = Color.white;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;

            SpriteState state = button.spriteState;
            state.highlightedSprite = HomeNormal;
            state.selectedSprite = HomeNormal;
            state.pressedSprite = HomePressed != null ? HomePressed : HomeNormal;
            state.disabledSprite = HomeDisabled != null ? HomeDisabled : HomeNormal;
            button.spriteState = state;

            Debug.Log($"Premium home icon applied: {button.name}");

            // The approved asset is icon-only, so remove legacy HOME captions on
            // wide popup buttons while preserving the original Button callback.
            foreach (TextMeshProUGUI label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                string value = label.text == null ? string.Empty : label.text.Trim().ToUpperInvariant();
                if (value == "HOME" || value == "GO HOME")
                    label.gameObject.SetActive(false);
            }
        }

        public static void ApplySettingsButton(Transform root)
        {
            if (root == null || Settings == null) return;

            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name != "SettingsButton") continue;

                Image image = button.targetGraphic as Image;
                if (image == null) image = button.GetComponent<Image>();
                if (image == null) continue;

                image.sprite = Settings;
                image.color = Color.white;
                image.preserveAspect = true;
                break;
            }
        }

        public static void ApplyLifeIcon(Transform root)
        {
            if (root == null || Life == null) return;

            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.name != "Heart Image") continue;
                image.sprite = Life;
                image.color = Color.white;
                image.preserveAspect = true;
                break;
            }
        }

        private static Sprite Load(ref Sprite cache, string assetName)
        {
            if (cache == null)
                cache = Resources.Load<Sprite>(Root + assetName);

            return cache;
        }
    }
}
