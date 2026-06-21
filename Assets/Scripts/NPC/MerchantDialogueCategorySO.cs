using UnityEngine;

/// <summary>
/// ScriptableObject that defines a category of merchant dialogue lines.
/// Used by NPCAudio to organize dialogues by context (open shop, close, AFK, etc.).
/// </summary>
[CreateAssetMenu(fileName = "MerchantDialogueCategory", menuName = "Deadzone/Merchant Dialogue Category")]
public class MerchantDialogueCategorySO : ScriptableObject {
    [Tooltip("Category name for identification")]
    public string categoryName;

    [Tooltip("List of dialogue lines in this category")]
    public MerchantDialogueLine[] dialogues;

    [Tooltip("If false, prevents the same line from playing twice in a row")]
    public bool allowImmediateRepeat = false;
}

/// <summary>
/// Represents a single dialogue line with an audio clip and optional subtitle text.
/// </summary>
[System.Serializable]
public struct MerchantDialogueLine {
    [Tooltip("Audio clip that will be played for this line")]
    public AudioClip clip;

    [Tooltip("Manual subtitle text shown on screen while this line plays")]
    [TextArea(2, 4)]
    public string subtitle;
}
