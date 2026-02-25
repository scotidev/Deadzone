using System.Collections;
using UnityEngine;

// ==============================================================
//  SlowMotionManager
// ==============================================================
//  AULA: O QUE É O PADRÃO SINGLETON?
//
//  Um Singleton garante que exista APENAS UMA instância de um
//  componente em toda a cena. A instância fica guardada em uma
//  propriedade estática (Instance), que pode ser acessada de
//  qualquer script sem precisar de referências no Inspector.
//
//  Uso: SlowMotionManager.Instance?.TriggerSlowMotion(1.0f);
//
//  O "?" antes do ponto é o operador "null-conditional":
//  se Instance for null (manager não está na cena), simplesmente
//  não faz nada — sem erro de NullReferenceException.
// ==============================================================

/// <summary>
/// Manager responsável pelos efeitos de câmera lenta no jogo.
/// Adicione este componente a um GameObject na cena (ex: "_Managers").
/// </summary>
public class SlowMotionManager : MonoBehaviour {

    /// <summary>
    /// Referência estática global. Qualquer script acessa via SlowMotionManager.Instance
    /// </summary>
    public static SlowMotionManager Instance { get; private set; }

    [Header("Slow Motion Settings")]

    // ==============================================================
    //  AULA: [Range(min, max)] e [SerializeField]
    //
    //  [SerializeField] expõe um campo privado no Inspector da Unity.
    //  [Range(0.01f, 0.9f)] cria um slider no Inspector, impedindo
    //  valores inválidos (0 pausaria o jogo; 1 seria velocidade normal).
    //
    //  "f" após um número indica que é do tipo float (ex: 0.2f).
    //  Sem o "f", o C# trataria como double e geraria erro de compilação.
    // ==============================================================
    [Tooltip("Fator de escala do tempo durante a câmera lenta. 0.2 = 20% da velocidade normal.")]
    [Range(0.01f, 0.9f)]
    [SerializeField] private float slowTimeScale = 0.2f;

    // ==============================================================
    //  AULA: POR QUE GUARDAR A REFERÊNCIA DA COROUTINE?
    //
    //  Se dois barris explodirem em sequência (reação em cadeia),
    //  o segundo acionaria TriggerSlowMotion antes do primeiro terminar.
    //  Guardar _activeRoutine nos permite CANCELAR a câmera lenta
    //  anterior e REINICIAR o timer de 1 segundo do zero — assim
    //  explosões em cadeia estendem o efeito naturalmente.
    // ==============================================================
    /// <summary>
    /// Referência da coroutine ativa para podermos cancelá-la se necessário.
    /// </summary>
    private Coroutine _activeRoutine;

    private void Awake() {
        // ==============================================================
        //  AULA: IMPLEMENTAÇÃO DO SINGLETON
        //
        //  Quando a cena carrega, a Unity chama Awake() em todos os
        //  MonoBehaviours. Se já existe uma Instance (outra cópia do
        //  manager), a nova se auto-destrói. Caso contrário, ela
        //  se registra como a instância oficial.
        //
        //  Diferente do AudioManager, não usamos DontDestroyOnLoad aqui
        //  porque a câmera lenta é um efeito de cena — não precisa
        //  persistir entre cenas.
        // ==============================================================
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ==============================================================
    //  AULA: O QUE É UM MÉTODO PÚBLICO?
    //
    //  "public" significa que este método pode ser chamado por
    //  QUALQUER outro script — não só os da mesma classe.
    //  É a "porta de entrada" do nosso sistema de câmera lenta.
    //  O ExplosiveBarrelScript vai chamar exatamente este método.
    // ==============================================================
    /// <summary>
    /// Aciona a câmera lenta por uma duração em tempo REAL.
    /// Se já houver câmera lenta ativa, reinicia o timer.
    /// </summary>
    /// <param name="realDuration">Duração em segundos reais (não afetados pelo timeScale).</param>
    public void TriggerSlowMotion(float realDuration) {
        // Se uma câmera lenta já está rodando (explosão anterior ainda não terminou),
        // cancelamos a coroutine para reiniciar o contador do zero.
        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);

        // StartCoroutine inicia a execução da função SlowMotionRoutine de forma
        // assíncrona — ela roda "em paralelo" com o resto do jogo, pausando
        // apenas nos "yield return" sem travar o Update principal.
        _activeRoutine = StartCoroutine(SlowMotionRoutine(realDuration));
    }

    // ==============================================================
    //  AULA: O QUE É UMA COROUTINE?
    //
    //  Uma Coroutine é uma função que pode ser "pausada" no meio
    //  da execução com "yield return" e continuada depois, sem
    //  travar o jogo. É como um roteiro com marcadores de pausa.
    //
    //  "IEnumerator" é o tipo de retorno obrigatório para coroutines.
    //  O C# usa esse tipo para saber que a função pode ser pausada.
    //
    //  Fluxo desta coroutine:
    //    1. Reduz o Time.timeScale  →  câmera lenta começa
    //    2. yield return WaitForSecondsRealtime  →  espera 1s REAL
    //    3. Restaura o Time.timeScale  →  câmera lenta termina
    // ==============================================================
    /// <summary>
    /// Coroutine que executa o ciclo completo da câmera lenta.
    /// </summary>
    private IEnumerator SlowMotionRoutine(float realDuration) {

        // ── INÍCIO DA CÂMERA LENTA ──────────────────────────────────
        // Time.timeScale é o "acelerador/freio" global do tempo na Unity.
        // 1.0 = velocidade normal | 0.0 = jogo pausado | 0.2 = 20% da velocidade.
        Time.timeScale = slowTimeScale;

        // DETALHE TÉCNICO IMPORTANTE: Time.fixedDeltaTime é o intervalo
        // entre cada chamada do FixedUpdate (responsável pela física).
        //
        // Quando mudamos o timeScale, o fixedDeltaTime precisa ser ajustado
        // proporcionalmente. A fórmula padrão da Unity é:
        //     fixedDeltaTime = 0.02f * timeScale
        //
        // (0.02f = padrão de 50 atualizações físicas por segundo)
        //
        // Sem este ajuste, a física roda de forma inconsistente durante
        // a câmera lenta — objetos podem ter comportamento estranho.
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // ── ESPERA EM TEMPO REAL ─────────────────────────────────────
        // POR QUE não usar WaitForSeconds(realDuration)?
        //
        //   WaitForSeconds respeita o timeScale.
        //   Com timeScale = 0.2, esperar 1s de jogo = 5s reais. Errado!
        //
        //   WaitForSecondsRealtime ignora o timeScale completamente.
        //   Sempre espera o tempo real do relógio — exatamente o que queremos.
        yield return new WaitForSecondsRealtime(realDuration);

        // ── FIM DA CÂMERA LENTA ──────────────────────────────────────
        // Restaura o tempo para velocidade normal.
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        // Limpa a referência, sinalizando que não há câmera lenta ativa.
        _activeRoutine = null;
    }
}
