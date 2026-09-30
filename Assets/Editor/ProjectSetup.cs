using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Android;
using UnityEngine;

namespace WordQuest.EditorTools
{
    /// <summary>One-click project settings for the Android release (Tools > Word Quest > Apply Project Settings).</summary>
    public static class ProjectSetup
    {
        [MenuItem("Tools/Word Quest/Apply Project Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = "Tetra Developers";
            PlayerSettings.productName = "Word Quest Daily";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.coreinteractive.wordquestdaily");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            ApplyIcons();
            AssetDatabase.SaveAssets();
            Debug.Log("Word Quest: project settings applied");
        }

        static void ApplyIcons()
        {
            var bg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Icons/icon_bg.png");
            var fg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Icons/icon_fg.png");
            if (bg == null || fg == null) { Debug.LogWarning("Icon layers not found"); return; }
            foreach (var path in new[] { "Assets/Art/Icons/icon_bg.png", "Assets/Art/Icons/icon_fg.png" })
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                if (ti != null && (ti.textureType != TextureImporterType.Default || ti.isReadable == false))
                {
                    ti.textureType = TextureImporterType.Default; ti.isReadable = true; ti.mipmapEnabled = false;
                    ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None;
                    ti.SaveAndReimport();
                }
            }
            bg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Icons/icon_bg.png");
            fg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Icons/icon_fg.png");
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { bg }, IconKind.Application);
            var adaptive = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive);
            foreach (var icon in adaptive)
            {
                icon.SetTexture(bg, 0);
                icon.SetTexture(fg, 1);
            }
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive, adaptive);
        }
    }
}
