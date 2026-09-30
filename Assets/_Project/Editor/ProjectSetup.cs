using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WannabeSkattebo.EditorTools
{
    /// <summary>
    /// One-click (and first-open automatic) project setup: creates the run scene, registers it
    /// in Build Settings and applies iOS player settings. The game itself is built from code
    /// by GameBootstrap, so the scene only needs the default camera + light.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Run_Backyard.unity";
        public const string BundleId = "com.upraderes.wannabeskattebo";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath) && !Application.isBatchMode) Setup();
            };
        }

        [MenuItem("Wannabe Skattebo/Setup Project")]
        public static void Setup()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            Debug.Log("[Wannabe Skattebo] Project set up. Press Play to run the backyard.");
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Wannabe Studio";
            PlayerSettings.productName = "Wannabe Skattebo";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.statusBarHidden = true;
        }
    }
}
