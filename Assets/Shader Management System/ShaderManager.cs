using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class PreserveFlagData
{
    public string shaderName;
    public string propertyName;
    public bool preserve;
}

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
    public List<string> aliases;
    public string propertyType;
}

[System.Serializable]
public class ShaderPropertyReferenceData
{
    public List<ShaderPropertyMetadata> properties;
}

/// <summary>
/// ShaderManager handles core functionality:
///  • Collects all child materials
///  • Changes shader on all child materials
///  • Applies presets (including GPU Instancing and checking per‑property “preserve” flags)
///  • Saves presets (Editor: file dialog; Runtime: placeholder for server upload)
///  
/// It also integrates JSON mapping functionality (via ShaderPropertyReferenceLoader)
/// so that properties with different names can be mapped. (When running in the Editor,
/// the mapping uses ShaderUtil; in a runtime build the mapping falls back to a simple approach.)
/// </summary>
public class ShaderManager : MonoBehaviour
{
    // Serialized preserve flags – stored with the scene/prefab.
    public List<PreserveFlagData> preserveFlags = new List<PreserveFlagData>();

    // Runtime list of child materials.
    [HideInInspector]
    public List<Material> childMaterials = new List<Material>();

    #region Child Material Collection

    /// <summary>
    /// Collects all child materials from Renderers.
    /// </summary>
    public void CollectChildMaterials()
    {
        childMaterials.Clear();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;
            foreach (Material mat in rend.sharedMaterials)
            {
                if (mat != null && !childMaterials.Contains(mat))
                    childMaterials.Add(mat);
            }
        }
    }

    #endregion

    #region Shader Changing with JSON Mapping

    /// <summary>
    /// Changes the shader on all collected materials to newShader.
    /// Uses TransferProperties to map properties based on JSON mapping.
    /// </summary>
    public void ChangeShader(Shader newShader)
    {
        CollectChildMaterials();
        foreach (Material mat in childMaterials)
        {
            if (mat == null) continue;
            // Create a temporary material using the new shader.
            Material temp = new Material(newShader);
            // Transfer properties from the current material to temp using JSON mapping.
            TransferProperties(mat, temp);
            // Change the shader on the original material.
            mat.shader = newShader;
            // Transfer the properties from temp back to the material.
            TransferProperties(temp, mat);
        }
    }

    /// <summary>
    /// Transfers property values from source material to target material using JSON mapping.
    /// This method uses the JSON reference data (loaded via ShaderPropertyReferenceLoader) to
    /// determine the canonical property names and aliases, then copies values from source to target.
    /// </summary>
    public void TransferProperties(Material source, Material target)
    {
        ShaderPropertyReferenceData referenceData = ShaderPropertyReferenceLoader.LoadReferenceData();
        if (referenceData == null || referenceData.properties == null)
        {
            Debug.LogWarning("No JSON mapping data found.");
            return;
        }
        foreach (ShaderPropertyMetadata meta in referenceData.properties)
        {
            // Build a combined list of property names: canonical + aliases.
            List<string> allNames = new List<string>();
            if (meta.aliases != null)
            {
                foreach (string alias in meta.aliases)
                {
                    if (!allNames.Contains(alias))
                        allNames.Add(alias);
                }
            }
            // Find first name that exists in the source material.
            string sourceProp = null;
            foreach (string name in allNames)
            {
                if (source.HasProperty(name))
                {
                    sourceProp = name;
                    break;
                }
            }
            if (string.IsNullOrEmpty(sourceProp))
                continue;
            // Determine the target property name using mapping.
            string targetProp = GetMappedPropertyName(source.shader, target.shader, sourceProp);
            if (string.IsNullOrEmpty(targetProp))
                continue;

            // Transfer the property based on the property type from the JSON mapping.
            string pType = meta.propertyType.ToLower();
            try
            {
                if (pType == "float")
                {
                    float val = source.GetFloat(sourceProp);
                    target.SetFloat(targetProp, val);
                }
                else if (pType == "color")
                {
                    Color col = source.GetColor(sourceProp);
                    target.SetColor(targetProp, col);
                }
                else if (pType == "vector")
                {
                    Vector4 vec = source.GetVector(sourceProp);
                    target.SetVector(targetProp, vec);
                }
                else if (pType == "texture")
                {
                    Texture tex = source.GetTexture(sourceProp);
                    target.SetTexture(targetProp, tex);
                }
                else if (pType == "bool")
                {
                    // Assume bool stored as float 0 or 1.
                    float f = source.GetFloat(sourceProp);
                    bool b = Mathf.Approximately(f, 1f);
                    // For GPU Instancing, use target.enableInstancing.
                    if (targetProp == "EnableGPUInstancing")
                    {
                        target.enableInstancing = b;
                    }
                    else
                    {
                        target.SetFloat(targetProp, b ? 1f : 0f);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Error transferring property '{sourceProp}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Returns the mapped property name in the target shader based on the JSON reference.
    /// If no mapping is found, returns sourcePropName if the target shader has it.
    /// </summary>
    public string GetMappedPropertyName(Shader sourceShader, Shader targetShader, string sourcePropName)
    {
        var referenceData = ShaderPropertyReferenceLoader.LoadReferenceData();
        ShaderPropertyMetadata mapping = null;
        foreach (ShaderPropertyMetadata meta in referenceData.properties)
        {
            if ((meta.aliases != null && meta.aliases.Contains(sourcePropName)))
            {
                mapping = meta;
                break;
            }
        }
        if (mapping != null)
        {
            List<string> allNames = new List<string>();
            if (mapping.aliases != null)
            {
                foreach (string alias in mapping.aliases)
                {
                    if (!allNames.Contains(alias))
                        allNames.Add(alias);
                }
            }
#if UNITY_EDITOR
            foreach (string name in allNames)
            {
                if (ShaderHasProperty(targetShader, name))
                    return name;
            }
            return null;
#else
            return ShaderHasProperty(targetShader, sourcePropName) ? sourcePropName : null;
#endif
        }
#if UNITY_EDITOR
        return ShaderHasProperty(targetShader, sourcePropName) ? sourcePropName : null;
#else
        return ShaderHasProperty(targetShader, sourcePropName) ? sourcePropName : null;
#endif
    }

    /// <summary>
    /// Checks if the shader has a given property.
    /// Uses ShaderUtil in the Editor; at runtime, creates a temporary material.
    /// </summary>
    public bool ShaderHasProperty(Shader shader, string propName)
    {
#if UNITY_EDITOR
        int count = ShaderUtil.GetPropertyCount(shader);
        for (int i = 0; i < count; i++)
        {
            if (ShaderUtil.GetPropertyName(shader, i) == propName)
                return true;
        }
        return false;
#else
        Material temp = new Material(shader);
        bool hasProp = temp.HasProperty(propName);
        Destroy(temp);
        return hasProp;
#endif
    }

    #endregion

    #region Preset Application

    /// <summary>
    /// Applies the given preset to all collected materials.
    /// If the preset's shader differs from the current shader, it first changes the shader.
    /// It then iterates over the preset entries and applies each property,
    /// skipping those marked as preserved and those that match retain mappings.
    /// GPU Instancing (a Bool entry with propertyName "EnableGPUInstancing") is handled separately.
    /// </summary>
    public void ApplyPreset(ShaderPreset preset)
    {
        CollectChildMaterials();
        if (childMaterials.Count == 0)
        {
            Debug.LogWarning("No child materials found!");
            return;
        }
        // Use the shader of the first material.
        Shader currentShader = childMaterials[0].shader;
        if (preset.shader != currentShader)
        {
            ChangeShader(preset.shader);
            CollectChildMaterials();
            currentShader = preset.shader;
        }

        foreach (Material mat in childMaterials)
        {
            foreach (ShaderPropertyEntry entry in preset.propertyEntries)
            {
                // Handle GPU Instancing separately.
                if (entry.type == PresetShaderPropertyType.Bool && entry.propertyName == "EnableGPUInstancing")
                {
                    mat.enableInstancing = entry.boolValue;
                    EditorUtility.SetDirty(mat);
                    continue;
                }
                
                // Skip if this property is marked as preserved.
                if (GetPreserveFlag(mat.shader, entry.propertyName))
                    continue;
                
                // **New:** If this property is one of the "retain" mappings, skip applying its preset value.
                if (IsRetainMapping(entry.propertyName))
                    continue;
                
                // If the material doesn’t have the property, skip.
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
                EditorUtility.SetDirty(mat);
            }
        }
    }

    /// <summary>
    /// Checks if the given property name matches one of the known aliases for BaseColor or BaseMap,
    /// indicating that this property should not be updated by preset application.
    /// Adjust the list as needed.
    /// </summary>
    private bool IsRetainMapping(string propName)
    {
        // List of property names (aliases) that we want to lock and not update from presets.
        string[] retainProps = new string[] { "_BaseColor", "_AlbedoColor", "_Color", "_BaseMap", "_AlbedoMap", "_MainTex" };
        foreach (string s in retainProps)
        {
            if (s == propName)
                return true;
        }
        return false;
    }

    #endregion

    #region Preset Saving

    /// <summary>
    /// Saves the given preset.
    /// In the Editor, it automatically saves the preset asset in the folder
    /// "Assets/Shader Management System/presets" using the GameObject's name.
    /// At runtime, it uploads the preset to a server (placeholder).
    /// </summary>
    public void SavePreset(ShaderPreset preset)
    {
    #if UNITY_EDITOR
        // Ensure we have collected child materials.
        CollectChildMaterials();
        Material sample = null;
        // Try to get a sample from the current GameObject's Renderer first.
        Renderer selfRenderer = GetComponent<Renderer>();
        if (selfRenderer != null)
        {
            foreach (Material mat in selfRenderer.sharedMaterials)
            {
                if (mat != null && mat.shader == preset.shader)
                {
                    sample = mat;
                    break;
                }
            }
        }
        // If not found, use a child material.
        if (sample == null)
        {
            foreach (Material mat in childMaterials)
            {
                if (mat != null && mat.shader == preset.shader)
                {
                    sample = mat;
                    break;
                }
            }
        }
        if (sample == null)
        {
            Debug.LogWarning("No material found using the selected shader (" + preset.shader.name + ").");
            return;
        }
        
        int propertyCount = ShaderUtil.GetPropertyCount(sample.shader);
        preset.propertyEntries = new List<ShaderPropertyEntry>();
        for (int i = 0; i < propertyCount; i++)
        {
            if (ShaderUtil.IsShaderPropertyHidden(sample.shader, i))
                continue;
            
            string propName = ShaderUtil.GetPropertyName(sample.shader, i);
            if (!sample.HasProperty(propName))
                continue;
            
            ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(sample.shader, i);
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
            }
            preset.propertyEntries.Add(entry);
        }
        
        // Add GPU Instancing as a Bool entry.
        ShaderPropertyEntry instancingEntry = new ShaderPropertyEntry();
        instancingEntry.propertyName = "EnableGPUInstancing"; // Special name.
        instancingEntry.type = PresetShaderPropertyType.Bool;
        instancingEntry.boolValue = sample.enableInstancing;
        preset.propertyEntries.Add(instancingEntry);

        // Define the folder path.
        string folderPath = "Assets/Shader Management System/presets/";
        // Ensure the folder exists.
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Shader Management System", "presets");
        }
        // Build a filename using the GameObject's name and the preset name.
        string fileName = gameObject.name + RandomString() + ".asset";
        string path = folderPath + fileName;
        
        AssetDatabase.CreateAsset(preset, path);
        AssetDatabase.SaveAssets();
        Debug.Log("Preset saved at " + path);
    #else
        // At runtime: Implement your server upload logic here.
        Debug.Log("SavePreset called at runtime. Implement server upload logic here.");
    #endif
    }

    public string RandomString(int length = 4)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        char[] stringChars = new char[length];
        for (int i = 0; i < length; i++)
        {
            stringChars[i] = chars[Random.Range(0, chars.Length)];
        }
        return new string(stringChars);
    }
        


    #endregion

    #region JSON Preset Functionality

    /// <summary>
    /// Saves the current preset data as a JSON file.
    /// </summary>
    public void SavePresetAsJson(ShaderPreset preset)
    {
    #if UNITY_EDITOR
        // Populate preset data similar to the existing SavePreset method.
        CollectChildMaterials();
        Material sample = null;
        Renderer selfRenderer = GetComponent<Renderer>();
        if (selfRenderer != null)
        {
            foreach (Material mat in selfRenderer.sharedMaterials)
            {
                if (mat != null && mat.shader == preset.shader)
                {
                    sample = mat;
                    break;
                }
            }
        }
        if (sample == null)
        {
            foreach (Material mat in childMaterials)
            {
                if (mat != null && mat.shader == preset.shader)
                {
                    sample = mat;
                    break;
                }
            }
        }
        if (sample == null)
        {
            Debug.LogWarning("No material found using the selected shader (" + preset.shader.name + ").");
            return;
        }
        
        int propertyCount = ShaderUtil.GetPropertyCount(sample.shader);
        preset.propertyEntries = new List<ShaderPropertyEntry>();
        for (int i = 0; i < propertyCount; i++)
        {
            if (ShaderUtil.IsShaderPropertyHidden(sample.shader, i))
                continue;
                
            string propName = ShaderUtil.GetPropertyName(sample.shader, i);
            if (!sample.HasProperty(propName))
                continue;
                
            ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(sample.shader, i);
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
            }
            preset.propertyEntries.Add(entry);
        }
        
        // Add GPU Instancing as a Bool entry.
        ShaderPropertyEntry instancingEntry = new ShaderPropertyEntry();
        instancingEntry.propertyName = "EnableGPUInstancing"; // Special name.
        instancingEntry.type = PresetShaderPropertyType.Bool;
        instancingEntry.boolValue = sample.enableInstancing;
        preset.propertyEntries.Add(instancingEntry);

        // Serialize the preset to JSON.
        string json = JsonUtility.ToJson(preset, true);
        string fileName = gameObject.name + "_preset.json";
        string path = EditorUtility.SaveFilePanel("Save JSON Preset", "Assets/Shader Management System/presets/", fileName, "json");
        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, json);
            Debug.Log("JSON Preset saved at " + path);
        }
    #else
        Debug.Log("SavePresetAsJson called at runtime. JSON file saving is not supported in runtime builds.");
    #endif
    }

    /// <summary>
    /// Loads a preset from a JSON TextAsset and applies it.
    /// If parsing fails or the preset is invalid, logs an error.
    /// </summary>
    public void ApplyPresetFromJsonAsset(TextAsset jsonAsset)
    {
    #if UNITY_EDITOR
        if (jsonAsset == null)
        {
            Debug.LogError("No JSON asset provided.");
            return;
        }
        try
        {
            ShaderPreset preset = ScriptableObject.CreateInstance<ShaderPreset>();
            JsonUtility.FromJsonOverwrite(jsonAsset.text, preset);
            if (preset.shader == null)
            {
                Debug.LogError("Error parsing JSON: preset shader is null. The JSON may be invalid or missing required data.");
                return;
            }
            ApplyPreset(preset);
            Debug.Log("Applied JSON preset from asset: " + jsonAsset.name);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error parsing JSON preset: " + ex.Message);
        }
    #else
        Debug.Log("ApplyPresetFromJsonAsset called at runtime. JSON file loading is not supported in runtime builds.");
    #endif
    }

    #endregion


    #region Preserve Flag Management

    /// <summary>
    /// Gets the preserve flag for a given shader property.
    /// </summary>
    public bool GetPreserveFlag(Shader shader, string propName)
    {
        foreach (PreserveFlagData entry in preserveFlags)
        {
            if (entry.shaderName == shader.name && entry.propertyName == propName)
                return entry.preserve;
        }
        return false;
    }

    /// <summary>
    /// Sets the preserve flag for a given shader property.
    /// </summary>
    public void SetPreserveFlag(Shader shader, string propName, bool value)
    {
        foreach (PreserveFlagData entry in preserveFlags)
        {
            if (entry.shaderName == shader.name && entry.propertyName == propName)
            {
                entry.preserve = value;
                return;
            }
        }
        PreserveFlagData newEntry = new PreserveFlagData();
        newEntry.shaderName = shader.name;
        newEntry.propertyName = propName;
        newEntry.preserve = value;
        preserveFlags.Add(newEntry);
    }

    #endregion
}