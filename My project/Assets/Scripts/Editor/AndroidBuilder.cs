using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

namespace AIBeat.Editor
{
    public static class AndroidBuilder
    {
        private static readonly string BuildLogPath = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName, "build_result.txt");

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
            File.WriteAllText(BuildLogPath, "BUILDING...\n");
            // Android 모듈 확인
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                string msg = "[AndroidBuilder] Android Build Support module is NOT installed!";
                Debug.LogError(msg);
                File.WriteAllText(BuildLogPath, $"FAILED\n{msg}\n");
                return;
            }

            // 빌드 전 PlayerSettings 강제 적용
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.useAnimatedAutorotation = false;

            // 외부 저장소 읽기 권한 (Suno AI MP3 스캔용)
            PlayerSettings.Android.forceInternetPermission = true;
            // Write Access를 External(SDCard)로 설정 → READ_EXTERNAL_STORAGE 권한 자동 추가
            PlayerSettings.Android.forceSDCardPermission = true;

            Debug.Log("[AndroidBuilder] PlayerSettings: Portrait only, External storage access enabled");

            // 빌드 타겟 확인 및 전환
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[AndroidBuilder] Switching build target to Android...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // 빌드 씬 목록 (Build Settings 순서와 동일)
            string[] scenes = new string[]
            {
                "Assets/Scenes/SplashScene.unity",
                "Assets/Scenes/MainMenuScene.unity",
                "Assets/Scenes/SongSelectScene.unity",
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
                options = BuildOptions.None // Release 빌드 (Development/Debugging 제거)
            };

            Debug.Log($"[AndroidBuilder] Starting Android APK build...");
            Debug.Log($"[AndroidBuilder] Output: {apkPath}");

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                long sizeBytes = new FileInfo(apkPath).Length;
                float sizeMB = sizeBytes / (1024f * 1024f);
                string successMsg = $"BUILD SUCCEEDED! APK size: {sizeMB:F1} MB\nPath: {apkPath}";
                Debug.Log($"[AndroidBuilder] {successMsg}");
                File.WriteAllText(BuildLogPath, $"SUCCESS\n{successMsg}\n");
            }
            else if (summary.result == BuildResult.Failed)
            {
                string failMsg = $"BUILD FAILED! Errors: {summary.totalErrors}";
                Debug.LogError($"[AndroidBuilder] {failMsg}");
                File.WriteAllText(BuildLogPath, $"FAILED\n{failMsg}\n");
            }
        }
    }
}
