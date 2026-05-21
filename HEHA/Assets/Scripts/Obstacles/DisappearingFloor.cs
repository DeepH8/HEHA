using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace HEHA.Obby.Obstacles
{
    public class DisappearingFloor : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        static readonly int BlendId = Shader.PropertyToID("_Blend");
        static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

        [SerializeField] float holdBeforeDissolve = 0.4f;
        [SerializeField] float dissolveDuration = 0.75f;
        [SerializeField] float regenerateDelay = 3f;
        [SerializeField] bool regenerate = true;
        [SerializeField] Collider floorCollider;
        [SerializeField] Renderer floorRenderer;
        [SerializeField] Collider stepTrigger;

        bool activated;
        Material fadeMaterial;
        Color baseColor;
        Vector3 baseScale;

        void Awake()
        {
            if (floorCollider == null)
                floorCollider = GetComponent<Collider>();

            if (floorRenderer == null)
                floorRenderer = GetComponent<Renderer>();

            baseScale = transform.localScale;
            EnsureStepTrigger();

            if (floorRenderer != null)
            {
                fadeMaterial = floorRenderer.material;
                baseColor = fadeMaterial.HasProperty(BaseColorId)
                    ? fadeMaterial.GetColor(BaseColorId)
                    : fadeMaterial.color;
            }
        }

        void EnsureStepTrigger()
        {
            if (stepTrigger != null)
            {
                ConfigureStepTrigger(stepTrigger.gameObject);
                return;
            }

            Transform existing = transform.Find("StepTrigger");
            if (existing != null)
            {
                stepTrigger = existing.GetComponent<Collider>();
                if (stepTrigger != null)
                {
                    ConfigureStepTrigger(existing.gameObject);
                    return;
                }
            }

            GameObject triggerGo = new GameObject("StepTrigger");
            triggerGo.transform.SetParent(transform, false);
            triggerGo.transform.localPosition = new Vector3(0f, 0.55f, 0f);

            BoxCollider box = triggerGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.05f, 0.35f, 1.05f);
            stepTrigger = box;
            ConfigureStepTrigger(triggerGo);
        }

        void ConfigureStepTrigger(GameObject triggerGo)
        {
            Rigidbody rb = triggerGo.GetComponent<Rigidbody>();
            if (rb == null)
                rb = triggerGo.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.useGravity = false;

            DisappearingFloorStepDetector detector = triggerGo.GetComponent<DisappearingFloorStepDetector>();
            if (detector == null)
                detector = triggerGo.AddComponent<DisappearingFloorStepDetector>();

            detector.Bind(this);
        }

        public void ActivateFromPlayer()
        {
            if (activated)
                return;

            activated = true;
            StartCoroutine(DissolveRoutine());
        }

        public void NotifyPlayerStepped(Collider other)
        {
            if (activated || other == null)
                return;

            if (!IsPlayer(other))
                return;

            ActivateFromPlayer();
        }

        static bool IsPlayer(Collider other)
        {
            if (other.CompareTag("Player"))
                return true;

            return other.GetComponent<CharacterController>() != null
                || other.GetComponentInParent<CharacterController>() != null;
        }

        IEnumerator DissolveRoutine()
        {
            if (stepTrigger != null)
                stepTrigger.enabled = false;

            yield return new WaitForSeconds(holdBeforeDissolve);

            SetupFadeMaterial();

            float elapsed = 0f;
            while (elapsed < dissolveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dissolveDuration);
                float alpha = 1f - t;
                Vector3 scale = Vector3.Lerp(baseScale, new Vector3(baseScale.x, 0.02f, baseScale.z), t);

                transform.localScale = scale;
                ApplyAlpha(alpha);
                yield return null;
            }

            SetFloorVisible(false);

            if (regenerate)
            {
                yield return new WaitForSeconds(regenerateDelay);
                ResetFloor();
            }
        }

        void ResetFloor()
        {
            transform.localScale = baseScale;
            ApplyAlpha(1f);

            if (floorCollider != null)
                floorCollider.enabled = true;

            if (floorRenderer != null)
                floorRenderer.enabled = true;

            if (stepTrigger != null)
                stepTrigger.enabled = true;

            activated = false;
        }

        void SetupFadeMaterial()
        {
            if (fadeMaterial == null)
                return;

            fadeMaterial.SetFloat(SurfaceId, 1f);
            fadeMaterial.SetFloat(BlendId, 0f);
            fadeMaterial.SetInt(SrcBlendId, (int)BlendMode.SrcAlpha);
            fadeMaterial.SetInt(DstBlendId, (int)BlendMode.OneMinusSrcAlpha);
            fadeMaterial.SetInt(ZWriteId, 0);
            fadeMaterial.renderQueue = (int)RenderQueue.Transparent;
            fadeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        void ApplyAlpha(float alpha)
        {
            if (fadeMaterial == null)
                return;

            Color c = baseColor;
            c.a = alpha;
            fadeMaterial.SetColor(BaseColorId, c);
            fadeMaterial.color = c;
        }

        void SetFloorVisible(bool visible)
        {
            if (floorCollider != null)
                floorCollider.enabled = visible;

            if (floorRenderer != null)
                floorRenderer.enabled = visible;
        }
    }
}
