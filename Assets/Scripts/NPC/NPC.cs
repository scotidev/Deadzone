using UnityEngine;

/// <summary>
/// Represents the NPC that can be interacted with to open the shop interface.
/// Also handles proximity detection for dialogue triggers.
/// </summary>
public class NPC : Interactable {

    #region SERIALIZED FIELDS

    [SerializeField] private string npcName = "Merchant";



    #endregion

    #region UNITY

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            GetComponent<NPCAudio>()?.OnPlayerEnteredRange();
        }
    }

    #endregion

    #region METHODS

    public string GetNPCName() => npcName;

    /// <summary>
    /// Opens the shop interface through the ShopManager singleton when interacted with.
    /// </summary>
    public override void Interact() {
        if (ShopManager.Instance != null) {
            GetComponent<NPCAudio>()?.PlayRandomShopOpenDialogue();
            ShopManager.Instance.OpenShop();
        }
    }

    #endregion
}
