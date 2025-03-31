// ShaderManagerEditor.cs
// Assets/Shader Management System/Editor/
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(ShaderManager))]
public class ShaderManagerEditor : Editor
{
    private ShaderManager shaderManager;

    // For selecting a new shader.
    private Shader newShader;

    // For applying a preset.
    private ShaderPreset selectedPreset;

    // Tab index for current shader.
    private int selectedTab = 0;
    private string[] shaderNames;
    private List<Shader> shaderList = new List<Shader>();
    private Dictionary<Shader, List<Material>> materialsByShader = new Dictionary<Shader, List<Material>>();
    private Dictionary<Shader, MaterialEditor> materialEditors = new Dictionary<Shader, MaterialEditor>();

    public override void OnInspectorGUI()
    {
        shaderManager = (ShaderManager)target;
        // First update the shader list.
        UpdateShaderList();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shader Manager Controls", EditorStyles.boldLabel);

        // --- Tab View: List all unique shaders in child materials ---
        if (shaderList.Count > 0)
        {
            selectedTab = GUILayout.Toolbar(selectedTab, shaderNames);
        }
        else
        {
            EditorGUILayout.HelpBox("No shaders found in child objects.", MessageType.Info);
            DrawDefaultInspector();
            return;
        }
        Shader currentShader = shaderList[selectedTab];
        MaterialEditor currentMatEditor = materialEditors[currentShader];

        // --- Change Shader Button ---
        EditorGUILayout.Space();
        if (GUILayout.Button("Change Shader", GUILayout.Height(25)))
        {
            // Open your custom ShaderSelectionWindow to pick a shader.
            ShaderSelectionWindow.Show(currentShader, (selectedShader) =>
            {
                if (selectedShader != null && selectedShader != currentShader)
                {
                    shaderManager.ChangeShader(selectedShader);
                    UpdateShaderList();
                }
            });
        }
        // Also support ObjectPicker if needed.
        if (Event.current.commandName == "ObjectSelectorUpdated" &&
            EditorGUIUtility.GetObjectPickerControlID() == 123456789)
        {
            Shader newPickedShader = EditorGUIUtility.GetObjectPickerObject() as Shader;
            if (newPickedShader != null && newPickedShader != currentShader)
            {
                shaderManager.ChangeShader(newPickedShader);
                UpdateShaderList();
            }
        }

        EditorGUILayout.Space();
        // --- Preset Options ---
        EditorGUILayout.LabelField("Preset Options", EditorStyles.boldLabel);
        selectedPreset = EditorGUILayout.ObjectField("Preset to Apply", selectedPreset, typeof(ShaderPreset), false) as ShaderPreset;
        if (selectedPreset != null)
        {
            if (GUILayout.Button("Apply Preset", GUILayout.Height(25)))
            {
                shaderManager.ApplyPreset(selectedPreset);
            }
        }
        if (GUILayout.Button("Save Preset", GUILayout.Height(25)))
        {
            // Create a new preset instance and let ShaderManager save it.
            ShaderPreset preset = ScriptableObject.CreateInstance<ShaderPreset>();
            if (shaderManager.childMaterials.Count > 0)
            {
                preset.shader = shaderManager.childMaterials[0].shader;
                preset.presetName = preset.shader.name + " Preset";
            }
            else
            {
                preset.presetName = "New Preset";
            }
            shaderManager.SavePreset(preset);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // --- Shader Properties Listing ---
        EditorGUILayout.LabelField("Shader Properties (Public Only)", EditorStyles.boldLabel);
        int propCount = ShaderUtil.GetPropertyCount(currentShader);
        // Use the first material as sample.
        Material sample = materialsByShader[currentShader][0];
        for (int i = 0; i < propCount; i++)
        {
            if (ShaderUtil.IsShaderPropertyHidden(currentShader, i))
                continue;
            string propName = ShaderUtil.GetPropertyName(currentShader, i);
            if (!sample.HasProperty(propName))
                continue;
            ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(currentShader, i);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(propName, GUILayout.MaxWidth(150));

            // Draw property field based on type.
            EditorGUI.BeginDisabledGroup(GetProjectPreserveFlag(shaderManager, currentShader, propName));
            switch (propType)
            {
                case ShaderUtil.ShaderPropertyType.Color:
                    Color currentColor = sample.GetColor(propName);
                    Color newColor = EditorGUILayout.ColorField(currentColor);
                    if (newColor != currentColor)
                    {
                        foreach (Material mat in materialsByShader[currentShader])
                        {
                            if (mat.HasProperty(propName))
                            {
                                mat.SetColor(propName, newColor);
                                EditorUtility.SetDirty(mat);
                            }
                        }
                    }
                    break;
                case ShaderUtil.ShaderPropertyType.Float:
                case ShaderUtil.ShaderPropertyType.Range:
                    float currentFloat = sample.GetFloat(propName);
                    float newFloat = EditorGUILayout.FloatField(currentFloat);
                    if (newFloat != currentFloat)
                    {
                        foreach (Material mat in materialsByShader[currentShader])
                        {
                            if (mat.HasProperty(propName))
                            {
                                mat.SetFloat(propName, newFloat);
                                EditorUtility.SetDirty(mat);
                            }
                        }
                    }
                    break;
                case ShaderUtil.ShaderPropertyType.Vector:
                    Vector4 currentVector = sample.GetVector(propName);
                    Vector4 newVector = EditorGUILayout.Vector4Field("", currentVector);
                    if (newVector != currentVector)
                    {
                        foreach (Material mat in materialsByShader[currentShader])
                        {
                            if (mat.HasProperty(propName))
                            {
                                mat.SetVector(propName, newVector);
                                EditorUtility.SetDirty(mat);
                            }
                        }
                    }
                    break;
                case ShaderUtil.ShaderPropertyType.TexEnv:
                    Texture currentTex = sample.GetTexture(propName);
                    Texture newTex = EditorGUILayout.ObjectField(currentTex, typeof(Texture), false) as Texture;
                    if (newTex != currentTex)
                    {
                        foreach (Material mat in materialsByShader[currentShader])
                        {
                            if (mat.HasProperty(propName))
                            {
                                mat.SetTexture(propName, newTex);
                                EditorUtility.SetDirty(mat);
                            }
                        }
                    }
                    break;
            }
            EditorGUI.EndDisabledGroup();

            // --- Preserve Flag Toggle (Boolean, not dropdown) ---
            bool preserve = GetProjectPreserveFlag(shaderManager, currentShader, propName);
            bool newPreserve = EditorGUILayout.ToggleLeft("Preserve", preserve, GUILayout.Width(100));
            if (newPreserve != preserve)
                SetProjectPreserveFlag(shaderManager, currentShader, propName, newPreserve);
            
            EditorGUILayout.EndHorizontal();
        }
        
        // --- Additional Material Options (GPU Instancing) ---
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Additional Material Options", EditorStyles.boldLabel);
        bool gpuInstancing = sample.enableInstancing;
        bool newGpuInstancing = EditorGUILayout.Toggle("Enable GPU Instancing", gpuInstancing);
        if (newGpuInstancing != gpuInstancing)
        {
            foreach (Material mat in materialsByShader[currentShader])
            {
                mat.enableInstancing = newGpuInstancing;
                EditorUtility.SetDirty(mat);
            }
        }

        EditorGUILayout.Space();
        // --- Built-in Material Inspector (Optional) ---
        if (materialEditors.ContainsKey(currentShader) && materialEditors[currentShader] != null)
        {
            materialEditors[currentShader].serializedObject.Update();
            materialEditors[currentShader].OnInspectorGUI();
            materialEditors[currentShader].serializedObject.ApplyModifiedProperties();
        }
        
        // Finally, draw default inspector for other serialized fields.
        DrawDefaultInspector();
    }

    // --- Helper methods for Preserve Flags (Editor side, using ShaderManager data) ---
    private bool GetProjectPreserveFlag(ShaderManager manager, Shader shader, string propName)
    {
        foreach (var entry in manager.preserveFlags)
        {
            if (entry.shaderName == shader.name && entry.propertyName == propName)
                return entry.preserve;
        }
        return false;
    }

    private void SetProjectPreserveFlag(ShaderManager manager, Shader shader, string propName, bool value)
    {
        bool found = false;
        foreach (var entry in manager.preserveFlags)
        {
            if (entry.shaderName == shader.name && entry.propertyName == propName)
            {
                entry.preserve = value;
                found = true;
                break;
            }
        }
        if (!found)
        {
            PreserveFlagData newEntry = new PreserveFlagData();
            newEntry.shaderName = shader.name;
            newEntry.propertyName = propName;
            newEntry.preserve = value;
            manager.preserveFlags.Add(newEntry);
        }
        EditorUtility.SetDirty(manager);
    }

    // --- Update Shader List (similar to previous implementation) ---
    private void UpdateShaderList()
    {
        // Clear previous data.
        materialsByShader.Clear();
        shaderList.Clear();
        materialEditors.Clear();

        shaderManager.CollectChildMaterials();
        foreach (Material mat in shaderManager.childMaterials)
        {
            if (mat == null || mat.shader == null)
                continue;
            if (!materialsByShader.ContainsKey(mat.shader))
                materialsByShader[mat.shader] = new List<Material>();
            if (!materialsByShader[mat.shader].Contains(mat))
                materialsByShader[mat.shader].Add(mat);
        }
        foreach (var kvp in materialsByShader)
        {
            shaderList.Add(kvp.Key);
        }
        shaderNames = new string[shaderList.Count];
        for (int i = 0; i < shaderList.Count; i++)
        {
            shaderNames[i] = shaderList[i].name;
        }
        foreach (Shader shader in shaderList)
        {
            Material[] mats = materialsByShader[shader].ToArray();
            MaterialEditor matEditor = Editor.CreateEditor(mats, typeof(MaterialEditor)) as MaterialEditor;
            materialEditors.Add(shader, matEditor);
        }
        if (selectedTab >= shaderList.Count)
            selectedTab = 0;
    }
}