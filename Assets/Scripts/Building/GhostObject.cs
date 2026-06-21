using UnityEngine;

/// <summary>
/// Attatched to "_Ghost" prefabs. Responsible for changing the ghost's material to green (valid) or red (invalid) based on whether the placement is valid. Controlled by BuildingController.
/// </summary>
public class GhostObject : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Materials")]
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;

    #endregion

    #region FIELDS

    private Renderer[] renderers;
    private bool isPlaceable = false;

    #endregion

    #region PROPERTIES

    public bool IsPlaceable() => isPlaceable;

    #endregion

    #region UNITY

    private void Awake() {
        renderers = GetComponentsInChildren<Renderer>();
    }

    #endregion

    #region METHODS

    public void SetPlaceable(bool placeable) {
        isPlaceable = placeable;

        Material mat = placeable ? validMaterial : invalidMaterial;

        foreach (Renderer r in renderers)
            r.material = mat;
    }

    #endregion
}
