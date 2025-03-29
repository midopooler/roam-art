using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class ShaderSelectionWindow : EditorWindow
{
    private Action<Shader> onShaderSelected;
    private string searchText = "";
    private Vector2 scrollPos;
    // Group shaders by category (prefix before slash)
    private Dictionary<string, List<Shader>> groupedShaders = new Dictionary<string, List<Shader>>();
    // Store foldout states per group
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
    private Shader currentShader;

    public static void Show(Shader currentShader, Action<Shader> onShaderSelected)
    {
        ShaderSelectionWindow window = GetWindow<ShaderSelectionWindow>(true, "Select Shader", true);
        window.onShaderSelected = onShaderSelected;
        window.currentShader = currentShader;
        window.LoadShaders();
        window.ShowAuxWindow();
    }

    private void LoadShaders()
    {
        groupedShaders.Clear();
        foldoutStates.Clear();

        // Get all shaders in the project via AssetDatabase
        string[] guids = AssetDatabase.FindAssets("t:Shader");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader != null)
            {
                // Determine group: if the name contains '/', use the first part; otherwise, use "Uncategorized"
                string group = shader.name.Contains("/") ? shader.name.Split('/')[0] : "Uncategorized";

                if (!groupedShaders.ContainsKey(group))
                {
                    groupedShaders[group] = new List<Shader>();
                    foldoutStates[group] = false; // default closed
                }
                groupedShaders[group].Add(shader);
            }
        }

        // Sort shaders within each group by full shader name
        foreach (var group in groupedShaders.Keys.ToList())
        {
            groupedShaders[group] = groupedShaders[group].OrderBy(s => s.name).ToList();
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Select a Shader", EditorStyles.boldLabel);
        searchText = EditorGUILayout.TextField("Search", searchText);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var groupPair in groupedShaders)
        {
            // Filter shaders in the group based on search text (case insensitive)
            List<Shader> filteredShaders = groupPair.Value
                .Where(s => string.IsNullOrEmpty(searchText) || s.name.ToLower().Contains(searchText.ToLower()))
                .ToList();
            if (filteredShaders.Count == 0)
                continue;

            // Draw group header as foldout
            foldoutStates[groupPair.Key] = EditorGUILayout.Foldout(foldoutStates[groupPair.Key], groupPair.Key, true);
            if (foldoutStates[groupPair.Key])
            {
                EditorGUI.indentLevel++;
                foreach (Shader shader in filteredShaders)
                {
                    // Remove group prefix for display if present
                    string displayName = shader.name;
                    if (shader.name.Contains("/"))
                    {
                        string[] parts = shader.name.Split('/');
                        displayName = string.Join("/", parts.Skip(1).ToArray());
                    }

                    GUIStyle buttonStyle = (shader == currentShader)
                        ? new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold }
                        : GUI.skin.button;

                    if (GUILayout.Button(displayName, buttonStyle))
                    {
                        onShaderSelected?.Invoke(shader);
                        Close();
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndScrollView();
    }
}