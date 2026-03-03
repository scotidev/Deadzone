using System.Collections;
using UnityEngine;

// ==============================================================
//  O QUE FAZ O LogoIntro?
// ==============================================================
//  Responsabilidade ÚNICA e simples: cronometrar a intro e
//  carregar a cena Menu quando o tempo acabar.
//
//  Toda a parte visual (fade in, fade out, animações) é feita
//  diretamente no Animator da Unity — não é responsabilidade
//  do script. Essa separação segue o princípio SRP:
//  "Single Responsibility Principle" (Princípio da Responsabilidade
//  Única) — cada peça faz apenas uma coisa.
//
//  FLUXO:
//    1. Animator toca a animação da intro (fade in + logo + fade out)
//    2. Este script aguarda "duration" segundos em paralelo
//    3. Quando o tempo acaba → SceneLoader.LoadMenu()
//
//  SKIP: qualquer tecla ou clique do mouse pula direto para o Menu.
//
//  DICA: ajuste "duration" no Inspector para combinar exatamente
//  com a duração total da sua Animation Clip no Animator.
public class LogoIntro : MonoBehaviour {

    // ==============================================================
    //  CAMPOS SERIALIZADOS — visíveis e ajustáveis no Inspector
    // ==============================================================
    //  [SerializeField] expõe o campo private no Inspector sem
    //  torná-lo public — boa prática para encapsulamento.
    //  [Tooltip] exibe uma dica ao passar o mouse sobre o campo.

    // ==============================================================
    //  O QUE É duration?
    // ==============================================================
    //  É o tempo total em segundos que a cena Loader fica visível
    //  antes de carregar o Menu. Deve ser igual à duração da sua
    //  Animation Clip no Animator (fade in + hold + fade out).
    //  Ex: animação de 4 segundos → duration = 4
    [Tooltip("Duração total da intro em segundos. Ajuste para coincidir com a Animation Clip do Animator.")]
    [SerializeField] private float duration = 4f;

    [Tooltip("Se verdadeiro, qualquer tecla ou clique do mouse pula a intro e vai direto ao Menu.")]
    [SerializeField] private bool allowSkip = true;

    // ==============================================================
    //  O QUE É O FLAG "skipped"?
    // ==============================================================
    //  Variável de controle que evita chamar GoToMenu() duas vezes.
    //  Cenário problemático sem ela:
    //    Frame X: WaitAndLoad() termina → chama GoToMenu()
    //    Frame X: Input.anyKeyDown também é true → chama GoToMenu() de novo
    //  Com o flag, o segundo caminho é bloqueado após o primeiro disparar.
    private bool skipped = false;

    // ==============================================================
    //  Start() — executado uma vez, no primeiro frame
    // ==============================================================
    //  Por que Start() e não Awake()?
    //  O Start() garante que TODOS os Awake() do projeto já rodaram,
    //  incluindo o do GameManager e o do SceneLoader (que criam seus
    //  singletons). Se tentássemos chamar SceneLoader.Instance no
    //  Awake(), ele poderia ser null dependendo da ordem de execução.
    private void Start() {
        // Informa o GameManager que o jogo está na fase de intro.
        GameManager.Instance?.SetState(GameState.Loader);

        // ==============================================================
        //  StartCoroutine() — iniciando a contagem em paralelo
        // ==============================================================
        //  Uma Coroutine é um método que pode ser PAUSADO e RETOMADO
        //  sem travar o jogo. Usamos aqui para dizer:
        //  "espere 'duration' segundos e depois chame GoToMenu()".
        //
        //  Sem Coroutine, a única forma de esperar seria travar o
        //  Update() com um loop — o que congelaria o jogo inteiro.
        StartCoroutine(WaitAndLoad());
    }

    // ==============================================================
    //  Update() — verificado a cada frame
    // ==============================================================
    //  Detecta input para skip da intro.
    //  Input.anyKeyDown pertence ao Old Input System, mas é adequado
    //  aqui porque queremos capturar QUALQUER tecla/botão/clique
    //  sem precisar configurar uma InputAction específica.
    //  A cena Loader também não tem PlayerInput ativo.
    private void Update() {
        if (allowSkip && !skipped && Input.anyKeyDown)
            SkipIntro();
    }

    // ==============================================================
    //  WaitAndLoad() — a Coroutine de contagem regressiva
    // ==============================================================
    //  IEnumerator é o tipo de retorno obrigatório para Coroutines.
    //
    //  WaitForSeconds(t) é um objeto especial do Unity que diz ao
    //  motor: "pause esta Coroutine por t segundos e depois retome".
    //  Durante esses t segundos o jogo continua rodando normalmente
    //  — Update(), física, animações, tudo funciona.
    private IEnumerator WaitAndLoad() {
        // Pausa esta Coroutine por "duration" segundos.
        // O Animator toca a animação da intro durante este tempo.
        yield return new WaitForSeconds(duration);

        // Tempo esgotado — carrega o Menu.
        GoToMenu();
    }

    // ==============================================================
    //  SkipIntro() — interrompe a espera e vai direto ao Menu
    // ==============================================================
    //  StopAllCoroutines() cancela TODAS as Coroutines deste script,
    //  incluindo o WaitAndLoad() que ainda estaria contando.
    private void SkipIntro() {
        skipped = true;
        StopAllCoroutines();
        GoToMenu();
    }

    // ==============================================================
    //  GoToMenu() — ponto único de saída desta cena
    // ==============================================================
    //  Princípio DRY: "Don't Repeat Yourself" (Não se Repita).
    //  Tanto o fim natural (WaitAndLoad) quanto o skip passam por
    //  aqui — qualquer mudança futura precisa ser feita em um único lugar.
    private void GoToMenu() {
        SceneLoader.Instance?.LoadMenu();
    }
}
