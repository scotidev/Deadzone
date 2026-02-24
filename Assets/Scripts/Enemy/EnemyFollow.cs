using UnityEngine;
using UnityEngine.AI;

// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  EnemyFollow é responsável APENAS pelo movimento do inimigo.
//  Ele usa o NavMeshAgent da Unity para navegar pelo mapa e
//  seguir o jogador automaticamente, desviando de obstáculos.
//
//  SEPARAÇÃO DE RESPONSABILIDADES:
//  Em vez de colocar movimento E ataque no mesmo script,
//  separamos em EnemyFollow (mover) e EnemyAttack (atacar).
//  Isso facilita manutenção: mexer no ataque não afeta o movimento.

// ==============================================================
//  O QUE É O NAVMESH?
// ==============================================================
//  NavMesh (Navigation Mesh) é uma "malha de caminhos" invisível
//  que a Unity gera sobre a geometria da cena. Os agentes (inimigos)
//  só podem caminhar em áreas cobertas pelo NavMesh.
//
//  Vantagens:
//    - Desvio automático de paredes, móveis e obstáculos.
//    - Pathfinding (cálculo de caminho) otimizado internamente.
//    - Sem precisar escrever algoritmos de navegação.
//
//  PASSO OBRIGATÓRIO NO EDITOR:
//    Window → AI → Navigation → aba "Bake" → botão "Bake"
//  Sem isso o inimigo não consegue se mover!

/// <summary>
/// Controla o movimento do inimigo usando o NavMeshAgent da Unity.
/// A cada frame, atualiza o destino do agente para a posição do jogador.
///
/// O movimento pode ser pausado pelo EnemyAttack quando o inimigo
/// está dentro do alcance de ataque melee, evitando que o inimigo
/// deslize pelo jogador enquanto ataca.
///
/// PRÉ-REQUISITO: A cena deve ter um NavMesh baked.
///   Window → AI → Navigation → Bake
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyFollow : MonoBehaviour {

    // ==============================================================
    //  CAMPOS PRIVADOS
    // ==============================================================

    // Referência ao NavMeshAgent — o componente da Unity que faz
    // o pathfinding e move o GameObject pelo NavMesh.
    private NavMeshAgent agent;

    // Transform do jogador — guardamos apenas o Transform (posição,
    // rotação, escala) porque é o único dado que precisamos para seguir.
    private Transform playerTransform;

    // ==============================================================
    //  PROPRIEDADE LAZY (inicialização preguiçosa)
    // ==============================================================
    //  Por que isso existe?
    //  A Unity não garante a ordem de Awake() entre componentes do
    //  mesmo GameObject. Enemy.Awake() pode rodar antes de
    //  EnemyFollow.Awake() e chamar SetSpeed() — que precisa do agent.
    //
    //  A propriedade abaixo garante: "se agent for null, busco agora".
    //  Assim SetSpeed() nunca falha independente da ordem de Awake.

    private NavMeshAgent Agent {
        get {
            // Se agent ainda não foi atribuído, busca agora.
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            return agent;
        }
    }

    // ==============================================================
    //  AWAKE — inicialização antecipada
    // ==============================================================
    //  Fazemos FindPlayer() no Awake (não no Start) porque o
    //  EnemyAttack.Start() precisará chamar GetPlayerTransform()
    //  logo em seguida. Awake sempre roda antes de Start.

    private void Awake() {
        agent = GetComponent<NavMeshAgent>();
        FindPlayer();
    }

    // ==============================================================
    //  UPDATE — roda a cada frame
    // ==============================================================
    //  A cada frame dizemos ao NavMeshAgent: "vá até a posição do
    //  jogador". O agente recalcula o caminho automaticamente se o
    //  jogador se mover, desviando de qualquer obstáculo pelo NavMesh.

    private void Update() {
        // Não navega se: jogador não foi encontrado, agente é nulo,
        // agente está desligado, ou movimento foi pausado pelo EnemyAttack.
        if (playerTransform == null || Agent == null || !Agent.enabled || Agent.isStopped)
            return;

        // ==============================================================
        //  SetDestination(Vector3 position)
        // ==============================================================
        //  Diz ao NavMeshAgent o ponto de destino.
        //  O agente calcula o caminho pelo NavMesh e move o GameObject
        //  frame a frame em direção a esse destino.
        //  Chamando todo frame, garantimos que o destino é atualizado
        //  conforme o jogador se move.
        Agent.SetDestination(playerTransform.position);
    }

    // ==============================================================
    //  API PÚBLICA — chamada por Enemy e EnemyAttack
    // ==============================================================

    /// <summary>
    /// Define a velocidade de movimento do NavMeshAgent.
    /// Chamado por Enemy.Awake() após InitializeStats() configurar moveSpeed.
    /// Usa a propriedade Agent (lazy) para funcionar mesmo que
    /// EnemyFollow.Awake() ainda não tenha rodado.
    /// </summary>
    public void SetSpeed(float speed) {
        // "Agent.speed" é a propriedade do NavMeshAgent que controla
        // quantos metros por segundo o agente se move.
        Agent.speed = speed;
    }

    /// <summary>
    /// Pausa (enabled = false) ou retoma (enabled = true) a navegação.
    ///
    /// "isStopped = true"  → agente para no lugar, mantendo o caminho calculado.
    /// "isStopped = false" → agente retoma o movimento para o último destino.
    ///
    /// Chamado pelo EnemyAttack:
    ///   - Jogador entra no range → SetMovementEnabled(false) → para de mover
    ///   - Jogador sai do range  → SetMovementEnabled(true)  → retoma perseguição
    /// </summary>
    public void SetMovementEnabled(bool enabled) {
        // "isOnNavMesh" verifica se o agente está sobre uma área de NavMesh.
        // Sem essa checagem, "isStopped" causaria erro se o agente não
        // estiver posicionado corretamente no NavMesh ainda.
        if (Agent != null && Agent.isOnNavMesh)
            Agent.isStopped = !enabled;
    }

    /// <summary>
    /// Retorna o Transform do jogador (encontrado pelo FindPlayer).
    /// Chamado pelo EnemyAttack.Start() para obter a referência do jogador
    /// sem precisar fazer outra busca GameObject.FindWithTag().
    /// </summary>
    public Transform GetPlayerTransform() {
        // Lazy fallback: tenta encontrar novamente se perdeu a referência.
        if (playerTransform == null)
            FindPlayer();
        return playerTransform;
    }

    // ==============================================================
    //  MÉTODO PRIVADO AUXILIAR
    // ==============================================================

    private void FindPlayer() {
        // ==============================================================
        //  GameObject.FindWithTag("Player")
        // ==============================================================
        //  Busca em TODA a cena o primeiro GameObject com a tag "Player".
        //  É relativamente custoso (percorre todos os objetos da cena),
        //  por isso fazemos apenas UMA vez no Awake e guardamos a referência.
        //
        //  IMPORTANTE: o GameObject do jogador DEVE ter a tag "Player"
        //  configurada no Inspector, caso contrário retorna null.
        //  Tags são configuradas no topo do Inspector de qualquer objeto:
        //  clique no dropdown "Tag" → selecione "Player".
        GameObject playerObj = GameObject.FindWithTag("Player");

        if (playerObj != null)
            playerTransform = playerObj.transform;
        else
            Debug.LogWarning("[EnemyFollow] Nenhum GameObject com tag 'Player' encontrado. " +
                             "Verifique se o Player está na cena e tem a tag 'Player'.");
    }
}
