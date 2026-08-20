using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Premium, runtime-built transition used exclusively before a reserved and
    /// ready interstitial. Every visual part is a separate Unity UI element so
    /// the countdown and radial progress remain live and resolution-independent.
    /// </summary>
    public sealed class InterstitialLoadingOverlay : MonoBehaviour
    {
        private const int OverlaySortingOrder = 32000;

        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private RectTransform safeArea;
        private Image progressFill;
        private TextMeshProUGUI countdownText;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.99f;

        public void Initialise()
        {
            if (canvas != null)
                return;

            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // This top-level graphic covers the complete physical display rather
            // than only the safe area, so no edge, cutout or navigation-zone tap
            // can reach the result screen.
            Image blocker = CreateImage("FullScreenBlocker", transform, null,
                new Color(0.005f, 0.008f, 0.015f, 0.78f));
            Stretch(blocker.rectTransform);
            blocker.raycastTarget = true;

            Image vignette = CreateImage("DimOverlay", transform, null,
                new Color(0.015f, 0.04f, 0.09f, 0.28f));
            Stretch(vignette.rectTransform);
            vignette.raycastTarget = false;

            safeArea = CreateRect("SafeArea", transform);
            Stretch(safeArea);

            Sprite panelSprite = LoadTextureSprite("AdLoading/ad_loading_panel");
            Image shadow = CreateImage("PanelShadow", safeArea, panelSprite,
                new Color(0f, 0f, 0f, 0.52f));
            SetCenteredRect(shadow.rectTransform, new Vector2(790f, 730f), new Vector2(0f, -14f));
            shadow.preserveAspect = true;
            shadow.raycastTarget = false;

            Image panel = CreateImage("LoadingPanel", safeArea, panelSprite, Color.white);
            SetCenteredRect(panel.rectTransform, new Vector2(790f, 730f), Vector2.zero);
            panel.preserveAspect = true;
            panel.raycastTarget = false;

            Sprite ribbonSprite = LoadTextureSprite("AdLoading/ad_loading_ribbon");
            Image ribbon = CreateImage("HeaderRibbon", panel.transform, ribbonSprite, Color.white);
            SetCenteredRect(ribbon.rectTransform, new Vector2(710f, 166f), new Vector2(0f, 323f));
            ribbon.preserveAspect = true;
            ribbon.raycastTarget = false;

            TextMeshProUGUI header = CreateText("HeaderText", ribbon.transform,
                "AD LOADING", 66f, Color.white);
            Stretch(header.rectTransform, 18f);
            header.fontStyle = FontStyles.Bold;
            header.characterSpacing = 1.2f;
            header.outlineColor = new Color(0.32f, 0.12f, 0f, 0.95f);
            header.outlineWidth = 0.18f;
            AddShadow(header, new Color(0.18f, 0.05f, 0f, 0.8f), new Vector2(0f, -5f));

            Sprite ringSprite = CreateRadialSprite(256, 0.70f);
            Sprite discSprite = CreateRadialSprite(256, 0f);

            Image ringGlow = CreateImage("ProgressRingGlow", panel.transform, ringSprite,
                new Color(0.03f, 0.66f, 1f, 0.22f));
            SetCenteredRect(ringGlow.rectTransform, new Vector2(302f, 302f), new Vector2(0f, 50f));
            ringGlow.raycastTarget = false;

            Image ringBackground = CreateImage("ProgressRingBackground", panel.transform, ringSprite,
                new Color(0.08f, 0.28f, 0.62f, 0.92f));
            SetCenteredRect(ringBackground.rectTransform, new Vector2(272f, 272f), new Vector2(0f, 50f));
            ringBackground.raycastTarget = false;

            progressFill = CreateImage("ProgressRingFill", panel.transform, ringSprite,
                new Color(0.03f, 0.82f, 1f, 1f));
            SetCenteredRect(progressFill.rectTransform, new Vector2(272f, 272f), new Vector2(0f, 50f));
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Radial360;
            progressFill.fillOrigin = (int)Image.Origin360.Top;
            progressFill.fillClockwise = true;
            progressFill.fillAmount = 1f;
            progressFill.raycastTarget = false;

            Image innerDisc = CreateImage("CountdownDisc", panel.transform, discSprite,
                new Color(0.025f, 0.09f, 0.22f, 0.98f));
            SetCenteredRect(innerDisc.rectTransform, new Vector2(218f, 218f), new Vector2(0f, 50f));
            innerDisc.raycastTarget = false;

            countdownText = CreateText("CountdownText", innerDisc.transform, "3", 116f, Color.white);
            Stretch(countdownText.rectTransform, 8f);
            countdownText.fontStyle = FontStyles.Bold;
            countdownText.outlineColor = new Color(0.01f, 0.025f, 0.08f, 0.98f);
            countdownText.outlineWidth = 0.18f;
            AddShadow(countdownText, new Color(0f, 0f, 0f, 0.78f), new Vector2(0f, -7f));

            TextMeshProUGUI loading = CreateText("LoadingText", panel.transform,
                "Loading ad...", 50f, Color.white);
            SetCenteredRect(loading.rectTransform, new Vector2(610f, 72f), new Vector2(0f, -164f));
            loading.fontStyle = FontStyles.Bold;
            loading.outlineWidth = 0.12f;
            AddShadow(loading, new Color(0f, 0f, 0f, 0.7f), new Vector2(0f, -4f));

            TextMeshProUGUI pleaseWait = CreateText("PleaseWaitText", panel.transform,
                "Please wait a moment", 35f, new Color(0.70f, 0.78f, 0.92f, 1f));
            SetCenteredRect(pleaseWait.rectTransform, new Vector2(620f, 60f), new Vector2(0f, -224f));
            pleaseWait.outlineWidth = 0.07f;

            CreateSparkle(panel.transform, "CyanSparkle", new Vector2(-258f, 108f), 28f,
                new Color(0.20f, 0.76f, 1f, 1f));
            CreateSparkle(panel.transform, "BlueSparkle", new Vector2(-232f, -42f), 17f,
                new Color(0.18f, 0.48f, 1f, 1f));
            CreateSparkle(panel.transform, "GoldSparkleTop", new Vector2(254f, 122f), 30f,
                new Color(1f, 0.78f, 0.18f, 1f));
            CreateSparkle(panel.transform, "GoldSparkleBottom", new Vector2(232f, -56f), 24f,
                new Color(1f, 0.70f, 0.10f, 1f));

            gameObject.SetActive(false);
        }

        public void Show(int countdown)
        {
            Initialise();
            ApplySafeArea();
            SetCountdown(countdown);
            SetProgress(0f);
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void SetCountdown(int countdown)
        {
            if (countdownText == null)
                return;

            countdownText.text = countdown.ToString();
            countdownText.rectTransform.localScale = Vector3.one * 1.08f;
        }

        /// <param name="elapsedNormalized">0 at opening and 1 after exactly three seconds.</param>
        public void SetProgress(float elapsedNormalized)
        {
            if (progressFill != null)
                progressFill.fillAmount = 1f - Mathf.Clamp01(elapsedNormalized);
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (countdownText != null)
            {
                countdownText.rectTransform.localScale = Vector3.Lerp(
                    countdownText.rectTransform.localScale, Vector3.one,
                    12f * Time.unscaledDeltaTime);
            }

            // Read and intentionally consume the back intent at the highest UI
            // layer. The underlying result CanvasGroup is non-interactable too.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
            }
        }

        private void ApplySafeArea()
        {
            Rect area = Screen.safeArea;
            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);
            safeArea.anchorMin = new Vector2(area.xMin / width, area.yMin / height);
            safeArea.anchorMax = new Vector2(area.xMax / width, area.yMax / height);
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
        }

        private static Sprite LoadTextureSprite(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
                return null;

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite CreateRadialSprite(int size, float innerRadius)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = innerRadius > 0f ? "Ad Loading Ring" : "Ad Loading Disc",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            Vector2 center = Vector2.one * ((size - 1f) * 0.5f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float outerAlpha = 1f - Mathf.SmoothStep(0.94f, 1f, distance);
                    float innerAlpha = innerRadius <= 0f
                        ? 1f
                        : Mathf.SmoothStep(innerRadius, innerRadius + 0.045f, distance);
                    byte alpha = (byte)Mathf.RoundToInt(255f * outerAlpha * innerAlpha);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f,
                100f, 0, SpriteMeshType.FullRect);
        }

        private static void CreateSparkle(Transform parent, string name, Vector2 position,
            float size, Color color)
        {
            RectTransform holder = CreateRect(name, parent);
            SetCenteredRect(holder, new Vector2(size * 1.8f, size * 1.8f), position);

            Image diamond = CreateImage("Diamond", holder, null, color);
            SetCenteredRect(diamond.rectTransform, new Vector2(size * 0.72f, size * 0.72f), Vector2.zero);
            diamond.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            diamond.raycastTarget = false;

            Color soft = new Color(color.r, color.g, color.b, color.a * 0.65f);
            Image vertical = CreateImage("VerticalGlow", holder, null, soft);
            SetCenteredRect(vertical.rectTransform, new Vector2(size * 0.16f, size * 1.65f), Vector2.zero);
            vertical.raycastTarget = false;

            Image horizontal = CreateImage("HorizontalGlow", holder, null, soft);
            SetCenteredRect(horizontal.rectTransform, new Vector2(size * 1.65f, size * 0.16f), Vector2.zero);
            horizontal.raycastTarget = false;
        }

        private static void AddShadow(Graphic graphic, Color color, Vector2 distance)
        {
            Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value,
            float fontSize, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.outlineColor = new Color(0.005f, 0.02f, 0.08f, 0.95f);
            text.outlineWidth = 0.10f;
            return text;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one * 0.5f;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
