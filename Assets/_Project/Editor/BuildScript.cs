using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WannabeSkattebo.EditorTools
{
    /// <summary>
    /// iOS builds from the menu or the command line:
    ///   Unity -batchmode -quit -projectPath . -buildTarget iOS -executeMethod WannabeSkattebo.EditorTools.BuildScript.BuildiOS [-simulator]
    /// Produces an Xcode project in Builds/iOS (device) or Builds/iOS-Simulator.
    /// </summary>
    public static class BuildScript
    {
        [MenuItem("Wannabe Skattebo/Build iOS (Simulator)")]
        public static void BuildiOSSimulator() => Build(simulator: true);

        [MenuItem("Wannabe Skattebo/Build iOS (Device)")]
        public static void BuildiOSDevice() => Build(simulator: false);

        public static void BuildiOS()
        {
            bool simulator = Environment.GetCommandLineArgs().Contains("-simulator");
            Build(simulator);
        }

        static void Build(bool simulator)
        {
            if (!File.Exists(ProjectSetup.ScenePath)) ProjectSetup.Setup();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

            PlayerSettings.iOS.sdkVersion = simulator ? iOSSdkVersion.SimulatorSDK : iOSSdkVersion.DeviceSDK;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = simulator ? "Builds/iOS-Simulator" : "Builds/iOS",
                target = BuildTarget.iOS,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Wannabe Skattebo] iOS build OK -> {options.locationPathName} ({summary.totalSize / (1024 * 1024)} MB)");
            }
            else
            {
                Debug.LogError($"[Wannabe Skattebo] iOS build failed: {summary.result}, {summary.totalErrors} error(s)");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
