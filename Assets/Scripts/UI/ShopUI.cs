using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack;

/* REFATORAÇÃO: Esse script, assim como CreditsUI, ControlsUI, deve ser responsavel por: mostrar/ocultar o painel, mostrar o weaponpreview, mostrar os textos do item selecionado, atualizar a moeda atual, ter os botões de comprar mais munição, desbloquear ou fazer upgrade do item seleciondado, mostrar os stats dele, e atualizar tudo isso quando o jogador clicar em um card diferente. Temos o seguinte problema nesse script: ele esta responsavel por coisas demais, está grande demais. Coisas como por exemplo a distancia da camera, como já comentado no script de WeaponPreviewHandler, poderia ser responsabilidade desse outro script mesmo. Todo esse sistema de preview  em 3d, o ShopUI poderia ter só a referencia do que 
é pra ser mostrado, e a lógica de como fazer isso estar em outro script. 

O que mover: Métodos como RebuildPreviewModel, RotatePreviewModel, DestroyPreviewModel e as funções recursivas de AssignWeaponLayer.

Para onde: Crie um script chamado WeaponPreviewManager ou ItemPreviewer.

Como ficaria: O ShopUI apenas diria: previewManager.ShowItem(itemData);. O novo script cuidaria de toda a "sujeira" técnica de instanciar e girar o modelo.

Em realção a configurações da camera, como já dito no WeaponPreviewHandler, na verdade nao deveria ficar aqui nesse script nem deveria mexer na camera, na verdade deveriamos colocar assim: mudar a escala do modelo 3d pra ficar visivel no canvas, e  pode rajustar isso conforme o item pq eles tem tamanhos diferentes, poderia ser no ScritableObject do item, ter um campo pra isso, ou no script, outro script no caso. além disso, logica de preços devem ir para outro Script também, talvez shopmanager, talvez dentro do proprio SO, questão de analise.

Outro ponto: calculos nao devem ficar aqui, como o calculo da barra de status, deve ir pra outro script talvez WeaponStatsCalculator, ou StatBlockDisplay, questão de analisar e entender qual a melhor abordagem.

Mais uma analise: verificar se a arma está ou não desbloqueada, ou se chegou no nível máximo, isso poderia ser responsabilidade do próprio ShopItemCard,

já a questão de configurar o layout da grid, isso pode ser feito direto no editor, não precisa de código
*/

// REFATORAÇÃO: precisamos ser dinamicos na hora d emostrar stats, porque nem todo item tem os mesmos stats, por exemplo, armas tem dano, firerate e munição, mas um item de construção pode ter vida. medkit pode ter o quanto cura, etc. então pprecisamos de uma forma mais flexivel, precisamos analisar esse e outros scripts, ver como o projeto está agora e como podemos melhorá-lo.

// ULTIMA ANALISE: precismaos reduzir esse script,veriricar se há logica repetida, ou que deveria ser movida para outros scripts

/// <summary>
/// Manages the shop UI including item cards and shop panel interactions.
/// </summary>
public class ShopUI : BaseUI {

    #region SERIALIZED FIELDS

    [Header("Player Reference")]
    [SerializeField] private Character player;

    [Header("Shop Elements")]
    [SerializeField] private RectTransform itemsContainer;
    [SerializeField] private ShopItemCard shopItemCardPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("Selected Item")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private float previewRotationSpeed = 35f;
    [SerializeField] private TextMeshProUGUI selectedItemNameText;
    [SerializeField] private TextMeshProUGUI selectedItemDescriptionText;
    [SerializeField] private StatBlockDisplay damageBlockDisplay;
    [SerializeField] private StatBlockDisplay fireRateBlockDisplay;
    [SerializeField] private StatBlockDisplay ammoBlockDisplay;

    [Header("Ammo Button")]
    [SerializeField] private Button ammoButton;
    [SerializeField] private TextMeshProUGUI ammoPriceText;
    [SerializeField] private TextMeshProUGUI currentAmmoText;

    [Header("Action Button")]
    [SerializeField] private Button selectedItemActionButton;
    [SerializeField] private TextMeshProUGUI selectedItemActionButtonText;
    [SerializeField] private TextMeshProUGUI selectedItemPriceText;


    [SerializeField] private List<ShopItemDataSO> shopItems = new List<ShopItemDataSO>();

    [Header("Ammo Purchase")]
    // Aqui essa região também precisa ser refatorada, a lógica de preço e limite de munição deve ir para outro script, talvez ShopManager ou dentro do próprio ScriptableObject do item, questão de análise. O que deve ficar aqui: o botão acionando a compra d emunição, mostrar o preço e a munição atual, e atualizar isso quando o jogador clicar em um card diferente, ou seja, quando mudar a arma selecionada.
    [SerializeField] private List<ItemPricingConfig> itemPricingConfigs = new List<ItemPricingConfig>();


    #endregion

    #region FIELDS

    private ShopItemDataSO selectedItemData;
    private GameObject activePreviewModel;

    #endregion

    #region EVENTS

    /// <summary>
    /// Raised after a weapon is successfully unlocked in the shop.
    /// </summary>
    public static event Action<string> WeaponUnlocked;

