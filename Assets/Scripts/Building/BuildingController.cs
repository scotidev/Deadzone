using UnityEngine;
using UnityEngine.InputSystem;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Controls the building mode: selecting items, showing the ghost object, and placing the real object in the world.
/// </summary>
public class BuildingController : MonoBehaviour {
    /// <summary>Global access point to the single <see cref="BuildingController"/> instance.</summary>
    public static BuildingController Instance { get; private set; }

    [Header("Buildable Items (keys 6 / 7 / 8)")]
    [SerializeField] private BuildableSO itemSlot6; // Wall
    [SerializeField] private BuildableSO itemSlot7; // Explosive Barrel
    [SerializeField] private BuildableSO itemSlot8; // Beartrap

    [Header("Detection Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float maxPlacementDistance = 8f;
    [SerializeField] private Character playerCharacter;

    private Camera playerCamera;
    private GameObject currentGhost;
    private GhostObject currentGhostObject;
    private BuildableSO selectedItem;

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

        playerCharacter = FindObjectOfType<Character>();
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

    /// <summary>
    /// Called by PlayerInput when the player presses the key bound to "Place Buildable 1" (default: 6).
    /// </summary>
    public void OnPlaceBuildable1(InputAction.CallbackContext context) {
        if (context.phase == InputActionPhase.Performed)
            SelectItem(itemSlot6);
    }

    /// <summary>
    /// Called by PlayerInput when the player presses the key bound to "Place Buildable 2" (default: 7).
    /// </summary>
    public void OnPlaceBuildable2(InputAction.CallbackContext context) {
        if (context.phase == InputActionPhase.Performed)
            SelectItem(itemSlot7);
    }

    /// <summary>
    /// Called by PlayerInput when the player presses the key bound to "Place Buildable 3" (default: 8).
    /// </summary>
    public void OnPlaceBuildable3(InputAction.CallbackContext context) {
        if (context.phase == InputActionPhase.Performed)
            SelectItem(itemSlot8);
    }

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
    /// </summary>
    /// <param name="item"></param>
    private void SelectItem(BuildableSO item) {
        ResolvePlayerCharacter();
        if (item == null) return;

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
    /// object, if placement conditions are met.
    /// </summary>
    /// <remarks>This method checks whether the ghost object is active, the target location is valid, and a
    /// real prefab is assigned before instantiating the object. After successful placement, the method immediately
    /// cancels placement mode. The placed object remains in the scene until explicitly destroyed by other
    /// logic.</remarks>
    private void TryPlaceObject() {
        if (currentGhost == null || !currentGhost.activeSelf) return;

        if (currentGhostObject == null || !currentGhostObject.IsPlaceable()) return;

        if (selectedItem.realPrefab == null) return;

        Instantiate(selectedItem.realPrefab,
            currentGhost.transform.position,
            Quaternion.Euler(selectedItem.placementRotationEuler));

        CancelPlacement();
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
