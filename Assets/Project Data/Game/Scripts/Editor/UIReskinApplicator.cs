using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    public static class UIReskinApplicator
    {
        private const string GeneralUi = "Assets/Project Data/Game/Images/Base/General UI/";
        private const string StoreUi = "Assets/Project Data/Game/Images/Base/Store/";
        private const string MiscUi = "Assets/Project Data/Game/Images/Base/Misc/";

        private static readonly Color OffWhite = FromHex("F5FBFF");
        private static readonly Color Ink = FromHex("16354C");
        private static readonly Color PaleBlue = FromHex("CDEFF3");
        private static readonly Color Coral = FromHex("FF9C91");

        private static Sprite primaryButton;
        private static Sprite secondaryButton;
        private static Sprite warningButton;
        private static Sprite disabledButton;
        private static Sprite modalPanel;
        private static Sprite bluePanel;
        private static Sprite redPanel;
        private static Sprite itemPanel;
        private static Sprite statusPanel;
        private static Sprite background;

        [MenuItem("Tools/Project/UI Reskin/Apply Twilight Meadow Theme")]
        public static void ApplyTheme()
        {
            GenerateSharedArtwork();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            LoadThemeSprites();

            int prefabCount = ApplyToPrefabs();
            int sceneCount = ApplyToScenes();

            AssetDatabase.SaveAssets();
            Debug.Log($"UI_RESKIN_APPLIED theme=TwilightMeadow prefabs={prefabCount} scenes={sceneCount}");
        }

        public static void GenerateSharedArtworkOnly()
        {
            GenerateSharedArtwork();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();
            Debug.Log("UI_RESKIN_ARTWORK_REFRESHED theme=TwilightMeadowV2");
        }

        private static void GenerateSharedArtwork()
        {
            WriteButton(GeneralUi + "btn_green.png", 156, 216, 44, 10, 11,
                FromHex("35E5AE"), FromHex("076777"), FromHex("F7D773"), FromHex("A9FFF0"), 255);
            WriteButton(GeneralUi + "btn_purple.png", 156, 216, 44, 10, 11,
                FromHex("A16BFF"), FromHex("48277D"), FromHex("73E3DA"), FromHex("D9C1FF"), 255);
            WriteButton(GeneralUi + "btn_orange.png", 156, 216, 44, 10, 11,
                FromHex("FF7279"), FromHex("9B2D59"), FromHex("FFD873"), FromHex("FFC0C8"), 255);
            WriteButton(GeneralUi + "btn_gray.png", 156, 216, 44, 10, 11,
                FromHex("8FA3B8"), FromHex("354B63"), FromHex("BFD9DA"), FromHex("E5F2F4"), 240);

            WriteRounded(GeneralUi + "panel_dark.png", 512, 512, 82, 12, 10,
                FromHex("183E59"), FromHex("0B1E38"), FromHex("58D9D0"), 250);
            WriteRounded(GeneralUi + "panel_blue.png", 512, 512, 82, 12, 10,
                FromHex("236B83"), FromHex("123A58"), FromHex("7EE8E1"), 250);
            WriteRounded(GeneralUi + "panel_red.png", 512, 512, 82, 12, 10,
                FromHex("864151"), FromHex("4B263D"), FromHex("FFAA9F"), 250);
            WriteRounded(GeneralUi + "panel.png", 256, 256, 52, 8, 7,
                FromHex("1C4A64"), FromHex("102A46"), FromHex("65DED5"), 246);
            WriteRounded(GeneralUi + "panel_outlined_dark.png", 256, 256, 52, 8, 7,
                FromHex("183A54"), FromHex("0D2039"), FromHex("69E4DA"), 246);

            WriteRounded(StoreUi + "panel_yellow_white.png", 512, 512, 72, 12, 9,
                FromHex("F8FDFF"), FromHex("D9EEF2"), FromHex("F4C96C"), 250);
            WriteRounded(StoreUi + "panel_red_white.png", 512, 512, 72, 12, 9,
                FromHex("FFF9FA"), FromHex("F7DEE2"), FromHex("FF9B92"), 250);
            WriteRounded(StoreUi + "panel_blue_store.png", 512, 512, 72, 12, 9,
                FromHex("236982"), FromHex("153A5B"), FromHex("74E4DD"), 252);
            WriteRounded(StoreUi + "panel_purple_store.png", 512, 512, 72, 12, 9,
                FromHex("6758B5"), FromHex("302D68"), FromHex("C5B8FF"), 252);
            WriteRounded(StoreUi + "item_background.png", 512, 512, 76, 12, 8,
                FromHex("F7FDFF"), FromHex("D9ECF1"), FromHex("A9E5E4"), 238);
            WriteRounded(StoreUi + "header_orange.png", 512, 512, 78, 12, 9,
                FromHex("FFD47A"), FromHex("E88B58"), FromHex("FFF0B5"), 252);
            WriteRounded(MiscUi + "universal_back.png", 512, 512, 240, 8, 7,
                FromHex("244B65"), FromHex("102B47"), FromHex("70DED7"), 244);
        }

        private static int ApplyToPrefabs()
        {
            string[] paths = AssetDatabase.FindAssets("t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsUiPrefabPath)
                .OrderBy(path => path)
                .ToArray();

            int changed = 0;
            foreach (string path in paths)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (!ContainsUi(root))
                        continue;

                    StyleHierarchy(root, true);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            return changed;
        }

        private static int ApplyToScenes()
        {
            string originalScene = SceneManager.GetActiveScene().path;
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            int changed = 0;

            try
            {
                foreach (string path in scenes)
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    bool sceneChanged = false;
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        if (root.GetComponentInChildren<Canvas>(true) == null)
                            continue;

                        StyleHierarchy(root, false);
                        sceneChanged = true;
                    }

                    if (sceneChanged)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                        changed++;
                    }
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(originalScene) && File.Exists(originalScene))
                    EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
            }

            return changed;
        }

        private static void StyleHierarchy(GameObject root, bool skipNestedPrefabInstances)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true).Where(image =>
                         ShouldStyle(image.gameObject, root, skipNestedPrefabInstances)))
                StyleImage(image);

            foreach (Button button in root.GetComponentsInChildren<Button>(true).Where(button =>
                         ShouldStyle(button.gameObject, root, skipNestedPrefabInstances)))
                StyleButton(button);

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true).Where(text =>
                         ShouldStyle(text.gameObject, root, skipNestedPrefabInstances)))
                StyleText(text);
        }

        private static void StyleImage(Image image)
        {
            string path = GetPath(image.transform);
            string spritePath = image.sprite == null ? string.Empty : AssetDatabase.GetAssetPath(image.sprite);
            float alpha = image.color.a;

            if (IsScreenBackground(image, path))
            {
                image.sprite = background;
                image.type = Image.Type.Simple;
                image.color = new Color(1, 1, 1, Mathf.Max(alpha, 0.94f));
                return;
            }

            if (spritePath.EndsWith("Popup_01.png", StringComparison.OrdinalIgnoreCase) ||
                spritePath.EndsWith("TableFrame00.png", StringComparison.OrdinalIgnoreCase))
            {
                image.sprite = path.IndexOf("Time Background", StringComparison.OrdinalIgnoreCase) >= 0 ? itemPanel : bluePanel;
                image.type = Image.Type.Sliced;
                image.color = new Color(1, 1, 1, Mathf.Max(alpha, 0.92f));
            }
            else if (spritePath.EndsWith("Popup_02.png", StringComparison.OrdinalIgnoreCase))
            {
                image.sprite = itemPanel;
                image.type = Image.Type.Sliced;
                image.color = new Color(1, 1, 1, Mathf.Max(alpha, 0.78f));
            }
            else if (spritePath.EndsWith("panel_dark.png", StringComparison.OrdinalIgnoreCase))
            {
                image.sprite = modalPanel;
                image.color = new Color(1, 1, 1, alpha);
            }
            else if (spritePath.EndsWith("panel_blue.png", StringComparison.OrdinalIgnoreCase))
            {
                image.sprite = bluePanel;
                image.color = new Color(1, 1, 1, alpha);
            }
            else if (spritePath.EndsWith("panel_red.png", StringComparison.OrdinalIgnoreCase))
            {
                image.sprite = redPanel;
                image.color = new Color(1, 1, 1, alpha);
            }
            else if (spritePath.EndsWith("universal_back.png", StringComparison.OrdinalIgnoreCase))
            {
                image.sprite = statusPanel;
                image.color = new Color(1, 1, 1, Mathf.Max(alpha, 0.88f));
            }
        }

        private static void StyleButton(Button button)
        {
            Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image == null)
                return;

            string spritePath = image.sprite == null ? string.Empty : AssetDatabase.GetAssetPath(image.sprite);
            bool isShape = image.type == Image.Type.Sliced ||
                           spritePath.IndexOf("/Button/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           spritePath.IndexOf("/Slider/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           Path.GetFileName(spritePath).StartsWith("btn_", StringComparison.OrdinalIgnoreCase);

            if (isShape && !IsCloseIcon(button.name, spritePath))
            {
                image.sprite = ChooseButtonSprite(GetPath(button.transform));
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = FromHex("E9FFFF");
            colors.pressedColor = FromHex("C8DDD9");
            colors.selectedColor = FromHex("E2FAF6");
            colors.disabledColor = new Color(0.48f, 0.55f, 0.61f, 0.55f);
            colors.colorMultiplier = 1;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private static void StyleText(TMP_Text text)
        {
            string path = GetPath(text.transform);
            Button parentButton = text.GetComponentInParent<Button>();
            bool storeText = path.IndexOf("IAP Store", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             path.IndexOf("Pack", StringComparison.OrdinalIgnoreCase) >= 0;
            bool premiumCard = path.IndexOf("Starter Pack", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               path.IndexOf("Power Ups Pack", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               path.IndexOf("Power Pack", StringComparison.OrdinalIgnoreCase) >= 0;

            if (parentButton != null)
                text.color = OffWhite;
            else if (path.IndexOf("Currency Panel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     path.IndexOf("Lives Indicator", StringComparison.OrdinalIgnoreCase) >= 0)
                text.color = OffWhite;
            else if (storeText && !premiumCard &&
                     (text.name.IndexOf("Description", StringComparison.OrdinalIgnoreCase) >= 0 ||
                      text.name.IndexOf("Text", StringComparison.OrdinalIgnoreCase) >= 0))
                text.color = Ink;
            else if (text.name.IndexOf("PU Description", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     text.name.IndexOf("Lose Life", StringComparison.OrdinalIgnoreCase) >= 0)
                text.color = Ink;
            else if (text.name.IndexOf("Level Failed", StringComparison.OrdinalIgnoreCase) >= 0)
                text.color = Coral;
            else if (text.name.IndexOf("Description", StringComparison.OrdinalIgnoreCase) >= 0)
                text.color = PaleBlue;
            else
                text.color = OffWhite;

            if (parentButton != null || IsHeading(text.name))
                text.fontStyle |= FontStyles.Bold;

            if (IsHeading(text.name))
                text.characterSpacing = Mathf.Max(text.characterSpacing, 1.5f);
        }

        private static Sprite ChooseButtonSprite(string path)
        {
            if (ContainsAny(path, "Quit Button", "Menu Button", "Exit Button", "Replay Button"))
                return warningButton;
            if (ContainsAny(path, "Play Button", "Next Level Button", "Revive Button", "Purchase Button", "Buy Button", "Refill Button", "Remove Button"))
                return primaryButton;
            if (path.IndexOf("Disabled", StringComparison.OrdinalIgnoreCase) >= 0)
                return disabledButton;
            return secondaryButton;
        }

        private static void LoadThemeSprites()
        {
            primaryButton = LoadSprite(GeneralUi + "btn_green.png");
            secondaryButton = LoadSprite(GeneralUi + "btn_purple.png");
            warningButton = LoadSprite(GeneralUi + "btn_orange.png");
            disabledButton = LoadSprite(GeneralUi + "btn_gray.png");
            modalPanel = LoadSprite(GeneralUi + "panel_dark.png");
            bluePanel = LoadSprite(GeneralUi + "panel_blue.png");
            redPanel = LoadSprite(GeneralUi + "panel_red.png");
            itemPanel = LoadSprite(StoreUi + "item_background.png");
            statusPanel = LoadSprite(MiscUi + "universal_back.png");
            background = LoadSprite(StoreUi + "background_gradient_store.png");
        }

        private static void WriteRounded(string path, int width, int height, float radius, int inset, int shadow,
            Color top, Color bottom, Color border, byte alpha)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[width * height];
            Color shadowColor = new Color(0.015f, 0.04f, 0.09f, 0.45f);
            float left = inset;
            float right = width - inset - 1;
            float mainBottom = inset + shadow;
            float topEdge = height - inset - 1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = Color.clear;
                    if (InsideRounded(x, y, left, inset, right, topEdge - shadow, radius))
                        color = shadowColor;

                    if (InsideRounded(x, y, left, mainBottom, right, topEdge, radius))
                    {
                        float t = Mathf.InverseLerp(mainBottom, topEdge, y);
                        color = Color.Lerp(bottom, top, t);
                        color.a = alpha / 255f;

                        int borderSize = Mathf.Max(3, width / 85);
                        if (!InsideRounded(x, y, left + borderSize, mainBottom + borderSize,
                                right - borderSize, topEdge - borderSize, Mathf.Max(1, radius - borderSize)))
                        {
                            color = border;
                            color.a = alpha / 255f;
                        }
                        else if (t > 0.72f)
                        {
                            color = Color.Lerp(color, Color.white, (t - 0.72f) * 0.16f);
                            color.a = alpha / 255f;
                        }
                    }

                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void WriteButton(string path, int width, int height, float radius, int inset, int shadow,
            Color top, Color bottom, Color outerBorder, Color innerBorder, byte alpha)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[width * height];
            Color shadowColor = FromHex("10243C");
            shadowColor.a = 0.72f;
            float left = inset;
            float right = width - inset - 1;
            float mainBottom = inset + shadow;
            float topEdge = height - inset - 1;
            int outerSize = Mathf.Max(4, width / 22);
            int innerSize = Mathf.Max(2, width / 52);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = Color.clear;
                    if (InsideRounded(x, y, left, inset, right, topEdge - shadow, radius))
                        color = shadowColor;

                    if (InsideRounded(x, y, left, mainBottom, right, topEdge, radius))
                    {
                        float t = Mathf.InverseLerp(mainBottom, topEdge, y);
                        color = Color.Lerp(bottom, top, Mathf.SmoothStep(0, 1, t));
                        color.a = alpha / 255f;

                        bool insideOuter = InsideRounded(x, y, left + outerSize, mainBottom + outerSize,
                            right - outerSize, topEdge - outerSize, Mathf.Max(1, radius - outerSize));
                        bool insideInner = InsideRounded(x, y, left + outerSize + innerSize, mainBottom + outerSize + innerSize,
                            right - outerSize - innerSize, topEdge - outerSize - innerSize,
                            Mathf.Max(1, radius - outerSize - innerSize));

                        if (!insideOuter)
                        {
                            float rimLight = Mathf.Lerp(0.72f, 1.08f, t);
                            color = outerBorder * rimLight;
                            color.a = alpha / 255f;
                        }
                        else if (!insideInner)
                        {
                            color = innerBorder;
                            color.a = alpha / 255f;
                        }
                        else if (t > 0.66f)
                        {
                            color = Color.Lerp(color, Color.white, (t - 0.66f) * 0.28f);
                            color.a = alpha / 255f;
                        }
                        else if (t < 0.16f)
                        {
                            color = Color.Lerp(color, FromHex("10243C"), (0.16f - t) * 0.8f);
                            color.a = alpha / 255f;
                        }
                    }

                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static bool InsideRounded(float x, float y, float left, float bottom, float right, float top, float radius)
        {
            if (x < left || x > right || y < bottom || y > top)
                return false;
            float cx = Mathf.Clamp(x, left + radius, right - radius);
            float cy = Mathf.Clamp(y, bottom + radius, top - radius);
            float dx = x - cx;
            float dy = y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static bool IsUiPrefabPath(string path)
        {
            if (path.IndexOf("/Prefabs/UI/", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (path.IndexOf("/Extra Components/", StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return ContainsAny(path, "/IAP Module/", "/IAP Store/", "/Lives System/",
                "/Power Ups System/", "/Settings Panel/", "/Currency System/");
        }

        private static bool ShouldStyle(GameObject gameObject, GameObject editedRoot, bool skipNestedPrefabInstances)
        {
            if (!skipNestedPrefabInstances)
                return true;

            GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
            return instanceRoot == null || instanceRoot == editedRoot;
        }

        private static bool ContainsUi(GameObject root)
        {
            return root.GetComponentInChildren<Image>(true) != null || root.GetComponentInChildren<TMP_Text>(true) != null;
        }

        private static bool IsScreenBackground(Image image, string path)
        {
            string name = image.name;
            return (name.Equals("Background", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Background Image", StringComparison.OrdinalIgnoreCase)) &&
                   (path.IndexOf("UI Complete", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("UI Game Over", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("UI IAP Store", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsCloseIcon(string name, string spritePath)
        {
            return name.IndexOf("Close", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   (spritePath.EndsWith("btn_close.png", StringComparison.OrdinalIgnoreCase) ||
                    spritePath.IndexOf("btn_red_close", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsHeading(string name)
        {
            return ContainsAny(name, "Title", "Header", "Level Failed", "LevelText", "Completed", "Heading", "Starter Pack Text");
        }

        private static bool ContainsAny(string value, params string[] candidates)
        {
            return candidates.Any(candidate => value.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException($"Reskin sprite was not imported: {path}");
            return sprite;
        }

        private static string GetPath(Transform transform)
        {
            List<string> names = new List<string>();
            for (Transform current = transform; current != null; current = current.parent)
                names.Add(current.name);
            names.Reverse();
            return string.Join("/", names);
        }

        private static Color FromHex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString("#" + hex, out Color color))
                throw new ArgumentException($"Invalid color: {hex}");
            return color;
        }
    }
}
