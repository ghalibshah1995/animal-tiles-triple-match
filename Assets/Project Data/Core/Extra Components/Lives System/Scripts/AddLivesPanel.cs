using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class AddLivesPanel : MonoBehaviour
    {
        [SerializeField] RectTransform panel;
        [SerializeField] Vector3 hidePos;
        private Vector3 showPos;

        [SerializeField] Image backgroundImage;

        [SerializeField] Button button;
        [SerializeField] Button closeButton;

        [SerializeField] TMP_Text livesAmountText;
        [SerializeField] TMP_Text timeText;
        [SerializeField] AudioClip lifeRecievedAudio;

        Color backColor;

        private static int openedStack;
        public static bool IsPanelOpened => openedStack != 0;

        public SimpleBoolCallback OnPanelClosedCallback;
        private bool isShown;
        private bool bannerWasEnabled;

        private void Awake()
        {
            Init();
        }

        public void Init()
        {
            backColor = backgroundImage.color;
            showPos = panel.anchoredPosition;

            ConfigurePremiumLayout();
            PremiumUIStyler.StyleLivesPopup(transform, panel, button, closeButton, livesAmountText, timeText);

            button.gameObject.SetActive(AdMobConfiguration.AdsEnabled);
            button.onClick.AddListener(OnButtonClick);
            closeButton.onClick.AddListener(Hide);
        }

        private void ConfigurePremiumLayout()
        {
            TMP_Text[] buttonTexts = button.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < buttonTexts.Length; i++)
            {
                TMP_Text label = buttonTexts[i];
                if (label.gameObject.name == "Refill Text")
                {
                    label.text = "WATCH AD  +1 LIFE";
                    label.fontSize = 38f;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 26f;
                    label.fontSizeMax = 38f;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.alignment = TextAlignmentOptions.Center;
                    label.rectTransform.localScale = Vector3.one;
                    label.rectTransform.anchorMin = Vector2.zero;
                    label.rectTransform.anchorMax = Vector2.one;
                    label.rectTransform.offsetMin = new Vector2(18f, 12f);
                    label.rectTransform.offsetMax = new Vector2(-18f, -12f);
                }
                else
                {
                    label.gameObject.SetActive(false);
                }
            }

            Image[] buttonImages = button.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < buttonImages.Length; i++)
            {
                if (buttonImages[i].gameObject.name == "Ad Image")
                    buttonImages[i].gameObject.SetActive(false);
            }

            livesAmountText.fontSize = 94f;
            livesAmountText.enableAutoSizing = true;
            livesAmountText.fontSizeMin = 64f;
            livesAmountText.fontSizeMax = 94f;
            livesAmountText.rectTransform.anchoredPosition = new Vector2(-18f, -4f);

            timeText.fontSize = 44f;
            timeText.enableAutoSizing = true;
            timeText.fontSizeMin = 30f;
            timeText.fontSizeMax = 44f;

            TMP_Text[] panelTexts = panel.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < panelTexts.Length; i++)
            {
                if (panelTexts[i].gameObject.name == "Time Description Text")
                {
                    panelTexts[i].text = "Next life in";
                    panelTexts[i].fontSize = 38f;
                    panelTexts[i].enableAutoSizing = true;
                    panelTexts[i].fontSizeMin = 28f;
                    panelTexts[i].fontSizeMax = 38f;
                    panelTexts[i].rectTransform.anchoredPosition = new Vector2(0f, -155f);
                }
            }
        }

        private void OnEnable()
        {
            LivesManager.AddPanel(this);
        }

        private void OnDisable()
        {
            LivesManager.RemovePanel(this);
        }

        public void Show(SimpleBoolCallback onPanelClosed = null)
        {
            if (isShown)
            {
                OnPanelClosedCallback = onPanelClosed;
                return;
            }

            gameObject.SetActive(true);
            isShown = true;

            bannerWasEnabled = AdsManager.IsBannerEnabled;
            if (bannerWasEnabled)
                AdsManager.DisableBanner();

            backgroundImage.color = Color.clear;
            backgroundImage.DOColor(backColor, 0.3f);

            panel.anchoredPosition = hidePos;
            panel.DOAnchoredPosition(showPos, 0.3f).SetEasing(Ease.Type.SineOut);

            openedStack++;

            OnPanelClosedCallback = onPanelClosed;
        }

        public void Hide()
        {
            if (!isShown)
                return;

            isShown = false;
            backgroundImage.DOColor(Color.clear, 0.3f);
            panel.DOAnchoredPosition(hidePos, 0.3f).SetEasing(Ease.Type.SineIn).OnComplete(() => gameObject.SetActive(false));

            openedStack = Mathf.Max(0, openedStack - 1);

            SimpleBoolCallback callback = OnPanelClosedCallback;
            OnPanelClosedCallback = null;
            callback?.Invoke(false);

            if (bannerWasEnabled)
            {
                bannerWasEnabled = false;
                AdsManager.EnableBanner();
            }
        }

        public void OnButtonClick()
        {
            if (!AdMobConfiguration.AdsEnabled)
                return;

            button.interactable = false;
            AdsManager.ShowRewardBasedVideo(success =>
            {
                button.interactable = true;

                if (success)
                {
                    LivesManager.AddLife();
                    SaveController.Save(true);

                    if (lifeRecievedAudio != null)
                        AudioController.PlaySound(lifeRecievedAudio);

                    SimpleBoolCallback callback = OnPanelClosedCallback;
                    OnPanelClosedCallback = null;
                    callback?.Invoke(true);

                    Hide();
                }
            });
        }

        public void SetLivesCount(int count)
        {
            livesAmountText.text = count.ToString();
        }

        public void SetTime(string time)
        {
            timeText.text = time;
        }
    }
}
