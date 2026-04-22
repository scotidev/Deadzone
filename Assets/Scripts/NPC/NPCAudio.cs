using InfimaGames.LowPolyShooterPack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* REFATORAÇÃO: A melhor forma de implementar o que está no PDF é separar os Dados (as falas) da Lógica (o player de áudio).

ScriptableObjects para Seções: Em vez de uma struct simples, você pode criar um arquivo para cada categoria do PDF. Um ScriptableObject chamado DialogueCategory que contém uma lista de MerchantDialogueLine. E aí tocar as falas pelo script NPCAudio. Isso torna mais fácil para os designers editarem as falas sem mexer no código.

Mapeamento por ID: Para os desbloqueios de armas, você já usa um weaponID. Você pode expandir isso para os upgrades, permitindo que a robô diga frases específicas para o "Pente de 100 rounds" da SMG ou para as "Barricadas Indestrutíveis" */

/// <summary>
/// Represents one merchant dialogue line with manually authored subtitle text.
/// </summary>
[System.Serializable]
public struct MerchantDialogueLine {
    [Tooltip("Audio clip that will be played for this line.")]
    public AudioClip clip;

    [Tooltip("Manual subtitle text shown on screen while this line plays.")]
    [TextArea(2, 4)]
    public string subtitle;

    [Tooltip("Optional per-line volume scale.")]
    [Range(0f, 1f)]
    public float volumeScale;

    [Tooltip("If greater than zero, overrides subtitle display duration in seconds.")]
    public float subtitleDurationOverride;
}

/// <summary>
/// Maps a weapon identifier to its pool of merchant dialogue lines.
/// </summary>
[System.Serializable]
public struct WeaponUnlockDialoguePool {
    [Tooltip("Weapon ID used by ShopItemData and PlayerProgress (for example: Pistol, SMG, Shotgun).")]
    public string weaponID;

    [Tooltip("Randomized dialogue options used when this weapon is unlocked.")]
    public List<MerchantDialogueLine> lines;
}

/// <summary>
/// Handles merchant dialogue playback and subtitle display for shop-related events.
/// </summary>
[DisallowMultipleComponent]
public class NPCAudio : MonoBehaviour {
    [Header("Dialogue Pools - Shop Open")]
    [Tooltip("Random lines used when the player opens the shop.")]
    [SerializeField] private List<MerchantDialogueLine> shopOpenDialogues = new List<MerchantDialogueLine>();

    [Header("Dialogue Pools - Ammo Purchase")]
    [Tooltip("Random lines used when ammo is purchased.")]
    [SerializeField] private List<MerchantDialogueLine> ammoPurchaseDialogues = new List<MerchantDialogueLine>();

    [Header("Dialogue Pools - Weapon Unlock")]
    [Tooltip("Per-weapon random dialogue pools for unlock moments.")]
    [SerializeField] private List<WeaponUnlockDialoguePool> weaponUnlockDialogues = new List<WeaponUnlockDialoguePool>();

    [Header("Playback")]
    [Tooltip("Global merchant dialogue volume scale.")]
    [Range(0f, 1f)]
    [SerializeField] private float dialogueVolumeScale = 1f;

    [Tooltip("Adds a small extra time so subtitles do not disappear too abruptly.")]
    [SerializeField] private float subtitleExtraDuration = 0.1f;

    [Tooltip("Used if clip length is unavailable or zero.")]
    [SerializeField] private float fallbackSubtitleDuration = 2f;

    [Tooltip("Logs warnings when a dialogue pool for a context is missing.")]
    [SerializeField] private bool logMissingDialoguePools = true;

    [Header("Subtitle UI")]
    [Tooltip("Optional explicit subtitle UI reference. If empty, the singleton MerchantSubtitleUI is used.")]
    [SerializeField] private MerchantSubtitleUI subtitleUI;

    /// <summary>
    /// Cached reference to the centralized audio service.
    /// </summary>
    private IAudioManagerService audioService;

    /// <summary>
    /// Tracks whether a dialogue line is currently playing.
    /// </summary>
    private bool isDialoguePlaying;

    /// <summary>
    /// Coroutine used to release the dialogue lock when the current line ends.
    /// </summary>
    private Coroutine dialogueLockCoroutine;

