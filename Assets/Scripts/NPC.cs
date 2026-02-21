using UnityEngine;

/// <summary>
/// Represents the NPC that can be interacted with to open the shop interface.
/// </summary>
public class NPC : Interactable {
    [SerializeField] private string npcName = "Merchant";

    /// <returns>The NPC's name as a string.</returns>
    public string GetNPCName() => npcName;

    /// <summary>
    /// Opens the shop interface through the ShopInterface singleton when interacted with.
    /// </summary>
    public override void Interact() {
        if (ShopInterface.Instance != null) {
            ShopInterface.Instance.OpenShop();
        }
    }
}
