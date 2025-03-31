// ShaderManagerEditor.cs
// Assets/Shader Management System/Editor/)
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

// Loads the JSON reference data for shader property mappings.
public static class ShaderPropertyReferenceLoader
{
    private static ShaderPropertyReferenceData referenceData;

    public static ShaderPropertyReferenceData LoadReferenceData()
    {
        if (referenceData == null)
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>("ShaderPropertyReference");
            if (jsonAsset != null)
            {
                referenceData = JsonUtility.FromJson<ShaderPropertyReferenceData>(jsonAsset.text);
            }
            else
            {
                Debug.LogError("ShaderPropertyReference.json not found in Resources folder.");
                referenceData = new ShaderPropertyReferenceData { properties = new List<ShaderPropertyMetadata>() };
            }
        }
        return referenceData;
    }
}

// Data classes that match your JSON schema.
[System.Serializable]
public class ShaderPropertyMetadata
{
    public string canonicalName;
    public List<string> aliases;
    public string propertyType;
    public string description;
    public string defaultValue;
    public string usageNotes;
}

[System.Serializable]
public class ShaderPropertyReferenceData
{
    public List<ShaderPropertyMetadata> properties;
}

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
    // Control ID for shader picker (if needed).
    private const int ShaderPickerControlID = 123456789;

    // --- Project-based Preserve Flag Helpers ---
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
    // --- End Preserve Flag Helpers ---

    // --- Mapping Helpers ---
    private bool ShaderHasProperty(Shader shader, string propName)
    {
        int count = ShaderUtil.GetPropertyCount(shader);
        for (int i = 0; i < count; i++)
        {
            if (ShaderUtil.GetPropertyName(shader, i) == propName)
                return true;
        }
        return false;
    }

    // Returns the mapped property name in the target shader based on the JSON reference.
    private string GetMappedPropertyName(Shader sourceShader, Shader targetShader, string sourcePropName)
    {
        var referenceData = ShaderPropertyReferenceLoader.LoadReferenceData();
        ShaderPropertyMetadata mapping = null;
        // Find the mapping entry where the source property is either the canonical name or one of the aliases.
        foreach (var meta in referenceData.properties)
        {
            if (meta.canonicalName == sourcePropName || (meta.aliases != null && meta.aliases.Contains(sourcePropName)))
            {
                mapping = meta;
                break;
            }
        }
        
        if (mapping != null)
        {
            // Build a combined list that includes the canonical name and then all aliases.
            List<string> allNames = new List<string>();
            allNames.Add(mapping.canonicalName);
            if (mapping.aliases != null)
            {
                foreach (var alias in mapping.aliases)
                {
                    if (!allNames.Contains(alias))
                        allNames.Add(alias);
                }
            }
            // Iterate over the combined list and return the first name that exists on the target shader.
            foreach (var name in allNames)
            {
                if (ShaderHasProperty(targetShader, name))
                    return name;
            }
            return null;
        }
        // Fallback: if no mapping is found, return sourcePropName if it exists on target.
        return ShaderHasProperty(targetShader, sourcePropName) ? sourcePropName : null;
    }
    
    // Finds the property index in a shader.
    private int FindPropertyIndex(Shader shader, string propName)
    {
        int count = ShaderUtil.GetPropertyCount(shader);
        for (int i = 0; i < count; i++)
        {
            if (ShaderUtil.GetPropertyName(shader, i) == propName)
                return i;
        }
        return -1;
    }

    // --- TransferProperties ---
    // Transfers property values and preserve flags from source material to target material using the mapping.
    private void TransferProperties(Material source, Material target, ShaderManager manager)
    {
        int count = ShaderUtil.GetPropertyCount(source.shader);
        for (int i = 0; i < count; i++)
        {
            string srcPropName = ShaderUtil.GetPropertyName(source.shader, i);
            string targetPropName = GetMappedPropertyName(source.shader, target.shader, srcPropName);
            if (string.IsNullOrEmpty(targetPropName))
                continue;

            int targetIndex = FindPropertyIndex(target.shader, targetPropName);
            if (targetIndex == -1)
                continue;

            ShaderUtil.ShaderPropertyType srcType = ShaderUtil.GetPropertyType(source.shader, i);
            ShaderUtil.ShaderPropertyType targetType = ShaderUtil.GetPropertyType(target.shader, targetIndex);
            if (srcType != targetType)
            {
                Debug.LogWarning($"Type mismatch for property '{srcPropName}': Source type {srcType} vs. Target property '{targetPropName}' type {targetType}. Skipping transfer.");
                continue;
            }

            switch (srcType)
            {
                case ShaderUtil.ShaderPropertyType.Float:
                case ShaderUtil.ShaderPropertyType.Range:
                    float fval = source.GetFloat(srcPropName);
                    target.SetFloat(targetPropName, fval);
                    break;
                case ShaderUtil.ShaderPropertyType.Color:
                    Color cval = source.GetColor(srcPropName);
                    target.SetColor(targetPropName, cval);
                    break;
                case ShaderUtil.ShaderPropertyType.Vector:
                    Vector4 vval = source.GetVector(srcPropName);
                    target.SetVector(targetPropName, vval);
                    break;
                case ShaderUtil.ShaderPropertyType.TexEnv:
                    Texture tval = source.GetTexture(srcPropName);
                    target.SetTexture(targetPropName, tval);
                    break;
            }
            // Transfer the preserve flag if it is set.
            bool preserve = GetProjectPreserveFlag(manager, source.shader, srcPropName);
            if (preserve)
                SetProjectPreserveFlag(manager, target.shader, targetPropName, true);
        }
    }
    // --- End Mapping Helpers ---

    private void OnEnable()
    {
        UpdateShaderList();
    }

    private void OnDisable()
    {
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
        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;
            foreach (Material mat in rend.sharedMaterials)
            {
                if (mat == null || mat.shader == null)
                    continue;
                if (!materialsByShader.ContainsKey(mat.shader))
                    materialsByShader[mat.shader] = new List<Material>();
                if (!materialsByShader[mat.shader].Contains(mat))
                    materialsByShader[mat.shader].Add(mat);
            }
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

    public override void OnInspectorGUI()
    {
        ShaderManager manager = (ShaderManager)target;
        if (GUILayout.Button("Refresh Shader List"))
        {
            UpdateShaderList();
        }
        if (shaderList.Count == 0)
        {
            EditorGUILayout.HelpBox("No shaders found in child objects.", MessageType.Info);
            return;
        }
        selectedTab = GUILayout.Toolbar(selectedTab, shaderNames);
        Shader currentShader = shaderList[selectedTab];
        MaterialEditor currentMatEditor = materialEditors[currentShader];
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current Shader: " + currentShader.name, EditorStyles.boldLabel);

        // ---- CHANGE SHADER BUTTON ----
        EditorGUILayout.Space();
        if (GUILayout.Button("Change Shader", GUILayout.Height(25)))
        {
            // Using a custom ShaderSelectionWindow to pick a new shader (assumed implemented elsewhere)
            ShaderSelectionWindow.Show(currentShader, (selectedShader) =>
            {
                if (selectedShader != null && selectedShader != currentShader)
                {
                    foreach (Material mat in materialsByShader[currentShader])
                    {
                        // Create a temporary material with the new shader.
                        Material temp = new Material(selectedShader);
                        // Transfer properties using mapping.
                        //TransferProperties(mat, temp, manager);
                        temp.CopyPropertiesFromMaterial(mat);
                        // Now assign the new shader and transfer back.
                        mat.shader = selectedShader;
                        mat.CopyPropertiesFromMaterial(temp);
                        //TransferProperties(temp, mat, manager);
                        EditorUtility.SetDirty(mat);
                    }
                    UpdateShaderList();
                }
            });
        }

        if (Event.current.commandName == "ObjectSelectorUpdated" &&
            EditorGUIUtility.GetObjectPickerControlID() == ShaderPickerControlID)
        {
            Shader newShader = EditorGUIUtility.GetObjectPickerObject() as Shader;
            if (newShader != null && newShader != currentShader)
            {
                foreach (Material m in materialsByShader[currentShader])
                {
                    Material temp = new Material(newShader);
                    temp.CopyPropertiesFromMaterial(m);
                    m.shader = newShader;
                    m.CopyPropertiesFromMaterial(temp);
                    /*TransferProperties(m, temp, manager);
                    m.shader = newShader;
                    TransferProperties(temp, m, manager);*/
                    EditorUtility.SetDirty(m);
                }
                UpdateShaderList();
            }
        }
        
        // ---- Preset Options ----
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preset Options", EditorStyles.boldLabel);
        if (GUILayout.Button("Save Preset", GUILayout.Height(25)))
        {
            SavePreset(currentShader);
        }
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

        // ---- Custom Shader Property List with Preserve Toggles ----
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shader Properties (Bulk Changes Skip Preserved)", EditorStyles.boldLabel);
        int propCount = ShaderUtil.GetPropertyCount(currentShader);
        // Use the first material as sample
        Material sample = materialsByShader[currentShader][0];
        for (int i = 0; i < propCount; i++)
        {
            if (ShaderUtil.IsShaderPropertyHidden(currentShader, i))
                continue;
            string propName = ShaderUtil.GetPropertyName(currentShader, i);
            // NEW: Check if the sample material actually has this property. If not, skip it.
            if (!sample.HasProperty(propName))
                continue;
            ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(currentShader, i);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(propName, GUILayout.MaxWidth(150));
            bool preserve = GetProjectPreserveFlag(manager, currentShader, propName);
            EditorGUI.BeginDisabledGroup(preserve);
            switch (propType)
            {
                case ShaderUtil.ShaderPropertyType.Color:
                {
                    Color currentColor = sample.GetColor(propName);
                    Color newColor = EditorGUILayout.ColorField(currentColor);
                    if (!preserve && newColor != currentColor)
                    {
                        foreach (Material mat in materialsByShader[currentShader])
                        {
                            if(mat.HasProperty(propName))
                            {
                                mat.SetColor(propName, newColor);
                                EditorUtility.SetDirty(mat);
                            }
                        }
                    }
                    break;
                }
                case ShaderUtil.ShaderPropertyType.Float:
                case ShaderUtil.ShaderPropertyType.Range:
                {
                    float currentFloat = sample.GetFloat(propName);
                    float newFloat = EditorGUILayout.FloatField(currentFloat);
                    if (!preserve && newFloat != currentFloat)
                    {
                        foreach (Material mat in materialsByShader[currentShader])
                        {
                            if(mat.HasProperty(propName))
                            {
                                mat.SetFloat(propName, newFloat);
                                EditorUtility.SetDirty(mat);
                            }
                        }
                    }
                    break;
                }
                case ShaderUtil.ShaderPropertyType.Vector:
                {
                    Vector4 currentVector = sample.GetVector(propName);
                    Vector4 newVector = EditorGUILayout.Vector4Field("", currentVector);
                    if (!preserve && newVector != currentVector)
                    {
                        foreach (Material mat in materialsByShader[currentShader])
                        {
                            if(mat.HasProperty(propName))
                            {
                                mat.SetVector(propName, newVector);
                                EditorUtility.SetDirty(mat);
                            }
                        }
                    }
                    break;
                }
                case ShaderUtil.ShaderPropertyType.TexEnv:
                {
                    Texture currentTex = sample.GetTexture(propName);
                    Texture newTex = EditorGUILayout.ObjectField(currentTex, typeof(Texture), false) as Texture;
                    if (!preserve && newTex != currentTex)
                    {
                        foreach (Material mat in materialsByShader[currentShader])
                        {
                            if(mat.HasProperty(propName))
                            {
                                mat.SetTexture(propName, newTex);
                                EditorUtility.SetDirty(mat);
                            }
                        }
                    }
                    break;
                }
            }
            EditorGUI.EndDisabledGroup();
            bool newPreserve = EditorGUILayout.ToggleLeft("Preserve", preserve, GUILayout.Width(100));
            if (newPreserve != preserve)
                SetProjectPreserveFlag(manager, currentShader, propName, newPreserve);
            EditorGUILayout.EndHorizontal();
        }
        
        // ---- Draw Built-in Material Inspector ----
        EditorGUILayout.Space();
        if (currentMatEditor == null)
        {
            EditorGUILayout.HelpBox("No MaterialEditor found for this shader.", MessageType.Warning);
        }
        else
        {
            currentMatEditor.serializedObject.Update();
            currentMatEditor.OnInspectorGUI();
            currentMatEditor.serializedObject.ApplyModifiedProperties();
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
        if (!materialsByShader.ContainsKey(currentShader) || materialsByShader[currentShader].Count == 0)
        {
            Debug.LogWarning("No materials found for shader " + currentShader.name);
            return;
        }
        Material sample = materialsByShader[currentShader][0];
        ShaderPreset preset = ScriptableObject.CreateInstance<ShaderPreset>();
        preset.presetName = currentShader.name + " Preset";
        preset.shader = currentShader;
        int propertyCount = ShaderUtil.GetPropertyCount(currentShader);
        for (int i = 0; i < propertyCount; i++)
        {
            if (ShaderUtil.IsShaderPropertyHidden(currentShader, i))
                continue;
            string propName = ShaderUtil.GetPropertyName(currentShader, i);
            ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(currentShader, i);
            ShaderPropertyEntry entry = new ShaderPropertyEntry();
            entry.propertyName = propName;
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
        string path = EditorUtility.SaveFilePanelInProject("Save Shader Preset", preset.presetName, "asset", "Enter file name to save the preset to");
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
        // If the preset's shader differs, automatically change materials.
        if (preset.shader != currentShader)
        {
            List<Material> mats = materialsByShader[currentShader];
            foreach (Material mat in mats)
            {
                Material temp = new Material(preset.shader);
                temp.CopyPropertiesFromMaterial(mat);
                mat.shader = preset.shader;
                mat.CopyPropertiesFromMaterial(temp);
                /*TransferProperties(mat, temp, (ShaderManager)target);
                mat.shader = preset.shader;
                TransferProperties(temp, mat, (ShaderManager)target);*/
                EditorUtility.SetDirty(mat);
            }
            UpdateShaderList();
            currentShader = preset.shader;
        }
        if (!materialsByShader.ContainsKey(currentShader))
        {
            Debug.LogWarning("No materials found for shader " + currentShader.name);
            return;
        }
        foreach (Material mat in materialsByShader[currentShader])
        {
            foreach (ShaderPropertyEntry entry in preset.propertyEntries)
            {
                // Skip if the property is marked to be preserved.
                if (GetProjectPreserveFlag((ShaderManager)target, currentShader, entry.propertyName))
                    continue;
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