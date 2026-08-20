using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Watermelon
{
    public class UILevelQuitPopUp : MonoBehaviour, IPopupWindow
    {
        [SerializeField] Button closeSmallButton;
        [SerializeField] Button closeBigButton;
        [SerializeField] Button confirmButton;

        public SimpleCallback OnCancelExitEvent;
        public SimpleCallback OnConfirmExitEvent;

        private bool isOpened;
        public bool IsOpened => isOpened;
        private CanvasGroup canvasGroup;
        private float lastManualInputTime = -1f;
        private bool bannerWasEnabled;

        private void Awake()
        {
            PremiumUIStyler.StyleQuitPopup(transform);
            CacheComponents();
            BindButton(closeSmallButton, ExitPopCloseButton);
            BindButton(closeBigButton, ExitPopCloseButton);
            BindButton(confirmButton, ExitPopUpConfirmExitButton);
        }

        private void Update()
        {
            if (!isOpened)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitPopCloseButton();
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                HandleManualTouchFallback(Input.mousePosition);
            }
        }

        public void Show()
        {
            if (isOpened)
                return;

            CacheComponents();
            isOpened = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ConfigureModalSorting();
            SetInputState(true);

            bannerWasEnabled = AdsManager.IsBannerEnabled;
            if (bannerWasEnabled)
                AdsManager.DisableBanner();

            UIController.OnPopupWindowOpened(this);
        }

        public void Hide()
        {
            if (!isOpened)
                return;

            isOpened = false;
            SetInputState(false);
            gameObject.SetActive(false);

            UIController.OnPopupWindowClosed(this);

            if (bannerWasEnabled)
            {
                bannerWasEnabled = false;
                AdsManager.EnableBanner();
            }
        }

        public void ExitPopCloseButton()
        {
            Debug.Log("QUIT_POPUP_CANCEL_CLICKED");

            AudioController.PlaySound(AudioController.Sounds.buttonSound);

            OnCancelExitEvent?.Invoke();

            Hide();
        }

        public void ExitPopUpConfirmExitButton()
        {
            Debug.Log("QUIT_POPUP_CONFIRM_CLICKED");

            AudioController.PlaySound(AudioController.Sounds.buttonSound);

            OnConfirmExitEvent?.Invoke();

            Hide();
        }

        private void ConfigureModalSorting()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CacheComponents()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (closeSmallButton == null)
                closeSmallButton = transform.Find("Panel Back/Close Button")?.GetComponent<Button>();

            if (confirmButton == null)
                confirmButton = transform.Find("Panel Back/Quit Button")?.GetComponent<Button>();

            if (closeBigButton == null)
                closeBigButton = GetComponent<Button>();
        }

        private void BindButton(Button button, UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
            button.interactable = true;

            if (button.targetGraphic != null)
                button.targetGraphic.raycastTarget = true;
        }

        private void SetInputState(bool state)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = state;
                canvasGroup.blocksRaycasts = state;
            }

            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = state;

            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void HandleManualTouchFallback(Vector2 screenPosition)
        {
            if (Time.unscaledTime - lastManualInputTime < 0.15f)
                return;

            if (IsFallbackCloseHit(screenPosition))
            {
                lastManualInputTime = Time.unscaledTime;
                Debug.Log("QUIT_POPUP_MANUAL_CANCEL_HIT_SAFE_ZONE");
                ExitPopCloseButton();
                return;
            }

            if (IsFallbackConfirmHit(screenPosition))
            {
                lastManualInputTime = Time.unscaledTime;
                Debug.Log("QUIT_POPUP_MANUAL_CONFIRM_HIT_SAFE_ZONE");
                ExitPopUpConfirmExitButton();
                return;
            }

            if (IsButtonHit(closeSmallButton, screenPosition))
            {
                lastManualInputTime = Time.unscaledTime;
                Debug.Log("QUIT_POPUP_MANUAL_CANCEL_HIT");
                ExitPopCloseButton();
                return;
            }

            if (IsButtonHit(confirmButton, screenPosition))
            {
                lastManualInputTime = Time.unscaledTime;
                Debug.Log("QUIT_POPUP_MANUAL_CONFIRM_HIT");
                ExitPopUpConfirmExitButton();
                return;
            }
        }

        private bool IsButtonHit(Button button, Vector2 screenPosition)
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

        private bool IsFallbackCloseHit(Vector2 screenPosition)
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return false;

            float normalizedX = screenPosition.x / Screen.width;
            float normalizedY = screenPosition.y / Screen.height;

            return normalizedX >= 0.72f && normalizedX <= 0.95f && normalizedY >= 0.58f && normalizedY <= 0.82f;
        }

        private bool IsFallbackConfirmHit(Vector2 screenPosition)
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return false;

            float normalizedX = screenPosition.x / Screen.width;
            float normalizedY = screenPosition.y / Screen.height;

            return normalizedX >= 0.25f && normalizedX <= 0.75f && normalizedY >= 0.24f && normalizedY <= 0.48f;
        }
    }
}
