using UnityEngine;
using System.Collections.Generic;

public enum PresetShaderPropertyType
{
    Float,
    Color,
    Vector,
    Texture,
    Bool
}

[System.Serializable]
public class ShaderPropertyEntry
{
    public string propertyName;
    public PresetShaderPropertyType type;
    public float floatValue;
    public Color colorValue;
    public Vector4 vectorValue;
    public Texture textureValue;
    public bool boolValue; 
}

[System.Serializable]
[CreateAssetMenu(menuName = "Shader Preset", fileName = "NewShaderPreset")]
public class ShaderPreset : ScriptableObject
{
    public string presetName;
    public Shader shader;
    public List<ShaderPropertyEntry> propertyEntries = new List<ShaderPropertyEntry>();
}