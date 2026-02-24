using System;
using UnityEngine;

// ==============================================================
//  O QUE É UMA CLASSE ABSTRATA?
// ==============================================================
//  Uma classe abstrata é uma classe que NUNCA pode ser instanciada
//  diretamente. Você não pode arrastar o script Enemy.cs num
//  GameObject na Unity — ele serve apenas como BASE para herança.
//
//  ZombieDefault, ZombieFast e ZombieTank são todos inimigos.
//  Todos têm vida, tomam dano e morrem. Em vez de repetir esse
//  código em cada um dos três, colocamos tudo aqui na base.
//
//  A palavra "abstract" em:
//    protected abstract void InitializeStats()
//  significa: "eu declaro que esse método existe, mas cada filho
//  OBRIGATORIAMENTE deve implementar o seu próprio". O compilador
//  rejeita qualquer subclasse que não implemente InitializeStats().
//
//  HERANÇA em C# com ":" :
//    public class ZombieDefault : Enemy
//  → ZombieDefault É UM Enemy. Herda tudo que Enemy tem.

// ==============================================================
//  O QUE FAZ [RequireComponent]?
// ==============================================================
//  Este atributo diz à Unity: "Este script EXIGE que o GameObject
//  também tenha EnemyFollow e EnemyAttack".
//  Se tentar remover um deles no Inspector, a Unity recusa.
//  Se adicionar Enemy a um GameObject vazio, a Unity adiciona
//  EnemyFollow e EnemyAttack automaticamente junto.

/// <summary>
/// Classe abstrata base para todos os tipos de inimigo do jogo.
/// Fornece a lógica compartilhada de vida, recepção de dano e morte.
///
/// Subclasses concretas (ZombieDefault, ZombieFast, ZombieTank)
/// devem implementar InitializeStats() para definir seus próprios stats.
///
/// O evento estático OnAnyEnemyDied permite que o WaveManager conte
/// mortes sem precisar de referência direta a cada inimigo individual.
/// </summary>
[RequireComponent(typeof(EnemyFollow))]
[RequireComponent(typeof(EnemyAttack))]
public abstract class Enemy : MonoBehaviour {

    // ==============================================================
    //  CAMPOS PROTEGIDOS (protected)
    // ==============================================================
    //  "protected" = visível para esta classe E para qualquer classe
    //  que herde dela (os filhos ZombieDefault, ZombieFast, ZombieTank).
    //  É diferente de "private" (só esta classe) e
    //  "public" (qualquer classe do projeto).
    //
    //  Por que NÃO usamos [SerializeField] aqui?
    //  InitializeStats() sobrescreve esses valores durante o Awake.
    //  Se fossem SerializeField, o Inspector mostraria valores que
    //  seriam ignorados em execução — gerando confusão visual.
    //  Para ajustar stats, edite diretamente nos scripts dos subtipos.

    protected float maxHealth      = 100f;  // Vida máxima
    protected float moveSpeed      = 3.5f;  // Velocidade de movimento (metros/segundo)
    protected float attackDamage   = 10f;   // Dano por acerto melee
    protected float attackRange    = 1.8f;  // Distância para iniciar ataque (metros)
    protected float attackCooldown = 1.5f;  // Intervalo mínimo entre ataques (segundos)

    // ==============================================================
    //  ESTADO INTERNO (private)
    // ==============================================================
    //  "private" = só esta classe acessa. Mesmo os filhos não veem
    //  currentHealth diretamente — eles usam GetHealthFraction().

    private float currentHealth;  // Vida atual (cai ao tomar dano)
    private bool  isDead;         // Trava para impedir Die() duplo

    // Referências aos componentes irmãos no mesmo GameObject.
    // "protected" para que subclasses possam acessar se necessário.
    protected EnemyFollow enemyFollow;
    protected EnemyAttack enemyAttack;

