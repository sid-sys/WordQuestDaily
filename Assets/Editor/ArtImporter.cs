using System.IO;
using UnityEditor;
using UnityEngine;

namespace WordQuest.EditorTools
{
    /// <summary>Imports everything under Assets/Resources/Art as a UI sprite, with 9-slice borders for buttons, panels and frames.</summary>
    public class ArtImporter : AssetPostprocessor
    {
        const string Root = "Assets/Resources/Art/";

        static bool IsPill(string n) => n.StartsWith("btn_") || n == "chip" || n == "track" || n == "fill" || n == "navbar";
        static bool IsBox(string n) => n == "popup" || n.StartsWith("card");
        static bool IsFrame(string n) => n.StartsWith("frame_");

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var ti = (TextureImporter)assetImporter;
            string name = Path.GetFileNameWithoutExtension(assetPath);

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.spritePixelsPerUnit = 100;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(settings);

            if (!(IsPill(name) || IsBox(name) || IsFrame(name))) { ti.spriteBorder = Vector4.zero; return; }
            if (!TryPngSize(assetPath, out int w, out int h)) return;
            if (IsPill(name))
            {
                float lr = h * 0.48f, tb = h * 0.34f;
                ti.spriteBorder = new Vector4(lr, tb, lr, tb);
            }
            else if (IsBox(name))
            {
                float b = Mathf.Min(w, h) * 0.28f;
                ti.spriteBorder = new Vector4(b, b, b, b);
            }
            else
            {
                float b = Mathf.Min(w, h) * 0.30f;
                ti.spriteBorder = new Vector4(b, b, b, b);
            }
        }

        static bool TryPngSize(string assetPath, out int w, out int h)
        {
            w = h = 0;
            try
            {
                using (var fs = File.OpenRead(assetPath))
                {
                    var b = new byte[24];
                    if (fs.Read(b, 0, 24) < 24) return false;
                    w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
                    h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
                    return w > 0 && h > 0;
                }
            }
            catch { return false; }
        }
    }
}
