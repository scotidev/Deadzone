using UnityEngine;

// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  EnemyAttack é responsável APENAS pelo comportamento de ataque
//  melee do inimigo. A cada frame ele:
//    1. Mede a distância até o jogador.
//    2. Se o jogador estiver dentro de attackRange → para de se mover.
//    3. Se o cooldown passou → aplica dano ao jogador.
//    4. Se o jogador se afastar → retoma a perseguição.
//
//  Os stats (dano, range, cooldown) são injetados por Enemy.Awake()
//  via Configure() — este script não define valores por conta própria.

/// <summary>
/// Controla o comportamento de ataque melee para todos os tipos de inimigo.
///
/// Quando o jogador entra no alcance de ataque (attackRange), o inimigo
/// para de se mover e aplica dano periodicamente via IDamageable.
/// Quando o jogador se afasta, o movimento é retomado automaticamente.
/// </summary>
public class EnemyAttack : MonoBehaviour {

    // ==============================================================
    //  STATS — injetados por Enemy.Awake() via Configure()
    // ==============================================================
    //  Estes campos são "private" porque apenas este script os usa.
    //  Eles não são [SerializeField] porque o Enemy base class é quem
    //  decide os valores após chamar InitializeStats() no subtipo.

    private float attackDamage;    // Dano por acerto
    private float attackRange;     // Distância máxima para atacar (metros)
    private float attackCooldown;  // Segundos entre cada ataque

    // ==============================================================
    //  CONTROLE DE COOLDOWN
    // ==============================================================
    //  Guardamos o momento do último ataque em "lastAttackTime".
    //  Para saber se o cooldown passou:
    //    Time.time - lastAttackTime >= attackCooldown
    //
    //  "Time.time" = contador em segundos desde o início do jogo.
    //  Nunca para. Sempre cresce.

    private float lastAttackTime;

    // ==============================================================
    //  REFERÊNCIAS A OUTROS COMPONENTES
    // ==============================================================

    private EnemyFollow    enemyFollow;       // Para parar/retomar o movimento
    private Transform      playerTransform;   // Posição do jogador
    private IDamageable    playerDamageable;  // Interface de dano do jogador

    // Animator é opcional — se o prefab tiver, toca animação de ataque.
    private Animator animator;

    // ==============================================================
    //  HASH DE ANIMATOR
    // ==============================================================
    //  Em vez de usar a string "Attack" toda frame para acionar o
    //  Animator, convertemos para um número (hash) uma única vez.
    //  Usar hash é mais rápido que comparar strings repetidamente.
    //  Animator.StringToHash("Attack") = identificador numérico único
    //  para o parâmetro "Attack" do Animator Controller.
    private static readonly int HashAttack = Animator.StringToHash("Attack");

    // ==============================================================
    //  AWAKE — inicialização
    // ==============================================================

    private void Awake() {
        // Busca componentes irmãos no mesmo GameObject.
        enemyFollow = GetComponent<EnemyFollow>();
        animator    = GetComponent<Animator>();
        // Nota: animator pode ser null se o prefab não tiver Animator —
        // o código trata isso com checagem "if (animator != null)".
    }

    // ==============================================================
    //  START — roda UMA VEZ após todos os Awake() terminarem
    // ==============================================================
    //  Por que buscar o jogador no Start e não no Awake?
    //  EnemyFollow.Awake() já rodou e encontrou o jogador.
    //  No Start, podemos simplesmente perguntar ao EnemyFollow:
    //  "qual é o Transform do jogador?" — sem outra busca custosa.

    private void Start() {
        // Obtém o jogador via EnemyFollow (que já fez a busca no Awake).
        if (enemyFollow != null)
            playerTransform = enemyFollow.GetPlayerTransform();

        // Fallback: se EnemyFollow não encontrou, tentamos diretamente.
        if (playerTransform == null) {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }

        // ==============================================================
        //  GetComponent<IDamageable>()
        // ==============================================================
        //  Busca um componente que implemente a interface IDamageable.
        //  O PlayerHealth.cs implementa IDamageable, então esta busca
        //  retorna o PlayerHealth do jogador sem precisar saber o tipo
        //  concreto. É o poder das interfaces em ação.
        if (playerTransform != null)
            playerDamageable = playerTransform.GetComponent<IDamageable>();
    }

    // ==============================================================
    //  UPDATE — lógica principal, roda a cada frame
    // ==============================================================

    private void Update() {
        // Sem jogador não faz nada.
        if (playerTransform == null) return;

        // ==============================================================
        //  Vector3.Distance(a, b)
        // ==============================================================
        //  Calcula a distância euclidiana (em linha reta, em metros)
        //  entre dois pontos no espaço 3D.
        //  Mais simples que calcular o módulo do vetor diferença manualmente.
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Verifica se o jogador está dentro do alcance de ataque melee.
        bool inAttackRange = distanceToPlayer <= attackRange;

        // ==============================================================
        //  CONTROLE DO MOVIMENTO
        // ==============================================================
        //  Quando em alcance: para o movimento (inimigo "finca o pé" para atacar).
        //  Quando fora: retoma o movimento (volta a perseguir).
        //  EnemyFollow.SetMovementEnabled(false) → agent.isStopped = true
        //  EnemyFollow.SetMovementEnabled(true)  → agent.isStopped = false
        if (enemyFollow != null)
            enemyFollow.SetMovementEnabled(!inAttackRange);

        // ==============================================================
        //  LÓGICA DE COOLDOWN
        // ==============================================================
        //  Atacamos apenas se:
        //    1. O jogador está dentro do alcance.
        //    2. Já passaram "attackCooldown" segundos desde o último ataque.
        //  Isso evita que o inimigo aplique dano múltiplas vezes por frame.
        if (inAttackRange && Time.time - lastAttackTime >= attackCooldown) {
            Attack();
            lastAttackTime = Time.time; // Registra o momento do ataque.
        }
    }

    // ==============================================================
    //  INJEÇÃO DE STATS
    // ==============================================================

    /// <summary>
    /// Recebe os stats de ataque da classe Enemy base.
    /// Deve ser chamado antes do primeiro Update() deste componente.
    /// Chamado em Enemy.Awake() após InitializeStats() do subtipo rodar.
    /// </summary>
    public void Configure(float damage, float range, float cooldown) {
        attackDamage   = damage;
        attackRange    = range;
        attackCooldown = cooldown;
    }

    // ==============================================================
    //  ATAQUE
    // ==============================================================

    private void Attack() {
        // Dispara o trigger "Attack" no Animator (se existir).
        // O Animator Controller deve ter um parâmetro Trigger chamado "Attack".
        // Triggers são como botões: ficam ativos por apenas um frame,
        // depois voltam ao estado inativo automaticamente.
        if (animator != null)
            animator.SetTrigger(HashAttack);

        // ==============================================================
        //  APLICAÇÃO DE DANO VIA INTERFACE
        // ==============================================================
        //  "playerDamageable?.TakeDamage(attackDamage)"
        //
        //  "?." = null-conditional: se playerDamageable for null
        //  (jogador não tem PlayerHealth), simplesmente não faz nada.
        //
        //  TakeDamage() está definido em PlayerHealth e lida com:
        //    - Reduzir a vida do jogador pelo valor de attackDamage.
        //    - Disparar eventos de UI de vida.
        //    - Chamar Die() se a vida chegar a zero.
        playerDamageable?.TakeDamage(attackDamage);
    }
}
