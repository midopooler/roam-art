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
       [ShaderProperty("The main color of the material", "Changes the overall color of the object", "Color")]
        public string PropertyName;
        
        [ShaderProperty("Color value", "Changes the color of the material", "Color", "RGB values determine the color")]
        public Color ColorValue;
        
        [ShaderProperty("Vector value", "Controls directional properties", "Vector", "XYZW values control different aspects")]
        public Vector4 VectorValue;
        
        [ShaderProperty("Float value", "Controls intensity or scale", "Float", "0-1 range controls intensity")]
        public float FloatValue;
        
        [ShaderProperty("Texture value", "Applies textures to the material", "Texture", "Selects the texture to use")]
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