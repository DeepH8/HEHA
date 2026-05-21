#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HEHA.Obby.Editor
{
    public static class UrpMaterialConverter
    {
        const string UrpLitShaderName = "Universal Render Pipeline/Lit";

        static readonly string[] DefaultConvertRoots =
        {
            "Assets/Store/Waldemarst",
            "Assets/Store/Unvik_3D",
            "Assets/Waldemarst",
            "Assets/Unvik_3D",
        };

        [MenuItem("HEHA/Convert Materials To URP")]
        public static void ConvertImportedAssets()
        {
            ConvertMaterialsInFolders(DefaultConvertRoots, skipProjectMaterials: true);
        }

        [MenuItem("HEHA/Convert All Project Materials To URP")]
        public static void ConvertAllProjectMaterials()
        {
            ConvertMaterialsInFolders(new[] { "Assets" }, skipProjectMaterials: true);
        }

        static void ConvertMaterialsInFolders(string[] roots, bool skipProjectMaterials)
        {
            Shader urpLit = Shader.Find(UrpLitShaderName);
            if (urpLit == null)
            {
                Debug.LogError("HEHA: Could not find URP Lit shader. Is Universal RP installed?");
                return;
            }

            int converted = 0;
            int skipped = 0;

            foreach (string root in roots)
            {
                string[] guids = AssetDatabase.FindAssets("t:Material", new[] { root });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (skipProjectMaterials && IsHehaGameplayMaterial(path))
                    {
                        skipped++;
                        continue;
                    }

                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                        continue;

                    if (TryConvertMaterial(material, urpLit))
                    {
                        EditorUtility.SetDirty(material);
                        converted++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"HEHA: Converted {converted} materials to URP. Skipped {skipped}.");
        }

        static bool IsHehaGameplayMaterial(string path) =>
            path.StartsWith("Assets/Materials/") && !path.Contains("/Character/");

        public static bool TryConvertMaterial(Material material, Shader urpLit)
        {
            if (material == null || urpLit == null)
                return false;

            if (IsUrpShader(material.shader))
                return false;

            if (IsSkyboxMaterial(material))
                return TryConvertSkybox(material);

            if (IsFoliageMaterial(material))
                return ConvertFoliageToUrpLit(material, urpLit);

            return ConvertStandardToUrpLit(material, urpLit);
        }

        static bool IsUrpShader(Shader shader)
        {
            if (shader == null)
                return false;

            string name = shader.name;
            return name.StartsWith("Universal Render Pipeline/")
                || name.StartsWith("Shader Graphs/")
                || name.StartsWith("HDRP/");
        }

        static bool IsSkyboxMaterial(Material material)
        {
            string shaderName = material.shader != null ? material.shader.name : string.Empty;
            return shaderName.Contains("Skybox") || material.name.ToLowerInvariant().Contains("skybox");
        }

        static bool IsFoliageMaterial(Material material)
        {
            string shaderName = material.shader != null ? material.shader.name : string.Empty;
            return shaderName.Contains("Nature/Tree") || shaderName.Contains("Nature/SpeedTree");
        }

        static bool TryConvertSkybox(Material material)
        {
            Shader skyShader = Shader.Find("Universal Render Pipeline/Skybox/Procedural")
                ?? Shader.Find("Skybox/Procedural");

            if (skyShader == null)
            {
                Debug.LogWarning($"HEHA: No procedural skybox shader found for '{material.name}'.");
                return false;
            }

            material.shader = skyShader;
            return true;
        }

        static bool ConvertFoliageToUrpLit(Material material, Shader urpLit)
        {
            Texture mainTex = GetMainTexture(material);
            Color color = GetMainColor(material);
            float cutoff = material.HasProperty("_Cutoff") ? material.GetFloat("_Cutoff") : 0.5f;

            material.shader = urpLit;
            ApplyOpaqueSurface(material);

            if (mainTex != null)
                material.SetTexture("_BaseMap", mainTex);

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Cutoff", cutoff);
            material.SetFloat("_AlphaClip", 1f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.SetOverrideTag("RenderType", "TransparentCutout");
            return true;
        }

        static bool ConvertStandardToUrpLit(Material material, Shader urpLit)
        {
            Texture mainTex = GetMainTexture(material);
            Texture bumpMap = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;
            Texture metallicMap = material.HasProperty("_MetallicGlossMap")
                ? material.GetTexture("_MetallicGlossMap")
                : null;
            Color color = GetMainColor(material);
            float smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.5f;
            float metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;
            float mode = material.HasProperty("_Mode") ? material.GetFloat("_Mode") : 0f;
            float cutoff = material.HasProperty("_Cutoff") ? material.GetFloat("_Cutoff") : 0.5f;

            material.shader = urpLit;

            if (mainTex != null)
                material.SetTexture("_BaseMap", mainTex);

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);

            if (bumpMap != null)
            {
                material.SetTexture("_BumpMap", bumpMap);
                material.EnableKeyword("_NORMALMAP");
            }

            if (metallicMap != null)
                material.SetTexture("_MetallicGlossMap", metallicMap);

            if (Mathf.Approximately(mode, 1f))
                ApplyCutoutSurface(material, cutoff);
            else if (mode >= 2.5f)
                ApplyTransparentSurface(material);
            else
                ApplyOpaqueSurface(material);

            return true;
        }

        static Texture GetMainTexture(Material material)
        {
            if (material.HasProperty("_MainTex"))
            {
                Texture tex = material.GetTexture("_MainTex");
                if (tex != null)
                    return tex;
            }

            if (material.HasProperty("_BaseMap"))
                return material.GetTexture("_BaseMap");

            return null;
        }

        static Color GetMainColor(Material material)
        {
            if (material.HasProperty("_Color"))
                return material.GetColor("_Color");

            if (material.HasProperty("_BaseColor"))
                return material.GetColor("_BaseColor");

            return Color.white;
        }

        static void ApplyOpaqueSurface(Material material)
        {
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = -1;
            material.SetOverrideTag("RenderType", "Opaque");
        }

        static void ApplyCutoutSurface(Material material, float cutoff)
        {
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", cutoff);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.SetOverrideTag("RenderType", "TransparentCutout");
        }

        static void ApplyTransparentSurface(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
        }
    }
}
#endif
