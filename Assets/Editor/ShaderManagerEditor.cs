using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[CustomEditor(typeof(ShaderManager))]
public class ShaderManagerEditor : Editor
{
    private ShaderManager manager;
    private int selectedPresetIndex = 0;

    private void OnEnable()
    {
        manager = (ShaderManager)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (manager.stylePresets.Count > 0)
        {
            GUILayout.Label("Style Presets", EditorStyles.boldLabel);
            selectedPresetIndex = EditorGUILayout.Popup("Current Style", selectedPresetIndex, 
                manager.stylePresets.Select(p => p.StyleName).ToArray());

            if (GUILayout.Button("Apply Style"))
            {
                manager.ApplyStyle(manager.stylePresets[selectedPresetIndex]);
            }

            if (GUILayout.Button("Reset to Original"))
            {
                manager.ResetToOriginal();
            }

            // Show preview if available
            if (manager.stylePresets[selectedPresetIndex].PreviewMaterial != null)
            {
                GUILayout.Label("Preview");
                EditorGUILayout.ObjectField("Preview Material", 
                    manager.stylePresets[selectedPresetIndex].PreviewMaterial, 
                    typeof(Material), false);
            }
        }

        if (GUILayout.Button("Create New Style Preset"))
        {
            CreateNewPreset();
        }
    }

    private void CreateNewPreset()
    {
        ShaderStylePreset preset = ScriptableObject.CreateInstance<ShaderStylePreset>();
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path))
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }
        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/NewShaderStyle.asset");

        AssetDatabase.CreateAsset(preset, assetPathAndName);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = preset;
    }
}