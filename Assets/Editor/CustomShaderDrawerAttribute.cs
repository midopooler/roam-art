using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field)]
public class CustomShaderDrawerAttribute : PropertyAttribute
{
    public CustomShaderDrawerAttribute() { }
}