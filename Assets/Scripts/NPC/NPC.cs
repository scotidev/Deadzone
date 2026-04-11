using UnityEngine;

/// <summary>
/// Represents the NPC that can be interacted with to open the shop interface.
/// </summary>
public class NPC : Interactable {
    [SerializeField] private string npcName = "Merchant";
    [SerializeField] private NPCAudio npcAudio;

    /// <summary>
    /// Resolves optional component references.
    /// </summary>
    private void Awake()
    {
        // First principle: GetComponent lookup at startup avoids repeated lookups during interaction.
        if (npcAudio == null)
            npcAudio = GetComponent<NPCAudio>();
    }

    /// <returns>The NPC's name as a string.</returns>
    public string GetNPCName() => npcName;

    /// <summary>
    /// Opens the shop interface through the ShopInterface singleton when interacted with.
    /// </summary>
    public override void Interact()
    {
        if (ShopManager.Instance != null)
        {
            // First principle: dialogue is immediate player feedback, so play it at the interaction moment.
            npcAudio?.PlayRandomShopOpenDialogue();
            ShopManager.Instance.OpenShop();
        }
    }
}
