using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[System.Serializable]
public class MaterialTracker
{
    public Material OriginalMaterial;
    public bool IsShared;
    public bool HasSpecialTexture;
    public string TexturePath;
    
    public static MaterialTracker CreateFromMaterial(Material material)
    {
        MaterialTracker tracker = new MaterialTracker();
        tracker.OriginalMaterial = material;
        tracker.IsShared = IsMaterialShared(material);
        tracker.HasSpecialTexture = CheckForSpecialTexture(material);
        tracker.TexturePath = material.mainTexture != null ? AssetDatabase.GetAssetPath(material.mainTexture) : string.Empty;
        return tracker;
    }
    
    private static bool IsMaterialShared(Material material)
    {
        if (material == null) return false;
        return material.name.Contains("(Instance)");
    }
    
    private static bool CheckForSpecialTexture(Material material)
    {
        if (material == null) return false;
        string texturePath = AssetDatabase.GetAssetPath(material.mainTexture);
        return !string.IsNullOrEmpty(texturePath) && texturePath.Contains("Textures/Special");
    }
}