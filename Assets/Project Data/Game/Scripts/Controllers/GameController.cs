using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.Map;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Watermelon
{
    public class GameController : MonoBehaviour
    {
        private const string ResultsSinceAdPref = "results_since_interstitial_v2";

        private static GameController gameController;

        [DrawReference]
        [SerializeField] GameData data;

        [LineSpacer]
        [SerializeField] UIController uiController;

        private LevelController levelController;
        private ParticlesController particlesController;
        private FloatingTextController floatingTextController;
        private CurrenciesController currenciesController;
        private PUController powerUpController;
        private MapBehavior mapBehavior;
        private TutorialController tutorialController;
        private Coroutine resumeRecoveryCoroutine;

        public static GameData Data => gameController.data;

        private static bool isGameActive;
        public static bool IsGameActive => isGameActive;

        private void Awake()
        {
            gameController = this;

            // Matching levels are active-play sessions; prevent the display from sleeping mid-level.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            SaveController.Initialise(useAutoSave: false);

            // Cache components
            CacheComponent(out particlesController);
            CacheComponent(out floatingTextController);
            CacheComponent(out currenciesController);
            CacheComponent(out levelController);
            CacheComponent(out powerUpController); 
            CacheComponent(out mapBehavior);
            CacheComponent(out tutorialController);
        }

        private void Start()
        {
            InitialiseGame();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveController.Save();
            }
            else
            {
                QueueResumeRecovery();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveController.Save();
            }
            else
            {
                QueueResumeRecovery();
            }
        }

        public void InitialiseGame()
        {
            uiController.Initialise();

            particlesController.Initialise();
            floatingTextController.Inititalise();
            currenciesController.Initialise();

            powerUpController.Initialise();
            levelController.Initialise();
            tutorialController.Initialise();

            uiController.InitialisePages();

            ITutorial tutorial = TutorialController.GetTutorial(TutorialID.FirstLevel);
            if(data.ShowTutorial && !tutorial.IsFinished)
            {
                // Start first level tutorial
                tutorial.StartTutorial();
            }
            else
            {
                AdsManager.EnableBanner();
                mapBehavior.Show();

                // Display default page
                UIController.ShowPage<UIMainMenu>();

#if UNITY_EDITOR
                CheckIfNeedToAutoRunLevel();
#endif
            }

            // The launch App Open flow owns the existing loading screen and
            // releases it after the ad closes or its 7-second fallback.
            AdsManager.NotifyFirstUsableScreen();
        }

        private void QueueResumeRecovery()
        {
            if (!Initialiser.IsStartInitialized)
                return;

            if (resumeRecoveryCoroutine != null)
                StopCoroutine(resumeRecoveryCoroutine);

            resumeRecoveryCoroutine = StartCoroutine(ResumeRecoveryCoroutine());
        }

        private IEnumerator ResumeRecoveryCoroutine()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            Time.timeScale = 1f;

            if (isGameActive)
            {
                UIController.ShowPage<UIGame>();
                LevelController.RecoverActiveLevelAfterResume();
            }
            else
            {
                RecoverMenuMapAfterResume();
            }

            resumeRecoveryCoroutine = null;
        }

        private void RecoverMenuMapAfterResume()
        {
            UIMainMenu mainMenuPage = UIController.GetPage<UIMainMenu>();
            UIGame gamePage = UIController.GetPage<UIGame>();

            if (gamePage != null && gamePage.IsPageDisplayed)
            {
                isGameActive = true;
                LevelController.RecoverActiveLevelAfterResume();
                return;
            }

            if (mainMenuPage != null && mainMenuPage.IsPageDisplayed)
            {
                mapBehavior.Show();
            }
        }

        public static void LoadLevel(int index, SimpleCallback onLevelLoaded = null)
        {
            gameController.mapBehavior.Hide();

            UIController.HidePage<UIMainMenu>(() =>
            {
                UIController.ShowPage<UIGame>();

                gameController.levelController.LoadLevel(index, onLevelLoaded);

                isGameActive = true;
            });
        }

        public static void LoadCustomLevel(LevelData levelData, PreloadedLevelData preloadedLevelData, BackgroundData backgroundData, bool animateDock, SimpleCallback onLevelLoaded = null)
        {
            UIController.ShowPage<UIGame>();

            gameController.levelController.LoadCustomLevel(levelData, preloadedLevelData, backgroundData, animateDock, onLevelLoaded);

            isGameActive = true;
        }

        public static void OnLevelCompleted()
        {
            if (!isGameActive)
                return;

            UIController.HidePage<UIGame>(() =>
            {
                RecordResultForInterstitial();

                // The result must be visible and finish its entrance animation
                // before the page asks for an eligible interstitial transition.
                UIController.ShowPage<UIComplete>();
            });

            isGameActive = false;
        }

        public static void TryShowResultInterstitial(string placement, CanvasGroup resultInteractionGroup)
        {
            int resultsSinceAd = PlayerPrefs.GetInt(ResultsSinceAdPref, 0);
            if (resultsSinceAd < AdMobConfiguration.InterstitialResultInterval)
            {
                AdsManager.LogInterstitialNotEligible(placement,
                    $"frequency {resultsSinceAd}/{AdMobConfiguration.InterstitialResultInterval}");
                return;
            }

            bool transitionStarted = AdsManager.TryShowInterstitialTransition(placement,
                resultInteractionGroup);
            if (!transitionStarted)
                return;

            // Consume frequency only after a loaded ad has been reserved and the
            // guarded transition really starts. A not-ready ad leaves this pending
            // for the next valid result break (complete or failed).
            PlayerPrefs.SetInt(ResultsSinceAdPref, 0);
            PlayerPrefs.Save();
        }

        public static void OnLevelFailed()
        {
            if (!isGameActive)
                return;

            UIController.HidePage<UIGame>(() =>
            {
                RecordResultForInterstitial();
                UIController.ShowPage<UIGameOver>();
            });

            isGameActive = false;
        }

        private static void RecordResultForInterstitial()
        {
            int resultCount = PlayerPrefs.GetInt(ResultsSinceAdPref, 0) + 1;
            PlayerPrefs.SetInt(ResultsSinceAdPref, resultCount);
            PlayerPrefs.Save();
        }

        public static void LoadNextLevel(SimpleCallback onLevelLoaded = null)
        {
            LoadLevel(LevelController.DisplayedLevelIndex, onLevelLoaded);
        }

        public static void ReplayLevel()
        {
            isGameActive = false;

            UIController.ShowPage<UIMainMenu>();

            LoadLevel(LevelController.DisplayedLevelIndex);
        }

        public static void ReturnToMenu()
        {
            isGameActive = false;

            LevelController.UnloadLevel();

            gameController.mapBehavior.Show();

            AdsManager.EnableBanner();

            UIController.ShowPage<UIMainMenu>();
            AdsManager.NotifyFirstUsableScreen();
        }

        public static void Revive()
        {
            isGameActive = true;

            LevelController.ReturnTiles(3, null);
        }

        #region Extensions
        public bool CacheComponent<T>(out T component) where T : Component
        {
            Component unboxedComponent = gameObject.GetComponent(typeof(T));

            if (unboxedComponent != null)
            {
                component = (T)unboxedComponent;

                return true;
            }

            Debug.LogError(string.Format("Scripts Holder doesn't have {0} script added to it", typeof(T)));

            component = null;

            return false;
        }
        #endregion

        #region Dev

#if UNITY_EDITOR

        private static readonly string AUTO_RUN_LEVEL_SAVE_NAME = "auto run level editor";

        public static bool AutoRunLevelInEditor
        {
            get { return EditorPrefs.GetBool(AUTO_RUN_LEVEL_SAVE_NAME, false); }
            set { EditorPrefs.SetBool(AUTO_RUN_LEVEL_SAVE_NAME, value); }
        }

        private void CheckIfNeedToAutoRunLevel()
        {
            if (AutoRunLevelInEditor)
                LoadLevel(LevelController.DisplayedLevelIndex);

            AutoRunLevelInEditor = false;
        }
#endif


        #endregion
    }
}
