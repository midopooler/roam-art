// ShaderManagerEditor.cs
//  Assets/Shader Management System/Editor/ShaderManagerEditor.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

[CustomEditor(typeof(ShaderManager))]
public class ShaderManagerEditor : Editor
{
    private Dictionary<Shader, List<Material>> materialsByShader = new Dictionary<Shader, List<Material>>();
    private List<Shader> shaderList = new List<Shader>();
    private Dictionary<Shader, MaterialEditor> materialEditors = new Dictionary<Shader, MaterialEditor>();

    private string[] shaderNames;
    private int selectedTab = 0;

    // Field for choosing a preset asset to apply.
    private ShaderPreset selectedPreset;

    // We'll store a control ID to track our shader picker.
    private const int ShaderPickerControlID = 123456789;

    private void OnEnable()
    {
        UpdateShaderList();
    }

    private void OnDisable()
    {
        // Clean up editors to avoid memory leaks
        foreach (var kvp in materialEditors)
        {
            if (kvp.Value != null)
                DestroyImmediate(kvp.Value);
        }
        materialEditors.Clear();
    }

    /// <summary>
    /// Collect all unique shaders and materials from child renderers.
    /// Create a MaterialEditor for each group of materials that share the same shader.
    /// </summary>
    private void UpdateShaderList()
    {
        // Clean up old editors
        foreach (var kvp in materialEditors)
        {
            if (kvp.Value != null)
                DestroyImmediate(kvp.Value);
        }
        materialEditors.Clear();
        materialsByShader.Clear();
        shaderList.Clear();

        ShaderManager manager = (ShaderManager)target;
        Renderer[] renderers = manager.GetComponentsInChildren<Renderer>(true);

        // Gather materials by shader
        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;
            foreach (Material mat in rend.sharedMaterials)
            {
                if (mat == null || mat.shader == null) continue;

                if (!materialsByShader.ContainsKey(mat.shader))
                    materialsByShader[mat.shader] = new List<Material>();

                if (!materialsByShader[mat.shader].Contains(mat))
                    materialsByShader[mat.shader].Add(mat);
            }
        }

        // Build list of shaders
        foreach (var kvp in materialsByShader)
        {
            shaderList.Add(kvp.Key);
        }

        // Build array for toolbar names
        shaderNames = new string[shaderList.Count];
        for (int i = 0; i < shaderList.Count; i++)
        {
            shaderNames[i] = shaderList[i].name;
        }

        // Create a MaterialEditor for each shader’s materials
        foreach (Shader shader in shaderList)
        {
            Material[] mats = materialsByShader[shader].ToArray();
            MaterialEditor matEditor = Editor.CreateEditor(mats, typeof(MaterialEditor)) as MaterialEditor;
            materialEditors.Add(shader, matEditor);
        }

