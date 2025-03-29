using UnityEngine;
using System.Collections.Generic;
using System;

[ExecuteInEditMode]
public class ShaderManager : MonoBehaviour
{
    [SerializeField] public List<ShaderStylePreset> stylePresets = new List<ShaderStylePreset>();
    [SerializeField] private ShaderStylePreset currentStyle;
    [SerializeField] private bool preserveOriginalColors = true;

    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, Material[]> currentMaterials = new Dictionary<Renderer, Material[]>();

    private void Awake()
    {
        InitializeMaterialTracking();
    }

    private void InitializeMaterialTracking()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            originalMaterials[renderer] = renderer.sharedMaterials;
            currentMaterials[renderer] = renderer.sharedMaterials;
            Debug.Log($"Tracking materials for {renderer.name}");
        }
    }

    public void ApplyStyle(ShaderStylePreset preset)
    {
        if (preset == null) return;

        Debug.Log($"Applying style: {preset.StyleName}");
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        int materialsModified = 0;
        
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && materials[i].shader == preset.TargetShader)
                {
                    preset.ApplyToMaterial(materials[i]);
                    materialsModified++;
                    Debug.Log($"Applied style to material: {materials[i].name} on {renderer.name}");
                }
            }
        }

        if (materialsModified == 0)
        {
            Debug.LogWarning($"No materials found that use the target shader: {preset.TargetShader.name}");
        }
        else
        {
            Debug.Log($"Successfully applied style to {materialsModified} materials");
        }
    }

    public void ResetToOriginal()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        int materialsReset = 0;
        
        foreach (Renderer renderer in renderers)
        {
            if (originalMaterials.TryGetValue(renderer, out Material[] originalMats))
            {
                renderer.sharedMaterials = originalMats;
                materialsReset++;
                Debug.Log($"Reset materials for {renderer.name}");
            }
        }

        if (materialsReset > 0)
        {
            Debug.Log($"Successfully reset {materialsReset} materials to original state");
        }
        else
        {
            Debug.LogWarning("No materials were reset - either no materials were tracked or all materials were already in original state");
        }
    }
}