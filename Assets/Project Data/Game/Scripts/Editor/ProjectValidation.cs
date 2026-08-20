using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Watermelon
{
    public static class ProjectValidation
    {
        private const string AndroidBuildPath = "Builds/Android/AnimalTilesTripleMatch-Release.aab";
        private const string AndroidApkBuildPath = "Builds/Android/AnimalTilesTripleMatch.apk";
        private const string AndroidDevelopmentApkBuildPath = "Builds/Android/AnimalTilesTripleMatch-Development.apk";
        private const string SmokeActiveKey = "AnimalTilesTripleMatch.SmokeTest.Active";
        private const string SmokePhaseKey = "AnimalTilesTripleMatch.SmokeTest.Phase";
        private const string SmokeStartedKey = "AnimalTilesTripleMatch.SmokeTest.Started";
        private const string SmokeFailureKey = "AnimalTilesTripleMatch.SmokeTest.Failure";

        [InitializeOnLoadMethod]
        private static void ContinueSmokeTestAfterReload()
        {
            if (SessionState.GetBool(SmokeActiveKey, false))
                RegisterSmokeTestCallbacks();
        }

        [MenuItem("Tools/Project/Validate Project")]
        public static void ValidateProject()
        {
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .ToArray();

            if (buildScenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes are configured in Build Settings.");

            int missingScriptCount = 0;
            string originalScenePath = SceneManager.GetActiveScene().path;

            try
            {
                foreach (EditorBuildSettingsScene buildScene in buildScenes)
                {
                    if (string.IsNullOrEmpty(buildScene.path) || !File.Exists(buildScene.path))
                        throw new FileNotFoundException($"Build scene is missing: {buildScene.path}");

                    Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                    missingScriptCount += CountMissingScripts(scene.GetRootGameObjects());
                }

                string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToArray();

                foreach (string prefabPath in prefabPaths)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null)
                        missingScriptCount += CountMissingScripts(new[] { prefab });
                }

                if (missingScriptCount > 0)
                    throw new InvalidOperationException($"Project validation found {missingScriptCount} missing script component(s).");

                Debug.Log($"PROJECT_VALIDATION_SUCCESS scenes={buildScenes.Length} prefabs={prefabPaths.Length} missingScripts=0");
            }
            finally
            {
                if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }
        }

        public static void ImportTextMeshProEssentials()
        {
            string packagePath = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(packagePath))
                throw new FileNotFoundException("TMP Essential Resources package was not found in the package cache.");

            AssetDatabase.ImportPackage(Path.GetFullPath(packagePath), false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"TMP_ESSENTIALS_IMPORTED package={packagePath}");
        }

        public static void BuildAndroid()
        {
            BuildAndroidArtifact(AndroidBuildPath, true, "ANDROID_BUILD_RESULT");
        }

        public static void BuildAndroidApk()
        {
            BuildAndroidArtifact(AndroidApkBuildPath, false, BuildOptions.None, "ANDROID_APK_BUILD_RESULT");
        }

        public static void BuildAndroidDevelopmentApk()
        {
            BuildAndroidArtifact(AndroidDevelopmentApkBuildPath, false,
                BuildOptions.Development | BuildOptions.AllowDebugging,
                "ANDROID_DEVELOPMENT_APK_BUILD_RESULT");
        }

        private static void BuildAndroidArtifact(string buildPath, bool buildAppBundle, string resultLabel)
        {
            BuildAndroidArtifact(buildPath, buildAppBundle, BuildOptions.None, resultLabel);
        }

        private static void BuildAndroidArtifact(string buildPath, bool buildAppBundle, BuildOptions buildOptions,
            string resultLabel)
        {
            ValidateProject();
            ConfigureAndroidToolsFromEnvironment();
            ConfigureAndroidGraphicsApi();
            ConfigureAndroidSigningFromEnvironment(buildAppBundle);

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Unity Android Build Support is not installed for this editor.");

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            Directory.CreateDirectory(Path.GetDirectoryName(buildPath));
            EditorUserBuildSettings.buildAppBundle = buildAppBundle;

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = buildOptions
            });

            Debug.Log($"{resultLabel} result={report.summary.result} errors={report.summary.totalErrors} " +
                      $"warnings={report.summary.totalWarnings} size={report.summary.totalSize} output={buildPath}");

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Android build failed: {report.summary.result}");
        }

        private static void ConfigureAndroidSigningFromEnvironment(bool buildAppBundle)
        {
            if (!buildAppBundle)
                return;

            string keystorePath = Environment.GetEnvironmentVariable("UNITY_ANDROID_KEYSTORE_PATH");
            string keystorePassword = Environment.GetEnvironmentVariable("UNITY_ANDROID_KEYSTORE_PASSWORD");
            string alias = Environment.GetEnvironmentVariable("UNITY_ANDROID_KEY_ALIAS");
            string aliasPassword = Environment.GetEnvironmentVariable("UNITY_ANDROID_KEY_PASSWORD");

            if (string.IsNullOrWhiteSpace(keystorePath) || !File.Exists(keystorePath) ||
                string.IsNullOrWhiteSpace(keystorePassword) || string.IsNullOrWhiteSpace(alias) ||
                string.IsNullOrWhiteSpace(aliasPassword))
            {
                throw new InvalidOperationException(
                    "Release AAB signing is not configured. Set UNITY_ANDROID_KEYSTORE_PATH, " +
                    "UNITY_ANDROID_KEYSTORE_PASSWORD, UNITY_ANDROID_KEY_ALIAS, and " +
                    "UNITY_ANDROID_KEY_PASSWORD before building an Android App Bundle.");
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = aliasPassword;

            Debug.Log($"ANDROID_SIGNING release keystore={keystorePath} alias={alias}");
        }

        private static void ConfigureAndroidToolsFromEnvironment()
        {
            string jdkPath = Environment.GetEnvironmentVariable("UNITY_JDK_PATH");
            string sdkPath = Environment.GetEnvironmentVariable("UNITY_ANDROID_SDK_PATH");
            string ndkPath = Environment.GetEnvironmentVariable("UNITY_ANDROID_NDK_PATH");

            Type settingsType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UnityEditor.Android.AndroidExternalToolsSettings", false))
                .FirstOrDefault(type => type != null);

            if (settingsType == null)
            {
                Debug.LogWarning("ANDROID_TOOLS skipped: Unity Android Build Support is not installed.");
                return;
            }

            SetStaticString(settingsType, "jdkRootPath", jdkPath);
            SetStaticString(settingsType, "sdkRootPath", sdkPath);
            SetStaticString(settingsType, "ndkRootPath", ndkPath);

            Debug.Log($"ANDROID_TOOLS jdk={GetStaticString(settingsType, "jdkRootPath")} " +
                      $"sdk={GetStaticString(settingsType, "sdkRootPath")} ndk={GetStaticString(settingsType, "ndkRootPath")}");
        }

        private static void SetStaticString(Type type, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Static);
            if (field != null && field.FieldType == typeof(string))
            {
                field.SetValue(null, value);
                return;
            }

            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            if (property != null && property.CanWrite && property.PropertyType == typeof(string))
                property.SetValue(null, value);
        }

        private static string GetStaticString(Type type, string name)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Static);
            if (field != null && field.FieldType == typeof(string))
                return field.GetValue(null) as string ?? string.Empty;

            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            return property?.GetValue(null) as string ?? string.Empty;
        }

        private static void ConfigureAndroidGraphicsApi()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            Debug.Log("ANDROID_GRAPHICS_APIS OpenGLES3");
        }

        public static void RunPlayModeSmokeTest()
        {
            EditorSceneManager.OpenScene("Assets/Project Data/Game/Scenes/Init.unity", OpenSceneMode.Single);

            SessionState.SetBool(SmokeActiveKey, true);
            SessionState.SetInt(SmokePhaseKey, 0);
            SessionState.SetString(SmokeFailureKey, string.Empty);
            SessionState.SetFloat(SmokeStartedKey, (float)EditorApplication.timeSinceStartup);

            RegisterSmokeTestCallbacks();
            EditorApplication.isPlaying = true;
        }

        private static void RegisterSmokeTestCallbacks()
        {
            EditorApplication.update -= UpdateSmokeTest;
            EditorApplication.update += UpdateSmokeTest;
            Application.logMessageReceived -= CaptureSmokeTestFailure;
            Application.logMessageReceived += CaptureSmokeTestFailure;
        }

        private static void UpdateSmokeTest()
        {
            int phase = SessionState.GetInt(SmokePhaseKey, 0);
            double elapsed = EditorApplication.timeSinceStartup - SessionState.GetFloat(SmokeStartedKey, 0);

            if (phase == 0 && EditorApplication.isPlaying)
            {
                SessionState.SetInt(SmokePhaseKey, 1);
                SessionState.SetFloat(SmokeStartedKey, (float)EditorApplication.timeSinceStartup);
                return;
            }

            if (phase == 1 && (elapsed >= 12 || !string.IsNullOrEmpty(SessionState.GetString(SmokeFailureKey, string.Empty))))
            {
                SessionState.SetInt(SmokePhaseKey, 2);
                EditorApplication.isPlaying = false;
                return;
            }

            if (phase == 2 && !EditorApplication.isPlaying)
            {
                string failure = SessionState.GetString(SmokeFailureKey, string.Empty);
                ClearSmokeTestState();

                if (string.IsNullOrEmpty(failure))
                {
                    Debug.Log("PLAYMODE_SMOKE_SUCCESS durationSeconds=12 scene=Init");
                    EditorApplication.Exit(0);
                }
                else
                {
                    Debug.LogError($"PLAYMODE_SMOKE_FAILED {failure}");
                    EditorApplication.Exit(1);
                }
            }
        }

        private static void CaptureSmokeTestFailure(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Assert && type != LogType.Error)
                return;

            if (!EditorApplication.isPlaying || condition.Contains("No graphic device is available") ||
                stackTrace.Contains("UnityEditor.Search."))
                return;

            SessionState.SetString(SmokeFailureKey, $"type={type} message={condition} stack={stackTrace}");
        }

        private static void ClearSmokeTestState()
        {
            EditorApplication.update -= UpdateSmokeTest;
            Application.logMessageReceived -= CaptureSmokeTestFailure;
            SessionState.EraseBool(SmokeActiveKey);
            SessionState.EraseInt(SmokePhaseKey);
            SessionState.EraseFloat(SmokeStartedKey);
            SessionState.EraseString(SmokeFailureKey);
        }

        private static int CountMissingScripts(GameObject[] roots)
        {
            int missingScriptCount = 0;

            foreach (GameObject root in roots)
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    missingScriptCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
            }

            return missingScriptCount;
        }
    }
}
