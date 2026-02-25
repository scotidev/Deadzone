using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  WaveManager é o "maestro" do sistema de hordas. Ele coordena:
//    1. O início de cada onda (quando o jogador interage com WaveButton).
//    2. Quantos inimigos spawnar e de quais tipos.
//    3. A distribuição de inimigos entre os spawners da cena.
//    4. A contagem de mortes (ouvindo o evento Enemy.OnAnyEnemyDied).
//    5. O fim da onda (quando todos morreram) e a liberação do botão.
//
//  PADRÃO SINGLETON:
//  "public static WaveManager Instance" garante que exista apenas
//  uma instância. Outros scripts acessam via WaveManager.Instance
//  sem precisar de referência arrastada no Inspector.

/// <summary>
/// Manager singleton responsável por todo o ciclo de vida das ondas:
///   1. Jogador interage com WaveButton → StartNextWave() é chamado.
///   2. Calcula quantos inimigos e quais tipos estão disponíveis.
///   3. Distribui os inimigos entre os EnemySpawners da cena.
///   4. Ouve Enemy.OnAnyEnemyDied para contar mortes.
///   5. Quando o último inimigo morre, encerra a onda.
///
/// REGRAS DE ESCALONAMENTO:
///   Onda 1          → sempre 5 inimigos, só ZombieDefault.
///   Ondas 2–3       → +12–16% vs onda anterior, ZombieDefault + ZombieFast.
///   Onda 4 em diante → +12–16% vs onda anterior, todos os três tipos.
/// </summary>
public class WaveManager : MonoBehaviour {

    // ==============================================================
    //  SINGLETON
    // ==============================================================
    //  "{ get; private set; }" = qualquer script pode LER (get público)
    //  mas só WaveManager pode ESCREVER (set privado).
    //  Inicializado em Awake(), nulo antes disso.
    public static WaveManager Instance { get; private set; }

    // ==============================================================
    //  CAMPOS SERIALIZADOS — configurar no Inspector
    // ==============================================================

    // ==============================================================
    //  TIPOS DE INIMIGOS — lista extensível
    // ==============================================================
    //  Em vez de três campos separados (zombieDefaultPrefab, etc.),
    //  usamos uma List<EnemySpawnConfig> que aceita QUALQUER número
    //  de tipos de inimigo. Para adicionar um Boss futuramente:
    //    → Clique "+" na lista no Inspector.
    //    → Atribua o prefab, defina minimumWave e spawnWeight.
    //  Nenhuma linha de código precisa ser alterada.

    [Header("Tipos de Inimigos")]
    [Tooltip("Lista de todos os tipos de inimigo.\n" +
             "Configure prefab, onda mínima e peso de spawn para cada um.\n" +
             "Sugestão inicial: Default weight=5, Fast weight=3, Tank weight=1.")]
    [SerializeField] private List<EnemySpawnConfig> enemyTypes;

    // ==============================================================
    //  ESCALONAMENTO PROGRESSIVO DE ONDA
    // ==============================================================
    //  A taxa de crescimento começa alta e diminui a cada onda,
    //  até atingir o piso (minGrowthRate). Isso cria uma curva de
    //  dificuldade que acelera no início e estabiliza no longo prazo.
    //
    //  Fórmula: growth(wave) = Max(min, initial - (wave-2) * decrement)
    //  Exemplos com padrões (initial=25%, decrement=2%, min=5%):
    //    Onda 2: 25%   Onda 3: 23%   Onda 4: 21%
    //    Onda 5: 19%   Onda 6: 17%   Onda 7: 15%
    //    Onda 8: 13%   Onda 9: 11%   Onda 10: 9%
    //    Onda 11: 7%   Onda 12+: 5%  (piso atingido)

