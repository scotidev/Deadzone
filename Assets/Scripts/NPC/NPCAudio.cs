using InfimaGames.LowPolyShooterPack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ItemDialoguePool {
    public string itemID;
    public MerchantDialogueCategorySO dialogueCategory;
}

[DisallowMultipleComponent]
public class NPCAudio : MonoBehaviour {
    [Header("Dialogue Categories (ScriptableObjects)")]
    [SerializeField] private MerchantDialogueCategorySO closeRangeDialogue;
    [SerializeField] private MerchantDialogueCategorySO openShopDialogue;
    [SerializeField] private MerchantDialogueCategorySO outOfFundsDialogue;
    [SerializeField] private MerchantDialogueCategorySO playerAFKDialogue;
    [SerializeField] private MerchantDialogueCategorySO closeShopDialogue;
    [SerializeField] private MerchantDialogueCategorySO closeShopNoBuyDialogue;

    [Header("Conditional Dialogues - Unlock")]
    [Tooltip("Dialogue for each item when unlocked")]
    [SerializeField] private List<ItemDialoguePool> unlockDialogues = new List<ItemDialoguePool>();

    [Header("Conditional Dialogues - Upgrade")]
    [Tooltip("Dialogue for each item when upgraded")]
    [SerializeField] private List<ItemDialoguePool> upgradeDialogues = new List<ItemDialoguePool>();

    [Header("Conditional Dialogues - Buy Ammo")]
    [Tooltip("Dialogue for each item when buying ammo")]
    [SerializeField] private List<ItemDialoguePool> buyAmmoDialogues = new List<ItemDialoguePool>();

    [Header("Default Fallback Dialogues")]
    [SerializeField] private MerchantDialogueCategorySO defaultUpgradeDialogue;
    [SerializeField] private MerchantDialogueCategorySO defaultBuyAmmoDialogue;

    [Header("Playback Settings")]
    [Tooltip("Global merchant dialogue volume scale")]
    [Range(0f, 1f)]
    [SerializeField] private float dialogueVolumeScale = 1f;

    [Tooltip("Extra time added to subtitle duration after dialogue ends")]
    [SerializeField] private float subtitleExtraDuration = 1f;

    [Header("Subtitle UI")]
    [Tooltip("Optional explicit subtitle UI reference")]
    [SerializeField] private MerchantSubtitleUI subtitleUI;

    private IAudioManagerService audioService;
    private bool isDialoguePlaying;
    private Coroutine dialogueLockCoroutine;
    private int lastPlayedIndex = -1;
    private int lastUnlockPlayedIndex = -1;
    private int lastUpgradePlayedIndex = -1;
    private int lastAmmoPlayedIndex = -1;

    private void Awake() {
        ResolveAudioService();
        ResolveSubtitleUI();
    }

    private void OnEnable() {
        ShopManager.ItemUnlocked += HandleItemUnlocked;
        ShopManager.AmmoPurchased += HandleAmmoPurchased;
        ShopManager.ShopClosed += HandleShopClosed;
        ShopManager.PlayerAFK += HandlePlayerAFK;

        if (EconomyManager.Instance != null) {
            EconomyManager.Instance.OnPurchaseFailed += HandlePurchaseFailed;
        }

        UpgradeManager.OnItemUpgraded += HandleItemUpgraded;
    }

    private void OnDisable() {
        ShopManager.ItemUnlocked -= HandleItemUnlocked;
        ShopManager.AmmoPurchased -= HandleAmmoPurchased;
        ShopManager.ShopClosed -= HandleShopClosed;
        ShopManager.PlayerAFK -= HandlePlayerAFK;

        if (EconomyManager.Instance != null) {
            EconomyManager.Instance.OnPurchaseFailed -= HandlePurchaseFailed;
        }

        UpgradeManager.OnItemUpgraded -= HandleItemUpgraded;
    }

    public void OnPlayerEnteredRange() {
        PlayDialogueFromCategory(closeRangeDialogue, "close-range", ref lastPlayedIndex);
    }

    public void PlayRandomShopOpenDialogue() {
        PlayDialogueFromCategory(openShopDialogue, "shop-open", ref lastPlayedIndex);
    }

    private void HandleItemUnlocked(string itemID) {
        PlayDialogueFromItemPool(unlockDialogues, itemID, null, "unlock", ref lastUnlockPlayedIndex);
    }

    private void HandleAmmoPurchased(string itemID, int amountPurchased) {
        PlayDialogueFromItemPool(buyAmmoDialogues, itemID, defaultBuyAmmoDialogue, "buy-ammo", ref lastAmmoPlayedIndex);
    }

    private void HandleItemUpgraded(string itemID, ItemDataSO itemData) {
        PlayDialogueFromItemPool(upgradeDialogues, itemID, defaultUpgradeDialogue, "upgrade", ref lastUpgradePlayedIndex);
    }

