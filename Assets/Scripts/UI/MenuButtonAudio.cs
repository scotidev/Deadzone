using UnityEngine;
using UnityEngine.EventSystems;
using InfimaGames.LowPolyShooterPack;

// refatoração: esse script é necessario? ele Implementa IAudioManagerService? eke resoeuta o sistema de audio atual do projeto cm o ServiceLocator? ele esta sendo usado para tocar o som dos botões ao dar hover no menu, ou clicar, mas ele está respeitando o sistema atual? Se não estiver, temos que implementar uma lógica para fazer o que esse script faz (tocar som de hover e click) usando o IAudioManagerService, para garantir que o som seja tocado corretamente em qualquer cena do jogo, sem depender de um singleton específico. Talvez seja preciso deletar esse script e implementar a lógica de áudio diretamente no MenuImageScale, ou em algum outro script como MenuManager, analise necessaria para decidir a melhor abordagem. O importante é garantir que o sistema de áudio seja consistente e funcione em todas as cenas, sem depender de implementações específicas ou singletons que possam não estar presentes em todas as partes do jogo.

/// <summary>
/// Plays UI audio feedback (hover and click sounds) for menu buttons.
/// Designed to be used alongside <see cref="MenuImageScale"/> on the same GameObject.
/// </summary>
public class MenuButtonAudio : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler,
    IPointerDownHandler {

    [Header("Sounds")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    /// <summary>
    /// Flag que controla se o som de hover já foi tocado nesta sessão.
    /// Evita tocar o som múltiplas vezes enquanto o mouse está parado sobre o botão.
    /// </summary>
    private bool isHovered;

    /// <summary>
    /// Referência ao serviço de áudio.
    /// Obtida no Awake através do Service Locator.
    /// </summary>
    private IAudioManagerService audioService;

    private void Awake() {
        // Obtém o serviço de áudio registrado no Bootstraper
        audioService = ServiceLocator.Current.Get<IAudioManagerService>();
    }

    /// <summary>
    /// Called when the pointer enters the element. Triggers hover sound once.
    /// 
    /// IPointerEnterHandler é uma interface do Unity para eventos de mouse.
    /// Quando você implementa esta interface, o Unity chama este método automaticamente.
    /// </summary>
    public void OnPointerEnter(PointerEventData e) => TryPlayHover();

    /// <summary>
    /// Called when the element is selected via keyboard or gamepad. Triggers hover sound once.
    /// 
    /// ISelectHandler permite suporte a navegação por teclado/controle.
    /// Importante para acessibilidade.
    /// </summary>
    public void OnSelect(BaseEventData e) => TryPlayHover();

    /// <summary>
    /// Called when the pointer exits the element. Resets the hover guard.
    /// 
    /// Quando o mouse sai, resetamos a flag para permitir que o som toque
    /// novamente na próxima vez que o mouse entrar.
    /// </summary>
    public void OnPointerExit(PointerEventData e) => isHovered = false;

    /// <summary>
    /// Called when the element is deselected. Resets the hover guard.
    /// </summary>
    public void OnDeselect(BaseEventData e) => isHovered = false;

    /// <summary>
    /// Called when the pointer is pressed down. Plays the click sound.
    /// 
    /// IPointerDownHandler detecta quando o botão do mouse é pressionado.
    /// OnPointerDown dispara no momento do clique, antes de soltar.
    /// </summary>
    public void OnPointerDown(PointerEventData e) {
        // ?. só chama o método se audioService não for null
        // Evita NullReferenceException caso o serviço não esteja disponível
        audioService?.PlaySFX2D(clickSound);
    }

    /// <summary>
    /// Plays the hover sound only once per hover session, guarded by the isHovered flag.
    /// 
    /// Este método é "private" porque só deve ser chamado internamente.
    /// A flag isHovered funciona como um "debounce" para evitar spam de áudio.
    /// </summary>
    private void TryPlayHover() {
        // Early return: sai da função imediatamente se já tocou
        if (isHovered) return;

        // Toca o som de hover usando o serviço de áudio 2D (UI)
        audioService?.PlaySFX2D(hoverSound);

        // Marca como "já tocou" para não repetir
        isHovered = true;
    }
}
