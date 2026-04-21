using System.Collections.Generic;
using UnityEngine;

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
    private Dictionary<string, int> weaponLevels = new Dictionary<string, int>();
    private Dictionary<string, int> weaponReserveAmmo = new Dictionary<string, int>();
    private Dictionary<string, int> buildableQuantities = new Dictionary<string, int>();
    // UNIFICAR weaponReserveAmmo e buildableQuantities? ambos são quantidades limitadas, ambos podem ser consumidos, ambos podem ser adquiridos. Manter o unlockedWeapons e weaponLevels para todos os itens também, pois todos tem niveis e todos podem ser desbloqueados.
    public const int MAX_UPGRADE_LEVEL = 10;
    public const int MAX_BUILDABLE_QUANTITY = 5;
    // AQUI TAMBÉM PODERIA SER UNIFICADO, um limite dinâmico de quantidade para todos os itens, sejam armas ou buildables. O conceito de "munição" pode ser aplicado a ambos. AK47 poderia ter uma quantidade máxima de "munição" (itens disponíveis para uso), e barricada também poderia ter uma quantidade máxima de "munição" (barricadas disponíveis para colocar). Isso simplificaria a lógica e tornaria o sistema mais flexível para futuros itens. 
    //Outra consideração: nem toda arma tem seu nivel maximo sendo 10, isso deve ser dinâmico para cada item, talvez definido no SO do item. O mesmo vale para a quantidade máxima de buildables, poderia ser definida no SO do item também. Isso tornaria o sistema mais flexível e escalável para diferentes tipos de itens com diferentes requisitos de progressão.

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
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
    /// For weapons, it calls UnlockWeaponInternal. For buildables, it adds to inventory.
    /// This method consolidates the logic for unlocking weapons and adding buildables.
    /// </summary>
    /// <param name="itemData">The ShopItemDataSO containing item details.</param>
    /// <param name="quantity">The quantity to add for buildable items (default is 1).</param>
    public void UnlockItem(ShopItemDataSO itemData, int quantity = 1) {
        if (itemData == null || itemData.ItemData == null) {
            return;
        }

        if (itemData.ItemData is WeaponDataSO) {
            UnlockWeaponInternal(itemData.ItemID);
            Debug.Log($"[PlayerProgress] Unlocked weapon: {itemData.ItemID}");
        }
        // AQUI É PRECISO REFATORAR: nao precisamos checar a quantidade de um buildable e sim fazer  o mesmo que a arma: DESBLOQUEAR e inicializar esse item no inventario com a quantidade de 1. O jogador pode comprar mais depois, mas o desbloqueio é o que importa aqui, o que libera o item para aparecer no inventário e ser comprado. A quantidade inicial de um item desbloqueado pode ser definida como 1, e o jogador pode comprar mais para aumentar essa quantidade, mas o desbloqueio em si é o que torna o item disponível para uso. Então não é aqui que adicionamos MAIS barricadas por exemplo, é quando DESBLOQUEAMOS ela, adicionar barricadas deve ser com o mesmo botão de comprar munição no UI. Agora, se vamos criar um método unificado para desbloquear items chamado UnlockItemInternal, ou mover toda a lógica dele aqui pra dentro, ou desbloquearemos o item de acordo com seu tipo (weapon, buildable, grenade, vest, medkit...) isso depende de análise
        else if (itemData.ItemData is BuildableDataSO) {
            string buildableID = itemData.ItemID;
            int currentQty = GetBuildableQuantity(buildableID);

            if (currentQty >= MAX_BUILDABLE_QUANTITY) {
                return;
            }

            int newQty = Mathf.Min(currentQty + quantity, MAX_BUILDABLE_QUANTITY);
            buildableQuantities[buildableID] = newQty;

            Debug.Log($"[PlayerProgress] Added {quantity} {buildableID}. New total: {newQty}/{MAX_BUILDABLE_QUANTITY}");
        }
        else {
            Debug.LogWarning($"[PlayerProgress] Unsupported item type for unlocking/adding: {itemData.ItemData.GetType().Name} (ID: {itemData.ItemID})");
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
        }
        else {
            unlockedWeapons[weaponID] = true;
        }

        if (!weaponLevels.ContainsKey(weaponID)) {
            weaponLevels[weaponID] = 1;
        }

        // Initialize reserve ammo to 0 (player must buy ammo)
        if (!weaponReserveAmmo.ContainsKey(weaponID)) {
            weaponReserveAmmo[weaponID] = 0;
        }

        Debug.Log($"[PlayerProgress] Unlocked weapon (internal): {weaponID}");
    }

    /// <summary>
    /// Checks if a weapon is unlocked.
    /// </summary>
    /// <param name="weaponID">The weapon to check.</param>
    /// <returns>True if the weapon is unlocked.</returns>
    public bool IsWeaponUnlocked(string weaponID) {
        return unlockedWeapons.TryGetValue(weaponID, out bool unlocked) && unlocked;
    }

    // REFATORAÇÃO: deviamos checar não só a quantidade de buildables, mas de granadas e medkits tbm.
    /// <summary>
    /// Gets the current quantity of a buildable item in inventory.
    /// </summary>
    /// <param name="buildableID">The buildable type ID.</param>
    /// <returns>Current quantity (0-5).</returns>
    public int GetBuildableQuantity(string buildableID) {
        return buildableQuantities.TryGetValue(buildableID, out int qty) ? qty : 0;
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

        Debug.Log($"[PlayerProgress] {weaponID} upgraded to level {weaponLevels[weaponID]}");
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

    // REFATORAÇÃO: o check deve ser se a arma está no maximo nivel mas isso deve ser dinamico, barricadas por exemplo pode ir até o nivel 5, a pistola até o nivel 10. O nível máximo de upgrade para cada item pode ser definido no ScriptableObject do item, permitindo que diferentes tipos de itens tenham diferentes limites de upgrade. O método IsItemMaxLevel(string itemID) pode verificar o tipo do item e comparar o nível atual com o nível máximo definido no SO do item para determinar se o item está no nível máximo.
    /// <summary>
    /// Checks if a weapon is at maximum level.
    /// </summary>
    /// <param name="weaponID">The weapon to check.</param>
    /// <returns>True if at level 10.</returns>
    public bool IsWeaponMaxLevel(string weaponID) {
        return GetWeaponLevel(weaponID) >= MAX_UPGRADE_LEVEL;
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

    #endregion
}
