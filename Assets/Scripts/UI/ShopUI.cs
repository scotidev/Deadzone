using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private TextMeshProUGUI selectedItemPriceText;

    [Header("Ammo Button")]
    [SerializeField] private Button ammoButton;
    [SerializeField] private TextMeshProUGUI ammoPriceText;
    [SerializeField] private TextMeshProUGUI currentAmmoText;

    [Header("Action Button")]
    [SerializeField] private Button selectedItemActionButton;
    [SerializeField] private TextMeshProUGUI selectedItemActionButtonText;
    [SerializeField] private float previewModelScale = 100f; //deve ser refatorado, decidiremos a escala do preview no próprio ScriptableObject do item ou no script responsavel por isso.

    [SerializeField] private List<ShopItemDataSO> shopItems = new List<ShopItemDataSO>();

    [Header("Camera Adjustment")]
    // Diferentes armas têm tamanhos diferentes. Para que a câmera enquadre bem cada uma,
    // você pode customizar a posição Z da câmera por arma.
    // Tinhamos feito desse jeito ajustando a camera, mas agora vamos fazer ajustando o tamanho de cada preview, então isso pode ser removido pra dar lugar a um campo de escala no ScriptableObject do item ou no script responsavel por isso.
    [SerializeField] private List<WeaponCameraZPosition> cameraZPositions = new List<WeaponCameraZPosition>();
    [Header("Ammo Purchase")]
    // Aqui essa região também precisa ser refatorada, a lógica de preço e limite de munição deve ir para outro script, talvez ShopManager ou dentro do próprio ScriptableObject do item, questão de análise. O que deve ficar aqui: o botão acionando a compra d emunição, mostrar o preço e a munição atual, e atualizar isso quando o jogador clicar em um card diferente, ou seja, quando mudar a arma selecionada.
    [SerializeField] private List<WeaponAmmoPricing> weaponAmmoPricings = new List<WeaponAmmoPricing>();


    #endregion

    #region FIELDS

    private ShopItemDataSO selectedItemData;
    private GameObject activePreviewModel;
    private float originalCameraZ; // deve  ser removido na refatoração, a ideia é não mexer mais na câmera, mas sim ajustar a escala do modelo 3d para que ele fique visível no canvas, e isso pode ser configurado por item no ScriptableObject ou em outro script.

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

        Camera previewCamera = GetWeaponPreviewCamera();
        if (previewCamera != null) {
            originalCameraZ = previewCamera.transform.position.z;
        }
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

        HandleCardSelected(fallbackItem);
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

            float currentDamageNormalized = WeaponStatsCalculator.CalculateAndNormalizeDamage(weaponData, level);
            float currentFireRateNormalized = WeaponStatsCalculator.CalculateAndNormalizeFireRate(weaponData, level);
            float currentAmmoNormalized = WeaponStatsCalculator.CalculateAndNormalizeAmmo(weaponData, level);

            float nextDamageNormalized = level < PlayerProgress.MAX_UPGRADE_LEVEL
                ? WeaponStatsCalculator.CalculateAndNormalizeDamage(weaponData, level + 1)
                : currentDamageNormalized;
            float nextFireRateNormalized = level < PlayerProgress.MAX_UPGRADE_LEVEL
                ? WeaponStatsCalculator.CalculateAndNormalizeFireRate(weaponData, level + 1)
                : currentFireRateNormalized;
            float nextAmmoNormalized = level < PlayerProgress.MAX_UPGRADE_LEVEL
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
    /// Updates ammo purchase display for the selected weapon.
    /// Shows current reserve ammo and the cost to purchase more.
    /// </summary>
    private void UpdateAmmoDisplay(string weaponID) {
        if (string.IsNullOrEmpty(weaponID) || PlayerProgress.Instance == null) {
            ClearAmmoDisplay();
            return;
        }

        int currentAmmo = PlayerProgress.Instance.GetWeaponReserveAmmo(weaponID);

        WeaponAmmoPricing ammoPricing = GetWeaponAmmoPricing(weaponID);

        if (string.IsNullOrEmpty(ammoPricing.weaponID)) {
            ClearAmmoDisplay();
            return;
        }

        if (currentAmmoText != null) {
            currentAmmoText.text = $"{currentAmmo}/{ammoPricing.maxReserveAmmo}";
        }

        if (currentAmmo >= ammoPricing.maxReserveAmmo) {
            if (ammoPriceText != null) {
                ammoPriceText.text = "FULL AMMO";
            }
            if (ammoButton != null) {
                ammoButton.interactable = false;
            }
        } else {
            if (ammoPriceText != null) {
                ammoPriceText.text = $"${ammoPricing.costPerPurchase:N0}";
            }

            if (ammoButton != null) {
                ammoButton.interactable = EconomyManager.Instance != null &&
                                         EconomyManager.Instance.CanAfford(ammoPricing.costPerPurchase);
            }
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

        bool isUnlocked = PlayerProgress.Instance.IsWeaponUnlocked(itemID);
        int currentLevel = PlayerProgress.Instance.GetWeaponLevel(itemID);
        bool isMaxLevel = currentLevel >= PlayerProgress.MAX_UPGRADE_LEVEL;

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

            if (selectedItemPriceText != null)
                selectedItemPriceText.text = string.Empty;
        } else {
            int cost = CalculateUpgradeCostForItem(itemData, currentLevel);
            if (selectedItemActionButtonText != null)
                selectedItemActionButtonText.text = "Upgrade";

            if (selectedItemPriceText != null)
                selectedItemPriceText.text = $"${cost:N0}";

            selectedItemActionButton.interactable = EconomyManager.Instance.CanAfford(cost);
            selectedItemActionButton.onClick.AddListener(() => OnRightPanelUpgrade(itemData));
        }
    }

    /// <summary>
    /// Handles unlock from the right-panel action button.
    /// </summary>
    private void OnRightPanelUnlock(ShopItemDataSO itemData) {
        if (itemData == null || EconomyManager.Instance == null || PlayerProgress.Instance == null) {
            return;
        }

        if (EconomyManager.Instance.TrySpendCurrency(itemData.UnlockCost)) {
            PlayerProgress.Instance.UnlockItem(itemData);
            Debug.Log($"[ShopUI] Unlocked {itemData.ItemName}!");
            // REFATORAÇÃO: precisamos mesmo ter 2 chamadas diferentes? nao deveriamos ter só um méetodo para desbloquear qualquer item, sendo arma ou medkit, buildable, qualquer um?
            if (itemData.ItemData is WeaponDataSO) {
                WeaponUnlocked?.Invoke(itemData.ItemID);
            }
            RefreshAllCards();
        } else {
            int missingAmount = itemData.UnlockCost - EconomyManager.Instance.GetCurrentCurrency();
            Debug.LogWarning($"[ShopUI] Insufficient funds! Need {missingAmount} more coins.");
        }
    }

    /// <summary>
    /// Handles upgrade from the right-panel action button.
    /// </summary>
    private void OnRightPanelUpgrade(ShopItemDataSO itemData) {
        if (itemData == null || UpgradeManager.Instance == null || PlayerProgress.Instance == null) {
            return;
        }

        int currentLevel = PlayerProgress.Instance.GetWeaponLevel(itemData.ItemID);
        if (UpgradeManager.Instance.TryUpgradeWeapon(itemData.ItemID, itemData.BaseUpgradeCost)) {
            int newLevel = PlayerProgress.Instance.GetWeaponLevel(itemData.ItemID);
            Debug.Log($"[ShopUI] Upgraded {itemData.ItemName} to level {newLevel}!");
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
    /// Finds the ammo pricing configuration for the specified weapon ID.
    /// Returns a default struct if not found (weaponID will be empty string).
    /// </summary>
    private WeaponAmmoPricing GetWeaponAmmoPricing(string weaponID) {
        return weaponAmmoPricings.FirstOrDefault(w => w.weaponID == weaponID);
    }

    /// <summary>
    /// Handles ammo purchase button click.
    /// Validates funds, adds ammo to reserve, and deducts currency.
    /// </summary>
    private void OnAmmoButtonPressed() {

        string itemID = selectedItemData.ItemID;
        WeaponAmmoPricing ammoPricing = GetWeaponAmmoPricing(itemID);

        // Se não encontramos configuração (weaponID está vazio), significa que não há config
        if (string.IsNullOrEmpty(ammoPricing.weaponID)) {
            Debug.LogWarning($"[ShopUI.OnAmmoButtonPressed] No ammo pricing configuration found for weapon: {itemID}");
            return;
        }

        if (!EconomyManager.Instance.CanAfford(ammoPricing.costPerPurchase)) {
            int missingAmount = ammoPricing.costPerPurchase - EconomyManager.Instance.GetCurrentCurrency();
            Debug.LogWarning($"[ShopUI.OnAmmoButtonPressed] Insufficient funds! Need {missingAmount} more coins.");
            // Aqui poderíamos tocar um som de erro ou mostrar uma mensagem visual
            return;
        }

        int currentAmmo = PlayerProgress.Instance.GetWeaponReserveAmmo(itemID);
        int newAmmo = currentAmmo + ammoPricing.ammoPerPurchase;

        newAmmo = Mathf.Clamp(newAmmo, 0, ammoPricing.maxReserveAmmo);

        int actualAmmoAdded = newAmmo - currentAmmo;

        if (actualAmmoAdded <= 0) {
            Debug.LogWarning($"[ShopUI.OnAmmoButtonPressed] Weapon {itemID} already at max ammo ({ammoPricing.maxReserveAmmo})!");
            //mais um som de  erro
            return;
        }

        float ammoProportion = (float)actualAmmoAdded / ammoPricing.ammoPerPurchase;
        int actualCost = Mathf.RoundToInt(ammoPricing.costPerPurchase * ammoProportion);

        if (EconomyManager.Instance.TrySpendCurrency(actualCost)) {
            PlayerProgress.Instance.AddWeaponReserveAmmo(itemID, actualAmmoAdded);

            Debug.Log($"[ShopUI.OnAmmoButtonPressed] Purchased {actualAmmoAdded} ammo for {itemID}. Cost: ${actualCost}. New total: {newAmmo}");
            AmmoPurchased?.Invoke(itemID, actualAmmoAdded);

            UpdateSelectedItemInfo();
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

    /// <summary>
    /// Recreates the 3D preview model for the currently selected shop item.
    /// </summary>
    private void RebuildPreviewModel() {
        DestroyPreviewModel();

        activePreviewModel = Instantiate(selectedItemData.PreviewPrefab, previewAnchor);

        activePreviewModel.transform.localPosition = Vector3.zero;

        activePreviewModel.transform.localRotation = Quaternion.identity;

        Vector3 scaledSize = Vector3.one * previewModelScale;
        activePreviewModel.transform.localScale = scaledSize;

        AssignWeaponLayer(activePreviewModel);

        AdjustCameraZPosition();
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

        Camera previewCamera = GetWeaponPreviewCamera();
        if (previewCamera != null) {
            Vector3 newPosition = previewCamera.transform.position;
            newPosition.z = originalCameraZ;
            previewCamera.transform.position = newPosition;
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
    /// Helper method to find the weapon preview camera in the hierarchy.
    /// Returns the first Camera component found as a child of previewAnchor or null.
    /// </summary>
    private Camera GetWeaponPreviewCamera() {
        if (previewAnchor == null) return null;

        Transform parent = previewAnchor.parent;
        if (parent != null) {
            Camera cam = parent.GetComponentInChildren<Camera>();
            if (cam != null) return cam;
        }

        return null;
    }

    /// <summary>
    /// Dynamically adjusts the preview camera Z position based on the selected weapon.
    /// This prevents smaller weapons from appearing too close and larger weapons from being cut off.
    /// </summary>
    private void AdjustCameraZPosition() {
        if (selectedItemData == null) return;

        Camera previewCamera = GetWeaponPreviewCamera();
        if (previewCamera == null) return;

        string weaponID = selectedItemData.ItemID;
        WeaponCameraZPosition foundConfig = cameraZPositions.FirstOrDefault(c => c.weaponID == weaponID);

        if (!string.IsNullOrEmpty(foundConfig.weaponID)) {
            Vector3 newPosition = previewCamera.transform.position;
            newPosition.z = foundConfig.cameraZPosition;
            previewCamera.transform.position = newPosition;

        }
    }

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
/// Serializable struct to store custom camera Z position for specific weapons.
/// This allows designers to adjust camera depth per weapon type for optimal framing.
/// PRECISA SER REFATORADO
/// </summary>
[System.Serializable]
public struct WeaponCameraZPosition {

    [Tooltip("The unique identifier for the weapon (e.g., 'PISTOL_01', 'SMG', 'SHOTGUN').")]
    [SerializeField]
    public string weaponID;

    [Tooltip("Camera Z position for this weapon (can be negative). Negative = camera farther away, Positive = camera closer.")]
    [SerializeField]
    public float cameraZPosition;
}

/// <summary>
/// Serializable struct to configure ammo purchase pricing and limits for each weapon.
/// This defines how much ammo is added per purchase, the cost, and maximum reserve ammo.
/// precisa ser refatorado!!
/// </summary>
[System.Serializable]
public struct WeaponAmmoPricing {

    [Tooltip("The unique identifier for the weapon (e.g., 'Pistol', 'SMG', 'Shotgun').")]
    [SerializeField]
    public string weaponID;

    [Tooltip("Amount of ammo added per purchase.")]
    [SerializeField]
    public int ammoPerPurchase;

    [Tooltip("Cost in currency per ammo purchase.")]
    [SerializeField]
    public int costPerPurchase;

    [Tooltip("Maximum reserve ammo this weapon can hold.")]
    [SerializeField]
    public int maxReserveAmmo;
}
