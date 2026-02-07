using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

namespace AIBeat.Editor
{
    public static class AndroidBuilder
    {
        [MenuItem("Tools/A.I. BEAT/Check Android Module")]
        public static void CheckAndroidModule()
        {
            bool hasAndroid = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
            Debug.Log($"[AndroidBuilder] Android module installed: {hasAndroid}");
            Debug.Log($"[AndroidBuilder] Current build target: {EditorUserBuildSettings.activeBuildTarget}");
            Debug.Log($"[AndroidBuilder] Unity version: {Application.unityVersion}");
        }

        [MenuItem("Tools/A.I. BEAT/Build Android APK")]
        public static void BuildAndroidAPK()
        {
            // Android 모듈 확인
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[AndroidBuilder] Android Build Support module is NOT installed! Please install it via Unity Hub.");
                return;
            }

            // 빌드 타겟 확인 및 전환
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[AndroidBuilder] Switching build target to Android...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // 빌드 씬 목록
            string[] scenes = new string[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/SongSelect.unity",
                "Assets/Scenes/Gameplay.unity"
            };

            // 출력 경로
            string buildFolder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "Android");
            if (!Directory.Exists(buildFolder))
                Directory.CreateDirectory(buildFolder);

            string apkPath = Path.Combine(buildFolder, "AIBeat.apk");

            // 빌드 옵션
            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log($"[AndroidBuilder] Starting Android APK build...");
            Debug.Log($"[AndroidBuilder] Output: {apkPath}");

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                long sizeBytes = new FileInfo(apkPath).Length;
                float sizeMB = sizeBytes / (1024f * 1024f);
                Debug.Log($"[AndroidBuilder] BUILD SUCCEEDED! APK size: {sizeMB:F1} MB");
                Debug.Log($"[AndroidBuilder] APK path: {apkPath}");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[AndroidBuilder] BUILD FAILED! Errors: {summary.totalErrors}");
            }
        }
    }
}
