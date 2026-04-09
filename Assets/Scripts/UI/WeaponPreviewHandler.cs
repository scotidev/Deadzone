using UnityEngine;

/// <summary>
/// Manages the 3D weapon preview display in the shop.
/// Handles spawning, rotating, and destroying preview models.
/// </summary>
public class WeaponPreviewHandler : MonoBehaviour
{
    [Header("Preview Settings")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private float rotationSpeed = 35f;

    private GameObject activePreviewModel;

    /// <summary>
    /// Destroys the current preview model if one exists.
    /// Called before spawning a new one, or when closing the shop.
    /// </summary>
    /// <remarks>
    /// CONCEITO: Lifecycle Management
    /// Em jogos, é importante limpar objetos não mais necessários (memory leaks)
    /// Aqui destruímos o preview antigo antes de criar um novo
    /// </remarks>
    public void DestroyPreview()
    {
        // Guard clause: if nothing to destroy, exit early
        if (activePreviewModel == null) return;

        // CONCEITO: Destroy vs DestroyImmediate
        // Destroy() = destroi no final do frame (melhor performance)
        // DestroyImmediate() = destroi agora (às vezes necessário, mas lento)
        // Aqui usamos Destroy() porque não precisa ser imediato
        Destroy(activePreviewModel);
        
        // Set to null para evitar que tentemos destruir novamente
        activePreviewModel = null;
    }

    /// <summary>
    /// Spawns a new weapon preview model at the anchor position.
    /// </summary>
    /// <param name="previewPrefab">The prefab to instantiate (lightweight 3D model).</param>
    /// <remarks>
    /// CONCEITO: Prefab Instantiation
    /// Prefabs são templates reutilizáveis de GameObjects
    /// Instantiate() cria uma cópia em runtime (pode ser modificada sem afetar o prefab)
    /// </remarks>
    public void SpawnPreview(GameObject previewPrefab)
    {
        // Guard clause: if no prefab, can't spawn
        if (previewPrefab == null)
        {
            Debug.LogWarning("[WeaponPreviewHandler] Tried to spawn preview with null prefab!", this);
            return;
        }

        // Guard clause: if anchor doesn't exist, can't position preview
        if (previewAnchor == null)
        {
            Debug.LogWarning("[WeaponPreviewHandler] No preview anchor assigned!", this);
            return;
        }

        // Destroy the old preview before spawning a new one
        DestroyPreview();

        // CONCEITO: Instantiate com posição e rotação
        // Instantiate recebe: (prefab, posição, rotação)
        // Criamos a cópia já posicionada corretamente
        // identity = Quaternion.identity = sem rotação (0,0,0)
        activePreviewModel = Instantiate(
            previewPrefab,
            previewAnchor.position,
            Quaternion.identity
        );

        // CONCEITO: Parent de Transform
        // Quando você faz SetParent(), o objeto fica como filho do pai
        // Isso significa:
        // 1. Posição/rotação agora são RELATIVAS ao pai
        // 2. Se o pai se move, o filho também
        // 3. Facilita organização hierárquica
        // O false = não manter world position (usa posição relativa)
        activePreviewModel.transform.SetParent(previewAnchor, false);

        // CONCEITO: Local Position vs World Position
        // localPosition = posição RELATIVA ao pai
        // Aqui resetamos para (0,0,0) para a preview ficar centrada no anchor
        activePreviewModel.transform.localPosition = Vector3.zero;
        activePreviewModel.transform.localRotation = Quaternion.identity;

        Debug.Log($"[WeaponPreviewHandler] Spawned preview at anchor position.", this);
    }

    /// <summary>
    /// Continuously rotates the active preview model around its Y-axis.
    /// Should be called every frame from Update().
    /// </summary>
    /// <remarks>
    /// CONCEITO: Rotação por Frame
    /// Para rotação suave, multiplica-se a velocidade por Time.deltaTime
    /// Time.deltaTime = tempo desde o último frame (sempre ~0.016s a 60fps)
    /// Assim, a velocidade fica consistente independente do FPS
    /// 
    /// Exemplo: rotationSpeed = 35, Time.deltaTime = 0.016
    /// Rotação por frame = 35 * 0.016 = 0.56 graus
    /// </remarks>
    public void RotatePreview()
    {
        // Guard clause: if no preview model, nothing to rotate
        if (activePreviewModel == null) return;

        // CONCEITO: Rotate com Space.Self
        // Space.Self = rotaciona ao redor dos EIXOS LOCAIS do objeto
        // Space.World = rotaciona ao redor dos EIXOS GLOBAIS do mundo
        // 
        // Aqui queremos que a arma gire no seu próprio eixo Y
        // Então usamos Space.Self
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
}
