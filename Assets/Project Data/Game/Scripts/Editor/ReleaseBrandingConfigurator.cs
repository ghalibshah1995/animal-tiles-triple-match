using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Watermelon
{
    public static class ReleaseBrandingConfigurator
    {
        private const string ProductName = "Animal Tiles: Triple Match";
        private const string CompanyName = "Rational Studio";
        private const string AndroidPackage = "com.rationalstudio.animaltiles.triplematch";

        private const string LegacyIconPath = "Assets/Branding/Android/animal_tiles_legacy.png";
        private const string RoundIconPath = "Assets/Branding/Android/animal_tiles_round.png";
        private const string AdaptiveForegroundPath =
            "Assets/Branding/Android/animal_tiles_adaptive_foreground.png";
        private const string AdaptiveBackgroundPath =
            "Assets/Branding/Android/animal_tiles_adaptive_background.png";

        [MenuItem("Tools/Project/Apply Final Android Branding")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureTexture(LegacyIconPath, false);
            ConfigureTexture(RoundIconPath, false);
            ConfigureTexture(AdaptiveForegroundPath, true);
            ConfigureTexture(AdaptiveBackgroundPath, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Texture2D legacy = LoadIcon(LegacyIconPath);
            Texture2D round = LoadIcon(RoundIconPath);
            Texture2D adaptiveForeground = LoadIcon(AdaptiveForegroundPath);
            Texture2D adaptiveBackground = LoadIcon(AdaptiveBackgroundPath);

            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackage);

            PlatformIconKind[] kinds =
                PlayerSettings.GetSupportedIconKindsForPlatform(BuildTargetGroup.Android);
            foreach (PlatformIconKind kind in kinds)
            {
                PlatformIcon[] slots = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                string kindName = kind.ToString();

                foreach (PlatformIcon slot in slots)
                {
                    if (slot.maxLayerCount >= 2 ||
                        kindName.IndexOf("adaptive", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        slot.SetTextures(new[] { adaptiveForeground, adaptiveBackground });
                    }
                    else if (kindName.IndexOf("round", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        slot.SetTexture(round, 0);
                    }
                    else
                    {
                        slot.SetTexture(legacy, 0);
                    }
                }

                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, slots);
                Debug.Log($"ANDROID_ICON_ASSIGNED kind={kindName} slots={slots.Length}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"FINAL_BRANDING_APPLIED product={ProductName} package={AndroidPackage}");
        }

        private static void ConfigureTexture(string path, bool alphaTransparency)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Missing icon importer: " + path);

            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.alphaIsTransparency = alphaTransparency;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Texture2D LoadIcon(string path)
        {
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (icon == null)
                throw new InvalidOperationException("Unable to load icon: " + path);
            return icon;
        }
    }
}
