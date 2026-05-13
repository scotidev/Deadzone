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
    /// 1. Game start → queues startTutorials (mouse look, WASD, jump, crouch, run)
    /// 2. Shop closed with unlocks → queues item selection + attack tutorials
    /// 3. Ammo conditions → queues reload / melee tutorials (first time only)
    /// 4. Trigger zones → queue any tutorial step via QueueTutorial()
    /// 5. Each step completes via action OR timeout → auto-advances queue
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

        [Header("Shop Tutorial Templates")]
        [Tooltip("Tutorial step shown after selecting an unlocked item: 'Left click to use'.")]
        [SerializeField] private TutorialStepSO actionTutorialTemplate;

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
        private float elapsedTime;
        private bool isProcessing;
        private bool isResolved;

        private readonly List<string> recentlyUnlockedItems = new();

        private bool wasJumping;
        private bool wasCrouching;
        private bool wasRunning;
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

            tutorialUI?.Hide();
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

        #region QUEUE CONTROL

        /// <summary>
        /// Adds a step to the end of the queue. Starts processing if not already.
        /// Safe to call from TriggerZones or any external code.
        /// </summary>
        public void QueueTutorial(TutorialStepSO step) {
            if (step == null) return;

            // If a tutorial is already showing, interrupt it immediately
            if (currentStep != null) {
                tutorialUI?.Hide();
                currentStep = null;
                pendingQueue.Clear();
                isProcessing = false;
                elapsedTime = 0f;
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
            elapsedTime = 0f;
            isCompleting = false;
            completionTriggered = false;
        }

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

            elapsedTime = 0f;
            completionTriggered = false;
            tutorialUI?.Show(currentStep.TutorialText, currentStep.TutorialImage);
        }

        private void CompleteCurrentStep() {
            if (currentStep == null) return;

            currentStep = null;
            elapsedTime = 0f;
            tutorialUI?.Hide();

            if (pendingQueue.Count > 0) {
                ShowNextStep();
            } else {
                isProcessing = false;
            }
        }

        #endregion

        #region COMPLETION DETECTION

        private void ProcessCurrentStep() {
            if (currentStep == null || isCompleting) return;

            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Shopping) {
                return;
            }

            elapsedTime += Time.deltaTime;

            // Detect completion action once (don't lose it on the exact frame it happens)
            if (!completionTriggered)
                completionTriggered = CheckCompletion();

            float timeout = currentStep.Timeout > 0f ? currentStep.Timeout : tutorialUI.DefaultStepTimeout;
            float fadeStartTime = Mathf.Max(timeout - tutorialUI.FadeOutDuration, 0f);

            bool canCompleteByAction = completionTriggered && elapsedTime >= minimumDisplayTime;
            bool canCompleteByTimeout = elapsedTime >= fadeStartTime;

            if (canCompleteByAction || canCompleteByTimeout) {
                BeginCompletion();
            }
        }

        private void BeginCompletion() {
            isCompleting = true;
            tutorialUI?.StartFadeOut(OnFadeOutComplete);
        }

        private void OnFadeOutComplete() {
            isCompleting = false;
            CompleteCurrentStep();
        }

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

                default:
                    return false;
            }
        }

        #endregion

        #region AMMO CONDITIONS

        /// <summary>
        /// Monitors the equipped weapon's ammo state.
        /// Queues tutorials for empty magazine (reload) and empty total (melee).
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
                        null,
                        CompletionType.OnReloadPress,
                        "",
                        0f
                    );

                    QueueTutorial(step);
                }
            }

            if (previousTotalAmmo > 0 && totalAmmo <= 0 && !hasAmmo) {
                string stepId = $"ammo_total_empty_{weaponID}";
                if (!shownSteps.Contains(stepId)) {
                    TutorialStepSO step = ScriptableObject.CreateInstance<TutorialStepSO>();
                    step.Setup(
                        stepId,
                        "Aperte F para ataque melee",
                        null,
                        CompletionType.OnMeleePress,
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

        private void OnItemUnlocked(string itemID) {
            if (!recentlyUnlockedItems.Contains(itemID))
                recentlyUnlockedItems.Add(itemID);
        }

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
                string itemName = GetItemDisplayName(itemID);

                TutorialStepSO selectStep = ScriptableObject.CreateInstance<TutorialStepSO>();
                selectStep.Setup(
                    $"unlock_select_{itemID}",
                    $"Pressione {keyNumber} para selecionar {itemName}",
                    null,
                    CompletionType.OnItemSelected,
                    itemID,
                    0f
                );

                QueueTutorial(selectStep);
            }

            recentlyUnlockedItems.Clear();

            TutorialStepSO actionStep = ScriptableObject.CreateInstance<TutorialStepSO>();
            actionStep.Setup(
                "unlock_action",
                actionTutorialTemplate != null ? actionTutorialTemplate.TutorialText : "Clique com o botão esquerdo para usar o item",
                actionTutorialTemplate?.TutorialImage,
                CompletionType.OnAttack,
                "",
                actionTutorialTemplate != null ? actionTutorialTemplate.Timeout : 0f
            );

            QueueTutorial(actionStep);
        }

        #endregion

        #region HELPERS

        private void ResolvePlayer() {
            if (isResolved) return;

            IGameModeService gameMode = ServiceLocator.Current.Get<IGameModeService>();
            if (gameMode == null) return;

            playerCharacter = gameMode.GetPlayerCharacter();
            if (playerCharacter == null) return;

            playerCharacterInventory = playerCharacter.GetInventory();
            isResolved = true;

            if (pendingQueue.Count > 0 && !isProcessing)
                ProcessQueue();
        }

        private string GetItemDisplayName(string itemID) {
            ShopItemDataSO[] allItems = Resources.FindObjectsOfTypeAll<ShopItemDataSO>();
            foreach (ShopItemDataSO shopItem in allItems) {
                if (shopItem.ItemID == itemID)
                    return shopItem.ItemName;
            }

            return itemID;
        }

        #endregion

    }

}