    [Header("Escalonamento de Onda")]
    [Tooltip("Taxa de crescimento da onda 1 para a 2. 0.25 = +25%.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float initialGrowthRate = 0.25f;

    [Tooltip("Quanto a taxa reduz a cada onda. 0.02 = -2% por onda.")]
    [Range(0f, 0.1f)]
    [SerializeField] private float growthDecrement = 0.02f;

    [Tooltip("Taxa mínima de crescimento (piso). 0.05 = +5%.")]
    [Range(0.01f, 0.2f)]
    [SerializeField] private float minGrowthRate = 0.05f;

    [Tooltip("Número máximo de inimigos que podem existir em uma onda.\n" +
             "Protege contra crescimento exponencial em ondas muito altas.")]
    [SerializeField] private int maxEnemiesPerWave = 500;

    [Header("Spawners da Cena")]
    [Tooltip("Arraste aqui TODOS os GameObjects com EnemySpawner da cena.")]
    [SerializeField] private List<EnemySpawner> spawners;

    // ==============================================================
    //  LIMITE DE INIMIGOS SIMULTÂNEOS
    // ==============================================================
    //  Em vez de spawnar todos os inimigos da onda de uma vez
    //  (o que causaria lag/travamento), mantemos no máximo
    //  maxEnemiesAliveAtOnce inimigos vivos ao mesmo tempo.
    //
    //  Quando um inimigo morre e a contagem cai abaixo do limite,
    //  um novo é spawnado imediatamente para repor a vaga.
    //
    //  O contador da HUD mostra o TOTAL restante da onda:
    //  inimigos vivos na cena + inimigos ainda não spawnados.

    [Header("Limite de Inimigos Simultâneos")]
    [Tooltip("Máximo de inimigos vivos ao mesmo tempo na cena.\n" +
             "Quando um morre, um novo é spawnado para repor a vaga.\n" +
             "Valor sugerido: 10–20 para evitar lag.")]
    [SerializeField] private int maxEnemiesAliveAtOnce = 15;

    [Header("HUD")]
    [Tooltip("Referência ao componente WaveUI na cena.")]
    [SerializeField] private WaveUI waveUI;

    // ==============================================================
    //  ESTADO DA ONDA
    // ==============================================================
    //  Três contadores separados permitem calcular:
    //    Vivos na cena   = enemiesSpawned - enemiesKilled
    //    Restantes total = totalEnemiesForWave - enemiesKilled  ← usado na HUD
    //
    //  Isso garante que o contador mostra a onda INTEIRA,
    //  não apenas os inimigos atualmente na cena.

    private int  currentWave          = 0;     // Onda atual (0 = nenhuma iniciada)
    private int  totalEnemiesForWave  = 0;     // Total a spawnar nesta onda
    private int  enemiesSpawned       = 0;     // Quantos já foram spawnados
    private int  enemiesKilled        = 0;     // Quantos já morreram
    private int  lastWaveEnemyCount   = 5;     // Base para o cálculo da próxima onda
    private bool isWaveActive         = false; // Trava o WaveButton durante a onda

    // Cache dos tipos disponíveis na onda atual — evita recalcular a cada spawn.
    private List<EnemySpawnConfig> currentWaveEnemyTypes;

    // ==============================================================
    //  PROPRIEDADES PÚBLICAS (somente leitura)
    // ==============================================================
    //  "=>" é uma expression body — atalho para { return ...; }
    //  Outros scripts (WaveButton) leem IsWaveActive para decidir
    //  se permitem nova interação.

    /// <summary>True enquanto uma onda está em andamento.</summary>
    public bool IsWaveActive => isWaveActive;

    /// <summary>Número da onda atual (0 = nenhuma iniciada ainda).</summary>
    public int CurrentWave => currentWave;

    // ==============================================================
    //  UNITY — AWAKE / ONENABLE / ONDISABLE
    // ==============================================================

    private void Awake() {
        // Lógica Singleton: se não existe instância, eu sou ela.
        // Se já existe outra, me destruo para não duplicar.
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnEnable() {
        // ==============================================================
        //  INSCRIÇÃO EM EVENTO (subscribe)
        // ==============================================================
        //  "+=" adiciona HandleEnemyDied à lista de ouvintes do evento.
        //  Toda vez que qualquer Enemy morrer e disparar OnAnyEnemyDied,
        //  a Unity chamará automaticamente HandleEnemyDied() aqui.
        //
        //  Por que em OnEnable e não em Awake?
        //  OnEnable/OnDisable são PARES garantidos: sempre que o objeto
        //  for ativado/desativado, OnEnable/OnDisable rodam em sequência.
        //  Isso garante que nunca esquecemos de cancelar a inscrição.
        Enemy.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable() {
        // ==============================================================
        //  CANCELAR INSCRIÇÃO (unsubscribe)
        // ==============================================================
        //  "-=" remove HandleEnemyDied da lista de ouvintes.
        //  OBRIGATÓRIO: sem isso, quando o WaveManager for destruído,
        //  o evento ainda tentaria chamar HandleEnemyDied num objeto
        //  que não existe mais → MissingReferenceException.
        Enemy.OnAnyEnemyDied -= HandleEnemyDied;
    }

    // ==============================================================
    //  API PÚBLICA — chamada pelo WaveButton
    // ==============================================================

    /// <summary>
    /// Inicia a próxima onda de inimigos.
    /// Chamado por WaveButton.Interact() quando o jogador pressiona E.
    /// Bloqueia se uma onda já estiver em andamento.
    /// </summary>
    public void StartNextWave() {
        // Guard: bloqueia chamadas duplicadas enquanto a onda está ativa.
        if (isWaveActive) {
            Debug.LogWarning("[WaveManager] StartNextWave() chamado durante onda ativa. Ignorado.");
            return;
        }

        // Verifica se há spawners configurados antes de continuar.
        if (spawners == null || spawners.Count == 0) {
            Debug.LogError("[WaveManager] Nenhum EnemySpawner atribuído! Configure no Inspector.");
            return;
        }

        // Incrementa o número da onda.
        currentWave++;

        // Calcula o total de inimigos desta onda e reseta os contadores.
        totalEnemiesForWave = GetEnemyCountForWave(currentWave);
        lastWaveEnemyCount  = totalEnemiesForWave;
        enemiesSpawned      = 0;
        enemiesKilled       = 0;
        isWaveActive        = true;

        // Notifica o GameManager do novo estado (bloqueia loja, etc.).
        GameManager.Instance?.SetState(GameState.InWave);

        // Cacheia os tipos disponíveis para esta onda.
        currentWaveEnemyTypes = GetAvailableEnemyTypes(currentWave);

        // Spawna o lote inicial (até maxEnemiesAliveAtOnce inimigos de uma vez).
        // Os inimigos restantes são spawnados progressivamente conforme outros morrem.
        StartCoroutine(SpawnInitialBatch());

        // HUD: mostra o total da onda — inclui os que ainda não foram spawnados.
        if (waveUI != null) {
            waveUI.UpdateWaveNumber(currentWave);
            waveUI.UpdateEnemiesRemaining(totalEnemiesForWave);
            waveUI.SetStatus($"Wave {currentWave} — Sobreviva!");
        }

        Debug.Log($"[WaveManager] Onda {currentWave} iniciada — {totalEnemiesForWave} inimigos totais " +
                  $"(máx {maxEnemiesAliveAtOnce} simultâneos em {spawners.Count} spawner(s)).");
    }

    // ==============================================================
    //  SPAWN THROTTLED (com limite de simultâneos)
    // ==============================================================

    /// <summary>
    /// Spawna o lote inicial de inimigos ao iniciar a onda.
    /// Limita-se a maxEnemiesAliveAtOnce inimigos, com um pequeno
    /// delay entre cada spawn para evitar picos de CPU num único frame.
    /// </summary>
    private IEnumerator SpawnInitialBatch() {
        // Quantos spawnar agora: o menor entre o limite e o total da onda.
        // Ex: limite=15, total=5 → spawna 5.  limite=15, total=30 → spawna 15.
        int initialCount = Mathf.Min(maxEnemiesAliveAtOnce, totalEnemiesForWave);

        for (int i = 0; i < initialCount; i++) {
            SpawnOneEnemy();
            // Pequeno delay entre cada spawn do lote inicial.
            // Distribui o custo de Instantiate() em vários frames.
            yield return new WaitForSeconds(0.15f);
        }
    }

    /// <summary>
    /// Spawna exatamente UM inimigo em um spawner aleatório da cena.
    /// Incrementa enemiesSpawned e escolhe o tipo pelo peso configurado.
    /// </summary>
    private void SpawnOneEnemy() {
        if (enemiesSpawned >= totalEnemiesForWave) return;
        if (currentWaveEnemyTypes == null || currentWaveEnemyTypes.Count == 0) return;

        // ==============================================================
        //  Random.Range com inteiros → escolha de spawner aleatório
        // ==============================================================
        //  Com N spawners, sorteia um índice entre 0 e N-1.
        //  Distribui os spawns uniformemente entre todos os pontos.
        EnemySpawner spawner = spawners[Random.Range(0, spawners.Count)];
        spawner.SpawnSingleEnemy(currentWaveEnemyTypes);
        enemiesSpawned++;
    }

    /// <summary>
    /// Tenta spawnar o próximo inimigo da fila se:
    ///   1. Ainda há inimigos a spawnar nesta onda.
    ///   2. O número de inimigos vivos está abaixo do limite.
    ///
    /// Chamado sempre que um inimigo morre, garantindo que a cena
    /// se mantém populada sem ultrapassar o limite simultâneo.
    /// </summary>
    private void TrySpawnNext() {
        // Inimigos vivos na cena agora = spawnados - mortos.
        int aliveNow = enemiesSpawned - enemiesKilled;

        if (enemiesSpawned < totalEnemiesForWave && aliveNow < maxEnemiesAliveAtOnce)
            SpawnOneEnemy();
    }

    // ==============================================================
    //  CALLBACK DE MORTE DE INIMIGO
    // ==============================================================

    /// <summary>
    /// Chamado automaticamente toda vez que qualquer Enemy na cena morre.
    /// Assinado ao evento Enemy.OnAnyEnemyDied em OnEnable().
    /// </summary>
    private void HandleEnemyDied() {
        enemiesKilled++;

        // ==============================================================
        //  CONTADOR DA HUD — total restante da onda
        // ==============================================================
        //  Mostramos (total - mortos), não apenas os vivos na cena.
        //  Isso inclui inimigos ainda não spawnados, dando ao jogador
        //  uma noção real do quanto falta para terminar a onda.
        //
        //  Exemplo: onda com 30 inimigos, limite 15 simultâneos.
        //    No início: 15 na cena, 15 na fila → HUD mostra 30.
        //    Morre 1 → 14 na cena, 15 na fila, novo spawna → HUD mostra 29.
        //    Fim: 0 na cena, 0 na fila → HUD mostra 0.
        int totalRemaining = Mathf.Max(0, totalEnemiesForWave - enemiesKilled);
        if (waveUI != null)
            waveUI.UpdateEnemiesRemaining(totalRemaining);

        // Tenta spawnar o próximo inimigo da fila para preencher a vaga.
        TrySpawnNext();

        // Onda completa quando TODOS os inimigos da onda foram mortos.
        if (enemiesKilled >= totalEnemiesForWave)
            OnWaveCompleted();
    }

    /// <summary>
    /// Chamado quando o último inimigo da onda morre.
    /// Libera o WaveButton e atualiza o estado do jogo.
    /// </summary>
    private void OnWaveCompleted() {
        isWaveActive = false;

        // Retorna ao estado Playing para que a loja volte a funcionar.
        GameManager.Instance?.SetState(GameState.Playing);

        if (waveUI != null)
            waveUI.SetStatus($"Wave {currentWave} concluída! Interaja com o Wave Button para continuar.");

        Debug.Log($"[WaveManager] Onda {currentWave} finalizada!");
    }

    // ==============================================================
    //  CÁLCULOS DE ONDA
    // ==============================================================

    /// <summary>
    /// Retorna a quantidade de inimigos para a onda informada.
    ///
    /// Onda 1 → sempre 5 (fixo, amigável para tutorial).
    /// Onda N → Ceil(anterior * (1 + taxa)) limitado por maxEnemiesPerWave.
    ///
    /// A taxa de crescimento é progressivamente reduzida:
    ///   Wave 1→2: initialGrowthRate (padrão 25%)
    ///   Wave 2→3: initial - 1*decrement (23%)
    ///   Wave 3→4: initial - 2*decrement (21%)
    ///   ... até atingir minGrowthRate (padrão 5%)
    ///
    /// Mathf.CeilToInt arredonda para cima, garantindo que o número
    /// de inimigos nunca fique estagnado por truncamento.
    /// </summary>
    private int GetEnemyCountForWave(int wave) {
        if (wave == 1) return 5;

        // ==============================================================
        //  CÁLCULO DA TAXA PROGRESSIVA
        // ==============================================================
        //  (wave - 2) = quantas vezes já reduzimos desde a primeira transição.
        //  Wave 2: (2-2)*decrement = 0   → growth = initialGrowthRate
        //  Wave 3: (3-2)*decrement = 1x  → growth = initial - 1*decrement
        //  Wave 4: (4-2)*decrement = 2x  → growth = initial - 2*decrement
        //  Mathf.Max garante que nunca fique abaixo do piso mínimo.
        float rawGrowth = initialGrowthRate - (wave - 2) * growthDecrement;
        float growth    = Mathf.Max(minGrowthRate, rawGrowth);

        int count = Mathf.CeilToInt(lastWaveEnemyCount * (1f + growth));

        // ==============================================================
        //  LIMITE MÁXIMO DE INIMIGOS
        // ==============================================================
        //  Sem este limite, a fórmula pode produzir centenas de inimigos
        //  em ondas tardias, sobrecarregando a cena.
        //  Mathf.Min retorna o menor dos dois valores.
        return Mathf.Min(count, maxEnemiesPerWave);
    }

    /// <summary>
    /// Retorna a lista de tipos de inimigo permitidos para a onda atual.
    /// Filtra por minimumWave: só inclui tipos cuja onda mínima já foi atingida.
    ///
    /// Extensível: para adicionar o ZombieBoss basta criar uma entrada
    /// na lista enemyTypes no Inspector com minimumWave = onda desejada.
    /// Nenhuma linha de código precisa ser alterada aqui.
    /// </summary>
    private List<EnemySpawnConfig> GetAvailableEnemyTypes(int wave) {
        var available = new List<EnemySpawnConfig>();

        foreach (var config in enemyTypes) {
            // Só inclui se o prefab está atribuído E a onda mínima foi atingida.
            if (config.prefab != null && config.minimumWave <= wave)
                available.Add(config);
        }

        if (available.Count == 0)
            Debug.LogError($"[WaveManager] Nenhum inimigo disponível para a onda {wave}! " +
                           "Verifique as configurações de minimumWave nos Enemy Types.");

        return available;
    }
}
