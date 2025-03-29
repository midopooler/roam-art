using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field)]
public class ShaderPropertyAttribute : PropertyAttribute
{
    public string Description { get; private set; }
    public string VisualImpact { get; private set; }
    public string Category { get; private set; }
    public string ValueDescription { get; private set; }

    public ShaderPropertyAttribute(string description, string visualImpact, string category)
    {
        Description = description;
        VisualImpact = visualImpact;
        Category = category;
    }

    public ShaderPropertyAttribute(string description, string visualImpact, string category, string valueDescription)
    {
        Description = description;
        VisualImpact = visualImpact;
        Category = category;
        ValueDescription = valueDescription;
    }
}