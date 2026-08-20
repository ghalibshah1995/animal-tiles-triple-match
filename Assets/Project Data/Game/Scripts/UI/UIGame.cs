using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIGame : UIPage
    {
        [SerializeField] RectTransform safeAreaRectTransform;
        [SerializeField] CurrencyUIPanelSimple coinsPanel;
        [SerializeField] UILevelQuitPopUp quitPopUp;
        [SerializeField] UILevelNumberText levelNumberText;

        [SerializeField] PUUIController powerUpsUIController;
        public PUUIController PowerUpsUIController => powerUpsUIController;

        [SerializeField] UILevelQuitPopUp exitPopUp;
        [SerializeField] Button exitButton;
        [SerializeField] UIFadeAnimation exitButtonFadeAnimation;

        [SerializeField] GameObject devOverlay;

        [LineSpacer("Tutorial")]
        [SerializeField] GameObject tutorialPanelObject;
        [SerializeField] TextMeshProUGUI tutorialTitleText;
        [SerializeField] TextMeshProUGUI tutorialDescriptionText;
        [SerializeField] Button tutorialSkipButton;

        public override void Initialise()
        {
            coinsPanel.Initialise();
            PremiumUIAssets.ApplyHomeButton(exitButton);
            PremiumUIStyler.StyleGameplayHeader(exitButton, coinsPanel);
            exitButton.onClick.AddListener(ShowExitPopUp);
            exitButtonFadeAnimation.Hide(immediately: true);

            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
            NotchSaveArea.RegisterRectTransform((RectTransform)tutorialPanelObject.transform);

            DevPanelEnabler.RegisterPanel(devOverlay);

            tutorialSkipButton.onClick.AddListener(OnTutorialSkipButtonClicked);
        }

        private void OnEnable()
        {
            exitPopUp.OnConfirmExitEvent += ExitPopUpConfirmExitButton;
            exitPopUp.OnCancelExitEvent += ExitPopCloseButton;
        }

        private void OnDisable()
        {
            exitPopUp.OnConfirmExitEvent -= ExitPopUpConfirmExitButton;
            exitPopUp.OnCancelExitEvent -= ExitPopCloseButton;
        }

        private void Update()
        {
            if (exitPopUp != null && exitPopUp.IsOpened)
                return;

            if (Input.GetMouseButtonUp(0) && IsFallbackExitButtonHit(Input.mousePosition))
            {
                Debug.Log("GAME_EXIT_BUTTON_MANUAL_HIT_SAFE_ZONE");
                ShowExitPopUp();
            }
        }

        #region Show/Hide

        public override void PlayShowAnimation()
        {
            // Gameplay (including the first-level tutorial) must never share the
            // screen with a banner. Menu code explicitly restores it when needed.
            AdsManager.DisableBanner();

            coinsPanel.Activate();
            exitButtonFadeAnimation.Show();

            levelNumberText.ForceVisible();

            UIController.OnPageOpened(this);
        }

        public override void PlayHideAnimation()
        {
            coinsPanel.Disable();
            exitButtonFadeAnimation.Hide();

            UILevelNumberText.Hide();

            UIController.OnPageClosed(this);
        }

        public void UpdateLevelNumber(int levelNumber)
        {
            levelNumberText.UpdateLevelNumber(levelNumber);
            levelNumberText.ForceVisible();
        }
        #endregion

        public void ShowExitPopUp()
        {
            exitPopUp.Show();
            AudioController.PlaySound(AudioController.Sounds.buttonSound);
        }

        private bool IsFallbackExitButtonHit(Vector2 screenPosition)
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return false;

            float normalizedX = screenPosition.x / Screen.width;
            float normalizedY = screenPosition.y / Screen.height;

            return normalizedX >= 0.00f && normalizedX <= 0.18f && normalizedY >= 0.86f && normalizedY <= 1.00f;
        }

        public void ExitPopCloseButton()
        {
            exitPopUp.Hide();
        }

        public void ExitPopUpConfirmExitButton()
        {
            if (LivesManager.IsMaxLives)
                LivesManager.RemoveLife();

            UIController.HidePage<UIGame>();

            GameController.ReturnToMenu();

            exitPopUp.Hide();
        }

        #region Tutorial
        public void ActivateTutorial()
        {
            tutorialPanelObject.SetActive(true);

            exitButton.gameObject.SetActive(false);
            levelNumberText.gameObject.SetActive(false);

            powerUpsUIController.HidePanels();
        }

        public void DisableTutorial()
        {
            tutorialPanelObject.SetActive(false);

            exitButton.gameObject.SetActive(true);
            levelNumberText.gameObject.SetActive(true);
        }

        public void SetTutorialText(string title, string description)
        {
            tutorialTitleText.text = title;
            tutorialDescriptionText.text = description;

            tutorialTitleText.transform.localScale = Vector3.one * 0.6f;
            tutorialTitleText.transform.DOScale(1.0f, 0.3f).SetEasing(Ease.Type.BackOut);

            tutorialDescriptionText.transform.localScale = Vector3.one * 0.6f;
            tutorialDescriptionText.transform.DOScale(1.0f, 0.3f).SetEasing(Ease.Type.BackOut);
        }

        private void OnTutorialSkipButtonClicked()
        {
            ITutorial tutorial = TutorialController.GetTutorial(TutorialID.FirstLevel);
            if(tutorial != null)
            {
                FirstLevelTutorial firstLevelTutorial = (FirstLevelTutorial)tutorial;
                firstLevelTutorial.OnSkipButtonClicked();
            }
        }
        #endregion

        #region Development

        public void ReloadDev()
        {
            GameController.ReplayLevel();
        }

        public void HideDev()
        {
            devOverlay.SetActive(false);
        }

        public void OnLevelInputUpdatedDev(string newLevel)
        {
            int level = -1;

            if (int.TryParse(newLevel, out level))
            {
                LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
                levelSave.DisplayLevelIndex = Mathf.Clamp((level - 1), 0, int.MaxValue);
                levelSave.RealLevelIndex = levelSave.DisplayLevelIndex;

                GameController.ReplayLevel();
            }
        }

        public void PrevLevelDev()
        {
            LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
            levelSave.DisplayLevelIndex = Mathf.Clamp(levelSave.DisplayLevelIndex - 1, 0, int.MaxValue);
            levelSave.RealLevelIndex = levelSave.DisplayLevelIndex;

            GameController.ReplayLevel();
        }

        public void NextLevelDev()
        {
            LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
            levelSave.DisplayLevelIndex = levelSave.DisplayLevelIndex + 1;
            levelSave.RealLevelIndex = levelSave.DisplayLevelIndex;

            GameController.ReplayLevel();
        }

        #endregion
    }
}