    private void HandleShopClosed(bool hasPurchasedSomething) {
        if (hasPurchasedSomething) {
            PlayDialogueFromCategory(closeShopDialogue, "close-shop", ref lastPlayedIndex);
        } else {
            PlayDialogueFromCategory(closeShopNoBuyDialogue, "close-shop-no-buy", ref lastPlayedIndex);
        }
    }

    private void HandlePlayerAFK() {
        PlayDialogueFromCategory(playerAFKDialogue, "player-afk", ref lastPlayedIndex);
    }

    public void HandlePurchaseFailed(int cost, int currentCurrency) {
        PlayDialogueFromCategory(outOfFundsDialogue, "out-of-funds", ref lastPlayedIndex);
    }

    public void PlayOutOfFundsDialogue() {
        PlayDialogueFromCategory(outOfFundsDialogue, "out-of-funds", ref lastPlayedIndex);
    }

    public void OnButtonDisabled(ShopButtonDisabledReason reason) {
        if (reason == ShopButtonDisabledReason.InsufficientFunds) {
            PlayDialogueFromCategory(outOfFundsDialogue, "out-of-funds", ref lastPlayedIndex);
        }
    }

    private void PlayDialogueFromCategory(MerchantDialogueCategorySO category, string contextLabel, ref int lastIndex) {
        if (category == null || category.dialogues == null || category.dialogues.Length == 0) {
            return;
        }

        StopCurrentDialogue();

        MerchantDialogueLine line = GetRandomDialogueLine(category, ref lastIndex);
        if (line.clip == null) {
            Debug.LogWarning($"[NPCAudio] No playable dialogue for context '{contextLabel}'");
            return;
        }

        PlayLine(line);
    }

    private void PlayDialogueFromItemPool(List<ItemDialoguePool> pools, string itemID, MerchantDialogueCategorySO defaultCategory, string contextLabel, ref int lastIndex) {
        if (string.IsNullOrEmpty(itemID)) return;

        MerchantDialogueCategorySO category = GetCategoryFromPool(pools, itemID, defaultCategory);
        if (category == null) return;

        PlayDialogueFromCategory(category, contextLabel, ref lastIndex);
    }

    private void StopCurrentDialogue() {
        if (dialogueLockCoroutine != null) {
            StopCoroutine(dialogueLockCoroutine);
            dialogueLockCoroutine = null;
        }
        isDialoguePlaying = false;
        subtitleUI?.HideImmediate();
    }

    private MerchantDialogueCategorySO GetCategoryFromPool(List<ItemDialoguePool> pools, string itemID, MerchantDialogueCategorySO defaultCategory) {
        foreach (var pool in pools) {
            if (string.Equals(pool.itemID, itemID, System.StringComparison.OrdinalIgnoreCase)) {
                return pool.dialogueCategory;
            }
        }
        return defaultCategory;
    }

    private MerchantDialogueLine GetRandomDialogueLine(MerchantDialogueCategorySO category, ref int lastIndex) {
        MerchantDialogueLine[] lines = category.dialogues;

        if (category.allowImmediateRepeat) {
            int randomIndex = Random.Range(0, lines.Length);
            lastIndex = randomIndex;
            return lines[randomIndex];
        }

        if (lines.Length == 1) {
            lastIndex = 0;
            return lines[0];
        }

        int newIndex;
        do {
            newIndex = Random.Range(0, lines.Length);
        } while (newIndex == lastIndex && lines.Length > 1);

        lastIndex = newIndex;
        return lines[newIndex];
    }

    private void PlayLine(MerchantDialogueLine line) {
        ResolveAudioService();
        ResolveSubtitleUI();

        if (audioService == null || line.clip == null) return;

        audioService.PlayDialogue3D(line.clip, transform.position, dialogueVolumeScale, 1f, 50f);

        ShowSubtitle(line);
        BeginDialogueLock(line.clip.length);
    }

    private void ShowSubtitle(MerchantDialogueLine line) {
        if (subtitleUI == null || string.IsNullOrWhiteSpace(line.subtitle)) return;

        float duration = line.clip.length + subtitleExtraDuration;

        subtitleUI.ShowSubtitle(line.subtitle, duration);
    }

    private void BeginDialogueLock(float clipLength) {
        if (dialogueLockCoroutine != null) StopCoroutine(dialogueLockCoroutine);
        dialogueLockCoroutine = StartCoroutine(ReleaseLock(clipLength));
    }

    private IEnumerator ReleaseLock(float delay) {
        isDialoguePlaying = true;
        yield return new WaitForSeconds(Mathf.Max(0.1f, delay));
        isDialoguePlaying = false;
        dialogueLockCoroutine = null;
    }

    private void ResolveAudioService() {
        audioService ??= ServiceLocator.Current.Get<IAudioManagerService>();
    }

    private void ResolveSubtitleUI() {
        subtitleUI ??= MerchantSubtitleUI.Instance;
    }
}