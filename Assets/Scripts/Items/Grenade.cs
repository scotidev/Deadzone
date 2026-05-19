using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;
using Deadzone.UI;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Grenade consumable item. Players hold fire button to arm, release to throw.
    /// Hold-to-charge mechanic with detonation and explosion damage.
    /// </summary>
    public class Grenade : ItemBehaviour {

        #region ENUMS

        /// <summary>
        /// CONCEITO: Enum define os estados possíveis da granada.
        /// Isso evita usar strings e permite que o compilador valide transições.
        /// Cada estado representa uma etapa diferente do ciclo de vida da granada.
        /// </summary>
        private enum GrenadeState {
            Idle,       // Granada está na mão, pronta para ser armada
            Pinned,     // Pino foi puxado, player está segurando o botão
            Thrown,     // Granada foi lançada, aguardando detonação
            Exploded    // Granada explodiu, ciclo completo
        }

        #endregion

        #region SERIALIZED FIELDS

        [SerializeField] private GrenadeDataSO grenadeData;
        [SerializeField] private Sprite hudIcon;
        [SerializeField] private GameObject grenadePrefab;
        [SerializeField] private float throwForce = 20f;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip equipClip;
        [SerializeField] private float equipVolume = 1f;
        [SerializeField] private AudioClip pinPullClip;
        [SerializeField] private float pinPullVolume = 1f;
        [SerializeField] private AudioClip throwClip;
        [SerializeField] private float throwVolume = 1f;

        #endregion

        #region FIELDS

        private IAudioManagerService audioService;
        private GrenadeState currentState = GrenadeState.Idle;
        private GameObject thrownGrenadeInstance;

        #endregion

        #region UNITY

        private void Awake() {
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        }

        #endregion

        #region ITEM BEHAVIOUR IMPLEMENTATION

        public override string GetItemID() {
            if (grenadeData == null) {
                Debug.LogWarning("[Grenade] grenadeData is null!", gameObject);
                return "grenade_null";
            }
            return grenadeData.ItemID;
        }

        public override string GetDisplayName() {
            if (grenadeData == null) return "Unknown";
            return grenadeData.ItemName;
        }

        public override Sprite GetIcon() {
            if (hudIcon == null) {
                Debug.LogWarning("[Grenade] hudIcon is null!", gameObject);
                return null;
            }
            return hudIcon;
        }

        /// <summary>
        /// Called when player selects this item (key 5).
        /// Activate visual representation (grenade model in hand).
        /// Subscribe to Fire input callbacks.
        /// </summary>
        public override void OnSelected() {
            // LOG: report selection and inventory
            string id = GetItemID();
            int total = PlayerProgress.Instance != null ? PlayerProgress.Instance.GetItemTotal(id) : -1;
            Debug.Log($"[Grenade] OnSelected: itemID={id}, total={total}");

            // CONCEITO: Verificar se tem ammo ANTES de equipar.
            // CanBeUsed() já valida isso, então isto é apenas precaução.
            if (PlayerProgress.Instance != null) {
                
                // Se sem ammo e conseguiu chegar aqui (bug), não ativar
                if (total <= 0) {
                    return;
                }
                
                PlayerProgress.Instance.SetItemCurrent(id, 1);
            }
            
            PlayEquipSound();
            gameObject.SetActive(true);
            SubscribeToFireInput();
            
            // Reset state to Idle when selected
            currentState = GrenadeState.Idle;
        }

        /// <summary>
        /// Called when player selects another item.
        /// Deactivate visual and unsubscribe from input.
        /// </summary>
        public override void OnDeselected() {
            Debug.Log($"[Grenade] OnDeselected: itemID={GetItemID()}, state={currentState}");
            // CONCEITO: Se player trocou de item enquanto puxava a granada,
            // cancelar qualquer ação em andamento (hold).
            if (currentState == GrenadeState.Pinned) {
                // CONCEITO: Rescindir a inscrição impede que callbacks façam ação
                UnsubscribeFromFireInput();
                currentState = GrenadeState.Idle;
            } else if (currentState == GrenadeState.Thrown && thrownGrenadeInstance != null) {
                // Se já lançou, deixar a corrotina de detonação continuar
                // (a instância vai explodir normalmente)
            }
            
            gameObject.SetActive(false);
            UnsubscribeFromFireInput();
        }

        /// <summary>
        /// NORMAL use: Not used directly. Grenade uses input callbacks instead.
        /// This is called by ItemBehaviour interface but grenade ignores it.
        /// </summary>
        public override void OnUse() {
            // Grenade uses InputAction callbacks (OnFireStarted/OnFireCanceled) instead
            // This method is kept for interface compatibility but does nothing
        }

        /// <summary>
        /// Check if grenade can be selected (unlocked AND has quantity available).
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                return false;
            }

            bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(GetItemID());
            int totalAmmo = PlayerProgress.Instance.GetItemTotal(GetItemID());
            if (isUnlocked && totalAmmo <= 0)
                FeedbackMessageUI.Instance?.Show();
            return isUnlocked && totalAmmo > 0;
        }

        #endregion

        #region INPUT HANDLING

        /// <summary>
        /// Subscribe to Fire input using InputSystem callbacks.
        /// CONCEITO: Em vez de depender do Character para chamar callbacks,
        /// a Granada se inscreve diretamente no Input System.
        /// Isso permite que ela controle sua própria lógica de hold/release.
        /// </summary>
        private void SubscribeToFireInput() {
            // CONCEITO: Obtemos o InputSystem atual via GetComponent.
            // Cada cena tem um PlayerInput que gerencia os bindings.
            PlayerInput playerInput = GetComponentInParent<PlayerInput>();
            if (playerInput == null) {
                Debug.LogWarning("[Grenade] PlayerInput not found in parent hierarchy!");
                return;
            }

            // CONCEITO: Procuramos a ação "Fire" no ActionMap atual.
            InputAction fireAction = playerInput.actions["Fire"];
            if (fireAction == null) {
                Debug.LogWarning("[Grenade] Fire action not found in InputActions!");
                return;
            }

            // CONCEITO: Inscrever em Started (começou a apertar) e Canceled (soltou).
            // Performed seria a cada update enquanto segura, não é ideal aqui.
            fireAction.started += OnFireStarted;
            fireAction.canceled += OnFireCanceled;
        }

        /// <summary>
        /// Unsubscribe from Fire input to prevent callbacks after deselection.
        /// </summary>
        private void UnsubscribeFromFireInput() {
            PlayerInput playerInput = GetComponentInParent<PlayerInput>();
            if (playerInput == null) return;

            InputAction fireAction = playerInput.actions["Fire"];
            if (fireAction == null) return;

            fireAction.started -= OnFireStarted;
            fireAction.canceled -= OnFireCanceled;
        }

        /// <summary>
        /// Called when fire button is pressed (InputActionPhase.Started).
        /// Pull pin and enter Pinned state.
        /// </summary>
        private void OnFireStarted(InputAction.CallbackContext context) {
            // Only proceed if in Idle state.
            if (currentState != GrenadeState.Idle) {
                return;
            }

            Debug.Log($"[Grenade] OnFireStarted: itemID={GetItemID()}, state={currentState}");

            // Ensure audioService is available.
            EnsureAudioService();

            currentState = GrenadeState.Pinned;
            PlayPinPullSound();
        }

        /// <summary>
        /// Called when fire button is released (InputActionPhase.Canceled).
        /// Throw grenade and start detonation countdown.
        /// </summary>
        private void OnFireCanceled(InputAction.CallbackContext context) {
            // CONCEITO: Só proceder se estamos no estado Pinned (segurando o botão).
            // Se não tiver puxado o pino, não fazer nada.
            if (currentState != GrenadeState.Pinned) {
                return;
            }

            // CONCEITO: Verificar se tem ammo antes de lançar.
            // Se não tiver, rescindir e retornar ao estado Idle.
            if (!CanBeUsed()) {
                currentState = GrenadeState.Idle;
                return;
            }

            // CONCEITO: Verificar se tem quantidade em inventário.
            // GetItemTotal retorna o número total de granadas que o player tem.
            if (PlayerProgress.Instance != null && PlayerProgress.Instance.GetItemTotal(GetItemID()) <= 0) {
                currentState = GrenadeState.Idle;
                return;
            }

            // CONCEITO: Transição de estado: Pinned → Thrown
            currentState = GrenadeState.Thrown;
            
            ThrowGrenade();
            PlayThrowSound();
            
            // CONCEITO: Se ainda tem ammo depois de lançar, re-inscrever callbacks de input.
            // Isso permite que o player lance de novo SEM apertar a tecla 5 novamente.
            // A visual da granada continua na mão (porque não desativamos), e os callbacks agora estão re-inscritos.
            if (PlayerProgress.Instance != null && 
                PlayerProgress.Instance.GetItemTotal(GetItemID()) > 0) {
                currentState = GrenadeState.Idle; // Back to Idle to allow another throw
                SubscribeToFireInput(); // Re-subscribe for the next throw
            }
        }

        #endregion

        #region THROW LOGIC

        /// <summary>
        /// Instantiate grenade prefab and apply initial velocity.
        /// CONCEITO: Este método instancia o GameObject do prefab e o lança.
        /// O prefab tem Rigidbody com Is Kinematic=false para sofrer gravidade.
        /// </summary>
        private void ThrowGrenade() {
            if (grenadePrefab == null) {
                Debug.LogWarning("[Grenade] grenadePrefab is null!", gameObject);
                currentState = GrenadeState.Idle;
                return;
            }

            Character character = GetComponentInParent<Character>();
            if (character == null) {
                Debug.LogWarning("[Grenade] Character not found!");
                currentState = GrenadeState.Idle;
                return;
            }

            Transform cameraTransform = character.GetCameraWorld().transform;
            if (cameraTransform == null) {
                Debug.LogWarning("[Grenade] Camera not found!");
                currentState = GrenadeState.Idle;
                return;
            }

            // CONCEITO: Instanciar o prefab na posição da câmera do player.
            // Isso faz parecer que a granada sai da mão do player em primeira pessoa.
            thrownGrenadeInstance = Instantiate(grenadePrefab, cameraTransform.position, Quaternion.identity);

            // CONCEITO: Aplicar velocidade inicial (throwForce) na direção que o player está olhando.
            // linearVelocity é preferível a AddForce quando se quer velocidade instantânea
            Rigidbody rb = thrownGrenadeInstance.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.linearVelocity = cameraTransform.forward * throwForce;
            }

            // CONCEITO: O prefab lançado tem GrenadeThrown.cs anexado automaticamente.
            // Esse script gerencia sua própria detonação de forma independente.
            // Não é necessário referenciar aqui - StartCoroutine em GrenadeThrown.cs faz tudo.

            // CONCEITO: Consumir 1 granada do inventário.
            // UseItem decrementa o total em inventário e ammo em mão.
            if (PlayerProgress.Instance != null) {
                PlayerProgress.Instance.UseItem(GetItemID(), 1);
                int remaining = PlayerProgress.Instance.GetItemTotal(GetItemID());
                Debug.Log($"[Grenade] ThrowGrenade: itemID={GetItemID()}, remainingAfterUse={remaining}");

                // NOVO: Verificar se chegou a zero
                if (remaining > 0) {
                    // Ainda há granadas: mantém equipado para lançar novamente
                    PlayerProgress.Instance.SetItemCurrent(GetItemID(), 1);
                    Debug.Log($"[Grenade] ThrowGrenade: remaining={remaining}, keep grenades equipped");
                    // The grenade hand object remains active, re-subscribed in OnFireCanceled
                } else {
                    // Última granada lançada: volta à pistola com animação
                    PlayerProgress.Instance.SetItemCurrent(GetItemID(), 0);
                    Debug.Log($"[Grenade] ThrowGrenade: used last grenade, auto-equipping weapon");
                    
                    EquipWeaponAutomatically();
                }
            }
        }

        #endregion

        #region AUDIO

        /// <summary>
        /// Ensures audioService is cached. If null, attempts to re-cache from ServiceLocator.
        /// This handles cases where AudioManagerService may have been destroyed and recreated.
        /// </summary>
        private void EnsureAudioService()
        {
            if (audioService == null)
            {
                audioService = ServiceLocator.Current.Get<IAudioManagerService>();
            }
        }

        private void PlayEquipSound() {
            EnsureAudioService();
            
            if (equipClip != null && audioService != null) {
                audioService.PlaySFX2D(equipClip, equipVolume);
            }
        }

        private void PlayPinPullSound() {
            EnsureAudioService();
            
            if (pinPullClip != null && audioService != null) {
                audioService.PlaySFX2D(pinPullClip, pinPullVolume);
            }
        }

        private void PlayThrowSound() {
            EnsureAudioService();
            
            if (throwClip != null && audioService != null) {
                audioService.PlaySFX2D(throwClip, throwVolume);
            }
        }

        #endregion

        #region HELPER METHODS

        /// <summary>
        /// Automatically equips the default weapon (pistol) when grenade quantity reaches zero.
        /// Uses smooth animation transition via Character.TryRestoreWeaponSmoothly().
        /// CONCEITO: Assim como no Medkit, delegamos a responsabilidade da animação
        /// ao Character para garantir que o braço abaixe antes da troca ocorrer.
        /// </summary>
        private void EquipWeaponAutomatically() {
            Character character = GetComponentInParent<Character>();
            if (character == null) return;

            // Chama a nova lógica suave
            character.TryRestoreWeaponSmoothly();
        }

        #endregion
    }
}

