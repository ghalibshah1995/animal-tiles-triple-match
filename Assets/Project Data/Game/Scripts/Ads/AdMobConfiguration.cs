using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Authoritative Google Mobile Ads identity and environment selector.
    /// Development builds and the Unity Editor can never request production ads.
    /// </summary>
    public static class AdMobConfiguration
    {
        // Ads are intentionally disabled for this build of the game.
        public const bool AdsEnabled = false;

        public const string AndroidAppId = "ca-app-pub-9932752284287575~1663844102";

        public const string ProductionAppOpenId = "ca-app-pub-9932752284287575/8254403261";
        public const string ProductionBannerId = "ca-app-pub-9932752284287575/1912210400";
        public const string ProductionInterstitialId = "ca-app-pub-9932752284287575/6154707744";
        public const string ProductionRewardedId = "ca-app-pub-9932752284287575/6616787883";

        public const string TestAppOpenId = "ca-app-pub-3940256099942544/9257395921";
        public const string TestBannerId = "ca-app-pub-3940256099942544/9214589741";
        public const string TestInterstitialId = "ca-app-pub-3940256099942544/1033173712";
        public const string TestRewardedId = "ca-app-pub-3940256099942544/5224354917";

        public const int InterstitialResultInterval = 3;
        public const float InterstitialCooldownSeconds = 120f;
        public const float AppOpenBackgroundThresholdSeconds = 4f * 60f * 60f;
        public const float AppOpenCacheLifetimeHours = 4f;
        public const float FullScreenQuietPeriodSeconds = 5f;

        public static bool UseTestAds
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }

        public static string AppOpenId => UseTestAds ? TestAppOpenId : ProductionAppOpenId;
        public static string BannerId => UseTestAds ? TestBannerId : ProductionBannerId;
        public static string InterstitialId => UseTestAds ? TestInterstitialId : ProductionInterstitialId;
        public static string RewardedId => UseTestAds ? TestRewardedId : ProductionRewardedId;

        public static void LogEnvironment()
        {
            Debug.Log(!AdsEnabled
                ? "ADMOB_ENVIRONMENT ADS DISABLED"
                : UseTestAds
                ? "ADMOB_ENVIRONMENT TEST ADS enabled (compile-time Development Build)"
                : "ADMOB_ENVIRONMENT PRODUCTION ADS enabled (non-development build)");
        }
    }
}
