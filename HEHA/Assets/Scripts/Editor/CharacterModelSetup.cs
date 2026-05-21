#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using HEHA.Obby.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HEHA.Obby.Editor
{
    public static class CharacterModelSetup
    {
        public const string ModelPath = "Assets/Models/Character/Ch14_nonPBR.fbx";
        public const string AnimatorControllerPath = "Assets/Animations/PlayerAnimator.controller";
        public const string BodyMaterialPath = "Assets/Materials/Character/Ch14_Body.mat";
        public const string DiffuseTexturePath = "Assets/Models/Character/Ch14_1001_Diffuse.png";
        public const string NormalTexturePath = "Assets/Models/Character/Ch14_1001_Normal.png";

        static readonly (string path, string clipName)[] AnimationFiles =
        {
            ("Assets/Models/Character/Ch14_nonPBR@Standing W_Briefcase Idle.fbx", "Idle"),
            ("Assets/Models/Character/Ch14_nonPBR@Walking.fbx", "Walk"),
            ("Assets/Models/Character/Ch14_nonPBR@Running.fbx", "Run"),
            ("Assets/Models/Character/Ch14_nonPBR@Jumping Up.fbx", "Jump"),
            ("Assets/Models/Character/Ch14_nonPBR@Falling Idle.fbx", "Fall"),
        };

        public static bool IsAvailable =>
            File.Exists(Path.Combine(Application.dataPath, "Models/Character/Ch14_nonPBR.fbx"));

        [MenuItem("HEHA/Reimport Character Animations")]
        public static void ReimportAnimationsOnly()
        {
            ConfigureAllImporters();
            ApplyAllClipLoops();
            GetOrCreateAnimatorController();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("HEHA: Character animations reimported.");
        }

        [MenuItem("HEHA/Fix Character Materials")]
        public static void FixCharacterMaterialsMenu()
        {
            if (!IsAvailable)
            {
                Debug.LogWarning("HEHA: Ch14 model not found.");
                return;
            }

            ConfigureAllImporters();
            GetOrCreateBodyMaterial();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("HEHA: Character materials fixed.");
        }

        public static void ConfigureAllImporters()
        {
            if (!IsAvailable)
                return;

            ConfigureModelImporter(ModelPath, importAnimation: false);
            ConfigureCharacterTextures();
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
            GetOrCreateBodyMaterial();
            RemapModelMaterial(ModelPath);

            Avatar sourceAvatar = GetAvatarFromModel(ModelPath);
            foreach ((string path, string clipName) in AnimationFiles)
            {
                if (!File.Exists(ToFullPath(path)))
                    continue;

                ConfigureAnimationImporter(path, sourceAvatar, clipName);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        static string ToFullPath(string assetPath) =>
            Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));

        static void ConfigureModelImporter(string path, bool importAnimation)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                return;

            importer.animationType = (ModelImporterAnimationType)3;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = importAnimation;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.materialName = ModelImporterMaterialName.BasedOnTextureName;
            importer.materialSearch = ModelImporterMaterialSearch.RecursiveUp;
            importer.SaveAndReimport();
        }

        static void ConfigureCharacterTextures()
        {
            ConfigureTextureImporter(NormalTexturePath, TextureImporterType.NormalMap);
            ConfigureTextureImporter(DiffuseTexturePath, TextureImporterType.Default);
        }

        static void ConfigureTextureImporter(string assetPath, TextureImporterType type)
        {
            if (!File.Exists(ToFullPath(assetPath)))
                return;

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = type;
            importer.sRGBTexture = type != TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }

        public static Material GetOrCreateBodyMaterial()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Materials/Character"));

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, BodyMaterialPath);
            }
            else
            {
                mat.shader = shader;
            }

            Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(DiffuseTexturePath);
            if (diffuse == null)
                diffuse = FindEmbeddedTexture("diffuse", "albedo", "color");

            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTexturePath);
            if (normal == null)
                normal = FindEmbeddedTexture("normal", "nrm");

            if (diffuse != null)
            {
                mat.SetTexture("_BaseMap", diffuse);
                mat.SetColor("_BaseColor", Color.white);
            }

            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }

            mat.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Texture2D FindEmbeddedTexture(params string[] nameHints)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
            Texture2D fallback = null;
            foreach (Object asset in assets)
            {
                if (asset is not Texture2D texture || texture.name.StartsWith("__"))
                    continue;

                string name = texture.name.ToLowerInvariant();
                foreach (string hint in nameHints)
                {
                    if (name.Contains(hint))
                        return texture;
                }

                fallback ??= texture;
            }

            return fallback;
        }

        static void RemapModelMaterial(string modelPath)
        {
            Material bodyMaterial = GetOrCreateBodyMaterial();
            Material sourceMaterial = null;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is Material material && !material.name.StartsWith("__"))
                {
                    sourceMaterial = material;
                    break;
                }
            }

            if (sourceMaterial == null)
                return;

            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
                return;

            var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceMaterial.name);
            importer.AddRemap(id, bodyMaterial);
            AssetDatabase.WriteImportSettingsIfDirty(modelPath);
            importer.SaveAndReimport();
        }

        static void ApplyBodyMaterial(GameObject model)
        {
            Material bodyMaterial = GetOrCreateBodyMaterial();
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = bodyMaterial;
                renderer.sharedMaterials = mats;
            }
        }

        static void ConfigureAnimationImporter(string path, Avatar sourceAvatar, string clipLabel)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                return;

            importer.animationType = (ModelImporterAnimationType)3;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = sourceAvatar;
            importer.importAnimation = true;
            importer.importConstraints = false;
            importer.clipAnimations = System.Array.Empty<ModelImporterClipAnimation>();
            importer.SaveAndReimport();

            AnimationClip clip = LoadClip(path, clipLabel);
            if (clip != null)
                ApplyLoopSetting(clip, ShouldLoop(clipLabel));
        }

        static bool ShouldLoop(string clipLabel) =>
            clipLabel is "Idle" or "Walk" or "Run" or "Fall";

        static void ApplyAllClipLoops()
        {
            foreach ((string path, string clipName) in AnimationFiles)
            {
                AnimationClip clip = LoadClip(path, clipName);
                if (clip != null)
                    ApplyLoopSetting(clip, ShouldLoop(clipName));
            }
        }

        static void ApplyLoopSetting(AnimationClip clip, bool loop)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.loopBlend = loop;
            settings.loopBlendOrientation = loop;
            settings.loopBlendPositionY = loop;
            settings.loopBlendPositionXZ = loop;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
            EditorUtility.SetDirty(clip);
        }

        static Avatar GetAvatarFromModel(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is Avatar avatar)
                    return avatar;
            }

            return null;
        }

        static AnimationClip LoadClip(string path, string clipLabel)
        {
            AnimationClip best = null;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is not AnimationClip clip || clip.name.StartsWith("__") || clip.length < 0.001f)
                    continue;

                if (clip.name == clipLabel || clip.name.Contains(clipLabel))
                    return clip;

                best ??= clip;
            }

            if (best == null)
                Debug.LogWarning($"CharacterModelSetup: No animation clip with frames in {path}");

            return best;
        }

        public static AnimatorController GetOrCreateAnimatorController()
        {
            ConfigureAllImporters();

            AnimationClip idle = LoadClip(AnimationFiles[0].path, "Idle");
            AnimationClip walk = LoadClip(AnimationFiles[1].path, "Walk");
            AnimationClip run = LoadClip(AnimationFiles[2].path, "Run");
            AnimationClip jump = LoadClip(AnimationFiles[3].path, "Jump");
            AnimationClip fall = LoadClip(AnimationFiles[4].path, "Fall");

            ApplyLoopSetting(idle, true);
            ApplyLoopSetting(walk, true);
            ApplyLoopSetting(run, true);
            ApplyLoopSetting(fall, true);
            if (jump != null)
                ApplyLoopSetting(jump, false);

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Animations"));

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath) != null)
                AssetDatabase.DeleteAsset(AnimatorControllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsSprinting", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            AnimatorState idleState = sm.AddState("Idle", new Vector3(300f, 0f, 0f));
            idleState.motion = idle;
            sm.defaultState = idleState;

            AnimatorState walkState = sm.AddState("Walk", new Vector3(300f, 100f, 0f));
            walkState.motion = walk;

            AnimatorState runState = sm.AddState("Run", new Vector3(300f, 200f, 0f));
            runState.motion = run;

            AnimatorState jumpState = sm.AddState("Jump", new Vector3(550f, 100f, 0f));
            jumpState.motion = jump;

            AnimatorState fallState = sm.AddState("Fall", new Vector3(550f, 200f, 0f));
            fallState.motion = fall;

            AddTransition(idleState, walkState, AnimatorConditionMode.Greater, 0.1f, "Speed", false, true);
            AddTransition(walkState, idleState, AnimatorConditionMode.Less, 0.1f, "Speed");
            AddTransition(walkState, runState, AnimatorConditionMode.If, 0f, "IsSprinting");
            AddTransition(runState, walkState, AnimatorConditionMode.IfNot, 0f, "IsSprinting");
            AddTransition(runState, idleState, AnimatorConditionMode.Less, 0.1f, "Speed");
            AddTransition(walkState, idleState, AnimatorConditionMode.IfNot, 0f, "IsGrounded", true);

            AddTriggerTransition(sm, jumpState, "Jump");
            AddTransition(jumpState, fallState, AnimatorConditionMode.IfNot, 0f, "IsGrounded", true);
            AddTransition(fallState, idleState, AnimatorConditionMode.If, 0f, "IsGrounded", true);
            AddTransition(idleState, fallState, AnimatorConditionMode.IfNot, 0f, "IsGrounded", true);
            AddTransition(walkState, fallState, AnimatorConditionMode.IfNot, 0f, "IsGrounded", true);
            AddTransition(runState, fallState, AnimatorConditionMode.IfNot, 0f, "IsGrounded", true);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        static void AddTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold,
            string param, bool boolValue = false, bool requireGrounded = false)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.1f;
            if (mode == AnimatorConditionMode.If || mode == AnimatorConditionMode.IfNot)
                t.AddCondition(mode, 0f, param);
            else
                t.AddCondition(mode, threshold, param);

            if (requireGrounded)
                t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
        }

        static void AddTriggerTransition(AnimatorStateMachine sm, AnimatorState to, string trigger)
        {
            AnimatorStateTransition t = sm.AddAnyStateTransition(to);
            t.hasExitTime = false;
            t.duration = 0.05f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        public static bool TryAttachToPlayer(GameObject root, CharacterController controller, Transform visualRoot,
            RagdollDisassembler ragdoll, out float cameraPivotHeight)
        {
            cameraPivotHeight = 1.4f;
            ConfigureAllImporters();

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (source == null)
                return false;

            GameObject model = Object.Instantiate(source, visualRoot);
            model.name = "CharacterModel";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            ApplyBodyMaterial(model);
            ApplyLayerRecursive(model, root.layer);
            FitHumanoidToCharacterController(model, controller, out cameraPivotHeight);

            Animator animator = model.GetComponent<Animator>();
            if (animator == null)
                animator = model.AddComponent<Animator>();

            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = GetOrCreateAnimatorController();

            root.AddComponent<PlayerAnimationController>();
            PopulateHumanoidRagdoll(model, ragdoll, visualRoot.transform, animator);

            return true;
        }

        static void ApplyLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                ApplyLayerRecursive(child.gameObject, layer);
        }

        static void FitHumanoidToCharacterController(GameObject model, CharacterController controller,
            out float cameraPivotHeight)
        {
            Bounds bounds = CalculateRendererBounds(model);
            if (bounds.size.sqrMagnitude < 0.0001f)
            {
                cameraPivotHeight = controller.height * 0.75f;
                return;
            }

            float targetHeight = controller.height;
            float scale = targetHeight / bounds.size.y;
            model.transform.localScale = Vector3.one * scale;

            bounds = CalculateRendererBounds(model);
            Vector3 position = model.transform.localPosition;
            position.x = -bounds.center.x;
            position.z = -bounds.center.z;
            position.y = -bounds.min.y;
            model.transform.localPosition = position;

            cameraPivotHeight = bounds.max.y * 0.85f;
        }

        static Bounds CalculateRendererBounds(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(model.transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        static void PopulateHumanoidRagdoll(GameObject model, RagdollDisassembler ragdoll, Transform visualRoot,
            Animator animator)
        {
            var limbs = new List<RagdollDisassembler.LimbPart>();
            HumanBodyBones[] bones =
            {
                HumanBodyBones.Hips,
                HumanBodyBones.Spine,
                HumanBodyBones.Head,
                HumanBodyBones.LeftUpperArm,
                HumanBodyBones.RightUpperArm,
                HumanBodyBones.LeftLowerArm,
                HumanBodyBones.RightLowerArm,
                HumanBodyBones.LeftUpperLeg,
                HumanBodyBones.RightUpperLeg,
                HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.RightLowerLeg,
            };

            foreach (HumanBodyBones bone in bones)
            {
                Transform boneTransform = animator.GetBoneTransform(bone);
                if (boneTransform == null || boneTransform == animator.transform)
                    continue;

                GameObject part = boneTransform.gameObject;
                Rigidbody rb = part.GetComponent<Rigidbody>();
                if (rb == null)
                    rb = part.AddComponent<Rigidbody>();

                rb.isKinematic = true;
                rb.useGravity = false;

                CapsuleCollider col = part.GetComponent<CapsuleCollider>();
                if (col == null)
                    col = part.AddComponent<CapsuleCollider>();

                col.enabled = false;
                col.radius = 0.08f;
                col.height = 0.25f;
                col.direction = 1;

                limbs.Add(new RagdollDisassembler.LimbPart
                {
                    transform = boneTransform,
                    rigidbody = rb,
                    collider = col
                });
            }

            SerializedObject so = new SerializedObject(ragdoll);
            so.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            so.FindProperty("useHumanoidRagdoll").boolValue = true;
            SerializedProperty limbProp = so.FindProperty("limbs");
            limbProp.arraySize = limbs.Count;
            for (int i = 0; i < limbs.Count; i++)
            {
                SerializedProperty element = limbProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("transform").objectReferenceValue = limbs[i].transform;
                element.FindPropertyRelative("rigidbody").objectReferenceValue = limbs[i].rigidbody;
                element.FindPropertyRelative("collider").objectReferenceValue = limbs[i].collider;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