    /// <summary>
    /// Resolves dependencies before interaction starts.
    /// </summary>
    private void Awake() {
        ResolveAudioService();
        ResolveSubtitleUI();
    }

    /// <summary>
    /// Subscribes to shop events used to trigger contextual dialogue.
    /// </summary>
    private void OnEnable() {
        ShopUI.WeaponUnlocked += HandleWeaponUnlocked;
        ShopUI.AmmoPurchased += HandleAmmoPurchased;
    }

    /// <summary>
    /// Unsubscribes from shop events to prevent stale callbacks.
    /// </summary>
    private void OnDisable() {
        ShopUI.WeaponUnlocked -= HandleWeaponUnlocked;
        ShopUI.AmmoPurchased -= HandleAmmoPurchased;
    }

    /// <summary>
    /// Plays a random dialogue line for shop opening.
    /// </summary>
    public void PlayRandomShopOpenDialogue() {
        TryPlayRandomDialogue(shopOpenDialogues, "shop-open");
    }

    /// <summary>
    /// Legacy compatibility method mapped to random shop-open dialogue.
    /// </summary>
    public void PlayInteractionDialogue() {
        PlayRandomShopOpenDialogue();
    }

    /// <summary>
    /// Legacy compatibility method mapped to random shop-open dialogue.
    /// </summary>
    public void PlayOpenShopDialogue() {
        PlayRandomShopOpenDialogue();
    }

    /// <summary>
    /// Plays a random dialogue line for ammo purchases.
    /// </summary>
    public void PlayRandomAmmoPurchaseDialogue() {
        TryPlayRandomDialogue(ammoPurchaseDialogues, "ammo-purchase");
    }

    /// <summary>
    /// Plays a random dialogue line associated with a specific weapon unlock.
    /// </summary>
    /// <param name="weaponID">Unlocked weapon identifier.</param>
    public void PlayRandomWeaponUnlockDialogue(string weaponID) {
        if (!TryGetWeaponUnlockPool(weaponID, out List<MerchantDialogueLine> pool)) {
            if (logMissingDialoguePools)
                Debug.LogWarning($"[NPCAudio] No unlock dialogue pool configured for weapon '{weaponID}'.");
            return;
        }

        TryPlayRandomDialogue(pool, $"weapon-unlock:{weaponID}");
    }

    /// <summary>
    /// Handles ammo purchase notifications from ShopUI.
    /// </summary>
    /// <param name="weaponID">Weapon that received ammo.</param>
    /// <param name="amountPurchased">Purchased reserve amount.</param>
    private void HandleAmmoPurchased(string weaponID, int amountPurchased) {
        PlayRandomAmmoPurchaseDialogue();
    }

    /// <summary>
    /// Handles weapon unlock notifications from ShopUI.
    /// </summary>
    /// <param name="weaponID">Unlocked weapon identifier.</param>
    private void HandleWeaponUnlocked(string weaponID) {
        PlayRandomWeaponUnlockDialogue(weaponID);
    }

    /// <summary>
    /// Resolves the unified audio service from the Service Locator.
    /// </summary>
    private void ResolveAudioService() {
        audioService ??= ServiceLocator.Current.Get<IAudioManagerService>();
    }

    /// <summary>
    /// Resolves the subtitle UI reference.
    /// </summary>
    private void ResolveSubtitleUI() {
        subtitleUI ??= MerchantSubtitleUI.Instance;
    }

    /// <summary>
    /// Attempts to select and play a random dialogue line from a pool.
    /// </summary>
    /// <param name="pool">Candidate dialogue pool.</param>
    /// <param name="contextLabel">Context label used in logs.</param>
    /// <returns>True if a line was started.</returns>
    private bool TryPlayRandomDialogue(IReadOnlyList<MerchantDialogueLine> pool, string contextLabel) {
        // First principle: we avoid overlapping dialogue to keep voice and subtitles understandable.
        if (isDialoguePlaying)
            return false;

        if (!TryGetRandomPlayableLine(pool, out MerchantDialogueLine selectedLine)) {
            if (logMissingDialoguePools)
                Debug.LogWarning($"[NPCAudio] No playable dialogue line configured for context '{contextLabel}'.");
            return false;
        }

        PlayDialogueLine(selectedLine);
        return true;
    }

