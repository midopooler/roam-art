using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PresetUIManager : MonoBehaviour
{
    [Header("References")]
    // Reference to your ShaderManager instance in the scene.
    public ShaderManager shaderManager;
    // UI container (e.g., Content GameObject inside a Scroll View) where buttons will be added.
    public Transform buttonContainer;
    // A prefab for a UI button. It should have a Button component and a Text component (or child) for display.
    public GameObject buttonPrefab;

    [Header("Preset Data")]
    // List of available presets. You can populate this list in the inspector,
    // or modify the script to load them dynamically from Resources or another source.
    public List<ShaderPreset> availablePresets = new List<ShaderPreset>();

    void Start()
    {
        // Generate a button for each preset.
        GeneratePresetButtons();
    }

    /// <summary>
    /// Iterates through the availablePresets list and creates a button for each preset.
    /// </summary>
    void GeneratePresetButtons()
    {
        if (buttonContainer == null || buttonPrefab == null)
        {
            Debug.LogError("Button Container or Button Prefab is not assigned.");
            return;
        }

        foreach (ShaderPreset preset in availablePresets)
        {
            // Instantiate the button prefab as a child of the container.
            GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
            // Get the Button component.
            Button button = buttonObj.GetComponent<Button>();
            // Optionally, set the button's label to the preset's name.
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = preset.presetName;
            }
            // Add a listener to apply the preset when the button is clicked.
            button.onClick.AddListener(() => ApplyPreset(preset));
        }
    }

    /// <summary>
    /// Applies the selected preset by calling ShaderManager.ApplyPreset.
    /// </summary>
    /// <param name="preset">The preset to apply.</param>
    void ApplyPreset(ShaderPreset preset)
    {
        if (shaderManager != null && preset != null)
        {
            shaderManager.ApplyPreset(preset);
            Debug.Log("Applied preset: " + preset.presetName);
        }
        else
        {
            Debug.LogError("ShaderManager reference or preset is null.");
        }
    }
}