    /// <summary>
    /// Raised after reserve ammo is successfully purchased in the shop.
    /// </summary>
    public static event Action<string, int> AmmoPurchased;

    #endregion

    #region UNITY

    protected override void Awake() {
        base.Awake();
        BindButtons();

        SubscribeToCurrencyEvents();
    }

    /// </summary>
    protected override void Update() {
        base.Update();

        RotatePreviewModel();
    }

    private void OnDestroy() {
        if (EconomyManager.Instance != null) {
            EconomyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
        }

        DestroyPreviewModel();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Binds button click events to their handlers.
    /// </summary>
    private void BindButtons() {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClick);

        if (ammoButton != null)
            ammoButton.onClick.AddListener(OnAmmoButtonPressed);
    }

    /// <summary>
    /// Shows the shop panel and prepares it for interaction.
    /// This is called whenever the player opens the shop.
    /// We populate items, select the first weapon, and update currency display here.
    /// </summary>
    public override void Show() {
        base.Show();

        PopulateShopItems();

        SelectInitialItem();

        UpdateCurrencyDisplay();
    }

    /// <summary>
    /// Hides the shop panel and cleans up temporary preview objects.
    /// This is called when the player closes the shop or switches to a different UI.
    /// We destroy the 3D weapon preview here to free up memory.
    /// </summary>
    public override void Hide() {
        base.Hide();

        DestroyPreviewModel();
    }

    /// <summary>
    /// Subscribes to EconomyManager events to update currency display.
    /// </summary>
    private void SubscribeToCurrencyEvents() {
        if (EconomyManager.Instance != null) {
            EconomyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
        }
    }

    /// <summary>
    /// Event handler for currency changes.
    /// Updates the currency display text and refreshes button interactable state.
    /// </summary>
    /// <param name="newAmount">The new currency amount.</param>
    private void OnCurrencyChanged(int newAmount) {
        UpdateCurrencyDisplay();

        if (selectedItemData != null) {
            UpdateActionButton(selectedItemData, 0);
        }
    }

    /// <summary>
    /// Updates the currency display text in the shop UI.
    /// </summary>
    private void UpdateCurrencyDisplay() {
        if (currencyText == null || EconomyManager.Instance == null) return;

        int currentCurrency = EconomyManager.Instance.GetCurrentCurrency();
        currencyText.text = $"${currentCurrency:N0}";
    }

    /// <summary>
    /// Populates the shop with item cards based on the inspector item list.
    /// This is the entry point for creating all the shop items the player can browse.
    /// </summary>
    private void PopulateShopItems() {
        if (itemsContainer == null || shopItemCardPrefab == null) {
            Debug.LogWarning($"{nameof(ShopUI)} has missing references for items container or card prefab.", this);
            return;
        }

        ClearShopItems();

        CreateConfiguredItems();
    }

    /// <summary>
    /// Creates card instances from the configured inspector data.
    /// </summary>
    private void CreateConfiguredItems() {
        if (shopItems == null || shopItems.Count == 0) {
            Debug.LogWarning($"{nameof(ShopUI)} has no configured shop items.", this);
            return;
        }

        for (int index = 0; index < shopItems.Count; index++) {
            ShopItemDataSO itemData = shopItems[index];

            if (itemData == null) {
                Debug.LogWarning($"{nameof(ShopUI)} has a null item entry at index {index}.", this);
                continue;
            }

            ShopItemCard card = Instantiate(shopItemCardPrefab, itemsContainer);

            card.SetCallbacks(HandleCardSelected, HandleCardStateChanged, HandleCardUnlockUpgrade);

            card.Setup(itemData);
        }
    }

