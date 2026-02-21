using TMPro;
using UnityEngine;
using static UIManager;

/// <summary>
/// Manages player interaction with interactable objects in the game world.
/// Uses raycasting from the player camera to detect objects with Interactable components.
/// Allows interaction through a configurable key.
/// </summary>
public class PlayerInteraction : MonoBehaviour {
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    private Camera playerCamera;
    private Interactable currentInteractable;

    private void Start() {
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    /// <summary>
    /// Updates interactable object detection every frame and processes interaction input.
    /// Blocks detection when the shop is open to prevent UI overlap.
    /// </summary>
    private void Update() {
        if (ShopInterface.Instance != null && ShopInterface.Instance.IsShopOpen()) {
            if (currentInteractable != null) {
                currentInteractable = null;

                if (UIManager.Instance != null)
                    UIManager.Instance.ToggleInteractionPrompt(false);
            }
            return;
        }

        CheckForInteractable();

        if (currentInteractable != null && Input.GetKeyDown(interactionKey)) {
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
