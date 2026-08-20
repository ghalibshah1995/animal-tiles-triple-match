using UnityEditor;

namespace Watermelon
{
    public class PremiumUITextureImporter : AssetPostprocessor
    {
        private const string PremiumPath = "Assets/Resources/PremiumUI/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(PremiumPath, System.StringComparison.OrdinalIgnoreCase))
                return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            bool requiresLargeTexture =
                assetPath.EndsWith("night_results_bg.png", System.StringComparison.OrdinalIgnoreCase) ||
                assetPath.EndsWith("complete_reference_bg.png", System.StringComparison.OrdinalIgnoreCase) ||
                assetPath.EndsWith("booster_popup_frame.png", System.StringComparison.OrdinalIgnoreCase);
            importer.maxTextureSize = requiresLargeTexture ? 2048 : 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