    /// <summary>
    /// Clears all existing shop item cards from the container.
    /// </summary>
    private void ClearShopItems() {
        selectedItemData = null;

        foreach (Transform child in itemsContainer) {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Selects the first configured weapon so the right panel is populated when the shop opens.
    /// </summary>
    private void SelectInitialItem() {
        ShopItemDataSO fallbackItem = null;

        for (int index = 0; index < shopItems.Count; index++) {
            ShopItemDataSO itemData = shopItems[index];
            if (itemData == null) {
                continue;
            }

            if (fallbackItem == null) {
                fallbackItem = itemData;
            }

            if (itemData.ItemData is WeaponDataSO) {
                HandleCardSelected(itemData);
                return;
            }
        }

        // CONCEITO: Safety check. If no items were found, log warning instead of crashing.
        // This happens when shop isn't configured in Inspector yet.
        if (fallbackItem != null) {
            HandleCardSelected(fallbackItem);
        } else {
            Debug.LogWarning($"{nameof(ShopUI)}.{nameof(SelectInitialItem)}: No valid shop items found. Configure shop items in Inspector.", this);
        }
    }

    /// <summary>
    /// Updates the right-side selected item panel when a card is clicked.
    /// </summary>
    /// <param name="itemData">Data from the clicked card.</param>
    private void HandleCardSelected(ShopItemDataSO itemData) {
        selectedItemData = itemData;

        UpdateSelectedItemInfo();

        RebuildPreviewModel();
    }

    /// <summary>
    /// Keeps right-side stats synchronized after purchases and upgrades on the selected card.
    /// </summary>
    /// <param name="itemData">Data from the card that changed state.</param>
    private void HandleCardStateChanged(ShopItemDataSO itemData) {
        if (selectedItemData == null || itemData == null || itemData != selectedItemData) {
            return;
        }

        UpdateSelectedItemInfo();
    }

    /// <summary>
    /// Handles unlock/upgrade button click from the card, delegating logic to right-panel action button.
    /// </summary>
    private void HandleCardUnlockUpgrade(ShopItemDataSO itemData) {
        selectedItemData = itemData;
        UpdateSelectedItemInfo();
        if (selectedItemActionButton != null) {
            selectedItemActionButton.onClick.Invoke();
        }
    }

    /// <summary>
    /// Writes the selected item's stats to the right-side panel fields using stat blocks.
    /// </summary>
    private void UpdateSelectedItemInfo() {
        if (selectedItemData == null) {
            SetSelectedInfoTexts(string.Empty, string.Empty);
            ClearStatBlocks();
            UpdateActionButton(null, 0);
            ClearAmmoDisplay();
            return;
        }

        string itemName = selectedItemData.ItemName;
        string description = selectedItemData.Description;
        string priceText = string.Empty;

        if (PlayerProgress.Instance != null) {
            int currentLevel = PlayerProgress.Instance.GetWeaponLevel(selectedItemData.ItemID);
            int maxLevel = PlayerProgress.Instance.GetItemMaxLevel(selectedItemData.ItemID);

            if (currentLevel >= selectedItemData.LevelToUnlockExclusive) {
                description = selectedItemData.ExclusivePowerDescription;
            }
        }

        SetSelectedInfoTexts(itemName, description);

        // REFATORAÇÃO: isso tudo nao deveria ser responsabilidade do ShopUI, precisamos encontrar qual script deve ser responsavel por processar essa lógica e mover para ele, e chamar aqui somente  a função  
        if (selectedItemData.ItemData is WeaponDataSO weaponData) {
            int level = 1;
            if (PlayerProgress.Instance != null) {
                level = Mathf.Max(1, PlayerProgress.Instance.GetWeaponLevel(selectedItemData.ItemID));
            }

            int maxLevel = PlayerProgress.Instance.GetItemMaxLevel(selectedItemData.ItemID);
            bool canUpgrade = level < maxLevel;

            float currentDamageNormalized = WeaponStatsCalculator.CalculateAndNormalizeDamage(weaponData, level);
            float currentFireRateNormalized = WeaponStatsCalculator.CalculateAndNormalizeFireRate(weaponData, level);
            float currentAmmoNormalized = WeaponStatsCalculator.CalculateAndNormalizeAmmo(weaponData, level);

            float nextDamageNormalized = canUpgrade
                ? WeaponStatsCalculator.CalculateAndNormalizeDamage(weaponData, level + 1)
                : currentDamageNormalized;
            float nextFireRateNormalized = canUpgrade
                ? WeaponStatsCalculator.CalculateAndNormalizeFireRate(weaponData, level + 1)
                : currentFireRateNormalized;
            float nextAmmoNormalized = canUpgrade
                ? WeaponStatsCalculator.CalculateAndNormalizeAmmo(weaponData, level + 1)
                : currentAmmoNormalized;

            if (damageBlockDisplay != null) {
                damageBlockDisplay.SetMaxStatValue(WeaponStatsCalculator.STAT_BARS);
                damageBlockDisplay.SetStatValues(currentDamageNormalized, nextDamageNormalized);
            }

            if (fireRateBlockDisplay != null) {
                fireRateBlockDisplay.SetMaxStatValue(WeaponStatsCalculator.STAT_BARS);
                fireRateBlockDisplay.SetStatValues(currentFireRateNormalized, nextFireRateNormalized);
            }

            if (ammoBlockDisplay != null) {
                ammoBlockDisplay.SetMaxStatValue(WeaponStatsCalculator.STAT_BARS);
                ammoBlockDisplay.SetStatValues(currentAmmoNormalized, nextAmmoNormalized);
            }

            UpdateAmmoDisplay(selectedItemData.ItemID);
        } else {
            ClearStatBlocks();
            ClearAmmoDisplay();
        }

        UpdateActionButton(selectedItemData, 0);
    }

    /// <summary>
    /// Clears stat block displays (resets to empty).
    /// </summary>
    private void ClearStatBlocks() {
        if (damageBlockDisplay != null) damageBlockDisplay.SetStatValues(0f, 0f);
        if (fireRateBlockDisplay != null) fireRateBlockDisplay.SetStatValues(0f, 0f);
        if (ammoBlockDisplay != null) ammoBlockDisplay.SetStatValues(0f, 0f);
    }

    /// <summary>
    /// Updates ammo/quantity display for the selected item.
    /// Shows current reserve quantity and the cost to purchase more.
    /// CONCEITO: Works for all item types (weapons, buildables, consumables)
    /// by using generic GetItemPricingConfig() and checking IsItemUnlocked().
    /// SPECIAL CASE: For Vest, button repairs the vest instead of buying ammo.
    /// </summary>
    private void UpdateAmmoDisplay(string weaponID) {
        if (string.IsNullOrEmpty(weaponID) || PlayerProgress.Instance == null) {
            ClearAmmoDisplay();
            return;
        }

        ItemPricingConfig pricingConfig = GetItemPricingConfig(weaponID);

        if (string.IsNullOrEmpty(pricingConfig.itemID)) {
            ClearAmmoDisplay();
            return;
        }

        bool isVestItem = selectedItemData != null && selectedItemData.ItemData is VestDataSO;

        if (isVestItem) {
            UpdateAmmoDisplayForVest(pricingConfig);
            return;
        }

        int currentAmmo = PlayerProgress.Instance.GetWeaponReserveAmmo(weaponID);

        if (currentAmmoText != null) {
            currentAmmoText.text = $"{currentAmmo}/{pricingConfig.maxReserveQuantity}";
        }

        if (currentAmmo >= pricingConfig.maxReserveQuantity) {
            if (ammoPriceText != null) {
                ammoPriceText.text = "FULL";
            }
            if (ammoButton != null) {
                ammoButton.interactable = false;
            }
        } else {
            if (ammoPriceText != null) {
                ammoPriceText.text = $"${pricingConfig.costPerPurchase:N0}";
            }

            bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(weaponID);
            if (ammoButton != null) {
                ammoButton.interactable = isUnlocked &&
                                         EconomyManager.Instance != null &&
                                         EconomyManager.Instance.CanAfford(pricingConfig.costPerPurchase);
            }
        }
    }

    /// <summary>
    /// Special handling for Vest repair in the shop.
    /// Button repairs the vest instead of buying ammo.
    /// Disabled if vest is at 100% health.
    /// 
    /// DIFERENÇA PARA ARMAS:
    /// - Armas: botão compra munição de reserva
    /// - Vest: botão repara a armadura (chama RepairVest)
    /// </summary>
    private PlayerArmor GetPlayerArmor() {
        return player != null ? player.GetComponent<PlayerArmor>() : null;
    }

    /*=========================================================================
        UpdateAmmoDisplayForVest - Atualiza o botão +ammo quando Vest selecionada
        
        Estados posibles:
        - LOCKED: ainda não foi desbloqueada
        - X%: Porcentagem atual de armadura
        - FULL: armadura em 100% (botão desabilitado)
        - $XX: preço para reparo
    =========================================================================*/
    private void UpdateAmmoDisplayForVest(ItemPricingConfig pricingConfig) {
        PlayerArmor playerArmor = GetPlayerArmor();

        if (playerArmor == null || ammoButton == null) {
            if (ammoButton != null) ammoButton.interactable = false;
            return;
        }

        float armorFraction = playerArmor.GetArmorFraction();
        bool isFull = armorFraction >= 1f;
        bool isUnlocked = PlayerProgress.Instance != null && PlayerProgress.Instance.IsItemUnlocked(selectedItemData.ItemID);
        //bool isBroken = armorFraction <= 0f; // Variável não usada - podemos remover

        if (currentAmmoText != null) {
            if (!isUnlocked) {
                currentAmmoText.text = "LOCKED";
            } else {
                int currentArmor = Mathf.RoundToInt(armorFraction * 100f);
                currentAmmoText.text = $"{currentArmor}%";
            }
        }

        // Se não está desbloqueado, mostrar "LOCKED" e desabilitar botão
        if (!isUnlocked) {
            if (ammoPriceText != null) {
                ammoPriceText.text = "LOCKED";
            }
            ammoButton.interactable = false;
        }
        // Se está cheia, mostrar "FULL"
        else if (isFull) {
            if (ammoPriceText != null) {
                ammoPriceText.text = "FULL";
            }
            ammoButton.interactable = false;
        }
        // Se está quebrada ou danificada, mostrar preço e habilitar botão
        else {
            if (ammoPriceText != null) {
                ammoPriceText.text = $"${pricingConfig.costPerPurchase:N0}";
            }

            ammoButton.interactable = EconomyManager.Instance != null &&
                                     EconomyManager.Instance.CanAfford(pricingConfig.costPerPurchase);
        }
    }

    /// <summary>
    /// Clears ammo display texts (resets to empty).
    /// </summary>
    private void ClearAmmoDisplay() {
        if (currentAmmoText != null) currentAmmoText.text = string.Empty;
        if (ammoPriceText != null) ammoPriceText.text = string.Empty;
        if (ammoButton != null) ammoButton.interactable = false;
    }

    /// <summary>
    /// Assigns all right-side text fields in one place to keep UI updates consistent.
    /// </summary>
    private void SetSelectedInfoTexts(string itemName, string description) {
        if (selectedItemNameText != null) selectedItemNameText.text = itemName;
        if (selectedItemDescriptionText != null) selectedItemDescriptionText.text = description;
    }

    /// <summary>
    /// Updates the right-side action button state (Unlock, Upgrade, Buy Ammo, etc).
    /// CONCEITO: Generic check using IsItemUnlocked() instead of IsWeaponUnlocked()
    /// allows this to work for all item types (weapons, consumables, buildables).
    /// This ensures all 9 items behave consistently.
    /// </summary>
    private void UpdateActionButton(ShopItemDataSO itemData, int dummy) {
        if (selectedItemActionButton == null || itemData == null) {
            return;
        }

        selectedItemActionButton.onClick.RemoveAllListeners();

        if (PlayerProgress.Instance == null || EconomyManager.Instance == null) {
            selectedItemActionButton.interactable = false;
            return;
        }

        string itemID = itemData.ItemID;

        // CONCEITO: Use IsItemUnlocked() instead of IsWeaponUnlocked()
        // IsItemUnlocked() checks all dictionaries (weapons, buildables, consumables)
        bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(itemID);
        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemID);
        // CONCEITO: Use dynamic max level from item's ScriptableObject
        int maxLevel = PlayerProgress.Instance.GetItemMaxLevel(itemID);
        bool isMaxLevel = currentLevel >= maxLevel;

        if (!isUnlocked) {
            int cost = itemData.UnlockCost;
            if (selectedItemActionButtonText != null)
                selectedItemActionButtonText.text = "Unlock";

            if (selectedItemPriceText != null)
                selectedItemPriceText.text = $"${cost:N0}";

            selectedItemActionButton.interactable = EconomyManager.Instance.CanAfford(cost);
            selectedItemActionButton.onClick.AddListener(() => OnRightPanelUnlock(itemData));
        } else if (isMaxLevel) {
            if (selectedItemActionButtonText != null)
                selectedItemActionButtonText.text = "Maxed Out";

            selectedItemActionButton.interactable = false;

            // Hide price text when at max level
            if (selectedItemPriceText != null)
                selectedItemPriceText.text = string.Empty;
        } else {
            int cost = CalculateUpgradeCostForItem(itemData, currentLevel);
            if (selectedItemActionButtonText != null)
                selectedItemActionButtonText.text = "Upgrade";

            if (selectedItemPriceText != null)
                selectedItemPriceText.text = cost > 0 ? $"${cost:N0}" : string.Empty;

            selectedItemActionButton.interactable = cost > 0 && EconomyManager.Instance.CanAfford(cost);
            selectedItemActionButton.onClick.AddListener(() => OnRightPanelUpgrade(itemData));
        }

        // CONCEITO: After unlock, immediately enable the ammo button
        // if this item has ammo/quantity configuration. This allows players
        // to buy ammo immediately after unlocking without needing to reopen the shop.
        UpdateAmmoDisplay(itemID);
    }

    /// <summary>
    /// Handles unlock from the right-panel action button.
    /// </summary>
    private void OnRightPanelUnlock(ShopItemDataSO itemData) {
        if (itemData == null || EconomyManager.Instance == null || PlayerProgress.Instance == null) {
            Debug.LogWarning($"[ShopUI] OnRightPanelUnlock: null reference detected!");
            return;
        }

        Debug.Log($"[ShopUI] OnRightPanelUnlock called for '{itemData.ItemName}' (ID: {itemData.ItemID}, Cost: {itemData.UnlockCost})");

        if (EconomyManager.Instance.TrySpendCurrency(itemData.UnlockCost)) {
            Debug.Log($"[ShopUI] Currency spent successfully. Now unlocking item...");
            PlayerProgress.Instance.UnlockItem(itemData);
            Debug.Log($"[ShopUI] Unlocked {itemData.ItemName}!");

            // REFATORAÇÃO: precisamos mesmo ter 2 chamadas diferentes? nao deveriamos ter só um méetodo para desbloquear qualquer item, sendo arma ou medkit, buildable, qualquer um?
            if (itemData.ItemData is WeaponDataSO) {
                Debug.Log($"[ShopUI] WeaponDataSO detected, invoking WeaponUnlocked event");
                WeaponUnlocked?.Invoke(itemData.ItemID);
            } else if (itemData.ItemData is BuildableDataSO) {
                Debug.Log($"[ShopUI] BuildableDataSO detected for '{itemData.ItemName}'");
                // BuildableDataSO unlock is handled directly by PlayerProgress.UnlockItem()
            } else if (itemData.ItemData is MedkitDataSO) {
                Debug.Log($"[ShopUI] MedkitDataSO detected");
            } else if (itemData.ItemData is GrenadeDataSO) {
                Debug.Log($"[ShopUI] GrenadeDataSO detected");
            } else if (itemData.ItemData is VestDataSO) {
                Debug.Log($"[ShopUI] VestDataSO detected - auto-equipping Vest");
                EquipVestOnUnlock();
            }

            RefreshAllCards();
        } else {
            int missingAmount = itemData.UnlockCost - EconomyManager.Instance.GetCurrentCurrency();
            Debug.LogWarning($"[ShopUI] Insufficient funds! Need {missingAmount} more coins.");
        }
    }

    /// <summary>
    /// Handles upgrade from the right-panel action button.
    /*=========================================================================
    OnRightPanelUpgrade - Treats the upgrade button click
    
    When upgrading items in the shop:
    - Weapons: just upgrades stats (damage, firerate, etc)
    - Vest: in addition to stats, repairs to 100% and shows the HUD
    
    CONCEITO: We check if ItemData is VestDataSO to handle specially.
    =========================================================================*/
    private void OnRightPanelUpgrade(ShopItemDataSO itemData) {
        if (itemData == null || UpgradeManager.Instance == null || PlayerProgress.Instance == null) {
            return;
        }

        int currentLevel = PlayerProgress.Instance.GetItemLevel(itemData.ItemID);

        // Use TryUpgradeItem for ALL item types (works for Vest, weapons, etc.)
        if (UpgradeManager.Instance.TryUpgradeItem(itemData.ItemID, itemData.BaseUpgradeCost)) {
            int newLevel = PlayerProgress.Instance.GetItemLevel(itemData.ItemID);
            int maxLevel = PlayerProgress.Instance.GetItemMaxLevel(itemData.ItemID);
            Debug.Log($"[ShopUI] Upgraded {itemData.ItemName} para nível {newLevel}!");

            // Se for Vest, além de fazer upgrade, reparamos armor
            if (itemData.ItemData is VestDataSO) {
                PlayerArmor playerArmor = GetPlayerArmor();
                if (playerArmor != null) {
                    playerArmor.RepairVest();
                }
                PlayVestEquippedSound();

                // If reached max level (level 5), enable regeneration exclusive
                if (newLevel >= maxLevel) {
                    Debug.Log($"[ShopUI] {itemData.ItemName} reached MAX LEVEL! Enabling regeneration!");
                    PlayerArmor playerArmor2 = GetPlayerArmor();
                    if (playerArmor2 != null) {
                        playerArmor2.EnableRegeneration();
                    }
                }

                // Mostrar UI (se estava escondida)
                PlayerArmorUI armorUI = FindObjectOfType<PlayerArmorUI>();
                if (armorUI != null) {
                    armorUI.ShowArmorUI();
                }
            }

            RefreshAllCards();
        } else {
            int cost = CalculateUpgradeCostForItem(itemData, currentLevel);
            int missingAmount = cost - EconomyManager.Instance.GetCurrentCurrency();
            Debug.LogWarning($"[ShopUI] Insufficient funds! Need {missingAmount} more coins.");
        }
    }

    /// <summary>
    /// Helper to calculate upgrade cost for a given item at its current level.
    /// </summary>
    private int CalculateUpgradeCostForItem(ShopItemDataSO itemData, int currentLevel) {
        return UpgradeManager.Instance != null ? UpgradeManager.Instance.GetNextUpgradeCost(itemData.ItemID, itemData.BaseUpgradeCost) : 0;
    }

    /// <summary>
    /// Finds the pricing configuration for the specified item ID.
    /// Returns a default struct if not found (itemID will be empty string).
    /// CONCEITO: Renamed from GetWeaponAmmoPricing to GetItemPricingConfig
    /// to support all item types (weapons, consumables, buildables).
    /// </summary>
    private ItemPricingConfig GetItemPricingConfig(string itemID) {
        return itemPricingConfigs.FirstOrDefault(c => c.itemID == itemID);
    }

    /// <summary>
    /// Handles ammo/quantity purchase button click.
    /// Validates funds, adds quantity to reserve, and deducts currency.
    /// CONCEITO: Works for all item types (weapons, buildables, consumables).
    /// Gets pricing from unified ItemPricingConfig.
    /// </summary>
    private void OnAmmoButtonPressed() {
        if (selectedItemData == null) return;

        string itemID = selectedItemData.ItemID;
        ItemPricingConfig pricingConfig = GetItemPricingConfig(itemID);

        if (string.IsNullOrEmpty(pricingConfig.itemID)) {
            Debug.LogWarning($"[ShopUI.OnAmmoButtonPressed] No pricing configuration found for item: {itemID}");
            return;
        }

        bool isVestItem = selectedItemData.ItemData is VestDataSO;

        if (isVestItem) {
            RepairVest(pricingConfig);
            return;
        }

        if (!EconomyManager.Instance.CanAfford(pricingConfig.costPerPurchase)) {
            int missingAmount = pricingConfig.costPerPurchase - EconomyManager.Instance.GetCurrentCurrency();
            Debug.LogWarning($"[ShopUI.OnAmmoButtonPressed] Insufficient funds! Need {missingAmount} more coins.");
            return;
        }

        int currentQuantity = PlayerProgress.Instance.GetWeaponReserveAmmo(itemID);
        int newQuantity = currentQuantity + pricingConfig.quantityPerPurchase;

        newQuantity = Mathf.Clamp(newQuantity, 0, pricingConfig.maxReserveQuantity);

        int actualQuantityAdded = newQuantity - currentQuantity;

        if (actualQuantityAdded <= 0) {
            Debug.LogWarning($"[ShopUI.OnAmmoButtonPressed] Item {itemID} already at max quantity ({pricingConfig.maxReserveQuantity})!");
            return;
        }

        float quantityProportion = (float)actualQuantityAdded / pricingConfig.quantityPerPurchase;
        int actualCost = Mathf.RoundToInt(pricingConfig.costPerPurchase * quantityProportion);

        if (EconomyManager.Instance.TrySpendCurrency(actualCost)) {
            PlayerProgress.Instance.AddWeaponReserveAmmo(itemID, actualQuantityAdded);

            Debug.Log($"[ShopUI.OnAmmoButtonPressed] Purchased {actualQuantityAdded} quantity for {itemID}. Cost: ${actualCost}. New total: {newQuantity}");
            AmmoPurchased?.Invoke(itemID, actualQuantityAdded);

            UpdateSelectedItemInfo();
        }
    }

    /*=========================================================================
    RepairVest - Repara a vest quando jogador clica em +ammo
        
    Quando o jogador tem a Vest danificada (armor < 100%) e clica no botão +ammo,
    este método:
    1. Verifica se pode pagar
    2. Define armor = 100
    3. Toca som
    4. Mostra UI (se estava escondida)
    
    Note: Reparar usa AddArmor(100f) ao invés de RepairVest() porque
    o AddArmor também dispara evento para UI.
    =========================================================================*/
    private void RepairVest(ItemPricingConfig pricingConfig) {
        PlayerArmor playerArmor = GetPlayerArmor();

        if (playerArmor == null) {
            Debug.LogWarning("[ShopUI RepairVest] PlayerArmor não encontrado!");
            return;
        }

        // Verificar se tem dinheiro
        if (!EconomyManager.Instance.CanAfford(pricingConfig.costPerPurchase)) {
            int missingAmount = pricingConfig.costPerPurchase - EconomyManager.Instance.GetCurrentCurrency();
            Debug.LogWarning($"[ShopUI RepairVest] funds insuficientes! Precisa de {missingAmount} coins a mais.");
            return;
        }

        float armorFraction = playerArmor.GetArmorFraction();

        // Se já está cheia, não precisa reparo
        if (armorFraction >= 1f) {
            Debug.Log("[ShopUI RepairVest] Vest já está com armadura cheia!");
            return;
        }

        // Deduzir custo e reparo
        if (EconomyManager.Instance.TrySpendCurrency(pricingConfig.costPerPurchase)) {
            playerArmor.AddArmor(100f);

            Debug.Log($"[ShopUI.RepairVest] Vest reparada por ${pricingConfig.costPerPurchase}. Nova armadura: 100%");

            // Tocar som (same som de equipar)
            PlayVestEquippedSound();
            PlayVestEquippedSound();

            PlayerArmorUI armorUI = FindObjectOfType<PlayerArmorUI>();
            if (armorUI != null) {
                armorUI.ShowArmorUI();
            }

            UpdateSelectedItemInfo();
        }
    }

    /*=========================================================================
    PlayVestEquippedSound - Toca o som de vest equipped
        
    PROCURA DO COMPONENTE VEST:
    - Primeiro tenta no mesmo GameObject do Character
    - Se não encontrar, procura nos filhos (children)
    
    Isso é necessário porque não sabemos exatamente onde o script Vest está.
=========================================================================*/
    private void PlayVestEquippedSound() {
        if (player == null) {
            Debug.LogWarning("[ShopUI] player é null, não posso encontrar Vest!");
            return;
        }

        // Procurar o componente Vest no player ou seus filhos
        Vest vest = player.GetComponent<Vest>();
        if (vest == null) {
            vest = player.GetComponentInChildren<Vest>();
        }

        if (vest != null) {
            vest.PlayEquippedSound();
            Debug.Log("[ShopUI] PlayVestEquippedSound() chamado!");
        } else {
            Debug.LogWarning("[ShopUI] Componente Vest não encontrado no player!");
        }
    }

    /// <summary>
    /// Refreshes all card states after a purchase/upgrade (so levels and colors update).
    /// </summary>
    private void RefreshAllCards() {
        foreach (Transform child in itemsContainer) {
            ShopItemCard card = child.GetComponent<ShopItemCard>();
            if (card != null) {
                card.RefreshCardState();
            }
        }

        UpdateSelectedItemInfo();
    }

    /*=========================================================================
        EquipVestOnUnlock - Equip a vest quando jogador desbloqueia
        
        O que acontece quando jogador clica em "Unlock" para a Vest:
        1. Chama EquipVest() no PlayerArmor → define armor = 100
        2. Toca som de equipar (PlayVestEquippedSound)
        3. Mostra a UI da armadura (ShowArmorUI)
        
        Isso faz o colete funcionar pela primeira vez.
    =========================================================================*/
    private void EquipVestOnUnlock() {
        Debug.Log("[ShopUI] EquipVestOnUnlock chamado!");

        PlayerArmor playerArmor = GetPlayerArmor();
        Debug.Log($"[ShopUI] PlayerArmor encontrado: {playerArmor != null}");

        if (playerArmor != null) {
            // 1. Equipar (definir armor = 100)
            playerArmor.EquipVest();
            Debug.Log("[ShopUI] playerArmor.EquipVest() chamado!");

            // 2. Tocar som
            PlayVestEquippedSound();

            // 3. Mostrar UI
            PlayerArmorUI armorUI = FindObjectOfType<PlayerArmorUI>();
            Debug.Log($"[ShopUI] PlayerArmorUI encontrado: {armorUI != null}");

            if (armorUI != null) {
                armorUI.ShowArmorUI();
                Debug.Log("[ShopUI] armorUI.ShowArmorUI() chamado!");
            } else {
                Debug.LogError("[ShopUI] PlayerArmorUI não encontrado na cena!");
            }

            Debug.Log("[ShopUI] Vest equipada e HUD mostrado!");
        } else {
            Debug.LogError("[ShopUI] PlayerArmor não encontrado!");
        }
    }

    /// <summary>
    /// Recreates the 3D preview model for the currently selected shop item.
    /// </summary>
    private void RebuildPreviewModel() {
        // CONCEITO: Safety check. If selectedItemData or PreviewPrefab is null,
        // skip preview creation. This prevents crashes when shop isn't configured yet.
        if (selectedItemData == null || selectedItemData.PreviewPrefab == null) {
            Debug.LogWarning($"{nameof(ShopUI)}.{nameof(RebuildPreviewModel)}: selectedItemData or PreviewPrefab is null. Shop may not be configured.", this);
            return;
        }

        DestroyPreviewModel();

        activePreviewModel = Instantiate(selectedItemData.PreviewPrefab, previewAnchor);

        activePreviewModel.transform.localPosition = selectedItemData.PreviewPositionOffset;

        Quaternion rotationOffset = Quaternion.Euler(selectedItemData.PreviewRotationOffset);
        activePreviewModel.transform.localRotation = rotationOffset;

        activePreviewModel.transform.localScale = selectedItemData.PreviewScale;

        AssignWeaponLayer(activePreviewModel);
    }

    /// <summary>
    /// Rotates the selected preview model using unscaled time so it keeps rotating while gameplay is paused.
    /// </summary>
    private void RotatePreviewModel() {
        if (activePreviewModel == null) {
            return;
        }

        activePreviewModel.transform.Rotate(Vector3.up, previewRotationSpeed * Time.unscaledDeltaTime, Space.Self);
    }

    /// <summary>
    /// Destroys the active instantiated preview model, if any.
    /// </summary>
    private void DestroyPreviewModel() {
        if (activePreviewModel != null) {
            Destroy(activePreviewModel);
            activePreviewModel = null;
        }
    }

    /// <summary>
    /// Assigns the "Weapon" layer to a GameObject and all its children.
    /// </summary>
    private void AssignWeaponLayer(GameObject targetGameObject) {
        int weaponLayerID = LayerMask.NameToLayer("Weapon");

        if (weaponLayerID < 0) {
            Debug.LogError("[AssignWeaponLayer] Layer 'Weapon' does not exist! Create it in Edit → Project Settings → Tags and Layers");
            return;
        }

        targetGameObject.layer = weaponLayerID;

        AssignWeaponLayerToChildren(targetGameObject.transform, weaponLayerID);
    }

    /// <summary>
    /// Recursively assigns the "Weapon" layer to all child GameObjects.
    /// </summary>
    private void AssignWeaponLayerToChildren(Transform parent, int layerID) {
        foreach (Transform child in parent) {
            child.gameObject.layer = layerID;

            AssignWeaponLayerToChildren(child, layerID);
        }
    }

    /// <summary>
    /// <summary>
    /// Handles the close button click event.
    /// </summary>
    private void OnCloseClick() {
        if (ShopManager.Instance != null)
            ShopManager.Instance.CloseShop();
    }
}

