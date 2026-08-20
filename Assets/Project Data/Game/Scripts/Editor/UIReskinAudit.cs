using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    public static class UIReskinAudit
    {
        private const string OutputPath = "Logs/UIReskinInventory.md";
        private const string UsedArtworkOutputPath = "Logs/UsedIconButtonInventory.md";

        public static void CreateUsedIconButtonInventory()
        {
            Dictionary<string, UsedSpriteInfo> sprites = new Dictionary<string, UsedSpriteInfo>(StringComparer.OrdinalIgnoreCase);
            List<string> assets = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToList();
            assets.AddRange(AssetDatabase.FindAssets("t:Prefab", new[]
                {
                    "Assets/Project Data/Game/Prefabs",
                    "Assets/Project Data/Core/Extra Components"
                })
                .Select(AssetDatabase.GUIDToAssetPath));

            string originalScenePath = SceneManager.GetActiveScene().path;
            try
            {
                foreach (string assetPath in assets.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path))
                {
                    GameObject[] roots;
                    GameObject prefabRoot = null;
                    if (assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    {
                        Scene scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);
                        roots = scene.GetRootGameObjects();
                    }
                    else
                    {
                        prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                        roots = new[] { prefabRoot };
                    }

                    try
                    {
                        foreach (GameObject root in roots)
                        {
                            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                            {
                                if (image.sprite == null)
                                    continue;

                                string spritePath = AssetDatabase.GetAssetPath(image.sprite);
                                if (string.IsNullOrEmpty(spritePath))
                                    continue;

                                Button nearestButton = image.GetComponentInParent<Button>(true);
                                bool isButtonTarget = nearestButton != null && nearestButton.targetGraphic == image;
                                bool isButtonChild = nearestButton != null && !isButtonTarget;
                                if (!sprites.TryGetValue(spritePath, out UsedSpriteInfo info))
                                {
                                    info = new UsedSpriteInfo();
                                    sprites.Add(spritePath, info);
                                }

                                info.Total++;
                                if (isButtonTarget) info.ButtonTargets++;
                                else if (isButtonChild) info.ButtonChildren++;
                                else info.OtherImages++;
                                if (info.Samples.Count < 4)
                                    info.Samples.Add($"{assetPath} :: {GetHierarchyPath(image.transform)}");
                            }
                        }
                    }
                    finally
                    {
                        if (prefabRoot != null)
                            PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(UsedArtworkOutputPath));
            StringBuilder report = new StringBuilder();
            report.AppendLine("# Used Icon and Button Sprite Inventory");
            report.AppendLine();
            report.AppendLine("| Sprite | Total | Button target | Button child | Other UI | Samples |");
            report.AppendLine("|---|---:|---:|---:|---:|---|");
            foreach (KeyValuePair<string, UsedSpriteInfo> pair in sprites.OrderBy(pair => pair.Key))
            {
                UsedSpriteInfo info = pair.Value;
                report.AppendLine($"| `{pair.Key}` | {info.Total} | {info.ButtonTargets} | {info.ButtonChildren} | {info.OtherImages} | {string.Join("<br>", info.Samples)} |");
            }
            File.WriteAllText(UsedArtworkOutputPath, report.ToString(), Encoding.UTF8);
            Debug.Log($"USED_ICON_BUTTON_INVENTORY_SUCCESS sprites={sprites.Count} output={UsedArtworkOutputPath}");
        }

        [MenuItem("Tools/Project/UI Reskin/Create Inventory")]
        public static void CreateInventory()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            StringBuilder report = new StringBuilder(256 * 1024);
            report.AppendLine("# UI Reskin Inventory");
            report.AppendLine();
            report.AppendLine($"Generated: {DateTime.UtcNow:O}");
            report.AppendLine();

            string[] scenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            string originalScenePath = SceneManager.GetActiveScene().path;

            try
            {
                foreach (string scenePath in scenePaths)
                {
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    AppendAsset(report, "Scene", scenePath, scene.GetRootGameObjects());
                }

                string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[]
                    {
                        "Assets/Project Data/Game/Prefabs/UI",
                        "Assets/Project Data/Core/Prefabs"
                    })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => path.IndexOf("/UI/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   path.IndexOf("Canvas", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   path.IndexOf("Panel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   path.IndexOf("Indicator", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   path.IndexOf("Pop Up", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   path.IndexOf("Loading", StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(path => path)
                    .ToArray();

                foreach (string prefabPath in prefabPaths)
                {
                    GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                    try
                    {
                        AppendAsset(report, "Prefab", prefabPath, new[] { root });
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }

                File.WriteAllText(OutputPath, report.ToString(), Encoding.UTF8);
                Debug.Log($"UI_RESKIN_INVENTORY_SUCCESS scenes={scenePaths.Length} prefabs={prefabPaths.Length} output={OutputPath}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }
        }

        private sealed class UsedSpriteInfo
        {
            public int Total;
            public int ButtonTargets;
            public int ButtonChildren;
            public int OtherImages;
            public readonly List<string> Samples = new List<string>();
        }

        private static void AppendAsset(StringBuilder report, string kind, string path, IEnumerable<GameObject> roots)
        {
            Transform[] transforms = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            Button[] buttons = transforms.SelectMany(transform => transform.GetComponents<Button>()).ToArray();
            Image[] images = transforms.SelectMany(transform => transform.GetComponents<Image>()).ToArray();
            TMP_Text[] texts = transforms.SelectMany(transform => transform.GetComponents<TMP_Text>()).ToArray();
            Canvas[] canvases = transforms.SelectMany(transform => transform.GetComponents<Canvas>()).ToArray();

            report.AppendLine($"## {kind}: `{path}`");
            report.AppendLine();
            report.AppendLine($"Objects: {transforms.Length}; Canvases: {canvases.Length}; Images: {images.Length}; TMP texts: {texts.Length}; Buttons: {buttons.Length}");
            report.AppendLine();

            foreach (Button button in buttons.OrderBy(button => GetHierarchyPath(button.transform)))
            {
                report.AppendLine($"- Button `{GetHierarchyPath(button.transform)}` interactable={button.interactable} transition={button.transition} listeners={button.onClick.GetPersistentEventCount()}");
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    UnityEngine.Object target = button.onClick.GetPersistentTarget(i);
                    report.AppendLine($"  - `{target?.GetType().FullName ?? "null"}.{button.onClick.GetPersistentMethodName(i)}` state={button.onClick.GetPersistentListenerState(i)}");
                }
            }

            foreach (TMP_Text text in texts.OrderBy(text => GetHierarchyPath(text.transform)))
            {
                string value = (text.text ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
                report.AppendLine($"- Text `{GetHierarchyPath(text.transform)}` value=\"{value}\" fontSize={text.fontSize:0.##} color={ColorUtility.ToHtmlStringRGBA(text.color)} align={text.alignment}");
            }

            foreach (Image image in images.OrderBy(image => GetHierarchyPath(image.transform)))
            {
                string spritePath = image.sprite == null ? "none" : AssetDatabase.GetAssetPath(image.sprite);
                report.AppendLine($"- Image `{GetHierarchyPath(image.transform)}` sprite=`{spritePath}` type={image.type} color={ColorUtility.ToHtmlStringRGBA(image.color)} raycast={image.raycastTarget}");
            }

            report.AppendLine();
        }

        private static string GetHierarchyPath(Transform transform)
        {
            List<string> names = new List<string>();
            for (Transform current = transform; current != null; current = current.parent)
                names.Add(current.name);
            names.Reverse();
            return string.Join("/", names);
        }
    }
}
