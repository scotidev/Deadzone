using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Main Character Component. This component handles the most important functions of the character, and interfaces
    /// with basically every part of the asset, it is the hub where it all converges.
    /// </summary>
    [RequireComponent(typeof(CharacterKinematics))]
    public sealed class Character : CharacterBehaviour {

        #region SERIALIZED FIELDS

        [Header("Inventory")]

        [SerializeField] private InventoryBehaviour inventory;
        [SerializeField] private int startingWeaponIndex = 0;

        [Header("Cameras")]

        [Tooltip("Normal Camera.")]
        [SerializeField] private Camera cameraWorld;

        [Header("Animation")]

        [Tooltip("Determines how smooth the locomotion blendspace is.")]
        [SerializeField] private float dampTimeLocomotion = 0.15f;

        [Tooltip("How smoothly we play aiming transitions. Beware that this affects lots of things!")]
        [SerializeField]
        private float dampTimeAiming = 0.3f;

        [Header("Animation Procedural")]

        [SerializeField] private Animator characterAnimator;

        #endregion

        #region FIELDS

        private bool aiming;
        private bool running;
        private bool inspecting;
        private bool reloading;
        private bool holstered;
        private bool holstering;
        private bool isAttackingMelee;
        private int lastWeaponIndexBeforeMelee;
        private bool holdingButtonRun;
        private bool holdingButtonFire;
        private bool holdingButtonJump;
        private bool holdingButtonCrouch;
        private bool holdingButtonAim;
        private bool cursorLocked;
        private bool interfaceMode;
        private float lastShotTime;
        private int layerOverlay;
        private int layerHolster;
        private int layerActions;
        private CharacterKinematics characterKinematics;
        private WeaponBehaviour equippedWeapon;
        private RuntimeAnimatorController initialRuntimeController;
        private WeaponAttachmentManagerBehaviour weaponAttachmentManager;
        private ScopeBehaviour equippedWeaponScope;
        private MagazineBehaviour equippedWeaponMagazine;
        private Vector2 axisLook;
        private Vector2 axisMovement;
        private bool tutorialTextVisible;
        private IAudioManagerService audioService;

        #endregion

        #region PROPERTIES

        public override bool IsInterfaceMode() => interfaceMode;

        /// <summary>
        /// When activated (value = true), it disables cursor locking (making it visible) and updates the cursor state in Unity.
        /// </summary>
        public override void SetInterfaceMode(bool value) {
            interfaceMode = value;
            cursorLocked = !interfaceMode;
            UpdateCursorState();
        }

        /// <summary>
        /// Clears all active input states. Used when pausing/resuming to prevent ghost inputs.
        /// </summary>
        public void ClearInputStates() {
            holdingButtonFire = false;
            holdingButtonAim = false;
            holdingButtonRun = false;
            holdingButtonJump = false;
            holdingButtonCrouch = false;
        }

        public override Camera GetCameraWorld() => cameraWorld;

        public override InventoryBehaviour GetInventory() => inventory;

        public override bool IsCrosshairVisible() => !aiming && !holstered && !(ShopManager.Instance?.IsShopOpen() ?? false);
        public override bool IsRunning() => running;

        public override bool IsJumping() => holdingButtonJump;

        public override bool IsCrouching() => holdingButtonCrouch;

        public override bool IsAiming() => aiming;
        public override bool IsCursorLocked() => cursorLocked;
        public override bool IsAttackingMelee() => isAttackingMelee;

        public override bool IsTutorialTextVisible() => tutorialTextVisible;

        public override Vector2 GetInputMovement() => axisMovement;
        public override Vector2 GetInputLook() => axisLook;

        #endregion

        #region CONSTANTS

        private static readonly int HashAimingAlpha = Animator.StringToHash("Aiming");
        private static readonly int HashMovement = Animator.StringToHash("Movement");

        #endregion

        #region UNITY

        protected override void Awake() {
            cursorLocked = true;

            UpdateCursorState();

            characterKinematics = GetComponent<CharacterKinematics>();

            audioService = ServiceLocator.Current.Get<IAudioManagerService>();

            if (characterAnimator != null)
                initialRuntimeController = characterAnimator.runtimeAnimatorController;

            inventory.Init(startingWeaponIndex);

            RefreshWeaponSetup();
        }
        protected override void Start() {
            layerHolster = characterAnimator.GetLayerIndex("Layer Holster");
            layerActions = characterAnimator.GetLayerIndex("Layer Actions");
            layerOverlay = characterAnimator.GetLayerIndex("Layer Overlay");
        }

        protected override void Update() {
            aiming = holdingButtonAim && CanAim();
            running = holdingButtonRun && CanRun();

            if (holdingButtonFire) {
                if (BuildingController.Instance != null && BuildingController.Instance.IsPlacing) {
                    holdingButtonFire = false;
                } else if (equippedWeapon != null && equippedWeapon.gameObject.activeInHierarchy && CanPlayAnimationFire() && equippedWeapon.HasAmmunition() && equippedWeapon.IsAutomatic()) {
                    if (Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
                        Fire();
                }
            }

            UpdateAnimator();
        }

        protected override void LateUpdate() {
            if (equippedWeapon == null || equippedWeaponScope == null || characterKinematics == null)
                return;

            if (!equippedWeapon.gameObject.activeInHierarchy && inventory.GetEquippedItem() != (ItemBehaviour)equippedWeapon) {
                return;
            }

            characterKinematics.Compute();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Updates all the animator properties for this frame.
        /// </summary>
        private void UpdateAnimator() {
            characterAnimator.SetFloat(HashMovement, Mathf.Clamp01(Mathf.Abs(axisMovement.x) + Mathf.Abs(axisMovement.y)), dampTimeLocomotion, Time.deltaTime);

            characterAnimator.SetFloat(HashAimingAlpha, Convert.ToSingle(aiming), 0.25f / 1.0f * dampTimeAiming, Time.deltaTime);

            const string boolNameAim = "Aim";
            characterAnimator.SetBool(boolNameAim, aiming);

            const string boolNameRun = "Running";
            characterAnimator.SetBool(boolNameRun, running);
        }

        /// <summary>
        /// Plays the inspect animation.
        /// </summary>
        private void Inspect() {
            inspecting = true;
            characterAnimator.CrossFade("Inspect", 0.0f, layerActions, 0);
        }

        /// <summary>
        /// Fires the character's weapon.
        /// </summary>
        private void Fire() {
            lastShotTime = Time.time;
            equippedWeapon.Fire();

            const string stateName = "Fire";
            characterAnimator.CrossFade(stateName, 0.05f, layerOverlay, 0);
        }

        /// <summary>
        /// Plays the appropriate reload animation based on ammunition state.
        /// </summary>
        private void PlayReloadAnimation() {
            string stateName = equippedWeapon.HasAmmunition() ? "Reload" : "Reload Empty";

            characterAnimator.Play(stateName, layerActions, 0.0f);

            reloading = true;

            equippedWeapon.Reload();
        }

        /// <summary>
        /// Public method to request a weapon equip by index.
        /// This is called by external systems like ItemSelector.
        /// Validates that the weapon change is allowed before starting the equip coroutine.
        /// </summary>
        /// <param name="weaponIndex">Index of the weapon to equip in the inventory array</param>
        /// <returns>True if the equip was started, false if blocked</returns>
        public bool TryEquipWeapon(int weaponIndex) {
            return TryEquipItem(weaponIndex);
        }

        /// <summary>
        /// Tries to equip an item by its index in the inventory.
        /// Handles the validation and starts the smooth transition coroutine.
        /// </summary>
        public bool TryEquipItem(int index) {
            if (inventory == null) return false;

            if (inventory is Inventory inv && inv.GetSelectionIndex() == index && !holstered)
                return false;

            if (!CanChangeWeapon())
                return false;

            StartCoroutine(nameof(EquipItemCoroutine), index);
            return true;
        }

        /// <summary>
        /// Coroutine that handles the smooth transition between any two items.
        /// 1. Plays holster animation for current item and waits.
        /// 2. Swaps the item in inventory (logic + visual).
        /// 3. Plays unholster animation for the new item.
        /// </summary>
        private IEnumerator EquipItemCoroutine(int index) {
            if (!holstered) {
                SetHolstered(holstering = true);
                yield return new WaitUntil(() => holstering == false);
            }

            if (inventory is Inventory inv) {
                inv.SelectItem(index);
            }

            RefreshWeaponSetup();

            ItemBehaviour currentItem = inventory?.GetEquippedItem();
            if (currentItem != null && currentItem.KeepHolsteredOnEquip())
            {
                SetHolstered(true);
                yield break;
            }

            SetHolstered(false);

            if (characterAnimator != null) {
                float holsterWeight = characterAnimator.GetLayerWeight(layerHolster);
                if (holsterWeight < 0.01f) {
                    Debug.LogWarning($"[Character] Holster layer weight is ZERO! Force setting to 1 to show item.");
                    characterAnimator.SetLayerWeight(layerHolster, 1.0f);
                }

                characterAnimator.Play("Unholster", layerHolster, 0);
            }
        }

        /// <summary>
        /// Attempts to restore the last used weapon smoothly.
        /// Useful for when an item like a medkit or grenade is exhausted.
        /// </summary>
        public void TryRestoreWeaponSmoothly() {
            if (!CanChangeWeapon())
                return;

            StartCoroutine(nameof(RestoreWeaponCoroutine));
        }

        /// <summary>
        /// Coroutine that handles the smooth transition back to the last weapon.
        /// </summary>
        private IEnumerator RestoreWeaponCoroutine() {
            if (!holstered) {
                SetHolstered(holstering = true);
                yield return new WaitUntil(() => holstering == false);
            }

            if (inventory is Inventory inv) {
                inv.RestoreLastWeapon();
            }

            RefreshWeaponSetup();

            SetHolstered(false);
            characterAnimator.Play("Unholster", layerHolster, 0);
        }

        #region MELEE

        /// <summary>
        /// Starts a melee attack. Stores current weapon index and holsters the weapon.
        /// </summary>
        public bool StartMeleeAttack() {
            if (isAttackingMelee)
                return false;

            if (!CanPlayAnimationHolster())
                return false;

            int currentIndex = (inventory is Inventory inv) ? inv.GetSelectionIndex() : inventory.GetEquippedIndex();
            lastWeaponIndexBeforeMelee = currentIndex >= 0 ? currentIndex : 0;
            isAttackingMelee = true;
            SetHolstered(true);
            holstering = true;
            return true;
        }

        /// <summary>
        /// Ends the melee attack and restores the previous weapon.
        /// Called by Animation Event when melee animation ends.
        /// </summary>
        public void EndMeleeAttack() {
            if (!isAttackingMelee)
                return;

            isAttackingMelee = false;

            holstering = false;

            if (lastWeaponIndexBeforeMelee >= 0) {
                TryEquipItem(lastWeaponIndexBeforeMelee);
            }
        }

        /// <summary>
        /// Gets the weapon index to restore after melee attack.
        /// </summary>
        public int GetLastWeaponIndexBeforeMelee() => lastWeaponIndexBeforeMelee;

        #endregion

        /// <summary>
        /// Refresh all weapon things to make sure we're all set up!
        /// </summary>
        public void RefreshWeaponSetup() {
            equippedWeapon = inventory.GetEquipped();

            if (equippedWeapon != null && characterAnimator != null) {
                RuntimeAnimatorController newController = equippedWeapon.GetAnimatorController();

                if (characterAnimator.runtimeAnimatorController != newController) {
                    characterAnimator.runtimeAnimatorController = newController;
                }

                weaponAttachmentManager = equippedWeapon.GetAttachmentManager();
                if (weaponAttachmentManager != null) {
                    equippedWeaponScope = weaponAttachmentManager.GetEquippedScope();
                    equippedWeaponMagazine = weaponAttachmentManager.GetEquippedMagazine();
                }
            } else if (characterAnimator != null) {
                if (initialRuntimeController != null && characterAnimator.runtimeAnimatorController != initialRuntimeController)
                    characterAnimator.runtimeAnimatorController = initialRuntimeController;

                weaponAttachmentManager = null;
                equippedWeaponScope = null;
                equippedWeaponMagazine = null;
            }

            if (characterAnimator != null) {
                layerHolster = characterAnimator.GetLayerIndex("Layer Holster");
                layerActions = characterAnimator.GetLayerIndex("Layer Actions");
                layerOverlay = characterAnimator.GetLayerIndex("Layer Overlay");

                if (layerHolster == -1) layerHolster = 4;
                if (layerActions == -1) layerActions = 3;
                if (layerOverlay == -1) layerOverlay = 2;
            }
        }

        /// <summary>
        /// Plays the fire-empty animation when the weapon has no ammunition.
        /// </summary>
        private void FireEmpty() {
            lastShotTime = Time.time;

            characterAnimator.CrossFade("Fire Empty", 0.05f, layerOverlay, 0);
        }

        /// <summary>
        /// Updates the cursor state based on the value of the cursorLocked variable.
        /// </summary>
        private void UpdateCursorState() {
            Cursor.visible = !cursorLocked;
            Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
        }

        /// <summary>
        /// Updates the "Holstered" variable, along with the Character's Animator value.
        /// </summary>
        public override void SetHolstered(bool value = true) {
            holstered = value;

            const string boolName = "Holstered";
            characterAnimator.SetBool(boolName, holstered);
        }

        #region ACTION CHECKS

        /// <summary>
        /// Can Fire.
        /// </summary>
        private bool CanPlayAnimationFire() {
            if (equippedWeapon == null || !equippedWeapon.gameObject.activeInHierarchy)
                return false;

            if (holstered || holstering)
                return false;

            if (reloading)
                return false;

            if (inspecting)
                return false;

            if (isAttackingMelee)
                return false;

            return true;
        }

        /// <summary>
        /// Determines if we can play the reload animation.
        /// </summary>
        private bool CanPlayAnimationReload() {

            if (reloading)
                return false;

            if (inspecting)
                return false;

            if (isAttackingMelee)
                return false;

            return true;
        }

        /// <summary>
        /// Returns true if the character is able to holster their weapon.
        /// </summary>
        private bool CanPlayAnimationHolster() {

            if (reloading)
                return false;

            if (inspecting)
                return false;

            if (isAttackingMelee)
                return false;

            if (holstered)
                return false;

            if (holstering)
                return false;

            if (aiming)
                return false;

            if (holdingButtonFire)
                return false;

            return true;
        }

        /// <summary>
        /// Returns true if the Character can change their Weapon.
        /// </summary>
        private bool CanChangeWeapon() {
            if (holstering && !holstered)
                return false;

            if (reloading)
                return false;

            if (inspecting)
                return false;

            if (isAttackingMelee)
                return false;

            return true;
        }

        /// <summary>
        /// Returns true if the Character can play the Inspect animation.
        /// </summary>
        private bool CanPlayAnimationInspect() {

            if (holstered || holstering)
                return false;

            if (reloading)
                return false;

            if (inspecting)
                return false;

            return true;
        }

        /// <summary>
        /// Returns true if the Character can Aim.
        /// </summary>
        private bool CanAim() {

            if (holstered || inspecting)
                return false;

            if (reloading || holstering)
                return false;

            if (isAttackingMelee)
                return false;

            return true;
        }

        /// <summary>
        /// Returns true if the character can run.
        /// </summary>
        private bool CanRun() {

            if (inspecting)
                return false;

            if (aiming)
                return false;

            if (holdingButtonFire && (equippedWeapon != null && equippedWeapon.HasAmmunition()))
                return false;

            if (axisMovement.y <= 0 || Math.Abs(Mathf.Abs(axisMovement.x) - 1) < 0.01f)
                return false;

            return true;
        }

        #endregion

        #region INPUT

        /// <summary>
        /// Fire.
        /// </summary>
        public void OnTryFire(InputAction.CallbackContext context) {
            if (!cursorLocked || interfaceMode)
                return;

            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            if (BuildingController.Instance != null && BuildingController.Instance.IsPlacing) {
                holdingButtonFire = false;
                return;
            }

            switch (context) {

                case { phase: InputActionPhase.Started }:
                    holdingButtonFire = true;
                    break;
                case { phase: InputActionPhase.Performed }:
                    ItemBehaviour currentItem = inventory?.GetEquippedItem();
                    if (currentItem != null && !(currentItem is WeaponBehaviour)) {
                        inventory.TryUseEquippedItem();
                        break;
                    }

                    if (!CanPlayAnimationFire())
                        break;

                    if (equippedWeapon == null)
                        break;

                    if (equippedWeapon.HasAmmunition()) {
                        if (equippedWeapon.IsAutomatic())
                            break;

                        if (Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
                            Fire();
                    } else
                        FireEmpty();
                    break;
                case { phase: InputActionPhase.Canceled }:
                    holdingButtonFire = false;
                    break;
            }
        }

        /// <summary>
        /// Reload.
        /// </summary>
        public void OnTryPlayReload(InputAction.CallbackContext context) {

            if (!cursorLocked)
                return;

            if (!CanPlayAnimationReload())
                return;

            switch (context) {

                case { phase: InputActionPhase.Performed }:
                    if (equippedWeapon == null)
                        return;

                    if (equippedWeapon.IsFull())
                        return;

                    if (PlayerProgress.Instance != null) {
                        string id = equippedWeapon.GetItemID();
                        int total = PlayerProgress.Instance.GetItemTotal(id);

                        if (total <= 0) {
                            AudioClip clip = equippedWeapon.GetAudioClipFireEmpty();
                            if (clip != null) {
                                audioService?.PlaySFX2D(clip, 1f);
                            }
                            return;
                        }
                    }

                    PlayReloadAnimation();
                    break;
            }
        }

        /// <summary>
        /// Inspect.
        /// </summary>
        public void OnTryInspect(InputAction.CallbackContext context) {

            if (!cursorLocked)
                return;

            if (!CanPlayAnimationInspect())
                return;

            switch (context) {
                case { phase: InputActionPhase.Performed }:
                    Inspect();
                    break;
            }
        }
        /// <summary>
        /// Aiming.
        /// </summary>
        public void OnTryAiming(InputAction.CallbackContext context) {

            if (!cursorLocked || interfaceMode)
                return;

            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            switch (context.phase) {
                case InputActionPhase.Started:
                    holdingButtonAim = true;
                    break;
                case InputActionPhase.Canceled:
                    holdingButtonAim = false;
                    break;
            }
        }

        /// <summary>
        /// Holster.
        /// </summary>
        public void OnTryHolster(InputAction.CallbackContext context) {

            if (!cursorLocked)
                return;

            switch (context.phase) {
                case InputActionPhase.Performed:
                    if (CanPlayAnimationHolster()) {
                        SetHolstered(!holstered);
                        holstering = true;
                    }
                    break;
            }
        }
        /// <summary>
        /// Run.
        /// </summary>
        public void OnTryRun(InputAction.CallbackContext context) {
            if (!cursorLocked)
                return;

            switch (context.phase) {
                case InputActionPhase.Started:
                    holdingButtonRun = true;
                    break;
                case InputActionPhase.Canceled:
                    holdingButtonRun = false;
                    break;
            }
        }
        /// <summary>
        /// Jump.
        /// </summary>
        public void OnTryJump(InputAction.CallbackContext context) {
            if (!cursorLocked || interfaceMode)
                return;

            switch (context.phase) {
                case InputActionPhase.Started:
                    holdingButtonJump = true;
                    break;

                case InputActionPhase.Canceled:
                    holdingButtonJump = false;
                    break;
            }
        }

        /// <summary>
        /// Crouch.
        /// </summary>
        public void OnTryCrouch(InputAction.CallbackContext context) {
            if (!cursorLocked || interfaceMode)
                return;

            switch (context.phase) {
                case InputActionPhase.Started:
                    holdingButtonCrouch = true;
                    break;

                case InputActionPhase.Canceled:
                    holdingButtonCrouch = false;
                    break;
            }
        }

        /// <summary>
        /// Callback for unified item selection via numeric keys (1-9).
        /// Delegates to Inventory to handle both weapons and buildables.
        /// </summary>
        public void OnSelectItem(InputAction.CallbackContext context) {
            if (context.phase != InputActionPhase.Performed)
                return;

            if (cursorLocked == false || interfaceMode)
                return;

            if (inventory is Inventory inventoryScript) {
                int index = inventoryScript.GetIndexFromInput(context);

                if (index != -1) {
                    TryEquipItem(index);
                }
            }
        }

        /// <summary>
        /// Toggles cursor lock state.
        /// </summary>
        public void OnLockCursor(InputAction.CallbackContext context) {

            switch (context) {
                case { phase: InputActionPhase.Performed }:
                    cursorLocked = !cursorLocked;
                    UpdateCursorState();
                    break;
            }
        }

        /// <summary>
        /// Movement.
        /// </summary>
        public void OnMove(InputAction.CallbackContext context) {
            axisMovement = (cursorLocked && !interfaceMode) ? context.ReadValue<Vector2>() : default;
        }
        /// <summary>
        /// Look.
        /// </summary>
        public void OnLook(InputAction.CallbackContext context) {
            axisLook = (cursorLocked && !interfaceMode) ? context.ReadValue<Vector2>() : default;
        }

        /// <summary>
        /// Called in order to update the tutorial text value.
        /// </summary>
        public void OnUpdateTutorial(InputAction.CallbackContext context) {

            tutorialTextVisible = context switch {
                { phase: InputActionPhase.Started } => true,
                { phase: InputActionPhase.Canceled } => false,
                _ => tutorialTextVisible
            };
        }

        #endregion

        #region ANIMATION EVENTS

        public override void EjectCasing() {
            if (equippedWeapon != null)
                equippedWeapon.EjectCasing();
        }
        public override void FillAmmunition(int amount) {
            if (equippedWeapon != null)
                equippedWeapon.FillAmmunition(amount);
        }

        public override void SetActiveMagazine(int active) {
            equippedWeaponMagazine.gameObject.SetActive(active != 0);
        }

        public override void AnimationEndedReload() {
            reloading = false;
        }

        public override void AnimationEndedInspect() {
            inspecting = false;
        }
        public override void AnimationEndedHolster() {
            holstering = false;
        }

        #endregion

        #endregion
    }
}
