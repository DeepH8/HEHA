using System.Collections.Generic;
using UnityEngine;

namespace HEHA.Obby.Player
{
    public class RagdollDisassembler : MonoBehaviour
    {
        [SerializeField] List<LimbPart> limbs = new List<LimbPart>();
        [SerializeField] float explosionForce = 6f;
        [SerializeField] float explosionTorque = 4f;
        [SerializeField] Transform visualRoot;
        [SerializeField] bool useHumanoidRagdoll;

        static readonly List<GameObject> OrphanedParts = new List<GameObject>();

        [System.Serializable]
        public class LimbPart
        {
            public Transform transform;
            public Rigidbody rigidbody;
            public Collider collider;
        }

        void Awake()
        {
            if (limbs.Count == 0 && visualRoot != null)
                AutoDiscoverLimbs();

            foreach (LimbPart limb in limbs)
                SetLimbPhysics(limb, false);
        }

        void AutoDiscoverLimbs()
        {
            MeshFilter[] filters = visualRoot.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter filter in filters)
            {
                if (filter.sharedMesh == null)
                    continue;

                GameObject part = filter.gameObject;
                if (part.GetComponent<MeshRenderer>() == null)
                    continue;

                Rigidbody rb = part.GetComponent<Rigidbody>();
                if (rb == null)
                    rb = part.AddComponent<Rigidbody>();

                Collider col = part.GetComponent<MeshCollider>();
                if (col == null)
                {
                    foreach (Collider existing in part.GetComponents<Collider>())
                    {
                        if (existing is MeshCollider meshColliderExisting)
                        {
                            col = meshColliderExisting;
                            continue;
                        }

                        if (Application.isPlaying)
                            Destroy(existing);
                        else
                            DestroyImmediate(existing);
                    }

                    MeshCollider mc = part.AddComponent<MeshCollider>();
                    mc.sharedMesh = filter.sharedMesh;
                    mc.convex = true;
                    col = mc;
                }

                limbs.Add(new LimbPart
                {
                    transform = part.transform,
                    rigidbody = rb,
                    collider = col
                });
            }
        }

        public void Explode()
        {
            Animator animator = visualRoot != null ? visualRoot.GetComponentInChildren<Animator>() : null;
            if (animator != null)
                animator.enabled = false;

            if (!useHumanoidRagdoll && visualRoot != null)
                visualRoot.gameObject.SetActive(false);

            foreach (LimbPart limb in limbs)
            {
                if (limb.transform == null)
                    continue;

                limb.transform.SetParent(null, true);
                SetLimbPhysics(limb, true);

                Vector3 randomDir = Random.onUnitSphere;
                randomDir.y = Mathf.Abs(randomDir.y) * 0.5f + 0.5f;
                limb.rigidbody.AddForce(randomDir * explosionForce, ForceMode.Impulse);
                limb.rigidbody.AddTorque(Random.insideUnitSphere * explosionTorque, ForceMode.Impulse);

                OrphanedParts.Add(limb.transform.gameObject);
            }
        }

        public static void CleanupOrphanedParts()
        {
            for (int i = OrphanedParts.Count - 1; i >= 0; i--)
            {
                if (OrphanedParts[i] != null)
                    Destroy(OrphanedParts[i]);

                OrphanedParts.RemoveAt(i);
            }
        }

        static void SetLimbPhysics(LimbPart limb, bool enabled)
        {
            if (limb.rigidbody != null)
            {
                limb.rigidbody.isKinematic = !enabled;
                limb.rigidbody.useGravity = enabled;
            }

            if (limb.collider != null)
                limb.collider.enabled = enabled;
        }
    }
}
