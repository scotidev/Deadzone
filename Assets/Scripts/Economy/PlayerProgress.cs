using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks the player's progression through the game.
/// Stores weapon unlocks, upgrade levels, ammo reserves, and buildable quantities.
/// This is runtime-only data (not saved between sessions for web game).
/// </summary>
public class PlayerProgress : MonoBehaviour {

    /// <summary>Global access point to the single PlayerProgress instance.</summary>
    public static PlayerProgress Instance { get; private set; }

    /// <summary>
    /// Dictionary tracking which weapons are unlocked.
    /// Key = weaponID (e.g., "Pistol", "SMG"), Value = true if unlocked
    /// </summary>
    /// CONCEITO: Dictionary é uma estrutura de dados chave-valor que permite busca rápida O(1)
    /// É como um dicionário onde você procura uma palavra (chave) e encontra sua definição (valor)
    /// Exemplo: unlockedWeapons["Pistol"] = true (a Pistola está desbloqueada)
    private Dictionary<string, bool> unlockedWeapons = new Dictionary<string, bool>();

    /// <summary>
    /// Dictionary tracking upgrade level for each weapon.
    /// Key = weaponID, Value = current level (1-10)
    /// </summary>
    /// CONCEITO: Usamos Dictionary em vez de arrays porque não sabemos quantas armas teremos
    /// e porque a busca por ID é muito mais rápida que percorrer uma lista inteira
    private Dictionary<string, int> weaponLevels = new Dictionary<string, int>();

    /// <summary>
    /// Dictionary tracking reserve ammo for each weapon.
    /// Key = weaponID, Value = current reserve ammo count
    /// </summary>
    /// CONCEITO: Cada arma tem sua própria munição reserva independente
    /// O Dictionary permite gerenciar isso sem criar uma variável para cada arma
    private Dictionary<string, int> weaponReserveAmmo = new Dictionary<string, int>();

    /// <summary>
    /// Dictionary tracking quantity owned for buildable items.
    /// Key = buildableID (e.g., "Barricade", "ExplosiveBarrel"), Value = count (max 5)
    /// </summary>
    /// CONCEITO: Inventário de itens construíveis (barricadas, barris, armadilhas)
    /// Limitado a 5 de cada tipo no inventário (mas ilimitados posicionados no mapa)
    private Dictionary<string, int> buildableQuantities = new Dictionary<string, int>();

    /// <summary>
    /// Maximum upgrade level for any weapon.
    /// Level 10 unlocks exclusive power.
    /// </summary>
    /// CONCEITO: const significa que este valor nunca muda durante o jogo
    /// É uma constante definida em tempo de compilação (não pode ser modificada nem no Inspector)
    /// Usamos const para valores fixos de design que nunca devem mudar
    public const int MAX_UPGRADE_LEVEL = 10;

    /// <summary>
    /// Maximum buildable items of each type in inventory.
    /// Player can place unlimited in world, but can only carry 5 at a time.
    /// </summary>
    /// CONCEITO: Limite de inventário para balancear o gameplay
    /// O jogador pode ter muitas barricadas no mapa, mas só pode carregar 5 de cada vez
    public const int MAX_BUILDABLE_QUANTITY = 5;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Sets up the singleton pattern.
    /// </summary>
    private void Awake() {
        // Singleton pattern - ensure only one instance exists
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
            return;
        }

