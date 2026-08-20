using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIGameOver : UIPage
    {
        [SerializeField] RectTransform safeAreaRectTransform;
        
        [SerializeField] UIScaleAnimation levelFailed;
        [SerializeField] UIFadeAnimation backgroundFade;

        [SerializeField] Button menuButton;
        [SerializeField] Button replayButton;
        [SerializeField] Button reviveButton;

        [SerializeField] UIScaleAnimation menuButtonScalable;
        [SerializeField] UIScaleAnimation replayButtonScalable;
        [SerializeField] UIScaleAnimation reviveButtonScalable;

        [SerializeField] LivesIndicator livesIndicator;
        [SerializeField] AddLivesPanel addLivesPanel;

        private TweenCase continuePingPongCase;
        private TweenCase interstitialCheckTween;
        private CanvasGroup resultInteractionGroup;
        private bool interstitialCheckPerformed;

        public override void Initialise()
        {
            resultInteractionGroup = GetComponent<CanvasGroup>();
            if (resultInteractionGroup == null)
                resultInteractionGroup = gameObject.AddComponent<CanvasGroup>();

            PremiumUIAssets.ApplyHomeButton(menuButton);
            PremiumUIStyler.StyleFailPopup(transform);

            menuButton.onClick.AddListener(MenuButton);
            replayButton.onClick.AddListener(ReplayButton);
            TMP_Text reviveLabel = reviveButton.GetComponentInChildren<TMP_Text>(true);
            if (reviveLabel != null)
                reviveLabel.text = "WATCH AD\n+3 MOVES";

            reviveButton.gameObject.SetActive(AdMobConfiguration.AdsEnabled);
            reviveButton.onClick.AddListener(ReviveButton);

            LivesManager.AddIndicator(livesIndicator);
            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
        }

        #region Show/Hide

        public override void PlayShowAnimation()
        {
            AdsManager.DisableBanner();

            interstitialCheckPerformed = false;
            resultInteractionGroup.interactable = true;
            resultInteractionGroup.blocksRaycasts = true;
            menuButton.interactable = false;
            replayButton.interactable = false;
            reviveButton.interactable = false;

            levelFailed.Hide(immediately: true);
            menuButtonScalable.Hide(immediately: true);
            replayButtonScalable.Hide(immediately: true);
            reviveButtonScalable.Hide(immediately: true);

            float fadeDuration = 0.3f;
            backgroundFade.Show(fadeDuration);

            Tween.DelayedCall(fadeDuration * 0.8f, delegate
            {
                levelFailed.Show();

                menuButtonScalable.Show(scaleMultiplier: 1.05f, delay: 0.75f);
                replayButtonScalable.Show(scaleMultiplier: 1.05f, delay: 0.75f);
                if (AdMobConfiguration.AdsEnabled)
                {
                    reviveButtonScalable.Show(scaleMultiplier: 1.05f, delay: 0.25f);
                    continuePingPongCase = reviveButtonScalable.RectTransform.DOPingPongScale(1.0f, 1.05f, 0.9f, Ease.Type.QuadIn, Ease.Type.QuadOut, unscaledTime: true);
                }
                UIController.OnPageOpened(this);
            });

            interstitialCheckTween = Tween.DelayedCall(1.1f, delegate
            {
                interstitialCheckTween = null;
                if (!isPageDisplayed)
                    return;

                menuButton.interactable = true;
                replayButton.interactable = true;
                reviveButton.interactable = AdMobConfiguration.AdsEnabled;
                RequestEligibleInterstitial();
            }, unscaledTime: true);

        }

        public override void PlayHideAnimation()
        {
            backgroundFade.Hide(0.3f);

            if (interstitialCheckTween != null && interstitialCheckTween.IsActive)
                interstitialCheckTween.Kill();
            interstitialCheckTween = null;

            Tween.DelayedCall(0.3f, delegate
            {

                if (continuePingPongCase != null && continuePingPongCase.IsActive)
                    continuePingPongCase.Kill();

                UIController.OnPageClosed(this);
                AdsManager.EnableBanner();
            });
        }

        #endregion

        private void RequestEligibleInterstitial()
        {
            if (interstitialCheckPerformed || !isPageDisplayed)
                return;

            interstitialCheckPerformed = true;
            GameController.TryShowResultInterstitial("Level Failed", resultInteractionGroup);
        }

        #region Buttons 

        private void ReviveButton()
        {
            if (!AdMobConfiguration.AdsEnabled)
                return;

            AudioController.PlaySound(AudioController.Sounds.buttonSound);
            reviveButton.interactable = false;

            AdsManager.ShowRewardBasedVideo(success =>
            {
                reviveButton.interactable = true;
                ReviveCallback(success);
            });
        }

        private void ReviveCallback(bool watchedRV)
        {
            if (!watchedRV) return;

            UIController.HidePage<UIGameOver>();
            UIController.ShowPage<UIGame>();

            GameController.Revive();
        }

        private void ReplayButton()
        {
            AudioController.PlaySound(AudioController.Sounds.buttonSound);


            if (LivesManager.Lives > 0)
            {
                LivesManager.RemoveLife();

                UIController.HidePage<UIGameOver>();
                GameController.ReplayLevel();
            }
            else
            {
                addLivesPanel.Show();
            }
        }

        private void MenuButton()
        {
            AudioController.PlaySound(AudioController.Sounds.buttonSound);

            UIController.HidePage<UIGameOver>(() =>
            {
                GameController.ReturnToMenu();
            });
        }

        #endregion
    }
}
