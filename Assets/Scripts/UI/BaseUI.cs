using UnityEngine;

/// <summary>
/// Base class for all UI panels in the game.
/// Provides common functionality for showing and hiding panels.
/// </summary>
public abstract class BaseUI : MonoBehaviour
{
    [SerializeField] protected GameObject panel;

    // CORREÇÃO DO BUG: painéis não apareciam na primeira vez que eram abertos.
    //
    // CAUSA DO PROBLEMA (comportamento antigo):
    // Os painéis (PausePanel, ShopPanel, etc.) começavam INATIVOS na cena.
    // O Unity só chama o Start() de um script na primeira vez que o seu GameObject
    // se torna ativo. Portanto, ao chamar Show() pela primeira vez:
    //   - Frame N:   Show() → SetActive(true)  ✓ painel ativado
    //   - Frame N+1: Start() roda pela 1ª vez → SetActive(false)  ✗ painel escondido
    // Na segunda chamada em diante o Start() já havia rodado e não rodava mais,
    // então o painel ficava visível normalmente.
    //
    // SOLUÇÃO:
    // A flag abaixo é marcada como true dentro de Show(), antes de qualquer
    // SetActive. Dessa forma, quando o Start() for eventualmente executado,
    // ele verifica a flag e sabe que Show() já foi chamado — e não esconde o painel.
    private bool _showCalledBeforeStart = false;

    /// <summary>
    /// Shows the UI panel.
    /// </summary>
    public virtual void Show()
    {
        // Marcamos a flag ANTES do SetActive para garantir que,
        // caso o Start() rode no frame seguinte, ele não desfaça este Show().
        _showCalledBeforeStart = true;
        if (panel != null)
        {
            panel.SetActive(true);

            // Force immediate canvas update to work with Time.timeScale = 0
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());
        }
    }

    /// <summary>
    /// Hides the UI panel.
    /// </summary>
    public virtual void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>
    /// Checks if the panel is currently visible.
    /// </summary>
    /// <returns>True if panel is active, false otherwise.</returns>
    public bool IsVisible()
    {
        return panel != null && panel.activeSelf;
    }

    /// <summary>
    /// Initializes the UI panel on awake.
    /// Only hides the panel if it's NOT the same GameObject as this script.
    /// </summary>
    protected virtual void Awake()
    {
        // Don't hide panel in Awake - let Start handle it
        // This prevents issues when panel references itself
    }

    /// <summary>
    /// Hides the panel on Start to ensure it's hidden by default.
    /// </summary>
    protected virtual void Start()
    {
        // Só esconde o painel na inicialização se Show() ainda não tiver sido chamado.
        // Isso evita o bug onde Start() rodava no frame seguinte ao primeiro Show()
        // e apagava o painel que acabara de ser exibido.
        if (!_showCalledBeforeStart && panel != null)
            panel.SetActive(false);
    }
}
