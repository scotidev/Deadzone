using UnityEngine;
using UnityEngine.EventSystems;

// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  MenuImageScale anima a escala (tamanho) de um elemento de UI
//  em resposta à interação do jogador: hover, seleção por gamepad,
//  pressionar e soltar. Não toca nenhum som — só cuida da escala.
//
//  Como funciona a animação?
//  Em vez de pular diretamente para o tamanho final,
//  usamos Vector3.Lerp no Update() para suavizar a transição.
//  Isso cria um efeito de "elástico" que parece mais vivo.
//
//  Por que tantas interfaces?
//  Cada interface representa um tipo diferente de entrada:
//    IPointerEnterHandler / IPointerExitHandler  → mouse (PC)
//    ISelectHandler       / IDeselectHandler      → gamepad / teclado
//    IPointerDownHandler  / IPointerUpHandler     → pressionar / soltar

/// <summary>
/// Animates the local scale of a UI element in response to pointer
/// and selection events. Works for both mouse and gamepad navigation.
/// Attach alongside <see cref="MenuButtonAudio"/> when sound is needed.
/// </summary>
public class MenuImageScale : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler,
    IPointerDownHandler, IPointerUpHandler
{
    // ==============================================================
    //  ESCALAS — configuráveis no Inspector
    // ==============================================================
    //  Três estados visuais possíveis para o botão:
    //    Normal   → tamanho padrão em repouso
    //    Selected → levemente maior quando o cursor está em cima
    //    Pressed  → levemente menor para dar sensação de "apertar"
    //
    //  Vector3.one = (1, 1, 1) — tamanho 100% original do objeto.
    //  new Vector3(1.15f, 1.15f, 1f) = 115% em X e Y, 100% em Z.
    //  O Z fica em 1 porque em UI 2D não queremos distorção de profundidade.

    public Vector3 normalScale   = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.15f, 1.15f, 1f);
    public Vector3 pressedScale  = new Vector3(0.95f, 0.95f, 1f);

    // ==============================================================
    //  VELOCIDADE DA ANIMAÇÃO
    // ==============================================================
    //  Quanto maior o valor, mais rápido o objeto chega à escala alvo.
    //  Funciona multiplicado pelo Time.deltaTime dentro do Lerp.
    //  Valor 12 = animação rápida mas visível (não instantânea).

    public float speed = 12f;

    // ==============================================================
    //  ESTADO INTERNO
    // ==============================================================
    //  targetScale guarda para qual escala o objeto QUER chegar.
    //  O Update() interpola suavemente do tamanho atual até targetScale
    //  a cada frame. Nunca pulamos diretamente — sempre "escorregamos".

    private Vector3 targetScale;

    // ==============================================================
    //  START — roda uma vez quando o objeto é ativado
    // ==============================================================

    /// <summary>Initializes the target scale to the normal (resting) scale.</summary>
    private void Start() => targetScale = normalScale;

    // ==============================================================
    //  UPDATE — roda todo frame
    // ==============================================================

    /// <summary>Smoothly interpolates the object's local scale toward the target scale each frame.</summary>
    private void Update()
    {
        // Vector3.Lerp(a, b, t) retorna um ponto entre a e b.
        //   a = escala atual do objeto
        //   b = escala que queremos atingir (targetScale)
        //   t = "quanto avançamos neste frame" = speed * deltaTime
        //
        // Como "a" é sempre a escala ATUAL (que já está parcialmente interpolada),
        // a velocidade de aproximação diminui conforme chega ao destino —
        // isso cria o efeito suave de desaceleração ("ease out").
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }

    // ==============================================================
    //  EVENTOS DE PONTEIRO (mouse / touch)
    // ==============================================================
    //  OnPointerEnter → mouse entrou na área do botão
    //  OnPointerExit  → mouse saiu da área do botão
    //
    //  O EventSystem da Unity chama estes métodos automaticamente.
    //  Não precisamos de "if" nem de Update para detectar o hover —
    //  a Unity já fez isso por nós.

    /// <summary>Called when the pointer enters the element. Scales up to selected size.</summary>
    public void OnPointerEnter(PointerEventData e) => targetScale = selectedScale;

    /// <summary>Called when the pointer exits the element. Returns to normal size.</summary>
    public void OnPointerExit(PointerEventData e)  => targetScale = normalScale;

    // ==============================================================
    //  EVENTOS DE SELEÇÃO (gamepad / teclado)
    // ==============================================================
    //  Quando o jogador navega pela UI com gamepad ou Tab/setas,
    //  o EventSystem marca um elemento como "selecionado".
    //  OnSelect / OnDeselect são o equivalente do hover para gamepad.
    //  Sem eles, botões navegados por controle nunca escalariam.

    /// <summary>Called when the element is selected via keyboard or gamepad. Scales up to selected size.</summary>
    public void OnSelect(BaseEventData e)  => targetScale = selectedScale;

    /// <summary>Called when the element is deselected. Returns to normal size.</summary>
    public void OnDeselect(BaseEventData e) => targetScale = normalScale;

    // ==============================================================
    //  EVENTOS DE CLIQUE (pressionar e soltar)
    // ==============================================================
    //  OnPointerDown → dedo/botão foi pressionado sobre o elemento
    //  OnPointerUp   → dedo/botão foi solto
    //
    //  Reduzimos para pressedScale no Down para dar feedback visual
    //  de "afundamento". No Up, voltamos para selectedScale (não normal)
    //  porque o cursor ainda está em cima do botão.

    /// <summary>Called when the pointer is pressed down. Scales down to give a "pressed" feel.</summary>
    public void OnPointerDown(PointerEventData e) => targetScale = pressedScale;

    /// <summary>Called when the pointer is released. Returns to selected size since pointer is still over the element.</summary>
    public void OnPointerUp(PointerEventData e)   => targetScale = selectedScale;
}