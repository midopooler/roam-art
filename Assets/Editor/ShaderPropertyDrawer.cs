using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[CustomPropertyDrawer(typeof(Shader))]
public class ShaderPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get the current shader value
        Shader currentShader = (Shader)property.objectReferenceValue;
        
        // Create a popup with all shaders
        string[] shaderNames = GetAllShaderNames();
        int selectedIndex = 0;
        
        if (currentShader != null)
        {
            for (int i = 0; i < shaderNames.Length; i++)
            {
                if (shaderNames[i] == currentShader.name)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }
        
        int newSelection = EditorGUI.Popup(position, label.text, selectedIndex, shaderNames);
        
        if (newSelection != selectedIndex)
        {
            if (newSelection >= 0 && newSelection < shaderNames.Length)
            {
                Shader newShader = Shader.Find(shaderNames[newSelection]);
                property.objectReferenceValue = newShader;
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }

    private string[] GetAllShaderNames()
    {
        List<string> shaderNames = new List<string>();
        
        // Get all shaders in the project
        string[] shaderPaths = AssetDatabase.FindAssets("t:Shader");
        foreach (string path in shaderPaths)
        {
            string shaderPath = AssetDatabase.GUIDToAssetPath(path);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader != null)
            {
                shaderNames.Add(shader.name);
            }
        }

        return shaderNames.Distinct().ToArray();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label) + 20;
    }
}