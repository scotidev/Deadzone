using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages player interaction with interactable objects in the game world.
/// Uses raycasting from the player camera to detect objects with Interactable components.
/// Allows interaction through a configurable key.
/// </summary>
public class PlayerInteraction : MonoBehaviour {
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    private Camera playerCamera;
    private Interactable currentInteractable;

    private void Start() {
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update() {
        if (HandleShopOpenBlocking())
            return;

        CheckForInteractable();
        HandleInteractionInput();
    }

    /// <summary>
    /// Handles blocking of interaction detection when the shop is open or the player is placing a building item.
    /// Clears current interactable and hides UI prompt if either mode is active.
    /// </summary>
    private bool HandleShopOpenBlocking() {
        bool shouldBlock = GameManager.Instance != null && GameManager.Instance.State == GameState.Shopping;

        if (shouldBlock) {
            if (currentInteractable != null) {
                currentInteractable = null;

                if (UIManager.Instance != null)
                    UIManager.Instance.ToggleInteractionPrompt(false);
            }
        }

        return shouldBlock;
    }

    /// <summary>
    /// Processes player input for interacting with objects.
    /// Triggers interaction when E key is pressed and a valid interactable is detected.
    /// </summary>
    private void HandleInteractionInput() {
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Shopping)
            return;

        if (currentInteractable != null && Keyboard.current.eKey.wasPressedThisFrame) {
            currentInteractable.Interact();
        }
    }

    /// <summary>
    /// Performs a raycast from the camera center to detect interactable objects.
    /// Updates the UI interaction prompt based on what the player is looking at.
    /// </summary>
    private void CheckForInteractable() {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer)) {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();

            if (interactable != null) {
                if (currentInteractable != interactable) {
                    currentInteractable = interactable;

                    if (UIManager.Instance != null) {
                        UIManager.Instance.ToggleInteractionPrompt(true, interactable.GetInteractionPrompt());
                    }
                }
                return;
            }
        }

        if (currentInteractable != null) {
            currentInteractable = null;

            if (UIManager.Instance != null) {
                UIManager.Instance.ToggleInteractionPrompt(false);
            }
        }
    }
}
