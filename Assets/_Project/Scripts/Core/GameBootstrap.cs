using UnityEngine;

namespace WannabeSkattebo
{
    /// <summary>
    /// Creates the GameManager automatically after the first scene loads, so pressing Play
    /// in any scene (or launching the build) starts the game without manual scene wiring.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<GameManager>() != null) return;
            var go = new GameObject("Game");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameManager>();
        }
    }
}
