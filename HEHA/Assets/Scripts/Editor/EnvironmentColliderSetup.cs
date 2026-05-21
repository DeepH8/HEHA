#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace HEHA.Obby.Editor
{
    public static class EnvironmentColliderSetup
    {
        static readonly string[] DefaultStoreRoots =
        {
            "Assets/Store/Waldemarst",
            "Assets/Store/Unvik_3D",
        };

        [MenuItem("HEHA/Add Environment Colliders (Store Prefabs)")]
        public static void AddCollidersToStorePrefabs()
        {
            int prefabs = 0;
            int colliders = 0;

            foreach (string root in DefaultStoreRoots)
            {
                if (!AssetDatabase.IsValidFolder(root))
                    continue;

                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { root });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                    int added = AddCollidersToHierarchy(prefabRoot);
                    if (added > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                        prefabs++;
                        colliders += added;
                    }

                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"HEHA: Added {colliders} colliders across {prefabs} store prefabs.");
        }

        [MenuItem("HEHA/Add Environment Colliders (Open Scene)")]
        public static void AddCollidersToOpenScene()
        {
            int colliders = 0;
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                colliders += AddCollidersToHierarchy(root);

            Debug.Log($"HEHA: Added {colliders} colliders in the active scene.");
        }

        public static int AddCollidersToHierarchy(GameObject root)
        {
            if (root == null || IsPlayerHierarchy(root.transform))
                return 0;

            int added = 0;
            LODGroup[] lodGroups = root.GetComponentsInChildren<LODGroup>(true);
            foreach (LODGroup lodGroup in lodGroups)
            {
                if (IsPlayerHierarchy(lodGroup.transform))
                    continue;

                if (TryAddLodCapsule(lodGroup))
                    added++;
            }

            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                if (IsPlayerHierarchy(meshFilter.transform))
                    continue;

                if (meshFilter.GetComponent<BillboardRenderer>() != null)
                    continue;

                if (meshFilter.GetComponentInParent<LODGroup>() != null)
                    continue;

                if (HasCollider(meshFilter.gameObject))
                    continue;

                MeshCollider meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;
                added++;
            }

            return added;
        }

        static bool TryAddLodCapsule(LODGroup lodGroup)
        {
            if (HasCollider(lodGroup.gameObject))
                return false;

            CapsuleCollider capsule = lodGroup.gameObject.AddComponent<CapsuleCollider>();
            capsule.center = lodGroup.localReferencePoint;
            capsule.height = Mathf.Max(lodGroup.size, 0.5f);
            capsule.radius = Mathf.Max(lodGroup.size * 0.12f, 0.25f);
            capsule.direction = 1;
            return true;
        }

        static bool HasCollider(GameObject go)
        {
            Collider collider = go.GetComponent<Collider>();
            return collider != null && collider.enabled;
        }

        static bool IsPlayerHierarchy(Transform transform)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            while (transform != null)
            {
                if (transform.CompareTag("Player"))
                    return true;

                if (playerLayer >= 0 && transform.gameObject.layer == playerLayer)
                    return true;

                transform = transform.parent;
            }

            return false;
        }
    }
}
#endif
