using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Manages the shop UI including item cards and shop panel interactions.
/// </summary>
public class ShopUI : BaseUI {
    /// <summary>
    /// Raised after a weapon is successfully unlocked in the shop.
    /// </summary>
    public static event Action<string> WeaponUnlocked;

    /// <summary>
    /// Raised after reserve ammo is successfully purchased in the shop.
    /// </summary>
    public static event Action<string, int> AmmoPurchased;

    [Header("Shop Elements")]
    [SerializeField] private RectTransform itemsContainer;
    [SerializeField] private ShopItemCard shopItemCardPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private List<ShopItemData> shopItems = new List<ShopItemData>();

    [Header("Grid Layout")]
    [Tooltip("Forces the items grid to occupy only the left half of the shop panel.")]
    [SerializeField] private bool forceLeftHalfLayout = true;
    [Tooltip("Number of columns shown in the shop items grid.")]
    [SerializeField] private int gridColumns = 3;
    [Tooltip("Number of visible rows shown in the shop items grid.")]
    [SerializeField] private int gridRows = 4;
    [Tooltip("Padding applied inside the shop items grid.")]
    [SerializeField] private Vector2 gridPadding = new Vector2(16f, 16f);
    [Tooltip("Spacing between cards in the grid.")]
    [SerializeField] private Vector2 gridSpacing = new Vector2(12f, 12f);
    [Tooltip("Offset from the parent edges when forcing the left-half layout.")]
    [SerializeField] private Vector2 leftHalfOffsetMin = new Vector2(16f, 16f);
    [Tooltip("Negative values add inner margins from top/right when forcing left-half layout.")]
    [SerializeField] private Vector2 leftHalfOffsetMax = new Vector2(-16f, -16f);

    [Header("Right Panel - Selected Item")]
    // CONCEITO: Transform (Posição, Rotação e Escala)
    // Um Transform é um componente que define onde um GameObject está (posição),
    // para onde está virado (rotação) e seu tamanho (escala) no mundo 3D.
    // O "previewAnchor" é o Transform onde vamos criar a arma do preview.
    // Ele é como um "ponto de encontro" - qualquer objeto que criamos dentro dele
    // fica automaticamente alinhado com sua posição.
    [SerializeField] private Transform previewAnchor;
    
    // CONCEITO: Float (Número com Decimais)
    // Um float é um número que pode ter casa decimal (ex: 35.5, 0.1, 100.0)
    // previewRotationSpeed controla QUANTOS GRAUS por segundo a arma gira.
    // Quanto maior o número, mais rápido ela gira.
    [SerializeField] private float previewRotationSpeed = 35f;
    
    // CONCEITO: Escala e Proporção no Canvas
    // Canvases em Unity são gigantes comparados a objetos 3D normais.
    // Uma arma com scale 1 fica invisível dentro do Canvas!
    // previewModelScale é um multiplicador - se colocar 100, a arma fica 100x maior.
    // Você ajusta isso no Inspector até a arma ficar visível no preview.
    [SerializeField] private float previewModelScale = 100f;
    [Tooltip("Scale multiplier for the 3D preview model. Increase if model appears too small in the RawImage preview.")]
    
    [Header("Camera Adjustment")]
    // CONCEITO: Ajuste Manual de Câmera por Arma
    // Diferentes armas têm tamanhos diferentes. Para que a câmera enquadre bem cada uma,
    // você pode customizar a posição Z da câmera por arma.
    // Exemplo: Pistola (pequena) pode ficar mais perto, Shotgun (grande) mais longe.
    [SerializeField] private List<WeaponCameraZPosition> cameraZPositions = new List<WeaponCameraZPosition>();
    [Tooltip("Custom camera Z position for each weapon. Adjust manually in editor for each weapon.")]
    
    // CONCEITO: Armazenar Z Original
    // Guardamos o Z original da câmera para restaurar após destruir o preview
    private float originalCameraZ;
    [SerializeField] private TextMeshProUGUI selectedItemNameText;
    [SerializeField] private TextMeshProUGUI selectedItemDescriptionText;
    [SerializeField] private StatBlockDisplay damageBlockDisplay;
    [SerializeField] private StatBlockDisplay fireRateBlockDisplay;
    [SerializeField] private StatBlockDisplay ammoBlockDisplay;
    [SerializeField] private TextMeshProUGUI selectedItemPriceText;
    [SerializeField] private Button selectedItemActionButton;
    [SerializeField] private TextMeshProUGUI selectedItemActionButtonText;

    [Header("Ammo Purchase")]
    [Tooltip("Button to purchase additional ammo for the selected weapon.")]
    [SerializeField] private Button ammoButton;
    [Tooltip("Text displaying the ammo purchase price.")]
    [SerializeField] private TextMeshProUGUI ammoPriceText;
    [Tooltip("Text displaying current reserve ammo for the selected weapon.")]
    [SerializeField] private TextMeshProUGUI currentAmmoText;
    [Tooltip("Configuration for weapon ammo pricing and limits.")]
    [SerializeField] private List<WeaponAmmoPricing> weaponAmmoPricings = new List<WeaponAmmoPricing>();

    [Header("Currency Display")]
    [Tooltip("Text component to display player's current currency in the shop.")]
    [SerializeField] private TextMeshProUGUI currencyText;

    private GridLayoutGroup itemsGridLayout;
    private ShopItemData selectedItemData;
    
    // CONCEITO: GameObject (Objeto 3D)
    // Um GameObject é como uma "marionete" 3D dentro da cena.
    // Pode ser uma arma, um inimigo, um efeito visual, qualquer coisa.
    // activePreviewModel armazena a arma que o jogador está vendo no preview.
    // Inicialmente é null (vazio). Quando selecionamos uma arma, criamos um novo GameObject aqui.
    // Quando selecionamos outra arma, destruímos o antigo e criamos um novo.
    private GameObject activePreviewModel;

    /// <summary>
    /// Initialize the shop UI when the GameObject first loads.
    /// This method runs before Start() and is called only once in the object's lifetime.
    /// We use Awake() instead of Start() because we need to set up layout BEFORE any visual updates happen.
    /// </summary>
    protected override void Awake() {
        base.Awake();
        // Cache (store/save references to) the GridLayoutGroup component from the itemsContainer
        // We do this early so we don't have to search for it multiple times later
        CacheLayoutReferences();
        
        // Configure the grid layout now so the editor values are set before the shop is shown
        // This happens in Awake so anchors and constraints are ready when the Canvas renders
        ConfigureItemGridLayout();
        
        // Link button click events to their corresponding handler methods
        // This is how Unity knows what to do when a button is pressed
        BindButtons();
        
        // Register this UI to receive currency change notifications
        // This is the Observer Pattern - we "subscribe" to currency events
        SubscribeToCurrencyEvents();
        
        // CONCEITO: Caching de Valor Original
        // Guardamos a posição Z original da câmera para restaurar depois
        Camera previewCamera = GetWeaponPreviewCamera();
        if (previewCamera != null) {
            originalCameraZ = previewCamera.transform.position.z;
            Debug.Log($"[ShopUI.Awake] Stored original camera Z: {originalCameraZ}");
        }
    }

