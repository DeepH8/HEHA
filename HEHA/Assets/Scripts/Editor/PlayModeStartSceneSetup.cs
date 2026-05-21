#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HEHA.Obby.Editor
{
    [InitializeOnLoad]
    static class PlayModeStartSceneSetup
    {
        static PlayModeStartSceneSetup()
        {
            EditorApplication.delayCall += Apply;
        }

        static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            MainMenuSceneBuilder.ApplyPlayModeAndBuildSettings();
        }
    }
}
#endif
