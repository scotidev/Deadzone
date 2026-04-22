using UnityEngine;

/// <summary>
/// Represents the NPC that can be interacted with to open the shop interface.
/// </summary>
public class NPC : Interactable {

    #region SERIALIZED FIELDS

    [SerializeField] private string npcName = "Merchant";
    [SerializeField] private NPCAudio npcAudio;

    #endregion

    #region UNITY

    private void Awake() {
        if (npcAudio == null)
            npcAudio = GetComponent<NPCAudio>();
    }

    #endregion

    #region METHODS

    /// <returns>The NPC's name as a string.</returns>
    public string GetNPCName() => npcName;

    /// <summary>
    /// Opens the shop interface through the ShopInterface singleton when interacted with.
    /// </summary>
    public override void Interact() {
        if (ShopManager.Instance != null) {
            npcAudio?.PlayRandomShopOpenDialogue();
            ShopManager.Instance.OpenShop();
        }
    }

    #endregion
}
