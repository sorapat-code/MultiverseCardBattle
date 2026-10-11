using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MultiverseCardBattle.Editor
{
    /// <summary>
    /// แปลง Material ของ URP (Universal Render Pipeline/Lit, Simple Lit, Unlit) ในโฟลเดอร์ที่เลือก
    /// ให้ใช้ได้กับ Built-in Render Pipeline (ไม่งั้นจะขึ้นสีชมพู):
    ///   Lit / Simple Lit -> Standard (Cutout ถ้าเปิด Alpha Clip, Fade ถ้าโปร่งใส)
    ///   Unlit            -> Unlit/Texture หรือ Unlit/Transparent Cutout
    /// ใช้: เลือกโฟลเดอร์ของแพ็คใน Project แล้วเลือกเมนู Multiverse Card Battle/Assets/Convert URP Materials (Selected Folder)
    /// Shader Graph / SpeedTree / Terrain ทำให้อัตโนมัติไม่ได้ (จะรายงานชื่อไว้ใน Console)
    /// </summary>
    public static class UrpMaterialConverter
    {
        [MenuItem("Multiverse Card Battle/Assets/Convert URP Materials (Selected Folder)")]
        public static void ConvertSelected()
        {
            Object selected = Selection.activeObject;

            string folder = selected != null ? AssetDatabase.GetAssetPath(selected) : "";

            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog(
                    "Convert URP Materials", "เลือกโฟลเดอร์ในหน้าต่าง Project ก่อน", "OK");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });

            int converted = 0;
            List<string> skipped = new List<string>();

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                    if (material == null || material.shader == null)
                        continue;

                    string shaderName = material.shader.name;

                    bool broken = shaderName == "Hidden/InternalErrorShader";
                    bool urp = shaderName.StartsWith("Universal Render Pipeline/");

                    if (!urp && !broken)
                        continue;

                    if (Convert(material, shaderName))
                    {
                        EditorUtility.SetDirty(material);
                        converted++;
                    }
                    else
                    {
                        skipped.Add(path + "  (" + shaderName + ")");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            Debug.Log("[UrpMaterialConverter] converted " + converted + " materials in " + folder +
                (skipped.Count > 0 ? "\nSkipped (convert by hand):\n" + string.Join("\n", skipped) : ""));
        }

        private static bool Convert(Material material, string shaderName)
        {
            bool lit = shaderName.EndsWith("/Lit") || shaderName.EndsWith("/Simple Lit") ||
                       shaderName.EndsWith("/Complex Lit") || shaderName.EndsWith("/Baked Lit");
            bool unlit = shaderName.EndsWith("/Unlit");

            // ข้อมูลที่ต้องเก็บก่อนเปลี่ยนเชดเดอร์ (property ชื่อเดิมจะหายไป)
            Texture baseMap = Tex(material, "_BaseMap", "_MainTex");
            Texture bump = Tex(material, "_BumpMap", null);
            Texture emission = Tex(material, "_EmissionMap", null);

            Color color = material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : (material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white);

            Color emissionColor = material.HasProperty("_EmissionColor")
                ? material.GetColor("_EmissionColor") : Color.black;

            bool clip = material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > 0.5f;
            float cutoff = material.HasProperty("_Cutoff") ? material.GetFloat("_Cutoff") : 0.5f;
            bool transparent = material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f;
            bool twoSided = material.HasProperty("_Cull") && Mathf.Approximately(material.GetFloat("_Cull"), 0f);

            float smoothness = material.HasProperty("_Smoothness") ? material.GetFloat("_Smoothness") : 0.2f;
            float metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;

            if (unlit)
            {
                Shader shader = Shader.Find(clip ? "Unlit/Transparent Cutout" : "Unlit/Texture");

                if (shader == null)
                    return false;

                material.shader = shader;
                material.SetTexture("_MainTex", baseMap);

                if (clip && material.HasProperty("_Cutoff"))
                    material.SetFloat("_Cutoff", cutoff);

                return true;
            }

            if (!lit && !shaderName.StartsWith("Hidden/"))
                return false;

            Shader standard = Shader.Find("Standard");

            if (standard == null)
                return false;

            material.shader = standard;

            material.SetTexture("_MainTex", baseMap);
            material.SetColor("_Color", color);
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);

            if (bump != null)
            {
                material.SetTexture("_BumpMap", bump);
                material.EnableKeyword("_NORMALMAP");
            }

            if (emission != null || emissionColor.maxColorComponent > 0.01f)
            {
                material.SetTexture("_EmissionMap", emission);
                material.SetColor("_EmissionColor", emissionColor);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }

            if (clip)
            {
                material.SetFloat("_Mode", 1f); // Cutout
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.SetFloat("_SrcBlend", 1f);
                material.SetFloat("_DstBlend", 0f);
                material.SetFloat("_ZWrite", 1f);
                material.SetFloat("_Cutoff", cutoff);
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.renderQueue = 2450;
            }
            else if (transparent)
            {
                material.SetFloat("_Mode", 2f); // Fade
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat("_SrcBlend", 5f);
                material.SetFloat("_DstBlend", 10f);
                material.SetFloat("_ZWrite", 0f);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = 3000;
            }
            else
            {
                material.SetFloat("_Mode", 0f);
                material.SetOverrideTag("RenderType", "");
                material.SetFloat("_SrcBlend", 1f);
                material.SetFloat("_DstBlend", 0f);
                material.SetFloat("_ZWrite", 1f);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.renderQueue = -1;
            }

            // Standard ไม่มีสวิตช์สองหน้า: ใบไม้หน้าเดียวจะมองหลังไม่เห็น (รายงานไว้ให้ทราบ)
            if (twoSided)
                Debug.Log("[UrpMaterialConverter] " + material.name + " เป็นแบบสองหน้า (Standard เป็นหน้าเดียว) " +
                          "ถ้าใบไม้หายเมื่อมองจากด้านหลังให้ใช้เชดเดอร์ MCB/FoliageCutout แทน", material);

            return true;
        }

        private static Texture Tex(Material material, string first, string second)
        {
            if (material.HasProperty(first))
            {
                Texture texture = material.GetTexture(first);

                if (texture != null)
                    return texture;
            }

            if (second != null && material.HasProperty(second))
                return material.GetTexture(second);

            return null;
        }
    }
}