        // Reset tab if needed
        if (selectedTab >= shaderList.Count)
            selectedTab = 0;
    }

    public override void OnInspectorGUI()
    {
        // Button to refresh
        if (GUILayout.Button("Refresh Shader List"))
        {
            UpdateShaderList();
        }

        // If no shaders, show message
        if (shaderList.Count == 0)
        {
            EditorGUILayout.HelpBox("No shaders found in child objects.", MessageType.Info);
            return;
        }

        // Toolbar for shader selection
        selectedTab = GUILayout.Toolbar(selectedTab, shaderNames);
        Shader currentShader = shaderList[selectedTab];
        MaterialEditor currentMatEditor = materialEditors[currentShader];

        // Show current shader name
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current Shader: " + currentShader.name, EditorStyles.boldLabel);

        // ---- CHANGE SHADER BUTTON ----
        EditorGUILayout.Space();
        if (GUILayout.Button("Change Shader", GUILayout.Height(25)))
        {
            ShaderSelectionWindow.Show(currentShader, (selectedShader) =>
            {
                if (selectedShader != null && selectedShader != currentShader)
                {
                    foreach (Material mat in materialsByShader[currentShader])
                    {
                        Material temp = new Material(selectedShader);
                        temp.CopyPropertiesFromMaterial(mat);

                        mat.shader = selectedShader;
                        mat.CopyPropertiesFromMaterial(temp);

                        EditorUtility.SetDirty(mat);
                    }

                    UpdateShaderList();
                }
            });
        }


        // If user picked a new shader from the Object Picker, handle it:
        if (Event.current.commandName == "ObjectSelectorUpdated" &&
            EditorGUIUtility.GetObjectPickerControlID() == ShaderPickerControlID)
        {
            Shader newShader = EditorGUIUtility.GetObjectPickerObject() as Shader;
            if (newShader != null && newShader != currentShader)
            {
                // Apply the new shader to all relevant materials,
                // copying any matching properties.
                foreach (Material m in materialsByShader[currentShader])
                {
                    // We'll do a two-step process:
                    // 1) Create a temporary Material with the new shader
                    // 2) Copy properties from the old material to the temp
                    // 3) Assign new shader to the old material
                    // 4) Copy from temp back into the old material
                    Material temp = new Material(newShader);
                    temp.CopyPropertiesFromMaterial(m);

                    m.shader = newShader;
                    m.CopyPropertiesFromMaterial(temp);

                    EditorUtility.SetDirty(m);
                }

                // We need to refresh the dictionary & editor references 
                // so that the new shader is recognized in the tab list
                UpdateShaderList();
            }
        }

        // ---- Preset Options ----
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preset Options", EditorStyles.boldLabel);

        // Save Preset Button
        if (GUILayout.Button("Save Preset", GUILayout.Height(25)))
        {
            SavePreset(currentShader);
        }

        // Field to choose a preset to apply
        selectedPreset = EditorGUILayout.ObjectField("Preset to Apply", selectedPreset, typeof(ShaderPreset), false) as ShaderPreset;
        if (selectedPreset != null)
        {
            if (GUILayout.Button("Apply Preset", GUILayout.Height(25)))
            {
                ApplyPreset(selectedPreset, currentShader);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (currentMatEditor == null)
        {
            EditorGUILayout.HelpBox("No MaterialEditor found for this shader.", MessageType.Warning);
        }
        else
        {
            // Force update of serialized object to ensure properties are drawn
            currentMatEditor.serializedObject.Update();

            // Draw the standard Material Inspector UI
            currentMatEditor.OnInspectorGUI();

            // Apply any changes made
            currentMatEditor.serializedObject.ApplyModifiedProperties();

            // Mark materials as dirty so Unity saves changes
            if (GUI.changed)
            {
                foreach (Material m in materialsByShader[currentShader])
                {
                    EditorUtility.SetDirty(m);
                }
            }
        }
    }

    private void SavePreset(Shader currentShader)
    {
        // Use the first material as the representative sample.
        if (!materialsByShader.ContainsKey(currentShader) || materialsByShader[currentShader].Count == 0)
        {
            Debug.LogWarning("No materials found for shader " + currentShader.name);
            return;
        }
        Material sample = materialsByShader[currentShader][0];

        // Create a new preset asset instance.
        ShaderPreset preset = ScriptableObject.CreateInstance<ShaderPreset>();
        preset.presetName = currentShader.name + " Preset";
        preset.shader = currentShader;

        int propertyCount = ShaderUtil.GetPropertyCount(currentShader);
        for (int i = 0; i < propertyCount; i++)
        {
            // Only consider properties that are visible in the Material Inspector.
            if (ShaderUtil.IsShaderPropertyHidden(currentShader, i))
                continue;

            string propName = ShaderUtil.GetPropertyName(currentShader, i);
            ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(currentShader, i);

            ShaderPropertyEntry entry = new ShaderPropertyEntry();
            entry.propertyName = propName;

            // Map property types; treat Range as Float.
            switch (propType)
            {
                case ShaderUtil.ShaderPropertyType.Float:
                case ShaderUtil.ShaderPropertyType.Range:
                    entry.type = PresetShaderPropertyType.Float;
                    entry.floatValue = sample.GetFloat(propName);
                    break;
                case ShaderUtil.ShaderPropertyType.Color:
                    entry.type = PresetShaderPropertyType.Color;
                    entry.colorValue = sample.GetColor(propName);
                    break;
                case ShaderUtil.ShaderPropertyType.Vector:
                    entry.type = PresetShaderPropertyType.Vector;
                    entry.vectorValue = sample.GetVector(propName);
                    break;
                case ShaderUtil.ShaderPropertyType.TexEnv:
                    entry.type = PresetShaderPropertyType.Texture;
                    entry.textureValue = sample.GetTexture(propName);
                    break;
                default:
                    break;
            }
            preset.propertyEntries.Add(entry);
        }

        // Prompt the user for a save location.
        string path = EditorUtility.SaveFilePanelInProject("Save Shader Preset", preset.presetName, "asset", "Please enter a file name to save the preset to");
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            selectedPreset = preset;
            Debug.Log("Preset saved at " + path);
        }
    }

    private void ApplyPreset(ShaderPreset preset, Shader currentShader)
    {
        // If there are no materials for the current shader, exit.
        if (!materialsByShader.ContainsKey(currentShader))
        {
            Debug.LogWarning("No materials found for shader " + currentShader.name);
            return;
        }

        // If the preset's shader is different from the current shader, change all materials to the preset shader.
        if (preset.shader != currentShader)
        {
            List<Material> mats = materialsByShader[currentShader];
            foreach (Material mat in mats)
            {
                // Create a temporary material with the new shader to copy matching properties.
                Material temp = new Material(preset.shader);
                temp.CopyPropertiesFromMaterial(mat);
                
                // Change the shader on the original material.
                mat.shader = preset.shader;
                mat.CopyPropertiesFromMaterial(temp);
                EditorUtility.SetDirty(mat);
            }
            
            // Refresh the dictionary to reflect the shader changes.
            UpdateShaderList();
            
            // Set currentShader to the new shader.
            currentShader = preset.shader;
        }

        // Now, for each material that uses the current (preset) shader, apply the saved property values.
        if (!materialsByShader.ContainsKey(currentShader))
        {
            Debug.LogWarning("No materials found for shader " + currentShader.name);
            return;
        }

        foreach (Material mat in materialsByShader[currentShader])
        {
            foreach (ShaderPropertyEntry entry in preset.propertyEntries)
            {
                if (!mat.HasProperty(entry.propertyName))
                    continue;

                switch (entry.type)
                {
                    case PresetShaderPropertyType.Float:
                        mat.SetFloat(entry.propertyName, entry.floatValue);
                        break;
                    case PresetShaderPropertyType.Color:
                        mat.SetColor(entry.propertyName, entry.colorValue);
                        break;
                    case PresetShaderPropertyType.Vector:
                        mat.SetVector(entry.propertyName, entry.vectorValue);
                        break;
                    case PresetShaderPropertyType.Texture:
                        mat.SetTexture(entry.propertyName, entry.textureValue);
                        break;
                }
            }
            EditorUtility.SetDirty(mat);
        }
        Debug.Log("Preset applied to all materials using shader " + currentShader.name);
    }


}