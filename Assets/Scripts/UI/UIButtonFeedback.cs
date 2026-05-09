using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Deadzone.Interfaces;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Unified UI button feedback controller that handles both visual scale animation,
/// audio feedback on hover/click events, cursor change, and clickable area precision.
/// </summary>
public class UIButtonFeedback : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler,
    IPointerDownHandler, IPointerUpHandler {

    [Header("Visual Settings")]
    [Tooltip("Scale when button is in normal/idle state")]
    [SerializeField] private Vector3 normalScale = Vector3.one;

    [Tooltip("Scale when button is hovered or selected")]
    [SerializeField] private Vector3 selectedScale = new Vector3(1.15f, 1.15f, 1f);

    [Tooltip("Scale when button is pressed")]
    [SerializeField] private Vector3 pressedScale = new Vector3(0.95f, 0.95f, 1f);

    [Tooltip("Speed of scale interpolation")]
    [SerializeField] private float scaleSpeed = 12f;

    [Header("Audio - Hover")]
    [Tooltip("Sound played when mouse hovers over button")]
    [SerializeField] private AudioClip hoverSound;
    [Tooltip("Volume for hover sound (0 to 1)")]
    [SerializeField] private float hoverVolume = 1f;

    [Header("Audio - Click")]
    [Tooltip("Normal click sound")]
    [SerializeField] private AudioClip clickSound;
    [Tooltip("Volume for click sound (0 to 1)")]
    [SerializeField] private float clickVolume = 1f;

    [Header("Audio - Shop Actions")]
    [Tooltip("Sound played when unlocking an item")]
    [SerializeField] private AudioClip unlockSound;
    [Tooltip("Volume for unlock sound (0 to 1)")]
    [SerializeField] private float unlockVolume = 1f;

    [Tooltip("Sound played when upgrading an item")]
    [SerializeField] private AudioClip upgradeSound;
    [Tooltip("Volume for upgrade sound (0 to 1)")]
    [SerializeField] private float upgradeVolume = 1f;

    [Tooltip("Sound played when reaching max level on upgrade")]
    [SerializeField] private AudioClip maxedOutSound;
    [Tooltip("Volume for maxed out sound (0 to 1)")]
    [SerializeField] private float maxedOutVolume = 1f;

    [Header("Audio - Ammo")]
    [Tooltip("Sound for adding weapon ammo")]
    [SerializeField] private AudioClip ammoClickSound;
    [Tooltip("Volume for ammo click sound (0 to 1)")]
    [SerializeField] private float ammoClickVolume = 1f;

    [Tooltip("Sound for repairing vest")]
    [SerializeField] private AudioClip vestClickSound;
    [Tooltip("Volume for vest click sound (0 to 1)")]
    [SerializeField] private float vestClickVolume = 1f;

    [Tooltip("Sound for adding supplies (medkit/grenade/buildable)")]
    [SerializeField] private AudioClip suppliesClickSound;
    [Tooltip("Volume for supplies click sound (0 to 1)")]
    [SerializeField] private float suppliesClickVolume = 1f;

    [Header("Audio - Disabled")]
    [Tooltip("Sound played when clicking a disabled button")]
    [SerializeField] private AudioClip disabledClickSound;
    [Tooltip("Volume for disabled click sound (0 to 1)")]
    [SerializeField] private float disabledClickVolume = 1f;
    [Tooltip("Callback triggered when clicking a disabled button")]
    [SerializeField] private UnityEngine.Events.UnityEvent<ShopButtonDisabledReason> onDisabledClick;

    [Header("Click Area Fix")]
    [Tooltip("Enable precise click detection using image alpha")]
    [SerializeField] private bool useBoundFix = false;

    [Tooltip("Higher values require more opaque pixels to detect click (0 to 1). 0.1 = any visible pixel, 0.5 = only solid pixels")]
    [SerializeField] private float clickPrecision = 0.5f;

    [Header("Cursor Settings")]
    [Tooltip("Custom cursor texture (use hand_point.png from Kenney cursor pack)")]
    [SerializeField] private Texture2D cursorTexture;

    [Tooltip("Cursor hotspot offset")]
    [SerializeField] private Vector2 cursorHotspot = new Vector2(0, 0);

    private Vector3 targetScale;
    private bool isHovered;
    private IAudioManagerService audioService;
    private Image image;
    private bool isCursorSet;

    private void Awake() {
        targetScale = normalScale;
        audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        
        if (useBoundFix) {
            image = GetComponent<Image>();
        }
    }

    private void Start() {
        if (useBoundFix && image != null) {
            image.alphaHitTestMinimumThreshold = clickPrecision;
        }
    }

    private void Update() {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData e) {
        targetScale = selectedScale;
        TryPlayHover();
        SetCursor(true);
    }

    public void OnPointerExit(PointerEventData e) {
        targetScale = normalScale;
        isHovered = false;
        SetCursor(false);
    }

    public void OnDisable() {
        SetCursor(false);
    }

    public void OnSelect(BaseEventData e) {
        targetScale = selectedScale;
        TryPlayHover();
    }

    public void OnDeselect(BaseEventData e) {
        targetScale = normalScale;
        isHovered = false;
    }

    public void OnPointerDown(PointerEventData e) {
        Button button = GetComponent<Button>();
        if (button != null && !button.interactable) {
            audioService?.PlaySFX2D(disabledClickSound, disabledClickVolume);
            ShopButtonDisabledReason reason = GetButtonDisabledReason();
            onDisabledClick?.Invoke(reason);
            return;
        }

        targetScale = pressedScale;
        PlayClick();
    }

    private ShopButtonDisabledReason GetButtonDisabledReason() {
        if (ShopManager.Instance == null || ShopUI.Instance == null || ShopUI.Instance.SelectedItemData == null) {
            return ShopButtonDisabledReason.None;
        }

        ShopItemDataSO itemData = ShopUI.Instance.SelectedItemData;
        string buttonName = gameObject.name.ToLower();

        bool isActionButton = buttonName.Contains("action") || 
                              buttonName.Contains("upgrade") ||
                              buttonName.Contains("unlock") ||
                              buttonName.Contains("purchase") ||
                              buttonName.Contains("btn") && !buttonName.Contains("ammo");
        bool isAmmoButton = buttonName.Contains("ammo") || 
                            buttonName.Contains("refill") || 
                            buttonName.Contains("replenish");

        if (isActionButton) {
            return ShopManager.Instance.GetActionButtonDisabledReason(itemData);
        }
        
        if (isAmmoButton) {
            return ShopManager.Instance.GetAmmoButtonDisabledReason(itemData);
        }

        return ShopButtonDisabledReason.None;
    }

    public void OnPointerUp(PointerEventData e) {
        targetScale = selectedScale;
    }

    private void TryPlayHover() {
        if (isHovered) return;

        audioService?.PlaySFX2D(hoverSound, hoverVolume);
        isHovered = true;
    }

    private void PlayClick() {
        audioService?.PlaySFX2D(clickSound, clickVolume);
    }

    public void PlayUnlockSound() {
        audioService?.PlaySFX2D(unlockSound, unlockVolume);
    }

    public void PlayUpgradeSound() {
        audioService?.PlaySFX2D(upgradeSound, upgradeVolume);
    }

    public void PlayMaxedOutSound() {
        audioService?.PlaySFX2D(maxedOutSound, maxedOutVolume);
    }

    public void PlayAmmoClickSound() {
        audioService?.PlaySFX2D(ammoClickSound, ammoClickVolume);
    }

    public void PlayVestClickSound() {
        audioService?.PlaySFX2D(vestClickSound, vestClickVolume);
    }

    public void PlaySuppliesClickSound() {
        audioService?.PlaySFX2D(suppliesClickSound, suppliesClickVolume);
    }

    public void PlayDisabledClickSound() {
        audioService?.PlaySFX2D(disabledClickSound, disabledClickVolume);
    }

    private void SetCursor(bool showPointer) {
        if (cursorTexture == null) return;

        if (showPointer && !isCursorSet) {
            Texture2D cursor = CreateCursorTexture(cursorTexture);
            Cursor.SetCursor(cursor, cursorHotspot, CursorMode.Auto);
            isCursorSet = true;
        }
        else if (!showPointer && isCursorSet) {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            isCursorSet = false;
        }
    }

    private Texture2D CreateCursorTexture(Texture2D source) {
        Texture2D cursor = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        cursor.filterMode = FilterMode.Bilinear;
        cursor.wrapMode = TextureWrapMode.Clamp;
        
        Color[] pixels = source.GetPixels();
        cursor.SetPixels(pixels);
        cursor.Apply();
        
        return cursor;
    }
}