        // Initialize default progression state
        InitializeDefaults();
    }

    /// <summary>
    /// Sets up default progression values.
    /// Pistol (weaponID "Pistol") is unlocked by default.
    /// </summary>
    private void InitializeDefaults() {
        // Unlock the Pistol by default (starting weapon)
        UnlockWeapon("Pistol");
        
        Debug.Log("[PlayerProgress] Initialized. Pistol unlocked by default.");
    }

    #region Weapon Unlocks

    /// <summary>
    /// Unlocks a weapon, making it available for use.
    /// Also initializes its level to 1 if not already set.
    /// </summary>
    /// <param name="weaponID">The unique identifier of the weapon.</param>
    public void UnlockWeapon(string weaponID) {
        // If not already in dictionary, add it
        if (!unlockedWeapons.ContainsKey(weaponID)) {
            unlockedWeapons[weaponID] = true;
        }
        else {
            // Already exists, just set to true
            unlockedWeapons[weaponID] = true;
        }

        // Initialize level to 1 if not already tracked
        if (!weaponLevels.ContainsKey(weaponID)) {
            weaponLevels[weaponID] = 1;
        }

        // Initialize reserve ammo to 0 (player must buy ammo)
        if (!weaponReserveAmmo.ContainsKey(weaponID)) {
            weaponReserveAmmo[weaponID] = 0;
        }

        Debug.Log($"[PlayerProgress] Unlocked weapon: {weaponID}");
    }

    /// <summary>
    /// Checks if a weapon is unlocked.
    /// </summary>
    /// <param name="weaponID">The weapon to check.</param>
    /// <returns>True if the weapon is unlocked.</returns>
    public bool IsWeaponUnlocked(string weaponID) {
        // If key exists in dictionary and value is true, weapon is unlocked
        // The TryGetValue pattern safely checks if key exists
        return unlockedWeapons.TryGetValue(weaponID, out bool unlocked) && unlocked;
    }

    #endregion

    #region Weapon Upgrades

    /// <summary>
    /// Upgrades a weapon to the next level (up to max level 10).
    /// Returns true if upgrade succeeded, false if already at max.
    /// </summary>
    /// <param name="weaponID">The weapon to upgrade.</param>
    /// <returns>True if upgraded successfully.</returns>
    public bool UpgradeWeapon(string weaponID) {
        // Get current level (default to 1 if not tracked)
        int currentLevel = GetWeaponLevel(weaponID);

        // Check if already at max level
        if (currentLevel >= MAX_UPGRADE_LEVEL) {
            Debug.LogWarning($"[PlayerProgress] {weaponID} is already at max level {MAX_UPGRADE_LEVEL}.");
            return false;
        }

        // Increment level
        weaponLevels[weaponID] = currentLevel + 1;

        Debug.Log($"[PlayerProgress] {weaponID} upgraded to level {weaponLevels[weaponID]}");
        return true;
    }

    /// <summary>
    /// Gets the current upgrade level of a weapon.
    /// </summary>
    /// <param name="weaponID">The weapon to check.</param>
    /// <returns>Current level (1-10), or 1 if not yet upgraded.</returns>
    public int GetWeaponLevel(string weaponID) {
        // If weapon level is tracked, return it; otherwise default to 1
        return weaponLevels.TryGetValue(weaponID, out int level) ? level : 1;
    }

    /// <summary>
    /// Checks if a weapon is at maximum level.
    /// </summary>
    /// <param name="weaponID">The weapon to check.</param>
    /// <returns>True if at level 10.</returns>
    public bool IsWeaponMaxLevel(string weaponID) {
        return GetWeaponLevel(weaponID) >= MAX_UPGRADE_LEVEL;
    }

    #endregion

    #region Reserve Ammo

    /// <summary>
    /// Adds reserve ammo for a weapon, respecting the maximum limit.
    /// </summary>
    /// <param name="weaponID">The weapon to add ammo for.</param>
    /// <param name="amount">Amount of ammo to add.</param>
    /// <param name="maxReserve">Maximum reserve ammo allowed.</param>
    /// <returns>True if ammo was added (not already at max).</returns>
    public bool AddReserveAmmo(string weaponID, int amount, int maxReserve) {
        // Get current reserve ammo (default to 0)
        int currentAmmo = GetReserveAmmo(weaponID);

        // Check if already at max
        if (currentAmmo >= maxReserve) {
            Debug.LogWarning($"[PlayerProgress] {weaponID} reserve ammo is already at max ({maxReserve}).");
            return false;
        }

        // Add ammo, but clamp to max
        // Mathf.Min returns the smaller value, ensuring we don't exceed max
        int newAmmo = Mathf.Min(currentAmmo + amount, maxReserve);
        weaponReserveAmmo[weaponID] = newAmmo;

        Debug.Log($"[PlayerProgress] Added {amount} reserve ammo to {weaponID}. New total: {newAmmo}/{maxReserve}");
        return true;
    }

    /// <summary>
    /// Spends reserve ammo (when reloading).
    /// </summary>
    /// <param name="weaponID">The weapon to consume ammo from.</param>
    /// <param name="amount">Amount of ammo to consume.</param>
    /// <returns>True if enough ammo was available.</returns>
    public bool SpendReserveAmmo(string weaponID, int amount) {
        int currentAmmo = GetReserveAmmo(weaponID);

        // Check if enough ammo
        if (currentAmmo < amount) {
            return false;
        }

        // Deduct ammo
        weaponReserveAmmo[weaponID] = currentAmmo - amount;
        return true;
    }

    /// <summary>
    /// Gets the current reserve ammo for a weapon.
    /// </summary>
    /// <param name="weaponID">The weapon to check.</param>
    /// <returns>Current reserve ammo count.</returns>
    public int GetReserveAmmo(string weaponID) {
        return weaponReserveAmmo.TryGetValue(weaponID, out int ammo) ? ammo : 0;
    }

    /// <summary>
    /// Public wrapper for GetReserveAmmo to maintain consistent naming convention.
    /// Gets the current reserve ammo for a weapon (used by Shop UI).
    /// </summary>
    /// <param name="weaponID">The weapon to check.</param>
    /// <returns>Current reserve ammo count.</returns>
    public int GetWeaponReserveAmmo(string weaponID) {
        return GetReserveAmmo(weaponID);
    }

    /// <summary>
    /// Public wrapper for AddReserveAmmo to maintain consistent naming convention.
    /// Adds reserve ammo for a weapon without specifying max (used by Shop UI).
    /// </summary>
    /// <param name="weaponID">The weapon to add ammo for.</param>
    /// <param name="amount">Amount of ammo to add.</param>
    public void AddWeaponReserveAmmo(string weaponID, int amount) {
        // This is a simplified wrapper that doesn't enforce max limit
        // The caller (ShopUI) handles max limit enforcement
        int currentAmmo = GetReserveAmmo(weaponID);
        weaponReserveAmmo[weaponID] = currentAmmo + amount;
        Debug.Log($"[PlayerProgress] Added {amount} reserve ammo to {weaponID}. New total: {weaponReserveAmmo[weaponID]}");
    }

    #endregion

    #region Buildables

    /// <summary>
    /// Adds buildable items to inventory, respecting the max limit of 5.
    /// </summary>
    /// <param name="buildableID">The buildable type ID.</param>
    /// <param name="amount">Amount to add (typically 1 per purchase).</param>
    /// <returns>True if added successfully.</returns>
    public bool AddBuildable(string buildableID, int amount) {
        int currentQty = GetBuildableQuantity(buildableID);

        // Check if already at max
        if (currentQty >= MAX_BUILDABLE_QUANTITY) {
            Debug.LogWarning($"[PlayerProgress] {buildableID} is already at max quantity ({MAX_BUILDABLE_QUANTITY}).");
            return false;
        }

        // Add quantity, clamped to max
        int newQty = Mathf.Min(currentQty + amount, MAX_BUILDABLE_QUANTITY);
        buildableQuantities[buildableID] = newQty;

        Debug.Log($"[PlayerProgress] Added {amount} {buildableID}. New total: {newQty}/{MAX_BUILDABLE_QUANTITY}");
        return true;
    }

    /// <summary>
    /// Consumes a buildable item (when placing it in the world).
    /// </summary>
    /// <param name="buildableID">The buildable type ID.</param>
    /// <returns>True if an item was available to consume.</returns>
    public bool ConsumeBuildable(string buildableID) {
        int currentQty = GetBuildableQuantity(buildableID);

        // Check if any available
        if (currentQty <= 0) {
            return false;
        }

        // Deduct one
        buildableQuantities[buildableID] = currentQty - 1;
        Debug.Log($"[PlayerProgress] Consumed 1 {buildableID}. Remaining: {buildableQuantities[buildableID]}");
        return true;
    }

    /// <summary>
    /// Gets the current quantity of a buildable item in inventory.
    /// </summary>
    /// <param name="buildableID">The buildable type ID.</param>
    /// <returns>Current quantity (0-5).</returns>
    public int GetBuildableQuantity(string buildableID) {
        return buildableQuantities.TryGetValue(buildableID, out int qty) ? qty : 0;
    }

    #endregion

    #region Debug & Testing

    /// <summary>
    /// Resets all progression data.
    /// Useful for testing or restarting the game.
    /// </summary>
    public void ResetProgress() {
        unlockedWeapons.Clear();
        weaponLevels.Clear();
        weaponReserveAmmo.Clear();
        buildableQuantities.Clear();

        // Re-initialize defaults (unlock Pistol)
        InitializeDefaults();

        Debug.Log("[PlayerProgress] Progress reset.");
    }

    #endregion
}
