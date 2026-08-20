using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Applies the approved premium presentation while keeping the original prefab
    /// objects, serialized references and button callbacks intact.
    /// </summary>
    public static class PremiumUIStyler
    {
        private static readonly Color DeepNavy = new Color(0.025f, 0.10f, 0.20f, 1f);
        private static readonly Color Cyan = new Color(0.08f, 0.78f, 0.96f, 1f);
        private static readonly Color Gold = new Color(1f, 0.72f, 0.10f, 1f);
        private static readonly Color Coral = new Color(0.96f, 0.10f, 0.23f, 1f);
        private static readonly Color Green = new Color(0.08f, 0.82f, 0.22f, 1f);
        private static readonly Color Purple = new Color(0.58f, 0.12f, 0.86f, 1f);
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public static void StyleSettings(Transform root)
        {
            if (root == null) return;

            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                Image image = GetButtonImage(button);
                if (image == null) continue;

                AddDepth(image, button.name == "SettingsButton" ? Cyan : Gold, 2.2f, 5f);
                SetPremiumButtonColours(button);
            }
        }

        public static void StyleGameplayHeader(Button homeButton, CurrencyUIPanelSimple coinsPanel)
        {
            if (homeButton != null)
            {
                Image image = GetButtonImage(homeButton);
                if (image != null) AddDepth(image, Cyan, 2.6f, 6f);
                SetPremiumButtonColours(homeButton);
            }

            if (coinsPanel == null) return;

            RectTransform rect = coinsPanel.RectTransform != null
                ? coinsPanel.RectTransform
                : coinsPanel.GetComponent<RectTransform>();

            if (rect != null && rect.localScale.x > 0.9f)
                rect.localScale = new Vector3(0.88f, 0.88f, 1f);
        }

        public static void StyleLivesPopup(Transform root, RectTransform panel, Button rewardButton,
            Button closeButton, TMP_Text livesText, TMP_Text timerText)
        {
            if (root == null || panel == null) return;

            ConfigurePopupCanvas(root, 110);
            StyleDimOverlay(root);
            SetRect(panel, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero,
                new Vector2(800f, 1005f), new Vector2(0.5f, 0.5f));

            Image panelBackground = EnsureImage(panel, "Lives Reference Frame",
                PremiumSprite("lives_reference_frame", 0f));
            Stretch(panelBackground.rectTransform);
            panelBackground.color = Color.white;
            panelBackground.type = Image.Type.Simple;
            panelBackground.preserveAspect = false;
            panelBackground.raycastTarget = false;
            panelBackground.transform.SetAsFirstSibling();

            DisableNamedGraphic(panel, "Panel Graphics");
            DisableNamedGraphic(panel, "Panel Shine");

            TMP_Text title = FindText(root, "Title Text");
            if (title != null)
                title.gameObject.SetActive(false);

            RectTransform heartPanel = FindRect(root, "Heart_Panel");
            if (heartPanel != null)
            {
                Image inner = heartPanel.GetComponent<Image>();
                if (inner != null) inner.enabled = false;
            }

            Image heart = FindImage(root, "Heart Image");
            if (heart != null)
            {
                SetRect(heart.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(0f, 98f), new Vector2(438f, 402f), new Vector2(0.5f, 0.5f));
                heart.sprite = PremiumSprite("lives_clean_heart", 0f);
                heart.type = Image.Type.Simple;
                heart.preserveAspect = false;
                heart.color = Color.white;
                heart.enabled = true;
            }

            if (livesText != null)
            {
                livesText.textWrappingMode = TextWrappingModes.NoWrap;
                SetRect(livesText.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(-15f, 0f), new Vector2(180f, 135f), new Vector2(0.5f, 0.5f));
                StyleHeading(livesText, Color.white, 100f, 66f);
            }

            TMP_Text multiplier = FindText(root, "Refill Text (1)");
            if (multiplier != null)
                multiplier.gameObject.SetActive(false);

            Image plusImage = FindImage(root, "Plus Image");
            if (plusImage != null)
            {
                plusImage.gameObject.SetActive(true);
                SetRect(plusImage.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(95f, 0f), new Vector2(70f, 70f), new Vector2(0.5f, 0.5f));
                plusImage.color = Color.white;
                plusImage.preserveAspect = true;
            }

            TMP_Text nextLife = FindText(root, "Time Description Text");
            if (nextLife != null)
                nextLife.gameObject.SetActive(false);

            Image timeBack = FindImage(root, "Time Background Image");
            if (timeBack != null)
            {
                timeBack.sprite = PremiumSprite("lives_blank_timer", 0f);
                timeBack.type = Image.Type.Simple;
                timeBack.color = Color.white;
                SetRect(timeBack.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(0f, -220f), new Vector2(255f, 80f), new Vector2(0.5f, 0.5f));
                timeBack.preserveAspect = false;
            }

            if (timerText != null)
            {
                SetRect(timerText.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(0f, -2f), new Vector2(225f, 68f), new Vector2(0.5f, 0.5f));
                StyleHeading(timerText, Color.white, 44f, 30f);
            }

            if (rewardButton != null)
            {
                SetRect(rewardButton.transform as RectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(0f, -375f), new Vector2(590f, 145f), new Vector2(0.5f, 0.5f));
                MakeButtonGraphicsInvisible(rewardButton);
            }

            if (closeButton != null)
            {
                SetRect(closeButton.transform as RectTransform, Vector2.one, Vector2.one,
                    new Vector2(-64f, -64f), new Vector2(112f, 112f), new Vector2(0.5f, 0.5f));
                MakeButtonGraphicsInvisible(closeButton);
            }
        }

        public static void StyleQuitPopup(Transform root)
        {
            if (root == null) return;

            ConfigurePopupCanvas(root, 120);
            StyleDimOverlay(root);

            RectTransform panel = FindRect(root, "Panel Back");
            if (panel == null) return;

            SetRect(panel, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero,
                new Vector2(760f, 880f), new Vector2(0.5f, 0.5f));
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.sprite = PremiumSprite("panel_navy", 82f);
                panelImage.type = Image.Type.Sliced;
                panelImage.color = Color.white;
            }

            DisableNamedGraphic(panel, "Panel Shine");

            TMP_Text title = FindText(root, "Text (TMP)");
            if (title != null)
            {
                title.text = "QUIT LEVEL?";
                SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -100f), new Vector2(610f, 110f), new Vector2(0.5f, 0.5f));
                StyleHeading(title, new Color(1f, 0.94f, 0.80f), 62f, 42f);
            }

            Image warningBack = FindImage(root, "Life Lose Back Image");
            if (warningBack != null)
            {
                warningBack.sprite = PremiumSprite("panel_inner_ice", 65f);
                warningBack.type = Image.Type.Sliced;
                warningBack.color = new Color(0.62f, 0.04f, 0.12f, 1f);
                SetRect(warningBack.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(0f, 40f), new Vector2(590f, 470f), new Vector2(0.5f, 0.5f));
            }

            Image heart = FindImage(root, "Life Image");
            if (heart != null)
            {
                SetRect(heart.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(0f, 105f), new Vector2(300f, 280f), new Vector2(0.5f, 0.5f));
                heart.preserveAspect = true;
                heart.color = Color.white;
            }

            TMP_Text warning = FindText(root, "Lose Life Text");
            if (warning != null)
            {
                warning.text = "You will lose a life!";
                SetRect(warning.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(0f, -125f), new Vector2(520f, 80f), new Vector2(0.5f, 0.5f));
                StyleHeading(warning, Color.white, 38f, 28f);
            }

            Button close = FindButton(root, "Close Button");
            StyleCloseButton(close, new Vector2(4f, 2f));

            Button quit = FindButton(root, "Quit Button");
            StyleButton(quit, Coral, new Vector2(430f, 125f), new Vector2(0f, -338f), "QUIT", 48f);
        }

        public static void StyleBoosterPopup(Transform root, Image boosterPreview, TMP_Text amountText,
            TMP_Text descriptionText, Button buyButton, Button rewardButton, Button closeButton)
        {
            if (root == null) return;

            ConfigurePopupCanvas(root, 120);
            StyleDimOverlay(root);

            RectTransform panel = FindRect(root, "Panel Back");
            if (panel == null) return;

            SetRect(panel, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero,
                new Vector2(820f, 1174f), new Vector2(0.5f, 0.5f));
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.sprite = PremiumSprite("booster_popup_frame", 0f);
                panelImage.type = Image.Type.Simple;
                panelImage.color = Color.white;
                panelImage.preserveAspect = false;
                panelImage.raycastTarget = true;
            }
            DisableNamedGraphic(panel, "Panel Shine");

            Transform oldHeader = panel.Find("Premium Purple Header");
            if (oldHeader != null)
                oldHeader.gameObject.SetActive(false);

            TMP_Text heading = FindText(root, "Heading Text");
            if (heading != null)
            {
                heading.text = "NEED A BOOSTER?";
                SetRect(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -78f), new Vector2(670f, 100f), new Vector2(0.5f, 0.5f));
                StyleHeading(heading, Color.white, 50f, 34f);
                heading.transform.SetAsLastSibling();
            }

            Image boosterBack = FindImage(root, "PU Zone Back Image");
            if (boosterBack != null)
            {
                // This object is also the content container. Hide only its old
                // rectangular graphic so the reference frame's glow stays visible.
                boosterBack.enabled = false;
                SetRect(boosterBack.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(0f, 35f), new Vector2(700f, 770f), new Vector2(0.5f, 0.5f));
            }

            if (boosterPreview != null)
            {
                SetRect(boosterPreview.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(0f, 145f), new Vector2(355f, 355f), new Vector2(0.5f, 0.5f));
                boosterPreview.preserveAspect = true;
                boosterPreview.raycastTarget = false;
            }

            if (amountText != null)
            {
                SetRect(amountText.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(205f, 275f), new Vector2(122f, 105f), new Vector2(0.5f, 0.5f));
                StyleHeading(amountText, Color.white, 48f, 30f);
                amountText.color = Color.white;
                Image amountBadge = EnsureImage(amountText.transform.parent, "Premium Booster Amount Badge",
                    PremiumSprite("booster_amount_badge", 0f));
                amountBadge.type = Image.Type.Simple;
                amountBadge.color = Color.white;
                amountBadge.preserveAspect = true;
                amountBadge.raycastTarget = false;
                SetRect(amountBadge.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(205f, 275f), new Vector2(140f, 140f), new Vector2(0.5f, 0.5f));
                amountBadge.transform.SetSiblingIndex(Mathf.Max(0, amountText.transform.GetSiblingIndex()));
                amountText.transform.SetAsLastSibling();
            }

            if (descriptionText != null)
            {
                SetRect(descriptionText.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(0f, -175f), new Vector2(560f, 118f), new Vector2(0.5f, 0.5f));
                StyleHeading(descriptionText, Color.white, 34f, 24f);
            }

            // Ads are disabled, leaving only the coin purchase action. Center it
            // at the bottom of the booster popup instead of retaining the old
            // two-option (BUY + WATCH AD) layout.
            StyleGeneratedButton(buyButton, "booster_buy_button", new Vector2(320f, 135f),
                new Vector2(0f, -445f));
            PositionBoosterBuyContent(buyButton);
            StyleGeneratedButton(rewardButton, "booster_reward_button", new Vector2(390f, 135f),
                new Vector2(180f, -445f));
            PositionBoosterRewardContent(rewardButton);
            StyleCloseButton(closeButton, new Vector2(8f, 2f));
        }

        public static void StyleCompletePopup(Transform root)
        {
            if (root == null) return;
            ConfigureResultScreen(root, "complete_reference_bg");

            TMP_Text level = FindText(root, "LevelText");
            if (level != null)
                level.gameObject.SetActive(false);

            TMP_Text completed = FindText(root, "Completed Text");
            if (completed != null)
                completed.gameObject.SetActive(false);

            RectTransform holder = FindRect(root, "Level Complered Holder");
            if (holder != null)
                SetRect(holder, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(0f, 405f), new Vector2(900f, 310f), new Vector2(0.5f, 0.5f));

            RectTransform reward = FindRect(root, "Reward Label");
            if (reward != null)
            {
                SetRect(reward, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(0f, -165f), new Vector2(470f, 150f), new Vector2(0.5f, 0.5f));

                Image rewardBack = reward.GetComponent<Image>();
                if (rewardBack != null)
                {
                    rewardBack.color = Color.clear;
                    rewardBack.raycastTarget = false;
                }
            }

            Image rewardIcon = FindImage(root, "Reward Icon");
            if (rewardIcon != null)
                rewardIcon.enabled = false;

            TMP_Text amount = FindText(root, "Reward Amount Text");
            if (amount != null)
            {
                SetRect(amount.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(78f, -25f), new Vector2(210f, 92f), new Vector2(0.5f, 0.5f));
                StyleHeading(amount, Color.white, 58f, 38f);
            }

            Button multiply = FindButton(root, "Multiply Reward Button");
            StyleReferenceSpriteButton(multiply, "complete_watch_button", new Vector2(790f, 305f),
                new Vector2(0f, -430f));

            Button home = FindButton(root, "Home Button");
            StyleReferenceSpriteButton(home, "complete_home_button", new Vector2(235f, 220f),
                new Vector2(-240f, -770f));

            Button next = FindButton(root, "Next Level Button");
            StyleReferenceSpriteButton(next, "complete_next_button", new Vector2(430f, 220f),
                new Vector2(140f, -770f));

            Button headerHome = FindButton(root, "Go Home");
            if (headerHome != null)
            {
                headerHome.gameObject.SetActive(true);
                RectTransform headerRect = headerHome.transform as RectTransform;
                SetRect(headerRect, Vector2.up, Vector2.up, new Vector2(140f, -245f),
                    new Vector2(125f, 125f), new Vector2(0.5f, 0.5f));
                MakeButtonGraphicsInvisible(headerHome);
            }

            StyleCompleteCurrencyText(root);
        }

        public static void StyleFailPopup(Transform root)
        {
            if (root == null) return;
            ConfigureResultScreen(root);

            TMP_Text failed = FindText(root, "Level Failed Text");
            if (failed != null)
            {
                failed.text = "LEVEL\nFAILED";
                SetRect(failed.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(0f, 230f), new Vector2(850f, 390f), new Vector2(0.5f, 0.5f));
                StyleHeading(failed, Color.white, 112f, 64f);
                failed.outlineColor = new Color(0.98f, 0.12f, 0.32f, 1f);
            }

            Button menu = FindButton(root, "Menu Button");
            StyleButton(menu, Cyan, new Vector2(355f, 145f), new Vector2(-205f, -220f), "MENU", 46f);
            Button replay = FindButton(root, "Replay Button");
            StyleButton(replay, Purple, new Vector2(355f, 145f), new Vector2(205f, -220f), "REPLAY", 46f);
            PositionButtonIcon(replay, "Replay Image", -120f, 78f, 92f);
            Button revive = FindButton(root, "Revive Button");
            StyleButton(revive, Green, new Vector2(790f, 175f), new Vector2(0f, -430f),
                "WATCH AD\n+3 MOVES", 46f);
            PositionButtonIcon(revive, "Ad Image", -300f, 105f, 130f);
        }

        private static void ConfigureResultScreen(Transform root, string backgroundAsset = "night_results_bg")
        {
            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 80;
            }

            Image background = FindImage(root, "Background Image");
            if (background != null)
            {
                Texture2D texture = Resources.Load<Texture2D>("PremiumUI/" + backgroundAsset);
                if (texture != null)
                {
                    background.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    background.type = Image.Type.Simple;
                    background.preserveAspect = false;
                    background.color = Color.white;
                }
                Stretch(background.rectTransform);
                background.transform.SetAsFirstSibling();
            }

            RectTransform safeArea = FindRect(root, "Safe Area");
            if (safeArea != null)
            {
                safeArea.anchorMin = Vector2.zero;
                safeArea.anchorMax = Vector2.one;
                safeArea.offsetMin = new Vector2(24f, 38f);
                safeArea.offsetMax = new Vector2(-24f, -38f);
            }
        }

        private static void StyleDimOverlay(Transform root)
        {
            Image image = root.GetComponent<Image>();
            if (image == null)
            {
                image = root.GetComponentInChildren<Image>(true);
            }

            if (image != null && (image.transform == root || image.name.ToLowerInvariant().Contains("background")))
            {
                image.color = new Color(0.005f, 0.02f, 0.08f, 0.78f);
                image.raycastTarget = true;
                Stretch(image.rectTransform);
            }
        }

        private static void ConfigurePopupCanvas(Transform root, int sortingOrder)
        {
            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas == null) canvas = root.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            if (root.GetComponent<GraphicRaycaster>() == null)
                root.gameObject.AddComponent<GraphicRaycaster>();
        }

        private static void StyleButton(Button button, Color tint, Vector2 size, Vector2 position,
            string labelText, float fontSize)
        {
            if (button == null) return;

            RectTransform rect = button.transform as RectTransform;
            SetRect(rect, Vector2.one * 0.5f, Vector2.one * 0.5f, position, size, new Vector2(0.5f, 0.5f));
            rect.localScale = Vector3.one;

            Image image = GetButtonImage(button);
            if (image != null)
            {
                image.sprite = PremiumSprite("button_pearl", 52f);
                image.type = Image.Type.Sliced;
                image.color = tint;
                image.raycastTarget = true;
                AddDepth(image, Gold, 2.3f, 8f);
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                if (!string.IsNullOrEmpty(labelText)) label.text = labelText;
                label.gameObject.SetActive(true);
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(22f, 14f);
                label.rectTransform.offsetMax = new Vector2(-22f, -14f);
                StyleHeading(label, Color.white, fontSize, Mathf.Max(22f, fontSize * 0.65f));
            }

            SetPremiumButtonColours(button);
            button.transform.SetAsLastSibling();
        }

        private static void StyleCloseButton(Button button, Vector2 offset)
        {
            if (button == null) return;

            RectTransform rect = button.transform as RectTransform;
            SetRect(rect, Vector2.one, Vector2.one, offset, new Vector2(112f, 112f), new Vector2(0.5f, 0.5f));
            rect.localScale = Vector3.one;

            Image image = GetButtonImage(button);
            if (image != null)
            {
                image.sprite = PremiumSprite("button_close_red", 0f);
                image.type = Image.Type.Simple;
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = true;
            }

            Image[] childImages = button.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < childImages.Length; i++)
            {
                if (childImages[i] == image) continue;
                childImages[i].preserveAspect = true;
                childImages[i].color = Color.white;
                RectTransform childRect = childImages[i].rectTransform;
                childRect.anchorMin = new Vector2(0.25f, 0.25f);
                childRect.anchorMax = new Vector2(0.75f, 0.75f);
                childRect.offsetMin = Vector2.zero;
                childRect.offsetMax = Vector2.zero;
            }

            SetPremiumButtonColours(button);
            EnsureCloseGlyph(button);
            button.transform.SetAsLastSibling();
        }

        private static void PositionBoosterBuyContent(Button button)
        {
            if (button == null) return;

            TMP_Text buyLabel = FindText(button.transform, "Buy Text (TMP)");
            if (buyLabel != null)
            {
                buyLabel.text = "BUY";
                SetRect(buyLabel.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(-18f, 25f), new Vector2(220f, 54f), new Vector2(0.5f, 0.5f));
                StyleHeading(buyLabel, Color.white, 36f, 25f);
            }

            Image currencyIcon = FindImage(button.transform, "Currency Image");
            if (currencyIcon != null)
            {
                currencyIcon.gameObject.SetActive(true);
                currencyIcon.enabled = true;
                currencyIcon.preserveAspect = true;
                currencyIcon.raycastTarget = false;
                SetRect(currencyIcon.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(52f, -27f), new Vector2(44f, 44f), new Vector2(0.5f, 0.5f));
            }

            TMP_Text price = FindText(button.transform, "Price Text");
            if (price != null)
            {
                SetRect(price.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(-5f, -27f), new Vector2(88f, 52f), new Vector2(0.5f, 0.5f));
                StyleHeading(price, Color.white, 31f, 22f);
            }
        }

        private static void PositionBoosterRewardContent(Button button)
        {
            if (button == null) return;

            Image adBadge = EnsureImage(button.transform, "Premium Booster Ad Icon",
                PremiumSprite("booster_ad_icon", 0f));
            adBadge.type = Image.Type.Simple;
            adBadge.color = Color.white;
            adBadge.preserveAspect = true;
            adBadge.raycastTarget = false;
            SetRect(adBadge.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(-142f, 0f), new Vector2(82f, 82f), new Vector2(0.5f, 0.5f));

            TMP_Text label = FindText(button.transform, "Buy Text (TMP)");
            if (label != null)
            {
                label.text = "WATCH AD\n+1 BOOSTER";
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(82f, 12f);
                label.rectTransform.offsetMax = new Vector2(-12f, -12f);
                StyleHeading(label, Color.white, 30f, 20f);
            }

            Transform existing = button.transform.Find("Premium Booster Ad Glyph");
            TMP_Text glyph = existing == null ? null : existing.GetComponent<TMP_Text>();
            if (glyph == null && label != null)
            {
                glyph = Object.Instantiate(label, button.transform);
                glyph.name = "Premium Booster Ad Glyph";
            }

            if (glyph != null)
            {
                glyph.gameObject.SetActive(false);
                glyph.text = string.Empty;
                glyph.raycastTarget = false;
                SetRect(glyph.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(-137f, 0f), new Vector2(65f, 65f), new Vector2(0.5f, 0.5f));
                StyleHeading(glyph, Color.white, 44f, 32f);
            }

            adBadge.transform.SetAsLastSibling();
            if (label != null)
                label.transform.SetAsLastSibling();
        }

        private static void StyleGeneratedButton(Button button, string spriteName, Vector2 size,
            Vector2 position)
        {
            if (button == null) return;

            RectTransform rect = button.transform as RectTransform;
            SetRect(rect, Vector2.one * 0.5f, Vector2.one * 0.5f, position, size,
                new Vector2(0.5f, 0.5f));
            rect.localScale = Vector3.one;

            Image image = GetButtonImage(button);
            if (image != null)
            {
                image.sprite = PremiumSprite(spriteName, 0f);
                image.type = Image.Type.Simple;
                image.color = Color.white;
                image.preserveAspect = false;
                image.raycastTarget = true;
            }

            SetPremiumButtonColours(button);
            button.transform.SetAsLastSibling();
        }

        private static void StyleReferenceSpriteButton(Button button, string spriteName, Vector2 size,
            Vector2 position)
        {
            if (button == null) return;

            RectTransform rect = button.transform as RectTransform;
            SetRect(rect, Vector2.one * 0.5f, Vector2.one * 0.5f, position, size,
                new Vector2(0.5f, 0.5f));
            rect.localScale = Vector3.one;

            Image targetImage = GetButtonImage(button);
            Graphic[] graphics = button.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == targetImage) continue;
                Color hiddenColor = graphics[i].color;
                hiddenColor.a = 0f;
                graphics[i].color = hiddenColor;
                graphics[i].raycastTarget = false;
            }

            if (targetImage != null)
            {
                targetImage.gameObject.SetActive(true);
                targetImage.enabled = true;
                targetImage.sprite = PremiumSprite(spriteName, 0f);
                targetImage.type = Image.Type.Simple;
                targetImage.color = Color.white;
                targetImage.preserveAspect = false;
                targetImage.raycastTarget = true;
            }

            SetPremiumButtonColours(button);
            button.transform.SetAsLastSibling();
        }

        private static void MakeButtonGraphicsInvisible(Button button)
        {
            if (button == null) return;

            Graphic target = button.targetGraphic;
            Graphic[] graphics = button.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Color color = graphics[i].color;
                color.a = 0f;
                graphics[i].color = color;
                graphics[i].raycastTarget = graphics[i] == target;
            }

            button.transition = Selectable.Transition.None;
        }

        private static void StyleCompleteCurrencyText(Transform root)
        {
            RectTransform currencyPanel = FindRect(root, "Currency Panel Simple");
            if (currencyPanel == null) return;

            SetRect(currencyPanel, Vector2.one, Vector2.one, new Vector2(-235f, -245f),
                new Vector2(305f, 85f), new Vector2(0.5f, 0.5f));

            Image[] images = currencyPanel.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Color color = images[i].color;
                color.a = 0f;
                images[i].color = color;
                images[i].raycastTarget = false;
            }

            TMP_Text amount = FindText(currencyPanel, "Amount Text");
            if (amount != null)
            {
                amount.gameObject.SetActive(true);
                SetRect(amount.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(72f, 130f), new Vector2(145f, 70f), new Vector2(0.5f, 0.5f));
                StyleHeading(amount, Color.white, 44f, 30f);
            }
        }

        private static void EnsureCloseGlyph(Button button)
        {
            if (button == null) return;

            Transform existing = button.transform.Find("Premium Close Glyph");
            TMP_Text glyph = existing == null ? null : existing.GetComponent<TMP_Text>();
            if (glyph == null)
            {
                TMP_Text template = button.transform.parent.GetComponentInChildren<TMP_Text>(true);
                if (template == null) return;

                glyph = Object.Instantiate(template, button.transform);
                glyph.name = "Premium Close Glyph";
            }

            glyph.gameObject.SetActive(true);
            glyph.text = "X";
            glyph.raycastTarget = false;
            glyph.rectTransform.anchorMin = Vector2.zero;
            glyph.rectTransform.anchorMax = Vector2.one;
            glyph.rectTransform.offsetMin = new Vector2(20f, 18f);
            glyph.rectTransform.offsetMax = new Vector2(-20f, -18f);
            glyph.rectTransform.localScale = Vector3.one;
            StyleHeading(glyph, Color.white, 72f, 42f);
            glyph.transform.SetAsLastSibling();
        }

        private static void PositionButtonIcon(Button button, string iconName, float xPosition,
            float iconSize, float labelLeftInset)
        {
            if (button == null) return;

            Image icon = FindImage(button.transform, iconName);
            if (icon != null)
            {
                icon.gameObject.SetActive(true);
                icon.enabled = true;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                SetRect(icon.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                    new Vector2(xPosition, 0f), new Vector2(iconSize, iconSize), new Vector2(0.5f, 0.5f));
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(labelLeftInset, 14f);
                label.rectTransform.offsetMax = new Vector2(-22f, -14f);
            }
        }

        private static void StyleHeading(TMP_Text text, Color color, float maxSize, float minSize)
        {
            if (text == null) return;
            text.color = color;
            text.fontStyle |= FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMax = maxSize;
            text.fontSizeMin = minSize;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.outlineColor = new Color(0.015f, 0.035f, 0.09f, 1f);
            text.outlineWidth = 0.18f;
        }

        private static void StyleBody(TMP_Text text, float maxSize, float minSize, Color color)
        {
            if (text == null) return;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMax = maxSize;
            text.fontSizeMin = minSize;
            text.textWrappingMode = TextWrappingModes.Normal;
        }

        private static Sprite PremiumSprite(string name, float border)
        {
            string key = name + ":" + border;
            if (SpriteCache.TryGetValue(key, out Sprite sprite) && sprite != null)
                return sprite;

            Texture2D texture = Resources.Load<Texture2D>("PremiumUI/" + name);
            if (texture == null) return null;

            Vector4 borders = border <= 0f ? Vector4.zero : new Vector4(border, border, border, border);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borders);
            sprite.name = name + " Runtime Sprite";
            SpriteCache[key] = sprite;
            return sprite;
        }

        private static Image EnsureImage(Transform parent, string objectName, Sprite sprite)
        {
            Transform existing = parent.Find(objectName);
            Image image;
            if (existing != null)
            {
                image = existing.GetComponent<Image>();
                if (image == null) image = existing.gameObject.AddComponent<Image>();
            }
            else
            {
                GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                child.layer = parent.gameObject.layer;
                child.transform.SetParent(parent, false);
                image = child.GetComponent<Image>();
            }

            image.sprite = sprite;
            return image;
        }

        private static void DisableNamedGraphic(Transform root, string objectName)
        {
            Transform target = FindTransform(root, objectName);
            if (target != null && target != root)
                target.gameObject.SetActive(false);
        }

        private static void SetPremiumButtonColours(Button button)
        {
            if (button == null) return;
            ColorBlock colours = button.colors;
            colours.normalColor = Color.white;
            colours.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colours.pressedColor = new Color(0.78f, 0.88f, 0.96f, 1f);
            colours.selectedColor = Color.white;
            colours.disabledColor = new Color(0.40f, 0.46f, 0.54f, 0.72f);
            colours.colorMultiplier = 1f;
            colours.fadeDuration = 0.08f;
            button.colors = colours;
        }

        private static void AddDepth(Graphic graphic, Color accent, float outlineDistance, float shadowDistance)
        {
            if (graphic == null) return;

            Outline outline = graphic.GetComponent<Outline>();
            if (outline == null) outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = accent;
            outline.effectDistance = new Vector2(outlineDistance, -outlineDistance);
            outline.useGraphicAlpha = true;

            Shadow shadow = null;
            foreach (Shadow effect in graphic.GetComponents<Shadow>())
            {
                if (!(effect is Outline))
                {
                    shadow = effect;
                    break;
                }
            }

            if (shadow == null) shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.005f, 0.02f, 0.06f, 0.72f);
            shadow.effectDistance = new Vector2(0f, -shadowDistance);
            shadow.useGraphicAlpha = true;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size, Vector2 pivot)
        {
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static Image GetButtonImage(Button button)
        {
            if (button == null) return null;
            Image image = button.targetGraphic as Image;
            if (image == null) image = button.GetComponent<Image>();
            return image;
        }

        private static Transform FindTransform(Transform root, string objectName)
        {
            if (root == null) return null;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
                if (transforms[i].name == objectName) return transforms[i];
            return null;
        }

        private static RectTransform FindRect(Transform root, string objectName)
        {
            return FindTransform(root, objectName) as RectTransform;
        }

        private static Image FindImage(Transform root, string objectName)
        {
            Transform target = FindTransform(root, objectName);
            return target == null ? null : target.GetComponent<Image>();
        }

        private static Button FindButton(Transform root, string objectName)
        {
            Transform target = FindTransform(root, objectName);
            return target == null ? null : target.GetComponent<Button>();
        }

        private static TMP_Text FindText(Transform root, string objectName)
        {
            Transform target = FindTransform(root, objectName);
            return target == null ? null : target.GetComponent<TMP_Text>();
        }
    }
}