    // ==============================================================
    //  EVENTO ESTÁTICO
    // ==============================================================
    //  "static event" pertence à CLASSE, não a um objeto específico.
    //  Qualquer instância de Enemy que morrer dispara este mesmo evento.
    //
    //  Funcionamento passo a passo:
    //    1. WaveManager escreve: Enemy.OnAnyEnemyDied += HandleEnemyDied
    //       (significa: "quando o evento disparar, chame HandleEnemyDied")
    //    2. Qualquer zumbi morre → roda OnAnyEnemyDied?.Invoke()
    //    3. A Unity chama HandleEnemyDied no WaveManager automaticamente
    //
    //  "Action" é um delegate (ponteiro de função) sem parâmetros e
    //  sem retorno. Vive no namespace System (using System no topo).
    //
    //  "?" = operador null-conditional: só invoca se alguém está assinado.
    //  Sem "?", se ninguém assinou, causaria NullReferenceException.

    /// <summary>
    /// Disparado por qualquer Enemy ao morrer. O WaveManager escuta isto
    /// para decrementar o contador de inimigos vivos.
    /// </summary>
    public static event Action OnAnyEnemyDied;

    // ==============================================================
    //  AWAKE — primeiro método chamado pela Unity ao criar o objeto
    // ==============================================================
    //  Ordem dos eventos Unity: Awake → OnEnable → Start → Update
    //  Usamos Awake para cachear componentes porque é o mais cedo
    //  possível, antes de qualquer Start() de outros scripts.
    //
    //  "virtual" = subclasses PODEM sobrescrever com "override", mas
    //  não são obrigadas. Se sobrescreverem, devem chamar base.Awake()
    //  para não perder a lógica aqui.

    protected virtual void Awake() {
        // ==============================================================
        //  GetComponent<T>() — busca um componente no mesmo GameObject
        // ==============================================================
        //  "GetComponent<EnemyFollow>()" = "encontre o script EnemyFollow
        //  pendurado neste mesmo GameObject e me dê a referência".
        //  Salvamos em variável para não buscar novamente a cada frame.

        enemyFollow = GetComponent<EnemyFollow>();
        enemyAttack = GetComponent<EnemyAttack>();

        // ==============================================================
        //  CORREÇÃO DO BUG: inimigos sendo lançados pelo mapa ao tomar tiro
        // ==============================================================
        //  CAUSA DO PROBLEMA:
        //  O projétil (Projectile.cs) tem um Rigidbody com força física
        //  aplicada. Quando colide com o inimigo — que também tem um
        //  Rigidbody — a Unity aplica um IMPULSO físico que lança o inimigo.
        //
        //  O ZombieTank era o mais afetado porque sua velocidade é muito
        //  baixa (1.8 m/s). O NavMeshAgent não conseguia "reabsorver" o
        //  impulso da colisão, e o inimigo voava pelo mapa.
        //
        //  SOLUÇÃO: Rigidbody.isKinematic = true
        //  ┌─────────────────────────────────────────────────────────┐
        //  │  isKinematic = FALSE (padrão)                           │
        //  │    → o Rigidbody responde à física (gravidade, colisões)│
        //  │    → projétil aplica impulso → inimigo é lançado ❌     │
        //  │                                                          │
        //  │  isKinematic = TRUE  (nossa solução)                    │
        //  │    → o Rigidbody IGNORA forças físicas externas         │
        //  │    → NavMeshAgent controla o movimento ✅               │
        //  │    → Collider permanece para detecção de colisão ✅     │
        //  │    → OnCollisionEnter em Projectile.cs ainda dispara ✅ │
        //  │      (porque o PROJÉTIL tem Rigidbody não-kinematic)    │
        //  └─────────────────────────────────────────────────────────┘
        //
        //  REGRA GERAL DA UNITY:
        //  Sempre use isKinematic = true em objetos controlados por
        //  NavMeshAgent, Animator ou qualquer sistema não-físico.
        //  Rigidbody + NavMeshAgent sem isKinematic = comportamento imprevisível.
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        // Deixa cada subtipo definir seus próprios stats ANTES de
        // inicializar a vida. A ORDEM aqui é importante!
        InitializeStats();

        // Inicializa a vida atual com o valor definido em InitializeStats().
        // Exemplo: ZombieTank definiu maxHealth = 350 → currentHealth = 350.
        currentHealth = maxHealth;

        // Empurra os stats para os componentes irmãos.
        // SetSpeed usa inicialização lazy do NavMeshAgent para lidar com
        // a incerteza da ordem de Awake() entre componentes no mesmo GO.
        if (enemyFollow != null)
            enemyFollow.SetSpeed(moveSpeed);

        // Configure() injeta dano, range e cooldown no EnemyAttack.
        if (enemyAttack != null)
            enemyAttack.Configure(attackDamage, attackRange, attackCooldown);
    }

