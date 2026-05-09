using UnityEngine;

/// <summary>
/// Represents the NPC that can be interacted with to open the shop interface.
/// Also handles proximity detection for dialogue triggers.
/// </summary>
public class NPC : Interactable {

    #region SERIALIZED FIELDS

    [SerializeField] private string npcName = "Merchant";

    [Header("Proximity Settings")]
    [Tooltip("Radius for detecting when player is close to NPC")]
    [SerializeField] private float proximityRadius = 5f;

    #endregion

    #region UNITY

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            GetComponent<NPCAudio>()?.OnPlayerEnteredRange();
        }
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
            GetComponent<NPCAudio>()?.PlayRandomShopOpenDialogue();
            ShopManager.Instance.OpenShop();
        }
    }

    #endregion
}