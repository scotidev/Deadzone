using System.Collections.Generic;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

// REFATORAÇÃO: playerporogress deve estar ciente e atualizar  aquantidade de munição, o SO dos itens tem um valor não utilizado, seria a weaponReserveAmmo e buildableQuantities possiveis de serem unificadas para todos os items possuírem somente uma quantidade? porque filosoficamente falando, items colocáveis também tem "munição" ou seja quantidade limitada para usar, assim como armas tem munição. Fora que items colocáveis também tem niveis de upgrade, e podem ser variaveis como as armas.
// REFATORAÇÃO: playerprogress deveria ser um service de ServiceLocator? Analise profunda necessaria.

// REFATORAÇÃO: Unificar UnlockWeapon e AddBuildable em um único método UnlockItem, que verifica o tipo do item e executa a lógica apropriada. Isso evitaria confusão sobre qual método chamar para cada tipo de item e garantiria que a lógica de desbloqueio/adicionamento esteja centralizada.

// REFATORAÇÃO: ou talvez não, armas não são posicionaveis, tudo bem ter um método unico para desbloquear items de maneira geral (independente do tipo) e talvez seja melhor ter métodos específicos para adicionar munição para cada um, pois quando um buildable ou um consumível como granada, medkit ou vest são consumidos, o item em si some do inventario, o que nao acontece para armas. A arma continua no inventário, o que acaba é a munição dela. Ainda sim, todos os itens PODEM ter UPGRADE, barricadas por exemplo podem aumentar de HP  e tamanho. Logo, o que poderia  ser feito é: 1. ter um método UnlockItem para desbloquear qualquer item, seja arma ou buildable, e esse método cuida de adicionar o item ao dicionário correto e inicializar seus valores. 2. ter métodos específicos para adicionar "munição" para cada tipo de item, como AddWeaponAmmo e AddBuildableQuantity, que lidam com a lógica específica de cada tipo (arma tem munição, buildable tem quantidade). 3. Método de upgrade unificado que faz o upgrade de acordo com o tipo do item, se é medkit, grenade, buildable, weapon, vest... é preciso analisar todo esse contexto e gerar um plano de implementação para garantir que o sistema seja flexível, escalável e fácil de manter, evitando confusão sobre qual método usar para cada tipo de item e centralizando a lógica de progressão de maneira clara.

/// <summary>
/// Tracks the player's progression through the game.
/// Stores weapon unlocks, upgrade levels, ammo reserves, and buildable quantities.
/// This is runtime-only data (not saved between sessions for web game).
/// </summary>
public class PlayerProgress : MonoBehaviour {

    #region STATIC

    /// <summary>Global access point to the single <see cref="PlayerProgress"/> instance.</summary>
    public static PlayerProgress Instance { get; private set; }

    #endregion

    #region FIELDS

    private Dictionary<string, bool> unlockedWeapons = new Dictionary<string, bool>();
    private Dictionary<string, bool> unlockedBuildables = new Dictionary<string, bool>();
    private Dictionary<string, bool> unlockedConsumables = new Dictionary<string, bool>();
    private Dictionary<string, int> weaponLevels = new Dictionary<string, int>();
    private Dictionary<string, int> itemLevels = new Dictionary<string, int>(); // Unified levels for all items (weapons, buildables, consumables)
    private Dictionary<string, int> weaponReserveAmmo = new Dictionary<string, int>();
    private HashSet<string> ammoInitialized = new HashSet<string>();
    private Dictionary<string, int> buildableQuantities = new Dictionary<string, int>();

    // REFATORAÇÃO: Adicionado suporte a consumíveis genéricos (medkit, grenade, etc)
    private Dictionary<string, int> consumableQuantities = new Dictionary<string, int>();

    // NOVO: Unified ammo/quantity system
    // itemCurrentAmmo: quantidade na mão (magazine para armas, 1 para consumíveis/buildables quando em uso)
    // itemTotalAmmo: quantidade no inventário (reserva)
    private Dictionary<string, int> itemCurrentAmmo = new Dictionary<string, int>();
    private Dictionary<string, int> itemTotalAmmo = new Dictionary<string, int>();

