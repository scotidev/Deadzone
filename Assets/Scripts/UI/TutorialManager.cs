using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deadzone.UI {

    /// <summary>
    /// Singleton manager that controls the tutorial flow.
    /// Lives in the HUD canvas hierarchy. Processes a queue of TutorialStepSO,
    /// checks completion conditions, and handles timeouts.
    ///
    /// Flow:
    /// 1. Game start -> queues startTutorials (mouse look, WASD, jump, crouch, run)
    /// 2. Shop closed with unlocks -> queues item selection + attack tutorials
    /// 3. Ammo conditions -> queues reload / melee tutorials (first time only)
    /// 4. Trigger zones -> queue any tutorial step via QueueTutorial()
    /// 5. Each step completes via action OR timeout -> auto-advances queue
    /// </summary>
    public class TutorialManager : MonoBehaviour {

        #region STATIC

        public static TutorialManager Instance { get; private set; }

        #endregion

        #region SERIALIZED FIELDS

        [Header("References")]
        [SerializeField] private TutorialUI tutorialUI;

        [Header("Game Start Tutorials")]
        [Tooltip("Tutorial steps that play when the game starts (e.g. mouse look, WASD).")]
        [SerializeField] private List<TutorialStepSO> startTutorials;

        [Header("Shop Tutorial Sprites")]
        [Tooltip("Sprites dos n\u00fameros de 1 a 8 (\u00edndice 0 = tecla 1, \u00edndice 7 = tecla 8).")]
        [SerializeField] private Sprite[] numberSprites;

        [Header("Tutorial Icons")]
        [Tooltip("Sprite exibido no tutorial de recarga (tecla R).")]
        [SerializeField] private Sprite reloadIcon;

        [Header("Tutorial Audio")]
        [Tooltip("Background music that plays during the tutorial.")]
        [SerializeField] private AudioClip tutorialBGM;
        [SerializeField] private float tutorialBGMVolume = 0.5f;

        [Header("Behavior")]
        [Tooltip("Minimum seconds a tutorial stays visible even if the action is detected early.")]
        [SerializeField] private float minimumDisplayTime = 2f;

        #endregion

        #region FIELDS

        private CharacterBehaviour playerCharacter;
        private InventoryBehaviour playerCharacterInventory;

        private readonly HashSet<string> shownSteps = new();
        private readonly Queue<TutorialStepSO> pendingQueue = new();
        private TutorialStepSO currentStep;
        private float stepStartTime;
        private bool isProcessing;
        private bool isResolved;

        private readonly List<string> recentlyUnlockedItems = new();

        private bool wasJumping;
        private bool wasCrouching;
        private bool wasRunning;
        private bool wasAiming;
        private bool previousHadAmmo;
        private int previousTotalAmmo;
        private bool isCompleting;
        private bool completionTriggered;

        #endregion

        #region UNITY

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(gameObject);
                return;
            }
        }

        private void Start() {
            ResolvePlayer();
        }

        private void OnEnable() {
            ShopManager.ItemUnlocked += OnItemUnlocked;
            ShopManager.ShopClosed += OnShopClosed;
        }

        private void OnDisable() {
            ShopManager.ItemUnlocked -= OnItemUnlocked;
            ShopManager.ShopClosed -= OnShopClosed;
        }

        private void Update() {
            if (!isResolved) {
                ResolvePlayer();
                return;
            }

            ProcessCurrentStep();

            if (currentStep == null)
                CheckAmmoConditions();
        }

        #endregion

        #region METHODS

        #region QUEUE CONTROL

        /// <summary>
        /// Adds a step to the end of the queue. Starts processing if not already.
        /// Safe to call from TriggerZones or any external code.
        /// </summary>
        public void QueueTutorial(TutorialStepSO step) {
            if (step == null) return;

            if (currentStep != null) {
                tutorialUI?.Hide();
                currentStep = null;
                pendingQueue.Clear();
                isProcessing = false;
                isCompleting = false;
                completionTriggered = false;
            }

            pendingQueue.Enqueue(step);

            if (!isProcessing)
                ProcessQueue();
        }

        /// <summary>
        /// Cancels all pending tutorials and hides the current one.
        /// Called when the shop opens mid-tutorial.
        /// </summary>
        public void CancelAll() {
            if (currentStep != null) {
                tutorialUI?.Hide();
                currentStep = null;
            }

            pendingQueue.Clear();
            isProcessing = false;
            isCompleting = false;
            completionTriggered = false;
        }

        /// <summary>
        /// Starts processing the pending queue if not already processing.
        /// </summary>
        private void ProcessQueue() {
            if (isProcessing) return;

            if (pendingQueue.Count == 0) {
                if (currentStep == null)
                    tutorialUI?.Hide();

                return;
            }

            isProcessing = true;
            ShowNextStep();
        }

        /// <summary>
        /// Shows the next step from the queue, skipping already-shown or null steps.
        /// </summary>
        private void ShowNextStep() {
            if (!isResolved || pendingQueue.Count == 0) {
                isProcessing = false;
                return;
            }

            currentStep = pendingQueue.Dequeue();

            if (currentStep == null) {
                ShowNextStep();
                return;
            }

            if (shownSteps.Contains(currentStep.StepId)) {
                ShowNextStep();
                return;
            }

            shownSteps.Add(currentStep.StepId);

            stepStartTime = Time.realtimeSinceStartup;
            completionTriggered = false;
            tutorialUI?.Show(currentStep.TutorialText, currentStep.TutorialImage);
        }

        /// <summary>
        /// Completes the current step, handles auto-chaining, and shows the next step if available.
        /// </summary>
        private void CompleteCurrentStep() {
            if (currentStep == null) return;

            string nextId = currentStep.NextStepId;

            currentStep = null;
            tutorialUI?.Hide();

            bool isShopping = GameManager.Instance != null && GameManager.Instance.State == GameState.Shopping;
            if (isShopping) {
                isProcessing = false;
                return;
            }

            if (!string.IsNullOrEmpty(nextId)) {
                TutorialStepSO nextStep = FindStepById(nextId);
                if (nextStep != null) {
                    pendingQueue.Clear();
                    isProcessing = false;
                    pendingQueue.Enqueue(nextStep);
                    ProcessQueue();
                    return;
                }
            }

            if (pendingQueue.Count > 0) {
                ShowNextStep();
            } else {
                isProcessing = false;
            }
        }

        #endregion

        #region COMPLETION DETECTION

        /// <summary>
        /// Processes the current tutorial step, checking completion conditions and timeouts.
        /// </summary>
        private void ProcessCurrentStep() {
            if (currentStep == null || isCompleting) return;

            float elapsed = Time.realtimeSinceStartup - stepStartTime;

            bool isShopping = GameManager.Instance != null && GameManager.Instance.State == GameState.Shopping;

            if (!isShopping) {
                if (!completionTriggered)
                    completionTriggered = CheckCompletion();

                if (completionTriggered && (!string.IsNullOrEmpty(currentStep.NextStepId) || pendingQueue.Count > 0)) {
                    tutorialUI?.Hide();
                    CompleteCurrentStep();
                    return;
                }
            }

            float timeout = currentStep.Timeout > 0f ? currentStep.Timeout : tutorialUI.DefaultStepTimeout;
            float fadeStartTime = Mathf.Max(timeout - tutorialUI.FadeOutDuration, 0f);

            bool canCompleteByAction = !isShopping && completionTriggered && elapsed >= minimumDisplayTime;
            bool canCompleteByTimeout = elapsed >= fadeStartTime;

            if (canCompleteByAction || canCompleteByTimeout) {
                BeginCompletion();
            }
        }

        /// <summary>
        /// Starts the fade-out process for the current step.
        /// </summary>
        private void BeginCompletion() {
            isCompleting = true;
            tutorialUI?.StartFadeOut(OnFadeOutComplete);
        }

        /// <summary>
        /// Callback invoked when the fade-out animation completes.
        /// </summary>
        private void OnFadeOutComplete() {
            isCompleting = false;
            CompleteCurrentStep();
        }

        /// <summary>
        /// Checks whether the current step's completion condition has been met.
        /// </summary>
        private bool CheckCompletion() {
            switch (currentStep.CompletionType) {
                case CompletionType.OnMouseMove:
                    return playerCharacter != null && playerCharacter.GetInputLook().magnitude > 0.01f;

                case CompletionType.OnWASDPress:
                    return playerCharacter != null && playerCharacter.GetInputMovement().magnitude > 0.01f;

                case CompletionType.OnItemSelected: {
                    if (playerCharacterInventory == null) return false;
                    ItemBehaviour equipped = playerCharacterInventory.GetEquippedItem();
                    if (equipped == null) return false;
                    return equipped.GetItemID() == currentStep.CompletionParam;
                }

                case CompletionType.OnAttack: {
                    return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
                }

                case CompletionType.OnTimeout:
                    return false;

                case CompletionType.OnJumpPress: {
                    bool currentlyJumping = playerCharacter != null && playerCharacter.IsJumping();
                    bool justJumped = currentlyJumping && !wasJumping;
                    wasJumping = currentlyJumping;
                    return justJumped;
                }

                case CompletionType.OnCrouchPress: {
                    bool currentlyCrouching = playerCharacter != null && playerCharacter.IsCrouching();
                    bool justCrouched = currentlyCrouching && !wasCrouching;
                    wasCrouching = currentlyCrouching;
                    return justCrouched;
                }

                case CompletionType.OnRunPress: {
                    bool currentlyRunning = playerCharacter != null && playerCharacter.IsRunning();
                    bool justRan = currentlyRunning && !wasRunning;
                    wasRunning = currentlyRunning;
                    return justRan;
                }

                case CompletionType.OnReloadPress: {
                    return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
                }

                case CompletionType.OnMeleePress: {
                    return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
                }

                case CompletionType.OnAimPress: {
                    bool currentlyAiming = playerCharacter != null && playerCharacter.IsAiming();
                    bool justAimed = currentlyAiming && !wasAiming;
                    wasAiming = currentlyAiming;
                    return justAimed;
                }

                default:
                    return false;
            }
        }

        #endregion

        #region AMMO CONDITIONS

        /// <summary>
        /// Monitors the equipped weapon's ammo state.
        /// Queues tutorials for empty magazine (reload).
        /// Each tutorial only fires once per weapon per session via shownSteps.
        /// </summary>
        private void CheckAmmoConditions() {
            if (playerCharacterInventory == null) return;

            WeaponBehaviour equippedWeapon = playerCharacterInventory.GetEquipped();
            if (equippedWeapon == null) {
                previousHadAmmo = false;
                previousTotalAmmo = 0;
                return;
            }

            string weaponID = equippedWeapon.GetItemID();
            bool hasAmmo = equippedWeapon.HasAmmunition();
            int totalAmmo = PlayerProgress.Instance != null ? PlayerProgress.Instance.GetItemTotal(weaponID) : 0;

            if (previousHadAmmo && !hasAmmo) {
                string stepId = $"ammo_empty_{weaponID}";
                if (!shownSteps.Contains(stepId)) {
                    TutorialStepSO step = ScriptableObject.CreateInstance<TutorialStepSO>();
                    step.Setup(
                        stepId,
                        "Aperte R para recarregar",
                        reloadIcon,
                        CompletionType.OnReloadPress,
                        "",
                        0f
                    );

                    QueueTutorial(step);
                }
            }

            previousHadAmmo = hasAmmo;
            previousTotalAmmo = totalAmmo;
        }

        #endregion

        #region SHOP EVENTS

        /// <summary>
        /// Called when an item is unlocked in the shop. Records the item ID for later tutorial queueing.
        /// </summary>
        private void OnItemUnlocked(string itemID) {
            if (!recentlyUnlockedItems.Contains(itemID))
                recentlyUnlockedItems.Add(itemID);
        }

        /// <summary>
        /// Called when the shop closes. Queues item selection tutorials for any recently unlocked items.
        /// </summary>
        private void OnShopClosed(bool hasPurchased) {
            if (recentlyUnlockedItems.Count == 0)
                return;

            CancelAll();

            Inventory inventory = playerCharacterInventory as Inventory;
            if (inventory == null) return;

            foreach (string itemID in recentlyUnlockedItems) {
                int slotIndex = inventory.GetSlotIndexForItemID(itemID);
                if (slotIndex < 0) continue;

                int keyNumber = slotIndex + 1;

                TutorialStepSO selectStep = ScriptableObject.CreateInstance<TutorialStepSO>();
                selectStep.Setup(
                    $"unlock_select_{itemID}",
                    GetItemUnlockText(itemID),
                    numberSprites != null && keyNumber - 1 < numberSprites.Length ? numberSprites[keyNumber - 1] : null,
                    CompletionType.OnItemSelected,
                    itemID,
                    0f
                );

                pendingQueue.Enqueue(selectStep);
            }

            recentlyUnlockedItems.Clear();

            if (!isProcessing)
                ProcessQueue();
        }

        #endregion

        #region HELPERS

        /// <summary>
        /// Finds a TutorialStepSO by its stepId. Checks the serialized startTutorials list first,
        /// then searches all loaded TutorialStepSO assets.
        /// </summary>
        private TutorialStepSO FindStepById(string stepId) {
            foreach (TutorialStepSO step in startTutorials) {
                if (step != null && step.StepId == stepId)
                    return step;
            }

            TutorialStepSO[] allSteps = Resources.FindObjectsOfTypeAll<TutorialStepSO>();
            foreach (TutorialStepSO step in allSteps) {
                if (step != null && step.StepId == stepId)
                    return step;
            }

            return null;
        }

        /// <summary>
        /// Resolves the player character reference and starts processing the tutorial queue.
        /// </summary>
        private void ResolvePlayer() {
            if (isResolved) return;

            IGameModeService gameMode = ServiceLocator.Current.Get<IGameModeService>();
            if (gameMode == null) return;

            playerCharacter = gameMode.GetPlayerCharacter();
            if (playerCharacter == null) return;

            playerCharacterInventory = playerCharacter.GetInventory();
            isResolved = true;

            IAudioManagerService audioService = ServiceLocator.Current.Get<IAudioManagerService>();
            if (audioService != null && tutorialBGM != null) {
                audioService.PlayBGM(tutorialBGM, true, 1.5f, tutorialBGMVolume);
            }

            if (pendingQueue.Count > 0 && !isProcessing)
                ProcessQueue();
        }

        /// <summary>
        /// Gets the unlock text for a shop item by its ID.
        /// </summary>
        private string GetItemUnlockText(string itemID) {
            ShopItemDataSO[] allItems = Resources.FindObjectsOfTypeAll<ShopItemDataSO>();
            foreach (ShopItemDataSO shopItem in allItems) {
                if (shopItem.ItemID == itemID)
                    return shopItem.UnlockText;
            }

            return "pressione para selecionar";
        }

        #endregion

        #endregion

    }

}
