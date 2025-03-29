using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewShaderStyle", menuName = "Shader Management/Shader Style Preset")]
public class ShaderStylePreset : ScriptableObject
{
    public string StyleName;
    public Shader TargetShader;
    public Material PreviewMaterial;
    
    [System.Serializable]
    public class ShaderProperty
    {
        public string PropertyName;
        public Color ColorValue;
        public Vector4 VectorValue;
        public float FloatValue;
        public Texture TextureValue;
        public bool IsTexture;
        public bool IsColor;
        public bool IsVector;
        public bool IsFloat;
    }

    public List<ShaderProperty> Properties = new List<ShaderProperty>();

    public void ApplyToMaterial(Material material)
    {
        foreach (var prop in Properties)
        {
            if (prop.IsTexture)
            {
                material.SetTexture(prop.PropertyName, prop.TextureValue);
            }
            else if (prop.IsColor)
            {
                material.SetColor(prop.PropertyName, prop.ColorValue);
            }
            else if (prop.IsVector)
            {
                material.SetVector(prop.PropertyName, prop.VectorValue);
            }
            else if (prop.IsFloat)
            {
                material.SetFloat(prop.PropertyName, prop.FloatValue);
            }
        }
    }
}