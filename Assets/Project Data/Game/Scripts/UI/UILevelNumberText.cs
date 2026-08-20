using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UILevelNumberText : MonoBehaviour
    {
        private const string LEVEL_LABEL = "LEVEL {0}";
        private static UILevelNumberText instance;

        [SerializeField] UIScaleAnimation uIScalableObject;

        private static UIScaleAnimation UIScalableObject => instance != null ? instance.uIScalableObject : null;
        private TextMeshProUGUI levelNumberText;

        private static bool IsDisplayed = false;

        private void Awake()
        {
            instance = this;
            levelNumberText = GetComponent<TextMeshProUGUI>();
        }

        public static void Show(bool immediately = false)
        {
            if(instance == null)
                return;

            instance.ForceVisible();
        }

        public void ForceVisible()
        {
            if(levelNumberText == null)
                levelNumberText = GetComponent<TextMeshProUGUI>();

            gameObject.SetActive(true);

            var rectTransform = (RectTransform)transform;
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, -125f);
            rectTransform.sizeDelta = new Vector2(240f, 80f);
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.SetAsLastSibling();

            if(levelNumberText != null)
            {
                levelNumberText.enabled = true;
                levelNumberText.alpha = 1f;
                levelNumberText.alignment = TextAlignmentOptions.Center;
                levelNumberText.canvasRenderer.SetAlpha(1f);
            }

            IsDisplayed = true;
        }

        public static void Hide(bool immediately = false)
        {
            if (!IsDisplayed || instance == null)
                return;

            if (immediately)
                IsDisplayed = false;

            if (UIScalableObject == null)
            {
                IsDisplayed = false;
                if (instance.levelNumberText != null)
                    instance.levelNumberText.enabled = false;

                return;
            }

            UIScalableObject.Hide(scaleMultiplier: 1.05f, immediately: immediately, onCompleted: delegate
            {
                IsDisplayed = false;
                if (instance != null && instance.levelNumberText != null)
                    instance.levelNumberText.enabled = false;
            });
        }

        public void UpdateLevelNumber(int number)
        {
            if (levelNumberText == null)
                levelNumberText = GetComponent<TextMeshProUGUI>();

            if (levelNumberText == null)
            {
                Debug.LogWarning($"UILevelNumberText is missing TextMeshProUGUI on {name}; level label update skipped.");
                return;
            }

            if (!levelNumberText.enabled)
                levelNumberText.enabled = true;

            levelNumberText.text = string.Format(LEVEL_LABEL, number);
            ForceVisible();
        }

    }
}
