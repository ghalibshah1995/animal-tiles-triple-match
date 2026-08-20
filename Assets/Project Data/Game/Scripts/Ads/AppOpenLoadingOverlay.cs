using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Small branded cold-start wait surface. It is deliberately separate from
    /// InterstitialLoadingOverlay so the premium 3-2-1 ad countdown is never used
    /// for App Open, Rewarded or Banner ads.
    /// </summary>
    public sealed class AppOpenLoadingOverlay : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private RectTransform spinner;

        public void Initialise()
        {
            if (canvasGroup != null)
                return;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 31990;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

            Image background = CreateImage("BrandedBackground", transform,
                new Color(0.008f, 0.035f, 0.11f, 0.98f));
            Stretch(background.rectTransform);
            background.raycastTarget = true;

            TextMeshProUGUI title = CreateText("GameTitle", transform,
                "ANIMAL TILES:\nTRIPLE MATCH", 64f, Color.white);
            SetCentered(title.rectTransform, new Vector2(780f, 190f), new Vector2(0f, 80f));
            title.fontStyle = FontStyles.Bold;
            title.outlineColor = new Color(0f, 0.25f, 0.55f, 1f);
            title.outlineWidth = 0.16f;

            spinner = CreateRect("LoadingSpinner", transform);
            SetCentered(spinner, new Vector2(110f, 110f), new Vector2(0f, -90f));
            BuildSpinner(spinner);

            TextMeshProUGUI status = CreateText("Status", transform, "LOADING", 30f,
                new Color(0.52f, 0.82f, 1f, 1f));
            SetCentered(status.rectTransform, new Vector2(460f, 70f), new Vector2(0f, -190f));
            status.characterSpacing = 8f;

            Hide();
        }

        public void Show()
        {
            Initialise();
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
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
            if (spinner != null)
                spinner.Rotate(0f, 0f, -180f * Time.unscaledDeltaTime);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
            }
        }

        private static void BuildSpinner(RectTransform parent)
        {
            const int segments = 10;
            const float radius = 40f;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * (360f / segments);
                float radians = angle * Mathf.Deg2Rad;
                Image segment = CreateImage("Segment " + (i + 1), parent,
                    new Color(0.08f, 0.78f, 1f, Mathf.Lerp(0.18f, 1f, (i + 1f) / segments)));
                RectTransform rect = segment.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
                rect.sizeDelta = new Vector2(10f, 28f);
                rect.anchoredPosition = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * radius;
                rect.localEulerAngles = new Vector3(0f, 0f, -angle);
                segment.raycastTarget = false;
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value,
            float size, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