    #endregion

/// <summary>
/// Serializable struct to configure purchase pricing and limits for items (weapons, buildables, consumables).
/// Defines how much quantity/ammo is added per purchase, the cost, and maximum reserve.
/// CONCEITO: Renamed from WeaponAmmoPricing to ItemPricingConfig to support all item types.
/// All items work the same way: buy quantity, add to reserve, respect max limit.
/// </summary>
[System.Serializable]
public struct ItemPricingConfig {

    [Tooltip("The unique identifier for the item (e.g., 'Pistol', 'Barricade', 'Medkit').")]
    [SerializeField]
    public string itemID;

    [Tooltip("Amount of quantity/ammo added per purchase.")]
    [SerializeField]
    public int quantityPerPurchase;

    [Tooltip("Cost in currency per purchase.")]
    [SerializeField]
    public int costPerPurchase;

    [Tooltip("Maximum reserve quantity/ammo this item can hold.")]
    [SerializeField]
    public int maxReserveQuantity;
}

// Legacy alias for backwards compatibility
public struct WeaponAmmoPricing {

    public string weaponID;
    public int ammoPerPurchase;
    public int costPerPurchase;
    public int maxReserveAmmo;

    public WeaponAmmoPricing(ItemPricingConfig config) {
        weaponID = config.itemID;
        ammoPerPurchase = config.quantityPerPurchase;
        costPerPurchase = config.costPerPurchase;
        maxReserveAmmo = config.maxReserveQuantity;
    }
}
