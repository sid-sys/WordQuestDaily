using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WordQuest.EditorTools
{
    /// <summary>
    /// Android builds. The signing password is read from the KEYSTORE_PASS environment variable or from
    /// Keystore/KEYSTORE-INFO.txt (a git-ignored file), and is never written into the project.
    /// </summary>
    public static class BuildTool
    {
        const string KeystoreName = "wordquest-upload.keystore", Alias = "wordquest";

        static string Root => Directory.GetParent(Application.dataPath).FullName;

        static string ReadPassword()
        {
            var env = Environment.GetEnvironmentVariable("KEYSTORE_PASS");
            if (!string.IsNullOrEmpty(env)) return env;
            var info = Path.Combine(Root, "Keystore", "KEYSTORE-INFO.txt");
            if (File.Exists(info))
                foreach (var line in File.ReadAllLines(info))
                    if (line.StartsWith("Password:", StringComparison.OrdinalIgnoreCase)) return line.Substring(9).Trim();
            return null;
        }

        /// <summary>release = signed AAB for Google Play. Otherwise a signed development APK for a phone.</summary>
        public static string Build(bool bundle, int versionCode, string version)
        {
            string pass = ReadPassword();
            if (string.IsNullOrEmpty(pass)) return "NO PASSWORD";
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.Combine(Root, "Keystore", KeystoreName);
            PlayerSettings.Android.keystorePass = pass;
            PlayerSettings.Android.keyaliasName = Alias;
            PlayerSettings.Android.keyaliasPass = pass;
            PlayerSettings.Android.bundleVersionCode = versionCode;
            PlayerSettings.bundleVersion = version;
            EditorUserBuildSettings.buildAppBundle = bundle;
            EditorUserBuildSettings.development = !bundle;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            Directory.CreateDirectory(Path.Combine(Root, "Builds"));
            string path = Path.Combine(Root, "Builds", bundle ? $"WordQuestDaily-v{versionCode}.aab" : "WordQuestDaily-dev.apk");
            var scenes = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);
            if (scenes.Count == 0) scenes.Add("Assets/Scenes/SampleScene.unity");
            var opts = new BuildPlayerOptions { scenes = scenes.ToArray(), locationPathName = path, target = BuildTarget.Android, options = bundle ? BuildOptions.None : BuildOptions.Development };
            var report = BuildPipeline.BuildPlayer(opts);
            // wipe the password from the in-memory settings
            PlayerSettings.Android.keystorePass = ""; PlayerSettings.Android.keyaliasPass = "";
            return report.summary.result + " " + (report.summary.totalSize / (1024 * 1024)) + " MB -> " + path + " errors=" + report.summary.totalErrors;
        }

        [MenuItem("Tools/Word Quest/Build Release AAB")]
        public static void ReleaseMenu() { Debug.Log(Build(true, PlayerSettings.Android.bundleVersionCode, PlayerSettings.bundleVersion)); }
    }
}
