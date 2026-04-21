using UnityEngine;

/// <summary>
/// Abstract class representing an exclusive upgrade for an item. This class can be inherited by specific item types to implement unique exclusive behaviors and effects.
/// </summary>
public abstract class ItemExclusive : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Exclusive Upgrade Settings")]
    [SerializeField] protected int unlockLevel = 1;
    [SerializeField] protected string exclusiveDescription = "No exclusive upgrade description available.";

    #endregion

    #region FIELDS

    protected bool isExclusiveUnlocked = false;
    protected bool hasExclusiveBeenPurchased = false;

    #endregion

    #region UNITY

    protected virtual void Awake() {
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Configures the exclusive settings by specifying the unlock level and a description.
    /// </summary>
    /// <param name="level">The unlock level to set. Must be a non-negative integer.</param>
    /// <param name="description">The description to associate with the exclusive setting. Cannot be null.</param>
    public void SetupExclusive(int level, string description) {
        SetUnlockLevel(level);
        SetExclusiveDescription(description);
    }

    /// <summary>
    /// Initializes the exclusive behavior. Called when the item is instantiated or enabled.
    /// </summary>
    public virtual void Initialize() {

    }

    /// <summary>
    /// Verifies if the exclusive upgrade should be unlocked based on the current level of the item.
    /// </summary>
    /// <param name="currentLevel">The current level of the item.</param>
    public void CheckForExclusiveUnlock(int currentLevel) {
        if (!isExclusiveUnlocked && currentLevel >= unlockLevel) {
            isExclusiveUnlocked = true;
            OnExclusiveUnlocked();
        }
    }

    /// <summary>
    /// When the exclusive upgrade is unlocked, this method is called.
    /// </summary>
    protected virtual void OnExclusiveUnlocked() {
        Debug.Log($"Exclusive upgrade unlocked for item at level {unlockLevel}!");
    }

    /// <summary>
    /// Activates the exclusive upgrade.
    /// </summary>
    public virtual void ActivateExclusive() {
        if (isExclusiveUnlocked && !hasExclusiveBeenPurchased) {
            hasExclusiveBeenPurchased = true;
            ApplyExclusiveEffects();
            Debug.Log("Exclusive upgrade activated!");
        }
        else if (hasExclusiveBeenPurchased) {
            Debug.Log("Exclusive upgrade already activated.");
        }
        else {
            Debug.LogWarning("Attempted to activate exclusive upgrade before it was unlocked or purchased.");
        }
    }

    /// <summary>
    /// Applies the effects of the exclusive upgrade.
    /// </summary>
    protected abstract void ApplyExclusiveEffects();

    /// <summary>
    /// Returns the upgrade's exlusive description.
    /// </summary>
    public string GetExclusiveDescription() {
        if (isExclusiveUnlocked || hasExclusiveBeenPurchased) {
            return exclusiveDescription;
        }
        return null;
    }

    /// <summary>
    /// Defines the exclusive upgrade as purchased/activated.
    /// </summary>
    /// <param name="level">The unlock level.</param>
    public void SetUnlockLevel(int level) {
        this.unlockLevel = level;
    }

    /// <summary>
    /// Defines the exclusive upgrade as purchased/activated.
    /// </summary>
    /// <param name="description">The description of the upgrade.</param>
    public void SetExclusiveDescription(string description) {
        this.exclusiveDescription = description;
    }

    /// <summary>
    /// Checks if the exclusive upgrade has been purchased/activated.
    /// </summary>
    public bool HasExclusiveBeenPurchased() {
        return hasExclusiveBeenPurchased;
    }

    /// <summary>
    /// Checks if the exclusive upgrade is unlocked and can be purchased/activated.
    /// </summary>
    public bool IsExclusiveUnlocked() {
        return isExclusiveUnlocked;
    }

    #endregion
}
