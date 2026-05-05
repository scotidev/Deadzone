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
    private Dictionary<string, int> buildableQuantities = new Dictionary<string, int>();
    
    // REFATORAÇÃO: Adicionado suporte a consumíveis genéricos (medkit, grenade, etc)
    private Dictionary<string, int> consumableQuantities = new Dictionary<string, int>();
    
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
        UnlockWeaponInternal("Pistol");
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
        }
        else if (itemData.ItemData is MedkitDataSO) {
            UnlockConsumableInternal(itemData.ItemID, quantity);
            Debug.Log($"[PlayerProgress] Unlocked medkit: {itemData.ItemID}");
        }
        else if (itemData.ItemData is GrenadeDataSO) {
            UnlockConsumableInternal(itemData.ItemID, quantity);
            Debug.Log($"[PlayerProgress] Unlocked grenade: {itemData.ItemID}");
        }
        else if (itemData.ItemData is VestDataSO) {
            UnlockConsumableInternal(itemData.ItemID, 1); // Vest is always quantity 1
            Debug.Log($"[PlayerProgress] Unlocked vest: {itemData.ItemID}");
        }
        else {
            Debug.LogWarning($"[PlayerProgress] Unsupported item type for unlocking: {itemData.ItemData.GetType().Name} (ID: {itemData.ItemID})");
        }
    }

    /// <summary>
    /// Unlocks a weapon, making it available for use.
    /// Also initializes its level to 1 if not already set.
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

        Debug.Log($"[PlayerProgress] Unlocked weapon (internal): {weaponID}");
    }

    /// <summary>
    /// Unlocks a buildable item (Barricade, ExplosiveBarrel, BearTrap).
    /// Marks it as unlocked and initializes quantity to specified amount.
    /// </summary>
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
    }

    /// <summary>
    /// Unlocks a consumable item (Medkit, Grenade, Vest).
    /// Marks it as unlocked and initializes quantity to specified amount.
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
    /// </summary>
    /// <param name="buildableID">The buildable type ID.</param>
    /// <returns>Current quantity (0-5).</returns>
    public int GetBuildableQuantity(string buildableID) {
        // Use consumableQuantities (unified storage for buildables, medkits, grenades)
        // This matches where UnlockBuildableInternal() writes the quantity
        return consumableQuantities.TryGetValue(buildableID, out int qty) ? qty : 0;
    }

    // REFATORAÇÃO: aqui a mesma coisa do método acima, não é so o buildable que é consumido ao usar, é o medkit e a granada. Se zerarmos nosso inventario (usarmos todos) ele deve ficar em 0, mas nao precisa ser desbloqueado novamente, apenas comprado munição, o que adiciona esse mesmo item ao inventario novamente.
    /// <summary>
    /// Consumes a buildable item (when placing it in the world).
    /// </summary>
    /// <param name="buildableID">The buildable type ID.</param>
    /// <returns>True if an item was available to consume.</returns>
    public bool ConsumeBuildable(string buildableID) {
        int currentQty = GetBuildableQuantity(buildableID);

        if (currentQty <= 0) {
            return false;
        }

        buildableQuantities[buildableID] = currentQty - 1;
        return true;
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

    #region AMMO AND QUANTITY
    // SHOPUI DEVE USAR ESSES MÉTODOS.é importante verificar se quando clicamos para comprar, o script do shop está chamando esse método para adicionar munição a arma, e nao usando um método dentro do proprio shop ou o método abaixo AddWeaponReserveAmmo(). É preciso analisar se esse método abaixo é realmente necessario e qual a melhor abordagem para de fato implementar a compra de munições.

    /// COMO DITO NO inicio, tudo bem ter um método só para adicionar munição para armas, e outro método para adicionar quantidade para buildables, pois a lógica de cada um é diferente. Para as armas, o jogador compra munição que é adicionada à reserva, e quando recarrega, essa munição é consumida mas a arma permanece no inventário. Para os buildables, o jogador compra uma quantidade que é adicionada ao inventário, e quando usa um buildable (coloca no mundo), essa quantidade é consumida e o item pode desaparecer do inventário se a quantidade chegar a zero. Então faz sentido ter métodos separados para lidar com a lógica específica de cada tipo de item.
    /// <summary>
    /// Adds reserve ammo for a weapon, respecting the maximum limit.
    /// </summary>
    /// <param name="weaponID">The weapon to add ammo for.</param>
    /// <param name="amount">Amount of ammo to add.</param>
    /// <param name="maxReserve">Maximum reserve ammo allowed.</param>
    /// <returns>True if ammo was added (not already at max).</returns>
    public bool AddReserveAmmo(string weaponID, int amount, int maxReserve) {
        int currentAmmo = GetReserveAmmo(weaponID);

        if (currentAmmo >= maxReserve) {
            Debug.LogWarning($"[PlayerProgress] {weaponID} reserve ammo is already at max ({maxReserve}).");
            return false;
        }

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

    #region CONSUMABLES
    
    // REFATORAÇÃO RESOLVIDA: Consumíveis (medkit, grenade) agora têm suporte completo
    // com métodos específicos para gerenciar quantidade. Buildables também usam esses
    // métodos pois têm a mesma semântica: quantidade limitada que é consumida ao usar.
    
    /// <summary>
    /// Gets the current quantity of a consumable or buildable item.
    /// </summary>
    /// <param name="itemID">The consumable/buildable item ID.</param>
    /// <returns>Current quantity (0 if not found).</returns>
    public int GetConsumableQuantity(string itemID) {
        return consumableQuantities.TryGetValue(itemID, out int qty) ? qty : 0;
    }
    
    /// <summary>
    /// Adds quantity to a consumable or buildable item.
    /// Respects the maximum limit.
    /// </summary>
    /// <param name="itemID">The consumable/buildable item ID.</param>
    /// <param name="amount">Amount to add.</param>
    /// <param name="maxAmount">Maximum amount allowed (default 10).</param>
    /// <returns>True if quantity was added (not already at max).</returns>
    public bool AddConsumable(string itemID, int amount, int maxAmount = MAX_CONSUMABLE_QUANTITY) {
        int currentQty = GetConsumableQuantity(itemID);
        
        if (currentQty >= maxAmount) {
            Debug.LogWarning($"[PlayerProgress] {itemID} quantity is already at max ({maxAmount}).");
            return false;
        }
        
        int newQty = Mathf.Min(currentQty + amount, maxAmount);
        consumableQuantities[itemID] = newQty;
        
        Debug.Log($"[PlayerProgress] Added {amount} {itemID}. New total: {newQty}/{maxAmount}");
        return true;
    }
    
    /// <summary>
    /// Consumes (decrements) a consumable or buildable item.
    /// </summary>
    /// <param name="itemID">The consumable/buildable item ID.</param>
    /// <param name="amount">Amount to consume.</param>
    /// <returns>True if enough quantity was available to consume.</returns>
    public bool ConsumeItem(string itemID, int amount) {
        int currentQty = GetConsumableQuantity(itemID);
        
        if (currentQty < amount) {
            Debug.LogWarning($"[PlayerProgress] Not enough {itemID} to consume (have {currentQty}, need {amount}).");
            return false;
        }
        
        consumableQuantities[itemID] = currentQty - amount;
        Debug.Log($"[PlayerProgress] Consumed {amount} {itemID}. Remaining: {consumableQuantities[itemID]}");
        return true;
    }

    #endregion

    #endregion
}

