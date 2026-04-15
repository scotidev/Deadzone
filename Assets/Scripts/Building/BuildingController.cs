using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the building mode: selecting items, showing the ghost object, and placing the real object in the world.
/// </summary>
public class BuildingController : MonoBehaviour, ISelectableItem {
    /// <summary>Global access point to the single <see cref="BuildingController"/> instance.</summary>
    public static BuildingController Instance { get; private set; }

    [Header("Buildable Items (keys 6 / 7 / 8)")]
    [SerializeField] private BuildableDataSO itemSlot6; // Wall
    [SerializeField] private BuildableDataSO itemSlot7; // Explosive Barrel
    [SerializeField] private BuildableDataSO itemSlot8; // Beartrap

    [Header("Detection Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float maxPlacementDistance = 8f;
    [SerializeField] private Character playerCharacter;

    private Camera playerCamera;
    private GameObject currentGhost;
    private GhostObject currentGhostObject;
    private BuildableDataSO selectedItem;

    public bool IsPlacing => currentGhost != null;

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        ResolvePlayerCharacter();
    }

    /// <summary>
    /// Ensures a valid reference to the player's character component.
    /// </summary>
    private void ResolvePlayerCharacter() {
        if (playerCharacter != null)
            return;

        playerCharacter = FindFirstObjectByType<Character>();
    }

    private void Start() {
        playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update() {
        if (playerCamera == null) return;

        if (IsPlacing) {
            UpdateGhostPosition();
            HandlePlacementInput();
        }
    }

    // Legacy Input Actions removed: OnPlaceBuildable1, OnPlaceBuildable2, OnPlaceBuildable3.
    // Buildable selection is now centralized in ItemSelector.cs through SelectBuildableBySlot().

    /// <summary>
    /// Public method to select a buildable item by slot number (1, 2, or 3).
    /// Called by the unified weapon selection system in Character.cs.
    /// This allows keys 6, 7, 8 to be handled through the OnSelectWeapon method.
    /// </summary>
    /// <param name="slotNumber">The buildable slot to select (1 = Barricade/Key 6, 2 = Explosive Barrel/Key 7, 3 = Trap/Key 8)</param>
    public void SelectBuildableBySlot(int slotNumber) {
        // Based on the slot number, select the corresponding buildable item
        switch (slotNumber) {
            case 1: // Key 6 - Barricade
                SelectItem(itemSlot6);
                break;
            case 2: // Key 7 - Explosive Barrel
                SelectItem(itemSlot7);
                break;
            case 3: // Key 8 - Trap
                SelectItem(itemSlot8);
                break;
            default:
                // Invalid slot number
                Debug.LogWarning($"[BuildingController] Invalid buildable slot number: {slotNumber}. Must be 1, 2, or 3.");
                break;
        }
    }

    /// <summary>
    /// Receives the selected buildable item and sets up the ghost object for placement. If the same item is selected again, it cancels the placement mode.
    /// Checks if player has any of this buildable in inventory before allowing placement.
    /// </summary>
    /// <param name="item"></param>
    private void SelectItem(BuildableDataSO item) {
        ResolvePlayerCharacter();
        if (item == null) return;

        // Check if player has this buildable in inventory
        if (PlayerProgress.Instance != null) {
            string buildableID = GetBuildableID(item);
            if (!string.IsNullOrEmpty(buildableID)) {
                int quantity = PlayerProgress.Instance.GetBuildableQuantity(buildableID);
                if (quantity <= 0) {
                    Debug.LogWarning($"[BuildingController] No {buildableID} in inventory! Purchase from shop first.");
                    return;
                }
            }
        }

        if (selectedItem == item) {
            CancelPlacement();
            return;
        }

        DestroyCurrentGhost();

        selectedItem = item;

        playerCharacter?.SetHolstered(true);

        if (item.ghostPrefab == null) {
            Debug.LogWarning($"[BuildingController] ${item.name} não tem Ghost Prefab configurado no BuildableSO!");
            selectedItem = null;
            playerCharacter?.SetHolstered(false);
            return;
        }

        currentGhost = Instantiate(item.ghostPrefab, Vector3.zero, Quaternion.identity);

        foreach (Collider col in currentGhost.GetComponentsInChildren<Collider>())
            col.enabled = false;

        currentGhost.SetActive(false);

        currentGhostObject = currentGhost.GetComponent<GhostObject>();
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

            if (hit.normal.y < 0.5f) {
                currentGhost.SetActive(false);
                return;
            }

            Vector3 placementPos = hit.point + Vector3.up * (selectedItem.overlapBoxSize.y * 0.5f);

            currentGhost.transform.position = placementPos;

            currentGhost.transform.rotation = Quaternion.Euler(selectedItem.placementRotationEuler);

            LayerMask overlapMask = wallLayer.value != 0 ? obstacleLayer | wallLayer : obstacleLayer;

            Collider[] collisions = Physics.OverlapBox(
                placementPos,
                selectedItem.overlapBoxSize * 0.5f,
                Quaternion.identity,
                overlapMask
            );

            currentGhostObject?.SetPlaceable(collisions.Length == 0);

            currentGhost.SetActive(true);
        }
        else {
            currentGhost.SetActive(false);
        }
    }

    /// <summary>
    /// Processes user input to detect and confirm object placement when the left mouse button is clicked.
    /// </summary>
    private void HandlePlacementInput() {
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

        if (currentGhostObject == null || !currentGhostObject.IsPlaceable()) return;

        if (selectedItem.realPrefab == null) return;

        // Check if player has this buildable in inventory and consume one
        if (PlayerProgress.Instance != null) {
            string buildableID = GetBuildableID(selectedItem);
            if (!string.IsNullOrEmpty(buildableID)) {
                if (!PlayerProgress.Instance.ConsumeBuildable(buildableID)) {
                    Debug.LogWarning($"[BuildingController] Failed to consume {buildableID} - inventory empty!");
                    CancelPlacement();
                    return;
                }
            }
        }

        Instantiate(selectedItem.realPrefab,
            currentGhost.transform.position,
            Quaternion.Euler(selectedItem.placementRotationEuler));

        CancelPlacement();
    }

    /// <summary>
    /// Maps BuildableSO to buildable ID for inventory tracking.
    /// </summary>
    public string GetBuildableID(BuildableDataSO buildable) {
        if (buildable == itemSlot6) return "6"; // Barricades
        if (buildable == itemSlot7) return "7"; // ExplosiveBarrels
        if (buildable == itemSlot8) return "8"; // Traps
        return null;
    }

    /// <summary>
    /// Selects the currently chosen buildable item.
    /// This method is part of the ISelectableItem interface.
    /// </summary>
    public void Select() {
        // The BuildingController's Select logic is handled by SelectItem(BuildableDataSO item),
        // which is called by SelectBuildableBySlot(int slotNumber).
        // For ISelectableItem, we need a way to select the currently *staged* buildable.
        // If there's a selectedItem, re-select it to potentially cancel placement if already selected.
        if (selectedItem != null) {
            SelectItem(selectedItem);
        }
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
    /// </summary>
    private void CancelPlacement() {
        ResolvePlayerCharacter();
        DestroyCurrentGhost();
        selectedItem = null;
        playerCharacter?.SetHolstered(false);
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
        CancelPlacement();
    }
}
