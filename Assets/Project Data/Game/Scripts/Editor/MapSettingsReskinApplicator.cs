using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.Map;

namespace Watermelon
{
    public static class MapSettingsReskinApplicator
    {
        private const string MapPrefabFolder = "Assets/Project Data/Game/Prefabs/Map/";
        private const string SettingsPrefabPath = "Assets/Project Data/Core/Extra Components/Settings Panel/Prefabs/SettingsPanel.prefab";
        private const string PrivacyIconPath = "Assets/Project Data/Core/Extra Components/Settings Panel/Sprites/settings_privacy.png";

        public static void ApplyMapAndSettingsReskin()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ApplyMapLayout();
            ApplySettingsPanel();
            AssetDatabase.SaveAssets();
            Debug.Log("MAP_SETTINGS_RESKIN_APPLIED chunks=4 privacyButton=1");
        }

        [MenuItem("Tools/Project/UI Reskin/Fix Level Map Background Fit")]
        public static void FixLevelMapBackgroundFit()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ApplyMapLayout();
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL_MAP_BACKGROUND_FIT_SUCCESS chunks=4");
        }

        public static void AuditLevelMaps()
        {
            for (int chunkIndex = 1; chunkIndex <= 4; chunkIndex++)
            {
                string path = $"{MapPrefabFolder}Chunk_{chunkIndex}.prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    MapChunkBehavior chunk = root.GetComponent<MapChunkBehavior>();
                    SerializedProperty levels = new SerializedObject(chunk).FindProperty("levels");
                    string positions = string.Join("; ", Enumerable.Range(0, levels.arraySize).Select(index =>
                    {
                        MapLevelBehavior level = levels.GetArrayElementAtIndex(index).objectReferenceValue as MapLevelBehavior;
                        return $"{index + 1}=({level.transform.localPosition.x:0.###},{level.transform.localPosition.y:0.###})";
                    }));
                    Transform map = root.transform.Find("Background/Map");
                    SpriteRenderer mapRenderer = map.GetComponent<SpriteRenderer>();
                    Vector2 spriteSize = mapRenderer.sprite.bounds.size;
                    Vector3 parentScale = map.parent.localScale;
                    Vector2 fittedSize = new Vector2(
                        spriteSize.x * Mathf.Abs(map.localScale.x * parentScale.x),
                        spriteSize.y * Mathf.Abs(map.localScale.y * parentScale.y));
                    Debug.Log($"LEVEL_MAP_AUDIT chunk={chunkIndex} drawMode={mapRenderer.drawMode} " +
                              $"mapScale={map.localScale} fittedSize={fittedSize} positions={positions}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void ApplyMapLayout()
        {
            for (int chunkIndex = 1; chunkIndex <= 4; chunkIndex++)
            {
                string path = $"{MapPrefabFolder}Chunk_{chunkIndex}.prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Transform map = root.transform.Find("Background/Map");
                    if (map == null)
                        throw new MissingReferenceException($"Map transform is missing in {path}");

                    SpriteRenderer mapRenderer = map.GetComponent<SpriteRenderer>();
                    SpriteRenderer backgroundRenderer = root.transform.Find("Background").GetComponent<SpriteRenderer>();
                    if (mapRenderer.sprite == null)
                        throw new MissingReferenceException($"Map sprite is missing in {path}");

                    // The decorative Background object is intentionally four times wider
                    // than the visible map. Because Map is its child, using a unit scale
                    // also stretched the biome artwork four times and cropped most of it.
                    // Fit the sprite to the logical chunk size while cancelling the parent
                    // scale, so the complete road and all ten waypoint pads remain visible.
                    Vector2 spriteSize = mapRenderer.sprite.bounds.size;
                    Vector3 parentScale = map.parent.localScale;
                    mapRenderer.drawMode = SpriteDrawMode.Simple;
                    map.localScale = new Vector3(
                        backgroundRenderer.size.x / (spriteSize.x * Mathf.Abs(parentScale.x)),
                        backgroundRenderer.size.y / (spriteSize.y * Mathf.Abs(parentScale.y)),
                        1f);
                    // Every exported section now has the same 960x1024 ratio.
                    // Remove the legacy per-biome Y nudges so adjoining sprite
                    // edges share exactly the same chunk coordinate boundary.
                    Vector3 localPosition = map.localPosition;
                    map.localPosition = new Vector3(0f, 0f, localPosition.z);
                    mapRenderer.color = Color.white;
                    EditorUtility.SetDirty(mapRenderer);

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void ApplySettingsPanel()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SettingsPrefabPath);
            try
            {
                SettingsPanel panel = root.GetComponent<SettingsPanel>();
                Transform sound = root.transform.Find("SoundButton");
                Transform vibration = root.transform.Find("VibroButton");
                Transform noAds = root.transform.Find("NoAdsButton");
                Transform restore = root.transform.Find("RestorePurchasesButton");
                if (panel == null || sound == null || vibration == null || noAds == null || restore == null)
                    throw new MissingReferenceException("Settings panel structure is incomplete.");

                Transform privacy = root.transform.Find("PrivacyPolicyButton");
                SettingsLinkButton privacyLink;
                bool privacyCreated = false;
                if (privacy == null)
                {
                    GameObject privacyObject = Object.Instantiate(sound.gameObject, root.transform);
                    privacyObject.name = "PrivacyPolicyButton";
                    privacy = privacyObject.transform;
                    privacy.SetSiblingIndex(sound.GetSiblingIndex() + 1);

                    Button button = privacyObject.GetComponent<Button>();
                    while (button.onClick.GetPersistentEventCount() > 0)
                        UnityEventTools.RemovePersistentListener(button.onClick, 0);

                    SettingsSoundToggleButton soundToggle = privacyObject.GetComponent<SettingsSoundToggleButton>();
                    if (soundToggle != null)
                        Object.DestroyImmediate(soundToggle);

                    privacyLink = privacyObject.AddComponent<SettingsLinkButton>();
                    UnityEventTools.AddPersistentListener(button.onClick, privacyLink.OnClick);
                    privacyCreated = true;
                }
                else
                {
                    privacyLink = privacy.GetComponent<SettingsLinkButton>();
                }

                SerializedObject linkObject = new SerializedObject(privacyLink);
                linkObject.FindProperty("isActive").boolValue = true;
                // Leave an existing production URL intact when this tool is rerun.
                if (privacyCreated)
                    linkObject.FindProperty("url").stringValue = string.Empty;
                linkObject.ApplyModifiedPropertiesWithoutUndo();

                TextureImporter privacyImporter = AssetImporter.GetAtPath(PrivacyIconPath) as TextureImporter;
                if (privacyImporter == null)
                    throw new MissingReferenceException($"Privacy icon importer is missing: {PrivacyIconPath}");
                if (privacyImporter.textureType != TextureImporterType.Sprite)
                {
                    privacyImporter.textureType = TextureImporterType.Sprite;
                    privacyImporter.spriteImportMode = SpriteImportMode.Single;
                    privacyImporter.alphaIsTransparency = true;
                    privacyImporter.mipmapEnabled = false;
                    privacyImporter.SaveAndReimport();
                }

                Sprite privacySprite = AssetDatabase.LoadAssetAtPath<Sprite>(PrivacyIconPath);
                if (privacySprite == null)
                    throw new MissingReferenceException($"Privacy icon did not import: {PrivacyIconPath}");
                privacy.Find("Image").GetComponent<Image>().sprite = privacySprite;
                privacy.GetComponent<RectTransform>().anchoredPosition = new Vector2(5, -420);

                SettingsButtonBase[] orderedButtons =
                {
                    vibration.GetComponent<SettingsButtonBase>(),
                    sound.GetComponent<SettingsButtonBase>(),
                    privacyLink,
                    noAds.GetComponent<SettingsButtonBase>(),
                    restore.GetComponent<SettingsButtonBase>(),
                };

                SerializedObject panelObject = new SerializedObject(panel);
                SerializedProperty buttonInfos = panelObject.FindProperty("settingsButtonsInfo");
                buttonInfos.arraySize = orderedButtons.Length;
                for (int index = 0; index < orderedButtons.Length; index++)
                {
                    buttonInfos.GetArrayElementAtIndex(index)
                        .FindPropertyRelative("settingsButton").objectReferenceValue = orderedButtons[index];
                }
                panelObject.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(panel);
                EditorUtility.SetDirty(privacyLink);
                PrefabUtility.SaveAsPrefabAsset(root, SettingsPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
