#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace HEHA.Obby.Editor
{
    [InitializeOnLoad]
    static class ObbyAutoBuild
    {
        const string PlayerPrefabPath = "Assets/Prefabs/Player/PlayerR6.prefab";

        static ObbyAutoBuild()
        {
            EditorApplication.delayCall += TryBuild;
        }

        static void TryBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (File.Exists(PlayerPrefabPath))
                return;

            ObbyPrefabBuilder.BuildAll();
        }
    }
}
#endif
