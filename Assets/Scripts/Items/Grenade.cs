using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;

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
            Debug.Log($"[Grenade] Awake: audioService obtained: {(audioService != null ? "SUCCESS ✓" : "NULL ✗")}");
            if (audioService == null) {
                Debug.LogError("[Grenade] CRITICAL: audioService is null at Awake time!");
            }
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
            // CONCEITO: Verificar se tem ammo ANTES de equipar.
            // CanBeUsed() já valida isso, então isto é apenas precaução.
            if (PlayerProgress.Instance != null) {
                string id = GetItemID();
                int total = PlayerProgress.Instance.GetItemTotal(id);
                
                // Se sem ammo e conseguiu chegar aqui (bug), não ativar
                if (total <= 0) {
                    Debug.Log("[Grenade] OnSelected: Out of ammo! Not activating.");
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
                Debug.LogWarning($"[Grenade] CanBeUsed: PlayerProgress.Instance is NULL!");
                return false;
            }

            // CONCEITO: CanBeUsed() verifica se o item está desbloqueado E tem munição.
            // Inventory.SelectItem() usa isso para decidir se permite equipar.
            bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(GetItemID());
            int totalAmmo = PlayerProgress.Instance.GetItemTotal(GetItemID());
            
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
            Debug.Log("[Grenade] OnFireStarted called");
            Debug.Log($"  currentState: {currentState}");
            Debug.Log($"  audioService before check: {(audioService != null ? "Valid ✓" : "NULL ✗")}");
            
            // CONCEITO: Só proceder se estamos no estado Idle.
            // Se já estiver Pinned ou Thrown, ignorar.
            if (currentState != GrenadeState.Idle) {
                Debug.Log("[Grenade] OnFireStarted: Ignoring (not in Idle state)");
                return;
            }

            // CONCEITO: Re-cache audioService se ficar null (pode ser destruído entre Awake e agora).
            // Isso evita MissingReferenceException ao tentar chamar PlayPinPullSound.
            if (audioService == null) {
                Debug.LogWarning("[Grenade] OnFireStarted: audioService is null, attempting re-cache...");
                audioService = ServiceLocator.Current.Get<IAudioManagerService>();
                Debug.Log($"  Re-cache result: {(audioService != null ? "SUCCESS ✓" : "FAILED ✗")}");
                if (audioService == null) {
                    Debug.LogError("[Grenade] CRITICAL: Re-cache failed! audioService still null!");
                    return;
                }
            }

            // CONCEITO: Transição de estado: Idle → Pinned
            // O estado muda ANTES de tocar som para evitar race conditions
            currentState = GrenadeState.Pinned;
            
            PlayPinPullSound();
            Debug.Log("[Grenade] Pin pulled - ready to throw!");
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
                currentState = GrenadeState.Idle; // Volta ao estado Idle para poder lançar novamente
                SubscribeToFireInput(); // Re-inscrever para o próximo lançamento
                Debug.Log("[Grenade] Ready to throw again! (callbacks re-subscribed)");
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
                PlayerProgress.Instance.SetItemCurrent(GetItemID(), remaining > 0 ? 1 : 0);
                Debug.Log($"[Grenade] Thrown! Remaining grenades: {remaining}");
            }

            // CONCEITO: NÃO desativar mais o gameObject aqui.
            // A mão do player (Grenade.cs) continua ativa para relançar se houver mais ammo.
            // Se não houver mais ammo, o estado já trata disso no próximo lançamento.
        }

        #endregion

        #region AUDIO

        private void PlayEquipSound() {
            Debug.Log("[Grenade] PlayEquipSound called");
            if (equipClip != null && audioService != null) {
                audioService.PlaySFX2D(equipClip, equipVolume);
                Debug.Log("[Grenade] PlayEquipSound: SUCCESS");
            } else {
                Debug.LogWarning($"[Grenade] PlayEquipSound: Skipped - equipClip={equipClip}, audioService={audioService}");
            }
        }

        private void PlayPinPullSound() {
            Debug.Log("[Grenade] PlayPinPullSound called");
            Debug.Log($"  audioService: {(audioService != null ? "Valid ✓" : "NULL ✗")}");
            Debug.Log($"  pinPullClip: {pinPullClip}");
            
            if (audioService == null) {
                Debug.LogError("[Grenade] PlayPinPullSound: audioService is NULL! This will crash!");
                return;
            }
            
            if (pinPullClip != null && audioService != null) {
                audioService.PlaySFX2D(pinPullClip, pinPullVolume);
                Debug.Log("[Grenade] PlayPinPullSound: Sound played successfully");
            } else {
                Debug.LogWarning($"[Grenade] PlayPinPullSound: Skipped - pinPullClip={pinPullClip}, audioService={audioService}");
            }
        }

        private void PlayThrowSound() {
            Debug.Log("[Grenade] PlayThrowSound called");
            if (throwClip != null && audioService != null) {
                audioService.PlaySFX2D(throwClip, throwVolume);
                Debug.Log("[Grenade] PlayThrowSound: SUCCESS");
            } else {
                Debug.LogWarning($"[Grenade] PlayThrowSound: Skipped - throwClip={throwClip}, audioService={audioService}");
            }
        }

        #endregion
    }
}