    /// <summary>
    /// Keeps the selected 3D preview rotating while the shop is visible.
    /// Update() is called once per frame, but we follow best practices by only calling
    /// our rotation logic here - the actual rotation work is in RotatePreviewModel().
    /// This keeps Update() clean and readable.
    /// </summary>
    protected override void Update() {
        // Call parent Update for base functionality
        base.Update();
        
        // Continuously rotate the 3D weapon preview so the player can see all angles
        // We do this every frame for smooth rotation animation
        RotatePreviewModel();
    }

    /// <summary>
    /// Binds button click events to their handlers.
    /// </summary>
    private void BindButtons() {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClick);
        
        // CONCEITO: Null Checking (Validação)
        // Verificamos se ammoButton não é null antes de usá-lo
        // Se for null, significa que o designer não atribuiu no Inspector
        // Nesse caso, simplesmente pulamos (o botão não terá funcionalidade)
        if (ammoButton != null)
            ammoButton.onClick.AddListener(OnAmmoButtonPressed);
    }

    /// <summary>
    /// Shows the shop panel and prepares it for interaction.
    /// This is called whenever the player opens the shop.
    /// We populate items, select the first weapon, and update currency display here.
    /// </summary>
    public override void Show() {
        // Call the parent Show() method to handle base UI visibility (makes panel active and visible)
        base.Show();
        
        // Fill the shop grid with item cards by instantiating prefabs and binding callbacks
        // This creates all the visual buttons the player will click
        PopulateShopItems();
        
        // Automatically select the first weapon so the right panel shows something when shop opens
        // Better UX than an empty right side
        SelectInitialItem();
        
        // Update the currency text display to show how much money the player currently has
        UpdateCurrencyDisplay();
    }

    /// <summary>
    /// Hides the shop panel and cleans up temporary preview objects.
    /// This is called when the player closes the shop or switches to a different UI.
    /// We destroy the 3D weapon preview here to free up memory.
    /// </summary>
    public override void Hide() {
        // Call parent Hide() to handle base UI visibility (deactivates panel)
        base.Hide();
        
        // Remove the currently displayed 3D weapon preview from the scene
        // This frees memory since we don't need to render it anymore
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
    /// Called when the component is destroyed.
    /// Unsubscribes from events to prevent memory leaks.
    /// </summary>
    private void OnDestroy() {
        if (EconomyManager.Instance != null) {
            EconomyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
        }

        DestroyPreviewModel();
    }

    /// <summary>
    /// Event handler for currency changes.
    /// Updates the currency display text and refreshes button interactable state.
    /// 
    /// CONCEITO: Event Callback Pattern
    /// Quando a moeda muda, atualizamos não só o display de moeda,
    /// mas também o estado do botão (enabled/disabled) baseado na disponibilidade de fundos
    /// </summary>
    /// <param name="newAmount">The new currency amount.</param>
    private void OnCurrencyChanged(int newAmount) {
        UpdateCurrencyDisplay();
        
        // CONCEITO: Atualizar Estado do Botão
        // Quando a moeda muda, o botão pode ficar habilitado ou desabilitado
        // Por exemplo: jogador tinha $50, não podia fazer upgrade por $100
        // Então ganha moedas e agora tem $150, pode fazer upgrade!
        // Precisamos refrescar o estado do botão neste momento
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
        // Validate that we have the required references (itemsContainer and the prefab)
        // If either is missing, we can't create items
        if (itemsContainer == null || shopItemCardPrefab == null)
        {
            Debug.LogWarning($"{nameof(ShopUI)} has missing references for items container or card prefab.", this);
            return;
        }

        // First, remove all old item cards from the grid
        // This ensures we don't have duplicates if PopulateShopItems is called multiple times
        ClearShopItems();
        
        // Now create new card instances from our configured list
        CreateConfiguredItems();
    }

    /// <summary>
    /// Creates card instances from the configured inspector data.
    /// TEACHING NOTE: This uses Instantiate() to clone the prefab.
    /// A prefab is a pre-made template that we copy multiple times.
    /// Setting the parent creates a proper hierarchy and applies the parent's scale/position.
    /// </summary>
    private void CreateConfiguredItems() {
        // Check if we have items to create
        if (shopItems == null || shopItems.Count == 0)
        {
            Debug.LogWarning($"{nameof(ShopUI)} has no configured shop items.", this);
            return;
        }

        // Loop through each item in our list
        for (int index = 0; index < shopItems.Count; index++)
        {
            ShopItemData itemData = shopItems[index];
            
            // Skip null entries (empty slots)
            if (itemData == null)
            {
                Debug.LogWarning($"{nameof(ShopUI)} has a null item entry at index {index}.", this);
                continue;
            }

            // TEACHING: Instantiate creates a copy of the prefab
            // The second parameter (itemsContainer) sets this copy as a child of itemsContainer
            // This makes the card appear inside the grid
            ShopItemCard card = Instantiate(shopItemCardPrefab, itemsContainer);
            
            // Tell the card which methods to call when events happen
            // These are "callbacks" - functions we pass to another object so it can call us back
            card.SetCallbacks(HandleCardSelected, HandleCardStateChanged, HandleCardUnlockUpgrade);
            
            // Give the card its data (name, description, price, etc.)
            card.Setup(itemData);
        }
    }

    /// <summary>
    /// Clears all existing shop item cards from the container.
    /// TEACHING NOTE: This demonstrates cleanup - we must destroy old objects before creating new ones.
    /// If we don't, they'll stack up and waste memory.
    /// </summary>
    private void ClearShopItems() {
        // Reset the selected item tracker since we're removing all cards
        selectedItemData = null;
        
        // Loop through all child GameObjects of itemsContainer
        // Each child is a card from the previous population
        foreach (Transform child in itemsContainer) {
            // TEACHING: Destroy() removes a GameObject and frees its memory
            // It doesn't happen instantly - Unity queues it for cleanup at frame end
            // This is safer than immediate deletion
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Caches (stores references to) UI layout components needed to configure the shop grid at runtime.
    /// TEACHING NOTE: Caching means storing a reference to a component when we find it,
    /// instead of searching for it every time we need it. This is more efficient.
    /// </summary>
    private void CacheLayoutReferences() {
        // Find the GridLayoutGroup component on the itemsContainer
        // GetComponent searches the GameObject for a component of the specified type
        // We store it in itemsGridLayout so we can modify it later
        if (itemsContainer != null) {
            itemsGridLayout = itemsContainer.GetComponent<GridLayoutGroup>();
        }
    }

    /// <summary>
    /// Configures a fixed 3x4-style grid in the left half of the shop panel.
    /// TEACHING NOTE: This demonstrates several key UI concepts:
    /// 1. RectTransform manipulation - controlling where UI elements appear on screen
    /// 2. Anchors and Offsets - how Unity positions elements relative to their parent
    /// 3. GridLayoutGroup - automatic layout system that arranges children in grids
    /// All sizing (cell size, padding, spacing) should be configured in the Editor.
    /// </summary>
    private void ConfigureItemGridLayout() {
        Debug.Log($"[ShopUI.ConfigureItemGridLayout] Starting configuration");
        
        // Check if itemsContainer exists - if not, we can't configure anything
        // This is defensive programming: always validate your inputs
        if (itemsContainer == null) {
            Debug.LogError("[ShopUI.ConfigureItemGridLayout] itemsContainer is NULL!");
            return;
        }

        // Check if the GridLayoutGroup component exists on itemsContainer
        // Without this component, the grid layout won't work
        if (itemsGridLayout == null) {
            Debug.LogError("[ShopUI.ConfigureItemGridLayout] itemsGridLayout is NULL!");
            return;
        }

        Debug.Log($"[ShopUI.ConfigureItemGridLayout] Before layout changes - Rect: {itemsContainer.rect}");
        Debug.Log($"[ShopUI.ConfigureItemGridLayout] GridLayoutGroup settings BEFORE - Padding: {itemsGridLayout.padding}, Spacing: {itemsGridLayout.spacing}, CellSize: {itemsGridLayout.cellSize}");

        // TEACHING: Anchors define WHERE on the parent the element can expand to
        // TEACHING: Offsets define MARGINS from those anchor points
        // Force this RectTransform to cover only the left half, so the right half stays free for preview and stats.
        if (forceLeftHalfLayout) {
            Debug.Log($"[ShopUI.ConfigureItemGridLayout] Setting left-half layout");
            Debug.Log($"[ShopUI.ConfigureItemGridLayout] leftHalfOffsetMin: {leftHalfOffsetMin}, leftHalfOffsetMax: {leftHalfOffsetMax}");
            
            // Anchor to bottom-left (0, 0) and extend to middle-right (0.5, 1)
            // This means: start at screen bottom-left, go to halfway across horizontally and full height vertically
            itemsContainer.anchorMin = new Vector2(0f, 0f);
            itemsContainer.anchorMax = new Vector2(0.5f, 1f);
            
            // Pivot point is where the element rotates around - we set it to left-center
            itemsContainer.pivot = new Vector2(0f, 0.5f);
            
            // offsetMin is the margin from bottom-left, offsetMax is the margin from top-right
            // Negative offsetMax values create inner margins
            itemsContainer.offsetMin = leftHalfOffsetMin;
            itemsContainer.offsetMax = leftHalfOffsetMax;
            
            // Ensure the element is at the exact position we calculated with anchors/offsets
            itemsContainer.anchoredPosition = Vector2.zero;
            
            Debug.Log($"[ShopUI.ConfigureItemGridLayout] After anchor/offset changes - Rect: {itemsContainer.rect}");
        }

        // TEACHING: GridLayoutGroup.Constraint.FixedColumnCount means "always show this many columns"
        // TEACHING: The rows will automatically expand as we add more items
        // Only configure the grid layout constraint and alignment.
        // All other settings (cell size, spacing, padding) are configured in the Editor.
        Debug.Log($"[ShopUI.ConfigureItemGridLayout] Setting constraint to FixedColumnCount with {gridColumns} columns");
        
        // Set constraint to Fixed Column Count - this locks the number of columns
        itemsGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        
        // How many columns should show (3 means 3 items per row)
        itemsGridLayout.constraintCount = Mathf.Max(1, gridColumns);
        
        // Align all items to the upper-left corner of the grid
        itemsGridLayout.childAlignment = TextAnchor.UpperLeft;
        
        // Where the grid starts filling (upper-left = top-left corner)
        itemsGridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        
        // Direction to fill: Horizontal = left-to-right, then wrap to next row
        itemsGridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;

        Debug.Log($"[ShopUI.ConfigureItemGridLayout] GridLayoutGroup settings AFTER - Padding: {itemsGridLayout.padding}, Spacing: {itemsGridLayout.spacing}, CellSize: {itemsGridLayout.cellSize}");
    }

    /// <summary>
    /// Selects the first configured weapon so the right panel is populated when the shop opens.
    /// </summary>
    private void SelectInitialItem() {
        ShopItemData fallbackItem = null;

        for (int index = 0; index < shopItems.Count; index++) {
            ShopItemData itemData = shopItems[index];
            if (itemData == null) {
                continue;
            }

            if (fallbackItem == null) {
                fallbackItem = itemData;
            }

            if (itemData.IsWeapon) {
                HandleCardSelected(itemData);
                return;
            }
        }

        HandleCardSelected(fallbackItem);
    }

    /// <summary>
    /// Updates the right-side selected item panel when a card is clicked.
    /// 
    /// CONCEITO: Callback/Event Pattern
    /// Quando um card é clicado, ele chama essa função
    /// Aqui atualizamos:
    /// 1. selectedItemData = qual item o jogador selecionou
    /// 2. Textos na UI (nome, descrição, preço)
    /// 3. Barras de stats (dano, cadência, munição)
    /// 4. Modelo 3D da arma (instancia e posiciona)
    /// </summary>
    /// <param name="itemData">Data from the clicked card.</param>
    private void HandleCardSelected(ShopItemData itemData) {
        // Armazenar qual item foi selecionado
        // Isso é importante porque precisamos desse dado em vários lugares
        selectedItemData = itemData;
        
        // Atualizar as informações do painel direito
        // UpdateSelectedItemInfo() cuida de:
        // - Textos (nome, descrição, preço)
        // - Barras de stats normalizadas (0-5)
        // - Callbacks do botão de ação (comprar/fazer upgrade)
        UpdateSelectedItemInfo();
        
        // Instanciar e posicionar o modelo 3D da arma
        // RebuildPreviewModel() vai:
        // - Destruir o modelo anterior (se houver)
        // - Instanciar o novo prefab
        // - Posicionar sob previewAnchor
        RebuildPreviewModel();
    }

    /// <summary>
    /// Keeps right-side stats synchronized after purchases and upgrades on the selected card.
    /// </summary>
    /// <param name="itemData">Data from the card that changed state.</param>
    private void HandleCardStateChanged(ShopItemData itemData) {
        if (selectedItemData == null || itemData == null || itemData != selectedItemData) {
            return;
        }

        UpdateSelectedItemInfo();
    }

    /// <summary>
    /// Handles unlock/upgrade button click from the card, delegating logic to right-panel action button.
    /// </summary>
    private void HandleCardUnlockUpgrade(ShopItemData itemData) {
        selectedItemData = itemData;
        UpdateSelectedItemInfo();
        if (selectedItemActionButton != null) {
            selectedItemActionButton.onClick.Invoke();
        }
    }

    /// <summary>
    /// Writes the selected item's stats to the right-side panel fields using stat blocks.
    /// PEDAGÓGICO: Agora usamos WeaponStatsCalculator para normalizar stats para 5 barras
    /// Isso permite que o UI mostre uma representação visual proporcional dos stats
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

        SetSelectedInfoTexts(itemName, description);
        
        // Update stat blocks only for weapons
        if (selectedItemData.IsWeapon && selectedItemData.WeaponData != null) {
            int level = 1;
            if (PlayerProgress.Instance != null) {
                level = Mathf.Max(1, PlayerProgress.Instance.GetWeaponLevel(selectedItemData.ItemID));
            }

            WeaponDataSO weaponData = selectedItemData.WeaponData;
            
            // CONCEITO: Normalização de Stats
            // Em vez de mostrar valores brutos (100 damage, 200 RPM), 
            // convertemos para escala 0-5 barras usando WeaponStatsCalculator
            // Isso torna a interface mais legível e intuitiva
            
            // Current level stats (normalized to 0-5 bars)
            float currentDamageNormalized = WeaponStatsCalculator.CalculateAndNormalizeDamage(weaponData, level);
            float currentFireRateNormalized = WeaponStatsCalculator.CalculateAndNormalizeFireRate(weaponData, level);
            float currentAmmoNormalized = WeaponStatsCalculator.CalculateAndNormalizeAmmo(weaponData, level);

            // Get upgrade preview values (next level, normalized)
            float nextDamageNormalized = level < PlayerProgress.MAX_UPGRADE_LEVEL 
                ? WeaponStatsCalculator.CalculateAndNormalizeDamage(weaponData, level + 1) 
                : currentDamageNormalized;
            float nextFireRateNormalized = level < PlayerProgress.MAX_UPGRADE_LEVEL 
                ? WeaponStatsCalculator.CalculateAndNormalizeFireRate(weaponData, level + 1) 
                : currentFireRateNormalized;
            float nextAmmoNormalized = level < PlayerProgress.MAX_UPGRADE_LEVEL 
                ? WeaponStatsCalculator.CalculateAndNormalizeAmmo(weaponData, level + 1) 
                : currentAmmoNormalized;

            // Display stat blocks with upgrade preview
            // CONCEITO: StatBlockDisplay espera valores normalizados (0-5)
            // Agora passamos valores já normalizados ao invés de valores brutos
            if (damageBlockDisplay != null) {
                // Max stat value = 5 (normalized range)
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
            
            // CONCEITO: Atualizar Display de Munição
            // Mostra a munição reserva atual do jogador para esta arma
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
    /// 
    /// CONCEITO: Atualização Condicional de UI
    /// Cada elemento é atualizado individualmente com null checks
    /// Isso permite que o designer deixe alguns elementos vazios no Inspector
    /// </summary>
    private void UpdateAmmoDisplay(string weaponID) {
        // CONCEITO: Guard Clause (Validação Rápida)
        if (string.IsNullOrEmpty(weaponID) || PlayerProgress.Instance == null) {
            ClearAmmoDisplay();
            return;
        }

        // CONCEITO: Buscar Munição Reserva Atual
        // GetWeaponReserveAmmo retorna quantas balas o jogador tem dessa arma
        int currentAmmo = PlayerProgress.Instance.GetWeaponReserveAmmo(weaponID);
        
        // CONCEITO: Buscar Configuração de Preço
        // GetWeaponAmmoPricing encontra as informações de preço/quantidade desta arma
        WeaponAmmoPricing ammoPricing = GetWeaponAmmoPricing(weaponID);
        
        // Se não há configuração, não podemos mostrar preço
        if (string.IsNullOrEmpty(ammoPricing.weaponID)) {
            ClearAmmoDisplay();
            return;
        }

        // CONCEITO: Atualizar Texto de Munição Atual
        // Mostra "Current: 45/100" por exemplo
        if (currentAmmoText != null) {
            currentAmmoText.text = $"Current: {currentAmmo}/{ammoPricing.maxReserveAmmo}";
        }

        // CONCEITO: Checar se Está no Máximo
        // Se a munição atual já é igual ao máximo, não há razão para mostrar preço de compra
        if (currentAmmo >= ammoPricing.maxReserveAmmo) {
            // Munição em seu máximo
            if (ammoPriceText != null) {
                ammoPriceText.text = "MAX AMMO";
            }
            if (ammoButton != null) {
                ammoButton.interactable = false;
            }
        } else {
            // CONCEITO: Atualizar Preço de Compra
            // Mostra o preço para adicionar mais munição
            if (ammoPriceText != null) {
                ammoPriceText.text = $"${ammoPricing.costPerPurchase:N0}";
            }
            
            // CONCEITO: Habilitação Condicional do Botão
            // O botão só é clicável se o jogador tiver dinheiro suficiente
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
    private void UpdateActionButton(ShopItemData itemData, int dummy) {
        if (selectedItemActionButton == null || itemData == null) {
            return;
        }

        // CONCEITO: RemoveAllListeners (Limpeza de Callbacks)
        // Cada vez que atualizamos o botão, removemos os listeners antigos
        // Isso evita que múltiplas funções sejam chamadas ao clicar
        // (sem isso, um botão clicado 5 vezes teria 5 listeners!)
        selectedItemActionButton.onClick.RemoveAllListeners();

        if (PlayerProgress.Instance == null || EconomyManager.Instance == null) {
            selectedItemActionButton.interactable = false;
            return;
        }

        string itemID = itemData.ItemID;
        
        // CONCEITO: Booleanos (True/False)
        // isUnlocked é true se a arma foi desbloqueada
        // isMaxLevel é true se chegou no nível máximo (10)
        bool isUnlocked = PlayerProgress.Instance.IsWeaponUnlocked(itemID);
        int currentLevel = PlayerProgress.Instance.GetWeaponLevel(itemID);
        bool isMaxLevel = currentLevel >= PlayerProgress.MAX_UPGRADE_LEVEL;

        if (!isUnlocked) {
            // UNLOCK button - arma está bloqueada
            int cost = itemData.UnlockCost;
            if (selectedItemActionButtonText != null) 
                selectedItemActionButtonText.text = "UNLOCK";
            
            // CONCEITO: Exibir Preço
            // Mostramos o preço de desbloqueio no elemento de preço separado
            if (selectedItemPriceText != null)
                selectedItemPriceText.text = $"${cost:N0}";
            
            // CONCEITO: Habilitação Condicional de Botão
            // interactable = false desabilita o botão (fica cinza e não responde)
            // interactable = true o habilita (fica verde e clicável)
            selectedItemActionButton.interactable = EconomyManager.Instance.CanAfford(cost);
            selectedItemActionButton.onClick.AddListener(() => OnRightPanelUnlock(itemData));
        } else if (isMaxLevel) {
            // MAX LEVEL - arma chegou no nível máximo, não há mais upgrades
            // CONCEITO: "MAXED OUT" feedback visual
            // Mostramos "MAXED OUT" sem preço para indicar que não há mais ações disponíveis
            if (selectedItemActionButtonText != null) 
                selectedItemActionButtonText.text = "MAXED OUT";
            
            // Desabilita o botão porque não há mais ação disponível
            selectedItemActionButton.interactable = false;
            
            // CONCEITO: Ocultar Preço (Limpar Texto)
            // Quando está maxed out, não há preço a mostrar
            // Atribuir empty string ("") faz o texto desaparecer
            if (selectedItemPriceText != null)
                selectedItemPriceText.text = string.Empty;
        } else {
            // UPGRADE button - arma desbloqueada e pode fazer upgrade
            int cost = CalculateUpgradeCostForItem(itemData, currentLevel);
            if (selectedItemActionButtonText != null) 
                selectedItemActionButtonText.text = "UPGRADE";
            
            // CONCEITO: Exibir Preço
            // Mostramos o preço de upgrade no elemento de preço separado
            if (selectedItemPriceText != null)
                selectedItemPriceText.text = $"${cost:N0}";
            
            selectedItemActionButton.interactable = EconomyManager.Instance.CanAfford(cost);
            selectedItemActionButton.onClick.AddListener(() => OnRightPanelUpgrade(itemData));
        }
    }

    /// <summary>
    /// Handles unlock from the right-panel action button.
    /// </summary>
    private void OnRightPanelUnlock(ShopItemData itemData) {
        if (itemData == null || EconomyManager.Instance == null || PlayerProgress.Instance == null) {
            return;
        }

        if (EconomyManager.Instance.TrySpendCurrency(itemData.UnlockCost)) {
            PlayerProgress.Instance.UnlockWeapon(itemData.ItemID);
            Debug.Log($"[ShopUI] Unlocked {itemData.ItemName}!");
            WeaponUnlocked?.Invoke(itemData.ItemID);
            RefreshAllCards();
        } else {
            int missingAmount = itemData.UnlockCost - EconomyManager.Instance.GetCurrentCurrency();
            Debug.LogWarning($"[ShopUI] Insufficient funds! Need {missingAmount} more coins.");
        }
    }

    /// <summary>
    /// Handles upgrade from the right-panel action button.
    /// </summary>
    private void OnRightPanelUpgrade(ShopItemData itemData) {
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
    private int CalculateUpgradeCostForItem(ShopItemData itemData, int currentLevel) {
        return UpgradeManager.Instance != null ? UpgradeManager.Instance.GetNextUpgradeCost(itemData.ItemID, itemData.BaseUpgradeCost) : 0;
    }

    /// <summary>
    /// Finds the ammo pricing configuration for the specified weapon ID.
    /// Returns a default struct if not found (weaponID will be empty string).
    /// 
    /// CONCEITO: LINQ FirstOrDefault
    /// FirstOrDefault procura o primeiro item da lista que atende à condição
    /// Se encontrar, retorna esse item
    /// Se não encontrar, retorna um valor padrão (struct vazio com string vazio)
    /// </summary>
    private WeaponAmmoPricing GetWeaponAmmoPricing(string weaponID) {
        // CONCEITO: Busca em Lista (Linear Search)
        // FirstOrDefault percorre a lista até encontrar um item com weaponID correspondente
        // Isso é simples e funciona bem para listas pequenas (como nossa lista de armas)
        return weaponAmmoPricings.FirstOrDefault(w => w.weaponID == weaponID);
    }

    /// <summary>
    /// Handles ammo purchase button click.
    /// Validates funds, adds ammo to reserve, and deducts currency.
    /// </summary>
    private void OnAmmoButtonPressed() {
        // CONCEITO: Guard Clauses (Validações Rápidas)
        // No início da função, checamos todas as pré-condições
        // Se qualquer uma falhar, retornamos cedo evitando lógica desnecessária
        
        if (selectedItemData == null) {
            Debug.LogWarning("[ShopUI.OnAmmoButtonPressed] No weapon selected!");
            return;
        }

        if (PlayerProgress.Instance == null || EconomyManager.Instance == null) {
            Debug.LogError("[ShopUI.OnAmmoButtonPressed] PlayerProgress or EconomyManager is null!");
            return;
        }

        // CONCEITO: Encontrar Configuração
        // Buscamos as informações de preço/quantidade desta arma
        string itemID = selectedItemData.ItemID;
        WeaponAmmoPricing ammoPricing = GetWeaponAmmoPricing(itemID);
        
        // Se não encontramos configuração (weaponID está vazio), significa que não há config
        if (string.IsNullOrEmpty(ammoPricing.weaponID)) {
            Debug.LogWarning($"[ShopUI.OnAmmoButtonPressed] No ammo pricing configuration found for weapon: {itemID}");
            return;
        }

        // CONCEITO: Verificação de Fundos
        // Antes de gastar, verificamos se o jogador tem dinheiro suficiente
        if (!EconomyManager.Instance.CanAfford(ammoPricing.costPerPurchase)) {
            int missingAmount = ammoPricing.costPerPurchase - EconomyManager.Instance.GetCurrentCurrency();
            Debug.LogWarning($"[ShopUI.OnAmmoButtonPressed] Insufficient funds! Need {missingAmount} more coins.");
            // Aqui poderíamos tocar um som de erro ou mostrar uma mensagem visual
            return;
        }

        // CONCEITO: Obter Munição Atual
        // Consultamos quanto de munição reserva o jogador já tem dessa arma
        int currentAmmo = PlayerProgress.Instance.GetWeaponReserveAmmo(itemID);
        int newAmmo = currentAmmo + ammoPricing.ammoPerPurchase;
        
        // CONCEITO: Validação de Limite (Clamping)
        // Clamp garante que newAmmo não ultrapasse o máximo permitido
        // Se newAmmo = 950 e max = 500, Clamp(950, 0, 500) retorna 500
        newAmmo = Mathf.Clamp(newAmmo, 0, ammoPricing.maxReserveAmmo);
        
        // CONCEITO: Calcular Quantidade Real Adicionada
        // Se havia 490 e o máx é 500, só adicionamos 10 (não os 60 pedidos)
        int actualAmmoAdded = newAmmo - currentAmmo;
        
        // Se já está no máximo, não há nada a fazer
        if (actualAmmoAdded <= 0) {
            Debug.LogWarning($"[ShopUI.OnAmmoButtonPressed] Weapon {itemID} already at max ammo ({ammoPricing.maxReserveAmmo})!");
            return;
        }

        // CONCEITO: Calcular Preço Proporcional
        // Se o jogador só pode adicionar 10 ammo mas compraria 60, cobra menos
        // (10 / 60) * 300 = 50 moedas ao invés de 300
        float ammoProportion = (float)actualAmmoAdded / ammoPricing.ammoPerPurchase;
        int actualCost = Mathf.RoundToInt(ammoPricing.costPerPurchase * ammoProportion);
        
        // CONCEITO: Transação de Moeda
        // Tenta descontar a moeda
        if (EconomyManager.Instance.TrySpendCurrency(actualCost)) {
            // Se bem-sucedido, adiciona a munição
            PlayerProgress.Instance.AddWeaponReserveAmmo(itemID, actualAmmoAdded);
            
            Debug.Log($"[ShopUI.OnAmmoButtonPressed] Purchased {actualAmmoAdded} ammo for {itemID}. Cost: ${actualCost}. New total: {newAmmo}");
            AmmoPurchased?.Invoke(itemID, actualAmmoAdded);
            
            // Atualiza o display para refletir a nova munição
            UpdateSelectedItemInfo();
        } else {
            Debug.LogWarning($"[ShopUI.OnAmmoButtonPressed] Failed to spend currency!");
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
    /// 
    /// CONCEITO: Instanciação de Prefabs
    /// Um prefab é um template reutilizável. Aqui criamos uma cópia do modelo 3D da arma
    /// e a posicionamos sob o previewAnchor (Transform vazio que define onde a arma vai aparecer)
    /// </summary>
    private void RebuildPreviewModel() {
        DestroyPreviewModel();

        // Verificação 1: previewAnchor existe?
        // previewAnchor é o Transform onde a arma será instanciada
        // Se for null, não temos lugar para colocar a arma
        if (previewAnchor == null) {
            Debug.LogError("[RebuildPreviewModel] ❌ previewAnchor is NULL! Assign it in Inspector under 'Preview Anchor'");
            return;
        }

        // Verificação 2: selectedItemData existe?
        // selectedItemData contém dados do item selecionado (incluindo prefab)
        if (selectedItemData == null) {
            Debug.LogWarning("[RebuildPreviewModel] selectedItemData is NULL (nenhum item selecionado)");
            return;
        }

        // Verificação 3: PreviewPrefab está atribuído?
        // Cada ShopItemData precisa ter um prefab 3D atribuído
        // Se for null, significa que o item não tem modelo 3D configurado
        if (selectedItemData.PreviewPrefab == null) {
            Debug.LogWarning($"[RebuildPreviewModel] ⚠️  Item '{selectedItemData.ItemName}' não tem PreviewPrefab atribuído!");
            return;
        }

        Debug.Log($"[RebuildPreviewModel] 🔧 Iniciando... Item: {selectedItemData.ItemName}");
        Debug.Log($"[RebuildPreviewModel] Prefab: {selectedItemData.PreviewPrefab.name}, Parent: {previewAnchor.name}");

        // CONCEITO: Instantiate
        // Instantiate cria uma cópia do prefab em runtime
        // (parametro 1) = o prefab a copiar
        // (parametro 2) = Transform pai (a cópia vai ser filho desse Transform)
        // O modelo agora é filho do previewAnchor
        activePreviewModel = Instantiate(selectedItemData.PreviewPrefab, previewAnchor);
        
        // CONCEITO: LocalPosition vs WorldPosition
        // localPosition = posição RELATIVA ao pai (previewAnchor)
        // Aqui resetamos para (0,0,0) para a arma ficar centrada no anchor
        activePreviewModel.transform.localPosition = Vector3.zero;
        
        // CONCEITO: Quaternion.identity = Rotação Zero
        // identity significa "sem rotação" (0° em todos os eixos)
        // Começamos com a arma na rotação padrão
        activePreviewModel.transform.localRotation = Quaternion.identity;
        
        // CONCEITO: Escala para Preview em Canvas
        // O Canvas é gigante comparado à cena 3D normal
        // Uma arma em scale 1,1,1 fica praticamente invisível no preview
        // Multiplicamos a scale por previewModelScale para torná-la visível
        // Você ajusta previewModelScale no Inspector para encontrar o tamanho ideal
        Vector3 scaledSize = Vector3.one * previewModelScale;
        activePreviewModel.transform.localScale = scaledSize;

        // CONCEITO: Layer Assignment para RawImage + RenderTexture
        // A câmera de preview usa culling mask para renderizar apenas objetos na layer "Weapon"
        // Então precisamos colocar o modelo e todos seus filhos nessa layer
        // Isso garante que a câmera vê o modelo e o renderiza na RenderTexture
        AssignWeaponLayer(activePreviewModel);

        // Debug: Verificar se o modelo tem componentes de renderização
        // Um modelo 3D precisa ter Renderer para ser visualizado
        Renderer[] renderers = activePreviewModel.GetComponentsInChildren<Renderer>();
        Debug.Log($"[RebuildPreviewModel] ✓ Modelo instanciado: {activePreviewModel.name}");
        Debug.Log($"[RebuildPreviewModel] ✓ Renderers encontrados: {renderers.Length}");
        Debug.Log($"[RebuildPreviewModel] ✓ Layer assignment: {activePreviewModel.layer} (Weapon)");
        
        if (renderers.Length == 0) {
            Debug.LogWarning("[RebuildPreviewModel] ⚠️  Modelo não tem Renderer! Pode não aparecer visualmente.");
        }

        // Debug: Informação sobre o transform
        Debug.Log($"[RebuildPreviewModel] ✓ Posição local: {activePreviewModel.transform.localPosition}");
        Debug.Log($"[RebuildPreviewModel] ✓ Posição MUNDIAL: {activePreviewModel.transform.position}");
        Debug.Log($"[RebuildPreviewModel] ✓ Rotação local: {activePreviewModel.transform.localEulerAngles}");
        Debug.Log($"[RebuildPreviewModel] ✓ Scale local: {activePreviewModel.transform.localScale}");
        
        // Debug: Informação sobre a câmera de preview
        Camera previewCamera = GetWeaponPreviewCamera();
        if (previewCamera != null) {
            Debug.Log($"[RebuildPreviewModel] 📷 Preview Camera posição: {previewCamera.transform.position}");
            Debug.Log($"[RebuildPreviewModel] 📷 Preview Camera rotação: {previewCamera.transform.eulerAngles}");
            Debug.Log($"[RebuildPreviewModel] 📷 Preview Camera FOV (ANTES): {previewCamera.fieldOfView}");
        }
        
        // CONCEITO: Ajuste Dinâmico de Câmera
        // Após instanciar o modelo, ajustamos a posição Z da câmera para esta arma
        // Cada arma pode ter um Z diferente para enquadrar melhor
        AdjustCameraZPosition();
        
        if (previewCamera != null) {
            Debug.Log($"[RebuildPreviewModel] 📷 Preview Camera Z (DEPOIS): {previewCamera.transform.position.z}");
        }
    }

    /// <summary>
    /// Rotates the selected preview model using unscaled time so it keeps rotating while gameplay is paused.
    /// 
    /// CONCEITO: Rotação Contínua (Frame-based)
    /// Para fazer um objeto girar suavemente, multiplicamos a velocidade por Time.deltaTime
    /// Time.deltaTime = tempo desde o último frame (ex: 0.016 segundos a 60 FPS)
    /// Isso garante que a rotação seja consistente independente do FPS do jogador
    /// 
    /// EXEMPLO PRÁTICO:
    /// Se previewRotationSpeed = 360 (graus por segundo), com Time.deltaTime = 0.016 (60 FPS):
    ///   Rotação por frame = 360 * 0.016 = 5.76 graus por frame
    ///   Em 60 frames (1 segundo), faz 360 graus = uma volta completa!
    /// 
    /// CONCEITO: Time.unscaledDeltaTime
    /// unscaledDeltaTime continua funcionando mesmo quando Time.timeScale = 0 (pausa do jogo)
    /// Assim, a arma segue girando suavemente mesmo quando o jogo está pausado
    /// Isso é melhor para UI porque quer que elementos visuais sigam animados mesmo em pause
    /// </summary>
    private void RotatePreviewModel() {
        // CONCEITO: Guard Clause (Verificação de Proteção)
        // Se activePreviewModel é null (não existe), não há nada para girar
        // Em vez de deixar um erro acontecer, checamos e saímos cedo
        // Isso torna o código mais seguro e legível
        if (activePreviewModel == null) {
            return;
        }

        // CONCEITO: Rotate (Rotação Incremental)
        // transform.Rotate não DEFINE a rotação, ela ADICIONA à rotação existente
        // A cada frame, adicionamos mais alguns graus, criando a ilusão de movimento contínuo
        //
        // Parâmetros de Rotate:
        // 1) Vector3.up = eixo de rotação
        //    - Vector3.up = (0, 1, 0) = eixo Y (vertical)
        //    - Isso faz a arma girar como um pião
        // 
        // 2) previewRotationSpeed * Time.unscaledDeltaTime = quantidade em graus
        //    - previewRotationSpeed é a velocidade base (ex: 35 graus/segundo)
        //    - Time.unscaledDeltaTime é o tempo desde o último frame
        //    - Multiplicar garante movimento suave independente da taxa de frames
        // 
        // 3) Space.Self = usar EIXOS LOCAIS do objeto
        //    - Space.Self = os eixos do próprio objeto (sua rotação local)
        //    - Space.World = os eixos do mundo (eixo Y global)
        //    - Queremos Self porque a arma gira em relação a si mesma, não ao mundo
        activePreviewModel.transform.Rotate(Vector3.up, previewRotationSpeed * Time.unscaledDeltaTime, Space.Self);
    }

    /// <summary>
    /// Destroys the active instantiated preview model, if any.
    /// 
    /// CONCEITO: Destruição de GameObjects
    /// Quando você cria um GameObject com Instantiate(), ele continua existindo na memória até ser destruído.
    /// Se criássemos um novo a cada clique, sem destruir o antigo, teríamos MUITOS GameObjects invisíveis
    /// consumindo memória (memory leak).
    /// Destroy() remove o GameObject da cena e libera a memória que ele usava.
    /// 
    /// CONCEITO: Restaurar Estado Original
    /// Quando destruímos o preview, restauramos a câmera ao seu Z original.
    /// </summary>
    private void DestroyPreviewModel() {
        // Verificação: activePreviewModel != null significa "se o modelo existe"
        if (activePreviewModel != null) {
            // Destroy remove o GameObject da cena e da memória
            Destroy(activePreviewModel);
            // Depois de destruir, colocamos null para indicar "não há modelo agora"
            activePreviewModel = null;
        }
        
        // CONCEITO: Restaurar Posição Original da Câmera
        // Quando o preview é destruído, voltamos a câmera ao seu Z original
        // Assim, quando um novo weapon for selecionado, a câmera estará pronta
        Camera previewCamera = GetWeaponPreviewCamera();
        if (previewCamera != null) {
            Vector3 newPosition = previewCamera.transform.position;
            newPosition.z = originalCameraZ;
            previewCamera.transform.position = newPosition;
            Debug.Log($"[ShopUI] Restored camera Z to original: {originalCameraZ}");
        }
    }

    /// <summary>
    /// Assigns the "Weapon" layer to a GameObject and all its children.
    /// 
    /// CONCEITO: Layers em Unity (Filtragem de Renderização)
    /// Layers são como "categorias" que você coloca GameObjects.
    /// Uma câmera pode ter um "Culling Mask" que diz "só renderiza objetos na layer X".
    /// 
    /// Sistema de Preview 3D:
    /// - Câmera de preview tem Culling Mask = "Weapon" only
    /// - Isso significa: "renderize APENAS objetos na layer 'Weapon'"
    /// - Se a arma não estiver na layer "Weapon", a câmera não a vê!
    /// - Por isso precisamos colocar a arma (e todos seus filhos) nessa layer
    /// </summary>
    private void AssignWeaponLayer(GameObject targetGameObject) {
        // CONCEITO: LayerMask.NameToLayer (Conversão de String para ID)
        // Layers são identificadas por ID (0, 1, 2, etc), não por nome
        // LayerMask.NameToLayer("Weapon") converte "Weapon" (texto) para seu ID numérico
        // Isso é necessário porque o code só trabalha com números internamente
        int weaponLayerID = LayerMask.NameToLayer("Weapon");
        
        // CONCEITO: Validação (Verificação de Erro)
        // Se LayerMask retorna -1, significa que a layer não foi criada ainda
        // Checamos isso antes de continuar para evitar erros silenciosos
        if (weaponLayerID < 0) {
            Debug.LogError("[AssignWeaponLayer] ❌ Layer 'Weapon' does not exist! Create it in Edit → Project Settings → Tags and Layers");
            return;
        }
        
        // Atribui a layer ao GameObject raiz
        // targetGameObject.layer = ID da layer
        targetGameObject.layer = weaponLayerID;
        
        // CONCEITO: Chamada Recursiva
        // Recursão significa "uma função que chama a si mesma"
        // A arma pode ter filhos (mira, cano, etc) que também têm Renderers
        // Precisamos colocar TUDO na layer "Weapon", não só a raiz
        // AssignWeaponLayerToChildren vai fazer isso recursivamente
        AssignWeaponLayerToChildren(targetGameObject.transform, weaponLayerID);
    }
    
    /// <summary>
    /// Recursively assigns the "Weapon" layer to all child GameObjects.
    /// 
    /// CONCEITO: Recursão (Função que Chama a Si Mesma)
    /// Recursão é um padrão poderoso quando você precisa processar estruturas em árvore.
    /// Uma arma pode ter essa estrutura:
    ///   - Handgun (raiz)
    ///     - Barrel (filho)
    ///       - Muzzle (neto)
    ///     - Handle (filho)
    /// 
    /// Em vez de fazer loops complicados, recursão é perfeita aqui:
    /// Para cada filho, atribui a layer, e depois chama a si mesma nele.
    /// Isso processa TODOS os níveis automaticamente.
    /// </summary>
    private void AssignWeaponLayerToChildren(Transform parent, int layerID) {
        // CONCEITO: foreach (Para Cada)
        // foreach percorre uma coleção (lista, array, filhos, etc) um por um
        // Aqui: foreach (Transform child in parent) = "para cada filho deste parent"
        foreach (Transform child in parent) {
            // Atribui a layer ao filho
            // child.gameObject pega o GameObject desse Transform
            child.gameObject.layer = layerID;
            
            // CONCEITO: Recursão em Ação
            // Chamamos a própria função novamente, mas com "child" como novo parent
            // Se "child" tiver seus próprios filhos, eles vão ser processados também
            // Isso continua até não haver mais filhos (fim da recursão)
            AssignWeaponLayerToChildren(child, layerID);
        }
    }

    /// <summary>
    /// Helper method to find the weapon preview camera in the hierarchy.
    /// Returns the first Camera component found as a child of previewAnchor or null.
    /// 
    /// CONCEITO: Sistema RawImage + RenderTexture + Camera
    /// Este é um padrão avançado para renderizar cenas 3D dentro de UI Canvas 2D!
    /// 
    /// Como funciona em 3 passos:
    /// 1) WeaponPreviewCamera (câmera especial):
    ///    - Renderiza APENAS objetos na layer "Weapon"
    ///    - Seu resultado é salvo em WeaponPreviewRT (RenderTexture)
    ///    - Uma RenderTexture é como uma "foto" 2D da cena 3D
    /// 
    /// 2) WeaponPreviewRT (RenderTexture):
    ///    - Uma textura 2D que contém o que a câmera renderizou
    ///    - Atualiza a cada frame com a nova imagem 3D
    /// 
    /// 3) RawImage (componente UI):
    ///    - Um componente especial de UI que mostra uma textura
    ///    - Recebe WeaponPreviewRT como "Texture"
    ///    - Exibe a imagem 3D da câmera dentro do Canvas 2D!
    /// 
    /// Por que é tão complicado?
    /// - Canvas é 2D, não pode renderizar 3D direto
    /// - Solução: usar uma câmera invisível para "fotografar" o 3D
    /// - Salvar a foto em uma textura
    /// - Mostrar a foto no Canvas com RawImage
    /// </summary>
    private Camera GetWeaponPreviewCamera() {
        // CONCEITO: Null Checking (Validação de Existência)
        // previewAnchor pode ser null se não foi atribuído no Inspector
        // Checamos antes de usá-lo para evitar erros
        if (previewAnchor == null) return null;
        
        // CONCEITO: Transform.parent
        // Cada Transform tem um "pai" (parent)
        // Exemplo de hierarquia:
        //   WeaponPreview (pai)
        //     └─ WeaponSpinningAnchor (filho)
        //        └─ Arma instanciada aqui
        // 
        // previewAnchor é o WeaponSpinningAnchor
        // parent é o WeaponPreview (que contém a câmera)
        Transform parent = previewAnchor.parent;
        if (parent != null) {
            // CONCEITO: GetComponentInChildren<Camera>()
            // GetComponentInChildren busca um componente em todos os filhos
            // GetComponent busca no objeto atual
            // GetComponentInParent busca nos pais
            // 
            // Aqui dizemos: "procure uma câmera em WeaponPreview e seus filhos"
            // O <Camera> entre os < > é a GENÉRICA - diz ao C# qual tipo buscar
            Camera cam = parent.GetComponentInChildren<Camera>();
            if (cam != null) return cam;
        }
        
        // Se chegou aqui, não encontrou câmera
        return null;
    }

    /// <summary>
    /// Dynamically adjusts the preview camera Z position based on the selected weapon.
    /// This prevents smaller weapons from appearing too close and larger weapons from being cut off.
    /// </summary>
    private void AdjustCameraZPosition() {
        // CONCEITO: Guard Clause
        // Se não há dados da arma selecionada, não fazemos nada
        if (selectedItemData == null) return;
        
        Camera previewCamera = GetWeaponPreviewCamera();
        if (previewCamera == null) return;
        
        // CONCEITO: Busca em Lista com LINQ
        // FirstOrDefault procura o primeiro item que atende à condição
        // Se não encontrar, retorna um valor padrão (struct vazio)
        string weaponID = selectedItemData.ItemID;
        WeaponCameraZPosition foundConfig = cameraZPositions.FirstOrDefault(c => c.weaponID == weaponID);
        
        // CONCEITO: Verificação de Existência com Structs
        // Como struct não pode ser null, verificamos se weaponID não está vazio
        if (!string.IsNullOrEmpty(foundConfig.weaponID)) {
            // CONCEITO: Manipular Transform
            // transform.position é a posição world do objeto
            // Aqui alteramos apenas o Z (eixo profundidade), mantendo X e Y
            Vector3 newPosition = previewCamera.transform.position;
            newPosition.z = foundConfig.cameraZPosition;
            previewCamera.transform.position = newPosition;
            
            Debug.Log($"[ShopUI] Set camera Z to {foundConfig.cameraZPosition} for weapon: {weaponID}");
        } else {
            Debug.LogWarning($"[ShopUI] No camera Z configuration found for weapon: {weaponID}. Using current position.");
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

/// <summary>
/// Serializable struct to store custom camera Z position for specific weapons.
/// This allows designers to adjust camera depth per weapon type for optimal framing.
/// </summary>
[System.Serializable]
public struct WeaponCameraZPosition {
    // CONCEITO: Struct (Tipo de Dado Customizado)
    // Um struct é como uma "caixa" que pode conter vários valores relacionados
    // Aqui criamos uma caixa que contém:
    // 1. Um ID de arma (texto)
    // 2. Uma posição Z da câmera (número com decimais)
    // Quando você quer salvar um par de informações relacionadas, use struct
    
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
/// </summary>
[System.Serializable]
public struct WeaponAmmoPricing {
    // CONCEITO: Struct para Configuração de Dados
    // Este struct armazena as configurações de compra de munição para uma arma específica
    // Permite que diferentes armas tenham preços e quantidades diferentes
    // Exemplo: Pistola (+15 muni por $100) vs SMG (+60 muni por $300)
    
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
