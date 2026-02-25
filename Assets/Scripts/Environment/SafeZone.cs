using UnityEngine;

// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  SafeZone marca a área protegida da casa. Ela usa um BoxCollider
//  configurado como Trigger para detectar quando o jogador:
//
//    → ENTRA na casa  (OnTriggerEnter) → para o dano da névoa.
//    → SAI  da casa   (OnTriggerExit)  → inicia o dano da névoa.
//
//  O dano em si é gerenciado por PlayerHealth via coroutine interna.
//  SafeZone apenas diz "começa" ou "para" — sem lógica de dano aqui.
//
//  ONDE ADICIONAR:
//  Crie um GameObject vazio filho da casa (ex: "SafeZone").
//  Adicione um BoxCollider → marque "Is Trigger" → ajuste o tamanho.
//  Adicione este script ao mesmo GameObject.
//
//  TAG NECESSÁRIA:
//  O GameObject do Player DEVE ter a tag "Player".
//  (Edit → Project Settings → Tags and Layers → adicione "Player")

/// <summary>
/// Trigger que delimita a área segura da casa.
/// Quando o jogador sai, inicia o dano da névoa em PlayerHealth.
/// Quando o jogador entra, para o dano da névoa.
///
/// Requisitos:
///   - Este GameObject precisa de um BoxCollider com "Is Trigger" marcado.
///   - O Player deve ter a tag "Player".
///   - PlayerHealth deve estar no Player ou em um de seus pais.
/// </summary>
public class SafeZone : MonoBehaviour {

    // ==============================================================
    //  CONFIGURAÇÃO
    // ==============================================================

    [Header("Configuração Inicial")]
    [Tooltip("Marque TRUE se o jogador começa fora da safezone ao iniciar a cena.\n" +
             "Deixe FALSE (padrão) se o jogador começa dentro da casa.")]
    [SerializeField] private bool playerStartsOutside = false;

    // ==============================================================
    //  START — estado inicial
    // ==============================================================

    private void Start() {
        // Se o jogador começar fora da casa (cena específica, etc.),
        // inicia o dano da névoa imediatamente sem esperar pelo trigger.
        if (!playerStartsOutside) return;

        PlayerHealth health = FindPlayerHealth();

        if (health != null)
            health.StartPoisonDamage();
        else
            Debug.LogWarning("[SafeZone] PlayerHealth não encontrado! " +
                             "Verifique se o Player tem a tag 'Player'.");
    }

    // ==============================================================
    //  TRIGGERS
    // ==============================================================

    /// <summary>
    /// Chamado quando um Collider entra nesta área.
    /// Se for o Player, para o dano da névoa.
    /// </summary>
    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) return;

        // ==============================================================
        //  GetComponentInParent<T>()
        // ==============================================================
        //  Busca PlayerHealth no próprio objeto OU em qualquer pai dele
        //  na hierarquia. Isso evita problemas se o Collider do player
        //  está em um filho do GameObject raiz que tem o PlayerHealth.
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

        if (health == null) {
            Debug.LogWarning("[SafeZone] Player entrou na safezone mas PlayerHealth não foi encontrado.");
            return;
        }

        health.StopPoisonDamage();
    }

    /// <summary>
    /// Chamado quando um Collider sai desta área.
    /// Se for o Player, inicia o dano da névoa.
    /// </summary>
    private void OnTriggerExit(Collider other) {
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

        if (health == null) {
            Debug.LogWarning("[SafeZone] Player saiu da safezone mas PlayerHealth não foi encontrado.");
            return;
        }

        health.StartPoisonDamage();
    }

    // ==============================================================
    //  UTILITÁRIO
    // ==============================================================

    private PlayerHealth FindPlayerHealth() {
        // ==============================================================
        //  GameObject.FindWithTag()
        // ==============================================================
        //  Encontra o primeiro GameObject na cena com a tag informada.
        //  É mais rápido que Find(nome), mas AINDA assim deve ser evitado
        //  em Update(). Aqui só rodamos uma vez em Start() — OK.
        GameObject player = GameObject.FindWithTag("Player");
        return player != null ? player.GetComponentInParent<PlayerHealth>() : null;
    }

    // ==============================================================
    //  GIZMOS — visualização no Editor (não aparece no jogo)
    // ==============================================================
    //  Desenha um cubo verde semitransparente no Editor para você
    //  ver exatamente a área da safezone sem precisar rodar o jogo.

    private void OnDrawGizmos() {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);

        Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
