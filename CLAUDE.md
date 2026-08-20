# Animal Tiles: Triple Match — Claude Code Handoff

## Project

- Unity editor: `6000.5.0f1`
- Android package: `com.rationalstudio.animaltiles.triplematch`
- Android version: `1.0.0` (`versionCode` `56`)
- Minimum SDK: `26`
- Target SDK: `36`
- Active Git branch: `agent/full-ui-reskin`

## Non-negotiable rule

Preserve gameplay, level progression, controls, save-data keys, button actions,
navigation, game balance, ad/reward outcomes, and existing serialized Unity
references. UI and presentation changes must not break stored player progress.

## Build

Unity executable:

`C:\Program Files\Unity\Hub\Editor\6000.5.0f1\Editor\Unity.exe`

Automated Android development APK build method:

`Watermelon.ProjectValidation.BuildAndroidApk`

Default development output:

`Builds/Android/AnimalTilesTripleMatch-Development.apk`

APK/AAB files are local build artifacts and are intentionally excluded from Git.

## Ads

Google Mobile Ads official test IDs are configured for App Open, Banner,
Interstitial, and Rewarded formats. Do not publish a production build until the
owner's production IDs, UMP consent message, privacy policy, Data safety form,
and target-audience settings are complete.

## Important source areas

- Gameplay/UI scripts: `Assets/Project Data/Game/Scripts`
- Core UI systems: `Assets/Project Data/Core/Extra Components`
- Scenes: `Assets/Project Data/Game/Scenes`
- Android configuration: `Assets/Plugins/Android`
- Package dependencies: `Packages`
- Unity settings: `ProjectSettings`
- Full implementation history: `PROJECT_HISTORY.md`

Unity-generated folders such as `Library`, `Temp`, `Logs`, `obj`, and local build
outputs can be regenerated and must not be committed.