    // ==============================================================
    //  CONTRATO ABSTRATO
    // ==============================================================
    //  "abstract" = compilador EXIGE que cada subclasse implemente.
    //  Esta classe declara que InitializeStats existe, mas não define
    //  o que ele faz — cada filho decide por si.

    /// <summary>
    /// Chamado durante o Awake. Cada subclasse define aqui seus stats:
    /// maxHealth, moveSpeed, attackDamage, attackRange, attackCooldown.
    /// </summary>
    protected abstract void InitializeStats();

    // ==============================================================
    //  RECEPÇÃO DE DANO
    // ==============================================================

    /// <summary>
    /// Reduz a vida atual pelo valor informado.
    /// Dispara a morte se a vida chegar a zero.
    /// Chamado pelo Projectile.cs quando a bala acerta o inimigo.
    /// </summary>
    public void TakeDamage(float amount) {
        // Guarda de segurança: ignora dano se o inimigo já morreu.
        // Sem isso, duas balas chegando no mesmo frame chamariam
        // Die() duas vezes, disparando o evento OnAnyEnemyDied em duplicata
        // e fazendo o WaveManager subtrair 2 do contador por 1 morte.
        if (isDead) return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
            Die();
    }

    // ==============================================================
    //  MORTE
    // ==============================================================

    /// <summary>
    /// Lida com a morte do inimigo: desativa a IA, dispara o evento
    /// global de contagem e agenda a destruição do GameObject.
    /// "virtual" permite que subclasses adicionem efeitos extras
    /// (ex: explosão do ZombieTank) sem perder a lógica base.
    /// </summary>
    protected virtual void Die() {
        // Segundo guarda de segurança — garante execução única.
        if (isDead) return;
        isDead = true;

        // Para o inimigo de se mover e de atacar imediatamente.
        if (enemyFollow != null) enemyFollow.SetMovementEnabled(false);

        // "component.enabled = false" desliga o Update() do componente.
        // O EnemyAttack para de rodar sem precisar destruí-lo.
        if (enemyAttack != null) enemyAttack.enabled = false;

        // ==============================================================
        //  NOTIFICAÇÃO DO WAVEMANAGER via evento estático
        // ==============================================================
        //  OnAnyEnemyDied?.Invoke() dispara o evento.
        //  "?." = null-conditional: se ninguém assinou (lista vazia),
        //  não faz nada — evita NullReferenceException.
        //
        //  O WaveManager recebe isso em HandleEnemyDied() e decrementa
        //  enemiesAlive. Quando chega a 0, a onda termina.
        OnAnyEnemyDied?.Invoke();

        // ==============================================================
        //  Destroy(gameObject, delay)
        // ==============================================================
        //  Remove o GameObject da cena após "delay" segundos.
        //  1.5f = 1,5 segundos de espera para a animação de morte rodar.
        //  O segundo argumento é opcional; Destroy(gameObject) remove
        //  instantaneamente.
        Destroy(gameObject, 1.5f);
    }

    // ==============================================================
    //  GETTER UTILITÁRIO
    // ==============================================================

    /// <summary>
    /// Retorna a fração de vida atual entre 0 (morto) e 1 (cheio).
    /// Exemplo: 70 HP de 100 → retorna 0.7f.
    /// Útil para barras de vida na HUD.
    /// </summary>
    public float GetHealthFraction() => currentHealth / maxHealth;
}
