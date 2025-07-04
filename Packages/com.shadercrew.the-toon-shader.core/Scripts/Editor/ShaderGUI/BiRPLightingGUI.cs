using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static ShaderCrew.TheToonShader.TheToonShaderGUIEditor;

namespace ShaderCrew.TheToonShader
{
    public class BiRPLightingGUI
    {
        public MaterialEditor m_MaterialEditor;

        public Color textColor;
        Color originalColor;

        public enum BlendMode
        {
            Opaque,
            Cutout,
            //Transparent
        }
        public enum AlphaModes
        {
            Opaque,
            Blend,
            Add,
            PreMultiply
        }

        public enum SmoothnessMapChannel
        {
            SpecularMetallicAlpha,
            AlbedoAlpha,
        }



        // Standard
        public MaterialProperty blendMode = null;
        public MaterialProperty cullMode = null;

        public MaterialProperty albedoMap = null;
        public MaterialProperty albedoColor = null;
        public MaterialProperty alphaCutoff = null;
        public MaterialProperty metallic = null;
        public MaterialProperty metallicMap = null;

        public MaterialProperty smoothness = null;
        public MaterialProperty smoothnessScale = null;
        public MaterialProperty smoothnessMapChannel = null;

        public MaterialProperty bumpScale = null;
        public MaterialProperty bumpMap = null;

        public MaterialProperty emissionMap = null;
        public MaterialProperty emissionColor = null;

        public MaterialProperty occlusionMap = null;
        public MaterialProperty occlusionStrength = null;

        public MaterialProperty heigtMapScale = null;
        public MaterialProperty heightMap = null;

        public MaterialProperty detailMask = null;
        public MaterialProperty detailAlbedoMap = null;
        public MaterialProperty detailNormalMap = null;
        public MaterialProperty detailNormalMapScale = null;


        public MaterialProperty uvSetSecondary = null;



        private static class StandardLitStyles
        {
            // Standard
            public static GUIContent albedoText = EditorGUIUtility.TrTextContent("Albedo", "Albedo (RGB) and Transparency (A)");
            public static GUIContent alphaCutoffText = EditorGUIUtility.TrTextContent("Alpha Cutoff", "Threshold for alpha cutoff");
            public static GUIContent metallicMapText = EditorGUIUtility.TrTextContent("Metallic", "Metallic (R) and Smoothness (A)");

            public static GUIContent smoothnessText = EditorGUIUtility.TrTextContent("Smoothness", "Smoothness value");
            public static GUIContent smoothnessScaleText = EditorGUIUtility.TrTextContent("Smoothness", "Smoothness scale factor");
            public static GUIContent smoothnessMapChannelText = EditorGUIUtility.TrTextContent("Source", "Smoothness texture and channel");

            public static GUIContent normalMapText = EditorGUIUtility.TrTextContent("Normal Map", "Normal Map");

            public static GUIContent heightMapText = EditorGUIUtility.TrTextContent("Height Map", "Height Map (G)");

            public static GUIContent emissionText = EditorGUIUtility.TrTextContent("Emission", "Emission (RGB)");
            public static GUIContent occlusionMapText = EditorGUIUtility.TrTextContent("Occlusion", "Occlusion (G)");

            public static GUIContent detailMaskText = EditorGUIUtility.TrTextContent("Detail Mask", "Mask for Secondary Maps (A)");
            public static GUIContent detailAlbedoText = EditorGUIUtility.TrTextContent("Detail Albedo x2", "Albedo (RGB) multiplied by 2");
            public static GUIContent detailNormalMapText = EditorGUIUtility.TrTextContent("Normal Map", "Normal Map");
            public static GUIContent uvSetLabel = EditorGUIUtility.TrTextContent("UV Set");


            public static string secondaryMapsText = "Secondary Maps";
            public static string advancedText = "Advanced Options";
            public static string renderingMode = "Rendering Mode";

            public static readonly string[] blendNames = System.Enum.GetNames(typeof(BlendMode));

        }

