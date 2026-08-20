# Animal Tiles: Triple Match — Project History

Last updated: 2026-07-27

## Current project identity

- Product name: **Animal Tiles: Triple Match**
- Unity editor: **6000.5.0f1**
- Android application ID: `com.rationalstudio.animaltiles.triplematch`
- Version: `1.0.0` (`versionCode` `56`)
- Android: minimum SDK 26, target/compile SDK 36, ARM64, IL2CPP
- Enabled scenes: `Init.unity` and `Game.unity`
- GitHub repository: `ghalibshah1995/matching-game-unity`

This document consolidates the implementation and QA history. Git commit history
remains the authoritative record of individual file changes.

## 1. Cleanup and Unity modernization — 2026-07-15

- Upgraded the project from Unity 2021.3.42f1 to Unity 6000.5.0f1.
- Updated URP and required Unity packages for the Unity 6 editor line.
- Removed unused examples, template content, obsolete editor helpers, stale SDK
  adapters, and unreferenced GUI-pack content.
- Replaced deprecated Unity/TMP/editor APIs and resolved Unity 6 serialization
  and compilation warnings.
- Preserved gameplay, level data, progression, controls, save behavior, economy,
  boosters, animation, and the original two-scene navigation flow.
- Configured Android IL2CPP/ARM64 builds, current Gradle/JDK/NDK integration,
  AndroidX, cleartext restrictions, and safe release defaults.
- Removed legacy signing details, previous SDK identifiers, and obsolete project
  configuration from the initial source.

## 2. Full visual reskin — 2026-07-15 to 2026-07-18

- Introduced the premium navy, cyan, violet, mint, coral, and gold UI theme.
- Reskinned main menu, level selection, gameplay HUD, settings, store, lives,
  currency, popups, result screens, navigation, and shared buttons.
- Reworked all matching animal tiles, obstacle states, collection slots, and
  booster icons while keeping their serialized GUID references intact.
- Retained existing button events, scene/prefab names, save keys, IDs, controls,
  and gameplay balance.
- Rebuilt four level-map environments and aligned their paths and waypoint pads
  to the existing 40 serialized level buttons.

## 3. Seamless level-map world — 2026-07-17 to 2026-07-20

- Corrected map aspect fitting and removed stretched/cropped background output.
- Replaced abrupt forest/desert/snow/twilight joins with a continuous vertical
  environment and feathered biome transitions.
- Preserved the road and waypoint masks while repairing connector geometry at
  10-level chunk boundaries.
- Added a cyclic transition so the recycled fourth chunk joins the first chunk
  without a visible road strip or background seam.
- Verified the level map at 720x1280, 1080x1920, 1080x2160, 1080x2400, and
  1440x3200 layouts.

## 4. Gameplay state and popup repairs — 2026-07-18 to 2026-07-20

- Persisted the selected/current level safely across minimize, resume, process
  termination, and relaunch flows.
- Added level-index validation to prevent background-only/blank gameplay states.
- Fixed duplicate quit-popup subscriptions and modal sorting/input ownership.
- Added one-shot popup close behavior and guarded booster purchase taps.
- Persisted life, booster, and progression changes where they are earned, spent,
  or changed.
- Repaired gameplay level-label visibility and top HUD layout, including the
  lives timer and coin counter.

## 5. Premium result and reward UI — 2026-07-25 to 2026-07-26

- Rebuilt Level Failed, Level Completed, Quit Level, Get More Lives, and Need a
  Booster presentations to match the approved premium references.
- Corrected button art, content spacing, coin/reward centering, safe-area layout,
  and popup raycast behavior.
- Preserved replay, next, home, quit, purchase, rewarded, and close callbacks.

## 6. Android release identity, ads, and privacy — 2026-07-26

- Applied the final title, package identity, store icon, and Android API 36 setup.
- Integrated Google Mobile Ads for App Open, Banner, Interstitial, and Rewarded
  placements using official Google test ad units in development builds.
- Added guarded natural-break Interstitial frequency/cooldown handling.
- Added rewarded outcomes for lives, boosters, fail recovery moves, and doubled
  level-complete coins without changing the base reward rules.
- Added a first-run age-bracket selection and age-aware request configuration.
- Added UMP consent flow handling and safe failure behavior. A production UMP
  message must still be configured in the owner's AdMob account.
- Ensured full-screen ad state prevents App Open, Interstitial, and Rewarded
  formats from opening over one another.

## 7. Premium Interstitial loading transition — 2026-07-27

- Added a separate, runtime-built Interstitial loading Canvas with a full-screen
  blocker, dark dim layer, glossy navy/gold panel, ribbon, dynamic text, sparkles,
  and a radial progress ring.
- Added an exact unscaled-time `3 → 2 → 1` countdown. The Interstitial is already
  loaded and reserved before the countdown begins and is shown immediately after
  the three visible seconds.
- Disabled underlying result controls and Banner interaction during the transition.
- Added duplicate-request guards, background cancellation, open/failure callbacks,
  a presentation timeout, UI restoration, ad disposal, and next-ad preload.
- Kept App Open waiting UI separate; the Interstitial countdown is never used for
  App Open, Rewarded, or Banner ads.
- Development-only logs cover eligibility, readiness, countdown, show/open,
  failure, close, input restoration, and preload events.

## Validation performed

- Unity scans repeatedly reported two enabled scenes and no missing scripts.
- Play-mode smoke tests reached the Game scene without gameplay exceptions.
- Android development APK builds completed with IL2CPP and ARM64.
- The v56 Development APK completed with `errors=0`; package inspection confirmed
  versionCode 56, min SDK 26, target SDK 36, and `arm64-v8a`.
- A connected Xiaomi Android device successfully initialized Google Mobile Ads
  and loaded official test App Open, Banner, Interstitial, and Rewarded ads.
- Resume/relaunch, popup input, level selection, result layouts, and core ad state
  logs were checked during iterative device QA.

## Repository hygiene

The Git repository intentionally contains source-controlled Unity inputs only:

- `Assets` (including every required `.meta` file)
- `Packages`
- `ProjectSettings`
- this history and the coding handoff

Regenerable folders, APK/AAB outputs, build logs, screenshots, videos, archives,
QA helpers, emulator binaries, keystores, IDE state, and Unity caches are excluded.

## Remaining production steps

1. Replace all Google test ad unit IDs with the owner's production IDs.
2. Configure and publish the UMP consent message in AdMob.
3. Add the hosted privacy-policy URL and complete Google Play Data safety, Ads,
   Target audience, IARC, and App content declarations truthfully.
4. Create a private upload keystore and keep it outside Git; enable Play App Signing.
5. Increment `versionCode` for each Play upload and build a signed release AAB.
6. Run final physical-device regression tests across supported aspect ratios,
   cutouts, Android versions, offline/resume cases, and every ad outcome.
