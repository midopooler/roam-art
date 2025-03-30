using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PreserveFlagData
{
    public string shaderName;
    public string propertyName;
    public bool preserve;
}

public class ShaderManager : MonoBehaviour
{
    // This list stores preserve flags for each shader property.
    // It is serialized and becomes part of your scene or prefab.
    public List<PreserveFlagData> preserveFlags = new List<PreserveFlagData>();
}
