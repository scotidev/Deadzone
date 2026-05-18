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

        [Header("Detonation")]
        [SerializeField] private float fuseTime = 3f;

        [Header("Explosion VFX")]
        [SerializeField] private Transform explosionVFXPrefab;
        [SerializeField] private float minExplosionDelay = 0.05f;
        [SerializeField] private float maxExplosionDelay = 0.25f;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip equipClip;
        [SerializeField] private float equipVolume = 1f;
        [SerializeField] private AudioClip pinPullClip;
        [SerializeField] private float pinPullVolume = 1f;
        [SerializeField] private AudioClip throwClip;
        [SerializeField] private float throwVolume = 1f;
        [SerializeField] private AudioClip explosionClip;
        [SerializeField] private float explosionVolume = 1f;

        #endregion

        #region FIELDS

        private IAudioManagerService audioService;
        private GrenadeState currentState = GrenadeState.Idle;
        private GameObject thrownGrenadeInstance;
        private Coroutine detonationCoroutine;

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
            PlayEquipSound();
            
            // CONCEITO: Quando uma granada é selecionada, definir ammo atual como 1
            // (representando 1 na mão pronto para usar). Se não houver em estoque, 0.
            if (PlayerProgress.Instance != null) {
                string id = GetItemID();
                int total = PlayerProgress.Instance.GetItemTotal(id);
                PlayerProgress.Instance.SetItemCurrent(id, total > 0 ? 1 : 0);
            }
            
            // CONCEITO: Ativar o GameObject visual da granada (model na mão do player)
            gameObject.SetActive(true);
            
            // CONCEITO: Inscrever-se nos callbacks de input para fire (hold/release)
            // Isso permite que a granada controle seu próprio comportamento sem depender do Character
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
        /// Check if grenade is unlocked for selection.
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                Debug.LogWarning($"[Grenade] CanBeUsed: PlayerProgress.Instance is NULL!");
                return false;
            }

            // CONCEITO: CanBeUsed() verifica se o item está desbloqueado.
            // Quantidade é verificada durante uso (OnFireCanceled).
            bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(GetItemID());
            return isUnlocked;
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
            // CONCEITO: Só proceder se estamos no estado Idle.
            // Se já estiver Pinned ou Thrown, ignorar.
            if (currentState != GrenadeState.Idle) {
                return;
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

            // CONCEITO: Iniciar a corrotina de detonação.
            // Enquanto isso, desativar a visualização na mão já que lançamos.
            detonationCoroutine = StartCoroutine(DetonateAfterDelay(grenadeData));
            gameObject.SetActive(false);

            // CONCEITO: Consumir 1 granAda do inventário.
            // UseItem decrementa o total em inventário e ammo em mão.
            if (PlayerProgress.Instance != null) {
                PlayerProgress.Instance.UseItem(GetItemID(), 1);
                int remaining = PlayerProgress.Instance.GetItemTotal(GetItemID());
                PlayerProgress.Instance.SetItemCurrent(GetItemID(), remaining > 0 ? 1 : 0);
            }
        }

        #endregion

        #region DETONATION LOGIC

        /// <summary>
        /// Coroutine to handle detonation with delay.
        /// CONCEITO: IEnumerator permite pausar e retomar a execução.
        /// "yield return new WaitForSeconds(time)" pausa a corrotina por 'time' segundos.
        /// </summary>
        private IEnumerator DetonateAfterDelay(GrenadeDataSO data) {
            // CONCEITO: Delay aleatório pequeno torna a explosão mais realista.
            // Grenadas não explodem instantaneamente quando batem no chão.
            float randomDelay = Random.Range(minExplosionDelay, maxExplosionDelay);
            yield return new WaitForSeconds(randomDelay);

            // CONCEITO: Esperar pelo fuse time (3 segundos por padrão).
            // Durante este tempo, a granada já está em voo.
            yield return new WaitForSeconds(fuseTime);

            // CONCEITO: Chegou a hora de explodir. Transicionar para estado Exploded.
            currentState = GrenadeState.Exploded;

            if (thrownGrenadeInstance != null) {
                yield return StartCoroutine(Explode(thrownGrenadeInstance, data));
            }
        }

        /// <summary>
        /// Explode grenade at its current position.
        /// Apply damage to enemies, physics force to rigidbodies, and spawn VFX.
        /// </summary>
        private IEnumerator Explode(GameObject grenadeObject, GrenadeDataSO data) {
            if (data == null) {
                Debug.LogWarning("[Grenade] GrenadeDataSO is null during explosion!");
                yield break;
            }

            Vector3 explosionPos = grenadeObject.transform.position;
            
            // CONCEITO: Tocar som de explosão 3D na posição da granada.
            // Som 3D significa que a fonte é posicionada no mundo, afetando volume e pan por distância.
            PlayExplosionSound(explosionPos);

            // CONCEITO: Obter o nível da granada para escalar dano e raio.
            // Level 1 = base, Level 2 = base * 1.1, Level 3 = base * 1.2, etc.
            int grenadeLevel = 1;
            if (PlayerProgress.Instance != null) {
                grenadeLevel = PlayerProgress.Instance.GetItemLevel(GetItemID());
            }

            // CONCEITO: GetDamageAtLevel e GetRadiusAtLevel aplicam scaling automaticamente.
            float damage = data.GetDamageAtLevel(grenadeLevel);
            float radius = data.GetRadiusAtLevel(grenadeLevel);

            // CONCEITO: Physics.OverlapSphere encontra todos os colisores
            // dentro de uma esfera de raio 'radius' centrada em 'explosionPos'.
            // Isso nos dá todos os objetos atingidos pela explosão.
            Collider[] colliders = Physics.OverlapSphere(explosionPos, radius);

            foreach (Collider hit in colliders) {
                // CONCEITO: Se o collider tiver Rigidbody, aplicar força de explosão.
                // AddExplosionForce simula uma onda de choque radial realista.
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null) {
                    rb.AddExplosionForce(5000f, explosionPos, radius);
                }

                // CONCEITO: Se for um inimigo, aplicar dano direto.
                // Cada inimigo deve ter componente com tag "Enemy" ou interface IDamageable.
                if (hit.CompareTag("Enemy")) {
                    EnemyBase enemy = hit.GetComponent<EnemyBase>();
                    if (enemy != null) {
                        enemy.TakeDamage(damage);
                    }
                }
            }

            // CONCEITO: Raycast para baixo para encontrar o chão onde colocar o VFX.
            // A maioria das explosões acontece perto do solo, então colocar efeito lá é visual.
            RaycastHit hitInfo;
            if (Physics.Raycast(explosionPos, Vector3.down, out hitInfo, 50f)) {
                if (explosionVFXPrefab != null) {
                    // CONCEITO: Instantiar VFX no ponto onde o raycast acertou o chão.
                    // Rotacionar para que fique alinhado com a normal da superfície.
                    Instantiate(explosionVFXPrefab, hitInfo.point,
                        Quaternion.FromToRotation(Vector3.forward, hitInfo.normal));
                }
            }

            // CONCEITO: Destruir o GameObject da granada lançada.
            // Ela já explodiu, não precisa mais existir.
            Destroy(grenadeObject);
            thrownGrenadeInstance = null;
        }

        #endregion

        #region AUDIO

        private void PlayEquipSound() {
            if (equipClip != null && audioService != null) {
                audioService.PlaySFX2D(equipClip, equipVolume);
            }
        }

        private void PlayPinPullSound() {
            if (pinPullClip != null && audioService != null) {
                audioService.PlaySFX2D(pinPullClip, pinPullVolume);
            }
        }

        private void PlayThrowSound() {
            if (throwClip != null && audioService != null) {
                audioService.PlaySFX2D(throwClip, throwVolume);
            }
        }

        private void PlayExplosionSound(Vector3 position) {
            if (explosionClip != null && audioService != null) {
                audioService.PlaySFX3D(explosionClip, position, explosionVolume);
            }
        }

        #endregion
    }
}

