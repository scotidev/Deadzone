using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Relays pointer hover and click events from a map card to the SelectManager.
/// </summary>
public class MapCardPointerRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {

    /// <summary>
    /// Supported map options for this relay component.
    /// </summary>
    private enum MapCardOption {
        City,
        Desert,
        Forest
    }

    [Header("References")]
    [Tooltip("SelectManager responsible for map preview and scene loading")]
    [SerializeField] private SelectManager selectManager;

    [Tooltip("Map option represented by this card")]
    [SerializeField] private MapCardOption option;

    /// <summary>
    /// Handles pointer enter and forwards hover enter to SelectManager.
    /// </summary>
    /// <param name="eventData">Pointer event information provided by Unity.</param>
    public void OnPointerEnter(PointerEventData eventData) {
        // We guard against missing references to prevent null-reference exceptions during UI interactions.
        if (selectManager == null) {
            return;
        }

        // We map the enum option to the specific hover method so each card triggers its correct preview.
        switch (option) {
            case MapCardOption.City:
                selectManager.OnCityHoverEnter();
                break;
            case MapCardOption.Desert:
                selectManager.OnDesertHoverEnter();
                break;
            case MapCardOption.Forest:
                selectManager.OnForestHoverEnter();
                break;
        }
    }

    /// <summary>
    /// Handles pointer exit and forwards hover exit to SelectManager.
    /// </summary>
    /// <param name="eventData">Pointer event information provided by Unity.</param>
    public void OnPointerExit(PointerEventData eventData) {
        // Unity destroyed objects are not always CLR-null, so we guard explicitly before forwarding.
        if (selectManager == null) {
            return;
        }

        // We only need one shared exit call because SelectManager already restores the right visual state.
        selectManager.OnMapHoverExit();
    }

    /// <summary>
    /// Handles pointer click and forwards map selection to SelectManager.
    /// </summary>
    /// <param name="eventData">Pointer event information provided by Unity.</param>
    public void OnPointerClick(PointerEventData eventData) {
        // We guard against missing references to avoid runtime errors when clicking a card.
        if (selectManager == null) {
            return;
        }

        // We map the enum option to the specific select method so each card loads the expected scene.
        switch (option) {
            case MapCardOption.City:
                selectManager.OnCitySelect();
                break;
            case MapCardOption.Desert:
                selectManager.OnDesertSelect();
                break;
            case MapCardOption.Forest:
                selectManager.OnForestSelect();
                break;
        }
    }
}