    /// <summary>
    /// Attempts to pick one random playable line (clip not null) from a pool.
    /// </summary>
    /// <param name="pool">Candidate dialogue pool.</param>
    /// <param name="line">Selected line output.</param>
    /// <returns>True if a valid line was found.</returns>
    private bool TryGetRandomPlayableLine(IReadOnlyList<MerchantDialogueLine> pool, out MerchantDialogueLine line) {
        line = default;

        if (pool == null || pool.Count == 0)
            return false;

        int startIndex = Random.Range(0, pool.Count);
        for (int offset = 0; offset < pool.Count; offset++) {
            MerchantDialogueLine candidate = pool[(startIndex + offset) % pool.Count];
            if (candidate.clip == null)
                continue;

            line = candidate;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves a weapon-specific unlock pool by weapon ID.
    /// </summary>
    /// <param name="weaponID">Unlocked weapon identifier.</param>
    /// <param name="pool">Resolved pool output.</param>
    /// <returns>True if a matching pool was found.</returns>
    private bool TryGetWeaponUnlockPool(string weaponID, out List<MerchantDialogueLine> pool) {
        pool = null;
        if (string.IsNullOrWhiteSpace(weaponID) || weaponUnlockDialogues == null)
            return false;

        for (int index = 0; index < weaponUnlockDialogues.Count; index++) {
            WeaponUnlockDialoguePool configuredPool = weaponUnlockDialogues[index];
            if (string.Equals(configuredPool.weaponID, weaponID, System.StringComparison.OrdinalIgnoreCase)) {
                pool = configuredPool.lines;
                return pool != null && pool.Count > 0;
            }
        }

        return false;
    }

    /// <summary>
    /// Plays one dialogue line using the dialogue audio channel and subtitle UI.
    /// </summary>
    /// <param name="line">Dialogue line to play.</param>
    private void PlayDialogueLine(MerchantDialogueLine line) {
        ResolveAudioService();
        ResolveSubtitleUI();

        if (audioService == null || line.clip == null)
            return;

        // First principle: final line volume combines NPC-level scale with line-level scale for fine tuning.
        float finalVolumeScale = Mathf.Clamp01(dialogueVolumeScale * Mathf.Clamp01(line.volumeScale <= 0f ? 1f : line.volumeScale));
        audioService.PlayDialogue2D(line.clip, finalVolumeScale);

        ShowSubtitle(line);
        BeginDialogueLock(line.clip.length);
    }

    /// <summary>
    /// Shows the subtitle text for the current dialogue line.
    /// </summary>
    /// <param name="line">Current dialogue line.</param>
    private void ShowSubtitle(MerchantDialogueLine line) {
        if (subtitleUI == null || string.IsNullOrWhiteSpace(line.subtitle))
            return;

        float subtitleDuration = line.subtitleDurationOverride > 0f
            ? line.subtitleDurationOverride
            : Mathf.Max(fallbackSubtitleDuration, line.clip.length + subtitleExtraDuration);

        subtitleUI.ShowSubtitle(line.subtitle, subtitleDuration);
    }

    /// <summary>
    /// Starts a temporary lock window to avoid overlapping merchant lines.
    /// </summary>
    /// <param name="clipLength">Current clip length in seconds.</param>
    private void BeginDialogueLock(float clipLength) {
        if (dialogueLockCoroutine != null)
            StopCoroutine(dialogueLockCoroutine);

        float lockDuration = Mathf.Max(0.1f, clipLength);
        dialogueLockCoroutine = StartCoroutine(ReleaseDialogueLockAfterDelay(lockDuration));
    }

    /// <summary>
    /// Releases the dialogue lock after a delay.
    /// </summary>
    /// <param name="delay">Delay in seconds.</param>
    /// <returns>Coroutine enumerator.</returns>
    private IEnumerator ReleaseDialogueLockAfterDelay(float delay) {
        isDialoguePlaying = true;
        yield return new WaitForSeconds(delay);
        isDialoguePlaying = false;
        dialogueLockCoroutine = null;
    }
}