    public const int MAX_UPGRADE_LEVEL = 10;
    public const int MAX_BUILDABLE_QUANTITY = 5;
    public const int MAX_CONSUMABLE_QUANTITY = 10; // Default, pode ser overridden por item

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
            return;
        }

        InitializeDefaults();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Sets up default progression values.
    /// Pistol (weaponID "Pistol") is unlocked by default.
    /// </summary>
    private void InitializeDefaults() {
        UnlockWeaponInternal("1");
    }

    #region UNLOCKS

    /// <summary>
    /// Unlocks an item (weapon or buildable) based on its type.
    /// For weapons, it calls UnlockWeaponInternal. For buildables, it marks as unlocked and adds quantity.
    /// This method consolidates the logic for unlocking weapons and adding buildables.
    /// </summary>
    /// <param name="itemData">The ShopItemDataSO containing item details.</param>
    /// <param name="quantity">The quantity to add for buildable items (default is 1).</param>
    public void UnlockItem(ShopItemDataSO itemData, int quantity = 1) {
        if (itemData == null || itemData.ItemData == null) {
            Debug.LogWarning($"[PlayerProgress] UnlockItem: itemData or itemData.ItemData is NULL!");
            return;
        }

        Debug.Log($"[PlayerProgress] UnlockItem called for '{itemData.ItemID}' (Type: {itemData.ItemData.GetType().Name})");

        if (itemData.ItemData is WeaponDataSO) {
            UnlockWeaponInternal(itemData.ItemID);
            Debug.Log($"[PlayerProgress] Unlocked weapon: {itemData.ItemID}");
        }
        // AQUI É PRECISO REFATORAR: nao precisamos checar a quantidade de um buildable e sim fazer  o mesmo que a arma: DESBLOQUEAR e inicializar esse item no inventario com a quantidade de 1. O jogador pode comprar mais depois, mas o desbloqueio é o que importa aqui, o que libera o item para aparecer no inventário e ser comprado. A quantidade inicial de um item desbloqueado pode ser definida como 1, e o jogador pode comprar mais para aumentar essa quantidade, mas o desbloqueio em si é o que torna o item disponível para uso. Então não é aqui que adicionamos MAIS barricadas por exemplo, é quando DESBLOQUEAMOS ela, adicionar barricadas deve ser com o mesmo botão de comprar munição no UI. Agora, se vamos criar um método unificado para desbloquear items chamado UnlockItemInternal, ou mover toda a lógica dele aqui pra dentro, ou desbloquearemos o item de acordo com seu tipo (weapon, buildable, grenade, vest, medkit...) isso depende de análise
        else if (itemData.ItemData is BuildableDataSO) {
            UnlockBuildableInternal(itemData.ItemID, quantity);
            Debug.Log($"[PlayerProgress] Unlocked buildable: {itemData.ItemID}");
        } else if (itemData.ItemData is MedkitDataSO) {
            UnlockConsumableInternal(itemData.ItemID, quantity);
            Debug.Log($"[PlayerProgress] Unlocked medkit: {itemData.ItemID}");
        } else if (itemData.ItemData is GrenadeDataSO) {
            UnlockConsumableInternal(itemData.ItemID, quantity);
            Debug.Log($"[PlayerProgress] Unlocked grenade: {itemData.ItemID}");
        } else if (itemData.ItemData is VestDataSO) {
            UnlockConsumableInternal(itemData.ItemID, 1); // Vest is always quantity 1
            Debug.Log($"[PlayerProgress] Unlocked vest: {itemData.ItemID}");
        } else {
            Debug.LogWarning($"[PlayerProgress] Unsupported item type for unlocking: {itemData.ItemData.GetType().Name} (ID: {itemData.ItemID})");
        }
    }

    /// <summary>
    /// Unlocks a weapon, making it available for use.
    /// Also initializes its level to 1 and ammo to 0 (player must buy ammo).
    /// </summary>
    /// <param name="weaponID">The unique identifier of the weapon.</param>
    private void UnlockWeaponInternal(string weaponID) {
        if (!unlockedWeapons.ContainsKey(weaponID)) {
            unlockedWeapons[weaponID] = true;
        } else {
            unlockedWeapons[weaponID] = true;
        }

        if (!weaponLevels.ContainsKey(weaponID)) {
            weaponLevels[weaponID] = 1;
        }

        // CONCEITO: Initialize unified itemLevels dictionary alongside weaponLevels
        // This ensures GetItemLevel() can find the level for any item type
        if (!itemLevels.ContainsKey(weaponID)) {
            itemLevels[weaponID] = 1;
        }

        // Initialize reserve ammo to 0 (player must buy ammo)
        if (!weaponReserveAmmo.ContainsKey(weaponID)) {
            weaponReserveAmmo[weaponID] = 0;
        }

        // NEW: Initialize unified ammo system (current = 0, total = 0)
        InitializeItemAmmo(weaponID, 1);

        Debug.Log($"[PlayerProgress] Unlocked weapon (internal): {weaponID}");
    }

    /// <summary>
    /// Unlocks a buildable item (Barricade, ExplosiveBarrel, BearTrap).
    /// Marks it as unlocked and grants initialQuantity (default 1) in inventory.
    /// </summary>
    /// <param name="buildableID">The unique identifier of the buildable item.</param>
    /// <param name="initialQuantity">The quantity to grant on unlock (default is 1).</param>
    private void UnlockBuildableInternal(string buildableID, int initialQuantity = 1) {
        if (!unlockedBuildables.ContainsKey(buildableID)) {
            unlockedBuildables[buildableID] = true;
        } else {
            unlockedBuildables[buildableID] = true;
        }

        // CRÍTICO: Use consumableQuantities (unified storage) instead of buildableQuantities
        // GetBuildableQuantity() reads from consumableQuantities, so we must write there too
        if (!consumableQuantities.ContainsKey(buildableID)) {
            consumableQuantities[buildableID] = initialQuantity;
            Debug.Log($"[PlayerProgress] Unlocked buildable (internal): {buildableID} with quantity {initialQuantity}");
        } else {
            Debug.LogWarning($"[PlayerProgress] Buildable {buildableID} already unlocked with quantity {consumableQuantities[buildableID]}");
        }

        // CONCEITO: Initialize unified itemLevels dictionary for buildables
        // This ensures buildables can be upgraded just like weapons
        if (!itemLevels.ContainsKey(buildableID)) {
            itemLevels[buildableID] = 1;
        }

        // NEW: Initialize unified ammo system (current = 0, total = initialQuantity)
        // The player receives 1 buildable item in inventory on unlock
        InitializeItemAmmo(buildableID, 1);
        itemTotalAmmo[buildableID] = initialQuantity;
    }

    /// <summary>
    /// Unlocks a consumable item (Medkit, Grenade, Vest).
    /// Marks it as unlocked and initializes quantity to 0 (player must buy first).
    /// </summary>
    private void UnlockConsumableInternal(string consumableID, int initialQuantity = 1) {
        if (!unlockedConsumables.ContainsKey(consumableID)) {
            unlockedConsumables[consumableID] = true;
        } else {
            unlockedConsumables[consumableID] = true;
        }

        if (!consumableQuantities.ContainsKey(consumableID)) {
            consumableQuantities[consumableID] = initialQuantity;
        }

        // CONCEITO: Initialize unified itemLevels dictionary for consumables
        // This ensures consumables can be upgraded just like weapons
        if (!itemLevels.ContainsKey(consumableID)) {
            itemLevels[consumableID] = 1;
        }

        // NEW: Initialize unified ammo system (current = 0, total = 0)
        InitializeItemAmmo(consumableID, 1);

        Debug.Log($"[PlayerProgress] Unlocked consumable (internal): {consumableID} with quantity {initialQuantity}");
    }

    /// <summary>
    /// Checks if a weapon is unlocked.
    /// </summary>
    /// <param name="weaponID">The weapon to check.</param>
    /// <returns>True if the weapon is unlocked.</returns>
    public bool IsWeaponUnlocked(string weaponID) {
        bool isUnlocked = unlockedWeapons.TryGetValue(weaponID, out bool unlocked) && unlocked;
        Debug.Log($"[PlayerProgress] IsWeaponUnlocked({weaponID}): {isUnlocked} (exists in dict: {unlockedWeapons.ContainsKey(weaponID)})");
        return isUnlocked;
    }

    /// <summary>
    /// Generic method to check if any item (weapon, buildable, consumable) is unlocked.
    /// </summary>
    /// <param name="itemID">The item to check.</param>
    /// <returns>True if the item is unlocked.</returns>
    public bool IsItemUnlocked(string itemID) {
        // Check if it's a weapon
        if (unlockedWeapons.TryGetValue(itemID, out bool weaponUnlocked) && weaponUnlocked) {
            return true;
        }

        // Check if it's a buildable
        if (unlockedBuildables.TryGetValue(itemID, out bool buildableUnlocked) && buildableUnlocked) {
            return true;
        }

        // Check if it's a consumable
        if (unlockedConsumables.TryGetValue(itemID, out bool consumableUnlocked) && consumableUnlocked) {
            return true;
        }

        return false;
    }

    // REFATORAÇÃO: deviamos checar não só a quantidade de buildables, mas de granadas e medkits tbm.
    /// <summary>
    /// Gets the current quantity of a buildable item in inventory.
    /// Now uses the unified GetItemTotal() system.
    /// </summary>
    /// <param name="buildableID">The buildable type ID.</param>
    /// <returns>Current quantity.</returns>
    public int GetBuildableQuantity(string buildableID) {
        // NEW: Use unified GetItemTotal() instead of reading directly from consumableQuantities
        // This ensures sync with the new system
        return GetItemTotal(buildableID);
    }

    // REFATORAÇÃO: aqui a mesma coisa do método acima, não é so o buildable que é consumido ao usar, é o medkit e a granada. Se zerarmos nosso inventario (usarmos todos) ele deve ficar em 0, mas nao precisa ser desbloqueado novamente, apenas comprado munição, o que adiciona esse mesmo item ao inventario novamente.
    /// <summary>
    /// Consumes a buildable item (when placing it in the world).
    /// Now uses the unified UseItem() system instead of manipulating consumableQuantities directly.
    /// </summary>
    /// <param name="buildableID">The buildable type ID.</param>
    /// <returns>True if an item was available to consume.</returns>
    public bool ConsumeBuildable(string buildableID) {
        // NEW: Use unified UseItem() instead of manual manipulation
        // This ensures sync with the new itemTotalAmmo dictionary
        return UseItem(buildableID, 1);
    }

    #endregion

    #region UPGRADES

    //REFATORAÇÃO: aqui também, não é só a arma que tem upgrade, os buildables também podem ter upgrade, o medkit pode ter upgrade de cura, a barricada pode ter upgrade de HP e tamanho, a granada pode ter upgrade de dano e alcance... então o método de upgrade deve ser unificado para todos os tipos de itens, ou seja, UpgradeItem(string itemID) que verifica o tipo do item e faz o upgrade de acordo. O nível máximo de upgrade também pode ser definido no SO do item, ao invés de ser um valor fixo para todos os itens. Isso tornaria o sistema mais flexível e escalável para diferentes tipos de itens com diferentes requisitos de progressão.

    /// <summary>
    /// Upgrades a weapon to the next level (up to max level 10).
    /// Returns true if upgrade succeeded, false if already at max.
    /// </summary>
    /// <param name="weaponID">The weapon to upgrade.</param>
    /// <returns>True if upgraded successfully.</returns>
    public bool UpgradeWeapon(string weaponID) {
        int currentLevel = GetWeaponLevel(weaponID);

        if (currentLevel >= MAX_UPGRADE_LEVEL) {
            return false;
        }

        weaponLevels[weaponID] = currentLevel + 1;
        itemLevels[weaponID] = weaponLevels[weaponID]; // Sync with unified storage

        Debug.Log($"[PlayerProgress] {weaponID} upgraded to level {weaponLevels[weaponID]}");
        return true;
    }

    /// <summary>
    /// Generic method to upgrade any item (weapon, buildable, consumable).
    /// CONCEITO: Unified upgrade system that works for all 9 items.
    /// All items can be upgraded to a maximum level (currently 10 for all).
    /// </summary>
    /// <param name="itemID">The item to upgrade.</param>
    /// <returns>True if upgraded successfully.</returns>
    public bool UpgradeItem(string itemID) {
        int currentLevel = GetItemLevel(itemID);

        if (currentLevel >= MAX_UPGRADE_LEVEL) {
            return false;
        }

        itemLevels[itemID] = currentLevel + 1;

        // Also sync weaponLevels for backwards compatibility
        if (unlockedWeapons.ContainsKey(itemID)) {
            weaponLevels[itemID] = itemLevels[itemID];
        }

        // Grant 1 quantity in inventory for buildables on each upgrade
        if (unlockedBuildables.ContainsKey(itemID)) {
            AddItemAmmo(itemID, 1);
        }

        Debug.Log($"[PlayerProgress] {itemID} upgraded to level {itemLevels[itemID]}");
        return true;
    }

    //REFATORAÇÃO: GetItemLevel(string itemID) que retorna o nível de upgrade de um item, seja ele arma, buildable, medkit, granada... isso unificaria a lógica e tornaria o sistema mais flexível para diferentes tipos de itens. O nível de upgrade pode ser usado para determinar os benefícios do upgrade (dano aumentado para armas, cura aumentada para medkits, etc.) com base no tipo do item e seu nível de upgrade.
    /// <summary>
    /// Gets the current upgrade level of a weapon.
    /// </summary>
    /// <param name="weaponID">The weapon to check.</param>
    /// <returns>Current level (1-10), or 1 if not yet upgraded.</returns>
    public int GetWeaponLevel(string weaponID) {
        return weaponLevels.TryGetValue(weaponID, out int level) ? level : 1;
    }

    /// <summary>
    /// Generic method to get upgrade level of any item (weapon, buildable, consumable).
    /// CONCEITO: Unified level storage using itemLevels dictionary for all items.
    /// Returns 1 if item not yet unlocked (not found in itemLevels).
    /// </summary>
    /// <param name="itemID">The item to check.</param>
    /// <returns>Current level (1-10), or 1 if not yet upgraded.</returns>
    public int GetItemLevel(string itemID) {
        // Check unified itemLevels first
        if (itemLevels.TryGetValue(itemID, out int level)) {
            return level;
        }

        // Fallback to weaponLevels for backwards compatibility
        return GetWeaponLevel(itemID);
    }

    // REFATORAÇÃO: o check deve ser se a arma está no maximo nivel mas isso deve ser dinamico, barricadas por exemplo pode ir até o nivel 5, a pistola até o nivel 10. O nível máximo de upgrade para cada item pode ser definido no ScriptableObject do item, permitindo que diferentes tipos de itens tenham diferentes limites de upgrade. O método IsItemMaxLevel(string itemID) pode verificar o tipo do item e comparar o nível atual com o nível máximo definido no SO do item para determinar se o item está no nível máximo.
    /// <summary>
    /// Checks if a weapon is at maximum level.
    /// </summary>
    /// <param name="weaponID">The weapon to check.</param>
    /// <returns>True if at level 10.</returns>
    public bool IsWeaponMaxLevel(string weaponID) {
        return GetWeaponLevel(weaponID) >= MAX_UPGRADE_LEVEL;
    }

    /// <summary>
    /// Gets the maximum upgrade level for any item by reading from its ScriptableObject.
    /// This makes the max level configurable per item type.
    /// </summary>
    /// <param name="itemID">The item to check.</param>
    /// <returns>Maximum upgrade level (defaults to MAX_UPGRADE_LEVEL if not found).</returns>
    public int GetItemMaxLevel(string itemID) {
        // Try to find the ShopItemDataSO to get the max level from its ItemData
        var shopItemData = GetShopItemData(itemID);

        if (shopItemData != null && shopItemData.ItemData != null) {
            // Check different item types for their max level - use base class MaxUpgradeLevel from ItemDataSO
            // For VestDataSO, the MaxUpgradeLevel is set to 5 in the inspector (inherits from ItemDataSO)
            if (shopItemData.ItemData != null) {
                return shopItemData.ItemData.MaxUpgradeLevel;
            }
        }

        // Default fallback
        return MAX_UPGRADE_LEVEL;
    }

    /// <summary>
    /// Checks if an item is at its maximum level using dynamic max level.
    /// </summary>
    /// <param name="itemID">The item to check.</param>
    /// <returns>True if at maximum level for this specific item.</returns>
    public bool IsItemMaxLevel(string itemID) {
        return GetItemLevel(itemID) >= GetItemMaxLevel(itemID);
    }

    /// <summary>
    /// Gets the maximum ammo/quantity for an item at a specific level.
    /// This is the SINGLE SOURCE OF TRUTH for max ammo validation across the entire codebase.
    /// Calculates: min(baseAmmo * (1 + ammoScaling * (level - 1)), MaxAmmo)
    /// 
    /// This method centralizes ammo limit logic to eliminate redundancy in AmmoManager and ShopManager.
    /// Both should use this method instead of directly reading MaxAmmo.
    /// </summary>
    /// <param name="itemID">The item to get max ammo for.</param>
    /// <param name="level">The upgrade level to calculate ammo at.</param>
    /// <returns>Maximum ammo/quantity for the item at this level, or 10 (default) if item not found.</returns>
    public int GetMaxAmmoAtLevel(string itemID, int level) {
        var shopItemData = GetShopItemData(itemID);
        
        if (shopItemData?.ItemData == null) {
            Debug.LogWarning($"[PlayerProgress] GetMaxAmmoAtLevel: Could not find item data for {itemID}. Returning default 10.");
            return 10; // Default fallback
        }
        
        // Delegate to ItemDataSO's GetMaxAmmoAtLevel() which handles the scaling formula
        return shopItemData.ItemData.GetMaxAmmoAtLevel(level);
    }

    /// <summary>
    /// Gets the current ammo/quantity for ANY item type (weapons, buildables, consumables).
    /// This is a smart dispatcher that routes to the correct storage location based on item type.
    /// Returns 0 for Vest since it uses a special armor system (not quantity-based).
    /// SINGLE ENTRY POINT for UI queries about current item amounts.
    /// </summary>
    public int GetCurrentAmmoForItem(string itemID) {
        var shopItemData = GetShopItemData(itemID);
        if (shopItemData?.ItemData == null) {
            return 0;
        }
        
        // Weapons: stored in weaponReserveAmmo dictionary
        if (shopItemData.ItemData is WeaponDataSO) {
            return GetWeaponReserveAmmo(itemID);
        }
        
        // Buildables (BearTrap, Barricade, ExplosiveBarrel) and Consumables (Grenade, Medkit):
        // All stored in consumableQuantities dictionary
        if (shopItemData.ItemData is BuildableDataSO ||
            shopItemData.ItemData is MedkitDataSO ||
            shopItemData.ItemData is GrenadeDataSO) {
            return GetConsumableQuantity(itemID);
        }
        
        // Vest: uses special armor system (percentage-based, 0-100), not quantity-based
        // Handled separately in GetAmmoStatus() which accesses Vest component directly
        if (shopItemData.ItemData is VestDataSO) {
            return 0;
        }
        
        return 0;
    }

    /// <summary>
    /// Helper to find ShopItemDataSO by item ID.
    /// </summary>
    private ShopItemDataSO GetShopItemData(string itemID) {
        // This uses Resources.FindObjectsOfTypeAll which is expensive, but called infrequently
        // For better performance, consider caching this
        var allShopItems = UnityEngine.Resources.FindObjectsOfTypeAll<ShopItemDataSO>();
        foreach (var shopItem in allShopItems) {
            if (shopItem.ItemID == itemID) {
                return shopItem;
            }
        }
        return null;
    }

    #endregion

    #region UNIFIED AMMO/QUANTITY SYSTEM

    /// <summary>
    /// Initializes ammo/quantity for an item when it's unlocked.
    /// Sets current = 0 and total = 0 initially (player must buy ammo).
    /// For weapons: current = 0, total = 0 (player must buy ammo first).
    /// For consumables/buildables: current = 0, total = 0 (player must buy first).
    /// </summary>
    /// <param name="itemID">The item to initialize.</param>
    /// <param name="level">The upgrade level of the item.</param>
    public void InitializeItemAmmo(string itemID, int level = 1) {
        // LOG DIAGNÓSTICO: Verificar quando InitializeItemAmmo é chamado
        System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
        string callerInfo = "Unknown";
        if (stackTrace.FrameCount > 1) {
            var frame = stackTrace.GetFrame(1);
            callerInfo = $"{frame.GetMethod().DeclaringType?.Name}.{frame.GetMethod().Name}";
        }
        Debug.Log($"[PlayerProgress] InitializeItemAmmo CALLED by {callerInfo}: itemID={itemID}, level={level}");

        var shopItemData = GetShopItemData(itemID);
        if (shopItemData?.ItemData == null) {
            Debug.LogWarning($"[PlayerProgress] InitializeItemAmmo: Could not find item data for {itemID}");
            return;
        }

        int maxCurrent = shopItemData.ItemData.GetMaxCurrentCapacityAtLevel(level);
        
        int oldCurrent = GetItemCurrent(itemID);
        int oldTotal = GetItemTotal(itemID);
        
        // Initialize current ammo (start with empty magazine for weapons, 0 for consumables)
        itemCurrentAmmo[itemID] = 0;
        
        // Initialize total ammo (start with 0, player must buy ammo)
        itemTotalAmmo[itemID] = 0;

        Debug.Log($"[PlayerProgress] InitializeItemAmmo: {itemID} current {oldCurrent}->0, total {oldTotal}->0 (maxCurrent={maxCurrent})");
    }

    /// <summary>
    /// Gets the current quantity in hand (magazine for weapons, 1 for consumables when selected).
    /// CONCEITO: This is the amount currently being used (in the magazine or in hand).
    /// </summary>
    /// <param name="itemID">The item to check.</param>
    /// <returns>Current quantity in hand (0 if not found or item not unlocked).</returns>
    public int GetItemCurrent(string itemID) {
        return itemCurrentAmmo.TryGetValue(itemID, out int current) ? current : 0;
    }

    /// <summary>
    /// Gets the total quantity in inventory (reserve).
    /// CONCEITO: This is the amount stored in inventory, not currently in use.
    /// </summary>
    /// <param name="itemID">The item to check.</param>
    /// <returns>Total quantity in inventory (0 if not found or item not unlocked).</returns>
    public int GetItemTotal(string itemID) {
        return itemTotalAmmo.TryGetValue(itemID, out int total) ? total : 0;
    }

    /// <summary>
    /// Gets the maximum capacity for current (magazine/hand).
    /// For weapons: scales with magazine scaling. For consumables: always 1.
    /// </summary>
    /// <param name="itemID">The item to check.</param>
    /// <param name="level">The upgrade level (defaults to current level).</param>
    /// <returns>Maximum current capacity.</returns>
    public int GetItemMaxCurrent(string itemID, int level = -1) {
        if (level == -1) {
            level = GetItemLevel(itemID);
        }

        var shopItemData = GetShopItemData(itemID);
        if (shopItemData?.ItemData == null) {
            return 1; // Default fallback
        }

        return shopItemData.ItemData.GetMaxCurrentCapacityAtLevel(level);
    }

    /// <summary>
    /// Gets the maximum total capacity (total ammo/quantity allowed).
    /// This is determined by the MaxAmmo field in ItemDataSO and scales with upgrade level.
    /// </summary>
    /// <param name="itemID">The item to check.</param>
    /// <param name="level">The upgrade level (defaults to current level).</param>
    /// <returns>Maximum total capacity.</returns>
    public int GetItemMaxTotal(string itemID, int level = -1) {
        if (level == -1) {
            level = GetItemLevel(itemID);
        }

        return GetMaxAmmoAtLevel(itemID, level);
    }

    /// <summary>
    /// Adds ammo/quantity to the inventory (total).
    /// Respects the maximum limit for this item at its current level.
    /// </summary>
    /// <param name="itemID">The item to add ammo to.</param>
    /// <param name="amount">Amount to add.</param>
    /// <returns>True if ammo was added (not already at max).</returns>
    public bool AddItemAmmo(string itemID, int amount) {
        int level = GetItemLevel(itemID);
        int currentTotal = GetItemTotal(itemID);
        int maxTotal = GetItemMaxTotal(itemID, level);

        if (currentTotal >= maxTotal) {
            Debug.LogWarning($"[PlayerProgress] {itemID} inventory is already at max ({maxTotal}).");
            return false;
        }

        int newTotal = Mathf.Min(currentTotal + amount, maxTotal);
        itemTotalAmmo[itemID] = newTotal;

        Debug.Log($"[PlayerProgress] Added {amount} ammo to {itemID}. Total: {newTotal}/{maxTotal}");
        return true;
    }

    /// <summary>
    /// Uses an item (reduces total).
    /// For weapons: reduces total first (when firing empty magazine), then handles reload.
    /// For consumables/buildables: reduces total when used.
    /// </summary>
    /// <param name="itemID">The item being used.</param>
    /// <param name="amount">Amount to consume (default 1).</param>
    /// <returns>True if item was consumed successfully.</returns>
    public bool UseItem(string itemID, int amount = 1) {
        int currentTotal = GetItemTotal(itemID);

        if (currentTotal < amount) {
            Debug.LogWarning($"[PlayerProgress] Not enough {itemID} to use (have {currentTotal}, need {amount}).");
            return false;
        }

        itemTotalAmmo[itemID] = currentTotal - amount;
        Debug.Log($"[PlayerProgress] Used {amount} {itemID}. Remaining in inventory: {itemTotalAmmo[itemID]}");
        return true;
    }

    /// <summary>
    /// Sets the current ammo/quantity in hand.
    /// Used by weapons after reload or during initialization.
    /// </summary>
    /// <param name="itemID">The item to set.</param>
    /// <param name="amount">New amount in hand.</param>
    public void SetItemCurrent(string itemID, int amount) {
        int level = GetItemLevel(itemID);
        int maxCurrent = GetItemMaxCurrent(itemID, level);
        int oldValue = GetItemCurrent(itemID);
        
        // Clamp to valid range
        int clamped = Mathf.Clamp(amount, 0, maxCurrent);
        itemCurrentAmmo[itemID] = clamped;

        // LOG DIAGNÓSTICO: Só logar se realmente mudou ou for chamada externa
        if (oldValue != clamped) {
            Debug.Log($"[PlayerProgress] SetItemCurrent CHANGED: {itemID} {oldValue} -> {clamped}/{maxCurrent}");
        }
    }

    /// <summary>
    /// Checks if starting ammo has been granted to this weapon type.
    /// FIXED: Uses persistent HashSet in PlayerProgress (not per-instance field),
    /// so it works correctly even if weapons are cloned or destroyed.
    /// </summary>
    public bool IsAmmoInitialized(string itemID) {
        return ammoInitialized.Contains(itemID);
    }

    /// <summary>
    /// Marks a weapon type as having received its starting ammo.
    /// This ensures InitializeWeapon() only grants starting ammo once per item type,
    /// even if the weapon GameObject is cloned or re-initialized.
    /// </summary>
    public void MarkAmmoInitialized(string itemID) {
        if (!ammoInitialized.Contains(itemID)) {
            ammoInitialized.Add(itemID);
            Debug.Log($"[PlayerProgress] Marked {itemID} ammo as initialized");
        }
    }

    /// <summary>
    /// Sets the total ammo/quantity in inventory.
    /// Used by weapons during initialization to set starting reserve ammo.
    /// CONCEITO: This is the reserve/inventory amount, not the magazine/hand amount.
    /// </summary>
    /// <param name="itemID">The item to set.</param>
    /// <param name="amount">New total amount in inventory.</param>
    public void SetItemTotal(string itemID, int amount) {
        int maxTotal = GetItemMaxTotal(itemID);
        int oldValue = GetItemTotal(itemID);
        int clamped = Mathf.Max(0, amount);
        itemTotalAmmo[itemID] = clamped;
        // LOG DIAGNÓSTICO: Rastrear qualquer alteração no total
        if (oldValue != clamped) {
            Debug.Log($"[PlayerProgress] SetItemTotal CHANGED: {itemID} {oldValue} -> {clamped}/{maxTotal}");
        }
    }

    /// <summary>
    /// Transfers ammo from total (inventory) to current (magazine/hand).
    /// Used when reloading a weapon.
    /// </summary>
    /// <param name="itemID">The item to reload.</param>
    /// <returns>Amount of ammo transferred (0 if not possible).</returns>
    public int ReloadItem(string itemID) {
        int level = GetItemLevel(itemID);
        int maxCurrent = GetItemMaxCurrent(itemID, level);
        int currentCurrent = GetItemCurrent(itemID);
        int currentTotal = GetItemTotal(itemID);

        // Calculate how many bullets to transfer
        int ammoNeeded = maxCurrent - currentCurrent;
        int ammoAvailable = currentTotal;
        int ammoToTransfer = Mathf.Min(ammoNeeded, ammoAvailable);

        if (ammoToTransfer > 0) {
            // Transfer: reduce total, increase current
            itemTotalAmmo[itemID] = currentTotal - ammoToTransfer;
            itemCurrentAmmo[itemID] = currentCurrent + ammoToTransfer;

            Debug.Log($"[PlayerProgress] Reloaded {itemID}: transferred {ammoToTransfer} bullets. Current: {itemCurrentAmmo[itemID]}/{maxCurrent}, Total: {itemTotalAmmo[itemID]}");
        } else {
            Debug.LogWarning($"[PlayerProgress] Cannot reload {itemID} - no ammo available in total or magazine full.");
        }

        return ammoToTransfer;
    }

    #endregion

    #region AMMO AND QUANTITY
    // SHOPUI DEVE USAR ESSES MÉTODOS.é importante verificar se quando clicamos para comprar, o script do shop está chamando esse método para adicionar munição a arma, e nao usando um método dentro do proprio shop ou o método abaixo AddWeaponReserveAmmo(). É preciso analisar se esse método abaixo é realmente necessario e qual a melhor abordagem para de fato implementar a compra de munições.

    /// COMO DITO NO inicio, tudo bem ter um método só para adicionar munição para armas, e outro método para adicionar quantidade para buildables, pois a lógica de cada um é diferente. Para as armas, o jogador compra munição que é adicionada à reserva, e quando recarrega, essa munição é consumida mas a arma permanece no inventário. Para os buildables, o jogador compra uma quantidade que é adicionada ao inventário, e quando usa um buildable (coloca no mundo), essa quantidade é consumida e o item pode desaparecer do inventário se a quantidade chegar a zero. Então faz sentido ter métodos separados para lidar com a lógica específica de cada tipo de item.
    /// <summary>
    /// Adds reserve ammo for a weapon, respecting the maximum limit.
    /// NEW: Uses unified AddItemAmmo() system while maintaining backward compatibility with weaponReserveAmmo.
    /// </summary>
    /// <param name="weaponID">The weapon to add ammo for.</param>
    /// <param name="amount">Amount of ammo to add.</param>
    /// <param name="maxReserve">Maximum reserve ammo allowed.</param>
    /// <returns>True if ammo was added (not already at max).</returns>
    public bool AddReserveAmmo(string weaponID, int amount, int maxReserve) {
        // NEW: Use unified AddItemAmmo() instead of manipulating weaponReserveAmmo directly
        // This ensures sync with the new itemTotalAmmo dictionary
        bool added = AddItemAmmo(weaponID, amount);
        
        // LEGACY: Also update weaponReserveAmmo for backward compatibility
        if (added) {
            int currentAmmo = GetReserveAmmo(weaponID);
            weaponReserveAmmo[weaponID] = GetItemTotal(weaponID);
        }
        
        return added;
    }

    /// <summary>
    /// Spends reserve ammo (when reloading).
    /// NEW: Uses unified UseItem() system.
    /// </summary>
    /// <param name="weaponID">The weapon to consume ammo from.</param>
    /// <param name="amount">Amount of ammo to consume.</param>
    /// <returns>True if enough ammo was available.</returns>
    public bool SpendReserveAmmo(string weaponID, int amount) {
        // NEW: Use unified UseItem() instead of manipulating weaponReserveAmmo directly
        // This ensures sync with the new itemTotalAmmo dictionary
        return UseItem(weaponID, amount);
    }

    /// <summary>
    /// Gets the current reserve ammo for a weapon.
    /// NEW: Uses unified GetItemTotal() system.
    /// </summary>
    /// <param name="weaponID">The weapon to check.</param>
    /// <returns>Current reserve ammo count.</returns>
    public int GetReserveAmmo(string weaponID) {
        // NEW: Use unified GetItemTotal() instead of reading directly from weaponReserveAmmo
        return GetItemTotal(weaponID);
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
    /// Adds reserve ammo for a weapon (used by Shop UI).
    /// FIXED: Now uses unified AddItemAmmo() instead of writing to legacy weaponReserveAmmo.
    /// </summary>
    /// <param name="weaponID">The weapon to add ammo for.</param>
    /// <param name="amount">Amount of ammo to add.</param>
    public void AddWeaponReserveAmmo(string weaponID, int amount) {
        // FIXED: Use unified AddItemAmmo() which updates itemTotalAmmo (the single source of truth)
        // The legacy weaponReserveAmmo dict is no longer used for writes
        AddItemAmmo(weaponID, amount);
        Debug.Log($"[PlayerProgress] Added {amount} reserve ammo to {weaponID} via unified system.");
    }

    #endregion

    #region CONSUMABLES

    // REFATORAÇÃO RESOLVIDA: Consumíveis (medkit, grenade) agora têm suporte completo
    // com métodos específicos para gerenciar quantidade. Buildables também usam esses
    // métodos pois têm a mesma semântica: quantidade limitada que é consumida ao usar.

    /// <summary>
    /// Gets the current quantity of a consumable or buildable item.
    /// Now uses the unified GetItemTotal() system.
    /// </summary>
    /// <param name="itemID">The consumable/buildable item ID.</param>
    /// <returns>Current quantity.</returns>
    public int GetConsumableQuantity(string itemID) {
        // NEW: Use unified GetItemTotal() instead of reading directly from consumableQuantities
        // This ensures sync with the new system
        return GetItemTotal(itemID);
    }

    /// <summary>
    /// Adds quantity to a consumable or buildable item.
    /// Respects the maximum limit using the unified system.
    /// </summary>
    /// <param name="itemID">The consumable/buildable item ID.</param>
    /// <param name="amount">Amount to add.</param>
    /// <param name="maxAmount">Maximum amount allowed (default 10, overridden by ItemDataSO).</param>
    /// <returns>True if quantity was added (not already at max).</returns>
    public bool AddConsumable(string itemID, int amount, int maxAmount = MAX_CONSUMABLE_QUANTITY) {
        // NEW: Use unified AddItemAmmo() instead of manual manipulation
        // This ensures sync with the new system and respects ItemDataSO max values
        return AddItemAmmo(itemID, amount);
    }

    /// <summary>
    /// Consumes (decrements) a consumable or buildable item.
    /// Now uses the unified UseItem() system instead of manipulating consumableQuantities directly.
    /// </summary>
    /// <param name="itemID">The consumable/buildable item ID.</param>
    /// <param name="amount">Amount to consume.</param>
    /// <returns>True if enough quantity was available to consume.</returns>
    public bool ConsumeItem(string itemID, int amount) {
        // NEW: Use unified UseItem() instead of manual manipulation
        // This ensures sync with the new itemTotalAmmo dictionary
        return UseItem(itemID, amount);
    }

    #endregion

    #endregion
}

