// Place this script in an "Editor" folder (e.g., Assets/Shader Management System/Editor/ShaderManagerEditor.cs)
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(ShaderManager))]
public class ShaderManagerEditor : Editor
{
    // Dictionary to map each unique shader to the list of materials using it.
    private Dictionary<Shader, List<Material>> shaderMaterials = new Dictionary<Shader, List<Material>>();
    private Shader[] shaders;          // Array of unique shaders
    private string[] shaderNames;      // Names for tab view
    private int selectedTab = 0;       // Currently selected tab index
    private Vector2 scrollPos;         // Scroll position for property listing

    private void OnEnable()
    {
        UpdateShaderList();
    }

    // Collect all Renderers from the target's children and build the shader dictionary.
    private void UpdateShaderList()
    {
        shaderMaterials.Clear();
        ShaderManager manager = (ShaderManager)target;
        Renderer[] renderers = manager.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            // Use sharedMaterials so that changes are persistent.
            foreach (Material mat in rend.sharedMaterials)
            {
                if (mat == null) continue;
                Shader shader = mat.shader;
                if (shader == null) continue;
                if (!shaderMaterials.ContainsKey(shader))
                {
                    shaderMaterials.Add(shader, new List<Material>());
                }
                if (!shaderMaterials[shader].Contains(mat))
                {
                    shaderMaterials[shader].Add(mat);
                }
            }
        }
        shaders = new Shader[shaderMaterials.Count];
        shaderMaterials.Keys.CopyTo(shaders, 0);

        shaderNames = new string[shaders.Length];
        for (int i = 0; i < shaders.Length; i++)
        {
            shaderNames[i] = shaders[i].name;
        }
    }

    public override void OnInspectorGUI()
    {
        // Button to manually refresh the list
        if (GUILayout.Button("Refresh Shader List"))
        {
            UpdateShaderList();
        }

        // Phase 1: If no shaders were found, display a message.
        if (shaderMaterials.Count == 0)
        {
            EditorGUILayout.LabelField("No shaders found in child objects.");
            return;
        }

        // Phase 1 & 2: Create a tab view for each unique shader.
        selectedTab = GUILayout.Toolbar(selectedTab, shaderNames);

        // Begin scroll view for shader property listing.
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        Shader selectedShader = shaders[selectedTab];
        EditorGUILayout.LabelField("Shader: " + selectedShader.name, EditorStyles.boldLabel);

        // Retrieve the number of properties the shader exposes.
        int propertyCount = ShaderUtil.GetPropertyCount(selectedShader);
        for (int i = 0; i < propertyCount; i++)
        {
            // Only list public variables (skip hidden properties)
            if (ShaderUtil.IsShaderPropertyHidden(selectedShader, i))
                continue;

            string propName = ShaderUtil.GetPropertyName(selectedShader, i);
            string publicName = ShaderUtil.GetPropertyDescription(selectedShader, i);
            ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(selectedShader, i);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(propName, GUILayout.MaxWidth(150));
            EditorGUILayout.LabelField(publicName, GUILayout.MaxWidth(150));

            // Get the current value from the first material that uses this shader.
            Material sampleMaterial = shaderMaterials[selectedShader][0];
            switch (propType)
            {
                case ShaderUtil.ShaderPropertyType.Color:
                    Color currentColor = sampleMaterial.GetColor(propName);
                    Color newColor = EditorGUILayout.ColorField(currentColor);
                    if (newColor != currentColor)
                    {
                        // Phase 3: Update this property for all materials using the shader.
                        foreach (Material m in shaderMaterials[selectedShader])
                        {
                            m.SetColor(propName, newColor);
                            EditorUtility.SetDirty(m);
                        }
                    }
                    break;

                case ShaderUtil.ShaderPropertyType.Vector:
                    Vector4 currentVector = sampleMaterial.GetVector(propName);
                    Vector4 newVector = EditorGUILayout.Vector4Field("", currentVector);
                    if (newVector != currentVector)
                    {
                        foreach (Material m in shaderMaterials[selectedShader])
                        {
                            m.SetVector(propName, newVector);
                            EditorUtility.SetDirty(m);
                        }
                    }
                    break;

                case ShaderUtil.ShaderPropertyType.Float:
                case ShaderUtil.ShaderPropertyType.Range:
                    float currentFloat = sampleMaterial.GetFloat(propName);
                    float newFloat = EditorGUILayout.FloatField(currentFloat);
                    if (newFloat != currentFloat)
                    {
                        foreach (Material m in shaderMaterials[selectedShader])
                        {
                            m.SetFloat(propName, newFloat);
                            EditorUtility.SetDirty(m);
                        }
                    }
                    break;

                case ShaderUtil.ShaderPropertyType.TexEnv:
                    Texture currentTex = sampleMaterial.GetTexture(propName);
                    Texture newTex = EditorGUILayout.ObjectField(currentTex, typeof(Texture), false) as Texture;
                    if (newTex != currentTex)
                    {
                        foreach (Material m in shaderMaterials[selectedShader])
                        {
                            m.SetTexture(propName, newTex);
                            EditorUtility.SetDirty(m);
                        }
                    }
                    break;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        // Optionally, auto-refresh the list if GUI changes.
        if (GUI.changed)
        {
            UpdateShaderList();
        }
    }
}