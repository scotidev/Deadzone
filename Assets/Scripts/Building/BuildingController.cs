using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controller responsible for managing the building mechanics in the game.
/// </summary>
public class BuildingController : MonoBehaviour {

    #region STATIC

    /// <summary>Global access point to the single <see cref="BuildingController"/> instance.</summary>
    public static BuildingController Instance { get; private set; }

    #endregion

    #region SERIALIZED FIELDS

    [Header("Audio")]
    [SerializeField] private AudioClip invalidPlacementSound;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("Layer assigned to SafeZones. Excluded from placement overlap so SafeZones don't block building but trigger colliders (e.g. BearTrap) are still detected.")]
    [SerializeField] private LayerMask safeZoneLayer;
    [SerializeField] private Character playerCharacter;
    [SerializeField] private float maxPlacementDistance = 8f;
    [SerializeField] private float maxSlopeAngle = 45f;

    #endregion

    #region FIELDS

    private Camera playerCamera;
    private GameObject currentGhost;
    private GhostObject currentGhostObject;
    private BuildableDataSO selectedItem;
    private IAudioManagerService audioService;

    // Smoothing para o ghost (elimina tremor)
    private Vector3 targetGhostPosition;
    private Quaternion targetGhostRotation;
    private Vector3 ghostVelocity;

    #endregion

    #region PROPERTIES
    public BuildableDataSO CurrentSelectedItem => selectedItem;
    public bool IsPlacing => currentGhost != null;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        ResolvePlayerCharacter();
    }

    private void Start() {
        playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
        CancelPlacement();
    }

    private void Update() {
        if (playerCamera == null) return;

        if (IsPlacing) {
            UpdateGhostPosition();
            SmoothGhostTransform();
            HandlePlacementInput();
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Starts placement mode for a buildable item.
    /// Called by Inventory when player selects a buildable.
    /// </summary>
    public void StartPlacement(BuildableDataSO item) {
        ResolvePlayerCharacter();

        if (item == null) {
            return;
        }

        if (selectedItem == item) {
            DestroyCurrentGhost();
        }

        DestroyCurrentGhost();

        selectedItem = item;

        playerCharacter?.SetHolstered(true);

        if (item.GhostPrefab == null) {
            Debug.LogWarning($"[BuildingController] {item.name} não tem Ghost Prefab configurado no BuildableSO!");
            selectedItem = null;
            playerCharacter?.SetHolstered(false);
            return;
        }

        currentGhost = Instantiate(item.GhostPrefab, Vector3.zero, Quaternion.identity);

        foreach (Collider col in currentGhost.GetComponentsInChildren<Collider>())
            col.enabled = false;

        currentGhost.SetActive(false);

        currentGhostObject = currentGhost.GetComponent<GhostObject>();

        // Inicializa os targets de smoothing na posição atual do ghost
        // pra evitar que ele faça um "voo" desde Vector3.zero ao ser criado.
        targetGhostPosition = currentGhost.transform.position;
        targetGhostRotation = currentGhost.transform.rotation;
        ghostVelocity = Vector3.zero;
    }

    /// <summary>
    /// Cancels the current placement mode.
    /// </summary>
    public void CancelCurrentPlacement() {
        CancelPlacement();
    }

    /// <summary>
    /// Ensures a valid reference to the player's character component.
    /// </summary>
    private void ResolvePlayerCharacter() {
        if (playerCharacter != null)
            return;

        playerCharacter = FindFirstObjectByType<Character>();
    }

    /// <summary>
    /// Updates the position and rotation of the ghost object to reflect the player's current camera view and the
    /// detected placement surface.
    /// </summary>
    /// <remarks>This method casts a ray from the center of the player's camera to determine a valid placement
    /// location on the ground or wall. The ghost object is only shown if the detected surface is suitable for placement
    /// and not obstructed by other objects. The method also ensures the ghost is not positioned too close to the player
    /// and visually indicates whether the placement area is valid.</remarks>
    private void UpdateGhostPosition() {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        LayerMask groundMask = groundLayer.value != 0 ? groundLayer : Physics.DefaultRaycastLayers;
        LayerMask raycastMask = wallLayer.value != 0 ? groundMask | wallLayer : groundMask;

        if (Physics.Raycast(ray, out RaycastHit hit, maxPlacementDistance, raycastMask)
            && hit.distance > 0.5f) {

            // Convertemos o ângulo máximo em um valor de Normal.y usando Cosseno.
            float minNormalY = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);
            
            // Verificamos se a inclinação é válida.
            bool isSlopeValid = hit.normal.y >= minNormalY;

            // Verificamos se a superfície acertada está na layer Ground.
            bool isOnGroundLayer = ((1 << hit.collider.gameObject.layer) & groundMask) != 0;

            // --- CÁLCULO DE ROTAÇÃO ALINHADA AO TERRENO ---
            Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Quaternion playerLookRotation = Quaternion.Euler(0, playerCamera.transform.eulerAngles.y, 0);
            Quaternion itemCorrection = Quaternion.Euler(selectedItem.PlacementRotationEuler);
            
            Quaternion finalRotation = surfaceRotation * playerLookRotation * itemCorrection;
            targetGhostRotation = finalRotation;

            // --- POSICIONAMENTO ---
            Vector3 placementPos = hit.point + hit.normal * (selectedItem.OverlapBoxSize.y * 0.5f);
            // Só atualiza o target se o deslocamento for significativo (> 1cm) pra cortar micro-oscilações do raycast
            if (Vector3.Distance(targetGhostPosition, placementPos) > 0.01f)
                targetGhostPosition = placementPos;

            // CONCEITO: Incluímos trigger colliders na verificação para detectar BearTraps
            // (que usam colliders trigger para OnTriggerEnter com inimigos),
            // mas excluímos a layer da SafeZone para não bloquear construção sobre áreas seguras.
            LayerMask safeZoneMask = safeZoneLayer.value != 0 ? safeZoneLayer.value : 0;
            LayerMask overlapMask = ~groundMask & ~safeZoneMask;

            Collider[] collisions = Physics.OverlapBox(
                placementPos,
                selectedItem.OverlapBoxSize * 0.5f,
                finalRotation,
                overlapMask,
                QueryTriggerInteraction.Collide
            );

            bool hasInventory = HasInventoryQuantity();
            // Agora o objeto só pode ser posicionado se: tiver inventário, não houver colisão E a inclinação for válida.
            bool isPlaceable = hasInventory && collisions.Length == 0 && isSlopeValid && isOnGroundLayer;

            currentGhostObject?.SetPlaceable(isPlaceable);
            currentGhost.SetActive(true);
        } else {
            currentGhost.SetActive(false);
        }
    }

    /// <summary>
    /// Aplica SmoothDamp na posição e Slerp na rotação do ghost pra eliminar tremor.
    /// </summary>
    private void SmoothGhostTransform() {
        if (currentGhost == null) return;

        currentGhost.transform.position = Vector3.SmoothDamp(
            currentGhost.transform.position, targetGhostPosition, ref ghostVelocity, 0.04f);

        currentGhost.transform.rotation = Quaternion.Slerp(
            currentGhost.transform.rotation, targetGhostRotation, Time.deltaTime * 30f);
    }

    /// <summary>
    /// Checks if the player has at least one buildable in inventory.
    /// Used to force the ghost to red when out of items.
    /// </summary>
    private bool HasInventoryQuantity() {
        if (PlayerProgress.Instance == null || selectedItem == null)
            return false;

        string buildableID = GetBuildableID(selectedItem);
        if (string.IsNullOrEmpty(buildableID))
            return false;

        return PlayerProgress.Instance.GetBuildableQuantity(buildableID) > 0;
    }

    /// <summary>
    /// Processes user input to detect and confirm object placement when the left mouse button is clicked.
    /// </summary>
    private void HandlePlacementInput() {
        // SEGURANÇA: Não permitir posicionamento se estiver em modo de interface (loja/menus)
        // ou se o mouse estiver sobre um elemento da UI (botões do pause, etc).
        if (playerCharacter != null && playerCharacter.IsInterfaceMode())
            return;

        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryPlaceObject();
    }

    /// <summary>
    /// Attempts to place the currently selected object in the scene at the position and rotation of the active ghost
    /// object, if placement conditions are met. Consumes one buildable from inventory.
    /// </summary>
    /// <remarks>This method checks whether the ghost object is active, the target location is valid, and a
    /// real prefab is assigned before instantiating the object. After successful placement, the method immediately
    /// cancels placement mode. The placed object remains in the scene until explicitly destroyed by other
    /// logic.</remarks>
    private void TryPlaceObject() {
        if (currentGhost == null || !currentGhost.activeSelf) return;

        if (currentGhostObject == null || !currentGhostObject.IsPlaceable()) {
            if (invalidPlacementSound != null)
                audioService.PlaySFX2D(invalidPlacementSound);
            return;
        }

        if (selectedItem.RealPrefab == null) return;

        if (PlayerProgress.Instance != null) {
            string buildableID = GetBuildableID(selectedItem);

            if (!string.IsNullOrEmpty(buildableID)) {
                int beforeQty = PlayerProgress.Instance.GetItemTotal(buildableID);
                Debug.Log($"[BuildingController] TryPlaceObject: attempting to place {buildableID} ({selectedItem.ItemName}). before={beforeQty}");

                // NEW: Use unified UseItem() instead of ConsumeBuildable()
                // This updates the total inventory quantity
                if (!PlayerProgress.Instance.UseItem(buildableID, 1)) {
                    Debug.LogWarning($"[BuildingController] Failed to consume {buildableID} - inventory empty!");
                    CancelPlacement();
                    return;
                }

                int afterQty = PlayerProgress.Instance.GetItemTotal(buildableID);
                Debug.Log($"[BuildingController] TryPlaceObject: UseItem succeeded for {buildableID}. remaining={afterQty}");

                // FIXED: Reset current ammo to 0 after placing the buildable.
                // The "current" represents what's in hand - after placing, hand is empty.
                PlayerProgress.Instance.SetItemCurrent(buildableID, 0);

                if (afterQty > 0) {
                    Debug.Log($"[BuildingController] TryPlaceObject: remaining > 0, EXPECTED BEHAVIOR: keep item selected. (current code will still CancelPlacement)");
                } else {
                    Debug.Log($"[BuildingController] TryPlaceObject: used last {buildableID} - EXPECTED: auto-equip pistol");
                }
            }
        }

        // Instanciamos o objeto real exatamente na mesma posição e rotação do ghost atual.
        GameObject placedObject = Instantiate(selectedItem.RealPrefab,
            currentGhost.transform.position,
            currentGhost.transform.rotation);

        // Try to play placement sound and initialize if the placed object has the method
        if (placedObject != null) {
            // Try Barricade
            Barricade barricade = placedObject.GetComponent<Barricade>();
            if (barricade != null) {
                barricade.PlayPlacementSound();
                // CONCEITO: A barricada agora se auto-inicializa no Awake() lendo do
                // BuildableDataSO.GetResistanceAtLevel() via PlayerProgress.
                // O BuildingController não precisa mais calcular ou passar health.
            }

            // Try BearTrap
            BearTrap bearTrap = placedObject.GetComponent<BearTrap>();
            if (bearTrap != null) {
                bearTrap.PlayPlacementSound();
                bearTrap.SetPlaced(true);
            }

            // Try ExplosiveBarrel
            ExplosiveBarrel explosiveBarrel = placedObject.GetComponent<ExplosiveBarrel>();
            if (explosiveBarrel != null) {
                explosiveBarrel.PlayPlacementSound();
            }
        }

        // NEW: Check remaining quantity AFTER placement
        // CONCEITO: Se ainda houver quantidade no inventário, o buildable permanece equipado
        // e pronto para posicionar outro item. Apenas quando qty = 0, voltamos à pistola.
        if (PlayerProgress.Instance != null && selectedItem != null) {
            string buildableID = GetBuildableID(selectedItem);
            if (!string.IsNullOrEmpty(buildableID)) {
                int remainingQty = PlayerProgress.Instance.GetItemTotal(buildableID);
                if (remainingQty > 0) {
                    // Item ainda tem quantidade: reset ghost para novo placement, mantém item equipado
                    Debug.Log($"[BuildingController] TryPlaceObject: remaining qty={remainingQty}, resetting ghost for next placement");
                    ResetPlacementForNextItem();
                } else {
                    // Última unidade colocada: volta à pistola com animação suave através do Character
                    Debug.Log($"[BuildingController] TryPlaceObject: qty=0, triggering smooth weapon restoration");
                    
                    // Primeiro limpamos o estado de construção
                    CancelPlacement();
                    
                    // Depois pedimos ao personagem para restaurar a arma com holster animation
                    playerCharacter?.TryRestoreWeaponSmoothly();
                }
            }
        } else {
            // Fallback: Se PlayerProgress não disponível, cancela normalmente
            CancelPlacement();
        }
    }

    /// <summary>
    /// Maps BuildableSO to buildable ID for inventory tracking.
    /// </summary>
    /// <param name="buildable">The BuildableDataSO to get the ID from.</param>
    /// <returns>The itemID of the buildable.</returns>
    public string GetBuildableID(BuildableDataSO buildable) {
        if (buildable == null) return null;
        return buildable.ItemID;
    }

    /// <summary>
    /// Destroys the current ghost object, removing it from the scene and freeing associated memory.
    /// </summary>
    /// <remarks>Call this method when switching items to remove the ghost object without altering the
    /// weapon's state. This helps prevent unwanted animation flicker that may occur if the weapon state is
    /// changed.</remarks>
    private void DestroyCurrentGhost() {
        if (currentGhost != null) {
            Destroy(currentGhost);
            currentGhost = null;
        }
        currentGhostObject = null;
    }

    /// <summary>
    /// Cancels the current placement mode, cleaning up all related objects and UI elements.
    /// CONCEITO: Este método agora foca apenas em limpar o estado de construção (Ghost e item selecionado).
    /// A responsabilidade de voltar para a arma suavemente é delegada ao Character.
    /// </summary>
    public void CancelPlacement() {
        ResolvePlayerCharacter();
        DestroyCurrentGhost();
        selectedItem = null;
        
        // Removemos o RestoreLastWeapon e SetHolstered(false) daqui para evitar trocas bruscas.
        // Se este método for chamado via OnDeselected, o Character já estará gerenciando a troca suave.
        Debug.Log($"[BuildingController] CancelPlacement: Ghost and selection cleared.");
    }

    /// <summary>
    /// Resets the placement for the next item without canceling placement mode or returning to weapon.
    /// CONCEITO: Após posicionar um buildable com sucesso, se ainda há quantidade,
    /// destruímos o ghost atual e criamos um novo para o próximo posicionamento.
    /// Isso permite colocar múltiplos items do mesmo tipo sem apertar a tecla novamente.
    /// </summary>
    private void ResetPlacementForNextItem() {
        if (selectedItem == null) {
            CancelPlacement();
            return;
        }

        // Destruir ghost atual para criar novo
        DestroyCurrentGhost();

        // Recriar novo ghost para o próximo placement
        if (selectedItem.GhostPrefab == null) {
            Debug.LogWarning($"[BuildingController] {selectedItem.name} não tem Ghost Prefab configurado!");
            CancelPlacement();
            return;
        }

        currentGhost = Instantiate(selectedItem.GhostPrefab, Vector3.zero, Quaternion.identity);

        foreach (Collider col in currentGhost.GetComponentsInChildren<Collider>())
            col.enabled = false;

        currentGhost.SetActive(false);
        currentGhostObject = currentGhost.GetComponent<GhostObject>();

        targetGhostPosition = currentGhost.transform.position;
        targetGhostRotation = currentGhost.transform.rotation;
        ghostVelocity = Vector3.zero;

        // NOVO: Garantir que o item atual (em mão) seja contado como 1 no HUD
        // Isso resolve o problema do contador 'Current' mostrar 0 mesmo tendo mais itens.
        if (PlayerProgress.Instance != null && !string.IsNullOrEmpty(selectedItem.ItemID)) {
            PlayerProgress.Instance.SetItemCurrent(selectedItem.ItemID, 1);
        }

        Debug.Log($"[BuildingController] ResetPlacementForNextItem: Ghost reset for next {selectedItem.ItemName}");
    }

    #endregion
}
