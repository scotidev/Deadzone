using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the credits panel UI with auto-scrolling effect.
/// </summary>
public class CreditsUI : BaseUI {

    #region SERIALIZED FIELDS

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 50f;
    [SerializeField] private float endPadding = 100f;
    [SerializeField] private bool scrollUp;
    [SerializeField] private float startPadding;

    [Header("Content Prefabs")]
    [SerializeField] private TextMeshProUGUI sectionTitlePrefab;
    [SerializeField] private TextMeshProUGUI linePrefab;

    [Header("Sections (edit these in the Inspector)")]
    [SerializeField] private CreditSection[] sections;

    [Header("UI Elements")]
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private Button closeButton;

    #endregion

    #region FIELDS

    /// <summary>
    /// Enables Escape-close behavior for this panel.
    /// </summary>
    protected override bool CloseOnEscape => true;

    private bool isScrolling;
    private float contentHeight;
    private RectTransform maskRectTransform;

    #endregion

    #region UNITY

    protected override void Awake() {
        base.Awake();
        if (closeButton != null)
            closeButton.onClick.AddListener(OnBackClick);

        if (contentContainer != null) {
            maskRectTransform = contentContainer.parent.GetComponent<RectTransform>();

            contentContainer.anchorMin = new Vector2(0, 1);
            contentContainer.anchorMax = new Vector2(1, 1);
            contentContainer.pivot = new Vector2(0.5f, 1);
            contentContainer.anchoredPosition = Vector2.zero;
            contentContainer.sizeDelta = new Vector2(0, 0);
        }
    }

    public override void Show() {
        base.Show();
        PopulateContent();
        ResetScrollPosition();
        isScrolling = true;
    }

    public override void Hide() {
        isScrolling = false;
        ClearContent();
        base.Hide();
    }

    protected override void Update() {
        base.Update();
        if (!isScrolling || !IsVisible())
            return;

        if (scrollUp) {
            contentContainer.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);

            if (contentContainer.anchoredPosition.y >= contentHeight + endPadding)
                isScrolling = false;
        } else {
            contentContainer.anchoredPosition -= new Vector2(0, scrollSpeed * Time.deltaTime);

            if (contentContainer.anchoredPosition.y <= -(contentHeight + endPadding))
                isScrolling = false;
        }

    }

    #endregion

    #region METHODS

    /// <summary>
    /// Instantiates title and line prefabs for each section into the content container.
    /// </summary>
    private void PopulateContent() {
        foreach (CreditSection section in sections) {
            TextMeshProUGUI title = Instantiate(sectionTitlePrefab, contentContainer);
            title.text = section.sectionTitle;

            foreach (string line in section.lines) {
                TextMeshProUGUI lineText = Instantiate(linePrefab, contentContainer);
                lineText.text = line;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer);
        contentHeight = contentContainer.rect.height;
    }

    /// <summary>
    /// Destroys all dynamically-created content children.
    /// </summary>
    private void ClearContent() {
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
            Destroy(contentContainer.GetChild(i).gameObject);
        contentHeight = 0;
    }

    /// <summary>
    /// Positions content at the start position based on scroll direction.
    /// </summary>
    private void ResetScrollPosition() {
        if (scrollUp)
            contentContainer.anchoredPosition = new Vector2(0, -(maskRectTransform.rect.height + startPadding));
        else
            contentContainer.anchoredPosition = new Vector2(0, maskRectTransform.rect.height + startPadding);
    }

    /// <summary>
    /// Handles the close button click event.
    /// </summary>
    private void OnBackClick() {
        Hide();
    }

    /// <summary>
    /// Handles Escape key behavior by reusing the close action.
    /// </summary>
    protected override void OnEscapePressed() {
        OnBackClick();
    }

    #endregion
}

/// <summary>
/// Serializable data for one credits section (title + lines of text).
/// </summary>
[System.Serializable]
public class CreditSection {
    public string sectionTitle;
    [TextArea(1, 3)] public string[] lines;
}
