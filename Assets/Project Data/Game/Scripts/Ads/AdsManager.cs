using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Watermelon
{
    /// <summary>
    /// Google Mobile Ads bridge. Interstitials are reserved and presented through
    /// a single guarded result-screen transition; rewarded and app-open ads never
    /// share that transition.
    /// </summary>
    public sealed class AdsManager : MonoBehaviour
    {
        private const float InterstitialCountdownSeconds = 3f;
        private const float InterstitialOpenTimeoutSeconds = 8f;
        private const string LastInterstitialUtcTicksPref = "admob_last_interstitial_utc_ticks_v1";
        private const float AppOpenLaunchTimeoutSeconds = 7f;

        private enum InterstitialTransitionState
        {
            Idle,
            LoadingOverlay,
            ShowingInterstitial,
            ClosingInterstitial,
            RestoringUI
        }

        private enum AppOpenLaunchState
        {
            Idle,
            Loading,
            AdLoaded,
            ShowingAd,
            TimedOut,
            LoadFailed,
            ShowFailed,
            OpeningHome
        }

        private static AdsManager instance;
        private static bool sdkReady;
        private static bool sdkInitializationStarted;
        private static bool bannerRequested;

        private BannerView bannerView;
        private InterstitialAd interstitialAd;
        private RewardedAd rewardedAd;
        private AppOpenAd appOpenAd;
        private DateTime appOpenExpiry;
        private bool privacyFlowActive;
        private bool firstUsableScreenReady;

        private bool interstitialLoadInProgress;
        private bool rewardedLoadInProgress;
        private bool appOpenLoadInProgress;
        private int appOpenLoadToken;
        private bool isFullScreenAdShowing;
        private bool interstitialShowRequested;
        private float suppressInterstitialUntil;
        private AppOpenLaunchState appOpenLaunchState = AppOpenLaunchState.Idle;
        private bool launchFlowFinished;
        private bool previousBackButtonLeavesApp;

        private InterstitialTransitionState interstitialState = InterstitialTransitionState.Idle;
        private InterstitialLoadingOverlay loadingOverlay;
        private InterstitialAd transitioningInterstitial;
        private Coroutine countdownCoroutine;
        private Coroutine openTimeoutCoroutine;
        private CanvasGroup blockedResultGroup;
        private bool previousResultInteractable;
        private bool previousResultBlocksRaycasts;
        private Action<bool> transitionFinishedCallback;
        private bool transitioningInterstitialOpened;

        public static bool IsBannerEnabled => bannerRequested;
        public static bool IsInterstitialLoaded => instance != null && instance.interstitialAd != null && instance.interstitialAd.CanShowAd();
        public static bool IsRewardBasedVideoLoaded => instance != null && instance.rewardedAd != null && instance.rewardedAd.CanShowAd();
        public static bool IsAppOpenLoaded => instance != null && instance.appOpenAd != null &&
                                              instance.appOpenAd.CanShowAd() &&
                                              DateTime.UtcNow < instance.appOpenExpiry;
        public static bool IsInterstitialTransitionActive => instance != null && instance.interstitialState != InterstitialTransitionState.Idle;
        public static bool IsFullScreenAdShowing => instance != null && instance.isFullScreenAdShowing;
        public static bool IsPrivacyOptionsRequired =>
            AdMobConfiguration.AdsEnabled &&
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
        public static int BannerReservedHeightPixels
        {
            get
            {
                if (!AdMobConfiguration.AdsEnabled)
                    return 0;

                float density = Screen.dpi > 0f ? Screen.dpi / 160f : 1f;
                return Mathf.CeilToInt(90f * density);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject adsObject = new GameObject("Google Mobile Ads");
            DontDestroyOnLoad(adsObject);
            instance = adsObject.AddComponent<AdsManager>();
            instance.Prepare();
        }

        private void Prepare()
        {
            if (!AdMobConfiguration.AdsEnabled)
            {
                AdMobConfiguration.LogEnvironment();
                return;
            }

            GameObject overlayObject = new GameObject("InterstitialLoadingOverlay");
            overlayObject.transform.SetParent(transform, false);
            loadingOverlay = overlayObject.AddComponent<InterstitialLoadingOverlay>();
            loadingOverlay.Initialise();

            // The SDK's optional exception-reporting transport can attempt to use
            // UnityWebRequest while Android is still starting or shutting down.
            // Ad serving is unaffected by opting out, and this keeps lifecycle
            // transitions free from plugin telemetry exceptions.
            MobileAds.DisableSDKCrashReporting();
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            AdMobConfiguration.LogEnvironment();

            StartCoroutine(BeginPrivacyFlowAfterSceneLoad());
        }

        private IEnumerator BeginPrivacyFlowAfterSceneLoad()
        {
            // Let the first Unity scene create its EventSystem before presenting
            // the neutral age selector.
            yield return null;

            AgeSelectionOverlay.AgeGroup ageGroup = AgeSelectionOverlay.StoredAgeGroup;
            if (ageGroup == AgeSelectionOverlay.AgeGroup.Unknown)
            {
                // Do not interrupt first launch with an age-selection popup.
                // Keep the privacy/consent flow active using the adult treatment
                // as the neutral default for this development build.
                ageGroup = AgeSelectionOverlay.AgeGroup.Adult18Plus;
                PlayerPrefs.SetInt("privacy_age_group_v1", (int)ageGroup);
                PlayerPrefs.Save();
                Debug.Log("ADMOB_PRIVACY age selection skipped; default=Adult18Plus");
            }

            BeginConsentFlow(ageGroup);
        }

        private void BeginConsentFlow(AgeSelectionOverlay.AgeGroup ageGroup)
        {
            ApplyAgeTreatment(ageGroup);
            privacyFlowActive = true;

            ConsentRequestParameters parameters = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = ageGroup == AgeSelectionOverlay.AgeGroup.Teen13To15
            };

            Debug.Log("ADMOB_PRIVACY UMP update requested");
            ConsentInformation.Update(parameters, updateError =>
            {
                if (updateError != null)
                {
                    privacyFlowActive = false;
                    Debug.LogWarning("ADMOB_PRIVACY UMP update failed safely: " + updateError.Message);
                    CompletePrivacyGate();
                    return;
                }

                Debug.Log("ADMOB_PRIVACY UMP update completed status=" + ConsentInformation.ConsentStatus);
                ConsentForm.LoadAndShowConsentFormIfRequired(showError =>
                {
                    privacyFlowActive = false;
                    if (showError != null)
                        Debug.LogWarning("ADMOB_PRIVACY consent form failed safely: " + showError.Message);
                    else
                        Debug.Log("ADMOB_PRIVACY consent form flow completed");

                    CompletePrivacyGate();
                });
            });
        }

        private static void ApplyAgeTreatment(AgeSelectionOverlay.AgeGroup ageGroup)
        {
            bool youngerTeen = ageGroup == AgeSelectionOverlay.AgeGroup.Teen13To15;
            bool anyTeen = youngerTeen || ageGroup == AgeSelectionOverlay.AgeGroup.Teen16To17;

            RequestConfiguration configuration = new RequestConfiguration
            {
                AgeRestrictedTreatment = anyTeen
                    ? AgeRestrictedTreatment.Teen
                    : AgeRestrictedTreatment.Unspecified,
                MaxAdContentRating = anyTeen ? MaxAdContentRating.PG : MaxAdContentRating.T,
                TestDeviceIds = AdMobConfiguration.UseTestAds
                    ? new List<string> { AdRequest.TestDeviceSimulator }
                    : new List<string>()
            };

            MobileAds.SetRequestConfiguration(configuration);
            Debug.Log("ADMOB_PRIVACY age treatment applied group=" + ageGroup);
        }

        private void CompletePrivacyGate()
        {
            if (!AdMobConfiguration.AdsEnabled)
            {
                NotifyFirstUsableScreen();
                return;
            }

            if (!ConsentInformation.CanRequestAds())
            {
                Debug.LogWarning("ADMOB_PRIVACY ads remain disabled because CanRequestAds=false");
                return;
            }

            InitialiseSdkOnce();
        }

        private void InitialiseSdkOnce()
        {
            if (!AdMobConfiguration.AdsEnabled)
                return;

            if (sdkReady || sdkInitializationStarted)
                return;

            sdkInitializationStarted = true;
            MobileAds.Initialize(_ =>
            {
                sdkReady = true;
                Debug.Log("ADMOB_INITIALIZED_ONCE environment=" +
                          (AdMobConfiguration.UseTestAds ? "TEST" : "PRODUCTION"));

                RequestInterstitial();
                RequestRewardBasedVideo();
                RequestAppOpen();

                if (bannerRequested)
                    CreateBanner();

            });
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && interstitialState == InterstitialTransitionState.LoadingOverlay)
                CancelInterstitialTransition("application backgrounded during countdown");
        }

        private void Update()
        {
            if (appOpenLaunchState == AppOpenLaunchState.Loading ||
                appOpenLaunchState == AppOpenLaunchState.AdLoaded ||
                appOpenLaunchState == AppOpenLaunchState.ShowingAd)
            {
                // Keep Android Back from exiting while the launch gate owns the screen.
                Input.backButtonLeavesApp = false;
                if (Input.GetKeyDown(KeyCode.Escape))
                    Debug.Log("ADMOB_APP_OPEN back button ignored during launch flow");
            }
        }

        public static void EnableBanner()
        {
            if (!AdMobConfiguration.AdsEnabled)
            {
                bannerRequested = false;
                NotchSaveArea.Refresh(true);
                return;
            }

            bannerRequested = true;
            if (sdkReady && instance != null && !instance.ShouldSuppressBanner())
                instance.CreateBanner();

            NotchSaveArea.Refresh(true);
        }

        public static void DisableBanner()
        {
            bannerRequested = false;
            if (instance != null)
                instance.DestroyBannerView();

            NotchSaveArea.Refresh(true);
        }

        private bool ShouldSuppressBanner()
        {
            return !launchFlowFinished || isFullScreenAdShowing ||
                   interstitialState != InterstitialTransitionState.Idle;
        }

        private bool IsAppOpenLaunchActive =>
            appOpenLaunchState != AppOpenLaunchState.Idle && !launchFlowFinished;

        private void CreateBanner()
        {
            if (bannerView != null || !bannerRequested || ShouldSuppressBanner())
                return;

            int safeWidth = MobileAds.Utils.GetDeviceSafeWidth();
            AdSize adaptiveSize =
                AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(safeWidth);
            bannerView = new BannerView(AdMobConfiguration.BannerId, adaptiveSize, AdPosition.Bottom);
            bannerView.OnBannerAdLoaded += () =>
            {
                Debug.Log("ADMOB_BANNER_LOADED environment=" +
                          (AdMobConfiguration.UseTestAds ? "TEST" : "PRODUCTION"));
                NotchSaveArea.Refresh(true);
            };
            bannerView.OnBannerAdLoadFailed += error =>
            {
                Debug.LogWarning("ADMOB_BANNER_FAILED: " + error);
                DestroyBannerView();
            };
            bannerView.LoadAd(new AdRequest());
        }

        private void DestroyBannerView()
        {
            if (bannerView == null)
                return;

            bannerView.Destroy();
            bannerView = null;
        }

        private void RestoreBannerIfRequested()
        {
            if (bannerRequested && sdkReady && !ShouldSuppressBanner())
                CreateBanner();

            NotchSaveArea.Refresh(true);
        }

        public static void RequestInterstitial()
        {
            if (!AdMobConfiguration.AdsEnabled)
                return;

            if (!sdkReady || instance == null || IsInterstitialLoaded || instance.interstitialLoadInProgress ||
                instance.transitioningInterstitial != null)
                return;

            instance.interstitialLoadInProgress = true;
            instance.interstitialAd?.Destroy();
            instance.interstitialAd = null;
            DevLog("next Interstitial preload started");

            InterstitialAd.Load(AdMobConfiguration.InterstitialId, new AdRequest(), (ad, error) =>
            {
                if (instance == null)
                {
                    ad?.Destroy();
                    return;
                }

                instance.interstitialLoadInProgress = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning("ADMOB_INTERSTITIAL_FAILED: " + error);
                    return;
                }

                instance.interstitialAd = ad;
                Debug.Log("ADMOB_INTERSTITIAL_LOADED environment=" +
                          (AdMobConfiguration.UseTestAds ? "TEST" : "PRODUCTION"));
                DevLog("Interstitial loaded state: ready");
            });
        }

        public static bool TryShowInterstitialTransition(string placement, CanvasGroup resultGroup,
            Action<bool> callback = null)
        {
            if (!AdMobConfiguration.AdsEnabled)
            {
                callback?.Invoke(false);
                return false;
            }

            DevLog("Interstitial eligibility checked: " + placement);

            if (instance == null)
            {
                callback?.Invoke(false);
                return false;
            }

            if (!instance.CanStartInterstitialTransition(out string reason))
            {
                DevLog("Interstitial not eligible: " + placement + " - " + reason);
                DevLog("Interstitial loaded state: " + IsInterstitialLoaded);
                RequestInterstitial();
                callback?.Invoke(false);
                return false;
            }

            DevLog("Interstitial eligible: " + placement);
            DevLog("Interstitial ready");
            instance.BeginInterstitialTransition(resultGroup, callback);
            return true;
        }

        public static void LogInterstitialNotEligible(string placement, string reason)
        {
            DevLog("Interstitial not eligible: " + placement + " - " + reason);
        }

        // Kept for compatibility; all current callers should use the guarded
        // result-screen API above so no navigation button can trigger an ad.
        public static void ShowInterstitial(Action<bool> callback)
        {
            TryShowInterstitialTransition("legacy result placement", null, callback);
        }

        private bool CanStartInterstitialTransition(out string reason)
        {
            if (IsAppOpenLaunchActive)
            {
                reason = "App Open launch flow is active";
                return false;
            }

            if (!sdkReady)
            {
                reason = "SDK not initialized";
                return false;
            }

            if (interstitialState != InterstitialTransitionState.Idle || interstitialShowRequested)
            {
                reason = "another interstitial transition is active";
                return false;
            }

            if (isFullScreenAdShowing)
            {
                reason = "another full-screen ad is active";
                return false;
            }

            if (Time.realtimeSinceStartup < suppressInterstitialUntil)
            {
                reason = "full-screen ad cooldown is active";
                return false;
            }

            long lastShownTicks;
            if (long.TryParse(PlayerPrefs.GetString(LastInterstitialUtcTicksPref, "0"), out lastShownTicks) &&
                lastShownTicks > 0)
            {
                DateTime lastShown = new DateTime(lastShownTicks, DateTimeKind.Utc);
                if ((DateTime.UtcNow - lastShown).TotalSeconds <
                    AdMobConfiguration.InterstitialCooldownSeconds)
                {
                    reason = "120-second interstitial cooldown is active";
                    return false;
                }
            }

            if (!IsInterstitialLoaded)
            {
                reason = "interstitial is not loaded";
                return false;
            }

            reason = null;
            return true;
        }

        private void BeginInterstitialTransition(CanvasGroup resultGroup, Action<bool> callback)
        {
            interstitialShowRequested = true;
            interstitialState = InterstitialTransitionState.LoadingOverlay;
            transitionFinishedCallback = callback;
            transitioningInterstitialOpened = false;

            transitioningInterstitial = interstitialAd;
            interstitialAd = null;
            transitioningInterstitial.OnAdFullScreenContentOpened += OnTransitioningInterstitialOpened;
            transitioningInterstitial.OnAdFullScreenContentClosed += OnTransitioningInterstitialClosed;
            transitioningInterstitial.OnAdFullScreenContentFailed += OnTransitioningInterstitialFailed;

            BlockResultInteraction(resultGroup);
            DestroyBannerView();
            loadingOverlay.Show(3);
            DevLog("Loading screen opened");

            countdownCoroutine = StartCoroutine(InterstitialCountdown());
        }

        private IEnumerator InterstitialCountdown()
        {
            float startedAt = Time.realtimeSinceStartup;
            int displayedNumber = -1;

            while (interstitialState == InterstitialTransitionState.LoadingOverlay)
            {
                float elapsed = Mathf.Min(Time.realtimeSinceStartup - startedAt,
                    InterstitialCountdownSeconds);
                int number = Mathf.Clamp(3 - Mathf.FloorToInt(elapsed), 1, 3);

                if (number != displayedNumber)
                {
                    displayedNumber = number;
                    loadingOverlay.SetCountdown(number);
                    DevLog("Countdown " + number);
                }

                loadingOverlay.SetProgress(elapsed / InterstitialCountdownSeconds);

                if (elapsed >= InterstitialCountdownSeconds)
                    break;

                yield return null;
            }

            if (interstitialState != InterstitialTransitionState.LoadingOverlay)
                yield break;

            countdownCoroutine = null;
            RequestTransitioningInterstitialShow();
        }

        private void RequestTransitioningInterstitialShow()
        {
            if (interstitialState != InterstitialTransitionState.LoadingOverlay ||
                transitioningInterstitial == null || !transitioningInterstitial.CanShowAd())
            {
                FailInterstitialTransition("reserved interstitial was no longer ready");
                return;
            }

            interstitialState = InterstitialTransitionState.ShowingInterstitial;
            isFullScreenAdShowing = true;
            DevLog("Interstitial show requested");

            try
            {
                transitioningInterstitial.Show();
                openTimeoutCoroutine = StartCoroutine(WaitForInterstitialOpen());
            }
            catch (Exception exception)
            {
                Debug.LogWarning("ADMOB_INTERSTITIAL_SHOW_FAILED: " + exception.Message);
                FailInterstitialTransition("Show threw an exception");
            }
        }

        private IEnumerator WaitForInterstitialOpen()
        {
            float timeoutAt = Time.realtimeSinceStartup + InterstitialOpenTimeoutSeconds;
            while (!transitioningInterstitialOpened &&
                   interstitialState == InterstitialTransitionState.ShowingInterstitial &&
                   Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            openTimeoutCoroutine = null;
            if (!transitioningInterstitialOpened && interstitialState == InterstitialTransitionState.ShowingInterstitial)
                FailInterstitialTransition("ad-opening callback timed out");
        }

        private void OnTransitioningInterstitialOpened()
        {
            if (interstitialState != InterstitialTransitionState.ShowingInterstitial)
                return;

            transitioningInterstitialOpened = true;
            PlayerPrefs.SetString(LastInterstitialUtcTicksPref, DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
            if (openTimeoutCoroutine != null)
            {
                StopCoroutine(openTimeoutCoroutine);
                openTimeoutCoroutine = null;
            }

            DevLog("Interstitial opened");
        }

        private void OnTransitioningInterstitialClosed()
        {
            DevLog("Interstitial closed");
            FinishInterstitialTransition(transitioningInterstitialOpened);
        }

        private void OnTransitioningInterstitialFailed(AdError error)
        {
            Debug.LogWarning("ADMOB_INTERSTITIAL_SHOW_FAILED: " + error);
            DevLog("Interstitial failed to show");
            FailInterstitialTransition("Google Mobile Ads show failure");
        }

        private void CancelInterstitialTransition(string reason)
        {
            if (interstitialState != InterstitialTransitionState.LoadingOverlay)
                return;

            DevLog("Interstitial transition cancelled: " + reason);
            FinishInterstitialTransition(false);
        }

        private void FailInterstitialTransition(string reason)
        {
            DevLog("Interstitial failed to show: " + reason);
            FinishInterstitialTransition(false);
        }

        private void FinishInterstitialTransition(bool shown)
        {
            if (interstitialState == InterstitialTransitionState.Idle ||
                interstitialState == InterstitialTransitionState.RestoringUI)
                return;

            interstitialState = InterstitialTransitionState.ClosingInterstitial;

            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }

            if (openTimeoutCoroutine != null)
            {
                StopCoroutine(openTimeoutCoroutine);
                openTimeoutCoroutine = null;
            }

            interstitialState = InterstitialTransitionState.RestoringUI;
            loadingOverlay.Hide();
            DevLog("Loading screen closed");
            RestoreResultInteraction();

            if (transitioningInterstitial != null)
            {
                transitioningInterstitial.Destroy();
                transitioningInterstitial = null;
            }

            isFullScreenAdShowing = false;
            interstitialShowRequested = false;
            transitioningInterstitialOpened = false;
            suppressInterstitialUntil = Time.realtimeSinceStartup +
                                         AdMobConfiguration.FullScreenQuietPeriodSeconds;

            Action<bool> callback = transitionFinishedCallback;
            transitionFinishedCallback = null;
            interstitialState = InterstitialTransitionState.Idle;

            RestoreBannerIfRequested();
            DevLog("UI interaction restored");
            callback?.Invoke(shown);
            RequestInterstitial();
        }

        private void BlockResultInteraction(CanvasGroup resultGroup)
        {
            blockedResultGroup = resultGroup;
            if (blockedResultGroup == null)
                return;

            previousResultInteractable = blockedResultGroup.interactable;
            previousResultBlocksRaycasts = blockedResultGroup.blocksRaycasts;
            blockedResultGroup.interactable = false;
            blockedResultGroup.blocksRaycasts = false;
        }

        private void RestoreResultInteraction()
        {
            if (blockedResultGroup == null)
                return;

            blockedResultGroup.interactable = previousResultInteractable;
            blockedResultGroup.blocksRaycasts = previousResultBlocksRaycasts;
            blockedResultGroup = null;
        }

        public static void RequestRewardBasedVideo()
        {
            if (!AdMobConfiguration.AdsEnabled)
                return;

            if (!sdkReady || instance == null || IsRewardBasedVideoLoaded || instance.rewardedLoadInProgress)
                return;

            instance.rewardedLoadInProgress = true;
            instance.rewardedAd?.Destroy();
            instance.rewardedAd = null;

            RewardedAd.Load(AdMobConfiguration.RewardedId, new AdRequest(), (ad, error) =>
            {
                if (instance == null)
                {
                    ad?.Destroy();
                    return;
                }

                instance.rewardedLoadInProgress = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning("ADMOB_REWARDED_FAILED: " + error);
                    return;
                }

                instance.rewardedAd = ad;
                Debug.Log("ADMOB_REWARDED_LOADED environment=" +
                          (AdMobConfiguration.UseTestAds ? "TEST" : "PRODUCTION"));
            });
        }

        public static void ShowRewardBasedVideo(Action<bool> callback, bool showErrorMessage = true)
        {
            if (!AdMobConfiguration.AdsEnabled)
            {
                callback?.Invoke(false);
                return;
            }

            if (instance == null || IsInterstitialTransitionActive || instance.isFullScreenAdShowing ||
                instance.IsAppOpenLaunchActive)
            {
                callback?.Invoke(false);
                return;
            }

            if (!IsRewardBasedVideoLoaded)
            {
                RequestRewardBasedVideo();
                callback?.Invoke(false);
                return;
            }

            RewardedAd ad = instance.rewardedAd;
            instance.rewardedAd = null;
            instance.isFullScreenAdShowing = true;
            instance.DestroyBannerView();
            bool rewardEarned = false;
            bool callbackDelivered = false;
            bool finishDelivered = false;

            Action finish = () =>
            {
                if (finishDelivered)
                    return;

                finishDelivered = true;
                instance.isFullScreenAdShowing = false;
                instance.suppressInterstitialUntil = Time.realtimeSinceStartup +
                                                      AdMobConfiguration.FullScreenQuietPeriodSeconds;
                instance.RestoreBannerIfRequested();
                ad.Destroy();
                RequestRewardBasedVideo();
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                if (!rewardEarned && !callbackDelivered)
                {
                    callbackDelivered = true;
                    callback?.Invoke(false);
                }
                finish();
            };
            ad.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogWarning("ADMOB_REWARDED_SHOW_FAILED: " + error);
                if (!rewardEarned && !callbackDelivered)
                {
                    callbackDelivered = true;
                    callback?.Invoke(false);
                }
                finish();
            };

            Debug.Log("ADMOB_REWARDED_SHOW environment=" +
                      (AdMobConfiguration.UseTestAds ? "TEST" : "PRODUCTION"));
            try
            {
                ad.Show(_ =>
                {
                    rewardEarned = true;
                    if (!callbackDelivered)
                    {
                        callbackDelivered = true;
                        callback?.Invoke(true);
                    }
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning("ADMOB_REWARDED_SHOW_FAILED: " + exception.Message);
                if (!callbackDelivered)
                {
                    callbackDelivered = true;
                    callback?.Invoke(false);
                }
                finish();
            }
        }

        public static void RequestAppOpen()
        {
            if (!AdMobConfiguration.AdsEnabled)
                return;

            if (!sdkReady || instance == null || IsAppOpenLoaded || instance.appOpenLoadInProgress ||
                instance.launchFlowFinished)
                return;

            instance.appOpenLoadInProgress = true;
            int loadToken = ++instance.appOpenLoadToken;
            instance.appOpenAd?.Destroy();
            instance.appOpenAd = null;

            AppOpenAd.Load(AdMobConfiguration.AppOpenId, new AdRequest(), (ad, error) =>
            {
                if (instance == null)
                {
                    ad?.Destroy();
                    return;
                }

                instance.appOpenLoadInProgress = false;
                if (loadToken != instance.appOpenLoadToken || instance.launchFlowFinished ||
                    instance.appOpenLaunchState == AppOpenLaunchState.TimedOut ||
                    instance.appOpenLaunchState == AppOpenLaunchState.LoadFailed ||
                    instance.appOpenLaunchState == AppOpenLaunchState.ShowFailed ||
                    instance.appOpenLaunchState == AppOpenLaunchState.OpeningHome)
                {
                    ad?.Destroy();
                    Debug.Log("ADMOB_APP_OPEN late load ignored after launch fallback");
                    return;
                }

                if (error != null || ad == null)
                {
                    Debug.LogWarning("ADMOB_APP_OPEN_FAILED: " + error);
                    if (instance.appOpenLaunchState == AppOpenLaunchState.Loading)
                        instance.FailAppOpenLaunch(AppOpenLaunchState.LoadFailed, "load failed");
                    return;
                }

                instance.appOpenAd = ad;
                instance.appOpenExpiry = DateTime.UtcNow.AddHours(AdMobConfiguration.AppOpenCacheLifetimeHours);
                Debug.Log("ADMOB_APP_OPEN_LOADED environment=" +
                          (AdMobConfiguration.UseTestAds ? "TEST" : "PRODUCTION"));
                if (instance.appOpenLaunchState == AppOpenLaunchState.Loading)
                    instance.OnLaunchAppOpenLoaded();
            });
        }

        private void OnLaunchAppOpenLoaded()
        {
            if (appOpenLaunchState != AppOpenLaunchState.Loading)
                return;

            if (!IsAppOpenLoaded)
            {
                FailAppOpenLaunch(AppOpenLaunchState.LoadFailed, "loaded ad was no longer showable");
                return;
            }

            appOpenLaunchState = AppOpenLaunchState.AdLoaded;
            Debug.Log("ADMOB_APP_OPEN launch state=AdLoaded");
            ShowLaunchAppOpen();
        }

        private void ShowLaunchAppOpen()
        {
            if (appOpenLaunchState != AppOpenLaunchState.AdLoaded || !IsAppOpenLoaded)
            {
                FailAppOpenLaunch(AppOpenLaunchState.ShowFailed, "loaded ad was unavailable at show time");
                return;
            }

            AppOpenAd ad = instance.appOpenAd;
            appOpenAd = null;
            appOpenLaunchState = AppOpenLaunchState.ShowingAd;
            isFullScreenAdShowing = true;
            DestroyBannerView();
            bool completed = false;

            Action<bool> finish = success =>
            {
                if (completed)
                    return;

                completed = true;
                isFullScreenAdShowing = false;
                ad.Destroy();
                if (success)
                    OpenHomeOnce("ad closed");
                else
                    FailAppOpenLaunch(AppOpenLaunchState.ShowFailed, "ad failed to show");
            };

            ad.OnAdFullScreenContentClosed += () => finish(true);
            ad.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogWarning("ADMOB_APP_OPEN_SHOW_FAILED: " + error);
                finish(false);
            };

            Debug.Log("ADMOB_APP_OPEN_SHOW environment=" +
                      (AdMobConfiguration.UseTestAds ? "TEST" : "PRODUCTION"));
            try
            {
                ad.Show();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("ADMOB_APP_OPEN_SHOW_FAILED: " + exception.Message);
                finish(false);
            }
        }

        /// <summary>
        /// Called after the first scene has prepared the existing loading screen
        /// and the home/map UI. The loading screen remains visible until this
        /// launch-only App Open flow closes or falls back.
        /// </summary>
        public static void NotifyFirstUsableScreen()
        {
            if (instance == null)
                return;

            if (!AdMobConfiguration.AdsEnabled)
            {
                GameLoading.MarkAsReadyToHide();
                return;
            }

            instance.firstUsableScreenReady = true;
            instance.BeginAppOpenLaunch();
        }

        private void BeginAppOpenLaunch()
        {
            if (appOpenLaunchState != AppOpenLaunchState.Idle || !firstUsableScreenReady)
                return;

            appOpenLaunchState = AppOpenLaunchState.Loading;
            launchFlowFinished = false;
            previousBackButtonLeavesApp = Input.backButtonLeavesApp;
            Input.backButtonLeavesApp = false;
            DestroyBannerView();
            Debug.Log("ADMOB_APP_OPEN launch state=Loading timeoutSeconds=" + AppOpenLaunchTimeoutSeconds);

            if (sdkReady)
                RequestAppOpen();

            StartCoroutine(WaitForLaunchAppOpen());
        }

        private IEnumerator WaitForLaunchAppOpen()
        {
            float timeoutAt = Time.realtimeSinceStartup + AppOpenLaunchTimeoutSeconds;
            while (appOpenLaunchState == AppOpenLaunchState.Loading &&
                   Time.realtimeSinceStartup < timeoutAt)
            {
                if (sdkReady && !appOpenLoadInProgress && !IsAppOpenLoaded)
                    RequestAppOpen();

                if (IsAppOpenLoaded)
                {
                    OnLaunchAppOpenLoaded();
                    yield break;
                }

                yield return null;
            }

            if (appOpenLaunchState == AppOpenLaunchState.Loading)
                FailAppOpenLaunch(AppOpenLaunchState.TimedOut, "7-second timeout");
        }

        private void FailAppOpenLaunch(AppOpenLaunchState failureState, string reason)
        {
            if (launchFlowFinished || appOpenLaunchState == AppOpenLaunchState.OpeningHome)
                return;

            appOpenLaunchState = failureState;
            appOpenLoadToken++;
            appOpenLoadInProgress = false;
            appOpenAd?.Destroy();
            appOpenAd = null;
            Debug.LogWarning("ADMOB_APP_OPEN launch state=" + failureState + " reason=" + reason);
            OpenHomeOnce(reason);
        }

        private void OpenHomeOnce(string reason)
        {
            if (launchFlowFinished || appOpenLaunchState == AppOpenLaunchState.OpeningHome)
                return;

            appOpenLaunchState = AppOpenLaunchState.OpeningHome;
            launchFlowFinished = true;
            Input.backButtonLeavesApp = previousBackButtonLeavesApp;
            Debug.Log("ADMOB_APP_OPEN launch state=OpeningHome reason=" + reason);

            // GameController has already prepared the Home screen behind the
            // loading canvas. Releasing this gate is the single home transition.
            GameLoading.MarkAsReadyToHide();
            RestoreBannerIfRequested();
        }

        public static void ShowPrivacyOptionsOrPolicy(string privacyPolicyUrl)
        {
            if (!AdMobConfiguration.AdsEnabled)
                return;

            if (instance != null && IsPrivacyOptionsRequired)
            {
                instance.privacyFlowActive = true;
                ConsentForm.ShowPrivacyOptionsForm(error =>
                {
                    if (instance != null)
                        instance.privacyFlowActive = false;

                    if (error != null)
                        Debug.LogWarning("ADMOB_PRIVACY privacy options failed safely: " + error.Message);
                    else
                        Debug.Log("ADMOB_PRIVACY privacy options closed");
                });
                return;
            }

            if (!string.IsNullOrWhiteSpace(privacyPolicyUrl))
                Application.OpenURL(privacyPolicyUrl);
            else
                Debug.LogWarning("ADMOB_PRIVACY privacy policy URL is not configured yet.");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void DevLog(string message)
        {
            Debug.Log("INTERSTITIAL_TRANSITION: " + message);
        }
    }
}
