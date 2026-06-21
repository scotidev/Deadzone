using UnityEngine;

/// <summary>
/// Manages 3D item preview in shop UI.
/// Handles spawning, rotating, positioning, scaling, and layer assignment.
/// </summary>
public class ItemPreviewHandler : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Configuration")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private float rotationSpeed = 35f;

    #endregion

    #region FIELDS

    private GameObject activePreviewModel;

    #endregion

    #region UNITY

    private void Update() {
        RotatePreview();
    }

    private void OnDestroy() {
        DestroyPreview();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Shows the preview for a shop item with all its configurations.
    /// </summary>
    public void ShowItem(ShopItemDataSO itemData) {
        if (itemData == null || itemData.PreviewPrefab == null) {
            Debug.LogWarning("[ItemPreviewHandler] ShowItem: itemData or PreviewPrefab is null.", this);
            return;
        }

        DestroyPreview();

        activePreviewModel = Instantiate(itemData.PreviewPrefab, previewAnchor);

        activePreviewModel.transform.localPosition = itemData.PreviewPositionOffset;
        activePreviewModel.transform.localRotation = Quaternion.Euler(itemData.PreviewRotationOffset);
        activePreviewModel.transform.localScale = itemData.PreviewScale;

        AssignWeaponLayer(activePreviewModel);
    }

    /// <summary>
    /// Clears the current preview.
    /// </summary>
    public void DestroyPreview() {
        if (activePreviewModel != null) {
            Destroy(activePreviewModel);
            activePreviewModel = null;
        }
    }

    /// <summary>
    /// Continuously rotates the preview model.
    /// </summary>
    private void RotatePreview() {
        if (activePreviewModel == null) return;
        activePreviewModel.transform.Rotate(Vector3.up, rotationSpeed * Time.unscaledDeltaTime, Space.World);
    }

    /// <summary>
    /// Gets the active preview model.
    /// </summary>
    public GameObject GetActivePreview() => activePreviewModel;

    /// <summary>
    /// Checks if there's an active preview.
    /// </summary>
    public bool HasActivePreview() => activePreviewModel != null;

    /// <summary>
    /// Assigns Weapon layer to object and all children.
    /// </summary>
    private void AssignWeaponLayer(GameObject target) {
        int weaponLayerID = LayerMask.NameToLayer("Weapon");

        if (weaponLayerID < 0) {
            Debug.LogError("[ItemPreviewHandler] Layer 'Weapon' does not exist! Create it in Project Settings \u2192 Tags and Layers.");
            return;
        }

        target.layer = weaponLayerID;
        AssignWeaponLayerRecursive(target.transform, weaponLayerID);
    }

    /// <summary>
    /// Recursively assigns layer to children.
    /// </summary>
    private void AssignWeaponLayerRecursive(Transform parent, int layerID) {
        foreach (Transform child in parent) {
            child.gameObject.layer = layerID;
            AssignWeaponLayerRecursive(child, layerID);
        }
    }

    #endregion
}
