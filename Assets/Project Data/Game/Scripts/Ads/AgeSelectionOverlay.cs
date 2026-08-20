using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Neutral first-run age-group selector. It stores only the selected bracket;
    /// no birth date or personally identifiable information is collected.
    /// </summary>
    public sealed class AgeSelectionOverlay : MonoBehaviour
    {
        private const string AgeGroupPref = "privacy_age_group_v1";

        public enum AgeGroup
        {
            Unknown = 0,
            Teen13To15 = 1,
            Teen16To17 = 2,
            Adult18Plus = 3
        }

        private Canvas canvas;
        private Action<AgeGroup> selectionCallback;

        public static AgeGroup StoredAgeGroup =>
            (AgeGroup)PlayerPrefs.GetInt(AgeGroupPref, (int)AgeGroup.Unknown);

        public void Show(Action<AgeGroup> callback)
        {
            selectionCallback = callback;
            EnsureEventSystem();
            BuildUI();
            gameObject.SetActive(true);
        }

        private void BuildUI()
        {
            if (canvas != null)
                return;

            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(720f, 1600f);
            gameObject.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            Image backdrop = CreateImage("Backdrop", transform, new Color(0.015f, 0.06f, 0.16f, 0.985f));
            Stretch(backdrop.rectTransform);

            Image card = CreateImage("Age Card", transform, new Color(0.025f, 0.32f, 0.58f, 1f));
            card.rectTransform.anchorMin = new Vector2(0.08f, 0.22f);
            card.rectTransform.anchorMax = new Vector2(0.92f, 0.78f);
            card.rectTransform.offsetMin = Vector2.zero;
            card.rectTransform.offsetMax = Vector2.zero;

            CreateText("Title", card.transform, "SELECT YOUR AGE GROUP", 45, FontStyle.Bold,
                new Vector2(0.06f, 0.77f), new Vector2(0.94f, 0.94f));
            CreateText("Message", card.transform,
                "Choose your age range so we can apply the correct privacy and advertising settings.",
                25, FontStyle.Normal, new Vector2(0.10f, 0.60f), new Vector2(0.90f, 0.77f));

            CreateChoice(card.transform, "AGES 13-15", AgeGroup.Teen13To15, 0.48f);
            CreateChoice(card.transform, "AGES 16-17", AgeGroup.Teen16To17, 0.32f);
            CreateChoice(card.transform, "AGE 18+", AgeGroup.Adult18Plus, 0.16f);

            CreateText("Privacy Note", card.transform,
                "We store only this age range on your device.", 19, FontStyle.Normal,
                new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.12f));
        }

        private void CreateChoice(Transform parent, string label, AgeGroup group, float bottom)
        {
            Image image = CreateImage(label, parent, new Color(0.08f, 0.78f, 0.46f, 1f));
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.15f, bottom);
            rect.anchorMax = new Vector2(0.85f, bottom + 0.12f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => Select(group));
            CreateText("Label", image.transform, label, 31, FontStyle.Bold, Vector2.zero, Vector2.one);
        }

        private void Select(AgeGroup group)
        {
            PlayerPrefs.SetInt(AgeGroupPref, (int)group);
            PlayerPrefs.Save();

            Action<AgeGroup> callback = selectionCallback;
            selectionCallback = null;
            gameObject.SetActive(false);
            callback?.Invoke(group);
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);
            Image image = child.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void CreateText(string name, Transform parent, string value, int size, FontStyle style,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            child.transform.SetParent(parent, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = child.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            new GameObject("Privacy Event System", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
