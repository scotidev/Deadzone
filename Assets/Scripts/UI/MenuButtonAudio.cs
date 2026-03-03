using UnityEngine;
using UnityEngine.EventSystems;

// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  MenuButtonAudio toca sons de UI em resposta à interação do jogador.
//  Ele é a contraparte sonora do MenuImageScale — os dois scripts
//  vivem no mesmo GameObject e cada um cuida de uma responsabilidade:
//    MenuImageScale   → visual (escala)
//    MenuButtonAudio  → audio  (sons)
//
//  Por que separar em dois scripts?
//  Separação de responsabilidades: se você quiser um botão sem som,
//  basta não adicionar MenuButtonAudio. O MenuImageScale não precisa
//  saber se existe ou não som — ele só cuida da escala.
//
//  Como funciona a integração com o AudioManager?
//  Este script não tem AudioSource próprio. Ele passa o AudioClip
//  para AudioManager.Instance.PlaySFX(), que usa o canal central de
//  SFX para tocar o som. Assim o volume de todos os sons de UI é
//  controlado em um único lugar.
//
//  Por que não precisa de IPointerUpHandler?
//  O "soltar o clique" não precisa de som — apenas o "apertar" tem feedback
//  sonoro. Interfaces desnecessárias poluem o código sem benefício.

/// <summary>
/// Plays UI audio feedback (hover and click sounds) for menu buttons.
/// Designed to be used alongside <see cref="MenuImageScale"/> on the same GameObject.
/// Routes all audio through <see cref="AudioManager"/> to respect the global SFX volume.
/// </summary>
public class MenuButtonAudio : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler,       IDeselectHandler,
    IPointerDownHandler
{
    // ==============================================================
    //  CLIPS DE AUDIO — atribuir no Inspector
    // ==============================================================
    //  AudioClip é o arquivo de som em si (.wav, .mp3, .ogg).
    //  Diferente de AudioSource, AudioClip não toca nada sozinho —
    //  é só o dado bruto do áudio. Quem toca é o AudioManager.
    //
    //  Deixar um campo vazio (null) é seguro: TryPlayHover() e
    //  OnPointerDown() usam AudioManager.PlaySFX() que já verifica
    //  se o clip é null antes de tentar tocar.

    [Header("Sounds")]
    public AudioClip hoverSound;   // Som tocado ao passar o mouse por cima
    public AudioClip clickSound;   // Som tocado ao pressionar o botão

    // ==============================================================
    //  CONTROLE DE HOVER
    // ==============================================================
    //  isHovered evita que o som de hover toque múltiplas vezes
    //  enquanto o mouse permanece sobre o botão.
    //
    //  Sem essa flag, OnPointerEnter e OnSelect poderiam disparar
    //  em sequência (ex: ao navegar com gamepad e depois mover o mouse),
    //  causando sobreposição de sons.

    private bool isHovered;

    // ==============================================================
    //  EVENTOS DE HOVER — mouse e gamepad
    // ==============================================================

    /// <summary>Called when the pointer enters the element. Triggers hover sound once.</summary>
    public void OnPointerEnter(PointerEventData e) => TryPlayHover();

    /// <summary>Called when the element is selected via keyboard or gamepad. Triggers hover sound once.</summary>
    public void OnSelect(BaseEventData e)           => TryPlayHover();

    // ==============================================================
    //  RESET DO HOVER — ao sair do botão
    // ==============================================================
    //  Resetamos isHovered para que, quando o jogador voltar ao botão,
    //  o som de hover toque novamente. Sem este reset, o som só tocaria
    //  na primeira vez que o mouse entrasse no botão durante aquela sessão.

    /// <summary>Called when the pointer exits the element. Resets the hover guard.</summary>
    public void OnPointerExit(PointerEventData e)  => isHovered = false;

    /// <summary>Called when the element is deselected. Resets the hover guard.</summary>
    public void OnDeselect(BaseEventData e)         => isHovered = false;

    // ==============================================================
    //  EVENTO DE CLIQUE
    // ==============================================================

    /// <summary>Called when the pointer is pressed down. Plays the click sound.</summary>
    public void OnPointerDown(PointerEventData e)
    {
        // "?." é o operador null-conditional: se AudioManager.Instance for null
        // (cena sem AudioManager, por exemplo em testes), simplesmente não faz nada.
        // Evita NullReferenceException sem precisar de um bloco "if" separado.
        AudioManager.Instance?.PlaySFX(clickSound);
    }

    // ==============================================================
    //  LÓGICA DE HOVER COM GUARD
    // ==============================================================

    /// <summary>Plays the hover sound only once per hover session, guarded by the isHovered flag.</summary>
    private void TryPlayHover()
    {
        // Se já estamos com o mouse em cima, não toca de novo.
        // "return" interrompe o método imediatamente — é o "early return" pattern.
        if (isHovered) return;

        AudioManager.Instance?.PlaySFX(hoverSound);

        // Marca como "já tocou" para que chamadas subsequentes
        // (ex: OnSelect logo após OnPointerEnter) sejam ignoradas.
        isHovered = true;
    }
}
