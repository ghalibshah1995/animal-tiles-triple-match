using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class PUUIPurchasePanel : MonoBehaviour, IPopupWindow
    {
        private const int BoosterPopupSortingOrder = 10000;

        [SerializeField] GameObject powerUpPurchasePanel;
        [SerializeField] RectTransform safeAreaTransform;

        [Space(5)]
        [SerializeField] Image powerUpPurchasePreview;
        [SerializeField] TMP_Text powerUpPurchaseAmountText;
        [SerializeField] TMP_Text powerUpPurchaseDescriptionText;
        [SerializeField] TMP_Text powerUpPurchasePriceText;
        [SerializeField] Image powerUpPurchaseIcon;

        [Space(5)]
        [SerializeField] Button smallCloseButton;
        [SerializeField] Button bigCloseButton;
        [SerializeField] Button purchaseButton;

        private Button rewardedButton;

        [Space(5)]
        [SerializeField] CurrencyUIPanelSimple currencyPanel;

        private PUSettings settings;

        private bool isOpened;
        public bool IsOpened => isOpened;
        private bool isPurchaseInProgress;
        private bool isRuntimeInitialised;
        private bool bannerWasEnabled;
        private float lastManualInputTime = -1f;
        private int lastHandledInputFrame = -1;

        private void Awake()
        {
            EnsureRuntimeInitialised();
        }

        private void Update()
        {
            if (!isOpened || !Input.GetMouseButtonUp(0) || lastHandledInputFrame == Time.frameCount)
                return;

            HandleManualTouchFallback(Input.mousePosition);
        }

        private void EnsureRuntimeInitialised()
        {
            if (isRuntimeInitialised)
                return;

            ConfigurePremiumLayout();

            smallCloseButton.onClick.AddListener(ClosePurchasePUPanel);
            bigCloseButton.onClick.AddListener(ClosePurchasePUPanel);
            purchaseButton.onClick.AddListener(PurchasePUButton);
            rewardedButton.onClick.AddListener(RewardedPUButton);

            PremiumUIStyler.StyleBoosterPopup(powerUpPurchasePanel.transform, powerUpPurchasePreview,
                powerUpPurchaseAmountText, powerUpPurchaseDescriptionText, purchaseButton,
                rewardedButton, smallCloseButton);

            rewardedButton.gameObject.SetActive(AdMobConfiguration.AdsEnabled);

            isRuntimeInitialised = true;
        }

        public void Initialise()
        {
            NotchSaveArea.RegisterRectTransform(safeAreaTransform);
        }

        public void Show(PUSettings settings)
        {
            if (settings == null)
                return;

            EnsureRuntimeInitialised();

            this.settings = settings;
            isOpened = true;
            isPurchaseInProgress = false;
            purchaseButton.interactable = true;
            rewardedButton.interactable = AdMobConfiguration.AdsEnabled;
            rewardedButton.gameObject.SetActive(AdMobConfiguration.AdsEnabled);

            // The gameplay HUD already shows the live coin total behind this modal.
            // The prefab's second currency panel caused the stacked coin/number seen on device.
            currencyPanel.gameObject.SetActive(false);

            powerUpPurchasePanel.SetActive(true);
            powerUpPurchasePanel.transform.SetAsLastSibling();
            ConfigureModalSorting(powerUpPurchasePanel);
            SetInputState(true);

            bannerWasEnabled = AdsManager.IsBannerEnabled;
            if (bannerWasEnabled)
                AdsManager.DisableBanner();

            powerUpPurchasePreview.sprite = settings.Icon;
            powerUpPurchaseDescriptionText.text = settings.Description;
            powerUpPurchasePriceText.text = settings.Price.ToString();
            powerUpPurchaseAmountText.text = string.Format("x{0}", settings.PurchaseAmount);

            Currency currency = CurrenciesController.GetCurrency(settings.CurrencyType);
            powerUpPurchaseIcon.sprite = currency.Icon;

            UIController.OnPopupWindowOpened(this);
        }

        public void PurchasePUButton()
        {
            if (isPurchaseInProgress || settings == null)
                return;

            Debug.Log("BOOSTER_POPUP_BUY_CLICKED type=" + settings.Type);
            lastHandledInputFrame = Time.frameCount;

            isPurchaseInProgress = true;
            purchaseButton.interactable = false;

            AudioController.PlaySound(AudioController.Sounds.buttonSound);

            try
            {
                bool purchaseSuccessful = PUController.PurchasePowerUp(settings.Type);

                if (purchaseSuccessful)
                {
                    ClosePurchasePUPanel();
                    return;
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }

            // Never leave popup input locked after a rejected/failed purchase.
            isPurchaseInProgress = false;
            purchaseButton.interactable = true;
            rewardedButton.interactable = true;
        }

        public void RewardedPUButton()
        {
            if (!AdMobConfiguration.AdsEnabled)
                return;

            if (isPurchaseInProgress || settings == null)
                return;

            Debug.Log("BOOSTER_POPUP_REWARDED_CLICKED type=" + settings.Type);
            lastHandledInputFrame = Time.frameCount;

            isPurchaseInProgress = true;
            purchaseButton.interactable = false;
            rewardedButton.interactable = false;

            AudioController.PlaySound(AudioController.Sounds.buttonSound);
            AdsManager.ShowRewardBasedVideo(success =>
            {
                if (success)
                {
                    PUController.AddPowerUp(settings.Type, 1);
                    ClosePurchasePUPanel();
                    return;
                }

                isPurchaseInProgress = false;
                purchaseButton.interactable = true;
                rewardedButton.interactable = true;
            });
        }

        public void ClosePurchasePUPanel()
        {
            if (!isOpened)
                return;

            Debug.Log("BOOSTER_POPUP_CLOSE_CLICKED");
            lastHandledInputFrame = Time.frameCount;

            isOpened = false;
            isPurchaseInProgress = false;
            purchaseButton.interactable = true;
            rewardedButton.interactable = true;
            SetInputState(false);
            powerUpPurchasePanel.SetActive(false);

            UIController.OnPopupWindowClosed(this);

            if (bannerWasEnabled)
            {
                bannerWasEnabled = false;
                AdsManager.EnableBanner();
            }
        }

        private void HandleManualTouchFallback(Vector2 screenPosition)
        {
            if (Time.unscaledTime - lastManualInputTime < 0.15f)
                return;

            if (IsButtonHit(smallCloseButton, screenPosition))
            {
                lastManualInputTime = Time.unscaledTime;
                Debug.Log("BOOSTER_POPUP_MANUAL_CLOSE_HIT");
                ClosePurchasePUPanel();
                return;
            }

            if (IsButtonHit(purchaseButton, screenPosition))
            {
                lastManualInputTime = Time.unscaledTime;
                Debug.Log("BOOSTER_POPUP_MANUAL_BUY_HIT");
                PurchasePUButton();
                return;
            }

            if (IsButtonHit(rewardedButton, screenPosition))
            {
                lastManualInputTime = Time.unscaledTime;
                Debug.Log("BOOSTER_POPUP_MANUAL_REWARDED_HIT");
                RewardedPUButton();
            }
        }

        private static bool IsButtonHit(Button button, Vector2 screenPosition)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
                return false;

            RectTransform rectTransform = button.transform as RectTransform;
            if (rectTransform == null)
                return false;

            Canvas canvas = button.GetComponentInParent<Canvas>();
            Camera eventCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = canvas.worldCamera;

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, eventCamera);
        }

        private void SetInputState(bool state)
        {
            CanvasGroup canvasGroup = powerUpPurchasePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = powerUpPurchasePanel.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = state;
            canvasGroup.blocksRaycasts = state;

            GraphicRaycaster raycaster = powerUpPurchasePanel.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = state;
        }

        private void ConfigurePremiumLayout()
        {
            RectTransform panelBack = powerUpPurchasePanel.transform.Find("Panel Back") as RectTransform;
            if (panelBack != null)
            {
                panelBack.anchorMin = new Vector2(0.5f, 0.5f);
                panelBack.anchorMax = new Vector2(0.5f, 0.5f);
                panelBack.pivot = new Vector2(0.5f, 0.5f);
                panelBack.anchoredPosition = Vector2.zero;
                panelBack.localScale = Vector3.one;
                panelBack.sizeDelta = new Vector2(620f, 820f);
            }

            TMP_Text heading = FindText(powerUpPurchasePanel.transform, "Heading Text");
            if (heading != null)
            {
                heading.text = "NEED A BOOSTER?";
                heading.fontSize = 40f;
                heading.enableAutoSizing = true;
                heading.fontSizeMin = 30f;
                heading.fontSizeMax = 40f;
                heading.textWrappingMode = TextWrappingModes.NoWrap;
                heading.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                heading.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                heading.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                heading.rectTransform.sizeDelta = new Vector2(500f, 90f);
                heading.rectTransform.anchoredPosition = new Vector2(0f, -65f);
            }

            RectTransform zoneRect = powerUpPurchasePreview.transform.parent as RectTransform;
            if (zoneRect != null)
            {
                zoneRect.anchorMin = new Vector2(0.5f, 0.5f);
                zoneRect.anchorMax = new Vector2(0.5f, 0.5f);
                zoneRect.pivot = new Vector2(0.5f, 0.5f);
                zoneRect.anchoredPosition = new Vector2(0f, 45f);
                zoneRect.sizeDelta = new Vector2(500f, 500f);
            }

            powerUpPurchasePreview.rectTransform.sizeDelta = new Vector2(285f, 285f);
            powerUpPurchasePreview.rectTransform.anchoredPosition = new Vector2(0f, 85f);

            RectTransform descriptionRect = powerUpPurchaseDescriptionText.rectTransform;
            descriptionRect.anchorMin = new Vector2(0.5f, 0f);
            descriptionRect.anchorMax = new Vector2(0.5f, 0f);
            descriptionRect.pivot = new Vector2(0.5f, 0.5f);
            descriptionRect.sizeDelta = new Vector2(450f, 105f);
            descriptionRect.anchoredPosition = new Vector2(0f, 82f);
            powerUpPurchaseDescriptionText.enableAutoSizing = true;
            powerUpPurchaseDescriptionText.fontSizeMin = 22f;
            powerUpPurchaseDescriptionText.fontSizeMax = 32f;
            powerUpPurchaseDescriptionText.alignment = TextAlignmentOptions.Center;

            powerUpPurchaseAmountText.fontSize = 40f;
            powerUpPurchaseAmountText.enableAutoSizing = true;
            powerUpPurchaseAmountText.fontSizeMin = 28f;
            powerUpPurchaseAmountText.fontSizeMax = 40f;
            powerUpPurchaseAmountText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            powerUpPurchaseAmountText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            powerUpPurchaseAmountText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            powerUpPurchaseAmountText.rectTransform.sizeDelta = new Vector2(100f, 65f);
            powerUpPurchaseAmountText.rectTransform.anchoredPosition = new Vector2(145f, 155f);

            RectTransform buyRect = purchaseButton.transform as RectTransform;
            buyRect.anchorMin = new Vector2(0.5f, 0f);
            buyRect.anchorMax = new Vector2(0.5f, 0f);
            buyRect.pivot = new Vector2(0.5f, 0.5f);
            buyRect.localScale = Vector3.one;
            buyRect.sizeDelta = new Vector2(250f, 110f);
            // The rewarded/ad option is disabled, so keep the remaining purchase
            // action centered along the lower edge of the popup.
            buyRect.anchoredPosition = new Vector2(0f, 72f);

            TMP_Text buyLabel = FindText(purchaseButton.transform, "Buy Text (TMP)");
            if (buyLabel != null)
            {
                buyLabel.text = "BUY";
                buyLabel.enableAutoSizing = true;
                buyLabel.fontSizeMin = 20f;
                buyLabel.fontSizeMax = 30f;
                buyLabel.alignment = TextAlignmentOptions.Center;
                buyLabel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                buyLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                buyLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                buyLabel.rectTransform.sizeDelta = new Vector2(90f, 75f);
                buyLabel.rectTransform.anchoredPosition = new Vector2(-67f, 0f);
            }

            RectTransform currencyIconRect = powerUpPurchaseIcon.rectTransform;
            currencyIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            currencyIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            currencyIconRect.pivot = new Vector2(0.5f, 0.5f);
            currencyIconRect.sizeDelta = new Vector2(48f, 48f);
            currencyIconRect.anchoredPosition = new Vector2(5f, 0f);

            RectTransform priceRect = powerUpPurchasePriceText.rectTransform;
            priceRect.anchorMin = new Vector2(0.5f, 0.5f);
            priceRect.anchorMax = new Vector2(0.5f, 0.5f);
            priceRect.pivot = new Vector2(0.5f, 0.5f);
            priceRect.sizeDelta = new Vector2(75f, 75f);
            priceRect.anchoredPosition = new Vector2(68f, 0f);
            powerUpPurchasePriceText.enableAutoSizing = true;
            powerUpPurchasePriceText.fontSizeMin = 20f;
            powerUpPurchasePriceText.fontSizeMax = 30f;
            powerUpPurchasePriceText.alignment = TextAlignmentOptions.Center;

            rewardedButton = Instantiate(purchaseButton, purchaseButton.transform.parent);
            rewardedButton.name = "Watch Ad Button";

            RectTransform rewardedRect = rewardedButton.transform as RectTransform;
            rewardedRect.anchorMin = new Vector2(0.5f, 0f);
            rewardedRect.anchorMax = new Vector2(0.5f, 0f);
            rewardedRect.pivot = new Vector2(0.5f, 0.5f);
            rewardedRect.localScale = Vector3.one;
            rewardedRect.sizeDelta = new Vector2(250f, 110f);
            rewardedRect.anchoredPosition = new Vector2(135f, 85f);

            Image rewardedBackground = rewardedButton.GetComponent<Image>();
            if (rewardedBackground != null)
                rewardedBackground.color = new Color(0.45f, 0.95f, 1f, 1f);

            TMP_Text rewardedLabel = FindText(rewardedButton.transform, "Buy Text (TMP)");
            if (rewardedLabel != null)
            {
                rewardedLabel.text = "WATCH AD\n+1 BOOSTER";
                rewardedLabel.fontSize = 28f;
                rewardedLabel.enableAutoSizing = true;
                rewardedLabel.fontSizeMin = 18f;
                rewardedLabel.fontSizeMax = 28f;
                rewardedLabel.alignment = TextAlignmentOptions.Center;
                rewardedLabel.rectTransform.anchorMin = Vector2.zero;
                rewardedLabel.rectTransform.anchorMax = Vector2.one;
                rewardedLabel.rectTransform.offsetMin = new Vector2(16f, 10f);
                rewardedLabel.rectTransform.offsetMax = new Vector2(-16f, -10f);
            }

            RectTransform closeRect = smallCloseButton.transform as RectTransform;
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.localScale = Vector3.one;
            closeRect.sizeDelta = new Vector2(100f, 100f);
            closeRect.anchoredPosition = new Vector2(-25f, -25f);

            for (int i = 0; i < rewardedButton.transform.childCount; i++)
            {
                Transform child = rewardedButton.transform.GetChild(i);
                if (child.gameObject.name != "Buy Text (TMP)")
                    child.gameObject.SetActive(false);
            }
        }

        private static TMP_Text FindText(Transform root, string objectName)
        {
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].gameObject.name == objectName)
                    return texts[i];
            }

            return null;
        }

        private static void ConfigureModalSorting(GameObject popupRoot)
        {
            Canvas canvas = popupRoot.GetComponent<Canvas>();
            if (canvas == null)
                canvas = popupRoot.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = BoosterPopupSortingOrder;

            if (popupRoot.GetComponent<GraphicRaycaster>() == null)
                popupRoot.AddComponent<GraphicRaycaster>();
        }
    }
}
