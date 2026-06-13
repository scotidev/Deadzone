using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages player interaction with interactable objects in the game world.
/// Uses raycasting from the player camera to detect objects with Interactable/HUD components.
/// Allows interaction through a configurable key.
/// Also detects enemies for the health bar UI system.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{

    #region SERIALIZED FIELDS

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Enemy Detection")]
    [SerializeField] private float enemyDetectionDistance = 50f;
    [SerializeField] private LayerMask uiLayerMask; // Layer mask to ignore UI elements
    [SerializeField] private EnemyHealthBarUI enemyHealthBarUI;

    #endregion

    #region FIELDS

    private Camera playerCamera;
    private Interactable currentInteractable;
    private EnemyBase currentTargetedEnemy;

    #endregion

    #region UNITY

    private void Start()
    {
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (HandleShopOpenBlocking())
            return;

        CheckForInteractable();
        CheckForEnemy();
        HandleInteractionInput();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Handles blocking of interaction detection when the shop is open or the player is placing a building item.
    /// Clears current interactable and hides UI prompt if either mode is active.
    /// </summary>
    private bool HandleShopOpenBlocking()
    {
        bool shouldBlock = GameManager.Instance != null && GameManager.Instance.State == GameState.Shopping;

        if (shouldBlock)
        {
            if (currentInteractable != null)
            {
                currentInteractable = null;

                if (UIManager.Instance != null)
                    UIManager.Instance.ToggleInteractionPrompt(false);
            }

            if (currentTargetedEnemy != null)
            {
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
    private void HandleInteractionInput()
    {
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Shopping)
            return;

        if (currentInteractable != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            // Salva a referência antes de interagir
            Interactable interactableBefore = currentInteractable;
            
            // Realiza a interação
            interactableBefore.Interact();

            // Se o objeto foi destruído ou desativado pela interação (como no pickup), limpa o HUD
            if (interactableBefore == null || !interactableBefore.gameObject.activeInHierarchy)
            {
                currentInteractable = null;
                if (UIManager.Instance != null)
                    UIManager.Instance.ToggleInteractionPrompt(false);
            }
            else if (UIManager.Instance != null)
            {
                // Se o objeto ainda existe, atualiza o prompt (ex: mudou de "Abrir" para "Fechar")
                UIManager.Instance.ToggleInteractionPrompt(true, interactableBefore.GetInteractionPrompt());
            }
        }
    }

    /// <summary>
    /// Performs a raycast from the camera center to detect interactable objects.
    /// Updates the UI interaction prompt based on what the player is looking at.
    /// </summary>
    private void CheckForInteractable()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // CONCEITO: TryGetComponentInParent é mais eficiente que GetComponentInParent
        // porque usa TryGetComponent (nativo) em vez de GetComponentInParent (gerenciado).
        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer, QueryTriggerInteraction.Ignore)
            && TryGetComponentInParent(hit.collider, out Interactable interactable))
        {
            if (currentInteractable != interactable)
            {
                currentInteractable = interactable;

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ToggleInteractionPrompt(true, interactable.GetInteractionPrompt());
                }
            }
            return;
        }

        // Se chegamos aqui, o raycast não atingiu um Interactable válido.
        if (currentInteractable != null || (UIManager.Instance != null && UIManager.Instance.IsInteractionPromptActive()))
        {
            currentInteractable = null;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ToggleInteractionPrompt(false);
            }
        }
    }

    /// <summary>
    /// Performs a raycast to detect enemies for health bar display.
    /// Uses a longer range than interaction raycast.
    /// </summary>
    /// <summary>
    /// Helper que busca um componente no collider ou em seus pais usando TryGetComponent.
    /// CONCEITO: TryGetComponent é um método NATIVO da Unity, MAIS RÁPIDO que GetComponentInParent
    /// porque não aloca memória gerenciada. A diferença é crucial num método chamado todo frame.
    /// </summary>
    private bool TryGetComponentInParent<T>(Collider collider, out T component) where T : class {
        component = null;
        if (collider == null) return false;

        // CONCEITO: Primeiro tenta no próprio collider (mais rápido, sem subir hierarquia).
        if (collider.TryGetComponent(out T direct)) {
            component = direct;
            return true;
        }

        // CONCEITO: Se não achou, sobe na hierarquia procurando nos pais.
        Transform parent = collider.transform.parent;
        while (parent != null) {
            if (parent.TryGetComponent(out T found)) {
                component = found;
                return true;
            }
            parent = parent.parent;
        }

        return false;
    }

    private void CheckForEnemy()
    {
        if (enemyHealthBarUI == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // CONCEITO: ÚNICO raycast. O código original tinha DOIS raycasts idênticos
        // (linhas 171 E 173), que é um bug que dobrava o custo de física toda vez.
        if (Physics.Raycast(ray, out hit, enemyDetectionDistance, ~uiLayerMask)
            && TryGetComponentInParent(hit.collider, out EnemyBase enemy))
        {
            // CONCEITO: Encontramos um inimigo. Só atualizamos se for diferente do atual.
            if (currentTargetedEnemy != enemy)
            {
                // CONCEITO: Logger.Log só compila em Editor/Development Build.
                // Em release builds, esta linha é REMOVIDA pelo compilador.
                Logger.Log($"[PlayerInteraction] Detected new enemy: {enemy.name}. Setting as target.");
                currentTargetedEnemy = enemy;
                enemyHealthBarUI.SetTargetEnemy(enemy);
            }
            return;
        }

        // Se chegou aqui: raycast falhou ou o alvo não tem EnemyBase
        if (currentTargetedEnemy != null)
        {
            Logger.Log($"[PlayerInteraction] No enemy targeted or lost target. Current target was: {currentTargetedEnemy.name}. Setting target to null.");
            currentTargetedEnemy = null;
            enemyHealthBarUI.SetTargetEnemy(null);
        }

    #endregion
    }

}
