using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Represents an individual shop item card with dynamic unlock/upgrade/ammo buttons.
/// Updates based on PlayerProgress to show appropriate options and prices.
/// </summary>
public class ShopItemCard : MonoBehaviour {
    [Header("UI Elements")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemLevelText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemDamageText;
    [SerializeField] private TextMeshProUGUI itemFireRateText;
    [SerializeField] private TextMeshProUGUI itemAmmoCapacityText;

    [Header("Buttons")]
    [SerializeField] private Button unlockUpgradeButton;
    [SerializeField] private TextMeshProUGUI unlockUpgradeButtonText;
    [SerializeField] private TextMeshProUGUI unlockUpgradePriceText;
    [SerializeField] private Button buyAmmoButton;
    [SerializeField] private TextMeshProUGUI buyAmmoButtonText;
    [SerializeField] private TextMeshProUGUI buyAmmoPriceText;

    [Header("Visual States")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color maxLevelColor = new Color(1f, 0.84f, 0f, 1f);

    [Header("Feedback Messages")]
    [SerializeField] private TextMeshProUGUI feedbackMessageText;
    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private Color successColor = Color.green;

    [Header("Fire Rate Display Balance")]
    [SerializeField] private float minFireRateDisplay = 1f;
    [SerializeField] private float maxFireRateDisplay = 10f;
    [SerializeField] private float pistolReferenceFireRate = 315f;
    [SerializeField] private float smgReferenceFireRate = 630f;

    private ShopItemData currentItemData;
    private Coroutine messageCoroutine;

    private void Awake() {
        if (unlockUpgradeButton != null)
            unlockUpgradeButton.onClick.AddListener(OnUnlockUpgradeClick);
        if (buyAmmoButton != null)
            buyAmmoButton.onClick.AddListener(OnBuyAmmoClick);
    }

    public void Setup(ShopItemData itemData) {
        currentItemData = itemData;
        if (currentItemData == null) return;

        if (itemIcon != null) itemIcon.sprite = currentItemData.Icon;
        if (itemNameText != null) itemNameText.text = currentItemData.ItemName;
        if (itemDescriptionText != null) itemDescriptionText.text = currentItemData.Description;
        
        RefreshCardState();
    }

    /// MÉTODO PRINCIPAL: Atualiza o estado visual do card baseado no progresso do jogador
    /// Este método é chamado toda vez que algo muda (compra, upgrade, etc)
    /// CONCEITO: State-based UI - a interface reflete o estado do jogo
    private void RefreshCardState() {
        // EARLY RETURN: Se dados essenciais estão faltando, sai da função imediatamente
        // Isso previne NullReferenceException (erro muito comum em Unity)
        if (currentItemData == null || PlayerProgress.Instance == null) return;

        // CACHE DE VALORES: Armazenamos valores usados múltiplas vezes em variáveis locais
        // Isso evita chamar os mesmos métodos repetidamente (mais eficiente)
        string itemID = currentItemData.ItemID;
        bool isUnlocked = PlayerProgress.Instance.IsWeaponUnlocked(itemID);
        int currentLevel = PlayerProgress.Instance.GetWeaponLevel(itemID);
        bool isMaxLevel = currentLevel >= PlayerProgress.MAX_UPGRADE_LEVEL;

        // ATUALIZAÇÃO DE TEXTO DO NÍVEL
        // OPERADOR TERNÁRIO ANINHADO: condição ? verdadeiro : falso
        // Lê-se: "Se não desbloqueado, mostra LOCKED; senão, se max level, mostra MAX LEVEL; senão, mostra Level X/10"
        if (itemLevelText != null) {
            itemLevelText.text = !isUnlocked ? "LOCKED" : isMaxLevel ? "MAX LEVEL" : $"Level {currentLevel}/{PlayerProgress.MAX_UPGRADE_LEVEL}";
        }

        // FEEDBACK VISUAL: Cor de fundo muda baseado no estado
        // CINZA = locked, BRANCO = unlocked, DOURADO = max level
        if (cardBackground != null) {
            cardBackground.color = !isUnlocked ? lockedColor : isMaxLevel ? maxLevelColor : unlockedColor;
        }

        // BOTÃO UNLOCK/UPGRADE - Lógica de State Machine
        // O botão muda seu comportamento baseado no estado atual
        if (unlockUpgradeButton != null) {
            if (!isUnlocked) {
                // ESTADO 1: LOCKED - Botão mostra "UNLOCK" com preço
                int cost = currentItemData.UnlockCost;
                if (unlockUpgradeButtonText != null) unlockUpgradeButtonText.text = "UNLOCK";
                if (unlockUpgradePriceText != null) unlockUpgradePriceText.text = $"${cost:N0}"; // :N0 = separador de milhares
                unlockUpgradeButton.interactable = EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(cost);
            } else if (isMaxLevel) {
                // ESTADO 2: MAX LEVEL - Botão desabilitado
                if (unlockUpgradeButtonText != null) unlockUpgradeButtonText.text = "MAX LEVEL";
                if (unlockUpgradePriceText != null) unlockUpgradePriceText.text = "";
                unlockUpgradeButton.interactable = false;
            } else {
                // ESTADO 3: UNLOCKED (1-9) - Botão mostra "UPGRADE" com preço escalável
                int cost = CalculateUpgradeCost(currentLevel);
                if (unlockUpgradeButtonText != null) unlockUpgradeButtonText.text = $"UPGRADE (Lv.{currentLevel + 1})";
                if (unlockUpgradePriceText != null) unlockUpgradePriceText.text = $"${cost:N0}";
                unlockUpgradeButton.interactable = EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(cost);
            }
        }

        // BOTÃO AMMO/REPAIR/BUY - Comportamento diferente por tipo de item
        if (buyAmmoButton != null) {
            if (!isUnlocked) {
                // Se locked, esconde botão de ammo (não faz sentido comprar ammo de arma locked)
                buyAmmoButton.gameObject.SetActive(false);
            } else {
                buyAmmoButton.gameObject.SetActive(true);
                
                // TIPO 1: VEST (Colete) - Botão de REPAIR
                if (currentItemData.IsVest) {
                    int cost = currentItemData.UnitCost;
                    if (buyAmmoButtonText != null) buyAmmoButtonText.text = "REPAIR";
                    if (buyAmmoPriceText != null) buyAmmoPriceText.text = $"${cost:N0}";
                    buyAmmoButton.interactable = EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(cost);
                } 
                // TIPO 2: BUILDABLE (Barricadas/Barris/Armadilhas) - Mostra quantidade/limite
                else if (currentItemData.IsBuildable) {
                    int qty = PlayerProgress.Instance.GetBuildableQuantity(itemID);
                    int cost = currentItemData.UnitCost;
                    if (buyAmmoButtonText != null) buyAmmoButtonText.text = $"BUY ({qty}/{PlayerProgress.MAX_BUILDABLE_QUANTITY})";
                    if (buyAmmoPriceText != null) buyAmmoPriceText.text = $"${cost:N0}";
                    // Desabilita se inventário cheio (qty >= 5) ou sem dinheiro
                    buyAmmoButton.interactable = EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(cost) && qty < PlayerProgress.MAX_BUILDABLE_QUANTITY;
                } 
                // TIPO 3: WEAPON (Armas) - Comprar munição com limite de reserva
                else if (currentItemData.IsWeapon) {
                    int reserve = PlayerProgress.Instance.GetReserveAmmo(itemID);
                    int maxReserve = currentItemData.WeaponData != null ? currentItemData.WeaponData.maxReserveAmmo : 999;
                    int cost = currentItemData.AmmoCost;
                    if (buyAmmoButtonText != null) buyAmmoButtonText.text = $"AMMO ({reserve}/{maxReserve})";
                    if (buyAmmoPriceText != null) buyAmmoPriceText.text = $"${cost:N0}";
                    // Desabilita se munição cheia ou sem dinheiro
                    buyAmmoButton.interactable = EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(cost) && reserve < maxReserve;
                }
            }
        }

        // STATS DINÂMICOS: Se for arma com WeaponDataSO, mostra stats escalados pelo nível
        if (currentItemData.IsWeapon && currentItemData.WeaponData != null) {
            WeaponDataSO data = currentItemData.WeaponData;
            // :F0 = sem decimais (número inteiro), :F1 = 1 casa decimal
            if (itemDamageText != null) itemDamageText.text = $"Damage: {data.GetDamageAtLevel(currentLevel):F0}";
            if (itemFireRateText != null) itemFireRateText.text = $"Fire Rate: {CalculateFireRateDisplay(data, currentLevel):F1}";
            if (itemAmmoCapacityText != null) itemAmmoCapacityText.text = $"Ammo: {data.GetMagazineCapacityAtLevel(currentLevel)}";
        }
    }

    /// <summary>
    /// Converts real fire rate (RPM) into a balanced shop display value between 1 and 10.
    /// </summary>
    /// <param name="weaponData">Weapon data source for the real RPM value.</param>
    /// <param name="level">Current weapon level used in the RPM calculation.</param>
    /// <returns>Fire rate display value clamped to the configured range.</returns>
    private float CalculateFireRateDisplay(WeaponDataSO weaponData, int level) {
        // Read the current RPM from the weapon progression data to keep UI aligned with gameplay values.
        float currentFireRate = weaponData.GetFireRateAtLevel(level);

        // Use the pistol-to-SMG range as the baseline slope so pistol starts near 1 and SMG near 3.
        float fireRateRange = Mathf.Max(smgReferenceFireRate - pistolReferenceFireRate, 1f);
        float displayIncreasePerRpm = 2f / fireRateRange;

        // Map RPM to a display score where pistol reference = 1 and SMG reference ≈ 3.
        float normalizedDisplay = minFireRateDisplay + (currentFireRate - pistolReferenceFireRate) * displayIncreasePerRpm;

        // Keep the configured range safe even if someone sets max lower than min in the Inspector.
        float safeMaxDisplay = Mathf.Max(maxFireRateDisplay, minFireRateDisplay);

        // Clamp to keep the stat readable and capped at the intended UI maximum.
        return Mathf.Clamp(normalizedDisplay, minFireRateDisplay, safeMaxDisplay);
    }

    private void OnUnlockUpgradeClick() {
        if (currentItemData == null || PlayerProgress.Instance == null || EconomyManager.Instance == null) return;

        bool isUnlocked = PlayerProgress.Instance.IsWeaponUnlocked(currentItemData.ItemID);

        if (!isUnlocked) {
            // Handle unlock
            if (EconomyManager.Instance.TrySpendCurrency(currentItemData.UnlockCost)) {
                PlayerProgress.Instance.UnlockWeapon(currentItemData.ItemID);
                ShowFeedback($"{currentItemData.ItemName} UNLOCKED!", successColor);
                Debug.Log($"[ShopItemCard] Unlocked {currentItemData.ItemName}!");
                RefreshCardState();
            } else {
                int missingAmount = currentItemData.UnlockCost - EconomyManager.Instance.GetCurrentCurrency();
                ShowFeedback($"Insufficient funds! Need {missingAmount} more coins.", errorColor);
            }
        } else {
            // Handle upgrade
            int currentLevel = PlayerProgress.Instance.GetWeaponLevel(currentItemData.ItemID);
            
            if (currentLevel >= PlayerProgress.MAX_UPGRADE_LEVEL) {
                ShowFeedback("Already at MAX LEVEL! Exclusive power active.", errorColor);
                return;
            }

            int upgradeCost = CalculateUpgradeCost(currentLevel);
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.TryUpgradeWeapon(currentItemData.ItemID, currentItemData.BaseUpgradeCost)) {
                int newLevel = PlayerProgress.Instance.GetWeaponLevel(currentItemData.ItemID);
                if (newLevel == PlayerProgress.MAX_UPGRADE_LEVEL) {
                    ShowFeedback("MAX LEVEL REACHED! Exclusive Power Activated!", successColor);
                } else {
                    ShowFeedback($"Upgraded to Level {newLevel}!", successColor);
                }
                Debug.Log($"[ShopItemCard] Upgraded {currentItemData.ItemName}!");
                RefreshCardState();
            } else {
                int missingAmount = upgradeCost - EconomyManager.Instance.GetCurrentCurrency();
                ShowFeedback($"Insufficient funds! Need {missingAmount} more coins.", errorColor);
            }
        }
    }

    private void OnBuyAmmoClick() {
        if (currentItemData == null || PlayerProgress.Instance == null || EconomyManager.Instance == null) return;

        if (currentItemData.IsVest) {
            PlayerArmor armor = FindObjectOfType<PlayerArmor>();
            if (armor == null) {
                ShowFeedback("Player armor system not found!", errorColor);
                return;
            }

            if (armor.GetCurrentArmor() >= armor.GetMaxArmor()) {
                ShowFeedback("Vest already at full durability!", errorColor);
                return;
            }

            if (EconomyManager.Instance.TrySpendCurrency(currentItemData.UnitCost)) {
                armor.RepairArmor(armor.GetMaxArmor());
                ShowFeedback("Vest repaired!", successColor);
                RefreshCardState();
            } else {
                int missingAmount = currentItemData.UnitCost - EconomyManager.Instance.GetCurrentCurrency();
                ShowFeedback($"Insufficient funds! Need {missingAmount} more coins.", errorColor);
            }
        } else if (currentItemData.IsBuildable) {
            int currentQuantity = PlayerProgress.Instance.GetBuildableQuantity(currentItemData.ItemID);
            if (currentQuantity >= PlayerProgress.MAX_BUILDABLE_QUANTITY) {
                ShowFeedback($"Inventory full! Max {PlayerProgress.MAX_BUILDABLE_QUANTITY} {currentItemData.ItemName} allowed.", errorColor);
                return;
            }

            if (EconomyManager.Instance.TrySpendCurrency(currentItemData.UnitCost)) {
                PlayerProgress.Instance.AddBuildable(currentItemData.ItemID, 1);
                ShowFeedback($"{currentItemData.ItemName} purchased!", successColor);
                RefreshCardState();
            } else {
                int missingAmount = currentItemData.UnitCost - EconomyManager.Instance.GetCurrentCurrency();
                ShowFeedback($"Insufficient funds! Need {missingAmount} more coins.", errorColor);
            }
        } else if (currentItemData.IsWeapon) {
            int maxReserve = currentItemData.WeaponData != null ? currentItemData.WeaponData.maxReserveAmmo : 999;
            int currentAmmo = PlayerProgress.Instance.GetReserveAmmo(currentItemData.ItemID);

            if (currentAmmo >= maxReserve) {
                ShowFeedback($"Ammo reserve full! ({currentAmmo}/{maxReserve})", errorColor);
                return;
            }

            if (EconomyManager.Instance.TrySpendCurrency(currentItemData.AmmoCost)) {
                PlayerProgress.Instance.AddReserveAmmo(currentItemData.ItemID, currentItemData.AmmoAmountPerPurchase, maxReserve);
                ShowFeedback($"Ammo purchased!", successColor);
                RefreshCardState();
            } else {
                int missingAmount = currentItemData.AmmoCost - EconomyManager.Instance.GetCurrentCurrency();
                ShowFeedback($"Insufficient funds! Need {missingAmount} more coins.", errorColor);
            }
        }
    }

    /// <summary>
    /// Shows a temporary feedback message on the card.
    /// </summary>
    private void ShowFeedback(string message, Color color) {
        if (feedbackMessageText == null) return;

        // Cancel previous message if still showing
        if (messageCoroutine != null) {
            StopCoroutine(messageCoroutine);
        }

        feedbackMessageText.text = message;
        feedbackMessageText.color = color;
        feedbackMessageText.gameObject.SetActive(true);

        messageCoroutine = StartCoroutine(HideMessageAfterDelay(2.0f));
    }

    /// <summary>
    /// Hides the feedback message after a delay.
    /// </summary>
    private System.Collections.IEnumerator HideMessageAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        if (feedbackMessageText != null) {
            feedbackMessageText.gameObject.SetActive(false);
        }
        messageCoroutine = null;
    }

    private int CalculateUpgradeCost(int currentLevel) {
        return UpgradeManager.Instance != null ? UpgradeManager.Instance.GetNextUpgradeCost(currentItemData.ItemID, currentItemData.BaseUpgradeCost) : 0;
    }
}