        public void DoSetup(MaterialEditor materialEditor)
        {
            m_MaterialEditor = materialEditor;
            MaterialChanged(materialEditor.target as Material);


        }


        public static void DrawUILine(int thickness = 1, int padding = 10)
        {
            Color color = Color.black;
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            EditorGUI.DrawRect(r, color);
        }

        public void BiRPShaderPropertiesGUI(Material material, GeneralShadingMode shadingMode, LightFunction lightFunction)
        {

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            {
                BlendModePopup();
                m_MaterialEditor.ShaderProperty(cullMode, "Cull Mode");

                DoAlbedoArea(material);
                if(shadingMode == GeneralShadingMode.LightBased)
                {
                    DoSpecularMetallicArea(material);
                }

                DoNormalArea();

                if (shadingMode == GeneralShadingMode.LightBased)
                {
                    DoHeightMapArea();
                    DoOcclusionArea();
                    DoDetailMaskArea();
                }
                DoEmissionArea();

                bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;

                bool showTextureScaleOffset = (albedoMap.textureValue != null ||
                                                bumpMap.textureValue != null ||
                                                (emissionMap.textureValue != null && shouldEmissionBeEnabled));

                if (shadingMode == GeneralShadingMode.LightBased)
                {
                    showTextureScaleOffset = showTextureScaleOffset || occlusionMap.textureValue != null ||
                                                    heightMap.textureValue != null ||
                                                    metallicMap.textureValue != null ||
                                                    detailMask.textureValue != null;                } 

                if (showTextureScaleOffset)
                {
                    EditorGUI.indentLevel += 2;
                    m_MaterialEditor.TextureScaleOffsetProperty(albedoMap);
                    EditorGUI.indentLevel -= 2;
                }
                if (shadingMode == GeneralShadingMode.LightBased)
                {
                    EditorGUILayout.Space();
                    DoSecondaryArea();
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                //MaterialChanged(material);
                foreach (var obj in blendMode.targets)
                    MaterialChanged((Material)obj);
            }
            EditorGUILayout.Space();
        }





        public void OnlyBlendModeAndCullModeGUI()
        {
            BlendModePopup();
            m_MaterialEditor.ShaderProperty(cullMode, "Cull Mode");
        }

        public void OnlyCullModeGUI()
        {
            m_MaterialEditor.ShaderProperty(cullMode, "Cull Mode");
        }



        void BlendModePopup()
        {
            EditorGUI.showMixedValue = blendMode.hasMixedValue;
            var mode = (BlendMode)blendMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (BlendMode)EditorGUILayout.Popup(StandardLitStyles.renderingMode, (int)mode, StandardLitStyles.blendNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Mode");
                blendMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }


        static void MaterialChanged(Material material)
        {
            SetMaterialKeywords(material);
        }

        static void SetMaterialKeywords(Material material)
        {
            SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap") || material.GetTexture("_DetailNormalMap"));
            SetKeyword(material, "_METALLICGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
            SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));
            SetKeyword(material, "_DETAIL_MULX2", material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap"));

            MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);
            if (material.HasProperty("_SmoothnessTextureChannel"))
            {
                SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha);
            }

        }
        static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }





        void DoAlbedoArea(Material material)
        {
            float oriLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - 94;
            m_MaterialEditor.TexturePropertySingleLine(StandardLitStyles.albedoText, albedoMap, albedoColor);
            EditorGUIUtility.labelWidth = oriLabelWidth;

            if (((BlendMode)material.GetFloat("_Mode") == BlendMode.Cutout))
            {
                m_MaterialEditor.ShaderProperty(alphaCutoff, StandardLitStyles.alphaCutoffText.text, MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);
            }
        }


        void DoDetailMaskArea()
        {
            m_MaterialEditor.TexturePropertySingleLine(StandardLitStyles.detailMaskText, detailMask);
        }



