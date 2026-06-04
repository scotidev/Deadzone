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
        private RuntimeAnimatorController initialRuntimeController; // CONCEITO: Cache do controller original
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

            // CONCEITO: Armazenar o controller inicial para usar como base para braços 
            // quando não estivermos segurando uma arma de fogo específica.
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
                    // CONCEITO: Adicionado check de activeInHierarchy. Impede que o loop de tiro automático
                    // tente disparar uma arma que acabou de ser desativada durante uma troca de item.
                    if (Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
                        Fire();
                }
            }

            UpdateAnimator();
        }

        protected override void LateUpdate() {
            if (equippedWeapon == null || equippedWeaponScope == null || characterKinematics == null)
                return;

            // FIX: Se a arma que fornece os alvos de IK (equippedWeapon) está desativada (ex: trocamos p/ granada),
            // ignoramos o cálculo para evitar que os braços colapsem para o centro do personagem.
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

        private void PlayReloadAnimation() {
            string stateName = equippedWeapon.HasAmmunition() ? "Reload" : "Reload Empty";
            Debug.Log($"[Character] PlayReloadAnimation: stateName={stateName}, equippedWeapon={equippedWeapon}, weaponID={equippedWeapon?.GetItemID()}");

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
            // CONCEITO: Redireciona para o novo sistema unificado de troca suave.
            // Isso garante que trocas via código (como a pistola inicial) também sejam suaves.
            return TryEquipItem(weaponIndex);
        }

        /// <summary>
        /// Tries to equip an item by its index in the inventory.
        /// Handles the validation and starts the smooth transition coroutine.
        /// </summary>
        public bool TryEquipItem(int index) {
            if (inventory == null) return false;

            // FIX: Allow equipping the same item if we are currently holstered (e.g. after a melee attack).
            // This ensures that even if the inventory already has this item selected, we still trigger 
            // the unholster animation to bring it back to the player's hands.
            if (inventory is Inventory inv && inv.GetSelectionIndex() == index && !holstered)
                return false;

            if (!CanChangeWeapon())
                return false;

            StartCoroutine(nameof(EquipItemCoroutine), index);
            return true;
        }

        /// <summary>
        /// Coroutine that handles the smooth transition between ANY two items.
        /// 1. Plays holster animation for current item and waits.
        /// 2. Swaps the item in inventory (logic + visual).
        /// 3. Plays unholster animation for the new item.
        /// </summary>
        private IEnumerator EquipItemCoroutine(int index) {
            // Se não estiver guardado, precisamos guardar primeiro (tocar animação de holster)
            if (!holstered) {
                SetHolstered(holstering = true);
                // Espera até que o evento de animação 'AnimationEndedHolster' seja disparado
                yield return new WaitUntil(() => holstering == false);
            }

            // Troca o item no inventário enquanto a mão está em baixo
            if (inventory is Inventory inv) {
                inv.SelectItem(index);
            }

            // Atualiza referências de animação/componentes
            RefreshWeaponSetup();

            // Tira do holster para tocar a animação de sacar o novo item
            SetHolstered(false);
            
            if (characterAnimator != null) {
                // DIAGNÓSTICO DE ANIMAÇÃO:
                float holsterWeight = characterAnimator.GetLayerWeight(layerHolster);
                Debug.Log($"[Character] Playing Unholster: Layer={layerHolster}, Weight={holsterWeight}, Controller={characterAnimator.runtimeAnimatorController.name}");
                
                // Se o peso da camada de Holster estiver em 0, a animação nunca aparecerá!
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
            // Se não estiver guardado, precisamos guardar primeiro
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
        public void StartMeleeAttack() {
            if (isAttackingMelee)
                return;

            if (!CanPlayAnimationHolster())
                return;

            // FIX: Use the selection index (which includes medkits, buildables, etc.) instead of 
            // just the equipped weapon index. This ensures the correct item is restored after the attack.
            int currentIndex = (inventory is Inventory inv) ? inv.GetSelectionIndex() : inventory.GetEquippedIndex();
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

            // FIX: Reset holstering flag. If we were holstering specifically for the melee attack, 
            // we are done now. This prevents the character from being stuck in a "holstering" state 
            // if the animation event was missed or interrupted by a fire input.
            holstering = false;

            if (lastWeaponIndexBeforeMelee >= 0) {
                // Tenta restaurar a arma anterior de forma suave
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
                // CONCEITO: Se não houver arma (segurando granada/medkit), voltamos para o
                // controller inicial (neutro) e limpamos referências de acessórios.
                if (initialRuntimeController != null && characterAnimator.runtimeAnimatorController != initialRuntimeController)
                    characterAnimator.runtimeAnimatorController = initialRuntimeController;

                weaponAttachmentManager = null;
                equippedWeaponScope = null;
                equippedWeaponMagazine = null;
            }

            if (characterAnimator != null) {
                // CONCEITO: RE-CACHE de layers é OBRIGATÓRIO aqui. 
                // Se não re-cacharmos, o Character tentará tocar animações em índices de layers 
                // da arma anterior, o que causa o bug de "mão vazia" ou braços invisíveis.
                layerHolster = characterAnimator.GetLayerIndex("Layer Holster");
                layerActions = characterAnimator.GetLayerIndex("Layer Actions");
                layerOverlay = characterAnimator.GetLayerIndex("Layer Overlay");

                // SEGURANÇA: Fallback para os índices padrão do asset caso os nomes dos layers mudem.
                if (layerHolster == -1) layerHolster = 4;
                if (layerActions == -1) layerActions = 3;
                if (layerOverlay == -1) layerOverlay = 2;
            }
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
        }

        #region ACTION CHECKS

        /// <summary>
        /// Can Fire.
        /// </summary>
        private bool CanPlayAnimationFire() {
            // CONCEITO: Verificação de segurança - se não há arma ou se ela está inativa (ex: trocando p/ granada),
            // não permitimos que a lógica de disparo ou animação de fogo prossiga.
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

            return true;
        }

        /// <summary>
        /// Returns true if the Character can change their Weapon.
        /// </summary>
        private bool CanChangeWeapon() {
            // FIX: If we are already holstered, we can change the weapon logically in the inventory
            // even if the "holstering" flag is true (which might be a leftover from an interrupted animation).
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
            // SEGURANÇA: Não atirar/usar se o cursor estiver solto, em modo de interface
            // ou se o clique foi em cima de um elemento da UI (botão de menu, shop, etc).
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
                    // CHECK: If currently equipped item is NOT a weapon (medkit, grenade, etc),
                    // delegate to inventory to handle OnUse() instead of trying to fire.
                    // Must check BEFORE CanPlayAnimationFire(), since that method requires
                    // equippedWeapon != null which is false when a non-weapon item is selected.
                    ItemBehaviour currentItem = inventory?.GetEquippedItem();
                    if (currentItem != null && !(currentItem is WeaponBehaviour)) {
                        inventory.TryUseEquippedItem();
                        break;
                    }

                    if (!CanPlayAnimationFire())
                        break;

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
                // Obtém o índice desejado a partir da tecla pressionada
                int index = inventoryScript.GetIndexFromInput(context);
                
                // Tenta equipar o item de forma suave
                if (index != -1) {
                    TryEquipItem(index);
                }
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
