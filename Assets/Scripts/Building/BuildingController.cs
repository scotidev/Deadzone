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

    [Header("Buildable Items")]
    [SerializeField] private BuildableDataSO barricade;
    [SerializeField] private BuildableDataSO explosiveBarrel;
    [SerializeField] private BuildableDataSO bearTrap;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private Character playerCharacter;
    [SerializeField] private float maxPlacementDistance = 8f;

    #endregion

    #region FIELDS

    private Camera playerCamera;
    private GameObject currentGhost;
    private GhostObject currentGhostObject;
    private BuildableDataSO selectedItem;

    #endregion

    #region PROPERTIES
    public BuildableDataSO Barricade => barricade;
    public BuildableDataSO ExplosiveBarrel => explosiveBarrel;
    public BuildableDataSO BearTrap => bearTrap;
    public BuildableDataSO CurrentSelectedItem => selectedItem;
    public bool IsPlacing => currentGhost != null;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

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

        if (PlayerProgress.Instance != null) {
            string buildableID = GetBuildableID(item);

            if (!string.IsNullOrEmpty(buildableID)) {
                int quantity = PlayerProgress.Instance.GetBuildableQuantity(buildableID);

                if (quantity <= 0) {
                    //FUTURAMENTE DEVE TOCAR UM SOM DE ERRO AQUI PARA DAR FEEDBACK AUDITIVO AO JOGADOR
                    Debug.LogWarning($"[BuildingController] No {buildableID} in inventory! Purchase from shop first.");
                    return;
                }
            }
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

            if (hit.normal.y < 0.5f) {
                currentGhost.SetActive(false);
                return;
            }

            Vector3 placementPos = hit.point + Vector3.up * (selectedItem.OverlapBoxSize.y * 0.5f);

            currentGhost.transform.position = placementPos;

            currentGhost.transform.rotation = Quaternion.Euler(selectedItem.PlacementRotationEuler);
            LayerMask overlapMask = wallLayer.value != 0 ? obstacleLayer | wallLayer : obstacleLayer;

            Collider[] collisions = Physics.OverlapBox(
                placementPos,
                selectedItem.OverlapBoxSize * 0.5f,
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

        if (selectedItem.RealPrefab == null) return;

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

        GameObject placedObject = Instantiate(selectedItem.RealPrefab,
            currentGhost.transform.position,
            Quaternion.Euler(selectedItem.PlacementRotationEuler));

        CancelPlacement();
    }

    /// <summary>
    /// Maps BuildableSO to buildable ID for inventory tracking.
    /// </summary>
    /// <param name="buildable">The BuildableDataSO to get the ID from.</param>
    /// <returns>The itemID of the buildable.</returns>
    public string GetBuildableID(BuildableDataSO buildable) {
        if (buildable == null) return null;
        return buildable.itemID;
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
    public void CancelPlacement() {
        ResolvePlayerCharacter();
        DestroyCurrentGhost();
        selectedItem = null;
        playerCharacter?.SetHolstered(false);
    }

    #endregion
}