        void DoSecondaryArea()
        {
            GUILayout.Label(StandardLitStyles.secondaryMapsText, EditorStyles.boldLabel);
            m_MaterialEditor.TexturePropertySingleLine(StandardLitStyles.detailAlbedoText, detailAlbedoMap);
            m_MaterialEditor.TexturePropertySingleLine(StandardLitStyles.detailNormalMapText, detailNormalMap, detailNormalMapScale);
            m_MaterialEditor.TextureScaleOffsetProperty(detailAlbedoMap);
            m_MaterialEditor.ShaderProperty(uvSetSecondary, StandardLitStyles.uvSetLabel.text);

        }

        void DoNormalArea()
        {
            m_MaterialEditor.TexturePropertySingleLine(StandardLitStyles.normalMapText, bumpMap, bumpMap.textureValue != null ? bumpScale : null);
        }

        void DoHeightMapArea()
        {
            m_MaterialEditor.TexturePropertySingleLine(StandardLitStyles.heightMapText, heightMap, heightMap.textureValue != null ? heigtMapScale : null);
        }
        void DoSpecularMetallicArea(Material material)
        {
            bool hasGlossMap = metallicMap.textureValue != null;
            m_MaterialEditor.TexturePropertySingleLine(StandardLitStyles.metallicMapText, metallicMap, hasGlossMap ? null : metallic);

            bool showSmoothnessScale = hasGlossMap;
            if (smoothnessMapChannel != null)
            {
                int smoothnessChannel = (int)smoothnessMapChannel.floatValue;
                if (smoothnessChannel == (int)SmoothnessMapChannel.AlbedoAlpha)
                    showSmoothnessScale = true;
            }

            int indentation = 2;
            m_MaterialEditor.ShaderProperty(showSmoothnessScale ? smoothnessScale : smoothness, showSmoothnessScale ? StandardLitStyles.smoothnessScaleText : StandardLitStyles.smoothnessText, indentation);

            ++indentation;
            if (smoothnessMapChannel != null)
                m_MaterialEditor.ShaderProperty(smoothnessMapChannel, StandardLitStyles.smoothnessMapChannelText, indentation);
        }


        void DoOcclusionArea()
        {
            m_MaterialEditor.TexturePropertySingleLine(StandardLitStyles.occlusionMapText, occlusionMap, occlusionMap.textureValue != null ? occlusionStrength : null);
        }


        void DoEmissionArea()
        {
            if (m_MaterialEditor.EmissionEnabledProperty())
            {
                bool hadEmissionTexture = emissionMap.textureValue != null;

                m_MaterialEditor.TexturePropertyWithHDRColor(StandardLitStyles.emissionText, emissionMap, emissionColor, false);

                float brightness = emissionColor.colorValue.maxColorComponent;
                if (emissionMap.textureValue != null && !hadEmissionTexture && brightness <= 0f)
                {
                    emissionColor.colorValue = Color.white;
                }

                m_MaterialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
            }
        }






        static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
        {
            int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
            if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
                return SmoothnessMapChannel.AlbedoAlpha;
            else
                return SmoothnessMapChannel.SpecularMetallicAlpha;
        }




        public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
        {
            //switch (blendMode)
            //{
            //    case BlendMode.Opaque:
            //        material.SetFloat("_AlphaMode", (float)AlphaModes.Opaque);
            //        material.SetOverrideTag("RenderType", "");
            //        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            //        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            //        material.SetInt("_ZWrite", 1);
            //        //material.DisableKeyword("_ALPHATEST_ON");
            //        //material.renderQueue = -1;
            //        //material.SetColor("_Color", Color.green);
            //        break;
            //    case BlendMode.Cutout:
            //        material.SetFloat("_AlphaMode", (float)AlphaModes.Opaque);
            //        //material.SetInt("_ZWrite", 1);
            //        //material.EnableKeyword("_ALPHATEST_ON");
            //        //material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            //        break;
            //    case BlendMode.Transparent:
            //        material.SetOverrideTag("RenderType", "Transparent");
            //        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            //        //material.SetColor("_Color", Color.blue);
            //        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            //        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            //        material.SetInt("_ZWrite", 0);
            //        //material.SetFloat("_AlphaMode", (float)AlphaModes.Blend);
            //        break;
            //}
        }



    }
}
