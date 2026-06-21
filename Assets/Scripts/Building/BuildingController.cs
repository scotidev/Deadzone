using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controller responsible for managing the building mechanics in the game.
/// </summary>
public class BuildingController : MonoBehaviour {

    #region STATIC

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
            Debug.LogWarning($"[BuildingController] {item.name} does not have a Ghost Prefab configured in BuildableSO!");
            selectedItem = null;
            playerCharacter?.SetHolstered(false);
            return;
        }

        currentGhost = Instantiate(item.GhostPrefab, Vector3.zero, Quaternion.identity);

        foreach (Collider col in currentGhost.GetComponentsInChildren<Collider>())
            col.enabled = false;

        currentGhost.SetActive(false);

        currentGhostObject = currentGhost.GetComponent<GhostObject>();

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
    /// Updates the ghost object position and rotation based on the player's camera view and detected surface.
    /// </summary>
    private void UpdateGhostPosition() {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        LayerMask groundMask = groundLayer.value != 0 ? groundLayer : Physics.DefaultRaycastLayers;
        LayerMask raycastMask = wallLayer.value != 0 ? groundMask | wallLayer : groundMask;

        if (Physics.Raycast(ray, out RaycastHit hit, maxPlacementDistance, raycastMask)
            && hit.distance > 0.5f) {

            float minNormalY = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);

            bool isSlopeValid = hit.normal.y >= minNormalY;

            bool isOnGroundLayer = ((1 << hit.collider.gameObject.layer) & groundMask) != 0;

            Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Quaternion playerLookRotation = Quaternion.Euler(0, playerCamera.transform.eulerAngles.y, 0);
            Quaternion itemCorrection = Quaternion.Euler(selectedItem.PlacementRotationEuler);

            Quaternion finalRotation = surfaceRotation * playerLookRotation * itemCorrection;
            targetGhostRotation = finalRotation;

            Vector3 placementPos = hit.point + hit.normal * (selectedItem.OverlapBoxSize.y * 0.5f);
            if (Vector3.Distance(targetGhostPosition, placementPos) > 0.01f)
                targetGhostPosition = placementPos;

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
            bool isPlaceable = hasInventory && collisions.Length == 0 && isSlopeValid && isOnGroundLayer;

            currentGhostObject?.SetPlaceable(isPlaceable);
            currentGhost.SetActive(true);
        } else {
            currentGhost.SetActive(false);
        }
    }

    /// <summary>
    /// Smooths the ghost transform using SmoothDamp and Slerp to eliminate jitter.
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
    /// Processes user input to detect and confirm object placement.
    /// </summary>
    private void HandlePlacementInput() {
        if (playerCharacter != null && playerCharacter.IsInterfaceMode())
            return;

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryPlaceObject();
    }

    /// <summary>
    /// Attempts to place the selected object in the scene at the ghost position.
    /// </summary>
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
                if (!PlayerProgress.Instance.UseItem(buildableID, 1)) {
                    Debug.LogWarning($"[BuildingController] Failed to consume {buildableID} - inventory empty!");
                    CancelPlacement();
                    return;
                }

                PlayerProgress.Instance.SetItemCurrent(buildableID, 0);
            }
        }

        GameObject placedObject = Instantiate(selectedItem.RealPrefab,
            currentGhost.transform.position,
            currentGhost.transform.rotation);

        if (placedObject != null) {
            Barricade barricade = placedObject.GetComponent<Barricade>();
            if (barricade != null) {
                barricade.PlayPlacementSound();
            }

            BearTrap bearTrap = placedObject.GetComponent<BearTrap>();
            if (bearTrap != null) {
                bearTrap.PlayPlacementSound();
                bearTrap.SetPlaced(true);
            }

            ExplosiveBarrel explosiveBarrel = placedObject.GetComponent<ExplosiveBarrel>();
            if (explosiveBarrel != null) {
                explosiveBarrel.PlayPlacementSound();
            }
        }

        if (PlayerProgress.Instance != null && selectedItem != null) {
            string buildableID = GetBuildableID(selectedItem);
            if (!string.IsNullOrEmpty(buildableID)) {
                int remainingQty = PlayerProgress.Instance.GetItemTotal(buildableID);
                if (remainingQty > 0) {
                    ResetPlacementForNextItem();
                } else {
                    CancelPlacement();
                    playerCharacter?.TryRestoreWeaponSmoothly();
                }
            }
        } else {
            CancelPlacement();
        }
    }

    /// <summary>
    /// Maps BuildableSO to buildable ID for inventory tracking.
    /// </summary>
    public string GetBuildableID(BuildableDataSO buildable) {
        if (buildable == null) return null;
        return buildable.ItemID;
    }

    /// <summary>
    /// Destroys the current ghost object.
    /// </summary>
    private void DestroyCurrentGhost() {
        if (currentGhost != null) {
            Destroy(currentGhost);
            currentGhost = null;
        }
        currentGhostObject = null;
    }

    /// <summary>
    /// Cancels the current placement mode, cleaning up ghost and selection.
    /// </summary>
    public void CancelPlacement() {
        ResolvePlayerCharacter();
        DestroyCurrentGhost();
        selectedItem = null;
    }

    /// <summary>
    /// Resets the ghost for placing another item without exiting placement mode.
    /// </summary>
    private void ResetPlacementForNextItem() {
        if (selectedItem == null) {
            CancelPlacement();
            return;
        }

        DestroyCurrentGhost();

        if (selectedItem.GhostPrefab == null) {
            Debug.LogWarning($"[BuildingController] {selectedItem.name} does not have a Ghost Prefab configured!");
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

        if (PlayerProgress.Instance != null && !string.IsNullOrEmpty(selectedItem.ItemID)) {
            PlayerProgress.Instance.SetItemCurrent(selectedItem.ItemID, 1);
        }
    }

    #endregion
}
