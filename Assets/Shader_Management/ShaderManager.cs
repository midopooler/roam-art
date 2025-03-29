using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using static ShaderStylePreset;

[ExecuteInEditMode]
public class ShaderManager : MonoBehaviour
{
    [SerializeField] public List<ShaderStylePreset> stylePresets = new List<ShaderStylePreset>();
    [SerializeField] private ShaderStylePreset currentStyle;
    [SerializeField] private bool preserveOriginalColors = true;

    [SerializeField] private List<GameObject> selectedObjects = new List<GameObject>();
    private Dictionary<Renderer, List<MaterialTracker>> materialTrackers = new Dictionary<Renderer, List<MaterialTracker>>();
    private Dictionary<Renderer, Material[]> currentMaterials = new Dictionary<Renderer, Material[]>();

    private void Awake()
    {
        InitializeMaterialTracking();
    }

    private void InitializeMaterialTracking()
    {
        if (selectedObjects.Count == 0)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                TrackRenderer(renderer);
            }
        }
        else
        {
            foreach (var obj in selectedObjects)
            {
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    TrackRenderer(renderer);
                }
            }
        }
    }

    private void TrackRenderer(Renderer renderer)
    {
        List<MaterialTracker> trackers = new List<MaterialTracker>();
        foreach (Material material in renderer.sharedMaterials)
        {
            MaterialTracker tracker = MaterialTracker.CreateFromMaterial(material);
            trackers.Add(tracker);
            Debug.Log($"Tracking material: {material.name} for {renderer.name}");
            Debug.Log($"IsShared: {tracker.IsShared}, HasSpecialTexture: {tracker.HasSpecialTexture}");
        }
        materialTrackers[renderer] = trackers;
    }

    public void SelectObjects(params GameObject[] objects)
    {
        selectedObjects.Clear();
        foreach (var obj in objects)
        {
            selectedObjects.Add(obj);
        }
        InitializeMaterialTracking();
    }

    public void ApplyStyle(ShaderStylePreset preset)
    {
        if (preset == null) return;

        Debug.Log($"Applying style: {preset.StyleName}");
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        int materialsModified = 0;
        
        foreach (Renderer renderer in renderers)
        {
            if (!materialTrackers.TryGetValue(renderer, out List<MaterialTracker> trackers))
                continue;

            for (int i = 0; i < trackers.Count; i++)
            {
                MaterialTracker tracker = trackers[i];
                if (tracker.OriginalMaterial != null && 
                    tracker.OriginalMaterial.shader == preset.TargetShader && 
                    !tracker.HasSpecialTexture)
                {
                    preset.ApplyToMaterial(tracker.OriginalMaterial);
                    materialsModified++;
                    Debug.Log($"Applied style to material: {tracker.OriginalMaterial.name} on {renderer.name}");
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
            if (materialTrackers.TryGetValue(renderer, out List<MaterialTracker> trackers))
            {
                for (int i = 0; i < trackers.Count; i++)
                {
                    MaterialTracker tracker = trackers[i];
                    if (tracker.OriginalMaterial != null)
                    {
                        renderer.materials[i] = tracker.OriginalMaterial;
                        materialsReset++;
                        Debug.Log($"Reset material for {renderer.name}");
                    }
                }
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

    public void ShowPropertyChanges(ShaderStylePreset preset)
    {
        if (preset == null) return;

        Debug.Log($"Property changes for style: {preset.StyleName}");
        foreach (var prop in preset.Properties)
        {
            Debug.Log($"Property: {prop.PropertyName}");
            Debug.Log($"  Type: {GetPropertyType(prop)}");
            Debug.Log($"  Current Value: {GetCurrentValue(prop)}");
            Debug.Log($"  New Value: {GetNewValue(prop)}");
        }
    }

    private string GetPropertyType(ShaderProperty prop)
    {
        if (prop.IsColor) return "Color";
        if (prop.IsVector) return "Vector";
        if (prop.IsFloat) return "Float";
        if (prop.IsTexture) return "Texture";
        return "Unknown";
    }

    private string GetCurrentValue(ShaderProperty prop)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (materialTrackers.TryGetValue(renderer, out List<MaterialTracker> trackers))
            {
                foreach (var tracker in trackers)
                {
                    if (tracker.OriginalMaterial != null)
                    {
                        switch (GetPropertyType(prop))
                        {
                            case "Color":
                                return tracker.OriginalMaterial.GetColor(prop.PropertyName).ToString();
                            case "Vector":
                                return tracker.OriginalMaterial.GetVector(prop.PropertyName).ToString();
                            case "Float":
                                return tracker.OriginalMaterial.GetFloat(prop.PropertyName).ToString();
                            case "Texture":
                                return tracker.OriginalMaterial.GetTexture(prop.PropertyName)?.name ?? "None";
                        }
                    }
                }
            }
        }
        return "Not found";
    }

    private string GetNewValue(ShaderProperty prop)
    {
        switch (GetPropertyType(prop))
        {
            case "Color":
                return prop.ColorValue.ToString();
            case "Vector":
                return prop.VectorValue.ToString();
            case "Float":
                return prop.FloatValue.ToString();
            case "Texture":
                return prop.TextureValue?.name ?? "None";
            default:
                return "Unknown";
        }
    }
}