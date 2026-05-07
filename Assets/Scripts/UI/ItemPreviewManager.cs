using UnityEngine;

// REFATORAÇÃO: em vez de estar no ShopUI, a lógica de ajuste de camera para cada arma fica aqui, e na verdade não vamos mais ajustar a camera, ela vai ficar sempre na mesma posição, só que pra resolver o problema de a arma aparecer pequena demais (por causa do canvas ser enorme), vamos ajustar o TAMANHO (scale do item) pelo transform do prefab, e não mais pela posição da câmera. Assim, a câmera fica fixa, e cada arma tem seu próprio scale pré-definido no prefab, o que é mais simples e flexível (cada arma pode ter um scale diferente, sem precisar ajustar a câmera toda vez).

// REFATORAÇÃO: todo item deve rotacionar par ao mesmo lado, alguns items como barril, bear trap vem instanciados com aposição errada porque vieram do blender acredito, então vamos corrigir isso aqui no código, para garantir que todos os itens fiquem com a posição correta (mesmo que o prefab esteja errado). Assim, não precisamos ficar corrigindo os prefabs toda hora, e garantimos que a rotação fique consistente para todos os itens (todos girando para o mesmo lado, por exemplo, para a direita).

/// <summary>
/// Manages the 3D weapon preview display in the shop.
/// Handles spawning, rotating, and destroying preview models.
/// </summary>
public class WeaponPreviewHandler : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Preview Settings")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private float rotationSpeed = 35f;

    #endregion

    #region FIELDS

    private GameObject activePreviewModel;

    #endregion

    #region METHODS

    /// <summary>
    /// Destroys the current preview model if one exists.
    /// Called before spawning a new one, or when closing the shop.
    /// </summary>
    public void DestroyPreview() {
        if (activePreviewModel == null) return;

        Destroy(activePreviewModel);

        activePreviewModel = null;
    }

    /// <summary>
    /// Spawns a new weapon preview model at the anchor position.
    /// </summary>
    /// <param name="previewPrefab">The prefab to instantiate (lightweight 3D model).</param>
    public void SpawnPreview(GameObject previewPrefab) {
        if (previewPrefab == null) {
            Debug.LogWarning("[WeaponPreviewHandler] Tried to spawn preview with null prefab!", this);
            return;
        }

        if (previewAnchor == null) {
            Debug.LogWarning("[WeaponPreviewHandler] No preview anchor assigned!", this);
            return;
        }

        DestroyPreview();

        activePreviewModel = Instantiate(
            previewPrefab,
            previewAnchor.position,
            Quaternion.identity
        );

        activePreviewModel.transform.SetParent(previewAnchor, false);

        activePreviewModel.transform.localPosition = Vector3.zero;
        activePreviewModel.transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Continuously rotates the active preview model around its Y-axis.
    /// Should be called every frame from Update().
    /// </summary>
    public void RotatePreview() {
        if (activePreviewModel == null) return;

        activePreviewModel.transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
    }

    /// <summary>
    /// Gets the currently active preview model.
    /// Useful for debugging or adding additional effects.
    /// </summary>
    /// <returns>The active preview GameObject, or null if none exists.</returns>
    public GameObject GetActivePreview() => activePreviewModel;

    /// <summary>
    /// Checks if a preview is currently active.
    /// </summary>
    /// <returns>True if a preview model exists, false otherwise.</returns>
    public bool HasActivePreview() => activePreviewModel != null;

    #endregion
}
