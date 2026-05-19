// Copyright 2021, Infima Games. All Rights Reserved.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// REFATORAÇÃO: remover logica de inspect weapon
// REFATORAÇÃO: pq aqui lidamos com  [SerializeField] private int startingWeaponIndex = 0;? Isso nao deveria estar na logica de dentro do inventario?

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Main Character Component. This component handles the most important functions of the character, and interfaces
    /// with basically every part of the asset, it is the hub where it all converges.
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
        private WeaponAttachmentManagerBehaviour weaponAttachmentManager;
        private ScopeBehaviour equippedWeaponScope;
        private MagazineBehaviour equippedWeaponMagazine;
        private Vector2 axisLook;
        private Vector2 axisMovement;
        private bool tutorialTextVisible;

        #endregion

        #region PROPERTIES

        public override bool IsInterfaceMode() => interfaceMode;

        /// <summary>
        /// When activated ( value = true), it disables cursor locking (making it visible) and updates the cursor state in Unity.
        /// </summary>
        public override void SetInterfaceMode(bool value) {
            Debug.Log($"[Character] SetInterfaceMode({value}) chamado de:\n{new System.Diagnostics.StackTrace(true)}");
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
                } else if (equippedWeapon != null && CanPlayAnimationFire() && equippedWeapon.HasAmmunition() && equippedWeapon.IsAutomatic()) {
                    // CONCEITO: equippedWeapon null check added. Prevents crash if weapon not selected yet.
                    if (Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
                        Fire();
                }
            }

            UpdateAnimator();
        }

        protected override void LateUpdate() {
            if (equippedWeapon == null)
                return;

            if (equippedWeaponScope == null)
                return;

            if (characterKinematics != null) {
                characterKinematics.Compute();
            }
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

        private void PlayReloadAnimation() {
            string stateName = equippedWeapon.HasAmmunition() ? "Reload" : "Reload Empty";
            Debug.Log($"[Character] PlayReloadAnimation: stateName={stateName}, equippedWeapon={equippedWeapon}, weaponID={equippedWeapon?.GetItemID()}");

            characterAnimator.Play(stateName, layerActions, 0.0f);

            reloading = true;

            equippedWeapon.Reload();
        }

        /// <summary>
        /// Equip Weapon Coroutine.
        /// </summary>
        private IEnumerator Equip(int index = 0) {
            if (!holstered) {
                SetHolstered(holstering = true);
                yield return new WaitUntil(() => holstering == false);
            }

            SetHolstered(false);

            characterAnimator.Play("Unholster", layerHolster, 0);

            inventory.Equip(index);

            RefreshWeaponSetup();
        }

        #region MELEE

        /// <summary>
        /// Starts a melee attack. Stores current weapon index and holsters the weapon.
        /// </summary>
        public void StartMeleeAttack() {
            if (isAttackingMelee)
                return;

            if (!CanPlayAnimationHolster())
                return;

            int currentIndex = inventory.GetEquippedIndex();
            lastWeaponIndexBeforeMelee = currentIndex >= 0 ? currentIndex : 0;
            isAttackingMelee = true;
            SetHolstered(true);
            holstering = true;
        }

        /// <summary>
        /// Ends the melee attack and restores the previous weapon.
        /// Called by Animation Event when melee animation ends.
        /// </summary>
        public void EndMeleeAttack() {
            if (!isAttackingMelee)
                return;

            isAttackingMelee = false;

            if (lastWeaponIndexBeforeMelee >= 0) {
                SetHolstered(false);
                StartCoroutine(RestoreWeaponAfterUnholster());
            }
        }

        /// <summary>
        /// Waits for unholster animation to finish, then restores the weapon.
        /// </summary>
        private IEnumerator RestoreWeaponAfterUnholster() {
            yield return new WaitUntil(() => !holstering);

            inventory.Equip(lastWeaponIndexBeforeMelee);
            RefreshWeaponSetup();
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
            if (equippedWeapon == null)
                return;

            characterAnimator.runtimeAnimatorController = equippedWeapon.GetAnimatorController();

            weaponAttachmentManager = equippedWeapon.GetAttachmentManager();
            if (weaponAttachmentManager == null)
                return;

            equippedWeaponScope = weaponAttachmentManager.GetEquippedScope();
            equippedWeaponMagazine = weaponAttachmentManager.GetEquippedMagazine();
        }

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

            // LOG: report holster events and currently equipped item
            Inventory inv = inventory as Inventory;
            int eqIndex = inv != null ? inv.GetEquippedIndex() : -1;
            string eqID = inv != null && inv.GetEquippedItem() != null ? inv.GetEquippedItem().GetItemID() : "null";
            Debug.Log($"[Character] SetHolstered({value}) called. equippedIndex={eqIndex}, equippedItemID={eqID}");
        }

        #region ACTION CHECKS

        /// <summary>
        /// Can Fire.
        /// </summary>
        private bool CanPlayAnimationFire() {

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

            return true;
        }

        /// <summary>
        /// Returns true if the Character can change their Weapon.
        /// </summary>
        private bool CanChangeWeapon() {

            if (holstering)
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

            if (holdingButtonFire && equippedWeapon.HasAmmunition())
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

            if (BuildingController.Instance != null && BuildingController.Instance.IsPlacing) {
                holdingButtonFire = false;
                return;
            }

            switch (context) {

                case { phase: InputActionPhase.Started }:
                    holdingButtonFire = true;
                    break;
                case { phase: InputActionPhase.Performed }:
                    if (!CanPlayAnimationFire())
                        break;

                    // CHECK: If currently equipped item is NOT a weapon (medkit, grenade, etc),
                    // delegate to inventory to handle OnUse() instead of trying to fire.
                    ItemBehaviour currentItem = inventory?.GetEquippedItem();
                    if (currentItem != null && !(currentItem is WeaponBehaviour)) {
                        inventory.TryUseEquippedItem();
                        break;
                    }

                    // CONCEITO: Early safety check. If no weapon equipped, don't try to fire.
                    // This prevents NullReferenceException if equippedWeapon is null.
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
                    // SEGURANÇA: Verifica se há arma equipada antes de prosseguir.
                    if (equippedWeapon == null)
                        return;

                    // FIX: Verifica se o pente já está cheio — não há nada para recarregar.
                    if (equippedWeapon.IsFull())
                        return;

                    // FIX: Verifica se há munição na reserva ANTES de tocar a animação de reload.
                    // Se não houver munição na reserva e o pente não está cheio, toca um som
                    // de feedback (empty click) e retorna sem animação.
                    if (PlayerProgress.Instance != null) {
                        string id = equippedWeapon.GetItemID();
                        int total = PlayerProgress.Instance.GetItemTotal(id);
                        int localAmmo = equippedWeapon.GetAmmunitionCurrent();

                        Debug.Log($"[Character] OnTryPlayReload: weaponID={id}, localAmmo={localAmmo}, PP_total={total}");

                        if (total <= 0) {
                            Debug.Log($"[Character] OnTryPlayReload: no reserve ammo — playing empty click.");
                            AudioClip clip = equippedWeapon.GetAudioClipFireEmpty();
                            if (clip != null) {
                                AudioSource.PlayClipAtPoint(clip, transform.position);
                            }
                            return;
                        }
                    }

                    // Todas as verificações passaram — pode tocar a animação de reload.
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

            if (!cursorLocked)
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
        /// Public method to request a weapon equip by index.
        /// This is called by external systems like ItemSelector.
        /// Validates that the weapon change is allowed before starting the equip coroutine.
        /// </summary>
        /// <param name="weaponIndex">Index of the weapon to equip in the inventory array</param>
        /// <returns>True if the equip was started, false if blocked</returns>
        public bool TryEquipWeapon(int weaponIndex) {
            int currentIndex = inventory.GetEquippedIndex();

            if (currentIndex == weaponIndex)
                return false;

            if (!CanChangeWeapon())
                return false;

            StartCoroutine(nameof(Equip), weaponIndex);
            return true;
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
        /// 1. Plays holster animation for current item.
        /// 2. Waits for the animation to finish.
        /// 3. Swaps the item in inventory.
        /// 4. Plays unholster animation for the new weapon.
        /// </summary>
        private IEnumerator RestoreWeaponCoroutine() {
            // Se não estiver guardado, precisamos guardar primeiro (tocar animação de holster)
            if (!holstered) {
                SetHolstered(holstering = true);
                // Espera até que o evento de animação 'AnimationEndedHolster' seja disparado
                yield return new WaitUntil(() => holstering == false);
            }

            // Agora que a mão está "vazia" (embaixo), trocamos o item logicamente
            if (inventory is Inventory inv) {
                inv.RestoreLastWeapon();
            }

            // Atualiza as referências de animação e componentes para a nova arma (pistola)
            RefreshWeaponSetup();

            // Tira do holster para tocar a animação de sacar a arma
            SetHolstered(false);
            characterAnimator.Play("Unholster", layerHolster, 0);
        }

        /// <summary>
        /// Callback for unified item selection via numeric keys (1-9).
        /// Delegates to Inventory to handle both weapons and buildables.
        /// </summary>
        public void OnSelectItem(InputAction.CallbackContext context) {
            if (context.phase != InputActionPhase.Performed)
                return;

            if (inventory is Inventory inventoryScript) {
                inventoryScript.OnSelectItem(context);
            }
        }

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