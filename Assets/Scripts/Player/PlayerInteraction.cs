using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages player interaction with interactable objects in the game world.
/// Uses raycasting from the player camera to detect objects with Interactable/HUD components.
/// Allows interaction through a configurable key.
/// Also detects enemies for the health bar UI system.
/// </summary>
public class PlayerInteraction : MonoBehaviour {
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Enemy Detection")]
    [Tooltip("Maximum distance to detect enemies for health bar display.")]
    [SerializeField] private float enemyDetectionDistance = 50f;

    [Tooltip("Reference to the EnemyHealthBarUI component.")]
    [SerializeField] private EnemyHealthBarUI enemyHealthBarUI;

    private Camera playerCamera;
    private Interactable currentInteractable;
    private EnemyBase currentTargetedEnemy;

    private void Start() {
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update() {
        if (HandleShopOpenBlocking())
            return;

        CheckForInteractable();
        CheckForEnemy();
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

            if (currentTargetedEnemy != null) {
                currentTargetedEnemy = null;
                if (enemyHealthBarUI != null)
                    enemyHealthBarUI.SetTargetEnemy(null);
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

    /// <summary>
    /// Performs a raycast to detect enemies for health bar display.
    /// Uses a longer range than interaction raycast.
    /// </summary>
    private void CheckForEnemy() {
        if (enemyHealthBarUI == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, enemyDetectionDistance)) {
            EnemyBase enemy = hit.collider.GetComponentInParent<EnemyBase>();

            if (enemy != null) {
                if (currentTargetedEnemy != enemy) {
                    currentTargetedEnemy = enemy;
                    enemyHealthBarUI.SetTargetEnemy(enemy);
                }
                return;
            }
        }

        if (currentTargetedEnemy != null) {
            currentTargetedEnemy = null;
            enemyHealthBarUI.SetTargetEnemy(null);
        }
    }
}
